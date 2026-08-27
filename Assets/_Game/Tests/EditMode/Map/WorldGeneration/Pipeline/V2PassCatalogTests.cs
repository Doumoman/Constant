using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.Tests.EditMode.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Pipeline;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Pipeline
{
    [Category("MAP09_01")]
    public sealed class V2PassCatalogTests
    {
        private static readonly V2WorldGenerationPassId[] ExpectedPassIds =
        {
            V2WorldGenerationPassId.Pacing,
            V2WorldGenerationPassId.SpecialRegionReservation,
            V2WorldGenerationPassId.TerrainClusterReservation,
            V2WorldGenerationPassId.RouteSpine,
            V2WorldGenerationPassId.TraversalEnvelope,
            V2WorldGenerationPassId.MicroPattern,
            V2WorldGenerationPassId.TerrainCleanup,
            V2WorldGenerationPassId.ActivityEventOverlay,
            V2WorldGenerationPassId.TileValidation,
            V2WorldGenerationPassId.MicroChunkSlice,
        };

        private static readonly V2WorldGenerationArtifactId[] ExpectedOutputs =
        {
            V2WorldGenerationArtifactId.PacingPlan,
            V2WorldGenerationArtifactId.SpecialRegionReservationPlan,
            V2WorldGenerationArtifactId.TerrainClusterPlacementPlan,
            V2WorldGenerationArtifactId.RouteSpinePlan,
            V2WorldGenerationArtifactId.TraversalEnvelopePlan,
            V2WorldGenerationArtifactId.PatternApplicationPlan,
            V2WorldGenerationArtifactId.CleanTerrainCanvas,
            V2WorldGenerationArtifactId.ActivityEventPlacementPlan,
            V2WorldGenerationArtifactId.ValidatedSectorCanvas,
            V2WorldGenerationArtifactId.GeneratedMicroChunkSlices,
        };

        [Test]
        public void CatalogContainsExactTenPassesInStableNumericOrder()
        {
            Assert.That(V2PassCatalog.Entries, Has.Count.EqualTo(10));
            Assert.That(V2PassCatalog.Entries.Select(value => value.PassId), Is.EqualTo(ExpectedPassIds));
            Assert.That(V2PassCatalog.Entries.Select(value => value.Order),
                Is.EqualTo(Enumerable.Range(1, 10).Select(value => value * 10)));
        }

        [Test]
        public void CatalogContainsExactPrimaryOutputArtifacts()
        {
            Assert.That(
                V2PassCatalog.Entries.SelectMany(value => value.OutputArtifactIds),
                Is.EqualTo(ExpectedOutputs));
        }

        [Test]
        public void CatalogHasNoDuplicateIdsOrdersOrOutputs()
        {
            Assert.That(V2PassCatalog.Entries.Select(value => value.PassId).Distinct().Count(), Is.EqualTo(10));
            Assert.That(V2PassCatalog.Entries.Select(value => value.Order).Distinct().Count(), Is.EqualTo(10));
            Assert.That(V2PassCatalog.Entries.SelectMany(value => value.OutputArtifactIds).Distinct().Count(),
                Is.EqualTo(10));
        }

        [Test]
        public void CatalogValidatorAcceptsTheExplicitBuiltInChain()
        {
            var result = V2PassCatalogValidator.Validate(V2PassCatalog.Entries);
            Assert.That(result.IsValid, Is.True, string.Join("\n", result.Issues));
            Assert.That(result.Issues, Is.Empty);
        }

        [Test]
        public void CatalogValidatorReportsDuplicatePassIdsWithoutThrowing()
        {
            var duplicate = V2PassCatalog.Entries.ToArray();
            duplicate[1] = new V2PassContract(
                duplicate[0].PassId,
                duplicate[1].Order,
                duplicate[1].InputArtifactIds,
                duplicate[1].OutputArtifactIds,
                duplicate[1].FailureOwner,
                duplicate[1].FailurePolicy,
                duplicate[1].RetryScope,
                duplicate[1].RetryEscalation,
                duplicate[1].RngStream,
                duplicate[1].DescriptionId,
                duplicate[1].PreservesValidatedCanvasOnFailure);

            V2CatalogValidationResult result = null;
            Assert.DoesNotThrow(() => result = V2PassCatalogValidator.Validate(duplicate));
            Assert.That(result.Issues.Select(value => value.Code),
                Does.Contain(V2CatalogIssueCode.DuplicatePassId));
        }

        [Test]
        public void EveryInputIsApprovedBaselineOrAnEarlierOutput()
        {
            var available = new HashSet<V2WorldGenerationArtifactId>
            {
                V2WorldGenerationArtifactId.ApprovedMapBaseline,
            };
            foreach (var entry in V2PassCatalog.Entries)
            {
                Assert.That(entry.InputArtifactIds.All(available.Contains), Is.True, entry.PassId.ToString());
                foreach (var output in entry.OutputArtifactIds) available.Add(output);
            }
        }

        [Test]
        public void EveryIntermediateOutputIsConsumedExactlyByTheNextPass()
        {
            for (var index = 0; index < V2PassCatalog.Entries.Count - 1; index++)
            {
                Assert.That(
                    V2PassCatalog.Entries[index + 1].InputArtifactIds,
                    Is.EqualTo(V2PassCatalog.Entries[index].OutputArtifactIds),
                    V2PassCatalog.Entries[index].PassId.ToString());
            }
        }

        [Test]
        public void RequiredReservationAndPlanningOrderIsExact()
        {
            AssertOrder(V2WorldGenerationPassId.SpecialRegionReservation,
                V2WorldGenerationPassId.TerrainClusterReservation);
            AssertOrder(V2WorldGenerationPassId.RouteSpine,
                V2WorldGenerationPassId.TraversalEnvelope,
                V2WorldGenerationPassId.MicroPattern);
            AssertOrder(V2WorldGenerationPassId.TerrainCleanup,
                V2WorldGenerationPassId.ActivityEventOverlay);
            AssertOrder(V2WorldGenerationPassId.TileValidation,
                V2WorldGenerationPassId.MicroChunkSlice);
        }

        [Test]
        public void SliceIsFinalAndConsumesOnlyValidatedCanvas()
        {
            var slice = V2PassCatalog.Entries.Last();
            Assert.That(slice.PassId, Is.EqualTo(V2WorldGenerationPassId.MicroChunkSlice));
            Assert.That(slice.InputArtifactIds,
                Is.EqualTo(new[] { V2WorldGenerationArtifactId.ValidatedSectorCanvas }));
            Assert.That(slice.OutputArtifactIds,
                Is.EqualTo(new[] { V2WorldGenerationArtifactId.GeneratedMicroChunkSlices }));
        }

        [Test]
        public void CatalogAndEntryCollectionsRejectExternalMutation()
        {
            Assert.Throws<NotSupportedException>(() =>
                ((IList<V2PassContract>)V2PassCatalog.Entries).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<V2WorldGenerationArtifactId>)V2PassCatalog.Entries[0].InputArtifactIds).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<V2WorldGenerationArtifactId>)V2PassCatalog.Entries[0].OutputArtifactIds).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<V2RetryScope>)V2PassCatalog.Entries[8].RetryEscalation).Clear());
        }

        [Test]
        public void ContractPropertiesAreReadOnlyAndContractTypeIsSealed()
        {
            Assert.That(typeof(V2PassContract).IsSealed, Is.True);
            Assert.That(typeof(V2PassContract).GetProperties().Where(value => value.SetMethod != null), Is.Empty);
            Assert.That(typeof(V2InfrastructureFailureRule).GetProperties()
                .Where(value => value.SetMethod != null), Is.Empty);
        }

        [Test]
        public void StableDigestRepeatsExactly()
        {
            Assert.That(V2PassCatalog.StableDigest, Is.EqualTo(Map09ApprovedBaseline.CatalogDigest));
            Assert.That(V2PassCatalog.ComputeStableDigest(V2PassCatalog.Entries),
                Is.EqualTo(Map09ApprovedBaseline.CatalogDigest));
        }

        [Test]
        public void StableDigestSortsByStablePassIdRatherThanEnumerationOrder()
        {
            Assert.That(V2PassCatalog.ComputeStableDigest(V2PassCatalog.Entries.Reverse()),
                Is.EqualTo(V2PassCatalog.StableDigest));
        }

        [Test]
        public void DescriptionTextDoesNotAffectStableDigest()
        {
            var renamed = V2PassCatalog.Entries
                .Select((value, index) => value.WithDescriptionId("DISPLAY_TEXT_CHANGED_" + index))
                .ToArray();
            Assert.That(V2PassCatalog.ComputeStableDigest(renamed), Is.EqualTo(V2PassCatalog.StableDigest));
        }

        [Test]
        public void InfrastructureFailuresAreImmediateWithoutFallback()
        {
            Assert.That(V2PassCatalog.InfrastructureFailureRules.Select(value => value.Kind),
                Is.EqualTo(new[]
                {
                    V2InfrastructureFailureKind.Configuration,
                    V2InfrastructureFailureKind.Schema,
                    V2InfrastructureFailureKind.Baseline,
                }));
            Assert.That(V2PassCatalog.InfrastructureFailureRules.All(value =>
                value.Policy == V2FailurePolicy.ImmediateFailure &&
                value.RetryScope == V2RetryScope.None &&
                !value.AllowsSilentFallback), Is.True);
            Assert.That(V2PassCatalog.Entries.All(value => !value.AllowsSilentFallback), Is.True);
        }

        [Test]
        public void CandidateFailureMetadataDistinguishesPatternClusterAndFootprintScopes()
        {
            AssertScope(V2WorldGenerationPassId.MicroPattern, V2RetryScope.Pattern);
            AssertScope(V2WorldGenerationPassId.TerrainClusterReservation, V2RetryScope.Cluster);
            AssertScope(V2WorldGenerationPassId.SpecialRegionReservation, V2RetryScope.Footprint);
        }

        [Test]
        public void FinalValidationEscalatesOnlyPatternThenClusterThenFootprint()
        {
            var validation = Find(V2WorldGenerationPassId.TileValidation);
            Assert.That(validation.FailurePolicy, Is.EqualTo(V2FailurePolicy.OrderedEscalation));
            Assert.That(validation.RetryEscalation,
                Is.EqualTo(new[] { V2RetryScope.Pattern, V2RetryScope.Cluster, V2RetryScope.Footprint }));
        }

        [Test]
        public void SliceFailureIsImmediateAndPreservesValidatedCanvas()
        {
            var slice = Find(V2WorldGenerationPassId.MicroChunkSlice);
            Assert.That(slice.FailurePolicy, Is.EqualTo(V2FailurePolicy.ImmediateFailure));
            Assert.That(slice.RetryScope, Is.EqualTo(V2RetryScope.None));
            Assert.That(slice.RetryEscalation, Is.Empty);
            Assert.That(slice.PreservesValidatedCanvasOnFailure, Is.True);
        }

        [Test]
        public void RngOwnershipIsDeclaredWithoutAnExecutionDependency()
        {
            Assert.That(V2PassCatalog.Entries.Select(value => value.RngStream), Is.EqualTo(new[]
            {
                V2RngStreamId.Pacing,
                V2RngStreamId.SpecialRegionReservation,
                V2RngStreamId.TerrainClusterReservation,
                V2RngStreamId.RouteSpine,
                V2RngStreamId.None,
                V2RngStreamId.MicroPattern,
                V2RngStreamId.None,
                V2RngStreamId.ActivityEventOverlay,
                V2RngStreamId.None,
                V2RngStreamId.None,
            }));
        }

        [Test]
        public void BaselineFixtureFreezesApprovedDimensionsAndGenerationPhilosophy()
        {
            Assert.That(Map09ApprovedBaseline.MicroPatternSize, Is.EqualTo(new[] { 4, 4 }));
            Assert.That(Map09ApprovedBaseline.MicroChunkSize, Is.EqualTo(new[] { 12, 8 }));
            Assert.That(Map09ApprovedBaseline.SectorCanvasSize, Is.EqualTo(new[] { 48, 32 }));
            Assert.That(Map09ApprovedBaseline.GenerationPhilosophy,
                Is.EqualTo("Cluster-first>Pattern-second>Chunk-slice-last"));
        }

        [Test]
        public void BaselineFixtureFreezesApprovedRegressionCounts()
        {
            Assert.That(Map09ApprovedBaseline.Map08Focused, Is.EqualTo(840));
            Assert.That(Map09ApprovedBaseline.Map08Required, Is.EqualTo(9220));
            Assert.That(Map09ApprovedBaseline.Map07Required, Is.EqualTo(5422));
            Assert.That(Map09ApprovedBaseline.Map06Required, Is.EqualTo(2746));
            Assert.That(Map09ApprovedBaseline.Map05Required, Is.EqualTo(1959));
            Assert.That(Map09ApprovedBaseline.RequiredDistinct, Is.EqualTo(19347));
        }

        [Test]
        public void ApprovedMap08BoundaryEvidenceRecomputesExactly()
        {
            var evidence = MoonpalaceBoundaryPhaseExitFixture.GetOrCreate().Evidence;
            Assert.That(evidence.Report.Accepted, Is.True);
            Assert.That(evidence.Report.PairReportCount, Is.EqualTo(Map09ApprovedBaseline.BoundaryPairCount));
            Assert.That(evidence.Report.CandidateCountTotal, Is.EqualTo(Map09ApprovedBaseline.BoundaryCandidates));
            Assert.That(evidence.Report.MicrochunkCountTotal, Is.EqualTo(Map09ApprovedBaseline.BoundaryMicrochunks));
            Assert.That(evidence.Report.TileRowCountTotal, Is.EqualTo(Map09ApprovedBaseline.BoundaryTileRows));
            Assert.That(evidence.Report.SocketRowCountTotal, Is.EqualTo(Map09ApprovedBaseline.BoundarySocketRows));
            Assert.That(evidence.Report.StableDigest, Is.EqualTo(Map09ApprovedBaseline.BoundaryDigest));
            Assert.That(evidence.Candidates.Count * 2,
                Is.EqualTo(Map09ApprovedBaseline.DirectionalProjections));
            Assert.That(evidence.Candidates.Count(value => value.ToolRequirement == "NONE"),
                Is.EqualTo(Map09ApprovedBaseline.MandatoryNoToolCandidates));
        }

        [Test]
        public void AuthoringCsvAndMatchingMetaCountsAndManifestRecomputeExactly()
        {
            var root = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var csvFiles = Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories);
            var metaFiles = Directory.GetFiles(root, "*.csv.meta", SearchOption.AllDirectories);
            Assert.That(csvFiles, Has.Length.EqualTo(50));
            Assert.That(metaFiles, Has.Length.EqualTo(50));
            Assert.That(ComputeAuthoringManifest(root, csvFiles),
                Is.EqualTo(Map09ApprovedBaseline.AuthoringManifest));
        }

        [Test]
        public void GeneratedCsvInventoryRemainsEmpty()
        {
            var root = FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated");
            Assert.That(Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void NewProductionScopeContainsNoForbiddenSymbolsOrPrematureGraphs()
        {
            var root = FullPath("Assets/_Game/Map/Runtime/WorldGeneration/Pipeline");
            var forbidden = new[]
            {
                "StageMapGenerator",
                "GridWorld",
                "RoomTemplate",
                "RoomGridTransform",
                "TileMutationService",
                "SectorRecipeResolver",
                "UnityEditor",
                "TraversalGraph",
                "MechanismGraph",
                "ProgressionGraph",
            };
            var text = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(File.ReadAllText));
            Assert.That(forbidden.Where(text.Contains), Is.Empty);
        }

        [Test]
        public void ExistingWorldGenerationRootDoesNotExecuteTheV2Catalog()
        {
            var rootSource = File.ReadAllText(FullPath(
                "Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRoot.cs"));
            Assert.That(rootSource, Does.Not.Contain("V2PassCatalog"));
            Assert.That(rootSource, Does.Not.Contain("V2PassContract"));
        }

        private static void AssertOrder(params V2WorldGenerationPassId[] passIds)
        {
            var orders = passIds.Select(value => Find(value).Order).ToArray();
            Assert.That(orders, Is.Ordered.Ascending);
        }

        private static void AssertScope(V2WorldGenerationPassId passId, V2RetryScope scope)
        {
            var entry = Find(passId);
            Assert.That(entry.FailurePolicy, Is.EqualTo(V2FailurePolicy.ReselectWithinScope));
            Assert.That(entry.RetryScope, Is.EqualTo(scope));
        }

        private static V2PassContract Find(V2WorldGenerationPassId passId)
        {
            return V2PassCatalog.Entries.Single(value => value.PassId == passId);
        }

        private static string ComputeAuthoringManifest(string root, IEnumerable<string> paths)
        {
            var utf8WithoutBom = new UTF8Encoding(false);
            var utf8WithBom = new UTF8Encoding(true);
            var records = paths
                .Select(path => new
                {
                    Path = path,
                    Relative = path.Substring(root.Length + 1).Replace('\\', '/'),
                })
                .OrderBy(value => value.Relative, StringComparer.Ordinal)
                .Select(value =>
                {
                    var normalized = File.ReadAllText(value.Path, Encoding.UTF8)
                        .Replace("\r\n", "\n")
                        .Replace("\r", "\n");
                    var body = utf8WithoutBom.GetBytes(normalized);
                    var contentBytes = utf8WithBom.GetPreamble().Concat(body).ToArray();
                    return value.Relative + "\t" + Sha256(contentBytes);
                });
            return Sha256(utf8WithoutBom.GetBytes(string.Join("\n", records)));
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }

    internal static class Map09ApprovedBaseline
    {
        public const int Map08Focused = 840;
        public const int Map08Required = 9220;
        public const int Map07Required = 5422;
        public const int Map06Required = 2746;
        public const int Map05Required = 1959;
        public const int RequiredDistinct = 19347;
        public const int BoundaryPairCount = 6;
        public const int BoundaryCandidates = 31;
        public const int BoundaryMicrochunks = 31;
        public const int BoundaryTileRows = 2976;
        public const int BoundarySocketRows = 62;
        public const int DirectionalProjections = 62;
        public const int MandatoryNoToolCandidates = 31;
        public const string BoundaryDigest =
            "f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68";
        public const string AuthoringManifest =
            "f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb";
        public const string CatalogDigest =
            "90a2614f9a95c29f1546f350190010524672d4b4aa2d1ad1dfe7dbd431be50d5";
        public const string GenerationPhilosophy = "Cluster-first>Pattern-second>Chunk-slice-last";

        public static readonly int[] MicroPatternSize = { 4, 4 };
        public static readonly int[] MicroChunkSize = { 12, 8 };
        public static readonly int[] SectorCanvasSize = { 48, 32 };
    }
}
