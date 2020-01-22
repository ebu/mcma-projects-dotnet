#load "../../../tasks/task.csx"
#load "../../../tasks/task-runner.csx"
#load "../terraform-output.csx"
#load "./service-registry-populator.csx"

#r "nuget:Newtonsoft.Json, 11.0.2"
#r "nuget:Mcma.Core, 0.8.7"
#r "nuget:Mcma.Client, 0.8.7"
#r "nuget:Mcma.Azure.Client, 0.8.7"

using System;
using System.Text;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Mcma.Azure.Client;
using Mcma.Azure.Client.AzureAd;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Client;

public class ClearServiceRegistry : TaskBase
{
    protected override async Task<bool> ExecuteTask()
    {
        var serviceRegistryPopulator = new ServiceRegistryPopulator(TerraformOutput.Instance);

        var resourceManager = serviceRegistryPopulator.GetResourceManager();

        await resourceManager.InitAsync();

        foreach (var jobProfile in await resourceManager.QueryAsync<JobProfile>())
            await resourceManager.DeleteAsync(jobProfile);
        
        foreach (var service in  await resourceManager.QueryAsync<Service>())
            await resourceManager.DeleteAsync(service);

        return true;
    }
}