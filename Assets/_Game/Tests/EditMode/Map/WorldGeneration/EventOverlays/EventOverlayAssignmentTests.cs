using System;
using System.Collections;
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
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.EventOverlays
{
    [TestFixture]
    [Category("MAP12_04")]
    public sealed class EventOverlayAssignmentTests
    {
        private const string ActivityPlanDigest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        [Test]
        public void TerrainClusterAndSelectedActivityMarkersCompileWithExactOwnership()
        {
            var cluster = Compile(new[] { EmptyProfile(), Profile("EVT_CLUSTER", EventOverlayKind.Npc,
                    EventMarkerOperation.SpawnNpc, null) },
                new[] { Opportunity(0, Marker(EventMarkerTargetSourceKind.TerrainCluster, "TC_EVENT_ASSIGN")) });
            Assert.That(cluster.Success, Is.True, Errors(cluster.Errors));

            var activityId = new ActivityStructureId("ACT_EVENT_HOST");
            var activity = Compile(new[] { EmptyProfile(activityId), Profile("EVT_ACTIVITY", EventOverlayKind.State,
                    EventMarkerOperation.SetState, activityId) },
                new[] { Opportunity(0, Marker(EventMarkerTargetSourceKind.Activity, activityId.Value), activityId) });
            Assert.That(activity.Success, Is.True, Errors(activity.Errors));
            Assert.That(activity.Index.Candidates.Where(value => !value.IsEmpty).Single().Profile.ReferencedActivityId,
                Is.EqualTo(activityId));
            Assert.That(activity.Index.RngDrawCount, Is.Zero);
        }

        [TestCase(EventOverlayKind.Npc, EventMarkerOperation.SpawnNpc, SpecialRegionSlotKind.Npc)]
        [TestCase(EventOverlayKind.Reward, EventMarkerOperation.SpawnReward, SpecialRegionSlotKind.Reward)]
        [TestCase(EventOverlayKind.State, EventMarkerOperation.SetState, SpecialRegionSlotKind.Event)]
        [TestCase(EventOverlayKind.Cosmetic, EventMarkerOperation.EnableMarker, SpecialRegionSlotKind.Event)]
        public void SpecialRegionReplaceableSlotsAcceptOnlyExactKindMatrix(
            EventOverlayKind kind, EventMarkerOperation operation, SpecialRegionSlotKind slotKind)
        {
            var special = Special(slotKind);
            var result = Compile(new[] { EmptyProfile(), Profile("EVT_SPECIAL", kind, operation, null) },
                new[] { SpecialOpportunity(special) });
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            Assert.That(result.Index.Candidates.Count(value => !value.IsEmpty), Is.EqualTo(1));
        }

        [TestCase(SpecialRegionSlotKind.Facility)]
        [TestCase(SpecialRegionSlotKind.Enemy)]
        [TestCase(SpecialRegionSlotKind.Entry)]
        [TestCase(SpecialRegionSlotKind.Return)]
        public void FixedFacilityEnemyEntryAndReturnTargetsAreRejected(SpecialRegionSlotKind slotKind)
        {
            var special = Special(slotKind);
            var result = Compile(new[] { EmptyProfile(), Profile("EVT_SPECIAL_BAD", EventOverlayKind.Npc,
                    EventMarkerOperation.SpawnNpc, null) }, new[] { SpecialOpportunity(special) });
            Assert.That(result.Success, Is.False);
            AssertCode(result.Errors, EventOverlayAssignmentErrorCode.InvalidSpecialOverlap);

            var overlap = Special(SpecialRegionSlotKind.Npc, true);
            var fixedResult = Compile(new[] { EmptyProfile(), Profile("EVT_FIXED_BAD", EventOverlayKind.Npc,
                    EventMarkerOperation.SpawnNpc, null) }, new[] { SpecialOpportunity(overlap) });
            AssertCode(fixedResult.Errors, EventOverlayAssignmentErrorCode.FixedShellOverlap);
        }

        [Test]
        public void PersistenceAndEveryNonMarkerSurfaceRemainUnchanged()
        {
            var special = Special(SpecialRegionSlotKind.Reward);
            var baseline = SpecialOpportunity(special);
            Assert.That(Compile(new[] { EmptyProfile(), Profile("EVT_REWARD", EventOverlayKind.Reward,
                EventMarkerOperation.SpawnReward, null) }, new[] { baseline }).Success, Is.True);

            var source = baseline.Markers.Single();
            var mutated = Marker(source.SourceKind, source.SourceOwnerId, source.CompiledCoordinate,
                source.PersistenceKey, geometryMutations: 1);
            var mutationResult = Compile(new[] { EmptyProfile(), Profile("EVT_REWARD", EventOverlayKind.Reward,
                    EventMarkerOperation.SpawnReward, null) }, new[] { Opportunity(0, mutated) });
            AssertCode(mutationResult.Errors, EventOverlayAssignmentErrorCode.NonMarkerMutation);

            var wrongKey = Marker(EventMarkerTargetSourceKind.SpecialRegion, special.Id.Value,
                source.CompiledCoordinate, default(SpecialPersistenceKey));
            var wrongOpportunity = SpecialOpportunity(special, wrongKey);
            var provenance = Compile(new[] { EmptyProfile(), Profile("EVT_REWARD", EventOverlayKind.Reward,
                    EventMarkerOperation.SpawnReward, null) }, new[] { wrongOpportunity });
            AssertCode(provenance.Errors, EventOverlayAssignmentErrorCode.PersistenceProvenanceMismatch);
        }

        [Test]
        public void ExactlyOneEmptyVariantExistsAndEveryUnselectedOpportunityPublishesIt()
        {
            var missing = Compile(new[] { Profile("EVT_ONLY", EventOverlayKind.Npc,
                EventMarkerOperation.SpawnNpc, null) }, Opportunities(10));
            AssertCode(missing.Errors, EventOverlayAssignmentErrorCode.MissingEmptyVariant);

            var duplicate = Compile(new[] { EmptyProfile(), EmptyProfile(null, "EVT_EMPTY_SECOND"),
                Profile("EVT_ONLY", EventOverlayKind.Npc, EventMarkerOperation.SpawnNpc, null) }, Opportunities(10));
            AssertCode(duplicate.Errors, EventOverlayAssignmentErrorCode.DuplicateEmptyVariant);

            var plan = Plan(Index(100), 80);
            Assert.That(plan.Success, Is.True, Errors(plan.Errors));
            Assert.That(plan.Plan.Decisions, Has.Count.EqualTo(100));
            Assert.That(plan.Plan.Decisions.Count(value => value.DecisionKind == EventOverlayAssignmentDecisionKind.Assigned), Is.EqualTo(8));
            Assert.That(plan.Plan.Decisions.Count(value => value.DecisionKind == EventOverlayAssignmentDecisionKind.Empty), Is.EqualTo(92));
            Assert.That(plan.Plan.Decisions.Where(value => value.DecisionKind == EventOverlayAssignmentDecisionKind.Empty)
                .All(value => value.EventKind == EventOverlayKind.Empty && value.TotalWeight == 0 &&
                              value.WeightedDrawCountBefore == value.WeightedDrawCountAfter), Is.True);
        }

        [Test]
        public void FrequencyPolicyHonorsInclusiveThreeAndEightPercentBounds()
        {
            Assert.That(Plan(Index(100), 30).Plan.WorldBudget.AssignedCount, Is.EqualTo(3));
            Assert.That(Plan(Index(100), 80).Plan.WorldBudget.AssignedCount, Is.EqualTo(8));
            AssertCode(Plan(Index(100), 29).Errors, EventOverlayAssignmentErrorCode.InvalidFrequencyPolicy);
            AssertCode(Plan(Index(100), 81).Errors, EventOverlayAssignmentErrorCode.InvalidFrequencyPolicy);

            var lowSample = Plan(Index(1), 80);
            Assert.That(lowSample.Success, Is.True, Errors(lowSample.Errors));
            Assert.That(lowSample.Plan.WorldBudget.DiscreteApproximation, Is.True);
            Assert.That(lowSample.Plan.WorldBudget.AssignedCount, Is.Zero);
        }

        [Test]
        public void LargestRemainderBudgetsCloseAtWorldPatchAndSectorLevels()
        {
            var result = Plan(Index(100), 80);
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            var plan = result.Plan;
            Assert.That(plan.WorldBudget.EligibleCount, Is.EqualTo(100));
            Assert.That(plan.WorldBudget.AssignedCount, Is.EqualTo(8));
            Assert.That(plan.PatchBudgets.Sum(value => value.AssignedCount), Is.EqualTo(8));
            Assert.That(plan.SectorBudgets.Sum(value => value.AssignedCount), Is.EqualTo(8));
            Assert.That(plan.Budgets.All(value => value.AssignedCount + value.EmptyCount == value.EligibleCount), Is.True);
            Assert.That(plan.PatchBudgets.Select(value => value.ScopeId), Is.Ordered);
        }

        [Test]
        public void CooldownPublishesExclusionsAndFailsAtomicallyWhenQuotaCannotBeFilled()
        {
            var pass = Plan(Index(100, new[]
            {
                EmptyProfile(),
                Profile("EVT_COOLDOWN_A", EventOverlayKind.Npc, EventMarkerOperation.SpawnNpc, null, 10000, 200),
                Profile("EVT_COOLDOWN_B", EventOverlayKind.State, EventMarkerOperation.SetState, null, 1, 0),
            }), 80, 913UL);
            Assert.That(pass.Success, Is.True, Errors(pass.Errors));
            Assert.That(pass.Plan.Decisions.Where(value => value.DecisionKind == EventOverlayAssignmentDecisionKind.Assigned)
                .All(value => value.PreviousProgressionOrdinal < 0 ||
                              value.ActualProgressionGap >= value.RequiredProgressionGap), Is.True);
            Assert.That(pass.Plan.Decisions.SelectMany(value => value.CooldownExclusionEvidence).Any(), Is.True);

            var unsatisfied = Plan(Index(100, new[]
            {
                EmptyProfile(),
                Profile("EVT_COOLDOWN_ONLY", EventOverlayKind.Npc, EventMarkerOperation.SpawnNpc, null, 1, 200),
            }), 80);
            Assert.That(unsatisfied.Success, Is.False);
            Assert.That(unsatisfied.Plan, Is.Null);
            AssertCode(unsatisfied.Errors, EventOverlayAssignmentErrorCode.CooldownMakesTargetUnsatisfiable);
        }

        [Test]
        public void PopulationRngIsRepeatableOrderAndCultureIndependentAndSeedSensitive()
        {
            var profiles = StandardProfiles();
            var opportunities = Opportunities(100);
            var firstIndex = Compile(profiles, opportunities).Index;
            var reversedIndex = Compile(profiles.Reverse(), opportunities.Reverse()).Index;
            Assert.That(reversedIndex.CanonicalDigest, Is.EqualTo(firstIndex.CanonicalDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            EventOverlayAssignmentPlanResult first;
            EventOverlayAssignmentPlanResult repeated;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                first = Plan(firstIndex, 80, 7331UL, 2);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ko-KR");
                repeated = Plan(reversedIndex, 80, 7331UL, 2);
            }
            finally { CultureInfo.CurrentCulture = originalCulture; }
            Assert.That(first.Plan.CanonicalDigest, Is.EqualTo(repeated.Plan.CanonicalDigest));
            Assert.That(Plan(firstIndex, 80, 7332UL, 2).Plan.CanonicalDigest, Is.Not.EqualTo(first.Plan.CanonicalDigest));
            Assert.That(Plan(firstIndex, 80, 7331UL, 3).Plan.CanonicalDigest, Is.Not.EqualTo(first.Plan.CanonicalDigest));
        }

        [Test]
        public void EventAssignmentUsesPopulationSpawnScopesAndPreservesActivityPlanAndSectorRecipeRng()
        {
            var result = Plan(Index(100), 80);
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            Assert.That(result.Plan.RngStreamId, Is.EqualTo(WorldGenerationRngStreams.PopulationStreamId));
            Assert.That(result.Plan.RngResetScope, Is.EqualTo(RngResetScope.Spawn));
            Assert.That(result.Plan.ActivityFrequencyPlanDigest, Is.EqualTo(ActivityPlanDigest));
            Assert.That(result.Plan.Decisions.All(value => value.RngScopeIdentity ==
                "EVENT|" + value.Candidate.Opportunity.Sector.X.ToString(CultureInfo.InvariantCulture) + "," +
                value.Candidate.Opportunity.Sector.Y.ToString(CultureInfo.InvariantCulture) + "|" + value.OpportunityId), Is.True);
            Assert.That(result.Plan.RngStreamCreationCount, Is.EqualTo(100));
            Assert.That(result.Plan.RngDrawCount, Is.EqualTo(108));
        }

        [Test]
        public void InvalidInputCreatesNoPopulationStreamsOrDraws()
        {
            var nullRequest = EventOverlayAssignmentPlanner.Plan(null);
            Assert.That(nullRequest.RngStreamCreationCount, Is.Zero);
            Assert.That(nullRequest.RngDrawCount, Is.Zero);
            var invalidPolicy = Plan(Index(100), 81);
            Assert.That(invalidPolicy.RngStreamCreationCount, Is.Zero);
            Assert.That(invalidPolicy.RngDrawCount, Is.Zero);
            var invalidRng = EventOverlayAssignmentPlanner.Plan(new EventOverlayAssignmentPlanRequest(
                Index(100), new EventOverlayAssignmentPolicy(80), 1UL, -1, RngFactory()));
            Assert.That(invalidRng.RngStreamCreationCount, Is.Zero);
            Assert.That(invalidRng.RngDrawCount, Is.Zero);
        }

        [Test]
        public void AssignmentPlanIsImmutableAndOwnsNoWorldMutationSurface()
        {
            var result = Plan(Index(100), 80);
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            var plan = result.Plan;
            Assert.That(plan.GeometryWriteCount + plan.CollisionWriteCount + plan.RouteWriteCount +
                        plan.AccessWriteCount + plan.PacingWriteCount + plan.EnvelopeWriteCount +
                        plan.CanvasMutationCount + plan.PrefabMutationCount + plan.SceneMutationCount +
                        plan.TilemapMutationCount, Is.Zero);
            Assert.Throws<NotSupportedException>(() => ((IList)plan.Decisions).Clear());
            Assert.That(typeof(EventOverlayAssignmentPlan).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(value => value.SetMethod != null), Is.Empty);
            Assert.That(plan.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static EventOverlayAssignmentProfile[] StandardProfiles()
            => new[]
            {
                EmptyProfile(),
                Profile("EVT_NPC", EventOverlayKind.Npc, EventMarkerOperation.SpawnNpc, null, 3),
                Profile("EVT_STATE", EventOverlayKind.State, EventMarkerOperation.SetState, null, 2),
            };

        private static EventOverlayAssignmentProfile EmptyProfile(
            ActivityStructureId? activityId = null, string eventId = "EVT_EMPTY")
        {
            var contract = new EventOverlayContract(new EventOverlayId(eventId), EventOverlayKind.Empty,
                new TerrainClusterId("TC_EVENT_ASSIGN"), activityId, Array.Empty<EventMarkerAssignment>());
            return new EventOverlayAssignmentProfile(contract, 0, 0, Biomes(), Pacings(), Accesses(), activityId);
        }

        private static EventOverlayAssignmentProfile Profile(
            string eventId,
            EventOverlayKind kind,
            EventMarkerOperation operation,
            ActivityStructureId? activityId,
            int weight = 1,
            int gap = 0,
            string markerId = "MARKER_COMMON")
        {
            var contract = new EventOverlayContract(new EventOverlayId(eventId), kind,
                new TerrainClusterId("TC_EVENT_ASSIGN"), activityId,
                new[] { new EventMarkerAssignment(new EventMarkerId(markerId), operation, "PAYLOAD_EVENT") });
            return new EventOverlayAssignmentProfile(contract, weight, gap, Biomes(), Pacings(), Accesses(), activityId);
        }

        private static EventOverlayCandidateIndex Index(
            int count,
            IEnumerable<EventOverlayAssignmentProfile> profiles = null)
        {
            var result = Compile(profiles ?? StandardProfiles(), Opportunities(count));
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            return result.Index;
        }

        private static EventOverlayCandidateIndexResult Compile(
            IEnumerable<EventOverlayAssignmentProfile> profiles,
            IEnumerable<EventOverlayOpportunity> opportunities)
            => EventOverlayCandidateIndexCompiler.Compile(new EventOverlayCandidateIndexRequest(
                profiles, opportunities, ActivityPlanDigest));

        private static EventOverlayAssignmentPlanResult Plan(
            EventOverlayCandidateIndex index,
            int targetPermille,
            ulong seed = 7331UL,
            int attempt = 0)
            => EventOverlayAssignmentPlanner.Plan(new EventOverlayAssignmentPlanRequest(index,
                new EventOverlayAssignmentPolicy(targetPermille), seed, attempt, RngFactory()));

        private static EventOverlayOpportunity[] Opportunities(int count)
            => Enumerable.Range(0, count).Select(index => Opportunity(index,
                Marker(EventMarkerTargetSourceKind.TerrainCluster, "TC_EVENT_ASSIGN"))).ToArray();

        private static EventOverlayOpportunity Opportunity(
            int ordinal,
            EventMarkerTargetEvidence marker,
            ActivityStructureId? activityId = null)
        {
            var sector = new SectorCoord(ordinal % 10, ordinal / 10);
            return new EventOverlayOpportunity("EVENT_OPP_" + ordinal.ToString("D3", CultureInfo.InvariantCulture),
                sector, new BiomePatchId(ordinal < 50 ? "PATCH_A" : "PATCH_B"), ordinal,
                MoonpalaceBiomeId.MoonCrater, PacingRole.Activity, AccessClass.MandatoryNoTool,
                new TerrainClusterId("TC_EVENT_ASSIGN"), activityId, ActivityPlanDigest, new[] { marker });
        }

        private static EventMarkerTargetEvidence Marker(
            EventMarkerTargetSourceKind sourceKind,
            string ownerId,
            LocalTileCoord? coordinate = null,
            SpecialPersistenceKey persistenceKey = default(SpecialPersistenceKey),
            int geometryMutations = 0)
        {
            var tile = coordinate ?? new LocalTileCoord(4, 4);
            return new EventMarkerTargetEvidence(new EventMarkerId("MARKER_COMMON"), sourceKind,
                ownerId, tile, tile, sourceKind == EventMarkerTargetSourceKind.SpecialRegion ? "ReplaceableSlot" : "Npc",
                "AIR", "AIR", Digest('b'), Digest('b'), Digest('c'), Digest('c'), persistenceKey,
                persistenceKey.Value.Length == 0 ? string.Empty : Digest('d'),
                persistenceKey.Value.Length == 0 ? string.Empty : Digest('d'), geometryMutations);
        }

        private static SpecialRegionContract Special(SpecialRegionSlotKind kind, bool shellOverlap = false)
        {
            var regionId = new SpecialRegionId("SR_EVENT_HOST");
            var slotId = new SpecialRegionSlotId("SR_SLOT_EVENT_TARGET");
            var scope = kind == SpecialRegionSlotKind.Reward ? SpecialPersistenceScope.Reward :
                kind == SpecialRegionSlotKind.Event ? SpecialPersistenceScope.Encounter : SpecialPersistenceScope.Slot;
            var key = kind == SpecialRegionSlotKind.Reward
                ? SpecialPersistenceKey.ForSlot(regionId, scope, slotId)
                : default(SpecialPersistenceKey);
            var tile = new LocalTileCoord(4, 4);
            return new SpecialRegionContract(regionId, SpecialRegionKind.Village,
                new SiteReservationId("RES_EVENT_HOST"),
                new SpecialRegionFootprint(new[] { new SpecialRegionSectorOffset(0, 0) }),
                shellOverlap
                    ? new[] { new SpecialRegionFixedShellCell(new SpecialRegionSectorOffset(0, 0), tile, "SHELL_EVENT") }
                    : Array.Empty<SpecialRegionFixedShellCell>(),
                new[] { new SpecialRegionSlot(slotId, kind, new SpecialRegionSectorOffset(0, 0), tile,
                    false, scope, key) }, Array.Empty<SpecialRegionPort>(),
                key.Value.Length == 0 ? Array.Empty<SpecialPersistenceBinding>() : new[]
                {
                    new SpecialPersistenceBinding(key, scope, slotId, "INITIAL_AVAILABLE")
                });
        }

        private static EventOverlayOpportunity SpecialOpportunity(
            SpecialRegionContract special,
            EventMarkerTargetEvidence marker = null)
        {
            var slot = special.Slots.Single();
            return new EventOverlayOpportunity("EVENT_OPP_000", new SectorCoord(0, 0),
                new BiomePatchId("PATCH_A"), 0, MoonpalaceBiomeId.MoonCrater,
                PacingRole.Activity, AccessClass.MandatoryNoTool, new TerrainClusterId("TC_EVENT_ASSIGN"),
                null, ActivityPlanDigest, new[]
                {
                    marker ?? Marker(EventMarkerTargetSourceKind.SpecialRegion, special.Id.Value, slot.Tile, slot.PersistenceKey)
                }, EventSpecialOverlapKind.ReplaceableSlot, special,
                SpecialRegionCanonicalDigest.Compute(special), slot.Id);
        }

        private static MoonpalaceBiomeId[] Biomes() => new[] { MoonpalaceBiomeId.MoonCrater };
        private static PacingRole[] Pacings() => new[] { PacingRole.Activity };
        private static AccessClass[] Accesses() => new[] { AccessClass.MandatoryNoTool };

        private static DeterministicRngStreamFactory RngFactory(bool active = true, string scope = "SPAWN")
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                { WorldGenerationRngStreams.PopulationStreamId,
                    Definition(WorldGenerationRngStreams.PopulationStreamId, "A63D4078F9E21C55", scope, active) },
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
            SetAutoProperty(definition, "DescriptionKo", "MAP12_04 focused fixture");
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

        private static string Digest(char value) => new string(value, 64);
        private static string Errors(IEnumerable<EventOverlayAssignmentError> errors)
            => string.Join(";", (errors ?? Array.Empty<EventOverlayAssignmentError>()).Select(value => value.ToString()));

        private static void AssertCode(IEnumerable<EventOverlayAssignmentError> errors, EventOverlayAssignmentErrorCode code)
            => Assert.That(errors.Select(value => value.Code), Does.Contain(code), Errors(errors));
    }
}
