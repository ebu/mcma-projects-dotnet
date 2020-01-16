#r "System.IO.Compression"
#load "task.csx"

using System.IO;
using System.IO.Compression;

public class ZipTask : TaskBase
{
    public ZipTask(string sourceFolder, string destinationFile, string filter = null, IDictionary<string, int> externalAttributes = null)
    {
        SourceFolder = sourceFolder;
        DestinationFile = destinationFile;
        Filter = filter;
        ExternalAttributes = externalAttributes ?? new Dictionary<string, int>();
    }

    private string SourceFolder { get; }

    private string DestinationFile { get; }

    private string Filter { get; }

    public IDictionary<string, int> ExternalAttributes { get; }

    protected override async Task<bool> ExecuteTask()
    {
        var rootDirInfo = new DirectoryInfo(SourceFolder);
        if (!rootDirInfo.Exists)
            throw new Exception($"Source directory '{SourceFolder}' does not exist.");

        var zipFile = new FileInfo(DestinationFile);
        if (zipFile.Exists)
            zipFile.Delete();

        using (var zipArchive = new ZipArchive(zipFile.OpenWrite(), ZipArchiveMode.Create, false))
        {
            await AddEntries(zipArchive, rootDirInfo.FullName, rootDirInfo);
        }

        return true;
    }

    private async Task AddEntries(ZipArchive zipArchive, string root, FileSystemInfo fileSystemInfo)
    {
        if (fileSystemInfo is FileInfo fileInfo)
        {
            var entryPath = fileSystemInfo.FullName.Replace(root, string.Empty).TrimStart('\\').Replace("\\", "/");
            var entry = zipArchive.CreateEntry(entryPath);

            //Console.WriteLine($"Writing file {fileInfo.FullName} to archive at path {entryPath}...");

            if (ExternalAttributes.ContainsKey(entryPath))
            {
                //Console.WriteLine("Setting external attributes for zip entry " + entryPath + " to " + ExternalAttributes[entryPath]);
                entry.ExternalAttributes = ExternalAttributes[entryPath];
            }

            using (var srcFileStream = fileInfo.OpenRead())
            using (var entryStream = entry.Open())
                await srcFileStream.CopyToAsync(entryStream);
        }
        else if (fileSystemInfo is DirectoryInfo directoryInfo)
        {
            foreach (var childFileSystemInfo in directoryInfo.GetFileSystemInfos())
                await AddEntries(zipArchive, root, childFileSystemInfo);
        }
    }
}