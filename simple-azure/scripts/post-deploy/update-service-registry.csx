#r "nuget:Newtonsoft.Json, 12.0.3"
#r "nuget:Mcma.Core, 0.13.0"
#r "nuget:Mcma.Client, 0.13.0"
#r "nuget:Mcma.Azure.Client, 0.13.0"

#load "./json-data.csx"

using System;
using System.Text;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Mcma;
using Mcma.Azure.Client;
using Mcma.Azure.Client.AzureAd;
using Mcma.Client;
using Mcma.Serialization;

public class UpdateServiceRegistry
{
    public UpdateServiceRegistry(IResourceManagerProvider ResourceManagerProvider, JsonData jsonData)
    {
        ResourceManager = ResourceManagerProvider.Get(jsonData.ServicesUrl, jsonData.ServiceRegistry.AuthType, jsonData.ServiceRegistry.AuthContext);
        JsonData = jsonData;
    }

    private IResourceManager ResourceManager { get; }
    
    private JsonData JsonData { get; }

    public async Task ExecuteAsync()
    {
        // ensure the service registry record exists
        await InsertOrUpdateServiceRegistryAsync();

        // re-initialize now that the service registry records is in place
        await ResourceManager.InitAsync();

        // populate job profiles in the registry
        await PopulateJobProfilesAsync();

        // load service data with job profile IDs populated
        JsonData.SetJobProfileIds();

        // populate services in the registry
        await PopulateServicesAsync();
    }

    private async Task InsertOrUpdateServiceRegistryAsync()
    {
        var serviceRegistryServices = (await ResourceManager.QueryAsync<Service>(("name", "Service Registry"))).ToList();
        var retrievedServiceRegistry = serviceRegistryServices.FirstOrDefault();
        if (retrievedServiceRegistry != null)
        {
            JsonData.ServiceRegistry.Id = retrievedServiceRegistry.Id;
            Console.WriteLine("Updating Service Registry");
            await ResourceManager.UpdateAsync(JsonData.ServiceRegistry);

            if (serviceRegistryServices.Count > 1)
                foreach (var serviceToDelete in serviceRegistryServices.Skip(1))
                    await ResourceManager.DeleteAsync<Service>(serviceToDelete.Id);
        }
        else
        {
            Console.WriteLine("Inserting Service Registry");
            JsonData.ServiceRegistry.Id = (await ResourceManager.CreateAsync(JsonData.ServiceRegistry)).Id;
        }
    }

    private async Task PopulateJobProfilesAsync()
        =>
        await PopulateAsync(
            (await ResourceManager.QueryAsync<JobProfile>()).ToArray(),
            JsonData.JobProfiles.Value,
            jp => jp.Name);

    private async Task PopulateServicesAsync()
        =>
        await PopulateAsync(
            (await ResourceManager.QueryAsync<Service>())
                .Where(s => !JsonData.ServiceRegistry.Name.Equals(s.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray(),
            JsonData.Services.Value,
            s => s.Name);

    private async Task PopulateAsync<T>(T[] retrievedItems, T[] expectedItems, Func<T, string> getName) where T : McmaResource
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
                await ResourceManager.UpdateAsync(expectedItem);
            }
            else
            {
                // item was not matched in the expected list, so we need to remove it
                Console.WriteLine("Removing " + typeof(T).Name + " '" + getName(retrievedItem) + "'");
                await ResourceManager.DeleteAsync(retrievedItem);
            }
        }

        // anything without an ID at this point needs to be created
        foreach (var itemToInsert in expectedItems.Where(s => s.Id == null))
        {
            Console.WriteLine("Inserting " + typeof(T).Name + " '" + getName(itemToInsert) + "'");
            itemToInsert.Id = (await ResourceManager.CreateAsync(itemToInsert)).Id;
        }
    }
}