using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum FinalCanvasLayerKind
    {
        Terrain = 1,
        Affordance = 2,
        Material = 3,
        Hazard = 4,
        Marker = 5,
        Protection = 6,
        SourceOwner = 7,
    }

    public enum FinalCanvasCellKind
    {
        Unknown = 0,
        None = 1,
        Air = 2,
        Solid = 3,
        Ground = 4,
        Traversable = 5,
        Material = 6,
        Hazard = 7,
        Marker = 8,
        ProtectedOpen = 9,
        Owner = 10,
        Blocked = 11,
    }

    public enum FinalCanvasSourceOwner
    {
        Unknown = 0,
        Cleanup = 1,
        QuietFiller = 2,
        EventOverlay = 3,
        Activity = 4,
        MicroPattern = 5,
        TerrainCluster = 6,
        MandatoryRoute = 7,
        SpecialRegion = 8,
        Boundary = 9,
        FixedSlice = 10,
    }

    public enum FinalCanvasClaimPriority
    {
        Unknown = 0,
        Cleanup = 1,
        QuietFiller = 2,
        EventMarker = 3,
        ActivityMarker = 4,
        TerrainClusterPattern = 5,
        TerrainClusterSpine = 6,
        SpecialEntranceBuffer = 7,
        MandatoryRouteProtectedOpen = 8,
        BoundaryAperture = 9,
        SpecialFixedShell = 10,
        FixedSlice = 11,
    }

    public enum FinalCanvasProtectionKind
    {
        None = 0,
        FixedSlice = 1,
        SpecialFixedShell = 2,
        BoundaryAperture = 3,
        MandatoryRouteProtectedOpen = 4,
        SpecialEntranceBuffer = 5,
    }

    public enum FinalCanvasConflictKind
    {
        FixedSliceOverwrite = 1,
        BoundaryApertureOverwrite = 2,
        MandatoryRouteProtectedOpenBlocked = 3,
        SpecialEntranceBlocked = 4,
        ProtectionRemoval = 5,
        SamePriorityDifferentValue = 6,
    }

    public enum FinalCanvasLayerFailureCode
    {
        MissingRequest = 1,
        UpstreamExitNotApproved = 2,
        MissingUpstreamIdentity = 3,
        InvalidSectorIdentity = 4,
        InvalidDimensions = 5,
        InvalidCellCount = 6,
        InvalidCoordinate = 7,
        InvalidClaim = 8,
        MissingLayerCoverage = 9,
        ForbiddenOverwrite = 10,
        SameLayerConflict = 11,
        ForbiddenOperation = 12,
        InvalidDigest = 13,
    }

    public sealed class FinalCanvasCellCoordinate :
        IEquatable<FinalCanvasCellCoordinate>, IComparable<FinalCanvasCellCoordinate>
    {
        public FinalCanvasCellCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool IsInBounds => X >= 0 && X < SectorFinalCanvasLayerPlan.SectorWidth &&
                                  Y >= 0 && Y < SectorFinalCanvasLayerPlan.SectorHeight;
        public int RowMajorIndex => (Y * SectorFinalCanvasLayerPlan.SectorWidth) + X;

        public int CompareTo(FinalCanvasCellCoordinate other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            return comparison != 0 ? comparison : X.CompareTo(other.X);
        }

        public bool Equals(FinalCanvasCellCoordinate other) =>
            other != null && X == other.X && Y == other.Y;
        public override bool Equals(object obj) => Equals(obj as FinalCanvasCellCoordinate);
        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0},{1}", X, Y);
    }

    public sealed class FinalCanvasLayerClaim : IComparable<FinalCanvasLayerClaim>
    {
        public FinalCanvasLayerClaim(
            string claimId,
            FinalCanvasCellCoordinate coordinate,
            FinalCanvasLayerKind layer,
            FinalCanvasCellKind cellKind,
            FinalCanvasSourceOwner sourceOwner,
            FinalCanvasClaimPriority priority,
            FinalCanvasProtectionKind protection,
            bool isProtected,
            string provenanceId,
            string authorityReason,
            bool allowsExplicitMerge = false)
        {
            ClaimId = claimId ?? string.Empty;
            Coordinate = coordinate;
            Layer = layer;
            CellKind = cellKind;
            SourceOwner = sourceOwner;
            Priority = priority;
            Protection = protection;
            IsProtected = isProtected;
            ProvenanceId = provenanceId ?? string.Empty;
            AuthorityReason = authorityReason ?? string.Empty;
            AllowsExplicitMerge = allowsExplicitMerge;
        }

        public string ClaimId { get; }
        public FinalCanvasCellCoordinate Coordinate { get; }
        public FinalCanvasLayerKind Layer { get; }
        public FinalCanvasCellKind CellKind { get; }
        public FinalCanvasSourceOwner SourceOwner { get; }
        public FinalCanvasClaimPriority Priority { get; }
        public FinalCanvasProtectionKind Protection { get; }
        public bool IsProtected { get; }
        public string ProvenanceId { get; }
        public string AuthorityReason { get; }
        public bool AllowsExplicitMerge { get; }
        public string StableToken => string.Join("|", new[]
        {
            "CLAIM", Coordinate == null ? "MISSING" : Coordinate.ToString(),
            Layer.ToString().ToUpperInvariant(), Priority.ToString().ToUpperInvariant(),
            SourceOwner.ToString().ToUpperInvariant(), CellKind.ToString().ToUpperInvariant(),
            Protection.ToString().ToUpperInvariant(), IsProtected ? "1" : "0",
            ProvenanceId, ClaimId, AuthorityReason, AllowsExplicitMerge ? "1" : "0",
        });

        public int CompareTo(FinalCanvasLayerClaim other)
        {
            if (other == null) return -1;
            var comparison = CompareCoordinate(Coordinate, other.Coordinate);
            if (comparison != 0) return comparison;
            comparison = Layer.CompareTo(other.Layer);
            if (comparison != 0) return comparison;
            comparison = ((int)other.Priority).CompareTo((int)Priority);
            if (comparison != 0) return comparison;
            comparison = ((int)other.SourceOwner).CompareTo((int)SourceOwner);
            if (comparison != 0) return comparison;
            comparison = string.Compare(ProvenanceId, other.ProvenanceId, StringComparison.Ordinal);
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

    public sealed class FinalCanvasCell : IComparable<FinalCanvasCell>
    {
        private readonly ReadOnlyCollection<FinalCanvasLayerClaim> winners;

        internal FinalCanvasCell(
            FinalCanvasCellCoordinate coordinate,
            IEnumerable<FinalCanvasLayerClaim> sourceWinners)
        {
            Coordinate = coordinate;
            winners = new ReadOnlyCollection<FinalCanvasLayerClaim>((sourceWinners ??
                Array.Empty<FinalCanvasLayerClaim>()).OrderBy(value => value.Layer).ToArray());
            StableToken = string.Join("|", new[] { "CELL", Coordinate.ToString() }.Concat(
                winners.Select(value => value.StableToken)));
        }

        public FinalCanvasCellCoordinate Coordinate { get; }
        public int RowMajorIndex => Coordinate.RowMajorIndex;
        public IReadOnlyList<FinalCanvasLayerClaim> Winners => winners;
        public string StableToken { get; }
        public FinalCanvasLayerClaim Winner(FinalCanvasLayerKind layer) =>
            winners.Single(value => value.Layer == layer);
        public int CompareTo(FinalCanvasCell other) =>
            other == null ? -1 : Coordinate.CompareTo(other.Coordinate);
    }

    public sealed class FinalCanvasLayerSummary : IComparable<FinalCanvasLayerSummary>
    {
        public FinalCanvasLayerSummary(FinalCanvasLayerKind layer, int winnerCount)
        {
            Layer = layer;
            WinnerCount = winnerCount;
            StableToken = string.Join("|", new[]
            {
                "SUMMARY", Layer.ToString().ToUpperInvariant(),
                WinnerCount.ToString(CultureInfo.InvariantCulture),
            });
        }

        public FinalCanvasLayerKind Layer { get; }
        public int WinnerCount { get; }
        public string StableToken { get; }
        public int CompareTo(FinalCanvasLayerSummary other) =>
            other == null ? -1 : Layer.CompareTo(other.Layer);
    }

    public sealed class FinalCanvasConflict : IComparable<FinalCanvasConflict>
    {
        public FinalCanvasConflict(
            FinalCanvasConflictKind kind,
            FinalCanvasCellCoordinate coordinate,
            FinalCanvasLayerKind layer,
            FinalCanvasLayerClaim winner,
            FinalCanvasLayerClaim challenger,
            string reason)
        {
            Kind = kind;
            Coordinate = coordinate;
            Layer = layer;
            Winner = winner;
            Challenger = challenger;
            Reason = reason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "CONFLICT", Coordinate == null ? "MISSING" : Coordinate.ToString(),
                Layer.ToString().ToUpperInvariant(), Kind.ToString().ToUpperInvariant(),
                Winner == null ? "MISSING" : Winner.ClaimId,
                Challenger == null ? "MISSING" : Challenger.ClaimId, Reason,
            });
        }

        public FinalCanvasConflictKind Kind { get; }
        public FinalCanvasCellCoordinate Coordinate { get; }
        public FinalCanvasLayerKind Layer { get; }
        public FinalCanvasLayerClaim Winner { get; }
        public FinalCanvasLayerClaim Challenger { get; }
        public string Reason { get; }
        public string StableToken { get; }

        public int CompareTo(FinalCanvasConflict other)
        {
            if (other == null) return -1;
            var comparison = Coordinate == null
                ? (other.Coordinate == null ? 0 : 1)
                : (other.Coordinate == null ? -1 : Coordinate.CompareTo(other.Coordinate));
            if (comparison != 0) return comparison;
            comparison = Layer.CompareTo(other.Layer);
            if (comparison != 0) return comparison;
            comparison = CompareWinnerPriority(Winner, other.Winner);
            if (comparison != 0) return comparison;
            comparison = CompareWinnerSource(Winner, other.Winner);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            comparison = string.Compare(
                Winner == null ? string.Empty : Winner.ClaimId,
                other.Winner == null ? string.Empty : other.Winner.ClaimId,
                StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(
                    Challenger == null ? string.Empty : Challenger.ClaimId,
                    other.Challenger == null ? string.Empty : other.Challenger.ClaimId,
                    StringComparison.Ordinal);
        }

        private static int CompareWinnerPriority(
            FinalCanvasLayerClaim left,
            FinalCanvasLayerClaim right) => ((int)(right == null
            ? FinalCanvasClaimPriority.Unknown
            : right.Priority)).CompareTo((int)(left == null
            ? FinalCanvasClaimPriority.Unknown
            : left.Priority));

        private static int CompareWinnerSource(
            FinalCanvasLayerClaim left,
            FinalCanvasLayerClaim right) => ((int)(right == null
            ? FinalCanvasSourceOwner.Unknown
            : right.SourceOwner)).CompareTo((int)(left == null
            ? FinalCanvasSourceOwner.Unknown
            : left.SourceOwner));
    }

    public sealed class FinalCanvasLayerFailure :
        IComparable<FinalCanvasLayerFailure>, IEquatable<FinalCanvasLayerFailure>
    {
        public FinalCanvasLayerFailure(FinalCanvasLayerFailureCode code, string subject, string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public FinalCanvasLayerFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public int CompareTo(FinalCanvasLayerFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(FinalCanvasLayerFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as FinalCanvasLayerFailure);
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

    public sealed class FinalCanvasLayerRequest
    {
        private readonly ReadOnlyCollection<FinalCanvasLayerClaim> claims;

        public FinalCanvasLayerRequest(
            string sectorId,
            int width,
            int height,
            IEnumerable<FinalCanvasLayerClaim> sourceClaims,
            bool map15ExitApproved,
            string map15ExitDigest,
            string worldAssemblyDigest,
            string sectorOwnershipDigest,
            string boundaryAuthorityDigest,
            string fixedCanvasAuthorityDigest,
            string publicationLabel,
            int newRngDrawCount = 0,
            int sliceCreationCount = 0,
            int generatedFileWriteCount = 0,
            int tilemapMutationCount = 0,
            int sceneMutationCount = 0,
            int prefabMutationCount = 0,
            int gameObjectMutationCount = 0,
            int gameplaySpawnCount = 0,
            int productionSeedApprovalCount = 0,
            int sectorRerollCount = 0,
            int fallbackCarveCount = 0,
            int fullRegressionCount = 0)
        {
            SectorId = sectorId ?? string.Empty;
            Width = width;
            Height = height;
            var rawClaims = (sourceClaims ?? Array.Empty<FinalCanvasLayerClaim>()).ToArray();
            NullClaimCount = rawClaims.Count(value => value == null);
            claims = new ReadOnlyCollection<FinalCanvasLayerClaim>(rawClaims
                .Where(value => value != null).OrderBy(value => value).ToArray());
            Map15ExitApproved = map15ExitApproved;
            Map15ExitDigest = map15ExitDigest ?? string.Empty;
            WorldAssemblyDigest = worldAssemblyDigest ?? string.Empty;
            SectorOwnershipDigest = sectorOwnershipDigest ?? string.Empty;
            BoundaryAuthorityDigest = boundaryAuthorityDigest ?? string.Empty;
            FixedCanvasAuthorityDigest = fixedCanvasAuthorityDigest ?? string.Empty;
            PublicationLabel = publicationLabel ?? string.Empty;
            NewRngDrawCount = newRngDrawCount;
            SliceCreationCount = sliceCreationCount;
            GeneratedFileWriteCount = generatedFileWriteCount;
            TilemapMutationCount = tilemapMutationCount;
            SceneMutationCount = sceneMutationCount;
            PrefabMutationCount = prefabMutationCount;
            GameObjectMutationCount = gameObjectMutationCount;
            GameplaySpawnCount = gameplaySpawnCount;
            ProductionSeedApprovalCount = productionSeedApprovalCount;
            SectorRerollCount = sectorRerollCount;
            FallbackCarveCount = fallbackCarveCount;
            FullRegressionCount = fullRegressionCount;
            CanonicalDigest = FinalCanvasLayerDigest.ComputeInput(this);
        }

        public string SectorId { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<FinalCanvasLayerClaim> Claims => claims;
        public int NullClaimCount { get; }
        public bool Map15ExitApproved { get; }
        public string Map15ExitDigest { get; }
        public string WorldAssemblyDigest { get; }
        public string SectorOwnershipDigest { get; }
        public string BoundaryAuthorityDigest { get; }
        public string FixedCanvasAuthorityDigest { get; }
        public string PublicationLabel { get; }
        public int NewRngDrawCount { get; }
        public int SliceCreationCount { get; }
        public int GeneratedFileWriteCount { get; }
        public int TilemapMutationCount { get; }
        public int SceneMutationCount { get; }
        public int PrefabMutationCount { get; }
        public int GameObjectMutationCount { get; }
        public int GameplaySpawnCount { get; }
        public int ProductionSeedApprovalCount { get; }
        public int SectorRerollCount { get; }
        public int FallbackCarveCount { get; }
        public int FullRegressionCount { get; }
        public string CanonicalDigest { get; }
    }

    public sealed class SectorFinalCanvasLayerPlan
    {
        private readonly ReadOnlyCollection<FinalCanvasCell> cells;
        private readonly ReadOnlyCollection<FinalCanvasLayerSummary> summaries;
        private readonly ReadOnlyCollection<FinalCanvasConflict> conflicts;
        private readonly ReadOnlyDictionary<FinalCanvasSourceOwner, int> sourceOwnerCounts;
        private readonly ReadOnlyDictionary<FinalCanvasClaimPriority, int> priorityWinnerCounts;

        internal SectorFinalCanvasLayerPlan(
            FinalCanvasLayerRequest request,
            IEnumerable<FinalCanvasCell> sourceCells,
            IEnumerable<FinalCanvasLayerSummary> sourceSummaries,
            IEnumerable<FinalCanvasConflict> sourceConflicts,
            string outputDigest)
        {
            Request = request;
            cells = new ReadOnlyCollection<FinalCanvasCell>((sourceCells ??
                Array.Empty<FinalCanvasCell>()).OrderBy(value => value).ToArray());
            summaries = new ReadOnlyCollection<FinalCanvasLayerSummary>((sourceSummaries ??
                Array.Empty<FinalCanvasLayerSummary>()).OrderBy(value => value).ToArray());
            conflicts = new ReadOnlyCollection<FinalCanvasConflict>((sourceConflicts ??
                Array.Empty<FinalCanvasConflict>()).OrderBy(value => value).ToArray());
            var winners = cells.SelectMany(value => value.Winners).ToArray();
            sourceOwnerCounts = new ReadOnlyDictionary<FinalCanvasSourceOwner, int>(
                Enum.GetValues(typeof(FinalCanvasSourceOwner)).Cast<FinalCanvasSourceOwner>()
                    .ToDictionary(value => value, value => winners.Count(claim => claim.SourceOwner == value)));
            priorityWinnerCounts = new ReadOnlyDictionary<FinalCanvasClaimPriority, int>(
                Enum.GetValues(typeof(FinalCanvasClaimPriority)).Cast<FinalCanvasClaimPriority>()
                    .ToDictionary(value => value, value => winners.Count(claim => claim.Priority == value)));
            OutputDigest = outputDigest ?? string.Empty;
        }

        public const int SectorWidth = WorldGenConstants.SectorWidthTiles;
        public const int SectorHeight = WorldGenConstants.SectorHeightTiles;
        public const int CellCount = SectorWidth * SectorHeight;
        public const int RequiredLayerCount = 7;
        public const string PolicyVersion = "MAP16_01_FINAL_CANVAS_LAYER_POLICY_V1";
        public const string DownstreamOwner = "MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY";
        public const bool OpensDownstreamTask = false;

        public FinalCanvasLayerRequest Request { get; }
        public IReadOnlyList<FinalCanvasCell> Cells => cells;
        public IReadOnlyList<FinalCanvasLayerSummary> LayerSummaries => summaries;
        public IReadOnlyList<FinalCanvasConflict> Conflicts => conflicts;
        public IReadOnlyDictionary<FinalCanvasSourceOwner, int> SourceOwnerCounts => sourceOwnerCounts;
        public IReadOnlyDictionary<FinalCanvasClaimPriority, int> PriorityWinnerCounts => priorityWinnerCounts;
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int ObservedCellCount => cells.Count;
        public int UniqueCoordinateCount => cells.Select(value => value.Coordinate).Distinct().Count();
        public int OutOfBoundsCellCount => cells.Count(value => !value.Coordinate.IsInBounds);
        public int RequiredLayerKindCount => RequiredLayerCount;
        public int CoveredLayerKindCount => summaries.Count(value => value.WinnerCount > 0);
        public int MissingLayerKindCount => RequiredLayerKindCount - CoveredLayerKindCount;
        public int WinningClaimCount => cells.Sum(value => value.Winners.Count);
        public int WinningClaimsWithSourceOwnerCount => cells.SelectMany(value => value.Winners)
            .Count(value => value.SourceOwner != FinalCanvasSourceOwner.Unknown);
        public int WinningClaimsWithProvenanceCount => cells.SelectMany(value => value.Winners)
            .Count(value => !string.IsNullOrEmpty(value.ProvenanceId));
        public int ProtectedCellCount => cells.Count(value => value.Winners.Any(claim =>
            claim.IsProtected || claim.Protection != FinalCanvasProtectionKind.None));
        public int FixedCellCount => cells.Count(value => value.Winners.Any(claim =>
            claim.Priority == FinalCanvasClaimPriority.FixedSlice ||
            claim.Priority == FinalCanvasClaimPriority.SpecialFixedShell));
        public int BoundaryApertureCellCount => cells.Count(value => value.Winners.Any(claim =>
            claim.Priority == FinalCanvasClaimPriority.BoundaryAperture));
        public int MarkerCount => cells.SelectMany(value => value.Winners).Count(value =>
            value.Layer == FinalCanvasLayerKind.Marker && value.CellKind == FinalCanvasCellKind.Marker);
        public int ConflictCount => conflicts.Count;
        public int SilentOverwriteCount => 0;
        public int ProtectedOpenOverwriteViolationCount => conflicts.Count(value =>
            value.Kind == FinalCanvasConflictKind.MandatoryRouteProtectedOpenBlocked);
        public int SpecialEntranceBlockedViolationCount => conflicts.Count(value =>
            value.Kind == FinalCanvasConflictKind.SpecialEntranceBlocked);
        public int FixedPrecedenceWinCount => CountPrecedenceWins(FinalCanvasClaimPriority.FixedSlice);
        public int BoundaryPrecedenceWinCount => CountPrecedenceWins(FinalCanvasClaimPriority.BoundaryAperture);
        public int NewRngDrawCount => Request.NewRngDrawCount;
        public int SliceCreationCount => Request.SliceCreationCount;
        public int GeneratedFileWriteCount => Request.GeneratedFileWriteCount;
        public int TilemapMutationCount => Request.TilemapMutationCount;
        public int SceneMutationCount => Request.SceneMutationCount;
        public int PrefabMutationCount => Request.PrefabMutationCount;
        public int GameObjectMutationCount => Request.GameObjectMutationCount;
        public int GameplaySpawnCount => Request.GameplaySpawnCount;
        public int ProductionSeedApprovalCount => Request.ProductionSeedApprovalCount;
        public int SectorRerollCount => Request.SectorRerollCount;
        public int FallbackCarveCount => Request.FallbackCarveCount;
        public int FullRegressionCount => Request.FullRegressionCount;
        public int CountWinners(FinalCanvasSourceOwner owner) => sourceOwnerCounts[owner];
        public int CountWinners(FinalCanvasClaimPriority priority) => priorityWinnerCounts[priority];

        private int CountPrecedenceWins(FinalCanvasClaimPriority priority)
        {
            return cells.SelectMany(cell => cell.Winners.Select(winner => new { cell, winner }))
                .Count(pair => pair.winner.Priority == priority && Request.Claims.Any(claim =>
                    claim.Coordinate != null && claim.Coordinate.Equals(pair.cell.Coordinate) &&
                    claim.Layer == pair.winner.Layer && claim.Priority < pair.winner.Priority));
        }
    }

    public sealed class FinalCanvasLayerResult
    {
        private readonly ReadOnlyCollection<FinalCanvasLayerFailure> failures;
        private readonly ReadOnlyCollection<FinalCanvasConflict> conflicts;

        internal FinalCanvasLayerResult(
            FinalCanvasLayerRequest request,
            SectorFinalCanvasLayerPlan plan,
            IEnumerable<FinalCanvasLayerFailure> sourceFailures,
            IEnumerable<FinalCanvasConflict> sourceConflicts)
        {
            Request = request;
            Plan = plan;
            failures = new ReadOnlyCollection<FinalCanvasLayerFailure>((sourceFailures ??
                Array.Empty<FinalCanvasLayerFailure>()).OrderBy(value => value).ToArray());
            conflicts = new ReadOnlyCollection<FinalCanvasConflict>((sourceConflicts ??
                Array.Empty<FinalCanvasConflict>()).OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public FinalCanvasLayerRequest Request { get; }
        public SectorFinalCanvasLayerPlan Plan { get; }
        public IReadOnlyList<FinalCanvasLayerFailure> Failures => failures;
        public IReadOnlyList<FinalCanvasConflict> Conflicts => conflicts;
        public string InputDigest => Plan == null ? string.Empty : Plan.InputDigest;
        public string OutputDigest => Plan == null ? string.Empty : Plan.OutputDigest;
    }

    public static class FinalCanvasLayerDigest
    {
        public static string ComputeInput(FinalCanvasLayerRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + SectorFinalCanvasLayerPlan.PolicyVersion,
                "SECTOR|" + request.SectorId,
                "DIMENSIONS|" + Number(request.Width) + "|" + Number(request.Height),
                "MAP15_EXIT_APPROVED|" + (request.Map15ExitApproved ? "1" : "0"),
                "MAP15_EXIT|" + request.Map15ExitDigest,
                "WORLD_ASSEMBLY|" + request.WorldAssemblyDigest,
                "SECTOR_OWNERSHIP|" + request.SectorOwnershipDigest,
                "BOUNDARY_AUTHORITY|" + request.BoundaryAuthorityDigest,
                "FIXED_CANVAS_AUTHORITY|" + request.FixedCanvasAuthorityDigest,
                "PUBLICATION|" + request.PublicationLabel,
                "COUNTERS|" + string.Join("|", new[]
                {
                    Number(request.NewRngDrawCount), Number(request.SliceCreationCount),
                    Number(request.GeneratedFileWriteCount), Number(request.TilemapMutationCount),
                    Number(request.SceneMutationCount), Number(request.PrefabMutationCount),
                    Number(request.GameObjectMutationCount), Number(request.GameplaySpawnCount),
                    Number(request.ProductionSeedApprovalCount), Number(request.SectorRerollCount),
                    Number(request.FallbackCarveCount), Number(request.FullRegressionCount),
                }),
            };
            lines.AddRange(request.Claims.OrderBy(value => value).Select(value => value.StableToken));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(SectorFinalCanvasLayerPlan plan) => plan == null
            ? string.Empty
            : ComputeOutput(plan.Request, plan.Cells, plan.LayerSummaries, plan.Conflicts);

        internal static string ComputeOutput(
            FinalCanvasLayerRequest request,
            IEnumerable<FinalCanvasCell> cells,
            IEnumerable<FinalCanvasLayerSummary> summaries,
            IEnumerable<FinalCanvasConflict> conflicts)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "INPUT|" + request.CanonicalDigest,
                "DOWNSTREAM|" + SectorFinalCanvasLayerPlan.DownstreamOwner,
                "OPENS_DOWNSTREAM|0",
            };
            lines.AddRange((cells ?? Array.Empty<FinalCanvasCell>()).OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange((summaries ?? Array.Empty<FinalCanvasLayerSummary>()).OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.AddRange((conflicts ?? Array.Empty<FinalCanvasConflict>()).OrderBy(value => value)
                .Select(value => value.StableToken));
            lines.Add("MUTATIONS|" + string.Join("|", new[]
            {
                Number(request.NewRngDrawCount), Number(request.SliceCreationCount),
                Number(request.GeneratedFileWriteCount), Number(request.TilemapMutationCount),
                Number(request.SceneMutationCount), Number(request.PrefabMutationCount),
                Number(request.GameObjectMutationCount), Number(request.GameplaySpawnCount),
                Number(request.ProductionSeedApprovalCount), Number(request.SectorRerollCount),
                Number(request.FallbackCarveCount), Number(request.FullRegressionCount),
            }));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string text) =>
            BakingCanonicalDigest.HashCanonicalText(text ?? string.Empty);

        public static bool IsLowerHexSha256(string value) =>
            BakingCanonicalDigest.IsLowerHexSha256(value);

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
