namespace Rili.Debug.Shell
{
    public interface IUnishResourceWithEnv : IUnishResource
    {
        IUnishEnv BuiltInEnv { set; }
    }
}
