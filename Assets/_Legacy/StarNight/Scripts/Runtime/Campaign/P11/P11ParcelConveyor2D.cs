#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P11ParcelConveyor2D : MonoBehaviour
    {
        public const float DefaultSpeed = 2.4f;

        [SerializeField] private Vector2 direction = Vector2.right;
        [SerializeField, Min(0.1f)] private float speed =
            DefaultSpeed;
        [SerializeField] private SpriteRenderer directionVisual;

        public Vector2 Direction => direction;
        public float Speed => speed;
        public bool CrushingDisabled => true;

        public void Configure(
            Vector2 movementDirection,
            float movementSpeed = DefaultSpeed,
            SpriteRenderer arrowVisual = null)
        {
            direction = movementDirection.sqrMagnitude > 0.001f
                ? movementDirection.normalized
                : Vector2.right;
            speed = Mathf.Max(0.1f, movementSpeed);
            directionVisual = arrowVisual;
            RefreshDirectionVisual();
        }

        public void Reverse()
        {
            direction = -direction;
            RefreshDirectionVisual();
        }

        public Vector2 EvaluateDisplacement(float deltaSeconds)
        {
            return direction
                * speed
                * Mathf.Max(0f, deltaSeconds);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || body.bodyType == RigidbodyType2D.Static)
            {
                return;
            }

            body.position += EvaluateDisplacement(
                Time.fixedDeltaTime);
        }

        private void RefreshDirectionVisual()
        {
            if (directionVisual == null)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg;
            directionVisual.transform.rotation =
                Quaternion.Euler(0f, 0f, angle);
        }
    }
}

#endif
