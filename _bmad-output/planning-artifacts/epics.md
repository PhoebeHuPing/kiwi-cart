---
stepsCompleted: [1, 2]
inputDocuments: ['_bmad-output/planning-artifacts/prd-kiwicart/prd.md', '_bmad-output/planning-artifacts/architecture.md']
---

# KiwiCart - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for KiwiCart, decomposing the PRD and Architecture requirements into implementable stories for the .NET backend migration + frontend adaptation.

## Requirements Inventory

### Functional Requirements

- FR-1.1: Users can search products by name with debounced input
- FR-1.2: Search results display prices from all three stores side-by-side, sorted cheapest-first
- FR-1.3: Each result shows: product name, image, price, store brand, logo, unit price
- FR-1.4: Unit price calculated automatically (per L, per kg, per 100g) based on product name parsing
- FR-1.5: Hybrid cache with 24h expiry; stale/missing data triggers real-time API fetch
- FR-2.1: Users can add multiple items with quantities to a virtual Bucket
- FR-2.2: System calculates total Bucket cost per store
- FR-2.3: Missing items per store clearly identified
- FR-3.1: Google Maps integration showing nearby stores
- FR-3.3: Store distance calculated from user's browser geolocation
- FR-4.1: Auth0-based authentication (social login + email)
- FR-4.2: Authenticated users can save/remove favorite products

### Non-Functional Requirements

- NFR-1.1: Search results returned within 3s (cache hit <500ms)
- NFR-1.2: Bucket comparison completes within 5s for up to 10 items
- NFR-2.1: 99% uptime during NZ business hours (8am-10pm NZST)
- NFR-2.2: Support 500 concurrent users
- NFR-3.1: Prices no more than 24 hours stale
- NFR-4.1: WCAG AA compliant
- NFR-5.1: Auth0 JWT-based authentication for protected endpoints
- NFR-5.3: All API communication over HTTPS

### Additional Requirements (Architecture)

- AR-1: ASP.NET Core 8 solution scaffold (Api, Core, Infrastructure, Tests projects)
- AR-2: Docker Compose for local PostgreSQL
- AR-3: EF Core migrations for database schema (stores, products, prices, favorites, store_tokens)
- AR-4: Token management system (static cache → DB → Store API, 3-layer)
- AR-5: Polly resilience policies per store client (circuit breaker + retry + timeout)
- AR-6: GitHub Actions CI/CD pipeline (build, test, deploy)
- AR-7: Azure App Service deployment configuration

### FR Coverage Map

| Epic | Covers |
|------|--------|
| Epic 1: Infrastructure & Foundation | AR-1, AR-2, AR-3, AR-6, AR-7 |
| Epic 2: Store Integration & Token Mgmt | AR-4, AR-5, FR-1.5 |
| Epic 3: Price Comparison Core | FR-1.1 – FR-1.5, NFR-1.1 |
| Epic 4: Bucket Comparison | FR-2.1 – FR-2.3, NFR-1.2 |
| Epic 5: Store Locator | FR-3.1, FR-3.3 |
| Epic 6: Auth & Favorites | FR-4.1, FR-4.2, NFR-5.1 |
| Epic 7: Production Hardening | NFR-2.1, NFR-2.2, NFR-4.1, NFR-5.3 |

## Epic List

1. Epic 1: Infrastructure & Foundation
2. Epic 2: Store Integration & Token Management
3. Epic 3: Price Comparison Core
4. Epic 4: Bucket Comparison
5. Epic 5: Store Locator
6. Epic 6: Authentication & Favorites
7. Epic 7: Production Hardening & Launch

---

## Epic 1: Infrastructure & Foundation

**Goal:** Establish the .NET solution scaffold, database, CI/CD pipeline, and deployment target so all subsequent epics have a working foundation.

### Story 1.1: Create .NET Solution Scaffold

As a developer,
I want a properly structured ASP.NET Core 8 solution,
So that all subsequent code has a clean architecture to live in.

**Acceptance Criteria:**

**Given** a new `server-dotnet/` directory
**When** the solution is created
**Then** it contains 4 projects: KiwiCart.Api, KiwiCart.Core, KiwiCart.Infrastructure, KiwiCart.Tests
**And** project references follow dependency rules (Api→Core+Infra, Infra→Core, Core→nothing)
**And** `Program.cs` has basic DI, Swagger, CORS, and health check endpoint configured
**And** `dotnet build` succeeds with zero warnings

### Story 1.2: Configure Local PostgreSQL with Docker Compose

As a developer,
I want a Docker Compose setup for local PostgreSQL,
So that I can develop without external database dependencies.

**Acceptance Criteria:**

**Given** Docker is installed
**When** I run `docker-compose up`
**Then** a PostgreSQL 16 instance starts on port 5432
**And** database `kiwicart` is created with user `dev`/`dev`
**And** the .NET app can connect using `appsettings.Development.json`

### Story 1.3: Create EF Core Database Schema

As a developer,
I want EF Core migrations that create the full database schema,
So that the data layer is ready for all features.

**Acceptance Criteria:**

**Given** the AppDbContext is configured
**When** I run `dotnet ef database update`
**Then** tables are created: `products`, `stores`, `prices`, `favorites`, `store_tokens`
**And** `prices` has unique constraint on (product_id, store_id)
**And** `store_tokens` has unique constraint on store_name
**And** `favorites` has unique constraint on (user_id, product_name)

### Story 1.4: Setup GitHub Actions CI Pipeline

As a developer,
I want automated build and test on every PR,
So that broken code is caught before merge.

**Acceptance Criteria:**

**Given** a PR is opened against `main`
**When** GitHub Actions runs
**Then** .NET solution builds successfully
**And** all unit tests pass
**And** frontend `npm run build` succeeds
**And** pipeline result is reported on the PR

### Story 1.5: Configure Azure App Service Deployment

As a developer,
I want automated deployment to Azure on merge to main,
So that production stays up-to-date.

**Acceptance Criteria:**

**Given** a PR is merged to `main`
**When** the deploy workflow runs
**Then** the .NET API is published to Azure App Service (B1, Australia East)
**And** health check endpoint `/health` returns 200
**And** environment variables are loaded from Azure App Settings

---

## Epic 2: Store Integration & Token Management

**Goal:** Implement the token management system and resilient HTTP clients for all three stores, enabling real-time price data fetching.

### Story 2.1: Implement CachedTokenProvider Base Class

As a developer,
I want a 3-layer token caching system (static → DB → Store API),
So that concurrent requests reuse tokens and minimize Store API calls.

**Acceptance Criteria:**

**Given** a token request comes in
**When** a valid token exists in static memory
**Then** it is returned immediately without DB or API call
**And** when static cache is empty but DB has valid token, it's loaded from DB
**And** when both are empty/expired, a new token is fetched from Store API
**And** new tokens are persisted to DB with issued_at and expires_at from the Store
**And** SemaphoreSlim prevents concurrent fetches for the same store

### Story 2.2: Implement PakNSave Token Provider & Client

As a developer,
I want a PakNSave-specific token provider and API client,
So that I can fetch real-time prices from Pak'nSave.

**Acceptance Criteria:**

**Given** the PakNSaveTokenProvider is registered
**When** `SearchAsync("Milk")` is called
**Then** a valid token is obtained (fetched or cached)
**And** the search request is made to the Pak'nSave API
**And** results are mapped to `PriceResult` DTOs with storeName = "Pak'nSave"
**And** Polly policies are applied (retry 2x, circuit breaker after 5 failures, 2.5s timeout)

### Story 2.3: Implement NewWorld Token Provider & Client

As a developer,
I want a NewWorld-specific token provider and API client,
So that I can fetch real-time prices from New World.

**Acceptance Criteria:**

Same as Story 2.2 but for New World API and storeName = "New World"

### Story 2.4: Implement Woolworths Token Provider & Client

As a developer,
I want a Woolworths-specific token provider and API client,
So that I can fetch real-time prices from Woolworths.

**Acceptance Criteria:**

Same as Story 2.2 but for Woolworths API and storeName = "Woolworths"

### Story 2.5: Implement Graceful Degradation for Store Failures

As a developer,
I want failed store API calls to return empty results instead of crashing,
So that one store being down doesn't break the entire comparison.

**Acceptance Criteria:**

**Given** a store API is unreachable or circuit is open
**When** a price comparison is requested
**Then** results from available stores are still returned
**And** the failed store is excluded from results (not shown as error to user)
**And** the failure is logged with structured logging (Serilog)

---

## Epic 3: Price Comparison Core

**Goal:** Implement the core search and price comparison feature with hybrid caching.

### Story 3.1: Implement Price Cache Repository (Dapper)

As a developer,
I want a high-performance cache repository using Dapper,
So that cached prices are read/written efficiently.

**Acceptance Criteria:**

**Given** a search term
**When** `GetCachedPricesAsync(term)` is called
**Then** it returns prices joined with product and store data, filtered by 24h freshness
**And** `UpsertPriceAsync` uses INSERT ON CONFLICT UPDATE
**And** queries are parameterized (no SQL injection)

### Story 3.2: Implement PriceComparisonService

As a developer,
I want a service that orchestrates cache lookup and real-time fetching,
So that users get fast results with fresh data.

**Acceptance Criteria:**

**Given** a search term
**When** cache has fresh results → return immediately (<500ms)
**When** cache is stale/empty → fetch from all stores in parallel via `Task.WhenAll`
**Then** combined results are sorted by price ascending
**And** unit price is calculated for each result
**And** fresh results are upserted to cache in background (non-blocking)

### Story 3.3: Implement PriceCalculator Service

As a developer,
I want automatic unit price calculation from product names,
So that users can compare value across different package sizes.

**Acceptance Criteria:**

**Given** product name "Milk 2L" with price $5.00
**Then** unit price = "$2.50/L"
**Given** product name "Butter 500g" with price $6.00
**Then** unit price = "$1.20/100g"
**And** unrecognized formats return empty string (no crash)

### Story 3.4: Implement ProductsController Compare Endpoint

As a developer,
I want `GET /api/v1/products/compare?q={term}`,
So that the frontend can fetch price comparison data.

**Acceptance Criteria:**

**Given** `GET /api/v1/products/compare?q=Milk`
**When** the endpoint is called
**Then** it returns JSON array of PriceResult objects
**And** response includes output cache header (5 min for identical queries)
**And** rate limiting is applied (fixed window per IP)
**And** empty query returns 400 ProblemDetails

### Story 3.5: Update Frontend to Use New API

As a developer,
I want the React frontend to point to the new .NET API,
So that the UI works with the new backend.

**Acceptance Criteria:**

**Given** the Vite dev proxy is configured to new .NET port
**When** user searches for a product
**Then** results display correctly with store names, prices, unit prices
**And** loading states and error handling work as before

---

## Epic 4: Bucket Comparison

**Goal:** Implement multi-item bucket comparison across stores.

### Story 4.1: Implement BucketService

As a developer,
I want a service that calculates total bucket cost per store,
So that users can find the cheapest store for their whole shop.

**Acceptance Criteria:**

**Given** a Bucket with items [{name: "Milk", qty: 2}, {name: "Bread", qty: 1}]
**When** `CompareAsync(bucket)` is called
**Then** prices are fetched for each item from all stores (parallel with MaxDegreeOfParallelism)
**And** results show total per store, items found count, missing items list
**And** results sorted by items_found DESC then total_price ASC

### Story 4.2: Implement Bucket Compare Endpoint

As a developer,
I want `POST /api/v1/products/compare-bucket`,
So that the frontend can submit bucket comparisons.

**Acceptance Criteria:**

**Given** POST with body `{ "items": [{ "name": "Milk", "quantity": 2 }] }`
**When** the endpoint is called
**Then** it returns BucketComparisonResult array
**And** empty/invalid items returns 400 ProblemDetails
**And** rate limiting is stricter than search (heavier operation)

---

## Epic 5: Store Locator

**Goal:** Implement location-based store discovery.

### Story 5.1: Implement StoreService with Haversine

As a developer,
I want nearby store lookup using Haversine distance calculation,
So that users can find the closest stores.

**Acceptance Criteria:**

**Given** user coordinates and radius (default 5km)
**When** `GetNearbyAsync(lat, lng, radius)` is called
**Then** stores within radius are returned, sorted by distance
**And** invalid coordinates return empty list (not error)

### Story 5.2: Implement StoresController

As a developer,
I want `GET /api/v1/stores` and `GET /api/v1/stores/nearby`,
So that the frontend can display store locations.

**Acceptance Criteria:**

**Given** `GET /api/v1/stores/nearby?lat=-36.88&lng=174.63`
**Then** returns stores within 5km with distance field
**And** `GET /api/v1/stores` returns all stores
**And** missing/invalid lat/lng returns 400 ProblemDetails

---

## Epic 6: Authentication & Favorites

**Goal:** Implement Auth0 authentication and user favorites.

### Story 6.1: Configure Auth0 JWT Authentication

As a developer,
I want JWT Bearer authentication middleware,
So that protected endpoints validate Auth0 tokens.

**Acceptance Criteria:**

**Given** Auth0 domain and audience in config
**When** a request has valid Bearer token
**Then** `User.Claims` contains the Auth0 sub claim
**And** invalid/missing token on [Authorize] endpoints returns 401

### Story 6.2: Implement Favorites Endpoints

As a developer,
I want favorites CRUD via `GET/POST /api/v1/products/favorites`,
So that users can manage their saved products.

**Acceptance Criteria:**

**Given** authenticated user
**When** GET favorites → returns user's favorite product names
**When** POST `{ "name": "Milk" }` → toggles favorite (add if missing, remove if exists)
**And** unauthenticated requests return 401
**And** favorites are scoped to user_id from JWT sub claim

### Story 6.3: Fix Delete Endpoint Authorization

As a developer,
I want DELETE `/api/v1/products/{id}` to check ownership/role,
So that users cannot delete other users' products.

**Acceptance Criteria:**

**Given** authenticated user calls DELETE
**When** user does not have admin role
**Then** returns 403 Forbidden
**And** admin role can delete any product

---

## Epic 7: Production Hardening & Launch

**Goal:** Ensure production readiness with security, monitoring, and performance.

### Story 7.1: Add Global Error Handling & Security Headers

As a developer,
I want structured error handling and security middleware,
So that the API is safe and consistent.

**Acceptance Criteria:**

**Given** any unhandled exception
**Then** GlobalExceptionHandler returns ProblemDetails (never stack trace in production)
**And** CORS is configured for frontend origin only
**And** HSTS header is set in production
**And** rate limiting returns 429 with Retry-After header

### Story 7.2: Add Structured Logging & Health Checks

As a developer,
I want Serilog logging and comprehensive health checks,
So that I can monitor production and detect issues.

**Acceptance Criteria:**

**Given** the app is running
**When** `/health` is called
**Then** it checks: PostgreSQL connection, each store API reachability
**And** all requests are logged with correlation ID, duration, status code
**And** store API failures are logged with circuit breaker state

### Story 7.3: Performance Testing & Optimization

As a developer,
I want verified performance under load,
So that NFR targets are met before launch.

**Acceptance Criteria:**

**Given** 500 simulated concurrent users
**When** running search and bucket comparison workloads
**Then** search p95 <3s, bucket p95 <5s
**And** no connection pool exhaustion
**And** circuit breakers activate correctly under store API pressure
