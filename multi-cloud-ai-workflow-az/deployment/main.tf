#########################
# Provider registration 
#########################

provider "azurerm" {
  version = "~> 1.33.0"

  client_id       = "${var.azure_client_id}"
  client_secret   = "${var.azure_client_secret}"
  tenant_id       = "${var.azure_tenant_id}"
  subscription_id = "${var.azure_subscription_id}"
}

resource "azurerm_resource_group" "resource_group" {
  name     = "${var.global_prefix}-rg"
  location = "${var.azure_location}"
}

resource "azurerm_cosmosdb_account" "cosmosdb_account" {
  name                = "${var.global_prefix}-cosmosdb-account"
  resource_group_name = "${azurerm_resource_group.resource_group.name}"
  location            = "${var.azure_location}"
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "BoundedStaleness"
  }

  geo_location {
    failover_priority = 0
    location          = "${var.azure_location}"
  }
}

module "storage" {
  source = "./storage"

  global_prefix_lower_only = "${var.global_prefix_lower_only}"

  azure_client_id     = "${var.azure_client_id}"
  azure_client_secret = "${var.azure_client_secret}"
  azure_tenant_name   = "${var.azure_tenant_name}"
  azure_location      = "${var.azure_location}"

  resource_group_name = "${azurerm_resource_group.resource_group.name}"

  deploy_container     = "${var.deploy_container}"
  upload_container     = "${var.upload_container}"
  temp_container       = "${var.temp_container}"
  repository_container = "${var.repository_container}"
  website_container    = "${var.website_container}"
}

module "services" {
  source = "./services"

  private_encryption_key = "${var.private_encryption_key}"

  environment_name         = "${var.environment_name}"
  environment_type         = "${var.environment_type}"
  azure_location           = "${var.azure_location}"
  global_prefix            = "${var.global_prefix}"
  global_prefix_lower_only = "${var.global_prefix_lower_only}"
  resource_group_name      = "${azurerm_resource_group.resource_group.name}"

  cosmosdb_endpoint = "${azurerm_cosmosdb_account.cosmosdb_account.endpoint}"
  cosmosdb_key      = "${azurerm_cosmosdb_account.cosmosdb_account.primary_master_key}"

  app_storage_connection_string = "${module.storage.app_storage_connection_string}"
  app_storage_account_name      = "${module.storage.app_storage_account_name}"
  app_storage_sas               = "${module.storage.app_storage_sas}"
  deploy_container              = "${var.deploy_container}"

  media_storage_connection_string = "${module.storage.media_storage_connection_string}"
  media_storage_account_name      = "${module.storage.media_storage_account_name}"
  upload_container                = "${var.upload_container}"
  temp_container                  = "${var.temp_container}"
  repository_container            = "${var.repository_container}"
  website_container               = "${var.website_container}"

  azure_videoindexer_location         = "${var.azure_videoindexer_location}"
  azure_videoindexer_account_id       = "${var.azure_videoindexer_account_id}"
  azure_videoindexer_subscription_key = "${var.azure_videoindexer_subscription_key}"
  azure_videoindexer_api_url          = "${var.azure_videoindexer_api_url}"

  aws_access_key = "${var.aws_access_key}"
  aws_secret_key = "${var.aws_secret_key}"
  aws_region     = "${var.aws_region}"

  #conform_workflow_id = "${module.workflows.conform_workflow_id}"
  #ai_workflow_id      = "${module.workflows.ai_workflow_id}"
}

output "services_url" {
  value = "${module.services.services_url}"
}

output "service_registry_url" {
  value = "${module.services.service_registry_url}"
}

output "service_registry_key" {
  value = "${module.services.service_registry_key}"
}

output "job_repository_url" {
  value = "${module.services.job_repository_url}"
}

output "job_repository_key" {
  value = "${module.services.job_repository_key}"
}

output "job_processor_url" {
  value = "${module.services.job_processor_url}"
}

output "job_processor_key" {
  value = "${module.services.job_processor_key}"
}

output "ame_service_url" {
  value = "${module.services.ame_service_url}"
}

output "ame_service_key" {
  value = "${module.services.ame_service_key}"
}

output "aws_ai_service_url" {
  value = "${module.services.aws_ai_service_url}"
}

output "aws_ai_service_key" {
  value = "${module.services.aws_ai_service_key}"
}

output "azure_ai_service_url" {
  value = "${module.services.azure_ai_service_url}"
}

output "azure_ai_service_key" {
  value = "${module.services.azure_ai_service_key}"
}
