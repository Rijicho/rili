using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;

namespace Rili.Debug.Shell
{
    public abstract class UnishCommandBase
    {
        private          IUnishProcess        mShell;
        protected        UnishEnvSet          Env         => mShell?.Env;
        protected        UnishIOs             IO          => mShell?.IO;
        protected        IUnishFileSystemRoot Directory   => mShell?.Directory;
        protected        IUnishInterpreter    Interpreter => mShell?.Interpreter;
        internal virtual bool                 IsBuiltIn   => false;
        public abstract  string[]             Ops         { get; }

        public virtual string[] Aliases { get; } =
        {
        };

        public abstract (UnishVariableType type, string name, string defVal, string info)[] Params { get; }

        public virtual (UnishVariableType type, string name, string sName, string defVal, string info)[] Options { get; } =
        {
        };

        public virtual string Usage(string op)
        {
            return "";
        }

        protected UniTask RunNewCommandAsync(string cmd)
        {
            return Interpreter.RunCommandAsync(mShell, cmd);
        }

        protected abstract UniTask Run(
            Dictionary<string, UnishVariable> args,
            Dictionary<string, UnishVariable> options);

        public UniTask Run(IUnishProcess shell,
            Dictionary<string, UnishVariable> args,
            Dictionary<string, UnishVariable> options)
        {
            mShell = shell;
            return Run(args, options);
        }

        public UniTask WriteUsage(UnishIOs io, bool drawTopLine = true, bool drawBottomLine = true)
        {
            return WriteUsageInternal(io, Ops[0], drawTopLine, drawBottomLine);
        }

        public UniTask WriteUsage(UnishIOs io, string op, bool drawTopLine = true, bool drawBottomLine = true)
        {
            return WriteUsageInternal(io, op ?? Ops[0], drawTopLine, drawBottomLine);
        }

        protected UniTask WriteUsage(bool drawTopLine = true, bool drawBottomLine = true)
        {
            return WriteUsage(Ops[0], drawTopLine, drawBottomLine);
        }

        protected UniTask WriteUsage(string op, bool drawTopLine = true, bool drawBottomLine = true)
        {
            return WriteUsageInternal(mShell.IO, op, drawTopLine, drawBottomLine);
        }

        private async UniTask WriteUsageInternal(UnishIOs io, string op, bool drawTopLine, bool drawBottomLine)
        {
            if (drawTopLine)
            {
                await io.WriteLineAsync("+-----------------------------+", "#aaaaaa");
            }

            var optionString = Options?.Length > 0 ? "[<color=#ff7777>options</color>] " : "";
            var argString    = Params.ToSingleString(" ", toString: x => $"<color=#77ff77>{x.name}</color>");

            await io.WriteLineAsync($"<color=yellow>{op}</color> {optionString}{argString}");

            var sb = new StringBuilder();

            var typeNameWidth = 0;
            var argNameWidth  = 0;
            foreach (var (type, name, _, _) in Params)
            {
                if (type.ToString().Length > typeNameWidth)
                {
                    typeNameWidth = type.ToString().Length;
                }

                if (name.Length > argNameWidth)
                {
                    argNameWidth = name.Length;
                }
            }

            if (Params.Length > 0 && Params.Any(p => !string.IsNullOrEmpty(p.info)))
            {
                await io.WriteLineAsync("<color=#aaaaaa>params:</color>");
                foreach (var p in Params)
                {
                    var line =
                        $" <color=#aaaaaa><color=#77ff77>{p.name.PadRight(argNameWidth + 1)}</color> <color=cyan>{("<\b" + p.type + "\b>").PadRight(typeNameWidth + 4)}</color> {p.info}</color>";
                    if (p.defVal != null)
                    {
                        line +=
                            $" <color=#aaaaaa>(default: <color=#77aa77>{(string.IsNullOrWhiteSpace(p.defVal) ? $"\"{p.defVal}\"" : p.defVal)}</color>)</color>";
                    }

                    await io.WriteLineAsync(line);
                }
            }

            var flagSNameWidth = 0;
            var flagLNameWidth = 0;
            typeNameWidth = 0;
            foreach (var (type, name, sname, _, _) in Options)
            {
                if (type.ToString().Length > typeNameWidth)
                {
                    typeNameWidth = type.ToString().Length;
                }

                if (name.Length > flagLNameWidth)
                {
                    flagLNameWidth = name.Length;
                }

                if (sname.Length > flagSNameWidth)
                {
                    flagSNameWidth = sname.Length;
                }
            }

            
            if (Options?.Length > 0)
            {
                await io.WriteLineAsync("options:", "#aaaaaa");
                foreach (var option in Options)
                {
                    sb.Clear();
                    sb.Append("<color=#ff7777>");
                    if (!string.IsNullOrWhiteSpace(option.sName))
                    {
                        sb.Append($" -{option.sName.PadRight(flagSNameWidth)}");
                    }
                    else
                    {
                        sb.Append(new string(' ', flagSNameWidth + 2));
                    }

                    if (!string.IsNullOrWhiteSpace(option.name))
                    {
                        sb.Append($" --{option.name.PadRight(flagLNameWidth)}");
                    }
                    else
                    {
                        sb.Append(new string(' ', flagLNameWidth + 3));
                    }

                    sb.Append("</color>");
                    if (option.type != UnishVariableType.Unit)
                    {
                        sb.Append($" <color=cyan>{("<\b" + option.type + "\b>").PadRight(typeNameWidth+4)}</color>");
                    }
                    else
                    {
                        sb.Append(new string(' ', typeNameWidth + 3));
                    }

                    sb.Append($" {option.info}");

                    if (option.defVal != null)
                    {
                        sb.Append(
                            $" <color=#aaaaaa>(default: <color=#aa7777>{(string.IsNullOrWhiteSpace(option.defVal) ? $"\"{option.defVal}\"" : option.defVal)}</color>)</color>");
                    }

                    await io.WriteLineAsync(sb.ToString(), "#aaaaaa");
                }

                await io.WriteLineAsync("");
            }

            var usage = Usage(op);
            if (!string.IsNullOrEmpty(usage))
            {
                await io.WriteLineAsync(usage, "#aaaaaa");
            }

            if (drawBottomLine)
            {
                await io.WriteLineAsync("+-----------------------------+", "#aaaaaa");
            }
        }
    }
}

