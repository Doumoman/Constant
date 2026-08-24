using NUnit.Framework;
using StarNight.Character.Combat;
using StarNight.Character.Equipment;
using StarNight.Character.Survival;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Tests.Survival
{
    public sealed class CharacterUnifiedDamageAndHazardTests
    {
        private const int PlayerId = 777;

        [Test]
        public void Damage_ContactImpactExplosionAndHazardCanBecomeUnifiedRequests()
        {
            // (1) 접촉(CHAR04_02) → EnemyContact.
            var contact = new CharacterPlayerDamageCandidate(
                21, CharacterContactSide.Side, 1);
            var contactRequest = CharacterSurvivalDamageAdapters.FromContact(
                in contact, PlayerId);

            Assert.That(contactRequest.SourceKind,
                Is.EqualTo(CharacterDamageSourceKind.EnemyContact));
            Assert.That(contactRequest.SourceId, Is.EqualTo(21));
            Assert.That(contactRequest.TargetId, Is.EqualTo(PlayerId));
            Assert.That(contactRequest.TargetKind,
                Is.EqualTo(CharacterSurvivalTargetKind.Player));
            Assert.That(contactRequest.Amount, Is.EqualTo(1));
            Assert.That(contactRequest.BypassInvulnerability, Is.False);

            // (2) 임팩트(CHAR04_03) → ThrownObject (플레이어/적 양쪽).
            var playerImpact = new CharacterPlayerImpactDamageCandidate(42, 1);
            var playerImpactRequest = CharacterSurvivalDamageAdapters.FromImpact(
                in playerImpact, PlayerId);

            Assert.That(playerImpactRequest.SourceKind,
                Is.EqualTo(CharacterDamageSourceKind.ThrownObject));
            Assert.That(playerImpactRequest.TargetKind,
                Is.EqualTo(CharacterSurvivalTargetKind.Player));

            var enemyImpact = new CharacterEnemyImpactDamageCandidate(
                42, 21, Vector2.right, 1);
            var enemyImpactRequest = CharacterSurvivalDamageAdapters.FromImpact(
                in enemyImpact);

            Assert.That(enemyImpactRequest.TargetId, Is.EqualTo(21));
            Assert.That(enemyImpactRequest.TargetKind,
                Is.EqualTo(CharacterSurvivalTargetKind.Enemy));
            Assert.That(enemyImpactRequest.Direction, Is.EqualTo(Vector2.right));

            // (3) 폭발(CHAR05_01) → Explosion (자해 후보 포함).
            var playerExplosion = new CharacterPlayerExplosionDamageCandidate(
                PlayerId, 9, 2, Vector2.down);
            var playerExplosionRequest = CharacterSurvivalDamageAdapters.FromExplosion(
                in playerExplosion);

            Assert.That(playerExplosionRequest.SourceKind,
                Is.EqualTo(CharacterDamageSourceKind.Explosion));
            Assert.That(playerExplosionRequest.SourceId, Is.EqualTo(9));
            Assert.That(playerExplosionRequest.Amount, Is.EqualTo(2));
            Assert.That(playerExplosionRequest.Direction, Is.EqualTo(Vector2.down));

            var enemyExplosion = new CharacterEnemyExplosionDamageCandidate(
                21, 9, 2, Vector2.up);
            var enemyExplosionRequest = CharacterSurvivalDamageAdapters.FromExplosion(
                in enemyExplosion);

            Assert.That(enemyExplosionRequest.TargetKind,
                Is.EqualTo(CharacterSurvivalTargetKind.Enemy));

            // (4) 위험 → 통합 요청(다음 테스트에서 종류별 사상 검증).
            WorldTileCoord cell;
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(10, 5, out cell),
                Is.True);
            var hazard = new CharacterHazardDamageCandidate(
                CharacterHazardKind.Spike, 3, PlayerId,
                CharacterSurvivalTargetKind.Player, 1, Vector2.up, true, cell);

            CharacterSurvivalDamageRequest hazardRequest;
            Assert.That(CharacterHazardPolicy.TryCreateDamageRequest(
                in hazard, out hazardRequest), Is.True);
            Assert.That(hazardRequest.SourceKind,
                Is.EqualTo(CharacterDamageSourceKind.Spike));

            // 네 경로 전부 같은 통합 정책으로 소비 가능함을 확인 —
            // 폭발 요청 하나를 실제 체력에 적용해 본다.
            var settings = CharacterSurvivalSettings.Default;
            var state = CharacterHealthState.CreateFull(
                PlayerId, CharacterSurvivalTargetKind.Player, 4);
            var applied = CharacterHealthDamagePolicy.ApplyDamage(
                in state, in playerExplosionRequest, in settings);

            Assert.That(applied.AppliedAmount, Is.EqualTo(2));
            Assert.That(applied.NewState.CurrentHealth, Is.EqualTo(2));
        }

        [Test]
        public void Hazard_SpikeCrushFireCreateDamageCandidates()
        {
            WorldTileCoord cell;
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(7, 3, out cell),
                Is.True);

            // cause 사상: Spike→Spike, Crush→Crush, Fire/Generic→Environment
            // (스키마 cause 잠금 9종 확장 없음).
            var expectations = new[]
            {
                (CharacterHazardKind.Spike, CharacterDamageSourceKind.Spike),
                (CharacterHazardKind.Crush, CharacterDamageSourceKind.Crush),
                (CharacterHazardKind.Fire, CharacterDamageSourceKind.Environment),
                (CharacterHazardKind.Generic, CharacterDamageSourceKind.Environment)
            };

            foreach (var (hazardKind, expectedCause) in expectations)
            {
                var candidate = new CharacterHazardDamageCandidate(
                    hazardKind, 11, PlayerId,
                    CharacterSurvivalTargetKind.Player, 1, Vector2.up, true, cell);

                CharacterSurvivalDamageRequest request;
                Assert.That(CharacterHazardPolicy.TryCreateDamageRequest(
                    in candidate, out request), Is.True, hazardKind.ToString());
                Assert.That(request.SourceKind, Is.EqualTo(expectedCause),
                    hazardKind.ToString());
                Assert.That(request.SourceId, Is.EqualTo(11));
                Assert.That(request.Amount, Is.EqualTo(1));
                Assert.That(request.BypassInvulnerability, Is.False);
            }

            // 셀 좌표는 알려진 경우에만 후보에 기록된다.
            var withCell = new CharacterHazardDamageCandidate(
                CharacterHazardKind.Spike, 11, PlayerId,
                CharacterSurvivalTargetKind.Player, 1, Vector2.up, true, cell);
            Assert.That(withCell.HasCell, Is.True);
            Assert.That(withCell.Cell.X, Is.EqualTo(7));

            var withoutCell = new CharacterHazardDamageCandidate(
                CharacterHazardKind.Crush, 12, PlayerId,
                CharacterSurvivalTargetKind.Player, 1, Vector2.zero,
                false, default);
            Assert.That(withoutCell.HasCell, Is.False);
        }

        [Test]
        public void Hazard_VoidOrOutOfBoundsCreatesRunFailureRequest()
        {
            // Void는 피해 요청이 아니라 치명 경로다.
            var voidCandidate = new CharacterHazardDamageCandidate(
                CharacterHazardKind.Void, 13, PlayerId,
                CharacterSurvivalTargetKind.Player, 0, Vector2.zero,
                false, default);

            CharacterSurvivalDamageRequest damageRequest;
            Assert.That(CharacterHazardPolicy.TryCreateDamageRequest(
                in voidCandidate, out damageRequest), Is.False);

            // 대상이 누구든 사망 요청(cause Fall)은 만들어진다.
            var death = CharacterHazardPolicy.CreateVoidDeathRequest(
                PlayerId, CharacterSurvivalTargetKind.Player, 13);
            Assert.That(death.Cause, Is.EqualTo(CharacterDamageSourceKind.Fall));
            Assert.That(death.ActorId, Is.EqualTo(PlayerId));

            // 플레이어의 Void/월드 이탈만 런 실패 요청을 만든다.
            CharacterRunFailureRequest runFailure;
            Assert.That(CharacterHazardPolicy.TryCreateVoidRunFailure(
                PlayerId, CharacterSurvivalTargetKind.Player, "sector:0/chunk:0",
                out runFailure), Is.True);
            Assert.That(runFailure.Reason,
                Is.EqualTo(CharacterRunFailureReason.VoidOrOutOfBounds));
            Assert.That(runFailure.ActorId, Is.EqualTo(PlayerId));
            Assert.That(runFailure.HasReturnDestination, Is.True);

            Assert.That(CharacterHazardPolicy.TryCreateVoidRunFailure(
                21, CharacterSurvivalTargetKind.Enemy, null, out runFailure),
                Is.False);
        }
    }
}
