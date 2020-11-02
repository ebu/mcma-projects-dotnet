using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Options;

namespace Mcma.Azure.FFmpegService.Worker
{
    internal class FFmpegProcess : IFFmpegProcess
    {
        private const string FFmpegFolder = "exe";

        public FFmpegProcess(IOptions<ExecutionContextOptions> executionContextOptions)
        {
            HostRootDir = executionContextOptions.Value?.AppDirectory;
        }
        
        private string HostRootDir { get; }

        public async Task<(string stdOut, string stdErr)> RunAsync(params string[] args)
        {
            var processStartInfo =
                new ProcessStartInfo(Path.Combine(HostRootDir, FFmpegFolder, "ffmpeg.exe"), string.Join(" ", args))
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

            using var process = Process.Start(processStartInfo);
            
            if (process == null)
                throw new McmaException($"Failed to start FFmpeg process with arguments '{string.Join(" ", args)}'");
            
            var stdOut = await process.StandardOutput.ReadToEndAsync();
            var stdErr = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg process exited with code {process.ExitCode}:\r\nStdOut:\r\n{stdOut}StdErr:\r\n{stdErr}");

            return (stdOut, stdErr);
        }
    }
}