using System;
using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [DisallowMultipleComponent]
    public sealed class RaniLampController : MonoBehaviour
    {
        [SerializeField]
        private bool available = true;

        public event Action<bool> AvailabilityChanged;

        public bool IsAvailable => available;

        public void RechargeForChapter()
        {
            if (available)
            {
                return;
            }

            available = true;
            AvailabilityChanged?.Invoke(true);
        }

        public bool TryConsumeRescue()
        {
            if (!available)
            {
                return false;
            }

            available = false;
            AvailabilityChanged?.Invoke(false);
            return true;
        }
    }
}
