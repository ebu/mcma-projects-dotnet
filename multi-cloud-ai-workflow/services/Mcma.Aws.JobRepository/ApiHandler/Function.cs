using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Aws;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Aws.Api;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.JobRepository.ApiHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller = new ApiGatewayApiController();

        static Function()
        {
            Controller.AddRoute("GET", "/jobs", JobRoutes.GetJobsAsync);
            Controller.AddRoute("POST", "/jobs", JobRoutes.AddJobAsync);
            Controller.AddRoute("GET", "/jobs/{id}", JobRoutes.GetJobAsync);
            Controller.AddRoute("DELETE", "/jobs/{id}", JobRoutes.DeleteJobAsync);

            Controller.AddRoute("POST", "/jobs/{id}/stop", JobRoutes.StopJobAsync);
            Controller.AddRoute("POST", "/jobs/{id}/cancel", JobRoutes.CancelJobAsync);

            Controller.AddRoute("POST", "/jobs/{id}/notifications", JobRoutes.ProcessNotificationAsync);
        }

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
