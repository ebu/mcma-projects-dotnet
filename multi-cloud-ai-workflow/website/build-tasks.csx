#load "../build/aggregate-task.csx"
#load "../build/file-changes.csx"
#load "../build/npm.csx"

public static readonly IBuildTask BuildWebsite = new AggregateTask(
    new CheckForFileChanges("./website", "./website/dist/website"),
    Npm.Install("./website"),
    Npm.RunScript("./website", "build", "--prod", "--base-href", "./index.html", "--progress", "false"));