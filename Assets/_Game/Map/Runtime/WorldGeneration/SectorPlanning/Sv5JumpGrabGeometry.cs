using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public sealed class Sv5JumpGrabRouteLink
    {
        public Sv5JumpGrabRouteLink(
            int order,
            string linkId,
            Sv5JumpSupport source,
            Sv5JumpSupport target,
            Sv5JumpDirection direction,
            Sv5JumpPoint takeoff,
            Sv5JumpPoint landing,
            Sv5JumpMode mode,
            Sv5JumpGrabEdge grabEdge,
            bool requiredRoute,
            Sv5JumpValidationState validationState = Sv5JumpValidationState.Planned,
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
            Mode = mode;
            GrabEdge = grabEdge;
            RequiredRoute = requiredRoute;
            ValidationState = validationState;
            Measurement = Sv5JumpContract.Measure(
                linkId,
                source,
                target,
                direction,
                takeoff,
                landing,
                mode,
                grabEdge,
                validationState,
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
        public Sv5JumpMode Mode { get; }
        public Sv5JumpGrabEdge GrabEdge { get; }
        public bool RequiredRoute { get; }
        public Sv5JumpValidationState ValidationState { get; }
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
                    Landing + "|" + GapAir + "|" + Rise + "|" + Mode + "|" +
                    (GrabEdge == null ? string.Empty : GrabEdge.GrabEdgeId) + "|" + RequiredRoute + "|" +
                    ValidationState + "|" + ContractAccepted;
            }
        }
    }

    public sealed class Sv5JumpGrabClearance
    {
        public Sv5JumpGrabClearance(
            string grabEdgeId,
            Sv5JumpPoint hangBody,
            Sv5JumpPoint hangHead,
            Sv5JumpPoint pullUpFoot,
            Sv5JumpPoint pullUpHead,
            bool allAir)
        {
            GrabEdgeId = grabEdgeId;
            HangBody = hangBody;
            HangHead = hangHead;
            PullUpFoot = pullUpFoot;
            PullUpHead = pullUpHead;
            AllAir = allAir;
        }

        public string GrabEdgeId { get; }
        public Sv5JumpPoint HangBody { get; }
        public Sv5JumpPoint HangHead { get; }
        public Sv5JumpPoint PullUpFoot { get; }
        public Sv5JumpPoint PullUpHead { get; }
        public bool AllAir { get; }

        internal string DigestToken
        {
            get { return GrabEdgeId + "|" + HangBody + "|" + HangHead + "|" + PullUpFoot + "|" + PullUpHead + "|" + AllAir; }
        }
    }

    public sealed class Sv5JumpGrabAction
    {
        public Sv5JumpGrabAction(
            int order,
            string action,
            Sv5JumpPoint player,
            Sv5JumpPoint contact,
            string supportId)
        {
            Order = order;
            Action = action;
            Player = player;
            Contact = contact;
            SupportId = supportId;
        }

        public int Order { get; }
        public string Action { get; }
        public Sv5JumpPoint Player { get; }
        public Sv5JumpPoint Contact { get; }
        public string SupportId { get; }

        internal string DigestToken
        {
            get { return Order + "|" + Action + "|" + Player + "|" + Contact + "|" + SupportId; }
        }
    }

    public static class Sv5JumpGrabDiagnostic
    {
        public const string CanvasMismatch = "LOCAL_JUMP_ROOM_MUST_BE_24X32";
        public const string BaseDigestMismatch = "SV5_14_BASE_FIXTURE_DIGEST_MISMATCH";
        public const string SupportTransformMismatch = "SUPPORT_DIFF_EXCEEDS_JS04_TRANSLATION";
        public const string SupportOutOfBounds = "SUPPORT_OUT_OF_LOCAL_BOUNDS";
        public const string SupportOverlap = "SUPPORT_CELL_OVERLAP";
        public const string SupportCellMismatch = "SUPPORT_CELLS_DO_NOT_MATCH_DERIVED_RECTANGLES";
        public const string OldCellOccupied = "OLD_JS04_CELL_MUST_BE_AIR";
        public const string MovedCellMissing = "MOVED_JS04_SOLID_CELL_MISSING";
        public const string RouteSequenceMismatch = "SV5_14_ORDERED_ROUTE_CHANGED";
        public const string RouteLinkCount = "ROUTE_LINK_COUNT_MUST_BE_NINE";
        public const string RouteLinkMismatch = "ORDERED_ROUTE_LINK_MISMATCH";
        public const string ContractRejected = "SV5_13_CONTRACT_REJECTED";
        public const string GrabLinkCount = "EXACTLY_ONE_JUMP_GRAB_LINK_REQUIRED";
        public const string GrabEdgeCount = "EXACTLY_ONE_GRAB_EDGE_REQUIRED";
        public const string GrabLinkMismatch = "JS_LINK_03_MUST_BE_EXACT_JUMP_GRAB";
        public const string NormalJumpMismatch = "NORMAL_JUMP_MUST_HAVE_RISE_ZERO_TO_ONE_AND_NO_GRAB";
        public const string GrabTargetMismatch = "GRAB_TARGET_MUST_BE_JS04_SOLID";
        public const string GrabTargetNotSolid = "GRAB_TARGET_MUST_BE_SOLID";
        public const string GrabFaceMismatch = "GRAB_FACE_MUST_BE_EXPOSED_RIGHT_FACE";
        public const string ContactMismatch = "GRAB_CONTACT_MUST_OWN_JS04_RIGHT_TOP_CELL";
        public const string HangBodyBlocked = "HANG_BODY_CELL_MUST_BE_AIR";
        public const string HangHeadBlocked = "HANG_HEAD_CELL_MUST_BE_AIR";
        public const string PullUpFootBlocked = "PULL_UP_FOOT_MUST_BE_SUPPORTED_AIR";
        public const string PullUpHeadBlocked = "PULL_UP_HEAD_CELL_MUST_BE_AIR";
        public const string ClearanceMismatch = "GRAB_CLEARANCE_RECORD_MISMATCH";
        public const string ActionSequenceMismatch = "GRAB_ACTION_SEQUENCE_MISMATCH";
        public const string PrematurePlayerState = "PLAYER_VERIFIED_FORBIDDEN_BEFORE_SV5_20";
    }

    public sealed class Sv5JumpGrabPlan
    {
        internal Sv5JumpGrabPlan(
            int width,
            int height,
            string baseFixtureDigest,
            IEnumerable<Sv5JumpSupport> supports,
            IEnumerable<Sv5JumpSolidCell> supportCells,
            IEnumerable<string> requiredSupportSequence,
            IEnumerable<Sv5JumpGrabRouteLink> routeLinks,
            IEnumerable<Sv5JumpGrabEdge> grabEdges,
            IEnumerable<Sv5JumpGrabClearance> clearances,
            IEnumerable<Sv5JumpGrabAction> actions,
            Sv5JumpSolidSocket entrySocket,
            Sv5JumpSolidSocket exitSocket)
        {
            Width = width;
            Height = height;
            BaseFixtureDigest = baseFixtureDigest ?? string.Empty;
            Supports = Array.AsReadOnly((supports ?? Array.Empty<Sv5JumpSupport>())
                .OrderBy(value => value == null ? string.Empty : value.SupportId, StringComparer.Ordinal).ToArray());
            SupportCells = Array.AsReadOnly((supportCells ?? Array.Empty<Sv5JumpSolidCell>())
                .OrderBy(value => value).ToArray());
            RequiredSupportSequence = Array.AsReadOnly((requiredSupportSequence ?? Array.Empty<string>()).ToArray());
            RouteLinks = Array.AsReadOnly((routeLinks ?? Array.Empty<Sv5JumpGrabRouteLink>())
                .OrderBy(value => value == null ? int.MaxValue : value.Order).ToArray());
            GrabEdges = Array.AsReadOnly((grabEdges ?? Array.Empty<Sv5JumpGrabEdge>())
                .OrderBy(value => value).ToArray());
            Clearances = Array.AsReadOnly((clearances ?? Array.Empty<Sv5JumpGrabClearance>())
                .OrderBy(value => value == null ? string.Empty : value.GrabEdgeId, StringComparer.Ordinal).ToArray());
            Actions = Array.AsReadOnly((actions ?? Array.Empty<Sv5JumpGrabAction>())
                .OrderBy(value => value == null ? int.MaxValue : value.Order).ToArray());
            EntrySocket = entrySocket;
            ExitSocket = exitSocket;
            Diagnostics = Array.AsReadOnly(Validate().Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Digest = Hash(string.Join("\n", Supports.Select(SupportToken)
                .Concat(SupportCells.Select(value => value.DigestToken))
                .Concat(RequiredSupportSequence.Select((value, index) => index + "|" + value))
                .Concat(RouteLinks.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(GrabEdges.Select(value => value.DigestToken))
                .Concat(Clearances.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(Actions.Where(value => value != null).Select(value => value.DigestToken))));
        }

        public int Width { get; }
        public int Height { get; }
        public string BaseFixtureDigest { get; }
        public IReadOnlyList<Sv5JumpSupport> Supports { get; }
        public IReadOnlyList<Sv5JumpSolidCell> SupportCells { get; }
        public IReadOnlyList<string> RequiredSupportSequence { get; }
        public IReadOnlyList<Sv5JumpGrabRouteLink> RouteLinks { get; }
        public IReadOnlyList<Sv5JumpGrabEdge> GrabEdges { get; }
        public IReadOnlyList<Sv5JumpGrabClearance> Clearances { get; }
        public IReadOnlyList<Sv5JumpGrabAction> Actions { get; }
        public Sv5JumpSolidSocket EntrySocket { get; }
        public Sv5JumpSolidSocket ExitSocket { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public bool ReverseCompletionRequired { get { return false; } }
        public bool LadderLogicCreated { get { return false; } }
        public bool OneWayGrabAllowed { get { return false; } }
        public int WholeWorldBuildsInNewTargetedTests { get { return 0; } }
        public int WholeWorldSearchesInNewTargetedTests { get { return 0; } }
        public bool JumpContractReady { get { return true; } }
        public bool JumpSolidGeometryReady { get { return true; } }
        public bool JumpGrabGeometryReady { get { return Diagnostics.Count == 0; } }
        public bool ComposedGeometryReady { get { return false; } }
        public bool PlayerVerified { get { return false; } }

        public Sv5JumpSupport Support(string id)
        {
            return Supports.Single(value => string.Equals(value.SupportId, id, StringComparison.Ordinal));
        }

        public Sv5JumpGrabRouteLink Link(string id)
        {
            return RouteLinks.Single(value => string.Equals(value.LinkId, id, StringComparison.Ordinal));
        }

        private IEnumerable<string> Validate()
        {
            if (Width != Sv5JumpGrabGeometry.LocalCanvasWidth || Height != Sv5JumpGrabGeometry.LocalCanvasHeight)
            {
                yield return Sv5JumpGrabDiagnostic.CanvasMismatch;
            }

            Sv5JumpSolidPlan baseline = Sv5JumpSolidGeometry.CreateCanonicalLocalFixture();
            if (!string.Equals(BaseFixtureDigest, baseline.Digest, StringComparison.Ordinal))
            {
                yield return Sv5JumpGrabDiagnostic.BaseDigestMismatch;
            }

            var baselineById = baseline.Supports.ToDictionary(value => value.SupportId, StringComparer.Ordinal);
            var supportsById = Supports.Where(value => value != null)
                .GroupBy(value => value.SupportId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (Supports.Any(value => value == null) || Supports.Count != baseline.Supports.Count ||
                supportsById.Count != baselineById.Count || !supportsById.Keys.OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(baselineById.Keys.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
            {
                yield return Sv5JumpGrabDiagnostic.SupportTransformMismatch;
            }

            foreach (KeyValuePair<string, Sv5JumpSupport> pair in supportsById)
            {
                Sv5JumpSupport original;
                if (!baselineById.TryGetValue(pair.Key, out original))
                {
                    yield return Sv5JumpGrabDiagnostic.SupportTransformMismatch + ":" + pair.Key;
                    continue;
                }

                Sv5JumpSupport actual = pair.Value;
                int expectedY = original.Y + (pair.Key == Sv5JumpGrabGeometry.TargetSupportId ? 1 : 0);
                if (actual.Kind != original.Kind || actual.X != original.X || actual.Y != expectedY ||
                    actual.Width != original.Width || actual.Height != original.Height ||
                    actual.ActiveRoute != original.ActiveRoute || actual.DecorativeOnly != original.DecorativeOnly ||
                    actual.ValidationState != original.ValidationState)
                {
                    yield return Sv5JumpGrabDiagnostic.SupportTransformMismatch + ":" + pair.Key;
                }
            }

            var expectedCells = new Dictionary<Sv5JumpPoint, Sv5JumpSolidCell>();
            foreach (Sv5JumpSupport support in Supports.Where(value => value != null))
            {
                if (support.X < 0 || support.Y < 0 || support.X + support.Width > Width || support.Y + support.Height > Height)
                {
                    yield return Sv5JumpGrabDiagnostic.SupportOutOfBounds + ":" + support.SupportId;
                }

                for (int y = support.Y; y < support.TopY; y++)
                {
                    for (int x = support.TopXMin; x <= support.TopXMax; x++)
                    {
                        var point = new Sv5JumpPoint(x, y);
                        if (expectedCells.ContainsKey(point))
                        {
                            yield return Sv5JumpGrabDiagnostic.SupportOverlap + ":" + point;
                        }
                        else
                        {
                            expectedCells.Add(point, new Sv5JumpSolidCell(support.SupportId, point, support.Kind));
                        }
                    }
                }
            }

            var actualCells = new Dictionary<Sv5JumpPoint, Sv5JumpSolidCell>();
            bool cellMismatch = false;
            foreach (Sv5JumpSolidCell cell in SupportCells)
            {
                if (actualCells.ContainsKey(cell.Point) || cell.Point.X < 0 || cell.Point.Y < 0 ||
                    cell.Point.X >= Width || cell.Point.Y >= Height)
                {
                    cellMismatch = true;
                }
                else
                {
                    actualCells.Add(cell.Point, cell);
                }
            }

            if (actualCells.Count != expectedCells.Count || expectedCells.Any(pair =>
                !actualCells.ContainsKey(pair.Key) || !actualCells[pair.Key].Equals(pair.Value)))
            {
                cellMismatch = true;
            }

            if (cellMismatch)
            {
                yield return Sv5JumpGrabDiagnostic.SupportCellMismatch;
            }

            foreach (Sv5JumpPoint oldCell in new[] { new Sv5JumpPoint(14, 5), new Sv5JumpPoint(15, 5) })
            {
                if (actualCells.ContainsKey(oldCell))
                {
                    yield return Sv5JumpGrabDiagnostic.OldCellOccupied + ":" + oldCell;
                }
            }

            foreach (Sv5JumpPoint movedCell in new[] { new Sv5JumpPoint(14, 6), new Sv5JumpPoint(15, 6) })
            {
                Sv5JumpSolidCell actual;
                if (!actualCells.TryGetValue(movedCell, out actual) ||
                    !string.Equals(actual.SupportId, Sv5JumpGrabGeometry.TargetSupportId, StringComparison.Ordinal) ||
                    actual.Kind != Sv5JumpSupportKind.Solid)
                {
                    yield return Sv5JumpGrabDiagnostic.MovedCellMissing + ":" + movedCell;
                }
            }

            if (!RequiredSupportSequence.SequenceEqual(baseline.RequiredSupportSequence, StringComparer.Ordinal))
            {
                yield return Sv5JumpGrabDiagnostic.RouteSequenceMismatch;
            }

            if (RouteLinks.Count != 9)
            {
                yield return Sv5JumpGrabDiagnostic.RouteLinkCount;
            }

            int grabLinkCount = 0;
            for (int index = 0; index < RouteLinks.Count; index++)
            {
                Sv5JumpGrabRouteLink link = RouteLinks[index];
                if (link == null || index + 1 >= RequiredSupportSequence.Count || link.Order != index ||
                    link.Source == null || link.Target == null ||
                    !string.Equals(link.Source.SupportId, RequiredSupportSequence[index], StringComparison.Ordinal) ||
                    !string.Equals(link.Target.SupportId, RequiredSupportSequence[index + 1], StringComparison.Ordinal) ||
                    !link.RequiredRoute)
                {
                    yield return Sv5JumpGrabDiagnostic.RouteLinkMismatch + ":" + index;
                    continue;
                }

                if (!link.ContractAccepted)
                {
                    yield return Sv5JumpGrabDiagnostic.ContractRejected + ":" + link.LinkId + ":" +
                        string.Join("+", link.Measurement.RejectionReasons);
                }

                if (link.Mode == Sv5JumpMode.JumpGrab)
                {
                    grabLinkCount++;
                    if (!string.Equals(link.LinkId, Sv5JumpGrabGeometry.GrabLinkId, StringComparison.Ordinal) ||
                        link.Order != 3 || link.GapAir != 2 || link.Rise != 2 ||
                        link.Direction != Sv5JumpDirection.RightToLeft ||
                        !link.Takeoff.Equals(new Sv5JumpPoint(18, 5)) ||
                        !link.Landing.Equals(new Sv5JumpPoint(15, 7)) ||
                        link.GrabEdge == null ||
                        !string.Equals(link.GrabEdge.GrabEdgeId, Sv5JumpGrabGeometry.GrabEdgeId, StringComparison.Ordinal))
                    {
                        yield return Sv5JumpGrabDiagnostic.GrabLinkMismatch;
                    }
                }
                else if (link.Mode != Sv5JumpMode.Jump || link.GrabEdge != null || link.Rise < 0 || link.Rise > 1)
                {
                    yield return Sv5JumpGrabDiagnostic.NormalJumpMismatch + ":" + link.LinkId;
                }
            }

            if (grabLinkCount != 1)
            {
                yield return Sv5JumpGrabDiagnostic.GrabLinkCount;
            }

            if (GrabEdges.Count != 1)
            {
                yield return Sv5JumpGrabDiagnostic.GrabEdgeCount;
                yield break;
            }

            Sv5JumpGrabEdge edge = GrabEdges[0];
            Sv5JumpSupport target;
            if (!supportsById.TryGetValue(Sv5JumpGrabGeometry.TargetSupportId, out target) ||
                !string.Equals(edge.SupportId, Sv5JumpGrabGeometry.TargetSupportId, StringComparison.Ordinal))
            {
                yield return Sv5JumpGrabDiagnostic.GrabTargetMismatch;
                yield break;
            }

            if (target.Kind != Sv5JumpSupportKind.Solid)
            {
                yield return Sv5JumpGrabDiagnostic.GrabTargetNotSolid;
            }

            var expectedContact = new Sv5JumpPoint(target.TopXMax, target.TopY - 1);
            var expectedHangBody = new Sv5JumpPoint(target.TopXMax + 1, target.TopY - 1);
            var expectedHangHead = new Sv5JumpPoint(expectedHangBody.X, expectedHangBody.Y + 1);
            var expectedPullFoot = new Sv5JumpPoint(target.TopXMax, target.TopY);
            var expectedPullHead = new Sv5JumpPoint(expectedPullFoot.X, expectedPullFoot.Y + 1);
            Sv5JumpSolidCell contactCell;
            bool contactOwned = actualCells.TryGetValue(edge.Contact, out contactCell) &&
                string.Equals(contactCell.SupportId, Sv5JumpGrabGeometry.TargetSupportId, StringComparison.Ordinal) &&
                contactCell.Kind == Sv5JumpSupportKind.Solid;
            if (!edge.Contact.Equals(expectedContact) || !contactOwned)
            {
                yield return Sv5JumpGrabDiagnostic.ContactMismatch;
            }

            if (edge.Face != Sv5JumpGrabFace.Right || edge.ApproachDirection != Sv5JumpDirection.RightToLeft ||
                !edge.Exposed || actualCells.ContainsKey(expectedHangBody))
            {
                yield return Sv5JumpGrabDiagnostic.GrabFaceMismatch;
            }

            Sv5JumpGrabClearance clearance = Clearances.Count == 1 ? Clearances[0] : null;
            if (clearance == null || !string.Equals(clearance.GrabEdgeId, edge.GrabEdgeId, StringComparison.Ordinal) ||
                !clearance.HangBody.Equals(expectedHangBody) || !clearance.HangHead.Equals(expectedHangHead) ||
                !clearance.PullUpFoot.Equals(expectedPullFoot) || !clearance.PullUpHead.Equals(expectedPullHead))
            {
                yield return Sv5JumpGrabDiagnostic.ClearanceMismatch;
            }
            else
            {
                bool hangBodyAir = InBounds(clearance.HangBody) && !actualCells.ContainsKey(clearance.HangBody);
                bool hangHeadAir = InBounds(clearance.HangHead) && !actualCells.ContainsKey(clearance.HangHead);
                bool pullFootAir = InBounds(clearance.PullUpFoot) && !actualCells.ContainsKey(clearance.PullUpFoot) &&
                    actualCells.ContainsKey(new Sv5JumpPoint(clearance.PullUpFoot.X, clearance.PullUpFoot.Y - 1));
                bool pullHeadAir = InBounds(clearance.PullUpHead) && !actualCells.ContainsKey(clearance.PullUpHead);
                if (!hangBodyAir || !edge.HangBody.Equals(clearance.HangBody) || !edge.HangBodyClear)
                {
                    yield return Sv5JumpGrabDiagnostic.HangBodyBlocked;
                }
                if (!hangHeadAir)
                {
                    yield return Sv5JumpGrabDiagnostic.HangHeadBlocked;
                }
                if (!pullFootAir || !edge.PullUp.Equals(clearance.PullUpFoot) || !edge.PullUpClear)
                {
                    yield return Sv5JumpGrabDiagnostic.PullUpFootBlocked;
                }
                if (!pullHeadAir)
                {
                    yield return Sv5JumpGrabDiagnostic.PullUpHeadBlocked;
                }
                if (clearance.AllAir != (hangBodyAir && hangHeadAir && pullFootAir && pullHeadAir))
                {
                    yield return Sv5JumpGrabDiagnostic.ClearanceMismatch;
                }
            }

            string[] expectedActions = { "TAKEOFF", "CONTACT_HANG", "PULL_UP", "LAND" };
            Sv5JumpPoint[] expectedPlayers =
            {
                new Sv5JumpPoint(18, 5), new Sv5JumpPoint(16, 6),
                new Sv5JumpPoint(15, 7), new Sv5JumpPoint(15, 7)
            };
            Sv5JumpPoint[] expectedContacts =
            {
                new Sv5JumpPoint(-1, -1), new Sv5JumpPoint(15, 6),
                new Sv5JumpPoint(15, 6), new Sv5JumpPoint(-1, -1)
            };
            string[] expectedSupports =
            {
                "JS03_ONE_WAY", Sv5JumpGrabGeometry.TargetSupportId,
                Sv5JumpGrabGeometry.TargetSupportId, Sv5JumpGrabGeometry.TargetSupportId
            };
            if (Actions.Count != 4 || Actions.Where((value, index) => value == null || value.Order != index ||
                !string.Equals(value.Action, expectedActions[index], StringComparison.Ordinal) ||
                !value.Player.Equals(expectedPlayers[index]) || !value.Contact.Equals(expectedContacts[index]) ||
                !string.Equals(value.SupportId, expectedSupports[index], StringComparison.Ordinal)).Any())
            {
                yield return Sv5JumpGrabDiagnostic.ActionSequenceMismatch;
            }

            if (Supports.Any(value => value.ValidationState == Sv5JumpValidationState.PlayerVerified) ||
                RouteLinks.Any(value => value != null && value.ValidationState == Sv5JumpValidationState.PlayerVerified))
            {
                yield return Sv5JumpGrabDiagnostic.PrematurePlayerState;
            }
        }

        private bool InBounds(Sv5JumpPoint point)
        {
            return point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height;
        }

        private static string SupportToken(Sv5JumpSupport value)
        {
            return value == null ? "NULL_SUPPORT" : value.DigestToken;
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

    public static class Sv5JumpGrabGeometry
    {
        public const int LocalCanvasWidth = 24;
        public const int LocalCanvasHeight = 32;
        public const string TargetSupportId = "JS04_SOLID";
        public const string GrabLinkId = "JS_LINK_03";
        public const string GrabEdgeId = "JS04_RIGHT_GRAB";

        public static Sv5JumpGrabPlan CreateCanonicalLocalFixture()
        {
            Sv5JumpSolidPlan baseline = Sv5JumpSolidGeometry.CreateCanonicalLocalFixture();
            Sv5JumpSupport[] supports = baseline.Supports.Select(value => new Sv5JumpSupport(
                value.SupportId,
                value.Kind,
                value.X,
                value.Y + (value.SupportId == TargetSupportId ? 1 : 0),
                value.Width,
                value.Height,
                value.ActiveRoute,
                value.DecorativeOnly,
                value.ValidationState)).ToArray();
            Sv5JumpSolidCell[] cells = Sv5JumpSolidGeometry.EmitCells(supports).ToArray();
            var occupied = new HashSet<Sv5JumpPoint>(cells.Select(value => value.Point));
            Sv5JumpSupport target = supports.Single(value => value.SupportId == TargetSupportId);
            var contact = new Sv5JumpPoint(15, 6);
            var hangBody = new Sv5JumpPoint(16, 6);
            var hangHead = new Sv5JumpPoint(16, 7);
            var pullFoot = new Sv5JumpPoint(15, 7);
            var pullHead = new Sv5JumpPoint(15, 8);
            bool exposed = target.ContainsCell(contact) && !occupied.Contains(hangBody);
            bool hangClear = !occupied.Contains(hangBody) && !occupied.Contains(hangHead);
            bool pullClear = !occupied.Contains(pullFoot) && !occupied.Contains(pullHead) &&
                occupied.Contains(new Sv5JumpPoint(pullFoot.X, pullFoot.Y - 1));
            var edge = new Sv5JumpGrabEdge(
                GrabEdgeId,
                TargetSupportId,
                contact,
                Sv5JumpGrabFace.Right,
                Sv5JumpDirection.RightToLeft,
                hangBody,
                pullFoot,
                exposed,
                hangClear,
                pullClear);
            Sv5JumpGrabRouteLink[] links = BuildLinks(supports, baseline.RequiredSupportSequence, edge).ToArray();
            var clearance = new Sv5JumpGrabClearance(GrabEdgeId, hangBody, hangHead, pullFoot, pullHead,
                hangClear && pullClear);
            Sv5JumpGrabAction[] actions =
            {
                new Sv5JumpGrabAction(0, "TAKEOFF", new Sv5JumpPoint(18, 5), new Sv5JumpPoint(-1, -1), "JS03_ONE_WAY"),
                new Sv5JumpGrabAction(1, "CONTACT_HANG", hangBody, contact, TargetSupportId),
                new Sv5JumpGrabAction(2, "PULL_UP", pullFoot, contact, TargetSupportId),
                new Sv5JumpGrabAction(3, "LAND", pullFoot, new Sv5JumpPoint(-1, -1), TargetSupportId)
            };
            return BuildLocal(
                supports,
                baseline.RequiredSupportSequence,
                links,
                new[] { edge },
                new[] { clearance },
                actions,
                cells,
                baseline.EntrySocket,
                baseline.ExitSocket,
                baseline.Digest);
        }

        public static Sv5JumpGrabPlan BuildLocal(
            IEnumerable<Sv5JumpSupport> supports,
            IEnumerable<string> requiredSupportSequence,
            IEnumerable<Sv5JumpGrabRouteLink> routeLinks,
            IEnumerable<Sv5JumpGrabEdge> grabEdges,
            IEnumerable<Sv5JumpGrabClearance> clearances,
            IEnumerable<Sv5JumpGrabAction> actions,
            IEnumerable<Sv5JumpSolidCell> supportCells,
            Sv5JumpSolidSocket entrySocket,
            Sv5JumpSolidSocket exitSocket,
            string baseFixtureDigest,
            int width = LocalCanvasWidth,
            int height = LocalCanvasHeight)
        {
            return new Sv5JumpGrabPlan(
                width,
                height,
                baseFixtureDigest,
                supports,
                supportCells,
                requiredSupportSequence,
                routeLinks,
                grabEdges,
                clearances,
                actions,
                entrySocket,
                exitSocket);
        }

        private static IEnumerable<Sv5JumpGrabRouteLink> BuildLinks(
            IEnumerable<Sv5JumpSupport> supports,
            IReadOnlyList<string> sequence,
            Sv5JumpGrabEdge grabEdge)
        {
            var byId = supports.ToDictionary(value => value.SupportId, StringComparer.Ordinal);
            for (int index = 0; index + 1 < sequence.Count; index++)
            {
                Sv5JumpSupport source = byId[sequence[index]];
                Sv5JumpSupport target = byId[sequence[index + 1]];
                Sv5JumpDirection direction = target.X >= source.X
                    ? Sv5JumpDirection.LeftToRight
                    : Sv5JumpDirection.RightToLeft;
                var takeoff = new Sv5JumpPoint(
                    direction == Sv5JumpDirection.LeftToRight ? source.TopXMax : source.TopXMin,
                    source.TopY);
                var landing = new Sv5JumpPoint(
                    direction == Sv5JumpDirection.LeftToRight ? target.TopXMin : target.TopXMax,
                    target.TopY);
                Sv5JumpMode mode = index == 3 ? Sv5JumpMode.JumpGrab : Sv5JumpMode.Jump;
                yield return new Sv5JumpGrabRouteLink(
                    index,
                    "JS_LINK_" + index.ToString("00"),
                    source,
                    target,
                    direction,
                    takeoff,
                    landing,
                    mode,
                    index == 3 ? grabEdge : null,
                    true);
            }
        }
    }
}
