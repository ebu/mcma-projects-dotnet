public interface ITask
{
    Task<bool> Run();
}

public abstract class TaskBase : ITask
{
    public async Task<bool> Run()
    {
        try
        {
            return await ExecuteTask();
        }
        catch (Exception e)
        {
            throw new Exception($"An exception occurred running TaskBase {ToString()}. See inner exception for details.", e);
        }
    }

    protected abstract Task<bool> ExecuteTask();
}