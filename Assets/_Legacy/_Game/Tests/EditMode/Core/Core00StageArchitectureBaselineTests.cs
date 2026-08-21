#if LEGACY_DISABLED
using System.IO;
using NUnit.Framework;
using StarNight.Core.State;

namespace StarNight.Core.Tests
{
    public sealed class Core00StageArchitectureBaselineTests
    {
        [TearDown]
        public void TearDown()
        {
            FeatureFlag.ResetNewStageArchitecture();
        }

        [Test]
        public void Core00ContractsMatchTheNewStageArchitectureBaseline()
        {
            CollectionAssert.AreEqual(
                new[] { "Grid", "Player", "Inventory", "Tools", "Rooms", "Camera", "Streaming", "Secrets", "Maru" },
                StageArchitecturePaths.RequiredCoreFolders);
            CollectionAssert.AreEqual(
                new[]
                {
                    "microchunk_pattern_library.csv",
                    "room_role_recipe.csv",
                    "room_graph_template.csv",
                    "room_placement_profile.csv",
                    "special_content_pool.csv",
                    "secret_dimension_library.csv"
                },
                StageArchitecturePaths.ApprovedStageCsvFileNames);

            foreach (string folder in StageArchitecturePaths.RequiredCoreFolders)
            {
                Assert.That(Directory.Exists(Path.Combine(StageArchitecturePaths.CoreRoot, folder)), Is.True, folder);
            }

            Assert.That(Directory.Exists(StageArchitecturePaths.GlobalDataRoot), Is.True);
            Assert.That(Directory.Exists(StageArchitecturePaths.StageDataRoot), Is.True);
            Assert.That(Directory.Exists(StageArchitecturePaths.LegacyDataRoot), Is.True);
            Assert.That(Directory.Exists(StageArchitecturePaths.CoreValidationEditorRoot), Is.True);
            Assert.That(Directory.Exists(StageArchitecturePaths.StageAuthoringScenesRoot), Is.True);
            Assert.That(Directory.Exists(StageArchitecturePaths.EditorTestsRoot), Is.True);

            string itemMaster = Path.Combine(StageArchitecturePaths.GlobalDataRoot, "item_master.csv");
            Assert.That(File.ReadAllText(itemMaster).Trim(), Is.EqualTo("id,name,rank,price,price_inc,note"));
            Assert.That(StageArchitecturePaths.CanRuntimeReadDataPath(itemMaster), Is.True);
            Assert.That(StageArchitecturePaths.CanRuntimeReadDataPath(StageArchitecturePaths.StageDataRoot), Is.True);
            Assert.That(StageArchitecturePaths.CanRuntimeReadDataPath(StageArchitecturePaths.LegacyDataRoot), Is.False);
            Assert.That(StageArchitecturePaths.CanRuntimeReadDataPath(StageArchitecturePaths.LegacyDataRoot + "/old.csv"), Is.False);

            FeatureFlag.ResetNewStageArchitecture();
            Assert.That(FeatureFlag.NewStageArchitecture, Is.False);
            FeatureFlag.SetNewStageArchitecture(true);
            Assert.That(FeatureFlag.NewStageArchitecture, Is.True);
        }
    }
}

#endif
