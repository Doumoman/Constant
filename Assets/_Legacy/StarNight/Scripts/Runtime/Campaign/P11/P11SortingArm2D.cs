#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P11SortingArm2D : MonoBehaviour
    {
        [SerializeField] private P11ParcelLabel matchingLabel;
        [SerializeField] private Vector2 matchingDirection =
            Vector2.right;
        [SerializeField] private Vector2 otherDirection =
            Vector2.left;
        [SerializeField, Min(0.1f)] private float pushSpeed = 3f;
        [SerializeField] private SpriteRenderer labelVisual;

        public P11ParcelLabel MatchingLabel => matchingLabel;
        public bool PlayerOnlyPushedNotDamaged => true;

        public void Configure(
            P11ParcelLabel label,
            Vector2 matchingPush,
            Vector2 fallbackPush,
            float speed,
            SpriteRenderer visual)
        {
            matchingLabel = label;
            matchingDirection = matchingPush.normalized;
            otherDirection = fallbackPush.normalized;
            pushSpeed = Mathf.Max(0.1f, speed);
            labelVisual = visual;
            if (labelVisual != null)
            {
                labelVisual.color =
                    P11AddressableParcel2D.LabelColor(label);
            }
        }

        public Vector2 DirectionFor(P11AddressableParcel2D parcel)
        {
            return parcel != null && parcel.Label == matchingLabel
                ? matchingDirection
                : otherDirection;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || body.bodyType == RigidbodyType2D.Static)
            {
                return;
            }

            P11AddressableParcel2D parcel =
                other.GetComponent<P11AddressableParcel2D>();
            body.position += DirectionFor(parcel)
                * pushSpeed
                * Time.fixedDeltaTime;
        }
    }
}

#endif
