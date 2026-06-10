# Story 1.4: Setup GitHub Actions CI Pipeline

Status: ready-for-dev

## Story

As a developer,
I want automated build and test on every PR,
So that broken code is caught before merge.

## Acceptance Criteria

1. `.github/workflows/ci.yml` exists
2. Pipeline triggers on PRs to `main`
3. .NET solution builds successfully in CI
4. All .NET unit tests pass in CI
5. Frontend `npm run build` succeeds in CI
6. Pipeline result is reported on the PR

## Tasks / Subtasks

- [ ] Task 1: Create `.github/workflows/ci.yml` (AC: #1, #2, #3, #4, #5, #6)
  - [ ] Trigger on `pull_request` to `main`
  - [ ] Job 1: .NET — checkout, setup .NET 8, restore, build, test
  - [ ] Job 2: Frontend — checkout, setup Node 20, npm install, npm run build

- [ ] Task 2: Verify workflow syntax
  - [ ] Validate YAML structure is correct
  - [ ] Confirm paths are correct for solution and frontend

## Dev Notes

### Architecture Reference

From architecture doc CI/CD section:

**ci.yml (on PR):**
1. Checkout → Setup .NET 8 → Restore → Build → Test
2. Checkout → Setup Node 20 → npm install → npm run build

### Key Paths

- .NET solution: `server-dotnet/KiwiCart.sln`
- Frontend: root directory (package.json at project root)

### Workflow Template

```yaml
name: CI

on:
  pull_request:
    branches: [main]

jobs:
  dotnet:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
        working-directory: server-dotnet
      - run: dotnet build --no-restore
        working-directory: server-dotnet
      - run: dotnet test --no-build
        working-directory: server-dotnet

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
      - run: npm ci
      - run: npm run build
```

### References

- [Source: _bmad-output/planning-artifacts/architecture.md#CI/CD Pipeline]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.4]

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Completion Notes List

(to be filled on completion)

### File List

(to be filled — all files created/modified)
