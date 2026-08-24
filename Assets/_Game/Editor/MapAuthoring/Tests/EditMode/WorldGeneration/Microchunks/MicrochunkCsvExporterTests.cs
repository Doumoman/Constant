using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Microchunks;
using StarNight.MapAuthoring.Microchunks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_11")]
    public sealed class MicrochunkCsvExporterTests
    {
        private const string SelectedId = "MC_EXPORT_TEST";
        private const string OtherId = "MC_EXPORT_OTHER";

        public static IEnumerable<TestCaseData> ContractCases
        {
            get
            {
                for (var caseId = 0; caseId < 460; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MicrochunkCsvExporterContract_" + caseId.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void MicrochunkCsvExporterContract(int caseId)
        {
            var variant = caseId / 23;
            switch (caseId % 23)
            {
                case 0: AssertSelectedIdRequired(variant); break;
                case 1: AssertMissingCatalogRejected(variant); break;
                case 2: AssertExplicitCatalogCreation(variant); break;
                case 3: AssertDuplicateCatalogRejected(variant); break;
                case 4: AssertCompleteExportHasNinetySixCells(variant); break;
                case 5: AssertAllNoneCellsAreEmitted(variant); break;
                case 6: AssertSelectedRowsOnlyAreReplaced(variant); break;
                case 7: AssertGlobalSocketBandsRemainByteIdentical(variant); break;
                case 8: AssertEveryPlannedFileHasBom(variant); break;
                case 9: AssertRfc4180CommaQuoteAndCrlf(variant); break;
                case 10: AssertRfc4180LfEmptyAndMultiline(variant); break;
                case 11: AssertHeaderOrderIsExact(variant); break;
                case 12: AssertPrimaryKeyOrderingIsStable(variant); break;
                case 13: AssertPlanGenerationIsSideEffectFree(variant); break;
                case 14: AssertPlanReportsBeforeAfterHashes(variant); break;
                case 15: AssertAtomicTempFolderApply(variant); break;
                case 16: AssertSimulatedFailureRestoresEveryOriginal(variant); break;
                case 17: AssertExportedBytesCanBeReimported(variant); break;
                case 18: AssertExistingValidatorFeedbackIsExposed(variant); break;
                case 19: AssertWindowCommandsAreExplicitAndSceneSafe(variant); break;
                case 20: AssertEditorBoundaryAndImporterPreserved(variant); break;
                case 21: AssertFutureProductionSymbolsRemainAbsent(variant); break;
                default: AssertPlanIsDeterministic(variant); break;
            }
        }

        private static void AssertSelectedIdRequired(int variant)
        {
            var state = State(variant);
            Assert.That(() => new MicrochunkCsvExportRequest(null, state, null),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkCsvExportRequest(string.Empty, state, null),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkCsvExportRequest(" " + SelectedId, state, null),
                Throws.TypeOf<ArgumentException>());
            Assert.That(Request(variant).SelectedMicrochunkId, Is.EqualTo(SelectedId));
        }

        private static void AssertMissingCatalogRejected(int variant)
        {
            var plan = Exporter().BuildPlan(Source(catalog: Catalog(false, false)), Request(variant));
            Assert.That(plan.Success, Is.False);
            Assert.That(plan.Issues.Any(value => value.Code == "CATALOG_ROW_MISSING"), Is.True);
            Assert.That(plan.Files, Is.Empty);
        }

        private static void AssertExplicitCatalogCreation(int variant)
        {
            var request = Request(variant, true);
            var plan = Exporter().BuildPlan(Source(catalog: Catalog(false, false)), request);
            Assert.That(plan.Success, Is.True, JoinIssues(plan));
            var rows = Records(plan.GetFile(MicrochunkCsvImportSource.CatalogFileName).AfterBytes);
            Assert.That(rows.Count(record => record[0] == SelectedId), Is.EqualTo(1));
            Assert.That(rows.Single(record => record[0] == SelectedId)[1], Is.EqualTo("1"));
        }

        private static void AssertDuplicateCatalogRejected(int variant)
        {
            var catalog = CatalogHeader +
                          CatalogRow(SelectedId, "old-a") +
                          CatalogRow(SelectedId, "old-b") +
                          CatalogRow(OtherId, "other");
            var plan = Exporter().BuildPlan(Source(catalog: catalog), Request(variant));
            Assert.That(plan.Success, Is.False);
            Assert.That(plan.Issues.Count(value => value.Code == "CATALOG_ROW_DUPLICATE"), Is.EqualTo(1));
        }

        private static void AssertCompleteExportHasNinetySixCells(int variant)
        {
            var plan = Plan(variant);
            var records = Records(plan.GetFile(MicrochunkCsvImportSource.TileCellsFileName).AfterBytes);
            Assert.That(records.Count(record => record[0] == SelectedId), Is.EqualTo(96));
            Assert.That(records.Where(record => record[0] == SelectedId)
                .Select(record => record[1] + ":" + record[2]).Distinct().Count(), Is.EqualTo(96));
        }

        private static void AssertAllNoneCellsAreEmitted(int variant)
        {
            var state = State(variant, paintVariantCell: false);
            var plan = Exporter().BuildPlan(Source(), Request(variant, state: state));
            var selected = Records(plan.GetFile(MicrochunkCsvImportSource.TileCellsFileName).AfterBytes)
                .Where(record => record[0] == SelectedId)
                .ToList();
            Assert.That(selected, Has.Count.EqualTo(96));
            Assert.That(selected.SelectMany(record => record.Skip(3)), Is.All.EqualTo("NONE"));
        }

        private static void AssertSelectedRowsOnlyAreReplaced(int variant)
        {
            var plan = Plan(variant);
            var catalog = Records(plan.GetFile(MicrochunkCsvImportSource.CatalogFileName).AfterBytes);
            Assert.That(catalog.Single(record => record[0] == OtherId)[3], Is.EqualTo("other"));
            Assert.That(catalog.Single(record => record[0] == SelectedId)[3],
                Is.EqualTo("request-" + variant));

            var sockets = Records(plan.GetFile(MicrochunkCsvImportSource.SocketsFileName).AfterBytes);
            Assert.That(sockets.Any(record => record[0] == OtherId && record[1] == "SOCK_OTHER"), Is.True);
            Assert.That(sockets.Any(record => record[0] == SelectedId && record[1] == "SOCK_NEW"), Is.True);
            Assert.That(sockets.Any(record => record[0] == SelectedId && record[1] == "SOCK_OLD"), Is.False);

            var slots = Records(plan.GetFile(MicrochunkCsvImportSource.ObjectSlotsFileName).AfterBytes);
            Assert.That(slots.Any(record => record[0] == OtherId && record[1] == "SLOT_OTHER"), Is.True);
            Assert.That(slots.Any(record => record[0] == SelectedId && record[1] == "SLOT_NEW"), Is.True);
        }

        private static void AssertGlobalSocketBandsRemainByteIdentical(int variant)
        {
            var source = Source();
            var before = source.SocketBandBytes;
            var plan = Exporter().BuildPlan(source, Request(variant));
            var file = plan.GetFile(MicrochunkCsvImportSource.SocketBandsFileName);
            Assert.That(file.AfterBytes, Is.EqualTo(before));
            Assert.That(file.HasChanges, Is.False);
            Assert.That(file.RemovedRowCount, Is.Zero);
            Assert.That(file.InsertedRowCount, Is.Zero);
            Assert.That(plan.Issues.Single(value =>
                value.Code == "SOCKET_BAND_NON_OWNED_GLOBAL_SCHEMA").IsError, Is.False);
        }

        private static void AssertEveryPlannedFileHasBom(int variant)
        {
            var plan = Plan(variant);
            Assert.That(plan.Files, Has.Count.EqualTo(6));
            foreach (var file in plan.Files)
            {
                Assert.That(file.AfterBytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }), file.FileName);
            }
        }

        private static void AssertRfc4180CommaQuoteAndCrlf(int variant)
        {
            var notes = "comma, \"quote\"\r\nline-" + variant;
            var plan = Exporter().BuildPlan(Source(), Request(variant, notes: notes));
            Assert.That(plan.Success, Is.True, JoinIssues(plan));
            var row = Records(plan.GetFile(MicrochunkCsvImportSource.CatalogFileName).AfterBytes)
                .Single(record => record[0] == SelectedId);
            Assert.That(row[3], Is.EqualTo(notes));
            var text = Encoding.UTF8.GetString(
                plan.GetFile(MicrochunkCsvImportSource.CatalogFileName).AfterBytes.Skip(3).ToArray());
            Assert.That(text, Does.Contain("\"comma, \"\"quote\"\"\r\nline-"));
        }

        private static void AssertRfc4180LfEmptyAndMultiline(int variant)
        {
            var notes = "first\n\nlast-" + variant;
            var plan = Exporter().BuildPlan(Source(), Request(variant, notes: notes, displayName: string.Empty));
            var row = Records(plan.GetFile(MicrochunkCsvImportSource.CatalogFileName).AfterBytes)
                .Single(record => record[0] == SelectedId);
            Assert.That(row[2], Is.Empty);
            Assert.That(row[3], Is.EqualTo(notes));
        }

        private static void AssertHeaderOrderIsExact(int variant)
        {
            var source = Source();
            var plan = Exporter().BuildPlan(source, Request(variant));
            Assert.That(Headers(plan.GetFile(MicrochunkCsvImportSource.CatalogFileName).AfterBytes),
                Is.EqualTo(Headers(source.CatalogBytes)));
            Assert.That(Headers(plan.GetFile(MicrochunkCsvImportSource.TileCellsFileName).AfterBytes),
                Is.EqualTo(Headers(source.TileCellBytes)));
            Assert.That(Headers(plan.GetFile(MicrochunkCsvImportSource.SocketsFileName).AfterBytes),
                Is.EqualTo(Headers(source.SocketBytes)));
            Assert.That(Headers(plan.GetFile(MicrochunkCsvImportSource.ObjectSlotsFileName).AfterBytes),
                Is.EqualTo(Headers(source.ObjectSlotBytes)));
            Assert.That(Headers(plan.GetFile(MicrochunkCsvImportSource.VariantsFileName).AfterBytes),
                Is.EqualTo(Headers(source.VariantBytes)));
        }

        private static void AssertPrimaryKeyOrderingIsStable(int variant)
        {
            var plan = Plan(variant);
            var repeated = Exporter().BuildPlan(Source(), Request(variant));
            foreach (var file in plan.Files)
            {
                Assert.That(repeated.GetFile(file.FileName).FinalRowOrder,
                    Is.EqualTo(file.FinalRowOrder), file.FileName);
            }

            var coordinates = Records(plan.GetFile(MicrochunkCsvImportSource.TileCellsFileName).AfterBytes)
                .Where(record => record[0] == SelectedId)
                .Select(record => int.Parse(record[1], CultureInfo.InvariantCulture) * 8 +
                                  int.Parse(record[2], CultureInfo.InvariantCulture))
                .ToArray();
            Assert.That(coordinates, Is.EqualTo(Enumerable.Range(0, 96)));
        }

        private static void AssertPlanGenerationIsSideEffectFree(int variant)
        {
            var source = Source();
            var request = Request(variant);
            var catalogBefore = source.CatalogBytes;
            var tilesBefore = source.TileCellBytes;
            var stateBefore = StateSignature(request.EditorState);
            Exporter().BuildPlan(source, request);
            Assert.That(source.CatalogBytes, Is.EqualTo(catalogBefore));
            Assert.That(source.TileCellBytes, Is.EqualTo(tilesBefore));
            Assert.That(StateSignature(request.EditorState), Is.EqualTo(stateBefore));
        }

        private static void AssertPlanReportsBeforeAfterHashes(int variant)
        {
            var plan = Plan(variant);
            foreach (var file in plan.Files)
            {
                Assert.That(file.BeforeSha256, Has.Length.EqualTo(64));
                Assert.That(file.AfterSha256, Has.Length.EqualTo(64));
                Assert.That(file.BeforeSha256, Does.Match("^[0-9a-f]{64}$"));
                Assert.That(file.AfterSha256, Does.Match("^[0-9a-f]{64}$"));
            }
            Assert.That(plan.TotalRemovedRows, Is.GreaterThan(0));
            Assert.That(plan.TotalInsertedRows, Is.GreaterThanOrEqualTo(100));
        }

        private static void AssertAtomicTempFolderApply(int variant)
        {
            var source = Source();
            var plan = Exporter().BuildPlan(source, Request(variant));
            WithTempRoot(source, root =>
            {
                var result = Exporter().ApplyPlan(plan, root);
                Assert.That(result.Success, Is.True, JoinIssues(result));
                Assert.That(result.WrittenFileCount, Is.EqualTo(plan.ChangedFileCount));
                foreach (var file in plan.Files)
                {
                    Assert.That(File.ReadAllBytes(Target(root, file)), Is.EqualTo(file.AfterBytes));
                }
                Assert.That(Directory.GetFiles(root, "*.map07_11.*", SearchOption.AllDirectories), Is.Empty);
            });
        }

        private static void AssertSimulatedFailureRestoresEveryOriginal(int variant)
        {
            var source = Source();
            var plan = Exporter().BuildPlan(source, Request(variant));
            WithTempRoot(source, root =>
            {
                var before = plan.Files.ToDictionary(
                    file => file.FileName,
                    file => File.ReadAllBytes(Target(root, file)),
                    StringComparer.Ordinal);
                var result = Exporter().ApplyPlan(
                    plan,
                    root,
                    MicrochunkCsvImportSource.TileCellsFileName);
                Assert.That(result.Success, Is.False);
                Assert.That(result.Issues.Any(value => value.Code == "ATOMIC_APPLY_FAILED"), Is.True);
                foreach (var file in plan.Files)
                {
                    Assert.That(File.ReadAllBytes(Target(root, file)), Is.EqualTo(before[file.FileName]));
                }
            });
        }

        private static void AssertExportedBytesCanBeReimported(int variant)
        {
            var plan = Plan(variant);
            var source = new MicrochunkCsvImportSource(
                plan.GetFile(MicrochunkCsvImportSource.CatalogFileName).AfterBytes,
                plan.GetFile(MicrochunkCsvImportSource.TileCellsFileName).AfterBytes,
                plan.GetFile(MicrochunkCsvImportSource.SocketsFileName).AfterBytes,
                plan.GetFile(MicrochunkCsvImportSource.SocketBandsFileName).AfterBytes,
                plan.GetFile(MicrochunkCsvImportSource.ObjectSlotsFileName).AfterBytes,
                plan.GetFile(MicrochunkCsvImportSource.VariantsFileName).AfterBytes,
                Utf8Bom("tile_code,layer\r\nNONE,ANY\r\nGROUND,GroundSolid\r\n"),
                Array.Empty<byte>(),
                Utf8Bom(EdgeHeader + EdgeRow));
            var imported = new MicrochunkCsvImporter().Import(
                source,
                new MicrochunkCsvImportRequest(SelectedId));
            Assert.That(imported.Success, Is.True, string.Join("\n", imported.Issues));
            Assert.That(imported.GridState.CellCount, Is.EqualTo(96));
            Assert.That(imported.EditorState.SocketAuthoring.Sockets.Single().SocketId, Is.EqualTo("SOCK_NEW"));
            Assert.That(imported.EditorState.ObjectSlotAuthoring.Rows.Single().SlotId, Is.EqualTo("SLOT_NEW"));
        }

        private static void AssertExistingValidatorFeedbackIsExposed(int variant)
        {
            var request = Request(variant, state: State(variant, paintVariantCell: false, addRows: false));
            var before = StateSignature(request.EditorState);
            var plan = Exporter().BuildPlan(Source(), request);
            Assert.That(plan.HasValidationFeedback, Is.True);
            Assert.That(plan.ValidationFeedback.TileLayerResult, Is.TypeOf<MicrochunkTileLayerRuleResult>());
            Assert.That(plan.ValidationFeedback.CoverageResult, Is.TypeOf<Microchunk96CellValidationResult>());
            Assert.That(plan.ValidationFeedback.SocketResult, Is.TypeOf<MicrochunkSocketEdgeValidationResult>());
            Assert.That(plan.ValidationFeedback.ObjectSlotResult, Is.TypeOf<MicrochunkObjectSlotValidationResult>());
            Assert.That(StateSignature(request.EditorState), Is.EqualTo(before));
        }

        private static void AssertWindowCommandsAreExplicitAndSceneSafe(int variant)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var dirtyBefore = scene.IsValid() && scene.isDirty;
            var source = Source();
            var imported = new MicrochunkCsvImporter().Import(
                source,
                new MicrochunkCsvImportRequest(SelectedId));
            var window = ScriptableObject.CreateInstance<MicrochunkCsvExportWindow>();
            try
            {
                window.UseImportedState(imported);
                var plan = window.PreflightImportedState(source);
                Assert.That(plan.Success, Is.True, JoinIssues(plan));
                Assert.That(window.LastResult, Is.Null);
                Assert.That(window.SelectedMicrochunkId, Is.EqualTo(SelectedId));
                var methods = typeof(MicrochunkCsvExportWindow).GetMethods(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.DeclaredOnly)
                    .Select(value => value.Name)
                    .ToArray();
                Assert.That(methods, Does.Contain("PreflightImportedState"));
                Assert.That(methods, Does.Contain("Execute"));
                Assert.That(methods, Has.None.Contains("GenerateReport")
                    .And.None.Contains("CreateAsset").And.None.Contains("PreviewScreenshot"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
            var dirtyAfter = scene.IsValid() && scene.isDirty;
            Assert.That(dirtyAfter, Is.EqualTo(dirtyBefore));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertEditorBoundaryAndImporterPreserved(int variant)
        {
            Assert.That(typeof(MicrochunkCsvExporter).Assembly.GetName().Name,
                Is.EqualTo("MapAuthoring.Editor"));
            Assert.That(typeof(MicrochunkCsvExporter).Assembly,
                Is.SameAs(typeof(MicrochunkCsvImporter).Assembly));
            Assert.That(typeof(MicrochunkCsvExporter).Assembly,
                Is.Not.SameAs(typeof(
                    StarNight.Map.WorldGeneration.Microchunks.MicrochunkDefinition).Assembly));
            Assert.That(typeof(EditorWindow).IsAssignableFrom(typeof(MicrochunkCsvExportWindow)), Is.True);
            Assert.That(variant, Is.GreaterThanOrEqualTo(0));
        }

        private static void AssertFutureProductionSymbolsRemainAbsent(int variant)
        {
            var names = typeof(MicrochunkCsvExporter).Assembly.GetTypes().Select(value => value.Name).ToArray();
            foreach (var forbidden in new[]
                     {
                         "MicrochunkPreviewReport", "MicrochunkReachabilityHeatmap",
                         "MicrochunkStarterCatalogRoundTrip", "BoundaryChunkResolver",
                         "SectorRecipeResolver", "GeneratedSectorMicrochunkWriter",
                         "PopulationSlotIndex", "StableSpawnId", "WorldTraversalValidator"
                     })
            {
                Assert.That(names, Does.Not.Contain(forbidden));
            }
            Assert.That(variant, Is.LessThan(20));
        }

        private static void AssertPlanIsDeterministic(int variant)
        {
            var source = Source();
            var request = Request(variant);
            var first = Exporter().BuildPlan(source, request);
            var second = Exporter().BuildPlan(source, request);
            Assert.That(second.Files.Select(value => value.FileName),
                Is.EqualTo(first.Files.Select(value => value.FileName)));
            foreach (var file in first.Files)
            {
                var repeated = second.GetFile(file.FileName);
                Assert.That(repeated.AfterBytes, Is.EqualTo(file.AfterBytes));
                Assert.That(repeated.FinalRowOrder, Is.EqualTo(file.FinalRowOrder));
                Assert.That(repeated.AfterSha256, Is.EqualTo(file.AfterSha256));
            }
            Assert.That(second.Issues.Select(value => value.ToString()),
                Is.EqualTo(first.Issues.Select(value => value.ToString())));
        }

        private static MicrochunkCsvExporter Exporter()
        {
            return new MicrochunkCsvExporter();
        }

        private static MicrochunkCsvExportPlan Plan(int variant)
        {
            var plan = Exporter().BuildPlan(Source(), Request(variant));
            Assert.That(plan.Success, Is.True, JoinIssues(plan));
            return plan;
        }

        private static MicrochunkCsvExportRequest Request(
            int variant,
            bool allowNew = false,
            MicrochunkSocketAndSlotEditorViewModel state = null,
            string notes = null,
            string displayName = null)
        {
            var fields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["microchunk_id"] = SelectedId,
                ["tile_data_complete"] = "1",
                ["display_name"] = displayName ?? "Export " + variant,
                ["notes"] = notes ?? "request-" + variant
            };
            var catalog = new MicrochunkCsvCatalogMetadata(SelectedId, 2, true, fields);
            var variants = new[]
            {
                new MicrochunkCsvVariantMetadata(
                    SelectedId,
                    2,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["microchunk_id"] = SelectedId,
                        ["variant_id"] = "VAR_" + variant.ToString("D2"),
                        ["transform"] = "R0",
                        ["notes"] = "variant-" + variant
                    })
            };
            return new MicrochunkCsvExportRequest(
                SelectedId,
                state ?? State(variant),
                catalog,
                variants,
                allowNew);
        }

        private static MicrochunkSocketAndSlotEditorViewModel State(
            int variant,
            bool paintVariantCell = true,
            bool addRows = true)
        {
            var state = new MicrochunkSocketAndSlotEditorViewModel();
            if (paintVariantCell)
            {
                var index = variant % 96;
                state.Grid.State.PaintCell(
                    index % 12,
                    index / 12,
                    MicrochunkTileLayer.GroundSolid,
                    "GROUND");
                state.Grid.State.PaintCell(
                    index % 12,
                    index / 12,
                    MicrochunkTileLayer.Marker,
                    "MARKER_" + variant);
            }
            if (addRows)
            {
                state.SocketAuthoring.AddBand(new MicrochunkSocketBandAuthoringRow(
                    "BAND_H_MID", "L", 3, 4, 2));
                state.SocketAuthoring.AddSocket(new MicrochunkSocketAuthoringRow(
                    "SOCK_NEW", "L", "BAND_H_MID", "WALK", "EDGE_H_MID_WALK", true));
                state.ObjectSlotAuthoring.Add(new MicrochunkObjectSlotAuthoringRow(
                    "SLOT_NEW", 6, 1, "RESOURCE", "POOL_A"));
            }
            return state;
        }

        private static MicrochunkCsvImportSource Source(
            string catalog = null,
            string tiles = null,
            string sockets = null,
            string bands = null,
            string slots = null,
            string variants = null)
        {
            return new MicrochunkCsvImportSource(
                Utf8Bom(catalog ?? Catalog(true, true)),
                Utf8Bom(tiles ?? Tiles()),
                Utf8Bom(sockets ?? Sockets()),
                Utf8Bom(bands ?? Bands()),
                Utf8Bom(slots ?? Slots()),
                Utf8Bom(variants ?? Variants()),
                Utf8Bom("tile_code,layer\r\nNONE,ANY\r\nGROUND,GroundSolid\r\n"),
                Array.Empty<byte>(),
                Utf8Bom(EdgeHeader + EdgeRow));
        }

        private static string Catalog(bool includeSelected, bool includeOther)
        {
            return CatalogHeader +
                   (includeSelected ? CatalogRow(SelectedId, "old") : string.Empty) +
                   (includeOther ? CatalogRow(OtherId, "other") : string.Empty);
        }

        private static string CatalogRow(string id, string notes)
        {
            return id + ",1," + id + "," + notes + "\r\n";
        }

        private static string Tiles()
        {
            var rows = new StringBuilder(TileHeader);
            for (var index = 95; index >= 0; index--)
            {
                rows.Append(TileRow(SelectedId, index % 12, index / 12));
            }
            rows.Append(TileRow(OtherId, 0, 0));
            return rows.ToString();
        }

        private static string TileRow(string id, int x, int y)
        {
            return id + "," + x + "," + y + ",NONE,NONE,NONE,NONE,NONE,NONE,NONE,NONE\r\n";
        }

        private static string Sockets()
        {
            return SocketHeader +
                   SelectedId + ",SOCK_OLD,L,BAND_H_MID,WALK,BIDIRECTIONAL,1,NONE,EDGE_H_MID_WALK,BOTH,2,old\r\n" +
                   OtherId + ",SOCK_OTHER,R,BAND_H_MID,WALK,BIDIRECTIONAL,0,NONE,EDGE_H_MID_WALK,OPTIONAL,2,other\r\n";
        }

        private static string Bands()
        {
            return BandHeader + "BAND_H_MID,HORIZONTAL_EDGE,3,4,3.5,2,shared\r\n";
        }

        private static string Slots()
        {
            return SlotHeader +
                   SelectedId + ",SLOT_OLD,1,1,RESOURCE,POOL_A,0,NONE,1,0,NONE,old\r\n" +
                   OtherId + ",SLOT_OTHER,2,2,RESOURCE,POOL_A,0,NONE,1,0,NONE,other\r\n";
        }

        private static string Variants()
        {
            return VariantHeader +
                   SelectedId + ",VAR_OLD,R0,old\r\n" +
                   OtherId + ",VAR_OTHER,R0,other\r\n";
        }

        private static IReadOnlyList<IReadOnlyList<string>> Records(byte[] bytes)
        {
            var read = new Rfc4180CsvReader().Read(bytes, "test.csv");
            Assert.That(read.Success, Is.True, string.Join("\n", read.Errors));
            return read.Records.Skip(1)
                .Select(record => (IReadOnlyList<string>)record.Fields.Select(field => field.Value).ToList())
                .ToList();
        }

        private static IReadOnlyList<string> Headers(byte[] bytes)
        {
            var read = new Rfc4180CsvReader().Read(bytes, "test.csv");
            Assert.That(read.Success, Is.True, string.Join("\n", read.Errors));
            return read.Records[0].Fields.Select(field => field.Value).ToList();
        }

        private static byte[] Utf8Bom(string text)
        {
            var content = new UTF8Encoding(false, true).GetBytes(text);
            var result = new byte[content.Length + 3];
            result[0] = 0xEF;
            result[1] = 0xBB;
            result[2] = 0xBF;
            Buffer.BlockCopy(content, 0, result, 3, content.Length);
            return result;
        }

        private static string StateSignature(MicrochunkSocketAndSlotEditorViewModel state)
        {
            return string.Join("\n", state.Grid.State.Cells.Select(cell =>
                       cell.Coordinate.RowMajorIndex + ":" + string.Join("|", cell.TileCodes))) + "\n" +
                   string.Join("\n", state.SocketAuthoring.Sockets.Select(row => row.SocketId)) + "\n" +
                   string.Join("\n", state.SocketAuthoring.Bands.Select(row => row.BandId)) + "\n" +
                   string.Join("\n", state.ObjectSlotAuthoring.Rows.Select(row => row.SlotId));
        }

        private static void WithTempRoot(MicrochunkCsvImportSource source, Action<string> action)
        {
            var root = Path.Combine(Path.GetTempPath(), "MAP07_11_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "MicroChunk"));
            Directory.CreateDirectory(Path.Combine(root, "Route"));
            try
            {
                File.WriteAllBytes(Path.Combine(root, "MicroChunk", MicrochunkCsvImportSource.CatalogFileName), source.CatalogBytes);
                File.WriteAllBytes(Path.Combine(root, "MicroChunk", MicrochunkCsvImportSource.TileCellsFileName), source.TileCellBytes);
                File.WriteAllBytes(Path.Combine(root, "MicroChunk", MicrochunkCsvImportSource.SocketsFileName), source.SocketBytes);
                File.WriteAllBytes(Path.Combine(root, "Route", MicrochunkCsvImportSource.SocketBandsFileName), source.SocketBandBytes);
                File.WriteAllBytes(Path.Combine(root, "MicroChunk", MicrochunkCsvImportSource.ObjectSlotsFileName), source.ObjectSlotBytes);
                File.WriteAllBytes(Path.Combine(root, "MicroChunk", MicrochunkCsvImportSource.VariantsFileName), source.VariantBytes);
                action(root);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static string Target(string root, MicrochunkCsvExportFilePlan file)
        {
            return Path.Combine(root, file.RelativeDirectory, file.FileName);
        }

        private static string JoinIssues(MicrochunkCsvExportPlan plan)
        {
            return string.Join("\n", plan.Issues.Select(value => value.ToString()));
        }

        private static string JoinIssues(MicrochunkCsvExportResult result)
        {
            return string.Join("\n", result.Issues.Select(value => value.ToString()));
        }

        private const string CatalogHeader =
            "microchunk_id,tile_data_complete,display_name,notes\r\n";
        private const string TileHeader =
            "microchunk_id,local_x,local_y,ground_code,one_way_code,breakable_code,hazard_code," +
            "liquid_code,decor_back_code,decor_front_code,marker_code\r\n";
        private const string SocketHeader =
            "microchunk_id,socket_id,side,band_id,traversal_kind,direction,mandatory_allowed," +
            "tool_requirement,edge_signature_id,route_layer,minimum_safe_tiles,notes\r\n";
        private const string BandHeader =
            "band_id,axis,min_local_coord,max_local_coord,recommended_center,minimum_clearance_tiles,description_ko\r\n";
        private const string SlotHeader =
            "microchunk_id,slot_id,local_x,local_y,slot_category,allowed_pool_id,required," +
            "orientation,visible_from_route,forbidden_radius_tiles,required_marker_code,notes\r\n";
        private const string VariantHeader = "microchunk_id,variant_id,transform,notes\r\n";
        private const string EdgeHeader =
            "edge_signature_id,axis,band_id,traversal_kind,ground_entry_height,clearance_width," +
            "clearance_height,tool_requirement,mandatory_allowed,tags,notes\r\n";
        private const string EdgeRow =
            "EDGE_H_MID_WALK,HORIZONTAL_EDGE,BAND_H_MID,WALK,0,2,3,NONE,1,WALK,test\r\n";
    }
}
