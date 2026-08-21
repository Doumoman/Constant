#if LEGACY_DISABLED
using StarNight.Map;
using StarNight.Tools.Items;
using UnityEngine;

namespace StarNight.Tools.Core
{
    public enum ToolResourceMode
    {
        Infinite,
        Durability,
        Charge,
    }

    [CreateAssetMenu(menuName = "Game/Tools/Hand Tool Definition")]
    public sealed class HandToolDefinition : ItemDefinition
    {
        [SerializeField] private string toolId;
        [SerializeField] private string displayName;
        [SerializeField] private ToolTag toolTags;
        [SerializeField] private ToolResourceMode resourceMode;
        [SerializeField, Min(0)] private int maxResource;
        [SerializeField] private ToolActionProfile groundAction = new ToolActionProfile();
        [SerializeField] private ToolActionProfile airAction = new ToolActionProfile();
        [SerializeField] private Vector2Int[] targetCellOffsets = { Vector2Int.right };
        [SerializeField] private GameObject runtimePrefab;
        [SerializeField] private Sprite hudIcon;
        [SerializeField] private Sprite heldSprite;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField, Min(0)] private int shopPriceWon;
        [SerializeField] private AudioClip useSfx;
        [SerializeField] private GameObject useVfx;
        [SerializeField] private ToolFailureFeedback failureFeedback = ToolFailureFeedback.InvalidTarget;
        [SerializeField, Range(1, 8)] private int previewRangeCells = 1;
        [SerializeField, Range(0f, 180f)] private float previewAngleDegrees;

        public string ToolId => toolId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public ToolTag ToolTags => toolTags;
        public ToolResourceMode ResourceMode => resourceMode;
        public int MaxResource => maxResource;
        public ToolActionProfile GroundAction => groundAction;
        public ToolActionProfile AirAction => airAction;
        public Vector2Int[] TargetCellOffsets => targetCellOffsets ?? System.Array.Empty<Vector2Int>();
        public GameObject RuntimePrefab => runtimePrefab;
        public Sprite HudIcon => hudIcon;
        public Sprite HeldSprite => heldSprite;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public int ShopPriceWon => shopPriceWon;
        public AudioClip UseSfx => useSfx;
        public GameObject UseVfx => useVfx;
        public ToolFailureFeedback FailureFeedback => failureFeedback;
        public int PreviewRangeCells => previewRangeCells;
        public float PreviewAngleDegrees => previewAngleDegrees;
        public override int ItemId => base.ItemId > 0 ? base.ItemId : ToolItemIdCatalog.Resolve(ToolId);
        public override ItemUseCategory UseCategory => ToolId switch
        {
            "EQUIPMENT_SPRING" => ItemUseCategory.JumpModifier,
            "ITEM_MOON_EYE_COMPASS" => ItemUseCategory.PassiveDetector,
            _ => ItemUseCategory.ActiveTool,
        };
        public override int MaxDurability => resourceMode == ToolResourceMode.Infinite ? 0 : MaxResource;
        public override bool AllowDuplicate => true;
        public override bool CanDrop => true;
        public override bool TabSelectable => base.TabSelectable;
        public override int SelectionOrder => base.SelectionOrder != 0 ? base.SelectionOrder : ItemId;

        public void Configure(
            string id,
            string configuredDisplayName,
            ToolTag tags,
            ToolResourceMode mode,
            int resource,
            int price,
            ToolActionProfile configuredGroundAction,
            ToolActionProfile configuredAirAction,
            Vector2Int[] offsets,
            int rangeCells = 1,
            float angleDegrees = 0f)
        {
            toolId = id;
            displayName = configuredDisplayName;
            toolTags = tags;
            resourceMode = mode;
            maxResource = mode == ToolResourceMode.Infinite ? 0 : Mathf.Max(1, resource);
            shopPriceWon = Mathf.Max(0, price);
            groundAction = configuredGroundAction ?? new ToolActionProfile();
            airAction = configuredAirAction ?? new ToolActionProfile();
            targetCellOffsets = offsets ?? System.Array.Empty<Vector2Int>();
            previewRangeCells = Mathf.Clamp(rangeCells, 1, 8);
            previewAngleDegrees = Mathf.Clamp(angleDegrees, 0f, 180f);
            if (base.ItemId <= 0)
            {
                int resolvedItemId = ToolItemIdCatalog.Resolve(id);
                ConfigureItemContract(
                    resolvedItemId,
                    id == "EQUIPMENT_SPRING" ? ItemUseCategory.JumpModifier
                        : id == "ITEM_MOON_EYE_COMPASS" ? ItemUseCategory.PassiveDetector
                        : ItemUseCategory.ActiveTool,
                    mode == ToolResourceMode.Infinite ? 0 : maxResource,
                    true,
                    true,
                    true,
                    resolvedItemId);
            }
        }

        public void AssignRuntimePrefab(GameObject prefab)
        {
            runtimePrefab = prefab;
        }

        private void OnValidate()
        {
            maxResource = resourceMode == ToolResourceMode.Infinite ? 0 : Mathf.Max(1, maxResource);
            shopPriceWon = Mathf.Max(0, shopPriceWon);
            previewRangeCells = Mathf.Clamp(previewRangeCells, 1, 8);
            previewAngleDegrees = Mathf.Clamp(previewAngleDegrees, 0f, 180f);
            groundAction ??= new ToolActionProfile();
            airAction ??= new ToolActionProfile();
            targetCellOffsets ??= System.Array.Empty<Vector2Int>();
        }
    }
}

#endif
