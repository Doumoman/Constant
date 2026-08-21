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
    public sealed class SiteCandidateCostTests
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";
        private const string CraterBiomeId = "BIO_MOON_CRATER";
        private const string CassiaBiomeId = "BIO_CASSIA_ROOT";
        private const string MillBiomeId = "BIO_ABANDONED_MILL";
        private const string DoughBiomeId = "BIO_MOON_DOUGH";

        private static readonly FileSpec[] BiomeSpecs = CreateBiomeSpecs();
        private static readonly FileSpec[] SpecialSpecs = CreateSpecialSpecs();
        private static readonly StarterData Starter = BuildStarterData();

        public static IEnumerable AllSectorCases()
        {
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                yield return new TestCaseData(index).SetName("EdgeRing_Exhaustive_" + index);
        }

        public static IEnumerable ErrorCodeCases()
        {
            foreach (SiteCandidateCostErrorCode code in Enum.GetValues(typeof(SiteCandidateCostErrorCode)))
                yield return new TestCaseData(code, (int)code);
        }

        [Test]
        public void Weights_DefaultsAreExactAndImmutable()
        {
            var weights = SiteCandidateCostWeights.Default;

            Assert.That(weights.AltitudePerSector, Is.EqualTo(10));
            Assert.That(weights.EdgeClearanceDeficit, Is.EqualTo(25));
            Assert.That(weights.DistanceDeficit, Is.EqualTo(1000));
            Assert.That(weights.FutureCoreCapacityShortfall, Is.EqualTo(100));
            Assert.That(weights.CoreCluster, Is.EqualTo(10000));
            Assert.That(PublicSetters(typeof(SiteCandidateCostWeights)), Is.Empty);
        }

        [Test]
        public void Weights_CustomAndMaximumValuesRemainCheckedLongInputs()
        {
            var custom = new SiteCandidateCostWeights(1, 2, 3, 4, 5);
            var maximum = new SiteCandidateCostWeights(
                int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);

            Assert.That(new[] { custom.AltitudePerSector, custom.EdgeClearanceDeficit,
                custom.DistanceDeficit, custom.FutureCoreCapacityShortfall, custom.CoreCluster },
                Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(maximum.CoreCluster, Is.EqualTo(int.MaxValue));
        }

        [TestCase(-1, 0, 0, 0, 0)]
        [TestCase(0, -1, 0, 0, 0)]
        [TestCase(0, 0, -1, 0, 0)]
        [TestCase(0, 0, 0, -1, 0)]
        [TestCase(0, 0, 0, 0, -1)]
        public void Weights_NegativeComponentIsRejected(int altitude, int edge, int distance, int capacity, int cluster)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SiteCandidateCostWeights(altitude, edge, distance, capacity, cluster));
        }

        [TestCaseSource(nameof(ErrorCodeCases))]
        public void ErrorCode_UsesFrozenOrdinalOrder(SiteCandidateCostErrorCode code, int ordinal)
        {
            Assert.That((int)code, Is.EqualTo(ordinal));
            Assert.That(new SiteCandidateCostError(code, string.Empty, string.Empty, -1, "stable").Code,
                Is.EqualTo(code));
        }

        [Test]
        public void ErrorAndResult_ValidateAndExposeReadOnlySortedDeduplicatedState()
        {
            Assert.Throws<ArgumentException>(() => new SiteCandidateCostError(
                SiteCandidateCostErrorCode.InvalidCandidate, "bad", string.Empty, -1, "stable"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteCandidateCostError(
                SiteCandidateCostErrorCode.InvalidCandidate, string.Empty, string.Empty, 169, "stable"));
            Assert.Throws<ArgumentException>(() => new SiteCandidateCostError(
                SiteCandidateCostErrorCode.InvalidCandidate, string.Empty, string.Empty, -1, " "));

            var later = new SiteCandidateCostError(SiteCandidateCostErrorCode.MissingWeights,
                CassiaId, string.Empty, -1, "later");
            var earlier = new SiteCandidateCostError(SiteCandidateCostErrorCode.MissingCandidate,
                string.Empty, string.Empty, -1, "earlier");
            var failure = new SiteCandidateCostResult(null, new[] { later, earlier, earlier });

            Assert.That(failure.Succeeded, Is.False);
            Assert.That(failure.Breakdown, Is.Null);
            Assert.That(failure.Errors.Select(error => error.Code),
                Is.EqualTo(new[] { SiteCandidateCostErrorCode.MissingCandidate,
                    SiteCandidateCostErrorCode.MissingWeights }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SiteCandidateCostError>)failure.Errors).Clear());
        }

        [Test]
        public void Context_RejectsNullInputsAndInvalidCapacityBounds()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SiteCandidateCostContext(null, Array.Empty<FootprintPlacement>(), -1));
            Assert.Throws<ArgumentNullException>(() =>
                new SiteCandidateCostContext(Starter.Policy, null, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SiteCandidateCostContext(Starter.Policy, Array.Empty<FootprintPlacement>(), -2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SiteCandidateCostContext(Starter.Policy, Array.Empty<FootprintPlacement>(), 170));
        }

        [TestCase(-1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(168, true)]
        [TestCase(169, true)]
        public void Context_CapacityRangeAndEstimateFlagAreExact(int count, bool hasEstimate)
        {
            var context = Context(Array.Empty<FootprintPlacement>(), count);

            Assert.That(context.FutureCoreAvailableSectorCount, Is.EqualTo(count));
            Assert.That(context.HasFutureCoreCapacityEstimate, Is.EqualTo(hasEstimate));
        }

        [Test]
        public void Context_CopiesSortsAndProtectsCallerList()
        {
            var start = Single(SiteReservationKind.Start, WorldId, 0, 0);
            var boss = Rectangle(SiteReservationKind.Boss, BossId, 5, 0, 2, 1);
            var caller = new List<FootprintPlacement> { boss, start };
            var context = Context(caller, -1);
            caller.Clear();

            Assert.That(context.ExistingPlacements.Select(p => p.Candidate.Kind),
                Is.EqualTo(new[] { SiteReservationKind.Start, SiteReservationKind.Boss }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<FootprintPlacement>)context.ExistingPlacements).Clear());
        }

        [Test]
        public void Context_RejectsNullDuplicateUnknownAndOverlappingPlacements()
        {
            var start = Single(SiteReservationKind.Start, WorldId, 0, 0);
            var boss = Single(SiteReservationKind.Boss, BossId, 0, 0);
            Assert.Throws<ArgumentException>(() => Context(new FootprintPlacement[] { null }, -1));
            Assert.Throws<ArgumentException>(() => Context(new[] { start, start }, -1));
            Assert.Throws<ArgumentException>(() => Context(new[]
            {
                Single(SiteReservationKind.Start, "WORLD_UNKNOWN", 0, 0)
            }, -1));
            Assert.Throws<ArgumentException>(() => Context(new[] { start, boss }, -1));
        }

        [TestCase(MeteorId, SiteReservationKind.CoreResource, CraterBiomeId, 0, 7, 5, true)]
        [TestCase(CassiaId, SiteReservationKind.CoreResource, CassiaBiomeId, 2, 12, 5, false)]
        [TestCase(YeastId, SiteReservationKind.CoreResource, DoughBiomeId, 0, 7, 5, true)]
        [TestCase(ForgeId, SiteReservationKind.Forge, MillBiomeId, 1, 11, 4, false)]
        [TestCase(BossId, SiteReservationKind.Boss, MillBiomeId, 1, 11, 4, false)]
        public void StarterDefinitionGate_IsExact(
            string sourceId,
            SiteReservationKind expectedKind,
            string biomeId,
            int minimumY,
            int maximumY,
            int minimumCoreSectors,
            bool canTouchEdge)
        {
            var special = Starter.SpecialMaps[sourceId];
            var biome = Starter.Biomes[biomeId];
            var rule = Starter.CoreRules[biomeId];

            Assert.That(SiteReservationTokenCodec.TryParseKind(special.SiteRole, out var kind), Is.True);
            Assert.That(kind, Is.EqualTo(expectedKind));
            Assert.That(special.PrimaryBiomeId, Is.EqualTo(biomeId));
            Assert.That(biome.PreferredAltitudeMinSectorY, Is.EqualTo(minimumY));
            Assert.That(biome.PreferredAltitudeMaxSectorY, Is.EqualTo(maximumY));
            Assert.That(rule.PatchRole, Is.EqualTo("CORE"));
            Assert.That(rule.MinSectorCount, Is.EqualTo(minimumCoreSectors));
            Assert.That(rule.CanTouchWorldEdge, Is.EqualTo(canTouchEdge));
            Assert.That(rule.BufferRingSectors, Is.EqualTo(1));
        }

        [TestCaseSource(nameof(AllSectorCases))]
        public void EdgeRing_IsExhaustiveAcrossAll169Sectors(int sectorIndex)
        {
            var coordinate = WorldGridIndex.ToCoordinate(sectorIndex);
            var result = Calculate(CassiaId,
                Single(SiteReservationKind.CoreResource, CassiaId, coordinate.X, coordinate.Y),
                Array.Empty<FootprintPlacement>(), -1);
            var actualRing = Math.Min(Math.Min(coordinate.X, 12 - coordinate.X),
                Math.Min(coordinate.Y, 12 - coordinate.Y));

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.EdgeUnits, Is.EqualTo(Math.Max(0, 1 - actualRing)));
        }

        [TestCase(MeteorId, 0, 0)]
        [TestCase(MeteorId, 7, 0)]
        [TestCase(MeteorId, 8, 1)]
        [TestCase(MeteorId, 12, 5)]
        [TestCase(CassiaId, 0, 2)]
        [TestCase(CassiaId, 2, 0)]
        [TestCase(CassiaId, 12, 0)]
        [TestCase(ForgeId, 0, 1)]
        [TestCase(ForgeId, 1, 0)]
        [TestCase(ForgeId, 11, 0)]
        [TestCase(ForgeId, 12, 1)]
        [TestCase(YeastId, 0, 0)]
        [TestCase(YeastId, 7, 0)]
        [TestCase(YeastId, 8, 1)]
        public void Altitude_ExactStarterVectors(string sourceId, int y, int expectedUnits)
        {
            var result = Calculate(sourceId, PlacementForSource(sourceId, 6, y),
                Array.Empty<FootprintPlacement>(), sourceId == BossId ? -1 : 5);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.AltitudeUnits, Is.EqualTo(expectedUnits));
            Assert.That(result.Breakdown.AltitudePenalty, Is.EqualTo(expectedUnits * 10L));
        }

        [Test]
        public void Altitude_UsesMaximumOccupiedCellDistanceNotOriginOrAverage()
        {
            var candidate = Sparse(SiteReservationKind.CoreResource, MeteorId, 6, 7, 1, 3,
                new SectorCoord(0, 0), new SectorCoord(0, 1), new SectorCoord(0, 2));
            var result = Calculate(MeteorId, candidate, Array.Empty<FootprintPlacement>(), -1);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.AltitudeUnits, Is.EqualTo(2));
        }

        [TestCase(MeteorId, 0, 0)]
        [TestCase(YeastId, 0, 0)]
        [TestCase(CassiaId, 0, 1)]
        [TestCase(CassiaId, 1, 0)]
        [TestCase(ForgeId, 0, 1)]
        [TestCase(ForgeId, 1, 0)]
        [TestCase(BossId, 0, 1)]
        [TestCase(BossId, 1, 0)]
        public void Edge_ExactStarterVectors(string sourceId, int edgeRing, int expectedUnits)
        {
            var result = Calculate(sourceId, PlacementForSource(sourceId, edgeRing, 6),
                Array.Empty<FootprintPlacement>(), -1);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.EdgeUnits, Is.EqualTo(expectedUnits));
            Assert.That(result.Breakdown.EdgePenalty, Is.EqualTo(expectedUnits * 25L));
        }

        [TestCase(CassiaId, -1, 0, false)]
        [TestCase(CassiaId, 4, 1, true)]
        [TestCase(CassiaId, 5, 0, true)]
        [TestCase(ForgeId, -1, 0, false)]
        [TestCase(ForgeId, 3, 1, true)]
        [TestCase(ForgeId, 4, 0, true)]
        public void Capacity_CoreAndForgeExactVectors(
            string sourceId, int available, int expectedUnits, bool expectedEstimate)
        {
            var result = Calculate(sourceId, PlacementForSource(sourceId, 6, 6),
                Array.Empty<FootprintPlacement>(), available);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.FutureCoreCapacityUnits, Is.EqualTo(expectedUnits));
            Assert.That(result.Breakdown.HasFutureCoreCapacityEstimate, Is.EqualTo(expectedEstimate));
            Assert.That(result.Breakdown.FutureCoreCapacityPenalty, Is.EqualTo(expectedUnits * 100L));
        }

        [Test]
        public void Capacity_BossAndStartRejectProvidedEstimate()
        {
            var boss = Calculate(BossId, PlacementForSource(BossId, 6, 6),
                Array.Empty<FootprintPlacement>(), 4);
            var start = new SiteCandidateCostCalculator().Calculate(
                Single(SiteReservationKind.Start, WorldId, 6, 6), Context(Array.Empty<FootprintPlacement>(), 0),
                null, null, null, SiteCandidateCostWeights.Default);

            AssertFailure(boss, SiteCandidateCostErrorCode.InvalidFutureCapacityEstimate);
            AssertFailure(start, SiteCandidateCostErrorCode.InvalidFutureCapacityEstimate);
        }

        [TestCase(3, 4, 1, 1)]
        [TestCase(4, 4, 0, 0)]
        [TestCase(5, 4, 0, 0)]
        public void Distance_UsesExactMinimumConstraintDeficit(
            int actualDistance, int requiredDistance, int expectedUnits, int expectedViolations)
        {
            Assert.That(requiredDistance, Is.EqualTo(4));
            var start = Single(SiteReservationKind.Start, WorldId, 0, 2);
            var candidate = Single(SiteReservationKind.CoreResource, CassiaId, actualDistance, 2);
            var result = Calculate(CassiaId, candidate, new[] { start }, -1);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.DistanceConstraintCountChecked, Is.EqualTo(1));
            Assert.That(result.Breakdown.DistanceUnits, Is.EqualTo(expectedUnits));
            Assert.That(result.Breakdown.DistanceViolationCount, Is.EqualTo(expectedViolations));
        }

        [Test]
        public void Distance_SumsMultipleApplicableDeficitsUsingFootprintCells()
        {
            var start = Rectangle(SiteReservationKind.Start, WorldId, 0, 2, 2, 1);
            var boss = Rectangle(SiteReservationKind.Boss, BossId, 8, 2, 2, 1);
            var candidate = Rectangle(SiteReservationKind.CoreResource, CassiaId, 3, 2, 2, 1);
            var result = Calculate(CassiaId, candidate, new[] { boss, start }, -1);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.DistanceConstraintCountChecked, Is.EqualTo(2));
            Assert.That(result.Breakdown.DistanceUnits, Is.EqualTo(2));
            Assert.That(result.Breakdown.DistanceViolationCount, Is.EqualTo(1));
        }

        [Test]
        public void CandidateMissingPolicyKeyAndOverlapReturnNoPartialBreakdown()
        {
            var unknownStart = new SiteCandidateCostCalculator().Calculate(
                Single(SiteReservationKind.Start, "WORLD_UNKNOWN", 1, 1),
                Context(Array.Empty<FootprintPlacement>(), -1), null, null, null,
                SiteCandidateCostWeights.Default);
            var existing = Single(SiteReservationKind.Start, WorldId, 3, 2);
            var overlap = Calculate(CassiaId,
                Single(SiteReservationKind.CoreResource, CassiaId, 3, 2), new[] { existing }, -1);

            AssertFailure(unknownStart, SiteCandidateCostErrorCode.MissingPolicyKey);
            AssertFailure(overlap, SiteCandidateCostErrorCode.OverlappingPlacement);
            Assert.That(unknownStart.Breakdown, Is.Null);
            Assert.That(overlap.Breakdown, Is.Null);
        }

        [Test]
        public void Cluster_ExactThreeCoreFourByFourIsHardSignal()
        {
            var existing = new[]
            {
                Single(SiteReservationKind.CoreResource, CassiaId, 0, 0),
                Single(SiteReservationKind.CoreResource, YeastId, 3, 3)
            };
            var result = Calculate(MeteorId,
                Single(SiteReservationKind.CoreResource, MeteorId, 1, 2), existing, -1);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.CoreWindowWidth, Is.EqualTo(4));
            Assert.That(result.Breakdown.CoreWindowHeight, Is.EqualTo(4));
            Assert.That(result.Breakdown.CoreClusterDetected, Is.True);
            Assert.That(result.Breakdown.CoreClusterUnits, Is.EqualTo(1));
            Assert.That(result.Breakdown.HardConstraintsSatisfied, Is.False);
        }

        [Test]
        public void Cluster_ExactThreeCoreFiveByFourPassesClusterGate()
        {
            var existing = new[]
            {
                Single(SiteReservationKind.CoreResource, CassiaId, 0, 0),
                Single(SiteReservationKind.CoreResource, YeastId, 4, 3)
            };
            var result = Calculate(MeteorId,
                Single(SiteReservationKind.CoreResource, MeteorId, 2, 2), existing, -1);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.CoreWindowWidth, Is.EqualTo(5));
            Assert.That(result.Breakdown.CoreWindowHeight, Is.EqualTo(4));
            Assert.That(result.Breakdown.CoreClusterDetected, Is.False);
            Assert.That(result.Breakdown.CoreClusterUnits, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Cluster_FewerThanThreeCorePlacementsHasUnavailableWindow(int existingCoreCount)
        {
            var existing = existingCoreCount == 0
                ? Array.Empty<FootprintPlacement>()
                : new[] { Single(SiteReservationKind.CoreResource, CassiaId, 0, 0) };
            var result = Calculate(MeteorId,
                Single(SiteReservationKind.CoreResource, MeteorId, 6, 6), existing, -1);

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Breakdown.CoreClusterUnits, Is.Zero);
            Assert.That(result.Breakdown.CoreWindowWidth, Is.EqualTo(-1));
            Assert.That(result.Breakdown.CoreWindowHeight, Is.EqualTo(-1));
        }

        [Test]
        public void Aggregate_DefaultWeightsMatchesExact11145Vector()
        {
            var existing = new[]
            {
                Single(SiteReservationKind.Start, WorldId, 3, 0),
                Single(SiteReservationKind.CoreResource, YeastId, 3, 3),
                Single(SiteReservationKind.CoreResource, MeteorId, 3, 1)
            };
            var result = Calculate(CassiaId,
                Single(SiteReservationKind.CoreResource, CassiaId, 0, 0), existing, 4);
            var breakdown = result.Breakdown;

            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(new[] { breakdown.AltitudeUnits, breakdown.EdgeUnits,
                breakdown.DistanceUnits, breakdown.FutureCoreCapacityUnits,
                breakdown.CoreClusterUnits }, Is.EqualTo(new[] { 2, 1, 1, 1, 1 }));
            Assert.That(breakdown.AltitudePenalty, Is.EqualTo(20));
            Assert.That(breakdown.EdgePenalty, Is.EqualTo(25));
            Assert.That(breakdown.DistanceConstraintPenalty, Is.EqualTo(1000));
            Assert.That(breakdown.FutureCoreCapacityPenalty, Is.EqualTo(100));
            Assert.That(breakdown.QuadrantClusteringPenalty, Is.EqualTo(10000));
            Assert.That(breakdown.TotalCost, Is.EqualTo(11145));
            Assert.That(breakdown.HardConstraintsSatisfied, Is.False);
        }

        [Test]
        public void SoftCostsDoNotFlipHardSignalAndDistanceDoes()
        {
            var softOnly = Calculate(CassiaId,
                Single(SiteReservationKind.CoreResource, CassiaId, 0, 0),
                Array.Empty<FootprintPlacement>(), 0);
            var distance = Calculate(CassiaId,
                Single(SiteReservationKind.CoreResource, CassiaId, 3, 2),
                new[] { Single(SiteReservationKind.Start, WorldId, 0, 2) }, -1);

            Assert.That(softOnly.Breakdown.TotalCost, Is.GreaterThan(0));
            Assert.That(softOnly.Breakdown.HardConstraintsSatisfied, Is.True);
            Assert.That(distance.Breakdown.DistanceUnits, Is.EqualTo(1));
            Assert.That(distance.Breakdown.HardConstraintsSatisfied, Is.False);
        }

        [Test]
        public void Start_RequiresNullTypedInputsAndPublishesOnlyDistanceComponents()
        {
            var candidate = Single(SiteReservationKind.Start, WorldId, 0, 0);
            var success = new SiteCandidateCostCalculator().Calculate(candidate,
                Context(new[] { Single(SiteReservationKind.Boss, BossId, 4, 0) }, -1),
                null, null, null, SiteCandidateCostWeights.Default);
            var failure = new SiteCandidateCostCalculator().Calculate(candidate,
                Context(Array.Empty<FootprintPlacement>(), -1),
                Starter.SpecialMaps[BossId], Starter.Biomes[MillBiomeId],
                Starter.CoreRules[MillBiomeId], SiteCandidateCostWeights.Default);

            Assert.That(success.Succeeded, Is.True, FormatErrors(success));
            Assert.That(success.Breakdown.AltitudeUnits, Is.Zero);
            Assert.That(success.Breakdown.EdgeUnits, Is.Zero);
            Assert.That(success.Breakdown.FutureCoreCapacityUnits, Is.Zero);
            Assert.That(success.Breakdown.CoreClusterUnits, Is.Zero);
            Assert.That(success.Breakdown.DistanceConstraintCountChecked, Is.EqualTo(1));
            AssertFailure(failure, SiteCandidateCostErrorCode.UnexpectedSpecialMap);
            AssertFailure(failure, SiteCandidateCostErrorCode.UnexpectedPrimaryBiome);
            AssertFailure(failure, SiteCandidateCostErrorCode.UnexpectedCorePatchRule);
        }

        [Test]
        public void NullRequiredInputsFailWithoutPartialCost()
        {
            var candidate = Single(SiteReservationKind.CoreResource, CassiaId, 6, 6);
            var calculator = new SiteCandidateCostCalculator();

            AssertFailure(calculator.Calculate(null, Context(Array.Empty<FootprintPlacement>(), -1),
                null, null, null, SiteCandidateCostWeights.Default),
                SiteCandidateCostErrorCode.MissingCandidate);
            AssertFailure(calculator.Calculate(candidate, null,
                Starter.SpecialMaps[CassiaId], Starter.Biomes[CassiaBiomeId],
                Starter.CoreRules[CassiaBiomeId], SiteCandidateCostWeights.Default),
                SiteCandidateCostErrorCode.MissingContext);
            AssertFailure(calculator.Calculate(candidate, Context(Array.Empty<FootprintPlacement>(), -1),
                Starter.SpecialMaps[CassiaId], Starter.Biomes[CassiaBiomeId],
                Starter.CoreRules[CassiaBiomeId], null), SiteCandidateCostErrorCode.MissingWeights);
            var typed = calculator.Calculate(candidate, Context(Array.Empty<FootprintPlacement>(), -1),
                null, null, null, SiteCandidateCostWeights.Default);
            AssertFailure(typed, SiteCandidateCostErrorCode.MissingSpecialMap);
            AssertFailure(typed, SiteCandidateCostErrorCode.MissingPrimaryBiome);
            AssertFailure(typed, SiteCandidateCostErrorCode.MissingCorePatchRule);
            Assert.That(typed.Breakdown, Is.Null);
        }

        [Test]
        public void TypedIdentityMismatchesAreRejected()
        {
            var candidate = Single(SiteReservationKind.CoreResource, CassiaId, 6, 6);
            var context = Context(Array.Empty<FootprintPlacement>(), -1);
            var calculator = new SiteCandidateCostCalculator();

            var wrongMap = calculator.Calculate(candidate, context,
                Starter.SpecialMaps[MeteorId], Starter.Biomes[CassiaBiomeId],
                Starter.CoreRules[CassiaBiomeId], SiteCandidateCostWeights.Default);
            var wrongBiome = calculator.Calculate(candidate, context,
                Starter.SpecialMaps[CassiaId], Starter.Biomes[CraterBiomeId],
                Starter.CoreRules[CraterBiomeId], SiteCandidateCostWeights.Default);
            var wrongRule = calculator.Calculate(candidate, context,
                Starter.SpecialMaps[CassiaId], Starter.Biomes[CassiaBiomeId],
                Starter.CoreRules[CraterBiomeId], SiteCandidateCostWeights.Default);

            AssertFailure(wrongMap, SiteCandidateCostErrorCode.SourceIdentityMismatch);
            AssertFailure(wrongBiome, SiteCandidateCostErrorCode.SourceIdentityMismatch);
            AssertFailure(wrongRule, SiteCandidateCostErrorCode.SourceIdentityMismatch);
        }

        [Test]
        public void VillageCandidateIsRejected()
        {
            var result = new SiteCandidateCostCalculator().Calculate(
                Single(SiteReservationKind.Village, "SITE_VILLAGE_SAMPLE", 6, 6),
                Context(Array.Empty<FootprintPlacement>(), -1), null, null, null,
                SiteCandidateCostWeights.Default);

            AssertFailure(result, SiteCandidateCostErrorCode.InvalidCandidate);
        }

        [Test]
        public void ExistingOrderAndCollectionImplementationProduceIdenticalBreakdown()
        {
            var start = Single(SiteReservationKind.Start, WorldId, 0, 2);
            var boss = Single(SiteReservationKind.Boss, BossId, 10, 2);
            var candidate = Single(SiteReservationKind.CoreResource, CassiaId, 5, 2);
            var arrayResult = Calculate(CassiaId, candidate, new[] { start, boss }, 5);
            var listResult = Calculate(CassiaId, candidate,
                new List<FootprintPlacement> { boss, start }, 5);

            Assert.That(Snapshot(listResult), Is.EqualTo(Snapshot(arrayResult)));
        }

        [Test]
        public void CandidateOrdinalAndTransformPreserveEvidenceButNotCostComponents()
        {
            var first = Sparse(SiteReservationKind.CoreResource, CassiaId, 6, 6, 1, 1,
                0, SiteFootprintTransform.R0, new SectorCoord(0, 0));
            var second = Sparse(SiteReservationKind.CoreResource, CassiaId, 6, 6, 1, 1,
                99, SiteFootprintTransform.R180, new SectorCoord(0, 0));
            var firstResult = Calculate(CassiaId, first, Array.Empty<FootprintPlacement>(), 5);
            var secondResult = Calculate(CassiaId, second, Array.Empty<FootprintPlacement>(), 5);

            Assert.That(firstResult.Succeeded, Is.True, FormatErrors(firstResult));
            Assert.That(secondResult.Succeeded, Is.True, FormatErrors(secondResult));
            Assert.That(CostSnapshot(secondResult.Breakdown), Is.EqualTo(CostSnapshot(firstResult.Breakdown)));
            Assert.That(firstResult.Breakdown.Transform, Is.EqualTo(SiteFootprintTransform.R0));
            Assert.That(secondResult.Breakdown.Transform, Is.EqualTo(SiteFootprintTransform.R180));
        }

        [TestCase(0UL)]
        [TestCase(4660UL)]
        [TestCase(ulong.MaxValue)]
        public void SeedsAndRepeatedFreshOrReusedCalculatorsDoNotAffectCost(ulong unusedSeed)
        {
            var candidate = Single(SiteReservationKind.CoreResource, CassiaId, 6, 6,
                unchecked((int)(unusedSeed & 65535)));
            var context = Context(Array.Empty<FootprintPlacement>(), 5);
            var expected = Calculate(CassiaId, candidate, Array.Empty<FootprintPlacement>(), 5);
            var reused = new SiteCandidateCostCalculator();
            for (var iteration = 0; iteration < 100; iteration++)
            {
                var actual = reused.Calculate(candidate, context, Starter.SpecialMaps[CassiaId],
                    Starter.Biomes[CassiaBiomeId], Starter.CoreRules[CassiaBiomeId],
                    SiteCandidateCostWeights.Default);
                Assert.That(Snapshot(actual), Is.EqualTo(Snapshot(expected)));
                var fresh = Calculate(CassiaId, candidate, Array.Empty<FootprintPlacement>(), 5);
                Assert.That(Snapshot(fresh), Is.EqualTo(Snapshot(expected)));
            }
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void CultureDoesNotAffectObservableResult(string cultureName)
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                var result = Calculate(CassiaId,
                    Single(SiteReservationKind.CoreResource, CassiaId, 6, 6),
                    Array.Empty<FootprintPlacement>(), 5);
                Assert.That(Snapshot(result), Is.EqualTo(
                    "1|SITE_CASSIA_SAP_HEART|84|R0|0|0|0|0|0|0|1|5|5|0|-1|-1|1|0"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void PublicMutationSurfaceHasNoSettersOrFieldsAndCollectionsAreReadOnly()
        {
            foreach (var type in new[]
            {
                typeof(SiteCandidateCostWeights), typeof(SiteCandidateCostContext),
                typeof(SiteCandidateCostBreakdown), typeof(SiteCandidateCostError),
                typeof(SiteCandidateCostResult), typeof(SiteCandidateCostCalculator)
            })
            {
                Assert.That(PublicSetters(type), Is.Empty, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.Static), Is.Empty, type.FullName);
            }
        }

        [Test]
        public void ProductionTypesHaveNoForbiddenSubsystemDependencies()
        {
            var forbidden = new[] { "Rng", "Random", "Rank", "Select", "Backtrack", "Flood",
                "Village", "Pass", "Route", "File", "Stream", "Serializer" };
            var types = new[] { typeof(SiteCandidateCostWeights), typeof(SiteCandidateCostContext),
                typeof(SiteCandidateCostBreakdown), typeof(SiteCandidateCostError),
                typeof(SiteCandidateCostResult), typeof(SiteCandidateCostCalculator) };

            foreach (var type in types)
            {
                foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.DeclaredOnly |
                                                       BindingFlags.Instance | BindingFlags.Static))
                {
                    Assert.That(forbidden.Any(token => member.Name.IndexOf(token,
                        StringComparison.OrdinalIgnoreCase) >= 0), Is.False,
                        type.Name + "." + member.Name);
                }
            }
        }

        private static SiteCandidateCostResult Calculate(
            string sourceId,
            FootprintPlacement candidate,
            IEnumerable<FootprintPlacement> existing,
            int futureCapacity)
        {
            var special = Starter.SpecialMaps[sourceId];
            var biome = Starter.Biomes[special.PrimaryBiomeId];
            var rule = Starter.CoreRules[biome.BiomeId];
            return new SiteCandidateCostCalculator().Calculate(candidate,
                Context(existing, futureCapacity), special, biome, rule,
                SiteCandidateCostWeights.Default);
        }

        private static SiteCandidateCostContext Context(
            IEnumerable<FootprintPlacement> existing,
            int futureCapacity) => new SiteCandidateCostContext(Starter.Policy, existing, futureCapacity);

        private static FootprintPlacement PlacementForSource(string sourceId, int x, int y)
        {
            var kind = sourceId == BossId ? SiteReservationKind.Boss :
                sourceId == ForgeId ? SiteReservationKind.Forge : SiteReservationKind.CoreResource;
            return Single(kind, sourceId, x, y);
        }

        private static FootprintPlacement Single(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int candidateOrdinal = 0,
            SiteFootprintTransform transform = SiteFootprintTransform.R0) =>
            Sparse(kind, sourceId, x, y, 1, 1, candidateOrdinal, transform,
                new SectorCoord(0, 0));

        private static FootprintPlacement Rectangle(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int width,
            int height)
        {
            var cells = new List<SectorCoord>();
            for (var localY = 0; localY < height; localY++)
                for (var localX = 0; localX < width; localX++)
                    cells.Add(new SectorCoord(localX, localY));
            return Sparse(kind, sourceId, x, y, width, height, cells.ToArray());
        }

        private static FootprintPlacement Sparse(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int width,
            int height,
            params SectorCoord[] localCells) =>
            Sparse(kind, sourceId, x, y, width, height, 0, SiteFootprintTransform.R0, localCells);

        private static FootprintPlacement Sparse(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int width,
            int height,
            int candidateOrdinal,
            SiteFootprintTransform transform,
            params SectorCoord[] localCells)
        {
            var origin = new SectorCoord(x, y);
            var candidate = new SiteOriginCandidate(kind, sourceId, 0, origin,
                WorldGridIndex.ToIndex(origin), EdgeRing(origin), candidateOrdinal);
            var footprint = new SiteFootprint(width, height, transform, localCells.Select(cell =>
                new SiteFootprintCell(cell.X, cell.Y, "CELL", string.Empty, string.Empty,
                    Array.Empty<SiteEntrySide>())));
            var occupied = localCells.Select(cell => new SectorCoord(x + cell.X, y + cell.Y));
            return new FootprintPlacement(candidate, footprint, occupied,
                Array.Empty<FootprintPlacementEntry>());
        }

        private static int EdgeRing(SectorCoord origin) => Math.Min(
            Math.Min(origin.X, WorldGenConstants.SectorColumns - 1 - origin.X),
            Math.Min(origin.Y, WorldGenConstants.SectorRows - 1 - origin.Y));

        private static void AssertFailure(
            SiteCandidateCostResult result,
            SiteCandidateCostErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Breakdown, Is.Null);
            Assert.That(result.Errors.Any(error => error.Code == code), Is.True, FormatErrors(result));
        }

        private static string Snapshot(SiteCandidateCostResult result)
        {
            if (!result.Succeeded)
                return "0|" + string.Join(",", result.Errors.Select(error =>
                    error.Code + ":" + error.CandidateSourceDefinitionId + ":" +
                    error.ExistingSourceDefinitionId + ":" + error.SectorIndex + ":" + error.Message));
            var value = result.Breakdown;
            return "1|" + value.CandidateKey.SourceDefinitionId + "|" + value.CandidateOriginIndex +
                   "|" + value.Transform + "|" + value.AltitudeUnits + "|" + value.EdgeUnits +
                   "|" + value.DistanceUnits + "|" + value.DistanceConstraintCountChecked +
                   "|" + value.DistanceViolationCount + "|" + value.FutureCoreCapacityUnits +
                   "|" + (value.HasFutureCoreCapacityEstimate ? 1 : 0) + "|" +
                   value.RequiredCoreSectorCount + "|" + value.FutureCoreAvailableSectorCount +
                   "|" + value.CoreClusterUnits + "|" + value.CoreWindowWidth + "|" +
                   value.CoreWindowHeight + "|" + (value.HardConstraintsSatisfied ? 1 : 0) +
                   "|" + value.TotalCost;
        }

        private static string CostSnapshot(SiteCandidateCostBreakdown value) =>
            value.AltitudeUnits + "|" + value.EdgeUnits + "|" + value.DistanceUnits + "|" +
            value.FutureCoreCapacityUnits + "|" + value.CoreClusterUnits + "|" + value.TotalCost;

        private static IEnumerable<PropertyInfo> PublicSetters(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(property => property.SetMethod != null && property.SetMethod.IsPublic);

        private static string FormatErrors(SiteCandidateCostResult result) =>
            string.Join("\n", result.Errors.Select(error => error.Code + ":" + error.Message));

        private static StarterData BuildStarterData()
        {
            var biomeRows = new List<string[]>
            {
                BiomeRow(CraterBiomeId, 0, 7),
                BiomeRow(CassiaBiomeId, 2, 12),
                BiomeRow(MillBiomeId, 1, 11),
                BiomeRow(DoughBiomeId, 0, 7)
            };
            var patchRows = new List<string[]>
            {
                PatchRow(CraterBiomeId, 5, true),
                PatchRow(CassiaBiomeId, 5, false),
                PatchRow(MillBiomeId, 4, false),
                PatchRow(DoughBiomeId, 5, true)
            };
            var biomeSources = BiomeSpecs.Select(spec => BuildBiomeSource(spec,
                spec.FileName == "biome_types.csv" ? biomeRows :
                spec.FileName == "biome_patch_rules.csv" ? patchRows : null));
            var biomeResult = new BiomeBoundaryDefinitionBuilder().Build(biomeSources);
            if (!biomeResult.Success)
                throw new InvalidOperationException(string.Join("\n", biomeResult.Errors));

            var specialRows = new List<string[]>
            {
                SpecialRow(BossId, "BOSS", MillBiomeId, 2, 1),
                SpecialRow(ForgeId, "FORGE", MillBiomeId, 1, 1),
                SpecialRow(CassiaId, "CORE_RESOURCE", CassiaBiomeId, 1, 1),
                SpecialRow(YeastId, "CORE_RESOURCE", DoughBiomeId, 1, 1),
                SpecialRow(MeteorId, "CORE_RESOURCE", CraterBiomeId, 1, 1)
            };
            var specialSources = SpecialSpecs.Select(spec => BuildSpecialSource(spec,
                spec.FileName == "special_map_catalog.csv" ? specialRows : null));
            var specialResult = new SpecialVillageDefinitionBuilder().Build(specialSources);
            if (!specialResult.Success)
                throw new InvalidOperationException(string.Join("\n", specialResult.Errors));

            var policyResult = new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(
                WorldId, specialResult.DefinitionSet.SpecialMaps.Values);
            if (!policyResult.Succeeded)
                throw new InvalidOperationException(string.Join("\n", policyResult.Errors));

            var coreRules = biomeResult.DefinitionSet.BiomePatchRules.Values
                .ToDictionary(rule => rule.BiomeId, StringComparer.Ordinal);
            return new StarterData(biomeResult.DefinitionSet.BiomeTypes,
                coreRules, specialResult.DefinitionSet.SpecialMaps, policyResult.Policy);
        }

        private static string[] BiomeRow(string biomeId, int minimumY, int maximumY) => new[]
        {
            biomeId, "Biome", "STAGE_MOON", "1", "1", "169", "1",
            minimumY.ToString(CultureInfo.InvariantCulture),
            maximumY.ToString(CultureInfo.InvariantCulture), "1.0",
            "THEME_MOON", "AUDIO_MOON", "MICRO_MOON", "RECIPE_MOON",
            "RESOURCE_MOON", "ELEMENT_MOON", "SITE_REQUIRED", "1", "test"
        };

        private static string[] PatchRow(string biomeId, int minimumSectors, bool canTouchEdge) => new[]
        {
            "RULE_CORE_" + biomeId, biomeId, "CORE",
            minimumSectors.ToString(CultureInfo.InvariantCulture), "169", "1", "1", "1", "1.0",
            canTouchEdge ? "1" : "0", "1", "1", "1.0", "1.0", "1.0", "1.0",
            "1.0", "1.0", "1", "test"
        };

        private static string[] SpecialRow(
            string sourceId, string role, string biomeId, int width, int height) => new[]
        {
            sourceId, "Site", role, biomeId,
            width.ToString(CultureInfo.InvariantCulture), height.ToString(CultureInfo.InvariantCulture),
            "1", "4", "4", "1|2|3", "0", "REWARD_NONE", "FIXED", "1", "test"
        };

        private static BiomeBoundaryDefinitionSource BuildBiomeSource(
            FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var schemaRows = SchemaRows(spec);
            var catalog = new CsvSchemaCatalogBuilder().Build(schemaRows);
            if (!catalog.Success) throw new InvalidOperationException(string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var sourceRows = rows ?? new[] { StandardRow(spec) };
            var csv = BuildCsv(spec, sourceRows);
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            if (!validation.Success) throw new InvalidOperationException(string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            if (!keys.Success) throw new InvalidOperationException("Primary-key index build failed.");
            var result = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            return new BiomeBoundaryDefinitionSource(schema, result);
        }

        private static SpecialVillageDefinitionSource BuildSpecialSource(
            FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var schemaRows = SchemaRows(spec);
            var catalog = new CsvSchemaCatalogBuilder().Build(schemaRows);
            if (!catalog.Success) throw new InvalidOperationException(string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var sourceRows = rows ?? new[] { StandardRow(spec) };
            var csv = BuildCsv(spec, sourceRows);
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            if (!validation.Success) throw new InvalidOperationException(string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            if (!keys.Success) throw new InvalidOperationException("Primary-key index build failed.");
            var result = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            return new SpecialVillageDefinitionSource(schema, result);
        }

        private static IEnumerable<CsvSchemaDictionaryRow> SchemaRows(FileSpec spec)
        {
            return spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName,
                (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name,
                column.DataType,
                index < spec.PrimaryKeyCount ? "1" : "0",
                index < spec.PrimaryKeyCount ? (index + 1).ToString(CultureInfo.InvariantCulture) : string.Empty,
                string.Empty,
                column.AllowedValues,
                string.Empty,
                string.Empty,
                index + 2));
        }

        private static string BuildCsv(FileSpec spec, IEnumerable<string[]> sourceRows)
        {
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            foreach (var row in sourceRows) csv += "\n" + string.Join(",", row.Select(CsvCell));
            return csv;
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

        private static string CsvCell(string value) =>
            value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static FileSpec[] CreateBiomeSpecs() => new[]
        {
            File("biome_types.csv", 1, "biome_id:ID", "display_name_ko:STRING", "stage_id:ID", "required:BOOL", "min_patch_count:INT", "max_patch_count:INT", "min_core_patch_count:INT", "preferred_altitude_min_sector_y:INT", "preferred_altitude_max_sector_y:INT", "growth_weight:FLOAT", "tile_theme_id:ID", "audio_profile_id:ID", "microchunk_pool_prefix:ID", "sector_recipe_pool_prefix:ID", "common_resource_pool_id:ID", "map_element_pool_id:ID", "required_special_map_ids:ID_LIST", "active:BOOL", "notes:STRING"),
            File("biome_patch_rules.csv", 1, "patch_rule_id:ID", "biome_id:ID", "patch_role:ENUM:CORE|OUTER", "min_sector_count:INT", "max_sector_count:INT", "min_seed_distance:INT", "seed_count_min:INT", "seed_count_max:INT", "seed_weight:FLOAT", "can_touch_world_edge:BOOL", "buffer_ring_sectors:INT", "allow_single_sector:BOOL", "max_world_share:FLOAT", "distance_weight:FLOAT", "altitude_weight:FLOAT", "noise_weight:FLOAT", "compactness_weight:FLOAT", "branchiness_target:FLOAT", "active:BOOL", "notes:STRING"),
            File("biome_boundary_profiles.csv", 1, "boundary_profile_id:ID", "display_name_ko:STRING", "boundary_type:ENUM:WALL", "allowed_orientations:ENUM_LIST:L|R|U|D", "width_microchunks_min:INT", "width_microchunks_max:INT", "warning_microchunks_min:INT", "mandatory_route_allowed:BOOL", "tool_requirement:ENUM:NONE", "hard_border:BOOL", "active:BOOL", "notes:STRING"),
            File("biome_boundary_pair_rules.csv", 1, "boundary_pair_rule_id:ID", "biome_a_id:ID", "biome_b_id:ID", "allowed_boundary_profile_ids:ID_LIST", "boundary_profile_weights:INT_LIST", "default_boundary_profile_id:ID", "transition_resource_pool_id:ID", "transition_element_pool_id:ID", "min_shared_edge_count:INT", "active:BOOL", "notes:STRING"),
            File("boundary_chunk_catalog.csv", 1, "boundary_chunk_id:ID", "microchunk_id:ID", "biome_a_id:ID", "biome_b_id:ID", "boundary_profile_id:ID", "orientation:ENUM:L|R|U|D", "route_type:INT", "entry_edge_signature_id:ID", "exit_edge_signature_id:ID", "weight:INT", "reversible:BOOL", "active:BOOL", "notes:STRING")
        };

        private static FileSpec[] CreateSpecialSpecs() => new[]
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

        private static FileSpec File(string fileName, int primaryKeyCount, params string[] definitions)
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

        private sealed class StarterData
        {
            public StarterData(
                IReadOnlyDictionary<string, BiomeTypeDefinition> biomes,
                IReadOnlyDictionary<string, BiomePatchRuleDefinition> coreRules,
                IReadOnlyDictionary<string, SpecialMapDefinition> specialMaps,
                SiteDistancePolicy policy)
            {
                Biomes = biomes;
                CoreRules = coreRules;
                SpecialMaps = specialMaps;
                Policy = policy;
            }

            public IReadOnlyDictionary<string, BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyDictionary<string, BiomePatchRuleDefinition> CoreRules { get; }
            public IReadOnlyDictionary<string, SpecialMapDefinition> SpecialMaps { get; }
            public SiteDistancePolicy Policy { get; }
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
