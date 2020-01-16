#load "../../../tasks/task.csx"
#load "../../../tasks/task-runner.csx"

#load "../terraform-output.csx"
#load "./website-config-uploader.csx"

public class UploadWebsiteConfig : TaskBase
{
    public UploadWebsiteConfig()
    {
        WebsiteConfigUploader = new WebsiteConfigUploader(TerraformOutput);
    }
                
    private TerraformOutput TerraformOutput { get; } = TerraformOutput.Load();

    private WebsiteConfigUploader WebsiteConfigUploader { get; }

    protected override async Task<bool> ExecuteTask()
    {
        // upload website config file generated from terraform output
        await WebsiteConfigUploader.UploadConfigAsync();
        
        return true;
    }
}