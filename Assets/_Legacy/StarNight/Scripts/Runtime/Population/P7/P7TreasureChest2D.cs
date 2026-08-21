#if LEGACY_DISABLED
using System;
using StarNight.Player;
using UnityEngine;

namespace StarNight.Population.P7
{
    [DisallowMultipleComponent]
    public sealed class P7TreasureChest2D : MonoBehaviour
    {
        [SerializeField] private P7EconomyWallet2D wallet;
        [SerializeField, Range(
            P7EconomyRules.MinimumChestValue,
            P7EconomyRules.MaximumChestValue)]
        private int rewardValue = P7EconomyRules.MinimumChestValue;
        [SerializeField] private Collider2D chestCollider;
        [SerializeField] private SpriteRenderer[] closedVisuals =
            Array.Empty<SpriteRenderer>();

        public event Action<P7TreasureChest2D, int> Opened;
        public int RewardValue => rewardValue;
        public bool IsOpened { get; private set; }

        public void Configure(
            P7EconomyWallet2D targetWallet,
            int goldReward)
        {
            if (!P7EconomyRules.IsLegalChestValue(goldReward))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(goldReward),
                    goldReward,
                    "P7 treasure chests must grant 4 to 6 gold.");
            }

            wallet = targetWallet;
            rewardValue = goldReward;
            chestCollider = GetComponent<Collider2D>();
            closedVisuals = GetComponentsInChildren<SpriteRenderer>(true);
            IsOpened = false;
            SetPresentation(true);
        }

        public bool TryOpen()
        {
            if (IsOpened || wallet == null)
            {
                return false;
            }

            if (wallet.AddGold(rewardValue) != rewardValue)
            {
                return false;
            }

            IsOpened = true;
            SetPresentation(false);
            Opened?.Invoke(this, rewardValue);
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null
                && other.GetComponentInParent<PlayerInputAdapter>() != null)
            {
                TryOpen();
            }
        }

        private void OnValidate()
        {
            rewardValue = Mathf.Clamp(
                rewardValue,
                P7EconomyRules.MinimumChestValue,
                P7EconomyRules.MaximumChestValue);
        }

        private void SetPresentation(bool visible)
        {
            if (chestCollider != null)
            {
                chestCollider.enabled = visible;
            }

            for (int index = 0; index < closedVisuals.Length; index++)
            {
                if (closedVisuals[index] != null)
                {
                    closedVisuals[index].enabled = visible;
                }
            }
        }
    }
}

#endif
