#load "../build/project.csx"

public static AggregateTask BuildServices = new AggregateTask(
    new BuildProject("services/Mcma.Aws.ServiceRegistry/ApiHandler"),
    new BuildProject("services/Mcma.Aws.JobRepository/ApiHandler"),
    new BuildProject("services/Mcma.Aws.JobRepository/Worker"),
    new BuildProject("services/Mcma.Aws.JobProcessor/ApiHandler"),
    new BuildProject("services/Mcma.Aws.JobProcessor/Worker"),
    new BuildProject("services/Mcma.Aws.MediaRepository/ApiHandler"),
    new BuildProject("services/Mcma.Aws.WorkflowService/ApiHandler"),
    new BuildProject("services/Mcma.Aws.WorkflowService/Worker"),
    new BuildProject("services/Mcma.Aws.AmeService/ApiHandler"),
    new BuildProject("services/Mcma.Aws.AmeService/Worker")
    {
        PostBuildCopies =
        {
            {"externals/mediainfo/18.05.x86_64.RHEL_7", "bin"},
            {"externals/libmediainfo/18.05.x86_64.RHEL_7", "lib"},
            {"externals/libzen/0.4.37.x86_64.RHEL_7", "lib"}
        },
        Zip = {ExternalAttributes = {{"bin/mediainfo", 0755}}}
    },
    new BuildProject("services/Mcma.Aws.TransformService/ApiHandler"),
    new BuildProject("services/Mcma.Aws.TransformService/Worker")
    {
        PostBuildCopies =
        {
            {"externals/ffmpeg", "bin"}
        },
        Zip = {ExternalAttributes = {{"bin/ffmpeg", 0755}}}
    },
    new BuildProject("services/Mcma.Aws.AwsAiService/ApiHandler"),
    new BuildProject("services/Mcma.Aws.AwsAiService/S3Trigger"),
    new BuildProject("services/Mcma.Aws.AwsAiService/SnsTrigger"),
    new BuildProject("services/Mcma.Aws.AwsAiService/Worker"),
    new BuildProject("services/Mcma.Aws.AzureAiService/ApiHandler"),
    new BuildProject("services/Mcma.Aws.AzureAiService/ApiInsecure"),
    new BuildProject("services/Mcma.Aws.AzureAiService/Worker")
);

public static AggregateTask BuildServicesSln = new AggregateTask(
    new BuildProject("services/Mcma.Aws.ServiceRegistry/ApiHandler", false, false),
    new BuildProject("services/Mcma.Aws.JobRepository/ApiHandler", false, false),
    new BuildProject("services/Mcma.Aws.JobRepository/Worker", false, false),
    new BuildProject("services/Mcma.Aws.JobProcessor/ApiHandler", false, false),
    new BuildProject("services/Mcma.Aws.JobProcessor/Worker", false, false),
    new BuildProject("services/Mcma.Aws.MediaRepository/ApiHandler", false, false),
    new BuildProject("services/Mcma.Aws.WorkflowService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Aws.WorkflowService/Worker", false, false),
    new BuildProject("services/Mcma.Aws.AmeService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Aws.AmeService/Worker", false, false)
    {
        PostBuildCopies =
        {
            {"externals/mediainfo/18.05.x86_64.RHEL_7", "bin"},
            {"externals/libmediainfo/18.05.x86_64.RHEL_7", "lib"},
            {"externals/libzen/0.4.37.x86_64.RHEL_7", "lib"}
        },
        Zip = {ExternalAttributes = {{"bin/mediainfo", 0755}}}
    },
    new BuildProject("services/Mcma.Aws.TransformService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Aws.TransformService/Worker", false, false)
    {
        PostBuildCopies =
        {
            {"externals/ffmpeg", "bin"}
        },
        Zip = {ExternalAttributes = {{"bin/ffmpeg", 0755}}}
    },
    new BuildProject("services/Mcma.Aws.AwsAiService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Aws.AwsAiService/S3Trigger", false, false),
    new BuildProject("services/Mcma.Aws.AwsAiService/SnsTrigger", false, false),
    new BuildProject("services/Mcma.Aws.AwsAiService/Worker", false, false),
    new BuildProject("services/Mcma.Aws.AzureAiService/ApiHandler", false, false),
    new BuildProject("services/Mcma.Aws.AzureAiService/ApiInsecure", false, false),
    new BuildProject("services/Mcma.Aws.AzureAiService/Worker", false, false)
);