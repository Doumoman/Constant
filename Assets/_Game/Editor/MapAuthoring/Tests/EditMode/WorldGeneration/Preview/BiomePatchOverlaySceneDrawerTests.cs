using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.MapAuthoring.Editor.WorldGeneration.Preview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.MapAuthoring.Editor.Tests.WorldGeneration.Preview
{
    public sealed class BiomePatchOverlaySceneDrawerTests
    {
        public static IEnumerable<TestCaseData> ContractCases
        {
            get
            {
                for (var index = 0; index < 28; index++)
                    yield return new TestCaseData(index).SetName("DrawerInspector_ExactContract_" + index.ToString("D2"));
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void DrawerInspector_ExactContract(int caseId)
        {
            var drawer = typeof(BiomePatchOverlaySceneDrawer);
            var method = drawer.GetMethod(
                "DrawBiomePatchOverlay", BindingFlags.Public | BindingFlags.Static);
            var editor = drawer.Assembly.GetType(
                "StarNight.MapAuthoring.Editor.WorldGeneration.Preview.BiomePatchOverlayEditor",
                true);
            var inspector = editor.GetMethod(
                "OnInspectorGUI", BindingFlags.Public | BindingFlags.Instance);

            switch (caseId)
            {
                case 0: Assert.That(drawer.IsAbstract && drawer.IsSealed, Is.True); break;
                case 1: Assert.That(drawer.Namespace, Is.EqualTo("StarNight.MapAuthoring.Editor.WorldGeneration.Preview")); break;
                case 2: Assert.That(method, Is.Not.Null); break;
                case 3: Assert.That(method.ReturnType, Is.EqualTo(typeof(void))); break;
                case 4: Assert.That(method.GetParameters().Length, Is.EqualTo(2)); break;
                case 5: Assert.That(method.GetParameters()[0].ParameterType, Is.EqualTo(typeof(BiomePatchOverlay))); break;
                case 6: Assert.That(method.GetParameters()[1].ParameterType, Is.EqualTo(typeof(GizmoType))); break;
                case 7: Assert.That(method.GetCustomAttributes(typeof(DrawGizmo), false), Has.Length.EqualTo(1)); break;
                case 8:
                    Assert.That(method.GetMethodBody().ExceptionHandlingClauses.Any(value =>
                        value.Flags == ExceptionHandlingClauseOptions.Finally), Is.True);
                    break;
                case 9: Assert.That(drawer.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), Is.Empty); break;
                case 10: Assert.That(editor.IsSealed, Is.True); break;
                case 11: Assert.That(editor.IsPublic, Is.False); break;
                case 12: Assert.That(typeof(UnityEditor.Editor).IsAssignableFrom(editor), Is.True); break;
                case 13: Assert.That(editor.GetCustomAttributes(typeof(CustomEditor), false), Has.Length.EqualTo(1)); break;
                case 14: Assert.That(inspector, Is.Not.Null); break;
                case 15: Assert.That(inspector.GetBaseDefinition().DeclaringType, Is.EqualTo(typeof(UnityEditor.Editor))); break;
                case 16:
                    Assert.That(editor.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Select(value => value.Name), Is.EquivalentTo(new[] { "OnInspectorGUI" }));
                    break;
                case 17: Assert.That(editor.GetMethod("OnSceneGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null); break;
                case 18: Assert.That(editor.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null); break;
                case 19: Assert.That(editor.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), Is.Empty); break;
                case 20: Assert.That(drawer.Assembly.GetName().Name, Is.EqualTo("MapAuthoring.Editor")); break;
                case 21:
                    Assert.That(drawer.Assembly.GetReferencedAssemblies().Select(value => value.Name),
                        Does.Contain("Game.Map.Runtime"));
                    break;
                case 22:
                    Assert.That(drawer.GetMembers(BindingFlags.Public | BindingFlags.Static)
                        .Select(value => value.ToString()).Any(value =>
                            value.Contains("Undo") || value.Contains("SetDirty") || value.Contains("Save")), Is.False);
                    break;
                case 23:
                    Assert.That(typeof(BiomePatchOverlayGui).Assembly.GetReferencedAssemblies()
                        .Select(value => value.Name), Does.Not.Contain("UnityEditor"));
                    break;
                case 24: AssertProgressSceneStructure(); break;
                case 25: AssertProgressSceneStartsEmpty(); break;
                case 26: AssertKnownViableAction(); break;
                case 27: AssertRetryTabsAndClear(); break;
                default: throw new ArgumentOutOfRangeException(nameof(caseId));
            }
        }

        private static void AssertProgressSceneStructure()
        {
            WithProgressScene((scene, root, adapter) =>
            {
                Assert.That(root.tag, Is.EqualTo("EditorOnly"));
                Assert.That(root.GetComponents<Component>().Select(value => value.GetType()), Is.EqualTo(new[]
                {
                    typeof(Transform), typeof(WorldTopologyOverlay), typeof(SiteReservationOverlay),
                    typeof(BiomePatchOverlay), typeof(MapGenerationProgressSceneAdapter)
                }));
                var cameras = scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<Camera>(true)).ToArray();
                Assert.That(cameras, Has.Length.EqualTo(1));
                Assert.That(cameras[0].name, Is.EqualTo("Main Camera"));
                Assert.That(cameras[0].orthographic, Is.True);
                Assert.That(cameras[0].clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
                Assert.That(scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<Canvas>(true)), Is.Empty);
                Assert.That(EditorBuildSettings.scenes.Any(value =>
                    value.path == "Assets/_Game/Scenes/MapGenerationProgressTest.unity"), Is.False);
            });
        }

        private static void AssertProgressSceneStartsEmpty()
        {
            WithProgressScene((scene, root, adapter) =>
            {
                Assert.That(adapter.GenerationCalls, Is.Zero);
                Assert.That(adapter.TopologyOverlay.HasSnapshot, Is.False);
                Assert.That(adapter.SiteOverlay.HasSnapshot, Is.False);
                Assert.That(adapter.BiomeOverlay.HasSnapshot, Is.False);
                Assert.That(scene.isDirty, Is.False);
                var names = typeof(MapGenerationProgressSceneAdapter).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly).Select(value => value.Name).ToArray();
                Assert.That(names, Does.Not.Contain("Awake").And.Not.Contain("OnEnable").And.Not.Contain("Update"));
            });
        }

        private static void AssertKnownViableAction()
        {
            WithProgressScene((scene, root, adapter) =>
            {
                InvokeHarness("LoadKnownViable", adapter);
                Assert.That(adapter.TopologyOverlay.Snapshot.Count, Is.EqualTo(169));
                Assert.That(adapter.SiteOverlay.Snapshot.Count, Is.EqualTo(169));
                Assert.That(adapter.BiomeOverlay.Snapshot.Patches.Count, Is.EqualTo(17));
                Assert.That(adapter.BiomeOverlay.Snapshot.CoreCount, Is.EqualTo(4));
                Assert.That(adapter.BiomeOverlay.Snapshot.SatelliteCount, Is.EqualTo(10));
                Assert.That(adapter.BiomeOverlay.Snapshot.IntrusionCount, Is.EqualTo(3));
                Assert.That(adapter.BiomeOverlay.Snapshot.AssignedCount, Is.EqualTo(165));
                Assert.That(adapter.BiomeOverlay.Snapshot.UnassignedCount, Is.EqualTo(4));
                Assert.That(adapter.GenerationCalls, Is.EqualTo(1));
                Assert.That(adapter.BiomeOverlay.enabled, Is.True);
                Assert.That(scene.isDirty, Is.False);
            });
        }

        private static void AssertRetryTabsAndClear()
        {
            WithProgressScene((scene, root, adapter) =>
            {
                adapter.SeedText = "0x0123456789ABCDF9";
                adapter.AttemptOrdinal = 0;
                InvokeHarness("RunSelected", adapter);
                Assert.That(adapter.Status, Does.Contain("Retry required"));
                Assert.That(adapter.BiomeOverlay.HasSnapshot, Is.False);
                adapter.ShowTopology();
                Assert.That(new[] { adapter.TopologyOverlay.enabled, adapter.SiteOverlay.enabled,
                    adapter.BiomeOverlay.enabled }.Count(value => value), Is.EqualTo(1));
                adapter.ShowSites();
                Assert.That(new[] { adapter.TopologyOverlay.enabled, adapter.SiteOverlay.enabled,
                    adapter.BiomeOverlay.enabled }.Count(value => value), Is.EqualTo(1));
                adapter.Clear();
                Assert.That(adapter.TopologyOverlay.HasSnapshot || adapter.SiteOverlay.HasSnapshot ||
                    adapter.BiomeOverlay.HasSnapshot, Is.False);
            });
        }

        private static void InvokeHarness(string methodName, MapGenerationProgressSceneAdapter adapter)
        {
            var harness = typeof(BiomePatchOverlaySceneDrawer).Assembly.GetType(
                "StarNight.MapAuthoring.Editor.WorldGeneration.Preview.MapGenerationProgressSceneHarness", true);
            harness.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { adapter });
        }

        private static void WithProgressScene(
            Action<Scene, GameObject, MapGenerationProgressSceneAdapter> assertion)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Scene opened = default;
            try
            {
                opened = EditorSceneManager.OpenScene(
                    "Assets/_Game/Scenes/MapGenerationProgressTest.unity", OpenSceneMode.Additive);
                var root = opened.GetRootGameObjects().Single(value =>
                    value.name == "MAP Generation Progress Test");
                assertion(opened, root, root.GetComponent<MapGenerationProgressSceneAdapter>());
            }
            finally
            {
                if (setup.Any(value => value.isLoaded))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else if (opened.IsValid() && opened.isLoaded)
                    EditorSceneManager.CloseScene(opened, true);
            }
        }
    }
}
