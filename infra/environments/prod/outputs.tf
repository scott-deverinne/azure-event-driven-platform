output "resource_group_name" {
  description = "Name of the Azure Resource Group."
  value       = module.core.resource_group_name
}

output "resource_group_location" {
  description = "Location of the Azure Resource Group."
  value       = module.core.resource_group_location
}