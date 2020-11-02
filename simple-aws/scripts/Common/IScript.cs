using System.Threading.Tasks;

public interface IScript
{
    Task ExecuteAsync(params string[] args);
}