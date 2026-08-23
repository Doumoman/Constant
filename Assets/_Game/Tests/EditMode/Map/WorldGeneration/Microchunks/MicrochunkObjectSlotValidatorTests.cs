using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Map.Tests.WorldGeneration.Microchunks
{
    [Category("MAP07_05")]
    public sealed class MicrochunkObjectSlotValidatorTests
    {
        private static readonly MicrochunkSlotCategory[] AllCategories =
            (MicrochunkSlotCategory[])Enum.GetValues(typeof(MicrochunkSlotCategory));

        private static readonly MicrochunkObjectOrientation[] AllOrientations =
            (MicrochunkObjectOrientation[])Enum.GetValues(typeof(MicrochunkObjectOrientation));

        public static IEnumerable<TestCaseData> EveryAnchorCase
        {
            get
            {
                for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                {
                    yield return new TestCaseData(x, y);
                }
            }
        }

        public static IEnumerable<TestCaseData> EveryRadiusNeighborCase
        {
            get
            {
                for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                {
                    foreach (var offset in new[]
                             {
                                 new[] { -1, 0 },
                                 new[] { 1, 0 },
                                 new[] { 0, -1 },
                                 new[] { 0, 1 }
                             })
                    {
                        var neighborX = x + offset[0];
                        var neighborY = y + offset[1];
                        if (MicrochunkLocalCoord.TryCreate(neighborX, neighborY, out _))
                        {
                            yield return new TestCaseData(x, y, neighborX, neighborY);
                        }
                    }
                }
            }
        }

        public static IEnumerable<TestCaseData> CategoryCases =>
            AllCategories.Select(value => new TestCaseData(value));

        public static IEnumerable<TestCaseData> OrientationCases =>
            AllOrientations.Select(value => new TestCaseData(value));

        [TestCaseSource(nameof(EveryAnchorCase))]
        public void EveryTwelveByEightAnchorCoordinateIsAccepted(int x, int y)
        {
            var anchor = new MicrochunkLocalCoord(x, y);
            var result = Validate(
                Definition(AllCells(coordinate => Cell(coordinate, null,
                    coordinate == anchor ? "M_SAFE" : "NONE")),
                    new[] { Slot("SLOT", anchor) }, true),
                Policy());

            Assert.That(result.Success, Is.True);
        }

        [TestCaseSource(nameof(EveryRadiusNeighborCase))]
        public void EveryClippedManhattanRadiusNeighborDetectsBlockingTile(
            int anchorX,
            int anchorY,
            int neighborX,
            int neighborY)
        {
            var anchor = new MicrochunkLocalCoord(anchorX, anchorY);
            var neighbor = new MicrochunkLocalCoord(neighborX, neighborY);
            var cells = AllCells(coordinate => Cell(
                coordinate,
                coordinate == neighbor ? MicrochunkTileLayer.GroundSolid : (MicrochunkTileLayer?)null,
                coordinate == anchor ? "M_SAFE" : "NONE"));

            var result = Validate(
                Definition(cells, new[] { Slot("SLOT", anchor, radius: 1) }, true),
                Policy());

            Assert.That(result.Violations, Has.Count.EqualTo(1));
            Assert.That(result.Violations[0].Reason,
                Is.EqualTo(MicrochunkObjectSlotValidator.BlockingTileCellInSlotSafetyRadiusReason));
            Assert.That(result.Violations[0].Coordinate, Is.EqualTo(neighbor));
        }

        [TestCaseSource(nameof(CategoryCases))]
        public void EveryContractCategoryIsAccepted(MicrochunkSlotCategory category)
        {
            var definition = Definition(
                AllCells(coordinate => Cell(coordinate, null,
                    coordinate == new MicrochunkLocalCoord(0, 0) ? "M_SAFE" : "NONE")),
                new[] { Slot("SLOT", new MicrochunkLocalCoord(0, 0), category) },
                true);

            Assert.That(Validate(definition, Policy()).Success, Is.True);
        }

        [TestCaseSource(nameof(OrientationCases))]
        public void EveryContractOrientationIsAccepted(MicrochunkObjectOrientation orientation)
        {
            var definition = Definition(
                AllCells(coordinate => Cell(coordinate, null,
                    coordinate == new MicrochunkLocalCoord(0, 0) ? "M_SAFE" : "NONE")),
                new[] { Slot("SLOT", new MicrochunkLocalCoord(0, 0), orientation: orientation) },
                true);

            Assert.That(Validate(definition, Policy()).Success, Is.True);
        }

        [Test]
        public void PoolAndPolicyCaptureImmutableCanonicalSnapshots()
        {
            var categories = new List<MicrochunkSlotCategory>
            {
                MicrochunkSlotCategory.Reward,
                MicrochunkSlotCategory.Resource
            };
            var pool = new MicrochunkObjectSlotPoolDefinition(
                "POOL", categories, true, false, "notes");
            var pools = new List<MicrochunkObjectSlotPoolDefinition> { pool };
            var markers = new List<string> { "M_Z", "M_A" };
            var policy = new MicrochunkObjectSlotValidationPolicy(pools, markers);
            categories.Clear();
            pools.Clear();
            markers.Clear();

            Assert.That(pool.AllowedCategories,
                Is.EqualTo(new[] { MicrochunkSlotCategory.Resource, MicrochunkSlotCategory.Reward }));
            Assert.That(pool.RequiredSlotsAllowed, Is.True);
            Assert.That(pool.OptionalSlotsAllowed, Is.False);
            Assert.That(pool.Notes, Is.EqualTo("notes"));
            Assert.That(policy.PoolDefinitions.Select(value => value.PoolId), Is.EqualTo(new[] { "POOL" }));
            Assert.That(policy.AllowedMarkerCodes, Is.EqualTo(new[] { "M_A", "M_Z" }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)policy.AllowedMarkerCodes).Add("M_NEW"));
        }

        [Test]
        public void PoolAndPolicyRejectInvalidSourceRows()
        {
            Assert.Throws<ArgumentException>(() => new MicrochunkObjectSlotPoolDefinition(
                string.Empty, AllCategories, true, true, string.Empty));
            Assert.Throws<ArgumentException>(() => new MicrochunkObjectSlotPoolDefinition(
                "POOL", Array.Empty<MicrochunkSlotCategory>(), true, true, string.Empty));
            Assert.Throws<ArgumentException>(() => new MicrochunkObjectSlotPoolDefinition(
                "POOL", new[] { MicrochunkSlotCategory.Resource, MicrochunkSlotCategory.Resource },
                true, true, string.Empty));
            Assert.Throws<ArgumentException>(() => new MicrochunkObjectSlotValidationPolicy(
                new[] { Pool("POOL"), Pool("POOL") }, new[] { "M_SAFE" }));
            Assert.Throws<ArgumentException>(() => new MicrochunkObjectSlotValidationPolicy(
                new[] { Pool("POOL") }, new[] { "M_SAFE", "M_SAFE" }));
        }

        [Test]
        public void ExistingSlotModelEnforcesNonEmptyIdsAndNonNegativeRadius()
        {
            Assert.Throws<ArgumentException>(() => Slot(string.Empty, new MicrochunkLocalCoord(0, 0)));
            Assert.Throws<ArgumentException>(() => Slot(
                "SLOT", new MicrochunkLocalCoord(0, 0), poolId: string.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => Slot(
                "SLOT", new MicrochunkLocalCoord(0, 0), radius: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MicrochunkLocalCoord(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MicrochunkLocalCoord(12, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MicrochunkLocalCoord(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MicrochunkLocalCoord(0, 8));
        }

        [Test]
        public void MissingPoolIsReported()
        {
            var result = Validate(
                PartialDefinition(new[] { Slot("SLOT", new MicrochunkLocalCoord(0, 0), poolId: "UNKNOWN") }),
                Policy());

            Assert.That(result.Violations.Select(value => value.Reason),
                Does.Contain(MicrochunkObjectSlotValidator.AllowedPoolIdNotFoundReason));
        }

        [Test]
        public void PoolCategoryAndRequiredOptionalAllowancesAreValidated()
        {
            var policy = new MicrochunkObjectSlotValidationPolicy(
                new[]
                {
                    new MicrochunkObjectSlotPoolDefinition(
                        "POOL", new[] { MicrochunkSlotCategory.Reward }, false, false, string.Empty)
                },
                new[] { "M_SAFE" });
            var slots = new[]
            {
                Slot("REQUIRED", new MicrochunkLocalCoord(0, 0), required: true),
                Slot("OPTIONAL", new MicrochunkLocalCoord(1, 0), required: false)
            };
            var result = Validate(PartialDefinition(slots), policy);

            Assert.That(result.Violations.Count(value =>
                value.Reason == MicrochunkObjectSlotValidator.SlotCategoryNotAllowedByPoolReason), Is.EqualTo(2));
            Assert.That(result.Violations.Any(value =>
                value.Reason == MicrochunkObjectSlotValidator.PoolDisallowsRequiredSlotReason), Is.True);
            Assert.That(result.Violations.Any(value =>
                value.Reason == MicrochunkObjectSlotValidator.PoolDisallowsOptionalSlotReason), Is.True);
        }

        [Test]
        public void MissingAnchorInPartialDataUsesExactReasonWithoutCompletenessSweep()
        {
            var result = Validate(
                Definition(Array.Empty<MicrochunkTileCell>(),
                    new[] { Slot("SLOT", new MicrochunkLocalCoord(5, 4), radius: 3) }, false),
                Policy());

            Assert.That(result.Violations.Select(value => value.Reason),
                Is.EqualTo(new[] { MicrochunkObjectSlotValidator.MissingTileCellForSlotAnchorReason }));
        }

        [Test]
        public void CompleteFlagReportsOnlyMissingCellsWithinClippedSafetyRadius()
        {
            var anchor = new MicrochunkLocalCoord(0, 0);
            var missing = new MicrochunkLocalCoord(1, 0);
            var cells = AllCells(coordinate => Cell(coordinate, null,
                    coordinate == anchor ? "M_SAFE" : "NONE"))
                .Where(cell => cell.Coordinate != missing);

            var result = MicrochunkObjectSlotValidator.ValidateSlots(
                new MicrochunkId("MC_COMPLETE_DIRECT"), true, cells,
                new[] { Slot("SLOT", anchor, radius: 1) }, Policy());

            Assert.That(result.Violations, Has.Count.EqualTo(1));
            Assert.That(result.Violations[0].Reason,
                Is.EqualTo(MicrochunkObjectSlotValidator.MissingTileCellInSlotSafetyRadiusReason));
            Assert.That(result.Violations[0].Coordinate, Is.EqualTo(missing));
        }

        [Test]
        public void DuplicateIdsAndAnchorsAreReportedInStablePairOrder()
        {
            var anchor = new MicrochunkLocalCoord(2, 2);
            var slots = new[]
            {
                Slot("SLOT_A", anchor),
                Slot("SLOT_A", new MicrochunkLocalCoord(3, 2)),
                Slot("SLOT_B", anchor)
            };

            var result = MicrochunkObjectSlotValidator.ValidateSlots(
                new MicrochunkId("MC_DUPLICATES"), false,
                new[] { Cell(anchor, null, "M_SAFE"), Cell(new MicrochunkLocalCoord(3, 2), null, "M_SAFE") },
                slots,
                Policy());

            Assert.That(result.Violations.Count(value =>
                value.Reason == MicrochunkObjectSlotValidator.DuplicateSlotIdReason), Is.EqualTo(1));
            var anchorViolation = result.Violations.Single(value =>
                value.Reason == MicrochunkObjectSlotValidator.DuplicateSlotAnchorReason);
            Assert.That(anchorViolation.SlotId, Is.EqualTo("SLOT_A"));
            Assert.That(anchorViolation.ComparedSlotId, Is.EqualTo("SLOT_B"));
        }

        [Test]
        public void UnknownAndMismatchedRequiredMarkersAreReportedSeparately()
        {
            var anchor = new MicrochunkLocalCoord(0, 0);
            var result = Validate(
                Definition(new[] { Cell(anchor, null, "M_OTHER") },
                    new[] { Slot("SLOT", anchor, markerCode: "M_UNKNOWN") }, false),
                Policy());

            Assert.That(result.Violations.Select(value => value.Reason), Is.EqualTo(new[]
            {
                MicrochunkObjectSlotValidator.RequiredMarkerCodeNotAllowedReason,
                MicrochunkObjectSlotValidator.RequiredMarkerMismatchReason
            }));
        }

        [Test]
        public void MatchingRequiredMarkerIsAccepted()
        {
            var anchor = new MicrochunkLocalCoord(0, 0);
            var result = Validate(
                Definition(new[] { Cell(anchor, null, "M_SAFE") },
                    new[] { Slot("SLOT", anchor) }, false),
                Policy());

            Assert.That(result.Success, Is.True);
        }

        [TestCase(MicrochunkTileLayer.GroundSolid)]
        [TestCase(MicrochunkTileLayer.Breakable)]
        [TestCase(MicrochunkTileLayer.Hazard)]
        [TestCase(MicrochunkTileLayer.Liquid)]
        public void ContractBlockingLayersBlockAnchor(MicrochunkTileLayer layer)
        {
            var anchor = new MicrochunkLocalCoord(0, 0);
            var result = Validate(
                Definition(new[] { Cell(anchor, layer, "M_SAFE") },
                    new[] { Slot("SLOT", anchor) }, false),
                Policy());

            Assert.That(result.Violations.Single().Reason,
                Is.EqualTo(MicrochunkObjectSlotValidator.BlockingTileCellAtSlotAnchorReason));
        }

        [TestCase(MicrochunkTileLayer.OneWay)]
        [TestCase(MicrochunkTileLayer.DecorationBack)]
        [TestCase(MicrochunkTileLayer.DecorationFront)]
        [TestCase(MicrochunkTileLayer.Marker)]
        public void ContractNonBlockingLayersDoNotBlockAnchor(MicrochunkTileLayer layer)
        {
            var anchor = new MicrochunkLocalCoord(0, 0);
            var result = Validate(
                Definition(new[] { Cell(anchor, layer, "M_SAFE") },
                    new[] { Slot("SLOT", anchor) }, false),
                Policy());

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void NoneLayersDoNotBlockAnchor()
        {
            var anchor = new MicrochunkLocalCoord(0, 0);
            Assert.That(Validate(
                Definition(new[] { Cell(anchor, null, "M_SAFE") },
                    new[] { Slot("SLOT", anchor) }, false), Policy()).Success, Is.True);
        }

        [Test]
        public void PairSpacingIsReportedOnceUsingStableSlotOrder()
        {
            var first = new MicrochunkLocalCoord(2, 2);
            var second = new MicrochunkLocalCoord(4, 2);
            var result = Validate(
                Definition(new[] { Cell(first, null, "M_SAFE"), Cell(second, null, "M_SAFE") },
                    new[]
                    {
                        Slot("SLOT_Z", second, radius: 2),
                        Slot("SLOT_A", first, radius: 0)
                    }, false),
                Policy());

            var violation = result.Violations.Single(value =>
                value.Reason == MicrochunkObjectSlotValidator.SlotAnchorWithinForbiddenRadiusReason);
            Assert.That(violation.SlotId, Is.EqualTo("SLOT_A"));
            Assert.That(violation.ComparedSlotId, Is.EqualTo("SLOT_Z"));
            Assert.That(violation.Coordinate, Is.EqualTo(second));
        }

        [Test]
        public void PartialDataDoesNotRequireStandaloneNinetySixCellCompleteness()
        {
            var anchor = new MicrochunkLocalCoord(6, 3);
            var result = Validate(
                Definition(new[] { Cell(anchor, null, "M_SAFE") },
                    new[] { Slot("SLOT", anchor, radius: 4) }, false),
                Policy());

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void StarterCompatibleCategoriesAndMarkerCodesValidateTogether()
        {
            var rows = new[]
            {
                new { Id = "RESOURCE", Category = MicrochunkSlotCategory.Resource, Marker = "M_SLOT_RESOURCE", X = 0 },
                new { Id = "MAP_ELEMENT", Category = MicrochunkSlotCategory.MapElement, Marker = "M_SLOT_HAZARD", X = 2 },
                new { Id = "REWARD", Category = MicrochunkSlotCategory.Reward, Marker = "M_SLOT_EVENT", X = 4 },
                new { Id = "EVENT_TRIGGER", Category = MicrochunkSlotCategory.EventTrigger, Marker = "M_SAFE", X = 6 },
                new { Id = "NPC", Category = MicrochunkSlotCategory.Npc, Marker = "M_SAFE", X = 8 }
            };
            var markersByCoordinate = rows.ToDictionary(
                row => new MicrochunkLocalCoord(row.X, 0), row => row.Marker);
            var slots = rows.Select(row => Slot(
                row.Id,
                new MicrochunkLocalCoord(row.X, 0),
                row.Category,
                markerCode: row.Marker));
            var cells = AllCells(coordinate => Cell(
                coordinate,
                null,
                markersByCoordinate.TryGetValue(coordinate, out var marker) ? marker : "NONE"));

            var result = Validate(Definition(cells, slots, true), Policy());

            Assert.That(result.Success, Is.True);
            Assert.That(result.EvaluatedSlotCount, Is.EqualTo(5));
        }

        [TestCase(MicrochunkTransform.R0)]
        [TestCase(MicrochunkTransform.MirrorX)]
        [TestCase(MicrochunkTransform.MirrorY)]
        [TestCase(MicrochunkTransform.R180)]
        public void TransformsPreserveValidAnchorOrientationAndSafety(MicrochunkTransform transform)
        {
            var originalAnchor = new MicrochunkLocalCoord(2, 3);
            var original = Definition(
                AllCells(coordinate => Cell(coordinate, null,
                    coordinate == originalAnchor ? "M_SAFE" : "NONE")),
                new[] { Slot("SLOT", originalAnchor,
                    orientation: MicrochunkObjectOrientation.Left, radius: 2) },
                true);

            var transformed = MicrochunkTransformer.Transform(original, transform).Definition;
            var transformedSlot = transformed.ObjectSlots.Single();

            Assert.That(transformedSlot.Anchor,
                Is.EqualTo(MicrochunkTransformUtility.TransformCoordinate(originalAnchor, transform)));
            Assert.That(transformedSlot.Orientation,
                Is.EqualTo(MicrochunkTransformUtility.TransformOrientation(
                    MicrochunkObjectOrientation.Left, transform)));
            Assert.That(Validate(transformed, Policy()).Success, Is.True);
        }

        [Test]
        public void ValidationDoesNotMutateDefinitionsPolicyOrInputCells()
        {
            var anchor = new MicrochunkLocalCoord(1, 1);
            var cells = new List<MicrochunkTileCell> { Cell(anchor, null, "M_SAFE") };
            var slots = new List<MicrochunkObjectSlotDefinition> { Slot("SLOT", anchor) };
            var pool = Pool("POOL");
            var poolSource = new List<MicrochunkObjectSlotPoolDefinition> { pool };
            var markerSource = new List<string> { "M_SAFE" };
            var policy = new MicrochunkObjectSlotValidationPolicy(poolSource, markerSource);
            var definition = Definition(cells, slots, false);

            var result = Validate(definition, policy);

            Assert.That(result.Success, Is.True);
            Assert.That(cells, Has.Count.EqualTo(1));
            Assert.That(slots, Has.Count.EqualTo(1));
            Assert.That(poolSource, Has.Count.EqualTo(1));
            Assert.That(markerSource, Is.EqualTo(new[] { "M_SAFE" }));
            Assert.That(definition.TileCells.Single(), Is.SameAs(cells[0]));
            Assert.That(definition.ObjectSlots.Single(), Is.SameAs(slots[0]));
        }

        [Test]
        public void ResultIsImmutableAndUsesContractOrdering()
        {
            var id = new MicrochunkId("MC_ORDER");
            var coordinate = new MicrochunkLocalCoord(1, 0);
            var values = new List<MicrochunkObjectSlotValidationViolation>
            {
                Violation(id, "SLOT_B", "A", null, string.Empty),
                Violation(id, "SLOT_A", "B", coordinate, "SLOT_Z"),
                Violation(id, "SLOT_A", "A", coordinate, "SLOT_Z"),
                Violation(id, "SLOT_A", "B", null, "SLOT_Y")
            };
            var result = new MicrochunkObjectSlotValidationResult(2, values);
            values.Clear();

            Assert.That(result.Violations.Select(value =>
                    value.SlotId + "|" + value.Reason + "|" +
                    (value.HasCoordinate ? value.Coordinate.Value.RowMajorIndex.ToString() : "NONE") + "|" +
                    value.ComparedSlotId),
                Is.EqualTo(new[]
                {
                    "SLOT_A|A|1|SLOT_Z",
                    "SLOT_A|B|NONE|SLOT_Y",
                    "SLOT_A|B|1|SLOT_Z",
                    "SLOT_B|A|NONE|"
                }));
            Assert.That(result.IssueCount, Is.EqualTo(4));
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void NullValidationInputsAreRejected()
        {
            var definition = PartialDefinition(Array.Empty<MicrochunkObjectSlotDefinition>());
            Assert.Throws<ArgumentNullException>(() =>
                MicrochunkObjectSlotValidator.ValidateDefinition(null, Policy()));
            Assert.Throws<ArgumentNullException>(() =>
                MicrochunkObjectSlotValidator.ValidateDefinition(definition, null));
            Assert.Throws<ArgumentNullException>(() =>
                MicrochunkObjectSlotValidator.ValidateSlots(
                    definition.Id, false, null, definition.ObjectSlots, Policy()));
            Assert.Throws<ArgumentNullException>(() =>
                MicrochunkObjectSlotValidator.ValidateSlots(
                    definition.Id, false, definition.TileCells, null, Policy()));
        }

        private static MicrochunkObjectSlotValidationResult Validate(
            MicrochunkDefinition definition,
            MicrochunkObjectSlotValidationPolicy policy)
        {
            return MicrochunkObjectSlotValidator.ValidateDefinition(definition, policy);
        }

        private static MicrochunkObjectSlotValidationPolicy Policy()
        {
            return new MicrochunkObjectSlotValidationPolicy(
                new[] { Pool("POOL") },
                new[] { "M_SAFE", "M_SLOT_RESOURCE", "M_SLOT_HAZARD", "M_SLOT_EVENT" });
        }

        private static MicrochunkObjectSlotPoolDefinition Pool(string poolId)
        {
            return new MicrochunkObjectSlotPoolDefinition(
                poolId, AllCategories, true, true, string.Empty);
        }

        private static MicrochunkDefinition PartialDefinition(
            IEnumerable<MicrochunkObjectSlotDefinition> slots)
        {
            var cells = slots.Select(slot => Cell(slot.Anchor, null, slot.RequiredMarkerCode));
            return Definition(cells, slots, false);
        }

        private static MicrochunkDefinition Definition(
            IEnumerable<MicrochunkTileCell> cells,
            IEnumerable<MicrochunkObjectSlotDefinition> slots,
            bool complete)
        {
            return new MicrochunkDefinition(
                new MicrochunkId("MC_OBJECT_SLOT_TEST"),
                "object slot test",
                MicrochunkConstants.WidthTiles,
                MicrochunkConstants.HeightTiles,
                MicrochunkUsageClass.Traversal,
                new[] { "BIOME_TEST" },
                new[] { "ROUTE_TEST" },
                new[] { MicrochunkTransform.R0 },
                1,
                0,
                0,
                0,
                complete,
                "PREFAB_TEST",
                true,
                string.Empty,
                cells,
                Array.Empty<MicrochunkSocketDefinition>(),
                slots);
        }

        private static MicrochunkObjectSlotDefinition Slot(
            string id,
            MicrochunkLocalCoord anchor,
            MicrochunkSlotCategory category = MicrochunkSlotCategory.Resource,
            string poolId = "POOL",
            bool required = false,
            MicrochunkObjectOrientation orientation = MicrochunkObjectOrientation.None,
            int radius = 0,
            string markerCode = "M_SAFE")
        {
            return new MicrochunkObjectSlotDefinition(
                id,
                anchor,
                category,
                poolId,
                required,
                orientation,
                true,
                radius,
                markerCode,
                string.Empty);
        }

        private static IEnumerable<MicrochunkTileCell> AllCells(
            Func<MicrochunkLocalCoord, MicrochunkTileCell> factory)
        {
            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
            {
                yield return factory(new MicrochunkLocalCoord(x, y));
            }
        }

        private static MicrochunkTileCell Cell(
            MicrochunkLocalCoord coordinate,
            MicrochunkTileLayer? occupiedLayer,
            string markerCode)
        {
            string Code(MicrochunkTileLayer layer)
            {
                return occupiedLayer == layer ? "OCCUPIED" : "NONE";
            }

            return new MicrochunkTileCell(
                coordinate,
                Code(MicrochunkTileLayer.GroundSolid),
                Code(MicrochunkTileLayer.OneWay),
                Code(MicrochunkTileLayer.Breakable),
                Code(MicrochunkTileLayer.Hazard),
                Code(MicrochunkTileLayer.Liquid),
                Code(MicrochunkTileLayer.DecorationBack),
                Code(MicrochunkTileLayer.DecorationFront),
                markerCode);
        }

        private static MicrochunkObjectSlotValidationViolation Violation(
            MicrochunkId id,
            string slotId,
            string reason,
            MicrochunkLocalCoord? coordinate,
            string comparedSlotId)
        {
            return new MicrochunkObjectSlotValidationViolation(
                id,
                slotId,
                MicrochunkSlotCategory.Resource,
                "POOL",
                coordinate,
                comparedSlotId,
                reason);
        }
    }
}
