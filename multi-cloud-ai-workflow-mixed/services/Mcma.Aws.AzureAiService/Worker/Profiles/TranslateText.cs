using System;
using System.Threading.Tasks;
using Mcma.Core;
using Mcma.Worker;

namespace Mcma.Aws.AzureAiService.Worker
{
    internal class TranslateText : IJobProfileHandler<AIJob>
    {
        public const string Name = "Azure" + nameof(TranslateText);

        public Task ExecuteAsync(WorkerJobHelper<AIJob> job)
            => throw new NotImplementedException($"{Name} profile has not yet been implemented for Azure.");
    }
}
