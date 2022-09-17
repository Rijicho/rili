using UnityEngine;

namespace Rili.Debug.Shell
{
    /// <summary>
    ///     組み込み変数
    /// </summary>
    public class BuiltinEnv : EnvBase
    {
        protected override UnishVariable[] Initials { get; } =
        {
            new(UnishBuiltInEnvKeys.ProfilePath, "~/.uprofile"),
            new(UnishBuiltInEnvKeys.RcPath, "~/.unishrc"),
            new(UnishBuiltInEnvKeys.Prompt, "%d $ "),
            new(UnishBuiltInEnvKeys.CharCountPerLine, 100),
            new(UnishBuiltInEnvKeys.LineCount, 24),
            new(UnishBuiltInEnvKeys.BgColor, new Color(0, 0, 0, (float)0xcc / 0xff)),
            new(UnishBuiltInEnvKeys.Quit, false),
        };

        public override IUnishEnv Fork()
        {
            return this;
        }
    }
}
