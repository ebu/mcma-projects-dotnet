#load "../../build/build.csx"

public class TerraformOutput
{
    private static TerraformOutput Instance { get; set; }

    private TerraformOutput(IDictionary<string, string> outputs)
    {
        Outputs = outputs;
        ServiceUrls = outputs.Where(x => x.Key.EndsWith("_url")).ToDictionary(x => x.Key.Replace("_url", string.Empty), x => x.Value);
    }

    private IDictionary<string, string> Outputs { get; }

    public IDictionary<string, string> ServiceUrls { get; }

    public string ServiceRegistryUrl => ServiceUrls["service_registry"];

    public string ServicesUrl => $"{ServiceRegistryUrl}services";

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

    public static TerraformOutput Load()
    {
        if (Instance == null)
        {
            var outputs = ParseContent(File.ReadAllText($"{Build.Dirs.Deployment.TrimEnd('/')}/terraform.output"));
            
            Instance = new TerraformOutput(outputs);
        }
        return Instance;
    }

    private static IDictionary<string, string> ParseContent(string content)
    {
        var settings = new Dictionary<string, string>();

        foreach (var line in content.Split('\n'))
        {
            var parts = line.Split('=');

            if (parts.Length > 1)
                settings[parts[0].Trim()] = string.Join("=", parts.Skip(1).Select(x => x.Trim()));
        }

        return settings;
    }
}