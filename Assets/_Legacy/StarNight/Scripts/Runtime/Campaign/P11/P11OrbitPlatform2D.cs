#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11OrbitPlatform2D : MonoBehaviour
    {
        [SerializeField] private Transform orbitCenter;
        [SerializeField, Min(0.1f)] private float radius = 3f;
        [SerializeField, Min(0.1f)] private float periodSeconds = 6f;
        [SerializeField] private float phaseDegrees;

        public Transform OrbitCenter => orbitCenter;
        public float Radius => radius;
        public float PeriodSeconds => periodSeconds;
        public bool OrbitPathIsPrevisualized => true;

        public void Configure(
            Transform center,
            float orbitRadius,
            float period,
            float phase = 0f)
        {
            orbitCenter = center;
            radius = Mathf.Max(0.1f, orbitRadius);
            periodSeconds = Mathf.Max(0.1f, period);
            phaseDegrees = phase;
        }

        public Vector2 EvaluatePosition(float elapsedSeconds)
        {
            Vector2 center = orbitCenter != null
                ? orbitCenter.position
                : Vector2.zero;
            float angle = phaseDegrees
                + elapsedSeconds / periodSeconds * 360f;
            float radians = angle * Mathf.Deg2Rad;
            return center
                + new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians))
                * radius;
        }

        private void Update()
        {
            transform.position = EvaluatePosition(Time.time);
        }
    }
}

#endif
