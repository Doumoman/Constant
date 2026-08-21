#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Interaction.Carry
{
    [Serializable]
    public struct CarryObjectSnapshot
    {
        public string ObjectId;
        public CarryWeightClass WeightClass;
        public Vector2Int Footprint;
        public Vector2 Position;
        public Vector2 Velocity;
        public bool HeldInHandSlot;
        public bool CriticalCarry;
        public long LastActionId;
        public CarryRuntimeState RuntimeState;
        public float Rotation;
        public bool Active;

        public static CarryObjectSnapshot Capture(CarryableObject carryable, bool heldInHandSlot)
        {
            CarryObjectDefinition definition = carryable != null ? carryable.Definition : null;
            return new CarryObjectSnapshot
            {
                ObjectId = definition != null ? definition.ObjectId : string.Empty,
                WeightClass = definition != null ? definition.WeightClass : CarryWeightClass.Fixed,
                Footprint = definition != null ? definition.Footprint : Vector2Int.one,
                Position = carryable != null ? (Vector2)carryable.transform.position : Vector2.zero,
                Velocity = carryable != null ? carryable.Velocity : Vector2.zero,
                HeldInHandSlot = heldInHandSlot,
                CriticalCarry = definition != null && definition.CriticalCarry,
                LastActionId = carryable != null ? carryable.LastActionId : 0,
                RuntimeState = carryable != null ? carryable.RuntimeState : CarryRuntimeState.World,
                Rotation = carryable != null ? carryable.transform.eulerAngles.z : 0f,
                Active = carryable != null && carryable.gameObject.activeSelf,
            };
        }
    }

    public interface ICarryPortalClearance
    {
        bool Allows(CarryObjectDefinition definition);
    }
}

#endif
