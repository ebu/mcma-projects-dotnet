using System.Threading.Tasks;

namespace Mcma.GoogleCloud.Sample.Scripts.Common
{
    public interface IScript
    {
        Task ExecuteAsync(params string[] args);
    }
}