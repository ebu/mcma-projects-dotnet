# MCMA Multi-Cloud AI Workflow in Azure

This example workflow demonstrates how you can leverage AI technologies from multiple cloud vendors in a single media workflow hosted in Azure


## Requirements for running the example
* .NET Core 2.1 SDK or higher ([.NET Core Downloads](https://dotnet.microsoft.com/download))
* The `dotnet script` global tool ([dotnet-script on GitHub](https://github.com/filipw/dotnet-script)). Once the .NET Core CLI is installed, you can install this as a global tool from the cmd line with the following:
```
dotnet tool install -g dotnet-script
```
* Latest version of Terraform and available in PATH. See the [Terraform website](https://www.terraform.io/)
* The Azure CLI. Download and install from [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
* An Azure account with an existing subscription
    * As part of the setup process, we will configure a service principal for use by Terraform deployment process (see [Setup in Azure Portal](#setup-in-azure-portal) below)
* AWS account with access key and secret key that can be used to deploy resources
* Azure video indexer account. A free account can be used for testing. Follow these instructions: [https://docs.microsoft.com/en-us/azure/cognitive-services/video-indexer/video-indexer-use-apis

## Setup procedure
1. Clone this repository to your local harddrive
2. Navigate to the `multi-cloud-ai-workflow-az` folder.
3. Create a new file named `task-inputs.json`
4. Add the following information to the created file and update the parameter values reflecting your AWS account and Azure account (see [Setup in Azure Portal](#setup-in-azure-portal) below)
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

## Setup in Azure Portal
1. Login to the Azure Portal
2. On the landing page, click on `Subscriptions`, copy your subscription ID, and set it as the `azureSubscriptionId` in the `task-inputs.json`:
![Copy Subscription ID](https://raw.githubusercontent.com/ebu/mcma-projects-dotnet/master/multi-cloud-ai-workflow-az/screenshots/terraform-sp-setup-6.png)
3. Open Azure AD
4. On the landing page, copy the tenantID and set it as the `azureTenantId` in the `task-inputs.json`:
![Copy Tenant ID](https://raw.githubusercontent.com/ebu/mcma-projects-dotnet/master/multi-cloud-ai-workflow-az/screenshots/terraform-sp-setup-7.png)
5. In the Default Directory, find the `App registrations` section on the left-hand side
6. Create a new App Registration for Terraform:
![Create new App Registration](https://raw.githubusercontent.com/ebu/mcma-projects-dotnet/master/multi-cloud-ai-workflow-az/screenshots/terraform-sp-setup-1.png)
7. Copy the client ID from the new app registration and set it as the `azureClientId` in the `task-inputs.json`:
![Copy App Registration Client ID](https://raw.githubusercontent.com/ebu/mcma-projects-dotnet/master/multi-cloud-ai-workflow-az/screenshots/terraform-sp-setup-3.png)
8. Under the new App Registration, click `Certificates & secrets` on the left-hand side
9. Click on `New client secret` to generate a new key:
![Generate App Registration Client Secret](https://raw.githubusercontent.com/ebu/mcma-projects-dotnet/master/multi-cloud-ai-workflow-az/screenshots/terraform-sp-setup-5.png)
10. Copy the new key and set it as the `azureClientSecret` in the `task-inputs.json`:
![Copy App Registration Client Secret](https://raw.githubusercontent.com/ebu/mcma-projects-dotnet/master/multi-cloud-ai-workflow-az/screenshots/terraform-sp-setup-8.png)
11. Return to the Default Directory and find the `Roles and administrators` section on the left-hand side
12. Find the `Global administrator` built-in role and click on it
13. Add your new Terraform Service Principal to the role:
![Make Terraform an Admin](https://raw.githubusercontent.com/ebu/mcma-projects-dotnet/master/multi-cloud-ai-workflow-az/screenshots/terraform-sp-setup-2.png)
14. Return to the main page and open your subscription
15. On the right-hand side, click on `Access control (IAM)`
16. Click on `+ Add` to add a new role assignment, filter for the Owner role, find the terraform service principal, and save:
![Make Terraform an Admin](https://raw.githubusercontent.com/ebu/mcma-projects-dotnet/master/multi-cloud-ai-workflow-az/screenshots/terraform-sp-setup-9.png)
