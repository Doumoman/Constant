#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Interaction.Carry;
using UnityEngine;

namespace StarNight.Interaction.Tests
{
    public sealed class CarryPlacementResolverTests
    {
        private readonly CarryPlacementResolver resolver = new CarryPlacementResolver();

        [Test]
        public void FrontCellSucceedsFirst()
        {
            var world = new PlacementWorldProbe();

            Assert.That(Resolve(world, Vector2Int.one, out CarryPlacementResult result), Is.True);
            Assert.That(result.BaseCell, Is.EqualTo(new Vector2Int(1, 0)));
        }

        [Test]
        public void BlockedFrontFallsBackToCurrentThenBehind()
        {
            var world = new PlacementWorldProbe();
            world.Blocked.Add(new Vector2Int(1, 0));

            Assert.That(Resolve(world, Vector2Int.one, out CarryPlacementResult current), Is.True);
            Assert.That(current.BaseCell, Is.EqualTo(Vector2Int.zero));

            world.Blocked.Add(Vector2Int.zero);
            Assert.That(Resolve(world, Vector2Int.one, out CarryPlacementResult behind), Is.True);
            Assert.That(behind.BaseCell, Is.EqualTo(new Vector2Int(-1, 0)));
        }

        [Test]
        public void PortalGapRejectsEveryCandidate()
        {
            var world = new PlacementWorldProbe { PortalEverywhere = true };

            Assert.That(Resolve(world, Vector2Int.one, out CarryPlacementResult result), Is.False);
            Assert.That(result.Failure, Is.EqualTo(CarryPlacementFailure.PortalGap));
        }

        [Test]
        public void VoidRejectsEveryCandidate()
        {
            var world = new PlacementWorldProbe { VoidEverywhere = true };

            Assert.That(Resolve(world, Vector2Int.one, out CarryPlacementResult result), Is.False);
            Assert.That(result.Failure, Is.EqualTo(CarryPlacementFailure.Void));
        }

        [Test]
        public void HeavyOneByTwoChecksFullClearanceBeforeFallback()
        {
            var world = new PlacementWorldProbe
            {
                ClearRule = footprint => footprint.height == 2 && footprint.position == Vector2Int.zero
            };

            Assert.That(Resolve(world, new Vector2Int(1, 2), out CarryPlacementResult result), Is.True);
            Assert.That(result.BaseCell, Is.EqualTo(Vector2Int.zero));
            Assert.That(world.TestedFootprints, Has.Some.Matches<RectInt>(rect => rect.height == 2));
        }

        private bool Resolve(
            PlacementWorldProbe world,
            Vector2Int footprint,
            out CarryPlacementResult result)
        {
            return resolver.TryResolve(
                new CarryPlacementRequest(Vector2Int.zero, 1, footprint, Vector2.zero),
                world,
                out result);
        }

        private sealed class PlacementWorldProbe : ICarryPlacementWorld
        {
            public readonly HashSet<Vector2Int> Blocked = new HashSet<Vector2Int>();
            public readonly List<RectInt> TestedFootprints = new List<RectInt>();
            public bool PortalEverywhere;
            public bool VoidEverywhere;
            public Func<RectInt, bool> ClearRule;

            public bool IsInsideRoom(RectInt footprint) => true;
            public bool IsFootprintClear(RectInt footprint)
            {
                TestedFootprints.Add(footprint);
                return ClearRule != null ? ClearRule(footprint) : !Blocked.Contains(footprint.position);
            }
            public bool HasStableSupport(RectInt footprint) => true;
            public bool IsPortalGap(RectInt footprint) => PortalEverywhere;
            public bool IsVoid(RectInt footprint) => VoidEverywhere;
        }
    }
}

#endif
