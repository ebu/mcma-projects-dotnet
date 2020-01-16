#load "./cmd-task.csx"
#load "./path-helper.csx"

#r "nuget:Newtonsoft.Json, 11.0.2"

using Newtonsoft.Json.Linq;

public class AzCli : CmdTask
{
    public AzCli(params string[] args)
        : base(PathHelper.Resolve("az"), args)
    {
    }
}

public class AzLogin : AzCli
{
    public AzLogin()
        : base("login")
    {
    }

    protected override bool RedirectStandardOutput => true;

    public JObject Account { get; private set; }

    protected override Task<bool> OnExit()
    {
        Account = JArray.Parse(StandardOutput).OfType<JObject>().FirstOrDefault();

        return Task.FromResult(true);
    }
}

public class AzSpCreate : AzCli
{
    public AzSpCreate(string name, string role)
        : base("ad", "sp", "create-for-rbac", "--name", name, "--role", role)
    {
    }

    protected override bool RedirectStandardOutput => true;

    public JObject ServicePrincipal { get; private set; }

    protected override Task<bool> OnExit()
    {
        ServicePrincipal = JObject.Parse(StandardOutput);

        return Task.FromResult(true);
    }
}