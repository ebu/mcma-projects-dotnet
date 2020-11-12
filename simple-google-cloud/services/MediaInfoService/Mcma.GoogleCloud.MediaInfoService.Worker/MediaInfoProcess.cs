using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Mcma.Logging;

namespace Mcma.GoogleCloud.MediaInfoService.Worker
{
    internal class MediaInfoProcess
    {
        private const string MediaInfoFolder = "/opt/bin/";

        public static async Task<MediaInfoProcess> RunAsync(ILogger logger, params string[] args)
        {
            var mediaInfoProcess = new MediaInfoProcess(logger, args);
            await mediaInfoProcess.RunAsync();
            return mediaInfoProcess;
        }

        private MediaInfoProcess(ILogger logger, params string[] args)
        {
            Logger = logger;
            ProcessStartInfo = 
                new ProcessStartInfo(Path.Combine(MediaInfoFolder, "mediainfo"), string.Join(" ", args))
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
        }

        private ILogger Logger { get; }

        private ProcessStartInfo ProcessStartInfo { get; }

        public string StdOut { get; private set; }

        public string StdErr { get; private set; }

        public async Task RunAsync()
        {
            using (var process = Process.Start(ProcessStartInfo))
            {
                if (process == null)
                    throw new McmaException($"Failed to start process at {ProcessStartInfo.FileName}. Process.Start returned null.");
                
                Logger.Debug("MediaInfo process started. Reading stdout and stderr...");
                StdOut = await process.StandardOutput.ReadToEndAsync();
                StdErr = await process.StandardError.ReadToEndAsync();

                Logger.Debug("Waiting for MediaInfo process to exit...");
                process.WaitForExit();
                Logger.Debug($"MediaInfo process exited with code {process.ExitCode}.");

                if (process.ExitCode != 0)
                    throw new Exception($"MediaInfo process exited with code {process.ExitCode}:\r\nStdOut:\r\n{StdOut}StdErr:\r\n{StdErr}");
            }
        }
    }
}