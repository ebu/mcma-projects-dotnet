using System;
using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Mcma.GoogleCloud.HttpFunctionsApi;
using Microsoft.AspNetCore.Http;

namespace Mcma.GoogleCloud.FFmpegService.ApiHandler
{
    [FunctionsStartup(typeof(FFmpegServiceApiHandlerStartup))]
    public class FFmpegServiceApiHandler : IHttpFunction
    {
        public FFmpegServiceApiHandler(IHttpFunctionApiController controller)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        private IHttpFunctionApiController Controller { get; }

        public Task HandleAsync(HttpContext context) => Controller.HandleRequestAsync(context);
    }
}
