using Mcma.Utility;

namespace Mcma.Azure.MediaInfoService.Worker
{
    internal class MediaInfoServiceWorkerOptions
    {
        public string MediaStorageConnectionString { get; set; } = McmaEnvironmentVariables.Get("MEDIA_STORAGE_CONNECTION_STRING", false);
    }
}