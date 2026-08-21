#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Interaction.State;
using UnityEngine;

namespace StarNight.Interaction.Carry
{
    public enum CarryPlacementFailure
    {
        None,
        OutsideRoom,
        Blocked,
        Unsupported,
        PortalGap,
        Void,
        NoCandidate,
    }

    public readonly struct CarryPlacementRequest
    {
        public CarryPlacementRequest(
            Vector2Int footCell,
            int facingSign,
            Vector2Int footprint,
            Vector2 gridOrigin,
            float cellSize = 1f)
        {
            FootCell = footCell;
            FacingSign = facingSign < 0 ? -1 : 1;
            Footprint = new Vector2Int(1, Mathf.Clamp(footprint.y, 1, 2));
            GridOrigin = gridOrigin;
            CellSize = Mathf.Max(0.01f, cellSize);
        }

        public Vector2Int FootCell { get; }
        public int FacingSign { get; }
        public Vector2Int Footprint { get; }
        public Vector2 GridOrigin { get; }
        public float CellSize { get; }
    }

    public readonly struct CarryPlacementResult
    {
        public CarryPlacementResult(bool success, Vector2Int baseCell, Vector2 worldPosition, CarryPlacementFailure failure)
        {
            Success = success;
            BaseCell = baseCell;
            WorldPosition = worldPosition;
            Failure = failure;
        }

        public bool Success { get; }
        public Vector2Int BaseCell { get; }
        public Vector2 WorldPosition { get; }
        public CarryPlacementFailure Failure { get; }
    }

    public interface ICarryPlacementWorld
    {
        bool IsInsideRoom(RectInt footprint);
        bool IsFootprintClear(RectInt footprint);
        bool HasStableSupport(RectInt footprint);
        bool IsPortalGap(RectInt footprint);
        bool IsVoid(RectInt footprint);
    }

    public sealed class CarryPlacementResolver
    {
        public bool TryResolve(
            CarryPlacementRequest request,
            ICarryPlacementWorld world,
            out CarryPlacementResult result)
        {
            if (world == null)
            {
                result = new CarryPlacementResult(false, default, default, CarryPlacementFailure.NoCandidate);
                return false;
            }

            Vector2Int front = request.FootCell + Vector2Int.right * request.FacingSign;
            Vector2Int[] candidates =
            {
                front,
                request.FootCell,
                request.FootCell - Vector2Int.right * request.FacingSign,
                front + Vector2Int.down,
            };
            CarryPlacementFailure lastFailure = CarryPlacementFailure.NoCandidate;
            for (int index = 0; index < candidates.Length; index++)
            {
                Vector2Int baseCell = candidates[index];
                RectInt footprint = new RectInt(baseCell, request.Footprint);
                if (!world.IsInsideRoom(footprint))
                {
                    lastFailure = CarryPlacementFailure.OutsideRoom;
                    continue;
                }

                if (world.IsPortalGap(footprint))
                {
                    lastFailure = CarryPlacementFailure.PortalGap;
                    continue;
                }

                if (world.IsVoid(footprint))
                {
                    lastFailure = CarryPlacementFailure.Void;
                    continue;
                }

                if (!world.IsFootprintClear(footprint))
                {
                    lastFailure = CarryPlacementFailure.Blocked;
                    continue;
                }

                if (!world.HasStableSupport(footprint))
                {
                    lastFailure = CarryPlacementFailure.Unsupported;
                    continue;
                }

                Vector2 offset = new Vector2(
                    (request.Footprint.x - 1) * 0.5f,
                    (request.Footprint.y - 1) * 0.5f);
                Vector2 worldPosition = request.GridOrigin
                    + ((Vector2)baseCell + offset) * request.CellSize;
                result = new CarryPlacementResult(true, baseCell, worldPosition, CarryPlacementFailure.None);
                return true;
            }

            result = new CarryPlacementResult(false, default, default, lastFailure);
            return false;
        }
    }

    public sealed class PhysicsCarryPlacementWorld : ICarryPlacementWorld
    {
        private readonly RectInt roomBounds;
        private readonly Vector2 gridOrigin;
        private readonly float cellSize;
        private readonly ProjectPhysicsProfile physicsProfile;
        private readonly Collider2D[] overlaps = new Collider2D[16];

        public PhysicsCarryPlacementWorld(
            RectInt bounds,
            Vector2 origin,
            float size,
            ProjectPhysicsProfile profile)
        {
            roomBounds = bounds;
            gridOrigin = origin;
            cellSize = Mathf.Max(0.01f, size);
            physicsProfile = profile;
        }

        public bool IsInsideRoom(RectInt footprint)
        {
            return roomBounds.width <= 0 || roomBounds.height <= 0
                || roomBounds.Contains(footprint.min)
                && roomBounds.Contains(footprint.max - Vector2Int.one);
        }

        public bool IsFootprintClear(RectInt footprint)
        {
            return !HasOverlap(footprint, physicsProfile != null ? physicsProfile.DropBlockMask : 0);
        }

        public bool HasStableSupport(RectInt footprint)
        {
            if (physicsProfile == null || physicsProfile.GroundMask.value == 0)
            {
                return true;
            }

            Vector2 center = CellCenter(footprint);
            Vector2 supportCenter = new Vector2(
                center.x,
                gridOrigin.y + (footprint.yMin - 0.51f) * cellSize);
            Vector2 supportSize = new Vector2(footprint.width * cellSize * 0.90f, cellSize * 0.08f);
            return Physics2D.OverlapBox(supportCenter, supportSize, 0f, physicsProfile.GroundMask) != null;
        }

        public bool IsPortalGap(RectInt footprint)
        {
            return HasOverlap(footprint, physicsProfile != null ? physicsProfile.PortalBoundaryMask : 0);
        }

        public bool IsVoid(RectInt footprint)
        {
            return HasOverlap(footprint, physicsProfile != null ? physicsProfile.VoidRecoveryMask : 0);
        }

        private bool HasOverlap(RectInt footprint, LayerMask mask)
        {
            if (mask.value == 0)
            {
                return false;
            }

            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(mask);
            Vector2 size = new Vector2(
                footprint.width * cellSize * 0.96f,
                footprint.height * cellSize * 0.96f);
            return Physics2D.OverlapBox(CellCenter(footprint), size, 0f, filter, overlaps) > 0;
        }

        private Vector2 CellCenter(RectInt footprint)
        {
            return gridOrigin + new Vector2(
                footprint.xMin + (footprint.width - 1) * 0.5f,
                footprint.yMin + (footprint.height - 1) * 0.5f) * cellSize;
        }
    }
}

#endif
