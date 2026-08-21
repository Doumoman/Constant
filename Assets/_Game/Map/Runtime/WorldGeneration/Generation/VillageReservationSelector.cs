using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class VillageReservationSelector
    {
        private const string VillageProfileId = "VIL_MOON_PRIMARY";
        private const string WorldProfileId = "WORLD_MOONPALACE_V1";
        private const string VillageSpecialMapId = "SITE_PRIMARY_VILLAGE";
        private const string EntrySocketId = "ENTRY_L";

        public VillageReservationResult Reserve(
            CoreCapacityApproval coreCapacityApproval,
            VillageProfileDefinition villageProfile,
            SpecialMapDefinition villageSpecialMap,
            IEnumerable<SpecialMapEntrySocketDefinition> villageEntrySockets,
            IEnumerable<VillageLayoutDefinition> villageLayouts,
            DeterministicRngStream siteRng)
        {
            var errors = new List<VillageReservationError>();
            var entries = SnapshotEntries(villageEntrySockets, errors);
            var layouts = SnapshotLayouts(villageLayouts, errors);

            ValidateApproval(coreCapacityApproval, villageProfile, errors);
            var bucketCatalog = ValidateProfile(villageProfile, errors);
            ValidateSpecialMap(villageSpecialMap, errors);
            ValidateEntries(entries, villageSpecialMap, errors);
            ValidateLayouts(layouts, villageProfile, errors);
            if (siteRng == null)
                AddError(errors, VillageReservationErrorCode.MissingSiteRng,
                    string.Empty, -1, "The continued site RNG stream is required.");

            if (errors.Count != 0)
                return Invalid(errors);

            var rngBefore = siteRng.DrawCount;
            var bucketRoll = siteRng.NextInt(100);
            var bucket = bucketCatalog.SelectByRoll(bucketRoll);
            var evaluation = Evaluate(
                coreCapacityApproval, villageProfile, villageSpecialMap, layouts, bucket);

            if (evaluation.ViableCandidateCount == 0)
            {
                var diagnostics = new VillageReservationDiagnostics(
                    bucket, bucketRoll, evaluation.Diagnostics, 1,
                    rngBefore, siteRng.DrawCount, -1, -1);
                var rejection = new VillageReservationRejection(
                    VillageReservationRejectionReason.SelectedBucketHasNoViableCandidate,
                    bucket.BucketOrdinal, bucket.MinDistanceInclusive,
                    bucket.MaxDistanceInclusive, evaluation.SourceCandidateCount, 0,
                    "The selected distance bucket has no viable Village candidate.");
                return new VillageReservationResult(
                    VillageReservationStatus.ReservationRejected, null, diagnostics,
                    new[] { rejection }, Array.Empty<VillageReservationError>());
            }

            var viableLayouts = evaluation.Layouts.Where(item => item.Candidates.Count > 0).ToArray();
            var weightTotal = viableLayouts.Sum(item => item.Layout.SelectionWeight);
            var layoutRoll = siteRng.NextInt(weightTotal);
            var selectedLayout = SelectLayout(viableLayouts, layoutRoll);
            var candidateRoll = siteRng.NextInt(selectedLayout.Candidates.Count);
            var selectedCandidate = selectedLayout.Candidates[candidateRoll];
            var entry = entries[0];
            var selection = new VillageReservationSelection(
                villageProfile, villageSpecialMap, entry, selectedLayout.Layout,
                bucket, selectedCandidate, bucketRoll, layoutRoll, candidateRoll);
            var approval = new VillageReservationApproval(coreCapacityApproval, selection);
            var completedDiagnostics = new VillageReservationDiagnostics(
                bucket, bucketRoll, evaluation.Diagnostics, 3,
                rngBefore, siteRng.DrawCount, layoutRoll, candidateRoll);
            return new VillageReservationResult(
                VillageReservationStatus.Completed, approval, completedDiagnostics,
                Array.Empty<VillageReservationRejection>(), Array.Empty<VillageReservationError>());
        }

        private static VillageReservationResult Invalid(IEnumerable<VillageReservationError> errors) =>
            new VillageReservationResult(
                VillageReservationStatus.InvalidInput, null, null,
                Array.Empty<VillageReservationRejection>(), errors);

        private static List<SpecialMapEntrySocketDefinition> SnapshotEntries(
            IEnumerable<SpecialMapEntrySocketDefinition> source,
            ICollection<VillageReservationError> errors)
        {
            if (source == null)
            {
                AddError(errors, VillageReservationErrorCode.MissingEntrySockets,
                    string.Empty, -1, "Village entry sockets are required.");
                return new List<SpecialMapEntrySocketDefinition>();
            }
            return new List<SpecialMapEntrySocketDefinition>(source);
        }

        private static List<VillageLayoutDefinition> SnapshotLayouts(
            IEnumerable<VillageLayoutDefinition> source,
            ICollection<VillageReservationError> errors)
        {
            if (source == null)
            {
                AddError(errors, VillageReservationErrorCode.MissingLayouts,
                    string.Empty, -1, "Village layouts are required.");
                return new List<VillageLayoutDefinition>();
            }
            return new List<VillageLayoutDefinition>(source);
        }

        private static VillageDistanceBucketCatalog ValidateProfile(
            VillageProfileDefinition profile,
            ICollection<VillageReservationError> errors)
        {
            if (profile == null)
            {
                AddError(errors, VillageReservationErrorCode.MissingVillageProfile,
                    string.Empty, -1, "The Village profile is required.");
                return null;
            }
            if (!string.Equals(profile.VillageProfileId, VillageProfileId, StringComparison.Ordinal) ||
                !profile.Active ||
                !string.Equals(profile.WorldProfileId, WorldProfileId, StringComparison.Ordinal) ||
                profile.FacilityCountMin != 5 || profile.FacilityCountMax != 6 ||
                profile.MaximumSectorCount != 2)
                AddError(errors, VillageReservationErrorCode.InvalidVillageProfile,
                    CanonicalOrEmpty(profile.VillageProfileId), -1,
                    "The Village profile does not match the frozen active profile contract.");

            if (!VillageDistanceBucketCatalog.TryParse(
                    profile.StartDistanceBuckets, out var catalog, out _))
                AddError(errors, VillageReservationErrorCode.InvalidDistanceBuckets,
                    CanonicalOrEmpty(profile.VillageProfileId), -1,
                    "The Village distance buckets do not match the frozen starter grammar.");
            return catalog;
        }

        private static void ValidateSpecialMap(
            SpecialMapDefinition specialMap,
            ICollection<VillageReservationError> errors)
        {
            if (specialMap == null)
            {
                AddError(errors, VillageReservationErrorCode.MissingVillageSpecialMap,
                    string.Empty, -1, "The Village special-map definition is required.");
                return;
            }
            if (!string.Equals(specialMap.SpecialMapId, VillageSpecialMapId, StringComparison.Ordinal) ||
                !specialMap.Active ||
                !string.Equals(specialMap.SiteRole, "VILLAGE", StringComparison.Ordinal) ||
                specialMap.RequiredCount != 1 ||
                !string.Equals(specialMap.GenerationMode, "VILLAGE_LAYOUT", StringComparison.Ordinal) ||
                specialMap.PrimaryBiomeId.Length != 0 ||
                specialMap.MinGraphDistanceFromStart != 0 ||
                specialMap.MinGraphDistanceToOtherCoreSites != 2 ||
                !ExactRoutes(specialMap.AllowedEntryRouteTypes))
                AddError(errors, VillageReservationErrorCode.InvalidVillageSpecialMap,
                    CanonicalOrEmpty(specialMap.SpecialMapId), -1,
                    "The Village special-map definition does not match the frozen contract.");
        }

        private static void ValidateEntries(
            IReadOnlyList<SpecialMapEntrySocketDefinition> entries,
            SpecialMapDefinition specialMap,
            ICollection<VillageReservationError> errors)
        {
            if (entries.Count == 0)
            {
                AddError(errors, VillageReservationErrorCode.MissingEntrySockets,
                    string.Empty, -1, "Exactly one Village entry socket is required.");
                return;
            }
            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    AddError(errors, VillageReservationErrorCode.NullEntrySocket,
                        string.Empty, -1, "Village entry sockets cannot contain null.");
                    continue;
                }
                if (!string.Equals(entry.EntrySocketId, EntrySocketId, StringComparison.Ordinal) ||
                    (specialMap != null && !string.Equals(
                        entry.SpecialMapId, specialMap.SpecialMapId, StringComparison.Ordinal)))
                    AddError(errors, VillageReservationErrorCode.UnexpectedEntrySocket,
                        CanonicalOrEmpty(entry.EntrySocketId), -1,
                        "An unexpected Village entry socket was supplied.");
                if (!string.Equals(entry.SpecialMapId, VillageSpecialMapId, StringComparison.Ordinal) ||
                    entry.LocalSectorX != 0 || entry.LocalSectorY != 0 ||
                    !string.Equals(entry.Side, "L", StringComparison.Ordinal) ||
                    !entry.Required || !entry.ReturnPathRequired ||
                    !ExactRoutes(entry.AllowedRouteTypes))
                    AddError(errors, VillageReservationErrorCode.InvalidEntrySocket,
                        CanonicalOrEmpty(entry.EntrySocketId), -1,
                        "The Village entry socket does not match the frozen template.");
            }
            if (entries.Count != 1)
                AddError(errors, VillageReservationErrorCode.UnexpectedEntrySocket,
                    string.Empty, -1, "Exactly one Village entry socket must be supplied.");
        }

        private static void ValidateLayouts(
            IReadOnlyList<VillageLayoutDefinition> layouts,
            VillageProfileDefinition profile,
            ICollection<VillageReservationError> errors)
        {
            if (layouts.Count == 0)
                AddError(errors, VillageReservationErrorCode.MissingLayouts,
                    string.Empty, -1, "At least one allowed Village layout is required.");

            var supplied = new Dictionary<string, VillageLayoutDefinition>(StringComparer.Ordinal);
            foreach (var layout in layouts)
            {
                if (layout == null)
                {
                    AddError(errors, VillageReservationErrorCode.NullLayout,
                        string.Empty, -1, "Village layouts cannot contain null.");
                    continue;
                }
                var id = CanonicalOrEmpty(layout.VillageLayoutId);
                if (id.Length == 0 || !supplied.TryAdd(id, layout))
                    AddError(errors, VillageReservationErrorCode.DuplicateLayoutId,
                        id, -1, "Village layout IDs must be canonical and unique.");
            }

            if (profile == null) return;
            var allowed = new HashSet<string>(StringComparer.Ordinal);
            if (profile.AllowedLayoutIds == null || profile.AllowedLayoutIds.Count == 0)
                AddError(errors, VillageReservationErrorCode.InvalidVillageProfile,
                    CanonicalOrEmpty(profile.VillageProfileId), -1,
                    "The Village profile must allow at least one layout.");
            else
                foreach (var id in profile.AllowedLayoutIds)
                {
                    if (!SitePlacementKey.IsCanonicalId(id) || !allowed.Add(id))
                        AddError(errors, VillageReservationErrorCode.InvalidVillageProfile,
                            CanonicalOrEmpty(id), -1,
                            "Allowed Village layout IDs must be canonical and unique.");
                }

            foreach (var id in allowed)
                if (!supplied.ContainsKey(id))
                    AddError(errors, VillageReservationErrorCode.MissingAllowedLayout,
                        id, -1, "An allowed Village layout was not supplied.");
            foreach (var pair in supplied)
            {
                if (!allowed.Contains(pair.Key))
                    AddError(errors, VillageReservationErrorCode.UnexpectedLayout,
                        pair.Key, -1, "An unexpected Village layout was supplied.");
                ValidateLayout(pair.Value, profile, errors);
            }

            long weightTotal = 0;
            foreach (var pair in supplied)
                if (allowed.Contains(pair.Key) && pair.Value.SelectionWeight > 0)
                    weightTotal += pair.Value.SelectionWeight;
            if (weightTotal > int.MaxValue)
                AddError(errors, VillageReservationErrorCode.InvalidLayout,
                    string.Empty, -1, "The allowed Village layout weight total is too large.");
        }

        private static void ValidateLayout(
            VillageLayoutDefinition layout,
            VillageProfileDefinition profile,
            ICollection<VillageReservationError> errors)
        {
            var validDimensions =
                (layout.FootprintWidthSectors == 1 && layout.FootprintHeightSectors == 1) ||
                (layout.FootprintWidthSectors == 2 && layout.FootprintHeightSectors == 1) ||
                (layout.FootprintWidthSectors == 1 && layout.FootprintHeightSectors == 2);
            var sidesValid = layout.EntrySides != null && layout.EntrySides.Count > 0;
            var sides = new HashSet<SiteEntrySide>();
            if (sidesValid)
                foreach (var token in layout.EntrySides)
                {
                    if (!SiteReservationTokenCodec.TryParseEntrySide(token, out var side) || !sides.Add(side))
                        sidesValid = false;
                }
            if (!layout.Active || layout.SelectionWeight <= 0 ||
                layout.TargetFacilityCount < profile.FacilityCountMin ||
                layout.TargetFacilityCount > profile.FacilityCountMax ||
                layout.FootprintWidthSectors <= 0 || layout.FootprintHeightSectors <= 0 ||
                !validDimensions ||
                layout.FootprintWidthSectors * layout.FootprintHeightSectors > profile.MaximumSectorCount ||
                !sidesValid)
                AddError(errors, VillageReservationErrorCode.InvalidLayout,
                    CanonicalOrEmpty(layout.VillageLayoutId), -1,
                    "A Village layout violates the active dimensions, facilities, entry-side, or weight contract.");
        }

        private static void ValidateApproval(
            CoreCapacityApproval approval,
            VillageProfileDefinition profile,
            ICollection<VillageReservationError> errors)
        {
            if (approval == null)
            {
                AddError(errors, VillageReservationErrorCode.MissingCoreCapacityApproval,
                    string.Empty, -1, "A Core capacity approval is required.");
                return;
            }
            var plan = approval.SelectionPlan;
            if (plan == null || plan.SelectedCount != 6 || plan.Steps.Count != 6 ||
                plan.SelectedPlacements.Count != 6 || approval.CapacitySiteCount != 4 ||
                approval.Witnesses.Count != 4 || approval.TotalWitnessSectorCount != 20)
            {
                AddError(errors, VillageReservationErrorCode.InvalidCoreCapacityApproval,
                    string.Empty, -1, "The Core capacity approval does not have the frozen 6/4/20 shape.");
                return;
            }

            var expectedKinds = new[]
            {
                SiteReservationKind.Start, SiteReservationKind.Boss,
                SiteReservationKind.Forge, SiteReservationKind.CoreResource,
                SiteReservationKind.CoreResource, SiteReservationKind.CoreResource
            };
            var occupied = new HashSet<int>();
            for (var index = 0; index < plan.SelectedPlacements.Count; index++)
            {
                var placement = plan.SelectedPlacements[index];
                if (placement == null || placement.Candidate == null || placement.Footprint == null ||
                    placement.Candidate.Kind != expectedKinds[index] ||
                    SitePlacementKey.FromPlacement(placement) != plan.Steps[index].Key ||
                    (index == 0 && profile != null && !string.Equals(
                        placement.Candidate.SourceDefinitionId, profile.WorldProfileId,
                        StringComparison.Ordinal)))
                {
                    AddError(errors, VillageReservationErrorCode.InvalidSelectedPlacement,
                        placement == null || placement.Candidate == null
                            ? string.Empty : CanonicalOrEmpty(placement.Candidate.SourceDefinitionId),
                        -1, "A selected placement violates the frozen identity or order contract.");
                    continue;
                }
                foreach (var sector in placement.OccupiedSectors)
                {
                    int sectorIndex;
                    try { sectorIndex = WorldGridIndex.ToIndex(sector); }
                    catch (ArgumentOutOfRangeException)
                    {
                        AddError(errors, VillageReservationErrorCode.InvalidSelectedPlacement,
                            CanonicalOrEmpty(placement.Candidate.SourceDefinitionId), -1,
                            "A selected placement contains an invalid world sector.");
                        continue;
                    }
                    if (!occupied.Add(sectorIndex))
                        AddError(errors, VillageReservationErrorCode.InvalidSelectedPlacement,
                            CanonicalOrEmpty(placement.Candidate.SourceDefinitionId), sectorIndex,
                            "Selected placement footprints must not overlap.");
                }
            }

            var witnessClaimed = new HashSet<int>();
            for (var index = 0; index < approval.Witnesses.Count; index++)
            {
                var witness = approval.Witnesses[index];
                var expectedKey = plan.Steps[index + 2].Key;
                if (witness == null || witness.Key != expectedKey ||
                    witness.WitnessSectorIndices.Count != 5 ||
                    witness.RequiredWitnessSectorCount != 5)
                {
                    AddError(errors, VillageReservationErrorCode.InvalidCapacityWitness,
                        witness == null ? string.Empty : CanonicalOrEmpty(witness.Key.SourceDefinitionId),
                        -1, "A capacity witness violates the frozen identity or five-sector contract.");
                    continue;
                }
                var owner = plan.SelectedPlacements[index + 2];
                var ownerFootprint = owner.OccupiedSectors.Select(WorldGridIndex.ToIndex).OrderBy(value => value).ToArray();
                if (!ownerFootprint.SequenceEqual(witness.FootprintSectorIndices))
                    AddError(errors, VillageReservationErrorCode.InvalidCapacityWitness,
                        CanonicalOrEmpty(witness.Key.SourceDefinitionId), -1,
                        "A capacity witness does not belong to its selected placement footprint.");
                foreach (var sectorIndex in witness.WitnessSectorIndices)
                    if (!witnessClaimed.Add(sectorIndex))
                        AddError(errors, VillageReservationErrorCode.InvalidCapacityWitness,
                            CanonicalOrEmpty(witness.Key.SourceDefinitionId), sectorIndex,
                            "Capacity witness sectors must be pairwise disjoint.");
            }
        }

        private static Evaluation Evaluate(
            CoreCapacityApproval approval,
            VillageProfileDefinition profile,
            SpecialMapDefinition specialMap,
            IReadOnlyList<VillageLayoutDefinition> layouts,
            VillageDistanceBucket bucket)
        {
            var existingOccupied = new HashSet<int>();
            var existingApproaches = new HashSet<int>();
            var protectedWitness = new HashSet<int>();
            foreach (var placement in approval.SelectionPlan.SelectedPlacements)
            {
                foreach (var sector in placement.OccupiedSectors)
                    existingOccupied.Add(WorldGridIndex.ToIndex(sector));
                foreach (var entry in placement.Entries)
                    existingApproaches.Add(WorldGridIndex.ToIndex(entry.ExteriorSector));
            }
            foreach (var witness in approval.Witnesses)
                foreach (var sectorIndex in witness.WitnessSectorIndices)
                    protectedWitness.Add(sectorIndex);

            var start = approval.SelectionPlan.SelectedPlacements.Single(item =>
                item.Candidate.Kind == SiteReservationKind.Start);
            var otherSites = approval.SelectionPlan.SelectedPlacements.Where(item =>
                item.Candidate.Kind != SiteReservationKind.Start).ToArray();
            var layoutResults = new List<LayoutEvaluation>();
            var diagnostics = new List<VillageLayoutCandidateDiagnostics>();
            var globalCandidateOrdinal = 0;

            foreach (var layout in layouts.OrderBy(item => item.VillageLayoutId, StringComparer.Ordinal))
            {
                var sides = CanonicalSides(layout.EntrySides);
                var accumulator = new LayoutAccumulator(layout, sides.Count);
                var candidates = new List<VillageReservationCandidate>();
                for (var y = 0; y <= WorldGenConstants.SectorRows - layout.FootprintHeightSectors; y++)
                {
                    for (var x = 0; x <= WorldGenConstants.SectorColumns - layout.FootprintWidthSectors; x++)
                    {
                        var origin = new SectorCoord(x, y);
                        var originIndex = WorldGridIndex.ToIndex(origin);
                        var occupied = RectangleIndices(
                            origin, layout.FootprintWidthSectors, layout.FootprintHeightSectors);
                        foreach (var side in sides)
                        {
                            accumulator.Raw++;
                            EntryCoordinates(origin, layout.FootprintWidthSectors,
                                layout.FootprintHeightSectors, side,
                                out var entryFootprint, out var entryExterior);
                            if (!IsWorld(entryExterior))
                            {
                                accumulator.EntryOutside++;
                                continue;
                            }

                            accumulator.Source++;
                            var startDistance = MinimumDistance(occupied, start.OccupiedSectors);
                            var candidate = new VillageReservationCandidate(
                                profile.VillageProfileId, specialMap.SpecialMapId,
                                layout.VillageLayoutId, layout.SelectionWeight, origin,
                                originIndex, globalCandidateOrdinal++,
                                layout.FootprintWidthSectors, layout.FootprintHeightSectors,
                                occupied, side, WorldGridIndex.ToIndex(entryFootprint),
                                WorldGridIndex.ToIndex(entryExterior), startDistance,
                                bucket.BucketOrdinal);

                            if (Overlaps(occupied, existingOccupied)) accumulator.FootprintOverlap++;
                            else if (Overlaps(occupied, protectedWitness)) accumulator.ProtectedWitness++;
                            else if (Overlaps(occupied, existingApproaches)) accumulator.BlocksApproach++;
                            else if (existingOccupied.Contains(candidate.EntryExteriorSectorIndex))
                                accumulator.EntryOccupied++;
                            else if (otherSites.Any(site => MinimumDistance(
                                occupied, site.OccupiedSectors) < specialMap.MinGraphDistanceToOtherCoreSites))
                                accumulator.OtherDistance++;
                            else if (!bucket.Contains(startDistance)) accumulator.StartDistance++;
                            else
                            {
                                accumulator.Viable++;
                                candidates.Add(candidate);
                            }
                        }
                    }
                }
                layoutResults.Add(new LayoutEvaluation(layout, candidates));
                diagnostics.Add(accumulator.ToDiagnostics());
            }
            return new Evaluation(layoutResults, diagnostics);
        }

        private static LayoutEvaluation SelectLayout(
            IReadOnlyList<LayoutEvaluation> layouts,
            int roll)
        {
            var cumulative = 0;
            foreach (var layout in layouts)
            {
                cumulative += layout.Layout.SelectionWeight;
                if (roll < cumulative) return layout;
            }
            throw new InvalidOperationException("The layout roll is outside the viable weight table.");
        }

        private static IReadOnlyList<SiteEntrySide> CanonicalSides(IReadOnlyList<string> tokens)
        {
            var found = new HashSet<SiteEntrySide>();
            foreach (var token in tokens)
                if (SiteReservationTokenCodec.TryParseEntrySide(token, out var side)) found.Add(side);
            var result = new List<SiteEntrySide>();
            foreach (var side in new[]
                     { SiteEntrySide.L, SiteEntrySide.R, SiteEntrySide.U, SiteEntrySide.D })
                if (found.Contains(side)) result.Add(side);
            return new ReadOnlyCollection<SiteEntrySide>(result);
        }

        private static List<int> RectangleIndices(SectorCoord origin, int width, int height)
        {
            var result = new List<int>(width * height);
            for (var localY = 0; localY < height; localY++)
                for (var localX = 0; localX < width; localX++)
                    result.Add(WorldGridIndex.ToIndex(new SectorCoord(
                        origin.X + localX, origin.Y + localY)));
            result.Sort();
            return result;
        }

        private static void EntryCoordinates(
            SectorCoord origin,
            int width,
            int height,
            SiteEntrySide side,
            out SectorCoord footprint,
            out SectorCoord exterior)
        {
            int localX;
            int localY;
            switch (side)
            {
                case SiteEntrySide.L: localX = 0; localY = (height - 1) / 2; break;
                case SiteEntrySide.R: localX = width - 1; localY = (height - 1) / 2; break;
                case SiteEntrySide.D: localX = (width - 1) / 2; localY = 0; break;
                case SiteEntrySide.U: localX = (width - 1) / 2; localY = height - 1; break;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
            footprint = new SectorCoord(origin.X + localX, origin.Y + localY);
            SiteReservationTokenCodec.GetDelta(side, out var deltaX, out var deltaY);
            exterior = new SectorCoord(footprint.X + deltaX, footprint.Y + deltaY);
        }

        private static int MinimumDistance(
            IEnumerable<int> leftIndices,
            IEnumerable<SectorCoord> rightCoordinates)
        {
            var result = int.MaxValue;
            foreach (var leftIndex in leftIndices)
            {
                var left = WorldGridIndex.ToCoordinate(leftIndex);
                foreach (var right in rightCoordinates)
                    result = Math.Min(result, Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y));
            }
            return result;
        }

        private static bool Overlaps(IEnumerable<int> values, HashSet<int> set)
        {
            foreach (var value in values)
                if (set.Contains(value)) return true;
            return false;
        }

        private static bool IsWorld(SectorCoord coordinate) =>
            coordinate.X >= 0 && coordinate.X < WorldGenConstants.SectorColumns &&
            coordinate.Y >= 0 && coordinate.Y < WorldGenConstants.SectorRows;

        private static bool ExactRoutes(IReadOnlyList<int> routes) =>
            routes != null && routes.Count == 3 &&
            routes[0] == 1 && routes[1] == 2 && routes[2] == 3;

        private static string CanonicalOrEmpty(string value) =>
            SitePlacementKey.IsCanonicalId(value) ? value : string.Empty;

        private static void AddError(
            ICollection<VillageReservationError> target,
            VillageReservationErrorCode code,
            string definitionId,
            int sectorIndex,
            string message) => target.Add(new VillageReservationError(
                code, CanonicalOrEmpty(definitionId), sectorIndex, message));

        private sealed class LayoutAccumulator
        {
            private readonly VillageLayoutDefinition layout;

            public LayoutAccumulator(VillageLayoutDefinition layout, int sideCount)
            {
                this.layout = layout;
                var originCount = (WorldGenConstants.SectorColumns - layout.FootprintWidthSectors + 1) *
                                  (WorldGenConstants.SectorRows - layout.FootprintHeightSectors + 1);
                ExpectedRaw = originCount * sideCount;
            }

            public int ExpectedRaw { get; }
            public int Raw { get; set; }
            public int EntryOutside { get; set; }
            public int Source { get; set; }
            public int FootprintOverlap { get; set; }
            public int ProtectedWitness { get; set; }
            public int BlocksApproach { get; set; }
            public int EntryOccupied { get; set; }
            public int OtherDistance { get; set; }
            public int StartDistance { get; set; }
            public int Viable { get; set; }

            public VillageLayoutCandidateDiagnostics ToDiagnostics()
            {
                if (Raw != ExpectedRaw)
                    throw new InvalidOperationException("Raw Village candidate enumeration count changed.");
                return new VillageLayoutCandidateDiagnostics(
                    layout.VillageLayoutId, layout.SelectionWeight, Raw, EntryOutside,
                    Source, FootprintOverlap, ProtectedWitness, BlocksApproach,
                    EntryOccupied, OtherDistance, StartDistance, Viable);
            }
        }

        private sealed class LayoutEvaluation
        {
            public LayoutEvaluation(
                VillageLayoutDefinition layout,
                IEnumerable<VillageReservationCandidate> candidates)
            {
                Layout = layout;
                Candidates = new ReadOnlyCollection<VillageReservationCandidate>(
                    new List<VillageReservationCandidate>(candidates));
            }

            public VillageLayoutDefinition Layout { get; }
            public IReadOnlyList<VillageReservationCandidate> Candidates { get; }
        }

        private sealed class Evaluation
        {
            public Evaluation(
                IEnumerable<LayoutEvaluation> layouts,
                IEnumerable<VillageLayoutCandidateDiagnostics> diagnostics)
            {
                Layouts = new ReadOnlyCollection<LayoutEvaluation>(new List<LayoutEvaluation>(layouts));
                Diagnostics = new ReadOnlyCollection<VillageLayoutCandidateDiagnostics>(
                    new List<VillageLayoutCandidateDiagnostics>(diagnostics));
                SourceCandidateCount = Diagnostics.Sum(item => item.SourceCandidateCount);
                ViableCandidateCount = Diagnostics.Sum(item => item.ViableCandidateCount);
            }

            public IReadOnlyList<LayoutEvaluation> Layouts { get; }
            public IReadOnlyList<VillageLayoutCandidateDiagnostics> Diagnostics { get; }
            public int SourceCandidateCount { get; }
            public int ViableCandidateCount { get; }
        }
    }
}
