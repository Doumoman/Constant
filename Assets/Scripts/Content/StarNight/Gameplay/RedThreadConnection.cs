using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class RedThreadConnection : MonoBehaviour
    {
        private RedThreadSystem owner;
        private FableObject endpointA;
        private FableObject endpointB;
        private LineRenderer line;
        private float restLength;
        private float stiffness;
        private float damping;
        private float breakTension;
        private float overstressDuration;
        private bool locked;

        public FableObject EndpointA => endpointA;
        public FableObject EndpointB => endpointB;
        public float RestLength => restLength;
        public float CurrentTension { get; private set; }
        public float TensionRatio => breakTension > 0f ? CurrentTension / breakTension : 0f;
        public bool Locked => locked;

        public void Configure(RedThreadSystem system, FableObject a, FableObject b, Material material,
            float contractedLength, float springStiffness, float springDamping, float snapTension)
        {
            owner = system;
            endpointA = a;
            endpointB = b;
            restLength = Mathf.Max(0.35f, contractedLength);
            stiffness = Mathf.Max(0.1f, springStiffness);
            damping = Mathf.Max(0f, springDamping);
            breakTension = Mathf.Max(1f, snapTension);

            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.sharedMaterial = material;
            line.widthMultiplier = 0.075f;
            line.numCapVertices = 3;
            line.sortingOrder = 46;
            UpdateLine();
        }

        public bool Contains(FableObject target) => target != null && (endpointA == target || endpointB == target);

        public bool Connects(FableObject a, FableObject b) =>
            a != null && b != null &&
            ((endpointA == a && endpointB == b) || (endpointA == b && endpointB == a));

        public FableObject Other(FableObject target)
        {
            if (endpointA == target) return endpointB;
            if (endpointB == target) return endpointA;
            return null;
        }

        public void LockAsRepaired()
        {
            locked = true;
            breakTension *= 4f;
            if (line != null)
            {
                line.widthMultiplier = 0.1f;
            }
        }

        private void FixedUpdate()
        {
            if (endpointA == null || endpointB == null)
            {
                owner?.BreakConnection(this, "끝점이 사라졌다", false);
                return;
            }

            Vector2 a = endpointA.transform.position;
            Vector2 b = endpointB.transform.position;
            Vector2 delta = b - a;
            float distance = delta.magnitude;
            CurrentTension = CalculateTension(distance, restLength, stiffness);
            if (distance > 0.001f && CurrentTension > 0f)
            {
                Vector2 direction = delta / distance;
                Vector2 velocityA = VelocityOf(endpointA.Body);
                Vector2 velocityB = VelocityOf(endpointB.Body);
                float separatingSpeed = Vector2.Dot(velocityB - velocityA, direction);
                float forceMagnitude = Mathf.Max(0f, CurrentTension + separatingSpeed * damping);
                Vector2 force = direction * forceMagnitude;
                ApplyForce(endpointA.Body, force);
                ApplyForce(endpointB.Body, -force);
            }

            if (!locked && CurrentTension > breakTension)
            {
                overstressDuration += Time.fixedDeltaTime;
                if (overstressDuration >= 0.22f)
                {
                    owner?.BreakConnection(this, "장력을 견디지 못했다", false);
                }
            }
            else
            {
                overstressDuration = Mathf.Max(0f, overstressDuration - Time.fixedDeltaTime * 2f);
            }
        }

        private void LateUpdate()
        {
            UpdateLine();
        }

        private void UpdateLine()
        {
            if (line == null || endpointA == null || endpointB == null)
            {
                return;
            }

            line.SetPosition(0, endpointA.transform.position);
            line.SetPosition(1, endpointB.transform.position);
            float danger = Mathf.Clamp01(TensionRatio);
            Color color = Color.Lerp(new Color(0.92f, 0.12f, 0.3f), new Color(1f, 0.82f, 0.25f), danger);
            line.startColor = color;
            line.endColor = color;
        }

        private static Vector2 VelocityOf(Rigidbody2D body)
        {
            return body != null && body.simulated ? body.linearVelocity : Vector2.zero;
        }

        private static void ApplyForce(Rigidbody2D body, Vector2 force)
        {
            if (body != null && body.simulated && body.bodyType == RigidbodyType2D.Dynamic)
            {
                body.AddForce(force, ForceMode2D.Force);
            }
        }

        public static float CalculateTension(float distance, float targetLength, float springStiffness)
        {
            return Mathf.Max(0f, distance - targetLength) * Mathf.Max(0f, springStiffness);
        }
    }
}
