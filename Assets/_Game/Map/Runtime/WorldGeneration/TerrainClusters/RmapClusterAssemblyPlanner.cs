using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Biomes;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    /// <summary>
    /// RMAP16's static, full-world handoff.  This deliberately stops before
    /// RMAP17's Tilemap baking: every entry is a base-cell or separate overlay.
    /// </summary>
    public enum Rmap16TerrainSourceKind
    {
        DensityField = 1,
        Pattern = 2,
        ClusterSlope = 3,
        ClusterCave = 4,
        ClusterCorridor = 5,
        ClusterHalfPipe = 6,
        Route = 7,
        Secret = 8,
        SpecialReservation = 9,
    }

    public enum Rmap16ChunkState { Active = 1, Secret = 2, InactiveSolid = 3, SpecialReserved = 4 }

    public sealed class Rmap16TerrainCell
    {
        internal Rmap16TerrainCell(int x, int y, RmapPatternBaseCell baseCell,
            Rmap16TerrainSourceKind sourceKind, string sourceId, string patchId, string patternCandidateId)
        {
            X = x; Y = y; BaseCell = baseCell; SourceKind = sourceKind;
            SourceId = sourceId ?? string.Empty; PatchId = patchId ?? string.Empty;
            PatternCandidateId = patternCandidateId ?? string.Empty;
        }

        public int X { get; }
        public int Y { get; }
        public RmapPatternBaseCell BaseCell { get; }
        public Rmap16TerrainSourceKind SourceKind { get; }
        public string SourceId { get; }
        public string PatchId { get; }
        public string PatternCandidateId { get; }
    }

    public sealed class Rmap16Cluster
    {
        internal Rmap16Cluster(string id, string shape, int microX, int microY, int width, int height)
        {
            Id = id; Shape = shape; MicroX = microX; MicroY = microY; WidthMicroChunks = width;
            HeightMicroChunks = height;
            ChunkCoordinates = new ReadOnlyCollection<Rmap16MicroChunkCoordinate>(Enumerable.Range(0, height)
                .SelectMany(y => Enumerable.Range(0, width).Select(x => new Rmap16MicroChunkCoordinate(microX + x, microY + y)))
                .OrderBy(value => value).ToArray());
            MaskDigest = RmapWorldDefinition.Hash(string.Join("\n", new[] { "RMAP16_CLUSTER_MASK_V1", id, shape,
                string.Join(";", ChunkCoordinates.Select(value => value.X.ToString(CultureInfo.InvariantCulture) + "," +
                    value.Y.ToString(CultureInfo.InvariantCulture))) }));
        }

        public string Id { get; }
        public string Shape { get; }
        public int MicroX { get; }
        public int MicroY { get; }
        public int WidthMicroChunks { get; }
        public int HeightMicroChunks { get; }
        public IReadOnlyList<Rmap16MicroChunkCoordinate> ChunkCoordinates { get; }
        public int MicroChunkCount => ChunkCoordinates.Count;
        public string MaskDigest { get; }
    }

    public readonly struct Rmap16MicroChunkCoordinate : IComparable<Rmap16MicroChunkCoordinate>, IEquatable<Rmap16MicroChunkCoordinate>
    {
        public Rmap16MicroChunkCoordinate(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public int CompareTo(Rmap16MicroChunkCoordinate other) { int result = Y.CompareTo(other.Y); return result != 0 ? result : X.CompareTo(other.X); }
        public bool Equals(Rmap16MicroChunkCoordinate other) => X == other.X && Y == other.Y;
        public override bool Equals(object value) => value is Rmap16MicroChunkCoordinate && Equals((Rmap16MicroChunkCoordinate)value);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," + Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class Rmap16Chunk
    {
        internal Rmap16Chunk(Rmap16MicroChunkCoordinate coordinate, string patchId, Rmap16ChunkState state,
            string clusterId, int protectedCellCount, string portType)
        {
            Coordinate = coordinate; PatchId = patchId; State = state; ClusterId = clusterId ?? string.Empty;
            ProtectedCellCount = protectedCellCount; PortType = portType ?? string.Empty;
        }
        public Rmap16MicroChunkCoordinate Coordinate { get; }
        public string PatchId { get; }
        public Rmap16ChunkState State { get; }
        public string ClusterId { get; }
        public int ProtectedCellCount { get; }
        public string PortType { get; }
    }

    public sealed class Rmap16PatternPlacement
    {
        internal Rmap16PatternPlacement(string clusterId, int x, int y, RmapPatternPool500Entry entry)
        {
            ClusterId = clusterId; X = x; Y = y; CandidateId = entry.CandidateId; PoolIndex = entry.PoolIndex;
            Role = entry.PrimaryRole.ToString(); SourceId = entry.SourceId; BaseCells16 = entry.BaseCells16;
        }
        public string ClusterId { get; }
        public int X { get; }
        public int Y { get; }
        public string CandidateId { get; }
        public int PoolIndex { get; }
        public string Role { get; }
        public string SourceId { get; }
        public string BaseCells16 { get; }
    }

    public sealed class Rmap16Overlay
    {
        internal Rmap16Overlay(string id, string kind, int x, int y, string ownerId, string condition)
        { Id = id; Kind = kind; X = x; Y = y; OwnerId = ownerId; Condition = condition; }
        public string Id { get; }
        public string Kind { get; }
        public int X { get; }
        public int Y { get; }
        public string OwnerId { get; }
        public string Condition { get; }
    }

    public sealed class Rmap16Port
    {
        internal Rmap16Port(RmapSpecialAccess access, RmapSpecialWorldPoint point)
        {
            Id = access.Id; SiteId = access.SiteId; NodeId = access.SourceNodeId; Side = access.Side.ToString();
            Flow = access.Flow.ToString(); Required = access.Required; Condition = access.Condition; X = point.X; Y = point.Y;
        }
        public string Id { get; }
        public string SiteId { get; }
        public string NodeId { get; }
        public string Side { get; }
        public string Flow { get; }
        public bool Required { get; }
        public string Condition { get; }
        public int X { get; }
        public int Y { get; }
    }

    public sealed class Rmap16Route
    {
        internal Rmap16Route(RmapWorldGraphEdge edge, string fromPortId, string toPortId,
            IEnumerable<RmapSpecialWorldPoint> cells)
        {
            EdgeId = edge.EdgeId; SourceNodeId = edge.SourceNodeId; TargetNodeId = edge.TargetNodeId;
            Condition = edge.TraversalCondition; FromPortId = fromPortId; ToPortId = toPortId;
            Cells = new ReadOnlyCollection<RmapSpecialWorldPoint>((cells ?? Array.Empty<RmapSpecialWorldPoint>()).ToArray());
            StaticTraversalContract = "PLAYER_0.4x0.8|STEP_MAX_1|JUMP_MAX_2|GRAB_REQUIRED_FOR_VERTICAL";
        }
        public string EdgeId { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }
        public string Condition { get; }
        public string FromPortId { get; }
        public string ToPortId { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Cells { get; }
        public string StaticTraversalContract { get; }
    }

    public sealed class Rmap16Secret
    {
        internal Rmap16Secret(string id, IEnumerable<Rmap16MicroChunkCoordinate> chunks,
            RmapSpecialWorldPoint breakable, IEnumerable<RmapSpecialWorldPoint> clues)
        {
            Id = id; Chunks = new ReadOnlyCollection<Rmap16MicroChunkCoordinate>((chunks ??
                Array.Empty<Rmap16MicroChunkCoordinate>()).OrderBy(value => value).ToArray());
            BreakableAccess = breakable; Clues = new ReadOnlyCollection<RmapSpecialWorldPoint>((clues ??
                Array.Empty<RmapSpecialWorldPoint>()).OrderBy(value => value).ToArray());
        }
        public string Id { get; }
        public IReadOnlyList<Rmap16MicroChunkCoordinate> Chunks { get; }
        public RmapSpecialWorldPoint BreakableAccess { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Clues { get; }
    }

    public sealed class Rmap16StateGeometry
    {
        internal Rmap16StateGeometry(string ownerId, string state, RmapSpecialWorldPoint point,
            RmapPatternBaseCell baseCell, string reason)
        { OwnerId = ownerId; State = state; Point = point; BaseCell = baseCell; Reason = reason; }
        public string OwnerId { get; }
        public string State { get; }
        public RmapSpecialWorldPoint Point { get; }
        public RmapPatternBaseCell BaseCell { get; }
        public string Reason { get; }
    }

    public sealed class Rmap16DensityMeasurement
    {
        internal Rmap16DensityMeasurement(string patchId, RmapBiomeDensityProfileId profile,
            int solid, int air, int oneWay, int minimum, int maximum)
        { PatchId = patchId; Profile = profile; Solid = solid; Air = air; OneWay = oneWay; MinimumPermille = minimum; MaximumPermille = maximum; }
        public string PatchId { get; }
        public RmapBiomeDensityProfileId Profile { get; }
        public int Solid { get; }
        public int Air { get; }
        public int OneWay { get; }
        public int Total => Solid + Air + OneWay;
        public int DensityPermille => Total == 0 ? 0 : (Solid * 1000) / Total;
        public int MinimumPermille { get; }
        public int MaximumPermille { get; }
        public bool IsWithinTarget => DensityPermille >= MinimumPermille && DensityPermille <= MaximumPermille;
    }

    public sealed class Rmap16ClusterAssemblyPlan
    {
        internal Rmap16ClusterAssemblyPlan(RmapWorldDefinition definition, RmapWorldGraphPlan graph,
            RmapWorldBiomePlan biomePlan, RmapSpecialReservationPlan specialPlan,
            IEnumerable<Rmap16TerrainCell> cells, IEnumerable<Rmap16Cluster> clusters,
            IEnumerable<Rmap16Chunk> chunks, IEnumerable<Rmap16PatternPlacement> patterns,
            IEnumerable<Rmap16Overlay> overlays, IEnumerable<Rmap16Port> ports,
            IEnumerable<Rmap16Route> routes, IEnumerable<Rmap16Secret> secrets,
            IEnumerable<Rmap16StateGeometry> stateGeometry, IEnumerable<Rmap16DensityMeasurement> densities,
            int terrainReservationGateCalls, int rejectedProtectedWrites)
        {
            Definition = definition; Graph = graph; BiomePlan = biomePlan; SpecialPlan = specialPlan;
            Cells = new ReadOnlyCollection<Rmap16TerrainCell>((cells ?? Array.Empty<Rmap16TerrainCell>()).ToArray());
            Clusters = new ReadOnlyCollection<Rmap16Cluster>((clusters ?? Array.Empty<Rmap16Cluster>()).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
            Chunks = new ReadOnlyCollection<Rmap16Chunk>((chunks ?? Array.Empty<Rmap16Chunk>()).OrderBy(value => value.Coordinate).ToArray());
            Patterns = new ReadOnlyCollection<Rmap16PatternPlacement>((patterns ?? Array.Empty<Rmap16PatternPlacement>()).OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
            Overlays = new ReadOnlyCollection<Rmap16Overlay>((overlays ?? Array.Empty<Rmap16Overlay>()).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
            Ports = new ReadOnlyCollection<Rmap16Port>((ports ?? Array.Empty<Rmap16Port>()).OrderBy(value => value.Id, StringComparer.Ordinal).ThenBy(value => value.Y).ToArray());
            Routes = new ReadOnlyCollection<Rmap16Route>((routes ?? Array.Empty<Rmap16Route>()).OrderBy(value => value.EdgeId, StringComparer.Ordinal).ToArray());
            Secrets = new ReadOnlyCollection<Rmap16Secret>((secrets ?? Array.Empty<Rmap16Secret>()).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
            StateGeometry = new ReadOnlyCollection<Rmap16StateGeometry>((stateGeometry ?? Array.Empty<Rmap16StateGeometry>()).OrderBy(value => value.OwnerId, StringComparer.Ordinal).ThenBy(value => value.State, StringComparer.Ordinal).ThenBy(value => value.Point.Y).ThenBy(value => value.Point.X).ToArray());
            Densities = new ReadOnlyCollection<Rmap16DensityMeasurement>((densities ?? Array.Empty<Rmap16DensityMeasurement>()).OrderBy(value => value.PatchId, StringComparer.Ordinal).ToArray());
            TerrainReservationGateCalls = terrainReservationGateCalls; RejectedProtectedWrites = rejectedProtectedWrites;
            Digest = RmapWorldDefinition.Hash(string.Join("\n", new[] { "RMAP16_CLUSTER_ASSEMBLY_V1", definition.Digest,
                graph.Digest, biomePlan.Digest, specialPlan.Digest, string.Join(";", Clusters.Select(value => value.MaskDigest)),
                string.Join(";", Routes.Select(value => value.EdgeId + ":" + value.Cells.Count.ToString(CultureInfo.InvariantCulture))),
                string.Join(";", Densities.Select(value => value.PatchId + ":" + value.DensityPermille.ToString(CultureInfo.InvariantCulture)))}));
        }
        public RmapWorldDefinition Definition { get; }
        public RmapWorldGraphPlan Graph { get; }
        public RmapWorldBiomePlan BiomePlan { get; }
        public RmapSpecialReservationPlan SpecialPlan { get; }
        public IReadOnlyList<Rmap16TerrainCell> Cells { get; }
        public IReadOnlyList<Rmap16Cluster> Clusters { get; }
        public IReadOnlyList<Rmap16Chunk> Chunks { get; }
        public IReadOnlyList<Rmap16PatternPlacement> Patterns { get; }
        public IReadOnlyList<Rmap16Overlay> Overlays { get; }
        public IReadOnlyList<Rmap16Port> Ports { get; }
        public IReadOnlyList<Rmap16Route> Routes { get; }
        public IReadOnlyList<Rmap16Secret> Secrets { get; }
        public IReadOnlyList<Rmap16StateGeometry> StateGeometry { get; }
        public IReadOnlyList<Rmap16DensityMeasurement> Densities { get; }
        public int TerrainReservationGateCalls { get; }
        public int RejectedProtectedWrites { get; }
        public string Digest { get; }
        public bool Success => Cells.Count == RmapWorldBiomePlanner.WorldWidthTiles * RmapWorldBiomePlanner.WorldHeightTiles &&
            Chunks.Count == RmapWorldBiomePlanner.MicroChunkCount && Clusters.All(value => value.MicroChunkCount >= 2 && value.MicroChunkCount <= 8) &&
            Densities.All(value => value.IsWithinTarget) && TerrainReservationGateCalls > 0;
        public Rmap16TerrainCell GetCell(int x, int y) => Cells[(y * RmapWorldBiomePlanner.WorldWidthTiles) + x];
    }

    public static class RmapClusterAssemblyPlanner
    {
        public const string RulesetVersion = "RMAP16_CLUSTERS_V1";
        public const int WorldWidthTiles = RmapWorldBiomePlanner.WorldWidthTiles;
        public const int WorldHeightTiles = RmapWorldBiomePlanner.WorldHeightTiles;
        public const int MicroChunkWidthTiles = RmapWorldBiomePlanner.MicroChunkWidthTiles;
        public const int MicroChunkHeightTiles = RmapWorldBiomePlanner.MicroChunkHeightTiles;

        public static Rmap16ClusterAssemblyPlan Plan(RmapWorldDefinition definition, WorldGenerationRngStreams rngStreams)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            RmapWorldBiomePlan biome = RmapWorldBiomePlanner.Plan(definition, rngStreams);
            return Plan(definition, biome.Graph, biome, RmapSpecialReservationPlanner.Plan(biome, rngStreams), rngStreams);
        }

        /// <summary>Explicit RMAP12 -> RMAP13 -> RMAP14 -> RMAP15 consumer used by RMAP17's future bake adapter.</summary>
        public static Rmap16ClusterAssemblyPlan Plan(RmapWorldDefinition definition, RmapWorldGraphPlan graph,
            RmapWorldBiomePlan biomePlan, RmapSpecialReservationPlan specialPlan, WorldGenerationRngStreams rngStreams)
        {
            if (definition == null || graph == null || biomePlan == null || specialPlan == null || rngStreams == null)
                throw new ArgumentNullException("RMAP16 needs every verified upstream input.");
            if (!graph.Success || !biomePlan.Success || !specialPlan.Success || biomePlan.Definition.Digest != definition.Digest ||
                biomePlan.Graph.Digest != graph.Digest || specialPlan.BiomePlan.Digest != biomePlan.Digest)
                throw new ArgumentException("RMAP16 upstream contract linkage is invalid.");

            var ledger = new BuildLedger();
            Rmap16TerrainCell[] cells = CreateFixedSpecialLayer(biomePlan, specialPlan);
            FillDensityField(cells, definition, biomePlan, specialPlan, ledger);
            var occupied = new HashSet<Rmap16MicroChunkCoordinate>();
            var clusters = new List<Rmap16Cluster>
            {
                FindCluster("RMAP16_CLUSTER_SLOPE", "SLOPE", 2, 2, 3, 3, occupied, specialPlan),
                FindCluster("RMAP16_CLUSTER_CAVE", "CAVE", 2, 2, 22, 8, occupied, specialPlan),
                FindCluster("RMAP16_CLUSTER_CORRIDOR", "CORRIDOR", 3, 1, 4, 33, occupied, specialPlan),
                FindCluster("RMAP16_CLUSTER_HALFPIPE", "HALF_PIPE", 2, 1, 30, 42, occupied, specialPlan),
            };
            foreach (Rmap16Cluster cluster in clusters) foreach (Rmap16MicroChunkCoordinate coordinate in cluster.ChunkCoordinates) occupied.Add(coordinate);

            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();
            var patterns = new List<Rmap16PatternPlacement>();
            var overlays = new List<Rmap16Overlay>();
            int ordinal = 0;
            foreach (Rmap16Cluster cluster in clusters)
            {
                ApplyPatterns(cells, definition, specialPlan, pool, cluster, patterns, ledger, ref ordinal);
                ApplyShape(cells, specialPlan, cluster, overlays, ledger);
            }

            Rmap16Cluster secretCluster = FindCluster("RMAP16_SECRET_CAVITY", "SECRET", 2, 1, 36, 18, occupied, specialPlan);
            var secret = ApplySecret(cells, specialPlan, secretCluster, overlays, ledger);
            var ports = specialPlan.Accesses.SelectMany(value => value.OpenCells.Select(point => new Rmap16Port(value, point))).ToArray();
            List<Rmap16Route> routes = ApplyGraphRoutes(cells, graph, specialPlan, overlays, ledger);
            BalanceDensities(cells, biomePlan, specialPlan, ledger);
            List<Rmap16DensityMeasurement> densities = MeasureDensities(cells, biomePlan);
            List<Rmap16Chunk> chunks = BuildChunks(cells, biomePlan, specialPlan, clusters, secret);
            List<Rmap16StateGeometry> state = BuildStateGeometry(specialPlan, secret);
            return new Rmap16ClusterAssemblyPlan(definition, graph, biomePlan, specialPlan, cells, clusters, chunks,
                patterns, overlays, ports, routes, new[] { secret }, state, densities, ledger.GateCalls, ledger.RejectedWrites);
        }

        private static Rmap16TerrainCell[] CreateFixedSpecialLayer(RmapWorldBiomePlan biome, RmapSpecialReservationPlan special)
        {
            var result = new Rmap16TerrainCell[WorldWidthTiles * WorldHeightTiles];
            for (int y = 0; y < WorldHeightTiles; y++) for (int x = 0; x < WorldWidthTiles; x++)
            {
                string patch = biome.GetCell(x / MicroChunkWidthTiles, y / MicroChunkHeightTiles).PatchId;
                result[Index(x, y)] = new Rmap16TerrainCell(x, y, RmapPatternBaseCell.Air,
                    Rmap16TerrainSourceKind.DensityField, "UNINITIALIZED", patch, string.Empty);
            }
            foreach (RmapSpecialWorldCell cell in special.Cells)
                result[Index(cell.World.X, cell.World.Y)] = new Rmap16TerrainCell(cell.World.X, cell.World.Y,
                    cell.BaseCell, Rmap16TerrainSourceKind.SpecialReservation, cell.SiteId, cell.PatchId, string.Empty);
            return result;
        }

        private static void FillDensityField(Rmap16TerrainCell[] cells, RmapWorldDefinition definition,
            RmapWorldBiomePlan biome, RmapSpecialReservationPlan special, BuildLedger ledger)
        {
            for (int microY = 0; microY < RmapWorldBiomePlanner.MicroChunkRows; microY++)
            for (int microX = 0; microX < RmapWorldBiomePlanner.MicroChunkColumns; microX++)
            {
                RmapWorldBiomePatch patch = biome.GetCell(microX, microY).Patch;
                RmapBiomeDensityProfileDefinition profile = RmapWorldBiomePlanner.ProfileCatalog[patch.DensityProfile];
                int target = (profile.TargetMinimumPermille + profile.TargetMaximumPermille) / 2;
                var points = new List<RmapSpecialWorldPoint>(MicroChunkWidthTiles * MicroChunkHeightTiles);
                for (int y = microY * MicroChunkHeightTiles; y < (microY + 1) * MicroChunkHeightTiles; y++)
                for (int x = microX * MicroChunkWidthTiles; x < (microX + 1) * MicroChunkWidthTiles; x++) points.Add(new RmapSpecialWorldPoint(x, y));
                WriteGeneral(cells, special, points, point => DensityValue(definition.Request.Seed, point.X, point.Y, target),
                    Rmap16TerrainSourceKind.DensityField, patch.PatchId, string.Empty, ledger);
            }
        }

        private static RmapPatternBaseCell DensityValue(ulong seed, int x, int y, int targetPermille)
        {
            unchecked
            {
                uint value = (uint)seed ^ ((uint)x * 747796405u) ^ ((uint)y * 2891336453u);
                value = (value ^ (value >> 16)) * 2246822519u;
                return (value % 1000u) < targetPermille ? RmapPatternBaseCell.Solid : RmapPatternBaseCell.Air;
            }
        }

        private static Rmap16Cluster FindCluster(string id, string shape, int width, int height, int preferredX,
            int preferredY, ISet<Rmap16MicroChunkCoordinate> occupied, RmapSpecialReservationPlan special)
        {
            for (int offsetY = 0; offsetY < RmapWorldBiomePlanner.MicroChunkRows; offsetY++)
            for (int offsetX = 0; offsetX < RmapWorldBiomePlanner.MicroChunkColumns; offsetX++)
            {
                int x = (preferredX + offsetX) % RmapWorldBiomePlanner.MicroChunkColumns;
                int y = (preferredY + offsetY) % RmapWorldBiomePlanner.MicroChunkRows;
                if (x + width > RmapWorldBiomePlanner.MicroChunkColumns || y + height > RmapWorldBiomePlanner.MicroChunkRows) continue;
                bool clear = true;
                for (int cy = y; cy < y + height && clear; cy++) for (int cx = x; cx < x + width; cx++)
                {
                    if (occupied.Contains(new Rmap16MicroChunkCoordinate(cx, cy)) || ChunkHasSpecial(cx, cy, special)) { clear = false; break; }
                }
                if (clear) return new Rmap16Cluster(id, shape, x, y, width, height);
            }
            throw new InvalidOperationException("No non-reserved contiguous RMAP16 cluster footprint remains for " + id + ".");
        }

        private static bool ChunkHasSpecial(int microX, int microY, RmapSpecialReservationPlan special)
        {
            for (int y = microY * MicroChunkHeightTiles; y < (microY + 1) * MicroChunkHeightTiles; y++)
            for (int x = microX * MicroChunkWidthTiles; x < (microX + 1) * MicroChunkWidthTiles; x++)
                if (special.TryGetCell(new RmapSpecialWorldPoint(x, y), out _)) return true;
            return false;
        }

        private static void ApplyPatterns(Rmap16TerrainCell[] cells, RmapWorldDefinition definition,
            RmapSpecialReservationPlan special, RmapPatternPool500Snapshot pool, Rmap16Cluster cluster,
            ICollection<Rmap16PatternPlacement> output, BuildLedger ledger, ref int ordinal)
        {
            foreach (Rmap16MicroChunkCoordinate chunk in cluster.ChunkCoordinates)
            for (int slotY = 0; slotY < 2; slotY++) for (int slotX = 0; slotX < 3; slotX++)
            {
                int poolIndex = (int)((definition.Request.Seed + (ulong)(ordinal * 37 + slotX * 11 + slotY * 17)) % (ulong)pool.Candidates.Count);
                RmapPatternPool500Entry entry = pool.Candidates[poolIndex];
                int originX = chunk.X * MicroChunkWidthTiles + slotX * RmapPatternCatalog.Width;
                int originY = chunk.Y * MicroChunkHeightTiles + slotY * RmapPatternCatalog.Height;
                var points = Enumerable.Range(0, RmapPatternCatalog.Height).SelectMany(localY => Enumerable.Range(0,
                    RmapPatternCatalog.Width).Select(localX => new RmapSpecialWorldPoint(originX + localX, originY + localY))).ToArray();
                WriteGeneral(cells, special, points, point => entry.GetCell(point.X - originX, point.Y - originY),
                    Rmap16TerrainSourceKind.Pattern, cluster.Id, entry.CandidateId, ledger);
                output.Add(new Rmap16PatternPlacement(cluster.Id, originX, originY, entry));
                ordinal++;
            }
        }

        private static void ApplyShape(Rmap16TerrainCell[] cells, RmapSpecialReservationPlan special,
            Rmap16Cluster cluster, ICollection<Rmap16Overlay> overlays, BuildLedger ledger)
        {
            int minX = cluster.MicroX * MicroChunkWidthTiles; int minY = cluster.MicroY * MicroChunkHeightTiles;
            int width = cluster.WidthMicroChunks * MicroChunkWidthTiles; int height = cluster.HeightMicroChunks * MicroChunkHeightTiles;
            var points = Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x =>
                new RmapSpecialWorldPoint(minX + x, minY + y))).ToArray();
            Rmap16TerrainSourceKind kind = cluster.Shape == "SLOPE" ? Rmap16TerrainSourceKind.ClusterSlope :
                cluster.Shape == "CAVE" ? Rmap16TerrainSourceKind.ClusterCave : cluster.Shape == "CORRIDOR" ?
                Rmap16TerrainSourceKind.ClusterCorridor : Rmap16TerrainSourceKind.ClusterHalfPipe;
            WriteGeneral(cells, special, points, point => ShapeValue(cluster.Shape, point.X - minX, point.Y - minY, width, height),
                kind, cluster.Id, string.Empty, ledger);
            if (cluster.Shape == "CAVE")
            {
                for (int y = 3; y < height - 2; y += 4) overlays.Add(new Rmap16Overlay(cluster.Id + "_LADDER_" + y,
                    "LADDER", minX + width / 2, minY + y, cluster.Id, "STATIC_CLIMB_GUIDE"));
            }
            if (cluster.Shape == "HALF_PIPE")
            {
                overlays.Add(new Rmap16Overlay(cluster.Id + "_GRAB_LEFT", "GRAB_EDGE", minX + 1, minY + height - 3,
                    cluster.Id, "STATIC_GRAB_REQUIRED"));
                overlays.Add(new Rmap16Overlay(cluster.Id + "_GRAB_RIGHT", "GRAB_EDGE", minX + width - 2, minY + height - 3,
                    cluster.Id, "STATIC_GRAB_REQUIRED"));
            }
        }

        private static RmapPatternBaseCell ShapeValue(string shape, int x, int y, int width, int height)
        {
            if (shape == "SLOPE")
            {
                int floor = 1 + (x * Math.Max(1, height - 5)) / Math.Max(1, width - 1);
                return y <= floor ? RmapPatternBaseCell.Solid : RmapPatternBaseCell.Air;
            }
            if (shape == "CAVE")
            {
                int dx = Math.Abs((x * 2) - (width - 1)); int dy = Math.Abs((y * 2) - (height - 1));
                if ((dx * dx) / Math.Max(1, width) + (dy * dy) / Math.Max(1, height) < Math.Max(width, height) / 2)
                    return y == 2 ? RmapPatternBaseCell.OneWayPlatform : RmapPatternBaseCell.Air;
                return RmapPatternBaseCell.Solid;
            }
            if (shape == "CORRIDOR") return y == 1 ? RmapPatternBaseCell.OneWayPlatform :
                (y >= 2 && y <= Math.Min(height - 2, 5) ? RmapPatternBaseCell.Air : RmapPatternBaseCell.Solid);
            int middle = Math.Abs(x - ((width - 1) / 2)); int floorHalfPipe = 1 + (middle * Math.Max(1, height - 5)) / Math.Max(1, width / 2);
            return y < floorHalfPipe ? RmapPatternBaseCell.Solid : y == floorHalfPipe ? RmapPatternBaseCell.OneWayPlatform : RmapPatternBaseCell.Air;
        }

        private static Rmap16Secret ApplySecret(Rmap16TerrainCell[] cells, RmapSpecialReservationPlan special,
            Rmap16Cluster cluster, ICollection<Rmap16Overlay> overlays, BuildLedger ledger)
        {
            int minX = cluster.MicroX * MicroChunkWidthTiles; int minY = cluster.MicroY * MicroChunkHeightTiles;
            int width = cluster.WidthMicroChunks * MicroChunkWidthTiles; int height = cluster.HeightMicroChunks * MicroChunkHeightTiles;
            var points = Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x =>
                new RmapSpecialWorldPoint(minX + x, minY + y))).ToArray();
            RmapSpecialWorldPoint access = new RmapSpecialWorldPoint(minX, minY + height / 2);
            WriteGeneral(cells, special, points, point =>
            {
                int localX = point.X - minX; int localY = point.Y - minY;
                if (point.Equals(access)) return RmapPatternBaseCell.Solid;
                return localX > 1 && localX < width - 2 && localY > 1 && localY < height - 2 ? RmapPatternBaseCell.Air : RmapPatternBaseCell.Solid;
            }, Rmap16TerrainSourceKind.Secret, cluster.Id, string.Empty, ledger);
            RmapSpecialWorldPoint firstClue = new RmapSpecialWorldPoint(minX + width / 3, minY + height / 2);
            RmapSpecialWorldPoint secondClue = new RmapSpecialWorldPoint(minX + (width * 2) / 3, minY + height / 2);
            overlays.Add(new Rmap16Overlay(cluster.Id + "_BREAKABLE", "BREAKABLE_ACCESS", access.X, access.Y, cluster.Id, "SECRET_SEALED"));
            overlays.Add(new Rmap16Overlay(cluster.Id + "_CLUE_A", "SECRET_CLUE", firstClue.X, firstClue.Y, cluster.Id, "OBSERVE_FIRST"));
            overlays.Add(new Rmap16Overlay(cluster.Id + "_CLUE_B", "SECRET_CLUE", secondClue.X, secondClue.Y, cluster.Id, "OBSERVE_FIRST"));
            return new Rmap16Secret(cluster.Id, cluster.ChunkCoordinates, access, new[] { firstClue, secondClue });
        }

        private static List<Rmap16Route> ApplyGraphRoutes(Rmap16TerrainCell[] cells, RmapWorldGraphPlan graph,
            RmapSpecialReservationPlan special, ICollection<Rmap16Overlay> overlays, BuildLedger ledger)
        {
            var routes = new List<Rmap16Route>();
            foreach (RmapWorldGraphEdge edge in graph.Edges.OrderBy(value => value.EdgeId, StringComparer.Ordinal))
            {
                RmapSpecialAccess from = SelectAccess(special, edge.SourceNodeId, true);
                RmapSpecialAccess to = SelectAccess(special, edge.TargetNodeId, false);
                RmapSpecialWorldPoint start = ExteriorPoint(from); RmapSpecialWorldPoint end = ExteriorPoint(to);
                List<RmapSpecialWorldPoint> path = FindUnreservedPath(start, end, special);
                if (path.Count == 0) throw new InvalidOperationException("RMAP16 could not make a protected-safe route for " + edge.EdgeId + ".");
                var clearance = path.Concat(path.Select(point => new RmapSpecialWorldPoint(point.X, point.Y + 1)))
                    .Where(InBounds).Distinct().ToArray();
                WriteGeneral(cells, special, clearance, point => RmapPatternBaseCell.Air, Rmap16TerrainSourceKind.Route,
                    edge.EdgeId, string.Empty, ledger);
                var pathSet = new HashSet<RmapSpecialWorldPoint>(path);
                var supports = path.Select(point => new RmapSpecialWorldPoint(point.X, point.Y - 1)).Where(point =>
                    InBounds(point) && !pathSet.Contains(point)).Distinct().ToArray();
                WriteGeneral(cells, special, supports, point => RmapPatternBaseCell.Solid, Rmap16TerrainSourceKind.Route,
                    edge.EdgeId + "_SUPPORT", string.Empty, ledger);
                for (int index = 1; index < path.Count; index++) if (path[index].X == path[index - 1].X && index % 4 == 0)
                    overlays.Add(new Rmap16Overlay(edge.EdgeId + "_GRAB_" + index.ToString(CultureInfo.InvariantCulture), "GRAB_EDGE",
                        path[index].X, path[index].Y, edge.EdgeId, "STATIC_VERTICAL_TRANSITION"));
                routes.Add(new Rmap16Route(edge, from.Id, to.Id, path));
            }
            return routes;
        }

        private static RmapSpecialAccess SelectAccess(RmapSpecialReservationPlan special, string nodeId, bool outgoing)
        {
            RmapSpecialAccess value = special.Accesses.Where(access => AccessOwnsNode(access, nodeId) &&
                (outgoing ? access.Flow == RmapSpecialAccessFlow.Out || access.Flow == RmapSpecialAccessFlow.Both :
                access.Flow == RmapSpecialAccessFlow.In || access.Flow == RmapSpecialAccessFlow.Both)).OrderBy(access => access.Id,
                StringComparer.Ordinal).FirstOrDefault();
            if (value == null) throw new InvalidOperationException("RMAP15 has no compatible port for graph node " + nodeId + ".");
            return value;
        }

        private static bool AccessOwnsNode(RmapSpecialAccess access, string nodeId) => (access.SourceNodeId ?? string.Empty)
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Any(value => string.Equals(value, nodeId, StringComparison.Ordinal));

        private static RmapSpecialWorldPoint ExteriorPoint(RmapSpecialAccess access)
        {
            RmapSpecialWorldPoint point = access.OpenCells.OrderBy(value => value).ElementAt(access.OpenCells.Count / 2);
            switch (access.Side)
            {
                case RmapWorldGraphDirection.Left: return new RmapSpecialWorldPoint(point.X - 1, point.Y);
                case RmapWorldGraphDirection.Right: return new RmapSpecialWorldPoint(point.X + 1, point.Y);
                case RmapWorldGraphDirection.Up: return new RmapSpecialWorldPoint(point.X, point.Y + 1);
                default: return new RmapSpecialWorldPoint(point.X, point.Y - 1);
            }
        }

        private static List<RmapSpecialWorldPoint> FindUnreservedPath(RmapSpecialWorldPoint start, RmapSpecialWorldPoint end,
            RmapSpecialReservationPlan special)
        {
            if (!InBounds(start) || !InBounds(end)) throw new ArgumentOutOfRangeException("Route port exterior is outside the world.");
            int total = WorldWidthTiles * WorldHeightTiles;
            var parent = Enumerable.Repeat(-2, total).ToArray(); var queue = new Queue<int>();
            int startIndex = Index(start.X, start.Y); int endIndex = Index(end.X, end.Y); parent[startIndex] = -1; queue.Enqueue(startIndex);
            int[] deltaX = { -1, 1, 0, 0 }; int[] deltaY = { 0, 0, -1, 1 };
            while (queue.Count != 0 && parent[endIndex] == -2)
            {
                int index = queue.Dequeue(); int x = index % WorldWidthTiles; int y = index / WorldWidthTiles;
                for (int direction = 0; direction < 4; direction++)
                {
                    int nx = x + deltaX[direction]; int ny = y + deltaY[direction];
                    if (nx < 0 || nx >= WorldWidthTiles || ny < 0 || ny >= WorldHeightTiles) continue;
                    int next = Index(nx, ny); if (parent[next] != -2 || special.TryGetCell(new RmapSpecialWorldPoint(nx, ny), out _)) continue;
                    parent[next] = index; queue.Enqueue(next);
                }
            }
            if (parent[endIndex] == -2) return new List<RmapSpecialWorldPoint>();
            var result = new List<RmapSpecialWorldPoint>();
            for (int cursor = endIndex; cursor != -1; cursor = parent[cursor]) result.Add(new RmapSpecialWorldPoint(cursor % WorldWidthTiles, cursor / WorldWidthTiles));
            result.Reverse(); return result;
        }

        private static void BalanceDensities(Rmap16TerrainCell[] cells, RmapWorldBiomePlan biome,
            RmapSpecialReservationPlan special, BuildLedger ledger)
        {
            foreach (RmapWorldBiomePatch patch in biome.Patches)
            {
                RmapBiomeDensityProfileDefinition profile = RmapWorldBiomePlanner.ProfileCatalog[patch.DensityProfile];
                int desiredSolid = (((profile.TargetMinimumPermille + profile.TargetMaximumPermille) / 2) *
                    ((patch.MaxTileX - patch.MinTileX + 1) * (patch.MaxTileY - patch.MinTileY + 1))) / 1000;
                List<RmapSpecialWorldPoint> candidates = Enumerable.Range(patch.MinTileY, patch.MaxTileY - patch.MinTileY + 1)
                    .SelectMany(y => Enumerable.Range(patch.MinTileX, patch.MaxTileX - patch.MinTileX + 1).Select(x => new RmapSpecialWorldPoint(x, y)))
                    .Where(point => cells[Index(point.X, point.Y)].SourceKind == Rmap16TerrainSourceKind.DensityField).ToList();
                int currentSolid = Enumerable.Range(patch.MinTileY, patch.MaxTileY - patch.MinTileY + 1).SelectMany(y =>
                    Enumerable.Range(patch.MinTileX, patch.MaxTileX - patch.MinTileX + 1).Select(x => cells[Index(x, y)])).Count(cell => cell.BaseCell == RmapPatternBaseCell.Solid);
                RmapPatternBaseCell replacement = currentSolid < desiredSolid ? RmapPatternBaseCell.Solid : RmapPatternBaseCell.Air;
                int needed = Math.Abs(desiredSolid - currentSolid);
                IEnumerable<RmapSpecialWorldPoint> selected = candidates.Where(point => cells[Index(point.X, point.Y)].BaseCell != replacement)
                    .OrderBy(point => point.Y).ThenBy(point => point.X).Take(needed).ToArray();
                WriteGeneral(cells, special, selected, point => replacement, Rmap16TerrainSourceKind.DensityField,
                    patch.PatchId + "_BALANCED", string.Empty, ledger);
            }
        }

        private static List<Rmap16DensityMeasurement> MeasureDensities(Rmap16TerrainCell[] cells, RmapWorldBiomePlan biome)
        {
            var output = new List<Rmap16DensityMeasurement>();
            foreach (RmapWorldBiomePatch patch in biome.Patches)
            {
                var patchCells = Enumerable.Range(patch.MinTileY, patch.MaxTileY - patch.MinTileY + 1).SelectMany(y =>
                    Enumerable.Range(patch.MinTileX, patch.MaxTileX - patch.MinTileX + 1).Select(x => cells[Index(x, y)])).ToArray();
                RmapBiomeDensityProfileDefinition profile = RmapWorldBiomePlanner.ProfileCatalog[patch.DensityProfile];
                output.Add(new Rmap16DensityMeasurement(patch.PatchId, patch.DensityProfile,
                    patchCells.Count(value => value.BaseCell == RmapPatternBaseCell.Solid),
                    patchCells.Count(value => value.BaseCell == RmapPatternBaseCell.Air),
                    patchCells.Count(value => value.BaseCell == RmapPatternBaseCell.OneWayPlatform),
                    profile.TargetMinimumPermille, profile.TargetMaximumPermille));
            }
            return output;
        }

        private static List<Rmap16Chunk> BuildChunks(Rmap16TerrainCell[] cells, RmapWorldBiomePlan biome,
            RmapSpecialReservationPlan special, IEnumerable<Rmap16Cluster> clusters, Rmap16Secret secret)
        {
            var clusterByChunk = clusters.SelectMany(cluster => cluster.ChunkCoordinates.Select(point => new { point, cluster.Id }))
                .ToDictionary(value => value.point, value => value.Id);
            var secretChunks = new HashSet<Rmap16MicroChunkCoordinate>(secret.Chunks);
            var result = new List<Rmap16Chunk>(RmapWorldBiomePlanner.MicroChunkCount);
            for (int y = 0; y < RmapWorldBiomePlanner.MicroChunkRows; y++) for (int x = 0; x < RmapWorldBiomePlanner.MicroChunkColumns; x++)
            {
                var coordinate = new Rmap16MicroChunkCoordinate(x, y); int protectedCells = 0;
                for (int tileY = y * MicroChunkHeightTiles; tileY < (y + 1) * MicroChunkHeightTiles; tileY++)
                for (int tileX = x * MicroChunkWidthTiles; tileX < (x + 1) * MicroChunkWidthTiles; tileX++) if (special.TryGetCell(new RmapSpecialWorldPoint(tileX, tileY), out _)) protectedCells++;
                string clusterId; Rmap16ChunkState state = secretChunks.Contains(coordinate) ? Rmap16ChunkState.Secret :
                    clusterByChunk.TryGetValue(coordinate, out clusterId) ? Rmap16ChunkState.Active : protectedCells > 0 ?
                    Rmap16ChunkState.SpecialReserved : Rmap16ChunkState.InactiveSolid;
                if (!clusterByChunk.TryGetValue(coordinate, out clusterId)) clusterId = string.Empty;
                string portType = state == Rmap16ChunkState.Active ? "TYPE0_PUBLIC" : state == Rmap16ChunkState.Secret ?
                    "TYPE0_SECRET_ONE_ENTRY" : state == Rmap16ChunkState.SpecialReserved ? "RMAP15_RESERVED_ACCESS" : "INACTIVE_NO_PORT";
                result.Add(new Rmap16Chunk(coordinate, biome.GetCell(x, y).PatchId, state, clusterId, protectedCells, portType));
            }
            return result;
        }

        private static List<Rmap16StateGeometry> BuildStateGeometry(RmapSpecialReservationPlan special, Rmap16Secret secret)
        {
            var output = special.StateGeometry.Select(value => new Rmap16StateGeometry(value.SiteId, value.State,
                value.World, value.BaseCell, value.Reason)).ToList();
            output.Add(new Rmap16StateGeometry(secret.Id, "SEALED", secret.BreakableAccess, RmapPatternBaseCell.Solid,
                "Breakable access is closed until the optional secret is discovered."));
            output.Add(new Rmap16StateGeometry(secret.Id, "OPEN", secret.BreakableAccess, RmapPatternBaseCell.Air,
                "Breakable access opens into the static secret cavity."));
            return output;
        }

        private static void WriteGeneral(Rmap16TerrainCell[] cells, RmapSpecialReservationPlan special,
            IEnumerable<RmapSpecialWorldPoint> proposed, Func<RmapSpecialWorldPoint, RmapPatternBaseCell> value,
            Rmap16TerrainSourceKind sourceKind, string sourceId, string patternCandidateId, BuildLedger ledger)
        {
            RmapSpecialWorldPoint[] points = (proposed ?? Array.Empty<RmapSpecialWorldPoint>()).Where(InBounds).Distinct().ToArray();
            if (points.Length == 0) return;
            ledger.GateCalls++;
            RmapSpecialTerrainReservationDecision decision = special.EvaluateTerrainCells(points);
            if (decision.IsAllowed) { foreach (RmapSpecialWorldPoint point in points) Set(cells, point, value(point), sourceKind, sourceId, patternCandidateId); return; }
            foreach (RmapSpecialWorldPoint point in points)
            {
                ledger.GateCalls++;
                if (special.EvaluateTerrainCells(new[] { point }).IsAllowed) Set(cells, point, value(point), sourceKind, sourceId, patternCandidateId);
                else ledger.RejectedWrites++;
            }
        }

        private static void Set(Rmap16TerrainCell[] cells, RmapSpecialWorldPoint point, RmapPatternBaseCell value,
            Rmap16TerrainSourceKind sourceKind, string sourceId, string patternCandidateId)
        {
            Rmap16TerrainCell old = cells[Index(point.X, point.Y)];
            cells[Index(point.X, point.Y)] = new Rmap16TerrainCell(point.X, point.Y, value, sourceKind, sourceId, old.PatchId, patternCandidateId);
        }

        private static int Index(int x, int y) => (y * WorldWidthTiles) + x;
        private static bool InBounds(RmapSpecialWorldPoint point) => point.X >= 0 && point.X < WorldWidthTiles && point.Y >= 0 && point.Y < WorldHeightTiles;
        private sealed class BuildLedger { public int GateCalls; public int RejectedWrites; }
    }

    public static class RmapClusterAssemblyExport
    {
        public static string ManifestJson(Rmap16ClusterAssemblyPlan plan) => "{\n" +
            "  \"format\": \"RMAP16_CLUSTER_ASSEMBLY_V1\",\n" +
            "  \"definition_digest\": \"" + plan.Definition.Digest + "\",\n" +
            "  \"rmap13_graph_digest\": \"" + plan.Graph.Digest + "\",\n" +
            "  \"rmap14_biome_digest\": \"" + plan.BiomePlan.Digest + "\",\n" +
            "  \"rmap15_special_digest\": \"" + plan.SpecialPlan.Digest + "\",\n" +
            "  \"cells\": " + plan.Cells.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"chunks\": " + plan.Chunks.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"terrain_reservation_gate_calls\": " + plan.TerrainReservationGateCalls.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"rmap17_bake\": \"NOT_STARTED\",\n" +
            "  \"digest\": \"" + plan.Digest + "\"\n}\n";

        public static string ClustersCsv(Rmap16ClusterAssemblyPlan plan) => Lines("cluster_id,shape,micro_x,micro_y,width_microchunks,height_microchunks,chunk_count,mask_digest",
            plan.Clusters.Select(value => Row(value.Id, value.Shape, value.MicroX, value.MicroY, value.WidthMicroChunks, value.HeightMicroChunks, value.MicroChunkCount, value.MaskDigest)));
        public static string ChunksCsv(Rmap16ClusterAssemblyPlan plan) => Lines("micro_x,micro_y,patch_id,state,cluster_id,protected_cell_count,port_type",
            plan.Chunks.Select(value => Row(value.Coordinate.X, value.Coordinate.Y, value.PatchId, value.State, value.ClusterId, value.ProtectedCellCount, value.PortType)));
        public static string CellsCsv(Rmap16ClusterAssemblyPlan plan) => Lines("world_x,world_y,base,source_kind,source_id,patch_id,pattern_candidate_id",
            plan.Cells.Select(value => Row(value.X, value.Y, Token(value.BaseCell), value.SourceKind, value.SourceId, value.PatchId, value.PatternCandidateId)));
        public static string PatternsCsv(Rmap16ClusterAssemblyPlan plan) => Lines("cluster_id,world_x,world_y,pool_index,candidate_id,role,source_id,base_cells16",
            plan.Patterns.Select(value => Row(value.ClusterId, value.X, value.Y, value.PoolIndex, value.CandidateId, value.Role, value.SourceId, value.BaseCells16)));
        public static string OverlaysCsv(Rmap16ClusterAssemblyPlan plan) => Lines("overlay_id,kind,world_x,world_y,owner_id,condition",
            plan.Overlays.Select(value => Row(value.Id, value.Kind, value.X, value.Y, value.OwnerId, value.Condition)));
        public static string PortsCsv(Rmap16ClusterAssemblyPlan plan) => Lines("port_id,site_id,node_id,side,flow,required,condition,world_x,world_y",
            plan.Ports.Select(value => Row(value.Id, value.SiteId, value.NodeId, value.Side, value.Flow, value.Required, value.Condition, value.X, value.Y)));
        public static string RoutesCsv(Rmap16ClusterAssemblyPlan plan) => Lines("edge_id,source_node_id,target_node_id,condition,from_port_id,to_port_id,cell_ordinal,world_x,world_y,static_traversal_contract",
            plan.Routes.SelectMany(route => route.Cells.Select((cell, index) => Row(route.EdgeId, route.SourceNodeId, route.TargetNodeId,
                route.Condition, route.FromPortId, route.ToPortId, index, cell.X, cell.Y, route.StaticTraversalContract))));
        public static string SecretsCsv(Rmap16ClusterAssemblyPlan plan) => Lines("secret_id,chunk_coordinates,breakable_x,breakable_y,clue_coordinates,entry_contract",
            plan.Secrets.Select(value => Row(value.Id, string.Join("|", value.Chunks), value.BreakableAccess.X, value.BreakableAccess.Y,
                string.Join("|", value.Clues), "ONE_DECLARED_BREAKABLE_ENTRY")));
        public static string StateGeometryCsv(Rmap16ClusterAssemblyPlan plan) => Lines("owner_id,state,world_x,world_y,base,reason",
            plan.StateGeometry.Select(value => Row(value.OwnerId, value.State, value.Point.X, value.Point.Y, Token(value.BaseCell), value.Reason)));
        public static string DensityCsv(Rmap16ClusterAssemblyPlan plan) => Lines("patch_id,profile,solid,air,one_way,total,density_permille,target_min_permille,target_max_permille,within_target",
            plan.Densities.Select(value => Row(value.PatchId, value.Profile, value.Solid, value.Air, value.OneWay, value.Total,
                value.DensityPermille, value.MinimumPermille, value.MaximumPermille, value.IsWithinTarget)));
        public static string ShapeSummaryCsv(Rmap16ClusterAssemblyPlan plan) => Lines("kind,count,detail",
            new[] { Row("CLUSTER", plan.Clusters.Count, "2_TO_8_CONNECTED_MICROCHUNKS"), Row("SECRET", plan.Secrets.Count,
                "1_TO_6_MICROCHUNKS_WITH_TWO_CLUES"), Row("ROUTE", plan.Routes.Count, "GRAPH_EDGE_STATIC_CELL_SPINES"),
                Row("OVERLAY", plan.Overlays.Count, "SEPARATE_FROM_BASE"), Row("RESERVATION_GATE", plan.TerrainReservationGateCalls,
                "RMAP15_EVALUATE_TERRAIN_CELLS") });

        private static string Lines(string header, IEnumerable<string> rows) => header + "\n" + string.Join("\n", rows ?? Array.Empty<string>()) + "\n";
        private static string Row(params object[] values) => string.Join(",", (values ?? Array.Empty<object>()).Select(value => "\"" +
            (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Replace("\"", "\"\"") + "\""));
        private static string Token(RmapPatternBaseCell cell) => cell == RmapPatternBaseCell.Air ? "A" : cell == RmapPatternBaseCell.Solid ? "S" : "O";
    }
}
