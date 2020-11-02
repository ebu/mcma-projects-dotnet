using System;

namespace Mcma.Azure.Sample.Scripts.Common
{
    public class ExecutionIdProvider
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }
}