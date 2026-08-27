using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public enum MicroPatternCleanupRule
    {
        SolidSpeck = 1,
        AirPinhole = 2,
        HeadSnag = 3,
        BoxedBottomPit = 4,
    }

    public enum MicroPatternCleanupIssueCode
    {
        ProtectedWriteBlocked = 1,
        InsufficientNeighborhood = 2,
    }

    public enum MicroPatternLocalCleanupErrorCode
    {
        MissingInput = 1,
        InvalidCoordinate = 2,
        DuplicateCoordinate = 3,
        MissingOwnedCell = 4,
        UnexpectedOwnedCell = 5,
        InvalidHalo = 6,
        InvalidProtection = 7,
        ConflictingCleanupProposal = 8,
        AtomicCleanupRejected = 9,
    }

    public sealed class MicroPatternCleanupCell
    {
        private readonly ReadOnlyCollection<MicroPatternProtectedCell> protectionProvenance;

        public MicroPatternCleanupCell(
            LocalTileCoord targetCoordinate,
            bool solid,
            bool isOwned,
            bool isProtected,
            IEnumerable<MicroPatternProtectedCell> protectionProvenance = null)
        {
            TargetCoordinate = targetCoordinate;
            Solid = solid;
            IsOwned = isOwned;
            IsProtected = isProtected;
            var copy = protectionProvenance == null
                ? Array.Empty<MicroPatternProtectedCell>()
                : protectionProvenance.Distinct().ToArray();
            Array.Sort(copy, CompareProtection);
            this.protectionProvenance =
                new ReadOnlyCollection<MicroPatternProtectedCell>(copy);
        }

        public LocalTileCoord TargetCoordinate { get; }
        public bool Solid { get; }
        public bool IsOwned { get; }
        public bool IsProtected { get; }
        public IReadOnlyList<MicroPatternProtectedCell> ProtectionProvenance => protectionProvenance;

        private static int CompareProtection(
            MicroPatternProtectedCell left,
            MicroPatternProtectedCell right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return left.CompareTo(right);
        }
    }

    public sealed class MicroPatternCleanupSnapshot
    {
        private readonly ReadOnlyCollection<MicroPatternCleanupCell> cells;

        public MicroPatternCleanupSnapshot(IEnumerable<MicroPatternCleanupCell> cells)
        {
            var copy = cells == null
                ? Array.Empty<MicroPatternCleanupCell>()
                : cells.ToArray();
            Array.Sort(copy, CompareCells);
            this.cells = new ReadOnlyCollection<MicroPatternCleanupCell>(copy);
        }

        public IReadOnlyList<MicroPatternCleanupCell> Cells => cells;

        private static int CompareCells(MicroPatternCleanupCell left, MicroPatternCleanupCell right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            var comparison = left.TargetCoordinate.Y.CompareTo(right.TargetCoordinate.Y);
            return comparison != 0
                ? comparison
                : left.TargetCoordinate.X.CompareTo(right.TargetCoordinate.X);
        }
    }

    public sealed class MicroPatternCleanupNeighborEvidence :
        IEquatable<MicroPatternCleanupNeighborEvidence>,
        IComparable<MicroPatternCleanupNeighborEvidence>
    {
        public MicroPatternCleanupNeighborEvidence(
            string relativePosition,
            LocalTileCoord targetCoordinate,
            bool solid)
        {
            RelativePosition = relativePosition ?? string.Empty;
            TargetCoordinate = targetCoordinate;
            Solid = solid;
        }

        public string RelativePosition { get; }
        public LocalTileCoord TargetCoordinate { get; }
        public bool Solid { get; }

        public int CompareTo(MicroPatternCleanupNeighborEvidence other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(
                RelativePosition,
                other.RelativePosition,
                StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.Y.CompareTo(other.TargetCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.X.CompareTo(other.TargetCoordinate.X);
            return comparison != 0 ? comparison : Solid.CompareTo(other.Solid);
        }

        public bool Equals(MicroPatternCleanupNeighborEvidence other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternCleanupNeighborEvidence);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return RelativePosition + "|" + Coordinate(TargetCoordinate) + "|" +
                   (Solid ? "Solid" : "Air");
        }

        private static string Coordinate(LocalTileCoord coordinate)
        {
            return coordinate.X.ToString(CultureInfo.InvariantCulture) + "," +
                   coordinate.Y.ToString(CultureInfo.InvariantCulture);
        }
    }

    public sealed class MicroPatternCleanupIssue :
        IEquatable<MicroPatternCleanupIssue>,
        IComparable<MicroPatternCleanupIssue>
    {
        private readonly ReadOnlyCollection<MicroPatternProtectedCell> protectionProvenance;

        public MicroPatternCleanupIssue(
            MicroPatternCleanupIssueCode code,
            LocalTileCoord targetCoordinate,
            MicroPatternCleanupRule rule,
            string detail,
            IEnumerable<MicroPatternProtectedCell> protectionProvenance = null)
        {
            Code = code;
            TargetCoordinate = targetCoordinate;
            Rule = rule;
            Detail = detail ?? string.Empty;
            var copy = (protectionProvenance ?? Array.Empty<MicroPatternProtectedCell>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.protectionProvenance =
                new ReadOnlyCollection<MicroPatternProtectedCell>(copy);
        }

        public MicroPatternCleanupIssueCode Code { get; }
        public LocalTileCoord TargetCoordinate { get; }
        public MicroPatternCleanupRule Rule { get; }
        public string Detail { get; }
        public IReadOnlyList<MicroPatternProtectedCell> ProtectionProvenance => protectionProvenance;

        public int CompareTo(MicroPatternCleanupIssue other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.Y.CompareTo(other.TargetCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.X.CompareTo(other.TargetCoordinate.X);
            if (comparison != 0) return comparison;
            comparison = ((int)Rule).CompareTo((int)other.Rule);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Detail, other.Detail, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(ProvenanceKey(), other.ProvenanceKey(), StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternCleanupIssue other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternCleanupIssue);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Code + "|" + Coordinate(TargetCoordinate) + "|" + Rule + "|" +
                   Detail + "|" + ProvenanceKey();
        }

        private string ProvenanceKey()
        {
            return string.Join(";", protectionProvenance.Select(value => value.ToString()));
        }

        private static string Coordinate(LocalTileCoord coordinate)
        {
            return coordinate.X.ToString(CultureInfo.InvariantCulture) + "," +
                   coordinate.Y.ToString(CultureInfo.InvariantCulture);
        }
    }

    public sealed class MicroPatternCleanupProposal :
        IEquatable<MicroPatternCleanupProposal>,
        IComparable<MicroPatternCleanupProposal>
    {
        private readonly ReadOnlyCollection<MicroPatternCleanupRule> rules;
        private readonly ReadOnlyCollection<MicroPatternCleanupNeighborEvidence> neighborhoodEvidence;

        public MicroPatternCleanupProposal(
            LocalTileCoord targetCoordinate,
            bool desiredSolid,
            MicroPatternCleanupRule rule,
            IEnumerable<MicroPatternCleanupNeighborEvidence> neighborhoodEvidence = null)
            : this(targetCoordinate, desiredSolid, new[] { rule }, neighborhoodEvidence)
        {
        }

        internal MicroPatternCleanupProposal(
            LocalTileCoord targetCoordinate,
            bool desiredSolid,
            IEnumerable<MicroPatternCleanupRule> rules,
            IEnumerable<MicroPatternCleanupNeighborEvidence> neighborhoodEvidence)
        {
            TargetCoordinate = targetCoordinate;
            DesiredSolid = desiredSolid;
            var ruleCopy = (rules ?? Array.Empty<MicroPatternCleanupRule>())
                .Distinct()
                .OrderBy(value => (int)value)
                .ToArray();
            this.rules = new ReadOnlyCollection<MicroPatternCleanupRule>(ruleCopy);
            var evidenceCopy = (neighborhoodEvidence ??
                                Array.Empty<MicroPatternCleanupNeighborEvidence>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.neighborhoodEvidence =
                new ReadOnlyCollection<MicroPatternCleanupNeighborEvidence>(evidenceCopy);
        }

        public LocalTileCoord TargetCoordinate { get; }
        public bool DesiredSolid { get; }
        public IReadOnlyList<MicroPatternCleanupRule> Rules => rules;
        public IReadOnlyList<MicroPatternCleanupNeighborEvidence> NeighborhoodEvidence =>
            neighborhoodEvidence;

        public int CompareTo(MicroPatternCleanupProposal other)
        {
            if (other == null) return -1;
            var comparison = TargetCoordinate.Y.CompareTo(other.TargetCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.X.CompareTo(other.TargetCoordinate.X);
            if (comparison != 0) return comparison;
            comparison = DesiredSolid.CompareTo(other.DesiredSolid);
            if (comparison != 0) return comparison;
            comparison = string.Compare(RuleKey(), other.RuleKey(), StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(EvidenceKey(), other.EvidenceKey(), StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternCleanupProposal other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternCleanupProposal);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Coordinate(TargetCoordinate) + "|" + DesiredSolid + "|" + RuleKey() + "|" +
                   EvidenceKey();
        }

        private string RuleKey()
        {
            return string.Join(",", rules.Select(value => value.ToString()));
        }

        private string EvidenceKey()
        {
            return string.Join(";", neighborhoodEvidence.Select(value => value.ToString()));
        }

        private static string Coordinate(LocalTileCoord coordinate)
        {
            return coordinate.X.ToString(CultureInfo.InvariantCulture) + "," +
                   coordinate.Y.ToString(CultureInfo.InvariantCulture);
        }
    }

    public sealed class MicroPatternCleanupCellDelta
    {
        private readonly ReadOnlyCollection<MicroPatternCleanupRule> rules;
        private readonly ReadOnlyCollection<MicroPatternCleanupNeighborEvidence> neighborhoodEvidence;
        private readonly ReadOnlyCollection<MicroPatternProtectedCell> protectionEvidence;

        internal MicroPatternCleanupCellDelta(
            LocalTileCoord targetCoordinate,
            bool beforeSolid,
            bool afterSolid,
            IEnumerable<MicroPatternCleanupRule> rules,
            IEnumerable<MicroPatternCleanupNeighborEvidence> neighborhoodEvidence,
            IEnumerable<MicroPatternProtectedCell> protectionEvidence)
        {
            TargetCoordinate = targetCoordinate;
            BeforeSolid = beforeSolid;
            AfterSolid = afterSolid;
            this.rules = new ReadOnlyCollection<MicroPatternCleanupRule>(
                rules.Distinct().OrderBy(value => (int)value).ToArray());
            this.neighborhoodEvidence =
                new ReadOnlyCollection<MicroPatternCleanupNeighborEvidence>(
                    neighborhoodEvidence.Where(value => value != null)
                        .Distinct().OrderBy(value => value).ToArray());
            this.protectionEvidence = new ReadOnlyCollection<MicroPatternProtectedCell>(
                protectionEvidence.Where(value => value != null)
                    .Distinct().OrderBy(value => value).ToArray());
        }

        public LocalTileCoord TargetCoordinate { get; }
        public bool BeforeSolid { get; }
        public bool AfterSolid { get; }
        public IReadOnlyList<MicroPatternCleanupRule> Rules => rules;
        public IReadOnlyList<MicroPatternCleanupNeighborEvidence> NeighborhoodEvidence =>
            neighborhoodEvidence;
        public IReadOnlyList<MicroPatternProtectedCell> ProtectionEvidence => protectionEvidence;
    }

    public sealed class MicroPatternCleanupDelta
    {
        private readonly ReadOnlyCollection<MicroPatternCleanupCellDelta> cells;

        internal MicroPatternCleanupDelta(
            IEnumerable<MicroPatternCleanupCellDelta> cells,
            string stableDigest)
        {
            var copy = cells.OrderBy(value => value.TargetCoordinate.Y)
                .ThenBy(value => value.TargetCoordinate.X)
                .ToArray();
            this.cells = new ReadOnlyCollection<MicroPatternCleanupCellDelta>(copy);
            StableDigest = stableDigest ?? string.Empty;
        }

        public IReadOnlyList<MicroPatternCleanupCellDelta> Cells => cells;
        public string StableDigest { get; }
    }

    public sealed class MicroPatternLocalCleanupError :
        IEquatable<MicroPatternLocalCleanupError>,
        IComparable<MicroPatternLocalCleanupError>
    {
        public MicroPatternLocalCleanupError(
            MicroPatternLocalCleanupErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternLocalCleanupErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternLocalCleanupError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternLocalCleanupError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternLocalCleanupError);
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

    public sealed class MicroPatternLocalCleanupResult
    {
        private readonly ReadOnlyCollection<MicroPatternCleanupProposal> proposals;
        private readonly ReadOnlyCollection<MicroPatternCleanupIssue> issues;
        private readonly ReadOnlyCollection<MicroPatternLocalCleanupError> errors;

        internal MicroPatternLocalCleanupResult(
            MicroPatternCleanupDelta delta,
            IEnumerable<MicroPatternCleanupProposal> proposals,
            IEnumerable<MicroPatternCleanupIssue> issues,
            IEnumerable<MicroPatternLocalCleanupError> errors)
        {
            var errorCopy = (errors ?? Array.Empty<MicroPatternLocalCleanupError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternLocalCleanupError>(errorCopy);
            var proposalCopy = (proposals ?? Array.Empty<MicroPatternCleanupProposal>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.proposals = new ReadOnlyCollection<MicroPatternCleanupProposal>(proposalCopy);
            var issueCopy = (issues ?? Array.Empty<MicroPatternCleanupIssue>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.issues = new ReadOnlyCollection<MicroPatternCleanupIssue>(issueCopy);
            Delta = errorCopy.Length == 0 ? delta : null;
            StableDigest = Delta == null ? string.Empty : Delta.StableDigest;
        }

        public bool Success => Delta != null && errors.Count == 0;
        public MicroPatternCleanupDelta Delta { get; }
        public IReadOnlyList<MicroPatternCleanupProposal> Proposals => proposals;
        public IReadOnlyList<MicroPatternCleanupIssue> Issues => issues;
        public IReadOnlyList<MicroPatternLocalCleanupError> Errors => errors;
        public string StableDigest { get; }
    }

    public static class MicroPatternLocalCleanup
    {
        private static readonly NeighborRequirement[] Cardinal =
        {
            new NeighborRequirement("Up", 0, 1),
            new NeighborRequirement("Down", 0, -1),
            new NeighborRequirement("Left", -1, 0),
            new NeighborRequirement("Right", 1, 0),
        };

        private static readonly NeighborRequirement[] SixCellShape =
        {
            new NeighborRequirement("Up", 0, 1),
            new NeighborRequirement("UpLeft", -1, 1),
            new NeighborRequirement("UpRight", 1, 1),
            new NeighborRequirement("Left", -1, 0),
            new NeighborRequirement("Right", 1, 0),
            new NeighborRequirement("Down", 0, -1),
        };

        public static MicroPatternLocalCleanupResult Evaluate(
            MicroPatternCleanupSnapshot snapshot)
        {
            var errors = ValidateSnapshot(snapshot, out var byCoordinate);
            if (errors.Count != 0)
            {
                return new MicroPatternLocalCleanupResult(null, null, null, errors);
            }

            var proposals = new List<MicroPatternCleanupProposal>();
            var issues = new List<MicroPatternCleanupIssue>();
            foreach (var center in snapshot.Cells.Where(value => value.IsOwned))
            {
                if (center.Solid)
                {
                    Detect(
                        center,
                        MicroPatternCleanupRule.SolidSpeck,
                        false,
                        Cardinal,
                        value => !value["Up"] && !value["Down"] &&
                                 !value["Left"] && !value["Right"],
                        byCoordinate,
                        proposals,
                        issues,
                        errors);
                    Detect(
                        center,
                        MicroPatternCleanupRule.HeadSnag,
                        false,
                        SixCellShape,
                        value => value["Up"] && value["UpLeft"] && value["UpRight"] &&
                                 !value["Left"] && !value["Right"] && !value["Down"],
                        byCoordinate,
                        proposals,
                        issues,
                        errors);
                }
                else
                {
                    Detect(
                        center,
                        MicroPatternCleanupRule.AirPinhole,
                        true,
                        Cardinal,
                        value => value["Up"] && value["Down"] &&
                                 value["Left"] && value["Right"],
                        byCoordinate,
                        proposals,
                        issues,
                        errors);
                    Detect(
                        center,
                        MicroPatternCleanupRule.BoxedBottomPit,
                        true,
                        SixCellShape,
                        value => !value["Up"] && value["UpLeft"] && value["UpRight"] &&
                                 value["Left"] && value["Right"] && value["Down"],
                        byCoordinate,
                        proposals,
                        issues,
                        errors);
                }
            }

            if (errors.Count != 0)
            {
                return new MicroPatternLocalCleanupResult(null, proposals, issues, errors);
            }
            return ResolveValidated(snapshot, byCoordinate, proposals, issues);
        }

        public static MicroPatternLocalCleanupResult ResolveProposals(
            MicroPatternCleanupSnapshot snapshot,
            IEnumerable<MicroPatternCleanupProposal> proposals)
        {
            var errors = ValidateSnapshot(snapshot, out var byCoordinate);
            if (proposals == null)
            {
                errors.Add(Error(
                    MicroPatternLocalCleanupErrorCode.MissingInput,
                    "proposals",
                    "Cleanup proposals are required."));
                return new MicroPatternLocalCleanupResult(null, null, null, errors);
            }

            var copy = proposals.ToArray();
            for (var index = 0; index < copy.Length; index++)
            {
                var proposal = copy[index];
                var path = "proposals[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                if (proposal == null || proposal.Rules.Count == 0 ||
                    proposal.Rules.Any(value => !IsDefined(value)))
                {
                    errors.Add(Error(
                        MicroPatternLocalCleanupErrorCode.AtomicCleanupRejected,
                        path,
                        "A proposal with canonical rule provenance is required."));
                    continue;
                }
                if (!byCoordinate.TryGetValue(proposal.TargetCoordinate, out var target))
                {
                    errors.Add(Error(
                        MicroPatternLocalCleanupErrorCode.InvalidCoordinate,
                        path,
                        Coordinate(proposal.TargetCoordinate)));
                }
                else if (!target.IsOwned)
                {
                    errors.Add(Error(
                        MicroPatternLocalCleanupErrorCode.UnexpectedOwnedCell,
                        path,
                        Coordinate(proposal.TargetCoordinate)));
                }
            }

            if (errors.Count != 0)
            {
                return new MicroPatternLocalCleanupResult(null, copy, null, errors);
            }
            return ResolveValidated(snapshot, byCoordinate, copy, Array.Empty<MicroPatternCleanupIssue>());
        }

        private static MicroPatternLocalCleanupResult ResolveValidated(
            MicroPatternCleanupSnapshot snapshot,
            IReadOnlyDictionary<LocalTileCoord, MicroPatternCleanupCell> byCoordinate,
            IEnumerable<MicroPatternCleanupProposal> proposals,
            IEnumerable<MicroPatternCleanupIssue> detectedIssues)
        {
            var proposalArray = proposals.Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray();
            var errors = new List<MicroPatternLocalCleanupError>();
            var issues = new List<MicroPatternCleanupIssue>(detectedIssues);
            var coalesced = new List<MicroPatternCleanupProposal>();
            foreach (var group in proposalArray.GroupBy(value => value.TargetCoordinate)
                         .OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X))
            {
                var desired = group.Select(value => value.DesiredSolid).Distinct().ToArray();
                if (desired.Length != 1)
                {
                    errors.Add(Error(
                        MicroPatternLocalCleanupErrorCode.ConflictingCleanupProposal,
                        "proposals[" + Coordinate(group.Key) + "]",
                        string.Join(",", desired.OrderBy(value => value))));
                    continue;
                }

                coalesced.Add(new MicroPatternCleanupProposal(
                    group.Key,
                    desired[0],
                    group.SelectMany(value => value.Rules),
                    group.SelectMany(value => value.NeighborhoodEvidence)));
            }

            if (errors.Count != 0)
            {
                errors.Add(Error(
                    MicroPatternLocalCleanupErrorCode.AtomicCleanupRejected,
                    "proposals",
                    "Conflicting cleanup proposals reject the whole batch."));
                return new MicroPatternLocalCleanupResult(null, proposalArray, issues, errors);
            }

            var deltas = new List<MicroPatternCleanupCellDelta>();
            foreach (var proposal in coalesced)
            {
                var target = byCoordinate[proposal.TargetCoordinate];
                if (target.IsProtected)
                {
                    foreach (var rule in proposal.Rules)
                    {
                        issues.Add(new MicroPatternCleanupIssue(
                            MicroPatternCleanupIssueCode.ProtectedWriteBlocked,
                            proposal.TargetCoordinate,
                            rule,
                            "Protected cleanup target was not changed.",
                            target.ProtectionProvenance));
                    }
                    continue;
                }
                if (target.Solid == proposal.DesiredSolid) continue;
                deltas.Add(new MicroPatternCleanupCellDelta(
                    proposal.TargetCoordinate,
                    target.Solid,
                    proposal.DesiredSolid,
                    proposal.Rules,
                    proposal.NeighborhoodEvidence,
                    target.ProtectionProvenance));
            }

            issues = issues.Distinct().OrderBy(value => value).ToList();
            coalesced = coalesced.Distinct().OrderBy(value => value).ToList();
            deltas = deltas.OrderBy(value => value.TargetCoordinate.Y)
                .ThenBy(value => value.TargetCoordinate.X)
                .ToList();
            var digest = MicroPatternCleanupCanonicalDigest.Compute(
                snapshot,
                issues,
                coalesced,
                deltas);
            return new MicroPatternLocalCleanupResult(
                new MicroPatternCleanupDelta(deltas, digest),
                coalesced,
                issues,
                errors);
        }

        private static List<MicroPatternLocalCleanupError> ValidateSnapshot(
            MicroPatternCleanupSnapshot snapshot,
            out IReadOnlyDictionary<LocalTileCoord, MicroPatternCleanupCell> byCoordinate)
        {
            var errors = new List<MicroPatternLocalCleanupError>();
            var mutable = new Dictionary<LocalTileCoord, MicroPatternCleanupCell>();
            byCoordinate = mutable;
            if (snapshot == null)
            {
                errors.Add(Error(
                    MicroPatternLocalCleanupErrorCode.MissingInput,
                    "snapshot",
                    "Cleanup snapshot is required."));
                return errors;
            }

            foreach (var cell in snapshot.Cells)
            {
                if (cell == null)
                {
                    errors.Add(Error(
                        MicroPatternLocalCleanupErrorCode.InvalidCoordinate,
                        "snapshot.cells",
                        "Null cleanup cell."));
                    continue;
                }
                if (!mutable.TryAdd(cell.TargetCoordinate, cell))
                {
                    errors.Add(Error(
                        MicroPatternLocalCleanupErrorCode.DuplicateCoordinate,
                        "snapshot.cells[" + Coordinate(cell.TargetCoordinate) + "]",
                        "Cleanup coordinates must be unique."));
                }
                ValidateProtection(cell, errors);
            }

            var owned = snapshot.Cells.Where(value => value != null && value.IsOwned).ToArray();
            if (owned.Length == 0)
            {
                errors.Add(Error(
                    MicroPatternLocalCleanupErrorCode.MissingOwnedCell,
                    "snapshot.cells",
                    "At least one owned cleanup target is required."));
            }
            else
            {
                foreach (var halo in snapshot.Cells.Where(value => value != null && !value.IsOwned))
                {
                    if (!owned.Any(value => IsOneCellHalo(value.TargetCoordinate, halo.TargetCoordinate)))
                    {
                        errors.Add(Error(
                            MicroPatternLocalCleanupErrorCode.InvalidHalo,
                            "snapshot.cells[" + Coordinate(halo.TargetCoordinate) + "]",
                            "Read-only halo cells must be within one cell of an owned target."));
                    }
                }
            }
            return errors;
        }

        private static void ValidateProtection(
            MicroPatternCleanupCell cell,
            ICollection<MicroPatternLocalCleanupError> errors)
        {
            var path = "snapshot.cells[" + Coordinate(cell.TargetCoordinate) + "].protection";
            if (cell.IsProtected != (cell.ProtectionProvenance.Count != 0))
            {
                errors.Add(Error(
                    MicroPatternLocalCleanupErrorCode.InvalidProtection,
                    path,
                    "Protected flag and provenance must agree."));
            }
            foreach (var source in cell.ProtectionProvenance)
            {
                if (source == null || source.TargetCoordinate != cell.TargetCoordinate ||
                    source.SourceKind < MicroPatternProtectedSourceKind.RouteSpine ||
                    source.SourceKind > MicroPatternProtectedSourceKind.SpecialFixedEntry ||
                    !IsStableId(source.SourceId))
                {
                    errors.Add(Error(
                        MicroPatternLocalCleanupErrorCode.InvalidProtection,
                        path,
                        source == null ? "<null>" : source.ToString()));
                }
            }
        }

        private static void Detect(
            MicroPatternCleanupCell center,
            MicroPatternCleanupRule rule,
            bool desiredSolid,
            IEnumerable<NeighborRequirement> requirements,
            Func<IReadOnlyDictionary<string, bool>, bool> predicate,
            IReadOnlyDictionary<LocalTileCoord, MicroPatternCleanupCell> byCoordinate,
            ICollection<MicroPatternCleanupProposal> proposals,
            ICollection<MicroPatternCleanupIssue> issues,
            ICollection<MicroPatternLocalCleanupError> errors)
        {
            var states = new Dictionary<string, bool>(StringComparer.Ordinal);
            var evidence = new List<MicroPatternCleanupNeighborEvidence>();
            var missing = new List<string>();
            foreach (var requirement in requirements)
            {
                LocalTileCoord coordinate;
                try
                {
                    coordinate = new LocalTileCoord(
                        checked(center.TargetCoordinate.X + requirement.OffsetX),
                        checked(center.TargetCoordinate.Y + requirement.OffsetY));
                }
                catch (OverflowException)
                {
                    errors.Add(Error(
                        MicroPatternLocalCleanupErrorCode.InvalidCoordinate,
                        "snapshot.cells[" + Coordinate(center.TargetCoordinate) + "]",
                        rule + " neighbor coordinate overflow."));
                    return;
                }

                if (!byCoordinate.TryGetValue(coordinate, out var neighbor))
                {
                    missing.Add(requirement.Name + "@" + Coordinate(coordinate));
                    continue;
                }
                states.Add(requirement.Name, neighbor.Solid);
                evidence.Add(new MicroPatternCleanupNeighborEvidence(
                    requirement.Name,
                    coordinate,
                    neighbor.Solid));
            }

            if (missing.Count != 0)
            {
                issues.Add(new MicroPatternCleanupIssue(
                    MicroPatternCleanupIssueCode.InsufficientNeighborhood,
                    center.TargetCoordinate,
                    rule,
                    string.Join(",", missing.OrderBy(value => value, StringComparer.Ordinal))));
                return;
            }
            if (predicate(states))
            {
                proposals.Add(new MicroPatternCleanupProposal(
                    center.TargetCoordinate,
                    desiredSolid,
                    rule,
                    evidence));
            }
        }

        private static bool IsOneCellHalo(LocalTileCoord owned, LocalTileCoord halo)
        {
            var x = Math.Abs((long)owned.X - halo.X);
            var y = Math.Abs((long)owned.Y - halo.Y);
            return x <= 1 && y <= 1 && (x != 0 || y != 0);
        }

        private static bool IsStableId(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] < 'A' || value[0] > 'Z') return false;
            for (var index = 1; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') && character != '_') return false;
            }
            return true;
        }

        private static bool IsDefined(MicroPatternCleanupRule rule)
        {
            return rule >= MicroPatternCleanupRule.SolidSpeck &&
                   rule <= MicroPatternCleanupRule.BoxedBottomPit;
        }

        private static MicroPatternLocalCleanupError Error(
            MicroPatternLocalCleanupErrorCode code,
            string path,
            string detail)
        {
            return new MicroPatternLocalCleanupError(code, path, detail);
        }

        private static string Coordinate(LocalTileCoord coordinate)
        {
            return coordinate.X.ToString(CultureInfo.InvariantCulture) + "," +
                   coordinate.Y.ToString(CultureInfo.InvariantCulture);
        }

        private readonly struct NeighborRequirement
        {
            public NeighborRequirement(string name, int offsetX, int offsetY)
            {
                Name = name;
                OffsetX = offsetX;
                OffsetY = offsetY;
            }

            public string Name { get; }
            public int OffsetX { get; }
            public int OffsetY { get; }
        }
    }

    public static class MicroPatternCleanupCanonicalDigest
    {
        public const string Ruleset = "MAP10_05_CLEANUP_V1";

        public static string Compute(
            MicroPatternCleanupSnapshot snapshot,
            IEnumerable<MicroPatternCleanupIssue> issues,
            IEnumerable<MicroPatternCleanupProposal> proposals,
            IEnumerable<MicroPatternCleanupCellDelta> deltas)
        {
            var material = new StringBuilder();
            MicroPatternContractDigest.Append(material, "RULESET", Ruleset);
            foreach (var cell in snapshot.Cells.OrderBy(value => value.TargetCoordinate.Y)
                         .ThenBy(value => value.TargetCoordinate.X))
            {
                MicroPatternContractDigest.Append(
                    material,
                    "CELL",
                    Number(cell.TargetCoordinate.X),
                    Number(cell.TargetCoordinate.Y),
                    cell.IsOwned ? "OWNED" : "HALO",
                    cell.Solid ? "SOLID" : "AIR",
                    cell.IsProtected ? "PROTECTED" : "OPEN");
                foreach (var source in cell.ProtectionProvenance)
                {
                    MicroPatternContractDigest.Append(
                        material,
                        "PROTECTION",
                        source.SourceKind.ToString(),
                        source.SourceId);
                }
            }
            foreach (var issue in issues.OrderBy(value => value))
            {
                MicroPatternContractDigest.Append(material, "ISSUE", issue.ToString());
            }
            foreach (var proposal in proposals.OrderBy(value => value))
            {
                MicroPatternContractDigest.Append(material, "PROPOSAL", proposal.ToString());
            }
            foreach (var delta in deltas.OrderBy(value => value.TargetCoordinate.Y)
                         .ThenBy(value => value.TargetCoordinate.X))
            {
                MicroPatternContractDigest.Append(
                    material,
                    "DELTA",
                    Number(delta.TargetCoordinate.X),
                    Number(delta.TargetCoordinate.Y),
                    delta.BeforeSolid ? "SOLID" : "AIR",
                    delta.AfterSolid ? "SOLID" : "AIR",
                    string.Join(",", delta.Rules.Select(value => value.ToString())));
                foreach (var evidence in delta.NeighborhoodEvidence)
                {
                    MicroPatternContractDigest.Append(material, "NEIGHBOR", evidence.ToString());
                }
                foreach (var source in delta.ProtectionEvidence)
                {
                    MicroPatternContractDigest.Append(material, "DELTA_PROTECTION", source.ToString());
                }
            }
            return MicroPatternContractDigest.Hash(material);
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
