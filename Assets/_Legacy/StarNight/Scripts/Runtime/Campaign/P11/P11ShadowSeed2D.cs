#if LEGACY_DISABLED
using System;
using StarNight.Objects;
using StarNight.Player;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(CarryableObject2D))]
    public sealed class P11ShadowSeed2D : MonoBehaviour
    {
        public const float DefaultSlowMultiplier = 0.55f;

        [SerializeField] private CarryableObject2D carryable;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D slowTrigger;
        [SerializeField] private P11RotatingSunRay2D rotatingRay;
        [SerializeField] private P11LightReactivePlatform2D flowerPlatform;
        [SerializeField] private SpriteRenderer seedVisual;
        [SerializeField, Range(0.1f, 0.95f)]
        private float slowMultiplier = DefaultSlowMultiplier;
        [SerializeField] private bool consumedByLight;
        [SerializeField] private int slowApplicationCount;
        [SerializeField] private Rigidbody2D lastSlowedBody;

        public event Action ConsumedByLight;

        public CarryableObject2D Carryable => carryable;
        public P11RotatingSunRay2D RotatingRay => rotatingRay;
        public P11LightReactivePlatform2D FlowerPlatform =>
            flowerPlatform;
        public float SlowMultiplier => slowMultiplier;
        public bool IsConsumed => consumedByLight;
        public bool DealsDamage => false;
        public int SlowApplicationCount => slowApplicationCount;
        public Rigidbody2D LastSlowedBody => lastSlowedBody;
        public bool CarryThrowCompatible =>
            carryable != null
            && (carryable.Traits & WorldObjectTraits.Carryable) != 0;
        public bool IsConfigured =>
            carryable != null
            && body != null
            && slowTrigger != null
            && rotatingRay != null
            && flowerPlatform != null;

        public void Configure(
            CarryableObject2D targetCarryable,
            Collider2D targetSlowTrigger,
            P11RotatingSunRay2D lightSource,
            P11LightReactivePlatform2D targetFlowerPlatform,
            SpriteRenderer visual = null,
            float movementSlowMultiplier = DefaultSlowMultiplier)
        {
            carryable = targetCarryable != null
                ? targetCarryable
                : GetComponent<CarryableObject2D>();
            body = GetComponent<Rigidbody2D>();
            slowTrigger = targetSlowTrigger;
            rotatingRay = lightSource;
            flowerPlatform = targetFlowerPlatform;
            seedVisual = visual;
            slowMultiplier = Mathf.Clamp(
                movementSlowMultiplier,
                0.1f,
                0.95f);
            if (slowTrigger != null)
            {
                slowTrigger.isTrigger = true;
            }

            consumedByLight = false;
            slowApplicationCount = 0;
            lastSlowedBody = null;
            if (seedVisual != null)
            {
                seedVisual.enabled = true;
            }
        }

        public bool TryApplySlow(PlayerMotor2D playerMotor)
        {
            if (consumedByLight || playerMotor == null)
            {
                return false;
            }

            Rigidbody2D playerBody = playerMotor.Body != null
                ? playerMotor.Body
                : playerMotor.GetComponent<Rigidbody2D>();
            if (playerBody == null)
            {
                return false;
            }

            Vector2 velocity = playerBody.linearVelocity;
            playerBody.linearVelocity = new Vector2(
                velocity.x * slowMultiplier,
                velocity.y);
            lastSlowedBody = playerBody;
            slowApplicationCount++;
            return true;
        }

        public bool TryApplySlow(Collider2D other)
        {
            return other != null
                && TryApplySlow(
                    other.GetComponentInParent<PlayerMotor2D>());
        }

        public bool EvaluateIlluminationNow()
        {
            return !consumedByLight
                && rotatingRay != null
                && rotatingRay.IsPointIlluminated(transform.position)
                && ConsumeSeedByLight();
        }

        public bool ConsumeSeedByLight()
        {
            if (consumedByLight)
            {
                return false;
            }

            consumedByLight = true;
            flowerPlatform?.SetIlluminated(true);
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
            }

            Collider2D[] colliders =
                GetComponentsInChildren<Collider2D>(true);
            for (int index = 0; index < colliders.Length; index++)
            {
                colliders[index].enabled = false;
            }

            if (carryable != null)
            {
                carryable.enabled = false;
            }

            if (seedVisual != null)
            {
                seedVisual.enabled = false;
            }

            ConsumedByLight?.Invoke();
            return true;
        }

        private void Awake()
        {
            if (carryable == null)
            {
                carryable = GetComponent<CarryableObject2D>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        private void Update()
        {
            EvaluateIlluminationNow();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryApplySlow(other);
        }
    }
}

#endif
