using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using StarNight.MapAuthoring.WorldGeneration.Import;

namespace StarNight.MapAuthoring.WorldGeneration.TerrainClusters
{
    public enum TerrainClusterPreviewMode
    {
        PatternFree = 1,
        PatternA = 2,
        PatternB = 3,
    }

    public sealed class TerrainClusterPreviewRequest
    {
        public TerrainClusterPreviewRequest(string clusterId, string variantId, TerrainClusterPreviewMode mode)
        {
            ClusterId = clusterId ?? string.Empty;
            VariantId = variantId ?? string.Empty;
            Mode = mode;
        }

        public string ClusterId { get; }
        public string VariantId { get; }
        public TerrainClusterPreviewMode Mode { get; }
    }

    public sealed class TerrainClusterPreviewCell
    {
        private readonly ReadOnlyCollection<string> tokens;

        internal TerrainClusterPreviewCell(
            LocalTileCoord localCoordinate,
            LocalTileCoord frameCoordinate,
            ClusterChunkCoord owningChunk,
            bool active,
            string occupancy,
            IEnumerable<string> sourceTokens)
        {
            LocalCoordinate = localCoordinate;
            FrameCoordinate = frameCoordinate;
            OwningChunk = owningChunk;
            Active = active;
            Occupancy = occupancy ?? string.Empty;
            tokens = CopyStrings(sourceTokens);
        }

        public LocalTileCoord LocalCoordinate { get; }
        public LocalTileCoord FrameCoordinate { get; }
        public ClusterChunkCoord OwningChunk { get; }
        public bool Active { get; }
        public string Occupancy { get; }
        public IReadOnlyList<string> Tokens => tokens;

        private static ReadOnlyCollection<string> CopyStrings(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public sealed class TerrainClusterOverlaySegment
    {
        internal TerrainClusterOverlaySegment(
            LocalTileCoord start,
            LocalTileCoord end,
            string token,
            string detail)
        {
            Start = start;
            End = end;
            Token = token ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public LocalTileCoord Start { get; }
        public LocalTileCoord End { get; }
        public string Token { get; }
        public string Detail { get; }
    }

    public sealed class TerrainClusterPreviewAnchor
    {
        internal TerrainClusterPreviewAnchor(LocalTileCoord coordinate, string token, string detail)
        {
            Coordinate = coordinate;
            Token = token ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public LocalTileCoord Coordinate { get; }
        public string Token { get; }
        public string Detail { get; }
    }

    public sealed class TerrainClusterPatternDiffSnapshot
    {
        private readonly ReadOnlyCollection<LocalTileCoord> changedCoordinates;

        internal TerrainClusterPatternDiffSnapshot(
            string patternId,
            MicroPatternTransform transform,
            LocalTileCoord origin,
            string placementId,
            string applicationPlanDigest,
            string renderDigest,
            string beforeDigest,
            string afterDigest,
            IEnumerable<LocalTileCoord> sourceChangedCoordinates,
            int targetCount,
            int protectedWriteCount,
            int protectedValueChangeCount)
        {
            PatternId = patternId ?? string.Empty;
            Transform = transform;
            Origin = origin;
            PlacementId = placementId ?? string.Empty;
            ApplicationPlanDigest = applicationPlanDigest ?? string.Empty;
            RenderDigest = renderDigest ?? string.Empty;
            BeforeDigest = beforeDigest ?? string.Empty;
            AfterDigest = afterDigest ?? string.Empty;
            changedCoordinates = CopyCoordinates(sourceChangedCoordinates);
            TargetCount = targetCount;
            ProtectedWriteCount = protectedWriteCount;
            ProtectedValueChangeCount = protectedValueChangeCount;
        }

        public bool IsPatternFree => PatternId.Length == 0;
        public string PatternId { get; }
        public MicroPatternTransform Transform { get; }
        public LocalTileCoord Origin { get; }
        public string PlacementId { get; }
        public string ApplicationPlanDigest { get; }
        public string RenderDigest { get; }
        public string BeforeDigest { get; }
        public string AfterDigest { get; }
        public IReadOnlyList<LocalTileCoord> ChangedCoordinates => changedCoordinates;
        public int TargetCount { get; }
        public int ChangedCount => changedCoordinates.Count;
        public int ProtectedWriteCount { get; }
        public int ProtectedValueChangeCount { get; }

        private static ReadOnlyCollection<LocalTileCoord> CopyCoordinates(IEnumerable<LocalTileCoord> source) =>
            new ReadOnlyCollection<LocalTileCoord>((source ?? Array.Empty<LocalTileCoord>())
                .Distinct().OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
    }

    public sealed class TerrainClusterChunkDensitySnapshot
    {
        internal TerrainClusterChunkDensitySnapshot(ClusterChunkCoord chunk, int solidCount, int airCount)
        {
            Chunk = chunk;
            SolidCount = solidCount;
            AirCount = airCount;
        }

        public ClusterChunkCoord Chunk { get; }
        public int SolidCount { get; }
        public int AirCount { get; }
        public int ActiveCount => SolidCount + AirCount;
    }

    public sealed class TerrainClusterDensitySnapshot
    {
        private readonly ReadOnlyCollection<TerrainClusterChunkDensitySnapshot> chunks;

        internal TerrainClusterDensitySnapshot(
            int activeCount,
            int solidCount,
            int absoluteProtectedCount,
            int patternTargetCount,
            int patternChangedCount,
            int affordanceCount,
            int markerCount,
            IEnumerable<TerrainClusterChunkDensitySnapshot> sourceChunks)
        {
            ActiveCount = activeCount;
            SolidCount = solidCount;
            AbsoluteProtectedCount = absoluteProtectedCount;
            PatternTargetCount = patternTargetCount;
            PatternChangedCount = patternChangedCount;
            AffordanceCount = affordanceCount;
            MarkerCount = markerCount;
            chunks = new ReadOnlyCollection<TerrainClusterChunkDensitySnapshot>(
                (sourceChunks ?? Array.Empty<TerrainClusterChunkDensitySnapshot>())
                    .OrderBy(value => value.Chunk.Y).ThenBy(value => value.Chunk.X).ToArray());
        }

        public int ActiveCount { get; }
        public int SolidCount { get; }
        public int AirCount => ActiveCount - SolidCount;
        public int AbsoluteProtectedCount { get; }
        public int PatternTargetCount { get; }
        public int PatternChangedCount { get; }
        public int AffordanceCount { get; }
        public int MarkerCount { get; }
        public IReadOnlyList<TerrainClusterChunkDensitySnapshot> Chunks => chunks;
        public string SolidRatio => Ratio(SolidCount, ActiveCount);
        public string AirRatio => Ratio(AirCount, ActiveCount);
        public string ProtectedRatio => Ratio(AbsoluteProtectedCount, ActiveCount);
        public string PatternChangedRatio => Ratio(PatternChangedCount, ActiveCount);

        private static string Ratio(int value, int total) => total == 0
            ? "0"
            : ((decimal)value / total).ToString("0.######", CultureInfo.InvariantCulture);
    }

    public sealed class TerrainClusterSectorFrameSnapshot
    {
        public const int Width = 48;
        public const int Height = 32;
        public const int ChunkWidth = 12;
        public const int ChunkHeight = 8;

        private readonly ReadOnlyCollection<LocalTileCoord> activeCoordinates;

        internal TerrainClusterSectorFrameSnapshot(
            int offsetX,
            int offsetY,
            IEnumerable<LocalTileCoord> sourceActiveCoordinates)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            activeCoordinates = new ReadOnlyCollection<LocalTileCoord>(
                (sourceActiveCoordinates ?? Array.Empty<LocalTileCoord>())
                    .Distinct().OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
        }

        public int OffsetX { get; }
        public int OffsetY { get; }
        public IReadOnlyList<LocalTileCoord> ActiveCoordinates => activeCoordinates;
        public int GridColumnCount => 4;
        public int GridRowCount => 4;
        public string EmptySpaceToken => "UNOWNED_DIAGNOSTIC";

        public LocalTileCoord Translate(LocalTileCoord local) =>
            new LocalTileCoord(local.X + OffsetX, local.Y + OffsetY);

        public bool Contains(LocalTileCoord coordinate) =>
            coordinate.X >= 0 && coordinate.X < Width && coordinate.Y >= 0 && coordinate.Y < Height;
    }

    public sealed class TerrainClusterPreviewSnapshot
    {
        private readonly ReadOnlyCollection<CompiledClusterChunkCell> chunkCells;
        private readonly ReadOnlyCollection<TerrainClusterPreviewCell> cells;
        private readonly ReadOnlyCollection<TerrainClusterPreviewAnchor> anchors;
        private readonly ReadOnlyCollection<TerrainClusterOverlaySegment> segments;
        private readonly ReadOnlyCollection<LocalTileCoord> envelopeCoordinates;
        private readonly ReadOnlyCollection<LocalTileCoord> absoluteProtectedCoordinates;
        private readonly ReadOnlyCollection<LocalTileCoord> baselineCoordinates;
        private readonly ReadOnlyCollection<LocalTileCoord> highRouteCoordinates;
        private readonly ReadOnlyCollection<LocalTileCoord> recoveryCoordinates;
        private readonly ReadOnlyCollection<string> routeEvidence;
        private readonly ReadOnlyCollection<string> quietEvidence;

        internal TerrainClusterPreviewSnapshot(
            string clusterId,
            MoonpalaceBiomeId biome,
            PacingRole pacingRole,
            string footprintVariantId,
            string variantId,
            bool isBaselineVariant,
            string structuralSignature,
            string catalogDigest,
            string contractDigest,
            string canvasDigest,
            string roleSocketDigest,
            string traversalDigest,
            string routeWitnessDigest,
            TerrainClusterPatternDiffSnapshot pattern,
            TerrainClusterDensitySnapshot density,
            TerrainClusterSectorFrameSnapshot sectorFrame,
            IEnumerable<CompiledClusterChunkCell> sourceChunkCells,
            IEnumerable<TerrainClusterPreviewCell> sourceCells,
            IEnumerable<TerrainClusterPreviewAnchor> sourceAnchors,
            IEnumerable<TerrainClusterOverlaySegment> sourceSegments,
            IEnumerable<LocalTileCoord> sourceEnvelopeCoordinates,
            IEnumerable<LocalTileCoord> sourceProtectedCoordinates,
            IEnumerable<LocalTileCoord> sourceBaselineCoordinates,
            IEnumerable<LocalTileCoord> sourceHighRouteCoordinates,
            IEnumerable<LocalTileCoord> sourceRecoveryCoordinates,
            IEnumerable<string> sourceRouteEvidence,
            IEnumerable<string> sourceQuietEvidence,
            string stableDigest)
        {
            ClusterId = clusterId ?? string.Empty;
            Biome = biome;
            PacingRole = pacingRole;
            FootprintVariantId = footprintVariantId ?? string.Empty;
            VariantId = variantId ?? string.Empty;
            IsBaselineVariant = isBaselineVariant;
            StructuralSignature = structuralSignature ?? string.Empty;
            CatalogDigest = catalogDigest ?? string.Empty;
            ContractDigest = contractDigest ?? string.Empty;
            CanvasDigest = canvasDigest ?? string.Empty;
            RoleSocketDigest = roleSocketDigest ?? string.Empty;
            TraversalDigest = traversalDigest ?? string.Empty;
            RouteWitnessDigest = routeWitnessDigest ?? string.Empty;
            Pattern = pattern;
            Density = density;
            SectorFrame = sectorFrame;
            chunkCells = new ReadOnlyCollection<CompiledClusterChunkCell>((sourceChunkCells ?? Array.Empty<CompiledClusterChunkCell>())
                .OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X).ToArray());
            cells = new ReadOnlyCollection<TerrainClusterPreviewCell>((sourceCells ?? Array.Empty<TerrainClusterPreviewCell>())
                .OrderBy(value => value.LocalCoordinate.Y).ThenBy(value => value.LocalCoordinate.X).ToArray());
            anchors = new ReadOnlyCollection<TerrainClusterPreviewAnchor>((sourceAnchors ?? Array.Empty<TerrainClusterPreviewAnchor>())
                .OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X)
                .ThenBy(value => value.Token, StringComparer.Ordinal).ToArray());
            segments = new ReadOnlyCollection<TerrainClusterOverlaySegment>((sourceSegments ?? Array.Empty<TerrainClusterOverlaySegment>())
                .OrderBy(value => value.Token, StringComparer.Ordinal).ThenBy(value => value.Detail, StringComparer.Ordinal).ToArray());
            envelopeCoordinates = CopyCoordinates(sourceEnvelopeCoordinates);
            absoluteProtectedCoordinates = CopyCoordinates(sourceProtectedCoordinates);
            baselineCoordinates = CopyCoordinates(sourceBaselineCoordinates);
            highRouteCoordinates = CopyCoordinates(sourceHighRouteCoordinates);
            recoveryCoordinates = CopyCoordinates(sourceRecoveryCoordinates);
            routeEvidence = CopyStrings(sourceRouteEvidence);
            quietEvidence = CopyStrings(sourceQuietEvidence);
            StableDigest = stableDigest ?? string.Empty;
        }

        public string ClusterId { get; }
        public MoonpalaceBiomeId Biome { get; }
        public PacingRole PacingRole { get; }
        public string FootprintVariantId { get; }
        public string VariantId { get; }
        public bool IsBaselineVariant { get; }
        public string StructuralSignature { get; }
        public string CatalogDigest { get; }
        public string ContractDigest { get; }
        public string CanvasDigest { get; }
        public string RoleSocketDigest { get; }
        public string TraversalDigest { get; }
        public string RouteWitnessDigest { get; }
        public TerrainClusterPatternDiffSnapshot Pattern { get; }
        public TerrainClusterDensitySnapshot Density { get; }
        public TerrainClusterSectorFrameSnapshot SectorFrame { get; }
        public IReadOnlyList<CompiledClusterChunkCell> ChunkCells => chunkCells;
        public IReadOnlyList<TerrainClusterPreviewCell> Cells => cells;
        public IReadOnlyList<TerrainClusterPreviewAnchor> Anchors => anchors;
        public IReadOnlyList<TerrainClusterOverlaySegment> Segments => segments;
        public IReadOnlyList<LocalTileCoord> EnvelopeCoordinates => envelopeCoordinates;
        public IReadOnlyList<LocalTileCoord> AbsoluteProtectedCoordinates => absoluteProtectedCoordinates;
        public IReadOnlyList<LocalTileCoord> BaselineCoordinates => baselineCoordinates;
        public IReadOnlyList<LocalTileCoord> HighRouteCoordinates => highRouteCoordinates;
        public IReadOnlyList<LocalTileCoord> RecoveryCoordinates => recoveryCoordinates;
        public IReadOnlyList<string> RouteEvidence => routeEvidence;
        public IReadOnlyList<string> QuietEvidence => quietEvidence;
        public string StableDigest { get; }

        private static ReadOnlyCollection<LocalTileCoord> CopyCoordinates(IEnumerable<LocalTileCoord> source) =>
            new ReadOnlyCollection<LocalTileCoord>((source ?? Array.Empty<LocalTileCoord>())
                .Distinct().OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());

        private static ReadOnlyCollection<string> CopyStrings(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Where(value => value != null).OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public enum TerrainClusterPreviewBuildErrorCode
    {
        MissingRequest = 1,
        ImportFailed = 2,
        ClusterNotFound = 3,
        InvalidVariant = 4,
        CompileFailed = 5,
        DiagnosticPatternUnavailable = 6,
        PatternRenderFailed = 7,
        QuietEvidenceFailed = 8,
        SectorFrameOverflow = 9,
    }

    public sealed class TerrainClusterPreviewBuildError : IComparable<TerrainClusterPreviewBuildError>
    {
        internal TerrainClusterPreviewBuildError(TerrainClusterPreviewBuildErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public TerrainClusterPreviewBuildErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(TerrainClusterPreviewBuildError other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class TerrainClusterPreviewBuildResult
    {
        private readonly ReadOnlyCollection<TerrainClusterPreviewBuildError> errors;

        internal TerrainClusterPreviewBuildResult(
            TerrainClusterPreviewSnapshot snapshot,
            IEnumerable<TerrainClusterPreviewBuildError> sourceErrors)
        {
            var copy = (sourceErrors ?? Array.Empty<TerrainClusterPreviewBuildError>())
                .Where(value => value != null).OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<TerrainClusterPreviewBuildError>(copy);
            Snapshot = copy.Length == 0 ? snapshot : null;
        }

        public bool Success => Snapshot != null && errors.Count == 0;
        public TerrainClusterPreviewSnapshot Snapshot { get; }
        public IReadOnlyList<TerrainClusterPreviewBuildError> Errors => errors;
    }

    public sealed class TerrainClusterPreviewModel
    {
        public const string ApprovedCatalogDigest = "9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7";

        private static readonly IReadOnlyDictionary<string, string[]> DiagnosticPatterns =
            new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "TC_CRATER_QUIET_RIM", new[] { "MP_CRATER_BOWL", "MP_CRATER_ROCK_SHELF" } },
                { "TC_ROOT_HOLLOW_POCKET", new[] { "MP_ROOT_ARCH", "MP_ROOT_HOLLOW_POCKET" } },
                { "TC_MILL_BROKEN_PILLAR", new[] { "MP_MILL_BROKEN_PILLAR", "MP_MILL_ORTHOGONAL_CARVE" } },
                { "TC_DOUGH_STICKY_RISE_RECOVERY", new[] { "MP_DOUGH_BOUNCE_CUP", "MP_DOUGH_STICKY_SHELF" } },
            });

        public TerrainClusterCsvImportResult LoadCatalog() => new TerrainClusterCsvImporterV2().Import();
        public MicroPatternCsvImportResult LoadPatternCatalog() => new MicroPatternCsvImporterV2().Import();

        public TerrainClusterPreviewBuildResult Build(TerrainClusterPreviewRequest request)
        {
            var clusters = LoadCatalog();
            var patterns = LoadPatternCatalog();
            var errors = new List<TerrainClusterPreviewBuildError>();
            if (!clusters.Success)
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.ImportFailed, "terrainClusterCatalog",
                    string.Join(";", clusters.Errors.Select(value => value.ToString()))));
            if (!patterns.Success || !patterns.Published)
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.ImportFailed, "microPatternCatalog",
                    string.Join(";", patterns.Errors.Select(value => value.ToString()))));
            if (errors.Count != 0) return new TerrainClusterPreviewBuildResult(null, errors);
            return Build(request, clusters.Catalog, clusters.StableDigest, patterns.Catalog, patterns.StableDigest);
        }

        public TerrainClusterPreviewBuildResult Build(
            TerrainClusterPreviewRequest request,
            TerrainClusterAuthoringCatalog clusterCatalog,
            string clusterCatalogDigest,
            MicroPatternAuthoringCatalog patternCatalog,
            string patternCatalogDigest)
        {
            var errors = new List<TerrainClusterPreviewBuildError>();
            if (request == null)
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.MissingRequest, "request", "Preview request is required."));
                return new TerrainClusterPreviewBuildResult(null, errors);
            }
            if (clusterCatalog == null || patternCatalog == null)
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.ImportFailed, "catalog", "Both published catalogs are required."));
                return new TerrainClusterPreviewBuildResult(null, errors);
            }
            if (!Enum.IsDefined(typeof(TerrainClusterPreviewMode), request.Mode))
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.MissingRequest, "request.mode", request.Mode.ToString()));
                return new TerrainClusterPreviewBuildResult(null, errors);
            }
            if (!clusterCatalog.TryGet(new TerrainClusterId(request.ClusterId), out var entry))
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.ClusterNotFound, "request.clusterId", request.ClusterId));
                return new TerrainClusterPreviewBuildResult(null, errors);
            }

            var validation = TerrainClusterContractValidator.Validate(entry.Contract);
            if (!validation.IsValid)
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.CompileFailed, "contract",
                    string.Join(";", validation.Errors.Select(value => value.ToString()))));
                return new TerrainClusterPreviewBuildResult(null, errors);
            }
            var footprint = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(entry.Contract, ClusterFootprintTransform.R0));
            if (!footprint.IsSuccess)
                return Failure("footprint", footprint.Errors.Select(value => value.ToString()), errors);
            var canvas = footprint.LocalCanvas;
            var sourceEntry = entry.Contract.Ports.Single(value => value.IsPrimary && value.Kind == ClusterPortKind.Entry);
            var sourceExit = entry.Contract.Ports.Single(value => value.IsPrimary && value.Kind == ClusterPortKind.Exit);
            var socketEvidence = new[]
            {
                new ClusterSectorSocketEvidence("SR_ENTRY_" + entry.Id.Value, "SOCKET_ENTRY_" + entry.Id.Value,
                    sourceEntry.OutwardSide, 2, true, ClusterPortKind.Entry),
                new ClusterSectorSocketEvidence("SR_EXIT_" + entry.Id.Value, "SOCKET_EXIT_" + entry.Id.Value,
                    sourceExit.OutwardSide, 3, true, ClusterPortKind.Exit),
            };
            var role = TerrainClusterRoleSocketCompiler.Compile(new TerrainClusterRoleSocketCompileRequest(
                entry.Contract, validation.CanonicalDigest, canvas, canvas.CanonicalDigest, socketEvidence));
            if (!role.IsSuccess) return Failure("roleSocket", role.Errors.Select(value => value.ToString()), errors);
            var traversal = TerrainClusterTraversalCompiler.Compile(new TerrainClusterTraversalCompileRequest(
                entry.Contract, validation.CanonicalDigest, canvas, canvas.CanonicalDigest,
                role.Contract, role.CanonicalDigest));
            if (!traversal.IsSuccess) return Failure("traversal", traversal.Errors.Select(value => value.ToString()), errors);
            var witness = TerrainClusterRouteWitnessCompiler.Compile(new TerrainClusterRouteWitnessCompileRequest(
                canvas, canvas.CanonicalDigest, role.Contract, role.CanonicalDigest,
                traversal.Compilation, traversal.CanonicalDigest, entry.RouteIntent));
            if (!witness.IsSuccess) return Failure("routeWitness", witness.Errors.Select(value => value.ToString()), errors);

            var requestedVariant = request.VariantId.Length == 0 ? entry.BaselineVariantId : new SpineVariantId(request.VariantId);
            if (!traversal.Compilation.TryGetVariant(requestedVariant, out var selectedVariant))
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.InvalidVariant, "request.variantId", requestedVariant.Value));
                return new TerrainClusterPreviewBuildResult(null, errors);
            }

            TerrainClusterPatternRenderResult render;
            if (request.Mode == TerrainClusterPreviewMode.PatternFree)
            {
                render = Render(canvas, traversal.Compilation, witness.Report, patternCatalog, patternCatalogDigest,
                    Array.Empty<TerrainClusterPatternZoneCell>(), Array.Empty<TerrainClusterPatternPlacementIntent>());
            }
            else
            {
                render = BuildDiagnosticRender(request, canvas, traversal.Compilation, witness.Report,
                    patternCatalog, patternCatalogDigest, errors);
            }
            if (render == null || !render.Success)
            {
                if (render != null)
                    errors.Add(Error(TerrainClusterPreviewBuildErrorCode.PatternRenderFailed, "patternRender",
                        string.Join(";", render.Errors.Select(value => value.ToString()))));
                return new TerrainClusterPreviewBuildResult(null, errors);
            }

            var quietEvidence = BuildQuietEvidence(entry, canvas, role.Contract, traversal.Compilation,
                witness.Report, render.Report, errors);
            if (errors.Count != 0) return new TerrainClusterPreviewBuildResult(null, errors);

            if (!TryBuildFrame(canvas, out var frame, out var frameDetail))
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.SectorFrameOverflow,
                    "sectorFrame", frameDetail));
                return new TerrainClusterPreviewBuildResult(null, errors);
            }
            var pattern = BuildPatternSnapshot(render.Report);
            var density = BuildDensity(render.Report, traversal.Compilation.ProtectedTiles);
            var baseline = witness.Report.BaselineRoute.CompiledCoordinates;
            var highs = witness.Report.HighRoutes.SelectMany(value => value.OrderedEdges)
                .SelectMany(value => new[] { value.CompiledStartCoordinate, value.CompiledEndCoordinate });
            var recoveries = witness.Report.RecoveryRoutes.SelectMany(value => value.CompiledCoordinates);
            var envelope = selectedVariant.Edges.SelectMany(value => value.Envelope.AllTiles)
                .Select(value => value.CompiledCoordinate);
            var cells = BuildCells(canvas, frame, render.Report, selectedVariant,
                traversal.Compilation.ProtectedTiles, baseline, highs, recoveries);
            var anchors = BuildAnchors(role.Contract);
            var segments = BuildSegments(selectedVariant, witness.Report);
            var routeEvidence = BuildRouteEvidence(witness.Report);
            var digest = ComputeSnapshotDigest(entry, requestedVariant, clusterCatalogDigest,
                validation.CanonicalDigest, role.CanonicalDigest, traversal.CanonicalDigest,
                witness.CanonicalDigest, render.CanonicalDigest, frame, cells, routeEvidence, quietEvidence);
            var snapshot = new TerrainClusterPreviewSnapshot(
                entry.Id.Value, entry.Biome, entry.PacingRole, entry.FootprintVariantId,
                requestedVariant.Value, selectedVariant.IsBaseline, entry.StructuralSignature,
                clusterCatalogDigest, validation.CanonicalDigest, canvas.CanonicalDigest,
                role.CanonicalDigest, traversal.CanonicalDigest, witness.CanonicalDigest,
                pattern, density, frame, canvas.ChunkCells, cells, anchors, segments,
                envelope, traversal.Compilation.ProtectedTiles.Select(value => value.CompiledCoordinate),
                baseline, highs, recoveries, routeEvidence, quietEvidence, digest);
            return new TerrainClusterPreviewBuildResult(snapshot, errors);
        }

        public IReadOnlyList<string> DiagnosticPatternIds(string clusterId) =>
            DiagnosticPatterns.TryGetValue(clusterId ?? string.Empty, out var values)
                ? new ReadOnlyCollection<string>(values.ToArray())
                : new ReadOnlyCollection<string>(Array.Empty<string>());

        private static TerrainClusterPatternRenderResult BuildDiagnosticRender(
            TerrainClusterPreviewRequest request,
            TerrainClusterLocalCanvas canvas,
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessReport witness,
            MicroPatternAuthoringCatalog catalog,
            string catalogDigest,
            ICollection<TerrainClusterPreviewBuildError> errors)
        {
            if (!DiagnosticPatterns.TryGetValue(request.ClusterId, out var pair))
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.DiagnosticPatternUnavailable,
                    "request.clusterId", "Pattern A/B is defined only for the exact four representatives."));
                return null;
            }
            var patternId = pair[request.Mode == TerrainClusterPreviewMode.PatternA ? 0 : 1];
            if (!catalog.TryGetDefinition(new MicroPatternId(patternId), out var definition))
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.DiagnosticPatternUnavailable,
                    "patternCatalog/" + patternId, "Definition was not found."));
                return null;
            }
            var protectedSet = new HashSet<LocalTileCoord>(
                traversal.ProtectedTiles.Select(value => value.CompiledCoordinate));
            var rejections = new List<string>();
            foreach (var transform in definition.AllowedTransforms.OrderBy(value => (int)value))
            {
                var transformed = MicroPatternTransformer.Transform(definition, transform);
                if (!transformed.Success)
                {
                    rejections.Add(transform + ":transform");
                    continue;
                }
                for (var y = 0; y <= canvas.TileHeight - MicroPatternDefinition.RequiredHeight; y++)
                for (var x = 0; x <= canvas.TileWidth - MicroPatternDefinition.RequiredWidth; x++)
                {
                    var origin = new LocalTileCoord(x, y);
                    if (!TryBuildZones(canvas, witness.StaticShell, protectedSet,
                            transformed.Pattern, origin, out var zones, out var rejection))
                    {
                        rejections.Add(transform + "@" + x + "," + y + ":" + rejection);
                        continue;
                    }
                    var placement = new TerrainClusterPatternPlacementIntent(
                        request.Mode == TerrainClusterPreviewMode.PatternA ? "TCP_PREVIEW_A" : "TCP_PREVIEW_B",
                        definition.Id, transform, origin, definition.ComputeStableDigest());
                    var result = Render(canvas, traversal, witness, catalog, catalogDigest,
                        zones, new[] { placement });
                    if (result.Success && result.Report.ChangedCoordinateCount > 0) return result;
                    rejections.Add(transform + "@" + x + "," + y + ":" +
                        (result.Success ? "empty-diff" : string.Join(",", result.Errors.Select(value => value.Code))));
                }
            }
            errors.Add(Error(TerrainClusterPreviewBuildErrorCode.DiagnosticPatternUnavailable,
                request.ClusterId + "/" + patternId,
                "No deterministic valid origin. " + string.Join(";", rejections.Take(32))));
            return null;
        }

        private static bool TryBuildZones(
            TerrainClusterLocalCanvas canvas,
            TerrainClusterStaticShell shell,
            ISet<LocalTileCoord> protectedSet,
            TransformedMicroPattern pattern,
            LocalTileCoord origin,
            out TerrainClusterPatternZoneCell[] zones,
            out string rejection)
        {
            var kinds = new Dictionary<LocalTileCoord, TerrainClusterPatternZoneKind>();
            foreach (var cell in pattern.Cells)
            {
                var target = new LocalTileCoord(origin.X + cell.Coordinate.X, origin.Y + cell.Coordinate.Y);
                if (!canvas.TryGetTileCell(target, out var canvasCell) || canvasCell.State != ClusterChunkMaskState.Active ||
                    !shell.TryGetCell(target, out var shellCell))
                {
                    zones = Array.Empty<TerrainClusterPatternZoneCell>();
                    rejection = "outside-active";
                    return false;
                }
                foreach (var instruction in cell.Instructions.Where(value => value.Operation != MicroPatternOperation.NoChange))
                {
                    if (protectedSet.Contains(target))
                    {
                        zones = Array.Empty<TerrainClusterPatternZoneCell>();
                        rejection = "absolute-protected";
                        return false;
                    }
                    TerrainClusterPatternZoneKind kind;
                    if (instruction.Layer == MicroPatternLayer.Geometry && instruction.Operation == MicroPatternOperation.AddSolid)
                    {
                        if (shellCell.Occupancy == TerrainClusterShellOccupancy.Solid)
                        {
                            zones = Array.Empty<TerrainClusterPatternZoneCell>();
                            rejection = "add-on-solid";
                            return false;
                        }
                        kind = TerrainClusterPatternZoneKind.GeometryAdd;
                    }
                    else if (instruction.Layer == MicroPatternLayer.Geometry && instruction.Operation == MicroPatternOperation.CarveAir)
                        kind = TerrainClusterPatternZoneKind.GeometryCarve;
                    else if (instruction.Layer == MicroPatternLayer.Affordance && instruction.Operation == MicroPatternOperation.SetAffordance)
                        kind = TerrainClusterPatternZoneKind.Affordance;
                    else if (instruction.Layer == MicroPatternLayer.Marker && instruction.Operation == MicroPatternOperation.SetMarker)
                        kind = TerrainClusterPatternZoneKind.Marker;
                    else
                    {
                        zones = Array.Empty<TerrainClusterPatternZoneCell>();
                        rejection = "unsupported-operation";
                        return false;
                    }
                    if (kinds.TryGetValue(target, out var existing) && existing != kind)
                    {
                        zones = Array.Empty<TerrainClusterPatternZoneCell>();
                        rejection = "conflicting-zone";
                        return false;
                    }
                    kinds[target] = kind;
                }
            }
            zones = kinds.OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X)
                .Select(value => new TerrainClusterPatternZoneCell(value.Key, value.Value)).ToArray();
            rejection = string.Empty;
            return zones.Length != 0;
        }

        private static TerrainClusterPatternRenderResult Render(
            TerrainClusterLocalCanvas canvas,
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessReport witness,
            MicroPatternAuthoringCatalog catalog,
            string catalogDigest,
            IEnumerable<TerrainClusterPatternZoneCell> zones,
            IEnumerable<TerrainClusterPatternPlacementIntent> placements) =>
            TerrainClusterPatternRenderer.Render(new TerrainClusterPatternRenderRequest(
                canvas, canvas.CanonicalDigest, traversal, traversal.CanonicalDigest,
                witness, witness.CanonicalDigest, catalog, catalogDigest, zones, placements));

        private static IReadOnlyList<string> BuildQuietEvidence(
            TerrainClusterAuthoringEntry entry,
            TerrainClusterLocalCanvas canvas,
            TerrainClusterRoleSocketContract role,
            TerrainClusterTraversalCompilation traversal,
            TerrainClusterRouteWitnessReport witness,
            TerrainClusterPatternRenderReport render,
            ICollection<TerrainClusterPreviewBuildError> errors)
        {
            if (entry.PacingRole != PacingRole.Quiet) return Array.Empty<string>();
            var access = entry.PortAccess.Values.Distinct().OrderBy(value => value).ToArray();
            var profile = new TerrainClusterQuietBufferProfile(
                "QBUF_PREVIEW_" + entry.Id.Value.Substring(3), entry.Biome,
                new[] { TerrainClusterQuietBufferUse.BeforeLandmark, TerrainClusterQuietBufferUse.AfterLandmark,
                    TerrainClusterQuietBufferUse.UnplacedSpace },
                new[] { entry.PacingRole }, access,
                canvas, canvas.CanonicalDigest, role, role.CanonicalDigest,
                traversal, traversal.CanonicalDigest, witness, witness.CanonicalDigest,
                render, render.CanonicalDigest);
            var compiled = TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(new[] { profile }));
            if (!compiled.IsSuccess)
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.QuietEvidenceFailed, "quietPool",
                    string.Join(";", compiled.Errors.Select(value => value.ToString()))));
                return Array.Empty<string>();
            }
            var entryPort = role.Ports.Single(value => value.Kind == ClusterPortKind.Entry);
            var exitPort = role.Ports.Single(value => value.Kind == ClusterPortKind.Exit);
            var routeType = entryPort.CompatibleRouteTypes.Intersect(exitPort.CompatibleRouteTypes).OrderBy(value => value).First();
            var query = compiled.Pool.Query(new TerrainClusterQuietBufferQuery(
                entry.Biome, TerrainClusterQuietBufferUse.BeforeLandmark,
                entryPort.CompiledOutwardSide, exitPort.CompiledOutwardSide,
                routeType, entry.PacingRole, access.First(), 2, compiled.CanonicalDigest));
            if (!query.IsSuccess || query.QueryResult.MatchCount != 1)
            {
                errors.Add(Error(TerrainClusterPreviewBuildErrorCode.QuietEvidenceFailed, "quietQuery",
                    string.Join(";", query.Errors.Select(value => value.ToString()))));
                return Array.Empty<string>();
            }
            return new[]
            {
                "QUIET_POOL|" + compiled.CanonicalDigest,
                "QUIET_QUERY|" + query.CanonicalDigest,
                "QUIET_MATCH|" + query.QueryResult.MatchedCandidateIds.Single(),
                "RNG_DRAWS|" + query.QueryResult.RngDrawCount.ToString(CultureInfo.InvariantCulture),
            };
        }

        private static bool TryBuildFrame(
            TerrainClusterLocalCanvas canvas,
            out TerrainClusterSectorFrameSnapshot frame,
            out string detail)
        {
            var offsetX = (TerrainClusterSectorFrameSnapshot.Width - canvas.TileWidth) / 2;
            var offsetY = (TerrainClusterSectorFrameSnapshot.Height - canvas.TileHeight) / 2;
            var active = canvas.TileCells.Where(value => value.State == ClusterChunkMaskState.Active)
                .Select(value => new LocalTileCoord(value.Coordinate.X + offsetX, value.Coordinate.Y + offsetY))
                .OrderBy(value => value.Y).ThenBy(value => value.X).ToArray();
            var publishedFrame = new TerrainClusterSectorFrameSnapshot(offsetX, offsetY, active);
            frame = publishedFrame;
            if (canvas.TileWidth > TerrainClusterSectorFrameSnapshot.Width ||
                canvas.TileHeight > TerrainClusterSectorFrameSnapshot.Height ||
                active.Any(value => !publishedFrame.Contains(value)))
            {
                detail = "Translation-only projection cannot fit local canvas " +
                         Number(canvas.TileWidth) + "x" + Number(canvas.TileHeight) +
                         " into the required 48x32 sector frame; centered offset=" +
                         Number(offsetX) + "," + Number(offsetY) + ".";
                frame = null;
                return false;
            }
            detail = string.Empty;
            return true;
        }

        private static TerrainClusterPatternDiffSnapshot BuildPatternSnapshot(TerrainClusterPatternRenderReport report)
        {
            var placement = report.Placements.SingleOrDefault();
            var delta = report.RenderDelta;
            return new TerrainClusterPatternDiffSnapshot(
                placement == null ? string.Empty : placement.PatternId.Value,
                placement == null ? MicroPatternTransform.R0 : placement.Transform,
                placement == null ? default : placement.Origin,
                placement == null ? string.Empty : placement.PlacementId,
                report.ApplicationPlanDigest,
                delta == null ? string.Empty : delta.StableDigest,
                report.InitialWorkingCanvas.CanonicalDigest,
                report.FinalWorkingCanvas.CanonicalDigest,
                delta == null ? Array.Empty<LocalTileCoord>() : delta.Cells.Where(value => !value.ValuesEqual)
                    .Select(value => value.TargetCoordinate),
                report.Map10TargetCoordinateCount,
                report.ProtectedWriteCount,
                report.ProtectedValueChangeCount);
        }

        private static TerrainClusterDensitySnapshot BuildDensity(
            TerrainClusterPatternRenderReport report,
            IEnumerable<ClusterTraversalProtectedTile> protectedTiles)
        {
            var cells = report.FinalWorkingCanvas.Cells;
            var chunks = cells.GroupBy(value => value.StaticShellCell.OwningChunk)
                .Select(group => new TerrainClusterChunkDensitySnapshot(group.Key,
                    group.Count(value => value.Solid), group.Count(value => !value.Solid)));
            return new TerrainClusterDensitySnapshot(cells.Count, cells.Count(value => value.Solid),
                protectedTiles.Select(value => value.CompiledCoordinate).Distinct().Count(),
                report.Map10TargetCoordinateCount, report.ChangedCoordinateCount,
                cells.Count(value => !string.IsNullOrEmpty(value.AffordanceId)),
                cells.Count(value => !string.IsNullOrEmpty(value.MarkerId)), chunks);
        }

        private static IReadOnlyList<TerrainClusterPreviewCell> BuildCells(
            TerrainClusterLocalCanvas canvas,
            TerrainClusterSectorFrameSnapshot frame,
            TerrainClusterPatternRenderReport render,
            CompiledClusterSpineVariant variant,
            IEnumerable<ClusterTraversalProtectedTile> protectedTiles,
            IEnumerable<LocalTileCoord> baseline,
            IEnumerable<LocalTileCoord> high,
            IEnumerable<LocalTileCoord> recovery)
        {
            var protectedSet = new HashSet<LocalTileCoord>(protectedTiles.Select(value => value.CompiledCoordinate));
            var spine = new HashSet<LocalTileCoord>(variant.Nodes.Select(value => value.CompiledCoordinate));
            var envelopes = new HashSet<LocalTileCoord>(variant.Edges.SelectMany(value => value.Envelope.AllTiles)
                .Select(value => value.CompiledCoordinate));
            var baseSet = new HashSet<LocalTileCoord>(baseline);
            var highSet = new HashSet<LocalTileCoord>(high);
            var recoverySet = new HashSet<LocalTileCoord>(recovery);
            var diff = render.RenderDelta == null
                ? new Dictionary<LocalTileCoord, MicroPatternRenderedCellDelta>()
                : render.RenderDelta.Cells.Where(value => !value.ValuesEqual).ToDictionary(value => value.TargetCoordinate);
            var output = new List<TerrainClusterPreviewCell>();
            foreach (var source in canvas.TileCells)
            {
                var active = source.State == ClusterChunkMaskState.Active;
                render.FinalWorkingCanvas.TryGetCell(source.Coordinate, out var working);
                var tokens = new List<string>();
                if (active) tokens.Add(working != null && working.Solid ? "S Solid" : "A Air");
                if (spine.Contains(source.Coordinate)) tokens.Add("SP Spine");
                if (envelopes.Contains(source.Coordinate)) tokens.Add("EV Envelope");
                if (protectedSet.Contains(source.Coordinate)) tokens.Add("AP AbsoluteProtected");
                if (baseSet.Contains(source.Coordinate)) tokens.Add("B Base");
                if (highSet.Contains(source.Coordinate)) tokens.Add("H High");
                if (recoverySet.Contains(source.Coordinate)) tokens.Add("R Recovery");
                if (diff.TryGetValue(source.Coordinate, out var changed))
                    tokens.Add(changed.After.Solid ? "P+ Pattern Add" : "P- Pattern Carve");
                output.Add(new TerrainClusterPreviewCell(source.Coordinate, frame.Translate(source.Coordinate),
                    source.OwningChunk, active, active ? (working != null && working.Solid ? "Solid" : "Air") : "Inactive", tokens));
            }
            return output;
        }

        private static IReadOnlyList<TerrainClusterPreviewAnchor> BuildAnchors(TerrainClusterRoleSocketContract role)
        {
            var anchors = role.Roles.Select(value => new TerrainClusterPreviewAnchor(
                value.CompiledCoordinate, value.Role == ClusterRoleKind.Entry ? "EN Entry" :
                    value.Role == ClusterRoleKind.Exit ? "EX Exit" : "ROLE " + value.Role,
                value.AnchorId + "|" + value.TraversalNodeId)).ToList();
            anchors.AddRange(role.Ports.Select(value => new TerrainClusterPreviewAnchor(value.CompiledCoordinate,
                value.Kind == ClusterPortKind.Entry ? "EN Entry" : "EX Exit",
                value.PortId + "|" + value.CompiledOutwardSide + "|" +
                string.Join(",", value.CompatibleRouteTypes.Select(item => item.ToString(CultureInfo.InvariantCulture))))));
            return anchors;
        }

        private static IReadOnlyList<TerrainClusterOverlaySegment> BuildSegments(
            CompiledClusterSpineVariant variant,
            TerrainClusterRouteWitnessReport witness)
        {
            var output = variant.Edges.Select(value => new TerrainClusterOverlaySegment(
                value.CompiledStartCoordinate, value.CompiledEndCoordinate, "SP Spine",
                value.EdgeId + "|" + value.MovementKind)).ToList();
            output.AddRange(witness.BaselineRoute.OrderedEdges.Select(value => new TerrainClusterOverlaySegment(
                value.CompiledStartCoordinate, value.CompiledEndCoordinate, "B Base",
                value.EdgeId + "|" + value.EstimatedDurationMilliseconds.ToString(CultureInfo.InvariantCulture) + "ms")));
            output.AddRange(witness.HighRoutes.SelectMany(route => route.OrderedEdges.Select(value =>
                new TerrainClusterOverlaySegment(value.CompiledStartCoordinate, value.CompiledEndCoordinate, "H High",
                    route.HighRouteId + "|" + value.EdgeId))));
            output.AddRange(witness.RecoveryRoutes.SelectMany(route => route.OrderedEdges.Select(value =>
                new TerrainClusterOverlaySegment(value.CompiledStartCoordinate, value.CompiledEndCoordinate, "R Recovery",
                    route.HighRouteId + "|" + route.FailureNodeId + "|" + value.EdgeId))));
            return output;
        }

        private static IReadOnlyList<string> BuildRouteEvidence(TerrainClusterRouteWitnessReport witness)
        {
            var output = new List<string>
            {
                "BASE|" + witness.BaselineRoute.EntryNodeId + "|" + witness.BaselineRoute.ExitNodeId + "|" +
                witness.BaselineRoute.TotalEstimatedDurationMilliseconds.ToString(CultureInfo.InvariantCulture) + "ms",
            };
            output.AddRange(witness.HighRoutes.Select(value => "HIGH|" + value.HighRouteId + "|" +
                value.BaseDivergenceNodeId + "|" + value.BaseRejoinNodeId + "|" + value.HighPointNodeId + "|" +
                string.Join(",", value.BenefitIds) + "|FAIL=" + string.Join(",", value.FailureNodeIds)));
            output.AddRange(witness.RecoveryRoutes.Select(value => "RECOVERY|" + value.HighRouteId + "|" +
                value.FailureNodeId + "|" + value.TargetBaselineNodeId + "|" +
                value.TotalEstimatedDurationMilliseconds.ToString(CultureInfo.InvariantCulture) + "ms"));
            return output;
        }

        private static string ComputeSnapshotDigest(
            TerrainClusterAuthoringEntry entry,
            SpineVariantId variant,
            string catalogDigest,
            string contractDigest,
            string roleDigest,
            string traversalDigest,
            string witnessDigest,
            string renderDigest,
            TerrainClusterSectorFrameSnapshot frame,
            IEnumerable<TerrainClusterPreviewCell> cells,
            IEnumerable<string> routeEvidence,
            IEnumerable<string> quietEvidence)
        {
            var material = new StringBuilder();
            Append(material, "CLUSTER", entry.Id.Value, entry.Biome.ToString(), entry.PacingRole.ToString(), entry.FootprintVariantId);
            Append(material, "VARIANT", variant.Value, entry.StructuralSignature);
            Append(material, "DIGESTS", catalogDigest, contractDigest, roleDigest, traversalDigest, witnessDigest, renderDigest);
            Append(material, "FRAME", Number(frame.OffsetX), Number(frame.OffsetY), "48", "32");
            foreach (var cell in cells.OrderBy(value => value.LocalCoordinate.Y).ThenBy(value => value.LocalCoordinate.X))
                Append(material, "CELL", Number(cell.LocalCoordinate.X), Number(cell.LocalCoordinate.Y),
                    cell.Active ? "1" : "0", cell.Occupancy, string.Join(",", cell.Tokens));
            foreach (var value in routeEvidence.OrderBy(value => value, StringComparer.Ordinal)) Append(material, "ROUTE", value);
            foreach (var value in quietEvidence.OrderBy(value => value, StringComparer.Ordinal)) Append(material, "QUIET", value);
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(material.ToString()))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static TerrainClusterPreviewBuildResult Failure(
            string path,
            IEnumerable<string> details,
            ICollection<TerrainClusterPreviewBuildError> errors)
        {
            errors.Add(Error(TerrainClusterPreviewBuildErrorCode.CompileFailed, path, string.Join(";", details)));
            return new TerrainClusterPreviewBuildResult(null, errors);
        }

        private static TerrainClusterPreviewBuildError Error(
            TerrainClusterPreviewBuildErrorCode code,
            string path,
            string detail) => new TerrainClusterPreviewBuildError(code, path, detail);

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static void Append(StringBuilder target, params string[] fields)
        {
            foreach (var field in fields)
            {
                var value = field ?? string.Empty;
                target.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
            }
            target.Append('\n');
        }
    }
}
