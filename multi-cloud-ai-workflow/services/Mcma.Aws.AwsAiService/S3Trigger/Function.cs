using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Lambda.Serialization;
using Amazon.Lambda.S3Events;
using Newtonsoft.Json.Linq;
using Mcma.Aws;
using Mcma.Core.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Mcma.Core.Logging;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.ContextVariables;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AwsAiService.S3Trigger
{

    public class Function
    {
        private IContextVariableProvider ContextVariableProvider { get; } = new EnvironmentVariableProvider();

        public async Task Handler(S3Event @event, ILambdaContext context)
        {
            if (@event == null || @event.Records == null)
                return;

            foreach (var record in @event.Records)
            {
                try
                {
                    var awsS3Bucket = record.S3.Bucket.Name;
                    var awsS3Key = record.S3.Object.Key;

                    if (!Regex.IsMatch(awsS3Key, "^TranscriptionJob-[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\\.json$"))
                        throw new Exception("S3 key '" + awsS3Key + "' is not an expected file name for transcribe output");

                    var transcribeJobUuid = awsS3Key.Substring(awsS3Key.IndexOf("-") + 1, awsS3Key.LastIndexOf(".") - awsS3Key.IndexOf("-") - 1);

                    var jobAssignmentId = ContextVariableProvider.GetRequiredContextVariable("PublicUrl") + "/job-assignments/" + transcribeJobUuid;

                    var invokeParams = new InvokeRequest
                    {
                        FunctionName = ContextVariableProvider.GetRequiredContextVariable("WorkerFunctionName"),
                        InvocationType = "Event",
                        LogType = "None",
                        Payload = new
                        {
                            operationName = "ProcessTranscribeJobResult",
                            contextVariables = ContextVariableProvider.GetAllContextVariables(),
                            input = new
                            {
                                jobAssignmentId,
                                outputFile = new S3Locator { AwsS3Bucket = awsS3Bucket, AwsS3Key = awsS3Key }
                            }
                        }.ToMcmaJson().ToString()
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