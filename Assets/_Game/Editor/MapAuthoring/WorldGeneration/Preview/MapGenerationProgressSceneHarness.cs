using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor.WorldGeneration.Preview
{
    [CustomEditor(typeof(MapGenerationProgressSceneAdapter))]
    internal sealed class MapGenerationProgressSceneHarness : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var adapter = (MapGenerationProgressSceneAdapter)target;
            adapter.SeedText = EditorGUILayout.TextField("Seed", adapter.SeedText);
            adapter.AttemptOrdinal = EditorGUILayout.IntSlider("Attempt", adapter.AttemptOrdinal, 0, 99);
            EditorGUILayout.LabelField("Selected", adapter.SelectedTab.ToString());
            EditorGUILayout.HelpBox(adapter.Status, MessageType.Info);
            if (GUILayout.Button("Load Known Viable")) Act(adapter, () => LoadKnownViable(adapter));
            if (GUILayout.Button("Run Selected Single Attempt")) Act(adapter, () => RunSelected(adapter));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show Topology")) Act(adapter, adapter.ShowTopology);
            if (GUILayout.Button("Show Sites")) Act(adapter, adapter.ShowSites);
            if (GUILayout.Button("Show Biomes")) Act(adapter, adapter.ShowBiomes);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Clear")) Act(adapter, adapter.Clear);
        }

        internal static void LoadKnownViable(MapGenerationProgressSceneAdapter adapter)
        {
            adapter.SeedText = "0x0123456789ABCDF9";
            adapter.AttemptOrdinal = 24;
            RunSelected(adapter);
            if (adapter.BiomeOverlay.HasSnapshot) adapter.ShowBiomes();
        }

        internal static void RunSelected(MapGenerationProgressSceneAdapter adapter)
        {
            adapter.Clear();
            if (!TryParseSeed(adapter.SeedText, out var seed))
            {
                adapter.PublishStatus("Invalid seed. Use decimal or 0x-prefixed hexadecimal.", true);
                return;
            }

            try
            {
                var testType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(value => value.GetType(
                        "StarNight.Map.Tests.WorldGeneration.Generation.Map04ExitTests", false))
                    .FirstOrDefault(value => value != null);
                if (testType == null)
                    throw new InvalidOperationException("Game.Map.Tests.EditMode is not loaded.");

                var fixture = InvokeStatic(testType, "BuildFixture");
                var source = InvokeStatic(testType, "BuildSourceSnapshot", seed);
                var world = InvokeStatic(testType, "CreateSourceWorld", seed);
                var servicesType = testType.GetNestedType("PipelineServices", BindingFlags.NonPublic);
                var services = Activator.CreateInstance(servicesType, true);
                var record = InvokeStatic(
                    testType, "RunAttempt", seed, adapter.AttemptOrdinal, source, world,
                    services, fixture, false, false);
                var recordType = record.GetType();
                if (!(bool)Get(recordType, record, "Completed"))
                {
                    adapter.PublishStatus(string.Format(
                        CultureInfo.InvariantCulture, "Retry required: {0} / {1} / RNG {2}",
                        Get(recordType, record, "TerminalStage"), Get(recordType, record, "Reason"),
                        Get(recordType, record, "RngDrawCount")), true);
                    return;
                }

                var validation = (BiomePatchValidationResult)Get(recordType, record, "Validation");
                adapter.TopologyOverlay.SetSnapshot(new GridInitializationPass().Execute(seed));
                InjectSiteSnapshot(adapter.SiteOverlay, (SiteReservationSnapshot)source);
                adapter.BiomeOverlay.SetSnapshot(validation.Publication);
                adapter.PublishStatus(string.Format(
                    CultureInfo.InvariantCulture,
                    "Completed seed {0}, attempt {1}: {2} patches ({3}/{4}/{5}), {6}/{7} sectors, rules {8}/15, RNG {9}",
                    seed, adapter.AttemptOrdinal, Get(recordType, record, "PatchCount"),
                    Get(recordType, record, "CoreCount"), Get(recordType, record, "SatelliteCount"),
                    Get(recordType, record, "IntrusionCount"), Get(recordType, record, "AssignedCount"),
                    Get(recordType, record, "UnassignedCount"), Get(recordType, record, "RuleCount"),
                    Get(recordType, record, "RngDrawCount")), true);
            }
            catch (Exception exception)
            {
                adapter.PublishStatus(
                    "Fixture unavailable: " + (exception.InnerException ?? exception).Message, true);
            }
        }

        private static void Act(MapGenerationProgressSceneAdapter adapter, Action action)
        {
            action();
            ClearSceneDirtiness(adapter.gameObject.scene);
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static bool TryParseSeed(string text, out ulong seed)
        {
            var value = (text ?? string.Empty).Trim();
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ulong.TryParse(value.Substring(2), NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out seed);
            return ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out seed);
        }

        private static object InvokeStatic(Type type, string name, params object[] arguments)
        {
            var method = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(value => value.Name == name && value.GetParameters().Length == arguments.Length);
            return method.Invoke(null, arguments);
        }

        private static object Get(Type type, object target, string name) =>
            type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(target, null);

        private static void InjectSiteSnapshot(
            SiteReservationOverlay overlay,
            SiteReservationSnapshot source)
        {
            var entries = new List<SiteEntrySide>[source.Sectors.Count];
            for (var index = 0; index < entries.Length; index++) entries[index] = new List<SiteEntrySide>();
            foreach (var entry in source.EntryAnchors)
                entries[WorldGridIndex.ToIndex(entry.FootprintSector)].Add(entry.Side);

            var witnesses = new SiteReservationId?[source.Sectors.Count];
            var used = new HashSet<int>();
            foreach (var seed in source.CoreBiomeSeeds)
            {
                var owned = 0;
                var origin = WorldGridIndex.ToIndex(seed.SeedSector);
                for (var offset = 0; offset < source.Sectors.Count && owned < 5; offset++)
                {
                    var index = (origin + offset) % source.Sectors.Count;
                    if (!source.GetSector(index).IsReserved && used.Add(index))
                    {
                        witnesses[index] = seed.SourceReservationId;
                        owned++;
                    }
                }
            }

            var cellConstructor = typeof(SiteReservationOverlayCell).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var cells = new List<SiteReservationOverlayCell>(source.Sectors.Count);
            for (var index = 0; index < source.Sectors.Count; index++)
            {
                var sector = source.GetSector(index);
                SiteReservation reservation = null;
                if (sector.ReservationId.HasValue)
                    source.TryGetReservation(sector.ReservationId.Value, out reservation);
                cells.Add((SiteReservationOverlayCell)cellConstructor.Invoke(
                    new object[] { sector, reservation, entries[index], witnesses[index] }));
            }

            var rowConstructor = typeof(SiteReservationOverlayDiagnosticRow).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var rows = new List<SiteReservationOverlayDiagnosticRow>(16);
            for (var index = 0; index < 16; index++)
            {
                var diagnosticClass = index < 12
                    ? SiteReservationOverlayDiagnosticClass.CandidateRejection
                    : index < 14
                        ? SiteReservationOverlayDiagnosticClass.FinalGate
                        : SiteReservationOverlayDiagnosticClass.SoftCost;
                rows.Add((SiteReservationOverlayDiagnosticRow)rowConstructor.Invoke(new object[]
                {
                    (SiteReservationOverlayDiagnosticKind)index, diagnosticClass,
                    "MANUAL_" + index.ToString("D2", CultureInfo.InvariantCulture),
                    "Manual fixture diagnostic", 0L
                }));
            }

            var snapshotConstructor = typeof(SiteReservationOverlaySnapshot).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var snapshot = (SiteReservationOverlaySnapshot)snapshotConstructor.Invoke(new object[]
            {
                source.Seed, new ReadOnlyCollection<SiteReservationOverlayCell>(cells),
                new ReadOnlyCollection<SiteReservationOverlayDiagnosticRow>(rows),
                source.Reservations.Count, source.Sectors.Count(value => value.IsReserved),
                source.EntryAnchors.Count, source.CoreBiomeSeeds.Count, 20, 6
            });
            typeof(SiteReservationOverlay).GetField(
                "snapshot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(overlay, snapshot);
        }

        private static void ClearSceneDirtiness(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid()) return;
            var clear = typeof(EditorSceneManager).GetMethod(
                "ClearSceneDirtiness", BindingFlags.Static | BindingFlags.NonPublic);
            if (clear != null) clear.Invoke(null, new object[] { scene });
        }
    }
}
