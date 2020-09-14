﻿using Mcma.Context;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Mcma.Azure.JobProcessor
{
    public static class ContextVariableProviderExtensions
    {
        public static long? DefaultJobTimeoutInMinutes(this IContextVariableProvider contextVariableProvider)
            => 
                long.TryParse(contextVariableProvider.GetOptionalContextVariable(nameof(DefaultJobTimeoutInMinutes)), out var tmp)
                    ? tmp
                    : default(long?);
    }
}