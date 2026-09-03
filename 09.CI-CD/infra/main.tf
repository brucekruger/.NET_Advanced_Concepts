terraform {
  required_version = ">= 1.0"
  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 3.0"
    }
  }
}

provider "docker" {
  # Connects to local Docker daemon
  # For remote, use: host = "unix:///var/run/docker.sock" or tcp://...
}

# ============================================================================
# Local Docker Compose Deployment
# ============================================================================

# Note: This is a simplified example. In production, you'd typically:
# 1. Use docker-compose provider or shell provisioner to manage compose
# 2. For cloud deployments, replace with Azure Container Apps, AKS, ECS, etc.

# Example: Pull images (if using registry)
resource "docker_image" "catalog_service" {
  name = "catalog-service:latest"
  build {
    context    = "../"
    dockerfile = "CatalogService.Api/Dockerfile"
  }
}

resource "docker_image" "cart_service" {
  name = "cart-service:latest"
  build {
    context    = "../"
    dockerfile = "CartService.Api/Dockerfile"
  }
}

# ============================================================================
# Example: SQL Server Container (for reference)
# ============================================================================

resource "docker_image" "mssql" {
  name = "mcr.microsoft.com/mssql/server:2022-latest"
}

resource "docker_image" "redis" {
  name = "redis:7-alpine"
}

# ============================================================================
# Outputs
# ============================================================================

output "catalog_service_image" {
  value       = docker_image.catalog_service.repo_digest
  description = "Catalog Service Docker image digest"
}

output "cart_service_image" {
  value       = docker_image.cart_service.repo_digest
  description = "Cart Service Docker image digest"
}
