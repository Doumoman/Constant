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
    public sealed class Sv5SpaceSidepathTests
    {
        private static readonly Lazy<Sv5SidepathPlan> DefaultOne=new Lazy<Sv5SidepathPlan>(()=>Sv5SpaceSidepaths.Build(Sv5SpaceLoopTests.DefaultPlan,null,1));
        private static readonly Lazy<Sv5SidepathPlan> DefaultFour=new Lazy<Sv5SidepathPlan>(()=>Sv5SpaceSidepaths.Build(Sv5SpaceLoopTests.DefaultPlan,null,4));
        private static readonly Lazy<Sv5SpaceGraphPlan> Default=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.AttachSidepaths(Sv5SpaceLoopTests.DefaultPlan,DefaultOne.Value));
        private static readonly Lazy<Sv5SpaceGraphPlan> Repeat=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.AttachSidepaths(Sv5SpaceLoopTests.RepeatPlan,Sv5SpaceSidepaths.Build(Sv5SpaceLoopTests.RepeatPlan,null,1)));
        private static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));

        public static void WriteFinalEvidence()
        {
            string output=Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_10_SIDEPATH");
            Sv5SidepathExport.WriteComparison(output,Default.Value,Repeat.Value);
            Sv5SidepathExport.WriteWorkerDeterminism(output,DefaultOne.Value,DefaultFour.Value);
        }

        [Test,Timeout(600000)] public void T01_DefaultMeetsProductionOwnershipDensity()
        {
            var p=Default.Value;Assert.That(p.Success,Is.True,Detail(p));Assert.That(p.Sidepaths.AcceptedCount,Is.GreaterThanOrEqualTo(8));
            Assert.That(p.Sidepaths.ReturningCount,Is.GreaterThanOrEqualTo(5));Assert.That(p.Sidepaths.DistinctSpaceGroupCount,Is.GreaterThanOrEqualTo(6));
            Assert.That(p.Sidepaths.Links.GroupBy(v=>v.SpaceGroupId).Max(v=>v.Count()),Is.LessThanOrEqualTo(2));
        }
        [Test,Timeout(600000)] public void T02_RepeatMeetsProductionOwnershipDensity()
        {
            var p=Repeat.Value;Assert.That(p.Success,Is.True,Detail(p));Assert.That(p.Sidepaths.AcceptedCount,Is.GreaterThanOrEqualTo(8));
            Assert.That(p.Sidepaths.ReturningCount,Is.GreaterThanOrEqualTo(5));Assert.That(p.Sidepaths.DistinctSpaceGroupCount,Is.GreaterThanOrEqualTo(6));
        }
        [Test] public void T03_XWindowPairEnumerationIsCompleteAndOrdered()
        {
            var index=Default.Value.Sidepaths.EndpointIndex;var endpoints=index.Endpoints;
            Assert.That(endpoints,Is.Ordered.Using<Sv5SidepathEndpoint>((a,b)=>a.CompareTo(b)));
            int expected=0;for(int i=0;i<endpoints.Count;i++)for(int j=i+1;j<endpoints.Count&&endpoints[j].World.X-endpoints[i].World.X<=49;j++)expected++;
            Assert.That(index.Pairs.Count,Is.EqualTo(expected));Assert.That(index.Pairs.All(v=>v.Dx<=49),Is.True);
        }
        [Test] public void T04_EligiblePairsRespectManhattanBoundary()
        {Assert.That(Default.Value.Sidepaths.EndpointIndex.Pairs.Where(v=>v.Eligible).All(v=>v.Manhattan<=49),Is.True);}
        [Test] public void T05_NormalizedEndpointPairsHaveNoDuplicates()
        {
            var pairs=Default.Value.Sidepaths.EndpointIndex.Pairs;
            Assert.That(pairs.Select(v=>v.PairKey).Distinct(StringComparer.Ordinal).Count(),Is.EqualTo(pairs.Count));
            Assert.That(pairs.All(v=>v.PairKey==Sv5SidepathEndpointPair.Key(v.From.EndpointId,v.To.EndpointId)),Is.True);
        }
        [Test] public void T06_SameRoomAndDirectPairsAreExcludedBeforeGeneration()
        {
            var pairs=Default.Value.Sidepaths.EndpointIndex.Pairs;
            Assert.That(pairs.Where(v=>v.From.RoomId==v.To.RoomId).All(v=>v.RejectionReason=="SAME_ROOM"),Is.True);
            Assert.That(pairs.Where(v=>v.RejectionReason=="DIRECT_ROOM_PAIR").All(v=>!v.Eligible),Is.True);
            Assert.That(pairs.Where(v=>v.Eligible).All(v=>v.From.RoomId!=v.To.RoomId),Is.True);
        }
        [Test] public void T07_OwnershipDistributionUsesActualConnectionGroups()
        {
            var p=Default.Value;var ids=new HashSet<string>(p.Connections.Select(v=>v.Id),StringComparer.Ordinal);
            Assert.That(p.Sidepaths.Links.All(v=>ids.Contains(v.SpaceGroupId)),Is.True);
            Assert.That(p.Sidepaths.Links.GroupBy(v=>v.SpaceGroupId).All(v=>v.Count()<=2),Is.True);
        }
        [Test] public void T08_CenterlineUsesTwentyToFiftyActualAirCells()
        {
            var p=Default.Value.Sidepaths;foreach(var link in p.Links){Assert.That(link.Centerline.Count,Is.InRange(20,50));
                Assert.That(link.Centerline.Distinct().Count(),Is.EqualTo(link.Centerline.Count));
                Assert.That(link.Centerline.All(v=>Sv5LoopTopology.ValueAt(p.FinalOccupancy,v)==Sv5InfillCellValue.Air),Is.True);}
        }
        [Test] public void T09_OrdinaryMovementHasHeadroomAndSupport()
        {
            var p=Default.Value.Sidepaths;foreach(var link in p.Links)for(int i=0;i<link.Centerline.Count;i++)
            {var v=link.Centerline[i];Assert.That(Sv5LoopTopology.ValueAt(p.FinalOccupancy,new Sv5SpecialWorldPoint(v.X,v.Y+1)),Is.EqualTo(Sv5InfillCellValue.Air));
                if(Sv5SpaceSidepaths.MovementRole(link.Centerline,i)==Sv5SidepathMovementRole.SupportedFoot)
                    Assert.That(Sv5LoopTopology.ValueAt(p.FinalOccupancy,new Sv5SpecialWorldPoint(v.X,v.Y-1)),Is.EqualTo(Sv5InfillCellValue.Solid));}
        }
        [Test] public void T10_PathsAreIrregularWithoutSawtoothOrLongVerticalTube()
        {
            foreach(var link in Default.Value.Sidepaths.Links){var path=link.Centerline;Assert.That(link.DirectionChanges,Is.GreaterThanOrEqualTo(2));
                Assert.That(Enumerable.Range(0,Math.Max(0,path.Count-2)).Any(i=>path[i].X==path[i+1].X&&path[i+1].X==path[i+2].X),Is.False);
                var dirs=path.Zip(path.Skip(1),(a,b)=>(b.X-a.X)+":"+(b.Y-a.Y)).ToArray();var runs=new List<int>();int n=0;string prior=null;
                foreach(string d in dirs){if(prior!=null&&d!=prior){runs.Add(n);n=0;}prior=d;n++;}runs.Add(n);
                Assert.That(runs.Distinct().Count(),Is.GreaterThanOrEqualTo(2));Assert.That(Enumerable.Range(0,Math.Max(0,runs.Count-2)).Any(i=>runs[i]==1&&runs[i+1]==1&&runs[i+2]==1),Is.False);}
        }
        [Test] public void T11_ReturningAndDeadEndSearchHaveTypedRecoverySemantics()
        {
            var p=Default.Value.Sidepaths;Assert.That(p.Candidates.Any(v=>v.Kind==Sv5SidepathKind.Returning),Is.True);
            Assert.That(p.Searches.Count,Is.EqualTo(p.EndpointIndex.Endpoints.Count));Assert.That(p.Searches.All(v=>v.Expansions<=26),Is.True);
            Assert.That(p.Links.Where(v=>v.Kind==Sv5SidepathKind.Returning).All(v=>v.Rejoins&&v.Returnable&&v.FromRoomId!=v.ToRoomId),Is.True);
            Assert.That(p.Links.Where(v=>v.Kind==Sv5SidepathKind.DeadEnd).All(v=>!v.Rejoins&&!v.Returnable&&v.ToRoomId==string.Empty),Is.True);
        }
        [Test] public void T12_ProtectionAndPerCandidatePerformanceStayLocal()
        {
            var p=Default.Value;Assert.That(p.Sidepaths.Links.All(v=>v.BypassCount==0&&v.LegalStatesChecked==9&&v.ResourceOrdersChecked==6),Is.True);
            Assert.That(p.Sidepaths.Performance.WholeWorldCopyPerCandidate,Is.Zero);Assert.That(p.Sidepaths.Performance.WholeWorldBfsPerCandidate,Is.Zero);
            Assert.That(p.PhysicalProduct.Success,Is.True);
        }
        [Test] public void T13_WorkerOneAndFourProduceIdenticalDigest()
        {
            Assert.That(DefaultOne.Value.Digest,Is.EqualTo(DefaultFour.Value.Digest));
            Assert.That(DefaultOne.Value.Performance.WorkerCount,Is.EqualTo(1));Assert.That(DefaultFour.Value.Performance.WorkerCount,Is.EqualTo(4));
            TestContext.WriteLine("WORKER_TIMING|1|"+DefaultOne.Value.Performance.TimingsMilliseconds["candidate_generation_ms"].ToString("0.000",System.Globalization.CultureInfo.InvariantCulture));
            TestContext.WriteLine("WORKER_TIMING|4|"+DefaultFour.Value.Performance.TimingsMilliseconds["candidate_generation_ms"].ToString("0.000",System.Globalization.CultureInfo.InvariantCulture));
        }
        [Test,Timeout(600000)] public void T14_RepeatedParallelRunIsDeterministicAndFinalEvidenceIsPresent()
        {
            var second=Sv5SpaceSidepaths.Build(Sv5SpaceLoopTests.DefaultPlan,null,4);Assert.That(second.Digest,Is.EqualTo(DefaultFour.Value.Digest));
            string output=Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_10_SIDEPATH");
            foreach(string profile in new[]{"default","repeat"})foreach(string file in new[]{"sidepaths.json","sidepath_candidates.csv","sidepath_links.csv",
                "sidepath_cells.csv","sidepath_changed_cells.csv","protected_cells.csv","direct_room_pairs.csv","sidepath_checks.csv","endpoint_index.csv","endpoint_pairs.csv","sidepath_search_performance.json",
                "sidepath_validation.json","preview/sidepaths.svg"})Assert.That(File.Exists(Path.Combine(output,profile,file)),Is.True,profile+"/"+file);
            Assert.That(File.Exists(Path.Combine(output,"worker_determinism.json")),Is.True);
        }
        private static string Detail(Sv5SpaceGraphPlan p)=>"diagnostics="+string.Join(";",p.Sidepaths.Diagnostics)+" endpoints="+
            p.Sidepaths.EndpointIndex.Endpoints.Count+" pairs="+p.Sidepaths.EndpointIndex.Pairs.Count+" candidates="+p.Sidepaths.Candidates.Count+
            " accepted="+p.Sidepaths.AcceptedCount+" groups="+p.Sidepaths.DistinctSpaceGroupCount+" rejections="+
            string.Join(";",p.Sidepaths.Rejections.Select(v=>v.Key+"="+v.Value));
    }
}
#endif
