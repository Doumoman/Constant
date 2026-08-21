#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DisallowMultipleComponent]
    public sealed class P8ReturnPile2D : MonoBehaviour
    {
        [SerializeField] private Transform depositAnchor;
        [SerializeField] private Vector2 slotSpacing =
            new Vector2(0.45f, 0.18f);
        [SerializeField, Min(1)] private int rowWidth = 3;

        private readonly List<P8MaruTarget2D> deposited =
            new List<P8MaruTarget2D>();

        public event Action<P8MaruTarget2D> ItemDeposited;
        public event Action<P8MaruTarget2D> ItemRecovered;

        public Transform DepositAnchor =>
            depositAnchor != null ? depositAnchor : transform;
        public IReadOnlyList<P8MaruTarget2D> Deposited => deposited;
        public int DepositedCount => deposited.Count;

        public void Configure(
            Transform anchor = null,
            Vector2? spacing = null,
            int configuredRowWidth = 3)
        {
            depositAnchor = anchor != null ? anchor : transform;
            slotSpacing = spacing ?? new Vector2(0.45f, 0.18f);
            rowWidth = Mathf.Max(1, configuredRowWidth);
        }

        public bool Deposit(P8MaruTarget2D target)
        {
            PurgeMissing();
            if (target == null
                || target.TargetKind == P8MaruTargetKind.Player
                || deposited.Contains(target))
            {
                return false;
            }

            Vector2 position = SlotPosition(deposited.Count);
            if (!target.MoveToReturnPile(position))
            {
                return false;
            }

            deposited.Add(target);
            target.RecoveredFromPile -= HandleRecovered;
            target.RecoveredFromPile += HandleRecovered;
            ItemDeposited?.Invoke(target);
            return true;
        }

        public bool Contains(P8MaruTarget2D target)
        {
            PurgeMissing();
            return target != null && deposited.Contains(target);
        }

        public void ClearForTests()
        {
            for (int index = 0; index < deposited.Count; index++)
            {
                if (deposited[index] != null)
                {
                    deposited[index].RecoveredFromPile -= HandleRecovered;
                }
            }

            deposited.Clear();
        }

        private Vector2 SlotPosition(int index)
        {
            int column = index % rowWidth;
            int row = index / rowWidth;
            Vector2 origin = DepositAnchor.position;
            return origin
                + new Vector2(
                    column * slotSpacing.x,
                    row * slotSpacing.y);
        }

        private void HandleRecovered(P8MaruTarget2D target)
        {
            if (!deposited.Remove(target))
            {
                return;
            }

            target.RecoveredFromPile -= HandleRecovered;
            ItemRecovered?.Invoke(target);
        }

        private void PurgeMissing()
        {
            for (int index = deposited.Count - 1; index >= 0; index--)
            {
                if (deposited[index] == null)
                {
                    deposited.RemoveAt(index);
                }
            }
        }
    }
}

#endif
