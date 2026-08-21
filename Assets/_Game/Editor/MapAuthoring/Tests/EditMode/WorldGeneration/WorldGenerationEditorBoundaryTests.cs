using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.WorldGeneration
{
    public sealed class WorldGenerationEditorBoundaryTests
    {
        private const string EditorAssemblyPath =
            "Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef";

        private const string RuntimeTestAssemblyPath =
            "Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef";

        private const string EditorTestAssemblyPath =
            "Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef";

        private static readonly string[] WorldGenerationRoots =
        {
            "Assets/_Game/Map/Runtime/WorldGeneration",
            "Assets/_Game/Editor/MapAuthoring/WorldGeneration",
            "Assets/_Game/Tests/EditMode/Map/WorldGeneration",
            "Assets/_Game/Tests/PlayMode/Map/WorldGeneration",
            "Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration",
            "Assets/_Game/Map/Data/WorldGeneration",
        };

        [Test]
        public void MapAuthoringEditorAssemblyRemainsEditorOnlyAndReferencesMapRuntime()
        {
            var definition = LoadAssemblyDefinition(EditorAssemblyPath);
            Assert.That(definition.name, Is.EqualTo("MapAuthoring.Editor"),
                EditorAssemblyPath + ": assembly name changed");
            Assert.That(definition.includePlatforms ?? Array.Empty<string>(),
                Is.EquivalentTo(new[] { "Editor" }),
                EditorAssemblyPath + ": assembly must remain Editor-only");
            AssertReferencesInclude(definition, EditorAssemblyPath, "Game.Map.Runtime");
        }

        [Test]
        public void RuntimeEditModeAssemblyReferencesMapRuntimeAndTestRunners()
        {
            var definition = LoadAssemblyDefinition(RuntimeTestAssemblyPath);
            Assert.That(definition.name, Is.EqualTo("Game.Map.Tests.EditMode"),
                RuntimeTestAssemblyPath + ": assembly name changed");
            AssertReferencesInclude(
                definition,
                RuntimeTestAssemblyPath,
                "Game.Map.Runtime",
                "UnityEditor.TestRunner",
                "UnityEngine.TestRunner");
        }

        [Test]
        public void EditorEditModeAssemblyReferencesRuntimeEditorAndTestRunners()
        {
            var definition = LoadAssemblyDefinition(EditorTestAssemblyPath);
            Assert.That(definition.name, Is.EqualTo("MapAuthoring.Tests.EditMode"),
                EditorTestAssemblyPath + ": assembly name changed");
            AssertReferencesInclude(
                definition,
                EditorTestAssemblyPath,
                "Game.Map.Runtime",
                "MapAuthoring.Editor",
                "UnityEditor.TestRunner",
                "UnityEngine.TestRunner");
        }

        [Test]
        public void WorldGenerationUsesNoDedicatedAssemblyDefinitionOrReference()
        {
            var violations = WorldGenerationRoots
                .SelectMany(FindAssemblyBoundaryFiles)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(violations, Is.Empty,
                "WorldGeneration must reuse approved assemblies; forbidden files:\n" +
                string.Join("\n", violations));
        }

        private static void AssertReferencesInclude(
            AssemblyDefinition definition,
            string projectRelativePath,
            params string[] expectedReferences)
        {
            var actualReferences = definition.references ?? Array.Empty<string>();
            var missingReferences = expectedReferences
                .Where(expected => !actualReferences.Contains(expected, StringComparer.Ordinal))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(missingReferences, Is.Empty,
                projectRelativePath + ": missing required references: " +
                string.Join(", ", missingReferences));
        }

        private static IEnumerable<string> FindAssemblyBoundaryFiles(string projectRelativeRoot)
        {
            var fullRoot = ToFullPath(projectRelativeRoot);
            if (!Directory.Exists(fullRoot))
            {
                return Array.Empty<string>();
            }

            return Directory.EnumerateFiles(fullRoot, "*.asmdef", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(fullRoot, "*.asmref", SearchOption.AllDirectories))
                .Select(ToProjectRelativePath)
                .ToArray();
        }

        private static AssemblyDefinition LoadAssemblyDefinition(string projectRelativePath)
        {
            var json = File.ReadAllText(ToFullPath(projectRelativePath));
            var definition = JsonUtility.FromJson<AssemblyDefinition>(json);
            Assert.That(definition, Is.Not.Null,
                projectRelativePath + ": assembly definition JSON could not be parsed");
            return definition;
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string ToFullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                ProjectRoot,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ToProjectRelativePath(string fullPath)
        {
            var normalizedRoot = ProjectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            var normalizedFullPath = Path.GetFullPath(fullPath);
            return normalizedFullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                ? normalizedFullPath.Substring(normalizedRoot.Length).Replace('\\', '/')
                : normalizedFullPath.Replace('\\', '/');
        }

        [Serializable]
        private sealed class AssemblyDefinition
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
        }
    }
}
