variable "subscription_id" {
  type = string
}

variable "location" {
  type = string
}

variable "environment" {
  type = string
}

variable "project_name" {
  type = string
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

variable "api_service_plan_sku_name" {
  type    = string
  default = "F1"
}

variable "function_service_plan_sku_name" {
  type    = string
  default = "Y1"
}

variable "storage_account_replication_type" {
  type    = string
  default = "RAGRS"
}

variable "servicebus_sku" {
  type    = string
  default = "Standard"
}

variable "aspnetcore_environment" {
  type    = string
  default = "Development"
}
