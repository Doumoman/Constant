#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class P5RunState2D : MonoBehaviour
    {
        public const int MaximumSmallNumber = 999;

        [SerializeField, Min(0)] private int gold;
        [SerializeField, Min(0)] private int moonCakes;

        public event Action<int> GoldChanged;
        public event Action<int> MoonCakesChanged;

        public int Gold => gold;
        public int MoonCakes => moonCakes;

        public void Configure(int initialGold = 0, int initialMoonCakes = 0)
        {
            SetGold(initialGold);
            SetMoonCakes(initialMoonCakes);
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

            if (amount == 0)
            {
                return true;
            }

            SetGold(gold - amount);
            return true;
        }

        public int AddMoonCakes(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int before = moonCakes;
            SetMoonCakes(moonCakes + amount);
            return moonCakes - before;
        }

        public bool TryConsumeMoonCake()
        {
            if (moonCakes <= 0)
            {
                return false;
            }

            SetMoonCakes(moonCakes - 1);
            return true;
        }

        public void ResetRunForTests(int initialGold = 0, int initialMoonCakes = 0)
        {
            Configure(initialGold, initialMoonCakes);
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

        private void SetMoonCakes(int value)
        {
            int next = Mathf.Clamp(value, 0, MaximumSmallNumber);
            if (next == moonCakes)
            {
                return;
            }

            moonCakes = next;
            MoonCakesChanged?.Invoke(moonCakes);
        }
    }
}

#endif
