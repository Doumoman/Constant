using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5JumpRecoveryMode
    {
        Walk,
        Jump,
        Drop,
    }

    public enum Sv5JumpRecoveryRouteKind
    {
        Linked,
        AlreadyAtCheckpoint,
    }

    public enum Sv5JumpRecoveryPhase
    {
        Source,
        Transit,
        Target,
    }

    public static class Sv5JumpRecoveryDiagnostic
    {
        public const string ClearanceMismatch = "SV5_18_CLEARANCE_IDENTITY_MISMATCH";
        public const string OverlayDensity = "RECOVERY_OVERLAY_GROUP_OR_CELL_COUNT_INVALID";
        public const string OverlayShape = "RECOVERY_CATCH_RUN_MUST_BE_HORIZONTAL_TWO_TO_FIVE_CELLS";
        public const string OverlayOccupancyCollision = "RECOVERY_OVERLAY_OCCUPANCY_COLLISION";
        public const string OverlayMainTraceOverlap = "RECOVERY_OVERLAY_OVERLAPS_MAIN_TRACE";
        public const string OverlayGrabContactOverlap = "RECOVERY_OVERLAY_OVERLAPS_GRAB_CONTACT";
        public const string OverlayMirrorMismatch = "RECOVERY_OVERLAY_MIRROR_MISMATCH";
        public const string SolidFillSixBySix = "RECOVERY_COMPOSITION_CONTAINS_SOLID_SIX_BY_SIX";
        public const string ProbeSetMismatch = "MISS_PROBE_SET_MUST_MATCH_EIGHTEEN_LINKS";
        public const string ProbeOriginMismatch = "MISS_PROBE_ORIGIN_IS_NOT_LOWER_MIDDLE_AIR_SAMPLE";
        public const string MissingCatch = "MISS_PROBE_HAS_NO_VALID_CATCH";
        public const string SkippedNearerCatch = "MISS_PROBE_SKIPPED_NEARER_OCCUPIED_CELL";
        public const string BlockedLandingBody = "MISS_LANDING_BODY_CELL_BLOCKED";
        public const string BlockedLandingHead = "MISS_LANDING_HEAD_CELL_BLOCKED";
        public const string BlockedFallRay = "MISS_FALL_RAY_BLOCKED_BEFORE_CATCH";
        public const string ProbeBindingMismatch = "MISS_PROBE_SOURCE_BINDING_MISMATCH";
        public const string RouteSetMismatch = "RECOVERY_ROUTE_SET_MUST_MATCH_EIGHTEEN_PROBES";
        public const string RouteFlagsInvalid = "RECOVERY_ROUTE_FLAGS_INVALID";
        public const string RouteEndpointMismatch = "RECOVERY_ROUTE_ENDPOINT_MISMATCH";
        public const string AlreadyAtCheckpointInvalid = "ALREADY_AT_CHECKPOINT_REQUIRES_ZERO_LINKS_AND_EQUAL_ENDPOINTS";
        public const string DisconnectedRoute = "LINKED_RECOVERY_REQUIRES_CONNECTED_MOVEMENT";
        public const string LinkDiscontinuity = "RECOVERY_LINK_DISCONTINUITY";
        public const string UnsupportedEndpoint = "RECOVERY_LINK_ENDPOINT_UNSUPPORTED";
        public const string InvalidMovementMode = "RECOVERY_MOVEMENT_MODE_INVALID";
        public const string JumpRiseExceeded = "RECOVERY_JUMP_RISE_EXCEEDS_ONE";
        public const string RisingDrop = "RECOVERY_DROP_MUST_NOT_RISE";
        public const string TraceCountInvalid = "RECOVERY_TRACE_STATE_COUNT_INVALID";
        public const string TraceEndpointMismatch = "RECOVERY_TRACE_ENDPOINT_MISMATCH";
        public const string TraceBodyBlocked = "RECOVERY_TRACE_BODY_BLOCKED";
        public const string TraceHeadBlocked = "RECOVERY_TRACE_HEAD_BLOCKED";
        public const string TraceNonLocalStep = "RECOVERY_TRACE_STEP_EXCEEDS_LOCAL_CELL";
        public const string TraceDiagonalClip = "RECOVERY_TRACE_DIAGONAL_SUPERCOVER_BLOCKED";
        public const string LaterCheckpointShortcut = "RECOVERY_CHECKPOINT_IS_LATER_THAN_FAILED_LINK";
        public const string CheckpointDiversity = "RECOVERY_CHECKPOINT_ORDER_DIVERSITY_BELOW_TWO";
        public const string MirrorMismatch = "RECOVERY_PROBE_OR_ROUTE_MIRROR_MISMATCH";
        public const string ReverseRequired = "REVERSE_RECOVERY_CLAIM_FORBIDDEN";
        public const string FalseReadiness = "RECOVERY_READINESS_CLAIM_WITH_INVALID_PROOF";
        public const string PlayerVerified = "PLAYER_VERIFIED_REQUIRES_SV5_20";
    }

    public sealed class Sv5JumpRecoveryCell : IComparable<Sv5JumpRecoveryCell>
    {
        public Sv5JumpRecoveryCell(
            string recipeId,
            string groupId,
            Sv5JumpPoint point,
            string collision,
            string ownerId)
        {
            RecipeId = recipeId ?? string.Empty;
            GroupId = groupId ?? string.Empty;
            Point = point;
            Collision = collision ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
        }

        public string RecipeId { get; }
        public string GroupId { get; }
        public Sv5JumpPoint Point { get; }
        public string Collision { get; }
        public string Role { get { return "RECOVERY_CATCH"; } }
        public string OwnerId { get; }

        public int CompareTo(Sv5JumpRecoveryCell other)
        {
            if (other == null) return 1;
            int recipe = string.Compare(RecipeId, other.RecipeId, StringComparison.Ordinal);
            if (recipe != 0) return recipe;
            int group = string.Compare(GroupId, other.GroupId, StringComparison.Ordinal);
            return group != 0 ? group : Point.CompareTo(other.Point);
        }

        internal string DigestToken
        {
            get { return RecipeId + "|" + GroupId + "|" + Point + "|" + Collision + "|" + OwnerId; }
        }
    }

    public sealed class Sv5JumpMissProbe
    {
        public Sv5JumpMissProbe(
            string recipeId,
            int failedLinkOrder,
            string sourceLinkId,
            int sourceSampleOrder,
            Sv5JumpPoint origin,
            string catchSource,
            string catchGroupId,
            Sv5JumpPoint catchPoint,
            Sv5JumpPoint landingBody,
            Sv5JumpPoint landingHead,
            int fallDistance,
            bool caught)
        {
            RecipeId = recipeId ?? string.Empty;
            FailedLinkOrder = failedLinkOrder;
            SourceLinkId = sourceLinkId ?? string.Empty;
            SourceSampleOrder = sourceSampleOrder;
            Origin = origin;
            CatchSource = catchSource ?? string.Empty;
            CatchGroupId = catchGroupId ?? string.Empty;
            CatchPoint = catchPoint;
            LandingBody = landingBody;
            LandingHead = landingHead;
            FallDistance = fallDistance;
            Caught = caught;
        }

        public string RecipeId { get; }
        public int FailedLinkOrder { get; }
        public string SourceLinkId { get; }
        public int ProbeOrder { get { return 0; } }
        public int SourceSampleOrder { get; }
        public Sv5JumpPoint Origin { get; }
        public string CatchSource { get; }
        public string CatchGroupId { get; }
        public Sv5JumpPoint CatchPoint { get; }
        public Sv5JumpPoint LandingBody { get; }
        public Sv5JumpPoint LandingHead { get; }
        public int FallDistance { get; }
        public bool Caught { get; }

        internal string DigestToken
        {
            get
            {
                return RecipeId + "|" + FailedLinkOrder + "|" + SourceLinkId + "|" + SourceSampleOrder + "|" +
                    Origin + "|" + CatchSource + "|" + CatchGroupId + "|" + CatchPoint + "|" + LandingBody +
                    "|" + LandingHead + "|" + FallDistance + "|" + Caught;
            }
        }
    }

    public sealed class Sv5JumpRecoveryTraceSample
    {
        public Sv5JumpRecoveryTraceSample(int sampleOrder, Sv5JumpRecoveryPhase phase, Sv5JumpPoint body)
        {
            SampleOrder = sampleOrder;
            Phase = phase;
            Body = body;
            Head = new Sv5JumpPoint(body.X, body.Y + 1);
        }

        public int SampleOrder { get; }
        public Sv5JumpRecoveryPhase Phase { get; }
        public Sv5JumpPoint Body { get; }
        public Sv5JumpPoint Head { get; }

        internal string DigestToken
        {
            get { return SampleOrder + "|" + Sv5JumpRecovery.PhaseName(Phase) + "|" + Body + "|" + Head; }
        }
    }

    public sealed class Sv5JumpRecoveryLink
    {
        public Sv5JumpRecoveryLink(
            string recipeId,
            string routeId,
            int linkOrder,
            string recoveryLinkId,
            Sv5JumpRecoveryMode mode,
            Sv5JumpPoint source,
            Sv5JumpPoint target,
            IEnumerable<Sv5JumpRecoveryTraceSample> trace)
        {
            RecipeId = recipeId ?? string.Empty;
            RouteId = routeId ?? string.Empty;
            LinkOrder = linkOrder;
            RecoveryLinkId = recoveryLinkId ?? string.Empty;
            Mode = mode;
            Source = source;
            Target = target;
            Trace = Array.AsReadOnly((trace ?? Array.Empty<Sv5JumpRecoveryTraceSample>()).ToArray());
        }

        public string RecipeId { get; }
        public string RouteId { get; }
        public int LinkOrder { get; }
        public string RecoveryLinkId { get; }
        public Sv5JumpRecoveryMode Mode { get; }
        public Sv5JumpPoint Source { get; }
        public Sv5JumpPoint Target { get; }
        public int Rise { get { return Target.Y - Source.Y; } }
        public IReadOnlyList<Sv5JumpRecoveryTraceSample> Trace { get; }

        internal string DigestToken
        {
            get
            {
                return RecipeId + "|" + RouteId + "|" + LinkOrder + "|" + RecoveryLinkId + "|" +
                    Sv5JumpRecovery.ModeName(Mode) + "|" + Source + "|" + Target + "|" +
                    string.Join(";", Trace.Where(value => value != null).Select(value => value.DigestToken));
            }
        }
    }

    public sealed class Sv5JumpRecoveryRoute
    {
        public Sv5JumpRecoveryRoute(
            string recipeId,
            string routeId,
            int failedLinkOrder,
            int checkpointLinkOrder,
            Sv5JumpRecoveryRouteKind routeKind,
            Sv5JumpPoint start,
            Sv5JumpPoint end,
            IEnumerable<Sv5JumpRecoveryLink> links,
            bool recoveryPass = true,
            bool reverseRequired = false,
            string diagnostic = "")
        {
            RecipeId = recipeId ?? string.Empty;
            RouteId = routeId ?? string.Empty;
            FailedLinkOrder = failedLinkOrder;
            CheckpointLinkOrder = checkpointLinkOrder;
            RouteKind = routeKind;
            Start = start;
            End = end;
            Links = Array.AsReadOnly((links ?? Array.Empty<Sv5JumpRecoveryLink>()).OrderBy(value =>
                value == null ? int.MaxValue : value.LinkOrder).ToArray());
            RecoveryPass = recoveryPass;
            ReverseRequired = reverseRequired;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public string RecipeId { get; }
        public string RouteId { get; }
        public int FailedLinkOrder { get; }
        public int CheckpointLinkOrder { get; }
        public Sv5JumpRecoveryRouteKind RouteKind { get; }
        public Sv5JumpPoint Start { get; }
        public Sv5JumpPoint End { get; }
        public IReadOnlyList<Sv5JumpRecoveryLink> Links { get; }
        public bool RecoveryPass { get; }
        public bool ReverseRequired { get; }
        public string Diagnostic { get; }

        internal string DigestToken
        {
            get
            {
                return RecipeId + "|" + RouteId + "|" + FailedLinkOrder + "|" + CheckpointLinkOrder + "|" +
                    Sv5JumpRecovery.RouteKindName(RouteKind) + "|" + Start + "|" + End + "|" +
                    string.Join(";", Links.Where(value => value != null).Select(value => value.DigestToken)) + "|" +
                    RecoveryPass + "|" + ReverseRequired + "|" + Diagnostic;
            }
        }
    }

    public sealed class Sv5JumpRecoveryPlan
    {
        internal Sv5JumpRecoveryPlan(
            string inputClearanceDigest,
            IEnumerable<Sv5JumpRecoveryCell> overlay,
            IEnumerable<Sv5JumpMissProbe> probes,
            IEnumerable<Sv5JumpRecoveryRoute> routes,
            IEnumerable<string> diagnostics,
            bool jumpRecipeReady,
            bool composedGeometryReady,
            bool sweptClearanceReady)
        {
            InputClearanceDigest = inputClearanceDigest ?? string.Empty;
            Overlay = Array.AsReadOnly((overlay ?? Array.Empty<Sv5JumpRecoveryCell>()).OrderBy(value => value).ToArray());
            Probes = Array.AsReadOnly((probes ?? Array.Empty<Sv5JumpMissProbe>())
                .OrderBy(value => value == null ? string.Empty : value.RecipeId, StringComparer.Ordinal)
                .ThenBy(value => value == null ? int.MaxValue : value.FailedLinkOrder).ToArray());
            Routes = Array.AsReadOnly((routes ?? Array.Empty<Sv5JumpRecoveryRoute>())
                .OrderBy(value => value == null ? string.Empty : value.RecipeId, StringComparer.Ordinal)
                .ThenBy(value => value == null ? int.MaxValue : value.FailedLinkOrder).ToArray());
            Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            JumpRecipeReady = jumpRecipeReady;
            ComposedGeometryReady = composedGeometryReady;
            SweptClearanceReady = sweptClearanceReady;
            RecoveryReady = Diagnostics.Count == 0 && Probes.Count == 18 && Probes.All(value => value.Caught) &&
                Routes.Count == 18 && Routes.All(value => value.RecoveryPass);
            Digest = Sv5JumpRecovery.Hash(string.Join("\n", new[] { InputClearanceDigest }
                .Concat(Overlay.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(Probes.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(Routes.Where(value => value != null).Select(value => value.DigestToken))));
        }

        public string InputClearanceDigest { get; }
        public IReadOnlyList<Sv5JumpRecoveryCell> Overlay { get; }
        public IReadOnlyList<Sv5JumpMissProbe> Probes { get; }
        public IReadOnlyList<Sv5JumpRecoveryRoute> Routes { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public bool JumpRecipeReady { get; }
        public bool ComposedGeometryReady { get; }
        public bool SweptClearanceReady { get; }
        public bool RecoveryReady { get; }
        public bool PlayerVerified { get { return false; } }
        public bool ReverseRequired { get { return false; } }
        public int WholeWorldBuilds { get { return 0; } }
        public int WholeWorldSearches { get { return 0; } }
        public int GlobalEndpointComparisons { get { return 0; } }
        public int OverlayGroupCount { get { return Overlay.Select(value => value.RecipeId + "|" + value.GroupId).Distinct().Count(); } }
        public int RecoveryLinkCount { get { return Routes.Sum(value => value.Links.Count); } }
        public int RecoveryTraceStateCount { get { return Routes.Sum(value => value.Links.Sum(link => link.Trace.Count)); } }
    }

    public static class Sv5JumpRecovery
    {
        public const int LocalWidth = 24;
        public const int LocalHeight = 32;
        public const int MaxTraceStatesPerLink = 16;
        public const string InputClearanceDigest = "48c3754b8109aa0601760567010b9aece67c5305907dfb76c7b155b3d4c37f13";

        private sealed class RouteSpec
        {
            public RouteSpec(int checkpointOrder, Sv5JumpRecoveryRouteKind kind, Sv5JumpRecoveryMode mode, params Sv5JumpPoint[] trace)
            {
                CheckpointOrder = checkpointOrder;
                Kind = kind;
                Mode = mode;
                Trace = trace ?? Array.Empty<Sv5JumpPoint>();
            }

            public int CheckpointOrder { get; }
            public Sv5JumpRecoveryRouteKind Kind { get; }
            public Sv5JumpRecoveryMode Mode { get; }
            public IReadOnlyList<Sv5JumpPoint> Trace { get; }
        }

        public static Sv5JumpRecoveryPlan CreateCanonicalLocalProof()
        {
            Sv5JumpClearancePlan clearance = Sv5JumpClearance.CreateCanonicalLocalProof();
            IReadOnlyList<Sv5JumpRecoveryCell> overlay = CreateCanonicalOverlay();
            IReadOnlyList<Sv5JumpMissProbe> probes = CreateCanonicalProbes(clearance, overlay);
            IReadOnlyList<Sv5JumpRecoveryRoute> routes = CreateCanonicalRoutes(clearance, probes);
            return Validate(clearance, overlay, probes, routes, recoveryReadyClaim: true);
        }

        public static Sv5JumpRecoveryPlan Validate(
            Sv5JumpClearancePlan clearance,
            IEnumerable<Sv5JumpRecoveryCell> overlay,
            IEnumerable<Sv5JumpMissProbe> probes,
            IEnumerable<Sv5JumpRecoveryRoute> routes,
            bool recoveryReadyClaim = false,
            bool playerVerifiedClaim = false)
        {
            var errors = new List<string>();
            var cells = (overlay ?? Array.Empty<Sv5JumpRecoveryCell>()).ToArray();
            var misses = (probes ?? Array.Empty<Sv5JumpMissProbe>()).ToArray();
            var recoveryRoutes = (routes ?? Array.Empty<Sv5JumpRecoveryRoute>()).ToArray();
            bool clearanceReady = clearance != null && clearance.SweptClearanceReady && clearance.Diagnostics.Count == 0 &&
                string.Equals(clearance.Digest, InputClearanceDigest, StringComparison.Ordinal);
            if (!clearanceReady) errors.Add(Sv5JumpRecoveryDiagnostic.ClearanceMismatch);

            Sv5JumpRecipeCatalogModel catalog = Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
            var recipes = catalog.Recipes.ToDictionary(value => value.RecipeId, StringComparer.Ordinal);
            var baseOccupied = recipes.ToDictionary(pair => pair.Key,
                pair => new HashSet<Sv5JumpPoint>(pair.Value.Occupancy.Select(value => value.Point)), StringComparer.Ordinal);
            var baseSolid = recipes.ToDictionary(pair => pair.Key,
                pair => new HashSet<Sv5JumpPoint>(pair.Value.Occupancy.Where(value => value.Collision == "SOLID")
                    .Select(value => value.Point)), StringComparer.Ordinal);

            var overlayByRecipe = new Dictionary<string, HashSet<Sv5JumpPoint>>(StringComparer.Ordinal);
            foreach (string recipeId in recipes.Keys) overlayByRecipe[recipeId] = new HashSet<Sv5JumpPoint>();
            var overlayByPoint = new Dictionary<string, Sv5JumpRecoveryCell>(StringComparer.Ordinal);
            foreach (Sv5JumpRecoveryCell cell in cells)
            {
                if (cell == null || !recipes.ContainsKey(cell.RecipeId) || !Inside(cell.Point) ||
                    (cell.Collision != "SOLID" && cell.Collision != "TOP_ONLY") || cell.GroupId == string.Empty ||
                    cell.OwnerId == string.Empty)
                {
                    errors.Add(Sv5JumpRecoveryDiagnostic.OverlayShape);
                    continue;
                }
                string pointKey = Key(cell.RecipeId, cell.Point);
                if (baseOccupied[cell.RecipeId].Contains(cell.Point) || !overlayByRecipe[cell.RecipeId].Add(cell.Point) ||
                    overlayByPoint.ContainsKey(pointKey))
                    errors.Add(Sv5JumpRecoveryDiagnostic.OverlayOccupancyCollision + ":" + pointKey);
                else
                    overlayByPoint[pointKey] = cell;
            }

            foreach (string recipeId in recipes.Keys)
            {
                Sv5JumpRecoveryCell[][] groups = cells.Where(value => value != null && value.RecipeId == recipeId)
                    .GroupBy(value => value.GroupId, StringComparer.Ordinal).Select(value => value.ToArray()).ToArray();
                if (groups.Length < 3 || groups.Length > 9 || overlayByRecipe[recipeId].Count > 36)
                    errors.Add(Sv5JumpRecoveryDiagnostic.OverlayDensity + ":" + recipeId);
                foreach (Sv5JumpRecoveryCell[] group in groups)
                {
                    int[] xs = group.Select(value => value.Point.X).OrderBy(value => value).ToArray();
                    if (group.Length < 2 || group.Length > 5 || group.Select(value => value.Point.Y).Distinct().Count() != 1 ||
                        xs.Distinct().Count() != xs.Length || !xs.SequenceEqual(Enumerable.Range(xs[0], xs.Length)) ||
                        group.Select(value => value.OwnerId).Distinct(StringComparer.Ordinal).Count() != 1)
                        errors.Add(Sv5JumpRecoveryDiagnostic.OverlayShape + ":" + recipeId + ":" + group[0].GroupId);
                }
            }

            if (clearance != null)
            {
                foreach (string recipeId in recipes.Keys)
                {
                    var mainFootprint = new HashSet<Sv5JumpPoint>(clearance.Links.Where(value => value.Trace.RecipeId == recipeId)
                        .SelectMany(value => value.Trace.Samples).SelectMany(value => new[] { value.Body, value.Head }));
                    if (overlayByRecipe[recipeId].Overlaps(mainFootprint))
                        errors.Add(Sv5JumpRecoveryDiagnostic.OverlayMainTraceOverlap + ":" + recipeId);
                    if (clearance.GrabContacts.Where(value => value.RecipeId == recipeId)
                        .Any(value => overlayByRecipe[recipeId].Contains(value.Contact)))
                        errors.Add(Sv5JumpRecoveryDiagnostic.OverlayGrabContactOverlap + ":" + recipeId);
                }
            }

            ValidateOverlayMirror(cells, errors);
            ValidateSolidFill(recipes, cells, baseSolid, errors);

            var combined = recipes.Keys.ToDictionary(value => value,
                value => new HashSet<Sv5JumpPoint>(baseOccupied[value].Concat(overlayByRecipe[value])), StringComparer.Ordinal);
            var sourceTraces = clearance == null ? new Dictionary<string, Sv5JumpClearanceTrace>(StringComparer.Ordinal) :
                clearance.Links.ToDictionary(value => Key(value.Trace.RecipeId, value.Trace.Order), value => value.Trace,
                    StringComparer.Ordinal);
            var probeByKey = misses.Where(value => value != null).GroupBy(value => Key(value.RecipeId, value.FailedLinkOrder),
                    StringComparer.Ordinal).ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            if (misses.Length != 18 || probeByKey.Count != 18 || !probeByKey.Keys.OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(sourceTraces.Keys.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
                errors.Add(Sv5JumpRecoveryDiagnostic.ProbeSetMismatch);

            var validLandings = new Dictionary<string, Sv5JumpPoint>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, Sv5JumpMissProbe> entry in probeByKey)
            {
                Sv5JumpClearanceTrace source;
                if (!sourceTraces.TryGetValue(entry.Key, out source) || !recipes.ContainsKey(entry.Value.RecipeId))
                {
                    errors.Add(Sv5JumpRecoveryDiagnostic.ProbeBindingMismatch + ":" + entry.Key);
                    continue;
                }
                Sv5JumpClearanceSample[] air = source.Samples.Where(value => value.Phase == Sv5JumpClearancePhase.Air)
                    .OrderBy(value => value.SampleOrder).ToArray();
                Sv5JumpClearanceSample selected = air.Length == 0 ? null : air[(air.Length - 1) / 2];
                if (selected == null || entry.Value.ProbeOrder != 0 || entry.Value.SourceSampleOrder != selected.SampleOrder ||
                    !entry.Value.Origin.Equals(selected.Body))
                    errors.Add(Sv5JumpRecoveryDiagnostic.ProbeOriginMismatch + ":" + entry.Key);
                if (entry.Value.SourceLinkId != source.SourceLinkId || !entry.Value.Caught)
                    errors.Add(Sv5JumpRecoveryDiagnostic.ProbeBindingMismatch + ":" + entry.Key);

                int? firstY = FirstCatchY(entry.Value.Origin, combined[entry.Value.RecipeId]);
                if (!firstY.HasValue)
                {
                    errors.Add(Sv5JumpRecoveryDiagnostic.MissingCatch + ":" + entry.Key);
                    continue;
                }
                Sv5JumpPoint expectedCatch = new Sv5JumpPoint(entry.Value.Origin.X, firstY.Value);
                if (!entry.Value.CatchPoint.Equals(expectedCatch))
                    errors.Add(Sv5JumpRecoveryDiagnostic.SkippedNearerCatch + ":" + entry.Key);
                Sv5JumpPoint expectedBody = new Sv5JumpPoint(expectedCatch.X, expectedCatch.Y + 1);
                Sv5JumpPoint expectedHead = new Sv5JumpPoint(expectedCatch.X, expectedCatch.Y + 2);
                if (!entry.Value.LandingBody.Equals(expectedBody) || combined[entry.Value.RecipeId].Contains(expectedBody))
                    errors.Add(Sv5JumpRecoveryDiagnostic.BlockedLandingBody + ":" + entry.Key);
                if (!entry.Value.LandingHead.Equals(expectedHead) || combined[entry.Value.RecipeId].Contains(expectedHead))
                    errors.Add(Sv5JumpRecoveryDiagnostic.BlockedLandingHead + ":" + entry.Key);
                if (entry.Value.FallDistance != entry.Value.Origin.Y - expectedBody.Y || entry.Value.FallDistance < 0)
                    errors.Add(Sv5JumpRecoveryDiagnostic.ProbeBindingMismatch + ":DISTANCE:" + entry.Key);
                Sv5JumpRecoveryCell catchCell;
                bool recoveryCatch = overlayByPoint.TryGetValue(Key(entry.Value.RecipeId, expectedCatch), out catchCell);
                if (entry.Value.CatchSource != (recoveryCatch ? "RECOVERY" : "BASE") ||
                    entry.Value.CatchGroupId != (recoveryCatch ? catchCell.GroupId : string.Empty))
                    errors.Add(Sv5JumpRecoveryDiagnostic.ProbeBindingMismatch + ":CATCH:" + entry.Key);
                for (int y = entry.Value.Origin.Y; y >= expectedBody.Y; y--)
                    if (combined[entry.Value.RecipeId].Contains(new Sv5JumpPoint(entry.Value.Origin.X, y)) ||
                        combined[entry.Value.RecipeId].Contains(new Sv5JumpPoint(entry.Value.Origin.X, y + 1)))
                        errors.Add(Sv5JumpRecoveryDiagnostic.BlockedFallRay + ":" + entry.Key + ":" + y);
                validLandings[entry.Key] = expectedBody;
            }

            var routeByKey = recoveryRoutes.Where(value => value != null)
                .GroupBy(value => Key(value.RecipeId, value.FailedLinkOrder), StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            if (recoveryRoutes.Length != 18 || routeByKey.Count != 18 || !routeByKey.Keys.OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(sourceTraces.Keys.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
                errors.Add(Sv5JumpRecoveryDiagnostic.RouteSetMismatch);

            foreach (KeyValuePair<string, Sv5JumpRecoveryRoute> entry in routeByKey)
            {
                Sv5JumpRecoveryRoute route = entry.Value;
                if (!recipes.ContainsKey(route.RecipeId)) continue;
                if (route.CheckpointLinkOrder < 0 || route.CheckpointLinkOrder > 8 || route.CheckpointLinkOrder > route.FailedLinkOrder)
                {
                    errors.Add(Sv5JumpRecoveryDiagnostic.LaterCheckpointShortcut + ":" + entry.Key);
                    continue;
                }
                Sv5JumpClearanceTrace checkpoint;
                Sv5JumpPoint start;
                if (!sourceTraces.TryGetValue(Key(route.RecipeId, route.CheckpointLinkOrder), out checkpoint) ||
                    !validLandings.TryGetValue(entry.Key, out start))
                {
                    errors.Add(Sv5JumpRecoveryDiagnostic.RouteEndpointMismatch + ":" + entry.Key);
                    continue;
                }
                if (!route.Start.Equals(start) || !route.End.Equals(checkpoint.EffectiveTakeoff))
                    errors.Add(Sv5JumpRecoveryDiagnostic.RouteEndpointMismatch + ":" + entry.Key);
                if (route.RouteId == string.Empty || !route.RecoveryPass || route.Diagnostic != string.Empty)
                    errors.Add(Sv5JumpRecoveryDiagnostic.RouteFlagsInvalid + ":" + entry.Key);
                if (route.ReverseRequired) errors.Add(Sv5JumpRecoveryDiagnostic.ReverseRequired + ":" + entry.Key);

                if (route.RouteKind == Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint)
                {
                    if (!route.Start.Equals(route.End) || route.Links.Count != 0)
                        errors.Add(Sv5JumpRecoveryDiagnostic.AlreadyAtCheckpointInvalid + ":" + entry.Key);
                    continue;
                }
                if (route.RouteKind != Sv5JumpRecoveryRouteKind.Linked || route.Links.Count == 0)
                {
                    errors.Add(Sv5JumpRecoveryDiagnostic.DisconnectedRoute + ":" + entry.Key);
                    continue;
                }
                if (!route.Links[0].Source.Equals(route.Start) || !route.Links[route.Links.Count - 1].Target.Equals(route.End))
                    errors.Add(Sv5JumpRecoveryDiagnostic.DisconnectedRoute + ":" + entry.Key);
                for (int index = 0; index < route.Links.Count; index++)
                {
                    Sv5JumpRecoveryLink link = route.Links[index];
                    if (link == null || link.LinkOrder != index || link.RecipeId != route.RecipeId || link.RouteId != route.RouteId)
                    {
                        errors.Add(Sv5JumpRecoveryDiagnostic.LinkDiscontinuity + ":" + entry.Key + ":" + index);
                        continue;
                    }
                    if (index > 0 && !route.Links[index - 1].Target.Equals(link.Source))
                        errors.Add(Sv5JumpRecoveryDiagnostic.LinkDiscontinuity + ":" + entry.Key + ":" + index);
                    ValidateRecoveryLink(link, combined[route.RecipeId], errors, entry.Key);
                }
            }

            foreach (string recipeId in recipes.Keys)
                if (routeByKey.Values.Where(value => value.RecipeId == recipeId).Select(value => value.CheckpointLinkOrder)
                    .Distinct().Count() < 2)
                    errors.Add(Sv5JumpRecoveryDiagnostic.CheckpointDiversity + ":" + recipeId);
            ValidateProbeRouteMirrors(probeByKey, routeByKey, errors);

            if (playerVerifiedClaim) errors.Add(Sv5JumpRecoveryDiagnostic.PlayerVerified);
            if (recoveryReadyClaim && errors.Count != 0) errors.Add(Sv5JumpRecoveryDiagnostic.FalseReadiness);
            return new Sv5JumpRecoveryPlan(clearance == null ? string.Empty : clearance.Digest, cells, misses,
                recoveryRoutes, errors, clearanceReady, clearanceReady, clearanceReady);
        }

        public static IReadOnlyList<Sv5JumpRecoveryCell> CreateCanonicalOverlay()
        {
            var cells = new List<Sv5JumpRecoveryCell>();
            AddRun(cells, Sv5JumpRecipeCatalog.R0RecipeId, "RG_ENTRY_CATCH", 3, 5, 1);
            AddRun(cells, Sv5JumpRecipeCatalog.R0RecipeId, "RG_MID_CATCH", 9, 11, 2);
            AddRun(cells, Sv5JumpRecipeCatalog.R0RecipeId, "RG_GRAB_CATCH", 16, 17, 4);
            foreach (Sv5JumpRecoveryCell source in cells.ToArray())
                cells.Add(new Sv5JumpRecoveryCell(Sv5JumpRecipeCatalog.MirrorRecipeId, source.GroupId,
                    new Sv5JumpPoint(23 - source.Point.X, source.Point.Y), source.Collision, source.OwnerId));
            return Array.AsReadOnly(cells.OrderBy(value => value).ToArray());
        }

        public static IReadOnlyList<Sv5JumpMissProbe> CreateCanonicalProbes(
            Sv5JumpClearancePlan clearance,
            IEnumerable<Sv5JumpRecoveryCell> overlay)
        {
            if (clearance == null) throw new ArgumentNullException(nameof(clearance));
            Sv5JumpRecipeCatalogModel catalog = Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
            Sv5JumpRecoveryCell[] cells = (overlay ?? Array.Empty<Sv5JumpRecoveryCell>()).ToArray();
            var probes = new List<Sv5JumpMissProbe>();
            foreach (Sv5JumpClearanceTrace trace in clearance.Links.Select(value => value.Trace)
                .OrderBy(value => value.RecipeId, StringComparer.Ordinal).ThenBy(value => value.Order))
            {
                Sv5JumpClearanceSample[] air = trace.Samples.Where(value => value.Phase == Sv5JumpClearancePhase.Air)
                    .OrderBy(value => value.SampleOrder).ToArray();
                if (air.Length == 0) throw new InvalidOperationException("Every main link must expose an AIR sample.");
                Sv5JumpClearanceSample selected = air[(air.Length - 1) / 2];
                var occupied = new HashSet<Sv5JumpPoint>(catalog.Recipe(trace.RecipeId).Occupancy.Select(value => value.Point)
                    .Concat(cells.Where(value => value.RecipeId == trace.RecipeId).Select(value => value.Point)));
                int? catchY = FirstCatchY(selected.Body, occupied);
                if (!catchY.HasValue) throw new InvalidOperationException("Canonical recovery overlay missed " + trace.RecipeId + ":" + trace.SourceLinkId);
                Sv5JumpPoint catchPoint = new Sv5JumpPoint(selected.Body.X, catchY.Value);
                Sv5JumpRecoveryCell recovery = cells.SingleOrDefault(value => value.RecipeId == trace.RecipeId && value.Point.Equals(catchPoint));
                Sv5JumpPoint body = new Sv5JumpPoint(catchPoint.X, catchPoint.Y + 1);
                probes.Add(new Sv5JumpMissProbe(trace.RecipeId, trace.Order, trace.SourceLinkId, selected.SampleOrder,
                    selected.Body, recovery == null ? "BASE" : "RECOVERY", recovery == null ? string.Empty : recovery.GroupId,
                    catchPoint, body, new Sv5JumpPoint(body.X, body.Y + 1), selected.Body.Y - body.Y, true));
            }
            return Array.AsReadOnly(probes.ToArray());
        }

        public static IReadOnlyList<Sv5JumpRecoveryRoute> CreateCanonicalRoutes(
            Sv5JumpClearancePlan clearance,
            IEnumerable<Sv5JumpMissProbe> probes)
        {
            if (clearance == null) throw new ArgumentNullException(nameof(clearance));
            var result = new List<Sv5JumpRecoveryRoute>();
            foreach (Sv5JumpMissProbe probe in (probes ?? Array.Empty<Sv5JumpMissProbe>())
                .OrderBy(value => value.RecipeId, StringComparer.Ordinal).ThenBy(value => value.FailedLinkOrder))
            {
                bool mirror = probe.RecipeId == Sv5JumpRecipeCatalog.MirrorRecipeId;
                RouteSpec spec = RouteSpecs()[probe.FailedLinkOrder];
                Sv5JumpPoint endpoint = clearance.Links.Single(value => value.Trace.RecipeId == probe.RecipeId &&
                    value.Trace.Order == spec.CheckpointOrder).Trace.EffectiveTakeoff;
                string routeId = "RECOVERY_ROUTE_" + probe.FailedLinkOrder.ToString("00");
                var links = new List<Sv5JumpRecoveryLink>();
                if (spec.Kind == Sv5JumpRecoveryRouteKind.Linked)
                {
                    Sv5JumpPoint[] points = spec.Trace.Select(value => mirror ? new Sv5JumpPoint(23 - value.X, value.Y) : value).ToArray();
                    var trace = points.Select((point, index) => new Sv5JumpRecoveryTraceSample(index,
                        index == 0 ? Sv5JumpRecoveryPhase.Source : index + 1 == points.Length
                            ? Sv5JumpRecoveryPhase.Target : Sv5JumpRecoveryPhase.Transit, point));
                    links.Add(new Sv5JumpRecoveryLink(probe.RecipeId, routeId, 0, routeId + "_LINK_00",
                        spec.Mode, points[0], points[points.Length - 1], trace));
                }
                result.Add(new Sv5JumpRecoveryRoute(probe.RecipeId, routeId, probe.FailedLinkOrder,
                    spec.CheckpointOrder, spec.Kind, probe.LandingBody, endpoint, links));
            }
            return Array.AsReadOnly(result.ToArray());
        }

        public static string ModeName(Sv5JumpRecoveryMode value)
        {
            switch (value)
            {
                case Sv5JumpRecoveryMode.Walk: return "WALK";
                case Sv5JumpRecoveryMode.Jump: return "JUMP";
                case Sv5JumpRecoveryMode.Drop: return "DROP";
                default: return "INVALID";
            }
        }

        public static string RouteKindName(Sv5JumpRecoveryRouteKind value)
        {
            return value == Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint ? "ALREADY_AT_CHECKPOINT" : "LINKED";
        }

        public static string PhaseName(Sv5JumpRecoveryPhase value)
        {
            switch (value)
            {
                case Sv5JumpRecoveryPhase.Source: return "SOURCE";
                case Sv5JumpRecoveryPhase.Target: return "TARGET";
                default: return "TRANSIT";
            }
        }

        internal static string Hash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)))
                    .Replace("-", string.Empty).ToLowerInvariant();
        }

        private static RouteSpec[] RouteSpecs()
        {
            return new[]
            {
                new RouteSpec(0, Sv5JumpRecoveryRouteKind.Linked, Sv5JumpRecoveryMode.Walk,
                    P(4,2), P(3,2), P(2,2)),
                new RouteSpec(1, Sv5JumpRecoveryRouteKind.Linked, Sv5JumpRecoveryMode.Walk,
                    P(10,3), P(9,3), P(8,3)),
                new RouteSpec(2, Sv5JumpRecoveryRouteKind.Linked, Sv5JumpRecoveryMode.Drop,
                    P(14,7), P(13,7), P(13,6), P(13,5), P(13,4)),
                new RouteSpec(3, Sv5JumpRecoveryRouteKind.Linked, Sv5JumpRecoveryMode.Walk,
                    P(17,5), P(18,5)),
                new RouteSpec(2, Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint, Sv5JumpRecoveryMode.Walk),
                new RouteSpec(1, Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint, Sv5JumpRecoveryMode.Walk),
                new RouteSpec(6, Sv5JumpRecoveryRouteKind.Linked, Sv5JumpRecoveryMode.Drop,
                    P(4,10), P(5,10), P(5,9), P(5,8)),
                new RouteSpec(0, Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint, Sv5JumpRecoveryMode.Walk),
                new RouteSpec(6, Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint, Sv5JumpRecoveryMode.Walk),
            };
        }

        private static void AddRun(ICollection<Sv5JumpRecoveryCell> cells, string recipeId, string groupId, int minX, int maxX, int y)
        {
            for (int x = minX; x <= maxX; x++)
                cells.Add(new Sv5JumpRecoveryCell(recipeId, groupId, new Sv5JumpPoint(x, y), "TOP_ONLY", groupId));
        }

        private static void ValidateOverlayMirror(IEnumerable<Sv5JumpRecoveryCell> cells, ICollection<string> errors)
        {
            var r0 = new HashSet<string>(cells.Where(value => value != null && value.RecipeId == Sv5JumpRecipeCatalog.R0RecipeId)
                .Select(value => (23 - value.Point.X) + ":" + value.Point.Y + "|" + value.Collision + "|" + value.GroupId));
            var mx = new HashSet<string>(cells.Where(value => value != null && value.RecipeId == Sv5JumpRecipeCatalog.MirrorRecipeId)
                .Select(value => value.Point.X + ":" + value.Point.Y + "|" + value.Collision + "|" + value.GroupId));
            if (!r0.SetEquals(mx)) errors.Add(Sv5JumpRecoveryDiagnostic.OverlayMirrorMismatch);
        }

        private static void ValidateSolidFill(
            IReadOnlyDictionary<string, Sv5JumpRecipeVariant> recipes,
            IEnumerable<Sv5JumpRecoveryCell> cells,
            IReadOnlyDictionary<string, HashSet<Sv5JumpPoint>> baseSolid,
            ICollection<string> errors)
        {
            foreach (string recipeId in recipes.Keys)
            {
                var solid = new HashSet<Sv5JumpPoint>(baseSolid[recipeId]);
                solid.UnionWith(cells.Where(value => value != null && value.RecipeId == recipeId && value.Collision == "SOLID")
                    .Select(value => value.Point));
                for (int x = 0; x <= LocalWidth - 6; x++)
                    for (int y = 0; y <= LocalHeight - 6; y++)
                        if (Enumerable.Range(x, 6).SelectMany(xx => Enumerable.Range(y, 6)
                            .Select(yy => new Sv5JumpPoint(xx, yy))).All(solid.Contains))
                            errors.Add(Sv5JumpRecoveryDiagnostic.SolidFillSixBySix + ":" + recipeId + ":" + x + ":" + y);
            }
        }

        private static void ValidateRecoveryLink(
            Sv5JumpRecoveryLink link,
            ISet<Sv5JumpPoint> occupied,
            ICollection<string> errors,
            string routeKey)
        {
            if (!Enum.IsDefined(typeof(Sv5JumpRecoveryMode), link.Mode))
                errors.Add(Sv5JumpRecoveryDiagnostic.InvalidMovementMode + ":" + routeKey);
            if (link.Mode == Sv5JumpRecoveryMode.Walk && link.Rise != 0)
                errors.Add(Sv5JumpRecoveryDiagnostic.InvalidMovementMode + ":WALK:" + routeKey);
            if (link.Mode == Sv5JumpRecoveryMode.Jump && link.Rise > 1)
                errors.Add(Sv5JumpRecoveryDiagnostic.JumpRiseExceeded + ":" + routeKey);
            if (link.Mode == Sv5JumpRecoveryMode.Drop && link.Rise > 0)
                errors.Add(Sv5JumpRecoveryDiagnostic.RisingDrop + ":" + routeKey);
            foreach (Sv5JumpPoint endpoint in new[] { link.Source, link.Target })
                if (!occupied.Contains(new Sv5JumpPoint(endpoint.X, endpoint.Y - 1)))
                    errors.Add(Sv5JumpRecoveryDiagnostic.UnsupportedEndpoint + ":" + routeKey + ":" + endpoint);
            if (link.Trace.Count == 0 || link.Trace.Count > MaxTraceStatesPerLink)
            {
                errors.Add(Sv5JumpRecoveryDiagnostic.TraceCountInvalid + ":" + routeKey);
                return;
            }
            if (link.Trace[0] == null || link.Trace[link.Trace.Count - 1] == null ||
                !link.Trace[0].Body.Equals(link.Source) || !link.Trace[link.Trace.Count - 1].Body.Equals(link.Target) ||
                link.Trace[0].Phase != Sv5JumpRecoveryPhase.Source ||
                link.Trace[link.Trace.Count - 1].Phase != Sv5JumpRecoveryPhase.Target)
                errors.Add(Sv5JumpRecoveryDiagnostic.TraceEndpointMismatch + ":" + routeKey);
            for (int index = 0; index < link.Trace.Count; index++)
            {
                Sv5JumpRecoveryTraceSample sample = link.Trace[index];
                if (sample == null || sample.SampleOrder != index || !Inside(sample.Body))
                {
                    errors.Add(Sv5JumpRecoveryDiagnostic.TraceBodyBlocked + ":" + routeKey + ":" + index);
                    continue;
                }
                if (occupied.Contains(sample.Body)) errors.Add(Sv5JumpRecoveryDiagnostic.TraceBodyBlocked + ":" + routeKey + ":" + index);
                if (!Inside(sample.Head) || !sample.Head.Equals(new Sv5JumpPoint(sample.Body.X, sample.Body.Y + 1)) ||
                    occupied.Contains(sample.Head))
                    errors.Add(Sv5JumpRecoveryDiagnostic.TraceHeadBlocked + ":" + routeKey + ":" + index);
                if (index + 1 >= link.Trace.Count) continue;
                Sv5JumpRecoveryTraceSample next = link.Trace[index + 1];
                if (next == null) continue;
                int dx = next.Body.X - sample.Body.X;
                int dy = next.Body.Y - sample.Body.Y;
                if ((dx == 0 && dy == 0) || Math.Abs(dx) > 1 || Math.Abs(dy) > 1)
                    errors.Add(Sv5JumpRecoveryDiagnostic.TraceNonLocalStep + ":" + routeKey + ":" + index);
                if (dx != 0 && dy != 0)
                    foreach (Sv5JumpPoint corner in new[]
                    {
                        new Sv5JumpPoint(next.Body.X, sample.Body.Y),
                        new Sv5JumpPoint(sample.Body.X, next.Body.Y),
                    })
                        if (occupied.Contains(corner) || occupied.Contains(new Sv5JumpPoint(corner.X, corner.Y + 1)))
                            errors.Add(Sv5JumpRecoveryDiagnostic.TraceDiagonalClip + ":" + routeKey + ":" + index);
            }
        }

        private static void ValidateProbeRouteMirrors(
            IReadOnlyDictionary<string, Sv5JumpMissProbe> probes,
            IReadOnlyDictionary<string, Sv5JumpRecoveryRoute> routes,
            ICollection<string> errors)
        {
            for (int order = 0; order < 9; order++)
            {
                Sv5JumpMissProbe a;
                Sv5JumpMissProbe b;
                Sv5JumpRecoveryRoute ra;
                Sv5JumpRecoveryRoute rb;
                if (!probes.TryGetValue(Key(Sv5JumpRecipeCatalog.R0RecipeId, order), out a) ||
                    !probes.TryGetValue(Key(Sv5JumpRecipeCatalog.MirrorRecipeId, order), out b) ||
                    !routes.TryGetValue(Key(Sv5JumpRecipeCatalog.R0RecipeId, order), out ra) ||
                    !routes.TryGetValue(Key(Sv5JumpRecipeCatalog.MirrorRecipeId, order), out rb))
                    continue;
                if (!Mirror(a.Origin, b.Origin) || !Mirror(a.CatchPoint, b.CatchPoint) ||
                    !Mirror(a.LandingBody, b.LandingBody) || ra.CheckpointLinkOrder != rb.CheckpointLinkOrder ||
                    ra.RouteKind != rb.RouteKind || !Mirror(ra.Start, rb.Start) || !Mirror(ra.End, rb.End) ||
                    ra.Links.Count != rb.Links.Count)
                {
                    errors.Add(Sv5JumpRecoveryDiagnostic.MirrorMismatch + ":" + order);
                    continue;
                }
                for (int linkIndex = 0; linkIndex < ra.Links.Count; linkIndex++)
                {
                    Sv5JumpRecoveryLink la = ra.Links[linkIndex];
                    Sv5JumpRecoveryLink lb = rb.Links[linkIndex];
                    if (la.Mode != lb.Mode || !Mirror(la.Source, lb.Source) || !Mirror(la.Target, lb.Target) ||
                        la.Trace.Count != lb.Trace.Count)
                        errors.Add(Sv5JumpRecoveryDiagnostic.MirrorMismatch + ":LINK:" + order + ":" + linkIndex);
                    for (int sample = 0; sample < Math.Min(la.Trace.Count, lb.Trace.Count); sample++)
                        if (!Mirror(la.Trace[sample].Body, lb.Trace[sample].Body) || la.Trace[sample].Phase != lb.Trace[sample].Phase)
                            errors.Add(Sv5JumpRecoveryDiagnostic.MirrorMismatch + ":TRACE:" + order + ":" + sample);
                }
            }
        }

        private static int? FirstCatchY(Sv5JumpPoint origin, ISet<Sv5JumpPoint> occupied)
        {
            for (int y = origin.Y - 1; y >= 0; y--)
                if (occupied.Contains(new Sv5JumpPoint(origin.X, y))) return y;
            return null;
        }

        private static bool Inside(Sv5JumpPoint point)
        {
            return point.X >= 0 && point.X < LocalWidth && point.Y >= 0 && point.Y < LocalHeight;
        }

        private static bool Mirror(Sv5JumpPoint r0, Sv5JumpPoint mx)
        {
            return mx.X == 23 - r0.X && mx.Y == r0.Y;
        }

        private static string Key(string recipeId, int order)
        {
            return (recipeId ?? string.Empty) + "|" + order;
        }

        private static string Key(string recipeId, Sv5JumpPoint point)
        {
            return (recipeId ?? string.Empty) + "|" + point.X + "|" + point.Y;
        }

        private static Sv5JumpPoint P(int x, int y)
        {
            return new Sv5JumpPoint(x, y);
        }
    }
}
