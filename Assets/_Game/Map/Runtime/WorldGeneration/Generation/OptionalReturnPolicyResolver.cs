using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalReturnPolicyResolver
    {
        private static readonly Direction[] Directions =
        {
            new Direction(-1, 0),
            new Direction(1, 0),
            new Direction(0, 1),
            new Direction(0, -1)
        };

        public OptionalReturnPolicyResult Resolve(
            Type0RouteMaskAssignmentResult type0Assignments,
            OptionalAccessAssignmentResult accessAssignments,
            OptionalRewardTierResult rewardTiers,
            OptionalReturnPolicySettings settings)
        {
            if (type0Assignments == null)
                return Failure(OptionalReturnPolicyResolutionStatus.InvalidInput,
                    OptionalReturnPolicyResolutionErrorCode.NullInput, "type0Assignments", "Type0 assignments are required.");
            if (accessAssignments == null)
                return Failure(OptionalReturnPolicyResolutionStatus.InvalidInput,
                    OptionalReturnPolicyResolutionErrorCode.NullInput, "accessAssignments", "Access assignments are required.");
            if (rewardTiers == null)
                return Failure(OptionalReturnPolicyResolutionStatus.InvalidInput,
                    OptionalReturnPolicyResolutionErrorCode.NullInput, "rewardTiers", "Reward tiers are required.");
            if (settings == null)
                return Failure(OptionalReturnPolicyResolutionStatus.InvalidSettings,
                    OptionalReturnPolicyResolutionErrorCode.NullInput, "settings", "Return policy settings are required.");

            var snapshot = type0Assignments.SourceSnapshot;
            if (!type0Assignments.IsSuccess || !accessAssignments.IsSuccess || !rewardTiers.IsSuccess || snapshot == null)
                return Failure(OptionalReturnPolicyResolutionStatus.InvalidSource,
                    OptionalReturnPolicyResolutionErrorCode.InvalidStatus, "Status", "Every source result must be completed.",
                    type0Assignments, accessAssignments, rewardTiers);

            if (!OptionalReturnPolicyResult.IsLowerHexDigest(type0Assignments.CanonicalDigest) ||
                !OptionalReturnPolicyResult.IsLowerHexDigest(type0Assignments.SourceGrowthDigest) ||
                !OptionalReturnPolicyResult.IsLowerHexDigest(accessAssignments.CanonicalDigest) ||
                !OptionalReturnPolicyResult.IsLowerHexDigest(accessAssignments.SourceType0AssignmentDigest) ||
                !OptionalReturnPolicyResult.IsLowerHexDigest(accessAssignments.SourceGrowthDigest) ||
                !OptionalReturnPolicyResult.IsLowerHexDigest(rewardTiers.CanonicalDigest) ||
                !OptionalReturnPolicyResult.IsLowerHexDigest(rewardTiers.SourceType0AssignmentDigest) ||
                !OptionalReturnPolicyResult.IsLowerHexDigest(rewardTiers.SourceAccessAssignmentDigest) ||
                !OptionalReturnPolicyResult.IsLowerHexDigest(rewardTiers.SourceGrowthDigest))
                return Failure(OptionalReturnPolicyResolutionStatus.InvalidSource,
                    OptionalReturnPolicyResolutionErrorCode.InvalidDigest, "Digest", "Every source digest must be lowercase SHA-256.",
                    type0Assignments, accessAssignments, rewardTiers);

            if (!string.Equals(accessAssignments.SourceType0AssignmentDigest, type0Assignments.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(rewardTiers.SourceType0AssignmentDigest, type0Assignments.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(rewardTiers.SourceAccessAssignmentDigest, accessAssignments.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(accessAssignments.SourceGrowthDigest, type0Assignments.SourceGrowthDigest, StringComparison.Ordinal) ||
                !string.Equals(rewardTiers.SourceGrowthDigest, type0Assignments.SourceGrowthDigest, StringComparison.Ordinal))
                return Failure(OptionalReturnPolicyResolutionStatus.InvalidSource,
                    OptionalReturnPolicyResolutionErrorCode.SourceMismatch, "SourceChain", "Source-chain digests do not match.",
                    type0Assignments, accessAssignments, rewardTiers);

            if (!HasValidAccounting(type0Assignments, accessAssignments, rewardTiers))
                return Failure(OptionalReturnPolicyResolutionStatus.InvalidSource,
                    OptionalReturnPolicyResolutionErrorCode.InvalidAccounting, "Diagnostics", "Source diagnostics do not match published values.",
                    type0Assignments, accessAssignments, rewardTiers);

            var regions = snapshot.Regions.OrderBy(value => value.RegionId).ToList();
            var accessByRegion = new Dictionary<OptionalRegionId, OptionalAccessAssignment>();
            foreach (var assignment in accessAssignments.Assignments)
            {
                if (accessByRegion.ContainsKey(assignment.RegionId))
                    return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidSource,
                        OptionalReturnPolicyResolutionErrorCode.DuplicateRegion, assignment.RegionId,
                        -1, assignment.AttachmentOrder, "AccessAssignments", "Access region IDs must be unique.",
                        type0Assignments, accessAssignments, rewardTiers);
                accessByRegion.Add(assignment.RegionId, assignment);
            }

            var rewardByRegion = new Dictionary<OptionalRegionId, OptionalRewardTierAssignment>();
            foreach (var assignment in rewardTiers.Assignments)
            {
                if (rewardByRegion.ContainsKey(assignment.RegionId))
                    return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidSource,
                        OptionalReturnPolicyResolutionErrorCode.DuplicateRegion, assignment.RegionId,
                        -1, assignment.AttachmentOrder, "RewardTierAssignments", "Reward-tier region IDs must be unique.",
                        type0Assignments, accessAssignments, rewardTiers);
                rewardByRegion.Add(assignment.RegionId, assignment);
            }

            var type0BySector = new Dictionary<int, Type0RouteMaskAssignment>();
            foreach (var assignment in type0Assignments.Assignments)
            {
                if (type0BySector.ContainsKey(assignment.SectorIndex))
                    return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidSource,
                        OptionalReturnPolicyResolutionErrorCode.InvalidAccounting, assignment.RegionId,
                        assignment.SectorIndex, -1, "Type0Assignments", "Type0 sector assignments must be unique.",
                        type0Assignments, accessAssignments, rewardTiers);
                type0BySector.Add(assignment.SectorIndex, assignment);
            }

            var staged = new List<OptionalReturnPolicyAssignment>();
            var totalEdges = 0;
            var totalReturnable = 0;
            var totalWitnessSectors = 0;
            var totalWitnessEdges = 0;
            var maximumWitnessSectors = 0;

            for (var regionOrdinal = 0; regionOrdinal < regions.Count; regionOrdinal++)
            {
                var region = regions[regionOrdinal];
                if (!accessByRegion.TryGetValue(region.RegionId, out var access) ||
                    !rewardByRegion.TryGetValue(region.RegionId, out var reward))
                    return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidSource,
                        OptionalReturnPolicyResolutionErrorCode.MissingRegion, region.RegionId,
                        -1, region.Attachment.AttachmentOrder, "RegionJoin", "Every region requires access and reward assignments.",
                        type0Assignments, accessAssignments, rewardTiers);

                if (!PreservesSourceIdentity(region, regionOrdinal, access, reward))
                    return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidSource,
                        OptionalReturnPolicyResolutionErrorCode.SourceMismatch, region.RegionId,
                        region.Attachment.EntrySectorIndex, region.Attachment.AttachmentOrder,
                        "RegionIdentity", "Attachment, access, clue, or reward identity changed across the source chain.",
                        type0Assignments, accessAssignments, rewardTiers);

                if (region.ReturnPolicy != OptionalReturnPolicy.BacktrackToAttachment)
                    return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidSource,
                        OptionalReturnPolicyResolutionErrorCode.SourceMismatch, region.RegionId,
                        region.Attachment.EntrySectorIndex, region.Attachment.AttachmentOrder,
                        "ReturnPolicy", "The source region must use BacktrackToAttachment.",
                        type0Assignments, accessAssignments, rewardTiers);

                foreach (var cell in region.Cells)
                {
                    if (cell.RequiresReturnConnection)
                        return FailureForRegion(OptionalReturnPolicyResolutionStatus.UnsupportedReturnRequirement,
                            OptionalReturnPolicyResolutionErrorCode.UnsupportedReturnRequirement, region.RegionId,
                            cell.SectorIndex, region.Attachment.AttachmentOrder,
                            "RequiresReturnConnection", "A separate return connection cannot be synthesized.",
                            type0Assignments, accessAssignments, rewardTiers);
                    if (!type0BySector.TryGetValue(cell.SectorIndex, out var type0) ||
                        type0.RegionId != cell.RegionId || type0.Sector != cell.Sector ||
                        type0.Depth != cell.Depth || type0.IsAttachmentCell != cell.IsAttachmentCell)
                        return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidSource,
                            OptionalReturnPolicyResolutionErrorCode.SourceMismatch, region.RegionId,
                            cell.SectorIndex, region.Attachment.AttachmentOrder,
                            "Type0CellIdentity", "Type0 assignment identity must match its source cell.",
                            type0Assignments, accessAssignments, rewardTiers);
                }

                var attachmentMask = type0BySector[region.Attachment.EntrySectorIndex].OpenMask;
                if (IsOpen(attachmentMask,
                        -region.Attachment.EntrySideFromMandatoryDx,
                        -region.Attachment.EntrySideFromMandatoryDy))
                    return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidTopology,
                        OptionalReturnPolicyResolutionErrorCode.InvalidAttachment, region.RegionId,
                        region.Attachment.EntrySectorIndex, region.Attachment.AttachmentOrder,
                        "AttachmentBoundary", "The attachment-to-mandatory BaseEdge must remain closed.",
                        type0Assignments, accessAssignments, rewardTiers, 1);

                var parentBySector = new Dictionary<int, int>();
                var visited = new HashSet<int>();
                var queue = new Queue<OptionalRegionCell>();
                var root = region.Cells.Single(value => value.IsAttachmentCell);
                visited.Add(root.SectorIndex);
                queue.Enqueue(root);
                var regionEdgeCount = 0;

                foreach (var cell in region.Cells)
                {
                    var sourceMask = type0BySector[cell.SectorIndex].OpenMask;
                    foreach (var direction in Directions)
                    {
                        if (!IsOpen(sourceMask, direction.Dx, direction.Dy)) continue;
                        var neighbor = FindNeighbor(region.Cells, cell.Sector.X + direction.Dx, cell.Sector.Y + direction.Dy);
                        if (neighbor == null)
                            return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidTopology,
                                OptionalReturnPolicyResolutionErrorCode.InvalidBaseEdge, region.RegionId,
                                cell.SectorIndex, region.Attachment.AttachmentOrder,
                                "BaseEdge", "Open Type0 sides must target a cell in the same region.",
                                type0Assignments, accessAssignments, rewardTiers);
                        if (!IsOpen(type0BySector[neighbor.SectorIndex].OpenMask, -direction.Dx, -direction.Dy))
                            return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidTopology,
                                OptionalReturnPolicyResolutionErrorCode.NonReciprocalBaseEdge, region.RegionId,
                                cell.SectorIndex, region.Attachment.AttachmentOrder,
                                "BaseEdge", "Internal Type0 BaseEdges must be reciprocal.",
                                type0Assignments, accessAssignments, rewardTiers);
                        if (cell.SectorIndex < neighbor.SectorIndex) regionEdgeCount++;
                    }
                }

                while (queue.Count != 0)
                {
                    var cell = queue.Dequeue();
                    var sourceMask = type0BySector[cell.SectorIndex].OpenMask;
                    foreach (var direction in Directions)
                    {
                        if (!IsOpen(sourceMask, direction.Dx, direction.Dy)) continue;
                        var neighbor = FindNeighbor(region.Cells, cell.Sector.X + direction.Dx, cell.Sector.Y + direction.Dy);
                        if (neighbor == null || visited.Contains(neighbor.SectorIndex)) continue;
                        visited.Add(neighbor.SectorIndex);
                        parentBySector.Add(neighbor.SectorIndex, cell.SectorIndex);
                        queue.Enqueue(neighbor);
                    }
                }

                if (visited.Count != region.Cells.Count)
                {
                    var unreachable = region.Cells.OrderBy(value => value.SectorIndex)
                        .First(value => !visited.Contains(value.SectorIndex));
                    return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidTopology,
                        OptionalReturnPolicyResolutionErrorCode.UnreachableCell, region.RegionId,
                        unreachable.SectorIndex, region.Attachment.AttachmentOrder,
                        "ReturnGraph", "Every optional cell must reach the attachment cell.",
                        type0Assignments, accessAssignments, rewardTiers,
                        0, visited.Count, region.Cells.Count - visited.Count, regionEdgeCount);
                }

                var critical = region.Cells.OrderByDescending(value => value.Depth.Value)
                    .ThenBy(value => value.SectorIndex).First();
                var path = new List<int> { critical.SectorIndex };
                var cursor = critical.SectorIndex;
                while (cursor != root.SectorIndex)
                {
                    cursor = parentBySector[cursor];
                    path.Add(cursor);
                }
                if (path.Count > settings.MaximumBacktrackSectorCount)
                    return FailureForRegion(OptionalReturnPolicyResolutionStatus.InvalidTopology,
                        OptionalReturnPolicyResolutionErrorCode.PathLimitExceeded, region.RegionId,
                        critical.SectorIndex, region.Attachment.AttachmentOrder,
                        "MaximumBacktrackSectorCount", "The canonical critical witness exceeds the configured path limit.",
                        type0Assignments, accessAssignments, rewardTiers,
                        0, visited.Count, 0, regionEdgeCount);

                staged.Add(new OptionalReturnPolicyAssignment(
                    region.RegionId,
                    regionOrdinal,
                    region.Attachment.AttachmentOrder,
                    access.AccessRule,
                    reward.RewardTier,
                    OptionalReturnPolicy.BacktrackToAttachment,
                    critical.SectorIndex,
                    critical.Depth,
                    region.Attachment.EntrySectorIndex,
                    region.Attachment.MandatoryRouteSectorIndex,
                    path,
                    path.Count - 1,
                    region.Cells.Count,
                    true,
                    false));
                totalEdges += regionEdgeCount;
                totalReturnable += visited.Count;
                totalWitnessSectors += path.Count;
                totalWitnessEdges += path.Count - 1;
                maximumWitnessSectors = Math.Max(maximumWitnessSectors, path.Count);
            }

            var diagnostics = new OptionalReturnPolicyDiagnostics(
                regions.Count,
                type0Assignments.Assignments.Count,
                accessAssignments.Assignments.Count,
                rewardTiers.Assignments.Count,
                staged.Count,
                staged.Count,
                0,
                0,
                totalReturnable,
                0,
                totalEdges,
                totalWitnessSectors,
                totalWitnessEdges,
                maximumWitnessSectors,
                staged.Count,
                0,
                0,
                0,
                0,
                0);
            var digest = ComputeDigest(type0Assignments, accessAssignments, rewardTiers, settings, staged, diagnostics);
            return new OptionalReturnPolicyResult(
                OptionalReturnPolicyResolutionStatus.Completed,
                staged,
                diagnostics,
                Array.Empty<OptionalReturnPolicyResolutionError>(),
                type0Assignments.CanonicalDigest,
                accessAssignments.CanonicalDigest,
                rewardTiers.CanonicalDigest,
                type0Assignments.SourceGrowthDigest,
                digest);
        }

        private static bool HasValidAccounting(
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access,
            OptionalRewardTierResult reward)
        {
            var snapshot = type0.SourceSnapshot;
            return type0.Diagnostics.SourceRegionCount == snapshot.Regions.Count &&
                   type0.Diagnostics.SourceCellCount == snapshot.Cells.Count &&
                   type0.Diagnostics.AssignmentCount == type0.Assignments.Count &&
                   type0.Assignments.Count == snapshot.Cells.Count &&
                   type0.Diagnostics.MandatoryBoundaryBaseOpenCount == 0 &&
                   type0.Diagnostics.RngDrawCount == 0 && type0.Diagnostics.SourceMutationCount == 0 &&
                   access.Diagnostics.SourceRegionCount == snapshot.Regions.Count &&
                   access.Diagnostics.SourceCellCount == snapshot.Cells.Count &&
                   access.Diagnostics.SourceType0AssignmentCount == type0.Assignments.Count &&
                   access.Diagnostics.AssignmentCount == access.Assignments.Count &&
                   access.Diagnostics.ClueCount == access.Clues.Count &&
                   access.Assignments.Count == snapshot.Regions.Count &&
                   access.Diagnostics.AttachmentBoundaryBaseOpenCount == 0 &&
                   access.Diagnostics.RngDrawCount == 0 && access.Diagnostics.SourceMutationCount == 0 &&
                   reward.Diagnostics.SourceRegionCount == snapshot.Regions.Count &&
                   reward.Diagnostics.SourceType0CellAssignmentCount == type0.Assignments.Count &&
                   reward.Diagnostics.SourceAccessAssignmentCount == access.Assignments.Count &&
                   reward.Diagnostics.TierAssignmentCount == reward.Assignments.Count &&
                   reward.Assignments.Count == snapshot.Regions.Count &&
                   reward.Diagnostics.MandatoryRewardSelectionCount == 0 &&
                   reward.Diagnostics.RngDrawCount == 0 && reward.Diagnostics.SourceMutationCount == 0;
        }

        private static bool PreservesSourceIdentity(
            OptionalRegion region,
            int regionOrdinal,
            OptionalAccessAssignment access,
            OptionalRewardTierAssignment reward)
        {
            var attachment = region.Attachment;
            return access.RegionId == region.RegionId && reward.RegionId == region.RegionId &&
                   access.RegionOrdinal == regionOrdinal && reward.RegionOrdinal == regionOrdinal &&
                   access.AttachmentOrder == attachment.AttachmentOrder &&
                   reward.AttachmentOrder == attachment.AttachmentOrder &&
                   access.MandatoryRouteSectorIndex == attachment.MandatoryRouteSectorIndex &&
                   access.MandatoryRouteSector == attachment.MandatoryRouteSector &&
                   access.EntrySectorIndex == attachment.EntrySectorIndex &&
                   access.EntrySector == attachment.EntrySector &&
                   access.EntrySideFromMandatoryDx == attachment.EntrySideFromMandatoryDx &&
                   access.EntrySideFromMandatoryDy == attachment.EntrySideFromMandatoryDy &&
                   reward.ClueId == access.Clue.ClueId && reward.AccessRule == access.AccessRule &&
                   reward.MaxDepth == region.MaxDepth.Value &&
                   reward.ToolCostTier == access.ToolCostTier &&
                   reward.ExplosiveFuelCost == access.ExplosiveFuelCost &&
                   reward.HiddenClueDifficulty == access.HiddenClueDifficulty &&
                   reward.RequiresPartialRewardPreview == access.RequiresPartialRewardPreview;
        }

        private static OptionalRegionCell FindNeighbor(
            IReadOnlyList<OptionalRegionCell> cells,
            int x,
            int y)
        {
            for (var index = 0; index < cells.Count; index++)
            {
                if (cells[index].Sector.X == x && cells[index].Sector.Y == y) return cells[index];
            }
            return null;
        }

        private static bool IsOpen(Type0RouteOpenMask mask, int dx, int dy)
        {
            if (dx == -1 && dy == 0) return mask.OpenLeft;
            if (dx == 1 && dy == 0) return mask.OpenRight;
            if (dx == 0 && dy == 1) return mask.OpenUp;
            if (dx == 0 && dy == -1) return mask.OpenDown;
            throw new ArgumentException("Direction must be cardinal.");
        }

        private static string ComputeDigest(
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access,
            OptionalRewardTierResult reward,
            OptionalReturnPolicySettings settings,
            IReadOnlyList<OptionalReturnPolicyAssignment> assignments,
            OptionalReturnPolicyDiagnostics diagnostics)
        {
            var builder = new StringBuilder();
            Append(builder, "type0", type0.CanonicalDigest);
            Append(builder, "access", access.CanonicalDigest);
            Append(builder, "reward", reward.CanonicalDigest);
            Append(builder, "growth", type0.SourceGrowthDigest);
            Append(builder, "maximumBacktrackSectorCount", settings.MaximumBacktrackSectorCount);
            Append(builder, "requireAllCellsReturnable", settings.RequireAllCellsReturnable ? 1 : 0);
            foreach (var assignment in assignments.OrderBy(value => value.RegionId))
            {
                Append(builder, "region", assignment.RegionId.ToString());
                Append(builder, "ordinal", assignment.RegionOrdinal);
                Append(builder, "attachment", assignment.AttachmentOrder);
                Append(builder, "accessRule", (int)assignment.AccessRule);
                Append(builder, "rewardTier", (int)assignment.RewardTier);
                Append(builder, "returnPolicy", (int)assignment.ReturnPolicy);
                Append(builder, "criticalSector", assignment.CriticalSourceSectorIndex);
                Append(builder, "criticalDepth", assignment.CriticalSourceDepth.Value);
                Append(builder, "entry", assignment.AttachmentEntrySectorIndex);
                Append(builder, "destination", assignment.ReturnDestinationMandatorySectorIndex);
                foreach (var sectorIndex in assignment.CriticalReturnPathSectorIndices)
                    Append(builder, "path", sectorIndex);
                Append(builder, "pathEdges", assignment.CriticalReturnEdgeCount);
                Append(builder, "returnable", assignment.ReturnableCellCount);
                Append(builder, "sameBoundary", assignment.UsesSameOpenedAttachmentBoundary ? 1 : 0);
                Append(builder, "device", assignment.RequiresReturnDevice ? 1 : 0);
            }
            AppendDiagnostics(builder, diagnostics);
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static void AppendDiagnostics(StringBuilder builder, OptionalReturnPolicyDiagnostics value)
        {
            Append(builder, "sourceRegions", value.SourceRegionCount);
            Append(builder, "sourceType0", value.SourceType0CellAssignmentCount);
            Append(builder, "sourceAccess", value.SourceAccessAssignmentCount);
            Append(builder, "sourceReward", value.SourceRewardTierAssignmentCount);
            Append(builder, "assignments", value.AssignmentCount);
            Append(builder, "backtrack", value.BacktrackCount);
            Append(builder, "returnGate", value.ReturnGateCount);
            Append(builder, "safeExit", value.SafeExitCount);
            Append(builder, "returnable", value.ReturnableCellCount);
            Append(builder, "nonReturnable", value.NonReturnableCellCount);
            Append(builder, "internalEdges", value.InternalUndirectedBaseEdgeCount);
            Append(builder, "witnessSectors", value.CriticalWitnessSectorCountTotal);
            Append(builder, "witnessEdges", value.CriticalWitnessEdgeCountTotal);
            Append(builder, "maximumWitness", value.MaximumCriticalWitnessSectorCount);
            Append(builder, "sameBoundary", value.SameOpenedAttachmentReturnCount);
            Append(builder, "devices", value.ReturnDeviceReservationCount);
            Append(builder, "extraExit", value.ExtraSafeExitReservationCount);
            Append(builder, "attachmentBaseOpen", value.AttachmentBoundaryBaseOpenCount);
            Append(builder, "rng", value.RngDrawCount);
            Append(builder, "mutation", value.SourceMutationCount);
        }

        private static void Append(StringBuilder builder, string name, object value)
        {
            builder.Append(name).Append('=').Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append('\n');
        }

        private static OptionalReturnPolicyResult Failure(
            OptionalReturnPolicyResolutionStatus status,
            OptionalReturnPolicyResolutionErrorCode code,
            string sourceField,
            string message,
            Type0RouteMaskAssignmentResult type0 = null,
            OptionalAccessAssignmentResult access = null,
            OptionalRewardTierResult reward = null)
        {
            return FailureForRegion(status, code, default(OptionalRegionId), -1, -1,
                sourceField, message, type0, access, reward);
        }

        private static OptionalReturnPolicyResult FailureForRegion(
            OptionalReturnPolicyResolutionStatus status,
            OptionalReturnPolicyResolutionErrorCode code,
            OptionalRegionId regionId,
            int sectorIndex,
            int attachmentOrder,
            string sourceField,
            string message,
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access,
            OptionalRewardTierResult reward,
            int attachmentBoundaryBaseOpenCount = 0,
            int returnableCellCount = 0,
            int nonReturnableCellCount = 0,
            int internalUndirectedBaseEdgeCount = 0)
        {
            var diagnostics = new OptionalReturnPolicyDiagnostics(
                type0?.SourceSnapshot?.Regions.Count ?? 0,
                type0?.Assignments.Count ?? 0,
                access?.Assignments.Count ?? 0,
                reward?.Assignments.Count ?? 0,
                0, 0, 0, 0,
                returnableCellCount,
                nonReturnableCellCount,
                internalUndirectedBaseEdgeCount,
                0, 0, 0, 0, 0, 0,
                attachmentBoundaryBaseOpenCount,
                0, 0);
            return new OptionalReturnPolicyResult(
                status,
                Array.Empty<OptionalReturnPolicyAssignment>(),
                diagnostics,
                new[] { new OptionalReturnPolicyResolutionError(code, regionId, sectorIndex, attachmentOrder, sourceField, message) },
                type0?.CanonicalDigest ?? string.Empty,
                access?.CanonicalDigest ?? string.Empty,
                reward?.CanonicalDigest ?? string.Empty,
                type0?.SourceGrowthDigest ?? string.Empty,
                string.Empty);
        }

        private readonly struct Direction
        {
            public Direction(int dx, int dy)
            {
                Dx = dx;
                Dy = dy;
            }

            public int Dx { get; }
            public int Dy { get; }
        }
    }
}
