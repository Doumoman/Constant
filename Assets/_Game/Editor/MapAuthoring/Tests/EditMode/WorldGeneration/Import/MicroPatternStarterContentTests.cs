using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.Import
{
    [TestFixture]
    [Category("MAP10_06")]
    public sealed class MicroPatternStarterContentTests
    {
        private const string ExpectedFullManifest =
            "4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851";
        private const string ExpectedLegacyManifest =
            "f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb";

        private static readonly string[] ExpectedCatalogRows =
        {
            "MP_CRATER_BOWL,500,MoonCrater,R0|MIRROR_Y,REJECT_CANDIDATE",
            "MP_CRATER_BROKEN_SLOPE,250,MoonCrater,R0|MIRROR_X|MIRROR_Y|R180,REJECT_CANDIDATE",
            "MP_CRATER_DUST_PATCH,1000,MoonCrater,R0,FORCE_NO_CHANGE",
            "MP_CRATER_GRIP_RIDGE,250,MoonCrater,R0|MIRROR_X|MIRROR_Y|R180,FORCE_NO_CHANGE",
            "MP_CRATER_METEOR_CUE,1000,MoonCrater,R0,FORCE_NO_CHANGE",
            "MP_CRATER_ROCK_SHELF,250,MoonCrater,R0|MIRROR_X|MIRROR_Y|R180,REJECT_CANDIDATE",
            "MP_DOUGH_BOUNCE_CUP,1000,MoonDough,R0,REJECT_CANDIDATE",
            "MP_DOUGH_BOUNCE_STRIP,1000,MoonDough,R0,FORCE_NO_CHANGE",
            "MP_DOUGH_FERMENT_PATCH,1000,MoonDough,R0,FORCE_NO_CHANGE",
            "MP_DOUGH_RECOVERY_PAD,1000,MoonDough,R0,FORCE_NO_CHANGE",
            "MP_DOUGH_SOFT_POCKET,250,MoonDough,R0|MIRROR_X|MIRROR_Y|R180,REJECT_CANDIDATE",
            "MP_DOUGH_STICKY_SHELF,250,MoonDough,R0|MIRROR_X|MIRROR_Y|R180,REJECT_CANDIDATE",
            "MP_MILL_BEAM_GRIP,500,AbandonedMill,R0|MIRROR_Y,FORCE_NO_CHANGE",
            "MP_MILL_BEAM_OVERHANG,250,AbandonedMill,R0|MIRROR_X|MIRROR_Y|R180,REJECT_CANDIDATE",
            "MP_MILL_BROKEN_PILLAR,250,AbandonedMill,R0|MIRROR_X|MIRROR_Y|R180,REJECT_CANDIDATE",
            "MP_MILL_GEAR_SOCKET,1000,AbandonedMill,R0,FORCE_NO_CHANGE",
            "MP_MILL_ORTHOGONAL_CARVE,250,AbandonedMill,R0|MIRROR_X|MIRROR_Y|R180,REJECT_CANDIDATE",
            "MP_MILL_RUST_PATCH,500,AbandonedMill,R0|MIRROR_X,FORCE_NO_CHANGE",
            "MP_ROOT_ARCH,500,CassiaRoot,R0|MIRROR_Y,REJECT_CANDIDATE",
            "MP_ROOT_CLIMB_VINES,500,CassiaRoot,R0|MIRROR_X,FORCE_NO_CHANGE",
            "MP_ROOT_HOLLOW_POCKET,250,CassiaRoot,R0|MIRROR_X|MIRROR_Y|R180,REJECT_CANDIDATE",
            "MP_ROOT_SAP_PATCH,1000,CassiaRoot,R0,FORCE_NO_CHANGE",
            "MP_ROOT_SPROUT_MARK,1000,CassiaRoot,R0,FORCE_NO_CHANGE",
            "MP_ROOT_VERTICAL_TUNNEL,1000,CassiaRoot,R0,REJECT_CANDIDATE",
        };

        private static readonly Dictionary<string, string[]> ExpectedGeometry =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "MP_CRATER_BROKEN_SLOPE", new[] { "...+", "..++", ".+++", "++++" } },
                { "MP_CRATER_BOWL", new[] { ".--.", "----", ".--.", "...." } },
                { "MP_CRATER_ROCK_SHELF", new[] { "....", ".+++", "++++", "...." } },
                { "MP_ROOT_ARCH", new[] { "++++", "+--+", "+--+", "...." } },
                { "MP_ROOT_VERTICAL_TUNNEL", new[] { ".--.", ".--.", ".--.", ".--." } },
                { "MP_ROOT_HOLLOW_POCKET", new[] { "....", ".---", ".--+", ".+++" } },
                { "MP_MILL_BROKEN_PILLAR", new[] { ".++.", ".++.", ".+..", ".++." } },
                { "MP_MILL_BEAM_OVERHANG", new[] { "++++", "...+", "...+", "...." } },
                { "MP_MILL_ORTHOGONAL_CARVE", new[] { "---.", "..-.", "..-.", "...." } },
                { "MP_DOUGH_BOUNCE_CUP", new[] { "....", "+--+", "+--+", ".++." } },
                { "MP_DOUGH_SOFT_POCKET", new[] { "....", ".---", ".---", "..-." } },
                { "MP_DOUGH_STICKY_SHELF", new[] { "....", "....", "++++", "..++" } },
            };

        private static readonly string[] ExpectedPayloads =
        {
            "AFF_BOUNCE", "AFF_CLIMB", "AFF_GRAB", "AFF_GRIP",
            "HZ_FERMENT_BUBBLE", "HZ_METEOR_EDGE", "HZ_SHARP_DEBRIS", "HZ_STICKY_SAP",
            "MARK_CRATER_DETAIL", "MARK_GEAR_SOCKET", "MARK_METEOR_CUE", "MARK_RECOVERY_PAD",
            "MARK_ROOT_SPROUT", "MAT_CASSIA_SAP", "MAT_DOUGH_FERMENT", "MAT_DOUGH_SOFT",
            "MAT_MILL_IRON", "MAT_MILL_RUST", "MAT_MOON_DUST", "MAT_ROOT_FIBER",
            "SURF_CRATER_ROUGH", "SURF_DOUGH_SOFT", "SURF_MILL_BEAM", "SURF_ROOT_BARK",
        };

        [Test]
        public void PhysicalFilesHaveExactBomLfHeadersRowsAndCanonicalOrder()
        {
            var catalog = ReadPhysical(MicroPatternCsvImporterV2.CatalogProjectRelativePath,
                MicroPatternCsvImporterV2.CatalogExpectedHeader);
            var cells = ReadPhysical(MicroPatternCsvImporterV2.CellsProjectRelativePath,
                MicroPatternCsvImporterV2.CellsExpectedHeader);

            Assert.That(catalog.Rows, Is.EqualTo(ExpectedCatalogRows));
            Assert.That(cells.Rows, Has.Length.EqualTo(453));
            Assert.That(catalog.Rows.Select(value => value.Split(',')[0]),
                Is.EqualTo(catalog.Rows.Select(value => value.Split(',')[0])
                    .OrderBy(value => value, StringComparer.Ordinal)));

            var actualCellKeys = cells.Rows.Select(CellOrderKey).ToArray();
            Assert.That(actualCellKeys, Is.EqualTo(actualCellKeys
                .OrderBy(value => value.PatternId, StringComparer.Ordinal)
                .ThenBy(value => value.Y)
                .ThenBy(value => value.X)
                .ThenBy(value => value.Layer)));
        }

        [Test]
        public void ImportPublishesExactImmutableCatalogAndAssignments()
        {
            var result = Import();
            Assert.That(result.Success, Is.True, Errors(result));
            Assert.That(result.Published, Is.True);
            Assert.That(result.IsHeaderOnly, Is.False);
            Assert.That(result.Catalog.Count, Is.EqualTo(24));
            Assert.That(result.StableDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternDefinition>)result.Catalog.Definitions).Add(null));

            foreach (var row in ExpectedCatalogRows)
            {
                var fields = row.Split(',');
                var definition = result.Catalog.Definitions.Single(value => value.Id.Value == fields[0]);
                Assert.That(definition.Weight, Is.EqualTo(int.Parse(fields[1])), fields[0]);
                Assert.That(definition.AllowedBiomes.Single().CanonicalId, Is.EqualTo(fields[2]), fields[0]);
                Assert.That(string.Join("|", definition.AllowedTransforms.Select(TransformToken)),
                    Is.EqualTo(fields[3]), fields[0]);
                Assert.That(PolicyToken(definition.ProtectedPolicy), Is.EqualTo(fields[4]), fields[0]);
                Assert.That(definition.Weight * definition.AllowedTransforms.Count,
                    Is.EqualTo(1000), fields[0]);
                Assert.That(definition.Cells, Has.Count.EqualTo(16), fields[0]);
            }

            Assert.That(result.Catalog.Definitions
                .GroupBy(value => value.AllowedBiomes.Single().CanonicalId)
                .ToDictionary(value => value.Key, value => value.Count(), StringComparer.Ordinal)
                .Values, Is.All.EqualTo(6));
            Assert.That(result.Catalog.Definitions.Count(value => ExpectedGeometry.ContainsKey(value.Id.Value)),
                Is.EqualTo(12));
            Assert.That(result.Catalog.Definitions.Count(value =>
                value.ProtectedPolicy == MicroPatternProtectedPolicy.RejectCandidate), Is.EqualTo(12));
            Assert.That(result.Catalog.Definitions.Count(value =>
                value.ProtectedPolicy == MicroPatternProtectedPolicy.ForceNoChange), Is.EqualTo(12));
        }

        [Test]
        public void GeometryDefinitionsMatchDetailedGoldenTemplatesAndTotals()
        {
            var definitions = Import().Catalog.Definitions;
            foreach (var pair in ExpectedGeometry)
            {
                var definition = definitions.Single(value => value.Id.Value == pair.Key);
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        var instruction = definition.Cells.Single(value =>
                                value.Coordinate.X == x && value.Coordinate.Y == y)
                            .Instructions.Single(value => value.Layer == MicroPatternLayer.Geometry);
                        Assert.That(GeometrySymbol(instruction.Operation),
                            Is.EqualTo(pair.Value[3 - y][x]), pair.Key + " (" + x + "," + y + ")");
                        Assert.That(instruction.PayloadId, Is.Empty, pair.Key);
                    }
                }
            }

            var geometry = definitions.SelectMany(value => value.Cells)
                .SelectMany(value => value.Instructions)
                .Where(value => value.Layer == MicroPatternLayer.Geometry)
                .ToArray();
            Assert.That(geometry, Has.Length.EqualTo(384));
            Assert.That(geometry.Count(value => value.Operation == MicroPatternOperation.AddSolid),
                Is.EqualTo(54));
            Assert.That(geometry.Count(value => value.Operation == MicroPatternOperation.CarveAir),
                Is.EqualTo(41));
            Assert.That(geometry.Count(value => value.Operation == MicroPatternOperation.NoChange),
                Is.EqualTo(289));
        }

        [Test]
        public void AdditionalLayerMatrixMatchesGoldenCoordinatesAndPayloads()
        {
            var definitions = Import().Catalog.Definitions;
            var actual = definitions.SelectMany(definition => definition.Cells.SelectMany(cell =>
                    cell.Instructions.Where(instruction =>
                            instruction.Layer != MicroPatternLayer.Geometry &&
                            instruction.Operation != MicroPatternOperation.NoChange)
                        .Select(instruction => ExtraKey(
                            definition.Id.Value, cell.Coordinate.X, cell.Coordinate.Y,
                            instruction.Layer, instruction.Operation, instruction.PayloadId))))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var expected = ExpectedExtras().OrderBy(value => value, StringComparer.Ordinal).ToArray();

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Has.Length.EqualTo(69));
            Assert.That(LayerCount(actual, MicroPatternLayer.Surface), Is.EqualTo(16));
            Assert.That(LayerCount(actual, MicroPatternLayer.Affordance), Is.EqualTo(10));
            Assert.That(LayerCount(actual, MicroPatternLayer.Material), Is.EqualTo(26));
            Assert.That(LayerCount(actual, MicroPatternLayer.Hazard), Is.EqualTo(8));
            Assert.That(LayerCount(actual, MicroPatternLayer.Marker), Is.EqualTo(9));
            Assert.That(actual.Select(value => value.Split('|')[5]).Distinct().OrderBy(value => value),
                Is.EqualTo(ExpectedPayloads));
            Assert.That(actual.Select(value => value.Split('|')[5]), Is.All.Not.Empty);
        }

        [Test]
        public void SignaturesAreTwelveDistinctNonZeroAndTwelveExplicitZero()
        {
            var definitions = Import().Catalog.Definitions;
            var geometry = definitions.Where(value => ExpectedGeometry.ContainsKey(value.Id.Value))
                .Select(Signature).ToArray();
            var nonGeometry = definitions.Where(value => !ExpectedGeometry.ContainsKey(value.Id.Value))
                .Select(Signature).ToArray();

            Assert.That(geometry, Has.Length.EqualTo(12));
            Assert.That(geometry.All(value => value.AddSolidMask != 0 || value.CarveAirMask != 0), Is.True);
            Assert.That(geometry.Select(value => value.StableDigest).Distinct().ToArray(), Has.Length.EqualTo(12));
            Assert.That(nonGeometry, Has.Length.EqualTo(12));
            Assert.That(nonGeometry.All(value => value.AddSolidMask == 0 && value.CarveAirMask == 0), Is.True);
            Assert.That(nonGeometry.Select(value => value.StableDigest).Distinct().ToArray(), Has.Length.EqualTo(1));
        }

        [Test]
        public void CatalogDigestIsIndependentOfPhysicalRowOrder()
        {
            var importer = new MicroPatternCsvImporterV2();
            var catalog = File.ReadAllBytes(FullPath(MicroPatternCsvImporterV2.CatalogProjectRelativePath));
            var cells = File.ReadAllBytes(FullPath(MicroPatternCsvImporterV2.CellsProjectRelativePath));
            var canonical = importer.ParseBytes(catalog, cells);
            var reordered = importer.ParseBytes(ReverseDataRows(catalog), ReverseDataRows(cells));

            Assert.That(canonical.Success, Is.True, Errors(canonical));
            Assert.That(reordered.Success, Is.True, Errors(reordered));
            Assert.That(reordered.StableDigest, Is.EqualTo(canonical.StableDigest));
        }

        [Test]
        public void AuthoringBoundaryMetasAndGeneratedDirectoryRemainExact()
        {
            var root = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var allCsv = Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories);
            var allMeta = Directory.GetFiles(root, "*.csv.meta", SearchOption.AllDirectories);
            var legacyCsv = allCsv.Where(path => !Relative(root, path).StartsWith(
                "MicroPattern/", StringComparison.Ordinal)).ToArray();
            var legacyMeta = allMeta.Where(path => !Relative(root, path).StartsWith(
                "MicroPattern/", StringComparison.Ordinal)).ToArray();

            Assert.That(allCsv, Has.Length.EqualTo(52));
            Assert.That(allMeta, Has.Length.EqualTo(52));
            Assert.That(legacyCsv, Has.Length.EqualTo(50));
            Assert.That(legacyMeta, Has.Length.EqualTo(50));
            Assert.That(ComputeManifest(root, legacyCsv), Is.EqualTo(ExpectedLegacyManifest));
            Assert.That(ComputeManifest(root, allCsv), Is.EqualTo(ExpectedFullManifest));
            Assert.That(Sha256(File.ReadAllBytes(FullPath(
                    MicroPatternCsvImporterV2.CatalogProjectRelativePath + ".meta"))),
                Is.EqualTo("c3008c5d8286936f12293f4680e46380df236bdaf29a9585fcda5935e9b0ca06"));
            Assert.That(Sha256(File.ReadAllBytes(FullPath(
                    MicroPatternCsvImporterV2.CellsProjectRelativePath + ".meta"))),
                Is.EqualTo("9ff73bf9a52af439554158b143c72e0a97726740c1227de4289f2d65e5f1617b"));

            var generated = FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated");
            Assert.That(Directory.GetFiles(generated, "*.csv", SearchOption.AllDirectories), Is.Empty);
        }

        private static MicroPatternCsvImportResult Import()
        {
            var result = new MicroPatternCsvImporterV2().Import();
            Assert.That(result.Success, Is.True, Errors(result));
            return result;
        }

        private static MicroPatternSilhouetteSignature Signature(MicroPatternDefinition definition)
        {
            var transformed = MicroPatternTransformer.Transform(definition, MicroPatternTransform.R0);
            Assert.That(transformed.Success, Is.True, definition.Id.Value);
            var application = MicroPatternApplicationPlanner.Plan(
                transformed.Pattern,
                new MicroPatternPlacement(new LocalTileCoord(0, 0)),
                Array.Empty<MicroPatternProtectedCell>());
            Assert.That(application.Success, Is.True, definition.Id.Value);
            var signature = MicroPatternSilhouetteSignatureBuilder.Build(application.Plan);
            Assert.That(signature.Success, Is.True, definition.Id.Value);
            return signature.Signature;
        }

        private static IEnumerable<string> ExpectedExtras()
        {
            var rows = new List<string>();
            Add(rows, "MP_CRATER_GRIP_RIDGE", MicroPatternLayer.Surface,
                MicroPatternOperation.SetSurface, "SURF_CRATER_ROUGH", "0,1", "1,1", "2,1", "3,1");
            Add(rows, "MP_CRATER_GRIP_RIDGE", MicroPatternLayer.Affordance,
                MicroPatternOperation.SetAffordance, "AFF_GRIP", "0,1", "2,1");
            Add(rows, "MP_ROOT_CLIMB_VINES", MicroPatternLayer.Surface,
                MicroPatternOperation.SetSurface, "SURF_ROOT_BARK", "1,0", "1,1", "1,2", "1,3");
            Add(rows, "MP_ROOT_CLIMB_VINES", MicroPatternLayer.Affordance,
                MicroPatternOperation.SetAffordance, "AFF_CLIMB", "1,0", "1,1", "1,2", "1,3");
            Add(rows, "MP_MILL_BEAM_GRIP", MicroPatternLayer.Surface,
                MicroPatternOperation.SetSurface, "SURF_MILL_BEAM", "0,2", "1,2", "2,2", "3,2");
            Add(rows, "MP_MILL_BEAM_GRIP", MicroPatternLayer.Affordance,
                MicroPatternOperation.SetAffordance, "AFF_GRAB", "0,2", "3,2");
            Add(rows, "MP_DOUGH_BOUNCE_STRIP", MicroPatternLayer.Surface,
                MicroPatternOperation.SetSurface, "SURF_DOUGH_SOFT", "0,0", "1,0", "2,0", "3,0");
            Add(rows, "MP_DOUGH_BOUNCE_STRIP", MicroPatternLayer.Affordance,
                MicroPatternOperation.SetAffordance, "AFF_BOUNCE", "1,0", "2,0");

            Add(rows, "MP_CRATER_DUST_PATCH", MicroPatternLayer.Material,
                MicroPatternOperation.SetMaterial, "MAT_MOON_DUST", "1,1", "2,1", "1,2", "2,2");
            Add(rows, "MP_CRATER_DUST_PATCH", MicroPatternLayer.Marker,
                MicroPatternOperation.SetMarker, "MARK_CRATER_DETAIL", "2,2");
            Add(rows, "MP_CRATER_METEOR_CUE", MicroPatternLayer.Hazard,
                MicroPatternOperation.SetHazard, "HZ_METEOR_EDGE", "1,0", "2,0");
            Add(rows, "MP_CRATER_METEOR_CUE", MicroPatternLayer.Marker,
                MicroPatternOperation.SetMarker, "MARK_METEOR_CUE", "1,1", "2,1");
            Add(rows, "MP_ROOT_SAP_PATCH", MicroPatternLayer.Material,
                MicroPatternOperation.SetMaterial, "MAT_CASSIA_SAP", "1,1", "2,1", "1,2", "2,2");
            Add(rows, "MP_ROOT_SAP_PATCH", MicroPatternLayer.Hazard,
                MicroPatternOperation.SetHazard, "HZ_STICKY_SAP", "1,1", "2,1");
            Add(rows, "MP_ROOT_SPROUT_MARK", MicroPatternLayer.Material,
                MicroPatternOperation.SetMaterial, "MAT_ROOT_FIBER", "1,0", "2,0");
            Add(rows, "MP_ROOT_SPROUT_MARK", MicroPatternLayer.Marker,
                MicroPatternOperation.SetMarker, "MARK_ROOT_SPROUT", "1,1", "2,1");
            Add(rows, "MP_MILL_RUST_PATCH", MicroPatternLayer.Material,
                MicroPatternOperation.SetMaterial, "MAT_MILL_RUST", "0,0", "1,1", "2,2", "3,3");
            Add(rows, "MP_MILL_RUST_PATCH", MicroPatternLayer.Hazard,
                MicroPatternOperation.SetHazard, "HZ_SHARP_DEBRIS", "1,0", "2,0");
            Add(rows, "MP_MILL_GEAR_SOCKET", MicroPatternLayer.Material,
                MicroPatternOperation.SetMaterial, "MAT_MILL_IRON", "1,1", "2,1", "1,2", "2,2");
            Add(rows, "MP_MILL_GEAR_SOCKET", MicroPatternLayer.Marker,
                MicroPatternOperation.SetMarker, "MARK_GEAR_SOCKET", "1,1", "2,2");
            Add(rows, "MP_DOUGH_FERMENT_PATCH", MicroPatternLayer.Material,
                MicroPatternOperation.SetMaterial, "MAT_DOUGH_FERMENT", "1,1", "2,1", "1,2", "2,2");
            Add(rows, "MP_DOUGH_FERMENT_PATCH", MicroPatternLayer.Hazard,
                MicroPatternOperation.SetHazard, "HZ_FERMENT_BUBBLE", "1,2", "2,2");
            Add(rows, "MP_DOUGH_RECOVERY_PAD", MicroPatternLayer.Material,
                MicroPatternOperation.SetMaterial, "MAT_DOUGH_SOFT", "0,0", "1,0", "2,0", "3,0");
            Add(rows, "MP_DOUGH_RECOVERY_PAD", MicroPatternLayer.Marker,
                MicroPatternOperation.SetMarker, "MARK_RECOVERY_PAD", "1,0", "2,0");
            return rows;
        }

        private static void Add(
            ICollection<string> rows,
            string patternId,
            MicroPatternLayer layer,
            MicroPatternOperation operation,
            string payload,
            params string[] coordinates)
        {
            foreach (var coordinate in coordinates)
            {
                var parts = coordinate.Split(',');
                rows.Add(ExtraKey(patternId, int.Parse(parts[0]), int.Parse(parts[1]),
                    layer, operation, payload));
            }
        }

        private static string ExtraKey(
            string patternId,
            int x,
            int y,
            MicroPatternLayer layer,
            MicroPatternOperation operation,
            string payload)
        {
            return patternId + "|" + x + "," + y + "|" + layer + "|" + operation + "|write|" + payload;
        }

        private static int LayerCount(IEnumerable<string> rows, MicroPatternLayer layer)
        {
            return rows.Count(value => value.Split('|')[2] == layer.ToString());
        }

        private static PhysicalCsv ReadPhysical(string projectRelativePath, string expectedHeader)
        {
            var bytes = File.ReadAllBytes(FullPath(projectRelativePath));
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }), projectRelativePath);
            Assert.That(bytes, Has.None.EqualTo((byte)'\r'), projectRelativePath);
            Assert.That(bytes.Last(), Is.EqualTo((byte)'\n'), projectRelativePath);
            var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
            Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal), Is.False, projectRelativePath);
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            Assert.That(lines[0], Is.EqualTo(expectedHeader), projectRelativePath);
            Assert.That(lines.Last(), Is.Empty, projectRelativePath);
            return new PhysicalCsv(lines[0], lines.Skip(1).Take(lines.Length - 2).ToArray());
        }

        private static CellKey CellOrderKey(string row)
        {
            var fields = row.Split(',');
            return new CellKey(fields[0], int.Parse(fields[1]), int.Parse(fields[2]), LayerRank(fields[4]));
        }

        private static int LayerRank(string layer)
        {
            switch (layer)
            {
                case "GEOMETRY": return 1;
                case "SURFACE": return 2;
                case "AFFORDANCE": return 3;
                case "MATERIAL": return 4;
                case "HAZARD": return 5;
                case "MARKER": return 6;
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }
        }

        private static char GeometrySymbol(MicroPatternOperation operation)
        {
            switch (operation)
            {
                case MicroPatternOperation.NoChange: return '.';
                case MicroPatternOperation.AddSolid: return '+';
                case MicroPatternOperation.CarveAir: return '-';
                default: throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }

        private static string TransformToken(MicroPatternTransform transform)
        {
            switch (transform)
            {
                case MicroPatternTransform.R0: return "R0";
                case MicroPatternTransform.MirrorX: return "MIRROR_X";
                case MicroPatternTransform.MirrorY: return "MIRROR_Y";
                case MicroPatternTransform.R180: return "R180";
                default: throw new ArgumentOutOfRangeException(nameof(transform), transform, null);
            }
        }

        private static string PolicyToken(MicroPatternProtectedPolicy policy)
        {
            return policy == MicroPatternProtectedPolicy.RejectCandidate
                ? "REJECT_CANDIDATE"
                : "FORCE_NO_CHANGE";
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

        private static string Relative(string root, string path)
        {
            return path.Substring(root.Length + 1).Replace('\\', '/');
        }

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

        private static string Errors(MicroPatternCsvImportResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class PhysicalCsv
        {
            public PhysicalCsv(string header, string[] rows)
            {
                Header = header;
                Rows = rows;
            }

            public string Header { get; }
            public string[] Rows { get; }
        }

        private readonly struct CellKey : IEquatable<CellKey>
        {
            public CellKey(string patternId, int x, int y, int layer)
            {
                PatternId = patternId;
                X = x;
                Y = y;
                Layer = layer;
            }

            public string PatternId { get; }
            public int X { get; }
            public int Y { get; }
            public int Layer { get; }

            public bool Equals(CellKey other)
            {
                return string.Equals(PatternId, other.PatternId, StringComparison.Ordinal) &&
                       X == other.X && Y == other.Y && Layer == other.Layer;
            }

            public override bool Equals(object obj)
            {
                return obj is CellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = StringComparer.Ordinal.GetHashCode(PatternId);
                    hash = (hash * 397) ^ X;
                    hash = (hash * 397) ^ Y;
                    return (hash * 397) ^ Layer;
                }
            }
        }
    }
}
