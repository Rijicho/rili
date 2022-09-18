using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    public class UnishEnvSet : IUnishResource
    {
        /// <summary>
        /// Builtin variables can be referenced in all processes.
        /// Changes in child processes are applied to parents.
        /// </summary>
        public readonly IUnishEnv BuiltIn;

        /// <summary>
        /// Exported variables can be referenced in all child processes.
        /// Changes in child processes are NOT applied to parents.
        /// </summary>
        public readonly IUnishEnv Exported;

        /// <summary>
        /// Shell variables cannot be referenced in any other processes.
        /// </summary>
        public readonly IUnishEnv Shell;

        public UnishEnvSet(IUnishEnv builtIn = default, IUnishEnv export = default, IUnishEnv shell = default)
        {
            BuiltIn  = builtIn ?? new BuiltinEnv();
            Exported = export ?? new ExportEnv();
            Shell    = shell ?? new ShellEnv();
        }

        public UnishEnvSet Fork()
        {
            return new UnishEnvSet(BuiltIn.Fork(), Exported.Fork(), Shell.Fork());
        }

        public async UniTask InitializeAsync()
        {
            await BuiltIn.InitializeAsync();
            await Exported.InitializeAsync();
            await Shell.InitializeAsync();
        }

        public async UniTask FinalizeAsync()
        {
            await Shell.FinalizeAsync();
            await Exported.FinalizeAsync();
            await BuiltIn.FinalizeAsync();
        }

        public IEnumerator<KeyValuePair<string, UnishVariable>> GetEnumerator()
        {
            foreach (var e in BuiltIn)
            {
                yield return e;
            }

            foreach (var e in Exported)
            {
                yield return e;
            }

            foreach (var e in Shell)
            {
                yield return e;
            }
        }

        public bool ContainsKey(string key)
        {
            return BuiltIn.ContainsKey(key) || Exported.ContainsKey(key) || Shell.ContainsKey(key);
        }

        public bool TryGetValue(string key, out UnishVariable result)
        {
            if (BuiltIn.TryGetValue(key, out result))
            {
                return true;
            }

            if (Exported.TryGetValue(key, out result))
            {
                return true;
            }

            if (Shell.TryGetValue(key, out result))
            {
                return true;
            }

            result = UnishVariable.Unit(key);
            return false;
        }

        public bool TryRemove(string key)
        {
            if (BuiltIn.ContainsKey(key))
            {
                BuiltIn.Remove(key);
                return true;
            }

            if (Exported.ContainsKey(key))
            {
                Exported.Remove(key);
                return true;
            }

            if (Shell.ContainsKey(key))
            {
                Shell.Remove(key);
                return true;
            }

            return false;
        }
    }
}
