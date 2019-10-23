resource "azurerm_template_deployment" "media_storage_api_connection" {
  name                = "media-storage-api-connection"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    accountName      = "${var.media_storage_account_name}"
    accessKey        = "${var.media_storage_access_key}"
  }

  template_body = "${file("./workflows/storage-connection-arm.json")}"
}

locals {
  media_storage_connection_id = "/subscriptions/${var.azure_subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.Web/connections/${var.media_storage_account_name}"
}
