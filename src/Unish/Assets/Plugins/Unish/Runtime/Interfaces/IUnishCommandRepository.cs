using System.Collections.Generic;

namespace Rili.Debug.Shell
{
    public interface IUnishCommandRepository : IUnishResource
    {
        IReadOnlyDictionary<string, UnishCommandBase> Map { get; }
    }
}
