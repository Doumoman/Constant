using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Interaction;
using UnityEngine;

namespace StarNight.Character.Tests.Interaction
{
    public sealed class CharacterCarryInteractionTests
    {
        private const int OwnerId = 777;

        private sealed class FakePlacementSpaceQuery : ICharacterPlacementSpaceQuery
        {
            private readonly HashSet<Vector2> blocked = new HashSet<Vector2>();

            public void Block(Vector2 position)
            {
                blocked.Add(position);
            }

            public bool IsPlacementFree(Vector2 position)
            {
                return !blocked.Contains(position);
            }
        }

        private static CharacterCarryCandidate Stone(int id = 42)
        {
            return new CharacterCarryCandidate(
                id,
                CharacterCarryCandidateKind.OrdinaryCarryable,
                new Vector2(1f, 0f),
                1f, 1f, true, true, 0);
        }

        private static CharacterCarryInteraction CreateCarrying(
            FakePlacementSpaceQuery placement)
        {
            var interaction = new CharacterCarryInteraction(
                CharacterCarryInteractionSettings.Default, placement, OwnerId);

            Assert.That(interaction.TryPickUp(Stone()), Is.True);
            return interaction;
        }

        [Test]
        public void CarrySlot_PickupFillsSingleSlotAndRejectsSecondPickup()
        {
            var interaction = new CharacterCarryInteraction(
                CharacterCarryInteractionSettings.Default,
                new FakePlacementSpaceQuery(),
                OwnerId);

            Assert.That(interaction.IsCarrying, Is.False);
            Assert.That(interaction.TryPickUp(Stone(42)), Is.True);
            Assert.That(interaction.IsCarrying, Is.True);
            Assert.That(interaction.HeldObjectId, Is.EqualTo(42));

            // 슬롯이 차 있으면 두 번째 들기는 거부되고 기존 휴대물이 유지된다.
            Assert.That(interaction.TryPickUp(Stone(43)), Is.False);
            Assert.That(interaction.HeldObjectId, Is.EqualTo(42));

            // 부적격 후보(1×1 초과)도 거부된다.
            var oversized = new CharacterCarryCandidate(
                44, CharacterCarryCandidateKind.OrdinaryCarryable,
                Vector2.zero, 2f, 1f, true, true, 0);
            var empty = new CharacterCarryInteraction(
                CharacterCarryInteractionSettings.Default,
                new FakePlacementSpaceQuery(),
                OwnerId);

            Assert.That(empty.TryPickUp(oversized), Is.False);
            Assert.That(empty.IsCarrying, Is.False);
        }

        [Test]
        public void CarryDrop_DownActionCreatesSafeDropPlacementRequest()
        {
            var interaction = CreateCarrying(new FakePlacementSpaceQuery());

            // 아래+행동 조합은 CHAR01 입력 계약의 SafeDrop intent다.
            var downAndAction = new CharacterInputSnapshot(
                0f, true,
                CharacterButtonSnapshot.Idle(0L),
                CharacterButtonSnapshot.Pressed(0L),
                CharacterButtonSnapshot.Idle(0L),
                CharacterButtonSnapshot.Idle(0L));

            Assert.That(downAndAction.SafeDropPressedThisFrame, Is.True);

            var feet = new Vector2(5f, 3f);
            CharacterCarryPlacementRequest request;

            Assert.That(interaction.TryCreateSafeDrop(feet, out request), Is.True);
            Assert.That(request.HeldObjectId, Is.EqualTo(42));
            Assert.That(request.OwnerId, Is.EqualTo(OwnerId));
            Assert.That(request.Position, Is.EqualTo(
                feet + interaction.Settings.SafeDropOffset));
            Assert.That(request.OwnerCollisionGraceSeconds, Is.EqualTo(
                interaction.Settings.OwnerCollisionGraceSeconds));

            // 수락된 내려놓기만 슬롯을 비운다.
            Assert.That(interaction.IsCarrying, Is.False);
        }

        [Test]
        public void CarryDrop_BlockedDestinationRejectsAndKeepsHeldObject()
        {
            var placement = new FakePlacementSpaceQuery();
            var interaction = CreateCarrying(placement);
            var feet = new Vector2(5f, 3f);

            placement.Block(feet + interaction.Settings.SafeDropOffset);

            CharacterCarryPlacementRequest request;

            // 막힌 목적지: 겹침 배치 없이 거부, 휴대 유지.
            Assert.That(interaction.TryCreateSafeDrop(feet, out request), Is.False);
            Assert.That(interaction.IsCarrying, Is.True);
            Assert.That(interaction.HeldObjectId, Is.EqualTo(42));
        }

        [Test]
        public void CarryThrow_RightActionCreatesRightThrowRequest()
        {
            var interaction = CreateCarrying(new FakePlacementSpaceQuery());
            CharacterThrowDirection direction;

            Assert.That(CharacterThrowDirectionResolver.TryResolve(
                false, 1f, out direction), Is.True);
            Assert.That(direction, Is.EqualTo(CharacterThrowDirection.Right));

            CharacterCarryThrowRequest request;

            Assert.That(interaction.TryCreateThrow(direction, out request), Is.True);
            Assert.That(request.Direction, Is.EqualTo(CharacterThrowDirection.Right));
            Assert.That(request.DirectionVector, Is.EqualTo(Vector2.right));
            Assert.That(request.Speed, Is.EqualTo(interaction.Settings.ThrowSpeed));
            Assert.That(request.HeldObjectId, Is.EqualTo(42));
            Assert.That(request.OwnerId, Is.EqualTo(OwnerId));
            Assert.That(interaction.IsCarrying, Is.False);
        }

        [Test]
        public void CarryThrow_LeftActionCreatesLeftThrowRequest()
        {
            var interaction = CreateCarrying(new FakePlacementSpaceQuery());
            CharacterThrowDirection direction;

            Assert.That(CharacterThrowDirectionResolver.TryResolve(
                false, -1f, out direction), Is.True);
            Assert.That(direction, Is.EqualTo(CharacterThrowDirection.Left));

            CharacterCarryThrowRequest request;

            Assert.That(interaction.TryCreateThrow(direction, out request), Is.True);
            Assert.That(request.Direction, Is.EqualTo(CharacterThrowDirection.Left));
            Assert.That(request.DirectionVector, Is.EqualTo(Vector2.left));
            Assert.That(interaction.IsCarrying, Is.False);
        }

        [Test]
        public void CarryThrow_UpActionCreatesUpThrowRequestAndHasPriority()
        {
            var interaction = CreateCarrying(new FakePlacementSpaceQuery());
            CharacterThrowDirection direction;

            // 결정적 우선순위: 위+수평 동시 입력이면 Up이 우선한다.
            Assert.That(CharacterThrowDirectionResolver.TryResolve(
                true, 1f, out direction), Is.True);
            Assert.That(direction, Is.EqualTo(CharacterThrowDirection.Up));

            CharacterCarryThrowRequest request;

            Assert.That(interaction.TryCreateThrow(direction, out request), Is.True);
            Assert.That(request.Direction, Is.EqualTo(CharacterThrowDirection.Up));
            Assert.That(request.DirectionVector, Is.EqualTo(Vector2.up));
            Assert.That(interaction.IsCarrying, Is.False);
        }

        [Test]
        public void CarryThrow_RejectedThrowKeepsHeldObject()
        {
            // 방향 입력이 없으면 투척 의도가 아니다 — 휴대 유지.
            var interaction = CreateCarrying(new FakePlacementSpaceQuery());
            CharacterThrowDirection direction;

            Assert.That(CharacterThrowDirectionResolver.TryResolve(
                false, 0f, out direction), Is.False);
            Assert.That(interaction.IsCarrying, Is.True);
            Assert.That(interaction.HeldObjectId, Is.EqualTo(42));

            // 휴대 중이 아니면 투척 요청 자체가 거부된다.
            var empty = new CharacterCarryInteraction(
                CharacterCarryInteractionSettings.Default,
                new FakePlacementSpaceQuery(),
                OwnerId);
            CharacterCarryThrowRequest request;

            Assert.That(empty.TryCreateThrow(
                CharacterThrowDirection.Right, out request), Is.False);
        }

        [Test]
        public void CarryOwnerCollisionGrace_IsCentralizedAndIncludedInDropAndThrowRequests()
        {
            // grace는 설정에서만 중앙 관리되며 음수는 거부된다.
            Assert.That(() => new CharacterCarryInteractionSettings(
                    Vector2.zero, 7f, -0.1f),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());

            var settings = new CharacterCarryInteractionSettings(
                Vector2.zero, 5f, 0.4f);
            var placement = new FakePlacementSpaceQuery();
            var interaction = new CharacterCarryInteraction(settings, placement, OwnerId);

            Assert.That(interaction.TryPickUp(Stone()), Is.True);

            CharacterCarryPlacementRequest drop;

            Assert.That(interaction.TryCreateSafeDrop(Vector2.zero, out drop), Is.True);
            Assert.That(drop.OwnerCollisionGraceSeconds, Is.EqualTo(0.4f));

            Assert.That(interaction.TryPickUp(Stone(43)), Is.True);

            CharacterCarryThrowRequest thrown;

            Assert.That(interaction.TryCreateThrow(
                CharacterThrowDirection.Up, out thrown), Is.True);
            Assert.That(thrown.OwnerCollisionGraceSeconds, Is.EqualTo(0.4f));
            Assert.That(thrown.OwnerId, Is.EqualTo(OwnerId));
        }
    }
}
