#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;

namespace StarNight.Map.Tests.EditMode.Rmap10
{
    [Category("RMAP10")]
    public sealed class RmapSmallRunHarnessTests
    {
        [Test]
        public void E03_E06_ThreeRepresentativeSeedsAreBoundedReproducibleAndUseTheDeclaredPortForms()
        {
            var first = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(1107, 36, 24,
                RmapSmallRunRecipe.PortGalleryV1));
            var repeat = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(1107, 36, 24,
                RmapSmallRunRecipe.PortGalleryV1));
            var wide = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(2203, 48, 24,
                RmapSmallRunRecipe.PortGalleryV1));
            var alternate = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(3301, 36, 24,
                RmapSmallRunRecipe.PortGalleryV1));

            Assert.That(new[] { first, repeat, wide, alternate }.All(value => value.Success), Is.True);
            Assert.That(first.PlanDigest, Is.EqualTo(repeat.PlanDigest));
            Assert.That(first.BaseDigest, Is.EqualTo(repeat.BaseDigest));
            Assert.That(first.PlanDigest, Is.Not.EqualTo(alternate.PlanDigest));
            Assert.That(wide.Request.Width, Is.EqualTo(48));
            Assert.That(first.Chunks, Has.Count.EqualTo(9));
            Assert.That(first.Chunks.All(value => value.Selections.Count == 6 && value.BaseCells.Count == 96), Is.True);
            Assert.That(first.Chunks.Select(value => value.Source.ChunkType).Where(value => value.HasValue)
                .Select(value => value.Value), Does.Contain(RmapPortChunkType.Type1));
            Assert.That(first.Chunks.Select(value => value.Source.ChunkType).Where(value => value.HasValue)
                .Select(value => value.Value), Does.Contain(RmapPortChunkType.Type2));
            Assert.That(first.Chunks.Select(value => value.Source.ChunkType).Where(value => value.HasValue)
                .Select(value => value.Value), Does.Contain(RmapPortChunkType.Type3));
            Assert.That(first.Chunks.Select(value => value.Source.ChunkType).Where(value => value.HasValue)
                .Select(value => value.Value), Does.Contain(RmapPortChunkType.Type4));
            Assert.That(first.Chunks.Count(value => value.Source.ChunkType == RmapPortChunkType.Type0), Is.GreaterThanOrEqualTo(2));
            Assert.That(first.Chunks.Any(value => value.Source.ChunkId == "T0_BREAKABLE_SECRET"), Is.True);
            Assert.That(first.Ports.Any(value => value.TraversalKind == RmapPortTraversalKind.Drop), Is.True);
            Assert.That(first.Ports.Any(value => value.TraversalKind == RmapPortTraversalKind.Climb), Is.True);
            Assert.That(first.TotalSelectionAttempts, Is.LessThanOrEqualTo(first.Chunks.Count + 2));
        }

        [Test]
        public void E03_E05_GlobalPortCoordinatesAndSelectedCellsStayInsideThePhysicalPlan()
        {
            var plan = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(1107, 36, 24,
                RmapSmallRunRecipe.PortGalleryV1));
            Assert.That(plan.Success, Is.True, plan.FailureSummary);
            foreach (RmapSmallRunPort port in plan.Ports)
            foreach (UnityEngine.Vector2Int cell in port.GlobalCells)
            {
                Assert.That(cell.x, Is.InRange(0, plan.Request.Width - 1), port.PortId);
                Assert.That(cell.y, Is.InRange(0, plan.Request.Height - 1), port.PortId);
                Assert.That(plan.IsSolid(cell.x, cell.y), Is.False, port.PortId + " is physically open at " + cell);
            }
            RmapSmallRunChunk climb = plan.Chunks.Single(value => value.Source.ChunkId == "T3_CLIMB");
            Assert.That(climb.Overlays, Has.Count.EqualTo(7));
            Assert.That(climb.Overlays.All(value => climb.GetBaseCell(value.Cell.X, value.Cell.Y) == RmapPatternBaseCell.Air), Is.True);
            Assert.That(plan.Chunks.SelectMany(value => value.BaseCells).Any(value => value == RmapPatternBaseCell.OneWayPlatform), Is.True);
        }

        [Test]
        public void E07_RejectsUnsupportedOrUnalignedRequestsWithoutReturningAStalePlan()
        {
            var unaligned = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(1107, 35, 24,
                RmapSmallRunRecipe.PortGalleryV1));
            var unsupported = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(1107, 60, 24,
                RmapSmallRunRecipe.PortGalleryV1));
            Assert.That(unaligned.Success, Is.False);
            Assert.That(unaligned.FailureSummary, Does.Contain("SIZE_NOT_CHUNK_ALIGNED"));
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.FailureSummary, Does.Contain("UNSUPPORTED_SIZE"));
            Assert.That(unaligned.Chunks, Is.Empty);
            Assert.That(unsupported.Chunks, Is.Empty);
        }
    }
}
#endif
