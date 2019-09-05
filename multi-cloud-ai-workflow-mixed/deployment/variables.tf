#########################
# Environment Variables
#########################

variable "environment_name" {}
variable "environment_type" {}

variable "global_prefix" {}
variable "global_prefix_lower_only" {}

#########################
# AWS Variables
#########################

variable "aws_account_id" {}
variable "aws_access_key" {}
variable "aws_secret_key" {}
variable "aws_region" {}
variable "aws_instance_type" {}
variable "aws_instance_count" {}

#########################
# Storage Variables
#########################

variable "upload_bucket" {}
variable "temp_bucket" {}
variable "repository_bucket" {}
variable "website_bucket" {}

#########################
# Azure Variables
#########################

variable private_encryption_key {}
variable "azure_client_id" {}
variable "azure_client_secret" {}
variable "azure_tenant_id" {}
variable "azure_tenant_name" {}
variable "azure_subscription_id" {}
variable "azure_location" {}

variable "deploy_container" {}

variable "azure_videoindexer_location" {}
variable "azure_videoindexer_account_id" {}
variable "azure_videoindexer_subscription_key" {}
variable "azure_videoindexer_api_url" {}
