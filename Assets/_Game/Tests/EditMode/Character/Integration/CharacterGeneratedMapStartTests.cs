using NUnit.Framework;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.Integration
{
    public sealed class CharacterGeneratedMapStartTests
    {
        private const int ActorId = 777;
        private const float Tolerance = 1e-4f;

        /// <summary>방 A = 월드 좌하단 마이크로청크(12×8): 셀 [0..11]×[0..7].</summary>
        private static CharacterGeneratedMapStartSnapshot ValidStart(
            int startX = 5, int startY = 3)
        {
            var startCell = new WorldTileCoord(startX, startY);
            return new CharacterGeneratedMapStartSnapshot(
                1,
                CharacterRoomId.FromWorldTile(new WorldTileCoord(0, 0)),
                true,
                startCell,
                new WorldTileCoord(0, 0),
                new WorldTileCoord(11, 7));
        }

        [Test]
        public void GeneratedMapStart_ValidStartCreatesPlayerSpawnRequest()
        {
            var snapshot = ValidStart();
            CharacterPlayerSpawnRequest request;
            CharacterIntegrationDiagnostic diagnostic;

            Assert.That(CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                in snapshot, ActorId, out request, out diagnostic), Is.True);

            Assert.That(request.ActorId, Is.EqualTo(ActorId));
            Assert.That(request.StartCell.X, Is.EqualTo(5));
            Assert.That(request.StartCell.Y, Is.EqualTo(3));
            Assert.That(request.StartRoomId.Equals(snapshot.StartRoomId), Is.True);
        }

        [Test]
        public void GeneratedMapStart_InvalidOrOutOfBoundsStartCreatesDiagnosticOnly()
        {
            CharacterPlayerSpawnRequest request;
            CharacterIntegrationDiagnostic diagnostic;

            // (a) 시작 셀 자체가 없음.
            var missing = new CharacterGeneratedMapStartSnapshot(
                1, CharacterRoomId.FromWorldTile(new WorldTileCoord(0, 0)),
                false, default, new WorldTileCoord(0, 0), new WorldTileCoord(11, 7));
            Assert.That(CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                in missing, ActorId, out request, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind,
                Is.EqualTo(CharacterIntegrationDiagnosticKind.MissingStartCell));

            // (b) 월드 경계 밖(원시 좌표 700, 월드 폭 624).
            var outsideWorld = new CharacterGeneratedMapStartSnapshot(
                1, CharacterRoomId.FromWorldTile(new WorldTileCoord(0, 0)),
                true, new WorldTileCoord(700, 5),
                new WorldTileCoord(0, 0), new WorldTileCoord(11, 7));
            Assert.That(CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                in outsideWorld, ActorId, out request, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind,
                Is.EqualTo(CharacterIntegrationDiagnosticKind.StartCellOutsideWorldBounds));

            // (c) 방 경계 밖(셀 20은 방 [0..11] 밖).
            var outsideRoom = new CharacterGeneratedMapStartSnapshot(
                1, CharacterRoomId.FromWorldTile(new WorldTileCoord(0, 0)),
                true, new WorldTileCoord(20, 3),
                new WorldTileCoord(0, 0), new WorldTileCoord(11, 7));
            Assert.That(CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                in outsideRoom, ActorId, out request, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind,
                Is.EqualTo(CharacterIntegrationDiagnosticKind.StartCellOutsideRoomBounds));

            // (d) 셀에서 유도한 방과 선언된 시작 방 불일치.
            var mismatch = new CharacterGeneratedMapStartSnapshot(
                1, CharacterRoomId.FromWorldTile(new WorldTileCoord(12, 0)),
                true, new WorldTileCoord(5, 3),
                new WorldTileCoord(0, 0), new WorldTileCoord(11, 7));
            Assert.That(CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                in mismatch, ActorId, out request, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind,
                Is.EqualTo(CharacterIntegrationDiagnosticKind.StartRoomMismatch));
        }

        [Test]
        public void GeneratedMapStart_SpawnRequestUsesMapCoordinateBridgeCenter()
        {
            var snapshot = ValidStart(startX: 5, startY: 3);
            CharacterPlayerSpawnRequest request;
            CharacterIntegrationDiagnostic diagnostic;

            Assert.That(CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                in snapshot, ActorId, out request, out diagnostic), Is.True);

            // 월드 중심은 공용 좌표 브리지 값과 정확히 일치한다(1셀=1u, 중심 +0.5).
            var expected = CharacterMapCoordinateBridge.GetCellCenter(
                snapshot.StartCell);
            Assert.That(request.WorldCenter.x, Is.EqualTo(expected.x).Within(Tolerance));
            Assert.That(request.WorldCenter.y, Is.EqualTo(expected.y).Within(Tolerance));
            Assert.That(request.WorldCenter.x, Is.EqualTo(5.5f).Within(Tolerance));
            Assert.That(request.WorldCenter.y, Is.EqualTo(3.5f).Within(Tolerance));
        }
    }
}
