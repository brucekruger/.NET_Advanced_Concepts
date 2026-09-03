#!/bin/bash
# GitLab Runner Setup Script for 09.CI-CD Project
# This script installs and registers a GitLab Runner with Docker executor

set -e

echo "======================================"
echo "GitLab Runner Setup for 09.CI-CD"
echo "======================================"
echo ""

# Check if gitlab-runner is installed
if ! command -v gitlab-runner &> /dev/null; then
    echo "GitLab Runner not found. Installing..."

    # Detect OS
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux
        curl -L https://packages.gitlab.com/install/repositories/runner/gitlab-runner/script.deb.sh | bash
        apt-get install gitlab-runner
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        brew install gitlab-runner
    elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
        # Windows
        echo "Please download and install GitLab Runner from:"
        echo "https://docs.gitlab.com/runner/install/windows.html"
        exit 1
    fi
else
    echo "✓ GitLab Runner is already installed"
fi

echo ""
echo "Next steps:"
echo "1. Go to your GitLab project: Settings → CI/CD → Runners"
echo "2. Copy the registration token"
echo "3. Run the following command (replace <TOKEN> with your token):"
echo ""
echo "    gitlab-runner register"
echo "      --url https://gitlab.com/"
echo "      --registration-token <TOKEN>"
echo "      --executor docker"
echo "      --docker-image mcr.microsoft.com/dotnet/sdk:10.0"
echo "      --docker-volumes /var/run/docker.sock:/var/run/docker.sock"
echo "      --description 'Docker Runner for 09.CI-CD'"
echo "      --tag-list 'docker,dotnet10'"
echo "      --run-untagged"
echo ""
echo "4. Verify the runner is registered:"
echo "    gitlab-runner list"
echo ""
echo "5. Start the runner:"
echo "    gitlab-runner run"
echo ""
