using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Azure.Sample.Scripts.Common;

namespace Mcma.Azure.Sample.Scripts.RunJobs
{
    public class RunJobsScript : IScript
    {
        public RunJobsScript(FileUploader fileUploader, JobInitiator jobInitiator, JobPoller jobPoller, TerraformOutput terraformOutput)
        {
            FileUploader = fileUploader ?? throw new ArgumentNullException(nameof(fileUploader));
            JobInitiator = jobInitiator ?? throw new ArgumentNullException(nameof(jobInitiator));
            JobPoller = jobPoller ?? throw new ArgumentNullException(nameof(jobPoller));
            TerraformOutput = terraformOutput ?? throw new ArgumentNullException(nameof(terraformOutput));
        }

        private FileUploader FileUploader { get; }

        private JobInitiator JobInitiator { get; }

        private JobPoller JobPoller { get; }

        private TerraformOutput TerraformOutput { get; }

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
                var uploadedFileName = await FileUploader.UploadFileAsync(testFilePath);

                var jobIds = new List<string>
                {
                    await JobInitiator.StartJobAsync<AmeJob>("ExtractTechnicalMetadata", uploadedFileName, "metadata"),
                    await JobInitiator.StartJobAsync<TransformJob>("ExtractThumbnail", uploadedFileName, "thumbnail")
                };

                var jobs = await JobPoller.PollJobsForCompletionAsync(jobIds);

                foreach (var job in jobs.Values)
                {
                    if (job.Status == JobStatus.Completed)
                    {
                        var fileLocator = job.JobOutput.Get<BlobStorageFileLocator>("outputFile");
                        var url = fileLocator.Proxy(TerraformOutput.MediaStorageConnectionString).GetPublicReadOnlyUrl();
                        Console.WriteLine("Job output: " + url);
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