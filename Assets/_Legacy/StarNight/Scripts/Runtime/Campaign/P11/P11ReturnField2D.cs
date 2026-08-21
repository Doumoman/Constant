#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P11ReturnField2D : MonoBehaviour
    {
        public const float TelegraphSeconds = 1f;

        [SerializeField, Min(1f)] private float intervalSeconds = 5f;
        [SerializeField] private SpriteRenderer rewindVisual;
        private readonly Dictionary<Rigidbody2D, Vector2> origins =
            new Dictionary<Rigidbody2D, Vector2>();
        private readonly Dictionary<Rigidbody2D, int> contacts =
            new Dictionary<Rigidbody2D, int>();
        private readonly List<Rigidbody2D> staleBodies =
            new List<Rigidbody2D>();
        private float elapsed;
        private int pulseCount;
        private int lastPulseBodyCount;

        public float IntervalSeconds => intervalSeconds;
        public float ElapsedSeconds => elapsed;
        public int RegisteredBodyCount => origins.Count;
        public int PulseCount => pulseCount;
        public int LastPulseBodyCount => lastPulseBodyCount;
        public bool TelegraphActive => rewindVisual != null
            && rewindVisual.enabled;
        public bool PlayerTakesDamage => false;
        public bool MaruUnaffected => true;

        public void Configure(
            float interval,
            SpriteRenderer visual)
        {
            intervalSeconds = Mathf.Max(1f, interval);
            rewindVisual = visual;
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }

            elapsed = 0f;
            pulseCount = 0;
            lastPulseBodyCount = 0;
            ClearRegistrations();
        }

        public bool Register(Rigidbody2D body)
        {
            if (!CanRegister(body) || origins.ContainsKey(body))
            {
                return false;
            }

            origins.Add(body, body.position);
            return true;
        }

        public bool Unregister(Rigidbody2D body)
        {
            if (body == null || !origins.Remove(body))
            {
                return false;
            }

            contacts.Remove(body);
            return true;
        }

        public bool HandleTriggerEnter(Collider2D other)
        {
            Rigidbody2D body = other != null
                ? other.attachedRigidbody
                : null;
            if (!CanRegister(body))
            {
                return false;
            }

            if (!origins.ContainsKey(body))
            {
                origins.Add(body, body.position);
            }

            contacts.TryGetValue(body, out int count);
            contacts[body] = count + 1;
            return count == 0;
        }

        public bool HandleTriggerExit(Collider2D other)
        {
            Rigidbody2D body = other != null
                ? other.attachedRigidbody
                : null;
            if (body == null || !origins.ContainsKey(body))
            {
                return false;
            }

            if (contacts.TryGetValue(body, out int count)
                && count > 1)
            {
                contacts[body] = count - 1;
                return false;
            }

            return Unregister(body);
        }

        public int PulseNow()
        {
            staleBodies.Clear();
            int movedBodies = 0;
            foreach (KeyValuePair<Rigidbody2D, Vector2> pair
                in origins)
            {
                if (pair.Key == null)
                {
                    staleBodies.Add(pair.Key);
                    continue;
                }

                pair.Key.position = pair.Value;
                pair.Key.linearVelocity = Vector2.zero;
                pair.Key.angularVelocity = 0f;
                pair.Key.WakeUp();
                movedBodies++;
            }

            for (int index = 0; index < staleBodies.Count; index++)
            {
                origins.Remove(staleBodies[index]);
                contacts.Remove(staleBodies[index]);
            }

            elapsed = 0f;
            pulseCount++;
            lastPulseBodyCount = movedBodies;
            Physics2D.SyncTransforms();
            return movedBodies;
        }

        public void ClearRegistrations()
        {
            origins.Clear();
            contacts.Clear();
        }

        public bool TryGetOrigin(
            Rigidbody2D body,
            out Vector2 origin)
        {
            if (body != null && origins.TryGetValue(body, out origin))
            {
                return true;
            }

            origin = default;
            return false;
        }

        public void TickForTests(float deltaSeconds)
        {
            Tick(deltaSeconds);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTriggerEnter(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            HandleTriggerExit(other);
        }

        private void OnDisable()
        {
            ClearRegistrations();
            elapsed = 0f;
            if (rewindVisual != null)
            {
                rewindVisual.enabled = false;
            }
        }

        private void Tick(float deltaSeconds)
        {
            elapsed += Mathf.Max(0f, deltaSeconds);
            if (rewindVisual != null)
            {
                rewindVisual.enabled =
                    elapsed >= intervalSeconds - TelegraphSeconds;
            }

            if (elapsed >= intervalSeconds)
            {
                PulseNow();
            }
        }

        private static bool CanRegister(Rigidbody2D body)
        {
            if (body == null
                || body.bodyType == RigidbodyType2D.Static)
            {
                return false;
            }

            P11InvariantStarBlock2D invariant =
                body.GetComponent<P11InvariantStarBlock2D>();
            return invariant == null || !invariant.ReturnFieldInvariant;
        }
    }
}

#endif
