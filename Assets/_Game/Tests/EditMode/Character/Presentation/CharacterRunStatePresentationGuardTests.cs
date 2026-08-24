using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Presentation;
using StarNight.Character.RunState;
using UnityEngine;

namespace StarNight.Character.Tests.Presentation
{
    public sealed class CharacterRunStatePresentationGuardTests
    {
        private static Type[] RunStateAndPresentationTypes()
        {
            var assembly = typeof(CharacterPresentationBridge).Assembly;
            return assembly.GetTypes()
                .Where(type =>
                    type.Namespace == "StarNight.Character.RunState"
                    || type.Namespace == "StarNight.Character.Presentation")
                .ToArray();
        }

        [Test]
        public void RunStatePresentationRuntime_DoesNotUseAnimatorPhysicsSceneHudSaveAudioOrForbiddenActions()
        {
            var runtimeAssembly = typeof(CharacterPresentationBridge).Assembly;
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            // Animator/Tilemap/UI/Audio/TMP 모듈 참조 부재 — 연출 브리지는
            // 요청 데이터만 만들고 어떤 재생 권위도 갖지 않는다.
            // (Physics2DModule 참조는 CHAR01 승인 충돌 "질의" 어댑터 소관 —
            // CHAR05_01~03 가드와 동일한 확립 검증 레벨.)
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AnimationModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.TilemapModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.UIModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.UI"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AudioModule"));
            Assert.That(referenced, Does.Not.Contain("Unity.TextMeshPro"));

            var types = RunStateAndPresentationTypes();
            Assert.That(types.Length, Is.GreaterThanOrEqualTo(10));

            foreach (var type in types)
            {
                // RunState/Presentation 런타임은 MonoBehaviour/Component가
                // 아니다 — Component가 아니면 물리 콜백은 호출되지 않는다.
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

                // 표면 타입에 Animator/Tilemap/물리/씬/UI/오디오/세이브
                // 타입이 등장하지 않는다.
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

                // 금지 이동/공격 개념 + 세이브/씬 명명이 스며들지 않았다.
                var memberNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance
                        | BindingFlags.Static)
                    .Select(member => member.Name)
                    .ToArray();

                foreach (var keyword in new[]
                    { "BasicAttack", "Melee", "Shoot", "Dash", "WallJump",
                      "DoubleJump", "LoadScene", "Reload", "PlayerPrefs",
                      "PlayAudio", "PlayAnimation" })
                {
                    Assert.That(type.Name, Does.Not.Contain(keyword));
                    Assert.That(memberNames, Has.None.Contains(keyword));
                }
            }

            // 논리 행동 ID는 잠금 5종 그대로다.
            Assert.That(Enum.GetNames(typeof(CharacterActionId)), Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));

            // 런 상태 enum은 데이터 2종뿐이다(연출 상태 아님).
            Assert.That(Enum.GetNames(typeof(CharacterRunStatus)),
                Is.EquivalentTo(new[] { "Active", "Failed" }));
        }
    }
}
