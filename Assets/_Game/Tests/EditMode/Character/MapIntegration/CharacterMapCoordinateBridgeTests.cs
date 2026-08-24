using NUnit.Framework;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Tests.MapIntegration
{
    public sealed class CharacterMapCoordinateBridgeTests
    {
        [Test]
        public void CoordinateBridge_UsesMapWorldCoordinateUtility()
        {
            // 셀 스케일 잠금 계약.
            Assert.That(CharacterMapCoordinateBridge.WorldUnitsPerCell, Is.EqualTo(1f));

            // 월드 좌표 → 타일: floor 후 MAP 공용 유틸리티 검증 경유.
            WorldTileCoord tile;

            Assert.That(
                CharacterMapCoordinateBridge.TryGetTileCoordinate(
                    new Vector2(3.4f, 5.9f), out tile), Is.True);
            Assert.That(tile.X, Is.EqualTo(3));
            Assert.That(tile.Y, Is.EqualTo(5));

            // MAP 공용 유틸리티와 동일 판정(위임 확인).
            WorldTileCoord utilityTile;

            Assert.That(
                WorldCoordinateUtility.TryCreateWorldTile(3, 5, out utilityTile), Is.True);
            Assert.That(tile.X, Is.EqualTo(utilityTile.X));
            Assert.That(tile.Y, Is.EqualTo(utilityTile.Y));

            // 타일 → 셀 원점/중심.
            Assert.That(CharacterMapCoordinateBridge.GetCellOrigin(tile),
                Is.EqualTo(new Vector2(3f, 5f)));
            Assert.That(CharacterMapCoordinateBridge.GetCellCenter(tile),
                Is.EqualTo(new Vector2(3.5f, 5.5f)));

            // 원점(0,0) 경계 포함.
            Assert.That(
                CharacterMapCoordinateBridge.TryGetTileCoordinate(
                    new Vector2(0f, 0f), out tile), Is.True);
            Assert.That(tile.X, Is.EqualTo(0));
            Assert.That(tile.Y, Is.EqualTo(0));
        }

        [Test]
        public void CoordinateBridge_RejectsOutOfBoundsWithoutClamping()
        {
            WorldTileCoord tile;

            // 음수 방향 범위 밖 — clamp 없이 거부(out은 default).
            Assert.That(
                CharacterMapCoordinateBridge.TryGetTileCoordinate(
                    new Vector2(-0.5f, 5f), out tile), Is.False);
            Assert.That(tile.X, Is.EqualTo(0));
            Assert.That(tile.Y, Is.EqualTo(0));

            // 상한 밖(월드 624×416 타일).
            Assert.That(
                CharacterMapCoordinateBridge.TryGetTileCoordinate(
                    new Vector2(WorldGenConstants.WorldWidthTiles + 0.5f, 5f), out tile),
                Is.False);
            Assert.That(
                CharacterMapCoordinateBridge.TryGetTileCoordinate(
                    new Vector2(5f, WorldGenConstants.WorldHeightTiles + 3f), out tile),
                Is.False);

            // 최대 경계 셀 내부는 유효하다.
            Assert.That(
                CharacterMapCoordinateBridge.TryGetTileCoordinate(
                    new Vector2(
                        WorldGenConstants.WorldWidthTiles - 0.1f,
                        WorldGenConstants.WorldHeightTiles - 0.1f), out tile), Is.True);
            Assert.That(tile.X, Is.EqualTo(WorldGenConstants.WorldWidthTiles - 1));
            Assert.That(tile.Y, Is.EqualTo(WorldGenConstants.WorldHeightTiles - 1));
        }
    }
}
