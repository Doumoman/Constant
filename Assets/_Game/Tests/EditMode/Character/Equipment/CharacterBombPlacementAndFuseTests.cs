using NUnit.Framework;
using StarNight.Character.Equipment;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.Equipment
{
    public sealed class CharacterBombPlacementAndFuseTests
    {
        private const int ActorId = 777;
        private static readonly WorldTileCoord Cell = new WorldTileCoord(10, 5);

        private static CharacterBombPlacementInput Input(
            int bombCount,
            bool hasValidCell = true,
            bool placeable = true)
        {
            return new CharacterBombPlacementInput(
                ActorId, hasValidCell, Cell, bombCount, placeable);
        }

        [Test]
        public void BombPlacement_AvailableBombCreatesPlacementAndSpendRequest()
        {
            CharacterBombPlacementRequest placement;
            CharacterBombSpendRequest spend;

            Assert.That(CharacterBombPlacementPolicy.TryCreatePlacement(
                Input(bombCount: 3), out placement, out spend), Is.True);

            Assert.That(placement.ActorId, Is.EqualTo(ActorId));
            Assert.That(placement.TargetCell.X, Is.EqualTo(10));
            Assert.That(placement.TargetCell.Y, Is.EqualTo(5));

            // 소모는 요청일 뿐이다 — 인벤토리 수량은 어디에서도 변조되지 않는다.
            Assert.That(spend.ActorId, Is.EqualTo(ActorId));
            Assert.That(spend.Amount, Is.EqualTo(1));
        }

        [Test]
        public void BombPlacement_NoAvailableBombCreatesNoPlacement()
        {
            CharacterBombPlacementRequest placement;
            CharacterBombSpendRequest spend;

            Assert.That(CharacterBombPlacementPolicy.TryCreatePlacement(
                Input(bombCount: 0), out placement, out spend), Is.False);
            Assert.That(CharacterBombPlacementPolicy.TryCreatePlacement(
                Input(bombCount: -1), out placement, out spend), Is.False);
        }

        [Test]
        public void BombPlacement_BlockedOrOutOfBoundsCellRefusesPlacement()
        {
            CharacterBombPlacementRequest placement;
            CharacterBombSpendRequest spend;

            // 막힘/점유 셀 — 설치·소모 요청 없음.
            Assert.That(CharacterBombPlacementPolicy.TryCreatePlacement(
                Input(bombCount: 3, placeable: false), out placement, out spend),
                Is.False);

            // 월드 범위 밖(브리지 변환 실패로 유효 셀 없음) — 요청 없음.
            Assert.That(CharacterBombPlacementPolicy.TryCreatePlacement(
                Input(bombCount: 3, hasValidCell: false), out placement, out spend),
                Is.False);
        }

        [Test]
        public void BombFuse_PositiveRemainingTimeCreatesNoExplosion()
        {
            var fuse = new CharacterBombFuse(
                1, ActorId, Cell, CharacterBombSettings.Default);
            CharacterExplosionRequest request;

            // 퓨즈(2.5s)가 남아 있는 동안은 폭발 요청이 없다.
            Assert.That(fuse.Tick(1.0f, out request), Is.False);
            Assert.That(fuse.Tick(1.0f, out request), Is.False);
            Assert.That(fuse.HasExploded, Is.False);
            Assert.That(fuse.RemainingFuseSeconds, Is.EqualTo(0.5f).Within(1e-4f));

            // 음수 delta는 0으로 clamp되어 시간이 되돌아가지 않는다.
            Assert.That(fuse.Tick(-5f, out request), Is.False);
            Assert.That(fuse.RemainingFuseSeconds, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void BombFuse_ReachesZeroCreatesSingleExplosionRequest()
        {
            var settings = CharacterBombSettings.Default;
            var fuse = new CharacterBombFuse(9, ActorId, Cell, settings);
            CharacterExplosionRequest request;

            Assert.That(fuse.Tick(settings.FuseSeconds + 1f, out request), Is.True);
            Assert.That(fuse.HasExploded, Is.True);
            Assert.That(request.ExplosionId, Is.EqualTo(9));
            Assert.That(request.OwnerId, Is.EqualTo(ActorId));
            Assert.That(request.CenterCell.X, Is.EqualTo(10));
            Assert.That(request.RadiusCells, Is.EqualTo(settings.ExplosionRadiusCells));
            Assert.That(request.DamageAmount, Is.EqualTo(settings.ExplosionDamageAmount));

            // 이후 틱에서는 다시 발행되지 않는다 — 정확히 한 번.
            Assert.That(fuse.Tick(1f, out request), Is.False);
            Assert.That(fuse.Tick(100f, out request), Is.False);
        }
    }
}
