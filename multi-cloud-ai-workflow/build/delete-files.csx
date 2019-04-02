#load "task.csx"

using System.IO;
using System.Text.RegularExpressions;

public class DeleteFiles : BuildTask
{
    public DeleteFiles(string folder, params string[] excludes)
    {
        Folder = folder;
        Excludes = excludes;
    }

    private string Folder { get; }

    private string[] Excludes { get; }

    protected override Task<bool> ExecuteTask()
    {
        var folderInfo = new DirectoryInfo(Folder);
        if (folderInfo.Exists)
        {
            foreach (var fileInfo in folderInfo.GetFiles("*.*", SearchOption.AllDirectories))
                fileInfo.Delete();

            foreach (var subFolderInfo in folderInfo.EnumerateDirectories("*.*", SearchOption.AllDirectories).OrderByDescending(i => i.FullName))
                if (!subFolderInfo.GetFiles("*.*", SearchOption.AllDirectories).Any())
                    subFolderInfo.Delete();
        }

        return Task.FromResult(true);
    }
}