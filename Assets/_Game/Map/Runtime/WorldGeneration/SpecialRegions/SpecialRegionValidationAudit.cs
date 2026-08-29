using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public enum SpecialRegionAuditFamily
    {
        Village = 1,
        CoreResource = 2,
        Landmark = 3,
    }

    public enum SpecialRegionAuditBinding
    {
        ReferenceFixture = 1,
        DeferredToMAP14 = 2,
    }

    public enum SpecialRegionAuditSection
    {
        Identity = 1,
        FootprintBindingBuffer = 2,
        FixedCollision = 3,
        FixedAccess = 4,
        ReplaceableSlots = 5,
        Routes = 6,
        States = 7,
        ResetPersistence = 8,
    }

    public enum SpecialRegionValidationAuditErrorCode
    {
        MissingInput = 1,
        DuplicateArtifact = 2,
        MissingArtifact = 3,
        IdentityMismatch = 4,
        DigestMismatch = 5,
        FootprintMismatch = 6,
        MissingSectorCoverage = 7,
        MissingSeamCrossing = 8,
        SiteBindingMismatch = 9,
        BufferMismatch = 10,
        CollisionOwnerMismatch = 11,
        FixedReplaceableOverlap = 12,
        PersistenceMismatch = 13,
        MissingRouteWitness = 14,
        RouteOrderMismatch = 15,
        MandatoryToolDependency = 16,
        UnrecoverableFailure = 17,
        StateVariantMismatch = 18,
        ResetMismatch = 19,
        ResourceLossRisk = 20,
        DuplicateBenefitRisk = 21,
        DeferredWorldClaim = 22,
        MutationClaim = 23,
        NonCanonicalPublication = 24,
    }

    public enum SpecialRegionAuditTokenKind
    {
        DesignChunk = 1,
        SectorSeam = 2,
        Entry = 3,
        Return = 4,
        Apron = 5,
        Buffer = 6,
        FixedCollision = 7,
        FixedAccess = 8,
        Facility = 9,
        Npc = 10,
        Enemy = 11,
        Event = 12,
        Reward = 13,
        LowRoute = 14,
        HighRoute = 15,
        RecoveryRoute = 16,
        State = 17,
        Reset = 18,
    }

    public sealed class SpecialRegionValidationAuditError :
        IEquatable<SpecialRegionValidationAuditError>, IComparable<SpecialRegionValidationAuditError>
    {
        public SpecialRegionValidationAuditError(
            SpecialRegionValidationAuditErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SpecialRegionValidationAuditErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(SpecialRegionValidationAuditError other)
        {
            if (other == null) return -1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SpecialRegionValidationAuditError other)
            => other != null && Code == other.Code &&
               string.Equals(Path, other.Path, StringComparison.Ordinal) &&
               string.Equals(Detail, other.Detail, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as SpecialRegionValidationAuditError);

        public override int GetHashCode()
        {
            unchecked
            {
                var value = (int)Code;
                value = (value * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (value * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class SpecialRegionAuditRoute
    {
        private readonly ReadOnlyCollection<string> nodeIds;

        public SpecialRegionAuditRoute(
            string routeId,
            string routeKind,
            IEnumerable<string> nodeIds,
            bool mandatoryNoTool,
            bool ordered,
            bool recovery)
        {
            RouteId = routeId ?? string.Empty;
            RouteKind = routeKind ?? string.Empty;
            this.nodeIds = new ReadOnlyCollection<string>(
                (nodeIds ?? Array.Empty<string>()).Select(value => value ?? string.Empty).ToArray());
            MandatoryNoTool = mandatoryNoTool;
            Ordered = ordered;
            Recovery = recovery;
        }

        public string RouteId { get; }
        public string RouteKind { get; }
        public IReadOnlyList<string> NodeIds => nodeIds;
        public bool MandatoryNoTool { get; }
        public bool Ordered { get; }
        public bool Recovery { get; }
    }

    public sealed class SpecialRegionAuditToken
    {
        public SpecialRegionAuditToken(
            SpecialRegionAuditTokenKind kind,
            string id,
            int x,
            int y,
            string label)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            X = x;
            Y = y;
            Label = label ?? string.Empty;
        }

        public SpecialRegionAuditTokenKind Kind { get; }
        public string Id { get; }
        public int X { get; }
        public int Y { get; }
        public string Label { get; }
    }

    public sealed class SpecialRegionAuditMetrics
    {
        public SpecialRegionAuditMetrics(
            bool identityMatches,
            bool digestsMatch,
            bool footprintMatches,
            int sectorCoverageCount,
            int seamCrossingCount,
            bool siteBindingMatches,
            bool bufferMatches,
            bool collisionOwnerMatches,
            int fixedReplaceableOverlapCount,
            bool persistenceMatches,
            bool routeOrderMatches,
            int mandatoryToolDependencyCount,
            int unrecoverableFailureCount,
            bool stateVariantMatches,
            bool resetMatches,
            int resourceLossRiskCount,
            int duplicateBenefitRiskCount,
            int worldOriginClaimCount,
            int reservationClaimCount,
            int bridgeClaimCount,
            int placedOwnershipClaimCount,
            int mutationClaimCount,
            bool canonicalPublication)
        {
            IdentityMatches = identityMatches;
            DigestsMatch = digestsMatch;
            FootprintMatches = footprintMatches;
            SectorCoverageCount = sectorCoverageCount;
            SeamCrossingCount = seamCrossingCount;
            SiteBindingMatches = siteBindingMatches;
            BufferMatches = bufferMatches;
            CollisionOwnerMatches = collisionOwnerMatches;
            FixedReplaceableOverlapCount = fixedReplaceableOverlapCount;
            PersistenceMatches = persistenceMatches;
            RouteOrderMatches = routeOrderMatches;
            MandatoryToolDependencyCount = mandatoryToolDependencyCount;
            UnrecoverableFailureCount = unrecoverableFailureCount;
            StateVariantMatches = stateVariantMatches;
            ResetMatches = resetMatches;
            ResourceLossRiskCount = resourceLossRiskCount;
            DuplicateBenefitRiskCount = duplicateBenefitRiskCount;
            WorldOriginClaimCount = worldOriginClaimCount;
            ReservationClaimCount = reservationClaimCount;
            BridgeClaimCount = bridgeClaimCount;
            PlacedOwnershipClaimCount = placedOwnershipClaimCount;
            MutationClaimCount = mutationClaimCount;
            CanonicalPublication = canonicalPublication;
        }

        public bool IdentityMatches { get; }
        public bool DigestsMatch { get; }
        public bool FootprintMatches { get; }
        public int SectorCoverageCount { get; }
        public int SeamCrossingCount { get; }
        public bool SiteBindingMatches { get; }
        public bool BufferMatches { get; }
        public bool CollisionOwnerMatches { get; }
        public int FixedReplaceableOverlapCount { get; }
        public bool PersistenceMatches { get; }
        public bool RouteOrderMatches { get; }
        public int MandatoryToolDependencyCount { get; }
        public int UnrecoverableFailureCount { get; }
        public bool StateVariantMatches { get; }
        public bool ResetMatches { get; }
        public int ResourceLossRiskCount { get; }
        public int DuplicateBenefitRiskCount { get; }
        public int WorldOriginClaimCount { get; }
        public int ReservationClaimCount { get; }
        public int BridgeClaimCount { get; }
        public int PlacedOwnershipClaimCount { get; }
        public int MutationClaimCount { get; }
        public bool CanonicalPublication { get; }

        public SpecialRegionAuditMetrics WithViolation(SpecialRegionValidationAuditErrorCode code)
        {
            return new SpecialRegionAuditMetrics(
                code == SpecialRegionValidationAuditErrorCode.IdentityMismatch ? false : IdentityMatches,
                code == SpecialRegionValidationAuditErrorCode.DigestMismatch ? false : DigestsMatch,
                code == SpecialRegionValidationAuditErrorCode.FootprintMismatch ? false : FootprintMatches,
                code == SpecialRegionValidationAuditErrorCode.MissingSectorCoverage ? 0 : SectorCoverageCount,
                code == SpecialRegionValidationAuditErrorCode.MissingSeamCrossing ? 0 : SeamCrossingCount,
                code == SpecialRegionValidationAuditErrorCode.SiteBindingMismatch ? false : SiteBindingMatches,
                code == SpecialRegionValidationAuditErrorCode.BufferMismatch ? false : BufferMatches,
                code == SpecialRegionValidationAuditErrorCode.CollisionOwnerMismatch ? false : CollisionOwnerMatches,
                code == SpecialRegionValidationAuditErrorCode.FixedReplaceableOverlap ? 1 : FixedReplaceableOverlapCount,
                code == SpecialRegionValidationAuditErrorCode.PersistenceMismatch ? false : PersistenceMatches,
                code == SpecialRegionValidationAuditErrorCode.RouteOrderMismatch ? false : RouteOrderMatches,
                code == SpecialRegionValidationAuditErrorCode.MandatoryToolDependency ? 1 : MandatoryToolDependencyCount,
                code == SpecialRegionValidationAuditErrorCode.UnrecoverableFailure ? 1 : UnrecoverableFailureCount,
                code == SpecialRegionValidationAuditErrorCode.StateVariantMismatch ? false : StateVariantMatches,
                code == SpecialRegionValidationAuditErrorCode.ResetMismatch ? false : ResetMatches,
                code == SpecialRegionValidationAuditErrorCode.ResourceLossRisk ? 1 : ResourceLossRiskCount,
                code == SpecialRegionValidationAuditErrorCode.DuplicateBenefitRisk ? 1 : DuplicateBenefitRiskCount,
                code == SpecialRegionValidationAuditErrorCode.DeferredWorldClaim ? 1 : WorldOriginClaimCount,
                code == SpecialRegionValidationAuditErrorCode.DeferredWorldClaim ? 1 : ReservationClaimCount,
                code == SpecialRegionValidationAuditErrorCode.DeferredWorldClaim ? 1 : BridgeClaimCount,
                code == SpecialRegionValidationAuditErrorCode.DeferredWorldClaim ? 1 : PlacedOwnershipClaimCount,
                code == SpecialRegionValidationAuditErrorCode.MutationClaim ? 1 : MutationClaimCount,
                code == SpecialRegionValidationAuditErrorCode.NonCanonicalPublication ? false : CanonicalPublication);
        }
    }

    public sealed class SpecialRegionAuditArtifactInput
    {
        private readonly ReadOnlyCollection<SpecialRegionSlotKind> slotKinds;
        private readonly ReadOnlyCollection<SpecialRegionAuditRoute> routes;
        private readonly ReadOnlyCollection<SpecialRegionAuditToken> tokens;

        public SpecialRegionAuditArtifactInput(
            int canonicalOrder,
            string artifactId,
            SpecialRegionAuditFamily family,
            SpecialRegionAuditBinding binding,
            SpecialRegionKind regionKind,
            string kindOrTheme,
            int footprintWidth,
            int footprintHeight,
            int designWidth,
            int designHeight,
            int activeChunkCount,
            int fixedCollisionCount,
            int fixedAccessCount,
            IEnumerable<SpecialRegionSlotKind> slotKinds,
            IEnumerable<SpecialRegionAuditRoute> routes,
            int stateCount,
            int resetCount,
            int persistenceCheckpointCount,
            int persistenceKeyCount,
            int requiredRewardCount,
            string sourceDigest,
            string componentDigest,
            string artifactDigest,
            SpecialRegionAuditMetrics metrics,
            IEnumerable<SpecialRegionAuditToken> tokens)
        {
            CanonicalOrder = canonicalOrder;
            ArtifactId = artifactId ?? string.Empty;
            Family = family;
            Binding = binding;
            RegionKind = regionKind;
            KindOrTheme = kindOrTheme ?? string.Empty;
            FootprintWidth = footprintWidth;
            FootprintHeight = footprintHeight;
            DesignWidth = designWidth;
            DesignHeight = designHeight;
            ActiveChunkCount = activeChunkCount;
            FixedCollisionCount = fixedCollisionCount;
            FixedAccessCount = fixedAccessCount;
            this.slotKinds = new ReadOnlyCollection<SpecialRegionSlotKind>(
                (slotKinds ?? Array.Empty<SpecialRegionSlotKind>()).Distinct().OrderBy(value => value).ToArray());
            this.routes = new ReadOnlyCollection<SpecialRegionAuditRoute>(
                (routes ?? Array.Empty<SpecialRegionAuditRoute>()).Where(value => value != null)
                .OrderBy(value => value.RouteId, StringComparer.Ordinal).ToArray());
            StateCount = stateCount;
            ResetCount = resetCount;
            PersistenceCheckpointCount = persistenceCheckpointCount;
            PersistenceKeyCount = persistenceKeyCount;
            RequiredRewardCount = requiredRewardCount;
            SourceDigest = sourceDigest ?? string.Empty;
            ComponentDigest = componentDigest ?? string.Empty;
            ArtifactDigest = artifactDigest ?? string.Empty;
            Metrics = metrics;
            this.tokens = new ReadOnlyCollection<SpecialRegionAuditToken>(
                (tokens ?? Array.Empty<SpecialRegionAuditToken>()).Where(value => value != null)
                .OrderBy(value => value.Kind).ThenBy(value => value.Y).ThenBy(value => value.X)
                .ThenBy(value => value.Id, StringComparer.Ordinal).ToArray());
        }

        public int CanonicalOrder { get; }
        public string ArtifactId { get; }
        public SpecialRegionAuditFamily Family { get; }
        public SpecialRegionAuditBinding Binding { get; }
        public SpecialRegionKind RegionKind { get; }
        public string KindOrTheme { get; }
        public int FootprintWidth { get; }
        public int FootprintHeight { get; }
        public int DesignWidth { get; }
        public int DesignHeight { get; }
        public int ActiveChunkCount { get; }
        public int FixedCollisionCount { get; }
        public int FixedAccessCount { get; }
        public IReadOnlyList<SpecialRegionSlotKind> SlotKinds => slotKinds;
        public IReadOnlyList<SpecialRegionAuditRoute> Routes => routes;
        public int StateCount { get; }
        public int ResetCount { get; }
        public int PersistenceCheckpointCount { get; }
        public int PersistenceKeyCount { get; }
        public int RequiredRewardCount { get; }
        public string SourceDigest { get; }
        public string ComponentDigest { get; }
        public string ArtifactDigest { get; }
        public SpecialRegionAuditMetrics Metrics { get; }
        public IReadOnlyList<SpecialRegionAuditToken> Tokens => tokens;

        public SpecialRegionAuditArtifactInput WithViolation(SpecialRegionValidationAuditErrorCode code)
            => new SpecialRegionAuditArtifactInput(
                CanonicalOrder, ArtifactId, Family, Binding, RegionKind, KindOrTheme,
                FootprintWidth, FootprintHeight, DesignWidth, DesignHeight, ActiveChunkCount,
                FixedCollisionCount, FixedAccessCount, slotKinds, routes, StateCount, ResetCount,
                PersistenceCheckpointCount, PersistenceKeyCount, RequiredRewardCount,
                SourceDigest, ComponentDigest, ArtifactDigest,
                Metrics == null ? null : Metrics.WithViolation(code), tokens);
    }

    public sealed class SpecialRegionAuditRequest
    {
        private readonly ReadOnlyCollection<SpecialRegionAuditArtifactInput> artifacts;

        public SpecialRegionAuditRequest(IEnumerable<SpecialRegionAuditArtifactInput> artifacts)
        {
            this.artifacts = new ReadOnlyCollection<SpecialRegionAuditArtifactInput>(
                (artifacts ?? Array.Empty<SpecialRegionAuditArtifactInput>()).ToArray());
        }

        public IReadOnlyList<SpecialRegionAuditArtifactInput> Artifacts => artifacts;
    }

    public sealed class SpecialRegionAuditSectionResult
    {
        public SpecialRegionAuditSectionResult(
            SpecialRegionAuditSection section,
            bool passed,
            int observedCount,
            string detail,
            string canonicalDigest)
        {
            Section = section;
            Passed = passed;
            ObservedCount = observedCount;
            Detail = detail ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public SpecialRegionAuditSection Section { get; }
        public bool Passed { get; }
        public int ObservedCount { get; }
        public string Detail { get; }
        public string CanonicalDigest { get; }
    }

    public sealed class SpecialRegionAuditArtifactResult
    {
        private readonly ReadOnlyCollection<SpecialRegionAuditSectionResult> sections;
        private readonly ReadOnlyCollection<SpecialRegionAuditRoute> routes;
        private readonly ReadOnlyCollection<SpecialRegionAuditToken> tokens;

        internal SpecialRegionAuditArtifactResult(
            SpecialRegionAuditArtifactInput input,
            IEnumerable<SpecialRegionAuditSectionResult> sections,
            string canonicalDigest)
        {
            Input = input;
            this.sections = new ReadOnlyCollection<SpecialRegionAuditSectionResult>(
                sections.OrderBy(value => value.Section).ToArray());
            this.routes = new ReadOnlyCollection<SpecialRegionAuditRoute>(input.Routes.ToArray());
            this.tokens = new ReadOnlyCollection<SpecialRegionAuditToken>(input.Tokens.ToArray());
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public SpecialRegionAuditArtifactInput Input { get; }
        public string ArtifactId => Input.ArtifactId;
        public SpecialRegionAuditFamily Family => Input.Family;
        public SpecialRegionAuditBinding Binding => Input.Binding;
        public IReadOnlyList<SpecialRegionAuditSectionResult> Sections => sections;
        public IReadOnlyList<SpecialRegionAuditRoute> Routes => routes;
        public IReadOnlyList<SpecialRegionAuditToken> Tokens => tokens;
        public string CanonicalDigest { get; }
        public bool Passed => sections.All(value => value.Passed);
    }

    public sealed class SpecialRegionValidationReport
    {
        private readonly ReadOnlyCollection<SpecialRegionAuditArtifactResult> artifacts;

        internal SpecialRegionValidationReport(
            IEnumerable<SpecialRegionAuditArtifactResult> artifacts,
            string canonicalDigest)
        {
            this.artifacts = new ReadOnlyCollection<SpecialRegionAuditArtifactResult>(
                artifacts.OrderBy(value => value.Input.CanonicalOrder).ToArray());
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public IReadOnlyList<SpecialRegionAuditArtifactResult> Artifacts => artifacts;
        public int ArtifactCount => artifacts.Count;
        public int SectionPassCount => artifacts.SelectMany(value => value.Sections).Count(value => value.Passed);
        public int SectionFailCount => artifacts.SelectMany(value => value.Sections).Count(value => !value.Passed);
        public int ReferenceFixtureCount => artifacts.Count(value => value.Binding == SpecialRegionAuditBinding.ReferenceFixture);
        public int DeferredToMAP14Count => artifacts.Count(value => value.Binding == SpecialRegionAuditBinding.DeferredToMAP14);
        public int RouteCount => artifacts.Sum(value => value.Input.Routes.Count);
        public int StateCount => artifacts.Sum(value => value.Input.StateCount);
        public int ResetCount => artifacts.Sum(value => value.Input.ResetCount);
        public int PersistenceCheckpointCount => artifacts.Sum(value => value.Input.PersistenceCheckpointCount);
        public int PersistenceKeyCount => artifacts.Sum(value => value.Input.PersistenceKeyCount);
        public int MutationClaimCount => artifacts.Sum(value => value.Input.Metrics.MutationClaimCount);
        public int SolverClaimCount => 0;
        public int GameplayClaimCount => 0;
        public string CanonicalDigest { get; }
    }

    public sealed class SpecialRegionValidationAuditResult
    {
        private readonly ReadOnlyCollection<SpecialRegionValidationAuditError> errors;

        internal SpecialRegionValidationAuditResult(
            SpecialRegionValidationReport report,
            IEnumerable<SpecialRegionValidationAuditError> errors,
            string canonicalDigest)
        {
            var values = (errors ?? Array.Empty<SpecialRegionValidationAuditError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<SpecialRegionValidationAuditError>(values);
            Report = values.Length == 0 ? report : null;
            CanonicalDigest = Report == null ? string.Empty : canonicalDigest ?? string.Empty;
        }

        public bool Success => Report != null && errors.Count == 0;
        public SpecialRegionValidationReport Report { get; }
        public IReadOnlyList<SpecialRegionValidationAuditError> Errors => errors;
        public string CanonicalDigest { get; }
    }
}
