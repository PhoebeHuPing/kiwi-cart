using System.Text.Json;
using System.Threading.RateLimiting;
using System.Globalization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using KiwiCart.Api.Middleware;
using KiwiCart.Infrastructure.Data;
using KiwiCart.Infrastructure.TokenProviders;
using KiwiCart.Infrastructure.StoreClients;
using KiwiCart.Infrastructure.Services;
using KiwiCart.Infrastructure.Repositories;
using KiwiCart.Core.Interfaces;
using KiwiCart.Core.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {RequestId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// One-off admin command: export the local product_gtins table to the seed JSON.
// Usage: dotnet run -- export-gtins [optional output path]
if (args.Length > 0 && args[0] == "export-gtins")
{
    var cfg = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
    var conn = cfg.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not configured");

    // Default output: the Infrastructure Seed folder (embedded resource source).
    var output = args.Length > 1
        ? args[1]
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../KiwiCart.Infrastructure/Seed/gtin-seed.json"));

    var count = await KiwiCart.Infrastructure.Seed.GtinSeed.ExportAsync(conn, output);
    Console.WriteLine($"Exported {count} product_gtins rows to {output}");
    return;
}

// One-off admin command: import GTIN seed data from embedded JSON.
// Usage: dotnet run -- seed-gtins
if (args.Length > 0 && args[0] == "seed-gtins")
{
    var cfg = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
    var conn = cfg.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not configured");

    await using var connection = new NpgsqlConnection(conn);
    await connection.OpenAsync();
    
    var inserted = KiwiCart.Infrastructure.Seed.GtinSeed.ImportEmbedded(connection);
    Console.WriteLine($"Imported {inserted} product_gtins rows from embedded seed");
    return;
}

// One-off admin command: import the Woolworths store seed JSON (fetched from
// cdx.nz via the browser, since that host is unreachable server-side) into the
// stores table. Idempotent via UpsertStoresAsync. Usage: dotnet run -- import-ww-stores
if (args.Length > 0 && args[0] == "import-ww-stores")
{
    var cfg = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var seedPath = args.Length > 1
        ? args[1]
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../KiwiCart.Infrastructure/Seed/ww-stores-seed.json"));

    var result = await KiwiCart.Infrastructure.Seed.WoolworthsStoreSeed.ImportAsync(cfg, seedPath);
    Console.WriteLine($"Imported Woolworths stores: inserted {result.Inserted}, updated {result.Updated}");
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "",
        name: "postgresql",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "https://kiwicart.azurewebsites.net",
                "https://kiwi-cart.azurewebsites.net")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
builder.Services.AddRateLimiter(options =>
{
    // Return 429 with a Retry-After hint when a limit is exceeded.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"Too many requests, please try again later.\"}", ct);
    };

    // Global fallback: per-IP fixed window. Applies to every endpoint unless
    // a more specific named policy is attached.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 100,
        });
    });

    // Stricter per-IP limit for the price comparison endpoint (hits external APIs).
    options.AddPolicy("compare", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 20,
        });
    });

    // Tightest per-IP limit for basket comparison: one request fans out to
    // N items x 3 supermarkets of external calls, so it is the most expensive.
    options.AddPolicy("bucket", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 5,
        });
    });

    // Dedicated per-IP limit for AI endpoints. Gemini calls are billed/quota'd
    // and slower than store calls, so this is kept independent and strict.
    options.AddPolicy("ai", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 5,
        });
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Store token providers (Singleton - preserves static cache)
builder.Services.AddSingleton<PakNSaveTokenProvider>();
builder.Services.AddSingleton<NewWorldTokenProvider>();
builder.Services.AddSingleton<WoolworthsTokenProvider>();
builder.Services.AddSingleton<ITokenProvider>(sp => sp.GetRequiredService<PakNSaveTokenProvider>());
builder.Services.AddSingleton<ITokenProvider>(sp => sp.GetRequiredService<NewWorldTokenProvider>());
builder.Services.AddSingleton<ITokenProvider>(sp => sp.GetRequiredService<WoolworthsTokenProvider>());

// HttpClients with Polly resilience
var retryPolicy = HttpPolicyExtensions.HandleTransientHttpError()
    .RetryAsync(2);
var circuitBreakerPolicy = HttpPolicyExtensions.HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

builder.Services.AddHttpClient("PakNSaveAuth", c =>
{
    c.BaseAddress = new Uri("https://www.paknsave.co.nz");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
})
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient("PakNSave", c =>
{
    c.BaseAddress = new Uri("https://api-prod.paknsave.co.nz");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy)
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient("NewWorldAuth", c =>
{
    c.BaseAddress = new Uri("https://www.newworld.co.nz");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
})
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient("NewWorld", c =>
{
    c.BaseAddress = new Uri("https://api-prod.newworld.co.nz");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy)
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient("WoolworthsAuth", c =>
{
    c.BaseAddress = new Uri("https://www.woolworths.co.nz");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
})
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient("Woolworths", c =>
{
    c.BaseAddress = new Uri("https://www.woolworths.co.nz");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy)
.AddPolicyHandler(timeoutPolicy);

// Gemini AI client. Longer per-attempt timeout than store calls (generation is
// slower) and its own retry policy, independent of the supermarket circuit
// breaker so AI failures never trip store traffic and vice versa.
var geminiTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient(GeminiClient.HttpClientName, c =>
{
    c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    c.Timeout = TimeSpan.FromSeconds(35);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(geminiTimeoutPolicy);

// Store API clients (Singleton)
builder.Services.AddSingleton<PakNSaveClient>(sp => new PakNSaveClient(
    sp.GetRequiredService<PakNSaveTokenProvider>(),
    sp.GetRequiredService<IHttpClientFactory>(),
    sp.GetRequiredService<ILogger<PakNSaveClient>>()));
builder.Services.AddSingleton<NewWorldClient>(sp => new NewWorldClient(
    sp.GetRequiredService<NewWorldTokenProvider>(),
    sp.GetRequiredService<IHttpClientFactory>(),
    sp.GetRequiredService<ILogger<NewWorldClient>>()));
builder.Services.AddSingleton<WoolworthsClient>(sp => new WoolworthsClient(
    sp.GetRequiredService<WoolworthsTokenProvider>(),
    sp.GetRequiredService<IHttpClientFactory>(),
    sp.GetRequiredService<ILogger<WoolworthsClient>>()));

// Store aggregator (graceful degradation - one store down doesn't break others)
builder.Services.AddSingleton<StoreApiClient>(sp => sp.GetRequiredService<PakNSaveClient>());
builder.Services.AddSingleton<StoreApiClient>(sp => sp.GetRequiredService<NewWorldClient>());
builder.Services.AddSingleton<StoreApiClient>(sp => sp.GetRequiredService<WoolworthsClient>());
builder.Services.AddSingleton<IStoreAggregator, StoreAggregator>();

// Foodstuffs clients also resolve GTINs via their detail endpoints (for admin backfill).
builder.Services.AddSingleton<IGtinLookupClient>(sp => sp.GetRequiredService<PakNSaveClient>());
builder.Services.AddSingleton<IGtinLookupClient>(sp => sp.GetRequiredService<NewWorldClient>());

// Price comparison services (Scoped - per-request)
builder.Services.AddScoped<IPriceCacheRepository, PriceCacheRepository>();
builder.Services.AddScoped<IProductGtinRepository, ProductGtinRepository>();
builder.Services.AddScoped<IGtinBackfillService, GtinBackfillService>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddScoped<IStoreSyncService, StoreSyncService>();
builder.Services.AddScoped<IPriceCalculator, PriceCalculator>();
builder.Services.AddScoped<IPriceComparisonService, PriceComparisonService>();
builder.Services.AddScoped<IBucketService, BucketService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IFavoritesService, FavoritesService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();

// Gemini AI (Phase 0 base). Options bound from the "Gemini" config section;
// the API key comes from user-secrets locally / Azure App Settings in prod.
builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddScoped<IGeminiClient, GeminiClient>();

// Meal-plan response cache (Phase 1.4). The concrete planner is registered
// directly, then wrapped by a caching decorator so identical prompts within
// the TTL skip the billed Gemini call and the price fan-out. Size limit keeps
// the free-text key space bounded; each entry has Size = 1.
var mealPlanCacheSection = builder.Configuration.GetSection(MealPlanCacheOptions.SectionName);
builder.Services.Configure<MealPlanCacheOptions>(mealPlanCacheSection);
var mealPlanCacheOptions = mealPlanCacheSection.Get<MealPlanCacheOptions>() ?? new MealPlanCacheOptions();
builder.Services.AddMemoryCache(o => o.SizeLimit = mealPlanCacheOptions.MaxEntries);
builder.Services.AddScoped<MealPlanService>();
builder.Services.AddScoped<IMealPlanService>(sp => new CachingMealPlanService(
    sp.GetRequiredService<MealPlanService>(),
    sp.GetRequiredService<IMemoryCache>(),
    sp.GetRequiredService<IOptions<MealPlanCacheOptions>>(),
    sp.GetRequiredService<ILogger<CachingMealPlanService>>()));

// Personalized suggestions (Phase 2). Aggregates the user's favorites, asks
// the AI for relevant products, then reuses the price comparison service to
// cost each and compute cross-store savings.
builder.Services.AddScoped<ISuggestionService, SuggestionService>();

// Auth0 JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth0:Domain"];
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters.RoleClaimType = "https://kiwicart.co.nz/roles";
    });
builder.Services.AddAuthorization();

builder.Services.AddOutputCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
