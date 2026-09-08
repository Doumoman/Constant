using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.WorldGeneration.WorldData
{
    public enum RmapWorldRngStream { RunGraph, Biome, SpecialReservation, Port, Pattern, Overlay, Population }
    public enum RmapWorldStableIdKind { MicroChunk, BreakableTile, Mechanism, RewardChest, SpecialRegionTrigger, MonsterSpawnSlot, NPCShopSlot }

    public sealed class RmapWorldDataRequest
    {
        public RmapWorldDataRequest(ulong seed, string contentVersion, string generatorVersion)
        {
            Seed = seed;
            ContentVersion = Require(contentVersion, nameof(contentVersion));
            GeneratorVersion = Require(generatorVersion, nameof(generatorVersion));
        }
        public ulong Seed { get; }
        public string ContentVersion { get; }
        public string GeneratorVersion { get; }
        private static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("World-data version is required.", name);
            return value.Trim();
        }
    }

    public sealed class RmapWorldStableId : IComparable<RmapWorldStableId>
    {
        public RmapWorldStableId(RmapWorldStableIdKind kind, string scope, int ordinal)
        {
            if (string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("Stable-ID scope is required.", nameof(scope));
            if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal));
            Kind = kind; Scope = scope.Trim(); Ordinal = ordinal;
            Value = "RMAP12|" + kind + "|" + Scope + "|" + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        public RmapWorldStableIdKind Kind { get; }
        public string Scope { get; }
        public int Ordinal { get; }
        public string Value { get; }
        public int CompareTo(RmapWorldStableId other) => other == null ? 1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public override string ToString() => Value;
    }

    public sealed class RmapWorldDefinition
    {
        internal RmapWorldDefinition(RmapWorldDataRequest request, string poolVersion, string poolDigest,
            IDictionary<RmapWorldRngStream, string> streams, IEnumerable<RmapWorldStableId> ids)
        {
            Request = request; PoolVersion = poolVersion; PoolDigest = poolDigest;
            StreamSeeds = new ReadOnlyDictionary<RmapWorldRngStream, string>(new Dictionary<RmapWorldRngStream, string>(streams));
            StableIds = new ReadOnlyCollection<RmapWorldStableId>(ids.OrderBy(value => value).ToArray());
            Digest = Hash(string.Join("\n", new[] { request.Seed.ToString(), request.ContentVersion, request.GeneratorVersion, poolVersion, poolDigest }
                .Concat(StreamSeeds.OrderBy(value => value.Key).Select(value => value.Key + "=" + value.Value))
                .Concat(StableIds.Select(value => value.Value))));
        }
        public RmapWorldDataRequest Request { get; }
        public string PoolVersion { get; }
        public string PoolDigest { get; }
        public IReadOnlyDictionary<RmapWorldRngStream, string> StreamSeeds { get; }
        public IReadOnlyList<RmapWorldStableId> StableIds { get; }
        public string Digest { get; }
        internal static string Hash(string text)
        {
            using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty)).Select(value => value.ToString("x2")));
        }
    }

    public sealed class RmapWorldMutationState
    {
        private readonly SortedDictionary<string, string> values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        public void Apply(RmapWorldStableId id, string state)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            values[id.Value] = state ?? string.Empty;
        }
        public IReadOnlyDictionary<string, string> Snapshot => new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(values));
        public string Serialize() => string.Join("\n", values.Select(value => value.Key + "\t" + value.Value.Replace("\t", " ").Replace("\n", " ")));
        public static RmapWorldMutationState Deserialize(string text)
        {
            var state = new RmapWorldMutationState();
            foreach (string line in (text ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int split = line.IndexOf('\t');
                if (split <= 0) throw new ArgumentException("Invalid world mutation record.", nameof(text));
                state.values.Add(line.Substring(0, split), line.Substring(split + 1));
            }
            return state;
        }
    }

    public static class RmapWorldDataGenerator
    {
        public static RmapWorldDefinition Generate(RmapWorldDataRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();
            var streams = Enum.GetValues(typeof(RmapWorldRngStream)).Cast<RmapWorldRngStream>().ToDictionary(stream => stream,
                stream => RmapWorldDefinition.Hash(request.Seed + "|" + request.ContentVersion + "|" + request.GeneratorVersion + "|" +
                    RmapPatternPool500.DataVersion + "|" + pool.Digest + "|" + stream + "|WORLD|0"));
            var ids = Enum.GetValues(typeof(RmapWorldStableIdKind)).Cast<RmapWorldStableIdKind>()
                .Select(kind => new RmapWorldStableId(kind, "WORLD", 0)).ToArray();
            return new RmapWorldDefinition(request, RmapPatternPool500.DataVersion, pool.Digest, streams, ids);
        }
    }
}
