using System.Threading.Tasks;

namespace Mcma.Azure.MediaInfoService.Worker
{
    internal interface IMediaInfoProcess
    {
        Task<(string stdOut, string stdErr)> RunAsync(params string[] args);
    }
}