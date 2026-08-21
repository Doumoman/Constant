#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.World;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class RoomFocusCameraTests
    {
        private const float WideAspect = 16f / 9f;

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
        public void OrthographicSize_FramesOneMacroRoomWithoutOverZoom()
        {
            float size = RoomFocusCamera2D.CalculateOrthographicSize(
                new Rect(0f, 0f, 12f, 8f),
                WideAspect,
                RoomFocusCamera2D.DefaultMargin);

            Assert.That(size, Is.GreaterThanOrEqualTo(4f));
            Assert.That(size, Is.LessThanOrEqualTo(5f));
            Assert.That(
                size,
                Is.GreaterThanOrEqualTo(4f + RoomFocusCamera2D.DefaultMargin - 0.001f),
                "Room height must fit inside the framed view.");
            Assert.That(
                size * WideAspect,
                Is.GreaterThanOrEqualTo(6f + RoomFocusCamera2D.DefaultMargin - 0.001f),
                "Room width must fit inside the framed view.");
        }

        [Test]
        public void FramedPosition_FollowsTargetButNeverLeavesLargeRoom()
        {
            Rect room = new Rect(0f, 0f, 36f, 16f);
            const float size = 5f;
            float halfWidth = size * WideAspect;

            Vector2 nearCorner = RoomFocusCamera2D.CalculateFramedPosition(
                room,
                new Vector2(2f, 1f),
                size,
                WideAspect);
            Assert.That(nearCorner.x, Is.EqualTo(room.xMin + halfWidth).Within(0.001f));
            Assert.That(nearCorner.y, Is.EqualTo(room.yMin + size).Within(0.001f));
            Assert.That(nearCorner.x - halfWidth, Is.GreaterThanOrEqualTo(room.xMin - 0.001f));
            Assert.That(nearCorner.y - size, Is.GreaterThanOrEqualTo(room.yMin - 0.001f));

            Vector2 middle = RoomFocusCamera2D.CalculateFramedPosition(
                room,
                new Vector2(18f, 8f),
                size,
                WideAspect);
            Assert.That(middle.x, Is.EqualTo(18f).Within(0.001f));
            Assert.That(middle.y, Is.EqualTo(8f).Within(0.001f));

            Vector2 farCorner = RoomFocusCamera2D.CalculateFramedPosition(
                room,
                new Vector2(35f, 15f),
                size,
                WideAspect);
            Assert.That(farCorner.x + halfWidth, Is.LessThanOrEqualTo(room.xMax + 0.001f));
            Assert.That(farCorner.y + size, Is.LessThanOrEqualTo(room.yMax + 0.001f));
        }

        [Test]
        public void FramedPosition_LocksToRoomCenterWhenRoomFitsOnScreen()
        {
            Rect room = new Rect(4f, 2f, 12f, 8f);
            float size = RoomFocusCamera2D.CalculateOrthographicSize(
                room,
                WideAspect,
                RoomFocusCamera2D.DefaultMargin);

            Vector2 framed = RoomFocusCamera2D.CalculateFramedPosition(
                room,
                new Vector2(5f, 3f),
                size,
                WideAspect);

            Assert.That(framed.x, Is.EqualTo(room.center.x).Within(0.001f));
            Assert.That(framed.y, Is.EqualTo(room.center.y).Within(0.001f));
        }

        [Test]
        public void FindContaining_PicksOverlappingRoomWithNearestCenter()
        {
            RoomBounds2D left = CreateRoom("left", new Rect(0f, 0f, 12f, 8f));
            RoomBounds2D right = CreateRoom("right", new Rect(10f, 0f, 12f, 8f));

            Assert.That(
                RoomBounds2D.FindContaining(new Vector2(10.5f, 4f)),
                Is.SameAs(left));
            Assert.That(
                RoomBounds2D.FindContaining(new Vector2(11.5f, 4f)),
                Is.SameAs(right));
            Assert.That(
                RoomBounds2D.FindContaining(new Vector2(40f, 4f)),
                Is.Null);
        }

        [Test]
        public void ActiveRooms_DropsDisabledRoom()
        {
            RoomBounds2D kept = CreateRoom("kept", new Rect(0f, 0f, 12f, 8f));
            RoomBounds2D hidden = CreateRoom("hidden", new Rect(20f, 0f, 12f, 8f));

            Assert.That(RoomBounds2D.ActiveRooms, Contains.Item(hidden));

            hidden.gameObject.SetActive(false);

            Assert.That(RoomBounds2D.ActiveRooms, Has.No.Member(hidden));
            Assert.That(RoomBounds2D.ActiveRooms, Contains.Item(kept));
            Assert.That(
                RoomBounds2D.FindContaining(new Vector2(26f, 4f)),
                Is.Null);
        }

        private RoomBounds2D CreateRoom(string roomId, Rect worldRect)
        {
            var host = new GameObject(roomId);
            created.Add(host);
            RoomBounds2D room = host.AddComponent<RoomBounds2D>();
            room.Configure(roomId, worldRect);
            return room;
        }
    }
}

#endif
