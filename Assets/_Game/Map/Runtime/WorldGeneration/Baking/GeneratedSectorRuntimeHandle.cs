using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedSectorRuntimeState
    {
        Unloaded = 1,
        Preloaded = 2,
        Active = 3,
        SleepingModified = 4,
    }

    public sealed class GeneratedSectorRuntimeHandleId :
        IEquatable<GeneratedSectorRuntimeHandleId>, IComparable<GeneratedSectorRuntimeHandleId>
    {
        public GeneratedSectorRuntimeHandleId(
            GeneratedSectorIndexCoordinate sector,
            string seedIdentity,
            string generatorVersion,
            string dataVersion,
            string bakeDigest)
        {
            Sector = sector;
            SeedIdentity = seedIdentity ?? string.Empty;
            GeneratorVersion = generatorVersion ?? string.Empty;
            DataVersion = dataVersion ?? string.Empty;
            BakeDigest = bakeDigest ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "SECTOR_RUNTIME_HANDLE_ID", Sector == null ? "MISSING" : Sector.ToString(),
                SeedIdentity, GeneratorVersion, DataVersion, BakeDigest,
            });
            Value = BakingCanonicalDigest.HashCanonicalLines(new[] { StableToken });
        }

        public GeneratedSectorIndexCoordinate Sector { get; }
        public string SeedIdentity { get; }
        public string GeneratorVersion { get; }
        public string DataVersion { get; }
        public string BakeDigest { get; }
        public string StableToken { get; }
        public string Value { get; }
        public bool IsValid => Sector != null && Sector.IsInBounds &&
            !string.IsNullOrWhiteSpace(SeedIdentity) &&
            !string.IsNullOrWhiteSpace(GeneratorVersion) &&
            !string.IsNullOrWhiteSpace(DataVersion) &&
            GeneratedColliderRebuildDigest.IsLowerHexSha256(BakeDigest) &&
            GeneratedColliderRebuildDigest.IsLowerHexSha256(Value);
        public int CompareTo(GeneratedSectorRuntimeHandleId other) => other == null ? -1 :
            string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(GeneratedSectorRuntimeHandleId other) => other != null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedSectorRuntimeHandleId);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public enum GeneratedSectorRuntimeHandleFailureCode
    {
        MissingHandle = 1,
        MissingCacheEntry = 2,
        StaleCacheKey = 3,
        ForbiddenTransition = 4,
        SectorMismatch = 5,
        MutationRevisionMismatch = 6,
        InvalidDirtyReason = 7,
        InvalidDigest = 8,
    }

    public sealed class GeneratedSectorRuntimeHandleFailure :
        IEquatable<GeneratedSectorRuntimeHandleFailure>,
        IComparable<GeneratedSectorRuntimeHandleFailure>
    {
        public GeneratedSectorRuntimeHandleFailure(
            GeneratedSectorRuntimeHandleFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedSectorRuntimeHandleFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public string StableToken => Code + "|" + Subject + "|" + Reason;
        public int CompareTo(GeneratedSectorRuntimeHandleFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedSectorRuntimeHandleFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as GeneratedSectorRuntimeHandleFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedSectorRuntimeHandle
    {
        private readonly ReadOnlyCollection<string> diagnostics;

        internal GeneratedSectorRuntimeHandle(
            GeneratedSectorRuntimeHandleId id,
            GeneratedSectorIndexCoordinate sector,
            GeneratedSectorRuntimeState state,
            GeneratedColliderCacheKey cacheKey,
            string geometryDigest,
            string bakeDigest,
            string seamDigest,
            string registryDigest,
            int mutationRevision,
            bool isDirty,
            string dirtyReason,
            IEnumerable<string> sourceDiagnostics)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Sector = sector ?? throw new ArgumentNullException(nameof(sector));
            State = state;
            CacheKey = cacheKey;
            GeometryDigest = geometryDigest ?? string.Empty;
            BakeDigest = bakeDigest ?? string.Empty;
            SeamDigest = seamDigest ?? string.Empty;
            RegistryDigest = registryDigest ?? string.Empty;
            MutationRevision = mutationRevision;
            IsDirty = isDirty;
            DirtyReason = dirtyReason ?? string.Empty;
            diagnostics = new ReadOnlyCollection<string>((sourceDiagnostics ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Digest = GeneratedSectorRuntimeHandleDigest.Compute(this);
        }

        public GeneratedSectorRuntimeHandleId Id { get; }
        public GeneratedSectorIndexCoordinate Sector { get; }
        public GeneratedSectorRuntimeState State { get; }
        public GeneratedColliderCacheKey CacheKey { get; }
        public bool RetainsRuntimeCache => CacheKey != null;
        public string GeometryDigest { get; }
        public string BakeDigest { get; }
        public string SeamDigest { get; }
        public string RegistryDigest { get; }
        public int MutationRevision { get; }
        public bool IsDirty { get; }
        public string DirtyReason { get; }
        public IReadOnlyList<string> Diagnostics => diagnostics;
        public string Digest { get; }
        public int DurableSaveWriteCount => 0;
        public int TilemapComponentWriteCount => 0;
        public int ColliderCreationCount => 0;
        public int RigidbodyCreationCount => 0;
        public int PhysicsQueryCount => 0;
        public int PhysicsSimulationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int TilemapMutationCount => 0;
        public int GameObjectInstantiationCount => 0;
        public int PrefabInstantiationCount => 0;
        public int GeneratedFileWriteCount => 0;
        public int RuntimeObjectSpawnCount => 0;
    }

    public sealed class GeneratedSectorRuntimeTransition
    {
        internal GeneratedSectorRuntimeTransition(
            GeneratedSectorRuntimeState from,
            GeneratedSectorRuntimeState to,
            GeneratedSectorIndexCoordinate sector,
            int fromRevision,
            int toRevision,
            bool allowed,
            string dirtyReason)
        {
            From = from;
            To = to;
            Sector = sector;
            FromRevision = fromRevision;
            ToRevision = toRevision;
            Allowed = allowed;
            DirtyReason = dirtyReason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "SECTOR_RUNTIME_TRANSITION", From.ToString().ToUpperInvariant(),
                To.ToString().ToUpperInvariant(), Sector == null ? "MISSING" : Sector.ToString(),
                Number(FromRevision), Number(ToRevision), Allowed ? "1" : "0", DirtyReason,
            });
        }

        public GeneratedSectorRuntimeState From { get; }
        public GeneratedSectorRuntimeState To { get; }
        public GeneratedSectorIndexCoordinate Sector { get; }
        public int FromRevision { get; }
        public int ToRevision { get; }
        public bool Allowed { get; }
        public string DirtyReason { get; }
        public string StableToken { get; }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedSectorRuntimeTransitionRequest
    {
        public GeneratedSectorRuntimeTransitionRequest(
            GeneratedSectorRuntimeHandle handle,
            GeneratedSectorRuntimeState targetState,
            GeneratedSectorIndexCoordinate sector,
            GeneratedColliderCacheSnapshot cacheSnapshot,
            GeneratedColliderCacheEntry cacheEntry = null,
            int? mutationRevision = null,
            string dirtyReason = null)
        {
            Handle = handle;
            TargetState = targetState;
            Sector = sector;
            CacheSnapshot = cacheSnapshot ?? GeneratedColliderCacheSnapshot.Empty;
            CacheEntry = cacheEntry;
            MutationRevision = mutationRevision;
            DirtyReason = dirtyReason ?? string.Empty;
        }

        public GeneratedSectorRuntimeHandle Handle { get; }
        public GeneratedSectorRuntimeState TargetState { get; }
        public GeneratedSectorIndexCoordinate Sector { get; }
        public GeneratedColliderCacheSnapshot CacheSnapshot { get; }
        public GeneratedColliderCacheEntry CacheEntry { get; }
        public int? MutationRevision { get; }
        public string DirtyReason { get; }
    }

    public sealed class GeneratedSectorRuntimeHandleResult
    {
        private readonly ReadOnlyCollection<GeneratedSectorRuntimeHandleFailure> failures;

        internal GeneratedSectorRuntimeHandleResult(
            GeneratedSectorRuntimeHandle sourceHandle,
            GeneratedSectorRuntimeHandle handle,
            GeneratedSectorRuntimeTransition transition,
            GeneratedColliderCacheSnapshot cacheSnapshot,
            IEnumerable<GeneratedSectorRuntimeHandleFailure> sourceFailures)
        {
            SourceHandle = sourceHandle;
            Handle = handle;
            Transition = transition;
            CacheSnapshot = cacheSnapshot ?? GeneratedColliderCacheSnapshot.Empty;
            failures = new ReadOnlyCollection<GeneratedSectorRuntimeHandleFailure>((sourceFailures ??
                Array.Empty<GeneratedSectorRuntimeHandleFailure>()).Distinct()
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Handle != null && Transition != null && Transition.Allowed &&
            failures.Count == 0;
        public GeneratedSectorRuntimeHandle SourceHandle { get; }
        public GeneratedSectorRuntimeHandle Handle { get; }
        public GeneratedSectorRuntimeTransition Transition { get; }
        public GeneratedColliderCacheSnapshot CacheSnapshot { get; }
        public IReadOnlyList<GeneratedSectorRuntimeHandleFailure> Failures => failures;
    }

    public static class GeneratedSectorRuntimeHandleDigest
    {
        public static string Compute(GeneratedSectorRuntimeHandle handle)
        {
            if (handle == null) return string.Empty;
            var lines = new List<string>
            {
                "RUNTIME_HANDLE|" + handle.Id.Value,
                "SECTOR|" + handle.Sector,
                "STATE|" + handle.State.ToString().ToUpperInvariant(),
                "CACHE|" + (handle.CacheKey == null ? "NONE" : handle.CacheKey.Digest),
                "UPSTREAM|" + handle.GeometryDigest + "|" + handle.BakeDigest + "|" +
                    handle.SeamDigest + "|" + handle.RegistryDigest,
                "MUTATION|" + Number(handle.MutationRevision) + "|" +
                    (handle.IsDirty ? "1" : "0") + "|" + handle.DirtyReason,
                "NO_SIDE_EFFECTS|0|0|0|0|0|0|0|0|0|0|0|0|0",
            };
            lines.AddRange(handle.Diagnostics.OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => "DIAGNOSTIC|" + value));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
