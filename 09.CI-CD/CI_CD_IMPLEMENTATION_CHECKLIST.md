# CI/CD Implementation Checklist

Use this checklist to track your progress through the CI/CD implementation and deployment.

## Phase 0: Prerequisites & Setup

- [ ] Read `IMPLEMENTATION_PLAN.md` to understand the project goals
- [ ] Verify .NET 9 SDK installed: `dotnet --version`
- [ ] Verify Docker installed: `docker --version`
- [ ] Have GitLab account access (gitlab.com or private instance)
- [ ] Have access to register GitLab Runner

## Phase 1: Files & Configuration

### Core Pipeline Files
- [ ] `.gitlab-ci.yml` created with:
  - [ ] Build stage (build:catalog, build:cart, build:all:master)
  - [ ] Test stage (test:catalog, test:cart, test:all:master)
  - [ ] Package stage (package:docker:master)
  - [ ] Infra stage (infra:plan:mr, infra:apply:master)
  - [ ] Deploy stage (deploy:docker-compose:master)
- [ ] `.gitignore` created with terraform and sensitive file patterns
- [ ] `ci/build-test.yml` created (optional templates)
- [ ] `ci/deploy.yml` created (optional templates)

### Infrastructure as Code
- [ ] `infra/main.tf` created with Docker resources
- [ ] `infra/variables.tf` created with input variables
- [ ] `infra/terraform.tfvars.example` created as reference

### Helper Scripts
- [ ] `scripts/setup-gitlab-runner.sh` created (Linux/macOS)
- [ ] `scripts/setup-gitlab-runner.ps1` created (Windows)
- [ ] `scripts/validate-ci.sh` created

### Documentation
- [ ] `GETTING_STARTED.md` created and reviewed
- [ ] `CI_CD_PIPELINE_GUIDE.md` created and reviewed
- [ ] `ADVANCED_CI_CD_FEATURES.md` created (reference)
- [ ] `CI_CD_IMPLEMENTATION_SUMMARY.md` created

### Validation
- [ ] All new files added to git: `git add .`
- [ ] Commit message descriptive: `git commit -m "feat: add GitLab CI/CD pipeline"`
- [ ] `.gitlab-ci.yml` syntax valid (GitLab will validate on push)

## Phase 2: GitLab Project Setup

### Create Project
- [ ] GitLab project created (https://gitlab.com/new)
- [ ] Project named: `09.CI-CD`
- [ ] Visibility set to Private (recommended)
- [ ] Project URL noted (e.g., https://gitlab.com/mygroup/09.CI-CD)

### Push Repository
- [ ] Remote added: `git remote add gitlab <URL>.git`
- [ ] Current branch pushed: `git push -u gitlab feature/09.CI-CD`
- [ ] Master branch pushed: `git push gitlab master`
- [ ] Verify in GitLab UI → Repository → Branches
  - [ ] Both `master` and `feature/09.CI-CD` visible

### Verify Files
- [ ] GitLab shows all new files in `feature/09.CI-CD` branch:
  - [ ] `.gitlab-ci.yml`
  - [ ] `.gitignore`
  - [ ] `ci/` folder with templates
  - [ ] `infra/` folder with Terraform
  - [ ] `scripts/` folder with setup guides
  - [ ] Documentation files

## Phase 3: GitLab Runner Setup

### Installation
- [ ] GitLab Runner downloaded/installed
  - [ ] Windows: `choco install gitlab-runner` or manual download
  - [ ] macOS: `brew install gitlab-runner`
  - [ ] Linux: `apt-get install gitlab-runner`
- [ ] Verify installation: `gitlab-runner --version`

### Runner Registration
- [ ] Go to GitLab project → Settings → CI/CD → Runners
- [ ] Copy registration token (Group or instance level)
- [ ] Run registration command:
  ```bash
  gitlab-runner register \
    --url https://gitlab.com/ \
    --registration-token <TOKEN> \
    --executor docker \
    --docker-image docker:latest \
    --docker-volumes /var/run/docker.sock:/var/run/docker.sock
  ```
- [ ] Verify runner registered: `gitlab-runner list`
- [ ] Runner shown as **Online** in GitLab UI (green circle)

### Start Runner
- [ ] Runner started:
  - [ ] Foreground (testing): `gitlab-runner run`
  - [ ] Background (production): `gitlab-runner install && gitlab-runner start`
- [ ] Logs show "Runner started" message

## Phase 4: Protected Branch Configuration

### Master Branch Protection
- [ ] Go to GitLab project → Settings → Repository
- [ ] Scroll to Protected Branches
- [ ] Click **Add protection**
- [ ] Configure:
  - [ ] Branch name: `master`
  - [ ] ✓ Protect branch
  - [ ] ✓ Require pipelines to succeed
  - [ ] ✓ Require all approvals before merge (optional, recommended)
  - [ ] Allow force push (optional, ✓ for flexibility)
- [ ] Click **Save**
- [ ] Verify master shows as "Protected" in Branches list

## Phase 5: Test Merge Request Pipeline

### Create Feature Branch & MR
- [ ] Create local branch: `git checkout -b feature/test-pipeline`
- [ ] Make small test change (e.g., update README)
- [ ] Commit: `git commit -m "test: verify pipeline triggers"`
- [ ] Push to GitLab: `git push -u gitlab feature/test-pipeline`
- [ ] Go to GitLab project → Merge requests
- [ ] Click **New merge request**
  - [ ] Source: `feature/test-pipeline`
  - [ ] Target: `master`
  - [ ] Title: "test: verify CI/CD pipeline"
  - [ ] Create MR

### Monitor MR Pipeline
- [ ] MR page shows "Pipeline #XX" indicator
- [ ] Wait for pipeline to start (may take 30 seconds)
- [ ] Jobs appear:
  - [ ] `validate:gitlab-ci` (optional, may fail - that's ok)
  - [ ] `build:catalog` (running or skipped)
  - [ ] `build:cart` (running or skipped)
  - [ ] `test:catalog` (running or skipped)
  - [ ] `test:cart` (running or skipped)
- [ ] Watch logs by clicking job name
- [ ] All jobs pass (green ✓)
- [ ] MR page shows "Can be merged" message

### Test Results
- [ ] Go to MR → Tests tab
- [ ] View junit XML reports (if tests executed)
- [ ] Test count shown (e.g., "2 tests passed")

## Phase 6: Merge to Master

### Merge MR
- [ ] MR pipeline is green (all jobs passed)
- [ ] MR shows "Can be merged" button
- [ ] Optional: Request approval from team member
- [ ] Click **Merge**
  - [ ] ✓ Squash commits when merge request is accepted (recommended)
  - [ ] Click **Merge** (confirm)
- [ ] MR closed
- [ ] Verify `feature/test-pipeline` branch deleted

### Master Pipeline Execution
- [ ] Go to GitLab → CI/CD → Pipelines
- [ ] New pipeline appears for `master` branch
- [ ] Wait for jobs to complete:
  - [ ] `build:all:master` (running → passed ✓)
  - [ ] `test:all:master` (running → passed ✓)
  - [ ] `package:docker:master` (running → passed ✓)
  - [ ] `infra:plan:mr` (skipped, only on feature branches)
  - [ ] `infra:apply:master` (waiting for manual trigger)
  - [ ] `deploy:docker-compose:master` (waiting for manual trigger)

## Phase 7: Manual Deployment

### Docker Compose Deployment
- [ ] Pipeline on master is green
- [ ] Navigate to pipeline → Deploy stage
- [ ] Find job `deploy:docker-compose:master`
- [ ] Click play icon (▶) to trigger
- [ ] Confirm manual trigger
- [ ] Watch deployment logs:
  - [ ] `docker-compose up -d` starts services
  - [ ] Services initialize
  - [ ] Health checks run (curl /health)
- [ ] Job completes successfully ✓

### Verify Deployment
- [ ] List running containers: `docker ps`
  - [ ] See `catalog-*` and `cart-*` containers
  - [ ] Containers in "Up" state
- [ ] Check logs: `docker-compose logs -f`
- [ ] Test endpoints:
  ```bash
  curl http://localhost:5001/health    # Catalog
  curl http://localhost:5002/health    # Cart
  ```
- [ ] Both return HTTP 200 OK with health data

## Phase 8: Infrastructure (Terraform)

### Plan Infrastructure
- [ ] Return to pipeline on master
- [ ] Find job `infra:plan:mr`
- [ ] Click play icon (▶)
- [ ] Confirm trigger
- [ ] Watch logs:
  - [ ] `terraform init` completes
  - [ ] `terraform plan` shows resource diff
- [ ] Review plan output for expected changes

### Apply Infrastructure (Optional)
- [ ] Find job `infra:apply:master`
- [ ] Click play icon (▶)
- [ ] Confirm trigger
- [ ] Watch logs:
  - [ ] `terraform init`
  - [ ] `terraform apply -auto-approve`
  - [ ] Resources created/updated
- [ ] Job completes ✓

## Phase 9: Verification & Documentation

### Pipeline Functionality
- [ ] MR pipeline blocks merge if tests fail (try reverting a test)
- [ ] Master pipeline runs automatically after merge
- [ ] Manual deploy trigger works
- [ ] Artifacts (test reports, terraform plans) downloadable
- [ ] Environment variables available in jobs

### Documentation Review
- [ ] Team has read `GETTING_STARTED.md`
- [ ] Team understands branching strategy (GitLab Flow)
- [ ] Team knows how to:
  - [ ] Create feature branch
  - [ ] Open MR
  - [ ] Trigger deploy
  - [ ] View pipeline logs
  - [ ] Debug failures

### Monitoring & Alerts (Optional)
- [ ] Set up Slack/email notifications (see `ADVANCED_CI_CD_FEATURES.md`)
- [ ] Configure pipeline failure alerts
- [ ] Test notification delivery

## Phase 10: Final Acceptance

### Code Quality
- [ ] `.gitlab-ci.yml` is clean and well-commented
- [ ] No sensitive data in config files
- [ ] All variables properly masked in GitLab
- [ ] `.gitignore` prevents accidental commits

### Performance
- [ ] Build job completes in < 5 minutes (first run may be slower)
- [ ] Test job completes in < 10 minutes
- [ ] Docker image build completes in < 5 minutes
- [ ] Deploy job completes in < 2 minutes

### Coverage
- [ ] Path-based filtering works (jobs skip unchanged services)
- [ ] Both services build and test successfully
- [ ] Terraform plan/apply works without errors
- [ ] Docker Compose deployment succeeds

### Documentation
- [ ] `GETTING_STARTED.md` is clear and actionable
- [ ] `CI_CD_PIPELINE_GUIDE.md` covers all scenarios
- [ ] Troubleshooting section helps resolve issues
- [ ] Examples are accurate and tested

## Sign-Off

- [ ] All items checked
- [ ] Team briefed on CI/CD process
- [ ] Runner maintenance plan defined
- [ ] Backup/recovery procedures documented
- [ ] Ready for production use

**Date Completed**: _______________

**Completed By**: _______________

**Team Lead Approval**: _______________

---

## Quick Reference Commands

```bash
# View pipeline status
gitlab-runner list

# Start runner
gitlab-runner run

# Validate locally (requires gitlab-runner)
gitlab-runner exec docker build:catalog

# View Docker logs
docker-compose logs -f

# Restart services
docker-compose restart

# Clean up (⚠️ removes data)
docker-compose down -v
```

---

## Troubleshooting Quick Links

- **Runner not appearing**: See `GETTING_STARTED.md` → Troubleshooting
- **Pipeline stuck**: Check `.gitlab-ci.yml` syntax in GitLab UI
- **Jobs timing out**: Increase timeout or optimize execution
- **Docker issues**: Verify daemon running and socket accessible
- **Test failures**: Review job logs for detailed error messages

---

**Status**: Implementation Checklist  
**Last Updated**: 2026-09-03  
**Target Framework**: .NET 9  
