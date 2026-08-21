#if LEGACY_DISABLED
using System;
using StarNight.Player;
using UnityEngine;

namespace StarNight.Population.P7
{
    [DisallowMultipleComponent]
    public sealed class P7GoldPickup2D : MonoBehaviour
    {
        [SerializeField] private P7EconomyWallet2D wallet;
        [SerializeField, Min(1)] private int value =
            P7EconomyRules.SmallGoldValue;
        [SerializeField] private Collider2D pickupCollider;
        [SerializeField] private SpriteRenderer[] renderers =
            Array.Empty<SpriteRenderer>();

        public event Action<P7GoldPickup2D, int> Collected;
        public int Value => value;
        public bool IsCollected { get; private set; }

        public void Configure(
            P7EconomyWallet2D targetWallet,
            int goldValue)
        {
            if (!P7EconomyRules.IsLegalLooseGoldValue(goldValue))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(goldValue),
                    goldValue,
                    "P7 loose gold must use value 1 or 3.");
            }

            wallet = targetWallet;
            value = goldValue;
            pickupCollider = GetComponent<Collider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            IsCollected = false;
            SetPresentation(true);
        }

        public bool TryCollect()
        {
            if (IsCollected || wallet == null)
            {
                return false;
            }

            if (wallet.AddGold(value) != value)
            {
                return false;
            }

            IsCollected = true;
            SetPresentation(false);
            Collected?.Invoke(this, value);
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null
                && other.GetComponentInParent<PlayerInputAdapter>() != null)
            {
                TryCollect();
            }
        }

        private void OnValidate()
        {
            value = value >= P7EconomyRules.BigGoldValue
                ? P7EconomyRules.BigGoldValue
                : P7EconomyRules.SmallGoldValue;
        }

        private void SetPresentation(bool visible)
        {
            if (pickupCollider != null)
            {
                pickupCollider.enabled = visible;
            }

            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].enabled = visible;
                }
            }
        }
    }
}

#endif
