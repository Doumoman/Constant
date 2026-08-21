using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [TestFixture]
    public sealed class OptionalRegionModelsTests
    {
        [TestCase("A")]
        [TestCase("REGION")]
        [TestCase("REGION_0")]
        [TestCase("OPTIONAL_001")]
        [TestCase("TYPE0_SECRET")]
        [TestCase("A0")]
        [TestCase("0")]
        [TestCase("_")]
        [TestCase("ABC_DEF_123")]
        [TestCase("Z9_")]
        public void ValidRegionIdsRoundTrip(string value)
        {
            Assert.That(OptionalRegionId.TryCreate(value, out var parsed), Is.True);
            Assert.That(parsed.IsValid, Is.True);
            Assert.That(parsed.Value, Is.EqualTo(value));
            Assert.That(parsed.ToString(), Is.EqualTo(value));
            Assert.That(new OptionalRegionId(value), Is.EqualTo(parsed));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("REGION A")]
        [TestCase("region")]
        [TestCase("Region")]
        [TestCase("REGION-A")]
        [TestCase("REGION.A")]
        [TestCase("REGION/A")]
        [TestCase("한글")]
        [TestCase("É")]
        [TestCase("A\tB")]
        [TestCase("A\nB")]
        [TestCase("A+B")]
        public void InvalidRegionIdsAreRejected(string value)
        {
            Assert.That(OptionalRegionId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null)
                Assert.Throws<ArgumentNullException>(() => new OptionalRegionId(value));
            else
                Assert.Throws<ArgumentException>(() => new OptionalRegionId(value));
        }

        [TestCase("A", "B", -1)]
        [TestCase("B", "A", 1)]
        [TestCase("A", "A", 0)]
        [TestCase("A0", "A_", -1)]
        [TestCase("Z", "_", -1)]
        [TestCase("REGION_01", "REGION_02", -1)]
        public void RegionIdOrderingIsOrdinal(string left, string right, int sign)
        {
            var leftId = new OptionalRegionId(left);
            var rightId = new OptionalRegionId(right);
            Assert.That(Math.Sign(leftId.CompareTo(rightId)), Is.EqualTo(sign));
        }

        [Test]
        public void RegionIdEqualityOperatorsAndHashAreStable()
        {
            var first = new OptionalRegionId("REGION_A");
            var second = new OptionalRegionId(new string("REGION_A".ToCharArray()));
            var other = new OptionalRegionId("REGION_B");
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
            Assert.That(first.Equals((object)second), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(first, Is.Not.EqualTo(other));
        }

        [Test]
        public void DefaultRegionIdIsInvalid()
        {
            var id = default(OptionalRegionId);
            Assert.That(id.IsValid, Is.False);
            Assert.That(id.Value, Is.Empty);
            Assert.That(id.ToString(), Is.Empty);
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void RegionIdAndTokensAreCultureIndependent(string cultureName)
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName);
                var id = new OptionalRegionId("REGION_I");
                Assert.That(id.ToString(), Is.EqualTo("REGION_I"));
                Assert.That(OptionalRegionTokenCodec.TryParseAccessRule("HIDDEN", out var rule), Is.True);
                Assert.That(OptionalRegionTokenCodec.ToToken(rule), Is.EqualTo("HIDDEN"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [TestCase("BASIC", OptionalRegionAccessRule.Basic)]
        [TestCase("TOOL", OptionalRegionAccessRule.Tool)]
        [TestCase("ENVIRONMENT", OptionalRegionAccessRule.Environment)]
        [TestCase("EXPLOSIVE", OptionalRegionAccessRule.Explosive)]
        [TestCase("HIDDEN", OptionalRegionAccessRule.Hidden)]
        public void AccessTokensRoundTrip(string token, OptionalRegionAccessRule value)
        {
            Assert.That(OptionalRegionTokenCodec.TryParseAccessRule(token, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(value));
            Assert.That(OptionalRegionTokenCodec.ToToken(value), Is.EqualTo(token));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("basic")]
        [TestCase("Basic")]
        [TestCase(" BASIC")]
        [TestCase("BASIC ")]
        [TestCase("0")]
        [TestCase("5")]
        [TestCase("TOOLS")]
        [TestCase("ENV")]
        [TestCase("HİDDEN")]
        public void InvalidAccessTokensAreRejected(string token)
        {
            Assert.That(OptionalRegionTokenCodec.TryParseAccessRule(token, out _), Is.False);
        }

        [TestCase("NONE", OptionalRewardTier.None)]
        [TestCase("LOW", OptionalRewardTier.Low)]
        [TestCase("MEDIUM", OptionalRewardTier.Medium)]
        [TestCase("HIGH", OptionalRewardTier.High)]
        [TestCase("UNIQUE", OptionalRewardTier.Unique)]
        public void RewardTokensRoundTrip(string token, OptionalRewardTier value)
        {
            Assert.That(OptionalRegionTokenCodec.TryParseRewardTier(token, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(value));
            Assert.That(OptionalRegionTokenCodec.ToToken(value), Is.EqualTo(token));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("none")]
        [TestCase("None")]
        [TestCase(" NONE")]
        [TestCase("NONE ")]
        [TestCase("0")]
        [TestCase("5")]
        [TestCase("RARE")]
        [TestCase("MAX")]
        [TestCase("UNIQUE\t")]
        public void InvalidRewardTokensAreRejected(string token)
        {
            Assert.That(OptionalRegionTokenCodec.TryParseRewardTier(token, out _), Is.False);
        }

        [TestCase("BACKTRACK", OptionalReturnPolicy.BacktrackToAttachment)]
        [TestCase("RETURN_GATE", OptionalReturnPolicy.ReturnGateToMandatory)]
        [TestCase("SAFE_EXIT", OptionalReturnPolicy.SafeExitToMandatory)]
        public void ReturnTokensRoundTrip(string token, OptionalReturnPolicy value)
        {
            Assert.That(OptionalRegionTokenCodec.TryParseReturnPolicy(token, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(value));
            Assert.That(OptionalRegionTokenCodec.ToToken(value), Is.EqualTo(token));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("backtrack")]
        [TestCase("Backtrack")]
        [TestCase(" BACKTRACK")]
        [TestCase("BACKTRACK ")]
        [TestCase("0")]
        [TestCase("3")]
        [TestCase("RETURN")]
        [TestCase("GATE")]
        [TestCase("SAFE-EXIT")]
        public void InvalidReturnTokensAreRejected(string token)
        {
            Assert.That(OptionalRegionTokenCodec.TryParseReturnPolicy(token, out _), Is.False);
        }

        [Test]
        public void UndefinedAccessEnumCannotSerialize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OptionalRegionTokenCodec.ToToken((OptionalRegionAccessRule)99));
        }

        [Test]
        public void UndefinedRewardEnumCannotSerialize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OptionalRegionTokenCodec.ToToken((OptionalRewardTier)99));
        }

        [Test]
        public void UndefinedReturnEnumCannotSerialize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OptionalRegionTokenCodec.ToToken((OptionalReturnPolicy)99));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ValidDepthsRoundTrip(int value)
        {
            Assert.That(OptionalRegionDepth.TryCreate(value, out var depth), Is.True);
            Assert.That(depth.IsValid, Is.True);
            Assert.That(depth.Value, Is.EqualTo(value));
            Assert.That(depth.ToString(), Is.EqualTo(value.ToString(CultureInfo.InvariantCulture)));
            Assert.That(new OptionalRegionDepth(value), Is.EqualTo(depth));
        }

        [TestCase(int.MinValue)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(99)]
        [TestCase(int.MaxValue)]
        public void InvalidDepthsFailTryCreate(int value)
        {
            Assert.That(OptionalRegionDepth.TryCreate(value, out var depth), Is.False);
            Assert.That(depth.IsValid, Is.False);
        }

        [TestCase(0)]
        [TestCase(5)]
        public void InvalidDepthConstructorThrows(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OptionalRegionDepth(value));
        }

        [Test]
        public void DefaultDepthIsInvalid()
        {
            var depth = default(OptionalRegionDepth);
            Assert.That(depth.IsValid, Is.False);
            Assert.That(depth.Value, Is.Zero);
        }

        [TestCase(-1, 0)]
        [TestCase(1, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 1)]
        public void AttachmentAcceptsEveryCardinalDirection(int dx, int dy)
        {
            var mandatory = new SectorCoord(6, 6);
            var entry = new SectorCoord(6 + dx, 6 + dy);
            var attachment = CreateAttachment(
                new OptionalRegionId("REGION_A"),
                WorldGridIndex.ToIndex(mandatory),
                WorldGridIndex.ToIndex(entry),
                dx,
                dy,
                12);
            Assert.That(attachment.EntrySideFromMandatoryDx, Is.EqualTo(dx));
            Assert.That(attachment.EntrySideFromMandatoryDy, Is.EqualTo(dy));
            Assert.That(attachment.InitialDepth.Value, Is.EqualTo(1));
            Assert.That(attachment.AttachmentOrder, Is.EqualTo(12));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(-1, -1)]
        [TestCase(2, 0)]
        [TestCase(0, 2)]
        [TestCase(-2, 0)]
        [TestCase(0, -2)]
        [TestCase(2, 2)]
        public void AttachmentRejectsNonCardinalDirection(int dx, int dy)
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionAttachment(
                new OptionalRegionId("REGION_A"),
                0,
                84,
                WorldGridIndex.ToCoordinate(84),
                new MandatoryRouteGraphNodeId("NODE_084_MANDATORY"),
                85,
                WorldGridIndex.ToCoordinate(85),
                dx,
                dy,
                new OptionalRegionDepth(1)));
        }

        [TestCase(-1)]
        [TestCase(10000)]
        public void AttachmentRejectsOrderOutsideRange(int order)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateAttachment(new OptionalRegionId("REGION_A"), 84, 85, 1, 0, order));
        }

        [TestCase(83, 1, 0)]
        [TestCase(97, 1, 0)]
        public void AttachmentRejectsDirectionThatDoesNotMatchEntry(int entryIndex, int dx, int dy)
        {
            Assert.Throws<ArgumentException>(() => CreateAttachment(new OptionalRegionId("REGION_A"), 84, entryIndex, dx, dy));
        }

        [TestCase(-1)]
        [TestCase(169)]
        public void AttachmentRejectsInvalidMandatoryIndex(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OptionalRegionAttachment(
                new OptionalRegionId("REGION_A"), 0, index, new SectorCoord(0, 0),
                new MandatoryRouteGraphNodeId("NODE_084_MANDATORY"), 85, WorldGridIndex.ToCoordinate(85),
                1, 0, new OptionalRegionDepth(1)));
        }

        [Test]
        public void AttachmentRejectsInvalidRegionId()
        {
            Assert.Throws<ArgumentException>(() => CreateAttachment(default(OptionalRegionId), 84, 85, 1, 0));
        }

        [Test]
        public void AttachmentRejectsInvalidGraphNodeId()
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionAttachment(
                new OptionalRegionId("REGION_A"), 0, 84, WorldGridIndex.ToCoordinate(84),
                default(MandatoryRouteGraphNodeId), 85, WorldGridIndex.ToCoordinate(85),
                1, 0, new OptionalRegionDepth(1)));
        }

        [Test]
        public void AttachmentRejectsNonInitialDepth()
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionAttachment(
                new OptionalRegionId("REGION_A"), 0, 84, WorldGridIndex.ToCoordinate(84),
                new MandatoryRouteGraphNodeId("NODE_084_MANDATORY"), 85, WorldGridIndex.ToCoordinate(85),
                1, 0, new OptionalRegionDepth(2)));
        }

        [Test]
        public void AttachmentRejectsCoordinateIdentityMismatch()
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionAttachment(
                new OptionalRegionId("REGION_A"), 0, 84, WorldGridIndex.ToCoordinate(85),
                new MandatoryRouteGraphNodeId("NODE_084_MANDATORY"), 85, WorldGridIndex.ToCoordinate(85),
                1, 0, new OptionalRegionDepth(1)));
        }

        [TestCase(1, false, false)]
        [TestCase(1, false, true)]
        [TestCase(2, false, false)]
        [TestCase(2, false, true)]
        [TestCase(3, false, false)]
        [TestCase(4, false, true)]
        [TestCase(1, true, false)]
        [TestCase(1, true, true)]
        public void CellPreservesDepthAndFlags(int depth, bool attachment, bool requiresReturn)
        {
            var cell = CreateCell(new OptionalRegionId("REGION_A"), 85, depth, attachment, requiresReturn);
            Assert.That(cell.Depth.Value, Is.EqualTo(depth));
            Assert.That(cell.IsAttachmentCell, Is.EqualTo(attachment));
            Assert.That(cell.RequiresReturnConnection, Is.EqualTo(requiresReturn));
            Assert.That(cell.Sector, Is.EqualTo(WorldGridIndex.ToCoordinate(85)));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void AttachmentCellRejectsDepthAboveOne(int depth)
        {
            Assert.Throws<ArgumentException>(() => CreateCell(new OptionalRegionId("REGION_A"), 85, depth, true, false));
        }

        [TestCase(-1)]
        [TestCase(169)]
        public void CellRejectsInvalidIndex(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OptionalRegionCell(
                new OptionalRegionId("REGION_A"), index, new SectorCoord(0, 0),
                new OptionalRegionDepth(1), false, false));
        }

        [Test]
        public void CellRejectsInvalidRegionId()
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionCell(
                default(OptionalRegionId), 85, WorldGridIndex.ToCoordinate(85),
                new OptionalRegionDepth(1), false, false));
        }

        [Test]
        public void CellRejectsDefaultDepth()
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionCell(
                new OptionalRegionId("REGION_A"), 85, WorldGridIndex.ToCoordinate(85),
                default(OptionalRegionDepth), false, false));
        }

        [Test]
        public void CellRejectsCoordinateIdentityMismatch()
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionCell(
                new OptionalRegionId("REGION_A"), 85, WorldGridIndex.ToCoordinate(84),
                new OptionalRegionDepth(1), false, false));
        }

        [Test]
        public void RegionCopiesSortsAndFreezesCells()
        {
            var id = new OptionalRegionId("REGION_A");
            var source = new List<OptionalRegionCell>
            {
                CreateCell(id, 98, 2, false, true),
                CreateCell(id, 85, 1, true, false)
            };
            var region = CreateRegion(id, 84, 85, source, 2);
            source.Clear();
            Assert.That(region.Cells.Select(cell => cell.SectorIndex), Is.EqualTo(new[] { 85, 98 }));
            Assert.Throws<NotSupportedException>(() => ((IList<OptionalRegionCell>)region.Cells).Add(CreateCell(id, 99, 3, false, false)));
            Assert.That(region.MaxDepth.Value, Is.EqualTo(2));
        }

        [TestCase(OptionalRegionAccessRule.Basic, OptionalRewardTier.None, OptionalReturnPolicy.BacktrackToAttachment)]
        [TestCase(OptionalRegionAccessRule.Tool, OptionalRewardTier.Low, OptionalReturnPolicy.ReturnGateToMandatory)]
        [TestCase(OptionalRegionAccessRule.Environment, OptionalRewardTier.Medium, OptionalReturnPolicy.SafeExitToMandatory)]
        [TestCase(OptionalRegionAccessRule.Explosive, OptionalRewardTier.High, OptionalReturnPolicy.BacktrackToAttachment)]
        [TestCase(OptionalRegionAccessRule.Hidden, OptionalRewardTier.Unique, OptionalReturnPolicy.SafeExitToMandatory)]
        public void RegionPreservesEnumData(OptionalRegionAccessRule access, OptionalRewardTier reward, OptionalReturnPolicy policy)
        {
            var region = CreateRegion(access, reward, policy);
            Assert.That(region.AccessRule, Is.EqualTo(access));
            Assert.That(region.RewardTier, Is.EqualTo(reward));
            Assert.That(region.ReturnPolicy, Is.EqualTo(policy));
        }

        [Test]
        public void HiddenRegionMayHaveNoReward()
        {
            var region = CreateRegion(OptionalRegionAccessRule.Hidden, OptionalRewardTier.None, OptionalReturnPolicy.BacktrackToAttachment);
            Assert.That(region.RewardTier, Is.EqualTo(OptionalRewardTier.None));
        }

        [Test]
        public void RegionRejectsMismatchedAttachmentId()
        {
            var id = new OptionalRegionId("REGION_A");
            var other = new OptionalRegionId("REGION_B");
            Assert.Throws<ArgumentException>(() => new OptionalRegion(
                id, CreateAttachment(other, 84, 85, 1, 0), OptionalRegionAccessRule.Basic,
                OptionalRewardTier.Low, OptionalReturnPolicy.BacktrackToAttachment,
                new[] { CreateCell(id, 85, 1, true, false) }, new OptionalRegionDepth(1)));
        }

        [Test]
        public void RegionRejectsMismatchedCellId()
        {
            var id = new OptionalRegionId("REGION_A");
            var other = new OptionalRegionId("REGION_B");
            Assert.Throws<ArgumentException>(() => new OptionalRegion(
                id, CreateAttachment(id, 84, 85, 1, 0), OptionalRegionAccessRule.Basic,
                OptionalRewardTier.Low, OptionalReturnPolicy.BacktrackToAttachment,
                new[] { CreateCell(other, 85, 1, true, false) }, new OptionalRegionDepth(1)));
        }

        [Test]
        public void RegionRejectsDuplicateSector()
        {
            var id = new OptionalRegionId("REGION_A");
            Assert.Throws<ArgumentException>(() => new OptionalRegion(
                id, CreateAttachment(id, 84, 85, 1, 0), OptionalRegionAccessRule.Basic,
                OptionalRewardTier.Low, OptionalReturnPolicy.BacktrackToAttachment,
                new[] { CreateCell(id, 85, 1, true, false), CreateCell(id, 85, 1, false, true) },
                new OptionalRegionDepth(1)));
        }

        [Test]
        public void RegionRejectsEmptyCells()
        {
            var id = new OptionalRegionId("REGION_A");
            Assert.Throws<ArgumentException>(() => new OptionalRegion(
                id, CreateAttachment(id, 84, 85, 1, 0), OptionalRegionAccessRule.Basic,
                OptionalRewardTier.Low, OptionalReturnPolicy.BacktrackToAttachment,
                Array.Empty<OptionalRegionCell>(), new OptionalRegionDepth(1)));
        }

        [Test]
        public void RegionRejectsMissingAttachmentCell()
        {
            var id = new OptionalRegionId("REGION_A");
            Assert.Throws<ArgumentException>(() => new OptionalRegion(
                id, CreateAttachment(id, 84, 85, 1, 0), OptionalRegionAccessRule.Basic,
                OptionalRewardTier.Low, OptionalReturnPolicy.BacktrackToAttachment,
                new[] { CreateCell(id, 98, 2, false, false) }, new OptionalRegionDepth(2)));
        }

        [Test]
        public void RegionRejectsTwoAttachmentCells()
        {
            var id = new OptionalRegionId("REGION_A");
            Assert.Throws<ArgumentException>(() => new OptionalRegion(
                id, CreateAttachment(id, 84, 85, 1, 0), OptionalRegionAccessRule.Basic,
                OptionalRewardTier.Low, OptionalReturnPolicy.BacktrackToAttachment,
                new[] { CreateCell(id, 85, 1, true, false), CreateCell(id, 98, 1, true, false) },
                new OptionalRegionDepth(1)));
        }

        [Test]
        public void RegionRejectsWrongMaxDepth()
        {
            var id = new OptionalRegionId("REGION_A");
            Assert.Throws<ArgumentException>(() => CreateRegion(
                id, 84, 85,
                new[] { CreateCell(id, 85, 1, true, false), CreateCell(id, 98, 2, false, false) },
                1));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void RegionRejectsUndefinedAccessRule(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateRegion((OptionalRegionAccessRule)value, OptionalRewardTier.Low, OptionalReturnPolicy.BacktrackToAttachment));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void RegionRejectsUndefinedRewardTier(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateRegion(OptionalRegionAccessRule.Basic, (OptionalRewardTier)value, OptionalReturnPolicy.BacktrackToAttachment));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void RegionRejectsUndefinedReturnPolicy(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateRegion(OptionalRegionAccessRule.Basic, OptionalRewardTier.Low, (OptionalReturnPolicy)value));
        }

        [Test]
        public void EmptySnapshotPreservesMandatoryIdentity()
        {
            var snapshot = CreateEmptySnapshot();
            Assert.That(snapshot.IsEmpty, Is.True);
            Assert.That(snapshot.Regions, Is.Empty);
            Assert.That(snapshot.Cells, Is.Empty);
            Assert.That(snapshot.MandatoryRouteSectorIndices, Has.Count.EqualTo(47));
            Assert.That(snapshot.SourceMandatoryNodeCount, Is.EqualTo(47));
            Assert.That(snapshot.SourceMandatoryDirectedEdgeCount, Is.EqualTo(96));
            Assert.That(snapshot.SourceMandatoryRouteCellCount, Is.EqualTo(47));
            Assert.That(snapshot.SourceMandatoryGraphDigest, Is.EqualTo(GraphDigest()));
        }

        [Test]
        public void SnapshotCopiesSortsAndFreezesEveryCollection()
        {
            var regionB = CreateSnapshotRegion("REGION_B", 45, 58, 71);
            var regionA = CreateSnapshotRegion("REGION_A", 46, 47, 60);
            var regions = new List<OptionalRegion> { regionB, regionA };
            var cells = new List<OptionalRegionCell>(regionB.Cells.Concat(regionA.Cells).Reverse());
            var mandatory = MandatoryIndices().Reverse().ToList();
            var snapshot = CreateSnapshot(regions, cells, mandatory);
            regions.Clear();
            cells.Clear();
            mandatory.Clear();
            Assert.That(snapshot.Regions.Select(region => region.RegionId.Value), Is.EqualTo(new[] { "REGION_A", "REGION_B" }));
            Assert.That(snapshot.Cells.Select(cell => cell.SectorIndex), Is.EqualTo(new[] { 47, 58, 60, 71 }));
            Assert.That(snapshot.MandatoryRouteSectorIndices, Is.EqualTo(Enumerable.Range(0, 47)));
            Assert.Throws<NotSupportedException>(() => ((IList<OptionalRegion>)snapshot.Regions).Add(regionA));
            Assert.Throws<NotSupportedException>(() => ((IList<OptionalRegionCell>)snapshot.Cells).Add(regionA.Cells[0]));
            Assert.Throws<NotSupportedException>(() => ((IList<int>)snapshot.MandatoryRouteSectorIndices).Add(99));
        }

        [TestCase(46)]
        [TestCase(48)]
        public void SnapshotRejectsWrongMandatoryNodeCount(int value)
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionSnapshot(
                Array.Empty<OptionalRegion>(), Array.Empty<OptionalRegionCell>(), MandatoryIndices(),
                value, 96, 47, GraphDigest()));
        }

        [TestCase(95)]
        [TestCase(97)]
        public void SnapshotRejectsWrongDirectedEdgeCount(int value)
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionSnapshot(
                Array.Empty<OptionalRegion>(), Array.Empty<OptionalRegionCell>(), MandatoryIndices(),
                47, value, 47, GraphDigest()));
        }

        [TestCase(46)]
        [TestCase(48)]
        public void SnapshotRejectsWrongMandatoryCellCount(int value)
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionSnapshot(
                Array.Empty<OptionalRegion>(), Array.Empty<OptionalRegionCell>(), MandatoryIndices(),
                47, 96, value, GraphDigest()));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase(" DIGEST")]
        [TestCase("DIGEST ")]
        public void SnapshotRejectsInvalidDigest(string digest)
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionSnapshot(
                Array.Empty<OptionalRegion>(), Array.Empty<OptionalRegionCell>(), MandatoryIndices(),
                47, 96, 47, digest));
        }

        [Test]
        public void SnapshotRejectsDuplicateRegionId()
        {
            var region = CreateSnapshotRegion("REGION_A", 46, 47, 60);
            Assert.Throws<ArgumentException>(() => CreateSnapshot(new[] { region, region }, region.Cells, MandatoryIndices()));
        }

        [Test]
        public void SnapshotRejectsDuplicateOptionalSector()
        {
            var region = CreateSnapshotRegion("REGION_A", 46, 47, 60);
            Assert.Throws<ArgumentException>(() => CreateSnapshot(new[] { region }, new[] { region.Cells[0], region.Cells[0] }, MandatoryIndices()));
        }

        [Test]
        public void SnapshotRejectsMandatoryOverlap()
        {
            var id = new OptionalRegionId("REGION_A");
            var region = CreateRegion(
                id, 46, 47,
                new[] { CreateCell(id, 47, 1, true, false), CreateCell(id, 1, 2, false, false) },
                2);
            Assert.Throws<ArgumentException>(() => CreateSnapshot(new[] { region }, region.Cells, MandatoryIndices()));
        }

        [Test]
        public void SnapshotRejectsCellFromUnknownRegion()
        {
            var region = CreateSnapshotRegion("REGION_A", 46, 47, 60);
            var unknown = CreateCell(new OptionalRegionId("REGION_B"), 61, 1, true, false);
            Assert.Throws<ArgumentException>(() => CreateSnapshot(new[] { region }, new[] { unknown }, MandatoryIndices()));
        }

        [Test]
        public void SnapshotRejectsCellsThatDoNotMatchAggregate()
        {
            var region = CreateSnapshotRegion("REGION_A", 46, 47, 60);
            Assert.Throws<ArgumentException>(() => CreateSnapshot(new[] { region }, new[] { region.Cells[0] }, MandatoryIndices()));
        }

        [Test]
        public void SnapshotRejectsRegionWithoutPublishedCells()
        {
            var region = CreateSnapshotRegion("REGION_A", 46, 47, 60);
            Assert.Throws<ArgumentException>(() => CreateSnapshot(new[] { region }, Array.Empty<OptionalRegionCell>(), MandatoryIndices()));
        }

        [Test]
        public void SnapshotRejectsCellsWithoutRegions()
        {
            var id = new OptionalRegionId("REGION_A");
            Assert.Throws<ArgumentException>(() => CreateSnapshot(
                Array.Empty<OptionalRegion>(), new[] { CreateCell(id, 47, 1, true, false) }, MandatoryIndices()));
        }

        [Test]
        public void SnapshotRejectsDuplicateMandatoryIndex()
        {
            var indices = MandatoryIndices().ToList();
            indices[46] = 45;
            Assert.Throws<ArgumentException>(() => new OptionalRegionSnapshot(
                Array.Empty<OptionalRegion>(), Array.Empty<OptionalRegionCell>(), indices,
                47, 96, 47, GraphDigest()));
        }

        [TestCase(46)]
        [TestCase(48)]
        public void SnapshotRejectsWrongMandatoryIndexCardinality(int count)
        {
            Assert.Throws<ArgumentException>(() => new OptionalRegionSnapshot(
                Array.Empty<OptionalRegion>(), Array.Empty<OptionalRegionCell>(), Enumerable.Range(0, count),
                47, 96, 47, GraphDigest()));
        }

        [Test]
        public void SnapshotRejectsAttachmentOutsideMandatoryRoute()
        {
            var region = CreateSnapshotRegion("REGION_A", 84, 85, 98);
            Assert.Throws<ArgumentException>(() => CreateSnapshot(new[] { region }, region.Cells, MandatoryIndices()));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void RepeatedConstructionHasOneDeterministicSignature(int permutation)
        {
            var region = CreateSnapshotRegion("REGION_A", 46, 47, 60);
            var cells = (permutation & 1) == 0 ? region.Cells : region.Cells.Reverse();
            var mandatory = (permutation & 2) == 0 ? MandatoryIndices() : MandatoryIndices().Reverse();
            var snapshot = CreateSnapshot(new[] { region }, cells, mandatory);
            Assert.That(Signature(snapshot), Is.EqualTo("REGION_A|47:1:True;60:2:False|0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46|" + GraphDigest()));
        }

        [Test]
        public void RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap06_04PlusSymbols()
        {
            var types = OptionalTypes();
            foreach (var type in types)
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            var assembly = typeof(OptionalRegion).Assembly;
            Assert.That(assembly.GetReferencedAssemblies().Any(value => value.Name == "UnityEditor"), Is.False);
            var names = string.Join("|", assembly.GetTypes().Select(value => value.Name));
            foreach (var forbidden in new[]
            {
                "GeneratedOptionalRegionCsvWriter", "OptionalClueAssigner",
                "OptionalAccessAssigner", "OptionalReturnConnection", "OptionalReturnDevice",
                "OptionalRegionOverlayRenderer", "OptionalRegionValidationOverlayWindow"
            })
                Assert.That(names, Does.Not.Contain(forbidden));
        }

        [Test]
        public void OptionalModelsExposeNoRouteMaskOrGeneratedWriterSurface()
        {
            var surface = string.Join("|", OptionalTypes().SelectMany(type =>
                type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                    .Select(member => member.Name)));
            Assert.That(surface, Does.Not.Contain("RouteMask"));
            Assert.That(surface, Does.Not.Contain("GeneratedEdge"));
            Assert.That(surface, Does.Not.Contain("GeneratedCsv"));
            Assert.That(surface, Does.Not.Contain("RewardSpawn"));
            Assert.That(surface, Does.Not.Contain("ClueId"));
        }

        [Test]
        public void OptionalModelsExposeNoRngClockFilesystemOrUnityLifecycle()
        {
            var types = OptionalTypes();
            Assert.That(types.All(type => type.BaseType == typeof(object) || type.IsValueType || type.IsAbstract && type.IsSealed), Is.True);
            var surface = string.Join("|", types.SelectMany(type =>
                type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                    .Select(member => member.ToString())));
            Assert.That(surface, Does.Not.Contain("Random"));
            Assert.That(surface, Does.Not.Contain("Rng"));
            Assert.That(surface, Does.Not.Contain("File"));
            Assert.That(surface, Does.Not.Contain("Clock"));
            Assert.That(surface, Does.Not.Contain("MonoBehaviour"));
        }

        [Test]
        public void Map05Type4ContractIsNotCanonicalizedByOptionalModels()
        {
            var surface = string.Join("|", OptionalTypes().SelectMany(type =>
                type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                    .Select(property => property.Name)));
            Assert.That(surface, Does.Not.Contain("OpenLeft"));
            Assert.That(surface, Does.Not.Contain("OpenRight"));
            Assert.That(surface, Does.Not.Contain("Type4"));
            Assert.That(new[] { "UD", "LUD", "RUD", "LRUD" }, Has.Length.EqualTo(4));
        }

        private static Type[] OptionalTypes()
        {
            return new[]
            {
                typeof(OptionalRegionId), typeof(OptionalRegionAccessRule), typeof(OptionalRewardTier),
                typeof(OptionalReturnPolicy), typeof(OptionalRegionDepth), typeof(OptionalRegionTokenCodec),
                typeof(OptionalRegionAttachment), typeof(OptionalRegionCell), typeof(OptionalRegion),
                typeof(OptionalRegionSnapshot)
            };
        }

        private static OptionalRegionAttachment CreateAttachment(
            OptionalRegionId id,
            int mandatoryIndex,
            int entryIndex,
            int dx,
            int dy,
            int order = 0)
        {
            return new OptionalRegionAttachment(
                id,
                order,
                mandatoryIndex,
                WorldGridIndex.ToCoordinate(mandatoryIndex),
                new MandatoryRouteGraphNodeId("NODE_" + mandatoryIndex.ToString("D3", CultureInfo.InvariantCulture) + "_MANDATORY"),
                entryIndex,
                WorldGridIndex.ToCoordinate(entryIndex),
                dx,
                dy,
                new OptionalRegionDepth(1));
        }

        private static OptionalRegionCell CreateCell(
            OptionalRegionId id,
            int index,
            int depth,
            bool attachment,
            bool requiresReturn)
        {
            return new OptionalRegionCell(
                id,
                index,
                WorldGridIndex.ToCoordinate(index),
                new OptionalRegionDepth(depth),
                attachment,
                requiresReturn);
        }

        private static OptionalRegion CreateRegion(
            OptionalRegionId id,
            int mandatoryIndex,
            int entryIndex,
            IEnumerable<OptionalRegionCell> cells,
            int maxDepth)
        {
            var mandatory = WorldGridIndex.ToCoordinate(mandatoryIndex);
            var entry = WorldGridIndex.ToCoordinate(entryIndex);
            return new OptionalRegion(
                id,
                CreateAttachment(id, mandatoryIndex, entryIndex, entry.X - mandatory.X, entry.Y - mandatory.Y),
                OptionalRegionAccessRule.Basic,
                OptionalRewardTier.Low,
                OptionalReturnPolicy.BacktrackToAttachment,
                cells,
                new OptionalRegionDepth(maxDepth));
        }

        private static OptionalRegion CreateRegion(
            OptionalRegionAccessRule access,
            OptionalRewardTier reward,
            OptionalReturnPolicy policy)
        {
            var id = new OptionalRegionId("REGION_A");
            return new OptionalRegion(
                id,
                CreateAttachment(id, 84, 85, 1, 0),
                access,
                reward,
                policy,
                new[] { CreateCell(id, 85, 1, true, false) },
                new OptionalRegionDepth(1));
        }

        private static OptionalRegion CreateSnapshotRegion(string idValue, int mandatoryIndex, int entryIndex, int depthTwoIndex)
        {
            var id = new OptionalRegionId(idValue);
            return CreateRegion(
                id,
                mandatoryIndex,
                entryIndex,
                new[]
                {
                    CreateCell(id, depthTwoIndex, 2, false, true),
                    CreateCell(id, entryIndex, 1, true, false)
                },
                2);
        }

        private static IEnumerable<int> MandatoryIndices()
        {
            return Enumerable.Range(0, 47);
        }

        private static string GraphDigest()
        {
            return "08fe445a875777b7bb783690f88f415b60f0be255823f9f5d0cbbab1a07d2ca0";
        }

        private static OptionalRegionSnapshot CreateEmptySnapshot()
        {
            return new OptionalRegionSnapshot(
                Array.Empty<OptionalRegion>(),
                Array.Empty<OptionalRegionCell>(),
                MandatoryIndices(),
                47,
                96,
                47,
                GraphDigest());
        }

        private static OptionalRegionSnapshot CreateSnapshot(
            IEnumerable<OptionalRegion> regions,
            IEnumerable<OptionalRegionCell> cells,
            IEnumerable<int> mandatory)
        {
            return new OptionalRegionSnapshot(regions, cells, mandatory, 47, 96, 47, GraphDigest());
        }

        private static string Signature(OptionalRegionSnapshot snapshot)
        {
            return string.Join(",", snapshot.Regions.Select(region => region.RegionId.Value)) + "|" +
                string.Join(";", snapshot.Cells.Select(cell =>
                    cell.SectorIndex + ":" + cell.Depth.Value + ":" + cell.IsAttachmentCell)) + "|" +
                string.Join(",", snapshot.MandatoryRouteSectorIndices) + "|" +
                snapshot.SourceMandatoryGraphDigest;
        }
    }
}
