using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Client;
using Mcma.WorkerInvoker;
using Microsoft.Extensions.Options;

namespace Mcma.Azure.JobProcessor.ApiHandler
{
    public class JobRoutes : McmaApiRouteCollection
    {
        public JobRoutes(IDataController dataController,
                         IResourceManagerProvider resourceManagerProvider,
                         IWorkerInvoker workerInvoker,
                         IOptions<McmaApiOptions> options)
        {
            ResourceManagerProvider = resourceManagerProvider ?? throw new ArgumentNullException(nameof(resourceManagerProvider));
            DataController = dataController ?? throw new ArgumentNullException(nameof(dataController));
            WorkerInvoker = workerInvoker ?? throw new ArgumentNullException(nameof(workerInvoker));
            Options = options.Value ?? new McmaApiOptions();

            AddRoute(new DelegateMcmaApiRoute(HttpMethod.Get, "/jobs", QueryAsync));
            AddRoute(new DelegateMcmaApiRoute(HttpMethod.Post, "/jobs", CreateAsync));
            AddRoute(new DelegateMcmaApiRoute(HttpMethod.Get, "/jobs/{jobId}", GetAsync));
            AddRoute(new DelegateMcmaApiRoute(HttpMethod.Delete, "/jobs/{jobId}", DeleteAsync));

            AddRoute(new DelegateMcmaApiRoute(HttpMethod.Post, "/jobs/{jobId}/cancel", CancelAsync));
            AddRoute(new DelegateMcmaApiRoute(HttpMethod.Post, "/jobs/{jobId}/restart", RestartAsync));
        }

        private IDataController DataController { get; }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IWorkerInvoker WorkerInvoker { get; }
        
        private McmaApiOptions Options { get; }

        private async Task QueryAsync(McmaApiRequestContext requestContext)
            => requestContext.SetResponseBody(
                await DataController.QueryJobsAsync(requestContext.BuildQueryParameters(100), requestContext.PageStartToken()));

        private async Task CreateAsync(McmaApiRequestContext requestContext)
        {
            var job = requestContext.GetRequestBody<Job>();

            job.Status = JobStatus.New;
            if (job.Tracker == null)
            {
                var label = job.Type;
                try
                {
                    var resourceManager = ResourceManagerProvider.Get();
                    var jobProfile = await resourceManager.GetAsync<JobProfile>(job.JobProfileId);
                    label += " with JobProfile " + jobProfile.Name;
                }
                catch (Exception error)
                {
                    requestContext.GetLogger().Error(error);
                    label += " with unknown JobProfile";
                }

                job.Tracker = new McmaTracker {Id = Guid.NewGuid().ToString(), Label = label};
            }
            
            job = await DataController.AddJobAsync(job);
            
            requestContext.SetResponseBody(job);

            await WorkerInvoker.InvokeAsync("StartJob",
                                            new
                                            {
                                                jobId = job.Id
                                            },
                                            job.Tracker);
        }

        private async Task GetAsync(McmaApiRequestContext requestContext)
        {
            var jobId = requestContext.JobId();

            var job = await DataController.GetJobAsync($"{Options.PublicUrl}/jobs/{jobId}");
            if (job == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }

            requestContext.SetResponseBody(job);
        }

        private async Task DeleteAsync(McmaApiRequestContext requestContext)
        {
            var jobId = requestContext.JobId();
            
            var job = await DataController.GetJobAsync($"{Options.PublicUrl}/jobs/{jobId}");
            if (job == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }

            if (job.Status != JobStatus.Completed &&
                job.Status != JobStatus.Failed &&
                job.Status != JobStatus.Canceled)
            {
                requestContext.SetResponseStatusCode(HttpStatusCode.Conflict, $"Cannot delete job while it is in non-final state ({job.Status})");
                return;
            }
            
            requestContext.SetResponseStatusCode(HttpStatusCode.Accepted);

            await WorkerInvoker.InvokeAsync("DeleteJob",
                                            new
                                            {
                                                jobId = job.Id
                                            },
                                            job.Tracker);
        }

        private async Task CancelAsync(McmaApiRequestContext requestContext)
        {
            var jobId = requestContext.JobId();
            
            var job = await DataController.GetJobAsync($"{Options.PublicUrl}/jobs/{jobId}");
            if (job == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }

            if (job.Status == JobStatus.Completed ||
                job.Status == JobStatus.Failed ||
                job.Status == JobStatus.Canceled)
            {
                requestContext.SetResponseStatusCode(HttpStatusCode.Conflict, $"Cannot cancel job when it is in final state ({job.Status})");
                return;
            }
            
            requestContext.SetResponseStatusCode(HttpStatusCode.Accepted);

            await WorkerInvoker.InvokeAsync("CancelJob",
                                            new
                                            {
                                                jobId = job.Id
                                            },
                                            job.Tracker);
        }

        private async Task RestartAsync(McmaApiRequestContext requestContext)
        {
            var jobId = requestContext.JobId();
            
            var job = await DataController.GetJobAsync($"{Options.PublicUrl}/jobs/{jobId}");
            if (job == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }

            if (job.Status != JobStatus.Completed &&
                job.Status != JobStatus.Failed &&
                job.Status != JobStatus.Canceled)
            {
                requestContext.SetResponseStatusCode(HttpStatusCode.Conflict, $"Cannot restart job while it is in non-final state ({job.Status})");
                return;
            }

            if (job.Deadline.HasValue && job.Deadline.Value < DateTime.UtcNow)
            {
                requestContext.SetResponseStatusCode(HttpStatusCode.Conflict, $"Cannot restart job when deadline is in the past ({job.Deadline})");
                return;
            }
            
            requestContext.SetResponseStatusCode(HttpStatusCode.Accepted);

            await WorkerInvoker.InvokeAsync("CancelJob",
                                            new
                                            {
                                                jobId = job.Id
                                            },
                                            job.Tracker);
        }
    }
}