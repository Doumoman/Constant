#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P11GravityDial2D : P5ContextInteractable2D
    {
        private sealed class BodyState
        {
            public BodyState(float scale)
            {
                OriginalGravityScale = scale;
                ContactCount = 1;
            }

            public float OriginalGravityScale { get; }
            public int ContactCount { get; set; }
        }

        [SerializeField] private Vector2 gravityDirection =
            Vector2.down;
        [SerializeField, Min(0.1f)] private float acceleration =
            9.81f;
        [SerializeField] private Transform dialVisual;
        [SerializeField] private int rotationCount;
        [SerializeField] private int lastAppliedBodyCount;
        private readonly Dictionary<Rigidbody2D, BodyState> bodies =
            new Dictionary<Rigidbody2D, BodyState>();
        private readonly List<Rigidbody2D> staleBodies =
            new List<Rigidbody2D>();

        public Vector2 GravityDirection => gravityDirection;
        public float Acceleration => acceleration;
        public int RegisteredBodyCount => bodies.Count;
        public int RotationCount => rotationCount;
        public int LastAppliedBodyCount => lastAppliedBodyCount;
        public bool HasAffectedBody => lastAppliedBodyCount > 0;

        public void Configure(
            Vector2 initialDirection,
            float gravityAcceleration,
            Transform visual = null,
            float interactionRadius = 2f)
        {
            ClearBodies();
            gravityDirection = NormalizeCardinal(initialDirection);
            acceleration = Mathf.Max(0.1f, gravityAcceleration);
            dialVisual = visual;
            rotationCount = 0;
            lastAppliedBodyCount = 0;
            Collider2D volume = GetComponent<Collider2D>();
            if (volume != null)
            {
                volume.isTrigger = true;
            }

            ConfigureInteraction(
                transform,
                interactionRadius,
                68);
            RefreshVisual();
        }

        public bool RotateClockwise()
        {
            return SetDirection(new Vector2(
                gravityDirection.y,
                -gravityDirection.x));
        }

        public bool RotateCounterClockwise()
        {
            return SetDirection(new Vector2(
                -gravityDirection.y,
                gravityDirection.x));
        }

        public bool SetDirection(Vector2 direction)
        {
            Vector2 next = NormalizeCardinal(direction);
            if (next == gravityDirection)
            {
                return false;
            }

            gravityDirection = next;
            rotationCount++;
            RefreshVisual();
            return true;
        }

        public bool Register(Rigidbody2D body)
        {
            if (!CanAffect(body))
            {
                return false;
            }

            if (bodies.TryGetValue(body, out BodyState existing))
            {
                existing.ContactCount++;
                return false;
            }

            bodies.Add(body, new BodyState(body.gravityScale));
            body.gravityScale = 0f;
            return true;
        }

        public bool Unregister(Rigidbody2D body, bool force = false)
        {
            if (body == null
                || !bodies.TryGetValue(body, out BodyState state))
            {
                return false;
            }

            if (!force && state.ContactCount > 1)
            {
                state.ContactCount--;
                return false;
            }

            body.gravityScale = state.OriginalGravityScale;
            bodies.Remove(body);
            return true;
        }

        public bool HandleTriggerEnter(Collider2D other)
        {
            return Register(other != null
                ? other.attachedRigidbody
                : null);
        }

        public bool HandleTriggerExit(Collider2D other)
        {
            return Unregister(other != null
                ? other.attachedRigidbody
                : null);
        }

        public int ApplyGravityStep(float deltaSeconds)
        {
            staleBodies.Clear();
            int affected = 0;
            float delta = Mathf.Max(0f, deltaSeconds);
            foreach (KeyValuePair<Rigidbody2D, BodyState> pair
                in bodies)
            {
                if (pair.Key == null)
                {
                    staleBodies.Add(pair.Key);
                    continue;
                }

                pair.Key.linearVelocity +=
                    gravityDirection * acceleration * delta;
                affected++;
            }

            for (int index = 0; index < staleBodies.Count; index++)
            {
                bodies.Remove(staleBodies[index]);
            }

            lastAppliedBodyCount = affected;
            return affected;
        }

        public void ClearBodies()
        {
            foreach (KeyValuePair<Rigidbody2D, BodyState> pair
                in bodies)
            {
                if (pair.Key != null)
                {
                    pair.Key.gravityScale =
                        pair.Value.OriginalGravityScale;
                }
            }

            bodies.Clear();
            lastAppliedBodyCount = 0;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return isActiveAndEnabled;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return RotateClockwise();
        }

        protected override void OnDisable()
        {
            ClearBodies();
            base.OnDisable();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTriggerEnter(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            HandleTriggerExit(other);
        }

        private void FixedUpdate()
        {
            ApplyGravityStep(Time.fixedDeltaTime);
        }

        private void RefreshVisual()
        {
            if (dialVisual == null)
            {
                return;
            }

            float angle = Mathf.Atan2(
                gravityDirection.y,
                gravityDirection.x) * Mathf.Rad2Deg + 90f;
            dialVisual.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static bool CanAffect(Rigidbody2D body)
        {
            return body != null
                && body.bodyType != RigidbodyType2D.Static
                && body.GetComponent<P11InvariantStarBlock2D>()
                    == null;
        }

        private static Vector2 NormalizeCardinal(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                return Vector2.down;
            }

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x >= 0f
                    ? Vector2.right
                    : Vector2.left;
            }

            return direction.y >= 0f
                ? Vector2.up
                : Vector2.down;
        }
    }
}

#endif
