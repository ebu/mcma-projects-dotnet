using System;
using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Mcma.GoogleCloud.HttpFunctionsApi;
using Microsoft.AspNetCore.Http;

namespace Mcma.GoogleCloud.JobProcessor.ApiHandler
{
    [FunctionsStartup(typeof(JobProcessorApiHandlerStartup))]
    public class JobProcessorApiHandler : IHttpFunction
    {
        public JobProcessorApiHandler(IHttpFunctionApiController controller)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }
        
        private IHttpFunctionApiController Controller { get; }

        public Task HandleAsync(HttpContext context) => Controller.HandleRequestAsync(context);
    }
}
