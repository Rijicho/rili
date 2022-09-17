using UnityEngine;

namespace Rili.Debug.Shell
{
    public class DefaultTimeProvider : IUnishTimeProvider
    {
        private DefaultTimeProvider()
        {
        }

        private static DefaultTimeProvider mInstance;

        public static DefaultTimeProvider Instance => mInstance ??= new DefaultTimeProvider();

        public float Now => Time.unscaledTime;
    }
}
