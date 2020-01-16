#load "../../../tasks/task.csx"
#load "../../../tasks/task-runner.csx"

#load "../terraform-output.csx"
#load "./storage-api-version-setter.csx"

public class SetStorageVersion : TaskBase
{
    public SetStorageVersion()
    {
        StorageApiVersionSetter = new StorageApiVersionSetter(TerraformOutput);
    }

    private TerraformOutput TerraformOutput { get; } = TerraformOutput.Load();

    private StorageApiVersionSetter StorageApiVersionSetter { get; }

    protected override Task<bool> ExecuteTask()
    {   
        // set storage version in order to enable partial content for seeking in videos
        StorageApiVersionSetter.SetDefaultServiceVersion(TerraformOutput.MediaStorageConnectionString);
        
        return Task.FromResult(true);
    }
}
