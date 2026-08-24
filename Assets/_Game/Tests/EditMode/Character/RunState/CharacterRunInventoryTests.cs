using NUnit.Framework;
using StarNight.Character.Equipment;
using StarNight.Character.RunState;

namespace StarNight.Character.Tests.RunState
{
    public sealed class CharacterRunInventoryTests
    {
        private const int PlayerId = 777;

        [Test]
        public void RunInventory_DefaultBombAndRopeCountsAreCentralized()
        {
            // 시작 수량은 중앙 설정 한 곳(레거시 RunState 선례 4/4)에서 온다.
            var settings = CharacterRunStateSettings.Default;
            Assert.That(settings.StartingBombCount, Is.EqualTo(4));
            Assert.That(settings.StartingRopeCount, Is.EqualTo(4));

            var inventory = CharacterRunInventoryState.CreateStarting(
                PlayerId, in settings);
            Assert.That(inventory.ActorId, Is.EqualTo(PlayerId));
            Assert.That(inventory.BombCount, Is.EqualTo(4));
            Assert.That(inventory.RopeCount, Is.EqualTo(4));

            // 생성자는 음수를 0으로 clamp한다.
            var clamped = new CharacterRunInventoryState(PlayerId, -1, -5);
            Assert.That(clamped.BombCount, Is.EqualTo(0));
            Assert.That(clamped.RopeCount, Is.EqualTo(0));
        }

        [Test]
        public void RunInventory_BombAndRopeSpendRequestsDecreaseCounts()
        {
            var inventory = CharacterRunInventoryState.CreateStarting(
                PlayerId, CharacterRunStateSettings.Default);

            // CHAR05_01 폭탄 소모 요청을 그대로 소비한다.
            var bombResult = CharacterRunInventoryPolicy.ApplyBombSpend(
                in inventory, new CharacterBombSpendRequest(PlayerId, 1));

            Assert.That(bombResult.AppliedAmount, Is.EqualTo(1));
            Assert.That(bombResult.NewState.BombCount, Is.EqualTo(3));
            Assert.That(bombResult.NewState.RopeCount, Is.EqualTo(4));

            // CHAR05_02 로프 소모 요청을 그대로 소비한다.
            var ropeResult = CharacterRunInventoryPolicy.ApplyRopeSpend(
                bombResult.NewState, new CharacterRopeSpendRequest(PlayerId, 2));

            Assert.That(ropeResult.AppliedAmount, Is.EqualTo(2));
            Assert.That(ropeResult.NewState.RopeCount, Is.EqualTo(2));
            Assert.That(ropeResult.NewState.BombCount, Is.EqualTo(3));
        }

        [Test]
        public void RunInventory_SpendCannotGoBelowZeroOrMutateInput()
        {
            var inventory = new CharacterRunInventoryState(PlayerId, 1, 0);

            // 보유(1)보다 큰 소모(5) → 0에서 멈추고 실제 적용분만 기록.
            var bombResult = CharacterRunInventoryPolicy.ApplyBombSpend(
                in inventory, new CharacterBombSpendRequest(PlayerId, 5));

            Assert.That(bombResult.NewState.BombCount, Is.EqualTo(0));
            Assert.That(bombResult.AppliedAmount, Is.EqualTo(1));

            // 입력 상태는 불변이다.
            Assert.That(inventory.BombCount, Is.EqualTo(1));

            // 보유 0에서 로프 소모 → 변화 없음.
            var ropeResult = CharacterRunInventoryPolicy.ApplyRopeSpend(
                in inventory, new CharacterRopeSpendRequest(PlayerId, 1));
            Assert.That(ropeResult.AppliedAmount, Is.EqualTo(0));
            Assert.That(ropeResult.Changed, Is.False);
            Assert.That(ropeResult.NewState.RopeCount, Is.EqualTo(0));

            // 대상 불일치·비양수 요청 → 변화 없음.
            Assert.That(CharacterRunInventoryPolicy.ApplyBombSpend(
                in inventory, new CharacterBombSpendRequest(999, 1))
                .AppliedAmount, Is.EqualTo(0));
            Assert.That(CharacterRunInventoryPolicy.ApplyBombSpend(
                in inventory, new CharacterBombSpendRequest(PlayerId, 0))
                .AppliedAmount, Is.EqualTo(0));
            Assert.That(CharacterRunInventoryPolicy.ApplyBombSpend(
                in inventory, new CharacterBombSpendRequest(PlayerId, -2))
                .AppliedAmount, Is.EqualTo(0));
        }
    }
}
