using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Movement;
using UnityEngine;

namespace StarNight.Character.Tests
{
    public sealed class CharacterLandingAndMovementBoundaryTests
    {
        [Test]
        public void LandingDetector_FiresOnlyOnAirborneToGroundedTransition()
        {
            var detector = new CharacterLandingDetector();

            Assert.That(detector.DetectLanding(false, true), Is.True);
            Assert.That(detector.DetectLanding(true, true), Is.False);
            Assert.That(detector.DetectLanding(false, false), Is.False);
            Assert.That(detector.DetectLanding(true, false), Is.False);
        }

        [Test]
        public void LandingDetector_ResetsJumpConsumedState()
        {
            var detector = new CharacterLandingDetector();
            var controller = new CharacterJumpController(CharacterJumpSettings.Default);
            var state = new CharacterJumpState();
            var velocity = Vector2.zero;

            // 점프해서 소비 상태로 만든다.
            state.NoteGrounded(0.0d);
            state.NoteJumpPressed(0.0d);
            controller.TryStartJump(state, true, 0.0d, ref velocity);

            Assert.That(state.JumpConsumed, Is.True);

            // 착지: airborne → grounded 전환에서 하강 속도 정리 + 점프 소비 reset.
            velocity = new Vector2(1f, -6f);
            var landed = detector.Step(state, false, true, 1.0d, ref velocity);

            Assert.That(landed, Is.True);
            Assert.That(velocity.y, Is.EqualTo(0f));
            Assert.That(velocity.x, Is.EqualTo(1f));
            Assert.That(state.JumpConsumed, Is.False);

            // 전환이 아니면 아무것도 하지 않는다.
            var unchangedVelocity = new Vector2(0f, -3f);
            var notLanded = detector.Step(state, true, true, 2.0d, ref unchangedVelocity);

            Assert.That(notLanded, Is.False);
            Assert.That(unchangedVelocity.y, Is.EqualTo(-3f));
        }

        [Test]
        public void MovementRuntime_DoesNotDeclareForbiddenMovementOrBasicAttackFeatures()
        {
            var forbidden = new[]
            {
                "WallJump", "Dash", "DoubleJump", "Attack", "BasicAttack", "Melee", "Shoot"
            };
            var runtimeTypes = typeof(CharacterJumpController).Assembly.GetTypes();

            foreach (var type in runtimeTypes)
            {
                foreach (var keyword in forbidden)
                {
                    Assert.That(type.Name, Does.Not.Contain(keyword),
                        "런타임 타입 이름에 금지 기능이 있다: " + type.Name);

                    var memberNames = type
                        .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                        .Select(member => member.Name);

                    foreach (var memberName in memberNames)
                    {
                        Assert.That(memberName, Does.Not.Contain(keyword),
                            type.Name + " 공개 멤버에 금지 기능이 있다: " + memberName);
                    }
                }
            }

            // 논리 행동 ID는 5개 그대로이며 일반 공격 값이 없다.
            var actionNames = Enum.GetNames(typeof(CharacterActionId));

            Assert.That(actionNames.Length, Is.EqualTo(5));
            Assert.That(actionNames, Does.Not.Contain("Attack"));
        }
    }
}
