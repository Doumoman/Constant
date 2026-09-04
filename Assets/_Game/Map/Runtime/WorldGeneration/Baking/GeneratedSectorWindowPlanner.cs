using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedSectorWindowPlanner
    {
        public static GeneratedSectorStreamingResult Plan(GeneratedSectorWindowRequest request)
        {
            var failures = new List<GeneratedSectorStreamingFailure>();
            if (request == null)
            {
                Add(failures, GeneratedSectorStreamingFailureCode.MissingRequest, "request",
                    "A sector window request is required.");
                return Failure(null, failures);
            }

            ValidateRequest(request, failures);
            if (failures.Count != 0) return Failure(request, failures);

            var handles = request.Handles.ToDictionary(
                value => Key(value.Sector), value => value, StringComparer.Ordinal);
            var entries = request.CacheEntries.GroupBy(value => Key(value.Key.Sector))
                .ToDictionary(group => group.Key, group => group.OrderBy(value =>
                    value.Key.MutationRevision).ToArray(), StringComparer.Ordinal);
            var preloadCoordinates = EnumerateWindow(request.Center,
                GeneratedSectorStreamingWindow.PreloadRadius).ToArray();
            var activeCoordinates = EnumerateWindow(request.Center,
                GeneratedSectorStreamingWindow.ActiveRadius).ToArray();
            ValidateSources(preloadCoordinates, handles, entries, failures);
            if (failures.Count != 0) return Failure(request, failures);

            var preloadMembers = preloadCoordinates.Select(coordinate =>
            {
                var handle = handles[Key(coordinate)];
                var entry = FindEntry(handle, entries[Key(coordinate)]);
                var expected = coordinate.ChebyshevDistance(request.Center) <=
                               GeneratedSectorStreamingWindow.ActiveRadius
                    ? GeneratedSectorRuntimeState.Active
                    : handle.State == GeneratedSectorRuntimeState.SleepingModified
                        ? GeneratedSectorRuntimeState.SleepingModified
                        : GeneratedSectorRuntimeState.Preloaded;
                return new GeneratedSectorWindowMember(coordinate,
                    coordinate.ChebyshevDistance(request.Center), GeneratedSectorWindowKind.Preload,
                    expected, entry.Key);
            }).ToArray();
            var activeMembers = activeCoordinates.Select(coordinate =>
            {
                var handle = handles[Key(coordinate)];
                var entry = FindEntry(handle, entries[Key(coordinate)]);
                return new GeneratedSectorWindowMember(coordinate,
                    coordinate.ChebyshevDistance(request.Center), GeneratedSectorWindowKind.Active,
                    GeneratedSectorRuntimeState.Active, entry.Key);
            }).ToArray();
            var candidates = BuildCandidates(request, handles, entries, preloadCoordinates, failures);
            if (failures.Count != 0) return Failure(request, failures);

            var window = new GeneratedSectorStreamingWindow(request, preloadMembers,
                activeMembers, candidates);
            if (!window.ActiveIsSubsetOfPreload || window.DuplicatePreloadMemberCount != 0 ||
                window.DuplicateActiveMemberCount != 0 || window.OutOfWorldPreloadMemberCount != 0 ||
                window.OutOfWorldActiveMemberCount != 0 ||
                window.PreactivationCandidates.Any(value => !value.IsInsideValidWindow))
                Add(failures, GeneratedSectorStreamingFailureCode.InvalidWindow, "window",
                    "Window membership must be unique, in-world, and nested.");
            if (!GeneratedSectorWindowDigest.IsLowerHexSha256(window.Digest))
                Add(failures, GeneratedSectorStreamingFailureCode.InvalidDigest, "windowDigest",
                    "Window digest must be lower-hex SHA-256.");
            if (failures.Count != 0) return Failure(request, failures);

            var diff = BuildDiff(request.PreviousWindow, window, handles);
            var transitionPlan = BuildTransitionPlan(window, request.Handles,
                request.CacheEntries, failures);
            if (!GeneratedSectorWindowDigest.IsLowerHexSha256(diff.Digest) ||
                transitionPlan == null ||
                !GeneratedSectorWindowDigest.IsLowerHexSha256(transitionPlan.Digest))
                Add(failures, GeneratedSectorStreamingFailureCode.InvalidDigest,
                    "downstreamDigest", "Diff and transition digests must be lower-hex SHA-256.");
            return failures.Count == 0
                ? new GeneratedSectorStreamingResult(request, window, diff, transitionPlan,
                    Array.Empty<GeneratedSectorStreamingFailure>())
                : Failure(request, failures);
        }

        private static void ValidateRequest(
            GeneratedSectorWindowRequest request,
            ICollection<GeneratedSectorStreamingFailure> failures)
        {
            if (request.Center == null || !request.Center.IsInWorld)
                Add(failures, GeneratedSectorStreamingFailureCode.InvalidCenter, "center",
                    "Center sector must be inside the canonical 13x13 world.");
            if (double.IsNaN(request.LocalProgressX) || double.IsInfinity(request.LocalProgressX) ||
                double.IsNaN(request.LocalProgressY) || double.IsInfinity(request.LocalProgressY) ||
                request.LocalProgressX < 0d || request.LocalProgressX > 1d ||
                request.LocalProgressY < 0d || request.LocalProgressY > 1d)
                Add(failures, GeneratedSectorStreamingFailureCode.InvalidLocalProgress,
                    "localProgress", "Local progress must be finite and normalized to 0..1.");
            if (request.PreactivationPolicy == null || !request.PreactivationPolicy.IsValid)
                Add(failures, GeneratedSectorStreamingFailureCode.InvalidPolicy,
                    "preactivationPolicy", "A valid low/high hysteresis policy is required.");
            if (request.NullHandleCount != 0 || request.NullCacheEntryCount != 0)
                Add(failures, GeneratedSectorStreamingFailureCode.NullSource, "sources",
                    "Null handles and cache entries are forbidden.");
            var duplicateHandles = request.Handles.GroupBy(value => Key(value.Sector))
                .Count(group => group.Count() != 1);
            if (duplicateHandles != 0)
                Add(failures, GeneratedSectorStreamingFailureCode.DuplicateHandle, "handles",
                    "Each sector may publish exactly one runtime handle.");
            var duplicateEntries = request.CacheEntries.GroupBy(value =>
                    Key(value.Key.Sector) + "|" + value.Key.MutationRevision)
                .Count(group => group.Count() != 1);
            if (duplicateEntries != 0)
                Add(failures, GeneratedSectorStreamingFailureCode.DuplicateCacheEntry,
                    "cacheEntries", "Each sector/revision may publish exactly one cache entry.");
            if (request.Handles.Any(value => value.Sector == null || !value.Sector.IsInBounds ||
                    !GeneratedSectorRuntimeHandleDigest.IsLowerHexSha256(value.Digest)))
                Add(failures, GeneratedSectorStreamingFailureCode.InvalidHandle, "handles",
                    "Runtime handles must have in-world coordinates and valid digests.");
            if (!GeneratedSectorWindowDigest.IsLowerHexSha256(request.CanonicalDigest))
                Add(failures, GeneratedSectorStreamingFailureCode.InvalidDigest, "requestDigest",
                    "Request digest must be lower-hex SHA-256.");
        }

        private static void ValidateSources(
            IEnumerable<GeneratedSectorCoordinate> coordinates,
            IReadOnlyDictionary<string, GeneratedSectorRuntimeHandle> handles,
            IReadOnlyDictionary<string, GeneratedColliderCacheEntry[]> entries,
            ICollection<GeneratedSectorStreamingFailure> failures)
        {
            foreach (var coordinate in coordinates)
            {
                GeneratedSectorRuntimeHandle handle;
                if (!handles.TryGetValue(Key(coordinate), out handle))
                {
                    Add(failures, GeneratedSectorStreamingFailureCode.MissingHandle,
                        coordinate.ToString(), "Window sector is missing its runtime handle.");
                    continue;
                }
                GeneratedColliderCacheEntry[] candidates;
                if (!entries.TryGetValue(Key(coordinate), out candidates) ||
                    FindEntry(handle, candidates) == null)
                    Add(failures, GeneratedSectorStreamingFailureCode.MissingCache,
                        coordinate.ToString(), "Window sector is missing a coherent revision cache entry.");
            }
        }

        private static GeneratedSectorPreactivationCandidate[] BuildCandidates(
            GeneratedSectorWindowRequest request,
            IReadOnlyDictionary<string, GeneratedSectorRuntimeHandle> handles,
            IReadOnlyDictionary<string, GeneratedColliderCacheEntry[]> entries,
            IReadOnlyCollection<GeneratedSectorCoordinate> preloadCoordinates,
            ICollection<GeneratedSectorStreamingFailure> failures)
        {
            var intents = request.PreactivationPolicy.Evaluate(request.Center,
                request.LocalProgressX, request.LocalProgressY, request.DirectionHint,
                request.PreactivationLatched);
            var result = new List<GeneratedSectorPreactivationCandidate>();
            foreach (var intent in intents)
            {
                GeneratedSectorRuntimeHandle handle;
                GeneratedColliderCacheEntry[] candidates;
                if (!handles.TryGetValue(Key(intent.Coordinate), out handle))
                {
                    Add(failures, GeneratedSectorStreamingFailureCode.MissingHandle,
                        intent.Coordinate.ToString(), "Preactivation candidate is missing its handle.");
                    continue;
                }
                if (!entries.TryGetValue(Key(intent.Coordinate), out candidates))
                {
                    Add(failures, GeneratedSectorStreamingFailureCode.MissingCache,
                        intent.Coordinate.ToString(), "Preactivation candidate is missing its cache entry.");
                    continue;
                }
                var entry = FindEntry(handle, candidates);
                if (entry == null)
                {
                    Add(failures, GeneratedSectorStreamingFailureCode.MissingCache,
                        intent.Coordinate.ToString(),
                        "Preactivation candidate cache does not match its mutation revision.");
                    continue;
                }
                var insideCurrent = preloadCoordinates.Contains(intent.Coordinate);
                var insideNext = intent.Coordinate.ChebyshevDistance(intent.Coordinate) <=
                                 GeneratedSectorStreamingWindow.PreloadRadius;
                result.Add(new GeneratedSectorPreactivationCandidate(intent.Coordinate,
                    intent.Direction, intent.Reason, insideCurrent, insideNext, entry.Key,
                    GeneratedSectorRuntimeState.Active));
            }
            return result.OrderBy(value => value).ToArray();
        }

        private static GeneratedSectorWindowDiff BuildDiff(
            GeneratedSectorStreamingWindow previous,
            GeneratedSectorStreamingWindow next,
            IReadOnlyDictionary<string, GeneratedSectorRuntimeHandle> handles)
        {
            var previousPreload = previous == null
                ? new HashSet<GeneratedSectorCoordinate>()
                : new HashSet<GeneratedSectorCoordinate>(previous.PreloadMembers
                    .Select(value => value.Coordinate));
            var previousActive = previous == null
                ? new HashSet<GeneratedSectorCoordinate>()
                : new HashSet<GeneratedSectorCoordinate>(previous.ActiveMembers
                    .Select(value => value.Coordinate));
            var nextPreload = new HashSet<GeneratedSectorCoordinate>(next.PreloadMembers
                .Select(value => value.Coordinate));
            var nextActive = new HashSet<GeneratedSectorCoordinate>(next.ActiveMembers
                .Select(value => value.Coordinate));
            var coordinates = previousPreload.Concat(nextPreload).Distinct().OrderBy(value => value);
            var changes = new List<GeneratedSectorWindowChange>();
            foreach (var coordinate in coordinates)
            {
                var wasPreload = previousPreload.Contains(coordinate);
                var wasActive = previousActive.Contains(coordinate);
                var isPreload = nextPreload.Contains(coordinate);
                var isActive = nextActive.Contains(coordinate);
                GeneratedSectorRuntimeHandle handle;
                handles.TryGetValue(Key(coordinate), out handle);
                var sourceState = handle == null ? GeneratedSectorRuntimeState.Unloaded : handle.State;
                var targetState = isActive ? GeneratedSectorRuntimeState.Active : isPreload
                    ? sourceState == GeneratedSectorRuntimeState.SleepingModified
                        ? GeneratedSectorRuntimeState.SleepingModified
                        : GeneratedSectorRuntimeState.Preloaded
                    : GeneratedSectorRuntimeState.Unloaded;
                if (!wasPreload && isPreload)
                    Change(changes, coordinate, GeneratedSectorWindowChangeKind.AddPreload,
                        sourceState, targetState, handle);
                if (wasPreload && !isPreload)
                {
                    Change(changes, coordinate, GeneratedSectorWindowChangeKind.RemovePreload,
                        sourceState, targetState, handle);
                    Change(changes, coordinate, GeneratedSectorWindowChangeKind.EvictCandidate,
                        sourceState, targetState, handle);
                }
                if (!wasActive && isActive)
                    Change(changes, coordinate,
                        GeneratedSectorWindowChangeKind.PromotePreloadToActive,
                        sourceState, targetState, handle);
                if (wasActive && !isActive && isPreload)
                    Change(changes, coordinate,
                        GeneratedSectorWindowChangeKind.DemoteActiveToPreload,
                        sourceState, targetState, handle);
                if (wasActive && isActive && sourceState !=
                    GeneratedSectorRuntimeState.SleepingModified)
                    Change(changes, coordinate, GeneratedSectorWindowChangeKind.PreserveActive,
                        sourceState, targetState, handle);
                if (wasPreload && isPreload && !wasActive && !isActive &&
                    sourceState != GeneratedSectorRuntimeState.SleepingModified)
                    Change(changes, coordinate, GeneratedSectorWindowChangeKind.PreservePreload,
                        sourceState, targetState, handle);
                if (sourceState == GeneratedSectorRuntimeState.SleepingModified &&
                    (wasPreload || isPreload))
                    Change(changes, coordinate,
                        GeneratedSectorWindowChangeKind.PreserveSleepingModified,
                        sourceState, targetState, handle);
            }
            return new GeneratedSectorWindowDiff(previous, next, changes);
        }

        private static GeneratedSectorHandleTransitionPlan BuildTransitionPlan(
            GeneratedSectorStreamingWindow window,
            IEnumerable<GeneratedSectorRuntimeHandle> sourceHandles,
            IEnumerable<GeneratedColliderCacheEntry> sourceEntries,
            ICollection<GeneratedSectorStreamingFailure> failures)
        {
            var handles = sourceHandles.ToDictionary(value => Key(value.Sector), value => value,
                StringComparer.Ordinal);
            var entries = sourceEntries.GroupBy(value => Key(value.Key.Sector))
                .ToDictionary(group => group.Key, group => group.OrderBy(value =>
                    value.Key.MutationRevision).ToArray(), StringComparer.Ordinal);
            var snapshot = GeneratedColliderCacheSnapshot.Empty;
            foreach (var entry in sourceEntries.OrderBy(value => value)) snapshot = snapshot.Store(entry);
            var records = new List<GeneratedSectorHandleTransitionRecord>();
            var active = new HashSet<GeneratedSectorCoordinate>(window.ActiveMembers
                .Select(value => value.Coordinate));
            var preload = new HashSet<GeneratedSectorCoordinate>(window.PreloadMembers
                .Select(value => value.Coordinate));
            foreach (var handleKey in handles.Keys.OrderBy(value => handles[value].Sector).ToArray())
            {
                var handle = handles[handleKey];
                var coordinate = GeneratedSectorCoordinate.FromRuntime(handle.Sector);
                var wantsActive = active.Contains(coordinate);
                var wantsPreload = preload.Contains(coordinate);
                GeneratedColliderCacheEntry[] candidates;
                entries.TryGetValue(handleKey, out candidates);
                var entry = candidates == null ? null : FindEntry(handle, candidates);

                if (wantsActive)
                {
                    if (handle.State == GeneratedSectorRuntimeState.Unloaded)
                        handle = Apply(handle, GeneratedSectorRuntimeState.Preloaded, "ADD_PRELOAD",
                            entry, ref snapshot, records, failures);
                    if (handle != null && (handle.State == GeneratedSectorRuntimeState.Preloaded ||
                                           handle.State == GeneratedSectorRuntimeState.SleepingModified))
                        handle = Apply(handle, GeneratedSectorRuntimeState.Active, "PROMOTE_ACTIVE",
                            handle.State == GeneratedSectorRuntimeState.SleepingModified ? entry : null,
                            ref snapshot, records, failures);
                }
                else if (wantsPreload)
                {
                    if (handle.State == GeneratedSectorRuntimeState.Unloaded)
                        handle = Apply(handle, GeneratedSectorRuntimeState.Preloaded, "ADD_PRELOAD",
                            entry, ref snapshot, records, failures);
                    else if (handle.State == GeneratedSectorRuntimeState.Active)
                        handle = Apply(handle, GeneratedSectorRuntimeState.Preloaded, "DEMOTE_PRELOAD",
                            null, ref snapshot, records, failures);
                }
                else
                {
                    if (handle.State == GeneratedSectorRuntimeState.Active)
                        handle = Apply(handle, GeneratedSectorRuntimeState.Preloaded, "DEMOTE_FOR_EVICT",
                            null, ref snapshot, records, failures);
                    if (handle != null && (handle.State == GeneratedSectorRuntimeState.Preloaded ||
                                           handle.State == GeneratedSectorRuntimeState.SleepingModified))
                        handle = Apply(handle, GeneratedSectorRuntimeState.Unloaded, "EVICT",
                            null, ref snapshot, records, failures);
                }
                if (handle != null) handles[handleKey] = handle;
            }
            if (failures.Count != 0) return null;
            return new GeneratedSectorHandleTransitionPlan(records, handles.Values, snapshot);
        }

        private static GeneratedSectorRuntimeHandle Apply(
            GeneratedSectorRuntimeHandle handle,
            GeneratedSectorRuntimeState target,
            string reason,
            GeneratedColliderCacheEntry entry,
            ref GeneratedColliderCacheSnapshot snapshot,
            ICollection<GeneratedSectorHandleTransitionRecord> records,
            ICollection<GeneratedSectorStreamingFailure> failures)
        {
            var result = GeneratedSectorRuntimeHandleLifecycle.Transition(
                new GeneratedSectorRuntimeTransitionRequest(handle, target, handle.Sector,
                    snapshot, entry));
            var record = new GeneratedSectorHandleTransitionRecord(records.Count,
                GeneratedSectorCoordinate.FromRuntime(handle.Sector), reason, result);
            records.Add(record);
            if (!result.Success)
            {
                var reasonText = string.Join(";", result.Failures.Select(value => value.StableToken));
                Add(failures, GeneratedSectorStreamingFailureCode.ForbiddenTransition,
                    handle.Sector.ToString(), reasonText);
                return null;
            }
            snapshot = result.CacheSnapshot;
            return result.Handle;
        }

        private static GeneratedColliderCacheEntry FindEntry(
            GeneratedSectorRuntimeHandle handle,
            IEnumerable<GeneratedColliderCacheEntry> entries) => entries == null || handle == null
                ? null : entries.SingleOrDefault(value => value.IsCoherent &&
                    value.Key.Sector.Equals(handle.Sector) &&
                    value.Key.MutationRevision == handle.MutationRevision &&
                    string.Equals(value.Key.GeometryDigest, handle.GeometryDigest,
                        StringComparison.Ordinal) &&
                    string.Equals(value.Key.BakeDigest, handle.BakeDigest,
                        StringComparison.Ordinal) &&
                    string.Equals(value.Key.SeamDigest, handle.SeamDigest,
                        StringComparison.Ordinal) &&
                    string.Equals(value.Key.RegistryDigest, handle.RegistryDigest,
                        StringComparison.Ordinal));

        private static IEnumerable<GeneratedSectorCoordinate> EnumerateWindow(
            GeneratedSectorCoordinate center,
            int radius)
        {
            for (var y = center.Y - radius; y <= center.Y + radius; y++)
                for (var x = center.X - radius; x <= center.X + radius; x++)
                {
                    var coordinate = new GeneratedSectorCoordinate(x, y);
                    if (coordinate.IsInWorld) yield return coordinate;
                }
        }

        private static void Change(
            ICollection<GeneratedSectorWindowChange> changes,
            GeneratedSectorCoordinate coordinate,
            GeneratedSectorWindowChangeKind kind,
            GeneratedSectorRuntimeState source,
            GeneratedSectorRuntimeState target,
            GeneratedSectorRuntimeHandle handle) => changes.Add(new GeneratedSectorWindowChange(
                coordinate, kind, source, target, handle == null ? 0 : handle.MutationRevision,
                handle == null ? string.Empty : handle.DirtyReason));
        private static string Key(GeneratedSectorCoordinate coordinate) => coordinate == null
            ? "MISSING" : coordinate.ToString();
        private static string Key(GeneratedSectorIndexCoordinate coordinate) => coordinate == null
            ? "MISSING" : coordinate.ToString();
        private static void Add(
            ICollection<GeneratedSectorStreamingFailure> failures,
            GeneratedSectorStreamingFailureCode code,
            string subject,
            string reason) => failures.Add(new GeneratedSectorStreamingFailure(code, subject, reason));
        private static GeneratedSectorStreamingResult Failure(
            GeneratedSectorWindowRequest request,
            IEnumerable<GeneratedSectorStreamingFailure> failures) =>
            new GeneratedSectorStreamingResult(request, null, null, null, failures);
    }
}
