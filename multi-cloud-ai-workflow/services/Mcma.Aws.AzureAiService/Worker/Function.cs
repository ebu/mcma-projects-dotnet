using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Aws;
using Mcma.Aws.Worker;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Worker;
using Mcma.Core;
using Mcma.Aws.DynamoDb;
using Mcma.Worker.Builders;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AzureAiService.Worker
{
    public class Function
    {
        public static IWorker Worker { get; } =
            new WorkerBuilder()
                .HandleJobsOfType<AIJob>(x =>
                    x.AddProfile<TranscribeAudio>(TranscribeAudio.Name)
                     .AddProfile<TranslateText>(TranslateText.Name)
                     .AddProfile<ExtractAllAIMetadata>(ExtractAllAIMetadata.Name))
                .HandleRequestsOfType<ProcessNotificationRequest>(
                    x =>
                        x.WithOperation(ProcessNotificationHandler.OperationName,
                            y =>
                                y.Handle(
                                    new ProcessNotificationHandler(
                                        new DynamoDbTableProvider<JobAssignment>(),
                                        new AwsWorkerResourceManagerProvider()))))
                .Build();

        public async Task Handler(WorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            await Worker.DoWorkAsync(@event);
        }
    }
}
