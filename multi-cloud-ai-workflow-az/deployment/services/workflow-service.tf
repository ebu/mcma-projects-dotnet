locals {
  workflow_service_api_zip_file      = "./../services/Mcma.Azure.WorkflowService/ApiHandler/dist/function.zip"
  workflow_service_worker_zip_file   = "./../services/Mcma.Azure.WorkflowService/Worker/dist/function.zip"
  workflow_service_api_function_name = "${var.global_prefix}-workflow-service-api"
  workflow_service_url               = "https://${local.workflow_service_api_function_name}.azurewebsites.net"
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

resource "azurerm_function_app" "workflow_service_worker_function" {
  name                      = "${var.global_prefix}-workflow-service-worker"
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
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.workflow_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    WorkQueueStorage             = var.app_storage_connection_string
    TableName                    = "WorkflowService"
    PublicUrl                    = local.workflow_service_url
    AzureSubscriptionId          = var.azure_subscription_id
    AzureTenantId                = var.azure_tenant_id
    AzureResourceGroupName       = var.resource_group_name
    CosmosDbEndpoint             = var.cosmosdb_endpoint
    CosmosDbKey                  = var.cosmosdb_key
    CosmosDbDatabaseId           = local.cosmosdb_id
    CosmosDbRegion               = var.azure_location
    ServicesUrl                  = local.services_url
    ServiceRegistryUrl           = local.service_registry_url
    ServicesAuthType             = "AzureAD"
    ServicesAuthContext          = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
    MediaStorageAccountName      = var.media_storage_account_name
    MediaStorageConnectionString = var.media_storage_connection_string
  }

  provisioner "local-exec" {
    command = "az webapp start --resource-group ${var.resource_group_name} --name ${azurerm_function_app.workflow_service_worker_function.name}"
  }
}

data "azurerm_subscription" "primary" {}

resource "azurerm_role_definition" "workflow_service_worker_role" {
  name  = "Workflow Invoker"
  scope = data.azurerm_subscription.primary.id

  permissions {
    actions = [
      "Microsoft.Logic/workflows/read",
      "Microsoft.Logic/workflows/triggers/listCallbackUrl/action",
      "Microsoft.Logic/workflows/run/action"
    ]
    not_actions = []
  }

  assignable_scopes = [data.azurerm_subscription.primary.id]
}

resource "azurerm_role_assignment" "workflow_service_worker_role_assignment" {
  scope              = data.azurerm_subscription.primary.id
  role_definition_id = azurerm_role_definition.workflow_service_worker_role.id
  principal_id       = azurerm_function_app.workflow_service_worker_function.identity[0].principal_id
}

#===================================================================
# API Function
#===================================================================

resource "azuread_application" "workflow_service_app" {
  name            = local.workflow_service_api_function_name
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

resource "azurerm_function_app" "workflow_service_api_function" {
  name                      = local.workflow_service_api_function_name
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
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.workflow_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    TableName           = "WorkflowService"
    PublicUrl           = local.workflow_service_url
    CosmosDbEndpoint    = var.cosmosdb_endpoint
    CosmosDbKey         = var.cosmosdb_key
    CosmosDbDatabaseId  = local.cosmosdb_id
    CosmosDbRegion      = var.azure_location
    WorkerFunctionId    = azurerm_storage_queue.workflow_service_worker_function_queue.name
    ServicesUrl         = local.services_url
    ServicesAuthType    = "AzureAD"
    ServicesAuthContext = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
  }
}

output workflow_service_url {
  value = "${local.workflow_service_url}/"
}

output workflow_service_worker_url {
  value = "https://${azurerm_function_app.workflow_service_worker_function.default_hostname}/"
}
