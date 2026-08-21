#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;

namespace StarNight.Map.Tests
{
    public sealed class MapE01OccupancyTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        private static IEnumerable RegistrationFootprints
        {
            get
            {
                yield return new TestCaseData(
                    new Vector2Int(1, 1),
                    new[] { new Vector2Int(0, 0) })
                    .SetName("RegisterAndUnregister_1x1");

                yield return new TestCaseData(
                    new Vector2Int(2, 1),
                    new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) })
                    .SetName("RegisterAndUnregister_2x1");

                yield return new TestCaseData(
                    new Vector2Int(2, 2),
                    new[]
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                    })
                    .SetName("RegisterAndUnregister_LShape");
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [TestCaseSource(nameof(RegistrationFootprints))]
        public void FootprintRegistersAndUnregistersAtomically(
            Vector2Int bounds,
            Vector2Int[] occupiedCells)
        {
            var registry = CreateComponent<RoomElementRegistry>("Registry");
            var occupier = CreateOccupier(
                "Occupier",
                new Vector2Int(10, 5),
                CreateFootprint(bounds, occupiedCells),
                OccupancyLayer.Terrain);

            Assert.That(registry.TryRegister(occupier, out var conflict), Is.True, conflict.Reason);
            Assert.That(registry.RegisteredOccupierCount, Is.EqualTo(1));
            Assert.That(registry.OccupiedCellCount, Is.EqualTo(occupiedCells.Length));

            var expectedCells = occupiedCells
                .Select(cell => new GridCell(10 + cell.x, 5 + cell.y));
            foreach (var cell in expectedCells)
            {
                Assert.That(registry.GetLayers(cell), Is.EqualTo(OccupancyLayer.Terrain));
            }

            Assert.That(registry.Unregister(occupier), Is.True);
            Assert.That(registry.RegisteredOccupierCount, Is.Zero);
            Assert.That(registry.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void DuplicateOccupiedCellIsRejectedBeforeRegistration()
        {
            var footprint = CreateFootprint(
                new Vector2Int(1, 1),
                new[] { Vector2Int.zero, Vector2Int.zero });
            var registry = CreateComponent<RoomElementRegistry>("Registry");
            var occupier = CreateOccupier(
                "InvalidOccupier",
                Vector2Int.zero,
                footprint,
                OccupancyLayer.Terrain);

            Assert.That(registry.CanRegister(occupier, out var conflict), Is.False);
            Assert.That(conflict.Reason, Does.Contain("duplicate"));
            Assert.That(registry.RegisteredOccupierCount, Is.Zero);
            Assert.That(registry.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void OverlapFailureDoesNotPartiallyRegisterIncomingFootprint()
        {
            var registry = CreateComponent<RoomElementRegistry>("Registry");
            var existing = CreateOccupier(
                "ExistingTerrain",
                new Vector2Int(5, 3),
                CreateFootprint(Vector2Int.one, new[] { Vector2Int.zero }),
                OccupancyLayer.Terrain);
            var incoming = CreateOccupier(
                "IncomingDynamic",
                new Vector2Int(5, 3),
                CreateFootprint(
                    new Vector2Int(2, 1),
                    new[] { Vector2Int.zero, Vector2Int.right }),
                OccupancyLayer.Dynamic);

            Assert.That(registry.TryRegister(existing, out var firstConflict), Is.True, firstConflict.Reason);
            Assert.That(registry.CanRegister(incoming, out var previewConflict), Is.False);
            Assert.That(previewConflict.Cell, Is.EqualTo(new GridCell(5, 3)));
            Assert.That(registry.TryRegister(incoming, out _), Is.False);

            Assert.That(registry.RegisteredOccupierCount, Is.EqualTo(1));
            Assert.That(registry.GetLayers(new GridCell(5, 3)), Is.EqualTo(OccupancyLayer.Terrain));
            Assert.That(registry.GetLayers(new GridCell(6, 3)), Is.EqualTo(OccupancyLayer.None));
        }

        [Test]
        public void DocumentedOverlayLayersRemainCompatible()
        {
            Assert.That(
                OccupancyRules.CanOverlap(OccupancyLayer.Terrain, OccupancyLayer.Fixture),
                Is.True);
            Assert.That(
                OccupancyRules.CanOverlap(OccupancyLayer.Terrain, OccupancyLayer.Hazard),
                Is.True);
            Assert.That(
                OccupancyRules.CanOverlap(OccupancyLayer.Dynamic, OccupancyLayer.Terrain),
                Is.False);
            Assert.That(
                OccupancyRules.CanOverlap(OccupancyLayer.Logic, OccupancyLayer.Dynamic),
                Is.True);
            Assert.That(
                OccupancyRules.CanOverlap(OccupancyLayer.Decoration, OccupancyLayer.OneWay),
                Is.True);
        }

        private T CreateComponent<T>(string objectName)
            where T : Component
        {
            var gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private GridOccupier CreateOccupier(
            string objectName,
            Vector2Int anchorCell,
            CellFootprint footprint,
            OccupancyLayer layer)
        {
            var occupier = CreateComponent<GridOccupier>(objectName);
            occupier.Configure(anchorCell, footprint, layer);
            return occupier;
        }

        private static CellFootprint CreateFootprint(
            Vector2Int bounds,
            IEnumerable<Vector2Int> occupiedCells)
        {
            return new CellFootprint
            {
                BoundsSize = bounds,
                PivotCell = Vector2Int.zero,
                OccupiedCells = occupiedCells.ToList(),
            };
        }
    }
}

#endif
