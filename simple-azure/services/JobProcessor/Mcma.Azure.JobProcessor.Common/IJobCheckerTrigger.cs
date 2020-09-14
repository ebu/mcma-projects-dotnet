using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;

namespace Mcma.Azure.JobProcessor.Common
{
    public interface IJobCheckerTrigger
    {
        Task EnableAsync();
        Task DisableAsync();
    }
}