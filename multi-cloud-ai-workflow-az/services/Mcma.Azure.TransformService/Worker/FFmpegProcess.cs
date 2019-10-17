using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Mcma.Core.Context;

namespace Mcma.Azure.TransformService.Worker
{
    internal class FFmpegProcess
    {
        private const string FFmpegFolder = "exe";

        public static string HostRootDir { get; set; }

        public static async Task<FFmpegProcess> RunAsync(IContext context, params string[] args)
        {
            var ffmpegProcess = new FFmpegProcess(context, args);
            await ffmpegProcess.RunAsync();
            return ffmpegProcess;
        }

        private FFmpegProcess(IContext context, params string[] args)
        {
            ProcessStartInfo = 
                new ProcessStartInfo(Path.Combine(HostRootDir, FFmpegFolder, "ffmpeg.exe"), string.Join(" ", args))
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
        }

        private IContext Context { get; }

        private ProcessStartInfo ProcessStartInfo { get; }

        public string StdOut { get; private set; }

        public string StdErr { get; private set; }

        public async Task RunAsync()
        {
            using (var process = Process.Start(ProcessStartInfo))
            {
                Context.Logger.Debug("FFmpeg process started. Reading stdout and stderr...");
                StdOut = await process.StandardOutput.ReadToEndAsync();
                StdErr = await process.StandardError.ReadToEndAsync();

                Context.Logger.Debug("Waiting for FFmpeg process to exit...");
                process.WaitForExit();
                Context.Logger.Debug($"FFmpeg process exited with code {process.ExitCode}.");

                if (process.ExitCode != 0)
                    throw new Exception($"FFmpeg process exited with code {process.ExitCode}:\r\nStdOut:\r\n{StdOut}StdErr:\r\n{StdErr}");
            }
        }
    }
}