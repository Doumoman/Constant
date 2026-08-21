using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.MapAuthoring.Preview;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration
{
    [Category("MAP06_10")]
    public sealed class OptionalRegionOverlaySceneDrawerTests
    {
        private OptionalRegionOverlaySnapshot snapshot;

        public static IEnumerable<int> Cases => Enumerable.Range(0, 40);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var type = Assembly.Load("Game.Map.Tests.EditMode")
                .GetType("StarNight.Map.Tests.WorldGeneration.OptionalRegionOverlayTests", true);
            var fixture = Activator.CreateInstance(type);
            type.GetMethod("OneTimeSetUp").Invoke(fixture, null);
            snapshot = (OptionalRegionOverlaySnapshot)type
                .GetField("snapshot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture);
            Assert.That(snapshot.IsSuccess, Is.True);
        }

        [TestCaseSource(nameof(Cases))]
        public void DrawCommandsAreDeterministicAssetFreeAndSceneReady(int caseId)
        {
            var commands = OptionalRegionOverlaySceneDrawer.BuildDrawCommands(snapshot);
            var repeated = OptionalRegionOverlaySceneDrawer.BuildDrawCommands(snapshot);
            Assert.That(commands, Has.Count.EqualTo(371));
            Assert.That(commands.Select(value => value.Order), Is.EqualTo(Enumerable.Range(0, 371)));
            Assert.That(repeated.Select(Signature), Is.EqualTo(commands.Select(Signature)));

            switch (caseId % 8)
            {
                case 0:
                    Assert.That(Count(commands, OptionalRegionOverlayDrawCommandKind.Cell), Is.EqualTo(169));
                    Assert.That(Count(commands, OptionalRegionOverlayDrawCommandKind.DepthLabel), Is.EqualTo(39));
                    break;
                case 1:
                    Assert.That(Count(commands, OptionalRegionOverlayDrawCommandKind.AttachmentContact), Is.EqualTo(12));
                    Assert.That(Count(commands, OptionalRegionOverlayDrawCommandKind.ReturnWitness), Is.EqualTo(19));
                    break;
                case 2:
                    Assert.That(Count(commands, OptionalRegionOverlayDrawCommandKind.RewardMarker), Is.EqualTo(39));
                    Assert.That(Count(commands, OptionalRegionOverlayDrawCommandKind.InactiveMarker), Is.EqualTo(78));
                    break;
                case 3:
                    Assert.That(Count(commands, OptionalRegionOverlayDrawCommandKind.Legend), Is.EqualTo(15));
                    Assert.That(Count(commands, OptionalRegionOverlayDrawCommandKind.ValidationIssue), Is.Zero);
                    break;
                case 4:
                    Assert.That(OptionalRegionOverlaySceneDrawer.BuildDrawCommands(null), Is.Empty);
                    break;
                case 5:
                    Assert.That(OptionalRegionOverlaySceneDrawer.SectorCenter(0, 1f),
                        Is.EqualTo(new Vector3(24f, 16f, 0f)));
                    Assert.That(OptionalRegionOverlaySceneDrawer.SectorCenter(1, 1f).x, Is.EqualTo(72f));
                    Assert.That(OptionalRegionOverlaySceneDrawer.SectorCenter(13, 1f).y, Is.EqualTo(48f));
                    break;
                case 6:
                    Assert.That(() => OptionalRegionOverlaySceneDrawer.SectorCenter(-1, 1f),
                        Throws.TypeOf<ArgumentOutOfRangeException>());
                    Assert.That(() => OptionalRegionOverlaySceneDrawer.SectorCenter(0, 0f),
                        Throws.TypeOf<ArgumentOutOfRangeException>());
                    break;
                default:
                    foreach (OptionalRegionOverlayColorToken token in Enum.GetValues(typeof(OptionalRegionOverlayColorToken)))
                    {
                        var color = OptionalRegionOverlaySceneDrawer.ToColor(token);
                        Assert.That(color.a, Is.EqualTo(1f));
                    }
                    break;
            }

            if (caseId < 24)
            {
                var command = commands[caseId * 13 % commands.Count];
                Assert.That(command.Label, Is.Not.Empty);
                Assert.That(Enum.IsDefined(typeof(OptionalRegionOverlayColorToken), command.ColorToken), Is.True);
                TestContext.WriteLine("MAP06_SCENE_VISUAL_{0:00} kind={1} from={2} to={3} color={4} label={5}",
                    caseId + 1, command.Kind, command.FromSectorIndex, command.ToSectorIndex,
                    command.ColorToken, command.Label);
            }
        }

        private static int Count(
            IReadOnlyList<OptionalRegionOverlayDrawCommand> commands,
            OptionalRegionOverlayDrawCommandKind kind)
        {
            return commands.Count(value => value.Kind == kind);
        }

        private static string Signature(OptionalRegionOverlayDrawCommand value)
        {
            return value.Order + ":" + value.Kind + ":" + value.FromSectorIndex + ":" +
                   value.ToSectorIndex + ":" + value.ColorToken + ":" + value.Label;
        }
    }
}
