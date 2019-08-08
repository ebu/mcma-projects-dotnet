using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Mcma.Aws.Client;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Data;
using Mcma.Worker;
using Mcma.Worker.Builders;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AmeService.Worker
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(AwsV4AuthContext.Global));

        private static IDbTableProvider<JobAssignment> DbTableProvider { get; } =
            new DynamoDbTableProvider<JobAssignment>();

        private static IWorker Worker { get; }
            = new WorkerBuilder()
                .HandleJobsOfType<AmeJob>(
                    DbTableProvider,
                    ResourceManagerProvider,
                    x => x.AddProfile<ExtractTechnicalMetadata>())
                .Build();

        public async Task Handler(WorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            await Worker.DoWorkAsync(@event);
        }
    }
}
