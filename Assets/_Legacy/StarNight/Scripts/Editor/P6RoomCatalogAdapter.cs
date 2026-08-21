#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEditor;
using UnityEngine;

namespace StarNight.Editor
{
    [Flags]
    internal enum P6CatalogSocketSides
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Bottom = 1 << 2,
        Top = 1 << 3
    }

    internal readonly struct P6CatalogSocketDescriptor
    {
        public readonly string SocketId;
        public readonly RoomSocketSide Side;
        public readonly Vector2Int CellOffset;
        public readonly Vector2Int OpeningSize;
        public readonly RoomTraversalType TraversalType;
        public readonly bool MainRouteAllowed;
        public readonly Vector2Int ValidationAnchor;

        public P6CatalogSocketDescriptor(RoomSocket2D socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            SocketId = socket.SocketId;
            Side = socket.Side;
            CellOffset = socket.CellOffset;
            OpeningSize = socket.OpeningSize;
            TraversalType = socket.TraversalType;
            MainRouteAllowed = socket.MainRouteAllowed;
            ValidationAnchor = socket.ValidationAnchor;
        }
    }

    internal sealed class P6RoomCatalogEntry
    {
        private readonly P6CatalogSocketDescriptor[] sockets;

        public P6RoomCatalogEntry(
            GameObject prefab,
            string prefabPath,
            RoomTemplate2D template)
        {
            Prefab = prefab != null
                ? prefab
                : throw new ArgumentNullException(nameof(prefab));
            PrefabPath = !string.IsNullOrWhiteSpace(prefabPath)
                ? prefabPath
                : throw new ArgumentException(
                    "A prefab asset path is required.",
                    nameof(prefabPath));
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            PrefabId = template.RoomId;
            Archetype = template.Archetype;
            SupportedRoles = template.Roles;
            Region = template.Region;
            MacroSize = template.MacroSize;
            LogicalSize = template.LogicalSize;
            TraversalProof =
                P6RoomTraversalProofFactory.Capture(template);

            sockets = new P6CatalogSocketDescriptor[template.Sockets.Count];
            P6CatalogSocketSides socketSides = P6CatalogSocketSides.None;
            P6CatalogSocketSides mainRouteSocketSides =
                P6CatalogSocketSides.None;
            int directSocketCapacity = 0;
            for (int index = 0; index < template.Sockets.Count; index++)
            {
                RoomSocket2D socket = template.Sockets[index];
                if (socket == null)
                {
                    throw new InvalidOperationException(
                        $"[{PrefabId}] Socket {index} is null.");
                }

                P6CatalogSocketDescriptor descriptor =
                    new P6CatalogSocketDescriptor(socket);
                sockets[index] = descriptor;

                P6CatalogSocketSides side = ToCatalogSide(descriptor.Side);
                socketSides |= side;
                if (descriptor.MainRouteAllowed
                    && RoomSocketRules.IsMainRouteTraversal(
                        descriptor.TraversalType))
                {
                    mainRouteSocketSides |= side;
                    directSocketCapacity++;
                }
            }

            SocketSides = socketSides;
            MainRouteSocketSides = mainRouteSocketSides;
            DirectSocketCapacity = directSocketCapacity;
        }

        public GameObject Prefab { get; }
        public string PrefabPath { get; }
        public string PrefabId { get; }
        public RoomArchetype Archetype { get; }
        public RoomRole SupportedRoles { get; }
        public RoomRegion Region { get; }
        public Vector2Int MacroSize { get; }
        public Vector2Int LogicalSize { get; }
        public P6RoomTraversalProof TraversalProof { get; }
        public IReadOnlyList<P6CatalogSocketDescriptor> Sockets => sockets;
        public int SocketCount => sockets.Length;
        public P6CatalogSocketSides SocketSides { get; }
        public P6CatalogSocketSides MainRouteSocketSides { get; }
        public int DirectSocketCapacity { get; }
        public int MaxUses => P6RoomCatalogAdapter.MaxUsesPerPrefab;
        public int SelectionWeight => P6RoomCatalogAdapter.DefaultSelectionWeight;
        public bool RotationAllowed => false;

        public bool SupportsAllRoles(RoomRole requiredRoles)
        {
            return requiredRoles == RoomRole.None
                || (SupportedRoles & requiredRoles) == requiredRoles;
        }

        public bool SupportsSocketSides(
            P6CatalogSocketSides requiredSides,
            bool mainRouteOnly)
        {
            P6CatalogSocketSides available = mainRouteOnly
                ? MainRouteSocketSides
                : SocketSides;
            return (available & requiredSides) == requiredSides;
        }

        private static P6CatalogSocketSides ToCatalogSide(
            RoomSocketSide side)
        {
            switch (side)
            {
                case RoomSocketSide.Left:
                    return P6CatalogSocketSides.Left;
                case RoomSocketSide.Right:
                    return P6CatalogSocketSides.Right;
                case RoomSocketSide.Bottom:
                    return P6CatalogSocketSides.Bottom;
                case RoomSocketSide.Top:
                    return P6CatalogSocketSides.Top;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(side),
                        side,
                        "Unknown room socket side.");
            }
        }
    }

    internal static class P6RoomCatalogAdapter
    {
        public const string MoonPalaceLibraryPath =
            "Assets/StarNight/Data/P4/P4_MoonRoomPrefabLibrary.asset";
        // 문서 22.1은 하한만 규정한다. 방을 더 만들어도 게이트가 막지 않아야 한다.
        public const int MinimumMoonPalaceRoomCount = 33;
        public const int MaxUsesPerPrefab = 2;
        public const int DefaultSelectionWeight = 1;

        public static P6RoomCatalogEntry[] LoadMoonPalaceEntries()
        {
            RoomPrefabLibrary library =
                AssetDatabase.LoadAssetAtPath<RoomPrefabLibrary>(
                    MoonPalaceLibraryPath);
            if (library == null)
            {
                throw new InvalidOperationException(
                    $"P4 room library is missing: {MoonPalaceLibraryPath}");
            }

            if (library.Region != RoomRegion.MoonPalace)
            {
                throw new InvalidOperationException(
                    $"P4 room library region must be {RoomRegion.MoonPalace}, "
                    + $"found {library.Region}.");
            }

            List<P6RoomCatalogEntry> entries =
                new List<P6RoomCatalogEntry>(library.RoomPrefabs.Count);
            for (int index = 0; index < library.RoomPrefabs.Count; index++)
            {
                GameObject prefab = library.RoomPrefabs[index];
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"P4 room library entry {index} is null.");
                }

                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                RoomTemplate2D template =
                    prefab.GetComponent<RoomTemplate2D>();
                if (template == null)
                {
                    throw new InvalidOperationException(
                        $"P4 room prefab has no {nameof(RoomTemplate2D)}: "
                        + prefabPath);
                }

                entries.Add(
                    new P6RoomCatalogEntry(
                        prefab,
                        prefabPath,
                        template));
            }

            P6RoomCatalogEntry[] result = entries
                .OrderBy(
                    entry => entry.PrefabId,
                    StringComparer.Ordinal)
                .ToArray();
            ValidateMoonPalaceEntries(result);
            return result;
        }

        public static P6RoomCatalogEntry[] FindCandidates(
            IReadOnlyList<P6RoomCatalogEntry> entries,
            RoomRegion region,
            RoomArchetype? archetype,
            RoomRole requiredRoles,
            P6CatalogSocketSides requiredSocketSides,
            bool mainRouteSocketsOnly)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            List<P6RoomCatalogEntry> candidates =
                new List<P6RoomCatalogEntry>();
            for (int index = 0; index < entries.Count; index++)
            {
                P6RoomCatalogEntry entry = entries[index];
                if (entry == null
                    || entry.Region != region
                    || (archetype.HasValue
                        && entry.Archetype != archetype.Value)
                    || !entry.SupportsAllRoles(requiredRoles)
                    || !entry.SupportsSocketSides(
                        requiredSocketSides,
                        mainRouteSocketsOnly))
                {
                    continue;
                }

                candidates.Add(entry);
            }

            return candidates
                .OrderBy(
                    entry => entry.PrefabId,
                    StringComparer.Ordinal)
                .ToArray();
        }

        public static P6RoomPrefabDescriptor CreateDescriptor(
            P6RoomCatalogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return new P6RoomPrefabDescriptor(
                entry.PrefabId,
                entry.Archetype,
                entry.SupportedRoles,
                entry.Region,
                entry.MacroSize,
                (P6SocketSides)(int)entry.SocketSides,
                entry.MainRouteSocketSides
                    != P6CatalogSocketSides.None,
                entry.SelectionWeight,
                entry.MaxUses,
                entry.TraversalProof);
        }

        public static bool TryFindDirectSocketPair(
            P6RoomCatalogEntry first,
            P6RoomCatalogEntry second,
            bool mainRouteOnly,
            out P6CatalogSocketDescriptor firstSocket,
            out P6CatalogSocketDescriptor secondSocket)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            for (int firstIndex = 0;
                 firstIndex < first.Sockets.Count;
                 firstIndex++)
            {
                P6CatalogSocketDescriptor candidateFirst =
                    first.Sockets[firstIndex];
                if (mainRouteOnly && !candidateFirst.MainRouteAllowed)
                {
                    continue;
                }

                for (int secondIndex = 0;
                     secondIndex < second.Sockets.Count;
                     secondIndex++)
                {
                    P6CatalogSocketDescriptor candidateSecond =
                        second.Sockets[secondIndex];
                    if (mainRouteOnly && !candidateSecond.MainRouteAllowed)
                    {
                        continue;
                    }

                    if (!AreDirectlyCompatible(
                            first,
                            candidateFirst,
                            second,
                            candidateSecond))
                    {
                        continue;
                    }

                    firstSocket = candidateFirst;
                    secondSocket = candidateSecond;
                    return true;
                }
            }

            firstSocket = default;
            secondSocket = default;
            return false;
        }

        public static bool CanReserve(
            P6RoomCatalogEntry entry,
            IReadOnlyDictionary<string, int> currentUses)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (currentUses == null)
            {
                throw new ArgumentNullException(nameof(currentUses));
            }

            return !currentUses.TryGetValue(
                    entry.PrefabId,
                    out int uses)
                || uses < entry.MaxUses;
        }

        public static void ValidateMoonPalaceEntries(
            IReadOnlyList<P6RoomCatalogEntry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (entries.Count < MinimumMoonPalaceRoomCount)
            {
                throw new InvalidOperationException(
                    "P6 requires the complete P4 Moon Palace catalog: "
                    + $"at least {MinimumMoonPalaceRoomCount} rooms expected, "
                    + $"{entries.Count} found.");
            }

            HashSet<string> prefabIds =
                new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> prefabPaths =
                new HashSet<string>(StringComparer.Ordinal);
            List<RoomTemplate2D> templates =
                new List<RoomTemplate2D>(entries.Count);
            RoomCategoryCounts counts = default;

            for (int index = 0; index < entries.Count; index++)
            {
                P6RoomCatalogEntry entry = entries[index];
                if (entry == null)
                {
                    throw new InvalidOperationException(
                        $"P6 room catalog entry {index} is null.");
                }

                ValidateEntry(entry);
                if (!prefabIds.Add(entry.PrefabId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate P4 room id: {entry.PrefabId}");
                }

                if (!prefabPaths.Add(entry.PrefabPath))
                {
                    throw new InvalidOperationException(
                        $"Duplicate P4 room prefab path: {entry.PrefabPath}");
                }

                RoomTemplate2D template =
                    entry.Prefab.GetComponent<RoomTemplate2D>();
                templates.Add(template);
                AddCategory(entry, ref counts);
            }

            ValidateCategoryCounts(counts);
            RoomValidationReport report =
                RoomPrefabValidator.ValidateLibrary(templates);
            if (!report.Passed)
            {
                throw new InvalidOperationException(
                    "P4 room validation failed before P6 generation:"
                    + Environment.NewLine
                    + report);
            }
        }

        private static void ValidateEntry(P6RoomCatalogEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.PrefabId))
            {
                throw new InvalidOperationException(
                    $"P4 room id is empty: {entry.PrefabPath}");
            }

            if (entry.Region != RoomRegion.MoonPalace)
            {
                throw new InvalidOperationException(
                    $"[{entry.PrefabId}] Region must be "
                    + $"{RoomRegion.MoonPalace}, found {entry.Region}.");
            }

            if (entry.MacroSize.x <= 0 || entry.MacroSize.y <= 0)
            {
                throw new InvalidOperationException(
                    $"[{entry.PrefabId}] Macro size must be positive.");
            }

            Vector2Int macroCellSize = RoomTemplate2D.MacroCellSize;
            Vector2Int maximumLogicalSize = new Vector2Int(
                entry.MacroSize.x * macroCellSize.x,
                entry.MacroSize.y * macroCellSize.y);
            if (entry.LogicalSize.x <= 0
                || entry.LogicalSize.y <= 0
                || entry.LogicalSize.x > maximumLogicalSize.x
                || entry.LogicalSize.y > maximumLogicalSize.y)
            {
                throw new InvalidOperationException(
                    $"[{entry.PrefabId}] Logical size {entry.LogicalSize} "
                    + $"does not fit macro footprint {entry.MacroSize} "
                    + $"({maximumLogicalSize} cells).");
            }

            if (entry.SocketCount <= 0)
            {
                throw new InvalidOperationException(
                    $"[{entry.PrefabId}] At least one authored socket is "
                    + "required.");
            }

            if (entry.TraversalProof == null
                || !entry.TraversalProof.Passed)
            {
                string issues =
                    entry.TraversalProof == null
                        ? "proof was not captured"
                        : string.Join(
                            Environment.NewLine,
                            entry.TraversalProof.ValidationIssues);
                throw new InvalidOperationException(
                    $"[{entry.PrefabId}] P6 traversal proof failed: "
                    + issues);
            }

            HashSet<string> socketIds =
                new HashSet<string>(StringComparer.Ordinal);
            for (int socketIndex = 0;
                 socketIndex < entry.Sockets.Count;
                 socketIndex++)
            {
                P6CatalogSocketDescriptor socket =
                    entry.Sockets[socketIndex];
                if (string.IsNullOrWhiteSpace(socket.SocketId)
                    || !socketIds.Add(socket.SocketId))
                {
                    throw new InvalidOperationException(
                        $"[{entry.PrefabId}] Socket ids must be non-empty "
                        + "and unique.");
                }
            }

            if (entry.MaxUses != 2)
            {
                throw new InvalidOperationException(
                    $"[{entry.PrefabId}] P6 repeat cap must remain 2.");
            }

            if (entry.RotationAllowed)
            {
                throw new InvalidOperationException(
                    $"[{entry.PrefabId}] Authored P4 rooms cannot rotate.");
            }
        }

        private static bool AreDirectlyCompatible(
            P6RoomCatalogEntry firstRoom,
            P6CatalogSocketDescriptor first,
            P6RoomCatalogEntry secondRoom,
            P6CatalogSocketDescriptor second)
        {
            if (!RoomSocketRules.AreOpposite(first.Side, second.Side)
                || first.OpeningSize != second.OpeningSize
                || first.TraversalType != second.TraversalType
                || first.TraversalType == RoomTraversalType.ExitOnly)
            {
                return false;
            }

            bool laneMatches =
                first.Side == RoomSocketSide.Left
                || first.Side == RoomSocketSide.Right
                    ? PositiveModulo(first.CellOffset.y, 8)
                        == PositiveModulo(second.CellOffset.y, 8)
                    : PositiveModulo(first.CellOffset.x, 12)
                        == PositiveModulo(second.CellOffset.x, 12);
            if (!laneMatches)
            {
                return false;
            }

            return firstRoom.Region == RoomRegion.Universal
                || secondRoom.Region == RoomRegion.Universal
                || firstRoom.Region == secondRoom.Region;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int remainder = value % divisor;
            return remainder < 0
                ? remainder + divisor
                : remainder;
        }

        private static void AddCategory(
            P6RoomCatalogEntry entry,
            ref RoomCategoryCounts counts)
        {
            int specialRoleCount = 0;
            if (entry.SupportsAllRoles(RoomRole.ShopCorridor))
            {
                counts.Shop++;
                specialRoleCount++;
            }

            if (entry.SupportsAllRoles(RoomRole.SupplyCorridor))
            {
                counts.Supply++;
                specialRoleCount++;
            }

            if (entry.SupportsAllRoles(RoomRole.RecordRoom))
            {
                counts.Record++;
                specialRoleCount++;
            }

            if (entry.SupportsAllRoles(RoomRole.MaruStatue))
            {
                counts.Maru++;
                specialRoleCount++;
            }

            if (specialRoleCount > 1)
            {
                throw new InvalidOperationException(
                    $"[{entry.PrefabId}] A P4 prefab cannot satisfy "
                    + "multiple special-room quotas.");
            }

            if (specialRoleCount != 0)
            {
                return;
            }

            switch (entry.Archetype)
            {
                case RoomArchetype.Small:
                    counts.Small++;
                    break;
                case RoomArchetype.Standard:
                    counts.Standard++;
                    break;
                case RoomArchetype.Wide:
                    counts.Wide++;
                    break;
                case RoomArchetype.Tall:
                    counts.Tall++;
                    break;
                case RoomArchetype.Large:
                    counts.Large++;
                    break;
            }
        }

        private static void ValidateCategoryCounts(
            RoomCategoryCounts counts)
        {
            List<string> shortfalls = new List<string>();
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.MicroStandardRoom,
                counts.Small + counts.Standard);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.WideRoom,
                counts.Wide);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.TallRoom,
                counts.Tall);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.LargeRoom,
                counts.Large);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.RestCorridorRoom,
                counts.Shop + counts.Supply);
            RequireQuotaMinimum(
                shortfalls,
                RoomContentQuotaCategory.RecordRoom,
                counts.Record);

            // 특수 역할은 종류마다 최소 1종이 있어야 P6가 예약 배치를 할 수 있다.
            RequireMinimum(shortfalls, "shop corridor", counts.Shop, 1);
            RequireMinimum(shortfalls, "supply corridor", counts.Supply, 1);
            RequireMinimum(shortfalls, "Maru statue", counts.Maru, 1);

            if (shortfalls.Count == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                "P4 room category counts fall below the documented minimums: "
                + string.Join(", ", shortfalls));
        }

        private static void RequireQuotaMinimum(
            List<string> shortfalls,
            RoomContentQuotaCategory category,
            int actual)
        {
            if (!RoomContentQuota.TryGetRule(
                    category,
                    out RoomContentQuotaRule rule)
                || !rule.LibraryHardRequirement)
            {
                return;
            }

            RequireMinimum(shortfalls, rule.DisplayName, actual, rule.Minimum);
        }

        private static void RequireMinimum(
            List<string> shortfalls,
            string label,
            int actual,
            int minimum)
        {
            if (actual < minimum)
            {
                shortfalls.Add($"{label} {actual}/{minimum}");
            }
        }

        private struct RoomCategoryCounts
        {
            public int Small;
            public int Standard;
            public int Wide;
            public int Tall;
            public int Large;
            public int Shop;
            public int Supply;
            public int Record;
            public int Maru;
        }
    }
}

#endif
