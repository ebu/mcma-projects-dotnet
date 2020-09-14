namespace Mcma.Azure.JobProcessor.Worker
{
    internal class JobFailure : JobReference
    {
        public ProblemDetail Error { get; set; }
    }
}