using System;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api.Routing.Defaults;
using Mcma.Aws.S3;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.ApiGateway;
using Mcma.Aws.CloudWatch;
using Mcma.Aws.Lambda;
using Mcma.Context;
using Mcma.Data;
using Mcma.Serialization;
using Mcma.WorkerInvoker;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.MediaInfoService.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<AwsS3FileLocator>().Add<AwsS3FolderLocator>();
        
        private static IContextVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static AwsCloudWatchLoggerProvider LoggerProvider { get; } =
            new AwsCloudWatchLoggerProvider("mediainfo-service-worker", Environment.GetEnvironmentVariable("LogGroupName"));

        private static IDocumentDatabaseTableProvider DbTableProvider { get; } =
            new DynamoDbTableProvider();
        
        private static IWorkerInvoker LambdaWorkerInvoker { get; } = new LambdaWorkerInvoker(EnvironmentVariableProvider);

        private static ApiGatewayApiController ApiController { get; } =
            new DefaultJobRouteCollection(DbTableProvider, LambdaWorkerInvoker)
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
