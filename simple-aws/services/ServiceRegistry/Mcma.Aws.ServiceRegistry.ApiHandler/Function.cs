using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api.Routing.Defaults.Routes;
using Mcma.Aws.Functions;
using Mcma.Aws.Functions.ApiHandler;
using Mcma.Aws.Lambda;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.ServiceRegistry.ApiHandler
{
    public class ServiceRegistryApiHandler : McmaLambdaFunction<McmaLambdaApiHandler, APIGatewayProxyRequest, APIGatewayProxyResponse>
    {
        protected override void Configure(IServiceCollection services)
            => services.AddMcmaLambdaApiHandler("service-registry-api-handler",
                                                apiBuilder =>
                                                 apiBuilder.AddDefaultRoutes<Service>()
                                                           .AddDefaultRoutes<JobProfile>());
    }
}
