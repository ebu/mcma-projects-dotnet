locals {
  job_processor_api_zip_file    = "./../services/Mcma.Azure.JobProcessor/ApiHandler/dist/function.zip"
  job_processor_worker_zip_file = "./../services/Mcma.Azure.JobProcessor/Worker/dist/function.zip"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "job_processor_worker_function_queue" {
  name                 = "job-processor-work-queue"
  resource_group_name  = "${var.resource_group_name}"
  storage_account_name = "${var.app_storage_account_name}"
}

resource "azurerm_storage_blob" "job_processor_worker_function_zip" {
  name                   = "job-processor/worker/function_${filesha256("${local.job_processor_worker_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.job_processor_worker_zip_file}"
}

resource "azurerm_application_insights" "job_processor_worker_appinsights" {
  name                = "${var.global_prefix_lower_only}jobprocessorworker_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "job_processor_worker_function" {
  name                      = "${var.global_prefix_lower_only}jobprocessorworker"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.job_processor_worker_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.job_processor_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.job_processor_worker_appinsights.instrumentation_key}"
    AzureWebJobsStorage            = "${var.app_storage_connection_string}"

    FunctionKeyEncryptionKey = "${var.private_encryption_key}"
    TableName                = "JobProcessor"
    PublicUrl                = "https://${var.global_prefix_lower_only}jobprocessorworker.azurewebsites.net/"
    CosmosDbEndpoint         = "${var.cosmosdb_endpoint}"
    CosmosDbKey              = "${var.cosmosdb_key}"
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = "${var.azure_location}"
    ServicesUrl              = "${local.services_url}"
    ServicesAuthType         = "AzureFunctionKey"
    ServicesAuthContext      = "{ \"functionKey\": \"${local.service_registry_key}\", \"isEncrypted\": false }"
  }
}

resource "azurerm_template_deployment" "job_processor_worker_function_key" {
  name                = "jobprocessorworkerfunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.job_processor_worker_function.name}"
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

resource "azurerm_storage_blob" "job_processor_api_function_zip" {
  name                   = "job-processor/api/function_${filesha256("${local.job_processor_api_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.job_processor_api_zip_file}"
}

resource "azurerm_application_insights" "job_processor_api_appinsights" {
  name                = "${var.global_prefix_lower_only}jobprocessorapi_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "job_processor_api_function" {
  name                      = "${var.global_prefix_lower_only}jobprocessorapi"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.job_processor_api_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.job_processor_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.job_processor_api_appinsights.instrumentation_key}"

    FunctionKeyEncryptionKey = "${var.private_encryption_key}"
    TableName                = "JobProcessor"
    PublicUrl                = "https://${var.global_prefix_lower_only}jobprocessorapi.azurewebsites.net/"
    CosmosDbEndpoint         = "${var.cosmosdb_endpoint}"
    CosmosDbKey              = "${var.cosmosdb_key}"
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = "${var.azure_location}"
    WorkerFunctionId         = "${azurerm_storage_queue.job_processor_worker_function_queue.name}"
  }
}

resource "azurerm_template_deployment" "job_processor_api_function_key" {
  name                = "jobprocessorapifunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.job_processor_api_function.name}"
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

output job_processor_url {
  value = "https://${azurerm_function_app.job_processor_api_function.default_hostname}/"
}

output "job_processor_key" {
  value = "${lookup(azurerm_template_deployment.job_processor_api_function_key.outputs, "functionkey")}"
}
