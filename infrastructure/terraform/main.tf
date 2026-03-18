terraform {
  required_version = ">= 1.7.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.90"
    }
  }

  backend "azurerm" {
    resource_group_name  = "whycespace-tfstate"
    storage_account_name = "whycespacetfstate"
    container_name       = "tfstate"
    key                  = "whycespace.tfstate"
  }
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "whycespace" {
  name     = "rg-whycespace-${var.environment}"
  location = var.location

  tags = {
    project     = "whycespace"
    environment = var.environment
    managed_by  = "terraform"
  }
}

resource "azurerm_kubernetes_cluster" "whycespace" {
  name                = "aks-whycespace-${var.environment}"
  location            = azurerm_resource_group.whycespace.location
  resource_group_name = azurerm_resource_group.whycespace.name
  dns_prefix          = "whycespace-${var.environment}"

  default_node_pool {
    name       = "system"
    node_count = var.node_count
    vm_size    = var.node_size
  }

  identity {
    type = "SystemAssigned"
  }

  tags = {
    project     = "whycespace"
    environment = var.environment
  }
}

resource "azurerm_postgresql_flexible_server" "whycespace" {
  name                = "psql-whycespace-${var.environment}"
  resource_group_name = azurerm_resource_group.whycespace.name
  location            = azurerm_resource_group.whycespace.location
  version             = "16"

  administrator_login    = var.postgres_admin_user
  administrator_password = var.postgres_admin_password

  storage_mb = var.postgres_storage_mb
  sku_name   = var.postgres_sku

  tags = {
    project     = "whycespace"
    environment = var.environment
  }
}

resource "azurerm_redis_cache" "whycespace" {
  name                = "redis-whycespace-${var.environment}"
  location            = azurerm_resource_group.whycespace.location
  resource_group_name = azurerm_resource_group.whycespace.name
  capacity            = var.redis_capacity
  family              = "C"
  sku_name            = var.redis_sku
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"

  tags = {
    project     = "whycespace"
    environment = var.environment
  }
}
