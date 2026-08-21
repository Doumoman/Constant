using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration
{
    public sealed class WorldGenerationModuleStructureTests
    {
        private static readonly string[] ApprovedDirectoryPaths =
        {
            "Assets/_Game/Map/Runtime/WorldGeneration",
            "Assets/_Game/Map/Runtime/WorldGeneration/Domain",
            "Assets/_Game/Map/Runtime/WorldGeneration/Data",
            "Assets/_Game/Map/Runtime/WorldGeneration/Generation",
            "Assets/_Game/Map/Runtime/WorldGeneration/Validation",
            "Assets/_Game/Map/Runtime/WorldGeneration/Random",
            "Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics",
            "Assets/_Game/Editor/MapAuthoring/WorldGeneration",
            "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import",
            "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Validation",
            "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview",
            "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Windows",
            "Assets/_Game/Tests/EditMode/Map/WorldGeneration",
            "Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain",
            "Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data",
            "Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation",
            "Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation",
            "Assets/_Game/Tests/EditMode/Map/WorldGeneration/Determinism",
            "Assets/_Game/Tests/PlayMode/Map/WorldGeneration",
            "Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration",
            "Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import",
            "Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Validation",
            "Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview",
            "Assets/_Game/Map/Data/WorldGeneration",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/World",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Route",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Biome",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/SpecialMap",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Village",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Population",
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Items",
            "Assets/_Game/Map/Data/WorldGeneration/Imported",
            "Assets/_Game/Map/Data/WorldGeneration/GeneratedDebug",
        };

        private static readonly string[] MajorRootPaths =
        {
            "Assets/_Game/Map/Runtime/WorldGeneration",
            "Assets/_Game/Editor/MapAuthoring/WorldGeneration",
            "Assets/_Game/Tests/EditMode/Map/WorldGeneration",
            "Assets/_Game/Tests/PlayMode/Map/WorldGeneration",
            "Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration",
            "Assets/_Game/Map/Data/WorldGeneration",
        };

        [Test]
        public void ApprovedDirectorySetContainsExactlyThirtySixExistingPaths()
        {
            var duplicatePaths = ApprovedDirectoryPaths
                .GroupBy(path => path, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var missingPaths = ApprovedDirectoryPaths
                .Where(path => !Directory.Exists(ToFullPath(path)))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(ApprovedDirectoryPaths, Has.Length.EqualTo(36));
            Assert.That(duplicatePaths, Is.Empty,
                "Duplicate approved WorldGeneration directories:\n" + string.Join("\n", duplicatePaths));
            Assert.That(missingPaths, Is.Empty,
                "Missing approved WorldGeneration directories:\n" + string.Join("\n", missingPaths));
        }

        [Test]
        public void MajorRootsRemainDistinctAndProjectRelative()
        {
            var violations = new List<string>();
            for (var index = 0; index < MajorRootPaths.Length; index++)
            {
                var path = NormalizeProjectPath(MajorRootPaths[index]);
                if (Path.IsPathRooted(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    violations.Add(path + ": root must be project-relative under Assets");
                }

                for (var otherIndex = index + 1; otherIndex < MajorRootPaths.Length; otherIndex++)
                {
                    var otherPath = NormalizeProjectPath(MajorRootPaths[otherIndex]);
                    if (path.Equals(otherPath, StringComparison.Ordinal) ||
                        path.StartsWith(otherPath + "/", StringComparison.Ordinal) ||
                        otherPath.StartsWith(path + "/", StringComparison.Ordinal))
                    {
                        violations.Add(path + " <-> " + otherPath + ": major roots must remain distinct");
                    }
                }
            }

            Assert.That(
                violations.OrderBy(value => value, StringComparer.Ordinal),
                Is.Empty,
                "WorldGeneration major-root boundary violations:\n" + string.Join("\n", violations));
        }

        [Test]
        public void WorldGenerationRootsContainNoAssemblyDefinitionsOrReferences()
        {
            var violations = MajorRootPaths
                .SelectMany(FindAssemblyBoundaryFiles)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(violations, Is.Empty,
                "WorldGeneration must reuse existing assemblies; forbidden assembly files:\n" +
                string.Join("\n", violations));
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

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string ToFullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                ProjectRoot,
                NormalizeProjectPath(projectRelativePath)
                    .Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ToProjectRelativePath(string fullPath)
        {
            var normalizedRoot = ProjectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            var normalizedFullPath = Path.GetFullPath(fullPath);
            if (!normalizedFullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedFullPath.Replace('\\', '/');
            }

            return normalizedFullPath.Substring(normalizedRoot.Length).Replace('\\', '/');
        }

        private static string NormalizeProjectPath(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }
    }
}
