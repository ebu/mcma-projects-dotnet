locals {
  job_repository_api_zip_file         = "./../services/Mcma.Azure.JobRepository/ApiHandler/dist/function.zip"
  job_repository_worker_zip_file      = "./../services/Mcma.Azure.JobRepository/Worker/dist/function.zip"
  job_repository_api_function_name    = "${var.global_prefix}-job-repository-api"
  job_repository_url                  = "https://${local.job_repository_api_function_name}.azurewebsites.net"
}

#===================================================================
# Worker Function
#===================================================================

resource "azurerm_storage_queue" "job_repository_worker_function_queue" {
  name                 = "job-repository-work-queue"
  storage_account_name = var.app_storage_account_name
}

resource "azurerm_storage_blob" "job_repository_worker_function_zip" {
  name                   = "job-repository/worker/function_${filesha256(local.job_repository_worker_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.job_repository_worker_zip_file
}

resource "azurerm_function_app" "job_repository_worker_function" {
  name                      = "${var.global_prefix}-job-repository-worker"
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
    HASH                           = filesha256(local.job_repository_worker_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.job_repository_worker_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    WorkQueueStorage         = var.app_storage_connection_string
    TableName                = "JobRepository"
    PublicUrl                = local.job_repository_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = local.cosmosdb_id
    CosmosDbRegion           = var.azure_location
    ServicesUrl              = local.services_url
    ServicesAuthType         = "AzureAD"
    ServicesAuthContext      = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
  }

  provisioner "local-exec" {
    command = "az webapp start --name ${azurerm_function_app.job_repository_worker_function.name}"
  }
}

#===================================================================
# API Function
#===================================================================

resource "azuread_application" "job_repository_app" {
  name            = local.job_repository_api_function_name
  identifier_uris = [local.job_repository_url]
}

resource "azuread_service_principal" "job_repository_sp" {
  application_id               = azuread_application.job_repository_app.application_id
  app_role_assignment_required = false
}

resource "azurerm_storage_blob" "job_repository_api_function_zip" {
  name                   = "job-repository/api/function_${filesha256(local.job_repository_api_zip_file)}.zip"
  resource_group_name    = var.resource_group_name
  storage_account_name   = var.app_storage_account_name
  storage_container_name = var.deploy_container
  type                   = "block"
  source                 = local.job_repository_api_zip_file
}

resource "azurerm_function_app" "job_repository_api_function" {
  name                      = local.job_repository_api_function_name
  location                  = var.azure_location
  resource_group_name       = var.resource_group_name
  app_service_plan_id       = azurerm_app_service_plan.mcma_services.id
  storage_connection_string = var.app_storage_connection_string
  version                   = "~2"

  site_config {
    cors {
      allowed_origins = [
        "https://${var.website_domain}",
        "http://localhost:4200"
      ]
      support_credentials = true
    }
  }

  auth_settings {
    enabled                       = true
    issuer                        = "https://sts.windows.net/${var.azure_tenant_id}"
    default_provider              = "AzureActiveDirectory"
    unauthenticated_client_action = "RedirectToLoginPage"
    active_directory {
      client_id         = azuread_application.job_repository_app.application_id
      allowed_audiences = [local.job_repository_url]
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.job_repository_api_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${local.deploy_container_url}/${azurerm_storage_blob.job_repository_api_function_zip.name}${var.app_storage_sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = azurerm_application_insights.services_appinsights.instrumentation_key

    TableName                = "JobRepository"
    PublicUrl                = local.job_repository_url
    CosmosDbEndpoint         = var.cosmosdb_endpoint
    CosmosDbKey              = var.cosmosdb_key
    CosmosDbDatabaseId       = local.cosmosdb_id
    CosmosDbRegion           = var.azure_location
    WorkerFunctionId         = azurerm_storage_queue.job_repository_worker_function_queue.name
  }
}

output job_repository_url {
  value = "${local.job_repository_url}/"
}

output job_repository_app_id {
  value = azuread_application.job_repository_app.application_id
}

output job_repository_scope {
  value = azuread_application.job_repository_app.oauth2_permissions[0].id
}
