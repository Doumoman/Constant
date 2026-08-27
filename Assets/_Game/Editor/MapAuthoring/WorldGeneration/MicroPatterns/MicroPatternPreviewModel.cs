using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.MapAuthoring.WorldGeneration.Import;

namespace StarNight.MapAuthoring.WorldGeneration.MicroPatterns
{
    public enum MicroPatternPreviewFixtureKind
    {
        Clean = 1,
        ProtectedOverlap = 2,
        SameLayerConflict = 3,
    }

    public enum MicroPatternPreviewRoleGroup
    {
        Geometry = 1,
        SurfaceAffordance = 2,
        Detail = 3,
    }

    public sealed class MicroPatternPreviewRequest
    {
        public MicroPatternPreviewRequest(
            string patternId,
            MicroPatternTransform transform,
            MicroPatternPreviewFixtureKind fixtureKind)
        {
            PatternId = patternId ?? string.Empty;
            Transform = transform;
            FixtureKind = fixtureKind;
        }

        public string PatternId { get; }
        public MicroPatternTransform Transform { get; }
        public MicroPatternPreviewFixtureKind FixtureKind { get; }
    }

    public sealed class MicroPatternPreviewCell
    {
        private readonly ReadOnlyCollection<string> tokens;
        private readonly ReadOnlyCollection<string> details;

        internal MicroPatternPreviewCell(
            LocalTileCoord coordinate,
            IEnumerable<string> sourceTokens,
            IEnumerable<string> sourceDetails,
            bool isProtected)
        {
            Coordinate = coordinate;
            tokens = new ReadOnlyCollection<string>((sourceTokens ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrEmpty(value))
                .ToArray());
            details = new ReadOnlyCollection<string>((sourceDetails ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrEmpty(value))
                .ToArray());
            IsProtected = isProtected;
        }

        public LocalTileCoord Coordinate { get; }
        public IReadOnlyList<string> Tokens => tokens;
        public IReadOnlyList<string> Details => details;
        public bool IsProtected { get; }
        public string CompactToken => tokens.Count == 0 ? "·" : string.Join(" ", tokens);
    }

    public sealed class MicroPatternPreviewWrite : IComparable<MicroPatternPreviewWrite>
    {
        private readonly ReadOnlyCollection<string> provenance;

        internal MicroPatternPreviewWrite(MicroPatternLayerWrite source)
        {
            TargetCoordinate = source.TargetCoordinate;
            Stage = source.Stage;
            Layer = source.Layer;
            Operation = source.Operation;
            SemanticValue = source.SemanticValue;
            IsIdempotent = source.IsIdempotent;
            provenance = new ReadOnlyCollection<string>(source.Provenance
                .Select(value => value.ToString())
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
        }

        public LocalTileCoord TargetCoordinate { get; }
        public MicroPatternRenderStage Stage { get; }
        public MicroPatternLayer Layer { get; }
        public MicroPatternOperation Operation { get; }
        public string SemanticValue { get; }
        public bool IsIdempotent { get; }
        public IReadOnlyList<string> Provenance => provenance;

        public int CompareTo(MicroPatternPreviewWrite other)
        {
            if (other == null) return -1;
            var comparison = ((int)Stage).CompareTo((int)other.Stage);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.Y.CompareTo(other.TargetCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.X.CompareTo(other.TargetCoordinate.X);
            return comparison != 0 ? comparison : ((int)Layer).CompareTo((int)other.Layer);
        }
    }

    public sealed class MicroPatternPreviewDiff : IComparable<MicroPatternPreviewDiff>
    {
        internal MicroPatternPreviewDiff(
            LocalTileCoord targetCoordinate,
            MicroPatternRenderStage stage,
            MicroPatternLayer layer,
            string beforeValue,
            string afterValue)
        {
            TargetCoordinate = targetCoordinate;
            Stage = stage;
            Layer = layer;
            BeforeValue = beforeValue ?? string.Empty;
            AfterValue = afterValue ?? string.Empty;
        }

        public LocalTileCoord TargetCoordinate { get; }
        public MicroPatternRenderStage Stage { get; }
        public MicroPatternLayer Layer { get; }
        public string BeforeValue { get; }
        public string AfterValue { get; }
        public bool Changed => !string.Equals(BeforeValue, AfterValue, StringComparison.Ordinal);

        public int CompareTo(MicroPatternPreviewDiff other)
        {
            if (other == null) return -1;
            var comparison = ((int)Stage).CompareTo((int)other.Stage);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.Y.CompareTo(other.TargetCoordinate.Y);
            if (comparison != 0) return comparison;
            comparison = TargetCoordinate.X.CompareTo(other.TargetCoordinate.X);
            return comparison != 0 ? comparison : ((int)Layer).CompareTo((int)other.Layer);
        }
    }

    public enum MicroPatternPreviewBuildErrorCode
    {
        MissingRequest = 1,
        ImportFailed = 2,
        MissingCatalog = 3,
        PatternNotFound = 4,
        InvalidFixture = 5,
        TransformFailed = 6,
        PlanFailed = 7,
        RenderFailed = 8,
        SignatureFailed = 9,
        FixtureExpectationFailed = 10,
    }

    public sealed class MicroPatternPreviewBuildError :
        IComparable<MicroPatternPreviewBuildError>,
        IEquatable<MicroPatternPreviewBuildError>
    {
        public MicroPatternPreviewBuildError(
            MicroPatternPreviewBuildErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternPreviewBuildErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternPreviewBuildError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternPreviewBuildError other) =>
            other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as MicroPatternPreviewBuildError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class MicroPatternPreviewSnapshot
    {
        private readonly ReadOnlyCollection<MicroPatternTransform> allowedTransforms;
        private readonly ReadOnlyCollection<string> protectedProvenance;
        private readonly ReadOnlyCollection<MicroPatternPreviewCell> originalCells;
        private readonly ReadOnlyCollection<MicroPatternPreviewCell> transformedCells;
        private readonly ReadOnlyCollection<MicroPatternPreviewCell> protectedEffectiveCells;
        private readonly ReadOnlyCollection<MicroPatternPreviewCell> beforeCells;
        private readonly ReadOnlyCollection<MicroPatternPreviewCell> afterCells;
        private readonly ReadOnlyCollection<MicroPatternPreviewWrite> writes;
        private readonly ReadOnlyCollection<MicroPatternPreviewDiff> diffs;
        private readonly ReadOnlyCollection<string> pipelineErrors;
        private readonly ReadOnlyCollection<string> conflictEvidence;

        internal MicroPatternPreviewSnapshot(PreviewSnapshotData data, string stableDigest)
        {
            PatternId = data.PatternId ?? string.Empty;
            BiomeId = data.BiomeId ?? string.Empty;
            RoleGroup = data.RoleGroup;
            Weight = data.Weight;
            ProtectedPolicy = data.ProtectedPolicy;
            allowedTransforms = Copy(data.AllowedTransforms, value => (int)value);
            SelectedTransform = data.SelectedTransform;
            FixtureKind = data.FixtureKind;
            CatalogDigest = data.CatalogDigest ?? string.Empty;
            DefinitionDigest = data.DefinitionDigest ?? string.Empty;
            TransformDigest = data.TransformDigest ?? string.Empty;
            PlanPublished = data.PlanPublished;
            PlanDigest = data.PlanDigest ?? string.Empty;
            RendererInvoked = data.RendererInvoked;
            RenderPublished = data.RenderPublished;
            RenderDigest = data.RenderDigest ?? string.Empty;
            SilhouetteAddSolidMask = data.SilhouetteAddSolidMask;
            SilhouetteCarveAirMask = data.SilhouetteCarveAirMask;
            SilhouetteDigest = data.SilhouetteDigest ?? string.Empty;
            ProtectedHitCount = data.ProtectedHitCount;
            protectedProvenance = CopyStrings(data.ProtectedProvenance);
            originalCells = CopyCells(data.OriginalCells);
            transformedCells = CopyCells(data.TransformedCells);
            protectedEffectiveCells = CopyCells(data.ProtectedEffectiveCells);
            beforeCells = CopyCells(data.BeforeCells);
            afterCells = CopyCells(data.AfterCells);
            writes = new ReadOnlyCollection<MicroPatternPreviewWrite>(
                (data.Writes ?? Array.Empty<MicroPatternPreviewWrite>()).OrderBy(value => value).ToArray());
            diffs = new ReadOnlyCollection<MicroPatternPreviewDiff>(
                (data.Diffs ?? Array.Empty<MicroPatternPreviewDiff>()).OrderBy(value => value).ToArray());
            pipelineErrors = CopyStrings(data.PipelineErrors);
            conflictEvidence = CopyStrings(data.ConflictEvidence);
            StableDigest = stableDigest ?? string.Empty;
        }

        public string PatternId { get; }
        public string BiomeId { get; }
        public MicroPatternPreviewRoleGroup RoleGroup { get; }
        public int Weight { get; }
        public MicroPatternProtectedPolicy ProtectedPolicy { get; }
        public IReadOnlyList<MicroPatternTransform> AllowedTransforms => allowedTransforms;
        public MicroPatternTransform SelectedTransform { get; }
        public MicroPatternPreviewFixtureKind FixtureKind { get; }
        public string CatalogDigest { get; }
        public string DefinitionDigest { get; }
        public string TransformDigest { get; }
        public bool PlanPublished { get; }
        public string PlanDigest { get; }
        public bool RendererInvoked { get; }
        public bool RenderPublished { get; }
        public string RenderDigest { get; }
        public ushort SilhouetteAddSolidMask { get; }
        public ushort SilhouetteCarveAirMask { get; }
        public string SilhouetteDigest { get; }
        public int ProtectedHitCount { get; }
        public IReadOnlyList<string> ProtectedProvenance => protectedProvenance;
        public IReadOnlyList<MicroPatternPreviewCell> OriginalCells => originalCells;
        public IReadOnlyList<MicroPatternPreviewCell> TransformedCells => transformedCells;
        public IReadOnlyList<MicroPatternPreviewCell> ProtectedEffectiveCells => protectedEffectiveCells;
        public IReadOnlyList<MicroPatternPreviewCell> BeforeCells => beforeCells;
        public IReadOnlyList<MicroPatternPreviewCell> AfterCells => afterCells;
        public IReadOnlyList<MicroPatternPreviewWrite> Writes => writes;
        public IReadOnlyList<MicroPatternPreviewDiff> Diffs => diffs;
        public IReadOnlyList<string> PipelineErrors => pipelineErrors;
        public IReadOnlyList<string> ConflictEvidence => conflictEvidence;
        public string StableDigest { get; }
        public int PanelCount => 5;

        internal PreviewSnapshotData ToData()
        {
            return new PreviewSnapshotData
            {
                PatternId = PatternId,
                BiomeId = BiomeId,
                RoleGroup = RoleGroup,
                Weight = Weight,
                ProtectedPolicy = ProtectedPolicy,
                AllowedTransforms = allowedTransforms,
                SelectedTransform = SelectedTransform,
                FixtureKind = FixtureKind,
                CatalogDigest = CatalogDigest,
                DefinitionDigest = DefinitionDigest,
                TransformDigest = TransformDigest,
                PlanPublished = PlanPublished,
                PlanDigest = PlanDigest,
                RendererInvoked = RendererInvoked,
                RenderPublished = RenderPublished,
                RenderDigest = RenderDigest,
                SilhouetteAddSolidMask = SilhouetteAddSolidMask,
                SilhouetteCarveAirMask = SilhouetteCarveAirMask,
                SilhouetteDigest = SilhouetteDigest,
                ProtectedHitCount = ProtectedHitCount,
                ProtectedProvenance = protectedProvenance,
                OriginalCells = originalCells,
                TransformedCells = transformedCells,
                ProtectedEffectiveCells = protectedEffectiveCells,
                BeforeCells = beforeCells,
                AfterCells = afterCells,
                Writes = writes,
                Diffs = diffs,
                PipelineErrors = pipelineErrors,
                ConflictEvidence = conflictEvidence,
            };
        }

        private static ReadOnlyCollection<T> Copy<T, TKey>(IEnumerable<T> source, Func<T, TKey> key) =>
            new ReadOnlyCollection<T>((source ?? Array.Empty<T>()).OrderBy(key).ToArray());

        private static ReadOnlyCollection<string> CopyStrings(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Where(value => value != null)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());

        private static ReadOnlyCollection<MicroPatternPreviewCell> CopyCells(
            IEnumerable<MicroPatternPreviewCell> source) =>
            new ReadOnlyCollection<MicroPatternPreviewCell>((source ?? Array.Empty<MicroPatternPreviewCell>())
                .OrderBy(value => value.Coordinate.Y)
                .ThenBy(value => value.Coordinate.X)
                .ToArray());
    }

    public sealed class MicroPatternPreviewBuildResult
    {
        private readonly ReadOnlyCollection<MicroPatternPreviewBuildError> errors;

        internal MicroPatternPreviewBuildResult(
            MicroPatternPreviewSnapshot snapshot,
            IEnumerable<MicroPatternPreviewBuildError> sourceErrors)
        {
            var copy = (sourceErrors ?? Array.Empty<MicroPatternPreviewBuildError>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            errors = new ReadOnlyCollection<MicroPatternPreviewBuildError>(copy);
            Snapshot = copy.Length == 0 ? snapshot : null;
        }

        public bool Success => Snapshot != null && errors.Count == 0;
        public MicroPatternPreviewSnapshot Snapshot { get; }
        public IReadOnlyList<MicroPatternPreviewBuildError> Errors => errors;
    }

    internal sealed class PreviewSnapshotData
    {
        public string PatternId;
        public string BiomeId;
        public MicroPatternPreviewRoleGroup RoleGroup;
        public int Weight;
        public MicroPatternProtectedPolicy ProtectedPolicy;
        public IEnumerable<MicroPatternTransform> AllowedTransforms;
        public MicroPatternTransform SelectedTransform;
        public MicroPatternPreviewFixtureKind FixtureKind;
        public string CatalogDigest;
        public string DefinitionDigest;
        public string TransformDigest;
        public bool PlanPublished;
        public string PlanDigest;
        public bool RendererInvoked;
        public bool RenderPublished;
        public string RenderDigest;
        public ushort SilhouetteAddSolidMask;
        public ushort SilhouetteCarveAirMask;
        public string SilhouetteDigest;
        public int ProtectedHitCount;
        public IEnumerable<string> ProtectedProvenance;
        public IEnumerable<MicroPatternPreviewCell> OriginalCells;
        public IEnumerable<MicroPatternPreviewCell> TransformedCells;
        public IEnumerable<MicroPatternPreviewCell> ProtectedEffectiveCells;
        public IEnumerable<MicroPatternPreviewCell> BeforeCells;
        public IEnumerable<MicroPatternPreviewCell> AfterCells;
        public IEnumerable<MicroPatternPreviewWrite> Writes;
        public IEnumerable<MicroPatternPreviewDiff> Diffs;
        public IEnumerable<string> PipelineErrors;
        public IEnumerable<string> ConflictEvidence;
    }

    public sealed class MicroPatternPreviewModel
    {
        public const string ConflictFirstPatternId = "MP_CRATER_DUST_PATCH";
        public const string ConflictSecondPatternId = "MP_ROOT_SAP_PATCH";
        public const string ProtectedSourceId = "PREVIEW_TRAVERSAL_ENVELOPE";

        public MicroPatternCsvImportResult LoadCatalog()
        {
            return new MicroPatternCsvImporterV2().Import();
        }

        public MicroPatternPreviewBuildResult Build(MicroPatternPreviewRequest request)
        {
            var import = LoadCatalog();
            if (!import.Success || !import.Published)
            {
                var errors = import.Errors.Select(value => Error(
                    MicroPatternPreviewBuildErrorCode.ImportFailed,
                    value.FilePath,
                    value.ToString())).ToList();
                if (errors.Count == 0)
                {
                    errors.Add(Error(MicroPatternPreviewBuildErrorCode.ImportFailed,
                        "catalog", "Physical importer did not publish a catalog."));
                }
                return new MicroPatternPreviewBuildResult(null, errors);
            }
            return Build(request, import.Catalog);
        }

        public MicroPatternPreviewBuildResult Build(
            MicroPatternPreviewRequest request,
            MicroPatternAuthoringCatalog catalog)
        {
            var errors = new List<MicroPatternPreviewBuildError>();
            if (request == null)
            {
                errors.Add(Error(MicroPatternPreviewBuildErrorCode.MissingRequest,
                    "request", "Preview request is required."));
                return new MicroPatternPreviewBuildResult(null, errors);
            }
            if (catalog == null)
            {
                errors.Add(Error(MicroPatternPreviewBuildErrorCode.MissingCatalog,
                    "catalog", "Published MicroPattern catalog is required."));
                return new MicroPatternPreviewBuildResult(null, errors);
            }
            if (!IsDefined(request.FixtureKind))
            {
                errors.Add(Error(MicroPatternPreviewBuildErrorCode.InvalidFixture,
                    "request.fixtureKind", ((int)request.FixtureKind).ToString(CultureInfo.InvariantCulture)));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            MicroPatternDefinition definition;
            if (!catalog.TryGetDefinition(new MicroPatternId(request.PatternId), out definition))
            {
                errors.Add(Error(MicroPatternPreviewBuildErrorCode.PatternNotFound,
                    "request.patternId", request.PatternId));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            return request.FixtureKind == MicroPatternPreviewFixtureKind.SameLayerConflict
                ? BuildConflict(catalog, errors)
                : BuildSingle(request, catalog, definition, errors);
        }

        public static MicroPatternPreviewRoleGroup GetRoleGroup(MicroPatternDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var writes = definition.Cells.SelectMany(value => value.Instructions)
                .Where(value => value.Operation != MicroPatternOperation.NoChange)
                .ToArray();
            if (writes.Any(value => value.Layer == MicroPatternLayer.Geometry))
                return MicroPatternPreviewRoleGroup.Geometry;
            if (writes.Any(value => value.Layer == MicroPatternLayer.Surface ||
                                    value.Layer == MicroPatternLayer.Affordance))
                return MicroPatternPreviewRoleGroup.SurfaceAffordance;
            return MicroPatternPreviewRoleGroup.Detail;
        }

        private static MicroPatternPreviewBuildResult BuildSingle(
            MicroPatternPreviewRequest request,
            MicroPatternAuthoringCatalog catalog,
            MicroPatternDefinition definition,
            ICollection<MicroPatternPreviewBuildError> errors)
        {
            var transformed = MicroPatternTransformer.Transform(definition, request.Transform);
            if (!transformed.Success)
            {
                foreach (var error in transformed.Errors)
                    errors.Add(Error(MicroPatternPreviewBuildErrorCode.TransformFailed,
                        error.Path, error.ToString()));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            var placement = new MicroPatternPlacement(new LocalTileCoord(0, 0));
            var protectedCells = request.FixtureKind == MicroPatternPreviewFixtureKind.ProtectedOverlap
                ? BuildProtectedCells(transformed.Pattern)
                : Array.Empty<MicroPatternProtectedCell>();
            var application = MicroPatternApplicationPlanner.Plan(
                transformed.Pattern, placement, protectedCells);

            if (request.FixtureKind == MicroPatternPreviewFixtureKind.ProtectedOverlap &&
                definition.ProtectedPolicy == MicroPatternProtectedPolicy.RejectCandidate)
            {
                if (application.Success || application.RejectedHits.Count == 0 ||
                    !application.Errors.Any(value =>
                        value.Code == MicroPatternApplicationErrorCode.ProtectedWriteRejected))
                {
                    errors.Add(Error(MicroPatternPreviewBuildErrorCode.FixtureExpectationFailed,
                        "fixture.protectedOverlap", "RejectCandidate did not reject the protected write."));
                    return new MicroPatternPreviewBuildResult(null, errors);
                }

                var target = BuildWitnessTargetFromTransformed(transformed.Pattern);
                var data = BaseData(catalog, definition, transformed.Pattern, request.FixtureKind);
                data.PlanPublished = false;
                data.RendererInvoked = false;
                data.RenderPublished = false;
                data.ProtectedHitCount = application.RejectedHits.Count;
                data.ProtectedProvenance = protectedCells.Select(value => value.ToString());
                data.OriginalCells = InstructionCells(definition.Cells, Array.Empty<LocalTileCoord>());
                data.TransformedCells = InstructionCells(transformed.Pattern.Cells,
                    protectedCells.Select(value => value.TargetCoordinate));
                data.ProtectedEffectiveCells = data.TransformedCells;
                data.BeforeCells = StateCells(target.Cells,
                    protectedCells.Select(value => value.TargetCoordinate));
                data.AfterCells = StateCells(target.Cells,
                    protectedCells.Select(value => value.TargetCoordinate));
                data.Writes = Array.Empty<MicroPatternPreviewWrite>();
                data.Diffs = Array.Empty<MicroPatternPreviewDiff>();
                data.PipelineErrors = application.Errors.Select(value => value.ToString());
                data.ConflictEvidence = Array.Empty<string>();
                return Publish(data, errors);
            }

            if (!application.Success)
            {
                foreach (var error in application.Errors)
                    errors.Add(Error(MicroPatternPreviewBuildErrorCode.PlanFailed,
                        error.Path, error.ToString()));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            var renderTarget = BuildWitnessTarget(new[] { application.Plan });
            var render = MicroPatternOrderedRenderer.Render(
                new[]
                {
                    new MicroPatternRenderRequest(
                        new MicroPatternRenderRequestId("MPR_PREVIEW_PRIMARY"),
                        application.Plan),
                },
                renderTarget);
            if (!render.Success)
            {
                foreach (var error in render.Errors)
                    errors.Add(Error(MicroPatternPreviewBuildErrorCode.RenderFailed,
                        error.Path, error.ToString()));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            var signature = MicroPatternSilhouetteSignatureBuilder.Build(application.Plan);
            if (!signature.Success)
            {
                foreach (var error in signature.Errors)
                    errors.Add(Error(MicroPatternPreviewBuildErrorCode.SignatureFailed,
                        error.Path, error.ToString()));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            var protectedCoordinates = application.Plan.ProtectedMask.Entries
                .Select(value => value.TargetCoordinate)
                .ToArray();
            var after = BuildAfterStates(renderTarget, render.Delta);
            var dataSuccess = BaseData(catalog, definition, transformed.Pattern, request.FixtureKind);
            dataSuccess.PlanPublished = true;
            dataSuccess.PlanDigest = application.StableDigest;
            dataSuccess.RendererInvoked = true;
            dataSuccess.RenderPublished = true;
            dataSuccess.RenderDigest = render.StableDigest;
            dataSuccess.SilhouetteAddSolidMask = signature.Signature.AddSolidMask;
            dataSuccess.SilhouetteCarveAirMask = signature.Signature.CarveAirMask;
            dataSuccess.SilhouetteDigest = signature.StableDigest;
            dataSuccess.ProtectedHitCount = application.Plan.ProtectedHits.Count;
            dataSuccess.ProtectedProvenance = protectedCells.Select(value => value.ToString());
            dataSuccess.OriginalCells = InstructionCells(definition.Cells, Array.Empty<LocalTileCoord>());
            dataSuccess.TransformedCells = InstructionCells(transformed.Pattern.Cells, protectedCoordinates);
            dataSuccess.ProtectedEffectiveCells = InstructionCells(
                application.Plan.Cells.Select(value =>
                    new MicroPatternCell(value.LocalCoordinate, value.Instructions)),
                protectedCoordinates);
            dataSuccess.BeforeCells = StateCells(renderTarget.Cells, protectedCoordinates);
            dataSuccess.AfterCells = StateCells(after, protectedCoordinates);
            dataSuccess.Writes = render.Delta.Writes.Select(value => new MicroPatternPreviewWrite(value));
            dataSuccess.Diffs = BuildDiffs(render.Delta);
            dataSuccess.PipelineErrors = Array.Empty<string>();
            dataSuccess.ConflictEvidence = Array.Empty<string>();
            return Publish(dataSuccess, errors);
        }

        private static MicroPatternPreviewBuildResult BuildConflict(
            MicroPatternAuthoringCatalog catalog,
            ICollection<MicroPatternPreviewBuildError> errors)
        {
            MicroPatternDefinition first;
            MicroPatternDefinition second;
            if (!catalog.TryGetDefinition(new MicroPatternId(ConflictFirstPatternId), out first) ||
                !catalog.TryGetDefinition(new MicroPatternId(ConflictSecondPatternId), out second))
            {
                errors.Add(Error(MicroPatternPreviewBuildErrorCode.PatternNotFound,
                    "fixture.sameLayerConflict", ConflictFirstPatternId + "|" + ConflictSecondPatternId));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            var firstTransform = MicroPatternTransformer.Transform(first, MicroPatternTransform.R0);
            var secondTransform = MicroPatternTransformer.Transform(second, MicroPatternTransform.R0);
            if (!firstTransform.Success || !secondTransform.Success)
            {
                errors.Add(Error(MicroPatternPreviewBuildErrorCode.TransformFailed,
                    "fixture.sameLayerConflict", "R0 transform failed."));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            var placement = new MicroPatternPlacement(new LocalTileCoord(0, 0));
            var firstPlan = MicroPatternApplicationPlanner.Plan(
                firstTransform.Pattern, placement, Array.Empty<MicroPatternProtectedCell>());
            var secondPlan = MicroPatternApplicationPlanner.Plan(
                secondTransform.Pattern, placement, Array.Empty<MicroPatternProtectedCell>());
            if (!firstPlan.Success || !secondPlan.Success)
            {
                errors.Add(Error(MicroPatternPreviewBuildErrorCode.PlanFailed,
                    "fixture.sameLayerConflict", "Both fixed starter plans must publish."));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            var target = BuildWitnessTarget(new[] { firstPlan.Plan, secondPlan.Plan });
            var render = MicroPatternOrderedRenderer.Render(
                new[]
                {
                    new MicroPatternRenderRequest(
                        new MicroPatternRenderRequestId("MPR_PREVIEW_ROOT"), secondPlan.Plan),
                    new MicroPatternRenderRequest(
                        new MicroPatternRenderRequestId("MPR_PREVIEW_CRATER"), firstPlan.Plan),
                },
                target);
            if (render.Success || render.Delta != null || render.StableDigest.Length != 0 ||
                render.Conflicts.Count == 0 ||
                !render.Errors.Any(value => value.Code == MicroPatternRenderErrorCode.AtomicRenderRejected))
            {
                errors.Add(Error(MicroPatternPreviewBuildErrorCode.FixtureExpectationFailed,
                    "fixture.sameLayerConflict", "Renderer did not publish the expected atomic Material conflict."));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            var signature = MicroPatternSilhouetteSignatureBuilder.Build(firstPlan.Plan);
            if (!signature.Success)
            {
                errors.Add(Error(MicroPatternPreviewBuildErrorCode.SignatureFailed,
                    "fixture.sameLayerConflict", "Primary signature did not publish."));
                return new MicroPatternPreviewBuildResult(null, errors);
            }

            var data = BaseData(catalog, first, firstTransform.Pattern,
                MicroPatternPreviewFixtureKind.SameLayerConflict);
            data.PlanPublished = true;
            data.PlanDigest = firstPlan.StableDigest;
            data.RendererInvoked = true;
            data.RenderPublished = false;
            data.SilhouetteAddSolidMask = signature.Signature.AddSolidMask;
            data.SilhouetteCarveAirMask = signature.Signature.CarveAirMask;
            data.SilhouetteDigest = signature.StableDigest;
            data.OriginalCells = InstructionCells(first.Cells, Array.Empty<LocalTileCoord>());
            data.TransformedCells = InstructionCells(firstTransform.Pattern.Cells, Array.Empty<LocalTileCoord>());
            data.ProtectedEffectiveCells = InstructionCells(
                firstPlan.Plan.Cells.Select(value =>
                    new MicroPatternCell(value.LocalCoordinate, value.Instructions)),
                Array.Empty<LocalTileCoord>());
            data.BeforeCells = StateCells(target.Cells, Array.Empty<LocalTileCoord>());
            data.AfterCells = StateCells(target.Cells, Array.Empty<LocalTileCoord>());
            data.Writes = Array.Empty<MicroPatternPreviewWrite>();
            data.Diffs = Array.Empty<MicroPatternPreviewDiff>();
            data.PipelineErrors = render.Errors.Select(value => value.ToString());
            data.ConflictEvidence = render.Conflicts.Select(ConflictKey);
            return Publish(data, errors);
        }

        private static PreviewSnapshotData BaseData(
            MicroPatternAuthoringCatalog catalog,
            MicroPatternDefinition definition,
            TransformedMicroPattern transformed,
            MicroPatternPreviewFixtureKind fixture)
        {
            return new PreviewSnapshotData
            {
                PatternId = definition.Id.Value,
                BiomeId = definition.AllowedBiomes.Single().CanonicalId,
                RoleGroup = GetRoleGroup(definition),
                Weight = definition.Weight,
                ProtectedPolicy = definition.ProtectedPolicy,
                AllowedTransforms = definition.AllowedTransforms,
                SelectedTransform = transformed.Transform,
                FixtureKind = fixture,
                CatalogDigest = catalog.StableDigest,
                DefinitionDigest = definition.ComputeStableDigest(),
                TransformDigest = transformed.StableDigest,
                PlanDigest = string.Empty,
                RenderDigest = string.Empty,
                SilhouetteDigest = string.Empty,
                ProtectedProvenance = Array.Empty<string>(),
                PipelineErrors = Array.Empty<string>(),
                ConflictEvidence = Array.Empty<string>(),
            };
        }

        private static MicroPatternPreviewBuildResult Publish(
            PreviewSnapshotData data,
            IEnumerable<MicroPatternPreviewBuildError> errors)
        {
            var provisional = new MicroPatternPreviewSnapshot(data, string.Empty);
            var digest = MicroPatternPreviewCanonicalDigest.Compute(provisional);
            return new MicroPatternPreviewBuildResult(
                new MicroPatternPreviewSnapshot(provisional.ToData(), digest),
                errors);
        }

        private static MicroPatternProtectedCell[] BuildProtectedCells(TransformedMicroPattern pattern)
        {
            var coordinate = pattern.Cells
                .Where(cell => cell.Instructions.Any(value =>
                    value.Operation != MicroPatternOperation.NoChange))
                .OrderBy(cell => cell.Coordinate.Y)
                .ThenBy(cell => cell.Coordinate.X)
                .Select(cell => cell.Coordinate)
                .First();
            return new[]
            {
                new MicroPatternProtectedCell(
                    coordinate,
                    MicroPatternProtectedSourceKind.TraversalEnvelope,
                    ProtectedSourceId),
            };
        }

        private static MicroPatternRenderTarget BuildWitnessTargetFromTransformed(
            TransformedMicroPattern pattern)
        {
            var cells = pattern.Cells.Select(cell => new MicroPatternRenderCellState(
                cell.Coordinate,
                cell.Instructions.Any(value => value.Operation == MicroPatternOperation.CarveAir),
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));
            return new MicroPatternRenderTarget(cells);
        }

        private static MicroPatternRenderTarget BuildWitnessTarget(
            IEnumerable<MicroPatternApplicationPlan> plans)
        {
            var planCopy = plans.OrderBy(value => value.SourcePatternId).ToArray();
            var coordinates = planCopy.SelectMany(value => value.Cells)
                .Select(value => value.TargetCoordinate)
                .Distinct()
                .OrderBy(value => value.Y)
                .ThenBy(value => value.X)
                .ToArray();
            var cells = coordinates.Select(coordinate =>
            {
                var geometry = planCopy.SelectMany(value => value.Cells)
                    .Where(value => value.TargetCoordinate.Equals(coordinate))
                    .SelectMany(value => value.Instructions)
                    .Where(value => value.Layer == MicroPatternLayer.Geometry)
                    .Select(value => value.Operation)
                    .ToArray();
                var beforeSolid = geometry.Contains(MicroPatternOperation.CarveAir);
                return new MicroPatternRenderCellState(
                    coordinate,
                    beforeSolid,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            });
            return new MicroPatternRenderTarget(cells);
        }

        private static MicroPatternRenderCellState[] BuildAfterStates(
            MicroPatternRenderTarget target,
            MicroPatternRenderDelta delta)
        {
            var byCoordinate = target.Cells.ToDictionary(value => value.TargetCoordinate);
            foreach (var cell in delta.Cells) byCoordinate[cell.TargetCoordinate] = cell.After;
            return byCoordinate.Values.OrderBy(value => value.TargetCoordinate.Y)
                .ThenBy(value => value.TargetCoordinate.X)
                .ToArray();
        }

        private static MicroPatternPreviewCell[] InstructionCells(
            IEnumerable<MicroPatternCell> source,
            IEnumerable<LocalTileCoord> protectedCoordinates)
        {
            var protectedSet = new HashSet<LocalTileCoord>(protectedCoordinates);
            return source.OrderBy(value => value.Coordinate.Y)
                .ThenBy(value => value.Coordinate.X)
                .Select(cell =>
                {
                    var writes = cell.Instructions
                        .Where(value => value.Operation != MicroPatternOperation.NoChange)
                        .OrderBy(value => (int)value.Layer)
                        .ToArray();
                    return new MicroPatternPreviewCell(
                        cell.Coordinate,
                        writes.Select(Token),
                        writes.Select(value => value.Layer + "|" + value.Operation + "|" + value.PayloadId),
                        protectedSet.Contains(cell.Coordinate));
                })
                .ToArray();
        }

        private static MicroPatternPreviewCell[] StateCells(
            IEnumerable<MicroPatternRenderCellState> source,
            IEnumerable<LocalTileCoord> protectedCoordinates)
        {
            var protectedSet = new HashSet<LocalTileCoord>(protectedCoordinates);
            return source.OrderBy(value => value.TargetCoordinate.Y)
                .ThenBy(value => value.TargetCoordinate.X)
                .Select(cell =>
                {
                    var tokens = new List<string>();
                    var details = new List<string>
                    {
                        "Geometry|" + (cell.Solid ? "SOLID" : "AIR"),
                    };
                    if (cell.Solid) tokens.Add("G+");
                    AddStateToken(tokens, details, "S", "Surface", cell.SurfaceId);
                    AddStateToken(tokens, details, "A", "Affordance", cell.AffordanceId);
                    AddStateToken(tokens, details, "M", "Material", cell.MaterialId);
                    AddStateToken(tokens, details, "H", "Hazard", cell.HazardId);
                    AddStateToken(tokens, details, "K", "Marker", cell.MarkerId);
                    return new MicroPatternPreviewCell(
                        cell.TargetCoordinate,
                        tokens,
                        details,
                        protectedSet.Contains(cell.TargetCoordinate));
                })
                .ToArray();
        }

        private static void AddStateToken(
            ICollection<string> tokens,
            ICollection<string> details,
            string token,
            string layer,
            string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            tokens.Add(token);
            details.Add(layer + "|" + value);
        }

        private static MicroPatternPreviewDiff[] BuildDiffs(MicroPatternRenderDelta delta)
        {
            return delta.Cells.SelectMany(cell => cell.Writes.Select(write =>
                    new MicroPatternPreviewDiff(
                        cell.TargetCoordinate,
                        write.Stage,
                        write.Layer,
                        cell.Before.GetSemanticValue(write.Layer),
                        cell.After.GetSemanticValue(write.Layer))))
                .OrderBy(value => value)
                .ToArray();
        }

        private static string ConflictKey(MicroPatternRenderConflict conflict)
        {
            return Coordinate(conflict.TargetCoordinate) + "|" + conflict.Layer + "|" +
                   string.Join(",", conflict.Alternatives.Select(value => value.SemanticValue));
        }

        private static string Token(MicroPatternInstruction instruction)
        {
            switch (instruction.Operation)
            {
                case MicroPatternOperation.AddSolid: return "G+";
                case MicroPatternOperation.CarveAir: return "G-";
                case MicroPatternOperation.SetSurface: return "S";
                case MicroPatternOperation.SetAffordance: return "A";
                case MicroPatternOperation.SetMaterial: return "M";
                case MicroPatternOperation.SetHazard: return "H";
                case MicroPatternOperation.SetMarker: return "K";
                default: return "·";
            }
        }

        private static bool IsDefined(MicroPatternPreviewFixtureKind fixture) =>
            fixture >= MicroPatternPreviewFixtureKind.Clean &&
            fixture <= MicroPatternPreviewFixtureKind.SameLayerConflict;

        private static MicroPatternPreviewBuildError Error(
            MicroPatternPreviewBuildErrorCode code,
            string path,
            string detail) => new MicroPatternPreviewBuildError(code, path, detail);

        private static string Coordinate(LocalTileCoord value) =>
            value.X.ToString(CultureInfo.InvariantCulture) + "," +
            value.Y.ToString(CultureInfo.InvariantCulture);
    }

    public static class MicroPatternPreviewCanonicalDigest
    {
        public const string Ruleset = "MAP10_07_PREVIEW_V1";

        public static string Compute(MicroPatternPreviewSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var material = new StringBuilder();
            Append(material, "RULESET", Ruleset);
            Append(material, "PATTERN", snapshot.PatternId, snapshot.BiomeId,
                snapshot.RoleGroup.ToString(), Number(snapshot.Weight), snapshot.ProtectedPolicy.ToString());
            Append(material, "REQUEST", snapshot.SelectedTransform.ToString(), snapshot.FixtureKind.ToString());
            Append(material, "DIGESTS", snapshot.CatalogDigest, snapshot.DefinitionDigest,
                snapshot.TransformDigest, snapshot.PlanDigest, snapshot.RenderDigest,
                snapshot.SilhouetteDigest);
            Append(material, "STATUS", snapshot.PlanPublished ? "PLAN" : "NO_PLAN",
                snapshot.RendererInvoked ? "RENDER_INVOKED" : "RENDER_NOT_INVOKED",
                snapshot.RenderPublished ? "RENDER" : "NO_RENDER",
                Number(snapshot.ProtectedHitCount),
                snapshot.SilhouetteAddSolidMask.ToString("X4", CultureInfo.InvariantCulture),
                snapshot.SilhouetteCarveAirMask.ToString("X4", CultureInfo.InvariantCulture));
            foreach (var transform in snapshot.AllowedTransforms)
                Append(material, "ALLOWED_TRANSFORM", transform.ToString());
            foreach (var value in snapshot.ProtectedProvenance)
                Append(material, "PROTECTED", value);
            AppendCells(material, "ORIGINAL", snapshot.OriginalCells);
            AppendCells(material, "TRANSFORMED", snapshot.TransformedCells);
            AppendCells(material, "EFFECTIVE", snapshot.ProtectedEffectiveCells);
            AppendCells(material, "BEFORE", snapshot.BeforeCells);
            AppendCells(material, "AFTER", snapshot.AfterCells);
            foreach (var write in snapshot.Writes)
            {
                Append(material, "WRITE", Number((int)write.Stage), Coordinate(write.TargetCoordinate),
                    write.Layer.ToString(), write.Operation.ToString(), write.SemanticValue,
                    write.IsIdempotent ? "IDEMPOTENT" : "MUTATING");
                foreach (var provenance in write.Provenance)
                    Append(material, "WRITE_SOURCE", provenance);
            }
            foreach (var diff in snapshot.Diffs)
            {
                Append(material, "DIFF", Number((int)diff.Stage), Coordinate(diff.TargetCoordinate),
                    diff.Layer.ToString(), diff.BeforeValue, diff.AfterValue,
                    diff.Changed ? "CHANGED" : "EQUAL");
            }
            foreach (var error in snapshot.PipelineErrors) Append(material, "PIPELINE_ERROR", error);
            foreach (var conflict in snapshot.ConflictEvidence) Append(material, "CONFLICT", conflict);

            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(Encoding.UTF8.GetBytes(material.ToString()))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void AppendCells(
            StringBuilder material,
            string label,
            IEnumerable<MicroPatternPreviewCell> cells)
        {
            foreach (var cell in cells.OrderBy(value => value.Coordinate.Y)
                         .ThenBy(value => value.Coordinate.X))
            {
                Append(material, label, Coordinate(cell.Coordinate),
                    cell.IsProtected ? "PROTECTED" : "OPEN", cell.CompactToken);
                foreach (var detail in cell.Details) Append(material, label + "_DETAIL", detail);
            }
        }

        private static void Append(StringBuilder target, params string[] fields)
        {
            foreach (var field in fields)
            {
                var value = field ?? string.Empty;
                target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
                target.Append(':');
                target.Append(value);
            }
            target.Append('\n');
        }

        private static string Coordinate(LocalTileCoord value) =>
            Number(value.X) + "," + Number(value.Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
