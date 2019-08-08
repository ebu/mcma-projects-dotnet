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
        private static IDbTableProvider<Service> ServiceDbTableProvider { get; } = new DynamoDbTableProvider<Service>();
        
        private static IDbTableProvider<JobProfile> JobProfileDbTableProvider { get; } = new DynamoDbTableProvider<JobProfile>();

        private static McmaApiRouteCollection ContentRoutes { get; } =
            new DefaultRouteCollectionBuilder<Service>(ServiceDbTableProvider).AddAll().Build();

        private static McmaApiRouteCollection EssenceRoutes { get; } =
            new DefaultRouteCollectionBuilder<JobProfile>(JobProfileDbTableProvider).AddAll().Build();
            
        private static ApiGatewayApiController Controller { get; } = 
            new McmaApiRouteCollection()
                .AddRoutes(ContentRoutes)
                .AddRoutes(EssenceRoutes)
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
