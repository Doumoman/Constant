#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11LightReactivePlatform2D : MonoBehaviour
    {
        [SerializeField] private Collider2D platformCollider;
        [SerializeField] private SpriteRenderer platformVisual;
        [SerializeField] private Transform lightTarget;
        [SerializeField] private bool illuminated;
        [SerializeField] private int illuminationChangeCount;

        public bool Illuminated => illuminated;
        public Collider2D PlatformCollider => platformCollider;
        public SpriteRenderer PlatformVisual => platformVisual;
        public Vector2 LightTargetPosition => lightTarget != null
            ? lightTarget.position
            : transform.position;
        public bool PhysicalColliderEnabled =>
            platformCollider != null && platformCollider.enabled;
        public int IlluminationChangeCount =>
            illuminationChangeCount;
        public bool IsConfigured =>
            platformCollider != null && platformVisual != null;

        public void Configure(
            Collider2D collider,
            SpriteRenderer visual,
            bool startsIlluminated = false,
            Transform receiverTarget = null)
        {
            platformCollider = collider;
            platformVisual = visual;
            lightTarget = receiverTarget;
            illuminated = startsIlluminated;
            illuminationChangeCount = 0;
            RefreshState();
        }

        public void SetIlluminated(bool value)
        {
            if (illuminated == value)
            {
                RefreshState();
                return;
            }

            illuminated = value;
            illuminationChangeCount++;
            RefreshState();
        }

        public bool TrySetIlluminated(bool value)
        {
            if (illuminated == value)
            {
                return false;
            }

            SetIlluminated(value);
            return true;
        }

        private void RefreshState()
        {
            if (platformCollider != null)
            {
                platformCollider.enabled = illuminated;
            }

            if (platformVisual != null)
            {
                platformVisual.color = illuminated
                    ? new Color(1f, 0.82f, 0.28f, 1f)
                    : new Color(0.25f, 0.16f, 0.38f, 0.55f);
            }
        }
    }
}

#endif
