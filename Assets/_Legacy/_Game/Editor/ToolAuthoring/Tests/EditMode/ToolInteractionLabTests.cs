#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Interaction.Carry;
using StarNight.Tools.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.ToolAuthoring.Tests
{
    public sealed class ToolInteractionLabTests
    {
        [Test]
        public void Workbench_UsesSixApprovedTabs_AndLabStaysOutOfBuildSettings()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    "Tool Definition", "Carry Object", "Reaction Matrix",
                    "Action Timeline", "Throw Preview", "Batch Validation",
                },
                ToolInteractionWorkbenchWindow.RequiredTabLabels);

            Assert.That(
                EditorBuildSettings.scenes.Any(scene => scene.path == ToolInteractionLabBuilder.ScenePath),
                Is.False);
        }

        [Test]
        public void LabScene_ContainsApprovedHierarchy()
        {
            Scene scene = EditorSceneManager.OpenScene(ToolInteractionLabBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject root = scene.GetRootGameObjects().Single(item => item.name == "ToolInteractionLab");
                string[] requiredPaths =
                {
                    "LabBootstrap",
                    "Tool13Approval",
                    "TestGrid/TerrainCollisionTilemap",
                    "TestGrid/OneWayCollisionTilemap",
                    "TestGrid/UnbreakableBoundaryTilemap",
                    "TestGrid/LogicTilemap",
                    "PlayerTestRig",
                    "ToolRack/BombStation",
                    "ToolRack/RopeStation",
                    "ToolRack/PickaxeStation",
                    "ToolRack/ShovelStation",
                    "ToolRack/WateringStation",
                    "ToolRack/PounderStation",
                    "ToolRack/HookStation",
                    "ToolRack/UmbrellaStation",
                    "CarryObjectRack/LightObjects",
                    "CarryObjectRack/MediumObjects",
                    "CarryObjectRack/HeavyObjects",
                    "CarryObjectRack/CriticalObjects",
                    "TestZones/InteractionPriorityZone",
                    "TestZones/DropPlacementZone",
                    "TestZones/ThrowLane",
                    "TestZones/BombChamber",
                    "TestZones/RopeTower",
                    "TestZones/SoilGarden",
                    "TestZones/PoundRoom",
                    "TestZones/HookLane",
                    "TestZones/WindTunnel",
                    "TestZones/PortalCarryZone",
                    "ReactionTargetWall",
                    "Camera",
                    "LabUI",
                };

                foreach (string path in requiredPaths)
                {
                    Assert.That(root.transform.Find(path), Is.Not.Null, path);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GeneratedDefinitions_CoverSixApprovedHandTools()
        {
            HandToolDefinition[] tools = AssetDatabase.FindAssets("t:HandToolDefinition", new[] { ToolInteractionLabBuilder.ToolDataFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<HandToolDefinition>)
                .Where(tool => tool != null)
                .OrderBy(tool => tool.ToolId)
                .ToArray();

            Assert.That(tools, Has.Length.EqualTo(6));
            Assert.That(tools.Single(tool => tool.ToolId == "TOOL_PICKAXE").MaxResource, Is.EqualTo(12));
            Assert.That(tools.Single(tool => tool.ToolId == "TOOL_SHOVEL").MaxResource, Is.EqualTo(10));
            Assert.That(tools.Single(tool => tool.ToolId == "TOOL_WATERING_CAN").MaxResource, Is.EqualTo(6));
            Assert.That(tools.Single(tool => tool.ToolId == "TOOL_POUNDER").MaxResource, Is.EqualTo(8));
            Assert.That(tools.Single(tool => tool.ToolId == "TOOL_HOOK_LAUNCHER").PreviewRangeCells, Is.EqualTo(7));
            Assert.That(tools.Single(tool => tool.ToolId == "TOOL_WIND_UMBRELLA").PreviewAngleDegrees, Is.EqualTo(120f));
        }

        [Test]
        public void GeneratedCarryDefinitions_CoverWeightClassesAndCriticalCarry()
        {
            CarryObjectDefinition[] objects = AssetDatabase.FindAssets("t:CarryObjectDefinition", new[] { ToolInteractionLabBuilder.CarryDataFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CarryObjectDefinition>)
                .Where(item => item != null && item.ObjectId.StartsWith("LAB_CARRY_"))
                .ToArray();

            Assert.That(objects, Has.Length.EqualTo(4));
            Assert.That(objects.Any(item => item.WeightClass == CarryWeightClass.Light && !item.CriticalCarry), Is.True);
            Assert.That(objects.Any(item => item.WeightClass == CarryWeightClass.Medium), Is.True);
            Assert.That(objects.Any(item => item.WeightClass == CarryWeightClass.Heavy && item.PlateWeight == 2), Is.True);
            Assert.That(objects.Single(item => item.CriticalCarry).ObjectId, Is.EqualTo("LAB_CARRY_CRITICAL"));
        }
    }
}

#endif
