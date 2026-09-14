using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5JumpRecipeTransform
    {
        R0,
        MirrorX,
    }

    public sealed class Sv5JumpRecipeSupport
    {
        public Sv5JumpRecipeSupport(string sourceSupportId, Sv5JumpSupport support)
        {
            SourceSupportId = sourceSupportId ?? string.Empty;
            Support = support;
        }

        public string SourceSupportId { get; }
        public Sv5JumpSupport Support { get; }

        internal string DigestToken
        {
            get { return SourceSupportId + "|" + (Support == null ? string.Empty : Support.DigestToken); }
        }
    }

    public sealed class Sv5JumpRecipeOccupancy : IComparable<Sv5JumpRecipeOccupancy>
    {
        public Sv5JumpRecipeOccupancy(
            Sv5JumpPoint point,
            string collision,
            string sourceOwnerId,
            string source,
            string supportKind)
        {
            Point = point;
            Collision = collision ?? string.Empty;
            SourceOwnerId = sourceOwnerId ?? string.Empty;
            Source = source ?? string.Empty;
            SupportKind = supportKind ?? string.Empty;
        }

        public Sv5JumpPoint Point { get; }
        public string Collision { get; }
        public string SourceOwnerId { get; }
        public string Source { get; }
        public string SupportKind { get; }

        public int CompareTo(Sv5JumpRecipeOccupancy other)
        {
            if (other == null) return 1;
            int point = Point.CompareTo(other.Point);
            return point != 0 ? point : string.Compare(SourceOwnerId, other.SourceOwnerId, StringComparison.Ordinal);
        }

        internal string DigestToken
        {
            get { return Point + "|" + Collision + "|" + SourceOwnerId + "|" + Source + "|" + SupportKind; }
        }
    }

    public sealed class Sv5JumpRecipeLink
    {
        public Sv5JumpRecipeLink(string sourceLinkId, Sv5JumpGrabRouteLink link)
        {
            SourceLinkId = sourceLinkId ?? string.Empty;
            Link = link;
        }

        public string SourceLinkId { get; }
        public Sv5JumpGrabRouteLink Link { get; }

        internal string DigestToken
        {
            get { return SourceLinkId + "|" + (Link == null ? string.Empty : Link.DigestToken); }
        }
    }

    public sealed class Sv5JumpRecipeGrab
    {
        public Sv5JumpRecipeGrab(
            string sourceGrabEdgeId,
            Sv5JumpGrabEdge edge,
            Sv5JumpPoint hangHead,
            Sv5JumpPoint pullUpHead)
        {
            SourceGrabEdgeId = sourceGrabEdgeId ?? string.Empty;
            Edge = edge;
            HangHead = hangHead;
            PullUpHead = pullUpHead;
        }

        public string SourceGrabEdgeId { get; }
        public Sv5JumpGrabEdge Edge { get; }
        public Sv5JumpPoint HangHead { get; }
        public Sv5JumpPoint PullUpHead { get; }

        internal string DigestToken
        {
            get
            {
                return SourceGrabEdgeId + "|" + (Edge == null ? string.Empty : Edge.DigestToken) +
                    "|" + HangHead + "|" + PullUpHead;
            }
        }
    }

    public sealed class Sv5JumpRecipeSegment : IComparable<Sv5JumpRecipeSegment>
    {
        public Sv5JumpRecipeSegment(
            int segmentOrder,
            string segmentId,
            string purpose,
            IEnumerable<string> sourceLinkIds)
        {
            SegmentOrder = segmentOrder;
            SegmentId = segmentId ?? string.Empty;
            Purpose = purpose ?? string.Empty;
            SourceLinkIds = Array.AsReadOnly((sourceLinkIds ?? Array.Empty<string>()).ToArray());
        }

        public int SegmentOrder { get; }
        public string SegmentId { get; }
        public string Purpose { get; }
        public IReadOnlyList<string> SourceLinkIds { get; }

        public int CompareTo(Sv5JumpRecipeSegment other)
        {
            return other == null ? 1 : SegmentOrder.CompareTo(other.SegmentOrder);
        }

        internal string DigestToken
        {
            get { return SegmentOrder + "|" + SegmentId + "|" + Purpose + "|" + string.Join(";", SourceLinkIds); }
        }
    }

    public sealed class Sv5JumpRecipeReferenceChange : IComparable<Sv5JumpRecipeReferenceChange>
    {
        public Sv5JumpRecipeReferenceChange(int x, int y, char before, char draftAfter)
        {
            X = x;
            Y = y;
            Before = before;
            DraftAfter = draftAfter;
        }

        public int X { get; }
        public int Y { get; }
        public char Before { get; }
        public char DraftAfter { get; }

        public int CompareTo(Sv5JumpRecipeReferenceChange other)
        {
            if (other == null) return 1;
            int y = Y.CompareTo(other.Y);
            return y != 0 ? y : X.CompareTo(other.X);
        }
    }

    public sealed class Sv5JumpRecipeCorrespondence
    {
        public Sv5JumpRecipeCorrespondence(string intentId, string description)
        {
            IntentId = intentId ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string IntentId { get; }
        public string Description { get; }
        public string Relationship { get { return "DESIGN_LINEAGE_ONLY"; } }
        public bool ProductionEquivalence { get { return false; } }
        public bool PlayerVerified { get { return false; } }
    }

    public sealed class Sv5JumpRecipeReference
    {
        public Sv5JumpRecipeReference(
            IEnumerable<string> beforeRows,
            IEnumerable<string> draftAfterRows,
            IEnumerable<Sv5JumpRecipeCorrespondence> correspondences)
        {
            BeforeRows = Array.AsReadOnly((beforeRows ?? Array.Empty<string>()).ToArray());
            DraftAfterRows = Array.AsReadOnly((draftAfterRows ?? Array.Empty<string>()).ToArray());
            Correspondences = Array.AsReadOnly((correspondences ?? Array.Empty<Sv5JumpRecipeCorrespondence>()).ToArray());
            Changes = Array.AsReadOnly(BuildChanges().OrderBy(value => value).ToArray());
            BeforeDigest = Sv5JumpRecipeCatalog.Hash(string.Join("\n", BeforeRows) + "\n");
            DraftAfterDigest = Sv5JumpRecipeCatalog.Hash(string.Join("\n", DraftAfterRows) + "\n");
            Diagnostics = Array.AsReadOnly(Validate().Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public IReadOnlyList<string> BeforeRows { get; }
        public IReadOnlyList<string> DraftAfterRows { get; }
        public IReadOnlyList<Sv5JumpRecipeReferenceChange> Changes { get; }
        public IReadOnlyList<Sv5JumpRecipeCorrespondence> Correspondences { get; }
        public string BeforeDigest { get; }
        public string DraftAfterDigest { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public bool ProductionEquivalenceClaimed { get { return false; } }
        public bool PlayerVerified { get { return false; } }
        public string RuntimeRole { get { return "REFERENCE_ONLY_NOT_RUNTIME_GEOMETRY"; } }

        private IEnumerable<Sv5JumpRecipeReferenceChange> BuildChanges()
        {
            int height = Math.Min(BeforeRows.Count, DraftAfterRows.Count);
            for (int y = 0; y < height; y++)
            {
                int width = Math.Min(BeforeRows[y].Length, DraftAfterRows[y].Length);
                for (int x = 0; x < width; x++)
                    if (BeforeRows[y][x] != DraftAfterRows[y][x])
                        yield return new Sv5JumpRecipeReferenceChange(x, y, BeforeRows[y][x], DraftAfterRows[y][x]);
            }
        }

        private IEnumerable<string> Validate()
        {
            if (!ValidGrid(BeforeRows)) yield return Sv5JumpRecipeDiagnostic.ReferenceGridMismatch + ":BEFORE";
            if (!ValidGrid(DraftAfterRows)) yield return Sv5JumpRecipeDiagnostic.ReferenceGridMismatch + ":DRAFT_AFTER";
            if (!string.Equals(BeforeDigest, Sv5JumpRecipeCatalog.ReferenceBeforeDigest, StringComparison.Ordinal))
                yield return Sv5JumpRecipeDiagnostic.ReferenceDigestMismatch + ":BEFORE";
            if (!string.Equals(DraftAfterDigest, Sv5JumpRecipeCatalog.ReferenceDraftAfterDigest, StringComparison.Ordinal))
                yield return Sv5JumpRecipeDiagnostic.ReferenceDigestMismatch + ":DRAFT_AFTER";
            if (Count(BeforeRows, 'S') != 176 || Count(BeforeRows, 'A') != 546 || Count(BeforeRows, 'O') != 46)
                yield return Sv5JumpRecipeDiagnostic.ReferenceCountMismatch + ":BEFORE";
            if (Count(DraftAfterRows, 'S') != 249 || Count(DraftAfterRows, 'A') != 491 || Count(DraftAfterRows, 'O') != 28)
                yield return Sv5JumpRecipeDiagnostic.ReferenceCountMismatch + ":DRAFT_AFTER";
            if (Changes.Count != 141) yield return Sv5JumpRecipeDiagnostic.ReferenceChangeMismatch;
            string[] expected = { "IRREGULAR_SOLID_BACKING", "LEVEL_CONNECTION", "MIXED_SOLID_ONE_WAY", "PLUS_ONE_JUMP", "PLUS_TWO_JUMP_GRAB" };
            string[] actual = Correspondences.Where(value => value != null).Select(value => value.IntentId)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal) || Correspondences.Count != 5)
                yield return Sv5JumpRecipeDiagnostic.ReferenceCorrespondenceMismatch;
            if (Correspondences.Any(value => value == null || value.ProductionEquivalence || value.PlayerVerified ||
                value.Relationship != "DESIGN_LINEAGE_ONLY"))
                yield return Sv5JumpRecipeDiagnostic.ReferenceClaimMismatch;
        }

        private static bool ValidGrid(IReadOnlyList<string> rows)
        {
            return rows.Count == 32 && rows.All(row => row != null && row.Length == 24 && row.All(cell => cell == 'S' || cell == 'A' || cell == 'O'));
        }

        private static int Count(IEnumerable<string> rows, char cell)
        {
            return rows.Sum(row => row == null ? 0 : row.Count(value => value == cell));
        }
    }

    public static class Sv5JumpRecipeDiagnostic
    {
        public const string BaseDigestMismatch = "SV5_16_BASE_FIXTURE_DIGEST_MISMATCH";
        public const string RecipeIdentityMismatch = "RECIPE_ID_OR_TRANSFORM_MISMATCH";
        public const string SupportMismatch = "RECIPE_SUPPORT_TRANSFORM_MISMATCH";
        public const string OccupancyMismatch = "RECIPE_OCCUPANCY_TRANSFORM_MISMATCH";
        public const string RouteMismatch = "RECIPE_ROUTE_TRANSFORM_MISMATCH";
        public const string MovementMixtureMismatch = "RECIPE_MOVEMENT_MIXTURE_MISMATCH";
        public const string GrabMismatch = "RECIPE_EXPLICIT_GRAB_TRANSFORM_MISMATCH";
        public const string SegmentMismatch = "RECIPE_SEGMENT_PARTITION_MISMATCH";
        public const string ContractRejected = "TRANSFORMED_ROUTE_CONTRACT_REJECTED";
        public const string PrematureState = "SWEPT_RECOVERY_OR_PLAYER_STATE_FORBIDDEN";
        public const string ReferenceGridMismatch = "REFERENCE_012_GRID_MISMATCH";
        public const string ReferenceDigestMismatch = "REFERENCE_012_DIGEST_MISMATCH";
        public const string ReferenceCountMismatch = "REFERENCE_012_CELL_COUNT_MISMATCH";
        public const string ReferenceChangeMismatch = "REFERENCE_012_CHANGE_SET_MUST_BE_141";
        public const string ReferenceCorrespondenceMismatch = "REFERENCE_012_CORRESPONDENCE_MISMATCH";
        public const string ReferenceClaimMismatch = "REFERENCE_012_FALSE_RUNTIME_OR_PLAYER_CLAIM";
        public const string CatalogMismatch = "CATALOG_MUST_CONTAIN_EXACT_R0_AND_MIRROR_X";
    }

    public sealed class Sv5JumpRecipeVariant
    {
        public Sv5JumpRecipeVariant(
            string recipeId,
            Sv5JumpRecipeTransform transform,
            string baseFixtureDigest,
            IEnumerable<Sv5JumpRecipeSupport> supports,
            IEnumerable<Sv5JumpRecipeOccupancy> occupancy,
            IEnumerable<Sv5JumpRecipeLink> routeLinks,
            IEnumerable<Sv5JumpRecipeGrab> grabEdges,
            IEnumerable<Sv5JumpRecipeSegment> segments)
        {
            RecipeId = recipeId ?? string.Empty;
            Transform = transform;
            BaseFixtureDigest = baseFixtureDigest ?? string.Empty;
            Supports = Array.AsReadOnly((supports ?? Array.Empty<Sv5JumpRecipeSupport>())
                .OrderBy(value => value == null ? string.Empty : value.SourceSupportId, StringComparer.Ordinal).ToArray());
            Occupancy = Array.AsReadOnly((occupancy ?? Array.Empty<Sv5JumpRecipeOccupancy>()).OrderBy(value => value).ToArray());
            RouteLinks = Array.AsReadOnly((routeLinks ?? Array.Empty<Sv5JumpRecipeLink>())
                .OrderBy(value => value == null || value.Link == null ? int.MaxValue : value.Link.Order).ToArray());
            GrabEdges = Array.AsReadOnly((grabEdges ?? Array.Empty<Sv5JumpRecipeGrab>())
                .OrderBy(value => value == null ? string.Empty : value.SourceGrabEdgeId, StringComparer.Ordinal).ToArray());
            Segments = Array.AsReadOnly((segments ?? Array.Empty<Sv5JumpRecipeSegment>()).OrderBy(value => value).ToArray());
            Diagnostics = Array.AsReadOnly(Sv5JumpRecipeCatalog.ValidateVariant(this).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Digest = Sv5JumpRecipeCatalog.Hash(string.Join("\n", new[] { RecipeId, ExportTransform, BaseFixtureDigest }
                .Concat(Supports.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(Occupancy.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(RouteLinks.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(GrabEdges.Where(value => value != null).Select(value => value.DigestToken))
                .Concat(Segments.Where(value => value != null).Select(value => value.DigestToken))));
        }

        public string RecipeId { get; }
        public Sv5JumpRecipeTransform Transform { get; }
        public string ExportTransform { get { return Transform == Sv5JumpRecipeTransform.R0 ? "R0" : "MIRROR_X"; } }
        public string BaseFixtureDigest { get; }
        public IReadOnlyList<Sv5JumpRecipeSupport> Supports { get; }
        public IReadOnlyList<Sv5JumpRecipeOccupancy> Occupancy { get; }
        public IReadOnlyList<Sv5JumpRecipeLink> RouteLinks { get; }
        public IReadOnlyList<Sv5JumpRecipeGrab> GrabEdges { get; }
        public IReadOnlyList<Sv5JumpRecipeSegment> Segments { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public int BaseSupportCellCount { get { return Occupancy.Count(value => value != null && value.Source == "BASE_SUPPORT"); } }
        public int OutlineCellCount { get { return Occupancy.Count(value => value != null && value.Source == "OUTLINE_BACKING"); } }
        public int PlusOneJumpCount { get { return RouteLinks.Count(value => value != null && value.Link != null && value.Link.Mode == Sv5JumpMode.Jump && value.Link.Rise == 1); } }
        public int PlusTwoJumpGrabCount { get { return RouteLinks.Count(value => value != null && value.Link != null && value.Link.Mode == Sv5JumpMode.JumpGrab && value.Link.Rise == 2); } }
        public int LevelJumpCount { get { return RouteLinks.Count(value => value != null && value.Link != null && value.Link.Mode == Sv5JumpMode.Jump && value.Link.Rise == 0); } }
        public bool StaticRecipeOnly { get { return true; } }
        public bool ProductionEquivalenceToReference012 { get { return false; } }
        public bool PlayerVerified { get { return false; } }
        public bool ReverseCompletionRequired { get { return false; } }
        public bool SweptClearanceReady { get { return false; } }
        public bool RecoveryReady { get { return false; } }
        public bool JumpRecipeReady { get { return Diagnostics.Count == 0; } }
    }

    public sealed class Sv5JumpRecipeCatalogModel
    {
        public Sv5JumpRecipeCatalogModel(
            string baseFixtureDigest,
            IEnumerable<Sv5JumpRecipeVariant> recipes,
            Sv5JumpRecipeReference reference)
        {
            BaseFixtureDigest = baseFixtureDigest ?? string.Empty;
            Recipes = Array.AsReadOnly((recipes ?? Array.Empty<Sv5JumpRecipeVariant>())
                .OrderBy(value => value == null ? string.Empty : value.RecipeId, StringComparer.Ordinal).ToArray());
            Reference012 = reference;
            Diagnostics = Array.AsReadOnly(Validate().Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Digest = Sv5JumpRecipeCatalog.Hash(string.Join("\n", new[] { BaseFixtureDigest }
                .Concat(Recipes.Where(value => value != null).Select(value => value.Digest))
                .Concat(new[] { Reference012 == null ? string.Empty : Reference012.BeforeDigest,
                    Reference012 == null ? string.Empty : Reference012.DraftAfterDigest })));
        }

        public string BaseFixtureDigest { get; }
        public IReadOnlyList<Sv5JumpRecipeVariant> Recipes { get; }
        public Sv5JumpRecipeReference Reference012 { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public bool JumpRecipeReady { get { return Diagnostics.Count == 0; } }
        public bool ComposedGeometryReady { get { return Diagnostics.Count == 0; } }
        public bool SweptClearanceReady { get { return false; } }
        public bool RecoveryReady { get { return false; } }
        public bool PlayerVerified { get { return false; } }
        public int WholeWorldBuildsInNewTargetedTests { get { return 0; } }
        public int WholeWorldSearchesInNewTargetedTests { get { return 0; } }

        public Sv5JumpRecipeVariant Recipe(string recipeId)
        {
            return Recipes.Single(value => value != null && string.Equals(value.RecipeId, recipeId, StringComparison.Ordinal));
        }

        private IEnumerable<string> Validate()
        {
            if (!string.Equals(BaseFixtureDigest, Sv5JumpRecipeCatalog.BaseFixtureDigest, StringComparison.Ordinal))
                yield return Sv5JumpRecipeDiagnostic.BaseDigestMismatch;
            if (Recipes.Count != 2 || Recipes.Any(value => value == null) ||
                !Recipes.Select(value => value.RecipeId).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(
                    new[] { Sv5JumpRecipeCatalog.MirrorRecipeId, Sv5JumpRecipeCatalog.R0RecipeId }, StringComparer.Ordinal))
                yield return Sv5JumpRecipeDiagnostic.CatalogMismatch;
            if (Recipes.Any(value => value != null && value.Diagnostics.Count != 0))
                yield return Sv5JumpRecipeDiagnostic.CatalogMismatch + ":VARIANT";
            if (Reference012 == null || Reference012.Diagnostics.Count != 0 ||
                Reference012.ProductionEquivalenceClaimed || Reference012.PlayerVerified)
                yield return Sv5JumpRecipeDiagnostic.ReferenceClaimMismatch;
        }
    }

    public static class Sv5JumpRecipeCatalog
    {
        private static readonly Lazy<Sv5JumpOutlinePlan> CanonicalSource =
            new Lazy<Sv5JumpOutlinePlan>(Sv5JumpOutlineGeometry.CreateCanonicalLocalFixture);

        public const int LocalCanvasWidth = 24;
        public const int LocalCanvasHeight = 32;
        public const string BaseFixtureDigest = "0ac93bf0ddc1a203baa47517096516c2df98d67978d3c80131a13edcedd5df9c";
        public const string R0RecipeId = "JUMP012_MIXED_R0";
        public const string MirrorRecipeId = "JUMP012_MIXED_MX";
        public const string ReferenceBeforeDigest = "9cf9f0de9e144951ccc63c4264dfb4ddfa4f1f611ff293e8df15dc28eefad02c";
        public const string ReferenceDraftAfterDigest = "36c4f5a585b15505e8a91d59bd519a212cb02ae2faa8b9cca493e7722e43f7e1";

        public static Sv5JumpRecipeCatalogModel CreateCanonicalCatalog()
        {
            Sv5JumpOutlinePlan source = CanonicalSource.Value;
            return new Sv5JumpRecipeCatalogModel(source.Digest, new[]
            {
                CreateVariant(source, Sv5JumpRecipeTransform.R0),
                CreateVariant(source, Sv5JumpRecipeTransform.MirrorX),
            }, CreateReference012());
        }

        public static Sv5JumpRecipeVariant CreateVariant(Sv5JumpOutlinePlan source, Sv5JumpRecipeTransform transform)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            string recipeId = transform == Sv5JumpRecipeTransform.R0 ? R0RecipeId : MirrorRecipeId;
            Sv5JumpRecipeSupport[] supports = source.Supports.Select(value => new Sv5JumpRecipeSupport(
                value.SupportId,
                new Sv5JumpSupport(value.SupportId, value.Kind, TransformSupportX(value, transform), value.Y,
                    value.Width, value.Height, value.ActiveRoute, value.DecorativeOnly, value.ValidationState))).ToArray();
            var supportsById = supports.ToDictionary(value => value.SourceSupportId, value => value.Support, StringComparer.Ordinal);

            Sv5JumpRecipeOccupancy[] occupancy = source.FinalOccupancy.Select(value => new Sv5JumpRecipeOccupancy(
                TransformPoint(value.Point, transform), value.Collision, value.OwnerId, value.Source, value.SupportKind)).ToArray();

            Sv5JumpRecipeGrab[] grabs = source.GrabEdges.Select(value =>
            {
                Sv5JumpGrabClearance clearance = source.GrabClearances.Single(item => item.GrabEdgeId == value.GrabEdgeId);
                return new Sv5JumpRecipeGrab(value.GrabEdgeId, new Sv5JumpGrabEdge(
                    value.GrabEdgeId,
                    value.SupportId,
                    TransformPoint(value.Contact, transform),
                    TransformFace(value.Face, transform),
                    TransformDirection(value.ApproachDirection, transform),
                    TransformPoint(value.HangBody, transform),
                    TransformPoint(value.PullUp, transform),
                    value.Exposed,
                    value.HangBodyClear,
                    value.PullUpClear),
                    TransformPoint(clearance.HangHead, transform),
                    TransformPoint(clearance.PullUpHead, transform));
            }).ToArray();
            var grabsById = grabs.ToDictionary(value => value.SourceGrabEdgeId, value => value.Edge, StringComparer.Ordinal);

            Sv5JumpRecipeLink[] links = source.RouteLinks.Select(value =>
            {
                Sv5JumpGrabEdge grab = value.GrabEdge == null ? null : grabsById[value.GrabEdge.GrabEdgeId];
                return new Sv5JumpRecipeLink(value.LinkId, new Sv5JumpGrabRouteLink(
                    value.Order,
                    value.LinkId,
                    supportsById[value.Source.SupportId],
                    supportsById[value.Target.SupportId],
                    TransformDirection(value.Direction, transform),
                    TransformPoint(value.Takeoff, transform),
                    TransformPoint(value.Landing, transform),
                    value.Mode,
                    grab,
                    value.RequiredRoute,
                    value.ValidationState,
                    value.GapAir,
                    value.Rise));
            }).ToArray();

            return new Sv5JumpRecipeVariant(recipeId, transform, source.Digest, supports, occupancy, links, grabs, ExactSegments());
        }

        public static Sv5JumpRecipeReference CreateReference012()
        {
            return new Sv5JumpRecipeReference(ReferenceBeforeRows(), ReferenceDraftAfterRows(), new[]
            {
                new Sv5JumpRecipeCorrespondence("MIXED_SOLID_ONE_WAY", "Shared mixed support intent only"),
                new Sv5JumpRecipeCorrespondence("PLUS_ONE_JUMP", "Shared one-cell ascent intent only"),
                new Sv5JumpRecipeCorrespondence("PLUS_TWO_JUMP_GRAB", "Shared two-cell Grab intent only"),
                new Sv5JumpRecipeCorrespondence("LEVEL_CONNECTION", "Shared level connection intent only"),
                new Sv5JumpRecipeCorrespondence("IRREGULAR_SOLID_BACKING", "Shared irregular backing intent only"),
            });
        }

        public static IEnumerable<Sv5JumpRecipeSegment> ExactSegments()
        {
            yield return new Sv5JumpRecipeSegment(0, "SEG_ASCENT_A", "THREE_PLUS_ONE_JUMPS",
                new[] { "JS_LINK_00", "JS_LINK_01", "JS_LINK_02" });
            yield return new Sv5JumpRecipeSegment(1, "SEG_GRAB_TURN", "PLUS_TWO_GRAB_THEN_LEVEL",
                new[] { "JS_LINK_03", "JS_LINK_04" });
            yield return new Sv5JumpRecipeSegment(2, "SEG_ASCENT_B", "TWO_PLUS_ONE_JUMPS",
                new[] { "JS_LINK_05", "JS_LINK_06" });
            yield return new Sv5JumpRecipeSegment(3, "SEG_ASCENT_C", "TWO_PLUS_ONE_JUMPS",
                new[] { "JS_LINK_07", "JS_LINK_08" });
        }

        public static Sv5JumpPoint TransformPoint(Sv5JumpPoint point, Sv5JumpRecipeTransform transform)
        {
            return transform == Sv5JumpRecipeTransform.R0 ? point : new Sv5JumpPoint(23 - point.X, point.Y);
        }

        public static Sv5JumpDirection TransformDirection(Sv5JumpDirection direction, Sv5JumpRecipeTransform transform)
        {
            if (transform == Sv5JumpRecipeTransform.R0) return direction;
            return direction == Sv5JumpDirection.LeftToRight ? Sv5JumpDirection.RightToLeft : Sv5JumpDirection.LeftToRight;
        }

        public static Sv5JumpGrabFace TransformFace(Sv5JumpGrabFace face, Sv5JumpRecipeTransform transform)
        {
            if (transform == Sv5JumpRecipeTransform.R0) return face;
            return face == Sv5JumpGrabFace.Left ? Sv5JumpGrabFace.Right : Sv5JumpGrabFace.Left;
        }

        internal static IEnumerable<string> ValidateVariant(Sv5JumpRecipeVariant variant)
        {
            Sv5JumpOutlinePlan source = CanonicalSource.Value;
            string expectedId = variant.Transform == Sv5JumpRecipeTransform.R0 ? R0RecipeId : MirrorRecipeId;
            if (!string.Equals(variant.BaseFixtureDigest, BaseFixtureDigest, StringComparison.Ordinal))
                yield return Sv5JumpRecipeDiagnostic.BaseDigestMismatch;
            if (!string.Equals(variant.RecipeId, expectedId, StringComparison.Ordinal))
                yield return Sv5JumpRecipeDiagnostic.RecipeIdentityMismatch;

            var expectedSupports = source.Supports.ToDictionary(value => value.SupportId, StringComparer.Ordinal);
            if (variant.Supports.Count != 10 || variant.Supports.Any(value => value == null || value.Support == null) ||
                variant.Supports.Select(value => value.SourceSupportId).Distinct(StringComparer.Ordinal).Count() != 10)
                yield return Sv5JumpRecipeDiagnostic.SupportMismatch;
            foreach (Sv5JumpRecipeSupport row in variant.Supports.Where(value => value != null && value.Support != null))
            {
                Sv5JumpSupport expected;
                if (!expectedSupports.TryGetValue(row.SourceSupportId, out expected) ||
                    !SupportMatches(row.Support, expected, variant.Transform))
                    yield return Sv5JumpRecipeDiagnostic.SupportMismatch + ":" + row.SourceSupportId;
            }

            var expectedOccupancy = new HashSet<string>(source.FinalOccupancy.Select(value => OccupancyToken(
                TransformPoint(value.Point, variant.Transform), value.Collision, value.OwnerId, value.Source, value.SupportKind)));
            var actualOccupancy = new HashSet<string>(variant.Occupancy.Where(value => value != null).Select(value =>
                OccupancyToken(value.Point, value.Collision, value.SourceOwnerId, value.Source, value.SupportKind)));
            if (variant.Occupancy.Count != 44 || actualOccupancy.Count != 44 || !actualOccupancy.SetEquals(expectedOccupancy) ||
                variant.BaseSupportCellCount != 27 || variant.OutlineCellCount != 17)
                yield return Sv5JumpRecipeDiagnostic.OccupancyMismatch;

            var expectedLinks = source.RouteLinks.ToDictionary(value => value.LinkId, StringComparer.Ordinal);
            if (variant.RouteLinks.Count != 9 || variant.RouteLinks.Any(value => value == null || value.Link == null) ||
                !variant.RouteLinks.Where(value => value != null && value.Link != null).Select(value => value.Link.Order).SequenceEqual(Enumerable.Range(0, 9)))
                yield return Sv5JumpRecipeDiagnostic.RouteMismatch;
            foreach (Sv5JumpRecipeLink row in variant.RouteLinks.Where(value => value != null && value.Link != null))
            {
                Sv5JumpGrabRouteLink expected;
                if (!expectedLinks.TryGetValue(row.SourceLinkId, out expected) || !LinkMatches(row.Link, expected, variant.Transform))
                    yield return Sv5JumpRecipeDiagnostic.RouteMismatch + ":" + row.SourceLinkId;
                if (!row.Link.ContractAccepted) yield return Sv5JumpRecipeDiagnostic.ContractRejected + ":" + row.SourceLinkId;
            }
            if (variant.PlusOneJumpCount != 7 || variant.PlusTwoJumpGrabCount != 1 || variant.LevelJumpCount != 1 ||
                variant.RouteLinks.Any(value => value != null && value.Link != null && value.Link.Rise > 2))
                yield return Sv5JumpRecipeDiagnostic.MovementMixtureMismatch;

            if (variant.GrabEdges.Count != 1 || variant.GrabEdges[0] == null || variant.GrabEdges[0].Edge == null ||
                !GrabMatches(variant.GrabEdges[0], source, variant.Transform))
                yield return Sv5JumpRecipeDiagnostic.GrabMismatch;

            string[] expectedPartition = Enumerable.Range(0, 9).Select(index => "JS_LINK_" + index.ToString("00")).ToArray();
            string[] actualPartition = variant.Segments.Where(value => value != null).OrderBy(value => value.SegmentOrder)
                .SelectMany(value => value.SourceLinkIds).ToArray();
            string[] expectedSegments = ExactSegments().Select(value => value.DigestToken).ToArray();
            string[] actualSegments = variant.Segments.Where(value => value != null).Select(value => value.DigestToken).ToArray();
            if (variant.Segments.Count != 4 || !actualPartition.SequenceEqual(expectedPartition, StringComparer.Ordinal) ||
                !actualSegments.SequenceEqual(expectedSegments, StringComparer.Ordinal))
                yield return Sv5JumpRecipeDiagnostic.SegmentMismatch;
            if (variant.PlayerVerified || variant.SweptClearanceReady || variant.RecoveryReady || !variant.StaticRecipeOnly ||
                variant.ProductionEquivalenceToReference012)
                yield return Sv5JumpRecipeDiagnostic.PrematureState;
        }

        internal static string Hash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)))
                    .Replace("-", string.Empty).ToLowerInvariant();
        }

        private static int TransformSupportX(Sv5JumpSupport support, Sv5JumpRecipeTransform transform)
        {
            return transform == Sv5JumpRecipeTransform.R0 ? support.X : 23 - support.TopXMax;
        }

        private static bool SupportMatches(Sv5JumpSupport actual, Sv5JumpSupport source, Sv5JumpRecipeTransform transform)
        {
            return actual.SupportId == source.SupportId && actual.Kind == source.Kind &&
                actual.X == TransformSupportX(source, transform) && actual.Y == source.Y &&
                actual.Width == source.Width && actual.Height == source.Height &&
                actual.ActiveRoute == source.ActiveRoute && actual.DecorativeOnly == source.DecorativeOnly &&
                actual.ValidationState == source.ValidationState;
        }

        private static bool LinkMatches(Sv5JumpGrabRouteLink actual, Sv5JumpGrabRouteLink source, Sv5JumpRecipeTransform transform)
        {
            return actual.Order == source.Order && actual.LinkId == source.LinkId &&
                actual.Source.SupportId == source.Source.SupportId && actual.Target.SupportId == source.Target.SupportId &&
                actual.Direction == TransformDirection(source.Direction, transform) &&
                actual.Takeoff.Equals(TransformPoint(source.Takeoff, transform)) &&
                actual.Landing.Equals(TransformPoint(source.Landing, transform)) &&
                actual.Mode == source.Mode && actual.GapAir == source.GapAir && actual.Rise == source.Rise &&
                actual.RequiredRoute == source.RequiredRoute && actual.ValidationState == source.ValidationState &&
                (actual.GrabEdge == null ? string.Empty : actual.GrabEdge.GrabEdgeId) ==
                (source.GrabEdge == null ? string.Empty : source.GrabEdge.GrabEdgeId);
        }

        private static bool GrabMatches(Sv5JumpRecipeGrab actual, Sv5JumpOutlinePlan source, Sv5JumpRecipeTransform transform)
        {
            Sv5JumpGrabEdge edge = source.GrabEdges.Single();
            Sv5JumpGrabClearance clearance = source.GrabClearances.Single();
            return actual.SourceGrabEdgeId == edge.GrabEdgeId && actual.Edge.GrabEdgeId == edge.GrabEdgeId &&
                actual.Edge.SupportId == edge.SupportId && actual.Edge.Contact.Equals(TransformPoint(edge.Contact, transform)) &&
                actual.Edge.Face == TransformFace(edge.Face, transform) &&
                actual.Edge.ApproachDirection == TransformDirection(edge.ApproachDirection, transform) &&
                actual.Edge.HangBody.Equals(TransformPoint(edge.HangBody, transform)) &&
                actual.Edge.PullUp.Equals(TransformPoint(edge.PullUp, transform)) &&
                actual.HangHead.Equals(TransformPoint(clearance.HangHead, transform)) &&
                actual.PullUpHead.Equals(TransformPoint(clearance.PullUpHead, transform)) &&
                actual.Edge.Exposed && actual.Edge.HangBodyClear && actual.Edge.PullUpClear;
        }

        private static string OccupancyToken(Sv5JumpPoint point, string collision, string owner, string source, string kind)
        {
            return point + "|" + collision + "|" + owner + "|" + source + "|" + kind;
        }

        private static IEnumerable<string> ReferenceBeforeRows()
        {
            return new[]
            {
                "AAAASSSSSSSSSSSSSSSSSSSS", "SSSSSSSSSSSSSSSSSSSSSSSS", "SSAOOAAAAAAAAAAAAAAAAASS", "AAAAAAAAOOAAAAAAAAAAAASS",
                "AAAAAAAAAAAAAAOOAAAAAASA", "AAAAAAAAAAAAAAAAAAAOOASA", "SSAAAAAAAAAAAAOOAAAAAASA", "SSAAAAAAOOAAAAAAAAAAAASS",
                "SSAOOAAAAAAAAAAAAAAAAASS", "SSAAAAAAOOAAAAAAAAAAAASS", "SSAAAAAAAAAAAAOOAAAAAASS", "SSAAAAAAAAAAAAAAAAAOOASS",
                "SSAAAAAAAAAAAAOOAAAAAASS", "SSAAAAAAOOAAAAAAAAAAAASS", "SSAOOAAAAAAAAAAAAAAAAASS", "SSAAAAAAOOAAAAAAAAAAAASA",
                "SSAAAAAAAAAAAAOOAAAAAASA", "SSAAAAAAAAAAAAAAAAAOOASA", "SSAAAAAAAAAAAAOOAAAAAASS", "SSAAAAAAOOAAAAAAAAAAAASA",
                "SSAOOAAAAAAAAAAAAAAAAASA", "SSAAAAAAOOAAAAAAAAAAAASA", "SSAAAAAAAAAAAAOOAAAAAASS", "SSAAAAAAAAAAAAAAAAAOOASS",
                "SSAAAAAAAAAAAAAAAAAAAAAA", "SSAAAAAAAAAAAAAAAAAAAAAA", "SSAAAAAAAAAAAAAAAAAAAAAA", "SSAAAAAAAAAAAAAAAAAAAASS",
                "SSAAAAAAAAAAAAAAAAAAAASS", "SSAAAAAAAAAAAAAAAAAAAASS", "SSSSSSSSSSSSSSSSSSSSSSSS", "SSSSOAOAAAASSSSSSSSSSSSS",
            };
        }

        private static IEnumerable<string> ReferenceDraftAfterRows()
        {
            return new[]
            {
                "SSSSSSSSSSSSSSSSSSSSSSSS", "SSSSSSSSSSSSSSSSSSSSSSSS", "SSSSSSSSSSSSSSSSSSSSSSSS", "AAAAAAASSSAAAAAAAAAAAASS",
                "AAAAAAAAAAAAAAAAAAAAAASS", "AAAAAAAAAAAASSSAAAAAAASS", "SSAAAAAAAAAAAAAAAOOAAASS", "SSAAAAAAAAAAOOAAAAAAAASS",
                "SSAAAAAOOAAAAAAAAAAAAASS", "SSSSSAAAAAAAAAAAAAAAASSS", "SSAAAAAOOAAAAAAAAAAAASSS", "SSAAAAAAAAAAOOAAAAAAASSS",
                "SSSAAAAAAAAAAAAAAOOAAASS", "SSSAAAAAAAAASSSAAAAAAASS", "SSSAAAAOOAAAAAAAAAAAAASS", "SSSSSAAAAAAAAAAAAAAAAASS",
                "SSAAAAAOOAAAAAAAAAAAAASS", "SSAAAAAAAAAAOOAAAAAAAASS", "SSAAAAAAAAAAAAAAAOOAAASS", "SSAAAAAAAAAASSSAAAAAAASS",
                "SSAAAAAOOAAAAAAAAAAAAASS", "SSSSSAAAAAAAAAAAAAAAAASS", "SSAAAAAOOAAAAAAAAAAAAASS", "SSAAAAAAAAAAOOAAAOOAASSS",
                "SSAAAAAAAAAAAAAAAAAAAAAA", "SSAAAAAAAAAAAAAAAAAAAAAA", "SSAAAAAAAAAAAAAAAAAAAAAA", "SSAAAAAAAAAAAAAAAAAAAASS",
                "SSAAAAAAAAAAAAAAAAAAAASS", "SSAAAAAASSSSSAAAAAAAAASS", "SSSSSSSSSSSSSSSSSSSSSSSS", "SSSSSSSSSSSSSSSSSSSSSSSS",
            };
        }
    }
}
