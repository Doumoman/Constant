using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.Tests.EditMode.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.EventOverlays
{
    [TestFixture]
    [Category("MAP09_05")]
    public sealed class EventOverlayContractTests
    {
        [Test]
        public void ValidMarkerOnlyOverlayPublishesContractAndDigest()
        {
            var fixture = CreateFixture();
            var result = Validate(fixture, CreateOverlay(fixture));
            Assert.That(result.IsValid, Is.True, Join(result));
            Assert.That(result.Contract, Is.Not.Null);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void ExplicitEmptyOverlayIsTheOnlyZeroAssignmentVariant()
        {
            var fixture = CreateFixture();
            var empty = new EventOverlayContract(new EventOverlayId("EVT_EMPTY"), EventOverlayKind.Empty,
                fixture.Shell.Id, fixture.Activity.Id, Array.Empty<EventMarkerAssignment>());
            Assert.That(Validate(fixture, empty).IsValid, Is.True);
            AssertError(Validate(fixture, new EventOverlayContract(new EventOverlayId("EVT_BAD_EMPTY"),
                EventOverlayKind.Empty, fixture.Shell.Id, fixture.Activity.Id,
                new[] { Assignment("MARKER_ACTIVITY_CUE", EventMarkerOperation.EnableMarker, "COSMETIC_ON") })),
                EventOverlayValidationErrorCode.EmptyWithAssignment);
            AssertError(Validate(fixture, new EventOverlayContract(new EventOverlayId("EVT_BAD_NPC"),
                EventOverlayKind.Npc, fixture.Shell.Id, fixture.Activity.Id, Array.Empty<EventMarkerAssignment>())),
                EventOverlayValidationErrorCode.NonEmptyWithoutAssignment);
        }

        [Test]
        public void ExactOverlayKindsAndMarkerOperationsArePublished()
        {
            Assert.That(Enum.GetNames(typeof(EventOverlayKind)),
                Is.EqualTo(new[] { "Npc", "Reward", "State", "Cosmetic", "Empty" }));
            Assert.That(Enum.GetNames(typeof(EventMarkerOperation)),
                Is.EqualTo(new[] { "EnableMarker", "DisableMarker", "SpawnNpc", "SpawnReward", "SetState" }));
        }

        [TestCase("")]
        [TestCase("EVT_")]
        [TestCase("evt_BAD")]
        [TestCase("EVT-BAD")]
        [TestCase(" EVT_BAD")]
        public void InvalidEventIdsAreRejected(string value)
        {
            var fixture = CreateFixture();
            var source = CreateOverlay(fixture);
            var broken = new EventOverlayContract(new EventOverlayId(value), source.Kind,
                source.TerrainClusterId, source.ActivityStructureId, source.Assignments);
            AssertError(Validate(fixture, broken), EventOverlayValidationErrorCode.InvalidId);
        }

        [TestCase(EventOverlayKind.Npc, EventMarkerOperation.SpawnNpc)]
        [TestCase(EventOverlayKind.Reward, EventMarkerOperation.SpawnReward)]
        [TestCase(EventOverlayKind.State, EventMarkerOperation.SetState)]
        [TestCase(EventOverlayKind.Cosmetic, EventMarkerOperation.EnableMarker)]
        [TestCase(EventOverlayKind.Cosmetic, EventMarkerOperation.DisableMarker)]
        public void KindOperationCompatibilityMatrixAcceptsOnlyPublishedPairs(
            EventOverlayKind kind,
            EventMarkerOperation operation)
        {
            var fixture = CreateFixture();
            var contract = new EventOverlayContract(new EventOverlayId("EVT_MATRIX"), kind,
                fixture.Shell.Id, fixture.Activity.Id,
                new[] { Assignment("MARKER_ACTIVITY_CUE", operation, "PAYLOAD_MATRIX") });
            Assert.That(Validate(fixture, contract).IsValid, Is.True, Join(Validate(fixture, contract)));
        }

        [TestCase(EventOverlayKind.Npc, EventMarkerOperation.SpawnReward)]
        [TestCase(EventOverlayKind.Reward, EventMarkerOperation.SpawnNpc)]
        [TestCase(EventOverlayKind.State, EventMarkerOperation.EnableMarker)]
        [TestCase(EventOverlayKind.Cosmetic, EventMarkerOperation.SetState)]
        [TestCase(EventOverlayKind.Empty, EventMarkerOperation.DisableMarker)]
        public void KindOperationCompatibilityMatrixRejectsForeignPairs(
            EventOverlayKind kind,
            EventMarkerOperation operation)
        {
            var fixture = CreateFixture();
            var contract = new EventOverlayContract(new EventOverlayId("EVT_BAD_MATRIX"), kind,
                fixture.Shell.Id, fixture.Activity.Id,
                new[] { Assignment("MARKER_ACTIVITY_CUE", operation, "PAYLOAD_MATRIX") });
            AssertError(Validate(fixture, contract), EventOverlayValidationErrorCode.InvalidMarkerOperation);
        }

        [Test]
        public void MarkerMustBeStableExistingAndUniqueWithStablePayload()
        {
            var fixture = CreateFixture();
            var contract = new EventOverlayContract(new EventOverlayId("EVT_BAD_MARKERS"), EventOverlayKind.Npc,
                fixture.Shell.Id, fixture.Activity.Id, new[]
                {
                    Assignment("MARKER_MISSING", EventMarkerOperation.SpawnNpc, "bad-payload"),
                    Assignment("MARKER_MISSING", EventMarkerOperation.SpawnNpc, "bad-payload"),
                    Assignment("bad_marker", EventMarkerOperation.SpawnNpc, "PAYLOAD_OK"),
                });
            var result = Validate(fixture, contract);
            AssertError(result, EventOverlayValidationErrorCode.InvalidMarker);
            AssertError(result, EventOverlayValidationErrorCode.InvalidMarkerOperation);
        }

        [Test]
        public void ClusterOnlyMarkerOverlayDoesNotRequireActivityOwnership()
        {
            var fixture = CreateFixture();
            var shellDigest = TerrainClusterContractValidator.Validate(fixture.Shell).CanonicalDigest;
            var evidence = new EventOverlayRemovalEvidence(shellDigest, shellDigest, new string('c', 64),
                new string('c', 64), AccessClass.MandatoryNoTool, AccessClass.MandatoryNoTool, string.Empty, string.Empty);
            var contract = new EventOverlayContract(new EventOverlayId("EVT_CLUSTER_STATE"), EventOverlayKind.State,
                fixture.Shell.Id, null,
                new[] { Assignment("MARKER_CLUSTER_STATE", EventMarkerOperation.SetState, "STATE_ACTIVE") });
            var result = EventOverlayValidator.Validate(contract, fixture.Shell, null,
                new[] { new EventMarkerId("MARKER_CLUSTER_STATE") }, evidence);
            Assert.That(result.IsValid, Is.True, Join(result));
        }

        [Test]
        public void ShellAndActivityReferencesMustResolveAndValidate()
        {
            var fixture = CreateFixture();
            var source = CreateOverlay(fixture);
            var wrongShell = new EventOverlayContract(source.Id, source.Kind, new TerrainClusterId("TC_OTHER"),
                source.ActivityStructureId, source.Assignments);
            AssertError(Validate(fixture, wrongShell), EventOverlayValidationErrorCode.InvalidShellReference);
            var missingActivity = new EventOverlayContract(source.Id, source.Kind, source.TerrainClusterId,
                new ActivityStructureId("ACT_MISSING"), source.Assignments);
            AssertError(Validate(fixture, missingActivity), EventOverlayValidationErrorCode.InvalidShellReference);
        }

        [Test]
        public void EventRemovalPreservesShellMandatoryPathAccessAndActivitySafety()
        {
            var fixture = CreateFixture();
            var source = fixture.Evidence;
            var broken = new EventOverlayRemovalEvidence(source.StaticShellDigestBeforeRemoval,
                new string('d', 64), source.MandatoryPathDigestBeforeRemoval, new string('e', 64),
                source.AccessClassBeforeRemoval, AccessClass.OptionalTool,
                source.ActivityRemovalSafetyDigestBeforeRemoval, new string('f', 64));
            AssertError(Validate(fixture, CreateOverlay(fixture), broken),
                EventOverlayValidationErrorCode.NonMarkerMutation);
        }

        [Test]
        public void ExplicitNonMarkerMutationDeclarationIsRejected()
        {
            var fixture = CreateFixture();
            var source = fixture.Evidence;
            var broken = new EventOverlayRemovalEvidence(source.StaticShellDigestBeforeRemoval,
                source.StaticShellDigestAfterRemoval, source.MandatoryPathDigestBeforeRemoval,
                source.MandatoryPathDigestAfterRemoval, source.AccessClassBeforeRemoval,
                source.AccessClassAfterRemoval, source.ActivityRemovalSafetyDigestBeforeRemoval,
                source.ActivityRemovalSafetyDigestAfterRemoval, true);
            AssertError(Validate(fixture, CreateOverlay(fixture), broken),
                EventOverlayValidationErrorCode.NonMarkerMutation);
        }

        [Test]
        public void RemovalEvidenceIsMandatory()
        {
            var fixture = CreateFixture();
            AssertError(EventOverlayValidator.Validate(CreateOverlay(fixture), fixture.Shell,
                fixture.Activity, fixture.Markers, null), EventOverlayValidationErrorCode.MissingInput);
        }

        [Test]
        public void AssignmentCollectionIsDefensiveReadOnlyAndCanonicalOrderIndependent()
        {
            var fixture = CreateFixture();
            var list = new List<EventMarkerAssignment>
            {
                Assignment("MARKER_ACTIVITY_TRIGGER", EventMarkerOperation.EnableMarker, "COSMETIC_TRIGGER"),
                Assignment("MARKER_ACTIVITY_CUE", EventMarkerOperation.EnableMarker, "COSMETIC_CUE"),
            };
            var source = new EventOverlayContract(new EventOverlayId("EVT_COSMETIC"), EventOverlayKind.Cosmetic,
                fixture.Shell.Id, fixture.Activity.Id, list);
            var digest = Validate(fixture, source).CanonicalDigest;
            list.Clear();
            Assert.That(source.Assignments, Has.Count.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => ((IList)source.Assignments).Clear());
            var reversed = new EventOverlayContract(source.Id, source.Kind, source.TerrainClusterId,
                source.ActivityStructureId, source.Assignments.Reverse());
            Assert.That(Validate(fixture, reversed).CanonicalDigest, Is.EqualTo(digest));
        }

        [Test]
        public void DisplayTextAndRemovalEvidenceDoNotAffectMarkerSemanticDigest()
        {
            var fixture = CreateFixture();
            var source = CreateOverlay(fixture);
            var baseline = Validate(fixture, source).CanonicalDigest;
            var display = new EventOverlayContract(source.Id, source.Kind, source.TerrainClusterId,
                source.ActivityStructureId, source.Assignments, "다른 표시");
            Assert.That(Validate(fixture, display).CanonicalDigest, Is.EqualTo(baseline));
            var evidence = new EventOverlayRemovalEvidence(fixture.Evidence.StaticShellDigestBeforeRemoval,
                fixture.Evidence.StaticShellDigestAfterRemoval, new string('9', 64), new string('9', 64),
                AccessClass.MandatoryNoTool, AccessClass.MandatoryNoTool,
                fixture.Evidence.ActivityRemovalSafetyDigestBeforeRemoval,
                fixture.Evidence.ActivityRemovalSafetyDigestAfterRemoval);
            Assert.That(Validate(fixture, source, evidence).CanonicalDigest, Is.EqualTo(baseline));
        }

        [Test]
        public void MarkerOperationPayloadAndReferenceSemanticsChangeDigest()
        {
            var fixture = CreateFixture();
            var source = CreateOverlay(fixture);
            var baseline = Validate(fixture, source).CanonicalDigest;
            var changed = new EventOverlayContract(new EventOverlayId("EVT_OTHER"), EventOverlayKind.Npc,
                fixture.Shell.Id, fixture.Activity.Id,
                new[] { Assignment("MARKER_ACTIVITY_TRIGGER", EventMarkerOperation.SpawnNpc, "NPC_OTHER") });
            Assert.That(Validate(fixture, changed).CanonicalDigest, Is.Not.EqualTo(baseline));
        }

        [Test]
        public void InvalidInputAccumulatesStableErrorsAndPublishesNothing()
        {
            var fixture = CreateFixture();
            var broken = new EventOverlayContract(new EventOverlayId("bad"), (EventOverlayKind)999,
                new TerrainClusterId("TC_BAD"), fixture.Activity.Id,
                new[] { Assignment("bad", (EventMarkerOperation)999, "bad") });
            var first = Validate(fixture, broken);
            var second = Validate(fixture, broken);
            Assert.That(first.IsValid, Is.False);
            Assert.That(first.Contract, Is.Null);
            Assert.That(first.CanonicalDigest, Is.Empty);
            Assert.That(first.Errors, Is.Ordered);
            Assert.That(first.Errors.Select(value => value.ToString()), Is.EqualTo(second.Errors.Select(value => value.ToString())));
        }

        [Test]
        public void EventContractOwnsMarkerAssignmentsOnlyAndIsImmutable()
        {
            Assert.That(typeof(EventOverlayContract).IsSealed, Is.True);
            Assert.That(typeof(EventMarkerAssignment).IsSealed, Is.True);
            Assert.That(typeof(EventOverlayContract).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(value => value.SetMethod != null), Is.Empty);
            var names = typeof(EventOverlayContract).GetProperties().Select(value => value.Name).ToArray();
            Assert.That(names, Is.EqualTo(new[]
                { "Id", "Kind", "TerrainClusterId", "ActivityStructureId", "Assignments", "DisplayText" }));
            Assert.That(names, Has.None.Contains("Graph"));
            Assert.That(names, Has.None.Contains("Collision"));
            Assert.That(names, Has.None.Contains("Route"));
            Assert.That(names, Has.None.Contains("Access"));
            Assert.That(names, Has.None.Contains("Pacing"));
            Assert.That(names, Has.None.Contains("Envelope"));
        }

        [Test]
        public void ProductionScopeContainsNoGraphsTileWritesRngFileOrUnityLifecycle()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath,
                "_Game/Map/Runtime/WorldGeneration/EventOverlays"));
            var source = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal).Select(File.ReadAllText));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine", "System.IO", "System.Random", "Random.", "MonoBehaviour",
                         "MechanismGraph", "ProgressionGraph", "TraversalMovementKind", "StageMapGenerator",
                         "GridWorld", "RoomTemplate", "RoomGridTransform", "TileMutationService", "SectorRecipeResolver",
                     })
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
        }

        private static Fixture CreateFixture()
        {
            var shell = ActivityEventFixture.CreateShell();
            var activity = ActivityEventFixture.CreateActivity(shell);
            return new Fixture(shell, activity,
                activity.Slots.Select(value => new EventMarkerId(value.MarkerId)).ToArray(),
                ActivityEventFixture.CreateEventEvidence(shell, activity));
        }

        private static EventOverlayContract CreateOverlay(Fixture fixture)
            => new EventOverlayContract(new EventOverlayId("EVT_LIVE_BASELINE"), EventOverlayKind.Npc,
                fixture.Shell.Id, fixture.Activity.Id,
                new[] { Assignment("MARKER_ACTIVITY_CUE", EventMarkerOperation.SpawnNpc, "NPC_MOON_GUIDE") },
                "Fixture event");

        private static EventMarkerAssignment Assignment(string marker, EventMarkerOperation operation, string payload)
            => new EventMarkerAssignment(new EventMarkerId(marker), operation, payload);

        private static EventOverlayValidationResult Validate(
            Fixture fixture,
            EventOverlayContract contract,
            EventOverlayRemovalEvidence evidence = null)
            => EventOverlayValidator.Validate(contract, fixture.Shell, fixture.Activity,
                fixture.Markers, evidence ?? fixture.Evidence);

        private static void AssertError(EventOverlayValidationResult result, EventOverlayValidationErrorCode code)
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), Join(result));
        }

        private static string Join(EventOverlayValidationResult result)
            => string.Join("\n", result.Errors.Select(value => value.ToString()));

        private sealed class Fixture
        {
            public Fixture(TerrainClusterContract shell, ActivityStructureContract activity,
                IReadOnlyList<EventMarkerId> markers, EventOverlayRemovalEvidence evidence)
            {
                Shell = shell;
                Activity = activity;
                Markers = markers;
                Evidence = evidence;
            }
            public TerrainClusterContract Shell { get; }
            public ActivityStructureContract Activity { get; }
            public IReadOnlyList<EventMarkerId> Markers { get; }
            public EventOverlayRemovalEvidence Evidence { get; }
        }
    }
}
