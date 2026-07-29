using System;
using UnityEngine;

namespace StarNight.Rewrite.Core
{
    [Serializable]
    public sealed class RunLoadout
    {
        public const int RopeCapacity = 6;
        public const int BombCapacity = 6;
        public const int StartingRopes = 3;
        public const int StartingBombs = 3;

        [SerializeField]
        private int ropes = StartingRopes;

        [SerializeField]
        private int bombs = StartingBombs;

        [SerializeField]
        private int gold;

        [SerializeField]
        private HandToolId handTool;

        [SerializeField]
        private string promiseItemId = string.Empty;

        public event Action Changed;

        public int Ropes => ropes;
        public int Bombs => bombs;
        public int Gold => gold;
        public HandToolId HandTool => handTool;
        public string PromiseItemId => promiseItemId;
        public bool HasPromiseItem => !string.IsNullOrWhiteSpace(promiseItemId);

        public void ResetForNewRun()
        {
            ropes = StartingRopes;
            bombs = StartingBombs;
            gold = 0;
            handTool = HandToolId.None;
            promiseItemId = string.Empty;
            Changed?.Invoke();
        }

        public int AddRopes(int amount)
        {
            return AddClamped(ref ropes, amount, RopeCapacity);
        }

        public int AddBombs(int amount)
        {
            return AddClamped(ref bombs, amount, BombCapacity);
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            gold = checked(gold + amount);
            Changed?.Invoke();
        }

        public bool TryConsumeRope()
        {
            return TryConsume(ref ropes);
        }

        public bool TryConsumeBomb()
        {
            return TryConsume(ref bombs);
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

            gold -= amount;
            Changed?.Invoke();
            return true;
        }

        public HandToolId EquipHandTool(HandToolId nextTool)
        {
            HandToolId previous = handTool;
            if (previous == nextTool)
            {
                return previous;
            }

            handTool = nextTool;
            Changed?.Invoke();
            return previous;
        }

        public bool TryAttachPromiseItem(string itemId)
        {
            if (HasPromiseItem || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            promiseItemId = itemId.Trim();
            Changed?.Invoke();
            return true;
        }

        public string DetachPromiseItem()
        {
            string previous = promiseItemId;
            if (!HasPromiseItem)
            {
                return string.Empty;
            }

            promiseItemId = string.Empty;
            Changed?.Invoke();
            return previous;
        }

        private int AddClamped(ref int value, int amount, int capacity)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int previous = value;
            value = Mathf.Clamp(value + amount, 0, capacity);
            int added = value - previous;
            if (added > 0)
            {
                Changed?.Invoke();
            }

            return added;
        }

        private bool TryConsume(ref int value)
        {
            if (value <= 0)
            {
                return false;
            }

            value--;
            Changed?.Invoke();
            return true;
        }
    }
}
