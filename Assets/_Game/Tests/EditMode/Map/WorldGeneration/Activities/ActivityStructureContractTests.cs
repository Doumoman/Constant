using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Activities
{
    [TestFixture]
    [Category("MAP09_05")]
    public sealed class ActivityStructureContractTests
    {
        [Test]
        public void ValidActivityPublishesContractAndCanonicalDigest()
        {
            var shell = ActivityEventFixture.CreateShell();
            var result = ActivityContractValidator.Validate(ActivityEventFixture.CreateActivity(shell), shell);
            Assert.That(result.IsValid, Is.True, ActivityEventFixture.Join(result));
            Assert.That(result.Contract, Is.Not.Null);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void ExactSlotCueMechanismAndProgressionEnumsArePublished()
        {
            Assert.That(Enum.GetNames(typeof(ActivitySlotKind)), Is.EqualTo(new[]
                { "Cue", "Trigger", "Device", "Hazard", "Projectile", "Reward", "Recovery", "Reset", "Npc" }));
            Assert.That(Enum.GetNames(typeof(ActivityCueKind)), Is.EqualTo(new[]
                { "Visual", "Audio", "Environment", "Motion" }));
            Assert.That(Enum.GetNames(typeof(MechanismNodeKind)), Is.EqualTo(new[]
                { "CueEmitter", "Trigger", "Device", "Hazard", "ProjectileEmitter", "RewardEmitter", "RecoveryController", "ResetController" }));
            Assert.That(Enum.GetNames(typeof(MechanismRelationKind)), Is.EqualTo(new[]
                { "Activates", "Drives", "Emits", "Enables", "Disables", "Resets" }));
            Assert.That(Enum.GetNames(typeof(ProgressionPhaseKind)), Is.EqualTo(new[]
                { "Cue", "Activation", "Core", "Reward", "Recovery", "Reset", "Exit" }));
            Assert.That(Enum.GetNames(typeof(ProgressionEdgeKind)), Is.EqualTo(new[]
                { "Advance", "Failure", "Reset", "Exit" }));
        }

        [TestCase("")]
        [TestCase("ACT_")]
        [TestCase("act_BAD")]
        [TestCase("ACT-BAD")]
        [TestCase(" ACT_BAD")]
        public void InvalidActivityIdsAreRejected(string value)
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source,
                id: new ActivityStructureId(value)), shell), ActivityValidationErrorCode.InvalidId);
        }

        [TestCase(ActivitySlotKind.Cue)]
        [TestCase(ActivitySlotKind.Trigger)]
        [TestCase(ActivitySlotKind.Recovery)]
        public void RequiredSlotKindsCannotBeRemoved(ActivitySlotKind kind)
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source,
                slots: source.Slots.Where(value => value.Kind != kind)), shell),
                ActivityValidationErrorCode.MissingCueOrTrigger);
        }

        [Test]
        public void SlotIdsMarkersCoordinatesAndUniquenessAreValidated()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var broken = source.Slots.Concat(new[]
            {
                new ActivitySlot(new ActivitySlotId("slot_BAD"), ActivitySlotKind.Cue,
                    new LocalTileCoord(200, 2), "bad-marker"),
                source.Slots[0],
            });
            var result = ActivityEventFixture.Validate(ActivityEventFixture.With(source, slots: broken), shell);
            AssertError(result, ActivityValidationErrorCode.InvalidSlot);
            Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void CueMustBeNonEmptyUniqueCueSlottedAndPreActivationDetectable()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source,
                cues: Array.Empty<ActivityCue>()), shell), ActivityValidationErrorCode.MissingCueOrTrigger);
            var bad = new[]
            {
                new ActivityCue(ActivityCueKind.Visual, new ActivitySlotId("SLOT_TRIGGER"), false),
                new ActivityCue(ActivityCueKind.Visual, new ActivitySlotId("SLOT_TRIGGER"), false),
            };
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, cues: bad), shell),
                ActivityValidationErrorCode.InvalidCue);
        }

        [TestCase(TraversalGraphKind.Traversal)]
        [TestCase(TraversalGraphKind.Progression)]
        public void MechanismGraphRejectsForeignGraphKinds(TraversalGraphKind graphKind)
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var graph = new MechanismGraph(source.MechanismGraph.Nodes, source.MechanismGraph.Edges, graphKind);
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, mechanism: graph), shell),
                ActivityValidationErrorCode.InvalidGraphKind);
        }

        [Test]
        public void MechanismNodesRequireCompatibleSlotsAndUniqueStableIds()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var nodes = source.MechanismGraph.Nodes.Concat(new[]
            {
                source.MechanismGraph.Nodes[0],
                new MechanismNode("mech_BAD", MechanismNodeKind.RewardEmitter,
                    new ActivitySlotId("SLOT_TRIGGER")),
            });
            var graph = new MechanismGraph(nodes, source.MechanismGraph.Edges);
            var result = ActivityEventFixture.Validate(ActivityEventFixture.With(source, mechanism: graph), shell);
            AssertError(result, ActivityValidationErrorCode.DuplicateNodeOrEdge);
            AssertError(result, ActivityValidationErrorCode.MissingReference);
            AssertError(result, ActivityValidationErrorCode.InvalidId);
        }

        [Test]
        public void MechanismEdgesRejectMissingSelfDuplicateAndInvalidRelations()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var edges = source.MechanismGraph.Edges.Concat(new[]
            {
                source.MechanismGraph.Edges[0],
                new MechanismEdge("MECH_EDGE_SELF", "MECH_DEVICE", "MECH_DEVICE", MechanismRelationKind.Drives),
                new MechanismEdge("MECH_EDGE_MISSING", "MECH_MISSING", "MECH_DEVICE", MechanismRelationKind.Activates),
                new MechanismEdge("MECH_EDGE_BAD_REL", "MECH_REWARD", "MECH_DEVICE", MechanismRelationKind.Drives),
            });
            var graph = new MechanismGraph(source.MechanismGraph.Nodes, edges);
            var result = ActivityEventFixture.Validate(ActivityEventFixture.With(source, mechanism: graph), shell);
            AssertError(result, ActivityValidationErrorCode.DuplicateNodeOrEdge);
            AssertError(result, ActivityValidationErrorCode.MissingReference);
            AssertError(result, ActivityValidationErrorCode.InvalidMechanismRelation);
        }

        [Test]
        public void MechanismRequiresExactlyOneTriggerAndOneTriggerReachableComponent()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var orphanGraph = new MechanismGraph(source.MechanismGraph.Nodes,
                source.MechanismGraph.Edges.Where(value => value.ToNodeId != "MECH_RECOVERY"));
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, mechanism: orphanGraph), shell),
                ActivityValidationErrorCode.UnreachableMechanismNode);
            var noTrigger = new MechanismGraph(source.MechanismGraph.Nodes.Where(value => value.Kind != MechanismNodeKind.Trigger),
                source.MechanismGraph.Edges);
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, mechanism: noTrigger), shell),
                ActivityValidationErrorCode.MissingCueOrTrigger);
        }

        [TestCase(TraversalGraphKind.Traversal)]
        [TestCase(TraversalGraphKind.Mechanism)]
        public void ProgressionGraphRejectsForeignGraphKinds(TraversalGraphKind graphKind)
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var graph = new ProgressionGraph(source.ProgressionGraph.StartNodeId,
                source.ProgressionGraph.TerminalNodeId, source.ProgressionGraph.Nodes,
                source.ProgressionGraph.Edges, graphKind);
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, progression: graph), shell),
                ActivityValidationErrorCode.InvalidGraphKind);
        }

        [TestCase(ProgressionPhaseKind.Cue)]
        [TestCase(ProgressionPhaseKind.Activation)]
        [TestCase(ProgressionPhaseKind.Core)]
        [TestCase(ProgressionPhaseKind.Reward)]
        [TestCase(ProgressionPhaseKind.Recovery)]
        [TestCase(ProgressionPhaseKind.Exit)]
        public void EveryRequiredProgressionPhaseIsEnforced(ProgressionPhaseKind phase)
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var nodes = source.ProgressionGraph.Nodes.Where(value => value.Phase != phase).ToArray();
            var graph = new ProgressionGraph(source.ProgressionGraph.StartNodeId,
                source.ProgressionGraph.TerminalNodeId, nodes, source.ProgressionGraph.Edges);
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, progression: graph), shell),
                ActivityValidationErrorCode.MissingProgressionPhase);
        }

        [Test]
        public void ProgressionStartTerminalAndOrderedSuccessPathAreRequired()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var wrongEnds = new ProgressionGraph("PROG_CORE", "PROG_RECOVERY",
                source.ProgressionGraph.Nodes, source.ProgressionGraph.Edges);
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, progression: wrongEnds), shell),
                ActivityValidationErrorCode.InvalidProgressionOrder);
            var withoutRewardAdvance = new ProgressionGraph(source.ProgressionGraph.StartNodeId,
                source.ProgressionGraph.TerminalNodeId, source.ProgressionGraph.Nodes,
                source.ProgressionGraph.Edges.Where(value => value.EdgeId != "PROG_EDGE_CORE_REWARD"));
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, progression: withoutRewardAdvance), shell),
                ActivityValidationErrorCode.InvalidProgressionOrder);
        }

        [Test]
        public void FailureResetAndExitDirectionRulesAreEnforced()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var extra = new[]
            {
                new ProgressionEdge("PROG_EDGE_BAD_FAILURE", "PROG_CORE", "PROG_REWARD", ProgressionEdgeKind.Failure),
                new ProgressionEdge("PROG_EDGE_BAD_RESET", "PROG_RESET", "PROG_REWARD", ProgressionEdgeKind.Reset),
                new ProgressionEdge("PROG_EDGE_BAD_EXIT", "PROG_EXIT", "PROG_CORE", ProgressionEdgeKind.Exit),
            };
            var graph = new ProgressionGraph(source.ProgressionGraph.StartNodeId,
                source.ProgressionGraph.TerminalNodeId, source.ProgressionGraph.Nodes,
                source.ProgressionGraph.Edges.Concat(extra));
            var result = ActivityEventFixture.Validate(ActivityEventFixture.With(source, progression: graph), shell);
            AssertError(result, ActivityValidationErrorCode.InvalidFailureOrReset);
            AssertError(result, ActivityValidationErrorCode.InvalidProgressionOrder);
        }

        [Test]
        public void InfiniteProgressionComponentWithoutRecoveryOrExitIsRejected()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var edges = new[]
            {
                new ProgressionEdge("PROG_EDGE_CUE_ACTIVATION", "PROG_CUE", "PROG_ACTIVATION", ProgressionEdgeKind.Advance),
                new ProgressionEdge("PROG_EDGE_ACTIVATION_CORE", "PROG_ACTIVATION", "PROG_CORE", ProgressionEdgeKind.Advance),
                new ProgressionEdge("PROG_EDGE_CORE_RESET", "PROG_CORE", "PROG_RESET", ProgressionEdgeKind.Failure),
                new ProgressionEdge("PROG_EDGE_RESET_CORE", "PROG_RESET", "PROG_CORE", ProgressionEdgeKind.Reset),
            };
            var graph = new ProgressionGraph("PROG_CUE", "PROG_EXIT", source.ProgressionGraph.Nodes, edges);
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, progression: graph), shell),
                ActivityValidationErrorCode.NoRecoveryOrExit);
        }

        [Test]
        public void RemovalSafetyRequiresNonEmptyUniqueInFootprintSafeAndRecoveryTiles()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var safety = ActivityEventFixture.CreateSafety(shell,
                safe: Array.Empty<LocalTileCoord>(),
                recovery: new[] { new LocalTileCoord(100, 100), new LocalTileCoord(100, 100) });
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, safety: safety), shell),
                ActivityValidationErrorCode.InvalidRemovalSafety);
        }

        [Test]
        public void RemovalSafetyRejectsChangedRouteAccessTraversalAndExitIdentity()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var safety = ActivityEventFixture.CreateSafety(shell, routeAfter: 2,
                accessAfter: AccessClass.OptionalTool, digestAfter: new string('a', 64), exitId: "NODE_CORE");
            AssertError(ActivityEventFixture.Validate(ActivityEventFixture.With(source, safety: safety), shell),
                ActivityValidationErrorCode.InvalidRemovalSafety);
        }

        [Test]
        public void RemovalSafetyRejectsUnsafeFlagsAndProtectedPermanentWrite()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var protectedTile = shell.Traversal.Variants[0].Edges[0].Envelope.ProtectedTiles[0];
            var safety = ActivityEventFixture.CreateSafety(shell, preserveTraversal: false,
                preserveAccess: false, permanentAllowed: true, exitDestruction: true,
                writes: new[] { protectedTile });
            var result = ActivityEventFixture.Validate(ActivityEventFixture.With(source, safety: safety), shell);
            AssertError(result, ActivityValidationErrorCode.InvalidRemovalSafety);
            AssertError(result, ActivityValidationErrorCode.ProtectedMutation);
        }

        [Test]
        public void ActivityRejectsWrongShellAndSpineReferences()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var broken = ActivityEventFixture.With(source,
                clusterId: new TerrainClusterId("TC_OTHER"), spineId: new SpineVariantId("SPINE_OTHER"));
            AssertError(ActivityEventFixture.Validate(broken, shell), ActivityValidationErrorCode.InvalidShellReference);
        }

        [Test]
        public void CollectionsAreDefensiveReadOnlyAndCanonicalOrderIndependent()
        {
            var shell = ActivityEventFixture.CreateShell();
            var slots = ActivityEventFixture.CreateSlots().ToList();
            var activity = ActivityEventFixture.CreateActivity(shell, slots);
            var before = ActivityEventFixture.Validate(activity, shell).CanonicalDigest;
            slots.Clear();
            Assert.That(activity.Slots, Has.Count.EqualTo(6));
            Assert.That(activity.Slots, Is.InstanceOf<System.Collections.IList>());
            Assert.Throws<NotSupportedException>(() => ((System.Collections.IList)activity.Slots).Clear());
            var reversed = ActivityEventFixture.With(activity,
                slots: activity.Slots.Reverse(), cues: activity.Cues.Reverse(),
                mechanism: new MechanismGraph(activity.MechanismGraph.Nodes.Reverse(), activity.MechanismGraph.Edges.Reverse()),
                progression: new ProgressionGraph(activity.ProgressionGraph.StartNodeId,
                    activity.ProgressionGraph.TerminalNodeId, activity.ProgressionGraph.Nodes.Reverse(),
                    activity.ProgressionGraph.Edges.Reverse()));
            Assert.That(ActivityEventFixture.Validate(reversed, shell).CanonicalDigest, Is.EqualTo(before));
        }

        [Test]
        public void DisplayTextDoesNotAffectDigestButEveryContractSemanticDoes()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var baseline = ActivityEventFixture.Validate(source, shell).CanonicalDigest;
            Assert.That(ActivityEventFixture.Validate(ActivityEventFixture.With(source, display: "다른 표시"), shell).CanonicalDigest,
                Is.EqualTo(baseline));
            var changedCues = source.Cues.Select(value => new ActivityCue(ActivityCueKind.Audio,
                value.SlotId, value.DetectableBeforeActivation));
            Assert.That(ActivityEventFixture.Validate(ActivityEventFixture.With(source, cues: changedCues), shell).CanonicalDigest,
                Is.Not.EqualTo(baseline));
        }

        [Test]
        public void InvalidInputAccumulatesStableErrorsAndPublishesNoPartialContractOrDigest()
        {
            var shell = ActivityEventFixture.CreateShell();
            var source = ActivityEventFixture.CreateActivity(shell);
            var broken = ActivityEventFixture.With(source, id: new ActivityStructureId("bad"),
                slots: Array.Empty<ActivitySlot>(), cues: Array.Empty<ActivityCue>(),
                mechanism: new MechanismGraph(Array.Empty<MechanismNode>(), Array.Empty<MechanismEdge>()));
            var first = ActivityEventFixture.Validate(broken, shell);
            var second = ActivityEventFixture.Validate(broken, shell);
            Assert.That(first.IsValid, Is.False);
            Assert.That(first.Contract, Is.Null);
            Assert.That(first.CanonicalDigest, Is.Empty);
            Assert.That(first.Errors.Select(value => value.ToString()), Is.EqualTo(second.Errors.Select(value => value.ToString())));
            Assert.That(first.Errors, Is.Ordered);
        }

        [Test]
        public void ContractSurfaceIsImmutableAndGraphOwnershipIsSeparated()
        {
            foreach (var type in new[]
                     {
                         typeof(ActivitySlot), typeof(ActivityCue), typeof(MechanismNode), typeof(MechanismEdge),
                         typeof(MechanismGraph), typeof(ProgressionNode), typeof(ProgressionEdge),
                         typeof(ProgressionGraph), typeof(ActivityRemovalSafety), typeof(ActivityStructureContract),
                     })
            {
                Assert.That(type.IsSealed, Is.True, type.Name);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(value => value.SetMethod != null), Is.Empty, type.Name);
            }
            Assert.That(typeof(ActivityStructureContract).GetProperties().Select(value => value.Name),
                Does.Contain("MechanismGraph").And.Contain("ProgressionGraph"));
        }

        [Test]
        public void ProductionScopeContainsNoExecutionRngFileOrUnityLifecycleDependencies()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath,
                "_Game/Map/Runtime/WorldGeneration/Activities"));
            var source = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal).Select(File.ReadAllText));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine", "System.IO", "System.Random", "Random.",
                         "MonoBehaviour", "Update()", "StageMapGenerator", "GridWorld", "RoomTemplate",
                         "RoomGridTransform", "TileMutationService", "SectorRecipeResolver", "TraversalMovementKind",
                     })
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
        }

        private static void AssertError(ActivityValidationResult result, ActivityValidationErrorCode code)
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), ActivityEventFixture.Join(result));
        }
    }

    internal static class ActivityEventFixture
    {
        public static TerrainClusterContract CreateShell()
        {
            var roles = new[]
            {
                Role("ANCHOR_ENTRY", ClusterRoleKind.Entry, 0, "NODE_ENTRY"),
                Role("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp, 4, "NODE_BUILD_UP"),
                Role("ANCHOR_CORE", ClusterRoleKind.Core, 9, "NODE_CORE"),
                Role("ANCHOR_RECOVERY", ClusterRoleKind.Recovery, 15, "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_REWARD", ClusterRoleKind.Reward, new LocalTileCoord(18, 2), "NODE_REWARD"),
                Role("ANCHOR_EXIT", ClusterRoleKind.Exit, 23, "NODE_EXIT"),
            };
            var nodes = roles.Select(value => new TraversalNode(value.TraversalNodeId, value.Tile,
                value.Role != ClusterRoleKind.Reward, value.AnchorId)).ToArray();
            var byId = nodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var edges = new[]
            {
                Edge("EDGE_ENTRY_BUILD", byId["NODE_ENTRY"], byId["NODE_BUILD_UP"], TraversalMovementKind.Walk),
                Edge("EDGE_BUILD_CORE", byId["NODE_BUILD_UP"], byId["NODE_CORE"], TraversalMovementKind.Jump),
                Edge("EDGE_CORE_RECOVERY", byId["NODE_CORE"], byId["NODE_RECOVERY"], TraversalMovementKind.Drop),
                Edge("EDGE_RECOVERY_EXIT", byId["NODE_RECOVERY"], byId["NODE_EXIT"], TraversalMovementKind.Slide),
            };
            var variant = new SpineVariant(new SpineVariantId("SPINE_ACTIVITY_BASELINE"), true,
                TraversalGraphKind.Traversal, nodes, edges);
            return new TerrainClusterContract(new TerrainClusterId("TC_ACTIVITY_SHELL"),
                new ClusterFootprint(new[] { new ClusterChunkCoord(0, 0), new ClusterChunkCoord(1, 0) }),
                roles,
                new[]
                {
                    new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                        roles[0].Tile, ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                    new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                        roles[5].Tile, ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
                },
                new TerrainClusterTraversalContract(new[] { variant }), "Fixture shell");
        }

        public static ActivityStructureContract CreateActivity(
            TerrainClusterContract shell,
            IEnumerable<ActivitySlot> slots = null)
        {
            var actualSlots = (slots ?? CreateSlots()).ToArray();
            return new ActivityStructureContract(new ActivityStructureId("ACT_LIVE_BASELINE"), shell.Id,
                new SpineVariantId("SPINE_ACTIVITY_BASELINE"),
                new[] { PacingRole.Activity, PacingRole.Risk, PacingRole.Recovery },
                new[] { AccessClass.MandatoryNoTool, AccessClass.OptionalNoTool },
                actualSlots,
                new[] { new ActivityCue(ActivityCueKind.Visual, new ActivitySlotId("SLOT_CUE"), true) },
                CreateMechanism(), CreateProgression(), CreateSafety(shell), "Fixture activity");
        }

        public static IEnumerable<ActivitySlot> CreateSlots()
        {
            return new[]
            {
                Slot("SLOT_CUE", ActivitySlotKind.Cue, 1),
                Slot("SLOT_TRIGGER", ActivitySlotKind.Trigger, 2),
                Slot("SLOT_DEVICE", ActivitySlotKind.Device, 4),
                Slot("SLOT_REWARD", ActivitySlotKind.Reward, 8),
                Slot("SLOT_RECOVERY", ActivitySlotKind.Recovery, 15),
                Slot("SLOT_RESET", ActivitySlotKind.Reset, 16),
            };
        }

        public static MechanismGraph CreateMechanism()
        {
            var nodes = new[]
            {
                new MechanismNode("MECH_TRIGGER", MechanismNodeKind.Trigger, new ActivitySlotId("SLOT_TRIGGER")),
                new MechanismNode("MECH_DEVICE", MechanismNodeKind.Device, new ActivitySlotId("SLOT_DEVICE")),
                new MechanismNode("MECH_REWARD", MechanismNodeKind.RewardEmitter, new ActivitySlotId("SLOT_REWARD")),
                new MechanismNode("MECH_RECOVERY", MechanismNodeKind.RecoveryController, new ActivitySlotId("SLOT_RECOVERY")),
                new MechanismNode("MECH_RESET", MechanismNodeKind.ResetController, new ActivitySlotId("SLOT_RESET")),
            };
            return new MechanismGraph(nodes, new[]
            {
                new MechanismEdge("MECH_EDGE_TRIGGER_DEVICE", "MECH_TRIGGER", "MECH_DEVICE", MechanismRelationKind.Activates),
                new MechanismEdge("MECH_EDGE_DEVICE_REWARD", "MECH_DEVICE", "MECH_REWARD", MechanismRelationKind.Drives),
                new MechanismEdge("MECH_EDGE_DEVICE_RECOVERY", "MECH_DEVICE", "MECH_RECOVERY", MechanismRelationKind.Enables),
                new MechanismEdge("MECH_EDGE_DEVICE_RESET", "MECH_DEVICE", "MECH_RESET", MechanismRelationKind.Enables),
            });
        }

        public static ProgressionGraph CreateProgression()
        {
            var nodes = new[]
            {
                new ProgressionNode("PROG_CUE", ProgressionPhaseKind.Cue),
                new ProgressionNode("PROG_ACTIVATION", ProgressionPhaseKind.Activation),
                new ProgressionNode("PROG_CORE", ProgressionPhaseKind.Core),
                new ProgressionNode("PROG_REWARD", ProgressionPhaseKind.Reward),
                new ProgressionNode("PROG_RECOVERY", ProgressionPhaseKind.Recovery),
                new ProgressionNode("PROG_RESET", ProgressionPhaseKind.Reset),
                new ProgressionNode("PROG_EXIT", ProgressionPhaseKind.Exit),
            };
            return new ProgressionGraph("PROG_CUE", "PROG_EXIT", nodes, new[]
            {
                new ProgressionEdge("PROG_EDGE_CUE_ACTIVATION", "PROG_CUE", "PROG_ACTIVATION", ProgressionEdgeKind.Advance),
                new ProgressionEdge("PROG_EDGE_ACTIVATION_CORE", "PROG_ACTIVATION", "PROG_CORE", ProgressionEdgeKind.Advance),
                new ProgressionEdge("PROG_EDGE_CORE_REWARD", "PROG_CORE", "PROG_REWARD", ProgressionEdgeKind.Advance),
                new ProgressionEdge("PROG_EDGE_REWARD_RECOVERY", "PROG_REWARD", "PROG_RECOVERY", ProgressionEdgeKind.Advance),
                new ProgressionEdge("PROG_EDGE_RECOVERY_EXIT", "PROG_RECOVERY", "PROG_EXIT", ProgressionEdgeKind.Exit),
                new ProgressionEdge("PROG_EDGE_CORE_RESET", "PROG_CORE", "PROG_RESET", ProgressionEdgeKind.Failure),
                new ProgressionEdge("PROG_EDGE_RESET_CORE", "PROG_RESET", "PROG_CORE", ProgressionEdgeKind.Reset),
            });
        }

        public static ActivityRemovalSafety CreateSafety(
            TerrainClusterContract shell,
            IEnumerable<LocalTileCoord> safe = null,
            IEnumerable<LocalTileCoord> recovery = null,
            bool preserveTraversal = true,
            bool preserveAccess = true,
            bool permanentAllowed = false,
            bool exitDestruction = false,
            int routeAfter = 1,
            AccessClass accessAfter = AccessClass.MandatoryNoTool,
            string digestAfter = null,
            string exitId = "NODE_EXIT",
            IEnumerable<LocalTileCoord> writes = null)
        {
            var digest = TerrainClusterContractValidator.Validate(shell).CanonicalDigest;
            return new ActivityRemovalSafety(new SpineVariantId("SPINE_ACTIVITY_BASELINE"),
                "NODE_ENTRY", exitId, safe ?? new[] { new LocalTileCoord(5, 2) },
                recovery ?? new[] { new LocalTileCoord(15, 2) }, preserveTraversal, preserveAccess,
                permanentAllowed, exitDestruction, 1, routeAfter, AccessClass.MandatoryNoTool,
                accessAfter, digest, digestAfter ?? digest, writes);
        }

        public static ActivityStructureContract With(
            ActivityStructureContract source,
            ActivityStructureId? id = null,
            TerrainClusterId? clusterId = null,
            SpineVariantId? spineId = null,
            IEnumerable<ActivitySlot> slots = null,
            IEnumerable<ActivityCue> cues = null,
            MechanismGraph mechanism = null,
            ProgressionGraph progression = null,
            ActivityRemovalSafety safety = null,
            string display = null)
        {
            return new ActivityStructureContract(id ?? source.Id, clusterId ?? source.TerrainClusterId,
                spineId ?? source.CompatibleSpineVariantId, source.CompatiblePacingRoles,
                source.CompatibleAccessClasses, slots ?? source.Slots, cues ?? source.Cues,
                mechanism ?? source.MechanismGraph, progression ?? source.ProgressionGraph,
                safety ?? source.RemovalSafety, display ?? source.DisplayText);
        }

        public static ActivityValidationResult Validate(ActivityStructureContract activity, TerrainClusterContract shell)
            => ActivityContractValidator.Validate(activity, shell);

        public static string Join(ActivityValidationResult result)
            => string.Join("\n", result.Errors.Select(value => value.ToString()));

        public static EventOverlayRemovalEvidence CreateEventEvidence(TerrainClusterContract shell, ActivityStructureContract activity)
        {
            var shellDigest = TerrainClusterContractValidator.Validate(shell).CanonicalDigest;
            var activityDigest = ActivityContractValidator.Validate(activity, shell).CanonicalDigest;
            var mandatoryPathDigest = new string('b', 64);
            return new EventOverlayRemovalEvidence(shellDigest, shellDigest, mandatoryPathDigest,
                mandatoryPathDigest, AccessClass.MandatoryNoTool, AccessClass.MandatoryNoTool,
                activityDigest, activityDigest);
        }

        private static ActivitySlot Slot(string id, ActivitySlotKind kind, int x)
            => new ActivitySlot(new ActivitySlotId(id), kind, new LocalTileCoord(x, 2),
                "MARKER_ACTIVITY_" + id.Substring("SLOT_".Length));

        private static ClusterRoleAnchor Role(string id, ClusterRoleKind kind, int x, string node)
            => new ClusterRoleAnchor(id, kind, new LocalTileCoord(x, 1), node);

        private static TraversalEdge Edge(string id, TraversalNode from, TraversalNode to, TraversalMovementKind movement)
        {
            var floor = movement == TraversalMovementKind.Walk || movement == TraversalMovementKind.Slide
                ? new[] { new LocalTileCoord(from.Tile.X, 0) }
                : Array.Empty<LocalTileCoord>();
            var jump = movement == TraversalMovementKind.Jump
                ? new[] { new LocalTileCoord((from.Tile.X + to.Tile.X) / 2, 3) }
                : Array.Empty<LocalTileCoord>();
            var drop = movement == TraversalMovementKind.Drop
                ? new[] { new LocalTileCoord((from.Tile.X + to.Tile.X) / 2, 2) }
                : Array.Empty<LocalTileCoord>();
            var envelope = new TraversalEnvelope(new[] { from.Tile, to.Tile }, floor,
                new[] { from.Tile, to.Tile }, jump, drop, new[] { to.Tile }, new[] { to.Tile });
            return new TraversalEdge(id, from.NodeId, to.NodeId, movement, from.Tile, to.Tile,
                1, 2, to.Tile, to.Tile, true, envelope);
        }
    }
}
