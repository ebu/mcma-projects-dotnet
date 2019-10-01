resource "azurerm_storage_account" "app_storage_account" {
  name                     = "${var.global_prefix_lower_only}app"
  resource_group_name      = "${var.resource_group_name}"
  location                 = "${var.azure_location}"
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

data "azurerm_storage_account_sas" "app_storage_sas" {
  connection_string = "${azurerm_storage_account.app_storage_account.primary_connection_string}"
  https_only        = true

  resource_types {
    service   = false
    container = false
    object    = true
  }

  services {
    blob  = true
    queue = false
    table = false
    file  = false
  }

  start  = "2019-08-19"
  expiry = "2020-08-19"

  permissions {
    read    = true
    write   = false
    delete  = false
    list    = false
    add     = false
    create  = false
    update  = false
    process = false
  }
}

resource "azurerm_storage_container" "deploy_container" {
  name                  = "${var.deploy_container}"
  resource_group_name   = "${var.resource_group_name}"
  storage_account_name  = "${azurerm_storage_account.app_storage_account.name}"
  container_access_type = "private"
}

resource "azurerm_storage_account" "media_storage_account" {
  name                     = "${var.global_prefix_lower_only}media"
  resource_group_name      = "${var.resource_group_name}"
  location                 = "${var.azure_location}"
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "upload_container" {
  name                  = "${var.upload_container}"
  resource_group_name   = "${var.resource_group_name}"
  storage_account_name  = "${azurerm_storage_account.media_storage_account.name}"
  container_access_type = "private"
}

data "azurerm_storage_account_sas" "upload_container_sas" {
  connection_string = "${azurerm_storage_account.media_storage_account.primary_connection_string}"
  https_only        = true

  resource_types {
    service   = false
    container = true
    object    = true
  }

  services {
    blob  = true
    queue = false
    table = false
    file  = false
  }

  start  = "${formatdate("YYYY-MM-DD", timestamp())}"
  expiry = "${formatdate("YYYY-MM-DD", timeadd(timestamp(), "87600h"))}"

  permissions {
    read    = true
    write   = true
    delete  = false
    list    = true
    add     = true
    create  = true
    update  = false
    process = false
  }
}

resource "azurerm_storage_container" "temp_container" {
  name                  = "${var.temp_container}"
  resource_group_name   = "${var.resource_group_name}"
  storage_account_name  = "${azurerm_storage_account.media_storage_account.name}"
  container_access_type = "private"
}

resource "azurerm_storage_container" "repository_container" {
  name                  = "${var.repository_container}"
  resource_group_name   = "${var.resource_group_name}"
  storage_account_name  = "${azurerm_storage_account.media_storage_account.name}"
  container_access_type = "private"
}

resource "azurerm_storage_account" "website_storage_account" {
  name                     = "${var.global_prefix_lower_only}website"
  resource_group_name      = "${var.resource_group_name}"
  location                 = "${var.azure_location}"
  account_kind             = "StorageV2"
  account_tier             = "Standard"
  account_replication_type = "LRS"

  provisioner "local-exec" {
    command = "az login  --service-principal -u ${var.azure_client_id} -p ${var.azure_client_secret} --tenant ${var.azure_tenant_name} | az storage blob service-properties update --account-name ${azurerm_storage_account.website_storage_account.name} --static-website  --index-document index.html --404-document 404.html"
  }
}

resource "azurerm_storage_container" "website_container" {
  name                  = "${var.website_container}"
  resource_group_name   = "${var.resource_group_name}"
  storage_account_name  = "${azurerm_storage_account.website_storage_account.name}"
  container_access_type = "blob"
}

output "app_storage_connection_string" {
  value = "${azurerm_storage_account.app_storage_account.primary_connection_string}"
}

output "app_storage_account_name" {
  value = "${azurerm_storage_account.app_storage_account.name}"
}

output "app_storage_sas" {
  value = "${data.azurerm_storage_account_sas.app_storage_sas.sas}"
}

output "media_storage_connection_string" {
  value = "${azurerm_storage_account.media_storage_account.primary_connection_string}"
}

output "upload_container_sas" {
  value ="${data.azurerm_storage_account_sas.upload_container_sas.sas}"
}