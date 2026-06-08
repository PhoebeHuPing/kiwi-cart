using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using KiwiCart.Api.Middleware;
using KiwiCart.Infrastructure.Data;
using KiwiCart.Infrastructure.TokenProviders;
using KiwiCart.Infrastructure.StoreClients;
using KiwiCart.Infrastructure.Services;
using KiwiCart.Infrastructure.Repositories;
using KiwiCart.Core.Interfaces;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "https://kiwicart.azurewebsites.net")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("default", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
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
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2.5));

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
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
})
.AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient("Woolworths", c =>
{
    c.BaseAddress = new Uri("https://www.woolworths.co.nz");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy)
.AddPolicyHandler(timeoutPolicy);

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

// Price comparison services (Scoped - per-request)
builder.Services.AddScoped<IPriceCacheRepository, PriceCacheRepository>();
builder.Services.AddScoped<IPriceCalculator, PriceCalculator>();
builder.Services.AddScoped<IPriceComparisonService, PriceComparisonService>();
builder.Services.AddScoped<IBucketService, BucketService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IFavoritesService, FavoritesService>();

// Auth0 JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth0:Domain"];
        options.Audience = builder.Configuration["Auth0:Audience"];
    });
builder.Services.AddAuthorization();

builder.Services.AddOutputCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
