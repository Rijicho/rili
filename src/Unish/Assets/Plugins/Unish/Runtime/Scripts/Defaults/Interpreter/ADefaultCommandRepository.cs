using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    public abstract class ADefaultCommandRepository : IUnishCommandRepository
    {
        public           IReadOnlyDictionary<string, UnishCommandBase> Map => mMap;
        private readonly Dictionary<string, UnishCommandBase>          mMap = new();

        public virtual UniTask InitializeAsync()
        {
            mMap.Clear();
            AddCommands(GetCommandTypes());
            return default;
        }

        public virtual UniTask FinalizeAsync()
        {
            mMap.Clear();
            return default;
        }

        protected abstract IEnumerable<Type> GetCommandTypes();

        private void AddCommands(IEnumerable<Type> cmdTypes)
        {
            foreach (var t in cmdTypes)
            {
                var instance = Activator.CreateInstance(t) as UnishCommandBase;
                foreach (var op in instance.Ops)
                {
                    mMap[op] = instance;
                }

                foreach (var alias in instance.Aliases)
                {
                    mMap["@" + alias] = instance;
                }
            }
        }
    }
}
