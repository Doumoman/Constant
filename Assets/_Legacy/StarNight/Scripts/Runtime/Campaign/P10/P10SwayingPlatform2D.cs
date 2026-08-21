#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P10SwayingPlatform2D : MonoBehaviour
    {
        [SerializeField] private Vector2 axis = Vector2.right;
        [SerializeField, Min(0f)] private float travelDistance = 2f;
        [SerializeField, Min(0.5f)] private float cycleSeconds = 4f;
        [SerializeField, Range(0f, 1f)] private float phaseOffset;

        private Rigidbody2D body;
        private Vector2 origin;

        public Vector2 Axis => axis;
        public float TravelDistance => travelDistance;
        public float CycleSeconds => cycleSeconds;
        public bool IsConfigured =>
            body != null
            && body.bodyType == RigidbodyType2D.Kinematic
            && axis.sqrMagnitude > 0.01f
            && travelDistance > 0f;

        public void Configure(
            Vector2 movementAxis,
            float distance,
            float periodSeconds,
            float normalizedPhase = 0f)
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            axis = movementAxis.sqrMagnitude > 0.01f
                ? movementAxis.normalized
                : Vector2.right;
            travelDistance = Mathf.Max(0f, distance);
            cycleSeconds = Mathf.Max(0.5f, periodSeconds);
            phaseOffset = Mathf.Repeat(normalizedPhase, 1f);
            origin = body.position;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            origin = body.position;
        }

        private void OnEnable()
        {
            if (body != null)
            {
                origin = body.position;
            }
        }

        private void FixedUpdate()
        {
            if (body == null || travelDistance <= 0f)
            {
                return;
            }

            float phase =
                (Time.time / cycleSeconds + phaseOffset)
                * Mathf.PI * 2f;
            body.MovePosition(
                origin + axis * (Mathf.Sin(phase) * travelDistance));
        }
    }
}

#endif
