namespace Rili.Debug.Shell
{
    public enum UnishCommandTokenType
    {
        Invalid,
        Param,
        OptionLong,
        OptionShort,
        RedirectIn,
        RedirectOut,
        RedirectOutAppend,
        RedirectErr,
        RedirectErrAppend,
        Pipe,
        Separate,
    }
}
