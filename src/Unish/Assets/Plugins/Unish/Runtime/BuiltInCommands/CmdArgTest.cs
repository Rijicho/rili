using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    internal class CmdArgTest : UnishCommandBase
    {
        public override string[] Ops { get; } =
        {
            "argtest",
        };

        public override (UnishVariableType type, string name, string defVal, string info)[] Params { get; } =
        {
            (UnishVariableType.String, "no-default", null, "required parameter"),
            (UnishVariableType.String, "empty-default", "", "not-required parameter with empty default value"),
            (UnishVariableType.String, "string", "hoge", "text param"),
            (UnishVariableType.Bool, "bool", "false", "bool param"),
            (UnishVariableType.Int, "int", "57", "int param"),
            (UnishVariableType.Float, "float", "2.71828", "float param"),
            (UnishVariableType.Vector2, "vector2", "[0.1,1]", "Vector2 param"),
            (UnishVariableType.Vector3, "vector3", "[1,10,100]", "Vector3 param"),
            (UnishVariableType.Color, "color", "red", "Color param"),
            (UnishVariableType.Array, "array", "( foo bar  )", "Array param"),
        };

        public override (UnishVariableType type, string name, string sName, string defVal, string info)[] Options { get; } =
        {
            (UnishVariableType.Unit, "unit", "u", null, "unit option, no argument"),
            (UnishVariableType.String, "string", "s", "fuga", "string option"),
            (UnishVariableType.Bool, "bool", "b", "true", "bool option"),
            (UnishVariableType.Int, "int", "i", "42", "int option"),
            (UnishVariableType.Float, "float", "f", "3.14", "float option"),
            (UnishVariableType.Vector2, "vector2", "v", "[1,2]", "Vector2 option"),
            (UnishVariableType.Vector3, "vector3", "w", "[1.2,3.4,5.6]", "Vector3 option"),
            (UnishVariableType.Color, "", "c", "#33ccffaa", "Color option without long name"),
            (UnishVariableType.Array, "array", "", "(hoge fuga piyo nyan )", "Array option without short name"),
            (UnishVariableType.String, "empty-default", "e", "", "empty default value"),
            (UnishVariableType.String, "no-default", "n", null, "argument required"),
        };

        protected override async UniTask Run(Dictionary<string, UnishVariable> args,
            Dictionary<string, UnishVariable> options)
        {
            await IO.WriteLineAsync($"$# = {args["#"].S}");
            for (var i = 0; i <= args["#"].I; i++)
            {
                await IO.WriteLineAsync($"${i} = {args[$"{i}"].S}");
            }

            await IO.WriteLineAsync($"$- = {args["-"].S}");
            await IO.WriteLineAsync($"$@ = {args["@"].S}");
            await IO.WriteLineAsync($"$* = {args["*"].S}");

            foreach (var a in args)
            {
                await IO.WriteLineAsync($"args[{a.Key}]={a.Value.S}");
            }

            foreach (var o in options)
            {
                await IO.WriteLineAsync($"options[{o.Key}]={o.Value.S}");
            }
        }
    }
}

