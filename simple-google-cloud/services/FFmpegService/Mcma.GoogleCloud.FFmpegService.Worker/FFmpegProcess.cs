using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Mcma.Logging;

namespace Mcma.GoogleCloud.FFmpegService.Worker
{
    internal class FFmpegProcess
    {
        private const string FFmpegFolder = "/opt/bin";

        public static async Task<FFmpegProcess> RunAsync(ILogger logger, params string[] args)
        {
            var ffmpegProcess = new FFmpegProcess(logger, args);
            await ffmpegProcess.RunAsync();
            return ffmpegProcess;
        }

        private FFmpegProcess(ILogger logger, params string[] args)
        {
            Logger = logger;

            ProcessStartInfo = 
                new ProcessStartInfo(Path.Combine(FFmpegFolder, "ffmpeg"), string.Join(" ", args))
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
                Logger.Debug("FFmpeg process started. Reading stdout and stderr...");
                StdOut = await process.StandardOutput.ReadToEndAsync();
                StdErr = await process.StandardError.ReadToEndAsync();

                Logger.Debug("Waiting for FFmpeg process to exit...");
                process.WaitForExit();
                Logger.Debug($"FFmpeg process exited with code {process.ExitCode}.");

                if (process.ExitCode != 0)
                    throw new Exception($"FFmpeg process exited with code {process.ExitCode}:\r\nStdOut:\r\n{StdOut}StdErr:\r\n{StdErr}");
            }
        }
    }
}