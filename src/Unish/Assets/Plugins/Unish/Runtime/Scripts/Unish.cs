using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Pool;

namespace Rili.Debug.Shell
{
    public class Unish : Unish<UnishCore>
    {
        public Unish(
            UnishEnvSet env = default,
            IUnishTerminal terminal = default,
            IUnishInterpreter interpreter = default,
            IUnishFileSystemRoot fileSystem = default) : base(env, terminal, interpreter, fileSystem)
        {
        }
    }

    public class Unish<T>
        where T : class, IUniShell, new()
    {
        private UnishEnvSet                       mEnv;
        private IUnishTerminal                    mTerminal;
        private IUnishInterpreter                 mInterpreter;
        private IUnishFileSystemRoot              mFileSystem;
        private IUniShell                         mTerminalShell;
        private bool                              mIsUprofileExecuted;
        private Dictionary<string, UnishVariable> mUProfileBuiltinEnv;
        private Dictionary<string, UnishVariable> mUProfileGlobalEnv;

        public bool IsRunning { get; private set; }
        
        // ----------------------------------
        // public methods
        // ----------------------------------

        public Unish(
            UnishEnvSet env = default,
            IUnishTerminal terminal = default,
            IUnishInterpreter interpreter = default,
            IUnishFileSystemRoot fileSystem = default)
        {
            mEnv         = env ?? new UnishEnvSet(new BuiltinEnv(), new ExportEnv(), new ShellEnv());
            mTerminal    = terminal ?? new DefaultTerminal();
            mInterpreter = interpreter ?? new DefaultInterpreter(AllAsmSearchCommandRepository.Instance);
            mFileSystem  = fileSystem ?? new UnishFileSystemRoot();
            var fds = new UnishIOs(mTerminal.ReadLinesAsync, mTerminal.WriteAsync, mTerminal.WriteErrorAsync, mEnv.BuiltIn);
            mTerminalShell = new T
            {
                Parent      = null,
                Env         = mEnv,
                IO          = fds,
                Interpreter = mInterpreter,
                Directory   = mFileSystem,
            };
        }


        
        public void Run()
        {
            RunAsync().Forget();
        }

        public async UniTask RunAsync()
        {
            if (IsRunning)
            {
                UnityEngine.Debug.LogWarning("This Unish instance is already running.");
                return;
            }
            
            IsRunning = true;
            try
            {
                await Init();
                await mTerminalShell.RunAsync();
                await Quit();
            }
            finally
            {
                IsRunning = false;
            }
        }

        // ----------------------------------
        // protected methods
        // ----------------------------------

        protected virtual UniTask OnPreInitAsync()
        {
            return default;
        }

        protected virtual UniTask OnPostInitAsync()
        {
            return default;
        }

        protected virtual UniTask OnPreQuitAsync()
        {
            return default;
        }

        protected virtual UniTask OnPostQuitAsync()
        {
            return default;
        }

        protected virtual UniTask RunBuiltInProfile()
        {
            mEnv.BuiltIn.Set(UnishBuiltInEnvKeys.WorkingDirectory, UnishPathConstants.Root);
            if (!mEnv.BuiltIn.TryGet(UnishBuiltInEnvKeys.HomePath, out string homePath))
            {
                mEnv.BuiltIn.Set(UnishBuiltInEnvKeys.HomePath, UnishPathConstants.Root);
            }

            if (mFileSystem.TryFindEntry(homePath, out var home) && (home.IsDirectory || home.IsFileSystem))
            {
                mEnv.BuiltIn.Set(UnishBuiltInEnvKeys.WorkingDirectory, home.Path);
            }

            return default;
        }

        protected virtual async UniTask RunUserProfiles()
        {
            var profile = mEnv.BuiltIn[UnishBuiltInEnvKeys.ProfilePath].S;
            var rc      = mEnv.BuiltIn[UnishBuiltInEnvKeys.RcPath].S;
            if (!mIsUprofileExecuted)
            {
                if (mFileSystem.TryFindEntry(profile, out _))
                {
                    var defaultBuiltinEnv = mEnv.BuiltIn.ToDictionary(x => x.Key, x => x.Value);
                    var defaultGlobalEnv  = mEnv.Exported.ToDictionary(x => x.Key, x => x.Value);
                    await foreach (var c in mFileSystem.ReadLines(profile))
                    {
                        var cmd = c.Trim();
                        if (string.IsNullOrEmpty(cmd) || cmd.StartsWith("#"))
                        {
                            continue;
                        }

                        await mInterpreter.RunCommandAsync(mTerminalShell, c);
                    }

                    
                    // uprofile????????????????????????????????????????????????????????????????????????????????????
                    mUProfileBuiltinEnv = mEnv.BuiltIn.ToDictionary(x => x.Key, x => x.Value);
                    mUProfileGlobalEnv  = mEnv.Exported.ToDictionary(x => x.Key, x => x.Value);
                    
                    var toRemove = ListPool<string>.Get();
                    foreach (var e in defaultBuiltinEnv)
                    {
                        if (mUProfileBuiltinEnv.TryGetValue(e.Key, out var v) && e.Value.S == v.S && e.Value.Type == v.Type)
                        {
                            toRemove.Add(e.Key);
                        }
                    }

                    foreach (var k in toRemove)
                    {
                        mUProfileBuiltinEnv.Remove(k);
                    }
                    toRemove.Clear();
                    foreach (var e in defaultGlobalEnv)
                    {
                        if (mUProfileGlobalEnv.TryGetValue(e.Key, out var v) && e.Value.S == v.S && e.Value.Type == v.Type)
                        {
                            toRemove.Add(e.Key);
                        }
                    }

                    foreach (var k in toRemove)
                    {
                        mUProfileGlobalEnv.Remove(k);
                    }

                    ListPool<string>.Release(toRemove);
                    
                    mIsUprofileExecuted = true;
                }
            }
            else
            {
                foreach (var e in mUProfileBuiltinEnv)
                {
                    mEnv.BuiltIn[e.Key] = e.Value;
                }
                foreach (var e in mUProfileGlobalEnv)
                {
                    mEnv.Exported[e.Key] = e.Value;
                }
            }

            if (mFileSystem.TryFindEntry(rc, out _))
            {
                await foreach (var c in mFileSystem.ReadLines(rc))
                {
                    var cmd = c.Trim();
                    if (string.IsNullOrEmpty(cmd) || cmd.StartsWith("#"))
                    {
                        continue;
                    }

                    await mInterpreter.RunCommandAsync(mTerminalShell, c);
                }
            }
        }

        // ----------------------------------
        // private methods
        // ----------------------------------


        private async UniTask Init()
        {
            await OnPreInitAsync();
            await mEnv.InitializeAsync();
            await mTerminal.InitializeAsync(mEnv.BuiltIn);
            await mFileSystem.InitializeAsync(mEnv.BuiltIn);
            await mInterpreter.InitializeAsync(mEnv.BuiltIn);
            await RunBuiltInProfile();
            await RunUserProfiles();
            await OnPostInitAsync();
        }

        private async UniTask Quit()
        {
            await OnPreQuitAsync();
            await mInterpreter.FinalizeAsync();
            await mFileSystem.FinalizeAsync();
            await mTerminal.FinalizeAsync();
            await mEnv.FinalizeAsync();
            await OnPostQuitAsync();
        }
    }
}

