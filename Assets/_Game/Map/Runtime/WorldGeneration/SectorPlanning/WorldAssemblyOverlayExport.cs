using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum WorldAssemblyOverlayLayerKind
    {
        Topology = 0,
        SolveOrder = 1,
        IntersectorEdges = 2,
        BoundaryPairs = 3,
        SpecialReservations = 4,
        ClusterReservations = 5,
        PacingDensity = 6,
        ActivityEventCaps = 7,
        RollbackScopes = 8,
        FailureReports = 9,
        HashChain = 10,
        MutationProof = 11,
    }

    public enum WorldAssemblyOverlayTokenKind
    {
        Sector = 0,
        Edge = 1,
        Layer = 2,
        Hash = 3,
        BatchCase = 4,
        SolverUpperBound = 5,
    }

    public enum WorldAssemblyOverlaySeverity
    {
        Information = 0,
        Warning = 1,
        Error = 2,
    }

    public enum WorldAssemblyHashKind
    {
        Input = 0,
        Output = 1,
    }

    public enum WorldSolverUpperBoundKind
    {
        SolveSteps = 0,
        InternalEdges = 1,
        EdgeEndpoints = 2,
        RollbackSectorsPerFailure = 3,
        SectorLocalRetryAttempts = 4,
        WholeWorldRerandom = 5,
        FallbackCarve = 6,
        SilentWidening = 7,
        FileWrites = 8,
        ScenePrefabTilemapGameObjectMutations = 9,
    }

    public enum WorldAssemblyOverlayFailureCode
    {
        MissingRequest = 0,
        MissingWorldSolveOrder = 1,
        FailedWorldSolveOrder = 2,
        MissingIntersectorPlan = 3,
        MissingReservationPlan = 4,
        MissingPacingDensityPlan = 5,
        MissingRollbackPlan = 6,
        InvalidWorldDimensions = 7,
        InvalidWorldSectorCount = 8,
        InvalidInternalEdgeCount = 9,
        InvalidEndpointCount = 10,
        InvalidAuthorityLink = 11,
        InvalidDigest = 12,
        MissingRequiredLayer = 13,
        InvalidBatchLabels = 14,
        ProductionSeedApprovalForbidden = 15,
        SolverUpperBoundExceeded = 16,
        WholeWorldRerandomForbidden = 17,
        FallbackCarveForbidden = 18,
        SilentWideningForbidden = 19,
        FileWriteForbidden = 20,
        MutationClaim = 21,
        InvalidOverlayToken = 22,
    }

    public sealed class WorldAssemblyOverlaySector : IComparable<WorldAssemblyOverlaySector>
    {
        public WorldAssemblyOverlaySector(
            WorldSectorId sectorId,
            WorldSectorCoordinate coordinate,
            int solveStepIndex,
            int routeType,
            AccessClass accessClass,
            PacingRole pacingRole,
            int specialMarkerCount,
            int reservationMarkerCount,
            int pacingWindowCount,
            int pacingBudgetCount,
            int rollbackMarkerCount,
            int failureMarkerCount,
            string stableToken)
        {
            SectorId = sectorId;
            Coordinate = coordinate;
            SolveStepIndex = solveStepIndex;
            RouteType = routeType;
            AccessClass = accessClass;
            PacingRole = pacingRole;
            SpecialMarkerCount = specialMarkerCount;
            ReservationMarkerCount = reservationMarkerCount;
            PacingWindowCount = pacingWindowCount;
            PacingBudgetCount = pacingBudgetCount;
            RollbackMarkerCount = rollbackMarkerCount;
            FailureMarkerCount = failureMarkerCount;
            StableToken = stableToken ?? string.Empty;
        }

        public WorldAssemblyOverlayTokenKind TokenKind => WorldAssemblyOverlayTokenKind.Sector;
        public WorldSectorId SectorId { get; }
        public WorldSectorCoordinate Coordinate { get; }
        public int SolveStepIndex { get; }
        public int RouteType { get; }
        public AccessClass AccessClass { get; }
        public PacingRole PacingRole { get; }
        public int SpecialMarkerCount { get; }
        public int ReservationMarkerCount { get; }
        public int PacingWindowCount { get; }
        public int PacingBudgetCount { get; }
        public int RollbackMarkerCount { get; }
        public int FailureMarkerCount { get; }
        public string StableToken { get; }

        public int CompareTo(WorldAssemblyOverlaySector other) =>
            other == null ? -1 : SectorId.CompareTo(other.SectorId);
    }

    public sealed class WorldAssemblyOverlayEdge : IComparable<WorldAssemblyOverlayEdge>
    {
        public WorldAssemblyOverlayEdge(
            WorldIntersectorEdgeId edgeId,
            WorldSectorId minSectorId,
            WorldSectorId maxSectorId,
            WorldEdgeOrientation orientation,
            int endpointCount,
            bool socketCompatible,
            bool boundaryPair,
            bool mandatoryRoute,
            bool externalSocket,
            string edgeDigest,
            string stableToken)
        {
            EdgeId = edgeId;
            MinSectorId = minSectorId;
            MaxSectorId = maxSectorId;
            Orientation = orientation;
            EndpointCount = endpointCount;
            SocketCompatible = socketCompatible;
            BoundaryPair = boundaryPair;
            MandatoryRoute = mandatoryRoute;
            ExternalSocket = externalSocket;
            EdgeDigest = edgeDigest ?? string.Empty;
            StableToken = stableToken ?? string.Empty;
        }

        public WorldAssemblyOverlayTokenKind TokenKind => WorldAssemblyOverlayTokenKind.Edge;
        public WorldIntersectorEdgeId EdgeId { get; }
        public WorldSectorId MinSectorId { get; }
        public WorldSectorId MaxSectorId { get; }
        public WorldEdgeOrientation Orientation { get; }
        public int EndpointCount { get; }
        public bool SocketCompatible { get; }
        public bool BoundaryPair { get; }
        public bool MandatoryRoute { get; }
        public bool ExternalSocket { get; }
        public string EdgeDigest { get; }
        public string StableToken { get; }

        public int CompareTo(WorldAssemblyOverlayEdge other) =>
            other == null ? -1 : EdgeId.CompareTo(other.EdgeId);
    }

    public sealed class WorldAssemblyOverlayLayer : IComparable<WorldAssemblyOverlayLayer>
    {
        private readonly ReadOnlyCollection<string> tokens;

        public WorldAssemblyOverlayLayer(
            WorldAssemblyOverlayLayerKind kind,
            WorldAssemblyOverlaySeverity severity,
            IEnumerable<string> sourceTokens,
            string unavailableReason)
        {
            Kind = kind;
            Severity = severity;
            tokens = new ReadOnlyCollection<string>((sourceTokens ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
            UnavailableReason = unavailableReason ?? string.Empty;
            StableId = "LAYER_" + kind.ToString().ToUpperInvariant();
        }

        public WorldAssemblyOverlayTokenKind TokenKind => WorldAssemblyOverlayTokenKind.Layer;
        public WorldAssemblyOverlayLayerKind Kind { get; }
        public WorldAssemblyOverlaySeverity Severity { get; }
        public string StableId { get; }
        public IReadOnlyList<string> Tokens => tokens;
        public string UnavailableReason { get; }
        public bool IsAvailable => tokens.Count > 0;
        public bool HasExplicitUnavailableReason => !string.IsNullOrEmpty(UnavailableReason);
        public int ItemCount => tokens.Count;

        public int CompareTo(WorldAssemblyOverlayLayer other) =>
            other == null ? -1 : Kind.CompareTo(other.Kind);
    }

    public sealed class WorldAssemblyHashRecord : IComparable<WorldAssemblyHashRecord>
    {
        public WorldAssemblyHashRecord(string taskId, WorldAssemblyHashKind kind, string digest)
        {
            TaskId = taskId ?? string.Empty;
            Kind = kind;
            Digest = digest ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "HASH", TaskId, Kind.ToString().ToUpperInvariant(), Digest,
            });
        }

        public WorldAssemblyOverlayTokenKind TokenKind => WorldAssemblyOverlayTokenKind.Hash;
        public string TaskId { get; }
        public WorldAssemblyHashKind Kind { get; }
        public string Digest { get; }
        public string StableToken { get; }

        public int CompareTo(WorldAssemblyHashRecord other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(TaskId, other.TaskId, StringComparison.Ordinal);
            return comparison != 0 ? comparison : Kind.CompareTo(other.Kind);
        }
    }

    public sealed class WorldSolverUpperBound : IComparable<WorldSolverUpperBound>
    {
        public WorldSolverUpperBound(
            WorldSolverUpperBoundKind kind,
            int actual,
            int limit,
            string sourceOwner)
        {
            Kind = kind;
            Actual = actual;
            Limit = limit;
            SourceOwner = sourceOwner ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "BOUND", Kind.ToString().ToUpperInvariant(),
                Actual.ToString(CultureInfo.InvariantCulture),
                Limit.ToString(CultureInfo.InvariantCulture), SourceOwner,
            });
        }

        public WorldAssemblyOverlayTokenKind TokenKind => WorldAssemblyOverlayTokenKind.SolverUpperBound;
        public WorldSolverUpperBoundKind Kind { get; }
        public int Actual { get; }
        public int Limit { get; }
        public string SourceOwner { get; }
        public bool Pass => Actual >= 0 && Actual <= Limit;
        public string StableToken { get; }

        public int CompareTo(WorldSolverUpperBound other) =>
            other == null ? -1 : Kind.CompareTo(other.Kind);
    }

    public sealed class WorldBatchPlanCase : IComparable<WorldBatchPlanCase>
    {
        public WorldBatchPlanCase(
            string label,
            int connectedComponentCount,
            int duplicateIdCount,
            int missingRequiredBoundaryPairCount,
            int untypedReservationConflictCount,
            int acceptedPacingViolationCount,
            int maximumRollbackSectorCount,
            bool solverUpperBoundsPass,
            bool productionSeedApproval)
        {
            Label = label ?? string.Empty;
            ConnectedComponentCount = connectedComponentCount;
            DuplicateIdCount = duplicateIdCount;
            MissingRequiredBoundaryPairCount = missingRequiredBoundaryPairCount;
            UntypedReservationConflictCount = untypedReservationConflictCount;
            AcceptedPacingViolationCount = acceptedPacingViolationCount;
            MaximumRollbackSectorCount = maximumRollbackSectorCount;
            SolverUpperBoundsPass = solverUpperBoundsPass;
            ProductionSeedApproval = productionSeedApproval;
            StableToken = string.Join("|", new[]
            {
                "BATCH", Label,
                ConnectedComponentCount.ToString(CultureInfo.InvariantCulture),
                DuplicateIdCount.ToString(CultureInfo.InvariantCulture),
                MissingRequiredBoundaryPairCount.ToString(CultureInfo.InvariantCulture),
                UntypedReservationConflictCount.ToString(CultureInfo.InvariantCulture),
                AcceptedPacingViolationCount.ToString(CultureInfo.InvariantCulture),
                MaximumRollbackSectorCount.ToString(CultureInfo.InvariantCulture),
                SolverUpperBoundsPass ? "1" : "0", ProductionSeedApproval ? "1" : "0",
            });
        }

        public WorldAssemblyOverlayTokenKind TokenKind => WorldAssemblyOverlayTokenKind.BatchCase;
        public string Label { get; }
        public int ConnectedComponentCount { get; }
        public int DuplicateIdCount { get; }
        public int MissingRequiredBoundaryPairCount { get; }
        public int UntypedReservationConflictCount { get; }
        public int AcceptedPacingViolationCount { get; }
        public int MaximumRollbackSectorCount { get; }
        public bool SolverUpperBoundsPass { get; }
        public bool ProductionSeedApproval { get; }
        public bool GraphVerdictPass => ConnectedComponentCount == 1 && DuplicateIdCount == 0;
        public bool ReservationVerdictPass => MissingRequiredBoundaryPairCount == 0 &&
                                              UntypedReservationConflictCount == 0;
        public bool PacingVerdictPass => AcceptedPacingViolationCount == 0;
        public bool RollbackVerdictPass => MaximumRollbackSectorCount <=
                                           WorldNeighborRollbackPlan.MaximumScopeSectorCount;
        public bool Pass => GraphVerdictPass && ReservationVerdictPass && PacingVerdictPass &&
                            RollbackVerdictPass && SolverUpperBoundsPass && !ProductionSeedApproval;
        public string StableToken { get; }

        public int CompareTo(WorldBatchPlanCase other) => other == null
            ? -1
            : string.Compare(Label, other.Label, StringComparison.Ordinal);
    }

    public sealed class WorldBatchPlanReport
    {
        private readonly ReadOnlyCollection<WorldBatchPlanCase> cases;
        private readonly ReadOnlyCollection<WorldSolverUpperBound> solverUpperBounds;

        public WorldBatchPlanReport(
            IEnumerable<WorldBatchPlanCase> sourceCases,
            IEnumerable<WorldSolverUpperBound> sourceSolverUpperBounds)
        {
            cases = new ReadOnlyCollection<WorldBatchPlanCase>((sourceCases ??
                Array.Empty<WorldBatchPlanCase>()).Where(value => value != null)
                .OrderBy(value => WorldAssemblyOverlayExport.RequiredBatchLabelOrder(value.Label))
                .ThenBy(value => value.Label, StringComparer.Ordinal).ToArray());
            solverUpperBounds = new ReadOnlyCollection<WorldSolverUpperBound>((sourceSolverUpperBounds ??
                Array.Empty<WorldSolverUpperBound>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        public IReadOnlyList<WorldBatchPlanCase> Cases => cases;
        public IReadOnlyList<WorldSolverUpperBound> SolverUpperBounds => solverUpperBounds;
        public int RequiredCaseCount => WorldAssemblyOverlayExport.RequiredBatchCaseCount;
        public int CoveredCaseCount => cases.Select(value => value.Label)
            .Intersect(WorldAssemblyOverlayExport.RequiredBatchLabels, StringComparer.Ordinal).Count();
        public int MissingCaseCount => RequiredCaseCount - CoveredCaseCount;
        public int PassingCaseCount => cases.Count(value => value.Pass);
        public int RequiredUpperBoundCount => WorldAssemblyOverlayExport.RequiredSolverUpperBoundCount;
        public int CoveredUpperBoundCount => solverUpperBounds.Select(value => value.Kind).Distinct().Count();
        public int MissingUpperBoundCount => RequiredUpperBoundCount - CoveredUpperBoundCount;
        public int UpperBoundViolationCount => solverUpperBounds.Count(value => !value.Pass);
        public int ProductionSeedApprovalCount => cases.Count(value => value.ProductionSeedApproval);
        public bool Pass => MissingCaseCount == 0 && PassingCaseCount == RequiredCaseCount &&
                            MissingUpperBoundCount == 0 && UpperBoundViolationCount == 0 &&
                            ProductionSeedApprovalCount == 0;
    }

    public sealed class WorldAssemblyOverlayRequest
    {
        private readonly ReadOnlyCollection<string> batchCaseLabels;

        public WorldAssemblyOverlayRequest(
            WorldSolveOrderResult solveOrder,
            WorldIntersectorEdgePlan intersectorPlan,
            WorldMultiSectorReservationPlan reservationPlan,
            WorldPacingDensityPlan pacingDensityPlan,
            WorldNeighborRollbackPlan rollbackPlan,
            IEnumerable<string> sourceBatchCaseLabels,
            string publicationLabel,
            int wholeWorldRerandomCount = 0,
            int fallbackCarveCount = 0,
            int silentWideningCount = 0,
            int generatedFileWriteCount = 0,
            int tilemapMutationCount = 0,
            int sceneMutationCount = 0,
            int prefabMutationCount = 0,
            int gameObjectMutationCount = 0,
            int gameplaySpawnCount = 0,
            int authoringMutationCount = 0,
            int worldPlanMutationCount = 0,
            int intersectorPlanMutationCount = 0,
            int reservationPlanMutationCount = 0,
            int pacingDensityPlanMutationCount = 0,
            int rollbackPlanMutationCount = 0,
            int productionSeedApprovalCount = 0,
            int fullRegressionCount = 0)
        {
            SolveOrder = solveOrder;
            IntersectorPlan = intersectorPlan;
            ReservationPlan = reservationPlan;
            PacingDensityPlan = pacingDensityPlan;
            RollbackPlan = rollbackPlan;
            var rawLabels = (sourceBatchCaseLabels ?? Array.Empty<string>()).ToArray();
            NullBatchCaseLabelCount = rawLabels.Count(value => value == null);
            batchCaseLabels = new ReadOnlyCollection<string>(rawLabels
                .Where(value => value != null).Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            PublicationLabel = publicationLabel ?? string.Empty;
            WholeWorldRerandomCount = wholeWorldRerandomCount;
            FallbackCarveCount = fallbackCarveCount;
            SilentWideningCount = silentWideningCount;
            GeneratedFileWriteCount = generatedFileWriteCount;
            TilemapMutationCount = tilemapMutationCount;
            SceneMutationCount = sceneMutationCount;
            PrefabMutationCount = prefabMutationCount;
            GameObjectMutationCount = gameObjectMutationCount;
            GameplaySpawnCount = gameplaySpawnCount;
            AuthoringMutationCount = authoringMutationCount;
            WorldPlanMutationCount = worldPlanMutationCount;
            IntersectorPlanMutationCount = intersectorPlanMutationCount;
            ReservationPlanMutationCount = reservationPlanMutationCount;
            PacingDensityPlanMutationCount = pacingDensityPlanMutationCount;
            RollbackPlanMutationCount = rollbackPlanMutationCount;
            ProductionSeedApprovalCount = productionSeedApprovalCount;
            FullRegressionCount = fullRegressionCount;
            CanonicalDigest = WorldAssemblyOverlayDigest.ComputeInput(this);
        }

        public WorldSolveOrderResult SolveOrder { get; }
        public WorldIntersectorEdgePlan IntersectorPlan { get; }
        public WorldMultiSectorReservationPlan ReservationPlan { get; }
        public WorldPacingDensityPlan PacingDensityPlan { get; }
        public WorldNeighborRollbackPlan RollbackPlan { get; }
        public IReadOnlyList<string> BatchCaseLabels => batchCaseLabels;
        public int NullBatchCaseLabelCount { get; }
        public string PublicationLabel { get; }
        public int WholeWorldRerandomCount { get; }
        public int FallbackCarveCount { get; }
        public int SilentWideningCount { get; }
        public int GeneratedFileWriteCount { get; }
        public int TilemapMutationCount { get; }
        public int SceneMutationCount { get; }
        public int PrefabMutationCount { get; }
        public int GameObjectMutationCount { get; }
        public int GameplaySpawnCount { get; }
        public int AuthoringMutationCount { get; }
        public int WorldPlanMutationCount { get; }
        public int IntersectorPlanMutationCount { get; }
        public int ReservationPlanMutationCount { get; }
        public int PacingDensityPlanMutationCount { get; }
        public int RollbackPlanMutationCount { get; }
        public int ProductionSeedApprovalCount { get; }
        public int FullRegressionCount { get; }
        public string CanonicalDigest { get; }
    }

    public sealed class WorldAssemblyOverlayExport
    {
        private readonly ReadOnlyCollection<WorldAssemblyOverlaySector> sectors;
        private readonly ReadOnlyCollection<WorldAssemblyOverlayEdge> edges;
        private readonly ReadOnlyCollection<WorldAssemblyOverlayLayer> layers;
        private readonly ReadOnlyCollection<WorldAssemblyHashRecord> hashRecords;

        internal WorldAssemblyOverlayExport(
            WorldAssemblyOverlayRequest request,
            IEnumerable<WorldAssemblyOverlaySector> sourceSectors,
            IEnumerable<WorldAssemblyOverlayEdge> sourceEdges,
            IEnumerable<WorldAssemblyOverlayLayer> sourceLayers,
            IEnumerable<WorldAssemblyHashRecord> sourceHashRecords,
            WorldBatchPlanReport batchReport,
            string outputDigest)
        {
            Request = request;
            sectors = Freeze(sourceSectors);
            edges = Freeze(sourceEdges);
            layers = Freeze(sourceLayers);
            hashRecords = Freeze(sourceHashRecords);
            BatchReport = batchReport;
            OutputDigest = outputDigest ?? string.Empty;
        }

        public const int WorldWidthTiles = GeneratedTerrainGeometrySnapshot.CanonicalWorldWidth;
        public const int WorldHeightTiles = GeneratedTerrainGeometrySnapshot.CanonicalWorldHeight;
        public const int SectorWidthTiles = GeneratedTerrainGeometrySnapshot.CanonicalSectorWidth;
        public const int SectorHeightTiles = GeneratedTerrainGeometrySnapshot.CanonicalSectorHeight;
        public const int SectorColumns = GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns;
        public const int SectorRows = GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorRows;
        public const int WorldSectorCount = GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorCount;
        public const int InternalEdgeCount = 312;
        public const int EdgeEndpointCount = 624;
        public const int RequiredLayerCount = 12;
        public const int RequiredHashRecordCount = 10;
        public const int RequiredBatchCaseCount = 4;
        public const int RequiredSolverUpperBoundCount = 10;
        public const string DownstreamOwner = "MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT";
        public const bool OpensDownstreamTask = false;

        private static readonly string[] requiredBatchLabels =
        {
            "REFERENCE_WORLD_BASELINE",
            "REFERENCE_WORLD_BOUNDARY_HEAVY",
            "REFERENCE_WORLD_SPECIAL_RESERVATION",
            "REFERENCE_WORLD_ROLLBACK_FAILURE",
        };

        private static readonly ReadOnlyCollection<string> readOnlyRequiredBatchLabels =
            new ReadOnlyCollection<string>(requiredBatchLabels);

        public WorldAssemblyOverlayRequest Request { get; }
        public static IReadOnlyList<string> RequiredBatchLabels => readOnlyRequiredBatchLabels;
        public IReadOnlyList<WorldAssemblyOverlaySector> Sectors => sectors;
        public IReadOnlyList<WorldAssemblyOverlayEdge> Edges => edges;
        public IReadOnlyList<WorldAssemblyOverlayLayer> Layers => layers;
        public IReadOnlyList<WorldAssemblyHashRecord> HashRecords => hashRecords;
        public WorldBatchPlanReport BatchReport { get; }
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int OverlaySectorCount => sectors.Count;
        public int OverlayEdgeCount => edges.Count;
        public int CoveredLayerCount => layers.Select(value => value.Kind).Distinct().Count();
        public int MissingLayerCount => RequiredLayerCount - CoveredLayerCount;
        public int CoveredHashRecordCount => hashRecords
            .Select(value => value.TaskId + "|" + value.Kind).Distinct(StringComparer.Ordinal).Count();
        public int MissingHashRecordCount => RequiredHashRecordCount - CoveredHashRecordCount;
        public int GeneratedFileWriteCount => Request.GeneratedFileWriteCount;
        public int TilemapMutationCount => Request.TilemapMutationCount;
        public int SceneMutationCount => Request.SceneMutationCount;
        public int PrefabMutationCount => Request.PrefabMutationCount;
        public int GameObjectMutationCount => Request.GameObjectMutationCount;
        public int GameplaySpawnCount => Request.GameplaySpawnCount;
        public int AuthoringMutationCount => Request.AuthoringMutationCount;
        public int WorldPlanMutationCount => Request.WorldPlanMutationCount;
        public int IntersectorPlanMutationCount => Request.IntersectorPlanMutationCount;
        public int ReservationPlanMutationCount => Request.ReservationPlanMutationCount;
        public int PacingDensityPlanMutationCount => Request.PacingDensityPlanMutationCount;
        public int RollbackPlanMutationCount => Request.RollbackPlanMutationCount;
        public int ProductionSeedApprovalCount => Request.ProductionSeedApprovalCount;
        public int FullRegressionCount => Request.FullRegressionCount;

        internal static int RequiredBatchLabelOrder(string label) =>
            Array.IndexOf(requiredBatchLabels, label);

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((source ?? Array.Empty<T>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
    }

    public sealed class WorldAssemblyOverlayFailure :
        IComparable<WorldAssemblyOverlayFailure>, IEquatable<WorldAssemblyOverlayFailure>
    {
        public WorldAssemblyOverlayFailure(WorldAssemblyOverlayFailureCode code, string subject, string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public WorldAssemblyOverlayFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }

        public int CompareTo(WorldAssemblyOverlayFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }

        public bool Equals(WorldAssemblyOverlayFailure other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as WorldAssemblyOverlayFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Reason;
    }

    public sealed class WorldAssemblyOverlayResult
    {
        private readonly ReadOnlyCollection<WorldAssemblyOverlayFailure> failures;

        private WorldAssemblyOverlayResult(
            WorldAssemblyOverlayExport export,
            IEnumerable<WorldAssemblyOverlayFailure> sourceFailures)
        {
            Export = export;
            failures = new ReadOnlyCollection<WorldAssemblyOverlayFailure>((sourceFailures ??
                Array.Empty<WorldAssemblyOverlayFailure>()).Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray());
        }

        public bool Success => Export != null && failures.Count == 0;
        public WorldAssemblyOverlayExport Export { get; }
        public IReadOnlyList<WorldAssemblyOverlayFailure> Failures => failures;
        public string InputDigest => Export == null ? string.Empty : Export.InputDigest;
        public string OutputDigest => Export == null ? string.Empty : Export.OutputDigest;

        internal static WorldAssemblyOverlayResult Pass(WorldAssemblyOverlayExport export) =>
            new WorldAssemblyOverlayResult(export, Array.Empty<WorldAssemblyOverlayFailure>());

        internal static WorldAssemblyOverlayResult Fail(IEnumerable<WorldAssemblyOverlayFailure> sourceFailures) =>
            new WorldAssemblyOverlayResult(null, sourceFailures);
    }

    public static class WorldAssemblyOverlayDigest
    {
        public static string ComputeInput(WorldAssemblyOverlayRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "MAP15_01_INPUT|" + Digest(request.SolveOrder, true),
                "MAP15_01_OUTPUT|" + Digest(request.SolveOrder, false),
                "MAP15_02_INPUT|" + Digest(request.IntersectorPlan, true),
                "MAP15_02_OUTPUT|" + Digest(request.IntersectorPlan, false),
                "MAP15_03_INPUT|" + Digest(request.ReservationPlan, true),
                "MAP15_03_OUTPUT|" + Digest(request.ReservationPlan, false),
                "MAP15_04_INPUT|" + Digest(request.PacingDensityPlan, true),
                "MAP15_04_OUTPUT|" + Digest(request.PacingDensityPlan, false),
                "MAP15_05_INPUT|" + Digest(request.RollbackPlan, true),
                "MAP15_05_OUTPUT|" + Digest(request.RollbackPlan, false),
                "PUBLICATION|" + Token(request.PublicationLabel),
                "FORBIDDEN|" + Numbers(request.WholeWorldRerandomCount, request.FallbackCarveCount,
                    request.SilentWideningCount, request.GeneratedFileWriteCount),
                "MUTATION|" + Numbers(request.TilemapMutationCount, request.SceneMutationCount,
                    request.PrefabMutationCount, request.GameObjectMutationCount, request.GameplaySpawnCount,
                    request.AuthoringMutationCount, request.WorldPlanMutationCount,
                    request.IntersectorPlanMutationCount, request.ReservationPlanMutationCount,
                    request.PacingDensityPlanMutationCount, request.RollbackPlanMutationCount),
                "CLAIMS|" + Numbers(request.ProductionSeedApprovalCount, request.FullRegressionCount,
                    request.NullBatchCaseLabelCount),
            };
            lines.AddRange(request.BatchCaseLabels.Select(value => "BATCH|" + Token(value)));
            return HashCanonicalText(string.Join("\n", lines));
        }

        internal static string ComputeOutput(
            string inputDigest,
            IEnumerable<WorldAssemblyOverlaySector> sectors,
            IEnumerable<WorldAssemblyOverlayEdge> edges,
            IEnumerable<WorldAssemblyOverlayLayer> layers,
            IEnumerable<WorldAssemblyHashRecord> hashes,
            WorldBatchPlanReport report)
        {
            var lines = new List<string> { "INPUT|" + Token(inputDigest) };
            lines.AddRange((sectors ?? Array.Empty<WorldAssemblyOverlaySector>())
                .OrderBy(value => value).Select(value => value.StableToken));
            lines.AddRange((edges ?? Array.Empty<WorldAssemblyOverlayEdge>())
                .OrderBy(value => value).Select(value => value.StableToken));
            foreach (var layer in (layers ?? Array.Empty<WorldAssemblyOverlayLayer>()).OrderBy(value => value))
            {
                lines.Add("LAYER|" + layer.Kind + "|" + Token(layer.UnavailableReason));
                lines.AddRange(layer.Tokens.Select(value => "LAYER_TOKEN|" + layer.Kind + "|" + Token(value)));
            }
            lines.AddRange((hashes ?? Array.Empty<WorldAssemblyHashRecord>()).OrderBy(value => value)
                .Select(value => value.StableToken));
            if (report != null)
            {
                lines.AddRange(report.Cases.Select(value => value.StableToken));
                lines.AddRange(report.SolverUpperBounds.Select(value => value.StableToken));
            }
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) builder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        internal static string Token(string value)
        {
            var builder = new StringBuilder();
            foreach (var character in value ?? string.Empty)
            {
                if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') || character == '_' || character == '-' ||
                    character == '.' || character == ':')
                {
                    builder.Append(character);
                }
                else
                {
                    builder.Append('_').Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                }
            }
            return builder.ToString();
        }

        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Numbers(params int[] values) =>
            string.Join("|", values.Select(Number));

        private static string Digest(WorldSolveOrderResult value, bool input) => value == null
            ? string.Empty
            : input ? value.InputDigest : value.OutputDigest;

        private static string Digest(WorldIntersectorEdgePlan value, bool input) => value == null
            ? string.Empty
            : input ? value.InputDigest : value.OutputDigest;

        private static string Digest(WorldMultiSectorReservationPlan value, bool input) => value == null
            ? string.Empty
            : input ? value.InputDigest : value.OutputDigest;

        private static string Digest(WorldPacingDensityPlan value, bool input) => value == null
            ? string.Empty
            : input ? value.InputDigest : value.OutputDigest;

        private static string Digest(WorldNeighborRollbackPlan value, bool input) => value == null
            ? string.Empty
            : input ? value.InputDigest : value.OutputDigest;
    }
}
