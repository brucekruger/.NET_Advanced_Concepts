# GitLab CI/CD Pipeline Documentation

## Overview

This project implements a **GitLab CI/CD pipeline** following **GitLab Flow** (simplified) for the 09.CI-CD microservices project. The pipeline automates building, testing, packaging, and deploying the **CatalogService.Api** and **CartService.Api** services targeting **.NET 9**.

---

## Quick Start

### Prerequisites

1. **GitLab Repository**: Push the project to a GitLab instance
   ```bash
   git remote add gitlab <GITLAB_PROJECT_URL>.git
   git push -u gitlab feature/09.CI-CD
   ```

2. **GitLab Runner**: Set up a runner with Docker support
   ```bash
   # On your runner machine
   gitlab-runner install
   gitlab-runner register
   # Choose executor: docker
   # Docker image: docker:latest (or mcr.microsoft.com/dotnet/sdk:9.0)
   ```

3. **Protected Branch**: Mark `master` as protected in GitLab
   - Settings → Repository → Protected Branches
   - Require pipeline to succeed before merge

4. **CI/CD Variables** (optional, for docker push):
   - `DOCKER_REGISTRY_USER`: Your registry username
   - `DOCKER_REGISTRY_PASSWORD`: Your registry password (masked)

---

## Branching Strategy

### Git Flow (Simplified for this project)

```
master (protected, always deployable)
   │
   ├── feature/09.ci-pipeline
   ├── feature/09.path-filters
   └── feature/09.cd-terraform
```

**Rules:**
1. Only `master` is long-lived.
2. Create short-lived branches for features/fixes: `feature/*` or `fix/*`.
3. Push to GitLab and open a **Merge Request (MR)** into `master`.
4. Pipeline must be green (all jobs pass) before merge.
5. Use **Squash commits** option when merging for a clean history.
6. After merge to `master`, a deploy pipeline runs automatically.

---

## Pipeline Architecture

### Merge Request Pipelines (on: `merge_request_event`)

Triggered when an MR is opened or updated targeting `master`.

| Job | Trigger | Purpose |
|-----|---------|---------|
| `build:catalog` | Changes in `CatalogService*/**` | Restore & compile Catalog |
| `build:cart` | Changes in `CartService*/**` | Restore & compile Cart |
| `test:catalog` | Changes in `CatalogService*/**` | Run Catalog unit + integration tests |
| `test:cart` | Changes in `CartService*/**` | Run Cart tests |
| `infra:plan:mr` | Changes in `infra/**` or `docker-compose.yml` | Terraform plan (dry-run) |

**Status**: MR cannot merge unless pipeline succeeds.

### Master Branch Pipelines (on: `push` to `master`)

Triggered after an MR is merged into `master`.

| Stage | Jobs | Purpose |
|-------|------|---------|
| **build** | `build:all:master` | Restore & compile full solution |
| **test** | `test:all:master` | All unit + integration tests |
| **package** | `package:docker:master` | Build Docker images |
| **infra** | `infra:apply:master` | Terraform apply (manual trigger) |
| **deploy** | `deploy:docker-compose:master` | Deploy with Docker Compose + smoke tests (manual trigger) |

---

## File Structure

```
09.CI-CD/
├── .gitlab-ci.yml                 # Main CI/CD pipeline definition
├── ci/
│   ├── build-test.yml             # (Optional) Build/test templates
│   └── deploy.yml                 # (Optional) Deploy templates
├── infra/
│   ├── main.tf                    # Terraform resources
│   ├── variables.tf               # Terraform variables
│   └── terraform.tfvars           # (Git-ignored) Local overrides
├── CatalogService.Api/
│   └── Dockerfile
├── CartService.Api/
│   └── Dockerfile
├── docker-compose.yml             # Local/CD deployment
├── 09.CI-CD.sln                   # Solution file
└── README.md                      # This file
```

---

## Path-Based Triggering

The pipeline uses **GitLab `rules: changes`** to detect which services changed:

### Catalog Service
Triggers `build:catalog`, `test:catalog`, `test:all:master` when changes in:
- `CatalogService*/**` (includes all subdirectories)
- `*.sln` (solution file)
- `.gitlab-ci.yml` (pipeline config)
- `ci/**` (shared templates)

### Cart Service
Triggers `build:cart`, `test:cart`, `test:all:master` when changes in:
- `CartService*/**`
- `*.sln`
- `.gitlab-ci.yml`
- `ci/**`

### Shared / Infra
Always affects both services or infrastructure:
- `docker-compose.yml`
- `infra/**`
- `README.md` (skips heavy build/test jobs)

---

## Environment Variables

Key variables available in the pipeline:

| Variable | Value | Purpose |
|----------|-------|---------|
| `$CI_COMMIT_SHA` | Git commit hash | Unique build identifier |
| `$CI_COMMIT_REF_SLUG` | Branch name (slugified) | Cache key, image tag |
| `$CI_REGISTRY_IMAGE` | GitLab Registry image path | Docker image repository |
| `$DOTNET_SDK_IMAGE` | `mcr.microsoft.com/dotnet/sdk:9.0` | Build/test base image |
| `$DOTNET_RUNTIME_IMAGE` | `mcr.microsoft.com/dotnet/aspnet:9.0` | Runtime base image |
| `$MSSQL_IMAGE`, `$REDIS_IMAGE`, etc. | Service images | docker-compose |

---

## Common Tasks

### Run a Local Test Build

Test the pipeline without pushing:

```bash
# Install GitLab Runner locally
# Then, in the project root:
gitlab-runner exec docker build:catalog
gitlab-runner exec docker test:catalog
```

### Trigger a Manual Deployment

1. Go to **CI/CD → Pipelines** (on master branch).
2. Click the running pipeline.
3. Scroll to **deploy** stage and click the play icon (▶) next to `deploy:docker-compose:master` or `infra:apply:master`.

### View Test Results

1. Go to **CI/CD → Pipelines**.
2. Click a pipeline → **Tests** tab to see junit XML results.

### Debug a Failing Job

1. Go to **CI/CD → Pipelines**.
2. Click the failing job.
3. Scroll to the end of the log for error details.
4. Common issues:
   - Docker daemon not running on the runner
   - Missing environment variables
   - Test database not accessible

---

## Protected Branch Rules

To enforce quality gates on `master`:

1. **Settings → Repository → Protected Branches**
2. Protect `master`:
   - ✅ Require pipelines to succeed
   - ✅ Require approvals (optional, 1+ reviewer)
   - ✅ Dismiss approvals on new commit
   - ✅ Allow force push (optional)

---

## CI/CD Best Practices Applied

1. **Caching**: NuGet packages cached per branch to speed up builds.
2. **Artifacts**: Test reports and terraform plans persisted between stages.
3. **Rules**: Path-based job filtering to avoid unnecessary runs.
4. **Needs**: Explicit job dependencies (e.g., `test` needs `build`).
5. **Secrets**: Sensitive variables marked as masked/protected.
6. **Manual Triggers**: Deploy & Terraform apply are manual to prevent auto-deploy.
7. **Health Checks**: Smoke tests run post-deploy to verify endpoints.

---

## Stretch Goals (Optional)

### Push to GitLab Container Registry

1. Uncomment docker push in `package:docker:master`:
   ```bash
   docker login -u $CI_REGISTRY_USER -p $CI_REGISTRY_PASSWORD $CI_REGISTRY
   docker push $REGISTRY_IMAGE/catalog-service:latest
   docker push $REGISTRY_IMAGE/cart-service:latest
   ```

2. Add CI/CD variables in GitLab:
   - `$CI_REGISTRY_USER`
   - `$CI_REGISTRY_PASSWORD` (masked)

### Deploy to Azure Container Apps

Replace `deploy:docker-compose:master` with:

```yaml
deploy:azure:master:
  stage: deploy
  image: mcr.microsoft.com/azure-cli:latest
  script:
    - az login --service-principal -u $AZURE_CLIENT_ID -p $AZURE_CLIENT_SECRET --tenant $AZURE_TENANT_ID
    - az containerapp up --name catalog-service --resource-group $AZURE_RESOURCE_GROUP --image $REGISTRY_IMAGE/catalog-service:latest
    - az containerapp up --name cart-service --resource-group $AZURE_RESOURCE_GROUP --image $REGISTRY_IMAGE/cart-service:latest
```

Add variables:
- `$AZURE_CLIENT_ID`
- `$AZURE_CLIENT_SECRET` (masked)
- `$AZURE_TENANT_ID`
- `$AZURE_RESOURCE_GROUP`

---

## Acceptance Checklist

- [ ] `.gitlab-ci.yml` file created and pushed to `feature/09.CI-CD` branch
- [ ] GitLab Runner set up (docker executor)
- [ ] Push branch to GitLab, open MR
- [ ] MR pipeline runs (build + test jobs pass)
- [ ] Merge MR after pipeline success
- [ ] Master pipeline runs (build + test + package)
- [ ] Manual trigger `deploy:docker-compose:master` succeeds
- [ ] Docker containers running and `/health` endpoints respond
- [ ] Protected branch rules enforced (pipeline must pass)
- [ ] Test results visible in GitLab UI (junit reports)
- [ ] Terraform plan succeeds (`infra/` changes)
- [ ] Documentation updated in README

---

## Troubleshooting

### Pipeline stuck or not triggering

- Check `.gitlab-ci.yml` syntax: **CI/CD → Pipelines → Validate**
- Verify runner is registered: **Settings → CI/CD → Runners**
- Check `rules: changes:` paths match your file structure

### Docker build fails with "Cannot connect to Docker daemon"

- Ensure runner has Docker socket access:
  ```bash
  gitlab-runner verify
  ```
- For shell executor, install Docker Desktop and add runner user to docker group:
  ```bash
  sudo usermod -aG docker $USER
  ```

### Tests timeout

- Increase `CI_JOB_TIMEOUT` in runner config or job-level `timeout:`.
- Consider splitting large test suites.

### Out of disk space

- Clean up old pipelines: **Settings → CI/CD → Artifacts**
- Set shorter expiry times for artifacts.

---

## References

- [GitLab CI/CD Documentation](https://docs.gitlab.com/ee/ci/)
- [GitLab Runner Installation](https://docs.gitlab.com/runner/install/)
- [Protected Branches](https://docs.gitlab.com/ee/user/project/protected_branches.html)
- [GitLab Flow](https://docs.gitlab.com/ee/topics/gitlab_flow.html)
- [Docker Executor](https://docs.gitlab.com/runner/executors/docker.html)
- [Terraform GitLab Provider](https://registry.terraform.io/providers/gitlabhq/gitlab/latest)

---

## Contact & Support

For questions or issues:
1. Check the pipeline logs: **CI/CD → Pipelines → [Job Name]**
2. Review `.gitlab-ci.yml` comments for inline guidance
3. Consult `IMPLEMENTATION_PLAN.md` for design decisions

---

**Last Updated**: 2026-09-03  
**Target Framework**: .NET 9  
**Platform**: GitLab CI/CD  
**Status**: Initial Implementation
