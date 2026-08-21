#if LEGACY_DISABLED
using System;
using NUnit.Framework;
using StarNight.Player.Safety;
using StarNight.Stage.Lab;
using StarNight.Stage.Layout;
using StarNight.Stage.CameraSystem;
using StarNight.Stage.Rooms;
using StarNight.Stage.Transitions;
using StarNight.Stage.Validation;
using StarNight.Stage.Visuals;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Stage.Tests
{
    public sealed class RoomRuntimeContractTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("RoomRuntimeContractTests");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void RoomStateAndTransitionTimingMatchCore04Contract()
        {
            CollectionAssert.AreEqual(
                new[] { "Dormant", "NeighborPreview", "TransitionTarget", "Active", "ResidualSimulation", "Frozen" },
                Enum.GetNames(typeof(RoomSimulationState)));
            Assert.That(RoomTransitionController.HorizontalInputLockSeconds, Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(RoomCameraController.DefaultTransitionSeconds, Is.EqualTo(0.22f).Within(0.0001f));
        }

        [Test]
        public void RoomSafetyNodesInstallCore01PlayerContracts()
        {
            BuildLinkedRooms(out RoomRuntime roomA, out _, out _, out _);

            PlayerRecoveryZoneRelay voidRelay =
                roomA.VoidRecoveryZone.GetComponent<PlayerRecoveryZoneRelay>();
            PlayerRecoveryZoneRelay hardFailRelay =
                roomA.HardFailSafePlane.GetComponent<PlayerRecoveryZoneRelay>();
            PlayerSafeCell2D safeCell =
                roomA.SafeCellRoot.GetComponentInChildren<PlayerSafeCell2D>(true);

            Assert.That(voidRelay, Is.Not.Null);
            Assert.That(voidRelay.RecoveryCause, Is.EqualTo(PlayerRecoveryCause.VoidRecoveryZone));
            Assert.That(hardFailRelay, Is.Not.Null);
            Assert.That(hardFailRelay.RecoveryCause, Is.EqualTo(PlayerRecoveryCause.HardFailSafePlane));
            Assert.That(safeCell, Is.Not.Null);
            Assert.That(safeCell.State.IsValid, Is.True);
        }

        [Test]
        public void CameraTileProfileMatchesGCore02FrameAndClampModes()
        {
            var profile = new CameraTileProfile();

            CollectionAssert.AreEqual(
                new[]
                {
                    "Fixed",
                    "BoundedX",
                    "BoundedY",
                    "BoundedXY",
                    "BoundedXAnchors",
                    "BoundedYAnchors",
                    "BoundedXYAnchors",
                },
                Enum.GetNames(typeof(RoomCameraMode)));
            Assert.That(profile.OrthographicSize, Is.EqualTo(5.5f));
            Assert.That(profile.VisibleWidthTiles, Is.EqualTo(19.5556f).Within(0.001f));
            Assert.That(profile.criticalWidthTiles, Is.EqualTo(18f));
            Assert.That(profile.criticalHeightTiles, Is.EqualTo(10f));
            Assert.That(profile.ResolveMode(new Vector2Int(20, 11)), Is.EqualTo(RoomCameraMode.Fixed));
            Assert.That(profile.ResolveMode(new Vector2Int(21, 11)), Is.EqualTo(RoomCameraMode.BoundedX));
            Assert.That(profile.ResolveMode(new Vector2Int(20, 12)), Is.EqualTo(RoomCameraMode.BoundedY));
            Assert.That(profile.ResolveMode(new Vector2Int(21, 12)), Is.EqualTo(RoomCameraMode.BoundedXY));
        }

        [Test]
        public void CameraViewportPreservesSixteenByNineWithoutExtraGameplaySight()
        {
            var profile = new CameraTileProfile();
            Rect wide = profile.CalculateViewportRect(21f / 9f);
            Rect narrow = profile.CalculateViewportRect(4f / 3f);

            Assert.That(wide.width, Is.LessThan(1f));
            Assert.That(wide.height, Is.EqualTo(1f));
            Assert.That(narrow.width, Is.EqualTo(1f));
            Assert.That(narrow.height, Is.LessThan(1f));
        }

        [Test]
        public void CameraClampsToCurrentRoomAndDoesNotRevealAdjacentRoom()
        {
            BuildLinkedRooms(out RoomRuntime roomA, out RoomRuntime roomB, out RoomPortal2D portalA, out _);
            GameObject cameraObject = new GameObject("GCORE02_Camera");
            cameraObject.transform.SetParent(root.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            RoomCameraController controller = cameraObject.AddComponent<RoomCameraController>();
            controller.Configure(camera);

            controller.SnapToRoom(roomA, new Vector2(roomA.WorldBounds.xMax, roomA.WorldBounds.center.y));

            float viewportRight = camera.transform.position.x + controller.TileProfile.VisibleWidthTiles * 0.5f;
            Assert.That(camera.orthographicSize, Is.EqualTo(5.5f));
            Assert.That(viewportRight, Is.LessThanOrEqualTo(roomA.WorldBounds.xMax + 0.001f));
            Assert.That(controller.IsViewportInside(roomA), Is.True);

            RoomThemeOcclusionMask themeMask = roomA.GetComponentInChildren<RoomThemeOcclusionMask>(true);
            Assert.That(themeMask, Is.Not.Null);
            Assert.That(themeMask.CoversRoom(), Is.True);
            Assert.That(portalA.GetComponentInChildren<PortalFacade>(true), Is.Not.Null);

            roomA.SetSimulationState(RoomSimulationState.Active);
            roomB.SetSimulationState(RoomSimulationState.NeighborPreview);
            Assert.That(roomB.GridVisual.gameObject.activeSelf, Is.False);

            GameObject objective = new GameObject("CriticalObjective");
            objective.transform.SetParent(roomA.transform, false);
            objective.transform.position = camera.transform.position;
            objective.AddComponent<CameraCriticalTarget>().Configure(CameraCriticalTargetKind.Objective);
            Assert.That(controller.AreCriticalTargetsInside(roomA), Is.True);
        }

        [Test]
        public void CellCoordinatesUseOneByOneCenterContract()
        {
            GameObject gridObject = new GameObject("GridLogic");
            gridObject.transform.SetParent(root.transform, false);
            gridObject.transform.position = new Vector3(-8f, -4f, 0f);
            RoomGridTransform grid = gridObject.AddComponent<RoomGridTransform>();

            Assert.That(grid.CellToWorld(new Vector2Int(0, 0)), Is.EqualTo(new Vector3(-7.5f, -3.5f, 0f)));
            Assert.That(grid.WorldToCell(new Vector3(-6.1f, -1.9f, 0f)), Is.EqualTo(new Vector2Int(1, 2)));
        }

        [Test]
        public void TwoRoomsOwnIndependentGridsAndTilemaps()
        {
            BuildLinkedRooms(out RoomRuntime roomA, out RoomRuntime roomB, out _, out _);
            Grid gridA = roomA.transform.Find("GridLogic").GetComponent<Grid>();
            Grid gridB = roomB.transform.Find("GridLogic").GetComponent<Grid>();
            Tilemap terrainA = roomA.transform.Find("GridLogic/TerrainCollisionTilemap").GetComponent<Tilemap>();
            Tilemap terrainB = roomB.transform.Find("GridLogic/TerrainCollisionTilemap").GetComponent<Tilemap>();

            Assert.That(gridA, Is.Not.Null);
            Assert.That(gridB, Is.Not.Null.And.Not.SameAs(gridA));
            Assert.That(gridA.cellSize, Is.EqualTo(Vector3.one));
            Assert.That(terrainA, Is.Not.Null);
            Assert.That(terrainB, Is.Not.Null.And.Not.SameAs(terrainA));
            Assert.That(roomA.CameraMode, Is.EqualTo(RoomCameraMode.BoundedX));
            Assert.That(roomB.CameraMode, Is.EqualTo(RoomCameraMode.BoundedX));
        }

        [Test]
        public void ValidPrototypeRoomsAndConnectionAreApproved()
        {
            BuildLinkedRooms(out RoomRuntime roomA, out RoomRuntime roomB, out RoomPortal2D portalA, out RoomPortal2D portalB);

            Assert.That(RoomGeometryValidator.ValidateAndApply(roomA).IsApproved, Is.True);
            Assert.That(RoomGeometryValidator.ValidateAndApply(roomB).IsApproved, Is.True);
            Assert.That(RoomGeometryValidator.ValidateConnection(portalA, portalB).IsApproved, Is.True);
            Assert.That(roomA.GeometryApproved, Is.True);
            Assert.That(roomB.GeometryApproved, Is.True);
            Assert.That(RoomPortalContract.SocketWidthCells, Is.EqualTo(1));
            Assert.That(RoomPortalContract.InteriorClearanceCells, Is.EqualTo(1));
            Assert.That(RoomPortalContract.EntrySafeFloorWidthCells, Is.EqualTo(2));
            Assert.That(RoomPortalContract.PortalPaddingCells, Is.EqualTo(2));
            Assert.That(RoomPortalContract.PlayerColliderWidthCells, Is.EqualTo(0.72f));
            Assert.That(RoomPortalContract.PlayerColliderHeightCells, Is.EqualTo(0.92f));
            Assert.That(portalA.HasProtectedSafeFloor && portalB.HasProtectedSafeFloor, Is.True);

            var graph = new StageRoomGraph();
            Assert.That(graph.AddRoom(roomA, true), Is.True);
            Assert.That(graph.AddRoom(roomB), Is.True);
            var edge = new RoomEdge
            {
                EdgeId = "EDGE_A_B",
                FromNodeId = roomA.RoomId,
                ToNodeId = roomB.RoomId,
                FromSocket = portalA.PortalId,
                ToSocket = portalB.PortalId,
                EdgeType = RoomEdgeType.PortalPair,
            };
            Assert.That(graph.Connect(edge), Is.True);
            Assert.That(graph.EdgeCount, Is.EqualTo(1));
            Assert.That(graph.AreAdjacent(roomA.RoomId, roomB.RoomId), Is.True);
            Assert.That(graph.AreAdjacent(roomB.RoomId, roomA.RoomId), Is.True);
            Assert.That(graph.TryGetEdge(edge.EdgeId, out RoomEdge stored), Is.True);
            Assert.That(stored.Bidirectional, Is.True);
        }

        [Test]
        public void MissingBoundaryAndRecoveryZoneAreRejected()
        {
            BuildLinkedRooms(out RoomRuntime roomA, out _, out _, out _);
            UnityEngine.Object.DestroyImmediate(roomA.transform.Find("GridLogic/UnbreakableBoundaryTilemap").gameObject);
            UnityEngine.Object.DestroyImmediate(roomA.transform.Find("VoidRecoveryRoot/VoidRecoveryZone").gameObject);

            RoomValidationReport report = RoomGeometryValidator.ValidateAndApply(roomA);

            Assert.That(report.IsApproved, Is.False);
            Assert.That(report.Errors, Has.Some.Contains("UnbreakableBoundaryTilemap"));
            Assert.That(report.Errors, Has.Some.Contains("VoidRecoveryZone"));
            Assert.That(roomA.GeometryApproved, Is.False);
        }

        [Test]
        public void FrozenRoomRestoresPersistentDynamicTransformOnReentry()
        {
            BuildLinkedRooms(out RoomRuntime roomA, out _, out _, out _);
            RoomPersistentTransform2D persistent = roomA.GetComponentInChildren<RoomPersistentTransform2D>(true);
            Rigidbody2D body = persistent.GetComponent<Rigidbody2D>();
            Vector2 savedPosition = new Vector2(-11.25f, -1.75f);

            roomA.SetSimulationState(RoomSimulationState.Active);
            body.position = savedPosition;
            body.transform.position = savedPosition;
            roomA.SetSimulationState(RoomSimulationState.Frozen);
            body.position = new Vector2(99f, 99f);
            roomA.SetSimulationState(RoomSimulationState.Active);

            Assert.That(body.position.x, Is.EqualTo(savedPosition.x).Within(0.001f));
            Assert.That(body.position.y, Is.EqualTo(savedPosition.y).Within(0.001f));
            Assert.That(roomA.PersistentState.Revision, Is.EqualTo(1));
        }

        private void BuildLinkedRooms(
            out RoomRuntime roomA,
            out RoomRuntime roomB,
            out RoomPortal2D portalA,
            out RoomPortal2D portalB)
        {
            roomA = Core04TwoRoomLab.BuildPrototypeRoom(
                root.transform,
                "Room_A",
                new Vector2(-Core04TwoRoomLab.RoomWidth, -Core04TwoRoomLab.RoomHeight * 0.5f),
                Color.cyan,
                false,
                true,
                out portalA);
            roomB = Core04TwoRoomLab.BuildPrototypeRoom(
                root.transform,
                "Room_B",
                new Vector2(0f, -Core04TwoRoomLab.RoomHeight * 0.5f),
                Color.magenta,
                true,
                false,
                out portalB);
            portalA.Link(portalB);
            portalB.Link(portalA);
        }
    }
}

#endif
