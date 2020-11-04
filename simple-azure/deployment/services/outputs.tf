output ffmpeg_service_url {
  value = "${local.ffmpeg_service_url}/"
}

output job_processor_url {
  value = "${local.job_processor_api_url}/"
}

output mediainfo_service_url {
  value = "${local.mediainfo_service_url}/"
}

output service_registry_url {
  value = "${local.service_registry_url}/"
}

output services_url {
  value = local.services_url
}

output services_auth_type {
  value = local.services_auth_type
}

output services_auth_context {
  value = local.services_auth_context
}