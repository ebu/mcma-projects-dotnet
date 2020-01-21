#load "../../../tasks/task.csx"
#load "../../../tasks/task-runner.csx"

#load "../terraform-output.csx"
#load "./storage-api-version-setter.csx"

public class SetStorageVersion : TaskBase
{
    protected override Task<bool> ExecuteTask()
    {   
        var storageApiVersionSetter = new StorageApiVersionSetter();

        // set storage version in order to enable partial content for seeking in videos
        storageApiVersionSetter.SetDefaultServiceVersion(TerraformOutput.Instance.MediaStorageConnectionString);
        
        return Task.FromResult(true);
    }
}
