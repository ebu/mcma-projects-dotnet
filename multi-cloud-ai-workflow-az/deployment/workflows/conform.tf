resource "azurerm_template_deployment" "conform_workflow" {
  name                = "conform-workflow"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"
  depends_on          = [azurerm_template_deployment.media_storage_api_connection]

  parameters = {
    mediaStorageConnectionId = "${local.media_storage_connection_id}"
    mediaStorageAccountName  = "${var.media_storage_account_name}"
    repositoryContainerName  = "${var.repository_container}"
  }

  template_body = "${file("./workflows/conform-arm.json")}"
}
