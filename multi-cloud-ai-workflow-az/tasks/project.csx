#load "task.csx"
#load "task-runner.csx"
#load "aggregate-task.csx"
#load "file-changes.csx"
#load "copy-files.csx"
#load "delete-files.csx"
#load "dotnet-cli.csx"
#load "zip.csx"

using System.Text.RegularExpressions;

public class CheckProjectForChanges : TaskBase
{
    public CheckProjectForChanges(string project)
    {
        Project = project;
    }

    private string Project { get; }

    private const string BinFolderRegex = "[\\\\|\\/]bin[\\\\|\\/]";
    private const string ObjFolderRegex = "[\\\\|\\/]obj[\\\\|\\/]";

    protected override async Task<bool> ExecuteTask()
    {
        var checks = new List<ITask> {new CheckForFileChanges(Project, $"{Project}/dist/function.zip", BinFolderRegex, ObjFolderRegex)};

        checks.AddRange(
            Directory.EnumerateFiles(Project, "*.csproj")
                .SelectMany(GetProjectDependencies).Distinct()
                .Select(projFile => new CheckForFileChanges(new FileInfo(projFile).Directory.FullName, $"{Project}/dist/function.zip", BinFolderRegex, ObjFolderRegex)));

        foreach (var check in checks)
            if (await check.Run())
                return true;

        return false;
    }
    
    private string[] GetProjectDependencies(string projFile)
    {
        var dependencies = new List<string>();

        var projFileContents = File.ReadAllText(projFile);
        var projFileInfo = new FileInfo(projFile);

        foreach (Match match in Regex.Matches(projFileContents, "\\<ProjectReference Include=\"(.+)\" \\/\\>"))
        {
            var capture = match.Groups.OfType<Group>().Skip(1).FirstOrDefault()?.Captures.OfType<Capture>().FirstOrDefault();
            if (capture == null)
                continue;

            var dependencyProjFile = new Uri(Path.Combine(projFileInfo.Directory.FullName, capture.Value.Replace("\\", "/"))).AbsolutePath;

            dependencies.AddRange(GetProjectDependencies(dependencyProjFile));
            
            dependencies.Add(dependencyProjFile);
        }

        return dependencies.ToArray();
    }
}

public class BuildProject : TaskBase
{
    private const string DistFolder = "dist";
    private const string StagingFolder = DistFolder + "/staging";

    public BuildProject(string projectFolder, bool build = true, bool clean = true)
    {
        ProjectFolder = projectFolder;
        DistFullPath = $"{projectFolder}/{DistFolder}";
        StagingFullPath = $"{projectFolder}/{StagingFolder}";
        OutputZipFile = $"{DistFullPath}/function.zip";

        if (clean)
            Clean = new DeleteFiles(StagingFullPath);

        if (build)
        {
            CheckForSourceChanges = new CheckProjectForChanges(projectFolder);

            ProjectBuild =
                new AggregateTask(
                    DotNetCli.Clean(projectFolder, StagingFolder),
                    DotNetCli.Publish(projectFolder, StagingFolder));
        }
        else
            CheckForOutputChanges = new CheckForFileChanges(StagingFullPath, OutputZipFile, StagingFolderChangeCheckExcludes);

        var csprojFiles = Directory.GetFiles(projectFolder, "*.csproj");
        if (csprojFiles.Length == 0)
            throw new Exception($"No .csproj file found in {projectFolder}.");
        if (csprojFiles.Length > 1)
            throw new Exception($"More than 1 .csproj file found in {projectFolder}.");

        ProjectFile = csprojFiles[0];
        ProjectName = ProjectFile.Replace(".csproj", ""); 
        
        // set json files with read permissions for all for builds on Linux boxes
        Zip = new ZipTask(StagingFullPath, OutputZipFile, externalAttributes: ZipFileExternalAttributes);
    }

    protected string ProjectFolder { get; }

    protected string ProjectFile { get; }

    protected string ProjectName { get; }

    protected string DistFullPath { get; }

    protected string OutputZipFile { get; }

    protected string StagingFullPath { get; }

    protected ITask CheckForSourceChanges { get; }

    protected ITask CheckForOutputChanges { get; }

    protected ITask Clean { get; }

    protected ITask ProjectBuild { get; }

    public IDictionary<string, string> PostBuildCopies { get; } = new Dictionary<string, string>();

    public ZipTask Zip { get; }

    protected virtual string[] StagingFolderChangeCheckExcludes { get; } = new string[] {".*.deps.json$", "extensions.json$", "function.json$"};

    protected virtual IDictionary<string, int> ZipFileExternalAttributes { get; } = new Dictionary<string, int>();

    protected override async Task<bool> ExecuteTask()
    {
        if (CheckForSourceChanges == null || await CheckForSourceChanges.Run())
        {
            if (Clean != null)
                await Clean.Run();

            if (ProjectBuild != null)
                await ProjectBuild.Run();

            foreach (var postBuildCopy in PostBuildCopies.Select(kvp => new CopyFiles(Path.Combine(ProjectFolder, kvp.Key), Path.Combine(StagingFullPath, kvp.Value))))
                await postBuildCopy.Run();

            if (CheckForSourceChanges != null || await CheckForOutputChanges.Run())
            {
                Console.WriteLine($"Zipping project {ProjectFolder} output...");
                await Zip.Run();
            }
            else
                Console.WriteLine($"Zip file for project {ProjectFolder} is up-to-date.");
        }
        else
            Console.WriteLine($"Project {ProjectFolder} is up-to-date.");

        return true;
    }
}