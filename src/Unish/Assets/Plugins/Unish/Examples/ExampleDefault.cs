using UnityEngine;
using UnityEngine.InputSystem;

namespace Rili.Debug.Shell.Example
{
    public class ExampleDefault : MonoBehaviour
    {
        private Unish mShell;

        private void Start()
        {
            mShell = new Unish();
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
}
