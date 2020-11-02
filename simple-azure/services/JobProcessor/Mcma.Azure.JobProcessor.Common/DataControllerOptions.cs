using Mcma.Utility;

namespace Mcma.Azure.JobProcessor.Common
{
    public class DataControllerOptions
    {
        public string PublicUrl { get; set; } = McmaEnvironmentVariables.Get("PUBLIC_URL");
    }
}