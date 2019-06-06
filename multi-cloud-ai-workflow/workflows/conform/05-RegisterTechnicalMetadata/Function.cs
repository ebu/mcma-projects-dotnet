using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.RegisterTechnicalMetadata
{
    public class Function
    {   
        private string GetAmeJobId(JToken @event)
        {
            return @event["data"]["ameJobId"].FirstOrDefault()?.ToString();
        }

        private BMEssence CreateBmEssence(BMContent bmContent, S3Locator location, JToken mediaInfo)
        {
            return new BMEssence
            {
                BmContent = bmContent.Id,
                Locations = new Locator[] {location},
                ["technicalMetadata"] = mediaInfo
            };
        }

        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 36
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var ameJobId = GetAmeJobId(@event);
            if (ameJobId == null)
                throw new Exception("Failed to obtain AmeJob ID");
            Logger.Debug("[AmeJobID]: " + ameJobId);

            var ameJob = await resourceManager.ResolveAsync<AmeJob>(ameJobId);

            S3Locator outputFile;
            if (!ameJob.JobOutput.TryGet<S3Locator>(nameof(outputFile), false, out outputFile))
                throw new Exception("Unable to get outputFile from AmeJob output.");

            var s3Bucket = outputFile.AwsS3Bucket;
            var s3Key = outputFile.AwsS3Key;
            GetObjectResponse s3Object;
            try
            {
                var s3 = await outputFile.GetClientAsync();
                s3Object = await s3.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = s3Bucket,
                    Key = s3Key
                });
            }
            catch (Exception error)
            {
                throw new Exception("Unable to get media info file in bucket '" + s3Bucket + "' with key '" + s3Key + " due to error: " + error);
            }
            var mediaInfo = JToken.Parse(await new StreamReader(s3Object.ResponseStream).ReadToEndAsync());

            var bmc = await resourceManager.ResolveAsync<BMContent>(@event["data"]["bmContent"].ToString());

            Logger.Debug("[BMContent]: " + bmc.ToMcmaJson());

            Logger.Debug("[@event]:" + @event.ToMcmaJson().ToString());
            Logger.Debug("[mediaInfo]:" + mediaInfo.ToMcmaJson().ToString());

            var bme = CreateBmEssence(bmc, @event["data"]["repositoryFile"].ToMcmaObject<S3Locator>(), mediaInfo);
            
            Logger.Debug("Serializing essence...");
            Logger.Debug("[bme]:" + bme.ToMcmaJson().ToString());

            Logger.Debug("Creating essence...");
            bme = await resourceManager.CreateAsync(bme);
            if (bme.Id == null)
                throw new Exception("Failed to register BMEssence");
            Logger.Debug("[BMEssence ID]: " + bme.Id);

            bmc.BmEssences.Add(bme.Id);

            bmc = await resourceManager.UpdateAsync<BMContent>(bmc);

            return bme.Id;
        }
    }
}