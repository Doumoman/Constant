using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionValidator
    {
        private const int MandatoryNodeCount = 47;
        private const int MandatoryDirectedEdgeCount = 96;
        private const int MandatoryUndirectedEdgeCount = 48;
        private const int MandatoryRouteCellCount = 47;

        public OptionalRegionValidationReport Validate(
            GeneratedWorldData world,
            SiteReservationSnapshot siteReservations,
            BiomePatchValidationPublication biomePublication,
            MandatoryRouteGraph graph,
            MandatoryRouteValidationReport mandatoryValidation,
            OptionalRegionSnapshot optionalRegions,
            Type0RouteMaskAssignmentResult type0Assignments,
            OptionalAccessAssignmentResult accessAssignments,
            OptionalRewardTierResult rewardTiers,
            OptionalReturnPolicyResult returnPolicies,
            InactiveBufferAssignmentResult inactiveBuffers,
            OptionalRegionValidationSettings settings)
        {
            var issues = new List<OptionalRegionValidationIssue>();
            AddNull(issues, world, "world");
            AddNull(issues, siteReservations, "siteReservations");
            AddNull(issues, biomePublication, "biomePublication");
            AddNull(issues, graph, "graph");
            AddNull(issues, mandatoryValidation, "mandatoryValidation");
            AddNull(issues, optionalRegions, "optionalRegions");
            AddNull(issues, type0Assignments, "type0Assignments");
            AddNull(issues, accessAssignments, "accessAssignments");
            AddNull(issues, rewardTiers, "rewardTiers");
            AddNull(issues, returnPolicies, "returnPolicies");
            AddNull(issues, inactiveBuffers, "inactiveBuffers");
            AddNull(issues, settings, "settings");
            if (issues.Count != 0)
                return CreateReport(OptionalRegionValidationStatus.InvalidInput, issues, world, graph, optionalRegions,
                    type0Assignments, accessAssignments, rewardTiers, returnPolicies, inactiveBuffers, settings,
                    0, 0, 0, 0);

            if (!settings.IsApproved)
            {
                AddFalseSettings(issues, settings);
                return CreateReport(OptionalRegionValidationStatus.InvalidSettings, issues, world, graph, optionalRegions,
                    type0Assignments, accessAssignments, rewardTiers, returnPolicies, inactiveBuffers, settings,
                    0, 0, 0, 0);
            }

            ValidateWorldAndSources(issues, world, siteReservations, biomePublication, graph, mandatoryValidation);
            ValidateStatusesAndDigests(issues, optionalRegions, type0Assignments, accessAssignments,
                rewardTiers, returnPolicies, inactiveBuffers);

            var regionMap = BuildRegionMap(issues, optionalRegions);
            var optionalCellMap = BuildOptionalCellMap(issues, optionalRegions, regionMap);
            var type0Map = BuildType0Map(issues, type0Assignments);
            var accessMap = BuildAccessMap(issues, accessAssignments);
            var rewardMap = BuildRewardMap(issues, rewardTiers);
            var returnMap = BuildReturnMap(issues, returnPolicies);
            ValidateRegionIdentity(issues, regionMap, optionalCellMap, type0Map, accessMap, rewardMap, returnMap,
                accessAssignments, returnPolicies);

            var type0LeftRightOpenCount = ValidateType0Rules(issues, optionalRegions, type0Assignments);
            var missingClueCount = ValidateAccessAndRewardRules(issues, regionMap, accessMap, rewardMap,
                accessAssignments, rewardTiers);
            var missingReturnPolicyCount = ValidateReturnRules(issues, regionMap, returnMap, returnPolicies);
            var openEdgeToInactiveCount = ValidateInactiveRules(issues, world, siteReservations, graph,
                optionalCellMap, type0Assignments, rewardMap, inactiveBuffers);

            var rngDrawCount = type0Assignments.Diagnostics.RngDrawCount + accessAssignments.Diagnostics.RngDrawCount +
                               rewardTiers.Diagnostics.RngDrawCount + returnPolicies.Diagnostics.RngDrawCount +
                               inactiveBuffers.Diagnostics.RngDrawCount;
            var sourceMutationCount = type0Assignments.Diagnostics.SourceMutationCount +
                                      accessAssignments.Diagnostics.SourceMutationCount +
                                      rewardTiers.Diagnostics.SourceMutationCount +
                                      returnPolicies.Diagnostics.SourceMutationCount +
                                      inactiveBuffers.Diagnostics.SourceMutationCount;
            if (rngDrawCount != 0)
                Add(issues, OptionalRegionValidationIssueCode.RngConsumed, default(OptionalRegionId), -1,
                    "SourceChain", "RngDrawCount", "Optional validation requires zero RNG consumption.");
            if (sourceMutationCount != 0)
                Add(issues, OptionalRegionValidationIssueCode.SourceMutation, default(OptionalRegionId), -1,
                    "SourceChain", "SourceMutationCount", "Optional validation requires zero source mutation.");

            var normalized = Normalize(issues);
            var status = SelectStatus(normalized);
            var reportRng = status == OptionalRegionValidationStatus.Valid ? rngDrawCount : 0;
            var reportMutation = status == OptionalRegionValidationStatus.Valid ? sourceMutationCount : 0;
            return CreateReport(status, normalized, world, graph, optionalRegions, type0Assignments,
                accessAssignments, rewardTiers, returnPolicies, inactiveBuffers, settings,
                type0LeftRightOpenCount, missingClueCount, missingReturnPolicyCount,
                openEdgeToInactiveCount, reportRng, reportMutation);
        }

        private static void ValidateWorldAndSources(
            ICollection<OptionalRegionValidationIssue> issues,
            GeneratedWorldData world,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome,
            MandatoryRouteGraph graph,
            MandatoryRouteValidationReport report)
        {
            if (WorldGenConstants.SectorColumns != 13 || WorldGenConstants.SectorRows != 13 ||
                WorldGenConstants.SectorCount != 169 || world.Cells.Count != WorldGenConstants.SectorCount)
                Add(issues, OptionalRegionValidationIssueCode.InvalidWorld, default(OptionalRegionId), -1,
                    "World", "Dimensions", "World must be the exact 13x13, 169-sector publication.");

            var seen = new HashSet<int>();
            for (var ordinal = 0; ordinal < world.Cells.Count; ordinal++)
            {
                var cell = world.Cells[ordinal];
                if (cell == null || cell.Index != ordinal || cell.Coordinate != WorldGridIndex.ToCoordinate(ordinal))
                    Add(issues, OptionalRegionValidationIssueCode.InvalidWorld, default(OptionalRegionId),
                        cell == null ? -1 : cell.Index, "World", "Cells",
                        "World cells must preserve unique row-major index and coordinate identity.");
                else if (!seen.Add(cell.Index))
                    Add(issues, OptionalRegionValidationIssueCode.DuplicateSector, default(OptionalRegionId),
                        cell.Index, "World", "Cells", "World sector indices must be unique.");
            }

            if (site.Sectors.Count != WorldGenConstants.SectorCount ||
                biome.WorldWithBiomeAssignments == null ||
                biome.WorldWithBiomeAssignments.Cells.Count != WorldGenConstants.SectorCount)
                Add(issues, OptionalRegionValidationIssueCode.SourceMismatch, default(OptionalRegionId), -1,
                    "P00-P02", "WorldSectorCount", "Site and biome publications must cover the full world.");

            if (!ReferenceEquals(graph.RouteStampedWorld, world) ||
                !ReferenceEquals(graph.SourceTerminalSet.SourceSiteSnapshot, site) ||
                !ReferenceEquals(graph.SourceTerminalSet.SourceBiomePublication, biome) ||
                !ReferenceEquals(report.SourceGraph, graph) ||
                !ReferenceEquals(report.SourceWorld, world))
                Add(issues, OptionalRegionValidationIssueCode.SourceMismatch, default(OptionalRegionId), -1,
                    "Mandatory", "SourceIdentity", "Mandatory graph and validation report must preserve source object identity.");

            if (!report.IsValid)
                Add(issues, OptionalRegionValidationIssueCode.InvalidMandatoryGraph, default(OptionalRegionId), -1,
                    "MandatoryValidation", "IsValid", "Mandatory validation must be an approved valid publication.");
            if (graph.NodeCount != MandatoryNodeCount || graph.DirectedEdgeCount != MandatoryDirectedEdgeCount ||
                graph.UndirectedEdgeCount != MandatoryUndirectedEdgeCount || graph.CellCount != MandatoryRouteCellCount)
                Add(issues, OptionalRegionValidationIssueCode.InvalidMandatoryGraph, default(OptionalRegionId), -1,
                    "MandatoryGraph", "Identity", "Mandatory graph identity must be 47/96/48/47.");
        }

        private static void ValidateStatusesAndDigests(
            ICollection<OptionalRegionValidationIssue> issues,
            OptionalRegionSnapshot optionalRegions,
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access,
            OptionalRewardTierResult reward,
            OptionalReturnPolicyResult returns,
            InactiveBufferAssignmentResult inactive)
        {
            if (type0.Status != Type0RouteMaskAssignmentStatus.Completed)
                InvalidStatus(issues, "Type0", type0.Status.ToString());
            if (access.Status != OptionalAccessAssignmentStatus.Completed)
                InvalidStatus(issues, "Access", access.Status.ToString());
            if (reward.Status != OptionalRewardTierCalculationStatus.Completed)
                InvalidStatus(issues, "Reward", reward.Status.ToString());
            if (returns.Status != OptionalReturnPolicyResolutionStatus.Completed)
                InvalidStatus(issues, "Return", returns.Status.ToString());
            if (inactive.Status != InactiveBufferAssignmentStatus.Completed)
                InvalidStatus(issues, "Inactive", inactive.Status.ToString());

            var mandatoryDigest = optionalRegions.SourceMandatoryGraphDigest;
            var expectedMandatoryDigest = string.Format(CultureInfo.InvariantCulture, "MAP05_GRAPH_{0}_{1}_{2}_{3}",
                MandatoryNodeCount, MandatoryDirectedEdgeCount, MandatoryUndirectedEdgeCount, MandatoryRouteCellCount);
            CheckIdentity(issues, mandatoryDigest, expectedMandatoryDigest, "OptionalRegions", "SourceMandatoryGraphDigest");
            CheckDigest(issues, type0.SourceGrowthDigest, "Type0", "SourceGrowthDigest");
            CheckDigest(issues, type0.SourceRouteMaskCatalogDigest, "Type0", "SourceRouteMaskCatalogDigest");
            CheckDigest(issues, type0.CanonicalDigest, "Type0", "CanonicalDigest");
            CheckDigest(issues, access.CanonicalDigest, "Access", "CanonicalDigest");
            CheckDigest(issues, reward.CanonicalDigest, "Reward", "CanonicalDigest");
            CheckDigest(issues, returns.CanonicalDigest, "Return", "CanonicalDigest");
            CheckDigest(issues, inactive.CanonicalDigest, "Inactive", "CanonicalDigest");

            if (!ReferenceEquals(type0.SourceSnapshot, optionalRegions))
                SourceMismatch(issues, "Type0", "SourceSnapshot");
            CheckChain(issues, access.SourceType0AssignmentDigest, type0.CanonicalDigest, "Access", "SourceType0AssignmentDigest");
            CheckChain(issues, access.SourceGrowthDigest, type0.SourceGrowthDigest, "Access", "SourceGrowthDigest");
            CheckChain(issues, reward.SourceType0AssignmentDigest, type0.CanonicalDigest, "Reward", "SourceType0AssignmentDigest");
            CheckChain(issues, reward.SourceAccessAssignmentDigest, access.CanonicalDigest, "Reward", "SourceAccessAssignmentDigest");
            CheckChain(issues, reward.SourceGrowthDigest, type0.SourceGrowthDigest, "Reward", "SourceGrowthDigest");
            CheckChain(issues, returns.SourceType0AssignmentDigest, type0.CanonicalDigest, "Return", "SourceType0AssignmentDigest");
            CheckChain(issues, returns.SourceAccessAssignmentDigest, access.CanonicalDigest, "Return", "SourceAccessAssignmentDigest");
            CheckChain(issues, returns.SourceRewardTierDigest, reward.CanonicalDigest, "Return", "SourceRewardTierDigest");
            CheckChain(issues, returns.SourceGrowthDigest, type0.SourceGrowthDigest, "Return", "SourceGrowthDigest");
            CheckChain(issues, inactive.SourceMandatoryGraphDigest, mandatoryDigest, "Inactive", "SourceMandatoryGraphDigest");
            CheckChain(issues, inactive.SourceType0AssignmentDigest, type0.CanonicalDigest, "Inactive", "SourceType0AssignmentDigest");
            CheckChain(issues, inactive.SourceGrowthDigest, type0.SourceGrowthDigest, "Inactive", "SourceGrowthDigest");
            CheckChain(issues, inactive.SourceReturnPolicyDigest, returns.CanonicalDigest, "Inactive", "SourceReturnPolicyDigest");
        }

        private static Dictionary<OptionalRegionId, OptionalRegion> BuildRegionMap(
            ICollection<OptionalRegionValidationIssue> issues, OptionalRegionSnapshot snapshot)
        {
            var result = new Dictionary<OptionalRegionId, OptionalRegion>();
            foreach (var region in snapshot.Regions)
            {
                if (region == null || !region.RegionId.IsValid)
                {
                    Add(issues, OptionalRegionValidationIssueCode.InvalidOptionalRegionSnapshot,
                        default(OptionalRegionId), -1, "OptionalRegions", "Regions", "Regions must contain valid immutable entries.");
                    continue;
                }
                if (!result.TryAdd(region.RegionId, region))
                    Add(issues, OptionalRegionValidationIssueCode.DuplicateRegion, region.RegionId, -1,
                        "OptionalRegions", "RegionId", "Optional region IDs must be unique.");
            }
            if (snapshot.SourceMandatoryNodeCount != MandatoryNodeCount ||
                snapshot.SourceMandatoryDirectedEdgeCount != MandatoryDirectedEdgeCount ||
                snapshot.SourceMandatoryRouteCellCount != MandatoryRouteCellCount)
                Add(issues, OptionalRegionValidationIssueCode.InvalidOptionalRegionSnapshot,
                    default(OptionalRegionId), -1, "OptionalRegions", "MandatoryIdentity",
                    "Optional snapshot must preserve the mandatory graph identity.");
            return result;
        }

        private static Dictionary<int, OptionalRegionCell> BuildOptionalCellMap(
            ICollection<OptionalRegionValidationIssue> issues,
            OptionalRegionSnapshot snapshot,
            IDictionary<OptionalRegionId, OptionalRegion> regions)
        {
            var result = new Dictionary<int, OptionalRegionCell>();
            foreach (var cell in snapshot.Cells)
            {
                if (cell == null || !regions.ContainsKey(cell.RegionId))
                {
                    Add(issues, OptionalRegionValidationIssueCode.RegionIdentityMismatch,
                        cell == null ? default(OptionalRegionId) : cell.RegionId,
                        cell == null ? -1 : cell.SectorIndex, "OptionalRegions", "Cells",
                        "Every optional cell must belong to one published region.");
                    continue;
                }
                if (!result.TryAdd(cell.SectorIndex, cell))
                    Add(issues, OptionalRegionValidationIssueCode.DuplicateSector, cell.RegionId, cell.SectorIndex,
                        "OptionalRegions", "SectorIndex", "Optional sector ownership must be unique.");
            }
            return result;
        }

        private static Dictionary<int, Type0RouteMaskAssignment> BuildType0Map(
            ICollection<OptionalRegionValidationIssue> issues, Type0RouteMaskAssignmentResult source)
        {
            var result = new Dictionary<int, Type0RouteMaskAssignment>();
            foreach (var assignment in source.Assignments)
            {
                if (assignment == null || !result.TryAdd(assignment.SectorIndex, assignment))
                    Add(issues, OptionalRegionValidationIssueCode.DuplicateSector,
                        assignment == null ? default(OptionalRegionId) : assignment.RegionId,
                        assignment == null ? -1 : assignment.SectorIndex, "Type0", "SectorIndex",
                        "Type0 assignments must own unique sectors.");
            }
            return result;
        }

        private static Dictionary<OptionalRegionId, OptionalAccessAssignment> BuildAccessMap(
            ICollection<OptionalRegionValidationIssue> issues, OptionalAccessAssignmentResult source)
        {
            var result = new Dictionary<OptionalRegionId, OptionalAccessAssignment>();
            foreach (var assignment in source.Assignments)
            {
                if (assignment == null || !result.TryAdd(assignment.RegionId, assignment))
                    Add(issues, OptionalRegionValidationIssueCode.DuplicateRegion,
                        assignment == null ? default(OptionalRegionId) : assignment.RegionId, -1,
                        "Access", "RegionId", "Access assignments must be one-to-one by region.");
            }
            return result;
        }

        private static Dictionary<OptionalRegionId, OptionalRewardTierAssignment> BuildRewardMap(
            ICollection<OptionalRegionValidationIssue> issues, OptionalRewardTierResult source)
        {
            var result = new Dictionary<OptionalRegionId, OptionalRewardTierAssignment>();
            foreach (var assignment in source.Assignments)
            {
                if (assignment == null || !result.TryAdd(assignment.RegionId, assignment))
                    Add(issues, OptionalRegionValidationIssueCode.DuplicateRegion,
                        assignment == null ? default(OptionalRegionId) : assignment.RegionId, -1,
                        "Reward", "RegionId", "Reward assignments must be one-to-one by region.");
            }
            return result;
        }

        private static Dictionary<OptionalRegionId, OptionalReturnPolicyAssignment> BuildReturnMap(
            ICollection<OptionalRegionValidationIssue> issues, OptionalReturnPolicyResult source)
        {
            var result = new Dictionary<OptionalRegionId, OptionalReturnPolicyAssignment>();
            foreach (var assignment in source.Assignments)
            {
                if (assignment == null || !result.TryAdd(assignment.RegionId, assignment))
                    Add(issues, OptionalRegionValidationIssueCode.DuplicateRegion,
                        assignment == null ? default(OptionalRegionId) : assignment.RegionId, -1,
                        "Return", "RegionId", "Return assignments must be one-to-one by region.");
            }
            return result;
        }

        private static void ValidateRegionIdentity(
            ICollection<OptionalRegionValidationIssue> issues,
            IDictionary<OptionalRegionId, OptionalRegion> regions,
            IDictionary<int, OptionalRegionCell> cells,
            IDictionary<int, Type0RouteMaskAssignment> type0,
            IDictionary<OptionalRegionId, OptionalAccessAssignment> access,
            IDictionary<OptionalRegionId, OptionalRewardTierAssignment> reward,
            IDictionary<OptionalRegionId, OptionalReturnPolicyAssignment> returns,
            OptionalAccessAssignmentResult accessResult,
            OptionalReturnPolicyResult returnResult)
        {
            foreach (var pair in regions.OrderBy(value => value.Key))
            {
                var region = pair.Value;
                if (!access.ContainsKey(pair.Key))
                    Add(issues, OptionalRegionValidationIssueCode.MissingAccessRule, pair.Key, -1,
                        "Access", "RegionId", "Every optional region requires one access assignment.");
                if (!reward.ContainsKey(pair.Key))
                    Add(issues, OptionalRegionValidationIssueCode.MissingRewardTier, pair.Key, -1,
                        "Reward", "RegionId", "Every optional region requires one reward tier assignment.");
                if (!returns.ContainsKey(pair.Key))
                    Add(issues, OptionalRegionValidationIssueCode.MissingReturnPolicy, pair.Key, -1,
                        "Return", "RegionId", "Every optional region requires one return assignment.");
                foreach (var cell in region.Cells)
                {
                    if (!cells.TryGetValue(cell.SectorIndex, out var published) ||
                        !ReferenceEquals(cell, published) ||
                        !type0.TryGetValue(cell.SectorIndex, out var mask) || mask.RegionId != pair.Key)
                        Add(issues, OptionalRegionValidationIssueCode.RegionIdentityMismatch, pair.Key,
                            cell.SectorIndex, "SourceChain", "RegionCell",
                            "Optional region and Type0 cell identities must match one-to-one.");
                }
            }

            foreach (var id in access.Keys.Concat(reward.Keys).Concat(returns.Keys).Distinct().OrderBy(value => value))
            {
                if (!regions.ContainsKey(id))
                    Add(issues, OptionalRegionValidationIssueCode.RegionIdentityMismatch, id, -1,
                        "SourceChain", "RegionId", "Downstream assignments must reference a published optional region.");
            }
            if (accessResult.Assignments.Count != regions.Count || accessResult.Clues.Count != regions.Count ||
                returnResult.Assignments.Count != regions.Count || type0.Count != cells.Count)
                Add(issues, OptionalRegionValidationIssueCode.RegionIdentityMismatch, default(OptionalRegionId), -1,
                    "SourceChain", "Counts", "Optional source-chain counts must preserve one-to-one identity.");
        }

        private static int ValidateType0Rules(
            ICollection<OptionalRegionValidationIssue> issues,
            OptionalRegionSnapshot snapshot,
            Type0RouteMaskAssignmentResult type0)
        {
            if (type0.Assignments.Count != snapshot.Cells.Count ||
                type0.Diagnostics.SourceRegionCount != snapshot.Regions.Count ||
                type0.Diagnostics.SourceCellCount != snapshot.Cells.Count ||
                type0.Diagnostics.AssignmentCount != type0.Assignments.Count ||
                type0.Diagnostics.AttachmentBoundaryClosedCount != snapshot.Regions.Count ||
                type0.Diagnostics.MandatoryBoundaryBaseOpenCount != 0)
                Add(issues, OptionalRegionValidationIssueCode.InvalidType0Assignment,
                    default(OptionalRegionId), -1, "Type0", "Diagnostics",
                    "Type0 diagnostics must preserve the optional snapshot and closed attachment boundary.");

            var count = 0;
            foreach (var assignment in type0.Assignments)
            {
                if (assignment.OpenMask.OpenLeft && assignment.OpenMask.OpenRight)
                {
                    count++;
                    Add(issues, OptionalRegionValidationIssueCode.Type0LeftRightOpen, assignment.RegionId,
                        assignment.SectorIndex, "Type0", "OpenMask",
                        "Type0 masks cannot open left and right simultaneously.");
                }
            }
            if (type0.Diagnostics.HorizontalThroughCount != count)
                Add(issues, OptionalRegionValidationIssueCode.InvalidType0Assignment,
                    default(OptionalRegionId), -1, "Type0", "HorizontalThroughCount",
                    "Type0 horizontal-through diagnostics must match the assignment facts.");
            return count;
        }

        private static int ValidateAccessAndRewardRules(
            ICollection<OptionalRegionValidationIssue> issues,
            IDictionary<OptionalRegionId, OptionalRegion> regions,
            IDictionary<OptionalRegionId, OptionalAccessAssignment> access,
            IDictionary<OptionalRegionId, OptionalRewardTierAssignment> reward,
            OptionalAccessAssignmentResult accessResult,
            OptionalRewardTierResult rewardResult)
        {
            var missingClues = 0;
            var clueSet = new HashSet<OptionalAccessClue>(accessResult.Clues);
            foreach (var pair in access.OrderBy(value => value.Key))
            {
                var assignment = pair.Value;
                if (!Enum.IsDefined(typeof(OptionalRegionAccessRule), assignment.AccessRule))
                    Add(issues, OptionalRegionValidationIssueCode.MissingAccessRule, pair.Key,
                        assignment.EntrySectorIndex, "Access", "AccessRule", "Access rule must be defined.");
                if (assignment.Clue == null || !clueSet.Contains(assignment.Clue))
                {
                    missingClues++;
                    Add(issues, OptionalRegionValidationIssueCode.MissingVisibleClue, pair.Key,
                        assignment.EntrySectorIndex, "Access", "Clue", "Every access assignment requires one visible clue.");
                }
            }
            if (accessResult.Diagnostics.AssignmentCount != accessResult.Assignments.Count ||
                accessResult.Diagnostics.ClueCount != accessResult.Clues.Count ||
                accessResult.Diagnostics.PerceptibleClueCount != accessResult.Clues.Count ||
                accessResult.Diagnostics.AttachmentBoundaryBaseOpenCount != 0)
                Add(issues, OptionalRegionValidationIssueCode.InvalidAccessAssignment,
                    default(OptionalRegionId), -1, "Access", "Diagnostics",
                    "Access diagnostics must publish one perceptible clue per region and no base opening.");

            if (rewardResult.Diagnostics.TierAssignmentCount != rewardResult.Assignments.Count ||
                rewardResult.Assignments.Count != regions.Count)
                Add(issues, OptionalRegionValidationIssueCode.InvalidRewardAssignment,
                    default(OptionalRegionId), -1, "Reward", "Diagnostics",
                    "Reward diagnostics must publish one tier per optional region.");
            if (rewardResult.Diagnostics.MandatoryRewardSelectionCount != 0)
                Add(issues, OptionalRegionValidationIssueCode.MandatoryRewardAssigned,
                    default(OptionalRegionId), -1, "Reward", "MandatoryRewardSelectionCount",
                    "Optional rewards cannot be assigned to mandatory route cells.");
            foreach (var pair in reward)
            {
                if (pair.Value.RewardTier == OptionalRewardTier.None)
                    Add(issues, OptionalRegionValidationIssueCode.MissingRewardTier, pair.Key, -1,
                        "Reward", "RewardTier", "Every optional region requires a non-none reward tier.");
            }
            return missingClues;
        }

        private static int ValidateReturnRules(
            ICollection<OptionalRegionValidationIssue> issues,
            IDictionary<OptionalRegionId, OptionalRegion> regions,
            IDictionary<OptionalRegionId, OptionalReturnPolicyAssignment> returns,
            OptionalReturnPolicyResult result)
        {
            var missing = 0;
            foreach (var pair in regions)
            {
                if (!returns.TryGetValue(pair.Key, out var assignment))
                {
                    missing++;
                    continue;
                }
                if (assignment.ReturnPolicy != OptionalReturnPolicy.BacktrackToAttachment ||
                    assignment.ReturnableCellCount != pair.Value.Cells.Count ||
                    !assignment.UsesSameOpenedAttachmentBoundary || assignment.RequiresReturnDevice)
                    Add(issues, OptionalRegionValidationIssueCode.NonReturnableOptionalCell, pair.Key,
                        assignment.CriticalSourceSectorIndex, "Return", "Returnability",
                        "All optional cells must return by the opened attachment boundary.");
            }
            if (result.Diagnostics.AssignmentCount != result.Assignments.Count ||
                result.Diagnostics.BacktrackCount != result.Assignments.Count ||
                result.Diagnostics.ReturnGateCount != 0 || result.Diagnostics.SafeExitCount != 0 ||
                result.Diagnostics.ReturnableCellCount != regions.Values.Sum(value => value.Cells.Count) ||
                result.Diagnostics.NonReturnableCellCount != 0 ||
                result.Diagnostics.ReturnDeviceReservationCount != 0 ||
                result.Diagnostics.ExtraSafeExitReservationCount != 0)
                Add(issues, OptionalRegionValidationIssueCode.InvalidReturnPolicy,
                    default(OptionalRegionId), -1, "Return", "Diagnostics",
                    "Return diagnostics must preserve complete backtrack returnability.");
            return missing;
        }

        private static int ValidateInactiveRules(
            ICollection<OptionalRegionValidationIssue> issues,
            GeneratedWorldData world,
            SiteReservationSnapshot site,
            MandatoryRouteGraph graph,
            IDictionary<int, OptionalRegionCell> optionalCells,
            Type0RouteMaskAssignmentResult type0,
            IDictionary<OptionalRegionId, OptionalRewardTierAssignment> rewards,
            InactiveBufferAssignmentResult inactive)
        {
            var reserved = new HashSet<int>(site.Sectors.Where(value => value.IsReserved).Select(value => value.Index));
            var mandatory = new HashSet<int>(graph.Cells.Select(value => value.SectorIndex));
            var type0Sectors = new HashSet<int>(type0.Assignments.Select(value => value.SectorIndex));
            var inactiveSectors = new HashSet<int>();
            foreach (var assignment in inactive.Assignments)
            {
                if (!inactiveSectors.Add(assignment.SectorIndex))
                    Add(issues, OptionalRegionValidationIssueCode.DuplicateSector, default(OptionalRegionId),
                        assignment.SectorIndex, "Inactive", "SectorIndex", "Inactive assignments must be unique.");
            }

            var overlap = new HashSet<int>(reserved);
            overlap.IntersectWith(mandatory);
            var approvedOverlap = new HashSet<int>(graph.Cells
                .Where(value => overlap.Contains(value.SectorIndex) && value.IsApprovedReservedAdapter)
                .Select(value => value.SectorIndex));
            if (!overlap.SetEquals(approvedOverlap) ||
                inactive.Diagnostics.ApprovedReservedAdapterOverlapCount != approvedOverlap.Count ||
                approvedOverlap.Overlaps(inactiveSectors))
                Add(issues, OptionalRegionValidationIssueCode.ReservedAdapterMismatch,
                    default(OptionalRegionId), -1, "Inactive", "ApprovedReservedAdapters",
                    "Approved site/mandatory adapters must match the protected overlap and remain active.");

            var protectedUnion = new HashSet<int>(reserved);
            protectedUnion.UnionWith(mandatory);
            protectedUnion.UnionWith(type0Sectors);
            var expectedInactive = new HashSet<int>(Enumerable.Range(0, world.Cells.Count));
            expectedInactive.ExceptWith(protectedUnion);
            if (!expectedInactive.SetEquals(inactiveSectors) ||
                inactive.Diagnostics.WorldSectorCount != world.Cells.Count ||
                inactive.Diagnostics.ProtectedUnionCount != protectedUnion.Count ||
                inactive.Diagnostics.AssignmentCount != inactive.Assignments.Count ||
                inactive.Diagnostics.ProtectedUnionCount + inactive.Diagnostics.AssignmentCount != world.Cells.Count ||
                inactive.Diagnostics.DecorativeBoundaryCount + inactive.Diagnostics.InteriorInactiveCount != inactive.Assignments.Count ||
                inactive.Diagnostics.UnassignedSectorCount != 0 ||
                inactive.Diagnostics.IllegalOwnershipOverlapCount != 0 ||
                inactive.Diagnostics.DuplicateSectorCount != 0)
                Add(issues, OptionalRegionValidationIssueCode.InactiveAccountingMismatch,
                    default(OptionalRegionId), -1, "Inactive", "Accounting",
                    "Inactive buffers must provide exact full-world exclusive accounting.");

            var openCount = inactive.Diagnostics.OpenEdgeToInactiveCount;
            foreach (var assignment in type0.Assignments)
            {
                var neighbors = new[]
                {
                    WorldGridIndex.GetLeftIndex(assignment.SectorIndex),
                    WorldGridIndex.GetRightIndex(assignment.SectorIndex),
                    WorldGridIndex.GetUpIndex(assignment.SectorIndex),
                    WorldGridIndex.GetDownIndex(assignment.SectorIndex)
                };
                var open = new[]
                {
                    assignment.OpenMask.OpenLeft, assignment.OpenMask.OpenRight,
                    assignment.OpenMask.OpenUp, assignment.OpenMask.OpenDown
                };
                for (var index = 0; index < neighbors.Length; index++)
                {
                    if (open[index] && neighbors[index] >= 0 && inactiveSectors.Contains(neighbors[index]))
                    {
                        openCount++;
                        Add(issues, OptionalRegionValidationIssueCode.OpenEdgeToInactive, assignment.RegionId,
                            assignment.SectorIndex, "Type0", "OpenMask",
                            "Optional route openings cannot point into inactive sectors.");
                    }
                }
            }
            if (inactive.Diagnostics.OpenEdgeToInactiveCount != 0)
                Add(issues, OptionalRegionValidationIssueCode.OpenEdgeToInactive, default(OptionalRegionId), -1,
                    "Inactive", "OpenEdgeToInactiveCount", "Inactive diagnostics must contain no open edge.");

            foreach (var reward in rewards)
            {
                if (!optionalCells.Values.Any(value => value.RegionId == reward.Key) ||
                    optionalCells.Values.Where(value => value.RegionId == reward.Key).Any(value =>
                        reserved.Contains(value.SectorIndex) || mandatory.Contains(value.SectorIndex) ||
                        inactiveSectors.Contains(value.SectorIndex) || !type0Sectors.Contains(value.SectorIndex)))
                    Add(issues, OptionalRegionValidationIssueCode.MandatoryRewardAssigned, reward.Key, -1,
                        "Reward", "RegionCells", "Reward regions must contain Type0 cells only.");
            }
            return openCount;
        }

        private static OptionalRegionValidationReport CreateReport(
            OptionalRegionValidationStatus status,
            IEnumerable<OptionalRegionValidationIssue> sourceIssues,
            GeneratedWorldData world,
            MandatoryRouteGraph graph,
            OptionalRegionSnapshot optionalRegions,
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access,
            OptionalRewardTierResult reward,
            OptionalReturnPolicyResult returns,
            InactiveBufferAssignmentResult inactive,
            OptionalRegionValidationSettings settings,
            int type0LeftRightOpenCount,
            int missingClueCount,
            int missingReturnPolicyCount,
            int openEdgeToInactiveCount,
            int rngDrawCount = 0,
            int sourceMutationCount = 0)
        {
            var issues = Normalize(sourceIssues);
            var diagnostics = new OptionalRegionValidationDiagnostics(
                world == null ? 0 : world.Cells.Count,
                graph == null ? 0 : graph.Cells.Count,
                optionalRegions == null ? 0 : optionalRegions.Regions.Count,
                type0 == null ? 0 : type0.Assignments.Count,
                access == null ? 0 : access.Assignments.Count,
                access == null ? 0 : access.Diagnostics.PerceptibleClueCount,
                reward == null ? 0 : reward.Assignments.Count,
                reward == null ? 0 : reward.Diagnostics.MandatoryRewardSelectionCount,
                returns == null ? 0 : returns.Assignments.Count,
                returns == null ? 0 : returns.Diagnostics.ReturnableCellCount,
                returns == null ? 0 : returns.Diagnostics.NonReturnableCellCount,
                inactive == null ? 0 : inactive.Assignments.Count,
                inactive == null ? 0 : inactive.Diagnostics.DecorativeBoundaryCount,
                inactive == null ? 0 : inactive.Diagnostics.InteriorInactiveCount,
                inactive == null ? 0 : inactive.Diagnostics.ProtectedUnionCount,
                inactive == null ? 0 : inactive.Diagnostics.ApprovedReservedAdapterOverlapCount,
                openEdgeToInactiveCount,
                type0LeftRightOpenCount,
                missingClueCount,
                missingReturnPolicyCount,
                issues.Count,
                rngDrawCount,
                sourceMutationCount);

            var mandatoryDigest = optionalRegions == null ? string.Empty : optionalRegions.SourceMandatoryGraphDigest;
            var growthDigest = type0 == null ? string.Empty : type0.SourceGrowthDigest;
            var type0Digest = type0 == null ? string.Empty : type0.CanonicalDigest;
            var accessDigest = access == null ? string.Empty : access.CanonicalDigest;
            var rewardDigest = reward == null ? string.Empty : reward.CanonicalDigest;
            var returnDigest = returns == null ? string.Empty : returns.CanonicalDigest;
            var inactiveDigest = inactive == null ? string.Empty : inactive.CanonicalDigest;
            var canonicalDigest = status == OptionalRegionValidationStatus.Valid
                ? ComputeDigest(settings, diagnostics, mandatoryDigest, growthDigest, type0Digest,
                    accessDigest, rewardDigest, returnDigest, inactiveDigest, optionalRegions,
                    type0, access, reward, returns, inactive, issues)
                : string.Empty;
            return new OptionalRegionValidationReport(status, diagnostics, issues, mandatoryDigest,
                growthDigest, type0Digest, accessDigest, rewardDigest, returnDigest, inactiveDigest,
                canonicalDigest);
        }

        private static string ComputeDigest(
            OptionalRegionValidationSettings settings,
            OptionalRegionValidationDiagnostics diagnostics,
            string mandatoryDigest,
            string growthDigest,
            string type0Digest,
            string accessDigest,
            string rewardDigest,
            string returnDigest,
            string inactiveDigest,
            OptionalRegionSnapshot regions,
            Type0RouteMaskAssignmentResult type0,
            OptionalAccessAssignmentResult access,
            OptionalRewardTierResult reward,
            OptionalReturnPolicyResult returns,
            InactiveBufferAssignmentResult inactive,
            IEnumerable<OptionalRegionValidationIssue> issues)
        {
            var builder = new StringBuilder();
            Append(builder, "contract", "MAP06_09_OPTIONAL_REGION_VALIDATION_V1");
            Append(builder, "mandatory", mandatoryDigest);
            Append(builder, "growth", growthDigest);
            Append(builder, "type0", type0Digest);
            Append(builder, "access", accessDigest);
            Append(builder, "reward", rewardDigest);
            Append(builder, "return", returnDigest);
            Append(builder, "inactive", inactiveDigest);
            foreach (var value in SettingsValues(settings)) Append(builder, "setting", value ? "1" : "0");
            foreach (var value in DiagnosticValues(diagnostics)) Append(builder, "diagnostic", value.ToString(CultureInfo.InvariantCulture));

            foreach (var region in regions.Regions.OrderBy(value => value.RegionId))
            {
                Append(builder, "region", string.Join("|", region.RegionId.Value,
                    region.Attachment.AttachmentOrder.ToString(CultureInfo.InvariantCulture),
                    region.Attachment.MandatoryRouteSectorIndex.ToString(CultureInfo.InvariantCulture),
                    region.Attachment.EntrySectorIndex.ToString(CultureInfo.InvariantCulture),
                    region.MaxDepth.Value.ToString(CultureInfo.InvariantCulture)));
                foreach (var cell in region.Cells.OrderBy(value => value.SectorIndex))
                    Append(builder, "cell", string.Join("|", region.RegionId.Value,
                        cell.SectorIndex.ToString(CultureInfo.InvariantCulture),
                        cell.Depth.Value.ToString(CultureInfo.InvariantCulture),
                        cell.IsAttachmentCell ? "1" : "0", cell.RequiresReturnConnection ? "1" : "0"));
            }
            foreach (var assignment in type0.Assignments.OrderBy(value => value.SectorIndex))
                Append(builder, "type0Fact", assignment.RegionId.Value + "|" +
                    assignment.SectorIndex.ToString(CultureInfo.InvariantCulture) + "|" + assignment.OpenMask);
            foreach (var assignment in access.Assignments.OrderBy(value => value.RegionId))
                Append(builder, "accessFact", assignment.RegionId.Value + "|" + assignment.AccessRule + "|" +
                    assignment.EntrySectorIndex.ToString(CultureInfo.InvariantCulture));
            foreach (var assignment in reward.Assignments.OrderBy(value => value.RegionId))
                Append(builder, "rewardFact", assignment.RegionId.Value + "|" + assignment.RewardTier + "|" +
                    assignment.RewardScore.ToString(CultureInfo.InvariantCulture));
            foreach (var assignment in returns.Assignments.OrderBy(value => value.RegionId))
                Append(builder, "returnFact", assignment.RegionId.Value + "|" + assignment.ReturnPolicy + "|" +
                    assignment.ReturnableCellCount.ToString(CultureInfo.InvariantCulture));
            foreach (var assignment in inactive.Assignments.OrderBy(value => value.SectorIndex))
                Append(builder, "inactiveFact", assignment.SectorIndex.ToString(CultureInfo.InvariantCulture) + "|" +
                    assignment.Kind + "|" + string.Join(",", assignment.ProtectedNeighborSectorIndices) + "|" +
                    string.Join(",", assignment.InactiveNeighborSectorIndices));
            foreach (var issue in issues)
                Append(builder, "issue", issue.Code + "|" + issue.RegionId.Value + "|" +
                    issue.SectorIndex.ToString(CultureInfo.InvariantCulture) + "|" + issue.Source + "|" +
                    issue.Field + "|" + issue.Message);

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                var result = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static IEnumerable<bool> SettingsValues(OptionalRegionValidationSettings value)
        {
            yield return value.RequireMandatoryGraphIdentity;
            yield return value.RequireSourceDigests;
            yield return value.RequireRegionIdentity;
            yield return value.RequireType0NoLeftRight;
            yield return value.RequireReturnability;
            yield return value.RequireVisibleClues;
            yield return value.ForbidMandatoryRewards;
            yield return value.RequireInactiveFullAccounting;
            yield return value.RequireNoRngOrSourceMutation;
        }

        private static IEnumerable<int> DiagnosticValues(OptionalRegionValidationDiagnostics value)
        {
            yield return value.WorldSectorCount;
            yield return value.MandatoryRouteCellCount;
            yield return value.OptionalRegionCount;
            yield return value.Type0CellCount;
            yield return value.AccessAssignmentCount;
            yield return value.VisibleClueCount;
            yield return value.RewardAssignmentCount;
            yield return value.MandatoryRewardAssignmentCount;
            yield return value.ReturnAssignmentCount;
            yield return value.ReturnableCellCount;
            yield return value.NonReturnableCellCount;
            yield return value.InactiveBufferAssignmentCount;
            yield return value.DecorativeBoundaryCount;
            yield return value.InteriorInactiveCount;
            yield return value.ProtectedUnionCount;
            yield return value.ApprovedReservedAdapterOverlapCount;
            yield return value.OpenEdgeToInactiveCount;
            yield return value.Type0LeftRightOpenCount;
            yield return value.MissingClueCount;
            yield return value.MissingReturnPolicyCount;
            yield return value.IssueCount;
            yield return value.RngDrawCount;
            yield return value.SourceMutationCount;
        }

        private static void Append(StringBuilder builder, string field, string value)
        {
            builder.Append(field).Append('=').Append(value ?? string.Empty).Append('\n');
        }

        private static OptionalRegionValidationStatus SelectStatus(IReadOnlyList<OptionalRegionValidationIssue> issues)
        {
            if (issues.Count == 0) return OptionalRegionValidationStatus.Valid;
            if (issues.Any(value => value.Code == OptionalRegionValidationIssueCode.NullInput))
                return OptionalRegionValidationStatus.InvalidInput;
            if (issues.Any(value => value.Source == "Settings"))
                return OptionalRegionValidationStatus.InvalidSettings;
            if (issues.Any(value => value.Code == OptionalRegionValidationIssueCode.InvalidStatus ||
                                    value.Code == OptionalRegionValidationIssueCode.InvalidDigest ||
                                    value.Code == OptionalRegionValidationIssueCode.SourceMismatch ||
                                    value.Code == OptionalRegionValidationIssueCode.RngConsumed ||
                                    value.Code == OptionalRegionValidationIssueCode.SourceMutation))
                return OptionalRegionValidationStatus.InvalidSource;
            if (issues.Any(value => value.Code == OptionalRegionValidationIssueCode.InactiveAccountingMismatch ||
                                    value.Code == OptionalRegionValidationIssueCode.MandatoryRewardAssigned))
                return OptionalRegionValidationStatus.InvalidAccounting;
            if (issues.Any(value => value.Code == OptionalRegionValidationIssueCode.InvalidWorld ||
                                    value.Code == OptionalRegionValidationIssueCode.InvalidMandatoryGraph ||
                                    value.Code == OptionalRegionValidationIssueCode.InvalidOptionalRegionSnapshot ||
                                    value.Code == OptionalRegionValidationIssueCode.Type0LeftRightOpen ||
                                    value.Code == OptionalRegionValidationIssueCode.OpenEdgeToInactive ||
                                    value.Code == OptionalRegionValidationIssueCode.DuplicateSector))
                return OptionalRegionValidationStatus.InvalidTopology;
            return OptionalRegionValidationStatus.InvalidRules;
        }

        private static List<OptionalRegionValidationIssue> Normalize(IEnumerable<OptionalRegionValidationIssue> source)
        {
            var result = new List<OptionalRegionValidationIssue>(source);
            result.Sort(OptionalRegionValidationIssue.Compare);
            for (var index = result.Count - 1; index > 0; index--)
            {
                if (result[index].SameIdentity(result[index - 1])) result.RemoveAt(index);
            }
            return result;
        }

        private static void AddNull(ICollection<OptionalRegionValidationIssue> issues, object value, string field)
        {
            if (value == null)
                Add(issues, OptionalRegionValidationIssueCode.NullInput, default(OptionalRegionId), -1,
                    "Input", field, "Validation inputs cannot be null.");
        }

        private static void AddFalseSettings(
            ICollection<OptionalRegionValidationIssue> issues, OptionalRegionValidationSettings settings)
        {
            var names = new[]
            {
                "RequireMandatoryGraphIdentity", "RequireSourceDigests", "RequireRegionIdentity",
                "RequireType0NoLeftRight", "RequireReturnability", "RequireVisibleClues",
                "ForbidMandatoryRewards", "RequireInactiveFullAccounting", "RequireNoRngOrSourceMutation"
            };
            var values = SettingsValues(settings).ToArray();
            for (var index = 0; index < values.Length; index++)
            {
                if (!values[index]) Add(issues, OptionalRegionValidationIssueCode.SourceMismatch,
                    default(OptionalRegionId), -1, "Settings", names[index],
                    "All MAP06_09 validation settings must be enabled.");
            }
        }

        private static void InvalidStatus(ICollection<OptionalRegionValidationIssue> issues, string source, string value)
        {
            Add(issues, OptionalRegionValidationIssueCode.InvalidStatus, default(OptionalRegionId), -1,
                source, "Status", "Source status must be Completed: " + value + ".");
        }

        private static void CheckDigest(
            ICollection<OptionalRegionValidationIssue> issues, string value, string source, string field)
        {
            if (!OptionalRegionValidationReport.IsLowerHexDigest(value))
                Add(issues, OptionalRegionValidationIssueCode.InvalidDigest, default(OptionalRegionId), -1,
                    source, field, "Digest must be lowercase 64-hex.");
        }

        private static void CheckIdentity(
            ICollection<OptionalRegionValidationIssue> issues, string actual, string expected, string source, string field)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                Add(issues, OptionalRegionValidationIssueCode.InvalidDigest, default(OptionalRegionId), -1,
                    source, field, "Mandatory graph digest must match the approved graph identity.");
        }

        private static void CheckChain(
            ICollection<OptionalRegionValidationIssue> issues, string actual, string expected, string source, string field)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal)) SourceMismatch(issues, source, field);
        }

        private static void SourceMismatch(
            ICollection<OptionalRegionValidationIssue> issues, string source, string field)
        {
            Add(issues, OptionalRegionValidationIssueCode.SourceMismatch, default(OptionalRegionId), -1,
                source, field, "Source-chain identity does not match the preceding artifact.");
        }

        private static void Add(
            ICollection<OptionalRegionValidationIssue> issues,
            OptionalRegionValidationIssueCode code,
            OptionalRegionId regionId,
            int sectorIndex,
            string source,
            string field,
            string message)
        {
            issues.Add(new OptionalRegionValidationIssue(code, regionId, sectorIndex, source, field, message));
        }
    }
}
