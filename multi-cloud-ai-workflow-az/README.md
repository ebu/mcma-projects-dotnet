# MCMA Multi-Cloud AI Workflow in Azure

This example workflow demonstrates how you can leverage AI technologies from multiple cloud vendors in a single media workflow hosted in Azure


## Requirements for running the example
* .NET Core 2.1 SDK or higher ([.NET Core Downloads](https://dotnet.microsoft.com/download))
* The `dotnet script` global tool ([dotnet-script on GitHub](https://github.com/filipw/dotnet-script)). Once the .NET Core CLI is installed, you can install this as a global tool from the cmd line with the following:
```
dotnet tool install -g dotnet-script
```
* Latest version of Terraform and available in PATH. See the [Terraform website](https://www.terraform.io/)
* Azure account with a service principal for use by Terraform deployment process (see [Setting up your Service Principal](##Setting up your Service Principal) below)
* AWS account
* Azure video indexer account, a free account can be used for testing. Follow these instructions: https://docs.microsoft.com/en-us/azure/cognitive-services/video-indexer/video-indexer-use-apis

## Setting up your Service Principal
1. Login to the Azure Portal
2. Open Azure AD
3. On the left-hand side, find the App Registrations section
4. Create a new App Registration for Terraform
[Create new App Registration](https://raw.githubusercontent.com/ebu/mcma-projects-dotnet/master/multi-cloud-ai-workflow-az/screenshots/terraform-sp-setup-1.png)

## Setup procedure
1. Clone this repository to your local harddrive
2. Navigate to the `multi-cloud-ai-workflow-az` folder.
3. Create a new file named `task-inputs.json`
4. Add the following information to the created file and update the parameter values reflecting your AWS account and Azure account 
```jsonc
{
  // general MCMA settings
  "environmentName": "",                    // A unique name for grouping your resources
  "environmentType": "",                    // The type of environment being deployed, e.g. dev, beta, production, etc

  // Azure account settings
  "azureSubscriptionId": "",                // The ID of the subscription in which you want to create Azure resources
  "azureTenantId": "",                      // The ID of the Azure AD tenant to be used by your Azure resources
  "azureLocation": "",                      // The region to which to deploy your Azure resources
  "azureClientId": "",                      // The client ID for your terraform app registration created above
  "azureClientSecret": "",                  // The client sercret for your terraform app registration created above

  // AWS settings (used to interact with AWS AI services)
  "awsAccessKey": "",                       // An access key for a user in AWS that's permissioned to deploy resources
  "awsSecretKey": "",                       // A secret key for a user in AWS that's permissioned to deploy resources
  "awsRegion": "",                          // The region to which to deploy AWS resources

  // Azure VideoIndexer settings
  "azureVideoIndexerAccountId": "",         // The ID of the VideoIndexer account to use (see instructions above for creating account)
  "azureVideoIndexerSubscriptionKey": ""    // The key for the VideoIndexer account to use
}
```

5. Save the file.
6. Open command line in `multi-cloud-ai-workflow-az` folder.
7. Execute the deploy script. This can take a few minutes.
    * Windows: `./tasks deploy`
    * Mac/Linux: `./tasks.sh deploy`
8. If no errors have occured until now you have successfully setup the infrastructure in your Azure cloud. Go to https://portal.azure.com/ and sign in to see your cloud infrastructure.
