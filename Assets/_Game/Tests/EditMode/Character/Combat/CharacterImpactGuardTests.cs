using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Character.Combat;
using StarNight.Character.Input;
using UnityEngine;

namespace StarNight.Character.Tests.Combat
{
    public sealed class CharacterImpactGuardTests
    {
        [Test]
        public void Impact_RuntimeDoesNotUseAnimatorEventsAsImpactAuthority()
        {
            var runtimeAssembly = typeof(CharacterImpactPolicy).Assembly;
            var referenced = runtimeAssembly.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .ToArray();

            Assert.That(referenced, Does.Not.Contain("UnityEngine.AnimationModule"));

            // 임팩트 판정은 결정적이다 — 같은 입력이면 같은 출력.
            var policy = new CharacterImpactPolicy(CharacterImpactSettings.Default);
            var source = new CharacterImpactSource(
                42, 777, true, CharacterImpactSourceKind.ThrownObject,
                new Vector2(6f, 0f), 0f);
            var target = new CharacterImpactTarget(
                CharacterImpactTargetKind.Enemy, 9, true);

            var first = policy.Evaluate(in source, in target);
            var second = policy.Evaluate(in source, in target);

            Assert.That(second.HasEnemyDamageCandidate,
                Is.EqualTo(first.HasEnemyDamageCandidate));
            Assert.That(second.EnemyDamageCandidate.Amount,
                Is.EqualTo(first.EnemyDamageCandidate.Amount));

            // 임팩트 표면에 Animator/Animation 타입이 등장하지 않는다.
            var impactTypes = new[]
            {
                typeof(CharacterImpactPolicy),
                typeof(CharacterImpactSource),
                typeof(CharacterImpactTarget),
                typeof(CharacterImpactResult)
            };

            foreach (var type in impactTypes)
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
        }

        [Test]
        public void NoBasicAttack_ActionSurfaceRemainsLocked()
        {
            // 논리 행동 ID는 잠금 5종 그대로다.
            Assert.That(Enum.GetNames(typeof(CharacterActionId)), Is.EquivalentTo(
                new[] { "Jump", "Action", "SafeDrop", "Bomb", "Rope" }));

            // 입력 스냅샷 표면에도 공격 계열 intent가 없다.
            var snapshotMembers = typeof(CharacterInputSnapshot)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Select(member => member.Name)
                .ToArray();

            Assert.That(snapshotMembers, Has.None.Contains("Attack"));
            Assert.That(snapshotMembers, Has.None.Contains("Melee"));
            Assert.That(snapshotMembers, Has.None.Contains("Shoot"));
        }

        [Test]
        public void NoBasicAttack_RuntimeDoesNotIntroduceForbiddenMovementOrAttackFeatures()
        {
            var forbidden = new[]
            {
                "BasicAttack", "Melee", "Shoot",
                "Dash", "WallJump", "DoubleJump"
            };
            var runtimeTypes = typeof(CharacterImpactPolicy).Assembly.GetTypes();

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

            // "Attack" 단독 키워드도 타입 이름 기준으로 부재 확인
            // (멤버 스캔은 위 BasicAttack 계열로 커버 — Impact 계약에 공격 명명 없음).
            foreach (var type in runtimeTypes)
            {
                Assert.That(type.Name, Does.Not.Contain("Attack"));
            }
        }
    }
}
