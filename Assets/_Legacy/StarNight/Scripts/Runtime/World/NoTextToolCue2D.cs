#if LEGACY_DISABLED
using StarNight.Tools;
using UnityEngine;

namespace StarNight.World
{
    [DisallowMultipleComponent]
    public sealed class NoTextToolCue2D : MonoBehaviour
    {
        [SerializeField] private P3ToolKind toolKind;
        [SerializeField] private Transform player;
        [SerializeField] private SpriteRenderer cueVisual;
        [SerializeField] private Vector2 motionAxis = Vector2.right;
        [SerializeField, Min(0f)] private float amplitude = 0.45f;
        [SerializeField, Min(0.1f)] private float period = 1.1f;
        [SerializeField, Min(0.1f)] private float revealRadius = 5f;
        [SerializeField] private Color dimColor =
            new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private Color brightColor =
            new Color(1f, 0.88f, 0.34f, 0.95f);

        private Vector3 origin;

        public P3ToolKind ToolKind => toolKind;

        public void Configure(
            P3ToolKind kind,
            Transform targetPlayer,
            SpriteRenderer visual,
            Vector2 axis,
            float movementAmplitude = 0.45f,
            float cyclePeriod = 1.1f,
            float visibleRadius = 5f)
        {
            toolKind = kind;
            player = targetPlayer;
            cueVisual = visual;
            motionAxis = axis.sqrMagnitude > 0.0001f
                ? axis.normalized
                : Vector2.right;
            amplitude = Mathf.Max(0f, movementAmplitude);
            period = Mathf.Max(0.1f, cyclePeriod);
            revealRadius = Mathf.Max(0.1f, visibleRadius);
            origin = transform.localPosition;
            ApplyVisual(0f);
        }

        private void Awake()
        {
            origin = transform.localPosition;
            if (cueVisual == null)
            {
                cueVisual = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Update()
        {
            float normalized = Mathf.Repeat(Time.unscaledTime / period, 1f);
            float wave = Mathf.Sin(normalized * Mathf.PI);
            transform.localPosition =
                origin + (Vector3)(motionAxis * amplitude * wave);
            ApplyVisual(wave);
        }

        private void ApplyVisual(float wave)
        {
            if (cueVisual == null)
            {
                return;
            }

            bool nearby = player == null
                || ((Vector2)player.position - (Vector2)transform.position)
                    .sqrMagnitude <= revealRadius * revealRadius;
            cueVisual.color = nearby
                ? Color.Lerp(dimColor, brightColor, 0.35f + wave * 0.65f)
                : dimColor;
        }
    }
}

#endif
