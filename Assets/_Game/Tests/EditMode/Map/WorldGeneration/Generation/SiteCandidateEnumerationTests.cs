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
    public sealed class SiteCandidateEnumerationTests
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string GenerationId = "GEN_MOONPALACE_V1";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";

        private static readonly FileSpec[] WorldSpecs = CreateWorldSpecs();
        private static readonly FileSpec[] SpecialSpecs = CreateSpecialSpecs();

        private static IEnumerable<TestCaseData> EveryGridOrigin
        {
            get
            {
                for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                {
                    var coordinate = WorldGridIndex.ToCoordinate(index);
                    yield return new TestCaseData(index, coordinate.X, coordinate.Y)
                        .SetName("Origin_" + index.ToString("D3", CultureInfo.InvariantCulture));
                }
            }
        }

        [TestCaseSource(nameof(EveryGridOrigin))]
        public void Candidate_PreservesExactGridIdentityAndEdgeRing(int index, int x, int y)
        {
            var origin = new SectorCoord(x, y);
            var edgeRing = EdgeRing(origin);
            var candidate = new SiteOriginCandidate(
                SiteReservationKind.Boss, BossId, 0, origin, index, edgeRing, index);

            Assert.That(candidate.Kind, Is.EqualTo(SiteReservationKind.Boss));
            Assert.That(candidate.SourceDefinitionId, Is.EqualTo(BossId));
            Assert.That(candidate.RequiredInstanceOrdinal, Is.Zero);
            Assert.That(candidate.Origin, Is.EqualTo(origin));
            Assert.That(candidate.OriginIndex, Is.EqualTo(index));
            Assert.That(candidate.EdgeRing, Is.EqualTo(edgeRing));
            Assert.That(candidate.CandidateOrdinal, Is.EqualTo(index));
        }

        [Test]
        public void EdgeRing_DistributionIsExact()
        {
            var counts = new int[7];
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                counts[EdgeRing(WorldGridIndex.ToCoordinate(index))]++;
            Assert.That(counts, Is.EqualTo(new[] { 48, 40, 32, 24, 16, 8, 1 }));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void Candidate_RejectsUndefinedKind(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SiteOriginCandidate((SiteReservationKind)value, BossId, 0,
                    new SectorCoord(0, 0), 0, 0, 0));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("lowercase")]
        [TestCase("BAD-ID")]
        public void Candidate_RejectsNonCanonicalSource(string sourceId)
        {
            Assert.Catch<ArgumentException>(() =>
                new SiteOriginCandidate(SiteReservationKind.Boss, sourceId, 0,
                    new SectorCoord(0, 0), 0, 0, 0));
        }

        [Test]
        public void Candidate_RejectsNegativeInstanceOrdinal()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SiteOriginCandidate(SiteReservationKind.Boss, BossId, -1,
                    new SectorCoord(0, 0), 0, 0, 0));
        }

        [Test]
        public void Candidate_RejectsOriginIndexMismatch()
        {
            Assert.Throws<ArgumentException>(() =>
                new SiteOriginCandidate(SiteReservationKind.Boss, BossId, 0,
                    new SectorCoord(1, 0), 0, 0, 0));
        }

        [Test]
        public void Candidate_RejectsEdgeRingMismatch()
        {
            Assert.Throws<ArgumentException>(() =>
                new SiteOriginCandidate(SiteReservationKind.Boss, BossId, 0,
                    new SectorCoord(0, 0), 0, 1, 0));
        }

        [Test]
        public void Candidate_RejectsNegativeCandidateOrdinal()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SiteOriginCandidate(SiteReservationKind.Boss, BossId, 0,
                    new SectorCoord(0, 0), 0, 0, -1));
        }

        [TestCase(SiteReservationKind.Start, 0)]
        [TestCase(SiteReservationKind.Boss, 10)]
        [TestCase(SiteReservationKind.Forge, 20)]
        [TestCase(SiteReservationKind.CoreResource, 30)]
        public void Group_UsesExactPlacementPriority(SiteReservationKind kind, int expected)
        {
            var source = kind == SiteReservationKind.Start ? WorldId : SourceFor(kind);
            var group = new SiteCandidateGroup(kind, source, 0,
                new[] { Candidate(kind, source, 0, 0) });
            Assert.That(group.PlacementPriority, Is.EqualTo(expected));
        }

        [Test]
        public void Group_SortsByOriginIndexIndependentOfCallerOrder()
        {
            var group = new SiteCandidateGroup(SiteReservationKind.Boss, BossId, 0, new[]
            {
                Candidate(SiteReservationKind.Boss, BossId, 2, 2),
                Candidate(SiteReservationKind.Boss, BossId, 0, 0),
                Candidate(SiteReservationKind.Boss, BossId, 1, 1)
            });
            Assert.That(group.Candidates.Select(item => item.OriginIndex), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void Group_LookupsUseOrdinalAndOrigin()
        {
            var group = CreateAllOriginGroup(SiteReservationKind.Boss, BossId);
            Assert.That(group.Count, Is.EqualTo(169));
            Assert.That(group.GetCandidate(168).Origin, Is.EqualTo(new SectorCoord(12, 12)));
            Assert.That(group.TryGetCandidateByOrigin(new SectorCoord(6, 6), out var candidate), Is.True);
            Assert.That(candidate.OriginIndex, Is.EqualTo(84));
            Assert.That(group.TryGetCandidateByOrigin(new SectorCoord(-1, 0), out _), Is.False);
        }

        [Test]
        public void Group_RejectsNullEmptyAndNullItemCollections()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SiteCandidateGroup(SiteReservationKind.Boss, BossId, 0, null));
            Assert.Throws<ArgumentException>(() =>
                new SiteCandidateGroup(SiteReservationKind.Boss, BossId, 0,
                    Array.Empty<SiteOriginCandidate>()));
            Assert.Throws<ArgumentException>(() =>
                new SiteCandidateGroup(SiteReservationKind.Boss, BossId, 0,
                    new SiteOriginCandidate[] { null }));
        }

        [Test]
        public void Group_RejectsIdentityAndOrdinalMismatches()
        {
            Assert.Throws<ArgumentException>(() => new SiteCandidateGroup(
                SiteReservationKind.Boss, BossId, 0,
                new[] { Candidate(SiteReservationKind.Forge, ForgeId, 0, 0) }));
            Assert.Throws<ArgumentException>(() => new SiteCandidateGroup(
                SiteReservationKind.Boss, BossId, 0,
                new[] { Candidate(SiteReservationKind.Boss, BossId, 0, 1) }));
        }

        [Test]
        public void Group_RejectsDuplicateOriginAndVillage()
        {
            var candidate = Candidate(SiteReservationKind.Boss, BossId, 0, 0);
            Assert.Throws<ArgumentException>(() => new SiteCandidateGroup(
                SiteReservationKind.Boss, BossId, 0, new[] { candidate, candidate }));
            Assert.Throws<ArgumentException>(() => new SiteCandidateGroup(
                SiteReservationKind.Village, "SITE_VILLAGE", 0,
                new[] { Candidate(SiteReservationKind.Village, "SITE_VILLAGE", 0, 0) }));
        }

        [Test]
        public void Group_CollectionsAreReadOnly()
        {
            var group = CreateAllOriginGroup(SiteReservationKind.Boss, BossId);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SiteOriginCandidate>)group.Candidates).Clear());
            Assert.Throws<ArgumentOutOfRangeException>(() => group.GetCandidate(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => group.GetCandidate(group.Count));
        }

        [Test]
        public void Catalog_OrdersExactGroupsAndCounts()
        {
            var catalog = CreateCatalog(4660);
            Assert.That(catalog.Seed, Is.EqualTo(4660));
            Assert.That(catalog.WorldProfileId, Is.EqualTo(WorldId));
            Assert.That(catalog.GenerationProfileId, Is.EqualTo(GenerationId));
            Assert.That(catalog.StartGroup, Is.SameAs(catalog.Groups[0]));
            Assert.That(catalog.Groups.Select(item => item.SourceDefinitionId), Is.EqualTo(new[]
            {
                WorldId, BossId, ForgeId, CassiaId, YeastId, MeteorId
            }));
            Assert.That(catalog.Groups.Select(item => item.Count), Is.EqualTo(new[]
            {
                88, 169, 169, 169, 169, 169
            }));
            Assert.That(catalog.TotalCandidateCount, Is.EqualTo(933));
        }

        [Test]
        public void Catalog_SiteInputOrderDoesNotAffectOrder()
        {
            var sites = CreateSiteGroups();
            sites.Reverse();
            var catalog = new SiteCandidateCatalog(0, WorldId, GenerationId, CreateStartGroup(), sites);
            Assert.That(catalog.SiteGroups.Select(item => item.SourceDefinitionId), Is.EqualTo(new[]
            {
                BossId, ForgeId, CassiaId, YeastId, MeteorId
            }));
        }

        [Test]
        public void Catalog_TryGetGroupUsesExactKey()
        {
            var catalog = CreateCatalog(0);
            Assert.That(catalog.TryGetGroup(SiteReservationKind.CoreResource, YeastId, 0, out var group), Is.True);
            Assert.That(group.SourceDefinitionId, Is.EqualTo(YeastId));
            Assert.That(catalog.TryGetGroup(SiteReservationKind.Village, "SITE_VILLAGE", 0, out _), Is.False);
        }

        [Test]
        public void Catalog_RejectsWrongCountsNullAndDuplicateKeys()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SiteCandidateCatalog(0, WorldId, GenerationId, null, CreateSiteGroups()));
            Assert.Throws<ArgumentNullException>(() =>
                new SiteCandidateCatalog(0, WorldId, GenerationId, CreateStartGroup(), null));
            Assert.Throws<ArgumentException>(() =>
                new SiteCandidateCatalog(0, WorldId, GenerationId, CreateStartGroup(), CreateSiteGroups().Take(4)));
            var duplicates = CreateSiteGroups();
            duplicates[4] = duplicates[3];
            Assert.Throws<ArgumentException>(() =>
                new SiteCandidateCatalog(0, WorldId, GenerationId, CreateStartGroup(), duplicates));
        }

        [Test]
        public void Catalog_RejectsStartIdentityAndWrongKindDistribution()
        {
            Assert.Throws<ArgumentException>(() => new SiteCandidateCatalog(
                0, "WORLD_OTHER", GenerationId, CreateStartGroup(), CreateSiteGroups()));
            var sites = CreateSiteGroups();
            sites[0] = CreateAllOriginGroup(SiteReservationKind.CoreResource, "SITE_OTHER_CORE");
            Assert.Throws<ArgumentException>(() => new SiteCandidateCatalog(
                0, WorldId, GenerationId, CreateStartGroup(), sites));
        }

        [Test]
        public void Catalog_CollectionsAreReadOnly()
        {
            var catalog = CreateCatalog(0);
            Assert.Throws<NotSupportedException>(() => ((IList<SiteCandidateGroup>)catalog.Groups).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<SiteCandidateGroup>)catalog.SiteGroups).Clear());
        }

        [TestCaseSource(nameof(AllErrorCodes))]
        public void EnumerationError_PreservesEveryDefinedCode(SiteCandidateEnumerationErrorCode code)
        {
            var error = new SiteCandidateEnumerationError(code, string.Empty, "Stable message.");
            Assert.That(error.ErrorCode, Is.EqualTo(code));
            Assert.That(error.SourceDefinitionId, Is.Empty);
            Assert.That(error.Message, Is.EqualTo("Stable message."));
        }

        [Test]
        public void EnumerationError_RejectsInvalidIdentityAndMessage()
        {
            Assert.Throws<ArgumentException>(() => new SiteCandidateEnumerationError(
                SiteCandidateEnumerationErrorCode.InvalidGrid, "bad-id", "message"));
            Assert.Throws<ArgumentNullException>(() => new SiteCandidateEnumerationError(
                SiteCandidateEnumerationErrorCode.InvalidGrid, string.Empty, null));
            Assert.Throws<ArgumentException>(() => new SiteCandidateEnumerationError(
                SiteCandidateEnumerationErrorCode.InvalidGrid, string.Empty, "  "));
        }

        [Test]
        public void EnumerationResult_EnforcesSuccessFailureExclusivityAndReadOnlyErrors()
        {
            var catalog = CreateCatalog(0);
            var success = new SiteCandidateEnumerationResult(catalog,
                Array.Empty<SiteCandidateEnumerationError>());
            var failure = new SiteCandidateEnumerationResult(null, new[]
            {
                new SiteCandidateEnumerationError(
                    SiteCandidateEnumerationErrorCode.MissingGrid, string.Empty, "Missing grid.")
            });
            Assert.That(success.Succeeded, Is.True);
            Assert.That(success.Catalog, Is.SameAs(catalog));
            Assert.That(failure.Succeeded, Is.False);
            Assert.That(failure.Catalog, Is.Null);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SiteCandidateEnumerationError>)failure.Errors).Clear());
            Assert.Throws<ArgumentException>(() => new SiteCandidateEnumerationResult(
                null, Array.Empty<SiteCandidateEnumerationError>()));
            Assert.Throws<ArgumentException>(() => new SiteCandidateEnumerationResult(
                catalog, new[] { failure.Errors[0] }));
        }

        [Test]
        public void Enumerator_ProducesExactStarterCatalog()
        {
            var result = Enumerate(4660);
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Catalog.Seed, Is.EqualTo(4660));
            Assert.That(result.Catalog.Groups.Count, Is.EqualTo(6));
            Assert.That(result.Catalog.StartGroup.Count, Is.EqualTo(88));
            Assert.That(result.Catalog.SiteGroups.Sum(group => group.Count), Is.EqualTo(845));
            Assert.That(result.Catalog.TotalCandidateCount, Is.EqualTo(933));
            Assert.That(result.Catalog.Groups.Count(group => group.Kind == SiteReservationKind.Village), Is.Zero);
        }

        [Test]
        public void Enumerator_StartMembershipIsExactOuterTwoRings()
        {
            var start = Enumerate(0).Catalog.StartGroup;
            Assert.That(start.Candidates.Count(item => item.EdgeRing == 0), Is.EqualTo(48));
            Assert.That(start.Candidates.Count(item => item.EdgeRing == 1), Is.EqualTo(40));
            Assert.That(start.Candidates.Select(item => item.OriginIndex).Distinct().Count(), Is.EqualTo(88));
            foreach (var corner in new[]
                     {
                         new SectorCoord(0, 0), new SectorCoord(12, 0),
                         new SectorCoord(0, 12), new SectorCoord(12, 12)
                     })
                Assert.That(start.TryGetCandidateByOrigin(corner, out _), Is.True);
            Assert.That(start.TryGetCandidateByOrigin(new SectorCoord(2, 2), out _), Is.False);
            Assert.That(start.TryGetCandidateByOrigin(new SectorCoord(6, 6), out _), Is.False);
        }

        [Test]
        public void Enumerator_EachSpecialGroupContainsEveryRawOriginIncludingBossBoundary()
        {
            foreach (var group in Enumerate(0).Catalog.SiteGroups)
            {
                Assert.That(group.Count, Is.EqualTo(169));
                Assert.That(group.Candidates.Select(item => item.OriginIndex),
                    Is.EqualTo(Enumerable.Range(0, 169)));
                Assert.That(group.Candidates.Select(item => item.Origin).Distinct().Count(), Is.EqualTo(169));
            }
            var boss = Enumerate(0).Catalog.SiteGroups.Single(group => group.Kind == SiteReservationKind.Boss);
            Assert.That(boss.TryGetCandidateByOrigin(new SectorCoord(12, 12), out var boundary), Is.True);
            Assert.That(boundary.OriginIndex, Is.EqualTo(168));
        }

        [TestCase(0UL)]
        [TestCase(4660UL)]
        [TestCase(ulong.MaxValue)]
        public void Enumerator_PreservesSeedWithoutChangingMembership(ulong seed)
        {
            var result = Enumerate(seed);
            Assert.That(result.Catalog.Seed, Is.EqualTo(seed));
            Assert.That(Snapshot(result.Catalog), Is.EqualTo(Snapshot(Enumerate(0).Catalog)));
        }

        [Test]
        public void Enumerator_InputOrderAndCollectionImplementationDoNotAffectOutput()
        {
            var profiles = CreateProfiles();
            var maps = CreateSpecialMaps().ToList();
            var grid = new GridInitializationPass().Execute(4660);
            var forward = new SiteCandidateEnumerator().Enumerate(grid, profiles.World, profiles.Generation, maps);
            maps.Reverse();
            var reverse = new SiteCandidateEnumerator().Enumerate(grid, profiles.World, profiles.Generation, maps.ToArray());
            Assert.That(Snapshot(reverse.Catalog), Is.EqualTo(Snapshot(forward.Catalog)));
        }

        [Test]
        public void Enumerator_ReusedAndFreshInstancesAreStableForOneHundredRuns()
        {
            var profiles = CreateProfiles();
            var maps = CreateSpecialMaps();
            var grid = new GridInitializationPass().Execute(4660);
            var reused = new SiteCandidateEnumerator();
            var expected = Snapshot(reused.Enumerate(grid, profiles.World, profiles.Generation, maps).Catalog);
            for (var run = 0; run < 100; run++)
            {
                Assert.That(Snapshot(reused.Enumerate(grid, profiles.World, profiles.Generation, maps).Catalog),
                    Is.EqualTo(expected));
                Assert.That(Snapshot(new SiteCandidateEnumerator().Enumerate(
                    grid, profiles.World, profiles.Generation, maps).Catalog), Is.EqualTo(expected));
            }
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void Enumerator_IsCultureInvariant(string cultureName)
        {
            var original = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                Assert.That(Snapshot(Enumerate(4660).Catalog), Is.EqualTo(Snapshot(Enumerate(4660).Catalog)));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
                CultureInfo.CurrentUICulture = originalUi;
            }
        }

        [Test]
        public void Enumerator_OutputIsolatedFromCallerCollectionMutation()
        {
            var profiles = CreateProfiles();
            var maps = CreateSpecialMaps().ToList();
            var result = new SiteCandidateEnumerator().Enumerate(
                new GridInitializationPass().Execute(0), profiles.World, profiles.Generation, maps);
            var snapshot = Snapshot(result.Catalog);
            maps.Clear();
            Assert.That(Snapshot(result.Catalog), Is.EqualTo(snapshot));
        }

        [Test]
        public void Enumerator_AccumulatesMissingInputsWithoutPartialCatalog()
        {
            var result = new SiteCandidateEnumerator().Enumerate(null, null, null, null);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(SiteCandidateEnumerationErrorCode.MissingGrid));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(SiteCandidateEnumerationErrorCode.MissingWorldProfile));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(SiteCandidateEnumerationErrorCode.MissingGenerationProfile));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(SiteCandidateEnumerationErrorCode.MissingSpecialMapInput));
            AssertSorted(result.Errors);
        }

        [TestCase("world")]
        [TestCase("generation")]
        public void Enumerator_RejectsInactiveProfiles(string target)
        {
            var profiles = CreateProfiles((world, generation) =>
            {
                if (target == "world") world[19] = "0";
                else generation[22] = "0";
            });
            AssertFailure(Enumerate(profiles, CreateSpecialMaps()),
                SiteCandidateEnumerationErrorCode.InactiveProfile);
        }

        [Test]
        public void Enumerator_RejectsProfileWorldMismatch()
        {
            var profiles = CreateProfiles((_, generation) => generation[1] = "WORLD_OTHER");
            AssertFailure(Enumerate(profiles, CreateSpecialMaps()),
                SiteCandidateEnumerationErrorCode.ProfileWorldMismatch);
        }

        [TestCase(2, "623")]
        [TestCase(3, "415")]
        [TestCase(4, "47")]
        [TestCase(5, "31")]
        [TestCase(6, "12")]
        [TestCase(7, "12")]
        [TestCase(8, "11")]
        [TestCase(9, "7")]
        [TestCase(10, "3")]
        [TestCase(11, "3")]
        public void Enumerator_RejectsEveryFixedWorldDimensionMismatch(int field, string value)
        {
            var profiles = CreateProfiles((world, _) => world[field] = value);
            AssertFailure(Enumerate(profiles, CreateSpecialMaps()),
                SiteCandidateEnumerationErrorCode.InvalidWorldDimensions);
        }

        [TestCase(-1, 1)]
        [TestCase(0, 7)]
        [TestCase(2, 1)]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        public void Enumerator_RejectsNonStarterRingContract(int minimum, int maximum)
        {
            var profiles = CreateProfiles((_, generation) =>
            {
                generation[10] = minimum.ToString(CultureInfo.InvariantCulture);
                generation[11] = maximum.ToString(CultureInfo.InvariantCulture);
            });
            AssertFailure(Enumerate(profiles, CreateSpecialMaps()),
                SiteCandidateEnumerationErrorCode.InvalidStartRing);
        }

        [Test]
        public void Enumerator_RejectsNullAndDuplicateSpecialMaps()
        {
            var profiles = CreateProfiles();
            var maps = CreateSpecialMaps().ToList();
            maps.Add(null);
            maps.Add(maps[0]);
            var result = Enumerate(profiles, maps);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(SiteCandidateEnumerationErrorCode.NullSpecialMap));
            Assert.That(result.Errors.Select(error => error.ErrorCode), Does.Contain(SiteCandidateEnumerationErrorCode.DuplicateSpecialMapId));
            AssertSorted(result.Errors);
        }

        [Test]
        public void Enumerator_RejectsMissingAndInactiveExpectedSite()
        {
            AssertFailure(Enumerate(CreateProfiles(), CreateSpecialMaps(rows =>
                rows.RemoveAll(row => row[0] == BossId))), SiteCandidateEnumerationErrorCode.MissingRequiredSite);
            AssertFailure(Enumerate(CreateProfiles(), CreateSpecialMaps(rows =>
                FindRow(rows, ForgeId)[13] = "0")), SiteCandidateEnumerationErrorCode.MissingRequiredSite);
        }

        [TestCase(BossId, "FORGE")]
        [TestCase(ForgeId, "CORE_RESOURCE")]
        [TestCase(CassiaId, "BOSS")]
        public void Enumerator_RejectsRequiredRoleMismatch(string sourceId, string role)
        {
            AssertFailure(Enumerate(CreateProfiles(), CreateSpecialMaps(rows =>
                FindRow(rows, sourceId)[2] = role)), SiteCandidateEnumerationErrorCode.SiteRoleMismatch);
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(-1)]
        public void Enumerator_RejectsRequiredCountMismatch(int count)
        {
            AssertFailure(Enumerate(CreateProfiles(), CreateSpecialMaps(rows =>
                FindRow(rows, MeteorId)[6] = count.ToString(CultureInfo.InvariantCulture))),
                SiteCandidateEnumerationErrorCode.InvalidRequiredCount);
        }

        [TestCase(4, 0)]
        [TestCase(5, 14)]
        [TestCase(7, -1)]
        [TestCase(8, -1)]
        public void Enumerator_RejectsInvalidSiteDefinitionScalar(int field, int value)
        {
            AssertFailure(Enumerate(CreateProfiles(), CreateSpecialMaps(rows =>
                FindRow(rows, CassiaId)[field] = value.ToString(CultureInfo.InvariantCulture))),
                SiteCandidateEnumerationErrorCode.InvalidSiteDefinition);
        }

        [TestCase("")]
        [TestCase("0")]
        [TestCase("1|1")]
        [TestCase("1|4")]
        public void Enumerator_RejectsInvalidEntryRouteTypes(string routes)
        {
            AssertFailure(Enumerate(CreateProfiles(), CreateSpecialMaps(rows =>
                FindRow(rows, YeastId)[9] = routes)),
                SiteCandidateEnumerationErrorCode.InvalidSiteDefinition);
        }

        [TestCase("BOSS")]
        [TestCase("FORGE")]
        [TestCase("CORE_RESOURCE")]
        [TestCase("UNKNOWN")]
        public void Enumerator_RejectsUnexpectedActiveRequiredSite(string role)
        {
            AssertFailure(Enumerate(CreateProfiles(), CreateSpecialMaps(rows =>
            {
                var extra = ValidSiteRow("SITE_UNEXPECTED", role);
                rows.Add(extra);
            })), SiteCandidateEnumerationErrorCode.UnexpectedRequiredSite);
        }

        [Test]
        public void Enumerator_ExcludesActiveVillageWithoutError()
        {
            var result = Enumerate(0);
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Catalog.Groups.Any(group => group.Kind == SiteReservationKind.Village), Is.False);
        }

        [Test]
        public void Enumerator_ErrorOrderingIsSourceThenCodeThenMessageOrdinal()
        {
            var maps = CreateSpecialMaps(rows =>
            {
                FindRow(rows, MeteorId)[6] = "2";
                FindRow(rows, MeteorId)[4] = "0";
                rows.RemoveAll(row => row[0] == BossId);
            }).ToList();
            maps.Add(null);
            var result = Enumerate(CreateProfiles(), maps);
            AssertSorted(result.Errors);
        }

        [Test]
        public void PublicModelsAreSealedReadOnlyAndHaveNoMutableStaticState()
        {
            foreach (var type in new[]
                     {
                         typeof(SiteOriginCandidate), typeof(SiteCandidateGroup),
                         typeof(SiteCandidateCatalog), typeof(SiteCandidateEnumerationError),
                         typeof(SiteCandidateEnumerationResult), typeof(SiteCandidateEnumerator)
                     })
            {
                Assert.That(type.IsSealed, Is.True, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance), Is.Empty, type.FullName);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .All(property => property.SetMethod == null), Is.True, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .All(field => field.IsLiteral || field.IsInitOnly), Is.True, type.FullName);
            }
        }

        [Test]
        public void Enumerator_PublicDependencySurfaceContainsNoRngTransformSolverOrIoTypes()
        {
            var methods = typeof(SiteCandidateEnumerator).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            Assert.That(methods.Count(method => method.Name == nameof(SiteCandidateEnumerator.Enumerate)), Is.EqualTo(1));
            var signature = string.Join("|", methods.Select(method => method.ToString()));
            Assert.That(signature, Does.Not.Contain("Random"));
            Assert.That(signature, Does.Not.Contain("Transform"));
            Assert.That(signature, Does.Not.Contain("Solver"));
            Assert.That(signature, Does.Not.Contain("Stream"));
            Assert.That(signature, Does.Not.Contain("File"));
        }

        private static IEnumerable<SiteCandidateEnumerationErrorCode> AllErrorCodes()
        {
            return Enum.GetValues(typeof(SiteCandidateEnumerationErrorCode))
                .Cast<SiteCandidateEnumerationErrorCode>();
        }

        private static SiteCandidateEnumerationResult Enumerate(ulong seed)
        {
            return Enumerate(CreateProfiles(), CreateSpecialMaps(), seed);
        }

        private static SiteCandidateEnumerationResult Enumerate(
            ProfilePair profiles,
            IEnumerable<SpecialMapDefinition> maps,
            ulong seed = 4660)
        {
            return new SiteCandidateEnumerator().Enumerate(
                new GridInitializationPass().Execute(seed),
                profiles.World,
                profiles.Generation,
                maps);
        }

        private static void AssertFailure(
            SiteCandidateEnumerationResult result,
            SiteCandidateEnumerationErrorCode errorCode)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Catalog, Is.Null);
            Assert.That(result.Errors.Any(error => error.ErrorCode == errorCode), Is.True, FormatErrors(result));
            AssertSorted(result.Errors);
        }

        private static void AssertSorted(IReadOnlyList<SiteCandidateEnumerationError> errors)
        {
            for (var index = 1; index < errors.Count; index++)
            {
                var left = errors[index - 1];
                var right = errors[index];
                var comparison = string.Compare(
                    left.SourceDefinitionId,
                    right.SourceDefinitionId,
                    StringComparison.Ordinal);
                if (comparison == 0) comparison = left.ErrorCode.CompareTo(right.ErrorCode);
                if (comparison == 0)
                    comparison = string.Compare(left.Message, right.Message, StringComparison.Ordinal);
                Assert.That(comparison, Is.LessThanOrEqualTo(0),
                    "Error ordering mismatch at index " + index.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static string FormatErrors(SiteCandidateEnumerationResult result)
        {
            return string.Join("\n", result.Errors.Select(error =>
                error.SourceDefinitionId + " " + error.ErrorCode + " " + error.Message));
        }

        private static string Snapshot(SiteCandidateCatalog catalog)
        {
            return string.Join("|", catalog.Groups.Select(group =>
                ((int)group.Kind).ToString(CultureInfo.InvariantCulture) + ":" +
                group.SourceDefinitionId + ":" + group.RequiredInstanceOrdinal + ":" +
                string.Join(",", group.Candidates.Select(candidate =>
                    candidate.OriginIndex + "/" + candidate.EdgeRing + "/" + candidate.CandidateOrdinal))));
        }

        private static int EdgeRing(SectorCoord origin)
        {
            return Math.Min(
                Math.Min(origin.X, WorldGenConstants.SectorColumns - 1 - origin.X),
                Math.Min(origin.Y, WorldGenConstants.SectorRows - 1 - origin.Y));
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

        private static string SourceFor(SiteReservationKind kind)
        {
            switch (kind)
            {
                case SiteReservationKind.Boss: return BossId;
                case SiteReservationKind.Forge: return ForgeId;
                case SiteReservationKind.CoreResource: return CassiaId;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static SiteCandidateGroup CreateStartGroup()
        {
            var candidates = new List<SiteOriginCandidate>();
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var origin = WorldGridIndex.ToCoordinate(index);
                var ring = EdgeRing(origin);
                if (ring > 1) continue;
                candidates.Add(new SiteOriginCandidate(SiteReservationKind.Start, WorldId, 0,
                    origin, index, ring, candidates.Count));
            }
            return new SiteCandidateGroup(SiteReservationKind.Start, WorldId, 0, candidates);
        }

        private static SiteCandidateGroup CreateAllOriginGroup(SiteReservationKind kind, string sourceId)
        {
            return new SiteCandidateGroup(kind, sourceId, 0,
                Enumerable.Range(0, WorldGenConstants.SectorCount)
                    .Select(index => Candidate(kind, sourceId, index, index)));
        }

        private static List<SiteCandidateGroup> CreateSiteGroups()
        {
            return new List<SiteCandidateGroup>
            {
                CreateAllOriginGroup(SiteReservationKind.Boss, BossId),
                CreateAllOriginGroup(SiteReservationKind.Forge, ForgeId),
                CreateAllOriginGroup(SiteReservationKind.CoreResource, CassiaId),
                CreateAllOriginGroup(SiteReservationKind.CoreResource, YeastId),
                CreateAllOriginGroup(SiteReservationKind.CoreResource, MeteorId)
            };
        }

        private static SiteCandidateCatalog CreateCatalog(ulong seed)
        {
            return new SiteCandidateCatalog(seed, WorldId, GenerationId, CreateStartGroup(), CreateSiteGroups());
        }

        private static ProfilePair CreateProfiles(Action<string[], string[]> configure = null)
        {
            var worldRow = new[]
            {
                WorldId, "Moon Palace", "624", "416", "48", "32", "13", "13", "12", "8", "4", "4",
                "0", "0", "0", "0", "0", "0.25", "0", "1", "test"
            };
            var generationRow = new[]
            {
                GenerationId, WorldId, "0", "0", "0", "0", "0", "0", "0", "0", "0", "1",
                "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "1", "test"
            };
            configure?.Invoke(worldRow, generationRow);

            var sources = new List<WorldRouteDefinitionSource>();
            foreach (var spec in WorldSpecs)
            {
                IReadOnlyList<string[]> rows = null;
                if (spec.FileName == "world_profiles.csv") rows = new[] { worldRow };
                if (spec.FileName == "generation_profiles.csv") rows = new[] { generationRow };
                sources.Add(BuildWorldSource(spec, rows));
            }
            var result = new WorldRouteDefinitionBuilder().Build(sources);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            return new ProfilePair(
                result.DefinitionSet.WorldProfiles.Values.Single(),
                result.DefinitionSet.GenerationProfiles.Values.Single());
        }

        private static IReadOnlyList<SpecialMapDefinition> CreateSpecialMaps(
            Action<List<string[]>> configure = null)
        {
            var siteRows = new List<string[]>
            {
                ValidSiteRow(BossId, "BOSS", 2, 1),
                ValidSiteRow(ForgeId, "FORGE"),
                ValidSiteRow(CassiaId, "CORE_RESOURCE"),
                ValidSiteRow(YeastId, "CORE_RESOURCE"),
                ValidSiteRow(MeteorId, "CORE_RESOURCE"),
                ValidSiteRow("SITE_VILLAGE_SAMPLE", "VILLAGE")
            };
            configure?.Invoke(siteRows);

            var sources = new List<SpecialVillageDefinitionSource>();
            foreach (var spec in SpecialSpecs)
            {
                IReadOnlyList<string[]> rows = spec.FileName == "special_map_catalog.csv"
                    ? siteRows
                    : null;
                sources.Add(BuildSpecialSource(spec, rows));
            }
            var result = new SpecialVillageDefinitionBuilder().Build(sources);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            return result.DefinitionSet.SpecialMaps.Values.ToList();
        }

        private static string[] ValidSiteRow(
            string sourceId,
            string role,
            int width = 1,
            int height = 1)
        {
            return new[]
            {
                sourceId, "Site", role, "BIOME_MOON", width.ToString(CultureInfo.InvariantCulture),
                height.ToString(CultureInfo.InvariantCulture), "1", "0", "0", "1|2|3", "0",
                "REWARD_NONE", "FIXED", "1", "test"
            };
        }

        private static string[] FindRow(IEnumerable<string[]> rows, string sourceId)
        {
            return rows.Single(row => row[0] == sourceId);
        }

        private static WorldRouteDefinitionSource BuildWorldSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var parsed = Parse(spec, rows);
            return new WorldRouteDefinitionSource(parsed.Schema, parsed.Result);
        }

        private static SpecialVillageDefinitionSource BuildSpecialSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var parsed = Parse(spec, rows);
            return new SpecialVillageDefinitionSource(parsed.Schema, parsed.Result);
        }

        private static ParsedSource Parse(FileSpec spec, IReadOnlyList<string[]> rows)
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
            var read = new Rfc4180CsvReader().Read(new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            Assert.That(validation.Success, Is.True, string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            Assert.That(keys.Success, Is.True);
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            Assert.That(parsed.Success, Is.True, string.Join("\n", parsed.Errors));
            return new ParsedSource(schema, parsed);
        }

        private static string[] StandardRow(FileSpec spec)
        {
            return spec.Columns.Select((column, index) =>
            {
                var allowed = column.AllowedValues.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
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
                    case "ENUM_LIST": return "ENUM_A";
                    case "INT_LIST": return "1|2";
                    case "HEX": return "0x0A";
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

        private static FileSpec[] CreateWorldSpecs()
        {
            return new[]
            {
                File("world_profiles.csv", 1, "world_profile_id:ID", "display_name_ko:STRING", "width_tiles:INT", "height_tiles:INT", "sector_width_tiles:INT", "sector_height_tiles:INT", "sector_cols:INT", "sector_rows:INT", "micro_width_tiles:INT", "micro_height_tiles:INT", "micro_cols_per_sector:INT", "micro_rows_per_sector:INT", "min_completion_distance_tiles:INT", "max_shortest_completion_distance_tiles:INT", "normal_completion_min_tiles:INT", "normal_completion_max_tiles:INT", "optional_completion_max_tiles:INT", "max_revisit_ratio:FLOAT", "required_village_count:INT", "active:BOOL", "notes:STRING"),
                File("generation_profiles.csv", 1, "generation_profile_id:ID", "world_profile_id:ID", "mandatory_sector_min:INT", "mandatory_sector_max:INT", "type0_sector_min:INT", "type0_sector_max:INT", "reserved_sector_min:INT", "reserved_sector_max:INT", "inactive_sector_min:INT", "inactive_sector_max:INT", "start_edge_ring_min:INT", "start_edge_ring_max:INT", "mandatory_loop_min:INT", "mandatory_loop_max:INT", "optional_region_depth_min:INT", "optional_region_depth_max:INT", "optional_region_count_min:INT", "optional_region_count_max:INT", "site_reservation_retry_max:INT", "biome_retry_max:INT", "route_retry_max:INT", "sector_solve_retry_max:INT", "active:BOOL", "notes:STRING"),
                File("generation_passes.csv", 3, "generation_profile_id:ID", "pass_order:INT", "pass_id:ID", "class_name:STRING", "rng_stream_id:ID", "input_artifacts:ID_LIST", "output_artifacts:ID_LIST", "failure_policy:ENUM", "max_retry_count:INT", "enabled:BOOL", "notes:STRING"),
                File("rng_streams.csv", 1, "rng_stream_id:ID", "salt_hex:HEX", "reset_scope:ENUM", "description_ko:STRING", "active:BOOL"),
                File("sector_route_masks.csv", 1, "route_mask_id:ID", "route_type:INT", "open_l:BOOL", "open_r:BOOL", "open_u:BOOL", "open_d:BOOL", "mandatory_allowed:BOOL", "description_ko:STRING", "active:BOOL"),
                File("socket_band_definitions.csv", 1, "band_id:ID", "axis:ENUM", "min_local_coord:INT", "max_local_coord:INT", "recommended_center:FLOAT", "minimum_clearance_tiles:INT", "description_ko:STRING"),
                File("edge_signatures.csv", 1, "edge_signature_id:ID", "axis:ENUM", "band_id:ID", "traversal_kind:ENUM", "ground_entry_height:INT", "clearance_width:INT", "clearance_height:INT", "tool_requirement:ENUM", "mandatory_allowed:BOOL", "tags:ID_LIST", "notes:STRING"),
                File("edge_signature_compatibility.csv", 2, "signature_a:ID", "signature_b:ID", "compatible:BOOL", "adapter_microchunk_pool_id:ID", "notes:STRING"),
                File("sector_recipe_catalog.csv", 1, "sector_recipe_id:ID", "display_name_ko:STRING", "route_type:INT", "route_mask_id:ID", "primary_biome_id:ID", "secondary_biome_id:ID", "boundary_profile_id:ID", "recipe_kind:ENUM", "microchunk_budget_profile_id:ID", "selection_weight:INT", "supports_special_entry:BOOL", "supports_village_entry:BOOL", "active:BOOL", "notes:STRING"),
                File("sector_recipe_cells.csv", 3, "sector_recipe_id:ID", "chunk_x:INT", "chunk_y:INT", "cell_role:ENUM", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_usage_class:ENUM_LIST", "required_route_roles:ID_LIST", "required_biome_ids:ID_LIST", "required_signature_l:ID", "required_signature_r:ID", "required_signature_u:ID", "required_signature_d:ID", "transform_policy:ENUM_LIST:R0|MIRROR_X|MIRROR_Y|R180", "notes:STRING"),
                File("sector_recipe_paths.csv", 3, "sector_recipe_id:ID", "path_id:ID", "path_order:INT", "chunk_x:INT", "chunk_y:INT", "enter_side:ENUM", "exit_side:ENUM", "mandatory:BOOL", "traversal_kind:ENUM", "max_jump_tiles:INT", "notes:STRING"),
                File("sector_external_sockets.csv", 2, "sector_recipe_id:ID", "socket_id:ID", "side:ENUM", "edge_chunk_index:INT", "band_id:ID", "traversal_kind:ENUM", "mandatory_allowed:BOOL", "edge_signature_id:ID", "notes:STRING"),
                File("sector_recipe_pool_entries.csv", 3, "sector_recipe_pool_id:ID", "entry_order:INT", "sector_recipe_id:ID", "weight:INT", "min_repeat_distance_sectors:INT", "required_patch_role:ENUM", "active:BOOL")
            };
        }

        private static FileSpec[] CreateSpecialSpecs()
        {
            return new[]
            {
                File("event_activation_routes.csv", 1, "event_route_id:ID", "special_map_id:ID", "event_id:ID", "mandatory:BOOL", "allowed_sector_types:INT_LIST", "requires_tool:BOOL", "requires_consumable:BOOL", "min_safe_tiles_before_trigger:INT", "return_path_required:BOOL", "trigger_slot_id:ID", "notes:STRING"),
                File("special_map_catalog.csv", 1, "special_map_id:ID", "display_name_ko:STRING", "site_role:ENUM:BOSS|FORGE|CORE_RESOURCE|VILLAGE|UNKNOWN", "primary_biome_id:ID", "footprint_width_sectors:INT", "footprint_height_sectors:INT", "required_count:INT", "min_graph_distance_from_start:INT", "min_graph_distance_to_other_core_sites:INT", "allowed_entry_route_types:INT_LIST", "requires_tool:BOOL", "mandatory_reward_id:ID", "generation_mode:ENUM:FIXED|GENERATED", "active:BOOL", "notes:STRING"),
                File("special_map_entry_sockets.csv", 2, "special_map_id:ID", "entry_socket_id:ID", "local_sector_x:INT", "local_sector_y:INT", "side:ENUM", "allowed_route_types:INT_LIST", "required:BOOL", "return_path_required:BOOL", "notes:STRING"),
                File("special_map_footprint_cells.csv", 3, "special_map_id:ID", "local_sector_x:INT", "local_sector_y:INT", "local_role:ENUM", "required_primary_biome_id:ID", "fixed_sector_recipe_id:ID", "required_open_sides:ENUM_LIST", "notes:STRING"),
                File("special_map_rewards.csv", 2, "special_map_id:ID", "reward_order:INT", "reward_id:ID", "reward_kind:ENUM", "mandatory:BOOL", "slot_id:ID", "quantity_min:INT", "quantity_max:INT", "notes:STRING"),
                File("shop_archetypes.csv", 1, "shop_archetype_id:ID", "display_name_ko:STRING", "shop_type:ENUM", "item_slot_count_min:INT", "item_slot_count_max:INT", "base_price_multiplier:FLOAT", "allows_reputation_reward:BOOL", "active:BOOL", "notes:STRING"),
                File("shop_inventory_rules.csv", 2, "shop_archetype_id:ID", "slot_index:INT", "spawn_pool_id:ID", "guaranteed:BOOL", "quantity_min:INT", "quantity_max:INT", "price_min_gold:INT", "price_max_gold:INT", "required_favor_tier:INT", "active:BOOL", "notes:STRING"),
                File("shopkeeper_species.csv", 1, "species_id:ID", "display_name_ko:STRING", "prefab_id:ID", "dialogue_style_id:ID", "animation_set_id:ID", "selection_weight:INT", "allowed_biome_ids:ID_LIST", "active:BOOL", "notes:STRING"),
                File("village_facilities.csv", 1, "facility_id:ID", "display_name_ko:STRING", "facility_group:ENUM", "fixed:BOOL", "selection_weight:INT", "prefab_id:ID", "shop_archetype_id:ID", "evacuated_prefab_id:ID", "active:BOOL", "notes:STRING"),
                File("village_layout_catalog.csv", 1, "village_layout_id:ID", "display_name_ko:STRING", "footprint_width_sectors:INT", "footprint_height_sectors:INT", "target_facility_count:INT", "entry_sides:ENUM_LIST", "selection_weight:INT", "active:BOOL", "notes:STRING"),
                File("village_layout_cells.csv", 3, "village_layout_id:ID", "local_chunk_x:INT", "local_chunk_y:INT", "cell_role:ENUM", "facility_slot_id:ID", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_entry_side:ENUM", "notes:STRING"),
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

        private sealed class ProfilePair
        {
            public ProfilePair(WorldProfileDefinition world, GenerationProfileDefinition generation)
            {
                World = world;
                Generation = generation;
            }

            public WorldProfileDefinition World { get; }
            public GenerationProfileDefinition Generation { get; }
        }

        private sealed class ParsedSource
        {
            public ParsedSource(CsvFileSchema schema, CsvScalarAndListParseResult result)
            {
                Schema = schema;
                Result = result;
            }

            public CsvFileSchema Schema { get; }
            public CsvScalarAndListParseResult Result { get; }
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
