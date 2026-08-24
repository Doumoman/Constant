namespace StarNight.Character.Integration
{
    /// <summary>생성 맵 통합 진단 종류 — 복구 가능한 입력 결함은 예외가
    /// 아니라 진단 데이터로 보고한다.</summary>
    public enum CharacterIntegrationDiagnosticKind
    {
        MissingStartCell,
        StartCellOutsideWorldBounds,
        StartCellOutsideRoomBounds,
        StartRoomMismatch,
        UndeclaredRouteEdge,
        RouteBlockedMissingRoom,
        RouteBlockedUnpreparedRoom,
        UnsupportedRouteRequirement,
        MissingBombSupport,
        MissingRopeSupport
    }
}
