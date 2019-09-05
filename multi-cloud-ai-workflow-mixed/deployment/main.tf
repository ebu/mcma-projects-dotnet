#########################
# Provider registration 
#########################

provider "aws" {
  version = "~> 2.7"

  access_key = "${var.aws_access_key}"
  secret_key = "${var.aws_secret_key}"
  region     = "${var.aws_region}"
}

#########################
# Module registration 
# Run a terraform get on each module before executing this script
#########################

module "cognito" {
  source = "./cognito"

  global_prefix = "${var.global_prefix}"

  upload_bucket     = "${var.upload_bucket}"
  temp_bucket       = "${var.temp_bucket}"
  repository_bucket = "${var.repository_bucket}"
  website_bucket    = "${var.website_bucket}"

  aws_account_id = "${var.aws_account_id}"
  aws_access_key = "${var.aws_access_key}"
  aws_secret_key = "${var.aws_secret_key}"
  aws_region     = "${var.aws_region}"
}

module "storage" {
  source = "./storage"

  global_prefix            = "${var.global_prefix}"
  global_prefix_lower_only = "${var.global_prefix_lower_only}"

  upload_bucket     = "${var.upload_bucket}"
  temp_bucket       = "${var.temp_bucket}"
  repository_bucket = "${var.repository_bucket}"
  website_bucket    = "${var.website_bucket}"

  aws_account_id = "${var.aws_account_id}"
  aws_access_key = "${var.aws_access_key}"
  aws_secret_key = "${var.aws_secret_key}"
  aws_region     = "${var.aws_region}"

  resource_group_name = "${azurerm_resource_group.resource_group.name}"
  azure_client_id     = "${var.azure_client_id}"
  azure_client_secret = "${var.azure_client_secret}"
  azure_tenant_name   = "${var.azure_tenant_name}"
  azure_location      = "${var.azure_location}"

  deploy_container = "${var.deploy_container}"
}

module "services" {
  source = "./services"

  environment_name         = "${var.environment_name}"
  environment_type         = "${var.environment_type}"
  global_prefix            = "${var.global_prefix}"
  global_prefix_lower_only = "${var.global_prefix_lower_only}"

  upload_bucket     = "${var.upload_bucket}"
  temp_bucket       = "${var.temp_bucket}"
  repository_bucket = "${var.repository_bucket}"
  website_bucket    = "${var.website_bucket}"

  conform_workflow_id = "${module.workflows.conform_workflow_id}"
  ai_workflow_id      = "${module.workflows.ai_workflow_id}"

  # Uncomment if you want to include the ec2 transform service in the deployment
  # ec2_transform_service_hostname = "${module.ec2.elb.hostname}"

  aws_account_id = "${var.aws_account_id}"
  aws_access_key = "${var.aws_access_key}"
  aws_secret_key = "${var.aws_secret_key}"
  aws_region     = "${var.aws_region}"

  private_encryption_key = "${var.private_encryption_key}"

  azure_location      = "${var.azure_location}"
  resource_group_name = "${azurerm_resource_group.resource_group.name}"

  cosmosdb_endpoint = "${azurerm_cosmosdb_account.cosmosdb_account.endpoint}"
  cosmosdb_key      = "${azurerm_cosmosdb_account.cosmosdb_account.primary_master_key}"

  app_storage_connection_string = "${module.storage.app_storage_connection_string}"
  app_storage_account_name      = "${module.storage.app_storage_account_name}"
  deploy_container              = "${var.deploy_container}"

  azure_videoindexer_location         = "${var.azure_videoindexer_location}"
  azure_videoindexer_account_id       = "${var.azure_videoindexer_account_id}"
  azure_videoindexer_subscription_key = "${var.azure_videoindexer_subscription_key}"
  azure_videoindexer_api_url          = "${var.azure_videoindexer_api_url}"
}

module "workflows" {
  source = "./workflows"

  environment_type   = "${var.environment_type}"
  global_prefix      = "${var.global_prefix}"
  services_url       = "${module.services.services_url}"
  services_auth_type = "${module.services.services_auth_type}"

  upload_bucket     = "${var.upload_bucket}"
  temp_bucket       = "${var.temp_bucket}"
  repository_bucket = "${var.repository_bucket}"
  website_bucket    = "${var.website_bucket}"

  aws_account_id = "${var.aws_account_id}"
  aws_access_key = "${var.aws_access_key}"
  aws_secret_key = "${var.aws_secret_key}"
  aws_region     = "${var.aws_region}"
  
  private_encryption_key = "${var.private_encryption_key}"
}

# Uncomment if you want to include the ec2 transform service in the deployment
# module "ec2" {
#   source = "./ec2"

#   global_prefix = "${var.global_prefix}"

#   aws_account_id     = "${var.aws_account_id}"
#   aws_access_key     = "${var.aws_access_key}"
#   aws_secret_key     = "${var.aws_secret_key}"
#   aws_region         = "${var.aws_region}"
#   aws_instance_type  = "${var.aws_instance_type}"
#   aws_instance_count = "${var.aws_instance_count}"

#   services_url          = "${module.services.services_url}"
#   services_auth_type    = "${module.services.services_auth_type}"
# }

output "aws_region" {
  value = "${var.aws_region}"
}

output "cognito_user_pool_id" {
  value = "${module.cognito.user_pool_id}"
}

output "cognito_user_pool_client_id" {
  value = "${module.cognito.user_pool_client_id}"
}

output "cognito_identity_pool_id" {
  value = "${module.cognito.identity_pool_id}"
}

output "upload_bucket" {
  value = "${module.storage.upload_bucket}"
}

output "website_bucket" {
  value = "${module.storage.website_bucket}"
}

output "website_url" {
  value = "${module.storage.website_url}"
}

output "service_registry_url" {
  value = "${module.services.service_registry_url}"
}

output "services_url" {
  value = "${module.services.services_url}"
}

output "services_auth_type" {
  value = "${module.services.services_auth_type}"
}

output "media_repository_url" {
  value = "${module.services.media_repository_url}"
}

output "media_repository_key" {
  value = "${module.services.media_repository_key}"
}

output "job_repository_url" {
  value = "${module.services.job_repository_url}"
}

output "job_processor_service_url" {
  value = "${module.services.job_processor_service_url}"
}

output "ame_service_url" {
  value = "${module.services.ame_service_url}"
}

output "workflow_service_url" {
  value = "${module.services.workflow_service_url}"
}

output "workflow_service_notification_url" {
  value = "${module.workflows.workflow_service_notification_url}"
}

output "transform_service_url" {
  value = "${module.services.transform_service_url}"
}

output "aws_ai_service_url" {
  value = "${module.services.aws_ai_service_url}"
}

output "azure_ai_service_url" {
  value = "${module.services.azure_ai_service_url}"
}

# Uncomment if you want to include the ec2 transform service in the deployment
# output "ec2_transform_service_hostname" {
#   value = "${module.ec2.elb.hostname}"
# }
