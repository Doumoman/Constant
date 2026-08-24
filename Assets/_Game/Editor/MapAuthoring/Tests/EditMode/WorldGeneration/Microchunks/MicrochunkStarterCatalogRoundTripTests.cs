using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Microchunks;
using StarNight.MapAuthoring.Microchunks;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_13")]
    public sealed class MicrochunkStarterCatalogRoundTripTests
    {
        private Map07AuditEvidence evidence;

        public static IEnumerable<TestCaseData> ContractCases
        {
            get
            {
                for (var caseId = 0; caseId < 620; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MicrochunkStarterCatalogRoundTripContract_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void BuildFullStarterAudit()
        {
            evidence = Map07AuditHarness.GetOrCreate();
        }

        [TestCaseSource(nameof(ContractCases))]
        public void MicrochunkStarterCatalogRoundTripContract(int caseId)
        {
            var starter = evidence.Starters[caseId % evidence.Starters.Count];
            switch (caseId % 20)
            {
                case 0:
                    Assert.That(evidence.Starters, Is.Not.Empty);
                    Assert.That(evidence.Starters.Select(value => value.MicrochunkId), Is.Unique);
                    break;
                case 1:
                    Assert.That(starter.ImportSuccess, Is.True, starter.Diagnostic);
                    Assert.That(starter.TileAndCoverageValidationSuccess, Is.True, starter.Diagnostic);
                    break;
                case 2:
                    Assert.That(starter.TileCellCount, Is.EqualTo(MicrochunkConstants.CellCount));
                    Assert.That(starter.UniqueTileCellCount, Is.EqualTo(MicrochunkConstants.CellCount));
                    break;
                case 3:
                    Assert.That(starter.PreviewTransformCount, Is.EqualTo(starter.ExpectedTransformCount));
                    Assert.That(starter.PreviewCellCount,
                        Is.EqualTo(starter.ExpectedTransformCount * MicrochunkConstants.CellCount));
                    break;
                case 4:
                    Assert.That(starter.AllTransformValidationDeterministic, Is.True, starter.Diagnostic);
                    break;
                case 5:
                    Assert.That(starter.AllMandatoryPairsReachableWithoutTools, Is.True, starter.Diagnostic);
                    Assert.That(starter.ReachablePairCount, Is.EqualTo(starter.EvaluatedPairCount));
                    break;
                case 6:
                    Assert.That(starter.SourceStatePreservedByPreview, Is.True);
                    Assert.That(starter.PreviewSuccess, Is.True, starter.Diagnostic);
                    break;
                case 7:
                    Assert.That(starter.ExportPlanSuccess, Is.True, starter.Diagnostic);
                    Assert.That(starter.ExportApplySuccess, Is.True, starter.Diagnostic);
                    break;
                case 8:
                    Assert.That(starter.RemovedCatalogRows, Is.EqualTo(1));
                    Assert.That(starter.InsertedCatalogRows, Is.EqualTo(1));
                    break;
                case 9:
                    Assert.That(starter.RemovedTileRows, Is.EqualTo(MicrochunkConstants.CellCount));
                    Assert.That(starter.InsertedTileRows, Is.EqualTo(MicrochunkConstants.CellCount));
                    break;
                case 10:
                    Assert.That(starter.SelectedOwnedRowsReplacedExactly, Is.True, starter.Diagnostic);
                    break;
                case 11:
                    Assert.That(starter.SharedSocketBandsPreserved, Is.True, starter.Diagnostic);
                    break;
                case 12:
                    Assert.That(starter.AllExportFilesHaveUtf8Bom, Is.True, starter.Diagnostic);
                    Assert.That(starter.SchemaHeadersPreserved, Is.True, starter.Diagnostic);
                    break;
                case 13:
                    Assert.That(starter.ExportPlanDeterministic, Is.True, starter.Diagnostic);
                    Assert.That(starter.StableRowOrder, Is.True, starter.Diagnostic);
                    break;
                case 14:
                    Assert.That(starter.ReimportSuccess, Is.True, starter.Diagnostic);
                    Assert.That(starter.NormalizedStateRoundTrips, Is.True, starter.Diagnostic);
                    break;
                case 15:
                    Assert.That(starter.CatalogMetadataRoundTrips, Is.True, starter.Diagnostic);
                    Assert.That(starter.VariantMetadataRoundTrips, Is.True, starter.Diagnostic);
                    break;
                case 16:
                    Assert.That(evidence.NegativeCoverageContractsPass, Is.True);
                    break;
                case 17:
                    Assert.That(evidence.ProjectAuthoringSourcePreserved, Is.True);
                    Assert.That(evidence.TempResidueCount, Is.Zero);
                    break;
                case 18:
                    Assert.That(MicrochunkPreviewRequest.SupportedTransforms, Is.EqualTo(new[]
                    {
                        MicrochunkTransform.R0,
                        MicrochunkTransform.MirrorX,
                        MicrochunkTransform.MirrorY,
                        MicrochunkTransform.R180
                    }));
                    break;
                default:
                    Assert.That(evidence.SceneDirtyBefore, Is.EqualTo(evidence.SceneDirtyAfter));
                    Assert.That(typeof(MicrochunkCsvImporter).Assembly.GetName().Name,
                        Is.EqualTo("MapAuthoring.Editor"));
                    break;
            }
        }
    }

    internal static class Map07AuditHarness
    {
        private static readonly object Sync = new object();
        private static Map07AuditEvidence cached;

        public static Map07AuditEvidence GetOrCreate()
        {
            lock (Sync)
            {
                if (cached == null) cached = Build();
                return cached;
            }
        }

        private static Map07AuditEvidence Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var dirtyBefore = scene.IsValid() && scene.isDirty;
            var source = MicrochunkCsvImportSource.FromProjectAuthoringCsv();
            var sourceSignature = SourceSignature(source);
            var ids = CatalogIds(source);
            var starters = new List<Map07StarterEvidence>();
            var tempResidueCount = 0;

            foreach (var id in ids)
            {
                starters.Add(AuditStarter(source, id, ref tempResidueCount));
            }

            var negativeCoverageContractsPass = ValidateNegativeCoverageContracts(
                starters[0].ImportedState,
                starters[0].MicrochunkId);
            var sourceAfter = MicrochunkCsvImportSource.FromProjectAuthoringCsv();
            var dirtyAfter = scene.IsValid() && scene.isDirty;
            return new Map07AuditEvidence(
                starters,
                negativeCoverageContractsPass,
                string.Equals(sourceSignature, SourceSignature(sourceAfter), StringComparison.Ordinal),
                tempResidueCount,
                dirtyBefore,
                dirtyAfter);
        }

        private static Map07StarterEvidence AuditStarter(
            MicrochunkCsvImportSource source,
            string id,
            ref int tempResidueCount)
        {
            var importer = new MicrochunkCsvImporter();
            var imported = importer.Import(source, new MicrochunkCsvImportRequest(id));
            Require(imported.Success, id + " import failed: " + Join(imported.Issues));
            Require(imported.HasValidationFeedback,
                id + " import validation feedback unavailable: " + Join(imported.Issues));
            Require(imported.ValidationFeedback.TileLayerResult.Success &&
                    imported.ValidationFeedback.CoverageResult.Success,
                id + " starter structural validation failed: " + ValidationDiagnostic(imported));

            var beforePreview = StateSignature(imported.EditorState);
            var selectedTransforms = AllowedTransforms(imported.Catalog);
            var previewRequest = new MicrochunkPreviewRequest(
                id,
                imported.EditorState,
                selectedTransforms,
                importIssues: imported.Issues);
            var previewBuilder = new MicrochunkPreviewBuilder();
            var preview = previewBuilder.Build(previewRequest);
            var repeatedPreview = previewBuilder.Build(previewRequest);
            var afterPreview = StateSignature(imported.EditorState);
            var transforms = preview.Transforms;
            var structuralValidationSuccess = transforms.All(value =>
                value.TileLayerResult != null && value.TileLayerResult.Success &&
                value.CoverageResult != null && value.CoverageResult.Success &&
                value.SocketResult != null &&
                value.ObjectSlotResult != null &&
                value.ReachabilityResult != null && value.ReachabilityResult.Success);
            var validationSuccess = structuralValidationSuccess &&
                                    string.Equals(
                                        PreviewValidationSignature(preview),
                                        PreviewValidationSignature(repeatedPreview),
                                        StringComparison.Ordinal);
            var evaluatedPairs = transforms.Sum(value => value.ReachabilityResult.EvaluatedPairCount);
            var reachablePairs = transforms.Sum(value => value.ReachabilityResult.ReachablePairCount);

            var exporter = new MicrochunkCsvExporter();
            var request = MicrochunkCsvExportRequest.FromImportResult(imported);
            var exportSource = ExportableSource(source);
            var plan = exporter.BuildPlan(exportSource, request);
            var repeatedPlan = exporter.BuildPlan(exportSource, request);
            Require(plan.Success, id + " export plan failed: " + Join(plan.Issues));
            Require(repeatedPlan.Success, id + " repeated export plan failed: " + Join(repeatedPlan.Issues));

            var tempRoot = Path.Combine(Path.GetTempPath(), "MAP07_13_" + Guid.NewGuid().ToString("N"));
            MicrochunkCsvImportResult reimported;
            MicrochunkCsvExportResult applied;
            try
            {
                WriteTempAuthoringSource(tempRoot, exportSource);
                applied = exporter.ApplyPlan(plan, tempRoot);
                Require(applied.Success, id + " temp export failed: " + Join(applied.Issues));
                reimported = importer.Import(ReadTempAuthoringSource(tempRoot, exportSource),
                    new MicrochunkCsvImportRequest(id));
                Require(reimported.Success, id + " re-import failed: " + Join(reimported.Issues));
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
                if (Directory.Exists(tempRoot)) tempResidueCount++;
            }

            var catalogPlan = plan.GetFile(MicrochunkCsvImportSource.CatalogFileName);
            var tilePlan = plan.GetFile(MicrochunkCsvImportSource.TileCellsFileName);
            var socketPlan = plan.GetFile(MicrochunkCsvImportSource.SocketsFileName);
            var bandPlan = plan.GetFile(MicrochunkCsvImportSource.SocketBandsFileName);
            var slotPlan = plan.GetFile(MicrochunkCsvImportSource.ObjectSlotsFileName);
            var variantPlan = plan.GetFile(MicrochunkCsvImportSource.VariantsFileName);
            var repeatedFiles = repeatedPlan.Files.ToDictionary(value => value.FileName, StringComparer.Ordinal);
            var deterministic = plan.Files.All(value =>
                repeatedFiles.TryGetValue(value.FileName, out var repeated) &&
                string.Equals(value.AfterSha256, repeated.AfterSha256, StringComparison.Ordinal) &&
                value.FinalRowOrder.SequenceEqual(repeated.FinalRowOrder));
            var ownedRowsExact =
                socketPlan.RemovedRowCount == imported.EditorState.SocketAuthoring.Sockets.Count &&
                socketPlan.InsertedRowCount == imported.EditorState.SocketAuthoring.Sockets.Count &&
                slotPlan.RemovedRowCount == imported.EditorState.ObjectSlotAuthoring.Rows.Count &&
                slotPlan.InsertedRowCount == imported.EditorState.ObjectSlotAuthoring.Rows.Count &&
                variantPlan.RemovedRowCount == imported.Variants.Count &&
                variantPlan.InsertedRowCount == imported.Variants.Count;
            var allBom = plan.Files.All(value => HasUtf8Bom(value.AfterBytes));
            var schemaHeaders = plan.Files.All(value => value.Headers.Count > 0) &&
                                catalogPlan.Headers.Contains("microchunk_id") &&
                                tilePlan.Headers.Contains("local_x") &&
                                tilePlan.Headers.Contains("local_y") &&
                                socketPlan.Headers.Contains("socket_id") &&
                                bandPlan.Headers.Contains("band_id") &&
                                slotPlan.Headers.Contains("slot_id") &&
                                variantPlan.Headers.Contains("microchunk_id");

            return new Map07StarterEvidence(
                id,
                imported.EditorState,
                imported.Success,
                imported.ValidationFeedback.TileLayerResult.Success &&
                imported.ValidationFeedback.CoverageResult.Success,
                imported.GridState.CellCount,
                imported.GridState.Cells.Select(value => value.Coordinate).Distinct().Count(),
                transforms.Count == selectedTransforms.Count && transforms.All(value =>
                    value.Cells.Count == MicrochunkConstants.CellCount),
                selectedTransforms.Count,
                transforms.Count,
                transforms.Sum(value => value.Cells.Count),
                validationSuccess,
                reachablePairs == evaluatedPairs,
                evaluatedPairs,
                reachablePairs,
                string.Equals(beforePreview, afterPreview, StringComparison.Ordinal),
                plan.Success,
                applied.Success,
                catalogPlan.RemovedRowCount,
                catalogPlan.InsertedRowCount,
                tilePlan.RemovedRowCount,
                tilePlan.InsertedRowCount,
                ownedRowsExact,
                !bandPlan.HasChanges && bandPlan.RemovedRowCount == 0 && bandPlan.InsertedRowCount == 0,
                allBom,
                schemaHeaders,
                deterministic,
                plan.Files.All(value => IsStable(value.FinalRowOrder)),
                reimported.Success,
                string.Equals(StateSignature(imported.EditorState), StateSignature(reimported.EditorState),
                    StringComparison.Ordinal),
                MetadataSignature(imported.Catalog.Fields) == MetadataSignature(reimported.Catalog.Fields),
                VariantSignature(imported) == VariantSignature(reimported),
                Join(imported.Issues.Cast<object>().Concat(preview.Issues.Cast<object>())));
        }

        private static bool ValidateNegativeCoverageContracts(
            MicrochunkSocketAndSlotEditorViewModel state,
            string id)
        {
            var microchunkId = new MicrochunkId(id);
            var cells = state.Grid.ProjectTileCells().ToList();
            var records = cells.Select((cell, index) => new Microchunk96CellRecord(
                microchunkId, index, cell.Coordinate.X, cell.Coordinate.Y, cell)).ToList();
            var validator = new Microchunk96CellValidator();

            var missing = validator.ValidateRecords(
                microchunkId, records.Take(records.Count - 1), Microchunk96CellValidationPolicy.Complete);
            var duplicateRecords = new List<Microchunk96CellRecord>(records)
            {
                new Microchunk96CellRecord(microchunkId, records.Count, 0, 0, cells[0])
            };
            var duplicate = validator.ValidateRecords(
                microchunkId, duplicateRecords, Microchunk96CellValidationPolicy.Complete);
            var outOfRangeRecords = records.Take(records.Count - 1).ToList();
            outOfRangeRecords.Add(new Microchunk96CellRecord(
                microchunkId, records.Count - 1, MicrochunkConstants.WidthTiles, 0));
            var outOfRange = validator.ValidateRecords(
                microchunkId, outOfRangeRecords, Microchunk96CellValidationPolicy.Complete);

            return !missing.Success && missing.MissingCount == 1 &&
                   !duplicate.Success && duplicate.DuplicateCount == 1 &&
                   !outOfRange.Success && outOfRange.OutOfRangeCount == 1;
        }

        private static IReadOnlyList<string> CatalogIds(MicrochunkCsvImportSource source)
        {
            var read = new Rfc4180CsvReader().Read(
                source.CatalogBytes,
                MicrochunkCsvImportSource.CatalogFileName);
            Require(read.Success, "Starter catalog RFC4180 parse failed: " + string.Join("\n", read.Errors));
            Require(read.Records.Count > 1, "Starter catalog contains no data rows.");
            var headers = read.Records[0].Fields.Select(value => value.Value).ToList();
            var idIndex = headers.IndexOf("microchunk_id");
            Require(idIndex >= 0, "Starter catalog has no microchunk_id header.");
            var ids = read.Records.Skip(1)
                .Select(record => record.Fields[idIndex].Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
            Require(ids.Count > 0 && ids.Distinct(StringComparer.Ordinal).Count() == ids.Count,
                "Starter catalog IDs must be non-empty and unique.");
            return new ReadOnlyCollection<string>(ids);
        }

        private static IReadOnlyList<MicrochunkTransform> AllowedTransforms(
            MicrochunkCsvCatalogMetadata catalog)
        {
            string raw = null;
            foreach (var key in new[] { "allowed_transforms", "allowed_transform_set", "transforms" })
            {
                if (catalog.Fields.TryGetValue(key, out raw) && !string.IsNullOrWhiteSpace(raw)) break;
                raw = null;
            }
            if (raw == null) return MicrochunkPreviewRequest.SupportedTransforms;

            var values = new List<MicrochunkTransform>();
            foreach (var token in raw.Split(new[] { '|', ';', ',', ' ' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                switch (token.Trim().ToUpperInvariant())
                {
                    case "R0": values.Add(MicrochunkTransform.R0); break;
                    case "MIRROR_X": values.Add(MicrochunkTransform.MirrorX); break;
                    case "MIRROR_Y": values.Add(MicrochunkTransform.MirrorY); break;
                    case "R180": values.Add(MicrochunkTransform.R180); break;
                    default: throw new InvalidOperationException(
                        catalog.MicrochunkId + " declares unsupported transform " + token + ".");
                }
            }
            Require(values.Count > 0, catalog.MicrochunkId + " declares no allowed transforms.");
            return new ReadOnlyCollection<MicrochunkTransform>(values.Distinct().OrderBy(value => value).ToList());
        }

        private static void WriteTempAuthoringSource(string root, MicrochunkCsvImportSource source)
        {
            var microchunk = Path.Combine(root, "MicroChunk");
            var route = Path.Combine(root, "Route");
            Directory.CreateDirectory(microchunk);
            Directory.CreateDirectory(route);
            File.WriteAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.CatalogFileName), source.CatalogBytes);
            File.WriteAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.TileCellsFileName), source.TileCellBytes);
            File.WriteAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.SocketsFileName), source.SocketBytes);
            File.WriteAllBytes(Path.Combine(route, MicrochunkCsvImportSource.SocketBandsFileName), source.SocketBandBytes);
            File.WriteAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.ObjectSlotsFileName), source.ObjectSlotBytes);
            File.WriteAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.VariantsFileName), source.VariantBytes);
        }

        private static MicrochunkCsvImportSource ExportableSource(MicrochunkCsvImportSource source)
        {
            return new MicrochunkCsvImportSource(
                source.CatalogBytes,
                source.TileCellBytes,
                NonEmpty(source.SocketBytes,
                    "microchunk_id,socket_id,side,band_id,traversal_kind,direction,mandatory_allowed," +
                    "tool_requirement,edge_signature_id,route_layer,minimum_safe_tiles,notes\r\n"),
                NonEmpty(source.SocketBandBytes,
                    "band_id,axis,min_local_coord,max_local_coord,recommended_center," +
                    "minimum_clearance_tiles,description_ko\r\n"),
                NonEmpty(source.ObjectSlotBytes,
                    "microchunk_id,slot_id,local_x,local_y,slot_category,allowed_pool_id,required," +
                    "orientation,visible_from_route,forbidden_radius_tiles,required_marker_code,notes\r\n"),
                NonEmpty(source.VariantBytes, "microchunk_id,variant_id,transform,notes\r\n"),
                source.TileCodeBytes,
                source.ObjectSlotPoolBytes,
                source.EdgeSignatureBytes);
        }

        private static byte[] NonEmpty(byte[] bytes, string header)
        {
            return bytes.Length == 0 ? Utf8Bom(header) : bytes;
        }

        private static byte[] Utf8Bom(string value)
        {
            var content = new UTF8Encoding(false, true).GetBytes(value);
            var bytes = new byte[content.Length + 3];
            bytes[0] = 0xEF;
            bytes[1] = 0xBB;
            bytes[2] = 0xBF;
            Buffer.BlockCopy(content, 0, bytes, 3, content.Length);
            return bytes;
        }

        private static MicrochunkCsvImportSource ReadTempAuthoringSource(
            string root,
            MicrochunkCsvImportSource original)
        {
            var microchunk = Path.Combine(root, "MicroChunk");
            var route = Path.Combine(root, "Route");
            return new MicrochunkCsvImportSource(
                File.ReadAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.CatalogFileName)),
                File.ReadAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.TileCellsFileName)),
                File.ReadAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.SocketsFileName)),
                File.ReadAllBytes(Path.Combine(route, MicrochunkCsvImportSource.SocketBandsFileName)),
                File.ReadAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.ObjectSlotsFileName)),
                File.ReadAllBytes(Path.Combine(microchunk, MicrochunkCsvImportSource.VariantsFileName)),
                original.TileCodeBytes,
                original.ObjectSlotPoolBytes,
                original.EdgeSignatureBytes);
        }

        private static string StateSignature(MicrochunkSocketAndSlotEditorViewModel state)
        {
            return string.Join("\n", state.Grid.State.Cells.Select(value =>
                       value.Coordinate.RowMajorIndex + ":" + string.Join("|", value.TileCodes))) + "\nS=" +
                   string.Join("\n", state.SocketAuthoring.Sockets.Select(value => string.Join("|",
                       value.SocketId, value.SideToken, value.BandId, value.TraversalKindToken,
                       value.EdgeSignatureId, value.MandatoryAllowed, value.ToolRequirementToken))) + "\nB=" +
                   string.Join("\n", state.SocketAuthoring.Bands.Select(value => string.Join("|",
                       value.BandId, value.SideToken, value.InclusiveStart, value.InclusiveEnd,
                       value.MinimumClearanceTiles))) + "\nO=" +
                   string.Join("\n", state.ObjectSlotAuthoring.Rows.Select(value => string.Join("|",
                       value.SlotId, value.Anchor.X, value.Anchor.Y, value.CategoryToken, value.PoolId,
                       value.OrientationToken, value.Required, value.VisibleFromRoute,
                       value.SafetyRadiusTiles, value.RequiredMarkerCode)));
        }

        private static string SourceSignature(MicrochunkCsvImportSource source)
        {
            return string.Join("|", new[]
            {
                Convert.ToBase64String(source.CatalogBytes), Convert.ToBase64String(source.TileCellBytes),
                Convert.ToBase64String(source.SocketBytes), Convert.ToBase64String(source.SocketBandBytes),
                Convert.ToBase64String(source.ObjectSlotBytes), Convert.ToBase64String(source.VariantBytes),
                Convert.ToBase64String(source.TileCodeBytes), Convert.ToBase64String(source.ObjectSlotPoolBytes),
                Convert.ToBase64String(source.EdgeSignatureBytes)
            });
        }

        private static string MetadataSignature(IReadOnlyDictionary<string, string> fields)
        {
            return string.Join("\n", fields.OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => value.Key + "=" + value.Value));
        }

        private static string VariantSignature(MicrochunkCsvImportResult result)
        {
            return string.Join("\n--\n", result.Variants.Select(value => MetadataSignature(value.Fields)));
        }

        private static string PreviewValidationSignature(MicrochunkPreviewReport report)
        {
            return string.Join("\n", report.Transforms.Select(value =>
                value.Transform + ":" +
                value.TileLayerResult.ViolationCount + ":" +
                value.CoverageResult.IssueCount + ":" +
                string.Join("|", value.SocketResult.Violations.Select(issue =>
                    issue.SocketId + ":" + issue.Reason)) + ":" +
                string.Join("|", value.ObjectSlotResult.Violations.Select(issue =>
                    issue.SlotId + ":" + issue.Reason + ":" + issue.ComparedSlotId)) + ":" +
                value.ReachabilityResult.EvaluatedPairCount + ":" +
                value.ReachabilityResult.ReachablePairCount));
        }

        private static string ValidationDiagnostic(MicrochunkCsvImportResult result)
        {
            if (!result.HasValidationFeedback) return "feedback unavailable; " + Join(result.Issues);
            var value = result.ValidationFeedback;
            return "tile=" + value.TileLayerResult.Success + "/" + value.TileLayerResult.ViolationCount +
                   ", coverage=" + value.CoverageResult.Success + "/" + value.CoverageResult.IssueCount +
                   ", socket=" + value.SocketResult.Success + "/" + value.SocketResult.IssueCount +
                   ", slot=" + value.ObjectSlotResult.Success + "/" + value.ObjectSlotResult.IssueCount +
                   " [" + string.Join(",", value.ObjectSlotResult.Violations.Select(issue =>
                       issue.SlotId + ":" + issue.Reason + ":" + issue.ComparedSlotId)) + "]" +
                   "; " + Join(result.Issues);
        }

        private static bool HasUtf8Bom(byte[] bytes)
        {
            return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        }

        private static bool IsStable(IReadOnlyList<string> values)
        {
            return values.Count == values.Distinct(StringComparer.Ordinal).Count();
        }

        private static string Join(IEnumerable<object> values)
        {
            return string.Join("\n", values.Select(value => value == null ? string.Empty : value.ToString()));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }

    internal sealed class Map07AuditEvidence
    {
        public IReadOnlyList<Map07StarterEvidence> Starters { get; }
        public bool NegativeCoverageContractsPass { get; }
        public bool ProjectAuthoringSourcePreserved { get; }
        public int TempResidueCount { get; }
        public bool SceneDirtyBefore { get; }
        public bool SceneDirtyAfter { get; }

        public Map07AuditEvidence(
            IEnumerable<Map07StarterEvidence> starters,
            bool negativeCoverageContractsPass,
            bool projectAuthoringSourcePreserved,
            int tempResidueCount,
            bool sceneDirtyBefore,
            bool sceneDirtyAfter)
        {
            Starters = new ReadOnlyCollection<Map07StarterEvidence>(starters.ToList());
            NegativeCoverageContractsPass = negativeCoverageContractsPass;
            ProjectAuthoringSourcePreserved = projectAuthoringSourcePreserved;
            TempResidueCount = tempResidueCount;
            SceneDirtyBefore = sceneDirtyBefore;
            SceneDirtyAfter = sceneDirtyAfter;
        }
    }

    internal sealed class Map07StarterEvidence
    {
        public string MicrochunkId { get; }
        public MicrochunkSocketAndSlotEditorViewModel ImportedState { get; }
        public bool ImportSuccess { get; }
        public bool TileAndCoverageValidationSuccess { get; }
        public int TileCellCount { get; }
        public int UniqueTileCellCount { get; }
        public bool PreviewSuccess { get; }
        public int ExpectedTransformCount { get; }
        public int PreviewTransformCount { get; }
        public int PreviewCellCount { get; }
        public bool AllTransformValidationDeterministic { get; }
        public bool AllMandatoryPairsReachableWithoutTools { get; }
        public int EvaluatedPairCount { get; }
        public int ReachablePairCount { get; }
        public bool SourceStatePreservedByPreview { get; }
        public bool ExportPlanSuccess { get; }
        public bool ExportApplySuccess { get; }
        public int RemovedCatalogRows { get; }
        public int InsertedCatalogRows { get; }
        public int RemovedTileRows { get; }
        public int InsertedTileRows { get; }
        public bool SelectedOwnedRowsReplacedExactly { get; }
        public bool SharedSocketBandsPreserved { get; }
        public bool AllExportFilesHaveUtf8Bom { get; }
        public bool SchemaHeadersPreserved { get; }
        public bool ExportPlanDeterministic { get; }
        public bool StableRowOrder { get; }
        public bool ReimportSuccess { get; }
        public bool NormalizedStateRoundTrips { get; }
        public bool CatalogMetadataRoundTrips { get; }
        public bool VariantMetadataRoundTrips { get; }
        public string Diagnostic { get; }

        public Map07StarterEvidence(
            string microchunkId,
            MicrochunkSocketAndSlotEditorViewModel importedState,
            bool importSuccess, bool tileAndCoverageValidationSuccess,
            int tileCellCount, int uniqueTileCellCount,
            bool previewSuccess, int expectedTransformCount, int previewTransformCount, int previewCellCount,
            bool allTransformValidationDeterministic, bool allMandatoryPairsReachableWithoutTools,
            int evaluatedPairCount, int reachablePairCount, bool sourceStatePreservedByPreview,
            bool exportPlanSuccess, bool exportApplySuccess, int removedCatalogRows,
            int insertedCatalogRows, int removedTileRows, int insertedTileRows,
            bool selectedOwnedRowsReplacedExactly, bool sharedSocketBandsPreserved,
            bool allExportFilesHaveUtf8Bom, bool schemaHeadersPreserved,
            bool exportPlanDeterministic, bool stableRowOrder, bool reimportSuccess,
            bool normalizedStateRoundTrips, bool catalogMetadataRoundTrips,
            bool variantMetadataRoundTrips, string diagnostic)
        {
            MicrochunkId = microchunkId;
            ImportedState = importedState;
            ImportSuccess = importSuccess;
            TileAndCoverageValidationSuccess = tileAndCoverageValidationSuccess;
            TileCellCount = tileCellCount;
            UniqueTileCellCount = uniqueTileCellCount;
            PreviewSuccess = previewSuccess;
            ExpectedTransformCount = expectedTransformCount;
            PreviewTransformCount = previewTransformCount;
            PreviewCellCount = previewCellCount;
            AllTransformValidationDeterministic = allTransformValidationDeterministic;
            AllMandatoryPairsReachableWithoutTools = allMandatoryPairsReachableWithoutTools;
            EvaluatedPairCount = evaluatedPairCount;
            ReachablePairCount = reachablePairCount;
            SourceStatePreservedByPreview = sourceStatePreservedByPreview;
            ExportPlanSuccess = exportPlanSuccess;
            ExportApplySuccess = exportApplySuccess;
            RemovedCatalogRows = removedCatalogRows;
            InsertedCatalogRows = insertedCatalogRows;
            RemovedTileRows = removedTileRows;
            InsertedTileRows = insertedTileRows;
            SelectedOwnedRowsReplacedExactly = selectedOwnedRowsReplacedExactly;
            SharedSocketBandsPreserved = sharedSocketBandsPreserved;
            AllExportFilesHaveUtf8Bom = allExportFilesHaveUtf8Bom;
            SchemaHeadersPreserved = schemaHeadersPreserved;
            ExportPlanDeterministic = exportPlanDeterministic;
            StableRowOrder = stableRowOrder;
            ReimportSuccess = reimportSuccess;
            NormalizedStateRoundTrips = normalizedStateRoundTrips;
            CatalogMetadataRoundTrips = catalogMetadataRoundTrips;
            VariantMetadataRoundTrips = variantMetadataRoundTrips;
            Diagnostic = diagnostic;
        }
    }
}
