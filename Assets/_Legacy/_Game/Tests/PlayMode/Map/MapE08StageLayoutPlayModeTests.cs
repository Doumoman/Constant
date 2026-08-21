#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Stage.Layout;
using StarNight.Stage.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE08StageLayoutPlayModeTests
    {
        [UnityTest]
        public IEnumerator VariableRoomGridAndSocketCompatibilityRemainDeterministic()
        {
            Assert.That(RoomSizeCatalog.Micro, Is.EqualTo(new Vector2Int(12, 8)));
            Assert.That(RoomSizeCatalog.Wide, Is.EqualTo(new Vector2Int(24, 8)));
            Assert.That(RoomSizeCatalog.Tall, Is.EqualTo(new Vector2Int(12, 16)));
            Assert.That(RoomSizeCatalog.Large, Is.EqualTo(new Vector2Int(24, 16)));
            Assert.That(StageLayoutGraphUtility.SnapToPlacementGrid(new Vector2Int(5, -3)), Is.EqualTo(new Vector2Int(4, -4)));

            var right = new RoomSocketDefinition
            {
                SocketGuid = "Right",
                Side = CardinalDirection.Right,
                LocalCell = new Vector2Int(12, 2),
                OpeningSizeCells = Vector2Int.one,
                Traversal = TraversalType.Walk,
                FloorHeightCell = 2,
            };
            var left = new RoomSocketDefinition
            {
                SocketGuid = "Left",
                Side = CardinalDirection.Left,
                LocalCell = new Vector2Int(0, 2),
                OpeningSizeCells = Vector2Int.one,
                Traversal = TraversalType.Walk,
                FloorHeightCell = 2,
            };

            Assert.That(StageLayoutGraphUtility.IsSocketOnBoundary(right, RoomSizeCatalog.Micro), Is.True);
            Assert.That(StageLayoutGraphUtility.GetCompatibility(right, left), Is.EqualTo(SocketCompatibility.Compatible));
            left.FloorHeightCell = 3;
            Assert.That(StageLayoutGraphUtility.GetCompatibility(right, left), Is.EqualTo(SocketCompatibility.FloorHeightMismatch));
            Assert.That(StageLayoutGraphUtility.RoomsOverlap(Vector2Int.zero, RoomSizeCatalog.Micro, new Vector2Int(12, 0), RoomSizeCatalog.Wide), Is.False);
            yield return null;
        }
    }
}

#endif
