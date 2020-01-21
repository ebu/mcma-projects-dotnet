locals {
  transform_service_api_zip_file         = "./../services/Mcma.Azure.TransformService/ApiHandler/dist/function.zip"
  transform_service_worker_zip_file      = "./../services/Mcma.Azure.TransformService/Worker/dist/function.zip"
  transform_service_api_function_name    = "${var.global_prefix}-transform-service-api"
  transform_service_url                  = "https://${local.transform_service_api_function_name}.azurewebsites.net"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "transform_service_worker_function_queue" {
  name                 = "transform-service-work-queue"
  storage_account_name = var.app_storage_account_name
}

resource "azurerm_storage_blob" "transform_service_worker_function_zip" {
  name                   = "transform-service/worker/function_${filesha256(local.transform_service_worker_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.transform_service_worker_zip_file
}

resource "azurerm_function_app" "transform_service_worker_function" {
  name                      = "${var.global_prefix}-transform-service-worker"
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
    HASH                           = filesha256(local.transform_service_worker_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.transform_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    WorkQueueStorage             = var.app_storage_connection_string
    TableName                    = "TransformService"
    PublicUrl                    = local.transform_service_url
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

resource "azuread_application" "transform_service_app" {
  name            = local.transform_service_api_function_name
  identifier_uris = [local.transform_service_url]
}

resource "azuread_service_principal" "transform_service_sp" {
  application_id               = azuread_application.transform_service_app.application_id
  app_role_assignment_required = false
}

resource "azurerm_storage_blob" "transform_service_api_function_zip" {
  name                   = "transform-service/api/function_${filesha256(local.transform_service_api_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.transform_service_api_zip_file
}

resource "azurerm_function_app" "transform_service_api_function" {
  name                      = local.transform_service_api_function_name
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
      client_id         = azuread_application.transform_service_app.application_id
      allowed_audiences = [local.transform_service_url]
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.transform_service_api_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.transform_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    TableName                = "TransformService"
    PublicUrl                = local.transform_service_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = local.cosmosdb_id
    CosmosDbRegion           = var.azure_location
    WorkerFunctionId         = azurerm_storage_queue.transform_service_worker_function_queue.name
  }
}

output transform_service_url {
  value = "${local.transform_service_url}/"
}
