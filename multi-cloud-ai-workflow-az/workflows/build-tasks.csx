#load "../build/project.csx"

public static readonly IBuildTask BuildWorkflows = new AggregateTask(
    // BuildCommonSteps,
    // BuildConformWorkflow,
    // BuildAiWorkflow
);

public static readonly IBuildTask BuildWorkflowsSln = new AggregateTask(
    // BuildCommonStepsSln,
    // BuildConformWorkflowSln,
    // BuildAiWorkflowSln
);