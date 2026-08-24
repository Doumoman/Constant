using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Equipment;
using StarNight.Character.Input;
using StarNight.Character.Traversal;
using UnityEngine;

namespace StarNight.Character.Tests.Traversal
{
    public sealed class CharacterRopeGuardTests
    {
        /// <summary>로프 계약 표면: Traversal 전체 + Equipment의 Rope 타입.</summary>
        private static Type[] RopeRuntimeTypes()
        {
            var assembly = typeof(CharacterRopeClimbPolicy).Assembly;
            return assembly.GetTypes()
                .Where(type =>
                    type.Namespace == "StarNight.Character.Traversal"
                    || (type.Namespace == "StarNight.Character.Equipment"
                        && type.Name.Contains("Rope")))
                .ToArray();
        }

        [Test]
        public void RopeRuntime_DoesNotUseAnimatorPhysicsTilemapOrForbiddenActions()
        {
            var runtimeAssembly = typeof(CharacterRopeClimbPolicy).Assembly;
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            // Animator/Tilemap 직접 접근 금지(잠금 규칙).
            // Physics2DModule 참조는 CHAR01 승인 충돌 "질의" 어댑터 소관이므로
            // 로프 가드는 로프 타입 범위에서 물리 콜백 부재를 검증한다.
            Assert.That(referenced, Does.Not.Contain("UnityEngine.AnimationModule"));
            Assert.That(referenced, Does.Not.Contain("UnityEngine.TilemapModule"));

            var ropeTypes = RopeRuntimeTypes();
            Assert.That(ropeTypes.Length, Is.GreaterThanOrEqualTo(11));

            foreach (var type in ropeTypes)
            {
                // 로프 런타임은 MonoBehaviour/Component가 아니다 — 순수 값/정책.
                // Component가 아니면 Unity 물리 콜백은 애초에 호출되지 않는다.
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

                // 표면 타입에 Animator/Tilemap/물리 타입이 등장하지 않는다.
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
                }
            }

            // 논리 행동 ID는 잠금 5종 그대로다 — Rope는 기존 슬롯을 쓴다.
            Assert.That(Enum.GetNames(typeof(CharacterActionId)), Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));
        }

        [Test]
        public void RopeRuntime_DoesNotIntroduceDashWallJumpDoubleJumpOrBasicAttack()
        {
            var forbidden = new[]
            {
                "Dash", "WallJump", "DoubleJump",
                "BasicAttack", "Melee", "Shoot"
            };

            // 런타임 어셈블리 전체 타입 이름에 금지 개념이 없다.
            var runtimeTypes = typeof(CharacterRopeClimbPolicy).Assembly.GetTypes();
            foreach (var type in runtimeTypes)
            {
                foreach (var keyword in forbidden)
                {
                    Assert.That(type.Name, Does.Not.Contain(keyword),
                        "런타임 타입 이름에 금지 개념이 있다: " + type.Name);
                }
            }

            // 로프 계약 표면(공개 멤버)에도 금지 개념·추가 공중 제어가 없다.
            foreach (var type in RopeRuntimeTypes())
            {
                var memberNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance
                        | BindingFlags.Static)
                    .Select(member => member.Name)
                    .ToArray();

                foreach (var keyword in forbidden)
                {
                    Assert.That(memberNames, Has.None.Contains(keyword));
                }

                // 로프 등반은 추가 공중 제어를 부여하지 않는다.
                Assert.That(memberNames, Has.None.Contains("AirControl"));
                Assert.That(memberNames, Has.None.Contains("AirAccel"));
            }

            // 모터 요청은 수직 성분만 기술한다 — 수평 속도 멤버가 없어
            // 구조적으로 추가 공중 제어를 만들 수 없다.
            var motorMembers = typeof(CharacterRopeClimbMotorRequest)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();

            Assert.That(motorMembers, Is.EquivalentTo(
                new[] { "ActorId", "VerticalVelocity", "TargetWorldY" }));
        }
    }
}
