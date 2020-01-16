#load "task.csx"

public class AggregateTask : TaskBase
{
    public AggregateTask(params ITask[] tasks)
    {
        Tasks = new List<ITask>(tasks);
    }

    private List<ITask> Tasks { get; }

    public AggregateTask Add(ITask task)
    {
        Tasks.Add(task);
        return this;
    }

    protected override async Task<bool> ExecuteTask()
    {
        foreach (var task in Tasks)
            if (!await task.Run())
                break;
        
        return true;
    }
}