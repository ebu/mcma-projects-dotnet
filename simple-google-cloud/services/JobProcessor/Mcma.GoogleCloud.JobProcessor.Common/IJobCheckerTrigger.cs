using System;
using System.Threading.Tasks;

namespace Mcma.GoogleCloud.JobProcessor.Common
{
    public interface IJobCheckerTrigger
    {
        Task EnableAsync();
        Task DisableAsync();
    }
}