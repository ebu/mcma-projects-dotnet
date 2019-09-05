using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Worker;

namespace Mcma.Aws.TransformService.Worker
{
    internal class CreateProxyEC2 : IJobProfileHandler<TransformJob>
    {
        public const string Name = nameof(CreateProxyEC2);

        public async Task ExecuteAsync(WorkerJobHelper<TransformJob> job)
        {
            var ec2hostname = job.Request.GetRequiredContextVariable("HostnameInstanceEC2");

            var ec2Url = "http://" + ec2hostname + "/new-transform-job";

            var message = new
            {
                input = job.JobInput,
                notificationEndpoint = new NotificationEndpoint {HttpEndpoint = job.JobAssignmentId + "/notifications"}
            };

            Logger.Debug("Sending to", ec2Url, "message", message);
            var mcmaHttp = new McmaHttpClient();
            await mcmaHttp.PostAsJsonAsync(ec2Url, message);
            Logger.Debug("Done");
        }
    }
}
