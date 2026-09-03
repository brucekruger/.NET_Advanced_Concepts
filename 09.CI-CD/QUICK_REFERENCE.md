# GitLab CI/CD Quick Reference Card

A handy reference for common GitLab CI/CD operations and commands.

## Branching Commands

```bash
# Create and push feature branch
git checkout -b feature/my-feature master
git push -u gitlab feature/my-feature

# After merge, clean up local branch
git branch -d feature/my-feature
git fetch gitlab --prune  # Remove remote tracking branches

# Switch to master and sync
git checkout master
git pull gitlab master
```

## Pipeline Operations

### View Pipeline Status

```bash
# Online - GitLab UI
# Project → CI/CD → Pipelines

# CLI - Requires GitLab CLI (glab)
glab pipeline list
glab pipeline view <PIPELINE_ID>
```

### Trigger Manual Jobs

```bash
# Via Web UI
# 1. Go to CI/CD → Pipelines
# 2. Click pipeline number
# 3. Find job
# 4. Click play icon (▶)
# 5. Confirm

# Via CLI (glab)
glab pipeline run
```

### View Job Logs

```bash
# Online
# 1. Go to pipeline
# 2. Click job name
# 3. Scroll to bottom for latest output

# CLI (glab)
glab job trace <JOB_ID>
glab job logs <JOB_ID> --follow
```

## Runner Commands

### Runner Management

```bash
# Check runner version
gitlab-runner --version

# List registered runners
gitlab-runner list

# Register new runner
gitlab-runner register --url https://gitlab.com/ --registration-token <TOKEN>

# Remove runner
gitlab-runner unregister --name <RUNNER_NAME>

# Update runner
gitlab-runner update
```

### Start/Stop Runner

```bash
# Run in foreground (development)
gitlab-runner run

# Install as service (production)
gitlab-runner install --user SYSTEM

# Start service
gitlab-runner start

# Stop service
gitlab-runner stop

# View service status
gitlab-runner status

# View runner logs
gitlab-runner --debug run
```

## Local Testing

```bash
# Test a job locally (requires Docker)
gitlab-runner exec docker build:catalog

# Test with custom image
gitlab-runner exec docker --docker-image mcr.microsoft.com/dotnet/sdk:10.0 build:catalog

# List available jobs (simulate)
grep "^[a-z].*:$" .gitlab-ci.yml
```

## Docker & Deployment

### Docker Commands

```bash
# Build images manually
docker build -t catalog-service:latest -f CatalogService.Api/Dockerfile .
docker build -t cart-service:latest -f CartService.Api/Dockerfile .

# List images
docker images

# Remove image
docker rmi <IMAGE_ID>
```

### Docker Compose Commands

```bash
# Start services
docker-compose up -d

# View running services
docker ps

# View logs
docker-compose logs -f [SERVICE_NAME]

# Restart service
docker-compose restart [SERVICE_NAME]

# Stop all services
docker-compose stop

# Remove all services and volumes (careful!)
docker-compose down -v

# Test endpoint
docker run --network host curlimages/curl:latest curl http://localhost:5001/health
```

## Terraform Commands

```bash
cd infra/

# Initialize Terraform (downloads providers)
terraform init

# Validate configuration
terraform validate

# Plan changes (dry-run)
terraform plan -out=tfplan

# Apply changes
terraform apply tfplan

# Show current state
terraform show

# Destroy resources (⚠️ careful)
terraform destroy

# Format code
terraform fmt -recursive
```

## GitLab UI Navigation

```
Project Home
├── Repository
│   ├── Files
│   ├── Branches
│   ├── Tags
│   ├── Commits
│   └── Compare (for diffs)
│
├── Merge Requests
│   ├── List
│   ├── New MR
│   └── Draft MRs
│
├── CI/CD
│   ├── Pipelines (current status)
│   ├── Jobs (detailed logs)
│   ├── Artifacts (downloadable files)
│   ├── Schedules (recurring pipelines)
│   └── Runners (worker machines)
│
├── Deployments
│   ├── Environments (staging, prod, etc.)
│   └── Deployments (history)
│
└── Settings
    ├── Repository
    │   ├── Protected Branches
    │   └── Deploy Keys
    ├── CI/CD
    │   ├── Runners
    │   ├── CI/CD Variables
    │   ├── Pipelines
    │   └── Schedules
    └── Integrations (Slack, etc.)
```

## Common Issues & Fixes

### Pipeline Won't Trigger

```bash
# Check runner is online
gitlab-runner list

# Validate .gitlab-ci.yml syntax
# Option 1: GitLab UI → CI/CD → Pipelines → Validate
# Option 2: Local validation (requires yq)
yq eval . .gitlab-ci.yml

# Check branch push event
git push -u gitlab feature/my-feature
# Should see "Create merge request" link in terminal
```

### Runner Job Timeout

```bash
# Increase job timeout in .gitlab-ci.yml
build:catalog:
  timeout: 30 minutes
  script:
    - dotnet build ...

# Or increase runner config timeout (gitlab-runner config.toml)
# Edit: ~/.gitlab-runner/config.toml
# Find: timeout = 3600
# Change: timeout = 5400  (90 minutes)
```

### Docker Build Fails

```bash
# Check Docker daemon
docker ps  # Should list containers

# Verify runner Docker access
docker ps  # Run as user gitlab-runner runs as

# Check disk space
docker system df  # Show space used

# Clean up
docker system prune -a
```

### Tests Timing Out

```bash
# Run tests locally to measure time
dotnet test CatalogService.UnitTests/CatalogService.UnitTests.csproj -c Release

# If slow, consider:
# 1. Increase timeout in .gitlab-ci.yml
# 2. Split tests into multiple jobs
# 3. Use test parallelization
```

## Environment Variables

### Built-in Variables

```yaml
$CI_COMMIT_SHA      # Full commit hash (40 chars)
$CI_COMMIT_SHORT_SHA # Short commit hash (8 chars)
$CI_COMMIT_REF_SLUG # Branch name (lowercase, hyphenated)
$CI_COMMIT_MESSAGE  # Commit message
$CI_PIPELINE_ID     # Pipeline ID number
$CI_JOB_ID          # Job ID number
$CI_REGISTRY_IMAGE  # Registry image path
$CI_MERGE_REQUEST_* # MR details (if MR event)
```

### Custom Variables

Set in GitLab UI → Settings → CI/CD → Variables:

```bash
# Protected (masked)
DOCKER_REGISTRY_PASSWORD  # Only used in protected branches/tags
SONAR_TOKEN              # For code quality scanning

# Masked (hidden in logs)
DATABASE_PASSWORD
API_KEY

# File-based (for certs, keys)
DOCKER_AUTH_CONFIG  # Uploaded file
```

## Performance Tips

```bash
# Use caching for dependencies
cache:
  key: build-$CI_COMMIT_REF_SLUG
  paths:
    - .nuget/

# Use artifacts to pass data between jobs
artifacts:
  paths:
    - bin/Release/

# Set needs: to only wait for required jobs
test:catalog:
  needs:
    - build:catalog

# Use before_script for one-time setup
before_script:
  - export PATH="$PATH:/root/.dotnet/tools"

# Run jobs in parallel (default)
# Sequential: needs: to chain jobs
```

## Security Best Practices

```bash
# Never commit secrets
# Use CI/CD Variables (masked) instead
# Mark as Protected if prod-only

# .gitignore sensitive files
/infra/terraform.tfvars
.env
*.pem
secrets/

# Use service accounts for CI
# Not personal user credentials

# Audit runner access
gitlab-runner verify

# Keep runner updated
gitlab-runner update
```

## Useful Links

```
GitLab Dashboard:          https://gitlab.com/dashboard
Your Projects:             https://gitlab.com/dashboard/projects
Project CI/CD:             https://gitlab.com/YOUR_GROUP/09.CI-CD/-/pipelines
GitLab Docs:               https://docs.gitlab.com/ee/
CI/CD Docs:                https://docs.gitlab.com/ee/ci/
Runner Docs:               https://docs.gitlab.com/runner/
Terraform Docs:            https://registry.terraform.io/
Docker Compose Docs:       https://docs.docker.com/compose/
.NET 10 Docs:               https://learn.microsoft.com/en-us/dotnet/
```

## Time Savers

### Copy Frequently Used Job

```yaml
# Keep a template at top of .gitlab-ci.yml
.standard_build: &standard_build
  image: $DOTNET_SDK_IMAGE
  cache:
    key: build-$CI_COMMIT_REF_SLUG
    paths:
      - .nuget/
  script:
    - dotnet restore
    - dotnet build -c Release

# Use with
build:catalog:
  <<: *standard_build
  rules:
    - if: '$CI_MERGE_REQUEST_EVENT_TYPE == "merge_request"'
      changes:
        - CatalogService*/**
```

### Quick MR Template

Create `.gitlab/merge_request_templates/default.md`:

```markdown
## Description
Brief description of changes

## Related Issue
Closes #123

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation

## Checklist
- [ ] Code reviewed locally
- [ ] Tests passing
- [ ] Documentation updated
```

## Emergency Commands

```bash
# Force push (⚠️ only if no one else is on branch)
git push -f gitlab feature/my-feature

# Revert last commit and push
git revert HEAD
git push gitlab master

# Cancel running pipeline
# via UI: Pipelines → click running pipeline → Cancel

# Remove stuck runner
gitlab-runner unregister --name problem-runner
gitlab-runner register --new-runner  # Register new one

# Emergency deploy (not recommended)
# Direct docker-compose on server
ssh deploy@server
cd /app
docker-compose pull
docker-compose up -d
```

---

## Cheat Sheet Summary

| Task | Command |
|------|---------|
| Create feature | `git checkout -b feature/name` |
| Push branch | `git push -u gitlab feature/name` |
| Open MR | Go to GitLab UI → New MR |
| View pipeline | Project → CI/CD → Pipelines |
| View logs | Click pipeline → Click job |
| Start runner | `gitlab-runner run` |
| Deploy | Click play icon on deploy job |
| Check health | `curl http://localhost:5001/health` |
| View containers | `docker ps` |
| View logs | `docker-compose logs -f` |
| Stop services | `docker-compose down` |

---

**Quick Ref v1.0** | .NET 10 | GitLab CI/CD | Last Updated: 2026-09-03
