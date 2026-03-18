output "resource_group_name" {
  description = "Resource group name"
  value       = azurerm_resource_group.whycespace.name
}

output "aks_cluster_name" {
  description = "AKS cluster name"
  value       = azurerm_kubernetes_cluster.whycespace.name
}

output "aks_kube_config" {
  description = "AKS kubeconfig"
  value       = azurerm_kubernetes_cluster.whycespace.kube_config_raw
  sensitive   = true
}

output "postgres_fqdn" {
  description = "PostgreSQL server FQDN"
  value       = azurerm_postgresql_flexible_server.whycespace.fqdn
}

output "redis_hostname" {
  description = "Redis cache hostname"
  value       = azurerm_redis_cache.whycespace.hostname
}

output "redis_primary_key" {
  description = "Redis primary access key"
  value       = azurerm_redis_cache.whycespace.primary_access_key
  sensitive   = true
}
