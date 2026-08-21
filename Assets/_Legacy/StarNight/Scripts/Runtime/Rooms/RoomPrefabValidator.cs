#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Rooms
{
    public static class RoomPrefabValidator
    {
        private const int RequiredP4RoomCount = 33;

        public static RoomValidationReport Validate(RoomTemplate2D room)
        {
            RoomValidationReport report = new RoomValidationReport();
            if (room == null)
            {
                report.Add("Room template is null.");
                return report;
            }

            string prefix = $"[{room.RoomId}] ";
            ValidateIdentity(room, report, prefix);
            ValidateFootprint(room, report, prefix);
            ValidateLayers(room, report, prefix);
            ValidateContentRules(room, report, prefix);

            RoomTraversalSnapshot snapshot = RoomTraversalSnapshot.Capture(room);
            ValidateSockets(room, snapshot, report, prefix);
            ValidateSafeCells(room, snapshot, report, prefix);
            ValidateOptionalContentReturns(room, snapshot, report, prefix);
            ValidateMaruEntrySpace(room, snapshot, report, prefix);
            ValidateMovementLayers(room, snapshot, report, prefix);
            ValidateLeafCoreAction(room, snapshot, report, prefix);
            return report;
        }

        public static RoomValidationReport ValidateLibrary(
            IReadOnlyList<RoomTemplate2D> rooms)
        {
            RoomValidationReport report = new RoomValidationReport();
            if (rooms == null)
            {
                report.Add("P4 room library is null.");
                return report;
            }

            if (rooms.Count < RequiredP4RoomCount)
            {
                report.Add(
                    $"P4 requires at least {RequiredP4RoomCount} rooms, found {rooms.Count}.");
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> sentences = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < rooms.Count; index++)
            {
                RoomTemplate2D room = rooms[index];
                RoomValidationReport roomReport = Validate(room);
                report.AddRange(roomReport);
                if (room == null)
                {
                    continue;
                }

                if (!ids.Add(room.RoomId))
                {
                    report.Add($"Duplicate room id: {room.RoomId}.");
                }

                if (!sentences.Add(room.CoreActionSentence))
                {
                    report.Add(
                        $"Core action sentence is duplicated: {room.CoreActionSentence}");
                }
            }

            RoomLibraryCounts counts = CountLibrary(rooms);
            RequireMinimum(
                report,
                "ShopCorridor",
                counts.ShopCorridorRoom,
                1);
            RequireMinimum(
                report,
                "SupplyCorridor",
                counts.SupplyCorridorRoom,
                1);
            RequireMinimum(report, "MaruStatue", counts.MaruRoom, 1);
            RequireDocumentedMinimums(report, counts);
            ValidateSocketCatalogue(rooms, report);
            return report;
        }

        // 문서 22.1/22.2 대비 부족분 리포트. 하드 검증과 분리된 경로이므로
        // 미달이 있어도 빌드를 중단시키지 않는다.
        public static RoomContentQuotaReport BuildContentQuotaReport(
            IReadOnlyList<RoomTemplate2D> rooms)
        {
            RoomLibraryCounts counts = CountLibrary(rooms);
            RoomContentQuotaReport report = new RoomContentQuotaReport();
            IReadOnlyList<RoomContentQuotaRule> rules = RoomContentQuota.Rules;
            for (int index = 0; index < rules.Count; index++)
            {
                RoomContentQuotaRule rule = rules[index];
                report.Add(
                    new RoomContentQuotaEntry(
                        rule,
                        counts.GetCount(rule.Category)));
            }

            return report;
        }

        public static bool TryGetContentShortfalls(
            IReadOnlyList<RoomTemplate2D> rooms,
            out IReadOnlyList<RoomContentQuotaEntry> shortfalls)
        {
            return BuildContentQuotaReport(rooms)
                .TryGetShortfalls(out shortfalls);
        }

        public static bool IsSingleSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text)
                || text.Contains("\n")
                || text.Contains("\r"))
            {
                return false;
            }

            string trimmed = text.Trim();
            char final = trimmed[trimmed.Length - 1];
            if (final != '.' && final != '!' && final != '?')
            {
                return false;
            }

            int terminalCount = 0;
            for (int index = 0; index < trimmed.Length; index++)
            {
                char character = trimmed[index];
                if (character == '.' || character == '!' || character == '?')
                {
                    terminalCount++;
                }
            }

            return terminalCount == 1;
        }

        private static void ValidateIdentity(
            RoomTemplate2D room,
            RoomValidationReport report,
            string prefix)
        {
            if (string.IsNullOrWhiteSpace(room.RoomId))
            {
                report.Add(prefix + "Room id is empty.");
            }

            if (!IsSingleSentence(room.CoreActionSentence))
            {
                report.Add(prefix + "Core action must be exactly one sentence.");
            }

            if (room.Region == RoomRegion.Universal)
            {
                report.Add(prefix + "P4 room must declare a concrete region theme.");
            }
        }

        private static void ValidateFootprint(
            RoomTemplate2D room,
            RoomValidationReport report,
            string prefix)
        {
            Vector2Int expectedMacro = Vector2Int.one;
            Vector2Int maximum = RoomTemplate2D.MacroCellSize;
            switch (room.Archetype)
            {
                case RoomArchetype.Wide:
                    expectedMacro = new Vector2Int(2, 1);
                    maximum = new Vector2Int(24, 8);
                    break;
                case RoomArchetype.Tall:
                    expectedMacro = new Vector2Int(1, 2);
                    maximum = new Vector2Int(12, 16);
                    break;
                case RoomArchetype.Large:
                    expectedMacro = new Vector2Int(2, 2);
                    maximum = new Vector2Int(24, 16);
                    break;
                case RoomArchetype.HorizontalCorridor:
                    maximum = new Vector2Int(
                        Mathf.Max(
                            RoomTemplate2D.MacroCellSize.x,
                            room.MacroSize.x
                                * RoomTemplate2D.MacroCellSize.x),
                        5);
                    if (room.LogicalSize.y < 3 || room.LogicalSize.y > 5)
                    {
                        report.Add(prefix + "Horizontal corridor height must be 3-5 cells.");
                    }
                    break;
                case RoomArchetype.VerticalCorridor:
                    maximum = new Vector2Int(
                        5,
                        Mathf.Max(
                            RoomTemplate2D.MacroCellSize.y,
                            room.MacroSize.y
                                * RoomTemplate2D.MacroCellSize.y));
                    if (room.LogicalSize.x < 3 || room.LogicalSize.x > 5)
                    {
                        report.Add(prefix + "Vertical corridor width must be 3-5 cells.");
                    }
                    break;
            }

            if (room.Archetype != RoomArchetype.HorizontalCorridor
                && room.Archetype != RoomArchetype.VerticalCorridor
                && room.Archetype != RoomArchetype.Boss
                && room.MacroSize != expectedMacro)
            {
                report.Add(
                    prefix + $"Macro size must be {expectedMacro}, found {room.MacroSize}.");
            }

            if (room.LogicalSize.x <= 0
                || room.LogicalSize.y <= 0
                || room.LogicalSize.x > maximum.x
                || room.LogicalSize.y > maximum.y)
            {
                report.Add(
                    prefix + $"Logical size {room.LogicalSize} exceeds {maximum}.");
            }

            if (room.Archetype == RoomArchetype.Small
                && room.ExpectedTraversalSeconds > 10f)
            {
                report.Add(prefix + "Small room traversal target exceeds 10 seconds.");
            }

            if (room.Archetype == RoomArchetype.Large)
            {
                if (room.LandmarkCount != 1)
                {
                    report.Add(prefix + "Large rooms require exactly one landmark.");
                }

                if (room.MovementLayerCount < 2)
                {
                    report.Add(prefix + "Large rooms require at least two movement layers.");
                }
            }
        }

        private static void ValidateLayers(
            RoomTemplate2D room,
            RoomValidationReport report,
            string prefix)
        {
            if (room.Grid == null
                || room.Terrain == null
                || room.OneWay == null
                || room.Fixture == null
                || room.Hazard == null
                || room.Decoration == null
                || room.Logic == null)
            {
                report.Add(prefix + "Terrain/OneWay/Fixture/Hazard/Decoration/Logic layers are required.");
            }

            if (room.Grid != null
                && (Mathf.Abs(room.Grid.cellSize.x - 1f) > 0.001f
                    || Mathf.Abs(room.Grid.cellSize.y - 1f) > 0.001f))
            {
                report.Add(prefix + "Grid cells must be exactly 1x1.");
            }

            ValidateNoTilesOutside(room, room.Terrain, "Terrain", report, prefix);
            ValidateNoTilesOutside(room, room.OneWay, "OneWay", report, prefix);
            ValidateNoTilesOutside(room, room.Fixture, "Fixture", report, prefix);
            ValidateNoTilesOutside(room, room.Hazard, "Hazard", report, prefix);
            ValidateNoTilesOutside(room, room.Decoration, "Decoration", report, prefix);
            ValidateNoTilesOutside(room, room.Logic, "Logic", report, prefix);
        }

        private static void ValidateContentRules(
            RoomTemplate2D room,
            RoomValidationReport report,
            string prefix)
        {
            if (room.IntroducedRuleCount > 1)
            {
                report.Add(prefix + "A room may introduce at most one new rule.");
            }

            if (room.LandmarkCount > 1
                || room.CountSlots(RoomContentSlotKind.Landmark) > 1)
            {
                report.Add(prefix + "A room may contain at most one landmark.");
            }

            if (!room.ExitBlockProof)
            {
                report.Add(prefix + "Exit-block prevention is not declared.");
            }

            if (room.CountSlots(RoomContentSlotKind.SafeCell) == 0)
            {
                report.Add(prefix + "At least one safe cell is required.");
            }

            if (room.CountSlots(RoomContentSlotKind.MaruEntry) == 0)
            {
                report.Add(prefix + "A Maru entry slot is required.");
            }

            bool hasReward = false;
            for (int index = 0; index < room.ContentSlots.Count; index++)
            {
                RoomContentSlot2D slot = room.ContentSlots[index];
                if (slot == null)
                {
                    report.Add(prefix + "Content slot reference is missing.");
                    continue;
                }

                if (slot.Kind == RoomContentSlotKind.OptionalReward
                    || slot.Kind == RoomContentSlotKind.Treasure)
                {
                    hasReward = true;
                    if (!slot.VisibleFromMainRoute)
                    {
                        report.Add(prefix + $"Reward slot {slot.SlotId} is not pre-visible.");
                    }
                }
            }

            if (hasReward && !room.RewardVisibleFromMainRoute)
            {
                report.Add(prefix + "Room reward pre-visibility is not declared.");
            }

            if (room.Archetype == RoomArchetype.HorizontalCorridor)
            {
                RoomTraversalSnapshot snapshot = RoomTraversalSnapshot.Capture(room);
                if (room.HazardBudget != 0
                    || room.EnemyBudget != 0
                    || room.CountSlots(RoomContentSlotKind.Trap) != 0
                    || room.CountSlots(RoomContentSlotKind.Enemy) != 0
                    || (snapshot != null && snapshot.HazardCellCount != 0))
                {
                    report.Add(prefix + "Rest corridors cannot contain automatic hazards or regular enemies.");
                }

                int corridorRoles =
                    (room.HasRole(RoomRole.ShopCorridor) ? 1 : 0)
                    + (room.HasRole(RoomRole.SupplyCorridor) ? 1 : 0);
                if (corridorRoles != 1)
                {
                    report.Add(prefix + "A P4 horizontal corridor must perform exactly one rest role.");
                }
            }
        }

        private static void ValidateSockets(
            RoomTemplate2D room,
            RoomTraversalSnapshot snapshot,
            RoomValidationReport report,
            string prefix)
        {
            if (room.Sockets.Count == 0)
            {
                report.Add(prefix + "At least one socket is required.");
                return;
            }

            if (room.Archetype == RoomArchetype.Small
                && room.Sockets.Count > 2)
            {
                report.Add(prefix + "Small rooms may have only one or two entrances.");
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            List<RoomSocket2D> mainSockets = new List<RoomSocket2D>();
            for (int index = 0; index < room.Sockets.Count; index++)
            {
                RoomSocket2D socket = room.Sockets[index];
                if (socket == null)
                {
                    report.Add(prefix + "Socket reference is missing.");
                    continue;
                }

                if (!ids.Add(socket.SocketId))
                {
                    report.Add(prefix + $"Duplicate socket id: {socket.SocketId}.");
                }

                if (!socket.IsOnBoundary(room.LogicalSize))
                {
                    report.Add(prefix + $"Socket {socket.SocketId} is not on the room boundary.");
                }

                if (!room.CellBounds.Contains(socket.CellOffset))
                {
                    report.Add(prefix + $"Socket {socket.SocketId} is outside room bounds.");
                }

                ValidateSocketOpening(room, socket, snapshot, report, prefix);

                if (socket.MainRouteAllowed)
                {
                    mainSockets.Add(socket);
                    if (!RoomSocketRules.IsMainRouteTraversal(socket.TraversalType))
                    {
                        report.Add(
                            prefix + $"Main socket {socket.SocketId} uses a tool-only traversal.");
                    }
                }

                if (snapshot == null
                    || !snapshot.IsStandable(socket.ValidationAnchor))
                {
                    report.Add(
                        prefix + $"Socket {socket.SocketId} has no standable validation anchor.");
                }
            }

            if (mainSockets.Count == 0)
            {
                report.Add(prefix + "At least one main-route socket is required.");
                return;
            }

            for (int fromIndex = 0; fromIndex < mainSockets.Count; fromIndex++)
            {
                for (int toIndex = 0; toIndex < mainSockets.Count; toIndex++)
                {
                    if (fromIndex == toIndex)
                    {
                        continue;
                    }

                    RoomSocket2D from = mainSockets[fromIndex];
                    RoomSocket2D to = mainSockets[toIndex];
                    if (!RoomTraversalSolver.CanReach(
                            snapshot,
                            from.ValidationAnchor,
                            to.ValidationAnchor))
                    {
                        report.Add(
                            prefix + $"Automatic movement failed: {from.SocketId} -> {to.SocketId}.");
                    }
                }
            }

            RoomSocket2D returnSocket = mainSockets[0];
            for (int index = 0; index < room.Sockets.Count; index++)
            {
                RoomSocket2D socket = room.Sockets[index];
                if (socket == null || socket.MainRouteAllowed)
                {
                    continue;
                }

                if (!RoomTraversalSolver.CanReach(
                        snapshot,
                        returnSocket.ValidationAnchor,
                        socket.ValidationAnchor))
                {
                    report.Add(
                        prefix + $"Optional socket {socket.SocketId} cannot be entered from the main route.");
                }

                if (!RoomTraversalSolver.CanReach(
                        snapshot,
                        socket.ValidationAnchor,
                        returnSocket.ValidationAnchor))
                {
                    report.Add(
                        prefix + $"Optional socket {socket.SocketId} cannot return to the main route.");
                }
            }
        }

        private static void ValidateSafeCells(
            RoomTemplate2D room,
            RoomTraversalSnapshot snapshot,
            RoomValidationReport report,
            string prefix)
        {
            if (snapshot == null || room.Sockets.Count == 0)
            {
                return;
            }

            RoomSocket2D source = null;
            for (int index = 0; index < room.Sockets.Count; index++)
            {
                if (room.Sockets[index] != null
                    && room.Sockets[index].MainRouteAllowed)
                {
                    source = room.Sockets[index];
                    break;
                }
            }

            if (source == null)
            {
                return;
            }

            foreach (RoomContentSlot2D safeCell in
                     room.EnumerateSlots(RoomContentSlotKind.SafeCell))
            {
                if (!snapshot.IsStandable(safeCell.CellOffset))
                {
                    report.Add(prefix + $"Safe cell {safeCell.SlotId} is not standable.");
                }
                else if (!RoomTraversalSolver.CanReach(
                             snapshot,
                             source.ValidationAnchor,
                             safeCell.CellOffset))
                {
                    report.Add(prefix + $"Safe cell {safeCell.SlotId} is unreachable.");
                }
            }
        }

        private static void ValidateOptionalContentReturns(
            RoomTemplate2D room,
            RoomTraversalSnapshot snapshot,
            RoomValidationReport report,
            string prefix)
        {
            RoomSocket2D source = FindFirstMainSocket(room);
            if (snapshot == null || source == null)
            {
                return;
            }

            for (int index = 0; index < room.ContentSlots.Count; index++)
            {
                RoomContentSlot2D slot = room.ContentSlots[index];
                if (slot == null
                    || (slot.Kind != RoomContentSlotKind.OptionalReward
                        && slot.Kind != RoomContentSlotKind.Treasure))
                {
                    continue;
                }

                bool standable = snapshot.IsStandable(slot.CellOffset);
                bool canEnter = standable
                    && RoomTraversalSolver.CanReach(
                        snapshot,
                        source.ValidationAnchor,
                        slot.CellOffset);
                bool canReturn = standable
                    && RoomTraversalSolver.CanReach(
                        snapshot,
                        slot.CellOffset,
                        source.ValidationAnchor);
                if (!standable || !canEnter || !canReturn)
                {
                    report.Add(
                        prefix + $"Optional reward {slot.SlotId} round-trip failed "
                        + $"(standable={standable}, enter={canEnter}, return={canReturn}).");
                }
            }
        }

        private static void ValidateMaruEntrySpace(
            RoomTemplate2D room,
            RoomTraversalSnapshot snapshot,
            RoomValidationReport report,
            string prefix)
        {
            if (snapshot == null)
            {
                return;
            }

            foreach (RoomContentSlot2D entry in
                     room.EnumerateSlots(RoomContentSlotKind.MaruEntry))
            {
                int clearStandableCells = 0;
                for (int deltaX = -1; deltaX <= 1; deltaX++)
                {
                    Vector2Int cell =
                        entry.CellOffset + new Vector2Int(deltaX, 0);
                    if (snapshot.IsStandable(cell))
                    {
                        clearStandableCells++;
                    }
                }

                if (clearStandableCells < 2)
                {
                    report.Add(
                        prefix + $"Maru entry {entry.SlotId} lacks a clear approach lane.");
                }
            }
        }

        private static void ValidateMovementLayers(
            RoomTemplate2D room,
            RoomTraversalSnapshot snapshot,
            RoomValidationReport report,
            string prefix)
        {
            if (room.Archetype == RoomArchetype.Large
                && (snapshot == null || snapshot.CountStandableLayers() < 2))
            {
                report.Add(prefix + "Large room geometry has fewer than two movement layers.");
            }
        }

        private static void ValidateLeafCoreAction(
            RoomTemplate2D room,
            RoomTraversalSnapshot snapshot,
            RoomValidationReport report,
            string prefix)
        {
            RoomContentSlotKind? requiredKind = null;
            if (room.HasRole(RoomRole.RecordRoom))
            {
                requiredKind = RoomContentSlotKind.RecordGuest;
            }
            else if (room.HasRole(RoomRole.MaruStatue))
            {
                requiredKind = RoomContentSlotKind.MaruStatue;
            }

            // 문서 7.1은 귀향상 방에 최소 출구 2개를 허용하므로 소켓 상한만 역할별로 다르다.
            int maximumLeafSockets =
                room.HasRole(RoomRole.MaruStatue) ? 2 : 1;
            if (!requiredKind.HasValue
                || room.Sockets.Count == 0
                || room.Sockets.Count > maximumLeafSockets)
            {
                return;
            }

            int targetCount = 0;
            foreach (RoomContentSlot2D target in
                     room.EnumerateSlots(requiredKind.Value))
            {
                targetCount++;
                for (int index = 0; index < room.Sockets.Count; index++)
                {
                    RoomSocket2D socket = room.Sockets[index];
                    if (socket == null)
                    {
                        continue;
                    }

                    if (snapshot == null
                        || !snapshot.IsStandable(target.CellOffset)
                        || !RoomTraversalSolver.CanReach(
                            snapshot,
                            socket.ValidationAnchor,
                            target.CellOffset)
                        || !RoomTraversalSolver.CanReach(
                            snapshot,
                            target.CellOffset,
                            socket.ValidationAnchor))
                    {
                        report.Add(
                            prefix + $"Leaf core slot {target.SlotId} does not round-trip to socket {socket.SocketId}.");
                    }
                }
            }

            if (targetCount != 1)
            {
                report.Add(
                    prefix + $"Leaf room requires exactly one {requiredKind.Value} slot.");
            }
        }

        private static void ValidateSocketOpening(
            RoomTemplate2D room,
            RoomSocket2D socket,
            RoomTraversalSnapshot snapshot,
            RoomValidationReport report,
            string prefix)
        {
            for (int x = 0; x < socket.OpeningSize.x; x++)
            {
                for (int y = 0; y < socket.OpeningSize.y; y++)
                {
                    Vector2Int cell =
                        socket.CellOffset + new Vector2Int(x, y);
                    if (!room.CellBounds.Contains(cell))
                    {
                        report.Add(
                            prefix + $"Socket {socket.SocketId} opening exceeds room bounds.");
                    }
                    else if (snapshot != null && snapshot.IsTerrain(cell))
                    {
                        report.Add(
                            prefix + $"Socket {socket.SocketId} opening is blocked by Terrain.");
                    }
                }
            }
        }

        private static void ValidateNoTilesOutside(
            RoomTemplate2D room,
            UnityEngine.Tilemaps.Tilemap tilemap,
            string layerName,
            RoomValidationReport report,
            string prefix)
        {
            if (tilemap == null)
            {
                return;
            }

            BoundsInt bounds = tilemap.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (tilemap.HasTile(cell)
                    && !room.CellBounds.Contains(new Vector2Int(cell.x, cell.y)))
                {
                    report.Add(
                        prefix + $"{layerName} has a tile outside logical bounds at ({cell.x},{cell.y}).");
                }
            }
        }

        private static void ValidateSocketCatalogue(
            IReadOnlyList<RoomTemplate2D> rooms,
            RoomValidationReport report)
        {
            for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
            {
                RoomTemplate2D room = rooms[roomIndex];
                if (room == null)
                {
                    continue;
                }

                for (int socketIndex = 0;
                     socketIndex < room.Sockets.Count;
                     socketIndex++)
                {
                    RoomSocket2D socket = room.Sockets[socketIndex];
                    if (socket == null)
                    {
                        continue;
                    }

                    if (socket.TraversalType == RoomTraversalType.ExitOnly)
                    {
                        continue;
                    }

                    bool found = false;
                    for (int candidateRoomIndex = 0;
                         candidateRoomIndex < rooms.Count && !found;
                         candidateRoomIndex++)
                    {
                        if (candidateRoomIndex == roomIndex
                            || rooms[candidateRoomIndex] == null)
                        {
                            continue;
                        }

                        RoomTemplate2D candidateRoom = rooms[candidateRoomIndex];
                        for (int candidateSocketIndex = 0;
                             candidateSocketIndex < candidateRoom.Sockets.Count;
                             candidateSocketIndex++)
                        {
                            if (RoomSocketRules.AreCompatible(
                                    socket,
                                    candidateRoom.Sockets[candidateSocketIndex]))
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        report.Add(
                            $"[{room.RoomId}] Socket {socket.SocketId} has no compatible catalogue partner.");
                    }
                }
            }
        }

        private static RoomSocket2D FindFirstMainSocket(RoomTemplate2D room)
        {
            for (int index = 0; index < room.Sockets.Count; index++)
            {
                RoomSocket2D socket = room.Sockets[index];
                if (socket != null && socket.MainRouteAllowed)
                {
                    return socket;
                }
            }

            return null;
        }

        private static void RequireMinimum(
            RoomValidationReport report,
            string label,
            int actual,
            int required)
        {
            if (actual < required)
            {
                report.Add(
                    $"P4 requires {required} {label} rooms, found {actual}.");
            }
        }

        private static void RequireDocumentedMinimums(
            RoomValidationReport report,
            RoomLibraryCounts counts)
        {
            IReadOnlyList<RoomContentQuotaRule> rules = RoomContentQuota.Rules;
            for (int index = 0; index < rules.Count; index++)
            {
                RoomContentQuotaRule rule = rules[index];
                if (!rule.LibraryHardRequirement
                    || !rule.RoomLibraryMeasurable)
                {
                    continue;
                }

                int actual = counts.GetCount(rule.Category);
                if (actual < rule.Minimum)
                {
                    report.Add(
                        $"Document 22.1 requires at least {rule.Minimum} "
                        + $"'{rule.DisplayName}' per region, found {actual}.");
                }
            }
        }

        private static RoomLibraryCounts CountLibrary(
            IReadOnlyList<RoomTemplate2D> rooms)
        {
            RoomLibraryCounts counts = new RoomLibraryCounts();
            if (rooms == null)
            {
                return counts;
            }

            for (int index = 0; index < rooms.Count; index++)
            {
                RoomTemplate2D room = rooms[index];
                if (room == null)
                {
                    continue;
                }

                counts.Total++;
                bool isShop = room.HasRole(RoomRole.ShopCorridor);
                bool isSupply = room.HasRole(RoomRole.SupplyCorridor);
                bool isRecord = room.HasRole(RoomRole.RecordRoom)
                    || room.CountSlots(RoomContentSlotKind.RecordGuest) > 0;
                bool isMaru = room.HasRole(RoomRole.MaruStatue)
                    || room.CountSlots(RoomContentSlotKind.MaruStatue) > 0;
                bool isSpecial = isShop || isSupply || isRecord || isMaru;

                counts.ShopCorridorRoom += isShop ? 1 : 0;
                counts.SupplyCorridorRoom += isSupply ? 1 : 0;
                counts.RestCorridorRoom += isShop || isSupply ? 1 : 0;
                counts.RecordRoom += isRecord ? 1 : 0;
                counts.MaruRoom += isMaru ? 1 : 0;

                if (room.HasRole(RoomRole.LocalEvent)
                    || room.CountSlots(RoomContentSlotKind.LocalEvent) > 0)
                {
                    counts.LocalEventRoom++;
                }

                if (room.HasRole(RoomRole.Landmark)
                    || room.LandmarkCount > 0
                    || room.CountSlots(RoomContentSlotKind.Landmark) > 0)
                {
                    counts.LandmarkRoom++;
                }

                if (HasOptionalSocket(room))
                {
                    counts.SecretRoom++;
                }

                bool isCorridor =
                    room.Archetype == RoomArchetype.HorizontalCorridor
                    || room.Archetype == RoomArchetype.VerticalCorridor;
                if (!isCorridor
                    && (room.MacroSize.x
                            >= RoomContentQuota.LongHallDeepShaftMacroExtent
                        || room.MacroSize.y
                            >= RoomContentQuota.LongHallDeepShaftMacroExtent))
                {
                    counts.LongHallDeepShaftRoom++;
                }

                if (isSpecial)
                {
                    continue;
                }

                switch (room.Archetype)
                {
                    case RoomArchetype.Small:
                    case RoomArchetype.Standard:
                        counts.MicroStandardRoom++;
                        break;
                    case RoomArchetype.Wide:
                        counts.WideRoom++;
                        break;
                    case RoomArchetype.Tall:
                        counts.TallRoom++;
                        break;
                    case RoomArchetype.Large:
                        counts.LargeRoom++;
                        break;
                }
            }

            return counts;
        }

        private static bool HasOptionalSocket(RoomTemplate2D room)
        {
            for (int index = 0; index < room.Sockets.Count; index++)
            {
                RoomSocket2D socket = room.Sockets[index];
                if (socket != null && !socket.MainRouteAllowed)
                {
                    return true;
                }
            }

            return false;
        }

        private struct RoomLibraryCounts
        {
            public int MicroStandardRoom;
            public int WideRoom;
            public int TallRoom;
            public int LargeRoom;
            public int LongHallDeepShaftRoom;
            public int RestCorridorRoom;
            public int ShopCorridorRoom;
            public int SupplyCorridorRoom;
            public int LocalEventRoom;
            public int MaruRoom;
            public int RecordRoom;
            public int LandmarkRoom;
            public int SecretRoom;
            public int Total;

            public int GetCount(RoomContentQuotaCategory category)
            {
                switch (category)
                {
                    case RoomContentQuotaCategory.MicroStandardRoom:
                        return MicroStandardRoom;
                    case RoomContentQuotaCategory.WideRoom:
                        return WideRoom;
                    case RoomContentQuotaCategory.TallRoom:
                        return TallRoom;
                    case RoomContentQuotaCategory.LargeRoom:
                        return LargeRoom;
                    case RoomContentQuotaCategory.LongHallDeepShaftRoom:
                        return LongHallDeepShaftRoom;
                    case RoomContentQuotaCategory.RestCorridorRoom:
                        return RestCorridorRoom;
                    case RoomContentQuotaCategory.LocalEventRoom:
                        return LocalEventRoom;
                    case RoomContentQuotaCategory.MaruRoom:
                        return MaruRoom;
                    case RoomContentQuotaCategory.RecordRoom:
                        return RecordRoom;
                    case RoomContentQuotaCategory.LandmarkRoom:
                        return LandmarkRoom;
                    case RoomContentQuotaCategory.SecretRoom:
                        return SecretRoom;
                    case RoomContentQuotaCategory.RegionRoomTotal:
                        return Total;
                    default:
                        return 0;
                }
            }
        }
    }
}

#endif
