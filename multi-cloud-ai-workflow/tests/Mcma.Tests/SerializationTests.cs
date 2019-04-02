using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Aws;
using System.Text.RegularExpressions;

namespace Mcma.Tests
{
    public static class SerializationTests
    {
        public static void RunAll()
        {
            foreach (var method in typeof(SerializationTests).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(m => m.Name != nameof(RunAll)))
                method.Invoke(null, new object[0]);
        }

        public static void ToMcmaObject_ShouldDeserializeWorkflowJob()
        {
            var workflowJobJson =
            @"{
                ""@type"": ""WorkflowJob"",
                ""jobProfile"": ""https://w7eijekmuf.execute-api.us-east-2.amazonaws.com/dev/job-profiles/89f432b2-8e90-4d64-9d5d-9c082d6c3574"",
                ""jobInput"": {
                    ""@type"": ""JobParameterBag"",
                    ""metadata"": {
                        ""name"": ""test 1"",
                        ""description"": ""test 1""
                    },
                    ""inputFile"": {
                        ""@type"": ""S3Locator"",
                        ""awsS3Bucket"": ""triskel.mcma.us-east-2.dev.upload"",
                        ""awsS3Key"": ""ShowbizPKG091218__091118178.mp4""
                    }
                }
            }";

            var workflowJob = JObject.Parse(workflowJobJson).ToMcmaObject<WorkflowJob>();

            var serialized = workflowJob.ToMcmaJson();
            
            Console.WriteLine(serialized);
        }
    
        public static void ToMcmaObject_ShouldDeserializeBmEssence()
        {
            var bmEssenceJObj = JObject.Parse(File.ReadAllText("json/BmEssence.json"));

            var bmEssence = bmEssenceJObj.ToMcmaObject<BMEssence>();

            Console.WriteLine(bmEssence.Id);
            Console.WriteLine(bmEssence.Locations[0].Type);
            Console.WriteLine(((S3Locator)bmEssence.Locations[0]).AwsS3Key);
            Console.WriteLine(((S3Locator)bmEssence.Locations[0]).AwsS3Bucket);
        }

        private const string VIDEO_FORMAT = "AVC";
        private const string VIDEO_CODEC = "mp42";
        private const string VIDEO_CODEC_ISOM = "isom";
        private const int VIDEO_BITRATE_MB = 2;

        private static readonly int THRESHOLD_SECONDS = int.Parse(Environment.GetEnvironmentVariable("THESHOLD_SECONDS"));
        
        public static string ToMcmaObject_ShouldDeserializeBmEssenceMxf()
        {
            var bmEssenceJObj = JObject.Parse(File.ReadAllText("json/BmEssence-mxf.json"));

            var bme = bmEssenceJObj.ToMcmaObject<BMEssence>();

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

            var normalPlayTime = video.NormalPlayTime.ToString();
            var hour = Regex.Match(normalPlayTime, "(\\d*)H");
            var min = Regex.Match(normalPlayTime, "(\\d*)M");
            var sec = Regex.Match(normalPlayTime, "(\\d*)S");
            
            var totalSeconds =
                CalcSeconds(
                    hour.Success ? int.Parse(hour.Groups[1].Captures[0].Value) : 0,
                    min.Success ? int.Parse(min.Groups[1].Captures[0].Value) : 0,
                    double.Parse(sec.Groups[1].Captures[0].Value));

            Console.WriteLine("[Total Seconds]: " + totalSeconds);

            return totalSeconds <= THRESHOLD_SECONDS ? "short" : "long";
        }

        private static double CalcSeconds(int hour, int minute, double seconds)
            => (hour * 60 * 60) + (minute * 60) + seconds;
    }
}
