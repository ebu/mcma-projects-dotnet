using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Context;
using Mcma.WorkerInvoker;

namespace Mcma.Azure.JobProcessor.ApiHandler
{
    public class JobExecutionRoutes : McmaApiRouteCollection
    {
        public JobExecutionRoutes(DataController dataController, IContextVariableProvider contextVariableProvider, IWorkerInvoker workerInvoker)
        {
            DataController = dataController ?? throw new ArgumentNullException(nameof(dataController));
            ContextVariableProvider = contextVariableProvider ?? throw new ArgumentNullException(nameof(contextVariableProvider));
            WorkerInvoker = workerInvoker ?? throw new ArgumentNullException(nameof(workerInvoker));

            AddRoute(new McmaApiRoute(HttpMethod.Get, "/jobs/{jobId}/executions", QueryAsync));
            AddRoute(new McmaApiRoute(HttpMethod.Get, "/jobs/{jobId}/executions/{executionId}", GetAsync));
            AddRoute(new McmaApiRoute(HttpMethod.Post, "/jobs/{jobId}/executions/{executionId}/notifications", ProcessNotificationAsync));
        }
        
        private DataController DataController { get; }

        private IContextVariableProvider ContextVariableProvider { get; }

        private IWorkerInvoker WorkerInvoker { get; }

        private async Task QueryAsync(McmaApiRequestContext requestContext)
        {
            var jobId = requestContext.JobId();
            
            var job = await DataController.GetJobAsync($"{ContextVariableProvider.PublicUrl()}/jobs/{jobId}");
            if (job == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }
            
            requestContext.SetResponseBody(
                await DataController.QueryExecutionsAsync(job.Id, requestContext.BuildQueryParameters(), requestContext.PageStartToken()));
        }
        
        private async Task GetAsync(McmaApiRequestContext requestContext)
        {
            var jobId = requestContext.JobId();
            var executionId = requestContext.ExecutionId();

            var jobUrl = $"{ContextVariableProvider.PublicUrl()}/jobs/{jobId}";

            JobExecution execution;
            if (executionId == "latest")
                execution =
                    (await DataController.QueryExecutionsAsync(jobUrl, new JobResourceQueryParameters {Limit = 1}))
                        .Results
                        .FirstOrDefault();
            else
                execution = await DataController.GetExecutionAsync($"{jobUrl}/executions/{executionId}");

            if (execution == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }
            
            requestContext.SetResponseBody(execution);
        }

        private async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            var jobId = requestContext.JobId();
            var executionId = requestContext.ExecutionId();

            var job = await DataController.GetJobAsync($"{ContextVariableProvider.PublicUrl()}/jobs/{jobId}");
            var jobExecution = await DataController.GetExecutionAsync($"{ContextVariableProvider.PublicUrl()}/jobs/{jobId}/executions/{executionId}");

            if (job == null || jobExecution == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }

            var notification = requestContext.GetRequestBody<Notification>();
            if (notification == null)
            {
                requestContext.SetResponseBadRequestDueToMissingBody();
                return;
            }

            if (jobExecution.JobAssignmentId != null && jobExecution.JobAssignmentId != notification.Source)
            {
                requestContext.SetResponseStatus((int)HttpStatusCode.BadRequest, $"Unexpected notification from '{notification.Source}'");
                return;
            }
            
            requestContext.SetResponseStatusCode(HttpStatusCode.Accepted);

            await WorkerInvoker.InvokeAsync(requestContext.WorkerFunctionId(),
                                            "ProcessNotification",
                                            requestContext.ToDictionary(),
                                            new
                                            {
                                                jobId = job.Id,
                                                jobExecutionId = jobExecution.Id,
                                                notification
                                            },
                                            job.Tracker);
        }
    }
}