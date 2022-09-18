using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rili.Debug.Shell
{
    internal class CmdOpen : UnishCommandBase
    {
        public override string[] Ops { get; } =
        {
            "open",
        };

        public override (UnishVariableType type, string name, string defVal, string info)[] Params { get; } =
        {
            (UnishVariableType.String, "path", null, "URL or file path to open"),
        };

        protected override UniTask Run(Dictionary<string, UnishVariable> args,
            Dictionary<string, UnishVariable> options)
        {
            var path = args["path"].S;

            if (IsValidUrlPath(path))
            {
                Application.OpenURL(path);
                return default;
            }

            if (Directory.TryFindEntry(path, out var _))
            {
                Directory.Open(path);
                return default;
            }

            if (IsValidUrlPath("https://" + path))
            {
                Application.OpenURL("https://" + path);
                return default;
            }

            return IO.Err(new ArgumentException("Target not found: " + path));
        }

        public override string Usage(string op)
        {
            return "'open' opens specified URL or file by your default application.";
        }


        private static bool IsValidUrlPath(string path)
        {
            return Uri.TryCreate(path, UriKind.Absolute, out var result) && (result.Scheme == Uri.UriSchemeHttps || result.Scheme == Uri.UriSchemeHttp);
        }
    }
}
