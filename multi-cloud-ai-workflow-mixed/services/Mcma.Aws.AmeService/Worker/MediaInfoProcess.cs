using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mcma.Aws.AmeService.Worker
{
    internal class MediaInfoProcess
    {
        private const string MediaInfoFolder = "bin";

        static MediaInfoProcess()
        {
            // adding bin folder to process path
            Environment.SetEnvironmentVariable("PATH",
                Environment.GetEnvironmentVariable("PATH") + ":" + Environment.GetEnvironmentVariable("LAMBDA_TASK_ROOT") + "/" + MediaInfoFolder);
        }

        public static async Task<MediaInfoProcess> RunAsync(params string[] args)
        {
            var mediaInfoProcess = new MediaInfoProcess(args);
            await mediaInfoProcess.RunAsync();
            return mediaInfoProcess;
        }

        public MediaInfoProcess(params string[] args)
        {
            ProcessStartInfo = 
                new ProcessStartInfo(Path.Combine(Directory.GetCurrentDirectory(), MediaInfoFolder + "/mediainfo"), string.Join(" ", args))
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
        }

        private ProcessStartInfo ProcessStartInfo { get; }

        public string StdOut { get; private set; }

        public string StdErr { get; private set; }

        public async Task RunAsync()
        {
            using (var process = Process.Start(ProcessStartInfo))
            {
                process.WaitForExit();

                StdOut = await process.StandardOutput.ReadToEndAsync();
                StdErr = await process.StandardError.ReadToEndAsync();

                if (process.ExitCode != 0)
                    throw new Exception($"MediaInfo process exited with code {process.ExitCode}:\r\nStdOut:\r\n{StdOut}StdErr:\r\n{StdErr}");
            }
        }
    }
}