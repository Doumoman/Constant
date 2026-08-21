using System;
using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteCandidateCostCalculator
    {
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";
        private const string CraterBiomeId = "BIO_MOON_CRATER";
        private const string CassiaBiomeId = "BIO_CASSIA_ROOT";
        private const string MillBiomeId = "BIO_ABANDONED_MILL";
        private const string DoughBiomeId = "BIO_MOON_DOUGH";

        public SiteCandidateCostResult Calculate(
            FootprintPlacement candidate,
            SiteCandidateCostContext context,
            SpecialMapDefinition specialMap,
            BiomeTypeDefinition primaryBiome,
            BiomePatchRuleDefinition corePatchRule,
            SiteCandidateCostWeights weights)
        {
            var errors = new List<SiteCandidateCostError>();
            var candidateKey = default(SitePlacementKey);
            if (candidate == null)
            {
                Add(errors, SiteCandidateCostErrorCode.MissingCandidate, string.Empty, string.Empty, -1,
                    "A candidate placement is required.");
            }
            else if (!TryValidateCandidate(candidate, errors, out candidateKey))
            {
                candidateKey = default(SitePlacementKey);
            }

            if (context == null)
            {
                Add(errors, SiteCandidateCostErrorCode.MissingContext, Source(candidateKey), string.Empty, -1,
                    "A candidate-cost context is required.");
            }
            else
            {
                ValidateContext(context, candidateKey, errors);
            }

            if (weights == null)
            {
                Add(errors, SiteCandidateCostErrorCode.MissingWeights, Source(candidateKey), string.Empty, -1,
                    "Candidate-cost weights are required.");
            }
            else if (!ValidWeights(weights))
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidWeights, Source(candidateKey), string.Empty, -1,
                    "Candidate-cost weights must be non-negative.");
            }

            if (candidateKey.IsValid)
                ValidateTypedInputs(candidateKey, context, specialMap, primaryBiome, corePatchRule, errors);

            if (candidate != null && candidateKey.IsValid && context != null && context.DistancePolicy != null)
            {
                ValidateCandidateAgainstContext(candidate, candidateKey, context, errors);
                ValidateCoreSet(candidate, context.ExistingPlacements, errors);
            }

            if (errors.Count != 0) return SiteCandidateCostResult.Failure(errors);

            try
            {
                var altitudeUnits = 0;
                var edgeUnits = 0;
                var capacityUnits = 0;
                var requiredCoreSectorCount = 0;
                if (candidateKey.Kind != SiteReservationKind.Start)
                {
                    altitudeUnits = CalculateAltitudeUnits(candidate, primaryBiome);
                    edgeUnits = CalculateEdgeUnits(candidate, corePatchRule);
                    if (candidateKey.Kind == SiteReservationKind.CoreResource ||
                        candidateKey.Kind == SiteReservationKind.Forge)
                    {
                        requiredCoreSectorCount = corePatchRule.MinSectorCount;
                        if (context.HasFutureCoreCapacityEstimate)
                        {
                            capacityUnits = Math.Max(0, requiredCoreSectorCount -
                                context.FutureCoreAvailableSectorCount);
                        }
                    }
                }

                CalculateDistance(candidate, candidateKey, context, out var distanceUnits,
                    out var distanceChecked, out var distanceViolations);
                CalculateCluster(candidate, context.ExistingPlacements, out var clusterUnits,
                    out var clusterDetected, out var windowWidth, out var windowHeight);

                var breakdown = new SiteCandidateCostBreakdown(
                    candidateKey,
                    candidate.Candidate.OriginIndex,
                    candidate.Footprint.Transform,
                    altitudeUnits,
                    edgeUnits,
                    distanceUnits,
                    distanceChecked,
                    distanceViolations,
                    capacityUnits,
                    context.HasFutureCoreCapacityEstimate,
                    requiredCoreSectorCount,
                    context.FutureCoreAvailableSectorCount,
                    clusterUnits,
                    clusterDetected,
                    windowWidth,
                    windowHeight,
                    weights);
                return SiteCandidateCostResult.Success(breakdown);
            }
            catch (OverflowException)
            {
                return SiteCandidateCostResult.Failure(new[]
                {
                    Error(SiteCandidateCostErrorCode.CostOverflow, candidateKey.SourceDefinitionId,
                        string.Empty, -1, "The checked candidate cost overflowed Int64.")
                });
            }
        }

        private static bool TryValidateCandidate(
            FootprintPlacement candidate,
            ICollection<SiteCandidateCostError> errors,
            out SitePlacementKey key)
        {
            key = default(SitePlacementKey);
            var source = candidate.Candidate == null
                ? string.Empty
                : CanonicalOrEmpty(candidate.Candidate.SourceDefinitionId);
            if (candidate.Candidate == null || candidate.Footprint == null ||
                candidate.OccupiedSectors == null || candidate.OccupiedSectors.Count == 0 ||
                candidate.OccupiedSectors.Count != candidate.Footprint.Cells.Count)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidCandidate, source, string.Empty, -1,
                    "The candidate placement is incomplete.");
                return false;
            }
            try
            {
                key = SitePlacementKey.FromPlacement(candidate);
            }
            catch (ArgumentException)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidCandidate, source, string.Empty, -1,
                    "The candidate placement identity is invalid.");
                return false;
            }
            if (key.Kind == SiteReservationKind.Village)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidCandidate, key.SourceDefinitionId,
                    string.Empty, -1, "Village candidates are outside this cost contract.");
                return false;
            }
            return true;
        }

        private static void ValidateContext(
            SiteCandidateCostContext context,
            SitePlacementKey candidateKey,
            ICollection<SiteCandidateCostError> errors)
        {
            var candidateSource = Source(candidateKey);
            if (context.DistancePolicy == null)
            {
                Add(errors, SiteCandidateCostErrorCode.MissingDistancePolicy, candidateSource,
                    string.Empty, -1, "A site-distance policy is required.");
                return;
            }
            if (context.ExistingPlacements == null)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidExistingPlacement, candidateSource,
                    string.Empty, -1, "Existing placements are required.");
                return;
            }
            if (context.FutureCoreAvailableSectorCount < -1 ||
                context.FutureCoreAvailableSectorCount > WorldGenConstants.SectorCount)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidFutureCapacityEstimate,
                    candidateSource, string.Empty, -1,
                    "Future Core capacity must be unavailable or a world sector count.");
            }

            var keys = new HashSet<SitePlacementKey>();
            var owners = new Dictionary<int, SitePlacementKey>();
            foreach (var placement in context.ExistingPlacements)
            {
                if (placement == null || placement.Candidate == null || placement.Footprint == null ||
                    placement.OccupiedSectors == null || placement.OccupiedSectors.Count == 0)
                {
                    Add(errors, SiteCandidateCostErrorCode.InvalidExistingPlacement, candidateSource,
                        string.Empty, -1, "An existing placement is invalid.");
                    continue;
                }
                SitePlacementKey key;
                try { key = SitePlacementKey.FromPlacement(placement); }
                catch (ArgumentException)
                {
                    Add(errors, SiteCandidateCostErrorCode.InvalidExistingPlacement, candidateSource,
                        CanonicalOrEmpty(placement.Candidate.SourceDefinitionId), -1,
                        "An existing placement identity is invalid.");
                    continue;
                }
                if (!keys.Add(key))
                {
                    Add(errors, SiteCandidateCostErrorCode.DuplicateExistingPlacementKey,
                        candidateSource, key.SourceDefinitionId, -1,
                        "Existing placement keys must be unique.");
                }
                if (!Contains(context.DistancePolicy, key))
                {
                    Add(errors, SiteCandidateCostErrorCode.UnexpectedExistingKey,
                        candidateSource, key.SourceDefinitionId, -1,
                        "An existing placement key is outside the distance policy.");
                }
                foreach (var sector in placement.OccupiedSectors)
                {
                    if (!IsWorldSector(sector))
                    {
                        Add(errors, SiteCandidateCostErrorCode.InvalidExistingPlacement,
                            candidateSource, key.SourceDefinitionId, -1,
                            "An existing occupied sector is outside the world.");
                        continue;
                    }
                    var sectorIndex = WorldGridIndex.ToIndex(sector);
                    if (owners.TryGetValue(sectorIndex, out var owner) && owner != key)
                    {
                        Add(errors, SiteCandidateCostErrorCode.OverlappingPlacement,
                            candidateSource, key.SourceDefinitionId, sectorIndex,
                            "Existing placements cannot overlap.");
                    }
                    else if (!owners.ContainsKey(sectorIndex))
                    {
                        owners.Add(sectorIndex, key);
                    }
                }
            }
        }

        private static void ValidateTypedInputs(
            SitePlacementKey candidateKey,
            SiteCandidateCostContext context,
            SpecialMapDefinition specialMap,
            BiomeTypeDefinition primaryBiome,
            BiomePatchRuleDefinition corePatchRule,
            ICollection<SiteCandidateCostError> errors)
        {
            var source = candidateKey.SourceDefinitionId;
            if (candidateKey.Kind == SiteReservationKind.Start)
            {
                if (specialMap != null)
                    Add(errors, SiteCandidateCostErrorCode.UnexpectedSpecialMap, source, string.Empty, -1,
                        "Start candidates cannot receive a special-map definition.");
                if (primaryBiome != null)
                    Add(errors, SiteCandidateCostErrorCode.UnexpectedPrimaryBiome, source, string.Empty, -1,
                        "Start candidates cannot receive a primary-biome definition.");
                if (corePatchRule != null)
                    Add(errors, SiteCandidateCostErrorCode.UnexpectedCorePatchRule, source, string.Empty, -1,
                        "Start candidates cannot receive a Core patch rule.");
                if (context != null && context.FutureCoreAvailableSectorCount != -1)
                    Add(errors, SiteCandidateCostErrorCode.InvalidFutureCapacityEstimate,
                        source, string.Empty, -1, "Start candidates require an unavailable capacity estimate.");
                return;
            }

            if (specialMap == null)
                Add(errors, SiteCandidateCostErrorCode.MissingSpecialMap, source, string.Empty, -1,
                    "A special-map definition is required.");
            else
                ValidateSpecialMap(candidateKey, specialMap, errors);

            if (primaryBiome == null)
                Add(errors, SiteCandidateCostErrorCode.MissingPrimaryBiome, source, string.Empty, -1,
                    "A primary-biome definition is required.");
            else
                ValidatePrimaryBiome(candidateKey, specialMap, primaryBiome, errors);

            if (corePatchRule == null)
                Add(errors, SiteCandidateCostErrorCode.MissingCorePatchRule, source, string.Empty, -1,
                    "An active Core patch rule is required.");
            else
                ValidateCorePatchRule(candidateKey, primaryBiome, corePatchRule, errors);

            if (context != null && candidateKey.Kind == SiteReservationKind.Boss &&
                context.FutureCoreAvailableSectorCount != -1)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidFutureCapacityEstimate,
                    source, string.Empty, -1, "Boss candidates require an unavailable capacity estimate.");
            }
        }

        private static void ValidateSpecialMap(
            SitePlacementKey candidateKey,
            SpecialMapDefinition specialMap,
            ICollection<SiteCandidateCostError> errors)
        {
            var source = candidateKey.SourceDefinitionId;
            if (!specialMap.Active || !SitePlacementKey.IsCanonicalId(specialMap.SpecialMapId) ||
                specialMap.RequiredCount != 1)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidSpecialMap, source, string.Empty, -1,
                    "The special-map definition must be active with one required instance.");
            }
            if (!string.Equals(specialMap.SpecialMapId, source, StringComparison.Ordinal) ||
                !SiteReservationTokenCodec.TryParseKind(specialMap.SiteRole, out var kind) ||
                kind != candidateKey.Kind || candidateKey.RequiredInstanceOrdinal != 0)
            {
                Add(errors, SiteCandidateCostErrorCode.SourceIdentityMismatch, source, string.Empty, -1,
                    "Candidate and special-map source, kind, and ordinal must match.");
            }
            if (!TryGetStarter(candidateKey.SourceDefinitionId, out var expectedKind,
                    out var expectedBiomeId, out _, out _, out _, out _ ) ||
                expectedKind != candidateKey.Kind ||
                !string.Equals(specialMap.PrimaryBiomeId, expectedBiomeId, StringComparison.Ordinal))
            {
                Add(errors, SiteCandidateCostErrorCode.SourceIdentityMismatch, source, string.Empty, -1,
                    "The candidate must match the frozen starter site-to-biome identity.");
            }
        }

        private static void ValidatePrimaryBiome(
            SitePlacementKey candidateKey,
            SpecialMapDefinition specialMap,
            BiomeTypeDefinition primaryBiome,
            ICollection<SiteCandidateCostError> errors)
        {
            var source = candidateKey.SourceDefinitionId;
            if (!primaryBiome.Active || !SitePlacementKey.IsCanonicalId(primaryBiome.BiomeId) ||
                primaryBiome.PreferredAltitudeMinSectorY < 0 ||
                primaryBiome.PreferredAltitudeMinSectorY > primaryBiome.PreferredAltitudeMaxSectorY ||
                primaryBiome.PreferredAltitudeMaxSectorY >= WorldGenConstants.SectorRows)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidPrimaryBiome, source, string.Empty, -1,
                    "The primary biome must be active with a valid preferred altitude band.");
            }
            if (specialMap == null ||
                !string.Equals(specialMap.PrimaryBiomeId, primaryBiome.BiomeId, StringComparison.Ordinal))
            {
                Add(errors, SiteCandidateCostErrorCode.SourceIdentityMismatch, source, string.Empty, -1,
                    "The special-map and primary-biome identities must match.");
            }
            if (TryGetStarter(source, out _, out var expectedBiomeId, out var minY, out var maxY,
                    out _, out _) &&
                (!string.Equals(primaryBiome.BiomeId, expectedBiomeId, StringComparison.Ordinal) ||
                 primaryBiome.PreferredAltitudeMinSectorY != minY ||
                 primaryBiome.PreferredAltitudeMaxSectorY != maxY))
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidPrimaryBiome, source, string.Empty, -1,
                    "The primary biome must match the frozen starter altitude definition.");
            }
        }

        private static void ValidateCorePatchRule(
            SitePlacementKey candidateKey,
            BiomeTypeDefinition primaryBiome,
            BiomePatchRuleDefinition corePatchRule,
            ICollection<SiteCandidateCostError> errors)
        {
            var source = candidateKey.SourceDefinitionId;
            if (!corePatchRule.Active || !SitePlacementKey.IsCanonicalId(corePatchRule.PatchRuleId) ||
                !string.Equals(corePatchRule.PatchRole, "CORE", StringComparison.Ordinal) ||
                corePatchRule.MinSectorCount <= 0 || corePatchRule.BufferRingSectors < 0)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidCorePatchRule, source, string.Empty, -1,
                    "The Core patch rule must be active and have valid Core limits.");
            }
            if (primaryBiome == null ||
                !string.Equals(corePatchRule.BiomeId, primaryBiome.BiomeId, StringComparison.Ordinal))
            {
                Add(errors, SiteCandidateCostErrorCode.SourceIdentityMismatch, source, string.Empty, -1,
                    "The Core patch rule and primary-biome identities must match.");
            }
            if (TryGetStarter(source, out _, out var expectedBiomeId, out _, out _,
                    out var minimumSectors, out var canTouchEdge) &&
                (!string.Equals(corePatchRule.BiomeId, expectedBiomeId, StringComparison.Ordinal) ||
                 corePatchRule.MinSectorCount != minimumSectors ||
                 corePatchRule.CanTouchWorldEdge != canTouchEdge ||
                 corePatchRule.BufferRingSectors != 1))
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidCorePatchRule, source, string.Empty, -1,
                    "The Core patch rule must match the frozen starter definition.");
            }
        }

        private static void ValidateCandidateAgainstContext(
            FootprintPlacement candidate,
            SitePlacementKey candidateKey,
            SiteCandidateCostContext context,
            ICollection<SiteCandidateCostError> errors)
        {
            var policy = context.DistancePolicy;
            if (!Contains(policy, candidateKey))
            {
                Add(errors, SiteCandidateCostErrorCode.MissingPolicyKey,
                    candidateKey.SourceDefinitionId, string.Empty, -1,
                    "The candidate key is missing from the distance policy.");
            }

            foreach (var existing in context.ExistingPlacements)
            {
                if (existing == null || existing.Candidate == null) continue;
                var existingKey = SitePlacementKey.FromPlacement(existing);
                if (existingKey == candidateKey)
                {
                    Add(errors, SiteCandidateCostErrorCode.CandidateAlreadyPlaced,
                        candidateKey.SourceDefinitionId, existingKey.SourceDefinitionId, -1,
                        "The candidate key is already placed.");
                }
                else if (!policy.TryGetConstraint(candidateKey, existingKey, out _))
                {
                    Add(errors, SiteCandidateCostErrorCode.MissingDistanceConstraint,
                        candidateKey.SourceDefinitionId, existingKey.SourceDefinitionId, -1,
                        "The candidate-existing pair requires a distance constraint.");
                }
                foreach (var candidateSector in candidate.OccupiedSectors)
                {
                    foreach (var existingSector in existing.OccupiedSectors)
                    {
                        if (candidateSector != existingSector) continue;
                        Add(errors, SiteCandidateCostErrorCode.OverlappingPlacement,
                            candidateKey.SourceDefinitionId, existingKey.SourceDefinitionId,
                            WorldGridIndex.ToIndex(candidateSector),
                            "Candidate and existing placements cannot overlap.");
                    }
                }
            }
        }

        private static void ValidateCoreSet(
            FootprintPlacement candidate,
            IReadOnlyList<FootprintPlacement> existing,
            ICollection<SiteCandidateCostError> errors)
        {
            if (candidate.Candidate.Kind != SiteReservationKind.CoreResource) return;
            var source = candidate.Candidate.SourceDefinitionId;
            var sources = new HashSet<string>(StringComparer.Ordinal);
            var coreCount = 0;
            foreach (var placement in existing)
            {
                if (placement.Candidate.Kind != SiteReservationKind.CoreResource) continue;
                coreCount++;
                if (!sources.Add(placement.Candidate.SourceDefinitionId))
                {
                    Add(errors, SiteCandidateCostErrorCode.InvalidCoreResourceSet, source,
                        placement.Candidate.SourceDefinitionId, -1,
                        "Required Core source IDs must be unique.");
                }
            }
            coreCount++;
            if (!sources.Add(source))
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidCoreResourceSet, source, source, -1,
                    "Required Core source IDs must be unique.");
            }
            if (coreCount > 3)
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidCoreResourceSet, source, string.Empty, -1,
                    "At most three required Core sites may participate in clustering.");
            }
            if (coreCount == 3 &&
                (!sources.Contains(CassiaId) || !sources.Contains(YeastId) || !sources.Contains(MeteorId)))
            {
                Add(errors, SiteCandidateCostErrorCode.InvalidCoreResourceSet, source, string.Empty, -1,
                    "The exact three required Core sources are required.");
            }
        }

        private static int CalculateAltitudeUnits(
            FootprintPlacement candidate,
            BiomeTypeDefinition biome)
        {
            var maximum = 0;
            foreach (var sector in candidate.OccupiedSectors)
            {
                var distance = sector.Y < biome.PreferredAltitudeMinSectorY
                    ? biome.PreferredAltitudeMinSectorY - sector.Y
                    : sector.Y > biome.PreferredAltitudeMaxSectorY
                        ? sector.Y - biome.PreferredAltitudeMaxSectorY
                        : 0;
                maximum = Math.Max(maximum, distance);
            }
            return maximum;
        }

        private static int CalculateEdgeUnits(
            FootprintPlacement candidate,
            BiomePatchRuleDefinition rule)
        {
            var actual = int.MaxValue;
            foreach (var sector in candidate.OccupiedSectors)
            {
                actual = Math.Min(actual, Math.Min(
                    Math.Min(sector.X, WorldGenConstants.SectorColumns - 1 - sector.X),
                    Math.Min(sector.Y, WorldGenConstants.SectorRows - 1 - sector.Y)));
            }
            var required = rule.CanTouchWorldEdge ? 0 : rule.BufferRingSectors;
            return Math.Max(0, required - actual);
        }

        private static void CalculateDistance(
            FootprintPlacement candidate,
            SitePlacementKey candidateKey,
            SiteCandidateCostContext context,
            out int units,
            out int checkedCount,
            out int violationCount)
        {
            units = 0;
            checkedCount = 0;
            violationCount = 0;
            foreach (var existing in context.ExistingPlacements)
            {
                var existingKey = SitePlacementKey.FromPlacement(existing);
                context.DistancePolicy.TryGetConstraint(candidateKey, existingKey, out var constraint);
                var actual = MinimumDistance(candidate, existing);
                var deficit = Math.Max(0, constraint.MinimumDistance - actual);
                checked { units += deficit; }
                checkedCount++;
                if (deficit > 0) violationCount++;
            }
        }

        private static int MinimumDistance(FootprintPlacement first, FootprintPlacement second)
        {
            var minimum = int.MaxValue;
            foreach (var firstSector in first.OccupiedSectors)
            {
                foreach (var secondSector in second.OccupiedSectors)
                {
                    minimum = Math.Min(minimum,
                        Math.Abs(firstSector.X - secondSector.X) +
                        Math.Abs(firstSector.Y - secondSector.Y));
                }
            }
            return minimum;
        }

        private static void CalculateCluster(
            FootprintPlacement candidate,
            IReadOnlyList<FootprintPlacement> existing,
            out int units,
            out bool detected,
            out int width,
            out int height)
        {
            units = 0;
            detected = false;
            width = -1;
            height = -1;
            if (candidate.Candidate.Kind != SiteReservationKind.CoreResource) return;

            var corePlacements = new List<FootprintPlacement>();
            foreach (var placement in existing)
            {
                if (placement.Candidate.Kind == SiteReservationKind.CoreResource)
                    corePlacements.Add(placement);
            }
            corePlacements.Add(candidate);
            if (corePlacements.Count != 3) return;

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            foreach (var placement in corePlacements)
            {
                foreach (var sector in placement.OccupiedSectors)
                {
                    minX = Math.Min(minX, sector.X);
                    minY = Math.Min(minY, sector.Y);
                    maxX = Math.Max(maxX, sector.X);
                    maxY = Math.Max(maxY, sector.Y);
                }
            }
            width = maxX - minX + 1;
            height = maxY - minY + 1;
            detected = width <= 4 && height <= 4;
            units = detected ? 1 : 0;
        }

        private static bool TryGetStarter(
            string sourceId,
            out SiteReservationKind kind,
            out string biomeId,
            out int minimumY,
            out int maximumY,
            out int minimumCoreSectors,
            out bool canTouchEdge)
        {
            switch (sourceId)
            {
                case MeteorId:
                    kind = SiteReservationKind.CoreResource; biomeId = CraterBiomeId;
                    minimumY = 0; maximumY = 7; minimumCoreSectors = 5; canTouchEdge = true; return true;
                case CassiaId:
                    kind = SiteReservationKind.CoreResource; biomeId = CassiaBiomeId;
                    minimumY = 2; maximumY = 12; minimumCoreSectors = 5; canTouchEdge = false; return true;
                case YeastId:
                    kind = SiteReservationKind.CoreResource; biomeId = DoughBiomeId;
                    minimumY = 0; maximumY = 7; minimumCoreSectors = 5; canTouchEdge = true; return true;
                case ForgeId:
                    kind = SiteReservationKind.Forge; biomeId = MillBiomeId;
                    minimumY = 1; maximumY = 11; minimumCoreSectors = 4; canTouchEdge = false; return true;
                case BossId:
                    kind = SiteReservationKind.Boss; biomeId = MillBiomeId;
                    minimumY = 1; maximumY = 11; minimumCoreSectors = 4; canTouchEdge = false; return true;
                default:
                    kind = default(SiteReservationKind); biomeId = string.Empty;
                    minimumY = 0; maximumY = 0; minimumCoreSectors = 0; canTouchEdge = false; return false;
            }
        }

        private static bool ValidWeights(SiteCandidateCostWeights weights) =>
            weights.AltitudePerSector >= 0 && weights.EdgeClearanceDeficit >= 0 &&
            weights.DistanceDeficit >= 0 && weights.FutureCoreCapacityShortfall >= 0 &&
            weights.CoreCluster >= 0;

        private static bool Contains(SiteDistancePolicy policy, SitePlacementKey key)
        {
            foreach (var policyKey in policy.Keys)
            {
                if (policyKey == key) return true;
            }
            return false;
        }

        private static bool IsWorldSector(SectorCoord sector) =>
            sector.X >= 0 && sector.X < WorldGenConstants.SectorColumns &&
            sector.Y >= 0 && sector.Y < WorldGenConstants.SectorRows;
        private static string Source(SitePlacementKey key) => key.IsValid ? key.SourceDefinitionId : string.Empty;
        private static string CanonicalOrEmpty(string value) =>
            SitePlacementKey.IsCanonicalId(value) ? value : string.Empty;
        private static SiteCandidateCostError Error(
            SiteCandidateCostErrorCode code,
            string candidate,
            string existing,
            int sector,
            string message) => new SiteCandidateCostError(code, candidate, existing, sector, message);
        private static void Add(
            ICollection<SiteCandidateCostError> errors,
            SiteCandidateCostErrorCode code,
            string candidate,
            string existing,
            int sector,
            string message) => errors.Add(Error(code, candidate, existing, sector, message));
    }
}
