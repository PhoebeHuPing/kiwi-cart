---
baseline_commit: b4ee5e35bcf4695c69c68cfbf60263b6ea730afb
---

# Story 1.2: Configure Local PostgreSQL with Docker Compose

Status: done

## Story

As a developer,
I want a Docker Compose setup for local PostgreSQL,
So that I can develop without external database dependencies and have parity with production.

## Acceptance Criteria

1. `docker-compose.yml` exists at the project root
2. Running `docker-compose up` starts a PostgreSQL 16 instance on port 5432
3. Database `kiwicart` is created with user `dev` / password `dev`
4. `appsettings.Development.json` in `KiwiCart.Api` contains the correct connection string
5. `.env.example` documents all required environment variables
6. The .NET app can connect to the DB using the connection string (verified by health check or `dotnet ef dbcontext info`)

## Tasks / Subtasks

- [ ] Task 1: Create docker-compose.yml at project root (AC: #1, #2, #3)
  - [ ] Add `postgres:16` service with `POSTGRES_DB=kiwicart`, `POSTGRES_USER=dev`, `POSTGRES_PASSWORD=dev`
  - [ ] Expose port 5432:5432
  - [ ] Add named volume `postgres_data` for persistence
  - [ ] Add optional `pgadmin` service (port 5050) for database inspection

- [ ] Task 2: Update appsettings.Development.json (AC: #4)
  - [ ] Set `ConnectionStrings:DefaultConnection` to `Host=localhost;Port=5432;Database=kiwicart;Username=dev;Password=dev`

- [ ] Task 3: Register DbContext in Program.cs (AC: #6)
  - [ ] Add `AddDbContext<AppDbContext>` reading `ConnectionStrings:DefaultConnection`
  - [ ] Install `Npgsql.EntityFrameworkCore.PostgreSQL` already in Infrastructure csproj — ensure the DI wiring is in Api

- [ ] Task 4: Update .env.example (AC: #5)
  - [ ] Add `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` entries with comments

- [ ] Task 5: Verify connectivity (AC: #6)
  - [ ] Run `docker-compose up -d`
  - [ ] Run `dotnet build` — zero errors
  - [ ] Confirm app starts without DB connection errors (`dotnet run` or health check)

## Dev Notes

### Architecture Constraints

- `docker-compose.yml` lives at **project root** (`KiwiCart/`), NOT inside `server-dotnet/` — per architecture doc project structure
- Connection string format for Npgsql: `Host=localhost;Port=5432;Database=kiwicart;Username=dev;Password=dev`
- `AppDbContext` is in `KiwiCart.Infrastructure` — DI registration happens in `KiwiCart.Api/Program.cs`
- DO NOT run `dotnet ef migrations` in this story — that is Story 1.3

### docker-compose.yml Reference

```yaml
services:
  postgres:
    image: postgres:16
    container_name: kiwicart-postgres
    environment:
      POSTGRES_DB: kiwicart
      POSTGRES_USER: dev
      POSTGRES_PASSWORD: dev
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  pgadmin:
    image: dpage/pgadmin4
    container_name: kiwicart-pgadmin
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@kiwicart.local
      PGADMIN_DEFAULT_PASSWORD: admin
    ports:
      - "5050:80"
    depends_on:
      - postgres

volumes:
  postgres_data:
```

### Program.cs DbContext Registration

Add to `Program.cs` after existing service registrations:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Requires `using KiwiCart.Infrastructure.Data;` and `using Microsoft.EntityFrameworkCore;`

### appsettings.Development.json Target State

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Information" } },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=kiwicart;Username=dev;Password=dev"
  }
}
```

### NuGet Note

`Npgsql.EntityFrameworkCore.PostgreSQL` is already referenced in `KiwiCart.Infrastructure.csproj`.
`KiwiCart.Api` references `KiwiCart.Infrastructure`, so `UseNpgsql` extension is available in `Program.cs` without additional package.
However, `Microsoft.EntityFrameworkCore` usings may need `using Microsoft.EntityFrameworkCore;` at the top of `Program.cs`.

### Files to Modify (UPDATE — read before editing)

- `server-dotnet/src/KiwiCart.Api/Program.cs` — currently has service registrations; add DbContext after `AddProblemDetails()`
- `server-dotnet/src/KiwiCart.Api/appsettings.Development.json` — already has structure; update `ConnectionStrings.DefaultConnection`
- `.env.example` — already exists at project root; append new entries

### References

- [Source: _bmad-output/planning-artifacts/architecture.md#Local Development]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Completion Notes List

(to be filled on completion)

### File List

(to be filled — all files created/modified)
