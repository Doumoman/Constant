using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAccessRuleAssigner
    {
        public OptionalAccessAssignmentResult Assign(
            Type0RouteMaskAssignmentResult type0Assignments,
            OptionalAccessAssignmentSettings settings)
        {
            if (type0Assignments == null)
            {
                return Failure(
                    OptionalAccessAssignmentStatus.InvalidInput,
                    null,
                    new[] { Error("NULL_TYPE0_ASSIGNMENTS", "Type0 route mask assignment result cannot be null.") },
                    string.Empty,
                    string.Empty);
            }

            if (settings == null)
            {
                return Failure(
                    OptionalAccessAssignmentStatus.InvalidSettings,
                    type0Assignments,
                    new[] { Error("NULL_SETTINGS", "Optional access assignment settings cannot be null.") },
                    type0Assignments.CanonicalDigest,
                    type0Assignments.SourceGrowthDigest);
            }

            var boundaryFailure = false;
            var sourceErrors = ValidateSource(type0Assignments, ref boundaryFailure);
            if (sourceErrors.Count > 0)
            {
                return Failure(
                    boundaryFailure
                        ? OptionalAccessAssignmentStatus.InvalidBoundary
                        : OptionalAccessAssignmentStatus.InvalidInput,
                    type0Assignments,
                    sourceErrors,
                    type0Assignments.CanonicalDigest,
                    type0Assignments.SourceGrowthDigest);
            }

            var regions = new List<OptionalRegion>(type0Assignments.SourceSnapshot.Regions);
            regions.Sort((left, right) => left.RegionId.CompareTo(right.RegionId));
            var stagedAssignments = new List<OptionalAccessAssignment>(regions.Count);
            var stagedClues = new List<OptionalAccessClue>(regions.Count);
            var errors = new List<OptionalAccessAssignmentError>();
            var regionIds = new HashSet<OptionalRegionId>();
            var clueIds = new HashSet<OptionalAccessClueId>();
            var toolOrdinal = 0;
            var hiddenOrdinal = 0;

            for (var ordinal = 0; ordinal < regions.Count; ordinal++)
            {
                var region = regions[ordinal];
                var expectedId = "OPT_REGION_" + ordinal.ToString("D4", CultureInfo.InvariantCulture);
                if (!string.Equals(region.RegionId.Value, expectedId, StringComparison.Ordinal))
                {
                    errors.Add(new OptionalAccessAssignmentError(
                        "NON_CONTIGUOUS_REGION_ID", region.RegionId,
                        region.Attachment.AttachmentOrder, default(OptionalAccessClueId),
                        "Region IDs must be contiguous OPT_REGION_0000.. in ordinal order."));
                    continue;
                }

                if (!regionIds.Add(region.RegionId))
                {
                    errors.Add(new OptionalAccessAssignmentError(
                        "DUPLICATE_REGION_ID", region.RegionId,
                        region.Attachment.AttachmentOrder, default(OptionalAccessClueId),
                        "Each optional region may receive only one access assignment."));
                    continue;
                }

                var rule = settings.AccessRulePattern[ordinal % settings.AccessRulePattern.Count];
                var requirement = OptionalAccessRequirement.None;
                var traversal = OptionalAccessTraversalKind.OptionalBreak;
                var clueKind = OptionalAccessClueKind.BasicOpening;
                var toolCost = 0;
                var fuelCost = 0;
                var clueDifficulty = 0;
                var preview = false;
                var depthIndex = region.MaxDepth.Value - 1;

                switch (rule)
                {
                    case OptionalRegionAccessRule.Basic:
                        break;
                    case OptionalRegionAccessRule.Tool:
                        requirement = settings.ToolRequirementPattern[
                            toolOrdinal % settings.ToolRequirementPattern.Count];
                        toolOrdinal++;
                        clueKind = OptionalAccessClueKind.ToolSurface;
                        toolCost = settings.ToolCostTierByDepth[depthIndex];
                        break;
                    case OptionalRegionAccessRule.Environment:
                        requirement = OptionalAccessRequirement.Environment;
                        clueKind = OptionalAccessClueKind.EnvironmentDevice;
                        break;
                    case OptionalRegionAccessRule.Explosive:
                        requirement = OptionalAccessRequirement.Explosive;
                        clueKind = OptionalAccessClueKind.ExplosiveRewardPreview;
                        fuelCost = settings.ExplosiveFuelCostByDepth[depthIndex];
                        preview = true;
                        break;
                    case OptionalRegionAccessRule.Hidden:
                        traversal = OptionalAccessTraversalKind.Hidden;
                        clueKind = settings.HiddenCluePattern[
                            hiddenOrdinal % settings.HiddenCluePattern.Count];
                        hiddenOrdinal++;
                        clueDifficulty = settings.HiddenClueDifficultyByDepth[depthIndex];
                        break;
                    default:
                        errors.Add(new OptionalAccessAssignmentError(
                            "UNDEFINED_ACCESS_RULE", region.RegionId,
                            region.Attachment.AttachmentOrder, default(OptionalAccessClueId),
                            "Access rule pattern contains an undefined value."));
                        continue;
                }

                var clueText = "CLUE_" + region.RegionId.Value + "_" +
                               OptionalRegionTokenCodec.ToToken(rule);
                if (!OptionalAccessClueId.TryCreate(clueText, out var clueId))
                {
                    errors.Add(new OptionalAccessAssignmentError(
                        "INVALID_CLUE_ID", region.RegionId,
                        region.Attachment.AttachmentOrder, default(OptionalAccessClueId),
                        "Derived clue ID does not satisfy the canonical grammar."));
                    continue;
                }

                if (!clueIds.Add(clueId))
                {
                    errors.Add(new OptionalAccessAssignmentError(
                        "DUPLICATE_CLUE_ID", region.RegionId,
                        region.Attachment.AttachmentOrder, clueId,
                        "Each optional region must publish a unique clue ID."));
                    continue;
                }

                try
                {
                    var clue = new OptionalAccessClue(
                        clueId, region.RegionId, clueKind,
                        region.Attachment.AttachmentOrder, true, preview);
                    var attachment = region.Attachment;
                    var assignment = new OptionalAccessAssignment(
                        region.RegionId, ordinal, attachment.AttachmentOrder,
                        attachment.MandatoryRouteSectorIndex, attachment.MandatoryRouteSector,
                        attachment.EntrySectorIndex, attachment.EntrySector,
                        attachment.EntrySideFromMandatoryDx, attachment.EntrySideFromMandatoryDy,
                        rule, requirement, traversal, clue,
                        toolCost, fuelCost, clueDifficulty, preview);
                    stagedClues.Add(clue);
                    stagedAssignments.Add(assignment);
                }
                catch (Exception exception)
                {
                    errors.Add(new OptionalAccessAssignmentError(
                        "INVALID_RULE_MATRIX", region.RegionId,
                        region.Attachment.AttachmentOrder, clueId,
                        "Access assignment matrix validation failed: " + exception.GetType().Name + "."));
                }
            }

            if (errors.Count > 0 || stagedAssignments.Count != regions.Count || stagedClues.Count != regions.Count)
            {
                if (errors.Count == 0)
                    errors.Add(Error("INCOMPLETE_PUBLICATION", "Every optional region requires one assignment and one clue."));
                return Failure(
                    OptionalAccessAssignmentStatus.InvalidAssignment,
                    type0Assignments,
                    errors,
                    type0Assignments.CanonicalDigest,
                    type0Assignments.SourceGrowthDigest);
            }

            stagedAssignments.Sort((left, right) => left.RegionId.CompareTo(right.RegionId));
            stagedClues.Sort((left, right) => left.ClueId.CompareTo(right.ClueId));
            var diagnostics = CreateDiagnostics(type0Assignments, stagedAssignments, stagedClues);
            if (diagnostics.PerceptibleClueCount != regions.Count ||
                diagnostics.AssignmentCount != regions.Count ||
                diagnostics.ClueCount != regions.Count ||
                diagnostics.AttachmentBoundaryBaseOpenCount != 0)
            {
                return Failure(
                    OptionalAccessAssignmentStatus.InvalidAssignment,
                    type0Assignments,
                    new[] { Error("ASSIGNMENT_ACCOUNTING_MISMATCH", "Assignment diagnostics do not match the staged publication.") },
                    type0Assignments.CanonicalDigest,
                    type0Assignments.SourceGrowthDigest);
            }

            var digest = ComputeDigest(
                type0Assignments.CanonicalDigest,
                type0Assignments.SourceGrowthDigest,
                settings,
                stagedAssignments,
                stagedClues,
                diagnostics);
            return new OptionalAccessAssignmentResult(
                OptionalAccessAssignmentStatus.Completed,
                stagedAssignments,
                stagedClues,
                diagnostics,
                Array.Empty<OptionalAccessAssignmentError>(),
                type0Assignments.CanonicalDigest,
                type0Assignments.SourceGrowthDigest,
                digest);
        }

        private static List<OptionalAccessAssignmentError> ValidateSource(
            Type0RouteMaskAssignmentResult source,
            ref bool boundaryFailure)
        {
            var errors = new List<OptionalAccessAssignmentError>();
            if (!source.IsSuccess || source.Status != Type0RouteMaskAssignmentStatus.Completed)
                errors.Add(Error("TYPE0_NOT_COMPLETED", "Type0 route mask assignment must be completed."));
            if (source.SourceSnapshot == null)
                errors.Add(Error("NULL_TYPE0_SNAPSHOT", "Type0 assignment source snapshot cannot be null."));
            if (source.Diagnostics == null)
                errors.Add(Error("NULL_TYPE0_DIAGNOSTICS", "Type0 assignment diagnostics cannot be null."));
            if (!IsLowerHexDigest(source.CanonicalDigest) ||
                !IsLowerHexDigest(source.SourceGrowthDigest) ||
                !IsLowerHexDigest(source.SourceRouteMaskCatalogDigest))
                errors.Add(Error("INVALID_SOURCE_DIGEST", "Type0 and growth digests must be lowercase SHA-256 values."));
            if (source.RngDrawCount != 0)
                errors.Add(Error("TYPE0_RNG_NOT_ZERO", "Type0 assignment must consume zero RNG draws."));

            if (source.SourceSnapshot == null || source.Diagnostics == null) return errors;
            var snapshot = source.SourceSnapshot;
            var diagnostics = source.Diagnostics;
            if (diagnostics.SourceRegionCount != snapshot.Regions.Count ||
                diagnostics.SourceCellCount != snapshot.Cells.Count ||
                diagnostics.AssignmentCount != source.Assignments.Count ||
                source.Assignments.Count != snapshot.Cells.Count)
                errors.Add(Error("SOURCE_ACCOUNTING_MISMATCH", "Type0 diagnostics, snapshot, and assignments must agree."));
            if (diagnostics.SourceMutationCount != 0)
                errors.Add(Error("SOURCE_MUTATION_NOT_ZERO", "Type0 assignment source mutation count must remain zero."));

            if (diagnostics.AttachmentBoundaryClosedCount != snapshot.Regions.Count)
            {
                boundaryFailure = true;
                errors.Add(Error("ATTACHMENT_CLOSED_COUNT_MISMATCH", "Every optional attachment boundary must remain base-closed."));
            }
            if (diagnostics.MandatoryBoundaryBaseOpenCount != 0)
            {
                boundaryFailure = true;
                errors.Add(Error("MANDATORY_BASE_OPEN", "Optional access reservation cannot open a mandatory base edge."));
            }

            var assignmentsBySector = new Dictionary<int, Type0RouteMaskAssignment>();
            foreach (var assignment in source.Assignments)
            {
                if (assignment == null || !assignmentsBySector.TryAdd(
                        assignment == null ? -1 : assignment.SectorIndex, assignment))
                {
                    errors.Add(Error("DUPLICATE_OR_NULL_TYPE0_ASSIGNMENT", "Type0 assignments must be non-null and unique by sector."));
                    continue;
                }
                if (assignment.OpenMask.HasHorizontalThrough)
                    errors.Add(new OptionalAccessAssignmentError(
                        "TYPE0_HORIZONTAL_THROUGH", assignment.RegionId, -1,
                        default(OptionalAccessClueId), "Type0 assignments cannot open left and right simultaneously."));
            }

            foreach (var cell in snapshot.Cells)
            {
                if (!assignmentsBySector.TryGetValue(cell.SectorIndex, out var assignment) ||
                    assignment.RegionId != cell.RegionId ||
                    assignment.Sector != cell.Sector ||
                    assignment.Depth != cell.Depth ||
                    assignment.IsAttachmentCell != cell.IsAttachmentCell)
                {
                    errors.Add(new OptionalAccessAssignmentError(
                        "TYPE0_CELL_IDENTITY_MISMATCH", cell.RegionId, -1,
                        default(OptionalAccessClueId), "Type0 assignment must preserve every source cell identity."));
                }
            }

            foreach (var region in snapshot.Regions)
            {
                var attachment = region.Attachment;
                var expectedNeighbor = Neighbor(
                    attachment.MandatoryRouteSectorIndex,
                    attachment.EntrySideFromMandatoryDx,
                    attachment.EntrySideFromMandatoryDy);
                if (expectedNeighbor != attachment.EntrySectorIndex ||
                    !assignmentsBySector.TryGetValue(attachment.EntrySectorIndex, out var assignment) ||
                    assignment.RegionId != region.RegionId)
                {
                    boundaryFailure = true;
                    errors.Add(new OptionalAccessAssignmentError(
                        "INVALID_ATTACHMENT_IDENTITY", region.RegionId, attachment.AttachmentOrder,
                        default(OptionalAccessClueId), "Attachment direction and source Type0 assignment must agree."));
                    continue;
                }

                if (IsOpen(
                        assignment.OpenMask,
                        -attachment.EntrySideFromMandatoryDx,
                        -attachment.EntrySideFromMandatoryDy))
                {
                    boundaryFailure = true;
                    errors.Add(new OptionalAccessAssignmentError(
                        "ATTACHMENT_BASE_OPEN", region.RegionId, attachment.AttachmentOrder,
                        default(OptionalAccessClueId), "Attachment-to-mandatory base side must remain closed."));
                }
            }

            return errors;
        }

        private static OptionalAccessAssignmentDiagnostics CreateDiagnostics(
            Type0RouteMaskAssignmentResult source,
            IReadOnlyList<OptionalAccessAssignment> assignments,
            IReadOnlyList<OptionalAccessClue> clues)
        {
            var basic = 0;
            var tool = 0;
            var environment = 0;
            var explosive = 0;
            var hidden = 0;
            var pickaxe = 0;
            var shovel = 0;
            var rope = 0;
            var crack = 0;
            var light = 0;
            var sound = 0;
            var perceptible = 0;
            var preview = 0;
            foreach (var assignment in assignments)
            {
                switch (assignment.AccessRule)
                {
                    case OptionalRegionAccessRule.Basic: basic++; break;
                    case OptionalRegionAccessRule.Tool: tool++; break;
                    case OptionalRegionAccessRule.Environment: environment++; break;
                    case OptionalRegionAccessRule.Explosive: explosive++; break;
                    case OptionalRegionAccessRule.Hidden: hidden++; break;
                }
                switch (assignment.Requirement)
                {
                    case OptionalAccessRequirement.Pickaxe: pickaxe++; break;
                    case OptionalAccessRequirement.Shovel: shovel++; break;
                    case OptionalAccessRequirement.Rope: rope++; break;
                }
                if (assignment.RequiresPartialRewardPreview) preview++;
            }
            foreach (var clue in clues)
            {
                if (clue.IsPerceptibleFromMandatory) perceptible++;
                switch (clue.Kind)
                {
                    case OptionalAccessClueKind.HiddenCrack: crack++; break;
                    case OptionalAccessClueKind.HiddenLight: light++; break;
                    case OptionalAccessClueKind.HiddenSound: sound++; break;
                }
            }

            return new OptionalAccessAssignmentDiagnostics(
                source.SourceSnapshot.Regions.Count,
                source.SourceSnapshot.Cells.Count,
                source.Assignments.Count,
                assignments.Count,
                clues.Count,
                basic,
                tool,
                environment,
                explosive,
                hidden,
                pickaxe,
                shovel,
                rope,
                crack,
                light,
                sound,
                perceptible,
                preview,
                source.Diagnostics.MandatoryBoundaryBaseOpenCount,
                0,
                0);
        }

        private static OptionalAccessAssignmentResult Failure(
            OptionalAccessAssignmentStatus status,
            Type0RouteMaskAssignmentResult source,
            IEnumerable<OptionalAccessAssignmentError> errors,
            string type0Digest,
            string growthDigest)
        {
            var regions = source == null || source.SourceSnapshot == null ? 0 : source.SourceSnapshot.Regions.Count;
            var cells = source == null || source.SourceSnapshot == null ? 0 : source.SourceSnapshot.Cells.Count;
            var type0 = source == null ? 0 : source.Assignments.Count;
            var baseOpen = source == null || source.Diagnostics == null
                ? 0
                : source.Diagnostics.MandatoryBoundaryBaseOpenCount;
            var diagnostics = new OptionalAccessAssignmentDiagnostics(
                regions, cells, type0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, baseOpen, 0, 0);
            return new OptionalAccessAssignmentResult(
                status,
                Array.Empty<OptionalAccessAssignment>(),
                Array.Empty<OptionalAccessClue>(),
                diagnostics,
                errors,
                type0Digest,
                growthDigest,
                string.Empty);
        }

        private static string ComputeDigest(
            string type0Digest,
            string growthDigest,
            OptionalAccessAssignmentSettings settings,
            IReadOnlyList<OptionalAccessAssignment> assignments,
            IReadOnlyList<OptionalAccessClue> clues,
            OptionalAccessAssignmentDiagnostics diagnostics)
        {
            var text = new StringBuilder();
            text.Append("S|").Append(type0Digest).Append('|').Append(growthDigest).Append('\n');
            text.Append("R");
            foreach (var value in settings.AccessRulePattern)
                text.Append('|').Append(OptionalRegionTokenCodec.ToToken(value));
            text.Append('\n').Append("T");
            foreach (var value in settings.ToolRequirementPattern)
                text.Append('|').Append(OptionalAccessAssignmentEnums.ToToken(value));
            text.Append('\n').Append("H");
            foreach (var value in settings.HiddenCluePattern)
                text.Append('|').Append(OptionalAccessAssignmentEnums.ToToken(value));
            AppendDepthTable(text, "TC", settings.ToolCostTierByDepth);
            AppendDepthTable(text, "EF", settings.ExplosiveFuelCostByDepth);
            AppendDepthTable(text, "HD", settings.HiddenClueDifficultyByDepth);

            foreach (var assignment in assignments)
            {
                text.Append("A|").Append(assignment.RegionId.Value).Append('|')
                    .Append(Invariant(assignment.RegionOrdinal)).Append('|')
                    .Append(Invariant(assignment.AttachmentOrder)).Append('|')
                    .Append(Invariant(assignment.MandatoryRouteSectorIndex)).Append('|')
                    .Append(Invariant(assignment.EntrySectorIndex)).Append('|')
                    .Append(Invariant(assignment.EntrySideFromMandatoryDx)).Append('|')
                    .Append(Invariant(assignment.EntrySideFromMandatoryDy)).Append('|')
                    .Append(OptionalRegionTokenCodec.ToToken(assignment.AccessRule)).Append('|')
                    .Append(OptionalAccessAssignmentEnums.ToToken(assignment.Requirement)).Append('|')
                    .Append(OptionalAccessAssignmentEnums.ToToken(assignment.TraversalKind)).Append('|')
                    .Append(assignment.Clue.ClueId.Value).Append('|')
                    .Append(Invariant(assignment.ToolCostTier)).Append('|')
                    .Append(Invariant(assignment.ExplosiveFuelCost)).Append('|')
                    .Append(Invariant(assignment.HiddenClueDifficulty)).Append('|')
                    .Append(assignment.RequiresPartialRewardPreview ? '1' : '0').Append('\n');
            }
            foreach (var clue in clues)
            {
                text.Append("C|").Append(clue.ClueId.Value).Append('|')
                    .Append(clue.RegionId.Value).Append('|')
                    .Append(OptionalAccessAssignmentEnums.ToToken(clue.Kind)).Append('|')
                    .Append(Invariant(clue.AttachmentOrder)).Append('|')
                    .Append(clue.IsPerceptibleFromMandatory ? '1' : '0').Append('|')
                    .Append(clue.RequiresRewardPreview ? '1' : '0').Append('\n');
            }
            text.Append("D|")
                .Append(Invariant(diagnostics.SourceRegionCount)).Append('|')
                .Append(Invariant(diagnostics.SourceCellCount)).Append('|')
                .Append(Invariant(diagnostics.SourceType0AssignmentCount)).Append('|')
                .Append(Invariant(diagnostics.AssignmentCount)).Append('|')
                .Append(Invariant(diagnostics.ClueCount)).Append('|')
                .Append(Invariant(diagnostics.BasicCount)).Append('|')
                .Append(Invariant(diagnostics.ToolCount)).Append('|')
                .Append(Invariant(diagnostics.EnvironmentCount)).Append('|')
                .Append(Invariant(diagnostics.ExplosiveCount)).Append('|')
                .Append(Invariant(diagnostics.HiddenCount)).Append('|')
                .Append(Invariant(diagnostics.PickaxeCount)).Append('|')
                .Append(Invariant(diagnostics.ShovelCount)).Append('|')
                .Append(Invariant(diagnostics.RopeCount)).Append('|')
                .Append(Invariant(diagnostics.HiddenCrackCount)).Append('|')
                .Append(Invariant(diagnostics.HiddenLightCount)).Append('|')
                .Append(Invariant(diagnostics.HiddenSoundCount)).Append('|')
                .Append(Invariant(diagnostics.PerceptibleClueCount)).Append('|')
                .Append(Invariant(diagnostics.RewardPreviewReservationCount)).Append('|')
                .Append(Invariant(diagnostics.AttachmentBoundaryBaseOpenCount)).Append('|')
                .Append(Invariant(diagnostics.RngDrawCount)).Append('|')
                .Append(Invariant(diagnostics.SourceMutationCount)).Append('\n');
            return Sha256(text.ToString());
        }

        private static void AppendDepthTable(StringBuilder text, string name, IReadOnlyList<int> values)
        {
            text.Append('\n').Append(name);
            foreach (var value in values) text.Append('|').Append(Invariant(value));
            text.Append('\n');
        }

        private static int Neighbor(int sectorIndex, int dx, int dy)
        {
            var x = sectorIndex % WorldGenConstants.SectorColumns;
            var y = sectorIndex / WorldGenConstants.SectorColumns;
            var nextX = x + dx;
            var nextY = y + dy;
            if (nextX < 0 || nextX >= WorldGenConstants.SectorColumns ||
                nextY < 0 || nextY >= WorldGenConstants.SectorRows) return -1;
            return nextY * WorldGenConstants.SectorColumns + nextX;
        }

        private static bool IsOpen(Type0RouteOpenMask mask, int dx, int dy)
        {
            if (dx == -1 && dy == 0) return mask.OpenLeft;
            if (dx == 1 && dy == 0) return mask.OpenRight;
            if (dx == 0 && dy == 1) return mask.OpenUp;
            if (dx == 0 && dy == -1) return mask.OpenDown;
            return false;
        }

        private static OptionalAccessAssignmentError Error(string code, string message)
        {
            return new OptionalAccessAssignmentError(
                code, default(OptionalRegionId), -1, default(OptionalAccessClueId), message);
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
    }
}
