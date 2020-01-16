#load "task.csx"

using System.IO;
using System.Text.RegularExpressions;

public class CopyFiles : TaskBase
{
    public CopyFiles(string inputFolder, string outputFolder, params string[] excludes)
    {
        InputFolder = inputFolder;
        OutputFolder = outputFolder;
        Excludes = excludes;
    }

    private string InputFolder { get; }

    private string OutputFolder { get; }

    private string[] Excludes { get; }

    protected override Task<bool> ExecuteTask()
    {
        var inputFolderInfo = new DirectoryInfo(InputFolder);
        if (!inputFolderInfo.Exists)
            throw new Exception($"Input folder {InputFolder} does not exist.");

        var outputFolderInfo = new DirectoryInfo(OutputFolder);
        if (!outputFolderInfo.Exists)
            outputFolderInfo.Create();
        
        foreach (var srcFileInfo in inputFolderInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Where(f => Excludes.All(r => !Regex.IsMatch(f.FullName, r))))
        {
            var destFileInfo = new FileInfo(srcFileInfo.FullName.Replace(inputFolderInfo.FullName, outputFolderInfo.FullName));

            if (!destFileInfo.Directory.Exists)
                destFileInfo.Directory.Create();

            if (destFileInfo.Exists)
                destFileInfo.Delete();

            srcFileInfo.CopyTo(destFileInfo.FullName);
        }

        return Task.FromResult(true);
    }
}