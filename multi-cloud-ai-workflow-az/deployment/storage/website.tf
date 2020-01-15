locals {
  website_storage_account_name  = "${var.global_prefix_lower_only}website"
  website_domain                = "${local.website_storage_account_name}.blob.core.windows.net"
}

resource "azurerm_storage_account" "website_storage_account" {
  name                     = local.website_storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.azure_location
  account_kind             = "StorageV2"
  account_tier             = "Standard"
  account_replication_type = "LRS"

  provisioner "local-exec" {
    command = "az login  --service-principal -u ${var.azure_client_id} -p ${var.azure_client_secret} --tenant ${var.azure_tenant_name} | az storage blob service-properties update --account-name ${azurerm_storage_account.website_storage_account.name} --static-website  --index-document index.html --404-document 404.html"
  }
}

resource "azurerm_storage_container" "website_container" {
  name                  = var.website_container
  storage_account_name  = azurerm_storage_account.website_storage_account.name
  container_access_type = "blob"
}

resource "azurerm_template_deployment" "website_media_cors" {
  name                = "media-storage-cors-settings"
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"

  parameters = {
    storageAccountName = azurerm_storage_account.media_storage_account.name
    websiteDomain      = "https://${local.website_domain}"
  }

  template_body = file("./storage/website-media-cors.json")
}

output "website_storage_account_name" {
  value = azurerm_storage_account.website_storage_account.name
}

output "website_storage_connection_string" {
  value = azurerm_storage_account.website_storage_account.primary_connection_string
}

output "website_storage_access_key" {
  value = azurerm_storage_account.website_storage_account.primary_access_key
}

output "website_domain" {
  value = local.website_domain
}

output "website_url" {
  value = "https://${local.website_domain}/${var.website_container}"
}