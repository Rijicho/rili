namespace Rili.Debug.Shell
{
    public interface IUnishRealFileSystem : IUnishFileSystem
    {
        string RealRootPath { get; }
    }
}
