using Mcma.Api;
using Mcma.Api.Routing.Defaults.Routes;
using Mcma.GoogleCloud.Functions.ApiHandler;

namespace Mcma.GoogleCloud.ServiceRegistry.ApiHandler
{
    public class ServiceRegistryApiHandlerStartup : McmaApiHandlerStartup
    {
        public override string ApplicationName => "service-registry-api-handler";

        public override void BuildApi(McmaApiBuilder apiBuilder)
            =>
                apiBuilder.AddDefaultRoutes<Service>()
                          .AddDefaultRoutes<JobProfile>();

    }
}