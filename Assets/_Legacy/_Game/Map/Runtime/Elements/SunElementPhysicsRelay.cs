#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    public sealed class SunElementPhysicsRelay : MonoBehaviour
    {
        [SerializeField] private SunElementDriver driver;

        public void Configure(SunElementDriver sunDriver) => driver = sunDriver;

        private void OnTriggerStay2D(Collider2D other) => driver?.NotifyTriggerStay(other);
    }
}

#endif
