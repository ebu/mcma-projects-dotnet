using System;
using Mcma.Api;
using Mcma.Aws.JobProcessor.Common;

namespace Mcma.Aws.JobProcessor.ApiHandler
{
    public static class ApiRequestContextExtensions
    {
        public static string JobId(this McmaApiRequestContext requestContext)
            => requestContext.Request.PathVariables.TryGetValue("jobId", out var jobId) ? jobId.ToString() : null;
        
        public static string ExecutionId(this McmaApiRequestContext requestContext)
            => requestContext.Request.PathVariables.TryGetValue("executionId", out var executionId) ? executionId.ToString() : null;
        public static string PageStartToken(this McmaApiRequestContext requestContext)
            => requestContext.Request.QueryStringParameters.TryGetValue("pageStartToken", out var pageStartToken) ? pageStartToken : null;
        
        public static JobResourceQueryParameters BuildQueryParameters(this McmaApiRequestContext requestContext, int? fallbackLimit = null)
        {
            var parameters = new JobResourceQueryParameters();

            if (requestContext.Request.QueryStringParameters.TryGetValue("status", out var statusText) &&
                Enum.TryParse<JobStatus>(statusText, true, out var status))
                parameters.Status = status;

            if (requestContext.Request.QueryStringParameters.TryGetValue("from", out var tmpFrom) && DateTime.TryParse(tmpFrom, out var from))
                parameters.From = from;
            
            if (requestContext.Request.QueryStringParameters.TryGetValue("to", out var tmpTo) && DateTime.TryParse(tmpTo, out var to))
                parameters.To = to;

            if (requestContext.Request.QueryStringParameters.TryGetValue("order", out var order))
                parameters.Ascending = order.Equals("asc", StringComparison.OrdinalIgnoreCase);

            if (requestContext.Request.QueryStringParameters.TryGetValue("limit", out var tmpLimit) && int.TryParse(tmpLimit, out var limit))
                parameters.Limit = limit;

            if (!parameters.Limit.HasValue && fallbackLimit.HasValue && !parameters.From.HasValue && !parameters.To.HasValue)
                parameters.Limit = fallbackLimit;

            return parameters;
        }
    }
}