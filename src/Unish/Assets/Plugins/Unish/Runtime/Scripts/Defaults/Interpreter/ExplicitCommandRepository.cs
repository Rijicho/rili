using System;
using System.Collections.Generic;
using System.Linq;

namespace Rili.Debug.Shell
{
    public sealed class ExplicitCommandRepository : ADefaultCommandRepository
    {
        private readonly IReadOnlyList<Type> mCmdTypes;

        private static Type[] builtinCmds =
        {
            typeof(CmdAlias),
            typeof(CmdCatenate),
            typeof(CmdChangeDirectory),
            typeof(CmdEcho),
            typeof(CmdExport),
            typeof(CmdGrep),
            typeof(CmdHelp),
            typeof(CmdListCommand),
            typeof(CmdListSegment),
            typeof(CmdMakeDirectory),
            typeof(CmdMan),
            typeof(CmdOpen),
            typeof(CmdPwd),
            typeof(CmdQuit),
            typeof(CmdRemove),
            typeof(CmdSet),
            typeof(CmdSource),
            typeof(CmdTouch),
        };
        
        public ExplicitCommandRepository(params Type[] userCommands)
        {
            mCmdTypes = userCommands.Concat(builtinCmds).Distinct().ToList();
        }
        protected override IEnumerable<Type> GetCommandTypes()
        {
            return mCmdTypes;
        }
    }
}
