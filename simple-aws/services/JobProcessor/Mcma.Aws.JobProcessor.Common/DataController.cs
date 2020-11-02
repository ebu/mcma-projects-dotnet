using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Data;
using Mcma.Data.DocumentDatabase.Queries;
using Microsoft.Extensions.Options;

namespace Mcma.Aws.JobProcessor.Common
{
    internal class DataController : IDataController
    {
        public DataController(IDocumentDatabaseTable dbTable, IOptions<DataControllerOptions> options)
        {
            DbTable = dbTable ?? throw new ArgumentNullException(nameof(dbTable));
            PublicUrl = options.Value?.PublicUrl ?? throw new McmaException("PublicUrl not configured for DataController");
        }
        
        private IDocumentDatabaseTable DbTable { get; }
        
        private string PublicUrl { get; }

        private static string ExtractPath(string id)
        {
            var startIdx = id.IndexOf("/jobs/", StringComparison.OrdinalIgnoreCase);
            return id.Substring(startIdx);
        }
        
        public async Task<QueryResults<Job>> QueryJobsAsync(JobResourceQueryParameters queryParameters, string pageStartToken = null)
        {
            queryParameters.PartitionKey = "/jobs";
            return await DbTable.CustomQueryAsync<Job, JobResourceQueryParameters>(new CustomQuery<JobResourceQueryParameters>
            {
                Name = JobResourceQueryBuilder.QueryName,
                Parameters = queryParameters,
                PageStartToken = pageStartToken
            });
        }
        
        public async Task<Job> GetJobAsync(string jobId)
        {
            var jobPath = ExtractPath(jobId);
            return await DbTable.GetAsync<Job>(jobPath);
        }
        
        public async Task<Job> AddJobAsync(Job job)
        {
            var jobPath = $"/jobs/{Guid.NewGuid()}";
            job.Id = PublicUrl.TrimEnd('/') + jobPath;
            job.DateCreated = job.DateModified = DateTimeOffset.UtcNow;
            return await DbTable.PutAsync(jobPath, job);
        }
        
        public async Task<Job> UpdateJobAsync(Job job)
        {
            var jobPath = ExtractPath(job.Id);
            job.DateModified = DateTimeOffset.UtcNow;
            return await DbTable.PutAsync(jobPath, job);
        }
        
        public async Task DeleteJobAsync(string jobId)
        {
            var jobPath = ExtractPath(jobId);
            await DbTable.DeleteAsync(jobPath);
        }

        public async Task<QueryResults<JobExecution>> QueryExecutionsAsync(string jobId,
                                                                           JobResourceQueryParameters queryParameters,
                                                                           string pageStartToken = null)
        {
            var jobPath = ExtractPath(jobId);
            queryParameters.PartitionKey = $"{jobPath}/executions";
            return await DbTable.CustomQueryAsync<JobExecution, JobResourceQueryParameters>(new CustomQuery<JobResourceQueryParameters>
            {
                Name = JobResourceQueryBuilder.QueryName,
                Parameters = queryParameters,
                PageStartToken = pageStartToken
            });
        }

        public async Task<QueryResults<JobExecution>> GetExecutionsAsync(string jobId)
        {
            var jobPath = ExtractPath(jobId);
            return await DbTable.QueryAsync(new Query<JobExecution> { Path = jobPath });
        }

        public async Task<JobExecution> GetExecutionAsync(string jobExecutionId)
        {
            var jobExecutionPath = ExtractPath(jobExecutionId);
            return await DbTable.GetAsync<JobExecution>(jobExecutionPath);
        }

        public async Task<JobExecution> AddExecutionAsync(string jobId, JobExecution jobExecution)
        {
            var executions = await GetExecutionsAsync(jobId);
            var executionNumber = executions.Results.Count();

            jobExecution.Id = $"{jobId}/executions/{executionNumber}";
            jobExecution.DateCreated = jobExecution.DateModified = DateTimeOffset.UtcNow;
            
            var jobExecutionPath = $"{ExtractPath(jobId)}/executions/{executionNumber}";
            
            return await DbTable.PutAsync(jobExecutionPath, jobExecution);
        }

        public async Task<JobExecution> UpdateExecutionAsync(JobExecution jobExecution)
        {
            var jobExecutionPath = ExtractPath(jobExecution.Id);
            jobExecution.DateModified = DateTimeOffset.UtcNow;
            return await DbTable.PutAsync(jobExecutionPath, jobExecution);
        }

        public async Task DeleteExecutionAsync(string jobExecutionId)
        {
            var jobExecutionPath = ExtractPath(jobExecutionId);
            await DbTable.DeleteAsync(jobExecutionPath);
        }

        public Task<IDocumentDatabaseMutex> CreateMutexAsync(string mutexName, string mutexHolder)
            => DbTable.CreateMutexAsync(mutexName, mutexHolder);
    }
}