#if LEGACY_DISABLED
using System;
using StarNight.Player;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public sealed class P5GoldPickup2D : MonoBehaviour
    {
        public const int SmallGoldValue = 1;
        public const int BigGoldValue = 3;

        [SerializeField] private P5RunState2D runState;
        [SerializeField] private Transform player;
        [SerializeField, Min(1)] private int value = SmallGoldValue;
        [SerializeField, Min(0.1f)] private float pickupRadius = 0.75f;
        [SerializeField] private Collider2D pickupCollider;
        [SerializeField] private SpriteRenderer pickupRenderer;
        [SerializeField] private SpriteRenderer[] pickupRenderers =
            Array.Empty<SpriteRenderer>();

        public event Action<P5GoldPickup2D, int> Collected;

        public int Value => value;
        public bool IsCollected { get; private set; }

        public void Configure(
            P5RunState2D targetRunState,
            int goldValue,
            Transform targetPlayer = null,
            SpriteRenderer visual = null,
            float radius = 0.75f)
        {
            if (goldValue != SmallGoldValue
                && goldValue != BigGoldValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(goldValue),
                    goldValue,
                    "P5 gold pickups only support the GDD values 1 and 3.");
            }

            runState = targetRunState;
            value = goldValue;
            player = targetPlayer;
            pickupRenderer = visual != null
                ? visual
                : GetComponentInChildren<SpriteRenderer>();
            pickupRenderers =
                GetComponentsInChildren<SpriteRenderer>(true);
            pickupCollider = GetComponent<Collider2D>();
            pickupRadius = Mathf.Max(0.1f, radius);
            IsCollected = false;
            SetPresentation(true);
        }

        private void Awake()
        {
            if (runState == null)
            {
                runState = FindFirstObjectByType<P5RunState2D>();
            }

            if (pickupCollider == null)
            {
                pickupCollider = GetComponent<Collider2D>();
            }

            if (pickupRenderer == null)
            {
                pickupRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (pickupRenderers == null || pickupRenderers.Length == 0)
            {
                pickupRenderers =
                    GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        private void OnValidate()
        {
            value = value >= BigGoldValue
                ? BigGoldValue
                : SmallGoldValue;
        }

        private void Update()
        {
            if (IsCollected || player == null)
            {
                return;
            }

            float distanceSquared =
                ((Vector2)player.position - (Vector2)transform.position)
                .sqrMagnitude;
            if (distanceSquared <= pickupRadius * pickupRadius)
            {
                TryCollect();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null
                && other.GetComponentInParent<PlayerInputAdapter>() != null)
            {
                TryCollect();
            }
        }

        public bool CollectForTests()
        {
            return TryCollect();
        }

        public bool TryCollect()
        {
            if (IsCollected || runState == null)
            {
                return false;
            }

            int granted = runState.AddGold(value);
            if (granted != value)
            {
                return false;
            }

            IsCollected = true;
            SetPresentation(false);
            Collected?.Invoke(this, value);
            return true;
        }

        private void SetPresentation(bool visible)
        {
            if (pickupRenderers != null && pickupRenderers.Length > 0)
            {
                for (int index = 0; index < pickupRenderers.Length; index++)
                {
                    if (pickupRenderers[index] != null)
                    {
                        pickupRenderers[index].enabled = visible;
                    }
                }
            }
            else if (pickupRenderer != null)
            {
                pickupRenderer.enabled = visible;
            }

            if (pickupCollider != null)
            {
                pickupCollider.enabled = visible;
            }
        }
    }
}

#endif
