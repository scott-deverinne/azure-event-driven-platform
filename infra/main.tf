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
  partitioning_enabled = false

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