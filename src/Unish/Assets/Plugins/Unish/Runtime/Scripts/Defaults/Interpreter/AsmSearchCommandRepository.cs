using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rili.Debug.Shell
{
    public class AsmSearchCommandRepository : ADefaultCommandRepository
    {
        private HashSet<Assembly> mAsmCache;
        private List<Type>        mCommandTypesCache;

        public AsmSearchCommandRepository()
        {
            Cache(AppDomain.CurrentDomain.GetAssemblies());
        }

        public AsmSearchCommandRepository(params Assembly[] userAssemblies)
        {
            Cache(userAssemblies.Append(typeof(UnishCommandBase).Assembly).Distinct());
        }

        private void Cache(IEnumerable<Assembly> asms)
        {
            mAsmCache          = asms.ToHashSet();
            mCommandTypesCache = mAsmCache.SelectMany(asm => asm.GetTypes().Where(UnishCommandUtils.IsValidCommandType)).ToList();
        }

        protected sealed override IEnumerable<Type> GetCommandTypes()
        {
            return mCommandTypesCache;
        }
    }
}
