# GitLab CI/CD Implementation - Complete Package

## 🎯 Overview

This package contains a **complete, production-ready GitLab CI/CD pipeline** implementation for the **09.CI-CD** microservices project (.NET 9). It follows **GitLab Flow** branching strategy and implements both **Home Task 1 (CI)** and **Home Task 2 (CD + IaC)** requirements.

**Target Services:**
- CatalogService.Api
- CartService.Api

**Infrastructure:**
- SQL Server, Redis, RabbitMQ, Keycloak (via docker-compose)
- Terraform for IaC provisioning

---

## 📦 What's Included

### Core CI/CD Files

```
✅ .gitlab-ci.yml              Main pipeline definition (7 stages, 13+ jobs)
✅ ci/build-test.yml           Reusable build/test templates
✅ ci/deploy.yml               Reusable deploy templates
✅ .gitignore                  Prevent secrets/artifacts from committing
```

### Infrastructure as Code

```
✅ infra/main.tf                Terraform Docker resources
✅ infra/variables.tf           Terraform input variables
✅ infra/terraform.tfvars.example Example terraform values
```

### Helper Scripts

```
✅ scripts/setup-gitlab-runner.sh    Linux/macOS runner installation
✅ scripts/setup-gitlab-runner.ps1   Windows runner installation
✅ scripts/validate-ci.sh            Validate pipeline configuration
```

### Comprehensive Documentation

| Document | Purpose | Audience |
|----------|---------|----------|
| **GETTING_STARTED.md** | Step-by-step setup guide | Everyone (start here!) |
| **CI_CD_PIPELINE_GUIDE.md** | Detailed pipeline reference | DevOps / Tech Leads |
| **ADVANCED_CI_CD_FEATURES.md** | Optional extensions (Azure, SonarQube, etc.) | Advanced users |
| **CI_CD_IMPLEMENTATION_SUMMARY.md** | What was built and why | Project managers |
| **CI_CD_IMPLEMENTATION_CHECKLIST.md** | Phase-by-phase verification | QA / Team leads |
| **QUICK_REFERENCE.md** | Commands & common tasks | Daily users |

---

## 🚀 Quick Start (5 Minutes)

### Prerequisites
- Git
- .NET 9 SDK
- Docker
- GitLab account (gitlab.com or private)

### Steps

1. **Read the setup guide** (2 min)
   ```bash
   # Open and review
   cat GETTING_STARTED.md
   ```

2. **Push to GitLab** (1 min)
   ```bash
   git remote add gitlab https://gitlab.com/YOUR_GROUP/09.CI-CD.git
   git push -u gitlab feature/09.CI-CD
   git push gitlab master
   ```

3. **Install GitLab Runner** (1 min)
   - Windows: `choco install gitlab-runner`
   - macOS: `brew install gitlab-runner`
   - Linux: `apt-get install gitlab-runner`

4. **Register Runner** (1 min)
   - Get token from GitLab: Settings → CI/CD → Runners
   - Run: `gitlab-runner register --url https://gitlab.com/ --executor docker`

5. **Test Pipeline** (as needed)
   - Open MR from feature branch → watch pipeline run ✅

For detailed steps, see **GETTING_STARTED.md**.

---

## 🔄 Pipeline Architecture

### Merge Request Pipeline (Blocks Merge)

```
feature/* branch created
         ↓
    MR opened targeting master
         ↓
Path-based job detection (rules: changes)
         ├─ CatalogService files → build:catalog, test:catalog
         ├─ CartService files   → build:cart, test:cart
         ├─ Infra files        → infra:plan:mr
         └─ Both → both jobs
         ↓
All jobs must PASS before merge
         ↓
✅ Merge enabled when pipeline succeeds
```

### Master Pipeline (Continuous Deployment)

```
MR merged to master
         ↓
Automatic pipeline starts
         ├─ Stage: build    → build:all:master (full solution)
         ├─ Stage: test     → test:all:master (all tests)
         ├─ Stage: package  → package:docker:master (build images)
         ├─ Stage: infra    → infra:apply:master (manual trigger)
         └─ Stage: deploy   → deploy:docker-compose:master (manual)
         ↓
🟢 Green = Ready to deploy
🟡 Manual = Click to trigger deploy
```

---

## 📊 Implemented Features

### Home Task 1 — CI Pipeline ✅

| Requirement | Implementation |
|-------------|-----------------|
| Choose branching strategy | GitLab Flow (feature/* → master) |
| Build changed services | `build:catalog`, `build:cart` with path filtering |
| Run service tests | `test:catalog`, `test:cart` with junit reporting |
| Trigger on MR creation | `merge_request_event` in `.gitlab-ci.yml` |
| Trigger on merge | `push` to `master` branch |
| Quality gate (100% pass) | Protected branch + "Pipelines must succeed" rule |

### Home Task 2 — CD + IaC ✅

| Requirement | Implementation |
|-------------|-----------------|
| CD for services | Docker Compose (`deploy:docker-compose:master` job) |
| Cloud/container deployment | Docker containers on GitLab Runner |
| Infrastructure provisioning | Terraform (`infra/main.tf`, `infra/variables.tf`) |
| Automated IaC in pipeline | `infra:plan:mr` and `infra:apply:master` jobs |

### Additional ✅

| Feature | Status |
|---------|--------|
| Path-based job filtering | ✅ Implemented with `rules: changes` |
| Test result reporting | ✅ JUnit XML artifacts |
| Caching | ✅ NuGet cache per branch |
| Manual deploy trigger | ✅ Safety via `when: manual` |
| Smoke tests | ✅ Health endpoint validation |
| Documentation | ✅ 6+ guides included |
| Helper scripts | ✅ Windows/Linux/macOS support |

---

## 📁 File Structure

```
09.CI-CD/
├── .gitlab-ci.yml                        ← MAIN PIPELINE (start here!)
├── .gitignore                            ← Prevent secrets from committing
├── ci/
│   ├── build-test.yml                    ← Optional templates
│   └── deploy.yml                        ← Optional templates
├── infra/
│   ├── main.tf                           ← Terraform resources
│   ├── variables.tf                      ← Terraform variables
│   └── terraform.tfvars.example          ← Example values
├── scripts/
│   ├── setup-gitlab-runner.sh            ← Linux/macOS setup
│   ├── setup-gitlab-runner.ps1           ← Windows setup
│   └── validate-ci.sh                    ← Validation tool
│
├── [DOCUMENTATION]
├── GETTING_STARTED.md                    ← 👈 START HERE (step-by-step)
├── CI_CD_PIPELINE_GUIDE.md               ← Comprehensive reference
├── ADVANCED_CI_CD_FEATURES.md            ← Optional extensions
├── CI_CD_IMPLEMENTATION_SUMMARY.md       ← What was built
├── CI_CD_IMPLEMENTATION_CHECKLIST.md     ← Verification steps
├── QUICK_REFERENCE.md                    ← Commands cheat sheet
├── CI_CD_README.md                       ← This file
│
├── [EXISTING PROJECT FILES]
├── CatalogService.Api/
│   └── Dockerfile
├── CartService.Api/
│   └── Dockerfile
├── docker-compose.yml
├── 09.CI-CD.sln
├── IMPLEMENTATION_PLAN.md                ← Original design
└── README.md                             ← Project overview
```

---

## 🎓 Learning Path

**For Beginners (First Time):**
1. Read: `GETTING_STARTED.md` (Step 1-5)
2. Do: Follow setup steps, push code, open MR
3. Watch: Pipeline run in GitLab UI
4. Deploy: Manually trigger deploy job

**For Intermediate Users:**
1. Read: `CI_CD_PIPELINE_GUIDE.md` (understand architecture)
2. Review: `.gitlab-ci.yml` comments
3. Modify: Customize paths, timeouts, stages
4. Debug: Check job logs when issues arise

**For Advanced Users:**
1. Read: `ADVANCED_CI_CD_FEATURES.md` (optional extensions)
2. Extend: Add Azure, SonarQube, performance tests
3. Optimize: Caching, parallelization, cost reduction
4. Integrate: Slack alerts, approval workflows, environments

---

## 🛠️ Common Commands

### Git & GitLab

```bash
# Create feature branch and push
git checkout -b feature/my-feature master
git push -u gitlab feature/my-feature

# Open MR in GitLab UI (link in terminal output)

# After merge, cleanup
git checkout master
git pull gitlab master
git branch -d feature/my-feature
```

### GitLab Runner

```bash
# Install
choco install gitlab-runner  # Windows
brew install gitlab-runner    # macOS
apt-get install gitlab-runner # Linux

# Register (get token from GitLab UI)
gitlab-runner register --url https://gitlab.com/ --executor docker

# Start
gitlab-runner run           # Foreground (dev)
gitlab-runner install       # Install as service (prod)
gitlab-runner start         # Start service

# Verify
gitlab-runner list
```

### Docker & Compose

```bash
# View running containers
docker ps

# View logs
docker-compose logs -f [service-name]

# Test endpoints
curl http://localhost:5001/health    # Catalog
curl http://localhost:5002/health    # Cart

# Deploy
docker-compose up -d

# Cleanup
docker-compose down -v
```

### Terraform

```bash
cd infra/

# Plan changes
terraform plan -out=tfplan

# Apply
terraform apply tfplan

# Destroy
terraform destroy
```

For more commands, see **QUICK_REFERENCE.md**.

---

## ⚙️ Configuration

### GitLab Project Settings

1. **Protected Branches** (Settings → Repository)
   ```
   Branch: master
   ✓ Require pipelines to succeed
   ✓ Require approvals (optional)
   ```

2. **CI/CD Variables** (Settings → CI/CD)
   ```
   DOCKER_REGISTRY_PASSWORD     (masked, optional)
   SONAR_TOKEN                  (masked, optional)
   ```

3. **Runners** (Settings → CI/CD → Runners)
   ```
   Status: Online (green circle)
   Executor: Docker
   Tags: docker,dotnet9
   ```

### Local Configuration

1. **Docker** (must have socket access)
   ```bash
   # Linux
   sudo usermod -aG docker $USER

   # Windows: Use Docker Desktop with WSL 2
   ```

2. **Git** (configure user)
   ```bash
   git config --global user.name "Your Name"
   git config --global user.email "your.email@example.com"
   ```

3. **.gitignore** (already included)
   ```
   Prevents: terraform.tfstate, .env, *.pem, secrets/
   ```

---

## 🔍 Troubleshooting

### Pipeline Not Triggering

**Symptom**: Open MR but no pipeline shows  
**Solutions**:
1. Check runner is online: `gitlab-runner list`
2. Validate YAML: GitLab UI → CI/CD → Validate
3. Check `.gitlab-ci.yml` exists in branch
4. Refresh browser (may be cached)

### Runner Shows "Offline"

**Symptom**: Runner registered but shows offline in GitLab  
**Solutions**:
1. Verify runner is running: `gitlab-runner run`
2. Check firewall/proxy blocking access
3. Re-register runner (get fresh token)
4. Check runner logs: `gitlab-runner --debug run`

### Docker Build Fails

**Symptom**: `docker build` job fails  
**Solutions**:
1. Check Docker daemon running: `docker ps`
2. Check socket permissions: `ls -la /var/run/docker.sock`
3. Free up disk space: `docker system prune -a`
4. View full error in GitLab job logs

### Tests Timeout

**Symptom**: Test job hits timeout after 5+ minutes  
**Solutions**:
1. Increase timeout in `.gitlab-ci.yml`: `timeout: 30 minutes`
2. Run tests locally to measure: `dotnet test -c Release`
3. Parallelize tests across multiple jobs
4. Optimize slow tests (database setup, etc.)

For more help, see **CI_CD_PIPELINE_GUIDE.md → Troubleshooting**.

---

## ✅ Success Criteria

You'll know it's working when:

- [ ] GitLab project created and branch pushed
- [ ] GitLab Runner installed and online (green in UI)
- [ ] Master branch protected (requires pipeline to pass)
- [ ] Feature MR opens → Pipeline runs → All jobs pass ✅
- [ ] MR can merge after green pipeline
- [ ] Post-merge → Master pipeline runs automatically
- [ ] Deploy job can be manually triggered
- [ ] Services start: `docker ps` shows containers
- [ ] Health endpoints respond: `curl http://localhost:5001/health`
- [ ] Team can create features, merge with confidence

See **CI_CD_IMPLEMENTATION_CHECKLIST.md** for detailed verification.

---

## 📞 Support & Resources

### Documentation Files

| File | Purpose |
|------|---------|
| `GETTING_STARTED.md` | Step-by-step setup (start here!) |
| `CI_CD_PIPELINE_GUIDE.md` | Architecture & how-to guide |
| `ADVANCED_CI_CD_FEATURES.md` | Optional enhancements |
| `QUICK_REFERENCE.md` | Commands & troubleshooting |
| `CI_CD_IMPLEMENTATION_CHECKLIST.md` | Verification steps |

### External Links

- 📖 [GitLab CI/CD Docs](https://docs.gitlab.com/ee/ci/)
- 🏃 [GitLab Runner Docs](https://docs.gitlab.com/runner/)
- 🐳 [Docker Docs](https://docs.docker.com/)
- 🏗️ [Terraform Docs](https://www.terraform.io/docs/)
- 🔷 [.NET 9 Docs](https://learn.microsoft.com/en-us/dotnet/core/)

---

## 🎯 Next Steps

### Immediate (Day 1)
1. ✅ Read `GETTING_STARTED.md`
2. ✅ Push to GitLab
3. ✅ Install & register GitLab Runner
4. ✅ Create test MR and watch pipeline

### Short Term (Week 1)
1. ✅ Protect master branch
2. ✅ Deploy using docker-compose job
3. ✅ Verify services running
4. ✅ Brief team on workflow

### Medium Term (Month 1)
1. ✅ Integrate with team CI/CD process
2. ✅ Add Slack notifications (optional)
3. ✅ Set up monitoring/alerts
4. ✅ Document team-specific customizations

### Long Term (Optional)
1. ✅ Deploy to Azure Cloud (stretch goal)
2. ✅ Add code quality scanning (SonarQube)
3. ✅ Set up performance testing
4. ✅ Multi-environment pipelines (staging → prod)

---

## 📝 Customization

### Change Service Ports

Edit `.gitlab-ci.yml` or `infra/variables.tf`:
```yaml
# Port mapping
catalog_service_port: 5001  # Default
cart_service_port: 5002     # Default
```

### Add More Services

Duplicate jobs in `.gitlab-ci.yml`:
```yaml
build:inventory:
  stage: build
  rules:
    - changes:
        - 'InventoryService*/**'
  # ... rest of job
```

### Use Cloud Deployment

See `ADVANCED_CI_CD_FEATURES.md` → Azure section.

---

## ⚠️ Important Notes

1. **Secrets**: Never commit passwords/tokens
   - Use CI/CD Variables (masked)
   - `.gitignore` prevents accidents

2. **Runner Security**: Only use trusted runners
   - Limit to specific branches/tags if needed
   - Regularly update runner

3. **Production**: Manual deploy triggers prevent accidents
   - Never auto-deploy to production
   - Require approvals for sensitive stages

4. **Costs**: Docker builds can increase bill
   - Monitor runner usage
   - Clean up old artifacts regularly

---

## 📊 Success Metrics

Track these to ensure healthy CI/CD:

- **Build Success Rate**: Aim for 90%+ (green pipelines)
- **Average Build Time**: < 10 minutes
- **Test Coverage**: Maintain >80% (see advanced features)
- **Deployment Frequency**: Daily or on-demand
- **Mean Time to Deploy (MTTR)**: < 5 minutes
- **Mean Time to Recovery (MTTR)**: < 30 minutes

---

## 🎓 Training Resources

### For Developers
- Start: `GETTING_STARTED.md`
- Practice: Create feature → Open MR → Merge
- Learn: Read `.gitlab-ci.yml` comments

### For DevOps/SRE
- Read: `CI_CD_PIPELINE_GUIDE.md` (deep dive)
- Study: `ADVANCED_CI_CD_FEATURES.md` (extensions)
- Implement: Azure/cloud deployment, monitoring

### For Project Managers
- Review: `CI_CD_IMPLEMENTATION_SUMMARY.md` (what was built)
- Use: `CI_CD_IMPLEMENTATION_CHECKLIST.md` (track progress)
- Monitor: GitLab Pipelines dashboard (status overview)

---

## 💡 Tips & Tricks

### Speed Up First Build
```bash
# Pre-warm cache locally
dotnet restore
dotnet build -c Release
```

### Debug Job Locally
```bash
# Requires gitlab-runner
gitlab-runner exec docker build:catalog
```

### Bypass Pipeline (Emergency Only)
```bash
# ⚠️ Not recommended - protected branch prevents this
git push --force  # Won't work on protected branch
```

### View Runner Logs
```bash
# Foreground
gitlab-runner run

# Journalctl (systemd)
sudo journalctl -u gitlab-runner -f
```

---

## 🏆 Summary

This **complete CI/CD implementation** provides:

✅ **Automated builds** triggered by code changes  
✅ **Comprehensive testing** with junit reporting  
✅ **Quality gates** preventing broken merges  
✅ **Docker packaging** of microservices  
✅ **Infrastructure as Code** with Terraform  
✅ **Safe deployments** with manual triggers  
✅ **Health verification** via smoke tests  
✅ **Well-documented** with 6+ guides  

All following **GitLab Flow** branching strategy for team collaboration.

---

## 📬 Feedback & Improvements

- Found issues? Check troubleshooting section
- Want to extend? See `ADVANCED_CI_CD_FEATURES.md`
- Need customization? Modify `.gitlab-ci.yml` and test locally
- Questions? Consult `QUICK_REFERENCE.md` or `CI_CD_PIPELINE_GUIDE.md`

---

**Implementation Complete** ✅  
**Framework**: .NET 9  
**Platform**: GitLab CI/CD  
**Strategy**: GitLab Flow  
**Date**: 2026-09-03  

👉 **Start here**: [GETTING_STARTED.md](GETTING_STARTED.md)
