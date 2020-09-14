using System;
using System.Threading.Tasks;
using Mcma.Context;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Mcma.Azure.JobProcessor.Common
{
    internal static class FunctionAppSettingsHelper
    {
        private static EnvironmentVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static Lazy<AzureCredentials> Credentials { get; } = new Lazy<AzureCredentials>(() => EnvironmentVariableProvider.AzureCredentials());

        private static Lazy<string> FunctionName { get; } = new Lazy<string>(() => EnvironmentVariableProvider.FunctionName());

        private static Lazy<Task<IAzure>> AzureTask { get; } =
            new Lazy<Task<IAzure>>(
                () => Microsoft.Azure.Management.Fluent.Azure.Configure().Authenticate(Credentials.Value).WithDefaultSubscriptionAsync());

        private static Lazy<Task<IFunctionApp>> FunctionAppTask { get; } =
            new Lazy<Task<IFunctionApp>>(
                () => AzureTask.Value.ContinueWith(t => t.Result.AppServices.FunctionApps.GetByIdAsync(FunctionName.Value)).Unwrap());
        
        public static async Task SetAppSettingAsync(string key, string value)
        {
            var functionApp = await FunctionAppTask.Value;

            await functionApp.Update().WithAppSetting(key, value).ApplyAsync();
        }
    }
}