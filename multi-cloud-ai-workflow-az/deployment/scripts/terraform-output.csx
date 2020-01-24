#load "../../tasks/task-runner.csx"

public class TerraformOutput
{
    private static TerraformOutput _instance;

    private TerraformOutput(IDictionary<string, string> outputs)
    {
        Outputs = outputs;

        ServiceUrls = outputs.Where(x => x.Key.EndsWith("_url") && !x.Key.EndsWith("_worker_url")).ToDictionary(x => x.Key.Replace("_url", string.Empty), x => x.Value);
        WorkerUrls = outputs.Where(x => x.Key.EndsWith("_worker_url")).Select(kvp => kvp.Value).ToArray();
    }

    public static TerraformOutput Instance => _instance ?? (_instance = new TerraformOutput(ParseContent()));

    private IDictionary<string, string> Outputs { get; }

    public IDictionary<string, string> ServiceUrls { get; }

    public string ServiceRegistryUrl => ServiceUrls["service_registry"];

    public string ServicesUrl => $"{ServiceRegistryUrl}services";

    public string ServiceRegistryAppId => Outputs["service_registry_app_id"];

    public string JobProfilesUrl => $"{ServiceRegistryUrl}job-profiles";

    public string WebsiteStorageConnectionString => Outputs["website_storage_connection_string"];

    public string WebsiteStorageAccountName => Outputs["website_storage_account_name"];

    public string WebsiteContainer => Outputs["website_container"];

    public string MediaStorageAccountName => Outputs["media_storage_account_name"];

    public string MediaStorageConnectionString => Outputs["media_storage_connection_string"];

    public string UploadContainer => Outputs["upload_container"];

    public string WebsiteClientId => Outputs["website_client_id"];

    public string ServiceRegistryScope => Outputs["service_registry_scope"];

    public string JobRepositoryUrl => ServiceUrls["job_repository"];

    public string JobRepositoryScope => Outputs["job_repository_scope"];
    
    public string WebsiteUrl => $"{Outputs["website_url"]}/index.html";

    public string[] WorkerUrls { get; }

    private static IDictionary<string, string> ParseContent()
    {
        var settings = new Dictionary<string, string>();
        var tfOutput = $"{TaskRunner.Dirs.Deployment.TrimEnd('/')}/terraform.output";

        if (!File.Exists(tfOutput))
            return settings;

        var content = File.ReadAllText(tfOutput);

        foreach (var line in content.Split('\n'))
        {
            var parts = line.Split('=');

            if (parts.Length > 1)
            {
                var key = parts[0].Trim();
                var value = string.Join("=", parts.Skip(1).Select(x => x.Trim()));
                
                settings[key] = value;
            }
        }

        return settings;
    }
}