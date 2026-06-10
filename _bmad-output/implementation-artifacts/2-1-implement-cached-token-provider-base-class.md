---
baseline_commit: 7749d55fa1d39493f0586787115121d1d38ce467
---

# Story 2.1: Implement CachedTokenProvider Base Class

Status: done

## Story

As a developer,
I want a 3-layer token caching system (static → DB → Store API),
So that concurrent requests reuse tokens and minimize Store API calls.

## Acceptance Criteria

1. Given a token request, when a valid token exists in static memory, then it is returned immediately without DB or API call
2. Given static cache is empty, when DB has a valid (non-expired) token, then it is loaded from DB and placed in static cache
3. Given both static and DB are empty/expired, when a new token is fetched from Store API, then it is persisted to DB with `expires_at` from the Store response
4. Given concurrent token requests for the same store, then `SemaphoreSlim` prevents duplicate fetches (only one thread fetches, others wait and reuse)
5. Given a token is near expiry (within 5 minutes), it should be treated as expired and refreshed

## Tasks / Subtasks

- [x] Task 1: Update `ITokenProvider` interface (AC: #1-3)
  - [x] Add `Task<string> GetTokenAsync(CancellationToken ct = default)` method
  - [x] Add `string StoreName { get; }` property
- [x] Task 2: Update `StoreToken` entity (AC: #3)
  - [x] Add `IssuedAt` property
  - [x] Add `UpdatedAt` property
  - [x] Add `IsExpired` computed property
  - [x] Add `IsNearExpiry` computed property (5-min buffer)
- [x] Task 3: Update `TokenResponse` DTO (AC: #3)
  - [x] Add `IssuedAt` property
- [x] Task 4: Create EF Core migration for `store_tokens` schema update (AC: #3)
  - [x] Add `issued_at` and `updated_at` columns
- [x] Task 5: Implement `CachedTokenProvider` abstract class (AC: #1-5)
  - [x] Static `ConcurrentDictionary<string, StoreToken>` for Layer 1
  - [x] `SemaphoreSlim` per store for concurrency control
  - [x] `GetTokenAsync` orchestrating 3 layers with double-check pattern
  - [x] `LoadFromDbAsync` using AppDbContext
  - [x] `SaveToDbAsync` with upsert semantics
  - [x] Abstract `FetchTokenAsync` for subclass implementation
- [x] Task 6: Write unit tests (AC: #1-5)
  - [x] Test: returns cached token when static cache valid
  - [x] Test: loads from DB when static cache empty
  - [x] Test: fetches from API when both empty/expired
  - [x] Test: persists new token to DB after API fetch
  - [x] Test: SemaphoreSlim prevents concurrent fetches
  - [x] Test: near-expiry tokens treated as expired

## Dev Notes

### Architecture Compliance

- **Location:** `server-dotnet/src/KiwiCart.Infrastructure/TokenProviders/CachedTokenProvider.cs`
- **Registration:** Singleton (preserves static cache + semaphore lifetime)
- **Layer:** Infrastructure (implements Core interface)
- **Dependencies:** `AppDbContext` (injected), `ILogger<T>` (injected)

### Existing Code State

The following files exist and need UPDATE:

| File | Current State | Change Needed |
|------|--------------|---------------|
| `Core/Interfaces/ITokenProvider.cs` | Empty interface `{}` | Add `GetTokenAsync` + `StoreName` |
| `Core/Entities/StoreToken.cs` | Has `Id`, `StoreBrand`, `Token`, `ExpiresAt` | Add `IssuedAt`, `UpdatedAt`, computed props |
| `Core/DTOs/TokenResponse.cs` | Has `Token`, `ExpiresAt` | Add `IssuedAt` |
| `Infrastructure/Data/AppDbContext.cs` | Maps `store_tokens` with `id`, `store_brand`, `token`, `expires_at` | Add `issued_at`, `updated_at` column mappings |
| `Infrastructure/TokenProviders/.gitkeep` | Empty placeholder | Replace with `CachedTokenProvider.cs` |

### Critical Implementation Details

**StoreToken entity uses `StoreBrand` (not `StoreName`)** — the DB column is `store_brand`. Architecture doc says `StoreName` but existing code uses `StoreBrand`. Follow existing code pattern: use `StoreBrand` in entity, but `StoreName` as the abstract property name in `CachedTokenProvider` which maps to the brand key.

**DB schema:** `store_tokens` table has unique constraint on `store_brand`. Upsert pattern:
```sql
-- Use EF Core for token persistence (not Dapper) since this is CRUD, not hot-path
-- Dapper reserved for price cache reads (Story 3.1)
```

**Concurrency pattern:** Use per-store `SemaphoreSlim` via `ConcurrentDictionary<string, SemaphoreSlim>` — NOT a single global lock. Each store fetches independently.

**Double-check after lock acquisition** — another thread may have populated cache while waiting.

**Token lifetime:** Use `ExpiresAt` from the Store API response directly. Do NOT calculate expiry ourselves. Add 5-minute buffer via `IsNearExpiry`.

### Pattern from Architecture Doc

```csharp
public abstract class CachedTokenProvider : ITokenProvider
{
    private static readonly ConcurrentDictionary<string, StoreToken> _cache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    protected abstract string StoreName { get; }
    protected abstract Task<TokenResponse> FetchTokenAsync(CancellationToken ct);

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        // Layer 1: Static memory
        if (_cache.TryGetValue(StoreName, out var cached) && !cached.IsNearExpiry)
            return cached.Token;

        var semaphore = _locks.GetOrAdd(StoreName, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            // Double-check after lock
            if (_cache.TryGetValue(StoreName, out cached) && !cached.IsNearExpiry)
                return cached.Token;

            // Layer 2: DB
            var dbToken = await LoadFromDbAsync(ct);
            if (dbToken is not null && !dbToken.IsNearExpiry)
            {
                _cache[StoreName] = dbToken;
                return dbToken.Token;
            }

            // Layer 3: Store API
            var response = await FetchTokenAsync(ct);
            var newToken = new StoreToken
            {
                StoreBrand = StoreName,
                Token = response.Token,
                IssuedAt = response.IssuedAt,
                ExpiresAt = response.ExpiresAt,
                UpdatedAt = DateTime.UtcNow
            };
            await SaveToDbAsync(newToken, ct);
            _cache[StoreName] = newToken;
            return newToken.Token;
        }
        finally { semaphore.Release(); }
    }
}
```

### Testing Strategy

- Use xUnit (already in project)
- Mock `AppDbContext` using in-memory provider or create a test subclass of `CachedTokenProvider` that overrides `FetchTokenAsync`
- Test concurrency with `Task.WhenAll` and verify `FetchTokenAsync` called exactly once
- Test file: `tests/KiwiCart.Tests/Infrastructure/CachedTokenProviderTests.cs`

### Project Structure Notes

- Namespace: `KiwiCart.Infrastructure.TokenProviders`
- Interface namespace: `KiwiCart.Core.Interfaces`
- Entity namespace: `KiwiCart.Core.Entities`
- Follow existing patterns: primary constructor for DbContext, file-scoped namespaces

### References

- [Source: _bmad-output/planning-artifacts/architecture.md#Token Management Pattern]
- [Source: _bmad-output/planning-artifacts/architecture.md#DI Registration Pattern]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.1]

### Review Findings

- [x] [Review][Decision→Patch] D1: PakNSaveTokenProvider hardcodes 1h expiry — parse JWT `exp` claim, fallback 1h [PakNSaveTokenProvider.cs]
- [x] [Review][Decision→Patch] D2: ClearCache() nukes all stores — add InvalidateToken(storeName) for per-store invalidation [CachedTokenProvider.cs]
- [x] [Review][Patch] P2: 401 retry only clears static cache, not DB — InvalidateToken must also clear DB [CachedTokenProvider.cs + StoreApiClient.cs]
- [x] [Review][Patch] P4: SaveToDbAsync lacks unique constraint conflict handling [CachedTokenProvider.cs:SaveToDbAsync]
- [x] [Review][Defer] W1: Polly timeout ordering (per-attempt vs total) — deferred, product decision needed
- [x] [Review][Defer] W2: Hardcoded StoreId — deferred, location-aware feature (Epic 5)
- [x] [Review][Defer] W3: Shared Polly circuit breaker state — deferred, only one store client currently

## Dev Agent Record

### Agent Model Used

Kiro CLI (Auto)

### Debug Log References

### Completion Notes List

- All 5 ACs satisfied with 6 unit tests covering each scenario
- Used `ConcurrentDictionary<string, SemaphoreSlim>` for per-store locking (not global)
- TokenResponse converted to immutable record for value semantics
- `InternalsVisibleTo` added to Infrastructure project for test access to `ClearCache()`
- EF InMemory provider used for test isolation

### File List

- server-dotnet/src/KiwiCart.Core/Interfaces/ITokenProvider.cs (UPDATED)
- server-dotnet/src/KiwiCart.Core/Entities/StoreToken.cs (UPDATED)
- server-dotnet/src/KiwiCart.Core/DTOs/TokenResponse.cs (UPDATED)
- server-dotnet/src/KiwiCart.Infrastructure/Data/AppDbContext.cs (UPDATED)
- server-dotnet/src/KiwiCart.Infrastructure/KiwiCart.Infrastructure.csproj (UPDATED)
- server-dotnet/src/KiwiCart.Infrastructure/TokenProviders/CachedTokenProvider.cs (NEW)
- server-dotnet/src/KiwiCart.Infrastructure/Migrations/AddStoreTokenTimestamps (NEW)
- server-dotnet/tests/KiwiCart.Tests/KiwiCart.Tests.csproj (UPDATED)
- server-dotnet/tests/KiwiCart.Tests/Infrastructure/CachedTokenProviderTests.cs (NEW)
