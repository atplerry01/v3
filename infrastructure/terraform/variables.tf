variable "environment" {
  description = "Deployment environment"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "production"], var.environment)
    error_message = "Environment must be dev, staging, or production."
  }
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "uksouth"
}

variable "node_count" {
  description = "AKS node count"
  type        = number
  default     = 3
}

variable "node_size" {
  description = "AKS node VM size"
  type        = string
  default     = "Standard_D4s_v3"
}

variable "postgres_admin_user" {
  description = "PostgreSQL admin username"
  type        = string
  default     = "whyce_admin"
  sensitive   = true
}

variable "postgres_admin_password" {
  description = "PostgreSQL admin password"
  type        = string
  sensitive   = true
}

variable "postgres_storage_mb" {
  description = "PostgreSQL storage in MB"
  type        = number
  default     = 32768
}

variable "postgres_sku" {
  description = "PostgreSQL SKU"
  type        = string
  default     = "GP_Standard_D2s_v3"
}

variable "redis_capacity" {
  description = "Redis cache capacity"
  type        = number
  default     = 1
}

variable "redis_sku" {
  description = "Redis cache SKU"
  type        = string
  default     = "Standard"
}
