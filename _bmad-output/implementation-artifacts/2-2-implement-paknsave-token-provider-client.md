---
baseline_commit: 7749d55fa1d39493f0586787115121d1d38ce467
---

# Story 2.2: Implement PakNSave Token Provider & Client

Status: done

## Story

As a developer,
I want a PakNSave-specific token provider and API client,
So that I can fetch real-time prices from Pak'nSave.

## Acceptance Criteria

1. Given the PakNSaveTokenProvider is registered, when `GetTokenAsync()` is called, then a valid token is obtained via the CachedTokenProvider 3-layer system
2. Given a valid token, when `SearchAsync("Milk")` is called, then a search request is made to the Pak'nSave API and results mapped to `PriceResult` DTOs with storeName = "Pak'nSave"
3. Given the HttpClient registration, then Polly policies are applied: retry 2x, circuit breaker after 5 failures (30s break), 2.5s timeout
4. Given a 401 response, the token should be invalidated and retried once with a fresh token

## Tasks / Subtasks

- [x] Task 1: Implement PakNSaveTokenProvider (AC: #1)
  - [x] Extend CachedTokenProvider
  - [x] Implement FetchTokenAsync: POST to `/api/user/get-current-user`
  - [x] Extract `access_token` from response
- [x] Task 2: Implement StoreApiClient abstract base (AC: #2)
  - [x] Create abstract class with SearchAsync template method
  - [x] Handle 401 retry with token refresh
- [x] Task 3: Implement PakNSaveClient (AC: #2, #4)
  - [x] POST to `v1/edge/search/paginated/products`
  - [x] Map response products to PriceResult DTOs
  - [x] Price in response is cents → divide by 100
- [x] Task 4: Register HttpClient with Polly + DI (AC: #3)
  - [x] Named HttpClient "PakNSave" with base address
  - [x] Polly retry 2x, circuit breaker 5 failures/30s, timeout 2.5s
  - [x] Register PakNSaveTokenProvider as singleton
  - [x] Register PakNSaveClient as singleton
- [x] Task 5: Unit tests (AC: #1-4)
  - [x] Test client maps search results to PriceResult
  - [x] Test returns empty on failure
  - [x] Test 401 triggers token refresh and retry

## Dev Notes

### API Details (from existing Node.js implementation)

**Token endpoint:**
- POST `https://www.paknsave.co.nz/api/user/get-current-user`
- Body: `{}`
- Headers: User-Agent (browser-like), Content-Type: application/json
- Response: `{ "access_token": "..." }`

**Search endpoint:**
- POST `https://api-prod.paknsave.co.nz/v1/edge/search/paginated/products`
- Headers: Authorization: Bearer {token}, Content-Type: application/json, User-Agent
- Body: `{ "algoliaQuery": { "query": "..." }, "storeId": "...", "hitsPerPage": 50, "page": 0, "sortOrder": "NI_POPULARITY_ASC" }`
- Response: `{ "products": [{ "name": "...", "productId": "...", "singlePrice": { "price": 599 }, "images": { "primaryImages": { "400px": "..." } } }] }`
- Price is in CENTS (divide by 100)

**Config:**
- Domain: `paknsave.co.nz`
- API Domain: `api-prod.paknsave.co.nz`
- Default StoreId: `65defcf2-bc15-490e-a84f-1f13b769cd22`

### Architecture Compliance

- Token provider: `Infrastructure/TokenProviders/PakNSaveTokenProvider.cs` — Singleton
- Client: `Infrastructure/StoreClients/PakNSaveClient.cs` — Singleton
- Base class: `Infrastructure/StoreClients/StoreApiClient.cs`
- Polly registration in Program.cs via `AddHttpClient<T>().AddPolicyHandler(...)`

### References

- [Source: server/services/foodstuffs-base.ts]
- [Source: server/services/paknsave.ts]
- [Source: _bmad-output/planning-artifacts/architecture.md#Store API Client Pattern]
- [Source: _bmad-output/planning-artifacts/architecture.md#External API Resilience Pattern]

### Review Findings

- [x] [Review][Patch] P1: PakNSaveAuth HttpClient has no Polly policies — token fetch can block indefinitely [Program.cs]
- [x] [Review][Patch] P3: HttpResponseMessage not disposed in FetchTokenAsync and ExecuteSearchAsync [PakNSaveTokenProvider.cs + PakNSaveClient.cs]
- [x] [Review][Patch] P5: GetProperty("products") throws on schema change — use TryGetProperty [PakNSaveClient.cs]
- [x] [Review][Defer] W1: Polly timeout ordering — deferred, product decision
- [x] [Review][Defer] W2: Hardcoded StoreId — deferred, Epic 5
- [x] [Review][Defer] W3: Shared Polly circuit breaker state — deferred, single client currently

## Dev Agent Record

### Agent Model Used

Kiro CLI (Auto)

### Completion Notes List

- PakNSaveTokenProvider: POSTs to /api/user/get-current-user, extracts access_token, assumes 1h expiry (API doesn't return explicit expiry)
- StoreApiClient base class: Template Method pattern with 401 retry logic
- PakNSaveClient: Maps cents to dollars, handles missing singlePrice gracefully
- Polly policies: retry 2x transient, circuit breaker 5/30s, timeout 2.5s
- All 10 tests pass (no regressions)

### File List

- server-dotnet/src/KiwiCart.Infrastructure/TokenProviders/PakNSaveTokenProvider.cs (NEW)
- server-dotnet/src/KiwiCart.Infrastructure/StoreClients/StoreApiClient.cs (NEW)
- server-dotnet/src/KiwiCart.Infrastructure/StoreClients/PakNSaveClient.cs (NEW)
- server-dotnet/src/KiwiCart.Api/Program.cs (UPDATED)
- server-dotnet/tests/KiwiCart.Tests/Infrastructure/PakNSaveClientTests.cs (NEW)
