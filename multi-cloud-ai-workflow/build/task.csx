public interface IBuildTask
{
    Task<bool> Run();
}

public abstract class BuildTask : IBuildTask
{
    public async Task<bool> Run()
    {
        try
        {
            return await ExecuteTask();
        }
        catch (Exception e)
        {
            throw new Exception($"An exception occurred running BuildTask {ToString()}. See inner exception for details.", e);
        }
    }

    protected abstract Task<bool> ExecuteTask();
}