#if LEGACY_DISABLED
using System;
using StarNight.Core.Flow;
using StarNight.Core.State;
using UnityEngine;

namespace StarNight.Tools.Bomb
{
    [DisallowMultipleComponent]
    public sealed class BombInventoryState : MonoBehaviour
    {
        [SerializeField] private BombDefinition definition;
        [SerializeField] private int remaining;
        [SerializeField] private bool initialized;

        public event Action<int> CountChanged;

        public int Remaining => remaining;
        public bool HasBomb => initialized && remaining > 0;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            remaining = definition != null
                ? definition.StartingCount
                : BombDefinition.ApprovedStartingCount;
            initialized = true;
        }

        public bool TryConsume()
        {
            EnsureInitialized();
            if (remaining <= 0)
            {
                return false;
            }

            remaining--;
            CountChanged?.Invoke(remaining);
            MirrorRunState();
            return true;
        }

        public void Restore(int count)
        {
            remaining = Mathf.Max(0, count);
            initialized = true;
            CountChanged?.Invoke(remaining);
            MirrorRunState();
        }

        public void Configure(BombDefinition configuredDefinition, int? count = null)
        {
            definition = configuredDefinition;
            remaining = Mathf.Max(0, count ?? configuredDefinition?.StartingCount
                ?? BombDefinition.ApprovedStartingCount);
            initialized = true;
            MirrorRunState();
        }

        private void MirrorRunState()
        {
            if (GameBootstrap.IsReady
                && GameBootstrap.Instance.Services.TryGet(out RunManager manager)
                && manager.Current != null)
            {
                manager.Current.bombs = remaining;
            }
        }
    }
}

#endif
