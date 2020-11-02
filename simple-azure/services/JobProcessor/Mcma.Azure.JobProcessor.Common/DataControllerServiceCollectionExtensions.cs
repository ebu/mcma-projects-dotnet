using Mcma.Azure.CosmosDb;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.Azure.JobProcessor.Common
{
    public static class DataControllerServiceCollectionExtensions
    {
        public static IServiceCollection AddDataController(this IServiceCollection services, bool? consistentRead = null)
            =>
                services
                    .AddMcmaCosmosDb(
                        opts =>
                        {
                            opts.ConsistentGet = consistentRead;
                            opts.ConsistentQuery = consistentRead;
                        })
                    .AddSingleton<IDataController, DataController>();
    }
}