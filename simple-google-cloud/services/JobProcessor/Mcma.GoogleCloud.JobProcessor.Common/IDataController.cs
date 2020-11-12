using System.Threading.Tasks;
using Mcma.Data;

namespace Mcma.GoogleCloud.JobProcessor.Common
{
    public interface IDataController
    {
        Task<QueryResults<Job>> QueryJobsAsync(JobResourceQueryParameters queryParameters, string pageStartToken = null);

        Task<Job> GetJobAsync(string jobId);

        Task<Job> AddJobAsync(Job job);

        Task<Job> UpdateJobAsync(Job job);

        Task DeleteJobAsync(string jobId);

        Task<QueryResults<JobExecution>> QueryExecutionsAsync(string jobId,
                                                              JobResourceQueryParameters queryParameters,
                                                              string pageStartToken = null);

        Task<QueryResults<JobExecution>> GetExecutionsAsync(string jobId);

        Task<JobExecution> GetExecutionAsync(string jobExecutionId);

        Task<JobExecution> AddExecutionAsync(string jobId, JobExecution jobExecution);

        Task<JobExecution> UpdateExecutionAsync(JobExecution jobExecution);

        Task DeleteExecutionAsync(string jobExecutionId);

        Task<IDocumentDatabaseMutex> CreateMutexAsync(string mutexName, string mutexHolder);
    }
}