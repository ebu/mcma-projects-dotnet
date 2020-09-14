using System;

namespace Mcma.Azure.JobProcessor.Common
{
    public class JobResourceQueryParameters
    {
        public string PartitionKey { get; set; }
        
        public JobStatus Status { get; set; }
        
        public DateTime? From { get; set; }
        
        public DateTime? To { get; set; }
        
        public bool? Ascending { get; set; }
        
        public int? Limit { get; set; }

        public void Deconstruct(
            out string partitionKey,
            out JobStatus status,
            out DateTime? from,
            out DateTime? to,
            out bool? ascending,
            out int? limit)
        {
            partitionKey = PartitionKey;
            status = Status;
            from = From;
            to = To;
            ascending = Ascending;
            limit = Limit;
        }
    }
}