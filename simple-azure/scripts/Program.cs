using System;
using System.Threading.Tasks;
using Mcma.Azure.Sample.Scripts.Common;
using Mcma.Azure.Sample.Scripts.PostDeploy;
using Mcma.Azure.Sample.Scripts.RunJobs;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.Azure.Sample.Scripts
{
    public static class Program
    {
        public static async Task<int> Main(params string[] args)
        {
            if (args.Length == 0)
            {
                await Console.Error.WriteLineAsync("Please specify a script to run.");
                return -1;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "post-deploy":
                    await ScriptHelper.ExecuteTaskAsync<UpdateServiceRegistryScript>(args, services => services.AddSingleton<JsonData>());
                    return 0;
                case "run-jobs":
                    await ScriptHelper.ExecuteTaskAsync<RunJobsScript>(args, services => services.AddRunJobsDependencies());
                    return 0;
                default:
                    await Console.Error.WriteLineAsync($"Unknown script '{args[0]}' specified");
                    return -1;
            }
        }
    }
}