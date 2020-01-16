#load "../tasks/aggregate-task.csx"
#load "../tasks/file-changes.csx"
#load "../tasks/npm.csx"

public static readonly ITask BuildWebsite = new AggregateTask(
    new CheckForFileChanges("./website", "./website/dist/website"),
    Npm.Install("./website"),
    Npm.RunScript("./website", "build", "--prod", "--base-href", "./index.html", "--progress", "false"));