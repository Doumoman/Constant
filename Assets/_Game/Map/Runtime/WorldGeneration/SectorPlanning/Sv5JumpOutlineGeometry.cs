using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public sealed class Sv5JumpOutlineDepth : IComparable<Sv5JumpOutlineDepth>
    {
        public Sv5JumpOutlineDepth(string ownerSupportId, int x, int supportBottomY, int depth, int emittedCellCount)
        {
            OwnerSupportId = ownerSupportId ?? string.Empty;
            X = x;
            SupportBottomY = supportBottomY;
            Depth = depth;
            EmittedCellCount = emittedCellCount;
        }

        public string OwnerSupportId { get; }
        public int X { get; }
        public int SupportBottomY { get; }
        public int Depth { get; }
        public int EmittedCellCount { get; }

        public int CompareTo(Sv5JumpOutlineDepth other)
        {
            if (other == null) return 1;
            int owner = string.Compare(OwnerSupportId, other.OwnerSupportId, StringComparison.Ordinal);
            return owner != 0 ? owner : X.CompareTo(other.X);
        }

        internal string DigestToken
        {
            get { return OwnerSupportId + "|" + X + "|" + SupportBottomY + "|" + Depth + "|" + EmittedCellCount; }
        }
    }

    public readonly struct Sv5JumpOutlineCell : IEquatable<Sv5JumpOutlineCell>, IComparable<Sv5JumpOutlineCell>
    {
        public Sv5JumpOutlineCell(string outlineCellId, string ownerSupportId, Sv5JumpPoint point, int depth)
        {
            OutlineCellId = outlineCellId ?? string.Empty;
            OwnerSupportId = ownerSupportId ?? string.Empty;
            Point = point;
            Depth = depth;
        }

        public string OutlineCellId { get; }
        public string OwnerSupportId { get; }
        public Sv5JumpPoint Point { get; }
        public int Depth { get; }
        public string Collision { get { return "SOLID"; } }

        public bool Equals(Sv5JumpOutlineCell other)
        {
            return string.Equals(OutlineCellId, other.OutlineCellId, StringComparison.Ordinal) &&
                string.Equals(OwnerSupportId, other.OwnerSupportId, StringComparison.Ordinal) &&
                Point.Equals(other.Point) && Depth == other.Depth;
        }

        public override bool Equals(object obj)
        {
            return obj is Sv5JumpOutlineCell && Equals((Sv5JumpOutlineCell)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (((OutlineCellId.GetHashCode() * 397) ^ OwnerSupportId.GetHashCode()) * 397 ^
                    Point.GetHashCode()) * 397 ^ Depth;
            }
        }

        public int CompareTo(Sv5JumpOutlineCell other)
        {
            int point = Point.CompareTo(other.Point);
            return point != 0 ? point : string.Compare(OutlineCellId, other.OutlineCellId, StringComparison.Ordinal);
        }

        internal string DigestToken
        {
            get { return OutlineCellId + "|" + OwnerSupportId + "|" + Point + "|" + Depth + "|SOLID"; }
        }
    }

    public sealed class Sv5JumpFinalOccupancyCell : IComparable<Sv5JumpFinalOccupancyCell>
    {
        public Sv5JumpFinalOccupancyCell(
            Sv5JumpPoint point,
            string collision,
            string ownerId,
            string source,
            string supportKind)
        {
            Point = point;
            Collision = collision ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
            Source = source ?? string.Empty;
            SupportKind = supportKind ?? string.Empty;
        }

        public Sv5JumpPoint Point { get; }
        public string Collision { get; }
        public string OwnerId { get; }
        public string Source { get; }
        public string SupportKind { get; }

        public int CompareTo(Sv5JumpFinalOccupancyCell other)
        {
            if (other == null) return 1;
            int point = Point.CompareTo(other.Point);
            return point != 0 ? point : string.Compare(OwnerId, other.OwnerId, StringComparison.Ordinal);
        }

        internal string DigestToken
        {
            get { return Point + "|" + Collision + "|" + OwnerId + "|" + Source + "|" + SupportKind; }
        }
    }

    public static class Sv5JumpOutlineDiagnostic
    {
        public const string CanvasMismatch = "LOCAL_JUMP_ROOM_MUST_BE_24X32";
        public const string BaseDigestMismatch = "SV5_15_BASE_FIXTURE_DIGEST_MISMATCH";
        public const string BaseSupportMismatch = "SV5_15_SUPPORTS_OR_CELLS_CHANGED";
        public const string BaseRouteMismatch = "SV5_15_ROUTE_LINKS_CHANGED";
        public const string BaseGrabMismatch = "SV5_15_GRAB_DATA_CHANGED";
        public const string DepthDuplicate = "DUPLICATE_DEFORMATION_DEPTH_COLUMN";
        public const string DepthMissing = "MISSING_DEFORMATION_DEPTH_COLUMN";
        public const string DepthExtra = "EXTRA_DEFORMATION_DEPTH_COLUMN";
        public const string DepthMismatch = "DEFORMATION_DEPTH_MISMATCH";
        public const string DepthOwnerInvalid = "OUTLINE_OWNER_MUST_BE_SOLID";
        public const string DepthCountMismatch = "EMITTED_CELL_COUNT_MUST_EQUAL_DEPTH";
        public const string OutlineIdInvalid = "OUTLINE_CELL_ID_MISSING_OR_DUPLICATE";
        public const string OutlineDuplicate = "DUPLICATE_OUTLINE_COORDINATE";
        public const string OutlineMissing = "MISSING_OUTLINE_CELL";
        public const string OutlineExtra = "EXTRA_OR_MISMATCHED_OUTLINE_CELL";
        public const string OutlineOutOfBounds = "OUTLINE_CELL_OUT_OF_LOCAL_BOUNDS";
        public const string OutlineOverlap = "OUTLINE_OVERLAPS_BASE_SUPPORT";
        public const string OneWayBacking = "ONE_WAY_BACKING_FORBIDDEN";
        public const string IsolatedOutline = "ISOLATED_OUTLINE_CELL_FORBIDDEN";
        public const string FinalDuplicate = "DUPLICATE_FINAL_OCCUPANCY";
        public const string FinalMismatch = "FINAL_OCCUPANCY_MUST_BE_EXACT_27_PLUS_17_UNION";
        public const string RouteClearanceMissing = "ROUTE_CLEARANCE_MUST_COVER_TEN_SUPPORTS";
        public const string RouteClearanceBlocked = "ROUTE_BODY_OR_HEAD_NOT_AIR";
        public const string GrabClearanceBlocked = "ACCEPTED_GRAB_AIR_BLOCKED";
        public const string AutomaticGrab = "OUTLINE_FACE_AUTOMATIC_GRAB_FORBIDDEN";
        public const string SixBySix = "SOLID_6X6_WINDOW_FORBIDDEN";
        public const string RectangularShell = "RECTANGULAR_ROOM_SHELL_FORBIDDEN";
        public const string PrematurePlayerState = "PLAYER_VERIFIED_FORBIDDEN_BEFORE_SV5_20";
    }

    public sealed class Sv5JumpOutlinePlan
    {
        internal Sv5JumpOutlinePlan(
            int width,
            int height,
            string baseFixtureDigest,
            IEnumerable<Sv5JumpSupport> supports,
            IEnumerable<Sv5JumpSolidCell> supportCells,
            IEnumerable<string> requiredSupportSequence,
            IEnumerable<Sv5JumpGrabRouteLink> routeLinks,
            IEnumerable<Sv5JumpGrabEdge> grabEdges,
            IEnumerable<Sv5JumpGrabClearance> grabClearances,
            IEnumerable<Sv5JumpGrabAction> actions,
            Sv5JumpSolidSocket entrySocket,
            Sv5JumpSolidSocket exitSocket,
            IEnumerable<Sv5JumpOutlineDepth> deformationDepths,
            IEnumerable<Sv5JumpOutlineCell> outlineCells,
            IEnumerable<Sv5JumpFinalOccupancyCell> finalOccupancy,
            IEnumerable<Sv5JumpSolidClearance> routeClearances)
        {
            Width = width;
            Height = height;
            BaseFixtureDigest = baseFixtureDigest ?? string.Empty;
            Supports = Array.AsReadOnly((supports ?? Array.Empty<Sv5JumpSupport>())
                .OrderBy(value => value == null ? string.Empty : value.SupportId, StringComparer.Ordinal).ToArray());
            SupportCells = Array.AsReadOnly((supportCells ?? Array.Empty<Sv5JumpSolidCell>()).OrderBy(value => value).ToArray());
            RequiredSupportSequence = Array.AsReadOnly((requiredSupportSequence ?? Array.Empty<string>()).ToArray());
            RouteLinks = Array.AsReadOnly((routeLinks ?? Array.Empty<Sv5JumpGrabRouteLink>())
                .OrderBy(value => value == null ? int.MaxValue : value.Order).ToArray());
            GrabEdges = Array.AsReadOnly((grabEdges ?? Array.Empty<Sv5JumpGrabEdge>()).OrderBy(value => value).ToArray());
            GrabClearances = Array.AsReadOnly((grabClearances ?? Array.Empty<Sv5JumpGrabClearance>())
                .OrderBy(value => value == null ? string.Empty : value.GrabEdgeId, StringComparer.Ordinal).ToArray());
            Actions = Array.AsReadOnly((actions ?? Array.Empty<Sv5JumpGrabAction>())
                .OrderBy(value => value == null ? int.MaxValue : value.Order).ToArray());
            EntrySocket = entrySocket;
            ExitSocket = exitSocket;
            DeformationDepths = Array.AsReadOnly((deformationDepths ?? Array.Empty<Sv5JumpOutlineDepth>()).OrderBy(value => value).ToArray());
            OutlineCells = Array.AsReadOnly((outlineCells ?? Array.Empty<Sv5JumpOutlineCell>()).OrderBy(value => value).ToArray());
            FinalOccupancy = Array.AsReadOnly((finalOccupancy ?? Array.Empty<Sv5JumpFinalOccupancyCell>()).OrderBy(value => value).ToArray());
            RouteClearances = Array.AsReadOnly((routeClearances ?? Array.Empty<Sv5JumpSolidClearance>())
                .OrderBy(value => value == null ? string.Empty : value.SupportId, StringComparer.Ordinal).ToArray());
            FilledSolidSixBySixWindows = CountSixBySixWindows();
            RectangularRoomShellCreated = ContainsRectangularShell();
            Diagnostics = Array.AsReadOnly(Validate().Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Digest = Hash(string.Join("\n", new[] { BaseFixtureDigest }
                .Concat(Supports.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(SupportCells.Select(value => value.DigestToken))
                .Concat(RequiredSupportSequence.Select((value, index) => index + "|" + value))
                .Concat(RouteLinks.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(GrabEdges.Select(value => value.DigestToken))
                .Concat(GrabClearances.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(Actions.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(DeformationDepths.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(OutlineCells.Select(value => value.DigestToken))
                .Concat(FinalOccupancy.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(RouteClearances.Where(value => value != null).Select(value => value.DigestToken))));
        }

        public int Width { get; }
        public int Height { get; }
        public string BaseFixtureDigest { get; }
        public IReadOnlyList<Sv5JumpSupport> Supports { get; }
        public IReadOnlyList<Sv5JumpSolidCell> SupportCells { get; }
        public IReadOnlyList<string> RequiredSupportSequence { get; }
        public IReadOnlyList<Sv5JumpGrabRouteLink> RouteLinks { get; }
        public IReadOnlyList<Sv5JumpGrabEdge> GrabEdges { get; }
        public IReadOnlyList<Sv5JumpGrabClearance> GrabClearances { get; }
        public IReadOnlyList<Sv5JumpGrabAction> Actions { get; }
        public Sv5JumpSolidSocket EntrySocket { get; }
        public Sv5JumpSolidSocket ExitSocket { get; }
        public IReadOnlyList<Sv5JumpOutlineDepth> DeformationDepths { get; }
        public IReadOnlyList<Sv5JumpOutlineCell> OutlineCells { get; }
        public IReadOnlyList<Sv5JumpFinalOccupancyCell> FinalOccupancy { get; }
        public IReadOnlyList<Sv5JumpSolidClearance> RouteClearances { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public int FilledSolidSixBySixWindows { get; }
        public bool RectangularRoomShellCreated { get; }
        public bool OutlineFacesAutomaticallyGrabbable { get { return false; } }
        public bool ReverseCompletionRequired { get { return false; } }
        public int WholeWorldBuildsInNewTargetedTests { get { return 0; } }
        public int WholeWorldSearchesInNewTargetedTests { get { return 0; } }
        public bool JumpContractReady { get { return true; } }
        public bool JumpSolidGeometryReady { get { return true; } }
        public bool JumpGrabGeometryReady { get { return true; } }
        public bool JumpOutlineReady { get { return Diagnostics.Count == 0; } }
        public bool ComposedGeometryReady { get { return false; } }
        public bool PlayerVerified { get { return false; } }

        private IEnumerable<string> Validate()
        {
            if (Width != Sv5JumpOutlineGeometry.LocalCanvasWidth || Height != Sv5JumpOutlineGeometry.LocalCanvasHeight)
                yield return Sv5JumpOutlineDiagnostic.CanvasMismatch;

            Sv5JumpGrabPlan canonical = Sv5JumpGrabGeometry.CreateCanonicalLocalFixture();
            if (!string.Equals(BaseFixtureDigest, canonical.Digest, StringComparison.Ordinal))
                yield return Sv5JumpOutlineDiagnostic.BaseDigestMismatch;
            if (!SupportsMatch(Supports, canonical.Supports) || !SupportCells.SequenceEqual(canonical.SupportCells) ||
                !RequiredSupportSequence.SequenceEqual(canonical.RequiredSupportSequence, StringComparer.Ordinal) ||
                !SocketMatches(EntrySocket, canonical.EntrySocket) || !SocketMatches(ExitSocket, canonical.ExitSocket))
                yield return Sv5JumpOutlineDiagnostic.BaseSupportMismatch;
            if (!Tokens(RouteLinks).SequenceEqual(Tokens(canonical.RouteLinks), StringComparer.Ordinal))
                yield return Sv5JumpOutlineDiagnostic.BaseRouteMismatch;
            if (!Tokens(GrabEdges).SequenceEqual(Tokens(canonical.GrabEdges), StringComparer.Ordinal) ||
                !Tokens(GrabClearances).SequenceEqual(Tokens(canonical.Clearances), StringComparer.Ordinal) ||
                !Tokens(Actions).SequenceEqual(Tokens(canonical.Actions), StringComparer.Ordinal))
                yield return Sv5JumpOutlineDiagnostic.BaseGrabMismatch;

            var supportsById = Supports.Where(value => value != null)
                .GroupBy(value => value.SupportId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var expectedDepths = Sv5JumpOutlineGeometry.ExactDepthProfile().ToDictionary(
                value => value.OwnerSupportId + "|" + value.X, StringComparer.Ordinal);
            var actualDepths = new Dictionary<string, Sv5JumpOutlineDepth>(StringComparer.Ordinal);
            foreach (Sv5JumpOutlineDepth row in DeformationDepths)
            {
                if (row == null) continue;
                string key = row.OwnerSupportId + "|" + row.X;
                if (actualDepths.ContainsKey(key))
                    yield return Sv5JumpOutlineDiagnostic.DepthDuplicate + ":" + key;
                else
                    actualDepths.Add(key, row);
                Sv5JumpSupport support;
                if (!supportsById.TryGetValue(row.OwnerSupportId, out support) ||
                    support.Kind != Sv5JumpSupportKind.Solid || row.X < support.TopXMin || row.X > support.TopXMax)
                    yield return Sv5JumpOutlineDiagnostic.DepthOwnerInvalid + ":" + key;
                else if (row.SupportBottomY != support.Y)
                    yield return Sv5JumpOutlineDiagnostic.DepthMismatch + ":" + key;
                if (row.EmittedCellCount != row.Depth)
                    yield return Sv5JumpOutlineDiagnostic.DepthCountMismatch + ":" + key;
            }
            foreach (KeyValuePair<string, Sv5JumpOutlineDepth> expected in expectedDepths)
            {
                Sv5JumpOutlineDepth actual;
                if (!actualDepths.TryGetValue(expected.Key, out actual))
                    yield return Sv5JumpOutlineDiagnostic.DepthMissing + ":" + expected.Key;
                else if (actual.SupportBottomY != expected.Value.SupportBottomY || actual.Depth != expected.Value.Depth ||
                    actual.EmittedCellCount != expected.Value.EmittedCellCount)
                    yield return Sv5JumpOutlineDiagnostic.DepthMismatch + ":" + expected.Key;
            }
            foreach (string extra in actualDepths.Keys.Except(expectedDepths.Keys, StringComparer.Ordinal))
                yield return Sv5JumpOutlineDiagnostic.DepthExtra + ":" + extra;

            Sv5JumpOutlineCell[] expectedCells = Sv5JumpOutlineGeometry.EmitOutlineCells(
                Sv5JumpOutlineGeometry.ExactDepthProfile()).ToArray();
            var expectedByPoint = expectedCells.ToDictionary(value => value.Point);
            var actualByPoint = new Dictionary<Sv5JumpPoint, Sv5JumpOutlineCell>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var basePoints = new HashSet<Sv5JumpPoint>(SupportCells.Select(value => value.Point));
            foreach (Sv5JumpOutlineCell cell in OutlineCells)
            {
                if (string.IsNullOrWhiteSpace(cell.OutlineCellId) || !ids.Add(cell.OutlineCellId))
                    yield return Sv5JumpOutlineDiagnostic.OutlineIdInvalid + ":" + cell.OutlineCellId;
                if (actualByPoint.ContainsKey(cell.Point))
                    yield return Sv5JumpOutlineDiagnostic.OutlineDuplicate + ":" + cell.Point;
                else
                    actualByPoint.Add(cell.Point, cell);
                if (cell.Point.X < 0 || cell.Point.X >= Width || cell.Point.Y < 0 || cell.Point.Y >= Height)
                    yield return Sv5JumpOutlineDiagnostic.OutlineOutOfBounds + ":" + cell.Point;
                if (basePoints.Contains(cell.Point))
                    yield return Sv5JumpOutlineDiagnostic.OutlineOverlap + ":" + cell.Point;
                Sv5JumpSupport owner;
                if (!supportsById.TryGetValue(cell.OwnerSupportId, out owner) || owner.Kind != Sv5JumpSupportKind.Solid)
                    yield return Sv5JumpOutlineDiagnostic.OneWayBacking + ":" + cell.OwnerSupportId;
            }
            foreach (KeyValuePair<Sv5JumpPoint, Sv5JumpOutlineCell> expected in expectedByPoint)
            {
                Sv5JumpOutlineCell actual;
                if (!actualByPoint.TryGetValue(expected.Key, out actual))
                    yield return Sv5JumpOutlineDiagnostic.OutlineMissing + ":" + expected.Key;
                else if (!actual.Equals(expected.Value))
                    yield return Sv5JumpOutlineDiagnostic.OutlineExtra + ":" + expected.Key;
            }
            foreach (Sv5JumpPoint extra in actualByPoint.Keys.Except(expectedByPoint.Keys))
                yield return Sv5JumpOutlineDiagnostic.OutlineExtra + ":" + extra;
            foreach (Sv5JumpOutlineCell cell in OutlineCells)
            {
                var above = new Sv5JumpPoint(cell.Point.X, cell.Point.Y + 1);
                bool attached = basePoints.Contains(above) || OutlineCells.Any(value =>
                    value.Point.Equals(above) && string.Equals(value.OwnerSupportId, cell.OwnerSupportId, StringComparison.Ordinal));
                if (!attached)
                    yield return Sv5JumpOutlineDiagnostic.IsolatedOutline + ":" + cell.Point;
            }

            var expectedFinal = Sv5JumpOutlineGeometry.BuildFinalOccupancy(canonical.SupportCells, expectedCells)
                .ToDictionary(value => value.Point);
            var actualFinal = new Dictionary<Sv5JumpPoint, Sv5JumpFinalOccupancyCell>();
            foreach (Sv5JumpFinalOccupancyCell cell in FinalOccupancy.Where(value => value != null))
            {
                if (actualFinal.ContainsKey(cell.Point))
                    yield return Sv5JumpOutlineDiagnostic.FinalDuplicate + ":" + cell.Point;
                else
                    actualFinal.Add(cell.Point, cell);
            }
            if (actualFinal.Count != expectedFinal.Count || expectedFinal.Any(pair =>
                !actualFinal.ContainsKey(pair.Key) || !FinalCellMatches(actualFinal[pair.Key], pair.Value)))
                yield return Sv5JumpOutlineDiagnostic.FinalMismatch;

            if (RouteClearances.Count != canonical.Supports.Count ||
                RouteClearances.Select(value => value == null ? string.Empty : value.SupportId)
                    .Distinct(StringComparer.Ordinal).Count() != canonical.Supports.Count)
                yield return Sv5JumpOutlineDiagnostic.RouteClearanceMissing;
            foreach (Sv5JumpSolidClearance clearance in RouteClearances.Where(value => value != null))
            {
                Sv5JumpSupport support;
                bool valid = supportsById.TryGetValue(clearance.SupportId, out support) &&
                    support.SupportsFeet(clearance.Foot) && clearance.Body.Equals(clearance.Foot) &&
                    clearance.Head.Equals(new Sv5JumpPoint(clearance.Foot.X, clearance.Foot.Y + 1)) &&
                    InBounds(clearance.Body) && InBounds(clearance.Head) &&
                    !actualFinal.ContainsKey(clearance.Body) && !actualFinal.ContainsKey(clearance.Head) && clearance.Clear;
                if (!valid)
                    yield return Sv5JumpOutlineDiagnostic.RouteClearanceBlocked + ":" + clearance.SupportId;
            }

            foreach (Sv5JumpPoint point in new[]
            {
                new Sv5JumpPoint(16, 6), new Sv5JumpPoint(16, 7),
                new Sv5JumpPoint(15, 7), new Sv5JumpPoint(15, 8)
            })
                if (actualFinal.ContainsKey(point))
                    yield return Sv5JumpOutlineDiagnostic.GrabClearanceBlocked + ":" + point;

            if (GrabEdges.Count != 1 || !string.Equals(GrabEdges[0].GrabEdgeId, Sv5JumpGrabGeometry.GrabEdgeId, StringComparison.Ordinal))
                yield return Sv5JumpOutlineDiagnostic.AutomaticGrab;
            if (FilledSolidSixBySixWindows != 0)
                yield return Sv5JumpOutlineDiagnostic.SixBySix;
            if (RectangularRoomShellCreated)
                yield return Sv5JumpOutlineDiagnostic.RectangularShell;
            if (Supports.Any(value => value != null && value.ValidationState == Sv5JumpValidationState.PlayerVerified) ||
                RouteLinks.Any(value => value != null && value.ValidationState == Sv5JumpValidationState.PlayerVerified))
                yield return Sv5JumpOutlineDiagnostic.PrematurePlayerState;
        }

        private bool InBounds(Sv5JumpPoint point)
        {
            return point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height;
        }

        private int CountSixBySixWindows()
        {
            var solid = new HashSet<Sv5JumpPoint>(FinalOccupancy.Where(value => value != null && value.Collision == "SOLID")
                .Select(value => value.Point));
            int count = 0;
            for (int y = 0; y <= Height - 6; y++)
                for (int x = 0; x <= Width - 6; x++)
                    if (Enumerable.Range(0, 6).All(dy => Enumerable.Range(0, 6)
                        .All(dx => solid.Contains(new Sv5JumpPoint(x + dx, y + dy)))))
                        count++;
            return count;
        }

        private bool ContainsRectangularShell()
        {
            var solid = new HashSet<Sv5JumpPoint>(FinalOccupancy.Where(value => value != null && value.Collision == "SOLID")
                .Select(value => value.Point));
            for (int y0 = 0; y0 < Height - 2; y0++)
                for (int y1 = y0 + 2; y1 < Height; y1++)
                    for (int x0 = 0; x0 < Width - 2; x0++)
                        for (int x1 = x0 + 2; x1 < Width; x1++)
                        {
                            bool perimeter = true;
                            for (int x = x0; x <= x1 && perimeter; x++)
                                perimeter = solid.Contains(new Sv5JumpPoint(x, y0)) && solid.Contains(new Sv5JumpPoint(x, y1));
                            for (int y = y0; y <= y1 && perimeter; y++)
                                perimeter = solid.Contains(new Sv5JumpPoint(x0, y)) && solid.Contains(new Sv5JumpPoint(x1, y));
                            if (perimeter) return true;
                        }
            return false;
        }

        private static bool SupportsMatch(IEnumerable<Sv5JumpSupport> first, IEnumerable<Sv5JumpSupport> second)
        {
            string[] a = first.Where(value => value != null).Select(value => value.DigestToken).OrderBy(value => value).ToArray();
            string[] b = second.Where(value => value != null).Select(value => value.DigestToken).OrderBy(value => value).ToArray();
            return a.SequenceEqual(b, StringComparer.Ordinal);
        }

        private static bool SocketMatches(Sv5JumpSolidSocket first, Sv5JumpSolidSocket second)
        {
            return first != null && second != null && string.Equals(first.SocketId, second.SocketId, StringComparison.Ordinal) &&
                string.Equals(first.SupportId, second.SupportId, StringComparison.Ordinal) && first.Foot.Equals(second.Foot);
        }

        private static bool FinalCellMatches(Sv5JumpFinalOccupancyCell first, Sv5JumpFinalOccupancyCell second)
        {
            return first.Point.Equals(second.Point) && first.Collision == second.Collision &&
                first.OwnerId == second.OwnerId && first.Source == second.Source && first.SupportKind == second.SupportKind;
        }

        private static IEnumerable<string> Tokens(IEnumerable<Sv5JumpGrabRouteLink> values)
        {
            return values.Where(value => value != null).OrderBy(value => value.Order).Select(value => value.DigestToken);
        }

        private static IEnumerable<string> Tokens(IEnumerable<Sv5JumpGrabEdge> values)
        {
            return values.OrderBy(value => value).Select(value => value.DigestToken);
        }

        private static IEnumerable<string> Tokens(IEnumerable<Sv5JumpGrabClearance> values)
        {
            return values.Where(value => value != null).OrderBy(value => value.GrabEdgeId).Select(value => value.DigestToken);
        }

        private static IEnumerable<string> Tokens(IEnumerable<Sv5JumpGrabAction> values)
        {
            return values.Where(value => value != null).OrderBy(value => value.Order).Select(value => value.DigestToken);
        }

        private static string Hash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    public static class Sv5JumpOutlineGeometry
    {
        public const int LocalCanvasWidth = 24;
        public const int LocalCanvasHeight = 32;

        public static Sv5JumpOutlinePlan CreateCanonicalLocalFixture()
        {
            Sv5JumpGrabPlan basePlan = Sv5JumpGrabGeometry.CreateCanonicalLocalFixture();
            Sv5JumpOutlineDepth[] depths = ExactDepthProfile().ToArray();
            Sv5JumpOutlineCell[] outline = EmitOutlineCells(depths).ToArray();
            Sv5JumpFinalOccupancyCell[] final = BuildFinalOccupancy(basePlan.SupportCells, outline).ToArray();
            Sv5JumpSolidClearance[] routeClearance = BuildRouteClearances(
                basePlan.Supports, basePlan.RequiredSupportSequence, final, LocalCanvasWidth, LocalCanvasHeight).ToArray();
            return BuildLocal(basePlan, depths, outline, final, routeClearance);
        }

        public static Sv5JumpOutlinePlan BuildLocal(
            Sv5JumpGrabPlan basePlan,
            IEnumerable<Sv5JumpOutlineDepth> deformationDepths,
            IEnumerable<Sv5JumpOutlineCell> outlineCells = null,
            IEnumerable<Sv5JumpFinalOccupancyCell> finalOccupancy = null,
            IEnumerable<Sv5JumpSolidClearance> routeClearances = null,
            IEnumerable<Sv5JumpSupport> supports = null,
            IEnumerable<Sv5JumpSolidCell> supportCells = null,
            IEnumerable<string> requiredSupportSequence = null,
            IEnumerable<Sv5JumpGrabRouteLink> routeLinks = null,
            IEnumerable<Sv5JumpGrabEdge> grabEdges = null,
            IEnumerable<Sv5JumpGrabClearance> grabClearances = null,
            IEnumerable<Sv5JumpGrabAction> actions = null,
            string baseFixtureDigest = null,
            int width = LocalCanvasWidth,
            int height = LocalCanvasHeight)
        {
            if (basePlan == null) throw new ArgumentNullException(nameof(basePlan));
            Sv5JumpOutlineDepth[] depthArray = (deformationDepths ?? Array.Empty<Sv5JumpOutlineDepth>()).ToArray();
            Sv5JumpSupport[] supportArray = (supports ?? basePlan.Supports).ToArray();
            Sv5JumpSolidCell[] baseCellArray = (supportCells ?? basePlan.SupportCells).ToArray();
            Sv5JumpOutlineCell[] outlineArray = (outlineCells ?? EmitOutlineCells(depthArray)).ToArray();
            Sv5JumpFinalOccupancyCell[] finalArray = (finalOccupancy ?? BuildFinalOccupancy(baseCellArray, outlineArray)).ToArray();
            string[] sequence = (requiredSupportSequence ?? basePlan.RequiredSupportSequence).ToArray();
            Sv5JumpSolidClearance[] clearances = (routeClearances ??
                BuildRouteClearances(supportArray, sequence, finalArray, width, height)).ToArray();
            return new Sv5JumpOutlinePlan(
                width, height, baseFixtureDigest ?? basePlan.Digest, supportArray, baseCellArray, sequence,
                routeLinks ?? basePlan.RouteLinks, grabEdges ?? basePlan.GrabEdges,
                grabClearances ?? basePlan.Clearances, actions ?? basePlan.Actions,
                basePlan.EntrySocket, basePlan.ExitSocket, depthArray, outlineArray, finalArray, clearances);
        }

        public static IEnumerable<Sv5JumpOutlineDepth> ExactDepthProfile()
        {
            Sv5JumpGrabPlan plan = Sv5JumpGrabGeometry.CreateCanonicalLocalFixture();
            var supports = plan.Supports.ToDictionary(value => value.SupportId, StringComparer.Ordinal);
            foreach (Tuple<string, int, int> value in new[]
            {
                Tuple.Create("JS00_ENTRY_SOLID", 0, 1), Tuple.Create("JS00_ENTRY_SOLID", 1, 1),
                Tuple.Create("JS00_ENTRY_SOLID", 2, 0), Tuple.Create("JS02_SOLID", 12, 1),
                Tuple.Create("JS02_SOLID", 13, 2), Tuple.Create("JS02_SOLID", 14, 1),
                Tuple.Create("JS04_SOLID", 14, 2), Tuple.Create("JS04_SOLID", 15, 1),
                Tuple.Create("JS06_SOLID", 4, 2), Tuple.Create("JS06_SOLID", 5, 2),
                Tuple.Create("JS06_SOLID", 6, 1), Tuple.Create("JS08_SOLID", 3, 2),
                Tuple.Create("JS08_SOLID", 4, 1)
            })
                yield return new Sv5JumpOutlineDepth(value.Item1, value.Item2, supports[value.Item1].Y,
                    value.Item3, value.Item3);
        }

        public static IEnumerable<Sv5JumpOutlineCell> EmitOutlineCells(IEnumerable<Sv5JumpOutlineDepth> depths)
        {
            foreach (Sv5JumpOutlineDepth row in (depths ?? Array.Empty<Sv5JumpOutlineDepth>())
                .Where(value => value != null).OrderBy(value => value))
                for (int amount = 1; amount <= row.Depth; amount++)
                    yield return new Sv5JumpOutlineCell(
                        "OL_" + row.OwnerSupportId + "_X" + row.X.ToString("00") + "_D" + amount.ToString("00"),
                        row.OwnerSupportId,
                        new Sv5JumpPoint(row.X, row.SupportBottomY - amount),
                        amount);
        }

        public static IEnumerable<Sv5JumpFinalOccupancyCell> BuildFinalOccupancy(
            IEnumerable<Sv5JumpSolidCell> supportCells,
            IEnumerable<Sv5JumpOutlineCell> outlineCells)
        {
            foreach (Sv5JumpSolidCell cell in supportCells ?? Array.Empty<Sv5JumpSolidCell>())
                yield return new Sv5JumpFinalOccupancyCell(cell.Point, cell.Collision, cell.SupportId,
                    "BASE_SUPPORT", Sv5JumpSolidGeometry.KindName(cell.Kind));
            foreach (Sv5JumpOutlineCell cell in outlineCells ?? Array.Empty<Sv5JumpOutlineCell>())
                yield return new Sv5JumpFinalOccupancyCell(cell.Point, "SOLID", cell.OwnerSupportId,
                    "OUTLINE_BACKING", "SOLID");
        }

        public static IEnumerable<Sv5JumpSolidClearance> BuildRouteClearances(
            IEnumerable<Sv5JumpSupport> supports,
            IEnumerable<string> sequence,
            IEnumerable<Sv5JumpFinalOccupancyCell> finalOccupancy,
            int width,
            int height)
        {
            var byId = (supports ?? Array.Empty<Sv5JumpSupport>()).Where(value => value != null)
                .GroupBy(value => value.SupportId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var occupied = new HashSet<Sv5JumpPoint>((finalOccupancy ?? Array.Empty<Sv5JumpFinalOccupancyCell>())
                .Where(value => value != null).Select(value => value.Point));
            foreach (string supportId in sequence ?? Array.Empty<string>())
            {
                Sv5JumpSupport support;
                if (!byId.TryGetValue(supportId, out support)) continue;
                var selected = new Sv5JumpPoint(support.TopXMin, support.TopY);
                bool clear = false;
                for (int x = support.TopXMin; x <= support.TopXMax; x++)
                {
                    var body = new Sv5JumpPoint(x, support.TopY);
                    var head = new Sv5JumpPoint(x, support.TopY + 1);
                    if (x >= 0 && x < width && head.Y >= 0 && head.Y < height &&
                        !occupied.Contains(body) && !occupied.Contains(head))
                    {
                        selected = body;
                        clear = true;
                        break;
                    }
                }
                yield return new Sv5JumpSolidClearance(supportId, selected, selected,
                    new Sv5JumpPoint(selected.X, selected.Y + 1), clear);
            }
        }
    }
}
