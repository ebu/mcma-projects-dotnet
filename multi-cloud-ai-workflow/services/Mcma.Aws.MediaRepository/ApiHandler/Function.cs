using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Api;
using Mcma.Aws;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Aws.Api;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.MediaRepository.ApiHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller = new ApiGatewayApiController();

        static Function()
        {
            Controller.AddRoute("GET", "/bm-contents", BmContentRoutes.GetBmContentsAsync);
            Controller.AddRoute("POST", "/bm-contents", BmContentRoutes.AddBmContentAsync);
            Controller.AddRoute("GET", "/bm-contents/{id}", BmContentRoutes.GetBmContentAsync);
            Controller.AddRoute("PUT", "/bm-contents/{id}", BmContentRoutes.PutBmContentAsync);
            Controller.AddRoute("DELETE", "/bm-contents/{id}", BmContentRoutes.DeleteBmContentAsync);

            Controller.AddRoute("GET", "/bm-essences", BmEssenceRoutes.GetBmEssencesAsync);
            Controller.AddRoute("POST", "/bm-essences", BmEssenceRoutes.AddBmEssenceAsync);
            Controller.AddRoute("GET", "/bm-essences/{id}", BmEssenceRoutes.GetBmEssenceAsync);
            Controller.AddRoute("PUT", "/bm-essences/{id}", BmEssenceRoutes.PutBmEssenceAsync);
            Controller.AddRoute("DELETE", "/bm-essences/{id}", BmEssenceRoutes.DeleteBmEssenceAsync);
        }

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
