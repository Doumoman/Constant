#if LEGACY_DISABLED
using System;
using StarNight.Core.Flow;
using StarNight.Core.State;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeInventoryState : MonoBehaviour
    {
        [SerializeField] private RopeDefinition definition;
        [SerializeField] private int remaining;
        [SerializeField] private bool initialized;

        public event Action<int> CountChanged;

        public int Remaining => remaining;
        public bool HasRope => initialized && remaining > 0;

        private void Awake() => EnsureInitialized();

        public void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }
            remaining = definition != null ? definition.StartingCount : RopeDefinition.ApprovedStartingCount;
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

        public void Configure(RopeDefinition configuredDefinition, int? count = null)
        {
            definition = configuredDefinition;
            remaining = Mathf.Max(0, count ?? configuredDefinition?.StartingCount
                ?? RopeDefinition.ApprovedStartingCount);
            initialized = true;
            MirrorRunState();
        }

        public void Restore(int count)
        {
            remaining = Mathf.Max(0, count);
            initialized = true;
            CountChanged?.Invoke(remaining);
            MirrorRunState();
        }

        private void MirrorRunState()
        {
            if (GameBootstrap.IsReady
                && GameBootstrap.Instance.Services.TryGet(out RunManager manager)
                && manager.Current != null)
            {
                manager.Current.ropes = remaining;
            }
        }
    }
}

#endif
