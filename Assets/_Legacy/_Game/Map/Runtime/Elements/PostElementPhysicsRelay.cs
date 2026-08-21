#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    public sealed class PostElementPhysicsRelay : MonoBehaviour
    {
        [SerializeField] private PostElementDriver driver;

        public void Configure(PostElementDriver postDriver) => driver = postDriver;

        private void OnTriggerEnter2D(Collider2D other) => driver?.NotifyTriggerEnter(other);
        private void OnTriggerStay2D(Collider2D other) => driver?.NotifyTriggerStay(other);
        private void OnTriggerExit2D(Collider2D other) => driver?.NotifyTriggerExit(other);
    }
}

#endif
