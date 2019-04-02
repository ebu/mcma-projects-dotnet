#load "cmd-task.csx"

public class DotNetCli : CmdTask
{
    public DotNetCli(string project, string command, params string[] additionalArgs)
        : base("dotnet", string.Join(" ", new[] {command, project, string.Join(" ", additionalArgs.Where(a => !string.IsNullOrWhiteSpace(a)))}))
    {
    }

    public static DotNetCli Clean(string project, string outputDir = null, string config = "Release")
        => new DotNetCli(project, "clean", outputDir != null ? $"-o={outputDir}" : null, $"-c={config}");

    public static DotNetCli Restore(string project)
        => new DotNetCli(project, "restore");

    public static DotNetCli Build(string project, string outputDir = null, string config = "Release")
        => new DotNetCli(project, "build", outputDir != null ? $"-o={outputDir}" : null, $"-c={config}");

    public static DotNetCli Publish(string project, string outputDir = null, string config = "Release")
        => new DotNetCli(project, "publish", outputDir != null ? $"-o={outputDir}" : null, $"-c={config}");
}