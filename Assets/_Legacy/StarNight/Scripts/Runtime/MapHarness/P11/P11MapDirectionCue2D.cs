#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.MapHarness.P11
{
    [DisallowMultipleComponent]
    public sealed class P11MapDirectionCue2D : MonoBehaviour
    {
        [SerializeField] private P11MapCueKind cueKind;
        [SerializeField] private P11MapRouteKind route;
        [SerializeField] private Transform target;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool visibleAtEntry = true;
        [SerializeField] private bool routeVisible = true;
        [SerializeField] private bool pointAtTarget = true;
        [SerializeField] private bool pulse = true;
        [SerializeField, Min(0.01f)] private float pulseSpeed = 2f;
        [SerializeField, Range(0f, 0.5f)]
        private float pulseScale = 0.12f;
        [SerializeField, Range(0f, 0.75f)]
        private float pulseAlpha = 0.15f;

        private Vector3 baseScale = Vector3.one;
        private Color baseColor = Color.white;
        private bool visualStateCaptured;

        public P11MapCueKind CueKind => cueKind;
        public P11MapRouteKind Route => route;
        public Transform Target => target;
        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public bool VisibleAtEntry => visibleAtEntry;
        public bool RouteVisible => routeVisible;
        public bool IsVisible =>
            visibleAtEntry && routeVisible && enabled;
        public bool PointsAtTarget => pointAtTarget;
        public bool PulseEnabled => pulse;
        public bool RouteContractSatisfied =>
            P11MapCueRules.MatchesRouteContract(cueKind, route);
        public bool TextFreeVisualReady =>
            spriteRenderer != null
            && spriteRenderer.sprite != null;

        public void Configure(
            P11MapCueKind kind,
            P11MapRouteKind routeKind,
            Transform cueTarget,
            SpriteRenderer visual,
            bool entryVisible = true,
            bool shouldPointAtTarget = true,
            bool shouldPulse = true,
            float animationSpeed = 2f,
            float scaleAmount = 0.12f,
            float alphaAmount = 0.15f)
        {
            cueKind = kind;
            route = routeKind;
            target = cueTarget;
            spriteRenderer = visual;
            visibleAtEntry = entryVisible;
            routeVisible = true;
            pointAtTarget = shouldPointAtTarget;
            pulse = shouldPulse;
            pulseSpeed = Mathf.Max(0.01f, animationSpeed);
            pulseScale = Mathf.Clamp(scaleAmount, 0f, 0.5f);
            pulseAlpha = Mathf.Clamp(alphaAmount, 0f, 0.75f);
            visualStateCaptured = false;
            CaptureVisualState();
            RefreshVisualNow();
        }

        public void SetVisible(bool visible)
        {
            visibleAtEntry = visible;
            RefreshVisualNow();
        }

        public void SetRouteVisible(bool visible)
        {
            routeVisible = visible;
            RefreshVisualNow();
        }

        public void SetTarget(Transform cueTarget)
        {
            target = cueTarget;
            RefreshPointingNow();
        }

        public bool IsVisibleFor(P11MapRouteKind routeKind)
        {
            return IsVisible && route == routeKind;
        }

        public Vector3 EvaluatePulseScale(float time)
        {
            CaptureVisualState();
            if (!pulse)
            {
                return baseScale;
            }

            float wave = 0.5f
                + 0.5f * Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f);
            return baseScale * (1f + wave * pulseScale);
        }

        public Color EvaluatePulseColor(float time)
        {
            CaptureVisualState();
            if (!pulse)
            {
                return baseColor;
            }

            float wave = 0.5f
                + 0.5f * Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f);
            Color color = baseColor;
            color.a = Mathf.Clamp01(
                baseColor.a - pulseAlpha + wave * pulseAlpha);
            return color;
        }

        public void ApplyVisualAtTime(float time)
        {
            CaptureVisualState();
            transform.localScale = EvaluatePulseScale(time);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = EvaluatePulseColor(time);
                spriteRenderer.enabled = IsVisible;
            }

            RefreshPointingNow();
        }

        public void RefreshPointingNow()
        {
            if (!pointAtTarget || target == null)
            {
                return;
            }

            Vector3 delta = target.position - transform.position;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x)
                * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void RefreshVisualNow()
        {
            CaptureVisualState();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = IsVisible;
            }

            RefreshPointingNow();
        }

        private void Awake()
        {
            CaptureVisualState();
            RefreshVisualNow();
        }

        private void OnEnable()
        {
            CaptureVisualState();
            RefreshVisualNow();
        }

        private void Update()
        {
            ApplyVisualAtTime(Time.unscaledTime);
        }

        private void OnValidate()
        {
            pulseSpeed = Mathf.Max(0.01f, pulseSpeed);
            pulseScale = Mathf.Clamp(pulseScale, 0f, 0.5f);
            pulseAlpha = Mathf.Clamp(pulseAlpha, 0f, 0.75f);
            CaptureVisualState();
            RefreshVisualNow();
        }

        private void CaptureVisualState()
        {
            if (visualStateCaptured)
            {
                return;
            }

            baseScale = transform.localScale;
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            visualStateCaptured = true;
        }
    }
}

#endif
