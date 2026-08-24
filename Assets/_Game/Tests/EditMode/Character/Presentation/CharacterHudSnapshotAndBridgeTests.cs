using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.Equipment;
using StarNight.Character.Presentation;
using StarNight.Character.RunState;
using StarNight.Character.Survival;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Tests.Presentation
{
    public sealed class CharacterHudSnapshotAndBridgeTests
    {
        private const int PlayerId = 777;
        private const int EnemyId = 21;

        private static CharacterRunState ActiveRun(
            int currentHealth = 4,
            float invulnerability = 0f,
            int bombs = 4,
            int ropes = 4)
        {
            var health = new CharacterHealthState(
                PlayerId, CharacterSurvivalTargetKind.Player,
                currentHealth, 4, invulnerability);
            var inventory = new CharacterRunInventoryState(PlayerId, bombs, ropes);
            return CharacterRunState.CreateActive(PlayerId, in health, in inventory);
        }

        private static WorldTileCoord Cell(int x, int y)
        {
            WorldTileCoord cell;
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(x, y, out cell),
                Is.True);
            return cell;
        }

        [Test]
        public void HudSnapshot_ContainsHealthInventoryStatusAndReturnToken()
        {
            var run = ActiveRun(currentHealth: 3, invulnerability: 0.5f,
                bombs: 2, ropes: 1);
            var snapshot = CharacterHudSnapshot.FromRunState(in run);

            Assert.That(snapshot.CurrentHealth, Is.EqualTo(3));
            Assert.That(snapshot.MaxHealth, Is.EqualTo(4));
            Assert.That(snapshot.IsInvulnerable, Is.True);
            Assert.That(snapshot.BombCount, Is.EqualTo(2));
            Assert.That(snapshot.RopeCount, Is.EqualTo(1));
            Assert.That(snapshot.RunStatus, Is.EqualTo(CharacterRunStatus.Active));
            Assert.That(snapshot.HasReturnDestination, Is.False);

            // 런 실패 후에는 상태와 복귀 토큰이 그대로 실린다.
            var failed = run.ApplyRunFailure(new CharacterRunFailureRequest(
                CharacterRunFailureReason.VoidOrOutOfBounds, PlayerId,
                "sector:2/chunk:5"));
            var failedSnapshot = CharacterHudSnapshot.FromRunState(in failed);

            Assert.That(failedSnapshot.RunStatus,
                Is.EqualTo(CharacterRunStatus.Failed));
            Assert.That(failedSnapshot.ReturnDestinationToken,
                Is.EqualTo("sector:2/chunk:5"));

            // 같은 런 상태면 항상 같은 스냅샷(결정적).
            var again = CharacterHudSnapshot.FromRunState(in run);
            Assert.That(again.CurrentHealth, Is.EqualTo(snapshot.CurrentHealth));
            Assert.That(again.BombCount, Is.EqualTo(snapshot.BombCount));
            Assert.That(again.RunStatus, Is.EqualTo(snapshot.RunStatus));
        }

        [Test]
        public void HudSnapshot_IsDataOnlyAndDoesNotUseUnityUiSceneAudioOrSave()
        {
            // 표면 타입: 전 공개 속성이 원시값/enum/string뿐이다 —
            // UI/Canvas/TMP/GameObject/Scene/Audio/Animator/PlayerPrefs 부재.
            var properties = typeof(CharacterHudSnapshot).GetProperties();
            var allowedTypeNames = new[] { "Int32", "Boolean", "String",
                "CharacterRunStatus" };

            foreach (var property in properties)
            {
                Assert.That(allowedTypeNames,
                    Does.Contain(property.PropertyType.Name),
                    "HUD 스냅샷에 비데이터 표면이 있다: " + property.Name);
            }

            // 공개 setter 없음(불변 데이터).
            Assert.That(properties.All(
                property => property.GetSetMethod() == null), Is.True);

            // 명명 가드: Canvas/Text/Audio/Scene/Prefs 계열 명명 부재.
            var memberNames = typeof(CharacterHudSnapshot)
                .GetMembers()
                .Select(member => member.Name)
                .ToArray();

            foreach (var keyword in new[]
                { "Canvas", "TextMesh", "Audio", "Scene", "PlayerPrefs",
                  "GameObject", "Animator" })
            {
                Assert.That(memberNames, Has.None.Contains(keyword));
            }
        }

        [Test]
        public void PresentationBridge_DamageDeathAndRunFailureCreateEventRequests()
        {
            // 피해: 실제 적용분이 있을 때만 이벤트.
            var settings = CharacterSurvivalSettings.Default;
            var health = CharacterHealthState.CreateFull(
                PlayerId, CharacterSurvivalTargetKind.Player, 4);
            var applied = CharacterHealthDamagePolicy.ApplyDamage(
                in health,
                new CharacterSurvivalDamageRequest(
                    CharacterDamageSourceKind.EnemyContact, EnemyId, PlayerId,
                    CharacterSurvivalTargetKind.Player, 1, Vector2.zero, false),
                in settings);

            CharacterPresentationEventRequest damageEvent;
            Assert.That(CharacterPresentationBridge.TryCreateDamageEvent(
                in applied, PlayerId, out damageEvent), Is.True);
            Assert.That(damageEvent.Type,
                Is.EqualTo(CharacterPresentationEventType.Damage));
            Assert.That(damageEvent.ActorOrSourceId, Is.EqualTo(PlayerId));
            Assert.That(damageEvent.HasAmount, Is.True);
            Assert.That(damageEvent.Amount, Is.EqualTo(1));

            // 억제된 피해(무적)는 이벤트가 없다.
            var invulnerable = applied.NewState;
            var suppressed = CharacterHealthDamagePolicy.ApplyDamage(
                in invulnerable,
                new CharacterSurvivalDamageRequest(
                    CharacterDamageSourceKind.EnemyContact, EnemyId, PlayerId,
                    CharacterSurvivalTargetKind.Player, 1, Vector2.zero, false),
                in settings);
            Assert.That(CharacterPresentationBridge.TryCreateDamageEvent(
                in suppressed, PlayerId, out damageEvent), Is.False);

            // 사망·런 실패 이벤트.
            var deathEvent = CharacterPresentationBridge.CreateDeathEvent(
                new CharacterDeathRequest(EnemyId,
                    CharacterSurvivalTargetKind.Enemy,
                    CharacterDamageSourceKind.Stomp, PlayerId));
            Assert.That(deathEvent.Type,
                Is.EqualTo(CharacterPresentationEventType.Death));
            Assert.That(deathEvent.ActorOrSourceId, Is.EqualTo(EnemyId));

            var failureEvent = CharacterPresentationBridge.CreateRunFailureEvent(
                new CharacterRunFailureRequest(
                    CharacterRunFailureReason.PlayerDeath, PlayerId, null));
            Assert.That(failureEvent.Type,
                Is.EqualTo(CharacterPresentationEventType.RunFailure));
            Assert.That(failureEvent.ActorOrSourceId, Is.EqualTo(PlayerId));
        }

        [Test]
        public void PresentationBridge_BombRopeAndInventoryEventsAreRequestsOnly()
        {
            // 폭탄 설치/폭발 이벤트 — 셀 좌표를 실어 나른다.
            var placedEvent = CharacterPresentationBridge.CreateBombPlacedEvent(
                new CharacterBombPlacementRequest(PlayerId, Cell(10, 5)));
            Assert.That(placedEvent.Type,
                Is.EqualTo(CharacterPresentationEventType.BombPlaced));
            Assert.That(placedEvent.HasCell, Is.True);
            Assert.That(placedEvent.Cell.X, Is.EqualTo(10));

            var explodedEvent = CharacterPresentationBridge.CreateBombExplodedEvent(
                new CharacterExplosionRequest(9, PlayerId, Cell(10, 5), 1.5f, 2));
            Assert.That(explodedEvent.Type,
                Is.EqualTo(CharacterPresentationEventType.BombExploded));
            Assert.That(explodedEvent.Amount, Is.EqualTo(2));

            // 로프 설치 이벤트.
            var ropeEvent = CharacterPresentationBridge.CreateRopePlacedEvent(
                new CharacterRopePlacementRequest(PlayerId, Cell(12, 3)));
            Assert.That(ropeEvent.Type,
                Is.EqualTo(CharacterPresentationEventType.RopePlaced));
            Assert.That(ropeEvent.Cell.Y, Is.EqualTo(3));

            // 인벤토리 변화 이벤트 — 실제 변화가 있을 때만.
            var inventory = new CharacterRunInventoryState(PlayerId, 4, 4);
            var spent = CharacterRunInventoryPolicy.ApplyBombSpend(
                in inventory, new CharacterBombSpendRequest(PlayerId, 1));

            CharacterPresentationEventRequest inventoryEvent;
            Assert.That(CharacterPresentationBridge.TryCreateInventoryChangedEvent(
                in spent, out inventoryEvent), Is.True);
            Assert.That(inventoryEvent.Type,
                Is.EqualTo(CharacterPresentationEventType.InventoryChanged));

            var noChange = CharacterRunInventoryPolicy.ApplyBombSpend(
                in inventory, new CharacterBombSpendRequest(999, 1));
            Assert.That(CharacterPresentationBridge.TryCreateInventoryChangedEvent(
                in noChange, out inventoryEvent), Is.False);

            // 요청 값 객체는 불변이다 — 공개 setter 없음.
            Assert.That(typeof(CharacterPresentationEventRequest).GetProperties()
                .All(property => property.GetSetMethod() == null), Is.True);
        }

        [Test]
        public void PresentationBridge_EventsAreDeterministicOrderedAndDeduplicated()
        {
            // 뒤섞인 입력 + 중복 2건(같은 폭발, 같은 피해).
            var explosion = CharacterPresentationBridge.CreateBombExplodedEvent(
                new CharacterExplosionRequest(9, PlayerId, Cell(10, 5), 1.5f, 2));
            var damage = new CharacterPresentationEventRequest(
                CharacterPresentationEventType.Damage, PlayerId, true, 1,
                false, default, 0);
            var death = CharacterPresentationBridge.CreateDeathEvent(
                new CharacterDeathRequest(PlayerId,
                    CharacterSurvivalTargetKind.Player,
                    CharacterDamageSourceKind.Explosion, 9));
            var failure = CharacterPresentationBridge.CreateRunFailureEvent(
                new CharacterRunFailureRequest(
                    CharacterRunFailureReason.PlayerDeath, PlayerId, null));

            var scrambled = new List<CharacterPresentationEventRequest>
            {
                explosion, damage, failure, explosion, death, damage
            };

            var first = new List<CharacterPresentationEventRequest>();
            CharacterPresentationBridge.NormalizeBatch(scrambled, first);

            // 중복 2건 제거 → 4건, 우선순위 순서: 런 실패→사망→피해→폭발.
            Assert.That(first.Count, Is.EqualTo(4));
            Assert.That(first[0].Type,
                Is.EqualTo(CharacterPresentationEventType.RunFailure));
            Assert.That(first[1].Type,
                Is.EqualTo(CharacterPresentationEventType.Death));
            Assert.That(first[2].Type,
                Is.EqualTo(CharacterPresentationEventType.Damage));
            Assert.That(first[3].Type,
                Is.EqualTo(CharacterPresentationEventType.BombExploded));

            // 순번은 출력 순서대로 0..n-1.
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(first[index].SequenceId, Is.EqualTo(index));
            }

            // 같은 입력이면 반복 호출에도 완전히 같은 출력(안정·결정적).
            var second = new List<CharacterPresentationEventRequest>();
            CharacterPresentationBridge.NormalizeBatch(scrambled, second);

            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(second[index].Type, Is.EqualTo(first[index].Type));
                Assert.That(second[index].ActorOrSourceId,
                    Is.EqualTo(first[index].ActorOrSourceId));
                Assert.That(second[index].SequenceId,
                    Is.EqualTo(first[index].SequenceId));
            }
        }
    }
}
