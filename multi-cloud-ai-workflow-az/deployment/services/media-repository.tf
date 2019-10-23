locals {
  media_repository_api_zip_file = "./../services/Mcma.Azure.MediaRepository/ApiHandler/dist/function.zip"
}

resource "azurerm_storage_blob" "media_repository_api_function_zip" {
  name                   = "media-repository/function_${filesha256("${local.media_repository_api_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.media_repository_api_zip_file}"
}

resource "azurerm_application_insights" "media_repository_api_appinsights" {
  name                = "${var.global_prefix_lower_only}mediarepository"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "media_repository_api_function" {
  name                      = "${var.global_prefix_lower_only}mediarepositoryapi"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  site_config {
    cors {
      allowed_origins = [
        "https://${var.global_prefix_lower_only}website.azurewebsites.net/"
      ]
      support_credentials = true
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.media_repository_api_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.media_repository_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.media_repository_api_appinsights.instrumentation_key}"

    FunctionKeyEncryptionKey = "${var.private_encryption_key}"
    TableName                = "MediaRepository"
    PublicUrl                = "https://${var.global_prefix_lower_only}mediarepositoryapi.azurewebsites.net/"
    CosmosDbEndpoint         = "${var.cosmosdb_endpoint}"
    CosmosDbKey              = "${var.cosmosdb_key}"
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = "${var.azure_location}"
  }
}

resource "azurerm_template_deployment" "media_repository_function_key" {
  name                = "mediarepositoryfunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.media_repository_api_function.name}"
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

output media_repository_url {
  value = "https://${azurerm_function_app.media_repository_api_function.default_hostname}/"
}

output "media_repository_key" {
  value = "${lookup(azurerm_template_deployment.media_repository_function_key.outputs, "functionkey")}"
}