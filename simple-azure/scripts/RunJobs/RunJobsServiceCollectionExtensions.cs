using Microsoft.Extensions.DependencyInjection;

namespace Mcma.Azure.Sample.Scripts.RunJobs
{
    public static class RunJobsServiceCollectionExtensions
    {
        public static IServiceCollection AddRunJobsDependencies(this IServiceCollection services)
            => services.AddSingleton<FileUploader>()
                       .AddSingleton<JobInitiator>()
                       .AddSingleton<JobPoller>();
    }
}