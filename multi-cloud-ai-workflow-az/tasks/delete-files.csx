#load "task.csx"

using System.IO;
using System.Text.RegularExpressions;

public class DeleteFiles : TaskBase
{
    public DeleteFiles(string folder, string pattern = "*.*", params string[] excludes)
    {
        Folder = folder;
        Pattern = pattern;
        Excludes = excludes;
    }

    private string Folder { get; }

    private string Pattern { get; }

    private string[] Excludes { get; }

    protected override Task<bool> ExecuteTask()
    {
        var folderInfo = new DirectoryInfo(Folder);
        if (folderInfo.Exists)
        {
            foreach (var fileInfo in folderInfo.GetFiles(Pattern, SearchOption.AllDirectories))
                fileInfo.Delete();

            foreach (var subFolderInfo in folderInfo.EnumerateDirectories(Pattern, SearchOption.AllDirectories).OrderByDescending(i => i.FullName))
                if (!subFolderInfo.GetFiles(Pattern, SearchOption.AllDirectories).Any())
                    subFolderInfo.Delete();
        }

        return Task.FromResult(true);
    }
}