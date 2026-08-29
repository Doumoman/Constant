using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public enum SpecialRegionPlacementOwnerKind
    {
        ActivityStructure = 100,
        TerrainCluster = 200,
        RareRegion = 300,
        Village = 400,
        CoreResource = 500,
        Forge = 600,
        Boss = 700,
    }

    public sealed class SpecialRegionOccupancyClaim
    {
        private readonly ReadOnlyCollection<SpecialRegionTileCoordinate> cells;

        public SpecialRegionOccupancyClaim(
            string ownerId,
            SpecialRegionPlacementOwnerKind ownerKind,
            IEnumerable<SpecialRegionTileCoordinate> cells,
            bool isHardProtected = false,
            bool isCommitted = false)
        {
            OwnerId = ownerId ?? string.Empty;
            OwnerKind = ownerKind;
            this.cells = new ReadOnlyCollection<SpecialRegionTileCoordinate>(
                (cells ?? Array.Empty<SpecialRegionTileCoordinate>()).OrderBy(value => value).ToArray());
            IsHardProtected = isHardProtected;
            IsCommitted = isCommitted;
        }

        public string OwnerId { get; }
        public SpecialRegionPlacementOwnerKind OwnerKind { get; }
        public int Priority => SpecialRegionPlacementCollisionCompiler.GetPriority(OwnerKind);
        public IReadOnlyList<SpecialRegionTileCoordinate> Cells => cells;
        public bool IsHardProtected { get; }
        public bool IsCommitted { get; }
    }

    public enum SpecialRegionCollisionKind
    {
        NoOverlap = 1,
        HigherPriorityWins = 2,
        RequiresReplan = 3,
    }

    public sealed class SpecialRegionCollisionDecision :
        IEquatable<SpecialRegionCollisionDecision>, IComparable<SpecialRegionCollisionDecision>
    {
        private readonly ReadOnlyCollection<SpecialRegionTileCoordinate> overlap;

        internal SpecialRegionCollisionDecision(
            SpecialRegionCollisionKind kind,
            SpecialRegionOccupancyClaim left,
            SpecialRegionOccupancyClaim right,
            SpecialRegionOccupancyClaim winner,
            SpecialRegionOccupancyClaim loser,
            IEnumerable<SpecialRegionTileCoordinate> overlap)
        {
            Kind = kind;
            LeftOwnerId = left.OwnerId;
            LeftOwnerKind = left.OwnerKind;
            LeftPriority = left.Priority;
            RightOwnerId = right.OwnerId;
            RightOwnerKind = right.OwnerKind;
            RightPriority = right.Priority;
            WinnerOwnerId = winner == null ? string.Empty : winner.OwnerId;
            LoserOwnerId = loser == null ? string.Empty : loser.OwnerId;
            this.overlap = new ReadOnlyCollection<SpecialRegionTileCoordinate>(
                (overlap ?? Array.Empty<SpecialRegionTileCoordinate>()).Distinct().OrderBy(value => value).ToArray());
        }

        public SpecialRegionCollisionKind Kind { get; }
        public string LeftOwnerId { get; }
        public SpecialRegionPlacementOwnerKind LeftOwnerKind { get; }
        public int LeftPriority { get; }
        public string RightOwnerId { get; }
        public SpecialRegionPlacementOwnerKind RightOwnerKind { get; }
        public int RightPriority { get; }
        public string WinnerOwnerId { get; }
        public string LoserOwnerId { get; }
        public IReadOnlyList<SpecialRegionTileCoordinate> Overlap => overlap;
        public int OverlapCellCount => overlap.Count;

        public int CompareTo(SpecialRegionCollisionDecision other)
        {
            if (other == null) return -1;
            var value = string.Compare(LeftOwnerId, other.LeftOwnerId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(RightOwnerId, other.RightOwnerId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = Kind.CompareTo(other.Kind);
            if (value != 0) return value;
            value = string.Compare(WinnerOwnerId, other.WinnerOwnerId, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(LoserOwnerId, other.LoserOwnerId, StringComparison.Ordinal);
        }

        public bool Equals(SpecialRegionCollisionDecision other)
            => other != null && Kind == other.Kind &&
               string.Equals(LeftOwnerId, other.LeftOwnerId, StringComparison.Ordinal) &&
               string.Equals(RightOwnerId, other.RightOwnerId, StringComparison.Ordinal) &&
               string.Equals(WinnerOwnerId, other.WinnerOwnerId, StringComparison.Ordinal) &&
               string.Equals(LoserOwnerId, other.LoserOwnerId, StringComparison.Ordinal) &&
               overlap.SequenceEqual(other.overlap);

        public override bool Equals(object obj) => Equals(obj as SpecialRegionCollisionDecision);

        public override int GetHashCode()
        {
            unchecked
            {
                var value = (int)Kind;
                value = (value * 397) ^ StringComparer.Ordinal.GetHashCode(LeftOwnerId);
                value = (value * 397) ^ StringComparer.Ordinal.GetHashCode(RightOwnerId);
                value = (value * 397) ^ StringComparer.Ordinal.GetHashCode(WinnerOwnerId);
                return (value * 397) ^ StringComparer.Ordinal.GetHashCode(LoserOwnerId);
            }
        }

        public override string ToString()
            => Kind + "|" + LeftOwnerId + "|" + RightOwnerId + "|" + WinnerOwnerId + "|" + LoserOwnerId;
    }

    public sealed class SpecialRegionPlacementCollisionCompileRequest
    {
        private readonly ReadOnlyCollection<SpecialRegionOccupancyClaim> claims;

        public SpecialRegionPlacementCollisionCompileRequest(IEnumerable<SpecialRegionOccupancyClaim> claims)
        {
            var supplied = claims == null ? Array.Empty<SpecialRegionOccupancyClaim>() : claims.ToArray();
            this.claims = new ReadOnlyCollection<SpecialRegionOccupancyClaim>(
                supplied.Where(value => value != null).ToArray());
            SuppliedNullClaimCount = supplied.Count(value => value == null);
        }

        public IReadOnlyList<SpecialRegionOccupancyClaim> Claims => claims;
        internal int SuppliedNullClaimCount { get; }
    }

    public sealed class SpecialRegionPlacementCollisionPlan
    {
        private readonly ReadOnlyCollection<SpecialRegionOccupancyClaim> claims;
        private readonly ReadOnlyCollection<SpecialRegionCollisionDecision> decisions;
        private readonly ReadOnlyCollection<string> acceptedOwnerIds;
        private readonly ReadOnlyCollection<string> rejectedOwnerIds;
        private readonly ReadOnlyCollection<string> replanOwnerIds;

        internal SpecialRegionPlacementCollisionPlan(
            IEnumerable<SpecialRegionOccupancyClaim> claims,
            IEnumerable<SpecialRegionCollisionDecision> decisions,
            IEnumerable<string> acceptedOwnerIds,
            IEnumerable<string> rejectedOwnerIds,
            IEnumerable<string> replanOwnerIds)
        {
            this.claims = new ReadOnlyCollection<SpecialRegionOccupancyClaim>(
                claims.Select(Clone).OrderBy(value => value.OwnerId, StringComparer.Ordinal)
                    .ThenByDescending(value => value.Priority)
                    .ThenBy(value => string.Join(";", value.Cells), StringComparer.Ordinal).ToArray());
            this.decisions = new ReadOnlyCollection<SpecialRegionCollisionDecision>(
                decisions.Distinct().OrderBy(value => value).ToArray());
            this.acceptedOwnerIds = FreezeIds(acceptedOwnerIds);
            this.rejectedOwnerIds = FreezeIds(rejectedOwnerIds);
            this.replanOwnerIds = FreezeIds(replanOwnerIds);
        }

        public IReadOnlyList<SpecialRegionOccupancyClaim> Claims => claims;
        public IReadOnlyList<SpecialRegionCollisionDecision> Decisions => decisions;
        public IReadOnlyList<string> AcceptedOwnerIds => acceptedOwnerIds;
        public IReadOnlyList<string> RejectedOwnerIds => rejectedOwnerIds;
        public IReadOnlyList<string> RequiresReplanOwnerIds => replanOwnerIds;
        public int RemovedPayloadCount => 0;
        public int GlobalLayerReorderCount => 0;
        public string CanonicalDigest { get; internal set; }

        private static SpecialRegionOccupancyClaim Clone(SpecialRegionOccupancyClaim source)
            => new SpecialRegionOccupancyClaim(
                source.OwnerId, source.OwnerKind, source.Cells, source.IsHardProtected, source.IsCommitted);

        private static ReadOnlyCollection<string> FreezeIds(IEnumerable<string> values)
            => new ReadOnlyCollection<string>((values ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public enum SpecialRegionPlacementCollisionErrorCode
    {
        MissingInput = 1,
        InvalidOwner = 2,
        InvalidClaim = 3,
        HardProtectedCollision = 4,
        AmbiguousSamePriority = 5,
        NonCanonicalPublication = 6,
    }

    public sealed class SpecialRegionPlacementCollisionError :
        IEquatable<SpecialRegionPlacementCollisionError>, IComparable<SpecialRegionPlacementCollisionError>
    {
        public SpecialRegionPlacementCollisionError(
            SpecialRegionPlacementCollisionErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SpecialRegionPlacementCollisionErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(SpecialRegionPlacementCollisionError other)
        {
            if (other == null) return -1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SpecialRegionPlacementCollisionError other)
            => other != null && Code == other.Code &&
               string.Equals(Path, other.Path, StringComparison.Ordinal) &&
               string.Equals(Detail, other.Detail, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as SpecialRegionPlacementCollisionError);

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

    public sealed class SpecialRegionPlacementCollisionResult
    {
        private readonly ReadOnlyCollection<SpecialRegionPlacementCollisionError> errors;

        internal SpecialRegionPlacementCollisionResult(
            SpecialRegionPlacementCollisionPlan plan,
            IEnumerable<SpecialRegionPlacementCollisionError> errors)
        {
            var values = (errors ?? Array.Empty<SpecialRegionPlacementCollisionError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<SpecialRegionPlacementCollisionError>(values);
            Plan = values.Length == 0 ? plan : null;
            CanonicalDigest = Plan == null ? string.Empty : Plan.CanonicalDigest;
        }

        public bool Succeeded => Plan != null && errors.Count == 0;
        public SpecialRegionPlacementCollisionPlan Plan { get; }
        public IReadOnlyList<SpecialRegionPlacementCollisionError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class SpecialRegionPlacementCollisionCompiler
    {
        public static SpecialRegionPlacementCollisionResult Compile(
            SpecialRegionPlacementCollisionCompileRequest request)
        {
            if (request == null)
                return Failure(SpecialRegionPlacementCollisionErrorCode.MissingInput, "request");
            var errors = new List<SpecialRegionPlacementCollisionError>();
            if (request.SuppliedNullClaimCount != 0)
                Add(errors, SpecialRegionPlacementCollisionErrorCode.InvalidClaim,
                    "claims", "Null claims are not canonical.");
            if (request.Claims.Count == 0)
                Add(errors, SpecialRegionPlacementCollisionErrorCode.MissingInput,
                    "claims", "At least one occupancy claim is required.");

            foreach (var claim in request.Claims) ValidateClaim(claim, errors);
            var inconsistentOwners = request.Claims.GroupBy(value => value.OwnerId, StringComparer.Ordinal)
                .Where(group => group.Select(value => new
                {
                    value.OwnerKind,
                    value.IsHardProtected,
                    value.IsCommitted,
                }).Distinct().Count() != 1);
            foreach (var owner in inconsistentOwners)
                Add(errors, SpecialRegionPlacementCollisionErrorCode.InvalidOwner,
                    "claims/" + owner.Key, "One owner must publish one kind and protection state.");

            var ordered = request.Claims.OrderBy(value => value.OwnerId, StringComparer.Ordinal)
                .ThenByDescending(value => value.Priority)
                .ThenBy(value => string.Join(";", value.Cells), StringComparer.Ordinal).ToArray();
            var decisions = new List<SpecialRegionCollisionDecision>();
            var accepted = new HashSet<string>(ordered.Select(value => value.OwnerId), StringComparer.Ordinal);
            var rejected = new HashSet<string>(StringComparer.Ordinal);
            var replan = new HashSet<string>(StringComparer.Ordinal);

            for (var leftIndex = 0; leftIndex < ordered.Length; leftIndex++)
            {
                for (var rightIndex = leftIndex + 1; rightIndex < ordered.Length; rightIndex++)
                {
                    var left = ordered[leftIndex];
                    var right = ordered[rightIndex];
                    if (string.Equals(left.OwnerId, right.OwnerId, StringComparison.Ordinal)) continue;
                    var overlap = left.Cells.Intersect(right.Cells).OrderBy(value => value).ToArray();
                    if (overlap.Length == 0)
                    {
                        decisions.Add(new SpecialRegionCollisionDecision(
                            SpecialRegionCollisionKind.NoOverlap, left, right, null, null, overlap));
                        continue;
                    }
                    if (left.IsHardProtected || right.IsHardProtected)
                    {
                        Add(errors, SpecialRegionPlacementCollisionErrorCode.HardProtectedCollision,
                            PairPath(left, right), "HardProtected occupancy may never be overwritten.");
                        continue;
                    }
                    if (left.Priority == right.Priority)
                    {
                        Add(errors, SpecialRegionPlacementCollisionErrorCode.AmbiguousSamePriority,
                            PairPath(left, right), "Different same-priority owners overlap.");
                        continue;
                    }

                    var higher = left.Priority > right.Priority ? left : right;
                    var lower = ReferenceEquals(higher, left) ? right : left;
                    if (lower.IsCommitted)
                    {
                        decisions.Add(new SpecialRegionCollisionDecision(
                            SpecialRegionCollisionKind.RequiresReplan, left, right, higher, lower, overlap));
                        replan.Add(higher.OwnerId);
                    }
                    else
                    {
                        decisions.Add(new SpecialRegionCollisionDecision(
                            SpecialRegionCollisionKind.HigherPriorityWins, left, right, higher, lower, overlap));
                        rejected.Add(lower.OwnerId);
                        accepted.Remove(lower.OwnerId);
                    }
                }
            }

            if (errors.Count != 0) return new SpecialRegionPlacementCollisionResult(null, errors);
            var plan = new SpecialRegionPlacementCollisionPlan(ordered, decisions, accepted, rejected, replan);
            plan.CanonicalDigest = SpecialRegionPlacementCollisionCanonicalDigest.Compute(plan);
            if (string.IsNullOrEmpty(plan.CanonicalDigest))
                return Failure(SpecialRegionPlacementCollisionErrorCode.NonCanonicalPublication, "plan");
            return new SpecialRegionPlacementCollisionResult(plan, Array.Empty<SpecialRegionPlacementCollisionError>());
        }

        public static int GetPriority(SpecialRegionPlacementOwnerKind kind)
        {
            switch (kind)
            {
                case SpecialRegionPlacementOwnerKind.Boss: return 700;
                case SpecialRegionPlacementOwnerKind.Forge: return 600;
                case SpecialRegionPlacementOwnerKind.CoreResource: return 500;
                case SpecialRegionPlacementOwnerKind.Village: return 400;
                case SpecialRegionPlacementOwnerKind.RareRegion: return 300;
                case SpecialRegionPlacementOwnerKind.TerrainCluster: return 200;
                case SpecialRegionPlacementOwnerKind.ActivityStructure: return 100;
                default: return 0;
            }
        }

        private static void ValidateClaim(
            SpecialRegionOccupancyClaim claim,
            ICollection<SpecialRegionPlacementCollisionError> errors)
        {
            if (!IsCanonicalId(claim.OwnerId) || GetPriority(claim.OwnerKind) == 0)
                Add(errors, SpecialRegionPlacementCollisionErrorCode.InvalidOwner,
                    "claims/" + claim.OwnerId, "Stable owner ID and supported owner kind are required.");
            if (claim.Cells.Count == 0 || claim.Cells.Distinct().Count() != claim.Cells.Count ||
                claim.Cells.Any(value => !IsWorldSector(value.WorldSector) || !IsLocalTile(value.LocalTile)))
                Add(errors, SpecialRegionPlacementCollisionErrorCode.InvalidClaim,
                    "claims/" + claim.OwnerId, "Claim cells must be unique canonical world sector/local tiles.");
        }

        private static string PairPath(SpecialRegionOccupancyClaim left, SpecialRegionOccupancyClaim right)
            => "collisions/" + left.OwnerId + "/" + right.OwnerId;

        private static bool IsWorldSector(SectorCoord value)
            => value.X >= 0 && value.X < WorldGenConstants.SectorColumns &&
               value.Y >= 0 && value.Y < WorldGenConstants.SectorRows;

        private static bool IsLocalTile(LocalTileCoord value)
            => value.X >= 0 && value.X < WorldGenConstants.SectorWidthTiles &&
               value.Y >= 0 && value.Y < WorldGenConstants.SectorHeightTiles;

        private static bool IsCanonicalId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 128 || value.Trim() != value) return false;
            return value.All(character => (character >= 'a' && character <= 'z') ||
                (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9') ||
                character == '.' || character == '_' || character == ':' || character == '-');
        }

        private static SpecialRegionPlacementCollisionResult Failure(
            SpecialRegionPlacementCollisionErrorCode code, string path)
            => new SpecialRegionPlacementCollisionResult(null, new[]
            {
                new SpecialRegionPlacementCollisionError(code, path, "Required canonical input was not supplied.")
            });

        private static void Add(
            ICollection<SpecialRegionPlacementCollisionError> errors,
            SpecialRegionPlacementCollisionErrorCode code,
            string path,
            string detail)
            => errors.Add(new SpecialRegionPlacementCollisionError(code, path, detail));
    }

    public static class SpecialRegionPlacementCollisionCanonicalDigest
    {
        public static string Compute(SpecialRegionPlacementCollisionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var value = new StringBuilder();
            foreach (var claim in plan.Claims)
            {
                Append(value, "claim", claim.OwnerId + "/" + Number((int)claim.OwnerKind) + "/" +
                    Number(claim.Priority) + "/" + Flag(claim.IsHardProtected) + "/" + Flag(claim.IsCommitted));
                foreach (var cell in claim.Cells) Append(value, "claimCell", claim.OwnerId + "/" + cell);
            }
            foreach (var decision in plan.Decisions)
            {
                Append(value, "decision", Number((int)decision.Kind) + "/" + decision.LeftOwnerId + "/" +
                    decision.RightOwnerId + "/" + decision.WinnerOwnerId + "/" + decision.LoserOwnerId);
                foreach (var cell in decision.Overlap)
                    Append(value, "overlap", decision.LeftOwnerId + "/" + decision.RightOwnerId + "/" + cell);
            }
            foreach (var owner in plan.AcceptedOwnerIds) Append(value, "accepted", owner);
            foreach (var owner in plan.RejectedOwnerIds) Append(value, "rejected", owner);
            foreach (var owner in plan.RequiresReplanOwnerIds) Append(value, "replan", owner);
            return Sha256(value.ToString());
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Flag(bool value) => value ? "1" : "0";
        private static void Append(StringBuilder value, string name, string field)
            => value.Append(name).Append('=').Append(field ?? string.Empty).Append('\n');

        private static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(material))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
