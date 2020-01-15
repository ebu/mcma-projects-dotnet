locals {
  workflow_service_api_zip_file    = "./../services/Mcma.Azure.WorkflowService/ApiHandler/dist/function.zip"
  workflow_service_worker_zip_file = "./../services/Mcma.Azure.WorkflowService/Worker/dist/function.zip"
  workflow_service_subdomain       = "${var.global_prefix_lower_only}workflowserviceapi"
  workflow_service_url             = "https://${local.workflow_service_subdomain}.azurewebsites.net"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "workflow_service_worker_function_queue" {
  name                 = "workflow-service-work-queue"
  storage_account_name = var.app_storage_account_name
}

resource "azurerm_storage_blob" "workflow_service_worker_function_zip" {
  name                   = "workflow-service/worker/function_${filesha256(local.workflow_service_worker_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.workflow_service_worker_zip_file
}

resource "azurerm_application_insights" "workflow_service_worker_appinsights" {
  name                = "${var.global_prefix_lower_only}workflowserviceworker_appinsights"
  resource_group_name = var.resource_group_name
  location            = var.azure_location
  application_type    = "Web"
}

resource "azurerm_function_app" "workflow_service_worker_function" {
  name                      = "${var.global_prefix_lower_only}workflowserviceworker"
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
    HASH                           = filesha256(local.workflow_service_worker_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.workflow_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.workflow_service_worker_appinsights.instrumentation_key

    WorkQueueStorage             = var.app_storage_connection_string
    TableName                    = "WorkflowService"
    PublicUrl                    = "https://${var.global_prefix_lower_only}workflowserviceworker.azurewebsites.net/"
    AzureClientId                = var.azure_client_id
    AzureClientSecret            = var.azure_client_secret
    AzureSubscriptionId          = var.azure_subscription_id
    AzureTenantId                = var.azure_tenant_id
    AzureTenantName              = var.azure_tenant_name
    AzureResourceGroupName       = var.resource_group_name
    CosmosDbEndpoint             = var.cosmosdb_endpoint
    CosmosDbKey                  = var.cosmosdb_key
    CosmosDbDatabaseId           = "${var.global_prefix_lower_only}db"
    CosmosDbRegion               = var.azure_location
    ServicesUrl                  = local.services_url
    ServiceRegistryUrl           = local.service_registry_url
    ServicesAuthType             = "AzureAD"
    ServicesAuthContext          = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
    MediaStorageAccountName      = var.media_storage_account_name
    MediaStorageConnectionString = var.media_storage_connection_string
  }
}

#===================================================================
# API Function
#===================================================================

resource "azuread_application" "workflow_service_app" {
  name            = local.workflow_service_subdomain
  identifier_uris = [local.workflow_service_url]
}

resource "azuread_service_principal" "workflow_service_sp" {
  application_id               = azuread_application.workflow_service_app.application_id
  app_role_assignment_required = false
}

resource "azurerm_storage_blob" "workflow_service_api_function_zip" {
  name                   = "workflow-service/api/function_${filesha256(local.workflow_service_api_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.workflow_service_api_zip_file
}

resource "azurerm_application_insights" "workflow_service_api_appinsights" {
  name                = "${var.global_prefix_lower_only}workflowserviceapi_appinsights"
  resource_group_name = var.resource_group_name
  location            = var.azure_location
  application_type    = "Web"
}

resource "azurerm_function_app" "workflow_service_api_function" {
  name                      = "${var.global_prefix_lower_only}workflowserviceapi"
  location                  = var.azure_location
  resource_group_name       = var.resource_group_name
  app_service_plan_id       = azurerm_app_service_plan.mcma_services.id
  storage_connection_string = var.app_storage_connection_string
  version                   = "~2"

  identity {
    type = "SystemAssigned"
  }

  auth_settings {
    enabled                       = true
    issuer                        = "https://sts.windows.net/${var.azure_tenant_id}"
    default_provider              = "AzureActiveDirectory"
    unauthenticated_client_action = "RedirectToLoginPage"
    active_directory {
      client_id         = azuread_application.workflow_service_app.application_id
      allowed_audiences = [local.workflow_service_url]
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.workflow_service_api_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.workflow_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.workflow_service_api_appinsights.instrumentation_key

    TableName                = "WorkflowService"
    PublicUrl                = local.workflow_service_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = var.azure_location
    WorkerFunctionId         = azurerm_storage_queue.workflow_service_worker_function_queue.name
    ServicesUrl              = local.services_url
    ServicesAuthType         = "AzureAD"
    ServicesAuthContext      = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
  }
}


output workflow_service_url {
  value = "https://${azurerm_function_app.workflow_service_api_function.default_hostname}/"
}
