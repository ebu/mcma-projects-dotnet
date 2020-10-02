using Amazon.S3;
using Amazon.S3.Model;
using Mcma;
using Mcma.Aws.S3;
using Newtonsoft.Json.Linq;

public static async Task<AwsS3FileLocator> UploadFileAsync(string localFilePath, JObject terraformOutput, AwsCredentials awsCreds)
{
    if (!File.Exists(localFilePath))
        throw new McmaException($"Local file not found at provided path '{localFilePath}'");

    var key = Path.GetFileNameWithoutExtension(localFilePath) + "-" + Guid.NewGuid() + Path.GetExtension(localFilePath);
    var bucket = terraformOutput["upload_bucket"]["value"].Value<string>();

    var s3 = new AmazonS3Client(awsCreds.Credentials, awsCreds.Region);

    var resp = await s3.PutObjectAsync(new PutObjectRequest
    {
        BucketName = bucket,
        Key = key,
        FilePath = localFilePath
    });

    return new AwsS3FileLocator
    {
        Bucket = bucket,
        Key = key
    };
}