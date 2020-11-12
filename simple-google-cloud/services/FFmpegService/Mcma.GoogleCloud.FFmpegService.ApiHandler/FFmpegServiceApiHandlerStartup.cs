using Mcma.GoogleCloud.Functions.ApiHandler;

namespace Mcma.GoogleCloud.FFmpegService.ApiHandler
{
    public class FFmpegServiceApiHandlerStartup : McmaJobAssignmentApiHandlerStartup
    {
        public override string ApplicationName => "ffmpeg-service-api-handler";
    }
}