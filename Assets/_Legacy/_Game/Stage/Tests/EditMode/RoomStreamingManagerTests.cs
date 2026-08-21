#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Stage.Lab;
using StarNight.Stage.Rooms;
using StarNight.Stage.Streaming;
using UnityEngine;

namespace StarNight.Stage.Tests
{
    public sealed class RoomStreamingManagerTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void BeginBuildsOnlyCurrentAndOneHopAndRevisitKeepsInstance()
        {
            root = new GameObject("StreamingManagerTest");
            RoomStreamingManager manager = root.AddComponent<RoomStreamingManager>();
            var buildCounts = new Dictionary<string, int>();
            var plans = new List<RoomStreamPlan>
            {
                Plan("Room_A", 101, new[] { "Room_B" }, 0, buildCounts),
                Plan("Room_B", 102, new[] { "Room_A", "Room_C" }, 1, buildCounts),
                Plan("Room_C", 103, new[] { "Room_B", "Room_D" }, 2, buildCounts),
                Plan("Room_D", 104, new[] { "Room_C" }, 3, buildCounts),
            };
            manager.ConfigurePlans(plans);

            Assert.That(manager.Begin("Room_A"), Is.True);
            Assert.That(manager.InstantiatedCount, Is.EqualTo(2));
            Assert.That(manager.GetState("Room_A"), Is.EqualTo(RoomInstanceState.Active));
            Assert.That(manager.GetState("Room_B"), Is.EqualTo(RoomInstanceState.WarmLoaded));
            Assert.That(manager.GetState("Room_C"), Is.EqualTo(RoomInstanceState.Uninstantiated));
            Assert.That(manager.GetState("Room_D"), Is.EqualTo(RoomInstanceState.Uninstantiated));
            Assert.That(manager.TryGetRuntime("Room_A", out RoomRuntime firstA), Is.True);
            int firstInstanceId = firstA.GetInstanceID();

            Assert.That(manager.Activate("Room_B"), Is.True);
            Assert.That(manager.InstantiatedCount, Is.EqualTo(3));
            Assert.That(manager.GetState("Room_A"), Is.EqualTo(RoomInstanceState.FrozenVisited));
            Assert.That(manager.GetState("Room_C"), Is.EqualTo(RoomInstanceState.WarmLoaded));
            Assert.That(manager.GetState("Room_D"), Is.EqualTo(RoomInstanceState.Uninstantiated));
            Assert.That(manager.Activate("Room_A"), Is.True);
            Assert.That(manager.TryGetRuntime("Room_A", out RoomRuntime revisitedA), Is.True);
            Assert.That(revisitedA.GetInstanceID(), Is.EqualTo(firstInstanceId));
            Assert.That(buildCounts["Room_A"], Is.EqualTo(1));
        }

        private RoomStreamPlan Plan(
            string roomId,
            int seed,
            string[] neighbors,
            int index,
            IDictionary<string, int> buildCounts)
        {
            return new RoomStreamPlan(roomId, seed, neighbors, () =>
            {
                buildCounts[roomId] = buildCounts.TryGetValue(roomId, out int count) ? count + 1 : 1;
                bool left = index > 0;
                bool right = index < 3;
                return Core04TwoRoomLab.BuildPrototypeRoom(
                    root.transform,
                    roomId,
                    new Vector2(index * Core04TwoRoomLab.RoomWidth, 0f),
                    Color.black,
                    left,
                    right,
                    out _);
            });
        }
    }
}

#endif
