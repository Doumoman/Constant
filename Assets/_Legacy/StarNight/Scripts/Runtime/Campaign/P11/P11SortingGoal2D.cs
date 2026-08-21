#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P11SortingGoal2D : MonoBehaviour
    {
        [SerializeField] private P11ParcelLabel expectedLabel;
        [SerializeField] private P11StoryState2D storyState;
        [SerializeField] private Transform arrivalAnchor;
        [SerializeField] private SpriteRenderer goalVisual;
        [SerializeField] private bool completed;
        [SerializeField] private int acceptedCount;
        [SerializeField] private int rejectedCount;
        [SerializeField] private P11AddressableParcel2D acceptedParcel;

        public event Action<P11AddressableParcel2D> ParcelSorted;

        public P11ParcelLabel ExpectedLabel => expectedLabel;
        public P11StoryState2D StoryState => storyState;
        public bool Completed => completed;
        public int AcceptedCount => acceptedCount;
        public int RejectedCount => rejectedCount;
        public P11AddressableParcel2D AcceptedParcel =>
            acceptedParcel;
        public bool IsConfigured => storyState != null;

        public void Configure(
            P11ParcelLabel requiredLabel,
            P11StoryState2D state,
            Transform sortedArrival = null,
            SpriteRenderer visual = null)
        {
            expectedLabel = requiredLabel;
            storyState = state;
            arrivalAnchor = sortedArrival;
            goalVisual = visual;
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }

            completed = storyState != null
                && storyState.LostParcelSorted;
            acceptedCount = 0;
            rejectedCount = 0;
            acceptedParcel = null;
            RefreshVisual();
        }

        public bool TryAccept(P11AddressableParcel2D parcel)
        {
            if (completed || storyState == null || parcel == null)
            {
                return false;
            }

            if (parcel.Label != expectedLabel)
            {
                rejectedCount++;
                return false;
            }

            Rigidbody2D body = parcel.Body;
            if (body != null)
            {
                if (arrivalAnchor != null)
                {
                    body.position = arrivalAnchor.position;
                }

                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.Sleep();
                Physics2D.SyncTransforms();
            }

            storyState.MarkLostParcelSorted();
            completed = storyState.LostParcelSorted;
            if (!completed)
            {
                return false;
            }

            acceptedParcel = parcel;
            acceptedCount++;
            RefreshVisual();
            ParcelSorted?.Invoke(parcel);
            return true;
        }

        public bool TryAccept(Collider2D other)
        {
            return TryAccept(ResolveParcel(other));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryAccept(other);
        }

        private void RefreshVisual()
        {
            if (goalVisual == null)
            {
                return;
            }

            Color color = P11AddressableParcel2D.LabelColor(
                expectedLabel);
            color.a = completed ? 0.35f : 0.9f;
            goalVisual.color = color;
        }

        private static P11AddressableParcel2D ResolveParcel(
            Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            P11AddressableParcel2D direct =
                other.GetComponent<P11AddressableParcel2D>();
            if (direct != null)
            {
                return direct;
            }

            if (other.attachedRigidbody != null)
            {
                direct = other.attachedRigidbody.GetComponent<
                    P11AddressableParcel2D>();
                if (direct != null)
                {
                    return direct;
                }
            }

            return other.GetComponentInParent<
                P11AddressableParcel2D>();
        }
    }
}

#endif
