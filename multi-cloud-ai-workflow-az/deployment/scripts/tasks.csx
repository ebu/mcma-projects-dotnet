#load "../../tasks/aggregate-task.csx"

#load "./set-storage-version/set-storage-version.csx"
#load "./start-worker-functions/start-worker-functions.csx"
#load "./update-service-registry/update-service-registry.csx"
#load "./update-service-registry/clear-service-registry.csx"
#load "./upload-website-config/upload-website-config.csx"

public class Scripts : AggregateTask
{
    public static readonly AggregateTask PostDeploy = new AggregateTask(new SetStorageVersion(), new UpdateServiceRegistry(), new UploadWebsiteConfig());
    
    public static readonly ITask ClearServiceRegistry = new ClearServiceRegistry();

    public static readonly ITask StartWorkerFunctions = new StartWorkerFunctions();
}