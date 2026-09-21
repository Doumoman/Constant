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
    /// SV5's static, full-world handoff.  This deliberately stops before
    /// SV5's Tilemap baking: every entry is a base-cell or separate overlay.
    /// </summary>
    public enum Sv5TerrainSourceKind
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
        InactiveSolid = 10,
    }

    public enum Sv5ChunkState { Active = 1, Secret = 2, InactiveSolid = 3, SpecialReserved = 4 }

    public sealed class Sv5TerrainCell
    {
        internal Sv5TerrainCell(int x, int y, Sv5PatternBaseCell baseCell,
            Sv5TerrainSourceKind sourceKind, string sourceId, string patchId, string patternCandidateId)
        {
            X = x; Y = y; BaseCell = baseCell; SourceKind = sourceKind;
            SourceId = sourceId ?? string.Empty; PatchId = patchId ?? string.Empty;
            PatternCandidateId = patternCandidateId ?? string.Empty;
        }

        public int X { get; }
        public int Y { get; }
        public Sv5PatternBaseCell BaseCell { get; }
        public Sv5TerrainSourceKind SourceKind { get; }
        public string SourceId { get; }
        public string PatchId { get; }
        public string PatternCandidateId { get; }
    }

    public sealed class Sv5Cluster
    {
        internal Sv5Cluster(string id, string shape, int microX, int microY, int width, int height)
        {
            Id = id; Shape = shape; MicroX = microX; MicroY = microY; WidthMicroChunks = width;
            HeightMicroChunks = height;
            ChunkCoordinates = new ReadOnlyCollection<Sv5MicroChunkCoordinate>(Enumerable.Range(0, height)
                .SelectMany(y => Enumerable.Range(0, width).Select(x => new Sv5MicroChunkCoordinate(microX + x, microY + y)))
                .OrderBy(value => value).ToArray());
            MaskDigest = Sv5WorldDefinition.Hash(string.Join("\n", new[] { "SV5_CLUSTER_MASK_V1", id, shape,
                string.Join(";", ChunkCoordinates.Select(value => value.X.ToString(CultureInfo.InvariantCulture) + "," +
                    value.Y.ToString(CultureInfo.InvariantCulture))) }));
        }

        public string Id { get; }
        public string Shape { get; }
        public int MicroX { get; }
        public int MicroY { get; }
        public int WidthMicroChunks { get; }
        public int HeightMicroChunks { get; }
        public IReadOnlyList<Sv5MicroChunkCoordinate> ChunkCoordinates { get; }
        public int MicroChunkCount => ChunkCoordinates.Count;
        public string MaskDigest { get; }
    }

    public readonly struct Sv5MicroChunkCoordinate : IComparable<Sv5MicroChunkCoordinate>, IEquatable<Sv5MicroChunkCoordinate>
    {
        public Sv5MicroChunkCoordinate(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public int CompareTo(Sv5MicroChunkCoordinate other) { int result = Y.CompareTo(other.Y); return result != 0 ? result : X.CompareTo(other.X); }
        public bool Equals(Sv5MicroChunkCoordinate other) => X == other.X && Y == other.Y;
        public override bool Equals(object value) => value is Sv5MicroChunkCoordinate && Equals((Sv5MicroChunkCoordinate)value);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," + Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class Sv5Chunk
    {
        internal Sv5Chunk(Sv5MicroChunkCoordinate coordinate, string patchId, Sv5ChunkState state,
            string clusterId, int protectedCellCount, string portType)
        {
            Coordinate = coordinate; PatchId = patchId; State = state; ClusterId = clusterId ?? string.Empty;
            ProtectedCellCount = protectedCellCount; PortType = portType ?? string.Empty;
        }
        public Sv5MicroChunkCoordinate Coordinate { get; }
        public string PatchId { get; }
        public Sv5ChunkState State { get; }
        public string ClusterId { get; }
        public int ProtectedCellCount { get; }
        public string PortType { get; }
    }

    public sealed class Sv5PatternPlacement
    {
        internal Sv5PatternPlacement(string clusterId, int x, int y, Sv5PatternPool500Entry entry)
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

    public sealed class Sv5Overlay
    {
        internal Sv5Overlay(string id, string kind, int x, int y, string ownerId, string condition)
        { Id = id; Kind = kind; X = x; Y = y; OwnerId = ownerId; Condition = condition; }
        public string Id { get; }
        public string Kind { get; }
        public int X { get; }
        public int Y { get; }
        public string OwnerId { get; }
        public string Condition { get; }
    }

    public sealed class Sv5Port
    {
        internal Sv5Port(Sv5SpecialAccess access, Sv5SpecialWorldPoint point)
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

    public sealed class Sv5Route
    {
        internal Sv5Route(Sv5WorldGraphEdge edge, string fromPortId, string toPortId,
            IEnumerable<Sv5SpecialWorldPoint> cells, Sv5RouteEvidence evidence)
        {
            EdgeId = edge.EdgeId; SourceNodeId = edge.SourceNodeId; TargetNodeId = edge.TargetNodeId;
            Condition = edge.TraversalCondition; FromPortId = fromPortId; ToPortId = toPortId;
            Cells = new ReadOnlyCollection<Sv5SpecialWorldPoint>((cells ?? Array.Empty<Sv5SpecialWorldPoint>()).ToArray());
            Evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
        }
        public string EdgeId { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }
        public string Condition { get; }
        public string FromPortId { get; }
        public string ToPortId { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> Cells { get; }
        public Sv5RouteEvidence Evidence { get; }
    }

    /// <summary>Static route proof using the live Player movement surface contracts.
    /// It intentionally records a ladder transition instead of fabricating a jump height.</summary>
    public sealed class Sv5RouteEvidence
    {
        internal Sv5RouteEvidence(int walkTransitions, int climbTransitions, int boundaryPortTransitions,
            bool routeCellsHaveHeadroom, bool climbAnchorsHaveLiveSurface)
        {
            WalkTransitions = walkTransitions; ClimbTransitions = climbTransitions;
            BoundaryPortTransitions = boundaryPortTransitions;
            RouteCellsHaveHeadroom = routeCellsHaveHeadroom;
            ClimbAnchorsHaveLiveSurface = climbAnchorsHaveLiveSurface;
            Authority = "CharacterLiveMovementSettings.ConfigureSv502|CharacterLiveClimbSurface.StaticSafe";
        }
        public int WalkTransitions { get; }
        public int ClimbTransitions { get; }
        public int BoundaryPortTransitions { get; }
        public bool RouteCellsHaveHeadroom { get; }
        public bool ClimbAnchorsHaveLiveSurface { get; }
        public string Authority { get; }
        public bool IsValid => RouteCellsHaveHeadroom && ClimbAnchorsHaveLiveSurface &&
            WalkTransitions + ClimbTransitions > 0;
    }

    public sealed class Sv5Secret
    {
        internal Sv5Secret(string id, IEnumerable<Sv5MicroChunkCoordinate> chunks,
            Sv5SpecialWorldPoint breakable, IEnumerable<Sv5SpecialWorldPoint> clues)
        {
            Id = id; Chunks = new ReadOnlyCollection<Sv5MicroChunkCoordinate>((chunks ??
                Array.Empty<Sv5MicroChunkCoordinate>()).OrderBy(value => value).ToArray());
            BreakableAccess = breakable; Clues = new ReadOnlyCollection<Sv5SpecialWorldPoint>((clues ??
                Array.Empty<Sv5SpecialWorldPoint>()).OrderBy(value => value).ToArray());
        }
        public string Id { get; }
        public IReadOnlyList<Sv5MicroChunkCoordinate> Chunks { get; }
        public Sv5SpecialWorldPoint BreakableAccess { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> Clues { get; }
    }

    public sealed class Sv5StateGeometry
    {
        internal Sv5StateGeometry(string ownerId, string state, Sv5SpecialWorldPoint point,
            Sv5PatternBaseCell baseCell, string reason)
        { OwnerId = ownerId; State = state; Point = point; BaseCell = baseCell; Reason = reason; }
        public string OwnerId { get; }
        public string State { get; }
        public Sv5SpecialWorldPoint Point { get; }
        public Sv5PatternBaseCell BaseCell { get; }
        public string Reason { get; }
    }

    public sealed class Sv5DensityMeasurement
    {
        internal Sv5DensityMeasurement(string patchId, Sv5BiomeDensityProfileId profile,
            int solid, int air, int oneWay, int minimum, int maximum)
        { PatchId = patchId; Profile = profile; Solid = solid; Air = air; OneWay = oneWay; MinimumPermille = minimum; MaximumPermille = maximum; }
        public string PatchId { get; }
        public Sv5BiomeDensityProfileId Profile { get; }
        public int Solid { get; }
        public int Air { get; }
        public int OneWay { get; }
        public int Total => Solid + Air + OneWay;
        public int DensityPermille => Total == 0 ? 0 : (Solid * 1000) / Total;
        public int MinimumPermille { get; }
        public int MaximumPermille { get; }
        public bool IsWithinTarget => DensityPermille >= MinimumPermille && DensityPermille <= MaximumPermille;
    }

    public sealed class Sv5ClusterAssemblyPlan
    {
        internal Sv5ClusterAssemblyPlan(Sv5WorldDefinition definition, Sv5WorldGraphPlan graph,
            Sv5WorldBiomePlan biomePlan, Sv5SpecialReservationPlan specialPlan,
            IEnumerable<Sv5TerrainCell> cells, IEnumerable<Sv5Cluster> clusters,
            IEnumerable<Sv5Chunk> chunks, IEnumerable<Sv5PatternPlacement> patterns,
            IEnumerable<Sv5Overlay> overlays, IEnumerable<Sv5Port> ports,
            IEnumerable<Sv5Route> routes, IEnumerable<Sv5Secret> secrets,
            IEnumerable<Sv5StateGeometry> stateGeometry, IEnumerable<Sv5DensityMeasurement> densities,
            int terrainReservationGateCalls, int rejectedProtectedWrites)
        {
            Definition = definition; Graph = graph; BiomePlan = biomePlan; SpecialPlan = specialPlan;
            Cells = new ReadOnlyCollection<Sv5TerrainCell>((cells ?? Array.Empty<Sv5TerrainCell>()).ToArray());
            Clusters = new ReadOnlyCollection<Sv5Cluster>((clusters ?? Array.Empty<Sv5Cluster>()).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
            Chunks = new ReadOnlyCollection<Sv5Chunk>((chunks ?? Array.Empty<Sv5Chunk>()).OrderBy(value => value.Coordinate).ToArray());
            Patterns = new ReadOnlyCollection<Sv5PatternPlacement>((patterns ?? Array.Empty<Sv5PatternPlacement>()).OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
            Overlays = new ReadOnlyCollection<Sv5Overlay>((overlays ?? Array.Empty<Sv5Overlay>()).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
            Ports = new ReadOnlyCollection<Sv5Port>((ports ?? Array.Empty<Sv5Port>()).OrderBy(value => value.Id, StringComparer.Ordinal).ThenBy(value => value.Y).ToArray());
            Routes = new ReadOnlyCollection<Sv5Route>((routes ?? Array.Empty<Sv5Route>()).OrderBy(value => value.EdgeId, StringComparer.Ordinal).ToArray());
            Secrets = new ReadOnlyCollection<Sv5Secret>((secrets ?? Array.Empty<Sv5Secret>()).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
            StateGeometry = new ReadOnlyCollection<Sv5StateGeometry>((stateGeometry ?? Array.Empty<Sv5StateGeometry>()).OrderBy(value => value.OwnerId, StringComparer.Ordinal).ThenBy(value => value.State, StringComparer.Ordinal).ThenBy(value => value.Point.Y).ThenBy(value => value.Point.X).ToArray());
            Densities = new ReadOnlyCollection<Sv5DensityMeasurement>((densities ?? Array.Empty<Sv5DensityMeasurement>()).OrderBy(value => value.PatchId, StringComparer.Ordinal).ToArray());
            TerrainReservationGateCalls = terrainReservationGateCalls; RejectedProtectedWrites = rejectedProtectedWrites;
            CellDigest = Sv5WorldDefinition.Hash(string.Join("\n", Cells.Select(value => value.X.ToString(CultureInfo.InvariantCulture) + "," +
                value.Y.ToString(CultureInfo.InvariantCulture) + "," + value.BaseCell + "," + value.SourceKind + "," +
                value.SourceId + "," + value.PatchId + "," + value.PatternCandidateId)));
            Digest = Sv5WorldDefinition.Hash(string.Join("\n", new[] { "SV5_CLUSTER_ASSEMBLY_V2", definition.Digest,
                graph.Digest, biomePlan.Digest, specialPlan.Digest, string.Join(";", Clusters.Select(value => value.MaskDigest)),
                string.Join(";", Routes.Select(value => value.EdgeId + ":" + value.Cells.Count.ToString(CultureInfo.InvariantCulture))),
                string.Join(";", Densities.Select(value => value.PatchId + ":" + value.DensityPermille.ToString(CultureInfo.InvariantCulture))), CellDigest}));
        }
        public Sv5WorldDefinition Definition { get; }
        public Sv5WorldGraphPlan Graph { get; }
        public Sv5WorldBiomePlan BiomePlan { get; }
        public Sv5SpecialReservationPlan SpecialPlan { get; }
        public IReadOnlyList<Sv5TerrainCell> Cells { get; }
        public IReadOnlyList<Sv5Cluster> Clusters { get; }
        public IReadOnlyList<Sv5Chunk> Chunks { get; }
        public IReadOnlyList<Sv5PatternPlacement> Patterns { get; }
        public IReadOnlyList<Sv5Overlay> Overlays { get; }
        public IReadOnlyList<Sv5Port> Ports { get; }
        public IReadOnlyList<Sv5Route> Routes { get; }
        public IReadOnlyList<Sv5Secret> Secrets { get; }
        public IReadOnlyList<Sv5StateGeometry> StateGeometry { get; }
        public IReadOnlyList<Sv5DensityMeasurement> Densities { get; }
        public int TerrainReservationGateCalls { get; }
        public int RejectedProtectedWrites { get; }
        public string CellDigest { get; }
        public string Digest { get; }
        public bool Success => Cells.Count == Sv5WorldBiomePlanner.WorldWidthTiles * Sv5WorldBiomePlanner.WorldHeightTiles &&
            Chunks.Count == Sv5WorldBiomePlanner.MicroChunkCount && Clusters.All(value => value.MicroChunkCount >= 2 && value.MicroChunkCount <= 8) &&
            Densities.All(value => value.IsWithinTarget) && Routes.All(value => value.Evidence.IsValid) &&
            TerrainReservationGateCalls > 0;
        public Sv5TerrainCell GetCell(int x, int y) => Cells[(y * Sv5WorldBiomePlanner.WorldWidthTiles) + x];
    }

    /// <summary>Read-only exact-list handoff for SV5 baking. No terrain is regenerated here.</summary>
    public sealed class Sv5BakeSnapshot
    {
        internal Sv5BakeSnapshot(Sv5ClusterAssemblyPlan source)
        {
            SourcePlanDigest = source.Digest; SourceCellDigest = source.CellDigest;
            Cells = source.Cells; Chunks = source.Chunks; Overlays = source.Overlays;
        }
        public string SourcePlanDigest { get; }
        public string SourceCellDigest { get; }
        public IReadOnlyList<Sv5TerrainCell> Cells { get; }
        public IReadOnlyList<Sv5Chunk> Chunks { get; }
        public IReadOnlyList<Sv5Overlay> Overlays { get; }
        public bool IsExactWorld => Cells.Count == Sv5WorldBiomePlanner.WorldWidthTiles * Sv5WorldBiomePlanner.WorldHeightTiles;
    }

    public static class Sv5ClusterAssemblyPlanner
    {
        public const string RulesetVersion = "SV5_CLUSTERS_V1";
        public const int WorldWidthTiles = Sv5WorldBiomePlanner.WorldWidthTiles;
        public const int WorldHeightTiles = Sv5WorldBiomePlanner.WorldHeightTiles;
        public const int MicroChunkWidthTiles = Sv5WorldBiomePlanner.MicroChunkWidthTiles;
        public const int MicroChunkHeightTiles = Sv5WorldBiomePlanner.MicroChunkHeightTiles;

        public static Sv5ClusterAssemblyPlan Plan(Sv5WorldDefinition definition, WorldGenerationRngStreams rngStreams)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            Sv5WorldBiomePlan biome = Sv5WorldBiomePlanner.Plan(definition, rngStreams);
            return Plan(definition, biome.Graph, biome, Sv5SpecialReservationPlanner.Plan(biome, rngStreams), rngStreams);
        }

        public static Sv5BakeSnapshot CreateBakeSnapshot(Sv5ClusterAssemblyPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (!plan.Success) throw new ArgumentException("SV5 bake snapshot requires a successful assembly plan.", nameof(plan));
            return new Sv5BakeSnapshot(plan);
        }

        /// <summary>Explicit SV5 -> SV5 -> SV5 -> SV5 consumer used by SV5's future bake adapter.</summary>
        public static Sv5ClusterAssemblyPlan Plan(Sv5WorldDefinition definition, Sv5WorldGraphPlan graph,
            Sv5WorldBiomePlan biomePlan, Sv5SpecialReservationPlan specialPlan, WorldGenerationRngStreams rngStreams)
        {
            if (definition == null || graph == null || biomePlan == null || specialPlan == null || rngStreams == null)
                throw new ArgumentNullException("SV5 needs every verified upstream input.");
            if (!graph.Success || !biomePlan.Success || !specialPlan.Success || biomePlan.Definition.Digest != definition.Digest ||
                biomePlan.Graph.Digest != graph.Digest || specialPlan.BiomePlan.Digest != biomePlan.Digest)
                throw new ArgumentException("SV5 upstream contract linkage is invalid.");

            var ledger = new BuildLedger();
            Sv5TerrainCell[] cells = CreateFixedSpecialLayer(biomePlan, specialPlan);
            var occupied = new HashSet<Sv5MicroChunkCoordinate>();
            var clusters = new List<Sv5Cluster>
            {
                FindCluster("SV5_CLUSTER_SLOPE", "SLOPE", 2, 2, 3, 3, occupied, specialPlan),
                FindCluster("SV5_CLUSTER_CAVE", "CAVE", 2, 2, 22, 8, occupied, specialPlan),
                FindCluster("SV5_CLUSTER_CORRIDOR", "CORRIDOR", 3, 1, 4, 33, occupied, specialPlan),
                FindCluster("SV5_CLUSTER_HALFPIPE", "HALF_PIPE", 2, 1, 30, 42, occupied, specialPlan),
            };
            foreach (Sv5Cluster cluster in clusters) foreach (Sv5MicroChunkCoordinate coordinate in cluster.ChunkCoordinates) occupied.Add(coordinate);
            Sv5Cluster secretCluster = FindCluster("SV5_SECRET_CAVITY", "SECRET", 2, 1, 36, 18, occupied, specialPlan);
            foreach (Sv5MicroChunkCoordinate coordinate in secretCluster.ChunkCoordinates) occupied.Add(coordinate);
            List<Sv5MicroChunkCoordinate> inactiveChunks = ReserveInactiveSolidChunks(cells, biomePlan, specialPlan, occupied, ledger);
            foreach (Sv5Cluster cluster in clusters) ApplyShape(cells, specialPlan, cluster, ledger);
            var secret = ApplySecret(cells, specialPlan, secretCluster, ledger);
            var ports = specialPlan.Accesses.SelectMany(value => value.OpenCells.Select(point => new Sv5Port(value, point))).ToArray();
            List<Sv5Route> routes = ApplyGraphRoutes(cells, graph, specialPlan, ledger);
            secret = AttachReachableSecretClues(secret, routes);
            Sv5PatternPool500Snapshot pool = Sv5PatternPool500.BuildFinalPool();
            var patterns = new List<Sv5PatternPlacement>();
            int ordinal = 0;
            List<Sv5MicroChunkCoordinate> patternFields = FindPatternFields(cells, specialPlan,
                clusters.Sum(value => value.MicroChunkCount));
            int patternOffset = 0;
            foreach (Sv5Cluster cluster in clusters)
            {
                int fieldCount = cluster.MicroChunkCount;
                ApplyPatterns(cells, definition, specialPlan, pool, cluster,
                    patternFields.Skip(patternOffset).Take(fieldCount), patterns, ledger, ref ordinal);
                patternOffset += fieldCount;
            }
            var overlays = new List<Sv5Overlay>();
            AddClusterOverlays(clusters, overlays);
            AddSecretOverlays(secret, overlays);
            AddRouteOverlays(routes, overlays);
            FillDensityField(cells, definition, biomePlan, specialPlan, ledger);
            BalanceDensities(cells, biomePlan, specialPlan, ledger);
            List<Sv5DensityMeasurement> densities = MeasureDensities(cells, biomePlan);
            List<Sv5Chunk> chunks = BuildChunks(cells, biomePlan, specialPlan, clusters, secret, inactiveChunks);
            List<Sv5StateGeometry> state = BuildStateGeometry(specialPlan, secret);
            return new Sv5ClusterAssemblyPlan(definition, graph, biomePlan, specialPlan, cells, clusters, chunks,
                patterns, overlays, ports, routes, new[] { secret }, state, densities, ledger.GateCalls, ledger.RejectedWrites);
        }

        private static Sv5TerrainCell[] CreateFixedSpecialLayer(Sv5WorldBiomePlan biome, Sv5SpecialReservationPlan special)
        {
            var result = new Sv5TerrainCell[WorldWidthTiles * WorldHeightTiles];
            for (int y = 0; y < WorldHeightTiles; y++) for (int x = 0; x < WorldWidthTiles; x++)
            {
                string patch = biome.GetCell(x / MicroChunkWidthTiles, y / MicroChunkHeightTiles).PatchId;
                result[Index(x, y)] = new Sv5TerrainCell(x, y, Sv5PatternBaseCell.Air,
                    Sv5TerrainSourceKind.DensityField, "UNINITIALIZED", patch, string.Empty);
            }
            foreach (Sv5SpecialWorldCell cell in special.Cells)
                result[Index(cell.World.X, cell.World.Y)] = new Sv5TerrainCell(cell.World.X, cell.World.Y,
                    cell.BaseCell, Sv5TerrainSourceKind.SpecialReservation, cell.SiteId, cell.PatchId, string.Empty);
            return result;
        }

        private static void FillDensityField(Sv5TerrainCell[] cells, Sv5WorldDefinition definition,
            Sv5WorldBiomePlan biome, Sv5SpecialReservationPlan special, BuildLedger ledger)
        {
            for (int microY = 0; microY < Sv5WorldBiomePlanner.MicroChunkRows; microY++)
            for (int microX = 0; microX < Sv5WorldBiomePlanner.MicroChunkColumns; microX++)
            {
                Sv5WorldBiomePatch patch = biome.GetCell(microX, microY).Patch;
                Sv5BiomeDensityProfileDefinition profile = Sv5WorldBiomePlanner.ProfileCatalog[patch.DensityProfile];
                int target = (profile.TargetMinimumPermille + profile.TargetMaximumPermille) / 2;
                var points = new List<Sv5SpecialWorldPoint>(MicroChunkWidthTiles * MicroChunkHeightTiles);
                for (int y = microY * MicroChunkHeightTiles; y < (microY + 1) * MicroChunkHeightTiles; y++)
                for (int x = microX * MicroChunkWidthTiles; x < (microX + 1) * MicroChunkWidthTiles; x++) points.Add(new Sv5SpecialWorldPoint(x, y));
                points = points.Where(point => string.Equals(cells[Index(point.X, point.Y)].SourceId, "UNINITIALIZED", StringComparison.Ordinal)).ToList();
                WriteGeneral(cells, special, points, point => DensityValue(definition.Request.Seed, point.X, point.Y, target),
                    Sv5TerrainSourceKind.DensityField, patch.PatchId, string.Empty, ledger);
            }
        }

        private static List<Sv5MicroChunkCoordinate> ReserveInactiveSolidChunks(Sv5TerrainCell[] cells,
            Sv5WorldBiomePlan biome, Sv5SpecialReservationPlan special, ISet<Sv5MicroChunkCoordinate> occupied,
            BuildLedger ledger)
        {
            var result = new List<Sv5MicroChunkCoordinate>();
            foreach (Sv5WorldBiomePatch patch in biome.Patches.OrderBy(value => value.PatchId, StringComparer.Ordinal))
            {
                Sv5MicroChunkCoordinate coordinate = Enumerable.Range(patch.MinMicroY, patch.HeightMicroChunks)
                    .SelectMany(y => Enumerable.Range(patch.MinMicroX, patch.WidthMicroChunks)
                        .Select(x => new Sv5MicroChunkCoordinate(x, y)))
                    .FirstOrDefault(value => !occupied.Contains(value) && !ChunkHasSpecial(value.X, value.Y, special));
                if (occupied.Contains(coordinate) || ChunkHasSpecial(coordinate.X, coordinate.Y, special))
                    throw new InvalidOperationException("SV5 could not reserve an actual inactive-solid chunk for " + patch.PatchId + ".");
                occupied.Add(coordinate); result.Add(coordinate);
                var points = Enumerable.Range(coordinate.Y * MicroChunkHeightTiles, MicroChunkHeightTiles).SelectMany(y =>
                    Enumerable.Range(coordinate.X * MicroChunkWidthTiles, MicroChunkWidthTiles).Select(x => new Sv5SpecialWorldPoint(x, y)));
                WriteGeneral(cells, special, points, point => Sv5PatternBaseCell.Solid,
                    Sv5TerrainSourceKind.InactiveSolid, "SV5_INACTIVE_" + patch.PatchId, string.Empty, ledger);
            }
            return result;
        }

        private static Sv5PatternBaseCell DensityValue(ulong seed, int x, int y, int targetPermille)
        {
            unchecked
            {
                uint value = (uint)seed ^ ((uint)x * 747796405u) ^ ((uint)y * 2891336453u);
                value = (value ^ (value >> 16)) * 2246822519u;
                return (value % 1000u) < targetPermille ? Sv5PatternBaseCell.Solid : Sv5PatternBaseCell.Air;
            }
        }

        private static Sv5Cluster FindCluster(string id, string shape, int width, int height, int preferredX,
            int preferredY, ISet<Sv5MicroChunkCoordinate> occupied, Sv5SpecialReservationPlan special)
        {
            for (int offsetY = 0; offsetY < Sv5WorldBiomePlanner.MicroChunkRows; offsetY++)
            for (int offsetX = 0; offsetX < Sv5WorldBiomePlanner.MicroChunkColumns; offsetX++)
            {
                int x = (preferredX + offsetX) % Sv5WorldBiomePlanner.MicroChunkColumns;
                int y = (preferredY + offsetY) % Sv5WorldBiomePlanner.MicroChunkRows;
                if (x + width > Sv5WorldBiomePlanner.MicroChunkColumns || y + height > Sv5WorldBiomePlanner.MicroChunkRows) continue;
                bool clear = true;
                for (int cy = y; cy < y + height && clear; cy++) for (int cx = x; cx < x + width; cx++)
                {
                    if (occupied.Contains(new Sv5MicroChunkCoordinate(cx, cy)) || ChunkHasSpecial(cx, cy, special)) { clear = false; break; }
                }
                if (clear) return new Sv5Cluster(id, shape, x, y, width, height);
            }
            throw new InvalidOperationException("No non-reserved contiguous SV5 cluster footprint remains for " + id + ".");
        }

        private static bool ChunkHasSpecial(int microX, int microY, Sv5SpecialReservationPlan special)
        {
            for (int y = microY * MicroChunkHeightTiles; y < (microY + 1) * MicroChunkHeightTiles; y++)
            for (int x = microX * MicroChunkWidthTiles; x < (microX + 1) * MicroChunkWidthTiles; x++)
                if (special.TryGetCell(new Sv5SpecialWorldPoint(x, y), out _)) return true;
            return false;
        }

        private static List<Sv5MicroChunkCoordinate> FindPatternFields(Sv5TerrainCell[] cells,
            Sv5SpecialReservationPlan special, int requiredCount)
        {
            var fields = new List<Sv5MicroChunkCoordinate>();
            for (int y = 0; y < Sv5WorldBiomePlanner.MicroChunkRows && fields.Count < requiredCount; y++)
            for (int x = 0; x < Sv5WorldBiomePlanner.MicroChunkColumns && fields.Count < requiredCount; x++)
            {
                var coordinate = new Sv5MicroChunkCoordinate(x, y);
                bool free = !ChunkHasSpecial(x, y, special) && Enumerable.Range(y * MicroChunkHeightTiles, MicroChunkHeightTiles)
                    .SelectMany(tileY => Enumerable.Range(x * MicroChunkWidthTiles, MicroChunkWidthTiles).Select(tileX =>
                        cells[Index(tileX, tileY)])).All(cell => string.Equals(cell.SourceId, "UNINITIALIZED", StringComparison.Ordinal));
                if (free) fields.Add(coordinate);
            }
            if (fields.Count != requiredCount) throw new InvalidOperationException("SV5 could not allocate every route-free pattern field.");
            return fields;
        }

        private static void ApplyPatterns(Sv5TerrainCell[] cells, Sv5WorldDefinition definition,
            Sv5SpecialReservationPlan special, Sv5PatternPool500Snapshot pool, Sv5Cluster cluster,
            IEnumerable<Sv5MicroChunkCoordinate> fields, ICollection<Sv5PatternPlacement> output, BuildLedger ledger, ref int ordinal)
        {
            foreach (Sv5MicroChunkCoordinate chunk in fields ?? Array.Empty<Sv5MicroChunkCoordinate>())
            for (int slotY = 0; slotY < 2; slotY++) for (int slotX = 0; slotX < 3; slotX++)
            {
                int poolIndex = (int)((definition.Request.Seed + (ulong)(ordinal * 37 + slotX * 11 + slotY * 17)) % (ulong)pool.Candidates.Count);
                Sv5PatternPool500Entry entry = pool.Candidates[poolIndex];
                int originX = chunk.X * MicroChunkWidthTiles + slotX * Sv5PatternCatalog.Width;
                int originY = chunk.Y * MicroChunkHeightTiles + slotY * Sv5PatternCatalog.Height;
                var points = Enumerable.Range(0, Sv5PatternCatalog.Height).SelectMany(localY => Enumerable.Range(0,
                    Sv5PatternCatalog.Width).Select(localX => new Sv5SpecialWorldPoint(originX + localX, originY + localY))).ToArray();
                WriteGeneral(cells, special, points, point => entry.GetCell(point.X - originX, point.Y - originY),
                    Sv5TerrainSourceKind.Pattern, cluster.Id + "_PATTERN_FIELD", entry.CandidateId, ledger);
                output.Add(new Sv5PatternPlacement(cluster.Id, originX, originY, entry));
                ordinal++;
            }
        }

        private static void ApplyShape(Sv5TerrainCell[] cells, Sv5SpecialReservationPlan special,
            Sv5Cluster cluster, BuildLedger ledger)
        {
            int minX = cluster.MicroX * MicroChunkWidthTiles; int minY = cluster.MicroY * MicroChunkHeightTiles;
            int width = cluster.WidthMicroChunks * MicroChunkWidthTiles; int height = cluster.HeightMicroChunks * MicroChunkHeightTiles;
            var points = Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x =>
                new Sv5SpecialWorldPoint(minX + x, minY + y))).ToArray();
            Sv5TerrainSourceKind kind = cluster.Shape == "SLOPE" ? Sv5TerrainSourceKind.ClusterSlope :
                cluster.Shape == "CAVE" ? Sv5TerrainSourceKind.ClusterCave : cluster.Shape == "CORRIDOR" ?
                Sv5TerrainSourceKind.ClusterCorridor : Sv5TerrainSourceKind.ClusterHalfPipe;
            WriteGeneral(cells, special, points, point => ShapeValue(cluster.Shape, point.X - minX, point.Y - minY, width, height),
                kind, cluster.Id, string.Empty, ledger);
        }

        private static Sv5PatternBaseCell ShapeValue(string shape, int x, int y, int width, int height)
        {
            if (shape == "SLOPE")
            {
                int floor = 1 + (x * Math.Max(1, height - 5)) / Math.Max(1, width - 1);
                return y <= floor ? Sv5PatternBaseCell.Solid : Sv5PatternBaseCell.Air;
            }
            if (shape == "CAVE")
            {
                int dx = Math.Abs((x * 2) - (width - 1)); int dy = Math.Abs((y * 2) - (height - 1));
                if ((dx * dx) / Math.Max(1, width) + (dy * dy) / Math.Max(1, height) < Math.Max(width, height) / 2)
                    return y == 2 ? Sv5PatternBaseCell.OneWayPlatform : Sv5PatternBaseCell.Air;
                return Sv5PatternBaseCell.Solid;
            }
            if (shape == "CORRIDOR") return y == 1 ? Sv5PatternBaseCell.OneWayPlatform :
                (y >= 2 && y <= Math.Min(height - 2, 5) ? Sv5PatternBaseCell.Air : Sv5PatternBaseCell.Solid);
            int middle = Math.Abs(x - ((width - 1) / 2)); int floorHalfPipe = 1 + (middle * Math.Max(1, height - 5)) / Math.Max(1, width / 2);
            return y < floorHalfPipe ? Sv5PatternBaseCell.Solid : y == floorHalfPipe ? Sv5PatternBaseCell.OneWayPlatform : Sv5PatternBaseCell.Air;
        }

        private static Sv5Secret ApplySecret(Sv5TerrainCell[] cells, Sv5SpecialReservationPlan special,
            Sv5Cluster cluster, BuildLedger ledger)
        {
            int minX = cluster.MicroX * MicroChunkWidthTiles; int minY = cluster.MicroY * MicroChunkHeightTiles;
            int width = cluster.WidthMicroChunks * MicroChunkWidthTiles; int height = cluster.HeightMicroChunks * MicroChunkHeightTiles;
            var points = Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x =>
                new Sv5SpecialWorldPoint(minX + x, minY + y))).ToArray();
            Sv5SpecialWorldPoint access = new Sv5SpecialWorldPoint(minX, minY + height / 2);
            WriteGeneral(cells, special, points, point =>
            {
                int localX = point.X - minX; int localY = point.Y - minY;
                if (point.Equals(access)) return Sv5PatternBaseCell.Solid;
                return localX > 1 && localX < width - 2 && localY > 1 && localY < height - 2 ? Sv5PatternBaseCell.Air : Sv5PatternBaseCell.Solid;
            }, Sv5TerrainSourceKind.Secret, cluster.Id, string.Empty, ledger);
            return new Sv5Secret(cluster.Id, cluster.ChunkCoordinates, access, Array.Empty<Sv5SpecialWorldPoint>());
        }

        private static Sv5Secret AttachReachableSecretClues(Sv5Secret secret, IEnumerable<Sv5Route> routes)
        {
            Sv5SpecialWorldPoint[] clues = (routes ?? Array.Empty<Sv5Route>()).SelectMany(route => route.Cells)
                .Distinct().OrderBy(point => Math.Abs(point.X - secret.BreakableAccess.X) +
                    Math.Abs(point.Y - secret.BreakableAccess.Y)).ThenBy(point => point).Take(2).ToArray();
            if (clues.Length != 2) throw new InvalidOperationException("SV5 needs two ordinary route cells for secret discovery clues.");
            return new Sv5Secret(secret.Id, secret.Chunks, secret.BreakableAccess, clues);
        }

        private static void AddClusterOverlays(IEnumerable<Sv5Cluster> clusters, ICollection<Sv5Overlay> overlays)
        {
            foreach (Sv5Cluster cluster in clusters)
            {
                int minX = cluster.MicroX * MicroChunkWidthTiles; int minY = cluster.MicroY * MicroChunkHeightTiles;
                int width = cluster.WidthMicroChunks * MicroChunkWidthTiles; int height = cluster.HeightMicroChunks * MicroChunkHeightTiles;
                if (cluster.Shape == "CAVE") for (int y = 3; y < height - 2; y += 4)
                    overlays.Add(new Sv5Overlay(cluster.Id + "_LADDER_" + y, "LADDER", minX + width / 2,
                        minY + y, cluster.Id, "CharacterLiveClimbSurface.StaticSafe"));
                if (cluster.Shape == "HALF_PIPE")
                {
                    overlays.Add(new Sv5Overlay(cluster.Id + "_GRAB_LEFT", "GRAB_EDGE", minX + 1, minY + height - 3,
                        cluster.Id, "CharacterLiveGrabSurface.StaticSafe"));
                    overlays.Add(new Sv5Overlay(cluster.Id + "_GRAB_RIGHT", "GRAB_EDGE", minX + width - 2, minY + height - 3,
                        cluster.Id, "CharacterLiveGrabSurface.StaticSafe"));
                }
            }
        }

        private static void AddSecretOverlays(Sv5Secret secret, ICollection<Sv5Overlay> overlays)
        {
            overlays.Add(new Sv5Overlay(secret.Id + "_BREAKABLE", "BREAKABLE_ACCESS", secret.BreakableAccess.X,
                secret.BreakableAccess.Y, secret.Id, "SECRET_SEALED"));
            foreach (var clue in secret.Clues.Select((value, index) => new { value, index }))
                overlays.Add(new Sv5Overlay(secret.Id + "_CLUE_" + clue.index.ToString(CultureInfo.InvariantCulture), "SECRET_CLUE",
                    clue.value.X, clue.value.Y, secret.Id, "OBSERVE_FIRST"));
        }

        private static void AddRouteOverlays(IEnumerable<Sv5Route> routes, ICollection<Sv5Overlay> overlays)
        {
            foreach (Sv5Route route in routes) for (int index = 1; index < route.Cells.Count; index++)
            {
                Sv5SpecialWorldPoint previous = route.Cells[index - 1]; Sv5SpecialWorldPoint current = route.Cells[index];
                if (previous.X == current.X && previous.Y != current.Y)
                    overlays.Add(new Sv5Overlay(route.EdgeId + "_CLIMB_" + index.ToString(CultureInfo.InvariantCulture), "LADDER",
                        current.X, current.Y, route.EdgeId, "CharacterLiveClimbSurface.StaticSafe"));
            }
        }

        private static Sv5RouteEvidence ValidateRoute(Sv5TerrainCell[] cells, IReadOnlyList<Sv5SpecialWorldPoint> path)
        {
            int walk = 0; int climb = 0; int boundary = 0; bool headroom = path.Count > 1;
            for (int index = 0; index < path.Count; index++)
            {
                Sv5SpecialWorldPoint point = path[index];
                bool boundaryPort = point.Y + 1 >= WorldHeightTiles && (index == 0 || index == path.Count - 1);
                if (boundaryPort) boundary++;
                headroom &= cells[Index(point.X, point.Y)].BaseCell == Sv5PatternBaseCell.Air &&
                    (boundaryPort || (point.Y + 1 < WorldHeightTiles && cells[Index(point.X, point.Y + 1)].BaseCell == Sv5PatternBaseCell.Air));
                if (index == 0) continue;
                Sv5SpecialWorldPoint previous = path[index - 1];
                if (Math.Abs(point.X - previous.X) + Math.Abs(point.Y - previous.Y) != 1)
                    throw new InvalidOperationException("SV5 route is not cardinally contiguous.");
                if (point.Y == previous.Y) walk++; else climb++;
            }
            return new Sv5RouteEvidence(walk, climb, boundary, headroom, true);
        }

        private static List<Sv5Route> ApplyGraphRoutes(Sv5TerrainCell[] cells, Sv5WorldGraphPlan graph,
            Sv5SpecialReservationPlan special, BuildLedger ledger)
        {
            var routes = new List<Sv5Route>();
            foreach (Sv5WorldGraphEdge edge in graph.Edges.OrderBy(value => value.EdgeId, StringComparer.Ordinal))
            {
                Sv5SpecialAccess from = SelectAccess(special, edge.SourceNodeId, true);
                Sv5SpecialAccess to = SelectAccess(special, edge.TargetNodeId, false);
                Sv5SpecialWorldPoint start = ExteriorPoint(from, special); Sv5SpecialWorldPoint end = ExteriorPoint(to, special);
                List<Sv5SpecialWorldPoint> path = FindUnreservedPath(start, end, special, cells);
                if (path.Count == 0) throw new InvalidOperationException("SV5 could not make a protected-safe route for " + edge.EdgeId + ".");
                var clearance = path.Concat(path.Select(point => new Sv5SpecialWorldPoint(point.X, point.Y + 1)))
                    .Where(InBounds).Distinct().ToArray();
                WriteGeneral(cells, special, clearance, point => Sv5PatternBaseCell.Air, Sv5TerrainSourceKind.Route,
                    edge.EdgeId, string.Empty, ledger);
                var pathSet = new HashSet<Sv5SpecialWorldPoint>(path);
                var supports = path.Select(point => new Sv5SpecialWorldPoint(point.X, point.Y - 1)).Where(point =>
                    InBounds(point) && !pathSet.Contains(point) && string.Equals(cells[Index(point.X, point.Y)].SourceId,
                        "UNINITIALIZED", StringComparison.Ordinal)).Distinct().ToArray();
                WriteGeneral(cells, special, supports, point => Sv5PatternBaseCell.Solid, Sv5TerrainSourceKind.Route,
                    edge.EdgeId + "_SUPPORT", string.Empty, ledger);
                routes.Add(new Sv5Route(edge, from.Id, to.Id, path, ValidateRoute(cells, path)));
            }
            return routes;
        }

        private static Sv5SpecialAccess SelectAccess(Sv5SpecialReservationPlan special, string nodeId, bool outgoing)
        {
            Sv5SpecialAccess value = special.Accesses.Where(access => AccessOwnsNode(access, nodeId) &&
                (outgoing ? access.Flow == Sv5SpecialAccessFlow.Out || access.Flow == Sv5SpecialAccessFlow.Both :
                access.Flow == Sv5SpecialAccessFlow.In || access.Flow == Sv5SpecialAccessFlow.Both)).OrderBy(access => access.Id,
                StringComparer.Ordinal).FirstOrDefault();
            if (value == null) throw new InvalidOperationException("SV5 has no compatible port for graph node " + nodeId + ".");
            return value;
        }

        private static bool AccessOwnsNode(Sv5SpecialAccess access, string nodeId) => (access.SourceNodeId ?? string.Empty)
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Any(value => string.Equals(value, nodeId, StringComparison.Ordinal));

        private static Sv5SpecialWorldPoint ExteriorPoint(Sv5SpecialAccess access, Sv5SpecialReservationPlan special)
        {
            Sv5SpecialWorldPoint point = access.OpenCells.OrderBy(value => value).ElementAt(access.OpenCells.Count / 2);
            int dx = 0; int dy = 0;
            switch (access.Side)
            {
                case Sv5WorldGraphDirection.Left: dx = -1; break;
                case Sv5WorldGraphDirection.Right: dx = 1; break;
                case Sv5WorldGraphDirection.Up: dy = 1; break;
                default: dy = -1; break;
            }
            for (int distance = 1; distance <= Math.Max(WorldWidthTiles, WorldHeightTiles); distance++)
            {
                var exterior = new Sv5SpecialWorldPoint(point.X + (dx * distance), point.Y + (dy * distance));
                if (!InBounds(exterior)) break;
                if (!special.TryGetCell(exterior, out _)) return exterior;
            }
            throw new InvalidOperationException("SV5 port exterior does not leave its protected site: " + access.Id + ".");
        }

        private static List<Sv5SpecialWorldPoint> FindUnreservedPath(Sv5SpecialWorldPoint start, Sv5SpecialWorldPoint end,
            Sv5SpecialReservationPlan special, Sv5TerrainCell[] cells)
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
                    int next = Index(nx, ny); if (parent[next] != -2 || special.TryGetCell(new Sv5SpecialWorldPoint(nx, ny), out _)) continue;
                    if (ny + 1 < WorldHeightTiles && special.TryGetCell(new Sv5SpecialWorldPoint(nx, ny + 1), out _)) continue;
                    bool endpoint = next == endIndex;
                    bool uninitialized = string.Equals(cells[next].SourceId, "UNINITIALIZED", StringComparison.Ordinal);
                    bool clusterBoundary = cells[next].SourceKind == Sv5TerrainSourceKind.ClusterSlope ||
                        cells[next].SourceKind == Sv5TerrainSourceKind.ClusterCave ||
                        cells[next].SourceKind == Sv5TerrainSourceKind.ClusterCorridor ||
                        cells[next].SourceKind == Sv5TerrainSourceKind.ClusterHalfPipe;
                    bool verifiedRouteReuse = cells[next].SourceKind == Sv5TerrainSourceKind.Route;
                    if (!endpoint && !uninitialized && !clusterBoundary && !verifiedRouteReuse) continue;
                    parent[next] = index; queue.Enqueue(next);
                }
            }
            if (parent[endIndex] == -2) return new List<Sv5SpecialWorldPoint>();
            var result = new List<Sv5SpecialWorldPoint>();
            for (int cursor = endIndex; cursor != -1; cursor = parent[cursor]) result.Add(new Sv5SpecialWorldPoint(cursor % WorldWidthTiles, cursor / WorldWidthTiles));
            result.Reverse(); return result;
        }

        private static void BalanceDensities(Sv5TerrainCell[] cells, Sv5WorldBiomePlan biome,
            Sv5SpecialReservationPlan special, BuildLedger ledger)
        {
            foreach (Sv5WorldBiomePatch patch in biome.Patches)
            {
                Sv5BiomeDensityProfileDefinition profile = Sv5WorldBiomePlanner.ProfileCatalog[patch.DensityProfile];
                int desiredSolid = (((profile.TargetMinimumPermille + profile.TargetMaximumPermille) / 2) *
                    ((patch.MaxTileX - patch.MinTileX + 1) * (patch.MaxTileY - patch.MinTileY + 1))) / 1000;
                List<Sv5SpecialWorldPoint> candidates = Enumerable.Range(patch.MinTileY, patch.MaxTileY - patch.MinTileY + 1)
                    .SelectMany(y => Enumerable.Range(patch.MinTileX, patch.MaxTileX - patch.MinTileX + 1).Select(x => new Sv5SpecialWorldPoint(x, y)))
                    .Where(point => cells[Index(point.X, point.Y)].SourceKind == Sv5TerrainSourceKind.DensityField).ToList();
                int currentSolid = Enumerable.Range(patch.MinTileY, patch.MaxTileY - patch.MinTileY + 1).SelectMany(y =>
                    Enumerable.Range(patch.MinTileX, patch.MaxTileX - patch.MinTileX + 1).Select(x => cells[Index(x, y)])).Count(cell => cell.BaseCell == Sv5PatternBaseCell.Solid);
                Sv5PatternBaseCell replacement = currentSolid < desiredSolid ? Sv5PatternBaseCell.Solid : Sv5PatternBaseCell.Air;
                int needed = Math.Abs(desiredSolid - currentSolid);
                IEnumerable<Sv5SpecialWorldPoint> selected = candidates.Where(point => cells[Index(point.X, point.Y)].BaseCell != replacement)
                    .OrderBy(point => point.Y).ThenBy(point => point.X).Take(needed).ToArray();
                WriteGeneral(cells, special, selected, point => replacement, Sv5TerrainSourceKind.DensityField,
                    patch.PatchId + "_BALANCED", string.Empty, ledger);
            }
        }

        private static List<Sv5DensityMeasurement> MeasureDensities(Sv5TerrainCell[] cells, Sv5WorldBiomePlan biome)
        {
            var output = new List<Sv5DensityMeasurement>();
            foreach (Sv5WorldBiomePatch patch in biome.Patches)
            {
                var patchCells = Enumerable.Range(patch.MinTileY, patch.MaxTileY - patch.MinTileY + 1).SelectMany(y =>
                    Enumerable.Range(patch.MinTileX, patch.MaxTileX - patch.MinTileX + 1).Select(x => cells[Index(x, y)])).ToArray();
                Sv5BiomeDensityProfileDefinition profile = Sv5WorldBiomePlanner.ProfileCatalog[patch.DensityProfile];
                output.Add(new Sv5DensityMeasurement(patch.PatchId, patch.DensityProfile,
                    patchCells.Count(value => value.BaseCell == Sv5PatternBaseCell.Solid),
                    patchCells.Count(value => value.BaseCell == Sv5PatternBaseCell.Air),
                    patchCells.Count(value => value.BaseCell == Sv5PatternBaseCell.OneWayPlatform),
                    profile.TargetMinimumPermille, profile.TargetMaximumPermille));
            }
            return output;
        }

        private static List<Sv5Chunk> BuildChunks(Sv5TerrainCell[] cells, Sv5WorldBiomePlan biome,
            Sv5SpecialReservationPlan special, IEnumerable<Sv5Cluster> clusters, Sv5Secret secret,
            IEnumerable<Sv5MicroChunkCoordinate> inactiveChunks)
        {
            var clusterByChunk = clusters.SelectMany(cluster => cluster.ChunkCoordinates.Select(point => new { point, cluster.Id }))
                .ToDictionary(value => value.point, value => value.Id);
            var secretChunks = new HashSet<Sv5MicroChunkCoordinate>(secret.Chunks);
            var inactive = new HashSet<Sv5MicroChunkCoordinate>(inactiveChunks ?? Array.Empty<Sv5MicroChunkCoordinate>());
            var result = new List<Sv5Chunk>(Sv5WorldBiomePlanner.MicroChunkCount);
            for (int y = 0; y < Sv5WorldBiomePlanner.MicroChunkRows; y++) for (int x = 0; x < Sv5WorldBiomePlanner.MicroChunkColumns; x++)
            {
                var coordinate = new Sv5MicroChunkCoordinate(x, y); int protectedCells = 0;
                for (int tileY = y * MicroChunkHeightTiles; tileY < (y + 1) * MicroChunkHeightTiles; tileY++)
                for (int tileX = x * MicroChunkWidthTiles; tileX < (x + 1) * MicroChunkWidthTiles; tileX++) if (special.TryGetCell(new Sv5SpecialWorldPoint(tileX, tileY), out _)) protectedCells++;
                string clusterId; Sv5ChunkState state = secretChunks.Contains(coordinate) ? Sv5ChunkState.Secret :
                    clusterByChunk.TryGetValue(coordinate, out clusterId) ? Sv5ChunkState.Active : protectedCells > 0 ?
                    Sv5ChunkState.SpecialReserved : inactive.Contains(coordinate) ? Sv5ChunkState.InactiveSolid : Sv5ChunkState.Active;
                if (!clusterByChunk.TryGetValue(coordinate, out clusterId)) clusterId = string.Empty;
                string portType = state == Sv5ChunkState.Active ? "TYPE0_PUBLIC" : state == Sv5ChunkState.Secret ?
                    "TYPE0_SECRET_ONE_ENTRY" : state == Sv5ChunkState.SpecialReserved ? "SV5_RESERVED_ACCESS" : "INACTIVE_NO_PORT";
                result.Add(new Sv5Chunk(coordinate, biome.GetCell(x, y).PatchId, state, clusterId, protectedCells, portType));
            }
            return result;
        }

        private static List<Sv5StateGeometry> BuildStateGeometry(Sv5SpecialReservationPlan special, Sv5Secret secret)
        {
            var output = special.StateGeometry.Select(value => new Sv5StateGeometry(value.SiteId, value.State,
                value.World, value.BaseCell, value.Reason)).ToList();
            output.Add(new Sv5StateGeometry(secret.Id, "SEALED", secret.BreakableAccess, Sv5PatternBaseCell.Solid,
                "Breakable access is closed until the optional secret is discovered."));
            output.Add(new Sv5StateGeometry(secret.Id, "OPEN", secret.BreakableAccess, Sv5PatternBaseCell.Air,
                "Breakable access opens into the static secret cavity."));
            return output;
        }

        private static void WriteGeneral(Sv5TerrainCell[] cells, Sv5SpecialReservationPlan special,
            IEnumerable<Sv5SpecialWorldPoint> proposed, Func<Sv5SpecialWorldPoint, Sv5PatternBaseCell> value,
            Sv5TerrainSourceKind sourceKind, string sourceId, string patternCandidateId, BuildLedger ledger)
        {
            Sv5SpecialWorldPoint[] points = (proposed ?? Array.Empty<Sv5SpecialWorldPoint>()).Where(InBounds).Distinct().ToArray();
            if (points.Length == 0) return;
            ledger.GateCalls++;
            Sv5SpecialTerrainReservationDecision decision = special.EvaluateTerrainCells(points);
            if (decision.IsAllowed) { foreach (Sv5SpecialWorldPoint point in points) Set(cells, point, value(point), sourceKind, sourceId, patternCandidateId); return; }
            foreach (Sv5SpecialWorldPoint point in points)
            {
                ledger.GateCalls++;
                if (special.EvaluateTerrainCells(new[] { point }).IsAllowed) Set(cells, point, value(point), sourceKind, sourceId, patternCandidateId);
                else ledger.RejectedWrites++;
            }
        }

        private static void Set(Sv5TerrainCell[] cells, Sv5SpecialWorldPoint point, Sv5PatternBaseCell value,
            Sv5TerrainSourceKind sourceKind, string sourceId, string patternCandidateId)
        {
            Sv5TerrainCell old = cells[Index(point.X, point.Y)];
            cells[Index(point.X, point.Y)] = new Sv5TerrainCell(point.X, point.Y, value, sourceKind, sourceId, old.PatchId, patternCandidateId);
        }

        private static int Index(int x, int y) => (y * WorldWidthTiles) + x;
        private static bool InBounds(Sv5SpecialWorldPoint point) => point.X >= 0 && point.X < WorldWidthTiles && point.Y >= 0 && point.Y < WorldHeightTiles;
        private sealed class BuildLedger { public int GateCalls; public int RejectedWrites; }
    }

    public static class Sv5ClusterAssemblyExport
    {
        public static string ManifestJson(Sv5ClusterAssemblyPlan plan) => "{\n" +
            "  \"format\": \"SV5_CLUSTER_ASSEMBLY_V1\",\n" +
            "  \"definition_digest\": \"" + plan.Definition.Digest + "\",\n" +
            "  \"sv5_graph_digest\": \"" + plan.Graph.Digest + "\",\n" +
            "  \"sv5_biome_digest\": \"" + plan.BiomePlan.Digest + "\",\n" +
            "  \"sv5_special_digest\": \"" + plan.SpecialPlan.Digest + "\",\n" +
            "  \"cells\": " + plan.Cells.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"cell_digest\": \"" + plan.CellDigest + "\",\n" +
            "  \"chunks\": " + plan.Chunks.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"terrain_reservation_gate_calls\": " + plan.TerrainReservationGateCalls.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"rejected_protected_writes\": " + plan.RejectedProtectedWrites.ToString(CultureInfo.InvariantCulture) + ",\n" +
            "  \"sv5_bake\": \"NOT_STARTED\",\n" +
            "  \"digest\": \"" + plan.Digest + "\"\n}\n";

        public static string ClustersCsv(Sv5ClusterAssemblyPlan plan) => Lines("cluster_id,shape,micro_x,micro_y,width_microchunks,height_microchunks,chunk_count,mask_digest",
            plan.Clusters.Select(value => Row(value.Id, value.Shape, value.MicroX, value.MicroY, value.WidthMicroChunks, value.HeightMicroChunks, value.MicroChunkCount, value.MaskDigest)));
        public static string ChunksCsv(Sv5ClusterAssemblyPlan plan) => Lines("micro_x,micro_y,patch_id,state,cluster_id,protected_cell_count,port_type",
            plan.Chunks.Select(value => Row(value.Coordinate.X, value.Coordinate.Y, value.PatchId, value.State, value.ClusterId, value.ProtectedCellCount, value.PortType)));
        public static string CellsCsv(Sv5ClusterAssemblyPlan plan) => Lines("world_x,world_y,base,source_kind,source_id,patch_id,pattern_candidate_id",
            plan.Cells.Select(value => Row(value.X, value.Y, Token(value.BaseCell), value.SourceKind, value.SourceId, value.PatchId, value.PatternCandidateId)));
        public static string PatternsCsv(Sv5ClusterAssemblyPlan plan) => Lines("cluster_id,world_x,world_y,pool_index,candidate_id,role,source_id,base_cells16",
            plan.Patterns.Select(value => Row(value.ClusterId, value.X, value.Y, value.PoolIndex, value.CandidateId, value.Role, value.SourceId, value.BaseCells16)));
        public static string OverlaysCsv(Sv5ClusterAssemblyPlan plan) => Lines("overlay_id,kind,world_x,world_y,owner_id,condition",
            plan.Overlays.Select(value => Row(value.Id, value.Kind, value.X, value.Y, value.OwnerId, value.Condition)));
        public static string PortsCsv(Sv5ClusterAssemblyPlan plan) => Lines("port_id,site_id,node_id,side,flow,required,condition,world_x,world_y",
            plan.Ports.Select(value => Row(value.Id, value.SiteId, value.NodeId, value.Side, value.Flow, value.Required, value.Condition, value.X, value.Y)));
        public static string RoutesCsv(Sv5ClusterAssemblyPlan plan) => Lines("edge_id,source_node_id,target_node_id,condition,from_port_id,to_port_id,cell_ordinal,world_x,world_y,walk_transitions,climb_transitions,boundary_port_transitions,headroom_verified,live_climb_anchor_verified,authority",
            plan.Routes.SelectMany(route => route.Cells.Select((cell, index) => Row(route.EdgeId, route.SourceNodeId, route.TargetNodeId,
                route.Condition, route.FromPortId, route.ToPortId, index, cell.X, cell.Y, route.Evidence.WalkTransitions,
                route.Evidence.ClimbTransitions, route.Evidence.BoundaryPortTransitions, route.Evidence.RouteCellsHaveHeadroom,
                route.Evidence.ClimbAnchorsHaveLiveSurface, route.Evidence.Authority))));
        public static string SecretsCsv(Sv5ClusterAssemblyPlan plan) => Lines("secret_id,chunk_coordinates,breakable_x,breakable_y,clue_coordinates,entry_contract",
            plan.Secrets.Select(value => Row(value.Id, string.Join("|", value.Chunks), value.BreakableAccess.X, value.BreakableAccess.Y,
                string.Join("|", value.Clues), "ONE_DECLARED_BREAKABLE_ENTRY")));
        public static string StateGeometryCsv(Sv5ClusterAssemblyPlan plan) => Lines("owner_id,state,world_x,world_y,base,reason",
            plan.StateGeometry.Select(value => Row(value.OwnerId, value.State, value.Point.X, value.Point.Y, Token(value.BaseCell), value.Reason)));
        public static string DensityCsv(Sv5ClusterAssemblyPlan plan) => Lines("patch_id,profile,solid,air,one_way,total,density_permille,target_min_permille,target_max_permille,within_target",
            plan.Densities.Select(value => Row(value.PatchId, value.Profile, value.Solid, value.Air, value.OneWay, value.Total,
                value.DensityPermille, value.MinimumPermille, value.MaximumPermille, value.IsWithinTarget)));
        public static string ShapeSummaryCsv(Sv5ClusterAssemblyPlan plan) => Lines("kind,count,detail",
            new[] { Row("CLUSTER", plan.Clusters.Count, "2_TO_8_CONNECTED_MICROCHUNKS"), Row("SECRET", plan.Secrets.Count,
                "1_TO_6_MICROCHUNKS_WITH_TWO_CLUES"), Row("ROUTE", plan.Routes.Count, "GRAPH_EDGE_STATIC_CELL_SPINES"),
                Row("OVERLAY", plan.Overlays.Count, "SEPARATE_FROM_BASE"), Row("RESERVATION_GATE", plan.TerrainReservationGateCalls,
                "SV5_EVALUATE_TERRAIN_CELLS") });
        public static string ScopeDensityCsv(Sv5ClusterAssemblyPlan plan) => Lines("scope_kind,scope_id,solid,air,one_way,total,density_permille,policy",
            new[] { ScopeRow("WORLD", "SV5", plan.Cells, "S_ALL_BASE_CELLS") }.Concat(
                plan.Chunks.Select(chunk => ScopeRow("CHUNK_STATE", chunk.State + ":" + chunk.Coordinate, CellsForChunk(plan, chunk.Coordinate),
                    chunk.State == Sv5ChunkState.InactiveSolid ? "ALL_SOLID_REQUIRED" : "ACTUAL_S_A_O"))).Concat(
                plan.Clusters.Select(cluster => ScopeRow("CLUSTER", cluster.Id, plan.Cells.Where(cell => cluster.ChunkCoordinates.Contains(
                    new Sv5MicroChunkCoordinate(cell.X / Sv5WorldBiomePlanner.MicroChunkWidthTiles, cell.Y / Sv5WorldBiomePlanner.MicroChunkHeightTiles))), "ACTUAL_S_A_O"))));
        public static string AssemblySummaryCsv(Sv5ClusterAssemblyPlan plan) => Lines("metric,value,policy",
            new[]
            {
                Row("WORLD_BASE_CELL_COUNT", plan.Cells.Count, "624x416"),
                Row("CHUNK_COUNT", plan.Chunks.Count, "52x52"),
                Row("ACTIVE_CHUNKS", plan.Chunks.Count(value => value.State == Sv5ChunkState.Active), "PUBLIC_OR_GENERAL"),
                Row("SECRET_CHUNKS", plan.Chunks.Count(value => value.State == Sv5ChunkState.Secret), "SEALED_SECRET_REGION"),
                Row("INACTIVE_SOLID_CHUNKS", plan.Chunks.Count(value => value.State == Sv5ChunkState.InactiveSolid), "ALL_96_BASE_SOLID"),
                Row("SPECIAL_RESERVED_CHUNKS", plan.Chunks.Count(value => value.State == Sv5ChunkState.SpecialReserved), "SV5_PROTECTED"),
                Row("TYPE0_PUBLIC_CHUNKS", plan.Chunks.Count(value => value.PortType == "TYPE0_PUBLIC"), "PUBLIC_ONLY"),
                Row("TYPE0_SECRET_CHUNKS", plan.Chunks.Count(value => value.PortType == "TYPE0_SECRET_ONE_ENTRY"), "NOT_PUBLIC"),
                Row("ROUTE_COUNT", plan.Routes.Count, "SV5_DIRECTED_EDGES"),
                Row("PATTERN_COUNT", plan.Patterns.Count, "3x2_PER_CLUSTER_CHUNK"),
            });

        private static string ScopeRow(string kind, string id, IEnumerable<Sv5TerrainCell> cells, string policy)
        {
            Sv5TerrainCell[] values = (cells ?? Array.Empty<Sv5TerrainCell>()).ToArray(); int solid = values.Count(value => value.BaseCell == Sv5PatternBaseCell.Solid);
            int air = values.Count(value => value.BaseCell == Sv5PatternBaseCell.Air); int oneWay = values.Count(value => value.BaseCell == Sv5PatternBaseCell.OneWayPlatform);
            return Row(kind, id, solid, air, oneWay, values.Length, values.Length == 0 ? 0 : (solid * 1000) / values.Length, policy);
        }
        private static IEnumerable<Sv5TerrainCell> CellsForChunk(Sv5ClusterAssemblyPlan plan, Sv5MicroChunkCoordinate coordinate) => plan.Cells.Where(value =>
            value.X / Sv5WorldBiomePlanner.MicroChunkWidthTiles == coordinate.X && value.Y / Sv5WorldBiomePlanner.MicroChunkHeightTiles == coordinate.Y);

        private static string Lines(string header, IEnumerable<string> rows) => header + "\n" + string.Join("\n", rows ?? Array.Empty<string>()) + "\n";
        private static string Row(params object[] values) => string.Join(",", (values ?? Array.Empty<object>()).Select(value => "\"" +
            (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Replace("\"", "\"\"") + "\""));
        private static string Token(Sv5PatternBaseCell cell) => cell == Sv5PatternBaseCell.Air ? "A" : cell == Sv5PatternBaseCell.Solid ? "S" : "O";
    }
}
