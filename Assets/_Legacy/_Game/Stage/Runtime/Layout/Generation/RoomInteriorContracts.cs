#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    public enum ChunkPatternRole
    {
        Plain,
        Damage,
        Interaction,
        Puzzle,
        Condition,
    }

    public enum MicroCellKind
    {
        Solid,
        Empty,
        Hazard,
        Interaction,
        Puzzle,
        Reward,
        SoftSoil,
    }

    public enum MicroSocketSide
    {
        West,
        East,
        South,
        North,
    }

    public enum HiddenContentType
    {
        EmbeddedPocket,
        BlindSecretEntrance,
        SecretDimensionRoom,
    }

    public enum HiddenPocketHint
    {
        FineCrack,
        Starlight,
        Sound,
    }

    public enum ToolEscapeKind
    {
        ShovelDirt,
        BombCrack,
        RopeShaft,
    }

    [Serializable]
    public sealed class ChunkRoleDeckAdjustment
    {
        public ChunkPatternRole Role;
        [Range(-1, 1)] public int CountDelta;
    }

    [Serializable]
    public sealed class RoomInteriorGenerationRequest
    {
        public string RoomId = "COMMON_TEST_ROOM";
        public int Seed;
        public Vector2Int ChunkGridSize = new Vector2Int(4, 3);
        public List<ChunkRoleDeckAdjustment> RegionRoleAdjustments = new List<ChunkRoleDeckAdjustment>();
    }

    [Serializable]
    public sealed class GeneratedMicroSocket
    {
        public MicroSocketSide Side;
        public Vector2Int LocalCell;
        public Vector2Int NeighborChunk;
        public bool External;
    }

    [Serializable]
    public sealed class GeneratedMicroChunk
    {
        public const int Width = 8;
        public const int Height = 8;

        public Vector2Int GridCell;
        public Vector2Int OriginCell;
        public int GenerationOrder;
        public string PatternId;
        public ChunkPatternRole Role;
        public bool MainRoute;
        public List<GeneratedMicroSocket> Sockets = new List<GeneratedMicroSocket>();
        public MicroCellKind[] Cells = new MicroCellKind[Width * Height];

        public MicroCellKind GetCell(Vector2Int localCell)
        {
            return IsLocalCell(localCell) ? Cells[localCell.y * Width + localCell.x] : MicroCellKind.Solid;
        }

        public void SetCell(Vector2Int localCell, MicroCellKind kind)
        {
            if (IsLocalCell(localCell)) Cells[localCell.y * Width + localCell.x] = kind;
        }

        public static bool IsLocalCell(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }
    }

    [Serializable]
    public sealed class GeneratedHiddenContent
    {
        public string StableId;
        public HiddenContentType Type;
        public Vector2Int ChunkGridCell;
        public Vector2Int LocalCell;
        public HiddenPocketHint Hint;
        public ToolTag RevealTools;
    }

    [Serializable]
    public sealed class GeneratedToolEscape
    {
        public string PatternId;
        public ToolEscapeKind Kind;
        public Vector2Int ChunkGridCell;
        public ToolTag RequiredTool;
        public Vector2Int ToolPickupLocalCell;
        public Vector2Int RecoveryRackLocalCell;
        public Vector2Int RewardLocalCell;
        public float RecoveryDelaySeconds = 1.2f;
        public float AbandonHoldSeconds = 2f;
        public bool EmergencyDoorAfterThirdBell = true;
    }

    [Serializable]
    public sealed class RoomInteriorLayout
    {
        public string RoomId;
        public int Seed;
        public Vector2Int ChunkGridSize;
        public Vector2Int EntryWorldCell;
        public Vector2Int ExitWorldCell;
        public List<GeneratedMicroChunk> Chunks = new List<GeneratedMicroChunk>();
        public List<GeneratedHiddenContent> HiddenContents = new List<GeneratedHiddenContent>();
        public List<GeneratedToolEscape> ToolEscapes = new List<GeneratedToolEscape>();
        public string ValidationHash;
        public bool HasT0MainRoute;
        public List<string> ValidationErrors = new List<string>();

        public Vector2Int SizeCells => new Vector2Int(
            ChunkGridSize.x * GeneratedMicroChunk.Width,
            ChunkGridSize.y * GeneratedMicroChunk.Height);

        public GeneratedMicroChunk FindChunk(Vector2Int gridCell)
        {
            for (int index = 0; index < Chunks.Count; index++)
            {
                if (Chunks[index] != null && Chunks[index].GridCell == gridCell) return Chunks[index];
            }
            return null;
        }

        public MicroCellKind GetWorldCell(Vector2Int worldCell)
        {
            if (worldCell.x < 0 || worldCell.y < 0 || worldCell.x >= SizeCells.x || worldCell.y >= SizeCells.y)
            {
                return MicroCellKind.Solid;
            }
            Vector2Int chunkCell = new Vector2Int(
                worldCell.x / GeneratedMicroChunk.Width,
                worldCell.y / GeneratedMicroChunk.Height);
            GeneratedMicroChunk chunk = FindChunk(chunkCell);
            return chunk != null ? chunk.GetCell(worldCell - chunk.OriginCell) : MicroCellKind.Solid;
        }
    }
}

#endif
