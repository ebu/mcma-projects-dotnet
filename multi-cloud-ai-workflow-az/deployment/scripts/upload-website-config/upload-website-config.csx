#load "../../../tasks/task.csx"
#load "../../../tasks/task-runner.csx"

#load "../terraform-output.csx"
#load "./website-config-uploader.csx"

public class UploadWebsiteConfig : TaskBase
{
    protected override async Task<bool> ExecuteTask()
    {
        var websiteConfigUploader = new WebsiteConfigUploader(TerraformOutput.Instance);

        // upload website config file generated from terraform output
        await websiteConfigUploader.UploadConfigAsync();
        
        return true;
    }
}