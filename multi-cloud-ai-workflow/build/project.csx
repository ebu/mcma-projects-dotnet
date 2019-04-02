#load "task.csx"
#load "build.csx"
#load "aggregate-task.csx"
#load "file-changes.csx"
#load "copy-files.csx"
#load "delete-files.csx"
#load "dotnet-cli.csx"
#load "zip.csx"

using System.Text.RegularExpressions;

public class CheckProjectForChanges : BuildTask
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
        var checks = new List<IBuildTask> {new CheckForFileChanges(Project, $"{Project}/dist/lambda.zip", BinFolderRegex, ObjFolderRegex)};

        checks.AddRange(
            Directory.EnumerateFiles(Project, "*.csproj")
                .SelectMany(GetProjectDependencies).Distinct()
                .Select(projFile => new CheckForFileChanges(new FileInfo(projFile).Directory.FullName, $"{Project}/dist/lambda.zip", BinFolderRegex, ObjFolderRegex)));

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

public class BuildProject : BuildTask
{
    private const string DistFolder = "dist";
    private const string StagingFolder = DistFolder + "/staging";

    public BuildProject(string project, bool build = true, bool clean = true)
    {
        Project = project;
        DistFullPath = $"{project}/{DistFolder}";
        StagingFullPath = $"{project}/{StagingFolder}";

        if (clean)
            Clean = new DeleteFiles(StagingFullPath);

        if (build)
        {
            CheckForSourceChanges = new CheckProjectForChanges(project);

            ProjectBuild =
                new AggregateTask(
                    DotNetCli.Clean(project, StagingFolder),
                    DotNetCli.Publish(project, StagingFolder));
        }
        else
            CheckForOutputChanges = new CheckForFileChanges(StagingFullPath, $"{DistFullPath}/lambda.zip", "\\.json");
        
        Zip = new ZipTask(StagingFullPath, $"{DistFullPath}/lambda.zip");
    }

    private string Project { get; }

    private string DistFullPath { get; }

    private string StagingFullPath { get; }

    private IBuildTask CheckForSourceChanges { get; }

    private IBuildTask CheckForOutputChanges { get; }

    private IBuildTask Clean { get; }

    private IBuildTask ProjectBuild { get; }

    public IDictionary<string, string> PostBuildCopies { get; } = new Dictionary<string, string>();

    public ZipTask Zip { get; }

    protected override async Task<bool> ExecuteTask()
    {
        if (CheckForSourceChanges == null || await CheckForSourceChanges.Run())
        {
            if (Clean != null)
                await Clean.Run();

            if (ProjectBuild != null)
                await ProjectBuild.Run();

            foreach (var postBuildCopy in PostBuildCopies.Select(kvp => new CopyFiles(Path.Combine(Project, kvp.Key), Path.Combine(StagingFullPath, kvp.Value))))
                await postBuildCopy.Run();

            if (CheckForSourceChanges != null || await CheckForOutputChanges.Run())
            {
                Console.WriteLine($"Zipping project {Project} output...");
                await Zip.Run();
            }
            else
                Console.WriteLine($"Zip file for project {Project} is up-to-date.");
        }
        else
            Console.WriteLine($"Project {Project} is up-to-date.");

        return true;
    }
}