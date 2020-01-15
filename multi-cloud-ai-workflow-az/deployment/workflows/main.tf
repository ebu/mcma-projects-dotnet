resource "azuread_application" "logic_apps_app" {
  name                       = "${var.global_prefix}-logic-app"
  identifier_uris            = ["https://${var.global_prefix}-logic-app"]
}

resource "azuread_service_principal" "logic_apps_sp" {
  application_id               = azuread_application.logic_apps_app.application_id
  app_role_assignment_required = false
}

resource "random_password" "logic_apps_sp_password" {
  length = 16
  special = true
  override_special = "_%@"
}

resource "azuread_service_principal_password" "logic_apps_sp_password_assignment" {
  service_principal_id = azuread_service_principal.logic_apps_sp.id
  value                = random_password.logic_apps_sp_password.result
  end_date             = "2040-01-01T05:00:00Z"
}

resource "azurerm_template_deployment" "media_storage_api_connection" {
  name                = "media-storage-api-connection"
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"

  parameters = {
    accountName      = var.media_storage_account_name
    accessKey        = var.media_storage_access_key
  }

  template_body = file("./workflows/storage-connection.json")
}

locals {
  media_storage_connection_id   = "/subscriptions/${var.azure_subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.Web/connections/${var.media_storage_account_name}"
  client_id                     = azuread_application.logic_apps_app.application_id
  client_secret                 = random_password.logic_apps_sp_password.result
}
