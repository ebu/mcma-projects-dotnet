#load "../tasks/project.csx"

public static AggregateTask BuildServices = new AggregateTask(
    new BuildProject("services/Mcma.Azure.ServiceRegistry/ApiHandler", false, false),
    new BuildProject("services/Mcma.Azure.JobRepository/ApiHandler", false, false),
    new BuildProject("services/Mcma.Azure.JobRepository/Worker", false, false),
    new BuildProject("services/Mcma.Azure.JobProcessor/ApiHandler", false, false),
    new BuildProject("services/Mcma.Azure.JobProcessor/Worker", false, false),
    new BuildProject("services/Mcma.Azure.MediaRepository/ApiHandler", false, false),
    new BuildProject("services/Mcma.Azure.WorkflowService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Azure.WorkflowService/Worker", false, false),
    new BuildProject("services/Mcma.Azure.AmeService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Azure.AmeService/Worker", false, false),
    new BuildProject("services/Mcma.Azure.TransformService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Azure.TransformService/Worker", false, false),
    new BuildProject("services/Mcma.Azure.AwsAiService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Azure.AwsAiService/Sns", false, false),
    new BuildProject("services/Mcma.Azure.AwsAiService/Worker", false, false),
    new BuildProject("services/Mcma.Azure.AzureAiService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Azure.AzureAiService/Worker", false, false),
    new BuildProject("services/Mcma.Azure.AzureAiService/Notifications", false, false)
);