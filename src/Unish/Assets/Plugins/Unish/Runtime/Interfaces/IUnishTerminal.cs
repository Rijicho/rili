using System;
using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    public interface IUnishTerminal : IUnishResourceWithEnv
    {
        IUniTaskAsyncEnumerable<string> ReadLinesAsync(bool withPrompt = false);
        UniTask WriteAsync(string text);
        UniTask WriteErrorAsync(Exception error);
    }
}
