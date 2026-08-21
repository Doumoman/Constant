#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.MapHarness.P11;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    [TestFixture]
    public sealed class P11MapHarnessTests
    {
        private readonly List<Object> created = new List<Object>();
        private Sprite cueSprite;

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    Object.DestroyImmediate(created[index]);
                }
            }

            created.Clear();
            cueSprite = null;
        }

        [Test]
        public void MacroSizeRules_AllowOnlySixApprovedFootprints()
        {
            Vector2Int[] allowed =
            {
                new Vector2Int(1, 1),
                new Vector2Int(2, 1),
                new Vector2Int(1, 2),
                new Vector2Int(2, 2),
                new Vector2Int(3, 1),
                new Vector2Int(1, 3)
            };
            Vector2Int[] rejected =
            {
                Vector2Int.zero,
                new Vector2Int(3, 2),
                new Vector2Int(2, 3),
                new Vector2Int(4, 1)
            };

            foreach (Vector2Int size in allowed)
            {
                Assert.That(
                    P11MapMacroSizeRules.IsAllowed(size),
                    Is.True,
                    $"Approved size {size} was rejected.");
            }

            foreach (Vector2Int size in rejected)
            {
                Assert.That(
                    P11MapMacroSizeRules.IsAllowed(size),
                    Is.False,
                    $"Unsupported size {size} was accepted.");
            }

            Assert.That(
                P11MapMacroSizeRules.IsX1Allowed(
                    new Vector2Int(2, 2)),
                Is.True);
            Assert.That(
                P11MapMacroSizeRules.IsX1Allowed(
                    new Vector2Int(3, 1)),
                Is.False);
            Assert.That(
                P11MapMacroSizeRules.IsX1Allowed(
                    new Vector2Int(1, 3)),
                Is.False);
        }

        [Test]
        public void Room_AcceptsFiveZonesTwoMechanicsAndSafeRoutes()
        {
            P11MapRoom2D room = CreateRoom(
                "ValidChoiceRoom",
                new Vector2Int(2, 1),
                P11MapRoomRole.Main | P11MapRoomRole.Choice,
                true,
                0,
                mechanics: 2,
                optionalSocket: true,
                optionalReturnable: true);
            P11MapValidationReport report = room.ValidateNow();

            Assert.That(report.Passed, Is.True, report.ToString());
            Assert.That(room.MacroSizeAllowed, Is.True);
            Assert.That(room.MechanicsContractSatisfied, Is.True);
            Assert.That(room.HasAllFiveZones, Is.True);
            Assert.That(room.MainRouteToolFree, Is.True);
            Assert.That(room.OptionalRouteReturnable, Is.True);
            Assert.That(room.MainSocketCount, Is.EqualTo(2));
            Assert.That(room.OptionalSocketCount, Is.EqualTo(1));
        }

        [Test]
        public void Room_RejectsMissingZoneToolGateAndNoOptionalReturn()
        {
            GameObject root = Track("InvalidRoom");
            P11MapRoom2D room = root.AddComponent<P11MapRoom2D>();
            P11MapRoomZoneDefinition[] zones =
                CreateZones(root.transform)
                    .Where(zone =>
                        zone.Kind != P11MapRoomZone.ExitSafe)
                    .ToArray();
            P11MapSocketDefinition main = CreateSocket(
                root.transform,
                "MainToolGate",
                P11MapRouteKind.Main,
                P11MapSocketTraversal.HookOptional,
                true,
                true);
            P11MapSocketDefinition optional = CreateSocket(
                root.transform,
                "OneWayOptional",
                P11MapRouteKind.Optional,
                P11MapSocketTraversal.HookOptional,
                false,
                true);
            room.Configure(
                "InvalidRoom",
                "Cross the invalid fixture.",
                new Vector2Int(3, 2),
                3,
                P11MapRoomRole.Main,
                zones,
                new[] { main, optional },
                true,
                0);

            P11MapValidationReport report = room.ValidateNow();
            Assert.That(report.Passed, Is.False);
            Assert.That(
                report.ContainsCode("ROOM_MACRO_SIZE_INVALID"),
                Is.True);
            Assert.That(
                report.ContainsCode("ROOM_MECHANICS_OVER_BUDGET"),
                Is.True);
            Assert.That(
                report.ContainsCode("ROOM_ZONE_MISSING"),
                Is.True);
            Assert.That(
                report.ContainsCode(
                    "ROOM_MAIN_SOCKET_REQUIRES_TOOL"),
                Is.True);
            Assert.That(
                report.ContainsCode(
                    "ROOM_OPTIONAL_ROUTE_NO_RETURN"),
                Is.True);
        }

        [TestCase(P11MapStageSlot.X1)]
        [TestCase(P11MapStageSlot.X2)]
        [TestCase(P11MapStageSlot.X3)]
        public void Stage_ValidFixturesPassEverySlot(
            P11MapStageSlot slot)
        {
            P11MapStageHarness2D stage = CreateValidStage(slot);
            P11MapValidationReport report = stage.ValidateNow();

            Assert.That(report.Passed, Is.True, report.ToString());
            Assert.That(stage.ValidationPassed, Is.True);
            Assert.That(stage.MainPathToolFree, Is.True);
            Assert.That(
                stage.ExitDirectionContractSatisfied,
                Is.True);
            Assert.That(stage.CueRoutesSeparated, Is.True);
            Assert.That(stage.BaseStartToExitReachable, Is.True);
            Assert.That(stage.MaruEscapeReachable, Is.True);
            Assert.That(stage.MaruEscapeSeconds, Is.InRange(25f, 45f));
        }

        [Test]
        public void MainRoutePathLength_UsesOrderedMainRouteRoomsOnly()
        {
            P11MapRoom2D first = CreateRoom(
                "Path_Main_0",
                Vector2Int.one,
                P11MapRoomRole.Start | P11MapRoomRole.Main,
                true,
                0);
            P11MapRoom2D second = CreateRoom(
                "Path_Main_1",
                Vector2Int.one,
                P11MapRoomRole.Main,
                true,
                1);
            P11MapRoom2D third = CreateRoom(
                "Path_Main_2",
                Vector2Int.one,
                P11MapRoomRole.Exit | P11MapRoomRole.Main,
                true,
                2);
            P11MapRoom2D detour = CreateRoom(
                "Path_Optional",
                Vector2Int.one,
                P11MapRoomRole.Treasure,
                false,
                0);
            first.transform.position = Vector3.zero;
            second.transform.position = new Vector3(12f, 0f, 0f);
            third.transform.position = new Vector3(12f, 16f, 0f);
            detour.transform.position = new Vector3(200f, 200f, 0f);
            var unordered = new[] { third, detour, first, second };

            Assert.That(
                P11MapStageHarness2D.MeasureMainRoutePathLength(
                    unordered),
                Is.EqualTo(28f).Within(0.001f));
            Assert.That(
                P11MapStageHarness2D.EstimateMaruEscapeSeconds(
                    unordered),
                Is.EqualTo(
                        28f
                        / P11MapStageHarness2D.DefaultMaruMoveSpeed)
                    .Within(0.001f));
        }

        [Test]
        public void MaruEscapeContract_MapsToFiftyFiveToNinetyNineUnits()
        {
            const float speed =
                P11MapStageHarness2D.DefaultMaruMoveSpeed;

            Assert.That(
                P11MapStageHarness2D.IsMaruEscapeTimeWithinContract(
                    56f / speed),
                Is.True);
            Assert.That(
                P11MapStageHarness2D.IsMaruEscapeTimeWithinContract(
                    98f / speed),
                Is.True);
            Assert.That(
                P11MapStageHarness2D.IsMaruEscapeTimeWithinContract(
                    54f / speed),
                Is.False);
            Assert.That(
                P11MapStageHarness2D.IsMaruEscapeTimeWithinContract(
                    100f / speed),
                Is.False);
        }

        [Test]
        public void Stage_InvalidFixturesReportRhythmAndNewMechanic()
        {
            P11MapStageHarness2D x1 =
                CreateValidStage(P11MapStageSlot.X1);
            P11MapRoom2D[] tooFewX1Rooms =
                x1.Rooms.Take(x1.Rooms.Count - 1).ToArray();
            Reconfigure(x1, rooms: tooFewX1Rooms);
            Assert.That(
                x1.LastValidation.ContainsCode(
                    "STAGE_X1_ROOM_COUNT"),
                Is.True,
                x1.LastValidation.ToString());

            P11MapStageHarness2D x2 =
                CreateValidStage(P11MapStageSlot.X2);
            Reconfigure(x2, loops: 1);
            Assert.That(
                x2.LastValidation.ContainsCode(
                    "STAGE_X2_LOOP_COUNT"),
                Is.True,
                x2.LastValidation.ToString());

            P11MapStageHarness2D x3 =
                CreateValidStage(P11MapStageSlot.X3);
            P11MapRoom2D changedRoom = x3.Rooms[2];
            changedRoom.Configure(
                changedRoom.RoomId,
                changedRoom.CoreSentence,
                changedRoom.MacroSize,
                changedRoom.MechanicsCount,
                changedRoom.Roles,
                changedRoom.Zones.ToArray(),
                changedRoom.Sockets.ToArray(),
                changedRoom.OnMainRoute,
                changedRoom.MainRouteOrder,
                true,
                changedRoom.ReadableDuringMaruEscape);
            Reconfigure(x3);
            Assert.That(
                x3.LastValidation.ContainsCode(
                    "STAGE_X3_NEW_MECHANIC"),
                Is.True,
                x3.LastValidation.ToString());
        }

        [Test]
        public void CueKinds_KeepMainAndOptionalDirectionsSeparated()
        {
            Assert.That(
                P11MapCueRules.ExpectedRoute(
                    P11MapCueKind.MainExit),
                Is.EqualTo(P11MapRouteKind.Main));
            Assert.That(
                P11MapCueRules.ExpectedRoute(
                    P11MapCueKind.ExitEdge),
                Is.EqualTo(P11MapRouteKind.Main));
            Assert.That(
                P11MapCueRules.ExpectedRoute(
                    P11MapCueKind.Landmark),
                Is.EqualTo(P11MapRouteKind.Main));
            Assert.That(
                P11MapCueRules.ExpectedRoute(
                    P11MapCueKind.OptionalReward),
                Is.EqualTo(P11MapRouteKind.Optional));
            Assert.That(
                P11MapCueRules.ExpectedRoute(
                    P11MapCueKind.Folklore),
                Is.EqualTo(P11MapRouteKind.Optional));

            P11MapStageHarness2D stage =
                CreateValidStage(P11MapStageSlot.X1);
            P11MapDirectionCue2D[] cues = stage.Cues.ToArray();
            cues[0] = CreateCue(
                P11MapCueKind.MainExit,
                P11MapRouteKind.Optional,
                stage.Landmark);
            Reconfigure(stage, cues: cues);

            Assert.That(stage.CueRoutesSeparated, Is.False);
            Assert.That(
                stage.LastValidation.ContainsCode(
                    "STAGE_CUE_ROUTE_MIXED"),
                Is.True,
                stage.LastValidation.ToString());
        }

        [Test]
        public void DirectionPreview_AcceptsTenSecondsAndRejectsZero()
        {
            P11MapStageHarness2D stage =
                CreateValidStage(P11MapStageSlot.X1);
            Reconfigure(stage, previewSeconds: 10f);

            Assert.That(stage.ValidationPassed, Is.True);
            Assert.That(
                stage.ExitDirectionPreviewSeconds,
                Is.EqualTo(10f));
            Assert.That(
                stage.ExitDirectionContractSatisfied,
                Is.True);

            Reconfigure(stage, previewSeconds: 0f);
            Assert.That(stage.ValidationPassed, Is.False);
            Assert.That(
                stage.ExitDirectionContractSatisfied,
                Is.False);
            Assert.That(
                stage.LastValidation.ContainsCode(
                    "STAGE_EXIT_DIRECTION_10S"),
                Is.True,
                stage.LastValidation.ToString());
        }

        private P11MapStageHarness2D CreateValidStage(
            P11MapStageSlot slot)
        {
            GameObject root = Track($"Stage_{slot}");
            P11MapStageHarness2D stage =
                root.AddComponent<P11MapStageHarness2D>();
            Transform landmark =
                Track($"{slot}_Landmark").transform;
            P11MapRoom2D[] rooms;
            int loops;
            P11MapRegion region;
            P11MapMainDirection direction;
            var cues = new List<P11MapDirectionCue2D>
            {
                CreateCue(
                    P11MapCueKind.MainExit,
                    P11MapRouteKind.Main,
                    landmark),
                CreateCue(
                    P11MapCueKind.ExitEdge,
                    P11MapRouteKind.Main,
                    landmark),
                CreateCue(
                    P11MapCueKind.Landmark,
                    P11MapRouteKind.Main,
                    landmark)
            };

            switch (slot)
            {
                case P11MapStageSlot.X1:
                    rooms = CreateX1Rooms();
                    loops = 0;
                    region = P11MapRegion.StarPostOffice;
                    direction = P11MapMainDirection.Right;
                    break;
                case P11MapStageSlot.X2:
                    rooms = CreateX2Rooms();
                    loops = 2;
                    region = P11MapRegion.SunriseGarden;
                    direction = P11MapMainDirection.Right;
                    cues.Add(
                        CreateCue(
                            P11MapCueKind.Archive,
                            P11MapRouteKind.Optional,
                            landmark));
                    cues.Add(
                        CreateCue(
                            P11MapCueKind.Homecoming,
                            P11MapRouteKind.Optional,
                            landmark));
                    cues.Add(
                        CreateCue(
                            P11MapCueKind.Folklore,
                            P11MapRouteKind.Optional,
                            landmark));
                    break;
                default:
                    rooms = CreateX3Rooms();
                    loops = 0;
                    region = P11MapRegion.PolarisObservatory;
                    direction = P11MapMainDirection.Up;
                    break;
            }

            stage.Configure(
                $"Valid_{slot}",
                region,
                slot,
                slot == P11MapStageSlot.X3
                    ? P11MapStageArchetype.FixedBoss
                    : P11MapStageArchetype.Traverse,
                direction,
                landmark,
                rooms,
                cues.ToArray(),
                loops,
                0,
                true,
                true,
                35f,
                true,
                true,
                1f);
            return stage;
        }

        private P11MapRoom2D[] CreateX1Rooms()
        {
            var rooms = new P11MapRoom2D[9];
            for (int index = 0; index < rooms.Length; index++)
            {
                bool main = index < 7;
                P11MapRoomRole role;
                if (index == 0)
                {
                    role =
                        P11MapRoomRole.Start
                        | P11MapRoomRole.Teach;
                }
                else if (index == 1)
                {
                    role =
                        P11MapRoomRole.Main
                        | P11MapRoomRole.Teach;
                }
                else if (index == 6)
                {
                    role =
                        P11MapRoomRole.Main
                        | P11MapRoomRole.Exit;
                }
                else
                {
                    role = main
                        ? P11MapRoomRole.Main
                        : P11MapRoomRole.Utility;
                }

                rooms[index] = CreateRoom(
                    $"X1_Room_{index}",
                    index % 2 == 0
                        ? Vector2Int.one
                        : new Vector2Int(2, 1),
                    role,
                    main,
                    main ? index : 0,
                    mechanics: index < 2 ? 1 : 2);
            }

            return rooms;
        }

        private P11MapRoom2D[] CreateX2Rooms()
        {
            var rooms = new P11MapRoom2D[12];
            for (int index = 0; index < rooms.Length; index++)
            {
                bool main = index < 8;
                P11MapRoomRole role;
                switch (index)
                {
                    case 0:
                        role =
                            P11MapRoomRole.Start
                            | P11MapRoomRole.Main;
                        break;
                    case 7:
                        role =
                            P11MapRoomRole.Main
                            | P11MapRoomRole.Exit;
                        break;
                    case 8:
                        role = P11MapRoomRole.Landmark;
                        break;
                    case 9:
                        role = P11MapRoomRole.MaruRoom;
                        break;
                    case 10:
                        role =
                            P11MapRoomRole.RecordRoom
                            | P11MapRoomRole.LocalEvent;
                        break;
                    case 11:
                        role = P11MapRoomRole.ShopCorridor;
                        break;
                    default:
                        role = P11MapRoomRole.Main;
                        break;
                }

                Vector2Int size = index % 3 == 0
                    ? Vector2Int.one
                    : index % 3 == 1
                        ? new Vector2Int(2, 1)
                        : new Vector2Int(2, 2);
                rooms[index] = CreateRoom(
                    $"X2_Room_{index}",
                    size,
                    role,
                    main,
                    main ? index : 0);
            }

            return rooms;
        }

        private P11MapRoom2D[] CreateX3Rooms()
        {
            var rooms = new P11MapRoom2D[8];
            for (int index = 0; index < rooms.Length; index++)
            {
                P11MapRoomRole role;
                if (index == 0)
                {
                    role =
                        P11MapRoomRole.Start
                        | P11MapRoomRole.Main;
                }
                else if (index == 5)
                {
                    role =
                        P11MapRoomRole.Main
                        | P11MapRoomRole.SupplyCorridor;
                }
                else if (index == 6)
                {
                    role =
                        P11MapRoomRole.Main
                        | P11MapRoomRole.Exit;
                }
                else if (index == 7)
                {
                    role = P11MapRoomRole.Boss;
                }
                else
                {
                    role = P11MapRoomRole.Main;
                }

                rooms[index] = CreateRoom(
                    $"X3_Room_{index}",
                    index == 7
                        ? new Vector2Int(2, 2)
                        : index % 2 == 0
                            ? Vector2Int.one
                            : new Vector2Int(2, 1),
                    role,
                    true,
                    index,
                    mechanics: 2,
                    addsNewMechanic: false);
            }

            return rooms;
        }

        private P11MapRoom2D CreateRoom(
            string id,
            Vector2Int size,
            P11MapRoomRole roles,
            bool onMain,
            int order,
            int mechanics = 1,
            bool optionalSocket = false,
            bool optionalReturnable = true,
            bool addsNewMechanic = false)
        {
            GameObject root = Track(id);
            P11MapRoom2D room = root.AddComponent<P11MapRoom2D>();
            var sockets = new List<P11MapSocketDefinition>();
            if (onMain)
            {
                sockets.Add(
                    CreateSocket(
                        root.transform,
                        $"{id}_Main_A",
                        P11MapRouteKind.Main,
                        P11MapSocketTraversal.Walk,
                        true,
                        false));
                sockets.Add(
                    CreateSocket(
                        root.transform,
                        $"{id}_Main_B",
                        P11MapRouteKind.Main,
                        P11MapSocketTraversal.JumpSafe,
                        true,
                        false));
            }

            if (optionalSocket)
            {
                sockets.Add(
                    CreateSocket(
                        root.transform,
                        $"{id}_Optional",
                        P11MapRouteKind.Optional,
                        P11MapSocketTraversal.HookOptional,
                        optionalReturnable,
                        true));
            }

            room.Configure(
                id,
                $"Perform the core action in {id}.",
                size,
                mechanics,
                roles,
                CreateZones(root.transform),
                sockets.ToArray(),
                onMain,
                order,
                addsNewMechanic,
                true);
            return room;
        }

        private P11MapRoomZoneDefinition[] CreateZones(
            Transform parent)
        {
            P11MapRoomZone[] kinds =
            {
                P11MapRoomZone.EntrySafe,
                P11MapRoomZone.Telegraph,
                P11MapRoomZone.Action,
                P11MapRoomZone.Recovery,
                P11MapRoomZone.ExitSafe
            };
            var zones =
                new P11MapRoomZoneDefinition[kinds.Length];
            for (int index = 0; index < kinds.Length; index++)
            {
                Transform anchor =
                    Track($"{parent.name}_{kinds[index]}").transform;
                anchor.SetParent(parent, false);
                int safeCells =
                    kinds[index] == P11MapRoomZone.EntrySafe
                    || kinds[index] == P11MapRoomZone.ExitSafe
                        ? 2
                        : kinds[index] == P11MapRoomZone.Recovery
                            ? 1
                            : 0;
                zones[index] = new P11MapRoomZoneDefinition();
                zones[index].Configure(
                    kinds[index],
                    anchor,
                    safeCells,
                    true);
            }

            return zones;
        }

        private P11MapSocketDefinition CreateSocket(
            Transform parent,
            string id,
            P11MapRouteKind route,
            P11MapSocketTraversal traversal,
            bool returnable,
            bool requiresTool)
        {
            Transform anchor = Track($"{id}_Anchor").transform;
            anchor.SetParent(parent, false);
            var socket = new P11MapSocketDefinition();
            socket.Configure(
                id,
                anchor,
                route,
                traversal,
                returnable,
                requiresTool);
            return socket;
        }

        private P11MapDirectionCue2D CreateCue(
            P11MapCueKind kind,
            P11MapRouteKind route,
            Transform target)
        {
            GameObject root = Track($"Cue_{kind}_{created.Count}");
            SpriteRenderer renderer =
                root.AddComponent<SpriteRenderer>();
            renderer.sprite = CueSprite;
            P11MapDirectionCue2D cue =
                root.AddComponent<P11MapDirectionCue2D>();
            cue.Configure(
                kind,
                route,
                target,
                renderer,
                true,
                true,
                true);
            return cue;
        }

        private void Reconfigure(
            P11MapStageHarness2D stage,
            P11MapRoom2D[] rooms = null,
            P11MapDirectionCue2D[] cues = null,
            int? loops = null,
            float? previewSeconds = null)
        {
            stage.Configure(
                stage.StageId,
                stage.Region,
                stage.StageSlot,
                stage.Archetype,
                stage.MainDirection,
                stage.Landmark,
                rooms ?? stage.Rooms.ToArray(),
                cues ?? stage.Cues.ToArray(),
                loops ?? stage.LoopCount,
                stage.DirectionReversalCount,
                stage.BaseStartToExitReachable,
                stage.MaruEscapeReachable,
                stage.MaruEscapeSeconds,
                stage.PreviousActionPayoffVisible,
                stage.EntryDirectionPreviewEnabled,
                previewSeconds
                    ?? stage.ExitDirectionPreviewSeconds);
        }

        private Sprite CueSprite
        {
            get
            {
                if (cueSprite != null)
                {
                    return cueSprite;
                }

                var texture = new Texture2D(2, 2);
                texture.SetPixels(
                    new[]
                    {
                        Color.white,
                        Color.white,
                        Color.white,
                        Color.white
                    });
                texture.Apply();
                cueSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 2f, 2f),
                    new Vector2(0.5f, 0.5f),
                    2f);
                created.Add(texture);
                created.Add(cueSprite);
                return cueSprite;
            }
        }

        private GameObject Track(string name)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }
    }
}

#endif
