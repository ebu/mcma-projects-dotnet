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
using Mcma.Worker.Builders;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.JobRepository.Worker
{
    public class Function
    {
        private static IWorker Worker =
            new WorkerBuilder()
                .HandleRequestsOfType<CreateJobProcessRequest>(x =>
                    x.WithOperation(JobRepositoryWorkerOperations.CreateJobProcessOperationName,
                        y => y.Handle(JobRepositoryWorkerOperations.CreateJobProcessAsync)))
                .HandleRequestsOfType<DeleteJobProcessRequest>(x =>
                    x.WithOperation(JobRepositoryWorkerOperations.DeleteJobProcessOperationName,
                        y => y.Handle(JobRepositoryWorkerOperations.DeleteJobProcessAsync)))
                .HandleRequestsOfType<ProcessNotificationRequest>(x =>
                    x.WithOperation(JobRepositoryWorkerOperations.ProcessNotificationOperationName,
                        y => y.Handle(JobRepositoryWorkerOperations.ProcessNotificationAsync)))
                .Build();
        public async Task Handler(WorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());
            
            await Worker.DoWorkAsync(@event);
        }
    }
}
