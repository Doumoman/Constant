#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor.Compilation;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE00ContractTests
    {
        [Test]
        public void BuildTagMatchesApprovedSpecifications()
        {
            Assert.That(MapBuildTag.CoreSpecificationVersion, Is.EqualTo("2.1"));
            Assert.That(MapBuildTag.MapSpecificationVersion, Is.EqualTo("1.0"));
            Assert.That(MapBuildTag.Value, Does.StartWith("StarNight/Core-v2.1/Map-v1.0/"));
        }

        [Test]
        public void BothAuthoringScenesAreRejectedRegardlessOfPathSeparators()
        {
            var paths = new[]
            {
                EditorSceneBuildGuard.MapElementLabPath,
                EditorSceneBuildGuard.StageLayoutLabPath.Replace('/', '\\'),
                "Assets/_Game/Scenes/00_Boot.unity",
            };

            var result = EditorSceneBuildGuard.FindForbiddenScenePaths(paths);

            Assert.That(result, Is.EquivalentTo(new[]
            {
                EditorSceneBuildGuard.MapElementLabPath,
                EditorSceneBuildGuard.StageLayoutLabPath,
            }));
        }

        [Test]
        public void CurrentBuildSettingsContainNoAuthoringScene()
        {
            Assert.That(EditorSceneBuildGuard.FindForbiddenScenesInCurrentBuildSettings(), Is.Empty);
        }

        [Test]
        public void EditorAssemblyIsExcludedFromPlayerCompilation()
        {
            var playerAssemblies = CompilationPipeline
                .GetAssemblies(AssembliesType.Player)
                .Select(assembly => assembly.name);

            Assert.That(playerAssemblies, Does.Not.Contain("MapAuthoring.Editor"));
        }
    }
}

#endif
