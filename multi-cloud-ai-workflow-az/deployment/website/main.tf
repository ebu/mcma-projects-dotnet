resource "azuread_application" "website_app" {
  name                       = "${var.global_prefix}-website-app"
  identifier_uris            = ["https://${var.website_domain}"]
  reply_urls                 = ["${var.website_url}/index.html", "http://localhost:4200"]
  oauth2_allow_implicit_flow = true

  required_resource_access {
    resource_app_id = "00000003-0000-0000-c000-000000000000"

    resource_access {
      id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d"
      type = "Scope"
    }
  }

  required_resource_access {
    resource_app_id = "e406a681-f3d4-42a8-90b6-c2b029497af1"

    resource_access {
      id = "03e0da56-190b-40ad-a80c-ea378c433f7f"
      type = "Scope"
		}
  }

  required_resource_access {
    resource_app_id = var.service_registry_app_id

    resource_access {
      id   = var.service_registry_scope
      type = "Scope"
    }
  }

  required_resource_access {
    resource_app_id = var.job_repository_app_id

    resource_access {
      id   = var.job_repository_scope
      type = "Scope"
    }
  }

  required_resource_access {
    resource_app_id = var.media_repository_app_id

    resource_access {
      id   = var.media_repository_scope
      type = "Scope"
    }
  }
}

resource "azuread_service_principal" "website_sp" {
  application_id               = azuread_application.website_app.application_id
  app_role_assignment_required = false
}

resource "azuread_user" "website_user" {
  user_principal_name = "mcma-demo@evanverneyfinklive.onmicrosoft.com"
  display_name        = "MCMA Demo"
  password            = "Welcome2MCMA!"
}

resource "azurerm_role_assignment" "website_user_storage_access_role_assignment" {
  scope                = var.resource_group_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azuread_user.website_user.id
}

output website_client_id {
  value = azuread_application.website_app.application_id
}
