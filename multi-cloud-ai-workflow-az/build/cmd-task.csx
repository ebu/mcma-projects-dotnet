#load "task.csx"

using System.Threading.Tasks;

public class CmdTask : BuildTask
{
    public CmdTask(string cmd, params string[] args)
    {
        ProcessStartInfo = new ProcessStartInfo(cmd, string.Join(" ", args))
        {
            RedirectStandardOutput = RedirectStandardOutput,
            RedirectStandardError = RedirectStandardError
        };
    }

    protected virtual bool RedirectStandardOutput => false;

    private StringBuilder StandardOutputBuilder { get; } = new StringBuilder();

    protected string StandardOutput => StandardOutputBuilder.ToString();

    protected virtual bool RedirectStandardError => false;

    private StringBuilder StandardErrorBuilder { get; } = new StringBuilder();

    protected string StandardError => StandardErrorBuilder.ToString();

    protected ProcessStartInfo ProcessStartInfo { get; }

    protected Process Process { get; private set; }

    public string Cwd
    {
        get => ProcessStartInfo.WorkingDirectory;
        set => ProcessStartInfo.WorkingDirectory = value;
    }

    protected override Task<bool> ExecuteTask() => RunCmd();

    protected Task<bool> RunCmd()
    {
        Process = new Process {StartInfo = ProcessStartInfo};
        
        if (RedirectStandardOutput)
            Process.OutputDataReceived += OnOutputDataReceived;
        if (RedirectStandardError)
            Process.ErrorDataReceived += OnErrorDataReceived;

        Process.Start();

        if (RedirectStandardOutput)
            Process.BeginOutputReadLine();
        if (RedirectStandardError)
            Process.BeginErrorReadLine();

        return Task.Run(async () =>
        {
            Process.WaitForExit();

            if (RedirectStandardOutput)
                Process.OutputDataReceived -= OnOutputDataReceived;
            if (RedirectStandardError)
                Process.ErrorDataReceived -= OnErrorDataReceived;

            if (Process.ExitCode != 0)
                throw new Exception($"Process '{ProcessStartInfo.FileName}' exited with exit code {Process.ExitCode}.");

            return await OnExit();
        });
    }

    protected virtual void OnOutputDataReceived(object obj, DataReceivedEventArgs args)
    {
        if (args.Data != null)
            StandardOutputBuilder.AppendLine(args.Data);
    }

    protected virtual void OnErrorDataReceived(object obj, DataReceivedEventArgs args)
    {
        if (args.Data != null)
            StandardErrorBuilder.AppendLine(args.Data);
    }

    protected virtual Task<bool> OnExit() => Task.FromResult(true);
}