using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Interaction;

namespace StarNight.Character.Tests.Interaction
{
    public sealed class CharacterInteractionBoundaryTests
    {
        [Test]
        public void CarryContract_UsesRequestsAndDoesNotMutateCarryableInternals()
        {
            // 후보/요청은 immutable 값 객체다 — public setter·가변 필드가 없어
            // 캐릭터가 Carryable 내부 상태를 직접 수정할 경로가 없다.
            var contractTypes = new[]
            {
                typeof(CharacterCarryCandidate),
                typeof(CharacterCarryPlacementRequest),
                typeof(CharacterCarryThrowRequest)
            };

            foreach (var type in contractTypes)
            {
                foreach (var property in type.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance))
                {
                    Assert.That(property.CanWrite, Is.False,
                        type.Name + "에 public setter가 있다: " + property.Name);
                }

                var mutableFields = type
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(field => !field.IsInitOnly)
                    .ToArray();

                Assert.That(mutableFields, Is.Empty,
                    type.Name + "에 가변 public 필드가 있다");
            }

            // 공간 질의 계약은 read-only 형태(bool 반환 단일 질의)다.
            var queryMethods = typeof(ICharacterPlacementSpaceQuery).GetMethods();

            Assert.That(queryMethods.Length, Is.EqualTo(1));
            Assert.That(queryMethods[0].ReturnType, Is.EqualTo(typeof(bool)));
        }

        [Test]
        public void InteractionRuntime_DoesNotIntroduceBasicAttackDashWallJumpDoubleJumpOrShoot()
        {
            var forbidden = new[]
            {
                "Attack", "BasicAttack", "Melee", "Shoot",
                "Dash", "WallJump", "DoubleJump"
            };
            var runtimeTypes = typeof(CharacterCarryInteraction).Assembly.GetTypes();

            foreach (var type in runtimeTypes)
            {
                foreach (var keyword in forbidden)
                {
                    Assert.That(type.Name, Does.Not.Contain(keyword),
                        "런타임 타입 이름에 금지 개념이 있다: " + type.Name);

                    var memberNames = type
                        .GetMembers(BindingFlags.Public | BindingFlags.Instance
                            | BindingFlags.Static)
                        .Select(member => member.Name);

                    foreach (var memberName in memberNames)
                    {
                        Assert.That(memberName, Does.Not.Contain(keyword),
                            type.Name + " 공개 멤버에 금지 개념이 있다: " + memberName);
                    }
                }
            }

            // ActionId는 잠금 5종 그대로다 — 일반 공격 액션이 추가되지 않았다.
            Assert.That(Enum.GetNames(typeof(CharacterActionId)), Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));
        }
    }
}
