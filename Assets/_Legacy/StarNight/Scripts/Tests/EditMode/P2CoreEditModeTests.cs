#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Tiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityGrid = UnityEngine.Grid;

namespace StarNight.Tests.EditMode
{
    public sealed class P2CoreEditModeTests
    {
        [Test]
        public void ExplosionMask_IsExactCenteredThreeByThreeInStableRowOrder()
        {
            GridPos center = new GridPos(17, -4);
            GridPos[] expected =
            {
                new GridPos(16, -5),
                new GridPos(17, -5),
                new GridPos(18, -5),
                new GridPos(16, -4),
                new GridPos(17, -4),
                new GridPos(18, -4),
                new GridPos(16, -3),
                new GridPos(17, -3),
                new GridPos(18, -3)
            };

            GridPos[] actual = ExplosionMask3x3.Enumerate(center).ToArray();

            Assert.That(ExplosionMask3x3.CellCount, Is.EqualTo(9));
            Assert.That(ExplosionMask3x3.Offsets.Count, Is.EqualTo(9));
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Distinct().Count(), Is.EqualTo(9));
            Assert.That(ExplosionMask3x3.Contains(center, center), Is.True);
            Assert.That(
                ExplosionMask3x3.Contains(center, new GridPos(center.X + 1, center.Y - 1)),
                Is.True);
            Assert.That(
                ExplosionMask3x3.Contains(center, new GridPos(center.X + 2, center.Y)),
                Is.False);
            Assert.That(ExplosionConstants.BombFuseSeconds, Is.EqualTo(1.8f));
            Assert.That(ExplosionConstants.DefaultStartingBombCount, Is.EqualTo(4));
        }

        [Test]
        public void TileDefinitions_UseStoneDirtAndCrackedWallBreakMatrix()
        {
            Tile stoneTile = ScriptableObject.CreateInstance<Tile>();
            Tile dirtTile = ScriptableObject.CreateInstance<Tile>();
            Tile crackedTile = ScriptableObject.CreateInstance<Tile>();
            Tile reinforcedTile = ScriptableObject.CreateInstance<Tile>();
            TileDefinition stone = ScriptableObject.CreateInstance<TileDefinition>();
            TileDefinition dirt = ScriptableObject.CreateInstance<TileDefinition>();
            TileDefinition cracked = ScriptableObject.CreateInstance<TileDefinition>();
            TileDefinition reinforced = ScriptableObject.CreateInstance<TileDefinition>();

            try
            {
                stone.Configure(
                    "Stone",
                    stoneTile,
                    TileMaterialKind.Stone,
                    true,
                    TileBreakMethod.Pickaxe);
                dirt.Configure(
                    "Dirt",
                    dirtTile,
                    TileMaterialKind.Dirt,
                    true,
                    TileBreakMethod.Bomb | TileBreakMethod.Shovel);
                cracked.Configure(
                    "CrackedWall",
                    crackedTile,
                    TileMaterialKind.CrackedWall,
                    true,
                    TileBreakMethod.Bomb | TileBreakMethod.Pickaxe);
                reinforced.Configure(
                    "ReinforcedWall",
                    reinforcedTile,
                    TileMaterialKind.ReinforcedWall,
                    true,
                    TileBreakMethod.Bomb
                    | TileBreakMethod.Pickaxe
                    | TileBreakMethod.Shovel);

                Assert.That(stone.CanBreak(TileBreakMethod.Pickaxe), Is.True);
                Assert.That(stone.CanBreak(TileBreakMethod.Bomb), Is.False);
                Assert.That(stone.CanBreak(TileBreakMethod.Shovel), Is.False);

                Assert.That(dirt.CanBreak(TileBreakMethod.Bomb), Is.True);
                Assert.That(dirt.CanBreak(TileBreakMethod.Shovel), Is.True);
                Assert.That(dirt.CanBreak(TileBreakMethod.Pickaxe), Is.False);

                Assert.That(cracked.CanBreak(TileBreakMethod.Bomb), Is.True);
                Assert.That(cracked.CanBreak(TileBreakMethod.Pickaxe), Is.True);
                Assert.That(cracked.CanBreak(TileBreakMethod.Shovel), Is.False);

                Assert.That(reinforced.IsProtected, Is.True);
                Assert.That(reinforced.CanBreak(TileBreakMethod.Bomb), Is.False);
                Assert.That(reinforced.CanBreak(TileBreakMethod.Pickaxe), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(stone);
                Object.DestroyImmediate(dirt);
                Object.DestroyImmediate(cracked);
                Object.DestroyImmediate(reinforced);
                Object.DestroyImmediate(stoneTile);
                Object.DestroyImmediate(dirtTile);
                Object.DestroyImmediate(crackedTile);
                Object.DestroyImmediate(reinforcedTile);
            }
        }

        [Test]
        public void P2TileDefinitionAssets_WhenPresentRespectTheSameBreakMatrix()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:TileDefinition",
                new[] { "Assets/StarNight" });
            if (guids.Length == 0)
            {
                Assert.Pass("P2 can use runtime-created definitions; the runtime matrix is covered separately.");
            }

            TileDefinition[] definitions = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<TileDefinition>)
                .Where(definition => definition != null)
                .ToArray();

            TileDefinition stone = definitions.FirstOrDefault(
                definition => definition.MaterialKind == TileMaterialKind.Stone);
            TileDefinition dirt = definitions.FirstOrDefault(
                definition => definition.MaterialKind == TileMaterialKind.Dirt);
            TileDefinition cracked = definitions.FirstOrDefault(
                definition => definition.MaterialKind == TileMaterialKind.CrackedWall);

            Assert.That(stone, Is.Not.Null, "P2 assets must include a stone definition.");
            Assert.That(dirt, Is.Not.Null, "P2 assets must include a dirt definition.");
            Assert.That(cracked, Is.Not.Null, "P2 assets must include a cracked-wall definition.");

            Assert.That(stone.CanBreak(TileBreakMethod.Pickaxe), Is.True);
            Assert.That(stone.CanBreak(TileBreakMethod.Bomb), Is.False);
            Assert.That(dirt.CanBreak(TileBreakMethod.Bomb), Is.True);
            Assert.That(dirt.CanBreak(TileBreakMethod.Shovel), Is.True);
            Assert.That(dirt.CanBreak(TileBreakMethod.Pickaxe), Is.False);
            Assert.That(cracked.CanBreak(TileBreakMethod.Bomb), Is.True);
            Assert.That(cracked.CanBreak(TileBreakMethod.Pickaxe), Is.True);
            Assert.That(cracked.CanBreak(TileBreakMethod.Shovel), Is.False);
        }

        [Test]
        public void TileMutationBatch_ResolvesSameCellAtomicallyAndNotifiesAfterCommit()
        {
            using (P2TileWorldFixture fixture =
                new P2TileWorldFixture(new Vector2Int(8, 5)))
            {
                TileDefinition stone = fixture.CreateDefinition(
                    "Stone",
                    TileMaterialKind.Stone,
                    TileBreakMethod.Pickaxe);
                TileDefinition dirt = fixture.CreateDefinition(
                    "Dirt",
                    TileMaterialKind.Dirt,
                    TileBreakMethod.Bomb | TileBreakMethod.Shovel);
                TileDefinition cracked = fixture.CreateDefinition(
                    "Cracked",
                    TileMaterialKind.CrackedWall,
                    TileBreakMethod.Bomb | TileBreakMethod.Pickaxe);

                fixture.FillRow(0, stone);
                GridPos target = new GridPos(3, 3);
                fixture.SetTile(target, cracked);
                fixture.ConfigureService(
                    new GridPos(1, 1),
                    new GridPos(6, 1));

                int eventCount = 0;
                TileBase tileObservedByEvent = null;
                fixture.Service.BatchCommitted += report =>
                {
                    eventCount++;
                    tileObservedByEvent = fixture.Terrain.GetTile(
                        new Vector3Int(target.X, target.Y, 0));
                    Assert.That(report.PathVersion, Is.EqualTo(1));
                };

                long destroySequence = fixture.Service.EnqueueDestroy(
                    target,
                    TileBreakMethod.Bomb);
                long setSequence = fixture.Service.EnqueueSet(
                    target,
                    dirt,
                    TileBreakMethod.System);

                Assert.That(
                    fixture.Terrain.GetTile(new Vector3Int(target.X, target.Y, 0)),
                    Is.SameAs(cracked.Tile),
                    "Requests must remain queued until the fixed-tick batch is flushed.");

                TileMutationBatchReport report = fixture.Service.FlushPending();

                Assert.That(setSequence, Is.GreaterThan(destroySequence));
                Assert.That(report.Records.Count, Is.EqualTo(2));
                Assert.That(report.CommittedCount, Is.EqualTo(1));
                Assert.That(report.RejectedCount, Is.EqualTo(1));
                Assert.That(
                    report.Records[0].Rejection,
                    Is.EqualTo(TileMutationRejection.Superseded));
                Assert.That(report.Records[1].Committed, Is.True);
                Assert.That(
                    fixture.Terrain.GetTile(new Vector3Int(target.X, target.Y, 0)),
                    Is.SameAs(dirt.Tile));
                Assert.That(tileObservedByEvent, Is.SameAs(dirt.Tile));
                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(fixture.Service.PendingCount, Is.EqualTo(0));
                Assert.That(fixture.Service.MutationTick, Is.EqualTo(1));
                Assert.That(fixture.Service.PathVersion, Is.EqualTo(1));
                Assert.That(fixture.Service.LastReport, Is.SameAs(report));
                Assert.That(fixture.Service.IsCurrentExitReachable(), Is.True);
            }
        }

        [Test]
        public void TileMutation_RejectsProtectedExitAndOnlyEscapeRouteBlocker()
        {
            using (P2TileWorldFixture fixture =
                new P2TileWorldFixture(new Vector2Int(7, 3)))
            {
                TileDefinition stone = fixture.CreateDefinition(
                    "Stone",
                    TileMaterialKind.Stone,
                    TileBreakMethod.Pickaxe);
                TileDefinition reinforced = fixture.CreateDefinition(
                    "Reinforced",
                    TileMaterialKind.ReinforcedWall,
                    TileBreakMethod.None);

                fixture.FillRow(0, reinforced);
                fixture.FillRow(2, reinforced);
                GridPos start = new GridPos(1, 1);
                GridPos exit = new GridPos(5, 1);
                GridPos chokePoint = new GridPos(3, 1);
                fixture.ConfigureService(start, exit, new[] { exit });

                Assert.That(fixture.Service.IsCurrentExitReachable(), Is.True);
                Assert.That(fixture.Service.IsProtectedCell(exit), Is.True);

                fixture.Service.EnqueueSet(
                    chokePoint,
                    stone,
                    TileBreakMethod.System);
                fixture.Service.EnqueueDestroy(
                    exit,
                    TileBreakMethod.Bomb);
                TileMutationBatchReport report = fixture.Service.FlushPending();

                Assert.That(report.Records.Count, Is.EqualTo(2));
                Assert.That(report.CommittedCount, Is.EqualTo(0));
                Assert.That(report.RejectedCount, Is.EqualTo(2));
                Assert.That(
                    report.Records.Single(
                        record => record.Request.Cell == chokePoint).Rejection,
                    Is.EqualTo(TileMutationRejection.ExitBlocked));
                Assert.That(
                    report.Records.Single(
                        record => record.Request.Cell == exit).Rejection,
                    Is.EqualTo(TileMutationRejection.ProtectedExit));
                Assert.That(
                    fixture.Terrain.GetTile(
                        new Vector3Int(chokePoint.X, chokePoint.Y, 0)),
                    Is.Null);
                Assert.That(fixture.Service.PathVersion, Is.EqualTo(0));
                Assert.That(fixture.Service.IsCurrentExitReachable(), Is.True);
            }
        }

        [Test]
        public void GridWorld_MultiCellOccupancyMovesAtomicallyAndPreservesOwners()
        {
            GameObject root = new GameObject("P2_MultiCellOccupancy_Test");
            GameObject firstOwner = new GameObject("FirstOwner");
            GameObject blockerOwner = new GameObject("BlockerOwner");
            try
            {
                UnityGrid layout = root.AddComponent<UnityGrid>();
                GridWorld world = root.AddComponent<GridWorld>();
                world.Configure(
                    layout,
                    null,
                    null,
                    Vector2Int.zero,
                    new Vector2Int(8, 8));

                GridPos[] originalFootprint =
                {
                    new GridPos(1, 1),
                    new GridPos(2, 1),
                    new GridPos(1, 2),
                    new GridPos(2, 2)
                };
                GridPos blockerCell = new GridPos(4, 2);
                GridPos[] rejectedFootprint =
                {
                    new GridPos(3, 1),
                    new GridPos(4, 1),
                    new GridPos(3, 2),
                    blockerCell
                };
                GridPos[] movedFootprint =
                {
                    new GridPos(4, 4),
                    new GridPos(5, 4),
                    new GridPos(4, 5),
                    new GridPos(5, 5)
                };

                Assert.That(world.TryOccupy(originalFootprint, firstOwner), Is.True);
                Assert.That(world.TryOccupy(blockerCell, blockerOwner), Is.True);
                Assert.That(world.TryOccupy(rejectedFootprint, firstOwner), Is.False);

                foreach (GridPos cell in originalFootprint)
                {
                    Assert.That(world.TryGetOccupant(cell, out Object owner), Is.True);
                    Assert.That(owner, Is.SameAs(firstOwner));
                }

                Assert.That(
                    world.TryGetOccupant(blockerCell, out Object blocker),
                    Is.True);
                Assert.That(blocker, Is.SameAs(blockerOwner));

                Assert.That(world.TryOccupy(movedFootprint, firstOwner), Is.True);
                foreach (GridPos cell in originalFootprint)
                {
                    Assert.That(world.IsOccupied(cell), Is.False);
                }

                Assert.That(world.AnyOccupied(movedFootprint), Is.True);
                Assert.That(
                    world.AnyOccupied(movedFootprint, firstOwner),
                    Is.False);
                Assert.That(world.ReleaseAll(firstOwner), Is.EqualTo(4));
                Assert.That(world.AnyOccupied(movedFootprint), Is.False);
                Assert.That(world.IsOccupied(blockerCell), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(firstOwner);
                Object.DestroyImmediate(blockerOwner);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        [Timeout(5000)]
        public void ExplosionChain_OneHundredBombsRepeatedOneHundredTimesIsBoundedAndDeterministic()
        {
            const int bombCount = 100;
            const int repetitions = 100;
            List<ExplosionChainNode> reversedNodes =
                new List<ExplosionChainNode>(bombCount);
            for (int id = bombCount; id >= 1; id--)
            {
                reversedNodes.Add(new ExplosionChainNode(
                    id,
                    new GridPos(id - 1, 0)));
            }

            int[] expectedOrder = Enumerable.Range(1, bombCount).ToArray();
            int[] seeds = { 1, 1, 9999 };

            for (int iteration = 0; iteration < repetitions; iteration++)
            {
                ExplosionChainResolution result = ExplosionChainResolver.Resolve(
                    reversedNodes,
                    seeds,
                    bombCount);

                Assert.That(result.SeedCount, Is.EqualTo(1), $"iteration={iteration}");
                Assert.That(
                    result.ProcessedCount,
                    Is.EqualTo(bombCount),
                    $"iteration={iteration}");
                Assert.That(
                    result.ProcessingOrder,
                    Is.EqualTo(expectedOrder),
                    $"iteration={iteration}");
                Assert.That(result.HardCapReached, Is.False, $"iteration={iteration}");
                Assert.That(result.QueuedWhenCapped, Is.EqualTo(0), $"iteration={iteration}");
            }
        }

        private sealed class P2TileWorldFixture : System.IDisposable
        {
            private readonly List<Object> transientAssets = new List<Object>();
            private readonly List<TileDefinition> definitions =
                new List<TileDefinition>();

            public P2TileWorldFixture(Vector2Int size)
            {
                Root = new GameObject("P2_TileWorld_Test");
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
                TileMaterialKind kind,
                TileBreakMethod breakableBy,
                bool solid = true,
                bool sacred = false,
                bool requiredEventCore = false)
            {
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = id + "_Tile";
                TileDefinition definition =
                    ScriptableObject.CreateInstance<TileDefinition>();
                definition.name = id + "_Definition";
                definition.Configure(
                    id,
                    tile,
                    kind,
                    solid,
                    breakableBy,
                    sacred,
                    requiredEventCore);
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
                    new Vector3Int(cell.X, cell.Y, 0),
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
