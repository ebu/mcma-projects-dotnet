#===================================================================
# AWS SNS Topic, Subscription, and S3 Bucket Notifications
#===================================================================

provider "aws" {
  version = "~> 2.0"
  access_key = "${var.aws_access_key}"
  secret_key = "${var.aws_secret_key}"
  region = "${var.aws_region}"
}

resource "aws_s3_bucket" "aws_ai_input_bucket" {
  bucket = "${var.global_prefix}-${var.aws_region}-aws-ai-input"
  acl    = "private"
  force_destroy = true
}

resource "aws_s3_bucket" "aws_ai_output_bucket" {
  bucket = "${var.global_prefix}-${var.aws_region}-aws-ai-output"
  acl    = "private"
  force_destroy = true
}

resource "aws_sns_topic" "aws_ai_output_topic" {
  name = "${var.global_prefix}-${var.aws_region}-aws-ai-output"
  
  policy = <<POLICY
  {
    "Version": "2012-10-17",
    "Statement": [
      {
        "Sid": "s3bucket",
        "Effect": "Allow",
        "Principal": {
          "Service": "s3.amazonaws.com"
        },
        "Action": "sns:Publish",
        "Resource": "arn:aws:sns:*:*:${var.global_prefix}-${var.aws_region}-aws-ai-output",
        "Condition": {
            "ArnLike": { "aws:SourceArn": "${aws_s3_bucket.aws_ai_output_bucket.arn}" }
        }
      },
      {
        "Sid": "rekognition",
        "Effect": "Allow",
        "Principal": {
          "Service": "rekognition.amazonaws.com"
        },
        "Action": "sns:Publish",
        "Resource": "arn:aws:sns:*:*:${var.global_prefix}-${var.aws_region}-aws-ai-output"
      }
    ]
  }
  POLICY
}

resource "aws_s3_bucket_notification" "aws_ai_output_bucket_notification" {
  bucket = "${aws_s3_bucket.aws_ai_output_bucket.id}"

  topic {
    topic_arn     = "${aws_sns_topic.aws_ai_output_topic.arn}"
    events        = ["s3:ObjectCreated:*"]
  }
}

resource "aws_sns_topic_subscription" "aws_sns_topic_sub_lambda" {
  topic_arn = "${aws_sns_topic.aws_ai_output_topic.arn}"
  protocol = "https"
  endpoint = "https://${azurerm_function_app.aws_ai_service_api_function.default_hostname}/sns-notifications?code=${local.aws_ai_service_key}"
  endpoint_auto_confirms = true
}

resource "aws_iam_user" "aws_ai_user" {
  name = "${var.global_prefix}_aws_ai_user"
}

resource "aws_iam_user_policy" "aws_ai_user_policy" {
  name = "${var.global_prefix}_aws_ai_user_policy"
  user = "${aws_iam_user.aws_ai_user.name}"

  policy = <<POLICY
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "s3buckets",
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject"
      ],
      "Resource": "arn:aws:s3:::${var.global_prefix}-${var.aws_region}-aws-ai-*put/*"
    },
    {
      "Sid": "rekognition",
      "Effect": "Allow",
      "Action": "rekognition:StartCelebrityRecognition",
      "Resource": "*"
    },
    {
      "Sid": "transcribe",
      "Effect": "Allow",
      "Action": "transcribe:StartTranscriptionJob",
      "Resource": "*"
    },
    {
      "Sid": "translate",
      "Effect": "Allow",
      "Action": "translate:TranslateText",
      "Resource": "*"
    }
  ]
}
  POLICY
}

resource "aws_iam_access_key" "aws_ai_user_access_key" {
  user    = "${aws_iam_user.aws_ai_user.name}"
}


#===================================================================
# Azure Resources
#===================================================================

locals {
  aws_ai_service_api_zip_file    = "./../services/Mcma.Azure.AwsAiService/ApiHandler/dist/function.zip"
  aws_ai_service_worker_zip_file = "./../services/Mcma.Azure.AwsAiService/Worker/dist/function.zip"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "aws_ai_service_worker_function_queue" {
  name                 = "aws-ai-service-work-queue"
  resource_group_name  = "${var.resource_group_name}"
  storage_account_name = "${var.app_storage_account_name}"
}

resource "azurerm_storage_blob" "aws_ai_service_worker_function_zip" {
  name                   = "aws-ai-service/worker/function_${filesha256("${local.aws_ai_service_worker_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.aws_ai_service_worker_zip_file}"
}

resource "azurerm_application_insights" "aws_ai_service_worker_appinsights" {
  name                = "${var.global_prefix_lower_only}awsaiserviceworker_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "aws_ai_service_worker_function" {
  name                      = "${var.global_prefix_lower_only}awsaiserviceworker"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.aws_ai_service_worker_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.aws_ai_service_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.aws_ai_service_worker_appinsights.instrumentation_key}"

    WorkQueueStorage             = "${var.app_storage_connection_string}"
    FunctionKeyEncryptionKey     = "${var.private_encryption_key}"
    TableName                    = "AwsAiService"
    PublicUrl                    = "https://${var.global_prefix_lower_only}awsaiserviceworker.azurewebsites.net/"
    CosmosDbEndpoint             = "${var.cosmosdb_endpoint}"
    CosmosDbKey                  = "${var.cosmosdb_key}"
    CosmosDbDatabaseId           = "${var.global_prefix_lower_only}db"
    CosmosDbRegion               = "${var.azure_location}"
    ServicesUrl                  = "${local.services_url}"
    ServicesAuthType             = "AzureFunctionKey"
    ServicesAuthContext          = "{ \"functionKey\": \"${local.service_registry_key}\", \"isEncrypted\": false }"
    MediaStorageAccountName      = "${var.media_storage_account_name}"
    MediaStorageConnectionString = "${var.media_storage_connection_string}"
    AwsAccessKey                 = "${aws_iam_access_key.aws_ai_user_access_key.id}"
    AwsSecretKey                 = "${aws_iam_access_key.aws_ai_user_access_key.secret}"
  }
}

resource "azurerm_template_deployment" "aws_ai_service_worker_function_key" {
  name                = "awsaiserviceworkerfunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.aws_ai_service_worker_function.name}"
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

resource "azurerm_storage_blob" "aws_ai_service_api_function_zip" {
  name                   = "aws-ai-service/api/function_${filesha256("${local.aws_ai_service_api_zip_file}")}.zip"
  resource_group_name    = "${var.resource_group_name}"
  storage_account_name   = "${var.app_storage_account_name}"
  storage_container_name = "${var.deploy_container}"
  type                   = "block"
  source                 = "${local.aws_ai_service_api_zip_file}"
}

resource "azurerm_application_insights" "aws_ai_service_api_appinsights" {
  name                = "${var.global_prefix_lower_only}awsaiserviceapi_appinsights"
  resource_group_name = "${var.resource_group_name}"
  location            = "${var.azure_location}"
  application_type    = "Web"
}

resource "azurerm_function_app" "aws_ai_service_api_function" {
  name                      = "${var.global_prefix_lower_only}awsaiserviceapi"
  location                  = "${var.azure_location}"
  resource_group_name       = "${var.resource_group_name}"
  app_service_plan_id       = "${azurerm_app_service_plan.mcma_services.id}"
  storage_connection_string = "${var.app_storage_connection_string}"
  version                   = "~2"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = "${filesha256("${local.aws_ai_service_api_zip_file}")}"
    WEBSITE_RUN_FROM_PACKAGE       = "https://${var.app_storage_account_name}.blob.core.windows.net/${var.deploy_container}/${azurerm_storage_blob.aws_ai_service_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.aws_ai_service_api_appinsights.instrumentation_key}"

    FunctionKeyEncryptionKey = "${var.private_encryption_key}"
    TableName                = "AwsAiService"
    PublicUrl                = "https://${var.global_prefix_lower_only}awsaiserviceapi.azurewebsites.net/"
    CosmosDbEndpoint         = "${var.cosmosdb_endpoint}"
    CosmosDbKey              = "${var.cosmosdb_key}"
    CosmosDbDatabaseId       = "${var.global_prefix_lower_only}db"
    CosmosDbRegion           = "${var.azure_location}"
    WorkerFunctionId         = "${azurerm_storage_queue.aws_ai_service_worker_function_queue.name}"
  }
}

resource "azurerm_template_deployment" "aws_ai_service_api_function_key" {
  name                = "awsaiserviceapifunckeys"
  resource_group_name = "${var.resource_group_name}"
  deployment_mode     = "Incremental"

  parameters = {
    "functionApp" = "${azurerm_function_app.aws_ai_service_api_function.name}"
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

locals {
  aws_ai_service_key = "${lookup(azurerm_template_deployment.aws_ai_service_api_function_key.outputs, "functionkey")}"
}

output "aws_ai_service_url" {
  value = "https://${azurerm_function_app.aws_ai_service_api_function.default_hostname}/"
}

output "aws_ai_service_key" {
  value = "${local.aws_ai_service_key}"
}