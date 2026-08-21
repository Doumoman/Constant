#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P11MailTube2D : MonoBehaviour
    {
        [SerializeField] private P11MailTube2D pairedTube;
        [SerializeField] private Transform arrivalAnchor;
        [SerializeField] private P11ParcelLabel addressGlyph;
        [SerializeField, Min(0.1f)] private float reentryCooldown =
            0.6f;
        private float lastArrivalTime = float.NegativeInfinity;

        public P11MailTube2D PairedTube => pairedTube;
        public P11ParcelLabel AddressGlyph => addressGlyph;
        public bool MainRouteBidirectional =>
            pairedTube != null && pairedTube.pairedTube == this;

        public void Configure(
            P11MailTube2D pair,
            Transform arrival,
            P11ParcelLabel glyph)
        {
            pairedTube = pair;
            arrivalAnchor = arrival;
            addressGlyph = glyph;
        }

        public bool TrySend(Rigidbody2D body)
        {
            if (body == null
                || pairedTube == null
                || Time.time - lastArrivalTime < reentryCooldown)
            {
                return false;
            }

            Vector2 destination =
                pairedTube.arrivalAnchor != null
                    ? pairedTube.arrivalAnchor.position
                    : pairedTube.transform.position;
            body.position = destination;
            body.linearVelocity = Vector2.zero;
            pairedTube.lastArrivalTime = Time.time;
            Physics2D.SyncTransforms();
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TrySend(other.attachedRigidbody);
        }
    }
}

#endif
