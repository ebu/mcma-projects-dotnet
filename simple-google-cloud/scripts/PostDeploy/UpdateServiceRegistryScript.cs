using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.GoogleCloud.Sample.Scripts.Common;

namespace Mcma.GoogleCloud.Sample.Scripts.PostDeploy
{
    public class UpdateServiceRegistryScript : IScript
    {
        public UpdateServiceRegistryScript(IResourceManager resourceManager, JsonData jsonData)
        {
            ResourceManager = resourceManager;
            JsonData = jsonData;
        }

        private IResourceManager ResourceManager { get; }

        private JsonData JsonData { get; }

        public async Task ExecuteAsync(params string[] args)
        {
            // ensure the service registry record exists
            await InsertOrUpdateServiceRegistryAsync();

            // re-initialize now that the service registry records is in place
            await ResourceManager.InitAsync();

            // populate job profiles in the registry
            await PopulateJobProfilesAsync();

            // load service data with job profile IDs populated
            SetJobProfileIds();

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
         

         private void SetJobProfileIds()
         {
             foreach (var service in JsonData.Services.Value.Where(s => s.JobProfileIds != null && s.JobProfileIds.Length > 0))
             {
                 for (var i = 0; i < service.JobProfileIds.Length; i++)
                 {
                     var jobProfileName = service.JobProfileIds[i];

                     var jobProfileId = JsonData.JobProfiles.Value.FirstOrDefault(p => p.Name.Equals(jobProfileName, StringComparison.OrdinalIgnoreCase))?.Id;
                     if (jobProfileId == null)
                         throw new Exception($"Service {service.Name} references job profile '{jobProfileName}', but the profile has not been defined.");
                    
                     service.JobProfileIds[i] = jobProfileId;
                 }
             }
         }

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
                     await ResourceManager.DeleteAsync<T>(retrievedItem.Id);
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
}