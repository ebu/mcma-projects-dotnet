using System;

namespace Mcma.Azure.JobProcessor
{
    public class JobEventMessage
    {
        public JobEventMessage(Job job, JobProfile jobProfile, JobExecution jobExecution = null)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            
            JobId = job.Id;
            JobType = job.Type;
            JobProfileId = job.JobProfileId;
            JobProfileName = jobProfile?.Name;
            JobExecutionId = jobExecution?.Id;
            JobAssignmentId = jobExecution?.JobAssignmentId;
            JobInput = job.JobInput;
            JobStatus = job.Status;
            JobError = job.Error;
            JobActualStartDate = jobExecution?.ActualStartDate;
            JobActualEndDate = jobExecution?.ActualEndDate;
            JobActualDuration = jobExecution?.ActualDuration;
            JobOutput = jobExecution?.JobOutput;
        }
        
        public string JobId { get; }

        public string JobType { get; }

        public string JobProfileId { get; }

        public string JobProfileName { get; }

        public string JobExecutionId { get; }

        public string JobAssignmentId { get; }

        public JobParameterBag JobInput { get; }

        public JobStatus JobStatus { get; }

        public ProblemDetail JobError { get; }

        public DateTimeOffset? JobActualStartDate { get; }

        public DateTimeOffset? JobActualEndDate { get; }

        public long? JobActualDuration { get; }

        public JobParameterBag JobOutput { get; }
    }
}