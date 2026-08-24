using System.Linq;
using NUnit.Framework;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.MapIntegration
{
    public sealed class CharacterMapDependencyDirectionTests
    {
        [Test]
        public void DependencyGuard_AllowsOnlyGameMapRuntimeAndRejectsTilemapAuthoringLegacy()
        {
            var characterRuntime = typeof(CharacterMapCoordinateBridge).Assembly;
            var referenced = characterRuntime.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            // 승인 의존: MAP 공용 런타임 정확히 1개.
            var mapReferences = referenced
                .Where(name => name.StartsWith("Game.Map"))
                .ToArray();

            Assert.That(mapReferences, Is.EquivalentTo(new[] { "Game.Map.Runtime" }));

            // 계속 금지: Tilemap / InputSystem / authoring·editor·test / legacy / stale.
            Assert.That(referenced, Does.Not.Contain("UnityEngine.TilemapModule"));
            Assert.That(referenced, Does.Not.Contain("Unity.InputSystem"));
            Assert.That(referenced, Does.Not.Contain("Game.Stage.Runtime"));
            Assert.That(referenced, Does.Not.Contain("StarNight.Runtime"));

            foreach (var name in referenced)
            {
                Assert.That(name, Does.Not.StartWith("MapAuthoring"));
                Assert.That(name, Does.Not.Contain(".Editor"));
                Assert.That(name, Does.Not.Contain("Tests"));
            }

            // 의존 방향은 one-way다: MAP 공용 런타임은 캐릭터 런타임을 참조하지 않는다.
            var mapRuntime = typeof(WorldCoordinateUtility).Assembly;
            var mapReferenced = mapRuntime.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            foreach (var name in mapReferenced)
            {
                Assert.That(name, Does.Not.StartWith("Game.Character"));
            }

            // 전역 싱글톤 lookup 금지: MapIntegration 타입에 public static 가변
            // 인스턴스 필드/프로퍼티가 없다.
            var integrationTypes = characterRuntime.GetTypes()
                .Where(type => type.Namespace == "StarNight.Character.MapIntegration")
                .ToArray();

            Assert.That(integrationTypes, Is.Not.Empty);

            foreach (var type in integrationTypes)
            {
                var staticFields = type.GetFields(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                foreach (var field in staticFields)
                {
                    Assert.That(field.IsInitOnly || field.IsLiteral, Is.True,
                        type.Name + "에 가변 전역 상태가 있다: " + field.Name);
                }
            }
        }
    }
}
