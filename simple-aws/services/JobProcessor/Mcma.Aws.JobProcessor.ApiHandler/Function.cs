using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api.Routes;
using Mcma.Aws.ApiGateway;
using Mcma.Aws.Client;
using Mcma.Aws.CloudWatch;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Client;
using Mcma.Serialization;
using Mcma.WorkerInvoker;

using McmaLogger = Mcma.Logging.Logger;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.JobProcessor.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<AwsS3FileLocator>().Add<AwsS3FolderLocator>();

        private static AwsCloudWatchLoggerProvider LoggerProvider { get; } = new AwsCloudWatchLoggerProvider("job-processor-api-handler");

        private static IResourceManagerProvider ResourceManagerProvider { get; } = new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(AwsV4AuthContext.Global));

        private static DataController DataController { get; } = new DataController();

        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();
        
        private static ApiGatewayApiController ApiController { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(new JobRoutes(DataController, ResourceManagerProvider, WorkerInvoker))
                .AddRoutes(new JobExecutionRoutes(DataController, WorkerInvoker))
                .ToApiGatewayApiController(LoggerProvider);

        public static async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var logger = LoggerProvider.Get(context.AwsRequestId);
            try
            {
                logger.FunctionStart(context.AwsRequestId);
                logger.Debug(request);
                logger.Debug(context);
                
                return await ApiController.HandleRequestAsync(request, context);
            }
            finally
            {
                logger.FunctionEnd(context.AwsRequestId);
                await LoggerProvider.FlushAsync();
            }
        }
    }
}
