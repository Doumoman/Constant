using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.MapAuthoring.WorldGeneration.Import;
using StarNight.MapAuthoring.WorldGeneration.MicroPatterns;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.MicroPatterns
{
    [TestFixture]
    [Category("MAP10_07")]
    public sealed class MicroPatternPreviewTests
    {
        private const string ExpectedCatalogDigest =
            "6a5aefd2eb368348d594158cc3f14e94d0ea509ea2cdd207a7715e8da80d19ac";
        private const string ExpectedCatalogSha =
            "f9d9e9cc60c4e4d7561c5aa6502228c18fc9566e3e0febab206ea3264b408267";
        private const string ExpectedCellsSha =
            "e702ae5d02d7ec9d2cda129c1361699e37d942c280c8f9bd1f3200f155084381";
        private const string ExpectedAuthoringManifest =
            "4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851";

        private static readonly string[] ExpectedPatternIds =
        {
            "MP_CRATER_BOWL",
            "MP_CRATER_BROKEN_SLOPE",
            "MP_CRATER_DUST_PATCH",
            "MP_CRATER_GRIP_RIDGE",
            "MP_CRATER_METEOR_CUE",
            "MP_CRATER_ROCK_SHELF",
            "MP_DOUGH_BOUNCE_CUP",
            "MP_DOUGH_BOUNCE_STRIP",
            "MP_DOUGH_FERMENT_PATCH",
            "MP_DOUGH_RECOVERY_PAD",
            "MP_DOUGH_SOFT_POCKET",
            "MP_DOUGH_STICKY_SHELF",
            "MP_MILL_BEAM_GRIP",
            "MP_MILL_BEAM_OVERHANG",
            "MP_MILL_BROKEN_PILLAR",
            "MP_MILL_GEAR_SOCKET",
            "MP_MILL_ORTHOGONAL_CARVE",
            "MP_MILL_RUST_PATCH",
            "MP_ROOT_ARCH",
            "MP_ROOT_CLIMB_VINES",
            "MP_ROOT_HOLLOW_POCKET",
            "MP_ROOT_SAP_PATCH",
            "MP_ROOT_SPROUT_MARK",
            "MP_ROOT_VERTICAL_TUNNEL",
        };

        [Test]
        public void PhysicalCatalogAndAuthoringBoundaryRemainExact()
        {
            var import = Import();
            Assert.That(import.Published, Is.True);
            Assert.That(import.Catalog.Count, Is.EqualTo(24));
            Assert.That(import.StableDigest, Is.EqualTo(ExpectedCatalogDigest));

            var catalogPath = FullPath(MicroPatternCsvImporterV2.CatalogProjectRelativePath);
            var cellsPath = FullPath(MicroPatternCsvImporterV2.CellsProjectRelativePath);
            Assert.That(Sha256(File.ReadAllBytes(catalogPath)), Is.EqualTo(ExpectedCatalogSha));
            Assert.That(Sha256(File.ReadAllBytes(cellsPath)), Is.EqualTo(ExpectedCellsSha));
            Assert.That(DataRowCount(catalogPath), Is.EqualTo(24));
            Assert.That(DataRowCount(cellsPath), Is.EqualTo(453));

            var authoringRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var csvFiles = Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories);
            Assert.That(csvFiles, Has.Length.EqualTo(52));
            Assert.That(ComputeManifest(authoringRoot, csvFiles), Is.EqualTo(ExpectedAuthoringManifest));
            Assert.That(GeneratedCsvFiles(), Is.Empty);
        }

        [Test]
        public void SelectionInventoryHasExactBiomesRolesIdsAndAllowedPairs()
        {
            var definitions = Import().Catalog.Definitions;
            Assert.That(definitions.Select(value => value.Id.Value), Is.EqualTo(ExpectedPatternIds));
            Assert.That(definitions
                .GroupBy(value => value.AllowedBiomes.Single().CanonicalId)
                .ToDictionary(value => value.Key, value => value.Count(), StringComparer.Ordinal)
                .Values, Is.All.EqualTo(6));

            var roles = definitions.GroupBy(MicroPatternPreviewModel.GetRoleGroup)
                .ToDictionary(value => value.Key, value => value.Count());
            Assert.That(roles[MicroPatternPreviewRoleGroup.Geometry], Is.EqualTo(12));
            Assert.That(roles[MicroPatternPreviewRoleGroup.SurfaceAffordance], Is.EqualTo(4));
            Assert.That(roles[MicroPatternPreviewRoleGroup.Detail], Is.EqualTo(8));
            Assert.That(definitions.Sum(value => value.AllowedTransforms.Count), Is.EqualTo(56));
        }

        [Test]
        public void AllFiftySixCleanPairsPublishFivePanelsAndExposeEveryWrite()
        {
            var catalog = Import().Catalog;
            var model = new MicroPatternPreviewModel();
            var built = 0;
            foreach (var definition in catalog.Definitions)
            {
                foreach (var transform in definition.AllowedTransforms)
                {
                    var result = model.Build(new MicroPatternPreviewRequest(
                        definition.Id.Value, transform, MicroPatternPreviewFixtureKind.Clean), catalog);
                    Assert.That(result.Success, Is.True, PreviewErrors(result));
                    var snapshot = result.Snapshot;
                    var expectedWrites = definition.Cells.SelectMany(value => value.Instructions)
                        .Count(value => value.Operation != MicroPatternOperation.NoChange);

                    Assert.That(snapshot.PanelCount, Is.EqualTo(5), Pair(definition, transform));
                    AssertPanels(snapshot);
                    Assert.That(snapshot.PatternId, Is.EqualTo(definition.Id.Value));
                    Assert.That(snapshot.SelectedTransform, Is.EqualTo(transform));
                    Assert.That(snapshot.PlanPublished, Is.True);
                    Assert.That(snapshot.RendererInvoked, Is.True);
                    Assert.That(snapshot.RenderPublished, Is.True);
                    Assert.That(snapshot.Writes, Has.Count.EqualTo(expectedWrites));
                    Assert.That(snapshot.Diffs, Has.Count.EqualTo(expectedWrites));
                    Assert.That(snapshot.Diffs.All(value => value.Changed), Is.True);
                    Assert.That(snapshot.Writes.Select(value => (int)value.Stage), Is.Ordered);
                    Assert.That(snapshot.Writes.All(value =>
                        (int)value.Stage == ExpectedStage(value.Layer)), Is.True);
                    Assert.That(snapshot.PipelineErrors, Is.Empty);
                    Assert.That(snapshot.ConflictEvidence, Is.Empty);
                    AssertDigests(snapshot, true);
                    built++;
                }
            }
            Assert.That(built, Is.EqualTo(56));
        }

        [Test]
        public void TransformedCoordinatesMatchIndependentMap10_02Evidence()
        {
            var catalog = Import().Catalog;
            var model = new MicroPatternPreviewModel();
            foreach (var definition in catalog.Definitions)
            {
                foreach (var transform in definition.AllowedTransforms)
                {
                    var snapshot = model.Build(new MicroPatternPreviewRequest(
                        definition.Id.Value, transform, MicroPatternPreviewFixtureKind.Clean),
                        catalog).Snapshot;
                    var expected = definition.Cells.Select(cell =>
                            InstructionCellKey(Transform(cell.Coordinate, transform), cell.Instructions))
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                    var actual = snapshot.TransformedCells.Select(CellKey)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                    Assert.That(actual, Is.EqualTo(expected), Pair(definition, transform));
                }
            }
        }

        [Test]
        public void RejectCandidateProtectedOverlapRejectsAllTwelveWithoutRendererPublication()
        {
            var catalog = Import().Catalog;
            var model = new MicroPatternPreviewModel();
            var rejected = catalog.Definitions.Where(value =>
                value.ProtectedPolicy == MicroPatternProtectedPolicy.RejectCandidate).ToArray();
            Assert.That(rejected, Has.Length.EqualTo(12));

            foreach (var definition in rejected)
            {
                var result = model.Build(new MicroPatternPreviewRequest(
                    definition.Id.Value, MicroPatternTransform.R0,
                    MicroPatternPreviewFixtureKind.ProtectedOverlap), catalog);
                Assert.That(result.Success, Is.True, PreviewErrors(result));
                var snapshot = result.Snapshot;
                Assert.That(snapshot.PlanPublished, Is.False, definition.Id.Value);
                Assert.That(snapshot.PlanDigest, Is.Empty);
                Assert.That(snapshot.RendererInvoked, Is.False);
                Assert.That(snapshot.RenderPublished, Is.False);
                Assert.That(snapshot.RenderDigest, Is.Empty);
                Assert.That(snapshot.Writes, Is.Empty);
                Assert.That(snapshot.Diffs, Is.Empty);
                Assert.That(snapshot.ProtectedHitCount, Is.GreaterThan(0));
                Assert.That(snapshot.ProtectedProvenance.Any(value =>
                    value.Contains(MicroPatternPreviewModel.ProtectedSourceId)), Is.True);
                Assert.That(snapshot.TransformedCells.Count(value => value.IsProtected), Is.EqualTo(1));
                Assert.That(snapshot.PipelineErrors.Any(value =>
                    value.Contains("ProtectedWriteRejected")), Is.True);
                Assert.That(snapshot.BeforeCells.Select(StateKey),
                    Is.EqualTo(snapshot.AfterCells.Select(StateKey)));
                AssertDigests(snapshot, false);
            }
        }

        [Test]
        public void ForceNoChangeProtectedOverlapMasksAllTwelveAndPreservesProvenance()
        {
            var catalog = Import().Catalog;
            var model = new MicroPatternPreviewModel();
            var forced = catalog.Definitions.Where(value =>
                value.ProtectedPolicy == MicroPatternProtectedPolicy.ForceNoChange).ToArray();
            Assert.That(forced, Has.Length.EqualTo(12));

            foreach (var definition in forced)
            {
                var clean = model.Build(new MicroPatternPreviewRequest(
                    definition.Id.Value, MicroPatternTransform.R0,
                    MicroPatternPreviewFixtureKind.Clean), catalog).Snapshot;
                var result = model.Build(new MicroPatternPreviewRequest(
                    definition.Id.Value, MicroPatternTransform.R0,
                    MicroPatternPreviewFixtureKind.ProtectedOverlap), catalog);
                Assert.That(result.Success, Is.True, PreviewErrors(result));
                var snapshot = result.Snapshot;
                Assert.That(snapshot.PlanPublished, Is.True, definition.Id.Value);
                Assert.That(snapshot.RendererInvoked, Is.True);
                Assert.That(snapshot.RenderPublished, Is.True);
                Assert.That(snapshot.ProtectedHitCount, Is.GreaterThan(0));
                Assert.That(snapshot.ProtectedProvenance.Any(value =>
                    value.Contains(MicroPatternPreviewModel.ProtectedSourceId)), Is.True);
                var protectedCell = snapshot.ProtectedEffectiveCells.Single(value => value.IsProtected);
                Assert.That(protectedCell.CompactToken, Is.EqualTo("·"));
                Assert.That(snapshot.Writes.Count, Is.LessThan(clean.Writes.Count));
                Assert.That(snapshot.PipelineErrors, Is.Empty);
                Assert.That(snapshot.ConflictEvidence, Is.Empty);
                AssertDigests(snapshot, true);
            }
        }

        [Test]
        public void SameLayerConflictUsesActualRendererAndPublishesNoPartialDelta()
        {
            var catalog = Import().Catalog;
            var model = new MicroPatternPreviewModel();
            var request = new MicroPatternPreviewRequest(
                MicroPatternPreviewModel.ConflictFirstPatternId,
                MicroPatternTransform.R0,
                MicroPatternPreviewFixtureKind.SameLayerConflict);
            var first = model.Build(request, catalog);
            var second = model.Build(request, catalog);
            Assert.That(first.Success, Is.True, PreviewErrors(first));
            Assert.That(second.Success, Is.True, PreviewErrors(second));
            var snapshot = first.Snapshot;

            Assert.That(snapshot.PatternId,
                Is.EqualTo(MicroPatternPreviewModel.ConflictFirstPatternId));
            Assert.That(snapshot.PlanPublished, Is.True);
            Assert.That(snapshot.RendererInvoked, Is.True);
            Assert.That(snapshot.RenderPublished, Is.False);
            Assert.That(snapshot.RenderDigest, Is.Empty);
            Assert.That(snapshot.Writes, Is.Empty);
            Assert.That(snapshot.Diffs, Is.Empty);
            Assert.That(snapshot.PipelineErrors.Any(value =>
                value.Contains("AtomicRenderRejected")), Is.True);
            Assert.That(snapshot.PipelineErrors.Any(value =>
                value.Contains("ConflictingLayerWrite")), Is.True);
            Assert.That(snapshot.ConflictEvidence.Any(value => value.Contains("Material")), Is.True);
            Assert.That(snapshot.ConflictEvidence.Any(value => value.Contains("MAT_MOON_DUST")), Is.True);
            Assert.That(snapshot.ConflictEvidence.Any(value => value.Contains("MAT_CASSIA_SAP")), Is.True);
            Assert.That(snapshot.BeforeCells.Select(StateKey),
                Is.EqualTo(snapshot.AfterCells.Select(StateKey)));
            Assert.That(second.Snapshot.StableDigest, Is.EqualTo(snapshot.StableDigest));
        }

        [Test]
        public void PipelineAndPreviewDigestsRepeatAcrossPhysicalInputOrder()
        {
            var importer = new MicroPatternCsvImporterV2();
            var catalogBytes = File.ReadAllBytes(FullPath(
                MicroPatternCsvImporterV2.CatalogProjectRelativePath));
            var cellBytes = File.ReadAllBytes(FullPath(
                MicroPatternCsvImporterV2.CellsProjectRelativePath));
            var canonical = importer.ParseBytes(catalogBytes, cellBytes);
            var reordered = importer.ParseBytes(
                ReverseDataRows(catalogBytes), ReverseDataRows(cellBytes));
            Assert.That(canonical.Success, Is.True, ImportErrors(canonical));
            Assert.That(reordered.Success, Is.True, ImportErrors(reordered));
            Assert.That(reordered.StableDigest, Is.EqualTo(canonical.StableDigest));

            var model = new MicroPatternPreviewModel();
            var pairCount = 0;
            var zeroSignatures = 0;
            var nonZeroSignatures = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in canonical.Catalog.Definitions)
            {
                foreach (var transform in definition.AllowedTransforms)
                {
                    var request = new MicroPatternPreviewRequest(
                        definition.Id.Value, transform, MicroPatternPreviewFixtureKind.Clean);
                    var first = model.Build(request, canonical.Catalog).Snapshot;
                    var repeated = model.Build(request, canonical.Catalog).Snapshot;
                    var reorderedSnapshot = model.Build(request, reordered.Catalog).Snapshot;
                    Assert.That(repeated.DefinitionDigest, Is.EqualTo(first.DefinitionDigest));
                    Assert.That(repeated.PlanDigest, Is.EqualTo(first.PlanDigest));
                    Assert.That(repeated.RenderDigest, Is.EqualTo(first.RenderDigest));
                    Assert.That(repeated.SilhouetteDigest, Is.EqualTo(first.SilhouetteDigest));
                    Assert.That(repeated.StableDigest, Is.EqualTo(first.StableDigest));
                    Assert.That(reorderedSnapshot.StableDigest, Is.EqualTo(first.StableDigest));
                    pairCount++;
                }

                var r0 = model.Build(new MicroPatternPreviewRequest(
                    definition.Id.Value, MicroPatternTransform.R0,
                    MicroPatternPreviewFixtureKind.Clean), canonical.Catalog).Snapshot;
                if (r0.SilhouetteAddSolidMask == 0 && r0.SilhouetteCarveAirMask == 0)
                    zeroSignatures++;
                else
                    nonZeroSignatures.Add(r0.SilhouetteDigest);
            }
            Assert.That(pairCount, Is.EqualTo(56));
            Assert.That(zeroSignatures, Is.EqualTo(12));
            Assert.That(nonZeroSignatures, Has.Count.EqualTo(12));
        }

        [Test]
        public void SnapshotCollectionsAreImmutableAndWindowBindsExactMenuAndFivePanels()
        {
            var generatedBefore = GeneratedCsvFiles();
            var activeScene = EditorSceneManager.GetActiveScene();
            var sceneDirtyBefore = activeScene.isDirty;
            var rootCountBefore = activeScene.GetRootGameObjects().Length;
            var model = new MicroPatternPreviewModel();
            var snapshot = model.Build(new MicroPatternPreviewRequest(
                ExpectedPatternIds[0], MicroPatternTransform.R0,
                MicroPatternPreviewFixtureKind.Clean)).Snapshot;

            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternTransform>)snapshot.AllowedTransforms).Add(MicroPatternTransform.R180));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternPreviewCell>)snapshot.OriginalCells).Add(null));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)snapshot.OriginalCells[0].Tokens).Add("MUTATE"));

            var open = typeof(MicroPatternPreviewWindow).GetMethod(
                "Open", BindingFlags.Public | BindingFlags.Static);
            Assert.That(open, Is.Not.Null);
            Assert.That(open.GetCustomAttributes(typeof(MenuItem), false), Has.Length.EqualTo(1));
            Assert.That(MicroPatternPreviewWindow.MenuPath,
                Is.EqualTo("Tools/MapDesign/MicroPattern Preview"));
            Assert.That(MicroPatternPreviewWindow.WindowTitle, Is.EqualTo("MicroPattern Preview"));

            MicroPatternPreviewWindow window = null;
            try
            {
                window = MicroPatternPreviewWindow.Open();
                Assert.That(window.Reload(), Is.True, window.LastError);
                Assert.That(window.titleContent.text, Is.EqualTo("MicroPattern Preview"));
                Assert.That(window.PatternIds.Count, Is.EqualTo(24));
                Assert.That(window.PanelCount, Is.EqualTo(5));
                Assert.That(window.CurrentSnapshot, Is.Not.Null);
                AssertPanels(window.CurrentSnapshot);
                Assert.That(window.TrySelectBiome("MoonCrater"), Is.True);
                Assert.That(window.PatternIds.Count, Is.EqualTo(6));
                Assert.That(window.TrySelectFixture(
                    MicroPatternPreviewFixtureKind.SameLayerConflict), Is.True);
                Assert.That(window.CurrentSnapshot.ConflictEvidence, Is.Not.Empty);
                window.Repaint();
            }
            finally
            {
                if (window != null) window.Close();
            }

            Assert.That(EditorSceneManager.GetActiveScene().isDirty, Is.EqualTo(sceneDirtyBefore));
            Assert.That(EditorSceneManager.GetActiveScene().GetRootGameObjects(),
                Has.Length.EqualTo(rootCountBefore));
            Assert.That(GeneratedCsvFiles(), Is.EqualTo(generatedBefore));
        }

        private static MicroPatternCsvImportResult Import()
        {
            var result = new MicroPatternCsvImporterV2().Import();
            Assert.That(result.Success, Is.True, ImportErrors(result));
            return result;
        }

        private static void AssertPanels(MicroPatternPreviewSnapshot snapshot)
        {
            Assert.That(snapshot.OriginalCells, Has.Count.EqualTo(16));
            Assert.That(snapshot.TransformedCells, Has.Count.EqualTo(16));
            Assert.That(snapshot.ProtectedEffectiveCells, Has.Count.EqualTo(16));
            Assert.That(snapshot.BeforeCells, Has.Count.EqualTo(16));
            Assert.That(snapshot.AfterCells, Has.Count.EqualTo(16));
        }

        private static void AssertDigests(MicroPatternPreviewSnapshot snapshot, bool fullPipeline)
        {
            Assert.That(snapshot.CatalogDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(snapshot.DefinitionDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(snapshot.TransformDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(snapshot.StableDigest, Does.Match("^[0-9a-f]{64}$"));
            if (!fullPipeline) return;
            Assert.That(snapshot.PlanDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(snapshot.RenderDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(snapshot.SilhouetteDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static int ExpectedStage(MicroPatternLayer layer)
        {
            switch (layer)
            {
                case MicroPatternLayer.Geometry: return 10;
                case MicroPatternLayer.Surface: return 20;
                case MicroPatternLayer.Affordance: return 30;
                case MicroPatternLayer.Material: return 40;
                case MicroPatternLayer.Hazard: return 50;
                case MicroPatternLayer.Marker: return 60;
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }
        }

        private static LocalTileCoord Transform(LocalTileCoord source, MicroPatternTransform transform)
        {
            switch (transform)
            {
                case MicroPatternTransform.R0: return source;
                case MicroPatternTransform.MirrorX: return new LocalTileCoord(3 - source.X, source.Y);
                case MicroPatternTransform.MirrorY: return new LocalTileCoord(source.X, 3 - source.Y);
                case MicroPatternTransform.R180: return new LocalTileCoord(3 - source.X, 3 - source.Y);
                default: throw new ArgumentOutOfRangeException(nameof(transform), transform, null);
            }
        }

        private static string InstructionCellKey(
            LocalTileCoord coordinate,
            IEnumerable<MicroPatternInstruction> instructions)
        {
            var details = instructions.Where(value => value.Operation != MicroPatternOperation.NoChange)
                .OrderBy(value => (int)value.Layer)
                .Select(value => value.Layer + "|" + value.Operation + "|" + value.PayloadId);
            return coordinate.X + "," + coordinate.Y + "|" + string.Join(";", details);
        }

        private static string CellKey(MicroPatternPreviewCell cell) =>
            cell.Coordinate.X + "," + cell.Coordinate.Y + "|" + string.Join(";", cell.Details);

        private static string StateKey(MicroPatternPreviewCell cell) =>
            cell.Coordinate.X + "," + cell.Coordinate.Y + "|" +
            string.Join(";", cell.Details) + "|" + cell.IsProtected;

        private static string Pair(MicroPatternDefinition definition, MicroPatternTransform transform) =>
            definition.Id.Value + "|" + transform;

        private static int DataRowCount(string path)
        {
            return File.ReadAllLines(path, Encoding.UTF8).Count(value =>
                !string.IsNullOrEmpty(value)) - 1;
        }

        private static byte[] ReverseDataRows(byte[] source)
        {
            var text = Encoding.UTF8.GetString(source).TrimStart('\uFEFF');
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return Utf8Bom(lines[0] + "\n" + string.Join("\n", lines.Skip(1).Reverse()) + "\n");
        }

        private static string ComputeManifest(string root, IEnumerable<string> paths)
        {
            var noBom = new UTF8Encoding(false);
            var withBom = new UTF8Encoding(true);
            var records = paths.Select(path => new { Path = path, Relative = Relative(root, path) })
                .OrderBy(value => value.Relative, StringComparer.Ordinal)
                .Select(value =>
                {
                    var normalized = File.ReadAllText(value.Path, Encoding.UTF8)
                        .Replace("\r\n", "\n").Replace("\r", "\n");
                    return value.Relative + "\t" + Sha256(
                        withBom.GetPreamble().Concat(noBom.GetBytes(normalized)).ToArray());
                });
            return Sha256(noBom.GetBytes(string.Join("\n", records)));
        }

        private static string Relative(string root, string path) =>
            path.Substring(root.Length + 1).Replace('\\', '/');

        private static string Sha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
        }

        private static byte[] Utf8Bom(string value)
        {
            var noBom = new UTF8Encoding(false);
            var withBom = new UTF8Encoding(true);
            return withBom.GetPreamble().Concat(noBom.GetBytes(value)).ToArray();
        }

        private static string[] GeneratedCsvFiles()
        {
            return Directory.GetFiles(
                    FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated"),
                    "*.csv", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string ImportErrors(MicroPatternCsvImportResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));

        private static string PreviewErrors(MicroPatternPreviewBuildResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
