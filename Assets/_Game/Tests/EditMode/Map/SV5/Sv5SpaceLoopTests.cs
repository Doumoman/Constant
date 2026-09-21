#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5SpaceLoopTests
    {
        private static readonly Lazy<Sv5SpaceGraphPlan> Default=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.PlanWithLoops(Sv5RouteStatePolicyTests.RepresentativePlanForFix01,1304));
        private static readonly Lazy<Sv5SpaceGraphPlan> Repeat=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.PlanWithLoops(Sv5RouteStatePolicyTests.RepresentativePlanForFix01,1304,
                Sv5SpaceDiversity.RepeatProfile()));
        private static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        internal static Sv5SpaceGraphPlan DefaultPlan=>Default.Value;
        internal static Sv5SpaceGraphPlan RepeatPlan=>Repeat.Value;

        [Test,Timeout(1200000)] public void T01_DefaultProductionPlanMeetsHardMinimumAndFortyEightByThirtyTwoSpread()
        {
            var p=Default.Value; string detail=Detail(p);
            string debug=Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_09_FIX01/_work/debug"); Directory.CreateDirectory(debug);
            File.WriteAllText(Path.Combine(debug,"baseline_occupancy.csv"),Sv5LoopExport.OccupancyCsv(p.Loops.Topology.BaselineOccupancy));
            File.WriteAllText(Path.Combine(debug,"loop_candidates.csv"),Sv5LoopExport.CandidatesCsv(p));
            File.WriteAllText(Path.Combine(debug,"loop_links.csv"),Sv5LoopExport.LinksCsv(p));
            File.WriteAllText(Path.Combine(debug,"loop_cells.csv"),Sv5LoopExport.CellsCsv(p));
            Assert.That(p.Success,Is.True,detail); Assert.That(p.Loops.Success,Is.True,detail);
            Assert.That(p.Loops.AcceptedCount,Is.InRange(16,48)); Assert.That(p.Loops.DistinctSectorCount,Is.GreaterThanOrEqualTo(12));
            Assert.That(p.Loops.Links.GroupBy(l=>l.SectorId).Max(g=>g.Count()),Is.LessThanOrEqualTo(3));
        }

        [Test,Timeout(1200000)] public void T02_RepeatProductionPlanMeetsHardMinimumAndSpread()
        {
            var p=Repeat.Value; string detail=Detail(p);
            Assert.That(p.Success,Is.True,detail); Assert.That(p.Loops.AcceptedCount,Is.InRange(16,48),detail);
            Assert.That(p.Loops.DistinctSectorCount,Is.GreaterThanOrEqualTo(12),detail);
            Assert.That(p.Loops.Links.Max(l=>l.Centerline.Count),Is.LessThanOrEqualTo(24));
        }

        [Test,Timeout(1200000)] public void T03_SameSeedAndProfileReproduceCandidateCellDigestAndExportBytes()
        {
            var first=Default.Value;
            var second=Sv5SpaceGraphPlanner.PlanWithLoops(Sv5RouteStatePolicyTests.RepresentativePlanForFix01,1304);
            Assert.That(second.Loops.Candidates.Select(c=>c.Token),Is.EqualTo(first.Loops.Candidates.Select(c=>c.Token)));
            Assert.That(second.Loops.Links.Select(l=>l.Token),Is.EqualTo(first.Loops.Links.Select(l=>l.Token)));
            Assert.That(second.Loops.Cells.Select(c=>c.Token),Is.EqualTo(first.Loops.Cells.Select(c=>c.Token)));
            Assert.That(second.Loops.Digest,Is.EqualTo(first.Loops.Digest)); Assert.That(second.Digest,Is.EqualTo(first.Digest));
            Assert.That(Sv5LoopExport.LinksCsv(second),Is.EqualTo(Sv5LoopExport.LinksCsv(first)));
            Assert.That(Sv5LoopExport.CellsCsv(second),Is.EqualTo(Sv5LoopExport.CellsCsv(first)));
        }

        [Test] public void T04_InclusiveLengthAcceptsTwentyThreeAndTwentyFourAndRejectsTwentyFive()
        {
            Sv5SpecialWorldPoint[] Flat(int count)=>Enumerable.Range(0,count).Select(x=>new Sv5SpecialWorldPoint(x,20)).ToArray();
            Assert.That(Sv5SpaceLoops.ValidateSupportedPath(Flat(23)),Is.Empty);
            Assert.That(Sv5SpaceLoops.ValidateSupportedPath(Flat(24)),Is.Empty);
            Assert.That(Sv5SpaceLoops.ValidateSupportedPath(Flat(25)),Does.Contain("OVER_MAXIMUM|25/24"));
        }

        [Test] public void T05_SupportedPlusOneIsAllowedAndPlusTwoIsRejectedWithoutDiagonalTeleport()
        {
            var plusOne=new[]{new Sv5SpecialWorldPoint(10,10),new Sv5SpecialWorldPoint(11,11),new Sv5SpecialWorldPoint(12,11)};
            Assert.That(Sv5SpaceLoops.ValidateSupportedPath(plusOne),Is.Empty);
            Assert.That(Sv5SpaceInfill.CardinalCenterline(plusOne).Count,Is.EqualTo(4));
            var plusTwo=new[]{new Sv5SpecialWorldPoint(10,10),new Sv5SpecialWorldPoint(11,12),new Sv5SpecialWorldPoint(12,12)};
            Assert.That(Sv5SpaceLoops.ValidateSupportedPath(plusTwo),Does.Contain("UNSUPPORTED_PLUS_TWO"));
        }

        [Test,Timeout(1200000)] public void T06_LoopsUseActualAirSupportAndExplicitSolidOpeningOverrides()
        {
            var p=Default.Value; var source=p.Infill.Cells.ToDictionary(c=>c.World,c=>c);
            Assert.That(p.Loops.Cells.Any(c=>c.Role==Sv5LoopCellRole.ApertureOverride),Is.True);
            foreach(var link in p.Loops.Links)
            {
                Assert.That(link.Centerline.Count,Is.InRange(4,24));
                Assert.That(link.Centerline.Zip(link.Centerline.Skip(1),(a,b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)),Is.All.EqualTo(1));
                foreach(var foot in link.FootPath)
                {
                    Assert.That(link.Cells.Single(c=>c.World.Equals(foot)).FinalValue,Is.EqualTo(Sv5InfillCellValue.Air));
                    Assert.That(link.Cells.Single(c=>c.World.Equals(new Sv5SpecialWorldPoint(foot.X,foot.Y+1))).FinalValue,Is.EqualTo(Sv5InfillCellValue.Air));
                    Assert.That(link.Cells.Single(c=>c.World.Equals(new Sv5SpecialWorldPoint(foot.X,foot.Y-1))).FinalValue,Is.EqualTo(Sv5InfillCellValue.Solid));
                }
                foreach(var opening in link.Cells.Where(c=>c.Role==Sv5LoopCellRole.ApertureOverride))
                    Assert.That(source[opening.World].Value,Is.EqualTo(Sv5InfillCellValue.Solid));
            }
        }

        [Test,Timeout(1200000)] public void T07_EndpointsAreDistinctBranchesAndDirectParentChildPairsAreExcluded()
        {
            var p=Default.Value; var rooms=p.Infill.Rooms.ToDictionary(r=>r.Id,r=>r);
            foreach(var link in p.Loops.Links)
            {
                string a=link.FromRoomId,b=link.ToRoomId;
                Assert.That(a,Is.Not.EqualTo(b)); Assert.That(rooms[a].Parent,Is.Not.EqualTo(b)); Assert.That(rooms[b].Parent,Is.Not.EqualTo(a));
                Assert.That(rooms[a].Host,Is.EqualTo(rooms[b].Host));
                var from=p.Infill.Cells.Single(c=>c.World.Equals(link.FootPath.First()));
                var to=p.Infill.Cells.Single(c=>c.World.Equals(link.FootPath.Last()));
                Assert.That(from.Value,Is.EqualTo(Sv5InfillCellValue.Air)); Assert.That(to.Value,Is.EqualTo(Sv5InfillCellValue.Air));
                Assert.That(from.Owner,Is.EqualTo(link.FromApertureOwner)); Assert.That(to.Owner,Is.EqualTo(link.ToApertureOwner));
                Assert.That(Base(from.Owner),Is.EqualTo(a)); Assert.That(Base(to.Owner),Is.EqualTo(b));
            }
            var child=rooms.Values.First(r=>rooms.ContainsKey(r.Parent));
            Assert.That(Sv5SpaceLoops.PairExclusionReason(rooms[child.Parent],child,rooms[child.Parent].Id,child.Id),Is.EqualTo("DIRECT_PARENT_CHILD"));
        }

        [Test,Timeout(1200000)] public void T08_CoreTypeZeroGatePortAndForeignReservationsRemainUntouched()
        {
            var p=Default.Value; var core=p.Core.CoreCells.Select(c=>c.World).ToHashSet();
            var type0=p.Core.RouteSource.Secrets.SelectMany(s=>s.Chunks).SelectMany(chunk=>Enumerable.Range(0,8)
                .SelectMany(y=>Enumerable.Range(0,12).Select(x=>new Sv5SpecialWorldPoint(chunk.X*12+x,chunk.Y*8+y)))).ToHashSet();
            var gate=p.Gates.SelectMany(g=>g.BlockingCells.Concat(g.BlockingFaces.SelectMany(f=>new[]{f.First,f.Second}))).ToHashSet();
            var ports=p.Ports.SelectMany(v=>v.BoundaryCells.Concat(new[]{v.Anchor})).ToHashSet();
            foreach(var c in p.Loops.Cells.Where(c=>c.SourceValue==Sv5InfillCellValue.Unknown))
            { Assert.That(core.Contains(c.World),Is.False); Assert.That(type0.Contains(c.World),Is.False); Assert.That(gate.Contains(c.World),Is.False); Assert.That(ports.Contains(c.World),Is.False); }
            Assert.That(p.Loops.Links.Sum(l=>l.BypassCount),Is.Zero);
        }

        [Test,Timeout(1200000)] public void T09_EveryAcceptedLinkHasAlternatePathAndAddsExactlyOneCycleWithoutStubOrDuplicate()
        {
            var p=Default.Value;
            Assert.That(p.Loops.Links,Is.All.Matches<Sv5LoopLink>(l=>l.AlternatePathExists && l.CycleDelta==1));
            Assert.That(p.Loops.FinalCycleRank-p.Loops.BaselineCycleRank,Is.EqualTo(p.Loops.AcceptedCount));
            Assert.That(p.Loops.Links.Select(l=>string.Join("|",new[]{l.FootPath.First().ToString(),l.FootPath.Last().ToString()}
                .OrderBy(v=>v))).Distinct().Count(),Is.EqualTo(p.Loops.AcceptedCount));
            Assert.That(p.Loops.Cells.GroupBy(c=>c.World).Any(g=>g.Select(c=>c.LoopId).Distinct().Count()>1),Is.False);
        }

        [Test] public void T10_StrictActualCostReductionAloneClassifiesRandomShortcut()
        {
            Assert.That(Sv5SpaceLoops.Classify(20,19),Is.EqualTo(Sv5LoopKind.RandomShortcut));
            Assert.That(Sv5SpaceLoops.Classify(20,20),Is.EqualTo(Sv5LoopKind.Loop));
            Assert.That(Sv5SpaceLoops.Classify(20,21),Is.EqualTo(Sv5LoopKind.Loop));
            foreach(var link in Default.Value.Loops.Links)
                Assert.That(link.Kind,Is.EqualTo(link.NewCost<link.BaselineCost ? Sv5LoopKind.RandomShortcut : Sv5LoopKind.Loop));
        }

        [Test,Timeout(1200000)] public void T11_FourByFourMaskedPayloadReconstructsActualCellsWithTwoContourVariants()
        {
            foreach(var p in new[]{Default.Value,Repeat.Value})
            {
                var rebuilt=Sv5InfillPatterns.Reconstruct(p.Loops.Instances);
                Assert.That(rebuilt.Count,Is.EqualTo(p.Loops.Cells.Count));
                foreach(var c in p.Loops.Cells) Assert.That(rebuilt[c.World],Is.EqualTo(c.FinalValue));
                Assert.That(p.Loops.Patterns,Is.All.Matches<Sv5InfillPattern>(v=>v.Values.Count==16 && v.Mask!=0));
                Assert.That(p.Loops.Links.Select(l=>l.ContourVariant).Distinct(),Is.EquivalentTo(new[]{"SHELF","SCALLOP"}));
            }
        }

        [Test,Timeout(1200000)] public void T12_AllLegalFsmStatesAndSixResourceOrdersRemainPhysicallyValid()
        {
            foreach(var p in new[]{Default.Value,Repeat.Value})
            {
                Assert.That(p.PhysicalMovement.Success,Is.True,Detail(p)); Assert.That(p.PhysicalProduct.Success,Is.True,Detail(p));
                Assert.That(p.PhysicalProduct.Proofs.Count,Is.EqualTo(6));
                Assert.That(p.PhysicalProduct.Matrix.Select(r=>r.ResourceOrder).Distinct().Count(),Is.EqualTo(6));
                Assert.That(p.PhysicalProduct.Diagnostics,Is.Empty); Assert.That(p.Loops.Links.All(l=>l.LegalStatesChecked==9),Is.True);
            }
        }

        [Test,Timeout(1200000)] public void T13_ExportsAreByteStableAndWriteTheFinalDefaultRepeatEvidence()
        {
            string work=Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_09_FIX01/_work/export");
            string first=Path.Combine(work,"a"),second=Path.Combine(work,"b");
            Sv5LoopExport.WriteCase(Default.Value,first); Sv5LoopExport.WriteCase(Default.Value,second);
            foreach(string file in Directory.GetFiles(first,"*",SearchOption.AllDirectories))
            {
                string relative=file.Substring(first.Length+1); string counterpart=Path.Combine(second,relative);
                Assert.That(File.ReadAllBytes(counterpart),Is.EqualTo(File.ReadAllBytes(file)),relative);
            }
            string final=Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_09_FIX01");
            Sv5LoopExport.WriteComparison(final,Default.Value,Repeat.Value);
            foreach(string variant in new[]{"default","repeat"}) foreach(string required in new[]{"space_graph.json","loops.json","loop_candidates.csv","loop_links.csv","loop_cells.csv","loop_patterns.csv","loop_instances.csv","loop_validation.json","baseline_occupancy.csv","final_occupancy.csv","baseline_foot_nodes.csv","final_foot_nodes.csv","baseline_topology_edges.csv","final_topology_edges.csv","loop_topology_proofs.csv","topology_validation.json","preview/overview.svg","preview/detail.svg","preview/index.html"})
                Assert.That(File.Exists(Path.Combine(final,variant,required)),Is.True,variant+"/"+required);
        }

        [Test] public void T14_PreviousOneHundredTwoFocusedNamesRemainAndReadinessIsNotOverclaimed()
        {
            var xml=new System.Xml.XmlDocument(); xml.Load(Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_08_FIX01/focused_results.xml"));
            var tests=xml.SelectNodes("//test-case").Cast<System.Xml.XmlElement>().ToArray(); Assert.That(tests.Length,Is.EqualTo(102));
            foreach(var test in tests)
            {
                var type=typeof(Sv5SpaceLoopTests).Assembly.GetType(test.GetAttribute("classname"),true);
                var method=type.GetMethod(test.GetAttribute("methodname")); Assert.That(method,Is.Not.Null,test.GetAttribute("fullname"));
                Assert.That(method.GetCustomAttributes(typeof(TestAttribute),true),Is.Not.Empty); Assert.That(method.GetCustomAttributes(typeof(IgnoreAttribute),true),Is.Empty);
            }
            Assert.That(Default.Value.GeometryStateReady,Is.False); Assert.That(Default.Value.PlayerVerified,Is.False);
            Assert.That(Sv5SpaceGraphExport.SpaceGraphJson(Default.Value),Does.Contain("ACTUAL_TILE_LOOPS_STATIC_SCREEN"));
        }

        private static string Base(string owner)=>owner.EndsWith("_LINK",StringComparison.Ordinal) ? owner.Substring(0,owner.Length-5) : owner;
        private static string Detail(Sv5SpaceGraphPlan p)=>"accepted="+p.Loops.AcceptedCount+" sectors="+p.Loops.DistinctSectorCount+
            " candidates="+p.Loops.Candidates.Count+" eligible="+p.Loops.Candidates.Count(c=>c.Eligible)+"\n"+
            "candidate_pairs="+p.Loops.Candidates.Select(c=>c.PairKey).Distinct().Count()+" candidate_sectors="+
            p.Loops.Candidates.Select(c=>c.SectorId).Distinct().Count()+"\n"+
            "baseline_occupancy="+p.Loops.Topology.BaselineOccupancy.Count+" baseline_feet="+p.Loops.Topology.BaselineFootNodes.Count+
            " host_support="+p.Loops.Topology.BaselineOccupancy.Values.Count(c=>c.Provenance=="HOST_CORRIDOR_SUPPORT")+"\n"+
            string.Join("\n",p.Loops.Diagnostics)+"\n"+string.Join("\n",p.Diagnostics)+"\n"+
            string.Join("\n",p.Loops.Rejections.Select(r=>r.Key+"="+r.Value));
    }
}
#endif
