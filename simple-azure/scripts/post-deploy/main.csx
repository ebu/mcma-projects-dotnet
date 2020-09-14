#r "nuget:Newtonsoft.Json, 12.0.3"
#r "nuget:Mcma.Azure.Client, 0.13.0"

#load "./json-data.csx"
#load "./update-service-registry.csx"

using Newtonsoft.Json.Linq;
using Mcma.Azure.Client;
using Mcma.Client;

var azureTenantId = Args.FirstOrDefault(x => x.StartsWith("--azureTenantId="))?.Replace("--azureTenantId=", string.Empty);
var azureClientId = Args.FirstOrDefault(x => x.StartsWith("--azureClientId="))?.Replace("--azureClientId=", string.Empty);
var azureClientSecret = Args.FirstOrDefault(x => x.StartsWith("--azureClientSecret="))?.Replace("--azureClientSecret=", string.Empty);

var resourceManagerProvider = 
    new ResourceManagerProvider(
        new AuthProvider().AddAzureAdConfidentialClientAuth(azureTenantId, azureClientId, azureClientSecret));

const string TerraformOutputPath = "../../deployment/terraform.output.json";

async Task ExecuteAsync()
{
    try
    {
        var jsonData = JsonData.Load(TerraformOutputPath);

        var updateServiceRegistry = new UpdateServiceRegistry(resourceManagerProvider, jsonData);

        await updateServiceRegistry.ExecuteAsync();
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error);
    }
}

await ExecuteAsync();

Console.WriteLine("Done");