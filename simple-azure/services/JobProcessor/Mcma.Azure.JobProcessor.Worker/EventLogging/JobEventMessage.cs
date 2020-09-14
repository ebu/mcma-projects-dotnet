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
            JobProfile = job.JobProfile;
            JobProfileName = jobProfile?.Name;
            JobExecution = jobExecution?.Id;
            JobAssignment = jobExecution?.JobAssignment;
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

        public string JobProfile { get; }

        public string JobProfileName { get; }

        public string JobExecution { get; }

        public string JobAssignment { get; }

        public JobParameterBag JobInput { get; }

        public string JobStatus { get; }

        public ProblemDetail JobError { get; }

        public DateTime? JobActualStartDate { get; }

        public DateTime? JobActualEndDate { get; }

        public long? JobActualDuration { get; }

        public JobParameterBag JobOutput { get; }
    }
}