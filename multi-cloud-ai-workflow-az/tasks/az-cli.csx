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

public class AzAccountShow : AzCli
{
    public AzAccountShow()
        : base("account", "show")
    {
    }

    protected override bool RedirectStandardOutput => true;

    public JObject Account { get; private set; }

    protected override Task<bool> OnExit()
    {
        try
        {
            Account = JObject.Parse(StandardOutput);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

public class AzLogin : AzCli
{
    public AzLogin()
        : base("login")
    {
    }

    public AzLogin(string userName, string password, string tenant, bool isServicePrincipal)
        : base("login", isServicePrincipal ? "--service-principal" : "", "-u", userName, "-p", password, "--tenant", tenant)
    {
    }

    protected override bool RedirectStandardOutput => true;

    public JObject Account { get; private set; }

    protected override Task<bool> OnExit()
    {
        try
        {
            Account = JArray.Parse(StandardOutput).OfType<JObject>().FirstOrDefault();
            
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

public class AzSpCreate : AzCli
{
    public AzSpCreate(string name, string role, string subscriptionId)
        : base("ad", "sp", "create-for-rbac", $"--name={name}", $"--role={role}", $"--scopes=/subscriptions/{subscriptionId}")
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

public class AzAdAppPermissionGrant : AzCli
{
    public AzAdAppPermissionGrant(string appToAccessId, string callingAppId, string scope, string consentType = "AllPrincipals")
        : base("ad", "app", "permission", "grant", $"--id={callingAppId}", $"--api={appToAccessId}", $"--scope={scope}", $"--consent-type={consentType}")
    {
    }
}