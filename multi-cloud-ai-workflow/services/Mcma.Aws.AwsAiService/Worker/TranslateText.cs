using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon.Translate;
using Amazon.Translate.Model;
using Mcma.Core;
using Mcma.Aws.S3;
using Mcma.Worker;

namespace Mcma.Aws.AwsAiService.Worker
{
    internal class TranslateText : IJobProfileHandler<AIJob>
    {
        public const string Name = "AWS" + nameof(TranslateText);

        public async Task ExecuteAsync(WorkerJobHelper<AIJob> jobHelper)
        {
            S3Locator inputFile;
            if (!jobHelper.JobInput.TryGet<S3Locator>(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            S3Locator outputLocation;
            if (!jobHelper.JobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                throw new Exception("Invalid or missing output location.");

            var s3Bucket = inputFile.AwsS3Bucket;
            var s3Key = inputFile.AwsS3Key;

            GetObjectResponse s3Object;
            try
            {
                s3Object = await inputFile.GetAsync();
            }
            catch (Exception error)
            {
                throw new Exception($"Unable to read file in bucket '{s3Bucket}' with key '{s3Key}'.", error);
            }

            var inputText = await new StreamReader(s3Object.ResponseStream).ReadToEndAsync();

            var translateParameters = new TranslateTextRequest
            {
                SourceLanguageCode = jobHelper.JobInput.TryGet("sourceLanguageCode", out string srcLanguageCode) ? srcLanguageCode : "auto",
                TargetLanguageCode = jobHelper.JobInput.Get<string>("targetLanguageCode"),
                Text = inputText
            };

            var translateService = new AmazonTranslateClient();
            var translateResponse = await translateService.TranslateTextAsync(translateParameters);

            var s3Params = new PutObjectRequest
            {
                BucketName = outputLocation.AwsS3Bucket,
                Key = (!string.IsNullOrWhiteSpace(outputLocation.AwsS3KeyPrefix) ? outputLocation.AwsS3Key : string.Empty) + Guid.NewGuid() + ".txt",
                ContentBody = translateResponse.TranslatedText
            };

            var outputS3 = await outputLocation.GetClientAsync();
            await outputS3.PutObjectAsync(s3Params);

            jobHelper.JobOutput.Set("outputFile", new S3Locator
            {
                AwsS3Bucket = s3Params.BucketName,
                AwsS3Key = s3Params.Key
            });

            await jobHelper.CompleteAsync();
        }
    }
}
