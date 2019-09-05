resource "azurerm_app_service_plan" "mcma_services" {
  name                = "${var.global_prefix}-services-appsvcplan"
  location            = "${var.azure_location}"
  resource_group_name = "${var.resource_group_name}"
  kind                = "FunctionApp"

  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}