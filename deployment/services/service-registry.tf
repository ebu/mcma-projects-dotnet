locals {
  zip_file = "./../services/Mcma.Azure.ServiceRegistry/ApiHandler/dist/function.zip"
}

resource "azurerm_storage_blob" "service_registry_api_function_zip" {
  name                   = "service-registry/function_${filesha256("${local.zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.zip_file}"
}

data "azurerm_storage_account_sas" "service_registry_api_function_sas" {
  connection_string = "${var.app_storage_connection_string}"
  https_only        = true

  resource_types {
    service   = false
    container = false
    object    = true
  }

  services {
    blob  = true
    queue = false
    table = false
    file  = false
  }

  start  = "2019-08-19"
  expiry = "2020-08-19"

  permissions {
    read    = true
    write   = false
    delete  = false
    list    = false
    add     = false
    create  = false
    update  = false
    process = false
  }
}

resource "azurerm_application_insights" "service_registry_api_appinsights" {
  name                = "${var.global_prefix_lower_only}serviceregistry"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "service_registry_api_function" {
  name                      = "${var.global_prefix_lower_only}serviceregistryapi"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.service_registry_api_function_zip.name}${data.azurerm_storage_account_sas.service_registry_api_function_sas.sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.service_registry_api_appinsights.instrumentation_key}"

    FunctionKeyEncryptionKey = "${var.private_encryption_key}"
    TableName                = "ServiceRegistry"
    PublicUrl                = "https://${var.global_prefix_lower_only}serviceregistryapi.azurewebsites.net/"
    CosmosDbEndpoint         = "${var.cosmosdb_endpoint}"
    CosmosDbKey              = "${var.cosmosdb_key}"
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = "${var.azure_location}"
  }
}

resource "azurerm_template_deployment" "service_registry_function_key" {
  name                = "serviceregistryfunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.service_registry_api_function.name}"
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

output service_registry_url {
  value = "https://${azurerm_function_app.service_registry_api_function.default_hostname}/"
}

output "service_registry_key" {
  value = "${lookup(azurerm_template_deployment.service_registry_function_key.outputs, "functionkey")}"
}

output services_url {
  value = "https://${azurerm_function_app.service_registry_api_function.default_hostname}/services"
}
