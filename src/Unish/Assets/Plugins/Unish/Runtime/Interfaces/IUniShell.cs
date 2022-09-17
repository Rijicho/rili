using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    public interface IUniShell : IUnishProcess
    {
        UniTask RunAsync();
    }
}
