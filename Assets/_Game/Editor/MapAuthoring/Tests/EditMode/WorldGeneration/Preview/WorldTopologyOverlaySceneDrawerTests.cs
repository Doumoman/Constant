using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Preview
{
    public sealed class WorldTopologyOverlaySceneDrawerTests
    {
        private static readonly Type DrawerType = typeof(
            StarNight.MapAuthoring.Editor.WorldGeneration.Preview.WorldTopologyOverlaySceneDrawer);

        private static Type InspectorType => DrawerType.Assembly.GetType(
            "StarNight.MapAuthoring.Editor.WorldGeneration.Preview.WorldTopologyOverlayEditor",
            true);

        [TestCase("0", 0UL)]
        [TestCase("1", 1UL)]
        [TestCase("4660", 4660UL)]
        [TestCase("18446744073709551615", ulong.MaxValue)]
        public void InspectorSeedParser_AcceptsCanonicalValues(string text, ulong expected)
        {
            var arguments = new object[] { text, 0UL };

            var succeeded = (bool)GetSeedParser().Invoke(null, arguments);

            Assert.That(succeeded, Is.True);
            Assert.That((ulong)arguments[1], Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("00")]
        [TestCase("01")]
        [TestCase("+1")]
        [TestCase("-1")]
        [TestCase(" 1")]
        [TestCase("1 ")]
        [TestCase("1,000")]
        [TestCase("18446744073709551616")]
        [TestCase("１")]
        public void InspectorSeedParser_RejectsNonCanonicalValues(string text)
        {
            var arguments = new object[] { text, 123UL };

            var succeeded = (bool)GetSeedParser().Invoke(null, arguments);

            Assert.That(succeeded, Is.False);
            Assert.That((ulong)arguments[1], Is.EqualTo(0UL));
        }

        [Test]
        public void SceneDrawer_IsPublicStaticWithOneExactGizmoMethod()
        {
            Assert.That(DrawerType.IsPublic, Is.True);
            Assert.That(DrawerType.IsAbstract && DrawerType.IsSealed, Is.True);

            var methods = DrawerType.GetMethods(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributesData()
                    .Any(attribute => attribute.AttributeType == typeof(DrawGizmo)))
                .ToArray();

            Assert.That(methods.Length, Is.EqualTo(1));
            Assert.That(methods[0].ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(methods[0].GetParameters().Select(parameter => parameter.ParameterType),
                Is.EqualTo(new[] { typeof(WorldTopologyOverlay), typeof(GizmoType) }));
        }

        [Test]
        public void SceneDrawer_UsesExactGizmoMask()
        {
            var method = DrawerType.GetMethods(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Single(candidate => candidate.GetCustomAttributesData()
                    .Any(attribute => attribute.AttributeType == typeof(DrawGizmo)));
            var attribute = method.GetCustomAttributesData()
                .Single(candidate => candidate.AttributeType == typeof(DrawGizmo));
            var mask = (GizmoType)(int)attribute.ConstructorArguments[0].Value;

            Assert.That(mask,
                Is.EqualTo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected));
        }

        [Test]
        public void CustomInspector_IsInternalSealedAndTargetsOverlay()
        {
            var inspectorType = InspectorType;
            var attribute = inspectorType.GetCustomAttributesData()
                .Single(candidate => candidate.AttributeType == typeof(CustomEditor));

            Assert.That(inspectorType.IsNotPublic, Is.True);
            Assert.That(inspectorType.IsSealed, Is.True);
            Assert.That(typeof(UnityEditor.Editor).IsAssignableFrom(inspectorType), Is.True);
            Assert.That(attribute.ConstructorArguments[0].Value, Is.EqualTo(typeof(WorldTopologyOverlay)));
        }

        [Test]
        public void CustomInspector_InitialSeedTextIsExactZero()
        {
            var gameObject = new GameObject("WorldTopologyOverlayEditorTests");
            UnityEditor.Editor editor = null;
            try
            {
                var overlay = gameObject.AddComponent<WorldTopologyOverlay>();
                editor = UnityEditor.Editor.CreateEditor(overlay, InspectorType);
                var seedField = InspectorType.GetField(
                    "seedText",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(seedField, Is.Not.Null);
                Assert.That(seedField.GetValue(editor), Is.EqualTo("0"));
            }
            finally
            {
                if (editor != null)
                {
                    UnityEngine.Object.DestroyImmediate(editor);
                }

                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SceneAndGameSurfacesReferenceSameRuntimeDrawMethod()
        {
            var drawMethod = typeof(WorldTopologyOverlayGui).GetMethod(
                nameof(WorldTopologyOverlayGui.Draw),
                BindingFlags.Public | BindingFlags.Static);
            var gameMethod = typeof(WorldTopologyOverlay).GetMethod(
                "OnGUI",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var sceneMethod = DrawerType.GetMethods(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Single(method => method.GetCustomAttributesData()
                    .Any(attribute => attribute.AttributeType == typeof(DrawGizmo)));

            Assert.That(ContainsCall(gameMethod, drawMethod), Is.True);
            Assert.That(ContainsCall(sceneMethod, drawMethod), Is.True);
        }

        [Test]
        public void EditorTypesDoNotSubscribeToContinuousCallbacks()
        {
            var staticFields = DrawerType.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var inspectorFields = InspectorType.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(staticFields, Is.Empty);
            Assert.That(inspectorFields, Is.Empty);
            Assert.That(DrawerType.GetCustomAttributesData()
                .Any(attribute => attribute.AttributeType.Name.Contains("InitializeOnLoad")), Is.False);
            Assert.That(InspectorType.GetCustomAttributesData()
                .Any(attribute => attribute.AttributeType.Name.Contains("InitializeOnLoad")), Is.False);
        }

        private static MethodInfo GetSeedParser()
        {
            return InspectorType.GetMethod(
                "TryParseCanonicalSeed",
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        private static bool ContainsCall(MethodInfo caller, MethodInfo target)
        {
            var body = caller.GetMethodBody();
            if (body == null)
            {
                return false;
            }

            var bytes = body.GetILAsByteArray();
            for (var index = 0; index <= bytes.Length - 5; index++)
            {
                if (bytes[index] != 0x28 && bytes[index] != 0x6f)
                {
                    continue;
                }

                MethodBase resolved;
                try
                {
                    resolved = caller.Module.ResolveMethod(BitConverter.ToInt32(bytes, index + 1));
                }
                catch (ArgumentException)
                {
                    continue;
                }

                if (resolved.DeclaringType == target.DeclaringType &&
                    resolved.Name == target.Name &&
                    resolved.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(target.GetParameters().Select(parameter => parameter.ParameterType)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
