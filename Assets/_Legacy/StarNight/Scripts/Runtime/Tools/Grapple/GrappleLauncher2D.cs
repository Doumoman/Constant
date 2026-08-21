#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools.Grapple
{
    public interface IGrappleBossHook
    {
        bool OnGrappled(GrappleLauncher2D launcher, Vector2 hitPoint);
    }

    public readonly struct GrappleFireResult
    {
        public GrappleFireResult(
            bool fired,
            GrappleTargetKind targetKind,
            Vector2 direction,
            Vector2 hitPoint)
        {
            Fired = fired;
            TargetKind = targetKind;
            Direction = direction;
            HitPoint = hitPoint;
        }

        public bool Fired { get; }
        public GrappleTargetKind TargetKind { get; }
        public Vector2 Direction { get; }
        public Vector2 HitPoint { get; }

        public static GrappleFireResult Miss(Vector2 direction) =>
            new GrappleFireResult(false, GrappleTargetKind.None, direction, default);
    }

    [DefaultExecutionOrder(-80)]
    [DisallowMultipleComponent]
    public sealed class GrappleLauncher2D : MonoBehaviour
    {
        public const float MaximumRangeCells = 8f;
        public const float DefaultCooldownSeconds = 0.45f;

        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private Transform muzzle;
        [SerializeField] private LayerMask queryMask = ~0;
        [SerializeField] private LayerMask fixedTerrainMask;
        [SerializeField, Min(1f)] private float travelSpeed = 18f;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.18f;
        [SerializeField, Min(0.01f)] private float maximumTravelSeconds = 0.65f;

        private readonly List<GrappleTargetCandidate> candidates =
            new List<GrappleTargetCandidate>(16);
        private Vector2 travelTarget;
        private float travelRemaining;

        public bool IsTravelling { get; private set; }
        public float CooldownRemaining { get; private set; }
        public GrappleFireResult LastResult { get; private set; }

        public void Configure(
            Rigidbody2D targetBody,
            Collider2D targetCollider,
            Transform targetMuzzle,
            LayerMask targetQueryMask,
            LayerMask targetFixedTerrainMask)
        {
            playerBody = targetBody;
            playerCollider = targetCollider;
            muzzle = targetMuzzle;
            queryMask = targetQueryMask;
            fixedTerrainMask = targetFixedTerrainMask;
        }

        private void Awake()
        {
            if (playerBody == null)
            {
                playerBody = GetComponentInParent<Rigidbody2D>();
            }

            if (playerCollider == null && playerBody != null)
            {
                playerCollider = playerBody.GetComponent<Collider2D>();
            }

            if (muzzle == null)
            {
                muzzle = transform;
            }
        }

        private void FixedUpdate()
        {
            TickCooldownForTests(Time.fixedDeltaTime);
            StepTravelForTests(Time.fixedDeltaTime);
        }

        public GrappleFireResult TryUse(Vector2 rawAim)
        {
            Vector2 direction = GrappleAim8.Quantize(rawAim);
            if (CooldownRemaining > 0f || IsTravelling)
            {
                LastResult = GrappleFireResult.Miss(direction);
                return LastResult;
            }

            CooldownRemaining = DefaultCooldownSeconds;
            Vector2 origin = muzzle != null
                ? (Vector2)muzzle.position
                : playerBody != null ? playerBody.position : (Vector2)transform.position;
            BuildCandidates(origin, direction);
            if (!GrappleTargetSelector.TrySelect(
                    candidates,
                    MaximumRangeCells,
                    out GrappleTargetCandidate selected))
            {
                LastResult = GrappleFireResult.Miss(direction);
                return LastResult;
            }

            bool handled = HandleSelectedTarget(selected);
            LastResult = new GrappleFireResult(
                handled,
                handled ? selected.Kind : GrappleTargetKind.None,
                direction,
                selected.Point);
            return LastResult;
        }

        public void CancelTravel()
        {
            IsTravelling = false;
            travelRemaining = 0f;
        }

        public void TickCooldownForTests(float deltaTime)
        {
            CooldownRemaining = Mathf.Max(
                0f,
                CooldownRemaining - Mathf.Max(0f, deltaTime));
        }

        public void StepTravelForTests(float deltaTime)
        {
            if (!IsTravelling || playerBody == null)
            {
                return;
            }

            float step = Mathf.Max(0f, deltaTime);
            travelRemaining = Mathf.Max(0f, travelRemaining - step);
            Vector2 delta = travelTarget - playerBody.position;
            if (delta.magnitude <= arrivalDistance || travelRemaining <= 0f)
            {
                playerBody.linearVelocity = Vector2.zero;
                CancelTravel();
                return;
            }

            playerBody.linearVelocity = delta.normalized * travelSpeed;
        }

        private void BuildCandidates(Vector2 origin, Vector2 direction)
        {
            candidates.Clear();
            RaycastHit2D[] hits = Physics2D.RaycastAll(
                origin,
                direction,
                MaximumRangeCells,
                queryMask);
            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit2D hit = hits[index];
                Collider2D collider = hit.collider;
                if (collider == null
                    || IsOwnedByPlayerOrTool(collider))
                {
                    continue;
                }

                GrappleTargetKind kind = Classify(collider);
                if (kind == GrappleTargetKind.None)
                {
                    continue;
                }

                candidates.Add(new GrappleTargetCandidate(
                    kind,
                    hit.distance,
                    collider.GetInstanceID(),
                    hit.point,
                    collider));
            }
        }

        private bool IsOwnedByPlayerOrTool(Collider2D collider)
        {
            if (collider == playerCollider
                || collider.transform == transform
                || collider.transform.IsChildOf(transform))
            {
                return true;
            }

            if (playerBody == null)
            {
                return false;
            }

            Transform playerTransform = playerBody.transform;
            return collider.attachedRigidbody == playerBody
                || collider.transform == playerTransform
                || collider.transform.IsChildOf(playerTransform);
        }

        private GrappleTargetKind Classify(Collider2D collider)
        {
            if (FindBossHook(collider) != null)
            {
                return GrappleTargetKind.BossHook;
            }

            GrapplePullable2D pullable =
                collider.GetComponentInParent<GrapplePullable2D>();
            if (pullable != null && pullable.IsPullable)
            {
                return GrappleTargetKind.Pullable;
            }

            int mask = 1 << collider.gameObject.layer;
            Rigidbody2D attachedBody = collider.attachedRigidbody;
            bool isFixedGeometry = !collider.isTrigger
                && (attachedBody == null
                    || attachedBody.bodyType != RigidbodyType2D.Dynamic);
            return isFixedGeometry
                && (fixedTerrainMask.value & mask) != 0
                ? GrappleTargetKind.FixedTerrain
                : GrappleTargetKind.None;
        }

        private bool HandleSelectedTarget(GrappleTargetCandidate selected)
        {
            switch (selected.Kind)
            {
                case GrappleTargetKind.FixedTerrain:
                    if (playerBody == null)
                    {
                        return false;
                    }

                    travelTarget = selected.Point;
                    travelRemaining = maximumTravelSeconds;
                    IsTravelling = true;
                    return true;

                case GrappleTargetKind.Pullable:
                    GrapplePullable2D pullable =
                        selected.Collider.GetComponentInParent<GrapplePullable2D>();
                    return pullable != null
                        && pullable.PullToward(
                            playerBody != null
                                ? playerBody.position
                                : (Vector2)transform.position);

                case GrappleTargetKind.BossHook:
                    IGrappleBossHook bossHook = FindBossHook(selected.Collider);
                    return bossHook != null
                        && bossHook.OnGrappled(this, selected.Point);

                default:
                    return false;
            }
        }

        private static IGrappleBossHook FindBossHook(Collider2D collider)
        {
            MonoBehaviour[] behaviours =
                collider.GetComponentsInParent<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IGrappleBossHook hook)
                {
                    return hook;
                }
            }

            return null;
        }
    }
}

#endif
