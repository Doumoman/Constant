#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools.Umbrella
{
    [DisallowMultipleComponent]
    public sealed class WindZone2D : MonoBehaviour
    {
        private static readonly HashSet<WindZone2D> ActiveInternal =
            new HashSet<WindZone2D>();

        [SerializeField] private Collider2D zoneCollider;
        [SerializeField] private Vector2 direction = Vector2.up;
        [SerializeField, Min(0f)] private float strength = 8f;
        [SerializeField, Min(1f)] private float umbrellaMultiplier = 1.75f;
        [SerializeField] private int stableOrder;

        public static IReadOnlyCollection<WindZone2D> ActiveZones =>
            ActiveInternal;
        public int StableOrder => stableOrder;

        public void Configure(
            Collider2D collider,
            Vector2 windDirection,
            float windStrength,
            float configuredUmbrellaMultiplier = 1.75f,
            int configuredStableOrder = 0)
        {
            zoneCollider = collider != null
                ? collider
                : GetComponent<Collider2D>();
            direction = windDirection.sqrMagnitude > 0.0001f
                ? windDirection.normalized
                : Vector2.up;
            strength = Mathf.Max(0f, windStrength);
            umbrellaMultiplier = Mathf.Max(
                1f,
                configuredUmbrellaMultiplier);
            stableOrder = configuredStableOrder;
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void Awake()
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<Collider2D>();
            }
        }

        private void OnEnable()
        {
            ActiveInternal.Add(this);
        }

        private void OnDisable()
        {
            ActiveInternal.Remove(this);
        }

        public bool Contains(Vector2 worldPoint)
        {
            return zoneCollider != null
                && zoneCollider.OverlapPoint(worldPoint);
        }

        public Vector2 GetAcceleration(WindResponse response)
        {
            float multiplier = response == WindResponse.Umbrella
                ? umbrellaMultiplier
                : 1f;
            return direction * strength * multiplier;
        }
    }
}

#endif
