using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Movement;

namespace StarNight.Character.Tests
{
    public sealed class CharacterMovementBoundaryTests
    {
        private static Type[] RuntimeTypes()
        {
            return typeof(CharacterGroundMotor).Assembly.GetTypes();
        }

        private static string[] PublicMemberNames(Type type)
        {
            return type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(member => member.Name)
                .ToArray();
        }

        [Test]
        public void MovementRuntime_DoesNotDeclareJumpGravityAirControlOrLandingTypes()
        {
            // CHAR01_03 업데이트: Jump/Gravity/AirControl/Landing 개념은 이제
            // 승인된 Movement namespace 소관이다. 이 경계 테스트는 해당 개념이
            // Input/State namespace 타입으로 새어 나가지 않았음을 검증한다.
            var keywords = new[] { "Jump", "Gravity", "AirControl", "Landing" };
            var nonMovementTypes = RuntimeTypes()
                .Where(type =>
                    type.Namespace == "StarNight.Character.Input"
                    || type.Namespace == "StarNight.Character.State")
                .ToArray();

            Assert.That(nonMovementTypes, Is.Not.Empty);

            foreach (var type in nonMovementTypes)
            {
                foreach (var keyword in keywords)
                {
                    Assert.That(type.Name, Does.Not.Contain(keyword),
                        "Input/State 타입 이름에 Movement 소관 개념이 있다: " + type.Name);
                }
            }

            // 승인된 CHAR01_03 타입은 Movement namespace에 정확히 존재한다.
            var movementTypeNames = RuntimeTypes()
                .Where(type => type.Namespace == "StarNight.Character.Movement")
                .Select(type => type.Name)
                .ToArray();

            Assert.That(movementTypeNames, Does.Contain("CharacterJumpController"));
            Assert.That(movementTypeNames, Does.Contain("CharacterGravityMotor"));
            Assert.That(movementTypeNames, Does.Contain("CharacterAirControlMotor"));
            Assert.That(movementTypeNames, Does.Contain("CharacterLandingDetector"));
        }

        [Test]
        public void MovementRuntime_DoesNotDeclareForbiddenMovementFeatures()
        {
            var forbidden = new[] { "WallJump", "Dash", "DoubleJump", "Attack", "Melee" };

            foreach (var type in RuntimeTypes())
            {
                foreach (var keyword in forbidden)
                {
                    Assert.That(type.Name, Does.Not.Contain(keyword),
                        "런타임 타입 이름에 금지 기능이 있다: " + type.Name);

                    foreach (var memberName in PublicMemberNames(type))
                    {
                        Assert.That(memberName, Does.Not.Contain(keyword),
                            type.Name + " 공개 멤버에 금지 기능이 있다: " + memberName);
                    }
                }
            }

            // 논리 행동 ID도 5개 그대로다(일반 공격 미추가 회귀 확인).
            Assert.That(Enum.GetNames(typeof(CharacterActionId)).Length, Is.EqualTo(5));
        }
    }
}
