using NUnit.Framework;
using StarNight.Character.Equipment;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.Equipment
{
    public sealed class CharacterRopePlacementTests
    {
        private const int ActorId = 777;
        private static readonly WorldTileCoord Origin = new WorldTileCoord(10, 5);

        private static CharacterRopePlacementInput Input(
            int ropeCount,
            bool hasValidOrigin = true,
            bool placeable = true)
        {
            return new CharacterRopePlacementInput(
                ActorId, hasValidOrigin, Origin, ropeCount, placeable);
        }

        [Test]
        public void RopePlacement_AvailableRopeCreatesPlacementAndSpendRequest()
        {
            CharacterRopePlacementRequest placement;
            CharacterRopeSpendRequest spend;

            // 레거시 초기 보유 선례 4개 스냅샷.
            Assert.That(CharacterRopePlacementPolicy.TryCreatePlacement(
                Input(ropeCount: 4), out placement, out spend), Is.True);

            Assert.That(placement.ActorId, Is.EqualTo(ActorId));
            Assert.That(placement.OriginCell.X, Is.EqualTo(10));
            Assert.That(placement.OriginCell.Y, Is.EqualTo(5));

            // 소모는 요청일 뿐이다 — ropeCount는 어디에서도 변조되지 않는다.
            Assert.That(spend.ActorId, Is.EqualTo(ActorId));
            Assert.That(spend.Amount, Is.EqualTo(1));
        }

        [Test]
        public void RopePlacement_NoRopeCreatesNoPlacement()
        {
            CharacterRopePlacementRequest placement;
            CharacterRopeSpendRequest spend;

            Assert.That(CharacterRopePlacementPolicy.TryCreatePlacement(
                Input(ropeCount: 0), out placement, out spend), Is.False);
            Assert.That(CharacterRopePlacementPolicy.TryCreatePlacement(
                Input(ropeCount: -2), out placement, out spend), Is.False);
        }

        [Test]
        public void RopePlacement_BlockedOrOutOfBoundsOriginRefusesPlacement()
        {
            CharacterRopePlacementRequest placement;
            CharacterRopeSpendRequest spend;

            // 막힘/점유 원점 — 설치·소모 요청 없음.
            Assert.That(CharacterRopePlacementPolicy.TryCreatePlacement(
                Input(ropeCount: 4, placeable: false), out placement, out spend),
                Is.False);

            // 월드 범위 밖(유효 원점 없음) — 요청 없음.
            Assert.That(CharacterRopePlacementPolicy.TryCreatePlacement(
                Input(ropeCount: 4, hasValidOrigin: false), out placement, out spend),
                Is.False);
        }
    }
}
