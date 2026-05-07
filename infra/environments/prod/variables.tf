variable "subscription_id" {
  description = "Azure subscription ID where resources will be managed."
  type        = string
}

variable "location" {
  description = "Azure region for resources."
  type        = string
}

variable "environment" {
  description = "Environment name, for example dev, test, or prod."
  type        = string
}

variable "project_name" {
  description = "Project name used for naming and tagging Azure resources."
  type        = string
}
variable "resource_group_name" {
  type = string
}

variable "storage_account_name" {
  type = string
}

variable "servicebus_namespace_name" {
  type = string
}

variable "servicebus_queue_name" {
  type = string
}

variable "application_insights_name" {
  type = string
}

variable "key_vault_name" {
  type = string
}

variable "api_service_plan_name" {
  type = string
}

variable "function_service_plan_name" {
  type = string
}

variable "api_app_name" {
  type = string
}

variable "function_app_name" {
  type = string
}

variable "blob_container_name" {
  type = string
}
