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

public class UpdateServiceRegistry : TaskBase
{

    protected override async Task<bool> ExecuteTask()
    {
        var serviceRegistryPopulator = new ServiceRegistryPopulator(TerraformOutput.Instance);

        var resourceManager = serviceRegistryPopulator.GetResourceManager();

        // ensure the service registry record exists
        await InsertOrUpdateServiceRegistryAsync(serviceRegistryPopulator, resourceManager);

        // re-initialize now that the service registry records is in place
        await resourceManager.InitAsync();

        // populate job profiles in the registry
        await PopulateJobProfilesAsync(serviceRegistryPopulator, resourceManager);

        // load service data with job profile IDs populated
        serviceRegistryPopulator.LoadServicesWithJobProfileIds();

        // populate services in the registry
        await PopulateServicesAsync(serviceRegistryPopulator, resourceManager);

        return true;
    }

    private async Task InsertOrUpdateServiceRegistryAsync(ServiceRegistryPopulator serviceRegistryPopulator, ResourceManager resourceManager)
    {
        var retrievedServiceRegistry = (await resourceManager.QueryAsync<Service>(("name", "Service Registry"))).FirstOrDefault();
        if (retrievedServiceRegistry != null)
        {
            serviceRegistryPopulator.ServiceRegistry.Id = retrievedServiceRegistry.Id;
            Console.WriteLine("Updating Service Registry");
            await resourceManager.UpdateAsync(serviceRegistryPopulator.ServiceRegistry);
        }
        else
        {
            Console.WriteLine("Inserting Service Registry");
            serviceRegistryPopulator.ServiceRegistry.Id = (await resourceManager.CreateAsync(serviceRegistryPopulator.ServiceRegistry)).Id;
        }
    }

    private async Task PopulateJobProfilesAsync(ServiceRegistryPopulator serviceRegistryPopulator, ResourceManager resourceManager)
        =>
        await PopulateAsync(
            resourceManager,
            (await resourceManager.QueryAsync<JobProfile>()).ToArray(),
            serviceRegistryPopulator.JobProfiles,
            jp => jp.Name);

    private async Task PopulateServicesAsync(ServiceRegistryPopulator serviceRegistryPopulator, ResourceManager resourceManager)
        =>
        await PopulateAsync(
            resourceManager,
            (await resourceManager.QueryAsync<Service>())
                .Where(s => !serviceRegistryPopulator.ServiceRegistry.Name.Equals(s.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray(),
            serviceRegistryPopulator.Services,
            s => s.Name);

    private async Task PopulateAsync<T>(ResourceManager resourceManager, T[] retrievedItems, T[] expectedItems, Func<T, string> getName) where T : McmaResource
    {
        foreach (var retrievedItem in retrievedItems)
        {
            // try to match on name
            var expectedItem = expectedItems.FirstOrDefault(i => getName(i).Equals(getName(retrievedItem), StringComparison.OrdinalIgnoreCase));
            if (expectedItem != null)
            {
                // if we found a matching item, set the expected item ID and do an update
                expectedItem.Id = retrievedItem.Id;
                Console.WriteLine("Updating " + typeof(T).Name + " '" + getName(expectedItem) + "'");
                await resourceManager.UpdateAsync(expectedItem);
            }
            else
            {
                // item was not matched in the expected list, so we need to remove it
                Console.WriteLine("Removing " + typeof(T).Name + " '" + getName(retrievedItem) + "'");
                await resourceManager.DeleteAsync(retrievedItem);
            }
        }

        // anything without an ID at this point needs to be created
        foreach (var itemToInsert in expectedItems.Where(s => s.Id == null))
        {
            Console.WriteLine("Inserting " + typeof(T).Name + " '" + getName(itemToInsert) + "'");
            itemToInsert.Id = (await resourceManager.CreateAsync(itemToInsert)).Id;
        }
    }
}