using NUnit.Framework;
using StarNight.Character.Combat;
using UnityEngine;

namespace StarNight.Character.Tests.Combat
{
    public sealed class CharacterContactClassifierTests
    {
        private static readonly Vector2 PlayerHalf = new Vector2(0.36f, 0.45f);
        private static readonly Vector2 EnemyHalf = new Vector2(0.5f, 0.5f);

        private static CharacterContactClassification Classify(
            Vector2 playerCenter, float verticalVelocity, Vector2 enemyCenter)
        {
            return CharacterEnemyContactClassifier.Classify(
                playerCenter, PlayerHalf, verticalVelocity, enemyCenter, EnemyHalf);
        }

        [Test]
        public void ContactClassifier_DescendingTopContactIsValidStomp()
        {
            // 플레이어가 적 위에서 하강 중 겹침 — 유효 밟기.
            var classification = Classify(
                new Vector2(0f, 0.85f), -3f, Vector2.zero);

            Assert.That(classification.Side, Is.EqualTo(CharacterContactSide.Top));
            Assert.That(classification.IsValidStomp, Is.True);
        }

        [Test]
        public void ContactClassifier_RisingOrStationaryTopContactIsNotStomp()
        {
            // 상승 중 상단 접촉 — 밟기 아님.
            var rising = Classify(new Vector2(0f, 0.85f), 2f, Vector2.zero);

            Assert.That(rising.Side, Is.EqualTo(CharacterContactSide.Top));
            Assert.That(rising.IsValidStomp, Is.False);

            // 정지(0) 상단 접촉 — 밟기 아님.
            var stationary = Classify(new Vector2(0f, 0.85f), 0f, Vector2.zero);

            Assert.That(stationary.IsValidStomp, Is.False);

            // 분리 상태 — 전투 이벤트 없음.
            var separated = Classify(new Vector2(5f, 5f), -3f, Vector2.zero);

            Assert.That(separated.Side, Is.EqualTo(CharacterContactSide.None));
        }

        [Test]
        public void ContactClassifier_SideAndBottomContactBecomePlayerDamageCandidate()
        {
            var policy = new CharacterContactCombatPolicy(
                CharacterContactCombatSettings.Default);
            var hostile = new CharacterEnemyContactTarget(9, true, true, false);

            // 측면 접촉 → 플레이어 피해 후보.
            var side = Classify(new Vector2(0.8f, 0f), 0f, Vector2.zero);

            Assert.That(side.Side, Is.EqualTo(CharacterContactSide.Side));

            var sideResult = policy.Evaluate(in side, in hostile);

            Assert.That(sideResult.HasPlayerDamageCandidate, Is.True);
            Assert.That(sideResult.PlayerDamageCandidate.ContactSide,
                Is.EqualTo(CharacterContactSide.Side));

            // 하단 접촉(적이 위) → 플레이어 피해 후보.
            var bottom = Classify(new Vector2(0f, -0.85f), 1f, Vector2.zero);

            Assert.That(bottom.Side, Is.EqualTo(CharacterContactSide.Bottom));

            var bottomResult = policy.Evaluate(in bottom, in hostile);

            Assert.That(bottomResult.HasPlayerDamageCandidate, Is.True);
            Assert.That(bottomResult.PlayerDamageCandidate.SourceEnemyId, Is.EqualTo(9));
        }
    }
}
