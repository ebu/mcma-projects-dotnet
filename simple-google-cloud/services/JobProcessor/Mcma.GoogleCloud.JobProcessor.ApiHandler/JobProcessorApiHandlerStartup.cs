using Mcma.Api;
using Mcma.Client;
using Mcma.GoogleCloud.Client;
using Mcma.GoogleCloud.Functions.ApiHandler;
using Mcma.GoogleCloud.JobProcessor.Common;
using Mcma.GoogleCloud.PubSubWorkerInvoker;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.GoogleCloud.JobProcessor.ApiHandler
{
    public class JobProcessorApiHandlerStartup : McmaApiHandlerStartup
    {
        public override string ApplicationName => "job-processor-api-handler";

        protected override IServiceCollection ConfigureAdditionalServices(IServiceCollection services)
            => services
               .AddMcmaClient(clientBuilder => GoogleAuthenticatorRegistryExtensions.AddGoogleAuth(clientBuilder.Auth))
               .AddMcmaPubSubWorkerInvoker()
               .AddDataController();

        public override void BuildApi(McmaApiBuilder apiBuilder)
            => apiBuilder.AddRouteCollection<JobRoutes>()
                         .AddRouteCollection<JobExecutionRoutes>();
    }
}