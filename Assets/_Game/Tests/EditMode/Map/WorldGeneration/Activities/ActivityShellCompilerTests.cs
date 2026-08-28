using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.TerrainClusters.Authoring;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Activities
{
    [TestFixture]
    [Category("MAP12_01")]
    public sealed class ActivityShellCompilerTests
    {
        private const string RepresentativeClusterId = "TC_CRATER_BOWL_ASCENT";
        private const string ApprovedCatalogDigest =
            "9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7";

        private static readonly Lazy<ActivityShellFixture> Physical =
            new Lazy<ActivityShellFixture>(ActivityShellFixture.Build);

        [Test]
        public void RealTerrainClusterChainCompilesFourZonesAndAllSlotBindingsWithoutMutation()
        {
            var fixture = Physical.Value;
            var before = fixture.CaptureUpstream();
            var result = ActivityShellCompiler.Compile(fixture.Request());

            Assert.That(result.IsSuccess, Is.True, Join(result));
            Assert.That(result.Zones.Count, Is.EqualTo(4));
            Assert.That(result.ZoneCells.Count, Is.EqualTo(10));
            Assert.That(result.ZoneCells.Select(value => value.CompiledCoordinate).Distinct().Count(), Is.EqualTo(9));
            Assert.That(result.Slots.Count, Is.EqualTo(9));
            Assert.That(result.CueBindings.Count, Is.EqualTo(1));
            Assert.That(result.MechanismBindings.Count, Is.EqualTo(8));
            Assert.That(result.ProgressionBindings.Count, Is.EqualTo(7));
            Assert.That(result.ZoneCells.Count(value => value.IsAbsoluteProtected), Is.GreaterThan(0));
            Assert.That(result.Slots.Count(value => value.IsAbsoluteProtected), Is.GreaterThan(0));
            Assert.That(result.Canvas.GeometryWriteCount, Is.Zero);
            Assert.That(result.Canvas.GeometryChangeCount, Is.Zero);
            Assert.That(result.Canvas.RendererInvocationCount, Is.Zero);
            Assert.That(result.Canvas.RngDrawCount, Is.Zero);
            Assert.That(fixture.CaptureUpstream(), Is.EqualTo(before));

            foreach (var cell in result.ZoneCells)
            {
                LocalTileCoord source;
                Assert.That(fixture.LocalCanvas.TryGetSourceTile(cell.CompiledCoordinate, out source), Is.True);
                Assert.That(source, Is.EqualTo(cell.SourceCoordinate));
            }

            TestContext.WriteLine(
                "ACTIVITY_SHELL cluster=" + fixture.Entry.Id.Value +
                " variant=" + fixture.Entry.BaselineVariantId.Value +
                " zones=" + result.Zones.Count +
                " zone_cells=" + result.ZoneCells.Count +
                " unique_cells=" + result.ZoneCells.Select(value => value.CompiledCoordinate).Distinct().Count() +
                " slots=" + result.Slots.Count +
                " cue_bindings=" + result.CueBindings.Count +
                " mechanism_bindings=" + result.MechanismBindings.Count +
                " progression_bindings=" + result.ProgressionBindings.Count +
                " solid=" + result.ZoneCells.Count(value => value.Occupancy == TerrainClusterShellOccupancy.Solid) +
                " air=" + result.ZoneCells.Count(value => value.Occupancy == TerrainClusterShellOccupancy.Air) +
                " protected=" + result.ZoneCells.Count(value => value.IsAbsoluteProtected) +
                " digest=" + result.CanonicalDigest);
        }

        [Test]
        public void ReverseRepeatAndTurkishCultureProduceTheSameCanonicalArtifact()
        {
            var fixture = Physical.Value;
            var canonical = ActivityShellCompiler.Compile(fixture.Request());
            var repeated = ActivityShellCompiler.Compile(fixture.Request());
            Assert.That(canonical.IsSuccess, Is.True, Join(canonical));
            Assert.That(repeated.IsSuccess, Is.True, Join(repeated));

            var reversedActivity = ActivityShellFixture.CloneActivity(
                fixture.Activity,
                slots: fixture.Activity.Slots.Reverse(),
                cues: fixture.Activity.Cues.Reverse(),
                mechanism: new MechanismGraph(
                    fixture.Activity.MechanismGraph.Nodes.Reverse(),
                    fixture.Activity.MechanismGraph.Edges.Reverse()),
                progression: new ProgressionGraph(
                    fixture.Activity.ProgressionGraph.StartNodeId,
                    fixture.Activity.ProgressionGraph.TerminalNodeId,
                    fixture.Activity.ProgressionGraph.Nodes.Reverse(),
                    fixture.Activity.ProgressionGraph.Edges.Reverse()));
            var reversedZones = fixture.Zones.Reverse().Select(value =>
                new ActivityShellZoneDefinition(value.Kind, value.SourceCoordinates.Reverse())).ToArray();
            var reversed = ActivityShellCompiler.Compile(fixture.Request(
                activity: reversedActivity,
                zones: reversedZones,
                intents: fixture.Intents.Reverse()));
            Assert.That(reversed.IsSuccess, Is.True, Join(reversed));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            ActivityShellCompileResult turkish;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                turkish = ActivityShellCompiler.Compile(fixture.Request());
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            Assert.That(turkish.IsSuccess, Is.True, Join(turkish));
            Assert.That(repeated.CanonicalDigest, Is.EqualTo(canonical.CanonicalDigest));
            Assert.That(reversed.CanonicalDigest, Is.EqualTo(canonical.CanonicalDigest));
            Assert.That(turkish.CanonicalDigest, Is.EqualTo(canonical.CanonicalDigest));
        }

        [Test]
        public void EverySlotKindMapsToTheExactSemanticAndRequiredZone()
        {
            var result = ActivityShellCompiler.Compile(Physical.Value.Request());
            Assert.That(result.IsSuccess, Is.True, Join(result));
            var expected = new Dictionary<ActivitySlotKind, Tuple<ActivitySlotSemanticKind, ActivityShellZoneKind>>
            {
                { ActivitySlotKind.Cue, Tuple.Create(ActivitySlotSemanticKind.CueMarker, ActivityShellZoneKind.Cue) },
                { ActivitySlotKind.Trigger, Tuple.Create(ActivitySlotSemanticKind.PressurePlateTrigger, ActivityShellZoneKind.Core) },
                { ActivitySlotKind.Device, Tuple.Create(ActivitySlotSemanticKind.DeviceAnchor, ActivityShellZoneKind.Core) },
                { ActivitySlotKind.Hazard, Tuple.Create(ActivitySlotSemanticKind.ChaseOrHazardSpawn, ActivityShellZoneKind.Core) },
                { ActivitySlotKind.Projectile, Tuple.Create(ActivitySlotSemanticKind.ProjectileEmitter, ActivityShellZoneKind.Core) },
                { ActivitySlotKind.Reward, Tuple.Create(ActivitySlotSemanticKind.RewardAnchor, ActivityShellZoneKind.Reward) },
                { ActivitySlotKind.Recovery, Tuple.Create(ActivitySlotSemanticKind.RecoveryAnchor, ActivityShellZoneKind.Recovery) },
                { ActivitySlotKind.Reset, Tuple.Create(ActivitySlotSemanticKind.ResetAnchor, ActivityShellZoneKind.Recovery) },
                { ActivitySlotKind.Npc, Tuple.Create(ActivitySlotSemanticKind.NpcAnchor, ActivityShellZoneKind.Core) },
            };

            foreach (var slot in result.Slots)
            {
                Assert.That(slot.Semantic, Is.EqualTo(expected[slot.SlotKind].Item1), slot.SlotId.Value);
                Assert.That(slot.RequiredZone, Is.EqualTo(expected[slot.SlotKind].Item2), slot.SlotId.Value);
                Assert.That(result.Canvas.GetZoneCells(slot.RequiredZone)
                    .Any(value => value.SourceCoordinate == slot.SourceCoordinate), Is.True, slot.SlotId.Value);
            }
            Assert.That(result.ProgressionBindings.Single(value => value.Phase == ProgressionPhaseKind.Activation)
                .SlotId.Value, Is.EqualTo("SLOT_TRIGGER"));
            Assert.That(result.ProgressionBindings.Single(value => value.Phase == ProgressionPhaseKind.Reset)
                .SlotId.Value, Is.EqualTo("SLOT_RESET"));
            Assert.That(result.ProgressionBindings.Single(value => value.Phase == ProgressionPhaseKind.Exit)
                .TraversalNodeId, Is.EqualTo(Physical.Value.RouteWitness.BaselineRoute.ExitNodeId));
        }

        [Test]
        public void MissingDuplicateUnknownAndMismatchedIntentsFailAtomically()
        {
            var fixture = Physical.Value;
            var missing = ActivityShellCompiler.Compile(fixture.Request(
                intents: fixture.Intents.Where(value => value.SlotId.Value != "SLOT_NPC")));
            AssertAtomicFailure(missing, ActivityShellCompileErrorCode.MissingSlotIntent);

            var duplicate = ActivityShellCompiler.Compile(fixture.Request(
                intents: fixture.Intents.Concat(new[] { fixture.Intents[0] })));
            AssertAtomicFailure(duplicate, ActivityShellCompileErrorCode.DuplicateSlotIntent);

            var unknown = ActivityShellCompiler.Compile(fixture.Request(
                intents: fixture.Intents.Concat(new[]
                {
                    new ActivitySlotProjectionIntent(
                        new ActivitySlotId("SLOT_UNKNOWN"), ActivitySlotSemanticKind.NpcAnchor),
                })));
            AssertAtomicFailure(unknown, ActivityShellCompileErrorCode.UnknownSlot);

            var mismatched = fixture.Intents.Select(value => value.SlotId.Value == "SLOT_DEVICE"
                ? new ActivitySlotProjectionIntent(value.SlotId, ActivitySlotSemanticKind.RewardAnchor)
                : value).ToArray();
            AssertAtomicFailure(ActivityShellCompiler.Compile(fixture.Request(intents: mismatched)),
                ActivityShellCompileErrorCode.SlotSemanticMismatch);
        }

        [Test]
        public void OutOfActiveZoneAndSlotPlusMissingMechanismBindingFailAtomically()
        {
            var fixture = Physical.Value;
            var outside = new LocalTileCoord(999, 999);
            var outsideSlots = fixture.Activity.Slots.Select(value => value.Id.Value == "SLOT_CUE"
                ? new ActivitySlot(value.Id, value.Kind, outside, value.MarkerId)
                : value).ToArray();
            var outsideActivity = ActivityShellFixture.CloneActivity(fixture.Activity, slots: outsideSlots);
            var outsideZones = fixture.Zones.Select(value => value.Kind == ActivityShellZoneKind.Cue
                ? new ActivityShellZoneDefinition(value.Kind, new[] { outside })
                : value.Kind == ActivityShellZoneKind.Core
                    ? new ActivityShellZoneDefinition(value.Kind,
                        value.SourceCoordinates.Where(coordinate => coordinate != fixture.CueCoordinate).Concat(new[] { outside }))
                    : value).ToArray();
            var outsideResult = ActivityShellCompiler.Compile(fixture.Request(
                activity: outsideActivity, zones: outsideZones));
            AssertAtomicFailure(outsideResult, ActivityShellCompileErrorCode.InvalidZone);
            Assert.That(outsideResult.Errors.Select(value => value.Code),
                Does.Contain(ActivityShellCompileErrorCode.SlotOutsideActiveCanvas));

            var brokenNodes = fixture.Activity.MechanismGraph.Nodes.Select(value => value.NodeId == "MECH_DEVICE"
                ? new MechanismNode(value.NodeId, value.Kind, new ActivitySlotId("SLOT_UNKNOWN"))
                : value).ToArray();
            var brokenActivity = ActivityShellFixture.CloneActivity(fixture.Activity,
                mechanism: new MechanismGraph(brokenNodes, fixture.Activity.MechanismGraph.Edges));
            AssertAtomicFailure(ActivityShellCompiler.Compile(fixture.Request(activity: brokenActivity)),
                ActivityShellCompileErrorCode.MissingGraphSlotBinding);
        }

        [Test]
        public void ActivityIdentityArtifactDigestAndWorkingCanvasMismatchFailAtomically()
        {
            var fixture = Physical.Value;
            var wrongIdentity = ActivityShellFixture.CloneActivity(
                fixture.Activity,
                clusterId: new TerrainClusterId("TC_OTHER_CLUSTER"),
                spineId: new SpineVariantId("SPINE_OTHER_VARIANT"));
            AssertAtomicFailure(ActivityShellCompiler.Compile(fixture.Request(activity: wrongIdentity)),
                ActivityShellCompileErrorCode.IdentityMismatch);

            AssertAtomicFailure(ActivityShellCompiler.Compile(fixture.Request(
                    expectedActivityDigest: new string('a', 64))),
                ActivityShellCompileErrorCode.ArtifactDigestMismatch);
            AssertAtomicFailure(ActivityShellCompiler.Compile(fixture.Request(
                    expectedSourceDigest: new string('b', 64))),
                ActivityShellCompileErrorCode.ArtifactDigestMismatch);
            AssertAtomicFailure(ActivityShellCompiler.Compile(fixture.Request(
                    expectedWorkingDigest: new string('c', 64))),
                ActivityShellCompileErrorCode.ArtifactDigestMismatch);
        }

        [Test]
        public void PublicSurfaceIsImmutableAndProductionHasNoExecutionOrSideEffectDependencies()
        {
            foreach (var type in new[]
                     {
                         typeof(ActivityShellZoneDefinition), typeof(ActivitySlotProjectionIntent),
                         typeof(ProjectedActivityShellCell), typeof(ProjectedActivitySlot),
                         typeof(ActivityCueSlotBinding), typeof(ActivityMechanismSlotBinding),
                         typeof(ActivityProgressionShellBinding), typeof(ActivityShellCanvas),
                         typeof(ActivityShellCompileRequest), typeof(ActivityShellCompileError),
                         typeof(ActivityShellCompileResult),
                     })
            {
                Assert.That(type.IsSealed, Is.True, type.Name);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(value => value.SetMethod != null), Is.Empty, type.Name);
            }

            var sourceRoot = Path.GetFullPath(Path.Combine(Application.dataPath,
                "_Game/Map/Runtime/WorldGeneration/Activities"));
            var source = string.Join("\n", Directory.GetFiles(sourceRoot, "ActivityShell*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(value => value, StringComparer.Ordinal).Select(File.ReadAllText));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine", "System.IO", "System.Random", "Random.",
                         "MonoBehaviour", "Update()", "GameObject", "Tilemap", "Rigidbody",
                     })
            {
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
            }

            var zones = Physical.Value.Zones.ToList();
            var intents = Physical.Value.Intents.ToList();
            var request = Physical.Value.Request(zones: zones, intents: intents);
            zones.Clear();
            intents.Clear();
            Assert.That(request.Zones.Count, Is.EqualTo(4));
            Assert.That(request.SlotIntents.Count, Is.EqualTo(9));
            Assert.Throws<NotSupportedException>(() => ((IList)request.Zones).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList)request.SlotIntents).Clear());
        }

        private static void AssertAtomicFailure(
            ActivityShellCompileResult result,
            ActivityShellCompileErrorCode expected)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Canvas, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Zones.Count, Is.Zero);
            Assert.That(result.ZoneCells.Count, Is.Zero);
            Assert.That(result.Slots.Count, Is.Zero);
            Assert.That(result.CueBindings.Count, Is.Zero);
            Assert.That(result.MechanismBindings.Count, Is.Zero);
            Assert.That(result.ProgressionBindings.Count, Is.Zero);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expected), Join(result));
            Assert.That(result.Errors, Is.Ordered);
        }

        private static string Join(ActivityShellCompileResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private sealed class ActivityShellFixture
        {
            private ActivityShellFixture(
                TerrainClusterAuthoringCatalog terrainCatalog,
                TerrainClusterAuthoringEntry entry,
                MicroPatternAuthoringCatalog microCatalog,
                string sourceDigest,
                TerrainClusterLocalCanvas localCanvas,
                TerrainClusterRoleSocketContract roleSocket,
                TerrainClusterTraversalCompilation traversal,
                TerrainClusterRouteWitnessReport routeWitness,
                TerrainClusterPatternRenderReport patternReport,
                ActivityStructureContract activity,
                IReadOnlyList<ActivityShellZoneDefinition> zones,
                IReadOnlyList<ActivitySlotProjectionIntent> intents,
                LocalTileCoord cueCoordinate)
            {
                TerrainCatalog = terrainCatalog;
                Entry = entry;
                MicroCatalog = microCatalog;
                SourceDigest = sourceDigest;
                LocalCanvas = localCanvas;
                RoleSocket = roleSocket;
                Traversal = traversal;
                RouteWitness = routeWitness;
                PatternReport = patternReport;
                Activity = activity;
                Zones = zones;
                Intents = intents;
                CueCoordinate = cueCoordinate;
            }

            public TerrainClusterAuthoringCatalog TerrainCatalog { get; }
            public TerrainClusterAuthoringEntry Entry { get; }
            public MicroPatternAuthoringCatalog MicroCatalog { get; }
            public string SourceDigest { get; }
            public TerrainClusterLocalCanvas LocalCanvas { get; }
            public TerrainClusterRoleSocketContract RoleSocket { get; }
            public TerrainClusterTraversalCompilation Traversal { get; }
            public TerrainClusterRouteWitnessReport RouteWitness { get; }
            public TerrainClusterPatternRenderReport PatternReport { get; }
            public ActivityStructureContract Activity { get; }
            public IReadOnlyList<ActivityShellZoneDefinition> Zones { get; }
            public IReadOnlyList<ActivitySlotProjectionIntent> Intents { get; }
            public LocalTileCoord CueCoordinate { get; }

            public static ActivityShellFixture Build()
            {
                var terrainCatalog = ImportCatalog<TerrainClusterAuthoringCatalog>(
                    "StarNight.MapAuthoring.WorldGeneration.Import.TerrainClusterCsvImporterV2");
                Assert.That(terrainCatalog.StableDigest, Is.EqualTo(ApprovedCatalogDigest));
                Assert.That(terrainCatalog.Entries.Count, Is.EqualTo(16));
                TerrainClusterAuthoringEntry entry;
                Assert.That(terrainCatalog.TryGet(new TerrainClusterId(RepresentativeClusterId), out entry), Is.True);
                var microCatalog = ImportCatalog<MicroPatternAuthoringCatalog>(
                    "StarNight.MapAuthoring.WorldGeneration.Import.MicroPatternCsvImporterV2");

                var validation = TerrainClusterContractValidator.Validate(entry.Contract);
                Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
                var footprint = TerrainClusterFootprintCompiler.Compile(
                    new TerrainClusterFootprintCompileRequest(entry.Contract, ClusterFootprintTransform.R0));
                Assert.That(footprint.IsSuccess, Is.True, string.Join("\n", footprint.Errors));
                var sourceEntry = entry.Contract.Ports.Single(value => value.IsPrimary && value.Kind == ClusterPortKind.Entry);
                var sourceExit = entry.Contract.Ports.Single(value => value.IsPrimary && value.Kind == ClusterPortKind.Exit);
                var sockets = new[]
                {
                    new ClusterSectorSocketEvidence("SR_MAP12_ENTRY", "SOCKET_MAP12_ENTRY",
                        sourceEntry.OutwardSide, 2, true, ClusterPortKind.Entry),
                    new ClusterSectorSocketEvidence("SR_MAP12_EXIT", "SOCKET_MAP12_EXIT",
                        sourceExit.OutwardSide, 3, true, ClusterPortKind.Exit),
                };
                var role = TerrainClusterRoleSocketCompiler.Compile(
                    new TerrainClusterRoleSocketCompileRequest(entry.Contract, validation.CanonicalDigest,
                        footprint.LocalCanvas, footprint.CanonicalDigest, sockets));
                Assert.That(role.IsSuccess, Is.True, string.Join("\n", role.Errors));
                var traversal = TerrainClusterTraversalCompiler.Compile(
                    new TerrainClusterTraversalCompileRequest(entry.Contract, validation.CanonicalDigest,
                        footprint.LocalCanvas, footprint.CanonicalDigest,
                        role.Contract, role.CanonicalDigest));
                Assert.That(traversal.IsSuccess, Is.True, string.Join("\n", traversal.Errors));
                var witness = TerrainClusterRouteWitnessCompiler.Compile(
                    new TerrainClusterRouteWitnessCompileRequest(footprint.LocalCanvas, footprint.CanonicalDigest,
                        role.Contract, role.CanonicalDigest, traversal.Compilation,
                        traversal.CanonicalDigest, entry.RouteIntent));
                Assert.That(witness.IsSuccess, Is.True, string.Join("\n", witness.Errors));
                var pattern = TerrainClusterPatternRenderer.Render(
                    new TerrainClusterPatternRenderRequest(footprint.LocalCanvas, footprint.CanonicalDigest,
                        traversal.Compilation, traversal.CanonicalDigest,
                        witness.Report, witness.CanonicalDigest,
                        microCatalog, microCatalog.StableDigest,
                        Array.Empty<TerrainClusterPatternZoneCell>(),
                        Array.Empty<TerrainClusterPatternPlacementIntent>()));
                Assert.That(pattern.Success, Is.True, string.Join("\n", pattern.Errors));
                Assert.That(pattern.Report.IsPatternFree, Is.True);

                var coordinates = SelectCoordinates(footprint.LocalCanvas, traversal.Compilation);
                var slots = CreateSlots(coordinates);
                var activity = CreateActivity(entry, validation.CanonicalDigest, slots);
                var activityValidation = ActivityContractValidator.Validate(activity, entry.Contract);
                Assert.That(activityValidation.IsValid, Is.True, string.Join("\n", activityValidation.Errors));
                var zones = CreateZones(coordinates);
                var intents = CreateIntents(slots);
                return new ActivityShellFixture(
                    terrainCatalog, entry, microCatalog, validation.CanonicalDigest,
                    footprint.LocalCanvas, role.Contract, traversal.Compilation,
                    witness.Report, pattern.Report, activity, zones, intents, coordinates[0]);
            }

            public ActivityShellCompileRequest Request(
                ActivityStructureContract activity = null,
                IEnumerable<ActivityShellZoneDefinition> zones = null,
                IEnumerable<ActivitySlotProjectionIntent> intents = null,
                string expectedActivityDigest = null,
                string expectedSourceDigest = null,
                string expectedWorkingDigest = null)
            {
                var actualActivity = activity ?? Activity;
                var validation = ActivityContractValidator.Validate(actualActivity, Entry.Contract);
                return new ActivityShellCompileRequest(
                    Entry.Contract,
                    expectedSourceDigest ?? SourceDigest,
                    actualActivity,
                    expectedActivityDigest ?? validation.CanonicalDigest,
                    LocalCanvas,
                    LocalCanvas.CanonicalDigest,
                    RoleSocket,
                    RoleSocket.CanonicalDigest,
                    Traversal,
                    Traversal.CanonicalDigest,
                    RouteWitness,
                    RouteWitness.CanonicalDigest,
                    PatternReport,
                    PatternReport.CanonicalDigest,
                    expectedWorkingDigest ?? PatternReport.FinalWorkingCanvas.CanonicalDigest,
                    zones ?? Zones,
                    intents ?? Intents);
            }

            public string CaptureUpstream()
            {
                return string.Join("|", new[]
                {
                    SourceDigest,
                    LocalCanvas.CanonicalDigest,
                    RoleSocket.CanonicalDigest,
                    Traversal.CanonicalDigest,
                    RouteWitness.CanonicalDigest,
                    PatternReport.CanonicalDigest,
                    PatternReport.FinalWorkingCanvas.CanonicalDigest,
                    RouteWitness.StaticShell.ActiveTileCount.ToString(CultureInfo.InvariantCulture),
                    PatternReport.FinalWorkingCanvas.CoordinateCount.ToString(CultureInfo.InvariantCulture),
                });
            }

            public static ActivityStructureContract CloneActivity(
                ActivityStructureContract source,
                TerrainClusterId? clusterId = null,
                SpineVariantId? spineId = null,
                IEnumerable<ActivitySlot> slots = null,
                IEnumerable<ActivityCue> cues = null,
                MechanismGraph mechanism = null,
                ProgressionGraph progression = null)
            {
                return new ActivityStructureContract(
                    source.Id,
                    clusterId ?? source.TerrainClusterId,
                    spineId ?? source.CompatibleSpineVariantId,
                    source.CompatiblePacingRoles,
                    source.CompatibleAccessClasses,
                    slots ?? source.Slots,
                    cues ?? source.Cues,
                    mechanism ?? source.MechanismGraph,
                    progression ?? source.ProgressionGraph,
                    source.RemovalSafety,
                    source.DisplayText);
            }

            private static TCatalog ImportCatalog<TCatalog>(string importerTypeName)
                where TCatalog : class
            {
                var importerType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType(importerTypeName, false))
                    .FirstOrDefault(type => type != null);
                if (importerType == null)
                {
                    var editorAssembly = Assembly.Load("MapAuthoring.Editor");
                    importerType = editorAssembly.GetType(importerTypeName, true);
                }
                var importer = Activator.CreateInstance(importerType);
                var result = importerType.GetMethod("Import", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(importer, null);
                var resultType = result.GetType();
                var success = (bool)resultType.GetProperty("Success").GetValue(result, null);
                if (!success)
                {
                    var errors = ((IEnumerable)resultType.GetProperty("Errors").GetValue(result, null))
                        .Cast<object>().Select(value => value.ToString());
                    Assert.Fail(string.Join("\n", errors));
                }
                var catalog = resultType.GetProperty("Catalog").GetValue(result, null) as TCatalog;
                Assert.That(catalog, Is.Not.Null, importerTypeName);
                return catalog;
            }

            private static LocalTileCoord[] SelectCoordinates(
                TerrainClusterLocalCanvas canvas,
                TerrainClusterTraversalCompilation traversal)
            {
                var active = canvas.TileCells.Where(value => value.State == ClusterChunkMaskState.Active)
                    .Select(value => value.SourceCoordinate).Distinct().OrderBy(value => value.Y)
                    .ThenBy(value => value.X).ToArray();
                var activeSet = active.ToHashSet();
                var protectedSource = traversal.ProtectedTiles.SelectMany(value => value.Provenance)
                    .Select(value => value.SourceCoordinate).Where(activeSet.Contains).Distinct()
                    .OrderBy(value => value.Y).ThenBy(value => value.X).ToArray();
                Assert.That(protectedSource.Length, Is.GreaterThan(0));
                var selected = protectedSource.Take(1)
                    .Concat(active.Where(value => !protectedSource.Take(1).Contains(value)))
                    .Take(9).ToArray();
                Assert.That(selected.Length, Is.EqualTo(9));
                return selected;
            }

            private static ActivitySlot[] CreateSlots(IReadOnlyList<LocalTileCoord> coordinates)
            {
                return new[]
                {
                    Slot("SLOT_CUE", ActivitySlotKind.Cue, coordinates[0]),
                    Slot("SLOT_TRIGGER", ActivitySlotKind.Trigger, coordinates[1]),
                    Slot("SLOT_DEVICE", ActivitySlotKind.Device, coordinates[2]),
                    Slot("SLOT_HAZARD", ActivitySlotKind.Hazard, coordinates[3]),
                    Slot("SLOT_PROJECTILE", ActivitySlotKind.Projectile, coordinates[4]),
                    Slot("SLOT_REWARD", ActivitySlotKind.Reward, coordinates[5]),
                    Slot("SLOT_RECOVERY", ActivitySlotKind.Recovery, coordinates[6]),
                    Slot("SLOT_RESET", ActivitySlotKind.Reset, coordinates[7]),
                    Slot("SLOT_NPC", ActivitySlotKind.Npc, coordinates[8]),
                };
            }

            private static ActivityStructureContract CreateActivity(
                TerrainClusterAuthoringEntry entry,
                string contractDigest,
                IReadOnlyList<ActivitySlot> slots)
            {
                var nodes = new[]
                {
                    new MechanismNode("MECH_CUE", MechanismNodeKind.CueEmitter, new ActivitySlotId("SLOT_CUE")),
                    new MechanismNode("MECH_TRIGGER", MechanismNodeKind.Trigger, new ActivitySlotId("SLOT_TRIGGER")),
                    new MechanismNode("MECH_DEVICE", MechanismNodeKind.Device, new ActivitySlotId("SLOT_DEVICE")),
                    new MechanismNode("MECH_HAZARD", MechanismNodeKind.Hazard, new ActivitySlotId("SLOT_HAZARD")),
                    new MechanismNode("MECH_PROJECTILE", MechanismNodeKind.ProjectileEmitter, new ActivitySlotId("SLOT_PROJECTILE")),
                    new MechanismNode("MECH_REWARD", MechanismNodeKind.RewardEmitter, new ActivitySlotId("SLOT_REWARD")),
                    new MechanismNode("MECH_RECOVERY", MechanismNodeKind.RecoveryController, new ActivitySlotId("SLOT_RECOVERY")),
                    new MechanismNode("MECH_RESET", MechanismNodeKind.ResetController, new ActivitySlotId("SLOT_RESET")),
                };
                var mechanism = new MechanismGraph(nodes, new[]
                {
                    new MechanismEdge("MECH_EDGE_TRIGGER_CUE", "MECH_TRIGGER", "MECH_CUE", MechanismRelationKind.Activates),
                    new MechanismEdge("MECH_EDGE_TRIGGER_DEVICE", "MECH_TRIGGER", "MECH_DEVICE", MechanismRelationKind.Activates),
                    new MechanismEdge("MECH_EDGE_DEVICE_HAZARD", "MECH_DEVICE", "MECH_HAZARD", MechanismRelationKind.Drives),
                    new MechanismEdge("MECH_EDGE_DEVICE_PROJECTILE", "MECH_DEVICE", "MECH_PROJECTILE", MechanismRelationKind.Drives),
                    new MechanismEdge("MECH_EDGE_DEVICE_REWARD", "MECH_DEVICE", "MECH_REWARD", MechanismRelationKind.Drives),
                    new MechanismEdge("MECH_EDGE_DEVICE_RECOVERY", "MECH_DEVICE", "MECH_RECOVERY", MechanismRelationKind.Enables),
                    new MechanismEdge("MECH_EDGE_DEVICE_RESET", "MECH_DEVICE", "MECH_RESET", MechanismRelationKind.Enables),
                });
                var progressionNodes = new[]
                {
                    new ProgressionNode("PROG_CUE", ProgressionPhaseKind.Cue),
                    new ProgressionNode("PROG_ACTIVATION", ProgressionPhaseKind.Activation),
                    new ProgressionNode("PROG_CORE", ProgressionPhaseKind.Core),
                    new ProgressionNode("PROG_REWARD", ProgressionPhaseKind.Reward),
                    new ProgressionNode("PROG_RECOVERY", ProgressionPhaseKind.Recovery),
                    new ProgressionNode("PROG_RESET", ProgressionPhaseKind.Reset),
                    new ProgressionNode("PROG_EXIT", ProgressionPhaseKind.Exit),
                };
                var progression = new ProgressionGraph("PROG_CUE", "PROG_EXIT", progressionNodes, new[]
                {
                    new ProgressionEdge("PROG_EDGE_CUE_ACTIVATION", "PROG_CUE", "PROG_ACTIVATION", ProgressionEdgeKind.Advance),
                    new ProgressionEdge("PROG_EDGE_ACTIVATION_CORE", "PROG_ACTIVATION", "PROG_CORE", ProgressionEdgeKind.Advance),
                    new ProgressionEdge("PROG_EDGE_CORE_REWARD", "PROG_CORE", "PROG_REWARD", ProgressionEdgeKind.Advance),
                    new ProgressionEdge("PROG_EDGE_REWARD_RECOVERY", "PROG_REWARD", "PROG_RECOVERY", ProgressionEdgeKind.Advance),
                    new ProgressionEdge("PROG_EDGE_RECOVERY_EXIT", "PROG_RECOVERY", "PROG_EXIT", ProgressionEdgeKind.Exit),
                    new ProgressionEdge("PROG_EDGE_CORE_RESET", "PROG_CORE", "PROG_RESET", ProgressionEdgeKind.Failure),
                    new ProgressionEdge("PROG_EDGE_RESET_CORE", "PROG_RESET", "PROG_CORE", ProgressionEdgeKind.Reset),
                });
                var entryRole = entry.Contract.RoleAnchors.Single(value => value.Role == ClusterRoleKind.Entry);
                var exitRole = entry.Contract.RoleAnchors.Single(value => value.Role == ClusterRoleKind.Exit);
                var safety = new ActivityRemovalSafety(
                    entry.BaselineVariantId,
                    entryRole.TraversalNodeId,
                    exitRole.TraversalNodeId,
                    new[] { slots[2].Tile },
                    new[] { slots[6].Tile },
                    true,
                    true,
                    false,
                    false,
                    1,
                    1,
                    AccessClass.MandatoryNoTool,
                    AccessClass.MandatoryNoTool,
                    contractDigest,
                    contractDigest);
                return new ActivityStructureContract(
                    new ActivityStructureId("ACT_MAP12_CRATER_BOWL"),
                    entry.Id,
                    entry.BaselineVariantId,
                    new[] { PacingRole.Activity, PacingRole.Risk, PacingRole.Recovery },
                    new[] { AccessClass.MandatoryNoTool },
                    slots,
                    new[] { new ActivityCue(ActivityCueKind.Visual, new ActivitySlotId("SLOT_CUE"), true) },
                    mechanism,
                    progression,
                    safety,
                    "MAP12_01 test-owned Activity fixture");
            }

            private static ActivityShellZoneDefinition[] CreateZones(IReadOnlyList<LocalTileCoord> coordinates)
            {
                return new[]
                {
                    new ActivityShellZoneDefinition(ActivityShellZoneKind.Cue, new[] { coordinates[0] }),
                    new ActivityShellZoneDefinition(ActivityShellZoneKind.Core, new[]
                    {
                        coordinates[0], coordinates[1], coordinates[2], coordinates[3], coordinates[4], coordinates[8],
                    }),
                    new ActivityShellZoneDefinition(ActivityShellZoneKind.Reward, new[] { coordinates[5] }),
                    new ActivityShellZoneDefinition(ActivityShellZoneKind.Recovery, new[] { coordinates[6], coordinates[7] }),
                };
            }

            private static ActivitySlotProjectionIntent[] CreateIntents(IEnumerable<ActivitySlot> slots)
            {
                return slots.Select(value => new ActivitySlotProjectionIntent(value.Id, Semantic(value.Kind))).ToArray();
            }

            private static ActivitySlotSemanticKind Semantic(ActivitySlotKind kind)
            {
                switch (kind)
                {
                    case ActivitySlotKind.Cue: return ActivitySlotSemanticKind.CueMarker;
                    case ActivitySlotKind.Trigger: return ActivitySlotSemanticKind.PressurePlateTrigger;
                    case ActivitySlotKind.Device: return ActivitySlotSemanticKind.DeviceAnchor;
                    case ActivitySlotKind.Hazard: return ActivitySlotSemanticKind.ChaseOrHazardSpawn;
                    case ActivitySlotKind.Projectile: return ActivitySlotSemanticKind.ProjectileEmitter;
                    case ActivitySlotKind.Reward: return ActivitySlotSemanticKind.RewardAnchor;
                    case ActivitySlotKind.Recovery: return ActivitySlotSemanticKind.RecoveryAnchor;
                    case ActivitySlotKind.Reset: return ActivitySlotSemanticKind.ResetAnchor;
                    case ActivitySlotKind.Npc: return ActivitySlotSemanticKind.NpcAnchor;
                    default: throw new ArgumentOutOfRangeException(nameof(kind));
                }
            }

            private static ActivitySlot Slot(string id, ActivitySlotKind kind, LocalTileCoord tile)
            {
                return new ActivitySlot(new ActivitySlotId(id), kind, tile,
                    "MARKER_" + id.Substring("SLOT_".Length));
            }
        }
    }
}
