using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mcma.Aws.TransformService.Worker
{
    internal class FFmpegProcess
    {
        private const string FFmpegFolder = "bin";

        static FFmpegProcess()
        {
            // adding bin folder to process path
            Environment.SetEnvironmentVariable("PATH",
                Environment.GetEnvironmentVariable("PATH") + ":" + Environment.GetEnvironmentVariable("LAMBDA_TASK_ROOT") + "/" + FFmpegFolder);
        }

        public static async Task<FFmpegProcess> RunAsync(params string[] args)
        {
            var ffmpegProcess = new FFmpegProcess(args);
            await ffmpegProcess.RunAsync();
            return ffmpegProcess;
        }

        public FFmpegProcess(params string[] args)
        {
            ProcessStartInfo = 
                new ProcessStartInfo(Path.Combine(Directory.GetCurrentDirectory(), FFmpegFolder + "/ffmpeg"), string.Join(" ", args))
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
                    throw new Exception($"FFmpeg process exited with code {process.ExitCode}:\r\nStdOut:\r\n{StdOut}StdErr:\r\n{StdErr}");
            }
        }
    }
}