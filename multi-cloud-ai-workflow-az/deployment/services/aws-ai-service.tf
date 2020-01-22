#===================================================================
# Azure Resources
#===================================================================
locals {
  aws_ai_service_api_zip_file         = "./../services/Mcma.Azure.AwsAiService/ApiHandler/dist/function.zip"
  aws_ai_service_worker_zip_file      = "./../services/Mcma.Azure.AwsAiService/Worker/dist/function.zip"
  aws_ai_service_sns_zip_file         = "./../services/Mcma.Azure.AwsAiService/Sns/dist/function.zip"
  aws_ai_service_api_function_name    = "${var.global_prefix}-aws-ai-service-api"
  aws_ai_service_url                  = "https://${local.aws_ai_service_api_function_name}.azurewebsites.net"
  aws_ai_service_sns_func_key         = "${lookup(azurerm_template_deployment.aws_ai_service_sns_func_key.outputs, "functionkey")}"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "aws_ai_service_worker_function_queue" {
  name                 = "aws-ai-service-work-queue"
  storage_account_name = var.app_storage_account_name
}

resource "azurerm_storage_blob" "aws_ai_service_worker_function_zip" {
  name                   = "aws-ai-service/worker/function_${filesha256(local.aws_ai_service_worker_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.aws_ai_service_worker_zip_file
}

resource "azurerm_function_app" "aws_ai_service_worker_function" {
  name                      = "${var.global_prefix}-aws-ai-service-worker"
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
    HASH                           = filesha256(local.aws_ai_service_worker_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.aws_ai_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    WorkQueueStorage             = var.app_storage_connection_string
    TableName                    = "AwsAiService"
    PublicUrl                    = local.aws_ai_service_url
    CosmosDbEndpoint             = var.cosmosdb_endpoint
    CosmosDbKey                  = var.cosmosdb_key
    CosmosDbDatabaseId           = local.cosmosdb_id
    CosmosDbRegion               = var.azure_location
    ServicesUrl                  = local.services_url
    ServicesAuthType             = "AzureAD"
    ServicesAuthContext          = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
    MediaStorageAccountName      = var.media_storage_account_name
    MediaStorageConnectionString = var.media_storage_connection_string
    AwsAccessKey                 = aws_iam_access_key.aws_ai_user_access_key.id
    AwsSecretKey                 = aws_iam_access_key.aws_ai_user_access_key.secret
    AwsRegion                    = var.aws_region
    AwsAiOutputSnsTopicArn       = aws_sns_topic.aws_ai_output_topic.arn
    AwsAiInputBucket             = aws_s3_bucket.aws_ai_input_bucket.id
    AwsAiOutputBucket            = aws_s3_bucket.aws_ai_output_bucket.id
    AwsRekoSnsRoleArn            = aws_iam_role.aws_reko_sns_role.arn
  }

  provisioner "local-exec" {
    command = "az webapp start --name ${azurerm_function_app.aws_ai_service_worker_function.name}"
  }
}

#===================================================================
# API Function
#===================================================================

resource "azuread_application" "aws_ai_service_app" {
  name            = local.aws_ai_service_api_function_name
  identifier_uris = [local.aws_ai_service_url]
}

resource "azuread_service_principal" "aws_ai_service_sp" {
  application_id               = azuread_application.aws_ai_service_app.application_id
  app_role_assignment_required = false
}

resource "azurerm_storage_blob" "aws_ai_service_api_function_zip" {
  name                   = "aws-ai-service/api/function_${filesha256(local.aws_ai_service_api_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.aws_ai_service_api_zip_file
}

resource "azurerm_function_app" "aws_ai_service_api_function" {
  name                      = local.aws_ai_service_api_function_name
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
      client_id         = azuread_application.aws_ai_service_app.application_id
      allowed_audiences = [local.aws_ai_service_url]
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.aws_ai_service_api_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.aws_ai_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    TableName                = "AwsAiService"
    PublicUrl                = local.aws_ai_service_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = local.cosmosdb_id
    CosmosDbRegion           = var.azure_location
    WorkerFunctionId         = azurerm_storage_queue.aws_ai_service_worker_function_queue.name
  }
}

#===================================================================
# SNS Callback Function
#===================================================================

resource "azurerm_storage_blob" "aws_ai_service_sns_function_zip" {
  name                   = "aws-ai-service/sns/function_${filesha256(local.aws_ai_service_sns_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.aws_ai_service_sns_zip_file
}

resource "azurerm_function_app" "aws_ai_service_sns_function" {
  name                      = "${var.global_prefix}-aws-ai-service-sns"
  location                  = var.azure_location
  resource_group_name       = var.resource_group_name
  app_service_plan_id       = azurerm_app_service_plan.mcma_services.id
  storage_connection_string = var.app_storage_connection_string
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.aws_ai_service_sns_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.aws_ai_service_sns_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    TableName                = "AwsAiService"
    PublicUrl                = local.aws_ai_service_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = local.cosmosdb_id
    CosmosDbRegion           = var.azure_location
    WorkerFunctionId         = azurerm_storage_queue.aws_ai_service_worker_function_queue.name
  }
}

resource "azurerm_template_deployment" "aws_ai_service_sns_func_key" {
  name                = "awsaiservicesnsfunckeys"
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"

  parameters = {
    functionApp = azurerm_function_app.aws_ai_service_sns_function.name
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

output aws_ai_service_url {
  value = "${local.aws_ai_service_url}/"
}
