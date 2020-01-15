using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Aws.S3;
using Mcma.Core.Context;
using Mcma.Core.Logging;
using Mcma.Core.Utility;
using Mcma.Data;
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.AwsAiService.ApiHandler.Sns
{
    public static class SnsNotificationHandler
    {
        public static Func<McmaApiRequestContext, Task> Create(
            IDbTableProvider dbTableProvider,
            ILoggerProvider loggerProvider,
            Func<IContextVariableProvider, IWorkerInvoker> createWorkerInvoker)
        {
            var httpClient = new HttpClient();

            return async requestContext =>
            {
                var logger = loggerProvider.Get(requestContext.GetTracker());

                var messageTypeHeader = requestContext.GetRequestHeader(SnsConstants.MessageTypeHeader);
                if (string.IsNullOrWhiteSpace(messageTypeHeader))
                {
                    requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, "Request is not an SNS message.");
                    return;
                }

                switch (messageTypeHeader)
                {
                    case SnsConstants.MessageTypes.SubscriptionConfirmation:
                        await HandleSnsSubscriptionConfirmationAsync(requestContext, httpClient);
                        break;
                    case SnsConstants.MessageTypes.Notification:
                        await HandleSnsNotificationAsync(logger, requestContext, createWorkerInvoker(requestContext));
                        break;
                    default:
                        requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, "Request does not have a valid SNS message type.");
                        return;
                }
            };
        }

        private static async Task HandleSnsSubscriptionConfirmationAsync(McmaApiRequestContext requestContext, HttpClient httpClient)
        {
            var subscriptionConfirmationMessage = requestContext.Request.JsonBody.ToObject<SubscriptionConfirmationMessage>();

            if (string.IsNullOrWhiteSpace(subscriptionConfirmationMessage?.SubscribeURL))
            {
                requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, "Subscription confirmation message does not specify a subscriber url.");
                return;
            }

            var resp = await httpClient.GetAsync(subscriptionConfirmationMessage.SubscribeURL);
            if (!resp.IsSuccessStatusCode)
            {
                requestContext.SetResponseStatusCode(
                    HttpStatusCode.BadRequest,
                    $"GET request to subscriber url {subscriptionConfirmationMessage.SubscribeURL} returned a status code of {resp.StatusCode}.");
                return;
            }
        }
        
        private static async Task HandleSnsNotificationAsync(ILogger logger, McmaApiRequestContext requestContext, IWorkerInvoker workerInvoker)
        {
            logger.Debug($"Received SNS notification:{Environment.NewLine}{requestContext.Request.Body}");

            var notificationMessage = requestContext.Request.JsonBody.ToObject<NotificationMessage>();

            var notification = JObject.Parse(notificationMessage.Message);

            if (notification.Property("JobId") != null)
                await HandleRekognitionJobResultAsync(requestContext, workerInvoker, notification.ToObject<RekognitionNotification>());
            else if (notification.Property("Records") != null)
                await HandleS3NotificationAsync(requestContext, workerInvoker, notification.ToObject<S3Notification>());
        }

        private static async Task HandleS3NotificationAsync(
            McmaApiRequestContext requestContext,
            IWorkerInvoker workerInvoker,
            S3Notification s3Notification)
        {
            foreach (var s3 in s3Notification.Records.Select(r => r.S3).Where(x => x.Object?.Key != null && !x.Object.Key.StartsWith(".")))
            {
                var bucketName = s3.Bucket.Name;
                var objectKey = s3.Object.Key;

                if (!Regex.IsMatch(s3.Object.Key, "^TranscriptionJob-[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\\.json$"))
                    throw new Exception("S3 key '" + objectKey + "' is not an expected file name for transcribe output");

                var transcribeJobUuid = objectKey.Substring(objectKey.IndexOf("-") + 1, objectKey.LastIndexOf(".") - objectKey.IndexOf("-") - 1);

                var jobAssignmentId = requestContext.PublicUrl().TrimEnd('/') + "/job-assignments/" + transcribeJobUuid;

                await workerInvoker.InvokeAsync(
                    requestContext.WorkerFunctionId(),
                    "ProcessTranscribeJobResult",
                    input: new
                    {
                        jobAssignmentId,
                        outputFile = new AwsS3FileLocator
                        {
                            AwsS3Bucket = bucketName,
                            AwsS3Key = objectKey
                        }
                    });
            }
        }

        private static async Task HandleRekognitionJobResultAsync(
            McmaApiRequestContext requestContext,
            IWorkerInvoker workerInvoker,
            RekognitionNotification rekoNotification)
        {
            if (rekoNotification.JobTag == null)
                throw new Exception($"The jobAssignment couldn't be found in the SNS message");
            
            var jobAssignmentId = rekoNotification.JobTag.HexDecodeString();

            await workerInvoker.InvokeAsync(
                requestContext.WorkerFunctionId(),
                "ProcessRekognitionResult",
                input: new
                {
                    jobAssignmentId,
                    jobInfo = new
                    {
                        rekoJobId = rekoNotification.JobId,
                        rekoJobType = rekoNotification.API,
                        status = rekoNotification.Status
                    }
                });
        }
    }
}