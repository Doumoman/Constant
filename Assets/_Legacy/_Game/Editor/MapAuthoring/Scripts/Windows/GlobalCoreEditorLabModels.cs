#if LEGACY_DISABLED
using StarNight.Map;
using StarNight.Stage.CameraSystem;
using StarNight.Stage.Layout;
using StarNight.Stage.Rooms;
using StarNight.Stage.Secrets;
using StarNight.Tools.Inventory;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public sealed class StageCameraLabResult
    {
        public Vector2Int RoomSizeCells;
        public RoomCameraMode Mode;
        public float VisibleHeightTiles;
        public float VisibleWidthTiles;
        public Rect ViewportRect;
    }

    public sealed class SecretDimensionLabResult
    {
        public bool IsValid;
        public string Failure;
        public int SecretSeed;
        public string MainPortalId;
        public string ReturnPortalId;
        public Vector2Int ReturnSafeCell;
        public ToolTag RevealTool;
    }

    public sealed class InventoryInteractionLabState
    {
        public int ItemId;
        public int CurrentDurability;
        public int MaxDurability;
        public bool LastDuplicateRepaired;
        public bool RuntimeCopyReplaced;
        public string LastFeedbackMessage;
    }

    public static class GlobalCoreEditorLabModels
    {
        private const ToolTag SecretRevealTools = ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Shovel;

        public static RoomInteriorLayout GenerateRoomTiles(int seed, Vector2Int chunkGridSize)
        {
            return RoomInteriorGenerator.Generate(new RoomInteriorGenerationRequest
            {
                RoomId = "COMMON_TEST_ROOM",
                Seed = seed,
                ChunkGridSize = chunkGridSize,
            });
        }

        public static StageCameraLabResult PreviewCamera(Vector2Int roomSizeCells, float displayAspect)
        {
            var profile = new CameraTileProfile();
            Vector2Int size = new Vector2Int(
                Mathf.Max(1, roomSizeCells.x),
                Mathf.Max(1, roomSizeCells.y));
            return new StageCameraLabResult
            {
                RoomSizeCells = size,
                Mode = profile.ResolveMode(size),
                VisibleHeightTiles = profile.visibleHeightTiles,
                VisibleWidthTiles = profile.VisibleWidthTiles,
                ViewportRect = profile.CalculateViewportRect(Mathf.Max(0.01f, displayAspect)),
            };
        }

        public static SecretDimensionLabResult PreviewSecret(
            int stageSeed,
            string sourceRoomStableId,
            string anchorStableId,
            Vector2Int returnSafeCell,
            ToolTag revealTool)
        {
            string source = sourceRoomStableId?.Trim() ?? string.Empty;
            string anchor = anchorStableId?.Trim() ?? string.Empty;
            bool toolAllowed = revealTool != ToolTag.None
                               && (revealTool & SecretRevealTools) == revealTool;
            string failure = string.IsNullOrWhiteSpace(source)
                ? "Source Room Stable ID is required."
                : string.IsNullOrWhiteSpace(anchor)
                    ? "Anchor Stable ID is required."
                    : !toolAllowed
                        ? "Reveal tool must be Bomb, Pickaxe, or Shovel."
                        : string.Empty;
            return new SecretDimensionLabResult
            {
                IsValid = string.IsNullOrEmpty(failure),
                Failure = failure,
                SecretSeed = SecretSeedUtility.Create(stageSeed, source, anchor),
                MainPortalId = "SECRET_" + anchor,
                ReturnPortalId = "SECRET_RETURN_" + anchor,
                ReturnSafeCell = returnSafeCell,
                RevealTool = revealTool,
            };
        }

        public static InventoryInteractionLabState ApplyDuplicate(InventoryInteractionLabState state)
        {
            state ??= CreateInventoryState(1001, 1, 10);
            var entry = new InventoryEntry
            {
                ItemId = state.ItemId,
                MaxDurability = Mathf.Max(0, state.MaxDurability),
                CurrentDurability = Mathf.Clamp(state.CurrentDurability, 0, Mathf.Max(0, state.MaxDurability)),
            };
            var recovery = ItemDurabilityService.ApplyDuplicatePickup(entry, null);
            state.CurrentDurability = entry.CurrentDurability;
            state.MaxDurability = entry.MaxDurability;
            state.LastDuplicateRepaired = recovery.Succeeded;
            state.LastFeedbackMessage = recovery.Message ?? string.Empty;
            state.RuntimeCopyReplaced = false;
            return state;
        }

        public static InventoryInteractionLabState DepleteWithoutAutoSwap(InventoryInteractionLabState state)
        {
            state ??= CreateInventoryState(1001, 1, 10);
            state.CurrentDurability = 0;
            state.LastDuplicateRepaired = false;
            state.LastFeedbackMessage = string.Empty;
            state.RuntimeCopyReplaced = false;
            return state;
        }

        public static InventoryInteractionLabState CreateInventoryState(
            int itemId,
            int currentDurability,
            int maxDurability)
        {
            int maximum = Mathf.Max(0, maxDurability);
            return new InventoryInteractionLabState
            {
                ItemId = Mathf.Max(1, itemId),
                CurrentDurability = Mathf.Clamp(currentDurability, 0, maximum),
                MaxDurability = maximum,
            };
        }
    }
}

#endif
