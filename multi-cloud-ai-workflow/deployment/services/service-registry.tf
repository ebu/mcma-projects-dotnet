#################################
#  aws_lambda_function : service-registry-api-handler
#################################

resource "aws_lambda_function" "service-registry-api-handler" {
  filename         = "./../services/Mcma.Aws.ServiceRegistry/ApiHandler/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-service-registry-api-handler")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.ServiceRegistry.ApiHandler::Mcma.Aws.ServiceRegistry.ApiHandler.Function::Handler"
  source_code_hash = "${base64sha256(file("./../services/Mcma.Aws.ServiceRegistry/ApiHandler/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "30"
  memory_size      = "256"
}

##################################
# aws_dynamodb_table : service_registry_table
##################################

resource "aws_dynamodb_table" "service_registry_table" {
  name           = "${var.global_prefix}-service-registry"
  read_capacity  = 1
  write_capacity = 1
  hash_key       = "resource_type"
  range_key      = "resource_id"

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
#  aws_api_gateway_rest_api:  service_registry_api
##############################
resource "aws_api_gateway_rest_api" "service_registry_api" {
  name        = "${var.global_prefix}-service-registry"
  description = "Service Registry Rest Api"
}

resource "aws_api_gateway_resource" "service_registry_api_resource" {
  rest_api_id = "${aws_api_gateway_rest_api.service_registry_api.id}"
  parent_id   = "${aws_api_gateway_rest_api.service_registry_api.root_resource_id}"
  path_part   = "{proxy+}"
}

resource "aws_api_gateway_method" "service_registry_options_method" {
  rest_api_id   = "${aws_api_gateway_rest_api.service_registry_api.id}"
  resource_id   = "${aws_api_gateway_resource.service_registry_api_resource.id}"
  http_method   = "OPTIONS"
  authorization = "NONE"
}

resource "aws_api_gateway_method_response" "service_registry_options_200" {
  rest_api_id   = "${aws_api_gateway_rest_api.service_registry_api.id}"
  resource_id   = "${aws_api_gateway_resource.service_registry_api_resource.id}"
  http_method   = "${aws_api_gateway_method.service_registry_options_method.http_method}"
  status_code   = "200"

  response_models = {
      "application/json" = "Empty"
  }

  response_parameters = {
      "method.response.header.Access-Control-Allow-Headers" = true,
      "method.response.header.Access-Control-Allow-Methods" = true,
      "method.response.header.Access-Control-Allow-Origin"  = true
  }
}

resource "aws_api_gateway_integration" "service_registry_options_integration" {
  rest_api_id   = "${aws_api_gateway_rest_api.service_registry_api.id}"
  resource_id   = "${aws_api_gateway_resource.service_registry_api_resource.id}"
  http_method   = "${aws_api_gateway_method.service_registry_options_method.http_method}"
  type          = "MOCK"

  request_templates = {
    "application/json" = "{ \"statusCode\": 200 }"
  }
}

resource "aws_api_gateway_integration_response" "service_registry_options_integration_response" {
  rest_api_id   = "${aws_api_gateway_rest_api.service_registry_api.id}"
  resource_id   = "${aws_api_gateway_resource.service_registry_api_resource.id}"
  http_method   = "${aws_api_gateway_method.service_registry_options_method.http_method}"
  status_code   = "${aws_api_gateway_method_response.service_registry_options_200.status_code}"

  response_parameters = {
      "method.response.header.Access-Control-Allow-Headers" = "'Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token'",
      "method.response.header.Access-Control-Allow-Methods" = "'GET,OPTIONS,POST,PUT,PATCH,DELETE'",
      "method.response.header.Access-Control-Allow-Origin" = "'*'"
  }

  response_templates = {
    "application/json" = ""
  }
}

resource "aws_api_gateway_method" "service_registry_api_method" {
  rest_api_id   = "${aws_api_gateway_rest_api.service_registry_api.id}"
  resource_id   = "${aws_api_gateway_resource.service_registry_api_resource.id}"
  http_method   = "ANY"
  authorization = "AWS_IAM"
}

resource "aws_api_gateway_integration" "service_registry_api_method-integration" {
  rest_api_id             = "${aws_api_gateway_rest_api.service_registry_api.id}"
  resource_id             = "${aws_api_gateway_resource.service_registry_api_resource.id}"
  http_method             = "${aws_api_gateway_method.service_registry_api_method.http_method}"
  type                    = "AWS_PROXY"
  uri                     = "arn:aws:apigateway:${var.aws_region}:lambda:path/2015-03-31/functions/arn:aws:lambda:${var.aws_region}:${var.aws_account_id}:function:${aws_lambda_function.service-registry-api-handler.function_name}/invocations"
  integration_http_method = "POST"
}

resource "aws_lambda_permission" "apigw_service-registry-api-handler" {
  statement_id  = "AllowExecutionFromAPIGateway"
  action        = "lambda:InvokeFunction"
  function_name = "${aws_lambda_function.service-registry-api-handler.arn}"
  principal     = "apigateway.amazonaws.com"

  # More: http://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-control-access-using-iam-policies-to-invoke-api.html
  source_arn = "arn:aws:execute-api:${var.aws_region}:${var.aws_account_id}:${aws_api_gateway_rest_api.service_registry_api.id}/*/${aws_api_gateway_method.service_registry_api_method.http_method}/*"
}

resource "aws_api_gateway_deployment" "service_registry_deployment" {
  depends_on = [
    "aws_api_gateway_method.service_registry_api_method",
    "aws_api_gateway_integration.service_registry_api_method-integration",
  ]

  rest_api_id = "${aws_api_gateway_rest_api.service_registry_api.id}"
  stage_name  = "${var.environment_type}"

  variables = {
    "TableName" = "${var.global_prefix}-service-registry"
    "PublicUrl" = "${local.service_registry_url}"
    "DeploymentHash" = "${sha256(file("./services/service-registry.tf"))}"
  }
}

output "service_registry_url" {
  value = "${local.service_registry_url}"
}

output "services_auth_type" {
  value = "${local.services_auth_type}"
}

output "services_url" {
  value = "${local.services_url}"
}

locals {
  service_registry_url = "https://${aws_api_gateway_rest_api.service_registry_api.id}.execute-api.${var.aws_region}.amazonaws.com/${var.environment_type}"
  services_url          = "${local.service_registry_url}/services"
  services_auth_type    = "AWS4"
}
