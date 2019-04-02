using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.DecideTranscodeReqs
{
    public class Function
    {
        // Local Define
        private const string VIDEO_FORMAT = "AVC";
        private const string VIDEO_CODEC = "mp42";
        private const string VIDEO_CODEC_ISOM = "isom";
        private const int VIDEO_BITRATE_MB = 2;

        private static readonly int THRESHOLD_SECONDS = int.Parse(Environment.GetEnvironmentVariable("THESHOLD_SECONDS"));

        private double CalcSeconds(int hour, int minute, double seconds)
            => (hour * 60 * 60) + (minute * 60) + seconds;
 
        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());

            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 45
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var bme = await resourceManager.ResolveAsync<BMEssence>(@event["data"]["bmEssence"].ToString());

            var technicalMetadata = bme.Get<object>("technicalMetadata").ToMcmaJson();

            var ebuCoreMain = technicalMetadata["ebucore:ebuCoreMain"];
            var coreMetadata = ebuCoreMain["ebucore:coreMetadata"]?.FirstOrDefault();
            var containerFormat = coreMetadata["ebucore:format"]?.FirstOrDefault()?["ebucore:containerFormat"]?.FirstOrDefault();
            var duration = coreMetadata["ebucore:format"]?.FirstOrDefault()?["ebucore:duration"]?.FirstOrDefault();

            var video = new
            {
                Codec = containerFormat["ebucore:codec"]?.FirstOrDefault()?["ebucore:codecIdentifier"]?.FirstOrDefault()?["dc:identifier"]?.FirstOrDefault()?["#value"],
                BitRate = coreMetadata["ebucore:format"]?.FirstOrDefault()?["ebucore:videoFormat"]?.FirstOrDefault()?["ebucore:bitRate"]?.FirstOrDefault()?["#value"],
                Format = coreMetadata["ebucore:format"]?.FirstOrDefault()?["ebucore:videoFormat"]?.FirstOrDefault()?["@videoFormatName"],
                NormalPlayTime = duration["ebucore:normalPlayTime"]?.FirstOrDefault()?["#value"]
            };

            var codec = video.Codec?.ToString();
            var format = video.Format?.ToString();
            var parsedBitRate = double.TryParse(video.BitRate.ToString(), out var bitRate);
            var mbyte = parsedBitRate ? (bitRate / 8) / (1024 * 1024) : default(double?);

            if ((codec == VIDEO_CODEC || codec == VIDEO_CODEC_ISOM) && format == VIDEO_FORMAT && mbyte.HasValue && mbyte.Value <= VIDEO_BITRATE_MB)
                return "none";

            var normalPlayTime = video.NormalPlayTime?.ToString() ?? string.Empty;

            double totalSeconds;

            var ptSeconds = Regex.Match(normalPlayTime, "PT([0-9\\.]+)S");
            if (ptSeconds.Success)
            {
                totalSeconds = double.Parse(ptSeconds.Groups[1].Captures[0].Value);
            }
            else
            {
                var hour = Regex.Match(normalPlayTime, "(\\d*)H");
                var min = Regex.Match(normalPlayTime, "(\\d*)M");
                var sec = Regex.Match(normalPlayTime, "(\\d*)S");

                if (!sec.Success)
                    throw new Exception($"Invalid play time in technical metadata: {normalPlayTime ?? "[null]"}");
                
                totalSeconds =
                    CalcSeconds(
                        hour.Success ? int.Parse(hour.Groups[1].Captures[0].Value) : 0,
                        min.Success ? int.Parse(min.Groups[1].Captures[0].Value) : 0,
                        double.Parse(sec.Groups[1].Captures[0].Value));
            }

            Logger.Debug("[Total Seconds]: " + totalSeconds);

            return totalSeconds <= THRESHOLD_SECONDS ? "short" : "long";
        }
    }
}