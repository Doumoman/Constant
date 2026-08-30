using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum SectorPlannerDebugExportKind
    {
        SuccessPlan,
        FailureOneRing,
        GrayboxCatalog,
    }

    public enum SectorPlannerDebugSectionKind
    {
        SourceIdentity,
        RouteAccess,
        AnchorBoundarySpecial,
        SpineEnvelope,
        ClusterPattern,
        QuietActivityEvent,
        OwnershipPlanes,
        RetryRng,
        FailureRing,
        GrayboxCoverage,
        MutationProof,
    }

    public enum SectorPlannerDebugTokenKind
    {
        Empty,
        Terrain,
        Solid,
        ProtectedOpen,
        Reservation,
        Boundary,
        Special,
        Spine,
        Cluster,
        Pattern,
        Quiet,
        ActivityMarker,
        EventMarker,
        Suppressed,
        Conflict,
        RetryNode,
        FailureCenter,
        NeighborContext,
    }

    public enum SectorPlannerDebugSeverity
    {
        Info,
        Warning,
        Error,
    }

    public enum SectorPlannerGrayboxFixtureKind
    {
        OneSector,
        ThreeSector,
        FailureOneRing,
    }

    public enum SectorPlannerGrayboxCoverageKind
    {
        RouteType,
        Biome,
        BoundaryPair,
        SpecialRegion,
        PacingRole,
        AccessClass,
        OwnershipPlane,
        RetryStage,
    }

    public enum SectorPlannerDebugExportErrorCode
    {
        MissingInput,
        MissingRetryPlan,
        MissingOwnershipPlan,
        MissingPlannerInput,
        MissingFailureTrace,
        SectorMismatch,
        RingCenterMissing,
        RingNeighborMismatch,
        RingCoordinateOutOfBounds,
        DebugTokenOutOfBounds,
        DuplicateDebugToken,
        DuplicateSection,
        DuplicateFixtureId,
        CoverageMissingRouteType,
        CoverageMissingBiome,
        CoverageMissingBoundaryPair,
        CoverageMissingSpecialRegion,
        CoverageMissingOwnershipPlane,
        CoverageMissingRetryStage,
        OneSectorFixtureMissing,
        ThreeSectorFixtureMissing,
        FailureRingFixtureMissing,
        ThreeSectorAdjacencyBroken,
        FixtureOutOfBounds,
        FixtureUsesPrivateData,
        UnsupportedFileWriteClaim,
        GeneratedAssetMutationClaim,
        EditorWindowMutationClaim,
        TileMutationClaim,
        SceneMutationClaim,
        PrefabMutationClaim,
        GameObjectMutationClaim,
        PlayModeClaim,
        ExitApprovalClaim,
        NonCanonicalPublication,
    }

    public sealed class SectorPlannerDebugExportError :
        IEquatable<SectorPlannerDebugExportError>, IComparable<SectorPlannerDebugExportError>
    {
        public SectorPlannerDebugExportError(SectorPlannerDebugExportErrorCode code, string subject, string detail)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SectorPlannerDebugExportErrorCode Code { get; }
        public string Subject { get; }
        public string Detail { get; }

        public int CompareTo(SectorPlannerDebugExportError other)
        {
            if (other == null) return -1;
            var result = Code.CompareTo(other.Code);
            if (result != 0) return result;
            result = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return result != 0 ? result : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SectorPlannerDebugExportError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorPlannerDebugExportError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Detail;
    }

    public sealed class SectorPlannerDebugFact : IComparable<SectorPlannerDebugFact>
    {
        public SectorPlannerDebugFact(string key, string value)
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string Key { get; }
        public string Value { get; }

        public int CompareTo(SectorPlannerDebugFact other)
        {
            if (other == null) return -1;
            var result = string.Compare(Key, other.Key, StringComparison.Ordinal);
            return result != 0 ? result : string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public override string ToString() => Key + "=" + Value;
    }

    public sealed class SectorPlannerDebugToken : IComparable<SectorPlannerDebugToken>
    {
        public SectorPlannerDebugToken(
            SectorCoord sectorCoordinate,
            LocalTileCoord coordinate,
            SectorPlannerDebugTokenKind kind,
            string label,
            string sourceOwner,
            string sourceIdentity,
            string symbol = "")
        {
            SectorCoordinate = sectorCoordinate;
            Coordinate = coordinate;
            Kind = kind;
            Label = label ?? string.Empty;
            SourceOwner = sourceOwner ?? string.Empty;
            SourceIdentity = sourceIdentity ?? string.Empty;
            Symbol = string.IsNullOrEmpty(symbol) ? SectorPlannerDebugExporter.Symbol(kind) : symbol;
        }

        public SectorCoord SectorCoordinate { get; }
        public LocalTileCoord Coordinate { get; }
        public SectorPlannerDebugTokenKind Kind { get; }
        public string Label { get; }
        public string SourceOwner { get; }
        public string SourceIdentity { get; }
        public string Symbol { get; }

        public string Identity => string.Join("|", new[]
        {
            SectorCoordinate.X.ToString(CultureInfo.InvariantCulture),
            SectorCoordinate.Y.ToString(CultureInfo.InvariantCulture),
            Coordinate.X.ToString(CultureInfo.InvariantCulture),
            Coordinate.Y.ToString(CultureInfo.InvariantCulture),
            Kind.ToString(), Label, SourceOwner, SourceIdentity,
        });

        public int CompareTo(SectorPlannerDebugToken other)
        {
            if (other == null) return -1;
            var result = SectorCoordinate.Y.CompareTo(other.SectorCoordinate.Y);
            if (result != 0) return result;
            result = SectorCoordinate.X.CompareTo(other.SectorCoordinate.X);
            if (result != 0) return result;
            result = Coordinate.Y.CompareTo(other.Coordinate.Y);
            if (result != 0) return result;
            result = Coordinate.X.CompareTo(other.Coordinate.X);
            if (result != 0) return result;
            result = Kind.CompareTo(other.Kind);
            if (result != 0) return result;
            result = string.Compare(Label, other.Label, StringComparison.Ordinal);
            if (result != 0) return result;
            result = string.Compare(SourceOwner, other.SourceOwner, StringComparison.Ordinal);
            return result != 0 ? result : string.Compare(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal);
        }
    }

    public sealed class SectorPlannerDebugGridPayload : IComparable<SectorPlannerDebugGridPayload>
    {
        private readonly ReadOnlyCollection<string> rows;

        internal SectorPlannerDebugGridPayload(SectorCoord sectorCoordinate, IEnumerable<string> sourceRows)
        {
            SectorCoordinate = sectorCoordinate;
            rows = new ReadOnlyCollection<string>((sourceRows ?? Array.Empty<string>()).Select(value => value ?? string.Empty).ToArray());
            CanonicalDigest = SectorPlannerDebugCanonicalDigest.Hash(string.Join("\n", new[]
            {
                sectorCoordinate.X.ToString(CultureInfo.InvariantCulture),
                sectorCoordinate.Y.ToString(CultureInfo.InvariantCulture),
                string.Join("\n", rows),
            }));
        }

        public SectorCoord SectorCoordinate { get; }
        public IReadOnlyList<string> Rows => rows;
        public string CanonicalDigest { get; }

        public int CompareTo(SectorPlannerDebugGridPayload other)
        {
            if (other == null) return -1;
            var result = SectorCoordinate.Y.CompareTo(other.SectorCoordinate.Y);
            return result != 0 ? result : SectorCoordinate.X.CompareTo(other.SectorCoordinate.X);
        }
    }

    public sealed class SectorPlannerDebugSection : IComparable<SectorPlannerDebugSection>
    {
        private readonly ReadOnlyCollection<SectorPlannerDebugFact> facts;
        private readonly ReadOnlyCollection<SectorPlannerDebugToken> tokens;

        internal SectorPlannerDebugSection(
            string sectionId,
            SectorPlannerDebugSectionKind kind,
            SectorPlannerDebugSeverity severity,
            string sourceTaskId,
            string sourceDigest,
            string summary,
            IEnumerable<SectorPlannerDebugFact> sourceFacts,
            IEnumerable<SectorPlannerDebugToken> sourceTokens)
        {
            SectionId = sectionId ?? string.Empty;
            Kind = kind;
            Severity = severity;
            SourceTaskId = sourceTaskId ?? string.Empty;
            SourceDigest = sourceDigest ?? string.Empty;
            Summary = summary ?? string.Empty;
            facts = new ReadOnlyCollection<SectorPlannerDebugFact>((sourceFacts ?? Array.Empty<SectorPlannerDebugFact>()).OrderBy(value => value).ToArray());
            tokens = new ReadOnlyCollection<SectorPlannerDebugToken>((sourceTokens ?? Array.Empty<SectorPlannerDebugToken>()).OrderBy(value => value).ToArray());
            CanonicalDigest = SectorPlannerDebugCanonicalDigest.ComputeSection(this);
        }

        public string SectionId { get; }
        public SectorPlannerDebugSectionKind Kind { get; }
        public SectorPlannerDebugSeverity Severity { get; }
        public string SourceTaskId { get; }
        public string SourceDigest { get; }
        public string Summary { get; }
        public IReadOnlyList<SectorPlannerDebugFact> Facts => facts;
        public IReadOnlyList<SectorPlannerDebugToken> Tokens => tokens;
        public string CanonicalDigest { get; }

        public int CompareTo(SectorPlannerDebugSection other)
        {
            if (other == null) return -1;
            var result = Kind.CompareTo(other.Kind);
            return result != 0 ? result : string.Compare(SectionId, other.SectionId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorPlannerDebugMutationProof
    {
        internal SectorPlannerDebugMutationProof() { }

        public int RetryExecutionCount => 0;
        public int NewRngDrawCount => 0;
        public int FallbackCorridorCarveCount => 0;
        public int ValidationRelaxationCount => 0;
        public int WholeSectorRerandomCount => 0;
        public int WholeWorldRerandomCount => 0;
        public int FixedAnchorMutationCount => 0;
        public int BoundarySocketMutationCount => 0;
        public int SpecialRegionReservationMutationCount => 0;
        public int ProtectedNoWriteMaskRemovalCount => 0;
        public int TilemapWriteCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int GameObjectMutationCount => 0;
        public int EditorWindowMutationCount => 0;
        public int GeneratedDebugFileWriteCount => 0;
        public int ActivityRuntimeSpawnCount => 0;
        public int EventRuntimeSpawnCount => 0;
        public int GameplayExecutionCount => 0;
        public int ExitApprovalClaimCount => 0;
        public int TotalMutationCount => 0;
    }

    public sealed class SectorPlannerDebugExport
    {
        private readonly ReadOnlyCollection<SectorPlannerDebugSection> sections;
        private readonly ReadOnlyCollection<SectorPlannerDebugGridPayload> gridPayloads;
        private readonly ReadOnlyCollection<string> legend;

        internal SectorPlannerDebugExport(
            SectorPlannerDebugExportKind kind,
            string publicationLabel,
            string sourceDigest,
            IEnumerable<SectorPlannerDebugSection> sourceSections,
            IEnumerable<SectorPlannerDebugGridPayload> sourceGridPayloads,
            IEnumerable<string> sourceLegend,
            SectorPlannerDebugMutationProof mutationProof)
        {
            Kind = kind;
            PublicationLabel = publicationLabel ?? string.Empty;
            SourceDigest = sourceDigest ?? string.Empty;
            sections = new ReadOnlyCollection<SectorPlannerDebugSection>((sourceSections ?? Array.Empty<SectorPlannerDebugSection>()).OrderBy(value => value).ToArray());
            gridPayloads = new ReadOnlyCollection<SectorPlannerDebugGridPayload>((sourceGridPayloads ?? Array.Empty<SectorPlannerDebugGridPayload>()).OrderBy(value => value).ToArray());
            legend = new ReadOnlyCollection<string>((sourceLegend ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            MutationProof = mutationProof ?? new SectorPlannerDebugMutationProof();
            CanonicalDigest = SectorPlannerDebugCanonicalDigest.ComputeExport(this);
        }

        public SectorPlannerDebugExportKind Kind { get; }
        public string PublicationLabel { get; }
        public string SourceDigest { get; }
        public IReadOnlyList<SectorPlannerDebugSection> Sections => sections;
        public IReadOnlyList<SectorPlannerDebugGridPayload> GridPayloads => gridPayloads;
        public IReadOnlyList<string> Legend => legend;
        public SectorPlannerDebugMutationProof MutationProof { get; }
        public int SectionCount => sections.Count;
        public int TokenCount => sections.Sum(value => value.Tokens.Count);
        public int TextGridPayloadCount => gridPayloads.Count;
        public string CanonicalDigest { get; }
        public int FileWriteCount => 0;
        public int TilemapOwnershipClaimCount => 0;
    }

    public sealed class SectorPlannerDebugExportRequest
    {
        private readonly ReadOnlyCollection<SectorPlannerDebugToken> additionalTokens;

        public SectorPlannerDebugExportRequest(
            SectorPlannerRetryPlan retryPlan,
            IEnumerable<SectorPlannerDebugToken> sourceAdditionalTokens = null,
            string publicationLabel = "MAP14_09_REFERENCE_DEBUG_EXPORT",
            bool unsupportedFileWriteClaim = false,
            bool generatedAssetMutationClaim = false,
            bool editorWindowMutationClaim = false,
            bool tileMutationClaim = false,
            bool sceneMutationClaim = false,
            bool prefabMutationClaim = false,
            bool gameObjectMutationClaim = false,
            bool playModeClaim = false,
            bool exitApprovalClaim = false)
        {
            RetryPlan = retryPlan;
            additionalTokens = new ReadOnlyCollection<SectorPlannerDebugToken>((sourceAdditionalTokens ?? Array.Empty<SectorPlannerDebugToken>()).Where(value => value != null).ToArray());
            PublicationLabel = publicationLabel ?? string.Empty;
            UnsupportedFileWriteClaim = unsupportedFileWriteClaim;
            GeneratedAssetMutationClaim = generatedAssetMutationClaim;
            EditorWindowMutationClaim = editorWindowMutationClaim;
            TileMutationClaim = tileMutationClaim;
            SceneMutationClaim = sceneMutationClaim;
            PrefabMutationClaim = prefabMutationClaim;
            GameObjectMutationClaim = gameObjectMutationClaim;
            PlayModeClaim = playModeClaim;
            ExitApprovalClaim = exitApprovalClaim;
        }

        public SectorPlannerRetryPlan RetryPlan { get; }
        public IReadOnlyList<SectorPlannerDebugToken> AdditionalTokens => additionalTokens;
        public string PublicationLabel { get; }
        public bool UnsupportedFileWriteClaim { get; }
        public bool GeneratedAssetMutationClaim { get; }
        public bool EditorWindowMutationClaim { get; }
        public bool TileMutationClaim { get; }
        public bool SceneMutationClaim { get; }
        public bool PrefabMutationClaim { get; }
        public bool GameObjectMutationClaim { get; }
        public bool PlayModeClaim { get; }
        public bool ExitApprovalClaim { get; }
    }

    public sealed class SectorPlannerDebugExportResult
    {
        private readonly ReadOnlyCollection<SectorPlannerGrayboxFixture> fixtures;
        private readonly ReadOnlyCollection<SectorPlannerDebugExportError> errors;

        internal SectorPlannerDebugExportResult(
            SectorPlannerDebugExport export,
            SectorPlannerFailureRingSnapshot failureRing,
            IEnumerable<SectorPlannerGrayboxFixture> sourceFixtures,
            SectorPlannerGrayboxCoverageAudit coverageAudit,
            IEnumerable<SectorPlannerDebugExportError> sourceErrors)
        {
            var orderedErrors = (sourceErrors ?? Array.Empty<SectorPlannerDebugExportError>()).Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<SectorPlannerDebugExportError>(orderedErrors);
            if (orderedErrors.Length == 0)
            {
                Export = export;
                FailureRing = failureRing;
                fixtures = new ReadOnlyCollection<SectorPlannerGrayboxFixture>((sourceFixtures ?? Array.Empty<SectorPlannerGrayboxFixture>()).OrderBy(value => value).ToArray());
                CoverageAudit = coverageAudit;
            }
            else
            {
                Export = null;
                FailureRing = null;
                fixtures = new ReadOnlyCollection<SectorPlannerGrayboxFixture>(Array.Empty<SectorPlannerGrayboxFixture>());
                CoverageAudit = null;
            }
        }

        public bool Success => errors.Count == 0 && (Export != null || FailureRing != null || CoverageAudit != null);
        public SectorPlannerDebugExport Export { get; }
        public SectorPlannerFailureRingSnapshot FailureRing { get; }
        public IReadOnlyList<SectorPlannerGrayboxFixture> Fixtures => fixtures;
        public SectorPlannerGrayboxCoverageAudit CoverageAudit { get; }
        public IReadOnlyList<SectorPlannerDebugExportError> Errors => errors;
        public string CanonicalDigest => CoverageAudit != null ? CoverageAudit.CanonicalDigest :
            FailureRing != null ? FailureRing.CanonicalDigest : Export == null ? string.Empty : Export.CanonicalDigest;
        public int MutationCount => 0;
        public int NewRngDrawCount => 0;
        public int RetryExecutionCount => 0;
    }

    public static class SectorPlannerDebugExporter
    {
        public const string ReferencePublicationLabel = "MAP14_09_REFERENCE_DEBUG_EXPORT";

        public static SectorPlannerDebugExportResult Export(SectorPlannerDebugExportRequest request)
        {
            var errors = new List<SectorPlannerDebugExportError>();
            if (request == null)
            {
                Add(errors, SectorPlannerDebugExportErrorCode.MissingInput, "request", "A debug export request is required.");
                return Failed(errors);
            }

            ValidateRequest(request, errors);
            var retry = request.RetryPlan;
            var ownership = retry?.Request?.OwnershipPlan;
            var input = ownership?.Request?.Input;
            if (retry == null) Add(errors, SectorPlannerDebugExportErrorCode.MissingRetryPlan, "retryPlan", "MAP14_08 retry plan is required.");
            if (retry != null && ownership == null) Add(errors, SectorPlannerDebugExportErrorCode.MissingOwnershipPlan, "ownershipPlan", "MAP14_07 ownership plan is required.");
            if (ownership != null && input == null) Add(errors, SectorPlannerDebugExportErrorCode.MissingPlannerInput, "plannerInput", "MAP14_01 planner input is required.");
            if (retry != null && !retry.AllUpstreamIdentitiesPreserved)
                Add(errors, SectorPlannerDebugExportErrorCode.SectorMismatch, "identity", "MAP14_01~08 before/after identities must be equal.");
            if (errors.Count != 0) return Failed(errors);

            var anchor = ownership.Request.FixedAnchorPlan;
            var placement = ownership.Request.ClusterPlacementPlan;
            var spine = ownership.Request.SpineEnvelopePlan;
            var role = ownership.Request.RolePatternPlan;
            var render = ownership.Request.PatternRenderPlan;
            var quiet = ownership.Request.QuietActivityEventPlan;
            if (anchor == null || placement == null || spine == null || role == null || render == null || quiet == null)
            {
                Add(errors, SectorPlannerDebugExportErrorCode.MissingInput, "upstreamPlan", "Every MAP14_01~08 public plan is required.");
                return Failed(errors);
            }

            var assignments = ownership.Request.Assignments;
            var routeTokens = new List<SectorPlannerDebugToken>();
            var anchorTokens = AnchorTokens(anchor);
            var spineTokens = SpineTokens(spine);
            var clusterTokens = ClusterPatternTokens(placement, render);
            var quietTokens = QuietActivityEventTokens(quiet);
            var ownershipTokens = OwnershipTokens(ownership);
            var retryTokens = RetryTokens(retry);
            var sourceTokens = request.AdditionalTokens.ToList();
            var allTokens = sourceTokens.Concat(anchorTokens).Concat(spineTokens).Concat(clusterTokens)
                .Concat(quietTokens).Concat(ownershipTokens).Concat(retryTokens).ToArray();
            ValidateTokens(allTokens, errors);
            if (errors.Count != 0) return Failed(errors);

            var sections = new List<SectorPlannerDebugSection>
            {
                Section("MAP14_09_SOURCE_IDENTITY", SectorPlannerDebugSectionKind.SourceIdentity, "MAP14_01",
                    input.CanonicalDigest, "MAP14_01~08 source digests and version labels.",
                    SourceFacts(input, ownership, retry), sourceTokens),
                Section("MAP14_09_ROUTE_ACCESS", SectorPlannerDebugSectionKind.RouteAccess, "MAP14_01",
                    input.CanonicalDigest, "RouteType, AccessClass, sockets, and pacing decisions.",
                    RouteFacts(input, assignments), routeTokens),
                Section("MAP14_09_ANCHOR_BOUNDARY_SPECIAL", SectorPlannerDebugSectionKind.AnchorBoundarySpecial, "MAP14_02",
                    anchor.CanonicalDigest, "Fixed anchors, boundary identities, and SpecialRegion facts.",
                    AnchorFacts(input, anchor), anchorTokens),
                Section("MAP14_09_SPINE_ENVELOPE", SectorPlannerDebugSectionKind.SpineEnvelope, "MAP14_04",
                    spine.CanonicalDigest, "Spine nodes, edges, route envelope, and ProtectedOpen cells.",
                    SpineFacts(spine), spineTokens),
                Section("MAP14_09_CLUSTER_PATTERN", SectorPlannerDebugSectionKind.ClusterPattern, "MAP14_05",
                    render.CanonicalDigest, "Cluster placement, role zones, and MAP10 render evidence.",
                    ClusterFacts(placement, role, render), clusterTokens),
                Section("MAP14_09_QUIET_ACTIVITY_EVENT", SectorPlannerDebugSectionKind.QuietActivityEvent, "MAP14_06",
                    quiet.CanonicalDigest, "Quiet fill and marker-only Activity/Event decisions.",
                    QuietFacts(quiet), quietTokens),
                Section("MAP14_09_OWNERSHIP_PLANES", SectorPlannerDebugSectionKind.OwnershipPlanes, "MAP14_07",
                    ownership.CanonicalDigest, "Ownership planes, winners, suppressions, and conflicts.",
                    OwnershipFacts(ownership), ownershipTokens),
                Section("MAP14_09_RETRY_RNG", SectorPlannerDebugSectionKind.RetryRng, "MAP14_08",
                    retry.CanonicalDigest, "Retry terminal, stage counts, and RNG evidence without new draws.",
                    RetryFacts(retry), retryTokens),
                Section("MAP14_09_MUTATION_PROOF", SectorPlannerDebugSectionKind.MutationProof, "MAP14_09",
                    retry.CanonicalDigest, "All forbidden mutation and execution counters remain zero.",
                    MutationFacts(), Array.Empty<SectorPlannerDebugToken>()),
            };
            foreach (var duplicate in sections.GroupBy(value => value.SectionId, StringComparer.Ordinal).Where(value => value.Count() > 1))
                Add(errors, SectorPlannerDebugExportErrorCode.DuplicateSection, duplicate.Key, "Section IDs must be unique.");
            if (errors.Count != 0) return Failed(errors);

            var grids = input.Sectors.Select(sector => Grid(sector.Coordinate, allTokens)).ToArray();
            var legend = Enum.GetValues(typeof(SectorPlannerDebugTokenKind)).Cast<SectorPlannerDebugTokenKind>()
                .Select(kind => Symbol(kind) + "=" + kind).ToArray();
            var export = new SectorPlannerDebugExport(SectorPlannerDebugExportKind.SuccessPlan,
                request.PublicationLabel, retry.CanonicalDigest, sections, grids, legend,
                new SectorPlannerDebugMutationProof());
            return new SectorPlannerDebugExportResult(export, null, null, null, errors);
        }

        internal static string Symbol(SectorPlannerDebugTokenKind kind)
        {
            switch (kind)
            {
                case SectorPlannerDebugTokenKind.Empty: return ".";
                case SectorPlannerDebugTokenKind.Terrain: return "T";
                case SectorPlannerDebugTokenKind.Solid: return "#";
                case SectorPlannerDebugTokenKind.ProtectedOpen: return "O";
                case SectorPlannerDebugTokenKind.Reservation: return "R";
                case SectorPlannerDebugTokenKind.Boundary: return "B";
                case SectorPlannerDebugTokenKind.Special: return "S";
                case SectorPlannerDebugTokenKind.Spine: return "=";
                case SectorPlannerDebugTokenKind.Cluster: return "C";
                case SectorPlannerDebugTokenKind.Pattern: return "P";
                case SectorPlannerDebugTokenKind.Quiet: return "q";
                case SectorPlannerDebugTokenKind.ActivityMarker: return "A";
                case SectorPlannerDebugTokenKind.EventMarker: return "E";
                case SectorPlannerDebugTokenKind.Suppressed: return "x";
                case SectorPlannerDebugTokenKind.Conflict: return "!";
                case SectorPlannerDebugTokenKind.RetryNode: return "r";
                case SectorPlannerDebugTokenKind.FailureCenter: return "F";
                case SectorPlannerDebugTokenKind.NeighborContext: return "N";
                default: return "?";
            }
        }

        internal static bool Inside(LocalTileCoord coordinate) =>
            coordinate.X >= 0 && coordinate.X < 48 && coordinate.Y >= 0 && coordinate.Y < 32;

        internal static SectorPlannerDebugSection Section(
            string id, SectorPlannerDebugSectionKind kind, string task, string digest, string summary,
            IEnumerable<SectorPlannerDebugFact> facts, IEnumerable<SectorPlannerDebugToken> tokens) =>
            new SectorPlannerDebugSection(id, kind, SectorPlannerDebugSeverity.Info, task, digest, summary, facts, tokens);

        internal static void Add(ICollection<SectorPlannerDebugExportError> errors,
            SectorPlannerDebugExportErrorCode code, string subject, string detail) =>
            errors.Add(new SectorPlannerDebugExportError(code, subject, detail));

        internal static SectorPlannerDebugExportResult Failed(IEnumerable<SectorPlannerDebugExportError> errors) =>
            new SectorPlannerDebugExportResult(null, null, null, null, errors);

        private static void ValidateRequest(SectorPlannerDebugExportRequest request, ICollection<SectorPlannerDebugExportError> errors)
        {
            if (!string.Equals(request.PublicationLabel, ReferencePublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorPlannerDebugExportErrorCode.NonCanonicalPublication, "publicationLabel", "Debug export publication must use the MAP14_09 reference label.");
            if (request.UnsupportedFileWriteClaim) Add(errors, SectorPlannerDebugExportErrorCode.UnsupportedFileWriteClaim, "file", "Debug publication is in-memory only.");
            if (request.GeneratedAssetMutationClaim) Add(errors, SectorPlannerDebugExportErrorCode.GeneratedAssetMutationClaim, "asset", "Generated asset mutation is forbidden.");
            if (request.EditorWindowMutationClaim) Add(errors, SectorPlannerDebugExportErrorCode.EditorWindowMutationClaim, "editor", "EditorWindow and overlay mutation are forbidden.");
            if (request.TileMutationClaim) Add(errors, SectorPlannerDebugExportErrorCode.TileMutationClaim, "tile", "Tilemap ownership or write is forbidden.");
            if (request.SceneMutationClaim) Add(errors, SectorPlannerDebugExportErrorCode.SceneMutationClaim, "scene", "Scene mutation is forbidden.");
            if (request.PrefabMutationClaim) Add(errors, SectorPlannerDebugExportErrorCode.PrefabMutationClaim, "prefab", "Prefab mutation is forbidden.");
            if (request.GameObjectMutationClaim) Add(errors, SectorPlannerDebugExportErrorCode.GameObjectMutationClaim, "gameObject", "GameObject mutation is forbidden.");
            if (request.PlayModeClaim) Add(errors, SectorPlannerDebugExportErrorCode.PlayModeClaim, "playMode", "PlayMode publication is outside MAP14_09.");
            if (request.ExitApprovalClaim) Add(errors, SectorPlannerDebugExportErrorCode.ExitApprovalClaim, "exit", "MAP14 exit approval belongs to MAP14_10.");
        }

        private static void ValidateTokens(IEnumerable<SectorPlannerDebugToken> tokens, ICollection<SectorPlannerDebugExportError> errors)
        {
            var values = tokens.OrderBy(value => value).ToArray();
            foreach (var token in values.Where(value => !Inside(value.Coordinate)))
                Add(errors, SectorPlannerDebugExportErrorCode.DebugTokenOutOfBounds, token.Identity, "Debug tokens must remain inside 48x32 sector-local coordinates.");
            foreach (var duplicate in values.GroupBy(value => value.Identity, StringComparer.Ordinal).Where(value => value.Count() > 1))
                Add(errors, SectorPlannerDebugExportErrorCode.DuplicateDebugToken, duplicate.Key, "Debug token identity must be unique.");
        }

        private static IEnumerable<SectorPlannerDebugFact> SourceFacts(
            SectorPlannerInput input, SectorCanvasOwnershipPlan ownership, SectorPlannerRetryPlan retry)
        {
            return new[]
            {
                Fact("MAP14_01_INPUT", input.CanonicalDigest),
                Fact("MAP14_01_PACING", ownership.PacingAssignmentDigestBefore),
                Fact("MAP14_02_ANCHOR", ownership.FixedAnchorPlanDigestBefore),
                Fact("MAP14_03_CLUSTER", ownership.ClusterPlacementPlanDigestBefore),
                Fact("MAP14_04_SPINE", ownership.SpineEnvelopePlanDigestBefore),
                Fact("MAP14_05_ROLE_PATTERN", ownership.RolePatternPlanDigestBefore),
                Fact("MAP14_05_RENDER", ownership.PatternRenderPlanDigestBefore),
                Fact("MAP14_06_QUIET_ACTIVITY_EVENT", ownership.QuietActivityEventPlanDigestBefore),
                Fact("MAP14_07_OWNERSHIP", ownership.CanonicalDigest),
                Fact("MAP14_08_RETRY", retry.CanonicalDigest),
                Fact("MAP12_ACTIVITY_AUTHORITY", retry.ActivityAuthorityDigestBefore),
                Fact("MAP12_EVENT_AUTHORITY", retry.EventAuthorityDigestBefore),
                Fact("IDENTITIES_PRESERVED", retry.AllUpstreamIdentitiesPreserved),
            };
        }

        private static IEnumerable<SectorPlannerDebugFact> RouteFacts(
            SectorPlannerInput input, IEnumerable<SectorPacingAssignment> assignments)
        {
            var pacing = (assignments ?? Array.Empty<SectorPacingAssignment>()).ToDictionary(value => value.Coordinate);
            return input.Sectors.Select(sector =>
            {
                pacing.TryGetValue(sector.Coordinate, out var assignment);
                return Fact("SECTOR_" + sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture),
                    "route=" + sector.Route.RouteType.ToString(CultureInfo.InvariantCulture) +
                    ";access=" + sector.Route.AccessClass +
                    ";sockets=" + string.Join(",", sector.Route.ExternalSockets) +
                    ";pacing=" + (assignment == null ? string.Empty : assignment.PrimaryRole.ToString()) +
                    ";reasons=" + (assignment == null ? string.Empty : string.Join(",", assignment.Reasons)));
            });
        }

        private static IEnumerable<SectorPlannerDebugFact> AnchorFacts(SectorPlannerInput input, SectorFixedAnchorPlan anchor)
        {
            var facts = new List<SectorPlannerDebugFact>
            {
                Fact("ANCHORS", anchor.Anchors.Count),
                Fact("COMPATIBLE_OVERLAPS", anchor.CompatibleOverlapCount),
                Fact("BOUNDARY_IDENTITIES", string.Join(",", input.Sectors.SelectMany(value => value.Boundaries).Select(value => value.PairId + ":" + value.CandidateId).Distinct().OrderBy(value => value, StringComparer.Ordinal))),
                Fact("SPECIAL_IDENTITIES", string.Join(",", input.Sectors.Select(value => value.SpecialRegion).Where(value => value.Kind != SectorPlannerSpecialRegionKind.None).Select(value => value.Kind + ":" + value.Binding + ":" + value.RegionId).Concat(input.Sectors.SelectMany(value => value.OptionalRegions).Select(value => value.Kind + ":DeferredOptionalLocal:" + value.RegionId)).Distinct().OrderBy(value => value, StringComparer.Ordinal))),
            };
            foreach (SectorFixedAnchorKind kind in Enum.GetValues(typeof(SectorFixedAnchorKind))) facts.Add(Fact("ANCHOR_" + kind, anchor.Count(kind)));
            return facts;
        }

        private static IEnumerable<SectorPlannerDebugFact> SpineFacts(SectorSpineEnvelopePlan spine) => new[]
        {
            Fact("NODES", spine.NodeCount), Fact("EDGES", spine.EdgeCount),
            Fact("ENVELOPE_CELLS", spine.EnvelopeCellCount), Fact("PROTECTED_OPEN", spine.ProtectedOpenCellCount),
            Fact("MANDATORY_ROUTES", spine.MandatoryRouteCount), Fact("OPTIONAL_RECOVERY_ROUTES", spine.OptionalHighRecoveryRouteCount),
            Fact("ENVELOPE_DIGEST", spine.EnvelopeDigest),
        };

        private static IEnumerable<SectorPlannerDebugFact> ClusterFacts(
            SectorClusterPlacementPlan placement, SectorClusterRolePatternPlan role, SectorPatternRenderPlan render) => new[]
        {
            Fact("PLACEMENTS", placement.AcceptedPlacementCount), Fact("FOOTPRINT_CELLS", placement.PlacedFootprintCellCount),
            Fact("ROLE_CELLS", role.RoleCellCount), Fact("PATTERN_ZONES", role.PatternZoneCount),
            Fact("SELECTED_PATTERNS", render.SelectedPatternCount), Fact("RENDER_CELLS", render.RenderTargetCellCount),
            Fact("MAP10_PLANNER", render.Map10ApplicationPlannerType), Fact("MAP10_RENDERER", render.Map10OrderedRendererType),
            Fact("PROTECTED_WRITES", render.ProtectedWriteCount), Fact("RENDER_CONFLICTS", render.RendererConflictCount),
        };

        private static IEnumerable<SectorPlannerDebugFact> QuietFacts(SectorQuietActivityEventPlan quiet) => new[]
        {
            Fact("QUIET_FILL", quiet.QuietFillPlan.QuietFillCellCount), Fact("BUFFER", quiet.QuietFillPlan.BufferCellCount),
            Fact("PROTECTED_NO_WRITE", quiet.QuietFillPlan.ProtectedCoordinateCount), Fact("RESERVED_NO_WRITE", quiet.QuietFillPlan.ReservedCoordinateCount),
            Fact("ACTIVITY_SELECTED", quiet.ActivitySelectedCount), Fact("EVENT_NON_EMPTY", quiet.EventAssignedNonEmptyCount),
            Fact("EVENT_EMPTY", quiet.EventAssignedEmptyCount), Fact("ACTIVITY_MAP12_DRAWS", quiet.ActivityMap12RngDrawCount),
            Fact("EVENT_MAP12_DRAWS", quiet.EventMap12RngDrawCount),
        };

        private static IEnumerable<SectorPlannerDebugFact> OwnershipFacts(SectorCanvasOwnershipPlan ownership)
        {
            var facts = new List<SectorPlannerDebugFact>
            {
                Fact("CLAIMS", ownership.ClaimCount), Fact("WINNERS", ownership.WinnerClaimCount),
                Fact("SUPPRESSIONS", ownership.SuppressedClaimCount), Fact("CONFLICTS", ownership.ConflictCount),
                Fact("COVERAGE", ownership.CoverageCount), Fact("EXPECTED_COVERAGE", ownership.ExpectedCoverageCount),
            };
            foreach (SectorCanvasOwnershipPlane plane in Enum.GetValues(typeof(SectorCanvasOwnershipPlane))) facts.Add(Fact("PLANE_" + plane, ownership.CountOwned(plane)));
            foreach (SectorCanvasOwnerKind owner in Enum.GetValues(typeof(SectorCanvasOwnerKind))) facts.Add(Fact("OWNER_" + owner, ownership.CountWinners(owner)));
            return facts;
        }

        private static IEnumerable<SectorPlannerDebugFact> RetryFacts(SectorPlannerRetryPlan retry)
        {
            var facts = new List<SectorPlannerDebugFact>
            {
                Fact("TERMINAL", retry.TerminalDecision), Fact("ATTEMPTS", retry.AttemptTraces.Count),
                Fact("RETRY_NODES", retry.RetryNodeCount), Fact("MAP14_RNG_DRAWS", retry.Map14RetryRngDrawCount),
                Fact("MAP12_ACTIVITY_DRAWS", retry.Map12ActivityRngDrawCount), Fact("MAP12_EVENT_DRAWS", retry.Map12EventRngDrawCount),
                Fact("NEW_RNG_DRAWS", 0), Fact("RETRY_EXECUTIONS", 0),
            };
            foreach (SectorPlannerRetryStage stage in Enum.GetValues(typeof(SectorPlannerRetryStage))) facts.Add(Fact("STAGE_" + stage, retry.Count(stage)));
            return facts;
        }

        private static IEnumerable<SectorPlannerDebugFact> MutationFacts() => new[]
        {
            Fact("RETRY_EXECUTION", 0), Fact("NEW_RNG_DRAW", 0), Fact("FALLBACK_CORRIDOR", 0),
            Fact("VALIDATION_RELAXATION", 0), Fact("SECTOR_RERANDOM", 0), Fact("WORLD_RERANDOM", 0),
            Fact("FIXED_ANCHOR_MUTATION", 0), Fact("BOUNDARY_SOCKET_MUTATION", 0), Fact("SPECIAL_RESERVATION_MUTATION", 0),
            Fact("PROTECTED_MASK_REMOVAL", 0), Fact("TILEMAP_WRITE", 0), Fact("SCENE_MUTATION", 0),
            Fact("PREFAB_MUTATION", 0), Fact("GAMEOBJECT_MUTATION", 0), Fact("EDITOR_WINDOW_MUTATION", 0),
            Fact("GENERATED_DEBUG_FILE_WRITE", 0), Fact("ACTIVITY_RUNTIME_SPAWN", 0), Fact("EVENT_RUNTIME_SPAWN", 0),
            Fact("GAMEPLAY_EXECUTION", 0), Fact("MAP14_EXIT_APPROVAL", 0),
        };

        private static List<SectorPlannerDebugToken> AnchorTokens(SectorFixedAnchorPlan anchor) => anchor.Anchors.Select(value =>
        {
            var kind = value.Kind == SectorFixedAnchorKind.BoundaryFixedSlice || value.Kind == SectorFixedAnchorKind.BoundaryWarning
                ? SectorPlannerDebugTokenKind.Boundary
                : value.Kind == SectorFixedAnchorKind.SpecialFootprint || value.Kind == SectorFixedAnchorKind.SpecialEntryReturn || value.Kind == SectorFixedAnchorKind.SpecialApronBuffer
                    ? SectorPlannerDebugTokenKind.Special : SectorPlannerDebugTokenKind.Reservation;
            return new SectorPlannerDebugToken(value.SectorCoordinate, new LocalTileCoord(value.Rect.X, value.Rect.Y), kind,
                value.AnchorId, value.Source.ToString(), value.SourceIdentity);
        }).ToList();

        private static List<SectorPlannerDebugToken> SpineTokens(SectorSpineEnvelopePlan spine)
        {
            var result = spine.Graph.Nodes.Select(value => new SectorPlannerDebugToken(value.SectorCoordinate, value.Coordinate,
                SectorPlannerDebugTokenKind.Spine, value.NodeId, value.Kind.ToString(), value.SourceIdentity)).ToList();
            result.AddRange(spine.ProtectedOpenCells.Select(value => new SectorPlannerDebugToken(
                new SectorCoord(value.SectorIndex % 13, value.SectorIndex / 13), value.Coordinate, SectorPlannerDebugTokenKind.ProtectedOpen,
                value.EdgeId + ":" + value.Coordinate.X + "," + value.Coordinate.Y, value.Kind.ToString(), value.SourceIdentity)));
            return result;
        }

        private static List<SectorPlannerDebugToken> ClusterPatternTokens(SectorClusterPlacementPlan placement, SectorPatternRenderPlan render)
        {
            var result = placement.Placements.Select(value => new SectorPlannerDebugToken(value.SectorCoordinate,
                new LocalTileCoord(value.OriginX * 12, value.OriginY * 8), SectorPlannerDebugTokenKind.Cluster,
                value.ClusterId.Value + ":" + value.VariantId.Value, "MAP14_03", placement.CanonicalDigest)).ToList();
            foreach (var group in render.RenderCells.GroupBy(value => value.SectorIndex).OrderBy(value => value.Key))
            {
                result.AddRange(group.OrderBy(value => value).Take(8).Select(value => new SectorPlannerDebugToken(
                    value.SectorCoordinate, value.Coordinate, SectorPlannerDebugTokenKind.Pattern,
                    "PATTERN_CELL_" + value.Coordinate.X + "_" + value.Coordinate.Y,
                    "MAP10_RENDER", render.CanonicalDigest)));
            }
            return result;
        }

        private static List<SectorPlannerDebugToken> QuietActivityEventTokens(SectorQuietActivityEventPlan quiet)
        {
            var result = new List<SectorPlannerDebugToken>();
            foreach (var group in quiet.QuietFillPlan.Cells.Where(value => value.IsQuietFill).GroupBy(value => value.SectorIndex).OrderBy(value => value.Key))
            {
                var value = group.OrderBy(item => item).First();
                result.Add(new SectorPlannerDebugToken(value.SectorCoordinate, value.Coordinate, SectorPlannerDebugTokenKind.Quiet,
                    value.Kind + ":" + value.Coordinate.X + "," + value.Coordinate.Y, value.SourceKind.ToString(), value.SourceIdentity));
            }
            result.AddRange(quiet.ActivityDecisions.Where(value => value.State == SectorActivityEventPlacementState.Selected).Select(value =>
                new SectorPlannerDebugToken(value.Opportunity.SectorCoordinate, value.Opportunity.MarkerCoordinate,
                    SectorPlannerDebugTokenKind.ActivityMarker, value.OpportunityId, value.Opportunity.MarkerKind.ToString(), value.CandidateKey)));
            result.AddRange(quiet.EventDecisions.Where(value => value.State == SectorActivityEventPlacementState.Assigned || value.State == SectorActivityEventPlacementState.ExplicitEmpty).Select(value =>
                new SectorPlannerDebugToken(value.Opportunity.SectorCoordinate, value.Opportunity.MarkerCoordinate,
                    SectorPlannerDebugTokenKind.EventMarker, value.OpportunityId, value.Opportunity.MarkerKind.ToString(), value.CandidateKey)));
            return result;
        }

        private static List<SectorPlannerDebugToken> OwnershipTokens(SectorCanvasOwnershipPlan ownership)
        {
            var result = new List<SectorPlannerDebugToken>();
            foreach (var group in ownership.OwnedCells.GroupBy(value => value.SectorIndex + "|" + value.Plane).OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                var value = group.OrderBy(item => item).First();
                result.Add(new SectorPlannerDebugToken(value.SectorCoordinate, value.Coordinate,
                    value.Plane == SectorCanvasOwnershipPlane.Terrain ? SectorPlannerDebugTokenKind.Terrain : SectorPlannerDebugTokenKind.Reservation,
                    value.WinnerClaimId, value.OwnerKind.ToString(), value.SourceObjectId));
            }
            result.AddRange(ownership.SuppressedClaims.GroupBy(value => value.SectorIndex).OrderBy(value => value.Key).Select(group => group.OrderBy(value => value).First()).Select(value =>
                new SectorPlannerDebugToken(value.SectorCoordinate, value.Coordinate, SectorPlannerDebugTokenKind.Suppressed,
                    value.SuppressedClaimId, value.SuppressedOwnerKind.ToString(), value.WinnerClaimId)));
            result.AddRange(ownership.Conflicts.Select(value => new SectorPlannerDebugToken(value.SectorCoordinate, value.Coordinate,
                SectorPlannerDebugTokenKind.Conflict, value.Code.ToString(), value.Plane.ToString(), string.Join(",", value.ClaimIds))));
            return result;
        }

        private static List<SectorPlannerDebugToken> RetryTokens(SectorPlannerRetryPlan retry) => retry.NodeTraces.Select(value =>
            new SectorPlannerDebugToken(retry.Request.SectorCoordinate,
                new LocalTileCoord(value.AttemptTrace.NodeOrdinal % 48, 31), SectorPlannerDebugTokenKind.RetryNode,
                value.Stage + ":" + value.SelectedCandidateId, value.AttemptTrace.Failure.Owner.ToString(),
                value.RngTrace == null ? value.AttemptTrace.Reason : value.RngTrace.CanonicalDigest)).ToList();

        private static SectorPlannerDebugGridPayload Grid(SectorCoord sector, IEnumerable<SectorPlannerDebugToken> tokens)
        {
            var cells = tokens.Where(value => value.SectorCoordinate == sector).OrderBy(value => value)
                .Select(value => value.Symbol + "@" + value.Coordinate.X.ToString(CultureInfo.InvariantCulture) + "," +
                    value.Coordinate.Y.ToString(CultureInfo.InvariantCulture) + ":" + value.Label).ToArray();
            return new SectorPlannerDebugGridPayload(sector, new[] { cells.Length == 0 ? "." : string.Join(";", cells) });
        }

        private static SectorPlannerDebugFact Fact(string key, object value) =>
            new SectorPlannerDebugFact(key, Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    public static class SectorPlannerDebugCanonicalDigest
    {
        public static string ComputeSection(SectorPlannerDebugSection section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            return Hash(string.Join("\n", new[]
            {
                section.SectionId, section.Kind.ToString(), section.Severity.ToString(), section.SourceTaskId,
                section.SourceDigest, section.Summary,
                string.Join("\n", section.Facts.Select(value => value.ToString())),
                string.Join("\n", section.Tokens.Select(value => value.Identity)),
            }));
        }

        public static string ComputeExport(SectorPlannerDebugExport export)
        {
            if (export == null) throw new ArgumentNullException(nameof(export));
            return Hash(string.Join("\n", new[]
            {
                export.Kind.ToString(), export.PublicationLabel, export.SourceDigest,
                string.Join("\n", export.Sections.Select(value => value.CanonicalDigest)),
                string.Join("\n", export.GridPayloads.Select(value => value.CanonicalDigest)),
                string.Join("\n", export.Legend),
                export.MutationProof.TotalMutationCount.ToString(CultureInfo.InvariantCulture),
            }));
        }

        public static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(new UTF8Encoding(false).GetBytes(value ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
