using System;
using System.Linq;
using UnityEngine;

namespace Rili.Debug.Shell
{
    /// <summary>
    ///     組み込み変数
    /// </summary>
    public class BuiltinEnv : EnvBase
    {
        private readonly UnishVariable[] mRequiredBySystem =
        {
            new(UnishBuiltInEnvKeys.ProfilePath, "~/.uprofile"),
            new(UnishBuiltInEnvKeys.RcPath, "~/.unishrc"),
            new(UnishBuiltInEnvKeys.Prompt, "%d $ "),
            new(UnishBuiltInEnvKeys.CharCountPerLine, 100),
            new(UnishBuiltInEnvKeys.LineCount, 24),
            new(UnishBuiltInEnvKeys.BgColor, new Color(0, 0, 0, (float)0xcc / 0xff)),
            new(UnishBuiltInEnvKeys.Quit, false),
        };

        private readonly   UnishVariable[] mExtra;
        protected override UnishVariable[] Initials => mRequiredBySystem.Concat(mExtra).ToArray();

        public BuiltinEnv()
        {
            mExtra = Array.Empty<UnishVariable>();
        }

        public BuiltinEnv(params UnishVariable[] variables)
        {
            mExtra = new UnishVariable[variables.Length];
            for (var i = 0; i < variables.Length; i++)
            {
                mExtra[i] = variables[i];
            }
        }

        public override IUnishEnv Fork()
        {
            return this;
        }
    }
}
