#load "task.csx"
#load "cmd-task.csx"

public class Npm : CmdTask
{
    public static string Path { get; set; } = "npm";

    public Npm(params string[] args)
        : base(Path, args)
    {
    }

    public static IBuildTask Install(string dir) => new Npm("install") {Cwd = dir};

    public static IBuildTask RunScript(string dir, string script, params string[] args)
        => new Npm(new[] {"run-script", script, "--"}.Concat(args).ToArray()) {Cwd = dir};
}