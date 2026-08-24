using System;
using System.Collections.Generic;
using StarNight.Character.Integration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.GeneratedRunValidation
{
    /// <summary>
    /// 생성 런 스냅샷 — 검증 입력이 되는 읽기 전용 값 데이터 묶음.
    /// MAP 공용 도메인 값(WorldTileCoord/RoomId)과 CHAR06_01 스냅샷 계약만
    /// 담으며, 생성기 자체를 소유하지 않는다(생성 결과 → 스냅샷 투영은
    /// 라이브 통합 계층 소관). 스냅샷 생성·검증은 MAP 데이터를 편집하거나
    /// Tilemap 셀을 쓰지 않는다.
    /// </summary>
    public sealed class CharacterGeneratedRunSnapshot
    {
        private static readonly CharacterGeneratedRoomSnapshot[] EmptyRooms =
            Array.Empty<CharacterGeneratedRoomSnapshot>();

        private static readonly CharacterGeneratedMicrochunkSnapshot[] EmptyMicrochunks =
            Array.Empty<CharacterGeneratedMicrochunkSnapshot>();

        private static readonly CharacterGeneratedRouteEdgeSnapshot[] EmptyRoutes =
            Array.Empty<CharacterGeneratedRouteEdgeSnapshot>();

        private static readonly CharacterGeneratedItemPlacementSnapshot[] EmptyItems =
            Array.Empty<CharacterGeneratedItemPlacementSnapshot>();

        private static readonly WorldTileCoord[] EmptyCells =
            Array.Empty<WorldTileCoord>();

        public CharacterGeneratedRunSnapshot(
            int runId,
            int seed,
            CharacterGeneratedMapStartSnapshot start,
            IReadOnlyList<CharacterGeneratedRoomSnapshot> rooms,
            IReadOnlyList<CharacterGeneratedMicrochunkSnapshot> microchunks,
            IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> routes,
            IReadOnlyList<CharacterGeneratedItemPlacementSnapshot> itemPlacements,
            IReadOnlyList<WorldTileCoord> exitMarkers,
            IReadOnlyList<WorldTileCoord> blockedValidationCells)
        {
            RunId = runId;
            Seed = seed;
            Start = start;
            Rooms = rooms ?? EmptyRooms;
            Microchunks = microchunks ?? EmptyMicrochunks;
            Routes = routes ?? EmptyRoutes;
            ItemPlacements = itemPlacements ?? EmptyItems;
            ExitMarkers = exitMarkers ?? EmptyCells;
            BlockedValidationCells = blockedValidationCells ?? EmptyCells;
        }

        public int RunId { get; }
        public int Seed { get; }
        public CharacterGeneratedMapStartSnapshot Start { get; }
        public IReadOnlyList<CharacterGeneratedRoomSnapshot> Rooms { get; }
        public IReadOnlyList<CharacterGeneratedMicrochunkSnapshot> Microchunks { get; }
        public IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> Routes { get; }
        public IReadOnlyList<CharacterGeneratedItemPlacementSnapshot> ItemPlacements { get; }

        /// <summary>출구/목표 표식 셀(기록용 데이터).</summary>
        public IReadOnlyList<WorldTileCoord> ExitMarkers { get; }

        /// <summary>명시적으로 배치 금지된 검증 셀.</summary>
        public IReadOnlyList<WorldTileCoord> BlockedValidationCells { get; }
    }
}
