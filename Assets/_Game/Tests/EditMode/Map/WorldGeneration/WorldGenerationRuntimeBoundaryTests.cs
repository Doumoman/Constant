using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration
{
    public sealed class WorldGenerationRuntimeBoundaryTests
    {
        private const string RuntimeAssemblyPath =
            "Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef";

        private const string RuntimeWorldGenerationRoot =
            "Assets/_Game/Map/Runtime/WorldGeneration";

        private static readonly string[] ForbiddenDependencyNames =
        {
            "StarNight.Stage",
            "StarNight.Generation.P6",
            "StarNight.MapHarness.P11",
            "StageMapGenerator",
            "P6RoomGraphGenerator",
            "P11MapStageHarness2D",
        };

        private static readonly HashSet<string> ReservedTypeNames =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "GridWorld",
                "StageMapGenerator",
                "StageMapProfile",
                "StageGeneratedLayout",
                "RoomTemplate",
                "RoomGridTransform",
                "P6RoomGraphGenerator",
                "TileMutationService",
                "P11MapStageHarness2D",
            };

        private static readonly Regex NamespaceDeclaration = new Regex(
            @"(?m)^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\b",
            RegexOptions.CultureInvariant);

        private static readonly Regex TypeDeclaration = new Regex(
            @"\b(?:class|struct|interface|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b|" +
            @"\brecord(?:\s+(?:class|struct))?\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b",
            RegexOptions.CultureInvariant);

        private static readonly Regex CommentsAndLiterals = new Regex(
            "/\\*.*?\\*/|//[^\\r\\n]*|@\\\"(?:\\\"\\\"|[^\\\"])*\\\"|\\\"(?:\\\\.|[^\\\"\\\\])*\\\"|'(?:\\\\.|[^'\\\\])*'",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        [Test]
        public void RuntimeAssemblyKeepsApprovedIdentityAndEmptyReferences()
        {
            var definition = LoadAssemblyDefinition(RuntimeAssemblyPath);
            var references = definition.references ?? Array.Empty<string>();

            Assert.That(definition.name, Is.EqualTo("Game.Map.Runtime"),
                RuntimeAssemblyPath + ": assembly name changed");
            Assert.That(references, Is.Empty,
                RuntimeAssemblyPath + ": runtime assembly references must remain empty; found: " +
                string.Join(", ", references.OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(references.Any(reference =>
                    reference.IndexOf("UnityEditor", StringComparison.Ordinal) >= 0),
                Is.False,
                RuntimeAssemblyPath + ": UnityEditor assembly references are forbidden");
        }

        [Test]
        public void RuntimeSourcesUseOnlyApprovedWorldGenerationNamespaces()
        {
            var violations = new List<string>();
            foreach (var sourcePath in FindRuntimeSourcePaths())
            {
                var code = StripCommentsAndLiterals(File.ReadAllText(ToFullPath(sourcePath)));
                var namespaceMatches = NamespaceDeclaration.Matches(code).Cast<Match>().ToArray();
                if (namespaceMatches.Length == 0)
                {
                    violations.Add(sourcePath + ": namespace declaration is missing");
                    continue;
                }

                foreach (var match in namespaceMatches)
                {
                    var namespaceName = match.Groups[1].Value;
                    if (!namespaceName.Equals("StarNight.Map.WorldGeneration", StringComparison.Ordinal) &&
                        !namespaceName.StartsWith("StarNight.Map.WorldGeneration.", StringComparison.Ordinal))
                    {
                        violations.Add(sourcePath + ": forbidden namespace " + namespaceName);
                    }
                }
            }

            Assert.That(
                violations.OrderBy(value => value, StringComparer.Ordinal),
                Is.Empty,
                "Runtime WorldGeneration namespace violations:\n" + string.Join("\n", violations));
        }

        [Test]
        public void RuntimeSourcesRejectLegacyDependenciesAndReservedTypeDeclarations()
        {
            var violations = new List<string>();
            foreach (var sourcePath in FindRuntimeSourcePaths())
            {
                var code = StripCommentsAndLiterals(File.ReadAllText(ToFullPath(sourcePath)));
                if (Regex.IsMatch(
                        code,
                        @"(?m)^\s*(?:global\s+)?using\s+UnityEditor(?:\.|\s*;)",
                        RegexOptions.CultureInvariant))
                {
                    violations.Add(sourcePath + ": using UnityEditor is forbidden");
                }

                foreach (var dependencyName in ForbiddenDependencyNames)
                {
                    if (Regex.IsMatch(
                            code,
                            @"\b" + Regex.Escape(dependencyName) + @"\b",
                            RegexOptions.CultureInvariant))
                    {
                        violations.Add(sourcePath + ": forbidden dependency or identifier " + dependencyName);
                    }
                }

                foreach (Match declaration in TypeDeclaration.Matches(code))
                {
                    var typeName = declaration.Groups["name"].Value;
                    if (ReservedTypeNames.Contains(typeName))
                    {
                        violations.Add(sourcePath + ": reserved type declaration " + typeName);
                    }
                }
            }

            Assert.That(
                violations.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal),
                Is.Empty,
                "Runtime WorldGeneration dependency-boundary violations:\n" +
                string.Join("\n", violations));
        }

        private static string[] FindRuntimeSourcePaths()
        {
            var fullRoot = ToFullPath(RuntimeWorldGenerationRoot);
            if (!Directory.Exists(fullRoot))
            {
                return Array.Empty<string>();
            }

            return Directory.EnumerateFiles(fullRoot, "*.cs", SearchOption.AllDirectories)
                .Select(ToProjectRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
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

        private static string StripCommentsAndLiterals(string source)
        {
            return CommentsAndLiterals.Replace(source, match =>
                match.Value.IndexOf('\n') >= 0
                    ? new string('\n', match.Value.Count(character => character == '\n'))
                    : " ");
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
        }
    }
}
