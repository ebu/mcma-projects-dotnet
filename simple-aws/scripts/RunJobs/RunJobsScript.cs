using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Aws.S3;

namespace Mcma.Aws.Sample.Scripts.RunJobs
{
    public class RunJobsScript : IScript
    {
        public RunJobsScript(FileUploader fileUploader, JobInitiator jobInitiator, JobPoller jobPoller)
        {
            FileUploader = fileUploader;
            JobInitiator = jobInitiator;
            JobPoller = jobPoller;
        }

        private FileUploader FileUploader { get; }

        private JobInitiator JobInitiator { get; }

        private JobPoller JobPoller { get; }

        public async Task ExecuteAsync(params string[] args)
        {
            var testFilePath = args.FirstOrDefault(x => x.StartsWith("--testFilePath="))?.Replace("--testFilePath=", string.Empty);
            if (string.IsNullOrWhiteSpace(testFilePath))
            {
                await Console.Error.WriteLineAsync("Must provide a file to process as an argument");
                return;
            }

            try
            {
                Console.WriteLine("Uploading test file...");
                var uploadedObjectKey = await FileUploader.UploadFileAsync(testFilePath);

                var awsClientAssembly = AppDomain.CurrentDomain.GetAssemblies()
                                                 .FirstOrDefault(a => a.FullName != null && a.FullName.Contains("Mcma.Aws.Client"));
                
                Console.WriteLine(awsClientAssembly?.FullName + ": " + awsClientAssembly?.Location);

                var jobIds = new List<string>
                {
                    await JobInitiator.StartJobAsync<AmeJob>("ExtractTechnicalMetadata", uploadedObjectKey, "metadata"),
                    await JobInitiator.StartJobAsync<TransformJob>("ExtractThumbnail", uploadedObjectKey, "thumbnail")
                };

                var jobs = await JobPoller.PollJobsForCompletionAsync(jobIds);

                foreach (var job in jobs.Values)
                {
                    if (job.Status == JobStatus.Completed)
                    {
                        var fileLocator = job.JobOutput.Get<AwsS3FileLocator>("outputFile");
                        Console.WriteLine("Job output: " + fileLocator.Url);
                    }
                    else
                        Console.WriteLine($"Job finished with status {job.Status}");
                }
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error.ToString());
            }
        }
    }
}