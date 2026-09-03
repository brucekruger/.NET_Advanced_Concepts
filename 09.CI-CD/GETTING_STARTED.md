# GitLab CI/CD Implementation - Getting Started Guide

## Overview

This guide walks you through deploying the 09.CI-CD project with **GitLab CI/CD** following **GitLab Flow** branching strategy. The pipeline automates CI (build/test) and CD (package/deploy) for CatalogService.Api and CartService.Api targeting **.NET 10**.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Step 1: Push to GitLab](#step-1-push-to-gitlab)
3. [Step 2: Set Up GitLab Runner](#step-2-set-up-gitlab-runner)
4. [Step 3: Create Protected Branch](#step-3-create-protected-branch)
5. [Step 4: Create and Test MR](#step-4-create-and-test-merge-request)
6. [Step 5: Deploy](#step-5-deploy)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

- **Local Git repository**: The 09.CI-CD folder initialized with git
- **GitLab account**: Access to gitlab.com or private GitLab instance
- **Docker** installed (for local testing and runner)
- **GitLab Runner** (will be installed in Step 2)
- **.NET 10 SDK** (for local builds)

---

## Step 1: Push to GitLab

### 1.1 Create a GitLab Project

1. Log in to [gitlab.com](https://gitlab.com) (or your GitLab instance)
2. Click **New project** → **Create blank project**
3. Name: `09.CI-CD`
4. Visibility: Private (recommended)
5. Click **Create project**

### 1.2 Add GitLab Remote and Push

```bash
cd D:\Work\Trainings\.NET_Advanced_Concepts\09.CI-CD

# Add GitLab remote
git remote add gitlab https://gitlab.com/YOUR_USERNAME/09.CI-CD.git

# Push current branch (feature/09.CI-CD)
git push -u gitlab feature/09.CI-CD

# Push master (to set up base branch)
git push gitlab master
```

**Verify**: Go to your GitLab project → **Repository** tab. You should see both `master` and `feature/09.CI-CD` branches.

---

## Step 2: Set Up GitLab Runner

A **GitLab Runner** is a worker machine that executes CI/CD jobs. We'll use a **Docker executor** for reproducible builds.

### 2.1 Install GitLab Runner

**On Windows (Recommended for this project):**

```powershell
# Option A: Using Chocolatey
choco install gitlab-runner

# Option B: Using Windows Package Manager
winget install GitLab.Runner

# Option C: Download manually
# https://docs.gitlab.com/runner/install/windows.html
```

**On macOS:**

```bash
brew install gitlab-runner
```

**On Linux (Ubuntu/Debian):**

```bash
curl -L https://packages.gitlab.com/install/repositories/runner/gitlab-runner/script.deb.sh | bash
apt-get install gitlab-runner
```

### 2.2 Verify Installation

```bash
gitlab-runner --version
```

Output should show: `GitLab Runner Version X.X.X`

### 2.3 Register Runner with Docker

1. **Get registration token:**
   - Go to your GitLab project
   - **Settings** → **CI/CD** → **Runners** (left panel)
   - Copy the **Group registration token** (or instance token if available)

2. **Register the runner** (run in PowerShell/Terminal as Administrator):

```bash
gitlab-runner register \
  --url https://gitlab.com/ \
  --registration-token <YOUR_TOKEN> \
  --executor docker \
  --docker-image docker:latest \
  --docker-volumes /var/run/docker.sock:/var/run/docker.sock \
  --description "Docker Runner for 09.CI-CD" \
  --tag-list "docker,dotnet10" \
  --run-untagged
```

**Note for Windows**: If `/var/run/docker.sock` isn't available, use:
- Docker Desktop: `npipe:////./pipe/docker_engine`
- Or use PowerShell executor with Docker installed locally

### 2.4 Start the Runner

**Option A: Run in foreground (testing)**

```bash
gitlab-runner run
```

You should see: `Runner started! To quit, press Ctrl+C`

**Option B: Install as Service (production)**

```bash
# Windows (as Administrator)
gitlab-runner install --user SYSTEM

# Start the service
gitlab-runner start

# Verify
gitlab-runner list
```

**Option C: macOS / Linux**

```bash
# For systemd (Linux)
sudo systemctl start gitlab-runner

# For launchd (macOS)
brew services start gitlab-runner
```

### 2.5 Verify Runner in GitLab

1. Go to your GitLab project → **Settings** → **CI/CD** → **Runners**
2. You should see your runner listed as **Online** (green circle)

---

## Step 3: Create Protected Branch

Protected branches enforce that:
- Pipelines must pass before merge
- Only specific users can merge
- Force push is disabled

### 3.1 Set Up Protected Master

1. Go to your GitLab project → **Settings** → **Repository**
2. Scroll to **Protected Branches**
3. Click **Add protection**
4. **Branch name**: `master`
5. **Protect branch**: ✓
6. **Require pipelines to succeed**: ✓
7. **Require all approvals before merge** (optional): ✓
8. Click **Save**

---

## Step 4: Create and Test Merge Request

### 4.1 Open a Merge Request

1. Go to your GitLab project → **Merge requests** → **New merge request**
2. **Source branch**: `feature/09.CI-CD`
3. **Target branch**: `master`
4. **Title**: `feat: add CI/CD pipeline with GitLab Flow`
5. **Description**:
   ```
   # CI/CD Implementation

   - Add .gitlab-ci.yml with build, test, package, and deploy stages
   - Configure path-based triggering for CatalogService and CartService
   - Add Terraform IaC configuration
   - Implement GitLab Flow branching strategy

   Closes: #1 (optional, if you have an issue)
   ```
6. Click **Create merge request**

### 4.2 Monitor the Pipeline

1. The MR page should show **Pipeline #123** in green (or pending)
2. Click the pipeline link to view jobs:
   - `build:catalog` → Running/Passed
   - `test:catalog` → Running/Passed
   - `build:cart` → Running/Passed
   - `test:cart` → Running/Passed
3. Wait for all jobs to complete (typically 5-10 minutes for first run)

### 4.3 View Test Results

1. After pipeline passes, click **Merge request** → **Tests** tab
2. See junit XML reports from unit/integration tests

### 4.4 Merge the MR

Once the pipeline is green:

1. Click **Merge** button on the MR page
2. **Squash commits when merge request is accepted**: ✓ (recommended for clean history)
3. Click **Merge** → Confirmed

---

## Step 5: Deploy

After merging to master, the **master branch pipeline** automatically starts with build → test → package stages.

### 5.1 Deploy with Docker Compose (Manual)

1. Go to **CI/CD** → **Pipelines** (make sure you're on `master` branch)
2. Click the latest pipeline (should be green after build + test)
3. Scroll to **deploy** stage
4. Click the **play icon** (▶) next to `deploy:docker-compose:master`
5. Confirm the manual trigger
6. Watch the logs:
   - `docker-compose up -d` starts services
   - Smoke tests verify `/health` endpoints

**After deployment:**

```bash
# Verify containers are running
docker ps

# Check logs
docker-compose logs -f catalog-service-api
docker-compose logs -f cart-service-api

# Test endpoints
curl http://localhost:5001/health    # Catalog
curl http://localhost:5002/health    # Cart
```

### 5.2 Terraform Apply (Manual)

To provision infrastructure with Terraform:

1. Go to **CI/CD** → **Pipelines** (on master)
2. Click the pipeline
3. Scroll to **infra** stage
4. Click the **play icon** (▶) next to `infra:apply:master`
5. Confirm
6. Watch the logs for Terraform output

---

## Troubleshooting

### Runner Not Appearing in GitLab

**Symptom:** Pipeline hangs, no runner picked up

**Solution:**
1. Verify runner is running: `gitlab-runner list`
2. If offline, restart: `gitlab-runner run`
3. Check runner logs for errors:
   ```bash
   gitlab-runner --debug run
   ```
4. Ensure runner has `--run-untagged` or correct tags

### Pipeline Jobs Timeout

**Symptom:** `Job execution timeout after 5 minutes`

**Solution:**
1. Increase job timeout in `.gitlab-ci.yml`:
   ```yaml
   build:catalog:
     timeout: 30 minutes
   ```
2. Or increase runner timeout:
   ```bash
   gitlab-runner register ... --timeout 1800
   ```

### Docker Image Pull Fails

**Symptom:** `Error: pull access denied for mcr.microsoft.com/dotnet/sdk:10.0`

**Solution:**
1. Check network connectivity on runner machine
2. Verify Docker can pull images: `docker pull mcr.microsoft.com/dotnet/sdk:10.0`
3. If behind proxy, configure Docker daemon

### Test Failures

**Symptom:** `Test:catalog job failed`

**Solution:**
1. Click failed job in GitLab UI
2. Scroll to end of log to see error details
3. Common issues:
   - Database not running → Add `services:` to job config
   - Path issues → Check relative paths in tests
   - Missing dependencies → `dotnet restore` all projects

---

## Next Steps

1. **Monitor Pipeline**: Go to **CI/CD → Pipelines** regularly to ensure builds stay green
2. **Create Feature Branches**: For new features, branch from `master`:
   ```bash
   git checkout -b feature/my-feature master
   git push -u gitlab feature/my-feature
   git push gitlab feature/my-feature  # Create MR in GitLab UI
   ```
3. **Extend CD**: Add cloud deployment (Azure, AWS) in additional stages
4. **Set Up Notifications**: **Settings → Integrations** for Slack/email alerts

---

## Success Checklist

- [ ] GitLab project created and pushed
- [ ] GitLab Runner installed and online
- [ ] Master branch protected
- [ ] MR created from feature branch
- [ ] MR pipeline passed (build + test green)
- [ ] MR merged to master
- [ ] Master pipeline ran automatically
- [ ] Deploy job manually triggered and succeeded
- [ ] Docker containers running (`docker ps`)
- [ ] Health endpoints responding (`curl http://localhost:5001/health`)

---

## Support

For issues:
1. Check `.gitlab-ci.yml` comments for inline guidance
2. Review `CI_CD_PIPELINE_GUIDE.md` for detailed reference
3. Consult `IMPLEMENTATION_PLAN.md` for architectural decisions
4. GitLab Docs: https://docs.gitlab.com/ee/ci/

---

**Status**: Ready for deployment  
**Framework**: .NET 10  
**Platform**: GitLab CI/CD  
**Branch Strategy**: GitLab Flow  
