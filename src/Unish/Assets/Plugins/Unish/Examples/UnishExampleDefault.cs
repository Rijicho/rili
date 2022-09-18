using UnityEngine;

namespace Rili.Debug.Shell.Example
{
    public class UnishExampleDefault : MonoBehaviour
    {
        private Unish mShell;

        private void Start()
        {
            new Unish().Run();
        }
    }
}
