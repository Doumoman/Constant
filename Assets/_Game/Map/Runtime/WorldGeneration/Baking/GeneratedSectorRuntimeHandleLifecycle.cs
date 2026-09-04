using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedSectorRuntimeHandleLifecycle
    {
        public const string DownstreamOwner =
            "MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION";
        public const bool OpensDownstreamTask = false;

        private static readonly HashSet<string> AllowedPairs = new HashSet<string>(StringComparer.Ordinal)
        {
            Pair(GeneratedSectorRuntimeState.Unloaded, GeneratedSectorRuntimeState.Preloaded),
            Pair(GeneratedSectorRuntimeState.Preloaded, GeneratedSectorRuntimeState.Active),
            Pair(GeneratedSectorRuntimeState.Active, GeneratedSectorRuntimeState.Preloaded),
            Pair(GeneratedSectorRuntimeState.Active, GeneratedSectorRuntimeState.SleepingModified),
            Pair(GeneratedSectorRuntimeState.SleepingModified, GeneratedSectorRuntimeState.Active),
            Pair(GeneratedSectorRuntimeState.SleepingModified, GeneratedSectorRuntimeState.Unloaded),
            Pair(GeneratedSectorRuntimeState.Preloaded, GeneratedSectorRuntimeState.Unloaded),
        };

        public static GeneratedSectorRuntimeHandle CreateUnloaded(
            GeneratedColliderCacheEntry entry,
            string seedIdentity)
        {
            if (entry == null || !entry.IsCoherent)
                throw new ArgumentException("A coherent collider cache entry is required.", nameof(entry));
            var key = entry.Key;
            var id = new GeneratedSectorRuntimeHandleId(key.Sector, seedIdentity,
                key.GeneratorVersion, key.DataVersion, key.BakeDigest);
            if (!id.IsValid) throw new ArgumentException("Runtime handle identity is invalid.");
            return Create(id, GeneratedSectorRuntimeState.Unloaded, null, key,
                key.MutationRevision, false, string.Empty, new[] { "CREATED_UNLOADED" });
        }

        public static GeneratedSectorRuntimeHandleResult Transition(
            GeneratedSectorRuntimeTransitionRequest request)
        {
            var failures = new List<GeneratedSectorRuntimeHandleFailure>();
            var source = request == null ? null : request.Handle;
            if (source == null)
            {
                Add(failures, GeneratedSectorRuntimeHandleFailureCode.MissingHandle, "handle",
                    "A runtime handle is required.");
                return Result(source, null, request, failures,
                    request == null ? GeneratedColliderCacheSnapshot.Empty : request.CacheSnapshot);
            }

            if (request.Sector == null || !source.Sector.Equals(request.Sector))
                Add(failures, GeneratedSectorRuntimeHandleFailureCode.SectorMismatch, "sector",
                    "Transition sector must equal the handle sector.");
            var allowed = AllowedPairs.Contains(Pair(source.State, request.TargetState));
            if (!allowed)
                Add(failures, GeneratedSectorRuntimeHandleFailureCode.ForbiddenTransition, "state",
                    source.State + " -> " + request.TargetState + " is not documented.");

            var needsEntry = source.State == GeneratedSectorRuntimeState.Unloaded &&
                             request.TargetState == GeneratedSectorRuntimeState.Preloaded ||
                             source.State == GeneratedSectorRuntimeState.SleepingModified &&
                             request.TargetState == GeneratedSectorRuntimeState.Active;
            if (needsEntry && request.CacheEntry == null)
                Add(failures, GeneratedSectorRuntimeHandleFailureCode.MissingCacheEntry, "cacheEntry",
                    "This transition requires a validated collider cache entry.");
            if (request.CacheEntry != null && !Matches(source, request.CacheEntry,
                    needsEntry && source.State == GeneratedSectorRuntimeState.SleepingModified
                        ? source.MutationRevision : source.MutationRevision))
                Add(failures, GeneratedSectorRuntimeHandleFailureCode.StaleCacheKey, "cacheEntry",
                    "The cache key does not match handle identity, upstream digests, sector, or revision.");

            var targetRevision = source.MutationRevision;
            var targetDirty = source.IsDirty;
            var targetDirtyReason = source.DirtyReason;
            if (source.State == GeneratedSectorRuntimeState.Active &&
                request.TargetState == GeneratedSectorRuntimeState.SleepingModified)
            {
                if (!request.MutationRevision.HasValue ||
                    request.MutationRevision.Value <= source.MutationRevision)
                    Add(failures, GeneratedSectorRuntimeHandleFailureCode.MutationRevisionMismatch,
                        "mutationRevision", "A dirty transition must advance mutation revision.");
                else
                    targetRevision = request.MutationRevision.Value;
                if (string.IsNullOrWhiteSpace(request.DirtyReason))
                    Add(failures, GeneratedSectorRuntimeHandleFailureCode.InvalidDirtyReason,
                        "dirtyReason", "A dirty transition requires a stable reason.");
                else
                    targetDirtyReason = request.DirtyReason;
                targetDirty = true;
            }
            else if (request.MutationRevision.HasValue &&
                     request.MutationRevision.Value != source.MutationRevision)
                Add(failures, GeneratedSectorRuntimeHandleFailureCode.MutationRevisionMismatch,
                    "mutationRevision", "Only Active -> SleepingModified may advance revision.");

            if (!GeneratedSectorRuntimeHandleDigest.IsLowerHexSha256(source.Digest))
                Add(failures, GeneratedSectorRuntimeHandleFailureCode.InvalidDigest, "handleDigest",
                    "Source runtime handle digest must be lower-hex SHA-256.");
            if (failures.Count != 0)
                return Result(source, null, request, failures, request.CacheSnapshot);

            GeneratedColliderCacheKey nextKey;
            var nextSnapshot = request.CacheSnapshot;
            if (request.TargetState == GeneratedSectorRuntimeState.Unloaded ||
                request.TargetState == GeneratedSectorRuntimeState.SleepingModified)
            {
                if (source.CacheKey != null)
                    nextSnapshot = nextSnapshot.Invalidate(source.CacheKey);
                nextKey = null;
            }
            else if (request.CacheEntry != null)
                nextKey = request.CacheEntry.Key;
            else
                nextKey = source.CacheKey;

            var handle = Create(source.Id, request.TargetState, nextKey,
                new GeneratedColliderCacheKey(source.GeometryDigest, source.BakeDigest,
                    source.SeamDigest, source.RegistryDigest, source.Sector,
                    source.Id.GeneratorVersion, source.Id.DataVersion, targetRevision,
                    GeneratedColliderRebuildPlan.CollisionPolicyVersion),
                targetRevision, targetDirty, targetDirtyReason,
                source.Diagnostics.Concat(new[] { source.State + "_TO_" + request.TargetState }));
            var transition = new GeneratedSectorRuntimeTransition(source.State, request.TargetState,
                source.Sector, source.MutationRevision, targetRevision, true, targetDirtyReason);
            return new GeneratedSectorRuntimeHandleResult(source, handle, transition, nextSnapshot,
                Array.Empty<GeneratedSectorRuntimeHandleFailure>());
        }

        public static bool IsAllowed(
            GeneratedSectorRuntimeState from,
            GeneratedSectorRuntimeState to) => AllowedPairs.Contains(Pair(from, to));

        private static bool Matches(
            GeneratedSectorRuntimeHandle handle,
            GeneratedColliderCacheEntry entry,
            int expectedRevision) => entry.IsCoherent && entry.Key.Sector.Equals(handle.Sector) &&
            entry.Key.MutationRevision == expectedRevision &&
            string.Equals(entry.Key.GeometryDigest, handle.GeometryDigest, StringComparison.Ordinal) &&
            string.Equals(entry.Key.BakeDigest, handle.BakeDigest, StringComparison.Ordinal) &&
            string.Equals(entry.Key.SeamDigest, handle.SeamDigest, StringComparison.Ordinal) &&
            string.Equals(entry.Key.RegistryDigest, handle.RegistryDigest, StringComparison.Ordinal) &&
            string.Equals(entry.Key.GeneratorVersion, handle.Id.GeneratorVersion, StringComparison.Ordinal) &&
            string.Equals(entry.Key.DataVersion, handle.Id.DataVersion, StringComparison.Ordinal);

        private static GeneratedSectorRuntimeHandle Create(
            GeneratedSectorRuntimeHandleId id,
            GeneratedSectorRuntimeState state,
            GeneratedColliderCacheKey cacheKey,
            GeneratedColliderCacheKey identityKey,
            int mutationRevision,
            bool dirty,
            string dirtyReason,
            IEnumerable<string> diagnostics) => new GeneratedSectorRuntimeHandle(
                id, identityKey.Sector, state, cacheKey, identityKey.GeometryDigest,
                identityKey.BakeDigest, identityKey.SeamDigest, identityKey.RegistryDigest,
                mutationRevision, dirty, dirtyReason, diagnostics);

        private static GeneratedSectorRuntimeHandleResult Result(
            GeneratedSectorRuntimeHandle source,
            GeneratedSectorRuntimeHandle handle,
            GeneratedSectorRuntimeTransitionRequest request,
            IEnumerable<GeneratedSectorRuntimeHandleFailure> failures,
            GeneratedColliderCacheSnapshot snapshot)
        {
            var target = request == null ? GeneratedSectorRuntimeState.Unloaded : request.TargetState;
            var transition = source == null ? null : new GeneratedSectorRuntimeTransition(
                source.State, target, request.Sector, source.MutationRevision,
                request.MutationRevision ?? source.MutationRevision, false, request.DirtyReason);
            return new GeneratedSectorRuntimeHandleResult(source, handle, transition, snapshot, failures);
        }

        private static void Add(
            ICollection<GeneratedSectorRuntimeHandleFailure> failures,
            GeneratedSectorRuntimeHandleFailureCode code,
            string subject,
            string reason) => failures.Add(new GeneratedSectorRuntimeHandleFailure(code, subject, reason));
        private static string Pair(GeneratedSectorRuntimeState from, GeneratedSectorRuntimeState to) =>
            from + ">" + to;
    }
}
