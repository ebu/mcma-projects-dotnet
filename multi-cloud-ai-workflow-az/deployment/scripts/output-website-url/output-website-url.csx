#load "../../../tasks/task.csx"
#load "../../../tasks/task-runner.csx"

#load "../terraform-output.csx"

public class OutputWebsiteUrl : TaskBase
{
    protected override Task<bool> ExecuteTask()
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Website url:");
        Console.WriteLine();
        var curColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(TerraformOutput.Instance.WebsiteUrl);
        Console.ForegroundColor = curColor;
        Console.WriteLine();
        
        return Task.FromResult(true);
    }
}