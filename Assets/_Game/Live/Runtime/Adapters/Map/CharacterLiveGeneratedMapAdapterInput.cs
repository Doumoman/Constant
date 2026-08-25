using System;
using System.Collections.Generic;
using StarNight.Character.GeneratedRunValidation;
using StarNight.Character.Integration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Live.Adapters
{
    /// <summary>
    /// 생성 MAP 출력의 좁은 라이브측 입력 계약. MAP 런타임에 캐릭터 소비용
    /// 파사드가 없으므로(레지스트리·CHAR06_01 확립) 공용 도메인 값만 담는
    /// 이 인터페이스로 생성 결과를 받는다 — 생성 로직 대체물이 아니다.
    /// </summary>
    public sealed class CharacterLiveGeneratedMapAdapterInput
    {
        private static readonly CharacterLivePlacedMicrochunk[] EmptyChunks =
            Array.Empty<CharacterLivePlacedMicrochunk>();

        private static readonly CharacterGeneratedRouteEdgeSnapshot[] EmptyRoutes =
            Array.Empty<CharacterGeneratedRouteEdgeSnapshot>();

        private static readonly CharacterGeneratedItemPlacementSnapshot[] EmptyItems =
            Array.Empty<CharacterGeneratedItemPlacementSnapshot>();

        private static readonly WorldTileCoord[] EmptyCells =
            Array.Empty<WorldTileCoord>();

        public CharacterLiveGeneratedMapAdapterInput(
            int runId,
            int seed,
            bool hasStartCell,
            WorldTileCoord startCell,
            IReadOnlyList<CharacterLivePlacedMicrochunk> placedMicrochunks,
            IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> declaredRoutes,
            IReadOnlyList<CharacterGeneratedItemPlacementSnapshot> itemPlacements,
            IReadOnlyList<WorldTileCoord> exitMarkers,
            IReadOnlyList<WorldTileCoord> blockedValidationCells)
        {
            RunId = runId;
            Seed = seed;
            HasStartCell = hasStartCell;
            StartCell = startCell;
            PlacedMicrochunks = placedMicrochunks ?? EmptyChunks;
            DeclaredRoutes = declaredRoutes ?? EmptyRoutes;
            ItemPlacements = itemPlacements ?? EmptyItems;
            ExitMarkers = exitMarkers ?? EmptyCells;
            BlockedValidationCells = blockedValidationCells ?? EmptyCells;
        }

        public int RunId { get; }
        public int Seed { get; }
        public bool HasStartCell { get; }
        public WorldTileCoord StartCell { get; }
        public IReadOnlyList<CharacterLivePlacedMicrochunk> PlacedMicrochunks { get; }
        public IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> DeclaredRoutes { get; }
        public IReadOnlyList<CharacterGeneratedItemPlacementSnapshot> ItemPlacements { get; }
        public IReadOnlyList<WorldTileCoord> ExitMarkers { get; }
        public IReadOnlyList<WorldTileCoord> BlockedValidationCells { get; }
    }
}
