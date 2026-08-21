#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Core.Tools;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Map;
using StarNight.Stage.Rooms;
using StarNight.Stage.Secrets;
using StarNight.Tools.Compass;
using StarNight.Tools.Core;
using StarNight.Tools.Inventory;
using UnityEngine;

namespace StarNight.Stage.Tests
{
    public sealed class CompassCoreContractTests
    {
        private readonly List<Object> created = new List<Object>();

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
        }

        [Test]
        public void PassiveBandsAndSelectedFocusStayInsideCurrentRoomAndConsumeOnlyOnSuccess()
        {
            RoomRuntime roomA = CreateRoom("Room_A", Vector2.zero);
            RoomRuntime roomB = CreateRoom("Room_B", new Vector2(20f, 0f));
            SecretAnchor sameRoomGate = CreateAnchor(
                "A_GATE",
                roomA,
                new Vector2(6f, 0f),
                SecretGateType.DirtSeal);
            CreateAnchor(
                "B_GATE",
                roomB,
                new Vector2(1f, 0f),
                SecretGateType.CrackedWall);

            GameObject player = Track(new GameObject("CompassPlayer"));
            HandSlotPresenter presenter = player.AddComponent<HandSlotPresenter>();
            presenter.ConfigureForTests(player.transform);
            PlayerHandSlot handSlot = player.AddComponent<PlayerHandSlot>();
            handSlot.ConfigureForTests(presenter);
            EquipmentInventory inventory = player.AddComponent<EquipmentInventory>();
            inventory.ConfigureForTests(handSlot);
            SecretDetectorController detector = player.AddComponent<SecretDetectorController>();

            HandToolDefinition definition = Track(ScriptableObject.CreateInstance<HandToolDefinition>());
            definition.Configure(
                "ITEM_MOON_EYE_COMPASS",
                "달눈 나침반",
                ToolTag.None,
                ToolResourceMode.Durability,
                8,
                500,
                new ToolActionProfile(),
                new ToolActionProfile(),
                System.Array.Empty<Vector2Int>());
            GameObject compassObject = Track(new GameObject("CompassRuntime"));
            MoonEyeCompassRuntime compass = compassObject.AddComponent<MoonEyeCompassRuntime>();
            compass.Configure(definition);
            Assert.That(inventory.ResolvePickup(compass), Is.EqualTo(EquipmentPickupResult.Added));
            Assert.That(definition.TabSelectable, Is.True);
            Assert.That(CompassEquipmentContract.PassiveDetectionRangeCells, Is.EqualTo(6f));
            Assert.That(CompassEquipmentContract.FocusDetectionRangeCells, Is.EqualTo(8f));
            Assert.That(CompassEquipmentContract.FocusDurationSeconds, Is.EqualTo(3f));
            Assert.That(inventory.SelectedRuntime, Is.SameAs(compass));
            detector.Configure(inventory, roomA);

            detector.RefreshDetection();
            Assert.That(detector.Band, Is.EqualTo(SecretDetectionBand.Distant));
            Assert.That(detector.SlowBlinkActive, Is.True);
            Assert.That(detector.DistanceCells, Is.EqualTo(6f).Within(0.001f));

            player.transform.position = new Vector2(2f, 0f);
            detector.RefreshDetection();
            Assert.That(detector.Band, Is.EqualTo(SecretDetectionBand.Near));
            Assert.That(detector.FastBlinkActive, Is.True);

            player.transform.position = new Vector2(4f, 0f);
            detector.RefreshDetection();
            Assert.That(detector.Band, Is.EqualTo(SecretDetectionBand.Close));
            Assert.That(detector.NeedleVisible, Is.True);
            Assert.That(detector.Direction, Is.EqualTo(Vector2.right));

            player.transform.position = new Vector2(5f, 0f);
            detector.RefreshDetection();
            Assert.That(detector.Band, Is.EqualTo(SecretDetectionBand.Immediate));
            Assert.That(compass.TryPrimaryUse(
                handSlot,
                new PlayerActionContext(1, 0f, 0f, false),
                1,
                default), Is.True);
            Assert.That(compass.CurrentResource, Is.EqualTo(7));
            Assert.That(detector.FocusActive, Is.True);
            Assert.That(detector.FocusedAnchor, Is.SameAs(sameRoomGate));
            Assert.That(detector.FocusedToolFamily, Is.EqualTo(SecretGateToolFamily.Shovel));
            Assert.That(sameRoomGate.IsCompassFocused, Is.True);
            Assert.That(sameRoomGate.IsRevealed, Is.False, "Compass must never open the gate directly.");

            detector.ExpireFocusForTests();
            Assert.That(sameRoomGate.IsCompassFocused, Is.False);
            player.transform.position = new Vector2(15f, 0f);
            detector.RefreshDetection();
            Assert.That(detector.Band, Is.EqualTo(SecretDetectionBand.None));
            Assert.That(compass.TryPrimaryUse(
                handSlot,
                new PlayerActionContext(2, 0f, 0f, false),
                1,
                default), Is.False);
            Assert.That(compass.CurrentResource, Is.EqualTo(7));
        }

        private RoomRuntime CreateRoom(string id, Vector2 origin)
        {
            GameObject roomObject = Track(new GameObject(id));
            roomObject.transform.position = origin;
            RoomRuntime room = roomObject.AddComponent<RoomRuntime>();
            room.Configure(
                id,
                new Vector2Int(20, 10),
                RoomCameraMode.Fixed,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
            return room;
        }

        private SecretAnchor CreateAnchor(
            string id,
            RoomRuntime room,
            Vector2 position,
            SecretGateType gateType)
        {
            GameObject anchorObject = Track(new GameObject(id));
            anchorObject.transform.position = position;
            SecretAnchor anchor = anchorObject.AddComponent<SecretAnchor>();
            anchor.Configure(id, 1, id + "_SECRET", room, null, null, gateType);
            return anchor;
        }

        private T Track<T>(T value) where T : Object
        {
            created.Add(value);
            return value;
        }
    }
}

#endif
