using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class OptionalRegionOverlayBuilder
    {
        public OptionalRegionOverlaySnapshot Build(
            GeneratedWorldData world,
            OptionalRegionSnapshot optionalRegions,
            Type0RouteMaskAssignmentResult type0Assignments,
            OptionalAccessAssignmentResult accessAssignments,
            OptionalRewardTierResult rewardTiers,
            OptionalReturnPolicyResult returnPolicies,
            InactiveBufferAssignmentResult inactiveBuffers,
            OptionalRegionValidationReport validationReport,
            OptionalRegionOverlaySettings settings)
        {
            if (world == null || optionalRegions == null || type0Assignments == null ||
                accessAssignments == null || rewardTiers == null || returnPolicies == null ||
                inactiveBuffers == null || validationReport == null || settings == null)
                return OptionalRegionOverlaySnapshot.Failure(OptionalRegionOverlayStatus.InvalidInput);
            if (!settings.IsApproved)
                return OptionalRegionOverlaySnapshot.Failure(OptionalRegionOverlayStatus.InvalidSettings);
            if (!validationReport.IsValid)
                return OptionalRegionOverlaySnapshot.Failure(OptionalRegionOverlayStatus.InvalidValidationReport);
            if (!SourcesAreApproved(world, optionalRegions, type0Assignments, accessAssignments,
                    rewardTiers, returnPolicies, inactiveBuffers, validationReport))
                return OptionalRegionOverlaySnapshot.Failure(OptionalRegionOverlayStatus.InvalidSource);

            try
            {
                var cells = BuildCells(world, optionalRegions, type0Assignments, accessAssignments,
                    rewardTiers, returnPolicies, inactiveBuffers);
                var connections = BuildConnections(accessAssignments, returnPolicies);
                var legend = BuildLegend();
                if (!HasApprovedCounts(cells, connections, legend))
                    return OptionalRegionOverlaySnapshot.Failure(OptionalRegionOverlayStatus.InvalidSource);

                var digest = ComputeDigest(settings, validationReport, inactiveBuffers, cells, connections, legend);
                return new OptionalRegionOverlaySnapshot(
                    OptionalRegionOverlayStatus.Completed,
                    cells,
                    connections,
                    legend,
                    validationReport.CanonicalDigest,
                    inactiveBuffers.CanonicalDigest,
                    digest,
                    0);
            }
            catch (ArgumentException)
            {
                return OptionalRegionOverlaySnapshot.Failure(OptionalRegionOverlayStatus.InvalidSource);
            }
        }

        private static bool SourcesAreApproved(
            GeneratedWorldData world,
            OptionalRegionSnapshot regions,
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access,
            OptionalRewardTierResult reward,
            OptionalReturnPolicyResult returns,
            InactiveBufferAssignmentResult inactive,
            OptionalRegionValidationReport report)
        {
            var d = report.Diagnostics;
            return world.Cells.Count == 169 && ReferenceEquals(type0.SourceSnapshot, regions) &&
                   type0.IsSuccess && access.IsSuccess && reward.IsSuccess && returns.IsSuccess && inactive.IsSuccess &&
                   regions.Regions.Count == 12 && regions.Cells.Count == 39 && type0.Assignments.Count == 39 &&
                   access.Assignments.Count == 12 && access.Clues.Count == 12 && reward.Assignments.Count == 12 &&
                   returns.Assignments.Count == 12 && inactive.Assignments.Count == 78 &&
                   type0.Diagnostics.AttachmentBoundaryClosedCount == 12 &&
                   type0.Diagnostics.MandatoryBoundaryBaseOpenCount == 0 &&
                   type0.Diagnostics.HorizontalThroughCount == 0 &&
                   returns.Diagnostics.CriticalWitnessEdgeCountTotal == 19 &&
                   returns.Diagnostics.ReturnableCellCount == 39 && returns.Diagnostics.NonReturnableCellCount == 0 &&
                   reward.Diagnostics.MandatoryRewardSelectionCount == 0 &&
                   inactive.Diagnostics.ProtectedUnionCount == 91 &&
                   inactive.Diagnostics.DecorativeBoundaryCount == 52 && inactive.Diagnostics.InteriorInactiveCount == 26 &&
                   d.WorldSectorCount == 169 && d.MandatoryRouteCellCount == 47 && d.OptionalRegionCount == 12 &&
                   d.Type0CellCount == 39 && d.IssueCount == 0 && d.RngDrawCount == 0 && d.SourceMutationCount == 0 &&
                   string.Equals(report.SourceMandatoryGraphDigest, regions.SourceMandatoryGraphDigest, StringComparison.Ordinal) &&
                   string.Equals(report.SourceGrowthDigest, type0.SourceGrowthDigest, StringComparison.Ordinal) &&
                   string.Equals(report.SourceType0AssignmentDigest, type0.CanonicalDigest, StringComparison.Ordinal) &&
                   string.Equals(report.SourceAccessAssignmentDigest, access.CanonicalDigest, StringComparison.Ordinal) &&
                   string.Equals(report.SourceRewardTierDigest, reward.CanonicalDigest, StringComparison.Ordinal) &&
                   string.Equals(report.SourceReturnPolicyDigest, returns.CanonicalDigest, StringComparison.Ordinal) &&
                   string.Equals(report.SourceInactiveAssignmentDigest, inactive.CanonicalDigest, StringComparison.Ordinal);
        }

        private static List<OptionalRegionOverlayCell> BuildCells(
            GeneratedWorldData world,
            OptionalRegionSnapshot regions,
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access,
            OptionalRewardTierResult reward,
            OptionalReturnPolicyResult returns,
            InactiveBufferAssignmentResult inactive)
        {
            var mandatory = new HashSet<int>(regions.MandatoryRouteSectorIndices);
            var reservedAdapters = new HashSet<int>(new[] { 0, 28, 106 });
            var type0BySector = type0.Assignments.ToDictionary(value => value.SectorIndex);
            var inactiveBySector = inactive.Assignments.ToDictionary(value => value.SectorIndex);
            var accessByRegion = access.Assignments.ToDictionary(value => value.RegionId);
            var rewardByRegion = reward.Assignments.ToDictionary(value => value.RegionId);
            var returnByRegion = returns.Assignments.ToDictionary(value => value.RegionId);
            var values = new List<OptionalRegionOverlayCell>(169);

            for (var index = 0; index < 169; index++)
            {
                var reserved = reservedAdapters.Contains(index) ||
                               (!mandatory.Contains(index) && !type0BySector.ContainsKey(index) &&
                                !inactiveBySector.ContainsKey(index));
                if (reserved)
                {
                    values.Add(CreateRoleCell(index, OptionalRegionOverlayCellKind.ReservedSite,
                        OptionalRegionOverlayColorToken.ReservedSite, mandatory.Contains(index) ? "R*" : "R"));
                    continue;
                }
                if (mandatory.Contains(index))
                {
                    values.Add(CreateRoleCell(index, OptionalRegionOverlayCellKind.Mandatory,
                        OptionalRegionOverlayColorToken.Mandatory, "M"));
                    continue;
                }
                if (type0BySector.TryGetValue(index, out var mask))
                {
                    if (mask.OpenMask.OpenLeft && mask.OpenMask.OpenRight) throw new ArgumentException("Type0 L+R is forbidden.");
                    var accessValue = accessByRegion[mask.RegionId];
                    var rewardValue = rewardByRegion[mask.RegionId];
                    var returnValue = returnByRegion[mask.RegionId];
                    var layers = new List<OptionalRegionOverlayLayer>
                    {
                        OptionalRegionOverlayLayer.BaseRole,
                        OptionalRegionOverlayLayer.AccessRule,
                        OptionalRegionOverlayLayer.Depth,
                        OptionalRegionOverlayLayer.ReturnWitness,
                        OptionalRegionOverlayLayer.RewardTier
                    };
                    if (mask.IsAttachmentCell) layers.Add(OptionalRegionOverlayLayer.AttachmentContact);
                    values.Add(new OptionalRegionOverlayCell(
                        index,
                        mask.Sector,
                        OptionalRegionOverlayCellKind.Type0,
                        mask.RegionId,
                        mask.Depth.Value,
                        accessValue.AccessRule,
                        rewardValue.RewardTier,
                        returnValue.ReturnPolicy,
                        default(InactiveBufferKind),
                        AccessColor(accessValue.AccessRule),
                        mask.Depth.Value.ToString(CultureInfo.InvariantCulture),
                        layers));
                    continue;
                }
                if (!inactiveBySector.TryGetValue(index, out var inactiveValue))
                    throw new ArgumentException("Every sector must have exactly one overlay role.");
                var decorative = inactiveValue.Kind == InactiveBufferKind.DecorativeBoundary;
                values.Add(new OptionalRegionOverlayCell(
                    index,
                    inactiveValue.Coord,
                    decorative ? OptionalRegionOverlayCellKind.InactiveDecorative : OptionalRegionOverlayCellKind.InactiveInterior,
                    default(OptionalRegionId),
                    0,
                    default(OptionalRegionAccessRule),
                    OptionalRewardTier.None,
                    default(OptionalReturnPolicy),
                    inactiveValue.Kind,
                    decorative ? OptionalRegionOverlayColorToken.InactiveDecorative : OptionalRegionOverlayColorToken.InactiveInterior,
                    decorative ? "D" : "I",
                    new[] { OptionalRegionOverlayLayer.BaseRole, OptionalRegionOverlayLayer.InactiveKind }));
            }
            return values;
        }

        private static OptionalRegionOverlayCell CreateRoleCell(
            int index,
            OptionalRegionOverlayCellKind kind,
            OptionalRegionOverlayColorToken color,
            string label)
        {
            return new OptionalRegionOverlayCell(
                index,
                WorldGridIndex.ToCoordinate(index),
                kind,
                default(OptionalRegionId),
                0,
                default(OptionalRegionAccessRule),
                OptionalRewardTier.None,
                default(OptionalReturnPolicy),
                default(InactiveBufferKind),
                color,
                label,
                new[] { OptionalRegionOverlayLayer.BaseRole });
        }

        private static List<OptionalRegionOverlayConnection> BuildConnections(
            OptionalAccessAssignmentResult access,
            OptionalReturnPolicyResult returns)
        {
            var values = new List<OptionalRegionOverlayConnection>();
            var returnByRegion = returns.Assignments.ToDictionary(value => value.RegionId);
            foreach (var assignment in access.Assignments.OrderBy(value => value.RegionId))
            {
                var policy = returnByRegion[assignment.RegionId];
                values.Add(new OptionalRegionOverlayConnection(
                    OptionalRegionOverlayConnectionKind.AttachmentContact,
                    assignment.RegionId,
                    assignment.MandatoryRouteSectorIndex,
                    assignment.EntrySectorIndex,
                    "A",
                    assignment.AccessRule,
                    policy.ReturnPolicy));
            }
            foreach (var assignment in returns.Assignments.OrderBy(value => value.RegionId))
            {
                var path = assignment.CriticalReturnPathSectorIndices;
                for (var index = 0; index + 1 < path.Count; index++)
                {
                    values.Add(new OptionalRegionOverlayConnection(
                        OptionalRegionOverlayConnectionKind.ReturnWitness,
                        assignment.RegionId,
                        path[index],
                        path[index + 1],
                        "R",
                        assignment.AccessRule,
                        assignment.ReturnPolicy));
                }
            }
            return values;
        }

        private static List<OptionalRegionOverlayLegendEntry> BuildLegend()
        {
            var entries = new List<OptionalRegionOverlayLegendEntry>();
            AddLegend(entries, OptionalRegionOverlayLayer.AccessRule, OptionalRegionOverlayColorToken.Type0Basic, "Basic");
            AddLegend(entries, OptionalRegionOverlayLayer.AccessRule, OptionalRegionOverlayColorToken.Type0Tool, "Tool");
            AddLegend(entries, OptionalRegionOverlayLayer.AccessRule, OptionalRegionOverlayColorToken.Type0Environment, "Environment");
            AddLegend(entries, OptionalRegionOverlayLayer.AccessRule, OptionalRegionOverlayColorToken.Type0Explosive, "Explosive");
            AddLegend(entries, OptionalRegionOverlayLayer.AccessRule, OptionalRegionOverlayColorToken.Type0Hidden, "Hidden");
            AddLegend(entries, OptionalRegionOverlayLayer.RewardTier, OptionalRegionOverlayColorToken.RewardLow, "Reward Low");
            AddLegend(entries, OptionalRegionOverlayLayer.RewardTier, OptionalRegionOverlayColorToken.RewardMedium, "Reward Medium");
            AddLegend(entries, OptionalRegionOverlayLayer.RewardTier, OptionalRegionOverlayColorToken.RewardHigh, "Reward High");
            AddLegend(entries, OptionalRegionOverlayLayer.RewardTier, OptionalRegionOverlayColorToken.RewardUnique, "Reward Unique");
            AddLegend(entries, OptionalRegionOverlayLayer.ReturnWitness, OptionalRegionOverlayColorToken.ReturnBacktrack, "Return Backtrack");
            AddLegend(entries, OptionalRegionOverlayLayer.InactiveKind, OptionalRegionOverlayColorToken.InactiveInterior, "Inactive Interior");
            AddLegend(entries, OptionalRegionOverlayLayer.InactiveKind, OptionalRegionOverlayColorToken.InactiveDecorative, "Inactive Decorative");
            AddLegend(entries, OptionalRegionOverlayLayer.BaseRole, OptionalRegionOverlayColorToken.Mandatory, "Mandatory");
            AddLegend(entries, OptionalRegionOverlayLayer.BaseRole, OptionalRegionOverlayColorToken.ReservedSite, "Reserved Site");
            AddLegend(entries, OptionalRegionOverlayLayer.ValidationIssue, OptionalRegionOverlayColorToken.ValidationIssue, "Validation Issue");
            return entries;
        }

        private static void AddLegend(
            List<OptionalRegionOverlayLegendEntry> values,
            OptionalRegionOverlayLayer layer,
            OptionalRegionOverlayColorToken color,
            string label)
        {
            values.Add(new OptionalRegionOverlayLegendEntry(values.Count, layer, color, label));
        }

        private static bool HasApprovedCounts(
            List<OptionalRegionOverlayCell> cells,
            List<OptionalRegionOverlayConnection> connections,
            List<OptionalRegionOverlayLegendEntry> legend)
        {
            return cells.Count == 169 &&
                   cells.Count(value => value.Kind == OptionalRegionOverlayCellKind.Mandatory) == 44 &&
                   cells.Count(value => value.Kind == OptionalRegionOverlayCellKind.ReservedSite) == 8 &&
                   cells.Count(value => value.Kind == OptionalRegionOverlayCellKind.Type0) == 39 &&
                   cells.Count(value => value.Kind == OptionalRegionOverlayCellKind.InactiveInterior) == 26 &&
                   cells.Count(value => value.Kind == OptionalRegionOverlayCellKind.InactiveDecorative) == 52 &&
                   connections.Count(value => value.Kind == OptionalRegionOverlayConnectionKind.AttachmentContact) == 12 &&
                   connections.Count(value => value.Kind == OptionalRegionOverlayConnectionKind.ReturnWitness) == 19 &&
                   legend.Count == 15;
        }

        private static OptionalRegionOverlayColorToken AccessColor(OptionalRegionAccessRule value)
        {
            switch (value)
            {
                case OptionalRegionAccessRule.Basic: return OptionalRegionOverlayColorToken.Type0Basic;
                case OptionalRegionAccessRule.Tool: return OptionalRegionOverlayColorToken.Type0Tool;
                case OptionalRegionAccessRule.Environment: return OptionalRegionOverlayColorToken.Type0Environment;
                case OptionalRegionAccessRule.Explosive: return OptionalRegionOverlayColorToken.Type0Explosive;
                case OptionalRegionAccessRule.Hidden: return OptionalRegionOverlayColorToken.Type0Hidden;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static string ComputeDigest(
            OptionalRegionOverlaySettings settings,
            OptionalRegionValidationReport report,
            InactiveBufferAssignmentResult inactive,
            IEnumerable<OptionalRegionOverlayCell> cells,
            IEnumerable<OptionalRegionOverlayConnection> connections,
            IEnumerable<OptionalRegionOverlayLegendEntry> legend)
        {
            var text = new StringBuilder();
            text.Append("MAP06_OPTIONAL_OVERLAY_V1|")
                .Append(settings.ShowAccessRuleColors ? '1' : '0')
                .Append(settings.ShowDepthLabels ? '1' : '0')
                .Append(settings.ShowAttachmentContacts ? '1' : '0')
                .Append(settings.ShowReturnWitness ? '1' : '0')
                .Append(settings.ShowRewardTierMarkers ? '1' : '0')
                .Append(settings.ShowInactiveKinds ? '1' : '0')
                .Append(settings.ShowValidationIssues ? '1' : '0')
                .Append(settings.RequireValidReport ? '1' : '0').Append('|')
                .Append(report.CanonicalDigest).Append('|').Append(inactive.CanonicalDigest).Append('|')
                .Append(report.Diagnostics.WorldSectorCount).Append('|')
                .Append(report.Diagnostics.OptionalRegionCount).Append('|')
                .Append(report.Diagnostics.Type0CellCount).Append('|')
                .Append(report.Diagnostics.IssueCount).Append('|');
            foreach (var cell in cells)
            {
                text.Append(cell.SectorIndex).Append(':').Append((int)cell.Kind).Append(':')
                    .Append(cell.RegionId.Value).Append(':').Append(cell.Depth).Append(':')
                    .Append((int)cell.AccessRule).Append(':').Append((int)cell.RewardTier).Append(':')
                    .Append((int)cell.ReturnPolicy).Append(':').Append((int)cell.InactiveKind).Append(':')
                    .Append((int)cell.ColorToken).Append(':').Append(cell.Label).Append(':')
                    .Append(string.Join(",", cell.Layers.Select(value => ((int)value).ToString(CultureInfo.InvariantCulture))))
                    .Append('|');
            }
            foreach (var connection in connections)
            {
                text.Append((int)connection.Kind).Append(':').Append(connection.RegionId.Value).Append(':')
                    .Append(connection.FromSectorIndex).Append(':').Append(connection.ToSectorIndex).Append(':')
                    .Append(connection.Label).Append(':').Append((int)connection.AccessRule).Append(':')
                    .Append((int)connection.ReturnPolicy).Append('|');
            }
            foreach (var entry in legend)
            {
                text.Append(entry.Order).Append(':').Append((int)entry.Layer).Append(':')
                    .Append((int)entry.ColorToken).Append(':').Append(entry.Label).Append('|');
            }
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var result = new StringBuilder(64);
                foreach (var value in bytes) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
