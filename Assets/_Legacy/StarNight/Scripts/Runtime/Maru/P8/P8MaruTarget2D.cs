#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Objects;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DisallowMultipleComponent]
    public sealed class P8MaruTarget2D : MonoBehaviour
    {
        private static readonly HashSet<P8MaruTarget2D> ActiveInternal =
            new HashSet<P8MaruTarget2D>();

        [SerializeField] private P8MaruTargetKind targetKind =
            P8MaruTargetKind.Player;
        [SerializeField] private CarryableObject2D carryable;
        [SerializeField] private HandToolPickup2D handTool;
        [SerializeField] private bool available = true;
        [SerializeField] private bool inReturnPile;
        [SerializeField] private int stableOrder;

        public static IReadOnlyCollection<P8MaruTarget2D> ActiveTargets
        {
            get
            {
                ActiveInternal.RemoveWhere(value => value == null);
                return ActiveInternal;
            }
        }

        public event Action<P8MaruTarget2D> ReturnedToPile;
        public event Action<P8MaruTarget2D> RecoveredFromPile;

        public P8MaruTargetKind TargetKind => targetKind;
        public CarryableObject2D Carryable => carryable;
        public HandToolPickup2D HandTool => handTool;
        public bool InReturnPile => inReturnPile;
        public int StableOrder =>
            stableOrder != 0 ? stableOrder : GetInstanceID();
        public bool IsAvailable =>
            available
            && enabled
            && gameObject.activeInHierarchy
            && !inReturnPile
            && (carryable == null || !carryable.IsHeld)
            && (handTool == null || !handTool.IsHeld);

        public void Configure(
            P8MaruTargetKind kind,
            CarryableObject2D targetCarryable = null,
            HandToolPickup2D targetHandTool = null,
            int deterministicOrder = 0)
        {
            UnsubscribeRecovery();
            targetKind = kind;
            carryable = targetCarryable;
            handTool = targetHandTool;
            stableOrder = deterministicOrder;
            available = true;
            inReturnPile = false;
            if (enabled && gameObject.activeInHierarchy)
            {
                ActiveInternal.Add(this);
            }

            SubscribeRecovery();
        }

        private void OnEnable()
        {
            ActiveInternal.Add(this);
            SubscribeRecovery();
        }

        private void OnDisable()
        {
            ActiveInternal.Remove(this);
            UnsubscribeRecovery();
        }

        public bool MoveToReturnPile(Vector2 worldPosition)
        {
            if (!IsAvailable
                || targetKind == P8MaruTargetKind.Player)
            {
                return false;
            }

            bool moved;
            if (carryable != null)
            {
                moved = carryable.RelocateTo(worldPosition);
            }
            else if (handTool != null)
            {
                moved = handTool.RecoverTo(worldPosition);
            }
            else
            {
                transform.position = worldPosition;
                moved = true;
            }

            if (!moved)
            {
                return false;
            }

            inReturnPile = true;
            ReturnedToPile?.Invoke(this);
            return true;
        }

        public void SetAvailable(bool value)
        {
            available = value;
        }

        public void ReleaseFromPileForTests()
        {
            MarkRecovered();
        }

        private void SubscribeRecovery()
        {
            if (carryable != null)
            {
                carryable.PickedUp -= HandleCarryablePickedUp;
                carryable.PickedUp += HandleCarryablePickedUp;
            }

            if (handTool != null)
            {
                handTool.PickedUp -= HandleToolPickedUp;
                handTool.PickedUp += HandleToolPickedUp;
            }
        }

        private void UnsubscribeRecovery()
        {
            if (carryable != null)
            {
                carryable.PickedUp -= HandleCarryablePickedUp;
            }

            if (handTool != null)
            {
                handTool.PickedUp -= HandleToolPickedUp;
            }
        }

        private void HandleCarryablePickedUp(CarryableObject2D value)
        {
            MarkRecovered();
        }

        private void HandleToolPickedUp(HandToolPickup2D value)
        {
            MarkRecovered();
        }

        private void MarkRecovered()
        {
            if (!inReturnPile)
            {
                return;
            }

            inReturnPile = false;
            RecoveredFromPile?.Invoke(this);
        }
    }
}

#endif
