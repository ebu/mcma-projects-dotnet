output ffmpeg_service_url {
  value = "${local.ffmpeg_service_url}/"
}

output ffmpeg_service_worker_url {
  value = "https://${azurerm_function_app.ffmpeg_service_worker_function.default_hostname}/"
}

output job_processor_api_url {
  value = "${local.job_processor_api_url}/"
}

output job_processor_api_app_id {
  value = azuread_application.job_processor_api_app.application_id
}

output job_processor_api_scope {
  value = azuread_application.job_processor_api_app.oauth2_permissions
}

output job_processor_worker_url {
  value = "https://${azurerm_function_app.job_processor_worker_function.default_hostname}/"
}

output mediainfo_service_url {
  value = "${local.mediainfo_service_url}/"
}

output mediainfo_service_worker_url {
  value = "https://${azurerm_function_app.mediainfo_service_worker_function.default_hostname}/"
}

output service_registry_url {
  value = "${local.service_registry_url}/"
}

output services_url {
  value = local.services_url
}

output service_registry_app_id {
  value = azuread_application.service_registry_app.application_id
}

output service_registry_scope {
  value = azuread_application.service_registry_app.oauth2_permissions
}
