#if LEGACY_DISABLED
using StarNight.Core.Player;
using UnityEngine;

namespace StarNight.Player.Safety
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PlayerSafeCell2D : MonoBehaviour
    {
        [SerializeField] private SafeCellState state;

        public SafeCellState State => state;

        private void Awake()
        {
            EnsureTrigger();
            if (!state.IsValid)
            {
                state = SafeCellState.FromPlayerCenter(transform.position);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerOutOfBoundsGuard guard = other.GetComponentInParent<PlayerOutOfBoundsGuard>();
            if (guard != null)
            {
                guard.SetSafeCell(state.IsValid
                    ? state
                    : SafeCellState.FromPlayerCenter(transform.position));
            }
        }

        public void Configure(Vector2 playerCenter)
        {
            state = SafeCellState.FromPlayerCenter(playerCenter);
            EnsureTrigger();
        }

        private void EnsureTrigger()
        {
            BoxCollider2D trigger = GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = Vector2.one * 0.8f;
        }
    }
}

#endif
