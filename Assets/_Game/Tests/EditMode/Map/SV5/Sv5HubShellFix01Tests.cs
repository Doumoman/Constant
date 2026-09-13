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
    public sealed class Sv5HubShellFix01Tests
    {
        private static Sv5SpaceGraphPlan Baseline=>Sv5HubShellTests.DefaultBaselineForFix01;
        private static Sv5SpaceGraphPlan Plan=>Sv5HubShellTests.DefaultForFix01;
        private static RmapSpecialWorldPoint P(int x,int y)=>new RmapSpecialWorldPoint(x,y);

        [Test] public void F01_LegacyVerticalThenHorizontalRouteExposesOriginalFalsePositive()
        {
            var legacy=Sv5HubConnectionRouter.LegacyVerticalThenHorizontal(P(10,10),P(15,13));
            var blocked=new HashSet<RmapSpecialWorldPoint>{P(10,11)};
            Assert.That(Sv5HubConnectionRouter.BodyIntersects(legacy,blocked),Is.True,
                "The old claim-only L route crossed a protected cell while exported booleans stayed false.");
        }

        [Test] public void F02_BoundedRouterRejectsAnUnavoidableProtectedBodyWall()
        {
            var protectedCells=new HashSet<RmapSpecialWorldPoint>(Enumerable.Range(0,
                Sv5SpaceGraphPlanner.WorldHeight-1).Select(y=>P(11,y)));
            Assert.That(Route(P(10,10),P(16,10),protectedCells,new HashSet<RmapSpecialWorldPoint>(),
                new HashSet<RmapSpecialWorldPoint>(),new HashSet<RmapSpecialWorldPoint>(),out _,out string reason),Is.False);
            Assert.That(reason,Is.EqualTo("NO_ACTUAL_CELL_ROUTE"));
        }

        [Test] public void F03_Type0IntersectionIsComputedFromBodyCells()
        {
            var path=Sv5HubConnectionRouter.LegacyVerticalThenHorizontal(P(20,20),P(24,22));
            Assert.That(Sv5HubConnectionRouter.BodyIntersects(path,new HashSet<RmapSpecialWorldPoint>{P(20,21)}),Is.True);
        }

        [Test] public void F04_ProgressionIntersectionIsComputedFromBodyCells()
        {
            var path=Sv5HubConnectionRouter.LegacyVerticalThenHorizontal(P(30,30),P(34,32));
            Assert.That(Sv5HubConnectionRouter.BodyIntersects(path,new HashSet<RmapSpecialWorldPoint>{P(30,31)}),Is.True);
        }

        [Test] public void F05_ExternalAnchorIsTheOnlyReadOnlyForeignContact()
        {
            var foreign=new HashSet<RmapSpecialWorldPoint>{P(16,10)};
            Assert.That(Route(P(10,10),P(16,10),foreign,new HashSet<RmapSpecialWorldPoint>(),
                new HashSet<RmapSpecialWorldPoint>(),new HashSet<RmapSpecialWorldPoint>(),out var route,out _),Is.True);
            var anchor=route.Cells.Single(v=>v.Role==Sv5HubConnectionCellRole.Centerline&&v.World.Equals(P(16,10)));
            Assert.That(anchor.Changed,Is.False);Assert.That(anchor.Ownership,Is.Empty);
        }

        [Test] public void F06_EveryChangedConnectionCellHasExplicitOwnership()
        {
            var changed=Plan.HubShell.Connections.SelectMany(v=>v.Cells).Where(v=>v.Changed).ToArray();
            Assert.That(changed,Is.Not.Empty);Assert.That(changed.All(v=>v.Ownership=="HUB_CONNECTION_ACTUAL"),Is.True);
            Assert.That(changed.All(v=>Plan.HubShell.FinalOccupancy[v.World].Provenance=="HUB_CONNECTION_ACTUAL"),Is.True);
        }

        [Test] public void F07_EachConnectionStoresOneValidDirectionalWitness()
        {
            Assert.That(Plan.HubShell.Connections.All(v=>v.Direction=="HUB_TO_EXTERNAL"&&v.RouteVerified&&
                v.MovementWitness.SequenceEqual(v.Centerline)),Is.True);
        }

        [Test] public void F08_PostHubMovementAndNineBySixProductAreRebuilt()
        {
            Assert.That(Plan.PhysicalMovement.SemanticDigest,Is.Not.EqualTo(Baseline.PhysicalMovement.SemanticDigest));
            Assert.That(Plan.PhysicalProduct.Success,Is.True);Assert.That(Plan.PhysicalMovement.GateStateChecks.Count,Is.EqualTo(9));
            Assert.That(Plan.PhysicalProduct.Matrix.Select(v=>v.ResourceOrder).Distinct().Count(),Is.EqualTo(6));
        }

        [Test,Timeout(600000)] public void F09_RoutingAndPostHubDigestsAreDeterministic()
        {
            var hub=Sv5HubShell.Build(Baseline);var again=Sv5SpaceGraphPlanner.AttachHubShell(Baseline,hub);
            Assert.That(hub.Digest,Is.EqualTo(Plan.HubShell.Digest));
            Assert.That(again.PhysicalMovement.SemanticDigest,Is.EqualTo(Plan.PhysicalMovement.SemanticDigest));
            Assert.That(again.PhysicalProduct.SemanticDigest,Is.EqualTo(Plan.PhysicalProduct.SemanticDigest));
        }

        [Test] public void F10_RoutingWorkRespectsSparseLocalCaps()
        {
            var performance=Plan.HubShell.Performance;
            Assert.That(performance.LocalRouteAttempts,Is.InRange(Plan.HubShell.Connections.Count,4096));
            Assert.That(performance.WholeWorldCopyPerCandidate,Is.Zero);Assert.That(performance.WholeWorldBfsPerCandidate,Is.Zero);
            Assert.That(performance.GlobalProductRuns,Is.EqualTo(1));Assert.That(performance.RejectionHistogram,Is.Not.Empty);
        }

        [Test,Timeout(600000)] public void F11_C05ExportsContainIndependentConstraintBindings()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string output=Path.Combine(root,"MapDesign/MCP/GENERATED/SV5_11_FIX01");
            Sv5HubShellExport.WriteComparison(output,Plan,Sv5HubShellTests.RepeatForFix01);
            foreach(string profile in new[]{"default","repeat"})
            {
                string folder=Path.Combine(output,profile);
                foreach(string file in new[]{"hub_shell.json","hub_connections.csv","hub_connection_cells.csv",
                    "hub_connection_checks.csv","constraint_sources.json","hub_validation.json","preview/hub_connection_fix.svg"})
                    Assert.That(File.Exists(Path.Combine(folder,file)),Is.True,profile+"/"+file);
                string sources=File.ReadAllText(Path.Combine(folder,"constraint_sources.json"));
                foreach(string category in new[]{"PROTECTED","TYPE0","PROGRESSION_GATE","CORE","RESERVATION","INFILL","LOOP","SIDEPATH"})
                    Assert.That(sources,Does.Contain("\"category\":\""+category+"\""));
            }
        }

        [Test] public void F12_AllCenterlineAndHeadCellsAreActualFinalAir()
        {
            foreach(var connection in Plan.HubShell.Connections)
            foreach(var cell in connection.Cells.Where(v=>v.Role==Sv5HubConnectionCellRole.Centerline))
            {Assert.That(cell.FinalValue,Is.EqualTo(Sv5InfillCellValue.Air));Assert.That(cell.HeadValue,Is.EqualTo(Sv5InfillCellValue.Air));}
        }

        [Test] public void F13_ExternalAnchorsRemainUnchangedAndBodyFlagsAreClear()
        {
            foreach(var connection in Plan.HubShell.Connections)
            {
                Assert.That(connection.Cells.Single(v=>v.Role==Sv5HubConnectionCellRole.Centerline&&
                    v.World.Equals(connection.ExternalAnchor)).Changed,Is.False);
                Assert.That(connection.ProtectedOverlap||connection.Type0Overlap||connection.ProgressionBypass,Is.False);
            }
        }

        [Test] public void F14_ConnectionsRemainDistinctWithoutSharedOwnedCells()
        {
            var connections=Plan.HubShell.Connections;
            Assert.That(connections.Count,Is.InRange(4,6));
            Assert.That(connections.Select(v=>v.ExternalRoomId).Distinct().Count(),Is.EqualTo(connections.Count));
            Assert.That(connections.Select(v=>v.ExternalSpaceGroupId).Distinct().Count(),Is.EqualTo(connections.Count));
            Assert.That(connections.SelectMany(v=>v.Cells.Where(c=>c.Changed).Select(c=>c.World)).GroupBy(v=>v).All(g=>g.Count()==1),Is.True);
        }

        private static bool Route(RmapSpecialWorldPoint start,RmapSpecialWorldPoint end,
            ISet<RmapSpecialWorldPoint> protectedBody,ISet<RmapSpecialWorldPoint> type0,
            ISet<RmapSpecialWorldPoint> progression,ISet<RmapSpecialWorldPoint> endpointForbidden,
            out Sv5HubConnectionRoute route,out string reason)
        {
            var baseline=new Dictionary<RmapSpecialWorldPoint,Sv5LoopOccupancyCell>
            {
                {end,new Sv5LoopOccupancyCell(end,Sv5InfillCellValue.Air,"FOREIGN","ROOM")},
                {P(end.X,end.Y+1),new Sv5LoopOccupancyCell(P(end.X,end.Y+1),Sv5InfillCellValue.Air,"FOREIGN","ROOM")}
            };
            return Sv5HubConnectionRouter.TryRoute(start,end,new Sv5SpaceBounds(start.X,start.Y,1,1),baseline,
                protectedBody,type0,progression,endpointForbidden,new HashSet<RmapSpecialWorldPoint>(),out route,out reason);
        }
    }
}
#endif
