#load "task.csx"
#load "cmd-task.csx"

public class Terraform : CmdTask
{
    public static string Path { get; set; } = "terraform";

    public static string DefaultProjectDir { get; set; } = string.Empty;

    public Terraform(string command, params string[] additionalArgs)
        : base(Path, string.Join(" ", new[] {command, string.Join(" ", additionalArgs)}))
    {
        ProcessStartInfo.WorkingDirectory = DefaultProjectDir;
    }

    public string ProjectDir
    {
        get => ProcessStartInfo.WorkingDirectory;
        set => ProcessStartInfo.WorkingDirectory = value;
    }

    public static Terraform Init() => new Terraform("init");

    public static Terraform Plan() => new Terraform("plan");

    public static Terraform Apply() => new Terraform("apply", "-auto-approve");

    public static Terraform Destroy() => new Terraform("destroy", "-force");
}