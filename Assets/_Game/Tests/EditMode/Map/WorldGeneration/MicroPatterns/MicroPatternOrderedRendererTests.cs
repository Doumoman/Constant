using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.MicroPatterns
{
    [TestFixture]
    [Category("MAP10_03")]
    public sealed class MicroPatternOrderedRendererTests
    {
        [Test]
        public void WritesUseExactSixStageOrderAcrossRequestsAndCells()
        {
            var plans = new[]
            {
                Plan("MP_STAGE_MARKER", new LocalTileCoord(0, 2),
                    Instruction(MicroPatternLayer.Marker, MicroPatternOperation.SetMarker, "MARKER_A")),
                Plan("MP_STAGE_GEOMETRY", new LocalTileCoord(3, 3),
                    Instruction(MicroPatternLayer.Geometry, MicroPatternOperation.AddSolid)),
                Plan("MP_STAGE_HAZARD", new LocalTileCoord(2, 0),
                    Instruction(MicroPatternLayer.Hazard, MicroPatternOperation.SetHazard, "HAZARD_A")),
                Plan("MP_STAGE_SURFACE", new LocalTileCoord(0, 0),
                    Instruction(MicroPatternLayer.Surface, MicroPatternOperation.SetSurface, "SURFACE_A")),
                Plan("MP_STAGE_MATERIAL", new LocalTileCoord(0, 1),
                    Instruction(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MATERIAL_A")),
                Plan("MP_STAGE_AFFORDANCE", new LocalTileCoord(1, 0),
                    Instruction(MicroPatternLayer.Affordance, MicroPatternOperation.SetAffordance, "AFFORDANCE_A")),
            };
            var requests = plans.Select((plan, index) => Request("MPR_STAGE_" + index, plan)).Reverse().ToArray();
            var result = Render(requests, Target(plans));

            Assert.That(result.Delta.Writes.Select(value => (int)value.Stage),
                Is.EqualTo(new[] { 10, 20, 30, 40, 50, 60 }));
            Assert.That(result.Delta.Writes.Select(value => value.Layer), Is.EqualTo(new[]
            {
                MicroPatternLayer.Geometry,
                MicroPatternLayer.Surface,
                MicroPatternLayer.Affordance,
                MicroPatternLayer.Material,
                MicroPatternLayer.Hazard,
                MicroPatternLayer.Marker,
            }));
        }

        [Test]
        public void GeometryAndSetOperationsMutateOnlyTheirDestinationLayer()
        {
            var addPlan = Plan("MP_MUTATE_ADD", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Geometry, MicroPatternOperation.AddSolid),
                Instruction(MicroPatternLayer.Surface, MicroPatternOperation.SetSurface, "SURFACE_NEW"));
            var addTarget = Target(new[] { addPlan }, coordinate =>
                coordinate == new LocalTileCoord(0, 0)
                    ? State(coordinate, false, "SURFACE_OLD", "AFF_KEEP", "MAT_KEEP", "HZ_KEEP", "MARK_KEEP")
                    : State(coordinate));
            var added = Render(new[] { Request("MPR_ADD", addPlan) }, addTarget);
            var after = added.Delta.Cells.Single().After;
            Assert.That(after.Solid, Is.True);
            Assert.That(after.SurfaceId, Is.EqualTo("SURFACE_NEW"));
            Assert.That(after.AffordanceId, Is.EqualTo("AFF_KEEP"));
            Assert.That(after.MaterialId, Is.EqualTo("MAT_KEEP"));
            Assert.That(after.HazardId, Is.EqualTo("HZ_KEEP"));
            Assert.That(after.MarkerId, Is.EqualTo("MARK_KEEP"));

            var carvePlan = Plan("MP_MUTATE_CARVE", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Geometry, MicroPatternOperation.CarveAir));
            var carved = Render(new[] { Request("MPR_CARVE", carvePlan) },
                Target(new[] { carvePlan }, coordinate => State(coordinate, true, material: "MAT_KEEP")));
            Assert.That(carved.Delta.Cells.Single().After.Solid, Is.False);
            Assert.That(carved.Delta.Cells.Single().After.MaterialId, Is.EqualTo("MAT_KEEP"));
        }

        [Test]
        public void NoChangePreservesValuesAndExistingProvenance()
        {
            var plan = Plan("MP_NO_CHANGE", new LocalTileCoord(0, 0));
            var evidence = new MicroPatternRenderSourceEvidence(MicroPatternLayer.Geometry, "SRC_BASE");
            var target = Target(new[] { plan }, coordinate =>
                State(coordinate, true, "SURFACE_A", "AFF_A", "MAT_A", "HZ_A", "MARK_A",
                    new[] { evidence }));
            var beforeDigest = plan.StableDigest;
            var result = Render(new[] { Request("MPR_NO_CHANGE", plan) }, target);

            Assert.That(result.Delta.Writes, Is.Empty);
            Assert.That(result.Delta.Cells, Is.Empty);
            Assert.That(result.Delta.InputTarget.Cells.All(value => value.Solid), Is.True);
            Assert.That(result.Delta.InputTarget.Cells.All(value => value.Provenance.Single().Equals(evidence)), Is.True);
            Assert.That(plan.StableDigest, Is.EqualTo(beforeDigest));
            Assert.That(target.Cells.All(value => value.Provenance.Count == 1), Is.True);
        }

        [Test]
        public void TargetMustBeExactUnionWithoutMissingDuplicateOrExtraCells()
        {
            var plan = Plan("MP_TARGET", new LocalTileCoord(0, 0));
            var request = Request("MPR_TARGET", plan);
            var valid = Target(new[] { plan });
            var missing = new MicroPatternRenderTarget(valid.Cells.Skip(1));
            var duplicate = new MicroPatternRenderTarget(valid.Cells.Concat(new[] { valid.Cells[0] }));
            var extra = new MicroPatternRenderTarget(valid.Cells.Concat(new[]
            {
                State(new LocalTileCoord(99, 99)),
            }));

            AssertCode(MicroPatternOrderedRenderer.Render(new[] { request }, missing),
                MicroPatternRenderErrorCode.MissingTargetCell);
            AssertCode(MicroPatternOrderedRenderer.Render(new[] { request }, duplicate),
                MicroPatternRenderErrorCode.DuplicateTargetCell);
            AssertCode(MicroPatternOrderedRenderer.Render(new[] { request }, extra),
                MicroPatternRenderErrorCode.ExtraTargetCell);
        }

        [Test]
        public void IdenticalSameLayerWritesCoalesceAndUnionProvenance()
        {
            var first = Plan("MP_COALESCE_A", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MAT_A"));
            var second = Plan("MP_COALESCE_B", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MAT_A"));
            var result = Render(new[]
            {
                Request("MPR_COALESCE_B", second),
                Request("MPR_COALESCE_A", first),
            }, Target(new[] { first, second }));

            var write = result.Delta.Writes.Single();
            Assert.That(write.SemanticValue, Is.EqualTo("MAT_A"));
            Assert.That(write.IsCoalesced, Is.True);
            Assert.That(write.Provenance.Select(value => value.RequestId.Value),
                Is.EqualTo(new[] { "MPR_COALESCE_A", "MPR_COALESCE_B" }));
            Assert.That(write.Provenance.Select(value => value.SourcePatternId.Value),
                Is.EqualTo(new[] { "MP_COALESCE_A", "MP_COALESCE_B" }));
        }

        [Test]
        public void DifferentSameLayerWritesRejectWholeBatchWithStableEvidence()
        {
            var first = Plan("MP_CONFLICT_A", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MAT_A"));
            var second = Plan("MP_CONFLICT_B", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MAT_B"));
            var result = MicroPatternOrderedRenderer.Render(new[]
            {
                Request("MPR_CONFLICT_B", second),
                Request("MPR_CONFLICT_A", first),
            }, Target(new[] { first, second }));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Delta, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(result.Conflicts.Count, Is.EqualTo(1));
            Assert.That(result.Conflicts.Single().Alternatives.Select(value => value.SemanticValue),
                Is.EqualTo(new[] { "MAT_A", "MAT_B" }));
            AssertCode(result, MicroPatternRenderErrorCode.ConflictingLayerWrite);
            AssertCode(result, MicroPatternRenderErrorCode.AtomicRenderRejected);
        }

        [Test]
        public void DifferentLayersOnSameCellAreAllowedInStageOrder()
        {
            var surface = Plan("MP_LAYER_SURFACE", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Surface, MicroPatternOperation.SetSurface, "SURFACE_A"));
            var hazard = Plan("MP_LAYER_HAZARD", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Hazard, MicroPatternOperation.SetHazard, "HAZARD_A"));
            var result = Render(new[]
            {
                Request("MPR_LAYER_HAZARD", hazard),
                Request("MPR_LAYER_SURFACE", surface),
            }, Target(new[] { surface, hazard }));

            Assert.That(result.Delta.Writes.Select(value => value.Stage), Is.EqualTo(new[]
            {
                MicroPatternRenderStage.Surface,
                MicroPatternRenderStage.Hazard,
            }));
            var after = result.Delta.Cells.Single().After;
            Assert.That(after.SurfaceId, Is.EqualTo("SURFACE_A"));
            Assert.That(after.HazardId, Is.EqualTo("HAZARD_A"));
        }

        [Test]
        public void IdempotentWriteRetainsEvidenceAndMarksValueEquality()
        {
            var plan = Plan("MP_IDEMPOTENT", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MAT_A"));
            var target = Target(new[] { plan }, coordinate => State(coordinate, material: "MAT_A"));
            var result = Render(new[] { Request("MPR_IDEMPOTENT", plan) }, target);

            Assert.That(result.Delta.Writes.Single().IsIdempotent, Is.True);
            Assert.That(result.Delta.Writes.Single().Provenance.Count, Is.EqualTo(1));
            Assert.That(result.Delta.Cells.Single().ValuesEqual, Is.True);
            Assert.That(result.Delta.Cells.Single().Before.MaterialId, Is.EqualTo("MAT_A"));
            Assert.That(result.Delta.Cells.Single().After.MaterialId, Is.EqualTo("MAT_A"));
        }

        [Test]
        public void ProtectedNoChangeCellProducesNoRendererMutation()
        {
            var targetCoordinate = new LocalTileCoord(0, 0);
            var plan = PlanProtected("MP_PROTECTED", targetCoordinate,
                Instruction(MicroPatternLayer.Geometry, MicroPatternOperation.AddSolid));
            Assert.That(plan.ProtectedHits.Count, Is.EqualTo(1));
            Assert.That(plan.Cells.Single(value => value.TargetCoordinate == targetCoordinate)
                .Instructions.All(value => value.Operation == MicroPatternOperation.NoChange), Is.True);

            var result = Render(new[] { Request("MPR_PROTECTED", plan) }, Target(new[] { plan }));
            Assert.That(result.Delta.Writes, Is.Empty);
            Assert.That(result.Delta.Cells, Is.Empty);
        }

        [Test]
        public void InputsOutputsAreReadOnlyAndIssuesAccumulateInStableOrder()
        {
            var invalid = MicroPatternOrderedRenderer.Render(new MicroPatternRenderRequest[]
            {
                new MicroPatternRenderRequest(new MicroPatternRenderRequestId("bad"), null),
                new MicroPatternRenderRequest(new MicroPatternRenderRequestId("MPR_DUP"), null),
                new MicroPatternRenderRequest(new MicroPatternRenderRequestId("MPR_DUP"), null),
                null,
            }, new MicroPatternRenderTarget(Array.Empty<MicroPatternRenderCellState>()));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Errors.Select(value => value.Code), Does.Contain(MicroPatternRenderErrorCode.InvalidRequestId));
            Assert.That(invalid.Errors.Select(value => value.Code), Does.Contain(MicroPatternRenderErrorCode.DuplicateRequestId));
            Assert.That(invalid.Errors.Select(value => value.Code), Does.Contain(MicroPatternRenderErrorCode.InvalidApplicationPlan));
            Assert.That(invalid.Errors.Select(value => value.Code), Does.Contain(MicroPatternRenderErrorCode.MissingInput));
            Assert.That(invalid.Errors, Is.EqualTo(invalid.Errors.OrderBy(value => value).ToArray()));
            Assert.That(invalid.Errors.Distinct().Count(), Is.EqualTo(invalid.Errors.Count));

            var plan = Plan("MP_READ_ONLY", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Marker, MicroPatternOperation.SetMarker, "MARK_A"));
            var success = Render(new[] { Request("MPR_READ_ONLY", plan) }, Target(new[] { plan }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternLayerWrite>)success.Delta.Writes).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternRenderedCellDelta>)success.Delta.Cells).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternRenderSourceEvidence>)success.Delta.Writes.Single().Provenance).Clear());
        }

        [Test]
        public void ReversedRequestAndTargetEnumerationProduceSameDeltaAndDigest()
        {
            var first = Plan("MP_ORDER_A", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Surface, MicroPatternOperation.SetSurface, "SURFACE_A"));
            var second = Plan("MP_ORDER_B", new LocalTileCoord(0, 0),
                Instruction(MicroPatternLayer.Marker, MicroPatternOperation.SetMarker, "MARKER_A"));
            var requests = new[] { Request("MPR_ORDER_A", first), Request("MPR_ORDER_B", second) };
            var target = Target(new[] { first, second });
            var forward = Render(requests, target);
            var reversed = Render(requests.Reverse(), new MicroPatternRenderTarget(target.Cells.Reverse()));

            Assert.That(reversed.StableDigest, Is.EqualTo(forward.StableDigest));
            Assert.That(reversed.Delta.Writes.Select(WriteKey),
                Is.EqualTo(forward.Delta.Writes.Select(WriteKey)));
            Assert.That(reversed.Delta.Cells.Select(value => value.TargetCoordinate),
                Is.EqualTo(forward.Delta.Cells.Select(value => value.TargetCoordinate)));
        }

        [Test]
        public void RendererSurfaceHasNoRngFileUnityLifecycleOrTilemapSideEffects()
        {
            var root = FullPath("Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns");
            var files = new[]
            {
                "MicroPatternRenderTarget.cs",
                "MicroPatternRenderDelta.cs",
                "MicroPatternOrderedRenderer.cs",
            };
            var source = string.Join("\n", files.Select(value => File.ReadAllText(Path.Combine(root, value))));
            var forbidden = new[]
            {
                "UnityEditor", "UnityEngine", "MonoBehaviour", "System.Random", "UnityEngine.Random",
                "File.", "Directory.", "DateTime", "Tilemap", "SectorCanvasContract", "ValidationStamp",
                "Candidate", "Selector", "Cleanup",
            };
            Assert.That(forbidden.Where(source.Contains), Is.Empty);
        }

        private static MicroPatternRenderResult Render(
            IEnumerable<MicroPatternRenderRequest> requests,
            MicroPatternRenderTarget target)
        {
            var result = MicroPatternOrderedRenderer.Render(requests, target);
            Assert.That(result.Success, Is.True, Errors(result));
            Assert.That(result.StableDigest, Does.Match("^[0-9a-f]{64}$"));
            return result;
        }

        private static MicroPatternRenderRequest Request(string id, MicroPatternApplicationPlan plan)
        {
            return new MicroPatternRenderRequest(new MicroPatternRenderRequestId(id), plan);
        }

        private static MicroPatternApplicationPlan Plan(
            string patternId,
            LocalTileCoord writeCoordinate,
            params MicroPatternInstruction[] instructions)
        {
            return BuildPlan(patternId, writeCoordinate, instructions,
                Array.Empty<MicroPatternProtectedCell>());
        }

        private static MicroPatternApplicationPlan PlanProtected(
            string patternId,
            LocalTileCoord writeCoordinate,
            params MicroPatternInstruction[] instructions)
        {
            return BuildPlan(patternId, writeCoordinate, instructions, new[]
            {
                new MicroPatternProtectedCell(
                    writeCoordinate,
                    MicroPatternProtectedSourceKind.RouteSpine,
                    "SPINE_PROTECTED"),
            });
        }

        private static MicroPatternApplicationPlan BuildPlan(
            string patternId,
            LocalTileCoord writeCoordinate,
            IEnumerable<MicroPatternInstruction> instructions,
            IEnumerable<MicroPatternProtectedCell> protectedCells)
        {
            var cells = new List<MicroPatternCell>();
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var coordinate = new LocalTileCoord(x, y);
                    cells.Add(new MicroPatternCell(coordinate,
                        coordinate == writeCoordinate
                            ? instructions
                            : Array.Empty<MicroPatternInstruction>()));
                }
            }
            var definition = new MicroPatternDefinition(
                new MicroPatternId(patternId),
                4,
                4,
                cells,
                1,
                new[] { MoonpalaceBiomeId.MoonCrater },
                new[] { MicroPatternTransform.R0 },
                MicroPatternProtectedPolicy.ForceNoChange);
            var transformed = MicroPatternTransformer.Transform(definition, MicroPatternTransform.R0);
            Assert.That(transformed.Success, Is.True,
                string.Join("\n", transformed.Errors.Select(value => value.ToString())));
            var application = MicroPatternApplicationPlanner.Plan(
                transformed.Pattern,
                new MicroPatternPlacement(new LocalTileCoord(0, 0)),
                protectedCells);
            Assert.That(application.Success, Is.True,
                string.Join("\n", application.Errors.Select(value => value.ToString())));
            return application.Plan;
        }

        private static MicroPatternInstruction Instruction(
            MicroPatternLayer layer,
            MicroPatternOperation operation,
            string payload = null)
        {
            return new MicroPatternInstruction(layer, operation, payload);
        }

        private static MicroPatternRenderTarget Target(
            IEnumerable<MicroPatternApplicationPlan> plans,
            Func<LocalTileCoord, MicroPatternRenderCellState> factory = null)
        {
            var coordinates = plans.SelectMany(value => value.Cells)
                .Select(value => value.TargetCoordinate)
                .Distinct()
                .OrderBy(value => value.Y)
                .ThenBy(value => value.X)
                .ToArray();
            return new MicroPatternRenderTarget(coordinates.Select(value =>
                factory == null ? State(value) : factory(value)));
        }

        private static MicroPatternRenderCellState State(
            LocalTileCoord coordinate,
            bool solid = false,
            string surface = "",
            string affordance = "",
            string material = "",
            string hazard = "",
            string marker = "",
            IEnumerable<MicroPatternRenderSourceEvidence> provenance = null)
        {
            return new MicroPatternRenderCellState(
                coordinate, solid, surface, affordance, material, hazard, marker, provenance);
        }

        private static void AssertCode(
            MicroPatternRenderResult result,
            MicroPatternRenderErrorCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Delta, Is.Null);
            Assert.That(result.StableDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), Errors(result));
        }

        private static string WriteKey(MicroPatternLayerWrite value)
        {
            return value.Stage + "|" + value.TargetCoordinate + "|" + value.Layer + "|" + value.SemanticValue;
        }

        private static string Errors(MicroPatternRenderResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
