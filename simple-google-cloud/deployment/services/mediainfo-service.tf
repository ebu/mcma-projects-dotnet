locals {
  mediainfo_service_api_zip_file      = "../Services/MediaInfoService/Mcma.GoogleCloud.MediaInfoService.ApiHandler/dist/function.zip"
  mediainfo_service_worker_zip_file   = "../Services/MediaInfoService/Mcma.GoogleCloud.MediaInfoService.Worker/dist/function.zip"
  mediainfo_service_api_function_name = "${var.global_prefix}-mediainfo-service-api"
  mediainfo_service_api_url           = "https://${var.google_cloud_region}-${var.global_prefix}.cloudfunctions.net/${local.mediainfo_service_api_function_name}"
}

resource "google_pubsub_topic" "mediainfo_service_work_topic" {
  name = "mediainfo-service-work"
}

resource "google_storage_bucket_object" "mediainfo_service_api_handler_zip" {
  name   = "mediainfo-service/api-handler/function-${filesha256(local.mediainfo_service_api_zip_file)}.zip"
  bucket = var.functions_bucket
  source = local.mediainfo_service_api_zip_file
}

resource "google_cloudfunctions_function" "mediainfo_service_api_handler" {
  name    = local.mediainfo_service_api_function_name
  runtime = "dotnet3"

  available_memory_mb   = 128
  source_archive_bucket = var.functions_bucket
  source_archive_object = google_storage_bucket_object.mediainfo_service_api_handler_zip.name
  trigger_http          = true
  entry_point           = "Mcma.GoogleCloud.MediaInfoService.ApiHandler.MediaInfoServiceApiHandler"

  service_account_email = google_service_account.service_account.email

  environment_variables = {
    MCMA_TABLE_NAME           = "mediainfo-service"
    MCMA_PUBLIC_URL           = local.mediainfo_service_api_url
    MCMA_WORKER_PUBSUB_TOPIC    = google_pubsub_topic.mediainfo_service_work_topic.name
    MCMA_SERVICES_URL         = "${local.service_registry_api_url}/services"
    MCMA_SERVICES_AUTH_TYPE    = "Google"
    MCMA_SERVICES_AUTH_CONTEXT = "{ \"scopes\": \"${local.service_registry_api_url}\" }"
  }
}

resource "google_storage_bucket_object" "mediainfo_service_worker_zip" {
  name   = "mediainfo-service/api-handler/function-${filesha256(local.mediainfo_service_worker_zip_file)}.zip"
  bucket = var.functions_bucket
  source = local.mediainfo_service_worker_zip_file
}

resource "google_cloudfunctions_function" "mediainfo_service_worker" {
  name    = "${var.global_prefix}-mediainfo-service-worker"
  runtime = "dotnet3"

  available_memory_mb   = 128
  source_archive_bucket = var.functions_bucket
  source_archive_object = google_storage_bucket_object.mediainfo_service_worker_zip.name
  entry_point           = "Mcma.GoogleCloud.MediaInfoService.Worker.MediaInfoServiceWorker"

  event_trigger {
    event_type = "google.pubsub.topic.publish"
    resource   = google_pubsub_topic.mediainfo_service_work_topic.id
  }

  service_account_email = google_service_account.service_account.email

  environment_variables = {
    MCMA_TABLE_NAME           = "mediainfo-service"
    MCMA_PUBLIC_URL           = local.mediainfo_service_api_url
    MCMA_SERVICES_URL         = "${local.service_registry_api_url}/services"
    MCMA_SERVICES_AUTH_TYPE    = "Google"
    MCMA_SERVICES_AUTH_CONTEXT = "{ \"scopes\": \"${local.service_registry_api_url}\" }"
  }
}
