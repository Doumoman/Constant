#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Text;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Generation.P6
{
    public static class P6MoonPalaceProfile
    {
        public static readonly Vector2Int CanvasSize = new Vector2Int(8, 6);

        public static int GetRoomWeight(RoomArchetype archetype)
        {
            switch (archetype)
            {
                case RoomArchetype.Small: return 25;
                case RoomArchetype.Standard: return 30;
                case RoomArchetype.Wide: return 20;
                case RoomArchetype.Tall: return 15;
                case RoomArchetype.Large: return 10;
                default: return 0;
            }
        }
    }

    public static class P6RoomGraphGenerator
    {
        // P8 keeps the statue three to five graph rooms from Exit, so a
        // generated statue room stays inside the P6 shortcut window too.
        private const int MaruStatueMinExitHops = 3;
        private const int MaruStatueMaxExitHops =
            P6RoomGraphValidator.MaruStatueShortcutMaxHops;

        private sealed class PackedRoom
        {
            public int Id;
            public RectInt Bounds;
            public RoomArchetype DesiredArchetype;
            public RoomRole ReservedRole;
            public RoomRole SemanticRole;
            public bool ForcedStart;
            public bool ForcedExit;
            public bool Main;
            public RoomRole Role;
            public int Degree;
            public P6RoomPrefabDescriptor Prefab;
        }

        private sealed class EdgeAccessAssignment
        {
            public int FirstAccessId;
            public int SecondAccessId;
        }

        private sealed class UnionFind
        {
            private readonly int[] parent;
            private readonly byte[] rank;

            public UnionFind(int count)
            {
                parent = new int[count];
                rank = new byte[count];
                for (int index = 0; index < count; index++) parent[index] = index;
            }

            public int Find(int value)
            {
                while (parent[value] != value)
                {
                    parent[value] = parent[parent[value]];
                    value = parent[value];
                }
                return value;
            }

            public bool Join(int first, int second)
            {
                first = Find(first);
                second = Find(second);
                if (first == second) return false;
                if (rank[first] < rank[second]) parent[first] = second;
                else
                {
                    parent[second] = first;
                    if (rank[first] == rank[second]) rank[first]++;
                }
                return true;
            }
        }

        public static P6GenerationResult Generate(P6GenerationRequest request)
        {
            string requestIssue = ValidateRequest(request);
            if (requestIssue != null)
                return Failure(request == null ? 0 : request.Seed, 0,
                    P6GenerationFailure.InvalidRequest, requestIssue);

            P6GenerationFailure lastFailure = P6GenerationFailure.AttemptsExhausted;
            string lastReason = "No candidate was accepted.";
            int attempts = Math.Max(1, request.MaxAttempts);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int candidateSeed = P6DeterministicRandom.DeriveSeed(request.Seed, attempt);
                P6RoomGraphPlan plan;
                P6GenerationFailure failure;
                string reason;
                if (!TryGenerateCandidate(request, candidateSeed, out plan, out failure, out reason))
                {
                    lastFailure = failure;
                    lastReason = reason;
                    continue;
                }

                P6ValidationReport validation = P6RoomGraphValidator.Validate(request, plan);
                if (!validation.Passed)
                {
                    lastFailure = P6GenerationFailure.ValidationFailed;
                    lastReason = validation.ToString();
                    continue;
                }

                return new P6GenerationResult(
                    request.Seed, candidateSeed, attempt + 1, plan, validation,
                    P6GenerationFailure.None, string.Empty, Fingerprint(plan));
            }

            return Failure(request.Seed, attempts, lastFailure, lastReason);
        }

        public static bool TryGenerateCandidate(
            P6GenerationRequest request,
            int candidateSeed,
            out P6RoomGraphPlan plan,
            out P6GenerationFailure failure,
            out string reason)
        {
            plan = null;
            failure = P6GenerationFailure.None;
            reason = string.Empty;
            string requestIssue = ValidateRequest(request);
            if (requestIssue != null)
            {
                failure = P6GenerationFailure.InvalidRequest;
                reason = requestIssue;
                return false;
            }
            if (!HasValidatedTraversalCatalog(request))
            {
                failure = P6GenerationFailure.CatalogInsufficient;
                reason = "Catalog has no descriptor with a passed physical traversal proof.";
                return false;
            }

            var random = new P6DeterministicRandom(candidateSeed);
            P6StageArchetype stageArchetype =
                request.ArchetypeOverride ?? PickStageArchetype(request.StageSlot, ref random);
            List<PackedRoom> packed;
            if (!TryPack(request, stageArchetype, ref random, out packed))
            {
                failure = P6GenerationFailure.PackingFailed;
                reason = "Could not pack the requested room count into the macro canvas.";
                return false;
            }

            List<int> ordered = OrderForArchetype(packed, stageArchetype);
            List<int> mainPath = BuildMainPath(
                packed, ordered, request.StageSlot, stageArchetype,
                request.CanvasSize, ref random);
            if (mainPath.Count < 6)
            {
                failure = P6GenerationFailure.GraphFailed;
                reason = "Could not reserve a 6-9 room tool-free main path.";
                return false;
            }
            var preferredTree =
                BuildPreferredTree(packed, mainPath, stageArchetype);
            List<P6CandidateEdge> candidates =
                BuildCandidates(packed, preferredTree, request.CanvasSize);
            List<P6CandidateEdge> mstCandidates = BuildMst(packed.Count, candidates);
            if (mstCandidates.Count != packed.Count - 1)
            {
                failure = P6GenerationFailure.GraphFailed;
                reason = "Candidate graph did not produce a spanning tree.";
                return false;
            }

            int extraCount = Math.Min(random.NextInt(1, 4),
                candidates.Count - mstCandidates.Count);
            var protectedLeaves = new HashSet<int>();
            for (int index = 0; index < packed.Count; index++)
                if (packed[index].ReservedRole == RoomRole.RecordRoom
                    || packed[index].ReservedRole == RoomRole.MaruStatue
                    || (packed[index].SemanticRole
                        & RoomRole.RiskyChoice) != 0)
                    protectedLeaves.Add(index);
            List<P6CandidateEdge> additionalCandidates =
                SelectAdditional(
                    candidates, mstCandidates, extraCount,
                    protectedLeaves, packed, mainPath,
                    stageArchetype, ref random);
            EnsureMaruStatueShortcut(
                packed, mainPath, mstCandidates, additionalCandidates);
            AssignDegreesAndRoles(
                packed, mainPath, mstCandidates, additionalCandidates);
            if (!SelectPrefabs(request, packed, ref random))
            {
                failure = P6GenerationFailure.CatalogInsufficient;
                reason = "No valid prefab assignment obeyed region, footprint, route and repeat limits.";
                return false;
            }

            List<P6RoomNode> nodes = BuildNodes(packed);
            List<P6RoomAccess> accesses;
            Dictionary<long, EdgeAccessAssignment> edgeAccesses;
            if (!BuildAccessAssignments(
                    nodes, mainPath, mstCandidates, additionalCandidates,
                    stageArchetype, out accesses, out edgeAccesses))
            {
                failure = P6GenerationFailure.CorridorFailed;
                reason = "Actual prefab sockets cannot prove the selected physical graph.";
                return false;
            }
            List<P6GraphEdge> mstEdges;
            List<P6GraphEdge> additionalEdges;
            P6CorridorNetwork network;
            if (!RouteCorridors(
                    request.CanvasSize, nodes, accesses,
                    edgeAccesses, mstCandidates, additionalCandidates,
                    out mstEdges, out additionalEdges, out network))
            {
                failure = P6GenerationFailure.CorridorFailed;
                reason = "Macro A* could not connect every selected graph edge.";
                return false;
            }

            plan = new P6RoomGraphPlan(
                candidateSeed, request.StageSlot, stageArchetype,
                new RectInt(Vector2Int.zero, request.CanvasSize),
                nodes, candidates, mstEdges, additionalEdges,
                mainPath, mainPath[0], mainPath[mainPath.Count - 1], network);
            return true;
        }

        private static string ValidateRequest(P6GenerationRequest request)
        {
            if (request == null) return "Request is null.";
            if (request.CanvasSize.x < 2 || request.CanvasSize.y < 2)
                return "Canvas must be at least 2x2.";
            if (request.MinRooms < 2 || request.MaxRooms < request.MinRooms)
                return "Room range is invalid.";
            if (request.Prefabs == null || request.Prefabs.Count == 0)
                return "Prefab descriptor catalog is empty.";
            int valid = 0;
            for (int index = 0; index < request.Prefabs.Count; index++)
            {
                P6RoomPrefabDescriptor prefab = request.Prefabs[index];
                if (prefab != null
                    && !string.IsNullOrEmpty(prefab.PrefabId)
                    && prefab.MacroSize.x > 0 && prefab.MacroSize.y > 0
                    && prefab.SocketSides != P6SocketSides.None
                    && (prefab.Region == request.Region
                        || prefab.Region == RoomRegion.Universal))
                    valid++;
            }
            return valid == 0 ? "Catalog has no usable descriptor for this region." : null;
        }

        private static bool HasValidatedTraversalCatalog(
            P6GenerationRequest request)
        {
            for (int index = 0; index < request.Prefabs.Count; index++)
            {
                P6RoomPrefabDescriptor prefab = request.Prefabs[index];
                if (prefab != null
                    && prefab.HasValidatedTraversalProof
                    && (prefab.Region == request.Region
                        || prefab.Region == RoomRegion.Universal))
                    return true;
            }
            return false;
        }

        private static P6StageArchetype PickStageArchetype(
            P6StageSlot slot, ref P6DeterministicRandom random)
        {
            int roll = random.NextInt(100);
            if (slot == P6StageSlot.X2)
            {
                if (roll < 30) return P6StageArchetype.Circuit;
                if (roll < 60) return P6StageArchetype.Fork;
                if (roll < 75) return P6StageArchetype.Traverse;
                if (roll < 85) return P6StageArchetype.Descent;
                if (roll < 95) return P6StageArchetype.Ascent;
                return P6StageArchetype.Chase;
            }
            if (slot == P6StageSlot.X3)
            {
                if (roll < 35) return P6StageArchetype.Chase;
                if (roll < 60) return P6StageArchetype.Traverse;
                if (roll < 75) return P6StageArchetype.Descent;
                if (roll < 90) return P6StageArchetype.Ascent;
                if (roll < 95) return P6StageArchetype.Circuit;
                return P6StageArchetype.Fork;
            }
            if (roll < 40) return P6StageArchetype.Traverse;
            if (roll < 55) return P6StageArchetype.Descent;
            if (roll < 70) return P6StageArchetype.Ascent;
            if (roll < 82) return P6StageArchetype.Fork;
            if (roll < 94) return P6StageArchetype.Circuit;
            return P6StageArchetype.Chase;
        }

        private static bool TryPack(
            P6GenerationRequest request,
            P6StageArchetype stageArchetype,
            ref P6DeterministicRandom random,
            out List<PackedRoom> rooms)
        {
            rooms = new List<PackedRoom>();
            int canvasCells = request.CanvasSize.x * request.CanvasSize.y;
            int maximum = Math.Min(request.MaxRooms, canvasCells);
            if (request.StageSlot == P6StageSlot.X3)
                maximum = Math.Min(maximum, 13);
            if (maximum < request.MinRooms) return false;
            int minimum = request.StageSlot == P6StageSlot.X2
                ? Math.Max(10, request.MinRooms)
                : request.MinRooms;
            if (minimum > maximum) return false;
            int target = random.NextInt(minimum, maximum + 1);
            bool[,] occupied = new bool[request.CanvasSize.x, request.CanvasSize.y];

            if (!ReserveTerminalRooms(
                    request.CanvasSize, stageArchetype,
                    occupied, rooms, ref random))
                return false;

            Vector2Int largeSize = new Vector2Int(2, 2);
            bool hasLarge = HasFootprint(request, largeSize);
            if (request.StageSlot == P6StageSlot.X3)
            {
                bool authoredBoss =
                    CatalogSupportsRole(request, RoomRole.Boss);
                if (!hasLarge
                    || !HasRoleFootprint(
                        request, RoomRole.Landmark, largeSize)
                    || (authoredBoss
                        && !HasRoleFootprint(
                            request, RoomRole.Boss, largeSize)))
                    return false;
                RectInt landmarkBounds;
                RectInt bossBounds;
                if (!TryPlacePair(
                        largeSize, request.CanvasSize, occupied,
                        ref random, out landmarkBounds, out bossBounds))
                    return false;
                Mark(occupied, landmarkBounds);
                rooms.Add(new PackedRoom
                {
                    Id = rooms.Count,
                    Bounds = landmarkBounds,
                    DesiredArchetype = RoomArchetype.Large,
                    ReservedRole = RoomRole.Landmark
                });
                Mark(occupied, bossBounds);
                rooms.Add(new PackedRoom
                {
                    Id = rooms.Count,
                    Bounds = bossBounds,
                    DesiredArchetype = authoredBoss
                        ? RoomArchetype.Boss : RoomArchetype.Large,
                    ReservedRole = authoredBoss
                        ? RoomRole.Boss : RoomRole.None,
                    SemanticRole = RoomRole.Boss
                });
            }
            else if (hasLarge)
            {
                RectInt largeBounds;
                if (TryPlace(
                        largeSize, request.CanvasSize, occupied,
                        ref random, out largeBounds))
                {
                    Mark(occupied, largeBounds);
                    rooms.Add(new PackedRoom
                    {
                        Id = rooms.Count,
                        Bounds = largeBounds,
                        DesiredArchetype = RoomArchetype.Large,
                        ReservedRole = request.StageSlot == P6StageSlot.X2
                            ? RoomRole.Landmark : RoomRole.None
                    });
                }
                else if (request.StageSlot == P6StageSlot.X2)
                    return false;
            }

            if (request.StageSlot == P6StageSlot.X2)
            {
                RectInt shopBounds;
                if (!TryPlace(
                        new Vector2Int(2, 1), request.CanvasSize,
                        occupied, ref random, out shopBounds))
                    return false;
                Mark(occupied, shopBounds);
                rooms.Add(new PackedRoom
                {
                    Id = rooms.Count,
                    Bounds = shopBounds,
                    DesiredArchetype = RoomArchetype.HorizontalCorridor,
                    ReservedRole = RoomRole.ShopCorridor
                });

                RoomRole[] leafRoles =
                    { RoomRole.RecordRoom, RoomRole.MaruStatue };
                for (int leafIndex = 0; leafIndex < leafRoles.Length; leafIndex++)
                {
                    RectInt leafBounds;
                    if (!TryPlace(
                            Vector2Int.one, request.CanvasSize,
                            occupied, ref random, out leafBounds))
                        return false;
                    Mark(occupied, leafBounds);
                    rooms.Add(new PackedRoom
                    {
                        Id = rooms.Count,
                        Bounds = leafBounds,
                        DesiredArchetype = RoomArchetype.Small,
                        ReservedRole = leafRoles[leafIndex]
                    });
                }
            }

            if (request.StageSlot == P6StageSlot.X3)
            {
                if (!ReserveCatalogRole(
                        request, RoomRole.SupplyCorridor,
                        occupied, rooms, ref random))
                    return false;
            }

            if (stageArchetype == P6StageArchetype.Chase)
            {
                int longRooms = CountLongRooms(rooms);
                while (longRooms < 3)
                {
                    RoomArchetype desired = longRooms % 2 == 0
                        ? RoomArchetype.Wide : RoomArchetype.Tall;
                    Vector2Int size = SizeFor(desired);
                    if (!HasFootprint(request, size))
                        return false;
                    RectInt longBounds;
                    if (!TryPlace(
                            size, request.CanvasSize, occupied,
                            ref random, out longBounds))
                        return false;
                    Mark(occupied, longBounds);
                    rooms.Add(new PackedRoom
                    {
                        Id = rooms.Count,
                        Bounds = longBounds,
                        DesiredArchetype = desired
                    });
                    longRooms++;
                }
            }

            if (rooms.Count > target)
                target = rooms.Count;
            while (rooms.Count < target)
            {
                RoomArchetype desired =
                    PickRoomArchetype(stageArchetype, ref random);
                Vector2Int size = SizeFor(desired);
                if (!HasFootprint(request, size))
                {
                    desired = random.NextBool()
                        ? RoomArchetype.Standard : RoomArchetype.Small;
                    size = Vector2Int.one;
                }

                RectInt bounds;
                if (!TryPlace(size, request.CanvasSize, occupied, ref random, out bounds))
                {
                    size = Vector2Int.one;
                    desired = random.NextBool()
                        ? RoomArchetype.Standard : RoomArchetype.Small;
                    if (!TryPlace(size, request.CanvasSize, occupied, ref random, out bounds))
                        return false;
                }

                Mark(occupied, bounds);
                rooms.Add(new PackedRoom
                {
                    Id = rooms.Count,
                    Bounds = bounds,
                    DesiredArchetype = desired
                });
            }
            return rooms.Count >= request.MinRooms;
        }

        private static RoomArchetype PickRoomArchetype(
            P6StageArchetype stageArchetype,
            ref P6DeterministicRandom random)
        {
            int roll = random.NextInt(100);
            if (stageArchetype == P6StageArchetype.Chase)
            {
                if (roll < 40) return RoomArchetype.Wide;
                if (roll < 70) return RoomArchetype.Tall;
                if (roll < 85) return RoomArchetype.Standard;
                if (roll < 95) return RoomArchetype.Small;
                return RoomArchetype.Large;
            }
            if (roll < 25) return RoomArchetype.Small;
            if (roll < 55) return RoomArchetype.Standard;
            if (roll < 75) return RoomArchetype.Wide;
            if (roll < 90) return RoomArchetype.Tall;
            return RoomArchetype.Large;
        }

        private static bool ReserveTerminalRooms(
            Vector2Int canvas,
            P6StageArchetype archetype,
            bool[,] occupied,
            List<PackedRoom> rooms,
            ref P6DeterministicRandom random)
        {
            Vector2Int start;
            Vector2Int exit;
            if (archetype == P6StageArchetype.Descent)
            {
                start = new Vector2Int(random.NextInt(canvas.x), canvas.y - 1);
                exit = new Vector2Int(random.NextInt(canvas.x), 0);
            }
            else if (archetype == P6StageArchetype.Ascent)
            {
                start = new Vector2Int(random.NextInt(canvas.x), 0);
                exit = new Vector2Int(random.NextInt(canvas.x), canvas.y - 1);
            }
            else
            {
                start = new Vector2Int(0, random.NextInt(canvas.y));
                exit = new Vector2Int(canvas.x - 1, random.NextInt(canvas.y));
            }

            if (start == exit) return false;
            var startBounds = new RectInt(start, Vector2Int.one);
            var exitBounds = new RectInt(exit, Vector2Int.one);
            Mark(occupied, startBounds);
            rooms.Add(new PackedRoom
            {
                Id = rooms.Count,
                Bounds = startBounds,
                DesiredArchetype = RoomArchetype.Standard,
                ForcedStart = true
            });
            Mark(occupied, exitBounds);
            rooms.Add(new PackedRoom
            {
                Id = rooms.Count,
                Bounds = exitBounds,
                DesiredArchetype = RoomArchetype.Standard,
                ForcedExit = true
            });
            return true;
        }

        private static bool ReserveCatalogRole(
            P6GenerationRequest request,
            RoomRole role,
            bool[,] occupied,
            List<PackedRoom> rooms,
            ref P6DeterministicRandom random)
        {
            P6RoomPrefabDescriptor chosen = null;
            for (int index = 0; index < request.Prefabs.Count; index++)
            {
                P6RoomPrefabDescriptor prefab = request.Prefabs[index];
                if (prefab == null
                    || (prefab.Region != request.Region
                        && prefab.Region != RoomRegion.Universal)
                    || !prefab.Supports(role)
                    || !prefab.HasValidatedTraversalProof
                    || prefab.SocketSides == P6SocketSides.None)
                    continue;
                if (chosen == null
                    || string.CompareOrdinal(prefab.PrefabId, chosen.PrefabId) < 0)
                    chosen = prefab;
            }
            if (chosen == null) return false;

            RectInt bounds;
            if (!TryPlace(
                    chosen.MacroSize, request.CanvasSize, occupied,
                    ref random, out bounds))
                return false;
            Mark(occupied, bounds);
            rooms.Add(new PackedRoom
            {
                Id = rooms.Count,
                Bounds = bounds,
                DesiredArchetype = chosen.Archetype,
                ReservedRole = role
            });
            return true;
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
                    && prefab.HasValidatedTraversalProof
                    && prefab.Supports(role))
                    return true;
            }
            return false;
        }

        private static int CountLongRooms(List<PackedRoom> rooms)
        {
            int count = 0;
            for (int index = 0; index < rooms.Count; index++)
                if (rooms[index].DesiredArchetype == RoomArchetype.Wide
                    || rooms[index].DesiredArchetype == RoomArchetype.Tall
                    || rooms[index].DesiredArchetype
                        == RoomArchetype.HorizontalCorridor
                    || rooms[index].DesiredArchetype
                        == RoomArchetype.VerticalCorridor)
                    count++;
            return count;
        }

        private static Vector2Int SizeFor(RoomArchetype archetype)
        {
            switch (archetype)
            {
                case RoomArchetype.Wide:
                case RoomArchetype.HorizontalCorridor: return new Vector2Int(2, 1);
                case RoomArchetype.Tall:
                case RoomArchetype.VerticalCorridor: return new Vector2Int(1, 2);
                case RoomArchetype.Large: return new Vector2Int(2, 2);
                default: return Vector2Int.one;
            }
        }

        private static bool HasFootprint(P6GenerationRequest request, Vector2Int size)
        {
            for (int index = 0; index < request.Prefabs.Count; index++)
            {
                P6RoomPrefabDescriptor prefab = request.Prefabs[index];
                if (prefab != null && prefab.MacroSize == size
                    && prefab.HasValidatedTraversalProof
                    && prefab.SocketSides != P6SocketSides.None
                    && (prefab.Region == request.Region
                        || prefab.Region == RoomRegion.Universal))
                    return true;
            }
            return false;
        }

        private static bool HasRoleFootprint(
            P6GenerationRequest request,
            RoomRole role,
            Vector2Int size)
        {
            for (int index = 0; index < request.Prefabs.Count; index++)
            {
                P6RoomPrefabDescriptor prefab = request.Prefabs[index];
                if (prefab != null
                    && prefab.MacroSize == size
                    && prefab.HasValidatedTraversalProof
                    && prefab.SocketSides != P6SocketSides.None
                    && (prefab.Region == request.Region
                        || prefab.Region == RoomRegion.Universal)
                    && prefab.Supports(role))
                    return true;
            }
            return false;
        }

        private static bool TryPlacePair(
            Vector2Int size,
            Vector2Int canvas,
            bool[,] occupied,
            ref P6DeterministicRandom random,
            out RectInt first,
            out RectInt second)
        {
            var pairs = new List<RectInt[]>();
            for (int firstY = 0; firstY <= canvas.y - size.y; firstY++)
                for (int firstX = 0; firstX <= canvas.x - size.x; firstX++)
                {
                    var firstBounds =
                        new RectInt(firstX, firstY, size.x, size.y);
                    if (!IsFree(occupied, firstBounds)) continue;
                    for (int secondY = 0;
                         secondY <= canvas.y - size.y;
                         secondY++)
                        for (int secondX = 0;
                             secondX <= canvas.x - size.x;
                             secondX++)
                        {
                            var secondBounds =
                                new RectInt(
                                    secondX, secondY, size.x, size.y);
                            if (secondY < firstY
                                || (secondY == firstY
                                    && secondX <= firstX)
                                || firstBounds.Overlaps(secondBounds)
                                || !IsFree(occupied, secondBounds))
                                continue;
                            pairs.Add(new[] { firstBounds, secondBounds });
                        }
                }
            if (pairs.Count == 0)
            {
                first = default(RectInt);
                second = default(RectInt);
                return false;
            }
            RectInt[] selected = pairs[random.NextInt(pairs.Count)];
            first = selected[0];
            second = selected[1];
            return true;
        }

        private static bool TryPlace(
            Vector2Int size, Vector2Int canvas, bool[,] occupied,
            ref P6DeterministicRandom random, out RectInt result)
        {
            int maximumX = canvas.x - size.x;
            int maximumY = canvas.y - size.y;
            if (maximumX < 0 || maximumY < 0)
            {
                result = default(RectInt);
                return false;
            }

            Vector2Int preferred = new Vector2Int(
                random.NextInt(maximumX + 1),
                random.NextInt(maximumY + 1));
            var preferredBounds = new RectInt(preferred, size);
            if (IsFree(occupied, preferredBounds))
            {
                result = preferredBounds;
                return true;
            }

            Vector2 occupiedCenter = CalculateOccupiedCenter(
                occupied,
                canvas);
            Vector2 preferredCenter = new Vector2(
                preferred.x + size.x * 0.5f,
                preferred.y + size.y * 0.5f);
            Vector2 outwardDirection = preferredCenter - occupiedCenter;
            if (outwardDirection.sqrMagnitude < 0.001f)
            {
                outwardDirection = new Vector2(
                    random.NextBool() ? 1f : -1f,
                    random.NextBool() ? 1f : -1f);
            }

            int nearestDistance = int.MaxValue;
            float strongestOutwardScore = float.MinValue;
            var nearestPositions = new List<Vector2Int>();
            for (int y = 0; y <= canvas.y - size.y; y++)
                for (int x = 0; x <= canvas.x - size.x; x++)
                {
                    var bounds = new RectInt(x, y, size.x, size.y);
                    if (!IsFree(occupied, bounds))
                    {
                        continue;
                    }

                    int distance =
                        Mathf.Abs(x - preferred.x)
                        + Mathf.Abs(y - preferred.y);
                    Vector2 candidateCenter = new Vector2(
                        x + size.x * 0.5f,
                        y + size.y * 0.5f);
                    float outwardScore = Vector2.Dot(
                        candidateCenter - occupiedCenter,
                        outwardDirection);
                    if (distance < nearestDistance
                        || (distance == nearestDistance
                            && outwardScore
                                > strongestOutwardScore + 0.001f))
                    {
                        nearestDistance = distance;
                        strongestOutwardScore = outwardScore;
                        nearestPositions.Clear();
                        nearestPositions.Add(new Vector2Int(x, y));
                    }
                    else if (distance == nearestDistance
                        && Mathf.Abs(
                            outwardScore - strongestOutwardScore)
                            <= 0.001f)
                    {
                        nearestPositions.Add(new Vector2Int(x, y));
                    }
                }
            if (nearestPositions.Count == 0)
            {
                result = default(RectInt);
                return false;
            }

            Vector2Int position =
                nearestPositions[random.NextInt(nearestPositions.Count)];
            result = new RectInt(position, size);
            return true;
        }

        private static Vector2 CalculateOccupiedCenter(
            bool[,] occupied,
            Vector2Int canvas)
        {
            Vector2 sum = Vector2.zero;
            int count = 0;
            for (int y = 0; y < canvas.y; y++)
            {
                for (int x = 0; x < canvas.x; x++)
                {
                    if (!occupied[x, y])
                    {
                        continue;
                    }

                    sum += new Vector2(x + 0.5f, y + 0.5f);
                    count++;
                }
            }

            return count > 0
                ? sum / count
                : new Vector2(canvas.x * 0.5f, canvas.y * 0.5f);
        }

        private static bool IsFree(bool[,] occupied, RectInt bounds)
        {
            foreach (Vector2Int cell in bounds.allPositionsWithin)
                if (occupied[cell.x, cell.y]) return false;
            return true;
        }

        private static void Mark(bool[,] occupied, RectInt bounds)
        {
            foreach (Vector2Int cell in bounds.allPositionsWithin)
                occupied[cell.x, cell.y] = true;
        }

        private static List<int> OrderForArchetype(
            List<PackedRoom> rooms, P6StageArchetype archetype)
        {
            var order = new List<int>();
            for (int index = 0; index < rooms.Count; index++) order.Add(index);
            order.Sort((first, second) =>
            {
                Vector2Int a = Center(rooms[first].Bounds);
                Vector2Int b = Center(rooms[second].Bounds);
                int primary;
                int secondary;
                if (archetype == P6StageArchetype.Descent)
                {
                    primary = b.y.CompareTo(a.y);
                    secondary = a.x.CompareTo(b.x);
                }
                else if (archetype == P6StageArchetype.Ascent)
                {
                    primary = a.y.CompareTo(b.y);
                    secondary = a.x.CompareTo(b.x);
                }
                else
                {
                    primary = a.x.CompareTo(b.x);
                    secondary = a.y.CompareTo(b.y);
                }
                if (primary != 0) return primary;
                if (secondary != 0) return secondary;
                return first.CompareTo(second);
            });
            return order;
        }

        private static List<int> EvenlySelect(List<int> ordered, int count)
        {
            var selected = new List<int>();
            int previous = -1;
            for (int index = 0; index < count; index++)
            {
                int source = count == 1 ? 0 :
                    Mathf.RoundToInt(index * (ordered.Count - 1f) / (count - 1f));
                if (source <= previous) source = previous + 1;
                if (source >= ordered.Count) source = ordered.Count - 1;
                selected.Add(ordered[source]);
                previous = source;
            }
            return selected;
        }

        private static List<int> BuildMainPath(
            List<PackedRoom> rooms,
            List<int> ordered,
            P6StageSlot slot,
            P6StageArchetype archetype,
            Vector2Int canvas,
            ref P6DeterministicRandom random)
        {
            int startId = -1;
            int exitId = -1;
            var middleEligible = new List<int>();
            var optionalEligible = new List<int>();
            for (int index = 0; index < ordered.Count; index++)
            {
                PackedRoom room = rooms[ordered[index]];
                if (room.ForcedStart)
                {
                    startId = room.Id;
                    continue;
                }
                if (room.ForcedExit)
                {
                    exitId = room.Id;
                    continue;
                }
                if (room.ReservedRole == RoomRole.Landmark
                    || room.ReservedRole == RoomRole.RecordRoom
                    || room.ReservedRole == RoomRole.MaruStatue)
                {
                    optionalEligible.Add(room.Id);
                    continue;
                }
                middleEligible.Add(room.Id);
            }

            int riskyChoiceId = ChooseRiskyChoiceRoom(
                rooms, middleEligible, optionalEligible);
            if (riskyChoiceId < 0)
                return new List<int>();
            rooms[riskyChoiceId].SemanticRole |= RoomRole.RiskyChoice;
            middleEligible.Remove(riskyChoiceId);

            int maximum = Math.Min(9, middleEligible.Count + 2);
            if (startId < 0 || exitId < 0 || maximum < 6)
                return new List<int>();
            int count = random.NextInt(6, maximum + 1);
            List<int> middle = EvenlySelect(middleEligible, count - 2);

            if (slot == P6StageSlot.X2)
                ForceReservedRole(
                    rooms, middleEligible, middle,
                    RoomRole.ShopCorridor, middle.Count / 2);
            if (slot == P6StageSlot.X3)
            {
                ForceReservedRole(
                    rooms, middleEligible, middle,
                    RoomRole.SupplyCorridor, Math.Max(0, middle.Count - 2));
                ForceAssignedRole(
                    rooms, middleEligible, middle,
                    RoomRole.Boss, Math.Max(0, middle.Count - 1));
            }

            if (archetype == P6StageArchetype.Circuit && middle.Count > 0)
            {
                Vector2Int canvasCenter =
                    new Vector2Int(canvas.x / 2, canvas.y / 2);
                int hubId = middleEligible[0];
                int bestDistance = int.MaxValue;
                for (int index = 0; index < middleEligible.Count; index++)
                {
                    int candidate = middleEligible[index];
                    int distance =
                        Manhattan(Center(rooms[candidate].Bounds), canvasCenter);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        hubId = candidate;
                    }
                }
                ForceMiddleId(
                    rooms, middleEligible, middle,
                    hubId, middle.Count / 2);
            }

            middle.Sort((first, second) =>
                middleEligible.IndexOf(first).CompareTo(
                    middleEligible.IndexOf(second)));
            if (slot == P6StageSlot.X3)
                OrderX3TrailingRoles(rooms, middle);
            var result = new List<int>(count) { startId };
            result.AddRange(middle);
            result.Add(exitId);
            return result;
        }

        private static int ChooseRiskyChoiceRoom(
            List<PackedRoom> rooms,
            List<int> middleEligible,
            List<int> optionalEligible)
        {
            for (int index = 0; index < optionalEligible.Count; index++)
                if (rooms[optionalEligible[index]].ReservedRole
                    == RoomRole.Landmark)
                    return optionalEligible[index];

            for (int index = middleEligible.Count - 1; index >= 0; index--)
            {
                int roomId = middleEligible[index];
                if (rooms[roomId].ReservedRole == RoomRole.None
                    && rooms[roomId].SemanticRole == RoomRole.None)
                    return roomId;
            }

            return optionalEligible.Count > 0
                ? optionalEligible[optionalEligible.Count - 1]
                : -1;
        }

        private static void ForceReservedRole(
            List<PackedRoom> rooms,
            List<int> eligible,
            List<int> selected,
            RoomRole role,
            int preferredIndex)
        {
            for (int index = 0; index < eligible.Count; index++)
                if (rooms[eligible[index]].ReservedRole == role)
                {
                    ForceMiddleId(
                        rooms, eligible, selected,
                        eligible[index], preferredIndex);
                    return;
                }
        }

        private static void ForceAssignedRole(
            List<PackedRoom> rooms,
            List<int> eligible,
            List<int> selected,
            RoomRole role,
            int preferredIndex)
        {
            for (int index = 0; index < eligible.Count; index++)
                if (((rooms[eligible[index]].ReservedRole
                    | rooms[eligible[index]].SemanticRole) & role) != 0)
                {
                    ForceMiddleId(
                        rooms, eligible, selected,
                        eligible[index], preferredIndex);
                    return;
                }
        }

        private static void ForceMiddleId(
            List<PackedRoom> rooms,
            List<int> eligible,
            List<int> selected,
            int roomId,
            int preferredIndex)
        {
            if (selected.Count == 0 || selected.Contains(roomId)) return;
            int preferred =
                Mathf.Clamp(preferredIndex, 0, selected.Count - 1);
            for (int offset = 0; offset < selected.Count; offset++)
            {
                int replace = (preferred + offset) % selected.Count;
                RoomRole assigned =
                    rooms[selected[replace]].ReservedRole
                    | rooms[selected[replace]].SemanticRole;
                if ((assigned & (RoomRole.ShopCorridor
                    | RoomRole.SupplyCorridor
                    | RoomRole.Boss)) != 0)
                    continue;
                selected[replace] = roomId;
                return;
            }
        }

        private static void OrderX3TrailingRoles(
            List<PackedRoom> rooms,
            List<int> middle)
        {
            int supply = -1;
            int boss = -1;
            for (int index = 0; index < middle.Count; index++)
            {
                RoomRole assigned =
                    rooms[middle[index]].ReservedRole
                    | rooms[middle[index]].SemanticRole;
                if ((assigned & RoomRole.SupplyCorridor) != 0)
                    supply = middle[index];
                if ((assigned & RoomRole.Boss) != 0)
                    boss = middle[index];
            }

            if (supply >= 0) middle.Remove(supply);
            if (boss >= 0) middle.Remove(boss);
            if (supply >= 0) middle.Add(supply);
            if (boss >= 0) middle.Add(boss);
        }

        private static HashSet<long> BuildPreferredTree(
            List<PackedRoom> rooms,
            List<int> mainPath,
            P6StageArchetype archetype)
        {
            var selected = new HashSet<long>();
            var connected = new HashSet<int>();
            for (int index = 0; index < mainPath.Count; index++)
            {
                connected.Add(mainPath[index]);
                if (index > 0) selected.Add(Key(mainPath[index - 1], mainPath[index]));
            }
            int circuitAttachments = 0;
            bool forkBranchAttached = false;
            for (int room = 0; room < rooms.Count; room++)
            {
                if (connected.Contains(room)) continue;
                int nearest = -1;
                if ((rooms[room].SemanticRole & RoomRole.RiskyChoice) != 0)
                {
                    nearest = FindRiskyChoiceAnchor(
                        rooms, mainPath, room);
                }
                else if (archetype == P6StageArchetype.Circuit
                    && circuitAttachments < 2)
                {
                    nearest = mainPath[mainPath.Count / 2];
                    circuitAttachments++;
                }
                else if (archetype == P6StageArchetype.Fork
                    && !forkBranchAttached)
                {
                    nearest = mainPath[Math.Min(2, mainPath.Count - 2)];
                    forkBranchAttached = true;
                }
                int best = int.MaxValue;
                for (int mainIndex = 0;
                     nearest < 0 && mainIndex < mainPath.Count;
                     mainIndex++)
                {
                    int other = mainPath[mainIndex];
                    int score = SpatialWeight(rooms[room].Bounds, rooms[other].Bounds);
                    if (score < best || (score == best && other < nearest))
                    {
                        best = score;
                        nearest = other;
                    }
                }
                selected.Add(Key(room, nearest));
                connected.Add(room);
            }
            return selected;
        }

        private static int FindRiskyChoiceAnchor(
            List<PackedRoom> rooms,
            List<int> mainPath,
            int riskyChoiceId)
        {
            int selected = -1;
            int best = int.MaxValue;
            int maximumDistance = Math.Min(4, mainPath.Count - 1);
            for (int exitDistance = 2;
                 exitDistance <= maximumDistance;
                 exitDistance++)
            {
                int candidate =
                    mainPath[mainPath.Count - 1 - exitDistance];
                int score = SpatialWeight(
                    rooms[riskyChoiceId].Bounds,
                    rooms[candidate].Bounds);
                if (score < best
                    || (score == best && candidate < selected))
                {
                    best = score;
                    selected = candidate;
                }
            }
            return selected;
        }

        private static List<P6CandidateEdge> BuildCandidates(
            List<PackedRoom> rooms,
            HashSet<long> preferredTree,
            Vector2Int canvas)
        {
            var result = new List<P6CandidateEdge>();
            int maximumCandidateDistance =
                Math.Max(3, Math.Max(canvas.x, canvas.y) - 1);
            for (int first = 0; first < rooms.Count; first++)
                for (int second = first + 1; second < rooms.Count; second++)
                {
                    bool required = preferredTree.Contains(Key(first, second));
                    int centerDistance = Manhattan(
                        Center(rooms[first].Bounds),
                        Center(rooms[second].Bounds));
                    if (!required && centerDistance > maximumCandidateDistance)
                        continue;
                    int spatial = SpatialWeight(rooms[first].Bounds, rooms[second].Bounds);
                    int tie = first * rooms.Count + second;
                    int weight = required
                        ? spatial * 10 + tie
                        : 10000 + spatial * 10 + tie;
                    result.Add(new P6CandidateEdge(first, second, weight));
                }
            result.Sort(CompareCandidate);
            return result;
        }

        private static List<P6CandidateEdge> BuildMst(
            int roomCount, List<P6CandidateEdge> candidates)
        {
            var result = new List<P6CandidateEdge>();
            var sets = new UnionFind(roomCount);
            for (int index = 0; index < candidates.Count; index++)
            {
                P6CandidateEdge edge = candidates[index];
                if (sets.Join(edge.FirstNodeId, edge.SecondNodeId))
                    result.Add(edge);
                if (result.Count == roomCount - 1) break;
            }
            return result;
        }

        private static List<P6CandidateEdge> SelectAdditional(
            List<P6CandidateEdge> candidates,
            List<P6CandidateEdge> mst,
            int count,
            HashSet<int> protectedLeaves,
            List<PackedRoom> rooms,
            List<int> mainPath,
            P6StageArchetype archetype,
            ref P6DeterministicRandom random)
        {
            var mstKeys = new HashSet<long>();
            for (int index = 0; index < mst.Count; index++)
                mstKeys.Add(Key(mst[index].FirstNodeId, mst[index].SecondNodeId));
            var available = new List<P6CandidateEdge>();
            for (int index = 0; index < candidates.Count; index++)
                if (!mstKeys.Contains(Key(
                        candidates[index].FirstNodeId, candidates[index].SecondNodeId))
                    && !protectedLeaves.Contains(candidates[index].FirstNodeId)
                    && !protectedLeaves.Contains(candidates[index].SecondNodeId))
                    available.Add(candidates[index]);
            var result = new List<P6CandidateEdge>();
            if (archetype == P6StageArchetype.Fork)
            {
                var main = new HashSet<int>(mainPath);
                for (int index = 0; index < available.Count; index++)
                {
                    P6CandidateEdge candidate = available[index];
                    bool firstMain = main.Contains(candidate.FirstNodeId);
                    bool secondMain = main.Contains(candidate.SecondNodeId);
                    if (firstMain == secondMain) continue;
                    int branch = firstMain
                        ? candidate.SecondNodeId : candidate.FirstNodeId;
                    if (rooms[branch].ForcedStart || rooms[branch].ForcedExit)
                        continue;
                    result.Add(candidate);
                    available.RemoveAt(index);
                    break;
                }
            }
            int offset = available.Count == 0 ? 0 : random.NextInt(Math.Min(available.Count, 5));
            for (int index = 0;
                 result.Count < count && index < available.Count;
                 index++)
                result.Add(available[(offset + index) % available.Count]);
            return result;
        }

        private static void EnsureMaruStatueShortcut(
            List<PackedRoom> rooms,
            List<int> mainPath,
            List<P6CandidateEdge> mst,
            List<P6CandidateEdge> additional)
        {
            int statueId = -1;
            for (int index = 0; index < rooms.Count; index++)
                if (rooms[index].ReservedRole == RoomRole.MaruStatue)
                {
                    statueId = index;
                    break;
                }
            if (statueId < 0 || mainPath.Count < 3) return;

            int[] exitHops = BuildExitHopDistances(
                rooms.Count, mst, additional,
                mainPath[mainPath.Count - 1]);
            if (HasExitShortcut(exitHops, statueId)) return;

            var selected = new HashSet<long>();
            AddEdgeKeys(selected, mst);
            AddEdgeKeys(selected, additional);
            int anchor = -1;
            int best = int.MaxValue;
            for (int index = 1; index < mainPath.Count - 1; index++)
            {
                int candidate = mainPath[index];
                if (candidate == statueId
                    || exitHops[candidate] < MaruStatueMinExitHops - 1
                    || exitHops[candidate] > MaruStatueMaxExitHops - 1
                    || selected.Contains(Key(statueId, candidate)))
                    continue;
                int score = SpatialWeight(
                    rooms[statueId].Bounds, rooms[candidate].Bounds);
                if (score < best || (score == best && candidate < anchor))
                {
                    best = score;
                    anchor = candidate;
                }
            }
            if (anchor < 0) return;

            int low = Math.Min(statueId, anchor);
            int high = Math.Max(statueId, anchor);
            if (additional.Count >= 3)
                additional.RemoveRange(2, additional.Count - 2);
            additional.Add(new P6CandidateEdge(
                low, high, 10000 + best * 10 + low * rooms.Count + high));
        }

        private static void AddEdgeKeys(
            HashSet<long> keys, List<P6CandidateEdge> edges)
        {
            for (int index = 0; index < edges.Count; index++)
                keys.Add(Key(
                    edges[index].FirstNodeId, edges[index].SecondNodeId));
        }

        private static bool HasExitShortcut(int[] exitHops, int roomId)
        {
            return roomId >= 0
                && roomId < exitHops.Length
                && exitHops[roomId] >= 0
                && exitHops[roomId]
                    <= P6RoomGraphValidator.MaruStatueShortcutMaxHops;
        }

        private static int[] BuildExitHopDistances(
            int roomCount,
            List<P6CandidateEdge> mst,
            List<P6CandidateEdge> additional,
            int exitId)
        {
            var distances = new int[roomCount];
            var adjacency = new List<int>[roomCount];
            for (int index = 0; index < roomCount; index++)
            {
                distances[index] = -1;
                adjacency[index] = new List<int>();
            }
            AddAdjacency(adjacency, mst);
            AddAdjacency(adjacency, additional);
            if (exitId < 0 || exitId >= roomCount) return distances;

            var queue = new Queue<int>();
            distances[exitId] = 0;
            queue.Enqueue(exitId);
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

        private static void AddAdjacency(
            List<int>[] adjacency, List<P6CandidateEdge> edges)
        {
            for (int index = 0; index < edges.Count; index++)
            {
                P6CandidateEdge edge = edges[index];
                if (edge.FirstNodeId < 0
                    || edge.FirstNodeId >= adjacency.Length
                    || edge.SecondNodeId < 0
                    || edge.SecondNodeId >= adjacency.Length)
                    continue;
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }
        }

        private static void AssignDegreesAndRoles(
            List<PackedRoom> rooms, List<int> mainPath,
            List<P6CandidateEdge> mst, List<P6CandidateEdge> additional)
        {
            int[] degree = new int[rooms.Count];
            CountDegrees(mst, degree);
            CountDegrees(additional, degree);
            var main = new HashSet<int>(mainPath);
            for (int index = 0; index < rooms.Count; index++)
            {
                rooms[index].Degree = degree[index];
                rooms[index].Main = main.Contains(index);
                RoomRole role =
                    rooms[index].ReservedRole | rooms[index].SemanticRole;
                if (rooms[index].Main) role |= RoomRole.Main;
                if (index == mainPath[0]) role |= RoomRole.Start;
                else if (index == mainPath[mainPath.Count - 1])
                    role |= RoomRole.Exit;
                else if (!rooms[index].Main
                    && role == RoomRole.None
                    && degree[index] <= 1)
                    role |= index % 3 == 0
                        ? RoomRole.RecordRoom
                        : (index % 3 == 1
                            ? RoomRole.MaruStatue
                            : RoomRole.RelicRoom);
                else if (!rooms[index].Main
                    && role == RoomRole.None)
                    role |= RoomRole.Shortcut;

                if (degree[index] >= 3)
                    role |= RoomRole.LandmarkJunction;
                rooms[index].Role = role;
            }
        }

        private static void CountDegrees(List<P6CandidateEdge> edges, int[] degree)
        {
            for (int index = 0; index < edges.Count; index++)
            {
                degree[edges[index].FirstNodeId]++;
                degree[edges[index].SecondNodeId]++;
            }
        }

        private static bool SelectPrefabs(
            P6GenerationRequest request, List<PackedRoom> rooms,
            ref P6DeterministicRandom random)
        {
            var usage = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
            {
                PackedRoom room = rooms[roomIndex];
                List<P6RoomPrefabDescriptor> candidates =
                    FindPrefabCandidates(
                        request, room, usage, room.ReservedRole);
                if (candidates.Count == 0) return false;
                int total = 0;
                for (int index = 0; index < candidates.Count; index++)
                {
                    int category = candidates[index].Archetype == room.DesiredArchetype ? 4 : 1;
                    total += candidates[index].SelectionWeight * category;
                }
                int roll = random.NextInt(total);
                P6RoomPrefabDescriptor chosen = candidates[0];
                for (int index = 0; index < candidates.Count; index++)
                {
                    int category = candidates[index].Archetype == room.DesiredArchetype ? 4 : 1;
                    roll -= candidates[index].SelectionWeight * category;
                    if (roll < 0) { chosen = candidates[index]; break; }
                }
                room.Prefab = chosen;
                int count;
                usage.TryGetValue(chosen.PrefabId, out count);
                usage[chosen.PrefabId] = count + 1;
            }
            return true;
        }

        private static List<P6RoomPrefabDescriptor> FindPrefabCandidates(
            P6GenerationRequest request, PackedRoom room,
            Dictionary<string, int> usage,
            RoomRole requiredAuthoredRole)
        {
            var result = new List<P6RoomPrefabDescriptor>();
            for (int index = 0; index < request.Prefabs.Count; index++)
            {
                P6RoomPrefabDescriptor prefab = request.Prefabs[index];
                if (prefab == null || prefab.MacroSize != room.Bounds.size
                    || !prefab.HasValidatedTraversalProof
                    || prefab.SocketSides == P6SocketSides.None
                    || (prefab.Region != request.Region
                        && prefab.Region != RoomRegion.Universal)
                    || (room.Main && !prefab.MainRouteAllowed)
                    || !DescriptorSupportsPlacement(prefab, room))
                    continue;
                int count;
                usage.TryGetValue(prefab.PrefabId, out count);
                if (count >= prefab.MaxUses || count >= 2) continue;
                if (requiredAuthoredRole != RoomRole.None
                    && !prefab.Supports(requiredAuthoredRole))
                    continue;
                result.Add(prefab);
            }
            result.Sort((a, b) => string.CompareOrdinal(a.PrefabId, b.PrefabId));
            return result;
        }

        private static bool DescriptorSupportsPlacement(
            P6RoomPrefabDescriptor prefab, PackedRoom room)
        {
            if (prefab == null || !prefab.HasValidatedTraversalProof)
                return false;
            P6RoomTraversalProof proof = prefab.TraversalProof;
            int validSockets = 0;
            for (int index = 0; index < proof.Sockets.Count; index++)
                if (IsValidMainSocket(proof.Sockets[index])) validSockets++;
            if (validSockets == 0) return false;
            if (!room.Main || room.ForcedStart || room.ForcedExit)
                return true;
            for (int index = 0; index < proof.Links.Count; index++)
            {
                P6SocketLinkProof link = proof.Links[index];
                if (link.Kind == P6SocketLinkKind.MainRoutePair
                    && link.Passed
                    && !string.Equals(
                        link.FromSocketId,
                        link.ToSocketId,
                        StringComparison.Ordinal)
                    && IsValidMainSocket(
                        proof.FindSocket(link.FromSocketId))
                    && IsValidMainSocket(
                        proof.FindSocket(link.ToSocketId)))
                    return true;
            }
            return false;
        }

        private static List<P6RoomNode> BuildNodes(List<PackedRoom> rooms)
        {
            var nodes = new List<P6RoomNode>(rooms.Count);
            for (int index = 0; index < rooms.Count; index++)
            {
                var node = new P6RoomNode(
                    rooms[index].Id, rooms[index].Bounds, rooms[index].Prefab,
                    rooms[index].Role, rooms[index].Main);
                node.Degree = rooms[index].Degree;
                nodes.Add(node);
            }
            return nodes;
        }

        private static bool BuildAccessAssignments(
            List<P6RoomNode> nodes,
            List<int> mainPath,
            List<P6CandidateEdge> mst,
            List<P6CandidateEdge> additional,
            P6StageArchetype archetype,
            out List<P6RoomAccess> accesses,
            out Dictionary<long, EdgeAccessAssignment> edgeAccesses)
        {
            accesses = new List<P6RoomAccess>();
            edgeAccesses = new Dictionary<long, EdgeAccessAssignment>();
            var byNode = new Dictionary<int, List<int>>();
            var bySocket = new Dictionary<string, int>(StringComparer.Ordinal);
            var incoming = new Dictionary<int, int>();
            var outgoing = new Dictionary<int, int>();

            for (int index = 0; index < mainPath.Count; index++)
            {
                P6RoomNode node = nodes[mainPath[index]];
                P6SocketTraversalProof entry = null;
                P6SocketTraversalProof departure = null;
                if (index == 0)
                {
                    departure = ChooseMainSocket(node, archetype, false);
                }
                else if (index == mainPath.Count - 1)
                {
                    entry = ChooseMainSocket(node, archetype, true);
                }
                else if (!TryChooseMainPair(
                             node, archetype, out entry, out departure))
                {
                    return false;
                }

                if (entry != null)
                    incoming[node.Id] = GetOrCreateAccess(
                        node, entry, accesses, byNode, bySocket);
                if (departure != null)
                    outgoing[node.Id] = GetOrCreateAccess(
                        node, departure, accesses, byNode, bySocket);
            }

            for (int index = 1; index < mainPath.Count; index++)
            {
                int fromNode = mainPath[index - 1];
                int toNode = mainPath[index];
                if (!outgoing.ContainsKey(fromNode)
                    || !incoming.ContainsKey(toNode))
                    return false;
                edgeAccesses[Key(fromNode, toNode)] =
                    CreateAssignment(
                        fromNode, outgoing[fromNode],
                        toNode, incoming[toNode]);
            }

            if (!AssignRemainingEdges(
                    nodes, mst, false, accesses, byNode, bySocket,
                    edgeAccesses))
                return false;
            if (!AssignRemainingEdges(
                    nodes, additional, true, accesses, byNode, bySocket,
                    edgeAccesses))
                return false;
            return true;
        }

        private static bool AssignRemainingEdges(
            List<P6RoomNode> nodes,
            List<P6CandidateEdge> edges,
            bool reuseOnly,
            List<P6RoomAccess> accesses,
            Dictionary<int, List<int>> byNode,
            Dictionary<string, int> bySocket,
            Dictionary<long, EdgeAccessAssignment> edgeAccesses)
        {
            for (int index = 0; index < edges.Count; index++)
            {
                P6CandidateEdge edge = edges[index];
                long key = Key(edge.FirstNodeId, edge.SecondNodeId);
                if (edgeAccesses.ContainsKey(key)) continue;
                int firstAccess = ChooseEdgeAccess(
                    nodes[edge.FirstNodeId],
                    nodes[edge.SecondNodeId],
                    reuseOnly, accesses, byNode, bySocket);
                int secondAccess = ChooseEdgeAccess(
                    nodes[edge.SecondNodeId],
                    nodes[edge.FirstNodeId],
                    reuseOnly, accesses, byNode, bySocket);
                if (firstAccess < 0 || secondAccess < 0) return false;
                edgeAccesses[key] = CreateAssignment(
                    edge.FirstNodeId, firstAccess,
                    edge.SecondNodeId, secondAccess);
            }
            return true;
        }

        private static int ChooseEdgeAccess(
            P6RoomNode node,
            P6RoomNode other,
            bool reuseOnly,
            List<P6RoomAccess> accesses,
            Dictionary<int, List<int>> byNode,
            Dictionary<string, int> bySocket)
        {
            List<int> existing;
            byNode.TryGetValue(node.Id, out existing);
            bool hostsMaruStatue =
                P6RoomGraphValidator.HostsMaruStatue(node);
            bool singleAccessRoom =
                (node.Role & (RoomRole.Start | RoomRole.Exit
                    | RoomRole.RecordRoom
                    | RoomRole.RiskyChoice)) != 0
                || ((node.Role & RoomRole.MaruStatue) != 0
                    && !hostsMaruStatue)
                || node.TraversalProof.Sockets.Count == 1;
            // An authored homecoming statue opens one door per socket, so its
            // Exit shortcut may claim a second socket instead of reusing one.
            if ((!reuseOnly || hostsMaruStatue) && !singleAccessRoom)
            {
                P6SocketTraversalProof unused = ChooseUnusedSocket(
                    node, other, existing, accesses);
                if (unused != null)
                    return GetOrCreateAccess(
                        node, unused, accesses, byNode, bySocket);
            }
            if (existing != null && existing.Count > 0)
                return ChooseNearestExistingAccess(
                    existing, accesses, other.MacroBounds);

            P6SocketTraversalProof socket =
                ChooseMainSocket(node, P6StageArchetype.Traverse, false);
            return socket == null
                ? -1
                : GetOrCreateAccess(
                    node, socket, accesses, byNode, bySocket);
        }

        private static P6SocketTraversalProof ChooseUnusedSocket(
            P6RoomNode node,
            P6RoomNode other,
            List<int> existing,
            List<P6RoomAccess> accesses)
        {
            P6SocketTraversalProof best = null;
            int bestScore = int.MaxValue;
            for (int index = 0; index < node.TraversalProof.Sockets.Count; index++)
            {
                P6SocketTraversalProof socket =
                    node.TraversalProof.Sockets[index];
                if (!socket.IsToolFreeMainRouteSocket
                    || IsSocketUsed(socket.SocketId, existing, accesses))
                    continue;
                int score = SocketDirectionScore(
                    socket.Side,
                    Center(other.MacroBounds) - Center(node.MacroBounds));
                if (score < bestScore)
                {
                    bestScore = score;
                    best = socket;
                }
            }
            return best;
        }

        private static int ChooseNearestExistingAccess(
            List<int> existing,
            List<P6RoomAccess> accesses,
            RectInt otherBounds)
        {
            Vector2Int target = ScaleMacroCenter(otherBounds);
            int best = existing[0];
            int bestDistance = int.MaxValue;
            for (int index = 0; index < existing.Count; index++)
            {
                int accessId = existing[index];
                int distance = Manhattan(
                    accesses[accessId].PortalCell, target);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = accessId;
                }
            }
            return best;
        }

        private static bool IsSocketUsed(
            string socketId,
            List<int> existing,
            List<P6RoomAccess> accesses)
        {
            if (existing == null) return false;
            for (int index = 0; index < existing.Count; index++)
                if (string.Equals(
                        accesses[existing[index]].SocketId,
                        socketId,
                        StringComparison.Ordinal))
                    return true;
            return false;
        }

        private static bool TryChooseMainPair(
            P6RoomNode node,
            P6StageArchetype archetype,
            out P6SocketTraversalProof entry,
            out P6SocketTraversalProof departure)
        {
            entry = null;
            departure = null;
            int bestScore = int.MaxValue;
            for (int index = 0; index < node.TraversalProof.Links.Count; index++)
            {
                P6SocketLinkProof link = node.TraversalProof.Links[index];
                if (link.Kind != P6SocketLinkKind.MainRoutePair
                    || !link.Passed
                    || string.Equals(
                        link.FromSocketId,
                        link.ToSocketId,
                        StringComparison.Ordinal))
                    continue;
                P6SocketTraversalProof from =
                    node.TraversalProof.FindSocket(link.FromSocketId);
                P6SocketTraversalProof to =
                    node.TraversalProof.FindSocket(link.ToSocketId);
                if (!IsValidMainSocket(from) || !IsValidMainSocket(to))
                    continue;
                int score = MainSocketScore(from.Side, archetype, true)
                    + MainSocketScore(to.Side, archetype, false);
                if (score < bestScore)
                {
                    bestScore = score;
                    entry = from;
                    departure = to;
                }
            }
            return entry != null && departure != null;
        }

        private static P6SocketTraversalProof ChooseMainSocket(
            P6RoomNode node,
            P6StageArchetype archetype,
            bool incoming)
        {
            P6SocketTraversalProof best = null;
            int bestScore = int.MaxValue;
            for (int index = 0; index < node.TraversalProof.Sockets.Count; index++)
            {
                P6SocketTraversalProof socket =
                    node.TraversalProof.Sockets[index];
                if (!IsValidMainSocket(socket)) continue;
                int score = MainSocketScore(socket.Side, archetype, incoming);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = socket;
                }
            }
            return best;
        }

        private static bool IsValidMainSocket(P6SocketTraversalProof socket)
        {
            return socket != null
                && socket.IsToolFreeMainRouteSocket
                && socket.OpeningSize == Vector2Int.one
                && socket.AnchorStandable
                && socket.OneCellBodyClearance;
        }

        private static int MainSocketScore(
            RoomSocketSide side,
            P6StageArchetype archetype,
            bool incoming)
        {
            RoomSocketSide preferred;
            if (archetype == P6StageArchetype.Descent)
                preferred = incoming ? RoomSocketSide.Top : RoomSocketSide.Bottom;
            else if (archetype == P6StageArchetype.Ascent)
                preferred = incoming ? RoomSocketSide.Bottom : RoomSocketSide.Top;
            else
                preferred = incoming ? RoomSocketSide.Left : RoomSocketSide.Right;
            if (side == preferred) return 0;
            if ((side == RoomSocketSide.Left && preferred == RoomSocketSide.Right)
                || (side == RoomSocketSide.Right && preferred == RoomSocketSide.Left)
                || (side == RoomSocketSide.Top && preferred == RoomSocketSide.Bottom)
                || (side == RoomSocketSide.Bottom && preferred == RoomSocketSide.Top))
                return 2;
            return 1;
        }

        private static int SocketDirectionScore(
            RoomSocketSide side, Vector2Int direction)
        {
            if (Math.Abs(direction.x) >= Math.Abs(direction.y))
                return direction.x >= 0
                    ? (side == RoomSocketSide.Right ? 0 : 2)
                    : (side == RoomSocketSide.Left ? 0 : 2);
            return direction.y >= 0
                ? (side == RoomSocketSide.Top ? 0 : 2)
                : (side == RoomSocketSide.Bottom ? 0 : 2);
        }

        private static int GetOrCreateAccess(
            P6RoomNode node,
            P6SocketTraversalProof socket,
            List<P6RoomAccess> accesses,
            Dictionary<int, List<int>> byNode,
            Dictionary<string, int> bySocket)
        {
            string key = node.Id + ":" + socket.SocketId;
            int existing;
            if (bySocket.TryGetValue(key, out existing)) return existing;
            Vector2Int socketCell;
            Vector2Int portalCell;
            GetScaledPortal(
                node.MacroBounds,
                node.TraversalProof.LogicalSize,
                socket,
                out socketCell,
                out portalCell);
            int accessId = accesses.Count;
            accesses.Add(new P6RoomAccess(
                accessId, node.Id, socket, socketCell, portalCell));
            bySocket[key] = accessId;
            List<int> nodeAccesses;
            if (!byNode.TryGetValue(node.Id, out nodeAccesses))
            {
                nodeAccesses = new List<int>();
                byNode[node.Id] = nodeAccesses;
            }
            nodeAccesses.Add(accessId);
            return accessId;
        }

        private static EdgeAccessAssignment CreateAssignment(
            int firstNode, int firstAccess,
            int secondNode, int secondAccess)
        {
            if (firstNode <= secondNode)
                return new EdgeAccessAssignment
                {
                    FirstAccessId = firstAccess,
                    SecondAccessId = secondAccess
                };
            return new EdgeAccessAssignment
            {
                FirstAccessId = secondAccess,
                SecondAccessId = firstAccess
            };
        }

        private static bool RouteCorridors(
            Vector2Int canvas, List<P6RoomNode> nodes,
            List<P6RoomAccess> accesses,
            Dictionary<long, EdgeAccessAssignment> edgeAccesses,
            List<P6CandidateEdge> mstCandidates,
            List<P6CandidateEdge> additionalCandidates,
            out List<P6GraphEdge> mstEdges,
            out List<P6GraphEdge> additionalEdges,
            out P6CorridorNetwork network)
        {
            mstEdges = new List<P6GraphEdge>();
            additionalEdges = new List<P6GraphEdge>();
            network = null;
            P6MacroRouteGrid grid = CreateRoutingGrid(canvas, nodes, null, null);

            if (!RouteEdgeSet(
                    canvas, nodes, grid, accesses, edgeAccesses,
                    mstCandidates, P6EdgeKind.Mst, mstEdges))
                return false;
            if (!RouteEdgeSet(
                    canvas, nodes, grid, accesses, edgeAccesses,
                    additionalCandidates,
                    P6EdgeKind.Additional, additionalEdges))
                return false;

            var cells = new List<Vector2Int>(grid.CorridorCells);
            cells.Sort(CompareCell);
            var cellSet = new HashSet<Vector2Int>(cells);
            var junctions = new List<Vector2Int>();
            for (int index = 0; index < cells.Count; index++)
            {
                int neighbors = 0;
                if (cellSet.Contains(cells[index] + Vector2Int.left)) neighbors++;
                if (cellSet.Contains(cells[index] + Vector2Int.right)) neighbors++;
                if (cellSet.Contains(cells[index] + Vector2Int.down)) neighbors++;
                if (cellSet.Contains(cells[index] + Vector2Int.up)) neighbors++;
                if (neighbors >= 3) junctions.Add(cells[index]);
            }
            network = new P6CorridorNetwork(
                2, grid.Bounds, cells, junctions, accesses);
            return true;
        }

        private static bool RouteEdgeSet(
            Vector2Int canvas,
            List<P6RoomNode> nodes,
            P6MacroRouteGrid grid,
            List<P6RoomAccess> accesses,
            Dictionary<long, EdgeAccessAssignment> edgeAccesses,
            List<P6CandidateEdge> source, P6EdgeKind kind,
            List<P6GraphEdge> output)
        {
            for (int index = 0; index < source.Count; index++)
            {
                P6CandidateEdge edge = source[index];
                EdgeAccessAssignment assignment;
                if (!edgeAccesses.TryGetValue(
                        Key(edge.FirstNodeId, edge.SecondNodeId),
                        out assignment))
                    return false;
                Vector2Int start =
                    accesses[assignment.FirstAccessId].PortalCell;
                Vector2Int goal =
                    accesses[assignment.SecondAccessId].PortalCell;
                if (start == goal && kind == P6EdgeKind.Additional)
                    return false;
                P6MacroRouteResult route = P6MacroRouter.Route(grid, start, goal);
                if (!route.Succeeded) return false;
                var path = new List<Vector2Int>(route.Cells);
                if (kind == P6EdgeKind.Additional)
                {
                    List<Vector2Int> alternate;
                    if (!TryBuildPhysicalLoopRoute(
                            canvas, nodes, grid, start, goal,
                            path, out alternate))
                        return false;
                    path = alternate;
                }
                grid.AddCorridor(path);
                output.Add(new P6GraphEdge(
                    edge.FirstNodeId, edge.SecondNodeId,
                    assignment.FirstAccessId, assignment.SecondAccessId,
                    edge.Weight, kind, path));
            }
            return true;
        }

        private static bool TryBuildPhysicalLoopRoute(
            Vector2Int canvas,
            List<P6RoomNode> nodes,
            P6MacroRouteGrid current,
            Vector2Int start,
            Vector2Int goal,
            List<Vector2Int> existingRoute,
            out List<Vector2Int> alternate)
        {
            alternate = null;
            var existingCells =
                new HashSet<Vector2Int>(current.CorridorCells);
            if (PathIsSimple(existingRoute)
                && AddsNewCell(existingRoute, existingCells))
            {
                alternate = existingRoute;
                return true;
            }
            for (int index = 1; index < existingRoute.Count - 1; index++)
            {
                Vector2Int blocked = existingRoute[index];
                if (!existingCells.Contains(blocked)) continue;
                P6MacroRouteGrid alternateGrid = CreateRoutingGrid(
                    canvas, nodes, existingCells, blocked);
                P6MacroRouteResult route =
                    P6MacroRouter.Route(alternateGrid, start, goal);
                if (route.Succeeded
                    && PathIsSimple(route.Cells)
                    && AddsNewCell(route.Cells, existingCells))
                {
                    alternate = new List<Vector2Int>(route.Cells);
                    return true;
                }
            }
            return false;
        }

        private static P6MacroRouteGrid CreateRoutingGrid(
            Vector2Int canvas,
            List<P6RoomNode> nodes,
            IEnumerable<Vector2Int> existingCorridors,
            Vector2Int? blocked)
        {
            RectInt routingBounds = new RectInt(
                -1, -1, canvas.x * 2 + 3, canvas.y * 2 + 3);
            var grid = new P6MacroRouteGrid(routingBounds);
            for (int index = 0; index < nodes.Count; index++)
            {
                RectInt interior = ScaleRoomInterior(nodes[index].MacroBounds);
                grid.MarkRoomInterior(interior);
                grid.MarkRoomExterior(new RectInt(
                    interior.xMin - 1,
                    interior.yMin - 1,
                    interior.width + 2,
                    interior.height + 2));
            }
            grid.AddCorridor(existingCorridors);
            if (blocked.HasValue) grid.MarkForbidden(blocked.Value);
            return grid;
        }

        private static RectInt ScaleRoomInterior(RectInt macroBounds)
        {
            return new RectInt(
                macroBounds.xMin * 2 + 1,
                macroBounds.yMin * 2 + 1,
                macroBounds.width * 2 - 1,
                macroBounds.height * 2 - 1);
        }

        private static Vector2Int ScaleMacroCenter(RectInt macroBounds)
        {
            RectInt interior = ScaleRoomInterior(macroBounds);
            return new Vector2Int(
                interior.xMin + interior.width / 2,
                interior.yMin + interior.height / 2);
        }

        private static bool PathIsSimple(IReadOnlyList<Vector2Int> path)
        {
            var cells = new HashSet<Vector2Int>();
            for (int index = 0; index < path.Count; index++)
                if (!cells.Add(path[index])) return false;
            return true;
        }

        private static bool AddsNewCell(
            IReadOnlyList<Vector2Int> path,
            HashSet<Vector2Int> existing)
        {
            for (int index = 1; index < path.Count - 1; index++)
                if (!existing.Contains(path[index])) return true;
            return false;
        }

        private static void GetScaledPortal(
            RectInt bounds,
            Vector2Int logicalSize,
            P6SocketTraversalProof socketProof,
            out Vector2Int socket, out Vector2Int portal)
        {
            RectInt interior = ScaleRoomInterior(bounds);
            if (socketProof.Side == RoomSocketSide.Left
                || socketProof.Side == RoomSocketSide.Right)
            {
                int y = interior.yMin + ScaleSocketLane(
                    socketProof.CellOffset.y,
                    logicalSize.y,
                    interior.height);
                if (socketProof.Side == RoomSocketSide.Left)
                {
                    socket = new Vector2Int(interior.xMin, y);
                    portal = socket + Vector2Int.left;
                }
                else
                {
                    socket = new Vector2Int(interior.xMax - 1, y);
                    portal = socket + Vector2Int.right;
                }
            }
            else
            {
                int x = interior.xMin + ScaleSocketLane(
                    socketProof.CellOffset.x,
                    logicalSize.x,
                    interior.width);
                if (socketProof.Side == RoomSocketSide.Bottom)
                {
                    socket = new Vector2Int(x, interior.yMin);
                    portal = socket + Vector2Int.down;
                }
                else
                {
                    socket = new Vector2Int(x, interior.yMax - 1);
                    portal = socket + Vector2Int.up;
                }
            }
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

        private static int SpatialWeight(RectInt first, RectInt second)
        {
            Vector2Int a = Center(first);
            Vector2Int b = Center(second);
            int distance = Manhattan(a, b);
            bool orthogonal = a.x == b.x || a.y == b.y;
            return distance * 10 + (orthogonal ? 0 : 5);
        }

        private static Vector2Int Center(RectInt bounds) =>
            new Vector2Int(
                bounds.xMin + (bounds.width - 1) / 2,
                bounds.yMin + (bounds.height - 1) / 2);

        private static int Manhattan(Vector2Int first, Vector2Int second) =>
            Math.Abs(first.x - second.x) + Math.Abs(first.y - second.y);

        private static long Key(int first, int second)
        {
            uint low = (uint)Math.Min(first, second);
            uint high = (uint)Math.Max(first, second);
            return ((long)low << 32) | high;
        }

        private static int CompareCandidate(P6CandidateEdge a, P6CandidateEdge b)
        {
            int weight = a.Weight.CompareTo(b.Weight);
            if (weight != 0) return weight;
            int first = a.FirstNodeId.CompareTo(b.FirstNodeId);
            return first != 0 ? first : a.SecondNodeId.CompareTo(b.SecondNodeId);
        }

        private static int CompareCell(Vector2Int a, Vector2Int b)
        {
            int y = a.y.CompareTo(b.y);
            return y != 0 ? y : a.x.CompareTo(b.x);
        }

        private static P6GenerationResult Failure(
            int seed, int attempts, P6GenerationFailure failure, string reason)
        {
            return new P6GenerationResult(
                seed, seed, attempts, null, null, failure, reason, string.Empty);
        }

        private static string Fingerprint(P6RoomGraphPlan plan)
        {
            var text = new StringBuilder();
            text.Append(plan.Seed).Append('|').Append((int)plan.StageSlot)
                .Append('|').Append((int)plan.Archetype);
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode room = plan.Rooms[index];
                text.Append("|R").Append(room.Id).Append(':')
                    .Append(room.MacroBounds.x).Append(',')
                    .Append(room.MacroBounds.y).Append(',')
                    .Append(room.MacroBounds.width).Append(',')
                    .Append(room.MacroBounds.height).Append(',')
                    .Append(room.PrefabId).Append(',')
                    .Append((int)room.Role);
            }
            for (int index = 0; index < plan.Edges.Count; index++)
            {
                P6GraphEdge edge = plan.Edges[index];
                text.Append("|E").Append(edge.FirstNodeId).Append(',')
                    .Append(edge.SecondNodeId).Append(',')
                    .Append(edge.FirstAccessId).Append(',')
                    .Append(edge.SecondAccessId).Append(',')
                    .Append((int)edge.Kind);
                for (int cell = 0; cell < edge.CorridorCells.Count; cell++)
                    AppendCell(text, 'e', edge.CorridorCells[cell]);
            }
            for (int index = 0; index < plan.MainPathNodeIds.Count; index++)
                text.Append("|P").Append(plan.MainPathNodeIds[index]);
            if (plan.CorridorNetwork != null)
            {
                text.Append("|Q").Append(plan.CorridorNetwork.RoutingScale);
                for (int index = 0;
                     index < plan.CorridorNetwork.Cells.Count;
                     index++)
                    AppendCell(text, 'C', plan.CorridorNetwork.Cells[index]);
                for (int index = 0;
                     index < plan.CorridorNetwork.RoomAccesses.Count;
                     index++)
                {
                    P6RoomAccess access =
                        plan.CorridorNetwork.RoomAccesses[index];
                    text.Append("|A").Append(access.AccessId).Append(',')
                        .Append(access.NodeId).Append(',')
                        .Append(access.SocketId).Append(',')
                        .Append((int)access.SocketSide).Append(',')
                        .Append(access.CellOffset.x).Append(',')
                        .Append(access.CellOffset.y).Append(',')
                        .Append(access.OpeningSize.x).Append(',')
                        .Append(access.OpeningSize.y).Append(',')
                        .Append((int)access.TraversalType).Append(',')
                        .Append(access.MainRouteAllowed ? 1 : 0).Append(',')
                        .Append(access.ValidationAnchor.x).Append(',')
                        .Append(access.ValidationAnchor.y);
                    AppendCell(text, 'S', access.SocketCell);
                    AppendCell(text, 'O', access.PortalCell);
                }
                for (int index = 0;
                     index < plan.CorridorNetwork.JunctionCells.Count;
                     index++)
                    AppendCell(
                        text, 'J', plan.CorridorNetwork.JunctionCells[index]);
            }
            ulong hash = 14695981039346656037UL;
            string value = text.ToString();
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= 1099511628211UL;
            }
            return hash.ToString("X16");
        }

        private static void AppendCell(
            StringBuilder text, char prefix, Vector2Int cell)
        {
            text.Append('|').Append(prefix).Append(cell.x)
                .Append(',').Append(cell.y);
        }
    }
}

#endif
