#load "./tasks/task.csx"
#load "./tasks/task-runner.csx"
#load "./tasks/aggregate-task.csx"
#load "./tasks/path-helper.csx"
#load "./tasks/az-cli.csx"

#load "./services/tasks.csx"
#load "./website/tasks.csx"
#load "./deployment/tasks.csx"
#load "./deployment/scripts/tasks.csx"

TaskRunner.Dirs.Deployment = Terraform.DefaultProjectDir = $"{TaskRunner.RootDir.TrimEnd('/')}/deployment";

TaskRunner.ReadInputs(defaults: new Dictionary<string, string>
{
    ["azureVideoIndexerLocation"] = "trial",
    ["azureVideoIndexerApiUrl"] = "https://api.videoindexer.ai"
});

public static readonly ITask BuildAll = new AggregateTask(DotNetCli.Publish("."), BuildServices, BuildWebsite);

private static readonly ITask CliLogin = new AzLogin(TaskRunner.Inputs.azureClientId, TaskRunner.Inputs.azureClientSecret, TaskRunner.Inputs.azureTenantId, true);

TaskRunner.Tasks["cliLogin"] = CliLogin;
TaskRunner.Tasks["buildServices"] = BuildServices;
TaskRunner.Tasks["buildWebsite"] = BuildWebsite;
TaskRunner.Tasks["build"] = BuildAll;
TaskRunner.Tasks["deployNoBuild"] = Deploy;
TaskRunner.Tasks["deploy"] = new AggregateTask(BuildAll, Deploy);
TaskRunner.Tasks["destroy"] = Destroy;
TaskRunner.Tasks["postDeploy"] = Scripts.PostDeploy;
TaskRunner.Tasks["unregisterAll"] = Scripts.ClearServiceRegistry;
TaskRunner.Tasks["tfOutput"] = new RetrieveTerraformOutput();
TaskRunner.Tasks["generateTfVars"] = new GenerateTerraformTfVars();
TaskRunner.Tasks["generateWebsiteTf"] = new GenerateTerraformWebsiteTf();
TaskRunner.Tasks["plan"] = Plan;

await TaskRunner.Run(Args?.FirstOrDefault(), CliLogin);
