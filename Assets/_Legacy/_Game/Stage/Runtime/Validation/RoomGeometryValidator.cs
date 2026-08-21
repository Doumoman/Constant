#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Stage.Rooms;
using StarNight.Stage.Transitions;
using StarNight.Stage.Visuals;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Stage.Validation
{
    public sealed class RoomValidationReport
    {
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public bool IsApproved => errors.Count == 0;

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                errors.Add(error);
            }
        }
    }

    public static class RoomGeometryValidator
    {
        public const float StaticAlignmentTolerance = 0.001f;
        public const float ColliderTolerance = 0.01f;
        public const float PortalFloorHeightTolerance = 0.02f;

        private static readonly string[] RequiredPaths =
        {
            "Metadata",
            "GridLogic",
            "GridLogic/TerrainCollisionTilemap",
            "GridLogic/OneWayCollisionTilemap",
            "GridLogic/UnbreakableBoundaryTilemap",
            "GridLogic/HazardLogicTilemap",
            "GridLogic/InteractionLogicTilemap",
            "GridVisual",
            "CameraBounds",
            "CameraAnchors",
            "PortalRoot",
            "SpawnRoot",
            "DynamicRoot",
            "ElementSlotRoot",
            "SignalLinkRoot",
            "SafeCellRoot",
            "VoidRecoveryRoot",
            "VoidRecoveryRoot/VoidRecoveryZone",
            "VoidRecoveryRoot/HardFailSafePlane",
            "MaruLaneRoot",
            "AudioZone",
            "DebugRoot",
        };

        public static RoomValidationReport Validate(RoomRuntime room)
        {
            RoomValidationReport report = new RoomValidationReport();
            if (room == null)
            {
                report.AddError("RoomRuntime is missing.");
                return report;
            }

            if (!room.IsInitialized || string.IsNullOrWhiteSpace(room.RoomId))
            {
                report.AddError("Room metadata is not initialized.");
            }

            for (int index = 0; index < RequiredPaths.Length; index++)
            {
                if (room.transform.Find(RequiredPaths[index]) == null)
                {
                    report.AddError($"Required RoomRoot child is missing: {RequiredPaths[index]}");
                }
            }

            ValidateGrid(room, report);
            ValidateBoundary(room, report);
            ValidateRecovery(room, report);
            ValidateSafeCells(room, report);
            ValidatePortals(room, report);
            ValidateThemeMask(room, report);
            return report;
        }

        public static RoomValidationReport ValidateAndApply(RoomRuntime room)
        {
            RoomValidationReport report = Validate(room);
            if (room != null)
            {
                room.SetGeometryApproval(report.IsApproved);
            }

            return report;
        }

        public static RoomValidationReport ValidateConnection(RoomPortal2D first, RoomPortal2D second)
        {
            RoomValidationReport report = new RoomValidationReport();
            if (first == null || second == null)
            {
                report.AddError("Both portal sockets are required.");
                return report;
            }

            if (first.DestinationPortal != second || second.DestinationPortal != first)
            {
                report.AddError("Portal sockets are not linked bidirectionally.");
            }

            if (!AreOpposite(first.Side, second.Side))
            {
                report.AddError("Portal socket directions are incompatible.");
            }

            if (Mathf.Abs(first.FloorHeightCell - second.FloorHeightCell) > PortalFloorHeightTolerance)
            {
                report.AddError("Portal floor heights exceed the 0.02 cell tolerance.");
            }

            return report;
        }

        private static void ValidateGrid(RoomRuntime room, RoomValidationReport report)
        {
            Transform gridLogic = room.transform.Find("GridLogic");
            if (gridLogic == null)
            {
                return;
            }

            Grid grid = gridLogic.GetComponent<Grid>();
            RoomGridTransform gridTransform = gridLogic.GetComponent<RoomGridTransform>();
            if (grid == null || gridTransform == null)
            {
                report.AddError("GridLogic requires independent Grid and RoomGridTransform components.");
            }
            else if (Vector3.Distance(grid.cellSize, Vector3.one) > StaticAlignmentTolerance)
            {
                report.AddError("Grid cell size must be exactly 1x1.");
            }

            string[] tilemapNames =
            {
                "TerrainCollisionTilemap",
                "OneWayCollisionTilemap",
                "UnbreakableBoundaryTilemap",
                "HazardLogicTilemap",
                "InteractionLogicTilemap",
            };
            for (int index = 0; index < tilemapNames.Length; index++)
            {
                Transform child = gridLogic.Find(tilemapNames[index]);
                if (child != null && child.GetComponent<Tilemap>() == null)
                {
                    report.AddError($"{tilemapNames[index]} requires its own Tilemap component.");
                }
            }
        }

        private static void ValidateBoundary(RoomRuntime room, RoomValidationReport report)
        {
            Transform boundary = room.transform.Find("GridLogic/UnbreakableBoundaryTilemap");
            if (boundary == null)
            {
                return;
            }

            BoxCollider2D[] colliders = boundary.GetComponentsInChildren<BoxCollider2D>(true);
            if (colliders.Length < 3)
            {
                report.AddError("UnbreakableBoundary must close the room exterior around portal openings.");
            }
        }

        private static void ValidateRecovery(RoomRuntime room, RoomValidationReport report)
        {
            if (room.VoidRecoveryZone == null || !room.VoidRecoveryZone.isTrigger)
            {
                report.AddError("VoidRecoveryZone requires a trigger collider.");
            }
            else if (room.VoidRecoveryZone.bounds.size.x + ColliderTolerance < room.WorldBounds.width)
            {
                report.AddError("VoidRecoveryZone must cover the full room width.");
            }

            if (room.HardFailSafePlane == null || !room.HardFailSafePlane.isTrigger)
            {
                report.AddError("HardFailSafePlane requires a trigger collider.");
            }
        }

        private static void ValidateSafeCells(RoomRuntime room, RoomValidationReport report)
        {
            if (room.SafeCellRoot == null || room.SafeCellRoot.childCount == 0)
            {
                report.AddError("At least one SafeCell is required.");
            }
        }

        private static void ValidatePortals(RoomRuntime room, RoomValidationReport report)
        {
            RoomPortal2D[] portals = room.GetComponentsInChildren<RoomPortal2D>(true);
            if (portals.Length == 0)
            {
                report.AddError("At least one room portal is required.");
                return;
            }

            for (int index = 0; index < portals.Length; index++)
            {
                RoomPortal2D portal = portals[index];
                if (portal.PreviewLine == null || portal.CommitLine == null || portal.PortalBoundary == null || portal.EntryAnchor == null)
                {
                    report.AddError($"Portal {portal.PortalId} is missing Preview, Commit, Boundary, or EntryAnchor.");
                }

                if (portal.EntrySafeFloor == null || portal.EntrySafeFloor.localScale.x + ColliderTolerance < 2f)
                {
                    report.AddError($"Portal {portal.PortalId} requires a two-cell EntrySafeFloor.");
                }
                else if (!portal.HasProtectedSafeFloor)
                {
                    report.AddError($"Portal {portal.PortalId} EntrySafeFloor must be explosion protected.");
                }

                GameplayClearZone clearZone = portal.GetComponentInChildren<GameplayClearZone>(true);
                if (clearZone == null || clearZone.SizeCells.x + ColliderTolerance < RoomPortalContract.PortalPaddingCells)
                {
                    report.AddError($"Portal {portal.PortalId} requires a two-cell hazard-free padding zone.");
                }
            }
        }

        private static void ValidateThemeMask(RoomRuntime room, RoomValidationReport report)
        {
            RoomThemeOcclusionMask mask = room.GetComponentInChildren<RoomThemeOcclusionMask>(true);
            if (mask == null || !mask.CoversRoom())
            {
                report.AddError("Room Theme requires a dedicated occlusion mask covering the full Room Bounds.");
            }
        }

        private static bool AreOpposite(CardinalDirection first, CardinalDirection second)
        {
            return (first == CardinalDirection.Left && second == CardinalDirection.Right) ||
                   (first == CardinalDirection.Right && second == CardinalDirection.Left) ||
                   (first == CardinalDirection.Up && second == CardinalDirection.Down) ||
                   (first == CardinalDirection.Down && second == CardinalDirection.Up);
        }
    }
}

#endif
