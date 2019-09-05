using System.Threading.Tasks;
using Mcma.Core;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Mcma.Core.Serialization;
using Mcma.Worker;

namespace Mcma.Aws.WorkflowService.Worker
{
    internal class RunWorkflow : IJobProfileHandler<WorkflowJob>
    {
        public async Task ExecuteAsync(WorkerJobHelper<WorkflowJob> job)
        {
            using (var stepFunctionClient = new AmazonStepFunctionsClient())
                await stepFunctionClient.StartExecutionAsync(
                    new StartExecutionRequest
                    {
                        Input =
                            new
                            {
                                Input = job.JobInput,
                                NotificationEndpoint = new NotificationEndpoint {HttpEndpoint = job.JobAssignmentId + "/notifications"}
                            }.ToMcmaJson().ToString(),
                        StateMachineArn = job.Request.GetRequiredContextVariable($"{job.Profile.Name}Id")
                    });
        }
    }
}
