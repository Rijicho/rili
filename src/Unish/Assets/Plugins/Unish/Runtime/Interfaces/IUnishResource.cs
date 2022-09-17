using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    public interface IUnishResource
    {
        UniTask InitializeAsync();
        UniTask FinalizeAsync();
    }
}
