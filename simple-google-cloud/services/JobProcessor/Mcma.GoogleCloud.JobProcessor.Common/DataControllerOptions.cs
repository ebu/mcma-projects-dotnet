using Mcma.Utility;

namespace Mcma.GoogleCloud.JobProcessor.Common
{
    public class DataControllerOptions
    {
        public string PublicUrl { get; set; } = McmaEnvironmentVariables.Get("PUBLIC_URL");
    }
}