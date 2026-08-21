#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Pestle
{
    [DisallowMultipleComponent]
    public sealed class CompressibleElasticPlatform2D : PestleTargetCell2D
    {
        [SerializeField] private Collider2D elasticPlatformCollider;
        [SerializeField] private SpriteRenderer softVisual;
        [SerializeField] private SpriteRenderer compressedVisual;
        [SerializeField, Min(0f)] private float bounceVelocity = 8f;

        public event Action Compressed;
        public event Action<Rigidbody2D> Bounced;

        public bool IsCompressed { get; private set; }
        public float BounceVelocity => bounceVelocity;
        public override bool CanReceivePestle => !IsCompressed;

        protected override void Awake()
        {
            base.Awake();
            if (elasticPlatformCollider == null)
            {
                elasticPlatformCollider = GetComponent<Collider2D>();
            }

            ApplyState();
        }

        public void Configure(
            PestleInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            Collider2D platformCollider,
            SpriteRenderer targetSoftVisual = null,
            SpriteRenderer targetCompressedVisual = null,
            float launchVelocity = 8f)
        {
            ConfigureCell(registry, world, cell);
            elasticPlatformCollider = platformCollider;
            softVisual = targetSoftVisual;
            compressedVisual = targetCompressedVisual;
            bounceVelocity = Mathf.Max(0f, launchVelocity);
            IsCompressed = false;
            ApplyState();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsCompressed || collision == null)
            {
                return;
            }

            Rigidbody2D otherBody = collision.collider != null
                ? collision.collider.attachedRigidbody
                : null;
            if (otherBody == null
                || otherBody.bodyType != RigidbodyType2D.Dynamic
                || otherBody.worldCenterOfMass.y < transform.position.y
                || otherBody.linearVelocity.y > 0.1f)
            {
                return;
            }

            Vector2 velocity = otherBody.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, bounceVelocity);
            otherBody.linearVelocity = velocity;
            Bounced?.Invoke(otherBody);
        }

        public override PestleReactionKind TryReceivePestle(
            PestleStrikeContext context)
        {
            if (IsCompressed || context.StrikeCell != PestleCell)
            {
                return PestleReactionKind.None;
            }

            IsCompressed = true;
            ApplyState();
            Compressed?.Invoke();
            return PestleReactionKind.ElasticPlatformCreated;
        }

        public bool TryBounceForTests(
            Rigidbody2D body,
            bool bodyIsAbove = true)
        {
            if (!IsCompressed
                || body == null
                || body.bodyType != RigidbodyType2D.Dynamic
                || !bodyIsAbove
                || body.linearVelocity.y > 0.1f)
            {
                return false;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, bounceVelocity);
            body.linearVelocity = velocity;
            Bounced?.Invoke(body);
            return true;
        }

        public void ResetSoftForTests()
        {
            IsCompressed = false;
            ApplyState();
        }

        private void ApplyState()
        {
            if (elasticPlatformCollider != null)
            {
                elasticPlatformCollider.enabled = IsCompressed;
            }

            if (softVisual != null)
            {
                softVisual.enabled = !IsCompressed;
            }

            if (compressedVisual != null)
            {
                compressedVisual.enabled = IsCompressed;
            }
        }
    }
}

#endif
