#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5SpaceLoopTopologyFix01Tests
    {
        private static Sv5SpaceGraphPlan Plan=>Sv5SpaceLoopTests.DefaultPlan;

        [Test] public void T01_BaselineOccupancyHasUniqueActualValuesAndProvenance()
        {
            var occupancy=Plan.Loops.Topology.BaselineOccupancy;
            Assert.That(occupancy.Keys.Distinct().Count(),Is.EqualTo(occupancy.Count));
            Assert.That(occupancy.Values.All(c=>c.Value==Sv5InfillCellValue.Air ||
                c.Value==Sv5InfillCellValue.Solid || c.Value==Sv5InfillCellValue.Unknown),Is.True);
            Assert.That(occupancy.Values.Any(c=>c.Provenance.Contains("HOST_")),Is.True);
            Assert.That(occupancy.Values.Any(c=>c.Provenance.Contains("RESERVATION_")),Is.True);
        }

        [Test] public void T02_SupportedFootNodesReconstructFromBodyHeadAndSupport()
        {
            var topology=Plan.Loops.Topology;
            var rebuilt=Sv5LoopTopology.SupportedFootNodes(topology.BaselineOccupancy);
            Assert.That(rebuilt,Is.EquivalentTo(topology.BaselineFootNodes));
            foreach(var foot in rebuilt)
            {
                Assert.That(Sv5LoopTopology.ValueAt(topology.BaselineOccupancy,foot),Is.EqualTo(Sv5InfillCellValue.Air));
                Assert.That(Sv5LoopTopology.ValueAt(topology.BaselineOccupancy,
                    new Sv5SpecialWorldPoint(foot.X,foot.Y+1)),Is.EqualTo(Sv5InfillCellValue.Air));
                Assert.That(Sv5LoopTopology.ValueAt(topology.BaselineOccupancy,
                    new Sv5SpecialWorldPoint(foot.X,foot.Y-1)),Is.EqualTo(Sv5InfillCellValue.Solid));
            }
        }

        [Test] public void T03_EachCostIsRecomputedByBfsOnTheMatchingOccupancy()
        {
            var topology=Plan.Loops.Topology;
            foreach(var link in Plan.Loops.Links)
            {
                var proof=topology.Proofs.Single(p=>p.LoopId==link.Id);
                Assert.That(Sv5LoopTopology.ShortestCost(link.FootPath.First(),link.FootPath.Last(),
                    topology.BaselineFootNodes),Is.EqualTo(proof.BaselineCost));
                var overlay=new Dictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(topology.BaselineOccupancy);
                foreach(var cell in link.Cells) overlay[cell.World]=new Sv5LoopOccupancyCell(cell.World,
                    cell.FinalValue,"TEST_LOOP_RECONSTRUCTION",link.Id);
                var finalFeet=Sv5LoopTopology.SupportedFootNodes(
                    new ReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(overlay));
                Assert.That(Sv5LoopTopology.ShortestCost(link.FootPath.First(),link.FootPath.Last(),finalFeet),
                    Is.EqualTo(proof.FinalCost));
            }
        }

        [Test] public void T04_RoomMetricsAndBridgesComeFromTheExportedEdges()
        {
            var topology=Plan.Loops.Topology;
            Assert.That(topology.BaselineEdges.Select(e=>e.Id).Distinct().Count(),Is.EqualTo(topology.BaselineEdges.Count));
            Assert.That(topology.BaselineEdges.All(e=>e.FromVertex!=e.ToVertex),Is.True);
            Assert.That(topology.FinalEdges.Select(e=>e.Id).Distinct().Count(),Is.EqualTo(topology.FinalEdges.Count));
            Assert.That(topology.FinalEdges.All(e=>e.FromVertex!=e.ToVertex),Is.True);
            var before=Sv5LoopTopology.Measure(topology.BaselineEdges);
            var after=Sv5LoopTopology.Measure(topology.FinalEdges);
            Assert.That(before.Token,Is.EqualTo(topology.Before.Token));
            Assert.That(after.Token,Is.EqualTo(topology.After.Token));
            Assert.That(after.CycleRank-before.CycleRank,Is.EqualTo(Plan.Loops.AcceptedCount));
        }

        [Test] public void T05_InternalPathsHaveOnlyEndpointContactsAndTwoNewAirCells()
        {
            var topology=Plan.Loops.Topology;
            foreach(var link in Plan.Loops.Links)
            {
                var proof=topology.Proofs.Single(p=>p.LoopId==link.Id);
                var contacts=link.FootPath.Where(topology.BaselineFootNodes.Contains).ToArray();
                Assert.That(contacts,Is.EqualTo(new[]{link.FootPath.First(),link.FootPath.Last()}));
                int newAir=link.FootPath.Count(p=>link.Cells.Any(c=>c.World.Equals(p) &&
                    c.SourceValue!=Sv5InfillCellValue.Air && c.FinalValue==Sv5InfillCellValue.Air));
                Assert.That(newAir,Is.EqualTo(proof.NewlyCarvedAir));
                Assert.That(newAir,Is.GreaterThanOrEqualTo(2));
            }
        }

        [Test] public void T06_TypedLoopEdgeRemovalLeavesTheBaselineAlternate()
        {
            var topology=Plan.Loops.Topology;
            foreach(var link in Plan.Loops.Links)
            {
                var edge=topology.FinalEdges.Single(e=>e.Id==link.Id);
                Assert.That(edge.Kind,Is.EqualTo("LOOP")); Assert.That(edge.LoopId,Is.EqualTo(link.Id));
                Assert.That(Sv5LoopTopology.Connected(topology.FinalEdges,edge.FromVertex,edge.ToVertex,edge.Id),Is.True);
            }
        }

        [Test] public void T07_RemovingHeadroomInvalidatesTheReconstructedFootNode()
        {
            var topology=Plan.Loops.Topology; var foot=topology.BaselineFootNodes.First();
            var changed=new Dictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(topology.BaselineOccupancy);
            var head=new Sv5SpecialWorldPoint(foot.X,foot.Y+1);
            changed[head]=new Sv5LoopOccupancyCell(head,Sv5InfillCellValue.Solid,"NEGATIVE_HEADROOM",string.Empty);
            var rebuilt=Sv5LoopTopology.SupportedFootNodes(
                new ReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell>(changed));
            Assert.That(rebuilt.Contains(foot),Is.False);
        }

        [Test] public void T08_TopologyExportsAreDeterministicAndDoNotTrustStoredLabels()
        {
            string first=Sv5LoopExport.TopologyProofsCsv(Plan);
            string second=Sv5LoopExport.TopologyProofsCsv(Plan);
            Assert.That(second,Is.EqualTo(first));
            Assert.That(Plan.Loops.Topology.Proofs.All(p=>p.BaselineAlternatePath &&
                p.EdgeRemovalAlternatePath && p.CycleDelta==1),Is.True);
            Assert.That(Plan.Loops.Links.All(l=>l.Kind==Sv5SpaceLoops.Classify(l.BaselineCost,l.NewCost)),Is.True);
        }

        [Test] public void T09_FinalUnionPreservesNineStatesSixOrdersAndZeroBypass()
        {
            Assert.That(Plan.PhysicalMovement.Success,Is.True);
            Assert.That(Plan.PhysicalProduct.Success,Is.True);
            Assert.That(Plan.Loops.Links.All(l=>l.LegalStatesChecked==9 && l.ResourceOrdersChecked==6 &&
                l.BypassCount==0),Is.True);
        }
    }
}
#endif
