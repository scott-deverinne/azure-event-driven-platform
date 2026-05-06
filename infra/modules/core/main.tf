locals {
  common_tags = {
    environment = var.environment
    project     = var.project_name
    managed_by  = "terraform"
  }
}

resource "azurerm_resource_group" "main" {
  name     = "event-platform-rg"
  location = var.location

  tags = local.common_tags
}

resource "azurerm_storage_account" "main" {
  name                = "eventplatformstoragesdd"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  account_tier             = "Standard"
  account_replication_type = "RAGRS"

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
  name                = "event-platform-sb-scott"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  sku = "Standard"

  tags = local.common_tags
}

resource "azurerm_servicebus_queue" "events" {
  name         = "event-queue-dev"
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
  name                = "event-platform-ai"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  application_type    = "web"
  sampling_percentage = 0

  workspace_id = "/subscriptions/552d3bbc-1a07-49fa-9df2-d755c784c4e7/resourceGroups/DefaultResourceGroup-SUK/providers/Microsoft.OperationalInsights/workspaces/DefaultWorkspace-552d3bbc-1a07-49fa-9df2-d755c784c4e7-SUK"

  tags = local.common_tags
}

data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "main" {
  name                = "event-platform-kv"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tenant_id = data.azurerm_client_config.current.tenant_id

  sku_name = "standard"

  purge_protection_enabled   = false
  soft_delete_retention_days = 90

  tags = local.common_tags

  lifecycle {
    ignore_changes = [
      contact
    ]
  }
}

resource "azurerm_service_plan" "api" {
  name                = "ASP-eventplatformrg-a2f8"
  location            = "westeurope"
  resource_group_name = azurerm_resource_group.main.name

  os_type  = "Windows"
  sku_name = "F1"

  tags = local.common_tags
}

resource "azurerm_service_plan" "function" {
  name                = "ASP-eventplatformrg-9edf"
  location            = "westeurope"
  resource_group_name = azurerm_resource_group.main.name

  os_type  = "Windows"
  sku_name = "Y1"

  tags = local.common_tags
}

resource "azurerm_windows_web_app" "api" {
  name                = "event-platform-api-scott-dev"
  location            = "westeurope"
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.api.id

  https_only = true

  client_affinity_enabled                       = true
  ftp_publish_basic_authentication_enabled      = false
  webdeploy_publish_basic_authentication_enabled = false

  site_config {
    always_on  = false
    ftps_state = "FtpsOnly"
  }

  tags = local.common_tags

  lifecycle {
    ignore_changes = [
      app_settings,
      sticky_settings,
      tags["hidden-link: /app-insights-resource-id"],
      site_config[0].virtual_application,
    ]
  }

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_windows_function_app" "function" {
  name                = "event-platform-func-scott-dev"
  location            = "westeurope"
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.function.id

  storage_account_name       = azurerm_storage_account.main.name
  storage_account_access_key = azurerm_storage_account.main.primary_access_key

  builtin_logging_enabled = false
  client_certificate_mode = "Required"

  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = false

  site_config {
    ftps_state        = "FtpsOnly"
    use_32_bit_worker = false

    application_stack {
      dotnet_version = "v8.0"
    }
  }

  tags = local.common_tags

  lifecycle {
    ignore_changes = [
      app_settings,
      tags["hidden-link: /app-insights-resource-id"],
      site_config[0].application_insights_connection_string,
      site_config[0].cors,
    ]
  }

  identity {
    type = "SystemAssigned"
  }
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

resource "azurerm_key_vault_access_policy" "function" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_windows_function_app.function.identity[0].principal_id

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

# Temporarily managing secrets outside Terraform due to Azure Key Vault auth issue
#
# resource "azurerm_key_vault_secret" "servicebus" {
#   name         = "servicebus-connection"
#   value        = azurerm_servicebus_namespace.main.default_primary_connection_string
#   key_vault_id = azurerm_key_vault.main.id
#
#   depends_on = [
#     azurerm_key_vault_access_policy.terraform_user
#   ]
# }
#
# resource "azurerm_key_vault_secret" "storage" {
#   name         = "storage-connection"
#   value        = azurerm_storage_account.main.primary_connection_string
#   key_vault_id = azurerm_key_vault.main.id
#
#   depends_on = [
#     azurerm_key_vault_access_policy.terraform_user
#   ]
# }