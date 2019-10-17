using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Logging;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Azure.AzureAiService.Worker
{
    internal class ProcessNotification : WorkerOperationHandler<ProcessNotificationRequest>
    {
        public ProcessNotification(IDbTableProvider dbTableProvider, IResourceManagerProvider resourceManagerProvider)
        {
            DbTableProvider = dbTableProvider;
            ResourceManagerProvider = resourceManagerProvider;
        }

        private IDbTableProvider DbTableProvider { get; }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessNotificationRequest requestInput)
        {
            var azureVideoId = requestInput.Notification?.Id;
            var azureState = requestInput.Notification?.State;
            if (azureVideoId == null || azureState == null)
            {
                request.Logger.Warn("POST is not coming from Azure Video Indexer. Expected notification to have id and state properties.");
                return;
            }

            var jobHelper =
                new WorkerJobHelper<AIJob>(
                    DbTableProvider.Table<JobAssignment>(request.Variables.TableName()),
                    ResourceManagerProvider.Get(request.Variables),
                    request,
                    requestInput.JobAssignmentId);

            try
            {
                await jobHelper.InitializeAsync();

                var authTokenUrl = request.Variables.VideoIndexerApiUrl() + "/auth/" + request.Variables.VideoIndexerLocation() + "/Accounts/" + request.Variables.VideoIndexerAccountID() + "/AccessToken?allowEdit=true";
                var customHeaders = new Dictionary<string, string> { ["Ocp-Apim-Subscription-Key"] = request.Variables.VideoIndexerSubscriptionKey() };

                request.Logger.Debug($"Generate Azure Video Indexer Token: Doing a GET on {authTokenUrl}");
                var mcmaHttp = new McmaHttpClient();
                var response = await mcmaHttp.GetAsync(authTokenUrl, headers: customHeaders).WithErrorHandling();

                var apiToken = await response.Content.ReadAsJsonAsync();
                request.Logger.Debug($"Azure API Token: {apiToken}");
                
                var metadataFromAzureVideoIndexer =
                    $"{request.Variables.VideoIndexerApiUrl()}/{request.Variables.VideoIndexerLocation()}/Accounts/{request.Variables.VideoIndexerAccountID()}/Videos/{azureVideoId}/Index?accessToken={apiToken}&language=English";

                request.Logger.Debug($"Getting Azure video metadata from: {metadataFromAzureVideoIndexer}");
                var indexedVideoMetadataResponse = await mcmaHttp.GetAsync(metadataFromAzureVideoIndexer, headers: customHeaders).WithErrorHandling();

                var videoMetadata = await indexedVideoMetadataResponse.Content.ReadAsJsonAsync();
                request.Logger.Debug($"Azure AI video metadata: {videoMetadata}");
                
                BlobStorageFolderLocator outputLocation;
                if (!jobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                jobHelper.JobOutput["outputFile"] = 
                    await outputLocation.Proxy(jobHelper.Variables).PutAsTextAsync(azureVideoId + "-" + Guid.NewGuid() + ".json", videoMetadata.ToString());

                await jobHelper.CompleteAsync();
            }
            catch (Exception ex)
            {
                request.Logger.Exception(ex);
                try
                {
                    await jobHelper.FailAsync(ex.ToString());
                }
                catch (Exception innerEx)
                {
                    request.Logger.Exception(innerEx);
                }
            }
        }
    }
}
