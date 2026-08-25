using System.Collections.Generic;
using StarNight.Character.GeneratedRunValidation;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Character.Live.Adapters
{
    /// <summary>
    /// 생성 MAP 출력 → 캐릭터 생성 런 계약 투영기(순수·결정적). MAP 공용
    /// 계약만 읽고(정의/변환기/점유/좌표) 아무것도 변조하지 않는다.
    /// 좌표 수학은 WorldCoordinateUtility/WorldGenConstants에, 레이어 의미는
    /// MicrochunkTileLayerOccupancy + CharacterMapCellState.FromTileLayer에,
    /// 방/루트/아이템 검증은 CHAR06_02 정책에 전부 위임한다.
    /// 같은 입력이면 같은 투영/다이제스트다(입력 순서 보존).
    /// </summary>
    public static class CharacterLiveGeneratedMapAdapter
    {
        public static CharacterLiveGeneratedMapProjection Project(
            CharacterLiveGeneratedMapAdapterInput input,
            int actorId,
            in CharacterRunInventoryState inventory)
        {
            var diagnostics = new List<CharacterLiveGeneratedMapDiagnostic>();
            var rooms = new List<CharacterGeneratedRoomSnapshot>();
            var microchunks = new List<CharacterGeneratedMicrochunkSnapshot>();
            var readyRooms = new List<CharacterRoomId>();
            var generatedCells = new Dictionary<long, CharacterMapCellState>();
            var placedKeys = new HashSet<long>();

            if (input == null)
            {
                input = new CharacterLiveGeneratedMapAdapterInput(
                    0, 0, false, default, null, null, null, null, null);
            }

            // (1) 배치 청크 → 방/마이크로청크/셀 상태 (입력 순서 보존 — 결정적).
            for (int index = 0; index < input.PlacedMicrochunks.Count; index++)
            {
                ProjectChunk(
                    input.PlacedMicrochunks[index], index, diagnostics,
                    rooms, microchunks, readyRooms, generatedCells, placedKeys);
            }

            // (2) 시작 셀 — 배치된 방 안에 있어야 시작 스냅샷이 성립한다.
            CharacterGeneratedMapStartSnapshot start = ProjectStart(
                input, rooms, diagnostics);

            // (3) 캐릭터 생성 런 스냅샷 (루트/아이템/표식은 계약 그대로 전달).
            var snapshot = new CharacterGeneratedRunSnapshot(
                input.RunId,
                input.Seed,
                start,
                rooms,
                microchunks,
                input.DeclaredRoutes,
                input.ItemPlacements,
                input.ExitMarkers,
                input.BlockedValidationCells);

            // (4) 준비/루트/월드 질의 소스.
            var readinessSource = new CharacterLiveGeneratedReadinessSource(readyRooms);
            var routeSource = new CharacterLiveGeneratedRouteSource(
                input.DeclaredRoutes, readinessSource);
            var worldQuery = new CharacterLiveMapWorldQueryAdapter(generatedCells);

            // (5) 기존 캐릭터 검증 정책으로 투영 결과 검증(중복 구현 없음).
            CharacterGeneratedRunValidationResult validation =
                CharacterGeneratedRunValidationPolicy.Validate(
                    snapshot, actorId, in inventory, readinessSource);

            return new CharacterLiveGeneratedMapProjection(
                snapshot, validation, readinessSource, routeSource,
                worldQuery, diagnostics);
        }

        private static void ProjectChunk(
            in CharacterLivePlacedMicrochunk placed,
            int index,
            List<CharacterLiveGeneratedMapDiagnostic> diagnostics,
            List<CharacterGeneratedRoomSnapshot> rooms,
            List<CharacterGeneratedMicrochunkSnapshot> microchunks,
            List<CharacterRoomId> readyRooms,
            Dictionary<long, CharacterMapCellState> generatedCells,
            HashSet<long> placedKeys)
        {
            string subject = "chunk[" + index + "]:"
                + placed.Sector.X + "," + placed.Sector.Y
                + "/" + placed.Chunk.X + "," + placed.Chunk.Y;

            if (placed.Definition == null)
            {
                diagnostics.Add(new CharacterLiveGeneratedMapDiagnostic(
                    CharacterLiveGeneratedMapDiagnosticKind.MissingDefinition, subject));
                return;
            }

            if (!placed.Definition.TileDataComplete)
            {
                diagnostics.Add(new CharacterLiveGeneratedMapDiagnostic(
                    CharacterLiveGeneratedMapDiagnosticKind.IncompleteTileData,
                    subject + " def:" + placed.Definition.Id.Value));
                return;
            }

            if (placed.Definition.WidthTiles != WorldGenConstants.MicroChunkWidthTiles
                || placed.Definition.HeightTiles != WorldGenConstants.MicroChunkHeightTiles)
            {
                diagnostics.Add(new CharacterLiveGeneratedMapDiagnostic(
                    CharacterLiveGeneratedMapDiagnosticKind.DimensionMismatch,
                    subject + " " + placed.Definition.WidthTiles
                    + "x" + placed.Definition.HeightTiles));
                return;
            }

            if (!WorldCoordinateUtility.IsValid(placed.Sector)
                || !WorldCoordinateUtility.IsValid(placed.Chunk))
            {
                diagnostics.Add(new CharacterLiveGeneratedMapDiagnostic(
                    CharacterLiveGeneratedMapDiagnosticKind.ChunkOutsideWorld, subject));
                return;
            }

            WorldTileCoord baseTile = WorldCoordinateUtility.ToWorld(
                placed.Sector, placed.Chunk, new LocalTileCoord(0, 0));
            long placementKey = CharacterLiveMapWorldQueryAdapter.Key(
                baseTile.X, baseTile.Y);

            if (!placedKeys.Add(placementKey))
            {
                diagnostics.Add(new CharacterLiveGeneratedMapDiagnostic(
                    CharacterLiveGeneratedMapDiagnosticKind.DuplicateChunkPlacement,
                    subject));
                return;
            }

            WorldTileCoord maxTile = WorldCoordinateUtility.ToWorld(
                placed.Sector, placed.Chunk,
                new LocalTileCoord(
                    WorldGenConstants.MicroChunkWidthTiles - 1,
                    WorldGenConstants.MicroChunkHeightTiles - 1));

            CharacterRoomId roomId = CharacterRoomId.FromWorldTile(baseTile);
            rooms.Add(new CharacterGeneratedRoomSnapshot(roomId, baseTile, maxTile));
            microchunks.Add(new CharacterGeneratedMicrochunkSnapshot(
                roomId, baseTile, maxTile));
            readyRooms.Add(roomId);

            // 셀 상태: 공용 변환기 적용 → 점유 레이어 → 캐릭터 셀 상태 합성.
            MicrochunkDefinition resolved = MicrochunkTransformer
                .Transform(placed.Definition, placed.Transform).Definition;

            for (int cellIndex = 0; cellIndex < resolved.TileCells.Count; cellIndex++)
            {
                MicrochunkTileCell cell = resolved.TileCells[cellIndex];
                MicrochunkTileLayerOccupancy occupancy =
                    MicrochunkTileLayerOccupancy.FromCell(cell);

                CharacterMapCellState state = CharacterMapCellState.Empty;
                for (int layerIndex = 0;
                    layerIndex < occupancy.OccupiedLayers.Count; layerIndex++)
                {
                    state = state.Combine(CharacterMapCellState.FromTileLayer(
                        occupancy.OccupiedLayers[layerIndex]));
                }

                WorldTileCoord worldTile = WorldCoordinateUtility.ToWorld(
                    placed.Sector, placed.Chunk,
                    new LocalTileCoord(cell.Coordinate.X, cell.Coordinate.Y));

                // 생성된 셀은 빈 상태여도 기록한다(미생성 셀과 구분 유지).
                generatedCells[CharacterLiveMapWorldQueryAdapter.Key(
                    worldTile.X, worldTile.Y)] = state;
            }
        }

        private static CharacterGeneratedMapStartSnapshot ProjectStart(
            CharacterLiveGeneratedMapAdapterInput input,
            List<CharacterGeneratedRoomSnapshot> rooms,
            List<CharacterLiveGeneratedMapDiagnostic> diagnostics)
        {
            if (!input.HasStartCell)
            {
                return new CharacterGeneratedMapStartSnapshot(
                    input.RunId, default, false, default, default, default);
            }

            if (!WorldCoordinateUtility.IsValid(input.StartCell))
            {
                diagnostics.Add(new CharacterLiveGeneratedMapDiagnostic(
                    CharacterLiveGeneratedMapDiagnosticKind.StartCellOutsideWorld,
                    "cell:" + input.StartCell.X + "," + input.StartCell.Y));
                return new CharacterGeneratedMapStartSnapshot(
                    input.RunId, default, false, default, default, default);
            }

            for (int index = 0; index < rooms.Count; index++)
            {
                if (rooms[index].ContainsCell(input.StartCell))
                {
                    return new CharacterGeneratedMapStartSnapshot(
                        input.RunId,
                        rooms[index].RoomId,
                        true,
                        input.StartCell,
                        rooms[index].MinCell,
                        rooms[index].MaxCell);
                }
            }

            // 미배치 방의 시작 셀 — 시작 불성립(런을 미생성 공간에서 시작하지 않는다).
            diagnostics.Add(new CharacterLiveGeneratedMapDiagnostic(
                CharacterLiveGeneratedMapDiagnosticKind.StartRoomNotPlaced,
                "cell:" + input.StartCell.X + "," + input.StartCell.Y));
            return new CharacterGeneratedMapStartSnapshot(
                input.RunId, default, false, default, default, default);
        }
    }
}
