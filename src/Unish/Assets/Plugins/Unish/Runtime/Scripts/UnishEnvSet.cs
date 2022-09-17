using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    public class UnishEnvSet : IUnishResource
    {
        public readonly IUnishEnv BuiltIn;
        public readonly IUnishEnv Environment;
        public readonly IUnishEnv Shell;

        public UnishEnvSet(IUnishEnv builtIn=default, IUnishEnv global=default, IUnishEnv shell=default)
        {
            BuiltIn     = builtIn ?? new BuiltinEnv();
            Environment = global ?? new GlobalEnv();
            Shell       = shell ?? new ShellEnv();
        }

        public UnishEnvSet Fork()
        {
            return new UnishEnvSet(BuiltIn.Fork(), Environment.Fork(), Shell.Fork());
        }

        public async UniTask InitializeAsync()
        {
            await BuiltIn.InitializeAsync();
            await Environment.InitializeAsync();
            await Shell.InitializeAsync();
        }

        public async UniTask FinalizeAsync()
        {
            await Shell.FinalizeAsync();
            await Environment.FinalizeAsync();
            await BuiltIn.FinalizeAsync();
        }

        public IEnumerator<KeyValuePair<string, UnishVariable>> GetEnumerator()
        {
            foreach (var e in BuiltIn)
            {
                yield return e;
            }
            foreach (var e in Environment)
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
            return BuiltIn.ContainsKey(key) || Environment.ContainsKey(key) || Shell.ContainsKey(key);
        }
        
        public bool TryGetValue(string key, out UnishVariable result)
        {
            if (BuiltIn.TryGetValue(key, out result))
            {
                return true;
            }

            if (Environment.TryGetValue(key, out result))
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

            if (Environment.ContainsKey(key))
            {
                Environment.Remove(key);
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

