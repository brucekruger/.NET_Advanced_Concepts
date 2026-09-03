# Advanced GitLab CI/CD Features (Optional Reference)
# This file documents optional enhancements to the main .gitlab-ci.yml

## 1. Azure Cloud Deployment

When ready to deploy to Azure Container Apps or AKS:

```yaml
deploy:azure:master:
  stage: deploy
  image: mcr.microsoft.com/azure-cli:latest
  only:
    - master
  needs:
    - package:docker:master
  variables:
    AZURE_RESOURCE_GROUP: my-resource-group
    AZURE_LOCATION: eastus
  script:
    - az login --service-principal -u $AZURE_CLIENT_ID -p $AZURE_CLIENT_SECRET --tenant $AZURE_TENANT_ID
    - echo "Deploying to Azure Container Apps..."
    - az containerapp up 
        --name catalog-service 
        --resource-group $AZURE_RESOURCE_GROUP 
        --image $REGISTRY_IMAGE/catalog-service:latest 
        --environment myenv 
        --location $AZURE_LOCATION
    - az containerapp up 
        --name cart-service 
        --resource-group $AZURE_RESOURCE_GROUP 
        --image $REGISTRY_IMAGE/cart-service:latest 
        --environment myenv 
        --location $AZURE_LOCATION
  when: manual
  environment:
    name: production
    url: https://catalog-service.azurewebsites.net
```

**Prerequisites:**
- Azure subscription
- Service Principal with credentials
- CI/CD Variables:
  - `AZURE_CLIENT_ID`
  - `AZURE_CLIENT_SECRET` (masked)
  - `AZURE_TENANT_ID`

---

## 2. Push to GitLab Container Registry

Enhance the `package:docker:master` job:

```yaml
package:docker:master:
  stage: package
  image: docker:latest
  only:
    - master
  needs:
    - test:all:master
  before_script:
    - echo "$CI_REGISTRY_PASSWORD" | docker login -u $CI_REGISTRY_USER --password-stdin $CI_REGISTRY
  script:
    - echo "Building and pushing Docker images..."
    - docker build -t $REGISTRY_IMAGE/catalog-service:$CI_COMMIT_SHA -t $REGISTRY_IMAGE/catalog-service:latest -f CatalogService.Api/Dockerfile .
    - docker build -t $REGISTRY_IMAGE/cart-service:$CI_COMMIT_SHA -t $REGISTRY_IMAGE/cart-service:latest -f CartService.Api/Dockerfile .
    - docker push $REGISTRY_IMAGE/catalog-service:$CI_COMMIT_SHA
    - docker push $REGISTRY_IMAGE/catalog-service:latest
    - docker push $REGISTRY_IMAGE/cart-service:$CI_COMMIT_SHA
    - docker push $REGISTRY_IMAGE/cart-service:latest
  after_script:
    - docker logout $CI_REGISTRY
```

**Enable:** Set CI/CD Variables (auto-available in GitLab):
- `$CI_REGISTRY`
- `$CI_REGISTRY_IMAGE`
- `$CI_REGISTRY_USER`
- `$CI_REGISTRY_PASSWORD`

---

## 3. Code Quality & Coverage Reports

Add SonarQube or code coverage:

```yaml
analyze:quality:master:
  stage: test
  image: $DOTNET_SDK_IMAGE
  only:
    - master
  needs:
    - build:all:master
  script:
    - apt-get update && apt-get install -y java-11-openjdk
    - dotnet tool install --global dotnet-sonarscanner
    - export PATH="$PATH:/root/.dotnet/tools"
    - dotnet sonarscanner begin /k:09-ci-cd /d:sonar.host.url=$SONAR_HOST_URL /d:sonar.login=$SONAR_TOKEN
    - dotnet build 09.CI-CD.sln -c Release
    - dotnet sonarscanner end /d:sonar.login=$SONAR_TOKEN
  allow_failure: true
  when: manual
```

---

## 4. Performance Testing (Load Tests)

```yaml
test:performance:master:
  stage: test
  image: grafana/k6:latest
  only:
    - master
  needs:
    - deploy:docker-compose:master
  script:
    - echo "Running load tests..."
    - k6 run scripts/load-test.js --vus 10 --duration 30s
  artifacts:
    reports:
      performance: performance-results.json
  allow_failure: true
  when: manual
```

Create `scripts/load-test.js`:
```javascript
import http from 'k6/http';
import { check } from 'k6';

export default function () {
  let res = http.get('http://localhost:5001/api/v1/products');
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
}
```

---

## 5. Security Scanning (SAST / DAST)

### SAST (Static Application Security Testing)

```yaml
scan:sast:master:
  stage: test
  image: returntocorp/semgrep:latest
  only:
    - master
  script:
    - semgrep --config=p/owasp-top-ten --config=p/cwe-top-25 --json -o sast-report.json . || true
  artifacts:
    reports:
      sast: sast-report.json
  allow_failure: true
```

### DAST (Dynamic Application Security Testing)

```yaml
scan:dast:master:
  stage: test
  image: zaproxy/zaproxy:latest
  only:
    - master
  needs:
    - deploy:docker-compose:master
  script:
    - echo "Running DAST scan..."
    - zap-baseline.py -t http://localhost:5001 -r dast-report.html || true
  artifacts:
    paths:
      - dast-report.html
  allow_failure: true
  when: manual
```

---

## 6. Scheduled Nightly Builds

```yaml
nightly-build:
  stage: build
  image: $DOTNET_SDK_IMAGE
  script:
    - echo "Running nightly build..."
    - dotnet restore 09.CI-CD.sln
    - dotnet build 09.CI-CD.sln -c Release
  only:
    - schedules
```

Set up in GitLab:
- **Settings → CI/CD → Schedules**
- **New schedule**
- **Cron**: `0 2 * * *` (2 AM daily)

---

## 7. Slack / Email Notifications

```yaml
notify:success:
  stage: .post
  image: alpine:latest
  script:
    - apk add --no-cache curl
    - |
      curl -X POST $SLACK_WEBHOOK_URL \
        -H 'Content-Type: application/json' \
        -d "{
          \"text\": \"✅ Pipeline #$CI_PIPELINE_ID succeeded on $CI_COMMIT_BRANCH\",
          \"blocks\": [{
            \"type\": \"section\",
            \"text\": {\"type\": \"mrkdwn\", \"text\": \"*Deployment Successful*\n$CI_COMMIT_MESSAGE\n<$CI_PIPELINE_URL|View Pipeline>\"}
          }]
        }"
  only:
    - master
  when: on_success
```

**Setup:** Add CI/CD Variable `SLACK_WEBHOOK_URL` (masked)

---

## 8. Database Migrations

```yaml
migrate:database:master:
  stage: infra
  image: $DOTNET_SDK_IMAGE
  only:
    - master
  script:
    - echo "Running Entity Framework migrations..."
    - dotnet tool install --global dotnet-ef
    - export PATH="$PATH:/root/.dotnet/tools"
    - cd CatalogService.Api
    - dotnet ef database update --startup-project CatalogService.Api.csproj
  when: manual
```

---

## 9. Artifact Publishing (NuGet)

```yaml
publish:nuget:master:
  stage: package
  image: $DOTNET_SDK_IMAGE
  only:
    - master
  script:
    - echo "Publishing to NuGet..."
    - dotnet pack CatalogService.Domain/CatalogService.Domain.csproj -c Release -o /tmp/packages
    - dotnet nuget push /tmp/packages/*.nupkg -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json
  when: manual
```

---

## 10. Multi-Environment Deployments

```yaml
deploy:staging:
  stage: deploy
  environment:
    name: staging
    url: https://staging-catalog.example.com
  only:
    - develop
  script:
    - echo "Deploying to staging..."

deploy:production:
  stage: deploy
  environment:
    name: production
    url: https://catalog.example.com
  only:
    - master
  script:
    - echo "Deploying to production..."
  when: manual
  requirements:
    - for_each: ['approval1', 'approval2']
      required: true
```

---

## Implementation Priority

| Feature | Difficulty | Timing |
|---------|-----------|--------|
| Base CI/CD (build/test) | Easy | Now (Phase 1) |
| Docker packaging | Medium | Phase 1 |
| Terraform IaC | Medium | Phase 2 |
| Docker Compose deploy | Medium | Phase 2 |
| Azure deployment | Hard | Stretch (Phase 3) |
| Code quality / coverage | Hard | Stretch |
| Security scanning | Hard | Stretch |
| Performance testing | Very Hard | Stretch |

---

## Debugging Tips

### Enable Debug Logging

```yaml
build:catalog:
  script:
    - echo "Debug mode enabled"
    - set -x  # or for PowerShell: $DebugPreference = "Continue"
    - dotnet build ...
```

### Inspect Job Artifacts

```bash
# Download artifacts from failed job
cd path/to/project
git remote add gitlab https://gitlab.com/YOUR_PROJECT
git fetch gitlab
gitlab-runner artifacts-downloader --id <JOB_ID> --token <TOKEN> --output artifacts/
```

### Test Pipeline Locally

```bash
# Requires gitlab-runner
gitlab-runner exec docker build:catalog
gitlab-runner exec docker test:catalog
```

---

## References

- [GitLab CI/CD Variables](https://docs.gitlab.com/ee/ci/variables/)
- [GitLab Auto DevOps](https://docs.gitlab.com/ee/topics/autodevops/)
- [SAST Security Scanning](https://docs.gitlab.com/ee/user/application_security/sast/)
- [Terraform GitLab Provider](https://registry.terraform.io/providers/gitlabhq/gitlab/latest)

---

**Status**: Advanced features reference  
**Use**: Copy/paste sections into `.gitlab-ci.yml` as needed  
