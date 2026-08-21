#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Grid;
using StarNight.Tiles;
using StarNight.Tools;
using StarNight.Tools.Grapple;
using StarNight.Tools.Mining;
using StarNight.Tools.Pestle;
using StarNight.Tools.Rope;
using StarNight.Tools.Water;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityGrid = UnityEngine.Grid;

namespace StarNight.Tests.EditMode
{
    public sealed class P3ToolsEditModeTests
    {
        [Test]
        public void DiscoveryGate_IsCalculatedPerToolAcrossParticipants()
        {
            List<P3ToolDiscoverySessionSnapshot> sessions =
                new List<P3ToolDiscoverySessionSnapshot>();
            for (int participant = 0; participant < 5; participant++)
            {
                P3ToolDiscoveryOutcome[] outcomes =
                    P3ToolGardenContract.ToolOrder
                        .Select(kind => new P3ToolDiscoveryOutcome(
                            kind,
                            true,
                            participant < 4))
                        .ToArray();
                sessions.Add(new P3ToolDiscoverySessionSnapshot(
                    $"P{participant + 1}",
                    outcomes));
            }

            IReadOnlyDictionary<P3ToolKind, float> rates =
                P3ToolDiscoveryCohortEvaluator
                    .CalculatePerToolParticipantRates(sessions);
            Assert.That(
                rates.Values.All(rate =>
                    Mathf.Abs(rate - 0.8f) < 0.0001f),
                Is.True);
            Assert.That(
                P3ToolDiscoveryCohortEvaluator.MeetsEveryToolGate(
                    sessions),
                Is.True);

            P3ToolDiscoverySessionSnapshot fourth = sessions[3];
            P3ToolDiscoveryOutcome[] failedGrapple =
                fourth.Outcomes
                    .Select(outcome => new P3ToolDiscoveryOutcome(
                        outcome.Kind,
                        outcome.WasSeen,
                        outcome.Kind == P3ToolKind.Grapple
                            ? false
                            : outcome.SucceededWithinThirtySeconds))
                    .ToArray();
            sessions[3] = new P3ToolDiscoverySessionSnapshot(
                fourth.ParticipantId,
                failedGrapple);

            Assert.That(
                P3ToolDiscoveryCohortEvaluator
                    .CalculatePerToolParticipantRates(sessions)[
                        P3ToolKind.Grapple],
                Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(
                P3ToolDiscoveryCohortEvaluator.MeetsEveryToolGate(
                    sessions),
                Is.False);
        }

        [Test]
        public void HandToolPickup_FiniteUsesDepleteAndUnlimitedUsesNeverDeplete()
        {
            GameObject finiteObject = new GameObject("P3_FiniteTool_Test");
            BoxCollider2D finiteCollider =
                finiteObject.AddComponent<BoxCollider2D>();
            HandToolPickup2D finite =
                finiteObject.AddComponent<HandToolPickup2D>();
            finite.Configure(
                HandToolKind.Pickaxe,
                PickaxeTool2D.DefaultDurability,
                finiteCollider);

            GameObject unlimitedObject =
                new GameObject("P3_UnlimitedTool_Test");
            BoxCollider2D unlimitedCollider =
                unlimitedObject.AddComponent<BoxCollider2D>();
            HandToolPickup2D unlimited =
                unlimitedObject.AddComponent<HandToolPickup2D>();
            unlimited.Configure(
                HandToolKind.Pestle,
                0,
                unlimitedCollider);

            try
            {
                int finiteChangeEvents = 0;
                int unlimitedChangeEvents = 0;
                finite.UsesChanged += (tool, uses) => finiteChangeEvents++;
                unlimited.UsesChanged += (tool, uses) =>
                    unlimitedChangeEvents++;

                Assert.That(finite.HasFiniteUses, Is.True);
                Assert.That(
                    finite.MaximumUses,
                    Is.EqualTo(PickaxeTool2D.DefaultDurability));
                for (int use = 0;
                    use < PickaxeTool2D.DefaultDurability;
                    use++)
                {
                    Assert.That(finite.TryConsumeUse(), Is.True);
                    Assert.That(
                        finite.RemainingUses,
                        Is.EqualTo(
                            PickaxeTool2D.DefaultDurability - use - 1));
                }

                Assert.That(finite.HasUsesRemaining, Is.False);
                Assert.That(finite.TryConsumeUse(), Is.False);
                Assert.That(
                    finiteChangeEvents,
                    Is.EqualTo(PickaxeTool2D.DefaultDurability));

                finite.Recharge();
                Assert.That(
                    finite.RemainingUses,
                    Is.EqualTo(PickaxeTool2D.DefaultDurability));
                finite.SetRemainingUses(999);
                Assert.That(
                    finite.RemainingUses,
                    Is.EqualTo(PickaxeTool2D.DefaultDurability));
                finite.SetRemainingUses(-1);
                Assert.That(finite.RemainingUses, Is.EqualTo(0));

                Assert.That(unlimited.HasFiniteUses, Is.False);
                Assert.That(unlimited.MaximumUses, Is.EqualTo(0));
                Assert.That(unlimited.RemainingUses, Is.EqualTo(0));
                for (int use = 0; use < 100; use++)
                {
                    Assert.That(unlimited.TryConsumeUse(), Is.True);
                }

                unlimited.SetRemainingUses(20);
                unlimited.Recharge();
                Assert.That(unlimited.HasUsesRemaining, Is.True);
                Assert.That(unlimited.RemainingUses, Is.EqualTo(0));
                Assert.That(unlimitedChangeEvents, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(finiteObject);
                Object.DestroyImmediate(unlimitedObject);
            }
        }

        [Test]
        public void GrappleAim_QuantizesEveryOctantToTheEightDirections()
        {
            Vector2[] expected =
            {
                Vector2.right,
                new Vector2(1f, 1f).normalized,
                Vector2.up,
                new Vector2(-1f, 1f).normalized,
                Vector2.left,
                new Vector2(-1f, -1f).normalized,
                Vector2.down,
                new Vector2(1f, -1f).normalized
            };

            for (int index = 0; index < expected.Length; index++)
            {
                float radians = index * 45f * Mathf.Deg2Rad;
                Vector2 raw = new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians));
                Assert.That(
                    Vector2.Distance(GrappleAim8.Quantize(raw), expected[index]),
                    Is.LessThan(0.0001f),
                    $"octant={index}, raw={raw}");
            }

            Assert.That(GrappleAim8.Quantize(Vector2.zero), Is.EqualTo(Vector2.right));
            Assert.That(
                GrappleAim8.Quantize(new Vector2(100f, 98f)),
                Is.EqualTo(new Vector2(1f, 1f).normalized));
        }

        [Test]
        public void GrappleSelector_IsNearestThenKindThenStableOrderAndCapsAtEightCells()
        {
            GrappleTargetCandidate nearTerrain =
                Candidate(GrappleTargetKind.FixedTerrain, 2f, 50, 1f);
            GrappleTargetCandidate fartherBoss =
                Candidate(GrappleTargetKind.BossHook, 2.25f, 1, 2f);

            Assert.That(
                GrappleTargetSelector.TrySelect(
                    new[] { fartherBoss, nearTerrain },
                    8f,
                    out GrappleTargetCandidate nearest),
                Is.True);
            Assert.That(nearest.Point, Is.EqualTo(nearTerrain.Point));

            GrappleTargetCandidate equalTerrain =
                Candidate(GrappleTargetKind.FixedTerrain, 4f, 1, 3f);
            GrappleTargetCandidate equalPullable =
                Candidate(GrappleTargetKind.Pullable, 4f, 1, 4f);
            GrappleTargetCandidate equalBoss =
                Candidate(GrappleTargetKind.BossHook, 4f, 99, 5f);
            Assert.That(
                GrappleTargetSelector.TrySelect(
                    new[] { equalTerrain, equalPullable, equalBoss },
                    8f,
                    out GrappleTargetCandidate priority),
                Is.True);
            Assert.That(priority.Kind, Is.EqualTo(GrappleTargetKind.BossHook));

            GrappleTargetCandidate laterStable =
                Candidate(GrappleTargetKind.Pullable, 5f, 20, 6f);
            GrappleTargetCandidate earlierStable =
                Candidate(GrappleTargetKind.Pullable, 5.0005f, 3, 7f);
            Assert.That(
                GrappleTargetSelector.TrySelect(
                    new[] { laterStable, earlierStable },
                    8f,
                    out GrappleTargetCandidate stable),
                Is.True);
            Assert.That(stable.StableOrder, Is.EqualTo(3));

            GrappleTargetCandidate exactlyEight =
                Candidate(GrappleTargetKind.FixedTerrain, 8f, 1, 8f);
            GrappleTargetCandidate beyondEight =
                Candidate(GrappleTargetKind.BossHook, 8.001f, 0, 9f);
            Assert.That(
                GrappleTargetSelector.TrySelect(
                    new[] { beyondEight, exactlyEight },
                    8f,
                    out GrappleTargetCandidate ranged),
                Is.True);
            Assert.That(ranged.DistanceCells, Is.EqualTo(8f));
            Assert.That(
                GrappleTargetSelector.TrySelect(
                    new[] { beyondEight },
                    8f,
                    out _),
                Is.False);
        }

        [Test]
        public void RopeInstaller_ClampsToSixCellsAndCannotReachSeventhCellAnchor()
        {
            using (ToolTileWorld fixture =
                new ToolTileWorld(new Vector2Int(8, 10)))
            {
                TileDefinition floor = fixture.CreateDefinition(
                    "ReinforcedFloor",
                    TileMaterialKind.ReinforcedWall,
                    TileBreakMethod.None);
                fixture.FillRow(0, floor);
                fixture.ConfigureService(
                    new GridPos(0, 1),
                    new GridPos(7, 1));

                GridPos useCell = new GridPos(2, 1);
                GameObject anchorObject = new GameObject("P3_RopeAnchor_Test");
                anchorObject.transform.SetParent(fixture.Root.transform, false);
                RopeAnchor2D anchor = anchorObject.AddComponent<RopeAnchor2D>();
                anchor.Configure(
                    fixture.World,
                    new GridPos(2, 7),
                    RopeAnchorKind.Ring);

                GameObject installerObject =
                    new GameObject("P3_RopeInstaller_Test");
                installerObject.transform.SetParent(
                    fixture.Root.transform,
                    false);
                RopeInstaller2D installer =
                    installerObject.AddComponent<RopeInstaller2D>();
                installer.Configure(
                    fixture.World,
                    fixture.Service,
                    configuredMaximumLength: 99);

                Assert.That(RopeInstaller2D.DefaultStartingStock, Is.EqualTo(4));
                Assert.That(
                    RopePlacementSolver.DefaultMaximumLength,
                    Is.EqualTo(6));
                Assert.That(installer.MaximumLength, Is.EqualTo(6));
                Assert.That(
                    installer.TryBuildPlanForTests(
                        useCell,
                        out RopeInstallPlan plan,
                        out RopeInstallFailure failure),
                    Is.True);
                Assert.That(failure, Is.EqualTo(RopeInstallFailure.None));
                Assert.That(plan.Length, Is.EqualTo(6));
                Assert.That(
                    plan.ClimbableCells,
                    Is.EqualTo(Enumerable.Range(2, 6)
                        .Select(y => new GridPos(2, y))
                        .ToArray()));

                anchor.Configure(
                    fixture.World,
                    new GridPos(2, 8),
                    RopeAnchorKind.Ring);
                Assert.That(
                    installer.TryBuildPlanForTests(
                        useCell,
                        out _,
                        out failure),
                    Is.False);
                Assert.That(
                    failure,
                    Is.EqualTo(RopeInstallFailure.NoAnchorWithinRange));
            }
        }

        [Test]
        public void RopePlacement_RejectsProtectedSpanAndRequiredExitCell()
        {
            using (ToolTileWorld fixture =
                new ToolTileWorld(new Vector2Int(8, 8)))
            {
                TileDefinition floor = fixture.CreateDefinition(
                    "ReinforcedFloor",
                    TileMaterialKind.ReinforcedWall,
                    TileBreakMethod.None);
                fixture.FillRow(0, floor);
                GridPos start = new GridPos(0, 1);
                GridPos exit = new GridPos(7, 1);
                GridPos protectedSpan = new GridPos(2, 2);
                fixture.ConfigureService(
                    start,
                    exit,
                    new[] { protectedSpan });

                RopeInstaller2D installer =
                    fixture.Root.AddComponent<RopeInstaller2D>();
                installer.Configure(fixture.World, fixture.Service);

                GameObject spanAnchorObject =
                    new GameObject("P3_ProtectedSpan_Anchor");
                spanAnchorObject.transform.SetParent(
                    fixture.Root.transform,
                    false);
                RopeAnchor2D spanAnchor =
                    spanAnchorObject.AddComponent<RopeAnchor2D>();
                spanAnchor.Configure(
                    fixture.World,
                    new GridPos(2, 4),
                    RopeAnchorKind.Ring);

                Assert.That(fixture.Service.IsCurrentExitReachable(), Is.True);
                Assert.That(
                    installer.TryBuildPlanForTests(
                        new GridPos(2, 1),
                        out _,
                        out RopeInstallFailure protectedFailure),
                    Is.False);
                Assert.That(
                    protectedFailure,
                    Is.EqualTo(RopeInstallFailure.ProtectedRouteCell));

                GameObject exitAnchorObject =
                    new GameObject("P3_ExitCell_Anchor");
                exitAnchorObject.transform.SetParent(
                    fixture.Root.transform,
                    false);
                RopeAnchor2D exitAnchor =
                    exitAnchorObject.AddComponent<RopeAnchor2D>();
                exitAnchor.Configure(
                    fixture.World,
                    new GridPos(exit.X, exit.Y + 3),
                    RopeAnchorKind.Ring);

                Assert.That(
                    installer.TryBuildPlanForTests(
                        exit,
                        out _,
                        out RopeInstallFailure exitFailure),
                    Is.False);
                Assert.That(
                    exitFailure,
                    Is.EqualTo(RopeInstallFailure.ProtectedRouteCell));
                Assert.That(fixture.Service.IsCurrentExitReachable(), Is.True);
            }
        }

        [Test]
        public void Pickaxe_DestroysExactlyOneAdjacentStoneCell()
        {
            using (ToolTileWorld fixture =
                new ToolTileWorld(new Vector2Int(8, 5)))
            {
                TileDefinition floor = fixture.CreateDefinition(
                    "ReinforcedFloor",
                    TileMaterialKind.ReinforcedWall,
                    TileBreakMethod.None);
                TileDefinition stone = fixture.CreateDefinition(
                    "Stone",
                    TileMaterialKind.Stone,
                    TileBreakMethod.Pickaxe);
                fixture.FillRow(0, floor);
                GridPos origin = new GridPos(3, 2);
                GridPos adjacent = new GridPos(4, 2);
                GridPos nonAdjacent = new GridPos(5, 2);
                fixture.SetTile(adjacent, stone);
                fixture.SetTile(nonAdjacent, stone);
                fixture.ConfigureService(
                    new GridPos(0, 1),
                    new GridPos(7, 1));

                PickaxeTool2D pickaxe =
                    fixture.Root.AddComponent<PickaxeTool2D>();
                pickaxe.Configure(
                    fixture.World,
                    fixture.Service,
                    PickaxeTool2D.DefaultDurability,
                    0f);

                MiningUseResult result =
                    pickaxe.TryUseImmediatelyForTests(
                        origin,
                        Vector2Int.right,
                        true);

                Assert.That(PickaxeTool2D.DefaultDurability, Is.EqualTo(10));
                Assert.That(result.Queued, Is.True);
                Assert.That(result.TargetCell, Is.EqualTo(adjacent));
                Assert.That(result.RemainingDurability, Is.EqualTo(9));
                Assert.That(
                    fixture.Terrain.GetTile(ToVector3Int(adjacent)),
                    Is.Null);
                Assert.That(
                    fixture.Terrain.GetTile(ToVector3Int(nonAdjacent)),
                    Is.SameAs(stone.Tile));

                MiningUseResult diagonal =
                    pickaxe.TryUseImmediatelyForTests(
                        origin,
                        new Vector2Int(1, 1),
                        true);
                Assert.That(
                    diagonal.Failure,
                    Is.EqualTo(MiningUseFailure.InvalidDirection));
                Assert.That(pickaxe.RemainingDurability, Is.EqualTo(9));
                Assert.That(
                    fixture.Terrain.GetTile(ToVector3Int(nonAdjacent)),
                    Is.SameAs(stone.Tile));
            }
        }

        [Test]
        public void Shovel_OnlyRemovesDirtSandAshOrConfiguredSoftTerrain()
        {
            using (ToolTileWorld fixture =
                new ToolTileWorld(new Vector2Int(8, 6)))
            {
                TileDefinition floor = fixture.CreateDefinition(
                    "ReinforcedFloor",
                    TileMaterialKind.ReinforcedWall,
                    TileBreakMethod.None);
                TileDefinition dirt = fixture.CreateDefinition(
                    "Dirt",
                    TileMaterialKind.Dirt,
                    TileBreakMethod.Shovel | TileBreakMethod.Bomb);
                TileDefinition stone = fixture.CreateDefinition(
                    "Stone",
                    TileMaterialKind.Stone,
                    TileBreakMethod.Pickaxe);
                TileDefinition ash = fixture.CreateDefinition(
                    "Ash",
                    TileMaterialKind.Ash,
                    TileBreakMethod.Shovel);
                TileDefinition sand = fixture.CreateDefinition(
                    "Sand",
                    TileMaterialKind.Sand,
                    TileBreakMethod.Shovel);
                fixture.FillRow(0, floor);

                GridPos origin = new GridPos(3, 2);
                GridPos dirtCell = new GridPos(4, 2);
                GridPos stoneCell = new GridPos(2, 2);
                GridPos ashCell = new GridPos(3, 3);
                GridPos sandCell = new GridPos(3, 1);
                fixture.SetTile(dirtCell, dirt);
                fixture.SetTile(stoneCell, stone);
                fixture.SetTile(ashCell, ash);
                fixture.SetTile(sandCell, sand);
                fixture.ConfigureService(
                    new GridPos(0, 1),
                    new GridPos(7, 1));

                ShovelTool2D shovel =
                    fixture.Root.AddComponent<ShovelTool2D>();
                shovel.Configure(
                    fixture.World,
                    fixture.Service,
                    ShovelTool2D.DefaultDurability,
                    0f);

                MiningUseResult dirtResult =
                    shovel.TryUseImmediatelyForTests(
                        origin,
                        Vector2Int.right,
                        true);
                Assert.That(ShovelTool2D.DefaultDurability, Is.EqualTo(12));
                Assert.That(dirtResult.Queued, Is.True);
                Assert.That(shovel.RemainingDurability, Is.EqualTo(11));
                Assert.That(
                    fixture.Terrain.GetTile(ToVector3Int(dirtCell)),
                    Is.Null);

                MiningUseResult stoneResult =
                    shovel.TryUseImmediatelyForTests(
                        origin,
                        Vector2Int.left,
                        true);
                Assert.That(
                    stoneResult.Failure,
                    Is.EqualTo(MiningUseFailure.WrongTerrain));
                Assert.That(shovel.RemainingDurability, Is.EqualTo(11));
                Assert.That(
                    fixture.Terrain.GetTile(ToVector3Int(stoneCell)),
                    Is.SameAs(stone.Tile));

                MiningUseResult ashResult =
                    shovel.TryUseImmediatelyForTests(
                        origin,
                        Vector2Int.up,
                        true);
                Assert.That(ashResult.Queued, Is.True);
                Assert.That(shovel.RemainingDurability, Is.EqualTo(10));
                Assert.That(
                    fixture.Terrain.GetTile(ToVector3Int(ashCell)),
                    Is.Null);

                MiningUseResult sandResult =
                    shovel.TryUseImmediatelyForTests(
                        origin,
                        Vector2Int.down,
                        true);
                Assert.That(sandResult.Queued, Is.True);
                Assert.That(shovel.RemainingDurability, Is.EqualTo(9));
                Assert.That(
                    fixture.Terrain.GetTile(ToVector3Int(sandCell)),
                    Is.Null);
            }
        }

        [Test]
        public void WaterStream_UsesThreeForwardCellsAndNeverExceedsSixCells()
        {
            GridPos origin = new GridPos(2, 5);
            RectInt bounds = new RectInt(0, 0, 10, 10);
            GridPos[] expected =
            {
                new GridPos(3, 5),
                new GridPos(4, 5),
                new GridPos(5, 5),
                new GridPos(5, 4),
                new GridPos(6, 5),
                new GridPos(5, 3)
            };

            IReadOnlyList<GridPos> cells = WaterStreamResolver.Resolve(
                origin,
                new GridPos(8, 2),
                bounds,
                _ => false);
            IReadOnlyList<GridPos> clamped = WaterStreamResolver.Resolve(
                origin,
                new GridPos(1, 0),
                bounds,
                _ => false,
                WaterStreamResolver.ForwardRange,
                999);

            Assert.That(WaterStreamResolver.ForwardRange, Is.EqualTo(3));
            Assert.That(WaterStreamResolver.AbsoluteMaxCells, Is.EqualTo(6));
            Assert.That(cells, Is.EqualTo(expected));
            Assert.That(clamped.Count, Is.EqualTo(6));
            Assert.That(clamped.Distinct().Count(), Is.EqualTo(6));
            Assert.That(
                WaterStreamResolver.Resolve(
                    origin,
                    new GridPos(0, 0),
                    bounds,
                    _ => false),
                Is.Empty);
        }

        [Test]
        public void Pestle_StrikesOnlyCellBelowAndEnforcesDeliberateRecovery()
        {
            GameObject root = new GameObject("P3_Pestle_Test");
            P1lessGridWorld world = new P1lessGridWorld(
                root,
                new Vector2Int(8, 8));
            PestleInteractionRegistry2D registry =
                root.AddComponent<PestleInteractionRegistry2D>();

            GameObject stakeObject = new GameObject("P3_Pestle_Stake_Test");
            stakeObject.transform.SetParent(root.transform, false);
            DrivenStake2D stake = stakeObject.AddComponent<DrivenStake2D>();
            GridPos actorCell = new GridPos(4, 4);
            GridPos expectedStrikeCell = new GridPos(4, 3);
            stake.Configure(
                registry,
                world.World,
                expectedStrikeCell);

            PestleTool2D pestle = root.AddComponent<PestleTool2D>();
            pestle.Configure(
                world.World,
                registry,
                PestleTool2D.DefaultRecoveryDuration);

            try
            {
                Assert.That(
                    pestle.TryStrikeAt(
                        actorCell,
                        10f,
                        out PestleStrikeReport report),
                    Is.True);
                Assert.That(pestle.UsesDurability, Is.False);
                Assert.That(
                    pestle.RecoveryDuration,
                    Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(report.StrikeCell, Is.EqualTo(expectedStrikeCell));
                Assert.That(report.ReactionCount, Is.EqualTo(1));
                Assert.That(
                    report.CombinedReaction,
                    Is.EqualTo(PestleReactionKind.StakeDriven));
                Assert.That(stake.IsDriven, Is.True);
                Assert.That(
                    pestle.GetRemainingRecoveryAt(10.3f),
                    Is.EqualTo(0.5f).Within(0.0002f));

                Assert.That(
                    pestle.TryStrikeAt(actorCell, 10.79f, out _),
                    Is.False);
                Assert.That(
                    pestle.TryStrikeAt(
                        actorCell,
                        10.81f,
                        out PestleStrikeReport secondReport),
                    Is.True);
                Assert.That(secondReport.StrikeCell, Is.EqualTo(expectedStrikeCell));
                Assert.That(
                    secondReport.ReactionCount,
                    Is.EqualTo(0),
                    "The already-driven stake must not react twice.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GrappleTargetCandidate Candidate(
            GrappleTargetKind kind,
            float distance,
            int stableOrder,
            float pointId)
        {
            return new GrappleTargetCandidate(
                kind,
                distance,
                stableOrder,
                new Vector2(pointId, 0f),
                null);
        }

        private static Vector3Int ToVector3Int(GridPos cell)
        {
            return new Vector3Int(cell.X, cell.Y, 0);
        }

        private sealed class P1lessGridWorld
        {
            public P1lessGridWorld(GameObject root, Vector2Int size)
            {
                UnityGrid grid = root.AddComponent<UnityGrid>();
                World = root.AddComponent<GridWorld>();
                World.Configure(
                    grid,
                    null,
                    null,
                    Vector2Int.zero,
                    size);
            }

            public GridWorld World { get; }
        }

        private sealed class ToolTileWorld : System.IDisposable
        {
            private readonly List<Object> transientAssets = new List<Object>();
            private readonly List<TileDefinition> definitions =
                new List<TileDefinition>();

            public ToolTileWorld(Vector2Int size)
            {
                Root = new GameObject("P3_ToolTileWorld_Test");
                UnityGrid layout = Root.AddComponent<UnityGrid>();
                GameObject terrainObject = new GameObject("Terrain");
                terrainObject.transform.SetParent(Root.transform, false);
                Terrain = terrainObject.AddComponent<Tilemap>();
                terrainObject.AddComponent<TilemapRenderer>();

                World = Root.AddComponent<GridWorld>();
                World.Configure(
                    layout,
                    Terrain,
                    null,
                    Vector2Int.zero,
                    size);
                Service = Root.AddComponent<TileMutationService>();
            }

            public GameObject Root { get; }
            public GridWorld World { get; }
            public Tilemap Terrain { get; }
            public TileMutationService Service { get; }

            public TileDefinition CreateDefinition(
                string id,
                TileMaterialKind materialKind,
                TileBreakMethod breakableBy)
            {
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = id + "_Tile";
                TileDefinition definition =
                    ScriptableObject.CreateInstance<TileDefinition>();
                definition.name = id + "_Definition";
                definition.Configure(
                    id,
                    tile,
                    materialKind,
                    true,
                    breakableBy);
                transientAssets.Add(definition);
                transientAssets.Add(tile);
                definitions.Add(definition);
                return definition;
            }

            public void FillRow(int y, TileDefinition definition)
            {
                for (int x = World.CellBounds.xMin;
                    x < World.CellBounds.xMax;
                    x++)
                {
                    SetTile(new GridPos(x, y), definition);
                }
            }

            public void SetTile(GridPos cell, TileDefinition definition)
            {
                Terrain.SetTile(
                    ToVector3Int(cell),
                    definition != null ? definition.Tile : null);
            }

            public void ConfigureService(
                GridPos start,
                GridPos exit,
                GridPos[] protectedCells = null)
            {
                Service.Configure(
                    World,
                    Terrain,
                    null,
                    null,
                    null,
                    null,
                    definitions.ToArray(),
                    start,
                    exit,
                    protectedCells ?? new GridPos[0]);
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                for (int index = 0; index < transientAssets.Count; index++)
                {
                    if (transientAssets[index] != null)
                    {
                        Object.DestroyImmediate(transientAssets[index]);
                    }
                }
            }
        }
    }
}

#endif
