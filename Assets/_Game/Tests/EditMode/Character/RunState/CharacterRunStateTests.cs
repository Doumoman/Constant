using NUnit.Framework;
using StarNight.Character.RunState;
using StarNight.Character.Survival;
using UnityEngine;

namespace StarNight.Character.Tests.RunState
{
    public sealed class CharacterRunStateTests
    {
        private const int PlayerId = 777;
        private const int EnemyId = 21;

        private static CharacterRunState ActiveRun()
        {
            var health = CharacterHealthState.CreateFull(
                PlayerId, CharacterSurvivalTargetKind.Player, 4);
            var inventory = CharacterRunInventoryState.CreateStarting(
                PlayerId, CharacterRunStateSettings.Default);
            return CharacterRunState.CreateActive(PlayerId, in health, in inventory);
        }

        [Test]
        public void RunState_HealthSnapshotReflectsSurvivalState()
        {
            var run = ActiveRun();
            Assert.That(run.Status, Is.EqualTo(CharacterRunStatus.Active));
            Assert.That(run.Health.CurrentHealth, Is.EqualTo(4));

            // Survival 피해 적용 결과의 새 체력 상태를 런 상태에 반영한다.
            var settings = CharacterSurvivalSettings.Default;
            var damage = new CharacterSurvivalDamageRequest(
                CharacterDamageSourceKind.EnemyContact, EnemyId, PlayerId,
                CharacterSurvivalTargetKind.Player, 1, Vector2.zero, false);
            var health = run.Health;
            var applied = CharacterHealthDamagePolicy.ApplyDamage(
                in health, in damage, in settings);

            var updated = run.WithHealth(applied.NewState);

            Assert.That(updated.Health.CurrentHealth, Is.EqualTo(3));
            Assert.That(updated.Health.IsInvulnerable, Is.True);
            Assert.That(updated.Status, Is.EqualTo(CharacterRunStatus.Active));

            // 입력 런 상태는 불변이다.
            Assert.That(run.Health.CurrentHealth, Is.EqualTo(4));
        }

        [Test]
        public void RunState_PlayerRunFailureMarksRunFailedWithReturnToken()
        {
            var run = ActiveRun();

            // CHAR05_03 사슬: 플레이어 사망 → 런 실패 요청 → 런 상태 Failed.
            var playerDeath = new CharacterDeathRequest(
                PlayerId, CharacterSurvivalTargetKind.Player,
                CharacterDamageSourceKind.Explosion, 9);

            CharacterRunFailureRequest failure;
            Assert.That(CharacterRunFailurePolicy.TryCreateFromDeath(
                in playerDeath, "sector:1/chunk:4", out failure), Is.True);

            var failed = run.ApplyRunFailure(in failure);

            Assert.That(failed.Status, Is.EqualTo(CharacterRunStatus.Failed));
            Assert.That(failed.HasReturnDestination, Is.True);
            Assert.That(failed.ReturnDestinationToken, Is.EqualTo("sector:1/chunk:4"));

            // 입력 런 상태는 불변(Active 유지).
            Assert.That(run.Status, Is.EqualTo(CharacterRunStatus.Active));
        }

        [Test]
        public void RunState_NonPlayerDeathDoesNotFailPlayerRun()
        {
            var run = ActiveRun();

            // 적 사망은 CHAR05_03 정책상 런 실패 요청 자체를 만들지 못한다.
            var enemyDeath = new CharacterDeathRequest(
                EnemyId, CharacterSurvivalTargetKind.Enemy,
                CharacterDamageSourceKind.Stomp, PlayerId);

            CharacterRunFailureRequest failure;
            Assert.That(CharacterRunFailurePolicy.TryCreateFromDeath(
                in enemyDeath, null, out failure), Is.False);
            Assert.That(run.Status, Is.EqualTo(CharacterRunStatus.Active));

            // 다른 액터를 향한 런 실패 요청도 이 런을 실패시키지 않는다.
            var foreignFailure = new CharacterRunFailureRequest(
                CharacterRunFailureReason.PlayerDeath, 999, "sector:0/chunk:0");
            var unchanged = run.ApplyRunFailure(in foreignFailure);

            Assert.That(unchanged.Status, Is.EqualTo(CharacterRunStatus.Active));
            Assert.That(unchanged.HasReturnDestination, Is.False);
        }
    }
}
