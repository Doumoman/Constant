using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Movement;
using UnityEngine;

namespace StarNight.Character.Tests.MovementCourses
{
    public sealed class ForbiddenMovementRuleTests
    {
        private static Type[] RuntimeTypes()
        {
            return typeof(CharacterJumpController).Assembly.GetTypes();
        }

        private static void AssertKeywordAbsent(string keyword)
        {
            foreach (var type in RuntimeTypes())
            {
                Assert.That(type.Name, Does.Not.Contain(keyword),
                    "런타임 타입 이름에 금지 개념이 있다: " + type.Name);

                var memberNames = type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(member => member.Name);

                foreach (var memberName in memberNames)
                {
                    Assert.That(memberName, Does.Not.Contain(keyword),
                        type.Name + " 공개 멤버에 금지 개념이 있다: " + memberName);
                }
            }
        }

        [Test]
        public void ForbiddenMovement_NoWallJumpDashOrDoubleJumpTypesOrMembers()
        {
            AssertKeywordAbsent("WallJump");
            AssertKeywordAbsent("Dash");
            AssertKeywordAbsent("DoubleJump");
        }

        [Test]
        public void ForbiddenMovement_NoBasicAttackMeleeOrShootActions()
        {
            AssertKeywordAbsent("Attack");
            AssertKeywordAbsent("Melee");
            AssertKeywordAbsent("Shoot");

            var actionNames = Enum.GetNames(typeof(CharacterActionId));

            Assert.That(actionNames, Does.Not.Contain("Attack"));
            Assert.That(actionNames, Does.Not.Contain("BasicAttack"));
            Assert.That(actionNames, Does.Not.Contain("Melee"));
            Assert.That(actionNames, Does.Not.Contain("Shoot"));
        }

        [Test]
        public void ForbiddenMovement_CharacterActionIdRemainsLockedToFiveValues()
        {
            var actionNames = Enum.GetNames(typeof(CharacterActionId));

            Assert.That(actionNames.Length, Is.EqualTo(5));
            Assert.That(actionNames, Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));
        }

        [Test]
        public void ForbiddenMovement_StillHasNoWallJumpDashDoubleJumpOrBasicAttack()
        {
            // CHAR02_03 교정 이후에도 금지 이동·일반 공격이 추가되지 않았다.
            AssertKeywordAbsent("WallJump");
            AssertKeywordAbsent("Dash");
            AssertKeywordAbsent("DoubleJump");
            AssertKeywordAbsent("Attack");
            AssertKeywordAbsent("Melee");
            AssertKeywordAbsent("Shoot");

            Assert.That(Enum.GetNames(typeof(CharacterActionId)), Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));
        }

        [Test]
        public void ForbiddenMovement_SecondJumpStillFailsBeforeGroundedAgain()
        {
            var controller = new CharacterJumpController(CharacterJumpSettings.Default);
            var state = new CharacterJumpState();
            var velocity = Vector2.zero;

            state.NoteGrounded(0.0d);
            state.NoteJumpPressed(0.0d);

            Assert.That(controller.TryStartJump(state, true, 0.0d, ref velocity), Is.True);

            // 공중 재입력 — 코요테 창 안이라도 grounded 재획득 전 두 번째 점프는 불가.
            state.NoteJumpPressed(0.05d);

            Assert.That(controller.TryStartJump(state, false, 0.05d, ref velocity), Is.False);

            state.NoteJumpPressed(0.2d);

            Assert.That(controller.TryStartJump(state, false, 0.2d, ref velocity), Is.False);

            // grounded 재획득 후에만 새 점프가 가능하다.
            state.NoteGrounded(1.0d);
            state.NoteJumpPressed(1.0d);
            velocity = Vector2.zero;

            Assert.That(controller.TryStartJump(state, true, 1.0d, ref velocity), Is.True);
        }
    }
}
