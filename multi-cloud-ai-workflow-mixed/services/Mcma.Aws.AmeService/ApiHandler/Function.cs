using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api.Routes.Defaults;
using Mcma.Aws.ApiGateway;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AmeService.ApiHandler
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static ApiGatewayApiController Controller { get; } =
            new DefaultRouteCollectionBuilder<JobAssignment>(new DynamoDbTableProvider())
                .ForJobAssignments<LambdaWorkerInvoker>()
                .ToApiGatewayApiController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
