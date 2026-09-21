#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5TreeCanopyFix01Tests
    {
        private static readonly Lazy<Sv5SpaceGraphPlan> Default=new Lazy<Sv5SpaceGraphPlan>(()=>Attach(
            Sv5HubShellTests.DefaultForFix01));
        private static readonly Lazy<Sv5SpaceGraphPlan> Repeat=new Lazy<Sv5SpaceGraphPlan>(()=>Attach(
            Sv5HubShellTests.RepeatForFix01));
        private static Sv5SpaceGraphPlan Attach(Sv5SpaceGraphPlan hub)=>
            Sv5SpaceGraphPlanner.AttachTreeGrab(hub,Sv5TreeGrab.Build(hub));

        [Test,Timeout(600000)] public void F01_DefaultEnvelopeIsTenByTwentyFour()
        {Assert.That(Default.Value.TreeGrab.ReservedVolume.Width,Is.EqualTo(10));Assert.That(Default.Value.TreeGrab.ReservedVolume.Height,Is.EqualTo(24));}

        [Test] public void F02_EnvelopeStaysInsideTwelveByThirtyHubInnerVolume()
        {
            var tree=Default.Value.TreeGrab.ReservedVolume;var hub=Default.Value.HubShell.Footprint;
            var inner=new Sv5SpaceBounds(hub.X+6,hub.Y+5,12,30);
            Assert.That(inner.Contains(new Sv5SpecialWorldPoint(tree.X,tree.Y)),Is.True);
            Assert.That(inner.Contains(new Sv5SpecialWorldPoint(tree.MaxXExclusive-1,tree.MaxYExclusive-1)),Is.True);
        }

        [Test] public void F03_LowerRootWidthIsFourOrFiveCells()
        {
            var trunk=Default.Value.TreeGrab.Cells.Where(c=>c.Role==Sv5TreeCellRole.TrunkClimb).ToArray();
            int y=trunk.Min(c=>c.World.Y);Assert.That(trunk.Count(c=>c.World.Y==y),Is.InRange(4,5));
        }

        [Test] public void F04_ActualClimbableCanopySpansAtLeastEightColumns()
        {
            var xs=Default.Value.TreeGrab.Cells.Where(c=>c.Role==Sv5TreeCellRole.TrunkClimb).Select(c=>c.World.X).ToArray();
            Assert.That(xs.Max()-xs.Min()+1,Is.GreaterThanOrEqualTo(8));
        }

        [Test] public void F05_ClimbableCanopyReachesThreeCellsOnBothSidesOfRootAxis()
        {
            var tree=Default.Value.TreeGrab;int axis=tree.ReservedVolume.X+tree.ReservedVolume.Width/2;
            var xs=tree.Cells.Where(c=>c.Role==Sv5TreeCellRole.TrunkClimb).Select(c=>c.World.X).ToArray();
            Assert.That(axis-xs.Min(),Is.GreaterThanOrEqualTo(3));Assert.That(xs.Max()-axis,Is.GreaterThanOrEqualTo(3));
        }

        [Test] public void F06_AtLeastThreeActualForkPointsExist()
        {Assert.That(Default.Value.TreeGrab.MajorBranchPoints.Count,Is.GreaterThanOrEqualTo(3));}

        [Test] public void F07_ClimbableTreeHasFourToSixTerminalEndpoints()
        {Assert.That(Default.Value.TreeGrab.ClimbEndpoints.Count,Is.InRange(4,6));}

        [Test] public void F08_BranchPlatformsStayWithinSevenToTenCells()
        {Assert.That(Default.Value.TreeGrab.BranchPlatformCount,Is.InRange(7,10));}

        [Test] public void F09_BranchPlatformsRemainTopOnlyAndHaveZeroGrabSurfaces()
        {
            var tree=Default.Value.TreeGrab;var branches=tree.Cells.Where(c=>c.Role==Sv5TreeCellRole.BranchPlatform).ToArray();
            Assert.That(branches.All(c=>c.TopOnly&&!c.GrabEnabled),Is.True);Assert.That(tree.BranchGrabCount,Is.Zero);
        }

        [Test] public void F10_LongestStraightClimbLimbRunIsAtMostFiveCells()
        {Assert.That(Default.Value.TreeGrab.LongestStraightMainRun,Is.LessThanOrEqualTo(5));}

        [Test] public void F11_DefaultAndRepeatHaveDifferentRelativeTreeGeometry()
        {
            string Signature(Sv5SpaceGraphPlan plan)
            {
                var volume=plan.TreeGrab.ReservedVolume;
                return string.Join(";",plan.TreeGrab.Cells.Where(c=>c.Role!=Sv5TreeCellRole.MovementClearance)
                    .Select(c=>(c.World.X-volume.X)+":"+(c.World.Y-volume.Y)+":"+c.ExportRole).OrderBy(v=>v,StringComparer.Ordinal));
            }
            Assert.That(Signature(Default.Value),Is.Not.EqualTo(Signature(Repeat.Value)));
        }

        [Test] public void F12_ExporterUsesFix01CanopySchema()
        {Assert.That(Sv5TreeGrabExport.TreeJson(Default.Value),Does.Contain("SV5_12_FIX01_TREE_CANOPY/v1"));}
    }
}
#endif
