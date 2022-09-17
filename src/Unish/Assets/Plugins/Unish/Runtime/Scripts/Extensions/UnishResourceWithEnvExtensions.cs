using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    public static class UnishResourceWithEnvExtensions
    {
        public static UniTask InitializeAsync(this IUnishResourceWithEnv resource, IUnishEnv builtInEnv)
        {
            resource.BuiltInEnv = builtInEnv;
            return resource.InitializeAsync();
        }
    }
}
