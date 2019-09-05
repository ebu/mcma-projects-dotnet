using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Mcma.Aws.Client;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Data;
using Mcma.Worker;
using Mcma.Worker.Builders;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.WorkflowService.Worker
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(AwsV4AuthContext.Global));

        private static IDbTableProvider DbTableProvider { get; } = new DynamoDbTableProvider();

        private static IWorker Worker =
            new WorkerBuilder()
                .HandleJobsOfType<WorkflowJob>(
                    DbTableProvider,
                    ResourceManagerProvider,
                    x =>
                        x.AddProfile<RunWorkflow>("ConformWorkflow")
                        .AddProfile<RunWorkflow>("AiWorkflow"))
                .HandleOperation(new ProcessNotification(ResourceManagerProvider, DbTableProvider))
                .Build();
                
        public async Task Handler(WorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            await Worker.DoWorkAsync(@event);
        }
    }
}
