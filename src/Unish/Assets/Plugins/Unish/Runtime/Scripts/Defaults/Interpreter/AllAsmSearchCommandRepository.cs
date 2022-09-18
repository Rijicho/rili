namespace Rili.Debug.Shell
{
    public sealed class AllAsmSearchCommandRepository : AsmSearchCommandRepository
    {
        private AllAsmSearchCommandRepository()
        {
        }

        private static AllAsmSearchCommandRepository mInstance;
        public static  AllAsmSearchCommandRepository Instance => mInstance ??= new AllAsmSearchCommandRepository();
    }
}
