locals {
  ame_service_api_zip_file         = "./../services/Mcma.Azure.AmeService/ApiHandler/dist/function.zip"
  ame_service_worker_zip_file      = "./../services/Mcma.Azure.AmeService/Worker/dist/function.zip"
  ame_service_api_function_name    = "${var.global_prefix}-ame-service-api"
  ame_service_url                  = "https://${local.ame_service_api_function_name}.azurewebsites.net"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "ame_service_worker_function_queue" {
  name                 = "ame-service-work-queue"
  storage_account_name = var.app_storage_account_name
}

resource "azurerm_storage_blob" "ame_service_worker_function_zip" {
  name                   = "ame-service/worker/function_${filesha256(local.ame_service_worker_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.ame_service_worker_zip_file
}

resource "azurerm_function_app" "ame_service_worker_function" {
  name                      = "${var.global_prefix}-ame-service-worker"
  location                  = var.azure_location
  resource_group_name       = var.resource_group_name
  app_service_plan_id       = azurerm_app_service_plan.mcma_services.id
  storage_connection_string = var.app_storage_connection_string
  version                   = "~2"

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.ame_service_worker_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.ame_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    WorkQueueStorage             = var.app_storage_connection_string
    TableName                    = "AmeService"
    PublicUrl                    = local.ame_service_url
    CosmosDbEndpoint             = var.cosmosdb_endpoint
    CosmosDbKey                  = var.cosmosdb_key
    CosmosDbDatabaseId           = local.cosmosdb_id
    CosmosDbRegion               = var.azure_location
    ServicesUrl                  = local.services_url
    ServicesAuthType             = "AzureAD"
    ServicesAuthContext          = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
    MediaStorageAccountName      = var.media_storage_account_name
    MediaStorageConnectionString = var.media_storage_connection_string
  }
}

#===================================================================
# API Function
#===================================================================

resource "azuread_application" "ame_service_app" {
  name            = local.ame_service_api_function_name
  identifier_uris = [local.ame_service_url]
}

resource "azuread_service_principal" "ame_service_sp" {
  application_id               = azuread_application.ame_service_app.application_id
  app_role_assignment_required = false
}

resource "azurerm_storage_blob" "ame_service_api_function_zip" {
  name                   = "ame-service/api/function_${filesha256(local.ame_service_api_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.ame_service_api_zip_file
}

resource "azurerm_function_app" "ame_service_api_function" {
  name                      = local.ame_service_api_function_name
  location                  = var.azure_location
  resource_group_name       = var.resource_group_name
  app_service_plan_id       = azurerm_app_service_plan.mcma_services.id
  storage_connection_string = var.app_storage_connection_string
  version                   = "~2"

  auth_settings {
    enabled                       = true
    issuer                        = "https://sts.windows.net/${var.azure_tenant_id}"
    default_provider              = "AzureActiveDirectory"
    unauthenticated_client_action = "RedirectToLoginPage"
    active_directory {
      client_id         = azuread_application.ame_service_app.application_id
      allowed_audiences = [local.ame_service_url]
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.ame_service_api_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.ame_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    TableName                = "AmeService"
    PublicUrl                = local.ame_service_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = local.cosmosdb_id
    CosmosDbRegion           = var.azure_location
    WorkerFunctionId         = azurerm_storage_queue.ame_service_worker_function_queue.name
  }
}

output ame_service_url {
  value = "${local.ame_service_url}/"
}
