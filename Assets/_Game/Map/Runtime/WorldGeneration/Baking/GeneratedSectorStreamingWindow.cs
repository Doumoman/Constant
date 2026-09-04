using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedSectorCoordinate :
        IEquatable<GeneratedSectorCoordinate>, IComparable<GeneratedSectorCoordinate>
    {
        public GeneratedSectorCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInWorld => X >= 0 &&
            X < GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns && Y >= 0 &&
            Y < GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorRows;
        public int RowMajorIndex => Y * GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns + X;
        public GeneratedSectorIndexCoordinate ToRuntimeCoordinate() =>
            new GeneratedSectorIndexCoordinate(X, Y);
        public int ChebyshevDistance(GeneratedSectorCoordinate other) => other == null
            ? int.MaxValue : Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));
        public int CompareTo(GeneratedSectorCoordinate other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }
        public bool Equals(GeneratedSectorCoordinate other) => other != null &&
            X == other.X && Y == other.Y;
        public override bool Equals(object obj) => Equals(obj as GeneratedSectorCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }
        public override string ToString() => Number(X) + "," + Number(Y);
        public static GeneratedSectorCoordinate FromRuntime(GeneratedSectorIndexCoordinate value) =>
            value == null ? null : new GeneratedSectorCoordinate(value.X, value.Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public enum GeneratedSectorWindowKind
    {
        Preload = 1,
        Active = 2,
        Preactivation = 3,
        EvictCandidate = 4,
    }

    public sealed class GeneratedSectorWindowMember : IComparable<GeneratedSectorWindowMember>
    {
        public GeneratedSectorWindowMember(
            GeneratedSectorCoordinate coordinate,
            int distance,
            GeneratedSectorWindowKind kind,
            GeneratedSectorRuntimeState expectedState,
            GeneratedColliderCacheKey cacheKey)
        {
            Coordinate = coordinate;
            Distance = distance;
            Kind = kind;
            ExpectedState = expectedState;
            CacheKey = cacheKey;
            StableToken = string.Join("|", new[]
            {
                "SECTOR_WINDOW_MEMBER", Coordinate == null ? "MISSING" : Coordinate.ToString(),
                Number(Distance), Kind.ToString().ToUpperInvariant(),
                ExpectedState.ToString().ToUpperInvariant(),
                CacheKey == null ? "MISSING" : CacheKey.Digest,
            });
        }

        public GeneratedSectorCoordinate Coordinate { get; }
        public int Distance { get; }
        public GeneratedSectorWindowKind Kind { get; }
        public GeneratedSectorRuntimeState ExpectedState { get; }
        public GeneratedColliderCacheKey CacheKey { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedSectorWindowMember other)
        {
            if (other == null) return -1;
            var comparison = Coordinate == null
                ? (other.Coordinate == null ? 0 : 1)
                : Coordinate.CompareTo(other.Coordinate);
            return comparison != 0 ? comparison : Kind.CompareTo(other.Kind);
        }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedSectorPreactivationCandidate :
        IComparable<GeneratedSectorPreactivationCandidate>
    {
        internal GeneratedSectorPreactivationCandidate(
            GeneratedSectorCoordinate coordinate,
            GeneratedSectorDirectionHint direction,
            string reason,
            bool insideCurrentPreload,
            bool insideNextPreload,
            GeneratedColliderCacheKey cacheKey,
            GeneratedSectorRuntimeState expectedState)
        {
            Coordinate = coordinate;
            Direction = direction;
            Reason = reason ?? string.Empty;
            InsideCurrentPreload = insideCurrentPreload;
            InsideNextPreload = insideNextPreload;
            CacheKey = cacheKey;
            ExpectedState = expectedState;
            StableToken = string.Join("|", new[]
            {
                "PREACTIVATION_CANDIDATE", Coordinate == null ? "MISSING" : Coordinate.ToString(),
                Direction.ToString().ToUpperInvariant(), Reason,
                InsideCurrentPreload ? "1" : "0", InsideNextPreload ? "1" : "0",
                CacheKey == null ? "MISSING" : CacheKey.Digest,
                ExpectedState.ToString().ToUpperInvariant(),
            });
        }

        public GeneratedSectorCoordinate Coordinate { get; }
        public GeneratedSectorDirectionHint Direction { get; }
        public string Reason { get; }
        public bool InsideCurrentPreload { get; }
        public bool InsideNextPreload { get; }
        public bool IsInsideValidWindow => InsideCurrentPreload || InsideNextPreload;
        public GeneratedColliderCacheKey CacheKey { get; }
        public GeneratedSectorRuntimeState ExpectedState { get; }
        public bool SceneActivationExecuted => false;
        public string StableToken { get; }
        public int CompareTo(GeneratedSectorPreactivationCandidate other) => other == null ? -1 :
            Coordinate.CompareTo(other.Coordinate);
    }

    public sealed class GeneratedSectorWindowRequest
    {
        private readonly ReadOnlyCollection<GeneratedSectorRuntimeHandle> handles;
        private readonly ReadOnlyCollection<GeneratedColliderCacheEntry> cacheEntries;

        public GeneratedSectorWindowRequest(
            GeneratedSectorCoordinate center,
            double localProgressX,
            double localProgressY,
            GeneratedSectorDirectionHint directionHint,
            GeneratedSectorPreactivationPolicy preactivationPolicy,
            IEnumerable<GeneratedSectorRuntimeHandle> sourceHandles,
            IEnumerable<GeneratedColliderCacheEntry> sourceCacheEntries,
            GeneratedSectorStreamingWindow previousWindow = null,
            bool preactivationLatched = false)
        {
            Center = center;
            LocalProgressX = localProgressX;
            LocalProgressY = localProgressY;
            DirectionHint = directionHint;
            PreactivationPolicy = preactivationPolicy;
            PreviousWindow = previousWindow;
            PreactivationLatched = preactivationLatched;
            var rawHandles = (sourceHandles ?? Array.Empty<GeneratedSectorRuntimeHandle>()).ToArray();
            var rawEntries = (sourceCacheEntries ?? Array.Empty<GeneratedColliderCacheEntry>()).ToArray();
            NullHandleCount = rawHandles.Count(value => value == null);
            NullCacheEntryCount = rawEntries.Count(value => value == null);
            handles = new ReadOnlyCollection<GeneratedSectorRuntimeHandle>(rawHandles
                .Where(value => value != null).OrderBy(value => value.Sector)
                .ThenBy(value => value.Id.Value, StringComparer.Ordinal).ToArray());
            cacheEntries = new ReadOnlyCollection<GeneratedColliderCacheEntry>(rawEntries
                .Where(value => value != null).OrderBy(value => value).ToArray());
            CanonicalDigest = GeneratedSectorWindowDigest.ComputeRequest(this);
        }

        public GeneratedSectorCoordinate Center { get; }
        public double LocalProgressX { get; }
        public double LocalProgressY { get; }
        public GeneratedSectorDirectionHint DirectionHint { get; }
        public GeneratedSectorPreactivationPolicy PreactivationPolicy { get; }
        public IReadOnlyList<GeneratedSectorRuntimeHandle> Handles => handles;
        public IReadOnlyList<GeneratedColliderCacheEntry> CacheEntries => cacheEntries;
        public GeneratedSectorStreamingWindow PreviousWindow { get; }
        public bool PreactivationLatched { get; }
        public int NullHandleCount { get; }
        public int NullCacheEntryCount { get; }
        public string CanonicalDigest { get; }
    }

    public sealed class GeneratedSectorStreamingWindow
    {
        private readonly ReadOnlyCollection<GeneratedSectorWindowMember> preloadMembers;
        private readonly ReadOnlyCollection<GeneratedSectorWindowMember> activeMembers;
        private readonly ReadOnlyCollection<GeneratedSectorPreactivationCandidate> candidates;

        internal GeneratedSectorStreamingWindow(
            GeneratedSectorWindowRequest request,
            IEnumerable<GeneratedSectorWindowMember> sourcePreloadMembers,
            IEnumerable<GeneratedSectorWindowMember> sourceActiveMembers,
            IEnumerable<GeneratedSectorPreactivationCandidate> sourceCandidates)
        {
            Request = request;
            preloadMembers = new ReadOnlyCollection<GeneratedSectorWindowMember>((sourcePreloadMembers ??
                Array.Empty<GeneratedSectorWindowMember>()).OrderBy(value => value).ToArray());
            activeMembers = new ReadOnlyCollection<GeneratedSectorWindowMember>((sourceActiveMembers ??
                Array.Empty<GeneratedSectorWindowMember>()).OrderBy(value => value).ToArray());
            candidates = new ReadOnlyCollection<GeneratedSectorPreactivationCandidate>((sourceCandidates ??
                Array.Empty<GeneratedSectorPreactivationCandidate>()).OrderBy(value => value).ToArray());
            Digest = GeneratedSectorWindowDigest.ComputeWindow(this);
        }

        public const int PreloadRadius = 3;
        public const int ActiveRadius = 2;
        public const string PolicyVersion = "MAP17_04_SECTOR_STREAMING_WINDOW_V1";
        public const string DownstreamOwner = "MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE";
        public const bool OpensDownstreamTask = false;
        public GeneratedSectorWindowRequest Request { get; }
        public GeneratedSectorCoordinate Center => Request.Center;
        public IReadOnlyList<GeneratedSectorWindowMember> PreloadMembers => preloadMembers;
        public IReadOnlyList<GeneratedSectorWindowMember> ActiveMembers => activeMembers;
        public IReadOnlyList<GeneratedSectorPreactivationCandidate> PreactivationCandidates => candidates;
        public int PreloadCount => preloadMembers.Count;
        public int ActiveCount => activeMembers.Count;
        public int PreactivationCandidateCount => candidates.Count;
        public int DuplicatePreloadMemberCount => preloadMembers.Count - preloadMembers
            .Select(value => value.Coordinate).Distinct().Count();
        public int DuplicateActiveMemberCount => activeMembers.Count - activeMembers
            .Select(value => value.Coordinate).Distinct().Count();
        public int OutOfWorldPreloadMemberCount => preloadMembers.Count(value =>
            value.Coordinate == null || !value.Coordinate.IsInWorld);
        public int OutOfWorldActiveMemberCount => activeMembers.Count(value =>
            value.Coordinate == null || !value.Coordinate.IsInWorld);
        public int ActiveOutsidePreloadCount => activeMembers.Count(value => !preloadMembers.Any(preload =>
            preload.Coordinate.Equals(value.Coordinate)));
        public bool ActiveIsSubsetOfPreload => ActiveOutsidePreloadCount == 0;
        public int ExecutedSceneActivationCount => candidates.Count(value => value.SceneActivationExecuted);
        public string Digest { get; }
        public bool ContainsPreload(GeneratedSectorCoordinate coordinate) => coordinate != null &&
            preloadMembers.Any(value => value.Coordinate.Equals(coordinate));
        public bool ContainsActive(GeneratedSectorCoordinate coordinate) => coordinate != null &&
            activeMembers.Any(value => value.Coordinate.Equals(coordinate));
    }

    public enum GeneratedSectorWindowChangeKind
    {
        AddPreload = 1,
        RemovePreload = 2,
        PromotePreloadToActive = 3,
        DemoteActiveToPreload = 4,
        PreserveActive = 5,
        PreservePreload = 6,
        EvictCandidate = 7,
        PreserveSleepingModified = 8,
    }

    public sealed class GeneratedSectorWindowChange : IComparable<GeneratedSectorWindowChange>
    {
        internal GeneratedSectorWindowChange(
            GeneratedSectorCoordinate coordinate,
            GeneratedSectorWindowChangeKind kind,
            GeneratedSectorRuntimeState sourceState,
            GeneratedSectorRuntimeState targetState,
            int mutationRevision,
            string dirtyReason)
        {
            Coordinate = coordinate;
            Kind = kind;
            SourceState = sourceState;
            TargetState = targetState;
            MutationRevision = mutationRevision;
            DirtyReason = dirtyReason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "SECTOR_WINDOW_CHANGE", Coordinate == null ? "MISSING" : Coordinate.ToString(),
                Kind.ToString().ToUpperInvariant(), SourceState.ToString().ToUpperInvariant(),
                TargetState.ToString().ToUpperInvariant(), Number(MutationRevision), DirtyReason,
            });
        }

        public GeneratedSectorCoordinate Coordinate { get; }
        public GeneratedSectorWindowChangeKind Kind { get; }
        public GeneratedSectorRuntimeState SourceState { get; }
        public GeneratedSectorRuntimeState TargetState { get; }
        public int MutationRevision { get; }
        public string DirtyReason { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedSectorWindowChange other)
        {
            if (other == null) return -1;
            var comparison = Coordinate.CompareTo(other.Coordinate);
            return comparison != 0 ? comparison : Kind.CompareTo(other.Kind);
        }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedSectorWindowDiff
    {
        private readonly ReadOnlyCollection<GeneratedSectorWindowChange> changes;

        internal GeneratedSectorWindowDiff(
            GeneratedSectorStreamingWindow previous,
            GeneratedSectorStreamingWindow next,
            IEnumerable<GeneratedSectorWindowChange> sourceChanges)
        {
            Previous = previous;
            Next = next;
            changes = new ReadOnlyCollection<GeneratedSectorWindowChange>((sourceChanges ??
                Array.Empty<GeneratedSectorWindowChange>()).OrderBy(value => value).ToArray());
            Digest = GeneratedSectorWindowDigest.ComputeDiff(this);
        }

        public GeneratedSectorStreamingWindow Previous { get; }
        public GeneratedSectorStreamingWindow Next { get; }
        public IReadOnlyList<GeneratedSectorWindowChange> Changes => changes;
        public int AddPreloadCount => Count(GeneratedSectorWindowChangeKind.AddPreload);
        public int RemovePreloadCount => Count(GeneratedSectorWindowChangeKind.RemovePreload);
        public int PromoteCount => Count(GeneratedSectorWindowChangeKind.PromotePreloadToActive);
        public int DemoteCount => Count(GeneratedSectorWindowChangeKind.DemoteActiveToPreload);
        public int PreserveActiveCount => Count(GeneratedSectorWindowChangeKind.PreserveActive);
        public int PreservePreloadCount => Count(GeneratedSectorWindowChangeKind.PreservePreload);
        public int EvictCandidateCount => Count(GeneratedSectorWindowChangeKind.EvictCandidate);
        public int PreserveSleepingModifiedCount =>
            Count(GeneratedSectorWindowChangeKind.PreserveSleepingModified);
        public string Digest { get; }
        private int Count(GeneratedSectorWindowChangeKind kind) =>
            changes.Count(value => value.Kind == kind);
    }

    public sealed class GeneratedSectorHandleTransitionRecord :
        IComparable<GeneratedSectorHandleTransitionRecord>
    {
        internal GeneratedSectorHandleTransitionRecord(
            int ordinal,
            GeneratedSectorCoordinate coordinate,
            string reason,
            GeneratedSectorRuntimeHandleResult lifecycleResult)
        {
            Ordinal = ordinal;
            Coordinate = coordinate;
            Reason = reason ?? string.Empty;
            LifecycleResult = lifecycleResult;
            StableToken = string.Join("|", new[]
            {
                "SECTOR_HANDLE_TRANSITION", Number(Ordinal),
                Coordinate == null ? "MISSING" : Coordinate.ToString(), Reason,
                lifecycleResult == null || lifecycleResult.Transition == null
                    ? "MISSING" : lifecycleResult.Transition.StableToken,
                lifecycleResult == null || lifecycleResult.Handle == null
                    ? "MISSING" : lifecycleResult.Handle.Digest,
            });
        }

        public int Ordinal { get; }
        public GeneratedSectorCoordinate Coordinate { get; }
        public string Reason { get; }
        public GeneratedSectorRuntimeHandleResult LifecycleResult { get; }
        public bool Success => LifecycleResult != null && LifecycleResult.Success;
        public bool SceneActivationExecuted => false;
        public string StableToken { get; }
        public int CompareTo(GeneratedSectorHandleTransitionRecord other) => other == null ? -1 :
            Ordinal.CompareTo(other.Ordinal);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedSectorHandleTransitionPlan
    {
        private readonly ReadOnlyCollection<GeneratedSectorHandleTransitionRecord> records;
        private readonly ReadOnlyCollection<GeneratedSectorRuntimeHandle> finalHandles;

        internal GeneratedSectorHandleTransitionPlan(
            IEnumerable<GeneratedSectorHandleTransitionRecord> sourceRecords,
            IEnumerable<GeneratedSectorRuntimeHandle> sourceFinalHandles,
            GeneratedColliderCacheSnapshot cacheSnapshot)
        {
            records = new ReadOnlyCollection<GeneratedSectorHandleTransitionRecord>((sourceRecords ??
                Array.Empty<GeneratedSectorHandleTransitionRecord>()).OrderBy(value => value).ToArray());
            finalHandles = new ReadOnlyCollection<GeneratedSectorRuntimeHandle>((sourceFinalHandles ??
                Array.Empty<GeneratedSectorRuntimeHandle>()).Where(value => value != null)
                .OrderBy(value => value.Sector).ThenBy(value => value.Id.Value,
                    StringComparer.Ordinal).ToArray());
            CacheSnapshot = cacheSnapshot ?? GeneratedColliderCacheSnapshot.Empty;
            Digest = GeneratedSectorWindowDigest.ComputeTransitionPlan(this);
        }

        public IReadOnlyList<GeneratedSectorHandleTransitionRecord> Records => records;
        public IReadOnlyList<GeneratedSectorRuntimeHandle> FinalHandles => finalHandles;
        public GeneratedColliderCacheSnapshot CacheSnapshot { get; }
        public int RecordCount => records.Count;
        public int SuccessfulRecordCount => records.Count(value => value.Success);
        public int FailedRecordCount => records.Count - SuccessfulRecordCount;
        public int SceneActivationExecutionCount => records.Count(value => value.SceneActivationExecuted);
        public int DurableSaveWriteCount => finalHandles.Sum(value => value.DurableSaveWriteCount);
        public int TilemapComponentWriteCount => 0;
        public int ColliderCreationCount => 0;
        public int RigidbodyCreationCount => 0;
        public int PhysicsQueryCount => 0;
        public int PhysicsSimulationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int TilemapMutationCount => 0;
        public int GameObjectInstantiationCount => 0;
        public int GameObjectEnableCount => 0;
        public int GameObjectDisableCount => 0;
        public int GameObjectDestroyCount => 0;
        public int CameraReadCount => 0;
        public int CameraWriteCount => 0;
        public int CinemachineIntegrationCount => 0;
        public int AddressablesLoadCount => 0;
        public int ResourcesLoadCount => 0;
        public int AssetDatabaseLoadCount => 0;
        public int GeneratedCsvCommitCount => 0;
        public int GeneratedAssetCommitCount => 0;
        public int StableSpawnIdCount => 0;
        public int RuntimeObjectSpawnCount => 0;
        public int ProductionSeedApprovalCount => 0;
        public string Digest { get; }
    }

    public enum GeneratedSectorStreamingFailureCode
    {
        MissingRequest = 1,
        InvalidCenter = 2,
        InvalidLocalProgress = 3,
        InvalidPolicy = 4,
        NullSource = 5,
        DuplicateHandle = 6,
        DuplicateCacheEntry = 7,
        MissingHandle = 8,
        MissingCache = 9,
        InvalidHandle = 10,
        ForbiddenTransition = 11,
        InvalidWindow = 12,
        InvalidDigest = 13,
    }

    public sealed class GeneratedSectorStreamingFailure :
        IEquatable<GeneratedSectorStreamingFailure>, IComparable<GeneratedSectorStreamingFailure>
    {
        public GeneratedSectorStreamingFailure(
            GeneratedSectorStreamingFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedSectorStreamingFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public string StableToken => Code + "|" + Subject + "|" + Reason;
        public int CompareTo(GeneratedSectorStreamingFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedSectorStreamingFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as GeneratedSectorStreamingFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedSectorStreamingResult
    {
        private readonly ReadOnlyCollection<GeneratedSectorStreamingFailure> failures;

        internal GeneratedSectorStreamingResult(
            GeneratedSectorWindowRequest request,
            GeneratedSectorStreamingWindow window,
            GeneratedSectorWindowDiff diff,
            GeneratedSectorHandleTransitionPlan transitionPlan,
            IEnumerable<GeneratedSectorStreamingFailure> sourceFailures)
        {
            Request = request;
            Window = window;
            Diff = diff;
            TransitionPlan = transitionPlan;
            failures = new ReadOnlyCollection<GeneratedSectorStreamingFailure>((sourceFailures ??
                Array.Empty<GeneratedSectorStreamingFailure>()).Distinct()
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Window != null && Diff != null && TransitionPlan != null &&
            failures.Count == 0;
        public GeneratedSectorWindowRequest Request { get; }
        public GeneratedSectorStreamingWindow Window { get; }
        public GeneratedSectorWindowDiff Diff { get; }
        public GeneratedSectorHandleTransitionPlan TransitionPlan { get; }
        public IReadOnlyList<GeneratedSectorStreamingFailure> Failures => failures;
    }

    public static class GeneratedSectorWindowDigest
    {
        public static string ComputeRequest(GeneratedSectorWindowRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "WINDOW_POLICY|" + GeneratedSectorStreamingWindow.PolicyVersion,
                "CENTER|" + (request.Center == null ? "MISSING" : request.Center.ToString()),
                "PROGRESS|" + Number(request.LocalProgressX) + "|" +
                    Number(request.LocalProgressY),
                "DIRECTION|" + request.DirectionHint.ToString().ToUpperInvariant(),
                "PREACTIVATION|" + (request.PreactivationPolicy == null
                    ? "MISSING" : request.PreactivationPolicy.StableToken),
                "LATCHED|" + (request.PreactivationLatched ? "1" : "0"),
                "PREVIOUS|" + (request.PreviousWindow == null
                    ? "NONE" : request.PreviousWindow.Digest),
                "NULLS|" + Number(request.NullHandleCount) + "|" +
                    Number(request.NullCacheEntryCount),
            };
            lines.AddRange(request.Handles.OrderBy(value => value.Sector)
                .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                .Select(value => "HANDLE|" + value.Digest));
            lines.AddRange(request.CacheEntries.OrderBy(value => value)
                .Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static string ComputeWindow(GeneratedSectorStreamingWindow window)
        {
            if (window == null) return string.Empty;
            var lines = new List<string>
            {
                "WINDOW_POLICY|" + GeneratedSectorStreamingWindow.PolicyVersion,
                "REQUEST|" + window.Request.CanonicalDigest,
                "COUNTS|" + Number(window.PreloadCount) + "|" + Number(window.ActiveCount) + "|" +
                    Number(window.PreactivationCandidateCount),
            };
            lines.AddRange(window.PreloadMembers.OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange(window.ActiveMembers.OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange(window.PreactivationCandidates.OrderBy(value => value)
                .Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static string ComputeDiff(GeneratedSectorWindowDiff diff)
        {
            if (diff == null) return string.Empty;
            var lines = new List<string>
            {
                "WINDOW_DIFF|" + (diff.Previous == null ? "NONE" : diff.Previous.Digest) + "|" +
                    (diff.Next == null ? "MISSING" : diff.Next.Digest),
            };
            lines.AddRange(diff.Changes.OrderBy(value => value).Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static string ComputeTransitionPlan(GeneratedSectorHandleTransitionPlan plan)
        {
            if (plan == null) return string.Empty;
            var lines = new List<string>
            {
                "TRANSITION_PLAN|" + Number(plan.RecordCount) + "|" +
                    Number(plan.SuccessfulRecordCount) + "|" + Number(plan.FailedRecordCount),
                "CACHE|" + plan.CacheSnapshot.Digest,
            };
            lines.AddRange(plan.Records.OrderBy(value => value).Select(value => value.StableToken));
            lines.AddRange(plan.FinalHandles.OrderBy(value => value.Sector)
                .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                .Select(value => "FINAL_HANDLE|" + value.Digest));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
