#load "../../../tasks/task.csx"
#load "../../../tasks/task-runner.csx"

#load "../terraform-output.csx"

#r "System.Net.Http"

using System.Net.Http;

public class PingWorkerFunctions : TaskBase
{
    private HttpClient HttpClient { get; } = new HttpClient();

    protected override async Task<bool> ExecuteTask()
    {
        var getResponses =
            await Task.WhenAll(
                TerraformOutput.Instance.WorkerUrls.Select(x => HttpClient.GetAsync(x).ContinueWith(t => t.Result.IsSuccessStatusCode)).ToArray());

        return getResponses.All(x => x);
    }
}