#if LEGACY_DISABLED
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using UnityEngine;

namespace StarNight.Interaction.Carry
{
    public readonly struct ThrowResolution
    {
        public ThrowResolution(bool canThrow, bool shouldDropInstead, Vector2 velocity)
        {
            CanThrow = canThrow;
            ShouldDropInstead = shouldDropInstead;
            Velocity = velocity;
        }

        public bool CanThrow { get; }
        public bool ShouldDropInstead { get; }
        public Vector2 Velocity { get; }
    }

    public sealed class ThrowResolver
    {
        public ThrowResolution Resolve(CarryWeightClass weight, int facingSign, bool aimUp)
        {
            int facing = facingSign < 0 ? -1 : 1;
            if (weight == CarryWeightClass.Fixed)
            {
                return new ThrowResolution(false, false, Vector2.zero);
            }

            if (aimUp)
            {
                return weight switch
                {
                    CarryWeightClass.Light => new ThrowResolution(true, false, new Vector2(facing * 2.5f, 7.0f)),
                    CarryWeightClass.Medium => new ThrowResolution(true, false, new Vector2(facing * 1.8f, 5.6f)),
                    CarryWeightClass.Heavy => new ThrowResolution(false, true, Vector2.zero),
                    _ => new ThrowResolution(false, false, Vector2.zero),
                };
            }

            return weight switch
            {
                CarryWeightClass.Light => new ThrowResolution(true, false, new Vector2(facing * 6.5f, 2.0f)),
                CarryWeightClass.Medium => new ThrowResolution(true, false, new Vector2(facing * 5.0f, 1.5f)),
                CarryWeightClass.Heavy => new ThrowResolution(true, false, new Vector2(facing * 2.7f, 0.5f)),
                _ => new ThrowResolution(false, false, Vector2.zero),
            };
        }

        public bool IsThrowOriginClear(
            Vector2 origin,
            Vector2Int footprint,
            Vector2 velocity,
            LayerMask blockMask)
        {
            if (blockMask.value == 0 || velocity.sqrMagnitude <= Mathf.Epsilon)
            {
                return true;
            }

            Vector2 size = new Vector2(
                Mathf.Max(0.2f, footprint.x * 0.90f),
                Mathf.Max(0.2f, footprint.y * 0.90f));
            CapsuleDirection2D direction = size.y >= size.x
                ? CapsuleDirection2D.Vertical
                : CapsuleDirection2D.Horizontal;
            RaycastHit2D hit = Physics2D.CapsuleCast(
                origin,
                size,
                direction,
                0f,
                velocity.normalized,
                CarryableObject.PlayerCollisionRestoreDistance,
                blockMask);
            return hit.collider == null;
        }

        public bool TryThrow(
            PlayerHandSlot handSlot,
            PlayerActionContext context,
            int facingSign,
            LayerMask blockMask)
        {
            CarryableObject carryable = handSlot != null ? handSlot.HeldCarryable : null;
            CarryObjectDefinition definition = carryable != null ? carryable.Definition : null;
            if (definition == null || definition.PrimaryUseMode != PrimaryUseMode.Throw)
            {
                return false;
            }

            bool aimUp = context.LookVertical > 0.5f;
            ThrowResolution resolution = Resolve(definition.WeightClass, facingSign, aimUp);
            if (!resolution.CanThrow || resolution.ShouldDropInstead)
            {
                return false;
            }

            Vector2 origin = carryable.transform.position;
            if (!IsThrowOriginClear(origin, definition.Footprint, resolution.Velocity, blockMask))
            {
                return false;
            }

            return handSlot.TryThrowCurrent(origin, resolution.Velocity, context.ActionId);
        }
    }
}

#endif
