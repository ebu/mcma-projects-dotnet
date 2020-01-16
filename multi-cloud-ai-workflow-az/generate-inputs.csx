#load "./tasks/task-runner.csx"
#load "./tasks/task.csx"
#load "./tasks/az-cli.csx"

using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GenerateInputs : TaskBase
{
    protected override async Task<bool> ExecuteTask()
    {
        var inputsObj = JObject.FromObject(TaskRunner.Inputs);

        var azLogin = new AzLogin();
        if (!await azLogin.Run())
            return false;

        inputsObj["azureSubscriptionId"] = azLogin.Account["id"].Value<string>();
        inputsObj["azureTenantId"] = azLogin.Account["tenantId"].Value<string>();

        var azSpCreate = new AzSpCreate("terraform", "Owner");
        if (!await azSpCreate.Run())
            return false;

        inputsObj["azureClientId"] = azSpCreate.ServicePrincipal["appId"].Value<string>();
        inputsObj["azureClientSecret"] = azSpCreate.ServicePrincipal["password"].Value<string>();

        File.WriteAllText(TaskRunner.InputsFile, inputsObj.ToString(Formatting.Indented));

        return true;
    }
}