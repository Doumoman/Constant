#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public sealed class GroundProbe2D : MonoBehaviour
    {
        [SerializeField] private CapsuleCollider2D capsule;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float probeDistance = 0.08f;
        [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.55f;

        public bool IsGrounded { get; private set; }
        public RaycastHit2D GroundHit { get; private set; }

        public void Configure(CapsuleCollider2D targetCapsule, LayerMask layers, float distance)
        {
            capsule = targetCapsule;
            groundLayers = layers;
            probeDistance = distance;
        }

        private void Awake()
        {
            if (capsule == null)
            {
                capsule = GetComponent<CapsuleCollider2D>();
            }
        }

        public bool Refresh()
        {
            if (capsule == null)
            {
                IsGrounded = false;
                GroundHit = default;
                return false;
            }

            Bounds bounds = capsule.bounds;
            Vector2 probeSize = new Vector2(
                Mathf.Max(0.05f, bounds.size.x * 0.88f),
                Mathf.Max(0.05f, bounds.size.y * 0.94f));

            GroundHit = Physics2D.CapsuleCast(
                bounds.center,
                probeSize,
                CapsuleDirection2D.Vertical,
                transform.eulerAngles.z,
                Vector2.down,
                probeDistance,
                groundLayers.value);

            IsGrounded = GroundHit.collider != null && GroundHit.normal.y >= minimumGroundNormalY;
            return IsGrounded;
        }
    }
}

#endif
