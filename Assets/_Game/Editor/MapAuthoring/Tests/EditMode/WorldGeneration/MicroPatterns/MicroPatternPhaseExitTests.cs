using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.MapAuthoring.WorldGeneration.Import;
using StarNight.MapAuthoring.WorldGeneration.MicroPatterns;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.MicroPatterns
{
    [TestFixture]
    [Category("MAP10_08")]
    public sealed class MicroPatternPhaseExitTests
    {
        private const string ExpectedCatalogDigest =
            "6a5aefd2eb368348d594158cc3f14e94d0ea509ea2cdd207a7715e8da80d19ac";
        private const string ExpectedCatalogSha =
            "f9d9e9cc60c4e4d7561c5aa6502228c18fc9566e3e0febab206ea3264b408267";
        private const string ExpectedCellsSha =
            "e702ae5d02d7ec9d2cda129c1361699e37d942c280c8f9bd1f3200f155084381";
        private const string ExpectedAuthoringManifest =
            "4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851";
        private const string ProtectedSourceId = "EXIT_TRAVERSAL";

        private static readonly string[] ExpectedPatternIds =
        {
            "MP_CRATER_BOWL",
            "MP_CRATER_BROKEN_SLOPE",
            "MP_CRATER_DUST_PATCH",
            "MP_CRATER_GRIP_RIDGE",
            "MP_CRATER_METEOR_CUE",
            "MP_CRATER_ROCK_SHELF",
            "MP_DOUGH_BOUNCE_CUP",
            "MP_DOUGH_BOUNCE_STRIP",
            "MP_DOUGH_FERMENT_PATCH",
            "MP_DOUGH_RECOVERY_PAD",
            "MP_DOUGH_SOFT_POCKET",
            "MP_DOUGH_STICKY_SHELF",
            "MP_MILL_BEAM_GRIP",
            "MP_MILL_BEAM_OVERHANG",
            "MP_MILL_BROKEN_PILLAR",
            "MP_MILL_GEAR_SOCKET",
            "MP_MILL_ORTHOGONAL_CARVE",
            "MP_MILL_RUST_PATCH",
            "MP_ROOT_ARCH",
            "MP_ROOT_CLIMB_VINES",
            "MP_ROOT_HOLLOW_POCKET",
            "MP_ROOT_SAP_PATCH",
            "MP_ROOT_SPROUT_MARK",
            "MP_ROOT_VERTICAL_TUNNEL",
        };

        [Test]
        public void PhysicalAuthorityHashesInventoryAndImportAreExact()
        {
            var import = Import();
            var catalogPath = FullPath(MicroPatternCsvImporterV2.CatalogProjectRelativePath);
            var cellsPath = FullPath(MicroPatternCsvImporterV2.CellsProjectRelativePath);

            Assert.That(import.Published, Is.True);
            Assert.That(import.IsHeaderOnly, Is.False);
            Assert.That(import.Catalog.Count, Is.EqualTo(24));
            Assert.That(import.Catalog.Definitions.Select(value => value.Id.Value),
                Is.EqualTo(ExpectedPatternIds));
            Assert.That(import.StableDigest, Is.EqualTo(ExpectedCatalogDigest));
            Assert.That(Sha256(File.ReadAllBytes(catalogPath)), Is.EqualTo(ExpectedCatalogSha));
            Assert.That(Sha256(File.ReadAllBytes(cellsPath)), Is.EqualTo(ExpectedCellsSha));
            Assert.That(DataRowCount(catalogPath), Is.EqualTo(24));
            Assert.That(DataRowCount(cellsPath), Is.EqualTo(453));

            var authoringRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var csvFiles = Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories);
            Assert.That(csvFiles, Has.Length.EqualTo(52));
            Assert.That(ComputeManifest(authoringRoot, csvFiles), Is.EqualTo(ExpectedAuthoringManifest));
            Assert.That(GeneratedCsvFiles(), Is.Empty);
        }

        [Test]
        public void InMemoryCanonicalProjectionRoundTripIsLossless()
        {
            var physical = Import().Catalog;
            var catalogRows = ProjectCatalogRows(physical.Definitions);
            var cellRows = ProjectCellRows(physical.Definitions);

            Assert.That(catalogRows, Has.Length.EqualTo(24));
            Assert.That(cellRows, Has.Length.EqualTo(453));
            Assert.That(catalogRows.Select(value => value.PatternId),
                Is.EqualTo(ExpectedPatternIds));

            var builder = new MicroPatternCellSchemaBuilder();
            var forward = builder.Build(catalogRows, cellRows);
            var reverse = builder.Build(catalogRows.Reverse(), cellRows.Reverse());
            Assert.That(forward.Success, Is.True, SchemaErrors(forward));
            Assert.That(forward.Published, Is.True);
            Assert.That(reverse.Success, Is.True, SchemaErrors(reverse));
            Assert.That(forward.StableDigest, Is.EqualTo(ExpectedCatalogDigest));
            Assert.That(reverse.StableDigest, Is.EqualTo(forward.StableDigest));
            Assert.That(forward.Catalog.Definitions.Select(value => value.ComputeStableDigest()),
                Is.EqualTo(physical.Definitions.Select(value => value.ComputeStableDigest())));
        }

        [Test]
        public void ExactCellLayerContentTotalsAndTransformMassAreApproved()
        {
            var definitions = Import().Catalog.Definitions;
            foreach (var definition in definitions)
            {
                Assert.That(definition.Width, Is.EqualTo(4), definition.Id.Value);
                Assert.That(definition.Height, Is.EqualTo(4), definition.Id.Value);
                Assert.That(definition.Cells, Has.Count.EqualTo(16), definition.Id.Value);
                Assert.That(definition.Cells.Select(value => value.Coordinate).Distinct().Count(),
                    Is.EqualTo(16), definition.Id.Value);
                Assert.That(definition.Cells.All(value =>
                    value.Coordinate.X >= 0 && value.Coordinate.X < 4 &&
                    value.Coordinate.Y >= 0 && value.Coordinate.Y < 4), Is.True);
                Assert.That(definition.Cells.All(value =>
                    value.Instructions.Count == 6 &&
                    value.Instructions.Select(item => item.Layer).Distinct().Count() == 6), Is.True);
                Assert.That(definition.Weight * definition.AllowedTransforms.Count,
                    Is.EqualTo(1000), definition.Id.Value);
                Assert.That(MicroPatternValidator.Validate(definition).IsValid,
                    Is.True, definition.Id.Value);
            }

            var instructions = definitions.SelectMany(value => value.Cells)
                .SelectMany(value => value.Instructions).ToArray();
            var geometry = instructions.Where(value => value.Layer == MicroPatternLayer.Geometry)
                .ToArray();
            Assert.That(geometry.Count(value => value.Operation == MicroPatternOperation.AddSolid),
                Is.EqualTo(54));
            Assert.That(geometry.Count(value => value.Operation == MicroPatternOperation.CarveAir),
                Is.EqualTo(41));
            Assert.That(geometry.Count(value => value.Operation == MicroPatternOperation.NoChange),
                Is.EqualTo(289));
            Assert.That(instructions.Count(value =>
                value.Operation != MicroPatternOperation.NoChange), Is.EqualTo(164));
            Assert.That(instructions.Where(value => !string.IsNullOrEmpty(value.PayloadId))
                .Select(value => value.PayloadId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(24));
            Assert.That(instructions.Where(value =>
                value.Operation == MicroPatternOperation.NoChange ||
                value.Operation == MicroPatternOperation.AddSolid ||
                value.Operation == MicroPatternOperation.CarveAir)
                .All(value => string.IsNullOrEmpty(value.PayloadId)), Is.True);

            var biomeCounts = definitions.GroupBy(value => value.AllowedBiomes.Single().CanonicalId)
                .Select(value => value.Count()).ToArray();
            Assert.That(biomeCounts, Is.All.EqualTo(6));
            var roles = definitions.GroupBy(MicroPatternPreviewModel.GetRoleGroup)
                .ToDictionary(value => value.Key, value => value.Count());
            Assert.That(roles[MicroPatternPreviewRoleGroup.Geometry], Is.EqualTo(12));
            Assert.That(roles[MicroPatternPreviewRoleGroup.SurfaceAffordance], Is.EqualTo(4));
            Assert.That(roles[MicroPatternPreviewRoleGroup.Detail], Is.EqualTo(8));
        }

        [Test]
        public void AllFiftySixTransformsAreValidBoundedAndSelfInverse()
        {
            var definitions = Import().Catalog.Definitions;
            var pairCount = 0;
            foreach (var definition in definitions)
            {
                foreach (var transform in definition.AllowedTransforms)
                {
                    var first = MicroPatternTransformer.Transform(definition, transform);
                    Assert.That(first.Success, Is.True, TransformErrors(first));
                    Assert.That(first.Pattern.Cells, Has.Count.EqualTo(16));
                    Assert.That(first.Pattern.Cells.Select(value => value.Coordinate).Distinct().Count(),
                        Is.EqualTo(16));
                    Assert.That(first.Pattern.Cells.All(value =>
                        value.Coordinate.X >= 0 && value.Coordinate.X < 4 &&
                        value.Coordinate.Y >= 0 && value.Coordinate.Y < 4), Is.True);

                    var transformedDefinition = new MicroPatternDefinition(
                        definition.Id, 4, 4, first.Pattern.Cells, definition.Weight,
                        definition.AllowedBiomes, definition.AllowedTransforms,
                        definition.ProtectedPolicy, definition.DisplayId);
                    var second = MicroPatternTransformer.Transform(transformedDefinition, transform);
                    Assert.That(second.Success, Is.True, TransformErrors(second));
                    Assert.That(CellEvidence(second.Pattern.Cells),
                        Is.EqualTo(CellEvidence(definition.Cells)), Pair(definition, transform));
                    if (transform == MicroPatternTransform.R0)
                    {
                        Assert.That(CellEvidence(first.Pattern.Cells),
                            Is.EqualTo(CellEvidence(definition.Cells)), definition.Id.Value);
                    }
                    pairCount++;
                }
            }
            Assert.That(pairCount, Is.EqualTo(56));
        }

        [Test]
        public void AllTwentyFourProtectedOutcomesHaveZeroProtectedWrites()
        {
            var definitions = Import().Catalog.Definitions;
            var rejected = 0;
            var forced = 0;
            foreach (var definition in definitions)
            {
                var transformed = MicroPatternTransformer.Transform(definition, MicroPatternTransform.R0);
                Assert.That(transformed.Success, Is.True, TransformErrors(transformed));
                var target = transformed.Pattern.Cells
                    .Where(value => value.Instructions.Any(item =>
                        item.Operation != MicroPatternOperation.NoChange))
                    .OrderBy(value => value.Coordinate.Y)
                    .ThenBy(value => value.Coordinate.X)
                    .First().Coordinate;
                var protection = new MicroPatternProtectedCell(
                    target, MicroPatternProtectedSourceKind.TraversalEnvelope, ProtectedSourceId);
                var result = MicroPatternApplicationPlanner.Plan(
                    transformed.Pattern,
                    new MicroPatternPlacement(new LocalTileCoord(0, 0)),
                    new[] { protection });

                if (definition.ProtectedPolicy == MicroPatternProtectedPolicy.RejectCandidate)
                {
                    Assert.That(result.Success, Is.False, definition.Id.Value);
                    Assert.That(result.Plan, Is.Null);
                    Assert.That(result.StableDigest, Is.Empty);
                    Assert.That(result.RejectedHits, Is.Not.Empty);
                    Assert.That(result.Errors.Any(value =>
                        value.Code == MicroPatternApplicationErrorCode.ProtectedWriteRejected), Is.True);
                    rejected++;
                }
                else
                {
                    Assert.That(result.Success, Is.True, ApplicationErrors(result));
                    Assert.That(result.Plan.ProtectedHits, Is.Not.Empty);
                    var prepared = result.Plan.Cells.Single(value =>
                        value.TargetCoordinate.Equals(target));
                    Assert.That(prepared.Instructions.All(value =>
                        value.Operation == MicroPatternOperation.NoChange), Is.True);
                    Assert.That(result.Plan.ProtectedMask.Entries.Single().Provenance,
                        Does.Contain(protection));
                    Assert.That(result.Plan.Cells.Where(value => value.TargetCoordinate.Equals(target))
                        .SelectMany(value => value.Instructions)
                        .Count(value => value.Operation != MicroPatternOperation.NoChange), Is.Zero);
                    forced++;
                }
            }
            Assert.That(rejected, Is.EqualTo(12));
            Assert.That(forced, Is.EqualTo(12));
        }

        [Test]
        public void FourBiomeCandidateMassIndexAndReversalAreDeterministic()
        {
            var definitions = Import().Catalog.Definitions;
            var profiles = MicroPatternBiomeProfileCatalog.CreateBuiltIn();
            Assert.That(profiles.Profiles.Select(value => value.Biome.CanonicalId), Is.EqualTo(new[]
            {
                "MoonCrater", "CassiaRoot", "AbandonedMill", "MoonDough",
            }));
            Assert.That(profiles.Profiles.All(value =>
                value.DensityPolicy == MicroPatternDensityPolicy.Uncalibrated), Is.True);

            var candidateCount = 0;
            foreach (var biomeGroup in definitions.GroupBy(value => value.AllowedBiomes.Single()))
            {
                var sources = BuildSources(biomeGroup).ToArray();
                var forward = MicroPatternCandidateIndexBuilder.Build(
                    profiles, biomeGroup.Key, sources);
                var reverse = MicroPatternCandidateIndexBuilder.Build(
                    profiles, biomeGroup.Key, sources.Reverse());
                Assert.That(forward.Published, Is.True);
                Assert.That(forward.Rejections, Is.Empty, CandidateErrors(forward));
                Assert.That(reverse.Published, Is.True);
                Assert.That(reverse.Rejections, Is.Empty, CandidateErrors(reverse));
                Assert.That(reverse.Index.StableDigest, Is.EqualTo(forward.Index.StableDigest));
                Assert.That(forward.Index.Candidates.Select(value => value.Key.PatternId.Value)
                    .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(6));
                Assert.That(forward.Index.Candidates.GroupBy(value => value.Key.PatternId)
                    .All(group => group.Sum(value => value.Weight) == 1000), Is.True);
                candidateCount += forward.Index.Candidates.Count;
            }
            Assert.That(candidateCount, Is.EqualTo(56));
        }

        [Test]
        public void DeterministicRngRepeatsChangesInputsAndRejectsWithoutDraw()
        {
            var definitions = Import().Catalog.Definitions;
            var indexes = BuildIndexes(definitions);
            var rngDefinitions = CreateRngDefinitionSet();
            var selector = new MicroPatternDeterministicSelector(rngDefinitions);
            var request = SelectionRequest("MPS_EXIT", indexes["MoonCrater"]);
            var baseline = selector.Select(0x123456789ABCDEF0UL,
                new SectorCoord(2, 3), 4, new[] { request });
            var repeated = selector.Select(0x123456789ABCDEF0UL,
                new SectorCoord(2, 3), 4, new[] { request });
            var seedChanged = selector.Select(0x123456789ABCDEF1UL,
                new SectorCoord(2, 3), 4, new[] { request });
            var sectorChanged = selector.Select(0x123456789ABCDEF0UL,
                new SectorCoord(2, 4), 4, new[] { request });
            var attemptChanged = selector.Select(0x123456789ABCDEF0UL,
                new SectorCoord(2, 3), 5, new[] { request });
            var indexChanged = selector.Select(0x123456789ABCDEF0UL,
                new SectorCoord(2, 3), 4,
                new[] { SelectionRequest("MPS_EXIT", indexes["CassiaRoot"]) });

            Assert.That(baseline.Success, Is.True, SelectionErrors(baseline));
            Assert.That(repeated.StableDigest, Is.EqualTo(baseline.StableDigest));
            Assert.That(DecisionEvidence(repeated.Decisions.Single()),
                Is.EqualTo(DecisionEvidence(baseline.Decisions.Single())));
            Assert.That(seedChanged.StableDigest, Is.Not.EqualTo(baseline.StableDigest));
            Assert.That(sectorChanged.StableDigest, Is.Not.EqualTo(baseline.StableDigest));
            Assert.That(attemptChanged.StableDigest, Is.Not.EqualTo(baseline.StableDigest));
            Assert.That(indexChanged.StableDigest, Is.Not.EqualTo(baseline.StableDigest));

            var routeFactory = new DeterministicRngStreamFactory(rngDefinitions);
            var route = routeFactory.Create(WorldGenerationRngStreams.RouteStreamId,
                99UL, RngStreamScope.Pass("PASS_EXIT"));
            var routeExpected = routeFactory.Create(WorldGenerationRngStreams.RouteStreamId,
                99UL, RngStreamScope.Pass("PASS_EXIT"));
            selector.Select(99UL, new SectorCoord(1, 1), 0, new[] { request });
            Assert.That(route.DrawCount, Is.Zero);
            Assert.That(route.NextUInt64(), Is.EqualTo(routeExpected.NextUInt64()));

            var profiles = MicroPatternBiomeProfileCatalog.CreateBuiltIn();
            var emptyIndex = MicroPatternCandidateIndexBuilder.Build(
                profiles, MoonpalaceBiomeId.MoonCrater,
                Array.Empty<MicroPatternCandidateSource>()).Index;
            var empty = selector.Select(1UL, new SectorCoord(0, 0), 0,
                new[] { SelectionRequest("MPS_EMPTY", emptyIndex) });
            var invalid = selector.Select(1UL, new SectorCoord(0, 0), 0,
                new[] { SelectionRequest("bad", indexes["MoonCrater"]) });
            AssertRejectedWithoutDraw(empty, MicroPatternSelectionBatchErrorCode.EmptyCandidateIndex);
            AssertRejectedWithoutDraw(invalid, MicroPatternSelectionBatchErrorCode.InvalidRequestId);
        }

        [Test]
        public void AllFiftySixOrderedRendersPublishExactChangedDiffs()
        {
            var definitions = Import().Catalog.Definitions;
            var pairCount = 0;
            foreach (var definition in definitions)
            {
                foreach (var transform in definition.AllowedTransforms)
                {
                    var plan = Plan(definition, transform);
                    var target = WitnessTarget(new[] { plan });
                    var render = MicroPatternOrderedRenderer.Render(
                        new[]
                        {
                            new MicroPatternRenderRequest(
                                new MicroPatternRenderRequestId("MPR_EXIT_SINGLE"), plan),
                        }, target);
                    Assert.That(render.Success, Is.True, RenderErrors(render));
                    var expectedWrites = plan.Cells.SelectMany(value => value.Instructions)
                        .Count(value => value.Operation != MicroPatternOperation.NoChange);
                    Assert.That(render.Delta.Writes, Has.Count.EqualTo(expectedWrites),
                        Pair(definition, transform));
                    Assert.That(render.Delta.Writes.Select(value => (int)value.Stage), Is.Ordered);
                    Assert.That(render.Delta.Writes.All(value =>
                        (int)value.Stage == ExpectedStage(value.Layer)), Is.True);
                    Assert.That(render.Delta.Cells.SelectMany(value => value.Writes)
                        .All(write => !string.Equals(
                            render.Delta.Cells.Single(cell =>
                                cell.TargetCoordinate.Equals(write.TargetCoordinate))
                                .Before.GetSemanticValue(write.Layer),
                            render.Delta.Cells.Single(cell =>
                                cell.TargetCoordinate.Equals(write.TargetCoordinate))
                                .After.GetSemanticValue(write.Layer),
                            StringComparison.Ordinal)), Is.True);
                    foreach (var cell in render.Delta.Cells)
                    {
                        var written = new HashSet<MicroPatternLayer>(cell.Writes.Select(value => value.Layer));
                        foreach (MicroPatternLayer layer in Enum.GetValues(typeof(MicroPatternLayer)))
                        {
                            if (written.Contains(layer)) continue;
                            Assert.That(cell.After.GetSemanticValue(layer),
                                Is.EqualTo(cell.Before.GetSemanticValue(layer)),
                                Pair(definition, transform) + "|" + layer);
                        }
                    }
                    pairCount++;
                }
            }
            Assert.That(pairCount, Is.EqualTo(56));
        }

        [Test]
        public void SameLayerMaterialConflictIsAtomic()
        {
            var catalog = Import().Catalog;
            var first = catalog.Definitions.Single(value =>
                value.Id.Value == MicroPatternPreviewModel.ConflictFirstPatternId);
            var second = catalog.Definitions.Single(value =>
                value.Id.Value == MicroPatternPreviewModel.ConflictSecondPatternId);
            var firstPlan = Plan(first, MicroPatternTransform.R0);
            var secondPlan = Plan(second, MicroPatternTransform.R0);
            var target = WitnessTarget(new[] { firstPlan, secondPlan });
            var result = MicroPatternOrderedRenderer.Render(new[]
            {
                new MicroPatternRenderRequest(
                    new MicroPatternRenderRequestId("MPR_EXIT_ROOT"), secondPlan),
                new MicroPatternRenderRequest(
                    new MicroPatternRenderRequestId("MPR_EXIT_CRATER"), firstPlan),
            }, target);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Delta, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(result.Conflicts.Any(value =>
                value.Layer == MicroPatternLayer.Material), Is.True);
            Assert.That(result.Errors.Any(value =>
                value.Code == MicroPatternRenderErrorCode.ConflictingLayerWrite), Is.True);
            Assert.That(result.Errors.Any(value =>
                value.Code == MicroPatternRenderErrorCode.AtomicRenderRejected), Is.True);
            Assert.That(target.Cells.All(value =>
                string.IsNullOrEmpty(value.MaterialId)), Is.True);
        }

        [Test]
        public void SignaturesAndThirdRepeatFilteringIntegrateBeforeRng()
        {
            var definitions = Import().Catalog.Definitions;
            var zero = 0;
            var nonZero = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                string canonical = null;
                foreach (var transform in definition.AllowedTransforms)
                {
                    var result = MicroPatternSilhouetteSignatureBuilder.Build(Plan(definition, transform));
                    Assert.That(result.Success, Is.True, SignatureErrors(result));
                    if (canonical == null) canonical = result.StableDigest;
                    Assert.That(result.StableDigest, Is.EqualTo(canonical), Pair(definition, transform));
                }
                var r0 = MicroPatternSilhouetteSignatureBuilder.Build(
                    Plan(definition, MicroPatternTransform.R0)).Signature;
                if (r0.AddSolidMask == 0 && r0.CarveAirMask == 0) zero++;
                else nonZero.Add(r0.StableDigest);
            }
            Assert.That(zero, Is.EqualTo(12));
            Assert.That(nonZero, Has.Count.EqualTo(12));

            var repeated = definitions.Single(value =>
                value.Id.Value == "MP_CRATER_BROKEN_SLOPE");
            var historySignature = MicroPatternSilhouetteSignatureBuilder.Build(
                Plan(repeated, MicroPatternTransform.R0)).Signature;
            var craterSources = BuildSources(definitions.Where(value =>
                value.AllowedBiomes.Single() == MoonpalaceBiomeId.MoonCrater)).ToArray();
            var guarded = MicroPatternThirdRepeatGuard.Filter(
                new MicroPatternRepetitionContext(new[]
                {
                    new MicroPatternAcceptedHistoryItem(1, "MPP_EXIT_1", repeated.Id, historySignature),
                    new MicroPatternAcceptedHistoryItem(2, "MPP_EXIT_2", repeated.Id, historySignature),
                }), craterSources);
            Assert.That(guarded.Success, Is.True, RepetitionErrors(guarded));
            Assert.That(guarded.Exclusions, Has.Count.EqualTo(repeated.AllowedTransforms.Count));
            Assert.That(guarded.AllowedSources.All(value =>
                value.Definition.Id != repeated.Id), Is.True);

            var built = MicroPatternCandidateIndexBuilder.Build(
                MicroPatternBiomeProfileCatalog.CreateBuiltIn(),
                MoonpalaceBiomeId.MoonCrater, guarded.AllowedSources);
            Assert.That(built.Published, Is.True);
            Assert.That(built.Rejections, Is.Empty, CandidateErrors(built));
            var selected = new MicroPatternDeterministicSelector(CreateRngDefinitionSet()).Select(
                0x55AA55AAUL, new SectorCoord(3, 4), 0,
                new[] { SelectionRequest("MPS_GUARDED", built.Index) });
            Assert.That(selected.Success, Is.True, SelectionErrors(selected));
            Assert.That(selected.Decisions, Has.Count.EqualTo(1));
            Assert.That(selected.Decisions.Single().ChosenKey.PatternId, Is.Not.EqualTo(repeated.Id));
            Assert.That(selected.Decisions.Single().DrawCountAfter -
                selected.Decisions.Single().DrawCountBefore, Is.EqualTo(1UL));
        }

        [Test]
        public void LocalCleanupRulesAreBoundedProtectedAndNonCascading()
        {
            AssertCleanupDelta(MicroPatternLocalCleanup.Evaluate(Snapshot(
                Cell(0, 0, true, true), Cell(0, 1, false), Cell(0, -1, false),
                Cell(-1, 0, false), Cell(1, 0, false))),
                false, MicroPatternCleanupRule.SolidSpeck);
            AssertCleanupDelta(MicroPatternLocalCleanup.Evaluate(Snapshot(
                Cell(0, 0, false, true), Cell(0, 1, true), Cell(0, -1, true),
                Cell(-1, 0, true), Cell(1, 0, true))),
                true, MicroPatternCleanupRule.AirPinhole);
            AssertCleanupDelta(MicroPatternLocalCleanup.Evaluate(SixNeighborSnapshot(
                true, true, true, true, false, false, false)),
                false, MicroPatternCleanupRule.HeadSnag);
            AssertCleanupDelta(MicroPatternLocalCleanup.Evaluate(SixNeighborSnapshot(
                false, false, true, true, true, true, true)),
                true, MicroPatternCleanupRule.BoxedBottomPit);

            var coordinate = new LocalTileCoord(0, 0);
            var protection = new MicroPatternProtectedCell(
                coordinate, MicroPatternProtectedSourceKind.TraversalEnvelope, ProtectedSourceId);
            var protectedResult = MicroPatternLocalCleanup.Evaluate(Snapshot(
                new MicroPatternCleanupCell(coordinate, true, true, true, new[] { protection }),
                Cell(0, 1, false), Cell(0, -1, false),
                Cell(-1, 0, false), Cell(1, 0, false)));
            Assert.That(protectedResult.Success, Is.True, CleanupErrors(protectedResult));
            Assert.That(protectedResult.Delta.Cells, Is.Empty);
            var blocked = protectedResult.Issues.Single(value =>
                value.Code == MicroPatternCleanupIssueCode.ProtectedWriteBlocked);
            Assert.That(blocked.ProtectionProvenance, Does.Contain(protection));

            var incompleteSnapshot = Snapshot(
                Cell(0, 0, true, true), Cell(0, 1, false),
                Cell(0, -1, false), Cell(-1, 0, false));
            var first = MicroPatternLocalCleanup.Evaluate(incompleteSnapshot);
            var second = MicroPatternLocalCleanup.Evaluate(incompleteSnapshot);
            Assert.That(first.Success, Is.True, CleanupErrors(first));
            Assert.That(first.Delta.Cells, Is.Empty);
            Assert.That(first.Issues.Any(value =>
                value.Code == MicroPatternCleanupIssueCode.InsufficientNeighborhood), Is.True);
            Assert.That(second.StableDigest, Is.EqualTo(first.StableDigest));
            Assert.That(incompleteSnapshot.Cells.Single(value => value.IsOwned).Solid, Is.True);

            var source = File.ReadAllText(FullPath(
                "Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternLocalCleanup.cs"));
            foreach (var forbidden in new[]
            {
                "Reachability", "Pathfinding", "Tilemap", "SceneManager", "MicroPatternOrderedRenderer",
            })
            {
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
            }
        }

        [Test]
        public void PreviewEvidenceIsCompleteAndSideEffectFree()
        {
            var authoringRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var authoringBefore = ComputeManifest(authoringRoot,
                Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories));
            var generatedBefore = GeneratedCsvFiles();
            var assetPathsBefore = AssetPathDigest();
            var scene = EditorSceneManager.GetActiveScene();
            var dirtyBefore = scene.isDirty;
            var rootsBefore = scene.GetRootGameObjects().Length;

            var catalog = Import().Catalog;
            var model = new MicroPatternPreviewModel();
            var cleanCount = 0;
            foreach (var definition in catalog.Definitions)
            {
                foreach (var transform in definition.AllowedTransforms)
                {
                    var result = model.Build(new MicroPatternPreviewRequest(
                        definition.Id.Value, transform,
                        MicroPatternPreviewFixtureKind.Clean), catalog);
                    Assert.That(result.Success, Is.True, PreviewErrors(result));
                    AssertPreviewPanels(result.Snapshot);
                    Assert.That(result.Snapshot.Writes, Has.Count.EqualTo(result.Snapshot.Diffs.Count));
                    Assert.That(result.Snapshot.Diffs.All(value => value.Changed), Is.True);
                    Assert.That(result.Snapshot.StableDigest, Does.Match("^[0-9a-f]{64}$"));
                    cleanCount++;
                }
                var protectedResult = model.Build(new MicroPatternPreviewRequest(
                    definition.Id.Value, MicroPatternTransform.R0,
                    MicroPatternPreviewFixtureKind.ProtectedOverlap), catalog);
                Assert.That(protectedResult.Success, Is.True, PreviewErrors(protectedResult));
                Assert.That(protectedResult.Snapshot.ProtectedHitCount, Is.GreaterThan(0));
            }
            Assert.That(cleanCount, Is.EqualTo(56));

            var conflict = model.Build(new MicroPatternPreviewRequest(
                MicroPatternPreviewModel.ConflictFirstPatternId,
                MicroPatternTransform.R0,
                MicroPatternPreviewFixtureKind.SameLayerConflict), catalog);
            Assert.That(conflict.Success, Is.True, PreviewErrors(conflict));
            Assert.That(conflict.Snapshot.RenderPublished, Is.False);
            Assert.That(conflict.Snapshot.ConflictEvidence, Is.Not.Empty);
            Assert.That(conflict.Snapshot.Writes, Is.Empty);
            Assert.That(conflict.Snapshot.Diffs, Is.Empty);

            MicroPatternPreviewWindow window = null;
            try
            {
                window = MicroPatternPreviewWindow.Open();
                Assert.That(window.Reload(), Is.True, window.LastError);
                Assert.That(window.PatternIds.Count, Is.EqualTo(24));
                Assert.That(window.PanelCount, Is.EqualTo(5));
                AssertPreviewPanels(window.CurrentSnapshot);
                window.Repaint();
            }
            finally
            {
                if (window != null) window.Close();
            }

            Assert.That(ComputeManifest(authoringRoot,
                Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories)),
                Is.EqualTo(authoringBefore));
            Assert.That(GeneratedCsvFiles(), Is.EqualTo(generatedBefore));
            Assert.That(AssetPathDigest(), Is.EqualTo(assetPathsBefore));
            Assert.That(EditorSceneManager.GetActiveScene().isDirty, Is.EqualTo(dirtyBefore));
            Assert.That(EditorSceneManager.GetActiveScene().GetRootGameObjects(),
                Has.Length.EqualTo(rootsBefore));
        }

        private static MicroPatternCsvImportResult Import()
        {
            var result = new MicroPatternCsvImporterV2().Import();
            Assert.That(result.Success, Is.True, ImportErrors(result));
            return result;
        }

        private static MicroPatternCatalogRowV2[] ProjectCatalogRows(
            IEnumerable<MicroPatternDefinition> definitions)
        {
            return definitions.OrderBy(value => value.Id.Value, StringComparer.Ordinal)
                .Select((definition, index) => new MicroPatternCatalogRowV2(
                    definition.Id.Value,
                    definition.Weight.ToString(CultureInfo.InvariantCulture),
                    string.Join("|", definition.AllowedBiomes.Select(value => value.CanonicalId)),
                    string.Join("|", definition.AllowedTransforms.Select(TransformToken)),
                    definition.ProtectedPolicy == MicroPatternProtectedPolicy.RejectCandidate
                        ? "REJECT_CANDIDATE"
                        : "FORCE_NO_CHANGE",
                    "in-memory-exit-catalog",
                    index + 2))
                .ToArray();
        }

        private static MicroPatternCellRowV2[] ProjectCellRows(
            IEnumerable<MicroPatternDefinition> definitions)
        {
            var rows = new List<MicroPatternCellRowV2>();
            var record = 2;
            foreach (var definition in definitions.OrderBy(value => value.Id.Value, StringComparer.Ordinal))
            {
                foreach (var cell in definition.Cells.OrderBy(value => value.Coordinate.Y)
                             .ThenBy(value => value.Coordinate.X))
                {
                    foreach (var instruction in cell.Instructions.OrderBy(value => (int)value.Layer))
                    {
                        if (instruction.Layer != MicroPatternLayer.Geometry &&
                            instruction.Operation == MicroPatternOperation.NoChange) continue;
                        rows.Add(new MicroPatternCellRowV2(
                            definition.Id.Value,
                            cell.Coordinate.X.ToString(CultureInfo.InvariantCulture),
                            cell.Coordinate.Y.ToString(CultureInfo.InvariantCulture),
                            MicroPatternCellTokenCodec.ToOperationToken(instruction.Operation),
                            MicroPatternCellTokenCodec.ToLayerToken(instruction.Layer),
                            instruction.PayloadId,
                            "in-memory-exit-cells",
                            record++));
                    }
                }
            }
            return rows.ToArray();
        }

        private static IEnumerable<MicroPatternCandidateSource> BuildSources(
            IEnumerable<MicroPatternDefinition> definitions)
        {
            foreach (var definition in definitions.OrderBy(value => value.Id.Value, StringComparer.Ordinal))
            {
                foreach (var transform in definition.AllowedTransforms)
                {
                    yield return new MicroPatternCandidateSource(
                        definition, transform, Plan(definition, transform));
                }
            }
        }

        private static Dictionary<string, MicroPatternCandidateIndex> BuildIndexes(
            IEnumerable<MicroPatternDefinition> definitions)
        {
            var profiles = MicroPatternBiomeProfileCatalog.CreateBuiltIn();
            return definitions.GroupBy(value => value.AllowedBiomes.Single())
                .ToDictionary(
                    group => group.Key.CanonicalId,
                    group =>
                    {
                        var result = MicroPatternCandidateIndexBuilder.Build(
                            profiles, group.Key, BuildSources(group));
                        Assert.That(result.Published, Is.True);
                        Assert.That(result.Rejections, Is.Empty, CandidateErrors(result));
                        return result.Index;
                    },
                    StringComparer.Ordinal);
        }

        private static MicroPatternApplicationPlan Plan(
            MicroPatternDefinition definition,
            MicroPatternTransform transform)
        {
            var transformed = MicroPatternTransformer.Transform(definition, transform);
            Assert.That(transformed.Success, Is.True, TransformErrors(transformed));
            var result = MicroPatternApplicationPlanner.Plan(
                transformed.Pattern,
                new MicroPatternPlacement(new LocalTileCoord(0, 0)),
                Array.Empty<MicroPatternProtectedCell>());
            Assert.That(result.Success, Is.True, ApplicationErrors(result));
            return result.Plan;
        }

        private static MicroPatternRenderTarget WitnessTarget(
            IEnumerable<MicroPatternApplicationPlan> plans)
        {
            var copy = plans.ToArray();
            var cells = copy.SelectMany(value => value.Cells)
                .GroupBy(value => value.TargetCoordinate)
                .OrderBy(value => value.Key.Y)
                .ThenBy(value => value.Key.X)
                .Select(group => new MicroPatternRenderCellState(
                    group.Key,
                    group.SelectMany(value => value.Instructions).Any(value =>
                        value.Layer == MicroPatternLayer.Geometry &&
                        value.Operation == MicroPatternOperation.CarveAir),
                    string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty))
                .ToArray();
            return new MicroPatternRenderTarget(cells);
        }

        private static WorldRouteDefinitionSet CreateRngDefinitionSet()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                {
                    WorldGenerationRngStreams.SectorRecipeStreamId,
                    CreateRngDefinition(WorldGenerationRngStreams.SectorRecipeStreamId,
                        "E9931A70C2D520F4", "SECTOR")
                },
                {
                    WorldGenerationRngStreams.RouteStreamId,
                    CreateRngDefinition(WorldGenerationRngStreams.RouteStreamId,
                        "C00FEE12AB341901", "PASS")
                },
            };
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(
                typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams",
                new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return set;
        }

        private static RngStreamDefinition CreateRngDefinition(
            string id, string saltHex, string resetScope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(
                typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(saltHex));
            SetAutoProperty(definition, "ResetScope", resetScope);
            SetAutoProperty(definition, "DescriptionKo", "MAP10 Exit test");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2)
                .Select(index => byte.Parse(value.Substring(index * 2, 2),
                    NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var field = target.GetType().GetField(
                "<" + propertyName + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, propertyName);
            field.SetValue(target, value);
        }

        private static MicroPatternSelectionRequest SelectionRequest(
            string id, MicroPatternCandidateIndex index)
        {
            return new MicroPatternSelectionRequest(
                new MicroPatternSelectionRequestId(id), index);
        }

        private static void AssertRejectedWithoutDraw(
            MicroPatternSelectionBatchResult result,
            MicroPatternSelectionBatchErrorCode expected)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StreamCreated, Is.False);
            Assert.That(result.FinalDrawCount, Is.Zero);
            Assert.That(result.Decisions, Is.Empty);
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(result.Errors.Any(value => value.Code == expected), Is.True);
        }

        private static void AssertCleanupDelta(
            MicroPatternLocalCleanupResult result,
            bool expectedAfter,
            MicroPatternCleanupRule expectedRule)
        {
            Assert.That(result.Success, Is.True, CleanupErrors(result));
            Assert.That(result.Delta.Cells, Has.Count.EqualTo(1));
            var delta = result.Delta.Cells.Single();
            Assert.That(delta.AfterSolid, Is.EqualTo(expectedAfter));
            Assert.That(delta.BeforeSolid, Is.Not.EqualTo(expectedAfter));
            Assert.That(delta.Rules, Does.Contain(expectedRule));
            Assert.That(result.StableDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static MicroPatternCleanupSnapshot Snapshot(params MicroPatternCleanupCell[] cells)
        {
            return new MicroPatternCleanupSnapshot(cells);
        }

        private static MicroPatternCleanupCell Cell(
            int x, int y, bool solid, bool owned = false)
        {
            return new MicroPatternCleanupCell(
                new LocalTileCoord(x, y), solid, owned, false);
        }

        private static MicroPatternCleanupSnapshot SixNeighborSnapshot(
            bool center, bool up, bool upLeft, bool upRight,
            bool left, bool right, bool down)
        {
            return Snapshot(
                Cell(0, 0, center, true),
                Cell(0, 1, up), Cell(-1, 1, upLeft), Cell(1, 1, upRight),
                Cell(-1, 0, left), Cell(1, 0, right), Cell(0, -1, down));
        }

        private static void AssertPreviewPanels(MicroPatternPreviewSnapshot snapshot)
        {
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.PanelCount, Is.EqualTo(5));
            Assert.That(snapshot.OriginalCells, Has.Count.EqualTo(16));
            Assert.That(snapshot.TransformedCells, Has.Count.EqualTo(16));
            Assert.That(snapshot.ProtectedEffectiveCells, Has.Count.EqualTo(16));
            Assert.That(snapshot.BeforeCells, Has.Count.EqualTo(16));
            Assert.That(snapshot.AfterCells, Has.Count.EqualTo(16));
        }

        private static int ExpectedStage(MicroPatternLayer layer)
        {
            switch (layer)
            {
                case MicroPatternLayer.Geometry: return 10;
                case MicroPatternLayer.Surface: return 20;
                case MicroPatternLayer.Affordance: return 30;
                case MicroPatternLayer.Material: return 40;
                case MicroPatternLayer.Hazard: return 50;
                case MicroPatternLayer.Marker: return 60;
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }
        }

        private static string[] CellEvidence(IEnumerable<MicroPatternCell> cells)
        {
            return cells.OrderBy(value => value.Coordinate.Y)
                .ThenBy(value => value.Coordinate.X)
                .Select(cell => cell.Coordinate.X + "," + cell.Coordinate.Y + "|" +
                    string.Join(";", cell.Instructions.OrderBy(value => (int)value.Layer)
                        .Select(value => value.Layer + ":" + value.Operation + ":" + value.PayloadId)))
                .ToArray();
        }

        private static string TransformToken(MicroPatternTransform value)
        {
            switch (value)
            {
                case MicroPatternTransform.R0: return "R0";
                case MicroPatternTransform.MirrorX: return "MIRROR_X";
                case MicroPatternTransform.MirrorY: return "MIRROR_Y";
                case MicroPatternTransform.R180: return "R180";
                default: throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static string DecisionEvidence(MicroPatternSelectionDecision value)
        {
            return value.RequestId.Value + "|" + value.CandidateIndexDigest + "|" +
                   value.ChosenKey.CanonicalValue + "|" + value.ChosenCandidateOrdinal + "|" +
                   value.TotalWeight + "|" + value.Ticket + "|" + value.InitialState + "|" +
                   value.DrawCountBefore + "|" + value.DrawCountAfter;
        }

        private static string AssetPathDigest()
        {
            var material = string.Join("\n", AssetDatabase.GetAllAssetPaths()
                .OrderBy(value => value, StringComparer.Ordinal));
            return Sha256(new UTF8Encoding(false).GetBytes(material));
        }

        private static int DataRowCount(string path)
        {
            return File.ReadAllLines(path, Encoding.UTF8)
                .Count(value => !string.IsNullOrEmpty(value)) - 1;
        }

        private static string ComputeManifest(string root, IEnumerable<string> paths)
        {
            var noBom = new UTF8Encoding(false);
            var withBom = new UTF8Encoding(true);
            var records = paths.Select(path => new { Path = path, Relative = Relative(root, path) })
                .OrderBy(value => value.Relative, StringComparer.Ordinal)
                .Select(value =>
                {
                    var normalized = File.ReadAllText(value.Path, Encoding.UTF8)
                        .Replace("\r\n", "\n").Replace("\r", "\n");
                    return value.Relative + "\t" + Sha256(
                        withBom.GetPreamble().Concat(noBom.GetBytes(normalized)).ToArray());
                });
            return Sha256(noBom.GetBytes(string.Join("\n", records)));
        }

        private static string Relative(string root, string path)
        {
            return path.Substring(root.Length + 1).Replace('\\', '/');
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
                return string.Concat(sha256.ComputeHash(bytes)
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string[] GeneratedCsvFiles()
        {
            return Directory.GetFiles(
                    FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated"),
                    "*.csv", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Pair(MicroPatternDefinition definition, MicroPatternTransform transform)
        {
            return definition.Id.Value + "|" + transform;
        }

        private static string ImportErrors(MicroPatternCsvImportResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string SchemaErrors(MicroPatternCellSchemaResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string TransformErrors(MicroPatternTransformResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string ApplicationErrors(MicroPatternApplicationResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string CandidateErrors(MicroPatternCandidateIndexBuildResult result) =>
            string.Join("\n", result.Rejections.Select(value => value.ToString()));
        private static string SelectionErrors(MicroPatternSelectionBatchResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string RenderErrors(MicroPatternRenderResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string SignatureErrors(MicroPatternSilhouetteSignatureResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string RepetitionErrors(MicroPatternRepetitionGuardResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string CleanupErrors(MicroPatternLocalCleanupResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string PreviewErrors(MicroPatternPreviewBuildResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));
    }
}
