#r "nuget:AWSSDK.Core, 3.5.1.20"
#r "nuget:Mcma.Aws.Client, 0.13.16"

#load "../aws-creds.csx"
#load "./json-data.csx"
#load "./update-service-registry.csx"

using System;
using Amazon;
using Mcma.Aws.Client;
using Mcma.Client;
using Newtonsoft.Json.Linq;

const string TerraformOutputPath = "../../deployment/terraform.output.json";
const string AwsCredentialsPath = "../../deployment/aws-credentials.json";

async Task ExecuteAsync()
{
    try
    {
        var jsonData = JsonData.Load(TerraformOutputPath);
        var awsCreds = AwsCredentials.Load(AwsCredentialsPath);
        var resourceManagerProvider = new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(awsCreds.AuthContext));

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