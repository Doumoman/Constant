using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.GeneratedRunValidation;
using StarNight.Character.Input;
using StarNight.Character.RunState;
using UnityEngine;

namespace StarNight.Character.Tests.GeneratedRunValidation
{
    public sealed class CharacterGeneratedRunValidationGuardTests
    {
        private static Type[] ValidationRuntimeTypes()
        {
            return typeof(CharacterGeneratedRunValidationPolicy).Assembly
                .GetTypes()
                .Where(type =>
                    type.Namespace == "StarNight.Character.GeneratedRunValidation")
                .ToArray();
        }

        [Test]
        public void GeneratedRunValidation_DoesNotMutateMapTilemapScenePrefabPlayerTransformRunStateInventoryOrAssets()
        {
            // (1) 행동 검증: 검증·스윕 실행 후 입력이 전부 그대로다.
            var inventory = new CharacterRunInventoryState(
                CharacterGeneratedRunFixtures.ActorId, 2, 3);
            var readiness = CharacterGeneratedRunFixtures.ReadyRooms();
            var snapshot = CharacterGeneratedRunFixtures.ValidRun(11);

            var result = CharacterGeneratedRunValidationPolicy.Validate(
                snapshot, CharacterGeneratedRunFixtures.ActorId,
                in inventory, readiness);

            Assert.That(result.Passed, Is.True);

            // 인벤토리는 읽기만 했다(런 상태 무소모).
            Assert.That(inventory.BombCount, Is.EqualTo(2));
            Assert.That(inventory.RopeCount, Is.EqualTo(3));

            // 준비 소스는 질의만 했고 등록 상태 그대로다.
            Assert.That(readiness.QueryCount, Is.GreaterThanOrEqualTo(1));
            bool isReady;
            Assert.That(readiness.TryGetRoomReadiness(
                CharacterGeneratedRunFixtures.RoomB, out isReady), Is.True);
            Assert.That(isReady, Is.True);

            // 스냅샷 목록도 그대로다(검증이 데이터를 편집하지 않는다).
            Assert.That(snapshot.Rooms.Count, Is.EqualTo(2));
            Assert.That(snapshot.Microchunks.Count, Is.EqualTo(2));
            Assert.That(snapshot.Routes.Count, Is.EqualTo(1));
            Assert.That(snapshot.ItemPlacements.Count, Is.EqualTo(1));

            // (2) 표면 검증: 전 타입 불변(공개 setter·가변 공개 필드 없음,
            //     enum 제외) + 변조형 명명 부재.
            foreach (var type in ValidationRuntimeTypes())
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

                var memberNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance
                        | BindingFlags.Static)
                    .Select(member => member.Name)
                    .ToArray();

                foreach (var keyword in new[]
                    { "Instantiate", "Destroy", "Teleport", "LoadScene",
                      "SetTile", "Mutate", "Spend" })
                {
                    Assert.That(memberNames, Has.None.Contains(keyword),
                        type.Name + " 표면에 변조형 명명이 있다");
                }
            }
        }

        [Test]
        public void GeneratedRunValidation_DoesNotUseAnimatorPhysicsUiAudioSceneSaveOrForbiddenActions()
        {
            var runtimeAssembly = typeof(CharacterGeneratedRunValidationPolicy).Assembly;
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            // Animator/Tilemap/UI/오디오/TMP 모듈 참조 부재.
            // (Physics2DModule 참조는 CHAR01 승인 충돌 "질의" 어댑터 소관 —
            // CHAR05~06 가드들과 동일한 확립 검증 레벨.)
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AnimationModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.TilemapModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.UIModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.UI"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AudioModule"));
            Assert.That(referenced, Does.Not.Contain("Unity.TextMeshPro"));

            var types = ValidationRuntimeTypes();
            Assert.That(types.Length, Is.GreaterThanOrEqualTo(9));

            foreach (var type in types)
            {
                // 검증 런타임은 MonoBehaviour/Component가 아니다.
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

                // 표면 타입에 금지 계열 타입이 등장하지 않는다.
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

                // 금지 이동/공격 개념 부재.
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
