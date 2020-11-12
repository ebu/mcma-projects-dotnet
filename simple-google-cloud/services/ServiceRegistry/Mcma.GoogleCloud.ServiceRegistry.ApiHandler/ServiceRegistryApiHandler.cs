using System;
using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Mcma.GoogleCloud.HttpFunctionsApi;
using Microsoft.AspNetCore.Http;

namespace Mcma.GoogleCloud.ServiceRegistry.ApiHandler
{
    [FunctionsStartup(typeof(ServiceRegistryApiHandlerStartup))]
    public class ServiceRegistryApiHandler : IHttpFunction
    {
        public ServiceRegistryApiHandler(IHttpFunctionApiController controller)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        private IHttpFunctionApiController Controller { get; }

        public Task HandleAsync(HttpContext context) => Controller.HandleRequestAsync(context);
    }
}
