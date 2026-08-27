using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public sealed class MicroPatternAcceptedHistoryItem
    {
        public MicroPatternAcceptedHistoryItem(
            long placementSequence,
            string placementId,
            MicroPatternId patternId,
            MicroPatternSilhouetteSignature silhouetteSignature)
        {
            PlacementSequence = placementSequence;
            PlacementId = placementId ?? string.Empty;
            PatternId = patternId;
            SilhouetteSignature = silhouetteSignature;
        }

        public long PlacementSequence { get; }
        public string PlacementId { get; }
        public MicroPatternId PatternId { get; }
        public MicroPatternSilhouetteSignature SilhouetteSignature { get; }
    }

    public sealed class MicroPatternRepetitionContext
    {
        private readonly ReadOnlyCollection<MicroPatternAcceptedHistoryItem> acceptedHistory;

        public MicroPatternRepetitionContext(
            IEnumerable<MicroPatternAcceptedHistoryItem> acceptedHistory)
        {
            var copy = acceptedHistory == null
                ? Array.Empty<MicroPatternAcceptedHistoryItem>()
                : acceptedHistory.ToArray();
            Array.Sort(copy, CompareHistory);
            this.acceptedHistory = new ReadOnlyCollection<MicroPatternAcceptedHistoryItem>(copy);
        }

        public IReadOnlyList<MicroPatternAcceptedHistoryItem> AcceptedHistory => acceptedHistory;

        private static int CompareHistory(
            MicroPatternAcceptedHistoryItem left,
            MicroPatternAcceptedHistoryItem right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            var comparison = left.PlacementSequence.CompareTo(right.PlacementSequence);
            return comparison != 0
                ? comparison
                : string.Compare(left.PlacementId, right.PlacementId, StringComparison.Ordinal);
        }
    }

    public enum MicroPatternRepetitionErrorCode
    {
        MissingInput = 1,
        InvalidHistory = 2,
        DuplicateHistoryPlacement = 3,
        InvalidCandidateSource = 4,
        InvalidApplicationPlan = 5,
        NoCandidateAfterThirdRepeatGuard = 6,
    }

    public sealed class MicroPatternRepetitionGuardError :
        IEquatable<MicroPatternRepetitionGuardError>,
        IComparable<MicroPatternRepetitionGuardError>
    {
        public MicroPatternRepetitionGuardError(
            MicroPatternRepetitionErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternRepetitionErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternRepetitionGuardError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternRepetitionGuardError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternRepetitionGuardError);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Code + "|" + Path + "|" + Detail;
        }
    }

    public sealed class MicroPatternRepetitionExclusion :
        IEquatable<MicroPatternRepetitionExclusion>,
        IComparable<MicroPatternRepetitionExclusion>
    {
        internal MicroPatternRepetitionExclusion(
            MicroPatternId patternId,
            MicroPatternTransform transform,
            string applicationPlanDigest,
            string silhouetteDigest,
            IEnumerable<string> historyPlacementIds)
        {
            PatternId = patternId;
            Transform = transform;
            ApplicationPlanDigest = applicationPlanDigest ?? string.Empty;
            SilhouetteDigest = silhouetteDigest ?? string.Empty;
            var copy = (historyPlacementIds ?? Array.Empty<string>())
                .Where(value => value != null)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            HistoryPlacementIds = new ReadOnlyCollection<string>(copy);
        }

        public MicroPatternId PatternId { get; }
        public MicroPatternTransform Transform { get; }
        public string ApplicationPlanDigest { get; }
        public string SilhouetteDigest { get; }
        public IReadOnlyList<string> HistoryPlacementIds { get; }
        public string SourceIdentity => PatternId.Value + ":" + Transform + ":" + ApplicationPlanDigest;

        public int CompareTo(MicroPatternRepetitionExclusion other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(SilhouetteDigest, other.SilhouetteDigest, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(
                    string.Join(",", HistoryPlacementIds),
                    string.Join(",", other.HistoryPlacementIds),
                    StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternRepetitionExclusion other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternRepetitionExclusion);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return SourceIdentity + "|" + SilhouetteDigest + "|" +
                   string.Join(",", HistoryPlacementIds);
        }
    }

    public sealed class MicroPatternRepetitionGuardResult
    {
        private readonly ReadOnlyCollection<MicroPatternCandidateSource> allowedSources;
        private readonly ReadOnlyCollection<MicroPatternRepetitionExclusion> exclusions;
        private readonly ReadOnlyCollection<MicroPatternRepetitionGuardError> errors;

        internal MicroPatternRepetitionGuardResult(
            IEnumerable<MicroPatternCandidateSource> allowedSources,
            IEnumerable<MicroPatternRepetitionExclusion> exclusions,
            IEnumerable<MicroPatternRepetitionGuardError> errors,
            string stableDigest)
        {
            var errorCopy = (errors ?? Array.Empty<MicroPatternRepetitionGuardError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternRepetitionGuardError>(errorCopy);
            var allowedCopy = errorCopy.Length == 0
                ? (allowedSources ?? Array.Empty<MicroPatternCandidateSource>()).ToArray()
                : Array.Empty<MicroPatternCandidateSource>();
            this.allowedSources = new ReadOnlyCollection<MicroPatternCandidateSource>(allowedCopy);
            var exclusionCopy = (exclusions ?? Array.Empty<MicroPatternRepetitionExclusion>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.exclusions = new ReadOnlyCollection<MicroPatternRepetitionExclusion>(exclusionCopy);
            StableDigest = errorCopy.Length == 0 ? stableDigest ?? string.Empty : string.Empty;
        }

        public bool Success => errors.Count == 0;
        public IReadOnlyList<MicroPatternCandidateSource> AllowedSources => allowedSources;
        public IReadOnlyList<MicroPatternRepetitionExclusion> Exclusions => exclusions;
        public IReadOnlyList<MicroPatternRepetitionGuardError> Errors => errors;
        public string StableDigest { get; }
    }

    public static class MicroPatternThirdRepeatGuard
    {
        public static MicroPatternRepetitionGuardResult Filter(
            MicroPatternRepetitionContext context,
            IEnumerable<MicroPatternCandidateSource> candidateSources)
        {
            var errors = new List<MicroPatternRepetitionGuardError>();
            if (context == null)
            {
                errors.Add(Error(
                    MicroPatternRepetitionErrorCode.MissingInput,
                    "context",
                    "Repetition context is required."));
            }
            if (candidateSources == null)
            {
                errors.Add(Error(
                    MicroPatternRepetitionErrorCode.MissingInput,
                    "candidateSources",
                    "Candidate sources are required."));
            }
            if (errors.Count != 0)
            {
                return Result(null, null, errors, null, null);
            }

            var history = context.AcceptedHistory.ToArray();
            ValidateHistory(history, errors);

            var candidates = new List<CandidateEvidence>();
            foreach (var source in candidateSources)
            {
                ValidateCandidate(source, candidates, errors);
            }

            if (errors.Count != 0)
            {
                return Result(null, null, errors, history, candidates);
            }

            candidates.Sort((left, right) =>
                string.Compare(left.Identity, right.Identity, StringComparison.Ordinal));
            MicroPatternId? blockedPatternId = null;
            MicroPatternAcceptedHistoryItem[] repeatedHistory = Array.Empty<MicroPatternAcceptedHistoryItem>();
            if (history.Length >= 2)
            {
                repeatedHistory = history.Skip(history.Length - 2).ToArray();
                if (repeatedHistory[0].PatternId == repeatedHistory[1].PatternId)
                {
                    blockedPatternId = repeatedHistory[0].PatternId;
                }
            }

            var allowed = new List<MicroPatternCandidateSource>();
            var exclusions = new List<MicroPatternRepetitionExclusion>();
            foreach (var candidate in candidates)
            {
                if (blockedPatternId.HasValue && candidate.PatternId == blockedPatternId.Value)
                {
                    exclusions.Add(new MicroPatternRepetitionExclusion(
                        candidate.PatternId,
                        candidate.Source.Transform,
                        candidate.Source.ApplicationPlan.StableDigest,
                        candidate.Signature.StableDigest,
                        repeatedHistory.Select(value => value.PlacementId)));
                }
                else
                {
                    allowed.Add(candidate.Source);
                }
            }

            if (candidates.Count != 0 && allowed.Count == 0)
            {
                errors.Add(Error(
                    MicroPatternRepetitionErrorCode.NoCandidateAfterThirdRepeatGuard,
                    "candidateSources",
                    blockedPatternId.HasValue ? blockedPatternId.Value.Value : string.Empty));
            }

            return Result(allowed, exclusions, errors, history, candidates);
        }

        private static void ValidateHistory(
            IEnumerable<MicroPatternAcceptedHistoryItem> history,
            ICollection<MicroPatternRepetitionGuardError> errors)
        {
            var sequences = new HashSet<long>();
            var placementIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in history)
            {
                if (item == null)
                {
                    errors.Add(Error(
                        MicroPatternRepetitionErrorCode.InvalidHistory,
                        "context.acceptedHistory",
                        "Null history item."));
                    continue;
                }
                var path = "context.acceptedHistory[" +
                           item.PlacementSequence.ToString(CultureInfo.InvariantCulture) + "]";
                if (item.PlacementSequence < 0 ||
                    string.IsNullOrEmpty(item.PlacementId) ||
                    string.IsNullOrEmpty(item.PatternId.Value) ||
                    item.SilhouetteSignature == null ||
                    !MicroPatternContractDigest.IsLowerHexDigest(
                        item.SilhouetteSignature == null
                            ? string.Empty
                            : item.SilhouetteSignature.StableDigest))
                {
                    errors.Add(Error(
                        MicroPatternRepetitionErrorCode.InvalidHistory,
                        path,
                        item.PlacementId + "|" + item.PatternId.Value));
                }
                if (!sequences.Add(item.PlacementSequence) ||
                    !placementIds.Add(item.PlacementId))
                {
                    errors.Add(Error(
                        MicroPatternRepetitionErrorCode.DuplicateHistoryPlacement,
                        path,
                        item.PlacementId));
                }
            }
        }

        private static void ValidateCandidate(
            MicroPatternCandidateSource source,
            ICollection<CandidateEvidence> candidates,
            ICollection<MicroPatternRepetitionGuardError> errors)
        {
            if (source == null || source.Definition == null ||
                string.IsNullOrEmpty(source.Definition.Id.Value))
            {
                errors.Add(Error(
                    MicroPatternRepetitionErrorCode.InvalidCandidateSource,
                    "candidateSources",
                    "Candidate definition and exact pattern ID are required."));
                return;
            }

            var identity = SourceIdentity(source);
            if (source.ApplicationPlan == null ||
                source.ApplicationPlan.SourcePatternId != source.Definition.Id ||
                source.ApplicationPlan.Transform != source.Transform ||
                !MicroPatternContractDigest.IsLowerHexDigest(source.ApplicationPlan.StableDigest))
            {
                errors.Add(Error(
                    MicroPatternRepetitionErrorCode.InvalidApplicationPlan,
                    identity,
                    "A matching successful MAP10_02 application plan is required."));
                return;
            }

            var signature = MicroPatternSilhouetteSignatureBuilder.Build(source.ApplicationPlan);
            if (!signature.Success)
            {
                errors.Add(Error(
                    MicroPatternRepetitionErrorCode.InvalidApplicationPlan,
                    identity,
                    string.Join(";", signature.Errors.Select(value => value.ToString()))));
                return;
            }

            candidates.Add(new CandidateEvidence(source, signature.Signature, identity));
        }

        private static MicroPatternRepetitionGuardResult Result(
            IEnumerable<MicroPatternCandidateSource> allowed,
            IEnumerable<MicroPatternRepetitionExclusion> exclusions,
            IEnumerable<MicroPatternRepetitionGuardError> errors,
            IEnumerable<MicroPatternAcceptedHistoryItem> history,
            IEnumerable<CandidateEvidence> candidates)
        {
            var errorArray = (errors ?? Array.Empty<MicroPatternRepetitionGuardError>()).ToArray();
            var digest = errorArray.Length == 0
                ? ComputeDigest(history, candidates, allowed, exclusions)
                : string.Empty;
            return new MicroPatternRepetitionGuardResult(allowed, exclusions, errorArray, digest);
        }

        private static string ComputeDigest(
            IEnumerable<MicroPatternAcceptedHistoryItem> history,
            IEnumerable<CandidateEvidence> candidates,
            IEnumerable<MicroPatternCandidateSource> allowed,
            IEnumerable<MicroPatternRepetitionExclusion> exclusions)
        {
            var material = new StringBuilder();
            MicroPatternContractDigest.Append(material, "RULESET", "MAP10_05_REPETITION_V1");
            foreach (var item in (history ?? Array.Empty<MicroPatternAcceptedHistoryItem>()))
            {
                MicroPatternContractDigest.Append(
                    material,
                    "HISTORY",
                    item.PlacementSequence.ToString(CultureInfo.InvariantCulture),
                    item.PlacementId,
                    item.PatternId.Value,
                    item.SilhouetteSignature.StableDigest);
            }
            foreach (var candidate in (candidates ?? Array.Empty<CandidateEvidence>())
                         .OrderBy(value => value.Identity, StringComparer.Ordinal))
            {
                MicroPatternContractDigest.Append(
                    material,
                    "SOURCE",
                    candidate.Identity,
                    candidate.Signature.StableDigest);
            }
            foreach (var source in (allowed ?? Array.Empty<MicroPatternCandidateSource>())
                         .OrderBy(SourceIdentity, StringComparer.Ordinal))
            {
                MicroPatternContractDigest.Append(material, "ALLOWED", SourceIdentity(source));
            }
            foreach (var exclusion in (exclusions ?? Array.Empty<MicroPatternRepetitionExclusion>())
                         .OrderBy(value => value))
            {
                MicroPatternContractDigest.Append(material, "EXCLUDED", exclusion.ToString());
            }
            return MicroPatternContractDigest.Hash(material);
        }

        private static string SourceIdentity(MicroPatternCandidateSource source)
        {
            var id = source == null || source.Definition == null
                ? "<missing>"
                : source.Definition.Id.Value;
            var plan = source == null || source.ApplicationPlan == null
                ? "<missing>"
                : source.ApplicationPlan.StableDigest;
            var transform = source == null ? "<missing>" : source.Transform.ToString();
            return id + ":" + transform + ":" + plan;
        }

        private static MicroPatternRepetitionGuardError Error(
            MicroPatternRepetitionErrorCode code,
            string path,
            string detail)
        {
            return new MicroPatternRepetitionGuardError(code, path, detail);
        }

        private sealed class CandidateEvidence
        {
            public CandidateEvidence(
                MicroPatternCandidateSource source,
                MicroPatternSilhouetteSignature signature,
                string identity)
            {
                Source = source;
                Signature = signature;
                Identity = identity;
            }

            public MicroPatternCandidateSource Source { get; }
            public MicroPatternId PatternId => Source.Definition.Id;
            public MicroPatternSilhouetteSignature Signature { get; }
            public string Identity { get; }
        }
    }
}
