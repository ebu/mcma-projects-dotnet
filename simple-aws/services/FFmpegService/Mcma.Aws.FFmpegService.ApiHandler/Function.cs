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
using Mcma.Serialization;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.FFmpegService.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<AwsS3FileLocator>().Add<AwsS3FolderLocator>();

        private static AwsCloudWatchLoggerProvider LoggerProvider { get; } =
            new AwsCloudWatchLoggerProvider("ffmpeg-service-api-handler", Environment.GetEnvironmentVariable("LogGroupName"));

        private static DynamoDbTableProvider DbTableProvider { get; } = new DynamoDbTableProvider();

        private static ApiGatewayApiController ApiController { get; } =
            new DefaultJobRouteCollection(DbTableProvider, new LambdaWorkerInvoker(new EnvironmentVariableProvider()))
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
