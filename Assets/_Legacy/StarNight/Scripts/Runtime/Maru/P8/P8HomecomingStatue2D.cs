#if LEGACY_DISABLED
using System;
using StarNight.Explosions;
using StarNight.Objects;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class P8HomecomingStatue2D :
        MonoBehaviour,
        IExplosionReceiver2D
    {
        public static readonly Vector2Int FootprintCells =
            new Vector2Int(1, 2);
        public const int BaseCellCount = 1;

        [SerializeField] private P8MaruTimeline2D timeline;
        [SerializeField] private P8StarTear2D starTear;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D statueCollider;
        [SerializeField] private GameObject intactVisual;
        [SerializeField] private GameObject crackedVisual;
        [SerializeField] private GameObject destroyedVisual;
        [SerializeField] private SpriteRenderer starTearGlow;
        [SerializeField, Min(0.1f)] private float collisionImpactThreshold =
            2.5f;
        [SerializeField] private bool requireTwoStagesForChainImpact;
        [SerializeField] private P8StatueState state =
            P8StatueState.Intact;

        public event Action<P8StatueState> StateChanged;
        public event Action<P8StatueImpactKind> Destroyed;
        public event Action<P8StarTear2D> StarTearDropped;

        public P8MaruTimeline2D Timeline => timeline;
        public P8StarTear2D StarTear => starTear;
        public P8StatueState State => state;
        public P8StatueImpactKind LastImpactKind { get; private set; }
        public P8StatueDestructionResult LastAcceleration { get; private set; }
        public WorldObjectTraits Traits =>
            WorldObjectTraits.Heavy
            | WorldObjectTraits.Breakable
            | WorldObjectTraits.Pullable;
        public bool CanBeCarried => false;
        public bool IsDestroyed => state == P8StatueState.Destroyed;
        public bool RequireTwoStagesForChainImpact =>
            requireTwoStagesForChainImpact;

        public void Configure(
            P8MaruTimeline2D targetTimeline,
            P8StarTear2D containedStarTear,
            Rigidbody2D targetBody = null,
            Collider2D targetCollider = null,
            GameObject intact = null,
            GameObject cracked = null,
            GameObject broken = null,
            SpriteRenderer tearGlow = null,
            bool requireTwoStagesForChain = false)
        {
            requireTwoStagesForChainImpact = requireTwoStagesForChain;
            timeline = targetTimeline;
            starTear = containedStarTear;
            body = targetBody != null
                ? targetBody
                : GetComponent<Rigidbody2D>();
            statueCollider = targetCollider != null
                ? targetCollider
                : GetComponent<Collider2D>();
            intactVisual = intact;
            crackedVisual = cracked;
            destroyedVisual = broken;
            starTearGlow = tearGlow;
            ConfigurePhysics();
            ResetStatueForTests();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (statueCollider == null)
            {
                statueCollider = GetComponent<Collider2D>();
            }

            ConfigurePhysics();
            ApplyVisualState();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsDestroyed
                || collision == null
                || collision.relativeVelocity.magnitude
                    < collisionImpactThreshold)
            {
                return;
            }

            P8StatueImpactKind kind =
                collision.collider.GetComponentInParent<Bomb2D>() != null
                    ? P8StatueImpactKind.Bomb
                    : collision.collider.GetComponentInParent<
                        FallingObject2D>() != null
                        ? P8StatueImpactKind.FallingObject
                        : P8StatueImpactKind.Collision;
            ApplyImpact(kind);
        }

        public bool ApplyImpact(P8StatueImpactKind impactKind)
        {
            if (IsDestroyed)
            {
                return false;
            }

            LastImpactKind = impactKind;
            bool chainBlocked = requireTwoStagesForChainImpact
                && state == P8StatueState.Intact
                && IsChainImpact(impactKind);
            if (!chainBlocked
                && (impactKind == P8StatueImpactKind.Bomb
                    || state == P8StatueState.Cracked))
            {
                DestroyStatue(impactKind);
                return true;
            }

            state = P8StatueState.Cracked;
            timeline?.RingStatueCrack();
            ApplyVisualState();
            StateChanged?.Invoke(state);
            return true;
        }

        public void SetRequireTwoStagesForChainImpact(bool required)
        {
            requireTwoStagesForChainImpact = required;
        }

        public void ReceiveExplosion(ExplosionHit2D hit)
        {
            ApplyImpact(P8StatueImpactKind.Bomb);
        }

        public void ResetStatueForTests()
        {
            state = P8StatueState.Intact;
            LastImpactKind = default;
            LastAcceleration = default;
            if (statueCollider != null)
            {
                statueCollider.enabled = true;
            }

            if (starTear != null)
            {
                starTear.gameObject.SetActive(false);
            }

            ApplyVisualState();
        }

        private void DestroyStatue(P8StatueImpactKind impactKind)
        {
            state = P8StatueState.Destroyed;
            LastImpactKind = impactKind;
            LastAcceleration = timeline != null
                ? timeline.ApplyStatueDestroyed(impactKind)
                : default;
            if (statueCollider != null)
            {
                statueCollider.enabled = false;
            }

            if (starTear != null)
            {
                starTear.transform.SetParent(transform.parent, true);
                starTear.transform.position =
                    transform.position + new Vector3(0f, 1.15f, -0.1f);
                starTear.gameObject.SetActive(true);
                StarTearDropped?.Invoke(starTear);
            }

            ApplyVisualState();
            StateChanged?.Invoke(state);
            Destroyed?.Invoke(impactKind);
        }

        private static bool IsChainImpact(P8StatueImpactKind impactKind)
        {
            return impactKind == P8StatueImpactKind.Bomb;
        }

        private void ConfigurePhysics()
        {
            if (body == null)
            {
                return;
            }

            body.mass = 8f;
            body.freezeRotation = true;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void ApplyVisualState()
        {
            SetActive(intactVisual, state == P8StatueState.Intact);
            SetActive(crackedVisual, state == P8StatueState.Cracked);
            SetActive(
                destroyedVisual,
                state == P8StatueState.Destroyed);
            if (starTearGlow != null)
            {
                Color color = starTearGlow.color;
                color.a = state == P8StatueState.Cracked ? 1f : 0.55f;
                starTearGlow.color = color;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}

#endif
