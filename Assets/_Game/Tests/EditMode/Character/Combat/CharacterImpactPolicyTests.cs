using System.Linq;
using NUnit.Framework;
using StarNight.Character.Combat;
using UnityEngine;

namespace StarNight.Character.Tests.Combat
{
    public sealed class CharacterImpactPolicyTests
    {
        private const int OwnerId = 777;
        private const int ObjectId = 42;

        private static CharacterImpactPolicy CreatePolicy()
        {
            return new CharacterImpactPolicy(CharacterImpactSettings.Default);
        }

        private static CharacterImpactSource MovingThrownStone(
            Vector2 velocity, float graceRemaining)
        {
            return new CharacterImpactSource(
                ObjectId, OwnerId, true,
                CharacterImpactSourceKind.ThrownObject,
                velocity, graceRemaining);
        }

        [Test]
        public void Impact_ThrownObjectEnemyTargetCreatesDamageCandidate()
        {
            var policy = CreatePolicy();
            var source = MovingThrownStone(new Vector2(6f, 0f), 0.2f);
            var hostileEnemy = new CharacterImpactTarget(
                CharacterImpactTargetKind.Enemy, 9, true);

            // 유예는 소유자/자기 대상만 억제한다 — 적 대상은 유예 중에도 정상 임팩트.
            var result = policy.Evaluate(in source, in hostileEnemy);

            Assert.That(result.HasEnemyDamageCandidate, Is.True);
            Assert.That(result.EnemyDamageCandidate.SourceObjectId, Is.EqualTo(ObjectId));
            Assert.That(result.EnemyDamageCandidate.TargetEnemyId, Is.EqualTo(9));
            Assert.That(result.EnemyDamageCandidate.ImpactDirection,
                Is.EqualTo(Vector2.right));
            Assert.That(result.EnemyDamageCandidate.Amount,
                Is.EqualTo(policy.Settings.ThrownEnemyDamageAmount));
            Assert.That(result.HasObjectStopRequest, Is.False);
            Assert.That(result.HasPlayerDamageCandidate, Is.False);
        }

        [Test]
        public void Impact_OwnerGraceSuppressesOwnerSelfImpact()
        {
            var policy = CreatePolicy();
            var source = MovingThrownStone(new Vector2(6f, 0f), 0.2f);
            var ownerSelf = new CharacterImpactTarget(
                CharacterImpactTargetKind.Player, OwnerId, false);

            var result = policy.Evaluate(in source, in ownerSelf);

            Assert.That(result.HasEnemyDamageCandidate, Is.False);
            Assert.That(result.HasObjectStopRequest, Is.False);
            Assert.That(result.HasPlayerDamageCandidate, Is.False);
        }

        [Test]
        public void Impact_OwnerGraceExpiredAllowsEligibleImpact()
        {
            var policy = CreatePolicy();

            // 유예 만료 + 적격 대상(적대 적) → 정상 임팩트 판정.
            var expired = MovingThrownStone(new Vector2(-5f, 0f), 0f);
            var hostileEnemy = new CharacterImpactTarget(
                CharacterImpactTargetKind.Enemy, 11, true);

            var result = policy.Evaluate(in expired, in hostileEnemy);

            Assert.That(result.HasEnemyDamageCandidate, Is.True);
            Assert.That(result.EnemyDamageCandidate.ImpactDirection,
                Is.EqualTo(Vector2.left));
        }

        [Test]
        public void Impact_StationaryOrBelowThresholdSourceCreatesNoEvent()
        {
            var policy = CreatePolicy();
            var hostileEnemy = new CharacterImpactTarget(
                CharacterImpactTargetKind.Enemy, 9, true);

            // 정지 소스 — 이벤트 없음.
            var stationary = MovingThrownStone(Vector2.zero, 0f);

            Assert.That(policy.Evaluate(in stationary, in hostileEnemy)
                .HasEnemyDamageCandidate, Is.False);

            // 최소 임팩트 속도(1.5) 미만 — 이벤트 없음(고체 대상 포함).
            var slow = MovingThrownStone(new Vector2(1f, 0f), 0f);

            Assert.That(policy.Evaluate(in slow, in hostileEnemy)
                .HasEnemyDamageCandidate, Is.False);
            Assert.That(policy.Evaluate(in slow, CharacterImpactTarget.SolidWorld)
                .HasObjectStopRequest, Is.False);
        }

        [Test]
        public void Impact_SolidWorldCreatesObjectStopRequestOnly()
        {
            var policy = CreatePolicy();
            var source = MovingThrownStone(new Vector2(6f, -2f), 0.1f);

            var result = policy.Evaluate(in source, CharacterImpactTarget.SolidWorld);

            Assert.That(result.HasObjectStopRequest, Is.True);
            Assert.That(result.ObjectStopRequest.ObjectId, Is.EqualTo(ObjectId));
            Assert.That(result.HasEnemyDamageCandidate, Is.False);
            Assert.That(result.HasPlayerDamageCandidate, Is.False);

            // 정지 요청 타입에 지형 변경 관련 멤버가 없다(지형 변경은 CHAR05_01 소관).
            var stopMembers = typeof(CharacterObjectStopRequest)
                .GetProperties().Select(property => property.Name).ToArray();

            Assert.That(stopMembers, Has.None.Contains("Terrain"));
            Assert.That(stopMembers, Has.None.Contains("Tile"));
        }

        [Test]
        public void Impact_ResultSeparatesObjectEnemyAndPlayerRequests()
        {
            // 결과 구조가 소스 오브젝트/적/플레이어 요청 슬롯을 분리해 담는다.
            var resultProperties = typeof(CharacterImpactResult)
                .GetProperties().Select(property => property.Name).ToArray();

            Assert.That(resultProperties, Does.Contain("HasObjectStopRequest"));
            Assert.That(resultProperties, Does.Contain("HasEnemyDamageCandidate"));
            Assert.That(resultProperties, Does.Contain("HasPlayerDamageCandidate"));

            // 적 피해 후보에는 HP/제거/사망 적용 멤버가 없다(요청 전용).
            var candidateMembers = typeof(CharacterEnemyImpactDamageCandidate)
                .GetMembers().Select(member => member.Name).ToArray();

            Assert.That(candidateMembers, Has.None.Contains("Health"));
            Assert.That(candidateMembers, Has.None.Contains("Hp"));
            Assert.That(candidateMembers, Has.None.Contains("Remove"));
            Assert.That(candidateMembers, Has.None.Contains("Death"));
            Assert.That(candidateMembers, Has.None.Contains("Score"));

            // 적 임팩트 시 플레이어 슬롯은 발행되지 않는다(예약 슬롯 — 문서화 동작).
            var policy = CreatePolicy();
            var source = MovingThrownStone(new Vector2(6f, 0f), 0f);
            var hostileEnemy = new CharacterImpactTarget(
                CharacterImpactTargetKind.Enemy, 9, true);
            var result = policy.Evaluate(in source, in hostileEnemy);

            Assert.That(result.HasEnemyDamageCandidate, Is.True);
            Assert.That(result.HasPlayerDamageCandidate, Is.False);
            Assert.That(result.HasObjectStopRequest, Is.False);
        }

        [Test]
        public void Impact_NonHostileTargetDoesNotCreateEnemyDamageCandidate()
        {
            var policy = CreatePolicy();
            var source = MovingThrownStone(new Vector2(6f, 0f), 0f);

            // 기절 등 비적대 대상 — 명시적 적대가 아니면 피해 없음.
            var stunnedNonHostile = new CharacterImpactTarget(
                CharacterImpactTargetKind.Enemy, 21, false);

            Assert.That(policy.Evaluate(in source, in stunnedNonHostile)
                .HasEnemyDamageCandidate, Is.False);

            // 명시적으로 적대로 표시된 경우에만 피해 후보가 된다.
            var explicitlyHostile = new CharacterImpactTarget(
                CharacterImpactTargetKind.Enemy, 21, true);

            Assert.That(policy.Evaluate(in source, in explicitlyHostile)
                .HasEnemyDamageCandidate, Is.True);
        }
    }
}
