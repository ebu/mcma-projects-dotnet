using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Core.Serialization;
using Mcma.Aws;
using Mcma.Core.Logging;
using Mcma.Aws.Api;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.ServiceRegistry.ApiHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller = new ApiGatewayApiController();

        static Function()
        {
            Controller.AddRoute("GET", "/services", ServiceRoutes.GetServicesAsync);
            Controller.AddRoute("POST", "/services", ServiceRoutes.AddServiceAsync);
            Controller.AddRoute("GET", "/services/{id}", ServiceRoutes.GetServiceAsync);
            Controller.AddRoute("PUT", "/services/{id}", ServiceRoutes.PutServiceAsync);
            Controller.AddRoute("DELETE", "/services/{id}", ServiceRoutes.DeleteServiceAsync);

            Controller.AddRoute("GET", "/job-profiles", JobProfileRoutes.GetJobProfilesAsync);
            Controller.AddRoute("POST", "/job-profiles", JobProfileRoutes.AddJobProfileAsync);
            Controller.AddRoute("GET", "/job-profiles/{id}", JobProfileRoutes.GetJobProfileAsync);
            Controller.AddRoute("PUT", "/job-profiles/{id}", JobProfileRoutes.PutJobProfileAsync);
            Controller.AddRoute("DELETE", "/job-profiles/{id}", JobProfileRoutes.DeleteJobProfileAsync);
        }

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
