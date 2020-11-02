using System.Threading.Tasks;

namespace Mcma.Azure.FFmpegService.Worker
{
    internal interface IFFmpegProcess
    {
        Task<(string stdOut, string stdErr)> RunAsync(params string[] args);
    }
}