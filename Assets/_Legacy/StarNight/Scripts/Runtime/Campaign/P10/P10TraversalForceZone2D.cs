#if LEGACY_DISABLED
using StarNight.Player;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P10TraversalForceZone2D : MonoBehaviour
    {
        [SerializeField] private Vector2 direction = Vector2.right;
        [SerializeField, Min(0f)] private float acceleration = 5f;
        [SerializeField] private bool oscillates;
        [SerializeField, Min(0.25f)] private float halfCycleSeconds = 2f;
        [SerializeField] private SpriteRenderer indicator;

        private Collider2D trigger;

        public Vector2 Direction => direction;
        public float Acceleration => acceleration;
        public bool Oscillates => oscillates;
        public float HalfCycleSeconds => halfCycleSeconds;
        public bool IsConfigured =>
            trigger != null
            && trigger.isTrigger
            && direction.sqrMagnitude > 0.01f
            && acceleration > 0f;

        public void Configure(
            Vector2 forceDirection,
            float forceAcceleration,
            bool alternateDirection,
            float alternateHalfCycleSeconds,
            SpriteRenderer zoneIndicator)
        {
            trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            direction = forceDirection.sqrMagnitude > 0.01f
                ? forceDirection.normalized
                : Vector2.right;
            acceleration = Mathf.Max(0f, forceAcceleration);
            oscillates = alternateDirection;
            halfCycleSeconds = Mathf.Max(
                0.25f,
                alternateHalfCycleSeconds);
            indicator = zoneIndicator;
            RefreshIndicator();
        }

        private void Awake()
        {
            trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }

        private void OnValidate()
        {
            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = Vector2.right;
            }
            else
            {
                direction.Normalize();
            }

            acceleration = Mathf.Max(0f, acceleration);
            halfCycleSeconds = Mathf.Max(0.25f, halfCycleSeconds);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerMotor2D player =
                other.GetComponentInParent<PlayerMotor2D>();
            Rigidbody2D body = other.attachedRigidbody;
            if (player == null
                || body == null
                || !body.simulated
                || body.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            body.AddForce(
                CurrentDirection()
                    * acceleration
                    * Mathf.Max(1f, body.mass),
                ForceMode2D.Force);
        }

        private Vector2 CurrentDirection()
        {
            if (!oscillates)
            {
                return direction;
            }

            int halfCycle =
                Mathf.FloorToInt(Time.time / halfCycleSeconds);
            return (halfCycle & 1) == 0 ? direction : -direction;
        }

        private void RefreshIndicator()
        {
            if (indicator == null)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg;
            indicator.transform.localRotation =
                Quaternion.Euler(0f, 0f, angle);
        }
    }
}

#endif
