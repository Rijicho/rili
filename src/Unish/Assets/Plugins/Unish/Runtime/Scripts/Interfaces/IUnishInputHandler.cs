namespace Rili.Debug.Shell
{
    public interface IUnishInputHandler : IUnishResource
    {
        /// <summary>
        /// CheckInputOnThisFrame checks if the specified input was performed at current frame.
        /// </summary>
        /// <param name="input">Input type</param>
        /// <returns>Input state</returns>
        bool CheckInputOnThisFrame(UnishInputType input);

        /// <summary>
        /// CurrentCharInput is updated on character inputs such as 'a'.
        /// </summary>
        char CurrentCharInput { get; }
    }
}
