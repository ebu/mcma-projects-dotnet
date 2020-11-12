using Mcma.GoogleCloud.Firestore;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.GoogleCloud.JobProcessor.Common
{
    public static class DataControllerServiceCollectionExtensions
    {
        public static IServiceCollection AddDataController(this IServiceCollection services, bool? consistentRead = null)
            =>
                services
                    .AddMcmaFirestore()
                    .AddSingleton<IDataController, DataController>();
    }
}