
using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.Sample.Scripts.Common
{
    public class TerraformOutput : JObject
    {
        private const string TerraformOutputPath = "../deployment/terraform.output.json";

        public TerraformOutput()
            : base(Parse(File.ReadAllText(TerraformOutputPath)))
        {
            ServiceRegistryUrl = this["service_registry_url"]?["value"]?.Value<string>();
            if (ServiceRegistryUrl == null)
                throw new Exception("Service registry url not set.");
            
            ServicesUrl = this["services_url"]?["value"]?.Value<string>();
            ServiceRegistryAuthType = this["services_auth_type"]?["value"]?.Value<string>();
            ServiceRegistryAuthContext = this["services_auth_context"]?["value"]?.Value<string>();

            UploadContainer = this["upload_container"]?["value"]?.Value<string>();
            OutputContainer = this["output_container"]?["value"]?.Value<string>();
            MediaStorageAccountName = this["media_storage_account_name"]?["value"]?.Value<string>();
            MediaStorageConnectionString = this["media_storage_connection_string"]?["value"]?.Value<string>();
        }
        
        public string ServiceRegistryUrl { get; }
        
        public string ServiceRegistryAuthType { get; }
        
        public string ServiceRegistryAuthContext { get; }
        
        public string UploadContainer { get; }
        
        public string OutputContainer { get; }
        
        public string MediaStorageAccountName { get; }
        
        public string MediaStorageConnectionString { get; }

        public string ServicesUrl { get; }
    }
}