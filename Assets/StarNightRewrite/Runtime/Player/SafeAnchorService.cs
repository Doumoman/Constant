using System;
using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [DisallowMultipleComponent]
    public sealed class SafeAnchorService : MonoBehaviour
    {
        [SerializeField]
        private Vector2 currentAnchor;

        [SerializeField]
        private bool hasAnchor;

        public event Action<Vector2> AnchorChanged;

        public Vector2 CurrentAnchor => currentAnchor;
        public bool HasAnchor => hasAnchor;

        private void Awake()
        {
            if (!hasAnchor)
            {
                Register(transform.position);
            }
        }

        public void Register(Vector2 worldPosition)
        {
            currentAnchor = worldPosition;
            hasAnchor = true;
            AnchorChanged?.Invoke(currentAnchor);
        }

        public bool Recover(PlayerMotor2D motor)
        {
            if (!hasAnchor || motor == null)
            {
                return false;
            }

            motor.Teleport(currentAnchor);
            return true;
        }
    }
}
