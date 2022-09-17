using UnityEngine;
using UnityEngine.UI;

namespace Rili.Debug.Shell
{
    public class DefaultDisplay : MonoBehaviour
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        [field: SerializeField]
        public Image Background { get; private set; }

        [field: SerializeField]
        public Text Text { get; private set; }
    }
}
