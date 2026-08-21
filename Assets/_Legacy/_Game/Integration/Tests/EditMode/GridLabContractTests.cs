#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Integration.Editor;
using StarNight.Narrative;
using StarNight.Stage.Data;
using StarNight.Stage.Flow;
using StarNight.Stage.Lab;
using StarNight.UI.HUD;
using StarNight.UI.Menus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.Integration.Tests
{
    public sealed class GridLabContractTests
    {
        [Test]
        public void GridLabSceneContainsFourRoomCommonSystemHarness()
        {
            StageDefinition definition = AssetDatabase.LoadAssetAtPath<StageDefinition>(Core12GridLabBuilder.GridLabStagePath);
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.sceneName, Is.EqualTo("99_GridLab"));
            Assert.That(definition.regionId, Is.EqualTo("integration_lab"));
            Assert.That(definition.minRooms, Is.EqualTo(4));
            Assert.That(definition.maxRooms, Is.EqualTo(4));
            Assert.That(definition.maruSpawnTime, Is.EqualTo(GridLabSoakMonitor.RequiredDurationSeconds));

            var scene = EditorSceneManager.OpenScene(Core12GridLabBuilder.GridLabScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("GridLabRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<Core04TwoRoomLab>(), Is.Not.Null);
            Assert.That(root.GetComponent<Core04TwoRoomLab>().PrototypeRoomCount, Is.EqualTo(4));
            Assert.That(root.GetComponent<StageSceneBootstrap>()?.Definition, Is.SameAs(definition));
            Assert.That(root.GetComponent<Core12GridLab>(), Is.Not.Null);
            Assert.That(root.GetComponent<GridLabSoakMonitor>(), Is.Not.Null);
            Assert.That(root.GetComponent<HUDController>(), Is.Not.Null);
            Assert.That(root.GetComponent<NarrativeSystemController>(), Is.Not.Null);
            Assert.That(root.GetComponent<PauseMenuController>(), Is.Not.Null);
            Assert.That(scene.isLoaded, Is.True);
        }

        [Test]
        public void MemoryContractRepresentsThirtyMinutesAndBoundedGrowth()
        {
            Assert.That(GridLabSoakMonitor.RequiredDurationSeconds, Is.EqualTo(1800f));
            Assert.That(GridLabSoakMonitor.WarmupSeconds, Is.GreaterThanOrEqualTo(60f));
            Assert.That(GridLabSoakMonitor.MaximumManagedGrowthBytes, Is.LessThanOrEqualTo(32L * 1024L * 1024L));
            Assert.That(Core12GridLab.AcceleratedSoakSeconds, Is.EqualTo(1800));
        }
    }
}

#endif
