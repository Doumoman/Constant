using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Survival;
using UnityEngine;

namespace StarNight.Character.Tests.Survival
{
    public sealed class CharacterSurvivalGuardTests
    {
        private static Type[] SurvivalRuntimeTypes()
        {
            return typeof(CharacterHealthDamagePolicy).Assembly
                .GetTypes()
                .Where(type => type.Namespace == "StarNight.Character.Survival")
                .ToArray();
        }

        [Test]
        public void SurvivalRuntime_DoesNotUseAnimatorPhysicsSceneHudSaveOrForbiddenActions()
        {
            var runtimeAssembly = typeof(CharacterHealthDamagePolicy).Assembly;
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            // Animator/Tilemap/UI/Audio 모듈 참조 부재 — HUD·오디오·연출이
            // 권위가 될 수 없다. (Physics2DModule 참조는 CHAR01 승인 충돌
            // "질의" 어댑터 소관이라 Survival 가드는 타입 범위 콜백 부재로
            // 검증한다 — CHAR05_01/02 가드와 동일한 확립 레벨.)
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AnimationModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.TilemapModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.UIModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AudioModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.UI"));

            var survivalTypes = SurvivalRuntimeTypes();
            Assert.That(survivalTypes.Length, Is.GreaterThanOrEqualTo(15));

            foreach (var type in survivalTypes)
            {
                // Survival 런타임은 MonoBehaviour/Component가 아니다 —
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

                // 표면 타입에 Animator/Tilemap/물리/씬/UI/오디오 타입이 없다.
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
                }

                // 금지 이동/공격 개념이 생존 계약에 스며들지 않았다.
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

            // 논리 행동 ID는 잠금 5종 그대로다 — 생존 계약은 행동을 추가하지
            // 않는다.
            Assert.That(Enum.GetNames(typeof(CharacterActionId)), Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));

            // cause 열거형도 스키마 잠금 9종 그대로다(확장 없음).
            Assert.That(Enum.GetNames(typeof(CharacterDamageSourceKind)),
                Is.EquivalentTo(new[]
                {
                    "Stomp", "ThrownObject", "Explosion", "ToolHit",
                    "EnemyContact", "Spike", "Fall", "Crush", "Environment"
                }));
        }
    }
}
