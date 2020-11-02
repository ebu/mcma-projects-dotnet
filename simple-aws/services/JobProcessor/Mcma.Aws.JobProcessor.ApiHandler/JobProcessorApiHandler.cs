using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Aws.Client;
using Mcma.Aws.Functions;
using Mcma.Aws.Functions.ApiHandler;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Aws.Lambda;
using Mcma.Aws.WorkerInvoker;
using Mcma.Client;
using Mcma.Utility;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.JobProcessor.ApiHandler
{
    public class JobProcessorApiHandler : McmaLambdaFunction<McmaLambdaApiHandler, APIGatewayProxyRequest, APIGatewayProxyResponse>
    {
        protected override void Configure(IServiceCollection services)
            => services
               .AddMcmaClient(clientBuilder => clientBuilder.ConfigureDefaultsFromEnvironmentVariables().Auth.AddAws4Auth())
               .AddMcmaLambdaWorkerInvoker()
               .AddDataController()
               .AddMcmaLambdaApiHandler("job-processor-api-handler",
                                        apiBuilder => apiBuilder.AddRouteCollection<JobRoutes>().AddRouteCollection<JobExecutionRoutes>());
    }
}
