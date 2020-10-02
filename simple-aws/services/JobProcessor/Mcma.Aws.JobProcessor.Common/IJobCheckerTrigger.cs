using System.Threading.Tasks;

namespace Mcma.Aws.JobProcessor.Common
{
    public interface IJobCheckerTrigger
    {
        Task EnableAsync();
        Task DisableAsync();
    }
}