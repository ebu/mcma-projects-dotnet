#load "../../build/task.csx"
#load "../../build/build.csx"
#load "./terraform-output.csx"
#load "./service-registry-populator.csx"

#r "nuget:Newtonsoft.Json, 11.0.2"
#r "nuget:Mcma.Core, 0.8.6-beta1"
#r "nuget:Mcma.Client, 0.8.6-beta1"
#r "nuget:Mcma.Azure.Client, 0.8.6-beta1"

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

public class ClearServiceRegistry : BuildTask
{
    private IResourceManagerProvider ResourceManagerProvider { get; } =
        new ResourceManagerProvider(
            new AuthProvider().AddAzureAdConfidentialClientAuth(
                (string)Build.Inputs.azureTenantId,
                (string)Build.Inputs.azureClientId,
                (string)Build.Inputs.azureClientSecret));

    private ServiceRegistryPopulator ServiceRegistryPopulator { get; } = new ServiceRegistryPopulator(TerraformOutput.Load());                

    protected override async Task<bool> ExecuteTask()
    {
        var resourceManager = ServiceRegistryPopulator.GetResourceManager(ResourceManagerProvider);

        await resourceManager.InitAsync();

        foreach (var jobProfile in await resourceManager.QueryAsync<JobProfile>())
            await resourceManager.DeleteAsync(jobProfile);
        
        foreach (var service in  await resourceManager.QueryAsync<Service>())
            await resourceManager.DeleteAsync(service);

        return true;
    }
}