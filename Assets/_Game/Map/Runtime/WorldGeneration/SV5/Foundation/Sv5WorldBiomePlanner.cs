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
    public enum Sv5BiomeDensityProfileId { Open, Balanced, Dense }

    /// <summary>Planning ownership is deliberately distinct from baked collision
    /// or traversal. SV5 owns choosing any fixed special footprint.</summary>
    public enum Sv5BiomeOwnershipState { Active, Secret, InactiveSolid, SpecialReserved }

    internal static class Sv5WorldBiomeIdentity
    {
        public static string Hash(params string[] values) => Sv5WorldDefinition.Hash(string.Join("\n", values));

        public static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable value is required.", name);
            return value.Trim();
        }
    }

    public sealed class Sv5BiomeDensityProfileDefinition
    {
        internal Sv5BiomeDensityProfileDefinition(
            Sv5BiomeDensityProfileId id,
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
            Purpose = Sv5WorldBiomeIdentity.Require(purpose, nameof(purpose));
        }

        public Sv5BiomeDensityProfileId Id { get; }
        public int TargetMinimumPermille { get; }
        public int TargetMaximumPermille { get; }
        public string Purpose { get; }
    }

    public sealed class Sv5WorldBiomeCell : IComparable<Sv5WorldBiomeCell>
    {
        internal Sv5WorldBiomeCell(int x, int y, Sv5WorldBiomePatch patch)
        {
            if (x < 0 || x >= Sv5WorldBiomePlanner.MicroChunkColumns ||
                y < 0 || y >= Sv5WorldBiomePlanner.MicroChunkRows)
                throw new ArgumentOutOfRangeException(nameof(x));
            Patch = patch ?? throw new ArgumentNullException(nameof(patch));
            X = x;
            Y = y;
            Index = (y * Sv5WorldBiomePlanner.MicroChunkColumns) + x;
        }

        public int X { get; }
        public int Y { get; }
        public int Index { get; }
        public Sv5WorldBiomePatch Patch { get; }
        public string PatchId => Patch.PatchId;
        public MoonpalaceBiomeId Biome => Patch.Biome;
        public Sv5BiomeOwnershipState OwnershipState => Patch.OwnershipState;
        public int CompareTo(Sv5WorldBiomeCell other) => other == null ? 1 : Index.CompareTo(other.Index);
    }

    public sealed class Sv5WorldBiomePatch : IComparable<Sv5WorldBiomePatch>
    {
        internal Sv5WorldBiomePatch(
            string patchId,
            MoonpalaceBiomeId biome,
            int minMicroX,
            int minMicroY,
            int widthMicroChunks,
            int heightMicroChunks,
            Sv5BiomeDensityProfileId densityProfile,
            int openWeight,
            int balancedWeight,
            int denseWeight)
        {
            if (!biome.IsDefined) throw new ArgumentException("Biome must be defined.", nameof(biome));
            if (minMicroX < 0 || minMicroY < 0 || widthMicroChunks < 1 || heightMicroChunks < 1 ||
                minMicroX + widthMicroChunks > Sv5WorldBiomePlanner.MicroChunkColumns ||
                minMicroY + heightMicroChunks > Sv5WorldBiomePlanner.MicroChunkRows)
                throw new ArgumentOutOfRangeException(nameof(minMicroX));
            if (openWeight <= 0 || balancedWeight <= 0 || denseWeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(openWeight));

            PatchId = Sv5WorldBiomeIdentity.Require(patchId, nameof(patchId));
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
        public int MinTileX => MinMicroX * Sv5WorldBiomePlanner.MicroChunkWidthTiles;
        public int MinTileY => MinMicroY * Sv5WorldBiomePlanner.MicroChunkHeightTiles;
        public int MaxTileX => ((MaxMicroX + 1) * Sv5WorldBiomePlanner.MicroChunkWidthTiles) - 1;
        public int MaxTileY => ((MaxMicroY + 1) * Sv5WorldBiomePlanner.MicroChunkHeightTiles) - 1;
        public Sv5BiomeDensityProfileId DensityProfile { get; }
        public int OpenWeight { get; }
        public int BalancedWeight { get; }
        public int DenseWeight { get; }
        public Sv5BiomeOwnershipState OwnershipState => Sv5BiomeOwnershipState.Active;
        public string DensityMetricStatus => "PENDING_GEOMETRY";
        public string DensityMetricScopeId => "SV5_PATCH_SCOPE|" + PatchId;
        public int CompareTo(Sv5WorldBiomePatch other) => other == null ? 1 :
            string.Compare(PatchId, other.PatchId, StringComparison.Ordinal);
    }

    public sealed class Sv5WorldBiomeBoundary : IComparable<Sv5WorldBiomeBoundary>
    {
        internal Sv5WorldBiomeBoundary(
            Sv5WorldBiomeCell source,
            Sv5WorldBiomeCell target,
            Sv5WorldGraphDirection direction,
            Sv5BoundaryAuthoringSource sourceContract)
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
            Orientation = direction == Sv5WorldGraphDirection.Left || direction == Sv5WorldGraphDirection.Right
                ? MoonpalaceBoundaryOrientation.Horizontal : MoonpalaceBoundaryOrientation.Vertical;
            if (!SourceContract.PairDefinition.Supports(Orientation))
                throw new ArgumentException("Boundary direction is not supported by the existing pair contract.");
            BoundaryId = Sv5WorldBiomeIdentity.Hash("SV5_BOUNDARY_V1", Source.PatchId, Target.PatchId,
                Source.X.ToString(CultureInfo.InvariantCulture), Source.Y.ToString(CultureInfo.InvariantCulture),
                direction.ToString(), SourceContract.PairRuleId);
        }

        public string BoundaryId { get; }
        public Sv5WorldBiomeCell Source { get; }
        public Sv5WorldBiomeCell Target { get; }
        public Sv5WorldGraphDirection Direction { get; }
        public MoonpalaceBiomePair Pair { get; }
        public MoonpalaceBoundaryOrientation Orientation { get; }
        public Sv5BoundaryAuthoringSource SourceContract { get; }
        public string EdgeSignatureId => Orientation == MoonpalaceBoundaryOrientation.Horizontal
            ? SourceContract.HorizontalEdgeSignatureId : SourceContract.VerticalEdgeSignatureId;
        public int CompareTo(Sv5WorldBiomeBoundary other) => other == null ? 1 :
            string.Compare(BoundaryId, other.BoundaryId, StringComparison.Ordinal);
    }

    public sealed class Sv5BoundaryAuthoringSource
    {
        internal Sv5BoundaryAuthoringSource(
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
            SourceTypeName = Sv5WorldBiomeIdentity.Require(sourceTypeName, nameof(sourceTypeName));
            PairRuleId = Sv5WorldBiomeIdentity.Require(pairRuleId, nameof(pairRuleId));
            CandidateIds = Ordered(candidateIds, nameof(candidateIds));
            ProfileIds = Ordered(profileIds, nameof(profileIds));
            HorizontalEdgeSignatureId = Sv5WorldBiomeIdentity.Require(horizontalEdgeSignatureId, nameof(horizontalEdgeSignatureId));
            VerticalEdgeSignatureId = Sv5WorldBiomeIdentity.Require(verticalEdgeSignatureId, nameof(verticalEdgeSignatureId));
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

    public sealed class Sv5WorldBiomeReservationInput : IComparable<Sv5WorldBiomeReservationInput>
    {
        internal Sv5WorldBiomeReservationInput(
            Sv5WorldGraphReservation reservation,
            MoonpalaceBiomeId allowedBiome,
            IEnumerable<Sv5WorldBiomePatch> patches)
        {
            Reservation = reservation ?? throw new ArgumentNullException(nameof(reservation));
            if (!allowedBiome.IsDefined) throw new ArgumentException("Biome must be defined.", nameof(allowedBiome));
            var copy = (patches ?? Array.Empty<Sv5WorldBiomePatch>()).Where(value => value != null)
                .OrderBy(value => value).ToArray();
            if (copy.Length == 0 || copy.Any(value => value.Biome != allowedBiome))
                throw new ArgumentException("Reservation needs eligible same-biome patches.", nameof(patches));
            AllowedBiome = allowedBiome;
            CandidatePatches = new ReadOnlyCollection<Sv5WorldBiomePatch>(copy);
        }

        public Sv5WorldGraphReservation Reservation { get; }
        public Sv5WorldGraphRole Role => Reservation.Node.Role;
        public MoonpalaceBiomeId AllowedBiome { get; }
        public IReadOnlyList<Sv5WorldBiomePatch> CandidatePatches { get; }
        public string PlacementStatus => "NOT_PLACED_SV515";
        public string BoundsStatus => "PATCH_SET_ONLY";
        public string CandidatePatchIds => string.Join("|", CandidatePatches.Select(value => value.PatchId));
        public string CandidateBounds => string.Join("|", CandidatePatches.Select(value => string.Join(",", new[]
        {
            value.MinMicroX.ToString(CultureInfo.InvariantCulture), value.MinMicroY.ToString(CultureInfo.InvariantCulture),
            value.MaxMicroX.ToString(CultureInfo.InvariantCulture), value.MaxMicroY.ToString(CultureInfo.InvariantCulture),
        })));
        public int CompareTo(Sv5WorldBiomeReservationInput other) => other == null ? 1 :
            string.Compare(Reservation.ReservationId, other.Reservation.ReservationId, StringComparison.Ordinal);
    }

    public sealed class Sv5WorldBiomePlan
    {
        internal Sv5WorldBiomePlan(
            Sv5WorldDefinition definition,
            Sv5WorldGraphPlan graph,
            ulong profileRngInitialState,
            IEnumerable<Sv5WorldBiomePatch> patches,
            IEnumerable<Sv5WorldBiomeCell> cells,
            IEnumerable<Sv5WorldBiomeBoundary> boundaries,
            IEnumerable<Sv5WorldBiomeReservationInput> reservationInputs)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            ProfileRngInitialState = profileRngInitialState;
            Patches = Read(patches);
            Cells = Read(cells);
            Boundaries = Read(boundaries);
            ReservationInputs = Read(reservationInputs);
            if (Cells.Count != Sv5WorldBiomePlanner.MicroChunkCount ||
                Cells.Select(value => value.Index).Distinct().Count() != Cells.Count ||
                Cells.Any(value => !Patches.Contains(value.Patch)))
                throw new ArgumentException("The plan must own every microchunk exactly once.");
            if (Patches.Count == 0 || Patches.Any(value => !Cells.Any(cell => cell.PatchId == value.PatchId)))
                throw new ArgumentException("Every patch must own at least one microchunk.");
            if (ReservationInputs.Count != Graph.Reservations.Count)
                throw new ArgumentException("Every graph reservation needs an SV5 handoff input.");
            var digestLines = new[]
            {
                "SV5_BIOME_PLAN_V1", Definition.Digest, Graph.Digest,
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
            Digest = Sv5WorldBiomeIdentity.Hash(string.Join("\n", digestLines));
        }

        public Sv5WorldDefinition Definition { get; }
        public Sv5WorldGraphPlan Graph { get; }
        public ulong ProfileRngInitialState { get; }
        public IReadOnlyList<Sv5WorldBiomePatch> Patches { get; }
        public IReadOnlyList<Sv5WorldBiomeCell> Cells { get; }
        public IReadOnlyList<Sv5WorldBiomeBoundary> Boundaries { get; }
        public IReadOnlyList<Sv5WorldBiomeReservationInput> ReservationInputs { get; }
        public string Digest { get; }
        public bool Success => Graph.Success && Cells.Count == Sv5WorldBiomePlanner.MicroChunkCount &&
            Patches.All(patch => Cells.Any(cell => cell.PatchId == patch.PatchId)) &&
            ReservationInputs.All(value => value.CandidatePatches.Count > 0);

        public Sv5WorldBiomeCell GetCell(int x, int y)
        {
            if (x < 0 || x >= Sv5WorldBiomePlanner.MicroChunkColumns ||
                y < 0 || y >= Sv5WorldBiomePlanner.MicroChunkRows)
                throw new ArgumentOutOfRangeException(nameof(x));
            return Cells[(y * Sv5WorldBiomePlanner.MicroChunkColumns) + x];
        }

        private static IReadOnlyList<T> Read<T>(IEnumerable<T> values) where T : IComparable<T>
        {
            return new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }
    }

    public static class Sv5WorldBiomePlanner
    {
        public const int WorldWidthTiles = 624;
        public const int WorldHeightTiles = 416;
        public const int MicroChunkWidthTiles = 12;
        public const int MicroChunkHeightTiles = 8;
        public const int MicroChunkColumns = WorldWidthTiles / MicroChunkWidthTiles;
        public const int MicroChunkRows = WorldHeightTiles / MicroChunkHeightTiles;
        public const int MicroChunkCount = MicroChunkColumns * MicroChunkRows;
        public const string ProfileSelectionScope = "SV5_PROFILE_SELECTION";

        private static readonly int[,] PatchMatrix =
        {
            { 0, 1, 3, 2 },
            { 1, 2, 0, 3 },
            { 3, 0, 2, 1 },
            { 2, 3, 1, 0 },
        };

        private static readonly IReadOnlyDictionary<Sv5BiomeDensityProfileId, Sv5BiomeDensityProfileDefinition>
            DensityProfiles = new ReadOnlyDictionary<Sv5BiomeDensityProfileId, Sv5BiomeDensityProfileDefinition>(
                new Dictionary<Sv5BiomeDensityProfileId, Sv5BiomeDensityProfileDefinition>
                {
                    { Sv5BiomeDensityProfileId.Open, new Sv5BiomeDensityProfileDefinition(
                        Sv5BiomeDensityProfileId.Open, 400, 550, "Surface and observation space") },
                    { Sv5BiomeDensityProfileId.Balanced, new Sv5BiomeDensityProfileDefinition(
                        Sv5BiomeDensityProfileId.Balanced, 550, 650, "General traversal target") },
                    { Sv5BiomeDensityProfileId.Dense, new Sv5BiomeDensityProfileDefinition(
                        Sv5BiomeDensityProfileId.Dense, 650, 750, "Root, mine, and hostile-zone target") },
                });

        public static IReadOnlyDictionary<Sv5BiomeDensityProfileId, Sv5BiomeDensityProfileDefinition>
            ProfileCatalog => DensityProfiles;

        public static Sv5WorldBiomePlan Plan(
            Sv5WorldDefinition definition,
            WorldGenerationRngStreams rngStreams)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            if (definition.RngBindings[Sv5WorldRngStream.Biome].SourceStreamId !=
                WorldGenerationRngStreams.BiomePatchStreamId)
                throw new ArgumentException("SV5 requires the verified SV5 biome stream.", nameof(definition));

            Sv5WorldGraphPlan graph = Sv5WorldGraphPlanner.Plan(definition);
            if (!graph.Success) throw new InvalidOperationException("SV5 graph input did not pass.");
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
            var reservationInputs = graph.Reservations.Select(reservation => new Sv5WorldBiomeReservationInput(
                reservation, BiomeFor(reservation.Node.Role), patches.Where(patch => patch.Biome ==
                    BiomeFor(reservation.Node.Role))));
            return new Sv5WorldBiomePlan(definition, graph, profileRng.InitialState, patches, cells,
                boundaries, reservationInputs);
        }

        private static IEnumerable<PatchDraft> CreateDrafts(Sv5WorldDefinition definition, int biomeRotation)
        {
            var biomes = MoonpalaceBiomePairCatalog.Canonical.Biomes;
            const int blockSize = MicroChunkColumns / 4;
            for (var blockY = 0; blockY < 4; blockY++)
            for (var blockX = 0; blockX < 4; blockX++)
            {
                MoonpalaceBiomeId biome = biomes[(PatchMatrix[blockY, blockX] + biomeRotation) % biomes.Count];
                yield return new PatchDraft(Hash("SV5_PATCH_V1", definition.Digest, biome.CanonicalId,
                    blockX.ToString(CultureInfo.InvariantCulture), blockY.ToString(CultureInfo.InvariantCulture)),
                    biome, blockX * blockSize, blockY * blockSize, blockSize, blockSize);
            }
        }

        private static Sv5WorldBiomePatch CreatePatch(PatchDraft draft, DeterministicRngStream rng)
        {
            int open = Weight(draft.Biome, Sv5BiomeDensityProfileId.Open);
            int balanced = Weight(draft.Biome, Sv5BiomeDensityProfileId.Balanced);
            int dense = Weight(draft.Biome, Sv5BiomeDensityProfileId.Dense);
            int draw = rng.NextInt(open + balanced + dense);
            Sv5BiomeDensityProfileId profile = draw < open ? Sv5BiomeDensityProfileId.Open :
                draw < open + balanced ? Sv5BiomeDensityProfileId.Balanced : Sv5BiomeDensityProfileId.Dense;
            return new Sv5WorldBiomePatch(draft.PatchId, draft.Biome, draft.MinX, draft.MinY,
                draft.Width, draft.Height, profile, open, balanced, dense);
        }

        private static IReadOnlyList<Sv5WorldBiomeCell> CreateCells(
            IReadOnlyDictionary<string, Sv5WorldBiomePatch> patches)
        {
            var result = new List<Sv5WorldBiomeCell>(MicroChunkCount);
            const int blockSize = MicroChunkColumns / 4;
            for (var y = 0; y < MicroChunkRows; y++)
            for (var x = 0; x < MicroChunkColumns; x++)
            {
                string key = BlockKey((x / blockSize) * blockSize, (y / blockSize) * blockSize);
                if (!patches.TryGetValue(key, out Sv5WorldBiomePatch patch))
                    throw new InvalidOperationException("A 52x52 owner has no patch.");
                result.Add(new Sv5WorldBiomeCell(x, y, patch));
            }
            return result;
        }

        private static IReadOnlyList<Sv5WorldBiomeBoundary> CreateBoundaries(
            IReadOnlyList<Sv5WorldBiomeCell> cells)
        {
            var byIndex = cells.ToDictionary(value => value.Index, value => value);
            var result = new List<Sv5WorldBiomeBoundary>();
            for (var y = 0; y < MicroChunkRows; y++)
            for (var x = 0; x < MicroChunkColumns; x++)
            {
                Sv5WorldBiomeCell source = byIndex[(y * MicroChunkColumns) + x];
                if (x + 1 < MicroChunkColumns)
                    AddBoundary(result, source, byIndex[(y * MicroChunkColumns) + x + 1],
                        Sv5WorldGraphDirection.Right);
                if (y + 1 < MicroChunkRows)
                    AddBoundary(result, source, byIndex[((y + 1) * MicroChunkColumns) + x],
                        Sv5WorldGraphDirection.Up);
            }
            return result;
        }

        private static void AddBoundary(
            ICollection<Sv5WorldBiomeBoundary> result,
            Sv5WorldBiomeCell source,
            Sv5WorldBiomeCell target,
            Sv5WorldGraphDirection direction)
        {
            if (source.PatchId == target.PatchId) return;
            result.Add(new Sv5WorldBiomeBoundary(source, target, direction,
                BoundarySource(new MoonpalaceBiomePair(source.Biome, target.Biome))));
        }

        private static Sv5BoundaryAuthoringSource BoundarySource(MoonpalaceBiomePair pair)
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

        private static Sv5BoundaryAuthoringSource Source(
            MoonpalaceBiomePair pair,
            string typeName,
            string ruleId,
            IEnumerable<string> candidates,
            IEnumerable<string> profiles,
            string horizontal,
            string vertical) => new Sv5BoundaryAuthoringSource(pair, typeName, ruleId, candidates,
            profiles, horizontal, vertical);

        private static MoonpalaceBiomeId BiomeFor(Sv5WorldGraphRole role)
        {
            switch (role)
            {
                case Sv5WorldGraphRole.Start:
                case Sv5WorldGraphRole.MooncoreOre:
                case Sv5WorldGraphRole.Boss: return MoonpalaceBiomeId.MoonCrater;
                case Sv5WorldGraphRole.CondensedCoefficientSap:
                case Sv5WorldGraphRole.Exit: return MoonpalaceBiomeId.CassiaRoot;
                case Sv5WorldGraphRole.DeepStarYeast:
                case Sv5WorldGraphRole.Seal: return MoonpalaceBiomeId.MoonDough;
                case Sv5WorldGraphRole.Forge: return MoonpalaceBiomeId.AbandonedMill;
                default: throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        private static int Weight(MoonpalaceBiomeId biome, Sv5BiomeDensityProfileId profile)
        {
            switch (biome.CanonicalId)
            {
                case "MoonCrater": return profile == Sv5BiomeDensityProfileId.Open ? 4 :
                    profile == Sv5BiomeDensityProfileId.Balanced ? 4 : 2;
                case "CassiaRoot": return profile == Sv5BiomeDensityProfileId.Open ? 3 :
                    profile == Sv5BiomeDensityProfileId.Balanced ? 4 : 3;
                case "AbandonedMill": return profile == Sv5BiomeDensityProfileId.Open ? 2 :
                    profile == Sv5BiomeDensityProfileId.Balanced ? 4 : 4;
                case "MoonDough": return profile == Sv5BiomeDensityProfileId.Open ? 4 :
                    profile == Sv5BiomeDensityProfileId.Balanced ? 4 : 2;
                default: throw new ArgumentOutOfRangeException(nameof(biome));
            }
        }

        private static string BlockKey(int minX, int minY) => minX.ToString(CultureInfo.InvariantCulture) + "," +
            minY.ToString(CultureInfo.InvariantCulture);
        private static string Hash(params string[] values) => Sv5WorldDefinition.Hash(string.Join("\n", values));
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

    public static class Sv5WorldBiomeExport
    {
        public static string ManifestJson(Sv5WorldBiomePlan plan)
        {
            Require(plan);
            return "{\n" + string.Join(",\n", new[]
            {
                Property("schema", "SV5_BIOME_MANIFEST_V1"),
                Property("seed", plan.Definition.Request.Seed.ToString(CultureInfo.InvariantCulture), false),
                Property("content_version", plan.Definition.Request.ContentVersion),
                Property("generator_version", plan.Definition.Request.GeneratorVersion),
                Property("pool_version", plan.Definition.PoolVersion),
                Property("pool_digest", plan.Definition.PoolDigest),
                Property("world_definition_digest", plan.Definition.Digest),
                Property("world_graph_digest", plan.Graph.Digest),
                Property("plan_digest", plan.Digest),
                Property("profile_rng_stream", WorldGenerationRngStreams.BiomePatchStreamId),
                Property("profile_rng_scope", Sv5WorldBiomePlanner.ProfileSelectionScope),
                Property("profile_rng_initial_state", plan.ProfileRngInitialState.ToString("x16", CultureInfo.InvariantCulture)),
                Property("world_tile_bounds", "624x416"),
                Property("microchunk_grid", "52x52"),
                Property("microchunk_size_tiles", "12x8"),
                Property("ownership_count", plan.Cells.Count.ToString(CultureInfo.InvariantCulture), false),
                Property("patch_count", plan.Patches.Count.ToString(CultureInfo.InvariantCulture), false),
                Property("boundary_count", plan.Boundaries.Count.ToString(CultureInfo.InvariantCulture), false),
                Property("measured_density", "PENDING_GEOMETRY"),
                Property("physical_bake", "NOT_SV514"),
                Property("special_footprints", "NOT_PLACED_SV515"),
            }) + "\n}\n";
        }

        public static string PatchOwnershipCsv(Sv5WorldBiomePlan plan) => Lines(
            "micro_x,micro_y,tile_min_x,tile_min_y,patch_id,biome_key,owner_state", plan.Cells.OrderBy(value => value)
                .Select(value => Row(value.X, value.Y, value.X * Sv5WorldBiomePlanner.MicroChunkWidthTiles,
                    value.Y * Sv5WorldBiomePlanner.MicroChunkHeightTiles, value.PatchId,
                    value.Biome.CanonicalId, value.OwnershipState)));

        public static string PatchesCsv(Sv5WorldBiomePlan plan) => Lines(
            "patch_id,biome_key,biome_display_name,micro_min_x,micro_min_y,micro_max_x,micro_max_y,tile_min_x,tile_min_y,tile_max_x,tile_max_y,area_microchunks,density_profile,owner_state,density_metric_status,density_metric_scope", plan.Patches.OrderBy(value => value)
                .Select(value => Row(value.PatchId, value.BiomeKey, value.BiomeDisplayName, value.MinMicroX,
                    value.MinMicroY, value.MaxMicroX, value.MaxMicroY, value.MinTileX, value.MinTileY,
                    value.MaxTileX, value.MaxTileY, value.AreaMicroChunks, value.DensityProfile,
                    value.OwnershipState, value.DensityMetricStatus, value.DensityMetricScopeId)));

        public static string BoundariesCsv(Sv5WorldBiomePlan plan) => Lines(
            "boundary_id,source_micro_x,source_micro_y,target_micro_x,target_micro_y,direction,source_patch_id,target_patch_id,canonical_pair,orientation,source_contract,pair_rule_id,edge_signature_id,authoring_candidate_ids,authoring_profile_ids,tool_requirement,mandatory_route_allowed", plan.Boundaries.OrderBy(value => value)
                .Select(value => Row(value.BoundaryId, value.Source.X, value.Source.Y, value.Target.X,
                    value.Target.Y, value.Direction, value.Source.PatchId, value.Target.PatchId,
                    value.Pair.PairId, value.Orientation, value.SourceContract.SourceTypeName,
                    value.SourceContract.PairRuleId, value.EdgeSignatureId,
                    string.Join("|", value.SourceContract.CandidateIds),
                    string.Join("|", value.SourceContract.ProfileIds),
                    value.SourceContract.PairDefinition.MandatoryToolRequirement,
                    value.SourceContract.PairDefinition.MandatoryRouteAllowed)));

        public static string ProfilesCsv(Sv5WorldBiomePlan plan) => Lines(
            "patch_id,biome_key,selected_profile,open_weight,balanced_weight,dense_weight,target_min_permille,target_max_permille,metric_scope,measured_density", plan.Patches.OrderBy(value => value)
                .Select(value =>
                {
                    Sv5BiomeDensityProfileDefinition profile = Sv5WorldBiomePlanner.ProfileCatalog[value.DensityProfile];
                    return Row(value.PatchId, value.BiomeKey, value.DensityProfile, value.OpenWeight,
                        value.BalancedWeight, value.DenseWeight, profile.TargetMinimumPermille,
                        profile.TargetMaximumPermille, value.DensityMetricScopeId, value.DensityMetricStatus);
                }));

        public static string ReservationInputsCsv(Sv5WorldBiomePlan plan) => Lines(
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
        private static void Require(Sv5WorldBiomePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
        }
    }
}
