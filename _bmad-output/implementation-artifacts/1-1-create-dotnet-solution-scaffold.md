---
baseline_commit: 3f202a6d4b845a8aabb0fbe7fc99739e9bdceb9b
---

# Story 1.1: Create .NET Solution Scaffold

Status: done

## Story

As a developer,
I want a properly structured ASP.NET Core 8 solution with Clean Architecture,
so that all subsequent code has a consistent, well-organized foundation to build on.

## Acceptance Criteria

1. Solution `KiwiCart.sln` exists in `server-dotnet/` with 4 projects
2. Project references follow dependency rules: Api→Core+Infrastructure, Infrastructure→Core, Core→nothing
3. `Program.cs` has Swagger, CORS (allow frontend origin), health check `/health`, and JSON serialization configured
4. `dotnet build` succeeds with zero errors and zero warnings
5. `dotnet test` runs (even if no tests yet) without error
6. All naming conventions match architecture document (PascalCase classes, camelCase JSON, etc.)

## Tasks / Subtasks

- [ ] Task 1: Create solution and project structure (AC: #1, #2)
  - [ ] Create `server-dotnet/` directory
  - [ ] Run `dotnet new sln -n KiwiCart` in `server-dotnet/`
  - [ ] Create `src/KiwiCart.Api` (webapi template, no HTTPS redirect, no top-level controllers)
  - [ ] Create `src/KiwiCart.Core` (classlib)
  - [ ] Create `src/KiwiCart.Infrastructure` (classlib)
  - [ ] Create `tests/KiwiCart.Tests` (xunit)
  - [ ] Add all projects to solution
  - [ ] Add project references: Api→Core, Api→Infrastructure, Infrastructure→Core, Tests→All

- [ ] Task 2: Configure KiwiCart.Core project (AC: #6)
  - [ ] Create folder structure: `Entities/`, `DTOs/`, `Interfaces/`, `Exceptions/`
  - [ ] Add placeholder interfaces: `IPriceComparisonService.cs`, `IBucketService.cs`, `IStoreService.cs`, `IFavoritesService.cs`, `ITokenProvider.cs`
  - [ ] Add placeholder entities: `Store.cs`, `Product.cs`, `Price.cs`, `Favorite.cs`, `StoreToken.cs`, `Bucket.cs`, `BucketItem.cs`
  - [ ] Add DTOs: `PriceResult.cs`, `BucketComparisonResult.cs`, `BucketRequest.cs`, `TokenResponse.cs`
  - [ ] Add exceptions: `NotFoundException.cs`, `StoreApiException.cs`

- [ ] Task 3: Configure KiwiCart.Infrastructure project (AC: #6)
  - [ ] Create folder structure: `Data/`, `Repositories/`, `StoreClients/`, `TokenProviders/`, `Services/`
  - [ ] Add NuGet packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Dapper`, `Microsoft.Extensions.Http.Polly`, `Serilog.AspNetCore`
  - [ ] Create empty `AppDbContext.cs` with DbSet placeholders (no migrations yet — that's Story 1.3)

- [ ] Task 4: Configure KiwiCart.Api project (AC: #3, #4)
  - [ ] Add NuGet packages: `Microsoft.AspNetCore.Authentication.JwtBearer`, `AspNetCore.HealthChecks.NpgSql`, `Swashbuckle.AspNetCore`
  - [ ] Configure `Program.cs`:
    - JSON serialization: camelCase property naming
    - Swagger/OpenAPI
    - CORS: allow `http://localhost:5173` (Vite dev) and production origin
    - Health check: `app.MapHealthChecks("/health")`
    - Rate limiting: basic fixed-window placeholder
    - ProblemDetails for error responses
  - [ ] Create `appsettings.json` with structure for ConnectionStrings, Auth0, GoogleMaps
  - [ ] Create `appsettings.Development.json` with local dev values
  - [ ] Create `Middleware/GlobalExceptionHandler.cs` (basic — returns ProblemDetails)
  - [ ] Create empty controllers: `ProductsController.cs`, `StoresController.cs` with route attributes only

- [ ] Task 5: Configure KiwiCart.Tests project (AC: #5)
  - [ ] Add NuGet packages: `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `Moq`
  - [ ] Create one smoke test: `HealthCheckTests.cs` — verify `/health` returns 200

- [ ] Task 6: Verify build (AC: #4, #5)
  - [ ] Run `dotnet build` — zero errors, zero warnings
  - [ ] Run `dotnet test` — health check test passes

## Dev Notes

### Architecture Constraints

- **Dependency direction is CRITICAL:** Core has NO project references. Infrastructure references ONLY Core. Api references both.
- **No business logic in Api project** — controllers only map HTTP → service calls → responses
- **No framework dependencies in Core** — no EF Core attributes, no ASP.NET references in Core

### NuGet Packages (pinned versions)

```xml
<!-- KiwiCart.Api -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="8.*" />

<!-- KiwiCart.Infrastructure -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.*" />
<PackageReference Include="Dapper" Version="2.*" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.*" />
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />

<!-- KiwiCart.Tests -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.*" />
<PackageReference Include="Moq" Version="4.*" />
```

### Program.cs Skeleton

```csharp
var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader().AllowAnyMethod());
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

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### Project Structure Notes

- Solution lives in `server-dotnet/` alongside existing `server/` (parallel migration)
- Frontend `client/` remains untouched in this story
- `vite.config.js` proxy change happens in Story 3.5, not here

### References

- [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions]

## Dev Agent Record

### Agent Model Used

Kiro CLI (Auto)

### Completion Notes List

- All 6 tasks completed successfully
- dotnet build: 0 errors, 0 warnings
- dotnet test: 1 passed (HealthCheckTests), 0 failed
- Merged to main via PRs #48, #49, #50
- Completed: 2026-06-07

### File List

- .gitignore (updated with .NET and AI agent exclusions)
- server-dotnet/KiwiCart.sln
- server-dotnet/src/KiwiCart.Api/KiwiCart.Api.csproj
- server-dotnet/src/KiwiCart.Api/Program.cs
- server-dotnet/src/KiwiCart.Api/appsettings.json
- server-dotnet/src/KiwiCart.Api/appsettings.Development.json
- server-dotnet/src/KiwiCart.Api/Properties/launchSettings.json
- server-dotnet/src/KiwiCart.Api/Controllers/ProductsController.cs
- server-dotnet/src/KiwiCart.Api/Controllers/StoresController.cs
- server-dotnet/src/KiwiCart.Api/Middleware/GlobalExceptionHandler.cs
- server-dotnet/src/KiwiCart.Core/KiwiCart.Core.csproj
- server-dotnet/src/KiwiCart.Core/Interfaces/IPriceComparisonService.cs
- server-dotnet/src/KiwiCart.Core/Interfaces/IBucketService.cs
- server-dotnet/src/KiwiCart.Core/Interfaces/IStoreService.cs
- server-dotnet/src/KiwiCart.Core/Interfaces/IFavoritesService.cs
- server-dotnet/src/KiwiCart.Core/Interfaces/ITokenProvider.cs
- server-dotnet/src/KiwiCart.Core/Entities/Product.cs
- server-dotnet/src/KiwiCart.Core/Entities/Store.cs
- server-dotnet/src/KiwiCart.Core/Entities/Price.cs
- server-dotnet/src/KiwiCart.Core/Entities/Favorite.cs
- server-dotnet/src/KiwiCart.Core/Entities/StoreToken.cs
- server-dotnet/src/KiwiCart.Core/Entities/Bucket.cs
- server-dotnet/src/KiwiCart.Core/Entities/BucketItem.cs
- server-dotnet/src/KiwiCart.Core/DTOs/PriceResult.cs
- server-dotnet/src/KiwiCart.Core/DTOs/BucketComparisonResult.cs
- server-dotnet/src/KiwiCart.Core/DTOs/BucketRequest.cs
- server-dotnet/src/KiwiCart.Core/DTOs/TokenResponse.cs
- server-dotnet/src/KiwiCart.Core/Exceptions/NotFoundException.cs
- server-dotnet/src/KiwiCart.Core/Exceptions/StoreApiException.cs
- server-dotnet/src/KiwiCart.Infrastructure/KiwiCart.Infrastructure.csproj
- server-dotnet/src/KiwiCart.Infrastructure/Data/AppDbContext.cs
- server-dotnet/tests/KiwiCart.Tests/KiwiCart.Tests.csproj
- server-dotnet/tests/KiwiCart.Tests/HealthCheckTests.cs
