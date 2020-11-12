using Mcma.GoogleCloud.Storage.Proxies;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.GoogleCloud.Sample.Scripts.RunJobs
{
    public static class RunJobsServiceCollectionExtensions
    {
        public static IServiceCollection AddRunJobsDependencies(this IServiceCollection services)
            => services.AddSingleton<FileUploader>()
                       .AddSingleton<JobInitiator>()
                       .AddSingleton<JobPoller>()
                       .AddMcmaCloudStorage(builder => builder.CredentialsPath = "../google-cloud-credentials.json");
    }
}