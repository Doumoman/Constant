#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Player;
using UnityEngine;

namespace StarNight.Objects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PressurePlate2D : MonoBehaviour
    {
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color releasedColor = new Color(0.72f, 0.55f, 0.24f, 1f);
        [SerializeField] private Color pressedColor = new Color(1f, 0.82f, 0.30f, 1f);

        private readonly Dictionary<Component, HashSet<Collider2D>> contacts =
            new Dictionary<Component, HashSet<Collider2D>>();
        private readonly HashSet<Component> testPressers = new HashSet<Component>();
        private readonly List<Component> stalePressers = new List<Component>();

        public event Action<bool> PressedChanged;
        public event Action Pressed;
        public event Action Released;

        public bool IsPressed { get; private set; }
        public int PresserCount
        {
            get
            {
                int count = contacts.Count;
                foreach (Component testPresser in testPressers)
                {
                    if (!contacts.ContainsKey(testPresser))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Configure(Collider2D targetTrigger, SpriteRenderer targetVisual = null)
        {
            triggerCollider = targetTrigger;
            visual = targetVisual;
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            UpdateVisual();
        }

        private void Awake()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            triggerCollider.isTrigger = true;
            UpdateVisual();
        }

        private void FixedUpdate()
        {
            CleanupInactivePressers();
        }

        private void OnDisable()
        {
            contacts.Clear();
            testPressers.Clear();
            SetPressed(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            RegisterContact(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            RegisterContact(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Component presser = ResolvePresser(other);
            if (presser == null
                || !contacts.TryGetValue(
                    presser,
                    out HashSet<Collider2D> colliders))
            {
                return;
            }

            colliders.Remove(other);
            if (colliders.Count == 0)
            {
                contacts.Remove(presser);
            }

            RefreshPressedState();
        }

        public bool RegisterPresserForTests(Component presser)
        {
            if (!IsRecognizedPresser(presser))
            {
                return false;
            }

            testPressers.Add(presser);
            RefreshPressedState();
            return true;
        }

        public bool UnregisterPresserForTests(Component presser)
        {
            if (presser == null || !testPressers.Remove(presser))
            {
                return false;
            }

            RefreshPressedState();
            return true;
        }

        private void RegisterContact(Collider2D other)
        {
            Component presser = ResolvePresser(other);
            if (presser == null)
            {
                return;
            }

            if (!contacts.TryGetValue(
                    presser,
                    out HashSet<Collider2D> colliders))
            {
                colliders = new HashSet<Collider2D>();
                contacts.Add(presser, colliders);
            }

            colliders.Add(other);
            RefreshPressedState();
        }

        private Component ResolvePresser(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            PlayerMotor2D player = other.GetComponentInParent<PlayerMotor2D>();
            if (player != null)
            {
                return player;
            }

            CarryableObject2D carryable = other.GetComponentInParent<CarryableObject2D>();
            if (carryable != null)
            {
                return carryable;
            }

            return other.GetComponentInParent<FallingObject2D>();
        }

        private static bool IsRecognizedPresser(Component presser)
        {
            return presser is PlayerMotor2D
                || presser is CarryableObject2D
                || presser is FallingObject2D;
        }

        private void CleanupInactivePressers()
        {
            stalePressers.Clear();
            foreach (KeyValuePair<Component, HashSet<Collider2D>> entry in contacts)
            {
                Component presser = entry.Key;
                if (presser == null
                    || !presser.gameObject.activeInHierarchy
                    || !IsSimulated(presser))
                {
                    stalePressers.Add(presser);
                    continue;
                }

                entry.Value.RemoveWhere(collider =>
                    collider == null
                    || !collider.enabled
                    || !collider.gameObject.activeInHierarchy
                    || (collider.attachedRigidbody != null
                        && !collider.attachedRigidbody.simulated));
                if (entry.Value.Count == 0)
                {
                    stalePressers.Add(presser);
                }
            }

            for (int index = 0; index < stalePressers.Count; index++)
            {
                contacts.Remove(stalePressers[index]);
            }

            testPressers.RemoveWhere(presser =>
                presser == null || !presser.gameObject.activeInHierarchy);

            if (stalePressers.Count > 0 || IsPressed != (PresserCount > 0))
            {
                RefreshPressedState();
            }
        }

        private static bool IsSimulated(Component presser)
        {
            Rigidbody2D attachedBody = presser.GetComponent<Rigidbody2D>();
            return attachedBody == null || attachedBody.simulated;
        }

        private void RefreshPressedState()
        {
            SetPressed(PresserCount > 0);
        }

        private void SetPressed(bool pressed)
        {
            if (IsPressed == pressed)
            {
                UpdateVisual();
                return;
            }

            IsPressed = pressed;
            UpdateVisual();
            PressedChanged?.Invoke(IsPressed);
            if (IsPressed)
            {
                Pressed?.Invoke();
            }
            else
            {
                Released?.Invoke();
            }
        }

        private void UpdateVisual()
        {
            if (visual != null)
            {
                visual.color = IsPressed ? pressedColor : releasedColor;
            }
        }
    }
}

#endif
