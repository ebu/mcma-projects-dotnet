using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Lambda.Serialization;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json.Linq;
using Mcma.Aws;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Core.Utility;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AwsAiService.SnsTrigger
{

    public class Function
    {
        private StageVariables StageVariables { get; } = new StageVariables();

        public async Task Handler(SNSEvent @event, ILambdaContext context)
        {
            if (@event == null || @event.Records == null)
                return;

            foreach (var record in @event.Records)
            {
                try
                {
                    if (record.Sns == null)
                        throw new Exception("The payload doesn't contain expected data: Sns");

                    if (record.Sns.Message == null)
                        throw new Exception("The payload doesn't contain expectd data: Sns.Message");

                    var message = JToken.Parse(record.Sns.Message);
                    Logger.Debug($"SNS Message ==> {message}");

                    var rekoJobId = message["JobId"]?.Value<string>();
                    var rekoJobType = message["API"]?.Value<string>();
                    var status = message["Status"]?.Value<string>();

                    var jt = message["JobTag"]?.Value<string>();
                    if (jt == null)
                        throw new Exception($"The jobAssignment couldn't be found in the SNS message");

                    var jobAssignmentId = jt.HexDecodeString();

                    Logger.Debug($"rekoJobId: {rekoJobId}");
                    Logger.Debug($"rekoJobType: {rekoJobType}");
                    Logger.Debug($"status: {status}");
                    Logger.Debug($"jobAssignmentId: {jobAssignmentId}");

                    var invokeParams = new InvokeRequest
                    {
                        FunctionName = StageVariables.WorkerLambdaFunctionName,
                        InvocationType = "Event",
                        LogType = "None",
                        Payload = JObject.FromObject(new
                        {
                            action = "ProcessRekognitionResult",
                            stageVariables = StageVariables.ToDictionary(),
                            jobAssignmentId,
                            jobExternalInfo = new
                            {
                                rekoJobId,
                                rekoJobType,
                                status
                            }
                        }).ToString()
                    };

                    var lambda = new AmazonLambdaClient();
                    await lambda.InvokeAsync(invokeParams);
                }
                catch (Exception error)
                {
                    Logger.Error($"Failed processing record.\r\nRecord:\r\n{record.ToMcmaJson()}\r\nError:\r\n{error}");
                }
            }
        }
    }
}