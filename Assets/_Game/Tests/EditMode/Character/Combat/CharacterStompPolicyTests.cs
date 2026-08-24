using System.Linq;
using NUnit.Framework;
using StarNight.Character.Combat;

namespace StarNight.Character.Tests.Combat
{
    public sealed class CharacterStompPolicyTests
    {
        private static readonly CharacterContactClassification ValidStomp =
            new CharacterContactClassification(CharacterContactSide.Top, true);

        private static CharacterContactCombatPolicy CreatePolicy()
        {
            return new CharacterContactCombatPolicy(
                CharacterContactCombatSettings.Default);
        }

        [Test]
        public void Stomp_FirstStompOnNormalSmallEnemyProducesStunAndRebound()
        {
            var policy = CreatePolicy();
            var normalSmall = new CharacterEnemyContactTarget(3, true, true, false);

            var result = policy.Evaluate(in ValidStomp, in normalSmall);

            Assert.That(result.HasEnemyResult, Is.True);
            Assert.That(result.EnemyResult.Outcome,
                Is.EqualTo(CharacterStompOutcome.Stunned));
            Assert.That(result.EnemyResult.EnemyId, Is.EqualTo(3));
            Assert.That(result.EnemyResult.StunDurationSeconds,
                Is.EqualTo(policy.Settings.StunDurationSeconds));

            Assert.That(result.HasRebound, Is.True);
            Assert.That(result.Rebound.ReboundVerticalVelocity,
                Is.EqualTo(policy.Settings.StompReboundVelocity));
        }

        [Test]
        public void Stomp_SecondStompOnStunnedSmallEnemyProducesRemoval()
        {
            var policy = CreatePolicy();
            var stunnedSmall = new CharacterEnemyContactTarget(3, true, false, true);

            var result = policy.Evaluate(in ValidStomp, in stunnedSmall);

            Assert.That(result.HasEnemyResult, Is.True);
            Assert.That(result.EnemyResult.Outcome,
                Is.EqualTo(CharacterStompOutcome.Removed));
            Assert.That(result.HasRebound, Is.True);
        }

        [Test]
        public void Stomp_SeparatesPlayerReboundFromEnemyResult()
        {
            var policy = CreatePolicy();
            var normalSmall = new CharacterEnemyContactTarget(3, true, true, false);
            var result = policy.Evaluate(in ValidStomp, in normalSmall);

            // 적 결과와 플레이어 반동은 서로 다른 값 객체다.
            Assert.That(result.HasEnemyResult, Is.True);
            Assert.That(result.HasRebound, Is.True);

            // 적 결과 타입에는 플레이어 속도 필드가 없고,
            // 반동 타입에는 적 상태 필드가 없다(형태로 분리 보장).
            var enemyResultProperties = typeof(CharacterStompEnemyResult)
                .GetProperties().Select(property => property.Name).ToArray();
            var reboundProperties = typeof(CharacterStompReboundRequest)
                .GetProperties().Select(property => property.Name).ToArray();

            Assert.That(enemyResultProperties, Has.None.Contains("Velocity"));
            Assert.That(enemyResultProperties, Has.None.Contains("Rebound"));
            Assert.That(reboundProperties, Has.None.Contains("Enemy"));
            Assert.That(reboundProperties, Has.None.Contains("Outcome"));

            // 비소형 적 밟기: 기절/제거 흐름 없이 반동만(문서화된 동작).
            var large = new CharacterEnemyContactTarget(4, false, true, false);
            var largeResult = policy.Evaluate(in ValidStomp, in large);

            Assert.That(largeResult.HasEnemyResult, Is.False);
            Assert.That(largeResult.HasRebound, Is.True);
        }

        [Test]
        public void Stomp_ValidTopContactDoesNotCreatePlayerDamageCandidate()
        {
            var policy = CreatePolicy();
            var hostileSmall = new CharacterEnemyContactTarget(3, true, true, false);

            var result = policy.Evaluate(in ValidStomp, in hostileSmall);

            Assert.That(result.HasPlayerDamageCandidate, Is.False);

            // 상승/정지 상단 접촉(비밟기)도 피해 후보가 아니다(중립).
            var topNoStomp = new CharacterContactClassification(
                CharacterContactSide.Top, false);
            var neutral = policy.Evaluate(in topNoStomp, in hostileSmall);

            Assert.That(neutral.HasEnemyResult, Is.False);
            Assert.That(neutral.HasRebound, Is.False);
            Assert.That(neutral.HasPlayerDamageCandidate, Is.False);
        }
    }
}
