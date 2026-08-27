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
    [Category("MAP10_02")]
    public sealed class MicroPatternTransformAndProtectedMaskTests
    {
        [Test]
        public void FourTransformsMapAsymmetricCoordinateExactly()
        {
            var definition = Definition(MicroPatternProtectedPolicy.ForceNoChange, AllTransforms());
            var expected = new Dictionary<MicroPatternTransform, LocalTileCoord>
            {
                { MicroPatternTransform.R0, new LocalTileCoord(0, 1) },
                { MicroPatternTransform.MirrorX, new LocalTileCoord(3, 1) },
                { MicroPatternTransform.MirrorY, new LocalTileCoord(0, 2) },
                { MicroPatternTransform.R180, new LocalTileCoord(3, 2) },
            };

            foreach (var pair in expected)
            {
                var result = MicroPatternTransformer.Transform(definition, pair.Key);
                Assert.That(result.Success, Is.True, Errors(result));
                var marker = result.Pattern.Cells.Single(cell => cell.Instructions.Any(value =>
                    value.Operation == MicroPatternOperation.SetMarker));
                Assert.That(marker.Coordinate, Is.EqualTo(pair.Value), pair.Key.ToString());
            }
        }

        [Test]
        public void TransformPublishesExactCanonicalSixteenCellCoverage()
        {
            var result = Transform(Definition(), MicroPatternTransform.R180);
            Assert.That(result.Cells.Count, Is.EqualTo(16));
            Assert.That(result.Cells.Select(cell => MicroPatternDefinition.CanonicalCellIndex(cell.Coordinate)),
                Is.EqualTo(Enumerable.Range(0, 16)));
            Assert.That(result.Cells.Select(cell => cell.Coordinate).Distinct().Count(), Is.EqualTo(16));
        }

        [Test]
        public void TransformPreservesInstructionsAndDoesNotMutateSource()
        {
            var source = Definition();
            var sourceCell = source.Cells.Single(cell => cell.Coordinate == new LocalTileCoord(0, 1));
            var before = source.ComputeStableDigest();
            var transformed = Transform(source, MicroPatternTransform.MirrorX);
            var target = transformed.Cells.Single(cell => cell.Coordinate == new LocalTileCoord(3, 1));

            Assert.That(target.Instructions.Single().Layer, Is.EqualTo(sourceCell.Instructions.Single().Layer));
            Assert.That(target.Instructions.Single().Operation, Is.EqualTo(sourceCell.Instructions.Single().Operation));
            Assert.That(target.Instructions.Single().PayloadId, Is.EqualTo("MARKER_A"));
            Assert.That(source.ComputeStableDigest(), Is.EqualTo(before));
            Assert.That(sourceCell.Coordinate, Is.EqualTo(new LocalTileCoord(0, 1)));
            Assert.That(target.Instructions.Single(), Is.Not.SameAs(sourceCell.Instructions.Single()));
        }

        [Test]
        public void TransformRejectsDisallowedAndUndefinedValuesAtomically()
        {
            var source = Definition(
                MicroPatternProtectedPolicy.ForceNoChange,
                new[] { MicroPatternTransform.R0 });
            var disallowed = MicroPatternTransformer.Transform(source, MicroPatternTransform.MirrorX);
            var undefined = MicroPatternTransformer.Transform(source, (MicroPatternTransform)99);

            Assert.That(disallowed.Success, Is.False);
            Assert.That(disallowed.Pattern, Is.Null);
            Assert.That(disallowed.StableDigest, Is.Empty);
            Assert.That(disallowed.Errors.Select(value => value.Code),
                Does.Contain(MicroPatternTransformErrorCode.TransformNotAllowed));
            Assert.That(undefined.Errors.Select(value => value.Code),
                Does.Contain(MicroPatternTransformErrorCode.UnsupportedTransform));
            Assert.That(undefined.Pattern, Is.Null);
        }

        [Test]
        public void PlacementAddsOriginAndRejectsCoordinateOverflow()
        {
            var transformed = Transform(Definition(), MicroPatternTransform.MirrorX);
            var accepted = MicroPatternApplicationPlanner.Plan(
                transformed,
                new MicroPatternPlacement(new LocalTileCoord(10, 20)),
                Array.Empty<MicroPatternProtectedCell>());
            Assert.That(accepted.Success, Is.True, Errors(accepted));
            Assert.That(accepted.Plan.Cells.Single(value => value.LocalCoordinate == new LocalTileCoord(3, 1))
                .TargetCoordinate, Is.EqualTo(new LocalTileCoord(13, 21)));

            var rejected = MicroPatternApplicationPlanner.Plan(
                transformed,
                new MicroPatternPlacement(new LocalTileCoord(int.MaxValue - 2, 0)),
                Array.Empty<MicroPatternProtectedCell>());
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Plan, Is.Null);
            Assert.That(rejected.StableDigest, Is.Empty);
            Assert.That(rejected.Errors.Select(value => value.Code),
                Does.Contain(MicroPatternApplicationErrorCode.CoordinateOverflow));
        }

        [Test]
        public void MaskUnionsFourKindsDeduplicatesAndExcludesOutsidePlacement()
        {
            var target = new LocalTileCoord(11, 21);
            var sources = FourSources(target).Concat(new[]
            {
                new MicroPatternProtectedCell(target,
                    MicroPatternProtectedSourceKind.RouteSpine, "SPINE_A"),
                new MicroPatternProtectedCell(new LocalTileCoord(99, 99),
                    MicroPatternProtectedSourceKind.RouteSpine, "OUTSIDE"),
            }).ToArray();
            var first = MicroPatternProtectedMaskBuilder.Build(
                new MicroPatternPlacement(new LocalTileCoord(10, 20)), sources);
            var second = MicroPatternProtectedMaskBuilder.Build(
                new MicroPatternPlacement(new LocalTileCoord(10, 20)), sources.Reverse());

            Assert.That(first.Success, Is.True, MaskErrors(first));
            Assert.That(first.Mask.Entries.Count, Is.EqualTo(1));
            Assert.That(first.Mask.Entries.Single().Provenance.Count, Is.EqualTo(4));
            Assert.That(first.Mask.Entries.Single().Provenance.Select(value => value.SourceKind),
                Is.EqualTo(Enum.GetValues(typeof(MicroPatternProtectedSourceKind))
                    .Cast<MicroPatternProtectedSourceKind>()));
            Assert.That(second.StableDigest, Is.EqualTo(first.StableDigest));
        }

        [Test]
        public void ProtectionIntersectsTheTransformedCoordinate()
        {
            var transformed = Transform(Definition(), MicroPatternTransform.MirrorX);
            var target = new LocalTileCoord(13, 21);
            var result = MicroPatternApplicationPlanner.Plan(
                transformed,
                new MicroPatternPlacement(new LocalTileCoord(10, 20)),
                new[] { Protected(target, MicroPatternProtectedSourceKind.RouteSpine, "SPINE_A") });

            Assert.That(result.Success, Is.True, Errors(result));
            Assert.That(result.Plan.ProtectedHits.Single().LocalCoordinate,
                Is.EqualTo(new LocalTileCoord(3, 1)));
            Assert.That(result.Plan.ProtectedHits.Single().TargetCoordinate, Is.EqualTo(target));
        }

        [Test]
        public void ForceNoChangeMasksAllSixLayersAndPreservesUnprotectedWrites()
        {
            var transformed = Transform(Definition(sixWrites: true), MicroPatternTransform.R0);
            var protectedTarget = new LocalTileCoord(10, 21);
            var result = MicroPatternApplicationPlanner.Plan(
                transformed,
                new MicroPatternPlacement(new LocalTileCoord(10, 20)),
                FourSources(protectedTarget));

            Assert.That(result.Success, Is.True, Errors(result));
            var masked = result.Plan.Cells.Single(value => value.TargetCoordinate == protectedTarget);
            Assert.That(masked.Instructions.Count, Is.EqualTo(6));
            Assert.That(masked.Instructions.All(value =>
                value.Operation == MicroPatternOperation.NoChange && value.PayloadId == string.Empty), Is.True);
            Assert.That(result.Plan.ProtectedHits.Single().RemovedWriteCount, Is.EqualTo(6));
            Assert.That(transformed.Cells.Single(value => value.Coordinate == new LocalTileCoord(0, 1))
                .Instructions.Count(value => value.Operation != MicroPatternOperation.NoChange), Is.EqualTo(6));

            var unprotected = MicroPatternApplicationPlanner.Plan(
                transformed,
                new MicroPatternPlacement(new LocalTileCoord(10, 20)),
                Array.Empty<MicroPatternProtectedCell>());
            Assert.That(unprotected.Plan.Cells.Single(value => value.TargetCoordinate == protectedTarget)
                .Instructions.Count(value => value.Operation != MicroPatternOperation.NoChange), Is.EqualTo(6));
        }

        [Test]
        public void RejectCandidateIsAllOrNothingButAllowsNoChangeOverlap()
        {
            var transformed = Transform(
                Definition(MicroPatternProtectedPolicy.RejectCandidate),
                MicroPatternTransform.R0);
            var rejected = MicroPatternApplicationPlanner.Plan(
                transformed,
                new MicroPatternPlacement(new LocalTileCoord(10, 20)),
                FourSources(new LocalTileCoord(10, 21)));

            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Plan, Is.Null);
            Assert.That(rejected.StableDigest, Is.Empty);
            Assert.That(rejected.RejectedHits.Count, Is.EqualTo(1));
            Assert.That(rejected.Errors.Single(value =>
                value.Code == MicroPatternApplicationErrorCode.ProtectedWriteRejected).Provenance.Count,
                Is.EqualTo(4));

            var allowed = MicroPatternApplicationPlanner.Plan(
                transformed,
                new MicroPatternPlacement(new LocalTileCoord(10, 20)),
                FourSources(new LocalTileCoord(11, 21)));
            Assert.That(allowed.Success, Is.True, Errors(allowed));
            Assert.That(allowed.Plan.ProtectedHits, Is.Empty);
        }

        [Test]
        public void MultipleSourceHitEvidenceUsesStableOrdinalOrder()
        {
            var target = new LocalTileCoord(10, 21);
            var source = FourSources(target).Reverse().Concat(FourSources(target)).ToArray();
            var result = MicroPatternApplicationPlanner.Plan(
                Transform(Definition(), MicroPatternTransform.R0),
                new MicroPatternPlacement(new LocalTileCoord(10, 20)),
                source);
            var provenance = result.Plan.ProtectedHits.Single().Provenance;

            Assert.That(provenance.Count, Is.EqualTo(4));
            Assert.That(provenance, Is.EqualTo(provenance.OrderBy(value => value).ToArray()));
            Assert.That(provenance.Select(value => value.SourceId),
                Is.EqualTo(new[] { "SPINE_A", "ENVELOPE_A", "BOUNDARY_A", "ENTRY_A" }));
        }

        [Test]
        public void PlansAreReadOnlyAtomicAndInputOrderIndependent()
        {
            var firstDefinition = Definition(reverseCells: false);
            var secondDefinition = Definition(reverseCells: true);
            var sources = FourSources(new LocalTileCoord(10, 21)).ToArray();
            var first = MicroPatternApplicationPlanner.Plan(
                Transform(firstDefinition, MicroPatternTransform.R0),
                new MicroPatternPlacement(new LocalTileCoord(10, 20)),
                sources);
            var second = MicroPatternApplicationPlanner.Plan(
                Transform(secondDefinition, MicroPatternTransform.R0),
                new MicroPatternPlacement(new LocalTileCoord(10, 20)),
                sources.Reverse());

            Assert.That(first.Success, Is.True, Errors(first));
            Assert.That(second.StableDigest, Is.EqualTo(first.StableDigest));
            Assert.That(first.StableDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternPreparedCell>)first.Plan.Cells).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<MicroPatternProtectedMaskEntry>)first.Plan.ProtectedMask.Entries).Clear());

            var invalid = MicroPatternProtectedMaskBuilder.Build(
                new MicroPatternPlacement(new LocalTileCoord(0, 0)),
                new[] { Protected(new LocalTileCoord(0, 0),
                    (MicroPatternProtectedSourceKind)99, "bad id") });
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Mask, Is.Null);
            Assert.That(invalid.StableDigest, Is.Empty);
            Assert.That(invalid.Errors, Is.EqualTo(invalid.Errors.OrderBy(value => value).ToArray()));
        }

        [Test]
        public void RuntimeSurfaceHasNoRendererRngFileOrUnityLifecycleSideEffects()
        {
            var root = FullPath("Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns");
            var files = new[]
            {
                "MicroPatternTransforms.cs",
                "MicroPatternProtectedMask.cs",
                "MicroPatternApplicationPlan.cs",
            };
            var source = string.Join("\n", files.Select(value => File.ReadAllText(Path.Combine(root, value))));
            var forbidden = new[]
            {
                "UnityEditor", "UnityEngine", "MonoBehaviour", "System.Random", "UnityEngine.Random",
                "File.", "Directory.", "DateTime", "StageMapGenerator", "GridWorld", "Render(",
            };
            Assert.That(forbidden.Where(source.Contains), Is.Empty);
        }

        private static TransformedMicroPattern Transform(
            MicroPatternDefinition definition,
            MicroPatternTransform transform)
        {
            var result = MicroPatternTransformer.Transform(definition, transform);
            Assert.That(result.Success, Is.True, Errors(result));
            return result.Pattern;
        }

        private static MicroPatternDefinition Definition(
            MicroPatternProtectedPolicy policy = MicroPatternProtectedPolicy.ForceNoChange,
            IEnumerable<MicroPatternTransform> transforms = null,
            bool sixWrites = false,
            bool reverseCells = false)
        {
            var cells = new List<MicroPatternCell>();
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var coordinate = new LocalTileCoord(x, y);
                    IEnumerable<MicroPatternInstruction> instructions = Array.Empty<MicroPatternInstruction>();
                    if (coordinate == new LocalTileCoord(0, 1))
                    {
                        instructions = sixWrites ? SixWrites() : new[]
                        {
                            new MicroPatternInstruction(
                                MicroPatternLayer.Marker,
                                MicroPatternOperation.SetMarker,
                                "MARKER_A"),
                        };
                    }
                    cells.Add(new MicroPatternCell(coordinate, instructions));
                }
            }
            if (reverseCells) cells.Reverse();

            return new MicroPatternDefinition(
                new MicroPatternId("MP_TRANSFORM_TEST"),
                4,
                4,
                cells,
                1,
                new[] { MoonpalaceBiomeId.MoonCrater },
                transforms ?? AllTransforms(),
                policy);
        }

        private static MicroPatternInstruction[] SixWrites()
        {
            return new[]
            {
                new MicroPatternInstruction(MicroPatternLayer.Geometry, MicroPatternOperation.AddSolid),
                new MicroPatternInstruction(MicroPatternLayer.Surface, MicroPatternOperation.SetSurface, "SURFACE_A"),
                new MicroPatternInstruction(MicroPatternLayer.Affordance, MicroPatternOperation.SetAffordance, "AFFORDANCE_A"),
                new MicroPatternInstruction(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MATERIAL_A"),
                new MicroPatternInstruction(MicroPatternLayer.Hazard, MicroPatternOperation.SetHazard, "HAZARD_A"),
                new MicroPatternInstruction(MicroPatternLayer.Marker, MicroPatternOperation.SetMarker, "MARKER_A"),
            };
        }

        private static MicroPatternTransform[] AllTransforms()
        {
            return new[]
            {
                MicroPatternTransform.R0,
                MicroPatternTransform.MirrorX,
                MicroPatternTransform.MirrorY,
                MicroPatternTransform.R180,
            };
        }

        private static IEnumerable<MicroPatternProtectedCell> FourSources(LocalTileCoord target)
        {
            yield return Protected(target, MicroPatternProtectedSourceKind.RouteSpine, "SPINE_A");
            yield return Protected(target, MicroPatternProtectedSourceKind.TraversalEnvelope, "ENVELOPE_A");
            yield return Protected(target, MicroPatternProtectedSourceKind.BoundaryProtectedOpen, "BOUNDARY_A");
            yield return Protected(target, MicroPatternProtectedSourceKind.SpecialFixedEntry, "ENTRY_A");
        }

        private static MicroPatternProtectedCell Protected(
            LocalTileCoord target,
            MicroPatternProtectedSourceKind kind,
            string id)
        {
            return new MicroPatternProtectedCell(target, kind, id);
        }

        private static string Errors(MicroPatternTransformResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string Errors(MicroPatternApplicationResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static string MaskErrors(MicroPatternProtectedMaskResult result)
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
