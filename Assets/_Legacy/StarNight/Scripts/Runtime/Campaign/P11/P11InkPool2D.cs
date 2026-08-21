#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P11InkPool2D : MonoBehaviour
    {
        [SerializeField] private P11ParcelLabel appliedLabel;
        [SerializeField] private SpriteRenderer poolVisual;
        [SerializeField] private int applicationCount;
        [SerializeField] private P11AddressableParcel2D lastParcel;

        public P11ParcelLabel AppliedLabel => appliedLabel;
        public int ApplicationCount => applicationCount;
        public P11AddressableParcel2D LastParcel => lastParcel;
        public bool HasAppliedLabel => applicationCount > 0;

        public void Configure(
            P11ParcelLabel label,
            SpriteRenderer visual)
        {
            appliedLabel = label;
            poolVisual = visual;
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }

            if (poolVisual != null)
            {
                poolVisual.color =
                    P11AddressableParcel2D.LabelColor(label);
            }

            applicationCount = 0;
            lastParcel = null;
        }

        public bool TryApply(P11AddressableParcel2D parcel)
        {
            if (parcel == null || !parcel.ApplyLabel(appliedLabel))
            {
                return false;
            }

            lastParcel = parcel;
            applicationCount++;
            return true;
        }

        public bool TryApply(Collider2D other)
        {
            return TryApply(ResolveParcel(other));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApply(other);
        }

        private static P11AddressableParcel2D ResolveParcel(
            Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            P11AddressableParcel2D parcel =
                other.GetComponent<P11AddressableParcel2D>();
            if (parcel != null)
            {
                return parcel;
            }

            if (other.attachedRigidbody != null)
            {
                parcel = other.attachedRigidbody.GetComponent<
                    P11AddressableParcel2D>();
                if (parcel != null)
                {
                    return parcel;
                }
            }

            return other.GetComponentInParent<
                P11AddressableParcel2D>();
        }
    }
}

#endif
