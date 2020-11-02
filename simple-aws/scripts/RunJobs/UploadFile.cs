using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws.S3;
using Mcma.Aws.Sample.Scripts.Common;

namespace Mcma.Aws.Sample.Scripts.RunJobs
{
    public class FileUploader
    {
        public FileUploader(AwsCredentials awsCredentials, TerraformOutput terraformOutput, ExecutionIdProvider executionIdProvider)
        {
            AwsCredentials = awsCredentials;
            TerraformOutput = terraformOutput;
            ExecutionIdProvider = executionIdProvider;
        }
     
        private AwsCredentials AwsCredentials { get; }

        private TerraformOutput TerraformOutput { get; }

        private ExecutionIdProvider ExecutionIdProvider { get; }

        public async Task<string> UploadFileAsync(string localFilePath)
        {
            if (!File.Exists(localFilePath))
                throw new McmaException($"Local file not found at provided path '{localFilePath}'");

            var key = ExecutionIdProvider.Id + "/" + Path.GetFileName(localFilePath);

            var s3 = new AmazonS3Client(AwsCredentials.Credentials, AwsCredentials.Region);

            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = TerraformOutput.UploadBucket,
                Key = key,
                FilePath = localFilePath
            });

            return key;
        }
    }
}