#load "build/task.csx"
#load "build/build.csx"
#load "build/aggregate-task.csx"
#load "build/path-helper.csx"

#load "services/build-tasks.csx"
#load "workflows/build-tasks.csx"
#load "website/build-tasks.csx"
#load "deployment/build-tasks.csx"
#load "deployment/registry/register.csx"
#load "deployment/registry/unregister.csx"

using System.Runtime.InteropServices;

Build.Dirs.Deployment = Terraform.DefaultProjectDir = $"{Build.RootDir.TrimEnd('/')}/deployment";

Build.ReadInputs(defaults: new Dictionary<string, string>
{
    ["awsInstanceType"] = "t2.micro",
    ["awsInstanceCount"] = "1",
    ["azureVideoIndexerLocation"] = "trial",
    ["azureVideoIndexerAccountId"] = "undefined",
    ["azureVideoIndexerSubscriptionKey"] = "undefined",
    ["azureVideoIndexerApiUrl"] = "https://api.videoindexer.ai"
});

// Windows seems to require full paths to the executables when using ProcessStartInfo
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Npm.Path = PathHelper.Where(Npm.Path);
    Terraform.Path = PathHelper.Where(Terraform.Path);
}

public static readonly IBuildTask BuildAll = new AggregateTask(BuildServices, BuildWorkflows, BuildWebsite);
public static readonly IBuildTask BuildAllSln = new AggregateTask(DotNetCli.Publish("."), BuildServicesSln, BuildWorkflowsSln, BuildWebsite);

Build.Tasks["buildServices"] = BuildServices;
Build.Tasks["buildWorkflows"] = BuildWorkflows;
Build.Tasks["buildWebsite"] = BuildWebsite;
//Build.Tasks["build"] = BuildAll;
Build.Tasks["build"] = BuildAllSln;
Build.Tasks["deployNoBuild"] = Deploy;
Build.Tasks["deploy"] = new AggregateTask(BuildAllSln, Deploy);
Build.Tasks["destroy"] = Destroy;
Build.Tasks["register"] = new UpdateServiceRegistry();
Build.Tasks["unregister"] = new ClearServiceRegistry();
Build.Tasks["tfOutput"] = new RetrieveTerraformOutput();
Build.Tasks["generateAwsCreds"] = new GenerateAwsCredentialsJson();
Build.Tasks["generateTfVars"] = new GenerateTerraformTfVars();
Build.Tasks["generateWebsiteTf"] = new GenerateTerraformWebsiteTf();
Build.Tasks["plan"] = Plan;

await Build.Run(Args?.FirstOrDefault());
