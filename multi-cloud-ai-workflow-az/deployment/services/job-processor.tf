locals {
  job_processor_api_zip_file    = "./../services/Mcma.Azure.JobProcessor/ApiHandler/dist/function.zip"
  job_processor_worker_zip_file = "./../services/Mcma.Azure.JobProcessor/Worker/dist/function.zip"
  job_processor_subdomain       = "${var.global_prefix_lower_only}jobprocessorapi"
  job_processor_url             = "https://${local.job_processor_subdomain}.azurewebsites.net"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "job_processor_worker_function_queue" {
  name                 = "job-processor-work-queue"
  storage_account_name = var.app_storage_account_name
}

resource "azurerm_storage_blob" "job_processor_worker_function_zip" {
  name                   = "job-processor/worker/function_${filesha256(local.job_processor_worker_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.job_processor_worker_zip_file
}

resource "azurerm_application_insights" "job_processor_worker_appinsights" {
  name                = "${var.global_prefix_lower_only}jobprocessorworker_appinsights"
  resource_group_name = var.resource_group_name
  location            = var.azure_location
  application_type    = "Web"
}

resource "azurerm_function_app" "job_processor_worker_function" {
  name                      = "${var.global_prefix_lower_only}jobprocessorworker"
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
    HASH                           = filesha256(local.job_processor_worker_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.job_processor_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.job_processor_worker_appinsights.instrumentation_key
    
    WorkQueueStorage         = var.app_storage_connection_string
    TableName                = "JobProcessor"
    PublicUrl                = local.job_processor_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = var.azure_location
    ServicesUrl              = local.services_url
    ServicesAuthType         = "AzureAD"
    ServicesAuthContext      = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
  }
}

#===================================================================
# API Function
#===================================================================

resource "azuread_application" "job_processor_app" {
  name            = local.job_processor_subdomain
  identifier_uris = [local.job_processor_url]
}

resource "azuread_service_principal" "job_processor_sp" {
  application_id               = azuread_application.job_processor_app.application_id
  app_role_assignment_required = false
}

resource "azurerm_storage_blob" "job_processor_api_function_zip" {
  name                   = "job-processor/api/function_${filesha256(local.job_processor_api_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.job_processor_api_zip_file
}

resource "azurerm_application_insights" "job_processor_api_appinsights" {
  name                = "${var.global_prefix_lower_only}jobprocessorapi_appinsights"
  resource_group_name = var.resource_group_name
  location            = var.azure_location
  application_type    = "Web"
}

resource "azurerm_function_app" "job_processor_api_function" {
  name                      = "${var.global_prefix_lower_only}jobprocessorapi"
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
      client_id         = azuread_application.job_processor_app.application_id
      allowed_audiences = [local.job_processor_url]
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.job_processor_api_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.job_processor_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.job_processor_api_appinsights.instrumentation_key

    TableName                = "JobProcessor"
    PublicUrl                = local.job_processor_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = var.azure_location
    WorkerFunctionId         = azurerm_storage_queue.job_processor_worker_function_queue.name
  }
}

output job_processor_url {
  value = "https://${azurerm_function_app.job_processor_api_function.default_hostname}/"
}
