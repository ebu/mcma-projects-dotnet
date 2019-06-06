using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Aws.S3;
using Mcma.Aws.DynamoDb;
using Mcma.Worker;
using Mcma.Aws.Worker;
using Mcma.Data;
using Mcma.Core.ContextVariables;

namespace Mcma.Aws.AzureAiService.Worker
{
    internal class ProcessNotificationHandler : WorkerOperationHandler<ProcessNotificationRequest>
    {
        public const string OperationName = "ProcessNotification";

        public ProcessNotificationHandler(IDbTableProvider<JobAssignment> dbTableProvider, IWorkerResourceManagerProvider resourceManagerProvider)
        {
            DbTableProvider = dbTableProvider;
            ResourceManagerProvider = resourceManagerProvider;
        }

        private IDbTableProvider<JobAssignment> DbTableProvider { get; }

        private IWorkerResourceManagerProvider ResourceManagerProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessNotificationRequest requestInput)
        {
            var azureVideoId = requestInput.Notification?.Id;
            var azureState = requestInput.Notification?.State;
            if (azureVideoId == null || azureState == null)
            {
                Logger.Warn("POST is not coming from Azure Video Indexer. Expected notification to have id and state properties.");
                return;
            }

            var workerJobHelper =
                new WorkerJobHelper<AIJob>(
                    DbTableProvider.Table(request.TableName()),
                    ResourceManagerProvider.GetResourceManager(request),
                    request,
                    requestInput.JobAssignmentId);

            try
            {
                await workerJobHelper.InitializeAsync();

                var authTokenUrl = request.ApiUrl() + "/auth/" + request.Location() + "/Accounts/" + request.AccountID() + "/AccessToken?allowEdit=true";
                var customHeaders = new Dictionary<string, string> { ["Ocp-Apim-Subscription-Key"] = request.SubscriptionKey() };

                Logger.Debug($"Generate Azure Video Indexer Token: Doing a GET on {authTokenUrl}");
                var mcmaHttp = new McmaHttpClient();
                var response = await mcmaHttp.GetAsync(authTokenUrl, headers: customHeaders).WithErrorHandling();

                var apiToken = await response.Content.ReadAsJsonAsync();
                Logger.Debug($"Azure API Token: {apiToken}");
                
                var metadataFromAzureVideoIndexer =
                    $"{request.ApiUrl()}/{request.Location()}/Accounts/{request.AccountID()}/Videos/{azureVideoId}/Index?accessToken={apiToken}&language=English";

                Logger.Debug($"Getting Azure video metadata from: {metadataFromAzureVideoIndexer}");
                var indexedVideoMetadataResponse = await mcmaHttp.GetAsync(metadataFromAzureVideoIndexer, headers: customHeaders).WithErrorHandling();

                var videoMetadata = await indexedVideoMetadataResponse.Content.ReadAsJsonAsync();
                Logger.Debug($"Azure AI video metadata: {videoMetadata}");
                
                S3Locator outputLocation;
                if (!workerJobHelper.JobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                var jobOutputBucket = outputLocation.AwsS3Bucket;
                var jobOutputKeyPrefix = outputLocation.AwsS3KeyPrefix != null ? outputLocation.AwsS3KeyPrefix : "";

                // get the info about the destination bucket to store the result of the job
                var s3Params = new PutObjectRequest
                {
                    BucketName = jobOutputBucket,
                    Key = jobOutputKeyPrefix + azureVideoId + "-" + Guid.NewGuid() + ".json",
                    ContentBody = videoMetadata.ToString(),
                    ContentType = "application/json"
                };

                var destS3 = await outputLocation.GetClientAsync();
                await destS3.PutObjectAsync(s3Params);

                //updating JobAssignment with jobOutput
                workerJobHelper.JobOutput["outputFile"] = new S3Locator
                {
                    AwsS3Bucket = s3Params.BucketName,
                    AwsS3Key = s3Params.Key
                };

                await workerJobHelper.CompleteAsync();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                try
                {
                    await workerJobHelper.FailAsync(ex.ToString());
                }
                catch (Exception innerEx)
                {
                    Logger.Exception(innerEx);
                }
            }
        }
    }
}
