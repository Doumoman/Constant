using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRewardTierCalculator
    {
        public OptionalRewardTierResult Calculate(
            Type0RouteMaskAssignmentResult type0Assignments,
            OptionalAccessAssignmentResult accessAssignments,
            OptionalRewardTierSettings settings)
        {
            if (type0Assignments == null)
            {
                return Failure(
                    OptionalRewardTierCalculationStatus.InvalidInput,
                    null,
                    accessAssignments,
                    new[] { Error(OptionalRewardTierCalculationErrorCode.NullInput, "type0Assignments", "Type0 assignment result cannot be null.") });
            }
            if (accessAssignments == null)
            {
                return Failure(
                    OptionalRewardTierCalculationStatus.InvalidInput,
                    type0Assignments,
                    null,
                    new[] { Error(OptionalRewardTierCalculationErrorCode.NullInput, "accessAssignments", "Access assignment result cannot be null.") });
            }
            if (settings == null)
            {
                return Failure(
                    OptionalRewardTierCalculationStatus.InvalidSettings,
                    type0Assignments,
                    accessAssignments,
                    new[] { Error(OptionalRewardTierCalculationErrorCode.NullInput, "settings", "Reward tier settings cannot be null.") });
            }
            if (!SettingsAreValid(settings))
            {
                return Failure(
                    OptionalRewardTierCalculationStatus.InvalidSettings,
                    type0Assignments,
                    accessAssignments,
                    new[] { Error(OptionalRewardTierCalculationErrorCode.InvalidAccounting, "settings", "Reward tier settings do not satisfy the immutable contract.") });
            }

            var errors = ValidateSource(type0Assignments, accessAssignments);
            if (errors.Count != 0)
            {
                return Failure(
                    OptionalRewardTierCalculationStatus.InvalidSource,
                    type0Assignments,
                    accessAssignments,
                    errors);
            }

            var regions = new List<OptionalRegion>(type0Assignments.SourceSnapshot.Regions);
            regions.Sort((left, right) => left.RegionId.CompareTo(right.RegionId));
            var accessByRegion = new Dictionary<OptionalRegionId, OptionalAccessAssignment>();
            foreach (var assignment in accessAssignments.Assignments)
                accessByRegion.Add(assignment.RegionId, assignment);

            var staged = new List<OptionalRewardTierAssignment>(regions.Count);
            var low = 0;
            var medium = 0;
            var high = 0;
            var unique = 0;
            var depthTotal = 0;
            var toolTotal = 0;
            var explosiveTotal = 0;
            var hiddenTotal = 0;
            var previewCount = 0;
            var minimum = int.MaxValue;
            var maximum = 0;

            try
            {
                for (var ordinal = 0; ordinal < regions.Count; ordinal++)
                {
                    var region = regions[ordinal];
                    var access = accessByRegion[region.RegionId];
                    var rewardScore = CheckedRewardScore(
                        region.MaxDepth.Value,
                        settings.DepthWeight,
                        access.ToolCostTier,
                        access.ExplosiveFuelCost,
                        settings.ExplosiveFuelDivisor,
                        access.HiddenClueDifficulty,
                        out var depthScore,
                        out var toolScore,
                        out var explosiveScore,
                        out var hiddenScore);
                    var tier = SelectTier(rewardScore, settings.TierMinimumScores);
                    var assignment = new OptionalRewardTierAssignment(
                        region.RegionId,
                        ordinal,
                        access.AttachmentOrder,
                        access.Clue.ClueId,
                        access.AccessRule,
                        region.MaxDepth.Value,
                        access.ToolCostTier,
                        access.ExplosiveFuelCost,
                        access.HiddenClueDifficulty,
                        depthScore,
                        toolScore,
                        explosiveScore,
                        hiddenScore,
                        rewardScore,
                        tier,
                        access.RequiresPartialRewardPreview);
                    staged.Add(assignment);

                    checked
                    {
                        depthTotal += depthScore;
                        toolTotal += toolScore;
                        explosiveTotal += explosiveScore;
                        hiddenTotal += hiddenScore;
                    }
                    if (rewardScore < minimum) minimum = rewardScore;
                    if (rewardScore > maximum) maximum = rewardScore;
                    if (access.RequiresPartialRewardPreview) previewCount++;
                    switch (tier)
                    {
                        case OptionalRewardTier.Low: low++; break;
                        case OptionalRewardTier.Medium: medium++; break;
                        case OptionalRewardTier.High: high++; break;
                        case OptionalRewardTier.Unique: unique++; break;
                        default: throw new InvalidOperationException("Successful score calculation cannot publish the None tier.");
                    }
                }
            }
            catch (OverflowException)
            {
                return Failure(
                    OptionalRewardTierCalculationStatus.ArithmeticOverflow,
                    type0Assignments,
                    accessAssignments,
                    new[] { Error(OptionalRewardTierCalculationErrorCode.ArithmeticOverflow, "rewardScore", "Checked reward score arithmetic overflowed.") });
            }

            var diagnostics = new OptionalRewardTierDiagnostics(
                regions.Count,
                type0Assignments.Assignments.Count,
                accessAssignments.Assignments.Count,
                staged.Count,
                low,
                medium,
                high,
                unique,
                depthTotal,
                toolTotal,
                explosiveTotal,
                hiddenTotal,
                staged.Count == 0 ? 0 : minimum,
                staged.Count == 0 ? 0 : maximum,
                previewCount,
                0,
                0,
                0);
            var digest = ComputeDigest(
                type0Assignments.CanonicalDigest,
                accessAssignments.CanonicalDigest,
                type0Assignments.SourceGrowthDigest,
                settings,
                staged,
                diagnostics);
            return new OptionalRewardTierResult(
                OptionalRewardTierCalculationStatus.Completed,
                staged,
                diagnostics,
                Array.Empty<OptionalRewardTierCalculationError>(),
                type0Assignments.CanonicalDigest,
                accessAssignments.CanonicalDigest,
                type0Assignments.SourceGrowthDigest,
                digest);
        }

        private static List<OptionalRewardTierCalculationError> ValidateSource(
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access)
        {
            var errors = new List<OptionalRewardTierCalculationError>();
            if (!type0.IsSuccess || type0.Status != Type0RouteMaskAssignmentStatus.Completed)
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.InvalidStatus, "type0.status", "Type0 assignment result must be completed."));
            if (!access.IsSuccess || access.Status != OptionalAccessAssignmentStatus.Completed)
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.InvalidStatus, "access.status", "Access assignment result must be completed."));
            if (type0.SourceSnapshot == null)
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.InvalidAccounting, "type0.snapshot", "Type0 source snapshot is required."));
            if (type0.Diagnostics == null || access.Diagnostics == null)
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.InvalidAccounting, "diagnostics", "Source diagnostics are required."));

            if (!IsLowerHexDigest(type0.CanonicalDigest) ||
                !IsLowerHexDigest(type0.SourceGrowthDigest) ||
                !IsLowerHexDigest(type0.SourceRouteMaskCatalogDigest) ||
                !IsLowerHexDigest(access.SourceType0AssignmentDigest) ||
                !IsLowerHexDigest(access.SourceGrowthDigest) ||
                !IsLowerHexDigest(access.CanonicalDigest))
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.InvalidDigest, "sourceDigests", "All source-chain digests must be lowercase SHA-256 values."));
            if (!string.Equals(access.SourceType0AssignmentDigest, type0.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(access.SourceGrowthDigest, type0.SourceGrowthDigest, StringComparison.Ordinal))
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.SourceMismatch, "sourceChain", "Access and Type0 source-chain digests must match exactly."));

            if (type0.SourceSnapshot == null || type0.Diagnostics == null || access.Diagnostics == null)
                return errors;

            var snapshot = type0.SourceSnapshot;
            if (type0.Diagnostics.SourceRegionCount != snapshot.Regions.Count ||
                type0.Diagnostics.SourceCellCount != snapshot.Cells.Count ||
                type0.Diagnostics.AssignmentCount != type0.Assignments.Count ||
                type0.Assignments.Count != snapshot.Cells.Count ||
                access.Diagnostics.SourceRegionCount != snapshot.Regions.Count ||
                access.Diagnostics.SourceCellCount != snapshot.Cells.Count ||
                access.Diagnostics.SourceType0AssignmentCount != type0.Assignments.Count ||
                access.Diagnostics.AssignmentCount != access.Assignments.Count ||
                access.Diagnostics.ClueCount != access.Clues.Count ||
                access.Assignments.Count != access.Clues.Count ||
                access.Diagnostics.PerceptibleClueCount != access.Clues.Count)
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.InvalidAccounting, "sourceCounts", "Snapshot, assignment, clue, and diagnostics counts must agree."));
            if (type0.Diagnostics.AttachmentBoundaryClosedCount != snapshot.Regions.Count ||
                type0.Diagnostics.MandatoryBoundaryBaseOpenCount != 0 ||
                access.Diagnostics.AttachmentBoundaryBaseOpenCount != 0)
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.OpenAttachmentBoundary, "attachmentBoundary", "Every attachment boundary must remain base-closed."));
            if (type0.RngDrawCount != 0 || access.RngDrawCount != 0 ||
                type0.Diagnostics.SourceMutationCount != 0 || access.Diagnostics.SourceMutationCount != 0)
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.InvalidAccounting, "rngMutation", "Source RNG and mutation counts must remain zero."));

            var regionValues = new List<OptionalRegion>(snapshot.Regions);
            regionValues.Sort((left, right) => left.RegionId.CompareTo(right.RegionId));
            var regions = new Dictionary<OptionalRegionId, OptionalRegion>();
            foreach (var region in regionValues)
            {
                if (region == null || !region.RegionId.IsValid)
                {
                    errors.Add(Error(OptionalRewardTierCalculationErrorCode.MissingRegion, "snapshot.regions", "Source regions must be non-null and valid."));
                    continue;
                }
                if (!regions.TryAdd(region.RegionId, region))
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.DuplicateRegion, region.RegionId, region.Attachment.AttachmentOrder, "snapshot.regions", "Source region IDs must be unique."));
                if (!region.MaxDepth.IsValid)
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.InvalidDepth, region.RegionId, region.Attachment.AttachmentOrder, "region.maxDepth", "Region depth must remain in 1..4."));
            }

            var type0BySector = new Dictionary<int, Type0RouteMaskAssignment>();
            foreach (var assignment in type0.Assignments)
            {
                if (assignment == null || !type0BySector.TryAdd(assignment == null ? -1 : assignment.SectorIndex, assignment))
                {
                    errors.Add(Error(OptionalRewardTierCalculationErrorCode.InvalidAccounting, "type0.assignments", "Type0 assignments must be non-null and unique by sector."));
                    continue;
                }
                if (!regions.ContainsKey(assignment.RegionId) || assignment.OpenMask.HasHorizontalThrough)
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.InvalidMatrix, assignment.RegionId, -1, "type0.assignment", "Type0 assignment must preserve region identity and forbid L+R through masks."));
            }
            foreach (var cell in snapshot.Cells)
            {
                if (cell == null || !type0BySector.TryGetValue(cell == null ? -1 : cell.SectorIndex, out var assignment) ||
                    assignment.RegionId != cell.RegionId || assignment.Sector != cell.Sector ||
                    assignment.Depth != cell.Depth || assignment.IsAttachmentCell != cell.IsAttachmentCell)
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.InvalidAccounting, cell == null ? default(OptionalRegionId) : cell.RegionId, -1, "type0.cellJoin", "Every source cell requires one identity-preserving Type0 assignment."));
            }

            var accessByRegion = new Dictionary<OptionalRegionId, OptionalAccessAssignment>();
            foreach (var assignment in access.Assignments)
            {
                if (assignment == null)
                {
                    errors.Add(Error(OptionalRewardTierCalculationErrorCode.MissingRegion, "access.assignments", "Access assignments cannot contain null."));
                    continue;
                }
                if (!accessByRegion.TryAdd(assignment.RegionId, assignment))
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.DuplicateRegion, assignment.RegionId, assignment.AttachmentOrder, "access.assignments", "Each region requires exactly one access assignment."));
            }

            var clueIds = new HashSet<OptionalAccessClueId>();
            var clueRegions = new HashSet<OptionalRegionId>();
            foreach (var clue in access.Clues)
            {
                if (clue == null || !clueIds.Add(clue == null ? default(OptionalAccessClueId) : clue.ClueId) ||
                    !clueRegions.Add(clue == null ? default(OptionalRegionId) : clue.RegionId) ||
                    !clue.IsPerceptibleFromMandatory)
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.InvalidMatrix, clue == null ? default(OptionalRegionId) : clue.RegionId, clue == null ? -1 : clue.AttachmentOrder, "access.clues", "Clues must be unique and perceptible from mandatory."));
            }

            for (var ordinal = 0; ordinal < regionValues.Count; ordinal++)
            {
                var region = regionValues[ordinal];
                if (region == null) continue;
                if (!accessByRegion.TryGetValue(region.RegionId, out var assignment))
                {
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.MissingRegion, region.RegionId, region.Attachment.AttachmentOrder, "access.regionJoin", "Every source region requires one access assignment."));
                    continue;
                }
                if (assignment.RegionOrdinal != ordinal ||
                    assignment.AttachmentOrder != region.Attachment.AttachmentOrder ||
                    assignment.MandatoryRouteSectorIndex != region.Attachment.MandatoryRouteSectorIndex ||
                    assignment.MandatoryRouteSector != region.Attachment.MandatoryRouteSector ||
                    assignment.EntrySectorIndex != region.Attachment.EntrySectorIndex ||
                    assignment.EntrySector != region.Attachment.EntrySector ||
                    assignment.EntrySideFromMandatoryDx != region.Attachment.EntrySideFromMandatoryDx ||
                    assignment.EntrySideFromMandatoryDy != region.Attachment.EntrySideFromMandatoryDy ||
                    assignment.Clue == null || assignment.Clue.RegionId != region.RegionId ||
                    assignment.Clue.AttachmentOrder != region.Attachment.AttachmentOrder)
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.SourceMismatch, region.RegionId, region.Attachment.AttachmentOrder, "access.attachmentIdentity", "Access assignment must preserve region and attachment identity."));
                if (!AccessMatrixIsValid(assignment))
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.InvalidMatrix, region.RegionId, region.Attachment.AttachmentOrder, "access.matrix", "Access rule, clue, preview, and cost fields do not match the frozen matrix."));

                if (!type0BySector.TryGetValue(region.Attachment.EntrySectorIndex, out var entry) ||
                    entry.RegionId != region.RegionId ||
                    IsOpen(entry.OpenMask, -region.Attachment.EntrySideFromMandatoryDx, -region.Attachment.EntrySideFromMandatoryDy))
                    errors.Add(RegionError(OptionalRewardTierCalculationErrorCode.OpenAttachmentBoundary, region.RegionId, region.Attachment.AttachmentOrder, "attachment.baseSide", "Attachment-to-mandatory Type0 base side must remain closed."));
            }

            if (accessByRegion.Count != regions.Count || clueRegions.Count != regions.Count)
                errors.Add(Error(OptionalRewardTierCalculationErrorCode.InvalidAccounting, "regionJoin", "Region, access assignment, and clue identities must be one-to-one."));
            return errors;
        }

        private static bool AccessMatrixIsValid(OptionalAccessAssignment value)
        {
            if (value.Clue == null || !value.Clue.IsPerceptibleFromMandatory ||
                value.Clue.RequiresRewardPreview != value.RequiresPartialRewardPreview)
                return false;
            switch (value.AccessRule)
            {
                case OptionalRegionAccessRule.Basic:
                    return value.Requirement == OptionalAccessRequirement.None &&
                           value.TraversalKind == OptionalAccessTraversalKind.OptionalBreak &&
                           value.Clue.Kind == OptionalAccessClueKind.BasicOpening &&
                           value.ToolCostTier == 0 && value.ExplosiveFuelCost == 0 &&
                           value.HiddenClueDifficulty == 0 && !value.RequiresPartialRewardPreview;
                case OptionalRegionAccessRule.Tool:
                    return (value.Requirement == OptionalAccessRequirement.Pickaxe ||
                            value.Requirement == OptionalAccessRequirement.Shovel ||
                            value.Requirement == OptionalAccessRequirement.Rope) &&
                           value.TraversalKind == OptionalAccessTraversalKind.OptionalBreak &&
                           value.Clue.Kind == OptionalAccessClueKind.ToolSurface &&
                           value.ToolCostTier >= 1 && value.ToolCostTier <= 4 &&
                           value.ExplosiveFuelCost == 0 && value.HiddenClueDifficulty == 0 &&
                           !value.RequiresPartialRewardPreview;
                case OptionalRegionAccessRule.Environment:
                    return value.Requirement == OptionalAccessRequirement.Environment &&
                           value.TraversalKind == OptionalAccessTraversalKind.OptionalBreak &&
                           value.Clue.Kind == OptionalAccessClueKind.EnvironmentDevice &&
                           value.ToolCostTier == 0 && value.ExplosiveFuelCost == 0 &&
                           value.HiddenClueDifficulty == 0 && !value.RequiresPartialRewardPreview;
                case OptionalRegionAccessRule.Explosive:
                    return value.Requirement == OptionalAccessRequirement.Explosive &&
                           value.TraversalKind == OptionalAccessTraversalKind.OptionalBreak &&
                           value.Clue.Kind == OptionalAccessClueKind.ExplosiveRewardPreview &&
                           value.ToolCostTier == 0 && value.ExplosiveFuelCost >= 1 &&
                           value.ExplosiveFuelCost <= 100 && value.HiddenClueDifficulty == 0 &&
                           value.RequiresPartialRewardPreview;
                case OptionalRegionAccessRule.Hidden:
                    return value.Requirement == OptionalAccessRequirement.None &&
                           value.TraversalKind == OptionalAccessTraversalKind.Hidden &&
                           (value.Clue.Kind == OptionalAccessClueKind.HiddenCrack ||
                            value.Clue.Kind == OptionalAccessClueKind.HiddenLight ||
                            value.Clue.Kind == OptionalAccessClueKind.HiddenSound) &&
                           value.ToolCostTier == 0 && value.ExplosiveFuelCost == 0 &&
                           value.HiddenClueDifficulty >= 1 && value.HiddenClueDifficulty <= 4 &&
                           !value.RequiresPartialRewardPreview;
                default:
                    return false;
            }
        }

        private static bool SettingsAreValid(OptionalRewardTierSettings settings)
        {
            if (settings.DepthWeight < 1 || settings.DepthWeight > 100 ||
                settings.ExplosiveFuelDivisor < 1 || settings.ExplosiveFuelDivisor > 100 ||
                settings.TierMinimumScores == null || settings.TierMinimumScores.Count != 4 ||
                settings.TierMinimumScores[0] != 0)
                return false;
            for (var index = 0; index < settings.TierMinimumScores.Count; index++)
            {
                var value = settings.TierMinimumScores[index];
                if (value < 0 || value > 1000000 ||
                    (index > 0 && value <= settings.TierMinimumScores[index - 1]))
                    return false;
            }
            return true;
        }

        private static int CheckedRewardScore(
            int maxDepth,
            int depthWeight,
            int toolCostTier,
            int explosiveFuelCost,
            int explosiveFuelDivisor,
            int hiddenClueDifficulty,
            out int depthScore,
            out int toolCostScore,
            out int explosiveFuelScore,
            out int hiddenClueScore)
        {
            checked
            {
                depthScore = maxDepth * depthWeight;
                toolCostScore = toolCostTier;
                explosiveFuelScore = explosiveFuelCost / explosiveFuelDivisor;
                hiddenClueScore = hiddenClueDifficulty;
                return depthScore + toolCostScore + explosiveFuelScore + hiddenClueScore;
            }
        }

        private static OptionalRewardTier SelectTier(int score, IReadOnlyList<int> minimums)
        {
            if (score >= minimums[3]) return OptionalRewardTier.Unique;
            if (score >= minimums[2]) return OptionalRewardTier.High;
            if (score >= minimums[1]) return OptionalRewardTier.Medium;
            return OptionalRewardTier.Low;
        }

        private static OptionalRewardTierResult Failure(
            OptionalRewardTierCalculationStatus status,
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access,
            IEnumerable<OptionalRewardTierCalculationError> errors)
        {
            var regions = type0 == null || type0.SourceSnapshot == null ? 0 : type0.SourceSnapshot.Regions.Count;
            var type0Count = type0 == null ? 0 : type0.Assignments.Count;
            var accessCount = access == null ? 0 : access.Assignments.Count;
            var diagnostics = new OptionalRewardTierDiagnostics(
                regions, type0Count, accessCount, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            return new OptionalRewardTierResult(
                status,
                Array.Empty<OptionalRewardTierAssignment>(),
                diagnostics,
                errors,
                type0 == null ? string.Empty : type0.CanonicalDigest,
                access == null ? string.Empty : access.CanonicalDigest,
                type0 == null ? string.Empty : type0.SourceGrowthDigest,
                string.Empty);
        }

        private static string ComputeDigest(
            string type0Digest,
            string accessDigest,
            string growthDigest,
            OptionalRewardTierSettings settings,
            IReadOnlyList<OptionalRewardTierAssignment> assignments,
            OptionalRewardTierDiagnostics diagnostics)
        {
            var text = new StringBuilder();
            text.Append("S|").Append(type0Digest).Append('|').Append(accessDigest).Append('|').Append(growthDigest).Append('\n');
            text.Append("P|").Append(Invariant(settings.DepthWeight)).Append('|')
                .Append(Invariant(settings.ExplosiveFuelDivisor));
            foreach (var minimum in settings.TierMinimumScores)
                text.Append('|').Append(Invariant(minimum));
            text.Append('\n');
            foreach (var assignment in assignments)
            {
                text.Append("A|").Append(assignment.RegionId.Value).Append('|')
                    .Append(Invariant(assignment.RegionOrdinal)).Append('|')
                    .Append(Invariant(assignment.AttachmentOrder)).Append('|')
                    .Append(assignment.ClueId.Value).Append('|')
                    .Append(OptionalRegionTokenCodec.ToToken(assignment.AccessRule)).Append('|')
                    .Append(Invariant(assignment.MaxDepth)).Append('|')
                    .Append(Invariant(assignment.ToolCostTier)).Append('|')
                    .Append(Invariant(assignment.ExplosiveFuelCost)).Append('|')
                    .Append(Invariant(assignment.HiddenClueDifficulty)).Append('|')
                    .Append(Invariant(assignment.DepthScore)).Append('|')
                    .Append(Invariant(assignment.ToolCostScore)).Append('|')
                    .Append(Invariant(assignment.ExplosiveFuelScore)).Append('|')
                    .Append(Invariant(assignment.HiddenClueScore)).Append('|')
                    .Append(Invariant(assignment.RewardScore)).Append('|')
                    .Append(OptionalRegionTokenCodec.ToToken(assignment.RewardTier)).Append('|')
                    .Append(assignment.RequiresPartialRewardPreview ? '1' : '0').Append('\n');
            }
            text.Append("D|")
                .Append(Invariant(diagnostics.SourceRegionCount)).Append('|')
                .Append(Invariant(diagnostics.SourceType0CellAssignmentCount)).Append('|')
                .Append(Invariant(diagnostics.SourceAccessAssignmentCount)).Append('|')
                .Append(Invariant(diagnostics.TierAssignmentCount)).Append('|')
                .Append(Invariant(diagnostics.LowCount)).Append('|')
                .Append(Invariant(diagnostics.MediumCount)).Append('|')
                .Append(Invariant(diagnostics.HighCount)).Append('|')
                .Append(Invariant(diagnostics.UniqueCount)).Append('|')
                .Append(Invariant(diagnostics.DepthContributionTotal)).Append('|')
                .Append(Invariant(diagnostics.ToolContributionTotal)).Append('|')
                .Append(Invariant(diagnostics.ExplosiveContributionTotal)).Append('|')
                .Append(Invariant(diagnostics.HiddenContributionTotal)).Append('|')
                .Append(Invariant(diagnostics.RewardScoreMinimum)).Append('|')
                .Append(Invariant(diagnostics.RewardScoreMaximum)).Append('|')
                .Append(Invariant(diagnostics.RewardPreviewReservationCount)).Append('|')
                .Append(Invariant(diagnostics.MandatoryRewardSelectionCount)).Append('|')
                .Append(Invariant(diagnostics.RngDrawCount)).Append('|')
                .Append(Invariant(diagnostics.SourceMutationCount)).Append('\n');
            return Sha256(text.ToString());
        }

        private static OptionalRewardTierCalculationError Error(
            OptionalRewardTierCalculationErrorCode code,
            string sourceField,
            string message)
        {
            return new OptionalRewardTierCalculationError(
                code, default(OptionalRegionId), -1, sourceField, message);
        }

        private static OptionalRewardTierCalculationError RegionError(
            OptionalRewardTierCalculationErrorCode code,
            OptionalRegionId regionId,
            int attachmentOrder,
            string sourceField,
            string message)
        {
            return new OptionalRewardTierCalculationError(
                code, regionId, attachmentOrder, sourceField, message);
        }

        private static bool IsOpen(Type0RouteOpenMask mask, int dx, int dy)
        {
            if (dx == -1 && dy == 0) return mask.OpenLeft;
            if (dx == 1 && dy == 0) return mask.OpenRight;
            if (dx == 0 && dy == 1) return mask.OpenUp;
            if (dx == 0 && dy == -1) return mask.OpenDown;
            return false;
        }

        private static bool IsLowerHexDigest(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
            {
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')) return false;
            }
            return true;
        }

        private static string Invariant(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Sha256(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                var result = new StringBuilder(64);
                foreach (var item in hash)
                    result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
