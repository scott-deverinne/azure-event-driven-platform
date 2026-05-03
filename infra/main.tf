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