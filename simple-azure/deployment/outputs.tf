output service_registry_url {
  value = module.services.service_registry_url
}

output service_registry_scope {
  value  = module.services.service_registry_scope
}

output service_registry_app_id {
  value = module.services.service_registry_app_id
}

output job_processor_api_url {
  value = module.services.job_processor_api_url
}

output job_processor_api_scope {
  value  = module.services.job_processor_api_scope
}

output job_processor_worker_url {
  value = module.services.job_processor_worker_url
}

output mediainfo_service_url {
  value = module.services.mediainfo_service_url
}

output mediainfo_service_worker_url {
  value = module.services.mediainfo_service_worker_url
}

output ffmpeg_service_url {
  value = module.services.ffmpeg_service_url
}

output ffmpeg_service_worker_url {
  value = module.services.ffmpeg_service_worker_url
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

output output_container {
  value = var.output_container
}
