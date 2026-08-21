#if LEGACY_DISABLED
using StarNight.Objects;
using UnityEngine;

namespace StarNight.Tools.Grapple
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GrapplePullable2D : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private WorldObjectTraits traits =
            WorldObjectTraits.Pullable;
        [SerializeField, Min(0f)] private float pullImpulse = 8f;

        public Rigidbody2D Body => body;
        public WorldObjectTraits Traits => traits;
        public bool IsPullable => (traits & WorldObjectTraits.Pullable) != 0;

        public void Configure(
            Rigidbody2D targetBody,
            float configuredPullImpulse = 8f,
            WorldObjectTraits configuredTraits = WorldObjectTraits.Pullable)
        {
            body = targetBody != null ? targetBody : GetComponent<Rigidbody2D>();
            pullImpulse = Mathf.Max(0f, configuredPullImpulse);
            traits = configuredTraits;
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        public bool PullToward(Vector2 destination)
        {
            if (!IsPullable || body == null || body.bodyType != RigidbodyType2D.Dynamic)
            {
                return false;
            }

            Vector2 delta = destination - body.worldCenterOfMass;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            body.AddForce(delta.normalized * pullImpulse, ForceMode2D.Impulse);
            return true;
        }
    }
}

#endif
