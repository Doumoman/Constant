using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CoreCapacityFloodChecker
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";

        public CoreCapacityFloodResult Check(
            SiteReservationSelectionPlan selectionPlan,
            IEnumerable<CoreCapacityRequirement> requirements)
        {
            var errors = new List<CoreCapacityFloodError>();
            var selected = ValidateSelectionPlan(selectionPlan, errors);
            var orderedRequirements = SnapshotRequirements(requirements, errors);
            ValidateRequirements(orderedRequirements, selected, errors);
            if (errors.Count != 0)
            {
                return new CoreCapacityFloodResult(
                    CoreCapacityFloodStatus.InvalidInput,
                    null,
                    null,
                    Array.Empty<CoreCapacityFloodRejection>(),
                    errors);
            }

            try
            {
                return Evaluate(selectionPlan, selected, orderedRequirements);
            }
            catch (Exception)
            {
                AddError(errors, CoreCapacityFloodErrorCode.InternalInvariantViolation,
                    string.Empty, string.Empty, string.Empty, -1,
                    "Core capacity evaluation violated an internal invariant.");
                return new CoreCapacityFloodResult(
                    CoreCapacityFloodStatus.InvalidInput,
                    null,
                    null,
                    Array.Empty<CoreCapacityFloodRejection>(),
                    errors);
            }
        }

        private static CoreCapacityFloodResult Evaluate(
            SiteReservationSelectionPlan selectionPlan,
            IReadOnlyList<FootprintPlacement> selected,
            IReadOnlyList<CoreCapacityRequirement> requirements)
        {
            var works = new List<SiteWork>(requirements.Count);
            foreach (var requirement in requirements)
                works.Add(new SiteWork(requirement));

            var occupiedOwners = BuildOccupiedOwners(selected);
            foreach (var work in works) BuildMandatoryBuffer(work);

            var rejections = new List<CoreCapacityFloodRejection>();
            foreach (var work in works)
            {
                if (work.OutsideTheoreticalBufferCount > 0 &&
                    !work.Requirement.CorePatchRule.CanTouchWorldEdge)
                {
                    rejections.Add(new CoreCapacityFloodRejection(
                        CoreCapacityFloodRejectionReason.BufferOutsideWorld,
                        work.Requirement.Key,
                        default(SitePlacementKey),
                        -1,
                        work.MandatoryBuffer.Count + work.OutsideTheoreticalBufferCount,
                        work.MandatoryBuffer.Count,
                        "The mandatory cardinal buffer extends outside the world."));
                }

                foreach (var sector in work.MandatoryBuffer)
                {
                    if (occupiedOwners.TryGetValue(sector, out var owner) &&
                        owner != work.Requirement.Key)
                    {
                        work.BlockedMandatoryBufferCount++;
                        rejections.Add(new CoreCapacityFloodRejection(
                            CoreCapacityFloodRejectionReason.BufferBlockedBySelectedFootprint,
                            work.Requirement.Key,
                            owner,
                            sector,
                            1,
                            0,
                            "A selected footprint blocks the mandatory Core buffer."));
                    }
                }
            }

            for (var first = 0; first < works.Count; first++)
            {
                for (var second = first + 1; second < works.Count; second++)
                {
                    foreach (var sector in Intersect(
                                 works[first].MandatoryBuffer,
                                 works[second].MandatoryBuffer))
                    {
                        works[first].OverlappingMandatoryBufferCount++;
                        works[second].OverlappingMandatoryBufferCount++;
                        rejections.Add(BufferOverlap(works[first], works[second], sector));
                        rejections.Add(BufferOverlap(works[second], works[first], sector));
                    }
                }
            }

            var reservedFootprintCount = occupiedOwners.Count;
            if (rejections.Count != 0)
            {
                return Rejected(works, selected.Count, reservedFootprintCount, rejections);
            }

            foreach (var work in works)
            {
                var blocked = BuildIndependentBlocked(work, works, occupiedOwners);
                work.Reachable = Flood(work.MandatoryBuffer, blocked);
                work.FloodVisitedSectorCount = work.Reachable.Count;
                work.AvailableConnectedSectorCount = work.Reachable.Count;
                if (work.Reachable.Count < work.RequiredWitnessSectorCount)
                {
                    rejections.Add(new CoreCapacityFloodRejection(
                        CoreCapacityFloodRejectionReason.InsufficientConnectedCapacity,
                        work.Requirement.Key,
                        default(SitePlacementKey),
                        -1,
                        work.RequiredWitnessSectorCount,
                        work.Reachable.Count,
                        "The mandatory buffer lacks sufficient connected Core capacity."));
                }
            }
            if (rejections.Count != 0)
                return Rejected(works, selected.Count, reservedFootprintCount, rejections);

            var claimedBy = Enumerable.Repeat(-1, WorldGenConstants.SectorCount).ToArray();
            for (var owner = 0; owner < works.Count; owner++)
            {
                works[owner].Witness = new SortedSet<int>(works[owner].MandatoryBuffer);
                foreach (var sector in works[owner].MandatoryBuffer) claimedBy[sector] = owner;
            }

            for (var owner = 0; owner < works.Count; owner++)
            {
                var work = works[owner];
                AllocateWitness(work, owner, claimedBy, occupiedOwners);
                if (work.Witness.Count < work.RequiredWitnessSectorCount ||
                    !IsConnected(work.Witness))
                {
                    rejections.Add(new CoreCapacityFloodRejection(
                        CoreCapacityFloodRejectionReason.InsufficientDisjointCapacity,
                        work.Requirement.Key,
                        default(SitePlacementKey),
                        -1,
                        work.RequiredWitnessSectorCount,
                        Math.Min(work.Witness.Count, work.RequiredWitnessSectorCount),
                        "Canonical allocation lacks sufficient disjoint connected capacity."));
                    break;
                }
            }
            if (rejections.Count != 0)
                return Rejected(works, selected.Count, reservedFootprintCount, rejections);

            var witnesses = new List<CoreCapacityFloodWitness>(works.Count);
            foreach (var work in works)
            {
                witnesses.Add(new CoreCapacityFloodWitness(
                    work.Requirement.Key,
                    work.Requirement.PrimaryBiome.BiomeId,
                    work.Requirement.CorePatchRule.PatchRuleId,
                    work.Footprint[0],
                    work.Requirement.CorePatchRule.MinSectorCount,
                    work.Requirement.CorePatchRule.BufferRingSectors,
                    work.Requirement.CorePatchRule.CanTouchWorldEdge,
                    work.RequiredWitnessSectorCount,
                    work.AvailableConnectedSectorCount,
                    work.Footprint,
                    work.MandatoryBuffer,
                    work.Reachable,
                    work.Witness));
            }

            var diagnostics = BuildDiagnostics(
                works, selected.Count, reservedFootprintCount);
            var approval = new CoreCapacityApproval(selectionPlan, witnesses);
            return new CoreCapacityFloodResult(
                CoreCapacityFloodStatus.Completed,
                approval,
                diagnostics,
                Array.Empty<CoreCapacityFloodRejection>(),
                Array.Empty<CoreCapacityFloodError>());
        }

        private static List<FootprintPlacement> ValidateSelectionPlan(
            SiteReservationSelectionPlan plan,
            ICollection<CoreCapacityFloodError> errors)
        {
            var selected = new List<FootprintPlacement>();
            if (plan == null)
            {
                AddError(errors, CoreCapacityFloodErrorCode.MissingSelectionPlan,
                    string.Empty, string.Empty, string.Empty, -1,
                    "A completed six-site selection plan is required.");
                return selected;
            }

            var expected = SelectionKeys();
            if (plan.Steps == null || plan.SelectedPlacements == null ||
                plan.Steps.Count != expected.Count || plan.SelectedCount != expected.Count ||
                plan.SelectedPlacements.Count != expected.Count)
            {
                AddError(errors, CoreCapacityFloodErrorCode.InvalidSelectionPlan,
                    string.Empty, string.Empty, string.Empty, -1,
                    "The selection plan must contain exactly six ordered sites.");
                return selected;
            }

            var occupied = new Dictionary<int, SitePlacementKey>();
            for (var index = 0; index < expected.Count; index++)
            {
                var step = plan.Steps[index];
                var placement = plan.SelectedPlacements[index];
                var source = step == null ? string.Empty : Canonical(step.Key.SourceDefinitionId);
                if (step == null || step.Depth != index || step.Key != expected[index] ||
                    step.Option == null || step.Option.Placement == null || placement == null ||
                    !PlacementEquivalent(step.Option.Placement, placement))
                {
                    AddError(errors, CoreCapacityFloodErrorCode.InvalidSelectionPlan,
                        source, string.Empty, string.Empty, -1,
                        "A selection step does not match the exact depth, key, or placement order.");
                    continue;
                }

                selected.Add(placement);
                ValidateFootprint(placement, step.Key, errors);
                foreach (var coordinate in placement.OccupiedSectors)
                {
                    var sector = ValidSectorIndex(coordinate);
                    if (sector < 0) continue;
                    if (occupied.ContainsKey(sector))
                    {
                        AddError(errors, CoreCapacityFloodErrorCode.InvalidFootprint,
                            source, string.Empty, string.Empty, sector,
                            "Selected footprints cannot overlap.");
                    }
                    else occupied.Add(sector, step.Key);
                }
            }
            return selected;
        }

        private static void ValidateFootprint(
            FootprintPlacement placement,
            SitePlacementKey expectedKey,
            ICollection<CoreCapacityFloodError> errors)
        {
            var source = Canonical(expectedKey.SourceDefinitionId);
            if (placement.Candidate == null || placement.Footprint == null ||
                placement.OccupiedSectors == null || placement.Entries == null ||
                !Enum.IsDefined(typeof(SiteReservationKind), placement.Candidate.Kind) ||
                !Enum.IsDefined(typeof(SiteFootprintTransform), placement.Footprint.Transform) ||
                SitePlacementKey.FromPlacement(placement) != expectedKey ||
                placement.OccupiedSectors.Count == 0)
            {
                AddError(errors, CoreCapacityFloodErrorCode.InvalidFootprint,
                    source, string.Empty, string.Empty, -1,
                    "A selected placement has an invalid footprint identity.");
                return;
            }

            var occupied = new HashSet<int>();
            foreach (var coordinate in placement.OccupiedSectors)
            {
                var sector = ValidSectorIndex(coordinate);
                if (sector < 0 || !occupied.Add(sector))
                    AddError(errors, CoreCapacityFloodErrorCode.InvalidFootprint,
                        source, string.Empty, string.Empty, sector,
                        "Footprint sectors must be unique and inside the world.");
            }
            foreach (var entry in placement.Entries)
            {
                var sector = entry == null ? -1 : ValidSectorIndex(entry.FootprintSector);
                if (entry == null || !Enum.IsDefined(typeof(SiteEntrySide), entry.Side) ||
                    sector < 0 || !occupied.Contains(sector) ||
                    ValidSectorIndex(entry.ExteriorSector) < 0)
                {
                    AddError(errors, CoreCapacityFloodErrorCode.InvalidFootprint,
                        source, string.Empty, string.Empty, sector,
                        "A selected entry has an invalid sector identity.");
                    continue;
                }
                SiteReservationTokenCodec.GetDelta(entry.Side, out var deltaX, out var deltaY);
                if (entry.ExteriorSector != new SectorCoord(
                        entry.FootprintSector.X + deltaX,
                        entry.FootprintSector.Y + deltaY))
                {
                    AddError(errors, CoreCapacityFloodErrorCode.InvalidFootprint,
                        source, string.Empty, string.Empty, sector,
                        "A selected entry exterior must be one cardinal step away.");
                }
            }
        }

        private static List<CoreCapacityRequirement> SnapshotRequirements(
            IEnumerable<CoreCapacityRequirement> source,
            ICollection<CoreCapacityFloodError> errors)
        {
            var result = new List<CoreCapacityRequirement>();
            if (source == null)
            {
                AddError(errors, CoreCapacityFloodErrorCode.MissingRequirements,
                    string.Empty, string.Empty, string.Empty, -1,
                    "A Core capacity requirement collection is required.");
                return result;
            }
            try
            {
                foreach (var requirement in source)
                {
                    if (requirement == null)
                        AddError(errors, CoreCapacityFloodErrorCode.NullRequirement,
                            string.Empty, string.Empty, string.Empty, -1,
                            "Core capacity requirements cannot contain null.");
                    else result.Add(requirement);
                }
            }
            catch (Exception)
            {
                AddError(errors, CoreCapacityFloodErrorCode.InvalidRequirement,
                    string.Empty, string.Empty, string.Empty, -1,
                    "Core capacity requirements must be eagerly enumerable.");
            }
            result.Sort((left, right) => CapacityOrder(left.Key).CompareTo(CapacityOrder(right.Key)));
            return result;
        }

        private static void ValidateRequirements(
            IReadOnlyList<CoreCapacityRequirement> requirements,
            IReadOnlyList<FootprintPlacement> selected,
            ICollection<CoreCapacityFloodError> errors)
        {
            var expected = CapacityKeys();
            var seen = new HashSet<SitePlacementKey>();
            var selectedByKey = new Dictionary<SitePlacementKey, FootprintPlacement>();
            foreach (var placement in selected)
            {
                if (placement != null && placement.Candidate != null)
                    selectedByKey[SitePlacementKey.FromPlacement(placement)] = placement;
            }

            foreach (var requirement in requirements)
            {
                var site = Canonical(requirement.Key.SourceDefinitionId);
                var biome = requirement.PrimaryBiome == null
                    ? string.Empty : Canonical(requirement.PrimaryBiome.BiomeId);
                var rule = requirement.CorePatchRule == null
                    ? string.Empty : Canonical(requirement.CorePatchRule.PatchRuleId);
                if (!requirement.Key.IsValid)
                    AddError(errors, CoreCapacityFloodErrorCode.InvalidRequirement,
                        site, biome, rule, -1, "A capacity requirement needs a valid key.");
                else if (!Contains(expected, requirement.Key))
                    AddError(errors, CoreCapacityFloodErrorCode.UnexpectedRequirement,
                        site, biome, rule, -1, "The capacity requirement is outside the exact four-site set.");
                if (!seen.Add(requirement.Key))
                    AddError(errors, CoreCapacityFloodErrorCode.DuplicateRequirementKey,
                        site, biome, rule, -1, "Capacity requirement keys must be unique.");

                if (!selectedByKey.TryGetValue(requirement.Key, out var selectedPlacement))
                    AddError(errors, CoreCapacityFloodErrorCode.PlacementNotSelected,
                        site, biome, rule, -1, "The capacity placement is not in the selection plan.");
                else if (requirement.Placement == null ||
                         !PlacementEquivalent(requirement.Placement, selectedPlacement))
                    AddError(errors, CoreCapacityFloodErrorCode.PlacementIdentityMismatch,
                        site, biome, rule, -1,
                        "The capacity placement does not exactly match its selected placement.");

                ValidateDefinitions(requirement, site, biome, rule, errors);
            }
            foreach (var key in expected)
            {
                if (!seen.Contains(key))
                    AddError(errors, CoreCapacityFloodErrorCode.MissingRequiredRequirement,
                        key.SourceDefinitionId, string.Empty, string.Empty, -1,
                        "An exact required Core capacity site is missing.");
            }
        }

        private static void ValidateDefinitions(
            CoreCapacityRequirement requirement,
            string site,
            string biome,
            string rule,
            ICollection<CoreCapacityFloodError> errors)
        {
            var special = requirement.SpecialMap;
            if (special == null)
                AddError(errors, CoreCapacityFloodErrorCode.MissingSpecialMap,
                    site, biome, rule, -1, "A typed special-map definition is required.");
            else
            {
                var expectedRole = requirement.Key.Kind == SiteReservationKind.Forge
                    ? "FORGE" : "CORE_RESOURCE";
                if (!SitePlacementKey.IsCanonicalId(special.SpecialMapId) || !special.Active ||
                    special.RequiredCount != 1 ||
                    !string.Equals(special.SpecialMapId, requirement.Key.SourceDefinitionId,
                        StringComparison.Ordinal) ||
                    !string.Equals(special.SiteRole, expectedRole, StringComparison.Ordinal))
                    AddError(errors, CoreCapacityFloodErrorCode.InvalidSpecialMap,
                        site, biome, rule, -1, "The special-map definition is invalid for this capacity site.");
                if (requirement.Placement != null && requirement.Placement.Footprint != null &&
                    (special.FootprintWidthSectors != requirement.Placement.Footprint.Width ||
                     special.FootprintHeightSectors != requirement.Placement.Footprint.Height))
                    AddError(errors, CoreCapacityFloodErrorCode.DefinitionIdentityMismatch,
                        site, biome, rule, -1, "Special-map dimensions do not match the selected footprint.");
            }

            var primary = requirement.PrimaryBiome;
            if (primary == null)
                AddError(errors, CoreCapacityFloodErrorCode.MissingPrimaryBiome,
                    site, biome, rule, -1, "A typed primary-biome definition is required.");
            else if (!SitePlacementKey.IsCanonicalId(primary.BiomeId) || !primary.Active ||
                     primary.MinCorePatchCount != 1)
                AddError(errors, CoreCapacityFloodErrorCode.InvalidPrimaryBiome,
                    site, biome, rule, -1, "The primary biome is not an active one-Core definition.");

            var coreRule = requirement.CorePatchRule;
            if (coreRule == null)
                AddError(errors, CoreCapacityFloodErrorCode.MissingCorePatchRule,
                    site, biome, rule, -1, "An active Core patch rule is required.");
            else if (!SitePlacementKey.IsCanonicalId(coreRule.PatchRuleId) ||
                     !SitePlacementKey.IsCanonicalId(coreRule.BiomeId) ||
                     !string.Equals(coreRule.PatchRole, "CORE", StringComparison.Ordinal) ||
                     !coreRule.Active || coreRule.MinSectorCount < 1 ||
                     coreRule.MinSectorCount > coreRule.MaxSectorCount ||
                     coreRule.MaxSectorCount > WorldGenConstants.SectorCount ||
                     coreRule.BufferRingSectors < 0 || coreRule.BufferRingSectors > 12)
                AddError(errors, CoreCapacityFloodErrorCode.InvalidCorePatchRule,
                    site, biome, rule, -1, "The Core patch rule has an invalid role, range, or buffer.");

            if (special != null && primary != null &&
                !string.Equals(special.PrimaryBiomeId, primary.BiomeId, StringComparison.Ordinal) ||
                primary != null && coreRule != null &&
                !string.Equals(primary.BiomeId, coreRule.BiomeId, StringComparison.Ordinal))
            {
                AddError(errors, CoreCapacityFloodErrorCode.DefinitionIdentityMismatch,
                    site, biome, rule, -1,
                    "Special-map, biome, and Core-rule identities must match.");
            }
        }

        private static void BuildMandatoryBuffer(SiteWork work)
        {
            var radius = work.Requirement.CorePatchRule.BufferRingSectors;
            var outside = new HashSet<GridPoint>();
            foreach (var sector in work.Footprint)
            {
                var origin = WorldGridIndex.ToCoordinate(sector);
                for (var deltaX = -radius; deltaX <= radius; deltaX++)
                {
                    var vertical = radius - Math.Abs(deltaX);
                    for (var deltaY = -vertical; deltaY <= vertical; deltaY++)
                    {
                        var x = origin.X + deltaX;
                        var y = origin.Y + deltaY;
                        if (x < 0 || x >= WorldGenConstants.SectorColumns ||
                            y < 0 || y >= WorldGenConstants.SectorRows)
                            outside.Add(new GridPoint(x, y));
                        else
                            work.MandatoryBuffer.Add(WorldGridIndex.ToIndex(
                                new SectorCoord(x, y)));
                    }
                }
            }
            work.OutsideTheoreticalBufferCount = outside.Count;
            work.RequiredWitnessSectorCount = Math.Max(
                work.Requirement.CorePatchRule.MinSectorCount,
                work.MandatoryBuffer.Count);
        }

        private static HashSet<int> BuildIndependentBlocked(
            SiteWork own,
            IReadOnlyList<SiteWork> works,
            IReadOnlyDictionary<int, SitePlacementKey> occupiedOwners)
        {
            var blocked = new HashSet<int>();
            foreach (var pair in occupiedOwners)
                if (pair.Value != own.Requirement.Key) blocked.Add(pair.Key);
            foreach (var other in works)
                if (other != own) blocked.UnionWith(other.MandatoryBuffer);
            return blocked;
        }

        private static SortedSet<int> Flood(
            IEnumerable<int> seeds,
            ISet<int> blocked)
        {
            var visited = new bool[WorldGenConstants.SectorCount];
            var queue = new Queue<int>();
            foreach (var seed in seeds.OrderBy(value => value))
            {
                if (blocked.Contains(seed) || visited[seed]) continue;
                visited[seed] = true;
                queue.Enqueue(seed);
            }
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in Neighbors(current))
                {
                    if (visited[neighbor] || blocked.Contains(neighbor)) continue;
                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }
            var result = new SortedSet<int>();
            for (var index = 0; index < visited.Length; index++)
                if (visited[index]) result.Add(index);
            return result;
        }

        private static void AllocateWitness(
            SiteWork work,
            int owner,
            int[] claimedBy,
            IReadOnlyDictionary<int, SitePlacementKey> occupiedOwners)
        {
            var visited = new bool[WorldGenConstants.SectorCount];
            var frontier = new SortedSet<int>(work.Witness);
            foreach (var sector in frontier) visited[sector] = true;
            while (work.Witness.Count < work.RequiredWitnessSectorCount && frontier.Count != 0)
            {
                var next = new SortedSet<int>();
                foreach (var sector in frontier)
                {
                    foreach (var neighbor in Neighbors(sector))
                    {
                        if (visited[neighbor]) continue;
                        visited[neighbor] = true;
                        if (occupiedOwners.TryGetValue(neighbor, out var footprintOwner) &&
                            footprintOwner != work.Requirement.Key) continue;
                        if (claimedBy[neighbor] >= 0 && claimedBy[neighbor] != owner) continue;
                        next.Add(neighbor);
                    }
                }
                foreach (var sector in next)
                {
                    if (work.Witness.Count >= work.RequiredWitnessSectorCount) break;
                    work.Witness.Add(sector);
                    claimedBy[sector] = owner;
                }
                frontier = work.Witness.Count < work.RequiredWitnessSectorCount
                    ? next : new SortedSet<int>();
            }
        }

        private static bool IsConnected(IReadOnlyCollection<int> sectors)
        {
            if (sectors.Count == 0) return false;
            var set = new HashSet<int>(sectors);
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            var first = sectors.Min();
            visited.Add(first);
            queue.Enqueue(first);
            while (queue.Count != 0)
            {
                foreach (var neighbor in Neighbors(queue.Dequeue()))
                    if (set.Contains(neighbor) && visited.Add(neighbor)) queue.Enqueue(neighbor);
            }
            return visited.Count == set.Count;
        }

        private static IReadOnlyList<int> Neighbors(int sector)
        {
            var values = new List<int>(4);
            AddNeighbor(values, WorldGridIndex.GetLeftIndex(sector));
            AddNeighbor(values, WorldGridIndex.GetRightIndex(sector));
            AddNeighbor(values, WorldGridIndex.GetUpIndex(sector));
            AddNeighbor(values, WorldGridIndex.GetDownIndex(sector));
            values.Sort();
            return values;
        }

        private static void AddNeighbor(ICollection<int> target, int value)
        {
            if (value != SectorNeighborIndices.NoNeighbor) target.Add(value);
        }

        private static Dictionary<int, SitePlacementKey> BuildOccupiedOwners(
            IEnumerable<FootprintPlacement> placements)
        {
            var result = new Dictionary<int, SitePlacementKey>();
            foreach (var placement in placements)
            {
                var key = SitePlacementKey.FromPlacement(placement);
                foreach (var coordinate in placement.OccupiedSectors)
                    result.Add(WorldGridIndex.ToIndex(coordinate), key);
            }
            return result;
        }

        private static CoreCapacityFloodRejection BufferOverlap(
            SiteWork own,
            SiteWork other,
            int sector) => new CoreCapacityFloodRejection(
                CoreCapacityFloodRejectionReason.MandatoryBufferOverlap,
                own.Requirement.Key,
                other.Requirement.Key,
                sector,
                1,
                0,
                "Two Core capacity sites require the same mandatory buffer sector.");

        private static IEnumerable<int> Intersect(
            IEnumerable<int> first,
            ISet<int> second)
        {
            foreach (var value in first)
                if (second.Contains(value)) yield return value;
        }

        private static CoreCapacityFloodResult Rejected(
            IReadOnlyList<SiteWork> works,
            int selectedCount,
            int reservedFootprintCount,
            IEnumerable<CoreCapacityFloodRejection> rejections) =>
            new CoreCapacityFloodResult(
                CoreCapacityFloodStatus.CapacityRejected,
                null,
                BuildDiagnostics(works, selectedCount, reservedFootprintCount),
                rejections,
                Array.Empty<CoreCapacityFloodError>());

        private static CoreCapacityFloodDiagnostics BuildDiagnostics(
            IEnumerable<SiteWork> works,
            int selectedCount,
            int reservedFootprintCount)
        {
            var sites = new List<CoreCapacitySiteDiagnostics>();
            foreach (var work in works)
            {
                sites.Add(new CoreCapacitySiteDiagnostics(
                    work.Requirement.Key,
                    work.Footprint.Count,
                    work.MandatoryBuffer.Count,
                    work.OutsideTheoreticalBufferCount,
                    work.BlockedMandatoryBufferCount,
                    work.OverlappingMandatoryBufferCount,
                    work.Requirement.CorePatchRule.MinSectorCount,
                    work.RequiredWitnessSectorCount,
                    work.FloodVisitedSectorCount,
                    work.AvailableConnectedSectorCount,
                    work.Witness == null ? 0 : work.Witness.Count));
            }
            return new CoreCapacityFloodDiagnostics(sites, selectedCount, reservedFootprintCount);
        }

        private static bool PlacementEquivalent(
            FootprintPlacement left,
            FootprintPlacement right)
        {
            if (left == null || right == null || left.Candidate == null || right.Candidate == null ||
                left.Footprint == null || right.Footprint == null) return false;
            var first = left.Candidate;
            var second = right.Candidate;
            if (first.Kind != second.Kind ||
                !string.Equals(first.SourceDefinitionId, second.SourceDefinitionId,
                    StringComparison.Ordinal) ||
                first.RequiredInstanceOrdinal != second.RequiredInstanceOrdinal ||
                first.Origin != second.Origin || first.OriginIndex != second.OriginIndex ||
                first.CandidateOrdinal != second.CandidateOrdinal ||
                left.Footprint.Transform != right.Footprint.Transform ||
                !SequenceEqual(left.OccupiedSectors.Select(WorldGridIndex.ToIndex),
                    right.OccupiedSectors.Select(WorldGridIndex.ToIndex)) ||
                left.Entries.Count != right.Entries.Count)
                return false;

            for (var index = 0; index < left.Entries.Count; index++)
            {
                var a = left.Entries[index];
                var b = right.Entries[index];
                if (a == null || b == null ||
                    !string.Equals(a.EntrySocketId, b.EntrySocketId, StringComparison.Ordinal) ||
                    a.LocalX != b.LocalX || a.LocalY != b.LocalY ||
                    a.FootprintSector != b.FootprintSector || a.Side != b.Side ||
                    a.ExteriorSector != b.ExteriorSector || a.Required != b.Required ||
                    a.ReturnPathRequired != b.ReturnPathRequired ||
                    !SequenceEqual(a.AllowedRouteTypes, b.AllowedRouteTypes)) return false;
            }
            return true;
        }

        private static bool SequenceEqual<T>(IEnumerable<T> left, IEnumerable<T> right) =>
            left.SequenceEqual(right);

        private static int ValidSectorIndex(SectorCoord coordinate)
        {
            if (coordinate.X < 0 || coordinate.X >= WorldGenConstants.SectorColumns ||
                coordinate.Y < 0 || coordinate.Y >= WorldGenConstants.SectorRows) return -1;
            return WorldGridIndex.ToIndex(coordinate);
        }

        private static IReadOnlyList<SitePlacementKey> SelectionKeys() => new[]
        {
            new SitePlacementKey(SiteReservationKind.Start, WorldId, 0),
            new SitePlacementKey(SiteReservationKind.Boss, BossId, 0),
            new SitePlacementKey(SiteReservationKind.Forge, ForgeId, 0),
            new SitePlacementKey(SiteReservationKind.CoreResource, CassiaId, 0),
            new SitePlacementKey(SiteReservationKind.CoreResource, YeastId, 0),
            new SitePlacementKey(SiteReservationKind.CoreResource, MeteorId, 0)
        };

        private static IReadOnlyList<SitePlacementKey> CapacityKeys() => new[]
        {
            new SitePlacementKey(SiteReservationKind.Forge, ForgeId, 0),
            new SitePlacementKey(SiteReservationKind.CoreResource, CassiaId, 0),
            new SitePlacementKey(SiteReservationKind.CoreResource, YeastId, 0),
            new SitePlacementKey(SiteReservationKind.CoreResource, MeteorId, 0)
        };

        private static bool Contains(IReadOnlyList<SitePlacementKey> values, SitePlacementKey key)
        {
            foreach (var value in values) if (value == key) return true;
            return false;
        }

        private static int CapacityOrder(SitePlacementKey key)
        {
            var expected = CapacityKeys();
            for (var index = 0; index < expected.Count; index++)
                if (expected[index] == key) return index;
            return int.MaxValue;
        }

        private static string Canonical(string value) =>
            SitePlacementKey.IsCanonicalId(value) ? value : string.Empty;

        private static void AddError(
            ICollection<CoreCapacityFloodError> errors,
            CoreCapacityFloodErrorCode code,
            string site,
            string biome,
            string rule,
            int sector,
            string message) => errors.Add(new CoreCapacityFloodError(
                code, Canonical(site), Canonical(biome), Canonical(rule),
                sector >= 0 && sector < WorldGenConstants.SectorCount ? sector : -1,
                message));

        private sealed class SiteWork
        {
            public SiteWork(CoreCapacityRequirement requirement)
            {
                Requirement = requirement;
                Footprint = requirement.Placement.OccupiedSectors
                    .Select(WorldGridIndex.ToIndex).OrderBy(value => value).ToList();
            }

            public CoreCapacityRequirement Requirement { get; }
            public IReadOnlyList<int> Footprint { get; }
            public SortedSet<int> MandatoryBuffer { get; } = new SortedSet<int>();
            public int OutsideTheoreticalBufferCount { get; set; }
            public int BlockedMandatoryBufferCount { get; set; }
            public int OverlappingMandatoryBufferCount { get; set; }
            public int RequiredWitnessSectorCount { get; set; }
            public int FloodVisitedSectorCount { get; set; }
            public int AvailableConnectedSectorCount { get; set; }
            public SortedSet<int> Reachable { get; set; } = new SortedSet<int>();
            public SortedSet<int> Witness { get; set; }
        }

        private readonly struct GridPoint : IEquatable<GridPoint>
        {
            private readonly int x;
            private readonly int y;

            public GridPoint(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public bool Equals(GridPoint other) => x == other.x && y == other.y;
            public override bool Equals(object obj) => obj is GridPoint other && Equals(other);
            public override int GetHashCode() => (x * 397) ^ y;
        }
    }
}
