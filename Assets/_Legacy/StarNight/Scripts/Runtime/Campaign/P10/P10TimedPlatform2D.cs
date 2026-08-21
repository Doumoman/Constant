#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class P10TimedPlatform2D : MonoBehaviour
    {
        [SerializeField, Min(0.25f)] private float solidSeconds = 2.5f;
        [SerializeField, Min(0.25f)] private float hiddenSeconds = 1.25f;
        [SerializeField, Min(0f)] private float phaseOffsetSeconds;

        private Collider2D platformCollider;
        private SpriteRenderer platformRenderer;
        private Color solidColor = Color.white;

        public float SolidSeconds => solidSeconds;
        public float HiddenSeconds => hiddenSeconds;
        public bool IsSolid =>
            platformCollider != null && platformCollider.enabled;
        public bool IsConfigured =>
            platformCollider != null
            && platformRenderer != null
            && solidSeconds > 0f
            && hiddenSeconds > 0f;

        public void Configure(
            float visibleDuration,
            float hiddenDuration,
            float phaseSeconds)
        {
            CacheComponents();
            solidSeconds = Mathf.Max(0.25f, visibleDuration);
            hiddenSeconds = Mathf.Max(0.25f, hiddenDuration);
            phaseOffsetSeconds = Mathf.Max(0f, phaseSeconds);
            SetSolid(true);
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            SetSolid(true);
        }

        private void Update()
        {
            float period = solidSeconds + hiddenSeconds;
            float elapsed = Mathf.Repeat(
                Time.time + phaseOffsetSeconds,
                period);
            SetSolid(elapsed < solidSeconds);
        }

        private void CacheComponents()
        {
            platformCollider = GetComponent<Collider2D>();
            platformRenderer = GetComponent<SpriteRenderer>();
            if (platformRenderer != null)
            {
                solidColor = platformRenderer.color;
            }
        }

        private void SetSolid(bool solid)
        {
            if (platformCollider != null)
            {
                platformCollider.enabled = solid;
            }

            if (platformRenderer != null)
            {
                Color color = solidColor;
                color.a = solid ? solidColor.a : solidColor.a * 0.18f;
                platformRenderer.color = color;
            }
        }
    }
}

#endif
