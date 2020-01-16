locals {
  job_processor_api_zip_file         = "./../services/Mcma.Azure.JobProcessor/ApiHandler/dist/function.zip"
  job_processor_worker_zip_file      = "./../services/Mcma.Azure.JobProcessor/Worker/dist/function.zip"
  job_processor_api_function_name    = "${var.global_prefix}-job-processor-api"
  job_processor_url                  = "https://${local.job_processor_api_function_name}.azurewebsites.net"
  job_processor_worker_function_name = "${var.global_prefix}-job-processor-worker"
  job_processor_worker_function_key  = "${lookup(azurerm_template_deployment.job_processor_worker_function_key.outputs, "functionkey")}"
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

resource "azurerm_function_app" "job_processor_worker_function" {
  name                      = "${var.global_prefix}-job-processor-worker"
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
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.job_processor_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key
    
    WorkQueueStorage         = var.app_storage_connection_string
    TableName                = "JobProcessor"
    PublicUrl                = local.job_processor_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = local.cosmosdb_id
    CosmosDbRegion           = var.azure_location
    ServicesUrl              = local.services_url
    ServicesAuthType         = "AzureAD"
    ServicesAuthContext      = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
  }
}

resource "azurerm_template_deployment" "job_processor_worker_function_key" {
  name                = "job-processor-worker-func-key"
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"

  parameters = {
    functionApp = local.job_processor_worker_function_name
  }

  template_body = file("./services/function-key-template.json")
}

#===================================================================
# API Function
#===================================================================

resource "azuread_application" "job_processor_app" {
  name            = local.job_processor_api_function_name
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

resource "azurerm_function_app" "job_processor_api_function" {
  name                      = local.job_processor_api_function_name
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
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.job_processor_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    TableName                = "JobProcessor"
    PublicUrl                = local.job_processor_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = local.cosmosdb_id
    CosmosDbRegion           = var.azure_location
    WorkerFunctionId         = azurerm_storage_queue.job_processor_worker_function_queue.name
  }
}

output job_processor_url {
  value = "${local.job_processor_url}/"
}

output job_processor_worker_function_name {
    value = local.job_processor_worker_function_name
}

output job_processor_worker_function_key {
    value = local.job_processor_worker_function_key
}
