#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using StarNight.Tools.Water;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(GrowableVinePlatform2D))]
    public sealed class P11GrowingVine2D : MonoBehaviour
    {
        public const int MinimumGrowthCells = 3;
        public const int MaximumGrowthCells = 6;
        public const float DefaultIlluminatedWidthMultiplier = 1.75f;

        [SerializeField] private GrowableVinePlatform2D waterReactive;
        [SerializeField] private BoxCollider2D vineCollider;
        [SerializeField] private SpriteRenderer dryVisual;
        [SerializeField] private SpriteRenderer grownVisual;
        [SerializeField] private P11RotatingSunRay2D rotatingRay;
        [SerializeField, Range(MinimumGrowthCells, MaximumGrowthCells)]
        private int growthCells = MinimumGrowthCells;
        [SerializeField, Min(1f)] private float illuminatedWidthMultiplier =
            DefaultIlluminatedWidthMultiplier;
        [SerializeField] private bool illuminated;
        [SerializeField] private int geometryRevision;
        private Vector2 baseColliderSize = Vector2.one;
        private Vector2 baseColliderOffset;
        private Vector3 baseGrownVisualScale = Vector3.one;
        private bool subscribed;

        public event Action GeometryChanged;

        public GrowableVinePlatform2D WaterReactive => waterReactive;
        public bool IsGrown =>
            waterReactive != null && waterReactive.IsGrown;
        public bool Illuminated => illuminated;
        public int GrowthCells => growthCells;
        public float CurrentWidth => vineCollider != null
            ? vineCollider.size.x
            : 0f;
        public int GeometryRevision => geometryRevision;
        public bool IsConfigured =>
            waterReactive != null
            && vineCollider != null
            && growthCells >= MinimumGrowthCells
            && growthCells <= MaximumGrowthCells;

        public void Configure(
            WaterInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            BoxCollider2D targetCollider,
            SpriteRenderer targetDryVisual,
            SpriteRenderer targetGrownVisual,
            P11RotatingSunRay2D lightSource,
            int targetGrowthCells = MinimumGrowthCells,
            float lightWidthMultiplier =
                DefaultIlluminatedWidthMultiplier)
        {
            waterReactive = GetComponent<GrowableVinePlatform2D>();
            vineCollider = targetCollider != null
                ? targetCollider
                : GetComponent<BoxCollider2D>();
            dryVisual = targetDryVisual;
            grownVisual = targetGrownVisual;
            rotatingRay = lightSource;
            growthCells = Mathf.Clamp(
                targetGrowthCells,
                MinimumGrowthCells,
                MaximumGrowthCells);
            illuminatedWidthMultiplier = Mathf.Max(
                1f,
                lightWidthMultiplier);
            baseColliderSize = vineCollider != null
                ? vineCollider.size
                : Vector2.one;
            baseColliderOffset = vineCollider != null
                ? vineCollider.offset
                : Vector2.zero;
            baseGrownVisualScale = grownVisual != null
                ? grownVisual.transform.localScale
                : Vector3.one;
            illuminated = false;
            geometryRevision = 0;
            Subscribe();
            waterReactive.Configure(
                registry,
                world,
                cell,
                vineCollider,
                dryVisual,
                grownVisual,
                false);
            ApplyGeometry(false);
        }

        public bool RefreshIlluminationNow()
        {
            bool next = IsGrown
                && rotatingRay != null
                && rotatingRay.IsPointIlluminated(
                    GrowthCenterWorldPosition());
            return SetIlluminated(next);
        }

        public bool SetIlluminated(bool value)
        {
            bool next = IsGrown && value;
            if (illuminated == next)
            {
                return false;
            }

            illuminated = next;
            ApplyGeometry(true);
            return true;
        }

        public void ResetDryForTests()
        {
            waterReactive?.ResetDryForTests();
            illuminated = false;
            ApplyGeometry(true);
        }

        private void Awake()
        {
            waterReactive = GetComponent<GrowableVinePlatform2D>();
            if (vineCollider == null)
            {
                vineCollider = GetComponent<BoxCollider2D>();
            }

            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            RefreshIlluminationNow();
        }

        private void Subscribe()
        {
            if (subscribed || waterReactive == null)
            {
                return;
            }

            waterReactive.Grown += HandleGrown;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || waterReactive == null)
            {
                return;
            }

            waterReactive.Grown -= HandleGrown;
            subscribed = false;
        }

        private void HandleGrown()
        {
            illuminated = false;
            ApplyGeometry(true);
        }

        private Vector2 GrowthCenterWorldPosition()
        {
            return (Vector2)transform.position
                + Vector2.up * (growthCells * 0.5f);
        }

        private void ApplyGeometry(bool countRevision)
        {
            if (vineCollider != null)
            {
                if (IsGrown)
                {
                    float width = Mathf.Max(0.1f, baseColliderSize.x)
                        * (illuminated
                            ? illuminatedWidthMultiplier
                            : 1f);
                    float height = growthCells;
                    vineCollider.size = new Vector2(width, height);
                    vineCollider.offset = baseColliderOffset
                        + Vector2.up
                        * ((height - baseColliderSize.y) * 0.5f);
                    vineCollider.enabled = true;
                }
                else
                {
                    vineCollider.size = baseColliderSize;
                    vineCollider.offset = baseColliderOffset;
                    vineCollider.enabled = false;
                }
            }

            if (grownVisual != null)
            {
                grownVisual.transform.localScale = new Vector3(
                    baseGrownVisualScale.x
                    * (illuminated
                        ? illuminatedWidthMultiplier
                        : 1f),
                    baseGrownVisualScale.y * growthCells,
                    baseGrownVisualScale.z);
            }

            if (countRevision)
            {
                geometryRevision++;
                GeometryChanged?.Invoke();
            }
        }
    }
}

#endif
