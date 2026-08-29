using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests.PlayMode.WorldGeneration.Activities
{
    [TestFixture]
    [Category("MAP12_06")]
    public sealed class ActivityRemovalGrayboxPlayModeTests
    {
        private const int ShellWitnessCount = 4;

        [Test]
        public void RepresentativeDefinitions_AreExactImmutableGoldenValues()
        {
            IReadOnlyList<ActivityRemovalFixtureSnapshot> fixtures = CreateFixtures();

            CollectionAssert.AreEqual(
                new[]
                {
                    "ACT_CRATER_RICOCHET_MINE+EVT_METEOR_FALL",
                    "ACT_MILL_ESCORT_CART+EVT_WANDERING_MERCHANT",
                    "ACT_MILL_ESCORT_CART+EVT_RARE_CREATURE",
                    "ACT_MARU_REWIND_ANOMALY+EVT_MARU_INTERVENTION",
                    "ACT_DOUGH_TIME_TRIAL+EVT_EMPTY",
                },
                fixtures.Select(fixture => fixture.PairKey).ToArray());

            CollectionAssert.AreEqual(new[] { 8, 8, 8, 7, 7 }, fixtures.Select(fixture => fixture.ActivityMarkerCount));
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1, 0 }, fixtures.Select(fixture => fixture.EventMarkerCount));
            Assert.That(fixtures.All(fixture => fixture.SafetyWitnesses.Count == 2), Is.True);
            Assert.That(fixtures.All(fixture => fixture.DangerCoordinates.Count >= 1), Is.True);

            foreach (ActivityRemovalFixtureSnapshot fixture in fixtures)
            {
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<string>)fixture.ActivityMarkerTokens).Add("MUTATION"));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<LocalTileCoord>)fixture.DangerCoordinates).Add(new LocalTileCoord(0, 0)));
            }
        }

        [UnityTest]
        public IEnumerator ExactFiveFixtures_RunLifecycleAndTearDownWithoutLeaks()
        {
            foreach (ActivityRemovalFixtureSnapshot fixture in CreateFixtures())
            {
                int rootsBefore = ActivityRemovalGrayboxHarness.CountFixtureRoots();
                ActivityRemovalGrayboxHarness harness = new ActivityRemovalGrayboxHarness(fixture);
                harness.CreateStatic();

                AssertLifecycle(harness, "Static", ShellWitnessCount, 0, 0, 0);

                harness.EnterCue();
                AssertLifecycle(harness, "Cue", ShellWitnessCount, 1, 0, 0);
                Assert.That(harness.IsActivityMarkerActive("C"), Is.True, fixture.PairKey);
                Assert.That(harness.ActiveCoreMarkerCount, Is.Zero, fixture.PairKey);

                harness.EnterActive();
                AssertLifecycle(
                    harness,
                    "Active",
                    ShellWitnessCount,
                    fixture.ActivityMarkerCount,
                    fixture.EventMarkerCount,
                    0);

                harness.Interrupt();
                AssertLifecycle(
                    harness,
                    "Interrupted",
                    ShellWitnessCount,
                    fixture.PreservedActivityMarkerCount,
                    0,
                    0);
                Assert.That(harness.HasRequiredShellWitnesses, Is.True, fixture.PairKey);
                Assert.That(harness.HasWorstPositionSafetyWitnesses, Is.True, fixture.PairKey);

                harness.Reenter();
                AssertLifecycle(
                    harness,
                    "Reentered",
                    ShellWitnessCount,
                    fixture.ActivityMarkerCount,
                    fixture.EventMarkerCount,
                    0);

                harness.Remove();
                AssertLifecycle(harness, "Removed", ShellWitnessCount, 0, 0, 0);
                Assert.That(harness.HasRequiredShellWitnesses, Is.True, fixture.PairKey);
                Assert.That(harness.HasWorstPositionSafetyWitnesses, Is.True, fixture.PairKey);

                TestContext.WriteLine(
                    "{0}: Static=4/0/0 Cue=4/1/0 Active=4/{1}/{2} Interrupted=4/{3}/0 " +
                    "Reentered=4/{1}/{2} Removed=4/0/0 duplicate=0",
                    fixture.PairKey,
                    fixture.ActivityMarkerCount,
                    fixture.EventMarkerCount,
                    fixture.PreservedActivityMarkerCount);

                harness.ScheduleDestroy();
                yield return null;

                Assert.That(harness.RootWasDestroyed, Is.True, fixture.PairKey);
                Assert.That(ActivityRemovalGrayboxHarness.CountFixtureRoots(), Is.EqualTo(rootsBefore), fixture.PairKey);
            }
        }

        private static void AssertLifecycle(
            ActivityRemovalGrayboxHarness harness,
            string state,
            int expectedWitnesses,
            int expectedActivity,
            int expectedEvent,
            int expectedDuplicates)
        {
            Assert.That(harness.ActiveShellWitnessCount, Is.EqualTo(expectedWitnesses), state);
            Assert.That(harness.ActiveActivityMarkerCount, Is.EqualTo(expectedActivity), state);
            Assert.That(harness.ActiveEventMarkerCount, Is.EqualTo(expectedEvent), state);
            Assert.That(harness.DuplicateMarkerCount, Is.EqualTo(expectedDuplicates), state);
        }

        private static IReadOnlyList<ActivityRemovalFixtureSnapshot> CreateFixtures()
        {
            return Array.AsReadOnly(new[]
            {
                new ActivityRemovalFixtureSnapshot(
                    new ActivityStructureId("ACT_CRATER_RICOCHET_MINE"),
                    new EventOverlayId("EVT_METEOR_FALL"),
                    new[] { "C", "T", "D", "H", "P", "RW", "RC", "RS" },
                    new[] { "C", "T", "RW", "RC", "RS" },
                    new[] { new LocalTileCoord(15, 1), new LocalTileCoord(16, 1) },
                    new[] { new LocalTileCoord(4, 1), new LocalTileCoord(42, 1) },
                    1),
                new ActivityRemovalFixtureSnapshot(
                    new ActivityStructureId("ACT_MILL_ESCORT_CART"),
                    new EventOverlayId("EVT_WANDERING_MERCHANT"),
                    new[] { "C", "T", "D", "H", "N", "RW", "RC", "RS" },
                    new[] { "C", "T", "RW", "RC", "RS" },
                    new[] { new LocalTileCoord(15, 1), new LocalTileCoord(16, 1) },
                    new[] { new LocalTileCoord(4, 1), new LocalTileCoord(30, 1) },
                    1),
                new ActivityRemovalFixtureSnapshot(
                    new ActivityStructureId("ACT_MILL_ESCORT_CART"),
                    new EventOverlayId("EVT_RARE_CREATURE"),
                    new[] { "C", "T", "D", "H", "N", "RW", "RC", "RS" },
                    new[] { "C", "T", "RW", "RC", "RS" },
                    new[] { new LocalTileCoord(15, 1), new LocalTileCoord(16, 1) },
                    new[] { new LocalTileCoord(4, 1), new LocalTileCoord(30, 1) },
                    1),
                new ActivityRemovalFixtureSnapshot(
                    new ActivityStructureId("ACT_MARU_REWIND_ANOMALY"),
                    new EventOverlayId("EVT_MARU_INTERVENTION"),
                    new[] { "C", "T", "D", "H", "RW", "RC", "RS" },
                    new[] { "C", "T", "RW", "RC", "RS" },
                    new[] { new LocalTileCoord(15, 9) },
                    new[] { new LocalTileCoord(4, 1), new LocalTileCoord(30, 17) },
                    1),
                new ActivityRemovalFixtureSnapshot(
                    new ActivityStructureId("ACT_DOUGH_TIME_TRIAL"),
                    new EventOverlayId("EVT_EMPTY"),
                    new[] { "C", "T", "D", "H", "RW", "RC", "RS" },
                    new[] { "C", "T", "RW", "RC", "RS" },
                    new[] { new LocalTileCoord(15, 1) },
                    new[] { new LocalTileCoord(4, 1), new LocalTileCoord(30, 1) },
                    0),
            });
        }
    }

    internal sealed class ActivityRemovalFixtureSnapshot
    {
        public ActivityRemovalFixtureSnapshot(
            ActivityStructureId activityId,
            EventOverlayId eventId,
            IEnumerable<string> activityMarkerTokens,
            IEnumerable<string> preservedActivityMarkerTokens,
            IEnumerable<LocalTileCoord> dangerCoordinates,
            IEnumerable<LocalTileCoord> safetyWitnesses,
            int eventMarkerCount)
        {
            ActivityId = activityId;
            EventId = eventId;
            ActivityMarkerTokens = Array.AsReadOnly(activityMarkerTokens.ToArray());
            PreservedActivityMarkerTokens = Array.AsReadOnly(preservedActivityMarkerTokens.ToArray());
            DangerCoordinates = Array.AsReadOnly(dangerCoordinates.ToArray());
            SafetyWitnesses = Array.AsReadOnly(safetyWitnesses.ToArray());
            EventMarkerCount = eventMarkerCount;
        }

        public ActivityStructureId ActivityId { get; }
        public EventOverlayId EventId { get; }
        public ReadOnlyCollection<string> ActivityMarkerTokens { get; }
        public ReadOnlyCollection<string> PreservedActivityMarkerTokens { get; }
        public ReadOnlyCollection<LocalTileCoord> DangerCoordinates { get; }
        public ReadOnlyCollection<LocalTileCoord> SafetyWitnesses { get; }
        public int EventMarkerCount { get; }
        public int ActivityMarkerCount => ActivityMarkerTokens.Count;
        public int PreservedActivityMarkerCount => PreservedActivityMarkerTokens.Count;
        public string PairKey => ActivityId.Value + "+" + EventId.Value;
    }

    internal sealed class ActivityRemovalGrayboxHarness
    {
        private const string RootPrefix = "MAP12_06_FIXTURE_";
        private static readonly string[] ShellWitnessNames =
        {
            "WITNESS_EN",
            "WITNESS_EX",
            "WITNESS_SP",
            "WITNESS_RC",
        };

        private readonly ActivityRemovalFixtureSnapshot fixture;
        private readonly Dictionary<string, GameObject> activityMarkers = new Dictionary<string, GameObject>();
        private readonly List<GameObject> eventMarkers = new List<GameObject>();
        private GameObject root;

        public ActivityRemovalGrayboxHarness(ActivityRemovalFixtureSnapshot fixture)
        {
            this.fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }

        public int ActiveShellWitnessCount => CountActiveChildren("WITNESS_");
        public int ActiveActivityMarkerCount => activityMarkers.Values.Count(marker => marker != null && marker.activeSelf);
        public int ActiveEventMarkerCount => eventMarkers.Count(marker => marker != null && marker.activeSelf);
        public int ActiveCoreMarkerCount => activityMarkers.Count(pair =>
            IsCoreToken(pair.Key) && pair.Value != null && pair.Value.activeSelf);

        public int DuplicateMarkerCount
        {
            get
            {
                if (root == null)
                {
                    return 0;
                }

                int duplicateCount = 0;
                Dictionary<string, int> names = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int index = 0; index < root.transform.childCount; index++)
                {
                    string childName = root.transform.GetChild(index).name;
                    names.TryGetValue(childName, out int count);
                    names[childName] = count + 1;
                }

                foreach (int count in names.Values)
                {
                    duplicateCount += Math.Max(0, count - 1);
                }

                return duplicateCount;
            }
        }

        public bool HasRequiredShellWitnesses =>
            ShellWitnessNames.All(name => FindDirectChild(name) != null) &&
            FindDirectChild("WITNESS_EX").activeSelf;

        public bool HasWorstPositionSafetyWitnesses
        {
            get
            {
                foreach (LocalTileCoord danger in fixture.DangerCoordinates)
                {
                    LocalTileCoord nearest = fixture.SafetyWitnesses
                        .OrderBy(safety => ManhattanDistance(danger, safety))
                        .ThenBy(safety => safety.X)
                        .ThenBy(safety => safety.Y)
                        .First();
                    string witnessName = nearest == fixture.SafetyWitnesses[0] ? "WITNESS_SP" : "WITNESS_RC";
                    GameObject witness = FindDirectChild(witnessName);
                    if (witness == null || !witness.activeSelf)
                    {
                        return false;
                    }
                }

                GameObject exit = FindDirectChild("WITNESS_EX");
                return exit != null && exit.activeSelf;
            }
        }

        public bool RootWasDestroyed => root == null;

        public static int CountFixtureRoots()
        {
            return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(transform => transform.parent == null && transform.name.StartsWith(RootPrefix, StringComparison.Ordinal));
        }

        public void CreateStatic()
        {
            root = new GameObject(RootPrefix + fixture.ActivityId.Value + "_" + fixture.EventId.Value);
            foreach (string witnessName in ShellWitnessNames)
            {
                CreateChild(witnessName, true);
            }
        }

        public void EnterCue()
        {
            foreach (string token in fixture.ActivityMarkerTokens)
            {
                activityMarkers[token] = CreateChild("MARKER_ACTIVITY_" + token, token == "C");
            }
        }

        public void EnterActive()
        {
            foreach (GameObject marker in activityMarkers.Values)
            {
                marker.SetActive(true);
            }

            EnsureEventMarkers();
        }

        public void Interrupt()
        {
            foreach (string token in fixture.ActivityMarkerTokens.Except(fixture.PreservedActivityMarkerTokens).ToArray())
            {
                DestroyActivityMarker(token);
            }

            DestroyEventMarkers();
        }

        public void Reenter()
        {
            foreach (string token in fixture.ActivityMarkerTokens)
            {
                if (!activityMarkers.TryGetValue(token, out GameObject marker) || marker == null)
                {
                    activityMarkers[token] = CreateChild("MARKER_ACTIVITY_" + token, true);
                }
                else
                {
                    marker.SetActive(true);
                }
            }

            EnsureEventMarkers();
        }

        public void Remove()
        {
            foreach (string token in activityMarkers.Keys.ToArray())
            {
                DestroyActivityMarker(token);
            }

            DestroyEventMarkers();
        }

        public bool IsActivityMarkerActive(string token)
        {
            return activityMarkers.TryGetValue(token, out GameObject marker) && marker != null && marker.activeSelf;
        }

        public void ScheduleDestroy()
        {
            UnityEngine.Object.Destroy(root);
        }

        private static bool IsCoreToken(string token)
        {
            return token == "D" || token == "H" || token == "P" || token == "N";
        }

        private static int ManhattanDistance(LocalTileCoord left, LocalTileCoord right)
        {
            return Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
        }

        private GameObject CreateChild(string childName, bool active)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(root.transform, false);
            child.SetActive(active);
            return child;
        }

        private int CountActiveChildren(string prefix)
        {
            int count = 0;
            for (int index = 0; index < root.transform.childCount; index++)
            {
                Transform child = root.transform.GetChild(index);
                if (child.gameObject.activeSelf && child.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private GameObject FindDirectChild(string childName)
        {
            if (root == null)
            {
                return null;
            }

            Transform child = root.transform.Find(childName);
            return child == null ? null : child.gameObject;
        }

        private void EnsureEventMarkers()
        {
            if (fixture.EventMarkerCount == 0 || eventMarkers.Any(marker => marker != null))
            {
                return;
            }

            for (int index = 0; index < fixture.EventMarkerCount; index++)
            {
                eventMarkers.Add(CreateChild("MARKER_EVENT_" + index, true));
            }
        }

        private void DestroyActivityMarker(string token)
        {
            if (activityMarkers.TryGetValue(token, out GameObject marker) && marker != null)
            {
                UnityEngine.Object.DestroyImmediate(marker);
            }

            activityMarkers.Remove(token);
        }

        private void DestroyEventMarkers()
        {
            foreach (GameObject marker in eventMarkers)
            {
                if (marker != null)
                {
                    UnityEngine.Object.DestroyImmediate(marker);
                }
            }

            eventMarkers.Clear();
        }
    }
}
