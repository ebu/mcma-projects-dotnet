using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Mcma.Aws.S3;
using Mcma.Aws.Client;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.CloudWatch;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Aws.Lambda;
using Mcma.Client;
using Mcma.Serialization;
using Mcma.Worker;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.JobProcessor.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<AwsS3FileLocator>().Add<AwsS3FolderLocator>();

        private static AwsCloudWatchLoggerProvider LoggerProvider { get; } = new AwsCloudWatchLoggerProvider("job-processor-worker");

        private static ProviderCollection ProviderCollection { get; } = new ProviderCollection(
            LoggerProvider,
            new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(AwsV4AuthContext.Global)),
            new DynamoDbTableProvider()
        );

        private static DataController DataController { get; } = new DataController();

        private static IJobCheckerTrigger JobCheckerTrigger { get; } = new CloudWatchEventsJobCheckerTrigger();

        private static IWorker Worker { get; } =
            new Mcma.Worker.Worker(ProviderCollection)
                .AddOperation(new StartJob(ProviderCollection, DataController, JobCheckerTrigger))
                .AddOperation(new CancelJob(ProviderCollection, DataController))
                .AddOperation(new RestartJob(ProviderCollection, DataController, JobCheckerTrigger))
                .AddOperation(new FailJob(ProviderCollection, DataController))
                .AddOperation(new DeleteJob(ProviderCollection, DataController))
                .AddOperation(new ProcessNotification(ProviderCollection, DataController));

        public static async Task Handler(WorkerRequest request, ILambdaContext context)
        {
            var logger = LoggerProvider.Get(context.AwsRequestId);

            try
            {
                logger.FunctionStart(context.AwsRequestId);
                logger.Debug(request);
                logger.Debug(context);

                await Worker.DoWorkAsync(new WorkerRequestContext(request, context.AwsRequestId, logger));
            }
            finally
            {
                logger.FunctionEnd(context.AwsRequestId);
                await LoggerProvider.FlushAsync();
            }
        }
    }
}
