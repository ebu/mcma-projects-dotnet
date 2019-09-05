variable "global_prefix" {}
variable "global_prefix_lower_only" {}
variable "upload_bucket" {}
variable "temp_bucket" {}
variable "repository_bucket" {}
variable "website_bucket" {}
variable "conform_workflow_id" {}
variable "ai_workflow_id" {}

variable "ec2_transform_service_hostname" {
  default = "localhost"
}

variable "aws_account_id" {}
variable "aws_access_key" {}
variable "aws_secret_key" {}
variable "aws_region" {}
variable "environment_name" {}
variable "environment_type" {}


variable private_encryption_key {}
variable "azure_location" {}
variable "resource_group_name" {}

variable "cosmosdb_endpoint" {}
variable "cosmosdb_key" {}

variable "app_storage_connection_string" {}
variable "app_storage_account_name" {}
variable "deploy_container" {}

variable "azure_videoindexer_location" {}
variable "azure_videoindexer_account_id" {}
variable "azure_videoindexer_subscription_key" {}
variable "azure_videoindexer_api_url" {}