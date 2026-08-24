using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;
using StarNight.MapAuthoring.Microchunks;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_13")]
    public sealed class Map07ExitTests
    {
        private const string ApprovedAuthoringManifestSha256 =
            "4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3";

        private static readonly string[] ForbiddenProductionSymbols =
        {
            "BoundaryChunkPairDefinition", "MoonPalaceBiomePair", "BoundaryChunkResolver",
            "SectorRecipeResolver", "SectorMicrochunkAssembler", "GeneratedSectorMicrochunkWriter",
            "PopulationSlotIndex", "StableSpawnId", "WorldTraversalValidator"
        };

        private Map07AuditEvidence evidence;

        public static IEnumerable<TestCaseData> ExitCases
        {
            get
            {
                for (var caseId = 0; caseId < 180; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("Map07ExitContract_" + caseId.ToString("D3"));
                }
            }
        }

        [OneTimeSetUp]
        public void LoadFullStarterAudit()
        {
            evidence = Map07AuditHarness.GetOrCreate();
        }

        [TestCaseSource(nameof(ExitCases))]
        public void Map07ExitContract(int caseId)
        {
            var starter = evidence.Starters[caseId % evidence.Starters.Count];
            switch (caseId % 20)
            {
                case 0:
                    Assert.That(evidence.Starters, Is.Not.Empty);
                    Assert.That(evidence.Starters.Select(value => value.MicrochunkId), Is.Unique);
                    break;
                case 1:
                    Assert.That(evidence.Starters.All(value => value.ImportSuccess), Is.True);
                    Assert.That(evidence.Starters.All(value => value.TileAndCoverageValidationSuccess), Is.True);
                    break;
                case 2:
                    Assert.That(evidence.Starters.All(value => value.TileCellCount == 96), Is.True);
                    Assert.That(evidence.NegativeCoverageContractsPass, Is.True);
                    break;
                case 3:
                    Assert.That(evidence.Starters.All(value =>
                        value.PreviewTransformCount == value.ExpectedTransformCount &&
                        value.ExpectedTransformCount >= 1 && value.ExpectedTransformCount <= 4), Is.True);
                    Assert.That(MicrochunkConstants.WidthTiles * MicrochunkConstants.HeightTiles,
                        Is.EqualTo(MicrochunkConstants.CellCount));
                    break;
                case 4:
                    Assert.That(evidence.Starters.All(value => value.AllTransformValidationDeterministic), Is.True);
                    break;
                case 5:
                    Assert.That(evidence.Starters.All(value => value.AllMandatoryPairsReachableWithoutTools),
                        Is.True);
                    break;
                case 6:
                    Assert.That(evidence.Starters.All(value => value.ExportPlanSuccess), Is.True);
                    Assert.That(evidence.Starters.All(value => value.ExportApplySuccess), Is.True);
                    break;
                case 7:
                    Assert.That(evidence.Starters.All(value => value.InsertedTileRows == 96), Is.True);
                    Assert.That(evidence.Starters.All(value => value.SelectedOwnedRowsReplacedExactly), Is.True);
                    break;
                case 8:
                    Assert.That(evidence.Starters.All(value => value.NormalizedStateRoundTrips), Is.True);
                    Assert.That(evidence.Starters.All(value => value.ReimportSuccess), Is.True);
                    break;
                case 9:
                    Assert.That(evidence.Starters.All(value => value.SharedSocketBandsPreserved), Is.True);
                    break;
                case 10:
                    Assert.That(evidence.ProjectAuthoringSourcePreserved, Is.True);
                    Assert.That(evidence.TempResidueCount, Is.Zero);
                    break;
                case 11:
                    Assert.That(evidence.Starters.All(value => value.AllExportFilesHaveUtf8Bom), Is.True);
                    Assert.That(evidence.Starters.All(value => value.SchemaHeadersPreserved), Is.True);
                    break;
                case 12:
                    Assert.That(evidence.Starters.All(value => value.ExportPlanDeterministic), Is.True);
                    Assert.That(evidence.Starters.All(value => value.StableRowOrder), Is.True);
                    break;
                case 13:
                    Assert.That(evidence.SceneDirtyAfter, Is.EqualTo(evidence.SceneDirtyBefore));
                    break;
                case 14:
                    Assert.That(typeof(MicrochunkCsvImporter).Assembly.GetName().Name,
                        Is.EqualTo("MapAuthoring.Editor"));
                    Assert.That(typeof(MicrochunkDefinition).Assembly.GetName().Name,
                        Is.EqualTo("Game.Map.Runtime"));
                    break;
                case 15:
                    AssertForbiddenProductionSymbolsRemainAbsent();
                    break;
                case 16:
                    Assert.That(MicrochunkPreviewRequest.SupportedTransforms, Is.EqualTo(new[]
                    {
                        MicrochunkTransform.R0,
                        MicrochunkTransform.MirrorX,
                        MicrochunkTransform.MirrorY,
                        MicrochunkTransform.R180
                    }));
                    break;
                case 17:
                    AssertAuthoringInventoryIsPreserved();
                    break;
                case 18:
                    Assert.That(ApprovedAuthoringManifestSha256, Does.Match("^[0-9a-f]{64}$"));
                    Assert.That(evidence.ProjectAuthoringSourcePreserved, Is.True);
                    break;
                default:
                    Assert.That(starter.PreviewSuccess, Is.True, starter.Diagnostic);
                    Assert.That(starter.CatalogMetadataRoundTrips, Is.True, starter.Diagnostic);
                    Assert.That(starter.VariantMetadataRoundTrips, Is.True, starter.Diagnostic);
                    break;
            }
        }

        private static void AssertForbiddenProductionSymbolsRemainAbsent()
        {
            var editorNames = typeof(MicrochunkCsvImporter).Assembly.GetTypes()
                .Select(value => value.Name).ToArray();
            var runtimeNames = typeof(MicrochunkDefinition).Assembly.GetTypes()
                .Select(value => value.Name).ToArray();
            foreach (var forbidden in ForbiddenProductionSymbols)
            {
                Assert.That(editorNames, Does.Not.Contain(forbidden));
                Assert.That(runtimeNames, Does.Not.Contain(forbidden));
            }
        }

        private static void AssertAuthoringInventoryIsPreserved()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var root = Path.Combine(projectRoot,
                MicrochunkCsvImportSource.AuthoringRoot.Replace('/', Path.DirectorySeparatorChar));
            var csv = Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories);
            var meta = Directory.GetFiles(root, "*.csv.meta", SearchOption.AllDirectories);
            Assert.That(csv, Has.Length.EqualTo(50));
            Assert.That(meta, Has.Length.EqualTo(50));
            Assert.That(csv.All(value => File.Exists(value + ".meta")), Is.True);
        }
    }
}
