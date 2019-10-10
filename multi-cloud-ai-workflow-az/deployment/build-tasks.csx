#load "../build/build.csx"
#load "../build/aggregate-task.csx"
#load "../build/cmd-task.csx"
#load "../build/terraform.csx"
//#load "./registry/register.csx"

#r "nuget:Mcma.Azure.Client, 0.5.5.40"

using System.IO;
using System.Security.Cryptography;
using Mcma.Azure.Client;
using Newtonsoft.Json.Linq;

public class GenerateEncryptionKeys : BuildTask
{
    public static readonly string PrivateKeyFile = $"{Build.Dirs.Deployment}/private-key.json";
    
    public static readonly string PublicKeyFile = $"{Build.Dirs.Deployment}/public-key.json";

    protected override Task<bool> ExecuteTask()
    {
        if (File.Exists(PrivateKeyFile) && File.Exists(PublicKeyFile))
        {
            Console.WriteLine("Encryption key files already exist.");
            return Task.FromResult(true);
        }

        var (privateKey, publicKey) = EncryptionHelper.GenerateNewKeys();
        File.WriteAllText(PrivateKeyFile, privateKey);
        File.WriteAllText(PublicKeyFile, publicKey);

        return Task.FromResult(true);
    }
}

public class GenerateTerraformTfVars : BuildTask
{
    protected override Task<bool> ExecuteTask()
    {
        File.WriteAllText($"{Build.Dirs.Deployment}/terraform.tfvars", 
            new StringBuilder()
                .AppendLine($"private_encryption_key                = \"{File.ReadAllText(GenerateEncryptionKeys.PrivateKeyFile)}\"")
                .AppendLine($"environment_name                      = \"{Build.Inputs.environmentName}\"")
                .AppendLine($"environment_type                      = \"{Build.Inputs.environmentType}\"")
                .AppendLine($"global_prefix                         = \"{Build.Inputs.environmentName}-{Build.Inputs.environmentType}\"")
                .AppendLine($"global_prefix_lower_only              = \"{Build.Inputs.environmentName.ToLower()}{Build.Inputs.environmentType.ToLower()}\"")
                .AppendLine($"azure_client_id                       = \"{Build.Inputs.azureClientId}\"")
                .AppendLine($"azure_client_secret                   = \"{Build.Inputs.azureClientSecret}\"")
                .AppendLine($"azure_tenant_id                       = \"{Build.Inputs.azureTenantId}\"")
                .AppendLine($"azure_tenant_name                     = \"{Build.Inputs.azureTenantName}\"")
                .AppendLine($"azure_subscription_id                 = \"{Build.Inputs.azureSubscriptionId}\"")
                .AppendLine($"azure_location                        = \"{Build.Inputs.azureLocation}\"")
                .AppendLine($"aws_access_key                        = \"{Build.Inputs.awsAccessKey}\"")
                .AppendLine($"aws_secret_key                        = \"{Build.Inputs.awsSecretKey}\"")
                .AppendLine($"aws_region                            = \"{Build.Inputs.awsRegion}\"")
                .AppendLine($"deploy_container                      = \"{Build.Inputs.environmentName}-{Build.Inputs.environmentType}-deploy\"")
                .AppendLine($"upload_container                      = \"{Build.Inputs.environmentName}-{Build.Inputs.environmentType}-upload\"")
                .AppendLine($"temp_container                        = \"{Build.Inputs.environmentName}-{Build.Inputs.environmentType}-temp\"")
                .AppendLine($"repository_container                  = \"{Build.Inputs.environmentName}-{Build.Inputs.environmentType}-repository\"")
                .AppendLine($"website_container                     = \"{Build.Inputs.environmentName}-{Build.Inputs.environmentType}-website\"")
                .AppendLine($"azure_videoindexer_location           = \"{Build.Inputs.azureVideoIndexerLocation}\"")
                .AppendLine($"azure_videoindexer_account_id         = \"{Build.Inputs.azureVideoIndexerAccountId}\"")
                .AppendLine($"azure_videoindexer_subscription_key   = \"{Build.Inputs.azureVideoIndexerSubscriptionKey}\"")
                .AppendLine($"azure_videoindexer_api_url            = \"{Build.Inputs.azureVideoIndexerApiUrl}\"")
                .ToString());

        return Task.FromResult(true);
    }
}

public class GenerateTerraformWebsiteTf : BuildTask
{
    const string WebsiteDistDir = "website/dist/website";

    protected override Task<bool> ExecuteTask()
    {
        var tfFileContents = new StringBuilder();
        var idx = 0;

        foreach (var file in Directory.EnumerateFiles($"./{WebsiteDistDir}", "*.*", SearchOption.AllDirectories))
        {
            var mimeType = string.Empty;
            if (file.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                mimeType = "text/html";
            else if (file.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                mimeType = "text/css";
            else if (file.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                mimeType = "application/javascript";
            else if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                mimeType = "application/json";
            else if (file.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                mimeType = "image/x-icon";
            else if (file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                mimeType = "text/plain";
            else if (file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                mimeType = "image/svg+xml";

            var relativePath = file.Substring(file.IndexOf(WebsiteDistDir) + WebsiteDistDir.Length + 1).Replace("\\", "/");
            var fromRoot = file.Replace("\\", "/").Replace("./", "../");

            tfFileContents
                .Append("resource \"azurerm_storage_blob\" \"file_" + idx++ + "\" {\r\n" )
                .Append("  name                     = \"" + relativePath + "\"\r\n")
                .Append("  resource_group_name      = \"${var.resource_group_name}\"\r\n")
                .Append("  storage_account_name     = \"${azurerm_storage_account.website_storage_account.name}\"\r\n")
                .Append("  storage_container_name   = \"${azurerm_storage_container.website_container.name}\"\r\n")
                .Append("  source                   = \"" + fromRoot + "\"\r\n")
                .Append("  content_type             = \"" + mimeType + "\"\r\n")
                .Append("}\r\n\r\n");
        }

        File.WriteAllText($"{Build.Dirs.Deployment}/storage/website.tf", tfFileContents.ToString());

        return Task.FromResult(true);
    }
}

public class RetrieveTerraformOutput : Terraform
{
    public RetrieveTerraformOutput() : base("output") {}

    protected override bool RedirectStandardOutput => true;

    protected override Task<bool> OnExit()
    {
        Console.WriteLine("Writing to terraform.output:");
        Console.WriteLine(StandardOutput);
        File.WriteAllText($"{Build.Dirs.Deployment}/terraform.output", StandardOutput);
        return Task.FromResult(true);
    }
}

public static readonly IBuildTask Plan = new AggregateTask(
    new GenerateEncryptionKeys(),
    new GenerateTerraformTfVars(),
    new GenerateTerraformWebsiteTf(),
    Terraform.Init(),
    Terraform.Plan());

public static readonly IBuildTask Deploy = new AggregateTask(
    new GenerateEncryptionKeys(),
    new GenerateTerraformTfVars(),
    //new GenerateTerraformWebsiteTf(),
    Terraform.Init(),
    Terraform.Apply(),
    new RetrieveTerraformOutput(),
    //new GenerateAwsCredentialsJson(),
    new UpdateServiceRegistry());

public static readonly IBuildTask Destroy = new AggregateTask(
    new GenerateEncryptionKeys(),
    new GenerateTerraformTfVars(),
    new GenerateTerraformWebsiteTf(),
    Terraform.Init(),
    Terraform.Destroy());