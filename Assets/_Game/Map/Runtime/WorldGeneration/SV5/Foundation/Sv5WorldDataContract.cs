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
using StarNight.Map.WorldGeneration.SV5.Foundation;

namespace StarNight.Map.WorldGeneration.WorldData
{
    public enum Sv5WorldRngStream
    {
        RunGraph,
        Biome,
        SpecialReservation,
        Port,
        Pattern,
        Overlay,
        Population
    }

    public enum Sv5WorldStableIdKind
    {
        MicroChunk,
        BreakableTile,
        Mechanism,
        RewardChest,
        SpecialRegionTrigger,
        MonsterSpawnSlot,
        NPCShopSlot
    }

    /// <summary>All inputs which can change a deterministic SV5 definition.
    /// Width, height and recipe deliberately remain explicit because the current
    /// executable SV5 generator consumes those values directly.</summary>
    public sealed class Sv5WorldDataRequest
    {
        public Sv5WorldDataRequest(ulong seed, string contentVersion, string generatorVersion)
            : this(seed, contentVersion, generatorVersion, Sv5SmallRunHarness.MinimumWidth,
                Sv5SmallRunHarness.MinimumHeight, Sv5SmallRunRecipe.PortGalleryV1)
        {
        }

        public Sv5WorldDataRequest(
            ulong seed,
            string contentVersion,
            string generatorVersion,
            int runWidth,
            int runHeight,
            Sv5SmallRunRecipe recipe,
            int attemptOrdinal = 0)
        {
            if (seed > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(seed),
                    "SV5 currently accepts a non-negative Int32 seed.");
            if (runWidth <= 0 || runHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(runWidth));
            if (!Enum.IsDefined(typeof(Sv5SmallRunRecipe), recipe))
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
        public Sv5SmallRunRecipe Recipe { get; }
        public string RecipeId => Recipe.ToString();
        public int AttemptOrdinal { get; }

        public Sv5SmallRunRequest ToSmallRunRequest() => new Sv5SmallRunRequest(
            checked((int)Seed), RunWidth, RunHeight, Recipe);

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("World-data version is required.", name);
            return value.Trim();
        }
    }

    /// <summary>Names the existing RNG stream and reset scope used for one
    /// SV5 responsibility. No RNG implementation or mutable global state is
    /// introduced here.</summary>
    public sealed class Sv5WorldRngBinding : IComparable<Sv5WorldRngBinding>
    {
        internal Sv5WorldRngBinding(
            Sv5WorldRngStream stream,
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
                "SV5_RNG_BINDING_V2", Stream.ToString(), SourceStreamId,
                ScopeIdentity, attemptOrdinal.ToString(CultureInfo.InvariantCulture), InitialStateHex,
            });
        }

        public Sv5WorldRngStream Stream { get; }
        public string SourceStreamId { get; }
        public string ScopeIdentity { get; }
        public int AttemptOrdinal { get; }
        public ulong InitialState { get; }
        public string InitialStateHex { get; }
        public string StableToken { get; }
        public int CompareTo(Sv5WorldRngBinding other) => other == null
            ? 1 : Stream.CompareTo(other.Stream);

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("RNG source is required.", name);
            return value.Trim();
        }
    }

    /// <summary>Target identity is based on stable generated owner and slot keys,
    /// never enumeration order or a Unity instance ID.</summary>
    public sealed class Sv5WorldStableId : IComparable<Sv5WorldStableId>
    {
        public Sv5WorldStableId(
            Sv5WorldStableIdKind kind,
            string ownerIdentity,
            string slotIdentity)
        {
            Kind = kind;
            OwnerIdentity = Require(ownerIdentity, nameof(ownerIdentity));
            SlotIdentity = Require(slotIdentity, nameof(slotIdentity));
            StableToken = string.Join("|", new[]
            {
                "SV5_STABLE_ID_V2", Kind.ToString(), OwnerIdentity, SlotIdentity,
            });
            Value = Sv5WorldDefinition.Hash(StableToken);
        }

        public Sv5WorldStableIdKind Kind { get; }
        public string OwnerIdentity { get; }
        public string SlotIdentity { get; }
        public string StableToken { get; }
        public string Value { get; }
        public int CompareTo(Sv5WorldStableId other) => other == null
            ? 1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public override string ToString() => Value;

        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Stable-ID identity is required.", name);
            return value.Trim();
        }
    }

    public sealed class Sv5WorldDefinition
    {
        internal Sv5WorldDefinition(
            Sv5WorldDataRequest request,
            string poolVersion,
            string poolDigest,
            Sv5SmallRunPlan actualRunPlan,
            IEnumerable<Sv5WorldRngBinding> sourceBindings,
            IEnumerable<Sv5WorldStableId> sourceIds)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            PoolVersion = poolVersion ?? string.Empty;
            PoolDigest = poolDigest ?? string.Empty;
            ActualRunPlan = actualRunPlan ?? throw new ArgumentNullException(nameof(actualRunPlan));
            ActualRunPlanDigest = actualRunPlan.PlanDigest ?? string.Empty;

            var bindings = (sourceBindings ?? Array.Empty<Sv5WorldRngBinding>())
                .Where(value => value != null).OrderBy(value => value).ToArray();
            if (bindings.Length != Enum.GetValues(typeof(Sv5WorldRngStream)).Length ||
                bindings.Select(value => value.Stream).Distinct().Count() != bindings.Length)
                throw new ArgumentException("Each SV5 stream needs exactly one binding.", nameof(sourceBindings));
            RngBindings = new ReadOnlyDictionary<Sv5WorldRngStream, Sv5WorldRngBinding>(
                bindings.ToDictionary(value => value.Stream, value => value));
            StreamSeeds = new ReadOnlyDictionary<Sv5WorldRngStream, string>(
                bindings.ToDictionary(value => value.Stream, value => value.InitialStateHex));

            StableIds = new ReadOnlyCollection<Sv5WorldStableId>((sourceIds ??
                Array.Empty<Sv5WorldStableId>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            if (StableIds.Select(value => value.Value).Distinct(StringComparer.Ordinal).Count() != StableIds.Count)
                throw new ArgumentException("Stable IDs must be unique.", nameof(sourceIds));

            Digest = Hash(string.Join("\n", new[]
            {
                "SV5_WORLD_DEFINITION_V2",
                request.Seed.ToString(CultureInfo.InvariantCulture), request.ContentVersion,
                request.GeneratorVersion, request.RunWidth + "x" + request.RunHeight,
                request.RecipeId, request.AttemptOrdinal.ToString(CultureInfo.InvariantCulture),
                PoolVersion, PoolDigest, ActualRunPlanDigest,
            }.Concat(bindings.Select(value => value.StableToken))
                .Concat(StableIds.Select(value => value.StableToken))));
        }

        public Sv5WorldDataRequest Request { get; }
        public string PoolVersion { get; }
        public string PoolDigest { get; }
        public Sv5SmallRunPlan ActualRunPlan { get; }
        public string ActualRunPlanDigest { get; }
        public IReadOnlyDictionary<Sv5WorldRngStream, Sv5WorldRngBinding> RngBindings { get; }
        public IReadOnlyDictionary<Sv5WorldRngStream, string> StreamSeeds { get; }
        public IReadOnlyList<Sv5WorldStableId> StableIds { get; }
        public string Digest { get; }

        public IReadOnlyList<Sv5WorldStableId> GetStableIds(Sv5WorldStableIdKind kind) =>
            new ReadOnlyCollection<Sv5WorldStableId>(StableIds.Where(value => value.Kind == kind).ToArray());

        public Sv5WorldStableId FindStableId(
            Sv5WorldStableIdKind kind,
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

    /// <summary>Mutable values are keyed by the SV5 stable ID. The existing
    /// generated Save Manifest remains the serializer and physical mutation owner;
    /// this state restores only records whose saved slot reference is one of these
    /// deferred world-data identities.</summary>
    public sealed class Sv5WorldMutationState
    {
        private readonly SortedDictionary<string, string> values =
            new SortedDictionary<string, string>(StringComparer.Ordinal);

        public void Apply(Sv5WorldStableId id, string state)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            values[id.Value] = state ?? string.Empty;
        }

        public bool TryGetState(Sv5WorldStableId id, out string state)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            return values.TryGetValue(id.Value, out state);
        }

        public IReadOnlyDictionary<string, string> Snapshot =>
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(values));

        public static Sv5WorldMutationState RestoreFromSaveManifest(
            GeneratedWorldSaveManifest manifest,
            Sv5WorldDefinition definition)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var known = new HashSet<string>(definition.StableIds.Select(value => value.Value),
                StringComparer.Ordinal);
            var result = new Sv5WorldMutationState();
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

    public static class Sv5WorldDataGenerator
    {
        public static Sv5WorldDefinition Generate(
            Sv5WorldDataRequest request,
            StaticDataRegistry staticData) => Generate(request,
                new WorldGenerationRngStreams(staticData ?? throw new ArgumentNullException(nameof(staticData))));

        /// <summary>Adapter between the injected production RNG authority and the
        /// current executable SV5 small-run request/result. It does not create a
        /// player, camera, scene, clock, or mutable static world state.</summary>
        public static Sv5WorldDefinition Generate(
            Sv5WorldDataRequest request,
            WorldGenerationRngStreams rngStreams)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));

            Sv5SmallRunPlan runPlan = Sv5SmallRunHarness.Generate(request.ToSmallRunRequest());
            if (!runPlan.Success)
                throw new InvalidOperationException("SV5 run request was rejected: " + runPlan.FailureSummary);
            Sv5PatternPool500Snapshot pool = Sv5PatternPool500.BuildFinalPool();
            var bindings = Enum.GetValues(typeof(Sv5WorldRngStream)).Cast<Sv5WorldRngStream>()
                .Select(stream => CreateBinding(request, rngStreams, stream, request.AttemptOrdinal)).ToArray();
            return new Sv5WorldDefinition(request, Sv5PatternPool500.DataVersion, pool.Digest,
                runPlan, bindings, BuildStableIds(runPlan));
        }

        public static DeterministicRngStream CreateStream(
            Sv5WorldDataRequest request,
            WorldGenerationRngStreams rngStreams,
            Sv5WorldRngStream stream,
            int? attemptOrdinal = null)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            int attempt = attemptOrdinal ?? request.AttemptOrdinal;
            if (attempt < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            switch (stream)
            {
                case Sv5WorldRngStream.RunGraph:
                    return rngStreams.CreateRoute(request.Seed, "SV5_RUN_GRAPH", attempt);
                case Sv5WorldRngStream.Biome:
                    return rngStreams.CreateBiomePatch(request.Seed, "SV5_BIOME", attempt);
                case Sv5WorldRngStream.SpecialReservation:
                    return rngStreams.CreateWorldSite(request.Seed, attempt);
                case Sv5WorldRngStream.Port:
                    return rngStreams.CreateType0(request.Seed, "SV5_PORT", attempt);
                case Sv5WorldRngStream.Pattern:
                    return rngStreams.CreateSectorRecipe(request.Seed, new SectorCoord(0, 0), attempt);
                case Sv5WorldRngStream.Overlay:
                    return rngStreams.CreatePopulation(request.Seed, "SV5_OVERLAY", attempt);
                case Sv5WorldRngStream.Population:
                    return rngStreams.CreatePopulation(request.Seed, "SV5_POPULATION", attempt);
                default: throw new ArgumentOutOfRangeException(nameof(stream));
            }
        }

        private static Sv5WorldRngBinding CreateBinding(
            Sv5WorldDataRequest request,
            WorldGenerationRngStreams streams,
            Sv5WorldRngStream stream,
            int attempt)
        {
            DeterministicRngStream actual = CreateStream(request, streams, stream, attempt);
            switch (stream)
            {
                case Sv5WorldRngStream.RunGraph:
                    return new Sv5WorldRngBinding(stream, WorldGenerationRngStreams.RouteStreamId,
                        "SV5_RUN_GRAPH", attempt, actual.InitialState);
                case Sv5WorldRngStream.Biome:
                    return new Sv5WorldRngBinding(stream, WorldGenerationRngStreams.BiomePatchStreamId,
                        "SV5_BIOME", attempt, actual.InitialState);
                case Sv5WorldRngStream.SpecialReservation:
                    return new Sv5WorldRngBinding(stream, WorldGenerationRngStreams.WorldSiteStreamId,
                        "WORLD", attempt, actual.InitialState);
                case Sv5WorldRngStream.Port:
                    return new Sv5WorldRngBinding(stream, WorldGenerationRngStreams.Type0StreamId,
                        "SV5_PORT", attempt, actual.InitialState);
                case Sv5WorldRngStream.Pattern:
                    return new Sv5WorldRngBinding(stream, WorldGenerationRngStreams.SectorRecipeStreamId,
                        "0,0", attempt, actual.InitialState);
                case Sv5WorldRngStream.Overlay:
                    return new Sv5WorldRngBinding(stream, WorldGenerationRngStreams.PopulationStreamId,
                        "SV5_OVERLAY", attempt, actual.InitialState);
                case Sv5WorldRngStream.Population:
                    return new Sv5WorldRngBinding(stream, WorldGenerationRngStreams.PopulationStreamId,
                        "SV5_POPULATION", attempt, actual.InitialState);
                default: throw new ArgumentOutOfRangeException(nameof(stream));
            }
        }

        private static IEnumerable<Sv5WorldStableId> BuildStableIds(Sv5SmallRunPlan runPlan)
        {
            var chunks = runPlan.Chunks.OrderBy(value => value.InstanceId, StringComparer.Ordinal).ToArray();
            foreach (var chunk in chunks)
                yield return new Sv5WorldStableId(Sv5WorldStableIdKind.MicroChunk,
                    OwnerIdentity(chunk), "MICROCHUNK");

            foreach (var kind in Enum.GetValues(typeof(Sv5WorldStableIdKind))
                         .Cast<Sv5WorldStableIdKind>().Where(value => value != Sv5WorldStableIdKind.MicroChunk))
            foreach (var chunk in chunks.Take(2))
                yield return new Sv5WorldStableId(kind, OwnerIdentity(chunk),
                    "DEFERRED_SLOT:" + kind + ":0");
        }

        private static string OwnerIdentity(Sv5SmallRunChunk chunk) => string.Format(
            CultureInfo.InvariantCulture, "SV5_CHUNK|{0}|{1}|{2}",
            chunk.InstanceId, chunk.OriginX, chunk.OriginY);
    }
}
