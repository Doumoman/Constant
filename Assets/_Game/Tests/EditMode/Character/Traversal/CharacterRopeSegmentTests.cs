using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.Equipment;
using StarNight.Character.MapIntegration;
using StarNight.Character.Traversal;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.Traversal
{
    public sealed class CharacterRopeSegmentTests
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

        private static readonly CharacterMapCellState Solid =
            new CharacterMapCellState(true, false, false, false, false);

        [Test]
        public void RopeSegments_GenerateVerticalCellsUntilBlockedOrMaxLength()
        {
            var settings = CharacterRopeSettings.Default; // 최대 6셀
            var openColumn = new FakeMapWorldQuery();

            // (A) 빈 열: 최대 길이 6에서 캡.
            var maxRun = CharacterRopeSegmentPolicy.GenerateSegmentRequests(
                1, new WorldTileCoord(10, 5), in settings, openColumn);

            Assert.That(maxRun.Count, Is.EqualTo(6));
            Assert.That(maxRun[0].Cell.Y, Is.EqualTo(5));
            Assert.That(maxRun[5].Cell.Y, Is.EqualTo(10));
            Assert.That(maxRun.All(segment => segment.Cell.X == 10), Is.True);

            // (B) 고체 차단: (10,8) 고체면 진입 전에 멈춰 3개(5,6,7).
            var blocked = new FakeMapWorldQuery();
            blocked.SetCell(10, 8, Solid);

            var blockedRun = CharacterRopeSegmentPolicy.GenerateSegmentRequests(
                2, new WorldTileCoord(10, 5), in settings, blocked);

            Assert.That(blockedRun.Count, Is.EqualTo(3));
            Assert.That(blockedRun[blockedRun.Count - 1].Cell.Y, Is.EqualTo(7));

            // (C) 월드 상단 경계: y=413 시작이면 415까지 3개 후 중단.
            var nearTop = CharacterRopeSegmentPolicy.GenerateSegmentRequests(
                3, new WorldTileCoord(10, 413), in settings, openColumn);

            Assert.That(nearTop.Count, Is.EqualTo(3));
            Assert.That(nearTop[nearTop.Count - 1].Cell.Y, Is.EqualTo(415));

            // (D) 원점 자체가 고체면 세그먼트 없음(방어적).
            var originSolid = new FakeMapWorldQuery();
            originSolid.SetCell(10, 5, Solid);

            var none = CharacterRopeSegmentPolicy.GenerateSegmentRequests(
                4, new WorldTileCoord(10, 5), in settings, originSolid);

            Assert.That(none, Is.Empty);
        }

        [Test]
        public void RopeSegments_AreDeterministicOrderedAndDeduplicated()
        {
            var settings = CharacterRopeSettings.Default;
            var query = new FakeMapWorldQuery();

            var first = CharacterRopeSegmentPolicy.GenerateSegmentRequests(
                7, new WorldTileCoord(20, 30), in settings, query);
            var second = CharacterRopeSegmentPolicy.GenerateSegmentRequests(
                7, new WorldTileCoord(20, 30), in settings, query);

            Assert.That(first.Count, Is.EqualTo(6));
            Assert.That(second.Count, Is.EqualTo(first.Count));

            // 아래→위 엄격 오름차순 + 인덱스 0..n-1 + 재호출 동일 순서.
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(first[index].Cell.Y, Is.EqualTo(30 + index));
                Assert.That(first[index].IndexFromOrigin, Is.EqualTo(index));
                Assert.That(second[index].Cell.X, Is.EqualTo(first[index].Cell.X));
                Assert.That(second[index].Cell.Y, Is.EqualTo(first[index].Cell.Y));
            }

            // 중복 없음.
            var distinct = first
                .Select(segment => segment.Cell.X + "," + segment.Cell.Y)
                .Distinct()
                .Count();

            Assert.That(distinct, Is.EqualTo(first.Count));
        }

        [Test]
        public void RopeSegments_DoNotMutateMapOrTilemap()
        {
            var settings = CharacterRopeSettings.Default;
            var query = new FakeMapWorldQuery();
            query.SetCell(10, 8, Solid);

            var segments = CharacterRopeSegmentPolicy.GenerateSegmentRequests(
                1, new WorldTileCoord(10, 5), in settings, query);

            Assert.That(segments.Count, Is.EqualTo(3));

            // 생성 후에도 맵 상태는 그대로다 — 차단 셀은 여전히 고체이고
            // 통과 셀에는 어떤 데이터도 새로 쓰이지 않았다.
            CharacterMapCellState after;
            Assert.That(query.TryGetCellState(new WorldTileCoord(10, 8), out after), Is.True);
            Assert.That(after.IsSolid, Is.True);
            Assert.That(query.TryGetCellState(new WorldTileCoord(10, 6), out after), Is.False);

            // 세그먼트 요청은 셀 좌표·로프 ID·순번만 기술하는 불변 값 객체다.
            foreach (var segment in segments)
            {
                Assert.That(segment.RopeId, Is.EqualTo(1));
            }
            Assert.That(
                typeof(CharacterRopeSegmentRequest).GetProperties()
                    .All(property => property.GetSetMethod() == null),
                Is.True);
        }
    }
}
