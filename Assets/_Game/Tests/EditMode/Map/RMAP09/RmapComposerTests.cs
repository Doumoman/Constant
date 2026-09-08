#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.Rmap09
{
    [Category("RMAP09")]
    public sealed class RmapComposerTests
    {
        [Test]
        public void A22_ComposesSixRmap07SelectionsIntoACompleteProtected12x8Type3Base()
        {
            RmapComposerResult result = RmapComposer.Compose(RmapComposer.CreateFixtureRequest());

            Assert.That(result.Success, Is.True, result.FailureSummary);
            Assert.That(result.AttemptCount, Is.EqualTo(3));
            RmapComposerComposition composition = result.Composition;
            Assert.That(composition.BaseCells.Count, Is.EqualTo(96));
            Assert.That(composition.Selections.Count, Is.EqualTo(6));
            Assert.That(composition.Selections.Select(value => value.Proposal.StableKey).Distinct().Count(), Is.EqualTo(6));
            Assert.That(composition.Selections.Select(value => value.FinalCells.Count), Is.All.EqualTo(16));
            Assert.That(composition.PortChunk.ChunkId, Is.EqualTo("T3_CLIMB"));
            Assert.That(composition.PortChunk.ChunkType, Is.EqualTo(RmapPortChunkType.Type3));
            Assert.That(composition.PortChunk.SourcePlacement.CandidateId,
                Is.EqualTo("RMAP07_991204FBA2B6"));
            Assert.That(composition.PortChunk.SourcePlacement.OriginX, Is.EqualTo(4));
            Assert.That(composition.PortChunk.SourcePlacement.OriginY, Is.EqualTo(2));
            Assert.That(composition.BaseDigest, Has.Length.EqualTo(64));
            Assert.That(composition.CompositionDigest, Has.Length.EqualTo(64));
            Assert.That(composition.PortChunk.Ports.Where(value => value.Required).Select(value => value.PortId),
                Is.EquivalentTo(composition.RequiredPortIds));
            foreach (RmapEdgePort port in composition.PortChunk.Ports.Where(value => value.Required))
            foreach (int coordinate in port.OpenCells)
            {
                RmapPortCell edge = RmapPortCatalog.ToChunkCell(port.Side, coordinate);
                Assert.That(composition.GetBaseCell(edge.X, edge.Y), Is.EqualTo(RmapPatternBaseCell.Air),
                    port.PortId + " must stay open at " + coordinate);
            }
            Assert.That(composition.PortChunk.Ports.Where(value => value.Required).All(value =>
                value.TraversalKind == RmapPortTraversalKind.Walk || value.TraversalKind == RmapPortTraversalKind.Climb), Is.True);
            Assert.That(composition.PortChunk.Ports.Where(value => value.Required).All(value =>
                value.AllowsEntry || value.AllowsExit), Is.True);
            Assert.That(RmapPortCatalog.BuildFixture().ProfileDigest,
                Is.EqualTo(GeneratedTraversalProfileCatalog.Create().Digest));
        }

        [Test]
        public void A23_OverlayIsSeparateFromTheBaseAndKeepsTheClimbClearanceUntouched()
        {
            RmapComposerComposition composition = RmapComposer.Compose(
                RmapComposer.CreateFixtureRequest()).Composition;

            Assert.That(composition.Overlays.Count, Is.EqualTo(7));
            Assert.That(composition.Overlays.Select(value => value.Kind),
                Is.All.EqualTo(RmapComposerOverlayKind.Ladder));
            Assert.That(composition.Overlays.Select(value => value.Cell.X).Distinct(), Is.EquivalentTo(new[] { 5 }));
            Assert.That(composition.Overlays.Select(value => value.Cell.Y), Is.EqualTo(Enumerable.Range(1, 7)));
            foreach (RmapComposerOverlayCell overlay in composition.Overlays)
                Assert.That(composition.GetBaseCell(overlay.Cell.X, overlay.Cell.Y),
                    Is.EqualTo(RmapPatternBaseCell.Air), "Overlay may not convert base terrain.");
            Assert.That(composition.OptionalRouteEvidence,
                Is.EqualTo("PROFILE_CONTEXT_UNKNOWN_NOT_PROMOTED_TO_REQUIRED_PASS"));
        }

        [Test]
        public void A24_RecordsRejectedPlansAndStopsAtTheDeclaredBoundWithoutCarvingOrRepairing()
        {
            RmapComposerResult result = RmapComposer.Compose(RmapComposer.CreateFixtureRequest());

            Assert.That(result.AttemptTraces[0].Accepted, Is.False);
            Assert.That(result.AttemptTraces[0].Rejections.Any(value =>
                value.StartsWith("REQUIRED_PORT_BLOCKED:P_T3_L", System.StringComparison.Ordinal)), Is.True);
            Assert.That(result.AttemptTraces[1].Accepted, Is.False);
            Assert.That(result.AttemptTraces[1].Rejections.Any(value =>
                value.StartsWith("REQUIRED_PORT_BLOCKED:P_T3_U", System.StringComparison.Ordinal)), Is.True);
            Assert.That(result.AttemptTraces[2].Accepted, Is.True);
            Assert.That(result.AttemptTraces[2].AttemptId, Is.EqualTo("accepted-type3-ladder"));
            Assert.That(result.AttemptTraces.Select(value => value.AttemptNumber), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(result.Composition.BaseCells.Count(value => value == RmapPatternBaseCell.Solid), Is.EqualTo(18));

            RmapComposerRequest fixture = RmapComposer.CreateFixtureRequest();
            var oneAttempt = new RmapComposerRequest(fixture.Seed, fixture.TargetChunkId, 1, fixture.AttemptPlans);
            RmapComposerResult bounded = RmapComposer.Compose(oneAttempt);
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.AttemptCount, Is.EqualTo(1));
            Assert.That(bounded.AttemptTraces[0].AttemptId, Is.EqualTo("blocked-left-spine"));
            Assert.That(bounded.FailureSummary, Does.Contain("REQUIRED_PORT_BLOCKED:P_T3_L"));
        }
    }
}
#endif
