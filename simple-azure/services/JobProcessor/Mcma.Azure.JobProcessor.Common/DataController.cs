using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Azure.CosmosDb;
using Mcma.Data;
using Mcma.Data.DocumentDatabase.Queries;

namespace Mcma.Azure.JobProcessor.Common
{
    public class DataController
    {
        public DataController(string tableName, string publicUrl, bool? consistentRead = null)
        {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            PublicUrl = publicUrl ?? throw new ArgumentNullException(nameof(publicUrl));
            
            DbTableProvider = new CosmosDbTableProvider(new CosmosDbTableProviderConfiguration().FromEnvironmentVariables(),
                                                        GetCosmosDbTableOptions(consistentRead));
        }
        
        private CosmosDbTableProvider DbTableProvider { get; }
        
        private string TableName { get; }
        
        private string PublicUrl { get; }
        
        private IDocumentDatabaseTable DbTable { get; set; }

        private static CosmosDbTableOptions GetCosmosDbTableOptions(bool? consistentRead)
            => new CosmosDbTableOptions
            {
                ConsistentGet = consistentRead,
                ConsistentQuery = consistentRead
            };

        private static string ExtractPath(string id)
        {
            var startIdx = id.IndexOf("/jobs/", StringComparison.OrdinalIgnoreCase);
            return id.Substring(startIdx);
        }

        private static Query<T> BuildQuery<T>(JobResourceQueryParameters queryParameters, string pageStartToken) where T : JobBase
        {
            var (partitionKey, status, from, to, ascending, limit) = queryParameters;
            var query = new Query<T>
            {
                Path = partitionKey,
                SortBy = nameof(McmaResource.DateCreated),
                SortAscending = ascending ?? false,
                PageSize = limit,
                PageStartToken = pageStartToken
            };

            if (status.HasValue)
                query.AddFilterExpression(
                    new FilterCriteria<T, JobStatus>(j => j.Status, BinaryOperator.EqualTo, status.Value));
            
            if (from.HasValue)
                query.AddFilterExpression(
                    new FilterCriteria<T, DateTimeOffset?>(j => j.DateCreated, BinaryOperator.GreaterThanOrEqualTo, from.Value));
            
            if (to.HasValue)
                query.AddFilterExpression(
                    new FilterCriteria<T, DateTimeOffset?>(j => j.DateCreated, BinaryOperator.LessThanOrEqualTo, to.Value));

            return query;
        }

        private async Task InitAsync()
        {
            if (DbTable == null)
                DbTable = await DbTableProvider.GetAsync(TableName);
        }
        
        public async Task<QueryResults<Job>> QueryJobsAsync(JobResourceQueryParameters queryParameters, string pageStartToken = null)
        {
            await InitAsync();
            queryParameters.PartitionKey = "/jobs";
            return await DbTable.QueryAsync(BuildQuery<Job>(queryParameters, pageStartToken));
        }
        
        public async Task<Job> GetJobAsync(string jobId)
        {
            await InitAsync();
            var jobPath = ExtractPath(jobId);
            return await DbTable.GetAsync<Job>(jobPath);
        }
        
        public async Task<Job> AddJobAsync(Job job)
        {
            await InitAsync();
            var jobPath = $"/jobs/{Guid.NewGuid()}";
            job.Id = PublicUrl.TrimEnd('/') + jobPath;
            job.DateCreated = job.DateModified = DateTime.UtcNow;
            return await DbTable.PutAsync(jobPath, job);
        }
        
        public async Task<Job> UpdateJobAsync(Job job)
        {
            await InitAsync();
            var jobPath = ExtractPath(job.Id);
            job.DateModified = DateTime.UtcNow;
            return await DbTable.PutAsync(jobPath, job);
        }
        
        public async Task DeleteJobAsync(string jobId)
        {
            await InitAsync();
            var jobPath = ExtractPath(jobId);
            await DbTable.DeleteAsync(jobPath);
        }

        public async Task<QueryResults<JobExecution>> QueryExecutionsAsync(string jobId,
                                                                           JobResourceQueryParameters queryParameters,
                                                                           string pageStartToken = null)
        {
            await InitAsync();
            var jobPath = ExtractPath(jobId);
            queryParameters.PartitionKey = $"{jobPath}/executions";
            return await DbTable.QueryAsync(BuildQuery<JobExecution>(queryParameters, pageStartToken));
        }

        public async Task<QueryResults<JobExecution>> GetExecutionsAsync(string jobId)
        {
            await InitAsync();
            var jobPath = ExtractPath(jobId);
            return await DbTable.QueryAsync(new Query<JobExecution> { Path = jobPath });
        }

        public async Task<JobExecution> GetExecutionAsync(string jobExecutionId)
        {
            await InitAsync();
            var jobExecutionPath = ExtractPath(jobExecutionId);
            return await DbTable.GetAsync<JobExecution>(jobExecutionPath);
        }

        public async Task<JobExecution> AddExecutionAsync(string jobId, JobExecution jobExecution)
        {
            await InitAsync();
            
            var executions = await GetExecutionsAsync(jobId);
            var executionNumber = executions.Results.Count();

            jobExecution.Id = $"{jobId}/executions/{executionNumber}";
            jobExecution.DateCreated = jobExecution.DateModified = DateTime.UtcNow;
            
            var jobExecutionPath = $"{ExtractPath(jobId)}/executions/{executionNumber}";
            
            return await DbTable.PutAsync(jobExecutionPath, jobExecution);
        }

        public async Task<JobExecution> UpdateExecutionAsync(JobExecution jobExecution)
        {
            await InitAsync();
            var jobExecutionPath = ExtractPath(jobExecution.Id);
            jobExecution.DateModified = DateTime.UtcNow;
            return await DbTable.PutAsync(jobExecutionPath, jobExecution);
        }

        public async Task DeleteExecutionAsync(string jobExecutionId)
        {
            await InitAsync();
            var jobExecutionPath = ExtractPath(jobExecutionId);
            await DbTable.DeleteAsync(jobExecutionPath);
        }

        public async Task<IDocumentDatabaseMutex> CreateMutexAsync(string mutexName, string mutexHolder)
        {
            await InitAsync();
            return DbTable.CreateMutex(mutexName, mutexHolder);
        }
    }
}