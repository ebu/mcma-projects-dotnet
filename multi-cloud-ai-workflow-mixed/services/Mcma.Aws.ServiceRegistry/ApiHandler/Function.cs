using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api.Routes;
using Mcma.Api.Routes.Defaults;
using Mcma.Aws.ApiGateway;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Data;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.ServiceRegistry.ApiHandler
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();

        private static IDbTableProvider DbTableProvider { get; } = new DynamoDbTableProvider();

        private static McmaApiRouteCollection ServiceRoutes { get; } =
            new DefaultRouteCollectionBuilder<Service>(DbTableProvider).AddAll().Build();

        private static McmaApiRouteCollection JobProfileRoutes { get; } =
            new DefaultRouteCollectionBuilder<JobProfile>(DbTableProvider).AddAll().Build();
            
        private static ApiGatewayApiController Controller { get; } = 
            new McmaApiRouteCollection()
                .AddRoutes(ServiceRoutes)
                .AddRoutes(JobProfileRoutes)
                .ToApiGatewayApiController();

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            var resp = await Controller.HandleRequestAsync(request, context);
            Logger.Debug(resp.ToMcmaJson().ToString());
            return resp;
        }
    }
}
