using System;

namespace Mcma.Aws.JobProcessor.Common
{
    public class JobResourceQueryParameters
    {
        public string PartitionKey { get; set; }
        
        public JobStatus? Status { get; set; }
        
        public DateTimeOffset? From { get; set; }
        
        public DateTimeOffset? To { get; set; }
        
        public bool? Ascending { get; set; }
        
        public int? Limit { get; set; }

        public void Deconstruct(
            out string partitionKey,
            out JobStatus? status,
            out DateTimeOffset? from,
            out DateTimeOffset? to,
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