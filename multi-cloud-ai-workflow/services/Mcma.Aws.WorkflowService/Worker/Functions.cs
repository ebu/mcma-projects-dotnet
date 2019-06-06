using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Aws;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Worker;
using Mcma.Aws.Worker;
using Mcma.Core;
using Mcma.Worker.Builders;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.WorkflowService.Worker
{
    public class Function
    {
        private static IWorker Worker =
            new WorkerBuilder()
                .HandleJobsOfType<WorkflowJob>(x =>
                    x.AddProfile<RunWorkflow>("ConformWorkflow")
                     .AddProfile<RunWorkflow>("AiWorkflow"))
                .HandleRequestsOfType<ProcessNotificationRequest>(x =>
                    x.WithOperation(ProcessNotificationHandler.OperationName,
                        y => y.Handle<ProcessNotificationHandler>()))
                .Build();
        public async Task Handler(WorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            await Worker.DoWorkAsync(@event);
        }
    }
}
