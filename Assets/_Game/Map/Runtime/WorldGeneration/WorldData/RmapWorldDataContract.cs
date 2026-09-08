using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;

namespace StarNight.Map.WorldGeneration.WorldData
{
    public enum RmapWorldRngStream
    {
        RunGraph,
        Biome,
        SpecialReservation,
        Port,
        Pattern,
        Overlay,
        Population
    }

    public enum RmapWorldStableIdKind
    {
        MicroChunk,
        BreakableTile,
        Mechanism,
        RewardChest,
        SpecialRegionTrigger,
        MonsterSpawnSlot,
        NPCShopSlot
    }

    /// <summary>All inputs which can change a deterministic RMAP12 definition.
    /// Width, height and recipe deliberately remain explicit because the current
    /// executable RMAP10 generator consumes those values directly.</summary>
    public sealed class RmapWorldDataRequest
    {
        public RmapWorldDataRequest(ulong seed, string contentVersion, string generatorVersion)
            : this(seed, contentVersion, generatorVersion, RmapSmallRunHarness.MinimumWidth,
                RmapSmallRunHarness.MinimumHeight, RmapSmallRunRecipe.PortGalleryV1)
        {
        }

        public RmapWorldDataRequest(
            ulong seed,
            string contentVersion,
            string generatorVersion,
            int runWidth,
            int runHeight,
            RmapSmallRunRecipe recipe,
            int attemptOrdinal = 0)
        {
            if (seed > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(seed),
                    "RMAP10 currently accepts a non-negative Int32 seed.");
            if (runWidth <= 0 || runHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(runWidth));
            if (!Enum.IsDefined(typeof(RmapSmallRunRecipe), recipe))
                throw new ArgumentOutOfRangeException(nameof(recipe));
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));

            Seed = seed;
            ContentVersion = Require(contentVersion, nameof(contentVersion));
            GeneratorVersion = Require(generatorVersion, nameof(generatorVersion));
            RunWidth = runWidth;
            RunHeight = runHeight;
            Recipe = recipe;
            AttemptOrdinal = attemptOrdinal;
        }

        public ulong Seed { get; }
        public string ContentVersion { get; }
        public string GeneratorVersion { get; }
        public int RunWidth { get; }
        public int RunHeight { get; }
        public RmapSmallRunRecipe Recipe { get; }
        public string RecipeId => Recipe.ToString();
        public int AttemptOrdinal { get; }

        public RmapSmallRunRequest ToSmallRunRequest() => new RmapSmallRunRequest(
            checked((int)Seed), RunWidth, RunHeight, Recipe);

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("World-data version is required.", name);
            return value.Trim();
        }
    }

    /// <summary>Names the existing RNG stream and reset scope used for one
    /// RMAP12 responsibility. No RNG implementation or mutable global state is
    /// introduced here.</summary>
    public sealed class RmapWorldRngBinding : IComparable<RmapWorldRngBinding>
    {
        internal RmapWorldRngBinding(
            RmapWorldRngStream stream,
            string sourceStreamId,
            string scopeIdentity,
            int attemptOrdinal,
            ulong initialState)
        {
            Stream = stream;
            SourceStreamId = Require(sourceStreamId, nameof(sourceStreamId));
            ScopeIdentity = scopeIdentity ?? throw new ArgumentNullException(nameof(scopeIdentity));
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            AttemptOrdinal = attemptOrdinal;
            InitialState = initialState;
            InitialStateHex = initialState.ToString("x16", CultureInfo.InvariantCulture);
            StableToken = string.Join("|", new[]
            {
                "RMAP12_RNG_BINDING_V2", Stream.ToString(), SourceStreamId,
                ScopeIdentity, attemptOrdinal.ToString(CultureInfo.InvariantCulture), InitialStateHex,
            });
        }

        public RmapWorldRngStream Stream { get; }
        public string SourceStreamId { get; }
        public string ScopeIdentity { get; }
        public int AttemptOrdinal { get; }
        public ulong InitialState { get; }
        public string InitialStateHex { get; }
        public string StableToken { get; }
        public int CompareTo(RmapWorldRngBinding other) => other == null
            ? 1 : Stream.CompareTo(other.Stream);

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("RNG source is required.", name);
            return value.Trim();
        }
    }

    /// <summary>Target identity is based on stable generated owner and slot keys,
    /// never enumeration order or a Unity instance ID.</summary>
    public sealed class RmapWorldStableId : IComparable<RmapWorldStableId>
    {
        public RmapWorldStableId(
            RmapWorldStableIdKind kind,
            string ownerIdentity,
            string slotIdentity)
        {
            Kind = kind;
            OwnerIdentity = Require(ownerIdentity, nameof(ownerIdentity));
            SlotIdentity = Require(slotIdentity, nameof(slotIdentity));
            StableToken = string.Join("|", new[]
            {
                "RMAP12_STABLE_ID_V2", Kind.ToString(), OwnerIdentity, SlotIdentity,
            });
            Value = RmapWorldDefinition.Hash(StableToken);
        }

        public RmapWorldStableIdKind Kind { get; }
        public string OwnerIdentity { get; }
        public string SlotIdentity { get; }
        public string StableToken { get; }
        public string Value { get; }
        public int CompareTo(RmapWorldStableId other) => other == null
            ? 1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public override string ToString() => Value;

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Stable-ID identity is required.", name);
            return value.Trim();
        }
    }

    public sealed class RmapWorldDefinition
    {
        internal RmapWorldDefinition(
            RmapWorldDataRequest request,
            string poolVersion,
            string poolDigest,
            RmapSmallRunPlan actualRunPlan,
            IEnumerable<RmapWorldRngBinding> sourceBindings,
            IEnumerable<RmapWorldStableId> sourceIds)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            PoolVersion = poolVersion ?? string.Empty;
            PoolDigest = poolDigest ?? string.Empty;
            ActualRunPlan = actualRunPlan ?? throw new ArgumentNullException(nameof(actualRunPlan));
            ActualRunPlanDigest = actualRunPlan.PlanDigest ?? string.Empty;

            var bindings = (sourceBindings ?? Array.Empty<RmapWorldRngBinding>())
                .Where(value => value != null).OrderBy(value => value).ToArray();
            if (bindings.Length != Enum.GetValues(typeof(RmapWorldRngStream)).Length ||
                bindings.Select(value => value.Stream).Distinct().Count() != bindings.Length)
                throw new ArgumentException("Each RMAP12 stream needs exactly one binding.", nameof(sourceBindings));
            RngBindings = new ReadOnlyDictionary<RmapWorldRngStream, RmapWorldRngBinding>(
                bindings.ToDictionary(value => value.Stream, value => value));
            StreamSeeds = new ReadOnlyDictionary<RmapWorldRngStream, string>(
                bindings.ToDictionary(value => value.Stream, value => value.InitialStateHex));

            StableIds = new ReadOnlyCollection<RmapWorldStableId>((sourceIds ??
                Array.Empty<RmapWorldStableId>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            if (StableIds.Select(value => value.Value).Distinct(StringComparer.Ordinal).Count() != StableIds.Count)
                throw new ArgumentException("Stable IDs must be unique.", nameof(sourceIds));

            Digest = Hash(string.Join("\n", new[]
            {
                "RMAP12_WORLD_DEFINITION_V2",
                request.Seed.ToString(CultureInfo.InvariantCulture), request.ContentVersion,
                request.GeneratorVersion, request.RunWidth + "x" + request.RunHeight,
                request.RecipeId, request.AttemptOrdinal.ToString(CultureInfo.InvariantCulture),
                PoolVersion, PoolDigest, ActualRunPlanDigest,
            }.Concat(bindings.Select(value => value.StableToken))
                .Concat(StableIds.Select(value => value.StableToken))));
        }

        public RmapWorldDataRequest Request { get; }
        public string PoolVersion { get; }
        public string PoolDigest { get; }
        public RmapSmallRunPlan ActualRunPlan { get; }
        public string ActualRunPlanDigest { get; }
        public IReadOnlyDictionary<RmapWorldRngStream, RmapWorldRngBinding> RngBindings { get; }
        public IReadOnlyDictionary<RmapWorldRngStream, string> StreamSeeds { get; }
        public IReadOnlyList<RmapWorldStableId> StableIds { get; }
        public string Digest { get; }

        public IReadOnlyList<RmapWorldStableId> GetStableIds(RmapWorldStableIdKind kind) =>
            new ReadOnlyCollection<RmapWorldStableId>(StableIds.Where(value => value.Kind == kind).ToArray());

        public RmapWorldStableId FindStableId(
            RmapWorldStableIdKind kind,
            string ownerIdentity,
            string slotIdentity) => StableIds.SingleOrDefault(value => value.Kind == kind &&
                string.Equals(value.OwnerIdentity, ownerIdentity, StringComparison.Ordinal) &&
                string.Equals(value.SlotIdentity, slotIdentity, StringComparison.Ordinal));

        internal static string Hash(string text)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }

    /// <summary>Mutable values are keyed by the RMAP12 stable ID. The existing
    /// generated Save Manifest remains the serializer and physical mutation owner;
    /// this state restores only records whose saved slot reference is one of these
    /// deferred world-data identities.</summary>
    public sealed class RmapWorldMutationState
    {
        private readonly SortedDictionary<string, string> values =
            new SortedDictionary<string, string>(StringComparer.Ordinal);

        public void Apply(RmapWorldStableId id, string state)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            values[id.Value] = state ?? string.Empty;
        }

        public bool TryGetState(RmapWorldStableId id, out string state)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            return values.TryGetValue(id.Value, out state);
        }

        public IReadOnlyDictionary<string, string> Snapshot =>
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(values));

        public static RmapWorldMutationState RestoreFromSaveManifest(
            GeneratedWorldSaveManifest manifest,
            RmapWorldDefinition definition)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var known = new HashSet<string>(definition.StableIds.Select(value => value.Value),
                StringComparer.Ordinal);
            var result = new RmapWorldMutationState();
            foreach (var record in manifest.ModifiedSectorEntries.SelectMany(value => value.Records)
                         .OrderBy(value => value))
            {
                if (known.Contains(record.SlotReference))
                    result.values[record.SlotReference] = StateValue(record);
            }
            return result;
        }

        private static string StateValue(GeneratedSaveManifestRecordPayload record)
        {
            switch (record.Kind)
            {
                case GeneratedSectorModificationKind.DestroyTile: return "DESTROYED";
                case GeneratedSectorModificationKind.ReplaceTile: return "REPLACED:" + record.NewTileCode;
                case GeneratedSectorModificationKind.CollectPickup: return "COLLECTED";
                case GeneratedSectorModificationKind.ChangeDeviceState:
                    return record.StateKey + "=" + record.StateValue;
                case GeneratedSectorModificationKind.ConsumeSlot: return "CONSUMED";
                default: throw new ArgumentOutOfRangeException(nameof(record));
            }
        }
    }

    public static class RmapWorldDataGenerator
    {
        public static RmapWorldDefinition Generate(
            RmapWorldDataRequest request,
            StaticDataRegistry staticData) => Generate(request,
                new WorldGenerationRngStreams(staticData ?? throw new ArgumentNullException(nameof(staticData))));

        /// <summary>Adapter between the injected production RNG authority and the
        /// current executable RMAP10 small-run request/result. It does not create a
        /// player, camera, scene, clock, or mutable static world state.</summary>
        public static RmapWorldDefinition Generate(
            RmapWorldDataRequest request,
            WorldGenerationRngStreams rngStreams)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));

            RmapSmallRunPlan runPlan = RmapSmallRunHarness.Generate(request.ToSmallRunRequest());
            if (!runPlan.Success)
                throw new InvalidOperationException("RMAP10 run request was rejected: " + runPlan.FailureSummary);
            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();
            var bindings = Enum.GetValues(typeof(RmapWorldRngStream)).Cast<RmapWorldRngStream>()
                .Select(stream => CreateBinding(request, rngStreams, stream, request.AttemptOrdinal)).ToArray();
            return new RmapWorldDefinition(request, RmapPatternPool500.DataVersion, pool.Digest,
                runPlan, bindings, BuildStableIds(runPlan));
        }

        public static DeterministicRngStream CreateStream(
            RmapWorldDataRequest request,
            WorldGenerationRngStreams rngStreams,
            RmapWorldRngStream stream,
            int? attemptOrdinal = null)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            int attempt = attemptOrdinal ?? request.AttemptOrdinal;
            if (attempt < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            switch (stream)
            {
                case RmapWorldRngStream.RunGraph:
                    return rngStreams.CreateRoute(request.Seed, "RMAP12_RUN_GRAPH", attempt);
                case RmapWorldRngStream.Biome:
                    return rngStreams.CreateBiomePatch(request.Seed, "RMAP12_BIOME", attempt);
                case RmapWorldRngStream.SpecialReservation:
                    return rngStreams.CreateWorldSite(request.Seed, attempt);
                case RmapWorldRngStream.Port:
                    return rngStreams.CreateType0(request.Seed, "RMAP12_PORT", attempt);
                case RmapWorldRngStream.Pattern:
                    return rngStreams.CreateSectorRecipe(request.Seed, new SectorCoord(0, 0), attempt);
                case RmapWorldRngStream.Overlay:
                    return rngStreams.CreatePopulation(request.Seed, "RMAP12_OVERLAY", attempt);
                case RmapWorldRngStream.Population:
                    return rngStreams.CreatePopulation(request.Seed, "RMAP12_POPULATION", attempt);
                default: throw new ArgumentOutOfRangeException(nameof(stream));
            }
        }

        private static RmapWorldRngBinding CreateBinding(
            RmapWorldDataRequest request,
            WorldGenerationRngStreams streams,
            RmapWorldRngStream stream,
            int attempt)
        {
            DeterministicRngStream actual = CreateStream(request, streams, stream, attempt);
            switch (stream)
            {
                case RmapWorldRngStream.RunGraph:
                    return new RmapWorldRngBinding(stream, WorldGenerationRngStreams.RouteStreamId,
                        "RMAP12_RUN_GRAPH", attempt, actual.InitialState);
                case RmapWorldRngStream.Biome:
                    return new RmapWorldRngBinding(stream, WorldGenerationRngStreams.BiomePatchStreamId,
                        "RMAP12_BIOME", attempt, actual.InitialState);
                case RmapWorldRngStream.SpecialReservation:
                    return new RmapWorldRngBinding(stream, WorldGenerationRngStreams.WorldSiteStreamId,
                        "WORLD", attempt, actual.InitialState);
                case RmapWorldRngStream.Port:
                    return new RmapWorldRngBinding(stream, WorldGenerationRngStreams.Type0StreamId,
                        "RMAP12_PORT", attempt, actual.InitialState);
                case RmapWorldRngStream.Pattern:
                    return new RmapWorldRngBinding(stream, WorldGenerationRngStreams.SectorRecipeStreamId,
                        "0,0", attempt, actual.InitialState);
                case RmapWorldRngStream.Overlay:
                    return new RmapWorldRngBinding(stream, WorldGenerationRngStreams.PopulationStreamId,
                        "RMAP12_OVERLAY", attempt, actual.InitialState);
                case RmapWorldRngStream.Population:
                    return new RmapWorldRngBinding(stream, WorldGenerationRngStreams.PopulationStreamId,
                        "RMAP12_POPULATION", attempt, actual.InitialState);
                default: throw new ArgumentOutOfRangeException(nameof(stream));
            }
        }

        private static IEnumerable<RmapWorldStableId> BuildStableIds(RmapSmallRunPlan runPlan)
        {
            var chunks = runPlan.Chunks.OrderBy(value => value.InstanceId, StringComparer.Ordinal).ToArray();
            foreach (var chunk in chunks)
                yield return new RmapWorldStableId(RmapWorldStableIdKind.MicroChunk,
                    OwnerIdentity(chunk), "MICROCHUNK");

            foreach (var kind in Enum.GetValues(typeof(RmapWorldStableIdKind))
                         .Cast<RmapWorldStableIdKind>().Where(value => value != RmapWorldStableIdKind.MicroChunk))
            foreach (var chunk in chunks.Take(2))
                yield return new RmapWorldStableId(kind, OwnerIdentity(chunk),
                    "DEFERRED_SLOT:" + kind + ":0");
        }

        private static string OwnerIdentity(RmapSmallRunChunk chunk) => string.Format(
            CultureInfo.InvariantCulture, "RMAP10_CHUNK|{0}|{1}|{2}",
            chunk.InstanceId, chunk.OriginX, chunk.OriginY);
    }
}
