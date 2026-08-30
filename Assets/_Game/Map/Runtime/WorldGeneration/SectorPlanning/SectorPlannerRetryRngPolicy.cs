using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum SectorPlannerRetryStage
    {
        None = 0,
        PatternCandidate = 1,
        PatternTransform = 2,
        ClusterVariant = 3,
        ClusterFootprint = 4,
        SectorAttempt = 5,
        Abort = 6,
    }

    public enum SectorPlannerRetryDecisionKind
    {
        AcceptFirstPass = 0,
        RetryPatternCandidate = 1,
        RetryPatternTransform = 2,
        RetryClusterVariant = 3,
        RetryClusterFootprint = 4,
        AcceptRecovered = 5,
        AbortCapReached = 6,
        AbortUnownedFailure = 7,
        AbortForbiddenFallback = 8,
        AbortNonDeterministicTrace = 9,
    }

    public enum SectorPlannerRetryFailureOwner
    {
        Input = 0,
        Anchor = 1,
        ClusterPlacement = 2,
        SpineEnvelope = 3,
        PatternSelection = 4,
        PatternApplication = 5,
        PatternRender = 6,
        QuietActivityEvent = 7,
        CanvasOwnership = 8,
        RngPolicy = 9,
        ForbiddenFallback = 10,
        Unknown = 11,
    }

    public enum SectorPlannerRngPassScope
    {
        SectorPlan = 0,
        PatternCandidate = 1,
        PatternTransform = 2,
        ClusterVariant = 3,
        ClusterFootprint = 4,
        ActivitySelection = 5,
        EventSelection = 6,
        RetryDecision = 7,
    }

    public enum SectorPlannerRetryErrorCode
    {
        MissingInput = 0,
        MissingOwnershipPlan = 1,
        MissingRetryPolicy = 2,
        MissingRngAuthority = 3,
        SectorMismatch = 4,
        InvalidRetryOrder = 5,
        InvalidRetryLimit = 6,
        RetryCapExceeded = 7,
        NodeCapExceeded = 8,
        UnknownFailureOwner = 9,
        UnretryableFailure = 10,
        ForbiddenFallbackAttempt = 11,
        ValidationRelaxationAttempt = 12,
        WholeSectorRerandomAttempt = 13,
        WholeWorldRerandomAttempt = 14,
        SyntheticCorridorAttempt = 15,
        SocketMutationAttempt = 16,
        BoundaryMutationAttempt = 17,
        SpecialReservationMutationAttempt = 18,
        ProtectedMaskRelaxationAttempt = 19,
        NonDeterministicRngTrace = 20,
        RngStreamMismatch = 21,
        RngScopeMismatch = 22,
        RngDrawMismatch = 23,
        NegativeAttemptOrdinal = 24,
        DuplicateAttemptTrace = 25,
        DuplicateNodeTrace = 26,
        MissingTerminalDecision = 27,
        NonCanonicalPublication = 28,
        UpstreamMutationClaim = 29,
        PatternMutationClaim = 30,
        ClusterMutationClaim = 31,
        FootprintMutationClaim = 32,
        OwnershipMutationClaim = 33,
        TileMutationClaim = 34,
        SceneMutationClaim = 35,
    }

    public sealed class SectorPlannerRetryLimit
    {
        public SectorPlannerRetryLimit(
            int maxPatternCandidateAttemptsPerZone,
            int maxPatternTransformAttemptsPerPattern,
            int maxClusterVariantAttemptsPerSector,
            int maxClusterFootprintAttemptsPerSector,
            int maxRetryNodesPerSector,
            int maxTotalLocalAttemptsPerSector)
        {
            MaxPatternCandidateAttemptsPerZone = maxPatternCandidateAttemptsPerZone;
            MaxPatternTransformAttemptsPerPattern = maxPatternTransformAttemptsPerPattern;
            MaxClusterVariantAttemptsPerSector = maxClusterVariantAttemptsPerSector;
            MaxClusterFootprintAttemptsPerSector = maxClusterFootprintAttemptsPerSector;
            MaxRetryNodesPerSector = maxRetryNodesPerSector;
            MaxTotalLocalAttemptsPerSector = maxTotalLocalAttemptsPerSector;
        }

        public int MaxPatternCandidateAttemptsPerZone { get; }
        public int MaxPatternTransformAttemptsPerPattern { get; }
        public int MaxClusterVariantAttemptsPerSector { get; }
        public int MaxClusterFootprintAttemptsPerSector { get; }
        public int MaxRetryNodesPerSector { get; }
        public int MaxTotalLocalAttemptsPerSector { get; }

        public bool AllPositive => MaxPatternCandidateAttemptsPerZone > 0 &&
                                   MaxPatternTransformAttemptsPerPattern > 0 &&
                                   MaxClusterVariantAttemptsPerSector > 0 &&
                                   MaxClusterFootprintAttemptsPerSector > 0 &&
                                   MaxRetryNodesPerSector > 0 &&
                                   MaxTotalLocalAttemptsPerSector > 0;

        public int ForStage(SectorPlannerRetryStage stage)
        {
            switch (stage)
            {
                case SectorPlannerRetryStage.PatternCandidate:
                    return MaxPatternCandidateAttemptsPerZone;
                case SectorPlannerRetryStage.PatternTransform:
                    return MaxPatternTransformAttemptsPerPattern;
                case SectorPlannerRetryStage.ClusterVariant:
                    return MaxClusterVariantAttemptsPerSector;
                case SectorPlannerRetryStage.ClusterFootprint:
                    return MaxClusterFootprintAttemptsPerSector;
                default:
                    return 0;
            }
        }
    }

    public sealed class SectorPlannerRetryPolicy
    {
        private static readonly SectorPlannerRetryStage[] ExpectedRecoveryOrder =
        {
            SectorPlannerRetryStage.PatternCandidate,
            SectorPlannerRetryStage.PatternTransform,
            SectorPlannerRetryStage.ClusterVariant,
            SectorPlannerRetryStage.ClusterFootprint,
            SectorPlannerRetryStage.SectorAttempt,
            SectorPlannerRetryStage.Abort,
        };

        private readonly ReadOnlyCollection<SectorPlannerRetryStage> recoveryOrder;

        public SectorPlannerRetryPolicy(
            SectorPlannerRetryLimit limits,
            IEnumerable<SectorPlannerRetryStage> sourceRecoveryOrder,
            string rulesetVersion = "MAP14_08_RETRY_RNG_V1")
        {
            Limits = limits;
            recoveryOrder = new ReadOnlyCollection<SectorPlannerRetryStage>(
                (sourceRecoveryOrder ?? Array.Empty<SectorPlannerRetryStage>()).ToArray());
            RulesetVersion = rulesetVersion ?? string.Empty;
            CanonicalDigest = SectorPlannerRetryCanonicalDigest.ComputePolicy(this);
        }

        public SectorPlannerRetryLimit Limits { get; }
        public IReadOnlyList<SectorPlannerRetryStage> RecoveryOrder => recoveryOrder;
        public string RulesetVersion { get; }
        public string CanonicalDigest { get; }

        public static SectorPlannerRetryPolicy CreateDefault()
        {
            return new SectorPlannerRetryPolicy(
                new SectorPlannerRetryLimit(3, 2, 3, 3, 12, 8),
                ExpectedRecoveryOrder);
        }

        public bool HasCanonicalOrder => recoveryOrder.SequenceEqual(ExpectedRecoveryOrder);
    }

    public sealed class SectorPlannerRetryError :
        IComparable<SectorPlannerRetryError>, IEquatable<SectorPlannerRetryError>
    {
        public SectorPlannerRetryError(
            SectorPlannerRetryErrorCode code,
            string subject,
            string detail)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SectorPlannerRetryErrorCode Code { get; }
        public string Subject { get; }
        public string Detail { get; }

        public int CompareTo(SectorPlannerRetryError other)
        {
            if (ReferenceEquals(other, null)) return 1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SectorPlannerRetryError other)
        {
            return !ReferenceEquals(other, null) &&
                   Code == other.Code &&
                   string.Equals(Subject, other.Subject, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as SectorPlannerRetryError);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Subject);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}", Code, Subject, Detail);
        }
    }
}
