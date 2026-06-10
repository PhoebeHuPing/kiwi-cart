# Story 1.3: Create EF Core Database Schema

Status: ready-for-dev

## Story

As a developer,
I want EF Core migrations that create the full database schema,
So that the data layer is ready for all features.

## Acceptance Criteria

1. Running `dotnet ef database update` creates all tables
2. Tables created: `products`, `stores`, `prices`, `favorites`, `store_tokens`
3. `prices` has unique constraint on (product_id, store_id)
4. `store_tokens` has unique constraint on store_name
5. `favorites` has unique constraint on (user_id, product_id)
6. All entity configurations use snake_case for table/column names
7. `dotnet build` succeeds with zero errors and warnings

## Tasks / Subtasks

- [ ] Task 1: Install EF Core Design tools (AC: #1)
  - [ ] Add `Microsoft.EntityFrameworkCore.Design` to Api project for CLI tooling

- [ ] Task 2: Configure entity mappings in AppDbContext (AC: #2, #3, #4, #5, #6)
  - [ ] Override `OnModelCreating` with entity configurations
  - [ ] Configure snake_case table names for all entities
  - [ ] Configure snake_case column names
  - [ ] Add unique constraint on `prices` (product_id, store_id)
  - [ ] Add unique constraint on `store_tokens` (store_name/store_brand)
  - [ ] Add unique constraint on `favorites` (user_id, product_id)
  - [ ] Configure relationships (Price→Product, Price→Store, BucketItem→Bucket)

- [ ] Task 3: Create initial migration (AC: #1, #2)
  - [ ] Run `dotnet ef migrations add InitialCreate` from Infrastructure project
  - [ ] Verify generated migration creates all expected tables

- [ ] Task 4: Verify build (AC: #7)
  - [ ] Run `dotnet build` — zero errors, zero warnings
  - [ ] Run `dotnet test` — existing tests pass

## Dev Notes

### Architecture Constraints

- EF Core migrations live in `KiwiCart.Infrastructure/Data/Migrations/`
- `AppDbContext` is in `KiwiCart.Infrastructure/Data/AppDbContext.cs`
- Snake_case naming: use `ToTable("products")`, `HasColumnName("product_id")` etc.
- DO NOT add `Microsoft.EntityFrameworkCore` to Core project — Core has no framework deps

### Database Schema (from architecture doc)

```
products: id, name, brand, category
stores: id, name, brand, latitude, longitude, address
prices: id, product_id, store_id, amount, retrieved_at
  → UNIQUE(product_id, store_id)
favorites: id, user_id, product_id
  → UNIQUE(user_id, product_id)
store_tokens: id, store_brand, token, expires_at
  → UNIQUE(store_brand)
```

### EF Core CLI Commands

```bash
# From server-dotnet/ directory:
dotnet ef migrations add InitialCreate --project src/KiwiCart.Infrastructure --startup-project src/KiwiCart.Api

# To apply (requires running PostgreSQL):
dotnet ef database update --project src/KiwiCart.Infrastructure --startup-project src/KiwiCart.Api
```

### Naming Convention Approach

Use Fluent API in `OnModelCreating` to set snake_case names explicitly. Example:

```csharp
modelBuilder.Entity<Product>(e =>
{
    e.ToTable("products");
    e.Property(p => p.Id).HasColumnName("id");
    e.Property(p => p.Name).HasColumnName("name");
});
```

### References

- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Completion Notes List

(to be filled on completion)

### File List

(to be filled — all files created/modified)
