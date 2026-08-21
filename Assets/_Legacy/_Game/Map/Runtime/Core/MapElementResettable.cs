#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    public sealed class MapElementResettable : MonoBehaviour
    {
        [SerializeField] private MapElementInstance element;

        private ElementSnapshot baseline;

        private void Awake()
        {
            element = element != null ? element : GetComponent<MapElementInstance>();
            CaptureBaseline();
        }

        public void CaptureBaseline()
        {
            if (element != null)
            {
                baseline = element.CaptureSnapshot();
            }
        }

        public bool ResetToBaseline()
        {
            return element != null && baseline != null && element.RestoreSnapshot(baseline);
        }
    }
}

#endif
