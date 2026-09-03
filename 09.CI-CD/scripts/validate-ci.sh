#!/bin/bash
# Validate GitLab CI/CD Configuration
# This script performs basic checks on the .gitlab-ci.yml file

echo "======================================"
echo "GitLab CI/CD Validation"
echo "======================================"
echo ""

# Check if .gitlab-ci.yml exists
if [ ! -f ".gitlab-ci.yml" ]; then
    echo "✗ .gitlab-ci.yml not found"
    exit 1
fi

echo "✓ .gitlab-ci.yml found"
echo ""

# Basic YAML validation (if yq or similar is available)
if command -v yq &> /dev/null; then
    if yq eval . .gitlab-ci.yml > /dev/null 2>&1; then
        echo "✓ .gitlab-ci.yml is valid YAML"
    else
        echo "✗ .gitlab-ci.yml has YAML syntax errors"
        exit 1
    fi
else
    echo "⚠ yq not installed - skipping YAML validation"
    echo "  Install: brew install yq (macOS) or apt-get install yq (Linux)"
fi

echo ""

# Check for required stages
echo "Checking for required stages..."
if grep -q "^stages:" .gitlab-ci.yml; then
    echo "✓ Stages defined"
else
    echo "⚠ No stages defined (may be implicit)"
fi

# Check for required jobs
echo ""
echo "Checking for required jobs..."

REQUIRED_JOBS=(
    "build:catalog"
    "build:cart"
    "test:catalog"
    "test:cart"
    "build:all:master"
    "test:all:master"
)

for job in "${REQUIRED_JOBS[@]}"; do
    if grep -q "^  ${job}:" .gitlab-ci.yml || grep -q "^${job}:" .gitlab-ci.yml; then
        echo "  ✓ ${job}"
    else
        echo "  ⚠ ${job} not found"
    fi
done

echo ""
echo "Checking for Docker and Terraform jobs..."

if grep -q "package:docker:master" .gitlab-ci.yml; then
    echo "  ✓ Docker packaging job found"
fi

if grep -q "infra:plan:mr" .gitlab-ci.yml && grep -q "infra:apply:master" .gitlab-ci.yml; then
    echo "  ✓ Terraform jobs found"
fi

echo ""
echo "Checking project structure..."

REQUIRED_DIRS=(
    "infra"
    "ci"
    "scripts"
)

for dir in "${REQUIRED_DIRS[@]}"; do
    if [ -d "$dir" ]; then
        echo "  ✓ $dir/ directory found"
    else
        echo "  ⚠ $dir/ directory not found"
    fi
done

echo ""
echo "Checking Dockerfiles..."

if [ -f "CatalogService.Api/Dockerfile" ]; then
    echo "  ✓ CatalogService.Api/Dockerfile"
else
    echo "  ✗ CatalogService.Api/Dockerfile not found"
fi

if [ -f "CartService.Api/Dockerfile" ]; then
    echo "  ✓ CartService.Api/Dockerfile"
else
    echo "  ✗ CartService.Api/Dockerfile not found"
fi

echo ""
echo "Checking .gitignore..."

if [ -f ".gitignore" ]; then
    echo "  ✓ .gitignore found"
    if grep -q "infra/.terraform/" .gitignore; then
        echo "    ✓ Terraform files ignored"
    fi
else
    echo "  ⚠ .gitignore not found"
fi

echo ""
echo "======================================"
echo "Validation Complete"
echo "======================================"
echo ""
echo "Next: Push to GitLab and monitor the pipeline:"
echo "  git push -u origin feature/09.CI-CD"
echo "  Then open a Merge Request to master"
echo ""
