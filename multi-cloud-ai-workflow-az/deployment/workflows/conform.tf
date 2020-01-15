resource "azurerm_template_deployment" "conform_workflow" {
  name                = "conform-workflow"
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"
  depends_on          = [
    azurerm_template_deployment.media_storage_api_connection
  ]

  parameters = {
    tenantId                   = var.azure_tenant_id
    clientId                   = local.client_id
    clientSecret               = local.client_secret
    mediaStorageConnectionId   = local.media_storage_connection_id
    mediaStorageAccountName    = var.media_storage_account_name
    repositoryContainerName    = var.repository_container
    tempContainerName          = var.temp_container
    previewContainerName       = var.preview_container
  }

  template_body = file("./workflows/conform.json")
}
