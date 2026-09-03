using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum FinalRouteWitnessKind
    {
        BaseEntryToExit = 1,
        ExternalSocketToBaseRoute = 2,
        BoundaryApertureToBaseRoute = 3,
        HighRouteBranch = 4,
        HighFailureToBaseRecovery = 5,
        SpecialEntranceToBaseRoute = 6,
    }

    public enum FinalRouteNodeKind
    {
        PassableCell = 1,
        BaseEntry = 2,
        BaseExit = 3,
        ExternalSocket = 4,
        BoundaryAperture = 5,
        HighRouteBranch = 6,
        FailureSample = 7,
        SpecialEntrance = 8,
    }

    public enum FinalRouteEdgeKind
    {
        OrthogonalPassable = 1,
        DeclaredStep = 2,
        DeclaredDrop = 3,
        DeclaredJumpLink = 4,
        DeclaredLadderOrClimb = 5,
        DeclaredBounceOrDevice = 6,
        DeclaredRecoveryLink = 7,
        DeclaredSocketLink = 8,
    }

    public enum FinalRouteFailureKind
    {
        MissingRequest = 1,
        MissingCanvasPlan = 2,
        MissingProtectionDensityReport = 3,
        SourceReportMismatch = 4,
        InvalidSectorDimensions = 5,
        InvalidCellCount = 6,
        MissingLayer = 7,
        InvalidDigest = 8,
        ProtectionDensityRejected = 9,
        MissingBaseEntry = 10,
        MissingBaseExit = 11,
        InvalidAnchor = 12,
        BlockedAnchor = 13,
        MissingBaseRoute = 14,
        MissingExternalSocketWitness = 15,
        MissingBoundaryApertureWitness = 16,
        MissingHighFailureRecovery = 17,
        MissingSpecialEntranceWitness = 18,
        InvalidDeclaredEdge = 19,
        RouteCrossesBlockedCell = 20,
        StaticSoftlock = 21,
        ForbiddenOperation = 22,
    }

    public enum FinalRouteRecoveryKind
    {
        OrthogonalReturn = 1,
        DeclaredRecovery = 2,
    }

    public enum FinalRouteWitnessVerdict
    {
        Covered = 1,
        Missing = 2,
        Blocked = 3,
    }

    public sealed class FinalRouteAnchor : IComparable<FinalRouteAnchor>
    {
        public FinalRouteAnchor(
            string stableId,
            FinalRouteNodeKind kind,
            FinalCanvasCellCoordinate coordinate,
            FinalCanvasSourceOwner sourceOwner,
            FinalCanvasProtectionKind protection,
            bool required = true)
        {
            StableId = stableId ?? string.Empty;
            Kind = kind;
            Coordinate = coordinate;
            SourceOwner = sourceOwner;
            Protection = protection;
            Required = required;
            StableToken = string.Join("|", new[]
            {
                "ANCHOR", Kind.ToString().ToUpperInvariant(),
                Coordinate == null ? "MISSING" : Coordinate.ToString(),
                SourceOwner.ToString().ToUpperInvariant(),
                Protection.ToString().ToUpperInvariant(),
                Required ? "1" : "0", StableId,
            });
        }

        public string StableId { get; }
        public FinalRouteNodeKind Kind { get; }
        public FinalCanvasCellCoordinate Coordinate { get; }
        public FinalCanvasSourceOwner SourceOwner { get; }
        public FinalCanvasProtectionKind Protection { get; }
        public bool Required { get; }
        public string StableToken { get; }

        public int CompareTo(FinalRouteAnchor other)
        {
            if (other == null) return -1;
            var comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = CompareCoordinate(Coordinate, other.Coordinate);
            if (comparison != 0) return comparison;
            comparison = SourceOwner.CompareTo(other.SourceOwner);
            return comparison != 0
                ? comparison
                : string.Compare(StableId, other.StableId, StringComparison.Ordinal);
        }

        private static int CompareCoordinate(
            FinalCanvasCellCoordinate left,
            FinalCanvasCellCoordinate right) => left == null
            ? (right == null ? 0 : 1)
            : (right == null ? -1 : left.CompareTo(right));
    }

    public sealed class FinalRouteNode : IComparable<FinalRouteNode>
    {
        public FinalRouteNode(
            FinalCanvasCellCoordinate coordinate,
            FinalRouteNodeKind kind,
            FinalCanvasSourceOwner sourceOwner,
            string stableId)
        {
            Coordinate = coordinate;
            Kind = kind;
            SourceOwner = sourceOwner;
            StableId = stableId ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "NODE", Coordinate == null ? "MISSING" : Coordinate.ToString(),
                Kind.ToString().ToUpperInvariant(),
                SourceOwner.ToString().ToUpperInvariant(), StableId,
            });
        }

        public FinalCanvasCellCoordinate Coordinate { get; }
        public FinalRouteNodeKind Kind { get; }
        public FinalCanvasSourceOwner SourceOwner { get; }
        public string StableId { get; }
        public string StableToken { get; }

        public int CompareTo(FinalRouteNode other)
        {
            if (other == null) return -1;
            var comparison = Coordinate == null
                ? (other.Coordinate == null ? 0 : 1)
                : (other.Coordinate == null ? -1 : Coordinate.CompareTo(other.Coordinate));
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            return comparison != 0
                ? comparison
                : string.Compare(StableId, other.StableId, StringComparison.Ordinal);
        }
    }

    public sealed class FinalRouteEdge : IComparable<FinalRouteEdge>
    {
        public FinalRouteEdge(
            FinalCanvasCellCoordinate from,
            FinalCanvasCellCoordinate to,
            FinalRouteEdgeKind kind,
            FinalCanvasSourceOwner sourceOwner,
            string stableId,
            bool isBidirectional)
        {
            From = from;
            To = to;
            Kind = kind;
            SourceOwner = sourceOwner;
            StableId = stableId ?? string.Empty;
            IsBidirectional = isBidirectional;
            StableToken = string.Join("|", new[]
            {
                "EDGE", From == null ? "MISSING" : From.ToString(),
                To == null ? "MISSING" : To.ToString(),
                Kind.ToString().ToUpperInvariant(),
                SourceOwner.ToString().ToUpperInvariant(),
                IsBidirectional ? "1" : "0", StableId,
            });
        }

        public FinalCanvasCellCoordinate From { get; }
        public FinalCanvasCellCoordinate To { get; }
        public FinalRouteEdgeKind Kind { get; }
        public FinalCanvasSourceOwner SourceOwner { get; }
        public string StableId { get; }
        public bool IsBidirectional { get; }
        public string StableToken { get; }

        public int CompareTo(FinalRouteEdge other)
        {
            if (other == null) return -1;
            var comparison = CompareCoordinate(From, other.From);
            if (comparison != 0) return comparison;
            comparison = CompareCoordinate(To, other.To);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = SourceOwner.CompareTo(other.SourceOwner);
            return comparison != 0
                ? comparison
                : string.Compare(StableId, other.StableId, StringComparison.Ordinal);
        }

        private static int CompareCoordinate(
            FinalCanvasCellCoordinate left,
            FinalCanvasCellCoordinate right) => left == null
            ? (right == null ? 0 : 1)
            : (right == null ? -1 : left.CompareTo(right));
    }

    public sealed class FinalRouteWitness : IComparable<FinalRouteWitness>
    {
        private readonly ReadOnlyCollection<FinalCanvasCellCoordinate> path;

        public FinalRouteWitness(
            string stableId,
            FinalRouteWitnessKind kind,
            FinalRouteAnchor start,
            FinalRouteAnchor end,
            FinalRouteWitnessVerdict verdict,
            IEnumerable<FinalCanvasCellCoordinate> sourcePath)
        {
            StableId = stableId ?? string.Empty;
            Kind = kind;
            Start = start;
            End = end;
            Verdict = verdict;
            path = new ReadOnlyCollection<FinalCanvasCellCoordinate>((sourcePath ??
                Array.Empty<FinalCanvasCellCoordinate>()).ToArray());
            StableToken = string.Join("|", new[]
            {
                "WITNESS", Kind.ToString().ToUpperInvariant(),
                Start == null ? "MISSING" : Start.StableId,
                End == null ? "MISSING" : End.StableId,
                Verdict.ToString().ToUpperInvariant(), StableId,
                string.Join(";", path.Select(value => value == null ? "MISSING" : value.ToString())),
            });
        }

        public string StableId { get; }
        public FinalRouteWitnessKind Kind { get; }
        public FinalRouteAnchor Start { get; }
        public FinalRouteAnchor End { get; }
        public FinalRouteWitnessVerdict Verdict { get; }
        public IReadOnlyList<FinalCanvasCellCoordinate> Path => path;
        public int PathCellCount => path.Count;
        public string StableToken { get; }

        public int CompareTo(FinalRouteWitness other)
        {
            if (other == null) return -1;
            var comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Start == null ? string.Empty : Start.StableToken,
                other.Start == null ? string.Empty : other.Start.StableToken,
                StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(End == null ? string.Empty : End.StableToken,
                other.End == null ? string.Empty : other.End.StableToken,
                StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(StableId, other.StableId, StringComparison.Ordinal);
        }
    }

    public sealed class FinalRecoveryWitness : IComparable<FinalRecoveryWitness>
    {
        private readonly ReadOnlyCollection<FinalCanvasCellCoordinate> path;

        public FinalRecoveryWitness(
            string stableId,
            FinalRouteAnchor failureAnchor,
            FinalRouteAnchor targetBaseAnchor,
            FinalRouteRecoveryKind kind,
            FinalRouteWitnessVerdict verdict,
            IEnumerable<FinalCanvasCellCoordinate> sourcePath)
        {
            StableId = stableId ?? string.Empty;
            FailureAnchor = failureAnchor;
            TargetBaseAnchor = targetBaseAnchor;
            Kind = kind;
            Verdict = verdict;
            path = new ReadOnlyCollection<FinalCanvasCellCoordinate>((sourcePath ??
                Array.Empty<FinalCanvasCellCoordinate>()).ToArray());
            StableToken = string.Join("|", new[]
            {
                "RECOVERY", Kind.ToString().ToUpperInvariant(),
                FailureAnchor == null ? "MISSING" : FailureAnchor.StableId,
                TargetBaseAnchor == null ? "MISSING" : TargetBaseAnchor.StableId,
                Verdict.ToString().ToUpperInvariant(), StableId,
                string.Join(";", path.Select(value => value == null ? "MISSING" : value.ToString())),
            });
        }

        public string StableId { get; }
        public FinalRouteAnchor FailureAnchor { get; }
        public FinalRouteAnchor TargetBaseAnchor { get; }
        public FinalRouteRecoveryKind Kind { get; }
        public FinalRouteWitnessVerdict Verdict { get; }
        public IReadOnlyList<FinalCanvasCellCoordinate> Path => path;
        public int PathCellCount => path.Count;
        public bool UsesFallbackCarve => false;
        public bool UsesSilentWidening => false;
        public bool RequiresSectorRerender => false;
        public bool RequiresWholeWorldRerandom => false;
        public string StableToken { get; }

        public int CompareTo(FinalRecoveryWitness other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(
                FailureAnchor == null ? string.Empty : FailureAnchor.StableToken,
                other.FailureAnchor == null ? string.Empty : other.FailureAnchor.StableToken,
                StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = string.Compare(
                TargetBaseAnchor == null ? string.Empty : TargetBaseAnchor.StableToken,
                other.TargetBaseAnchor == null ? string.Empty : other.TargetBaseAnchor.StableToken,
                StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(StableId, other.StableId, StringComparison.Ordinal);
        }
    }

    public sealed class FinalRouteSoftlockCandidate : IComparable<FinalRouteSoftlockCandidate>
    {
        public FinalRouteSoftlockCandidate(
            FinalCanvasCellCoordinate coordinate,
            FinalRouteFailureKind kind,
            string stableId,
            string reason)
        {
            Coordinate = coordinate;
            Kind = kind;
            StableId = stableId ?? string.Empty;
            Reason = reason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "SOFTLOCK", Coordinate == null ? "MISSING" : Coordinate.ToString(),
                Kind.ToString().ToUpperInvariant(), StableId, Reason,
            });
        }

        public FinalCanvasCellCoordinate Coordinate { get; }
        public FinalRouteFailureKind Kind { get; }
        public string StableId { get; }
        public string Reason { get; }
        public string StableToken { get; }

        public int CompareTo(FinalRouteSoftlockCandidate other)
        {
            if (other == null) return -1;
            var comparison = Coordinate == null
                ? (other.Coordinate == null ? 0 : 1)
                : (other.Coordinate == null ? -1 : Coordinate.CompareTo(other.Coordinate));
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            return comparison != 0
                ? comparison
                : string.Compare(StableId, other.StableId, StringComparison.Ordinal);
        }
    }

    public sealed class FinalRouteRecoveryRequest
    {
        private readonly ReadOnlyCollection<FinalRouteAnchor> anchors;
        private readonly ReadOnlyCollection<FinalRouteEdge> declaredEdges;

        public FinalRouteRecoveryRequest(
            SectorFinalCanvasLayerPlan canvasPlan,
            SectorCanvasProtectionDensityReport protectionDensityReport,
            IEnumerable<FinalRouteAnchor> sourceAnchors,
            IEnumerable<FinalRouteEdge> sourceDeclaredEdges,
            string publicationLabel,
            int fallbackCarveCount = 0,
            int silentWideningCount = 0,
            int sectorRerenderCount = 0,
            int wholeWorldRerandomCount = 0,
            int playerPhysicsSimulationCount = 0,
            int playModeRunCount = 0,
            int tilemapBakeCount = 0,
            int sliceCreationCount = 0,
            int generatedFileWriteCount = 0,
            int tilemapMutationCount = 0,
            int sceneMutationCount = 0,
            int prefabMutationCount = 0,
            int gameObjectMutationCount = 0,
            int gameplaySpawnCount = 0,
            int fullRegressionCount = 0,
            int productionSeedApprovalCount = 0)
        {
            CanvasPlan = canvasPlan;
            ProtectionDensityReport = protectionDensityReport;
            var rawAnchors = (sourceAnchors ?? Array.Empty<FinalRouteAnchor>()).ToArray();
            var rawEdges = (sourceDeclaredEdges ?? Array.Empty<FinalRouteEdge>()).ToArray();
            NullAnchorCount = rawAnchors.Count(value => value == null);
            NullDeclaredEdgeCount = rawEdges.Count(value => value == null);
            anchors = new ReadOnlyCollection<FinalRouteAnchor>(rawAnchors
                .Where(value => value != null).OrderBy(value => value).ToArray());
            declaredEdges = new ReadOnlyCollection<FinalRouteEdge>(rawEdges
                .Where(value => value != null).OrderBy(value => value).ToArray());
            PublicationLabel = publicationLabel ?? string.Empty;
            FallbackCarveCount = fallbackCarveCount;
            SilentWideningCount = silentWideningCount;
            SectorRerenderCount = sectorRerenderCount;
            WholeWorldRerandomCount = wholeWorldRerandomCount;
            PlayerPhysicsSimulationCount = playerPhysicsSimulationCount;
            PlayModeRunCount = playModeRunCount;
            TilemapBakeCount = tilemapBakeCount;
            SliceCreationCount = sliceCreationCount;
            GeneratedFileWriteCount = generatedFileWriteCount;
            TilemapMutationCount = tilemapMutationCount;
            SceneMutationCount = sceneMutationCount;
            PrefabMutationCount = prefabMutationCount;
            GameObjectMutationCount = gameObjectMutationCount;
            GameplaySpawnCount = gameplaySpawnCount;
            FullRegressionCount = fullRegressionCount;
            ProductionSeedApprovalCount = productionSeedApprovalCount;
            CanonicalDigest = FinalRouteRecoveryDigest.ComputeInput(this);
        }

        public SectorFinalCanvasLayerPlan CanvasPlan { get; }
        public SectorCanvasProtectionDensityReport ProtectionDensityReport { get; }
        public IReadOnlyList<FinalRouteAnchor> Anchors => anchors;
        public IReadOnlyList<FinalRouteEdge> DeclaredEdges => declaredEdges;
        public int NullAnchorCount { get; }
        public int NullDeclaredEdgeCount { get; }
        public string PublicationLabel { get; }
        public int FallbackCarveCount { get; }
        public int SilentWideningCount { get; }
        public int SectorRerenderCount { get; }
        public int WholeWorldRerandomCount { get; }
        public int PlayerPhysicsSimulationCount { get; }
        public int PlayModeRunCount { get; }
        public int TilemapBakeCount { get; }
        public int SliceCreationCount { get; }
        public int GeneratedFileWriteCount { get; }
        public int TilemapMutationCount { get; }
        public int SceneMutationCount { get; }
        public int PrefabMutationCount { get; }
        public int GameObjectMutationCount { get; }
        public int GameplaySpawnCount { get; }
        public int FullRegressionCount { get; }
        public int ProductionSeedApprovalCount { get; }
        public string CanonicalDigest { get; }
    }

    public sealed class SectorFinalRouteRecoveryReport
    {
        private readonly ReadOnlyCollection<FinalRouteNode> nodes;
        private readonly ReadOnlyCollection<FinalRouteEdge> edges;
        private readonly ReadOnlyCollection<FinalRouteWitness> witnesses;
        private readonly ReadOnlyCollection<FinalRecoveryWitness> recoveryWitnesses;
        private readonly ReadOnlyCollection<FinalRouteSoftlockCandidate> softlockCandidates;

        internal SectorFinalRouteRecoveryReport(
            FinalRouteRecoveryRequest request,
            IEnumerable<FinalRouteNode> sourceNodes,
            IEnumerable<FinalRouteEdge> sourceEdges,
            IEnumerable<FinalRouteWitness> sourceWitnesses,
            IEnumerable<FinalRecoveryWitness> sourceRecoveryWitnesses,
            IEnumerable<FinalRouteSoftlockCandidate> sourceSoftlockCandidates,
            int solidCrossingCount,
            int hazardCrossingCount,
            int blockedProtectionCrossingCount)
        {
            Request = request;
            nodes = ReadOnlySorted(sourceNodes);
            edges = ReadOnlySorted(sourceEdges);
            witnesses = ReadOnlySorted(sourceWitnesses);
            recoveryWitnesses = ReadOnlySorted(sourceRecoveryWitnesses);
            softlockCandidates = ReadOnlySorted(sourceSoftlockCandidates);
            SolidCrossingCount = solidCrossingCount;
            HazardCrossingCount = hazardCrossingCount;
            BlockedProtectionCrossingCount = blockedProtectionCrossingCount;
            OutputDigest = FinalRouteRecoveryDigest.ComputeOutput(this);
        }

        public const int SectorWidth = SectorFinalCanvasLayerPlan.SectorWidth;
        public const int SectorHeight = SectorFinalCanvasLayerPlan.SectorHeight;
        public const int CellCount = SectorFinalCanvasLayerPlan.CellCount;
        public const string PolicyVersion = "MAP16_03_FINAL_ROUTE_RECOVERY_POLICY_V1";
        public const string DownstreamOwner =
            "MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION";
        public const bool OpensDownstreamTask = false;

        public FinalRouteRecoveryRequest Request { get; }
        public SectorFinalCanvasLayerPlan SourceCanvasPlan => Request.CanvasPlan;
        public SectorCanvasProtectionDensityReport SourceProtectionDensityReport =>
            Request.ProtectionDensityReport;
        public string SectorId => SourceCanvasPlan.Request.SectorId;
        public string SourceCanvasInputDigest => SourceCanvasPlan.InputDigest;
        public string SourceCanvasOutputDigest => SourceCanvasPlan.OutputDigest;
        public string SourceProtectionDensityInputDigest => SourceProtectionDensityReport.InputDigest;
        public string SourceProtectionDensityOutputDigest => SourceProtectionDensityReport.OutputDigest;
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int Width => SourceCanvasPlan.Request.Width;
        public int Height => SourceCanvasPlan.Request.Height;
        public int ObservedCellCount => SourceCanvasPlan.ObservedCellCount;
        public int UniqueCoordinateCount => SourceCanvasPlan.UniqueCoordinateCount;
        public IReadOnlyList<FinalRouteAnchor> Anchors => Request.Anchors;
        public FinalRouteAnchor BaseEntryAnchor => Anchors.Single(value =>
            value.Kind == FinalRouteNodeKind.BaseEntry);
        public FinalRouteAnchor BaseExitAnchor => Anchors.Single(value =>
            value.Kind == FinalRouteNodeKind.BaseExit);
        public IReadOnlyList<FinalRouteAnchor> ExternalSocketAnchors => FilterAnchors(
            FinalRouteNodeKind.ExternalSocket);
        public IReadOnlyList<FinalRouteAnchor> BoundaryApertureAnchors => FilterAnchors(
            FinalRouteNodeKind.BoundaryAperture);
        public IReadOnlyList<FinalRouteAnchor> HighRouteBranchAnchors => FilterAnchors(
            FinalRouteNodeKind.HighRouteBranch);
        public IReadOnlyList<FinalRouteAnchor> FailureSampleAnchors => FilterAnchors(
            FinalRouteNodeKind.FailureSample);
        public IReadOnlyList<FinalRouteAnchor> SpecialEntranceAnchors => FilterAnchors(
            FinalRouteNodeKind.SpecialEntrance);
        public IReadOnlyList<FinalRouteNode> Nodes => nodes;
        public IReadOnlyList<FinalRouteEdge> Edges => edges;
        public IReadOnlyList<FinalRouteWitness> Witnesses => witnesses;
        public IReadOnlyList<FinalRecoveryWitness> RecoveryWitnesses => recoveryWitnesses;
        public IReadOnlyList<FinalRouteSoftlockCandidate> SoftlockCandidates => softlockCandidates;
        public int RouteNodeCount => nodes.Count;
        public int RouteEdgeCount => edges.Count;
        public bool BaseRouteWitnessExists => CountWitnesses(
            FinalRouteWitnessKind.BaseEntryToExit) == 1;
        public bool BaseRouteStartEndMatch => BaseRouteWitnessExists &&
            witnesses.Single(value => value.Kind == FinalRouteWitnessKind.BaseEntryToExit)
                .Start.StableId == BaseEntryAnchor.StableId &&
            witnesses.Single(value => value.Kind == FinalRouteWitnessKind.BaseEntryToExit)
                .End.StableId == BaseExitAnchor.StableId;
        public int BaseRouteWitnessRequiredCount => 1;
        public int BaseRouteWitnessCoveredCount => CountWitnesses(
            FinalRouteWitnessKind.BaseEntryToExit);
        public int BaseRouteWitnessMissingCount =>
            BaseRouteWitnessRequiredCount - BaseRouteWitnessCoveredCount;
        public int ExternalSocketWitnessRequiredCount => RequiredAnchors(
            FinalRouteNodeKind.ExternalSocket);
        public int ExternalSocketWitnessCoveredCount => CountWitnesses(
            FinalRouteWitnessKind.ExternalSocketToBaseRoute);
        public int ExternalSocketWitnessMissingCount => ExternalSocketWitnessRequiredCount -
                                                        ExternalSocketWitnessCoveredCount;
        public int BoundaryApertureWitnessRequiredCount => RequiredAnchors(
            FinalRouteNodeKind.BoundaryAperture);
        public int BoundaryApertureWitnessCoveredCount => CountWitnesses(
            FinalRouteWitnessKind.BoundaryApertureToBaseRoute);
        public int BoundaryApertureWitnessMissingCount => BoundaryApertureWitnessRequiredCount -
                                                         BoundaryApertureWitnessCoveredCount;
        public int SpecialEntranceWitnessRequiredCount => RequiredAnchors(
            FinalRouteNodeKind.SpecialEntrance);
        public int SpecialEntranceWitnessCoveredCount => CountWitnesses(
            FinalRouteWitnessKind.SpecialEntranceToBaseRoute);
        public int SpecialEntranceWitnessMissingCount => SpecialEntranceWitnessRequiredCount -
                                                        SpecialEntranceWitnessCoveredCount;
        public int HighFailureSampleRequiredCount => RequiredAnchors(
            FinalRouteNodeKind.FailureSample);
        public int HighFailureSampleCoveredCount => CountWitnesses(
            FinalRouteWitnessKind.HighFailureToBaseRecovery);
        public int HighFailureSampleMissingCount => HighFailureSampleRequiredCount -
                                                   HighFailureSampleCoveredCount;
        public int RecoveryWitnessRequiredCount => HighFailureSampleRequiredCount;
        public int RecoveryWitnessCoveredCount => recoveryWitnesses.Count(value =>
            value.Verdict == FinalRouteWitnessVerdict.Covered);
        public int RecoveryWitnessMissingCount => RecoveryWitnessRequiredCount -
                                                  RecoveryWitnessCoveredCount;
        public int SolidCrossingCount { get; }
        public int HazardCrossingCount { get; }
        public int BlockedProtectionCrossingCount { get; }
        public int BlockedCellCrossingCount => SolidCrossingCount + HazardCrossingCount +
                                               BlockedProtectionCrossingCount;
        public int StaticSoftlockCandidateCount => softlockCandidates.Count;
        public int FallbackCarveCount => SourceCanvasPlan.FallbackCarveCount +
                                         Request.FallbackCarveCount;
        public int SilentWideningCount => Request.SilentWideningCount;
        public int SectorRerenderCount => Request.SectorRerenderCount;
        public int WholeWorldRerandomCount => Request.WholeWorldRerandomCount;
        public int PlayerPhysicsSimulationCount => Request.PlayerPhysicsSimulationCount;
        public int PlayModeRunCount => Request.PlayModeRunCount;
        public int TilemapBakeCount => Request.TilemapBakeCount;
        public int SliceCreationCount => SourceCanvasPlan.SliceCreationCount +
                                         Request.SliceCreationCount;
        public int GeneratedFileWriteCount => SourceCanvasPlan.GeneratedFileWriteCount +
                                              Request.GeneratedFileWriteCount;
        public int TilemapMutationCount => SourceCanvasPlan.TilemapMutationCount +
                                           Request.TilemapMutationCount;
        public int SceneMutationCount => SourceCanvasPlan.SceneMutationCount +
                                         Request.SceneMutationCount;
        public int PrefabMutationCount => SourceCanvasPlan.PrefabMutationCount +
                                          Request.PrefabMutationCount;
        public int GameObjectMutationCount => SourceCanvasPlan.GameObjectMutationCount +
                                              Request.GameObjectMutationCount;
        public int GameplaySpawnCount => SourceCanvasPlan.GameplaySpawnCount +
                                         Request.GameplaySpawnCount;
        public int FullRegressionCount => SourceCanvasPlan.FullRegressionCount +
                                          Request.FullRegressionCount;
        public int ProductionSeedApprovalCount => SourceCanvasPlan.ProductionSeedApprovalCount +
                                                  Request.ProductionSeedApprovalCount;

        public int CountWitnesses(FinalRouteWitnessKind kind) => witnesses.Count(value =>
            value.Kind == kind && value.Verdict == FinalRouteWitnessVerdict.Covered);

        private IReadOnlyList<FinalRouteAnchor> FilterAnchors(FinalRouteNodeKind kind) =>
            new ReadOnlyCollection<FinalRouteAnchor>(Anchors.Where(value => value.Kind == kind)
                .OrderBy(value => value).ToArray());

        private int RequiredAnchors(FinalRouteNodeKind kind) => Anchors.Count(value =>
            value.Kind == kind && value.Required);

        private static ReadOnlyCollection<T> ReadOnlySorted<T>(IEnumerable<T> source)
            where T : IComparable<T> => new ReadOnlyCollection<T>((source ?? Array.Empty<T>())
                .OrderBy(value => value).ToArray());
    }

    public sealed class FinalRouteRecoveryFailure :
        IComparable<FinalRouteRecoveryFailure>, IEquatable<FinalRouteRecoveryFailure>
    {
        public FinalRouteRecoveryFailure(
            FinalRouteFailureKind code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public FinalRouteFailureKind Code { get; }
        public string Subject { get; }
        public string Reason { get; }

        public int CompareTo(FinalRouteRecoveryFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }

        public bool Equals(FinalRouteRecoveryFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as FinalRouteRecoveryFailure);
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Code;
                hashCode = (hashCode * 397) ^ Subject.GetHashCode();
                hashCode = (hashCode * 397) ^ Reason.GetHashCode();
                return hashCode;
            }
        }
        public override string ToString() => Code + ":" + Subject + ":" + Reason;
    }

    public sealed class FinalRouteRecoveryResult
    {
        private readonly ReadOnlyCollection<FinalRouteRecoveryFailure> failures;

        internal FinalRouteRecoveryResult(
            FinalRouteRecoveryRequest request,
            SectorFinalRouteRecoveryReport report,
            IEnumerable<FinalRouteRecoveryFailure> sourceFailures)
        {
            Request = request;
            Report = report;
            failures = new ReadOnlyCollection<FinalRouteRecoveryFailure>((sourceFailures ??
                Array.Empty<FinalRouteRecoveryFailure>()).Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => Report != null && failures.Count == 0;
        public FinalRouteRecoveryRequest Request { get; }
        public SectorFinalRouteRecoveryReport Report { get; }
        public IReadOnlyList<FinalRouteRecoveryFailure> Failures => failures;
        public string InputDigest => Report == null ? string.Empty : Report.InputDigest;
        public string OutputDigest => Report == null ? string.Empty : Report.OutputDigest;
    }

    public static class FinalRouteRecoveryDigest
    {
        public static string ComputeInput(FinalRouteRecoveryRequest request)
        {
            if (request == null) return string.Empty;
            var plan = request.CanvasPlan;
            var density = request.ProtectionDensityReport;
            var lines = new List<string>
            {
                "POLICY|" + SectorFinalRouteRecoveryReport.PolicyVersion,
                "SECTOR|" + (plan == null || plan.Request == null
                    ? string.Empty : plan.Request.SectorId),
                "DIMENSIONS|" + Number(plan == null || plan.Request == null
                    ? 0 : plan.Request.Width) + "|" + Number(plan == null || plan.Request == null
                    ? 0 : plan.Request.Height),
                "CANVAS_INPUT|" + (plan == null ? string.Empty : plan.InputDigest),
                "CANVAS_OUTPUT|" + (plan == null ? string.Empty : plan.OutputDigest),
                "DENSITY_INPUT|" + (density == null ? string.Empty : density.InputDigest),
                "DENSITY_OUTPUT|" + (density == null ? string.Empty : density.OutputDigest),
                "PUBLICATION|" + request.PublicationLabel,
                "NULLS|" + Number(request.NullAnchorCount) + "|" +
                    Number(request.NullDeclaredEdgeCount),
                "OPERATIONS|" + string.Join("|", OperationCounts(request)),
            };
            lines.AddRange(request.Anchors.OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange(request.DeclaredEdges.OrderBy(value => value)
                .Select(value => value.StableToken));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(SectorFinalRouteRecoveryReport report)
        {
            if (report == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + SectorFinalRouteRecoveryReport.PolicyVersion,
                "INPUT|" + report.InputDigest,
                "COUNTS|" + Number(report.RouteNodeCount) + "|" +
                    Number(report.RouteEdgeCount) + "|" +
                    Number(report.Witnesses.Count) + "|" +
                    Number(report.RecoveryWitnesses.Count) + "|" +
                    Number(report.StaticSoftlockCandidateCount),
                "CROSSINGS|" + Number(report.SolidCrossingCount) + "|" +
                    Number(report.HazardCrossingCount) + "|" +
                    Number(report.BlockedProtectionCrossingCount),
                "WITNESS_COUNTS|" + string.Join("|", new[]
                {
                    Number(report.BaseRouteWitnessRequiredCount),
                    Number(report.BaseRouteWitnessCoveredCount),
                    Number(report.ExternalSocketWitnessRequiredCount),
                    Number(report.ExternalSocketWitnessCoveredCount),
                    Number(report.BoundaryApertureWitnessRequiredCount),
                    Number(report.BoundaryApertureWitnessCoveredCount),
                    Number(report.SpecialEntranceWitnessRequiredCount),
                    Number(report.SpecialEntranceWitnessCoveredCount),
                    Number(report.HighFailureSampleRequiredCount),
                    Number(report.HighFailureSampleCoveredCount),
                    Number(report.RecoveryWitnessRequiredCount),
                    Number(report.RecoveryWitnessCoveredCount),
                }),
                "MUTATIONS|" + string.Join("|", MutationCounts(report)),
                "DOWNSTREAM|" + SectorFinalRouteRecoveryReport.DownstreamOwner + "|" +
                    (SectorFinalRouteRecoveryReport.OpensDownstreamTask ? "1" : "0"),
            };
            lines.AddRange(report.Nodes.OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange(report.Edges.OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange(report.Witnesses.OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange(report.RecoveryWitnesses.OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange(report.SoftlockCandidates.OrderBy(value => value)
                .Select(value => value.StableToken));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string text) =>
            BakingCanonicalDigest.HashCanonicalText(text ?? string.Empty);

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);

        private static string[] OperationCounts(FinalRouteRecoveryRequest request) => new[]
        {
            Number(request.FallbackCarveCount), Number(request.SilentWideningCount),
            Number(request.SectorRerenderCount), Number(request.WholeWorldRerandomCount),
            Number(request.PlayerPhysicsSimulationCount), Number(request.PlayModeRunCount),
            Number(request.TilemapBakeCount), Number(request.SliceCreationCount),
            Number(request.GeneratedFileWriteCount), Number(request.TilemapMutationCount),
            Number(request.SceneMutationCount), Number(request.PrefabMutationCount),
            Number(request.GameObjectMutationCount), Number(request.GameplaySpawnCount),
            Number(request.FullRegressionCount), Number(request.ProductionSeedApprovalCount),
        };

        private static string[] MutationCounts(SectorFinalRouteRecoveryReport report) => new[]
        {
            Number(report.FallbackCarveCount), Number(report.SilentWideningCount),
            Number(report.SectorRerenderCount), Number(report.WholeWorldRerandomCount),
            Number(report.PlayerPhysicsSimulationCount), Number(report.PlayModeRunCount),
            Number(report.TilemapBakeCount), Number(report.SliceCreationCount),
            Number(report.GeneratedFileWriteCount), Number(report.TilemapMutationCount),
            Number(report.SceneMutationCount), Number(report.PrefabMutationCount),
            Number(report.GameObjectMutationCount), Number(report.GameplaySpawnCount),
            Number(report.FullRegressionCount), Number(report.ProductionSeedApprovalCount),
        };

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
