using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Biomes;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.Baking
{
    /// <summary>
    /// RMAP17's immutable projection of the exact RMAP16 bake snapshot. This
    /// class owns no Unity objects, scene lifecycle, mutable-world state, or
    /// second terrain-generation path.
    /// </summary>
    public sealed class Rmap17WorldBakePlan
    {
        internal Rmap17WorldBakePlan(
            Rmap16ClusterAssemblyPlan sourceAssembly,
            Rmap16BakeSnapshot snapshot,
            RmapSpecialWorldPoint playerStart)
        {
            SourceAssembly = sourceAssembly ?? throw new ArgumentNullException(nameof(sourceAssembly));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            PlayerStart = playerStart;
            sourceCells = snapshot.Cells ?? throw new ArgumentException("RMAP16 snapshot has no cells.", nameof(snapshot));
            solidCells = Freeze(sourceCells.Where(value => value.BaseCell == RmapPatternBaseCell.Solid));
            oneWayCells = Freeze(sourceCells.Where(value => value.BaseCell == RmapPatternBaseCell.OneWayPlatform));
            airCells = Freeze(sourceCells.Where(value => value.BaseCell == RmapPatternBaseCell.Air));
            ladderOverlays = FreezeOverlays(snapshot.Overlays.Where(value => value.Kind == "LADDER"));
            grabOverlays = FreezeOverlays(snapshot.Overlays.Where(value => value.Kind == "GRAB_EDGE"));
            markerOverlays = FreezeOverlays(snapshot.Overlays.Where(value => value.Kind != "LADDER" && value.Kind != "GRAB_EDGE"));
            Digest = RmapWorldDefinition.Hash(string.Join("\n", new[]
            {
                "RMAP17_STATIC_WORLD_BAKE_V1",
                SourcePlanDigest,
                SourceCellDigest,
                sourceCells.Count.ToString(CultureInfo.InvariantCulture),
                SolidCellCount.ToString(CultureInfo.InvariantCulture),
                AirCellCount.ToString(CultureInfo.InvariantCulture),
                OneWayCellCount.ToString(CultureInfo.InvariantCulture),
                LadderOverlayCount.ToString(CultureInfo.InvariantCulture),
                GrabOverlayCount.ToString(CultureInfo.InvariantCulture),
                MarkerOverlayCount.ToString(CultureInfo.InvariantCulture),
                PlayerStart.ToString(),
                string.Join(";", snapshot.Overlays.Select(value => value.Id + "|" + value.Kind + "|" + value.X + "," + value.Y)),
            }));
        }

        private readonly IReadOnlyList<Rmap16TerrainCell> sourceCells;
        private readonly ReadOnlyCollection<Rmap16TerrainCell> solidCells;
        private readonly ReadOnlyCollection<Rmap16TerrainCell> oneWayCells;
        private readonly ReadOnlyCollection<Rmap16TerrainCell> airCells;
        private readonly ReadOnlyCollection<Rmap16Overlay> ladderOverlays;
        private readonly ReadOnlyCollection<Rmap16Overlay> grabOverlays;
        private readonly ReadOnlyCollection<Rmap16Overlay> markerOverlays;

        public const int WidthTiles = RmapWorldBiomePlanner.WorldWidthTiles;
        public const int HeightTiles = RmapWorldBiomePlanner.WorldHeightTiles;

        public Rmap16ClusterAssemblyPlan SourceAssembly { get; }
        public Rmap16BakeSnapshot Snapshot { get; }
        public string SourcePlanDigest => Snapshot.SourcePlanDigest;
        public string SourceCellDigest => Snapshot.SourceCellDigest;
        public string SourceSpecialReservationDigest => SourceAssembly.SpecialPlan.Digest;
        public RmapSpecialWorldPoint PlayerStart { get; }
        public IReadOnlyList<Rmap16TerrainCell> SourceCells => sourceCells;
        public IReadOnlyList<Rmap16TerrainCell> SolidCells => solidCells;
        public IReadOnlyList<Rmap16TerrainCell> OneWayCells => oneWayCells;
        public IReadOnlyList<Rmap16TerrainCell> AirCells => airCells;
        public IReadOnlyList<Rmap16Overlay> LadderOverlays => ladderOverlays;
        public IReadOnlyList<Rmap16Overlay> GrabOverlays => grabOverlays;
        public IReadOnlyList<Rmap16Overlay> MarkerOverlays => markerOverlays;
        public int SourceCellCount => sourceCells.Count;
        public int SolidCellCount => solidCells.Count;
        public int AirCellCount => airCells.Count;
        public int OneWayCellCount => oneWayCells.Count;
        public int LadderOverlayCount => ladderOverlays.Count;
        public int GrabOverlayCount => grabOverlays.Count;
        public int MarkerOverlayCount => markerOverlays.Count;
        public string Digest { get; }

        public Rmap16TerrainCell GetCell(int x, int y)
        {
            if (x < 0 || x >= WidthTiles || y < 0 || y >= HeightTiles)
                throw new ArgumentOutOfRangeException(nameof(x));
            return sourceCells[(y * WidthTiles) + x];
        }

        public bool IsExactStaticWorld => SourceCellCount == WidthTiles * HeightTiles &&
            SolidCellCount + AirCellCount + OneWayCellCount == SourceCellCount &&
            SourceCells.Select(value => value.X + "," + value.Y).Distinct(StringComparer.Ordinal).Count() == SourceCellCount &&
            SourceCells.All(value => value.X >= 0 && value.X < WidthTiles && value.Y >= 0 && value.Y < HeightTiles) &&
            GetCell(PlayerStart.X, PlayerStart.Y).BaseCell == RmapPatternBaseCell.Air &&
            GetCell(PlayerStart.X, PlayerStart.Y - 1).BaseCell == RmapPatternBaseCell.Solid;

        private static ReadOnlyCollection<Rmap16TerrainCell> Freeze(IEnumerable<Rmap16TerrainCell> values) =>
            new ReadOnlyCollection<Rmap16TerrainCell>((values ?? Array.Empty<Rmap16TerrainCell>()).ToArray());

        private static ReadOnlyCollection<Rmap16Overlay> FreezeOverlays(IEnumerable<Rmap16Overlay> values) =>
            new ReadOnlyCollection<Rmap16Overlay>((values ?? Array.Empty<Rmap16Overlay>())
                .OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
    }

    public static class RmapWorldBakeExecutor
    {
        public static Rmap17WorldBakePlan Build(
            RmapWorldDefinition definition,
            WorldGenerationRngStreams rngStreams)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            Rmap16ClusterAssemblyPlan assembly = RmapClusterAssemblyPlanner.Plan(definition, rngStreams);
            return Build(assembly, RmapClusterAssemblyPlanner.CreateBakeSnapshot(assembly));
        }

        public static Rmap17WorldBakePlan Build(
            Rmap16ClusterAssemblyPlan sourceAssembly,
            Rmap16BakeSnapshot snapshot)
        {
            if (sourceAssembly == null) throw new ArgumentNullException(nameof(sourceAssembly));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!sourceAssembly.Success || !snapshot.IsExactWorld ||
                !string.Equals(snapshot.SourcePlanDigest, sourceAssembly.Digest, StringComparison.Ordinal) ||
                !string.Equals(snapshot.SourceCellDigest, sourceAssembly.CellDigest, StringComparison.Ordinal) ||
                !ReferenceEquals(snapshot.Cells, sourceAssembly.Cells) ||
                !ReferenceEquals(snapshot.Overlays, sourceAssembly.Overlays))
                throw new ArgumentException("RMAP17 requires the direct, successful RMAP16 snapshot handoff.", nameof(snapshot));

            RmapSpecialSlot startSlot = sourceAssembly.SpecialPlan.Slots.Single(value =>
                value.Kind == RmapSpecialSlotKind.Spawn);
            var plan = new Rmap17WorldBakePlan(sourceAssembly, snapshot, startSlot.World);
            if (!plan.IsExactStaticWorld)
                throw new InvalidOperationException("RMAP17 source cells or RMAP15 Start support are invalid.");
            if (sourceAssembly.SpecialPlan.Cells.Any(value =>
                plan.GetCell(value.World.X, value.World.Y).BaseCell != value.BaseCell))
                throw new InvalidOperationException("RMAP17 may not reinterpret protected RMAP15 cells.");
            if (plan.LadderOverlays.Any(value => !InBounds(value.X, value.Y)) ||
                plan.GrabOverlays.Any(value => !InBounds(value.X, value.Y)) ||
                plan.MarkerOverlays.Any(value => !InBounds(value.X, value.Y)))
                throw new InvalidOperationException("RMAP17 overlay coordinates must remain in the full world bounds.");
            return plan;
        }

        private static bool InBounds(int x, int y) =>
            x >= 0 && x < Rmap17WorldBakePlan.WidthTiles && y >= 0 && y < Rmap17WorldBakePlan.HeightTiles;
    }
}
