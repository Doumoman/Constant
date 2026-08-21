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
    public sealed class SiteDistanceIndexTests
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";

        private static readonly FileSpec[] SpecialSpecs = CreateSpecialSpecs();

        public static IEnumerable SectorOriginCases()
        {
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                yield return new TestCaseData(index).SetName("SectorDistance_Origin_" + index);
        }

        public static IEnumerable ErrorCodeCases()
        {
            foreach (SiteDistanceErrorCode code in Enum.GetValues(typeof(SiteDistanceErrorCode)))
                yield return new TestCaseData(code, (int)code);
        }

        [TestCaseSource(nameof(SectorOriginCases))]
        public void SectorDistance_ExhaustiveManhattanSymmetryAndIdentity(int firstIndex)
        {
            var first = WorldGridIndex.ToCoordinate(firstIndex);
            for (var secondIndex = 0; secondIndex < WorldGenConstants.SectorCount; secondIndex++)
            {
                var second = WorldGridIndex.ToCoordinate(secondIndex);
                var expected = Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
                if (firstIndex == secondIndex)
                {
                    var self = Build(Single(SiteReservationKind.Start, "WORLD_A", first.X, first.Y));
                    var key = self.Keys.Single();
                    Assert.That(self.TryGetDistance(key, key, out var distance), Is.True);
                    Assert.That(distance, Is.Zero);
                    Assert.That(self.TryGetRecord(key, key, out var record), Is.False);
                    Assert.That(record, Is.Null);
                    continue;
                }

                var index = Build(
                    Single(SiteReservationKind.Start, "WORLD_A", first.X, first.Y),
                    Single(SiteReservationKind.Boss, "SITE_B", second.X, second.Y));
                var firstKey = new SitePlacementKey(SiteReservationKind.Start, "WORLD_A", 0);
                var secondKey = new SitePlacementKey(SiteReservationKind.Boss, "SITE_B", 0);
                Assert.That(index.TryGetDistance(firstKey, secondKey, out var forward), Is.True);
                Assert.That(index.TryGetDistance(secondKey, firstKey, out var reverse), Is.True);
                Assert.That(forward, Is.EqualTo(expected));
                Assert.That(reverse, Is.EqualTo(expected));
            }
        }

        [TestCaseSource(nameof(ErrorCodeCases))]
        public void ErrorCode_UsesFrozenOrdinalOrder(SiteDistanceErrorCode code, int ordinal)
        {
            Assert.That((int)code, Is.EqualTo(ordinal));
            Assert.That(new SiteDistanceError(code, string.Empty, string.Empty, -1, "stable").Code,
                Is.EqualTo(code));
        }

        [TestCase(SiteReservationKind.Start, 0)]
        [TestCase(SiteReservationKind.Boss, 10)]
        [TestCase(SiteReservationKind.Forge, 20)]
        [TestCase(SiteReservationKind.CoreResource, 30)]
        [TestCase(SiteReservationKind.Village, 40)]
        public void PlacementKey_UsesExactPriorities(SiteReservationKind kind, int expected)
        {
            Assert.That(new SitePlacementKey(kind, "SITE_A", 0).PlacementPriority, Is.EqualTo(expected));
        }

        [Test]
        public void PlacementKey_DefaultInvalidAndConstructorGate()
        {
            Assert.That(default(SitePlacementKey).IsValid, Is.False);
            Assert.That(default(SitePlacementKey).SourceDefinitionId, Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SitePlacementKey((SiteReservationKind)99, "SITE_A", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SitePlacementKey(SiteReservationKind.Start, "SITE_A", -1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("site")]
        [TestCase("Site_A")]
        [TestCase("SITE-A")]
        [TestCase("SITE A")]
        [TestCase("SITE.1")]
        public void PlacementKey_RejectsNonCanonicalSources(string source)
        {
            Assert.Catch<ArgumentException>(() =>
                new SitePlacementKey(SiteReservationKind.Start, source, 0));
        }

        [Test]
        public void PlacementKey_EqualityHashAndCanonicalOrderAreStable()
        {
            var values = new[]
            {
                new SitePlacementKey(SiteReservationKind.CoreResource, "SITE_Z", 0),
                new SitePlacementKey(SiteReservationKind.Forge, "SITE_F", 0),
                new SitePlacementKey(SiteReservationKind.Boss, "SITE_B", 0),
                new SitePlacementKey(SiteReservationKind.Start, "WORLD_A", 0),
                new SitePlacementKey(SiteReservationKind.CoreResource, "SITE_A", 1),
                new SitePlacementKey(SiteReservationKind.CoreResource, "SITE_A", 0),
                new SitePlacementKey(SiteReservationKind.Village, "SITE_V", 0)
            };
            Array.Sort(values);
            Assert.That(values.Select(value => value.SourceDefinitionId), Is.EqualTo(new[]
            {
                "WORLD_A", "SITE_B", "SITE_F", "SITE_A", "SITE_A", "SITE_Z", "SITE_V"
            }));
            Assert.That(values[3].RequiredInstanceOrdinal, Is.Zero);
            Assert.That(values[4].RequiredInstanceOrdinal, Is.EqualTo(1));
            var equal = new SitePlacementKey(SiteReservationKind.CoreResource, "SITE_A", 0);
            Assert.That(values[3], Is.EqualTo(equal));
            Assert.That(values[3].GetHashCode(), Is.EqualTo(equal.GetHashCode()));
        }

        [TestCase(0, 0, 0, 0, 0)]
        [TestCase(0, 0, 1, 0, 1)]
        [TestCase(0, 0, 0, 1, 1)]
        [TestCase(0, 0, 12, 12, 24)]
        [TestCase(2, 9, 11, 3, 15)]
        public void Distance_ExactReferenceVectors(int ax, int ay, int bx, int by, int expected)
        {
            if (expected == 0)
            {
                var index = Build(Single(SiteReservationKind.Start, "WORLD_A", ax, ay));
                var key = index.Keys.Single();
                Assert.That(index.TryGetDistance(key, key, out var actual), Is.True);
                Assert.That(actual, Is.EqualTo(expected));
                return;
            }
            var pair = Build(
                Single(SiteReservationKind.Start, "WORLD_A", ax, ay),
                Single(SiteReservationKind.Boss, "SITE_B", bx, by));
            Assert.That(pair.Records.Single().Distance, Is.EqualTo(expected));
        }

        [Test]
        public void Distance_UsesOccupiedFootprintMinimumAndCanonicalTieBreak()
        {
            var singleVsBoss = Build(
                Single(SiteReservationKind.Start, "WORLD_A", 0, 0),
                Rectangle(SiteReservationKind.Boss, "SITE_B", 3, 0, 2, 1));
            Assert.That(singleVsBoss.Records.Single().Distance, Is.EqualTo(3));

            var sparseA = Sparse(SiteReservationKind.Start, "WORLD_A", 0, 0, 1, 3,
                new SectorCoord(0, 0), new SectorCoord(0, 2));
            var sparseB = Sparse(SiteReservationKind.Boss, "SITE_B", 2, 0, 1, 3,
                new SectorCoord(0, 0), new SectorCoord(0, 2));
            var record = Build(sparseB, sparseA).Records.Single();
            Assert.That(record.Distance, Is.EqualTo(2));
            Assert.That(record.FirstClosestSectorIndex, Is.EqualTo(0));
            Assert.That(record.SecondClosestSectorIndex, Is.EqualTo(2));
            Assert.That(record.FirstClosestSector, Is.EqualTo(new SectorCoord(0, 0)));
            Assert.That(record.SecondClosestSector, Is.EqualTo(new SectorCoord(2, 0)));
        }

        [Test]
        public void Builder_NullAndNullItemFailWithoutPartialIndex()
        {
            var missing = new SiteDistanceIndexBuilder().Build(null);
            AssertFailure(missing, SiteDistanceErrorCode.MissingPlacements);
            var nullItem = new SiteDistanceIndexBuilder().Build(new FootprintPlacement[] { null });
            AssertFailure(nullItem, SiteDistanceErrorCode.NullPlacement);
        }

        [Test]
        public void Builder_DuplicateKeyAndOverlapFailWithoutPartialIndex()
        {
            var duplicate = new SiteDistanceIndexBuilder().Build(new[]
            {
                Single(SiteReservationKind.Boss, BossId, 0, 0),
                Single(SiteReservationKind.Boss, BossId, 2, 0)
            });
            AssertFailure(duplicate, SiteDistanceErrorCode.DuplicatePlacementKey);

            var overlap = new SiteDistanceIndexBuilder().Build(new[]
            {
                Single(SiteReservationKind.Start, WorldId, 1, 1),
                Single(SiteReservationKind.Boss, BossId, 1, 1)
            });
            AssertFailure(overlap, SiteDistanceErrorCode.OverlappingPlacements);
            Assert.That(overlap.Errors.Single().SectorIndex, Is.EqualTo(14));
        }

        [Test]
        public void PlacementModel_RejectsOutOfWorldDuplicateAndMismatchedOccupiedInput()
        {
            var candidate = Candidate(SiteReservationKind.Start, WorldId, 0, 0);
            var footprint = Footprint(1, 1, new SectorCoord(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FootprintPlacement(
                candidate, footprint, new[] { new SectorCoord(-1, 0) },
                Array.Empty<FootprintPlacementEntry>()));
            Assert.Throws<ArgumentException>(() => new FootprintPlacement(
                candidate, footprint, new[] { new SectorCoord(0, 0), new SectorCoord(0, 0) },
                Array.Empty<FootprintPlacementEntry>()));
            Assert.Throws<ArgumentException>(() => new FootprintPlacement(
                candidate, footprint, new[] { new SectorCoord(1, 0) },
                Array.Empty<FootprintPlacementEntry>()));
        }

        [Test]
        public void Builder_EmptyAndSingleAreValidImmutableIndexes()
        {
            var empty = Build();
            Assert.That(empty.PlacementCount, Is.Zero);
            Assert.That(empty.PairCount, Is.Zero);

            var single = Build(Single(SiteReservationKind.Start, WorldId, 4, 4));
            Assert.That(single.PlacementCount, Is.EqualTo(1));
            Assert.That(single.PairCount, Is.Zero);
            var key = single.Keys.Single();
            Assert.That(single.Contains(key), Is.True);
            Assert.That(single.TryGetDistance(key, key, out var distance), Is.True);
            Assert.That(distance, Is.Zero);
            Assert.Throws<NotSupportedException>(() => ((IList<SitePlacementKey>)single.Keys).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<SiteDistanceRecord>)single.Records).Clear());
        }

        [Test]
        public void Builder_CanonicalSnapshotsIgnoreInputOrderAndCallerMutation()
        {
            var placements = PassingPlacements();
            var forward = Build(placements.ToArray());
            placements.Reverse();
            var reverse = Build(placements.ToArray());
            var expected = Snapshot(forward);
            placements.Clear();
            Assert.That(Snapshot(reverse), Is.EqualTo(expected));
            Assert.That(Snapshot(forward), Is.EqualTo(expected));
            Assert.That(forward.PairCount, Is.EqualTo(15));
            Assert.That(forward.PairCount, Is.EqualTo(forward.PlacementCount * (forward.PlacementCount - 1) / 2));
        }

        [Test]
        public void Lookup_InvalidMissingReversedAndSameKeySemanticsAreExact()
        {
            var index = Build(
                Single(SiteReservationKind.Start, WorldId, 0, 0),
                Single(SiteReservationKind.Boss, BossId, 4, 0));
            var start = index.Keys[0];
            var boss = index.Keys[1];
            Assert.That(index.TryGetRecord(boss, start, out var record), Is.True);
            Assert.That(record.First, Is.EqualTo(start));
            Assert.That(index.TryGetDistance(start, start, out var self), Is.True);
            Assert.That(self, Is.Zero);
            Assert.That(index.TryGetRecord(start, start, out _), Is.False);
            Assert.That(index.TryGetDistance(default(SitePlacementKey), boss, out var invalid), Is.False);
            Assert.That(invalid, Is.EqualTo(-1));
            Assert.That(index.TryGetDistance(new SitePlacementKey(SiteReservationKind.Forge, ForgeId, 0), boss,
                out var missing), Is.False);
            Assert.That(missing, Is.EqualTo(-1));
        }

        [Test]
        public void Policy_ExactRequiredDefinitionsAndConstraintDistribution()
        {
            var definitions = CreateDefinitions();
            Assert.That(definitions[BossId].MinGraphDistanceFromStart, Is.EqualTo(4));
            Assert.That(definitions[ForgeId].MinGraphDistanceFromStart, Is.EqualTo(2));
            Assert.That(definitions[CassiaId].MinGraphDistanceToOtherCoreSites, Is.EqualTo(3));
            Assert.That(definitions[YeastId].MinGraphDistanceToOtherCoreSites, Is.EqualTo(3));
            Assert.That(definitions[MeteorId].MinGraphDistanceToOtherCoreSites, Is.EqualTo(3));

            var result = new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(WorldId, definitions.Values);
            Assert.That(result.Succeeded, Is.True, FormatErrors(result.Errors));
            var policy = result.Policy;
            Assert.That(policy.Keys.Select(key => key.SourceDefinitionId), Is.EqualTo(new[]
            {
                WorldId, BossId, ForgeId, CassiaId, YeastId, MeteorId
            }));
            Assert.That(policy.ConstraintCount, Is.EqualTo(15));
            Assert.That(policy.Constraints.Count(item => item.MinimumDistance == 2), Is.EqualTo(5));
            Assert.That(policy.Constraints.Count(item => item.MinimumDistance == 3), Is.EqualTo(9));
            Assert.That(policy.Constraints.Count(item => item.MinimumDistance == 4), Is.EqualTo(1));
            Assert.That(policy.Constraints.Count(item => item.RuleKind == SiteDistanceRuleKind.StartToRequiredSite), Is.EqualTo(5));
            Assert.That(policy.Constraints.Count(item => item.RuleKind == SiteDistanceRuleKind.RequiredSiteToRequiredSite), Is.EqualTo(10));
            Assert.That(policy.Keys.Count(key => key.Kind == SiteReservationKind.Village), Is.Zero);
            Assert.Throws<NotSupportedException>(() => ((IList<SiteDistanceConstraint>)policy.Constraints).Clear());
        }

        [Test]
        public void Policy_MissingAndInvalidInputsProduceExactErrors()
        {
            var builder = new SiteDistancePolicyBuilder();
            AssertPolicyFailure(builder.BuildRequiredSitePolicy(null, CreateDefinitions().Values),
                SiteDistanceErrorCode.MissingStartSourceId);
            AssertPolicyFailure(builder.BuildRequiredSitePolicy("world", CreateDefinitions().Values),
                SiteDistanceErrorCode.InvalidStartSourceId);
            AssertPolicyFailure(builder.BuildRequiredSitePolicy(WorldId, null),
                SiteDistanceErrorCode.MissingSpecialMapInput);

            var withNull = CreateDefinitions().Values.Cast<SpecialMapDefinition>().ToList();
            withNull.Add(null);
            AssertPolicyFailure(builder.BuildRequiredSitePolicy(WorldId, withNull),
                SiteDistanceErrorCode.NullSpecialMap);

            var duplicate = CreateDefinitions().Values.ToList();
            duplicate.Add(duplicate[0]);
            AssertPolicyFailure(builder.BuildRequiredSitePolicy(WorldId, duplicate),
                SiteDistanceErrorCode.DuplicateSpecialMapId);
        }

        [TestCase(BossId, 13, "0", SiteDistanceErrorCode.InactiveRequiredSite)]
        [TestCase(BossId, 2, "FORGE", SiteDistanceErrorCode.SiteRoleMismatch)]
        [TestCase(BossId, 6, "2", SiteDistanceErrorCode.InvalidRequiredCount)]
        [TestCase(BossId, 7, "0", SiteDistanceErrorCode.InvalidDistanceRule)]
        [TestCase(BossId, 8, "25", SiteDistanceErrorCode.InvalidDistanceRule)]
        public void Policy_RequiredDefinitionGates(
            string sourceId,
            int column,
            string value,
            SiteDistanceErrorCode expected)
        {
            var definitions = CreateDefinitions(rows => FindRow(rows, sourceId)[column] = value);
            AssertPolicyFailure(
                new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(WorldId, definitions.Values), expected);
        }

        [Test]
        public void Policy_MissingAndUnexpectedRequiredSitesAreRejected()
        {
            var missing = CreateDefinitions(rows => rows.RemoveAll(row => row[0] == BossId));
            AssertPolicyFailure(new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(WorldId, missing.Values),
                SiteDistanceErrorCode.MissingRequiredSite);

            var unexpected = CreateDefinitions(rows => rows.Add(
                CatalogRow("SITE_OTHER_BOSS", "BOSS", 1, 1, 2, 2, true)));
            AssertPolicyFailure(new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(WorldId, unexpected.Values),
                SiteDistanceErrorCode.UnexpectedRequiredSite);
        }

        [Test]
        public void Policy_ActiveVillageIsValidlyExcluded()
        {
            var definitions = CreateDefinitions(rows => rows.Add(
                CatalogRow("SITE_VILLAGE_A", "VILLAGE", 1, 1, 2, 2, true)));
            var result = new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(WorldId, definitions.Values);
            Assert.That(result.Succeeded, Is.True, FormatErrors(result.Errors));
            Assert.That(result.Policy.Keys.Any(key => key.Kind == SiteReservationKind.Village), Is.False);
        }

        [Test]
        public void PolicyLookup_NormalizesPairsAndRejectsSelfOrMissing()
        {
            var policy = Policy();
            var start = policy.Keys[0];
            var boss = policy.Keys[1];
            Assert.That(policy.TryGetConstraint(boss, start, out var constraint), Is.True);
            Assert.That(constraint.MinimumDistance, Is.EqualTo(4));
            Assert.That(policy.TryGetConstraint(start, start, out _), Is.False);
            Assert.That(policy.TryGetConstraint(start,
                new SitePlacementKey(SiteReservationKind.Village, "SITE_VILLAGE", 0), out _), Is.False);
        }

        [Test]
        public void Evaluation_PassingExactSixSetSatisfiesAllFifteenConstraints()
        {
            var index = Build(PassingPlacements().ToArray());
            var result = index.Evaluate(Policy());
            Assert.That(result.Succeeded, Is.True, FormatErrors(result.Errors));
            Assert.That(result.Satisfied, Is.True);
            Assert.That(result.Violations, Is.Empty);
            foreach (var constraint in Policy().Constraints)
            {
                Assert.That(index.TryGetDistance(constraint.First, constraint.Second, out var distance), Is.True);
                Assert.That(distance, Is.GreaterThanOrEqualTo(constraint.MinimumDistance));
            }
        }

        [Test]
        public void Evaluation_ExactFourThreeAndTwoThresholdsPass()
        {
            var policy = Policy();

            var startBoundary = Build(PassingPlacements().ToArray());
            AssertPairPassesAtExactThreshold(startBoundary, policy, WorldId, BossId, 4);

            var corePlacements = PassingPlacements();
            corePlacements[4] = Single(SiteReservationKind.CoreResource, YeastId, 3, 4);
            AssertPairPassesAtExactThreshold(
                Build(corePlacements.ToArray()), policy, CassiaId, YeastId, 3);

            var sitePlacements = PassingPlacements();
            sitePlacements[2] = Single(SiteReservationKind.Forge, ForgeId, 7, 0);
            AssertPairPassesAtExactThreshold(
                Build(sitePlacements.ToArray()), policy, BossId, ForgeId, 2);
        }

        [Test]
        public void Evaluation_StartBossBoundaryViolationHasExactEvidence()
        {
            var placements = PassingPlacements();
            placements[1] = Rectangle(SiteReservationKind.Boss, BossId, 3, 0, 2, 1);
            var result = Build(placements.ToArray()).Evaluate(Policy());
            var violation = result.Violations.Single(item =>
                item.First.Kind == SiteReservationKind.Start && item.Second.Kind == SiteReservationKind.Boss);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Satisfied, Is.False);
            Assert.That(violation.ActualDistance, Is.EqualTo(3));
            Assert.That(violation.MinimumDistance, Is.EqualTo(4));
            Assert.That(violation.Deficit, Is.EqualTo(1));
            Assert.That(violation.FirstClosestSector, Is.EqualTo(new SectorCoord(0, 0)));
            Assert.That(violation.SecondClosestSector, Is.EqualTo(new SectorCoord(3, 0)));
        }

        [Test]
        public void Evaluation_CoreCoreAndForgeBossBoundariesHaveExactDeficit()
        {
            var corePlacements = PassingPlacements();
            corePlacements[4] = Single(SiteReservationKind.CoreResource, YeastId, 2, 4);
            var coreResult = Build(corePlacements.ToArray()).Evaluate(Policy());
            var coreViolation = coreResult.Violations.Single(item =>
                item.First.SourceDefinitionId == CassiaId && item.Second.SourceDefinitionId == YeastId);
            Assert.That(coreViolation.ActualDistance, Is.EqualTo(2));
            Assert.That(coreViolation.MinimumDistance, Is.EqualTo(3));
            Assert.That(coreViolation.Deficit, Is.EqualTo(1));

            var sitePlacements = PassingPlacements();
            sitePlacements[2] = Single(SiteReservationKind.Forge, ForgeId, 6, 0);
            var siteResult = Build(sitePlacements.ToArray()).Evaluate(Policy());
            var siteViolation = siteResult.Violations.Single(item =>
                item.First.Kind == SiteReservationKind.Boss && item.Second.Kind == SiteReservationKind.Forge);
            Assert.That(siteViolation.ActualDistance, Is.EqualTo(1));
            Assert.That(siteViolation.MinimumDistance, Is.EqualTo(2));
            Assert.That(siteViolation.Deficit, Is.EqualTo(1));
        }

        [Test]
        public void Evaluation_MultipleViolationsAreCanonicalAndReadOnly()
        {
            var placements = PassingPlacements();
            placements[1] = Rectangle(SiteReservationKind.Boss, BossId, 2, 0, 2, 1);
            placements[2] = Single(SiteReservationKind.Forge, ForgeId, 4, 0);
            var result = Build(placements.ToArray()).Evaluate(Policy());
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Violations.Count, Is.GreaterThanOrEqualTo(2));
            for (var index = 1; index < result.Violations.Count; index++)
            {
                var left = result.Violations[index - 1];
                var right = result.Violations[index];
                Assert.That(left.RuleKind.CompareTo(right.RuleKind) < 0 ||
                            (left.RuleKind == right.RuleKind && left.First.CompareTo(right.First) <= 0), Is.True);
            }
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SiteDistanceViolation>)result.Violations).Clear());
        }

        [Test]
        public void Evaluation_MissingPolicyAndKeyMismatchAreStructuralFailures()
        {
            var exact = Build(PassingPlacements().ToArray());
            var missingPolicy = exact.Evaluate(null);
            Assert.That(missingPolicy.Succeeded, Is.False);
            Assert.That(missingPolicy.Satisfied, Is.False);
            Assert.That(missingPolicy.Violations, Is.Empty);
            Assert.That(missingPolicy.Errors.Single().Code, Is.EqualTo(SiteDistanceErrorCode.MissingPolicy));

            var partial = PassingPlacements();
            partial.RemoveAt(partial.Count - 1);
            var missing = Build(partial.ToArray()).Evaluate(Policy());
            Assert.That(missing.Succeeded, Is.False);
            Assert.That(missing.Violations, Is.Empty);
            Assert.That(missing.Errors.Any(error => error.Code == SiteDistanceErrorCode.MissingPolicyKey), Is.True);

            var extra = PassingPlacements();
            extra.Add(Single(SiteReservationKind.Village, "SITE_VILLAGE", 12, 12));
            var unexpected = Build(extra.ToArray()).Evaluate(Policy());
            Assert.That(unexpected.Succeeded, Is.False);
            Assert.That(unexpected.Violations, Is.Empty);
            Assert.That(unexpected.Errors.Any(error => error.Code == SiteDistanceErrorCode.UnexpectedIndexKey), Is.True);
        }

        [Test]
        public void PartialState_UsesDirectPolicyAndIndexLookupWithoutCompleteEvaluation()
        {
            var partial = Build(
                Single(SiteReservationKind.Start, WorldId, 0, 0),
                Rectangle(SiteReservationKind.Boss, BossId, 4, 0, 2, 1));
            var policy = Policy();
            var start = policy.Keys[0];
            var boss = policy.Keys[1];
            Assert.That(policy.TryGetConstraint(start, boss, out var constraint), Is.True);
            Assert.That(partial.TryGetDistance(start, boss, out var distance), Is.True);
            Assert.That(distance, Is.EqualTo(constraint.MinimumDistance));
            Assert.That(partial.Evaluate(policy).Succeeded, Is.False);
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void Determinism_CultureInputOrderAndRepeatedBuildersAreStable(string cultureName)
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                var baseline = Snapshot(Build(PassingPlacements().ToArray()));
                for (var run = 0; run < 100; run++)
                {
                    var placements = PassingPlacements();
                    if ((run & 1) != 0) placements.Reverse();
                    Assert.That(Snapshot(Build(placements.ToArray())), Is.EqualTo(baseline));
                }
            }
            finally { CultureInfo.CurrentCulture = original; }
        }

        [TestCase(0UL)]
        [TestCase(4660UL)]
        [TestCase(ulong.MaxValue)]
        public void SeedCandidateOrdinalAndTransformDoNotAffectOccupiedDistance(ulong seed)
        {
            var ordinal = unchecked((int)(seed & 0x7fffffff));
            var first = Single(SiteReservationKind.Start, WorldId, 0, 0, ordinal,
                SiteFootprintTransform.R180);
            var second = Rectangle(SiteReservationKind.Boss, BossId, 4, 0, 2, 1,
                ordinal, SiteFootprintTransform.MirrorX);
            Assert.That(Build(first, second).Records.Single().Distance, Is.EqualTo(4));
        }

        [Test]
        public void PublicApi_HasNoMutableSurfaceOrLaterTaskDependencies()
        {
            var types = new[]
            {
                typeof(SitePlacementKey), typeof(SiteDistanceRecord), typeof(SiteDistanceError),
                typeof(SiteDistanceIndexResult), typeof(SiteDistancePolicyResult),
                typeof(SiteDistanceConstraint), typeof(SiteDistancePolicy),
                typeof(SiteDistancePolicyBuilder), typeof(SiteDistanceViolation),
                typeof(SiteDistanceEvaluationResult), typeof(SiteDistanceIndex),
                typeof(SiteDistanceIndexBuilder)
            };
            foreach (var type in types)
            {
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
                    Is.Empty, type.FullName);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.SetMethod != null), Is.Empty, type.FullName);
            }
            var names = string.Join("|", types.Select(type => type.FullName));
            Assert.That(names, Does.Not.Contain("Cost").And.Not.Contain("Backtrack")
                .And.Not.Contain("Capacity").And.Not.Contain("RoutePass"));
        }

        private static SiteDistanceIndex Build(params FootprintPlacement[] placements)
        {
            var result = new SiteDistanceIndexBuilder().Build(placements);
            Assert.That(result.Succeeded, Is.True, FormatErrors(result.Errors));
            return result.Index;
        }

        private static void AssertFailure(SiteDistanceIndexResult result, SiteDistanceErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Index, Is.Null);
            Assert.That(result.Errors.Any(error => error.Code == code), Is.True, FormatErrors(result.Errors));
        }

        private static void AssertPolicyFailure(SiteDistancePolicyResult result, SiteDistanceErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Policy, Is.Null);
            Assert.That(result.Errors.Any(error => error.Code == code), Is.True, FormatErrors(result.Errors));
        }

        private static void AssertPairPassesAtExactThreshold(
            SiteDistanceIndex index,
            SiteDistancePolicy policy,
            string firstSourceId,
            string secondSourceId,
            int expected)
        {
            var first = policy.Keys.Single(key => key.SourceDefinitionId == firstSourceId);
            var second = policy.Keys.Single(key => key.SourceDefinitionId == secondSourceId);
            Assert.That(policy.TryGetConstraint(first, second, out var constraint), Is.True);
            Assert.That(constraint.MinimumDistance, Is.EqualTo(expected));
            Assert.That(index.TryGetDistance(first, second, out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
            var evaluation = index.Evaluate(policy);
            Assert.That(evaluation.Succeeded, Is.True, FormatErrors(evaluation.Errors));
            Assert.That(evaluation.Violations.Any(violation =>
                violation.First.SourceDefinitionId == firstSourceId &&
                violation.Second.SourceDefinitionId == secondSourceId), Is.False);
        }

        private static SiteDistancePolicy Policy()
        {
            var result = new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(WorldId, CreateDefinitions().Values);
            Assert.That(result.Succeeded, Is.True, FormatErrors(result.Errors));
            return result.Policy;
        }

        private static List<FootprintPlacement> PassingPlacements()
        {
            return new List<FootprintPlacement>
            {
                Single(SiteReservationKind.Start, WorldId, 0, 0),
                Rectangle(SiteReservationKind.Boss, BossId, 4, 0, 2, 1),
                Single(SiteReservationKind.Forge, ForgeId, 8, 0),
                Single(SiteReservationKind.CoreResource, CassiaId, 0, 4),
                Single(SiteReservationKind.CoreResource, YeastId, 4, 6),
                Single(SiteReservationKind.CoreResource, MeteorId, 9, 6)
            };
        }

        private static FootprintPlacement Single(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int candidateOrdinal = 0,
            SiteFootprintTransform transform = SiteFootprintTransform.R0)
        {
            return Sparse(kind, sourceId, x, y, 1, 1, candidateOrdinal, transform,
                new SectorCoord(0, 0));
        }

        private static FootprintPlacement Rectangle(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int width,
            int height,
            int candidateOrdinal = 0,
            SiteFootprintTransform transform = SiteFootprintTransform.R0)
        {
            var cells = new List<SectorCoord>();
            for (var localY = 0; localY < height; localY++)
                for (var localX = 0; localX < width; localX++)
                    cells.Add(new SectorCoord(localX, localY));
            return Sparse(kind, sourceId, x, y, width, height, candidateOrdinal, transform,
                cells.ToArray());
        }

        private static FootprintPlacement Sparse(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int width,
            int height,
            params SectorCoord[] localCells)
        {
            return Sparse(kind, sourceId, x, y, width, height, 0, SiteFootprintTransform.R0, localCells);
        }

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
            var candidate = Candidate(kind, sourceId, x, y, candidateOrdinal);
            var footprint = Footprint(width, height, transform, localCells);
            var occupied = localCells.Select(cell => new SectorCoord(x + cell.X, y + cell.Y)).ToArray();
            return new FootprintPlacement(candidate, footprint, occupied,
                Array.Empty<FootprintPlacementEntry>());
        }

        private static SiteFootprint Footprint(
            int width,
            int height,
            params SectorCoord[] localCells) =>
            Footprint(width, height, SiteFootprintTransform.R0, localCells);

        private static SiteFootprint Footprint(
            int width,
            int height,
            SiteFootprintTransform transform,
            params SectorCoord[] localCells)
        {
            return new SiteFootprint(width, height, transform, localCells.Select(cell =>
                new SiteFootprintCell(cell.X, cell.Y, "CELL", string.Empty, string.Empty,
                    Array.Empty<SiteEntrySide>())));
        }

        private static SiteOriginCandidate Candidate(
            SiteReservationKind kind,
            string sourceId,
            int x,
            int y,
            int candidateOrdinal = 0)
        {
            var origin = new SectorCoord(x, y);
            return new SiteOriginCandidate(kind, sourceId, 0, origin,
                WorldGridIndex.ToIndex(origin), EdgeRing(origin), candidateOrdinal);
        }

        private static int EdgeRing(SectorCoord origin) => Math.Min(
            Math.Min(origin.X, WorldGenConstants.SectorColumns - 1 - origin.X),
            Math.Min(origin.Y, WorldGenConstants.SectorRows - 1 - origin.Y));

        private static string Snapshot(SiteDistanceIndex index)
        {
            return string.Join(",", index.Keys.Select(key =>
                       (int)key.Kind + ":" + key.SourceDefinitionId + ":" + key.RequiredInstanceOrdinal)) + "|" +
                   string.Join(",", index.Records.Select(record =>
                       record.First.SourceDefinitionId + ":" + record.Second.SourceDefinitionId + ":" +
                       record.Distance + ":" + record.FirstClosestSectorIndex + ":" +
                       record.SecondClosestSectorIndex));
        }

        private static IReadOnlyDictionary<string, SpecialMapDefinition> CreateDefinitions(
            Action<List<string[]>> configure = null)
        {
            var catalogRows = new List<string[]>
            {
                CatalogRow(BossId, "BOSS", 2, 1, 4, 2, true),
                CatalogRow(ForgeId, "FORGE", 1, 1, 2, 2, true),
                CatalogRow(CassiaId, "CORE_RESOURCE", 1, 1, 2, 3, true),
                CatalogRow(YeastId, "CORE_RESOURCE", 1, 1, 2, 3, true),
                CatalogRow(MeteorId, "CORE_RESOURCE", 1, 1, 2, 3, true)
            };
            configure?.Invoke(catalogRows);
            var sources = new List<SpecialVillageDefinitionSource>();
            foreach (var spec in SpecialSpecs)
            {
                sources.Add(BuildSpecialSource(spec,
                    spec.FileName == "special_map_catalog.csv" ? catalogRows : null));
            }
            var result = new SpecialVillageDefinitionBuilder().Build(sources);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            return result.DefinitionSet.SpecialMaps;
        }

        private static string[] CatalogRow(
            string sourceId,
            string role,
            int width,
            int height,
            int startDistance,
            int otherDistance,
            bool active)
        {
            return new[]
            {
                sourceId, "Site", role, "BIOME_MOON",
                width.ToString(CultureInfo.InvariantCulture),
                height.ToString(CultureInfo.InvariantCulture),
                "1", startDistance.ToString(CultureInfo.InvariantCulture),
                otherDistance.ToString(CultureInfo.InvariantCulture),
                "1|2|3", "0", "REWARD_NONE", "FIXED", active ? "1" : "0", "test"
            };
        }

        private static string[] FindRow(IEnumerable<string[]> rows, string sourceId) =>
            rows.Single(row => row[0] == sourceId);

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
                index < spec.PrimaryKeyCount ? (index + 1).ToString(CultureInfo.InvariantCulture) : string.Empty,
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

        private static string CsvCell(string value) =>
            value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";

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

        private static string FormatErrors(IEnumerable<SiteDistanceError> errors) =>
            string.Join("\n", errors.Select(error =>
                error.Code + ":" + error.FirstSourceDefinitionId + ":" + error.Message));

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
