#########################
# Provider registration 
#########################

provider "azurerm" {
  version = "~> 1.33.0"

  client_id       = var.azure_client_id
  client_secret   = var.azure_client_secret
  tenant_id       = var.azure_tenant_id
  subscription_id = var.azure_subscription_id
}

provider "azuread" {
  version = "= 0.6.0"

  client_id       = var.azure_client_id
  client_secret   = var.azure_client_secret
  tenant_id       = var.azure_tenant_id
  subscription_id = var.azure_subscription_id
}

resource "azurerm_resource_group" "resource_group" {
  name     = "${var.global_prefix}-rg"
  location = var.azure_location
}

resource "azurerm_cosmosdb_account" "cosmosdb_account" {
  name                = "${var.global_prefix}-cosmosdb-account"
  resource_group_name = azurerm_resource_group.resource_group.name
  location            = var.azure_location
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "BoundedStaleness"
  }

  geo_location {
    failover_priority = 0
    location          = var.azure_location
  }
}

module "storage" {
  source = "./storage"
  
  azure_client_id          = var.azure_client_id
  azure_client_secret      = var.azure_client_secret
  azure_tenant_id          = var.azure_tenant_id
  azure_location           = var.azure_location
  resource_group_name      = azurerm_resource_group.resource_group.name
  global_prefix            = var.global_prefix
  global_prefix_lower_only = var.global_prefix_lower_only

  deploy_container     = var.deploy_container
  upload_container     = var.upload_container
  temp_container       = var.temp_container
  repository_container = var.repository_container
  website_container    = var.website_container
  preview_container    = var.preview_container
}

module "workflows" {
  source = "./workflows"

  global_prefix         = var.global_prefix
  azure_subscription_id = var.azure_subscription_id
  azure_tenant_id       = var.azure_tenant_id
  azure_location        = var.azure_location
  resource_group_name   = azurerm_resource_group.resource_group.name

  media_storage_account_name = module.storage.media_storage_account_name
  media_storage_access_key   = module.storage.media_storage_access_key
  repository_container       = var.repository_container
  temp_container             = var.temp_container
  preview_container          = var.preview_container
}

module "services" {
  source = "./services"

  azure_subscription_id = var.azure_subscription_id
  azure_tenant_id       = var.azure_tenant_id
  azure_location        = var.azure_location

  environment_name         = var.environment_name
  environment_type         = var.environment_type
  global_prefix            = var.global_prefix
  resource_group_name      = azurerm_resource_group.resource_group.name
  resource_group_id        = azurerm_resource_group.resource_group.id

  cosmosdb_endpoint = azurerm_cosmosdb_account.cosmosdb_account.endpoint
  cosmosdb_key      = azurerm_cosmosdb_account.cosmosdb_account.primary_master_key

  app_storage_connection_string = module.storage.app_storage_connection_string
  app_storage_account_name      = module.storage.app_storage_account_name
  app_storage_sas               = module.storage.app_storage_sas
  deploy_container              = module.storage.deploy_container

  media_storage_connection_string = module.storage.media_storage_connection_string
  media_storage_account_name      = module.storage.media_storage_account_name

  website_domain = module.storage.website_domain

  azure_videoindexer_location         = var.azure_videoindexer_location
  azure_videoindexer_account_id       = var.azure_videoindexer_account_id
  azure_videoindexer_subscription_key = var.azure_videoindexer_subscription_key
  azure_videoindexer_api_url          = var.azure_videoindexer_api_url

  aws_access_key = var.aws_access_key
  aws_secret_key = var.aws_secret_key
  aws_region     = var.aws_region
}

module "website" {
  source = "./website"

  global_prefix = var.global_prefix

  resource_group_id = azurerm_resource_group.resource_group.id

  website_url    = module.storage.website_url
  website_domain = module.storage.website_domain

  service_registry_app_id = module.services.service_registry_app_id
  service_registry_scope  = module.services.service_registry_scope
  
  job_repository_app_id = module.services.job_repository_app_id
  job_repository_scope  = module.services.job_repository_scope
  
  media_repository_app_id = module.services.media_repository_app_id
  media_repository_scope  = module.services.media_repository_scope
}

output service_registry_url {
  value = module.services.service_registry_url
}

output service_registry_scope {
  value  = module.services.service_registry_scope
}

output service_registry_app_id {
  value = module.services.service_registry_app_id
}

output job_repository_url {
  value = module.services.job_repository_url
}

output job_repository_scope {
  value  = module.services.job_repository_scope
}

output job_repository_worker_url {
  value = module.services.job_repository_worker_url
}

output job_processor_url {
  value = module.services.job_processor_url
}

output job_processor_worker_url {
  value = module.services.job_processor_worker_url
}

output ame_service_url {
  value = module.services.ame_service_url
}

output ame_service_worker_url {
  value = module.services.ame_service_worker_url
}

output aws_ai_service_url {
  value = module.services.aws_ai_service_url
}

output aws_ai_service_worker_url {
  value = module.services.aws_ai_service_worker_url
}

output azure_ai_service_url {
  value = module.services.azure_ai_service_url
}

output azure_ai_service_worker_url {
  value = module.services.azure_ai_service_worker_url
}

output transform_service_url {
  value = module.services.transform_service_url
}

output transform_service_worker_url {
  value = module.services.transform_service_worker_url
}

output workflow_service_url {
  value = module.services.workflow_service_url
}

output workflow_service_worker_url {
  value = module.services.workflow_service_worker_url
}

output media_repository_url {
  value = module.services.media_repository_url
}

output media_storage_account_name {
  value = module.storage.media_storage_account_name
}

output media_storage_connection_string {
  value = module.storage.media_storage_connection_string
}

output upload_container {
  value = var.upload_container
}

output website_storage_connection_string {
  value = module.storage.website_storage_connection_string
}

output website_storage_account_name {
  value = module.storage.website_storage_account_name
}

output website_container {
  value = var.website_container
}

output website_domain {
  value = module.storage.website_domain
}

output website_url {
  value = module.storage.website_url
}

output website_client_id {
  value = module.website.website_client_id
}