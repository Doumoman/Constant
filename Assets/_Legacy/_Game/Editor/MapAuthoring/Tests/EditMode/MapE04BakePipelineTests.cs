#if LEGACY_DISABLED
using System;
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.Map.Placement;
using StarNight.MapAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE04BakePipelineTests
    {
        [Test]
        public void RebakePreservesGuidsAndInvalidSourceCannotOverwriteRuntime()
        {
            string elementId = "COMMON_Test_E04_Bake_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string authoringPath =
                $"Assets/_Game/Editor/MapAuthoring/SourceElements/TestsTemp/{elementId}.asset";
            var definition = MapElementDefinitionPresetFactory.CreatePreset(MapElementLabPreset.Spike1x1);
            definition.ElementId = elementId;
            definition.DisplayName = "MAP-E04 Bake Test";
            var paths = AssetPathUtility.GetMapElementBakePaths(definition);
            DeleteTestAssets(paths);
            AssetPathUtility.EnsureParentFolder(authoringPath);
            AssetDatabase.CreateAsset(definition, authoringPath);
            AssetDatabase.SaveAssets();

            try
            {
                var first = MapElementBakePipeline.Bake(definition);
                Assert.That(first.Success, Is.True, "First bake: " + Describe(first));
                Assert.That(first.Validation.ErrorCount, Is.Zero);

                var sourceGuid = AssetDatabase.AssetPathToGUID(paths.SourcePrefab);
                var runtimeGuid = AssetDatabase.AssetPathToGUID(paths.RuntimePrefab);
                var definitionGuid = AssetDatabase.AssetPathToGUID(paths.Definition);
                var visualGuid = AssetDatabase.AssetPathToGUID(paths.VisualProfile);
                Assert.That(sourceGuid, Is.Not.Empty);
                Assert.That(runtimeGuid, Is.Not.Empty);
                Assert.That(definitionGuid, Is.Not.Empty);
                Assert.That(visualGuid, Is.Not.Empty);

                var bakedDefinition = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(paths.Definition);
                var runtimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths.RuntimePrefab);
                var visualProfile = AssetDatabase.LoadAssetAtPath<MapElementVisualProfileAsset>(paths.VisualProfile);
                Assert.That(bakedDefinition.RuntimePrefab, Is.EqualTo(runtimePrefab));
                Assert.That(bakedDefinition.BakedVisualProfile, Is.EqualTo(visualProfile));
                Assert.That(bakedDefinition.BakeMetadata.SourceHash, Is.EqualTo(first.SourceHash));
                Assert.That(bakedDefinition.BakeMetadata.RuntimePrefabGuid, Is.EqualTo(runtimeGuid));
                Assert.That(runtimePrefab.GetComponent<MapElementInstance>(), Is.Not.Null);
                Assert.That(runtimePrefab.GetComponent<ElementRuntimeId>(), Is.Not.Null);
                Assert.That(runtimePrefab.GetComponent<GridOccupier>(), Is.Not.Null);
                Assert.That(runtimePrefab.GetComponent<ElementStateMachine>(), Is.Not.Null);
                Assert.That(runtimePrefab.GetComponent<MapElementResettable>(), Is.Not.Null);
                Assert.That(runtimePrefab.transform.Find("VisualRoot"), Is.Not.Null);
                Assert.That(runtimePrefab.transform.Find("PhysicsRoot"), Is.Not.Null);
                Assert.That(runtimePrefab.transform.Find("TriggerRoot"), Is.Not.Null);
                Assert.That(runtimePrefab.transform.Find("PathRoot"), Is.Not.Null);
                Assert.That(runtimePrefab.transform.Find("SignalPortRoot"), Is.Not.Null);
                Assert.That(runtimePrefab.transform.Find("AudioRoot"), Is.Not.Null);
                Assert.That(runtimePrefab.transform.Find("DebugRoot"), Is.Not.Null);

                definition = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(authoringPath);
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.ElementId, Is.EqualTo(elementId));
                definition.DisplayName = "MAP-E04 Bake Test Updated";
                EditorUtility.SetDirty(definition);
                var second = MapElementBakePipeline.Bake(definition);
                Assert.That(second.Success, Is.True, "Second bake: " + Describe(second));
                Assert.That(AssetDatabase.AssetPathToGUID(paths.SourcePrefab), Is.EqualTo(sourceGuid));
                Assert.That(AssetDatabase.AssetPathToGUID(paths.RuntimePrefab), Is.EqualTo(runtimeGuid));
                Assert.That(AssetDatabase.AssetPathToGUID(paths.Definition), Is.EqualTo(definitionGuid));
                Assert.That(AssetDatabase.AssetPathToGUID(paths.VisualProfile), Is.EqualTo(visualGuid));
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<MapElementDefinition>(paths.Definition).DisplayName,
                    Is.EqualTo("MAP-E04 Bake Test Updated"));

                var runtimeHashBeforeInvalidBake = BakeHashUtility.ComputeAssetFileHash(paths.RuntimePrefab);
                definition = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(authoringPath);
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.ElementId, Is.EqualTo(elementId));
                definition.Footprint.PivotCell = new Vector2Int(99, 99);
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();
                var rejected = MapElementBakePipeline.Bake(definition);
                Assert.That(rejected.Success, Is.False);
                Assert.That(rejected.Validation.ErrorCount, Is.GreaterThan(0));
                Assert.That(
                    BakeHashUtility.ComputeAssetFileHash(paths.RuntimePrefab),
                    Is.EqualTo(runtimeHashBeforeInvalidBake),
                    "Validation failure must not overwrite the existing Runtime Prefab.");
                Assert.That(AssetDatabase.AssetPathToGUID(paths.RuntimePrefab), Is.EqualTo(runtimeGuid));
            }
            finally
            {
                AssetDatabase.DeleteAsset(authoringPath);
                DeleteTestAssets(paths);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static void DeleteTestAssets(MapElementBakePaths paths)
        {
            AssetDatabase.DeleteAsset(paths.SourcePrefab);
            AssetDatabase.DeleteAsset(paths.RuntimePrefab);
            AssetDatabase.DeleteAsset(paths.Definition);
            AssetDatabase.DeleteAsset(paths.VisualProfile);
        }

        private static string Describe(MapElementBakeResult result)
        {
            string issues = result.Validation == null
                ? string.Empty
                : string.Join(" | ", result.Validation.Issues.Select(issue => issue.ToString()));
            return string.IsNullOrEmpty(issues) ? result.Message : result.Message + " | " + issues;
        }
    }
}

#endif
