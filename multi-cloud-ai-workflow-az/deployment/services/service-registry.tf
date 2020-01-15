locals {
  service_registry_api_zip_file = "./../services/Mcma.Azure.ServiceRegistry/ApiHandler/dist/function.zip"
  service_registry_subdomain    = "${var.global_prefix_lower_only}serviceregistryapi"
  service_registry_url          = "https://${local.service_registry_subdomain}.azurewebsites.net"
  services_url                  = "${local.service_registry_url}/services"
}

resource "azuread_application" "service_registry_app" {
  name            = local.service_registry_subdomain
  identifier_uris = [local.service_registry_url]
}

resource "azuread_service_principal" "service_registry_sp" {
  application_id               = azuread_application.service_registry_app.application_id
  app_role_assignment_required = false
}

resource "azurerm_storage_blob" "service_registry_api_function_zip" {
  name                   = "service-registry/function_${filesha256(local.service_registry_api_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.service_registry_api_zip_file
}

resource "azurerm_application_insights" "service_registry_api_appinsights" {
  name                = local.service_registry_subdomain
  resource_group_name = var.resource_group_name
  location            = var.azure_location
  application_type    = "Web"
}

resource "azurerm_function_app" "service_registry_api_function" {
  name                      = local.service_registry_subdomain
  location                  = var.azure_location
  resource_group_name       = var.resource_group_name
  app_service_plan_id       = azurerm_app_service_plan.mcma_services.id
  storage_connection_string = var.app_storage_connection_string
  version                   = "~2"

  site_config {
    cors {
      allowed_origins = [
        "https://${var.website_domain}",
        "http://localhost:4200"
      ]
      support_credentials = true
    }
  }

  auth_settings {
    enabled                       = true
    issuer                        = "https://sts.windows.net/${var.azure_tenant_id}"
    default_provider              = "AzureActiveDirectory"
    unauthenticated_client_action = "RedirectToLoginPage"
    active_directory {
      client_id         = azuread_application.service_registry_app.application_id
      allowed_audiences = [local.service_registry_url]
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.service_registry_api_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.service_registry_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.service_registry_api_appinsights.instrumentation_key

    TableName                = "ServiceRegistry"
    PublicUrl                = "https://${var.global_prefix_lower_only}serviceregistryapi.azurewebsites.net/"
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = var.azure_location
  }
}

output service_registry_url {
  value = "${local.service_registry_url}/"
}

output services_url {
  value = local.services_url
}

output service_registry_app_id {
  value = azuread_application.service_registry_app.application_id
}

output service_registry_scope {
  value = azuread_application.service_registry_app.oauth2_permissions[0].id
}
