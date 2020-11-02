locals {
  mediainfo_service_dir          = "../services/MediaInfoService"
  mediainfo_api_handler_zip_file = "${local.mediainfo_service_dir}/Mcma.Aws.MediaInfoService.ApiHandler/dist/function.zip"
  mediainfo_layer_zip_file       = "${local.mediainfo_service_dir}/MediaInfo/MediaInfo_CLI_19.09_Lambda_AL2.zip"
  mediainfo_worker_zip_file      = "${local.mediainfo_service_dir}/Mcma.Aws.MediaInfoService.Worker/dist/function.zip"
}

#################################
#  aws_lambda_function : mediainfo-service-api-handler
#################################

resource "aws_lambda_function" "mediainfo_service_api_handler" {
  filename         = local.mediainfo_api_handler_zip_file
  function_name    = format("%.64s", "${var.global_prefix}-mediainfo-service-api-handler")
  role             = aws_iam_role.iam_for_exec_lambda.arn
  handler          = "Mcma.Aws.MediaInfoService.ApiHandler::Mcma.Aws.MediaInfoService.ApiHandler.MediaInfoServiceApiHandler::ExecuteAsync"
  source_code_hash = filebase64sha256(local.mediainfo_api_handler_zip_file)
  runtime          = "dotnetcore3.1"
  timeout          = "30"
  memory_size      = "3008"

  environment {
    variables = {
      MCMA_LOG_GROUP_NAME              = var.global_prefix
      MCMA_TABLE_NAME                  = aws_dynamodb_table.mediainfo_service_table.name
      MCMA_PUBLIC_URL                  = local.mediainfo_service_url
      MCMA_SERVICES_URL                = local.services_url
      MCMA_SERVICES_AUTH_TYPE          = local.service_registry_auth_type
      MCMA_WORKER_LAMBDA_FUNCTION_NAME = aws_lambda_function.mediainfo_service_worker.function_name
    }
  }
}

#################################
#  aws_lambda_function : mediainfo-service-worker
#################################

resource "aws_lambda_layer_version" "mediainfo" {
  filename         = local.mediainfo_layer_zip_file
  layer_name       = "${var.global_prefix}-mediainfo-service-mediainfo"
  source_code_hash = filebase64sha256(local.mediainfo_layer_zip_file)
}

resource "aws_lambda_function" "mediainfo_service_worker" {
  filename         = local.mediainfo_worker_zip_file
  function_name    = format("%.64s", "${var.global_prefix}-mediainfo-service-worker")
  role             = aws_iam_role.iam_for_exec_lambda.arn
  handler          = "Mcma.Aws.MediaInfoService.Worker::Mcma.Aws.MediaInfoService.Worker.MediaInfoServiceWorker::ExecuteAsync"
  source_code_hash = filebase64sha256(local.mediainfo_worker_zip_file)
  runtime          = "dotnetcore3.1"
  timeout          = "900"
  memory_size      = "3008"

  layers = [aws_lambda_layer_version.mediainfo.arn]

  environment {
    variables = {
      MCMA_LOG_GROUP_NAME     = var.global_prefix
      MCMA_TABLE_NAME         = aws_dynamodb_table.mediainfo_service_table.name
      MCMA_PUBLIC_URL         = local.mediainfo_service_url
      MCMA_SERVICES_URL       = local.services_url
      MCMA_SERVICES_AUTH_TYPE = local.service_registry_auth_type
    }
  }
}

##################################
# aws_dynamodb_table : mediainfo_service_table
##################################

resource "aws_dynamodb_table" "mediainfo_service_table" {
  name         = "${var.global_prefix}-mediainfo-service"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "resource_type"
  range_key    = "resource_id"

  attribute {
    name = "resource_type"
    type = "S"
  }

  attribute {
    name = "resource_id"
    type = "S"
  }

  stream_enabled   = true
  stream_view_type = "NEW_IMAGE"
}

##############################
#  aws_api_gateway_rest_api:  mediainfo_service_api
##############################
resource "aws_api_gateway_rest_api" "mediainfo_service_api" {
  name        = "${var.global_prefix}-mediainfo-service"
  description = "MediaInfo Service REST API"

  endpoint_configuration {
    types = ["REGIONAL"]
  }
}

resource "aws_api_gateway_resource" "mediainfo_service_api_resource" {
  rest_api_id = aws_api_gateway_rest_api.mediainfo_service_api.id
  parent_id   = aws_api_gateway_rest_api.mediainfo_service_api.root_resource_id
  path_part   = "{proxy+}"
}

resource "aws_api_gateway_method" "mediainfo_service_options_method" {
  rest_api_id   = aws_api_gateway_rest_api.mediainfo_service_api.id
  resource_id   = aws_api_gateway_resource.mediainfo_service_api_resource.id
  http_method   = "OPTIONS"
  authorization = "NONE"
}

resource "aws_api_gateway_method_response" "mediainfo_service_options_200" {
  rest_api_id = aws_api_gateway_rest_api.mediainfo_service_api.id
  resource_id = aws_api_gateway_resource.mediainfo_service_api_resource.id
  http_method = aws_api_gateway_method.mediainfo_service_options_method.http_method
  status_code = "200"

  response_models = {
    "application/json" = "Empty"
  }

  response_parameters = {
    "method.response.header.Access-Control-Allow-Headers" = true
    "method.response.header.Access-Control-Allow-Methods" = true
    "method.response.header.Access-Control-Allow-Origin"  = true
  }
}

resource "aws_api_gateway_integration" "mediainfo_service_options_integration" {
  rest_api_id = aws_api_gateway_rest_api.mediainfo_service_api.id
  resource_id = aws_api_gateway_resource.mediainfo_service_api_resource.id
  http_method = aws_api_gateway_method.mediainfo_service_options_method.http_method
  type        = "MOCK"

  request_templates = {
    "application/json" = "{ \"statusCode\": 200 }"
  }
}

resource "aws_api_gateway_integration_response" "mediainfo_service_options_integration_response" {
  rest_api_id = aws_api_gateway_rest_api.mediainfo_service_api.id
  resource_id = aws_api_gateway_resource.mediainfo_service_api_resource.id
  http_method = aws_api_gateway_method.mediainfo_service_options_method.http_method
  status_code = aws_api_gateway_method_response.mediainfo_service_options_200.status_code

  response_parameters = {
    "method.response.header.Access-Control-Allow-Headers" = "'Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token'"
    "method.response.header.Access-Control-Allow-Methods" = "'GET,OPTIONS,POST,PUT,PATCH,DELETE'"
    "method.response.header.Access-Control-Allow-Origin"  = "'*'"
  }

  response_templates = {
    "application/json" = ""
  }
}

resource "aws_api_gateway_method" "mediainfo_service_api_method" {
  rest_api_id   = aws_api_gateway_rest_api.mediainfo_service_api.id
  resource_id   = aws_api_gateway_resource.mediainfo_service_api_resource.id
  http_method   = "ANY"
  authorization = "AWS_IAM"
}

resource "aws_api_gateway_integration" "mediainfo_service_api_method_integration" {
  rest_api_id             = aws_api_gateway_rest_api.mediainfo_service_api.id
  resource_id             = aws_api_gateway_resource.mediainfo_service_api_resource.id
  http_method             = aws_api_gateway_method.mediainfo_service_api_method.http_method
  type                    = "AWS_PROXY"
  uri                     = "arn:aws:apigateway:${var.aws_region}:lambda:path/2015-03-31/functions/arn:aws:lambda:${var.aws_region}:${var.aws_account_id}:function:${aws_lambda_function.mediainfo_service_api_handler.function_name}/invocations"
  integration_http_method = "POST"
}

resource "aws_lambda_permission" "apigw_mediainfo_service_api_handler" {
  statement_id  = "AllowExecutionFromAPIGateway"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.mediainfo_service_api_handler.arn
  principal     = "apigateway.amazonaws.com"
  source_arn    = "arn:aws:execute-api:${var.aws_region}:${var.aws_account_id}:${aws_api_gateway_rest_api.mediainfo_service_api.id}/*/${aws_api_gateway_method.mediainfo_service_api_method.http_method}/*"
}

resource "aws_api_gateway_deployment" "mediainfo_service_deployment" {
  depends_on = [
    aws_api_gateway_integration.mediainfo_service_api_method_integration,
    aws_api_gateway_integration.mediainfo_service_options_integration,
    aws_api_gateway_integration_response.mediainfo_service_options_integration_response,
  ]

  rest_api_id = aws_api_gateway_rest_api.mediainfo_service_api.id
}

resource "aws_api_gateway_stage" "mediainfo_service_gateway_stage" {
  depends_on = [
    aws_api_gateway_integration.mediainfo_service_api_method_integration,
    aws_api_gateway_integration.mediainfo_service_options_integration,
    aws_api_gateway_integration_response.mediainfo_service_options_integration_response,
  ]

  stage_name    = var.environment_type
  deployment_id = aws_api_gateway_deployment.mediainfo_service_deployment.id
  rest_api_id   = aws_api_gateway_rest_api.mediainfo_service_api.id

  variables = {
    DeploymentHash = filesha256("./services/mediainfo-service.tf")
  }
}

locals {
  mediainfo_service_url = "https://${aws_api_gateway_rest_api.mediainfo_service_api.id}.execute-api.${var.aws_region}.amazonaws.com/${var.environment_type}"
}
