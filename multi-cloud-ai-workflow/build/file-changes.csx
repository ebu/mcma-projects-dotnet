#load "task.csx"

using System.IO;
using System.Text.RegularExpressions;

public class CheckForFileChanges : BuildTask
{
    public CheckForFileChanges(string inputFolder, string outputFile, params string[] excludes)
    {
        InputFolder = inputFolder;
        OutputPath = outputFile;
        Excludes = excludes;
    }

    private string InputFolder { get; }

    private string OutputPath { get; }

    private string[] Excludes { get; }

    protected override Task<bool> ExecuteTask()
    {
        if (!Directory.Exists(OutputPath) && !File.Exists(OutputPath))
            return Task.FromResult(true);

        var lastWriteTime =
            File.GetAttributes(OutputPath).HasFlag(FileAttributes.Directory)
                ? Directory.EnumerateFiles(OutputPath, "*.*", SearchOption.AllDirectories).Max(f => new FileInfo(f).LastWriteTime)
                : new FileInfo(OutputPath).LastWriteTime;
        
        var inputFolderInfo = new DirectoryInfo(InputFolder);
        if (!inputFolderInfo.Exists)
            throw new Exception($"Input folder {InputFolder} does not exist.");

        foreach (var fileInfo in inputFolderInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Where(f => Excludes.All(r => !Regex.IsMatch(f.FullName, r))))
            if (fileInfo.LastWriteTime > lastWriteTime)
                return Task.FromResult(true);
        
        return Task.FromResult(false);
    }
}