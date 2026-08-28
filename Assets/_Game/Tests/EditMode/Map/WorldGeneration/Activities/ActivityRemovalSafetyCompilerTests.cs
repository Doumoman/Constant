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
    [Category("MAP12_02")]
    public sealed class ActivityRemovalSafetyCompilerTests
    {
        private const string RepresentativeClusterId = "TC_CRATER_BOWL_ASCENT";
        private const string ApprovedCatalogDigest =
            "9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7";

        private static readonly Lazy<RemovalFixture> Physical =
            new Lazy<RemovalFixture>(RemovalFixture.Build);

        [Test]
        public void RealChainPublishesCueRemovalSafeRecoveryAndCriticalPreservationProof()
        {
            var fixture = Physical.Value;
            var before = fixture.CaptureUpstream();
            var result = ActivityRemovalSafetyCompiler.Compile(fixture.Request());

            Assert.That(result.IsSuccess, Is.True, Join(result));
            Assert.That(result.CueProofs.Count, Is.EqualTo(1));
            Assert.That(result.CueProofs[0].ObservationEdgeOrdinal,
                Is.LessThan(result.CueProofs[0].ActivationBoundaryEdgeOrdinal));
            Assert.That(result.CueProofs[0].OccludingCoordinateCount, Is.Zero);
            Assert.That(result.ActiveSnapshot.Kind, Is.EqualTo(ActivityOverlaySnapshotKind.Active));
            Assert.That(result.ActiveSnapshot.OverlayIdentities.Count, Is.EqualTo(29));
            Assert.That(result.RemovedSnapshot.Kind, Is.EqualTo(ActivityOverlaySnapshotKind.Removed));
            Assert.That(result.RemovedSnapshot.OverlayIdentities, Is.Empty);
            Assert.That(result.Proof.ResidualOverlayCount, Is.Zero);
            Assert.That(result.Proof.UnderlyingTileDeltaCount, Is.Zero);
            Assert.That(result.ActiveSnapshot.StaticShellDigest,
                Is.EqualTo(result.RemovedSnapshot.StaticShellDigest));
            Assert.That(result.ActiveSnapshot.WorkingCanvasDigest,
                Is.EqualTo(result.RemovedSnapshot.WorkingCanvasDigest));
            Assert.That(result.ActiveSnapshot.TraversalDigest,
                Is.EqualTo(result.RemovedSnapshot.TraversalDigest));
            Assert.That(result.ActiveSnapshot.RouteWitnessDigest,
                Is.EqualTo(result.RemovedSnapshot.RouteWitnessDigest));
            Assert.That(result.ActiveSnapshot.AccessClass,
                Is.EqualTo(result.RemovedSnapshot.AccessClass));
            Assert.That(result.SafePocketProofs.Count, Is.EqualTo(1));
            Assert.That(result.SafePocketProofs[0].ConnectedToPublishedOpenEvidence, Is.True);
            Assert.That(result.SafePocketProofs[0].OccupancyAfterRemoval,
                Is.EqualTo(TerrainClusterShellOccupancy.Air));
            Assert.That(result.RecoveryProofs.Count, Is.EqualTo(1));
            Assert.That(result.RecoveryProofs[0].UsesSourceEdgesOnly, Is.True);
            Assert.That(result.RecoveryProofs[0].SyntheticEdgeCount, Is.Zero);
            Assert.That(result.RecoveryProofs[0].TeleportEdgeCount, Is.Zero);
            Assert.That(result.RecoveryProofs[0].EstimatedDurationMilliseconds,
                Is.InRange(2000, 5000));
            Assert.That(result.CriticalTargetProofs.Select(value => value.Kind),
                Is.EquivalentTo(new[]
                {
                    ActivityCriticalTargetKind.MandatoryExit,
                    ActivityCriticalTargetKind.Reward,
                }));
            Assert.That(result.CriticalTargetProofs.All(value => value.IsPreserved), Is.True);
            Assert.That(result.Proof.RendererInvocationCount, Is.Zero);
            Assert.That(result.Proof.GeometryWriteCount, Is.Zero);
            Assert.That(result.Proof.GeometryCarveCount, Is.Zero);
            Assert.That(result.Proof.RngDrawCount, Is.Zero);
            Assert.That(fixture.CaptureUpstream(), Is.EqualTo(before));

            TestContext.WriteLine(
                "REMOVAL_PROOF cluster=" + fixture.Entry.Id.Value +
                " variant=" + fixture.Entry.BaselineVariantId.Value +
                " source_shell=" + fixture.Shell.CanonicalDigest +
                " proof=" + result.CanonicalDigest +
                " active=" + result.ActiveSnapshot.OverlayIdentities.Count +
                " removed=" + result.RemovedSnapshot.OverlayIdentities.Count +
                " safe=" + Coordinate(result.SafePocketProofs[0].SourceCoordinate) +
                " recovery=" + Coordinate(result.RecoveryProofs[0].SourceCoordinate) +
                " recovery_ms=" + result.RecoveryProofs[0].EstimatedDurationMilliseconds);
        }

        [Test]
        public void VisualMotionUseClearSupercoverWhileAudioEnvironmentUseDistanceOnly()
        {
            var fixture = Physical.Value;
            foreach (var kind in new[] { ActivityCueKind.Visual, ActivityCueKind.Motion })
            {
                var clear = fixture.Scenario(kind, fixture.ClearCueSource);
                var result = ActivityRemovalSafetyCompiler.Compile(fixture.Request(clear));
                Assert.That(result.IsSuccess, Is.True, kind + "\n" + Join(result));
                Assert.That(result.CueProofs.Single().UsesDistanceOnly, Is.False);
                Assert.That(result.CueProofs.Single().SupercoverCoordinates.Count, Is.GreaterThan(0));
            }

            foreach (var kind in new[] { ActivityCueKind.Audio, ActivityCueKind.Environment })
            {
                var distanceOnly = fixture.Scenario(kind, fixture.OccludedCueSource);
                var result = ActivityRemovalSafetyCompiler.Compile(fixture.Request(distanceOnly));
                Assert.That(result.IsSuccess, Is.True, kind + "\n" + Join(result));
                Assert.That(result.CueProofs.Single().UsesDistanceOnly, Is.True);
                Assert.That(result.CueProofs.Single().SupercoverCoordinates, Is.Empty);
            }
        }

        [Test]
        public void SameOrAfterOutOfRangeAndOccludedCueFailAtomically()
        {
            var fixture = Physical.Value;
            var valid = fixture.Evidence(ActivityCueKind.Visual, fixture.ClearCueSource);
            var sameEdge = new ActivityCueObservationEvidence(
                valid.CueId, valid.CueKind, valid.SlotId,
                valid.BaselineWitnessObservationEdgeId,
                valid.BaselineWitnessObservationEdgeId,
                valid.ObservationSourceCoordinate,
                valid.MaximumObservationDistanceTiles);
            AssertAtomicFailure(
                ActivityRemovalSafetyCompiler.Compile(fixture.Request(cueEvidence: new[] { sameEdge })),
                ActivityRemovalSafetyCompileErrorCode.CueNotBeforeActivation);

            var outOfRange = new ActivityCueObservationEvidence(
                valid.CueId, valid.CueKind, valid.SlotId,
                valid.BaselineWitnessObservationEdgeId,
                valid.ActivationBoundaryEdgeId,
                valid.ObservationSourceCoordinate,
                0);
            AssertAtomicFailure(
                ActivityRemovalSafetyCompiler.Compile(fixture.Request(cueEvidence: new[] { outOfRange })),
                ActivityRemovalSafetyCompileErrorCode.CueOutOfRange);

            var occluded = fixture.Scenario(ActivityCueKind.Visual, fixture.OccludedCueSource);
            AssertAtomicFailure(
                ActivityRemovalSafetyCompiler.Compile(fixture.Request(occluded)),
                ActivityRemovalSafetyCompileErrorCode.CueOccluded);
        }

        [Test]
        public void MissingExtraResidualMutationAndArtifactDriftFailAtomically()
        {
            var fixture = Physical.Value;
            var exact = fixture.OverlayIdentities(fixture.Shell);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    removalIntent: new ActivityOverlayRemovalIntent(exact.Skip(1)))),
                ActivityRemovalSafetyCompileErrorCode.InvalidActiveSnapshot);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    removalIntent: new ActivityOverlayRemovalIntent(
                        exact, new[] { exact[0] }))),
                ActivityRemovalSafetyCompileErrorCode.ResidualOverlay);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    removalIntent: new ActivityOverlayRemovalIntent(
                        exact, permanentTileMutationDeclared: true))),
                ActivityRemovalSafetyCompileErrorCode.PermanentMutationDeclared);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    removalIntent: new ActivityOverlayRemovalIntent(
                        exact, staticShellDigestAfterRemovalDeclaration: new string('a', 64)))),
                ActivityRemovalSafetyCompileErrorCode.StaticShellChanged);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    removalIntent: new ActivityOverlayRemovalIntent(
                        exact, workingCanvasDigestAfterRemovalDeclaration: new string('b', 64)))),
                ActivityRemovalSafetyCompileErrorCode.WorkingCanvasChanged);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    removalIntent: new ActivityOverlayRemovalIntent(
                        exact, traversalDigestAfterRemovalDeclaration: new string('c', 64)))),
                ActivityRemovalSafetyCompileErrorCode.TraversalChanged);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    removalIntent: new ActivityOverlayRemovalIntent(
                        exact, accessClassAfterRemovalDeclaration: AccessClass.OptionalTool))),
                ActivityRemovalSafetyCompileErrorCode.AccessChanged);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    expectedShellDigest: new string('d', 64))),
                ActivityRemovalSafetyCompileErrorCode.ArtifactDigestMismatch);
        }

        [Test]
        public void UnsafePocketInvalidRecoveryAndCriticalDestructionFailAtomically()
        {
            var fixture = Physical.Value;
            var unsafeSafety = fixture.CloneSafety(
                safePockets: new[] { fixture.Shell.Slots.Single(value =>
                    value.Semantic == ActivitySlotSemanticKind.DeviceAnchor).SourceCoordinate });
            var unsafeScenario = fixture.ScenarioWithSafety(unsafeSafety);
            AssertAtomicFailure(
                ActivityRemovalSafetyCompiler.Compile(fixture.Request(unsafeScenario)),
                ActivityRemovalSafetyCompileErrorCode.UnsafePocketOverlap);

            var invalidRecovery = fixture.CloneSafety(
                recovery: new[] { fixture.NonWitnessAirSource });
            var invalidRecoveryScenario = fixture.ScenarioWithSafety(invalidRecovery);
            AssertAtomicFailure(
                ActivityRemovalSafetyCompiler.Compile(fixture.Request(invalidRecoveryScenario)),
                ActivityRemovalSafetyCompileErrorCode.InvalidRecoveryEvidence);

            var exact = fixture.OverlayIdentities(fixture.Shell);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    removalIntent: new ActivityOverlayRemovalIntent(
                        exact, mandatoryExitDestructionDeclared: true))),
                ActivityRemovalSafetyCompileErrorCode.ExitDestructionDeclared);
            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    removalIntent: new ActivityOverlayRemovalIntent(
                        exact, rewardDestructionDeclared: true))),
                ActivityRemovalSafetyCompileErrorCode.RewardDestructionDeclared);

            AssertAtomicFailure(ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                    critical: fixture.CriticalEvidence(fixture.Scenario(
                        ActivityCueKind.Visual, fixture.ClearCueSource))
                        .Where(value => value.Kind != ActivityCriticalTargetKind.Reward))),
                ActivityRemovalSafetyCompileErrorCode.MissingCriticalTarget);
        }

        [Test]
        public void RepeatReverseAndTurkishCultureProduceTheSameImmutableProof()
        {
            var fixture = Physical.Value;
            var canonical = ActivityRemovalSafetyCompiler.Compile(fixture.Request());
            var repeat = ActivityRemovalSafetyCompiler.Compile(fixture.Request());
            var reversed = ActivityRemovalSafetyCompiler.Compile(fixture.Request(
                cueEvidence: fixture.DefaultCueEvidence.Reverse(),
                removalIntent: new ActivityOverlayRemovalIntent(
                    fixture.OverlayIdentities(fixture.Shell).Reverse()),
                critical: fixture.CriticalEvidence(fixture.DefaultScenario).Reverse()));
            Assert.That(canonical.IsSuccess, Is.True, Join(canonical));
            Assert.That(repeat.IsSuccess, Is.True, Join(repeat));
            Assert.That(reversed.IsSuccess, Is.True, Join(reversed));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            ActivityRemovalSafetyCompileResult turkish;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                turkish = ActivityRemovalSafetyCompiler.Compile(fixture.Request());
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
            Assert.That(turkish.IsSuccess, Is.True, Join(turkish));
            Assert.That(repeat.CanonicalDigest, Is.EqualTo(canonical.CanonicalDigest));
            Assert.That(reversed.CanonicalDigest, Is.EqualTo(canonical.CanonicalDigest));
            Assert.That(turkish.CanonicalDigest, Is.EqualTo(canonical.CanonicalDigest));

            foreach (var type in new[]
                     {
                         typeof(ActivityCueObservationEvidence), typeof(ActivityCueObservationProof),
                         typeof(ActivityOverlayRemovalIntent), typeof(ActivityOverlaySnapshot),
                         typeof(ActivitySafePocketProof), typeof(ActivityRecoverySafetyProof),
                         typeof(ActivityCriticalTargetEvidence), typeof(ActivityCriticalPreservationProof),
                         typeof(ActivityRemovalSafetyProof), typeof(ActivityRemovalSafetyCompileRequest),
                         typeof(ActivityRemovalSafetyCompileError), typeof(ActivityRemovalSafetyCompileResult),
                     })
            {
                Assert.That(type.IsSealed, Is.True, type.Name);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(value => value.SetMethod != null), Is.Empty, type.Name);
            }
            Assert.Throws<NotSupportedException>(() => ((IList)canonical.CueProofs).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList)canonical.ActiveSnapshot.OverlayIdentities).Clear());
        }

        [Test]
        public void ProductionProofHasNoPrefabScenePhysicsAudioRngOrFileWriteDependencies()
        {
            var sourceRoot = Path.GetFullPath(Path.Combine(Application.dataPath,
                "_Game/Map/Runtime/WorldGeneration/Activities"));
            var source = string.Join("\n", new[]
            {
                Path.Combine(sourceRoot, "ActivityRemovalSafetyProof.cs"),
                Path.Combine(sourceRoot, "ActivityRemovalSafetyCompiler.cs"),
            }.Select(File.ReadAllText));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine", "System.IO", "System.Random", "Random.",
                         "MonoBehaviour", "GameObject", "Prefab", "Tilemap", "Rigidbody",
                         "Physics", "AudioSource", "Destroy(", "File.Write", "FileStream",
                     })
            {
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
            }
        }

        private static void AssertAtomicFailure(
            ActivityRemovalSafetyCompileResult result,
            ActivityRemovalSafetyCompileErrorCode expected)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Proof, Is.Null);
            Assert.That(result.ActiveSnapshot, Is.Null);
            Assert.That(result.RemovedSnapshot, Is.Null);
            Assert.That(result.CueProofs, Is.Empty);
            Assert.That(result.SafePocketProofs, Is.Empty);
            Assert.That(result.RecoveryProofs, Is.Empty);
            Assert.That(result.CriticalTargetProofs, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(expected), Join(result));
            Assert.That(result.Errors, Is.Ordered);
        }

        private static string Join(ActivityRemovalSafetyCompileResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string Coordinate(LocalTileCoord value)
        {
            return value.X.ToString(CultureInfo.InvariantCulture) + "," +
                   value.Y.ToString(CultureInfo.InvariantCulture);
        }

        private sealed class ActivityScenario
        {
            public ActivityScenario(ActivityStructureContract activity, ActivityShellCanvas shell,
                ActivityCueObservationEvidence evidence)
            {
                Activity = activity;
                Shell = shell;
                Evidence = evidence;
            }

            public ActivityStructureContract Activity { get; }
            public ActivityShellCanvas Shell { get; }
            public ActivityCueObservationEvidence Evidence { get; }
        }

        private sealed class RemovalFixture
        {
            private RemovalFixture(
                TerrainClusterAuthoringEntry entry,
                string sourceDigest,
                TerrainClusterLocalCanvas localCanvas,
                TerrainClusterRoleSocketContract roleSocket,
                TerrainClusterTraversalCompilation traversal,
                TerrainClusterRouteWitnessReport routeWitness,
                TerrainClusterPatternRenderReport patternReport,
                IReadOnlyList<ActivitySlotProjectionIntent> intents,
                LocalTileCoord clearCueSource,
                LocalTileCoord occludedCueSource,
                LocalTileCoord recoverySource,
                LocalTileCoord nonWitnessAirSource,
                string observationEdgeId,
                string activationEdgeId,
                LocalTileCoord observationSource,
                ActivityScenario defaultScenario)
            {
                Entry = entry;
                SourceDigest = sourceDigest;
                LocalCanvas = localCanvas;
                RoleSocket = roleSocket;
                Traversal = traversal;
                RouteWitness = routeWitness;
                PatternReport = patternReport;
                Intents = intents;
                ClearCueSource = clearCueSource;
                OccludedCueSource = occludedCueSource;
                RecoverySource = recoverySource;
                NonWitnessAirSource = nonWitnessAirSource;
                ObservationEdgeId = observationEdgeId;
                ActivationEdgeId = activationEdgeId;
                ObservationSource = observationSource;
                DefaultScenario = defaultScenario;
            }

            public TerrainClusterAuthoringEntry Entry { get; }
            public string SourceDigest { get; }
            public TerrainClusterLocalCanvas LocalCanvas { get; }
            public TerrainClusterRoleSocketContract RoleSocket { get; }
            public TerrainClusterTraversalCompilation Traversal { get; }
            public TerrainClusterRouteWitnessReport RouteWitness { get; }
            public TerrainClusterPatternRenderReport PatternReport { get; }
            public IReadOnlyList<ActivitySlotProjectionIntent> Intents { get; }
            public LocalTileCoord ClearCueSource { get; }
            public LocalTileCoord OccludedCueSource { get; }
            public LocalTileCoord RecoverySource { get; }
            public LocalTileCoord NonWitnessAirSource { get; }
            public string ObservationEdgeId { get; }
            public string ActivationEdgeId { get; }
            public LocalTileCoord ObservationSource { get; }
            public ActivityScenario DefaultScenario { get; }
            public ActivityStructureContract Activity => DefaultScenario.Activity;
            public ActivityShellCanvas Shell => DefaultScenario.Shell;
            public IReadOnlyList<ActivityCueObservationEvidence> DefaultCueEvidence =>
                new[] { DefaultScenario.Evidence };

            public static RemovalFixture Build()
            {
                var terrainCatalog = ImportCatalog<TerrainClusterAuthoringCatalog>(
                    "StarNight.MapAuthoring.WorldGeneration.Import.TerrainClusterCsvImporterV2");
                Assert.That(terrainCatalog.StableDigest, Is.EqualTo(ApprovedCatalogDigest));
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
                var role = TerrainClusterRoleSocketCompiler.Compile(new TerrainClusterRoleSocketCompileRequest(
                    entry.Contract, validation.CanonicalDigest, footprint.LocalCanvas, footprint.CanonicalDigest,
                    new[]
                    {
                        new ClusterSectorSocketEvidence("SR_MAP12_02_ENTRY", "SOCKET_MAP12_02_ENTRY",
                            sourceEntry.OutwardSide, 2, true, ClusterPortKind.Entry),
                        new ClusterSectorSocketEvidence("SR_MAP12_02_EXIT", "SOCKET_MAP12_02_EXIT",
                            sourceExit.OutwardSide, 3, true, ClusterPortKind.Exit),
                    }));
                Assert.That(role.IsSuccess, Is.True, string.Join("\n", role.Errors));
                var traversal = TerrainClusterTraversalCompiler.Compile(new TerrainClusterTraversalCompileRequest(
                    entry.Contract, validation.CanonicalDigest, footprint.LocalCanvas, footprint.CanonicalDigest,
                    role.Contract, role.CanonicalDigest));
                Assert.That(traversal.IsSuccess, Is.True, string.Join("\n", traversal.Errors));
                var witness = TerrainClusterRouteWitnessCompiler.Compile(new TerrainClusterRouteWitnessCompileRequest(
                    footprint.LocalCanvas, footprint.CanonicalDigest, role.Contract, role.CanonicalDigest,
                    traversal.Compilation, traversal.CanonicalDigest, entry.RouteIntent));
                Assert.That(witness.IsSuccess, Is.True, string.Join("\n", witness.Errors));
                var pattern = TerrainClusterPatternRenderer.Render(new TerrainClusterPatternRenderRequest(
                    footprint.LocalCanvas, footprint.CanonicalDigest,
                    traversal.Compilation, traversal.CanonicalDigest,
                    witness.Report, witness.CanonicalDigest,
                    microCatalog, microCatalog.StableDigest,
                    Array.Empty<TerrainClusterPatternZoneCell>(),
                    Array.Empty<TerrainClusterPatternPlacementIntent>()));
                Assert.That(pattern.Success, Is.True, string.Join("\n", pattern.Errors));
                Assert.That(pattern.Report.IsPatternFree, Is.True);
                Assert.That(witness.Report.BaselineRoute.OrderedEdges.Count, Is.GreaterThanOrEqualTo(2));

                CompiledClusterSpineVariant baseline;
                Assert.That(traversal.Compilation.TryGetVariant(entry.BaselineVariantId, out baseline), Is.True);
                var observationWitness = witness.Report.BaselineRoute.OrderedEdges[0];
                CompiledTraversalEdge observationEdge;
                Assert.That(baseline.TryGetEdge(observationWitness.EdgeId, out observationEdge), Is.True);
                var observationTile = observationEdge.Envelope.Centerline
                    .Concat(observationEdge.Envelope.Clearance)
                    .Concat(observationEdge.Envelope.Landing)
                    .First(value => IsAir(pattern.Report.FinalWorkingCanvas, value.CompiledCoordinate));
                var clearCue = observationTile.SourceCoordinate;
                var recoveryCompiled = witness.Report.RecoveryRoutes.SelectMany(value => value.CompiledCoordinates)
                    .Concat(witness.Report.RecoveryRoutes.SelectMany(value => value.CoveredProtectedTiles))
                    .Distinct().First(value => IsAir(pattern.Report.FinalWorkingCanvas, value));
                LocalTileCoord recoverySource;
                Assert.That(footprint.LocalCanvas.TryGetSourceTile(recoveryCompiled, out recoverySource), Is.True);
                var occludedCue = FindOccludedCue(
                    footprint.LocalCanvas, pattern.Report.FinalWorkingCanvas,
                    observationTile.CompiledCoordinate, clearCue, recoverySource);
                var publishedOpen = witness.Report.BaselineRoute.CoveredProtectedTiles
                    .Concat(witness.Report.RecoveryRoutes.SelectMany(value => value.CoveredProtectedTiles))
                    .Concat(witness.Report.BaselineRoute.CompiledCoordinates)
                    .Concat(witness.Report.RecoveryRoutes.SelectMany(value => value.CompiledCoordinates))
                    .ToHashSet();
                var nonWitnessCompiled = pattern.Report.FinalWorkingCanvas.Cells
                    .Where(value => !value.Solid && !publishedOpen.Contains(value.Coordinate))
                    .Select(value => value.Coordinate).First();
                LocalTileCoord nonWitnessSource;
                Assert.That(footprint.LocalCanvas.TryGetSourceTile(nonWitnessCompiled, out nonWitnessSource), Is.True);

                var coordinates = SelectSlotCoordinates(
                    footprint.LocalCanvas, pattern.Report.FinalWorkingCanvas, clearCue, recoverySource);
                var slots = CreateSlots(coordinates);
                var activity = CreateActivity(entry, validation.CanonicalDigest, slots,
                    clearCue, recoverySource);
                var intents = CreateIntents(slots);
                var zones = CreateZones(slots);
                var shell = CompileShell(entry, validation.CanonicalDigest, footprint.LocalCanvas,
                    role.Contract, traversal.Compilation, witness.Report, pattern.Report,
                    activity, zones, intents);
                var evidence = new ActivityCueObservationEvidence(
                    "CUE_OBSERVE_VISUAL", ActivityCueKind.Visual, new ActivitySlotId("SLOT_CUE"),
                    observationWitness.EdgeId, witness.Report.BaselineRoute.OrderedEdges[1].EdgeId,
                    observationTile.SourceCoordinate, 1);
                var scenario = new ActivityScenario(activity, shell, evidence);
                return new RemovalFixture(entry, validation.CanonicalDigest,
                    footprint.LocalCanvas, role.Contract, traversal.Compilation,
                    witness.Report, pattern.Report, intents, clearCue, occludedCue,
                    recoverySource, nonWitnessSource, observationWitness.EdgeId,
                    witness.Report.BaselineRoute.OrderedEdges[1].EdgeId,
                    observationTile.SourceCoordinate, scenario);
            }

            public ActivityScenario Scenario(ActivityCueKind kind, LocalTileCoord cueSource)
            {
                if (kind == DefaultScenario.Evidence.CueKind && cueSource == ClearCueSource)
                    return DefaultScenario;
                var slots = Activity.Slots.Select(value => value.Id.Value == "SLOT_CUE"
                    ? new ActivitySlot(value.Id, value.Kind, cueSource, value.MarkerId)
                    : value).ToArray();
                var activity = CloneActivity(Activity, slots: slots,
                    cues: new[] { new ActivityCue(kind, new ActivitySlotId("SLOT_CUE"), true) });
                var shell = CompileShell(Entry, SourceDigest, LocalCanvas, RoleSocket,
                    Traversal, RouteWitness, PatternReport, activity,
                    CreateZones(slots), Intents);
                return new ActivityScenario(activity, shell, Evidence(kind, cueSource));
            }

            public ActivityScenario ScenarioWithSafety(ActivityRemovalSafety safety)
            {
                var activity = CloneActivity(Activity, safety: safety);
                var shell = CompileShell(Entry, SourceDigest, LocalCanvas, RoleSocket,
                    Traversal, RouteWitness, PatternReport, activity,
                    CreateZones(activity.Slots), Intents);
                return new ActivityScenario(activity, shell,
                    Evidence(ActivityCueKind.Visual, ClearCueSource));
            }

            public ActivityCueObservationEvidence Evidence(ActivityCueKind kind, LocalTileCoord cueSource)
            {
                var distance = Math.Abs(ObservationSource.X - cueSource.X) +
                               Math.Abs(ObservationSource.Y - cueSource.Y);
                return new ActivityCueObservationEvidence(
                    "CUE_OBSERVE_" + kind.ToString().ToUpperInvariant(), kind,
                    new ActivitySlotId("SLOT_CUE"), ObservationEdgeId, ActivationEdgeId,
                    ObservationSource, Math.Max(1, distance));
            }

            public ActivityRemovalSafetyCompileRequest Request(
                ActivityScenario scenario = null,
                IEnumerable<ActivityCueObservationEvidence> cueEvidence = null,
                ActivityOverlayRemovalIntent removalIntent = null,
                IEnumerable<ActivityCriticalTargetEvidence> critical = null,
                string expectedShellDigest = null)
            {
                var actual = scenario ?? DefaultScenario;
                return new ActivityRemovalSafetyCompileRequest(
                    Entry.Contract, actual.Activity, actual.Shell, LocalCanvas, RoleSocket,
                    Traversal, RouteWitness, PatternReport,
                    expectedShellDigest ?? actual.Shell.CanonicalDigest,
                    cueEvidence ?? new[] { actual.Evidence },
                    removalIntent ?? new ActivityOverlayRemovalIntent(OverlayIdentities(actual.Shell)),
                    critical ?? CriticalEvidence(actual));
            }

            public IReadOnlyList<ActivityCriticalTargetEvidence> CriticalEvidence(ActivityScenario scenario)
            {
                ProjectedClusterPort exit;
                Assert.That(RoleSocket.TryGetPrimaryPort(ClusterPortKind.Exit, out exit), Is.True);
                var reward = scenario.Shell.Slots.Single(value =>
                    value.Semantic == ActivitySlotSemanticKind.RewardAnchor);
                var rewardBinding = scenario.Shell.ProgressionBindings.Single(value =>
                    value.Phase == ProgressionPhaseKind.Reward);
                return new[]
                {
                    new ActivityCriticalTargetEvidence(
                        ActivityCriticalTargetKind.MandatoryExit, exit.PortId, exit.SourceCoordinate,
                        exit.RoleAnchorId, RouteWitness.BaselineRoute.ExitNodeId),
                    new ActivityCriticalTargetEvidence(
                        ActivityCriticalTargetKind.Reward, reward.SlotId.Value,
                        reward.SourceCoordinate, rewardBinding.ProgressionNodeId, string.Empty),
                };
            }

            public IReadOnlyList<string> OverlayIdentities(ActivityShellCanvas shell)
            {
                return shell.Zones.Select(value => "ZONE|" + ((int)value.Kind).ToString(CultureInfo.InvariantCulture))
                    .Concat(shell.Slots.Select(value => "SLOT|" + value.SlotId.Value))
                    .Concat(shell.CueBindings.Select(value => "CUE|" +
                        ((int)value.CueKind).ToString(CultureInfo.InvariantCulture) + "|" + value.SlotId.Value))
                    .Concat(shell.MechanismBindings.Select(value =>
                        "MECHANISM|" + value.MechanismNodeId + "|" + value.SlotId.Value))
                    .Concat(shell.ProgressionBindings.Select(value =>
                        "PROGRESSION|" + value.ProgressionNodeId))
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }

            public ActivityRemovalSafety CloneSafety(
                IEnumerable<LocalTileCoord> safePockets = null,
                IEnumerable<LocalTileCoord> recovery = null)
            {
                var source = Activity.RemovalSafety;
                return new ActivityRemovalSafety(
                    source.BaselineSpineVariantId, source.EntryTraversalNodeId,
                    source.ExitTraversalNodeId, safePockets ?? source.SafePocketTiles,
                    recovery ?? source.RecoveryTiles, source.PreserveStaticTraversal,
                    source.PreserveAccessClass, source.PermanentSolidMutationAllowed,
                    source.MandatoryExitDestructionAllowed, source.RouteTypeBeforeRemoval,
                    source.RouteTypeAfterRemoval, source.AccessClassBeforeRemoval,
                    source.AccessClassAfterRemoval, source.TraversalDigestBeforeRemoval,
                    source.TraversalDigestAfterRemoval, source.PermanentSolidWriteTiles);
            }

            public string CaptureUpstream()
            {
                return string.Join("|", new[]
                {
                    SourceDigest, LocalCanvas.CanonicalDigest, RoleSocket.CanonicalDigest,
                    Traversal.CanonicalDigest, RouteWitness.CanonicalDigest,
                    PatternReport.CanonicalDigest, PatternReport.FinalWorkingCanvas.CanonicalDigest,
                    Shell.CanonicalDigest,
                });
            }

            private static TCatalog ImportCatalog<TCatalog>(string importerTypeName)
                where TCatalog : class
            {
                var importerType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType(importerTypeName, false))
                    .FirstOrDefault(type => type != null);
                if (importerType == null)
                    importerType = Assembly.Load("MapAuthoring.Editor").GetType(importerTypeName, true);
                var importer = Activator.CreateInstance(importerType);
                var result = importerType.GetMethod("Import", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(importer, null);
                var resultType = result.GetType();
                if (!(bool)resultType.GetProperty("Success").GetValue(result, null))
                {
                    var errors = ((IEnumerable)resultType.GetProperty("Errors").GetValue(result, null))
                        .Cast<object>().Select(value => value.ToString());
                    Assert.Fail(string.Join("\n", errors));
                }
                var catalog = resultType.GetProperty("Catalog").GetValue(result, null) as TCatalog;
                Assert.That(catalog, Is.Not.Null, importerTypeName);
                return catalog;
            }

            private static ActivityShellCanvas CompileShell(
                TerrainClusterAuthoringEntry entry,
                string sourceDigest,
                TerrainClusterLocalCanvas localCanvas,
                TerrainClusterRoleSocketContract roleSocket,
                TerrainClusterTraversalCompilation traversal,
                TerrainClusterRouteWitnessReport routeWitness,
                TerrainClusterPatternRenderReport pattern,
                ActivityStructureContract activity,
                IEnumerable<ActivityShellZoneDefinition> zones,
                IEnumerable<ActivitySlotProjectionIntent> intents)
            {
                var validation = ActivityContractValidator.Validate(activity, entry.Contract);
                Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
                var result = ActivityShellCompiler.Compile(new ActivityShellCompileRequest(
                    entry.Contract, sourceDigest, activity, validation.CanonicalDigest,
                    localCanvas, localCanvas.CanonicalDigest, roleSocket, roleSocket.CanonicalDigest,
                    traversal, traversal.CanonicalDigest, routeWitness, routeWitness.CanonicalDigest,
                    pattern, pattern.CanonicalDigest, pattern.FinalWorkingCanvas.CanonicalDigest,
                    zones, intents));
                Assert.That(result.IsSuccess, Is.True,
                    string.Join("\n", result.Errors.Select(value => value.ToString())));
                return result.Canvas;
            }

            private static LocalTileCoord[] SelectSlotCoordinates(
                TerrainClusterLocalCanvas canvas,
                TerrainClusterPatternWorkingCanvas working,
                LocalTileCoord cue,
                LocalTileCoord recovery)
            {
                var selected = new List<LocalTileCoord> { cue };
                var candidates = canvas.TileCells.Where(value => value.State == ClusterChunkMaskState.Active)
                    .Select(value => value.SourceCoordinate).Distinct()
                    .OrderBy(value => value.Y).ThenBy(value => value.X).ToArray();
                foreach (var candidate in candidates)
                {
                    if (candidate == recovery || selected.Contains(candidate)) continue;
                    selected.Add(candidate);
                    if (selected.Count == 6) break;
                }
                selected.Add(recovery);
                foreach (var candidate in candidates)
                {
                    if (selected.Contains(candidate)) continue;
                    selected.Add(candidate);
                    if (selected.Count == 9) break;
                }
                Assert.That(selected.Count, Is.EqualTo(9));
                return selected.ToArray();
            }

            private static LocalTileCoord FindOccludedCue(
                TerrainClusterLocalCanvas canvas,
                TerrainClusterPatternWorkingCanvas working,
                LocalTileCoord observationCompiled,
                LocalTileCoord clearCue,
                LocalTileCoord recovery)
            {
                foreach (var cell in working.Cells.Where(value => !value.Solid)
                             .OrderByDescending(value => Math.Abs(value.Coordinate.X - observationCompiled.X) +
                                                         Math.Abs(value.Coordinate.Y - observationCompiled.Y)))
                {
                    LocalTileCoord source;
                    if (!canvas.TryGetSourceTile(cell.Coordinate, out source) ||
                        source == clearCue || source == recovery) continue;
                    if (GridSupercover(observationCompiled, cell.Coordinate)
                        .Any(coordinate =>
                        {
                            TerrainClusterPatternWorkingCell candidate;
                            return working.TryGetCell(coordinate, out candidate) && candidate.Solid;
                        })) return source;
                }
                Assert.Fail("Expected an Air cue coordinate occluded by existing Static Shell Solid cells.");
                return clearCue;
            }

            private static IEnumerable<LocalTileCoord> GridSupercover(LocalTileCoord start, LocalTileCoord end)
            {
                var x = start.X;
                var y = start.Y;
                var dx = end.X - start.X;
                var dy = end.Y - start.Y;
                var nx = Math.Abs(dx);
                var ny = Math.Abs(dy);
                var signX = Math.Sign(dx);
                var signY = Math.Sign(dy);
                var ix = 0;
                var iy = 0;
                yield return new LocalTileCoord(x, y);
                while (ix < nx || iy < ny)
                {
                    var xDecision = (1 + (2 * ix)) * ny;
                    var yDecision = (1 + (2 * iy)) * nx;
                    if (xDecision == yDecision)
                    {
                        x += signX;
                        y += signY;
                        ix++;
                        iy++;
                    }
                    else if (xDecision < yDecision)
                    {
                        x += signX;
                        ix++;
                    }
                    else
                    {
                        y += signY;
                        iy++;
                    }
                    yield return new LocalTileCoord(x, y);
                }
            }

            private static bool IsAir(TerrainClusterPatternWorkingCanvas canvas, LocalTileCoord coordinate)
            {
                TerrainClusterPatternWorkingCell cell;
                return canvas.TryGetCell(coordinate, out cell) && !cell.Solid;
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
                IReadOnlyList<ActivitySlot> slots,
                LocalTileCoord safePocket,
                LocalTileCoord recovery)
            {
                var mechanism = new MechanismGraph(new[]
                {
                    new MechanismNode("MECH_CUE", MechanismNodeKind.CueEmitter, new ActivitySlotId("SLOT_CUE")),
                    new MechanismNode("MECH_TRIGGER", MechanismNodeKind.Trigger, new ActivitySlotId("SLOT_TRIGGER")),
                    new MechanismNode("MECH_DEVICE", MechanismNodeKind.Device, new ActivitySlotId("SLOT_DEVICE")),
                    new MechanismNode("MECH_HAZARD", MechanismNodeKind.Hazard, new ActivitySlotId("SLOT_HAZARD")),
                    new MechanismNode("MECH_PROJECTILE", MechanismNodeKind.ProjectileEmitter, new ActivitySlotId("SLOT_PROJECTILE")),
                    new MechanismNode("MECH_REWARD", MechanismNodeKind.RewardEmitter, new ActivitySlotId("SLOT_REWARD")),
                    new MechanismNode("MECH_RECOVERY", MechanismNodeKind.RecoveryController, new ActivitySlotId("SLOT_RECOVERY")),
                    new MechanismNode("MECH_RESET", MechanismNodeKind.ResetController, new ActivitySlotId("SLOT_RESET")),
                }, new[]
                {
                    new MechanismEdge("MECH_EDGE_TRIGGER_CUE", "MECH_TRIGGER", "MECH_CUE", MechanismRelationKind.Activates),
                    new MechanismEdge("MECH_EDGE_TRIGGER_DEVICE", "MECH_TRIGGER", "MECH_DEVICE", MechanismRelationKind.Activates),
                    new MechanismEdge("MECH_EDGE_DEVICE_HAZARD", "MECH_DEVICE", "MECH_HAZARD", MechanismRelationKind.Drives),
                    new MechanismEdge("MECH_EDGE_DEVICE_PROJECTILE", "MECH_DEVICE", "MECH_PROJECTILE", MechanismRelationKind.Drives),
                    new MechanismEdge("MECH_EDGE_DEVICE_REWARD", "MECH_DEVICE", "MECH_REWARD", MechanismRelationKind.Drives),
                    new MechanismEdge("MECH_EDGE_DEVICE_RECOVERY", "MECH_DEVICE", "MECH_RECOVERY", MechanismRelationKind.Enables),
                    new MechanismEdge("MECH_EDGE_DEVICE_RESET", "MECH_DEVICE", "MECH_RESET", MechanismRelationKind.Enables),
                });
                var progression = new ProgressionGraph("PROG_CUE", "PROG_EXIT", new[]
                {
                    new ProgressionNode("PROG_CUE", ProgressionPhaseKind.Cue),
                    new ProgressionNode("PROG_ACTIVATION", ProgressionPhaseKind.Activation),
                    new ProgressionNode("PROG_CORE", ProgressionPhaseKind.Core),
                    new ProgressionNode("PROG_REWARD", ProgressionPhaseKind.Reward),
                    new ProgressionNode("PROG_RECOVERY", ProgressionPhaseKind.Recovery),
                    new ProgressionNode("PROG_RESET", ProgressionPhaseKind.Reset),
                    new ProgressionNode("PROG_EXIT", ProgressionPhaseKind.Exit),
                }, new[]
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
                    entry.BaselineVariantId, entryRole.TraversalNodeId, exitRole.TraversalNodeId,
                    new[] { safePocket }, new[] { recovery }, true, true, false, false,
                    1, 1, AccessClass.MandatoryNoTool, AccessClass.MandatoryNoTool,
                    contractDigest, contractDigest);
                return new ActivityStructureContract(
                    new ActivityStructureId("ACT_MAP12_CRATER_BOWL"), entry.Id, entry.BaselineVariantId,
                    new[] { PacingRole.Activity, PacingRole.Risk, PacingRole.Recovery },
                    new[] { AccessClass.MandatoryNoTool }, slots,
                    new[] { new ActivityCue(ActivityCueKind.Visual, new ActivitySlotId("SLOT_CUE"), true) },
                    mechanism, progression, safety, "MAP12_02 test-owned Activity fixture");
            }

            private static ActivityStructureContract CloneActivity(
                ActivityStructureContract source,
                IEnumerable<ActivitySlot> slots = null,
                IEnumerable<ActivityCue> cues = null,
                ActivityRemovalSafety safety = null)
            {
                return new ActivityStructureContract(
                    source.Id, source.TerrainClusterId, source.CompatibleSpineVariantId,
                    source.CompatiblePacingRoles, source.CompatibleAccessClasses,
                    slots ?? source.Slots, cues ?? source.Cues, source.MechanismGraph,
                    source.ProgressionGraph, safety ?? source.RemovalSafety, source.DisplayText);
            }

            private static ActivityShellZoneDefinition[] CreateZones(IEnumerable<ActivitySlot> slots)
            {
                var byId = slots.ToDictionary(value => value.Id.Value, value => value.Tile, StringComparer.Ordinal);
                return new[]
                {
                    new ActivityShellZoneDefinition(ActivityShellZoneKind.Cue,
                        new[] { byId["SLOT_CUE"] }),
                    new ActivityShellZoneDefinition(ActivityShellZoneKind.Core, new[]
                    {
                        byId["SLOT_CUE"], byId["SLOT_TRIGGER"], byId["SLOT_DEVICE"],
                        byId["SLOT_HAZARD"], byId["SLOT_PROJECTILE"], byId["SLOT_NPC"],
                    }),
                    new ActivityShellZoneDefinition(ActivityShellZoneKind.Reward,
                        new[] { byId["SLOT_REWARD"] }),
                    new ActivityShellZoneDefinition(ActivityShellZoneKind.Recovery,
                        new[] { byId["SLOT_RECOVERY"], byId["SLOT_RESET"] }),
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
