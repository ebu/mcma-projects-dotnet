using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Mcma.Aws.S3;
using Mcma.Aws.Client;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.CloudWatch;
using Mcma.Aws.Lambda;
using Mcma.Client;
using Mcma.Serialization;
using Mcma.Worker;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.FFmpegService.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<AwsS3FileLocator>().Add<AwsS3FolderLocator>();

        private static AwsCloudWatchLoggerProvider LoggerProvider { get; } = new AwsCloudWatchLoggerProvider("ffmpeg-service-worker");

        private static IWorker Worker { get; } =
            new Mcma.Worker.Worker(
                    new ProviderCollection(
                        LoggerProvider,
                        new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(AwsV4AuthContext.Global)),
                        new DynamoDbTableProvider()))
                .AddJobProcessing<TransformJob>(x => x.AddProfile<ExtractThumbnail>());
            
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
