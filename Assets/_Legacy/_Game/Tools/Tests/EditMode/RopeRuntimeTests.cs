#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Interaction.Carry;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Map;
using StarNight.Tools.Rope;
using UnityEngine;

namespace StarNight.Tools.Tests
{
    public sealed class RopeRuntimeTests
    {
        private RopeDefinition definition;
        private readonly List<Object> createdAssets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<RopeDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (RopeActionController controller in Object.FindObjectsByType<RopeActionController>(FindObjectsSortMode.None))
            {
                if (controller != null)
                {
                    Object.DestroyImmediate(controller.gameObject);
                }
            }
            foreach (RopeInstallationRuntime installation in Object.FindObjectsByType<RopeInstallationRuntime>(FindObjectsSortMode.None))
            {
                if (installation != null)
                {
                    Object.DestroyImmediate(installation.gameObject);
                }
            }
            foreach (RopeSegmentRuntime segment in Object.FindObjectsByType<RopeSegmentRuntime>(FindObjectsSortMode.None))
            {
                if (segment != null)
                {
                    Object.DestroyImmediate(segment.gameObject);
                }
            }
            foreach (RopeClimbController climb in Object.FindObjectsByType<RopeClimbController>(FindObjectsSortMode.None))
            {
                if (climb != null)
                {
                    Object.DestroyImmediate(climb.gameObject);
                }
            }
            for (int index = 0; index < createdAssets.Count; index++)
            {
                if (createdAssets[index] != null)
                {
                    Object.DestroyImmediate(createdAssets[index]);
                }
            }
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void OpenColumnCreatesSixCellStarKnotPlan()
        {
            var world = new FakeRopeWorld();
            var resolver = new RopePlacementResolver();

            bool accepted = resolver.TryResolve(
                Vector2Int.zero,
                definition,
                world,
                out RopePlacementPlan plan,
                out RopePlacementFailure failure);

            Assert.That(accepted, Is.True);
            Assert.That(failure, Is.EqualTo(RopePlacementFailure.None));
            Assert.That(plan.AnchorKind, Is.EqualTo(RopeAnchorKind.StarKnot));
            Assert.That(plan.AnchorCell, Is.EqualTo(new Vector2Int(0, 6)));
            Assert.That(plan.SegmentCells, Has.Length.EqualTo(6));
            Assert.That(plan.SegmentCells[5], Is.EqualTo(new Vector2Int(0, 1)));
        }

        [Test]
        public void CeilingAndCommonAnchorStopBeforeSolidFloor()
        {
            var resolver = new RopePlacementResolver();
            var ceilingWorld = new FakeRopeWorld();
            ceilingWorld.Solid.Add(new Vector2Int(0, 4));
            ceilingWorld.Solid.Add(new Vector2Int(0, 0));
            Assert.That(resolver.TryResolve(
                Vector2Int.zero,
                definition,
                ceilingWorld,
                out RopePlacementPlan ceiling,
                out _), Is.True);
            Assert.That(ceiling.AnchorKind, Is.EqualTo(RopeAnchorKind.Ceiling));
            Assert.That(ceiling.AnchorCell, Is.EqualTo(new Vector2Int(0, 3)));
            Assert.That(ceiling.SegmentCells, Has.Length.EqualTo(3));

            var commonWorld = new FakeRopeWorld();
            commonWorld.Anchors.Add(new Vector2Int(0, 3));
            commonWorld.Solid.Add(Vector2Int.zero);
            Assert.That(resolver.TryResolve(
                Vector2Int.zero,
                definition,
                commonWorld,
                out RopePlacementPlan common,
                out _), Is.True);
            Assert.That(common.AnchorKind, Is.EqualTo(RopeAnchorKind.CommonAnchor));
            Assert.That(common.SegmentCells, Is.EqualTo(new[]
            {
                new Vector2Int(0, 2),
                new Vector2Int(0, 1),
            }));
        }

        [Test]
        public void EveryApprovedInvalidSpaceIsRejected()
        {
            var resolver = new RopePlacementResolver();
            AssertFailure(resolver, WorldWithSolid(new Vector2Int(0, 2)), RopePlacementFailure.InsufficientClearance);

            var outside = new FakeRopeWorld { Bounds = new RectInt(-2, -2, 4, 7) };
            AssertFailure(resolver, outside, RopePlacementFailure.AnchorOutsideRoom);

            var laser = new FakeRopeWorld();
            laser.Lasers.Add(new Vector2Int(0, 6));
            AssertFailure(resolver, laser, RopePlacementFailure.ActiveLaser);

            var portal = new FakeRopeWorld();
            portal.Portals.Add(new Vector2Int(0, 6));
            AssertFailure(resolver, portal, RopePlacementFailure.PortalBoundary);

            var existing = new FakeRopeWorld { ExistingColumn = true };
            AssertFailure(resolver, existing, RopePlacementFailure.ExistingRopeColumn);
        }

        [Test]
        public void ValidInstallConsumesOnceAndExistingColumnConsumesNothingMore()
        {
            var player = new GameObject("RopeActionPlayer");
            player.transform.position = new Vector2(5f, 0f);
            RopeInventoryState inventory = player.AddComponent<RopeInventoryState>();
            inventory.Configure(definition);
            PlayerActionLock actionLock = player.AddComponent<PlayerActionLock>();
            RopeActionController controller = player.AddComponent<RopeActionController>();
            var installationPrefabObject = new GameObject("RopeInstallationPrefab");
            RopeInstallationRuntime installationPrefab = installationPrefabObject.AddComponent<RopeInstallationRuntime>();
            var segmentPrefab = new GameObject("RopeSegmentPrefab");
            segmentPrefab.AddComponent<RopeSegmentRuntime>();
            var world = new FakeRopeWorld();
            controller.ConfigureForTests(
                definition,
                inventory,
                installationPrefab,
                segmentPrefab,
                actionLock,
                world,
                player.transform);

            Assert.That(controller.TryPlaceRope(new PlayerActionContext(101, 0f, 0f, false)), Is.True);
            Assert.That(inventory.Remaining, Is.EqualTo(3));
            Assert.That(controller.TryPlaceRope(new PlayerActionContext(102, 0f, 0f, false)), Is.True);
            Assert.That(inventory.Remaining, Is.EqualTo(3));
        }

        [Test]
        public void InvalidInstallDoesNotConsumeRope()
        {
            var player = new GameObject("RopeInvalidActionPlayer");
            player.transform.position = new Vector2(5f, 0f);
            RopeInventoryState inventory = player.AddComponent<RopeInventoryState>();
            inventory.Configure(definition);
            RopeActionController controller = player.AddComponent<RopeActionController>();
            var installationPrefabObject = new GameObject("RopeInvalidInstallationPrefab");
            RopeInstallationRuntime installationPrefab = installationPrefabObject.AddComponent<RopeInstallationRuntime>();
            var segmentPrefab = new GameObject("RopeInvalidSegmentPrefab");
            segmentPrefab.AddComponent<RopeSegmentRuntime>();
            controller.ConfigureForTests(
                definition,
                inventory,
                installationPrefab,
                segmentPrefab,
                null,
                WorldWithSolid(new Vector2Int(5, 2)),
                player.transform);

            Assert.That(controller.TryPlaceRope(new PlayerActionContext(201, 0f, 0f, false)), Is.False);
            Assert.That(inventory.Remaining, Is.EqualTo(4));
        }

        [Test]
        public void BrokenMiddleSegmentDropsLowerPartAndSnapshotKeepsUpperPart()
        {
            RopeInstallationRuntime installation = CreateInstallation(new[]
            {
                new Vector2Int(0, 3),
                new Vector2Int(0, 2),
                new Vector2Int(0, 1),
            });

            Assert.That(installation.BreakAt(1), Is.True);
            Assert.That(installation.Segments[0].IsAttached, Is.True);
            Assert.That(installation.Segments[1].IsAttached, Is.False);
            Assert.That(installation.Segments[2].IsAttached, Is.False);
            RopeSnapshot snapshot = installation.CaptureSnapshot();
            Assert.That(snapshot.RemainingSegmentCells, Is.EqualTo(new[] { new Vector2Int(0, 3) }));
        }

        [Test]
        public void BombReactionBreaksRopeAndClimbUsesFourCellsPerSecond()
        {
            RopeInstallationRuntime installation = CreateInstallation(new[] { new Vector2Int(0, 1) });
            RopeSegmentRuntime segment = installation.Segments[0];
            ToolReactionResult reaction = segment.TryReact(new ToolReactionContext
            {
                ActionId = 301,
                Tags = ToolTag.Bomb | ToolTag.HeavyImpact,
            });
            Assert.That(reaction.Accepted, Is.True);
            Assert.That(segment.IsAttached, Is.False);

            var player = new GameObject("RopeClimbPlayer");
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            PlayerActionLock actionLock = player.AddComponent<PlayerActionLock>();
            RopeClimbController climb = player.AddComponent<RopeClimbController>();
            var climbSegmentObject = new GameObject("AttachedClimbSegment");
            RopeSegmentRuntime climbSegment = climbSegmentObject.AddComponent<RopeSegmentRuntime>();
            climbSegment.Configure(null, 0, Vector2Int.zero);
            climb.ConfigureForTests(definition, body, null, actionLock);

            Assert.That(climb.TryBeginClimb(climbSegment), Is.True);
            climb.SetRopeInput(1f, 1f);
            climb.ApplyMovementOverride(body, 0.02f);
            Assert.That(body.linearVelocity.y, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(Mathf.Abs(body.linearVelocity.x), Is.LessThanOrEqualTo(4f));
            Assert.That(climb.TryJumpExit(), Is.True);
            Assert.That(body.linearVelocity.y, Is.EqualTo(6.5f).Within(0.0001f));
        }

        private RopeInstallationRuntime CreateInstallation(Vector2Int[] cells)
        {
            var installationObject = new GameObject("RopeInstallationTest");
            RopeInstallationRuntime installation = installationObject.AddComponent<RopeInstallationRuntime>();
            var segmentPrefab = new GameObject("RopeSegmentTestPrefab");
            segmentPrefab.AddComponent<RopeSegmentRuntime>();
            Assert.That(installation.Initialize(
                definition,
                new RopePlacementPlan(RopeAnchorKind.StarKnot, new Vector2Int(0, 4), cells),
                segmentPrefab,
                null,
                null,
                Vector2.zero,
                401), Is.True);
            installation.CompleteInstallationImmediately();
            return installation;
        }

        private void AssertFailure(
            RopePlacementResolver resolver,
            FakeRopeWorld world,
            RopePlacementFailure expected)
        {
            Assert.That(resolver.TryResolve(
                Vector2Int.zero,
                definition,
                world,
                out _,
                out RopePlacementFailure failure), Is.False);
            Assert.That(failure, Is.EqualTo(expected));
        }

        private static FakeRopeWorld WorldWithSolid(Vector2Int cell)
        {
            var world = new FakeRopeWorld();
            world.Solid.Add(cell);
            return world;
        }

        private sealed class FakeRopeWorld : IRopePlacementWorld
        {
            public RectInt Bounds = new RectInt(-10, -10, 20, 20);
            public readonly HashSet<Vector2Int> Solid = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> Anchors = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> Lasers = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> Portals = new HashSet<Vector2Int>();
            public bool ExistingColumn;

            public bool IsInsideRoom(Vector2Int cell) => Bounds.Contains(cell);
            public bool IsSolid(Vector2Int cell) => Solid.Contains(cell);
            public bool HasCommonRopeAnchor(Vector2Int cell) => Anchors.Contains(cell);
            public bool IsActiveLaser(Vector2Int cell) => Lasers.Contains(cell);
            public bool IsPortalBoundary(Vector2Int cell) => Portals.Contains(cell);
            public bool HasRopeInColumn(int columnX) => ExistingColumn;
        }
    }
}

#endif
