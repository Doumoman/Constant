using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Diagnostics
{
    [Category("MAP05_10")]
    public sealed class MandatoryRouteOverlayTests
    {
        private MandatoryRouteGraph graph; private MandatoryRouteValidationReport report; private MandatoryRouteOverlaySnapshot snapshot;
        public static IEnumerable DeterminismCases { get { for(var i=0;i<128;i++) yield return new TestCaseData(i); } }
        [OneTimeSetUp] public void OneTimeSetUp(){var fixture=new StarNight.Map.Tests.WorldGeneration.Generation.MandatoryRouteGraphBuilderTests();fixture.OneTimeSetUp();var field=fixture.GetType().GetField("baseline",BindingFlags.Instance|BindingFlags.NonPublic);graph=((MandatoryRouteGraphBuildResult)field.GetValue(fixture)).Graph;report=new MandatoryRouteGraphValidator().Validate(graph).Report;snapshot=MandatoryRouteOverlaySnapshot.Create(report);}
        [TestCaseSource(nameof(DeterminismCases))] public void SnapshotIsOrderedCultureStableAndDeterministic(int id){var old=CultureInfo.CurrentCulture;try{CultureInfo.CurrentCulture=(id&1)==0?CultureInfo.GetCultureInfo("en-US"):CultureInfo.GetCultureInfo("tr-TR");var value=MandatoryRouteOverlaySnapshot.Create(report);Assert.That(value.Cells.Select(x=>x.Index),Is.Ordered);Assert.That(string.Join("|",value.Cells.Select(x=>x.Label)),Is.EqualTo(string.Join("|",snapshot.Cells.Select(x=>x.Label))));}finally{CultureInfo.CurrentCulture=old;}}
        [Test] public void StarterVectorIsExact(){Assert.That(new[]{snapshot.NodeCount,snapshot.DirectedEdgeCount,snapshot.UndirectedEdgeCount,snapshot.RouteCellCount},Is.EqualTo(new[]{47,96,48,47}));Assert.That(new[]{snapshot.Type1Count,snapshot.Type2Count,snapshot.Type3Count,snapshot.Type4UdCount,snapshot.Type4LudCount,snapshot.Type4RudCount,snapshot.Type4LrudCount},Is.EqualTo(new[]{20,4,4,17,0,0,2}));Assert.That(new[]{snapshot.ReachableTerminalCount,snapshot.TerminalCount,snapshot.RepresentedLoopCount},Is.EqualTo(new[]{7,7,2}));}
        [TestCase(1,true,true,false,false,"T1")][TestCase(2,true,true,false,true,"T2")][TestCase(3,true,true,true,false,"T3")][TestCase(4,false,false,true,true,"T4-UD")][TestCase(4,true,false,true,true,"T4-LUD")][TestCase(4,false,true,true,true,"T4-RUD")][TestCase(4,true,true,true,true,"T4-LRUD")]
        public void DisplayTokensPreserveExactSides(int type,bool l,bool r,bool u,bool d,string token){Assert.That(MandatoryRouteOverlayCell.GetDisplayTypeToken(type,l,r,u,d),Is.EqualTo(token));}
        [Test] public void ValidationAndCsvSummaryIsSurfaced(){Assert.That(snapshot.ValidationBanner,Is.EqualTo("PASS_ROUTE 12/12 | V/E/W 0/0/0"));Assert.That(new[]{snapshot.GeneratedSectorCsvByteCount,snapshot.GeneratedEdgeCsvByteCount,snapshot.GeneratedEdgeRowCount},Is.EqualTo(new[]{16838,7094,96}));}
        [Test] public void SnapshotCollectionsAreImmutable(){Assert.Throws<NotSupportedException>(()=>((System.Collections.Generic.IList<MandatoryRouteOverlayCell>)snapshot.Cells).Clear());Assert.Throws<NotSupportedException>(()=>((System.Collections.Generic.IList<MandatoryRouteGraphEdge>)snapshot.Edges).Clear());}
        [Test] public void ProjectionDoesNotMutateSources(){var before=graph.GeneratedWorldEdgesCsv;MandatoryRouteOverlaySnapshot.Create(report);Assert.That(graph.GeneratedWorldEdgesCsv,Is.EqualTo(before));Assert.That(report.SourceGraph,Is.SameAs(graph));}
        [Test] public void Type4AlwaysHasUpDownAndPreservesLeftRight(){foreach(var cell in snapshot.Cells.Where(x=>x.RouteType==4)){Assert.That(cell.OpenUp&&cell.OpenDown,Is.True);var source=graph.Cells.Single(x=>x.SectorIndex==cell.Index);Assert.That(new[]{cell.OpenLeft,cell.OpenRight},Is.EqualTo(new[]{source.OpenLeft,source.OpenRight}));}}
        [Test] public void GuiHasDeterministicDesktopAndCompactRects(){var a=MandatoryRouteOverlayGui.GetCellRect(0);var b=MandatoryRouteOverlayGui.GetCellRect(0,true);Assert.That(new[]{a.width,a.height,b.width,b.height},Is.EqualTo(new[]{48f,48f,24f,24f}));Assert.That(MandatoryRouteOverlayGui.GetCellRect(168).position,Is.Not.EqualTo(a.position));}
        [Test] public void CellsCarryDistanceTerminalLoopAndEdgeGlyphData(){Assert.That(snapshot.Cells.All(x=>x.DistanceFromStart>=0&&x.DirectedEdgeCount>=1&&x.SideGlyph.Length==4),Is.True);Assert.That(snapshot.Cells.Count(x=>x.TerminalRoleToken.Length!=0),Is.GreaterThanOrEqualTo(7));Assert.That(snapshot.Cells.Count(x=>x.IsLoop),Is.GreaterThan(0));}
    }
}
