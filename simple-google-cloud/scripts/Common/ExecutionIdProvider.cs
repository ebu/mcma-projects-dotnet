using System;

namespace Mcma.GoogleCloud.Sample.Scripts.Common
{
    public class ExecutionIdProvider
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }
}