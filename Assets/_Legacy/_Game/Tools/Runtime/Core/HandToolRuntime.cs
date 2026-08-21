#if LEGACY_DISABLED
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Reactions;
using StarNight.Interaction.State;
using UnityEngine;

namespace StarNight.Tools.Core
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public abstract class HandToolRuntime : HandSlotItemRuntime,
        IHandSlotDropPreparation,
        IHandSlotHudSource,
        IRuntimeRoomStateParticipant
    {
        [SerializeField] private HandToolDefinition definition;
        [SerializeField] private ToolResourceState resourceState = new ToolResourceState();
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D[] toolColliders;
        [SerializeField] private SpriteRenderer toolRenderer;
        [SerializeField] private string roomPersistenceId;

        private PlayerHandSlot owningSlot;
        private int worldLayer;

        public override string RuntimeItemId => definition != null
            ? $"{definition.ToolId}:{GetInstanceID()}"
            : $"hand-tool:{GetInstanceID()}";
        public override HandSlotItemKind ItemKind => HandSlotItemKind.HandTool;
        public HandToolDefinition Definition => definition;
        public ToolResourceState ResourceState => resourceState;
        public int CurrentResource => resourceState.Current;
        public int MaximumResource => resourceState.Maximum;
        public override bool CanEnterHandSlot => definition != null;
        public string StableItemId => definition != null ? definition.ToolId : string.Empty;
        public string DisplayName => definition != null ? definition.DisplayName : string.Empty;
        public Sprite HudIcon => definition != null && definition.HudIcon != null
            ? definition.HudIcon
            : toolRenderer != null ? toolRenderer.sprite : null;
        public bool ShowResource => definition != null && definition.ResourceMode != ToolResourceMode.Infinite;
        public string PrimaryActionLabel => ResolvePrimaryActionLabel(definition != null ? definition.ToolId : string.Empty);
        public bool IsHandTool => true;
        public string RuntimeRoomStateId => string.IsNullOrWhiteSpace(roomPersistenceId)
            ? StableItemId + ":" + gameObject.name
            : roomPersistenceId;

        protected virtual void Awake()
        {
            EnsureReferences();
            worldLayer = gameObject.layer;
            resourceState.Initialize(definition);
        }

        public override bool CanWorldPickup(Vector2 actorPosition)
        {
            return definition != null
                && owningSlot == null
                && Vector2.Distance(actorPosition, transform.position) <= PlayerHandSlot.MaxPickupDistance;
        }

        public override bool TryEnterHandSlot(HandSlotPresenter presenter)
        {
            if (presenter == null || definition == null)
            {
                return false;
            }

            EnsureReferences();
            resourceState.Initialize(definition);
            if (owningSlot == null)
            {
                worldLayer = gameObject.layer;
            }
            owningSlot = presenter.GetComponentInParent<PlayerHandSlot>();
            transform.SetParent(presenter.CarrySocket, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
            }
            SetCollidersEnabled(false);
            SetLayerIfDefined("HeldObject");
            MirrorRunState(true);
            return owningSlot != null;
        }

        public override void ExitHandSlot(Vector2 worldPosition, bool restorePlayerCollision)
        {
            CancelActiveAction();
            owningSlot = null;
            transform.SetParent(null, true);
            transform.position = worldPosition;
            gameObject.layer = worldLayer;
            if (body != null)
            {
                body.simulated = true;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            SetCollidersEnabled(true);
            MirrorRunState(false);
        }

        public override bool TryPrimaryUse(
            PlayerHandSlot owner,
            PlayerActionContext context,
            int facingSign,
            LayerMask blockMask)
        {
            if (owner == null || owner.CurrentItem != this || definition == null)
            {
                return false;
            }

            ToolActionController controller = owner.GetComponent<ToolActionController>();
            return controller != null && controller.TryStart(this, owner, context, facingSign);
        }

        public virtual bool TryPrepareForDrop(PlayerActionContext context)
        {
            CancelActiveAction();
            return true;
        }

        public override bool CanPassPortal(StarNight.Interaction.Carry.ICarryPortalClearance clearance) => true;

        public override void SuspendForPortal(Transform carrySocket)
        {
            CancelActiveAction();
            if (body != null)
            {
                body.simulated = false;
            }
            SetCollidersEnabled(false);
            if (carrySocket != null)
            {
                transform.SetParent(carrySocket, false);
                transform.localPosition = Vector3.zero;
            }
        }

        public override bool RestoreAfterPortal(HandSlotPresenter presenter) => TryEnterHandSlot(presenter);

        public void Configure(HandToolDefinition configuredDefinition)
        {
            definition = configuredDefinition;
            resourceState ??= new ToolResourceState();
            resourceState.Initialize(definition);
            EnsureReferences();
        }

        public void RepairFull() => resourceState.RepairFull();

        public void StowForInventory(Transform inventoryRoot)
        {
            CancelActiveAction();
            owningSlot = null;
            transform.SetParent(inventoryRoot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (body != null)
            {
                body.simulated = false;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            SetCollidersEnabled(false);
            MirrorRunState(false);
            gameObject.SetActive(false);
        }

        public ToolSnapshot CaptureSnapshot()
        {
            return new ToolSnapshot
            {
                ToolId = StableItemId,
                Position = transform.position,
                Rotation = transform.eulerAngles.z,
                CurrentResource = CurrentResource,
                MaximumResource = MaximumResource,
                Active = gameObject.activeSelf,
            };
        }

        public bool RestoreSnapshot(ToolSnapshot snapshot)
        {
            if (snapshot == null
                || definition == null
                || !string.Equals(snapshot.ToolId, definition.ToolId, System.StringComparison.Ordinal))
            {
                return false;
            }

            CancelActiveAction();
            owningSlot = null;
            gameObject.SetActive(snapshot.Active);
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(
                snapshot.Position,
                Quaternion.Euler(0f, 0f, snapshot.Rotation));
            resourceState.RestoreCurrent(snapshot.CurrentResource);
            EnsureReferences();
            if (body != null)
            {
                body.simulated = false;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            SetCollidersEnabled(true);
            SetLayerIfDefined("DynamicObject");
            return true;
        }

        public string CaptureRuntimeRoomState() => JsonUtility.ToJson(CaptureSnapshot());

        public void RestoreRuntimeRoomState(string payload)
        {
            if (!string.IsNullOrWhiteSpace(payload))
            {
                RestoreSnapshot(JsonUtility.FromJson<ToolSnapshot>(payload));
            }
        }

        public void SetRoomPersistenceId(string persistenceId)
        {
            roomPersistenceId = persistenceId ?? string.Empty;
        }

        public virtual bool SupportsAirPound => false;

        public virtual ToolDispatchReport DispatchImpact(
            IToolReactionWorld world,
            ToolDispatchRequest request)
        {
            return world != null ? world.Dispatch(request) : ToolDispatchReport.Rejected();
        }

        protected virtual void CancelActiveAction()
        {
            PlayerHandSlot owner = owningSlot;
            owner?.GetComponent<ToolActionController>()?.CancelCurrentAction(this);
        }

        private void EnsureReferences()
        {
            body = body != null ? body : GetComponent<Rigidbody2D>();
            toolColliders = toolColliders != null && toolColliders.Length > 0
                ? toolColliders
                : GetComponentsInChildren<Collider2D>(true);
            toolRenderer = toolRenderer != null ? toolRenderer : GetComponentInChildren<SpriteRenderer>(true);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (toolColliders == null)
            {
                return;
            }
            for (int index = 0; index < toolColliders.Length; index++)
            {
                if (toolColliders[index] != null)
                {
                    toolColliders[index].enabled = enabled;
                }
            }
        }

        private void SetLayerIfDefined(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }

        private void MirrorRunState(bool held)
        {
            if (definition == null || !GameBootstrap.IsReady
                || !GameBootstrap.Instance.Services.TryGet(out RunManager manager)
                || manager.Current == null)
            {
                return;
            }

            if (held)
            {
                manager.Current.handToolId = definition.ToolId;
            }
            else if (string.Equals(manager.Current.handToolId, definition.ToolId, System.StringComparison.Ordinal))
            {
                manager.Current.handToolId = string.Empty;
            }
        }

        private static string ResolvePrimaryActionLabel(string toolId)
        {
            return toolId switch
            {
                "TOOL_PICKAXE" => "파기",
                "TOOL_SHOVEL" => "파기",
                "TOOL_WATERING_CAN" => "물주기",
                "TOOL_POUNDER" => "내리치기",
                "TOOL_HOOK_LAUNCHER" => "발사",
                "TOOL_WIND_UMBRELLA" => "펼치기",
                "ITEM_MOON_EYE_COMPASS" => "집중 탐지",
                _ => "사용",
            };
        }
    }
}

#endif
