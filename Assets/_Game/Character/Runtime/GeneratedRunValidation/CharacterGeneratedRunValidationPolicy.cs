using System.Collections.Generic;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.GeneratedRunValidation
{
    /// <summary>
    /// 생성 런 검증(순수·결정적). 방/마이크로청크 구조, 루트 참조, 아이템
    /// 배치, 장비 어포던스를 검사하고 전환/스폰 요청 생성은 CHAR06_01 통합
    /// 정책에 위임한다. 어떤 상태도 변조하지 않고 진단만 만든다.
    /// </summary>
    public static class CharacterGeneratedRunValidationPolicy
    {
        public static CharacterGeneratedRunValidationResult Validate(
            CharacterGeneratedRunSnapshot snapshot,
            int actorId,
            in CharacterRunInventoryState inventory,
            ICharacterRoomReadinessSource readinessSource)
        {
            var diagnostics = new List<CharacterGeneratedRunValidationDiagnostic>();
            int spawnCount = 0;
            int routeCount = 0;

            if (snapshot != null)
            {
                ValidateRooms(snapshot, diagnostics);
                ValidateMicrochunks(snapshot, diagnostics);
                ValidateRouteStructure(snapshot, diagnostics);
                ValidateItems(snapshot, diagnostics);

                // 전환/스폰 요청 생성과 역량/준비 게이트는 CHAR06_01 위임.
                var spawnRequests = new List<CharacterPlayerSpawnRequest>();
                var routeRequests = new List<CharacterGeneratedRouteTransitionRequest>();
                var integrationDiagnostics = new List<CharacterIntegrationDiagnostic>();

                var start = snapshot.Start;
                CharacterIntegrationBatchPolicy.BuildBatch(
                    in start, actorId, snapshot.Routes, in inventory,
                    readinessSource, spawnRequests, routeRequests,
                    integrationDiagnostics);

                spawnCount = spawnRequests.Count;
                routeCount = routeRequests.Count;

                for (int index = 0; index < integrationDiagnostics.Count; index++)
                {
                    var integration = integrationDiagnostics[index];
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.IntegrationRejected,
                        integration.Kind + " " + integration.Subject));
                }
            }

            int runId = snapshot != null ? snapshot.RunId : 0;
            int seed = snapshot != null ? snapshot.Seed : 0;

            return new CharacterGeneratedRunValidationResult(
                runId,
                seed,
                spawnCount,
                routeCount,
                diagnostics,
                ComputeDigest(runId, seed, spawnCount, routeCount, diagnostics));
        }

        private static void ValidateRooms(
            CharacterGeneratedRunSnapshot snapshot,
            List<CharacterGeneratedRunValidationDiagnostic> diagnostics)
        {
            for (int index = 0; index < snapshot.Rooms.Count; index++)
            {
                var room = snapshot.Rooms[index];

                // 방 ID 유일성.
                for (int other = 0; other < index; other++)
                {
                    if (snapshot.Rooms[other].RoomId.Equals(room.RoomId))
                    {
                        diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                            CharacterGeneratedRunValidationDiagnosticKind.DuplicateRoomId,
                            RoomSubject(room.RoomId)));
                        break;
                    }
                }

                // 방 경계는 월드 안의 정방향 사각형이어야 한다.
                if (!WorldCoordinateUtility.IsValid(room.MinCell)
                    || !WorldCoordinateUtility.IsValid(room.MaxCell)
                    || room.MinCell.X > room.MaxCell.X
                    || room.MinCell.Y > room.MaxCell.Y)
                {
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.RoomOutsideWorldBounds,
                        RoomSubject(room.RoomId)));
                }
            }
        }

        private static void ValidateMicrochunks(
            CharacterGeneratedRunSnapshot snapshot,
            List<CharacterGeneratedRunValidationDiagnostic> diagnostics)
        {
            for (int index = 0; index < snapshot.Microchunks.Count; index++)
            {
                var chunk = snapshot.Microchunks[index];
                string subject = "chunk:" + chunk.MinCell.X + "," + chunk.MinCell.Y;

                // MAP 마이크로청크 규격(12×8)과 격자 정렬.
                int width = chunk.MaxCell.X - chunk.MinCell.X + 1;
                int height = chunk.MaxCell.Y - chunk.MinCell.Y + 1;
                if (width != WorldGenConstants.MicroChunkWidthTiles
                    || height != WorldGenConstants.MicroChunkHeightTiles
                    || chunk.MinCell.X % WorldGenConstants.MicroChunkWidthTiles != 0
                    || chunk.MinCell.Y % WorldGenConstants.MicroChunkHeightTiles != 0)
                {
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.MicrochunkMisaligned,
                        subject));
                }

                // 소유 방 존재 + 방 경계 내 포함.
                CharacterGeneratedRoomSnapshot ownerRoom;
                if (!TryFindRoom(snapshot, chunk.OwnerRoomId, out ownerRoom))
                {
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.MicrochunkOwnerRoomMissing,
                        subject));
                }
                else if (!ownerRoom.ContainsCell(chunk.MinCell)
                    || !ownerRoom.ContainsCell(chunk.MaxCell))
                {
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.MicrochunkOutsideOwnerRoom,
                        subject));
                }

                // 같은 방 안 중복 점유 거부.
                for (int other = 0; other < index; other++)
                {
                    var previous = snapshot.Microchunks[other];
                    if (previous.OwnerRoomId.Equals(chunk.OwnerRoomId)
                        && previous.MinCell.Equals(chunk.MinCell))
                    {
                        diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                            CharacterGeneratedRunValidationDiagnosticKind.DuplicateMicrochunkOccupancy,
                            subject));
                        break;
                    }
                }
            }
        }

        private static void ValidateRouteStructure(
            CharacterGeneratedRunSnapshot snapshot,
            List<CharacterGeneratedRunValidationDiagnostic> diagnostics)
        {
            for (int index = 0; index < snapshot.Routes.Count; index++)
            {
                var route = snapshot.Routes[index];
                string subject = "route:" + route.RouteId;

                CharacterGeneratedRoomSnapshot sourceRoom;
                CharacterGeneratedRoomSnapshot targetRoom;
                bool sourceExists = TryFindRoom(snapshot, route.SourceRoom, out sourceRoom);
                bool targetExists = TryFindRoom(snapshot, route.TargetRoom, out targetRoom);

                if (!sourceExists || !targetExists)
                {
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.RouteRoomMissing,
                        subject));
                    continue;
                }

                if (!sourceRoom.ContainsCell(route.SourceExitCell)
                    || !targetRoom.ContainsCell(route.TargetEntryCell))
                {
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.RouteCellOutsideDeclaredRoom,
                        subject));
                }
            }
        }

        private static void ValidateItems(
            CharacterGeneratedRunSnapshot snapshot,
            List<CharacterGeneratedRunValidationDiagnostic> diagnostics)
        {
            for (int index = 0; index < snapshot.ItemPlacements.Count; index++)
            {
                var item = snapshot.ItemPlacements[index];
                string subject = "item:" + item.ItemId
                    + " " + RoomSubject(item.RoomId)
                    + " cell:" + item.Cell.X + "," + item.Cell.Y;

                CharacterGeneratedRoomSnapshot room;
                if (!TryFindRoom(snapshot, item.RoomId, out room))
                {
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.ItemRoomMissing,
                        subject));
                    continue;
                }

                if (!WorldCoordinateUtility.IsValid(item.Cell)
                    || !room.ContainsCell(item.Cell))
                {
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.ItemOutsideRoomOrWorld,
                        subject));
                    continue;
                }

                if (IsReservedCell(snapshot, item.Cell))
                {
                    diagnostics.Add(new CharacterGeneratedRunValidationDiagnostic(
                        CharacterGeneratedRunValidationDiagnosticKind.ItemOnReservedCell,
                        subject));
                }
            }
        }

        /// <summary>스폰 셀·루트 이탈/진입 셀·명시 금지 셀은 아이템 배치 불가.</summary>
        private static bool IsReservedCell(
            CharacterGeneratedRunSnapshot snapshot,
            WorldTileCoord cell)
        {
            if (snapshot.Start.HasStartCell && snapshot.Start.StartCell.Equals(cell))
            {
                return true;
            }

            for (int index = 0; index < snapshot.Routes.Count; index++)
            {
                if (snapshot.Routes[index].SourceExitCell.Equals(cell)
                    || snapshot.Routes[index].TargetEntryCell.Equals(cell))
                {
                    return true;
                }
            }

            for (int index = 0; index < snapshot.BlockedValidationCells.Count; index++)
            {
                if (snapshot.BlockedValidationCells[index].Equals(cell))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindRoom(
            CharacterGeneratedRunSnapshot snapshot,
            CharacterRoomId roomId,
            out CharacterGeneratedRoomSnapshot room)
        {
            for (int index = 0; index < snapshot.Rooms.Count; index++)
            {
                if (snapshot.Rooms[index].RoomId.Equals(roomId))
                {
                    room = snapshot.Rooms[index];
                    return true;
                }
            }

            room = default;
            return false;
        }

        private static string RoomSubject(CharacterRoomId roomId)
        {
            return "room:" + roomId.Sector.X + "," + roomId.Sector.Y
                + "/" + roomId.MicroChunk.X + "," + roomId.MicroChunk.Y;
        }

        /// <summary>FNV-1a 기반 결정적 다이제스트 — 같은 입력이면 항상 같다.</summary>
        private static string ComputeDigest(
            int runId,
            int seed,
            int spawnCount,
            int routeCount,
            List<CharacterGeneratedRunValidationDiagnostic> diagnostics)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261u;
                const uint prime = 16777619u;

                uint hash = offsetBasis;
                hash = (hash ^ (uint)runId) * prime;
                hash = (hash ^ (uint)seed) * prime;
                hash = (hash ^ (uint)spawnCount) * prime;
                hash = (hash ^ (uint)routeCount) * prime;

                for (int index = 0; index < diagnostics.Count; index++)
                {
                    hash = (hash ^ (uint)diagnostics[index].Kind) * prime;
                    string subject = diagnostics[index].Subject ?? string.Empty;
                    for (int character = 0; character < subject.Length; character++)
                    {
                        hash = (hash ^ subject[character]) * prime;
                    }
                }

                return hash.ToString("x8") + "-d" + diagnostics.Count
                    + "-s" + spawnCount + "-r" + routeCount;
            }
        }
    }
}
