using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.Equipment;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Tests.Equipment
{
    public sealed class CharacterExplosionPolicyTests
    {
        private sealed class FakeMapWorldQuery : ICharacterMapWorldQuery
        {
            private readonly Dictionary<long, CharacterMapCellState> cells =
                new Dictionary<long, CharacterMapCellState>();

            public void SetCell(int x, int y, CharacterMapCellState state)
            {
                cells[Key(x, y)] = state;
            }

            public bool TryGetCellState(WorldTileCoord tile, out CharacterMapCellState state)
            {
                return cells.TryGetValue(Key(tile.X, tile.Y), out state);
            }

            private static long Key(int x, int y)
            {
                return ((long)y << 32) | (uint)x;
            }
        }

        private static readonly CharacterMapCellState Breakable =
            new CharacterMapCellState(true, false, false, false, true);

        private static readonly CharacterMapCellState IndestructibleSolid =
            new CharacterMapCellState(true, false, false, false, false);

        private static CharacterExplosionRequest Explosion(int centerX, int centerY)
        {
            return new CharacterExplosionRequest(
                1, 777, new WorldTileCoord(centerX, centerY), 1.5f, 2);
        }

        [Test]
        public void Explosion_TerrainMutationRequestIncludesOnlyDestructibleCells()
        {
            var query = new FakeMapWorldQuery();

            // 중심 (10,5), 반경 1.5셀 = 3×3. 파괴 가능 2셀 + 비파괴 고체 + 빈 셀 혼재.
            query.SetCell(9, 5, Breakable);
            query.SetCell(11, 5, Breakable);
            query.SetCell(10, 4, IndestructibleSolid);
            query.SetCell(10, 6, CharacterMapCellState.Empty);

            var explosion = Explosion(10, 5);
            var requests = CharacterExplosionTerrainPolicy
                .CreateTerrainMutationRequests(in explosion, query);

            Assert.That(requests.Count, Is.EqualTo(2));

            var cellKeys = requests
                .Select(request => request.Cell.X + "," + request.Cell.Y)
                .ToArray();

            Assert.That(cellKeys, Is.EquivalentTo(new[] { "9,5", "11,5" }));

            foreach (var request in requests)
            {
                Assert.That(request.Intent,
                    Is.EqualTo(CharacterTerrainMutationIntent.DestroyBreakable));
                Assert.That(request.SourceExplosionId, Is.EqualTo(1));
            }
        }

        [Test]
        public void Explosion_TerrainMutationRequestIsDeterministicAndDeduplicated()
        {
            // 반경 1.5셀 → 3×3 마스크(9셀, 대각 포함 — 레거시 3×3 선례와 일치).
            var affected = CharacterExplosionTerrainPolicy
                .EnumerateAffectedCells(new WorldTileCoord(10, 5), 1.5f);

            Assert.That(affected.Count, Is.EqualTo(9));

            // 중복 없음.
            var distinct = affected
                .Select(cell => cell.X + "," + cell.Y)
                .Distinct()
                .Count();

            Assert.That(distinct, Is.EqualTo(9));

            // 결정적 순서: 두 번 호출해도 완전히 같은 순서.
            var second = CharacterExplosionTerrainPolicy
                .EnumerateAffectedCells(new WorldTileCoord(10, 5), 1.5f);

            for (int index = 0; index < affected.Count; index++)
            {
                Assert.That(second[index].X, Is.EqualTo(affected[index].X));
                Assert.That(second[index].Y, Is.EqualTo(affected[index].Y));
            }
        }

        [Test]
        public void Explosion_IndestructibleEmptyAndOutOfBoundsCellsAreSkipped()
        {
            // 월드 원점 모서리: 반경 1.5셀이면 범위 밖 셀이 열거에서 제외된다.
            var cornerAffected = CharacterExplosionTerrainPolicy
                .EnumerateAffectedCells(new WorldTileCoord(0, 0), 1.5f);

            Assert.That(cornerAffected.Count, Is.EqualTo(4)); // (0,0)(1,0)(0,1)(1,1)

            // 비파괴 고체·빈 셀·데이터 없는 셀은 변경 요청을 만들지 않는다.
            var query = new FakeMapWorldQuery();
            query.SetCell(0, 0, IndestructibleSolid);
            query.SetCell(1, 0, CharacterMapCellState.Empty);
            // (0,1), (1,1)은 데이터 없음(미생성).

            var explosion = Explosion(0, 0);
            var requests = CharacterExplosionTerrainPolicy
                .CreateTerrainMutationRequests(in explosion, query);

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void Explosion_EnemyAndPlayerTargetsWithinRadiusCreateDamageCandidates()
        {
            var explosion = Explosion(10, 5); // 중심 월드 (10.5, 5.5), 반경 1.5u
            var targets = new List<CharacterExplosionTargetSnapshot>
            {
                new CharacterExplosionTargetSnapshot(21, false, new Vector2(11.5f, 5.5f)),
                new CharacterExplosionTargetSnapshot(777, true, new Vector2(10.5f, 4.5f))
            };
            var enemies = new List<CharacterEnemyExplosionDamageCandidate>();
            var players = new List<CharacterPlayerExplosionDamageCandidate>();

            CharacterExplosionDamagePolicy.CreateDamageCandidates(
                in explosion, targets, enemies, players);

            Assert.That(enemies.Count, Is.EqualTo(1));
            Assert.That(enemies[0].TargetEnemyId, Is.EqualTo(21));
            Assert.That(enemies[0].SourceExplosionId, Is.EqualTo(1));
            Assert.That(enemies[0].Amount, Is.EqualTo(2));
            Assert.That(enemies[0].DirectionFromCenter, Is.EqualTo(Vector2.right));

            // 자기 폭탄이라도 반경 안 플레이어는 피해 후보가 된다(공용 계약).
            Assert.That(players.Count, Is.EqualTo(1));
            Assert.That(players[0].TargetPlayerId, Is.EqualTo(777));
            Assert.That(players[0].DirectionFromCenter, Is.EqualTo(Vector2.down));
        }

        [Test]
        public void Explosion_TargetsOutsideRadiusCreateNoDamageCandidate()
        {
            var explosion = Explosion(10, 5);
            var targets = new List<CharacterExplosionTargetSnapshot>
            {
                new CharacterExplosionTargetSnapshot(22, false, new Vector2(13.0f, 5.5f)),
                new CharacterExplosionTargetSnapshot(777, true, new Vector2(10.5f, 8.0f))
            };
            var enemies = new List<CharacterEnemyExplosionDamageCandidate>();
            var players = new List<CharacterPlayerExplosionDamageCandidate>();

            CharacterExplosionDamagePolicy.CreateDamageCandidates(
                in explosion, targets, enemies, players);

            Assert.That(enemies, Is.Empty);
            Assert.That(players, Is.Empty);
        }
    }
}
