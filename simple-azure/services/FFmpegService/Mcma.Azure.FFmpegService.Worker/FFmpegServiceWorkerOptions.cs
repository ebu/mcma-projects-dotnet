using Mcma.Utility;

namespace Mcma.Azure.FFmpegService.Worker
{
    internal class FFmpegServiceWorkerOptions
    {
        public string MediaStorageConnectionString { get; set; } = McmaEnvironmentVariables.Get("MEDIA_STORAGE_CONNECTION_STRING", false);
    }
}