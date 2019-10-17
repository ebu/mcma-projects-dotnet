locals {
  azure_ai_service_api_zip_file    = "./../services/Mcma.Azure.AzureAiService/ApiHandler/dist/function.zip"
  azure_ai_service_worker_zip_file = "./../services/Mcma.Azure.AzureAiService/Worker/dist/function.zip"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "azure_ai_service_worker_function_queue" {
  name                 = "azure-ai-service-work-queue"
  resource_group_name  = "${var.resource_group_name}"
  storage_account_name = "${var.app_storage_account_name}"
}

resource "azurerm_storage_blob" "azure_ai_service_worker_function_zip" {
  name                   = "azure-ai-service/worker/function_${filesha256("${local.azure_ai_service_worker_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.azure_ai_service_worker_zip_file}"
}

resource "azurerm_application_insights" "azure_ai_service_worker_appinsights" {
  name                = "${var.global_prefix_lower_only}azureaiserviceworker_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "azure_ai_service_worker_function" {
  name                      = "${var.global_prefix_lower_only}azureaiserviceworker"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.azure_ai_service_worker_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.azure_ai_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.azure_ai_service_worker_appinsights.instrumentation_key}"

    WorkQueueStorage                 = "${var.app_storage_connection_string}"
    FunctionKeyEncryptionKey         = "${var.private_encryption_key}"
    TableName                        = "AzureAiService"
    PublicUrl                        = "https://${var.global_prefix_lower_only}azureaiserviceworker.azurewebsites.net/"
    CosmosDbEndpoint                 = "${var.cosmosdb_endpoint}"
    CosmosDbKey                      = "${var.cosmosdb_key}"
    CosmosDbDatabaseId               = "${var.global_prefix_lower_only}db"
    CosmosDbRegion                   = "${var.azure_location}"
    ServicesUrl                      = "${local.services_url}"
    ServicesAuthType                 = "AzureFunctionKey"
    ServicesAuthContext              = "{ \"functionKey\": \"${local.service_registry_key}\", \"isEncrypted\": false }"
    MediaStorageAccountName          = "${var.media_storage_account_name}"
    MediaStorageConnectionString     = "${var.media_storage_connection_string}"
    AzureVideoIndexerLocation        = "${var.azure_videoindexer_location}"
    AzureVideoIndexerAccountId       = "${var.azure_videoindexer_account_id}"
    AzureVideoIndexerApiUrl          = "${var.azure_videoindexer_api_url}"
    AzureVideoIndexerSubscriptionKey = "${var.azure_videoindexer_subscription_key}"
    ApiHandlerKey                    = "${lookup(azurerm_template_deployment.azure_ai_service_api_function_key.outputs, "functionkey")}"
  }
}

resource "azurerm_template_deployment" "azure_ai_service_worker_function_key" {
  name                = "azureaiserviceworkerfunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.azure_ai_service_worker_function.name}"
  }

  template_body = <<BODY
  {
      "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
      "contentVersion": "1.0.0.0",
      "parameters": {
          "functionApp": {"type": "string", "defaultValue": ""}
      },
      "variables": {
          "functionAppId": "[resourceId('Microsoft.Web/sites', parameters('functionApp'))]"
      },
      "resources": [
      ],
      "outputs": {
          "functionkey": {
              "type": "string",
              "value": "[listkeys(concat(variables('functionAppId'), '/host/default'), '2018-11-01').functionKeys.default]"                                                                                }
      }
  }
  BODY
}

#===================================================================
# API Function
#===================================================================

resource "azurerm_storage_blob" "azure_ai_service_api_function_zip" {
  name                   = "azure-ai-service/api/function_${filesha256("${local.azure_ai_service_api_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.azure_ai_service_api_zip_file}"
}

resource "azurerm_application_insights" "azure_ai_service_api_appinsights" {
  name                = "${var.global_prefix_lower_only}azureaiserviceapi_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "azure_ai_service_api_function" {
  name                      = "${var.global_prefix_lower_only}azureaiserviceapi"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.azure_ai_service_api_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.azure_ai_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.azure_ai_service_api_appinsights.instrumentation_key}"

    FunctionKeyEncryptionKey = "${var.private_encryption_key}"
    TableName                = "AzureAiService"
    PublicUrl                = "https://${var.global_prefix_lower_only}azureaiserviceapi.azurewebsites.net/"
    CosmosDbEndpoint         = "${var.cosmosdb_endpoint}"
    CosmosDbKey              = "${var.cosmosdb_key}"
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = "${var.azure_location}"
    WorkerFunctionId         = "${azurerm_storage_queue.azure_ai_service_worker_function_queue.name}"
  }
}

resource "azurerm_template_deployment" "azure_ai_service_api_function_key" {
  name                = "azureaiserviceapifunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.azure_ai_service_api_function.name}"
  }

  template_body = <<BODY
  {
      "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
      "contentVersion": "1.0.0.0",
      "parameters": {
          "functionApp": {"type": "string", "defaultValue": ""}
      },
      "variables": {
          "functionAppId": "[resourceId('Microsoft.Web/sites', parameters('functionApp'))]"
      },
      "resources": [
      ],
      "outputs": {
          "functionkey": {
              "type": "string",
              "value": "[listkeys(concat(variables('functionAppId'), '/host/default'), '2018-11-01').functionKeys.default]"                                                                                }
      }
  }
  BODY
}

output azure_ai_service_url {
  value = "https://${azurerm_function_app.azure_ai_service_api_function.default_hostname}/"
}

output "azure_ai_service_key" {
  value = "${lookup(azurerm_template_deployment.azure_ai_service_api_function_key.outputs, "functionkey")}"
}
