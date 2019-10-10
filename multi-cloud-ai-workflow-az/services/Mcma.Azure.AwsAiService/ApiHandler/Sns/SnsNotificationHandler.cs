using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Aws.S3;
using Mcma.Client;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Core.Utility;
using Mcma.Data;
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.AwsAiService.ApiHandler.Sns
{
    public static class SnsNotificationHandler
    {

        public static Func<McmaApiRequestContext, Task> Create(
            IResourceManagerProvider resourceManagerProvider,
            IDbTableProvider dbTableProvider,
            IWorkerInvoker workerInvoker)
        {
            var httpClient = new HttpClient();

            return async requestContext =>
            {
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
                        await HandleSnsNotificationAsync(requestContext, workerInvoker);
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
        
        private static async Task HandleSnsNotificationAsync(McmaApiRequestContext requestContext, IWorkerInvoker workerInvoker)
        {
            var notificationMessage = requestContext.Request.JsonBody.ToObject<NotificationMessage>();

            var eventJson = JToken.Parse(notificationMessage.Message);
            var @event = eventJson.ToMcmaObject<AwsEvent>();

            string operationName = null;
            object input = null;
            foreach (var record in @event.Records)
            {
                if (record.S3 != null)
                    (operationName, input) = GetTranscribeJobResultWorkerParams(requestContext, record.S3.Bucket.Name, record.S3.Object.Key);
                else if (record.Sns != null)
                    (operationName, input) = GetRekognitionJobResultWorkerParams(requestContext, record.Sns.Message);
            }

            if (operationName == null)
            {
                Logger.Warn(
                    "Received notification with unrecognized message content. No SNS or S3 records found." + Environment.NewLine +
                    "JSON:" + Environment.NewLine +
                    eventJson);
                return;
            }

            await workerInvoker.InvokeAsync(requestContext.WorkerFunctionId(), operationName, input: input);
        }

        private static (string, object) GetTranscribeJobResultWorkerParams(McmaApiRequestContext requestContext, string bucketName, string objectKey)
        {
            if (!Regex.IsMatch(objectKey, "^TranscriptionJob-[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\\.json$"))
                throw new Exception("S3 key '" + objectKey + "' is not an expected file name for transcribe output");

            var transcribeJobUuid = objectKey.Substring(objectKey.IndexOf("-") + 1, objectKey.LastIndexOf(".") - objectKey.IndexOf("-") - 1);

            var jobAssignmentId = requestContext.PublicUrl() + "/job-assignments/" + transcribeJobUuid;
            
            return (
                "ProcessTranscribeJobResult",
                new
                {
                    jobAssignmentId,
                    outputFile = new S3FileLocator
                    {
                        Bucket = bucketName,
                        Key = objectKey
                    }
                }
            );
        }

        private static (string, object) GetRekognitionJobResultWorkerParams(McmaApiRequestContext requestContext, string messageBody)
        {
            var message = JToken.Parse(messageBody);
            var rekoJobId = message["JobId"]?.Value<string>();
            var rekoJobType = message["API"]?.Value<string>();
            var status = message["Status"]?.Value<string>();

            var jt = message["JobTag"]?.Value<string>();
            if (jt == null)
                throw new Exception($"The jobAssignment couldn't be found in the SNS message");
            
            var jobAssignmentId = jt.HexDecodeString();

            return (
                "ProcessRekognitionResult",
                new
                {
                    jobAssignmentId,
                    jobInfo = new
                    {
                        rekoJobId,
                        rekoJobType,
                        status
                    }
                }
            );
        }
    }
}