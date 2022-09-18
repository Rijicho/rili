using System;

namespace Rili.Debug.Shell
{
    /// <summary>
    ///     環境変数
    /// </summary>
    public class ExportEnv : EnvBase
    {
        private readonly   UnishVariable[] mInitials;
        protected override UnishVariable[] Initials => mInitials;

        public ExportEnv()
        {
            mInitials = Array.Empty<UnishVariable>();
        }

        public ExportEnv(params UnishVariable[] variables)
        {
            mInitials = new UnishVariable[variables.Length];
            for (var i = 0; i < variables.Length; i++)
            {
                mInitials[i] = variables[i];
            }
        }


        public override IUnishEnv Fork()
        {
            var ret = new ExportEnv();
            foreach (var kv in this)
            {
                ret[kv.Key] = kv.Value;
            }

            return ret;
        }
    }
}
