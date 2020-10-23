using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api.Routes;
using Mcma.Api.Routing.Defaults;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.ApiGateway;
using Mcma.Aws.CloudWatch;
using Mcma.Aws.Lambda;
using Mcma.Data;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.ServiceRegistry.ApiHandler
{
    public static class Function
    {
        private static AwsCloudWatchLoggerProvider LoggerProvider { get; } = new AwsCloudWatchLoggerProvider("service-registry-api-handler");

        private static IDocumentDatabaseTableProvider DbTableProvider { get; } = new DynamoDbTableProvider();

        private static ApiGatewayApiController ApiController { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(new DefaultRouteCollection<Service>(DbTableProvider))
                .AddRoutes(new DefaultRouteCollection<JobProfile>(DbTableProvider))
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
