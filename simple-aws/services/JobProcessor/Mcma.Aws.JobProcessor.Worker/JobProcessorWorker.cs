using Amazon.Lambda.Core;
using Mcma.Aws.Client;
using Mcma.Aws.Functions;
using Mcma.Aws.Functions.ApiHandler;
using Mcma.Aws.Functions.Worker;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Aws.Lambda;
using Mcma.Client;
using Mcma.Worker;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.JobProcessor.Worker
{
    public class JobProcessorWorker : McmaLambdaFunction<McmaLambdaWorker, McmaWorkerRequest>
    {
        protected override void Configure(IServiceCollection services)
        {
            services
                .AddMcmaClient(clientBuilder => clientBuilder.ConfigureDefaultsFromEnvironmentVariables().Auth.AddAws4Auth())
                .AddDataController()
                .AddSingleton<IJobCheckerTrigger, CloudWatchEventsJobCheckerTrigger>()
                .AddMcmaAwsLambdaWorker("job-processor-worker",
                                        builder =>
                                            builder.AddOperation<StartJob>()
                                                   .AddOperation<CancelJob>()
                                                   .AddOperation<RestartJob>()
                                                   .AddOperation<FailJob>()
                                                   .AddOperation<DeleteJob>()
                                                   .AddOperation<ProcessNotification>());
        }
    }
}
