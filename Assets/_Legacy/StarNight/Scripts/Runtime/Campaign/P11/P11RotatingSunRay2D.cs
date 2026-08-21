#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11RotatingSunRay2D : MonoBehaviour
    {
        public const float TelegraphSeconds = 0.8f;

        [SerializeField, Min(1f)] private float degreesPerSecond = 18f;
        [SerializeField] private SpriteRenderer telegraphVisual;
        [SerializeField] private P11LightReactivePlatform2D[] receivers =
            Array.Empty<P11LightReactivePlatform2D>();
        [SerializeField] private bool active = true;
        [SerializeField, Min(0.5f)] private float maxDistance = 18f;
        [SerializeField, Min(0.05f)] private float beamHalfWidth =
            0.65f;
        [SerializeField] private int illuminatedReceiverCount;
        [SerializeField] private int evaluationCount;

        public float DegreesPerSecond => degreesPerSecond;
        public bool Active => active;
        public float MaxDistance => maxDistance;
        public float BeamHalfWidth => beamHalfWidth;
        public int ReceiverCount => receivers != null
            ? receivers.Length
            : 0;
        public int IlluminatedReceiverCount =>
            illuminatedReceiverCount;
        public int EvaluationCount => evaluationCount;
        public bool DamagesAtMostOneHeart => true;
        public bool GrowsDryVines => true;

        public void Configure(
            float rotationSpeed,
            SpriteRenderer warningVisual,
            P11LightReactivePlatform2D[] lightReceivers)
        {
            degreesPerSecond = Mathf.Max(1f, rotationSpeed);
            telegraphVisual = warningVisual;
            receivers = lightReceivers
                ?? Array.Empty<P11LightReactivePlatform2D>();
            active = true;
            illuminatedReceiverCount = 0;
            evaluationCount = 0;
        }

        public void Configure(
            float rotationSpeed,
            SpriteRenderer warningVisual,
            P11LightReactivePlatform2D[] lightReceivers,
            float reach,
            float halfWidth)
        {
            Configure(
                rotationSpeed,
                warningVisual,
                lightReceivers);
            maxDistance = Mathf.Max(0.5f, reach);
            beamHalfWidth = Mathf.Max(0.05f, halfWidth);
        }

        public float EvaluateAngle(float elapsedSeconds)
        {
            return Mathf.Repeat(
                elapsedSeconds * degreesPerSecond,
                360f);
        }

        public void SetActive(bool value)
        {
            active = value;
            if (telegraphVisual != null)
            {
                telegraphVisual.enabled = value;
            }

            if (!value)
            {
                IlluminateReceivers(false);
            }
        }

        public void IlluminateReceivers(bool illuminated)
        {
            illuminatedReceiverCount = 0;
            for (int index = 0; index < receivers.Length; index++)
            {
                receivers[index]?.SetIlluminated(illuminated);
                if (illuminated && receivers[index] != null)
                {
                    illuminatedReceiverCount++;
                }
            }
        }

        public int EvaluateIlluminationNow()
        {
            evaluationCount++;
            illuminatedReceiverCount = 0;
            for (int index = 0; index < receivers.Length; index++)
            {
                P11LightReactivePlatform2D receiver =
                    receivers[index];
                if (receiver == null)
                {
                    continue;
                }

                bool illuminated = IsPointIlluminated(
                    receiver.LightTargetPosition);
                receiver.SetIlluminated(illuminated);
                if (illuminated)
                {
                    illuminatedReceiverCount++;
                }
            }

            return illuminatedReceiverCount;
        }

        public bool IsPointIlluminated(Vector2 worldPosition)
        {
            if (!active)
            {
                return false;
            }

            Vector2 origin = transform.position;
            Vector2 forward = transform.right;
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Vector2.right;
            }

            forward.Normalize();
            Vector2 offset = worldPosition - origin;
            float forwardDistance = Vector2.Dot(offset, forward);
            float perpendicularDistance = Mathf.Abs(
                forward.x * offset.y - forward.y * offset.x);
            return forwardDistance >= 0f
                && forwardDistance <= maxDistance
                && perpendicularDistance <= beamHalfWidth;
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            transform.Rotate(
                0f,
                0f,
                degreesPerSecond * Time.deltaTime);
            EvaluateIlluminationNow();
        }
    }
}

#endif
