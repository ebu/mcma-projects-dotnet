#load "../tasks/task-runner.csx"
#load "../tasks/aggregate-task.csx"
#load "../tasks/cmd-task.csx"
#load "../tasks/file-changes.csx"
#load "../tasks/terraform.csx"

#load "./scripts/tasks.csx"

using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

public class GenerateTerraformTfVars : TaskBase
{
    protected override Task<bool> ExecuteTask()
    {
        var tfVarsBuilder = 
            new StringBuilder()
                .AppendLine($"environment_name                      = \"{TaskRunner.Inputs.environmentName}\"")
                .AppendLine($"environment_type                      = \"{TaskRunner.Inputs.environmentType}\"")
                .AppendLine($"global_prefix                         = \"{TaskRunner.Inputs.environmentName}-{TaskRunner.Inputs.environmentType}\"")
                .AppendLine($"global_prefix_lower_only              = \"{TaskRunner.Inputs.environmentName.ToLower()}{TaskRunner.Inputs.environmentType.ToLower()}\"")
                .AppendLine($"azure_client_id                       = \"{TaskRunner.Inputs.azureClientId}\"")
                .AppendLine($"azure_client_secret                   = \"{TaskRunner.Inputs.azureClientSecret}\"")
                .AppendLine($"azure_tenant_id                       = \"{TaskRunner.Inputs.azureTenantId}\"")
                .AppendLine($"azure_subscription_id                 = \"{TaskRunner.Inputs.azureSubscriptionId}\"")
                .AppendLine($"azure_location                        = \"{TaskRunner.Inputs.azureLocation}\"")
                .AppendLine($"aws_access_key                        = \"{TaskRunner.Inputs.awsAccessKey}\"")
                .AppendLine($"aws_secret_key                        = \"{TaskRunner.Inputs.awsSecretKey}\"")
                .AppendLine($"aws_region                            = \"{TaskRunner.Inputs.awsRegion}\"")
                .AppendLine($"deploy_container                      = \"{TaskRunner.Inputs.environmentName}-{TaskRunner.Inputs.environmentType}-deploy\"")
                .AppendLine($"upload_container                      = \"{TaskRunner.Inputs.environmentName}-{TaskRunner.Inputs.environmentType}-upload\"")
                .AppendLine($"temp_container                        = \"{TaskRunner.Inputs.environmentName}-{TaskRunner.Inputs.environmentType}-temp\"")
                .AppendLine($"repository_container                  = \"{TaskRunner.Inputs.environmentName}-{TaskRunner.Inputs.environmentType}-repository\"")
                .AppendLine($"preview_container                     = \"{TaskRunner.Inputs.environmentName}-{TaskRunner.Inputs.environmentType}-preview\"")
                .AppendLine($"website_container                     = \"{TaskRunner.Inputs.environmentName}-{TaskRunner.Inputs.environmentType}-website\"")
                .AppendLine($"azure_videoindexer_location           = \"{TaskRunner.Inputs.azureVideoIndexerLocation}\"")
                .AppendLine($"azure_videoindexer_account_id         = \"{TaskRunner.Inputs.azureVideoIndexerAccountId}\"")
                .AppendLine($"azure_videoindexer_subscription_key   = \"{TaskRunner.Inputs.azureVideoIndexerSubscriptionKey}\"")
                .AppendLine($"azure_videoindexer_api_url            = \"{TaskRunner.Inputs.azureVideoIndexerApiUrl}\"");

        File.WriteAllText($"{TaskRunner.Dirs.Deployment}/terraform.tfvars", tfVarsBuilder.ToString());

        return Task.FromResult(true);
    }
}

public class GenerateTerraformWebsiteTf : TaskBase
{
    const string WebsiteDistDir = "website/dist/website";

    private ITask CheckDistFilesForChanges { get; } = new CheckForFileChanges($"./{WebsiteDistDir}", null);

    protected override Task<bool> ExecuteTask()
    {
        var tfFileContents = new StringBuilder();

        var md5 = MD5.Create();

        foreach (var file in Directory.EnumerateFiles($"./{WebsiteDistDir}", "*.*", SearchOption.AllDirectories))
        {
            // skip the config.json, as this will generated later and shouldn't be managed by terraform
            if (file.EndsWith("config.json", StringComparison.OrdinalIgnoreCase))
                continue;

            string mimeType;
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
            else if (file.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
                mimeType = "application/octet-stream";
            else
                continue;

            var relativePath = file.Substring(file.IndexOf(WebsiteDistDir) + WebsiteDistDir.Length + 1).Replace("\\", "/");
            var filePathFromRoot = file.Replace("\\", "/").Replace("./", "../");
            var absolutePath = Path.GetFullPath(Path.Combine(TaskRunner.RootDir, file));
            var fileHash = Convert.ToBase64String(md5.ComputeHash(File.ReadAllBytes(absolutePath))).Replace("=", "-").Replace("+", "-").Replace("/", "-");

            tfFileContents
                .AppendLine("resource \"azurerm_storage_blob\" \"website_file_" + fileHash + "\" {" )
                .AppendLine("  name                     = \"" + relativePath + "\"")
                .AppendLine("  resource_group_name      = var.resource_group_name")
                .AppendLine("  storage_account_name     = azurerm_storage_account.website_storage_account.name")
                .AppendLine("  storage_container_name   = azurerm_storage_container.website_container.name")
                .AppendLine("  type                     = \"block\"")
                .AppendLine("  source                   = \"" + filePathFromRoot + "\"")
                .AppendLine("  content_type             = \"" + mimeType + "\"")
                // .AppendLine()
                // .AppendLine("  metadata = {")
                // .AppendLine("    hash = filesha256(\"" + filePathFromRoot + "\")")
                // .AppendLine("  }")
                .AppendLine("}")
                .AppendLine();
        }

        File.WriteAllText($"{TaskRunner.Dirs.Deployment}/storage/website-files.tf", tfFileContents.ToString());

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
        File.WriteAllText($"{TaskRunner.Dirs.Deployment}/terraform.output", StandardOutput);
        return Task.FromResult(true);
    }
}

public static readonly ITask Plan = new AggregateTask(
    new GenerateTerraformTfVars(),
    new GenerateTerraformWebsiteTf(),
    Terraform.Init(),
    Terraform.Plan());

public static readonly ITask Deploy = new AggregateTask(
    new GenerateTerraformTfVars(),
    new GenerateTerraformWebsiteTf(),
    Terraform.Init(),
    Terraform.Apply(),
    new RetrieveTerraformOutput(),
    Scripts.PostDeploy);

public static readonly ITask Destroy = new AggregateTask(
    Terraform.Init(),
    Terraform.Destroy());