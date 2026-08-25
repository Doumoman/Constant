using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Live.Run
{
    /// <summary>
    /// L01_03 한정 임시 수동 시작 소스. 고정 시작 셀 하나로
    /// CharacterGeneratedMapStartSnapshot을 만든다 — MAP 방/마이크로청크/
    /// 루트/아이템/Tilemap을 생성하지 않으며, L02_02 MAP 어댑터가 캐릭터
    /// 런타임 무변경으로 교체할 수 있는 동일 계약 표면이다.
    /// 방 경계는 셀이 속한 마이크로청크(12×8) 정렬 경계를 WorldGenConstants
    /// 계약으로 계산한다(상수 복제 없음).
    /// </summary>
    public sealed class CharacterLiveManualStartSource : MonoBehaviour
    {
        [SerializeField] private int mapRunId = 1;
        [SerializeField] private int startCellX = 5;
        [SerializeField] private int startCellY = 0;

        public bool TryCreateStartSnapshot(
            out CharacterGeneratedMapStartSnapshot snapshot)
        {
            snapshot = default;

            WorldTileCoord startCell;
            if (!WorldCoordinateUtility.TryCreateWorldTile(
                startCellX, startCellY, out startCell))
            {
                Debug.LogWarning(
                    "CharacterLiveManualStartSource: 시작 셀이 월드 밖이다 ("
                    + startCellX + "," + startCellY + ")", this);
                return false;
            }

            int chunkMinX = (startCell.X / WorldGenConstants.MicroChunkWidthTiles)
                * WorldGenConstants.MicroChunkWidthTiles;
            int chunkMinY = (startCell.Y / WorldGenConstants.MicroChunkHeightTiles)
                * WorldGenConstants.MicroChunkHeightTiles;

            WorldTileCoord roomMin;
            WorldTileCoord roomMax;
            if (!WorldCoordinateUtility.TryCreateWorldTile(
                    chunkMinX, chunkMinY, out roomMin)
                || !WorldCoordinateUtility.TryCreateWorldTile(
                    chunkMinX + WorldGenConstants.MicroChunkWidthTiles - 1,
                    chunkMinY + WorldGenConstants.MicroChunkHeightTiles - 1,
                    out roomMax))
            {
                return false;
            }

            snapshot = new CharacterGeneratedMapStartSnapshot(
                mapRunId,
                CharacterRoomId.FromWorldTile(startCell),
                true,
                startCell,
                roomMin,
                roomMax);
            return true;
        }
    }
}
