using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rili.Debug.Shell.Example
{
    public class ExampleCustomized : MonoBehaviour
    {
        private Unish mShell;

        private void Start()
        {
            mShell = new CustomizedUnish();
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame && !mShell.IsRunning)
            {
                mShell.Reset();
                mShell.Run();
            }
        }
    }

    public sealed class CustomizedUnish : Unish
    {
        public CustomizedUnish() : base(
            // terminal is the set of presenter/views and initial controllers
            terminal: new CustomizedTerminal(),
            // env is the set of initial builtin/environment/shell valiables
            env: new UnishEnvSet(builtIn: new CustomizedBuiltinEnv()),
            // interpreter parses inputs and run commands 
            interpreter: new CustomizedInterpreter()
        )
        {
        }
    }

    public sealed class CustomizedBuiltinEnv : BuiltinEnv
    {
        // You can set extra builtin environment variables
        protected override UnishVariable[] Initials => base.Initials.Concat(new UnishVariable[]
        {
            new("YOUR_BUILTIN_INT_VAR", 1),
            new("YOUR_BUILTIN_STRING_VAR", "foo"),
            new("YOUR_BUILTIN_FLOAT_VAR", 0.1f),
            new("YOUR_BUILTIN_BOOL_VAR", true),
            new("YOUR_BUILTIN_VECTOR2_VAR", Vector2.one),
            new("YOUR_BUILTIN_VECTOR3_VAR", Vector3.one),
            new("YOUR_BUILTIN_COLOR_VAR", Color.red),
        }).ToArray();
    }


    public sealed class CustomizedTerminal : DefaultTerminal
    {
        public CustomizedTerminal() : base(
            font: Resources.Load<Font>("Unish/Fonts/FiraCode/FiraCode-Regular"),
            inputHandler: new DefaultInputHandler(DefaultTimeProvider.Instance),
            timeProvider: DefaultTimeProvider.Instance,
            colorParser: DefaultColorParser.Instance
        )
        {
        }
    }

    public sealed class CustomizedInterpreter : DefaultInterpreter
    {
        public CustomizedInterpreter() : base(CustomizedCmdRepo.Instance)
        {
        }
    }

    public sealed class CustomizedCmdRepo : DefaultCommandRepository
    {
        private CustomizedCmdRepo()
        {
        }

        private static    CustomizedCmdRepo mInstance;
        public new static CustomizedCmdRepo Instance => mInstance ??= new CustomizedCmdRepo();

        protected override IEnumerable<Assembly> GetDomainAssemblies()
        {
            // Provide all assembries that include commands.
            // NOTE:
            //   The default command repository returns AppDomain.CurrentDomain.GetAssemblies().
            //   Therefore this override is not required for extra commands, but recommended for performance tuning.
            yield return typeof(UnishCommandBase).Assembly; // Assembly for the builtin commands
            yield return typeof(ExtraCommand).Assembly;     // for extra commands
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
