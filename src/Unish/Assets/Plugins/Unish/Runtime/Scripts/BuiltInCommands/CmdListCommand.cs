using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    public class CmdListCommand : UnishCommandBase
    {
        public override string[] Ops { get; } =
        {
            "lc",
        };

        public override (UnishVariableType type, string name, string defVal, string info)[] Params { get; } =
        {
            (UnishVariableType.String, "pattern", "", "filter by a word"),
        };

        public override (UnishVariableType type, string name, string sName, string defVal, string info)[] Options { get; } =
        {
            (UnishVariableType.String, "sort", "s", "default", "sorting type（default/name）"),
            (UnishVariableType.Unit, "detail", "d", "", "show command details"),
            (UnishVariableType.Unit, "regex", "r", "", "regard the pattern as regular expressions"),
        };

        public override string Usage(string op)
        {
            return "'lc' shows the available command list.";
        }

        protected override async UniTask Run(Dictionary<string, UnishVariable> args,
            Dictionary<string, UnishVariable> options)
        {
            var filter  = new Regex(options.ContainsKey("r") ? args["pattern"].S : $".*{args["pattern"].S}.*");
            var isFirst = true;

            IEnumerable<KeyValuePair<string, UnishCommandBase>> ls;

            if (options.ContainsKey("s") && options["s"].S == "name")
            {
                ls = Interpreter.Commands.OrderBy(x => x.Key);
            }
            else
            {
                ls = Interpreter.Commands;
            }

            foreach (var c in ls)
            {
                if (string.IsNullOrWhiteSpace(c.Key))
                {
                    continue;
                }

                if (c.Key.StartsWith("@"))
                {
                    continue;
                }

                var m = filter.Match(c.Key);
                if (m.Success && m.Value == c.Key)
                {
                    if (options.ContainsKey("d"))
                    {
                        await c.Value.WriteUsage(IO, c.Key, isFirst);
                        isFirst = false;
                    }
                    else
                    {
                        await IO.WriteLineAsync("| " + c.Key);
                    }

                    await UniTask.Yield();
                }
            }
        }
    }
}

