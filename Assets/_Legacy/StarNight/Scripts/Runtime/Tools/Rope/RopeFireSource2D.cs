#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeFireSource2D : MonoBehaviour
    {
        private readonly List<RopeSegment2D> segmentSnapshot =
            new List<RopeSegment2D>();

        [SerializeField] private bool isBurning = true;
        [SerializeField] private Collider2D fireCollider;

        public bool IsBurning => isBurning;

        private void Awake()
        {
            if (fireCollider == null)
            {
                fireCollider = GetComponent<Collider2D>();
            }
        }

        private void FixedUpdate()
        {
            BreakOverlappingRopesForTests();
        }

        public void Configure(
            bool burning,
            Collider2D configuredCollider = null)
        {
            isBurning = burning;
            fireCollider = configuredCollider != null
                ? configuredCollider
                : GetComponent<Collider2D>();
        }

        public int BreakOverlappingRopesForTests()
        {
            if (!isBurning
                || fireCollider == null
                || !fireCollider.enabled)
            {
                return 0;
            }

            int broken = 0;
            segmentSnapshot.Clear();
            segmentSnapshot.AddRange(RopeSegment2D.ActiveSegments);
            for (int index = 0; index < segmentSnapshot.Count; index++)
            {
                RopeSegment2D segment = segmentSnapshot[index];
                if (segment == null
                    || !segment.IsClimbable
                    || segment.Trigger == null
                    || !segment.Trigger.enabled)
                {
                    continue;
                }

                ColliderDistance2D distance = Physics2D.Distance(
                    fireCollider,
                    segment.Trigger);
                if (!distance.isOverlapped)
                {
                    continue;
                }

                RopeInstallation2D installation = segment.Installation;
                if (installation != null
                    && installation.Break(RopeDamageKind.Fire, this))
                {
                    broken++;
                }
            }

            segmentSnapshot.Clear();
            return broken;
        }
    }
}

#endif
