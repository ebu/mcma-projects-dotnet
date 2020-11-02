using System;
using System.Threading.Tasks;
using Mcma.Azure.FunctionsApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Mcma.Azure.MediaInfoService.ApiHandler
{
    public class MediaInfoServiceApiHandler
    {
        public MediaInfoServiceApiHandler(IAzureFunctionApiController controller)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        private IAzureFunctionApiController Controller { get; }

        [FunctionName(nameof(MediaInfoServiceApiHandler))]
        public async Task<IActionResult> ExecuteAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ExecutionContext executionContext)
        {
            return await Controller.HandleRequestAsync(request, executionContext);
        }
    }
}
