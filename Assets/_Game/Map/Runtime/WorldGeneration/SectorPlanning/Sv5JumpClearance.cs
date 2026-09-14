using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5JumpClearancePhase
    {
        Takeoff,
        Air,
        Hang,
        PullUp,
        Landing,
    }

    public static class Sv5JumpClearanceDiagnostic
    {
        public const string CatalogMismatch = "SV5_17_CATALOG_DIGEST_MISMATCH";
        public const string LinkSetMismatch = "CLEARANCE_LINK_SET_MUST_MATCH_EIGHTEEN_SOURCE_LINKS";
        public const string SourceBindingMismatch = "SOURCE_LINK_BINDING_MISMATCH";
        public const string EffectiveTakeoffMismatch = "EFFECTIVE_TAKEOFF_CORRECTION_MISMATCH";
        public const string BlockedBody = "TRACE_BODY_CELL_BLOCKED";
        public const string BlockedHead = "TRACE_HEAD_CELL_BLOCKED";
        public const string DiagonalCornerClip = "TRACE_DIAGONAL_SUPERCOVER_BLOCKED";
        public const string WrongEndpoint = "TRACE_ENDPOINT_MISMATCH";
        public const string HorizontalReversal = "TRACE_HORIZONTAL_DIRECTION_REVERSED";
        public const string NonLocalStep = "TRACE_STEP_EXCEEDS_LOCAL_CELL";
        public const string DuplicateState = "TRACE_DUPLICATE_STATE";
        public const string OverlongTrace = "TRACE_STATE_COUNT_EXCEEDS_SIXTEEN";
        public const string ExcessiveApex = "TRACE_APEX_EXCEEDS_THREE_CELL_MARGIN";
        public const string PhaseMismatch = "TRACE_PHASE_SEQUENCE_MISMATCH";
        public const string MissingGrabContact = "JUMP_GRAB_CONTACT_MISSING";
        public const string WrongGrabFace = "JUMP_GRAB_FACE_OR_CONTACT_MISMATCH";
        public const string MissingGrabPhases = "JUMP_GRAB_HANG_PULL_UP_MISSING";
        public const string GrabClearanceMismatch = "JUMP_GRAB_CLEARANCE_CELL_MISMATCH";
        public const string BadMirror = "MIRROR_X_TRACE_MISMATCH";
        public const string FalseReadiness = "RECOVERY_OR_PLAYER_READINESS_FORBIDDEN";
    }

    public sealed class Sv5JumpClearanceSample
    {
        public Sv5JumpClearanceSample(int sampleOrder, Sv5JumpClearancePhase phase, Sv5JumpPoint body)
            : this(sampleOrder, phase, body, new Sv5JumpPoint(body.X, body.Y + 1))
        {
        }

        public Sv5JumpClearanceSample(
            int sampleOrder,
            Sv5JumpClearancePhase phase,
            Sv5JumpPoint body,
            Sv5JumpPoint head)
        {
            SampleOrder = sampleOrder;
            Phase = phase;
            Body = body;
            Head = head;
        }

        public int SampleOrder { get; }
        public Sv5JumpClearancePhase Phase { get; }
        public Sv5JumpPoint Foot { get { return Body; } }
        public Sv5JumpPoint Body { get; }
        public Sv5JumpPoint Head { get; }

        internal string DigestToken
        {
            get { return SampleOrder + "|" + Sv5JumpClearance.PhaseName(Phase) + "|" + Body + "|" + Head; }
        }
    }

    public sealed class Sv5JumpClearanceTrace
    {
        public Sv5JumpClearanceTrace(
            string recipeId,
            int order,
            string sourceLinkId,
            Sv5JumpMode mode,
            Sv5JumpDirection direction,
            Sv5JumpPoint sourceTakeoff,
            Sv5JumpPoint effectiveTakeoff,
            Sv5JumpPoint landing,
            bool endpointAdjusted,
            string correctionId,
            IEnumerable<Sv5JumpClearanceSample> samples)
        {
            RecipeId = recipeId ?? string.Empty;
            Order = order;
            SourceLinkId = sourceLinkId ?? string.Empty;
            Mode = mode;
            Direction = direction;
            SourceTakeoff = sourceTakeoff;
            EffectiveTakeoff = effectiveTakeoff;
            Landing = landing;
            EndpointAdjusted = endpointAdjusted;
            CorrectionId = correctionId ?? string.Empty;
            Samples = Array.AsReadOnly((samples ?? Array.Empty<Sv5JumpClearanceSample>()).ToArray());
        }

        public string RecipeId { get; }
        public int Order { get; }
        public string SourceLinkId { get; }
        public Sv5JumpMode Mode { get; }
        public Sv5JumpDirection Direction { get; }
        public Sv5JumpPoint SourceTakeoff { get; }
        public Sv5JumpPoint EffectiveTakeoff { get; }
        public Sv5JumpPoint Landing { get; }
        public bool EndpointAdjusted { get; }
        public string CorrectionId { get; }
        public IReadOnlyList<Sv5JumpClearanceSample> Samples { get; }

        internal string DigestToken
        {
            get
            {
                return string.Join("|", new[]
                {
                    RecipeId, Order.ToString(), SourceLinkId, Sv5JumpClearance.ModeName(Mode),
                    Sv5JumpClearance.DirectionName(Direction), SourceTakeoff.ToString(), EffectiveTakeoff.ToString(),
                    Landing.ToString(), EndpointAdjusted ? "true" : "false", CorrectionId,
                    string.Join(";", Samples.Where(value => value != null).Select(value => value.DigestToken))
                });
            }
        }
    }

    public sealed class Sv5JumpClearanceGrabContact
    {
        public Sv5JumpClearanceGrabContact(
            string recipeId,
            string sourceLinkId,
            string sourceGrabEdgeId,
            string targetSupportId,
            Sv5JumpPoint contact,
            Sv5JumpGrabFace face,
            Sv5JumpPoint hangBody,
            Sv5JumpPoint hangHead,
            Sv5JumpPoint pullUp,
            Sv5JumpPoint pullUpHead)
        {
            RecipeId = recipeId ?? string.Empty;
            SourceLinkId = sourceLinkId ?? string.Empty;
            SourceGrabEdgeId = sourceGrabEdgeId ?? string.Empty;
            TargetSupportId = targetSupportId ?? string.Empty;
            Contact = contact;
            Face = face;
            HangBody = hangBody;
            HangHead = hangHead;
            PullUp = pullUp;
            PullUpHead = pullUpHead;
        }

        public string RecipeId { get; }
        public string SourceLinkId { get; }
        public string SourceGrabEdgeId { get; }
        public string TargetSupportId { get; }
        public Sv5JumpPoint Contact { get; }
        public Sv5JumpGrabFace Face { get; }
        public Sv5JumpPoint HangBody { get; }
        public Sv5JumpPoint HangHead { get; }
        public Sv5JumpPoint PullUp { get; }
        public Sv5JumpPoint PullUpHead { get; }

        internal string DigestToken
        {
            get
            {
                return RecipeId + "|" + SourceLinkId + "|" + SourceGrabEdgeId + "|" + TargetSupportId + "|" +
                    Contact + "|" + Sv5JumpClearance.FaceName(Face) + "|" + HangBody + "|" + HangHead + "|" +
                    PullUp + "|" + PullUpHead;
            }
        }
    }

    public sealed class Sv5JumpClearanceLinkResult
    {
        internal Sv5JumpClearanceLinkResult(Sv5JumpClearanceTrace trace, IEnumerable<string> diagnostics)
        {
            Trace = trace;
            Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public Sv5JumpClearanceTrace Trace { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public bool ClearancePass { get { return Diagnostics.Count == 0; } }
    }

    public sealed class Sv5JumpClearancePlan
    {
        internal Sv5JumpClearancePlan(
            string inputCatalogDigest,
            IEnumerable<Sv5JumpClearanceLinkResult> links,
            IEnumerable<Sv5JumpClearanceGrabContact> grabContacts,
            IEnumerable<string> diagnostics,
            bool jumpRecipeReady,
            bool composedGeometryReady)
        {
            InputCatalogDigest = inputCatalogDigest ?? string.Empty;
            Links = Array.AsReadOnly((links ?? Array.Empty<Sv5JumpClearanceLinkResult>())
                .OrderBy(value => value == null || value.Trace == null ? string.Empty : value.Trace.RecipeId, StringComparer.Ordinal)
                .ThenBy(value => value == null || value.Trace == null ? int.MaxValue : value.Trace.Order).ToArray());
            GrabContacts = Array.AsReadOnly((grabContacts ?? Array.Empty<Sv5JumpClearanceGrabContact>())
                .OrderBy(value => value == null ? string.Empty : value.RecipeId, StringComparer.Ordinal).ToArray());
            Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            JumpRecipeReady = jumpRecipeReady;
            ComposedGeometryReady = composedGeometryReady;
            SweptClearanceReady = Diagnostics.Count == 0 && Links.Count == 18 && Links.All(value => value.ClearancePass);
            Digest = Sv5JumpClearance.Hash(string.Join("\n", new[]
            {
                InputCatalogDigest,
                Sv5JumpClearance.EndpointCorrectionId,
                Sv5JumpClearance.EffectiveLinkEndpointDigest,
            }.Concat(Links.Where(value => value != null && value.Trace != null).Select(value => value.Trace.DigestToken))
                .Concat(GrabContacts.Where(value => value != null).Select(value => value.DigestToken))));
        }

        public string InputCatalogDigest { get; }
        public IReadOnlyList<Sv5JumpClearanceLinkResult> Links { get; }
        public IReadOnlyList<Sv5JumpClearanceGrabContact> GrabContacts { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public bool JumpRecipeReady { get; }
        public bool ComposedGeometryReady { get; }
        public bool SweptClearanceReady { get; }
        public bool RecoveryReady { get { return false; } }
        public bool PlayerVerified { get { return false; } }
        public int WholeWorldBuilds { get { return 0; } }
        public int WholeWorldSearches { get { return 0; } }
        public int GlobalEndpointComparisons { get { return 0; } }
        public int EndpointCorrectionCount { get { return Links.Count(value => value.Trace.EndpointAdjusted); } }
    }

    public static class Sv5JumpClearance
    {
        public const int LocalWidth = 24;
        public const int LocalHeight = 32;
        public const int MaxTraceStates = 16;
        public const int MaxApexAboveHigherEndpoint = 3;
        public const string InputCatalogDigest = "19b4b3c37a1b1b11856eb1c946ce8ea64cbc16517dbfe9ace2ae66e4a5504022";
        public const string EndpointCorrectionId = "SV5_18_ENDPOINT_FIX01";
        public const string EffectiveLinkEndpointDigest = "9ca58b3070662afece14b812aff892ec8e31bac09938a7445a667f3b2aa8db78";
        public const string GrabLinkId = "JS_LINK_03";

        private static readonly Sv5JumpPoint[][] R0TracePoints =
        {
            Points(2,2, 3,3, 4,3, 5,3, 6,3),
            Points(8,3, 9,4, 10,4, 11,4, 12,4),
            Points(13,4, 13,5, 13,6, 13,7, 14,8, 15,8, 16,7, 17,6, 18,5),
            Points(18,5, 17,6, 16,6, 15,7),
            Points(14,7, 13,8, 12,8, 11,7),
            Points(9,7, 8,8, 7,8, 6,8),
            Points(5,8, 5,9, 5,10, 4,11, 3,11, 2,10, 1,9),
            Points(1,9, 2,10, 3,10),
            Points(4,10, 5,11, 6,12, 7,11),
        };

        public static Sv5JumpClearancePlan CreateCanonicalLocalProof()
        {
            Sv5JumpRecipeCatalogModel catalog = Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
            return Validate(catalog, CreateCanonicalTraces(catalog), CreateCanonicalGrabContacts(catalog));
        }

        public static Sv5JumpClearancePlan Validate(
            Sv5JumpRecipeCatalogModel catalog,
            IEnumerable<Sv5JumpClearanceTrace> traces,
            IEnumerable<Sv5JumpClearanceGrabContact> grabContacts,
            bool recoveryReadyClaim = false,
            bool playerVerifiedClaim = false)
        {
            var global = new List<string>();
            var rawTraces = (traces ?? Array.Empty<Sv5JumpClearanceTrace>()).ToArray();
            var rawGrabs = (grabContacts ?? Array.Empty<Sv5JumpClearanceGrabContact>()).ToArray();
            bool catalogReady = catalog != null && catalog.Diagnostics.Count == 0 &&
                string.Equals(catalog.Digest, InputCatalogDigest, StringComparison.Ordinal);
            if (!catalogReady) global.Add(Sv5JumpClearanceDiagnostic.CatalogMismatch);
            if (recoveryReadyClaim || playerVerifiedClaim) global.Add(Sv5JumpClearanceDiagnostic.FalseReadiness);

            var sourceLinks = new Dictionary<string, Sv5JumpRecipeLink>(StringComparer.Ordinal);
            var recipes = new Dictionary<string, Sv5JumpRecipeVariant>(StringComparer.Ordinal);
            if (catalog != null)
            {
                foreach (Sv5JumpRecipeVariant recipe in catalog.Recipes.Where(value => value != null))
                {
                    recipes[recipe.RecipeId] = recipe;
                    foreach (Sv5JumpRecipeLink link in recipe.RouteLinks.Where(value => value != null && value.Link != null))
                        sourceLinks[Key(recipe.RecipeId, link.Link.Order)] = link;
                }
            }

            var traceByKey = rawTraces.Where(value => value != null)
                .GroupBy(value => Key(value.RecipeId, value.Order), StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            if (rawTraces.Length != 18 || traceByKey.Count != 18 || !traceByKey.Keys.OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(sourceLinks.Keys.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
                global.Add(Sv5JumpClearanceDiagnostic.LinkSetMismatch);

            var linkDiagnostics = traceByKey.Keys.ToDictionary(value => value, value => new List<string>(), StringComparer.Ordinal);
            foreach (KeyValuePair<string, Sv5JumpClearanceTrace> entry in traceByKey)
            {
                Sv5JumpRecipeLink source;
                Sv5JumpRecipeVariant recipe;
                if (!sourceLinks.TryGetValue(entry.Key, out source) || !recipes.TryGetValue(entry.Value.RecipeId, out recipe))
                {
                    linkDiagnostics[entry.Key].Add(Sv5JumpClearanceDiagnostic.SourceBindingMismatch);
                    continue;
                }
                ValidateTrace(entry.Value, source, recipe, linkDiagnostics[entry.Key]);
            }

            ValidateGrabContacts(rawGrabs, recipes, traceByKey, linkDiagnostics, global);
            ValidateMirrors(traceByKey, global);

            var results = traceByKey.OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => new Sv5JumpClearanceLinkResult(value.Value, linkDiagnostics[value.Key])).ToArray();
            global.AddRange(results.SelectMany(value => value.Diagnostics.Select(error =>
                value.Trace.RecipeId + ":" + value.Trace.SourceLinkId + ":" + error)));
            return new Sv5JumpClearancePlan(catalog == null ? string.Empty : catalog.Digest, results, rawGrabs,
                global, catalogReady, catalogReady);
        }

        public static IReadOnlyList<Sv5JumpClearanceTrace> CreateCanonicalTraces(Sv5JumpRecipeCatalogModel catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var traces = new List<Sv5JumpClearanceTrace>();
            foreach (Sv5JumpRecipeVariant recipe in catalog.Recipes.OrderBy(value => value.RecipeId, StringComparer.Ordinal))
            {
                foreach (Sv5JumpRecipeLink row in recipe.RouteLinks.OrderBy(value => value.Link.Order))
                {
                    int order = row.Link.Order;
                    Sv5JumpPoint[] points = R0TracePoints[order].Select(value => recipe.Transform == Sv5JumpRecipeTransform.R0
                        ? value : new Sv5JumpPoint(23 - value.X, value.Y)).ToArray();
                    var samples = new List<Sv5JumpClearanceSample>();
                    for (int index = 0; index < points.Length; index++)
                    {
                        Sv5JumpClearancePhase phase = index == 0 ? Sv5JumpClearancePhase.Takeoff :
                            index + 1 == points.Length ? (row.Link.Mode == Sv5JumpMode.JumpGrab
                                ? Sv5JumpClearancePhase.PullUp : Sv5JumpClearancePhase.Landing) :
                            row.Link.Mode == Sv5JumpMode.JumpGrab && index + 2 == points.Length
                                ? Sv5JumpClearancePhase.Hang : Sv5JumpClearancePhase.Air;
                        samples.Add(new Sv5JumpClearanceSample(index, phase, points[index]));
                    }
                    Sv5JumpPoint effective = points[0];
                    bool adjusted = !effective.Equals(row.Link.Takeoff);
                    traces.Add(new Sv5JumpClearanceTrace(recipe.RecipeId, order, row.SourceLinkId, row.Link.Mode,
                        row.Link.Direction, row.Link.Takeoff, effective, row.Link.Landing, adjusted,
                        adjusted ? EndpointCorrectionId : string.Empty, samples));
                }
            }
            return Array.AsReadOnly(traces.ToArray());
        }

        public static IReadOnlyList<Sv5JumpClearanceGrabContact> CreateCanonicalGrabContacts(Sv5JumpRecipeCatalogModel catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            return Array.AsReadOnly(catalog.Recipes.OrderBy(value => value.RecipeId, StringComparer.Ordinal).Select(recipe =>
            {
                Sv5JumpRecipeGrab grab = recipe.GrabEdges.Single();
                return new Sv5JumpClearanceGrabContact(recipe.RecipeId, GrabLinkId, grab.SourceGrabEdgeId,
                    grab.Edge.SupportId, grab.Edge.Contact, grab.Edge.Face, grab.Edge.HangBody, grab.HangHead,
                    grab.Edge.PullUp, grab.PullUpHead);
            }).ToArray());
        }

        public static string PhaseName(Sv5JumpClearancePhase value)
        {
            switch (value)
            {
                case Sv5JumpClearancePhase.Takeoff: return "TAKEOFF";
                case Sv5JumpClearancePhase.Air: return "AIR";
                case Sv5JumpClearancePhase.Hang: return "HANG";
                case Sv5JumpClearancePhase.PullUp: return "PULL_UP";
                default: return "LANDING";
            }
        }

        public static string ModeName(Sv5JumpMode value)
        {
            return value == Sv5JumpMode.JumpGrab ? "JUMP_GRAB" : "JUMP";
        }

        public static string DirectionName(Sv5JumpDirection value)
        {
            return value == Sv5JumpDirection.LeftToRight ? "LEFT_TO_RIGHT" : "RIGHT_TO_LEFT";
        }

        public static string FaceName(Sv5JumpGrabFace value)
        {
            return value == Sv5JumpGrabFace.Left ? "LEFT" : "RIGHT";
        }

        internal static string Hash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)))
                    .Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void ValidateTrace(
            Sv5JumpClearanceTrace trace,
            Sv5JumpRecipeLink source,
            Sv5JumpRecipeVariant recipe,
            ICollection<string> diagnostics)
        {
            Sv5JumpGrabRouteLink link = source.Link;
            if (trace.SourceLinkId != source.SourceLinkId || trace.Mode != link.Mode || trace.Direction != link.Direction ||
                !trace.SourceTakeoff.Equals(link.Takeoff) || !trace.Landing.Equals(link.Landing))
                diagnostics.Add(Sv5JumpClearanceDiagnostic.SourceBindingMismatch);

            Sv5JumpPoint expectedTakeoff = CorrectedTakeoff(recipe.RecipeId, link.Order, link.Takeoff);
            bool adjusted = !expectedTakeoff.Equals(link.Takeoff);
            if (!trace.EffectiveTakeoff.Equals(expectedTakeoff) || trace.EndpointAdjusted != adjusted ||
                trace.CorrectionId != (adjusted ? EndpointCorrectionId : string.Empty))
                diagnostics.Add(Sv5JumpClearanceDiagnostic.EffectiveTakeoffMismatch);

            if (trace.Samples.Count == 0 || !trace.Samples[0].Body.Equals(expectedTakeoff) ||
                !trace.Samples[trace.Samples.Count - 1].Body.Equals(link.Landing))
                diagnostics.Add(Sv5JumpClearanceDiagnostic.WrongEndpoint);
            if (trace.Samples.Count > MaxTraceStates) diagnostics.Add(Sv5JumpClearanceDiagnostic.OverlongTrace);

            var occupied = new HashSet<Sv5JumpPoint>(recipe.Occupancy.Where(value => value != null).Select(value => value.Point));
            int higherEndpoint = Math.Max(expectedTakeoff.Y, link.Landing.Y);
            for (int index = 0; index < trace.Samples.Count; index++)
            {
                Sv5JumpClearanceSample sample = trace.Samples[index];
                if (sample == null)
                {
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.BlockedBody);
                    continue;
                }
                if (sample.SampleOrder != index) diagnostics.Add(Sv5JumpClearanceDiagnostic.PhaseMismatch);
                if (!sample.Head.Equals(new Sv5JumpPoint(sample.Body.X, sample.Body.Y + 1)))
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.BlockedHead);
                if (!Inside(sample.Body) || occupied.Contains(sample.Body))
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.BlockedBody);
                if (!Inside(sample.Head) || occupied.Contains(sample.Head))
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.BlockedHead);
                if (sample.Body.Y > higherEndpoint + MaxApexAboveHigherEndpoint)
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.ExcessiveApex);
            }

            Sv5JumpClearanceGrabContact proof = null;
            for (int index = 0; index + 1 < trace.Samples.Count; index++)
            {
                Sv5JumpClearanceSample a = trace.Samples[index];
                Sv5JumpClearanceSample b = trace.Samples[index + 1];
                if (a == null || b == null) continue;
                int dx = b.Body.X - a.Body.X;
                int dy = b.Body.Y - a.Body.Y;
                if (dx == 0 && dy == 0) diagnostics.Add(Sv5JumpClearanceDiagnostic.DuplicateState);
                if (Math.Abs(dx) > 1 || Math.Abs(dy) > 1) diagnostics.Add(Sv5JumpClearanceDiagnostic.NonLocalStep);
                if ((link.Direction == Sv5JumpDirection.LeftToRight && dx < 0) ||
                    (link.Direction == Sv5JumpDirection.RightToLeft && dx > 0))
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.HorizontalReversal);
                if (dx != 0 && dy != 0)
                {
                    foreach (Sv5JumpPoint corner in new[]
                    {
                        new Sv5JumpPoint(b.Body.X, a.Body.Y),
                        new Sv5JumpPoint(a.Body.X, b.Body.Y),
                    })
                    {
                        bool blocked = occupied.Contains(corner) || occupied.Contains(new Sv5JumpPoint(corner.X, corner.Y + 1));
                        bool intentionalContact = link.Mode == Sv5JumpMode.JumpGrab &&
                            a.Phase == Sv5JumpClearancePhase.Hang && b.Phase == Sv5JumpClearancePhase.PullUp &&
                            source.Link.GrabEdge != null && corner.Equals(source.Link.GrabEdge.Contact) &&
                            recipe.Occupancy.Any(value => value.Point.Equals(corner) && value.Collision == "SOLID");
                        if (blocked && !intentionalContact)
                            diagnostics.Add(Sv5JumpClearanceDiagnostic.DiagonalCornerClip);
                    }
                }
            }

            if (link.Mode == Sv5JumpMode.Jump)
            {
                if (trace.Samples.Count == 0 || trace.Samples[0].Phase != Sv5JumpClearancePhase.Takeoff ||
                    trace.Samples[trace.Samples.Count - 1].Phase != Sv5JumpClearancePhase.Landing ||
                    trace.Samples.Any(value => value == null || (value.Phase != Sv5JumpClearancePhase.Takeoff &&
                        value.Phase != Sv5JumpClearancePhase.Air && value.Phase != Sv5JumpClearancePhase.Landing)))
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.PhaseMismatch);
            }
            else
            {
                Sv5JumpClearanceSample hang = trace.Samples.SingleOrDefault(value => value != null && value.Phase == Sv5JumpClearancePhase.Hang);
                Sv5JumpClearanceSample pull = trace.Samples.SingleOrDefault(value => value != null && value.Phase == Sv5JumpClearancePhase.PullUp);
                if (trace.Samples.Count == 0 || trace.Samples[0].Phase != Sv5JumpClearancePhase.Takeoff ||
                    hang == null || pull == null || hang.SampleOrder >= pull.SampleOrder || pull.SampleOrder + 1 != trace.Samples.Count)
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.MissingGrabPhases);
                if (hang != null && pull != null && source.Link.GrabEdge != null &&
                    (!hang.Body.Equals(source.Link.GrabEdge.HangBody) || !pull.Body.Equals(source.Link.GrabEdge.PullUp)))
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.GrabClearanceMismatch);
            }
        }

        private static void ValidateGrabContacts(
            IReadOnlyList<Sv5JumpClearanceGrabContact> proofs,
            IReadOnlyDictionary<string, Sv5JumpRecipeVariant> recipes,
            IReadOnlyDictionary<string, Sv5JumpClearanceTrace> traces,
            IDictionary<string, List<string>> linkDiagnostics,
            ICollection<string> global)
        {
            var byRecipe = proofs.Where(value => value != null).GroupBy(value => value.RecipeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            foreach (KeyValuePair<string, Sv5JumpRecipeVariant> entry in recipes)
            {
                Sv5JumpRecipeGrab expected = entry.Value.GrabEdges.Single();
                string key = Key(entry.Key, 3);
                Sv5JumpClearanceGrabContact actual;
                if (!byRecipe.TryGetValue(entry.Key, out actual))
                {
                    AddLinkDiagnostic(linkDiagnostics, key, Sv5JumpClearanceDiagnostic.MissingGrabContact);
                    continue;
                }
                bool exact = actual.SourceLinkId == GrabLinkId && actual.SourceGrabEdgeId == expected.SourceGrabEdgeId &&
                    actual.TargetSupportId == expected.Edge.SupportId && actual.Contact.Equals(expected.Edge.Contact) &&
                    actual.HangBody.Equals(expected.Edge.HangBody) && actual.HangHead.Equals(expected.HangHead) &&
                    actual.PullUp.Equals(expected.Edge.PullUp) && actual.PullUpHead.Equals(expected.PullUpHead);
                bool contactSolid = entry.Value.Occupancy.Any(value => value.Point.Equals(actual.Contact) &&
                    value.Collision == "SOLID" && value.SupportKind == "SOLID" && value.SourceOwnerId == actual.TargetSupportId);
                if (!exact || !contactSolid || !expected.Edge.Exposed || !expected.Edge.Safe)
                    AddLinkDiagnostic(linkDiagnostics, key, Sv5JumpClearanceDiagnostic.MissingGrabContact);
                if (actual.Face != expected.Edge.Face)
                    AddLinkDiagnostic(linkDiagnostics, key, Sv5JumpClearanceDiagnostic.WrongGrabFace);

                Sv5JumpClearanceTrace trace;
                if (!traces.TryGetValue(key, out trace) || !trace.Samples.Any(value => value != null &&
                    value.Phase == Sv5JumpClearancePhase.Hang && value.Body.Equals(actual.HangBody) && value.Head.Equals(actual.HangHead)) ||
                    !trace.Samples.Any(value => value != null && value.Phase == Sv5JumpClearancePhase.PullUp &&
                        value.Body.Equals(actual.PullUp) && value.Head.Equals(actual.PullUpHead)))
                    AddLinkDiagnostic(linkDiagnostics, key, Sv5JumpClearanceDiagnostic.MissingGrabPhases);
            }
            if (proofs.Count != 2 || byRecipe.Count != 2)
                global.Add(Sv5JumpClearanceDiagnostic.MissingGrabContact);
        }

        private static void ValidateMirrors(
            IReadOnlyDictionary<string, Sv5JumpClearanceTrace> traces,
            ICollection<string> diagnostics)
        {
            for (int order = 0; order < 9; order++)
            {
                Sv5JumpClearanceTrace r0;
                Sv5JumpClearanceTrace mirror;
                if (!traces.TryGetValue(Key(Sv5JumpRecipeCatalog.R0RecipeId, order), out r0) ||
                    !traces.TryGetValue(Key(Sv5JumpRecipeCatalog.MirrorRecipeId, order), out mirror) ||
                    r0.Samples.Count != mirror.Samples.Count)
                {
                    diagnostics.Add(Sv5JumpClearanceDiagnostic.BadMirror + ":" + order);
                    continue;
                }
                for (int index = 0; index < r0.Samples.Count; index++)
                {
                    Sv5JumpClearanceSample a = r0.Samples[index];
                    Sv5JumpClearanceSample b = mirror.Samples[index];
                    if (a == null || b == null || b.Body.X != 23 - a.Body.X || b.Body.Y != a.Body.Y ||
                        b.Head.X != 23 - a.Head.X || b.Head.Y != a.Head.Y || b.Phase != a.Phase)
                        diagnostics.Add(Sv5JumpClearanceDiagnostic.BadMirror + ":" + order + ":" + index);
                }
            }
        }

        private static Sv5JumpPoint CorrectedTakeoff(string recipeId, int order, Sv5JumpPoint source)
        {
            if (recipeId == Sv5JumpRecipeCatalog.R0RecipeId && order == 2) return new Sv5JumpPoint(13, 4);
            if (recipeId == Sv5JumpRecipeCatalog.MirrorRecipeId && order == 2) return new Sv5JumpPoint(10, 4);
            if (recipeId == Sv5JumpRecipeCatalog.R0RecipeId && order == 6) return new Sv5JumpPoint(5, 8);
            if (recipeId == Sv5JumpRecipeCatalog.MirrorRecipeId && order == 6) return new Sv5JumpPoint(18, 8);
            return source;
        }

        private static void AddLinkDiagnostic(IDictionary<string, List<string>> values, string key, string diagnostic)
        {
            List<string> list;
            if (values.TryGetValue(key, out list)) list.Add(diagnostic);
        }

        private static bool Inside(Sv5JumpPoint point)
        {
            return point.X >= 0 && point.X < LocalWidth && point.Y >= 0 && point.Y < LocalHeight;
        }

        private static string Key(string recipeId, int order)
        {
            return (recipeId ?? string.Empty) + "|" + order;
        }

        private static Sv5JumpPoint[] Points(params int[] coordinates)
        {
            var points = new Sv5JumpPoint[coordinates.Length / 2];
            for (int index = 0; index < points.Length; index++)
                points[index] = new Sv5JumpPoint(coordinates[index * 2], coordinates[index * 2 + 1]);
            return points;
        }
    }
}
