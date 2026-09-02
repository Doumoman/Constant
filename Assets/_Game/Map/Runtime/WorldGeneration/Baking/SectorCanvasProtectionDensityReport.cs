using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum ProtectionIntrusionKind
    {
        ProtectedOpenSolidIntrusion = 1,
        ProtectedOpenHazardIntrusion = 2,
        BoundaryApertureBlocked = 3,
        FixedSliceOverwritten = 4,
        SpecialEntranceBlocked = 5,
        ProtectionLayerMissing = 6,
    }

    public enum CleanupCandidateKind
    {
        SingleCellSolidNoise = 1,
        SingleCellAirNoise = 2,
        HeadSnag = 3,
        ShallowPit = 4,
        OneCellLip = 5,
        UnownedAirPocket = 6,
    }

    public enum CleanupProjectionState
    {
        NoChange = 1,
        ProposedSafe = 2,
        RejectedProtected = 3,
    }

    public enum DensityBudgetKind
    {
        SolidDensity = 1,
        ReachableDensity = 2,
        UnownedAirMaxBox = 3,
        ProtectionIntrusion = 4,
        CleanupProjectionSafety = 5,
    }

    public enum DensityBudgetVerdict
    {
        Pass = 1,
        Fail = 2,
    }

    public enum UnownedAirRegionKind
    {
        Bounded = 1,
        Oversized = 2,
    }

    public enum ProtectionDensityFailureCode
    {
        MissingPlan = 1,
        InvalidCanvas = 2,
        MissingLayerData = 3,
        MissingSourceEvidence = 4,
        ProtectionIntrusion = 5,
        DensityOutOfRange = 6,
        UnownedAirTooLarge = 7,
        UnsafeCleanupProjection = 8,
        InvalidDigest = 9,
        ForbiddenOperation = 10,
    }

    public sealed class SectorCanvasProtectionIntrusion :
        IComparable<SectorCanvasProtectionIntrusion>
    {
        public SectorCanvasProtectionIntrusion(
            ProtectionIntrusionKind kind,
            FinalCanvasCellCoordinate coordinate,
            FinalCanvasLayerKind layer,
            FinalCanvasSourceOwner sourceOwner,
            string claimId,
            string reason)
        {
            Kind = kind;
            Coordinate = coordinate;
            Layer = layer;
            SourceOwner = sourceOwner;
            ClaimId = claimId ?? string.Empty;
            Reason = reason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "INTRUSION", Coordinate == null ? "MISSING" : Coordinate.ToString(),
                Kind.ToString().ToUpperInvariant(), Layer.ToString().ToUpperInvariant(),
                SourceOwner.ToString().ToUpperInvariant(), ClaimId, Reason,
            });
        }

        public ProtectionIntrusionKind Kind { get; }
        public FinalCanvasCellCoordinate Coordinate { get; }
        public FinalCanvasLayerKind Layer { get; }
        public FinalCanvasSourceOwner SourceOwner { get; }
        public string ClaimId { get; }
        public string Reason { get; }
        public string StableToken { get; }

        public int CompareTo(SectorCanvasProtectionIntrusion other)
        {
            if (other == null) return -1;
            var comparison = CompareCoordinate(Coordinate, other.Coordinate);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = SourceOwner.CompareTo(other.SourceOwner);
            return comparison != 0
                ? comparison
                : string.Compare(ClaimId, other.ClaimId, StringComparison.Ordinal);
        }

        private static int CompareCoordinate(
            FinalCanvasCellCoordinate left,
            FinalCanvasCellCoordinate right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return left.CompareTo(right);
        }
    }

    public sealed class SectorCanvasCleanupCandidate : IComparable<SectorCanvasCleanupCandidate>
    {
        public SectorCanvasCleanupCandidate(
            CleanupCandidateKind kind,
            FinalCanvasCellCoordinate coordinate,
            FinalCanvasCellKind currentCellKind,
            FinalCanvasCellKind projectedCellKind,
            FinalCanvasSourceOwner sourceOwner,
            string claimId,
            string reason)
        {
            Kind = kind;
            Coordinate = coordinate;
            CurrentCellKind = currentCellKind;
            ProjectedCellKind = projectedCellKind;
            SourceOwner = sourceOwner;
            ClaimId = claimId ?? string.Empty;
            Reason = reason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "CLEANUP", Coordinate == null ? "MISSING" : Coordinate.ToString(),
                Kind.ToString().ToUpperInvariant(), SourceOwner.ToString().ToUpperInvariant(),
                ClaimId, CurrentCellKind.ToString().ToUpperInvariant(),
                ProjectedCellKind.ToString().ToUpperInvariant(), Reason,
            });
        }

        public CleanupCandidateKind Kind { get; }
        public FinalCanvasCellCoordinate Coordinate { get; }
        public FinalCanvasCellKind CurrentCellKind { get; }
        public FinalCanvasCellKind ProjectedCellKind { get; }
        public FinalCanvasSourceOwner SourceOwner { get; }
        public string ClaimId { get; }
        public string Reason { get; }
        public string StableToken { get; }

        public int CompareTo(SectorCanvasCleanupCandidate other)
        {
            if (other == null) return -1;
            var comparison = Coordinate.CompareTo(other.Coordinate);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = SourceOwner.CompareTo(other.SourceOwner);
            return comparison != 0
                ? comparison
                : string.Compare(ClaimId, other.ClaimId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorCanvasCleanupProjection
    {
        private readonly ReadOnlyCollection<FinalCanvasCellCoordinate> changedCoordinates;

        public SectorCanvasCleanupProjection(
            IEnumerable<FinalCanvasCellCoordinate> sourceChangedCoordinates,
            int protectedOpenChangedCount,
            int fixedChangedCount,
            int boundaryChangedCount,
            int specialEntranceChangedCount,
            int rejectedProtectedCandidateCount)
        {
            changedCoordinates = new ReadOnlyCollection<FinalCanvasCellCoordinate>(
                (sourceChangedCoordinates ?? Array.Empty<FinalCanvasCellCoordinate>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray());
            ProtectedOpenChangedCount = protectedOpenChangedCount;
            FixedChangedCount = fixedChangedCount;
            BoundaryChangedCount = boundaryChangedCount;
            SpecialEntranceChangedCount = specialEntranceChangedCount;
            RejectedProtectedCandidateCount = rejectedProtectedCandidateCount;
            State = ProtectedAuthorityChangedCount > 0
                ? CleanupProjectionState.RejectedProtected
                : (changedCoordinates.Count == 0
                    ? CleanupProjectionState.NoChange
                    : CleanupProjectionState.ProposedSafe);
            StableToken = string.Join("|", new[]
            {
                "PROJECTION", State.ToString().ToUpperInvariant(),
                ChangedCellCount.ToString(CultureInfo.InvariantCulture),
                ProtectedOpenChangedCount.ToString(CultureInfo.InvariantCulture),
                FixedChangedCount.ToString(CultureInfo.InvariantCulture),
                BoundaryChangedCount.ToString(CultureInfo.InvariantCulture),
                SpecialEntranceChangedCount.ToString(CultureInfo.InvariantCulture),
                RejectedProtectedCandidateCount.ToString(CultureInfo.InvariantCulture),
                string.Join(";", changedCoordinates.Select(value => value.ToString())),
            });
        }

        public CleanupProjectionState State { get; }
        public IReadOnlyList<FinalCanvasCellCoordinate> ChangedCoordinates => changedCoordinates;
        public int ChangedCellCount => changedCoordinates.Count;
        public int ProtectedOpenChangedCount { get; }
        public int FixedChangedCount { get; }
        public int BoundaryChangedCount { get; }
        public int SpecialEntranceChangedCount { get; }
        public int RejectedProtectedCandidateCount { get; }
        public int ProtectedAuthorityChangedCount => ProtectedOpenChangedCount + FixedChangedCount +
                                                     BoundaryChangedCount + SpecialEntranceChangedCount;
        public bool IsSafe => ProtectedAuthorityChangedCount == 0;
        public string StableToken { get; }
    }

    public sealed class SectorCanvasDensityBudget : IComparable<SectorCanvasDensityBudget>
    {
        public SectorCanvasDensityBudget(
            DensityBudgetKind kind,
            int observed,
            int minimum,
            int maximum,
            bool dimensionsWithinLimit = true)
        {
            Kind = kind;
            Observed = observed;
            Minimum = minimum;
            Maximum = maximum;
            Verdict = observed >= minimum && observed <= maximum && dimensionsWithinLimit
                ? DensityBudgetVerdict.Pass
                : DensityBudgetVerdict.Fail;
            StableToken = string.Join("|", new[]
            {
                "BUDGET", Kind.ToString().ToUpperInvariant(),
                Observed.ToString(CultureInfo.InvariantCulture),
                Minimum.ToString(CultureInfo.InvariantCulture),
                Maximum.ToString(CultureInfo.InvariantCulture),
                Verdict.ToString().ToUpperInvariant(),
            });
        }

        public DensityBudgetKind Kind { get; }
        public int Observed { get; }
        public int Minimum { get; }
        public int Maximum { get; }
        public DensityBudgetVerdict Verdict { get; }
        public string StableToken { get; }
        public int CompareTo(SectorCanvasDensityBudget other) =>
            other == null ? -1 : Kind.CompareTo(other.Kind);
    }

    public sealed class SectorCanvasUnownedAirRegion :
        IComparable<SectorCanvasUnownedAirRegion>
    {
        public SectorCanvasUnownedAirRegion(
            FinalCanvasCellCoordinate minimumCoordinate,
            FinalCanvasCellCoordinate maximumCoordinate,
            int area,
            UnownedAirRegionKind kind)
        {
            MinimumCoordinate = minimumCoordinate;
            MaximumCoordinate = maximumCoordinate;
            Area = area;
            Width = maximumCoordinate == null || minimumCoordinate == null
                ? 0
                : maximumCoordinate.X - minimumCoordinate.X + 1;
            Height = maximumCoordinate == null || minimumCoordinate == null
                ? 0
                : maximumCoordinate.Y - minimumCoordinate.Y + 1;
            Kind = kind;
            StableToken = string.Join("|", new[]
            {
                "UNOWNED_AIR", MinimumCoordinate == null ? "MISSING" : MinimumCoordinate.ToString(),
                MaximumCoordinate == null ? "MISSING" : MaximumCoordinate.ToString(),
                Width.ToString(CultureInfo.InvariantCulture),
                Height.ToString(CultureInfo.InvariantCulture),
                Area.ToString(CultureInfo.InvariantCulture), Kind.ToString().ToUpperInvariant(),
            });
        }

        public FinalCanvasCellCoordinate MinimumCoordinate { get; }
        public FinalCanvasCellCoordinate MaximumCoordinate { get; }
        public int Width { get; }
        public int Height { get; }
        public int Area { get; }
        public UnownedAirRegionKind Kind { get; }
        public string StableToken { get; }

        public int CompareTo(SectorCanvasUnownedAirRegion other)
        {
            if (other == null) return -1;
            var comparison = MinimumCoordinate.CompareTo(other.MinimumCoordinate);
            if (comparison != 0) return comparison;
            comparison = Width.CompareTo(other.Width);
            if (comparison != 0) return comparison;
            comparison = Height.CompareTo(other.Height);
            return comparison != 0 ? comparison : Area.CompareTo(other.Area);
        }
    }

    public sealed class ProtectionDensityFailure :
        IComparable<ProtectionDensityFailure>, IEquatable<ProtectionDensityFailure>
    {
        public ProtectionDensityFailure(
            ProtectionDensityFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public ProtectionDensityFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public int CompareTo(ProtectionDensityFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(ProtectionDensityFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as ProtectionDensityFailure);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Subject);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Reason);
            }
        }
        public override string ToString() => Code + ":" + Subject + ":" + Reason;
    }

    public sealed class SectorCanvasProtectionDensityReport
    {
        private readonly ReadOnlyCollection<SectorCanvasProtectionIntrusion> intrusions;
        private readonly ReadOnlyCollection<SectorCanvasCleanupCandidate> cleanupCandidates;
        private readonly ReadOnlyCollection<SectorCanvasDensityBudget> budgets;
        private readonly ReadOnlyCollection<SectorCanvasUnownedAirRegion> unownedAirRegions;
        private readonly ReadOnlyDictionary<CleanupCandidateKind, int> cleanupCounts;

        internal SectorCanvasProtectionDensityReport(
            SectorFinalCanvasLayerPlan sourcePlan,
            int protectedOpenCellCount,
            int fixedCellCount,
            int boundaryApertureCellCount,
            int specialEntranceCellCount,
            int solidCellCount,
            int reachableCellCount,
            IEnumerable<SectorCanvasProtectionIntrusion> sourceIntrusions,
            IEnumerable<SectorCanvasCleanupCandidate> sourceCleanupCandidates,
            SectorCanvasCleanupProjection cleanupProjection,
            IEnumerable<SectorCanvasDensityBudget> sourceBudgets,
            IEnumerable<SectorCanvasUnownedAirRegion> sourceUnownedAirRegions,
            string outputDigest)
        {
            SourcePlan = sourcePlan;
            ProtectedOpenCellCount = protectedOpenCellCount;
            FixedCellCount = fixedCellCount;
            BoundaryApertureCellCount = boundaryApertureCellCount;
            SpecialEntranceCellCount = specialEntranceCellCount;
            SolidCellCount = solidCellCount;
            ReachableCellCount = reachableCellCount;
            intrusions = new ReadOnlyCollection<SectorCanvasProtectionIntrusion>(
                (sourceIntrusions ?? Array.Empty<SectorCanvasProtectionIntrusion>())
                .OrderBy(value => value).ToArray());
            cleanupCandidates = new ReadOnlyCollection<SectorCanvasCleanupCandidate>(
                (sourceCleanupCandidates ?? Array.Empty<SectorCanvasCleanupCandidate>())
                .OrderBy(value => value).ToArray());
            CleanupProjection = cleanupProjection;
            budgets = new ReadOnlyCollection<SectorCanvasDensityBudget>(
                (sourceBudgets ?? Array.Empty<SectorCanvasDensityBudget>())
                .OrderBy(value => value).ToArray());
            unownedAirRegions = new ReadOnlyCollection<SectorCanvasUnownedAirRegion>(
                (sourceUnownedAirRegions ?? Array.Empty<SectorCanvasUnownedAirRegion>())
                .OrderBy(value => value).ToArray());
            cleanupCounts = new ReadOnlyDictionary<CleanupCandidateKind, int>(
                Enum.GetValues(typeof(CleanupCandidateKind)).Cast<CleanupCandidateKind>()
                    .ToDictionary(kind => kind, kind => cleanupCandidates.Count(value => value.Kind == kind)));
            OutputDigest = outputDigest ?? string.Empty;
        }

        public const int SectorWidth = SectorFinalCanvasLayerPlan.SectorWidth;
        public const int SectorHeight = SectorFinalCanvasLayerPlan.SectorHeight;
        public const int CellCount = SectorFinalCanvasLayerPlan.CellCount;
        public const int RequiredLayerCount = SectorFinalCanvasLayerPlan.RequiredLayerCount;
        public const int SolidMinimumPermille = 400;
        public const int SolidMaximumPermille = 650;
        public const int ReachableMinimumPermille = 350;
        public const int ReachableMaximumPermille = 550;
        public const int UnownedAirMaximumWidth = 8;
        public const int UnownedAirMaximumHeight = 6;
        public const int UnownedAirMaximumArea = 48;
        public const string PolicyVersion = "MAP16_02_PROTECTION_CLEANUP_DENSITY_POLICY_V1";
        public const string DownstreamOwner = "MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY";
        public const bool OpensDownstreamTask = false;

        public SectorFinalCanvasLayerPlan SourcePlan { get; }
        public string SectorId => SourcePlan.Request.SectorId;
        public string SourceInputDigest => SourcePlan.InputDigest;
        public string SourceOutputDigest => SourcePlan.OutputDigest;
        public string InputDigest => ProtectionDensityDigest.ComputeInput(SourcePlan);
        public string OutputDigest { get; }
        public int ObservedCellCount => SourcePlan.ObservedCellCount;
        public int UniqueCoordinateCount => SourcePlan.UniqueCoordinateCount;
        public int OutOfBoundsCellCount => SourcePlan.OutOfBoundsCellCount;
        public int RequiredLayerKindCount => SourcePlan.RequiredLayerKindCount;
        public int CoveredLayerKindCount => SourcePlan.CoveredLayerKindCount;
        public int MissingLayerKindCount => SourcePlan.MissingLayerKindCount;
        public int ProtectedCellCount => SourcePlan.ProtectedCellCount;
        public int ProtectedOpenCellCount { get; }
        public int FixedCellCount { get; }
        public int BoundaryApertureCellCount { get; }
        public int SpecialEntranceCellCount { get; }
        public IReadOnlyList<SectorCanvasProtectionIntrusion> Intrusions => intrusions;
        public int ProtectionIntrusionCount => intrusions.Count;
        public IReadOnlyList<SectorCanvasCleanupCandidate> CleanupCandidates => cleanupCandidates;
        public IReadOnlyDictionary<CleanupCandidateKind, int> CleanupCandidateCounts => cleanupCounts;
        public int CleanupCandidateCount => cleanupCandidates.Count;
        public int RequiredCleanupCandidateKindCount =>
            Enum.GetValues(typeof(CleanupCandidateKind)).Length;
        public int CoveredCleanupCandidateKindCount => cleanupCounts.Count(pair => pair.Value > 0);
        public int MissingCleanupCandidateKindCount =>
            RequiredCleanupCandidateKindCount - CoveredCleanupCandidateKindCount;
        public SectorCanvasCleanupProjection CleanupProjection { get; }
        public int SolidCellCount { get; }
        public int SolidPermille => Permille(SolidCellCount);
        public int ReachableCellCount { get; }
        public int ReachablePermille => Permille(ReachableCellCount);
        public IReadOnlyList<SectorCanvasDensityBudget> Budgets => budgets;
        public int DensityBudgetViolationCount => budgets.Count(value =>
            value.Verdict == DensityBudgetVerdict.Fail);
        public IReadOnlyList<SectorCanvasUnownedAirRegion> UnownedAirRegions => unownedAirRegions;
        public int LargestUnownedAirWidth => unownedAirRegions.Count == 0
            ? 0
            : unownedAirRegions.Max(value => value.Width);
        public int LargestUnownedAirHeight => unownedAirRegions.Count == 0
            ? 0
            : unownedAirRegions.Max(value => value.Height);
        public int LargestUnownedAirArea => unownedAirRegions.Count == 0
            ? 0
            : unownedAirRegions.Max(value => value.Area);
        public int UnownedAirViolationCount => unownedAirRegions.Count(value =>
            value.Kind == UnownedAirRegionKind.Oversized);
        public int NewRngDrawCount => SourcePlan.NewRngDrawCount;
        public int SliceCreationCount => SourcePlan.SliceCreationCount;
        public int GeneratedFileWriteCount => SourcePlan.GeneratedFileWriteCount;
        public int TilemapMutationCount => SourcePlan.TilemapMutationCount;
        public int SceneMutationCount => SourcePlan.SceneMutationCount;
        public int PrefabMutationCount => SourcePlan.PrefabMutationCount;
        public int GameObjectMutationCount => SourcePlan.GameObjectMutationCount;
        public int GameplaySpawnCount => SourcePlan.GameplaySpawnCount;
        public int ProductionSeedApprovalCount => SourcePlan.ProductionSeedApprovalCount;
        public int SectorRerollCount => SourcePlan.SectorRerollCount;
        public int FallbackCarveCount => SourcePlan.FallbackCarveCount;
        public int FullRegressionCount => SourcePlan.FullRegressionCount;
        public int CountCandidates(CleanupCandidateKind kind) => cleanupCounts[kind];
        public int CountIntrusions(ProtectionIntrusionKind kind) =>
            intrusions.Count(value => value.Kind == kind);

        private static int Permille(int count) =>
            (count * 1000) / SectorFinalCanvasLayerPlan.CellCount;
    }

    public sealed class ProtectionDensityResult
    {
        private readonly ReadOnlyCollection<ProtectionDensityFailure> failures;
        private readonly ReadOnlyCollection<SectorCanvasProtectionIntrusion> intrusions;

        internal ProtectionDensityResult(
            SectorFinalCanvasLayerPlan sourcePlan,
            SectorCanvasProtectionDensityReport report,
            IEnumerable<ProtectionDensityFailure> sourceFailures,
            IEnumerable<SectorCanvasProtectionIntrusion> sourceIntrusions)
        {
            SourcePlan = sourcePlan;
            Report = report;
            failures = new ReadOnlyCollection<ProtectionDensityFailure>(
                (sourceFailures ?? Array.Empty<ProtectionDensityFailure>())
                .OrderBy(value => value).ToArray());
            intrusions = new ReadOnlyCollection<SectorCanvasProtectionIntrusion>(
                (sourceIntrusions ?? Array.Empty<SectorCanvasProtectionIntrusion>())
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Report != null && failures.Count == 0;
        public SectorFinalCanvasLayerPlan SourcePlan { get; }
        public SectorCanvasProtectionDensityReport Report { get; }
        public IReadOnlyList<ProtectionDensityFailure> Failures => failures;
        public IReadOnlyList<SectorCanvasProtectionIntrusion> Intrusions => intrusions;
        public string InputDigest => Report == null ? string.Empty : Report.InputDigest;
        public string OutputDigest => Report == null ? string.Empty : Report.OutputDigest;
    }

    public static class ProtectionDensityDigest
    {
        public static string ComputeInput(SectorFinalCanvasLayerPlan plan)
        {
            if (plan == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + SectorCanvasProtectionDensityReport.PolicyVersion,
                "SECTOR|" + plan.Request.SectorId,
                "DIMENSIONS|" + Number(SectorCanvasProtectionDensityReport.SectorWidth) + "|" +
                Number(SectorCanvasProtectionDensityReport.SectorHeight),
                "SOURCE_INPUT|" + plan.InputDigest,
                "SOURCE_OUTPUT|" + plan.OutputDigest,
            };
            lines.AddRange(plan.Cells.OrderBy(value => value)
                .Select(value => value.StableToken));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(SectorCanvasProtectionDensityReport report)
        {
            if (report == null) return string.Empty;
            return ComputeOutput(
                report.SourcePlan,
                report.ProtectedOpenCellCount,
                report.FixedCellCount,
                report.BoundaryApertureCellCount,
                report.SpecialEntranceCellCount,
                report.SolidCellCount,
                report.ReachableCellCount,
                report.Intrusions,
                report.CleanupCandidates,
                report.CleanupProjection,
                report.Budgets,
                report.UnownedAirRegions);
        }

        internal static string ComputeOutput(
            SectorFinalCanvasLayerPlan plan,
            int protectedOpenCellCount,
            int fixedCellCount,
            int boundaryApertureCellCount,
            int specialEntranceCellCount,
            int solidCellCount,
            int reachableCellCount,
            IEnumerable<SectorCanvasProtectionIntrusion> intrusions,
            IEnumerable<SectorCanvasCleanupCandidate> cleanupCandidates,
            SectorCanvasCleanupProjection projection,
            IEnumerable<SectorCanvasDensityBudget> budgets,
            IEnumerable<SectorCanvasUnownedAirRegion> regions)
        {
            if (plan == null || projection == null) return string.Empty;
            var lines = new List<string>
            {
                "INPUT|" + ComputeInput(plan),
                "COUNTS|" + string.Join("|", new[]
                {
                    Number(protectedOpenCellCount), Number(fixedCellCount),
                    Number(boundaryApertureCellCount), Number(specialEntranceCellCount),
                    Number(solidCellCount), Number(reachableCellCount),
                }),
            };
            lines.AddRange((intrusions ?? Array.Empty<SectorCanvasProtectionIntrusion>())
                .OrderBy(value => value).Select(value => value.StableToken));
            lines.AddRange((cleanupCandidates ?? Array.Empty<SectorCanvasCleanupCandidate>())
                .OrderBy(value => value).Select(value => value.StableToken));
            lines.Add(projection.StableToken);
            lines.AddRange((budgets ?? Array.Empty<SectorCanvasDensityBudget>())
                .OrderBy(value => value).Select(value => value.StableToken));
            lines.AddRange((regions ?? Array.Empty<SectorCanvasUnownedAirRegion>())
                .OrderBy(value => value).Select(value => value.StableToken));
            lines.Add("MUTATIONS|" + string.Join("|", new[]
            {
                Number(plan.NewRngDrawCount), Number(plan.SliceCreationCount),
                Number(plan.GeneratedFileWriteCount), Number(plan.TilemapMutationCount),
                Number(plan.SceneMutationCount), Number(plan.PrefabMutationCount),
                Number(plan.GameObjectMutationCount), Number(plan.GameplaySpawnCount),
                Number(plan.ProductionSeedApprovalCount), Number(plan.SectorRerollCount),
                Number(plan.FallbackCarveCount), Number(plan.FullRegressionCount),
            }));
            lines.Add("DOWNSTREAM|" + SectorCanvasProtectionDensityReport.DownstreamOwner);
            lines.Add("OPENS_DOWNSTREAM|0");
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string text)
        {
            var canonical = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(canonical))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        public static bool IsLowerHexSha256(string value) => value != null && value.Length == 64 &&
            value.All(character => (character >= '0' && character <= '9') ||
                                   (character >= 'a' && character <= 'f'));

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
