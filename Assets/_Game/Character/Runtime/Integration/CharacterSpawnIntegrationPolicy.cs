using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Integration
{
    /// <summary>
    /// 생성 맵 시작 → 스폰 요청 판정(순수·결정적). 유효하지 않은 시작은
    /// 예외 대신 진단을 반환한다. 월드 중심은 공용 좌표 브리지에서만 얻는다.
    /// </summary>
    public static class CharacterSpawnIntegrationPolicy
    {
        public static bool TryCreateSpawnRequest(
            in CharacterGeneratedMapStartSnapshot snapshot,
            int actorId,
            out CharacterPlayerSpawnRequest request,
            out CharacterIntegrationDiagnostic diagnostic)
        {
            request = default;
            diagnostic = default;

            if (!snapshot.HasStartCell)
            {
                diagnostic = new CharacterIntegrationDiagnostic(
                    CharacterIntegrationDiagnosticKind.MissingStartCell,
                    "run:" + snapshot.MapRunId);
                return false;
            }

            WorldTileCoord cell = snapshot.StartCell;

            if (!WorldCoordinateUtility.IsValid(cell))
            {
                diagnostic = new CharacterIntegrationDiagnostic(
                    CharacterIntegrationDiagnosticKind.StartCellOutsideWorldBounds,
                    CellSubject(cell));
                return false;
            }

            if (cell.X < snapshot.RoomMinCell.X || cell.X > snapshot.RoomMaxCell.X
                || cell.Y < snapshot.RoomMinCell.Y || cell.Y > snapshot.RoomMaxCell.Y)
            {
                diagnostic = new CharacterIntegrationDiagnostic(
                    CharacterIntegrationDiagnosticKind.StartCellOutsideRoomBounds,
                    CellSubject(cell));
                return false;
            }

            // 셀에서 유도한 방과 스냅샷이 선언한 시작 방이 일치해야 한다.
            if (!CharacterRoomId.FromWorldTile(cell).Equals(snapshot.StartRoomId))
            {
                diagnostic = new CharacterIntegrationDiagnostic(
                    CharacterIntegrationDiagnosticKind.StartRoomMismatch,
                    CellSubject(cell));
                return false;
            }

            request = new CharacterPlayerSpawnRequest(
                actorId,
                cell,
                CharacterMapCoordinateBridge.GetCellCenter(cell),
                snapshot.StartRoomId);
            return true;
        }

        private static string CellSubject(WorldTileCoord cell)
        {
            return "cell:" + cell.X + "," + cell.Y;
        }
    }
}
