namespace StarNight.Character.GeneratedRunValidation
{
    /// <summary>생성 런 검증 진단 종류.</summary>
    public enum CharacterGeneratedRunValidationDiagnosticKind
    {
        DuplicateRoomId,
        RoomOutsideWorldBounds,
        MicrochunkMisaligned,
        MicrochunkOwnerRoomMissing,
        MicrochunkOutsideOwnerRoom,
        DuplicateMicrochunkOccupancy,
        RouteRoomMissing,
        RouteCellOutsideDeclaredRoom,
        IntegrationRejected,
        ItemRoomMissing,
        ItemOutsideRoomOrWorld,
        ItemOnReservedCell
    }
}
