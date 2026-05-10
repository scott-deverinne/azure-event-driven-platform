module "core" {
  source = "../../modules/core"

  subscription_id = var.subscription_id
  location        = var.location
  environment     = var.environment
  project_name    = var.project_name

  resource_group_name        = var.resource_group_name
  storage_account_name       = var.storage_account_name
  servicebus_namespace_name  = var.servicebus_namespace_name
  servicebus_queue_name      = var.servicebus_queue_name
  application_insights_name  = var.application_insights_name
  key_vault_name             = var.key_vault_name
  api_service_plan_name      = var.api_service_plan_name
  function_service_plan_name = var.function_service_plan_name
  api_app_name               = var.api_app_name
  function_app_name          = var.function_app_name
  blob_container_name        = var.blob_container_name
}