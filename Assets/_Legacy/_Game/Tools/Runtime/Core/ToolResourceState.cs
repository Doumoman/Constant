#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Tools.Core
{
    [Serializable]
    public sealed class ToolResourceState
    {
        [SerializeField] private string configuredToolId;
        [SerializeField] private ToolResourceMode mode;
        [SerializeField, Min(0)] private int maximum;
        [SerializeField, Min(0)] private int current;
        [SerializeField] private bool initialized;

        public event Action<int, int> Changed;

        public ToolResourceMode Mode => mode;
        public int Current => mode == ToolResourceMode.Infinite ? 0 : current;
        public int Maximum => mode == ToolResourceMode.Infinite ? 0 : maximum;
        public bool IsInfinite => mode == ToolResourceMode.Infinite;
        public bool HasUsableResource => IsInfinite || current > 0;

        public void Initialize(HandToolDefinition definition)
        {
            if (definition == null)
            {
                initialized = false;
                configuredToolId = string.Empty;
                mode = ToolResourceMode.Infinite;
                maximum = 0;
                current = 0;
                return;
            }

            if (initialized && configuredToolId == definition.ToolId)
            {
                return;
            }

            configuredToolId = definition.ToolId;
            mode = definition.ResourceMode;
            maximum = mode == ToolResourceMode.Infinite ? 0 : Mathf.Max(1, definition.MaxResource);
            current = maximum;
            initialized = true;
            Changed?.Invoke(Current, Maximum);
        }

        public bool TryConsumeForSuccessfulReaction(bool successfulReaction)
        {
            if (!successfulReaction)
            {
                return false;
            }
            if (IsInfinite)
            {
                return true;
            }
            if (current <= 0)
            {
                return false;
            }

            current--;
            Changed?.Invoke(current, maximum);
            return true;
        }

        public void RepairFull()
        {
            if (IsInfinite)
            {
                return;
            }
            current = maximum;
            Changed?.Invoke(current, maximum);
        }

        public void RestoreCurrent(int value)
        {
            if (IsInfinite)
            {
                return;
            }
            current = Mathf.Clamp(value, 0, maximum);
            Changed?.Invoke(current, maximum);
        }

        public void ConfigureForTests(ToolResourceMode configuredMode, int max, int value)
        {
            configuredToolId = "TEST";
            mode = configuredMode;
            maximum = configuredMode == ToolResourceMode.Infinite ? 0 : Mathf.Max(1, max);
            current = configuredMode == ToolResourceMode.Infinite ? 0 : Mathf.Clamp(value, 0, maximum);
            initialized = true;
        }
    }
}

#endif
