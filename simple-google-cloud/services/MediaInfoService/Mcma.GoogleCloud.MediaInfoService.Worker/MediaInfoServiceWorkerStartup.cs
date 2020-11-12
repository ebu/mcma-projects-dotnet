using Mcma.GoogleCloud.Functions.Worker;
using Mcma.GoogleCloud.MediaInfoService.Worker.Profiles;
using Mcma.Worker;

namespace Mcma.GoogleCloud.MediaInfoService.Worker
{
    public class MediaInfoServiceWorkerStartup : McmaJobAssignmentWorkerStartup<AmeJob>
    {
        protected override string ApplicationName => "mediainfo-service-worker";
        
        protected override void AddProfiles(ProcessJobAssignmentOperationBuilder<AmeJob> builder)
            => builder.AddProfile<ExtractTechnicalMetadata>();
    }
}