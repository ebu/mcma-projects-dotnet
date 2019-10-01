locals {
  ame_service_api_zip_file    = "./../services/Mcma.Azure.AmeService/ApiHandler/dist/function.zip"
  ame_service_worker_zip_file = "./../services/Mcma.Azure.AmeService/Worker/dist/function.zip"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "ame_service_worker_function_queue" {
  name                 = "ame-service-work-queue"
  resource_group_name  = "${var.resource_group_name}"
  storage_account_name = "${var.app_storage_account_name}"
}

resource "azurerm_storage_blob" "ame_service_worker_function_zip" {
  name                   = "ame-service/worker/function_${filesha256("${local.ame_service_worker_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.ame_service_worker_zip_file}"
}

resource "azurerm_application_insights" "ame_service_worker_appinsights" {
  name                = "${var.global_prefix_lower_only}ameserviceworker_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "ame_service_worker_function" {
  name                      = "${var.global_prefix_lower_only}ameserviceworker"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.ame_service_worker_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.ame_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.ame_service_worker_appinsights.instrumentation_key}"
    AzureWebJobsStorage            = "${var.app_storage_connection_string}"

    FunctionKeyEncryptionKey     = "${var.private_encryption_key}"
    TableName                    = "AmeService"
    PublicUrl                    = "https://${var.global_prefix_lower_only}ameserviceworker.azurewebsites.net/"
    CosmosDbEndpoint             = "${var.cosmosdb_endpoint}"
    CosmosDbKey                  = "${var.cosmosdb_key}"
    CosmosDbDatabaseId           = "${var.global_prefix_lower_only}db"
    CosmosDbRegion               = "${var.azure_location}"
    ServicesUrl                  = "${local.services_url}"
    ServicesAuthType             = "AzureFunctionKey"
    ServicesAuthContext          = "{ \"functionKey\": \"${local.service_registry_key}\", \"isEncrypted\": false }"
    MediaStorageConnectionString = "${var.media_storage_connection_string}"
  }
}

resource "azurerm_template_deployment" "ame_service_worker_function_key" {
  name                = "ameserviceworkerfunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.ame_service_worker_function.name}"
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

resource "azurerm_storage_blob" "ame_service_api_function_zip" {
  name                   = "ame-service/api/function_${filesha256("${local.ame_service_api_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.ame_service_api_zip_file}"
}

resource "azurerm_application_insights" "ame_service_api_appinsights" {
  name                = "${var.global_prefix_lower_only}ameserviceapi_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "ame_service_api_function" {
  name                      = "${var.global_prefix_lower_only}ameserviceapi"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.ame_service_api_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.ame_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.ame_service_api_appinsights.instrumentation_key}"

    FunctionKeyEncryptionKey = "${var.private_encryption_key}"
    TableName                = "AmeService"
    PublicUrl                = "https://${var.global_prefix_lower_only}ameserviceapi.azurewebsites.net/"
    CosmosDbEndpoint         = "${var.cosmosdb_endpoint}"
    CosmosDbKey              = "${var.cosmosdb_key}"
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = "${var.azure_location}"
    WorkerFunctionId         = "${azurerm_storage_queue.ame_service_worker_function_queue.name}"
  }
}

resource "azurerm_template_deployment" "ame_service_api_function_key" {
  name                = "ameserviceapifunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.ame_service_api_function.name}"
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

output ame_service_url {
  value = "https://${azurerm_function_app.ame_service_api_function.default_hostname}/"
}

output "ame_service_key" {
  value = "${lookup(azurerm_template_deployment.ame_service_api_function_key.outputs, "functionkey")}"
}
