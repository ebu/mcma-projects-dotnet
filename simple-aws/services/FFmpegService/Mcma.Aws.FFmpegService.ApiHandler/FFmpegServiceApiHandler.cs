using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Aws.Functions;
using Mcma.Aws.Functions.ApiHandler;
using Mcma.Aws.Lambda;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.FFmpegService.ApiHandler
{
    public class FFmpegServiceApiHandler : McmaLambdaFunction<McmaLambdaApiHandler, APIGatewayProxyRequest, APIGatewayProxyResponse>
    {
        protected override void Configure(IServiceCollection services)
        {
            services.AddMcmaLambdaJobAssignmentApiHandler("ffmpeg-service-api-handler");
        }
    }
}
