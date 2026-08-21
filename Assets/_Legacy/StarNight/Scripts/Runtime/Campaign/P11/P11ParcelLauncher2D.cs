#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P11ParcelLauncher2D : MonoBehaviour
    {
        public const float DefaultLaunchSpeed = 9f;

        [SerializeField] private Vector2 launchDirection =
            Vector2.right;
        [SerializeField, Min(0.1f)] private float launchSpeed =
            DefaultLaunchSpeed;
        [SerializeField] private P11ParcelLabel acceptedLabel;
        [SerializeField] private SpriteRenderer directionVisual;
        [SerializeField] private int launchCount;
        [SerializeField] private Rigidbody2D lastLaunchedBody;
        [SerializeField] private Vector2 lastLaunchVelocity;

        public Vector2 LaunchDirection => launchDirection;
        public float LaunchSpeed => launchSpeed;
        public P11ParcelLabel AcceptedLabel => acceptedLabel;
        public int LaunchCount => launchCount;
        public Rigidbody2D LastLaunchedBody => lastLaunchedBody;
        public Vector2 LastLaunchVelocity => lastLaunchVelocity;
        public bool HasLaunched => launchCount > 0;

        public void Configure(
            Vector2 direction,
            float speed = DefaultLaunchSpeed,
            P11ParcelLabel requiredLabel = P11ParcelLabel.None,
            SpriteRenderer visual = null)
        {
            launchDirection = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
            launchSpeed = Mathf.Max(0.1f, speed);
            acceptedLabel = requiredLabel;
            directionVisual = visual;
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }

            launchCount = 0;
            lastLaunchedBody = null;
            lastLaunchVelocity = Vector2.zero;
            RefreshDirectionVisual();
        }

        public bool CanLaunch(Rigidbody2D body)
        {
            if (body == null
                || body.bodyType == RigidbodyType2D.Static)
            {
                return false;
            }

            if (acceptedLabel == P11ParcelLabel.None)
            {
                return body.GetComponent<P11AddressableParcel2D>()
                    != null;
            }

            P11AddressableParcel2D parcel =
                body.GetComponent<P11AddressableParcel2D>();
            return parcel != null && parcel.Label == acceptedLabel;
        }

        public bool TryLaunch(Rigidbody2D body)
        {
            if (!CanLaunch(body))
            {
                return false;
            }

            Vector2 velocity = launchDirection * launchSpeed;
            body.linearVelocity = velocity;
            body.angularVelocity = 0f;
            body.WakeUp();
            lastLaunchedBody = body;
            lastLaunchVelocity = velocity;
            launchCount++;
            return true;
        }

        public bool TryLaunch(Collider2D other)
        {
            return other != null
                && TryLaunch(other.attachedRigidbody);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryLaunch(other);
        }

        private void RefreshDirectionVisual()
        {
            if (directionVisual == null)
            {
                return;
            }

            float angle = Mathf.Atan2(
                launchDirection.y,
                launchDirection.x) * Mathf.Rad2Deg;
            directionVisual.transform.rotation =
                Quaternion.Euler(0f, 0f, angle);
        }
    }
}

#endif
