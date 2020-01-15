using System;
using System.Threading.Tasks;
using Mcma.Core;
using Mcma.Worker;

namespace Mcma.Azure.AzureAiService.Worker
{
    internal class TranscribeAudio : IJobProfile<AIJob>
    {
        public string Name => "Azure" + nameof(TranscribeAudio);

        public Task ExecuteAsync(ProcessJobAssignmentHelper<AIJob> job)
            => throw new NotImplementedException($"{Name} profile has not yet been implemented for Azure.");
    }
}
