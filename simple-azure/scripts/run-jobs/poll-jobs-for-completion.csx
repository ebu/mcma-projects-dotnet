using Mcma;
using Mcma.Client;

public static async Task<Dictionary<string, Job>> PollJobsForCompletionAsync(IResourceManager resourceManager, List<string> jobIds)
{
    var pollUntil = DateTime.Now.AddMinutes(2);
    var completedJobs =
        await Task.WhenAll(
            jobIds.Select(async jobId =>
            {
                var job = await resourceManager.GetAsync<Job>(jobId);
                while (job.Status != JobStatus.Completed && job.Status != JobStatus.Failed && job.Status != JobStatus.Canceled)
                {
                    if (DateTime.Now >= pollUntil)
                        throw new McmaException("Timeout elapsed waiting for job to complete.");

                    await Task.Delay(3000);
                    job = await resourceManager.GetAsync<Job>(jobId);
                }

                Console.WriteLine("Job " + jobId + " finished with status " + job.Status);
                
                return job;
            })
        );

    return completedJobs.ToDictionary(x => x.Id, x => x);
}