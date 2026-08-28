using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.TerrainClusters
{
    [TestFixture]
    [Category("MAP11_07")]
    public sealed class TerrainClusterStarterContentTests
    {
        [Test]
        public void EmptyAuthoringInputRejectsAtomicPublication()
        {
            var result = TerrainClusterAuthoringValidation.Build(
                Array.Empty<TerrainClusterAuthoringRow>());
            Assert.That(result.Success, Is.False);
            Assert.That(result.Published, Is.False);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterAuthoringErrorCode.MissingTable));
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterAuthoringErrorCode.AtomicPublishRejected));
        }

        [Test]
        public void ParsedRowsOwnImmutableOrdinalFieldSnapshots()
        {
            var mutable = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "cluster_id", "TC_SAMPLE" },
                { "pacing_role", "QUIET" },
            };
            var row = new TerrainClusterAuthoringRow(
                "TerrainCluster/terrain_cluster_catalog_v2.csv", 2, mutable);
            mutable["cluster_id"] = "TC_MUTATED";
            mutable.Add("extra", "value");

            Assert.That(row.TablePath,
                Is.EqualTo("TerrainCluster/terrain_cluster_catalog_v2.csv"));
            Assert.That(row.RecordNumber, Is.EqualTo(2));
            Assert.That(row.Get("cluster_id"), Is.EqualTo("TC_SAMPLE"));
            Assert.That(row.Get("extra"), Is.Empty);
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, string>)row.Fields).Add("forbidden", "value"));
        }

        [Test]
        public void RuntimeAuthoringBoundaryHasNoFilesystemRngUnityEditorOrPlacementOwnership()
        {
            var root = FullPath(
                "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Authoring");
            var source = string.Join("\n", Directory.GetFiles(root, "*.cs")
                .OrderBy(value => value, StringComparer.Ordinal).Select(File.ReadAllText));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "System.IO", "StageMapGenerator", "GridWorld",
                         "RoomTemplate", "RoomGridTransform", "TileMutationService",
                         "SectorRecipeResolver", "System.Random", "UnityEngine.Random",
                         "DeterministicRngStreamFactory", "Time.deltaTime", "Tilemap",
                     })
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
