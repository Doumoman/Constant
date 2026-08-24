using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;
using StarNight.MapAuthoring.Microchunks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_10")]
    public sealed class MicrochunkCsvImporterTests
    {
        private const string SelectedId = "MC_IMPORT_TEST";

        public static IEnumerable<TestCaseData> ContractCases
        {
            get
            {
                for (var caseId = 0; caseId < 420; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MicrochunkCsvImporterContract_" + caseId.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void MicrochunkCsvImporterContract(int caseId)
        {
            var variant = caseId / 21;
            switch (caseId % 21)
            {
                case 0: AssertSelectedIdIsRequired(variant); break;
                case 1: AssertMissingCatalogFails(variant); break;
                case 2: AssertDuplicateCatalogFails(variant); break;
                case 3: AssertRfc4180BomQuotedCommaAndNewline(variant); break;
                case 4: AssertCompleteTilesAreRowMajor(variant); break;
                case 5: AssertDuplicateTileFails(variant); break;
                case 6: AssertMissingCompleteTileFails(variant); break;
                case 7: AssertOutOfRangeTileFails(variant); break;
                case 8: AssertIncompleteTilesFillNoneWithWarnings(variant); break;
                case 9: AssertLayerValuesAreDetached(variant); break;
                case 10: AssertSocketsHydrateEveryOwnedField(variant); break;
                case 11: AssertSocketBandSideCompatibility(variant); break;
                case 12: AssertObjectSlotsHydrateEveryOwnedField(variant); break;
                case 13: AssertVariantsRemainMetadataOnly(variant); break;
                case 14: AssertDiagnosticsAreCanonicallyOrdered(variant); break;
                case 15: AssertExistingValidatorsConsumeImportedState(variant); break;
                case 16: AssertWindowHasNoPersistenceCommands(variant); break;
                case 17: AssertSourceSnapshotsAreReadOnly(variant); break;
                case 18: AssertEditorCollectionsUseCanonicalOrdering(variant); break;
                case 19: AssertEditorBoundaryAndFutureSymbols(variant); break;
                default: AssertProjectAuthoringImportIsReadOnly(variant); break;
            }
        }

        private static void AssertSelectedIdIsRequired(int variant)
        {
            Assert.That(() => new MicrochunkCsvImportRequest(null), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkCsvImportRequest(string.Empty), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkCsvImportRequest(" "), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new MicrochunkCsvImportRequest(" " + SelectedId), Throws.TypeOf<ArgumentException>());
            Assert.That(new MicrochunkCsvImportRequest(SelectedId).SelectedId.Value, Is.EqualTo(SelectedId));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertMissingCatalogFails(int variant)
        {
            var source = Source(catalog: Catalog("MC_OTHER", true), tiles: CompleteTiles(SelectedId));
            var result = Import(source);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.Issues.Single(issue => issue.Code == "CATALOG_ROW_MISSING").RowNumber,
                Is.EqualTo(0));
            Assert.That(variant, Is.GreaterThanOrEqualTo(0));
        }

        private static void AssertDuplicateCatalogFails(int variant)
        {
            var catalog = "microchunk_id,tile_data_complete,notes\r\n" +
                          SelectedId + ",1,a\r\n" + SelectedId + ",1,b\r\n";
            var result = Import(Source(catalog: catalog));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.Code == "CATALOG_ROW_DUPLICATE"), Is.True);
            Assert.That(result.Issues.Where(issue => issue.Code == "CATALOG_ROW_DUPLICATE")
                .Select(issue => issue.RowNumber), Is.EqualTo(new[] { 3 }));
            Assert.That(variant, Is.LessThan(20));
        }

        private static void AssertRfc4180BomQuotedCommaAndNewline(int variant)
        {
            var notes = "hello, \"moon\"\r\nline " + variant;
            var catalog = "microchunk_id,tile_data_complete,notes\r\n" + SelectedId +
                          ",1,\"hello, \"\"moon\"\"\r\nline " + variant + "\"\r\n";
            var result = Import(Source(catalog: catalog));
            Assert.That(result.Success, Is.True, JoinIssues(result));
            Assert.That(result.Catalog.Fields["notes"], Is.EqualTo(notes));
            Assert.That(result.Catalog.SourceRowNumber, Is.EqualTo(2));
        }

        private static void AssertCompleteTilesAreRowMajor(int variant)
        {
            var result = Import(Source());
            Assert.That(result.Success, Is.True, JoinIssues(result));
            Assert.That(result.GridState.Cells, Has.Count.EqualTo(96));
            Assert.That(result.GridState.Cells.Select(cell => cell.Coordinate.RowMajorIndex),
                Is.EqualTo(Enumerable.Range(0, 96)));
            Assert.That(result.GridState.GetTileCode(variant % 12, variant % 8,
                MicrochunkTileLayer.GroundSolid), Is.EqualTo("NONE"));
        }

        private static void AssertDuplicateTileFails(int variant)
        {
            var tiles = CompleteTiles(SelectedId) + TileRow(SelectedId, 0, 0, "DUP_" + variant);
            var result = Import(Source(tiles: tiles));
            Assert.That(result.Success, Is.False);
            var issue = result.Issues.Single(value => value.Code == "TILE_COORDINATE_DUPLICATE");
            Assert.That(issue.FileName, Is.EqualTo(MicrochunkCsvImportSource.TileCellsFileName));
            Assert.That(issue.RowNumber, Is.EqualTo(98));
        }

        private static void AssertMissingCompleteTileFails(int variant)
        {
            var missingIndex = variant;
            var tiles = TileHeader + string.Concat(
                Enumerable.Range(0, 96)
                    .Where(index => index != missingIndex)
                    .Select(index => TileRow(SelectedId, index % 12, index / 12)));
            var result = Import(Source(tiles: tiles));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.Code == "TILE_CELL_MISSING_" + missingIndex.ToString("D2")), Is.True);
            Assert.That(result.GridState.GetCell(missingIndex % 12, missingIndex / 12).TileCodes,
                Is.All.EqualTo("NONE"));
        }

        private static void AssertOutOfRangeTileFails(int variant)
        {
            var x = variant % 2 == 0 ? 12 : -1;
            var result = Import(Source(tiles:
                CompleteTiles(SelectedId) + TileRow(SelectedId, x, variant % 8)));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.Code == "TILE_COORDINATE_OUT_OF_RANGE"), Is.True);
            Assert.That(result.GridState.CellCount, Is.EqualTo(96));
        }

        private static void AssertIncompleteTilesFillNoneWithWarnings(int variant)
        {
            var x = variant % 12;
            var y = variant % 8;
            var result = Import(Source(
                catalog: Catalog(SelectedId, false),
                tiles: TileHeader + TileRow(SelectedId, x, y, "GROUND")));
            Assert.That(result.Success, Is.True, JoinIssues(result));
            Assert.That(result.GridState.CellCount, Is.EqualTo(96));
            Assert.That(result.GridState.GetTileCode(x, y, MicrochunkTileLayer.GroundSolid),
                Is.EqualTo("GROUND"));
            Assert.That(result.GridState.Cells.Where(cell => cell.Coordinate.X != x || cell.Coordinate.Y != y)
                .SelectMany(cell => cell.TileCodes), Is.All.EqualTo("NONE"));
            Assert.That(result.Issues.All(issue => !issue.IsError), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == "TILE_DATA_INCOMPLETE"), Is.True);
        }

        private static void AssertLayerValuesAreDetached(int variant)
        {
            var input = Utf8(CompleteTiles(SelectedId, variant));
            var source = new MicrochunkCsvImportSource(Utf8(Catalog(SelectedId, true)), input);
            var result = Import(source);
            var coordinate = variant % 96;
            Assert.That(result.GridState.GetTileCode(coordinate % 12, coordinate / 12,
                MicrochunkTileLayer.Marker), Is.EqualTo("M_IMPORTED_" + variant));
            input[input.Length - 2] = (byte)'X';
            Assert.That(result.GridState.GetTileCode(coordinate % 12, coordinate / 12,
                MicrochunkTileLayer.Marker), Is.EqualTo("M_IMPORTED_" + variant));
            Assert.That(result.GridState.GetCell(coordinate % 12, coordinate / 12).TileCodes.Count,
                Is.EqualTo(8));
        }

        private static void AssertSocketsHydrateEveryOwnedField(int variant)
        {
            var mandatory = variant % 2 == 0;
            var sockets = SocketHeader + SelectedId + ",SOCK_A,L,BAND_H_MID,WALK,BIDIRECTIONAL," +
                          (mandatory ? "1" : "0") + ",NONE,EDGE_H_MID_WALK,BOTH,2,\r\n";
            var result = Import(Source(sockets: sockets));
            Assert.That(result.Success, Is.True, JoinIssues(result));
            var row = result.EditorState.SocketAuthoring.Sockets.Single();
            Assert.That(row.SocketId, Is.EqualTo("SOCK_A"));
            Assert.That(row.SideToken, Is.EqualTo("L"));
            Assert.That(row.BandId, Is.EqualTo("BAND_H_MID"));
            Assert.That(row.TraversalKindToken, Is.EqualTo("WALK"));
            Assert.That(row.EdgeSignatureId, Is.EqualTo("EDGE_H_MID_WALK"));
            Assert.That(row.MandatoryAllowed, Is.EqualTo(mandatory));
            Assert.That(row.ToolRequirementToken, Is.EqualTo("NONE"));
        }

        private static void AssertSocketBandSideCompatibility(int variant)
        {
            var side = variant % 2 == 0 ? "L" : "U";
            var axis = variant % 2 == 0 ? "HORIZONTAL_EDGE" : "HORIZONTAL_EDGE";
            var sockets = SocketHeader + SelectedId + ",SOCK_A," + side +
                          ",BAND_X,WALK,BIDIRECTIONAL,1,NONE,EDGE_H_MID_WALK,BOTH,2,\r\n";
            var bands = BandHeader + "BAND_X," + axis + ",0,3,1.5,2,test\r\n";
            var result = Import(Source(sockets: sockets, bands: bands));
            if (side == "L")
            {
                Assert.That(result.EditorState.SocketAuthoring.Bands.Single().SideToken, Is.EqualTo("L"));
                Assert.That(result.Issues.Any(issue => issue.Code == "SOCKET_BAND_SIDE_INCOMPATIBLE"), Is.False);
            }
            else
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Issues.Any(issue => issue.Code == "SOCKET_BAND_SIDE_INCOMPATIBLE"), Is.True);
            }
        }

        private static void AssertObjectSlotsHydrateEveryOwnedField(int variant)
        {
            var orientation = new[] { "NONE", "L", "R", "U", "D" }[variant % 5];
            var slots = SlotHeader + SelectedId + ",SLOT_A,3,4,RESOURCE,POOL_A,1," +
                        orientation + ",0,2,NONE,\r\n";
            var result = Import(Source(slots: slots));
            Assert.That(result.Success, Is.True, JoinIssues(result));
            var row = result.EditorState.ObjectSlotAuthoring.Rows.Single();
            Assert.That(row.SlotId, Is.EqualTo("SLOT_A"));
            Assert.That(row.Anchor, Is.EqualTo(new MicrochunkLocalCoord(3, 4)));
            Assert.That(row.CategoryToken, Is.EqualTo("RESOURCE"));
            Assert.That(row.PoolId, Is.EqualTo("POOL_A"));
            Assert.That(row.Required, Is.True);
            Assert.That(row.VisibleFromRoute, Is.False);
            Assert.That(row.SafetyRadiusTiles, Is.EqualTo(2));
            Assert.That(row.OrientationToken, Is.EqualTo(orientation));
        }

        private static void AssertVariantsRemainMetadataOnly(int variant)
        {
            var variants = "microchunk_id,variant_id,transform,notes\r\n" + SelectedId +
                           ",VAR_" + variant + ",MIRROR_X,\"meta, only\"\r\n";
            var result = Import(Source(variants: variants));
            Assert.That(result.Success, Is.True, JoinIssues(result));
            Assert.That(result.Variants.Single().Fields["variant_id"], Is.EqualTo("VAR_" + variant));
            Assert.That(result.Variants.Single().Fields["notes"], Is.EqualTo("meta, only"));
            var names = typeof(MicrochunkCsvImporter).Assembly.GetTypes().Select(type => type.Name);
            Assert.That(names, Does.Not.Contain("MicrochunkTransformPreview"));
        }

        private static void AssertDiagnosticsAreCanonicallyOrdered(int variant)
        {
            var result = Import(Source(
                tiles: TileHeader + TileRow(SelectedId, 12, 0) + TileRow(SelectedId, 12, 0),
                catalog: Catalog(SelectedId, true)));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Zip(result.Issues.Skip(1),
                (left, right) => left.CompareTo(right) <= 0).All(value => value), Is.True);
            Assert.That(result.Issues.Select(issue => issue.FileName),
                Is.Ordered.Using((IComparer<string>)StringComparer.Ordinal));
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertExistingValidatorsConsumeImportedState(int variant)
        {
            var result = Import(Source());
            Assert.That(result.HasValidationFeedback, Is.True, JoinIssues(result));
            Assert.That(result.ValidationFeedback.TileLayerResult,
                Is.TypeOf<MicrochunkTileLayerRuleResult>());
            Assert.That(result.ValidationFeedback.CoverageResult,
                Is.TypeOf<Microchunk96CellValidationResult>());
            Assert.That(result.ValidationFeedback.SocketResult,
                Is.TypeOf<MicrochunkSocketEdgeValidationResult>());
            Assert.That(result.ValidationFeedback.ObjectSlotResult,
                Is.TypeOf<MicrochunkObjectSlotValidationResult>());
            var before = GridSignature(result.GridState);
            result.EditorState.Validate();
            Assert.That(GridSignature(result.GridState), Is.EqualTo(before));
            Assert.That(variant, Is.LessThan(20));
        }

        private static void AssertWindowHasNoPersistenceCommands(int variant)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var dirtyBefore = scene.IsValid() && scene.isDirty;
            var window = ScriptableObject.CreateInstance<MicrochunkCsvImportWindow>();
            try
            {
                var result = window.Import(Source(), SelectedId);
                Assert.That(result.Success, Is.True, JoinIssues(result));
                Assert.That(window.ImportedGrid, Is.SameAs(result.GridViewModel));
                Assert.That(window.ImportedSocketAndSlotState, Is.SameAs(result.EditorState));
                var names = typeof(MicrochunkCsvImportWindow).GetMethods(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.DeclaredOnly)
                    .Select(method => method.Name)
                    .ToArray();
                Assert.That(names, Has.None.Contains("Export").And.None.Contains("Save")
                    .And.None.Contains("Replace").And.None.Contains("CreateAsset"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
            var dirtyAfter = scene.IsValid() && scene.isDirty;
            Assert.That(dirtyAfter, Is.EqualTo(dirtyBefore));
            Assert.That(variant, Is.GreaterThanOrEqualTo(0));
        }

        private static void AssertSourceSnapshotsAreReadOnly(int variant)
        {
            var catalog = Utf8(Catalog(SelectedId, true));
            var tiles = Utf8(CompleteTiles(SelectedId));
            var source = new MicrochunkCsvImportSource(catalog, tiles);
            var expected = source.CatalogBytes[variant % source.CatalogBytes.Length];
            catalog[variant % catalog.Length] ^= 0x01;
            var exposed = source.CatalogBytes;
            exposed[variant % exposed.Length] ^= 0x01;
            Assert.That(source.CatalogBytes[variant % source.CatalogBytes.Length], Is.EqualTo(expected));
            Assert.That(Import(source).Success, Is.True);
        }

        private static void AssertEditorCollectionsUseCanonicalOrdering(int variant)
        {
            var sockets = SocketHeader +
                          SelectedId + ",Z_" + variant + ",R,BAND_H_MID,WALK,BIDIRECTIONAL,1,NONE,EDGE_H_MID_WALK,BOTH,2,\r\n" +
                          SelectedId + ",A_" + variant + ",L,BAND_H_MID,WALK,BIDIRECTIONAL,1,NONE,EDGE_H_MID_WALK,BOTH,2,\r\n";
            var slots = SlotHeader +
                        SelectedId + ",Z_" + variant + ",3,4,RESOURCE,POOL,0,NONE,1,0,NONE,\r\n" +
                        SelectedId + ",A_" + variant + ",5,4,RESOURCE,POOL,0,NONE,1,0,NONE,\r\n";
            var result = Import(Source(sockets: sockets, slots: slots));
            Assert.That(result.Success, Is.True, JoinIssues(result));
            Assert.That(result.EditorState.SocketAuthoring.Sockets.Select(row => row.SocketId),
                Is.EqualTo(new[] { "A_" + variant, "Z_" + variant }));
            Assert.That(result.EditorState.ObjectSlotAuthoring.Rows.Select(row => row.SlotId),
                Is.EqualTo(new[] { "A_" + variant, "Z_" + variant }));
        }

        private static void AssertEditorBoundaryAndFutureSymbols(int variant)
        {
            Assert.That(typeof(MicrochunkCsvImporter).Assembly.GetName().Name,
                Is.EqualTo("MapAuthoring.Editor"));
            Assert.That(typeof(MicrochunkCsvImporter).Assembly,
                Is.Not.EqualTo(typeof(MicrochunkDefinition).Assembly));
            Assert.That(typeof(EditorWindow).IsAssignableFrom(typeof(MicrochunkCsvImportWindow)), Is.True);
            var names = typeof(MicrochunkCsvImporter).Assembly.GetTypes().Select(type => type.Name).ToArray();
            Assert.That(names, Does.Contain("MicrochunkPreviewBuilder"));
            Assert.That(names, Does.Contain("MicrochunkPreviewReport"));
            Assert.That(names, Does.Contain("MicrochunkPreviewWindow"));
            foreach (var forbidden in new[]
                     {
                         "MicrochunkReachabilityHeatmap", "MicrochunkStarterCatalogRoundTrip",
                         "BoundaryChunkResolver", "SectorRecipeResolver", "GeneratedSectorMicrochunkWriter",
                         "PopulationSlotIndex", "StableSpawnId", "WorldTraversalValidator"
                     })
            {
                Assert.That(names, Does.Not.Contain(forbidden));
            }
            Assert.That(variant, Is.InRange(0, 19));
        }

        private static void AssertProjectAuthoringImportIsReadOnly(int variant)
        {
            var source = MicrochunkCsvImportSource.FromProjectAuthoringCsv();
            var beforeCatalog = source.CatalogBytes;
            var beforeTiles = source.TileCellBytes;
            var result = new MicrochunkCsvImporter().Import(
                source,
                new MicrochunkCsvImportRequest("MC_GRAY_H_STRAIGHT_01"));
            Assert.That(result.Success, Is.True, JoinIssues(result));
            Assert.That(result.GridState.CellCount, Is.EqualTo(96));
            Assert.That(source.CatalogBytes, Is.EqualTo(beforeCatalog));
            Assert.That(source.TileCellBytes, Is.EqualTo(beforeTiles));
            Assert.That(variant, Is.LessThan(20));
        }

        private static MicrochunkCsvImportResult Import(MicrochunkCsvImportSource source)
        {
            return new MicrochunkCsvImporter().Import(
                source,
                new MicrochunkCsvImportRequest(SelectedId));
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
                Utf8(catalog ?? Catalog(SelectedId, true)),
                Utf8(tiles ?? CompleteTiles(SelectedId)),
                Utf8(sockets ?? DefaultSockets),
                Utf8(bands ?? DefaultBands),
                Utf8(slots ?? DefaultSlots),
                Utf8(variants ?? VariantHeader),
                Utf8("tile_code,layer\r\nNONE,ANY\r\n"),
                Array.Empty<byte>(),
                Utf8(DefaultEdgeSignatures));
        }

        private static string Catalog(string id, bool complete)
        {
            return "microchunk_id,tile_data_complete,notes\r\n" + id + "," +
                   (complete ? "1" : "0") + ",test\r\n";
        }

        private static string CompleteTiles(string id, int markerIndex = -1)
        {
            return TileHeader + string.Concat(Enumerable.Range(0, 96).Select(index =>
                TileRow(id, index % 12, index / 12, "NONE",
                    index == markerIndex ? "M_IMPORTED_" + markerIndex : "NONE")));
        }

        private static string TileRow(
            string id,
            int x,
            int y,
            string ground = "NONE",
            string marker = "NONE")
        {
            return id + "," + x + "," + y + "," + ground +
                   ",NONE,NONE,NONE,NONE,NONE,NONE," + marker + "\r\n";
        }

        private static byte[] Utf8(string text)
        {
            var content = new UTF8Encoding(false, true).GetBytes(text);
            var bytes = new byte[content.Length + 3];
            bytes[0] = 0xEF;
            bytes[1] = 0xBB;
            bytes[2] = 0xBF;
            Buffer.BlockCopy(content, 0, bytes, 3, content.Length);
            return bytes;
        }

        private static string GridSignature(MicrochunkAuthoringGridState state)
        {
            return string.Join("\n", state.Cells.Select(cell =>
                cell.Coordinate.RowMajorIndex + ":" + string.Join("|", cell.TileCodes)));
        }

        private static string JoinIssues(MicrochunkCsvImportResult result)
        {
            return string.Join("\n", result.Issues.Select(issue => issue.ToString()));
        }

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
        private const string DefaultSockets = SocketHeader +
            SelectedId + ",SOCK_L,L,BAND_H_MID,WALK,BIDIRECTIONAL,1,NONE," +
            "EDGE_H_MID_WALK,BOTH,2,\r\n";
        private const string DefaultBands = BandHeader +
            "BAND_H_MID,HORIZONTAL_EDGE,3,4,3.5,2,middle\r\n";
        private const string DefaultSlots = SlotHeader +
            SelectedId + ",SLOT_A,6,1,RESOURCE,POOL_A,0,NONE,1,0,NONE,\r\n";
        private const string DefaultEdgeSignatures =
            "edge_signature_id,axis,band_id,traversal_kind,ground_entry_height,clearance_width," +
            "clearance_height,tool_requirement,mandatory_allowed,tags,notes\r\n" +
            "EDGE_H_MID_WALK,HORIZONTAL_EDGE,BAND_H_MID,WALK,0,2,3,NONE,1,WALK,test\r\n";
    }
}
