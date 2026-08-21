#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Maru.P8;
using UnityEngine;

namespace StarNight.MapHarness.P11
{
    [DisallowMultipleComponent]
    public sealed class P11MapStageHarness2D : MonoBehaviour
    {
        public const float MinimumMaruEscapeSeconds = 25f;
        public const float MaximumMaruEscapeSeconds = 45f;
        public const float DefaultMaruMoveSpeed =
            P8MaruPursuer2D.DefaultMoveSpeed;
        public const float MacroCellWidth = 12f;
        public const float MacroCellHeight = 8f;
        public const float MinimumReadableRoomWidth = 12f;
        public const float MinimumReadableRoomHeight = 8f;
        public const int MinimumReadableExitSafeCells = 2;
        public const float MinimumMaruEscapeClearance = 2f;

        [SerializeField] private string stageId;
        [SerializeField] private P11MapRegion region;
        [SerializeField] private P11MapStageSlot stageSlot;
        [SerializeField] private P11MapStageArchetype archetype;
        [SerializeField] private P11MapMainDirection mainDirection;
        [SerializeField] private Transform landmark;
        [SerializeField] private P11MapRoom2D[] rooms =
            Array.Empty<P11MapRoom2D>();
        [SerializeField] private P11MapDirectionCue2D[] cues =
            Array.Empty<P11MapDirectionCue2D>();
        [SerializeField, Min(0)] private int loopCount;
        [SerializeField, Min(0)] private int directionReversalCount;
        [SerializeField] private bool baseStartToExitReachable = true;
        [SerializeField] private bool maruEscapeReachable = true;
        [SerializeField, Min(0f)] private float maruEscapeSeconds = 35f;
        [SerializeField] private bool maruEscapeSecondsMeasured;
        [SerializeField] private bool previousActionPayoffVisible = true;
        [SerializeField] private bool entryDirectionPreviewEnabled = true;
        [SerializeField, Range(0f, 10f)]
        private float exitDirectionPreviewSeconds = 1f;
        [SerializeField] private P11MapValidationReport lastValidation =
            new P11MapValidationReport();

        public string StageId => stageId;
        public P11MapRegion Region => region;
        public P11MapStageSlot StageSlot => stageSlot;
        public P11MapStageArchetype Archetype => archetype;
        public P11MapMainDirection MainDirection => mainDirection;
        public Transform Landmark => landmark;
        public IReadOnlyList<P11MapRoom2D> Rooms => rooms;
        public IReadOnlyList<P11MapDirectionCue2D> Cues => cues;
        public int LoopCount => loopCount;
        public int DirectionReversalCount => directionReversalCount;
        public bool BaseStartToExitReachable =>
            baseStartToExitReachable;
        public bool MaruEscapeReachable => maruEscapeReachable;
        public float MaruEscapeSeconds => maruEscapeSeconds;
        public bool MaruEscapeSecondsMeasured =>
            maruEscapeSecondsMeasured;
        public bool MaruEscapeTimeWithinContract =>
            IsMaruEscapeTimeWithinContract(maruEscapeSeconds);
        public bool PreviousActionPayoffVisible =>
            previousActionPayoffVisible;
        public bool EntryDirectionPreviewEnabled =>
            entryDirectionPreviewEnabled;
        public float ExitDirectionPreviewSeconds =>
            exitDirectionPreviewSeconds;
        public P11MapValidationReport LastValidation =>
            lastValidation;
        public bool ValidationPassed =>
            lastValidation != null && lastValidation.Passed;
        public bool MainPathToolFree => CheckMainPathToolFree();
        public bool ExitDirectionContractSatisfied =>
            CheckExitDirectionContract();
        public bool CueRoutesSeparated => CheckCueRouteSeparation();

        public void Configure(
            string id,
            P11MapRegion stageRegion,
            P11MapStageSlot slot,
            P11MapStageArchetype stageArchetype,
            P11MapMainDirection direction,
            Transform stageLandmark,
            P11MapRoom2D[] stageRooms,
            P11MapDirectionCue2D[] directionCues,
            int loops,
            int directionReversals,
            bool startToExitReachable = true,
            bool maruCanReachExit = true,
            float maruExitSeconds = 35f,
            bool priorActionPayoffIsVisible = true,
            bool showEntryDirectionPreview = true,
            float directionPreviewSeconds = 1f,
            bool maruExitSecondsAreMeasured = false)
        {
            stageId = id ?? string.Empty;
            region = stageRegion;
            stageSlot = slot;
            archetype = stageArchetype;
            mainDirection = direction;
            landmark = stageLandmark;
            rooms = stageRooms ?? Array.Empty<P11MapRoom2D>();
            cues =
                directionCues ?? Array.Empty<P11MapDirectionCue2D>();
            loopCount = Mathf.Max(0, loops);
            directionReversalCount = Mathf.Max(
                0,
                directionReversals);
            baseStartToExitReachable = startToExitReachable;
            maruEscapeReachable = maruCanReachExit;
            maruEscapeSeconds = Mathf.Max(0f, maruExitSeconds);
            maruEscapeSecondsMeasured = maruExitSecondsAreMeasured;
            previousActionPayoffVisible =
                priorActionPayoffIsVisible;
            entryDirectionPreviewEnabled =
                showEntryDirectionPreview;
            exitDirectionPreviewSeconds = Mathf.Clamp(
                directionPreviewSeconds,
                0f,
                10f);
            ValidateNow();
        }

        public P11MapValidationReport ValidateNow()
        {
            var report = new P11MapValidationReport();
            string context = string.IsNullOrWhiteSpace(stageId)
                ? name
                : stageId;

            ValidateIdentity(report, context);
            ValidateRooms(report, context);
            ValidateStageRhythm(report, context);
            ValidateDirections(report, context);
            ValidateCues(report, context);
            ValidateMaruEscape(report, context);
            lastValidation = report;
            return report;
        }

        public int CountRoomsWithRole(P11MapRoomRole role)
        {
            int count = 0;
            for (int index = 0; index < rooms.Length; index++)
            {
                if (rooms[index] != null && rooms[index].HasRole(role))
                {
                    count++;
                }
            }

            return count;
        }

        public int CountCues(P11MapCueKind kind)
        {
            int count = 0;
            for (int index = 0; index < cues.Length; index++)
            {
                if (cues[index] != null
                    && cues[index].CueKind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        public bool HasRequiredRole(P11MapRoomRole role)
        {
            return CountRoomsWithRole(role) > 0;
        }

        public static bool IsMaruEscapeTimeWithinContract(
            float seconds)
        {
            return seconds >= MinimumMaruEscapeSeconds
                && seconds <= MaximumMaruEscapeSeconds;
        }

        public static float MeasureMainRoutePathLength(
            IReadOnlyList<P11MapRoom2D> stageRooms)
        {
            List<P11MapRoom2D> ordered =
                OrderMainRouteRooms(stageRooms);
            float length = 0f;
            for (int index = 0; index + 1 < ordered.Count; index++)
            {
                length += Vector2.Distance(
                    ordered[index].transform.position,
                    ordered[index + 1].transform.position);
            }

            return length;
        }

        public static float EstimateMaruEscapeSeconds(
            IReadOnlyList<P11MapRoom2D> stageRooms,
            float maruMoveSpeed = DefaultMaruMoveSpeed)
        {
            float speed = Mathf.Max(0.1f, maruMoveSpeed);
            return MeasureMainRoutePathLength(stageRooms) / speed;
        }

        public static Vector2 MeasureRoomWorldSize(P11MapRoom2D room)
        {
            return room == null
                ? Vector2.zero
                : new Vector2(
                    room.MacroSize.x * MacroCellWidth,
                    room.MacroSize.y * MacroCellHeight);
        }

        public static bool IsRoomReadableDuringMaruEscape(
            P11MapRoom2D room,
            Vector2 maruEntryPosition)
        {
            if (room == null)
            {
                return false;
            }

            Vector2 size = MeasureRoomWorldSize(room);
            if (size.x < MinimumReadableRoomWidth
                || size.y < MinimumReadableRoomHeight)
            {
                return false;
            }

            if (!room.TryGetZone(
                    P11MapRoomZone.ExitSafe,
                    out P11MapRoomZoneDefinition exitSafe)
                || exitSafe.Anchor == null
                || exitSafe.SafeCellCount
                    < MinimumReadableExitSafeCells)
            {
                return false;
            }

            return HasClearEscapeSocket(room, maruEntryPosition);
        }

        private static bool HasClearEscapeSocket(
            P11MapRoom2D room,
            Vector2 maruEntryPosition)
        {
            IReadOnlyList<P11MapSocketDefinition> sockets =
                room.Sockets;
            for (int index = 0; index < sockets.Count; index++)
            {
                P11MapSocketDefinition socket = sockets[index];
                if (!IsEscapeSocket(room, socket))
                {
                    continue;
                }

                if (Vector2.Distance(
                        socket.Anchor.position,
                        maruEntryPosition)
                    >= MinimumMaruEscapeClearance)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEscapeSocket(
            P11MapRoom2D room,
            P11MapSocketDefinition socket)
        {
            if (socket == null || socket.Anchor == null)
            {
                return false;
            }

            return room.OnMainRoute
                ? socket.IsMainRoute
                  && socket.IsToolFreeMainTraversal
                : socket.Returnable;
        }

        private static List<P11MapRoom2D> OrderMainRouteRooms(
            IReadOnlyList<P11MapRoom2D> stageRooms)
        {
            var ordered = new List<P11MapRoom2D>();
            if (stageRooms == null)
            {
                return ordered;
            }

            for (int index = 0; index < stageRooms.Count; index++)
            {
                P11MapRoom2D room = stageRooms[index];
                if (room != null && room.OnMainRoute)
                {
                    ordered.Add(room);
                }
            }

            ordered.Sort((left, right) =>
                left.MainRouteOrder.CompareTo(right.MainRouteOrder));
            return ordered;
        }

        private void ValidateIdentity(
            P11MapValidationReport report,
            string context)
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                report.AddError(
                    "STAGE_ID_MISSING",
                    "Stage id must be explicit.",
                    context);
            }

            if (landmark == null)
            {
                report.AddError(
                    "STAGE_LANDMARK_MISSING",
                    "A background landmark must anchor exit direction.",
                    context);
            }
        }

        private void ValidateRooms(
            P11MapValidationReport report,
            string context)
        {
            if (rooms.Length == 0)
            {
                report.AddError(
                    "STAGE_ROOMS_MISSING",
                    "Stage needs authored room contracts.",
                    context);
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var mainOrders = new HashSet<int>();
            for (int index = 0; index < rooms.Length; index++)
            {
                P11MapRoom2D room = rooms[index];
                if (room == null)
                {
                    report.AddError(
                        "STAGE_ROOM_NULL",
                        "Stage room entries cannot be null.",
                        context);
                    continue;
                }

                report.AddRange(room.ValidateNow(), context);
                if (!string.IsNullOrWhiteSpace(room.RoomId)
                    && !ids.Add(room.RoomId))
                {
                    report.AddError(
                        "STAGE_ROOM_ID_DUPLICATE",
                        $"Room id {room.RoomId} is duplicated.",
                        context);
                }

                if (room.OnMainRoute
                    && !mainOrders.Add(room.MainRouteOrder))
                {
                    report.AddError(
                        "STAGE_MAIN_ORDER_DUPLICATE",
                        $"Main route order {room.MainRouteOrder} "
                        + "is duplicated.",
                        context);
                }

                if (stageSlot == P11MapStageSlot.X1
                    && !P11MapMacroSizeRules.IsX1Allowed(
                        room.MacroSize))
                {
                    report.AddError(
                        "STAGE_X1_SPECIAL_SIZE",
                        "X-1 cannot use 3x1 or 1x3 rooms.",
                        room.RoomId);
                }

                if (stageSlot == P11MapStageSlot.X3
                    && room.IntroducesNewMechanic)
                {
                    report.AddError(
                        "STAGE_X3_NEW_MECHANIC",
                        "X-3 may only recombine learned mechanics.",
                        room.RoomId);
                }
            }

            ValidateMainRouteOrdering(report, context);
            ValidateRoomSizeDiversity(report, context);
        }

        private void ValidateStageRhythm(
            P11MapValidationReport report,
            string context)
        {
            int nonBossRooms = CountNonBossRooms();
            int bossRooms = CountRoomsWithRole(P11MapRoomRole.Boss);
            int mainRooms = CountMainRouteNonBossRooms();

            switch (stageSlot)
            {
                case P11MapStageSlot.X1:
                    ValidateRange(
                        nonBossRooms,
                        9,
                        11,
                        "STAGE_X1_ROOM_COUNT",
                        "X-1 needs 9-11 rooms.",
                        report,
                        context);
                    ValidateRange(
                        loopCount,
                        0,
                        1,
                        "STAGE_X1_LOOP_COUNT",
                        "X-1 allows 0-1 loop.",
                        report,
                        context);
                    ValidateRange(
                        mainRooms,
                        6,
                        8,
                        "STAGE_X1_MAIN_ROUTE_COUNT",
                        "X-1 main route needs 6-8 rooms.",
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.Start,
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.Main,
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.Exit,
                        report,
                        context);
                    if (CountRoomsWithRole(P11MapRoomRole.Teach) < 2)
                    {
                        report.AddError(
                            "STAGE_X1_TEACH_ROOMS",
                            "The first two X-1 rooms must safely teach.",
                            context);
                    }

                    ValidateFirstTwoTeachRooms(report, context);
                    break;

                case P11MapStageSlot.X2:
                    ValidateRange(
                        nonBossRooms,
                        12,
                        16,
                        "STAGE_X2_ROOM_COUNT",
                        "X-2 needs 12-16 rooms.",
                        report,
                        context);
                    ValidateRange(
                        loopCount,
                        2,
                        3,
                        "STAGE_X2_LOOP_COUNT",
                        "X-2 needs 2-3 loops.",
                        report,
                        context);
                    ValidateRange(
                        mainRooms,
                        8,
                        11,
                        "STAGE_X2_MAIN_ROUTE_COUNT",
                        "X-2 main route needs 8-11 rooms.",
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.Start,
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.Landmark,
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.MaruRoom,
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.RecordRoom,
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.ShopCorridor,
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.Exit,
                        report,
                        context);
                    if (!HasRequiredRole(P11MapRoomRole.LocalEvent)
                        && !HasRequiredRole(P11MapRoomRole.RelicRoom))
                    {
                        report.AddError(
                            "STAGE_X2_CHAIN_ROLE_MISSING",
                            "X-2 needs a local event or relic route.",
                            context);
                    }

                    if (!HasLargeOrSpecialRoom())
                    {
                        report.AddError(
                            "STAGE_X2_LARGE_ROOM_MISSING",
                            "X-2 needs a 2x2, 3x1, or 1x3 room.",
                            context);
                    }

                    break;

                case P11MapStageSlot.X3:
                    ValidateRange(
                        nonBossRooms,
                        7,
                        9,
                        "STAGE_X3_ROOM_COUNT",
                        "X-3 needs 7-9 pre-boss rooms.",
                        report,
                        context);
                    ValidateRange(
                        loopCount,
                        0,
                        1,
                        "STAGE_X3_LOOP_COUNT",
                        "X-3 allows 0-1 loop.",
                        report,
                        context);
                    ValidateRange(
                        mainRooms,
                        5,
                        7,
                        "STAGE_X3_MAIN_ROUTE_COUNT",
                        "X-3 main route needs 5-7 pre-boss rooms.",
                        report,
                        context);
                    if (bossRooms != 1)
                    {
                        report.AddError(
                            "STAGE_X3_BOSS_COUNT",
                            "X-3 needs exactly one boss room.",
                            context);
                    }

                    RequireRole(
                        P11MapRoomRole.Start,
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.SupplyCorridor,
                        report,
                        context);
                    RequireRole(
                        P11MapRoomRole.Exit,
                        report,
                        context);
                    if (!previousActionPayoffVisible)
                    {
                        report.AddError(
                            "STAGE_X3_PAYOFF_MISSING",
                            "Previous actions must visibly affect "
                            + "the X-3 main route.",
                            context);
                    }

                    break;
            }

            if (!baseStartToExitReachable)
            {
                report.AddError(
                    "STAGE_START_EXIT_UNREACHABLE",
                    "Base movement must reach the exit.",
                    context);
            }

            if (!MainPathToolFree)
            {
                report.AddError(
                    "STAGE_MAIN_PATH_TOOL_REQUIRED",
                    "Main-route sockets must remain tool-free.",
                    context);
            }
        }

        private void ValidateDirections(
            P11MapValidationReport report,
            string context)
        {
            int reversalLimit =
                region == P11MapRegion.PolarisObservatory
                && stageSlot == P11MapStageSlot.X2
                    ? 1
                    : 2;
            if (directionReversalCount > reversalLimit)
            {
                report.AddError(
                    "STAGE_DIRECTION_REVERSALS",
                    $"Direction reversals exceed {reversalLimit}.",
                    context);
            }

            if (!entryDirectionPreviewEnabled
                || exitDirectionPreviewSeconds <= 0f
                || exitDirectionPreviewSeconds > 10f)
            {
                report.AddError(
                    "STAGE_EXIT_DIRECTION_10S",
                    "Entry preview must reveal exit direction "
                    + "within ten seconds.",
                    context);
            }

            if (region == P11MapRegion.PolarisObservatory
                && mainDirection != P11MapMainDirection.Up
                && mainDirection != P11MapMainDirection.Center)
            {
                report.AddError(
                    "STAGE_POLARIS_DIRECTION",
                    "Observatory exit direction must stay up "
                    + "or centered on Polaris.",
                    context);
            }
        }

        private void ValidateCues(
            P11MapValidationReport report,
            string context)
        {
            if (cues.Length == 0)
            {
                report.AddError(
                    "STAGE_CUES_MISSING",
                    "Stage needs text-free direction cues.",
                    context);
                return;
            }

            for (int index = 0; index < cues.Length; index++)
            {
                P11MapDirectionCue2D cue = cues[index];
                if (cue == null)
                {
                    report.AddError(
                        "STAGE_CUE_NULL",
                        "Direction cue entries cannot be null.",
                        context);
                    continue;
                }

                if (!cue.RouteContractSatisfied)
                {
                    report.AddError(
                        "STAGE_CUE_ROUTE_MIXED",
                        $"{cue.CueKind} belongs to "
                        + $"{P11MapCueRules.ExpectedRoute(cue.CueKind)}.",
                        cue.name);
                }

                if (cue.Target == null)
                {
                    report.AddError(
                        "STAGE_CUE_TARGET_MISSING",
                        $"{cue.CueKind} needs a physical target.",
                        cue.name);
                }

                if (!cue.TextFreeVisualReady)
                {
                    report.AddError(
                        "STAGE_CUE_SPRITE_MISSING",
                        $"{cue.CueKind} needs a sprite visual.",
                        cue.name);
                }
            }

            RequireVisibleEntryCue(
                P11MapCueKind.MainExit,
                report,
                context);
            RequireVisibleEntryCue(
                P11MapCueKind.ExitEdge,
                report,
                context);
            RequireVisibleEntryCue(
                P11MapCueKind.Landmark,
                report,
                context);

            if (HasRequiredRole(P11MapRoomRole.RecordRoom))
            {
                RequireCue(
                    P11MapCueKind.Archive,
                    report,
                    context);
            }

            if (HasRequiredRole(P11MapRoomRole.MaruRoom))
            {
                RequireCue(
                    P11MapCueKind.Homecoming,
                    report,
                    context);
            }

            if (HasRequiredRole(P11MapRoomRole.LocalEvent)
                || HasRequiredRole(P11MapRoomRole.RelicRoom))
            {
                RequireCue(
                    P11MapCueKind.Folklore,
                    report,
                    context);
            }

            if (HasRequiredRole(P11MapRoomRole.Choice)
                || HasRequiredRole(P11MapRoomRole.Treasure))
            {
                RequireCue(
                    P11MapCueKind.OptionalReward,
                    report,
                    context);
            }
        }

        private void ValidateMaruEscape(
            P11MapValidationReport report,
            string context)
        {
            if (!maruEscapeReachable)
            {
                report.AddError(
                    "STAGE_MARU_ESCAPE_UNREACHABLE",
                    "Maru arrival must leave a readable exit route.",
                    context);
            }

            if (!IsMaruEscapeTimeWithinContract(maruEscapeSeconds))
            {
                string message =
                    "Base traversal after Maru appears must take "
                    + $"{MinimumMaruEscapeSeconds:0}-"
                    + $"{MaximumMaruEscapeSeconds:0} seconds, "
                    + $"but the route yields {maruEscapeSeconds:0.##}.";
                if (maruEscapeSecondsMeasured)
                {
                    report.AddWarning(
                        "STAGE_MARU_ESCAPE_TIME_MEASURED",
                        message,
                        context);
                }
                else
                {
                    report.AddError(
                        "STAGE_MARU_ESCAPE_TIME",
                        message,
                        context);
                }
            }

            P11MapRoom2D finalRoom = FindFinalMainRouteRoom();
            if (finalRoom != null
                && !finalRoom.ReadableDuringMaruEscape)
            {
                report.AddError(
                    "STAGE_MARU_FINAL_ROOM_UNREADABLE",
                    "The final route room must remain readable "
                    + "during pursuit.",
                    finalRoom.RoomId);
            }
        }

        private void ValidateFirstTwoTeachRooms(
            P11MapValidationReport report,
            string context)
        {
            P11MapRoom2D first = FindMainRoomByOrder(0);
            P11MapRoom2D second = FindMainRoomByOrder(1);
            if (first == null || second == null
                || !first.HasRole(P11MapRoomRole.Teach)
                || !second.HasRole(P11MapRoomRole.Teach)
                || first.MechanicsCount > 1
                || second.MechanicsCount > 1)
            {
                report.AddError(
                    "STAGE_X1_FIRST_TWO_NOT_SAFE_TEACH",
                    "Main orders 0 and 1 must be one-mechanic "
                    + "Teach rooms.",
                    context);
            }
        }

        private void ValidateMainRouteOrdering(
            P11MapValidationReport report,
            string context)
        {
            var orders = new List<int>();
            for (int index = 0; index < rooms.Length; index++)
            {
                P11MapRoom2D room = rooms[index];
                if (room != null && room.OnMainRoute)
                {
                    orders.Add(room.MainRouteOrder);
                }
            }

            orders.Sort();
            for (int index = 0; index < orders.Count; index++)
            {
                if (orders[index] != index)
                {
                    report.AddError(
                        "STAGE_MAIN_ORDER_GAP",
                        "Main route order must be contiguous from zero.",
                        context);
                    break;
                }
            }

            P11MapRoom2D first = FindMainRoomByOrder(0);
            P11MapRoom2D finalRoom = FindFinalMainRouteRoom();
            if (first != null
                && !first.HasRole(P11MapRoomRole.Start))
            {
                report.AddError(
                    "STAGE_MAIN_START_ROLE",
                    "Main route order zero must be the Start room.",
                    first.RoomId);
            }

            if (finalRoom != null
                && !finalRoom.HasRole(P11MapRoomRole.Exit)
                && !finalRoom.HasRole(P11MapRoomRole.Boss))
            {
                report.AddError(
                    "STAGE_MAIN_END_ROLE",
                    "Final main room must be Exit or Boss.",
                    finalRoom.RoomId);
            }
        }

        private void ValidateRoomSizeDiversity(
            P11MapValidationReport report,
            string context)
        {
            var sizes = new HashSet<Vector2Int>();
            for (int index = 0; index < rooms.Length; index++)
            {
                P11MapRoom2D room = rooms[index];
                if (room != null
                    && !room.HasRole(P11MapRoomRole.Boss))
                {
                    sizes.Add(room.MacroSize);
                }
            }

            int required = stageSlot == P11MapStageSlot.X2 ? 3 : 2;
            if (sizes.Count < required)
            {
                report.AddError(
                    "STAGE_ROOM_SIZE_VARIETY",
                    $"Stage needs at least {required} distinct "
                    + "macro room sizes.",
                    context);
            }
        }

        private void RequireRole(
            P11MapRoomRole role,
            P11MapValidationReport report,
            string context)
        {
            if (!HasRequiredRole(role))
            {
                report.AddError(
                    "STAGE_REQUIRED_ROLE_MISSING",
                    $"Required role {role} is missing.",
                    context);
            }
        }

        private void RequireCue(
            P11MapCueKind kind,
            P11MapValidationReport report,
            string context)
        {
            if (CountCues(kind) == 0)
            {
                report.AddError(
                    "STAGE_REQUIRED_CUE_MISSING",
                    $"Required cue {kind} is missing.",
                    context);
            }
        }

        private void RequireVisibleEntryCue(
            P11MapCueKind kind,
            P11MapValidationReport report,
            string context)
        {
            for (int index = 0; index < cues.Length; index++)
            {
                P11MapDirectionCue2D cue = cues[index];
                if (cue != null
                    && cue.CueKind == kind
                    && cue.VisibleAtEntry
                    && cue.RouteVisible
                    && cue.Route == P11MapRouteKind.Main)
                {
                    return;
                }
            }

            report.AddError(
                "STAGE_EXIT_CUE_NOT_VISIBLE",
                $"{kind} must be visible from stage entry.",
                context);
        }

        private bool CheckMainPathToolFree()
        {
            for (int index = 0; index < rooms.Length; index++)
            {
                P11MapRoom2D room = rooms[index];
                if (room != null
                    && room.OnMainRoute
                    && !room.MainRouteToolFree)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckExitDirectionContract()
        {
            if (!entryDirectionPreviewEnabled
                || exitDirectionPreviewSeconds <= 0f
                || exitDirectionPreviewSeconds > 10f
                || landmark == null)
            {
                return false;
            }

            return HasVisibleEntryCue(P11MapCueKind.MainExit)
                && HasVisibleEntryCue(P11MapCueKind.ExitEdge)
                && HasVisibleEntryCue(P11MapCueKind.Landmark);
        }

        private bool CheckCueRouteSeparation()
        {
            for (int index = 0; index < cues.Length; index++)
            {
                if (cues[index] != null
                    && !cues[index].RouteContractSatisfied)
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasVisibleEntryCue(P11MapCueKind kind)
        {
            for (int index = 0; index < cues.Length; index++)
            {
                P11MapDirectionCue2D cue = cues[index];
                if (cue != null
                    && cue.CueKind == kind
                    && cue.VisibleAtEntry
                    && cue.RouteVisible
                    && cue.Route == P11MapRouteKind.Main)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountNonBossRooms()
        {
            int count = 0;
            for (int index = 0; index < rooms.Length; index++)
            {
                if (rooms[index] != null
                    && !rooms[index].HasRole(P11MapRoomRole.Boss))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountMainRouteNonBossRooms()
        {
            int count = 0;
            for (int index = 0; index < rooms.Length; index++)
            {
                P11MapRoom2D room = rooms[index];
                if (room != null
                    && room.OnMainRoute
                    && !room.HasRole(P11MapRoomRole.Boss))
                {
                    count++;
                }
            }

            return count;
        }

        private bool HasLargeOrSpecialRoom()
        {
            for (int index = 0; index < rooms.Length; index++)
            {
                if (rooms[index] != null
                    && P11MapMacroSizeRules.IsLargeOrSpecial(
                        rooms[index].MacroSize))
                {
                    return true;
                }
            }

            return false;
        }

        private P11MapRoom2D FindMainRoomByOrder(int order)
        {
            for (int index = 0; index < rooms.Length; index++)
            {
                P11MapRoom2D room = rooms[index];
                if (room != null
                    && room.OnMainRoute
                    && room.MainRouteOrder == order)
                {
                    return room;
                }
            }

            return null;
        }

        private P11MapRoom2D FindFinalMainRouteRoom()
        {
            P11MapRoom2D finalRoom = null;
            int highestOrder = -1;
            for (int index = 0; index < rooms.Length; index++)
            {
                P11MapRoom2D room = rooms[index];
                if (room != null
                    && room.OnMainRoute
                    && room.MainRouteOrder > highestOrder)
                {
                    highestOrder = room.MainRouteOrder;
                    finalRoom = room;
                }
            }

            return finalRoom;
        }

        private static void ValidateRange(
            int value,
            int minimum,
            int maximum,
            string code,
            string message,
            P11MapValidationReport report,
            string context)
        {
            if (value < minimum || value > maximum)
            {
                report.AddError(code, message, context);
            }
        }

        private void OnValidate()
        {
            loopCount = Mathf.Max(0, loopCount);
            directionReversalCount = Mathf.Max(
                0,
                directionReversalCount);
            maruEscapeSeconds = Mathf.Max(0f, maruEscapeSeconds);
            exitDirectionPreviewSeconds = Mathf.Clamp(
                exitDirectionPreviewSeconds,
                0f,
                10f);
            ValidateNow();
        }
    }
}

#endif
