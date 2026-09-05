using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Population;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Validation
{
    [TestFixture]
    [Category("MAP19_05")]
    public sealed class GeneratedRepetitionEventRemovalValidatorTests
    {
        private static SourceScenario cachedSource;

        [Test]
        public void RepetitionValidatorDetectsPatternMirrorClusterAndActivityWindows()
        {
            var result = Run(Signatures(patternNear: true, clusterNear: true,
                activityNear: true)).Result;

            Assert.That(result.Success, Is.False);
            Assert.That(result.Failures.Select(value => value.Reason), Does.Contain(
                "PATTERN_MIRROR_WINDOW_VIOLATION"));
            Assert.That(result.Failures.Select(value => value.Reason), Does.Contain(
                "CLUSTER_STRUCTURAL_REPETITION_VIOLATION"));
            Assert.That(result.Failures.Select(value => value.Reason), Does.Contain(
                "ACTIVITY_EVENT_REPETITION_VIOLATION"));
        }

        [Test]
        public void RepetitionValidatorCollapsesMaterialOnlyDuplicatesWithoutChangingStructure()
        {
            var run = Run();
            AssertSuccess(run.Result);

            Assert.That(run.Result.Surface.Repetition.MaterialOnlyDuplicateCollapses,
                Is.EqualTo(1));
            Assert.That(run.Result.Surface.Repetition.PatternSignaturesChecked,
                Is.EqualTo(3));
            Assert.That(run.Result.Surface.Repetition.PatternMirrorPairsChecked,
                Is.EqualTo(1));
            Assert.That(run.Result.Surface.Repetition.PatternMirrorViolations, Is.Zero);
            Assert.That(run.Result.Surface.Repetition.UniqueStructuralSignatures,
                Is.EqualTo(5));
        }

        [Test]
        public void EventRemovalValidatorCompletesStaticShellAfterRemovingActivitiesAndEvents()
        {
            var result = Run().Result;
            AssertSuccess(result);
            var removal = result.Surface.Removal;

            Assert.That(removal.ActivityStructuresRemoved, Is.EqualTo(1));
            Assert.That(removal.EventOverlaysRemoved, Is.EqualTo(1));
            Assert.That(removal.StaticTransitionsRetained, Is.EqualTo(12));
            Assert.That(removal.RemovedTransitions, Is.EqualTo(2));
            Assert.That(removal.CompletionSearches, Is.EqualTo(1));
            Assert.That(removal.GoalsSatisfied, Is.EqualTo(1));
            Assert.That(removal.GoalsMissing, Is.Zero);
            Assert.That(removal.RemovalDependencyViolations, Is.Zero);
            Assert.That(removal.Completion.SuccessProofDigest,
                Is.EqualTo(GeneratedRepetitionEventRemovalValidator.ExpectedCompletionProofDigest));
        }

        [Test]
        public void EventRemovalValidatorKeepsSpecialMandatoryForgeSealBossGoalsExplicit()
        {
            var run = Run();
            AssertSuccess(run.Result);
            var retained = run.RemovalInput.Transitions.Where(value => !value.IsRemoved)
                .Select(value => value.Transition).ToArray();

            Assert.That(retained.Count(value => value.Kind ==
                GeneratedCompletionTransitionKind.CollectMandatoryResource), Is.EqualTo(3));
            Assert.That(retained.Select(value => value.Kind), Does.Contain(
                GeneratedCompletionTransitionKind.ActivateForge));
            Assert.That(retained.Select(value => value.Kind), Does.Contain(
                GeneratedCompletionTransitionKind.AcceptSeal));
            Assert.That(retained.Select(value => value.Kind), Does.Contain(
                GeneratedCompletionTransitionKind.DefeatBoss));
            Assert.That(retained.Select(value => value.Kind), Does.Contain(
                GeneratedCompletionTransitionKind.ExitSpecial));
            Assert.That(run.RemovalInput.Goal.RequiredResourceMask, Is.EqualTo(7));
            Assert.That(run.RemovalInput.Goal.Forge,
                Is.EqualTo(GeneratedForgeValidationState.Activated));
            Assert.That(run.RemovalInput.Goal.Seal,
                Is.EqualTo(GeneratedSealValidationState.Accepted));
            Assert.That(run.RemovalInput.Goal.Boss,
                Is.EqualTo(GeneratedBossValidationState.Defeated));
            Assert.That(run.RemovalInput.Goal.SpecialState,
                Is.EqualTo(GeneratedSpecialValidationState.Exited));
        }

        [Test]
        public void RepetitionEventRemovalConsumesMap19_02ToMap19_04SurfacesReadOnly()
        {
            var source = Source();
            var before = UpstreamDigests(source);
            var run = Run();
            AssertSuccess(run.Result);

            Assert.That(ReferenceEquals(run.RepetitionInput.Graph, source.Graph), Is.True);
            Assert.That(ReferenceEquals(run.RemovalInput.CompletionSearch,
                source.Completion), Is.True);
            Assert.That(ReferenceEquals(run.RepetitionInput.ClusterValidation,
                source.Map19_04), Is.True);
            Assert.That(ReferenceEquals(run.RepetitionInput.Canvas, source.Canvas), Is.True);
            Assert.That(ReferenceEquals(run.RepetitionInput.Slices, source.Slices), Is.True);
            Assert.That(UpstreamDigests(source), Is.EqualTo(before));
        }

        [Test]
        public void RepetitionEventRemovalRejectsMissingHandoffAndDigestMismatches()
        {
            var missing = Run(omitClusterValidation: true).Result;
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Failures.Select(value => value.Reason), Does.Contain(
                "MISSING_MAP19_04_HANDOFF"));

            var mismatch = Run(declaredCombined: new string('d', 64),
                declaredHandoff: new string('e', 64)).Result;
            Assert.That(mismatch.Success, Is.False);
            Assert.That(mismatch.Failures.Select(value => value.Reason), Does.Contain(
                "DECLARED_COMBINED_DIGEST_MISMATCH"));
            Assert.That(mismatch.Failures.Select(value => value.Reason), Does.Contain(
                "INCOMING_HANDOFF_DIGEST_MISMATCH"));
        }

        [Test]
        public void RepetitionEventRemovalFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var baseBindings = RemovalBindings().ToList();
            var activity = Contracts().ActivityA;
            baseBindings[0] = new GeneratedRemovalTransitionBinding(
                baseBindings[0].Transition,
                GeneratedRemovalTransitionSourceKind.ActivityStructure,
                "MAP12_ACTIVITY", Hash("RETAGGED_REQUIRED"), activityStructure: activity);
            var completionFailure = Run(removalBindings: baseBindings).Result;
            AssertAtomic(completionFailure, "REMOVAL_COMPLETION_FAILURE");

            var dependent = RemovalBindings().Select(value => value.IsRemoved &&
                    value.SourceKind == GeneratedRemovalTransitionSourceKind.ActivityStructure
                    ? new GeneratedRemovalTransitionBinding(value.Transition, value.SourceKind,
                        value.SourceOwner, value.SourceDigest, value.ActivityStructure,
                        value.EventOverlay, true)
                    : value).ToArray();
            AssertAtomic(Run(removalBindings: dependent).Result,
                "REMOVED_TRANSITION_DEPENDENCY_VIOLATION");

            AssertAtomic(Run(actionAudit: new GeneratedRepetitionEventRemovalActionAudit(
                runtimeObjectMutationAttempts: 1)).Result,
                "FORBIDDEN_API_OR_MUTATION_ATTEMPT");
            AssertAtomic(Run(actionAudit: new GeneratedRepetitionEventRemovalActionAudit(
                map19_06Started: true)).Result,
                "FORBIDDEN_API_OR_MUTATION_ATTEMPT");
        }

        [Test]
        public void RepetitionEventRemovalDigestIsStableAcrossRepeatReverseCultureAndOrder()
        {
            var first = Run();
            var repeat = Run();
            var reverse = Run(Signatures().Reverse(), RemovalBindings().Reverse(),
                Windows().Reverse());
            AssertSuccess(first.Result);
            AssertSuccess(repeat.Result);
            AssertSuccess(reverse.Result);
            Assert.That(repeat.Result.CombinedSuccessDigest,
                Is.EqualTo(first.Result.CombinedSuccessDigest));
            Assert.That(reverse.Result.CombinedSuccessDigest,
                Is.EqualTo(first.Result.CombinedSuccessDigest));

            var priorCulture = CultureInfo.CurrentCulture;
            var priorUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = Run();
                AssertSuccess(culture.Result);
                Assert.That(culture.Result.CombinedSuccessDigest,
                    Is.EqualTo(first.Result.CombinedSuccessDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = priorCulture;
                CultureInfo.CurrentUICulture = priorUiCulture;
            }

            var mutation = Run(Signatures(materialToken: "OBSIDIAN"));
            AssertSuccess(mutation.Result);
            Assert.That(mutation.Result.Surface.Repetition.InputDigest,
                Is.Not.EqualTo(first.Result.Surface.Repetition.InputDigest));
            Assert.That(mutation.Result.RepetitionSuccessDigest,
                Is.Not.EqualTo(first.Result.RepetitionSuccessDigest));
            Assert.That(mutation.Result.CombinedSuccessDigest,
                Is.Not.EqualTo(first.Result.CombinedSuccessDigest));
            Assert.That(mutation.Result.Map19_06HandoffDigest,
                Is.Not.EqualTo(first.Result.Map19_06HandoffDigest));
        }

        [Test]
        public void RepetitionEventRemovalPublishesMap19_06HandoffSurface()
        {
            var result = Run().Result;
            AssertSuccess(result);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.Surface.Repetition.InputDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.RepetitionSuccessDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.Surface.Removal.InputDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.RemovalSuccessDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.CombinedSuccessDigest), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                result.Map19_06HandoffDigest), Is.True);
        }

        [Test]
        public void Map19HandoffKeepsMap19_06Locked()
        {
            var run = Run();
            AssertSuccess(run.Result);
            Assert.That(run.Result.Surface.Map19_06Started, Is.False);
            Assert.That(run.RepetitionInput.ActionAudit.Map19_06Started, Is.False);
            Assert.That(run.RemovalInput.ActionAudit.Map19_06Started, Is.False);
            Assert.That(run.Result.Map19_06HandoffDigest, Is.Not.Empty);
            WriteEvidence(run.Result);
        }

        private static ValidationRun Run(
            IEnumerable<GeneratedRepetitionSignature> signatures = null,
            IEnumerable<GeneratedRemovalTransitionBinding> removalBindings = null,
            IEnumerable<GeneratedRepetitionWindow> windows = null,
            GeneratedRepetitionEventRemovalActionAudit actionAudit = null,
            string declaredCombined = null,
            string declaredHandoff = null,
            ClusterRecoveryValidationResult clusterValidation = null,
            bool omitClusterValidation = false)
        {
            var source = Source();
            var selectedClusterValidation = omitClusterValidation ? null :
                (clusterValidation ?? source.Map19_04);
            var audit = actionAudit ?? new GeneratedRepetitionEventRemovalActionAudit();
            var repetition = new GeneratedRepetitionValidationInput(source.Graph,
                source.Naked, source.Completion, selectedClusterValidation, source.Canvas,
                source.Slices, signatures ?? Signatures(), windows ?? Windows(), audit,
                declaredCombined, declaredHandoff);
            var bindings = (removalBindings ?? RemovalBindings()).ToArray();
            var removal = new GeneratedEventRemovalValidationInput(source.Graph,
                source.Naked, source.Completion, selectedClusterValidation, bindings,
                Goal(source.Graph), Movements(), audit, declaredCombined, declaredHandoff);
            return new ValidationRun(repetition, removal,
                GeneratedRepetitionEventRemovalValidator.Validate(repetition, removal));
        }

        private static IEnumerable<GeneratedRepetitionSignature> Signatures(
            bool patternNear = false,
            bool clusterNear = false,
            bool activityNear = false,
            string materialToken = "ICE")
        {
            var contracts = Contracts();
            var patternA = Pattern("PATTERN_A");
            var patternMirror = Pattern("PATTERN_MIRROR");
            yield return Signature(GeneratedRepetitionSourceKind.MicroPattern,
                "MAP10_MICRO_PATTERN", "PATTERN_A", "PATTERN_A", "CLUSTER_A", "BAND_A",
                "QUIET", 0, "MIRROR_PAIR_A", false, "SILHOUETTE_A", "SPINE_LOW",
                "WALK_JUMP", "NONE", "PROVENANCE_PATTERN_A", "STONE",
                patternA.StableDigest, microPattern: patternA);
            yield return Signature(GeneratedRepetitionSourceKind.MicroPattern,
                "MAP10_MICRO_PATTERN", "PATTERN_A_MIRROR", "PATTERN_A_MIRROR",
                "CLUSTER_A", patternNear ? "BAND_A" : "BAND_B", "QUIET",
                patternNear ? 1 : 5, "MIRROR_PAIR_A", true, "SILHOUETTE_A_MIRRORED",
                "SPINE_LOW", "WALK_JUMP", "NONE", "PROVENANCE_PATTERN_MIRROR", "STONE",
                patternMirror.StableDigest, microPattern: patternMirror);
            yield return Signature(GeneratedRepetitionSourceKind.MicroPattern,
                "MAP10_MICRO_PATTERN", "PATTERN_A_MATERIAL", "PATTERN_A_MATERIAL",
                "CLUSTER_A", "BAND_C", "QUIET", 10, string.Empty, false,
                "SILHOUETTE_A", "SPINE_LOW", "WALK_JUMP", "NONE",
                "PROVENANCE_PATTERN_A", materialToken, patternA.StableDigest,
                microPattern: patternA);
            yield return Signature(GeneratedRepetitionSourceKind.TerrainCluster,
                "MAP11_TERRAIN_CLUSTER", "CLUSTER_SIG_A", "CLUSTER_A", "CLUSTER_A",
                "CLUSTER_BAND_A", "TRAVERSAL", 20, string.Empty, false,
                "CLUSTER_SILHOUETTE_A", "PRIMARY_SPINE", "WALK_JUMP_DROP", "NONE",
                "PROVENANCE_CLUSTER_ARCHETYPE", "STONE", Hash("CLUSTER_SOURCE_A"),
                terrainCluster: contracts.ClusterA);
            yield return Signature(GeneratedRepetitionSourceKind.TerrainCluster,
                "MAP11_TERRAIN_CLUSTER", "CLUSTER_SIG_B", "CLUSTER_B", "CLUSTER_B",
                "CLUSTER_BAND_B", "TRAVERSAL", clusterNear ? 21 : 30, string.Empty,
                false, "CLUSTER_SILHOUETTE_A", "PRIMARY_SPINE", "WALK_JUMP_DROP",
                "NONE", "PROVENANCE_CLUSTER_ARCHETYPE", "STONE",
                Hash("CLUSTER_SOURCE_B"), terrainCluster: contracts.ClusterB);
            yield return Signature(GeneratedRepetitionSourceKind.ActivityStructure,
                "MAP12_ACTIVITY", "ACTIVITY_SIG_A", "ACTIVITY_A", "CLUSTER_A",
                "ACTIVITY_BAND_A", "RISK", 40, string.Empty, false,
                "ACTIVITY_SHELL_A", "SPINE_BRANCH", "OPTIONAL_NO_TOOL",
                "ARENA_ARCHETYPE", "PROVENANCE_ACTIVITY_ARCHETYPE", "BRASS",
                Hash("ACTIVITY_SOURCE_A"), activity: contracts.ActivityA);
            yield return Signature(GeneratedRepetitionSourceKind.ActivityStructure,
                "MAP12_ACTIVITY", "ACTIVITY_SIG_B", "ACTIVITY_B", "CLUSTER_B",
                "ACTIVITY_BAND_B", "RISK", activityNear ? 41 : 50, string.Empty,
                false, "ACTIVITY_SHELL_A", "SPINE_BRANCH", "OPTIONAL_NO_TOOL",
                "ARENA_ARCHETYPE", "PROVENANCE_ACTIVITY_ARCHETYPE", "BRASS",
                Hash("ACTIVITY_SOURCE_B"), activity: contracts.ActivityB);
            yield return Signature(GeneratedRepetitionSourceKind.EventOverlay,
                "MAP18_EVENT_OVERLAY", "EVENT_SIG_A", "EVENT_A", "CLUSTER_A",
                "EVENT_BAND_A", "NARRATIVE", 60, string.Empty, false,
                "MARKER_ONLY", "STATIC_SHELL_PRESERVED", "NO_MOVEMENT_CHANGE",
                "NPC_OVERLAY", "PROVENANCE_EVENT_A", "NPC_LABEL_A",
                Hash("EVENT_SOURCE_A"), eventOverlay: contracts.EventA);
        }

        private static GeneratedRepetitionSignature Signature(
            GeneratedRepetitionSourceKind kind, string owner, string signatureId,
            string sourceId, string clusterId, string localBand, string pacingBand,
            int ordinal, string mirrorPair, bool mirror, string silhouette,
            string route, string movement, string role, string provenance,
            string material, string digest, MicroPatternSilhouetteSignature microPattern = null,
            TerrainClusterContract terrainCluster = null,
            ActivityStructureContract activity = null,
            EventOverlayContract eventOverlay = null) => new GeneratedRepetitionSignature(kind,
            owner, signatureId, sourceId, clusterId, localBand, pacingBand, ordinal,
            mirrorPair, mirror, silhouette, route, movement, role, provenance, material,
            digest, microPattern, terrainCluster, activity, eventOverlay);

        private static GeneratedRepetitionWindow[] Windows() => new[]
        {
            new GeneratedRepetitionWindow("WINDOW_PATTERN_MIRROR",
                GeneratedRepetitionWindowKind.PatternMirror, 1),
            new GeneratedRepetitionWindow("WINDOW_CLUSTER",
                GeneratedRepetitionWindowKind.TerrainCluster, 2),
            new GeneratedRepetitionWindow("WINDOW_ACTIVITY_EVENT",
                GeneratedRepetitionWindowKind.ActivityEvent, 2),
        };

        private static IEnumerable<GeneratedRemovalTransitionBinding> RemovalBindings()
        {
            var source = Source();
            var contracts = Contracts();
            var resources = GeneratedMandatoryContentCatalog.CreateAuthoritative()
                .Where(value => value.IsCoreResource).OrderBy(value => value).ToArray();
            var transitions = new List<GeneratedCompletionStateTransition>
            {
                GeneratedCompletionStateTransition.FromMandatoryPopulation(
                    Node(source.Graph, 1, 0).NodeId, resources[0], 1),
                GeneratedCompletionStateTransition.FromMandatoryPopulation(
                    Node(source.Graph, 2, 0).NodeId, resources[1], 2),
                GeneratedCompletionStateTransition.FromMandatoryPopulation(
                    Node(source.Graph, 3, 0).NodeId, resources[2], 4),
            };
            var forge = SpecialSource(GeneratedSpecialStateExportKind.Forge,
                "SITE_MOON_SEAL_FORGE", "FORGE_STATE", 'a');
            transitions.AddRange(new[]
            {
                SpecialTransition(source.Graph, 4, forge,
                    GeneratedCompletionTransitionKind.MakeForgeAvailable),
                SpecialTransition(source.Graph, 4, forge,
                    GeneratedCompletionTransitionKind.ActivateForge),
                SpecialTransition(source.Graph, 5, forge,
                    GeneratedCompletionTransitionKind.OpenSeal),
                SpecialTransition(source.Graph, 5, forge,
                    GeneratedCompletionTransitionKind.AcceptSeal),
            });
            var special = SpecialSource(GeneratedSpecialStateExportKind.ActivityEventRuntime,
                "SITE_REQUIRED_SPECIAL", "SPECIAL_STATE", 'b');
            transitions.AddRange(new[]
            {
                SpecialTransition(source.Graph, 6, special,
                    GeneratedCompletionTransitionKind.EnterSpecial),
                SpecialTransition(source.Graph, 6, special,
                    GeneratedCompletionTransitionKind.ResolveSpecial),
                SpecialTransition(source.Graph, 6, special,
                    GeneratedCompletionTransitionKind.ExitSpecial),
            });
            var boss = SpecialSource(GeneratedSpecialStateExportKind.Boss,
                "SITE_MOON_BOSS_VAULT", "BOSS_STATE", 'c');
            transitions.AddRange(new[]
            {
                SpecialTransition(source.Graph, 7, boss,
                    GeneratedCompletionTransitionKind.MakeBossAvailable),
                SpecialTransition(source.Graph, 7, boss,
                    GeneratedCompletionTransitionKind.DefeatBoss),
            });
            for (var index = 0; index < transitions.Count; index++)
            {
                var kind = index < 3 ? GeneratedRemovalTransitionSourceKind.StaticMandatory :
                    GeneratedRemovalTransitionSourceKind.StaticSpecial;
                yield return new GeneratedRemovalTransitionBinding(transitions[index], kind,
                    index < 3 ? "MAP18_MANDATORY_POPULATION" : "MAP18_SPECIAL_STATE_EXPORT",
                    transitions[index].SourceDigest);
            }

            var activityDigest = Hash("ACTIVITY_REMOVAL_TRANSITION");
            yield return new GeneratedRemovalTransitionBinding(
                new GeneratedCompletionStateTransition(Node(source.Graph, 0, 0).NodeId,
                    GeneratedCompletionTransitionKind.CollectMandatoryResource,
                    GeneratedCompletionBindingSourceKind.SpecialRegionState,
                    contracts.ActivityA.Id.Value, activityDigest, 8,
                    "ACTIVITY_OPTIONAL_TRANSITION"),
                GeneratedRemovalTransitionSourceKind.ActivityStructure, "MAP12_ACTIVITY",
                activityDigest, activityStructure: contracts.ActivityA);
            var eventDigest = Hash("EVENT_REMOVAL_TRANSITION");
            yield return new GeneratedRemovalTransitionBinding(
                new GeneratedCompletionStateTransition(Node(source.Graph, 0, 0).NodeId,
                    GeneratedCompletionTransitionKind.CollectMandatoryResource,
                    GeneratedCompletionBindingSourceKind.SpecialRegionState,
                    contracts.EventA.Id.Value, eventDigest, 16,
                    "EVENT_OPTIONAL_TRANSITION"),
                GeneratedRemovalTransitionSourceKind.EventOverlay, "MAP18_EVENT_OVERLAY",
                eventDigest, eventOverlay: contracts.EventA);
        }

        private static GeneratedCompletionGoal Goal(GeneratedTileMovementGraph graph) =>
            new GeneratedCompletionGoal(Node(graph, 8, 0).NodeId, 7,
                GeneratedForgeValidationState.Activated,
                GeneratedSealValidationState.Accepted,
                GeneratedBossValidationState.Defeated,
                GeneratedSpecialValidationState.Exited);

        private static GeneratedCompletionStateTransition SpecialTransition(
            GeneratedTileMovementGraph graph, int x,
            GeneratedDeclaredSpecialStateSource source,
            GeneratedCompletionTransitionKind kind) =>
            GeneratedCompletionStateTransition.FromDeclaredSpecialState(
                Node(graph, x, 0).NodeId, source, kind);

        private static GeneratedDeclaredSpecialStateSource SpecialSource(
            GeneratedSpecialStateExportKind kind, string site, string state,
            char digestCharacter) => new GeneratedDeclaredSpecialStateSource(kind,
            "MAP18_SPECIAL_STATE_EXPORT", site, new SpecialPersistenceKey("SR_STATE_" + site),
            GeneratedSpecialStateSourceStatus.Active, state,
            new string(digestCharacter, 64));

        private static ContractSet Contracts()
        {
            var clusterA = Cluster("CLUSTER_A");
            var clusterB = Cluster("CLUSTER_B");
            var activityA = Activity("ACTIVITY_A", clusterA.Id);
            var activityB = Activity("ACTIVITY_B", clusterB.Id);
            var eventA = new EventOverlayContract(new EventOverlayId("EVENT_A"),
                EventOverlayKind.Npc, clusterA.Id, activityA.Id, null);
            return new ContractSet(clusterA, clusterB, activityA, activityB, eventA);
        }

        private static TerrainClusterContract Cluster(string id) => new TerrainClusterContract(
            new TerrainClusterId(id), new ClusterFootprint(null), null, null,
            new TerrainClusterTraversalContract(null));

        private static ActivityStructureContract Activity(string id, TerrainClusterId cluster) =>
            new ActivityStructureContract(new ActivityStructureId(id), cluster,
                new SpineVariantId("SPINE_A"), null, null, null, null, null, null, null);

        private static MicroPatternSilhouetteSignature Pattern(string key)
        {
            var constructor = typeof(MicroPatternSilhouetteSignature).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(ushort), typeof(ushort), typeof(MicroPatternTransform),
                    typeof(string) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (MicroPatternSilhouetteSignature)constructor.Invoke(new object[]
            {
                (ushort)1, (ushort)2, MicroPatternTransform.R0, Hash(key),
            });
        }

        private static SourceScenario Source()
        {
            if (cachedSource != null) return cachedSource;
            var method = typeof(GeneratedClusterRecoveryDensityValidatorTests).GetMethod(
                "CreateScenario", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var raw = method.Invoke(null, new object[] { false, false });
            cachedSource = new SourceScenario(
                Property<GeneratedTileMovementGraph>(raw, "Graph"),
                Property<SectorCanvasContract>(raw, "Canvas"),
                Property<GeneratedSliceSet>(raw, "Slices"),
                Property<NakedTraversalSearchResult>(raw, "Naked"),
                Property<GeneratedCompletionSearchResult>(raw, "Completion"),
                Property<ClusterRecoveryValidationResult>(raw, "Result"));
            Assert.That(cachedSource.Map19_04.Success, Is.True,
                string.Join("\n", cachedSource.Map19_04.Failures));
            return cachedSource;
        }

        private static T Property<T>(object source, string name) => (T)source.GetType()
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public).GetValue(source);

        private static GeneratedTileMovementNode Node(GeneratedTileMovementGraph graph,
            int x, int y) => graph.Nodes.Single(value =>
            value.Kind == GeneratedTileMovementNodeKind.Stand &&
            value.Cell.X == x && value.Cell.Y == y);

        private static TraversalMovementKind[] Movements() =>
            Enum.GetValues(typeof(TraversalMovementKind)).Cast<TraversalMovementKind>().ToArray();

        private static string Hash(string value) =>
            BakingCanonicalDigest.HashCanonicalLines(new[] { value });

        private static string[] UpstreamDigests(SourceScenario source) => new[]
        {
            source.Graph.GraphDigest, source.Graph.Map19_03HandoffDigest,
            source.Naked.SuccessProofDigest, source.Completion.SuccessProofDigest,
            source.Map19_04.RecoverySuccessDigest, source.Map19_04.DensitySuccessDigest,
            source.Map19_04.CombinedSuccessDigest, source.Map19_04.Map19_05HandoffDigest,
        };

        private static void AssertSuccess(GeneratedRepetitionValidationResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            Assert.That(result.Surface, Is.Not.Null);
            Assert.That(result.Failures, Is.Empty);
        }

        private static void AssertAtomic(GeneratedRepetitionValidationResult result,
            string expectedReason)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Surface, Is.Null);
            Assert.That(result.RepetitionSuccessDigest, Is.Empty);
            Assert.That(result.RemovalSuccessDigest, Is.Empty);
            Assert.That(result.Map19_06HandoffDigest, Is.Empty);
            Assert.That(result.Failures.Select(value => value.Reason),
                Does.Contain(expectedReason));
            Assert.That(result.Failures.All(value => !string.IsNullOrEmpty(value.Owner) &&
                !string.IsNullOrEmpty(value.Reason) && !string.IsNullOrEmpty(value.CaseId) &&
                !string.IsNullOrEmpty(value.OffendingKey) &&
                !string.IsNullOrEmpty(value.Expected) && !string.IsNullOrEmpty(value.Actual) &&
                !string.IsNullOrEmpty(value.SourceDigest) &&
                !string.IsNullOrEmpty(value.WindowOrCompletionFrontierEvidence)), Is.True);
        }

        private static void WriteEvidence(GeneratedRepetitionValidationResult result)
        {
            var repetition = result.Surface.Repetition;
            var removal = result.Surface.Removal;
            TestContext.Out.WriteLine("REPETITION_SOURCES_CHECKED=" +
                repetition.RepetitionSourcesChecked);
            TestContext.Out.WriteLine("PATTERN_SIGNATURES_CHECKED=" +
                repetition.PatternSignaturesChecked);
            TestContext.Out.WriteLine("PATTERN_MIRROR_PAIRS_CHECKED=" +
                repetition.PatternMirrorPairsChecked);
            TestContext.Out.WriteLine("CLUSTER_SIGNATURES_CHECKED=" +
                repetition.ClusterSignaturesChecked);
            TestContext.Out.WriteLine("CLUSTER_WINDOWS_CHECKED=" +
                repetition.ClusterRepetitionWindowsChecked);
            TestContext.Out.WriteLine("ACTIVITY_EVENT_SIGNATURES_CHECKED=" +
                repetition.ActivityEventSignaturesChecked);
            TestContext.Out.WriteLine("ACTIVITY_EVENT_WINDOWS_CHECKED=" +
                repetition.ActivityEventRepetitionWindowsChecked);
            TestContext.Out.WriteLine("MATERIAL_ONLY_COLLAPSES=" +
                repetition.MaterialOnlyDuplicateCollapses);
            TestContext.Out.WriteLine("REPETITION_INPUT_DIGEST=" + repetition.InputDigest);
            TestContext.Out.WriteLine("REPETITION_VALIDATION_DIGEST=" +
                repetition.ValidationDigest);
            TestContext.Out.WriteLine("ACTIVITY_STRUCTURES_REMOVED=" +
                removal.ActivityStructuresRemoved);
            TestContext.Out.WriteLine("EVENT_OVERLAYS_REMOVED=" +
                removal.EventOverlaysRemoved);
            TestContext.Out.WriteLine("STATIC_TRANSITIONS_RETAINED=" +
                removal.StaticTransitionsRetained);
            TestContext.Out.WriteLine("REMOVED_TRANSITIONS=" + removal.RemovedTransitions);
            TestContext.Out.WriteLine("SHORTEST_TRANSITION_COUNT=" +
                removal.ProofShortestTransitionCount);
            TestContext.Out.WriteLine("REMOVAL_INPUT_DIGEST=" + removal.InputDigest);
            TestContext.Out.WriteLine("REMOVAL_PROOF_DIGEST=" + removal.ProofDigest);
            TestContext.Out.WriteLine("COMBINED_DIGEST=" + result.CombinedSuccessDigest);
            TestContext.Out.WriteLine("MAP19_06_HANDOFF_DIGEST=" +
                result.Map19_06HandoffDigest);
        }

        private sealed class ValidationRun
        {
            public ValidationRun(GeneratedRepetitionValidationInput repetitionInput,
                GeneratedEventRemovalValidationInput removalInput,
                GeneratedRepetitionValidationResult result)
            {
                RepetitionInput = repetitionInput;
                RemovalInput = removalInput;
                Result = result;
            }
            public GeneratedRepetitionValidationInput RepetitionInput { get; }
            public GeneratedEventRemovalValidationInput RemovalInput { get; }
            public GeneratedRepetitionValidationResult Result { get; }
        }

        private sealed class SourceScenario
        {
            public SourceScenario(GeneratedTileMovementGraph graph, SectorCanvasContract canvas,
                GeneratedSliceSet slices, NakedTraversalSearchResult naked,
                GeneratedCompletionSearchResult completion,
                ClusterRecoveryValidationResult map19_04)
            {
                Graph = graph;
                Canvas = canvas;
                Slices = slices;
                Naked = naked;
                Completion = completion;
                Map19_04 = map19_04;
            }
            public GeneratedTileMovementGraph Graph { get; }
            public SectorCanvasContract Canvas { get; }
            public GeneratedSliceSet Slices { get; }
            public NakedTraversalSearchResult Naked { get; }
            public GeneratedCompletionSearchResult Completion { get; }
            public ClusterRecoveryValidationResult Map19_04 { get; }
        }

        private sealed class ContractSet
        {
            public ContractSet(TerrainClusterContract clusterA,
                TerrainClusterContract clusterB, ActivityStructureContract activityA,
                ActivityStructureContract activityB, EventOverlayContract eventA)
            {
                ClusterA = clusterA;
                ClusterB = clusterB;
                ActivityA = activityA;
                ActivityB = activityB;
                EventA = eventA;
            }
            public TerrainClusterContract ClusterA { get; }
            public TerrainClusterContract ClusterB { get; }
            public ActivityStructureContract ActivityA { get; }
            public ActivityStructureContract ActivityB { get; }
            public EventOverlayContract EventA { get; }
        }
    }
}
