using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP14_01")]
    public sealed class SectorPlannerInputAndPacingRoleTests
    {
        private PlannerFixtureSet fixtures;

        [SetUp]
        public void SetUp()
        {
            fixtures = PlannerFixtureSet.Create();
        }

        [Test]
        public void BuildPublishesImmutableCanonicalSectorPlannerInput()
        {
            var mutable = fixtures.ValidSectors.ToList();
            var request = fixtures.Request(mutable);
            mutable.Clear();

            var result = SectorPlannerInputBuilder.Build(request);

            Assert.That(result.Success, Is.True, JoinErrors(result));
            Assert.That(result.Input.Sectors.Count, Is.EqualTo(9));
            Assert.That(result.Input.Sectors.All(value => value.CanvasWidth == 48), Is.True);
            Assert.That(result.Input.Sectors.All(value => value.CanvasHeight == 32), Is.True);
            Assert.That(result.Input.PublicationLabel, Is.EqualTo("REFERENCE PLANNER INPUT"));
            Assert.That(result.Input.Authority.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.CanonicalDigest, Is.EqualTo(SectorPlannerInputCanonicalDigest.Compute(result.Input)));
            Assert.That(result.Input.Sectors.Select(value => value.SectorIndex), Is.Ordered);
            Assert.That(
                () => ((IList<SectorPlannerSectorSnapshot>)result.Input.Sectors).Add(fixtures.PlainTraversalBoundarySector),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void BuildConsumesCurrentPublicAuthoritiesWithoutReparsingOrMutation()
        {
            var result = fixtures.BuildValid();

            Assert.That(result.Success, Is.True, JoinErrors(result));
            var authority = result.Input.Authority;
            Assert.That(authority.GenerationLayerDigest, Is.EqualTo(GenerationLayerCatalog.StableDigest));
            Assert.That(authority.MicroPatternDigest, Is.EqualTo(PlannerFixtureSet.MicroPatternDigest));
            Assert.That(authority.MicroPatternCount, Is.EqualTo(24));
            Assert.That(authority.TerrainClusterDigest, Is.EqualTo(PlannerFixtureSet.TerrainClusterDigest));
            Assert.That(authority.TerrainClusterCount, Is.EqualTo(16));
            Assert.That(authority.ActivityDigest, Is.EqualTo(PlannerFixtureSet.ActivityDigest));
            Assert.That(authority.ActivityCount, Is.EqualTo(7));
            Assert.That(authority.EventDigest, Is.EqualTo(PlannerFixtureSet.EventDigest));
            Assert.That(authority.EventCount, Is.EqualTo(5));
            Assert.That(authority.SpecialRegionAuditDigest, Is.EqualTo(PlannerFixtureSet.SpecialRegionAuditDigest));
            Assert.That(authority.CoreResourceCatalogDigest, Is.EqualTo(CoreResourceRegionStarterCatalog.CanonicalDigest));
            Assert.That(authority.CoreResourceCount, Is.EqualTo(CoreResourceRegionStarterCatalog.Entries.Count));
            Assert.That(authority.SpecialLandmarkCatalogDigest, Is.EqualTo(SpecialLandmarkRegionStarterCatalog.CanonicalDigest));
            Assert.That(authority.SpecialLandmarkCount, Is.EqualTo(SpecialLandmarkRegionStarterCatalog.Entries.Count));
            Assert.That(result.Input.CsvReparseCount, Is.Zero);
            Assert.That(result.Input.GeneratedWriteCount, Is.Zero);
            Assert.That(result.Input.SceneMutationCount, Is.Zero);
            Assert.That(result.Input.AssetMutationCount, Is.Zero);
            Assert.That(result.Input.SolverInvocationCount, Is.Zero);
            Assert.That(result.Input.RandomDrawCount, Is.Zero);
        }

        [Test]
        public void BuildRejectsInvalidDuplicateMissingAndMutationClaimInputsAtomically()
        {
            var missing = SectorPlannerInputBuilder.Build(null);
            var duplicate = SectorPlannerInputBuilder.Build(fixtures.Request(new[]
            {
                fixtures.PlainTraversalBoundarySector,
                fixtures.PlainTraversalBoundarySector,
            }));
            var undefined = SectorPlannerInputBuilder.Build(fixtures.Request(new[] { fixtures.InvalidInputCases }));
            var coupled = SectorPlannerInputBuilder.Build(new SectorPlannerInputRequest(
                new[] { fixtures.PlainTraversalBoundarySector },
                fixtures.Authority,
                SectorPlannerInputBuilder.ReferencePublicationLabel,
                csvReparseCount: 1,
                generatedWriteCount: 1,
                sceneMutationCount: 1,
                assetMutationCount: 1,
                solverInvocationCount: 1,
                randomDrawCount: 1,
                pacingChangesAccess: true,
                pacingChangesRoute: true));

            var results = new[] { missing, duplicate, undefined, coupled };
            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Input == null), Is.True);
            Assert.That(results.All(value => value.CanonicalDigest == string.Empty), Is.True);
            Assert.That(missing.Errors.Select(value => value.Code), Does.Contain(SectorPlannerInputErrorCode.MissingInput));
            Assert.That(duplicate.Errors.Select(value => value.Code), Does.Contain(SectorPlannerInputErrorCode.DuplicateSector));
            Assert.That(undefined.Errors.Select(value => value.Code), Does.Contain(SectorPlannerInputErrorCode.PacingRoleUndefined));
            Assert.That(coupled.Errors.Select(value => value.Code), Does.Contain(SectorPlannerInputErrorCode.MutationClaim));
            Assert.That(coupled.Errors.Select(value => value.Code), Does.Contain(SectorPlannerInputErrorCode.PacingAccessCoupling));
            Assert.That(coupled.Errors.Select(value => value.Code), Does.Contain(SectorPlannerInputErrorCode.PacingRouteMutationClaim));
            foreach (var result in results)
            {
                Assert.That(result.Errors, Is.EqualTo(result.Errors.Distinct().OrderBy(value => value).ToArray()));
            }
        }

        [Test]
        public void PacingRoleAssignmentKeepsAccessRouteAndBoundaryIdentityUnchanged()
        {
            var input = fixtures.BuildValid().Input;
            var before = input.Sectors.ToDictionary(
                value => value.SectorIndex,
                value => Identity(value),
                EqualityComparer<int>.Default);

            var assignments = SectorPacingRolePlanner.Assign(input);

            Assert.That(assignments.Count, Is.EqualTo(9));
            Assert.That(input.Sectors.All(value => Identity(value) == before[value.SectorIndex]), Is.True);
            Assert.That(assignments.All(value => value.RouteMutationCount == 0), Is.True);
            Assert.That(assignments.All(value => value.AccessMutationCount == 0), Is.True);
            Assert.That(assignments.All(value => value.SocketMutationCount == 0), Is.True);
            Assert.That(assignments.All(value => value.BoundaryMutationCount == 0), Is.True);
            Assert.That(assignments.All(value => value.SiteMutationCount == 0), Is.True);
            Assert.That(assignments.All(value => value.CatalogMutationCount == 0), Is.True);
            Assert.That(assignments.All(value => value.SourceIdentityDigest.Length == 64), Is.True);
        }

        [Test]
        public void MandatoryResourceBossAndLandmarkReceiveHardPriorityRoles()
        {
            var input = fixtures.BuildValid().Input;
            var resource = SectorPacingRolePlanner.Assign(input, fixtures.CoreResourceSector.Coordinate);
            var forge = SectorPacingRolePlanner.Assign(input, fixtures.ForgeLandmarkSector.Coordinate);
            var boss = SectorPacingRolePlanner.Assign(input, fixtures.BossGateSector.Coordinate);

            Assert.That(resource.PrimaryRole, Is.EqualTo(PacingRole.Resource));
            Assert.That(resource.Candidates.Single().HardPriorityClass, Is.EqualTo(90));
            Assert.That(forge.PrimaryRole, Is.EqualTo(PacingRole.Landmark));
            Assert.That(forge.Candidates.Select(value => value.Role), Is.EqualTo(new[]
            {
                PacingRole.Landmark,
                PacingRole.Machinery,
            }));
            Assert.That(boss.PrimaryRole, Is.EqualTo(PacingRole.Boss));
            Assert.That(boss.Candidates.Single().HardPriorityClass, Is.EqualTo(100));
        }

        [Test]
        public void VillageAndOptionalDeferredDoNotBecomeProgressionBlockers()
        {
            var input = fixtures.BuildValid().Input;
            var village = input.Sectors.Single(value => value.SectorIndex == fixtures.VillageReferenceSector.SectorIndex);
            var optional = input.Sectors.Single(value => value.SectorIndex == fixtures.DeferredOptionalSector.SectorIndex);
            var villageAssignment = SectorPacingRolePlanner.Assign(input, village.Coordinate);
            var optionalAssignment = SectorPacingRolePlanner.Assign(input, optional.Coordinate);

            Assert.That(village.SpecialRegion.Binding, Is.EqualTo(SectorPlannerSpecialRegionBinding.ReferenceOnly));
            Assert.That(village.SpecialRegion.Reserved, Is.False);
            Assert.That(village.SpecialRegion.PlacedOwnershipClaim, Is.False);
            Assert.That(village.SpecialRegion.MandatoryProgressionDependency, Is.False);
            Assert.That(villageAssignment.PrimaryRole, Is.EqualTo(PacingRole.Safe));
            Assert.That(optional.SpecialRegion.Binding, Is.EqualTo(SectorPlannerSpecialRegionBinding.DeferredOptionalLocal));
            Assert.That(optional.SpecialRegion.Reserved, Is.False);
            Assert.That(optional.SpecialRegion.PlacedOwnershipClaim, Is.False);
            Assert.That(optional.OptionalRegions.Single().PlacedOwnershipClaim, Is.False);
            Assert.That(optionalAssignment.PrimaryRole, Is.EqualTo(PacingRole.Discovery));
            Assert.That(optionalAssignment.PlacementCount, Is.Zero);
        }

        [Test]
        public void BoundaryRouteRecoveryAndNeighborFactsProduceReasonsOnly()
        {
            var input = fixtures.BuildValid().Input;
            var boundary = SectorPacingRolePlanner.Assign(input, fixtures.PlainTraversalBoundarySector.Coordinate);
            var neighbor = SectorPacingRolePlanner.Assign(input, fixtures.NeighborInfluencedSector.Coordinate);

            Assert.That(boundary.PrimaryRole, Is.EqualTo(PacingRole.Traversal));
            Assert.That(boundary.Candidates.Select(value => value.Role), Is.EqualTo(new[]
            {
                PacingRole.Traversal,
                PacingRole.Recovery,
            }));
            Assert.That(boundary.Reasons, Does.Contain(SectorPacingReason.BoundaryWarning));
            Assert.That(boundary.Reasons, Does.Contain(SectorPacingReason.RouteRecoveryNeed));
            Assert.That(neighbor.PrimaryRole, Is.EqualTo(PacingRole.Traversal));
            Assert.That(neighbor.Reasons, Does.Contain(SectorPacingReason.NeighborPacingContext));
            Assert.That(input.Sectors.Single(value => value.SectorIndex == fixtures.NeighborInfluencedSector.SectorIndex).Neighbors.Count, Is.EqualTo(4));
            Assert.That(boundary.PlacementCount + neighbor.PlacementCount, Is.Zero);
            Assert.That(boundary.RouteMutationCount + neighbor.RouteMutationCount, Is.Zero);
            Assert.That(boundary.BoundaryMutationCount + neighbor.BoundaryMutationCount, Is.Zero);
        }

        [Test]
        public void ActivityEventAvailabilityProducesCandidateOnly()
        {
            var input = fixtures.BuildValid().Input;
            var assignment = SectorPacingRolePlanner.Assign(input, fixtures.ActivityCompatibleSector.Coordinate);

            Assert.That(assignment.PrimaryRole, Is.EqualTo(PacingRole.Activity));
            Assert.That(assignment.Candidates.Select(value => value.Role), Is.EqualTo(new[] { PacingRole.Activity }));
            Assert.That(assignment.Reasons, Does.Contain(SectorPacingReason.ActivityCatalogAvailable));
            Assert.That(assignment.Reasons, Does.Contain(SectorPacingReason.EventCatalogAvailable));
            Assert.That(assignment.PlacementCount, Is.Zero);
            Assert.That(assignment.MarkerCount, Is.Zero);
            Assert.That(assignment.SpawnCount, Is.Zero);
        }

        [Test]
        public void WorldProgressAndLandmarkDistanceInfluenceTieBreakDeterministically()
        {
            var early = fixtures.BuildVillageProbe(2, 4, 0);
            var late = fixtures.BuildVillageProbe(8, 4, 0);
            var safeDistance = fixtures.BuildVillageProbe(5, 6, 0);
            var landmarkDistance = fixtures.BuildVillageProbe(5, 0, 6);

            Assert.That(SectorPacingRolePlanner.Assign(early, new SectorCoord(2, 2)).PrimaryRole, Is.EqualTo(PacingRole.Safe));
            Assert.That(SectorPacingRolePlanner.Assign(late, new SectorCoord(2, 2)).PrimaryRole, Is.EqualTo(PacingRole.Landmark));
            Assert.That(SectorPacingRolePlanner.Assign(safeDistance, new SectorCoord(2, 2)).PrimaryRole, Is.EqualTo(PacingRole.Safe));
            Assert.That(SectorPacingRolePlanner.Assign(landmarkDistance, new SectorCoord(2, 2)).PrimaryRole, Is.EqualTo(PacingRole.Landmark));
            Assert.That(new[] { early, late, safeDistance, landmarkDistance }.All(value => value.RandomDrawCount == 0), Is.True);
        }

        [Test]
        public void PacingPublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = fixtures.BuildValid().Input;
                var repeat = fixtures.BuildValid().Input;
                var reverse = SectorPlannerInputBuilder.Build(fixtures.Request(fixtures.ValidSectors.Reverse())).Input;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var turkish = SectorPlannerInputBuilder.Build(fixtures.Request(fixtures.ValidSectors.Reverse())).Input;

                Assert.That(repeat.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(reverse.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(turkish.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                var firstAssignments = SectorPacingRolePlanner.Assign(first).Select(value => value.CanonicalDigest).ToArray();
                Assert.That(SectorPacingRolePlanner.Assign(repeat).Select(value => value.CanonicalDigest), Is.EqualTo(firstAssignments));
                Assert.That(SectorPacingRolePlanner.Assign(reverse).Select(value => value.CanonicalDigest), Is.EqualTo(firstAssignments));
                Assert.That(SectorPacingRolePlanner.Assign(turkish).Select(value => value.CanonicalDigest), Is.EqualTo(firstAssignments));
                Assert.That(SectorPacingRolePlanner.Assign(first).Sum(value => value.Candidates.Count), Is.EqualTo(12));
                Assert.That(SectorPacingRolePlanner.Assign(first).Sum(value => value.Reasons.Count), Is.EqualTo(12));
                Assert.That(SectorPacingRolePlanner.Assign(first).All(value => value.RandomDrawCount == 0), Is.True);

                var assignments = SectorPacingRolePlanner.Assign(first);
                var names = new[]
                {
                    "PlainTraversalBoundarySector",
                    "QuietBufferSector",
                    "VillageReferenceSector",
                    "CoreResourceSector",
                    "ForgeLandmarkSector",
                    "BossGateSector",
                    "ActivityCompatibleSector",
                    "DeferredOptionalSector",
                    "NeighborInfluencedSector",
                };
                TestContext.WriteLine("fixtureMatrix=10 validSectors=9 invalidFixtureGroups=4 canvas=48x32 candidates=12 reasons=12");
                TestContext.WriteLine("inputDigest=" + first.CanonicalDigest + " authorityDigest=" + first.Authority.CanonicalDigest);
                for (var index = 0; index < assignments.Count; index++)
                {
                    TestContext.WriteLine(names[index]
                                          + " primary=" + assignments[index].PrimaryRole
                                          + " candidates=" + assignments[index].Candidates.Count
                                          + " reasons=" + assignments[index].Reasons.Count
                                          + " digest=" + assignments[index].CanonicalDigest);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        private static string Identity(SectorPlannerSectorSnapshot sector)
        {
            return string.Join("|", new[]
            {
                sector.Route.RouteType.ToString(CultureInfo.InvariantCulture),
                sector.Route.AccessClass.ToString(),
                string.Join(",", sector.Route.ExternalSockets),
                string.Join(",", sector.Boundaries.Select(value => value.PairId + ":" + value.CandidateId)),
                string.Join(",", sector.Sites.Select(value => value.SiteId + ":" + value.ReservationId)),
                sector.SpecialRegion.RegionId,
                sector.SpecialRegion.Binding.ToString(),
            });
        }

        private static string JoinErrors(SectorPlannerInputBuildResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }
    }

    internal sealed class PlannerFixtureSet
    {
        public const string MicroPatternDigest = "6a5aefd2eb368348d594158cc3f14e94d0ea509ea2cdd207a7715e8da80d19ac";
        public const string TerrainClusterDigest = "9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7";
        public const string ActivityDigest = "3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a";
        public const string EventDigest = "2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0";
        public const string SpecialRegionAuditDigest = "a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e";

        private PlannerFixtureSet()
        {
            Authority = SectorPlannerAuthorityDigestSnapshot.CaptureCurrentPublicAuthorities(
                Digest("MAP00_08_REFERENCE_SUMMARY"),
                MicroPatternDigest,
                24,
                TerrainClusterDigest,
                16,
                ActivityDigest,
                7,
                EventDigest,
                5,
                SpecialRegionAuditDigest);

            PlainTraversalBoundarySector = Sector(
                0, 0,
                new[] { PacingRole.Traversal, PacingRole.Recovery },
                boundaries: new[] { new SectorPlannerBoundarySnapshot(SectorPlannerSide.Right, "BP_000_R", "BC_000_R_01", 1) },
                highRoute: true,
                recoveryNeeded: true);
            QuietBufferSector = Sector(1, 0, new[] { PacingRole.Quiet }, quiet: true);
            VillageReferenceSector = Sector(
                2, 0,
                new[] { PacingRole.Safe, PacingRole.Landmark },
                special: new SectorPlannerSpecialRegionSnapshot(
                    "SR_VILLAGE_REFERENCE_01",
                    SectorPlannerSpecialRegionKind.Village,
                    SectorPlannerSpecialRegionBinding.ReferenceOnly,
                    "VILLAGE_REFERENCE_SHELL",
                    false,
                    false,
                    false),
                progress: Progress(2, 4, 0));
            CoreResourceSector = Sector(
                3, 0,
                new[] { PacingRole.Resource },
                sites: new[] { new SectorPlannerSiteSnapshot("SITE_CORE_01", "CORE_RESOURCE", "RES_CORE_01", true) },
                special: Mandatory("SR_CORE_RESOURCE_01", SectorPlannerSpecialRegionKind.CoreResource, "FP_CORE_01"),
                progress: Progress(5, 0, 3));
            ForgeLandmarkSector = Sector(
                4, 0,
                new[] { PacingRole.Landmark, PacingRole.Machinery },
                sites: new[] { new SectorPlannerSiteSnapshot("SITE_FORGE_01", "FORGE_LANDMARK", "RES_FORGE_01", true) },
                special: Mandatory("SR_FORGE_01", SectorPlannerSpecialRegionKind.Forge, "FP_FORGE_01"),
                progress: Progress(7, 0, 3));
            BossGateSector = Sector(
                5, 0,
                new[] { PacingRole.Boss },
                sites: new[] { new SectorPlannerSiteSnapshot("SITE_BOSS_01", "BOSS_GATE", "RES_BOSS_01", true) },
                special: Mandatory("SR_BOSS_01", SectorPlannerSpecialRegionKind.Boss, "FP_BOSS_01"),
                progress: Progress(10, 0, 5));
            ActivityCompatibleSector = Sector(
                6, 0,
                new[] { PacingRole.Activity },
                activityAvailable: true,
                eventAvailable: true,
                progress: Progress(5, 2, 2));
            DeferredOptionalSector = Sector(
                7, 0,
                new[] { PacingRole.Discovery },
                special: new SectorPlannerSpecialRegionSnapshot(
                    "SR_MERCHANT_DEFERRED_01",
                    SectorPlannerSpecialRegionKind.Merchant,
                    SectorPlannerSpecialRegionBinding.DeferredOptionalLocal,
                    string.Empty,
                    false,
                    false,
                    false),
                optional: new[]
                {
                    new SectorPlannerOptionalRegionSnapshot(
                        "SR_MERCHANT_DEFERRED_01",
                        SectorPlannerSpecialRegionKind.Merchant,
                        true,
                        true,
                        false),
                },
                progress: Progress(4, 2, 1));
            NeighborInfluencedSector = Sector(
                8, 5,
                new[] { PacingRole.Traversal },
                neighbors: new[]
                {
                    Neighbor(SectorPlannerSide.Left, 7, 5, "L_OUT"),
                    Neighbor(SectorPlannerSide.Right, 9, 5, "R_OUT"),
                    Neighbor(SectorPlannerSide.Up, 8, 4, "U_OUT"),
                    Neighbor(SectorPlannerSide.Down, 8, 6, "D_OUT"),
                },
                progress: Progress(6, 3, 2));
            InvalidInputCases = Sector(9, 0, new[] { PacingRole.None });

            ValidSectors = new[]
            {
                PlainTraversalBoundarySector,
                QuietBufferSector,
                VillageReferenceSector,
                CoreResourceSector,
                ForgeLandmarkSector,
                BossGateSector,
                ActivityCompatibleSector,
                DeferredOptionalSector,
                NeighborInfluencedSector,
            };
        }

        public SectorPlannerAuthorityDigestSnapshot Authority { get; }
        public SectorPlannerSectorSnapshot PlainTraversalBoundarySector { get; }
        public SectorPlannerSectorSnapshot QuietBufferSector { get; }
        public SectorPlannerSectorSnapshot VillageReferenceSector { get; }
        public SectorPlannerSectorSnapshot CoreResourceSector { get; }
        public SectorPlannerSectorSnapshot ForgeLandmarkSector { get; }
        public SectorPlannerSectorSnapshot BossGateSector { get; }
        public SectorPlannerSectorSnapshot ActivityCompatibleSector { get; }
        public SectorPlannerSectorSnapshot DeferredOptionalSector { get; }
        public SectorPlannerSectorSnapshot NeighborInfluencedSector { get; }
        public SectorPlannerSectorSnapshot InvalidInputCases { get; }
        public IReadOnlyList<SectorPlannerSectorSnapshot> ValidSectors { get; }

        public static PlannerFixtureSet Create() => new PlannerFixtureSet();

        public SectorPlannerInputRequest Request(IEnumerable<SectorPlannerSectorSnapshot> sectors)
        {
            return new SectorPlannerInputRequest(
                sectors,
                Authority,
                SectorPlannerInputBuilder.ReferencePublicationLabel);
        }

        public SectorPlannerInputBuildResult BuildValid()
        {
            return SectorPlannerInputBuilder.Build(Request(ValidSectors));
        }

        public SectorPlannerInput BuildVillageProbe(
            int ordinal,
            int mandatoryDistance,
            int optionalDistance)
        {
            var sector = Sector(
                2, 2,
                new[] { PacingRole.Safe, PacingRole.Landmark },
                special: new SectorPlannerSpecialRegionSnapshot(
                    "SR_VILLAGE_PROBE",
                    SectorPlannerSpecialRegionKind.Village,
                    SectorPlannerSpecialRegionBinding.ReferenceOnly,
                    "VILLAGE_REFERENCE_SHELL",
                    false,
                    false,
                    false),
                progress: Progress(ordinal, mandatoryDistance, optionalDistance));
            var result = SectorPlannerInputBuilder.Build(Request(new[] { sector }));
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            return result.Input;
        }

        private static SectorPlannerSectorSnapshot Sector(
            int x,
            int y,
            IEnumerable<PacingRole> roles,
            IEnumerable<SectorPlannerBoundarySnapshot> boundaries = null,
            IEnumerable<SectorPlannerSiteSnapshot> sites = null,
            SectorPlannerSpecialRegionSnapshot special = null,
            IEnumerable<SectorPlannerOptionalRegionSnapshot> optional = null,
            IEnumerable<SectorPlannerNeighborSnapshot> neighbors = null,
            SectorPlannerWorldProgressSnapshot progress = null,
            bool highRoute = false,
            bool recoveryNeeded = false,
            bool quiet = false,
            bool activityAvailable = false,
            bool eventAvailable = false)
        {
            var coordinate = new SectorCoord(x, y);
            return new SectorPlannerSectorSnapshot(
                coordinate,
                (y * WorldGenConstants.SectorColumns) + x,
                WorldGenConstants.SectorWidthTiles,
                WorldGenConstants.SectorHeightTiles,
                new SectorPlannerBiomeSnapshot("PATCH_" + y.ToString("D2") + "_" + x.ToString("D2"), "MOON_PALACE"),
                new SectorPlannerRouteSnapshot(
                    1,
                    AccessClass.MandatoryNoTool,
                    new[] { "SOCKET_" + y.ToString("D2") + "_" + x.ToString("D2") },
                    highRoute,
                    recoveryNeeded),
                boundaries,
                sites,
                special ?? SectorPlannerSpecialRegionSnapshot.None,
                optional,
                neighbors,
                progress ?? Progress(3, 3, 3),
                roles,
                quiet,
                activityAvailable,
                eventAvailable);
        }

        private static SectorPlannerSpecialRegionSnapshot Mandatory(
            string id,
            SectorPlannerSpecialRegionKind kind,
            string footprint)
        {
            return new SectorPlannerSpecialRegionSnapshot(
                id,
                kind,
                SectorPlannerSpecialRegionBinding.ReservedMandatory,
                footprint,
                true,
                true,
                true);
        }

        private static SectorPlannerWorldProgressSnapshot Progress(int ordinal, int mandatory, int optional)
        {
            return new SectorPlannerWorldProgressSnapshot(ordinal, "CHAPTER_" + ordinal, "MAIN", mandatory, optional);
        }

        private static SectorPlannerNeighborSnapshot Neighbor(
            SectorPlannerSide side,
            int x,
            int y,
            string socket)
        {
            return new SectorPlannerNeighborSnapshot(
                side,
                new SectorCoord(x, y),
                1,
                AccessClass.MandatoryNoTool,
                new[] { socket },
                PacingRole.Traversal);
        }

        private static string Digest(string value)
        {
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
            {
                return string.Concat(algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))
                    .Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }
}
