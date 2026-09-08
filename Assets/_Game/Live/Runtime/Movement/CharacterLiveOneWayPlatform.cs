using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Character.Live.Movement
{
    /// <summary>
    /// RMAP04 top-only platform marker.  The component keeps the authored
    /// physical Collider2D addressable so a Player can ignore only its chosen
    /// drop-through target; it is intentionally never a Grab-safe surface.
    /// </summary>
    public sealed class CharacterLiveOneWayPlatform : MonoBehaviour
    {
        private static readonly Dictionary<int, CharacterLiveOneWayPlatform> ByColliderId =
            new Dictionary<int, CharacterLiveOneWayPlatform>();

        [SerializeField] private Collider2D platformCollider;

        public Collider2D PlatformCollider { get { return platformCollider; } }
        public float TopWorldY
        {
            get { return platformCollider != null ? platformCollider.bounds.max.y : transform.position.y; }
        }

        public void Configure(Collider2D collider)
        {
            Unregister();
            platformCollider = collider;
            Register();
        }

        public static bool TryFind(int colliderId, out CharacterLiveOneWayPlatform platform)
        {
            return ByColliderId.TryGetValue(colliderId, out platform);
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void Register()
        {
            if (platformCollider != null)
            {
                ByColliderId[platformCollider.GetInstanceID()] = this;
            }
        }

        private void Unregister()
        {
            if (platformCollider != null)
            {
                ByColliderId.Remove(platformCollider.GetInstanceID());
            }
        }
    }
}
