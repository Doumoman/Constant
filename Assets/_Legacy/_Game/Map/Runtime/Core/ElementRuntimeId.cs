#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    public sealed class ElementRuntimeId : MonoBehaviour
    {
        [SerializeField] private string value;

        public string Value => value;

        private void Awake()
        {
            EnsureValue();
        }

        private void Reset()
        {
            EnsureValue();
        }

        public string EnsureValue()
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                value = Guid.NewGuid().ToString("N");
            }

            return value;
        }

        public void SetValue(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                throw new ArgumentException("A runtime ID is required.", nameof(runtimeId));
            }

            value = runtimeId.Trim();
        }
    }
}

#endif
