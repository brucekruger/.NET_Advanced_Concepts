# Variables for infrastructure configuration

variable "docker_host" {
  description = "Docker host URL (local or remote)"
  type        = string
  default     = "unix:///var/run/docker.sock"
}

variable "catalog_service_port" {
  description = "Port for Catalog Service API"
  type        = number
  default     = 5001
}

variable "cart_service_port" {
  description = "Port for Cart Service API"
  type        = number
  default     = 5002
}

variable "sql_server_port" {
  description = "Port for SQL Server"
  type        = number
  default     = 1433
}

variable "redis_port" {
  description = "Port for Redis"
  type        = number
  default     = 6379
}

variable "environment" {
  description = "Environment name (development, staging, production)"
  type        = string
  default     = "development"
}

variable "sql_password" {
  description = "SQL Server SA password"
  type        = string
  sensitive   = true
  default     = "YourStrongPassword123!"
}
