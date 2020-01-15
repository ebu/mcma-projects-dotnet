using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Azure.AzureAiService.Worker
{
    internal class ProcessNotification : WorkerOperation<ProcessNotificationRequest>
    {
        public ProcessNotification(ProviderCollection providerCollection)
            : base(providerCollection)
        {
        }

        public override string Name => nameof(ProcessNotification);

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessNotificationRequest requestInput)
        {
            var logger = ProviderCollection.LoggerProvider.Get(request.Tracker);

            var azureVideoId = requestInput.Notification?.Id;
            var azureState = requestInput.Notification?.State;
            if (azureVideoId == null || azureState == null)
            {
                logger.Warn("POST is not coming from Azure Video Indexer. Expected notification to have id and state properties.");
                return;
            }

            var jobHelper =
                new ProcessJobAssignmentHelper<AIJob>(
                    ProviderCollection.DbTableProvider.Table<JobAssignment>(request.TableName()),
                    ProviderCollection.ResourceManagerProvider.Get(request),
                    logger,
                    request,
                    requestInput.JobAssignmentId);

            try
            {
                await jobHelper.InitializeAsync();

                var authTokenUrl = request.VideoIndexerApiUrl() + "/auth/" + request.VideoIndexerLocation() + "/Accounts/" + request.VideoIndexerAccountID() + "/AccessToken?allowEdit=true";
                var customHeaders = new Dictionary<string, string> { ["Ocp-Apim-Subscription-Key"] = request.VideoIndexerSubscriptionKey() };

                logger.Debug($"Generate Azure Video Indexer Token: Doing a GET on {authTokenUrl}");
                var mcmaHttp = new McmaHttpClient();
                var response = await mcmaHttp.GetAsync(authTokenUrl, headers: customHeaders).WithErrorHandling();

                var apiToken = await response.Content.ReadAsJsonAsync();
                logger.Debug($"Azure API Token: {apiToken}");
                
                var metadataFromAzureVideoIndexer =
                    $"{request.VideoIndexerApiUrl()}/{request.VideoIndexerLocation()}/Accounts/{request.VideoIndexerAccountID()}/Videos/{azureVideoId}/Index?accessToken={apiToken}&language=English";

                logger.Debug($"Getting Azure video metadata from: {metadataFromAzureVideoIndexer}");
                var indexedVideoMetadataResponse = await mcmaHttp.GetAsync(metadataFromAzureVideoIndexer, headers: customHeaders).WithErrorHandling();

                var videoMetadata = await indexedVideoMetadataResponse.Content.ReadAsJsonAsync();
                logger.Debug($"Azure AI video metadata: {videoMetadata}");
                
                BlobStorageFolderLocator outputLocation;
                if (!jobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                jobHelper.JobOutput["outputFile"] = 
                    await outputLocation.Proxy(request).PutAsTextAsync(azureVideoId + "-" + Guid.NewGuid() + ".json", videoMetadata.ToString());

                await jobHelper.CompleteAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                try
                {
                    await jobHelper.FailAsync(ex.ToString());
                }
                catch (Exception innerEx)
                {
                    logger.Error(innerEx);
                }
            }
        }
    }
}
