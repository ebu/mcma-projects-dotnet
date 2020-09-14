using Mcma;
using Mcma.Client;

public static async Task<Dictionary<string, Job>> PollJobsForCompletionAsync(IResourceManager resourceManager, List<string> jobIds)
{
    var completedJobs =
        await Task.WhenAll(
            jobIds.Select(async jobId =>
            {
                var job = await resourceManager.GetAsync<Job>(jobId);

                while (job.Status != JobStatus.Completed && job.Status != JobStatus.Failed && job.Status != JobStatus.Canceled)
                {
                    await Task.Delay(3000);
                    job = await resourceManager.GetAsync<Job>(jobId);
                }

                Console.WriteLine("Job " + jobId + " finished with status " + job.Status);
                
                return job;
            })
        );

    return completedJobs.ToDictionary(x => x.Id, x => x);
}