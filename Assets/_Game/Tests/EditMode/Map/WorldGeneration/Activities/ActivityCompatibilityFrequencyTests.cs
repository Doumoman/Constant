using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Activities
{
    [TestFixture]
    [Category("MAP12_03")]
    public sealed class ActivityCompatibilityFrequencyTests
    {
        private const string CatalogDigest = "9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7";
        private const string SignatureDigest = "2884a639d9cef923e8b86a7fba2c0430cdfad2de11a63fd138d51dacdce13d8a";
        private const string ManifestDigest = "ff4761537986a4c9433775359d9b62ad806914ef30462a320c97b355126a5b6c";
        private const string ShellDigest = "22a61392b9e1474c65dcf089f5caf1d14e20eb19250e1c8e06886143fd12fdd4";
        private const string SafetyDigest = "5c9c27d0e52b9465a9fcc0ab3c83b51aa968a8088cf55b22a026ce2ea6934334";

        [Test]
        public void EligibleCandidate_PublishesExactRepresentativeIdentity()
        {
            var result = Compile(new[] { Profile("ACTIVITY_ORDINARY") }, new[] { Opportunity(0) });

            Assert.That(result.Success, Is.True, Errors(result.Errors));
            Assert.That(result.Index.CandidateCount, Is.EqualTo(1));
            var candidate = result.Index.Candidates[0];
            Assert.That(candidate.TerrainClusterId.Value, Is.EqualTo("TC_CRATER_BOWL_ASCENT"));
            Assert.That(candidate.SpineVariantId.Value, Is.EqualTo("SPINE_CRATER_BOWL_ASCENT_BASE"));
            Assert.That(candidate.ClearanceWidth, Is.EqualTo(2));
            Assert.That(candidate.ClearanceHeight, Is.EqualTo(2));
            Assert.That(candidate.ShellDigest, Is.EqualTo(ShellDigest));
            Assert.That(candidate.RemovalSafetyDigest, Is.EqualTo(SafetyDigest));
            Assert.That(result.Index.RngStreamCreationCount, Is.Zero);
            Assert.That(result.Index.RngDrawCount, Is.Zero);
        }

        [Test]
        public void CompatibilityMismatches_PublishStableRejectionCodes()
        {
            var profiles = new[]
            {
                Profile("ACTIVITY_VALID"),
                Profile("ACTIVITY_BIOME", biomes: new[] { MoonpalaceBiomeId.CassiaRoot }),
                Profile("ACTIVITY_PACING", pacing: new[] { PacingRole.Risk }),
                Profile("ACTIVITY_ACCESS", access: new[] { AccessClass.OptionalTool }),
                Profile("ACTIVITY_CHUNKS", minimumChunks: 6, maximumChunks: 8),
                Profile("ACTIVITY_CLUSTER", clusterId: "TC_OTHER"),
                Profile("ACTIVITY_VARIANT", variantId: "SPINE_OTHER"),
                Profile("ACTIVITY_SHELL", shellDigest: Digest('c')),
                Profile("ACTIVITY_SAFETY", safetyDigest: Digest('d')),
            };
            var result = Compile(profiles, new[] { Opportunity(0) });

            Assert.That(result.Success, Is.True, Errors(result.Errors));
            var actual = new HashSet<ActivityCompatibilityRejectionCode>(result.Index.Rejections.Select(value => value.Code));
            foreach (var expected in new[]
                     {
                         ActivityCompatibilityRejectionCode.BiomeMismatch,
                         ActivityCompatibilityRejectionCode.PacingRoleMismatch,
                         ActivityCompatibilityRejectionCode.AccessClassMismatch,
                         ActivityCompatibilityRejectionCode.ActiveChunkCountMismatch,
                         ActivityCompatibilityRejectionCode.TerrainClusterMismatch,
                         ActivityCompatibilityRejectionCode.SpineVariantMismatch,
                         ActivityCompatibilityRejectionCode.ActivityShellDigestMismatch,
                         ActivityCompatibilityRejectionCode.RemovalSafetyDigestMismatch,
                     })
                Assert.That(actual.Contains(expected), Is.True, expected.ToString());
        }

        [Test]
        public void Clearance_RequiresExactUniqueAirRectangleWithoutReservationsOrProtection()
        {
            var invalid = new ActivityPlacementClearanceEvidence(
                new LocalTileCoord(0, 0), 2, 2,
                new[] { Tile(0, 0), Tile(1, 0), Tile(0, 1), Tile(1, 1), Tile(0, 1) },
                new[] { Tile(0, 0), Tile(1, 0), Tile(0, 1) },
                new[] { Tile(1, 0) }, new[] { Tile(0, 1) });
            var result = Compile(new[] { Profile("ACTIVITY_ORDINARY") },
                new[] { Opportunity(0), Opportunity(1, invalid) });

            Assert.That(result.Success, Is.True, Errors(result.Errors));
            var codes = new HashSet<ActivityCompatibilityRejectionCode>(result.Index.Rejections.Select(value => value.Code));
            Assert.That(codes.SetEquals(new[]
            {
                ActivityCompatibilityRejectionCode.ClearanceNotRectangular,
                ActivityCompatibilityRejectionCode.ClearanceNotAir,
                ActivityCompatibilityRejectionCode.ClearanceReserved,
                ActivityCompatibilityRejectionCode.ClearanceAbsoluteProtected,
            }), Is.True, string.Join(",", codes));
        }

        [Test]
        public void DuplicateCandidate_AllSourcesExcludedAndPublicationIsAtomic()
        {
            var duplicate = Opportunity(0);
            var result = Compile(new[] { Profile("ACTIVITY_ORDINARY") }, new[] { duplicate, duplicate });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Index, Is.Null);
            AssertCodes(result.Errors, ActivityCompatibilityErrorCode.DuplicateCandidate,
                ActivityCompatibilityErrorCode.EmptyCandidateIndex);
            Assert.That(result.RngStreamCreationCount, Is.Zero);
            Assert.That(result.RngDrawCount, Is.Zero);
        }

        [Test]
        public void CandidateIndex_IsCanonicalAcrossReverseOrderAndCulture()
        {
            var profiles = new[] { Profile("ACTIVITY_ORDINARY"), Profile("ACTIVITY_STRONG", ActivityStrengthClass.Strong) };
            var opportunities = Enumerable.Range(0, 12).Select(Opportunity).ToArray();
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var canonical = Compile(profiles, opportunities);
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                var reverse = Compile(profiles.Reverse(), opportunities.Reverse());
                Assert.That(canonical.Success, Is.True, Errors(canonical.Errors));
                Assert.That(reverse.Success, Is.True, Errors(reverse.Errors));
                Assert.That(reverse.Index.CanonicalDigest, Is.EqualTo(canonical.Index.CanonicalDigest));
                CollectionAssert.AreEqual(canonical.Index.Candidates.Select(value => value.CandidateKey),
                    reverse.Index.Candidates.Select(value => value.CandidateKey));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void FrequencyPolicy_AcceptsInclusiveBoundsAndRejectsOutsideBeforeRng()
        {
            var index = MainIndex(false);
            foreach (var permitted in new[] { 60, 120 })
            {
                var result = Plan(index, new ActivityFrequencyPolicy(permitted, 100, 100, 1));
                Assert.That(result.Success, Is.True, permitted + ":" + Errors(result.Errors));
            }
            foreach (var rejected in new[] { 59, 121 })
            {
                var result = Plan(index, new ActivityFrequencyPolicy(rejected, 100, 100, 1));
                Assert.That(result.Success, Is.False);
                Assert.That(result.Plan, Is.Null);
                AssertCodes(result.Errors, ActivityCompatibilityErrorCode.InvalidFrequencyPolicy);
                Assert.That(result.RngStreamCreationCount, Is.Zero);
                Assert.That(result.RngDrawCount, Is.Zero);
            }
        }

        [Test]
        public void HierarchicalBudgets_UseLargestRemainderAndPreserveExactSums()
        {
            var result = Plan(MainIndex(false), new ActivityFrequencyPolicy(80, 100, 100, 1));

            Assert.That(result.Success, Is.True, Errors(result.Errors));
            Assert.That(result.Plan.WorldBudget.EligibleCount, Is.EqualTo(100));
            Assert.That(result.Plan.WorldBudget.TargetCount, Is.EqualTo(8));
            Assert.That(result.Plan.PatchBudgets.Select(value => value.TargetCount), Is.EqualTo(new[] { 4, 4 }));
            Assert.That(result.Plan.PatchBudgets.Sum(value => value.TargetCount), Is.EqualTo(8));
            Assert.That(result.Plan.SectorBudgets.Sum(value => value.TargetCount), Is.EqualTo(8));
            Assert.That(result.Plan.Decisions.Count, Is.EqualTo(8));
        }

        [Test]
        public void FrequencyRates_ReportFeasibleWorldAndDiscreteSmallScopes()
        {
            var result = Plan(MainIndex(false), new ActivityFrequencyPolicy(80, 0, 0, 0));

            Assert.That(result.Success, Is.True, Errors(result.Errors));
            var world = result.Plan.WorldBudget;
            Assert.That(world.SelectedCount, Is.EqualTo(8));
            Assert.That(world.AchievedPermilleNumerator / world.AchievedPermilleDenominator, Is.EqualTo(80));
            Assert.That(world.BandFeasible, Is.True);
            Assert.That(world.DiscreteApproximation, Is.False);
            Assert.That(result.Plan.PatchBudgets.All(value => value.SelectedCount == 4 && value.EligibleCount == 50), Is.True);
            Assert.That(result.Plan.SectorBudgets.All(value => !value.BandFeasible && value.DiscreteApproximation), Is.True);
            TestContext.WriteLine("100 opportunities: world=8/100 (80 permille), patches=4/50+4/50, sectors=8 selected, strong=0; caps=0/0/0.");
        }

        [Test]
        public void WeightedDecisions_AreDeterministicAndSeedAttemptSensitive()
        {
            var index = MainIndex(true);
            var policy = new ActivityFrequencyPolicy(80, 2, 1, 1);
            var first = Plan(index, policy, 0x1020304050607080UL, 3);
            var repeat = Plan(index, policy, 0x1020304050607080UL, 3);
            var seedChanged = Plan(index, policy, 0x1020304050607081UL, 3);
            var attemptChanged = Plan(index, policy, 0x1020304050607080UL, 4);

            Assert.That(first.Success && repeat.Success && seedChanged.Success && attemptChanged.Success, Is.True);
            Assert.That(repeat.Plan.CanonicalDigest, Is.EqualTo(first.Plan.CanonicalDigest));
            CollectionAssert.AreEqual(first.Plan.Decisions.Select(DecisionEvidence), repeat.Plan.Decisions.Select(DecisionEvidence));
            Assert.That(seedChanged.Plan.CanonicalDigest, Is.Not.EqualTo(first.Plan.CanonicalDigest));
            Assert.That(attemptChanged.Plan.CanonicalDigest, Is.Not.EqualTo(first.Plan.CanonicalDigest));
            Assert.That(first.Plan.Decisions.All(value => value.WeightedTicket >= 0 && value.WeightedTicket < value.TotalWeight), Is.True);
            Assert.That(first.Plan.RngDrawCount, Is.EqualTo(108));
            Assert.That(first.Plan.RngStreamCreationCount, Is.EqualTo(100));
        }

        [Test]
        public void StrongCaps_ExcludeStrongAndChooseOrdinaryWithoutExceedingAnyScope()
        {
            var result = Plan(MainIndex(true), new ActivityFrequencyPolicy(80, 0, 0, 0));

            Assert.That(result.Success, Is.True, Errors(result.Errors));
            Assert.That(result.Plan.Decisions.Count, Is.EqualTo(8));
            Assert.That(result.Plan.Decisions.All(value => value.Strength == ActivityStrengthClass.Ordinary), Is.True);
            Assert.That(result.Plan.WorldBudget.StrongCount, Is.Zero);
            Assert.That(result.Plan.PatchBudgets.All(value => value.StrongCount == 0), Is.True);
            Assert.That(result.Plan.SectorBudgets.All(value => value.StrongCount == 0), Is.True);
            Assert.That(result.Plan.Decisions.All(value => value.WorldStrongBefore == 0 && value.WorldStrongAfter == 0 &&
                                                           value.PatchStrongBefore == 0 && value.PatchStrongAfter == 0 &&
                                                           value.SectorStrongBefore == 0 && value.SectorStrongAfter == 0), Is.True);
        }

        [Test]
        public void StrongOnlyTarget_WithZeroCapsFailsAtomically()
        {
            var result = Plan(MainIndex(true, strongOnly: true), new ActivityFrequencyPolicy(80, 0, 0, 0));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            AssertCodes(result.Errors, ActivityCompatibilityErrorCode.StrongCapUnsatisfiable);
            Assert.That(result.RngStreamCreationCount, Is.GreaterThan(0));
            Assert.That(result.RngDrawCount, Is.GreaterThan(0));
        }

        [Test]
        public void InvalidOrEmptyInput_CreatesNoStreamAndDoesNotPerturbOtherStream()
        {
            var factory = RngFactory();
            var before = factory.Create(WorldGenerationRngStreams.SectorRecipeStreamId, 77,
                RngStreamScope.Sector(new SectorCoord(4, 4), 2)).NextUInt64();
            var invalid = ActivityFrequencyPlanner.Plan(new ActivityFrequencyPlanRequest(
                null, new ActivityFrequencyPolicy(80, 1, 1, 1), 77, 2, factory));
            var after = factory.Create(WorldGenerationRngStreams.SectorRecipeStreamId, 77,
                RngStreamScope.Sector(new SectorCoord(4, 4), 2)).NextUInt64();

            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Plan, Is.Null);
            Assert.That(invalid.RngStreamCreationCount, Is.Zero);
            Assert.That(invalid.RngDrawCount, Is.Zero);
            Assert.That(after, Is.EqualTo(before));
            AssertCodes(invalid.Errors, ActivityCompatibilityErrorCode.EmptyCandidateIndex);
        }

        [Test]
        public void Planning_PublishesNoCanvasGeometryPrefabSceneOrTilemapMutation()
        {
            var profiles = new List<ActivityPlacementProfile> { Profile("ACTIVITY_ORDINARY") };
            var opportunities = Enumerable.Range(0, 100).Select(Opportunity).ToList();
            var request = Request(profiles, opportunities);
            profiles.Clear();
            opportunities.Clear();
            var compiled = ActivityCandidateIndexCompiler.Compile(request);
            var result = Plan(compiled.Index, new ActivityFrequencyPolicy(80, 0, 0, 0));

            Assert.That(compiled.Success && result.Success, Is.True, Errors(compiled.Errors.Concat(result.Errors)));
            Assert.That(request.Profiles.Count, Is.EqualTo(1));
            Assert.That(request.Opportunities.Count, Is.EqualTo(100));
            Assert.That(result.Plan.GeometryWriteCount, Is.Zero);
            Assert.That(result.Plan.CanvasMutationCount, Is.Zero);
            Assert.That(result.Plan.PrefabMutationCount, Is.Zero);
            Assert.That(result.Plan.SceneMutationCount, Is.Zero);
            Assert.That(result.Plan.TilemapMutationCount, Is.Zero);
        }

        private static ActivityCandidateIndex MainIndex(bool includeStrong, bool strongOnly = false)
        {
            var profiles = new List<ActivityPlacementProfile>();
            if (!strongOnly) profiles.Add(Profile("ACTIVITY_ORDINARY", weight: 1));
            if (includeStrong) profiles.Add(Profile("ACTIVITY_STRONG", ActivityStrengthClass.Strong, weight: 10000));
            var result = Compile(profiles, Enumerable.Range(0, 100).Select(Opportunity));
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            return result.Index;
        }

        private static ActivityFrequencyPlanResult Plan(
            ActivityCandidateIndex index,
            ActivityFrequencyPolicy policy,
            ulong seed = 0x1020304050607080UL,
            int attempt = 3)
        {
            return ActivityFrequencyPlanner.Plan(new ActivityFrequencyPlanRequest(index, policy, seed, attempt, RngFactory()));
        }

        private static ActivityCandidateIndexCompileResult Compile(
            IEnumerable<ActivityPlacementProfile> profiles,
            IEnumerable<ActivityPlacementOpportunity> opportunities)
        {
            return ActivityCandidateIndexCompiler.Compile(Request(profiles, opportunities));
        }

        private static ActivityCandidateIndexCompileRequest Request(
            IEnumerable<ActivityPlacementProfile> profiles,
            IEnumerable<ActivityPlacementOpportunity> opportunities)
        {
            return new ActivityCandidateIndexCompileRequest(profiles, opportunities, Ownership(),
                CatalogDigest, SignatureDigest, ManifestDigest);
        }

        private static ActivityPlacementProfile Profile(
            string activityId,
            ActivityStrengthClass strength = ActivityStrengthClass.Ordinary,
            IEnumerable<MoonpalaceBiomeId> biomes = null,
            IEnumerable<PacingRole> pacing = null,
            IEnumerable<AccessClass> access = null,
            int minimumChunks = 4,
            int maximumChunks = 6,
            string clusterId = "TC_CRATER_BOWL_ASCENT",
            string variantId = "SPINE_CRATER_BOWL_ASCENT_BASE",
            string shellDigest = ShellDigest,
            string safetyDigest = SafetyDigest,
            int weight = 100)
        {
            return new ActivityPlacementProfile(
                new ActivityStructureId(activityId), new TerrainClusterId(clusterId), new SpineVariantId(variantId),
                activityId == "ACTIVITY_STRONG" ? Digest('b') : Digest('a'), shellDigest, safetyDigest,
                biomes ?? new[] { MoonpalaceBiomeId.MoonCrater }, pacing ?? new[] { PacingRole.Activity },
                access ?? new[] { AccessClass.MandatoryNoTool }, minimumChunks, maximumChunks, 2, 2, weight, strength);
        }

        private static ActivityPlacementOpportunity Opportunity(int ordinal)
        {
            return Opportunity(ordinal, ValidClearance());
        }

        private static ActivityPlacementOpportunity Opportunity(
            int ordinal,
            ActivityPlacementClearanceEvidence clearance)
        {
            var sectorIndex = ordinal < 50 ? ordinal : 85 + ordinal - 50;
            var patchId = sectorIndex < 85 ? new BiomePatchId("PATCH_A") : new BiomePatchId("PATCH_B");
            return new ActivityPlacementOpportunity(
                "OPPORTUNITY_" + ordinal.ToString("D3", CultureInfo.InvariantCulture),
                WorldGridIndex.ToCoordinate(sectorIndex), patchId, MoonpalaceBiomeId.MoonCrater,
                new TerrainClusterId("TC_CRATER_BOWL_ASCENT"), new SpineVariantId("SPINE_CRATER_BOWL_ASCENT_BASE"),
                PacingRole.Activity, AccessClass.MandatoryNoTool, 5, clearance,
                CatalogDigest, SignatureDigest, ManifestDigest, ShellDigest, SafetyDigest);
        }

        private static ActivityPlacementClearanceEvidence ValidClearance()
        {
            var rectangle = new[] { Tile(0, 0), Tile(1, 0), Tile(0, 1), Tile(1, 1) };
            return new ActivityPlacementClearanceEvidence(new LocalTileCoord(0, 0), 2, 2,
                rectangle, rectangle, Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>());
        }

        private static LocalTileCoord Tile(int x, int y) => new LocalTileCoord(x, y);

        private static BiomePatchSnapshot Ownership()
        {
            var aIndices = Enumerable.Range(0, 85).ToArray();
            var bIndices = Enumerable.Range(85, WorldGenConstants.SectorCount - 85).ToArray();
            var patchA = new BiomePatch(new BiomePatchId("PATCH_A"), "BIO_MOON_CRATER", "RULE_A",
                BiomePatchRole.Satellite,
                new[] { new BiomePatchSeed(0, WorldGridIndex.ToCoordinate(0), BiomePatchRole.Satellite, null) }, aIndices);
            var patchB = new BiomePatch(new BiomePatchId("PATCH_B"), "BIO_MOON_CRATER", "RULE_B",
                BiomePatchRole.Satellite,
                new[] { new BiomePatchSeed(85, WorldGridIndex.ToCoordinate(85), BiomePatchRole.Satellite, null) }, bIndices);
            var ownership = Enumerable.Range(0, WorldGenConstants.SectorCount)
                .Select(index => new BiomeSectorOwnership(index, WorldGridIndex.ToCoordinate(index),
                    "BIO_MOON_CRATER", string.Empty, index < 85 ? patchA.Id : patchB.Id)).ToArray();
            return new BiomePatchSnapshot(42, new[] { patchA, patchB }, ownership, Array.Empty<BiomePatchSiteBinding>());
        }

        private static DeterministicRngStreamFactory RngFactory(bool active = true, string scope = "SECTOR")
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                { WorldGenerationRngStreams.SectorRecipeStreamId,
                    Definition(WorldGenerationRngStreams.SectorRecipeStreamId, "E9931A70C2D520F4", scope, active) },
            };
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new DeterministicRngStreamFactory(set);
        }

        private static RngStreamDefinition Definition(string id, string salt, string scope, bool active)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", Hex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "MAP12_03 focused fixture");
            SetAutoProperty(definition, "Active", active);
            return definition;
        }

        private static CsvHexValue Hex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2)
                .Select(index => byte.Parse(value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                .ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string property, object value)
        {
            var field = target.GetType().GetField("<" + property + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, property);
            field.SetValue(target, value);
        }

        private static string DecisionEvidence(ActivityPlacementDecision value)
        {
            return value.OpportunityId + "|" + value.ActivityId.Value + "|" + value.CandidateKey + "|" +
                   value.Priority.ToString(CultureInfo.InvariantCulture) + "|" + value.WeightedTicket.ToString(CultureInfo.InvariantCulture);
        }

        private static string Digest(char value) => new string(value, 64);

        private static string Errors(IEnumerable<ActivityCompatibilityError> errors)
        {
            return string.Join(";", (errors ?? Array.Empty<ActivityCompatibilityError>())
                .Select(value => value.Code + ":" + value.Path + ":" + value.Detail));
        }

        private static void AssertCodes(
            IEnumerable<ActivityCompatibilityError> errors,
            params ActivityCompatibilityErrorCode[] expected)
        {
            var actual = new HashSet<ActivityCompatibilityErrorCode>(errors.Select(value => value.Code));
            foreach (var code in expected) Assert.That(actual.Contains(code), Is.True, code.ToString());
        }
    }
}
