#load "task.csx"
#load "cmd-task.csx"
#load "path-helper.csx"

public class Npm : CmdTask
{
    public static string Path { get; set; } = "npm";

    public Npm(params string[] args)
        : base(PathHelper.Resolve(Path), args)
    {
    }

    public static ITask Install(string dir) => new Npm("install") {Cwd = dir};

    public static ITask RunScript(string dir, string script, params string[] args)
        => new Npm(new[] {"run-script", script, "--"}.Concat(args).ToArray()) {Cwd = dir};
}