#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    public enum P11MapElementState
    {
        Idle = 0,
        Telegraph = 1,
        Active = 2,
        Cooldown = 3,
        Settled = 4
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P11ReturnStamp2D : MonoBehaviour
    {
        public const float TelegraphSeconds = 0.8f;
        public const float CooldownSeconds = 1.4f;

        [SerializeField] private Transform returnAnchor;
        [SerializeField] private SpriteRenderer stampVisual;
        [SerializeField] private P11MapElementState state;
        [SerializeField] private float stateSeconds;
        private readonly HashSet<Rigidbody2D> queued =
            new HashSet<Rigidbody2D>();

        public event Action<P11MapElementState> StateChanged;
        public P11MapElementState State => state;
        public float StateSeconds => stateSeconds;
        public bool DealsDamage => false;
        public bool ReturnsPlayerToSafeCell => true;

        public void Configure(
            Transform safeReturnAnchor,
            SpriteRenderer visual)
        {
            returnAnchor = safeReturnAnchor;
            stampVisual = visual;
            ResetElement();
        }

        public bool TryQueue(Rigidbody2D body)
        {
            if (body == null
                || state == P11MapElementState.Active
                || state == P11MapElementState.Cooldown)
            {
                return false;
            }

            queued.Add(body);
            if (state == P11MapElementState.Idle)
            {
                SetState(P11MapElementState.Telegraph);
            }

            return true;
        }

        public void TickForTests(float deltaSeconds)
        {
            Tick(deltaSeconds);
        }

        public void ResetElement()
        {
            queued.Clear();
            state = P11MapElementState.Idle;
            stateSeconds = 0f;
            RefreshVisual();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryQueue(other.attachedRigidbody);
        }

        private void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f
                || state == P11MapElementState.Idle)
            {
                return;
            }

            stateSeconds += deltaSeconds;
            if (state == P11MapElementState.Telegraph
                && stateSeconds >= TelegraphSeconds)
            {
                SetState(P11MapElementState.Active);
                StampQueued();
                SetState(P11MapElementState.Cooldown);
            }
            else if (state == P11MapElementState.Cooldown
                && stateSeconds >= CooldownSeconds)
            {
                SetState(P11MapElementState.Idle);
            }
        }

        private void StampQueued()
        {
            Vector2 destination = returnAnchor != null
                ? returnAnchor.position
                : transform.position;
            foreach (Rigidbody2D body in queued)
            {
                if (body == null)
                {
                    continue;
                }

                body.position = destination;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            queued.Clear();
            Physics2D.SyncTransforms();
        }

        private void SetState(P11MapElementState next)
        {
            state = next;
            stateSeconds = 0f;
            RefreshVisual();
            StateChanged?.Invoke(next);
        }

        private void RefreshVisual()
        {
            if (stampVisual == null)
            {
                return;
            }

            stampVisual.color = state
                == P11MapElementState.Telegraph
                ? new Color(1f, 0.25f, 0.25f, 0.85f)
                : new Color(0.75f, 0.22f, 0.32f, 0.45f);
        }
    }
}

#endif
