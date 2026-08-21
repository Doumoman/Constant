#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Population.P7
{
    [DefaultExecutionOrder(-950)]
    [DisallowMultipleComponent]
    public sealed class P7EconomyWallet2D : MonoBehaviour
    {
        public const int MaximumSmallNumber = 999;

        [SerializeField, Min(0)] private int gold;

        public event Action<int> GoldChanged;
        public int Gold => gold;

        public void Configure(int initialGold = 0)
        {
            SetGold(initialGold);
        }

        public int AddGold(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int before = gold;
            SetGold(gold + amount);
            return gold - before;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0 || gold < amount)
            {
                return false;
            }

            SetGold(gold - amount);
            return true;
        }

        public void ResetAtRunEnd()
        {
            SetGold(0);
        }

        private void SetGold(int value)
        {
            int next = Mathf.Clamp(value, 0, MaximumSmallNumber);
            if (next == gold)
            {
                return;
            }

            gold = next;
            GoldChanged?.Invoke(gold);
        }
    }
}

#endif
