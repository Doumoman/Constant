namespace StarNight.Character.Live.Adapters
{
    /// <summary>
    /// 어댑터 계층 진단 종류(입력 형태 결함 전용). 방/루트/아이템/역량
    /// 검증 진단은 기존 캐릭터 생성 런 검증 정책이 소유한다(중복 없음).
    /// </summary>
    public enum CharacterLiveGeneratedMapDiagnosticKind
    {
        MissingDefinition,
        IncompleteTileData,
        DimensionMismatch,
        DuplicateChunkPlacement,
        ChunkOutsideWorld,
        StartCellOutsideWorld,
        StartRoomNotPlaced
    }
}
