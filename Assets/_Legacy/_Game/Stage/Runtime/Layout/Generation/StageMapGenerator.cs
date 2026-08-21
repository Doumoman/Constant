#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    public static class StageMapGenerator
    {
        private const int HorizontalStride = 42;
        private const int VerticalStride = 30;

        public static StageGeneratedLayout Generate(
            StageMapProfile profile,
            IReadOnlyList<RoomTemplate> sourceTemplates,
            int seed,
            int rerollNonce = 0,
            IReadOnlyDictionary<string, StageLockedRoom> lockedRooms = null)
        {
            var result = new StageGeneratedLayout
            {
                StageId = profile != null ? profile.StageId : string.Empty,
                Seed = seed,
                RerollNonce = rerollNonce,
            };
            if (profile == null || sourceTemplates == null)
            {
                result.ErrorCount = 1;
                result.ValidationHash = "INVALID_PROFILE";
                return result;
            }

            List<RoomTemplate> templates = sourceTemplates
                .Where(template => template != null && template.SizeCells.x > 0 && template.SizeCells.y > 0)
                .OrderBy(template => template.RoomId, StringComparer.Ordinal)
                .ToList();
            if (templates.Count == 0)
            {
                result.ErrorCount = 1;
                result.ValidationHash = "NO_TEMPLATES";
                return result;
            }

            var random = new StableRandom(CombineSeed(seed, rerollNonce));
            result.Family = ChooseFamily(profile, ref random);
            int minRooms = Mathf.Max(4, profile.MinRooms);
            int maxRooms = Mathf.Max(minRooms, profile.MaxRooms);
            int roomCount = random.RangeInclusive(minRooms, maxRooms);
            int branchMin = Mathf.Max(0, profile.BranchCountRange.x);
            int branchMax = Mathf.Max(branchMin, profile.BranchCountRange.y);
            int branchTarget = Mathf.Clamp(random.RangeInclusive(branchMin, branchMax), 0, Mathf.Max(0, roomCount - 4));
            if (profile.GuaranteedEvents != null && profile.GuaranteedEvents.Count > 0)
                branchTarget = Mathf.Clamp(Mathf.Max(2, branchTarget), 0, Mathf.Max(0, roomCount - 4));
            int effectiveBranchMin = profile.GuaranteedEvents != null && profile.GuaranteedEvents.Count > 0
                ? Mathf.Max(2, branchMin)
                : branchMin;
            int mainMin = Mathf.Clamp(profile.MainRouteLengthRange.x, 4, Mathf.Max(4, roomCount - effectiveBranchMin));
            int mainMax = Mathf.Clamp(profile.MainRouteLengthRange.y, mainMin, Mathf.Max(mainMin, roomCount - effectiveBranchMin));
            int mainCount = Mathf.Clamp(roomCount - branchTarget, mainMin, mainMax);
            mainCount = Mathf.Clamp(mainCount, roomCount - branchMax, roomCount - effectiveBranchMin);

            RoomTemplate startTemplate = FindRoleTemplate(templates, RoomRole.Start) ?? templates[0];
            RoomTemplate exitTemplate = FindRoleTemplate(templates, RoomRole.Exit) ?? templates[templates.Count - 1];
            for (int index = 0; index < roomCount; index++)
            {
                bool isMain = index < mainCount;
                RoomRole role = ResolveRole(index, mainCount, isMain, profile, ref random);
                RoomTemplate template = index == 0
                    ? startTemplate
                    : index == mainCount - 1
                        ? exitTemplate
                        : ChooseTemplate(templates, profile.SizeWeights, index, ref random);
                var room = new StageGeneratedRoom
                {
                    NodeGuid = CreateNodeGuid(profile.StageId, seed, index),
                    Template = template,
                    PositionCells = isMain
                        ? GetMainPosition(result.Family, index, mainCount)
                        : GetBranchPosition(result.Rooms, index - mainCount, mainCount, template.SizeCells, ref random),
                    Role = role,
                    MainRoute = isMain,
                };
                if (lockedRooms != null && lockedRooms.TryGetValue(room.NodeGuid, out StageLockedRoom locked) && locked != null)
                {
                    room.Template = locked.Template != null ? locked.Template : room.Template;
                    room.PositionCells = locked.PositionCells;
                    room.Role = locked.Role;
                    room.MainRoute = locked.MainRoute;
                    room.Locked = true;
                }
                result.Rooms.Add(room);
            }

            ResolveOverlaps(result.Rooms, ref random);
            CreateMainRouteConnections(result);
            CreateBranchConnections(result, mainCount);
            CreateLoopConnections(result, profile, ref random);
            FillElementSlots(result, profile, ref random);
            ValidateResult(result);
            result.EstimatedRoomMoves = Mathf.Max(0, result.Rooms.Count - 1);
            result.ValidationHash = CreateValidationHash(result);
            return result;
        }

        private static LayoutFamily ChooseFamily(StageMapProfile profile, ref StableRandom random)
        {
            if (profile.AllowedFamilies == null || profile.AllowedFamilies.Count == 0)
                return LayoutFamily.LinearBend;
            return profile.AllowedFamilies[random.Range(0, profile.AllowedFamilies.Count)];
        }

        private static RoomTemplate FindRoleTemplate(IReadOnlyList<RoomTemplate> templates, RoomRole role)
        {
            for (int index = 0; index < templates.Count; index++)
                if (templates[index].Role == role) return templates[index];
            return null;
        }

        private static RoomTemplate ChooseTemplate(
            IReadOnlyList<RoomTemplate> templates,
            RoomSizeWeights weights,
            int roomIndex,
            ref StableRandom random)
        {
            Vector2Int[] guaranteedSizes =
            {
                RoomSizeCatalog.Micro,
                RoomSizeCatalog.Wide,
                RoomSizeCatalog.Tall,
                RoomSizeCatalog.Large,
            };
            if (roomIndex < guaranteedSizes.Length)
            {
                for (int index = 0; index < templates.Count; index++)
                    if (templates[index].SizeCells == guaranteedSizes[roomIndex]) return templates[index];
            }

            int totalWeight = 0;
            for (int index = 0; index < templates.Count; index++)
                totalWeight += Mathf.Max(0, weights != null ? weights.GetWeight(templates[index].SizeCells) : 1);
            if (totalWeight <= 0) return templates[random.Range(0, templates.Count)];
            int roll = random.Range(0, totalWeight);
            for (int index = 0; index < templates.Count; index++)
            {
                roll -= Mathf.Max(0, weights != null ? weights.GetWeight(templates[index].SizeCells) : 1);
                if (roll < 0) return templates[index];
            }
            return templates[templates.Count - 1];
        }

        private static RoomRole ResolveRole(int index, int mainCount, bool isMain, StageMapProfile profile, ref StableRandom random)
        {
            if (index == 0) return RoomRole.Start;
            if (index == mainCount - 1) return RoomRole.Exit;
            if (isMain) return RoomRole.Main;
            int branchIndex = index - mainCount;
            if (branchIndex == 0) return RoomRole.Branch;
            if (branchIndex == 1 && profile.GuaranteedEvents != null && profile.GuaranteedEvents.Count > 0)
                return profile.GuaranteedEvents[0].TargetRole;
            int roll = random.Range(0, 10);
            return roll == 0 ? RoomRole.Secret : roll < 3 ? RoomRole.Rest : RoomRole.Branch;
        }

        private static Vector2Int GetMainPosition(LayoutFamily family, int index, int count)
        {
            switch (family)
            {
                case LayoutFamily.VerticalSpine:
                    return new Vector2Int((index % 2) * 14, index * VerticalStride);
                case LayoutFamily.TwinBranchMerge:
                    return new Vector2Int(index * HorizontalStride, index > 0 && index < count - 1 ? (index % 2 == 0 ? -16 : 16) : 0);
                case LayoutFamily.BrokenSpiral:
                    Vector2Int spiral = GetSpiralCoordinate(index);
                    return new Vector2Int(spiral.x * HorizontalStride, spiral.y * VerticalStride);
                case LayoutFamily.HubAndSpokes:
                    Vector2Int spoke = GetSpokeCoordinate(index);
                    return new Vector2Int(spoke.x * HorizontalStride, spoke.y * VerticalStride);
                default:
                    int bend = Mathf.Max(1, count / 2);
                    return new Vector2Int(index * HorizontalStride, index > bend ? VerticalStride : 0);
            }
        }

        private static Vector2Int GetBranchPosition(
            IReadOnlyList<StageGeneratedRoom> rooms,
            int branchIndex,
            int mainCount,
            Vector2Int size,
            ref StableRandom random)
        {
            int parentIndex = mainCount <= 2 ? 0 : 1 + branchIndex % (mainCount - 2);
            Vector2Int parent = rooms[parentIndex].PositionCells;
            int direction = branchIndex % 2 == 0 ? 1 : -1;
            Vector2Int desired = new Vector2Int(parent.x, parent.y + direction * VerticalStride);
            return FindFreePosition(desired, size, rooms, ref random);
        }

        private static void ResolveOverlaps(IList<StageGeneratedRoom> rooms, ref StableRandom random)
        {
            var accepted = new List<StageGeneratedRoom>();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int index = 0; index < rooms.Count; index++)
                {
                    StageGeneratedRoom room = rooms[index];
                    if ((pass == 0) != room.Locked) continue;
                    if (!room.Locked)
                        room.PositionCells = FindFreePosition(room.PositionCells, room.Template.SizeCells, accepted, ref random);
                    accepted.Add(room);
                }
            }
        }

        private static Vector2Int FindFreePosition(
            Vector2Int desired,
            Vector2Int size,
            IReadOnlyList<StageGeneratedRoom> rooms,
            ref StableRandom random)
        {
            desired = StageLayoutGraphUtility.SnapToPlacementGrid(desired);
            for (int ring = 0; ring < 20; ring++)
            {
                int phase = ring == 0 ? 0 : random.Range(0, 4);
                for (int direction = 0; direction < 4; direction++)
                {
                    int ordered = (direction + phase) % 4;
                    Vector2Int offset = ordered == 0 ? new Vector2Int(ring * HorizontalStride, 0) :
                        ordered == 1 ? new Vector2Int(-ring * HorizontalStride, 0) :
                        ordered == 2 ? new Vector2Int(0, ring * VerticalStride) :
                        new Vector2Int(0, -ring * VerticalStride);
                    Vector2Int candidate = StageLayoutGraphUtility.SnapToPlacementGrid(desired + offset);
                    bool overlaps = false;
                    for (int index = 0; index < rooms.Count; index++)
                    {
                        StageGeneratedRoom existing = rooms[index];
                        if (existing.Template != null && StageLayoutGraphUtility.RoomsOverlap(candidate, size, existing.PositionCells, existing.Template.SizeCells))
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (!overlaps) return candidate;
                }
            }
            return desired + new Vector2Int(rooms.Count * HorizontalStride, 0);
        }

        private static void CreateMainRouteConnections(StageGeneratedLayout result)
        {
            List<StageGeneratedRoom> mainRooms = result.Rooms.Where(room => room.MainRoute).ToList();
            for (int index = 0; index < mainRooms.Count - 1; index++)
                TryAddConnection(result, mainRooms[index], mainRooms[index + 1], GeneratedRouteKind.MainRoute, index);
        }

        private static void CreateBranchConnections(StageGeneratedLayout result, int mainCount)
        {
            for (int index = mainCount; index < result.Rooms.Count; index++)
            {
                int parentIndex = mainCount <= 2 ? 0 : 1 + (index - mainCount) % (mainCount - 2);
                GeneratedRouteKind kind = result.Rooms[index].Role == RoomRole.Secret ? GeneratedRouteKind.Secret : GeneratedRouteKind.Branch;
                TryAddConnection(result, result.Rooms[parentIndex], result.Rooms[index], kind, index);
            }
        }

        private static void CreateLoopConnections(StageGeneratedLayout result, StageMapProfile profile, ref StableRandom random)
        {
            int min = Mathf.Max(0, profile.LoopCountRange.x);
            int max = Mathf.Max(min, profile.LoopCountRange.y);
            int loops = random.RangeInclusive(min, max);
            for (int index = 0; index < loops && result.Rooms.Count > 4; index++)
            {
                StageGeneratedRoom source = result.Rooms[1 + random.Range(0, result.Rooms.Count - 2)];
                StageGeneratedRoom target = result.Rooms[1 + random.Range(0, result.Rooms.Count - 2)];
                if (source != target) TryAddConnection(result, source, target, GeneratedRouteKind.Loop, result.Connections.Count);
            }
        }

        private static void TryAddConnection(StageGeneratedLayout result, StageGeneratedRoom source, StageGeneratedRoom target, GeneratedRouteKind kind, int ordinal)
        {
            Vector2Int delta = target.PositionCells - source.PositionCells;
            CardinalDirection sourceSide;
            CardinalDirection targetSide;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                sourceSide = delta.x >= 0 ? CardinalDirection.Right : CardinalDirection.Left;
                targetSide = delta.x >= 0 ? CardinalDirection.Left : CardinalDirection.Right;
            }
            else
            {
                sourceSide = delta.y >= 0 ? CardinalDirection.Up : CardinalDirection.Down;
                targetSide = delta.y >= 0 ? CardinalDirection.Down : CardinalDirection.Up;
            }
            RoomSocketDefinition sourceSocket = FindSocket(source.Template, sourceSide, kind == GeneratedRouteKind.MainRoute);
            RoomSocketDefinition targetSocket = FindSocket(target.Template, targetSide, kind == GeneratedRouteKind.MainRoute);
            if (sourceSocket == null || targetSocket == null) return;
            result.Connections.Add(new StageGeneratedConnection
            {
                ConnectionGuid = $"{result.Seed}:C{ordinal:D2}:{source.NodeGuid}:{target.NodeGuid}",
                SourceNodeGuid = source.NodeGuid,
                SourceSocketGuid = sourceSocket.SocketGuid,
                TargetNodeGuid = target.NodeGuid,
                TargetSocketGuid = targetSocket.SocketGuid,
                RouteKind = kind,
                Bidirectional = true,
                EdgeType = kind == GeneratedRouteKind.Secret
                    ? RoomEdgeType.SecretGate
                    : RoomEdgeType.PortalPair,
                Condition = string.Empty,
            });
        }

        private static RoomSocketDefinition FindSocket(RoomTemplate template, CardinalDirection side, bool requireMain)
        {
            if (template == null || template.Sockets == null) return null;
            for (int index = 0; index < template.Sockets.Count; index++)
            {
                RoomSocketDefinition socket = template.Sockets[index];
                if (socket != null && socket.Side == side && (!requireMain || socket.MainRouteAllowed)) return socket;
            }
            return null;
        }

        private static void FillElementSlots(StageGeneratedLayout result, StageMapProfile profile, ref StableRandom random)
        {
            int maxSlots = profile.Budget != null ? Mathf.Max(0, profile.Budget.MaxSlotsPerRoom) : 0;
            for (int roomIndex = 0; roomIndex < result.Rooms.Count; roomIndex++)
            {
                StageGeneratedRoom room = result.Rooms[roomIndex];
                int count = maxSlots == 0 ? 0 : random.RangeInclusive(1, maxSlots);
                for (int slotIndex = 0; slotIndex < count; slotIndex++)
                {
                    GeneratedElementSlotKind kind = room.Role == RoomRole.Secret ? GeneratedElementSlotKind.Event :
                        room.Role == RoomRole.Rest && slotIndex == 0 ? GeneratedElementSlotKind.Shop :
                        (GeneratedElementSlotKind)random.Range(0, 2);
                    string contentId = string.Empty;
                    if (slotIndex == 0 && profile.GuaranteedEvents != null)
                    {
                        for (int eventIndex = 0; eventIndex < profile.GuaranteedEvents.Count; eventIndex++)
                        {
                            GuaranteedEventRule rule = profile.GuaranteedEvents[eventIndex];
                            if (rule != null && rule.TargetRole == room.Role && rule.MinimumCount > 0)
                            {
                                kind = GeneratedElementSlotKind.Event;
                                contentId = rule.EventId;
                                break;
                            }
                        }
                    }
                    room.ElementSlots.Add(new GeneratedElementSlot
                    {
                        SlotGuid = $"{room.NodeGuid}:S{slotIndex:D2}",
                        Kind = kind,
                        ContentId = contentId,
                        LocalCell = new Vector2Int(
                            random.RangeInclusive(2, Mathf.Max(2, room.Template.SizeCells.x - 2)),
                            random.RangeInclusive(1, Mathf.Max(1, room.Template.SizeCells.y - 2))),
                    });
                }
            }
        }

        private static void ValidateResult(StageGeneratedLayout result)
        {
            result.ErrorCount = 0;
            for (int first = 0; first < result.Rooms.Count; first++)
            {
                for (int second = first + 1; second < result.Rooms.Count; second++)
                {
                    StageGeneratedRoom a = result.Rooms[first];
                    StageGeneratedRoom b = result.Rooms[second];
                    if (StageLayoutGraphUtility.RoomsOverlap(a.PositionCells, a.Template.SizeCells, b.PositionCells, b.Template.SizeCells))
                        result.ErrorCount++;
                }
            }
            for (int index = 0; index < result.Connections.Count; index++)
            {
                StageGeneratedConnection edge = result.Connections[index];
                StageGeneratedRoom source = result.Rooms.FirstOrDefault(room => room.NodeGuid == edge.SourceNodeGuid);
                StageGeneratedRoom target = result.Rooms.FirstOrDefault(room => room.NodeGuid == edge.TargetNodeGuid);
                RoomSocketDefinition sourceSocket = FindSocketByGuid(source?.Template, edge.SourceSocketGuid);
                RoomSocketDefinition targetSocket = FindSocketByGuid(target?.Template, edge.TargetSocketGuid);
                if (!edge.IsValid || !edge.Bidirectional || edge.RequiresCorridor ||
                    StageLayoutGraphUtility.GetCompatibility(sourceSocket, targetSocket, source == target) !=
                    SocketCompatibility.Compatible)
                {
                    result.ErrorCount++;
                }
            }
            StageGeneratedRoom start = result.Rooms.FirstOrDefault(room => room.Role == RoomRole.Start);
            StageGeneratedRoom exit = result.Rooms.FirstOrDefault(room => room.Role == RoomRole.Exit);
            result.HasValidMainRoute = start != null && exit != null && IsReachable(result, start.NodeGuid, exit.NodeGuid, true);
            if (!result.HasValidMainRoute) result.ErrorCount++;
        }

        private static RoomSocketDefinition FindSocketByGuid(RoomTemplate template, string socketGuid)
        {
            return template?.Sockets?.FirstOrDefault(socket => socket != null &&
                string.Equals(socket.SocketGuid, socketGuid, StringComparison.Ordinal));
        }

        private static bool IsReachable(StageGeneratedLayout result, string start, string target, bool mainOnly)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal) { start };
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (string.Equals(current, target, StringComparison.Ordinal)) return true;
                for (int index = 0; index < result.Connections.Count; index++)
                {
                    StageGeneratedConnection edge = result.Connections[index];
                    if (mainOnly && edge.RouteKind != GeneratedRouteKind.MainRoute) continue;
                    string next = string.Equals(edge.SourceNodeGuid, current, StringComparison.Ordinal) ? edge.TargetNodeGuid :
                        string.Equals(edge.TargetNodeGuid, current, StringComparison.Ordinal) ? edge.SourceNodeGuid : null;
                    if (next != null && visited.Add(next)) queue.Enqueue(next);
                }
            }
            return false;
        }

        private static string CreateValidationHash(StageGeneratedLayout result)
        {
            uint hash = 2166136261u;
            AddHash(ref hash, result.StageId);
            AddHash(ref hash, result.Seed.ToString());
            AddHash(ref hash, result.RerollNonce.ToString());
            AddHash(ref hash, result.Family.ToString());
            for (int index = 0; index < result.Rooms.Count; index++)
            {
                StageGeneratedRoom room = result.Rooms[index];
                AddHash(ref hash, $"{room.NodeGuid}|{room.Template.RoomId}|{room.PositionCells.x}|{room.PositionCells.y}|{room.Role}|{room.Locked}");
            }
            for (int index = 0; index < result.Connections.Count; index++)
            {
                StageGeneratedConnection edge = result.Connections[index];
                AddHash(ref hash, $"{edge.EdgeId}|{edge.FromNodeId}|{edge.FromSocket}|{edge.ToNodeId}|{edge.ToSocket}|{edge.Bidirectional}|{edge.EdgeType}|{edge.Condition}|{edge.RouteKind}");
            }
            return hash.ToString("X8");
        }

        private static void AddHash(ref uint hash, string value)
        {
            if (value == null) return;
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= 16777619u;
            }
        }

        private static string CreateNodeGuid(string stageId, int seed, int index)
        {
            return $"{stageId}:{seed}:R{index:D2}";
        }

        private static uint CombineSeed(int seed, int rerollNonce)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= (uint)rerollNonce * 0x9E3779B9u;
                return value == 0 ? 0xA341316Cu : value;
            }
        }

        private static Vector2Int GetSpiralCoordinate(int index)
        {
            if (index == 0) return Vector2Int.zero;
            int layer = Mathf.CeilToInt((Mathf.Sqrt(index + 1) - 1f) * 0.5f);
            int leg = layer * 2;
            int max = (layer * 2 + 1) * (layer * 2 + 1) - 1;
            int offset = max - index;
            if (offset < leg) return new Vector2Int(layer - offset, -layer);
            if (offset < leg * 2) return new Vector2Int(-layer, -layer + (offset - leg));
            if (offset < leg * 3) return new Vector2Int(-layer + (offset - leg * 2), layer);
            return new Vector2Int(layer, layer - (offset - leg * 3));
        }

        private static Vector2Int GetSpokeCoordinate(int index)
        {
            if (index == 0) return new Vector2Int(-1, 0);
            if (index == 1) return Vector2Int.zero;
            Vector2Int[] directions =
            {
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.left,
                Vector2Int.down,
            };
            int spokeIndex = index - 2;
            int radius = 1 + spokeIndex / directions.Length;
            return directions[spokeIndex % directions.Length] * radius;
        }

        private struct StableRandom
        {
            private uint state;

            public StableRandom(uint seed)
            {
                state = seed == 0 ? 0xA341316Cu : seed;
            }

            public int Range(int minInclusive, int maxExclusive)
            {
                if (maxExclusive <= minInclusive) return minInclusive;
                uint value = NextUInt();
                return minInclusive + (int)(value % (uint)(maxExclusive - minInclusive));
            }

            public int RangeInclusive(int minInclusive, int maxInclusive)
            {
                return Range(minInclusive, maxInclusive + 1);
            }

            private uint NextUInt()
            {
                uint value = state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                state = value;
                return value;
            }
        }
    }
}

#endif
