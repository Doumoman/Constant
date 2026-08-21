#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Tools
{
    [DefaultExecutionOrder(80)]
    [DisallowMultipleComponent]
    public sealed class P3ToolHud2D : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private PlayerConsumableTools2D consumables;
        [SerializeField] private PlayerToolInventory2D inventory;
        [SerializeField] private SpriteRenderer ropeIcon;
        [SerializeField] private SpriteRenderer bombIcon;
        [SerializeField] private SpriteRenderer heldToolIcon;
        [SerializeField] private SpriteRenderer[] ropeDots =
            new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer[] bombDots =
            new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer[] heldToolDots =
            new SpriteRenderer[0];
        [SerializeField] private Vector2 viewportAnchor =
            new Vector2(0.045f, 0.93f);

        private HandToolPickup2D observedHeldTool;

        public int VisibleRopeDots => CountVisible(ropeDots);
        public int VisibleBombDots => CountVisible(bombDots);
        public int VisibleHeldToolDots => CountVisible(heldToolDots);
        public bool IsHeldToolIconVisible =>
            heldToolIcon != null && heldToolIcon.enabled;

        public void Configure(
            Camera camera,
            PlayerConsumableTools2D targetConsumables,
            PlayerToolInventory2D targetInventory,
            SpriteRenderer targetRopeIcon,
            SpriteRenderer targetBombIcon,
            SpriteRenderer targetHeldToolIcon,
            SpriteRenderer[] targetRopeDots,
            SpriteRenderer[] targetBombDots,
            SpriteRenderer[] targetHeldToolDots)
        {
            Unsubscribe();
            targetCamera = camera;
            consumables = targetConsumables;
            inventory = targetInventory;
            ropeIcon = targetRopeIcon;
            bombIcon = targetBombIcon;
            heldToolIcon = targetHeldToolIcon;
            ropeDots = targetRopeDots ?? new SpriteRenderer[0];
            bombDots = targetBombDots ?? new SpriteRenderer[0];
            heldToolDots =
                targetHeldToolDots ?? new SpriteRenderer[0];
            Subscribe();
            RefreshAll();
            UpdateAnchor();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            UpdateAnchor();
        }

        private void Subscribe()
        {
            if (consumables != null)
            {
                consumables.RopeStockChanged -= HandleRopeStockChanged;
                consumables.RopeStockChanged += HandleRopeStockChanged;
                consumables.BombStockChanged -= HandleBombStockChanged;
                consumables.BombStockChanged += HandleBombStockChanged;
            }

            if (inventory != null)
            {
                inventory.HeldToolChanged -= HandleHeldToolChanged;
                inventory.HeldToolChanged += HandleHeldToolChanged;
                ObserveHeldTool(inventory.HeldTool);
            }
        }

        private void Unsubscribe()
        {
            if (consumables != null)
            {
                consumables.RopeStockChanged -= HandleRopeStockChanged;
                consumables.BombStockChanged -= HandleBombStockChanged;
            }

            if (inventory != null)
            {
                inventory.HeldToolChanged -= HandleHeldToolChanged;
            }

            ObserveHeldTool(null);
        }

        private void HandleRopeStockChanged(int stock)
        {
            SetVisibleCount(ropeDots, stock);
        }

        private void HandleBombStockChanged(int stock)
        {
            SetVisibleCount(bombDots, stock);
        }

        private void HandleHeldToolChanged(HandToolPickup2D pickup)
        {
            ObserveHeldTool(pickup);
            RefreshHeldTool();
        }

        private void HandleHeldToolUsesChanged(
            HandToolPickup2D pickup,
            int remainingUses)
        {
            if (pickup == observedHeldTool)
            {
                RefreshHeldTool();
            }
        }

        private void ObserveHeldTool(HandToolPickup2D pickup)
        {
            if (observedHeldTool != null)
            {
                observedHeldTool.UsesChanged -=
                    HandleHeldToolUsesChanged;
            }

            observedHeldTool = pickup;
            if (observedHeldTool != null)
            {
                observedHeldTool.UsesChanged +=
                    HandleHeldToolUsesChanged;
            }
        }

        private void RefreshAll()
        {
            SetVisibleCount(
                ropeDots,
                consumables != null ? consumables.RopeStock : 0);
            SetVisibleCount(
                bombDots,
                consumables != null ? consumables.BombStock : 0);
            ObserveHeldTool(inventory != null ? inventory.HeldTool : null);
            RefreshHeldTool();
            if (ropeIcon != null)
            {
                ropeIcon.enabled = true;
            }

            if (bombIcon != null)
            {
                bombIcon.enabled = true;
            }
        }

        private void RefreshHeldTool()
        {
            bool hasHeldTool =
                observedHeldTool != null && observedHeldTool.IsHeld;
            if (heldToolIcon != null)
            {
                heldToolIcon.enabled = hasHeldTool;
                if (hasHeldTool
                    && observedHeldTool.ToolRenderer != null)
                {
                    heldToolIcon.sprite =
                        observedHeldTool.ToolRenderer.sprite;
                    heldToolIcon.color =
                        observedHeldTool.ToolRenderer.color;
                }
            }

            int visibleUses = hasHeldTool
                && observedHeldTool.HasFiniteUses
                    ? observedHeldTool.RemainingUses
                    : 0;
            SetVisibleCount(heldToolDots, visibleUses);
        }

        private void UpdateAnchor()
        {
            if (targetCamera == null)
            {
                return;
            }

            float distance = Mathf.Abs(
                targetCamera.transform.position.z
                - transform.position.z);
            Vector3 anchored = targetCamera.ViewportToWorldPoint(
                new Vector3(
                    viewportAnchor.x,
                    viewportAnchor.y,
                    distance));
            transform.position =
                new Vector3(anchored.x, anchored.y, 0f);
        }

        private static void SetVisibleCount(
            SpriteRenderer[] dots,
            int count)
        {
            if (dots == null)
            {
                return;
            }

            int clamped = Mathf.Clamp(count, 0, dots.Length);
            for (int index = 0; index < dots.Length; index++)
            {
                if (dots[index] != null)
                {
                    dots[index].enabled = index < clamped;
                }
            }
        }

        private static int CountVisible(SpriteRenderer[] dots)
        {
            if (dots == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < dots.Length; index++)
            {
                if (dots[index] != null && dots[index].enabled)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

#endif
