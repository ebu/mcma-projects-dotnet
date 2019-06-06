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
using Mcma.Core;
using Mcma.Aws.Worker;
using Mcma.Worker.Builders;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.TransformService.Worker
{
    public class Function
    {
        public static IWorker Worker { get; } =
            new WorkerBuilder()
                .HandleJobsOfType<TransformJob>(x =>
                    x.AddProfile<CreateProxyLambda>(CreateProxyLambda.Name)
                     .AddProfile<CreateProxyEC2>(CreateProxyEC2.Name))
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
