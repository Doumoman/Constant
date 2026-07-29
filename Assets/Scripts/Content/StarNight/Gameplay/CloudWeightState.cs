using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CloudWeightState : MonoBehaviour
    {
        [SerializeField] private float baseMass;
        [SerializeField] private float baseGravity;
        [SerializeField] private float transferredWeight;
        [SerializeField] private int transferCount;

        private Rigidbody2D body;
        private Rigidbody2D Body => body != null ? body : body = GetComponent<Rigidbody2D>();

        public float BaseMass => baseMass;
        public float BaseGravity => baseGravity;
        public float TransferredWeight => transferredWeight;
        public float CurrentMass => Body != null ? Body.mass : 0f;
        public int TransferCount => transferCount;
        public bool IsAirborne =>
            Body != null && Body.simulated &&
            (Body.gravityScale <= 0f || Body.linearVelocity.y > 0.8f || transform.position.y > 4f);
        public bool IsOverpressured =>
            baseMass > 0f && transferredWeight > Mathf.Max(3f, baseMass * 2.75f);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            CaptureBaseState();
        }

        public void CaptureBaseState()
        {
            body = Body;
            if (body == null || baseMass > 0f)
            {
                return;
            }

            baseMass = Mathf.Max(0.1f, body.mass);
            baseGravity = body.gravityScale;
        }

        public float Extract(float maximumAmount)
        {
            CaptureBaseState();
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic)
            {
                return 0f;
            }

            float available = Mathf.Max(0f, body.mass - 0.12f);
            float requested = Mathf.Max(0.2f, body.mass * 0.62f);
            float amount = Mathf.Min(available, requested, Mathf.Max(0f, maximumAmount));
            if (amount <= 0.01f)
            {
                return 0f;
            }

            transferredWeight -= amount;
            transferCount++;
            ApplyPhysicalState();
            body.AddForce(Vector2.up * Mathf.Min(5f, amount * 2.2f), ForceMode2D.Impulse);
            return amount;
        }

        public void Deposit(float amount)
        {
            CaptureBaseState();
            if (body == null || amount <= 0f)
            {
                return;
            }

            transferredWeight += amount;
            transferCount++;
            ApplyPhysicalState();
            body.AddForce(Vector2.down * Mathf.Min(7f, amount * 2.4f), ForceMode2D.Impulse);
        }

        private void ApplyPhysicalState()
        {
            float safeBaseMass = Mathf.Max(0.1f, baseMass);
            body.mass = Mathf.Max(0.12f, safeBaseMass + transferredWeight);
            float normalizedShift = transferredWeight / safeBaseMass;
            body.gravityScale = Mathf.Clamp(baseGravity + normalizedShift * 4f, -0.65f, 3.8f);
            body.linearDamping = Mathf.Lerp(0.35f, 1.7f, Mathf.InverseLerp(-0.5f, 2.5f, normalizedShift));
        }

        public static CloudWeightState GetOrAdd(FableObject target)
        {
            if (target == null || target.Body == null)
            {
                return null;
            }

            CloudWeightState state = target.GetComponent<CloudWeightState>();
            return state != null ? state : target.gameObject.AddComponent<CloudWeightState>();
        }
    }
}
