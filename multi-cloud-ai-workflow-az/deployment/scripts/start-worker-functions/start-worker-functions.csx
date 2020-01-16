#load "../../../tasks/task.csx"
#load "../terraform-output.csx"

#r "nuget:Newtonsoft.Json, 11.0.2"

using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class StartWorkerFunctions : TaskBase
{
    protected override Task<bool> ExecuteTask()
    {
        var terraformOutput = TerraformOutput.Load();

        Console.WriteLine(JObject.FromObject(terraformOutput.WorkerKeys));

        return Task.FromResult(true);
    }
}