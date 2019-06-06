using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Core.Serialization;
using Mcma.Aws;
using Mcma.Core.Logging;
using Mcma.Worker;
using Mcma.Worker.Builders;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.JobProcessor.Worker
{
    public class Function
    {
        private static IWorker Worker =
            new WorkerBuilder()
                .HandleRequestsOfType<CreateJobAssignmentRequest>(x =>
                    x.WithOperation(JobProcessorWorkerOperations.CreateJobAssignmentOperationName,
                        y => y.Handle(JobProcessorWorkerOperations.CreateJobAssignmentAsync)))
                .HandleRequestsOfType<DeleteJobAssignmentRequest>(x =>
                    x.WithOperation(JobProcessorWorkerOperations.DeleteJobAssignmentOperationName,
                        y => y.Handle(JobProcessorWorkerOperations.DeleteJobAssignmentAsync)))
                .HandleRequestsOfType<ProcessNotificationRequest>(x =>
                    x.WithOperation(JobProcessorWorkerOperations.ProcessNotificationOperationName,
                        y => y.Handle(JobProcessorWorkerOperations.ProcessNotificationAsync)))
                .Build();

        public async Task Handler(WorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            await Worker.DoWorkAsync(@event);
        }
    }
}
