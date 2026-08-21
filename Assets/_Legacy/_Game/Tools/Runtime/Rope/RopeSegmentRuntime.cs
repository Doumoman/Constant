#if LEGACY_DISABLED
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeSegmentRuntime : MonoBehaviour, IToolReactionReceiver
    {
        [SerializeField] private int segmentIndex;
        [SerializeField] private Vector2Int cell;
        [SerializeField] private bool attached = true;

        private RopeInstallationRuntime installation;
        private int lastActionId;
        private float fallingRemaining;

        public int SegmentIndex => segmentIndex;
        public Vector2Int Cell => cell;
        public bool IsAttached => attached;

        private void Update()
        {
            if (fallingRemaining <= 0f) return;
            fallingRemaining -= Time.deltaTime;
            if (fallingRemaining <= 0f) Destroy(gameObject);
        }

        public void Configure(RopeInstallationRuntime owner, int index, Vector2Int configuredCell)
        {
            installation = owner;
            segmentIndex = index;
            cell = configuredCell;
            attached = true;
            fallingRemaining = 0f;
        }

        public ToolReactionResult TryReact(ToolReactionContext context)
        {
            if (context.ActionId == lastActionId)
            {
                return ToolReactionResult.Rejected(FeedbackId.DuplicateAction);
            }
            lastActionId = context.ActionId;
            if (!attached
                || (context.Tags & (ToolTag.Bomb | ToolTag.Fire | ToolTag.Cut)) == 0
                || installation == null)
            {
                return ToolReactionResult.Rejected(FeedbackId.None);
            }

            bool changed = installation.BreakAt(segmentIndex);
            return new ToolReactionResult
            {
                Accepted = changed,
                ChangedState = changed,
                ConsumeToolResource = changed,
                Feedback = changed ? FeedbackId.Break : FeedbackId.None,
            };
        }

        public void BreakImmediately()
        {
            attached = false;
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            for (int index = 0; index < colliders.Length; index++) colliders[index].enabled = false;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++) renderers[index].enabled = false;
        }

        public void BeginFalling(float seconds)
        {
            attached = false;
            fallingRemaining = Mathf.Max(0.01f, seconds);
            transform.SetParent(null, true);
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 2f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            for (int index = 0; index < colliders.Length; index++) colliders[index].isTrigger = true;
        }

        public void RestoreAttached(RopeInstallationRuntime owner)
        {
            installation = owner;
            attached = true;
            fallingRemaining = 0f;
            transform.SetParent(owner.transform, true);
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.simulated = false;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            for (int index = 0; index < colliders.Length; index++)
            {
                colliders[index].enabled = true;
                colliders[index].isTrigger = true;
            }
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++) renderers[index].enabled = true;
        }
    }
}

#endif
