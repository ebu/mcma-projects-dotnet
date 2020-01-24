#load "../../tasks/aggregate-task.csx"

#load "./set-storage-version/set-storage-version.csx"
#load "./update-service-registry/update-service-registry.csx"
#load "./update-service-registry/clear-service-registry.csx"
#load "./upload-website-config/upload-website-config.csx"
#load "./ping-worker-functions/ping-worker-functions.csx"
#load "./output-website-url/output-website-url.csx"

public class Scripts : AggregateTask
{
    public static readonly AggregateTask PostDeploy =
        new AggregateTask(
            new SetStorageVersion(),
            new UpdateServiceRegistry(),
            new UploadWebsiteConfig(),
            new PingWorkerFunctions(),
            new OutputWebsiteUrl());
    
    public static readonly ITask ClearServiceRegistry = new ClearServiceRegistry();
}