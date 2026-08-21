#if LEGACY_DISABLED
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.State;
using UnityEngine;

namespace StarNight.Interaction.Carry
{
    [DisallowMultipleComponent]
    public sealed class HeldObjectClearanceSensor : MonoBehaviour
    {
        [SerializeField] private PlayerHandSlot handSlot;
        [SerializeField] private ProjectPhysicsProfile physicsProfile;
        [SerializeField] private float probeDistance = 0.08f;

        private readonly Collider2D[] overlaps = new Collider2D[16];

        public bool CanMoveLeft { get; private set; } = true;
        public bool CanMoveRight { get; private set; } = true;
        public bool HasCeilingClearance { get; private set; } = true;

        private void FixedUpdate()
        {
            Refresh();
        }

        public void Refresh()
        {
            CarryableObject carryable = handSlot != null ? handSlot.HeldCarryable : null;
            CarryObjectDefinition definition = carryable != null ? carryable.Definition : null;
            if (definition == null
                || definition.WeightClass != CarryWeightClass.Heavy
                || definition.Footprint.y < 2)
            {
                CanMoveLeft = true;
                CanMoveRight = true;
                HasCeilingClearance = true;
                return;
            }

            CanMoveLeft = IsClear(carryable, Vector2.left * probeDistance);
            CanMoveRight = IsClear(carryable, Vector2.right * probeDistance);
            HasCeilingClearance = IsClear(carryable, Vector2.up * probeDistance);
        }

        public bool AllowsDirection(Vector2 direction)
        {
            if (direction.x < -0.01f)
            {
                return CanMoveLeft;
            }
            if (direction.x > 0.01f)
            {
                return CanMoveRight;
            }
            return direction.y <= 0.01f || HasCeilingClearance;
        }

        public void ConfigureForTests(PlayerHandSlot slot, ProjectPhysicsProfile profile)
        {
            handSlot = slot;
            physicsProfile = profile;
        }

        private bool IsClear(CarryableObject carryable, Vector2 offset)
        {
            if (physicsProfile == null || physicsProfile.DropBlockMask.value == 0)
            {
                return true;
            }

            Vector2Int footprint = carryable.Definition.Footprint;
            Vector2 size = new Vector2(footprint.x * 0.90f, footprint.y * 0.90f);
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(physicsProfile.DropBlockMask);
            int count = Physics2D.OverlapBox(
                (Vector2)carryable.transform.position + offset,
                size,
                0f,
                filter,
                overlaps);
            for (int index = 0; index < count; index++)
            {
                if (overlaps[index] != null && !overlaps[index].transform.IsChildOf(carryable.transform))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

#endif
