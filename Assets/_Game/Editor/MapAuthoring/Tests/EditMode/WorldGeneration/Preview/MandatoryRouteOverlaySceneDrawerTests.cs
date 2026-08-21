using System.Collections;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.MapAuthoring.Editor.WorldGeneration.Preview;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Preview
{
    [Category("MAP05_10")]
    public sealed class MandatoryRouteOverlaySceneDrawerTests
    {
        private MandatoryRouteOverlaySnapshot snapshot;
        public static IEnumerable Cases { get { for(var i=0;i<24;i++) yield return new TestCaseData(i); } }
        [OneTimeSetUp] public void Setup(){var type=System.Reflection.Assembly.Load("Game.Map.Tests.EditMode").GetType("StarNight.Map.Tests.WorldGeneration.Generation.MandatoryRouteGraphBuilderTests",true);var fixture=System.Activator.CreateInstance(type);type.GetMethod("OneTimeSetUp").Invoke(fixture,null);var baseline=type.GetField("baseline",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(fixture);var graph=(MandatoryRouteGraph)baseline.GetType().GetProperty("Graph").GetValue(baseline);snapshot=MandatoryRouteOverlaySnapshot.Create(new MandatoryRouteGraphValidator().Validate(graph).Report);}
        [TestCaseSource(nameof(Cases))] public void CommandsAreDeterministicAndAssetFree(int id){var commands=MandatoryRouteOverlaySceneDrawer.BuildDrawCommands(snapshot);Assert.That(commands.Count,Is.EqualTo(47));var command=commands[id%commands.Count];Assert.That(command.Position,Is.EqualTo(MandatoryRouteOverlaySceneDrawer.ToWorldPosition(command.Index)));Assert.That(command.Label,Is.EqualTo(snapshot.Cells[id%commands.Count].Label));}
        [Test] public void NullSnapshotProducesNoCommands(){Assert.That(MandatoryRouteOverlaySceneDrawer.BuildDrawCommands(null),Is.Empty);}
        [Test] public void WorldMappingUsesSectorDimensions(){Assert.That(MandatoryRouteOverlaySceneDrawer.ToWorldPosition(0).x,Is.EqualTo(0));Assert.That(MandatoryRouteOverlaySceneDrawer.ToWorldPosition(1).x,Is.EqualTo(48));Assert.That(MandatoryRouteOverlaySceneDrawer.ToWorldPosition(13).y,Is.EqualTo(32));}
    }
}
