using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.Biomes
{
    public enum RmapBiomeDensityProfileId { Open, Balanced, Dense }

    /// <summary>Planning ownership is deliberately distinct from baked collision
    /// or traversal. RMAP15 owns choosing any fixed special footprint.</summary>
    public enum RmapBiomeOwnershipState { Active, Secret, InactiveSolid, SpecialReserved }

    internal static class RmapWorldBiomeIdentity
    {
        public static string Hash(params string[] values) => RmapWorldDefinition.Hash(string.Join("\n", values));

        public static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable value is required.", name);
            return value.Trim();
        }
    }

    public sealed class RmapBiomeDensityProfileDefinition
    {
        internal RmapBiomeDensityProfileDefinition(
            RmapBiomeDensityProfileId id,
            int targetMinimumPermille,
            int targetMaximumPermille,
            string purpose)
        {
            if (targetMinimumPermille < 0 || targetMaximumPermille < targetMinimumPermille ||
                targetMaximumPermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(targetMinimumPermille));
            Id = id;
            TargetMinimumPermille = targetMinimumPermille;
            TargetMaximumPermille = targetMaximumPermille;
            Purpose = RmapWorldBiomeIdentity.Require(purpose, nameof(purpose));
        }

        public RmapBiomeDensityProfileId Id { get; }
        public int TargetMinimumPermille { get; }
        public int TargetMaximumPermille { get; }
        public string Purpose { get; }
    }

    public sealed class RmapWorldBiomeCell : IComparable<RmapWorldBiomeCell>
    {
        internal RmapWorldBiomeCell(int x, int y, RmapWorldBiomePatch patch)
        {
            if (x < 0 || x >= RmapWorldBiomePlanner.MicroChunkColumns ||
                y < 0 || y >= RmapWorldBiomePlanner.MicroChunkRows)
                throw new ArgumentOutOfRangeException(nameof(x));
            Patch = patch ?? throw new ArgumentNullException(nameof(patch));
            X = x;
            Y = y;
            Index = (y * RmapWorldBiomePlanner.MicroChunkColumns) + x;
        }

        public int X { get; }
        public int Y { get; }
        public int Index { get; }
        public RmapWorldBiomePatch Patch { get; }
        public string PatchId => Patch.PatchId;
        public MoonpalaceBiomeId Biome => Patch.Biome;
        public RmapBiomeOwnershipState OwnershipState => Patch.OwnershipState;
        public int CompareTo(RmapWorldBiomeCell other) => other == null ? 1 : Index.CompareTo(other.Index);
    }

    public sealed class RmapWorldBiomePatch : IComparable<RmapWorldBiomePatch>
    {
        internal RmapWorldBiomePatch(
            string patchId,
            MoonpalaceBiomeId biome,
            int minMicroX,
            int minMicroY,
            int widthMicroChunks,
            int heightMicroChunks,
            RmapBiomeDensityProfileId densityProfile,
            int openWeight,
            int balancedWeight,
            int denseWeight)
        {
            if (!biome.IsDefined) throw new ArgumentException("Biome must be defined.", nameof(biome));
            if (minMicroX < 0 || minMicroY < 0 || widthMicroChunks < 1 || heightMicroChunks < 1 ||
                minMicroX + widthMicroChunks > RmapWorldBiomePlanner.MicroChunkColumns ||
                minMicroY + heightMicroChunks > RmapWorldBiomePlanner.MicroChunkRows)
                throw new ArgumentOutOfRangeException(nameof(minMicroX));
            if (openWeight <= 0 || balancedWeight <= 0 || denseWeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(openWeight));

            PatchId = RmapWorldBiomeIdentity.Require(patchId, nameof(patchId));
            Biome = biome;
            MinMicroX = minMicroX;
            MinMicroY = minMicroY;
            WidthMicroChunks = widthMicroChunks;
            HeightMicroChunks = heightMicroChunks;
            DensityProfile = densityProfile;
            OpenWeight = openWeight;
            BalancedWeight = balancedWeight;
            DenseWeight = denseWeight;
        }

        public string PatchId { get; }
        public MoonpalaceBiomeId Biome { get; }
        public string BiomeKey => Biome.CanonicalId;
        public string BiomeDisplayName => Biome.DisplayName;
        public int MinMicroX { get; }
        public int MinMicroY { get; }
        public int WidthMicroChunks { get; }
        public int HeightMicroChunks { get; }
        public int MaxMicroX => MinMicroX + WidthMicroChunks - 1;
        public int MaxMicroY => MinMicroY + HeightMicroChunks - 1;
        public int AreaMicroChunks => WidthMicroChunks * HeightMicroChunks;
        public int MinTileX => MinMicroX * RmapWorldBiomePlanner.MicroChunkWidthTiles;
        public int MinTileY => MinMicroY * RmapWorldBiomePlanner.MicroChunkHeightTiles;
        public int MaxTileX => ((MaxMicroX + 1) * RmapWorldBiomePlanner.MicroChunkWidthTiles) - 1;
        public int MaxTileY => ((MaxMicroY + 1) * RmapWorldBiomePlanner.MicroChunkHeightTiles) - 1;
        public RmapBiomeDensityProfileId DensityProfile { get; }
        public int OpenWeight { get; }
        public int BalancedWeight { get; }
        public int DenseWeight { get; }
        public RmapBiomeOwnershipState OwnershipState => RmapBiomeOwnershipState.Active;
        public string DensityMetricStatus => "PENDING_GEOMETRY";
        public string DensityMetricScopeId => "RMAP14_PATCH_SCOPE|" + PatchId;
        public int CompareTo(RmapWorldBiomePatch other) => other == null ? 1 :
            string.Compare(PatchId, other.PatchId, StringComparison.Ordinal);
    }

    public sealed class RmapWorldBiomeBoundary : IComparable<RmapWorldBiomeBoundary>
    {
        internal RmapWorldBiomeBoundary(
            RmapWorldBiomeCell source,
            RmapWorldBiomeCell target,
            RmapWorldGraphDirection direction,
            RmapBoundaryAuthoringSource sourceContract)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            SourceContract = sourceContract ?? throw new ArgumentNullException(nameof(sourceContract));
            if (Source.PatchId == Target.PatchId)
                throw new ArgumentException("A boundary requires different patches.", nameof(target));
            Direction = direction;
            Pair = new MoonpalaceBiomePair(Source.Biome, Target.Biome);
            if (Pair != SourceContract.Pair)
                throw new ArgumentException("Boundary source must own the canonical pair.", nameof(sourceContract));
            Orientation = direction == RmapWorldGraphDirection.Left || direction == RmapWorldGraphDirection.Right
                ? MoonpalaceBoundaryOrientation.Horizontal : MoonpalaceBoundaryOrientation.Vertical;
            if (!SourceContract.PairDefinition.Supports(Orientation))
                throw new ArgumentException("Boundary direction is not supported by the existing pair contract.");
            BoundaryId = RmapWorldBiomeIdentity.Hash("RMAP14_BOUNDARY_V1", Source.PatchId, Target.PatchId,
                Source.X.ToString(CultureInfo.InvariantCulture), Source.Y.ToString(CultureInfo.InvariantCulture),
                direction.ToString(), SourceContract.PairRuleId);
        }

        public string BoundaryId { get; }
        public RmapWorldBiomeCell Source { get; }
        public RmapWorldBiomeCell Target { get; }
        public RmapWorldGraphDirection Direction { get; }
        public MoonpalaceBiomePair Pair { get; }
        public MoonpalaceBoundaryOrientation Orientation { get; }
        public RmapBoundaryAuthoringSource SourceContract { get; }
        public string EdgeSignatureId => Orientation == MoonpalaceBoundaryOrientation.Horizontal
            ? SourceContract.HorizontalEdgeSignatureId : SourceContract.VerticalEdgeSignatureId;
        public int CompareTo(RmapWorldBiomeBoundary other) => other == null ? 1 :
            string.Compare(BoundaryId, other.BoundaryId, StringComparison.Ordinal);
    }

    public sealed class RmapBoundaryAuthoringSource
    {
        internal RmapBoundaryAuthoringSource(
            MoonpalaceBiomePair pair,
            string sourceTypeName,
            string pairRuleId,
            IEnumerable<string> candidateIds,
            IEnumerable<string> profileIds,
            string horizontalEdgeSignatureId,
            string verticalEdgeSignatureId)
        {
            if (!pair.IsDefined) throw new ArgumentException("Pair must be defined.", nameof(pair));
            Pair = pair;
            SourceTypeName = RmapWorldBiomeIdentity.Require(sourceTypeName, nameof(sourceTypeName));
            PairRuleId = RmapWorldBiomeIdentity.Require(pairRuleId, nameof(pairRuleId));
            CandidateIds = Ordered(candidateIds, nameof(candidateIds));
            ProfileIds = Ordered(profileIds, nameof(profileIds));
            HorizontalEdgeSignatureId = RmapWorldBiomeIdentity.Require(horizontalEdgeSignatureId, nameof(horizontalEdgeSignatureId));
            VerticalEdgeSignatureId = RmapWorldBiomeIdentity.Require(verticalEdgeSignatureId, nameof(verticalEdgeSignatureId));
            PairDefinition = MoonpalaceBiomePairCatalog.Canonical.GetDefinition(pair);
        }

        public MoonpalaceBiomePair Pair { get; }
        public string SourceTypeName { get; }
        public string PairRuleId { get; }
        public IReadOnlyList<string> CandidateIds { get; }
        public IReadOnlyList<string> ProfileIds { get; }
        public string HorizontalEdgeSignatureId { get; }
        public string VerticalEdgeSignatureId { get; }
        public MoonpalaceBiomePairDefinition PairDefinition { get; }

        private static IReadOnlyList<string> Ordered(IEnumerable<string> values, string name)
        {
            var copy = (values ?? throw new ArgumentNullException(name)).Where(value =>
                !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).OrderBy(value => value,
                StringComparer.Ordinal).ToArray();
            if (copy.Length == 0) throw new ArgumentException("Boundary authoring values are required.", name);
            return new ReadOnlyCollection<string>(copy);
        }
    }

    public sealed class RmapWorldBiomeReservationInput : IComparable<RmapWorldBiomeReservationInput>
    {
        internal RmapWorldBiomeReservationInput(
            RmapWorldGraphReservation reservation,
            MoonpalaceBiomeId allowedBiome,
            IEnumerable<RmapWorldBiomePatch> patches)
        {
            Reservation = reservation ?? throw new ArgumentNullException(nameof(reservation));
            if (!allowedBiome.IsDefined) throw new ArgumentException("Biome must be defined.", nameof(allowedBiome));
            var copy = (patches ?? Array.Empty<RmapWorldBiomePatch>()).Where(value => value != null)
                .OrderBy(value => value).ToArray();
            if (copy.Length == 0 || copy.Any(value => value.Biome != allowedBiome))
                throw new ArgumentException("Reservation needs eligible same-biome patches.", nameof(patches));
            AllowedBiome = allowedBiome;
            CandidatePatches = new ReadOnlyCollection<RmapWorldBiomePatch>(copy);
        }

        public RmapWorldGraphReservation Reservation { get; }
        public RmapWorldGraphRole Role => Reservation.Node.Role;
        public MoonpalaceBiomeId AllowedBiome { get; }
        public IReadOnlyList<RmapWorldBiomePatch> CandidatePatches { get; }
        public string PlacementStatus => "NOT_PLACED_RMAP15";
        public string BoundsStatus => "PATCH_SET_ONLY";
        public string CandidatePatchIds => string.Join("|", CandidatePatches.Select(value => value.PatchId));
        public string CandidateBounds => string.Join("|", CandidatePatches.Select(value => string.Join(",", new[]
        {
            value.MinMicroX.ToString(CultureInfo.InvariantCulture), value.MinMicroY.ToString(CultureInfo.InvariantCulture),
            value.MaxMicroX.ToString(CultureInfo.InvariantCulture), value.MaxMicroY.ToString(CultureInfo.InvariantCulture),
        })));
        public int CompareTo(RmapWorldBiomeReservationInput other) => other == null ? 1 :
            string.Compare(Reservation.ReservationId, other.Reservation.ReservationId, StringComparison.Ordinal);
    }

    public sealed class RmapWorldBiomePlan
    {
        internal RmapWorldBiomePlan(
            RmapWorldDefinition definition,
            RmapWorldGraphPlan graph,
            ulong profileRngInitialState,
            IEnumerable<RmapWorldBiomePatch> patches,
            IEnumerable<RmapWorldBiomeCell> cells,
            IEnumerable<RmapWorldBiomeBoundary> boundaries,
            IEnumerable<RmapWorldBiomeReservationInput> reservationInputs)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            ProfileRngInitialState = profileRngInitialState;
            Patches = Read(patches);
            Cells = Read(cells);
            Boundaries = Read(boundaries);
            ReservationInputs = Read(reservationInputs);
            if (Cells.Count != RmapWorldBiomePlanner.MicroChunkCount ||
                Cells.Select(value => value.Index).Distinct().Count() != Cells.Count ||
                Cells.Any(value => !Patches.Contains(value.Patch)))
                throw new ArgumentException("The plan must own every microchunk exactly once.");
            if (Patches.Count == 0 || Patches.Any(value => !Cells.Any(cell => cell.PatchId == value.PatchId)))
                throw new ArgumentException("Every patch must own at least one microchunk.");
            if (ReservationInputs.Count != Graph.Reservations.Count)
                throw new ArgumentException("Every graph reservation needs an RMAP15 handoff input.");
            var digestLines = new[]
            {
                "RMAP14_BIOME_PLAN_V1", Definition.Digest, Graph.Digest,
                ProfileRngInitialState.ToString("x16", CultureInfo.InvariantCulture),
            }.Concat(Patches.Select(value => string.Join("|", new[]
            {
                value.PatchId, value.BiomeKey, value.MinMicroX.ToString(CultureInfo.InvariantCulture),
                value.MinMicroY.ToString(CultureInfo.InvariantCulture), value.DensityProfile.ToString(),
            }))).Concat(Cells.Select(value => string.Join("|", new[]
            {
                value.Index.ToString(CultureInfo.InvariantCulture), value.PatchId, value.OwnershipState.ToString(),
            }))).Concat(Boundaries.Select(value => value.BoundaryId)).Concat(
                ReservationInputs.Select(value => value.Reservation.ReservationId));
            Digest = RmapWorldBiomeIdentity.Hash(string.Join("\n", digestLines));
        }

        public RmapWorldDefinition Definition { get; }
        public RmapWorldGraphPlan Graph { get; }
        public ulong ProfileRngInitialState { get; }
        public IReadOnlyList<RmapWorldBiomePatch> Patches { get; }
        public IReadOnlyList<RmapWorldBiomeCell> Cells { get; }
        public IReadOnlyList<RmapWorldBiomeBoundary> Boundaries { get; }
        public IReadOnlyList<RmapWorldBiomeReservationInput> ReservationInputs { get; }
        public string Digest { get; }
        public bool Success => Graph.Success && Cells.Count == RmapWorldBiomePlanner.MicroChunkCount &&
            Patches.All(patch => Cells.Any(cell => cell.PatchId == patch.PatchId)) &&
            ReservationInputs.All(value => value.CandidatePatches.Count > 0);

        public RmapWorldBiomeCell GetCell(int x, int y)
        {
            if (x < 0 || x >= RmapWorldBiomePlanner.MicroChunkColumns ||
                y < 0 || y >= RmapWorldBiomePlanner.MicroChunkRows)
                throw new ArgumentOutOfRangeException(nameof(x));
            return Cells[(y * RmapWorldBiomePlanner.MicroChunkColumns) + x];
        }

        private static IReadOnlyList<T> Read<T>(IEnumerable<T> values) where T : IComparable<T>
        {
            return new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }
    }

    public static class RmapWorldBiomePlanner
    {
        public const int WorldWidthTiles = 624;
        public const int WorldHeightTiles = 416;
        public const int MicroChunkWidthTiles = 12;
        public const int MicroChunkHeightTiles = 8;
        public const int MicroChunkColumns = WorldWidthTiles / MicroChunkWidthTiles;
        public const int MicroChunkRows = WorldHeightTiles / MicroChunkHeightTiles;
        public const int MicroChunkCount = MicroChunkColumns * MicroChunkRows;
        public const string ProfileSelectionScope = "RMAP14_PROFILE_SELECTION";

        private static readonly int[,] PatchMatrix =
        {
            { 0, 1, 3, 2 },
            { 1, 2, 0, 3 },
            { 3, 0, 2, 1 },
            { 2, 3, 1, 0 },
        };

        private static readonly IReadOnlyDictionary<RmapBiomeDensityProfileId, RmapBiomeDensityProfileDefinition>
            DensityProfiles = new ReadOnlyDictionary<RmapBiomeDensityProfileId, RmapBiomeDensityProfileDefinition>(
                new Dictionary<RmapBiomeDensityProfileId, RmapBiomeDensityProfileDefinition>
                {
                    { RmapBiomeDensityProfileId.Open, new RmapBiomeDensityProfileDefinition(
                        RmapBiomeDensityProfileId.Open, 400, 550, "Surface and observation space") },
                    { RmapBiomeDensityProfileId.Balanced, new RmapBiomeDensityProfileDefinition(
                        RmapBiomeDensityProfileId.Balanced, 550, 650, "General traversal target") },
                    { RmapBiomeDensityProfileId.Dense, new RmapBiomeDensityProfileDefinition(
                        RmapBiomeDensityProfileId.Dense, 650, 750, "Root, mine, and hostile-zone target") },
                });

        public static IReadOnlyDictionary<RmapBiomeDensityProfileId, RmapBiomeDensityProfileDefinition>
            ProfileCatalog => DensityProfiles;

        public static RmapWorldBiomePlan Plan(
            RmapWorldDefinition definition,
            WorldGenerationRngStreams rngStreams)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            if (definition.RngBindings[RmapWorldRngStream.Biome].SourceStreamId !=
                WorldGenerationRngStreams.BiomePatchStreamId)
                throw new ArgumentException("RMAP14 requires the verified RMAP12 biome stream.", nameof(definition));

            RmapWorldGraphPlan graph = RmapWorldGraphPlanner.Plan(definition);
            if (!graph.Success) throw new InvalidOperationException("RMAP13 graph input did not pass.");
            DeterministicRngStream profileRng = rngStreams.CreateBiomePatch(definition.Request.Seed,
                ProfileSelectionScope, definition.Request.AttemptOrdinal);
            int biomeRotation = profileRng.NextInt(4);
            var drafts = CreateDrafts(definition, biomeRotation);
            var patches = drafts.OrderBy(value => value.PatchId, StringComparer.Ordinal).Select(draft =>
                CreatePatch(draft, profileRng)).OrderBy(value => value).ToArray();
            var byBlock = patches.ToDictionary(value => BlockKey(value.MinMicroX, value.MinMicroY), value => value,
                StringComparer.Ordinal);
            var cells = CreateCells(byBlock);
            var boundaries = CreateBoundaries(cells);
            var reservationInputs = graph.Reservations.Select(reservation => new RmapWorldBiomeReservationInput(
                reservation, BiomeFor(reservation.Node.Role), patches.Where(patch => patch.Biome ==
                    BiomeFor(reservation.Node.Role))));
            return new RmapWorldBiomePlan(definition, graph, profileRng.InitialState, patches, cells,
                boundaries, reservationInputs);
        }

        private static IEnumerable<PatchDraft> CreateDrafts(RmapWorldDefinition definition, int biomeRotation)
        {
            var biomes = MoonpalaceBiomePairCatalog.Canonical.Biomes;
            const int blockSize = MicroChunkColumns / 4;
            for (var blockY = 0; blockY < 4; blockY++)
            for (var blockX = 0; blockX < 4; blockX++)
            {
                MoonpalaceBiomeId biome = biomes[(PatchMatrix[blockY, blockX] + biomeRotation) % biomes.Count];
                yield return new PatchDraft(Hash("RMAP14_PATCH_V1", definition.Digest, biome.CanonicalId,
                    blockX.ToString(CultureInfo.InvariantCulture), blockY.ToString(CultureInfo.InvariantCulture)),
                    biome, blockX * blockSize, blockY * blockSize, blockSize, blockSize);
            }
        }

        private static RmapWorldBiomePatch CreatePatch(PatchDraft draft, DeterministicRngStream rng)
        {
            int open = Weight(draft.Biome, RmapBiomeDensityProfileId.Open);
            int balanced = Weight(draft.Biome, RmapBiomeDensityProfileId.Balanced);
            int dense = Weight(draft.Biome, RmapBiomeDensityProfileId.Dense);
            int draw = rng.NextInt(open + balanced + dense);
            RmapBiomeDensityProfileId profile = draw < open ? RmapBiomeDensityProfileId.Open :
                draw < open + balanced ? RmapBiomeDensityProfileId.Balanced : RmapBiomeDensityProfileId.Dense;
            return new RmapWorldBiomePatch(draft.PatchId, draft.Biome, draft.MinX, draft.MinY,
                draft.Width, draft.Height, profile, open, balanced, dense);
        }

        private static IReadOnlyList<RmapWorldBiomeCell> CreateCells(
            IReadOnlyDictionary<string, RmapWorldBiomePatch> patches)
        {
            var result = new List<RmapWorldBiomeCell>(MicroChunkCount);
            const int blockSize = MicroChunkColumns / 4;
            for (var y = 0; y < MicroChunkRows; y++)
            for (var x = 0; x < MicroChunkColumns; x++)
            {
                string key = BlockKey((x / blockSize) * blockSize, (y / blockSize) * blockSize);
                if (!patches.TryGetValue(key, out RmapWorldBiomePatch patch))
                    throw new InvalidOperationException("A 52x52 owner has no patch.");
                result.Add(new RmapWorldBiomeCell(x, y, patch));
            }
            return result;
        }

        private static IReadOnlyList<RmapWorldBiomeBoundary> CreateBoundaries(
            IReadOnlyList<RmapWorldBiomeCell> cells)
        {
            var byIndex = cells.ToDictionary(value => value.Index, value => value);
            var result = new List<RmapWorldBiomeBoundary>();
            for (var y = 0; y < MicroChunkRows; y++)
            for (var x = 0; x < MicroChunkColumns; x++)
            {
                RmapWorldBiomeCell source = byIndex[(y * MicroChunkColumns) + x];
                if (x + 1 < MicroChunkColumns)
                    AddBoundary(result, source, byIndex[(y * MicroChunkColumns) + x + 1],
                        RmapWorldGraphDirection.Right);
                if (y + 1 < MicroChunkRows)
                    AddBoundary(result, source, byIndex[((y + 1) * MicroChunkColumns) + x],
                        RmapWorldGraphDirection.Up);
            }
            return result;
        }

        private static void AddBoundary(
            ICollection<RmapWorldBiomeBoundary> result,
            RmapWorldBiomeCell source,
            RmapWorldBiomeCell target,
            RmapWorldGraphDirection direction)
        {
            if (source.PatchId == target.PatchId) return;
            result.Add(new RmapWorldBiomeBoundary(source, target, direction,
                BoundarySource(new MoonpalaceBiomePair(source.Biome, target.Biome))));
        }

        private static RmapBoundaryAuthoringSource BoundarySource(MoonpalaceBiomePair pair)
        {
            if (pair == MoonpalaceCraterRootBoundaryAuthoringContract.Pair) return Source(
                pair, nameof(MoonpalaceCraterRootBoundaryAuthoringContract),
                MoonpalaceCraterRootBoundaryAuthoringContract.PairRuleId,
                MoonpalaceCraterRootBoundaryAuthoringContract.CandidateIds,
                MoonpalaceCraterRootBoundaryAuthoringContract.ProfileIds,
                MoonpalaceCraterRootBoundaryAuthoringContract.HorizontalEdgeSignatureId,
                MoonpalaceCraterRootBoundaryAuthoringContract.VerticalEdgeSignatureId);
            if (pair == MoonpalaceCraterMillBoundaryAuthoringContract.Pair) return Source(
                pair, nameof(MoonpalaceCraterMillBoundaryAuthoringContract),
                MoonpalaceCraterMillBoundaryAuthoringContract.PairRuleId,
                MoonpalaceCraterMillBoundaryAuthoringContract.CandidateIds,
                MoonpalaceCraterMillBoundaryAuthoringContract.ProfileIds,
                MoonpalaceCraterMillBoundaryAuthoringContract.HorizontalEdgeSignatureId,
                MoonpalaceCraterMillBoundaryAuthoringContract.VerticalEdgeSignatureId);
            if (pair == MoonpalaceCraterDoughBoundaryAuthoringContract.Pair) return Source(
                pair, nameof(MoonpalaceCraterDoughBoundaryAuthoringContract),
                MoonpalaceCraterDoughBoundaryAuthoringContract.PairRuleId,
                MoonpalaceCraterDoughBoundaryAuthoringContract.CandidateIds,
                MoonpalaceCraterDoughBoundaryAuthoringContract.ProfileIds,
                MoonpalaceCraterDoughBoundaryAuthoringContract.HorizontalEdgeSignatureId,
                MoonpalaceCraterDoughBoundaryAuthoringContract.VerticalEdgeSignatureId);
            if (pair == MoonpalaceRootMillBoundaryAuthoringContract.Pair) return Source(
                pair, nameof(MoonpalaceRootMillBoundaryAuthoringContract),
                MoonpalaceRootMillBoundaryAuthoringContract.PairRuleId,
                MoonpalaceRootMillBoundaryAuthoringContract.CandidateIds,
                MoonpalaceRootMillBoundaryAuthoringContract.ProfileIds,
                MoonpalaceRootMillBoundaryAuthoringContract.HorizontalEdgeSignatureId,
                MoonpalaceRootMillBoundaryAuthoringContract.VerticalEdgeSignatureId);
            if (pair == MoonpalaceRootDoughBoundaryAuthoringContract.Pair) return Source(
                pair, nameof(MoonpalaceRootDoughBoundaryAuthoringContract),
                MoonpalaceRootDoughBoundaryAuthoringContract.PairRuleId,
                MoonpalaceRootDoughBoundaryAuthoringContract.CandidateIds,
                MoonpalaceRootDoughBoundaryAuthoringContract.ProfileIds,
                MoonpalaceRootDoughBoundaryAuthoringContract.HorizontalEdgeSignatureId,
                MoonpalaceRootDoughBoundaryAuthoringContract.VerticalEdgeSignatureId);
            if (pair == MoonpalaceMillDoughBoundaryAuthoringContract.Pair) return Source(
                pair, nameof(MoonpalaceMillDoughBoundaryAuthoringContract),
                MoonpalaceMillDoughBoundaryAuthoringContract.PairRuleId,
                MoonpalaceMillDoughBoundaryAuthoringContract.CandidateIds,
                MoonpalaceMillDoughBoundaryAuthoringContract.ProfileIds,
                MoonpalaceMillDoughBoundaryAuthoringContract.HorizontalEdgeSignatureId,
                MoonpalaceMillDoughBoundaryAuthoringContract.VerticalEdgeSignatureId);
            throw new InvalidOperationException("No existing Moonpalace authoring source for " + pair.PairId + ".");
        }

        private static RmapBoundaryAuthoringSource Source(
            MoonpalaceBiomePair pair,
            string typeName,
            string ruleId,
            IEnumerable<string> candidates,
            IEnumerable<string> profiles,
            string horizontal,
            string vertical) => new RmapBoundaryAuthoringSource(pair, typeName, ruleId, candidates,
            profiles, horizontal, vertical);

        private static MoonpalaceBiomeId BiomeFor(RmapWorldGraphRole role)
        {
            switch (role)
            {
                case RmapWorldGraphRole.Start:
                case RmapWorldGraphRole.MooncoreOre:
                case RmapWorldGraphRole.Boss: return MoonpalaceBiomeId.MoonCrater;
                case RmapWorldGraphRole.CondensedCoefficientSap:
                case RmapWorldGraphRole.Exit: return MoonpalaceBiomeId.CassiaRoot;
                case RmapWorldGraphRole.DeepStarYeast:
                case RmapWorldGraphRole.Seal: return MoonpalaceBiomeId.MoonDough;
                case RmapWorldGraphRole.Forge: return MoonpalaceBiomeId.AbandonedMill;
                default: throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        private static int Weight(MoonpalaceBiomeId biome, RmapBiomeDensityProfileId profile)
        {
            switch (biome.CanonicalId)
            {
                case "MoonCrater": return profile == RmapBiomeDensityProfileId.Open ? 4 :
                    profile == RmapBiomeDensityProfileId.Balanced ? 4 : 2;
                case "CassiaRoot": return profile == RmapBiomeDensityProfileId.Open ? 3 :
                    profile == RmapBiomeDensityProfileId.Balanced ? 4 : 3;
                case "AbandonedMill": return profile == RmapBiomeDensityProfileId.Open ? 2 :
                    profile == RmapBiomeDensityProfileId.Balanced ? 4 : 4;
                case "MoonDough": return profile == RmapBiomeDensityProfileId.Open ? 4 :
                    profile == RmapBiomeDensityProfileId.Balanced ? 4 : 2;
                default: throw new ArgumentOutOfRangeException(nameof(biome));
            }
        }

        private static string BlockKey(int minX, int minY) => minX.ToString(CultureInfo.InvariantCulture) + "," +
            minY.ToString(CultureInfo.InvariantCulture);
        private static string Hash(params string[] values) => RmapWorldDefinition.Hash(string.Join("\n", values));
        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable value is required.", name);
            return value.Trim();
        }

        private sealed class PatchDraft
        {
            public PatchDraft(string patchId, MoonpalaceBiomeId biome, int minX, int minY, int width, int height)
            {
                PatchId = patchId;
                Biome = biome;
                MinX = minX;
                MinY = minY;
                Width = width;
                Height = height;
            }
            public string PatchId { get; }
            public MoonpalaceBiomeId Biome { get; }
            public int MinX { get; }
            public int MinY { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }

    public static class RmapWorldBiomeExport
    {
        public static string ManifestJson(RmapWorldBiomePlan plan)
        {
            Require(plan);
            return "{\n" + string.Join(",\n", new[]
            {
                Property("schema", "RMAP14_BIOME_MANIFEST_V1"),
                Property("seed", plan.Definition.Request.Seed.ToString(CultureInfo.InvariantCulture), false),
                Property("content_version", plan.Definition.Request.ContentVersion),
                Property("generator_version", plan.Definition.Request.GeneratorVersion),
                Property("pool_version", plan.Definition.PoolVersion),
                Property("pool_digest", plan.Definition.PoolDigest),
                Property("world_definition_digest", plan.Definition.Digest),
                Property("world_graph_digest", plan.Graph.Digest),
                Property("plan_digest", plan.Digest),
                Property("profile_rng_stream", WorldGenerationRngStreams.BiomePatchStreamId),
                Property("profile_rng_scope", RmapWorldBiomePlanner.ProfileSelectionScope),
                Property("profile_rng_initial_state", plan.ProfileRngInitialState.ToString("x16", CultureInfo.InvariantCulture)),
                Property("world_tile_bounds", "624x416"),
                Property("microchunk_grid", "52x52"),
                Property("microchunk_size_tiles", "12x8"),
                Property("ownership_count", plan.Cells.Count.ToString(CultureInfo.InvariantCulture), false),
                Property("patch_count", plan.Patches.Count.ToString(CultureInfo.InvariantCulture), false),
                Property("boundary_count", plan.Boundaries.Count.ToString(CultureInfo.InvariantCulture), false),
                Property("measured_density", "PENDING_GEOMETRY"),
                Property("physical_bake", "NOT_RMAP14"),
                Property("special_footprints", "NOT_PLACED_RMAP15"),
            }) + "\n}\n";
        }

        public static string PatchOwnershipCsv(RmapWorldBiomePlan plan) => Lines(
            "micro_x,micro_y,tile_min_x,tile_min_y,patch_id,biome_key,owner_state", plan.Cells.OrderBy(value => value)
                .Select(value => Row(value.X, value.Y, value.X * RmapWorldBiomePlanner.MicroChunkWidthTiles,
                    value.Y * RmapWorldBiomePlanner.MicroChunkHeightTiles, value.PatchId,
                    value.Biome.CanonicalId, value.OwnershipState)));

        public static string PatchesCsv(RmapWorldBiomePlan plan) => Lines(
            "patch_id,biome_key,biome_display_name,micro_min_x,micro_min_y,micro_max_x,micro_max_y,tile_min_x,tile_min_y,tile_max_x,tile_max_y,area_microchunks,density_profile,owner_state,density_metric_status,density_metric_scope", plan.Patches.OrderBy(value => value)
                .Select(value => Row(value.PatchId, value.BiomeKey, value.BiomeDisplayName, value.MinMicroX,
                    value.MinMicroY, value.MaxMicroX, value.MaxMicroY, value.MinTileX, value.MinTileY,
                    value.MaxTileX, value.MaxTileY, value.AreaMicroChunks, value.DensityProfile,
                    value.OwnershipState, value.DensityMetricStatus, value.DensityMetricScopeId)));

        public static string BoundariesCsv(RmapWorldBiomePlan plan) => Lines(
            "boundary_id,source_micro_x,source_micro_y,target_micro_x,target_micro_y,direction,source_patch_id,target_patch_id,canonical_pair,orientation,source_contract,pair_rule_id,edge_signature_id,authoring_candidate_ids,authoring_profile_ids,tool_requirement,mandatory_route_allowed", plan.Boundaries.OrderBy(value => value)
                .Select(value => Row(value.BoundaryId, value.Source.X, value.Source.Y, value.Target.X,
                    value.Target.Y, value.Direction, value.Source.PatchId, value.Target.PatchId,
                    value.Pair.PairId, value.Orientation, value.SourceContract.SourceTypeName,
                    value.SourceContract.PairRuleId, value.EdgeSignatureId,
                    string.Join("|", value.SourceContract.CandidateIds),
                    string.Join("|", value.SourceContract.ProfileIds),
                    value.SourceContract.PairDefinition.MandatoryToolRequirement,
                    value.SourceContract.PairDefinition.MandatoryRouteAllowed)));

        public static string ProfilesCsv(RmapWorldBiomePlan plan) => Lines(
            "patch_id,biome_key,selected_profile,open_weight,balanced_weight,dense_weight,target_min_permille,target_max_permille,metric_scope,measured_density", plan.Patches.OrderBy(value => value)
                .Select(value =>
                {
                    RmapBiomeDensityProfileDefinition profile = RmapWorldBiomePlanner.ProfileCatalog[value.DensityProfile];
                    return Row(value.PatchId, value.BiomeKey, value.DensityProfile, value.OpenWeight,
                        value.BalancedWeight, value.DenseWeight, profile.TargetMinimumPermille,
                        profile.TargetMaximumPermille, value.DensityMetricScopeId, value.DensityMetricStatus);
                }));

        public static string ReservationInputsCsv(RmapWorldBiomePlan plan) => Lines(
            "reservation_id,graph_node_id,role,graph_anchor_stable_id,entry_direction,release_condition,allowed_biome,candidate_patch_ids,candidate_micro_bounds,bounds_status,placement_status", plan.ReservationInputs.OrderBy(value => value)
                .Select(value => Row(value.Reservation.ReservationId, value.Reservation.Node.NodeId, value.Role,
                    value.Reservation.Node.AnchorStableId, value.Reservation.EntryDirection,
                    value.Reservation.ReleaseCondition, value.AllowedBiome.CanonicalId, value.CandidatePatchIds,
                    value.CandidateBounds, value.BoundsStatus, value.PlacementStatus)));

        private static string Lines(string header, IEnumerable<string> rows) => header + "\n" +
            string.Join("\n", rows ?? Array.Empty<string>()) + "\n";
        private static string Row(params object[] values) => string.Join(",", values.Select(value => Escape(
            value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture))));
        private static string Escape(string value) => '"' + (value ?? string.Empty).Replace("\"", "\"\"") + '"';
        private static string Property(string name, string value, bool quoted = true) => "  \"" + EscapeJson(name) + "\": " +
            (quoted ? "\"" + EscapeJson(value) + "\"" : value);
        private static string EscapeJson(string value) => (value ?? string.Empty).Replace("\\", "\\\\")
            .Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        private static void Require(RmapWorldBiomePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
        }
    }
}
