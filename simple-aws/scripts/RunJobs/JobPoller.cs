using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Client;

namespace Mcma.Aws.Sample.Scripts.RunJobs
{
    public class JobPoller
    {
        public JobPoller(IResourceManager resourceManager)
        {
            ResourceManager = resourceManager;
        }

        private IResourceManager ResourceManager { get; }
        
        public async Task<Dictionary<string, Job>> PollJobsForCompletionAsync(List<string> jobIds)
        {
            Console.WriteLine("Polling jobs for completion...");
            
            var pollUntil = DateTime.Now.AddMinutes(2);
            var completedJobs =
                await Task.WhenAll(
                    jobIds.Select(async jobId =>
                    {
                        var job = await ResourceManager.GetAsync<Job>(jobId);
                        while (job.Status != JobStatus.Completed && job.Status != JobStatus.Failed && job.Status != JobStatus.Canceled)
                        {
                            if (DateTime.Now >= pollUntil)
                                throw new McmaException("Timeout elapsed waiting for job to complete.");

                            await Task.Delay(3000);
                            job = await ResourceManager.GetAsync<Job>(jobId);
                        }

                        Console.WriteLine("Job " + jobId + " finished with status " + job.Status);
                
                        return job;
                    })
                );

            return completedJobs.ToDictionary(x => x.Id, x => x);
        }
    }
}
