#load "../build/project.csx"

public static readonly IBuildTask BuildCommonSteps = new AggregateTask(
    new BuildProject("workflows/ProcessWorkflowCompletion"),
    new BuildProject("workflows/ProcessWorkflowFailure"),
    new BuildProject("workflows/WorkflowActivityCallbackHandler")
);

public static readonly IBuildTask BuildConformWorkflow = new AggregateTask(
    new BuildProject("workflows/conform/01-ValidateWorkflowInput"),
    new BuildProject("workflows/conform/02-MoveContentToFileRep"),
    new BuildProject("workflows/conform/03-CreateMediaAsset"),
    new BuildProject("workflows/conform/04-ExtractTechnicalMetadata"),
    new BuildProject("workflows/conform/05-RegisterTechnicalMetadata"),
    new BuildProject("workflows/conform/06-DecideTranscodeReqs"),
    new BuildProject("workflows/conform/07a-ShortTranscode"),
    new BuildProject("workflows/conform/07b-LongTranscode"),
    new BuildProject("workflows/conform/08-RegisterProxyEssence"),
    new BuildProject("workflows/conform/09-CopyProxyToWebsiteStorage"),
    new BuildProject("workflows/conform/10-RegisterProxyWebsiteLoc"),
    new BuildProject("workflows/conform/11-StartAiWorkflow")
);

public static readonly IBuildTask BuildAiWorkflow = new AggregateTask(
    new BuildProject("workflows/ai/01-ValidateWorkflowInput"),
    new BuildProject("workflows/ai/02-ExtractSpeechToText"),
    new BuildProject("workflows/ai/03-RegisterSpeechToTextOutput"),
    new BuildProject("workflows/ai/04-TranslateSpeechTranscription"),
    new BuildProject("workflows/ai/05-RegisterSpeechTranslation"),
    new BuildProject("workflows/ai/06-DetectCelebritiesAws"),
    new BuildProject("workflows/ai/07-RegisterCelebritiesInfoAws"),
    new BuildProject("workflows/ai/08-DetectCelebritiesAzure"),
    new BuildProject("workflows/ai/09-RegisterCelebritiesInfoAzure")
);

public static readonly IBuildTask BuildWorkflows = new AggregateTask(
    BuildCommonSteps,
    BuildConformWorkflow,
    BuildAiWorkflow
);

public static readonly IBuildTask BuildCommonStepsSln = new AggregateTask(
    new BuildProject("workflows/ProcessWorkflowCompletion", false, false),
    new BuildProject("workflows/ProcessWorkflowFailure", false, false),
    new BuildProject("workflows/WorkflowActivityCallbackHandler", false, false)
);

public static readonly IBuildTask BuildConformWorkflowSln = new AggregateTask(
    new BuildProject("workflows/conform/01-ValidateWorkflowInput", false, false),
    new BuildProject("workflows/conform/02-MoveContentToFileRep", false, false),
    new BuildProject("workflows/conform/03-CreateMediaAsset", false, false),
    new BuildProject("workflows/conform/04-ExtractTechnicalMetadata", false, false),
    new BuildProject("workflows/conform/05-RegisterTechnicalMetadata", false, false),
    new BuildProject("workflows/conform/06-DecideTranscodeReqs", false, false),
    new BuildProject("workflows/conform/07a-ShortTranscode", false, false),
    new BuildProject("workflows/conform/07b-LongTranscode", false, false),
    new BuildProject("workflows/conform/08-RegisterProxyEssence", false, false),
    new BuildProject("workflows/conform/09-CopyProxyToWebsiteStorage", false, false),
    new BuildProject("workflows/conform/10-RegisterProxyWebsiteLoc", false, false),
    new BuildProject("workflows/conform/11-StartAiWorkflow", false, false)
);

public static readonly IBuildTask BuildAiWorkflowSln = new AggregateTask(
    new BuildProject("workflows/ai/01-ValidateWorkflowInput", false, false),
    new BuildProject("workflows/ai/02-ExtractSpeechToText", false, false),
    new BuildProject("workflows/ai/03-RegisterSpeechToTextOutput", false, false),
    new BuildProject("workflows/ai/04-TranslateSpeechTranscription", false, false),
    new BuildProject("workflows/ai/05-RegisterSpeechTranslation", false, false),
    new BuildProject("workflows/ai/06-DetectCelebritiesAws", false, false),
    new BuildProject("workflows/ai/07-RegisterCelebritiesInfoAws", false, false),
    new BuildProject("workflows/ai/08-DetectCelebritiesAzure", false, false),
    new BuildProject("workflows/ai/09-RegisterCelebritiesInfoAzure", false, false)
);

public static readonly IBuildTask BuildWorkflowsSln = new AggregateTask(
    BuildCommonStepsSln,
    BuildConformWorkflowSln,
    BuildAiWorkflowSln
);