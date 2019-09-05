resource "azurerm_storage_account" "app_storage_account" {
  name                     = "${var.global_prefix_lower_only}app"
  resource_group_name      = "${var.resource_group_name}"
  location                 = "${var.azure_location}"
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "deploy_container" {
  name                  = "${var.deploy_container}"
  resource_group_name   = "${var.resource_group_name}"
  storage_account_name  = "${azurerm_storage_account.app_storage_account.name}"
  container_access_type = "private"
}

output "app_storage_connection_string" {
  value = "${azurerm_storage_account.app_storage_account.primary_connection_string}"
}

output "app_storage_account_name" {
  value = "${azurerm_storage_account.app_storage_account.name}"
}