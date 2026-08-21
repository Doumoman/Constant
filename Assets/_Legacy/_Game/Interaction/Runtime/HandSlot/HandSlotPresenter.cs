#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Interaction.HandSlot
{
    [DisallowMultipleComponent]
    public sealed class HandSlotPresenter : MonoBehaviour
    {
        [SerializeField] private Transform carrySocket;
        [SerializeField] private Collider2D[] playerColliders;

        public Transform CarrySocket => carrySocket != null ? carrySocket : transform;
        public Collider2D[] PlayerColliders => playerColliders ?? System.Array.Empty<Collider2D>();

        private void Awake()
        {
            if (playerColliders == null || playerColliders.Length == 0)
            {
                playerColliders = GetComponentsInChildren<Collider2D>(true);
            }
        }

        public bool Attach(HandSlotItemRuntime item)
        {
            return item != null && item.TryEnterHandSlot(this);
        }

        public void ConfigureForTests(Transform socket, Collider2D[] colliders = null)
        {
            carrySocket = socket;
            playerColliders = colliders ?? System.Array.Empty<Collider2D>();
        }
    }
}

#endif
