locals {
  common_tags = {
    environment = var.environment
    project     = var.project_name
    managed_by  = "terraform"
  }
}

resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location

  tags = local.common_tags
}

resource "azurerm_storage_account" "main" {
  name                = var.storage_account_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  account_tier             = var.servicebus_sku
  account_replication_type = var.storage_account_replication_type

  allow_nested_items_to_be_public  = false
  cross_tenant_replication_enabled = false

  tags = local.common_tags
}

resource "azurerm_storage_container" "events" {
  name                  = "events"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "processed_events" {
  name                  = "processed-events"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "dead_letter" {
  name                  = "dead-letter"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

resource "azurerm_servicebus_namespace" "main" {
  name                = var.servicebus_namespace_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  sku = var.servicebus_sku

  tags = local.common_tags
}

resource "azurerm_servicebus_queue" "events" {
  name         = var.servicebus_queue_name
  namespace_id = azurerm_servicebus_namespace.main.id

  lock_duration = "PT1M"

  max_delivery_count = 10

  default_message_ttl = "P14D"

  duplicate_detection_history_time_window = "PT10M"

  batched_operations_enabled = true
  partitioning_enabled       = false

  dead_lettering_on_message_expiration = false
  requires_duplicate_detection         = false
  requires_session                     = false

  max_size_in_megabytes = 1024
}

resource "azurerm_application_insights" "main" {
  name                = var.application_insights_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  application_type    = "web"
  sampling_percentage = 0

  workspace_id = "/subscriptions/552d3bbc-1a07-49fa-9df2-d755c784c4e7/resourceGroups/DefaultResourceGroup-SUK/providers/Microsoft.OperationalInsights/workspaces/DefaultWorkspace-552d3bbc-1a07-49fa-9df2-d755c784c4e7-SUK"

  tags = local.common_tags
}

data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "main" {
  name                = var.key_vault_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tenant_id = data.azurerm_client_config.current.tenant_id

  sku_name = "standard"

  purge_protection_enabled   = false
  soft_delete_retention_days = 90

  tags = local.common_tags
}

resource "azurerm_service_plan" "api" {
  name                = var.api_service_plan_name
  location            = "westeurope"
  resource_group_name = azurerm_resource_group.main.name

  os_type  = "Windows"
  sku_name = var.api_service_plan_sku_name

  tags = local.common_tags
}

resource "azurerm_service_plan" "function" {
  name                = var.function_service_plan_name
  location            = "westeurope"
  resource_group_name = azurerm_resource_group.main.name

  os_type  = "Windows"
  sku_name = var.function_service_plan_sku_name

  tags = local.common_tags
}

resource "azurerm_windows_web_app" "api" {
  name                = var.api_app_name
  location            = "westeurope"
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.api.id

  https_only = true

  client_affinity_enabled                        = true
  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = false

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = var.aspnetcore_environment

    "ServiceBus__QueueName"      = var.servicebus_queue_name
    "BlobStorage__ContainerName" = var.blob_container_name

    "ServiceBusConnection"  = "@Microsoft.KeyVault(SecretUri=https://${var.key_vault_name}.vault.azure.net/secrets/servicebus-connection/)"
    "BlobStorageConnection" = "@Microsoft.KeyVault(SecretUri=https://${var.key_vault_name}.vault.azure.net/secrets/storage-connection/)"

    "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.main.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string

    "APPINSIGHTS_PROFILERFEATURE_VERSION"        = "1.0.0"
    "APPINSIGHTS_SNAPSHOTFEATURE_VERSION"        = "1.0.0"
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~2"
    "DiagnosticServices_EXTENSION_VERSION"       = "~3"
    "InstrumentationEngine_EXTENSION_VERSION"    = "disabled"
    "SnapshotDebugger_EXTENSION_VERSION"         = "disabled"

    "XDT_MicrosoftApplicationInsights_BaseExtensions" = "disabled"
    "XDT_MicrosoftApplicationInsights_Java"           = "1"
    "XDT_MicrosoftApplicationInsights_Mode"           = "recommended"
    "XDT_MicrosoftApplicationInsights_NodeJS"         = "1"
    "XDT_MicrosoftApplicationInsights_PreemptSdk"     = "disabled"
  }

  site_config {
    always_on  = false
    ftps_state = "FtpsOnly"
  }

  identity {
    type = "SystemAssigned"
  }

  tags = local.common_tags

  lifecycle {
    ignore_changes = [
      sticky_settings,
      tags["hidden-link: /app-insights-resource-id"],
      site_config[0].virtual_application,
    ]
  }
}

resource "azurerm_windows_function_app" "function" {
  name                = var.function_app_name
  location            = "westeurope"
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.function.id

  storage_account_name       = azurerm_storage_account.main.name
  storage_account_access_key = azurerm_storage_account.main.primary_access_key

  builtin_logging_enabled = false
  client_certificate_mode = "Required"

  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = false

  app_settings = {
    "AzureWebJobs.EventProcessorFunction.Disabled" = "1"
    "AzureWebJobs.ProcessEventFunction.Disabled"   = "0"
    "AzureWebJobsSecretStorageType"                = "files"

    "ServiceBus__QueueName"      = var.servicebus_queue_name
    "BlobStorage__ContainerName" = var.blob_container_name

    "ServiceBusConnection"  = "@Microsoft.KeyVault(SecretUri=https://${var.key_vault_name}.vault.azure.net/secrets/servicebus-connection/)"
    "BlobStorageConnection" = "@Microsoft.KeyVault(SecretUri=https://${var.key_vault_name}.vault.azure.net/secrets/storage-connection/)"

    "WEBSITE_ENABLE_SYNC_UPDATE_SITE"        = "true"
    "WEBSITE_RUN_FROM_PACKAGE"               = "1"
    "WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED" = "1"

    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
  }

  site_config {
    ftps_state        = "FtpsOnly"
    use_32_bit_worker = false

    application_stack {
      dotnet_version = "v8.0"
    }
  }

  identity {
    type = "SystemAssigned"
  }

  tags = local.common_tags

  lifecycle {
    ignore_changes = [
      app_settings["APPLICATIONINSIGHTS_CONNECTION_STRING"],
      sticky_settings,
      tags["hidden-link: /app-insights-resource-id"],
      site_config[0].application_insights_connection_string,
      site_config[0].cors,
    ]
  }
}

# Secrets are created outside Terraform using Azure CLI due to Azure Key Vault auth/token issues.
# Existing secrets:
# - servicebus-connection
# - storage-connection

resource "azurerm_key_vault_access_policy" "function" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_windows_function_app.function.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}

resource "azurerm_key_vault_access_policy" "api" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_windows_web_app.api.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}

resource "azurerm_key_vault_access_policy" "terraform_user" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = [
    "Get",
    "List",
    "Set",
    "Delete"
  ]

  certificate_permissions = [
    "Get",
    "List",
    "ManageContacts"
  ]
}