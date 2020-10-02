using System.Threading.Tasks;

namespace Mcma.Azure.JobProcessor.Common
{
    public interface IJobCheckerTrigger
    {
        Task EnableAsync();
        Task DisableAsync();
    }
}