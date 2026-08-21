using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration
{
    [Category("MAP06_10")]
    public sealed class OptionalRegionOverlayTests
    {
        private GeneratedWorldData world;
        private OptionalRegionSnapshot regions;
        private Type0RouteMaskAssignmentResult type0;
        private OptionalAccessAssignmentResult access;
        private OptionalRewardTierResult reward;
        private OptionalReturnPolicyResult returns;
        private InactiveBufferAssignmentResult inactive;
        private OptionalRegionValidationReport validation;
        private OptionalRegionOverlaySnapshot snapshot;

        public static IEnumerable<int> Cases => Enumerable.Range(0, 180);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new StarNight.Map.Tests.WorldGeneration.Generation.OptionalRegionValidatorTests();
            fixture.OneTimeSetUp();
            world = GetField<GeneratedWorldData>(fixture, "world");
            regions = GetField<OptionalRegionSnapshot>(fixture, "regions");
            type0 = GetField<Type0RouteMaskAssignmentResult>(fixture, "type0");
            access = GetField<OptionalAccessAssignmentResult>(fixture, "access");
            reward = GetField<OptionalRewardTierResult>(fixture, "reward");
            returns = GetField<OptionalReturnPolicyResult>(fixture, "returns");
            inactive = GetField<InactiveBufferAssignmentResult>(fixture, "inactive");
            validation = GetField<OptionalRegionValidationReport>(fixture, "baseline");
            snapshot = Build();
            Assert.That(snapshot.IsSuccess, Is.True);
        }

        [TestCaseSource(nameof(Cases))]
        public void ApprovedOverlayPublishesDeterministicImmutableVisualFacts(int caseId)
        {
            Assert.That(snapshot.Status, Is.EqualTo(OptionalRegionOverlayStatus.Completed));
            Assert.That(snapshot.RngDrawCount, Is.Zero);
            Assert.That(snapshot.Cells.Select(value => value.SectorIndex), Is.Ordered);

            switch (caseId % 15)
            {
                case 0:
                    Assert.That(snapshot.Cells, Has.Count.EqualTo(169));
                    Assert.That(new[]
                    {
                        CountCells(OptionalRegionOverlayCellKind.Mandatory),
                        CountCells(OptionalRegionOverlayCellKind.ReservedSite),
                        CountCells(OptionalRegionOverlayCellKind.Type0),
                        CountCells(OptionalRegionOverlayCellKind.InactiveInterior),
                        CountCells(OptionalRegionOverlayCellKind.InactiveDecorative)
                    }, Is.EqualTo(new[] { 44, 8, 39, 26, 52 }));
                    break;
                case 1:
                    Assert.That(snapshot.Connections.Count(value => value.Kind ==
                        OptionalRegionOverlayConnectionKind.AttachmentContact), Is.EqualTo(12));
                    Assert.That(snapshot.Connections.Count(value => value.Kind ==
                        OptionalRegionOverlayConnectionKind.ReturnWitness), Is.EqualTo(19));
                    Assert.That(snapshot.Connections.All(value => value.FromSectorIndex != value.ToSectorIndex), Is.True);
                    break;
                case 2:
                    Assert.That(snapshot.Legend, Has.Count.EqualTo(15));
                    Assert.That(snapshot.Legend.Select(value => value.Order), Is.EqualTo(Enumerable.Range(0, 15)));
                    Assert.That(snapshot.Legend.Select(value => value.Label).Distinct().Count(), Is.EqualTo(15));
                    break;
                case 3:
                    Assert.That(snapshot.SourceValidationDigest,
                        Is.EqualTo("1180f6a784b29739a2ca640d2c45398066ec7e636a8cb69ee307315cc20cc84e"));
                    Assert.That(snapshot.SourceInactiveDigest,
                        Is.EqualTo("426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578"));
                    Assert.That(snapshot.CanonicalDigest, Has.Length.EqualTo(64));
                    Assert.That(snapshot.CanonicalDigest.All(IsLowerHex), Is.True);
                    break;
                case 4:
                    var type0Cell = snapshot.Cells.Where(value => value.Kind == OptionalRegionOverlayCellKind.Type0)
                        .ElementAt(caseId % 39);
                    var sourceMask = type0.Assignments.Single(value => value.SectorIndex == type0Cell.SectorIndex);
                    Assert.That(type0Cell.RegionId, Is.EqualTo(sourceMask.RegionId));
                    Assert.That(type0Cell.Depth, Is.EqualTo(sourceMask.Depth.Value));
                    Assert.That(type0Cell.Label, Is.EqualTo(sourceMask.Depth.Value.ToString(CultureInfo.InvariantCulture)));
                    Assert.That(sourceMask.OpenMask.OpenLeft && sourceMask.OpenMask.OpenRight, Is.False);
                    break;
                case 5:
                    Assert.That(snapshot.Cells.Where(value => value.Kind == OptionalRegionOverlayCellKind.Type0)
                        .All(value => value.Layers.Contains(OptionalRegionOverlayLayer.AccessRule) &&
                                      value.Layers.Contains(OptionalRegionOverlayLayer.Depth) &&
                                      value.Layers.Contains(OptionalRegionOverlayLayer.RewardTier) &&
                                      value.Layers.Contains(OptionalRegionOverlayLayer.ReturnWitness)), Is.True);
                    break;
                case 6:
                    Assert.That(snapshot.Cells.Where(value => value.Kind == OptionalRegionOverlayCellKind.InactiveDecorative)
                        .All(value => value.Label == "D" && value.Layers.Contains(OptionalRegionOverlayLayer.InactiveKind)), Is.True);
                    Assert.That(snapshot.Cells.Where(value => value.Kind == OptionalRegionOverlayCellKind.InactiveInterior)
                        .All(value => value.Label == "I" && value.Layers.Contains(OptionalRegionOverlayLayer.InactiveKind)), Is.True);
                    break;
                case 7:
                    Assert.That(snapshot.Cells.Where(value => value.Kind == OptionalRegionOverlayCellKind.ReservedSite)
                        .Count(value => value.Label == "R*"), Is.EqualTo(3));
                    Assert.That(snapshot.Cells.Where(value => value.Kind == OptionalRegionOverlayCellKind.ReservedSite &&
                                                              value.Label == "R*")
                        .Select(value => value.SectorIndex), Is.EqualTo(new[] { 0, 28, 106 }));
                    break;
                case 8:
                    var repeated = Build();
                    Assert.That(repeated.CanonicalDigest, Is.EqualTo(snapshot.CanonicalDigest));
                    Assert.That(repeated.Cells.Select(value => value.Label),
                        Is.EqualTo(snapshot.Cells.Select(value => value.Label)));
                    break;
                case 9:
                    var invalidInput = new OptionalRegionOverlayBuilder().Build(null, regions, type0, access,
                        reward, returns, inactive, validation, OptionalRegionOverlaySettings.CreateApproved());
                    AssertAtomicFailure(invalidInput, OptionalRegionOverlayStatus.InvalidInput);
                    break;
                case 10:
                    var invalidSettings = new OptionalRegionOverlayBuilder().Build(world, regions, type0, access,
                        reward, returns, inactive, validation,
                        new OptionalRegionOverlaySettings(false, true, true, true, true, true, true, true));
                    AssertAtomicFailure(invalidSettings, OptionalRegionOverlayStatus.InvalidSettings);
                    break;
                case 11:
                    Assert.That(typeof(OptionalRegionOverlaySettings).GetProperties().All(value => !value.CanWrite), Is.True);
                    Assert.That(typeof(OptionalRegionOverlayCell).GetProperties().All(value => !value.CanWrite), Is.True);
                    Assert.That(typeof(OptionalRegionOverlayConnection).GetProperties().All(value => !value.CanWrite), Is.True);
                    Assert.That(typeof(OptionalRegionOverlaySnapshot).GetProperties().All(value => !value.CanWrite), Is.True);
                    break;
                case 12:
                    Assert.That(() => ((IList<OptionalRegionOverlayCell>)snapshot.Cells).Clear(),
                        Throws.TypeOf<NotSupportedException>());
                    Assert.That(() => ((IList<OptionalRegionOverlayConnection>)snapshot.Connections).Clear(),
                        Throws.TypeOf<NotSupportedException>());
                    Assert.That(() => ((IList<OptionalRegionOverlayLegendEntry>)snapshot.Legend).Clear(),
                        Throws.TypeOf<NotSupportedException>());
                    break;
                case 13:
                    Assert.That(snapshot.Cells.Where(value => value.Kind == OptionalRegionOverlayCellKind.Type0)
                        .Select(value => value.AccessRule).Distinct().Count(), Is.EqualTo(5));
                    Assert.That(snapshot.Cells.Where(value => value.Kind == OptionalRegionOverlayCellKind.Type0)
                        .Select(value => value.RewardTier).Distinct().Count(), Is.EqualTo(4));
                    Assert.That(snapshot.Cells.Where(value => value.Kind == OptionalRegionOverlayCellKind.Type0)
                        .All(value => value.ReturnPolicy == OptionalReturnPolicy.BacktrackToAttachment), Is.True);
                    break;
                default:
                    Assert.That(validation.IsValid, Is.True);
                    Assert.That(validation.Issues, Is.Empty);
                    Assert.That(validation.Diagnostics.SourceMutationCount, Is.Zero);
                    Assert.That(type0.Diagnostics.AttachmentBoundaryClosedCount, Is.EqualTo(12));
                    Assert.That(type0.Diagnostics.MandatoryBoundaryBaseOpenCount, Is.Zero);
                    break;
            }

            if (caseId < 24)
            {
                var visualCell = snapshot.Cells[caseId * 7 % snapshot.Cells.Count];
                Assert.That(visualCell.Label, Is.Not.Empty);
                Assert.That(Enum.IsDefined(typeof(OptionalRegionOverlayColorToken), visualCell.ColorToken), Is.True);
                Assert.That(visualCell.Layers[0], Is.EqualTo(OptionalRegionOverlayLayer.BaseRole));
                TestContext.WriteLine("MAP06_GAME_VISUAL_{0:00} sector={1} kind={2} color={3} label={4}",
                    caseId + 1, visualCell.SectorIndex, visualCell.Kind, visualCell.ColorToken, visualCell.Label);
            }
        }

        private OptionalRegionOverlaySnapshot Build()
        {
            return new OptionalRegionOverlayBuilder().Build(world, regions, type0, access, reward, returns,
                inactive, validation, OptionalRegionOverlaySettings.CreateApproved());
        }

        private int CountCells(OptionalRegionOverlayCellKind kind)
        {
            return snapshot.Cells.Count(value => value.Kind == kind);
        }

        private static void AssertAtomicFailure(
            OptionalRegionOverlaySnapshot value,
            OptionalRegionOverlayStatus expectedStatus)
        {
            Assert.That(value.Status, Is.EqualTo(expectedStatus));
            Assert.That(value.IsSuccess, Is.False);
            Assert.That(value.Cells, Is.Empty);
            Assert.That(value.Connections, Is.Empty);
            Assert.That(value.Legend, Is.Empty);
            Assert.That(value.CanonicalDigest, Is.Empty);
            Assert.That(value.RngDrawCount, Is.Zero);
        }

        private static bool IsLowerHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f');
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
