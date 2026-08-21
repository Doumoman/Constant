#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.MapHarness.P11
{
    [DisallowMultipleComponent]
    public sealed class P11MapRoom2D : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField, TextArea] private string coreSentence;
        [SerializeField] private Vector2Int macroSize = Vector2Int.one;
        [SerializeField, Range(0, 2)] private int mechanicsCount;
        [SerializeField] private P11MapRoomRole roles;
        [SerializeField] private P11MapRoomZoneDefinition[] zones =
            Array.Empty<P11MapRoomZoneDefinition>();
        [SerializeField] private P11MapSocketDefinition[] sockets =
            Array.Empty<P11MapSocketDefinition>();
        [SerializeField] private bool onMainRoute = true;
        [SerializeField, Min(0)] private int mainRouteOrder;
        [SerializeField] private bool introducesNewMechanic;
        [SerializeField] private bool readableDuringMaruEscape = true;

        public string RoomId => roomId;
        public string CoreSentence => coreSentence;
        public Vector2Int MacroSize => macroSize;
        public int MechanicsCount => mechanicsCount;
        public P11MapRoomRole Roles => roles;
        public IReadOnlyList<P11MapRoomZoneDefinition> Zones => zones;
        public IReadOnlyList<P11MapSocketDefinition> Sockets => sockets;
        public bool OnMainRoute => onMainRoute;
        public int MainRouteOrder => mainRouteOrder;
        public bool IntroducesNewMechanic => introducesNewMechanic;
        public bool ReadableDuringMaruEscape =>
            readableDuringMaruEscape;
        public bool MacroSizeAllowed =>
            P11MapMacroSizeRules.IsAllowed(macroSize);
        public bool MechanicsContractSatisfied =>
            mechanicsCount >= 0 && mechanicsCount <= 2;
        public bool HasAllFiveZones => CountDistinctZones() == 5;
        public bool MainRouteToolFree => CheckMainRouteToolFree();
        public bool OptionalRouteReturnable =>
            CheckOptionalRouteReturnable();
        public int MainSocketCount =>
            CountSockets(P11MapRouteKind.Main);
        public int OptionalSocketCount =>
            CountSockets(P11MapRouteKind.Optional);

        public void Configure(
            string id,
            string oneSentenceCoreAction,
            Vector2Int roomMacroSize,
            int roomMechanicsCount,
            P11MapRoomRole roomRoles,
            P11MapRoomZoneDefinition[] roomZones,
            P11MapSocketDefinition[] roomSockets,
            bool isOnMainRoute = true,
            int routeOrder = 0,
            bool addsNewMechanic = false,
            bool maruEscapeReadable = true)
        {
            roomId = id ?? string.Empty;
            coreSentence = oneSentenceCoreAction ?? string.Empty;
            macroSize = roomMacroSize;
            mechanicsCount = roomMechanicsCount;
            roles = roomRoles;
            zones = roomZones ?? Array.Empty<P11MapRoomZoneDefinition>();
            sockets =
                roomSockets ?? Array.Empty<P11MapSocketDefinition>();
            onMainRoute = isOnMainRoute;
            mainRouteOrder = Mathf.Max(0, routeOrder);
            introducesNewMechanic = addsNewMechanic;
            readableDuringMaruEscape = maruEscapeReadable;
        }

        public void ApplyMaruEscapeReadability(bool readable)
        {
            readableDuringMaruEscape = readable;
        }

        public bool HasRole(P11MapRoomRole role)
        {
            return (roles & role) == role;
        }

        public bool TryGetZone(
            P11MapRoomZone kind,
            out P11MapRoomZoneDefinition zone)
        {
            for (int index = 0; index < zones.Length; index++)
            {
                P11MapRoomZoneDefinition candidate = zones[index];
                if (candidate != null && candidate.Kind == kind)
                {
                    zone = candidate;
                    return true;
                }
            }

            zone = null;
            return false;
        }

        public P11MapValidationReport ValidateNow()
        {
            var report = new P11MapValidationReport();
            string context = string.IsNullOrWhiteSpace(roomId)
                ? name
                : roomId;

            if (string.IsNullOrWhiteSpace(roomId))
            {
                report.AddError(
                    "ROOM_ID_MISSING",
                    "Room id must be explicit.",
                    context);
            }

            if (string.IsNullOrWhiteSpace(coreSentence))
            {
                report.AddError(
                    "ROOM_CORE_SENTENCE_MISSING",
                    "Every room needs one core action sentence.",
                    context);
            }

            if (!MacroSizeAllowed)
            {
                report.AddError(
                    "ROOM_MACRO_SIZE_INVALID",
                    "Allowed macro sizes are 1x1, 2x1, 1x2, 2x2, "
                    + "3x1, and 1x3.",
                    context);
            }

            if (!MechanicsContractSatisfied)
            {
                report.AddError(
                    "ROOM_MECHANICS_OVER_BUDGET",
                    "A room may expose at most two mechanics.",
                    context);
            }

            ValidateZones(report, context);
            ValidateSockets(report, context);
            return report;
        }

        private void ValidateZones(
            P11MapValidationReport report,
            string context)
        {
            var seen = new HashSet<P11MapRoomZone>();
            for (int index = 0; index < zones.Length; index++)
            {
                P11MapRoomZoneDefinition zone = zones[index];
                if (zone == null)
                {
                    report.AddError(
                        "ROOM_ZONE_NULL",
                        "Room zone entries cannot be null.",
                        context);
                    continue;
                }

                if (!seen.Add(zone.Kind))
                {
                    report.AddError(
                        "ROOM_ZONE_DUPLICATE",
                        $"Zone {zone.Kind} appears more than once.",
                        context);
                }

                if (zone.Anchor == null)
                {
                    report.AddError(
                        "ROOM_ZONE_ANCHOR_MISSING",
                        $"Zone {zone.Kind} has no anchor.",
                        context);
                }
            }

            foreach (P11MapRoomZone kind
                in Enum.GetValues(typeof(P11MapRoomZone)))
            {
                if (!seen.Contains(kind))
                {
                    report.AddError(
                        "ROOM_ZONE_MISSING",
                        $"Required zone {kind} is missing.",
                        context);
                }
            }

            ValidateSafeZoneCells(
                P11MapRoomZone.EntrySafe,
                2,
                report,
                context);
            ValidateSafeZoneCells(
                P11MapRoomZone.ExitSafe,
                2,
                report,
                context);
            ValidateSafeZoneCells(
                P11MapRoomZone.Recovery,
                1,
                report,
                context);
        }

        private void ValidateSafeZoneCells(
            P11MapRoomZone kind,
            int minimum,
            P11MapValidationReport report,
            string context)
        {
            if (TryGetZone(kind, out P11MapRoomZoneDefinition zone)
                && zone.SafeCellCount < minimum)
            {
                report.AddError(
                    "ROOM_SAFE_ZONE_TOO_SMALL",
                    $"{kind} needs at least {minimum} safe cell(s).",
                    context);
            }
        }

        private void ValidateSockets(
            P11MapValidationReport report,
            string context)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < sockets.Length; index++)
            {
                P11MapSocketDefinition socket = sockets[index];
                if (socket == null)
                {
                    report.AddError(
                        "ROOM_SOCKET_NULL",
                        "Room socket entries cannot be null.",
                        context);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(socket.SocketId))
                {
                    report.AddError(
                        "ROOM_SOCKET_ID_MISSING",
                        "Every socket needs an id.",
                        context);
                }
                else if (!ids.Add(socket.SocketId))
                {
                    report.AddError(
                        "ROOM_SOCKET_ID_DUPLICATE",
                        $"Socket id {socket.SocketId} is duplicated.",
                        context);
                }

                if (socket.Anchor == null)
                {
                    report.AddError(
                        "ROOM_SOCKET_ANCHOR_MISSING",
                        $"Socket {socket.SocketId} has no anchor.",
                        context);
                }

                if (!socket.IsToolFreeMainTraversal)
                {
                    report.AddError(
                        "ROOM_MAIN_SOCKET_REQUIRES_TOOL",
                        $"Main socket {socket.SocketId} is not "
                        + "base-movement traversable.",
                        context);
                }
            }

            if (onMainRoute)
            {
                int minimum = HasRole(P11MapRoomRole.Start)
                    || HasRole(P11MapRoomRole.Exit)
                    || HasRole(P11MapRoomRole.Boss)
                    ? 1
                    : 2;
                if (MainSocketCount < minimum)
                {
                    report.AddError(
                        "ROOM_MAIN_SOCKET_COUNT",
                        $"Main-route room needs at least {minimum} "
                        + "main socket(s).",
                        context);
                }
            }

            if (!OptionalRouteReturnable)
            {
                report.AddError(
                    "ROOM_OPTIONAL_ROUTE_NO_RETURN",
                    "An optional route needs a returnable socket.",
                    context);
            }
        }

        private int CountDistinctZones()
        {
            var seen = new HashSet<P11MapRoomZone>();
            for (int index = 0; index < zones.Length; index++)
            {
                if (zones[index] != null)
                {
                    seen.Add(zones[index].Kind);
                }
            }

            return seen.Count;
        }

        private bool CheckMainRouteToolFree()
        {
            for (int index = 0; index < sockets.Length; index++)
            {
                P11MapSocketDefinition socket = sockets[index];
                if (socket != null && !socket.IsToolFreeMainTraversal)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckOptionalRouteReturnable()
        {
            int optionalCount = 0;
            for (int index = 0; index < sockets.Length; index++)
            {
                P11MapSocketDefinition socket = sockets[index];
                if (socket == null || !socket.IsOptionalRoute)
                {
                    continue;
                }

                optionalCount++;
                if (socket.Returnable)
                {
                    return true;
                }
            }

            return optionalCount == 0;
        }

        private int CountSockets(P11MapRouteKind route)
        {
            int count = 0;
            for (int index = 0; index < sockets.Length; index++)
            {
                if (sockets[index] != null
                    && sockets[index].Route == route)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

#endif
