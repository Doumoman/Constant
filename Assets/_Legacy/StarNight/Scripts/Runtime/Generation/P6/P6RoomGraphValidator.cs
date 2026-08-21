#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Generation.P6
{
    public static class P6RoomGraphValidator
    {
        public const int MaruStatueMinimumAccesses = 2;
        public const int MaruStatueShortcutMaxHops = 4;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up
        };

        public static P6ValidationReport Validate(
            P6GenerationRequest request, P6RoomGraphPlan plan)
        {
            var report = new P6ValidationReport();
            if (request == null || plan == null)
            {
                report.Add("Request or plan is null.");
                return report;
            }

            report.PackingSatisfied = ValidatePacking(request, plan, report);
            ValidateGraph(plan, report);
            report.ClosedLoopSatisfied =
                !request.RequiresClosedLoop || plan.HasClosedLoop;
            if (!report.ClosedLoopSatisfied)
                report.Add("X-2 requires at least one closed graph loop.");

            report.MainPathLengthSatisfied =
                plan.MainPathNodeIds.Count >= 6 && plan.MainPathNodeIds.Count <= 9;
            if (!report.MainPathLengthSatisfied)
                report.Add("Main path must contain 6-9 rooms.");

            report.PrefabUsageSatisfied = ValidatePrefabUsage(plan, report);
            report.SocketClaimsUnique = ValidateSocketClaims(plan, report);
            report.CorridorNetworkConnected = ValidateCorridorNetwork(plan, report);
            ValidatePhysicalTraversal(plan, report);
            report.RiskyChoiceSatisfied =
                ValidateRiskyChoice(plan, report);
            report.LandmarkPrioritySatisfied =
                ValidateLandmarkPriority(plan, report);
            ValidateMaruStatueRooms(plan, report);
            ValidateTerminalRegions(plan, report);
            ValidateCandidateLengths(plan, report);
            ValidateArchetypeStructure(plan, report);
            if (request.StageSlot == P6StageSlot.X2)
                ValidateX2Roles(plan, report);
            if (request.StageSlot == P6StageSlot.X3)
                report.X3RoleSequenceSatisfied =
                    ValidateX3Roles(request, plan, report);
            else
                report.X3RoleSequenceSatisfied = true;
            return report;
        }

        private static void ValidateTerminalRegions(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            P6RoomNode start = FindRoom(plan, plan.StartNodeId);
            P6RoomNode exit = FindRoom(plan, plan.ExitNodeId);
            if (start == null || exit == null)
            {
                report.Add("Start or Exit room is absent.");
                return;
            }

            bool opposite;
            if (plan.Archetype == P6StageArchetype.Descent)
                opposite = start.MacroBounds.yMax == plan.CanvasBounds.yMax
                    && exit.MacroBounds.yMin == plan.CanvasBounds.yMin;
            else if (plan.Archetype == P6StageArchetype.Ascent)
                opposite = start.MacroBounds.yMin == plan.CanvasBounds.yMin
                    && exit.MacroBounds.yMax == plan.CanvasBounds.yMax;
            else
                opposite = start.MacroBounds.xMin == plan.CanvasBounds.xMin
                    && exit.MacroBounds.xMax == plan.CanvasBounds.xMax;
            if (!opposite)
                report.Add("Start and Exit are not reserved in opposite archetype regions.");

            int minimumDistance =
                plan.Archetype == P6StageArchetype.Descent
                    || plan.Archetype == P6StageArchetype.Ascent
                    ? Math.Max(3, plan.CanvasBounds.height - 1)
                    : Math.Max(3, plan.CanvasBounds.width - 1);
            if (Manhattan(Center(start.MacroBounds), Center(exit.MacroBounds))
                < minimumDistance)
                report.Add("Start and Exit do not satisfy the minimum macro distance.");
        }

        private static void ValidateCandidateLengths(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            int maximumDistance = Math.Max(
                3, Math.Max(plan.CanvasBounds.width, plan.CanvasBounds.height) - 1);
            for (int index = 0; index < plan.CandidateEdges.Count; index++)
            {
                P6CandidateEdge edge = plan.CandidateEdges[index];
                P6RoomNode first = FindRoom(plan, edge.FirstNodeId);
                P6RoomNode second = FindRoom(plan, edge.SecondNodeId);
                if (first == null || second == null
                    || Manhattan(
                        Center(first.MacroBounds),
                        Center(second.MacroBounds)) > maximumDistance)
                {
                    report.Add("Candidate graph retained an excessively long edge.");
                    return;
                }
            }
        }

        private static void ValidateArchetypeStructure(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            if (plan.Archetype == P6StageArchetype.Circuit)
            {
                Vector2Int center = new Vector2Int(
                    plan.CanvasBounds.xMin + plan.CanvasBounds.width / 2,
                    plan.CanvasBounds.yMin + plan.CanvasBounds.height / 2);
                bool centralHub = false;
                for (int index = 0; index < plan.Rooms.Count; index++)
                    if (plan.Rooms[index].Degree >= 3
                        && Manhattan(
                            Center(plan.Rooms[index].MacroBounds), center) <= 2)
                    {
                        centralHub = true;
                        break;
                    }
                if (!plan.HasClosedLoop || !centralHub)
                    report.Add("Circuit requires a central degree-3 hub and a loop.");
            }
            else if (plan.Archetype == P6StageArchetype.Fork)
            {
                int branchJunctions = 0;
                for (int index = 0; index < plan.Rooms.Count; index++)
                    if (plan.Rooms[index].Degree >= 3) branchJunctions++;
                if (!plan.HasClosedLoop || branchJunctions < 2)
                    report.Add("Fork requires two branch/rejoin junctions and a loop.");
            }
            else if (plan.Archetype == P6StageArchetype.Chase)
            {
                int longRooms = 0;
                for (int index = 0; index < plan.Rooms.Count; index++)
                    if (plan.Rooms[index].Archetype == RoomArchetype.Wide
                        || plan.Rooms[index].Archetype == RoomArchetype.Tall
                        || plan.Rooms[index].Archetype
                            == RoomArchetype.HorizontalCorridor
                        || plan.Rooms[index].Archetype
                            == RoomArchetype.VerticalCorridor)
                        longRooms++;
                if (longRooms < 3)
                    report.Add("Chase requires at least three wide or tall rooms.");
            }

            for (int index = 0; index < plan.MainPathNodeIds.Count; index++)
            {
                P6RoomNode room =
                    FindRoom(plan, plan.MainPathNodeIds[index]);
                if (room == null || (room.Role & RoomRole.Main) == 0)
                {
                    report.Add("Every designated main-path room must carry Main.");
                    break;
                }
            }
        }

        private static bool ValidateX3Roles(
            P6GenerationRequest request,
            P6RoomGraphPlan plan,
            P6ValidationReport report)
        {
            bool passed = true;
            bool requiresSupply =
                CatalogSupportsRole(request, RoomRole.SupplyCorridor);
            bool authoredBossAvailable =
                CatalogSupportsRole(request, RoomRole.Boss);
            int supplyId = -1;
            int bossId = -1;
            int landmarkId = -1;
            int supplyCount = 0;
            int bossCount = 0;
            int landmarkCount = 0;
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode room = plan.Rooms[index];
                if ((room.Role & RoomRole.SupplyCorridor) != 0)
                {
                    supplyCount++;
                    supplyId = room.Id;
                }
                if ((room.Role & RoomRole.Boss) != 0)
                {
                    bossCount++;
                    bossId = room.Id;
                }
                if ((room.Role & RoomRole.Landmark) != 0)
                {
                    landmarkCount++;
                    landmarkId = room.Id;
                }
            }

            if (bossCount != 1)
            {
                report.Add(
                    "X-3 requires exactly one semantic 2x2 Boss room.");
                passed = false;
            }
            if (landmarkCount != 1)
            {
                report.Add(
                    "X-3 requires exactly one distinct authored "
                    + "2x2 Large Landmark room.");
                passed = false;
            }
            if (requiresSupply && supplyCount != 1)
            {
                report.Add(
                    "X-3 requires exactly one authored pre-boss Supply Corridor.");
                passed = false;
            }
            if (!passed) return false;

            int supplyIndex = IndexOf(plan.MainPathNodeIds, supplyId);
            int bossIndex = IndexOf(plan.MainPathNodeIds, bossId);
            P6RoomNode boss = plan.Rooms[bossId];
            P6RoomNode landmark = plan.Rooms[landmarkId];
            if (requiresSupply && supplyIndex < 1)
            {
                report.Add("X-3 Supply Corridor is not on the main path.");
                passed = false;
            }
            if (bossIndex < 1
                || bossIndex != plan.MainPathNodeIds.Count - 2)
            {
                report.Add(
                    "X-3 Boss must be the final main room before Exit.");
                passed = false;
            }
            if (boss.MacroBounds.size != new Vector2Int(2, 2))
            {
                report.Add("X-3 Boss reservation must occupy a 2x2 footprint.");
                passed = false;
            }
            if (landmark.Id == boss.Id
                || landmark.MacroBounds.Overlaps(boss.MacroBounds)
                || landmark.MacroBounds.size != new Vector2Int(2, 2)
                || landmark.OnMainPath
                || (landmark.Role & RoomRole.Main) != 0
                || (landmark.SupportedRoles & RoomRole.Landmark) == 0)
            {
                report.Add(
                    "X-3 Landmark must be a distinct non-overlapping, "
                    + "off-main authored 2x2 Landmark room.");
                passed = false;
            }
            if (authoredBossAvailable
                && (boss.SupportedRoles & RoomRole.Boss) == 0)
            {
                report.Add(
                    "X-3 selected a non-Boss descriptor despite authored "
                    + "Boss catalog support.");
                passed = false;
            }
            if (!authoredBossAvailable
                && boss.Archetype != RoomArchetype.Large)
            {
                report.Add(
                    "X-3 Boss placeholder must use a validated generic "
                    + "Large-room descriptor.");
                passed = false;
            }
            if (requiresSupply
                && (supplyId == bossId
                    || supplyIndex + 1 != bossIndex))
            {
                report.Add(
                    "X-3 requires distinct adjacent Supply -> Boss -> Exit.");
                passed = false;
            }
            return passed;
        }

        private static bool ValidateRiskyChoice(
            P6RoomGraphPlan plan,
            P6ValidationReport report)
        {
            P6RoomNode risky = null;
            int count = 0;
            for (int index = 0; index < plan.Rooms.Count; index++)
                if ((plan.Rooms[index].Role
                    & RoomRole.RiskyChoice) != 0)
                {
                    risky = plan.Rooms[index];
                    count++;
                }
            if (count != 1 || risky == null)
            {
                report.Add(
                    "Every stage requires exactly one semantic RiskyChoice room.");
                return false;
            }

            bool passed = true;
            if (risky.OnMainPath
                || (risky.Role & RoomRole.Main) != 0)
            {
                report.Add("RiskyChoice must remain off the main path.");
                passed = false;
            }

            int neighbor = -1;
            int degree = 0;
            for (int index = 0; index < plan.Edges.Count; index++)
            {
                P6GraphEdge edge = plan.Edges[index];
                if (edge.FirstNodeId == risky.Id)
                {
                    neighbor = edge.SecondNodeId;
                    degree++;
                }
                else if (edge.SecondNodeId == risky.Id)
                {
                    neighbor = edge.FirstNodeId;
                    degree++;
                }
            }
            if (degree != 1 || risky.Degree != 1)
            {
                report.Add(
                    "RiskyChoice must be a protected optional leaf.");
                passed = false;
            }

            int anchorIndex =
                IndexOf(plan.MainPathNodeIds, neighbor);
            int exitDistance = anchorIndex < 0
                ? -1
                : plan.MainPathNodeIds.Count - 1 - anchorIndex;
            if (anchorIndex < 0
                || exitDistance < 2
                || exitDistance > 4)
            {
                report.Add(
                    "RiskyChoice must attach to a main-path anchor "
                    + "2-4 room hops before Exit.");
                passed = false;
            }
            if (!report.OptionalRoomsReturnable)
            {
                report.Add(
                    "RiskyChoice is not physically reachable and returnable.");
                passed = false;
            }
            return passed;
        }

        private static bool ValidateLandmarkPriority(
            P6RoomGraphPlan plan,
            P6ValidationReport report)
        {
            bool passed = true;
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode room = plan.Rooms[index];
                bool hasPriority =
                    (room.Role & RoomRole.LandmarkJunction) != 0;
                if (room.Degree >= 3 && !hasPriority)
                {
                    report.Add(
                        "Every degree-3+ junction must retain "
                        + "LandmarkJunction semantics, including main rooms.");
                    passed = false;
                }
                else if (room.Degree < 3 && hasPriority)
                {
                    report.Add(
                        "LandmarkJunction was assigned to a non-junction room.");
                    passed = false;
                }
            }
            return passed;
        }

        private static bool ValidateMaruStatueRooms(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            bool passed = true;
            int[] exitHops = BuildExitHopDistances(plan);
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode room = plan.Rooms[index];
                if (!HostsMaruStatue(room)) continue;
                if (CountRoomAccesses(plan, room.Id)
                        >= MaruStatueMinimumAccesses
                    || HasExitShortcut(exitHops, room.Id))
                    continue;
                report.Add(
                    "MaruStatue room needs two exits or one direct "
                    + "shortcut to Exit: " + room.PrefabId);
                passed = false;
            }
            return passed;
        }

        public static bool HostsMaruStatue(P6RoomNode room)
        {
            return room != null
                && (room.Role & RoomRole.MaruStatue) != 0
                && (room.SupportedRoles & RoomRole.MaruStatue) != 0;
        }

        private static bool HasExitShortcut(int[] exitHops, int nodeId)
        {
            return nodeId >= 0
                && nodeId < exitHops.Length
                && exitHops[nodeId] >= 0
                && exitHops[nodeId] <= MaruStatueShortcutMaxHops;
        }

        private static int CountRoomAccesses(
            P6RoomGraphPlan plan, int nodeId)
        {
            if (plan.CorridorNetwork == null) return 0;
            int count = 0;
            for (int index = 0;
                 index < plan.CorridorNetwork.RoomAccesses.Count;
                 index++)
                if (plan.CorridorNetwork.RoomAccesses[index].NodeId == nodeId)
                    count++;
            return count;
        }

        private static int[] BuildExitHopDistances(P6RoomGraphPlan plan)
        {
            var distances = new int[plan.Rooms.Count];
            var adjacency = new List<int>[plan.Rooms.Count];
            for (int index = 0; index < distances.Length; index++)
            {
                distances[index] = -1;
                adjacency[index] = new List<int>();
            }
            for (int index = 0; index < plan.Edges.Count; index++)
            {
                P6GraphEdge edge = plan.Edges[index];
                if (edge.FirstNodeId < 0 || edge.FirstNodeId >= distances.Length
                    || edge.SecondNodeId < 0
                    || edge.SecondNodeId >= distances.Length)
                    continue;
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }
            if (plan.ExitNodeId < 0 || plan.ExitNodeId >= distances.Length)
                return distances;

            var queue = new Queue<int>();
            distances[plan.ExitNodeId] = 0;
            queue.Enqueue(plan.ExitNodeId);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                for (int index = 0; index < adjacency[current].Count; index++)
                {
                    int next = adjacency[current][index];
                    if (distances[next] >= 0) continue;
                    distances[next] = distances[current] + 1;
                    queue.Enqueue(next);
                }
            }
            return distances;
        }

        private static void ValidateX2Roles(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            RoomRole[] required =
            {
                RoomRole.ShopCorridor,
                RoomRole.RecordRoom,
                RoomRole.MaruStatue,
                RoomRole.Landmark
            };
            for (int roleIndex = 0; roleIndex < required.Length; roleIndex++)
            {
                bool found = false;
                for (int roomIndex = 0; roomIndex < plan.Rooms.Count; roomIndex++)
                    if ((plan.Rooms[roomIndex].Role & required[roleIndex]) != 0)
                    {
                        found = true;
                        break;
                    }
                if (!found)
                    report.Add("X-2 is missing required role: " + required[roleIndex]);
            }
        }

        private static bool ValidatePacking(
            P6GenerationRequest request,
            P6RoomGraphPlan plan,
            P6ValidationReport report)
        {
            bool passed = true;
            if (plan.Rooms.Count < request.MinRooms
                || plan.Rooms.Count > request.MaxRooms)
            {
                report.Add("Room count is outside the request range.");
                passed = false;
            }

            for (int first = 0; first < plan.Rooms.Count; first++)
            {
                RectInt bounds = plan.Rooms[first].MacroBounds;
                if (!Contains(plan.CanvasBounds, bounds))
                {
                    report.Add(
                        $"A room lies outside the {request.CanvasSize.x}x"
                        + $"{request.CanvasSize.y} macro canvas.");
                    passed = false;
                }
                for (int second = first + 1; second < plan.Rooms.Count; second++)
                    if (bounds.Overlaps(plan.Rooms[second].MacroBounds))
                    {
                        report.Add("Packed room footprints overlap.");
                        passed = false;
                    }
            }
            return passed;
        }

        private static bool ValidateGraph(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            if (plan.MstEdges.Count != plan.Rooms.Count - 1)
            {
                report.Add("MST edge count is not N-1.");
                return false;
            }
            if (plan.AdditionalEdges.Count < 1 || plan.AdditionalEdges.Count > 3)
            {
                report.Add("Additional edge count must be 1-3.");
                return false;
            }

            var adjacency = new List<int>[plan.Rooms.Count];
            for (int index = 0; index < adjacency.Length; index++)
                adjacency[index] = new List<int>();
            for (int index = 0; index < plan.Edges.Count; index++)
            {
                P6GraphEdge edge = plan.Edges[index];
                if (edge.FirstNodeId < 0 || edge.FirstNodeId >= adjacency.Length
                    || edge.SecondNodeId < 0 || edge.SecondNodeId >= adjacency.Length)
                {
                    report.Add("Graph edge references an invalid room.");
                    return false;
                }
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }

            var visited = new bool[adjacency.Length];
            var queue = new Queue<int>();
            queue.Enqueue(plan.StartNodeId);
            visited[plan.StartNodeId] = true;
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                for (int index = 0; index < adjacency[current].Count; index++)
                {
                    int next = adjacency[current][index];
                    if (visited[next]) continue;
                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }
            for (int index = 0; index < visited.Length; index++)
                if (!visited[index])
                {
                    report.Add("A room is unreachable or cannot return to the main graph.");
                    return false;
                }
            return visited[plan.ExitNodeId];
        }

        private static bool ValidatePrefabUsage(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            var uses = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                if (!plan.Rooms[index].HasValidatedTraversalProof)
                {
                    report.Add(
                        "Selected room lacks a passed physical traversal proof: "
                        + plan.Rooms[index].PrefabId);
                    return false;
                }
                string prefabId = plan.Rooms[index].PrefabId;
                int count;
                uses.TryGetValue(prefabId, out count);
                count++;
                uses[prefabId] = count;
                if (count > 2)
                {
                    report.Add("A prefab is used more than twice: " + prefabId);
                    return false;
                }
                if (plan.Rooms[index].OnMainPath
                    && !plan.Rooms[index].MainRouteAllowed)
                {
                    report.Add("A main-path prefab is not main-route safe.");
                    return false;
                }
            }
            return true;
        }

        private static bool ValidateSocketClaims(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            if (plan.CorridorNetwork == null)
            {
                report.Add("Physical corridor access list is absent.");
                return false;
            }

            var claims = new HashSet<string>(StringComparer.Ordinal);
            int[] perRoom = new int[plan.Rooms.Count];
            for (int index = 0; index < plan.CorridorNetwork.RoomAccesses.Count; index++)
            {
                P6RoomAccess access = plan.CorridorNetwork.RoomAccesses[index];
                if (access.AccessId != index
                    || access.NodeId < 0
                    || access.NodeId >= plan.Rooms.Count)
                {
                    report.Add("Physical access id or room id is invalid.");
                    return false;
                }
                P6RoomNode room = plan.Rooms[access.NodeId];
                P6SocketTraversalProof socket =
                    room.TraversalProof.FindSocket(access.SocketId);
                if (socket == null
                    || !claims.Add(access.NodeId + ":" + access.SocketId))
                {
                    report.Add("Access does not map to one unique authored socket.");
                    return false;
                }
                if (access.SocketSide != ToP6Side(socket.Side)
                    || access.CellOffset != socket.CellOffset
                    || access.OpeningSize != socket.OpeningSize
                    || access.TraversalType != socket.TraversalType
                    || access.MainRouteAllowed != socket.MainRouteAllowed
                    || access.ValidationAnchor != socket.ValidationAnchor)
                {
                    report.Add("Access lost exact authored socket metadata.");
                    return false;
                }
                if (access.PortalCell - access.SocketCell
                    != ExpectedPortalDelta(socket.Side))
                {
                    report.Add("Socket portal is not on its authored boundary side.");
                    return false;
                }
                Vector2Int expectedSocket;
                Vector2Int expectedPortal;
                CalculateScaledPortal(
                    room, socket, out expectedSocket, out expectedPortal);
                if (access.SocketCell != expectedSocket
                    || access.PortalCell != expectedPortal)
                {
                    report.Add("Access does not map to the exact scale-2 socket lane.");
                    return false;
                }
                perRoom[access.NodeId]++;
            }

            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode room = plan.Rooms[index];
                int expectedExact = room.OnMainPath
                    ? ((room.Role & (RoomRole.Start | RoomRole.Exit)) != 0
                        ? 1 : 2)
                    : ((room.Role & (RoomRole.RecordRoom
                        | RoomRole.RiskyChoice)) != 0 ? 1 : -1);
                if (perRoom[index] < 1
                    || (expectedExact == 1 && perRoom[index] != 1)
                    || (expectedExact == 2 && perRoom[index] < 2)
                    || perRoom[index] > room.TraversalProof.Sockets.Count)
                {
                    report.Add(
                        "Room access count violates its physical socket role: "
                        + room.PrefabId);
                    return false;
                }
            }

            for (int index = 0; index < plan.Edges.Count; index++)
            {
                P6GraphEdge edge = plan.Edges[index];
                if (!AccessMatchesNode(
                        plan, edge.FirstAccessId, edge.FirstNodeId)
                    || !AccessMatchesNode(
                        plan, edge.SecondAccessId, edge.SecondNodeId))
                {
                    report.Add("Graph edge references an access on the wrong room.");
                    return false;
                }
            }
            return true;
        }

        private static bool ValidateCorridorNetwork(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            if (plan.CorridorNetwork == null
                || plan.CorridorNetwork.Cells.Count == 0)
            {
                report.Add("Corridor network is empty.");
                return false;
            }
            if (plan.CorridorNetwork.RoutingScale != 2)
            {
                report.Add("Physical corridor lattice must use routing scale 2.");
                return false;
            }
            var cells = new HashSet<Vector2Int>(plan.CorridorNetwork.Cells);
            var interiors = BuildScaledInteriors(plan);
            foreach (Vector2Int cell in cells)
                if (interiors.Contains(cell))
                {
                    report.Add("A* corridor crossed a forbidden room interior.");
                    return false;
                }
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            Vector2Int start = plan.CorridorNetwork.Cells[0];
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                for (int direction = 0; direction < Directions.Length; direction++)
                {
                    Vector2Int next = current + Directions[direction];
                    if (cells.Contains(next) && visited.Add(next)) queue.Enqueue(next);
                }
            }
            if (visited.Count != cells.Count)
            {
                report.Add("Normalized corridor network is disconnected.");
                return false;
            }
            for (int index = 0; index < plan.CorridorNetwork.RoomAccesses.Count; index++)
            {
                P6RoomAccess access =
                    plan.CorridorNetwork.RoomAccesses[index];
                if (!cells.Contains(access.PortalCell)
                    || interiors.Contains(access.PortalCell))
                {
                    report.Add(
                        "A room portal is absent or overlaps another room interior.");
                    return false;
                }
            }

            for (int edgeIndex = 0; edgeIndex < plan.Edges.Count; edgeIndex++)
            {
                P6GraphEdge edge = plan.Edges[edgeIndex];
                IReadOnlyList<Vector2Int> path = plan.Edges[edgeIndex].CorridorCells;
                if (path.Count == 0)
                {
                    report.Add("A selected graph edge has no A* route.");
                    return false;
                }
                P6RoomAccess first =
                    plan.CorridorNetwork.RoomAccesses[edge.FirstAccessId];
                P6RoomAccess second =
                    plan.CorridorNetwork.RoomAccesses[edge.SecondAccessId];
                bool endpointsMatch =
                    path[0] == first.PortalCell
                    && path[path.Count - 1] == second.PortalCell;
                if (!endpointsMatch)
                {
                    report.Add("Corridor path endpoints do not match edge accesses.");
                    return false;
                }
                for (int cell = 1; cell < path.Count; cell++)
                    if (Manhattan(path[cell - 1], path[cell]) != 1)
                    {
                        report.Add("A* corridor path is not four-neighbor contiguous.");
                        return false;
                    }
                for (int cell = 0; cell < path.Count; cell++)
                    if (interiors.Contains(path[cell]))
                    {
                        report.Add("Corridor edge entered a forbidden room interior.");
                        return false;
                    }
            }

            if (plan.StageSlot == P6StageSlot.X2)
            {
                var mstCells = new HashSet<Vector2Int>();
                for (int index = 0; index < plan.MstEdges.Count; index++)
                    for (int cell = 0;
                         cell < plan.MstEdges[index].CorridorCells.Count;
                         cell++)
                        mstCells.Add(plan.MstEdges[index].CorridorCells[cell]);
                for (int index = 0; index < plan.AdditionalEdges.Count; index++)
                {
                    bool addsSegment = false;
                    IReadOnlyList<Vector2Int> path =
                        plan.AdditionalEdges[index].CorridorCells;
                    for (int cell = 1; cell < path.Count - 1; cell++)
                        if (!mstCells.Contains(path[cell]))
                        {
                            addsSegment = true;
                            break;
                        }
                    if (!addsSegment)
                    {
                        report.Add(
                            "X-2 additional edge fully overlaps the MST corridor.");
                        return false;
                    }
                }
                if (!ContainsPhysicalCycle(cells))
                {
                    report.Add("X-2 normalized corridor network has no physical loop.");
                    return false;
                }
            }
            return true;
        }

        private static void ValidatePhysicalTraversal(
            P6RoomGraphPlan plan, P6ValidationReport report)
        {
            int accessCount = plan.CorridorNetwork == null
                ? 0 : plan.CorridorNetwork.RoomAccesses.Count;
            var directed = new List<int>[accessCount];
            var reverse = new List<int>[accessCount];
            for (int index = 0; index < accessCount; index++)
            {
                directed[index] = new List<int>();
                reverse[index] = new List<int>();
            }

            for (int index = 0; index < plan.Edges.Count; index++)
            {
                P6GraphEdge edge = plan.Edges[index];
                AddDirected(
                    directed, reverse,
                    edge.FirstAccessId, edge.SecondAccessId);
                AddDirected(
                    directed, reverse,
                    edge.SecondAccessId, edge.FirstAccessId);
            }

            bool oneCell = true;
            bool jumpEnvelope = true;
            bool exitBlock = true;
            for (int roomIndex = 0; roomIndex < plan.Rooms.Count; roomIndex++)
            {
                P6RoomNode room = plan.Rooms[roomIndex];
                P6RoomTraversalProof proof = room.TraversalProof;
                exitBlock &= proof != null && proof.ExitBlockProof;
                if (proof == null) continue;
                for (int linkIndex = 0;
                     linkIndex < proof.Links.Count;
                     linkIndex++)
                {
                    P6SocketLinkProof link = proof.Links[linkIndex];
                    int from = FindAccessForSocket(
                        plan, room.Id, link.FromSocketId);
                    int to = FindAccessForSocket(
                        plan, room.Id, link.ToSocketId);
                    if (from < 0 || to < 0) continue;
                    if (link.Passed)
                        AddDirected(directed, reverse, from, to);
                    oneCell &= link.OneCellBodyClearance;
                    jumpEnvelope &= link.JumpEnvelopePassed;
                }
            }

            for (int index = 0; index < accessCount; index++)
            {
                P6RoomAccess access =
                    plan.CorridorNetwork.RoomAccesses[index];
                P6SocketTraversalProof socket =
                    plan.Rooms[access.NodeId].TraversalProof.FindSocket(
                        access.SocketId);
                oneCell &=
                    access.OpeningSize == Vector2Int.one
                    && socket != null
                    && socket.OneCellOpening
                    && socket.OneCellBodyClearance;
            }

            bool mainProof = true;
            int startAccess = -1;
            int exitAccess = -1;
            var mainAccesses = new HashSet<int>();
            int returnableDrops = 0;
            for (int index = 1;
                 index < plan.MainPathNodeIds.Count;
                 index++)
            {
                int previousNode = plan.MainPathNodeIds[index - 1];
                int currentNode = plan.MainPathNodeIds[index];
                P6GraphEdge edge = FindEdge(plan, previousNode, currentNode);
                if (edge == null)
                {
                    mainProof = false;
                    break;
                }
                int previousAccess = AccessForNode(edge, previousNode);
                int currentAccess = AccessForNode(edge, currentNode);
                mainAccesses.Add(previousAccess);
                mainAccesses.Add(currentAccess);
                if (index == 1) startAccess = previousAccess;
                if (index == plan.MainPathNodeIds.Count - 1)
                    exitAccess = currentAccess;
            }

            for (int index = 1;
                 mainProof && index < plan.MainPathNodeIds.Count - 1;
                 index++)
            {
                int nodeId = plan.MainPathNodeIds[index];
                P6GraphEdge incomingEdge = FindEdge(
                    plan, plan.MainPathNodeIds[index - 1], nodeId);
                P6GraphEdge outgoingEdge = FindEdge(
                    plan, nodeId, plan.MainPathNodeIds[index + 1]);
                int incomingAccess = AccessForNode(incomingEdge, nodeId);
                int outgoingAccess = AccessForNode(outgoingEdge, nodeId);
                if (incomingAccess == outgoingAccess
                    || !HasPassedDirectedMainLink(
                        plan.Rooms[nodeId],
                        plan.CorridorNetwork.RoomAccesses[incomingAccess].SocketId,
                        plan.CorridorNetwork.RoomAccesses[outgoingAccess].SocketId))
                {
                    mainProof = false;
                    break;
                }
            }

            foreach (int accessId in mainAccesses)
            {
                P6RoomAccess access =
                    plan.CorridorNetwork.RoomAccesses[accessId];
                mainProof &= access.IsToolFreeMainRouteAccess;
                if (access.TraversalType == RoomTraversalType.ReturnableDrop)
                    returnableDrops++;
            }
            mainProof &= returnableDrops <= 1;

            bool[] reachable = Traverse(directed, startAccess);
            report.StartExitReachable =
                startAccess >= 0
                && exitAccess >= 0
                && exitAccess < reachable.Length
                && reachable[exitAccess];

            bool[] canReachMain = TraverseFromMany(reverse, mainAccesses);
            bool optionalReturnable = report.StartExitReachable;
            for (int roomIndex = 0;
                 optionalReturnable && roomIndex < plan.Rooms.Count;
                 roomIndex++)
            {
                if (plan.Rooms[roomIndex].OnMainPath) continue;
                bool hasAccess = false;
                bool roomCanReturn = true;
                for (int access = 0; access < accessCount; access++)
                    if (plan.CorridorNetwork.RoomAccesses[access].NodeId
                        == roomIndex)
                    {
                        hasAccess = true;
                        roomCanReturn &= reachable[access]
                            && canReachMain[access];
                    }
                optionalReturnable &= hasAccess && roomCanReturn;
            }
            report.OptionalRoomsReturnable = optionalReturnable;
            report.OneCellOpeningsSatisfied = oneCell;
            report.JumpEnvelopeSatisfied = jumpEnvelope;
            report.ToolFreeMainPathSatisfied = mainProof;
            report.ExitBlockProofSatisfied = exitBlock;
            report.PhysicalTraversalSatisfied =
                report.CorridorNetworkConnected
                && report.StartExitReachable
                && report.OptionalRoomsReturnable
                && oneCell
                && jumpEnvelope
                && mainProof
                && exitBlock;

            if (!report.StartExitReachable)
                report.Add("Physical socket/corridor composition cannot reach Exit.");
            if (!report.OptionalRoomsReturnable)
                report.Add("An optional room cannot physically return to the main route.");
            if (!oneCell)
                report.Add("A selected physical traversal violates one-cell clearance.");
            if (!jumpEnvelope)
                report.Add("A selected internal traversal exceeds the jump envelope.");
            if (!mainProof)
                report.Add("Main route lacks distinct directed tool-free socket proofs.");
            if (!exitBlock)
                report.Add("A selected room lacks ExitBlockProof.");
        }

        private static void AddDirected(
            List<int>[] directed,
            List<int>[] reverse,
            int from,
            int to)
        {
            if (from < 0 || to < 0
                || from >= directed.Length || to >= directed.Length)
                return;
            if (!directed[from].Contains(to)) directed[from].Add(to);
            if (!reverse[to].Contains(from)) reverse[to].Add(from);
        }

        private static bool[] Traverse(List<int>[] graph, int start)
        {
            var visited = new bool[graph.Length];
            if (start < 0 || start >= graph.Length) return visited;
            var queue = new Queue<int>();
            queue.Enqueue(start);
            visited[start] = true;
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                for (int index = 0; index < graph[current].Count; index++)
                {
                    int next = graph[current][index];
                    if (visited[next]) continue;
                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }
            return visited;
        }

        private static bool[] TraverseFromMany(
            List<int>[] graph, HashSet<int> starts)
        {
            var visited = new bool[graph.Length];
            var queue = new Queue<int>();
            foreach (int start in starts)
                if (start >= 0 && start < graph.Length && !visited[start])
                {
                    visited[start] = true;
                    queue.Enqueue(start);
                }
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                for (int index = 0; index < graph[current].Count; index++)
                {
                    int next = graph[current][index];
                    if (visited[next]) continue;
                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }
            return visited;
        }

        private static bool Contains(RectInt outer, RectInt inner)
        {
            return inner.xMin >= outer.xMin && inner.yMin >= outer.yMin
                && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;
        }

        private static P6RoomNode FindRoom(
            P6RoomGraphPlan plan, int nodeId)
        {
            for (int index = 0; index < plan.Rooms.Count; index++)
                if (plan.Rooms[index].Id == nodeId) return plan.Rooms[index];
            return null;
        }

        private static bool AccessMatchesNode(
            P6RoomGraphPlan plan, int accessId, int nodeId)
        {
            return plan.CorridorNetwork != null
                && accessId >= 0
                && accessId < plan.CorridorNetwork.RoomAccesses.Count
                && plan.CorridorNetwork.RoomAccesses[accessId].NodeId == nodeId;
        }

        private static int FindAccessForSocket(
            P6RoomGraphPlan plan, int nodeId, string socketId)
        {
            for (int index = 0;
                 index < plan.CorridorNetwork.RoomAccesses.Count;
                 index++)
            {
                P6RoomAccess access =
                    plan.CorridorNetwork.RoomAccesses[index];
                if (access.NodeId == nodeId
                    && string.Equals(
                        access.SocketId, socketId, StringComparison.Ordinal))
                    return access.AccessId;
            }
            return -1;
        }

        private static P6GraphEdge FindEdge(
            P6RoomGraphPlan plan, int firstNode, int secondNode)
        {
            int low = Math.Min(firstNode, secondNode);
            int high = Math.Max(firstNode, secondNode);
            for (int index = 0; index < plan.Edges.Count; index++)
                if (plan.Edges[index].FirstNodeId == low
                    && plan.Edges[index].SecondNodeId == high)
                    return plan.Edges[index];
            return null;
        }

        private static int AccessForNode(P6GraphEdge edge, int nodeId)
        {
            if (edge == null) return -1;
            return edge.FirstNodeId == nodeId
                ? edge.FirstAccessId : edge.SecondAccessId;
        }

        private static bool HasPassedDirectedMainLink(
            P6RoomNode room, string fromSocketId, string toSocketId)
        {
            for (int index = 0;
                 index < room.TraversalProof.Links.Count;
                 index++)
            {
                P6SocketLinkProof link = room.TraversalProof.Links[index];
                if (link.Kind == P6SocketLinkKind.MainRoutePair
                    && link.Passed
                    && string.Equals(
                        link.FromSocketId,
                        fromSocketId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        link.ToSocketId,
                        toSocketId,
                        StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static HashSet<Vector2Int> BuildScaledInteriors(
            P6RoomGraphPlan plan)
        {
            var result = new HashSet<Vector2Int>();
            for (int index = 0; index < plan.Rooms.Count; index++)
                foreach (Vector2Int cell in
                         ScaledRoomInterior(
                             plan.Rooms[index].MacroBounds).allPositionsWithin)
                    result.Add(cell);
            return result;
        }

        private static RectInt ScaledRoomInterior(RectInt macroBounds)
        {
            return new RectInt(
                macroBounds.xMin * 2 + 1,
                macroBounds.yMin * 2 + 1,
                macroBounds.width * 2 - 1,
                macroBounds.height * 2 - 1);
        }

        private static bool ContainsPhysicalCycle(
            HashSet<Vector2Int> cells)
        {
            int edgeCount = 0;
            foreach (Vector2Int cell in cells)
            {
                if (cells.Contains(cell + Vector2Int.right)) edgeCount++;
                if (cells.Contains(cell + Vector2Int.up)) edgeCount++;
            }
            return edgeCount >= cells.Count;
        }

        private static P6SocketSides ToP6Side(RoomSocketSide side)
        {
            switch (side)
            {
                case RoomSocketSide.Left: return P6SocketSides.Left;
                case RoomSocketSide.Right: return P6SocketSides.Right;
                case RoomSocketSide.Bottom: return P6SocketSides.Bottom;
                case RoomSocketSide.Top: return P6SocketSides.Top;
                default: return P6SocketSides.None;
            }
        }

        private static Vector2Int ExpectedPortalDelta(RoomSocketSide side)
        {
            switch (side)
            {
                case RoomSocketSide.Left: return Vector2Int.left;
                case RoomSocketSide.Right: return Vector2Int.right;
                case RoomSocketSide.Bottom: return Vector2Int.down;
                case RoomSocketSide.Top: return Vector2Int.up;
                default: return Vector2Int.zero;
            }
        }

        private static void CalculateScaledPortal(
            P6RoomNode room,
            P6SocketTraversalProof socketProof,
            out Vector2Int socket,
            out Vector2Int portal)
        {
            RectInt interior = ScaledRoomInterior(room.MacroBounds);
            Vector2Int logicalSize = room.TraversalProof.LogicalSize;
            if (socketProof.Side == RoomSocketSide.Left
                || socketProof.Side == RoomSocketSide.Right)
            {
                int y = interior.yMin + ScaleSocketLane(
                    socketProof.CellOffset.y,
                    logicalSize.y,
                    interior.height);
                socket = new Vector2Int(
                    socketProof.Side == RoomSocketSide.Left
                        ? interior.xMin : interior.xMax - 1,
                    y);
            }
            else
            {
                int x = interior.xMin + ScaleSocketLane(
                    socketProof.CellOffset.x,
                    logicalSize.x,
                    interior.width);
                socket = new Vector2Int(
                    x,
                    socketProof.Side == RoomSocketSide.Bottom
                        ? interior.yMin : interior.yMax - 1);
            }
            portal = socket + ExpectedPortalDelta(socketProof.Side);
        }

        private static int ScaleSocketLane(
            int logicalOffset, int logicalSpan, int routingSpan)
        {
            if (logicalSpan <= 1 || routingSpan <= 1) return 0;
            return Mathf.Clamp(
                Mathf.RoundToInt(
                    logicalOffset * (routingSpan - 1f)
                    / (logicalSpan - 1f)),
                0,
                routingSpan - 1);
        }

        private static bool PlanHasRole(
            P6RoomGraphPlan plan, RoomRole role)
        {
            for (int index = 0; index < plan.Rooms.Count; index++)
                if ((plan.Rooms[index].Role & role) != 0) return true;
            return false;
        }

        private static bool CatalogSupportsRole(
            P6GenerationRequest request, RoomRole role)
        {
            for (int index = 0; index < request.Prefabs.Count; index++)
            {
                P6RoomPrefabDescriptor prefab = request.Prefabs[index];
                if (prefab != null
                    && (prefab.Region == request.Region
                        || prefab.Region == RoomRegion.Universal)
                    && prefab.Supports(role))
                    return true;
            }
            return false;
        }

        private static int IndexOf(
            IReadOnlyList<int> values,
            int target)
        {
            for (int index = 0; index < values.Count; index++)
                if (values[index] == target) return index;
            return -1;
        }

        private static Vector2Int Center(RectInt bounds)
        {
            return new Vector2Int(
                bounds.xMin + (bounds.width - 1) / 2,
                bounds.yMin + (bounds.height - 1) / 2);
        }

        private static int Manhattan(Vector2Int first, Vector2Int second)
        {
            return Math.Abs(first.x - second.x) + Math.Abs(first.y - second.y);
        }
    }
}

#endif
