#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5HubShellTests
    {
        private static readonly Lazy<Sv5SpaceGraphPlan> DefaultBaseline=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.AttachSidepaths(Sv5SpaceLoopTests.DefaultPlan,
                Sv5SpaceSidepaths.Build(Sv5SpaceLoopTests.DefaultPlan,null,1)));
        private static readonly Lazy<Sv5SpaceGraphPlan> RepeatBaseline=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.AttachSidepaths(Sv5SpaceLoopTests.RepeatPlan,
                Sv5SpaceSidepaths.Build(Sv5SpaceLoopTests.RepeatPlan,null,1)));
        private static readonly Lazy<Sv5SpaceGraphPlan> Default=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.AttachHubShell(DefaultBaseline.Value,Sv5HubShell.Build(DefaultBaseline.Value)));
        private static readonly Lazy<Sv5SpaceGraphPlan> Repeat=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.AttachHubShell(RepeatBaseline.Value,Sv5HubShell.Build(RepeatBaseline.Value)));
        private static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        internal static Sv5SpaceGraphPlan DefaultBaselineForFix01=>DefaultBaseline.Value;
        internal static Sv5SpaceGraphPlan RepeatBaselineForFix01=>RepeatBaseline.Value;
        internal static Sv5SpaceGraphPlan DefaultForFix01=>Default.Value;
        internal static Sv5SpaceGraphPlan RepeatForFix01=>Repeat.Value;

        [Test,Timeout(600000)] public void H01_DefaultBuildAcceptsOneActualHubShell()
        {var p=Default.Value;Assert.That(p.Success,Is.True,Detail(p));Assert.That(p.HubShell.Success,Is.True,Detail(p));Assert.That(p.HubShell.HubId,Is.Not.Empty);}

        [Test,Timeout(600000)] public void H02_RepeatBuildAcceptsOneActualHubShell()
        {var p=Repeat.Value;Assert.That(p.Success,Is.True,Detail(p));Assert.That(p.HubShell.Success,Is.True,Detail(p));Assert.That(p.HubShell.HubId,Is.Not.Empty);}

        [Test] public void H03_FootprintAndInnerVolumeUseActualOneByOneCells()
        {
            var h=Default.Value.HubShell;Assert.That(h.Footprint.Width,Is.EqualTo(24));Assert.That(h.Footprint.Height,Is.EqualTo(40));
            var inner=new Sv5SpaceBounds(h.Footprint.X+6,h.Footprint.Y+5,12,30);
            Assert.That(Enumerable.Range(inner.X,inner.Width).SelectMany(x=>Enumerable.Range(inner.Y,inner.Height)
                .Select(y=>new Sv5SpecialWorldPoint(x,y))).All(point=>h.Cells.Any(c=>c.World.Equals(point)&&c.Role!=Sv5HubCellRole.Solid)),Is.True);
        }

        [Test] public void H04_ShellOccupancyHasUniqueAirSolidTreeAndNeckCells()
        {
            var cells=Default.Value.HubShell.Cells;Assert.That(cells.Count,Is.GreaterThanOrEqualTo(24*40));
            Assert.That(cells.Select(v=>v.World).Distinct().Count(),Is.EqualTo(cells.Count));
            Assert.That(cells.Any(v=>v.Role==Sv5HubCellRole.Air),Is.True);Assert.That(cells.Any(v=>v.Role==Sv5HubCellRole.Solid),Is.True);
            Assert.That(cells.Any(v=>v.Role==Sv5HubCellRole.ReservedTree),Is.True);Assert.That(cells.Any(v=>v.Role==Sv5HubCellRole.PortNeck),Is.True);
        }

        [Test] public void H05_EveryCellCarriesFourByFourOwnershipAndHubIdentity()
        {
            var h=Default.Value.HubShell;Assert.That(h.Cells.All(v=>v.HubId==h.HubId&&v.MicroX==v.World.X/4&&v.MicroY==v.World.Y/4),Is.True);
            Assert.That(h.Cells.All(v=>v.MicroPatternOwner.StartsWith(h.HubId+"@",StringComparison.Ordinal)),Is.True);
        }

        [Test] public void H06_SixSocketsCoverBothSidesThreeTiersAndSixToSevenCellApertures()
        {
            var sockets=Default.Value.HubShell.Sockets;Assert.That(sockets.Count,Is.EqualTo(6));
            foreach(var side in new[]{Sv5HubSide.Left,Sv5HubSide.Right})
            {var set=sockets.Where(v=>v.Side==side).ToArray();Assert.That(set.Length,Is.EqualTo(3));Assert.That(set.Select(v=>v.Tier),Is.EquivalentTo(new[]{Sv5HubTier.Low,Sv5HubTier.Mid,Sv5HubTier.High}));}
            Assert.That(sockets.All(v=>v.ApertureHeight>=6&&v.ApertureHeight<=7),Is.True);
        }

        [Test] public void H07_OnlyActiveSocketsBecomePortsAndUnusedSocketsDoNotCount()
        {
            var h=Default.Value.HubShell;Assert.That(h.Ports.Count,Is.InRange(4,6));
            Assert.That(h.Sockets.Where(v=>v.Active).Select(v=>v.Id),Is.EquivalentTo(h.Ports.Select(v=>v.SocketId)));
            Assert.That(h.Sockets.Count(v=>!v.Active),Is.EqualTo(6-h.Ports.Count));
        }

        [Test] public void H08_ConnectionsUseDistinctExternalRoomsAndSpaceGroups()
        {
            var c=Default.Value.HubShell.Connections;Assert.That(c.Count,Is.EqualTo(Default.Value.HubShell.Ports.Count));
            Assert.That(c.Select(v=>v.ExternalRoomId).Distinct(StringComparer.Ordinal).Count(),Is.EqualTo(c.Count));
            Assert.That(c.Select(v=>v.ExternalSpaceGroupId).Distinct(StringComparer.Ordinal).Count(),Is.EqualTo(c.Count));
        }

        [Test] public void H09_EachPortHasCardinalEvidenceToItsExternalAnchor()
        {
            foreach(var c in Default.Value.HubShell.Connections)
            {Assert.That(c.Centerline.First(),Is.EqualTo(c.PortAnchor));Assert.That(c.Centerline.Last(),Is.EqualTo(c.ExternalAnchor));
                Assert.That(c.Centerline.Zip(c.Centerline.Skip(1),(a,b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)).All(v=>v==1),Is.True);
                Assert.That(c.RouteVerified,Is.True);Assert.That(c.MovementWitness,Is.Not.Empty);}
        }

        [Test] public void H10_AllActivePortAnchorsShareOnePassableShellComponent()
        {
            var h=Default.Value.HubShell;var passable=new HashSet<Sv5SpecialWorldPoint>(h.Cells.Where(v=>v.Role!=Sv5HubCellRole.Solid).Select(v=>v.World));
            var anchors=h.Ports.Select(v=>v.Anchor).ToArray();var visited=new HashSet<Sv5SpecialWorldPoint>{anchors[0]};var queue=new Queue<Sv5SpecialWorldPoint>();queue.Enqueue(anchors[0]);
            while(queue.Count!=0){var at=queue.Dequeue();foreach(var next in new[]{new Sv5SpecialWorldPoint(at.X-1,at.Y),new Sv5SpecialWorldPoint(at.X+1,at.Y),new Sv5SpecialWorldPoint(at.X,at.Y-1),new Sv5SpecialWorldPoint(at.X,at.Y+1)})if(passable.Contains(next)&&visited.Add(next))queue.Enqueue(next);}
            Assert.That(anchors.All(visited.Contains),Is.True);
        }

        [Test] public void H11_TreeSlotIsContinuousReservationWithoutReadinessPromotion()
        {
            var h=Default.Value.HubShell;var columns=h.Cells.Where(v=>v.Role==Sv5HubCellRole.ReservedTree).GroupBy(v=>v.World.X);
            Assert.That(columns.Any(group=>group.Max(v=>v.World.Y)-group.Min(v=>v.World.Y)+1==group.Count()),Is.True);
            Assert.That(h.TreeGrabGeometryReady,Is.False);Assert.That(h.ComposedGeometryReady,Is.False);Assert.That(h.PlayerVerified,Is.False);
        }

        [Test] public void H12_ProtectedTypeZeroAndProgressionEvidenceRemainClear()
        {
            var h=Default.Value.HubShell;
            var protectedCells=new HashSet<Sv5SpecialWorldPoint>(h.ConstraintSets.SelectMany(set=>set.Cells));
            Assert.That(h.Cells.All(v=>!protectedCells.Contains(v.World)),Is.True);
            Assert.That(h.Connections.All(v=>!Sv5HubConnectionRouter.BodyIntersects(v.Centerline,protectedCells)&&
                !v.ProtectedOverlap&&!v.Type0Overlap&&!v.ProgressionBypass),Is.True);
            Assert.That(h.Diagnostics,Is.Empty);Assert.That(Default.Value.PhysicalProduct.Success,Is.True);
        }

        [Test,Timeout(600000)] public void H13_SameSeedAndInputReproduceHubCandidateCellAndConnectionDigests()
        {
            var again=Sv5HubShell.Build(DefaultBaseline.Value);Assert.That(again.Digest,Is.EqualTo(Default.Value.HubShell.Digest));
            Assert.That(again.CellDigest,Is.EqualTo(Default.Value.HubShell.CellDigest));
        }

        [Test] public void H14_CandidateWorkIsLocalAndFinalProductPreservesNineBySixTopology()
        {
            var h=Default.Value.HubShell;Assert.That(h.Performance.WholeWorldCopyPerCandidate,Is.Zero);Assert.That(h.Performance.WholeWorldBfsPerCandidate,Is.Zero);
            Assert.That(h.Performance.GlobalProductRuns,Is.EqualTo(1));Assert.That(Default.Value.ProjectionProofs.Count,Is.EqualTo(6));
            Assert.That(Default.Value.PhysicalMovement.SemanticDigest,Is.Not.EqualTo(DefaultBaseline.Value.PhysicalMovement.SemanticDigest));
            Assert.That(Default.Value.PhysicalProduct.Success,Is.True);Assert.That(Default.Value.PhysicalProduct.Proofs.Count,Is.EqualTo(6));
        }

        [Test,Timeout(600000)] public void H15_FinalDefaultRepeatExportsAreByteStableAndComplete()
        {
            string output=Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_11_FIX01");
            Sv5HubShellExport.WriteComparison(output,Default.Value,Repeat.Value);
            string first=Sv5HubShellExport.HubShellJson(Default.Value);string second=Sv5HubShellExport.HubShellJson(Default.Value);
            Assert.That(second,Is.EqualTo(first));
            foreach(string profile in new[]{"default","repeat"})foreach(string file in new[]{"hub_shell.json","hub_candidates.csv","hub_cells.csv",
                "hub_sockets.csv","hub_ports.csv","hub_connections.csv","hub_connection_cells.csv","hub_connection_checks.csv",
                "constraint_sources.json","hub_validation.json","preview/hub_shell.svg","preview/hub_connection_fix.svg"})
                Assert.That(File.Exists(Path.Combine(output,profile,file)),Is.True,profile+"/"+file);
            Assert.That(File.Exists(Path.Combine(output,"hub_comparison.json")),Is.True);
        }

        [Test] public void H16_HubModelsAndExportsContainNoRetiredGridIdentifiers()
        {
            var types=new[]{typeof(Sv5HubShellPlan),typeof(Sv5HubCandidate),typeof(Sv5HubCell),typeof(Sv5HubSocket),
                typeof(Sv5HubPort),typeof(Sv5HubConnection),typeof(Sv5HubConnectionCell),typeof(Sv5HubConnectionRoute),
                typeof(Sv5HubConstraintSet),typeof(Sv5HubPerformance)};
            Assert.That(types.SelectMany(v=>v.GetProperties()).All(v=>v.Name.IndexOf("SectorId",StringComparison.Ordinal)<0),Is.True);
            var payloads=new[]{Sv5HubShellExport.HubShellJson(Default.Value),Sv5HubShellExport.CandidatesCsv(Default.Value),
                Sv5HubShellExport.CellsCsv(Default.Value),Sv5HubShellExport.SocketsCsv(Default.Value),
                Sv5HubShellExport.PortsCsv(Default.Value),Sv5HubShellExport.ConnectionsCsv(Default.Value),
                Sv5HubShellExport.ConnectionCellsCsv(Default.Value),Sv5HubShellExport.ConnectionChecksCsv(Default.Value),
                Sv5HubShellExport.ValidationJson(Default.Value)};
            Assert.That(payloads.All(v=>v.IndexOf("sector_id",StringComparison.OrdinalIgnoreCase)<0 &&
                v.IndexOf("sector_index",StringComparison.OrdinalIgnoreCase)<0),Is.True);
        }

        private static string Detail(Sv5SpaceGraphPlan plan)=>"diagnostics="+string.Join(";",plan.HubShell.Diagnostics)+
            " candidates="+plan.HubShell.Performance.CandidateCount+" ports="+plan.HubShell.Ports.Count+
            " cells="+plan.HubShell.Cells.Count+" attempts="+plan.HubShell.Performance.LocalRouteAttempts+
            " targets="+plan.HubShell.Performance.IndexedEndpointCount+
            " rejections="+string.Join(",",plan.HubShell.Performance.RejectionHistogram.Select(pair=>pair.Key+":"+pair.Value))+
            " constraints="+string.Join(",",plan.HubShell.ConstraintSets.Select(set=>set.Category+":"+set.Cells.Count));
    }
}
#endif
