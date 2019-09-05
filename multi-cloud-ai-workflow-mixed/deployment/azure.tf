provider "azurerm" {
  version = "~> 1.28.0"

  client_id       = "${var.azure_client_id}"
  client_secret   = "${var.azure_client_secret}"
  tenant_id       = "${var.azure_tenant_id}"
  subscription_id = "${var.azure_subscription_id}"

}

#########################
# Azure pre-reqs
#########################
resource "azurerm_resource_group" "resource_group" {
  name     = "${var.global_prefix}-rg"
  location = "${var.azure_location}"
}

resource "azurerm_cosmosdb_account" "cosmosdb_account" {
  name                = "${var.global_prefix}-cosmosdb-account"
  resource_group_name = "${azurerm_resource_group.resource_group.name}"
  location            = "${var.azure_location}"
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "BoundedStaleness"
  }

  geo_location {
    failover_priority = 0
    location          = "${var.azure_location}"
  }
}