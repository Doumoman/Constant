using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatchValidator
    {
        public BiomePatchValidationResult Validate(
            BiomePatchExportResult exportResult,
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> patchRules,
            IEnumerable<BiomeBoundaryProfileDefinition> boundaryProfiles,
            IEnumerable<BiomeBoundaryPairRuleDefinition> boundaryPairRules)
        {
            var errors = new List<BiomePatchValidationError>();
            var publication = ValidateExport(exportResult, errors);
            var biomes = BuildExactMap(
                biomeTypes, ExpectedBiomeIds(), value => value.BiomeId, value => value.Active,
                BiomePatchValidationErrorCode.MissingBiomeTypes, "biome", errors);
            var rules = BuildExactMap(
                patchRules, ExpectedPatchRuleIds(), value => value.PatchRuleId, value => value.Active,
                BiomePatchValidationErrorCode.MissingPatchRules, "patch_rule", errors);
            var profiles = BuildExactMap(
                boundaryProfiles, ExpectedBoundaryProfileIds(), value => value.BoundaryProfileId, value => value.Active,
                BiomePatchValidationErrorCode.MissingBoundaryProfiles, "boundary_profile", errors);
            var pairs = BuildExactMap(
                boundaryPairRules, ExpectedBoundaryPairRuleIds(), value => value.BoundaryPairRuleId, value => value.Active,
                BiomePatchValidationErrorCode.MissingBoundaryPairRules, "boundary_pair", errors);

            ValidateDefinitions(biomes, rules, profiles, pairs, errors);
            if (errors.Count != 0)
                return BiomePatchValidationResult.Invalid(errors);

            var context = new ValidationContext(publication, biomes, rules, profiles, pairs);
            var violations = new List<BiomePatchValidationViolation>();
            var checkedCounts = new Dictionary<BiomePatchValidationRule, int>();
            try
            {
                foreach (BiomePatchValidationRule rule in Enum.GetValues(typeof(BiomePatchValidationRule)))
                    checkedCounts[rule] = EvaluateRule(rule, context, violations);
            }
            catch (Exception exception)
            {
                errors.Add(new BiomePatchValidationError(
                    BiomePatchValidationErrorCode.InternalInvariantViolation,
                    exception.GetType().Name,
                    -1,
                    "Validation rule execution failed."));
                return BiomePatchValidationResult.Invalid(errors);
            }

            var ordered = BiomePatchValidationResult.SortAndDedupeViolations(violations);
            var ruleResults = new List<BiomePatchValidationRuleResult>();
            foreach (BiomePatchValidationRule rule in Enum.GetValues(typeof(BiomePatchValidationRule)))
            {
                ruleResults.Add(new BiomePatchValidationRuleResult(
                    rule,
                    checkedCounts[rule],
                    ordered.Count(value => value.Rule == rule)));
            }

            var diagnostics = BuildDiagnostics(context, ruleResults, ordered);
            if (ordered.Count != 0)
                return BiomePatchValidationResult.Rejected(diagnostics, ordered);

            var approved = new BiomePatchValidationPublication(publication, diagnostics);
            return BiomePatchValidationResult.Completed(approved, diagnostics);
        }

        private static BiomePatchExportPublication ValidateExport(
            BiomePatchExportResult exportResult,
            ICollection<BiomePatchValidationError> errors)
        {
            if (exportResult == null)
            {
                AddError(errors, BiomePatchValidationErrorCode.MissingExportResult,
                    "export", "Export result is required.");
                return null;
            }
            if (exportResult.Status != BiomePatchExportStatus.Completed || !exportResult.Succeeded)
                AddError(errors, BiomePatchValidationErrorCode.ExportNotCompleted,
                    "export", "Export result must be completed.");
            var publication = exportResult.Publication;
            if (publication == null)
            {
                AddError(errors, BiomePatchValidationErrorCode.MissingExportPublication,
                    "export", "Export publication is required.");
                return null;
            }

            try
            {
                var snapshot = publication.SourceCleanup == null ? null : publication.SourceCleanup.Snapshot;
                var sourceWorld = publication.SourceWorld;
                var world = publication.WorldWithBiomeAssignments;
                var sourceIntrusion = publication.SourceCleanup == null
                    ? null
                    : publication.SourceCleanup.SourceIntrusion;
                var siteSnapshot = sourceIntrusion == null || sourceIntrusion.Publication == null
                    ? null
                    : sourceIntrusion.Publication.SourceSiteSnapshot;
                if (snapshot == null || sourceWorld == null || world == null || siteSnapshot == null)
                {
                    AddError(errors, BiomePatchValidationErrorCode.InvalidExportPublication,
                        "source_chain", "Export source chain is incomplete.");
                    return publication;
                }
                if (snapshot.Seed != sourceWorld.Seed || snapshot.Seed != world.Seed ||
                    snapshot.Seed != siteSnapshot.Seed)
                    AddError(errors, BiomePatchValidationErrorCode.InvalidExportPublication,
                        "seed", "Export source chain must share one seed.");
                if (snapshot.Sectors.Count != WorldGenConstants.SectorCount ||
                    sourceWorld.Cells.Count != WorldGenConstants.SectorCount ||
                    world.Cells.Count != WorldGenConstants.SectorCount ||
                    siteSnapshot.Sectors.Count != WorldGenConstants.SectorCount)
                    AddError(errors, BiomePatchValidationErrorCode.InvalidExportPublication,
                        "world_rows", "Export source chain must contain exactly 169 rows.");
                if (publication.PatchRows == null ||
                    publication.PatchRows.Count != snapshot.Patches.Count ||
                    publication.PatchRowCount != publication.PatchRows.Count ||
                    publication.WorldSectorRowCount != WorldGenConstants.SectorCount)
                    AddError(errors, BiomePatchValidationErrorCode.InvalidExportPublication,
                        "publication_counts", "Export publication counts are inconsistent.");
                var patchBytes = publication.GeneratedBiomePatchesCsv;
                var worldBytes = publication.GeneratedWorldSectorsCsv;
                if (patchBytes == null || patchBytes.Length == 0 || worldBytes == null || worldBytes.Length == 0)
                    AddError(errors, BiomePatchValidationErrorCode.InvalidExportPublication,
                        "csv_bytes", "Export CSV bytes are required.");
            }
            catch (Exception)
            {
                AddError(errors, BiomePatchValidationErrorCode.InvalidExportPublication,
                    "export", "Export publication cannot be inspected.");
            }
            return publication;
        }

        private static Dictionary<string, T> BuildExactMap<T>(
            IEnumerable<T> source,
            IReadOnlyList<string> expectedIds,
            Func<T, string> idSelector,
            Func<T, bool> activeSelector,
            BiomePatchValidationErrorCode missingCollectionCode,
            string kind,
            ICollection<BiomePatchValidationError> errors)
            where T : class
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            if (source == null)
            {
                AddError(errors, missingCollectionCode, kind, kind + " definitions are required.");
                foreach (var expected in expectedIds)
                    AddError(errors, BiomePatchValidationErrorCode.MissingDefinition,
                        expected, "Required " + kind + " definition is missing.");
                return result;
            }

            try
            {
                foreach (var definition in source)
                {
                    if (definition == null)
                    {
                        AddError(errors, BiomePatchValidationErrorCode.NullDefinition,
                            kind, kind + " definitions cannot contain null.");
                        continue;
                    }
                    var id = idSelector(definition);
                    if (string.IsNullOrEmpty(id))
                    {
                        AddError(errors, BiomePatchValidationErrorCode.InvalidDefinition,
                            kind, kind + " definition ID is invalid.");
                        continue;
                    }
                    if (!result.TryAdd(id, definition))
                        AddError(errors, BiomePatchValidationErrorCode.DuplicateDefinition,
                            id, "Definition ID is duplicated.");
                    if (!activeSelector(definition))
                        AddError(errors, BiomePatchValidationErrorCode.InactiveDefinition,
                            id, "Required definition must be active.");
                }
            }
            catch (Exception)
            {
                AddError(errors, BiomePatchValidationErrorCode.InvalidDefinition,
                    kind, kind + " definitions cannot be enumerated.");
            }

            var expectedSet = new HashSet<string>(expectedIds, StringComparer.Ordinal);
            foreach (var expected in expectedIds)
                if (!result.ContainsKey(expected))
                    AddError(errors, BiomePatchValidationErrorCode.MissingDefinition,
                        expected, "Required " + kind + " definition is missing.");
            foreach (var id in result.Keys)
                if (!expectedSet.Contains(id))
                    AddError(errors, BiomePatchValidationErrorCode.UnexpectedDefinition,
                        id, "Unexpected " + kind + " definition is not allowed.");
            return result;
        }

        private static void ValidateDefinitions(
            IReadOnlyDictionary<string, BiomeTypeDefinition> biomes,
            IReadOnlyDictionary<string, BiomePatchRuleDefinition> rules,
            IReadOnlyDictionary<string, BiomeBoundaryProfileDefinition> profiles,
            IReadOnlyDictionary<string, BiomeBoundaryPairRuleDefinition> pairs,
            ICollection<BiomePatchValidationError> errors)
        {
            foreach (var biome in biomes.Values)
            {
                if (!biome.Required || biome.MinPatchCount < 0 ||
                    biome.MaxPatchCount < biome.MinPatchCount || biome.MinCorePatchCount < 1)
                    AddError(errors, BiomePatchValidationErrorCode.InvalidDefinition,
                        biome.BiomeId, "Biome count contract is invalid.");
            }

            foreach (var rule in rules.Values)
            {
                if (!biomes.ContainsKey(rule.BiomeId) ||
                    !BiomePatchRoleTokenCodec.TryParse(rule.PatchRole, out _) ||
                    rule.MinSectorCount < 1 || rule.MaxSectorCount < rule.MinSectorCount ||
                    rule.MinSeedDistance < 0 || rule.SeedCountMin < 0 ||
                    rule.SeedCountMax < rule.SeedCountMin)
                    AddError(errors, BiomePatchValidationErrorCode.InvalidDefinition,
                        rule.PatchRuleId, "Patch rule contract is invalid.");
                if (!IsFiniteShare(rule.MaxWorldShare))
                    AddError(errors, BiomePatchValidationErrorCode.InvalidShareDefinition,
                        rule.PatchRuleId, "Patch share must be finite and within (0,1].");
            }

            foreach (var biomeId in ExpectedBiomeIds())
            {
                var shares = rules.Values.Where(value =>
                {
                    if (!string.Equals(value.BiomeId, biomeId, StringComparison.Ordinal)) return false;
                    return BiomePatchRoleTokenCodec.TryParse(value.PatchRole, out var role) &&
                           role != BiomePatchRole.Intrusion;
                }).Select(value => value.MaxWorldShare).Distinct().ToArray();
                if (shares.Length != 1)
                    AddError(errors, BiomePatchValidationErrorCode.InvalidShareDefinition,
                        biomeId, "Normal patch rules must agree on one world-share cap.");
            }

            foreach (var profile in profiles.Values)
                if (string.IsNullOrEmpty(profile.BoundaryType))
                    AddError(errors, BiomePatchValidationErrorCode.InvalidDefinition,
                        profile.BoundaryProfileId, "Boundary profile type is required.");
            if (profiles.TryGetValue("BOUND_TUNNEL", out var tunnel) &&
                !string.Equals(tunnel.BoundaryType, "TUNNEL_INTRUSION", StringComparison.Ordinal))
                AddError(errors, BiomePatchValidationErrorCode.InvalidDefinition,
                    tunnel.BoundaryProfileId, "BOUND_TUNNEL must use TUNNEL_INTRUSION.");

            foreach (var pair in pairs.Values)
            {
                if (!biomes.ContainsKey(pair.BiomeAId) || !biomes.ContainsKey(pair.BiomeBId) ||
                    string.Equals(pair.BiomeAId, pair.BiomeBId, StringComparison.Ordinal) ||
                    pair.AllowedBoundaryProfileIds == null || pair.BoundaryProfileWeights == null ||
                    pair.AllowedBoundaryProfileIds.Count == 0 ||
                    pair.AllowedBoundaryProfileIds.Count != pair.BoundaryProfileWeights.Count ||
                    !ContainsOrdinal(pair.AllowedBoundaryProfileIds, pair.DefaultBoundaryProfileId))
                {
                    AddError(errors, BiomePatchValidationErrorCode.InvalidDefinition,
                        pair.BoundaryPairRuleId, "Boundary pair contract is invalid.");
                    continue;
                }
                foreach (var profileId in pair.AllowedBoundaryProfileIds)
                    if (!profiles.ContainsKey(profileId))
                        AddError(errors, BiomePatchValidationErrorCode.InvalidDefinition,
                            pair.BoundaryPairRuleId, "Boundary pair references an unknown profile.");
            }
        }

        private static int EvaluateRule(
            BiomePatchValidationRule rule,
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            switch (rule)
            {
                case BiomePatchValidationRule.RequiredBiomeCoverage:
                    return ValidateRequiredBiomeCoverage(context, violations);
                case BiomePatchValidationRule.PatchDefinitionIdentity:
                    return ValidatePatchDefinitionIdentity(context, violations);
                case BiomePatchValidationRule.PatchSizeLimits:
                    return ValidatePatchSizeLimits(context, violations);
                case BiomePatchValidationRule.PatchConnectivity:
                    return ValidatePatchConnectivity(context, violations);
                case BiomePatchValidationRule.PatchSeedContract:
                    return ValidatePatchSeedContract(context, violations);
                case BiomePatchValidationRule.NormalPatchCountRange:
                    return ValidateNormalPatchCountRange(context, violations);
                case BiomePatchValidationRule.PatchRuleCountRange:
                    return ValidatePatchRuleCountRange(context, violations);
                case BiomePatchValidationRule.SameRuleSeedDistance:
                    return ValidateSameRuleSeedDistance(context, violations);
                case BiomePatchValidationRule.WorldEdgePolicy:
                    return ValidateWorldEdgePolicy(context, violations);
                case BiomePatchValidationRule.WorldShareLimits:
                    return ValidateWorldShareLimits(context, violations);
                case BiomePatchValidationRule.CoreSiteOwnership:
                    return ValidateCoreSiteOwnership(context, violations);
                case BiomePatchValidationRule.ReservationAssignment:
                    return ValidateReservationAssignment(context, violations);
                case BiomePatchValidationRule.OwnershipExclusivity:
                    return ValidateOwnershipExclusivity(context, violations);
                case BiomePatchValidationRule.IntrusionBoundaryContract:
                    return ValidateIntrusionBoundaryContract(context, violations);
                case BiomePatchValidationRule.ExportReproducibility:
                    return ValidateExportReproducibility(context, violations);
                default:
                    throw new ArgumentOutOfRangeException(nameof(rule));
            }
        }

        private static int ValidateRequiredBiomeCoverage(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            foreach (var biome in context.Biomes.Values)
            {
                var actual = context.Snapshot.Patches.Count(value =>
                    value.Role == BiomePatchRole.Core &&
                    string.Equals(value.BiomeId, biome.BiomeId, StringComparison.Ordinal));
                if (actual < biome.MinCorePatchCount)
                    AddViolation(violations, BiomePatchValidationRule.RequiredBiomeCoverage,
                        biome.BiomeId, "", -1, AtLeast(biome.MinCorePatchCount), Number(actual),
                        "Required biome has too few Core patches.");
            }
            return context.Biomes.Count;
        }

        private static int ValidatePatchDefinitionIdentity(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            foreach (var patch in context.Snapshot.Patches)
            {
                if (!context.Rules.TryGetValue(patch.PatchRuleId, out var rule) ||
                    !BiomePatchRoleTokenCodec.TryParse(rule.PatchRole, out var role) ||
                    !string.Equals(patch.BiomeId, rule.BiomeId, StringComparison.Ordinal) ||
                    patch.Role != role)
                    AddViolation(violations, BiomePatchValidationRule.PatchDefinitionIdentity,
                        patch.BiomeId, patch.Id.Value, -1, "exact rule biome/role identity",
                        patch.PatchRuleId, "Patch identity does not match its definition.");
            }
            return context.Snapshot.Patches.Count;
        }

        private static int ValidatePatchSizeLimits(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            foreach (var patch in context.Snapshot.Patches)
            {
                if (!context.Rules.TryGetValue(patch.PatchRuleId, out var rule)) continue;
                var valid = patch.SectorCount >= rule.MinSectorCount &&
                            patch.SectorCount <= rule.MaxSectorCount;
                if (patch.Role == BiomePatchRole.Intrusion)
                    valid = valid && patch.SectorCount == 1 && rule.AllowSingleSector;
                else
                    valid = valid && patch.SectorCount >= 2 && patch.SectorCount <= 59 &&
                            !rule.AllowSingleSector;
                if (!valid)
                    AddViolation(violations, BiomePatchValidationRule.PatchSizeLimits,
                        patch.BiomeId, patch.Id.Value, -1,
                        patch.Role == BiomePatchRole.Intrusion
                            ? "exactly 1 and single-sector enabled"
                            : Number(rule.MinSectorCount) + ".." + Number(Math.Min(59, rule.MaxSectorCount)),
                        Number(patch.SectorCount), "Patch size is outside its contract.");
            }
            return context.Snapshot.Patches.Count;
        }

        private static int ValidatePatchConnectivity(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            foreach (var patch in context.Snapshot.Patches)
                if (!IsConnected(patch.SectorIndices))
                    AddViolation(violations, BiomePatchValidationRule.PatchConnectivity,
                        patch.BiomeId, patch.Id.Value, -1, "one cardinal component", "disconnected",
                        "Patch sectors are not cardinal connected.");
            return context.Snapshot.Patches.Count;
        }

        private static int ValidatePatchSeedContract(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            var checkedCount = 0;
            foreach (var patch in context.Snapshot.Patches)
            {
                if (patch.Seeds.Count == 0)
                    AddViolation(violations, BiomePatchValidationRule.PatchSeedContract,
                        patch.BiomeId, patch.Id.Value, -1, "at least one seed", "0",
                        "Patch has no seed.");
                foreach (var seed in patch.Seeds)
                {
                    checkedCount++;
                    var valid = seed != null && patch.ContainsSector(seed.SectorIndex) &&
                                seed.SectorIndex >= 0 && seed.SectorIndex < WorldGenConstants.SectorCount &&
                                seed.Sector.X == seed.SectorIndex % WorldGenConstants.SectorColumns &&
                                seed.Sector.Y == seed.SectorIndex / WorldGenConstants.SectorColumns &&
                                seed.Role == patch.Role;
                    if (seed != null && patch.Role == BiomePatchRole.Core)
                        valid = valid && seed.SourceSiteReservationId.HasValue && seed.SourceSiteReservationId.Value.IsValid;
                    else if (seed != null)
                        valid = valid && !seed.SourceSiteReservationId.HasValue;
                    if (!valid)
                        AddViolation(violations, BiomePatchValidationRule.PatchSeedContract,
                            patch.BiomeId, patch.Id.Value, seed == null ? -1 : seed.SectorIndex,
                            "contained row-major role/source seed", "invalid",
                            "Patch seed contract is invalid.");
                }
            }
            return checkedCount;
        }

        private static int ValidateNormalPatchCountRange(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            foreach (var biome in context.Biomes.Values)
            {
                var actual = context.Snapshot.Patches.Count(value =>
                    string.Equals(value.BiomeId, biome.BiomeId, StringComparison.Ordinal) &&
                    value.Role != BiomePatchRole.Intrusion);
                if (actual < biome.MinPatchCount || actual > biome.MaxPatchCount)
                    AddViolation(violations, BiomePatchValidationRule.NormalPatchCountRange,
                        biome.BiomeId, "", -1,
                        Number(biome.MinPatchCount) + ".." + Number(biome.MaxPatchCount),
                        Number(actual), "Normal patch count is outside the biome range.");
            }
            return context.Biomes.Count;
        }

        private static int ValidatePatchRuleCountRange(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            foreach (var rule in context.Rules.Values)
            {
                var actual = context.Snapshot.Patches.Count(value =>
                    string.Equals(value.PatchRuleId, rule.PatchRuleId, StringComparison.Ordinal));
                if (actual < rule.SeedCountMin || actual > rule.SeedCountMax)
                    AddViolation(violations, BiomePatchValidationRule.PatchRuleCountRange,
                        rule.BiomeId, "", -1,
                        Number(rule.SeedCountMin) + ".." + Number(rule.SeedCountMax),
                        Number(actual), "Patch-rule instance count is outside its range.");
            }
            return context.Rules.Count;
        }

        private static int ValidateSameRuleSeedDistance(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            var checkedCount = 0;
            foreach (var rule in context.Rules.Values)
            {
                var patches = context.Snapshot.Patches.Where(value =>
                    string.Equals(value.PatchRuleId, rule.PatchRuleId, StringComparison.Ordinal)).ToArray();
                for (var left = 0; left < patches.Length; left++)
                for (var right = left + 1; right < patches.Length; right++)
                {
                    checkedCount++;
                    var distance = MinimumSeedDistance(patches[left], patches[right]);
                    if (distance < rule.MinSeedDistance)
                        AddViolation(violations, BiomePatchValidationRule.SameRuleSeedDistance,
                            rule.BiomeId, patches[right].Id.Value, -1,
                            AtLeast(rule.MinSeedDistance), Number(distance),
                            "Same-rule patch seeds are too close.");
                }
            }
            return checkedCount;
        }

        private static int ValidateWorldEdgePolicy(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            foreach (var patch in context.Snapshot.Patches)
            {
                if (!context.Rules.TryGetValue(patch.PatchRuleId, out var rule) || rule.CanTouchWorldEdge)
                    continue;
                foreach (var sectorIndex in patch.SectorIndices)
                {
                    var x = sectorIndex % WorldGenConstants.SectorColumns;
                    var y = sectorIndex / WorldGenConstants.SectorColumns;
                    if (x == 0 || y == 0 || x == WorldGenConstants.SectorColumns - 1 ||
                        y == WorldGenConstants.SectorRows - 1)
                        AddViolation(violations, BiomePatchValidationRule.WorldEdgePolicy,
                            patch.BiomeId, patch.Id.Value, sectorIndex, "no world-edge cell",
                            x.ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture),
                            "Patch touches the world edge against its rule.");
                }
            }
            return context.Snapshot.Patches.Count;
        }

        private static int ValidateWorldShareLimits(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            var checkedCount = 0;
            foreach (var biome in context.Biomes.Values)
            {
                var normalRule = context.Rules.Values.First(value =>
                    string.Equals(value.BiomeId, biome.BiomeId, StringComparison.Ordinal) &&
                    !string.Equals(value.PatchRole, "INTRUSION", StringComparison.Ordinal));
                var cap = (int)Math.Floor(WorldGenConstants.SectorCount * normalRule.MaxWorldShare);
                var actual = context.Snapshot.Sectors.Count(value =>
                    value.IsAssigned && string.Equals(value.PrimaryBiomeId, biome.BiomeId, StringComparison.Ordinal));
                checkedCount++;
                if (actual > cap)
                    AddViolation(violations, BiomePatchValidationRule.WorldShareLimits,
                        biome.BiomeId, "", -1, "<=" + Number(cap), Number(actual),
                        "Biome exceeds its world-share cap.");
            }
            foreach (var rule in context.Rules.Values.Where(value =>
                         string.Equals(value.PatchRole, "INTRUSION", StringComparison.Ordinal)))
            {
                var cap = (int)Math.Floor(WorldGenConstants.SectorCount * rule.MaxWorldShare);
                var actual = context.Snapshot.Patches.Where(value =>
                    string.Equals(value.PatchRuleId, rule.PatchRuleId, StringComparison.Ordinal))
                    .Sum(value => value.SectorCount);
                checkedCount++;
                if (actual > cap)
                    AddViolation(violations, BiomePatchValidationRule.WorldShareLimits,
                        rule.BiomeId, "", -1, "<=" + Number(cap), Number(actual),
                        "Intrusion rule exceeds its world-share cap.");
            }
            return checkedCount;
        }

        private static int ValidateCoreSiteOwnership(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            var bindings = context.Snapshot.SiteBindings.ToDictionary(
                value => value.SiteReservationId, value => value);
            var checkedCount = 0;
            foreach (var binding in context.Snapshot.SiteBindings)
            {
                checkedCount++;
                BiomePatch patch = null;
                SiteReservation reservation = null;
                var valid = context.Snapshot.TryGetPatch(binding.PatchId, out patch) &&
                            patch.Role == BiomePatchRole.Core &&
                            string.Equals(binding.BiomeId, patch.BiomeId, StringComparison.Ordinal) &&
                            context.SiteSnapshot.TryGetReservation(binding.SiteReservationId, out reservation) &&
                            string.Equals(reservation.PrimaryBiomeId, binding.BiomeId, StringComparison.Ordinal);
                if (valid)
                {
                    var occupied = reservation.OccupiedSectors.Select(ToIndex).OrderBy(value => value).ToArray();
                    valid = occupied.SequenceEqual(binding.OccupiedSectorIndices);
                }
                if (!valid)
                    AddViolation(violations, BiomePatchValidationRule.CoreSiteOwnership,
                        binding.BiomeId, binding.PatchId.Value, -1, "exact Core reservation binding",
                        binding.SiteReservationId.Value, "Core site binding does not match its reservation.");

                foreach (var sectorIndex in binding.OccupiedSectorIndices)
                {
                    var ownership = context.Snapshot.GetSector(sectorIndex);
                    var seedMatches = patch == null ? 0 : patch.Seeds.Count(value =>
                        value.SectorIndex == sectorIndex && value.SourceSiteReservationId.HasValue &&
                        value.SourceSiteReservationId.Value == binding.SiteReservationId);
                    if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                        ownership.PatchId.Value != binding.PatchId || seedMatches != 1)
                        AddViolation(violations, BiomePatchValidationRule.CoreSiteOwnership,
                            binding.BiomeId, binding.PatchId.Value, sectorIndex,
                            "one matching Core seed and ownership", Number(seedMatches),
                            "Bound Core site sector is misowned.");
                }
            }

            foreach (var patch in context.Snapshot.Patches.Where(value => value.Role == BiomePatchRole.Core))
            foreach (var seed in patch.Seeds)
            {
                checkedCount++;
                if (!seed.SourceSiteReservationId.HasValue ||
                    !bindings.TryGetValue(seed.SourceSiteReservationId.Value, out var binding) ||
                    binding.PatchId != patch.Id || !Contains(binding.OccupiedSectorIndices, seed.SectorIndex))
                    AddViolation(violations, BiomePatchValidationRule.CoreSiteOwnership,
                        patch.BiomeId, patch.Id.Value, seed.SectorIndex,
                        "one reverse binding", "missing", "Core seed has no exact reverse binding.");
            }
            return checkedCount;
        }

        private static int ValidateReservationAssignment(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            var bindings = context.Snapshot.SiteBindings.ToDictionary(
                value => value.SiteReservationId, value => value);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var reservation = context.SiteSnapshot.GetSector(index);
                var ownership = context.Snapshot.GetSector(index);
                if (!reservation.IsReserved && !ownership.IsAssigned)
                    AddViolation(violations, BiomePatchValidationRule.ReservationAssignment,
                        "", "", index, "assigned P01-unreserved sector", "unassigned",
                        "Unreserved sector is unassigned.");
                if (!ownership.IsAssigned)
                {
                    var allowed = reservation.IsReserved && reservation.Kind.HasValue &&
                                  !IsCoreReservation(reservation.Kind.Value);
                    if (!allowed)
                        AddViolation(violations, BiomePatchValidationRule.ReservationAssignment,
                            "", "", index, "non-Core reserved footprint", "unassigned",
                            "Unassigned sector is not an allowed reserved footprint.");
                }
                if (reservation.IsReserved && reservation.Kind.HasValue &&
                    IsCoreReservation(reservation.Kind.Value))
                {
                    var valid = ownership.IsAssigned && reservation.ReservationId.HasValue &&
                                bindings.TryGetValue(reservation.ReservationId.Value, out var binding) &&
                                ownership.PatchId.HasValue && ownership.PatchId.Value == binding.PatchId &&
                                Contains(binding.OccupiedSectorIndices, index);
                    if (!valid)
                        AddViolation(violations, BiomePatchValidationRule.ReservationAssignment,
                            ownership.PrimaryBiomeId, ownership.PatchId.HasValue ? ownership.PatchId.Value.Value : "",
                            index, "matching Core reservation ownership", "mismatch",
                            "Core reservation footprint is not assigned to its Core patch.");
                }
            }
            return WorldGenConstants.SectorCount;
        }

        private static int ValidateOwnershipExclusivity(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            var memberships = new Dictionary<int, BiomePatch>();
            var patchSectorSum = 0;
            foreach (var patch in context.Snapshot.Patches)
            {
                patchSectorSum += patch.SectorCount;
                foreach (var sectorIndex in patch.SectorIndices)
                {
                    if (!memberships.TryAdd(sectorIndex, patch))
                        AddViolation(violations, BiomePatchValidationRule.OwnershipExclusivity,
                            patch.BiomeId, patch.Id.Value, sectorIndex, "one patch membership", "overlap",
                            "Sector belongs to more than one patch.");
                    var ownership = context.Snapshot.GetSector(sectorIndex);
                    if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                        ownership.PatchId.Value != patch.Id ||
                        !string.Equals(ownership.PrimaryBiomeId, patch.BiomeId, StringComparison.Ordinal))
                        AddViolation(violations, BiomePatchValidationRule.OwnershipExclusivity,
                            patch.BiomeId, patch.Id.Value, sectorIndex, "exact ownership row", "mismatch",
                            "Patch membership has no exact ownership.");
                }
            }

            var assigned = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var ownership = context.Snapshot.GetSector(index);
                if (ownership.SecondaryBiomeId.Length != 0)
                    AddViolation(violations, BiomePatchValidationRule.OwnershipExclusivity,
                        ownership.PrimaryBiomeId,
                        ownership.PatchId.HasValue ? ownership.PatchId.Value.Value : "",
                        index, "empty SecondaryBiomeId", ownership.SecondaryBiomeId,
                        "MAP04 ownership cannot contain a secondary biome.");
                if (!ownership.IsAssigned) continue;
                assigned++;
                if (!ownership.PatchId.HasValue ||
                    !context.Snapshot.TryGetPatch(ownership.PatchId.Value, out var patch) ||
                    !patch.ContainsSector(index) ||
                    !string.Equals(ownership.PrimaryBiomeId, patch.BiomeId, StringComparison.Ordinal))
                    AddViolation(violations, BiomePatchValidationRule.OwnershipExclusivity,
                        ownership.PrimaryBiomeId,
                        ownership.PatchId.HasValue ? ownership.PatchId.Value.Value : "",
                        index, "existing matching patch", "orphan",
                        "Assigned ownership is orphaned or mismatched.");
            }
            if (patchSectorSum != assigned || assigned != context.Snapshot.AssignedSectorCount ||
                assigned + context.Snapshot.UnassignedSectorCount != WorldGenConstants.SectorCount)
                AddViolation(violations, BiomePatchValidationRule.OwnershipExclusivity,
                    "", "", -1, "patch sum = assigned and conservation 169",
                    Number(patchSectorSum) + "/" + Number(assigned),
                    "Patch and ownership counts do not conserve the world.");
            return WorldGenConstants.SectorCount + context.Snapshot.Patches.Count;
        }

        private static int ValidateIntrusionBoundaryContract(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            var intrusions = context.Snapshot.Patches.Where(value =>
                value.Role == BiomePatchRole.Intrusion).ToArray();
            foreach (var intrusion in intrusions)
            {
                var valid = intrusion.SectorCount == 1;
                var hasAnchor = false;
                var hasHost = false;
                var sectorIndex = intrusion.SectorIndices[0];
                foreach (var neighborIndex in Neighbors(sectorIndex))
                {
                    var ownership = context.Snapshot.GetSector(neighborIndex);
                    if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                        !context.Snapshot.TryGetPatch(ownership.PatchId.Value, out var neighborPatch) ||
                        neighborPatch.Role == BiomePatchRole.Intrusion)
                        continue;
                    if (string.Equals(neighborPatch.BiomeId, intrusion.BiomeId, StringComparison.Ordinal))
                        hasAnchor = true;
                    else if (IsAllowedDirectedIntrusion(intrusion.BiomeId, neighborPatch.BiomeId) &&
                             PairAllowsTunnel(context.Pairs.Values, intrusion.BiomeId, neighborPatch.BiomeId))
                        hasHost = true;
                }
                valid = valid && hasAnchor && hasHost &&
                        context.Profiles.TryGetValue("BOUND_TUNNEL", out var tunnel) && tunnel.Active &&
                        string.Equals(tunnel.BoundaryType, "TUNNEL_INTRUSION", StringComparison.Ordinal);
                if (!valid)
                    AddViolation(violations, BiomePatchValidationRule.IntrusionBoundaryContract,
                        intrusion.BiomeId, intrusion.Id.Value, sectorIndex,
                        "one-cell anchor + allowed foreign tunnel host",
                        "anchor=" + hasAnchor + ",host=" + hasHost,
                        "Intrusion boundary contract is invalid.");
            }
            return intrusions.Length;
        }

        private static int ValidateExportReproducibility(
            ValidationContext context,
            ICollection<BiomePatchValidationViolation> violations)
        {
            var publication = context.Publication;
            var snapshot = context.Snapshot;
            var checkedCount = 0;
            if (publication.SourceWorld.Seed != snapshot.Seed ||
                publication.WorldWithBiomeAssignments.Seed != snapshot.Seed ||
                context.SiteSnapshot.Seed != snapshot.Seed)
                AddViolation(violations, BiomePatchValidationRule.ExportReproducibility,
                    "", "", -1, "one source-chain seed", "mismatch",
                    "Export source-chain seeds differ.");

            var rowsById = new Dictionary<BiomePatchId, GeneratedBiomePatchRow>();
            foreach (var row in publication.PatchRows)
            {
                checkedCount++;
                if (row == null || !rowsById.TryAdd(row.PatchInstanceId, row) ||
                    !snapshot.TryGetPatch(row.PatchInstanceId, out var patch) ||
                    !RowMatchesSnapshot(row, patch, snapshot))
                    AddViolation(violations, BiomePatchValidationRule.ExportReproducibility,
                        row == null ? "" : row.BiomeId,
                        row == null ? "" : row.PatchInstanceId.Value,
                        -1, "exact derived patch row", "mismatch",
                        "Generated patch row is not reproducible from the snapshot.");
            }
            foreach (var patch in snapshot.Patches)
                if (!rowsById.ContainsKey(patch.Id))
                    AddViolation(violations, BiomePatchValidationRule.ExportReproducibility,
                        patch.BiomeId, patch.Id.Value, -1, "one generated patch row", "missing",
                        "Snapshot patch has no generated row.");

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                checkedCount++;
                var ownership = snapshot.GetSector(index);
                var cell = publication.WorldWithBiomeAssignments.Cells[index];
                var valid = cell.Index == index && cell.Coordinate == ownership.Sector;
                if (ownership.IsAssigned)
                    valid = valid && ownership.PatchId.HasValue &&
                            string.Equals(cell.PrimaryBiomeId, ownership.PrimaryBiomeId, StringComparison.Ordinal) &&
                            string.Equals(cell.SecondaryBiomeId, ownership.SecondaryBiomeId, StringComparison.Ordinal) &&
                            string.Equals(cell.PatchId, ownership.PatchId.Value.Value, StringComparison.Ordinal);
                else
                    valid = valid && cell.PrimaryBiomeId.Length == 0 &&
                            cell.SecondaryBiomeId.Length == 0 && cell.PatchId.Length == 0;
                if (!valid)
                    AddViolation(violations, BiomePatchValidationRule.ExportReproducibility,
                        ownership.PrimaryBiomeId,
                        ownership.PatchId.HasValue ? ownership.PatchId.Value.Value : "",
                        index, "world fields equal ownership", "mismatch",
                        "Generated world row is not reproducible from the snapshot.");
            }

            byte[] patchBytes;
            byte[] worldBytes;
            try
            {
                patchBytes = GeneratedBiomePatchCsvSerializer.Serialize(publication.PatchRows);
                worldBytes = GeneratedWorldDataCsvSerializer.Serialize(publication.WorldWithBiomeAssignments);
            }
            catch (Exception)
            {
                patchBytes = Array.Empty<byte>();
                worldBytes = Array.Empty<byte>();
            }
            if (!BytesEqual(patchBytes, context.PatchBytes) ||
                !HasExactCsvShape(context.PatchBytes, GeneratedBiomePatchCsvSerializer.Header,
                    publication.PatchRows.Count + 1))
                AddViolation(violations, BiomePatchValidationRule.ExportReproducibility,
                    "", "", -1, "exact generated_biome_patches.csv bytes", "mismatch",
                    "Patch CSV bytes are not reproducible.");
            if (!BytesEqual(worldBytes, context.WorldBytes) ||
                !HasExactCsvShape(context.WorldBytes, GeneratedWorldDataCsvSerializer.Header,
                    WorldGenConstants.SectorCount + 1))
                AddViolation(violations, BiomePatchValidationRule.ExportReproducibility,
                    "", "", -1, "exact generated_world_sectors.csv bytes", "mismatch",
                    "World CSV bytes are not reproducible.");
            if (!string.Equals(publication.BiomePatchFileName,
                    GeneratedBiomePatchCsvSerializer.FileName, StringComparison.Ordinal) ||
                !string.Equals(publication.WorldSectorFileName,
                    GeneratedWorldDataCsvSerializer.FileName, StringComparison.Ordinal) ||
                publication.PatchRowCount != snapshot.Patches.Count ||
                publication.WorldSectorRowCount != WorldGenConstants.SectorCount ||
                publication.AssignedSectorCount != snapshot.AssignedSectorCount ||
                publication.UnassignedSectorCount != snapshot.UnassignedSectorCount)
                AddViolation(violations, BiomePatchValidationRule.ExportReproducibility,
                    "", "", -1, "exact filenames and conserved counts", "mismatch",
                    "Export publication metadata is inconsistent.");
            return checkedCount + 4;
        }

        private static BiomePatchValidationDiagnostics BuildDiagnostics(
            ValidationContext context,
            IEnumerable<BiomePatchValidationRuleResult> ruleResults,
            IReadOnlyList<BiomePatchValidationViolation> violations)
        {
            var patches = context.Snapshot.Patches;
            var patchSectorSum = patches.Sum(value => value.SectorCount);
            var unassignedNonReserved = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                if (context.Snapshot.GetSector(index).IsAssigned) continue;
                var reservation = context.SiteSnapshot.GetSector(index);
                if (!reservation.IsReserved || !reservation.Kind.HasValue ||
                    IsCoreReservation(reservation.Kind.Value))
                    unassignedNonReserved++;
            }
            return new BiomePatchValidationDiagnostics(
                context.Snapshot.Seed,
                ruleResults,
                violations,
                patches.Count,
                patches.Count(value => value.Role == BiomePatchRole.Core),
                patches.Count(value => value.Role == BiomePatchRole.Satellite),
                patches.Count(value => value.Role == BiomePatchRole.Intrusion),
                context.Snapshot.AssignedSectorCount,
                context.Snapshot.UnassignedSectorCount,
                patchSectorSum,
                context.Biomes.Values.Count(value => value.Required),
                context.Snapshot.SiteBindings.Count,
                patches.Count == 0 ? 0 : patches.Max(value => value.SectorCount),
                violations.Count(value => value.Rule == BiomePatchValidationRule.PatchConnectivity),
                CountPatchOverlaps(patches),
                CountOwnershipOrphans(context.Snapshot),
                unassignedNonReserved,
                violations.Count(value => value.Rule == BiomePatchValidationRule.CoreSiteOwnership ||
                                          value.Rule == BiomePatchValidationRule.ReservationAssignment),
                violations.Count(value => value.Rule == BiomePatchValidationRule.IntrusionBoundaryContract),
                context.Publication.PatchRowCount,
                context.Publication.WorldSectorRowCount,
                context.PatchBytes.Length,
                context.WorldBytes.Length,
                0,
                0);
        }

        private static bool RowMatchesSnapshot(
            GeneratedBiomePatchRow row,
            BiomePatch patch,
            BiomePatchSnapshot snapshot)
        {
            if (row.Seed != snapshot.Seed || row.PatchInstanceId != patch.Id ||
                !string.Equals(row.BiomeId, patch.BiomeId, StringComparison.Ordinal) ||
                row.PatchRole != patch.Role || row.SectorCount != patch.SectorCount)
                return false;
            var representative = patch.Seeds.Min(value => value.SectorIndex);
            if (row.SeedSectorX != representative % WorldGenConstants.SectorColumns ||
                row.SeedSectorY != representative / WorldGenConstants.SectorColumns ||
                row.MinX != patch.SectorIndices.Min(value => value % WorldGenConstants.SectorColumns) ||
                row.MinY != patch.SectorIndices.Min(value => value / WorldGenConstants.SectorColumns) ||
                row.MaxX != patch.SectorIndices.Max(value => value % WorldGenConstants.SectorColumns) ||
                row.MaxY != patch.SectorIndices.Max(value => value / WorldGenConstants.SectorColumns) ||
                row.PerimeterEdges != Perimeter(patch.SectorIndices))
                return false;
            var expectedSites = snapshot.SiteBindings.Where(value => value.PatchId == patch.Id)
                .Select(value => value.SiteReservationId).Distinct().OrderBy(value => value).ToArray();
            return expectedSites.SequenceEqual(row.SpecialMapInstanceIds);
        }

        private static bool IsConnected(IReadOnlyList<int> sectors)
        {
            if (sectors == null || sectors.Count == 0) return false;
            var members = new HashSet<int>(sectors);
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(sectors[0]);
            visited.Add(sectors[0]);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var next in Neighbors(current))
                    if (members.Contains(next) && visited.Add(next)) queue.Enqueue(next);
            }
            return visited.Count == members.Count;
        }

        private static IEnumerable<int> Neighbors(int index)
        {
            var x = index % WorldGenConstants.SectorColumns;
            var y = index / WorldGenConstants.SectorColumns;
            if (x > 0) yield return index - 1;
            if (x < WorldGenConstants.SectorColumns - 1) yield return index + 1;
            if (y > 0) yield return index - WorldGenConstants.SectorColumns;
            if (y < WorldGenConstants.SectorRows - 1) yield return index + WorldGenConstants.SectorColumns;
        }

        private static int MinimumSeedDistance(BiomePatch left, BiomePatch right)
        {
            var result = int.MaxValue;
            foreach (var a in left.Seeds)
            foreach (var b in right.Seeds)
            {
                var distance = Math.Abs(a.Sector.X - b.Sector.X) + Math.Abs(a.Sector.Y - b.Sector.Y);
                if (distance < result) result = distance;
            }
            return result == int.MaxValue ? -1 : result;
        }

        private static int Perimeter(IReadOnlyList<int> sectors)
        {
            var members = new HashSet<int>(sectors);
            var result = 0;
            foreach (var index in members)
            {
                var x = index % WorldGenConstants.SectorColumns;
                var y = index / WorldGenConstants.SectorColumns;
                if (x == 0 || !members.Contains(index - 1)) result++;
                if (x == WorldGenConstants.SectorColumns - 1 || !members.Contains(index + 1)) result++;
                if (y == 0 || !members.Contains(index - WorldGenConstants.SectorColumns)) result++;
                if (y == WorldGenConstants.SectorRows - 1 || !members.Contains(index + WorldGenConstants.SectorColumns)) result++;
            }
            return result;
        }

        private static bool PairAllowsTunnel(
            IEnumerable<BiomeBoundaryPairRuleDefinition> pairs,
            string left,
            string right)
        {
            foreach (var pair in pairs)
            {
                var matches = string.Equals(pair.BiomeAId, left, StringComparison.Ordinal) &&
                              string.Equals(pair.BiomeBId, right, StringComparison.Ordinal) ||
                              string.Equals(pair.BiomeAId, right, StringComparison.Ordinal) &&
                              string.Equals(pair.BiomeBId, left, StringComparison.Ordinal);
                if (matches && ContainsOrdinal(pair.AllowedBoundaryProfileIds, "BOUND_TUNNEL"))
                    return true;
            }
            return false;
        }

        private static bool IsAllowedDirectedIntrusion(string intruder, string host)
        {
            if (string.Equals(intruder, "BIO_CASSIA_ROOT", StringComparison.Ordinal))
                return string.Equals(host, "BIO_MOON_CRATER", StringComparison.Ordinal) ||
                       string.Equals(host, "BIO_ABANDONED_MILL", StringComparison.Ordinal) ||
                       string.Equals(host, "BIO_MOON_DOUGH", StringComparison.Ordinal);
            if (string.Equals(intruder, "BIO_ABANDONED_MILL", StringComparison.Ordinal))
                return string.Equals(host, "BIO_CASSIA_ROOT", StringComparison.Ordinal) ||
                       string.Equals(host, "BIO_MOON_DOUGH", StringComparison.Ordinal);
            return false;
        }

        private static bool HasExactCsvShape(byte[] bytes, string header, int lineCount)
        {
            if (bytes == null || bytes.Length < 5 || bytes[0] != 0xEF || bytes[1] != 0xBB || bytes[2] != 0xBF)
                return false;
            var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            if (!text.StartsWith(header + "\r\n", StringComparison.Ordinal) ||
                !text.EndsWith("\r\n", StringComparison.Ordinal) ||
                text.EndsWith("\r\n\r\n", StringComparison.Ordinal))
                return false;
            var actual = 0;
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '\n')
                {
                    if (index == 0 || text[index - 1] != '\r') return false;
                    actual++;
                }
                else if (text[index] == '\r' && (index + 1 >= text.Length || text[index + 1] != '\n'))
                    return false;
            }
            return actual == lineCount;
        }

        private static int CountPatchOverlaps(IEnumerable<BiomePatch> patches)
        {
            var seen = new HashSet<int>();
            var overlaps = 0;
            foreach (var patch in patches)
            foreach (var index in patch.SectorIndices)
                if (!seen.Add(index)) overlaps++;
            return overlaps;
        }

        private static int CountOwnershipOrphans(BiomePatchSnapshot snapshot)
        {
            var count = 0;
            foreach (var ownership in snapshot.Sectors)
                if (ownership.IsAssigned &&
                    (!ownership.PatchId.HasValue ||
                     !snapshot.TryGetPatch(ownership.PatchId.Value, out var patch) ||
                     !patch.ContainsSector(ownership.SectorIndex)))
                    count++;
            return count;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (var index = 0; index < left.Length; index++)
                if (left[index] != right[index]) return false;
            return true;
        }

        private static bool Contains<T>(IReadOnlyList<T> values, T expected)
            where T : IEquatable<T>
        {
            for (var index = 0; index < values.Count; index++)
                if (values[index].Equals(expected)) return true;
            return false;
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string expected)
        {
            if (values == null) return false;
            for (var index = 0; index < values.Count; index++)
                if (string.Equals(values[index], expected, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool IsCoreReservation(SiteReservationKind kind)
        {
            return kind == SiteReservationKind.CoreResource || kind == SiteReservationKind.Forge;
        }

        private static int ToIndex(SectorCoord coordinate)
        {
            return coordinate.Y * WorldGenConstants.SectorColumns + coordinate.X;
        }

        private static bool IsFiniteShare(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f && value <= 1f;
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string AtLeast(int value)
        {
            return ">=" + Number(value);
        }

        private static void AddViolation(
            ICollection<BiomePatchValidationViolation> violations,
            BiomePatchValidationRule rule,
            string biomeId,
            string patchId,
            int sectorIndex,
            string expected,
            string actual,
            string message)
        {
            violations.Add(new BiomePatchValidationViolation(
                rule, biomeId, patchId, sectorIndex, expected, actual, message));
        }

        private static void AddError(
            ICollection<BiomePatchValidationError> errors,
            BiomePatchValidationErrorCode code,
            string definitionId,
            string message,
            int sectorIndex = -1)
        {
            errors.Add(new BiomePatchValidationError(code, definitionId, sectorIndex, message));
        }

        private static string[] ExpectedBiomeIds()
        {
            return new[]
            {
                "BIO_MOON_CRATER", "BIO_CASSIA_ROOT", "BIO_ABANDONED_MILL", "BIO_MOON_DOUGH"
            };
        }

        private static string[] ExpectedPatchRuleIds()
        {
            return new[]
            {
                "PATCH_CRATER_CORE", "PATCH_CRATER_SAT", "PATCH_ROOT_CORE", "PATCH_ROOT_SAT",
                "PATCH_MILL_CORE", "PATCH_MILL_SAT", "PATCH_DOUGH_CORE", "PATCH_DOUGH_SAT",
                "PATCH_ROOT_INTRUSION", "PATCH_MILL_INTRUSION"
            };
        }

        private static string[] ExpectedBoundaryProfileIds()
        {
            return new[]
            {
                "BOUND_SOFT_BLEND", "BOUND_CLIFF", "BOUND_TUNNEL", "BOUND_LAYER",
                "BOUND_RUIN", "BOUND_HARD_STARSTONE"
            };
        }

        private static string[] ExpectedBoundaryPairRuleIds()
        {
            return new[]
            {
                "PAIR_CRATER_ROOT", "PAIR_CRATER_MILL", "PAIR_CRATER_DOUGH",
                "PAIR_ROOT_MILL", "PAIR_ROOT_DOUGH", "PAIR_MILL_DOUGH"
            };
        }

        private sealed class ValidationContext
        {
            public ValidationContext(
                BiomePatchExportPublication publication,
                IReadOnlyDictionary<string, BiomeTypeDefinition> biomes,
                IReadOnlyDictionary<string, BiomePatchRuleDefinition> rules,
                IReadOnlyDictionary<string, BiomeBoundaryProfileDefinition> profiles,
                IReadOnlyDictionary<string, BiomeBoundaryPairRuleDefinition> pairs)
            {
                Publication = publication;
                Snapshot = publication.SourceCleanup.Snapshot;
                SiteSnapshot = publication.SourceCleanup.SourceIntrusion.Publication.SourceSiteSnapshot;
                Biomes = biomes;
                Rules = rules;
                Profiles = profiles;
                Pairs = pairs;
                PatchBytes = publication.GeneratedBiomePatchesCsv;
                WorldBytes = publication.GeneratedWorldSectorsCsv;
            }

            public BiomePatchExportPublication Publication { get; }
            public BiomePatchSnapshot Snapshot { get; }
            public SiteReservationSnapshot SiteSnapshot { get; }
            public IReadOnlyDictionary<string, BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyDictionary<string, BiomePatchRuleDefinition> Rules { get; }
            public IReadOnlyDictionary<string, BiomeBoundaryProfileDefinition> Profiles { get; }
            public IReadOnlyDictionary<string, BiomeBoundaryPairRuleDefinition> Pairs { get; }
            public byte[] PatchBytes { get; }
            public byte[] WorldBytes { get; }
        }
    }
}
