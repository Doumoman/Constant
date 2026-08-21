#if LEGACY_DISABLED
using System;
using StarNight.Interaction.Carry;
using StarNight.Interaction.State;
using UnityEngine;

namespace StarNight.Tools.HookLauncher
{
    public sealed class PhysicsHookWorld : IHookWorld
    {
        private readonly ProjectPhysicsProfile physicsProfile;

        public PhysicsHookWorld(ProjectPhysicsProfile configuredProfile)
        {
            physicsProfile = configuredProfile;
        }

        public bool TryAcquire(HookFireQuery query, out HookLatch latch)
        {
            latch = default;
            RaycastHit2D[] hits = Physics2D.RaycastAll(
                query.Origin,
                query.Direction,
                query.MaximumDistance);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int index = 0; index < hits.Length; index++)
            {
                Collider2D collider = hits[index].collider;
                if (collider == null
                    || query.Instigator != null
                    && collider.transform.IsChildOf(query.Instigator.transform))
                {
                    continue;
                }

                if (!Contains(query, hits[index].point))
                {
                    return false;
                }

                int layer = collider.gameObject.layer;
                if (layer == LayerMask.NameToLayer("UnbreakableBoundary")
                    || layer == LayerMask.NameToLayer("PortalBoundary"))
                {
                    return false;
                }

                HookTarget hookTarget = collider.GetComponentInParent<HookTarget>();
                if (hookTarget != null)
                {
                    latch = hookTarget.CreateLatch();
                    return latch.IsValid;
                }

                CarryableObject carryable = collider.GetComponentInParent<CarryableObject>();
                if (carryable != null && carryable.Definition != null)
                {
                    latch = new HookLatch(
                        carryable.gameObject,
                        carryable.Definition.HookResponse,
                        carryable.Body);
                    return true;
                }

                if (layer == LayerMask.NameToLayer("TerrainSolid")
                    || layer == LayerMask.NameToLayer("DynamicObject"))
                {
                    return false;
                }
            }
            return false;
        }

        public bool TryResolveStep(
            Vector2 current,
            Vector2 desired,
            Vector2 capsuleSize,
            GameObject mover,
            GameObject ignoredTarget,
            out Vector2 resolvedPosition)
        {
            resolvedPosition = current;
            Vector2 delta = desired - current;
            float distance = delta.magnitude;
            if (distance <= 0.0001f)
            {
                resolvedPosition = desired;
                return true;
            }

            int mask = physicsProfile != null && physicsProfile.DropBlockMask.value != 0
                ? physicsProfile.DropBlockMask.value
                : LayerMask.GetMask(
                    "TerrainSolid",
                    "UnbreakableBoundary",
                    "PortalBoundary",
                    "DynamicObject");
            RaycastHit2D[] hits = Physics2D.CapsuleCastAll(
                current,
                new Vector2(Mathf.Max(0.05f, capsuleSize.x), Mathf.Max(0.05f, capsuleSize.y)),
                CapsuleDirection2D.Vertical,
                0f,
                delta / distance,
                distance + 0.02f,
                mask);
            for (int index = 0; index < hits.Length; index++)
            {
                Collider2D collider = hits[index].collider;
                if (collider == null
                    || mover != null && collider.transform.IsChildOf(mover.transform)
                    || ignoredTarget != null && collider.transform.IsChildOf(ignoredTarget.transform))
                {
                    continue;
                }
                return false;
            }

            resolvedPosition = desired;
            return true;
        }

        private static bool Contains(HookFireQuery query, Vector2 worldPosition)
        {
            if (query.RoomBounds.width <= 0 || query.RoomBounds.height <= 0)
            {
                return true;
            }
            Vector2 local = (worldPosition - query.GridOrigin) / query.CellSize;
            var cell = new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
            return query.RoomBounds.Contains(cell);
        }
    }
}

#endif
