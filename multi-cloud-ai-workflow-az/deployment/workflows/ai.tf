resource "azurerm_template_deployment" "ai_workflow" {
  name                = "ai-workflow"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"
  depends_on          = [
    azurerm_template_deployment.media_storage_api_connection,
    azurerm_template_deployment.website_storage_api_connection
  ]

  parameters = {
    mediaStorageConnectionId   = "${local.media_storage_connection_id}"
    mediaStorageAccountName    = "${var.media_storage_account_name}"
    repositoryContainerName    = "${var.repository_container}"
    tempContainerName          = "${var.temp_container}"
    websiteStorageConnectionId = "${local.website_storage_connection_id}"
    websiteStorageAccountName  = "${var.website_storage_account_name}"
    websiteContainerName       = "${var.website_container}"
  }

  template_body = "${file("./workflows/ai.json")}"
}
