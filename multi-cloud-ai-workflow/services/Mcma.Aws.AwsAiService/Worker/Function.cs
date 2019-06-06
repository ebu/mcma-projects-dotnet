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
using Mcma.Aws.DynamoDb;
using Mcma.Worker.Builders;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AwsAiService.Worker
{
    public class Function
    {
        private static IWorker Worker { get; } =
            new WorkerBuilder()
                .HandleJobsOfType<AIJob>(
                    x =>
                        x.AddProfile<TranscribeAudio>(TranscribeAudio.Name)
                         .AddProfile<TranslateText>(TranslateText.Name)
                         .AddProfile<DetectCelebrities>(DetectCelebrities.Name))
                .HandleRequestsOfType<ProcessTranscribeJobResult>(
                    x =>
                        x.WithOperation(ProcessTranscribeJobResultHandler.OperationName,
                            y =>
                                y.Handle(
                                    new ProcessTranscribeJobResultHandler(
                                        new DynamoDbTableProvider<JobAssignment>(),
                                        new AwsWorkerResourceManagerProvider()))))
                .HandleRequestsOfType<ProcessRekognitionResult>(
                    x =>
                        x.WithOperation(ProcessRekognitionResultHandler.OperationName,
                            y =>
                                y.Handle(
                                    new ProcessRekognitionResultHandler(
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
