using System.Linq;
using NUnit.Framework;
using StarNight.Character.Survival;

namespace StarNight.Character.Tests.Survival
{
    public sealed class CharacterDeathAndRunFailureTests
    {
        private const int PlayerId = 777;
        private const int EnemyId = 21;

        [Test]
        public void Death_EnemyDeathDoesNotCreatePlayerRunFailure()
        {
            // 적 사망 요청 → 런 실패 없음.
            var enemyDeath = new CharacterDeathRequest(
                EnemyId, CharacterSurvivalTargetKind.Enemy,
                CharacterDamageSourceKind.Explosion, 9);

            CharacterRunFailureRequest runFailure;
            Assert.That(CharacterRunFailurePolicy.TryCreateFromDeath(
                in enemyDeath, "sector:0/chunk:0", out runFailure), Is.False);

            // 적 낙사도 런 실패가 아니다.
            Assert.That(CharacterHazardPolicy.TryCreateVoidRunFailure(
                EnemyId, CharacterSurvivalTargetKind.Enemy, null, out runFailure),
                Is.False);

            // 통합 정책 경유로도 동일: 적 치명 피해 → 사망 요청은 있지만
            // 그 사망으로 런 실패는 만들어지지 않는다.
            var settings = CharacterSurvivalSettings.Default;
            var enemy = CharacterHealthState.CreateFull(
                EnemyId, CharacterSurvivalTargetKind.Enemy, 1);
            var lethal = CharacterHealthDamagePolicy.ApplyDamage(
                in enemy,
                new CharacterSurvivalDamageRequest(
                    CharacterDamageSourceKind.ThrownObject, 42, EnemyId,
                    CharacterSurvivalTargetKind.Enemy, 1,
                    UnityEngine.Vector2.zero, false),
                in settings);

            Assert.That(lethal.HasDeathRequest, Is.True);
            var enemyLethalDeath = lethal.DeathRequest;
            Assert.That(CharacterRunFailurePolicy.TryCreateFromDeath(
                in enemyLethalDeath, null, out runFailure), Is.False);
        }

        [Test]
        public void RunFailure_PlayerDeathCreatesRunFailureRequest()
        {
            var playerDeath = new CharacterDeathRequest(
                PlayerId, CharacterSurvivalTargetKind.Player,
                CharacterDamageSourceKind.EnemyContact, 21);

            CharacterRunFailureRequest runFailure;
            Assert.That(CharacterRunFailurePolicy.TryCreateFromDeath(
                in playerDeath, "sector:1/chunk:4", out runFailure), Is.True);

            Assert.That(runFailure.Reason,
                Is.EqualTo(CharacterRunFailureReason.PlayerDeath));
            Assert.That(runFailure.ActorId, Is.EqualTo(PlayerId));
            Assert.That(runFailure.ReturnDestinationToken,
                Is.EqualTo("sector:1/chunk:4"));

            // 치명 피해 → 사망 요청 → 런 실패 요청 전체 사슬.
            var settings = CharacterSurvivalSettings.Default;
            var player = CharacterHealthState.CreateFull(
                PlayerId, CharacterSurvivalTargetKind.Player, 4);
            var lethal = CharacterHealthDamagePolicy.ApplyDamage(
                in player,
                new CharacterSurvivalDamageRequest(
                    CharacterDamageSourceKind.Explosion, 9, PlayerId,
                    CharacterSurvivalTargetKind.Player, 4,
                    UnityEngine.Vector2.up, false),
                in settings);

            Assert.That(lethal.HasDeathRequest, Is.True);
            var playerLethalDeath = lethal.DeathRequest;
            Assert.That(CharacterRunFailurePolicy.TryCreateFromDeath(
                in playerLethalDeath, null, out runFailure), Is.True);
            Assert.That(runFailure.Reason,
                Is.EqualTo(CharacterRunFailureReason.PlayerDeath));
        }

        [Test]
        public void RunFailure_ReturnDestinationIsDataOnlyAndDoesNotReloadSceneOrSave()
        {
            // 복귀 목적지는 불투명 토큰 데이터일 뿐이다.
            var withToken = new CharacterRunFailureRequest(
                CharacterRunFailureReason.PlayerDeath, PlayerId, "sector:2/chunk:5");
            Assert.That(withToken.HasReturnDestination, Is.True);
            Assert.That(withToken.ReturnDestinationToken, Is.EqualTo("sector:2/chunk:5"));

            var withoutToken = new CharacterRunFailureRequest(
                CharacterRunFailureReason.VoidOrOutOfBounds, PlayerId, null);
            Assert.That(withoutToken.HasReturnDestination, Is.False);

            // Survival 공개 표면에 씬 리로드/세이브/HUD/transform 이동 명명이
            // 없다 — 데이터 전용 계약임을 리플렉션으로 보증한다.
            var forbiddenNames = new[]
            {
                "LoadScene", "Reload", "SceneManager", "Save",
                "PlayerPrefs", "Hud", "Teleport", "Transform"
            };
            var survivalTypes = typeof(CharacterRunFailureRequest).Assembly
                .GetTypes()
                .Where(type => type.Namespace == "StarNight.Character.Survival")
                .ToArray();

            foreach (var type in survivalTypes)
            {
                var names = type
                    .GetMembers(System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.Static)
                    .Select(member => member.Name)
                    .Concat(new[] { type.Name });

                foreach (var name in names)
                {
                    foreach (var keyword in forbiddenNames)
                    {
                        Assert.That(name, Does.Not.Contain(keyword),
                            type.Name + " 표면에 부작용 명명이 있다: " + name);
                    }
                }
            }
        }
    }
}
