using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Mcma.GoogleCloud.Sample.Scripts.Common;
using Mcma.GoogleCloud.Storage;
using Mcma.GoogleCloud.Storage.Proxies;

namespace Mcma.GoogleCloud.Sample.Scripts.RunJobs
{
    public class RunJobsScript : IScript
    {
        public RunJobsScript(FileUploader fileUploader, JobInitiator jobInitiator, JobPoller jobPoller, StorageClient storageClient)
        {
            FileUploader = fileUploader ?? throw new ArgumentNullException(nameof(fileUploader));
            JobInitiator = jobInitiator ?? throw new ArgumentNullException(nameof(jobInitiator));
            JobPoller = jobPoller ?? throw new ArgumentNullException(nameof(jobPoller));
            StorageClient = storageClient ?? throw new ArgumentNullException(nameof(storageClient));
        }

        private FileUploader FileUploader { get; }

        private JobInitiator JobInitiator { get; }

        private JobPoller JobPoller { get; }

        private StorageClient StorageClient { get; }

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
                var uploadedFileLocator = await FileUploader.UploadFileAsync(testFilePath);

                var awsClientAssembly = AppDomain.CurrentDomain.GetAssemblies()
                                                 .FirstOrDefault(a => a.FullName != null && a.FullName.Contains("Mcma.Aws.Client"));
                
                Console.WriteLine(awsClientAssembly?.FullName + ": " + awsClientAssembly?.Location);

                var jobIds = new List<string>
                {
                    await JobInitiator.StartJobAsync<AmeJob>("ExtractTechnicalMetadata", uploadedFileLocator, "metadata"),
                    await JobInitiator.StartJobAsync<TransformJob>("ExtractThumbnail", uploadedFileLocator, "thumbnail")
                };

                var jobs = await JobPoller.PollJobsForCompletionAsync(jobIds);

                foreach (var job in jobs.Values)
                {
                    if (job.Status == JobStatus.Completed)
                    {
                        var fileLocator = job.JobOutput.Get<CloudStorageFileLocator>("outputFile");
                        Console.WriteLine("Job output: " + await StorageClient.GetLocatorSignedUrlAsync(fileLocator));
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