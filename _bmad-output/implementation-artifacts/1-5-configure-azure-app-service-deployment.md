# Story 1.5: Configure Azure App Service Deployment

Status: in-progress

## Story

As a developer,
I want automated deployment to Azure on merge to main,
So that production stays up-to-date.

## Acceptance Criteria

1. `.github/workflows/deploy.yml` exists
2. Triggers on push to `main` (after PR merge)
3. Builds and publishes .NET API
4. Deploys to Azure App Service
5. Health check `/health` returns 200 after deploy

## Dev Agent Record

### Agent Model Used

Kiro CLI (Auto)

### File List

- .github/workflows/deploy.yml
