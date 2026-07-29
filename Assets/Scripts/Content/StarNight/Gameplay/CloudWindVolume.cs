using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CloudWindVolume : MonoBehaviour
    {
        [SerializeField] private Vector2 force = new(8f, 2f);
        [SerializeField] private float airborneMultiplier = 2.2f;

        public void Configure(Vector2 windForce, float lightObjectMultiplier = 2.2f)
        {
            force = windForce;
            airborneMultiplier = lightObjectMultiplier;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            CloudWeightState weight = body.GetComponent<CloudWeightState>();
            float multiplier = weight != null && weight.IsAirborne ? airborneMultiplier : 1f;
            body.AddForce(force * multiplier, ForceMode2D.Force);
        }
    }
}
