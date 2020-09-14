﻿using Mcma.Context;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Mcma.Azure.JobProcessor.PeriodicJobCleanup
{
    public static class ContextVariableProviderExtensions
    {
        public static int? JobRetentionPeriodInDays(this IContextVariableProvider contextVariableProvider)
            => 
                int.TryParse(contextVariableProvider.GetOptionalContextVariable(nameof(JobRetentionPeriodInDays)), out var tmp)
                    ? tmp
                    : default(int?);
    }
}