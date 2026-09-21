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
    public sealed class Sv5TreeGrabTests
    {
        private static readonly Lazy<Sv5SpaceGraphPlan> Default=new Lazy<Sv5SpaceGraphPlan>(()=>Attach(
            Sv5HubShellTests.DefaultForFix01));
        private static readonly Lazy<Sv5SpaceGraphPlan> Repeat=new Lazy<Sv5SpaceGraphPlan>(()=>Attach(
            Sv5HubShellTests.RepeatForFix01));
        private static Sv5SpaceGraphPlan Attach(Sv5SpaceGraphPlan hub)=>
            Sv5SpaceGraphPlanner.AttachTreeGrab(hub,Sv5TreeGrab.Build(hub));
        private static Sv5SpecialWorldPoint P(int x,int y)=>new Sv5SpecialWorldPoint(x,y);

        [Test,Timeout(600000)] public void T01_DefaultBuildProducesPassingBranchingTreeGeometry()
        {Assert.That(Default.Value.Success,Is.True,Detail(Default.Value));Assert.That(Default.Value.TreeGrab.Success,Is.True,Detail(Default.Value));}

        [Test,Timeout(600000)] public void T02_RepeatBuildProducesPassingBranchingTreeGeometry()
        {Assert.That(Repeat.Value.Success,Is.True,Detail(Repeat.Value));Assert.That(Repeat.Value.TreeGrab.Success,Is.True,Detail(Repeat.Value));}

        [Test] public void T03_DuplicatePhysicalExternalAnchorFixtureFails()
        {Assert.That(Sv5TreeGrab.AnchorsDistinct(new[]{P(1,2),P(1,2),P(2,2)}),Is.False);}

        [Test] public void T04_ExternalCenterlineReuseFixtureFails()
        {Assert.That(Sv5TreeGrab.PointSetsDisjoint(new[]{new[]{P(1,1),P(2,1)},new[]{P(2,1),P(3,1)}}),Is.False);}

        [Test] public void T05_SixCellEmptyVerticalClimbIsNotAnInitialGrab()
        {
            var edge=new Sv5MovementEdge(0,P(0,0),P(0,6),Sv5PlatformerMoveType.JumpGrab,true,true);
            Assert.That(Sv5TreeGrab.ValidateMovementEdge(edge,new HashSet<Sv5SpecialWorldPoint>(),
                new HashSet<Sv5SpecialWorldPoint>()),Is.False);
        }

        [Test] public void T06_InitialGrabWithoutTrunkContactFails()
        {
            var edge=new Sv5MovementEdge(0,P(0,0),P(1,2),Sv5PlatformerMoveType.JumpGrab,true,true);
            Assert.That(Sv5TreeGrab.ValidateMovementEdge(edge,new HashSet<Sv5SpecialWorldPoint>(),
                new HashSet<Sv5SpecialWorldPoint>()),Is.False);
        }

        [Test] public void T07_BlockedHeadClearanceFails()
        {
            var contact=P(1,2);var edge=new Sv5MovementEdge(0,P(0,0),P(0,2),
                Sv5PlatformerMoveType.JumpGrab,true,false,contact);
            Assert.That(Sv5TreeGrab.ValidateMovementEdge(edge,new HashSet<Sv5SpecialWorldPoint>{contact},
                new HashSet<Sv5SpecialWorldPoint>{contact}),Is.False);
        }

        [Test] public void T08_InitialJumpGrabRiseAboveTwoFails()
        {
            var contact=P(1,3);var edge=new Sv5MovementEdge(0,P(0,0),P(0,3),
                Sv5PlatformerMoveType.JumpGrab,true,true,contact);
            Assert.That(Sv5TreeGrab.ValidateMovementEdge(edge,new HashSet<Sv5SpecialWorldPoint>{contact},
                new HashSet<Sv5SpecialWorldPoint>{contact}),Is.False);
        }

        [Test] public void T09_DisconnectedUpperLevelFixtureFails()
        {Assert.That(Sv5TreeGrab.RoutesReachUpper(Array.Empty<Sv5TreeRoute>(),100),Is.False);}

        [Test] public void T10_MissingRecoveryFixtureFails()
        {Assert.That(Sv5TreeGrab.RecoveryCoversLevels(Array.Empty<Sv5TreeRoute>(),new[]{10,20,30}),Is.False);}

        [Test] public void T11_SixBySixSolidFillFixtureFails()
        {
            var solid=new HashSet<Sv5SpecialWorldPoint>(Enumerable.Range(0,6).SelectMany(x=>
                Enumerable.Range(0,6).Select(y=>P(x,y))));
            Assert.That(Sv5TreeGrab.HasSolidRectangle(solid,6,6),Is.True);
        }

        [Test] public void T12_ReadinessBoundaryRemainsExplicit()
        {
            Assert.That(Sv5HubShellTests.DefaultForFix01.HubShell.TreeGrabGeometryReady,Is.False);
            Assert.That(Default.Value.TreeGrab.TreeGrabGeometryReady,Is.True);
            Assert.That(Default.Value.TreeGrab.ComposedGeometryReady,Is.False);
            Assert.That(Default.Value.TreeGrab.PlayerVerified,Is.False);
        }

        [Test] public void T13_HubConnectionsHaveUniqueAnchorsAndDisjointExternalBodies()
        {
            var plan=Default.Value;
            Assert.That(Sv5TreeGrab.AnchorsDistinct(plan.HubShell.Connections.Select(value=>value.ExternalAnchor)),Is.True);
            Assert.That(Sv5TreeGrab.ConnectionBodiesDisjoint(plan.HubShell.Connections,plan.HubShell.Footprint),Is.True);
        }

        [Test] public void T14_ConnectionWitnessesUseSupportedPlatformerDeltas()
        {
            var plan=Default.Value;var occupancy=plan.TreeGrab.FinalOccupancy;
            foreach(var connection in plan.HubShell.Connections)foreach(var edge in connection.MovementWitness)
            {
                Assert.That(new[]{Sv5PlatformerMoveType.Walk,Sv5PlatformerMoveType.StepUp,
                    Sv5PlatformerMoveType.StepDown,Sv5PlatformerMoveType.Jump}.Contains(edge.MoveType),Is.True);
                Assert.That(edge.BodyClear&&edge.HeadClear,Is.True);
                foreach(var state in new[]{edge.From,edge.To})
                {
                    Assert.That(Sv5LoopTopology.ValueAt(occupancy,state),Is.EqualTo(Sv5InfillCellValue.Air));
                    Assert.That(Sv5LoopTopology.ValueAt(occupancy,P(state.X,state.Y+1)),Is.EqualTo(Sv5InfillCellValue.Air));
                    Assert.That(Sv5LoopTopology.ValueAt(occupancy,P(state.X,state.Y-1)),Is.EqualTo(Sv5InfillCellValue.Solid));
                }
            }
        }

        [Test] public void T15_LowerTrunkIsAtLeastOnePointFiveTimesUpperTrunk()
        {var tree=Default.Value.TreeGrab;Assert.That(tree.LowerTrunkWidth,Is.GreaterThanOrEqualTo(tree.UpperTrunkWidth*1.5));}

        [Test] public void T16_TrunkAverageWidthTapersFromLowerThroughUpper()
        {
            var tree=Default.Value.TreeGrab;
            Assert.That(tree.LowerTrunkWidth,Is.GreaterThanOrEqualTo(tree.MiddleTrunkWidth));
            Assert.That(tree.MiddleTrunkWidth,Is.GreaterThanOrEqualTo(tree.UpperTrunkWidth));
            Assert.That(tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.TrunkClimb)
                .GroupBy(cell=>cell.World.Y).OrderBy(group=>group.Key).First().Count(),Is.GreaterThanOrEqualTo(3));
        }

        [Test] public void T17_TreeHasMultipleActualMajorBranchPoints()
        {Assert.That(Default.Value.TreeGrab.MajorBranchPoints.Count,Is.GreaterThanOrEqualTo(2));}

        [Test] public void T18_TrunkClimbGraphIsConnectedAndHasMultipleEndpoints()
        {
            var tree=Default.Value.TreeGrab;var trunk=new HashSet<Sv5SpecialWorldPoint>(tree.Cells
                .Where(cell=>cell.Role==Sv5TreeCellRole.TrunkClimb).Select(cell=>cell.World));
            Assert.That(Sv5TreeGrab.Connected(trunk),Is.True);
            Assert.That(tree.ClimbEndpoints.Count,Is.GreaterThanOrEqualTo(3));
        }

        [Test] public void T19_SilhouetteIsNeitherSingleStraightNorPerfectlySymmetric()
        {
            var tree=Default.Value.TreeGrab;var trunk=tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.TrunkClimb)
                .Select(cell=>cell.World).ToArray();
            int center=Default.Value.HubShell.Footprint.X+11;
            Assert.That(trunk.Select(point=>point.X).Distinct().Count(),Is.GreaterThan(3));
            Assert.That(tree.LongestStraightMainRun,Is.LessThanOrEqualTo(9));
            Assert.That(Sv5TreeGrab.Symmetric(trunk,center),Is.False);
        }

        [Test] public void T20_BranchPlatformsAreTopOnlyAndNeverGrabSurfaces()
        {
            var tree=Default.Value.TreeGrab;var branches=tree.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.BranchPlatform).ToArray();
            Assert.That(branches.Length,Is.GreaterThanOrEqualTo(4));
            Assert.That(branches.All(cell=>cell.TopOnly&&!cell.GrabEnabled),Is.True);
            Assert.That(tree.BranchGrabCount,Is.Zero);
            Assert.That(tree.Surfaces.Any(surface=>branches.Any(cell=>cell.World.Equals(surface.World))),Is.False);
        }

        [Test] public void T21_DecorationHasNoCollisionOrMovementMeaning()
        {
            var decoration=Default.Value.TreeGrab.Cells.Where(cell=>cell.Role==Sv5TreeCellRole.DecorativeBranch||
                cell.Role==Sv5TreeCellRole.LeafDecoration).ToArray();
            Assert.That(decoration.Length,Is.GreaterThan(10));
            Assert.That(decoration.All(cell=>cell.IsVisualOnly&&!cell.MovementEnabled&&cell.FinalValue==Sv5InfillCellValue.Air),Is.True);
        }

        [Test] public void T22_RoutesCaptureOnceWithinTwoThenUseContinuousTrunkClimb()
        {
            foreach(var route in Default.Value.TreeGrab.Routes)
            {
                var first=route.Edges.First();Assert.That(first.MoveType,Is.EqualTo(Sv5PlatformerMoveType.JumpGrab));
                Assert.That(first.To.Y-first.From.Y,Is.InRange(1,2));
                Assert.That(route.Edges.Count(edge=>edge.MoveType==Sv5PlatformerMoveType.JumpGrab),Is.EqualTo(1));
                Assert.That(route.Edges.Skip(1).Take(route.Edges.Count-2).All(edge=>edge.TraversalState=="TRUNK_CLIMB"),Is.True);
                Assert.That(route.Edges.Last().TraversalState,Is.EqualTo("TRUNK_TO_BRANCH"));
            }
        }

        [Test] public void T23_TwoRoutesUseDistinctTrunkContactSequences()
        {
            var routes=Default.Value.TreeGrab.Routes;
            Assert.That(routes.Count,Is.EqualTo(2));
            Assert.That(routes.Select(route=>string.Join(";",route.Edges.Where(edge=>edge.Contact.HasValue)
                .Select(edge=>edge.Contact.Value))).Distinct().Count(),Is.EqualTo(2));
            Assert.That(Sv5TreeGrab.RoutesReachUpper(routes,Default.Value.TreeGrab.ReservedVolume.Y+22),Is.True);
        }

        [Test] public void T24_RequiredEntrancesExitsAndHubPortsStayOpen()
        {
            var plan=Default.Value;var tree=plan.TreeGrab;
            foreach(var point in tree.RequiredEntrances.Concat(tree.RequiredExits).Concat(plan.HubShell.Ports.Select(port=>port.Anchor)))
            {
                Assert.That(Sv5LoopTopology.ValueAt(tree.FinalOccupancy,point),Is.EqualTo(Sv5InfillCellValue.Air));
                Assert.That(Sv5LoopTopology.ValueAt(tree.FinalOccupancy,P(point.X,point.Y+1)),Is.EqualTo(Sv5InfillCellValue.Air));
            }
        }

        [Test] public void T25_EveryRequiredRecoveryEndsOnTopOnlySupportWithTwoAirCells()
        {
            var tree=Default.Value.TreeGrab;
            Assert.That(Sv5TreeGrab.RecoveryCoversLevels(tree.RecoveryRoutes,tree.RequiredLevels),Is.True);
            foreach(var route in tree.RecoveryRoutes)
            {
                var edge=route.Edges.Single();Assert.That(edge.MoveType,Is.EqualTo(Sv5PlatformerMoveType.Drop));
                Assert.That(edge.To.Y-edge.From.Y,Is.InRange(-4,-1));
                Assert.That(Sv5LoopTopology.ValueAt(tree.FinalOccupancy,P(edge.To.X,edge.To.Y-1)),Is.EqualTo(Sv5InfillCellValue.Solid));
                Assert.That(Sv5LoopTopology.ValueAt(tree.FinalOccupancy,edge.To),Is.EqualTo(Sv5InfillCellValue.Air));
                Assert.That(Sv5LoopTopology.ValueAt(tree.FinalOccupancy,P(edge.To.X,edge.To.Y+1)),Is.EqualTo(Sv5InfillCellValue.Air));
            }
        }

        [Test,Timeout(600000)] public void T26_DefaultAndRepeatAreIndividuallyDeterministic()
        {
            Assert.That(Sv5TreeGrab.Build(Sv5HubShellTests.DefaultForFix01).Digest,Is.EqualTo(Default.Value.TreeGrab.Digest));
            Assert.That(Sv5TreeGrab.Build(Sv5HubShellTests.RepeatForFix01).Digest,Is.EqualTo(Repeat.Value.TreeGrab.Digest));
        }

        [Test] public void T27_ProductAndBoundedGenerationRemainValid()
        {
            var plan=Default.Value;Assert.That(plan.PhysicalProduct.Success,Is.True);
            Assert.That(plan.PhysicalMovement.GateStateChecks.Count,Is.EqualTo(9));
            Assert.That(plan.PhysicalProduct.Matrix.Select(value=>value.ResourceOrder).Distinct().Count(),Is.EqualTo(6));
            Assert.That(plan.TreeGrab.GlobalProductRuns,Is.EqualTo(1));
            Assert.That(plan.TreeGrab.WholeWorldCopyPerCandidate,Is.Zero);
            Assert.That(plan.TreeGrab.WholeWorldBfsPerCandidate,Is.Zero);
            Assert.That(plan.TreeGrab.GenerationMilliseconds,Is.LessThan(45000));
        }

        [Test,Timeout(600000)] public void T28_DefaultAndRepeatExportsIncludeCollisionAndVisualEvidence()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string output=Path.Combine(root,"MapDesign/MCP/GENERATED/SV5_12_FIX01");
            Sv5TreeGrabExport.WriteComparison(output,Default.Value,Repeat.Value);
            foreach(string profile in new[]{"default","repeat"})foreach(string file in new[]{"hub_connections.csv",
                "hub_connection_cells.csv","hub_connection_movement.csv","tree_canopy.json","tree_grab.json","tree_cells.csv",
                "tree_limbs.csv","tree_forks.csv","tree_platforms.csv","tree_surfaces.csv",
                "tree_movement_witness.csv","tree_recovery_witness.csv","tree_climb_network.csv",
                "tree_validation.json","preview/tree_grab.svg","preview/tree_grab_overview.svg",
                "preview/tree_collision_overlay.svg"})Assert.That(File.Exists(Path.Combine(output,profile,file)),Is.True,profile+"/"+file);
            string validation=File.ReadAllText(Path.Combine(output,"default/tree_validation.json"));
            string overlay=File.ReadAllText(Path.Combine(output,"default/preview/tree_collision_overlay.svg"));
            Assert.That(validation,Does.Contain("\"status\":\"PASS\""));
            Assert.That(validation,Does.Contain("\"branch_grab_count\":0"));
            Assert.That(overlay,Does.Contain("landing only; no grab"));
        }

        private static string Detail(Sv5SpaceGraphPlan plan)=>string.Join("\n",plan.Diagnostics.Concat(
            plan.TreeGrab==null?Array.Empty<string>():plan.TreeGrab.Diagnostics));
    }
}
#endif
