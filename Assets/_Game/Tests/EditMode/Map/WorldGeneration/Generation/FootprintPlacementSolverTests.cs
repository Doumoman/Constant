using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class FootprintPlacementSolverTests
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";

        private static readonly FileSpec[] SpecialSpecs = CreateSpecialSpecs();
        private static readonly SiteFootprintTransform[] Transforms =
        {
            SiteFootprintTransform.R0,
            SiteFootprintTransform.MirrorX,
            SiteFootprintTransform.MirrorY,
            SiteFootprintTransform.R180
        };

        public static IEnumerable CoordinateCases()
        {
            foreach (var transform in Transforms)
            {
                for (var y = 0; y < 2; y++)
                {
                    for (var x = 0; x < 3; x++)
                    {
                        var expectedX = transform == SiteFootprintTransform.MirrorX ||
                                        transform == SiteFootprintTransform.R180
                            ? 2 - x
                            : x;
                        var expectedY = transform == SiteFootprintTransform.MirrorY ||
                                        transform == SiteFootprintTransform.R180
                            ? 1 - y
                            : y;
                        yield return new TestCaseData(transform, x, y, expectedX, expectedY);
                    }
                }
            }
        }

        public static IEnumerable SideCases()
        {
            foreach (var transform in Transforms)
            {
                foreach (SiteEntrySide side in Enum.GetValues(typeof(SiteEntrySide)))
                {
                    var expected = side;
                    if (transform == SiteFootprintTransform.MirrorX ||
                        transform == SiteFootprintTransform.R180)
                    {
                        if (expected == SiteEntrySide.L) expected = SiteEntrySide.R;
                        else if (expected == SiteEntrySide.R) expected = SiteEntrySide.L;
                    }
                    if (transform == SiteFootprintTransform.MirrorY ||
                        transform == SiteFootprintTransform.R180)
                    {
                        if (expected == SiteEntrySide.U) expected = SiteEntrySide.D;
                        else if (expected == SiteEntrySide.D) expected = SiteEntrySide.U;
                    }
                    yield return new TestCaseData(transform, side, expected);
                }
            }
        }

        public static IEnumerable StartCases()
        {
            var ordinal = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var origin = WorldGridIndex.ToCoordinate(index);
                if (EdgeRing(origin) <= 1)
                {
                    yield return new TestCaseData(index, ordinal);
                    ordinal++;
                }
            }
        }

        public static IEnumerable ErrorCodeCases()
        {
            foreach (FootprintPlacementErrorCode code in
                     Enum.GetValues(typeof(FootprintPlacementErrorCode)))
            {
                yield return new TestCaseData(code, (int)code);
            }
        }

        [TestCaseSource(nameof(CoordinateCases))]
        public void Transformer_AsymmetricThreeByTwoCoordinateTable(
            SiteFootprintTransform transform,
            int sourceX,
            int sourceY,
            int expectedX,
            int expectedY)
        {
            Assert.That(SiteFootprintTransformer.TryTransformCoordinate(
                3, 2, transform, sourceX, sourceY, out var actualX, out var actualY), Is.True);
            Assert.That(actualX, Is.EqualTo(expectedX));
            Assert.That(actualY, Is.EqualTo(expectedY));
        }

        [TestCaseSource(nameof(SideCases))]
        public void Transformer_ExactSideMapping(
            SiteFootprintTransform transform,
            SiteEntrySide source,
            SiteEntrySide expected)
        {
            Assert.That(SiteFootprintTransformer.TryTransformSide(transform, source, out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(0, 2, 0, 0)]
        [TestCase(3, 0, 0, 0)]
        [TestCase(14, 2, 0, 0)]
        [TestCase(3, 14, 0, 0)]
        [TestCase(3, 2, -1, 0)]
        [TestCase(3, 2, 3, 0)]
        [TestCase(3, 2, 0, -1)]
        [TestCase(3, 2, 0, 2)]
        public void Transformer_RejectsInvalidDimensionsAndSourceCoordinates(
            int width,
            int height,
            int sourceX,
            int sourceY)
        {
            Assert.That(SiteFootprintTransformer.TryTransformCoordinate(
                width, height, SiteFootprintTransform.R0, sourceX, sourceY, out _, out _), Is.False);
        }

        [Test]
        public void Transformer_RejectsUndefinedEnumsWithoutDimensionSwap()
        {
            Assert.That(SiteFootprintTransformer.TryTransformCoordinate(
                3, 2, (SiteFootprintTransform)99, 1, 1, out _, out _), Is.False);
            Assert.That(SiteFootprintTransformer.TryTransformSide(
                (SiteFootprintTransform)99, SiteEntrySide.L, out _), Is.False);
            Assert.That(SiteFootprintTransformer.TryTransformSide(
                SiteFootprintTransform.R0, (SiteEntrySide)99, out _), Is.False);
            Assert.That(Enum.GetNames(typeof(SiteFootprintTransform)),
                Is.EqualTo(new[] { "R0", "MirrorX", "MirrorY", "R180" }));
        }

        [TestCaseSource(nameof(StartCases))]
        public void SolveStart_AllExactRawCandidatesSucceed(int originIndex, int candidateOrdinal)
        {
            var candidate = Candidate(
                SiteReservationKind.Start,
                WorldId,
                originIndex,
                candidateOrdinal);
            var result = new FootprintPlacementSolver().SolveStart(
                candidate,
                FootprintPlacementBlockers.Empty);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Placement.Footprint.Transform, Is.EqualTo(SiteFootprintTransform.R0));
            Assert.That(result.Placement.Footprint.Width, Is.EqualTo(1));
            Assert.That(result.Placement.Footprint.Height, Is.EqualTo(1));
            Assert.That(result.Placement.OccupiedSectors, Is.EqualTo(new[] { candidate.Origin }));
            Assert.That(result.Placement.Entries, Is.Empty);
        }

        [TestCaseSource(nameof(ErrorCodeCases))]
        public void ErrorCode_UsesFrozenOrdinalOrder(FootprintPlacementErrorCode code, int ordinal)
        {
            Assert.That((int)code, Is.EqualTo(ordinal));
            var error = new FootprintPlacementError(code, string.Empty, string.Empty, -1, "stable");
            Assert.That(error.Code, Is.EqualTo(code));
        }

        [Test]
        public void StarterDefinitionsAndExactEvaluationMatrixPass()
        {
            var definitions = CreateDefinitionSet();
            AssertStarterDefinitions(definitions);

            var expectations = new[]
            {
                new MatrixExpectation(SiteReservationKind.Boss, BossId, 572, 52, 52),
                new MatrixExpectation(SiteReservationKind.Forge, ForgeId, 624, 0, 52),
                new MatrixExpectation(SiteReservationKind.CoreResource, CassiaId, 624, 0, 52),
                new MatrixExpectation(SiteReservationKind.CoreResource, YeastId, 624, 0, 52),
                new MatrixExpectation(SiteReservationKind.CoreResource, MeteorId, 624, 0, 52)
            };
            var solver = new FootprintPlacementSolver();
            var totalEvaluations = 88;
            var totalSuccess = 88;
            var totals = new Dictionary<FootprintPlacementErrorCode, int>();

            foreach (var expectation in expectations)
            {
                var map = definitions.SpecialMaps[expectation.SourceId];
                var cells = definitions.GetSpecialMapFootprintCells(expectation.SourceId);
                var entries = definitions.GetSpecialMapEntrySockets(expectation.SourceId);
                var success = 0;
                var localErrors = new Dictionary<FootprintPlacementErrorCode, int>();
                for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                {
                    foreach (var transform in Transforms)
                    {
                        var result = solver.SolveSpecialSite(
                            Candidate(expectation.Kind, expectation.SourceId, index, index),
                            transform,
                            map,
                            cells,
                            entries,
                            FootprintPlacementBlockers.Empty);
                        totalEvaluations++;
                        if (result.Succeeded)
                        {
                            success++;
                            totalSuccess++;
                        }
                        else
                        {
                            Assert.That(result.Errors.Count, Is.EqualTo(1));
                            Increment(localErrors, result.Errors[0].Code);
                            Increment(totals, result.Errors[0].Code);
                        }
                    }
                }

                Assert.That(success, Is.EqualTo(expectation.Success));
                Assert.That(Count(localErrors, FootprintPlacementErrorCode.FootprintOutsideWorld),
                    Is.EqualTo(expectation.FootprintOutside));
                Assert.That(Count(localErrors, FootprintPlacementErrorCode.EntryOutsideWorld),
                    Is.EqualTo(expectation.EntryOutside));
                Assert.That(localErrors.Keys.Except(new[]
                {
                    FootprintPlacementErrorCode.FootprintOutsideWorld,
                    FootprintPlacementErrorCode.EntryOutsideWorld
                }), Is.Empty);
            }

            Assert.That(totalEvaluations, Is.EqualTo(3468));
            Assert.That(totalSuccess, Is.EqualTo(3156));
            Assert.That(Count(totals, FootprintPlacementErrorCode.FootprintOutsideWorld), Is.EqualTo(52));
            Assert.That(Count(totals, FootprintPlacementErrorCode.EntryOutsideWorld), Is.EqualTo(260));

            var boss = definitions.SpecialMaps[BossId];
            foreach (var transform in Transforms)
            {
                var boundary = new FootprintPlacementSolver().SolveSpecialSite(
                    Candidate(SiteReservationKind.Boss, BossId, 168, 168),
                    transform,
                    boss,
                    definitions.GetSpecialMapFootprintCells(BossId),
                    definitions.GetSpecialMapEntrySockets(BossId),
                    FootprintPlacementBlockers.Empty);
                Assert.That(boundary.Errors.Select(error => error.Code),
                    Is.EqualTo(new[] { FootprintPlacementErrorCode.FootprintOutsideWorld }));
            }
        }

        [Test]
        public void Transform_PreservesPayloadAndUsesOneRuleForCellsSidesAndEntry()
        {
            var definitions = CreateDefinitionSet();
            var result = Solve(definitions, BossId, SiteReservationKind.Boss, 70,
                SiteFootprintTransform.MirrorX, FootprintPlacementBlockers.Empty);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Placement.Footprint.Width, Is.EqualTo(2));
            Assert.That(result.Placement.Footprint.Height, Is.EqualTo(1));
            var entryCell = result.Placement.Footprint.Cells.Single(cell => cell.LocalRole == "ENTRY");
            Assert.That(entryCell.LocalX, Is.EqualTo(1));
            Assert.That(entryCell.RequiredPrimaryBiomeId, Is.EqualTo("BIOME_MOON"));
            Assert.That(entryCell.FixedSectorRecipeId, Is.EqualTo("RECIPE_FIXED"));
            Assert.That(entryCell.RequiredOpenSides, Is.EqualTo(new[] { SiteEntrySide.R }));
            var entry = result.Placement.Entries.Single();
            Assert.That(entry.LocalX, Is.EqualTo(entryCell.LocalX));
            Assert.That(entry.LocalY, Is.EqualTo(entryCell.LocalY));
            Assert.That(entry.Side, Is.EqualTo(SiteEntrySide.R));
            Assert.That(entry.AllowedRouteTypes, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(entry.Required, Is.True);
            Assert.That(entry.ReturnPathRequired, Is.True);
        }

        [Test]
        public void Solver_UsesExactCollisionPrecedenceAndAllowsApprovedAdjacency()
        {
            var definitions = CreateDefinitionSet();
            var originIndex = 70;

            AssertFailure(
                Solve(definitions, ForgeId, SiteReservationKind.Forge, originIndex,
                    SiteFootprintTransform.R0,
                    new FootprintPlacementBlockers(new[] { originIndex }, Array.Empty<int>())),
                FootprintPlacementErrorCode.FootprintOverlap);
            AssertFailure(
                Solve(definitions, ForgeId, SiteReservationKind.Forge, originIndex,
                    SiteFootprintTransform.R0,
                    new FootprintPlacementBlockers(Array.Empty<int>(), new[] { originIndex })),
                FootprintPlacementErrorCode.BlocksExistingEntryApproach);
            AssertFailure(
                Solve(definitions, ForgeId, SiteReservationKind.Forge, originIndex,
                    SiteFootprintTransform.R0,
                    new FootprintPlacementBlockers(new[] { originIndex - 1 }, Array.Empty<int>())),
                FootprintPlacementErrorCode.EntryApproachOccupied);

            var protectedExterior = new FootprintPlacementBlockers(
                Array.Empty<int>(), new[] { originIndex - 1 });
            Assert.That(Solve(definitions, ForgeId, SiteReservationKind.Forge, originIndex,
                SiteFootprintTransform.R0, protectedExterior).Succeeded, Is.True);

            var adjacent = new FootprintPlacementBlockers(
                new[] { originIndex + WorldGenConstants.SectorColumns }, Array.Empty<int>());
            Assert.That(Solve(definitions, ForgeId, SiteReservationKind.Forge, originIndex,
                SiteFootprintTransform.R0, adjacent).Succeeded, Is.True);
        }

        [Test]
        public void Solver_RejectsOwnFootprintDuplicateFaceAndEntryNotOnFootprint()
        {
            var ownFacing = CreateDefinitionSet(rows =>
                FindRow(rows["special_map_entry_sockets.csv"], BossId)[4] = "R");
            AssertFailure(Solve(ownFacing, BossId, SiteReservationKind.Boss, 70,
                SiteFootprintTransform.R0, FootprintPlacementBlockers.Empty),
                FootprintPlacementErrorCode.EntryFacesOwnFootprint);

            var duplicateFace = CreateDefinitionSet(rows =>
                rows["special_map_entry_sockets.csv"].Add(
                    EntryRow(BossId, "ENTRY_SECOND", 0, 0, "L")));
            AssertFailure(Solve(duplicateFace, BossId, SiteReservationKind.Boss, 70,
                SiteFootprintTransform.R0, FootprintPlacementBlockers.Empty),
                FootprintPlacementErrorCode.DuplicateEntryFace);

            var sparse = CreateDefinitionSet(rows =>
            {
                rows["special_map_footprint_cells.csv"].RemoveAll(
                    row => row[0] == BossId && row[1] == "1");
                FindRow(rows["special_map_entry_sockets.csv"], BossId)[2] = "1";
            });
            AssertFailure(Solve(sparse, BossId, SiteReservationKind.Boss, 70,
                SiteFootprintTransform.R0, FootprintPlacementBlockers.Empty),
                FootprintPlacementErrorCode.EntryNotOnFootprint);
        }

        [Test]
        public void Solver_AccumulatesSourceInputErrorsWithoutPartialPlacement()
        {
            var definitions = CreateDefinitionSet();
            var boss = definitions.SpecialMaps[BossId];
            var result = new FootprintPlacementSolver().SolveSpecialSite(
                Candidate(SiteReservationKind.Forge, ForgeId, 70, 70),
                (SiteFootprintTransform)99,
                boss,
                new SpecialMapFootprintCellDefinition[] { null },
                new SpecialMapEntrySocketDefinition[] { null },
                null);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Placement, Is.Null);
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(FootprintPlacementErrorCode.MissingBlockers));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(FootprintPlacementErrorCode.UnsupportedTransform));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(FootprintPlacementErrorCode.InvalidSpecialMap));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(FootprintPlacementErrorCode.SourceIdentityMismatch));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(FootprintPlacementErrorCode.NullFootprintCell));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(FootprintPlacementErrorCode.NullEntrySocket));
            AssertSorted(result.Errors);
        }

        [Test]
        public void Blockers_CopySortValidateAndFromReservationsBuildsExactUnions()
        {
            var occupiedInput = new List<int> { 70, 10 };
            var protectedInput = new List<int> { 20, 19 };
            var blockers = new FootprintPlacementBlockers(occupiedInput, protectedInput);
            occupiedInput.Clear();
            protectedInput.Clear();
            Assert.That(blockers.OccupiedSectorIndices, Is.EqualTo(new[] { 10, 70 }));
            Assert.That(blockers.ProtectedEntryApproachSectorIndices, Is.EqualTo(new[] { 19, 20 }));
            Assert.Throws<ArgumentException>(() =>
                new FootprintPlacementBlockers(new[] { 1, 1 }, Array.Empty<int>()));
            Assert.Throws<ArgumentException>(() =>
                new FootprintPlacementBlockers(new[] { 1 }, new[] { 1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FootprintPlacementBlockers(new[] { -1 }, Array.Empty<int>()));

            var reservation = Reservation("SITE_A", new SectorCoord(5, 5), SiteEntrySide.L);
            var fromReservations = FootprintPlacementBlockers.FromReservations(new[] { reservation });
            Assert.That(fromReservations.OccupiedSectorIndices,
                Is.EqualTo(new[] { WorldGridIndex.ToIndex(new SectorCoord(5, 5)) }));
            Assert.That(fromReservations.ProtectedEntryApproachSectorIndices,
                Is.EqualTo(new[] { WorldGridIndex.ToIndex(new SectorCoord(4, 5)) }));
            Assert.Throws<ArgumentException>(() =>
                FootprintPlacementBlockers.FromReservations(new[] { reservation, reservation }));
            Assert.Throws<ArgumentException>(() =>
                FootprintPlacementBlockers.FromReservations(new SiteReservation[] { null }));
            Assert.Throws<ArgumentException>(() =>
                FootprintPlacementBlockers.FromReservations(new[]
                {
                    Reservation("SITE_A", new SectorCoord(5, 5), SiteEntrySide.L),
                    Reservation("SITE_B", new SectorCoord(5, 5), SiteEntrySide.R)
                }));
            Assert.Throws<ArgumentException>(() =>
                FootprintPlacementBlockers.FromReservations(new[]
                {
                    Reservation("SITE_EDGE", new SectorCoord(0, 0), SiteEntrySide.L)
                }));
        }

        [Test]
        public void ResultPlacementAndEntryAreCanonicalReadOnlySnapshots()
        {
            var definitions = CreateDefinitionSet();
            var result = Solve(definitions, BossId, SiteReservationKind.Boss, 70,
                SiteFootprintTransform.R0, FootprintPlacementBlockers.Empty);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Placement.OccupiedSectors.Select(WorldGridIndex.ToIndex), Is.Ordered);
            Assert.That(result.Placement.Entries.Select(entry => entry.EntrySocketId), Is.Ordered);
            Assert.That(result.Placement.TryGetFootprintCell(
                result.Placement.OccupiedSectors[0], out var cell), Is.True);
            Assert.That(cell, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SectorCoord>)result.Placement.OccupiedSectors).Add(new SectorCoord(0, 0)));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<int>)result.Placement.Entries[0].AllowedRouteTypes).Add(2));

            var errors = new List<FootprintPlacementError>
            {
                new FootprintPlacementError(FootprintPlacementErrorCode.EntryOutsideWorld,
                    BossId, "ENTRY_Z", -1, "z"),
                new FootprintPlacementError(FootprintPlacementErrorCode.MissingCandidate,
                    string.Empty, string.Empty, -1, "a")
            };
            var failure = FootprintPlacementResult.Failure(errors);
            errors.Clear();
            Assert.That(failure.Errors.Count, Is.EqualTo(2));
            AssertSorted(failure.Errors);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<FootprintPlacementError>)failure.Errors).Clear());
        }

        [Test]
        public void Solver_IsInputOrderSeedCultureAndReuseInvariant()
        {
            var definitions = CreateDefinitionSet();
            var map = definitions.SpecialMaps[BossId];
            var cells = definitions.GetSpecialMapFootprintCells(BossId).Reverse().ToList();
            var entries = definitions.GetSpecialMapEntrySockets(BossId).Reverse().ToList();
            var solver = new FootprintPlacementSolver();
            var baseline = Snapshot(solver.SolveSpecialSite(
                Candidate(SiteReservationKind.Boss, BossId, 70, 70),
                SiteFootprintTransform.R180, map, cells, entries,
                new FootprintPlacementBlockers(new[] { 1, 2 }, new[] { 3, 4 })));

            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                foreach (var cultureName in new[] { "en-US", "tr-TR" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                    for (var run = 0; run < 100; run++)
                    {
                        var actual = Snapshot(solver.SolveSpecialSite(
                            Candidate(SiteReservationKind.Boss, BossId, 70, run),
                            SiteFootprintTransform.R180, map,
                            cells.AsEnumerable().Reverse(), entries.AsEnumerable().Reverse(),
                            new FootprintPlacementBlockers(new[] { 2, 1 }, new[] { 4, 3 })));
                        Assert.That(actual, Is.EqualTo(baseline));
                    }
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void PublicApi_HasNoMutableSurfaceOrLaterTaskTypes()
        {
            var productionTypes = new[]
            {
                typeof(SiteFootprintTransformer), typeof(FootprintPlacementEntry),
                typeof(FootprintPlacement), typeof(FootprintPlacementBlockers),
                typeof(FootprintPlacementError), typeof(FootprintPlacementResult),
                typeof(FootprintPlacementSolver)
            };
            foreach (var type in productionTypes)
            {
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
                    Is.Empty, type.FullName);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.SetMethod != null), Is.Empty, type.FullName);
            }

            var methods = typeof(FootprintPlacementSolver)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(methods, Is.EqualTo(new[] { "SolveSpecialSite", "SolveStart" }));
            Assert.That(productionTypes.Select(type => type.FullName),
                Has.None.Contains("Distance").And.None.Contains("Cost")
                    .And.None.Contains("Backtrack").And.None.Contains("Village"));
        }

        private static FootprintPlacementResult Solve(
            SpecialVillageDefinitionSet definitions,
            string sourceId,
            SiteReservationKind kind,
            int originIndex,
            SiteFootprintTransform transform,
            FootprintPlacementBlockers blockers)
        {
            return new FootprintPlacementSolver().SolveSpecialSite(
                Candidate(kind, sourceId, originIndex, originIndex),
                transform,
                definitions.SpecialMaps[sourceId],
                definitions.GetSpecialMapFootprintCells(sourceId),
                definitions.GetSpecialMapEntrySockets(sourceId),
                blockers);
        }

        private static void AssertFailure(
            FootprintPlacementResult result,
            FootprintPlacementErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Placement, Is.Null);
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain(code));
            AssertSorted(result.Errors);
        }

        private static void AssertSorted(IReadOnlyList<FootprintPlacementError> errors)
        {
            for (var index = 1; index < errors.Count; index++)
            {
                Assert.That(Compare(errors[index - 1], errors[index]), Is.LessThanOrEqualTo(0));
            }
        }

        private static int Compare(FootprintPlacementError left, FootprintPlacementError right)
        {
            var code = left.Code.CompareTo(right.Code);
            if (code != 0) return code;
            var source = string.Compare(left.SourceDefinitionId, right.SourceDefinitionId, StringComparison.Ordinal);
            if (source != 0) return source;
            var entry = string.Compare(left.EntrySocketId, right.EntrySocketId, StringComparison.Ordinal);
            if (entry != 0) return entry;
            var sector = left.SectorIndex.CompareTo(right.SectorIndex);
            return sector != 0 ? sector : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static string Snapshot(FootprintPlacementResult result)
        {
            if (!result.Succeeded)
            {
                return string.Join("|", result.Errors.Select(error =>
                    (int)error.Code + ":" + error.SourceDefinitionId + ":" +
                    error.EntrySocketId + ":" + error.SectorIndex + ":" + error.Message));
            }
            return string.Join(",", result.Placement.OccupiedSectors.Select(WorldGridIndex.ToIndex)) + "|" +
                   string.Join(",", result.Placement.Entries.Select(entry =>
                       entry.EntrySocketId + ":" + entry.LocalX + ":" + entry.LocalY + ":" +
                       (int)entry.Side + ":" + WorldGridIndex.ToIndex(entry.ExteriorSector)));
        }

        private static SiteOriginCandidate Candidate(
            SiteReservationKind kind,
            string sourceId,
            int originIndex,
            int candidateOrdinal)
        {
            var origin = WorldGridIndex.ToCoordinate(originIndex);
            return new SiteOriginCandidate(kind, sourceId, 0, origin, originIndex,
                EdgeRing(origin), candidateOrdinal);
        }

        private static int EdgeRing(SectorCoord origin)
        {
            return Math.Min(
                Math.Min(origin.X, WorldGenConstants.SectorColumns - 1 - origin.X),
                Math.Min(origin.Y, WorldGenConstants.SectorRows - 1 - origin.Y));
        }

        private static SiteReservation Reservation(
            string id,
            SectorCoord origin,
            SiteEntrySide side)
        {
            var reservationId = new SiteReservationId(id);
            var footprint = new SiteFootprint(1, 1, SiteFootprintTransform.R0,
                new[]
                {
                    new SiteFootprintCell(0, 0, "CORE", string.Empty, string.Empty,
                        Array.Empty<SiteEntrySide>())
                });
            var anchor = new SiteEntryAnchor(reservationId, "ENTRY", origin, side,
                new[] { 1, 2, 3 }, true, true);
            return new SiteReservation(reservationId, SiteReservationKind.CoreResource,
                id, origin, footprint, string.Empty, 0, new[] { anchor });
        }

        private static void AssertStarterDefinitions(SpecialVillageDefinitionSet definitions)
        {
            var expected = new[]
            {
                new { Id = BossId, Role = "BOSS", Width = 2, Height = 1, Cells = 2 },
                new { Id = ForgeId, Role = "FORGE", Width = 1, Height = 1, Cells = 1 },
                new { Id = CassiaId, Role = "CORE_RESOURCE", Width = 1, Height = 1, Cells = 1 },
                new { Id = YeastId, Role = "CORE_RESOURCE", Width = 1, Height = 1, Cells = 1 },
                new { Id = MeteorId, Role = "CORE_RESOURCE", Width = 1, Height = 1, Cells = 1 }
            };
            foreach (var item in expected)
            {
                var map = definitions.SpecialMaps[item.Id];
                Assert.That(map.SiteRole, Is.EqualTo(item.Role));
                Assert.That(map.FootprintWidthSectors, Is.EqualTo(item.Width));
                Assert.That(map.FootprintHeightSectors, Is.EqualTo(item.Height));
                Assert.That(definitions.GetSpecialMapFootprintCells(item.Id).Count, Is.EqualTo(item.Cells));
                var entry = definitions.GetSpecialMapEntrySockets(item.Id).Single();
                Assert.That(entry.EntrySocketId, Is.EqualTo("ENTRY_L"));
                Assert.That(entry.AllowedRouteTypes, Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(entry.Required, Is.True);
                Assert.That(entry.ReturnPathRequired, Is.True);
            }
        }

        private static void Increment(
            IDictionary<FootprintPlacementErrorCode, int> counts,
            FootprintPlacementErrorCode code)
        {
            counts[code] = Count(counts, code) + 1;
        }

        private static int Count(
            IDictionary<FootprintPlacementErrorCode, int> counts,
            FootprintPlacementErrorCode code)
        {
            return counts.TryGetValue(code, out var value) ? value : 0;
        }

        private static SpecialVillageDefinitionSet CreateDefinitionSet(
            Action<Dictionary<string, List<string[]>>> configure = null)
        {
            var rows = StarterRows();
            configure?.Invoke(rows);
            var sources = new List<SpecialVillageDefinitionSource>();
            foreach (var spec in SpecialSpecs)
            {
                rows.TryGetValue(spec.FileName, out var exactRows);
                sources.Add(BuildSpecialSource(spec, exactRows));
            }
            var result = new SpecialVillageDefinitionBuilder().Build(sources);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            return result.DefinitionSet;
        }

        private static Dictionary<string, List<string[]>> StarterRows()
        {
            var catalog = new List<string[]>
            {
                CatalogRow(BossId, "BOSS", 2, 1),
                CatalogRow(ForgeId, "FORGE", 1, 1),
                CatalogRow(CassiaId, "CORE_RESOURCE", 1, 1),
                CatalogRow(YeastId, "CORE_RESOURCE", 1, 1),
                CatalogRow(MeteorId, "CORE_RESOURCE", 1, 1)
            };
            var footprints = new List<string[]>
            {
                FootprintRow(BossId, 0, 0, "ENTRY", "L"),
                FootprintRow(BossId, 1, 0, "ARENA", "R"),
                FootprintRow(ForgeId, 0, 0, "CORE", "L"),
                FootprintRow(CassiaId, 0, 0, "CORE", "L"),
                FootprintRow(YeastId, 0, 0, "CORE", "L"),
                FootprintRow(MeteorId, 0, 0, "CORE", "L")
            };
            var entries = new List<string[]>
            {
                EntryRow(BossId, "ENTRY_L", 0, 0, "L"),
                EntryRow(ForgeId, "ENTRY_L", 0, 0, "L"),
                EntryRow(CassiaId, "ENTRY_L", 0, 0, "L"),
                EntryRow(YeastId, "ENTRY_L", 0, 0, "L"),
                EntryRow(MeteorId, "ENTRY_L", 0, 0, "L")
            };
            return new Dictionary<string, List<string[]>>(StringComparer.Ordinal)
            {
                { "special_map_catalog.csv", catalog },
                { "special_map_footprint_cells.csv", footprints },
                { "special_map_entry_sockets.csv", entries }
            };
        }

        private static string[] CatalogRow(
            string sourceId,
            string role,
            int width,
            int height)
        {
            return new[]
            {
                sourceId, "Site", role, "BIOME_MOON",
                width.ToString(CultureInfo.InvariantCulture),
                height.ToString(CultureInfo.InvariantCulture),
                "1", "0", "0", "1|2|3", "0", "REWARD_NONE", "FIXED", "1", "test"
            };
        }

        private static string[] FootprintRow(
            string sourceId,
            int x,
            int y,
            string role,
            string sides)
        {
            return new[]
            {
                sourceId,
                x.ToString(CultureInfo.InvariantCulture),
                y.ToString(CultureInfo.InvariantCulture),
                role, "BIOME_MOON", "RECIPE_FIXED", sides, "test"
            };
        }

        private static string[] EntryRow(
            string sourceId,
            string entryId,
            int x,
            int y,
            string side)
        {
            return new[]
            {
                sourceId, entryId,
                x.ToString(CultureInfo.InvariantCulture),
                y.ToString(CultureInfo.InvariantCulture),
                side, "1|2|3", "1", "1", "test"
            };
        }

        private static string[] FindRow(IEnumerable<string[]> rows, string sourceId)
        {
            return rows.First(row => row[0] == sourceId);
        }

        private static SpecialVillageDefinitionSource BuildSpecialSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var schemaRows = spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName,
                (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name,
                column.DataType,
                index < spec.PrimaryKeyCount ? "1" : "0",
                index < spec.PrimaryKeyCount
                    ? (index + 1).ToString(CultureInfo.InvariantCulture)
                    : string.Empty,
                string.Empty,
                column.AllowedValues,
                string.Empty,
                string.Empty,
                index + 2));
            var catalog = new CsvSchemaCatalogBuilder().Build(schemaRows);
            Assert.That(catalog.Success, Is.True, string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var sourceRows = rows ?? new[] { StandardRow(spec) };
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            foreach (var row in sourceRows) csv += "\n" + string.Join(",", row.Select(CsvCell));
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            Assert.That(validation.Success, Is.True, string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            Assert.That(keys.Success, Is.True);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            return new SpecialVillageDefinitionSource(schema, parsed);
        }

        private static string[] StandardRow(FileSpec spec)
        {
            return spec.Columns.Select((column, index) =>
            {
                var allowed = column.AllowedValues.Split(
                    new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if ((column.DataType == "ENUM" || column.DataType == "ENUM_LIST") && allowed.Length > 0)
                    return allowed[0];
                switch (column.DataType)
                {
                    case "STRING": return "TEXT_" + (index + 1);
                    case "ID": return "ID_" + (index + 1);
                    case "INT": return (index + 1).ToString(CultureInfo.InvariantCulture);
                    case "FLOAT": return "0.25";
                    case "BOOL": return "0";
                    case "ID_LIST": return "LIST_A|LIST_B";
                    case "ENUM_LIST": return "L";
                    case "INT_LIST": return "1|2";
                    default: throw new ArgumentOutOfRangeException(nameof(column.DataType));
                }
            }).ToArray();
        }

        private static string CsvCell(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static FileSpec[] CreateSpecialSpecs()
        {
            return new[]
            {
                File("event_activation_routes.csv", 1, "event_route_id:ID", "special_map_id:ID", "event_id:ID", "mandatory:BOOL", "allowed_sector_types:INT_LIST", "requires_tool:BOOL", "requires_consumable:BOOL", "min_safe_tiles_before_trigger:INT", "return_path_required:BOOL", "trigger_slot_id:ID", "notes:STRING"),
                File("special_map_catalog.csv", 1, "special_map_id:ID", "display_name_ko:STRING", "site_role:ENUM:BOSS|FORGE|CORE_RESOURCE|VILLAGE", "primary_biome_id:ID", "footprint_width_sectors:INT", "footprint_height_sectors:INT", "required_count:INT", "min_graph_distance_from_start:INT", "min_graph_distance_to_other_core_sites:INT", "allowed_entry_route_types:INT_LIST", "requires_tool:BOOL", "mandatory_reward_id:ID", "generation_mode:ENUM:FIXED|GENERATED", "active:BOOL", "notes:STRING"),
                File("special_map_entry_sockets.csv", 2, "special_map_id:ID", "entry_socket_id:ID", "local_sector_x:INT", "local_sector_y:INT", "side:ENUM:L|R|U|D", "allowed_route_types:INT_LIST", "required:BOOL", "return_path_required:BOOL", "notes:STRING"),
                File("special_map_footprint_cells.csv", 3, "special_map_id:ID", "local_sector_x:INT", "local_sector_y:INT", "local_role:ENUM:ENTRY|ARENA|CORE", "required_primary_biome_id:ID", "fixed_sector_recipe_id:ID", "required_open_sides:ENUM_LIST:L|R|U|D", "notes:STRING"),
                File("special_map_rewards.csv", 2, "special_map_id:ID", "reward_order:INT", "reward_id:ID", "reward_kind:ENUM:ITEM", "mandatory:BOOL", "slot_id:ID", "quantity_min:INT", "quantity_max:INT", "notes:STRING"),
                File("shop_archetypes.csv", 1, "shop_archetype_id:ID", "display_name_ko:STRING", "shop_type:ENUM:GENERAL", "item_slot_count_min:INT", "item_slot_count_max:INT", "base_price_multiplier:FLOAT", "allows_reputation_reward:BOOL", "active:BOOL", "notes:STRING"),
                File("shop_inventory_rules.csv", 2, "shop_archetype_id:ID", "slot_index:INT", "spawn_pool_id:ID", "guaranteed:BOOL", "quantity_min:INT", "quantity_max:INT", "price_min_gold:INT", "price_max_gold:INT", "required_favor_tier:INT", "active:BOOL", "notes:STRING"),
                File("shopkeeper_species.csv", 1, "species_id:ID", "display_name_ko:STRING", "prefab_id:ID", "dialogue_style_id:ID", "animation_set_id:ID", "selection_weight:INT", "allowed_biome_ids:ID_LIST", "active:BOOL", "notes:STRING"),
                File("village_facilities.csv", 1, "facility_id:ID", "display_name_ko:STRING", "facility_group:ENUM:SHOP", "fixed:BOOL", "selection_weight:INT", "prefab_id:ID", "shop_archetype_id:ID", "evacuated_prefab_id:ID", "active:BOOL", "notes:STRING"),
                File("village_layout_catalog.csv", 1, "village_layout_id:ID", "display_name_ko:STRING", "footprint_width_sectors:INT", "footprint_height_sectors:INT", "target_facility_count:INT", "entry_sides:ENUM_LIST:L|R|U|D", "selection_weight:INT", "active:BOOL", "notes:STRING"),
                File("village_layout_cells.csv", 3, "village_layout_id:ID", "local_chunk_x:INT", "local_chunk_y:INT", "cell_role:ENUM:CORE", "facility_slot_id:ID", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_entry_side:ENUM:L|R|U|D", "notes:STRING"),
                File("village_profiles.csv", 1, "village_profile_id:ID", "display_name_ko:STRING", "world_profile_id:ID", "facility_count_min:INT", "facility_count_max:INT", "fixed_facility_ids:ID_LIST", "optional_facility_ids:ID_LIST", "allowed_layout_ids:ID_LIST", "start_distance_buckets:STRING", "maximum_sector_count:INT", "active:BOOL", "notes:STRING")
            };
        }

        private static FileSpec File(
            string fileName,
            int primaryKeyCount,
            params string[] definitions)
        {
            return new FileSpec(fileName, primaryKeyCount, definitions.Select(definition =>
            {
                var parts = definition.Split(':');
                var allowed = parts.Length > 2
                    ? parts[2]
                    : (parts[1] == "ENUM" || parts[1] == "ENUM_LIST" ? "ENUM_A|ENUM_B" : string.Empty);
                return new ColumnSpec(parts[0], parts[1], allowed);
            }).ToArray());
        }

        private sealed class MatrixExpectation
        {
            public MatrixExpectation(
                SiteReservationKind kind,
                string sourceId,
                int success,
                int footprintOutside,
                int entryOutside)
            {
                Kind = kind;
                SourceId = sourceId;
                Success = success;
                FootprintOutside = footprintOutside;
                EntryOutside = entryOutside;
            }

            public SiteReservationKind Kind { get; }
            public string SourceId { get; }
            public int Success { get; }
            public int FootprintOutside { get; }
            public int EntryOutside { get; }
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, int primaryKeyCount, IReadOnlyList<ColumnSpec> columns)
            {
                FileName = fileName;
                PrimaryKeyCount = primaryKeyCount;
                Columns = columns;
            }

            public string FileName { get; }
            public int PrimaryKeyCount { get; }
            public IReadOnlyList<ColumnSpec> Columns { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(string name, string dataType, string allowedValues)
            {
                Name = name;
                DataType = dataType;
                AllowedValues = allowedValues;
            }

            public string Name { get; }
            public string DataType { get; }
            public string AllowedValues { get; }
        }
    }
}
