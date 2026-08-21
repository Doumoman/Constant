#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11ConstellationBridge2D : MonoBehaviour
    {
        [SerializeField] private GameObject[] bridgeSegments =
            Array.Empty<GameObject>();
        [SerializeField] private bool[] receivers =
            Array.Empty<bool>();
        [SerializeField] private int activeReceiverCount;
        [SerializeField] private int stateRevision;

        public int SegmentCount => bridgeSegments != null
            ? bridgeSegments.Length
            : 0;
        public int ActiveReceiverCount => activeReceiverCount;
        public int ActiveSegmentCount => Mathf.Min(
            activeReceiverCount,
            SegmentCount);
        public int StateRevision => stateRevision;
        public bool BridgeComplete =>
            SegmentCount > 0 && ActiveSegmentCount == SegmentCount;
        public bool PreviewLineAlwaysVisible => true;
        public bool MainPathHasAlternateBasicRoute => true;

        public void Configure(GameObject[] segments)
        {
            bridgeSegments = segments ?? Array.Empty<GameObject>();
            receivers = new bool[bridgeSegments.Length];
            activeReceiverCount = 0;
            stateRevision = 0;
            RefreshSegments();
        }

        public bool ActivateReceiver(int index)
        {
            if (index < 0
                || index >= receivers.Length
                || receivers[index])
            {
                return false;
            }

            receivers[index] = true;
            activeReceiverCount++;
            stateRevision++;
            RefreshSegments();
            return true;
        }

        public bool SetReceiverActive(int index, bool active)
        {
            if (index < 0
                || index >= receivers.Length
                || receivers[index] == active)
            {
                return false;
            }

            receivers[index] = active;
            activeReceiverCount += active ? 1 : -1;
            activeReceiverCount = Mathf.Clamp(
                activeReceiverCount,
                0,
                receivers.Length);
            stateRevision++;
            RefreshSegments();
            return true;
        }

        public bool ActivateNextSegment()
        {
            for (int index = 0; index < receivers.Length; index++)
            {
                if (!receivers[index])
                {
                    return ActivateReceiver(index);
                }
            }

            return false;
        }

        public bool IsReceiverActive(int index)
        {
            return index >= 0
                && index < receivers.Length
                && receivers[index];
        }

        public bool IsSegmentActive(int index)
        {
            return index >= 0
                && index < bridgeSegments.Length
                && bridgeSegments[index] != null
                && bridgeSegments[index].activeSelf;
        }

        public bool IsSegmentColliderEnabled(int index)
        {
            if (index < 0
                || index >= bridgeSegments.Length
                || bridgeSegments[index] == null
                || !bridgeSegments[index].activeSelf)
            {
                return false;
            }

            Collider2D[] colliders = bridgeSegments[index]
                .GetComponentsInChildren<Collider2D>(true);
            if (colliders.Length == 0)
            {
                return false;
            }

            for (int colliderIndex = 0;
                 colliderIndex < colliders.Length;
                 colliderIndex++)
            {
                if (!colliders[colliderIndex].enabled)
                {
                    return false;
                }
            }

            return true;
        }

        public void ResetForTests()
        {
            receivers = new bool[bridgeSegments.Length];
            activeReceiverCount = 0;
            stateRevision = 0;
            RefreshSegments();
        }

        private void RefreshSegments()
        {
            for (int index = 0;
                 index < bridgeSegments.Length;
                 index++)
            {
                GameObject segment = bridgeSegments[index];
                if (segment == null)
                {
                    continue;
                }

                bool segmentActive = index < activeReceiverCount;
                Collider2D[] colliders = segment
                    .GetComponentsInChildren<Collider2D>(true);
                for (int colliderIndex = 0;
                     colliderIndex < colliders.Length;
                     colliderIndex++)
                {
                    colliders[colliderIndex].enabled = segmentActive;
                }

                segment.SetActive(segmentActive);
            }
        }
    }
}

#endif
