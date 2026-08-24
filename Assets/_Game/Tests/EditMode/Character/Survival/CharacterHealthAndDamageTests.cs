using NUnit.Framework;
using StarNight.Character.Survival;
using UnityEngine;

namespace StarNight.Character.Tests.Survival
{
    public sealed class CharacterHealthAndDamageTests
    {
        private const int PlayerId = 777;
        private const int EnemyId = 21;
        private const float Tolerance = 1e-4f;

        private static CharacterSurvivalDamageRequest Damage(
            int targetId,
            CharacterSurvivalTargetKind kind,
            int amount,
            bool bypass = false,
            CharacterDamageSourceKind source = CharacterDamageSourceKind.EnemyContact,
            int sourceId = 5)
        {
            return new CharacterSurvivalDamageRequest(
                source, sourceId, targetId, kind, amount, Vector2.zero, bypass);
        }

        [Test]
        public void Health_DamageReducesHealthAndClampsAtZero()
        {
            var settings = CharacterSurvivalSettings.Default; // 최대 체력 4
            var state = CharacterHealthState.CreateFull(
                PlayerId, CharacterSurvivalTargetKind.Player, settings.MaxPlayerHealth);

            Assert.That(state.CurrentHealth, Is.EqualTo(4));

            // 1 피해 → 3. 입력 상태는 불변이고 결과에 새 상태가 실린다.
            var first = CharacterHealthDamagePolicy.ApplyDamage(
                in state, Damage(PlayerId, CharacterSurvivalTargetKind.Player, 1),
                in settings);

            Assert.That(first.AppliedAmount, Is.EqualTo(1));
            Assert.That(first.NewState.CurrentHealth, Is.EqualTo(3));
            Assert.That(state.CurrentHealth, Is.EqualTo(4));

            // 피격 후 무적을 지나 보낸 뒤 과대 피해 → 0에서 clamp(음수 없음).
            var ready = first.NewState.TickInvulnerability(1.0f);
            var second = CharacterHealthDamagePolicy.ApplyDamage(
                in ready, Damage(PlayerId, CharacterSurvivalTargetKind.Player, 10),
                in settings);

            Assert.That(second.NewState.CurrentHealth, Is.EqualTo(0));
            Assert.That(second.AppliedAmount, Is.EqualTo(3));

            // 상태 생성자도 검증·clamp한다: current > max → max로.
            var clamped = new CharacterHealthState(
                PlayerId, CharacterSurvivalTargetKind.Player, 10, 4, -1f);
            Assert.That(clamped.CurrentHealth, Is.EqualTo(4));
            Assert.That(clamped.InvulnerabilityRemainingSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void Health_NonPositiveDamageCreatesNoChange()
        {
            var settings = CharacterSurvivalSettings.Default;
            var state = CharacterHealthState.CreateFull(
                EnemyId, CharacterSurvivalTargetKind.Enemy, 2);

            foreach (var amount in new[] { 0, -3 })
            {
                var result = CharacterHealthDamagePolicy.ApplyDamage(
                    in state,
                    Damage(EnemyId, CharacterSurvivalTargetKind.Enemy, amount),
                    in settings);

                Assert.That(result.AppliedAmount, Is.EqualTo(0));
                Assert.That(result.NewState.CurrentHealth, Is.EqualTo(2));
                Assert.That(result.WasSuppressedByInvulnerability, Is.False);
                Assert.That(result.HasDeathRequest, Is.False);
            }

            // 대상 불일치 요청도 변화 없음(방어적).
            var mismatch = CharacterHealthDamagePolicy.ApplyDamage(
                in state, Damage(999, CharacterSurvivalTargetKind.Enemy, 1),
                in settings);
            Assert.That(mismatch.AppliedAmount, Is.EqualTo(0));
        }

        [Test]
        public void Health_InvulnerabilitySuppressesDamageUnlessBypassed()
        {
            var settings = CharacterSurvivalSettings.Default;
            var invulnerable = new CharacterHealthState(
                PlayerId, CharacterSurvivalTargetKind.Player, 4, 4, 0.5f);

            // 무적 중 일반 피해 → 억제, 체력 불변.
            var suppressed = CharacterHealthDamagePolicy.ApplyDamage(
                in invulnerable,
                Damage(PlayerId, CharacterSurvivalTargetKind.Player, 1),
                in settings);

            Assert.That(suppressed.WasSuppressedByInvulnerability, Is.True);
            Assert.That(suppressed.AppliedAmount, Is.EqualTo(0));
            Assert.That(suppressed.NewState.CurrentHealth, Is.EqualTo(4));

            // 명시적 bypass 요청(스키마 기본 false)만 무적을 관통한다.
            var pierced = CharacterHealthDamagePolicy.ApplyDamage(
                in invulnerable,
                Damage(PlayerId, CharacterSurvivalTargetKind.Player, 1, bypass: true),
                in settings);

            Assert.That(pierced.WasSuppressedByInvulnerability, Is.False);
            Assert.That(pierced.AppliedAmount, Is.EqualTo(1));
            Assert.That(pierced.NewState.CurrentHealth, Is.EqualTo(3));

            // 무적 시간 흐름: 감소·음수 delta clamp·0 바닥.
            var ticked = invulnerable.TickInvulnerability(0.2f);
            Assert.That(ticked.InvulnerabilityRemainingSeconds,
                Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(ticked.TickInvulnerability(-5f).InvulnerabilityRemainingSeconds,
                Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(ticked.TickInvulnerability(0.4f).InvulnerabilityRemainingSeconds,
                Is.EqualTo(0f));

            // 무적이 끝나면 일반 피해가 다시 적용되고, 비치명 플레이어 피격은
            // 새 무적(0.8s 기준선)을 부여한다.
            var expired = invulnerable.TickInvulnerability(0.5f);
            var applied = CharacterHealthDamagePolicy.ApplyDamage(
                in expired,
                Damage(PlayerId, CharacterSurvivalTargetKind.Player, 1),
                in settings);

            Assert.That(applied.AppliedAmount, Is.EqualTo(1));
            Assert.That(applied.NewState.InvulnerabilityRemainingSeconds,
                Is.EqualTo(settings.PostHitInvulnerabilitySeconds).Within(Tolerance));
        }

        [Test]
        public void Health_LethalDamageCreatesDeathRequest()
        {
            var settings = CharacterSurvivalSettings.Default;

            // 적: 체력 1에 1 피해 → 사망 요청(actor/kind/cause/sourceId 기록).
            var enemy = CharacterHealthState.CreateFull(
                EnemyId, CharacterSurvivalTargetKind.Enemy, 1);
            var enemyResult = CharacterHealthDamagePolicy.ApplyDamage(
                in enemy,
                Damage(EnemyId, CharacterSurvivalTargetKind.Enemy, 1,
                    source: CharacterDamageSourceKind.Explosion, sourceId: 9),
                in settings);

            Assert.That(enemyResult.HasDeathRequest, Is.True);
            Assert.That(enemyResult.DeathRequest.ActorId, Is.EqualTo(EnemyId));
            Assert.That(enemyResult.DeathRequest.TargetKind,
                Is.EqualTo(CharacterSurvivalTargetKind.Enemy));
            Assert.That(enemyResult.DeathRequest.Cause,
                Is.EqualTo(CharacterDamageSourceKind.Explosion));
            Assert.That(enemyResult.DeathRequest.SourceId, Is.EqualTo(9));

            // 플레이어 치명: 체력 4에 4 피해 — 사망 요청, 무적 부여 없음.
            var player = CharacterHealthState.CreateFull(
                PlayerId, CharacterSurvivalTargetKind.Player, 4);
            var playerResult = CharacterHealthDamagePolicy.ApplyDamage(
                in player,
                Damage(PlayerId, CharacterSurvivalTargetKind.Player, 4),
                in settings);

            Assert.That(playerResult.HasDeathRequest, Is.True);
            Assert.That(playerResult.NewState.IsDepleted, Is.True);
            Assert.That(playerResult.NewState.InvulnerabilityRemainingSeconds,
                Is.EqualTo(0f));

            // 이미 소진된 상태에는 재차 사망 요청이 나오지 않는다.
            var depleted = playerResult.NewState;
            var again = CharacterHealthDamagePolicy.ApplyDamage(
                in depleted,
                Damage(PlayerId, CharacterSurvivalTargetKind.Player, 1),
                in settings);
            Assert.That(again.HasDeathRequest, Is.False);
            Assert.That(again.AppliedAmount, Is.EqualTo(0));
        }

        [Test]
        public void Health_NonLethalDamageCreatesNoDeathOrRunFailure()
        {
            var settings = CharacterSurvivalSettings.Default;
            var player = CharacterHealthState.CreateFull(
                PlayerId, CharacterSurvivalTargetKind.Player, 4);

            var result = CharacterHealthDamagePolicy.ApplyDamage(
                in player,
                Damage(PlayerId, CharacterSurvivalTargetKind.Player, 1),
                in settings);

            // 비치명 → 사망 요청 없음 → 런 실패로 이어질 사망 자체가 없다.
            Assert.That(result.HasDeathRequest, Is.False);
            Assert.That(result.NewState.CurrentHealth, Is.EqualTo(3));
            Assert.That(result.NewState.IsDepleted, Is.False);
        }
    }
}
