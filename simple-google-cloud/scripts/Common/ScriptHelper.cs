using System;
using System.Threading.Tasks;
using Mcma.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.GoogleCloud.Sample.Scripts.Common
{
    public static class ScriptHelper
    {
        public static Task ExecuteTaskAsync<T>(string[] args, Action<IServiceCollection> configure = null) where T : class, IScript
        {
            var services = new ServiceCollection();
            services.AddSingleton<ExecutionIdProvider>();
            services.AddMcmaClient(builder => builder.ConfigureForScripts());
            configure?.Invoke(services);
            services.AddSingleton<IScript, T>();

            var serviceProvider = services.BuildServiceProvider();
            var script = serviceProvider.GetRequiredService<IScript>();
            return script.ExecuteAsync(args);
        }
    }
}