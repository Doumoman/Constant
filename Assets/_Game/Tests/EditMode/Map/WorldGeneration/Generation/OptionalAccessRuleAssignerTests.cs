using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP06_05")]
    public sealed class OptionalAccessRuleAssignerTests
    {
        private Type0RouteMaskAssignmentResult type0;
        private OptionalAccessAssignmentSettings settings;
        private OptionalAccessAssignmentResult baseline;
        private MandatoryRouteGraph graph;
        private string sourceSignature;

        public static IEnumerable<int> ClueAndEnumCases => Enumerable.Range(0, 38);
        public static IEnumerable<int> SettingsCases => Enumerable.Range(0, 38);
        public static IEnumerable<int> MatrixCases => Enumerable.Range(0, 44);
        public static IEnumerable<int> CyclingCases => Enumerable.Range(0, 30);
        public static IEnumerable<int> AttachmentCases => Enumerable.Range(0, 34);
        public static IEnumerable<int> ClueCases => Enumerable.Range(0, 28);
        public static IEnumerable<int> DigestCases => Enumerable.Range(0, 28);
        public static IEnumerable<int> AtomicCases => Enumerable.Range(0, 24);
        public static IEnumerable<int> IntegrityCases => Enumerable.Range(0, 24);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var sourceFixture = new Type0RouteMaskAssignerTests();
            sourceFixture.OneTimeSetUp();
            type0 = GetField<Type0RouteMaskAssignmentResult>(
                sourceFixture, typeof(Type0RouteMaskAssignerTests), "baseline");
            graph = GetField<MandatoryRouteGraph>(
                sourceFixture, typeof(Type0RouteMaskAssignerTests), "graph");
            settings = ApprovedSettings();
            baseline = new OptionalAccessRuleAssigner().Assign(type0, settings);
            sourceSignature = SourceSignature();

            Assert.That(type0.IsSuccess, Is.True);
            Assert.That(baseline.IsSuccess, Is.True, FormatErrors(baseline));
            Assert.That(baseline.Assignments, Has.Count.EqualTo(12));
            Assert.That(baseline.Clues, Has.Count.EqualTo(12));
        }

        [TestCaseSource(nameof(ClueAndEnumCases))]
        public void ClueIdEnumsTokensAndValueImmutabilityAreExact(int caseId)
        {
            var requirements = new[]
            {
                OptionalAccessRequirement.None, OptionalAccessRequirement.Pickaxe,
                OptionalAccessRequirement.Shovel, OptionalAccessRequirement.Rope,
                OptionalAccessRequirement.Explosive, OptionalAccessRequirement.Environment
            };
            var requirementTokens = new[]
            {
                "NONE", "PICKAXE", "SHOVEL", "ROPE", "EXPLOSIVE", "ENVIRONMENT"
            };
            var clueKinds = new[]
            {
                OptionalAccessClueKind.BasicOpening, OptionalAccessClueKind.ToolSurface,
                OptionalAccessClueKind.EnvironmentDevice, OptionalAccessClueKind.ExplosiveRewardPreview,
                OptionalAccessClueKind.HiddenCrack, OptionalAccessClueKind.HiddenLight,
                OptionalAccessClueKind.HiddenSound
            };
            var clueTokens = new[]
            {
                "BASIC_OPENING", "TOOL_SURFACE", "ENVIRONMENT_DEVICE",
                "EXPLOSIVE_REWARD_PREVIEW", "HIDDEN_CRACK", "HIDDEN_LIGHT", "HIDDEN_SOUND"
            };

            if (caseId < 6)
            {
                Assert.That(OptionalAccessAssignmentEnums.TryParseRequirement(
                    requirementTokens[caseId], out var value), Is.True);
                Assert.That(value, Is.EqualTo(requirements[caseId]));
                Assert.That(OptionalAccessAssignmentEnums.ToToken(value), Is.EqualTo(requirementTokens[caseId]));
                return;
            }

            if (caseId < 13)
            {
                var index = caseId - 6;
                Assert.That(OptionalAccessAssignmentEnums.TryParseClueKind(
                    clueTokens[index], out var value), Is.True);
                Assert.That(value, Is.EqualTo(clueKinds[index]));
                Assert.That(OptionalAccessAssignmentEnums.ToToken(value), Is.EqualTo(clueTokens[index]));
                return;
            }

            if (caseId < 15)
            {
                var value = caseId == 13
                    ? OptionalAccessTraversalKind.OptionalBreak
                    : OptionalAccessTraversalKind.Hidden;
                var token = caseId == 13 ? "OPTIONAL_BREAK" : "HIDDEN";
                Assert.That(OptionalAccessAssignmentEnums.TryParseTraversalKind(token, out var parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(value));
                Assert.That(OptionalAccessAssignmentEnums.ToToken(value), Is.EqualTo(token));
                return;
            }

            if (caseId < 23)
            {
                var invalid = new[]
                {
                    null, string.Empty, " ", "pickaxe", "Pickaxe", "1", "HIDDEN ", " BASIC_OPENING"
                }[caseId - 15];
                Assert.That(OptionalAccessAssignmentEnums.TryParseRequirement(invalid, out _), Is.False);
                Assert.That(OptionalAccessAssignmentEnums.TryParseClueKind(invalid, out _), Is.False);
                Assert.That(OptionalAccessAssignmentEnums.TryParseTraversalKind(invalid, out _), Is.False);
                return;
            }

            if (caseId < 35)
            {
                var ordinal = caseId - 23;
                var value = "CLUE_OPT_REGION_" + ordinal.ToString("D4", CultureInfo.InvariantCulture) + "_BASIC";
                var id = new OptionalAccessClueId(value);
                Assert.That(id.IsValid, Is.True);
                Assert.That(id.Value, Is.EqualTo(value));
                Assert.That(OptionalAccessClueId.TryCreate(value, out var parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(id));
                Assert.That(parsed.CompareTo(id), Is.Zero);
                Assert.That(parsed.GetHashCode(), Is.EqualTo(id.GetHashCode()));
                return;
            }

            var invalidId = new[]
            {
                "CLUE_OPT_REGION_000_BASIC", "CLUE_OPT_REGION_0000_", "clue_OPT_REGION_0000_BASIC"
            }[caseId - 35];
            Assert.That(OptionalAccessClueId.TryCreate(invalidId, out var rejected), Is.False);
            Assert.That(rejected.IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => new OptionalAccessClueId(invalidId));
        }

        [TestCaseSource(nameof(SettingsCases))]
        public void SettingsValidateCopyAndPublishExactDepthTables(int caseId)
        {
            if (caseId < 18)
            {
                Assert.That(settings.AccessRulePattern, Is.EqualTo(new[]
                {
                    OptionalRegionAccessRule.Basic, OptionalRegionAccessRule.Tool,
                    OptionalRegionAccessRule.Environment, OptionalRegionAccessRule.Explosive,
                    OptionalRegionAccessRule.Hidden
                }));
                Assert.That(settings.ToolRequirementPattern, Is.EqualTo(new[]
                {
                    OptionalAccessRequirement.Pickaxe, OptionalAccessRequirement.Shovel,
                    OptionalAccessRequirement.Rope
                }));
                Assert.That(settings.HiddenCluePattern, Is.EqualTo(new[]
                {
                    OptionalAccessClueKind.HiddenCrack, OptionalAccessClueKind.HiddenLight,
                    OptionalAccessClueKind.HiddenSound
                }));
                Assert.That(settings.ToolCostTierByDepth, Is.EqualTo(new[] { 1, 2, 3, 4 }));
                Assert.That(settings.ExplosiveFuelCostByDepth, Is.EqualTo(new[] { 10, 20, 30, 40 }));
                Assert.That(settings.HiddenClueDifficultyByDepth, Is.EqualTo(new[] { 1, 2, 3, 4 }));
                return;
            }

            if (caseId < 26)
            {
                var access = new List<OptionalRegionAccessRule> { OptionalRegionAccessRule.Basic, OptionalRegionAccessRule.Tool };
                var tools = new List<OptionalAccessRequirement> { OptionalAccessRequirement.Pickaxe };
                var hidden = new List<OptionalAccessClueKind> { OptionalAccessClueKind.HiddenCrack };
                var toolCost = new List<int> { 1, 2, 3, 4 };
                var fuel = new List<int> { 10, 20, 30, 40 };
                var difficulty = new List<int> { 1, 2, 3, 4 };
                var copied = new OptionalAccessAssignmentSettings(access, tools, hidden, toolCost, fuel, difficulty);
                access[0] = OptionalRegionAccessRule.Hidden;
                tools[0] = OptionalAccessRequirement.Rope;
                hidden[0] = OptionalAccessClueKind.HiddenSound;
                toolCost[0] = 4;
                fuel[0] = 40;
                difficulty[0] = 4;
                Assert.That(copied.AccessRulePattern[0], Is.EqualTo(OptionalRegionAccessRule.Basic));
                Assert.That(copied.ToolRequirementPattern[0], Is.EqualTo(OptionalAccessRequirement.Pickaxe));
                Assert.That(copied.HiddenCluePattern[0], Is.EqualTo(OptionalAccessClueKind.HiddenCrack));
                Assert.That(copied.ToolCostTierByDepth[0], Is.EqualTo(1));
                Assert.That(copied.ExplosiveFuelCostByDepth[0], Is.EqualTo(10));
                Assert.That(copied.HiddenClueDifficultyByDepth[0], Is.EqualTo(1));
                return;
            }

            Assert.That(InvalidSettings(caseId - 26), Throws.Exception);
        }

        [TestCaseSource(nameof(MatrixCases))]
        public void FiveRuleDistributionAndConsistencyMatrixAreExact(int caseId)
        {
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            switch (assignment.AccessRule)
            {
                case OptionalRegionAccessRule.Basic:
                    AssertMatrix(assignment, OptionalAccessRequirement.None,
                        OptionalAccessTraversalKind.OptionalBreak, OptionalAccessClueKind.BasicOpening,
                        false, 0, 0, 0);
                    break;
                case OptionalRegionAccessRule.Tool:
                    Assert.That(new[]
                    {
                        OptionalAccessRequirement.Pickaxe, OptionalAccessRequirement.Shovel,
                        OptionalAccessRequirement.Rope
                    }, Does.Contain(assignment.Requirement));
                    AssertMatrix(assignment, assignment.Requirement,
                        OptionalAccessTraversalKind.OptionalBreak, OptionalAccessClueKind.ToolSurface,
                        false, assignment.ToolCostTier, 0, 0);
                    Assert.That(assignment.ToolCostTier, Is.InRange(1, 4));
                    break;
                case OptionalRegionAccessRule.Environment:
                    AssertMatrix(assignment, OptionalAccessRequirement.Environment,
                        OptionalAccessTraversalKind.OptionalBreak, OptionalAccessClueKind.EnvironmentDevice,
                        false, 0, 0, 0);
                    break;
                case OptionalRegionAccessRule.Explosive:
                    AssertMatrix(assignment, OptionalAccessRequirement.Explosive,
                        OptionalAccessTraversalKind.OptionalBreak, OptionalAccessClueKind.ExplosiveRewardPreview,
                        true, 0, assignment.ExplosiveFuelCost, 0);
                    Assert.That(assignment.ExplosiveFuelCost, Is.InRange(1, 100));
                    break;
                case OptionalRegionAccessRule.Hidden:
                    Assert.That(new[]
                    {
                        OptionalAccessClueKind.HiddenCrack, OptionalAccessClueKind.HiddenLight,
                        OptionalAccessClueKind.HiddenSound
                    }, Does.Contain(assignment.Clue.Kind));
                    AssertMatrix(assignment, OptionalAccessRequirement.None,
                        OptionalAccessTraversalKind.Hidden, assignment.Clue.Kind,
                        false, 0, 0, assignment.HiddenClueDifficulty);
                    Assert.That(assignment.HiddenClueDifficulty, Is.InRange(1, 4));
                    break;
            }

            Assert.That(baseline.Diagnostics.BasicCount, Is.EqualTo(3));
            Assert.That(baseline.Diagnostics.ToolCount, Is.EqualTo(3));
            Assert.That(baseline.Diagnostics.EnvironmentCount, Is.EqualTo(2));
            Assert.That(baseline.Diagnostics.ExplosiveCount, Is.EqualTo(2));
            Assert.That(baseline.Diagnostics.HiddenCount, Is.EqualTo(2));
        }

        [TestCaseSource(nameof(CyclingCases))]
        public void ToolAndHiddenPatternsCycleByTheirOwnRegionOrdinals(int caseId)
        {
            var tools = baseline.Assignments.Where(value => value.AccessRule == OptionalRegionAccessRule.Tool).ToArray();
            var hidden = baseline.Assignments.Where(value => value.AccessRule == OptionalRegionAccessRule.Hidden).ToArray();
            Assert.That(tools.Select(value => value.Requirement), Is.EqualTo(new[]
            {
                OptionalAccessRequirement.Pickaxe, OptionalAccessRequirement.Shovel,
                OptionalAccessRequirement.Rope
            }));
            Assert.That(hidden.Select(value => value.Clue.Kind), Is.EqualTo(new[]
            {
                OptionalAccessClueKind.HiddenCrack, OptionalAccessClueKind.HiddenLight
            }));
            Assert.That(settings.HiddenCluePattern[2], Is.EqualTo(OptionalAccessClueKind.HiddenSound));
            Assert.That(baseline.Diagnostics.PickaxeCount, Is.EqualTo(1));
            Assert.That(baseline.Diagnostics.ShovelCount, Is.EqualTo(1));
            Assert.That(baseline.Diagnostics.RopeCount, Is.EqualTo(1));
            Assert.That(baseline.Diagnostics.HiddenCrackCount, Is.EqualTo(1));
            Assert.That(baseline.Diagnostics.HiddenLightCount, Is.EqualTo(1));
            Assert.That(baseline.Diagnostics.HiddenSoundCount, Is.Zero);
            Assert.That(caseId, Is.InRange(0, 29));
        }

        [TestCaseSource(nameof(AttachmentCases))]
        public void AttachmentIdentityAndBaseClosedStateArePreserved(int caseId)
        {
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            var region = type0.SourceSnapshot.Regions.Single(value => value.RegionId == assignment.RegionId);
            var attachment = region.Attachment;
            Assert.That(assignment.AttachmentOrder, Is.EqualTo(attachment.AttachmentOrder));
            Assert.That(assignment.MandatoryRouteSectorIndex, Is.EqualTo(attachment.MandatoryRouteSectorIndex));
            Assert.That(assignment.MandatoryRouteSector, Is.EqualTo(attachment.MandatoryRouteSector));
            Assert.That(assignment.EntrySectorIndex, Is.EqualTo(attachment.EntrySectorIndex));
            Assert.That(assignment.EntrySector, Is.EqualTo(attachment.EntrySector));
            Assert.That(assignment.EntrySideFromMandatoryDx, Is.EqualTo(attachment.EntrySideFromMandatoryDx));
            Assert.That(assignment.EntrySideFromMandatoryDy, Is.EqualTo(attachment.EntrySideFromMandatoryDy));

            var type0Entry = type0.Assignments.Single(value => value.SectorIndex == attachment.EntrySectorIndex);
            Assert.That(IsOpen(type0Entry.OpenMask,
                -attachment.EntrySideFromMandatoryDx,
                -attachment.EntrySideFromMandatoryDy), Is.False);
            Assert.That(baseline.Diagnostics.AttachmentBoundaryBaseOpenCount, Is.Zero);
            Assert.That(type0.Diagnostics.AttachmentBoundaryClosedCount, Is.EqualTo(12));
            Assert.That(type0.Diagnostics.MandatoryBoundaryBaseOpenCount, Is.Zero);
            Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
        }

        [TestCaseSource(nameof(ClueCases))]
        public void EveryRegionHasOnePerceptibleClueAndExplosivePreviewReservation(int caseId)
        {
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            var clue = baseline.Clues.Single(value => value.RegionId == assignment.RegionId);
            Assert.That(clue, Is.SameAs(assignment.Clue));
            Assert.That(clue.ClueId.Value, Is.EqualTo(
                "CLUE_" + assignment.RegionId.Value + "_" + OptionalRegionTokenCodec.ToToken(assignment.AccessRule)));
            Assert.That(clue.AttachmentOrder, Is.EqualTo(assignment.AttachmentOrder));
            Assert.That(clue.IsPerceptibleFromMandatory, Is.True);
            Assert.That(clue.RequiresRewardPreview,
                Is.EqualTo(assignment.AccessRule == OptionalRegionAccessRule.Explosive));
            Assert.That(assignment.RequiresPartialRewardPreview, Is.EqualTo(clue.RequiresRewardPreview));
            Assert.That(baseline.Clues.Select(value => value.ClueId).Distinct().Count(), Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.PerceptibleClueCount, Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.RewardPreviewReservationCount, Is.EqualTo(2));
        }

        [TestCaseSource(nameof(DigestCases))]
        public void CostsDigestsCultureAndServiceReuseAreDeterministic(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = caseId % 3 == 0
                    ? CultureInfo.GetCultureInfo("tr-TR")
                    : caseId % 3 == 1
                        ? CultureInfo.GetCultureInfo("de-DE")
                        : CultureInfo.GetCultureInfo("en-US");
                var assigner = new OptionalAccessRuleAssigner();
                var first = assigner.Assign(type0, ApprovedSettings());
                var second = assigner.Assign(type0, ApprovedSettings());
                Assert.That(first.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(second.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(first.SourceType0AssignmentDigest, Is.EqualTo(type0.CanonicalDigest));
                Assert.That(first.SourceGrowthDigest, Is.EqualTo(type0.SourceGrowthDigest));
                Assert.That(first.CanonicalDigest, Has.Length.EqualTo(64));
                Assert.That(first.CanonicalDigest.All(IsLowerHex), Is.True);
                foreach (var assignment in first.Assignments)
                {
                    var depth = type0.SourceSnapshot.Regions.Single(
                        value => value.RegionId == assignment.RegionId).MaxDepth.Value;
                    if (assignment.AccessRule == OptionalRegionAccessRule.Tool)
                        Assert.That(assignment.ToolCostTier, Is.EqualTo(settings.ToolCostTierByDepth[depth - 1]));
                    if (assignment.AccessRule == OptionalRegionAccessRule.Explosive)
                        Assert.That(assignment.ExplosiveFuelCost, Is.EqualTo(settings.ExplosiveFuelCostByDepth[depth - 1]));
                    if (assignment.AccessRule == OptionalRegionAccessRule.Hidden)
                        Assert.That(assignment.HiddenClueDifficulty, Is.EqualTo(settings.HiddenClueDifficultyByDepth[depth - 1]));
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCaseSource(nameof(AtomicCases))]
        public void InvalidInputSettingsAndBoundaryFailuresAreAtomic(int caseId)
        {
            OptionalAccessAssignmentResult result;
            switch (caseId % 6)
            {
                case 0:
                    result = new OptionalAccessRuleAssigner().Assign(null, settings);
                    Assert.That(result.Status, Is.EqualTo(OptionalAccessAssignmentStatus.InvalidInput));
                    break;
                case 1:
                    result = new OptionalAccessRuleAssigner().Assign(type0, null);
                    Assert.That(result.Status, Is.EqualTo(OptionalAccessAssignmentStatus.InvalidSettings));
                    break;
                case 2:
                    result = new OptionalAccessRuleAssigner().Assign(CloneType0(baseOpen: 1), settings);
                    Assert.That(result.Status, Is.EqualTo(OptionalAccessAssignmentStatus.InvalidBoundary));
                    break;
                case 3:
                    result = new OptionalAccessRuleAssigner().Assign(CloneType0(closedDelta: -1), settings);
                    Assert.That(result.Status, Is.EqualTo(OptionalAccessAssignmentStatus.InvalidBoundary));
                    break;
                case 4:
                    result = new OptionalAccessRuleAssigner().Assign(CloneType0(sourceRegionDelta: 1), settings);
                    Assert.That(result.Status, Is.EqualTo(OptionalAccessAssignmentStatus.InvalidInput));
                    break;
                default:
                    result = new OptionalAccessRuleAssigner().Assign(CloneType0(canonicalDigest: new string('A', 64)), settings);
                    Assert.That(result.Status, Is.EqualTo(OptionalAccessAssignmentStatus.InvalidInput));
                    break;
            }

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Assignments, Is.Empty);
            Assert.That(result.Clues, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.RngDrawCount, Is.Zero);
            Assert.That(result.Diagnostics.SourceMutationCount, Is.Zero);
            Assert.That(result.Errors, Is.Ordered.Using<OptionalAccessAssignmentError>(
                Comparer<OptionalAccessAssignmentError>.Create(CompareErrors)));
        }

        [TestCaseSource(nameof(IntegrityCases))]
        public void RngMutationType4AndPhaseBoundaryRemainFrozen(int caseId)
        {
            switch (caseId % 6)
            {
                case 0:
                    Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
                    Assert.That(baseline.RngDrawCount, Is.Zero);
                    Assert.That(baseline.Diagnostics.SourceMutationCount, Is.Zero);
                    break;
                case 1:
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<OptionalAccessAssignment>)baseline.Assignments).Add(baseline.Assignments[0]));
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<OptionalAccessClue>)baseline.Clues).Add(baseline.Clues[0]));
                    break;
                case 2:
                {
                    var bits = caseId / 6;
                    var left = (bits & 1) != 0;
                    var right = (bits & 2) != 0;
                    Assert.That(graph.MaskFamily.TryResolve(left, right, true, true, out var mask), Is.True);
                    Assert.That(mask.OpenUp && mask.OpenDown, Is.True);
                    Assert.That(mask.OpenLeft, Is.EqualTo(left));
                    Assert.That(mask.OpenRight, Is.EqualTo(right));
                    break;
                }
                case 3:
                    foreach (var name in new[]
                    {
                        "OptionalAccessClueId", "OptionalAccessAssignmentEnums", "OptionalAccessClue",
                        "OptionalAccessAssignmentSettings", "OptionalAccessAssignment",
                        "OptionalAccessAssignmentDiagnostics", "OptionalAccessAssignmentResult",
                        "OptionalAccessRuleAssigner"
                    })
                        Assert.That(typeof(OptionalAccessRuleAssigner).Assembly.GetType(
                            "StarNight.Map.WorldGeneration.Generation." + name, false), Is.Not.Null);
                    break;
                case 4:
                    foreach (var name in new[]
                    {
                        "OptionalReturnConnection",
                        "OptionalRegionOverlayRenderer", "OptionalRegionValidationOverlayWindow", "OptionalRegionOverlay",
                        "GeneratedOptionalRegionCsvWriter"
                    })
                        Assert.That(typeof(OptionalAccessRuleAssigner).Assembly.GetType(
                            "StarNight.Map.WorldGeneration.Generation." + name, false), Is.Null);
                    break;
                default:
                    Assert.That(typeof(OptionalAccessRuleAssigner).Assembly.GetReferencedAssemblies()
                        .Any(value => value.Name == "UnityEditor"), Is.False);
                    Assert.That(type0.SourceSnapshot.Regions.All(value =>
                        value.AccessRule == OptionalRegionAccessRule.Basic &&
                        value.RewardTier == OptionalRewardTier.None &&
                        value.ReturnPolicy == OptionalReturnPolicy.BacktrackToAttachment), Is.True);
                    Assert.That(type0.Assignments.All(value => !value.OpenMask.HasHorizontalThrough), Is.True);
                    break;
            }
        }

        [Test]
        public void ApprovedFixturePublishesCanonicalSummary()
        {
            foreach (var assignment in baseline.Assignments)
            {
                TestContext.WriteLine(
                    "MAP06_05_ASSIGNMENT region={0} ordinal={1} attachment={2} mandatory={3} entry={4} dir={5},{6} rule={7} requirement={8} traversal={9} clue={10}/{11} tool={12} fuel={13} hidden={14} preview={15}",
                    assignment.RegionId.Value, assignment.RegionOrdinal, assignment.AttachmentOrder,
                    assignment.MandatoryRouteSectorIndex, assignment.EntrySectorIndex,
                    assignment.EntrySideFromMandatoryDx, assignment.EntrySideFromMandatoryDy,
                    OptionalRegionTokenCodec.ToToken(assignment.AccessRule),
                    OptionalAccessAssignmentEnums.ToToken(assignment.Requirement),
                    OptionalAccessAssignmentEnums.ToToken(assignment.TraversalKind),
                    assignment.Clue.ClueId.Value,
                    OptionalAccessAssignmentEnums.ToToken(assignment.Clue.Kind),
                    assignment.ToolCostTier, assignment.ExplosiveFuelCost,
                    assignment.HiddenClueDifficulty, assignment.RequiresPartialRewardPreview ? 1 : 0);
            }
            TestContext.WriteLine(
                "MAP06_05_SUMMARY source={0}/{1}/{2} rules={3}/{4}/{5}/{6}/{7} tools={8}/{9}/{10} hidden={11}/{12}/{13} clues={14} previews={15} baseOpen={16} rng={17} mutation={18} type0={19} growth={20} digest={21}",
                baseline.Diagnostics.SourceRegionCount, baseline.Diagnostics.SourceCellCount,
                baseline.Diagnostics.SourceType0AssignmentCount, baseline.Diagnostics.BasicCount,
                baseline.Diagnostics.ToolCount, baseline.Diagnostics.EnvironmentCount,
                baseline.Diagnostics.ExplosiveCount, baseline.Diagnostics.HiddenCount,
                baseline.Diagnostics.PickaxeCount, baseline.Diagnostics.ShovelCount,
                baseline.Diagnostics.RopeCount, baseline.Diagnostics.HiddenCrackCount,
                baseline.Diagnostics.HiddenLightCount, baseline.Diagnostics.HiddenSoundCount,
                baseline.Diagnostics.PerceptibleClueCount,
                baseline.Diagnostics.RewardPreviewReservationCount,
                baseline.Diagnostics.AttachmentBoundaryBaseOpenCount,
                baseline.RngDrawCount, baseline.Diagnostics.SourceMutationCount,
                baseline.SourceType0AssignmentDigest, baseline.SourceGrowthDigest,
                baseline.CanonicalDigest);

            Assert.That(type0.CanonicalDigest,
                Is.EqualTo("a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525"));
            Assert.That(type0.SourceGrowthDigest,
                Is.EqualTo("1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa"));
            Assert.That(baseline.Diagnostics.SourceRegionCount, Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.SourceCellCount, Is.EqualTo(39));
            Assert.That(baseline.Diagnostics.SourceType0AssignmentCount, Is.EqualTo(39));
            Assert.That(baseline.Diagnostics.AssignmentCount, Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.ClueCount, Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.PerceptibleClueCount, Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.RewardPreviewReservationCount, Is.EqualTo(2));
            Assert.That(baseline.Diagnostics.AttachmentBoundaryBaseOpenCount, Is.Zero);
            Assert.That(baseline.CanonicalDigest, Has.Length.EqualTo(64));
        }

        private static OptionalAccessAssignmentSettings ApprovedSettings()
        {
            return new OptionalAccessAssignmentSettings(
                new[]
                {
                    OptionalRegionAccessRule.Basic, OptionalRegionAccessRule.Tool,
                    OptionalRegionAccessRule.Environment, OptionalRegionAccessRule.Explosive,
                    OptionalRegionAccessRule.Hidden
                },
                new[]
                {
                    OptionalAccessRequirement.Pickaxe, OptionalAccessRequirement.Shovel,
                    OptionalAccessRequirement.Rope
                },
                new[]
                {
                    OptionalAccessClueKind.HiddenCrack, OptionalAccessClueKind.HiddenLight,
                    OptionalAccessClueKind.HiddenSound
                },
                new[] { 1, 2, 3, 4 },
                new[] { 10, 20, 30, 40 },
                new[] { 1, 2, 3, 4 });
        }

        private static TestDelegate InvalidSettings(int caseId)
        {
            return () =>
            {
                var access = new[] { OptionalRegionAccessRule.Basic };
                var tools = new[] { OptionalAccessRequirement.Pickaxe };
                var hidden = new[] { OptionalAccessClueKind.HiddenCrack };
                var toolCost = new[] { 1, 2, 3, 4 };
                var fuel = new[] { 10, 20, 30, 40 };
                var difficulty = new[] { 1, 2, 3, 4 };
                switch (caseId % 12)
                {
                    case 0: access = null; break;
                    case 1: access = Array.Empty<OptionalRegionAccessRule>(); break;
                    case 2: access = new[] { (OptionalRegionAccessRule)99 }; break;
                    case 3: tools = null; break;
                    case 4: tools = new[] { OptionalAccessRequirement.Explosive }; break;
                    case 5: hidden = null; break;
                    case 6: hidden = new[] { OptionalAccessClueKind.ToolSurface }; break;
                    case 7: toolCost = new[] { 1, 2, 3 }; break;
                    case 8: toolCost = new[] { 0, 2, 3, 4 }; break;
                    case 9: fuel = new[] { 10, 20, 30, 101 }; break;
                    case 10: difficulty = new[] { 1, 2, 3, 5 }; break;
                    default: tools = Array.Empty<OptionalAccessRequirement>(); break;
                }
                new OptionalAccessAssignmentSettings(access, tools, hidden, toolCost, fuel, difficulty);
            };
        }

        private Type0RouteMaskAssignmentResult CloneType0(
            int baseOpen = 0,
            int closedDelta = 0,
            int sourceRegionDelta = 0,
            string canonicalDigest = null)
        {
            var source = type0.Diagnostics;
            var diagnostics = new Type0RouteMaskAssignmentDiagnostics(
                source.SourceRouteMaskDefinitionCount,
                source.RegisteredType0MaskCount,
                source.IgnoredNonType0DefinitionCount,
                source.SourceRegionCount + sourceRegionDelta,
                source.SourceCellCount,
                source.AssignmentCount,
                source.InternalUndirectedEdgeCount,
                source.AttachmentBoundaryClosedCount + closedDelta,
                baseOpen,
                source.ClosedCrossRegionAdjacencyCount,
                source.HorizontalThroughCount,
                source.UnsupportedRequiredMaskCount,
                source.RngDrawCount,
                source.SourceMutationCount);
            var constructor = typeof(Type0RouteMaskAssignmentResult)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single();
            return (Type0RouteMaskAssignmentResult)constructor.Invoke(new object[]
            {
                Type0RouteMaskAssignmentStatus.Completed,
                type0.SourceSnapshot,
                type0.RegisteredMasks,
                type0.Assignments,
                diagnostics,
                Array.Empty<Type0RouteMaskAssignmentError>(),
                type0.SourceGrowthDigest,
                type0.SourceRouteMaskCatalogDigest,
                canonicalDigest ?? type0.CanonicalDigest
            });
        }

        private string SourceSignature()
        {
            return type0.CanonicalDigest + "|" + type0.SourceGrowthDigest + "|" +
                   string.Join(",", type0.Assignments.Select(value =>
                       value.RegionId.Value + ":" + value.SectorIndex + ":" + value.MaskId.Value)) + "|" +
                   string.Join(",", type0.SourceSnapshot.Regions.Select(value =>
                       value.RegionId.Value + ":" + value.AccessRule + ":" + value.RewardTier + ":" + value.ReturnPolicy)) + "|" +
                   graph.NodeCount + "/" + graph.DirectedEdgeCount + "/" + graph.UndirectedEdgeCount + "/" + graph.CellCount;
        }

        private static void AssertMatrix(
            OptionalAccessAssignment assignment,
            OptionalAccessRequirement requirement,
            OptionalAccessTraversalKind traversal,
            OptionalAccessClueKind clue,
            bool preview,
            int tool,
            int fuel,
            int hidden)
        {
            Assert.That(assignment.Requirement, Is.EqualTo(requirement));
            Assert.That(assignment.TraversalKind, Is.EqualTo(traversal));
            Assert.That(assignment.Clue.Kind, Is.EqualTo(clue));
            Assert.That(assignment.RequiresPartialRewardPreview, Is.EqualTo(preview));
            Assert.That(assignment.ToolCostTier, Is.EqualTo(tool));
            Assert.That(assignment.ExplosiveFuelCost, Is.EqualTo(fuel));
            Assert.That(assignment.HiddenClueDifficulty, Is.EqualTo(hidden));
        }

        private static int CompareErrors(OptionalAccessAssignmentError left, OptionalAccessAssignmentError right)
        {
            var code = string.Compare(left.Code, right.Code, StringComparison.Ordinal);
            if (code != 0) return code;
            var region = left.RegionId.CompareTo(right.RegionId);
            if (region != 0) return region;
            var attachment = left.AttachmentOrder.CompareTo(right.AttachmentOrder);
            if (attachment != 0) return attachment;
            var clue = left.ClueId.CompareTo(right.ClueId);
            return clue != 0 ? clue : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static bool IsOpen(Type0RouteOpenMask mask, int dx, int dy)
        {
            if (dx == -1 && dy == 0) return mask.OpenLeft;
            if (dx == 1 && dy == 0) return mask.OpenRight;
            if (dx == 0 && dy == 1) return mask.OpenUp;
            return mask.OpenDown;
        }

        private static bool IsLowerHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f');
        }

        private static string FormatErrors(OptionalAccessAssignmentResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.Code + ": " + value.Message));
        }

        private static T GetField<T>(object target, Type type, string name)
        {
            return (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
