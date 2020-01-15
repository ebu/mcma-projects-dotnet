resource "azurerm_storage_account" "media_storage_account" {
  name                     = "${var.global_prefix_lower_only}media"
  resource_group_name      = var.resource_group_name
  location                 = var.azure_location
  account_kind             = "Storage"
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "upload_container" {
  name                  = var.upload_container
  storage_account_name  = azurerm_storage_account.media_storage_account.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "temp_container" {
  name                  = var.temp_container
  storage_account_name  = azurerm_storage_account.media_storage_account.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "repository_container" {
  name                  = var.repository_container
  storage_account_name  = azurerm_storage_account.media_storage_account.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "preview_container" {
  name                  = var.preview_container
  storage_account_name  = azurerm_storage_account.media_storage_account.name
  container_access_type = "blob"
}

output "media_storage_connection_string" {
  value = azurerm_storage_account.media_storage_account.primary_connection_string
}

output "media_storage_account_name" {
  value = azurerm_storage_account.media_storage_account.name
}

output "media_storage_access_key" {
  value = azurerm_storage_account.media_storage_account.primary_access_key
}
