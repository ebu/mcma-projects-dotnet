using System;

namespace Mcma.Aws.Sample.Scripts.Common
{
    public class ExecutionIdProvider
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }
}