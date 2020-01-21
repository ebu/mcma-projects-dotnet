# multi-cloud-ai-workflow

This example workflow demonstrates how you can leverage AI technologies from multiple cloud vendors in a single media workflow


## Requirements for running the example
* .NET Core 2.1 SDK or higher ([.NET Core Downloads](https://dotnet.microsoft.com/download))
* The `dotnet script` global tool ([dotnet-script on GitHub](https://github.com/filipw/dotnet-script)). Onc thee .NET Core CLI is installed, you can install this as a global tool from the cmd line with the following:
```
dotnet tool install -g dotnet-script
```
* Latest version of Terraform and available in PATH. See the [Terraform website](https://www.terraform.io/)
* AWS account
* Azure video indexer account, a free account can be used for testing. Follow these instructions: https://docs.microsoft.com/en-us/azure/cognitive-services/video-indexer/video-indexer-use-apis


## Setup procedure
1. Clone this repository to your local harddrive
2. Navigate to the `multi-cloud-ai-workflow` folder.
3. Create file named build.inputs
4. Add the following information to the created file and update the parameter values reflecting your AWS account and Azure account 
```
# Mandatory settings

environmentName=com.your-domain.mcma
environmentType=dev

awsAccountId=<YOUR_AWS_ACCOUNT_ID>
awsAccessKey=<YOUR_AWS_ACCESS_KEY>
awsSecretKey=<YOUR_AWS_SECRET_KEY>
awsRegion=<YOUR_AWS_REGION>

# Optional settings, though without configuration some features may not work

awsInstanceType=<EC2_TRANSFORM_SERVICE_INSTANCE_TYPE - DEFAULTS TO "t2.micro">
awsInstanceCount=<EC2_TRANSFORM_SERVICE_INSTANCE_COUNT - DEFAULTS TO "1">

AzureLocation =  <YOUR AZURE REGION - USE "trial" FOR TESTING>
AzureVideoIndexerAccountID = <YOUR AZURE Video Indexer Account ID> 
AzureVideoIndexerSubscriptionKey = <YOUR AZURE SUBSCRIPTION KEY>
AzureVideoIndexerApiUrl = <AZURE VIDEO API END[POINT DEFAULT IS: https://api.videoindexer.ai>

```

5. Save the file.
6. Open command line in `multi-cloud-ai-workflow` folder.
7. Execute the deploy script. This can take a few minutes.
    * Windows: `./build.bat deploy`
    * Mac/Linux: `./build.sh deploy`
8. If no errors have occured until now you have successfully setup the infrastructure in your AWS cloud. Go to https://aws.amazon.com/console/ and sign in to see your cloud infrastructure. In case you do have errors it may be that your environmentName is either too long or not unique enough to guarantee unique names for your cloud resources e.g. bucket names.
