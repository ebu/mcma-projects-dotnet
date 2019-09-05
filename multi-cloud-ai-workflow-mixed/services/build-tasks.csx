#load "../build/project.csx"

public static AggregateTask BuildServices = new AggregateTask(
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.ServiceRegistry/ApiHandler"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.JobRepository/ApiHandler"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.JobRepository/Worker"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.JobProcessor/ApiHandler"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.JobProcessor/Worker"),
    new BuildAzureFunctionProject("services/Mcma.Azure.MediaRepository/ApiHandler"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.WorkflowService/ApiHandler"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.WorkflowService/Worker"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AmeService/ApiHandler"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AmeService/Worker")
    {
        PostBuildCopies =
        {
            {"externals/mediainfo/18.05.x86_64.RHEL_7", "bin"},
            {"externals/libmediainfo/18.05.x86_64.RHEL_7", "lib"},
            {"externals/libzen/0.4.37.x86_64.RHEL_7", "lib"}
        },
        Zip = {ExternalAttributes = {{"bin/mediainfo", 0755}}}
    },
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.TransformService/ApiHandler"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.TransformService/Worker")
    {
        PostBuildCopies =
        {
            {"externals/ffmpeg", "bin"}
        },
        Zip = {ExternalAttributes = {{"bin/ffmpeg", 0755}}}
    },
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AwsAiService/ApiHandler"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AwsAiService/S3Trigger"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AwsAiService/SnsTrigger"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AwsAiService/Worker"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AzureAiService/ApiHandler"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AzureAiService/ApiInsecure"),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AzureAiService/Worker")
);

public static AggregateTask BuildServicesSln = new AggregateTask(
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.ServiceRegistry/ApiHandler", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.JobRepository/ApiHandler", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.JobRepository/Worker", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.JobProcessor/ApiHandler", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.JobProcessor/Worker", false, false),
    new BuildAzureFunctionProject("services/Mcma.Azure.MediaRepository/ApiHandler", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.WorkflowService/ApiHandler", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.WorkflowService/Worker", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AmeService/ApiHandler", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AmeService/Worker", false, false)
    {
        PostBuildCopies =
        {
            {"externals/mediainfo/18.05.x86_64.RHEL_7", "bin"},
            {"externals/libmediainfo/18.05.x86_64.RHEL_7", "lib"},
            {"externals/libzen/0.4.37.x86_64.RHEL_7", "lib"}
        },
        Zip = {ExternalAttributes = {{"bin/mediainfo", 0755}}}
    },
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.TransformService/ApiHandler", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.TransformService/Worker", false, false)
    {
        PostBuildCopies =
        {
            {"externals/ffmpeg", "bin"}
        },
        Zip = {ExternalAttributes = {{"bin/ffmpeg", 0755}}}
    },
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AwsAiService/ApiHandler", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AwsAiService/S3Trigger", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AwsAiService/SnsTrigger", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AwsAiService/Worker", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AzureAiService/ApiHandler", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AzureAiService/ApiInsecure", false, false),
    new BuildAwsLambdaFunctionProject("services/Mcma.Aws.AzureAiService/Worker", false, false)
);