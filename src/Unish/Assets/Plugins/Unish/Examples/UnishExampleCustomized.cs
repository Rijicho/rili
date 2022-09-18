using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rili.Debug.Shell.Example
{
    public class UnishExampleCustomized : MonoBehaviour
    {
        private void Start()
        {
            new Unish(
                // Terminal is the set of presenter/views and initial input handlers.
                terminal: new DefaultTerminal(
                    font: Resources.Load<Font>("Unish/Fonts/FiraCode/FiraCode-Regular"),
                    inputHandler: new DefaultInputHandler(DefaultTimeProvider.Instance),
                    timeProvider: DefaultTimeProvider.Instance,
                    colorParser: DefaultColorParser.Instance
                ),
                // Env is the set of initial builtin/exported/shell valiables.
                env: new UnishEnvSet(
                    // Builtin variables can be referenced in all processes.
                    // Changes in child processes are applied to parents.
                    builtIn: new BuiltinEnv(
                        new("YOUR_BUILTIN_INT_VAR", 1),
                        new("YOUR_BUILTIN_STRING_VAR", "foo"),
                        new("YOUR_BUILTIN_FLOAT_VAR", 0.1f),
                        new("YOUR_BUILTIN_BOOL_VAR", true)
                    ),
                    // Exported variables can be referenced in all child processes.
                    // Changes in child processes are NOT applied to parents.
                    export: new ExportEnv(
                        new("YOUR_EXPORTED_VECTOR2_VAR", Vector2.one),
                        new("YOUR_EXPORTED_VECTOR3_VAR", Vector3.one),
                        new("YOUR_EXPORTED_COLOR_VAR", Color.red)
                    )
                ),
                // Interpreter parses inputs and run commands. You can configure the command list here.
                // Note that the default command repository searches commands from all assemblies in app and this configuration is not required.
                // However it is recommended to explicitly set commands or assemblies here for better initialization performance.
                interpreter: new DefaultInterpreter(
                    // You can set commands explicitly
                    cmdRepo: new ExplicitCommandRepository(
                        // Set user-defined commands. (Builtin commands will be added automatically)
                        typeof(ExtraCommand)
                    )
                    // Or auto-search commands in specified assemblies
                    /*
                    cmdRepo: new AsmSearchCommandRepository(
                        // Set assemblies in whitch the repository searches for user-defined commands
                        // (Builtin commands' assembly will be added automatically)
                        typeof(ExtraCommand).Assembly      
                    )
                    */
                )
            ).Run();
        }
    }

    // You can define any extra commands as follows:
    public sealed class ExtraCommand : UnishCommandBase
    {
        // [Required] Define command name here
        public override string[] Ops { get; } =
        {
            "cmd-name",
        };

        // [Optional] Define command aliases here
        public override string[] Aliases { get; } =
        {
            "cn",
        };

        public override (UnishVariableType type, string name, string defVal, string info)[] Params { get; } =
        {
            (UnishVariableType.String, "argument-name", null, "Write description for this argument here"),
            (UnishVariableType.Int, "optional-arg", "1", "This argument is optional"),
        };

        public override (UnishVariableType type, string name, string sName, string defVal, string info)[] Options { get; } =
        {
            (UnishVariableType.Unit, "flag-name", "f", null, "Write description for this flag here"),
            (UnishVariableType.String, "required-flag", "r", null, "Non-unit flags without a default value. It forces users to set this flag explicitly."),
            (UnishVariableType.Int, "non-required-flag", "n", "1", "Non-unit flags with a default value."),
        };

        protected override async UniTask Run(Dictionary<string, UnishVariable> args,
            Dictionary<string, UnishVariable> options)
        {
            var arg0 = args["argument-name"].S;
            var arg1 = args["optional-arg"].I;
            await IO.WriteLineAsync($"The input argument is \"{arg0}\", {arg1}.");

            if (options.ContainsKey("f"))
            {
                await IO.WriteLineAsync($"The flag [-f | --flag] is set.");
            }

            await IO.WriteLineAsync($"The flag [-r | --required-flag] has an argument: {options["r"].S}");
            await IO.WriteLineAsync($"The flag [-n | --non-required-flag] has an argument: {options["non-required-flag"].I}");
        }

        public override string Usage(string op)
        {
            return "This is a user-defined command.";
        }
    }
}
