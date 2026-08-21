#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StarNight.Interaction.Targeting
{
    [DisallowMultipleComponent]
    public sealed class InteractionProbe : MonoBehaviour
    {
        public static readonly Vector2 DefaultSize = new Vector2(1.10f, 1.00f);
        public const float DefaultCenterOffset = 0.45f;
        public const float DefaultMaxContextDistance = 0.85f;
        public const float DefaultSelectionHoldSeconds = 0.15f;

        [SerializeField] private Transform probeOrigin;
        [SerializeField] private Vector2 probeSize = new Vector2(1.10f, 1.00f);
        [SerializeField] private float centerOffset = DefaultCenterOffset;
        [SerializeField] private float maxContextDistance = DefaultMaxContextDistance;
        [SerializeField] private float selectionHoldSeconds = DefaultSelectionHoldSeconds;
        [SerializeField] private LayerMask interactionMask;
        [SerializeField] private LayerMask lineOfSightBlockMask;
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private int facingSign = 1;
        [SerializeField] private Object handSlotItem;

        private readonly Collider2D[] overlapBuffer = new Collider2D[32];
        private readonly List<InteractionCandidate> candidates = new List<InteractionCandidate>(16);
        private readonly HashSet<int> candidateInstanceIds = new HashSet<int>();
        private readonly InteractionTargetSelector selector = new InteractionTargetSelector();
        private InteractionCandidate selectedCandidate;
        private float selectedAt;

        public event Action<InteractionCandidate, InteractionCandidate> SelectionChanged;

        public InteractionCandidate SelectedCandidate => selectedCandidate;
        public Object HandSlotItem => handSlotItem;
        public int FacingSign => facingSign;

        private void Reset()
        {
            ApplyDefaultMasks();
        }

        private void Awake()
        {
            if (interactionMask.value == 0 || lineOfSightBlockMask.value == 0)
            {
                ApplyDefaultMasks();
            }
        }

        private void Update()
        {
            if (autoRefresh)
            {
                Refresh(Time.unscaledTime);
            }
        }

        public InteractionCandidate Refresh(float now)
        {
            Vector2 origin = probeOrigin != null ? probeOrigin.position : transform.position;
            Vector2 facing = facingSign < 0 ? Vector2.left : Vector2.right;
            Vector2 center = origin + facing * centerOffset;
            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = true
            };
            filter.SetLayerMask(interactionMask);

            int count = Physics2D.OverlapBox(center, probeSize, 0f, filter, overlapBuffer);
            candidates.Clear();
            candidateInstanceIds.Clear();
            ContextReceiverQuery query = new ContextReceiverQuery(gameObject, handSlotItem);
            float maxDistanceSquared = maxContextDistance * maxContextDistance;
            for (int index = 0; index < count; index++)
            {
                Collider2D overlap = overlapBuffer[index];
                if (overlap == null)
                {
                    continue;
                }

                InteractionCandidate candidate = overlap.GetComponentInParent<InteractionCandidate>();
                if (candidate == null
                    || !candidateInstanceIds.Add(candidate.GetInstanceID())
                    || !candidate.IsSelectable(query))
                {
                    continue;
                }

                Vector2 target = candidate.AnchorPosition;
                Vector2 offset = target - origin;
                if (offset.sqrMagnitude > maxDistanceSquared || IsLineOfSightBlocked(origin, offset))
                {
                    continue;
                }

                candidates.Add(candidate);
            }

            if (selectedCandidate != null
                && now - selectedAt < selectionHoldSeconds
                && candidates.Contains(selectedCandidate))
            {
                return selectedCandidate;
            }

            InteractionCandidate next = selector.Select(
                candidates,
                origin,
                facing,
                query,
                selectedCandidate);
            SetSelection(next, now);
            return selectedCandidate;
        }

        public void SetFacing(int sign)
        {
            facingSign = sign < 0 ? -1 : 1;
        }

        public void SetHandSlotItem(Object item)
        {
            handSlotItem = item;
        }

        public void ClearSelection()
        {
            SetSelection(null, Time.unscaledTime);
        }

        public void ConfigureForTests(
            LayerMask targetMask,
            LayerMask blockerMask,
            bool refreshAutomatically = false)
        {
            interactionMask = targetMask;
            lineOfSightBlockMask = blockerMask;
            autoRefresh = refreshAutomatically;
        }

        private bool IsLineOfSightBlocked(Vector2 origin, Vector2 offset)
        {
            float distance = offset.magnitude;
            if (distance <= Mathf.Epsilon || lineOfSightBlockMask.value == 0)
            {
                return false;
            }

            RaycastHit2D hit = Physics2D.Raycast(
                origin,
                offset / distance,
                distance,
                lineOfSightBlockMask);
            return hit.collider != null && hit.distance < distance - 0.001f;
        }

        private void SetSelection(InteractionCandidate next, float now)
        {
            if (next == selectedCandidate)
            {
                return;
            }

            InteractionCandidate previous = selectedCandidate;
            selectedCandidate = next;
            selectedAt = now;
            SelectionChanged?.Invoke(previous, next);
        }

        private void ApplyDefaultMasks()
        {
            interactionMask = LayerMask.GetMask("Interaction");
            lineOfSightBlockMask = LayerMask.GetMask("TerrainSolid", "UnbreakableBoundary");
        }
    }
}

#endif
