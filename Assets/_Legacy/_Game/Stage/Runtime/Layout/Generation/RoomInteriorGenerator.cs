#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    public static class RoomInteriorGenerator
    {
        private static readonly IReadOnlyDictionary<ChunkPatternRole, int> GlobalRoleDeck =
            new Dictionary<ChunkPatternRole, int>
            {
                { ChunkPatternRole.Plain, 4 },
                { ChunkPatternRole.Damage, 1 },
                { ChunkPatternRole.Interaction, 2 },
                { ChunkPatternRole.Puzzle, 1 },
                { ChunkPatternRole.Condition, 1 },
            };

        private static readonly IReadOnlyDictionary<ChunkPatternRole, string[]> GlobalPatternIds =
            new Dictionary<ChunkPatternRole, string[]>
            {
                { ChunkPatternRole.Plain, new[] { "GEN_PLAIN_01", "GEN_PLAIN_02", "GEN_PLAIN_03" } },
                { ChunkPatternRole.Damage, new[] { "GEN_DAMAGE_01", "GEN_DAMAGE_02", "GEN_DAMAGE_03" } },
                { ChunkPatternRole.Interaction, new[] { "GEN_INTERACTION_01", "GEN_INTERACTION_02", "GEN_INTERACTION_03" } },
                { ChunkPatternRole.Puzzle, new[] { "GEN_PUZZLE_01", "GEN_PUZZLE_02", "GEN_PUZZLE_03" } },
                { ChunkPatternRole.Condition, new[] { "GEN_CONDITION_01", "GEN_CONDITION_02", "GEN_CONDITION_03" } },
            };

        public static RoomInteriorLayout GenerateCommonTestRoom(int seed)
        {
            return Generate(new RoomInteriorGenerationRequest
            {
                RoomId = "COMMON_TEST_ROOM",
                Seed = seed,
                ChunkGridSize = new Vector2Int(4, 3),
            });
        }

        public static RoomInteriorLayout Generate(RoomInteriorGenerationRequest request)
        {
            request ??= new RoomInteriorGenerationRequest();
            Vector2Int gridSize = new Vector2Int(
                Mathf.Clamp(request.ChunkGridSize.x, 2, 16),
                Mathf.Clamp(request.ChunkGridSize.y, 1, 16));
            var layout = new RoomInteriorLayout
            {
                RoomId = string.IsNullOrWhiteSpace(request.RoomId) ? "COMMON_TEST_ROOM" : request.RoomId,
                Seed = request.Seed,
                ChunkGridSize = gridSize,
            };
            var random = new InteriorStableRandom(CombineSeed(request.Seed, layout.RoomId));
            List<Vector2Int> order = CreateGenerationOrder(gridSize);
            HashSet<Vector2Int> mainRoute = CreateMainRoute(gridSize);
            Dictionary<ChunkPatternRole, int> deck = CreateRoleDeck(request.RegionRoleAdjustments);
            Dictionary<string, int> patternCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            ChunkPatternRole? previousRole = null;
            int nonPlainRun = 0;
            int mainAssigned = 0;
            int mainDamage = 0;

            for (int index = 0; index < order.Count; index++)
            {
                Vector2Int gridCell = order[index];
                bool isMain = mainRoute.Contains(gridCell);
                bool forcePlain = index == 0 || gridCell == new Vector2Int(gridSize.x - 1, gridSize.y / 2) || nonPlainRun >= 2;
                ChunkPatternRole role = SelectRole(
                    deck,
                    previousRole,
                    isMain,
                    forcePlain,
                    mainAssigned,
                    mainDamage,
                    ref random);
                if (deck.TryGetValue(role, out int remaining) && remaining > 0) deck[role] = remaining - 1;
                if (deck.Values.All(value => value <= 0)) deck = CreateRoleDeck(request.RegionRoleAdjustments);

                string patternId = SelectPatternId(role, patternCounts, ref random);
                patternCounts.TryGetValue(patternId, out int patternCount);
                patternCounts[patternId] = patternCount + 1;
                var chunk = new GeneratedMicroChunk
                {
                    GridCell = gridCell,
                    OriginCell = new Vector2Int(
                        gridCell.x * GeneratedMicroChunk.Width,
                        gridCell.y * GeneratedMicroChunk.Height),
                    GenerationOrder = index,
                    PatternId = patternId,
                    Role = role,
                    MainRoute = isMain,
                    Cells = Enumerable.Repeat(
                        MicroCellKind.Solid,
                        GeneratedMicroChunk.Width * GeneratedMicroChunk.Height).ToArray(),
                };
                layout.Chunks.Add(chunk);
                previousRole = role;
                nonPlainRun = role == ChunkPatternRole.Plain ? 0 : nonPlainRun + 1;
                if (isMain)
                {
                    mainAssigned++;
                    if (role == ChunkPatternRole.Damage) mainDamage++;
                }
            }

            ConnectChunks(layout, ref random);
            CarveChunkInteriors(layout);
            AddRoleMarkers(layout);
            AddHiddenContentAndToolEscape(layout, ref random);
            ApplyValidation(layout);
            layout.ValidationHash = CreateValidationHash(layout);
            return layout;
        }

        private static List<Vector2Int> CreateGenerationOrder(Vector2Int gridSize)
        {
            int mainY = gridSize.y / 2;
            var result = new List<Vector2Int>(gridSize.x * gridSize.y);
            for (int x = 0; x < gridSize.x; x++) result.Add(new Vector2Int(x, mainY));
            for (int y = 0; y < gridSize.y; y++)
            {
                if (y == mainY) continue;
                for (int x = 0; x < gridSize.x; x++) result.Add(new Vector2Int(x, y));
            }
            return result;
        }

        private static HashSet<Vector2Int> CreateMainRoute(Vector2Int gridSize)
        {
            var result = new HashSet<Vector2Int>();
            int mainY = gridSize.y / 2;
            for (int x = 0; x < gridSize.x; x++) result.Add(new Vector2Int(x, mainY));
            return result;
        }

        private static Dictionary<ChunkPatternRole, int> CreateRoleDeck(
            IReadOnlyList<ChunkRoleDeckAdjustment> adjustments)
        {
            var result = GlobalRoleDeck.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (adjustments == null) return result;
            for (int index = 0; index < adjustments.Count; index++)
            {
                ChunkRoleDeckAdjustment adjustment = adjustments[index];
                if (adjustment == null || !result.ContainsKey(adjustment.Role)) continue;
                result[adjustment.Role] = Mathf.Max(0,
                    result[adjustment.Role] + Mathf.Clamp(adjustment.CountDelta, -1, 1));
            }
            result[ChunkPatternRole.Plain] = Mathf.Max(1, result[ChunkPatternRole.Plain]);
            return result;
        }

        private static ChunkPatternRole SelectRole(
            IReadOnlyDictionary<ChunkPatternRole, int> deck,
            ChunkPatternRole? previous,
            bool mainRoute,
            bool forcePlain,
            int mainAssigned,
            int mainDamage,
            ref InteriorStableRandom random)
        {
            if (forcePlain) return ChunkPatternRole.Plain;
            var candidates = new List<ChunkPatternRole>();
            foreach (KeyValuePair<ChunkPatternRole, int> pair in deck)
            {
                if (pair.Value <= 0 || pair.Key == ChunkPatternRole.Condition && mainRoute) continue;
                if (previous.HasValue && pair.Key == previous.Value &&
                    (pair.Key == ChunkPatternRole.Damage || pair.Key == ChunkPatternRole.Puzzle)) continue;
                if (mainRoute && pair.Key == ChunkPatternRole.Damage &&
                    (mainDamage + 1) / (float)(mainAssigned + 1) > 0.2f) continue;
                candidates.Add(pair.Key);
            }
            if (candidates.Count == 0) return ChunkPatternRole.Plain;
            int total = candidates.Sum(candidate => Mathf.Max(1, deck[candidate]));
            int roll = random.Range(0, total);
            for (int index = 0; index < candidates.Count; index++)
            {
                roll -= Mathf.Max(1, deck[candidates[index]]);
                if (roll < 0) return candidates[index];
            }
            return candidates[candidates.Count - 1];
        }

        private static string SelectPatternId(
            ChunkPatternRole role,
            IReadOnlyDictionary<string, int> patternCounts,
            ref InteriorStableRandom random)
        {
            string[] ids = GlobalPatternIds[role];
            int start = random.Range(0, ids.Length);
            for (int offset = 0; offset < ids.Length; offset++)
            {
                string candidate = ids[(start + offset) % ids.Length];
                patternCounts.TryGetValue(candidate, out int count);
                if (count < 2) return candidate;
            }
            int variant = ids.Length + 1;
            while (true)
            {
                string candidate = $"GEN_{role.ToString().ToUpperInvariant()}_{variant:D2}";
                patternCounts.TryGetValue(candidate, out int count);
                if (count < 2) return candidate;
                variant++;
            }
        }

        private static void ConnectChunks(RoomInteriorLayout layout, ref InteriorStableRandom random)
        {
            for (int y = 0; y < layout.ChunkGridSize.y; y++)
            {
                for (int x = 0; x < layout.ChunkGridSize.x; x++)
                {
                    GeneratedMicroChunk chunk = layout.FindChunk(new Vector2Int(x, y));
                    if (x + 1 < layout.ChunkGridSize.x)
                    {
                        GeneratedMicroChunk east = layout.FindChunk(new Vector2Int(x + 1, y));
                        int socketY = random.RangeInclusive(2, 5);
                        AddSocketPair(chunk, east, MicroSocketSide.East, socketY);
                    }
                    if (y + 1 < layout.ChunkGridSize.y)
                    {
                        GeneratedMicroChunk north = layout.FindChunk(new Vector2Int(x, y + 1));
                        int socketX = random.RangeInclusive(2, 5);
                        AddSocketPair(chunk, north, MicroSocketSide.North, socketX);
                    }
                }
            }

            int mainY = layout.ChunkGridSize.y / 2;
            GeneratedMicroChunk entryChunk = layout.FindChunk(new Vector2Int(0, mainY));
            GeneratedMicroChunk exitChunk = layout.FindChunk(new Vector2Int(layout.ChunkGridSize.x - 1, mainY));
            int entryY = random.RangeInclusive(2, 5);
            int exitY = random.RangeInclusive(2, 5);
            AddExternalSocket(entryChunk, MicroSocketSide.West, entryY);
            AddExternalSocket(exitChunk, MicroSocketSide.East, exitY);
            layout.EntryWorldCell = entryChunk.OriginCell + new Vector2Int(0, entryY);
            layout.ExitWorldCell = exitChunk.OriginCell + new Vector2Int(7, exitY);
        }

        private static void AddSocketPair(
            GeneratedMicroChunk first,
            GeneratedMicroChunk second,
            MicroSocketSide firstSide,
            int axisOffset)
        {
            MicroSocketSide secondSide = firstSide == MicroSocketSide.East
                ? MicroSocketSide.West
                : MicroSocketSide.South;
            first.Sockets.Add(new GeneratedMicroSocket
            {
                Side = firstSide,
                LocalCell = GetSocketCell(firstSide, axisOffset),
                NeighborChunk = second.GridCell,
            });
            second.Sockets.Add(new GeneratedMicroSocket
            {
                Side = secondSide,
                LocalCell = GetSocketCell(secondSide, axisOffset),
                NeighborChunk = first.GridCell,
            });
        }

        private static void AddExternalSocket(GeneratedMicroChunk chunk, MicroSocketSide side, int axisOffset)
        {
            chunk.Sockets.Add(new GeneratedMicroSocket
            {
                Side = side,
                LocalCell = GetSocketCell(side, axisOffset),
                NeighborChunk = new Vector2Int(-1, -1),
                External = true,
            });
        }

        private static Vector2Int GetSocketCell(MicroSocketSide side, int axisOffset)
        {
            return side switch
            {
                MicroSocketSide.West => new Vector2Int(0, axisOffset),
                MicroSocketSide.East => new Vector2Int(7, axisOffset),
                MicroSocketSide.South => new Vector2Int(axisOffset, 0),
                _ => new Vector2Int(axisOffset, 7),
            };
        }

        private static void CarveChunkInteriors(RoomInteriorLayout layout)
        {
            Vector2Int center = new Vector2Int(3, 3);
            for (int index = 0; index < layout.Chunks.Count; index++)
            {
                GeneratedMicroChunk chunk = layout.Chunks[index];
                chunk.SetCell(center, MicroCellKind.Empty);
                for (int socketIndex = 0; socketIndex < chunk.Sockets.Count; socketIndex++)
                {
                    GeneratedMicroSocket socket = chunk.Sockets[socketIndex];
                    Vector2Int inner = GetInnerCell(socket.Side, socket.LocalCell);
                    chunk.SetCell(socket.LocalCell, MicroCellKind.Empty);
                    chunk.SetCell(inner, MicroCellKind.Empty);
                    CarveManhattan(chunk, inner, center);
                }
            }
        }

        private static void CarveManhattan(GeneratedMicroChunk chunk, Vector2Int from, Vector2Int to)
        {
            Vector2Int cursor = from;
            chunk.SetCell(cursor, MicroCellKind.Empty);
            while (cursor.x != to.x)
            {
                cursor.x += cursor.x < to.x ? 1 : -1;
                chunk.SetCell(cursor, MicroCellKind.Empty);
            }
            while (cursor.y != to.y)
            {
                cursor.y += cursor.y < to.y ? 1 : -1;
                chunk.SetCell(cursor, MicroCellKind.Empty);
            }
        }

        private static Vector2Int GetInnerCell(MicroSocketSide side, Vector2Int cell)
        {
            return side switch
            {
                MicroSocketSide.West => cell + Vector2Int.right,
                MicroSocketSide.East => cell + Vector2Int.left,
                MicroSocketSide.South => cell + Vector2Int.up,
                _ => cell + Vector2Int.down,
            };
        }

        private static void AddRoleMarkers(RoomInteriorLayout layout)
        {
            for (int index = 0; index < layout.Chunks.Count; index++)
            {
                GeneratedMicroChunk chunk = layout.Chunks[index];
                Vector2Int marker = FindMarkerCell(chunk);
                if (!GeneratedMicroChunk.IsLocalCell(marker)) continue;
                MicroCellKind markerKind = chunk.Role switch
                {
                    ChunkPatternRole.Damage => MicroCellKind.Hazard,
                    ChunkPatternRole.Interaction => MicroCellKind.Interaction,
                    ChunkPatternRole.Puzzle => MicroCellKind.Puzzle,
                    ChunkPatternRole.Condition => MicroCellKind.Reward,
                    _ => MicroCellKind.Empty,
                };
                if (chunk.Role != ChunkPatternRole.Plain) chunk.SetCell(marker, markerKind);
            }
        }

        private static Vector2Int FindMarkerCell(GeneratedMicroChunk chunk)
        {
            Vector2Int[] candidates =
            {
                new Vector2Int(5, 5), new Vector2Int(2, 5),
                new Vector2Int(5, 2), new Vector2Int(2, 2),
            };
            for (int index = 0; index < candidates.Length; index++)
            {
                if (chunk.GetCell(candidates[index]) == MicroCellKind.Solid &&
                    (chunk.Role != ChunkPatternRole.Damage ||
                     !IsInsidePortalPadding(chunk, candidates[index])))
                {
                    return candidates[index];
                }
            }
            return new Vector2Int(-1, -1);
        }

        private static bool IsInsidePortalPadding(GeneratedMicroChunk chunk, Vector2Int localCell)
        {
            for (int index = 0; index < chunk.Sockets.Count; index++)
            {
                Vector2Int delta = localCell - chunk.Sockets[index].LocalCell;
                if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) <=
                    StarNight.Stage.Transitions.RoomPortalContract.PortalPaddingCells)
                {
                    return true;
                }
            }
            return false;
        }

        private static void AddHiddenContentAndToolEscape(
            RoomInteriorLayout layout,
            ref InteriorStableRandom random)
        {
            List<GeneratedMicroChunk> optionalChunks = layout.Chunks
                .Where(chunk => chunk != null && !chunk.MainRoute)
                .OrderBy(chunk => chunk.GenerationOrder)
                .ToList();
            if (optionalChunks.Count == 0) return;

            GeneratedMicroChunk pocketChunk = optionalChunks
                .FirstOrDefault(chunk => chunk.Role == ChunkPatternRole.Condition) ?? optionalChunks[0];
            Vector2Int pocketCell = FindSolidPocketCell(pocketChunk);
            if (GeneratedMicroChunk.IsLocalCell(pocketCell))
            {
                layout.HiddenContents.Add(new GeneratedHiddenContent
                {
                    StableId = $"{layout.RoomId}:{layout.Seed}:POCKET_00",
                    Type = HiddenContentType.EmbeddedPocket,
                    ChunkGridCell = pocketChunk.GridCell,
                    LocalCell = pocketCell,
                    Hint = (HiddenPocketHint)random.Range(0, 3),
                    RevealTools = ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Shovel,
                });
            }

            GeneratedMicroChunk escapeChunk = optionalChunks
                .FirstOrDefault(chunk => chunk != pocketChunk &&
                                         (chunk.Role == ChunkPatternRole.Condition || chunk.Role == ChunkPatternRole.Puzzle))
                ?? optionalChunks[optionalChunks.Count - 1];
            ToolEscapeKind kind = (ToolEscapeKind)random.Range(0, 3);
            ToolTag requiredTool = kind switch
            {
                ToolEscapeKind.ShovelDirt => ToolTag.Shovel,
                ToolEscapeKind.BombCrack => ToolTag.Bomb,
                _ => ToolTag.Rope,
            };
            string patternId = kind switch
            {
                ToolEscapeKind.ShovelDirt => "ESC_SHOVEL_DIRT_01",
                ToolEscapeKind.BombCrack => "ESC_BOMB_CRACK_01",
                _ => "ESC_ROPE_SHAFT_01",
            };
            Vector2Int toolCell = FindOrCarveWalkableCell(escapeChunk, new Vector2Int(2, 3));
            Vector2Int rackCell = FindOrCarveWalkableCell(escapeChunk, new Vector2Int(3, 3));
            Vector2Int rewardCell = FindOrCarveWalkableCell(escapeChunk, new Vector2Int(4, 3));
            escapeChunk.SetCell(toolCell, MicroCellKind.Interaction);
            escapeChunk.SetCell(rackCell, MicroCellKind.Interaction);
            escapeChunk.SetCell(rewardCell, MicroCellKind.Reward);
            layout.ToolEscapes.Add(new GeneratedToolEscape
            {
                PatternId = patternId,
                Kind = kind,
                ChunkGridCell = escapeChunk.GridCell,
                RequiredTool = requiredTool,
                ToolPickupLocalCell = toolCell,
                RecoveryRackLocalCell = rackCell,
                RewardLocalCell = rewardCell,
            });
        }

        private static Vector2Int FindSolidPocketCell(GeneratedMicroChunk chunk)
        {
            Vector2Int[] candidates =
            {
                new Vector2Int(6, 6), new Vector2Int(1, 6),
                new Vector2Int(6, 1), new Vector2Int(1, 1),
            };
            for (int index = 0; index < candidates.Length; index++)
            {
                if (chunk.GetCell(candidates[index]) == MicroCellKind.Solid) return candidates[index];
            }
            return new Vector2Int(-1, -1);
        }

        private static Vector2Int FindOrCarveWalkableCell(GeneratedMicroChunk chunk, Vector2Int preferred)
        {
            if (chunk.GetCell(preferred) != MicroCellKind.Solid) return preferred;
            Vector2Int center = new Vector2Int(3, 3);
            Vector2Int cursor = preferred;
            chunk.SetCell(cursor, MicroCellKind.Empty);
            while (cursor.x != center.x)
            {
                cursor.x += cursor.x < center.x ? 1 : -1;
                chunk.SetCell(cursor, MicroCellKind.Empty);
            }
            while (cursor.y != center.y)
            {
                cursor.y += cursor.y < center.y ? 1 : -1;
                chunk.SetCell(cursor, MicroCellKind.Empty);
            }
            return preferred;
        }

        private static void ApplyValidation(RoomInteriorLayout layout)
        {
            IReadOnlyList<string> errors = RoomInteriorValidator.Validate(layout);
            layout.ValidationErrors.Clear();
            layout.ValidationErrors.AddRange(errors);
            layout.HasT0MainRoute = !errors.Any(error => error.StartsWith("[T0]", StringComparison.Ordinal));
        }

        private static string CreateValidationHash(RoomInteriorLayout layout)
        {
            uint hash = 2166136261u;
            AddHash(ref hash, layout.RoomId);
            AddHash(ref hash, layout.Seed.ToString());
            AddHash(ref hash, layout.ChunkGridSize.ToString());
            foreach (GeneratedMicroChunk chunk in layout.Chunks.OrderBy(chunk => chunk.GenerationOrder))
            {
                AddHash(ref hash, $"{chunk.GridCell}|{chunk.PatternId}|{chunk.Role}|{chunk.MainRoute}");
                foreach (GeneratedMicroSocket socket in chunk.Sockets.OrderBy(socket => socket.Side))
                {
                    AddHash(ref hash, $"{socket.Side}|{socket.LocalCell}|{socket.NeighborChunk}|{socket.External}");
                }
            }
            foreach (GeneratedHiddenContent hidden in layout.HiddenContents)
            {
                AddHash(ref hash, $"{hidden.StableId}|{hidden.ChunkGridCell}|{hidden.LocalCell}|{hidden.Hint}|{hidden.RevealTools}");
            }
            foreach (GeneratedToolEscape escape in layout.ToolEscapes)
            {
                AddHash(ref hash, $"{escape.PatternId}|{escape.ChunkGridCell}|{escape.RequiredTool}");
            }
            return hash.ToString("X8");
        }

        private static uint CombineSeed(int seed, string roomId)
        {
            uint value = seed == 0 ? 0xA341316Cu : unchecked((uint)seed);
            if (roomId != null)
            {
                for (int index = 0; index < roomId.Length; index++)
                {
                    value ^= roomId[index];
                    value *= 16777619u;
                }
            }
            return value == 0 ? 0xA341316Cu : value;
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

        private struct InteriorStableRandom
        {
            private uint state;

            public InteriorStableRandom(uint seed)
            {
                state = seed == 0 ? 0xA341316Cu : seed;
            }

            public int Range(int minInclusive, int maxExclusive)
            {
                if (maxExclusive <= minInclusive) return minInclusive;
                return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
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
