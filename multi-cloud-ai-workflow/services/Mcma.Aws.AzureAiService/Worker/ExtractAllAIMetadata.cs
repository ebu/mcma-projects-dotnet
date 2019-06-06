using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Aws.S3;
using Mcma.Worker;

namespace Mcma.Aws.AzureAiService.Worker
{
    internal class ExtractAllAIMetadata : IJobProfileHandler<AIJob>
    {
        public const string Name = "Azure" + nameof(ExtractAllAIMetadata);

        public async Task ExecuteAsync(WorkerJobHelper<AIJob> jobHelper)
        {
            S3Locator inputFile;
            if (!jobHelper.JobInput.TryGet<S3Locator>(nameof(inputFile), out inputFile))
                throw new Exception($"Invalid or missing input file");

            string mediaFileUrl;
            if (!string.IsNullOrWhiteSpace(inputFile.HttpEndpoint))
            {
                mediaFileUrl = inputFile.HttpEndpoint;
            }
            else
            {
                var bucketLocation = await inputFile.GetBucketLocationAsync();
                var s3SubDomain = !string.IsNullOrWhiteSpace(bucketLocation) ? $"s3-{bucketLocation}" : "s3";
                mediaFileUrl = $"https://{s3SubDomain}.amazonaws.com/{inputFile.AwsS3Bucket}/{inputFile.AwsS3Key}";
            }

            var authTokenUrl = jobHelper.Request.ApiUrl() + "/auth/" + jobHelper.Request.Location() + "/Accounts/" + jobHelper.Request.AccountID() + "/AccessToken?allowEdit=true";
            var customHeaders = new Dictionary<string, string> { ["Ocp-Apim-Subscription-Key"] = jobHelper.Request.SubscriptionKey() };

            Logger.Debug($"Generate Azure Video Indexer Token: Doing a GET on {authTokenUrl}");
            var mcmaHttp = new McmaHttpClient();
            var response = await mcmaHttp.GetAsync(authTokenUrl, headers: customHeaders).WithErrorHandling();

            var apiToken = await response.Content.ReadAsJsonAsync();
            Logger.Debug($"Azure API Token: {apiToken}");

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

            var secureHost = new Uri(jobHelper.JobAssignmentId, UriKind.Absolute).Host;
            var nonSecureHost = new Uri(jobHelper.Request.GetRequiredContextVariable("PublicUrlNonSecure"), UriKind.Absolute).Host;

            var callbackUrl = Uri.EscapeDataString(jobHelper.JobAssignmentId.Replace(secureHost, nonSecureHost) + "/notifications");

            var postVideoUrl = jobHelper.Request.ApiUrl() + "/" + jobHelper.Request.Location() + "/Accounts/" + jobHelper.Request.AccountID() + "/Videos?accessToken=" + apiToken + "&name=" + inputFile.AwsS3Key + "&callbackUrl=" + callbackUrl + "&videoUrl=" + mediaFileUrl + "&fileName=" + inputFile.AwsS3Key;
            
            Logger.Debug($"Call Azure Video Indexer API: Doing a POST on {postVideoUrl}");
            var postVideoResponse = await mcmaHttp.PostAsync(postVideoUrl, null, customHeaders).WithErrorHandling();

            var azureAssetInfo = await postVideoResponse.Content.ReadAsJsonAsync();
            Logger.Debug("azureAssetInfo: ", azureAssetInfo);

            try
            {
                jobHelper.JobOutput["jobInfo"] = azureAssetInfo;

                await jobHelper.UpdateJobAssignmentOutputAsync();
            }
            catch (Exception error)
            {
                Logger.Error("Error updating the job", error);
            }
        }
    }
}
