namespace Mcma.GoogleCloud.JobProcessor.Worker
{
    internal class JobFailure : JobReference
    {
        public ProblemDetail Error { get; set; }
    }
}