# GitLab Runner Setup Script for 09.CI-CD Project (Windows)
# This script installs and registers a GitLab Runner with Docker executor

Write-Host "======================================"
Write-Host "GitLab Runner Setup for 09.CI-CD"
Write-Host "======================================"
Write-Host ""

# Check if gitlab-runner is installed
$runnerPath = where.exe gitlab-runner 2>$null
if (-not $runnerPath) {
    Write-Host "GitLab Runner not found. Please install it manually:" -ForegroundColor Yellow
    Write-Host "1. Download from: https://docs.gitlab.com/runner/install/windows.html"
    Write-Host "2. Or use Chocolatey: choco install gitlab-runner"
    Write-Host "3. Or use Windows Package Manager: winget install GitLab.Runner"
    exit 1
} else {
    Write-Host "✓ GitLab Runner is already installed at: $runnerPath" -ForegroundColor Green
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Go to your GitLab project: Settings → CI/CD → Runners"
Write-Host "2. Copy the registration token"
Write-Host "3. Run PowerShell as Administrator and execute:"
Write-Host ""
Write-Host "    gitlab-runner register" -ForegroundColor Yellow
Write-Host "      --url https://gitlab.com/" -ForegroundColor Yellow
Write-Host "      --registration-token <TOKEN>" -ForegroundColor Yellow
Write-Host "      --executor docker" -ForegroundColor Yellow
Write-Host "      --docker-image mcr.microsoft.com/dotnet/sdk:9.0" -ForegroundColor Yellow
Write-Host "      --docker-volumes /var/run/docker.sock:/var/run/docker.sock" -ForegroundColor Yellow
Write-Host "      --description 'Docker Runner for 09.CI-CD'" -ForegroundColor Yellow
Write-Host "      --tag-list 'docker,dotnet9'" -ForegroundColor Yellow
Write-Host "      --run-untagged" -ForegroundColor Yellow
Write-Host ""
Write-Host "4. Verify the runner is registered:"
Write-Host "    gitlab-runner list" -ForegroundColor Yellow
Write-Host ""
Write-Host "5. Install as service (optional, for auto-start):"
Write-Host "    gitlab-runner install --user SYSTEM --password <PASSWORD>" -ForegroundColor Yellow
Write-Host "    gitlab-runner start" -ForegroundColor Yellow
Write-Host ""
