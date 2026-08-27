using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public readonly struct MicroPatternSelectionRequestId :
        IEquatable<MicroPatternSelectionRequestId>,
        IComparable<MicroPatternSelectionRequestId>
    {
        private readonly string value;

        public MicroPatternSelectionRequestId(string value)
        {
            this.value = value;
        }

        public string Value => value ?? string.Empty;

        public int CompareTo(MicroPatternSelectionRequestId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternSelectionRequestId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is MicroPatternSelectionRequestId other && Equals(other);
        }

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public sealed class MicroPatternSelectionRequest
    {
        public MicroPatternSelectionRequest(
            MicroPatternSelectionRequestId requestId,
            MicroPatternCandidateIndex candidateIndex)
        {
            RequestId = requestId;
            CandidateIndex = candidateIndex;
        }

        public MicroPatternSelectionRequestId RequestId { get; }
        public MicroPatternCandidateIndex CandidateIndex { get; }
    }

    public sealed class MicroPatternSelectionDecision
    {
        internal MicroPatternSelectionDecision(
            MicroPatternSelectionRequestId requestId,
            string candidateIndexDigest,
            MicroPatternCandidateKey chosenKey,
            int chosenCandidateOrdinal,
            int totalWeight,
            int ticket,
            ulong initialState,
            ulong drawCountBefore,
            ulong drawCountAfter)
        {
            RequestId = requestId;
            CandidateIndexDigest = candidateIndexDigest;
            ChosenKey = chosenKey;
            ChosenCandidateOrdinal = chosenCandidateOrdinal;
            TotalWeight = totalWeight;
            Ticket = ticket;
            InitialState = initialState;
            DrawCountBefore = drawCountBefore;
            DrawCountAfter = drawCountAfter;
        }

        public MicroPatternSelectionRequestId RequestId { get; }
        public string CandidateIndexDigest { get; }
        public MicroPatternCandidateKey ChosenKey { get; }
        public int ChosenCandidateOrdinal { get; }
        public int TotalWeight { get; }
        public int Ticket { get; }
        public ulong InitialState { get; }
        public ulong DrawCountBefore { get; }
        public ulong DrawCountAfter { get; }
    }

    public enum MicroPatternSelectionBatchErrorCode
    {
        MissingInput = 1,
        InvalidRequestId = 2,
        DuplicateRequestId = 3,
        MissingCandidateIndex = 4,
        EmptyCandidateIndex = 5,
        InvalidCandidateIndexDigest = 6,
        InvalidCandidateOrder = 7,
        InvalidCandidateWeight = 8,
        TotalWeightOverflow = 9,
        InvalidScope = 10,
        InvalidRngDefinition = 11,
    }

    public sealed class MicroPatternSelectionBatchError :
        IEquatable<MicroPatternSelectionBatchError>,
        IComparable<MicroPatternSelectionBatchError>
    {
        public MicroPatternSelectionBatchError(
            MicroPatternSelectionBatchErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternSelectionBatchErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternSelectionBatchError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternSelectionBatchError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj) => Equals(obj as MicroPatternSelectionBatchError);
        public override int GetHashCode() => ToString().GetHashCode();
        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class MicroPatternSelectionBatchResult
    {
        private readonly ReadOnlyCollection<MicroPatternSelectionDecision> decisions;
        private readonly ReadOnlyCollection<MicroPatternSelectionBatchError> errors;

        internal MicroPatternSelectionBatchResult(
            IEnumerable<MicroPatternSelectionDecision> decisions,
            IEnumerable<MicroPatternSelectionBatchError> errors,
            string registeredStreamId,
            RngResetScope resetScope,
            string scopeIdentity,
            int attemptOrdinal,
            bool streamCreated,
            ulong initialState,
            ulong finalDrawCount,
            string stableDigest)
        {
            var errorCopy = (errors ?? Array.Empty<MicroPatternSelectionBatchError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternSelectionBatchError>(errorCopy);
            var decisionCopy = errorCopy.Length == 0
                ? (decisions ?? Array.Empty<MicroPatternSelectionDecision>())
                    .OrderBy(value => value.RequestId)
                    .ToArray()
                : Array.Empty<MicroPatternSelectionDecision>();
            this.decisions = new ReadOnlyCollection<MicroPatternSelectionDecision>(decisionCopy);
            StreamCreated = errorCopy.Length == 0 && streamCreated;
            RegisteredStreamId = StreamCreated ? registeredStreamId ?? string.Empty : string.Empty;
            ResetScope = resetScope;
            ScopeIdentity = StreamCreated ? scopeIdentity ?? string.Empty : string.Empty;
            AttemptOrdinal = StreamCreated ? attemptOrdinal : 0;
            InitialState = StreamCreated ? initialState : 0UL;
            FinalDrawCount = StreamCreated ? finalDrawCount : 0UL;
            StableDigest = errorCopy.Length == 0 ? stableDigest ?? string.Empty : string.Empty;
        }

        public bool Success => errors.Count == 0 && StreamCreated && decisions.Count != 0;
        public IReadOnlyList<MicroPatternSelectionDecision> Decisions => decisions;
        public IReadOnlyList<MicroPatternSelectionBatchError> Errors => errors;
        public bool StreamCreated { get; }
        public string RegisteredStreamId { get; }
        public RngResetScope ResetScope { get; }
        public string ScopeIdentity { get; }
        public int AttemptOrdinal { get; }
        public ulong InitialState { get; }
        public ulong FinalDrawCount { get; }
        public string StableDigest { get; }
    }

    public sealed class MicroPatternDeterministicSelector
    {
        public const string RulesetVersion = "MAP10_04_SELECTION_V1";
        public const string StreamId = WorldGenerationRngStreams.SectorRecipeStreamId;

        private readonly DeterministicRngStreamFactory streamFactory;

        public MicroPatternDeterministicSelector(WorldRouteDefinitionSet rngDefinitions)
        {
            streamFactory = new DeterministicRngStreamFactory(rngDefinitions);
        }

        public MicroPatternSelectionBatchResult Select(
            ulong worldSeed,
            SectorCoord sector,
            int attemptOrdinal,
            IEnumerable<MicroPatternSelectionRequest> requests)
        {
            var errors = new List<MicroPatternSelectionBatchError>();
            if (requests == null)
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.MissingInput,
                    "requests",
                    "Selection requests are required."));
                return Reject(errors);
            }

            var values = requests.ToArray();
            if (values.Length == 0)
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.MissingInput,
                    "requests",
                    "At least one selection request is required."));
            }

            foreach (var request in values)
            {
                ValidateRequest(request, errors);
            }

            foreach (var duplicate in values
                         .Where(value => value != null)
                         .GroupBy(value => value.RequestId.Value, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.DuplicateRequestId,
                    "requests[" + duplicate.Key + "]",
                    duplicate.Key));
            }

            if (errors.Count != 0) return Reject(errors);

            RngStreamScope scope;
            try
            {
                scope = RngStreamScope.Sector(sector, attemptOrdinal);
            }
            catch (Exception exception)
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.InvalidScope,
                    "scope",
                    exception.GetType().Name + ":" + exception.Message));
                return Reject(errors);
            }

            DeterministicRngStream stream;
            try
            {
                stream = streamFactory.Create(StreamId, worldSeed, scope);
            }
            catch (Exception exception)
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.InvalidRngDefinition,
                    "rngDefinitions[" + StreamId + "]",
                    exception.GetType().Name + ":" + exception.Message));
                return Reject(errors);
            }

            var decisions = new List<MicroPatternSelectionDecision>();
            foreach (var request in values.OrderBy(value => value.RequestId))
            {
                var totalWeight = checked((int)request.CandidateIndex.TotalWeight);
                var before = stream.DrawCount;
                var ticket = stream.NextInt(totalWeight);
                var chosenOrdinal = MicroPatternWeightedTicket.Resolve(
                    request.CandidateIndex.Candidates,
                    ticket);
                var after = stream.DrawCount;
                decisions.Add(new MicroPatternSelectionDecision(
                    request.RequestId,
                    request.CandidateIndex.StableDigest,
                    request.CandidateIndex.Candidates[chosenOrdinal].Key,
                    chosenOrdinal,
                    totalWeight,
                    ticket,
                    stream.InitialState,
                    before,
                    after));
            }

            var digest = MicroPatternSelectionCanonicalDigest.Compute(
                worldSeed,
                scope,
                stream.InitialState,
                stream.DrawCount,
                values,
                decisions);
            return new MicroPatternSelectionBatchResult(
                decisions,
                errors,
                StreamId,
                scope.ResetScope,
                scope.Identity,
                scope.AttemptOrdinal,
                true,
                stream.InitialState,
                stream.DrawCount,
                digest);
        }

        private static void ValidateRequest(
            MicroPatternSelectionRequest request,
            ICollection<MicroPatternSelectionBatchError> errors)
        {
            if (request == null)
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.MissingInput,
                    "requests",
                    "Request entry is required."));
                return;
            }

            var id = request.RequestId.Value;
            var path = "requests[" + id + "]";
            if (!IsRequestId(id))
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.InvalidRequestId,
                    path,
                    id));
            }

            var index = request.CandidateIndex;
            if (index == null)
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.MissingCandidateIndex,
                    path + ".index",
                    "Candidate index is required."));
                return;
            }

            if (!MicroPatternContractDigest.IsLowerHexDigest(index.StableDigest))
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.InvalidCandidateIndexDigest,
                    path + ".index.stableDigest",
                    index.StableDigest));
            }

            if (index.Candidates.Count == 0)
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.EmptyCandidateIndex,
                    path + ".index.candidates",
                    "No eligible candidate exists."));
                return;
            }

            long sum = 0;
            MicroPatternCandidateKey? previous = null;
            foreach (var candidate in index.Candidates)
            {
                if (candidate == null ||
                    candidate.Weight < MicroPatternDefinition.MinimumWeight ||
                    candidate.Weight > MicroPatternDefinition.MaximumWeight)
                {
                    errors.Add(Error(
                        MicroPatternSelectionBatchErrorCode.InvalidCandidateWeight,
                        path + ".index.candidates",
                        candidate == null
                            ? "<null>"
                            : candidate.Weight.ToString(CultureInfo.InvariantCulture)));
                    continue;
                }

                if (previous.HasValue && previous.Value.CompareTo(candidate.Key) >= 0)
                {
                    errors.Add(Error(
                        MicroPatternSelectionBatchErrorCode.InvalidCandidateOrder,
                        path + ".index.candidates",
                        candidate.Key.CanonicalValue));
                }
                previous = candidate.Key;

                try
                {
                    sum = checked(sum + candidate.Weight);
                }
                catch (OverflowException)
                {
                    errors.Add(Error(
                        MicroPatternSelectionBatchErrorCode.TotalWeightOverflow,
                        path + ".index.totalWeight",
                        "Int64 overflow."));
                }
            }

            if (sum < 1 || sum > int.MaxValue || sum != index.TotalWeight)
            {
                errors.Add(Error(
                    MicroPatternSelectionBatchErrorCode.TotalWeightOverflow,
                    path + ".index.totalWeight",
                    sum.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private static bool IsRequestId(string value)
        {
            const string prefix = "MPS_";
            if (value == null || !value.StartsWith(prefix, StringComparison.Ordinal) ||
                value.Length == prefix.Length) return false;
            for (var index = prefix.Length; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') &&
                    character != '_') return false;
            }
            return true;
        }

        private static MicroPatternSelectionBatchResult Reject(
            IEnumerable<MicroPatternSelectionBatchError> errors)
        {
            return new MicroPatternSelectionBatchResult(
                Array.Empty<MicroPatternSelectionDecision>(),
                errors,
                string.Empty,
                default,
                string.Empty,
                0,
                false,
                0UL,
                0UL,
                string.Empty);
        }

        private static MicroPatternSelectionBatchError Error(
            MicroPatternSelectionBatchErrorCode code,
            string path,
            string detail)
        {
            return new MicroPatternSelectionBatchError(code, path, detail);
        }
    }

    public static class MicroPatternWeightedTicket
    {
        public static int Resolve(
            IReadOnlyList<MicroPatternCandidate> candidates,
            int ticket)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new ArgumentException("At least one candidate is required.", nameof(candidates));
            }

            long total = 0;
            foreach (var candidate in candidates)
            {
                if (candidate == null || candidate.Weight <= 0)
                {
                    throw new ArgumentException("Every candidate must have a positive weight.", nameof(candidates));
                }
                total = checked(total + candidate.Weight);
            }

            if (total > int.MaxValue || ticket < 0 || ticket >= total)
            {
                throw new ArgumentOutOfRangeException(nameof(ticket));
            }

            long cumulative = 0;
            for (var index = 0; index < candidates.Count; index++)
            {
                cumulative += candidates[index].Weight;
                if (ticket < cumulative) return index;
            }

            throw new InvalidOperationException("Weighted ticket did not resolve to a candidate.");
        }
    }

    public static class MicroPatternSelectionCanonicalDigest
    {
        internal static string Compute(
            ulong worldSeed,
            RngStreamScope scope,
            ulong initialState,
            ulong finalDrawCount,
            IEnumerable<MicroPatternSelectionRequest> requests,
            IEnumerable<MicroPatternSelectionDecision> decisions)
        {
            var material = new StringBuilder();
            MicroPatternContractDigest.Append(
                material,
                "RULESET",
                MicroPatternDeterministicSelector.RulesetVersion);
            MicroPatternContractDigest.Append(
                material,
                "SESSION",
                worldSeed.ToString(CultureInfo.InvariantCulture),
                MicroPatternDeterministicSelector.StreamId,
                RngResetScopeToken.Format(scope.ResetScope),
                scope.Identity,
                scope.AttemptOrdinal.ToString(CultureInfo.InvariantCulture),
                initialState.ToString("x16", CultureInfo.InvariantCulture),
                finalDrawCount.ToString(CultureInfo.InvariantCulture));

            foreach (var request in requests.OrderBy(value => value.RequestId))
            {
                MicroPatternContractDigest.Append(
                    material,
                    "REQUEST",
                    request.RequestId.Value,
                    request.CandidateIndex.StableDigest);
            }

            foreach (var decision in decisions.OrderBy(value => value.RequestId))
            {
                MicroPatternContractDigest.Append(
                    material,
                    "DECISION",
                    decision.RequestId.Value,
                    decision.CandidateIndexDigest,
                    decision.ChosenKey.CanonicalValue,
                    decision.ChosenCandidateOrdinal.ToString(CultureInfo.InvariantCulture),
                    decision.TotalWeight.ToString(CultureInfo.InvariantCulture),
                    decision.Ticket.ToString(CultureInfo.InvariantCulture),
                    decision.InitialState.ToString("x16", CultureInfo.InvariantCulture),
                    decision.DrawCountBefore.ToString(CultureInfo.InvariantCulture),
                    decision.DrawCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            return MicroPatternContractDigest.Hash(material);
        }
    }
}
