#load "../build/build.csx"
#load "../build/aggregate-task.csx"
#load "../build/cmd-task.csx"
#load "../build/terraform.csx"
#load "./registry/register.csx"

#r "nuget:Mcma.Azure.Client, 0.5.3.56"

using System.IO;
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
                // general vars
                .AppendLine($"environment_name                      = \"{Build.Inputs.environmentName}\"")
                .AppendLine($"environment_type                      = \"{Build.Inputs.environmentType}\"")
                .AppendLine($"global_prefix                         = \"{Build.Inputs.environmentName}-{Build.Inputs.environmentType}\"")
                .AppendLine($"global_prefix_lower_only              = \"{Build.Inputs.environmentName.ToLower()}{Build.Inputs.environmentType.ToLower()}\"")

                // AWS vars
                .AppendLine($"aws_account_id                        = \"{Build.Inputs.awsAccountId}\"")
                .AppendLine($"aws_access_key                        = \"{Build.Inputs.awsAccessKey}\"")
                .AppendLine($"aws_secret_key                        = \"{Build.Inputs.awsSecretKey}\"")
                .AppendLine($"aws_region                            = \"{Build.Inputs.awsRegion}\"")
                .AppendLine($"aws_instance_type                     = \"{Build.Inputs.awsInstanceType}\"")
                .AppendLine($"aws_instance_count                    = \"{Build.Inputs.awsInstanceCount}\"")
                .AppendLine($"upload_bucket                         = \"{Build.Inputs.environmentName}.{Build.Inputs.awsRegion}.{Build.Inputs.environmentType}.upload\"")
                .AppendLine($"temp_bucket                           = \"{Build.Inputs.environmentName}.{Build.Inputs.awsRegion}.{Build.Inputs.environmentType}.temp\"")
                .AppendLine($"repository_bucket                     = \"{Build.Inputs.environmentName}.{Build.Inputs.awsRegion}.{Build.Inputs.environmentType}.repository\"")
                .AppendLine($"website_bucket                        = \"{Build.Inputs.environmentName}.{Build.Inputs.awsRegion}.{Build.Inputs.environmentType}.website\"")
                
                // Azure vars
                .AppendLine($"private_encryption_key                = \"{File.ReadAllText(GenerateEncryptionKeys.PrivateKeyFile)}\"")
                .AppendLine($"azure_client_id                       = \"{Build.Inputs.azureClientId}\"")
                .AppendLine($"azure_client_secret                   = \"{Build.Inputs.azureClientSecret}\"")
                .AppendLine($"azure_tenant_id                       = \"{Build.Inputs.azureTenantId}\"")
                .AppendLine($"azure_tenant_name                     = \"{Build.Inputs.azureTenantName}\"")
                .AppendLine($"azure_subscription_id                 = \"{Build.Inputs.azureSubscriptionId}\"")
                .AppendLine($"azure_location                        = \"{Build.Inputs.azureLocation}\"")
                .AppendLine($"deploy_container                      = \"{Build.Inputs.environmentName}-{Build.Inputs.environmentType}-deploy\"")
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
                .Append("resource \"aws_s3_bucket_object\" \"file_" + idx++ + "\" {\r\n" )
                .Append("  bucket       = \"${aws_s3_bucket.website.bucket}\"\r\n")
                .Append("  key          = \"" + relativePath + "\"\r\n")
                .Append("  source       = \"" + fromRoot + "\"\r\n")
                .Append("  content_type = \"" + mimeType + "\"\r\n")
                .Append("  etag         = \"${filemd5(\"" + fromRoot + "\")}\"\r\n")
                .Append("}\r\n\r\n");
        }

        File.WriteAllText($"{Build.Dirs.Deployment}/storage/website.tf", tfFileContents.ToString());

        return Task.FromResult(true);
    }
}

public class GenerateAwsCredentialsJson : BuildTask
{
    protected override Task<bool> ExecuteTask() =>
        Task.Run(() =>
            File.WriteAllText($"{Build.Dirs.Deployment}/aws-credentials.json",
                new StringBuilder()
                    .AppendLine("{")
                    .AppendLine($"    \"accessKeyId\": \"{Build.Inputs.awsAccessKey}\",")
                    .AppendLine($"    \"secretAccessKey\": \"{Build.Inputs.awsSecretKey}\",")
                    .AppendLine($"    \"region\": \"{Build.Inputs.awsRegion}\"")
                    .AppendLine("}")
                    .ToString()))
            .ContinueWith(t => true);
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
    new GenerateTerraformWebsiteTf(),
    Terraform.Init(),
    Terraform.Apply(),
    new RetrieveTerraformOutput(),
    new GenerateAwsCredentialsJson(),
    new UpdateServiceRegistry());

public static readonly IBuildTask Destroy = new AggregateTask(
    new GenerateTerraformTfVars(),
    new GenerateTerraformWebsiteTf(),
    Terraform.Init(),
    Terraform.Destroy());