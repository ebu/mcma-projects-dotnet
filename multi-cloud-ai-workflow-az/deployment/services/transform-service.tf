locals {
  transform_service_api_zip_file    = "./../services/Mcma.Azure.TransformService/ApiHandler/dist/function.zip"
  transform_service_worker_zip_file = "./../services/Mcma.Azure.TransformService/Worker/dist/function.zip"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "transform_service_worker_function_queue" {
  name                 = "transform-service-work-queue"
  resource_group_name  = "${var.resource_group_name}"
  storage_account_name = "${var.app_storage_account_name}"
}

resource "azurerm_storage_blob" "transform_service_worker_function_zip" {
  name                   = "transform-service/worker/function_${filesha256("${local.transform_service_worker_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.transform_service_worker_zip_file}"
}

resource "azurerm_application_insights" "transform_service_worker_appinsights" {
  name                = "${var.global_prefix_lower_only}transformserviceworker_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "transform_service_worker_function" {
  name                      = "${var.global_prefix_lower_only}transformserviceworker"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.transform_service_worker_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.transform_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.transform_service_worker_appinsights.instrumentation_key}"

    WorkQueueStorage             = "${var.app_storage_connection_string}"
    FunctionKeyEncryptionKey     = "${var.private_encryption_key}"
    TableName                    = "TransformService"
    PublicUrl                    = "https://${var.global_prefix_lower_only}transformserviceworker.azurewebsites.net/"
    CosmosDbEndpoint             = "${var.cosmosdb_endpoint}"
    CosmosDbKey                  = "${var.cosmosdb_key}"
    CosmosDbDatabaseId           = "${var.global_prefix_lower_only}db"
    CosmosDbRegion               = "${var.azure_location}"
    ServicesUrl                  = "${local.services_url}"
    ServicesAuthType             = "AzureFunctionKey"
    ServicesAuthContext          = "{ \"functionKey\": \"${local.service_registry_key}\", \"isEncrypted\": false }"
    MediaStorageAccountName      = "${var.media_storage_account_name}"
    MediaStorageConnectionString = "${var.media_storage_connection_string}"
  }
}

resource "azurerm_template_deployment" "transform_service_worker_function_key" {
  name                = "transformserviceworkerfunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.transform_service_worker_function.name}"
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
              "value": "[listkeys(concat(variables('functionAppId'), '/host/default'), '2018-11-01').functionKeys.default]"
          }
      }
  }
  BODY
}

#===================================================================
# API Function
#===================================================================

resource "azurerm_storage_blob" "transform_service_api_function_zip" {
  name                   = "transform-service/api/function_${filesha256("${local.transform_service_api_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.transform_service_api_zip_file}"
}

resource "azurerm_application_insights" "transform_service_api_appinsights" {
  name                = "${var.global_prefix_lower_only}transformserviceapi_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "transform_service_api_function" {
  name                      = "${var.global_prefix_lower_only}transformserviceapi"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.transform_service_api_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.transform_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.transform_service_api_appinsights.instrumentation_key}"

    FunctionKeyEncryptionKey = "${var.private_encryption_key}"
    TableName                = "TransformService"
    PublicUrl                = "https://${var.global_prefix_lower_only}transformserviceapi.azurewebsites.net/"
    CosmosDbEndpoint         = "${var.cosmosdb_endpoint}"
    CosmosDbKey              = "${var.cosmosdb_key}"
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = "${var.azure_location}"
    WorkerFunctionId         = "${azurerm_storage_queue.transform_service_worker_function_queue.name}"
  }
}

resource "azurerm_template_deployment" "transform_service_api_function_key" {
  name                = "transformserviceapifunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.transform_service_api_function.name}"
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
              "value": "[listkeys(concat(variables('functionAppId'), '/host/default'), '2018-11-01').functionKeys.default]"
          }
      }
  }
  BODY
}

output transform_service_url {
  value = "https://${azurerm_function_app.transform_service_api_function.default_hostname}/"
}

output "transform_service_key" {
  value = "${lookup(azurerm_template_deployment.transform_service_api_function_key.outputs, "functionkey")}"
}
