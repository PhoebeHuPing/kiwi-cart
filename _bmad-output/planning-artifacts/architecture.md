---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments: ['_bmad-output/planning-artifacts/prd-kiwicart/prd.md']
workflowType: 'architecture'
project_name: 'KiwiCart'
user_name: 'Phoebe'
date: '2026-06-07'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**
- 5 FR categories: Search/Compare (core), Basket Comparison, Store Locator, User Accounts, Data Caching
- Core pattern: real-time multi-source API aggregation with hybrid caching
- Basket comparison creates N×3 API fan-out (N items × 3 supermarkets)
- Location services require geospatial calculations (Haversine)

**Non-Functional Requirements:**
- Performance: cache hit <500ms, cache miss <3s, basket <5s for 10 items
- Availability: 99% uptime, 500 concurrent users
- Data freshness: 24-hour cache expiry with background refresh
- Accessibility: WCAG AA, high-contrast, screen-reader support
- Security: JWT auth, HTTPS, minimal PII

**Scale & Complexity:**
- Primary domain: Full-stack Web (API aggregation + SPA)
- Complexity level: Medium
- Estimated architectural components: 8-10

### Technical Constraints & Dependencies

- Unofficial API integrations — no SLA, may break without notice
- Google Maps API — usage-based pricing, needs budget cap
- Auth0 free tier — 7,000 MAU limit
- Single PostgreSQL instance (Render hosting)
- No scraping — standard API calls only

### Cross-Cutting Concerns Identified

1. **Fault Tolerance:** Any supermarket API failure must not crash the entire request
2. **Cache Consistency:** Expiry strategy + non-blocking background refresh
3. **Rate Limiting:** Protect against external API abuse / blocks
4. **Responsive Design:** Desktop + mobile from same codebase
5. **Graceful Degradation:** Per-supermarket availability indicators

## Technology Stack Evaluation

### Current Stack (Frontend — Unchanged)

| Category | Technology | Version |
|----------|-----------|---------|
| Framework | React | 18 |
| Build | Vite | 7 |
| Styling | Tailwind CSS | 4.0 |
| Data Fetching | TanStack Query | 5 |
| Routing | React Router | 7 |
| Auth (Client) | @auth0/auth0-react | 2.x |

### Backend Migration: Express.js → ASP.NET Core 8

**Decision: Migrate backend to ASP.NET Core 8 with Controllers pattern.**

**Rationale:**
- Multi-threaded Kestrel eliminates Express single-thread bottleneck under N×3 API fan-out
- Built-in `IHttpClientFactory` with Polly provides resilience (retry, circuit breaker, timeout) per supermarket
- Native rate limiting middleware protects unofficial APIs from over-calling
- Health checks, structured logging, OpenTelemetry built-in
- EF Core migrations provide safer schema evolution than Knex

**Tradeoffs Accepted:**
- Full backend rewrite required (not incremental)
- Heavier container images (~100MB vs ~40MB Node)
- Smaller NZ .NET talent pool for community contributions
- May be over-engineered for current 500-user target

### Backend Stack (New)

| Category | Technology | Justification |
|----------|-----------|---------------|
| Runtime | ASP.NET Core 8 | Multi-threaded, production-grade |
| API Style | Controllers | 5 domains with cross-cutting concerns |
| ORM (CRUD) | Entity Framework Core | Favorites, users, migrations |
| ORM (Cache) | Dapper | High-throughput cache reads/writes |
| Database | PostgreSQL (Npgsql) | Same as current production DB |
| Auth | JWT Bearer (Auth0) | First-class .NET middleware |
| Resilience | Polly | Circuit breaker + retry per API |
| Rate Limiting | AspNetCore.RateLimiting | Protect upstream APIs |
| Health Checks | AspNetCore.HealthChecks | DB + upstream monitoring |
| Logging | Serilog + OpenTelemetry | Structured observability |

### Solution Structure

```
KiwiCart.sln
├── src/
│   ├── KiwiCart.Api/            → Controllers, DI, middleware, Program.cs
│   ├── KiwiCart.Core/           → Interfaces, DTOs, domain models
│   └── KiwiCart.Infrastructure/ → EF DbContext, HttpClients, cache repository
└── tests/
    └── KiwiCart.Tests/          → Unit + integration tests (xUnit)
```

### Deployment Target

**Primary: Azure App Service (B1 tier, Australia East)**
- Native .NET support, zero cold-start with always-on
- NZ-adjacent region for low latency
- Auto-scale available on higher tiers

**Fallback: Railway**
- Container-based, simpler setup
- Acceptable for staging/early production

### Key Architectural Patterns Enabled by .NET

| Problem | .NET Solution |
|---------|--------------|
| Socket exhaustion on parallel API calls | `IHttpClientFactory` named clients |
| Upstream API instability | Polly circuit breaker per supermarket |
| No rate limiting | `AddFixedWindowLimiter` middleware |
| No health monitoring | `/health` endpoint with DB + API probes |
| Connection pool exhaustion | Npgsql built-in pooling + EF Core management |
| Slow upstream degrades all requests | Polly timeout policy (fail-fast at 2.5s) |

### Production Hardening (Carried from Party Mode Review)

**Must-fix before launch:**
1. Rate limiting on all public endpoints
2. Circuit breakers on all 3 supermarket API clients
3. Input validation via FluentValidation or DataAnnotations
4. Security headers (CORS, HSTS) via middleware
5. Structured error responses (ProblemDetails RFC 7807)
6. Health check endpoint for deployment orchestration

**Should-fix:**
- Monitoring/alerting (Application Insights or Sentry)
- CI/CD pipeline (GitHub Actions → Azure)
- Staging environment for pre-production validation

## Core Architectural Decisions

### API & Communication

| Decision | Choice | Rationale |
|----------|--------|-----------|
| API Versioning | URL-based (`/api/v1/`) | Compatible with existing frontend, simple, explicit |
| Error Format | RFC 7807 ProblemDetails | .NET built-in, structured, machine-readable |
| API Documentation | Swagger/OpenAPI (Swashbuckle) | Auto-generated from controllers |

### Data Architecture

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Production DB | PostgreSQL (Npgsql) | Existing production database, no migration needed |
| Local Dev DB | PostgreSQL via Docker | Parity with production, avoids SQLite migration bugs |
| ORM (CRUD) | EF Core 8 | Migrations, relationships, favorites/users |
| ORM (Cache) | Dapper | Raw SQL performance for high-throughput cache ops |
| DB Cache Expiry | 24 hours per product-supermarket pair | Balance freshness vs API load |
| Response Cache | 5-minute output cache for identical search queries | Reduces DB hits under repeated searches |

### Authentication & Security

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Auth Provider | Auth0 JWT Bearer | Existing integration, .NET first-class support |
| Authorization | Policy-based (ownership check on DELETE) | Fix current bug: any user can delete any product |
| Rate Limiting | Fixed window per IP | Protect upstream unofficial APIs |
| Input Validation | DataAnnotations + FluentValidation | Prevent injection, enforce constraints |
| Security Headers | CORS + HSTS via middleware | Standard hardening |

### Infrastructure & Deployment

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Hosting | Azure App Service (B1, Australia East) | Native .NET, always-on, NZ-adjacent |
| CI/CD | GitHub Actions | Free for public repos, .NET templates available |
| Environments | Production + Staging | Validate before deploy |
| Monitoring | Application Insights + Serilog | Azure-native, structured logs |
| Health Checks | `/health` endpoint (DB + upstream APIs) | Deployment orchestration + alerting |

### Frontend Architecture (Unchanged)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| State Management | TanStack Query (server state) + React Context (client state) | Already working, appropriate for data-fetching app |
| Routing | React Router 7 | Already implemented |
| API Client | Fetch via TanStack Query | Stale-while-revalidate pairs with backend cache |
| Styling | Tailwind CSS 4 | Already implemented, utility-first |

### Deferred Decisions (V2)

| Decision | Defer Reason |
|----------|-------------|
| WebSocket for real-time price updates | Not needed at 500 users with 24h cache |
| Redis as cache layer | PostgreSQL sufficient at current scale |
| Microservices split | Monolith appropriate for current complexity |
| CDN for static assets | Azure App Service handles static serving |

## Implementation Patterns & Consistency Rules

### Naming Conventions

| Location | Convention | Example |
|----------|-----------|---------|
| C# Class/Method | PascalCase | `StoresController`, `CompareAsync()` |
| C# Private Field | _camelCase | `_tokenProvider`, `_httpClient` |
| C# Interface | I-prefix | `IStoreService`, `IBucketService` |
| API Routes | kebab-case | `/api/v1/products/compare-bucket` |
| DB Tables | snake_case plural | `stores`, `prices`, `store_tokens` |
| DB Columns | snake_case | `store_name`, `expires_at` |
| JSON Response | camelCase | `{ "storeName": "", "totalPrice": 0 }` |
| React Components | PascalCase | `BucketComparisonDisplay.tsx` |
| React Hooks | camelCase use-prefix | `useBucket()` |

### Domain Terminology

| Old Term | New Term | Usage |
|----------|----------|-------|
| Supermarket | Store | All code, DB, API, UI |
| Basket | Bucket | All code, DB, API, UI |

### Domain Model (OOP)

```csharp
// === Entities (identity + behavior) ===

public class Store
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string LogoUrl { get; private set; }
    public string Address { get; private set; }
    public double Lat { get; private set; }
    public double Lng { get; private set; }

    public double DistanceTo(double lat, double lng) { /* Haversine */ }
}

public class Bucket
{
    public List<BucketItem> Items { get; private set; } = new();

    public void AddItem(string productName, int quantity) { ... }
    public void RemoveItem(string productName) { ... }
    public bool IsEmpty => Items.Count == 0;
}

public class BucketItem
{
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }

    public void UpdateQuantity(int qty)
    {
        if (qty <= 0) throw new ValidationException("Quantity must be positive");
        Quantity = qty;
    }
}

public class StoreToken
{
    public int Id { get; set; }
    public string StoreName { get; set; }
    public string Token { get; set; }
    public DateTime IssuedAt { get; set; }   // Store-provided
    public DateTime ExpiresAt { get; set; }  // Store-provided
    public DateTime UpdatedAt { get; set; }  // Our write time

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsNearExpiry => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
}

// === Value Objects (no identity, immutable) ===

public record PriceResult(
    string ProductName, string ImageUrl, string StoreName,
    string LogoUrl, decimal Price, string UnitPrice);

public record BucketComparisonResult(
    string StoreName, string LogoUrl, decimal TotalPrice,
    int ItemsFound, List<string> MissingItems, List<BucketItemDetail> Details);

public record TokenResponse(string Token, DateTime IssuedAt, DateTime ExpiresAt);
```

### Token Management Pattern

**Three-layer cache: Static Memory → DB → Store API**

```csharp
public abstract class CachedTokenProvider : ITokenProvider
{
    private static readonly ConcurrentDictionary<string, StoreToken> _cache = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);

    protected abstract string StoreName { get; }

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        // Layer 1: Static memory (fastest, shared across all requests)
        if (_cache.TryGetValue(StoreName, out var cached) && !cached.IsExpired)
            return cached.Token;

        await _lock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(StoreName, out cached) && !cached.IsExpired)
                return cached.Token;

            // Layer 2: DB (survives restarts, shared across instances)
            var dbToken = await LoadFromDbAsync(ct);
            if (dbToken is not null && !dbToken.IsExpired)
            {
                _cache[StoreName] = dbToken;
                return dbToken.Token;
            }

            // Layer 3: Fetch from Store API
            var response = await FetchTokenAsync(ct);
            var newToken = new StoreToken
            {
                StoreName = StoreName,
                Token = response.Token,
                IssuedAt = response.IssuedAt,
                ExpiresAt = response.ExpiresAt,
                UpdatedAt = DateTime.UtcNow
            };

            // Persist to DB + update static cache
            await SaveToDbAsync(newToken, ct);
            _cache[StoreName] = newToken;
            return newToken.Token;
        }
        finally { _lock.Release(); }
    }

    protected abstract Task<TokenResponse> FetchTokenAsync(CancellationToken ct);
}
```

**Registration:** Singleton (preserves static cache + semaphore lifetime)

### Store API Client Pattern (Polymorphism)

```csharp
// Base class — Template Method + Open/Closed Principle
public abstract class StoreApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenProvider _tokenProvider;

    protected abstract string StoreName { get; }

    public async Task<IReadOnlyList<PriceResult>> SearchAsync(string term)
    {
        var token = await _tokenProvider.GetTokenAsync();
        // Each subclass implements its own request/response mapping
        return await ExecuteSearchAsync(term, token);
    }

    protected abstract Task<IReadOnlyList<PriceResult>> ExecuteSearchAsync(string term, string token);
}

// Adding a new store = add one subclass, no existing code changes
public class PakNSaveClient : StoreApiClient { ... }
public class NewWorldClient : StoreApiClient { ... }
public class WoolworthsClient : StoreApiClient { ... }
```

### External API Resilience Pattern

```csharp
// Per-store named HttpClient with Polly policies
services.AddHttpClient("PakNSave")
    .AddPolicyHandler(Policy.RetryAsync(2))
    .AddPolicyHandler(Policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
    .AddPolicyHandler(Policy.TimeoutAsync(TimeSpan.FromSeconds(2.5)));

// Parallel fan-out — failed stores return empty, never throw
var results = await Task.WhenAll(
    _storeClients.Select(c => c.SearchAsync(term)));
return results.SelectMany(r => r).OrderBy(r => r.Price).ToList();
```

### Error Handling Pattern

```csharp
// Global exception handler middleware — controllers never try-catch
// Business exceptions → ProblemDetails
// External API failures → graceful degradation (empty results + log)
// Validation errors → 400 with field-level detail

// Never: res.status(500).send("Something went wrong")
// Always: structured ProblemDetails with correlation ID
```

### Cache Read/Write Pattern (Dapper)

```csharp
// Read: DB cache → miss → real-time fetch
// Write: background async, non-blocking
// Upsert via ON CONFLICT
await connection.ExecuteAsync(@"
    INSERT INTO prices (product_id, store_id, price, updated_at)
    VALUES (@ProductId, @StoreId, @Price, @UpdatedAt)
    ON CONFLICT (product_id, store_id)
    DO UPDATE SET price = @Price, updated_at = @UpdatedAt", record);
```

### DI Registration Pattern

```csharp
// Program.cs — single source of truth for all registrations
// Singleton: TokenProviders (static cache)
// Scoped: Services, Repositories (per-request lifetime)
// Transient: none (avoid for DB connections)

services.AddSingleton<PakNSaveTokenProvider>();
services.AddSingleton<NewWorldTokenProvider>();
services.AddSingleton<WoolworthsTokenProvider>();

services.AddScoped<IPriceComparisonService, PriceComparisonService>();
services.AddScoped<IBucketService, BucketService>();
services.AddScoped<IStoreService, StoreService>();
services.AddScoped<IFavoritesService, FavoritesService>();
```

### Controller Pattern

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    // Constructor injection only
    // Each action returns ActionResult<T>
    // [Authorize] on protected endpoints
    // Input validation via DataAnnotations on DTOs
}
```

### API Route Map

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/v1/products` | No | List all products |
| GET | `/api/v1/products/compare?q=` | No | Price comparison |
| POST | `/api/v1/products/compare-bucket` | No | Bucket total comparison |
| GET | `/api/v1/products/favorites` | Yes | User favorites |
| POST | `/api/v1/products/favorites` | Yes | Toggle favorite |
| DELETE | `/api/v1/products/{id}` | Yes | Delete product (owner only) |
| GET | `/api/v1/stores` | No | All stores |
| GET | `/api/v1/stores/nearby?lat=&lng=` | No | Nearby stores |

## Project Structure & Boundaries

### Solution Layout

```
KiwiCart/
├── client/                             # React 18 SPA (unchanged)
│   ├── apis/products.ts
│   ├── components/
│   │   ├── ui/PriceDisplay.tsx
│   │   ├── App.tsx
│   │   ├── ProductComparison.tsx
│   │   ├── BucketDrawer.tsx
│   │   ├── BucketComparisonDisplay.tsx
│   │   ├── StoreMap.tsx
│   │   ├── MyKitchen.tsx
│   │   └── DeveloperProfile.tsx
│   ├── contexts/BucketContext.tsx
│   ├── styles/index.css
│   ├── index.tsx
│   └── routes.tsx
│
├── server-dotnet/
│   ├── KiwiCart.sln
│   ├── src/
│   │   ├── KiwiCart.Api/               # HTTP layer
│   │   │   ├── Controllers/
│   │   │   │   ├── ProductsController.cs
│   │   │   │   └── StoresController.cs
│   │   │   ├── Middleware/GlobalExceptionHandler.cs
│   │   │   ├── Program.cs
│   │   │   ├── appsettings.json
│   │   │   └── appsettings.Development.json
│   │   │
│   │   ├── KiwiCart.Core/              # Domain (no dependencies)
│   │   │   ├── Entities/
│   │   │   │   ├── Store.cs, Product.cs, Price.cs
│   │   │   │   ├── Favorite.cs, StoreToken.cs
│   │   │   │   ├── Bucket.cs, BucketItem.cs
│   │   │   ├── DTOs/
│   │   │   │   ├── PriceResult.cs, BucketComparisonResult.cs
│   │   │   │   ├── BucketRequest.cs, TokenResponse.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── IPriceComparisonService.cs, IBucketService.cs
│   │   │   │   ├── IStoreService.cs, IFavoritesService.cs
│   │   │   │   ├── ITokenProvider.cs
│   │   │   │   ├── IPriceRepository.cs, IStoreRepository.cs
│   │   │   └── Exceptions/
│   │   │       ├── NotFoundException.cs, StoreApiException.cs
│   │   │
│   │   └── KiwiCart.Infrastructure/    # External concerns
│   │       ├── Data/
│   │       │   ├── AppDbContext.cs
│   │       │   └── Migrations/
│   │       ├── Repositories/
│   │       │   ├── PriceRepository.cs (Dapper)
│   │       │   ├── StoreRepository.cs (EF Core)
│   │       │   └── FavoritesRepository.cs (EF Core)
│   │       ├── StoreClients/
│   │       │   ├── StoreApiClient.cs (abstract)
│   │       │   ├── PakNSaveClient.cs
│   │       │   ├── NewWorldClient.cs
│   │       │   └── WoolworthsClient.cs
│   │       ├── TokenProviders/
│   │       │   ├── CachedTokenProvider.cs (abstract)
│   │       │   ├── PakNSaveTokenProvider.cs
│   │       │   ├── NewWorldTokenProvider.cs
│   │       │   └── WoolworthsTokenProvider.cs
│   │       └── Services/
│   │           ├── PriceComparisonService.cs
│   │           ├── BucketService.cs
│   │           ├── StoreService.cs
│   │           ├── FavoritesService.cs
│   │           └── PriceCalculator.cs
│   │
│   └── tests/KiwiCart.Tests/
│       ├── Controllers/
│       ├── Services/
│       └── Infrastructure/
│
├── .github/workflows/
│   ├── ci.yml                          # PR: build + test
│   └── deploy.yml                      # merge to main: deploy Azure
├── docker-compose.yml                  # Local PostgreSQL
├── package.json                        # Frontend scripts
├── vite.config.js
└── index.html
```

### Dependency Rules

```
Api → Core, Infrastructure
Infrastructure → Core
Core → (nothing — innermost layer)
Tests → All
```

### Layer Responsibilities

| Layer | Owns | Does NOT own |
|-------|------|-------------|
| Api | Request/response mapping, DI, middleware, auth | Business logic, DB access |
| Core | Entities, interfaces, DTOs, domain exceptions | Implementation details |
| Infrastructure | EF Context, Dapper queries, HTTP clients, token mgmt | HTTP routing, auth config |
| Tests | Assertions, mocks, test fixtures | Production code |

## Infrastructure & Deployment

### Local Development

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16
    ports: ["5432:5432"]
    environment:
      POSTGRES_DB: kiwicart
      POSTGRES_USER: dev
      POSTGRES_PASSWORD: dev
```

### CI/CD Pipeline (GitHub Actions)

**ci.yml (on PR):**
1. Checkout → Setup .NET 8 → Restore → Build → Test
2. Checkout → Setup Node 20 → npm install → npm run lint → npm run build

**deploy.yml (on merge to main):**
1. Build .NET → Publish → Deploy to Azure App Service
2. Build Vite → Deploy static to Azure (or same App Service)

### Azure App Service Configuration

| Setting | Value |
|---------|-------|
| Plan | B1 (Australia East) |
| Runtime | .NET 8 |
| Always On | Yes |
| Health Check | `/health` |
| Custom Domain | TBD |
| TLS | Managed certificate |

### Environment Variables

| Variable | Purpose | Where |
|----------|---------|-------|
| `ConnectionStrings__Default` | PostgreSQL connection | Azure App Settings |
| `Auth0__Domain` | Auth0 tenant | Azure App Settings |
| `Auth0__Audience` | Auth0 API identifier | Azure App Settings |
| `GoogleMaps__ApiKey` | Maps API key | Azure App Settings |
| `VITE_GOOGLE_MAPS_API_KEY` | Frontend maps key | Build-time env |
| `VITE_AUTH0_DOMAIN` | Frontend auth | Build-time env |
| `VITE_AUTH0_CLIENT_ID` | Frontend auth | Build-time env |

---

## Architecture Completion

**Document Status:** Final
**Created:** 2026-06-07
**Stakeholder:** Phoebe

### Summary of Key Decisions

1. Backend migration: Express.js → ASP.NET Core 8 (Controllers)
2. ORM: EF Core (CRUD) + Dapper (cache hot path)
3. Token management: Static singleton + DB persistence + Store API (3-layer)
4. Store API clients: Abstract base class with Polly resilience
5. Deployment: Azure App Service (B1, Australia East)
6. CI/CD: GitHub Actions
7. Terminology: Supermarket→Store, Basket→Bucket

### Next Steps

1. **Create Epics & Stories** (`bmad-create-epics-and-stories`) — break this architecture into implementable work
2. **Sprint Planning** (`bmad-sprint-planning`) — sequence the migration
3. **Implementation** — start with infrastructure setup (solution scaffold + Docker + CI)
