using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Combat;
using StarNight.Character.Input;
using StarNight.Character.Interaction;
using UnityEngine;

namespace StarNight.Character.Tests.Combat
{
    public sealed class CharacterContactDamageAndGuardTests
    {
        private static CharacterContactCombatPolicy CreatePolicy()
        {
            return new CharacterContactCombatPolicy(
                CharacterContactCombatSettings.Default);
        }

        [Test]
        public void ContactDamage_SideContactCreatesDamageRequestWithoutApplyingHealth()
        {
            var policy = CreatePolicy();
            var hostile = new CharacterEnemyContactTarget(7, true, true, false);
            var side = new CharacterContactClassification(
                CharacterContactSide.Side, false);

            var result = policy.Evaluate(in side, in hostile);

            Assert.That(result.HasPlayerDamageCandidate, Is.True);
            Assert.That(result.PlayerDamageCandidate.SourceEnemyId, Is.EqualTo(7));
            Assert.That(result.PlayerDamageCandidate.Amount,
                Is.EqualTo(policy.Settings.ContactDamageAmount));

            // 피해 후보는 요청 값 객체다 — 체력 차감 메서드·필드가 없다(CHAR05 소관).
            var candidateMembers = typeof(CharacterPlayerDamageCandidate)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Select(member => member.Name)
                .ToArray();

            Assert.That(candidateMembers, Has.None.Contains("Health"));
            Assert.That(candidateMembers, Has.None.Contains("Apply"));

            // 기절 등 비적대 대상의 측면 접촉은 비피해(문서화된 동작).
            var stunnedNonHostile = new CharacterEnemyContactTarget(8, true, false, true);
            var neutral = policy.Evaluate(in side, in stunnedNonHostile);

            Assert.That(neutral.HasPlayerDamageCandidate, Is.False);
        }

        [Test]
        public void ContactDamage_BottomContactCreatesDamageRequestWithoutApplyingHealth()
        {
            var policy = CreatePolicy();
            var hostile = new CharacterEnemyContactTarget(7, true, true, false);
            var bottom = new CharacterContactClassification(
                CharacterContactSide.Bottom, false);

            var result = policy.Evaluate(in bottom, in hostile);

            Assert.That(result.HasPlayerDamageCandidate, Is.True);
            Assert.That(result.PlayerDamageCandidate.ContactSide,
                Is.EqualTo(CharacterContactSide.Bottom));
            Assert.That(result.HasEnemyResult, Is.False);
            Assert.That(result.HasRebound, Is.False);
        }

        [Test]
        public void StunnedSmallEnemy_CanBeExposedAsCarryCandidate()
        {
            // 밟기로 기절한 소형 적이 CHAR04_01 휴대 후보 계약으로 노출된다.
            var stunned = new CharacterEnemyContactTarget(21, true, false, true);
            CharacterCarryCandidate candidate;

            Assert.That(CharacterStunnedEnemyCarryBridge.TryCreateCarryCandidate(
                in stunned, new Vector2(2f, 0f), 1f, 1f, 0, out candidate), Is.True);
            Assert.That(candidate.Kind,
                Is.EqualTo(CharacterCarryCandidateKind.StunnedSmallEnemy));
            Assert.That(candidate.Id, Is.EqualTo(21));
            Assert.That(candidate.IsEligibleForCarry, Is.True);

            // 실제 휴대 슬롯에 들어간다(계약 호환).
            var interaction = new CharacterCarryInteraction(
                CharacterCarryInteractionSettings.Default,
                new AlwaysFreePlacement(),
                1);

            Assert.That(interaction.TryPickUp(in candidate), Is.True);
            Assert.That(interaction.HeldKind,
                Is.EqualTo(CharacterCarryCandidateKind.StunnedSmallEnemy));

            // 기절하지 않았거나 소형이 아니면 노출되지 않는다.
            var normal = new CharacterEnemyContactTarget(22, true, true, false);

            Assert.That(CharacterStunnedEnemyCarryBridge.TryCreateCarryCandidate(
                in normal, Vector2.zero, 1f, 1f, 0, out candidate), Is.False);

            var largeStunned = new CharacterEnemyContactTarget(23, false, false, true);

            Assert.That(CharacterStunnedEnemyCarryBridge.TryCreateCarryCandidate(
                in largeStunned, Vector2.zero, 1f, 1f, 0, out candidate), Is.False);
        }

        [Test]
        public void CombatRuntime_DoesNotUseAnimatorEventsAsDamageAuthority()
        {
            var runtimeAssembly = typeof(CharacterContactCombatPolicy).Assembly;

            // 런타임은 Animation 모듈을 참조하지 않는다.
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            Assert.That(referenced, Does.Not.Contain("UnityEngine.AnimationModule"));

            // Combat 표면에 Animator/Animation 타입이 등장하지 않는다.
            var combatTypes = runtimeAssembly.GetTypes()
                .Where(type => type.Namespace == "StarNight.Character.Combat")
                .ToArray();

            Assert.That(combatTypes, Is.Not.Empty);

            foreach (var type in combatTypes)
            {
                var memberTypeNames = type
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

                foreach (var typeName in memberTypeNames)
                {
                    Assert.That(typeName, Does.Not.Contain("Animator"));
                    Assert.That(typeName, Does.Not.Contain("Animation"));
                }
            }

            // 판정은 결정적이다 — 같은 입력이면 같은 출력(권한이 콜백/이벤트에 없음).
            var policy = CreatePolicy();
            var stomp = new CharacterContactClassification(CharacterContactSide.Top, true);
            var enemy = new CharacterEnemyContactTarget(3, true, true, false);

            var first = policy.Evaluate(in stomp, in enemy);
            var second = policy.Evaluate(in stomp, in enemy);

            Assert.That(second.EnemyResult.Outcome, Is.EqualTo(first.EnemyResult.Outcome));
            Assert.That(second.Rebound.ReboundVerticalVelocity,
                Is.EqualTo(first.Rebound.ReboundVerticalVelocity));
        }

        [Test]
        public void CombatRuntime_DoesNotIntroduceBasicAttackDashWallJumpDoubleJumpOrShoot()
        {
            var forbidden = new[]
            {
                "Attack", "BasicAttack", "Melee", "Shoot",
                "Dash", "WallJump", "DoubleJump"
            };
            var runtimeTypes = typeof(CharacterContactCombatPolicy).Assembly.GetTypes();

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

            Assert.That(Enum.GetNames(typeof(CharacterActionId)), Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));
        }

        private sealed class AlwaysFreePlacement : ICharacterPlacementSpaceQuery
        {
            public bool IsPlacementFree(Vector2 position)
            {
                return true;
            }
        }
    }
}
