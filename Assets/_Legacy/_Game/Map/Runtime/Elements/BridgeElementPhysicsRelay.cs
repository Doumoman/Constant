#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    public sealed class BridgeElementPhysicsRelay : MonoBehaviour
    {
        [SerializeField] private BridgeElementDriver driver;

        public void Configure(BridgeElementDriver bridgeDriver) => driver = bridgeDriver;

        private void OnTriggerEnter2D(Collider2D other) => driver?.NotifyTriggerEnter(other);
        private void OnTriggerStay2D(Collider2D other) => driver?.NotifyTriggerStay(other);
        private void OnTriggerExit2D(Collider2D other) => driver?.NotifyTriggerExit(other);
        private void OnCollisionEnter2D(Collision2D collision) => driver?.NotifyCollisionEnter(collision);
    }
}

#endif
