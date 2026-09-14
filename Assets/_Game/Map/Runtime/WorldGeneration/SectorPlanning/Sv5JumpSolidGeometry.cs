using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public readonly struct Sv5JumpSolidCell : IEquatable<Sv5JumpSolidCell>, IComparable<Sv5JumpSolidCell>
    {
        public Sv5JumpSolidCell(string supportId, Sv5JumpPoint point, Sv5JumpSupportKind kind)
        {
            if (string.IsNullOrWhiteSpace(supportId))
            {
                throw new ArgumentException("A support cell requires a stable support ID.", nameof(supportId));
            }

            SupportId = supportId;
            Point = point;
            Kind = kind;
        }

        public string SupportId { get; }
        public Sv5JumpPoint Point { get; }
        public Sv5JumpSupportKind Kind { get; }
        public string Collision { get { return Kind == Sv5JumpSupportKind.Solid ? "SOLID" : "TOP_ONLY"; } }

        public bool Equals(Sv5JumpSolidCell other)
        {
            return string.Equals(SupportId, other.SupportId, StringComparison.Ordinal) &&
                Point.Equals(other.Point) && Kind == other.Kind;
        }

        public override bool Equals(object obj)
        {
            return obj is Sv5JumpSolidCell && Equals((Sv5JumpSolidCell)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((SupportId.GetHashCode() * 397) ^ Point.GetHashCode()) * 397 ^ (int)Kind;
            }
        }

        public int CompareTo(Sv5JumpSolidCell other)
        {
            int y = Point.Y.CompareTo(other.Point.Y);
            if (y != 0)
            {
                return y;
            }

            int x = Point.X.CompareTo(other.Point.X);
            return x != 0 ? x : string.Compare(SupportId, other.SupportId, StringComparison.Ordinal);
        }

        internal string DigestToken
        {
            get { return SupportId + "|" + Point + "|" + Sv5JumpSolidGeometry.KindName(Kind) + "|" + Collision; }
        }
    }

    public sealed class Sv5JumpSolidSocket
    {
        public Sv5JumpSolidSocket(string socketId, string supportId, Sv5JumpPoint foot)
        {
            if (string.IsNullOrWhiteSpace(socketId) || string.IsNullOrWhiteSpace(supportId))
            {
                throw new ArgumentException("A socket requires stable socket and support IDs.");
            }

            SocketId = socketId;
            SupportId = supportId;
            Foot = foot;
        }

        public string SocketId { get; }
        public string SupportId { get; }
        public Sv5JumpPoint Foot { get; }
        internal string DigestToken { get { return SocketId + "|" + SupportId + "|" + Foot; } }
    }

    public sealed class Sv5JumpSolidRouteLink
    {
        public Sv5JumpSolidRouteLink(
            int order,
            string linkId,
            Sv5JumpSupport source,
            Sv5JumpSupport target,
            Sv5JumpDirection direction,
            Sv5JumpPoint takeoff,
            Sv5JumpPoint landing,
            bool requiredRoute,
            int? declaredGapAir = null,
            int? declaredRise = null)
        {
            Order = order;
            LinkId = linkId;
            Source = source;
            Target = target;
            Direction = direction;
            Takeoff = takeoff;
            Landing = landing;
            RequiredRoute = requiredRoute;
            Measurement = Sv5JumpContract.Measure(
                linkId,
                source,
                target,
                direction,
                takeoff,
                landing,
                Sv5JumpMode.Jump,
                null,
                Sv5JumpValidationState.Planned,
                declaredGapAir,
                declaredRise);
        }

        public int Order { get; }
        public string LinkId { get; }
        public Sv5JumpSupport Source { get; }
        public Sv5JumpSupport Target { get; }
        public Sv5JumpDirection Direction { get; }
        public Sv5JumpPoint Takeoff { get; }
        public Sv5JumpPoint Landing { get; }
        public bool RequiredRoute { get; }
        public Sv5JumpMode Mode { get { return Sv5JumpMode.Jump; } }
        public Sv5JumpValidationState ValidationState { get { return Sv5JumpValidationState.Planned; } }
        public Sv5JumpMeasurementResult Measurement { get; }
        public bool ContractAccepted { get { return Measurement != null && Measurement.Success; } }
        public int GapAir { get { return Measurement == null || Measurement.Proof == null ? int.MinValue : Measurement.Proof.ComputedGapAir; } }
        public int Rise { get { return Measurement == null || Measurement.Proof == null ? int.MinValue : Measurement.Proof.ComputedRise; } }

        internal string DigestToken
        {
            get
            {
                return Order + "|" + LinkId + "|" + (Source == null ? string.Empty : Source.SupportId) + "|" +
                    (Target == null ? string.Empty : Target.SupportId) + "|" + Direction + "|" + Takeoff + "|" +
                    Landing + "|" + GapAir + "|" + Rise + "|JUMP|" + RequiredRoute + "|" + ContractAccepted;
            }
        }
    }

    public sealed class Sv5JumpSolidClearance
    {
        public Sv5JumpSolidClearance(
            string supportId,
            Sv5JumpPoint foot,
            Sv5JumpPoint body,
            Sv5JumpPoint head,
            bool clear)
        {
            SupportId = supportId;
            Foot = foot;
            Body = body;
            Head = head;
            Clear = clear;
        }

        public string SupportId { get; }
        public Sv5JumpPoint Foot { get; }
        public Sv5JumpPoint Body { get; }
        public Sv5JumpPoint Head { get; }
        public bool Clear { get; }
        internal string DigestToken { get { return SupportId + "|" + Foot + "|" + Body + "|" + Head + "|" + Clear; } }
    }

    public sealed class Sv5JumpSolidUse
    {
        internal Sv5JumpSolidUse(string supportId, int takeoffs, int landings, bool counted)
        {
            SupportId = supportId;
            AsTakeoffCount = takeoffs;
            AsLandingCount = landings;
            CountedActiveSolid = counted;
        }

        public string SupportId { get; }
        public int AsTakeoffCount { get; }
        public int AsLandingCount { get; }
        public bool CountedActiveSolid { get; }
        internal string DigestToken { get { return SupportId + "|" + AsTakeoffCount + "|" + AsLandingCount + "|" + CountedActiveSolid; } }
    }

    public static class Sv5JumpSolidDiagnostic
    {
        public const string CanvasMismatch = "LOCAL_CANVAS_MUST_BE_24X32";
        public const string DuplicateSupport = "DUPLICATE_SUPPORT_ID";
        public const string InvalidSupportSize = "SUPPORT_SIZE_OUTSIDE_PROFILE";
        public const string SupportOutOfBounds = "SUPPORT_OUT_OF_LOCAL_BOUNDS";
        public const string SupportOverlap = "SUPPORT_CELL_OVERLAP";
        public const string MissingSupportCell = "MISSING_SUPPORT_CELL";
        public const string ExtraSupportCell = "EXTRA_OR_MISMATCHED_SUPPORT_CELL";
        public const string CellOutOfBounds = "EMITTED_SUPPORT_CELL_OUT_OF_BOUNDS";
        public const string SolidSixBySix = "SOLID_6X6_WINDOW_FORBIDDEN";
        public const string TooFewRouteSupports = "REQUIRED_ROUTE_SUPPORT_COUNT_BELOW_TEN";
        public const string TooFewSolids = "ACTIVE_REQUIRED_SOLID_COUNT_BELOW_FOUR";
        public const string TooFewOneWays = "ACTIVE_REQUIRED_ONE_WAY_COUNT_BELOW_THREE";
        public const string RequiredSupportInvalid = "REQUIRED_SUPPORT_INACTIVE_OR_DECORATIVE";
        public const string RouteSequenceInvalid = "REQUIRED_SUPPORT_SEQUENCE_INVALID";
        public const string RouteOrderBroken = "ORDERED_ROUTE_LINK_MISMATCH";
        public const string RouteLinkCount = "ROUTE_LINK_COUNT_MISMATCH";
        public const string ContractRejected = "SV5_13_CONTRACT_REJECTED";
        public const string GapOutOfRange = "REQUIRED_ROUTE_GAP_AIR_OUTSIDE_ZERO_TO_THREE";
        public const string RiseOutOfRange = "REQUIRED_ROUTE_RISE_OUTSIDE_ZERO_TO_ONE";
        public const string UnsupportedTakeoff = "TAKEOFF_NOT_ON_SOURCE_TOP";
        public const string UnsupportedLanding = "LANDING_NOT_ON_TARGET_TOP";
        public const string VerticalSpan = "ROUTE_VERTICAL_SPAN_BELOW_EIGHT";
        public const string SolidNotUsed = "COUNTED_SOLID_NOT_USED_BY_ROUTE";
        public const string MissingSolidDirection = "ROUTE_MUST_LAND_ON_AND_DEPART_FROM_SOLID";
        public const string ClearanceMissing = "REQUIRED_SUPPORT_CLEARANCE_MISSING";
        public const string ClearanceBlocked = "RESERVED_CLEARANCE_BLOCKED";
        public const string SocketMismatch = "ENTRY_EXIT_SOCKET_MISMATCH";
        public const string PrematurePlayerState = "PLAYER_VERIFIED_FORBIDDEN_BEFORE_SV5_20";
    }

    public sealed class Sv5JumpSolidPlan
    {
        internal Sv5JumpSolidPlan(
            int width,
            int height,
            IEnumerable<Sv5JumpSupport> supports,
            IEnumerable<Sv5JumpSolidCell> supportCells,
            IEnumerable<string> requiredSupportSequence,
            IEnumerable<Sv5JumpSolidRouteLink> routeLinks,
            IEnumerable<Sv5JumpSolidClearance> clearances,
            Sv5JumpSolidSocket entrySocket,
            Sv5JumpSolidSocket exitSocket)
        {
            Width = width;
            Height = height;
            Supports = Array.AsReadOnly((supports ?? Array.Empty<Sv5JumpSupport>())
                .OrderBy(value => value == null ? string.Empty : value.SupportId, StringComparer.Ordinal).ToArray());
            SupportCells = Array.AsReadOnly((supportCells ?? Array.Empty<Sv5JumpSolidCell>()).OrderBy(value => value).ToArray());
            RequiredSupportSequence = Array.AsReadOnly((requiredSupportSequence ?? Array.Empty<string>()).ToArray());
            RouteLinks = Array.AsReadOnly((routeLinks ?? Array.Empty<Sv5JumpSolidRouteLink>())
                .OrderBy(value => value == null ? int.MaxValue : value.Order).ToArray());
            Clearances = Array.AsReadOnly((clearances ?? Array.Empty<Sv5JumpSolidClearance>())
                .OrderBy(value => value == null ? string.Empty : value.SupportId, StringComparer.Ordinal).ToArray());
            EntrySocket = entrySocket;
            ExitSocket = exitSocket;
            SolidUses = Array.AsReadOnly(BuildSolidUses().ToArray());
            Diagnostics = Array.AsReadOnly(Validate().Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Digest = Hash(string.Join("\n", Supports.Select(SupportToken)
                .Concat(SupportCells.Select(value => value.DigestToken))
                .Concat(RequiredSupportSequence.Select((value, index) => index + "|" + value))
                .Concat(RouteLinks.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(Clearances.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(SolidUses.Select(value => value.DigestToken))
                .Concat(new[]
                {
                    EntrySocket == null ? string.Empty : EntrySocket.DigestToken,
                    ExitSocket == null ? string.Empty : ExitSocket.DigestToken
                })));
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Sv5JumpSupport> Supports { get; }
        public IReadOnlyList<Sv5JumpSolidCell> SupportCells { get; }
        public IReadOnlyList<string> RequiredSupportSequence { get; }
        public IReadOnlyList<Sv5JumpSolidRouteLink> RouteLinks { get; }
        public IReadOnlyList<Sv5JumpSolidClearance> Clearances { get; }
        public IReadOnlyList<Sv5JumpSolidUse> SolidUses { get; }
        public Sv5JumpSolidSocket EntrySocket { get; }
        public Sv5JumpSolidSocket ExitSocket { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public bool ReverseCompletionRequired { get { return false; } }
        public int GrabEdgeCount { get { return 0; } }
        public int WholeWorldBuildsInNewTargetedTests { get { return 0; } }
        public int WholeWorldSearchesInNewTargetedTests { get { return 0; } }
        public bool JumpContractReady { get { return true; } }
        public bool JumpSolidGeometryReady { get { return Diagnostics.Count == 0; } }
        public bool JumpGrabGeometryReady { get { return false; } }
        public bool ComposedGeometryReady { get { return false; } }
        public bool PlayerVerified { get { return false; } }
        public int RouteVerticalSpan
        {
            get
            {
                var byId = Supports.Where(value => value != null)
                    .GroupBy(value => value.SupportId, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
                if (RequiredSupportSequence.Count == 0 ||
                    !byId.ContainsKey(RequiredSupportSequence[0]) ||
                    !byId.ContainsKey(RequiredSupportSequence[RequiredSupportSequence.Count - 1]))
                {
                    return 0;
                }

                return byId[RequiredSupportSequence[RequiredSupportSequence.Count - 1]].TopY -
                    byId[RequiredSupportSequence[0]].TopY;
            }
        }

        public bool RequiredRouteContains(string supportId)
        {
            return RequiredSupportSequence.Contains(supportId, StringComparer.Ordinal);
        }

        private IEnumerable<Sv5JumpSolidUse> BuildSolidUses()
        {
            var required = new HashSet<string>(RequiredSupportSequence, StringComparer.Ordinal);
            foreach (Sv5JumpSupport support in Supports.Where(value => value != null &&
                value.Kind == Sv5JumpSupportKind.Solid && required.Contains(value.SupportId)))
            {
                int takeoffs = RouteLinks.Count(value => value != null && value.Source != null &&
                    string.Equals(value.Source.SupportId, support.SupportId, StringComparison.Ordinal));
                int landings = RouteLinks.Count(value => value != null && value.Target != null &&
                    string.Equals(value.Target.SupportId, support.SupportId, StringComparison.Ordinal));
                yield return new Sv5JumpSolidUse(support.SupportId, takeoffs, landings,
                    support.ActiveRoute && !support.DecorativeOnly && takeoffs + landings > 0);
            }
        }

        private IEnumerable<string> Validate()
        {
            if (Width != Sv5JumpSolidGeometry.LocalCanvasWidth || Height != Sv5JumpSolidGeometry.LocalCanvasHeight)
            {
                yield return Sv5JumpSolidDiagnostic.CanvasMismatch;
            }

            Sv5JumpSupport[] nonNullSupports = Supports.Where(value => value != null).ToArray();
            if (nonNullSupports.Length != Supports.Count ||
                nonNullSupports.Select(value => value.SupportId).Distinct(StringComparer.Ordinal).Count() != nonNullSupports.Length)
            {
                yield return Sv5JumpSolidDiagnostic.DuplicateSupport;
            }

            var expectedCells = new Dictionary<Sv5JumpPoint, Sv5JumpSolidCell>();
            foreach (Sv5JumpSupport support in nonNullSupports)
            {
                bool sizeValid = support.Width >= 2 && support.Width <= 4 &&
                    ((support.Kind == Sv5JumpSupportKind.Solid && support.Height >= 1 && support.Height <= 3) ||
                     (support.Kind == Sv5JumpSupportKind.OneWay && support.Height == 1));
                if (!sizeValid)
                {
                    yield return Sv5JumpSolidDiagnostic.InvalidSupportSize + ":" + support.SupportId;
                }

                if (support.X < 0 || support.Y < 0 || support.X + support.Width > Width || support.Y + support.Height > Height)
                {
                    yield return Sv5JumpSolidDiagnostic.SupportOutOfBounds + ":" + support.SupportId;
                }

                for (int y = support.Y; y < support.TopY; y++)
                {
                    for (int x = support.X; x <= support.TopXMax; x++)
                    {
                        var point = new Sv5JumpPoint(x, y);
                        if (expectedCells.ContainsKey(point))
                        {
                            yield return Sv5JumpSolidDiagnostic.SupportOverlap + ":" + point;
                        }
                        else
                        {
                            expectedCells.Add(point, new Sv5JumpSolidCell(support.SupportId, point, support.Kind));
                        }
                    }
                }
            }

            var actualCells = new Dictionary<Sv5JumpPoint, Sv5JumpSolidCell>();
            foreach (Sv5JumpSolidCell cell in SupportCells)
            {
                if (cell.Point.X < 0 || cell.Point.Y < 0 || cell.Point.X >= Width || cell.Point.Y >= Height)
                {
                    yield return Sv5JumpSolidDiagnostic.CellOutOfBounds + ":" + cell.Point;
                }

                if (actualCells.ContainsKey(cell.Point))
                {
                    yield return Sv5JumpSolidDiagnostic.ExtraSupportCell + ":" + cell.Point;
                }
                else
                {
                    actualCells.Add(cell.Point, cell);
                }

                Sv5JumpSolidCell expected;
                if (!expectedCells.TryGetValue(cell.Point, out expected) || !expected.Equals(cell))
                {
                    yield return Sv5JumpSolidDiagnostic.ExtraSupportCell + ":" + cell.Point;
                }
            }

            foreach (KeyValuePair<Sv5JumpPoint, Sv5JumpSolidCell> expected in expectedCells)
            {
                Sv5JumpSolidCell actual;
                if (!actualCells.TryGetValue(expected.Key, out actual) || !actual.Equals(expected.Value))
                {
                    yield return Sv5JumpSolidDiagnostic.MissingSupportCell + ":" + expected.Key;
                }
            }

            var solidCells = new HashSet<Sv5JumpPoint>(SupportCells
                .Where(value => value.Kind == Sv5JumpSupportKind.Solid).Select(value => value.Point));
            for (int y = 0; y <= Height - 6; y++)
            {
                for (int x = 0; x <= Width - 6; x++)
                {
                    bool filled = true;
                    for (int dy = 0; dy < 6 && filled; dy++)
                    {
                        for (int dx = 0; dx < 6; dx++)
                        {
                            if (!solidCells.Contains(new Sv5JumpPoint(x + dx, y + dy)))
                            {
                                filled = false;
                                break;
                            }
                        }
                    }

                    if (filled)
                    {
                        yield return Sv5JumpSolidDiagnostic.SolidSixBySix + ":" + x + ":" + y;
                    }
                }
            }

            if (RequiredSupportSequence.Count < 10)
            {
                yield return Sv5JumpSolidDiagnostic.TooFewRouteSupports;
            }

            if (RequiredSupportSequence.Distinct(StringComparer.Ordinal).Count() != RequiredSupportSequence.Count)
            {
                yield return Sv5JumpSolidDiagnostic.RouteSequenceInvalid;
            }

            var supportById = nonNullSupports.GroupBy(value => value.SupportId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var requiredSupports = new List<Sv5JumpSupport>();
            foreach (string supportId in RequiredSupportSequence)
            {
                Sv5JumpSupport support;
                if (!supportById.TryGetValue(supportId, out support))
                {
                    yield return Sv5JumpSolidDiagnostic.RouteSequenceInvalid + ":" + supportId;
                    continue;
                }

                requiredSupports.Add(support);
                if (!support.ActiveRoute || support.DecorativeOnly)
                {
                    yield return Sv5JumpSolidDiagnostic.RequiredSupportInvalid + ":" + supportId;
                }
            }

            int activeSolids = requiredSupports.Count(value => value.ActiveRoute && !value.DecorativeOnly &&
                value.Kind == Sv5JumpSupportKind.Solid);
            int activeOneWays = requiredSupports.Count(value => value.ActiveRoute && !value.DecorativeOnly &&
                value.Kind == Sv5JumpSupportKind.OneWay);
            if (activeSolids < 4)
            {
                yield return Sv5JumpSolidDiagnostic.TooFewSolids;
            }

            if (activeOneWays < 3)
            {
                yield return Sv5JumpSolidDiagnostic.TooFewOneWays;
            }

            if (RouteLinks.Count != Math.Max(0, RequiredSupportSequence.Count - 1))
            {
                yield return Sv5JumpSolidDiagnostic.RouteLinkCount;
            }

            bool departsSolid = false;
            bool landsSolid = false;
            for (int index = 0; index < RouteLinks.Count; index++)
            {
                Sv5JumpSolidRouteLink link = RouteLinks[index];
                if (link == null)
                {
                    yield return Sv5JumpSolidDiagnostic.RouteOrderBroken + ":" + index;
                    continue;
                }

                if (link.Order != index || index + 1 >= RequiredSupportSequence.Count ||
                    link.Source == null || link.Target == null ||
                    !string.Equals(link.Source.SupportId, RequiredSupportSequence[index], StringComparison.Ordinal) ||
                    !string.Equals(link.Target.SupportId, RequiredSupportSequence[index + 1], StringComparison.Ordinal) ||
                    !link.RequiredRoute)
                {
                    yield return Sv5JumpSolidDiagnostic.RouteOrderBroken + ":" + link.LinkId;
                }

                if (!link.ContractAccepted)
                {
                    yield return Sv5JumpSolidDiagnostic.ContractRejected + ":" + link.LinkId + ":" +
                        string.Join("+", link.Measurement == null ? Array.Empty<string>() : link.Measurement.RejectionReasons);
                }

                if (link.GapAir < 0 || link.GapAir > 3)
                {
                    yield return Sv5JumpSolidDiagnostic.GapOutOfRange + ":" + link.LinkId;
                }

                if (link.Rise < 0 || link.Rise > 1)
                {
                    yield return Sv5JumpSolidDiagnostic.RiseOutOfRange + ":" + link.LinkId;
                }

                if (link.Source != null && !link.Source.SupportsFeet(link.Takeoff))
                {
                    yield return Sv5JumpSolidDiagnostic.UnsupportedTakeoff + ":" + link.LinkId;
                }

                if (link.Target != null && !link.Target.SupportsFeet(link.Landing))
                {
                    yield return Sv5JumpSolidDiagnostic.UnsupportedLanding + ":" + link.LinkId;
                }

                departsSolid |= link.Source != null && link.Source.Kind == Sv5JumpSupportKind.Solid;
                landsSolid |= link.Target != null && link.Target.Kind == Sv5JumpSupportKind.Solid;
            }

            if (!departsSolid || !landsSolid)
            {
                yield return Sv5JumpSolidDiagnostic.MissingSolidDirection;
            }

            if (RouteVerticalSpan < 8)
            {
                yield return Sv5JumpSolidDiagnostic.VerticalSpan;
            }

            foreach (Sv5JumpSolidUse use in SolidUses)
            {
                if (!use.CountedActiveSolid || use.AsTakeoffCount + use.AsLandingCount == 0)
                {
                    yield return Sv5JumpSolidDiagnostic.SolidNotUsed + ":" + use.SupportId;
                }
            }

            var clearanceById = Clearances.Where(value => value != null)
                .GroupBy(value => value.SupportId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            foreach (Sv5JumpSupport support in requiredSupports)
            {
                Sv5JumpSolidClearance[] values;
                if (!clearanceById.TryGetValue(support.SupportId, out values) || values.Length != 1)
                {
                    yield return Sv5JumpSolidDiagnostic.ClearanceMissing + ":" + support.SupportId;
                    continue;
                }

                Sv5JumpSolidClearance clearance = values[0];
                bool valid = support.SupportsFeet(clearance.Foot) && clearance.Body.Equals(clearance.Foot) &&
                    clearance.Head.Equals(new Sv5JumpPoint(clearance.Foot.X, clearance.Foot.Y + 1)) &&
                    clearance.Head.X >= 0 && clearance.Head.X < Width && clearance.Head.Y >= 0 && clearance.Head.Y < Height &&
                    !actualCells.ContainsKey(clearance.Body) && !actualCells.ContainsKey(clearance.Head) && clearance.Clear;
                if (!valid)
                {
                    yield return Sv5JumpSolidDiagnostic.ClearanceBlocked + ":" + support.SupportId;
                }
            }

            if (!ValidSocket(EntrySocket, RequiredSupportSequence.FirstOrDefault(), supportById) ||
                !ValidSocket(ExitSocket, RequiredSupportSequence.LastOrDefault(), supportById))
            {
                yield return Sv5JumpSolidDiagnostic.SocketMismatch;
            }

            if (Supports.Any(value => value != null && value.ValidationState == Sv5JumpValidationState.PlayerVerified) ||
                RouteLinks.Any(value => value != null && value.ValidationState == Sv5JumpValidationState.PlayerVerified))
            {
                yield return Sv5JumpSolidDiagnostic.PrematurePlayerState;
            }
        }

        private static bool ValidSocket(
            Sv5JumpSolidSocket socket,
            string expectedSupportId,
            IReadOnlyDictionary<string, Sv5JumpSupport> supports)
        {
            Sv5JumpSupport support;
            return socket != null && !string.IsNullOrEmpty(expectedSupportId) &&
                string.Equals(socket.SupportId, expectedSupportId, StringComparison.Ordinal) &&
                supports.TryGetValue(socket.SupportId, out support) && support.SupportsFeet(socket.Foot);
        }

        private static string SupportToken(Sv5JumpSupport value)
        {
            return value == null
                ? "NULL_SUPPORT"
                : value.SupportId + "|" + Sv5JumpSolidGeometry.KindName(value.Kind) + "|" + value.X + "|" +
                  value.Y + "|" + value.Width + "|" + value.Height + "|" + value.ActiveRoute + "|" +
                  value.DecorativeOnly + "|" + Sv5JumpContract.StateName(value.ValidationState);
        }

        private static string Hash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                return BitConverter.ToString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }

    public static class Sv5JumpSolidGeometry
    {
        public const int LocalCanvasWidth = 24;
        public const int LocalCanvasHeight = 32;

        public static Sv5JumpSolidPlan CreateCanonicalLocalFixture()
        {
            Sv5JumpSupport[] supports =
            {
                Support("JS00_ENTRY_SOLID", Sv5JumpSupportKind.Solid, 0, 1, 3),
                Support("JS01_ONE_WAY", Sv5JumpSupportKind.OneWay, 6, 2, 3),
                Support("JS02_SOLID", Sv5JumpSupportKind.Solid, 12, 3, 3),
                Support("JS03_ONE_WAY", Sv5JumpSupportKind.OneWay, 18, 4, 3),
                Support("JS04_SOLID", Sv5JumpSupportKind.Solid, 14, 5, 2),
                Support("JS05_ONE_WAY", Sv5JumpSupportKind.OneWay, 9, 6, 3),
                Support("JS06_SOLID", Sv5JumpSupportKind.Solid, 4, 7, 3),
                Support("JS07_ONE_WAY", Sv5JumpSupportKind.OneWay, 0, 8, 2),
                Support("JS08_SOLID", Sv5JumpSupportKind.Solid, 3, 9, 2),
                Support("JS09_EXIT_ONE_WAY", Sv5JumpSupportKind.OneWay, 7, 10, 3)
            };
            string[] sequence = supports.Select(value => value.SupportId).ToArray();
            return BuildLocal(supports, sequence);
        }

        public static Sv5JumpSolidPlan CreateAllOneWayFixture()
        {
            Sv5JumpSolidPlan canonical = CreateCanonicalLocalFixture();
            Sv5JumpSupport[] supports = canonical.Supports.Select(value => new Sv5JumpSupport(
                value.SupportId,
                Sv5JumpSupportKind.OneWay,
                value.X,
                value.TopY - 1,
                value.Width,
                1,
                value.ActiveRoute,
                value.DecorativeOnly,
                Sv5JumpValidationState.StaticScreen)).ToArray();
            return BuildLocal(supports, canonical.RequiredSupportSequence);
        }

        public static Sv5JumpSolidPlan BuildLocal(
            IEnumerable<Sv5JumpSupport> supports,
            IEnumerable<string> requiredSupportSequence,
            IEnumerable<Sv5JumpSolidRouteLink> routeLinks = null,
            IEnumerable<Sv5JumpSolidClearance> clearances = null,
            IEnumerable<Sv5JumpSolidCell> supportCells = null,
            Sv5JumpSolidSocket entrySocket = null,
            Sv5JumpSolidSocket exitSocket = null,
            int width = LocalCanvasWidth,
            int height = LocalCanvasHeight)
        {
            Sv5JumpSupport[] supportArray = (supports ?? Array.Empty<Sv5JumpSupport>()).ToArray();
            string[] sequence = (requiredSupportSequence ?? Array.Empty<string>()).ToArray();
            var byId = supportArray.Where(value => value != null)
                .GroupBy(value => value.SupportId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            Sv5JumpSolidCell[] cells = supportCells == null ? EmitCells(supportArray).ToArray() : supportCells.ToArray();
            Sv5JumpSolidRouteLink[] links = routeLinks == null ? BuildLinks(sequence, byId).ToArray() : routeLinks.ToArray();
            Sv5JumpSolidClearance[] reservations = clearances == null
                ? BuildClearances(sequence, byId, cells, width, height).ToArray()
                : clearances.ToArray();
            Sv5JumpSolidSocket entry = entrySocket ?? Socket("ENTRY", sequence.FirstOrDefault(), byId, true);
            Sv5JumpSolidSocket exit = exitSocket ?? Socket("EXIT", sequence.LastOrDefault(), byId, false);
            return new Sv5JumpSolidPlan(width, height, supportArray, cells, sequence, links, reservations, entry, exit);
        }

        public static IEnumerable<Sv5JumpSolidCell> EmitCells(IEnumerable<Sv5JumpSupport> supports)
        {
            foreach (Sv5JumpSupport support in (supports ?? Array.Empty<Sv5JumpSupport>())
                .Where(value => value != null).OrderBy(value => value.SupportId, StringComparer.Ordinal))
            {
                for (int y = support.Y; y < support.TopY; y++)
                {
                    for (int x = support.X; x <= support.TopXMax; x++)
                    {
                        yield return new Sv5JumpSolidCell(support.SupportId, new Sv5JumpPoint(x, y), support.Kind);
                    }
                }
            }
        }

        public static string KindName(Sv5JumpSupportKind kind)
        {
            return kind == Sv5JumpSupportKind.Solid ? "SOLID" : "ONE_WAY";
        }

        private static IEnumerable<Sv5JumpSolidRouteLink> BuildLinks(
            IReadOnlyList<string> sequence,
            IReadOnlyDictionary<string, Sv5JumpSupport> supports)
        {
            for (int index = 0; index + 1 < sequence.Count; index++)
            {
                Sv5JumpSupport source;
                Sv5JumpSupport target;
                if (!supports.TryGetValue(sequence[index], out source) || !supports.TryGetValue(sequence[index + 1], out target))
                {
                    continue;
                }

                Sv5JumpDirection direction = target.X >= source.X
                    ? Sv5JumpDirection.LeftToRight
                    : Sv5JumpDirection.RightToLeft;
                var takeoff = new Sv5JumpPoint(
                    direction == Sv5JumpDirection.LeftToRight ? source.TopXMax : source.TopXMin,
                    source.TopY);
                var landing = new Sv5JumpPoint(
                    direction == Sv5JumpDirection.LeftToRight ? target.TopXMin : target.TopXMax,
                    target.TopY);
                yield return new Sv5JumpSolidRouteLink(
                    index,
                    "JS_LINK_" + index.ToString("00"),
                    source,
                    target,
                    direction,
                    takeoff,
                    landing,
                    true);
            }
        }

        private static IEnumerable<Sv5JumpSolidClearance> BuildClearances(
            IEnumerable<string> sequence,
            IReadOnlyDictionary<string, Sv5JumpSupport> supports,
            IEnumerable<Sv5JumpSolidCell> cells,
            int width,
            int height)
        {
            var occupied = new HashSet<Sv5JumpPoint>(cells.Select(value => value.Point));
            foreach (string supportId in sequence)
            {
                Sv5JumpSupport support;
                if (!supports.TryGetValue(supportId, out support))
                {
                    continue;
                }

                Sv5JumpPoint selected = new Sv5JumpPoint(support.TopXMin, support.TopY);
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

                yield return new Sv5JumpSolidClearance(
                    supportId,
                    selected,
                    selected,
                    new Sv5JumpPoint(selected.X, selected.Y + 1),
                    clear);
            }
        }

        private static Sv5JumpSolidSocket Socket(
            string socketId,
            string supportId,
            IReadOnlyDictionary<string, Sv5JumpSupport> supports,
            bool first)
        {
            Sv5JumpSupport support;
            if (string.IsNullOrEmpty(supportId) || !supports.TryGetValue(supportId, out support))
            {
                return null;
            }

            int x = first ? support.TopXMin : support.TopXMax;
            return new Sv5JumpSolidSocket(socketId, supportId, new Sv5JumpPoint(x, support.TopY));
        }

        private static Sv5JumpSupport Support(
            string id,
            Sv5JumpSupportKind kind,
            int x,
            int y,
            int width)
        {
            return new Sv5JumpSupport(id, kind, x, y, width, 1, true, false,
                Sv5JumpValidationState.StaticScreen);
        }
    }
}
