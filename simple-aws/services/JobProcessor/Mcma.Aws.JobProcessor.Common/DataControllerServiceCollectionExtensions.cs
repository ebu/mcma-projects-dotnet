using Mcma.Aws.DynamoDb;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.Aws.JobProcessor.Common
{
    public static class DataControllerServiceCollectionExtensions
    {
        public static IServiceCollection AddDataController(this IServiceCollection services, bool? consistentRead = null)
            =>
                services
                    .AddMcmaDynamoDb(
                        opts =>
                        {
                            opts.ConsistentGet = consistentRead;
                            opts.ConsistentQuery = consistentRead;
                        },
                        builder =>
                            builder
                                .AddCustomQueryBuilder<JobResourceQueryParameters, JobResourceQueryBuilder>()
                                .AddAttributeMapping<JobBase>(
                                    "resource_status",
                                    (partitionKey, sortKey, resource) => $"{partitionKey}-{resource.Status}")
                                .AddAttributeMapping<JobBase>(
                                    "resource_created",
                                    (partitionKey, sortKey, resource) => resource.DateCreated?.ToUnixTimeMilliseconds()))
                    .AddSingleton<IDataController, DataController>();
    }
}