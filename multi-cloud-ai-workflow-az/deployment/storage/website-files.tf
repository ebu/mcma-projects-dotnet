resource "azurerm_storage_blob" "website_file_M9w4NqW9tXVfHeMbhiU7Xw--" {
  name                     = "3rdpartylicenses.txt"
  resource_group_name      = var.resource_group_name
  storage_account_name     = azurerm_storage_account.website_storage_account.name
  storage_container_name   = azurerm_storage_container.website_container.name
  type                     = "block"
  source                   = "../website/dist/website/3rdpartylicenses.txt"
  content_type             = "text/plain"
}

resource "azurerm_storage_blob" "website_file_uap8M4aTQkqumVmb7IdbXw--" {
  name                     = "favicon.ico"
  resource_group_name      = var.resource_group_name
  storage_account_name     = azurerm_storage_account.website_storage_account.name
  storage_container_name   = azurerm_storage_container.website_container.name
  type                     = "block"
  source                   = "../website/dist/website/favicon.ico"
  content_type             = "image/x-icon"
}

resource "azurerm_storage_blob" "website_file_FoIPwznGIGC-u9qAaTFP1Q--" {
  name                     = "index.html"
  resource_group_name      = var.resource_group_name
  storage_account_name     = azurerm_storage_account.website_storage_account.name
  storage_container_name   = azurerm_storage_container.website_container.name
  type                     = "block"
  source                   = "../website/dist/website/index.html"
  content_type             = "text/html"
}

resource "azurerm_storage_blob" "website_file_qbVNHFsrPOsL58r6O7rJXA--" {
  name                     = "main.bf60fad597ab79dbacbf.js"
  resource_group_name      = var.resource_group_name
  storage_account_name     = azurerm_storage_account.website_storage_account.name
  storage_container_name   = azurerm_storage_container.website_container.name
  type                     = "block"
  source                   = "../website/dist/website/main.bf60fad597ab79dbacbf.js"
  content_type             = "application/javascript"
}

resource "azurerm_storage_blob" "website_file_x-3W1RTsXKBZTMZFku-8-w--" {
  name                     = "polyfills-es5.7caebd67fb3e038e37b6.js"
  resource_group_name      = var.resource_group_name
  storage_account_name     = azurerm_storage_account.website_storage_account.name
  storage_container_name   = azurerm_storage_container.website_container.name
  type                     = "block"
  source                   = "../website/dist/website/polyfills-es5.7caebd67fb3e038e37b6.js"
  content_type             = "application/javascript"
}

resource "azurerm_storage_blob" "website_file_lcTqpmgAII93wdRSg8mHwQ--" {
  name                     = "polyfills.4d333e5632ea98e09193.js"
  resource_group_name      = var.resource_group_name
  storage_account_name     = azurerm_storage_account.website_storage_account.name
  storage_container_name   = azurerm_storage_container.website_container.name
  type                     = "block"
  source                   = "../website/dist/website/polyfills.4d333e5632ea98e09193.js"
  content_type             = "application/javascript"
}

resource "azurerm_storage_blob" "website_file_EkTT8vKOzGYZFXknrKlSAA--" {
  name                     = "runtime.a8ef3a8272419c2e2c66.js"
  resource_group_name      = var.resource_group_name
  storage_account_name     = azurerm_storage_account.website_storage_account.name
  storage_container_name   = azurerm_storage_container.website_container.name
  type                     = "block"
  source                   = "../website/dist/website/runtime.a8ef3a8272419c2e2c66.js"
  content_type             = "application/javascript"
}

resource "azurerm_storage_blob" "website_file_XVxRFVzjiMd1jOB-LmfnVA--" {
  name                     = "styles.0cffb98e0b6b26d1ac40.css"
  resource_group_name      = var.resource_group_name
  storage_account_name     = azurerm_storage_account.website_storage_account.name
  storage_container_name   = azurerm_storage_container.website_container.name
  type                     = "block"
  source                   = "../website/dist/website/styles.0cffb98e0b6b26d1ac40.css"
  content_type             = "text/css"
}

resource "azurerm_storage_blob" "website_file_0xoFBki5DYWK-ASJcbIZHg--" {
  name                     = "assets/images/cloud-logo.svg"
  resource_group_name      = var.resource_group_name
  storage_account_name     = azurerm_storage_account.website_storage_account.name
  storage_container_name   = azurerm_storage_container.website_container.name
  type                     = "block"
  source                   = "../website/dist/website/assets/images/cloud-logo.svg"
  content_type             = "image/svg+xml"
}

