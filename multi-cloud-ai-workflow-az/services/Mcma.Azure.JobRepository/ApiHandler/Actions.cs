using System;
using System.Net;
using System.Threading.Tasks;
using Mcma.Api;

namespace Mcma.Azure.JobRepository.ApiHandler
{
    public static class Actions
    {
        public static Func<McmaApiRequestContext, Task> StopHandler() => requestContext =>
        {
            requestContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            requestContext.Response.StatusMessage = "Stopping job is not implemented";
            return Task.CompletedTask;
        };
        
        public static Func<McmaApiRequestContext, Task> CancelHandler() => requestContext =>
        {
            requestContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            requestContext.Response.StatusMessage = "Stopping job is not implemented";
            return Task.CompletedTask;
        };
    }
}
