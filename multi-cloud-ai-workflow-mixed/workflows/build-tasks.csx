#load "../build/project.csx"

public static readonly IBuildTask BuildCommonSteps = new AggregateTask(
    new BuildAwsLambdaFunctionProject("workflows/ProcessWorkflowCompletion"),
    new BuildAwsLambdaFunctionProject("workflows/ProcessWorkflowFailure"),
    new BuildAwsLambdaFunctionProject("workflows/WorkflowActivityCallbackHandler")
);

public static readonly IBuildTask BuildConformWorkflow = new AggregateTask(
    new BuildAwsLambdaFunctionProject("workflows/conform/01-ValidateWorkflowInput"),
    new BuildAwsLambdaFunctionProject("workflows/conform/02-MoveContentToFileRep"),
    new BuildAwsLambdaFunctionProject("workflows/conform/03-CreateMediaAsset"),
    new BuildAwsLambdaFunctionProject("workflows/conform/04-ExtractTechnicalMetadata"),
    new BuildAwsLambdaFunctionProject("workflows/conform/05-RegisterTechnicalMetadata"),
    new BuildAwsLambdaFunctionProject("workflows/conform/06-DecideTranscodeReqs"),
    new BuildAwsLambdaFunctionProject("workflows/conform/07a-ShortTranscode"),
    new BuildAwsLambdaFunctionProject("workflows/conform/07b-LongTranscode"),
    new BuildAwsLambdaFunctionProject("workflows/conform/08-RegisterProxyEssence"),
    new BuildAwsLambdaFunctionProject("workflows/conform/09-CopyProxyToWebsiteStorage"),
    new BuildAwsLambdaFunctionProject("workflows/conform/10-RegisterProxyWebsiteLoc"),
    new BuildAwsLambdaFunctionProject("workflows/conform/11-StartAiWorkflow")
);

public static readonly IBuildTask BuildAiWorkflow = new AggregateTask(
    new BuildAwsLambdaFunctionProject("workflows/ai/01-ValidateWorkflowInput"),
    new BuildAwsLambdaFunctionProject("workflows/ai/02-ExtractSpeechToText"),
    new BuildAwsLambdaFunctionProject("workflows/ai/03-RegisterSpeechToTextOutput"),
    new BuildAwsLambdaFunctionProject("workflows/ai/04-TranslateSpeechTranscription"),
    new BuildAwsLambdaFunctionProject("workflows/ai/05-RegisterSpeechTranslation"),
    new BuildAwsLambdaFunctionProject("workflows/ai/06-DetectCelebritiesAws"),
    new BuildAwsLambdaFunctionProject("workflows/ai/07-RegisterCelebritiesInfoAws"),
    new BuildAwsLambdaFunctionProject("workflows/ai/08-DetectCelebritiesAzure"),
    new BuildAwsLambdaFunctionProject("workflows/ai/09-RegisterCelebritiesInfoAzure")
);

public static readonly IBuildTask BuildWorkflows = new AggregateTask(
    BuildCommonSteps,
    BuildConformWorkflow,
    BuildAiWorkflow
);

public static readonly IBuildTask BuildCommonStepsSln = new AggregateTask(
    new BuildAwsLambdaFunctionProject("workflows/ProcessWorkflowCompletion", false, false),
    new BuildAwsLambdaFunctionProject("workflows/ProcessWorkflowFailure", false, false),
    new BuildAwsLambdaFunctionProject("workflows/WorkflowActivityCallbackHandler", false, false)
);

public static readonly IBuildTask BuildConformWorkflowSln = new AggregateTask(
    new BuildAwsLambdaFunctionProject("workflows/conform/01-ValidateWorkflowInput", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/02-MoveContentToFileRep", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/03-CreateMediaAsset", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/04-ExtractTechnicalMetadata", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/05-RegisterTechnicalMetadata", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/06-DecideTranscodeReqs", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/07a-ShortTranscode", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/07b-LongTranscode", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/08-RegisterProxyEssence", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/09-CopyProxyToWebsiteStorage", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/10-RegisterProxyWebsiteLoc", false, false),
    new BuildAwsLambdaFunctionProject("workflows/conform/11-StartAiWorkflow", false, false)
);

public static readonly IBuildTask BuildAiWorkflowSln = new AggregateTask(
    new BuildAwsLambdaFunctionProject("workflows/ai/01-ValidateWorkflowInput", false, false),
    new BuildAwsLambdaFunctionProject("workflows/ai/02-ExtractSpeechToText", false, false),
    new BuildAwsLambdaFunctionProject("workflows/ai/03-RegisterSpeechToTextOutput", false, false),
    new BuildAwsLambdaFunctionProject("workflows/ai/04-TranslateSpeechTranscription", false, false),
    new BuildAwsLambdaFunctionProject("workflows/ai/05-RegisterSpeechTranslation", false, false),
    new BuildAwsLambdaFunctionProject("workflows/ai/06-DetectCelebritiesAws", false, false),
    new BuildAwsLambdaFunctionProject("workflows/ai/07-RegisterCelebritiesInfoAws", false, false),
    new BuildAwsLambdaFunctionProject("workflows/ai/08-DetectCelebritiesAzure", false, false),
    new BuildAwsLambdaFunctionProject("workflows/ai/09-RegisterCelebritiesInfoAzure", false, false)
);

public static readonly IBuildTask BuildWorkflowsSln = new AggregateTask(
    BuildCommonStepsSln,
    BuildConformWorkflowSln,
    BuildAiWorkflowSln
);