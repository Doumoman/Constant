using System;
using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(PlayerMotor2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerCarry : MonoBehaviour
    {
        [SerializeField]
        private Transform holdPoint;

        [SerializeField]
        private float holdDistance = 0.78f;

        [SerializeField]
        private Vector2 throwVelocity = new Vector2(7f, 3.5f);

        private PlayerMotor2D motor;
        private Carryable2D carried;

        public event Action<Carryable2D> CarryChanged;

        public bool IsCarrying => carried != null;
        public Carryable2D Carried => carried;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor2D>();
            if (holdPoint == null)
            {
                GameObject point = new GameObject("Carry Hold Point");
                point.transform.SetParent(transform, false);
                holdPoint = point.transform;
            }
        }

        private void LateUpdate()
        {
            if (holdPoint != null)
            {
                holdPoint.localPosition =
                    new Vector3(holdDistance * motor.FacingSign, 0.15f, 0f);
            }
        }

        public bool TryPickUp(Carryable2D item)
        {
            if (IsCarrying || item == null || !item.CanBeCarried)
            {
                return false;
            }

            carried = item;
            carried.AttachTo(holdPoint);
            CarryChanged?.Invoke(carried);
            return true;
        }

        public void Release(bool throwItem)
        {
            if (!IsCarrying)
            {
                return;
            }

            Carryable2D released = carried;
            carried = null;

            Vector2 velocity = throwItem
                ? new Vector2(throwVelocity.x * motor.FacingSign, throwVelocity.y)
                : Vector2.zero;
            released.Detach(velocity);
            CarryChanged?.Invoke(null);
        }
    }
}
