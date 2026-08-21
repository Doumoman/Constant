using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.Tests.WorldGeneration.Preview
{
    public sealed class SiteReservationOverlaySceneDrawerTests
    {
        private static readonly Type DrawerType = typeof(
            StarNight.MapAuthoring.Editor.WorldGeneration.Preview.SiteReservationOverlaySceneDrawer);

        private static Type InspectorType => DrawerType.Assembly.GetType(
            "StarNight.MapAuthoring.Editor.WorldGeneration.Preview.SiteReservationOverlayEditor",
            true);

        private static MethodInfo GizmoMethod => DrawerType.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Single(method => method.GetCustomAttributesData()
                .Any(attribute => attribute.AttributeType == typeof(DrawGizmo)));

        private static MethodInfo InspectorMethod => InspectorType.GetMethod(
            nameof(UnityEditor.Editor.OnInspectorGUI),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        [Test]
        public void SceneDrawer_IsPublicStaticWithOneExactGizmoMethod()
        {
            Assert.That(DrawerType.IsPublic, Is.True);
            Assert.That(DrawerType.IsAbstract && DrawerType.IsSealed, Is.True);
            Assert.That(GizmoMethod.ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(GizmoMethod.GetParameters().Select(parameter => parameter.ParameterType),
                Is.EqualTo(new[] { typeof(SiteReservationOverlay), typeof(GizmoType) }));
        }

        [Test]
        public void SceneDrawer_UsesExactGizmoMask()
        {
            var attribute = GizmoMethod.GetCustomAttributesData()
                .Single(candidate => candidate.AttributeType == typeof(DrawGizmo));
            var mask = (GizmoType)(int)attribute.ConstructorArguments[0].Value;

            Assert.That(mask,
                Is.EqualTo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected));
        }

        [Test]
        public void CustomInspector_IsInternalSealedAndTargetsExactOverlay()
        {
            var attribute = InspectorType.GetCustomAttributesData()
                .Single(candidate => candidate.AttributeType == typeof(CustomEditor));

            Assert.That(InspectorType.IsNotPublic, Is.True);
            Assert.That(InspectorType.IsSealed, Is.True);
            Assert.That(typeof(UnityEditor.Editor).IsAssignableFrom(InspectorType), Is.True);
            Assert.That(attribute.ConstructorArguments[0].Value,
                Is.EqualTo(typeof(SiteReservationOverlay)));
        }

        [Test]
        public void SceneAndGameSurfacesCallTheSameRuntimeDrawMethod()
        {
            var draw = typeof(SiteReservationOverlayGui).GetMethod(
                nameof(SiteReservationOverlayGui.Draw),
                BindingFlags.Public | BindingFlags.Static);
            var game = typeof(SiteReservationOverlay).GetMethod(
                "OnGUI",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(ContainsCall(game, draw), Is.True);
            Assert.That(ContainsCall(GizmoMethod, draw), Is.True);
        }

        [Test]
        public void SceneDrawer_AlwaysPairsBeginAndEndGuiCalls()
        {
            var calls = CalledMethods(GizmoMethod).ToArray();

            Assert.That(calls.Count(method => method.DeclaringType == typeof(Handles) &&
                                              method.Name == nameof(Handles.BeginGUI)), Is.EqualTo(1));
            Assert.That(calls.Count(method => method.DeclaringType == typeof(Handles) &&
                                              method.Name == nameof(Handles.EndGUI)), Is.EqualTo(1));
            Assert.That(GizmoMethod.GetMethodBody().ExceptionHandlingClauses
                .Any(clause => clause.Flags == ExceptionHandlingClauseOptions.Finally), Is.True);
        }

        [Test]
        public void CustomInspector_ContainsExactClearButtonAndNoPreviewButton()
        {
            Assert.That(ContainsStringLiteral(InspectorMethod, "Clear"), Is.True);
            Assert.That(ContainsStringLiteral(InspectorMethod, "Preview"), Is.False);
            Assert.That(ContainsStringLiteral(InspectorMethod, "Generate"), Is.False);
            Assert.That(ContainsStringLiteral(InspectorMethod, "Run"), Is.False);
        }

        [TestCase("ClearSnapshot")]
        [TestCase("RepaintAll")]
        [TestCase("QueuePlayerLoopUpdate")]
        public void CustomInspector_ClearPathCallsEachRequiredAction(string methodName)
        {
            Assert.That(CalledMethods(InspectorMethod).Count(method => method.Name == methodName),
                Is.EqualTo(1));
        }

        [TestCase("SetDirty")]
        [TestCase("RecordObject")]
        [TestCase("RegisterCompleteObjectUndo")]
        [TestCase("SaveScene")]
        [TestCase("MarkSceneDirty")]
        [TestCase("FindObjectOfType")]
        [TestCase("FindAnyObjectByType")]
        [TestCase("CreatePrimitive")]
        [TestCase("SetActiveScene")]
        [TestCase("Focus")]
        [TestCase("Repaint")]
        [TestCase("ValidateAndPublish")]
        [TestCase("Search")]
        [TestCase("Check")]
        [TestCase("Reserve")]
        public void CustomInspector_DoesNotCallForbiddenMutationOrGenerationApi(string methodName)
        {
            Assert.That(CalledMethods(InspectorMethod).Any(method => method.Name == methodName), Is.False);
        }

        [Test]
        public void EditorTypesHaveNoStaticMutableStateOrInitializeOnLoad()
        {
            Assert.That(DrawerType.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), Is.Empty);
            Assert.That(InspectorType.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), Is.Empty);
            Assert.That(DrawerType.GetCustomAttributesData()
                .Any(attribute => attribute.AttributeType.Name.Contains("InitializeOnLoad")), Is.False);
            Assert.That(InspectorType.GetCustomAttributesData()
                .Any(attribute => attribute.AttributeType.Name.Contains("InitializeOnLoad")), Is.False);
        }

        [Test]
        public void SceneDrawerDoesNotSubscribeToContinuousEditorCallbacks()
        {
            var calls = CalledMethods(GizmoMethod).Concat(CalledMethods(InspectorMethod)).ToArray();
            Assert.That(calls.Any(method => method.Name.StartsWith("add_", StringComparison.Ordinal)), Is.False);
            Assert.That(calls.Any(method => method.Name.StartsWith("remove_", StringComparison.Ordinal)), Is.False);
            Assert.That(calls.Any(method => method.Name == "get_duringSceneGui"), Is.False);
            Assert.That(calls.Any(method => method.Name == "get_update"), Is.False);
        }

        [Test]
        public void CustomInspectorUsesReadOnlyLabelsAndOneButtonCall()
        {
            var calls = CalledMethods(InspectorMethod).ToArray();
            Assert.That(calls.Any(method => method.DeclaringType == typeof(EditorGUILayout) &&
                                            method.Name == nameof(EditorGUILayout.LabelField)), Is.True);
            Assert.That(calls.Count(method => method.DeclaringType == typeof(GUILayout) &&
                                              method.Name == nameof(GUILayout.Button)), Is.EqualTo(1));
            Assert.That(calls.Any(method => method.Name.Contains("SerializedProperty")), Is.False);
        }

        [Test]
        public void EditorProductionTypeStaysInEditorAssembly()
        {
            Assert.That(DrawerType.Assembly.GetName().Name, Is.EqualTo("MapAuthoring.Editor"));
            Assert.That(InspectorType.Assembly, Is.SameAs(DrawerType.Assembly));
        }

        private static bool ContainsCall(MethodInfo caller, MethodInfo target)
        {
            return CalledMethods(caller).Any(method =>
                method.DeclaringType == target.DeclaringType && method.Name == target.Name &&
                method.GetParameters().Select(parameter => parameter.ParameterType)
                    .SequenceEqual(target.GetParameters().Select(parameter => parameter.ParameterType)));
        }

        private static IEnumerable<MethodBase> CalledMethods(MethodInfo caller)
        {
            var body = caller.GetMethodBody();
            if (body == null) yield break;
            var bytes = body.GetILAsByteArray();
            for (var index = 0; index <= bytes.Length - 5; index++)
            {
                if (bytes[index] != 0x28 && bytes[index] != 0x6f) continue;
                MethodBase resolved;
                try
                {
                    resolved = caller.Module.ResolveMethod(BitConverter.ToInt32(bytes, index + 1));
                }
                catch (ArgumentException)
                {
                    continue;
                }
                if (resolved != null) yield return resolved;
            }
        }

        private static bool ContainsStringLiteral(MethodInfo method, string expected)
        {
            var bytes = method.GetMethodBody().GetILAsByteArray();
            for (var index = 0; index <= bytes.Length - 5; index++)
            {
                if (bytes[index] != 0x72) continue;
                try
                {
                    if (string.Equals(
                        method.Module.ResolveString(BitConverter.ToInt32(bytes, index + 1)),
                        expected,
                        StringComparison.Ordinal)) return true;
                }
                catch (ArgumentException)
                {
                }
            }
            return false;
        }
    }
}
