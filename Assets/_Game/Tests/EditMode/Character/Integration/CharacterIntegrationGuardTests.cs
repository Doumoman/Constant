using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Tests.Integration
{
    public sealed class CharacterIntegrationGuardTests
    {
        private sealed class CountingReadinessSource : ICharacterRoomReadinessSource
        {
            private readonly Dictionary<CharacterRoomId, bool> rooms =
                new Dictionary<CharacterRoomId, bool>();

            public int QueryCount { get; private set; }

            public void SetRoom(CharacterRoomId room, bool isReady)
            {
                rooms[room] = isReady;
            }

            public bool TryGetRoomReadiness(CharacterRoomId room, out bool isReady)
            {
                QueryCount++;
                return rooms.TryGetValue(room, out isReady);
            }
        }

        private static Type[] IntegrationRuntimeTypes()
        {
            return typeof(CharacterIntegrationBatchPolicy).Assembly
                .GetTypes()
                .Where(type => type.Namespace == "StarNight.Character.Integration")
                .ToArray();
        }

        [Test]
        public void Integration_DoesNotMutateMapTilemapScenePrefabPlayerTransformOrRunState()
        {
            // (1) 행동 검증: 배치 실행 후에도 입력 상태는 전부 그대로다.
            var roomA = CharacterRoomId.FromWorldTile(new WorldTileCoord(0, 0));
            var roomB = CharacterRoomId.FromWorldTile(new WorldTileCoord(12, 0));
            var start = new CharacterGeneratedMapStartSnapshot(
                1, roomA, true, new WorldTileCoord(5, 3),
                new WorldTileCoord(0, 0), new WorldTileCoord(11, 7));
            var declared = new List<CharacterGeneratedRouteEdgeSnapshot>
            {
                new CharacterGeneratedRouteEdgeSnapshot(
                    3, roomA, roomB, CharacterRouteBoundarySide.Right,
                    new WorldTileCoord(11, 3), new WorldTileCoord(12, 3),
                    CharacterRouteRequirement.BombSupport)
            };
            var readiness = new CountingReadinessSource();
            readiness.SetRoom(roomB, true);
            var inventory = new CharacterRunInventoryState(777, 2, 3);

            var spawns = new List<CharacterPlayerSpawnRequest>();
            var routes = new List<CharacterGeneratedRouteTransitionRequest>();
            var diagnostics = new List<CharacterIntegrationDiagnostic>();

            CharacterIntegrationBatchPolicy.BuildBatch(
                in start, 777, declared, in inventory, readiness,
                spawns, routes, diagnostics);

            Assert.That(spawns.Count, Is.EqualTo(1));
            Assert.That(routes.Count, Is.EqualTo(1));

            // 인벤토리는 진단 판정에만 쓰였고 소모되지 않았다(런 상태 불변).
            Assert.That(inventory.BombCount, Is.EqualTo(2));
            Assert.That(inventory.RopeCount, Is.EqualTo(3));

            // 준비 소스는 읽기만 했다(질의 발생, 등록 상태 그대로).
            Assert.That(readiness.QueryCount, Is.GreaterThanOrEqualTo(1));
            bool isReady;
            Assert.That(readiness.TryGetRoomReadiness(roomB, out isReady), Is.True);
            Assert.That(isReady, Is.True);

            // (2) 표면 검증: Integration 전 타입은 불변 값/정책이다 —
            //     공개 setter·공개 가변 필드(enum 제외)가 없다.
            foreach (var type in IntegrationRuntimeTypes())
            {
                foreach (var property in type.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    Assert.That(property.GetSetMethod(), Is.Null,
                        type.Name + "." + property.Name + "에 공개 setter가 있다");
                }

                if (!type.IsEnum)
                {
                    foreach (var field in type.GetFields(
                        BindingFlags.Public | BindingFlags.Instance))
                    {
                        Assert.That(field.IsInitOnly, Is.True,
                            type.Name + "." + field.Name + "은 가변 공개 필드다");
                    }
                }

                // 변조형 명명 부재 — 스폰/전환은 요청일 뿐 적용이 아니다.
                var memberNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance
                        | BindingFlags.Static)
                    .Select(member => member.Name)
                    .ToArray();

                foreach (var keyword in new[]
                    { "Instantiate", "Destroy", "Teleport", "LoadScene",
                      "SetTile", "Mutate", "Apply" })
                {
                    Assert.That(memberNames, Has.None.Contains(keyword),
                        type.Name + " 표면에 변조형 명명이 있다");
                }
            }
        }

        [Test]
        public void IntegrationRuntime_DoesNotUseAnimatorPhysicsUiAudioSceneSaveOrForbiddenActions()
        {
            var runtimeAssembly = typeof(CharacterIntegrationBatchPolicy).Assembly;
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            // Animator/Tilemap/UI/오디오/TMP 모듈 참조 부재.
            // (Physics2DModule 참조는 CHAR01 승인 충돌 "질의" 어댑터 소관 —
            // CHAR05 가드들과 동일한 확립 검증 레벨.)
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AnimationModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.TilemapModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.UIModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.UI"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AudioModule"));
            Assert.That(referenced, Does.Not.Contain("Unity.TextMeshPro"));

            var types = IntegrationRuntimeTypes();
            Assert.That(types.Length, Is.GreaterThanOrEqualTo(12));

            foreach (var type in types)
            {
                // Integration 런타임은 MonoBehaviour/Component가 아니다 —
                // Component가 아니면 물리 콜백은 애초에 호출되지 않는다.
                Assert.That(typeof(Component).IsAssignableFrom(type), Is.False,
                    type.Name + "은 Unity Component다");

                foreach (var method in type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance | BindingFlags.Static
                        | BindingFlags.DeclaredOnly))
                {
                    Assert.That(method.Name, Does.Not.StartWith("OnCollision"),
                        type.Name + "에 물리 콜백이 있다: " + method.Name);
                    Assert.That(method.Name, Does.Not.StartWith("OnTrigger"),
                        type.Name + "에 물리 콜백이 있다: " + method.Name);
                }

                // 표면 타입에 Animator/Tilemap/물리/씬/UI/오디오/세이브/
                // GameObject 타입이 등장하지 않는다.
                var surfaceTypeNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance
                        | BindingFlags.Static)
                    .SelectMany(member =>
                    {
                        switch (member)
                        {
                            case MethodInfo method:
                                return method.GetParameters()
                                    .Select(parameter => parameter.ParameterType.Name)
                                    .Concat(new[] { method.ReturnType.Name });
                            case PropertyInfo property:
                                return new[] { property.PropertyType.Name };
                            case FieldInfo field:
                                return new[] { field.FieldType.Name };
                            default:
                                return Enumerable.Empty<string>();
                        }
                    });

                foreach (var typeName in surfaceTypeNames)
                {
                    Assert.That(typeName, Does.Not.Contain("Animator"));
                    Assert.That(typeName, Does.Not.Contain("Tilemap"));
                    Assert.That(typeName, Does.Not.Contain("Rigidbody"));
                    Assert.That(typeName, Does.Not.Contain("Collider"));
                    Assert.That(typeName, Does.Not.Contain("RaycastHit"));
                    Assert.That(typeName, Does.Not.Contain("Scene"));
                    Assert.That(typeName, Does.Not.Contain("Canvas"));
                    Assert.That(typeName, Does.Not.Contain("Audio"));
                    Assert.That(typeName, Does.Not.Contain("GameObject"));
                    Assert.That(typeName, Does.Not.Contain("PlayerPrefs"));
                }

                // 금지 이동/공격 개념이 통합 계약 표면에 스며들지 않았다
                // (잠금 밖 요구는 Unsupported 분류로만 존재).
                var memberNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance
                        | BindingFlags.Static)
                    .Select(member => member.Name)
                    .ToArray();

                foreach (var keyword in new[]
                    { "BasicAttack", "Melee", "Shoot", "Dash",
                      "WallJump", "DoubleJump" })
                {
                    Assert.That(type.Name, Does.Not.Contain(keyword));
                    Assert.That(memberNames, Has.None.Contains(keyword));
                }
            }

            // 논리 행동 ID는 잠금 5종 그대로다.
            Assert.That(Enum.GetNames(typeof(CharacterActionId)), Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));
        }
    }
}
