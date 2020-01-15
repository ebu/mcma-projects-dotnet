using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Client;
using Mcma.Core;
using Mcma.Worker;

namespace Mcma.Azure.AzureAiService.Worker
{
    internal class ExtractAllAIMetadata : IJobProfile<AIJob>
    {
        public string Name => "Azure" + nameof(ExtractAllAIMetadata);

        public async Task ExecuteAsync(ProcessJobAssignmentHelper<AIJob> jobHelper)
        {
            BlobStorageFileLocator inputFile;
            if (!jobHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception($"Invalid or missing input file");

            var mediaFileUrl = inputFile.Proxy(jobHelper.Request).GetPublicReadOnlyUrl();

            var authTokenUrl = jobHelper.Request.VideoIndexerApiUrl() + "/auth/" + jobHelper.Request.VideoIndexerLocation() + "/Accounts/" + jobHelper.Request.VideoIndexerAccountID() + "/AccessToken?allowEdit=true";
            var customHeaders = new Dictionary<string, string> { ["Ocp-Apim-Subscription-Key"] = jobHelper.Request.VideoIndexerSubscriptionKey() };

            jobHelper.Logger.Debug($"Generate Azure Video Indexer Token: Doing a GET on {authTokenUrl}");
            var mcmaHttp = new McmaHttpClient();
            var response = await mcmaHttp.GetAsync(authTokenUrl, headers: customHeaders).WithErrorHandling();

            var apiToken = await response.Content.ReadAsJsonAsync();
            jobHelper.Logger.Debug($"Azure API Token: {apiToken}");

            // call the Azure API to process the video 
            // in this scenario the video is located in a public link
            // so no need to upload the file to Azure
            /* Sample URL Structure      
                https://api.videoindexer.ai/{location}/Accounts/{accountId}/Videos?
                    accessToken={accessToken}&
                    name={name}?description={string}&
                    partition={string}&
                    externalId={string}&
                    callbackUrl={string}&
                    metadata={string}&
                    language={string}&
                    videoUrl={string}&
                    fileName={string}&
                    indexingPreset={string}&
                    streamingPreset=Default&
                    linguisticModelId={string}&
                    privacy={string}&
                    externalUrl={string}" */

            var trimmedJobAssignmentId = jobHelper.JobAssignmentId.TrimEnd('/');
            var jobAssignmentGuid = trimmedJobAssignmentId.Substring(trimmedJobAssignmentId.LastIndexOf('/') + 1);

            var callbackUrl =
                Uri.EscapeDataString(
                    jobHelper.Request.NotificationsUrl().TrimEnd('/') + "/job-assignments/" + jobAssignmentGuid + "/notifications?code=" + jobHelper.Request.NotificationHandlerKey());
            jobHelper.Logger.Debug("Callback url: " + callbackUrl);

            var videoUrl = Uri.EscapeDataString(mediaFileUrl);

            var postVideoUrl =
                jobHelper.Request.VideoIndexerApiUrl() + "/" + jobHelper.Request.VideoIndexerLocation() + "/Accounts/" + jobHelper.Request.VideoIndexerAccountID() + "/Videos" +
                    "?accessToken=" + apiToken +
                    "&name=" + inputFile.FilePath +
                    "&callbackUrl=" + callbackUrl +
                    "&videoUrl=" + videoUrl +
                    "&fileName=" + inputFile.FilePath;
            
            jobHelper.Logger.Debug($"Call Azure Video Indexer API: Doing a POST on {postVideoUrl}");
            var postVideoResponse = await mcmaHttp.PostAsync(postVideoUrl, null, customHeaders).WithErrorHandling();

            var azureAssetInfo = await postVideoResponse.Content.ReadAsJsonAsync();
            jobHelper.Logger.Debug("azureAssetInfo: ", azureAssetInfo);

            try
            {
                jobHelper.JobOutput["jobInfo"] = azureAssetInfo;

                await jobHelper.UpdateJobAssignmentOutputAsync();
            }
            catch (Exception error)
            {
                jobHelper.Logger.Error("Error updating the job", error);
            }
        }
    }
}
