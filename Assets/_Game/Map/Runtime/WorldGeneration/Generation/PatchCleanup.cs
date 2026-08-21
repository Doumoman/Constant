using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class PatchCleanup
    {
        private const int RequiredAssignedCount = 165;
        private const int RequiredUnassignedCount = 4;
        private const int RequiredCoreCount = 4;
        private const int PatchHardMaximum = 59;
        private const int CleanupStepLimit = WorldGenConstants.SectorCount * 4;

        public PatchCleanupResult Clean(
            IntrusionPlacementResult intrusionResult,
            IEnumerable<BiomeTypeDefinition> biomeTypes,
            IEnumerable<BiomePatchRuleDefinition> patchRules)
        {
            try
            {
                var context = new ValidationContext();
                var errors = new List<PatchCleanupError>();
                ValidateIntrusion(intrusionResult, context, errors);
                ValidateBiomeTypes(biomeTypes, context, errors);
                ValidatePatchRules(patchRules, context, errors);
                ValidateSource(context, errors);
                if (errors.Count != 0) return PatchCleanupResult.Invalid(errors);
                return Execute(context);
            }
            catch
            {
                return PatchCleanupResult.Invalid(new[]
                {
                    Error(
                        PatchCleanupErrorCode.InternalInvariantViolation,
                        string.Empty, -1, 0, 0,
                        "Patch cleanup violated an internal model invariant.")
                });
            }
        }

        private static void ValidateIntrusion(
            IntrusionPlacementResult result,
            ValidationContext context,
            ICollection<PatchCleanupError> errors)
        {
            context.Result = result;
            if (result == null)
            {
                errors.Add(StructuralError(
                    PatchCleanupErrorCode.MissingIntrusionResult,
                    "An Intrusion placement result is required."));
                return;
            }
            if (result.Status != IntrusionPlacementStatus.Completed || !result.Succeeded ||
                result.Errors == null || result.Errors.Count != 0)
                errors.Add(StructuralError(
                    PatchCleanupErrorCode.IntrusionNotCompleted,
                    "Intrusion placement must be completed without errors."));
            if (result.Publication == null)
                errors.Add(StructuralError(
                    PatchCleanupErrorCode.MissingPublication,
                    "A completed Intrusion publication is required."));
            if (result.Diagnostics == null)
                errors.Add(StructuralError(
                    PatchCleanupErrorCode.MissingDiagnostics,
                    "Intrusion diagnostics are required."));
            if (result.Publication == null) return;

            context.Intrusion = result.Publication;
            context.Source = result.Publication.Snapshot;
            context.Reservations = result.Publication.SourceSiteSnapshot;
            if (context.Source == null || context.Reservations == null)
                errors.Add(StructuralError(
                    PatchCleanupErrorCode.InvalidSourceSnapshot,
                    "The P03 biome snapshot and P01 reservation snapshot are required."));
        }

        private static void ValidateBiomeTypes(
            IEnumerable<BiomeTypeDefinition> source,
            ValidationContext context,
            ICollection<PatchCleanupError> errors)
        {
            if (source == null)
            {
                errors.Add(StructuralError(
                    PatchCleanupErrorCode.MissingBiomeTypes,
                    "Biome type definitions are required."));
                return;
            }
            foreach (var biome in source)
            {
                if (biome == null || !ReservationValidation.IsCanonicalId(biome.BiomeId, false) ||
                    !biome.Active || biome.MinPatchCount < 1 || biome.MaxPatchCount < biome.MinPatchCount)
                {
                    errors.Add(StructuralError(
                        PatchCleanupErrorCode.InvalidDefinition,
                        "Biome definitions must be active and structurally valid."));
                    continue;
                }
                if (!context.Biomes.TryAdd(biome.BiomeId, biome))
                    errors.Add(Error(
                        PatchCleanupErrorCode.InvalidDefinition,
                        biome.BiomeId, -1, 1, 2,
                        "Biome definition IDs must be unique."));
            }
        }

        private static void ValidatePatchRules(
            IEnumerable<BiomePatchRuleDefinition> source,
            ValidationContext context,
            ICollection<PatchCleanupError> errors)
        {
            if (source == null)
            {
                errors.Add(StructuralError(
                    PatchCleanupErrorCode.MissingPatchRules,
                    "Biome patch rules are required."));
                return;
            }
            foreach (var rule in source)
            {
                if (rule == null || !ReservationValidation.IsCanonicalId(rule.PatchRuleId, false) ||
                    !ReservationValidation.IsCanonicalId(rule.BiomeId, false) || !rule.Active ||
                    !BiomePatchRoleTokenCodec.TryParse(rule.PatchRole, out var role) ||
                    rule.MinSectorCount < 1 || rule.MaxSectorCount < rule.MinSectorCount ||
                    rule.MaxSectorCount > PatchHardMaximum || float.IsNaN(rule.MaxWorldShare) ||
                    float.IsInfinity(rule.MaxWorldShare) || rule.MaxWorldShare <= 0f)
                {
                    errors.Add(StructuralError(
                        PatchCleanupErrorCode.InvalidDefinition,
                        "Patch rules must be active and structurally valid."));
                    continue;
                }
                if (!context.Rules.TryAdd(rule.PatchRuleId, rule))
                    errors.Add(Error(
                        PatchCleanupErrorCode.InvalidDefinition,
                        rule.PatchRuleId, -1, 1, 2,
                        "Patch rule IDs must be unique."));
                else context.RuleRoles[rule.PatchRuleId] = role;
            }
        }

        private static void ValidateSource(
            ValidationContext context,
            ICollection<PatchCleanupError> errors)
        {
            if (context.Source == null || context.Reservations == null ||
                context.Result == null || context.Intrusion == null) return;

            var source = context.Source;
            var diagnostics = context.Result.Diagnostics;
            if (source.Sectors == null || source.Sectors.Count != WorldGenConstants.SectorCount ||
                source.Patches == null || source.SiteBindings == null ||
                context.Reservations.Sectors == null ||
                context.Reservations.Sectors.Count != WorldGenConstants.SectorCount ||
                source.Seed != context.Reservations.Seed || diagnostics == null ||
                diagnostics.WorldSeed != source.Seed ||
                diagnostics.FinalPatchCount != source.Patches.Count ||
                diagnostics.FinalAssignedSectorCount != source.AssignedSectorCount ||
                diagnostics.FinalUnassignedSectorCount != source.UnassignedSectorCount ||
                diagnostics.RngDrawCountAfter < diagnostics.RngDrawCountBefore)
                errors.Add(StructuralError(
                    PatchCleanupErrorCode.InvalidSourceSnapshot,
                    "Intrusion publication linkage and diagnostics must match P03."));

            if (source.AssignedSectorCount != RequiredAssignedCount ||
                source.UnassignedSectorCount != RequiredUnassignedCount || source.IsComplete ||
                context.Intrusion.TotalPatchCount != source.Patches.Count ||
                context.Intrusion.AssignedSectorCount != source.AssignedSectorCount ||
                context.Intrusion.UnassignedSectorCount != source.UnassignedSectorCount ||
                context.Intrusion.CorePatchCount != RequiredCoreCount ||
                context.Intrusion.CorePatchCount + context.Intrusion.SatellitePatchCount +
                    context.Intrusion.IntrusionPatchCount != context.Intrusion.TotalPatchCount)
                errors.Add(Error(
                    PatchCleanupErrorCode.InvalidSourceSnapshot,
                    string.Empty, -1, RequiredAssignedCount,
                    Math.Max(0, source.AssignedSectorCount),
                    "P03 must preserve its producer patch inventory, 165 assigned, and 4 reserved-unassigned sectors."));

            var patchIds = new HashSet<BiomePatchId>();
            var coreCount = 0;
            var intrusionCount = 0;
            foreach (var patch in source.Patches)
            {
                if (patch == null || !patch.Id.IsValid || !patchIds.Add(patch.Id) ||
                    patch.Seeds == null || patch.Seeds.Count == 0 || patch.SectorIndices == null ||
                    !context.Biomes.ContainsKey(SafeId(patch.BiomeId)) ||
                    !context.Rules.TryGetValue(SafeId(patch.PatchRuleId), out var rule) ||
                    !context.RuleRoles.TryGetValue(SafeId(patch.PatchRuleId), out var role) ||
                    role != patch.Role || !string.Equals(rule.BiomeId, patch.BiomeId, StringComparison.Ordinal) ||
                    patch.SectorCount < rule.MinSectorCount ||
                    patch.SectorCount > Math.Min(rule.MaxSectorCount, PatchHardMaximum))
                {
                    errors.Add(Error(
                        PatchCleanupErrorCode.InvalidSourceSnapshot,
                        patch == null ? string.Empty : SafeId(patch.PatchRuleId),
                        -1, 1, 0,
                        "Every source patch must match one active rule and size contract."));
                    continue;
                }
                if (patch.Role == BiomePatchRole.Core) coreCount++;
                else if (patch.Role == BiomePatchRole.Intrusion)
                {
                    intrusionCount++;
                    if (patch.SectorCount != 1 || patch.Seeds.Count != 1)
                        errors.Add(Error(
                            PatchCleanupErrorCode.InvalidSourceSnapshot,
                            patch.PatchRuleId, patch.SectorIndices.Count == 0 ? -1 : patch.SectorIndices[0],
                            1, patch.SectorCount,
                            "Every Intrusion patch must remain exact one-cell."));
                }
                else if (patch.Role != BiomePatchRole.Satellite)
                    errors.Add(StructuralError(
                        PatchCleanupErrorCode.InvalidSourceSnapshot,
                        "Patch roles must be Core, Satellite, or Intrusion."));

                context.Works[patch.Id] = new WorkingPatch(patch);
                foreach (var sectorIndex in patch.SectorIndices)
                {
                    if (context.OwnerIds[sectorIndex].HasValue)
                        errors.Add(Error(
                            PatchCleanupErrorCode.InvalidSourceSnapshot,
                            patch.Id.Value, sectorIndex, 1, 2,
                            "Patch sectors cannot overlap."));
                    context.OwnerIds[sectorIndex] = patch.Id;
                }
                foreach (var seed in patch.Seeds)
                {
                    if (seed == null || !patch.ContainsSector(seed.SectorIndex) || seed.Role != patch.Role)
                        errors.Add(Error(
                            PatchCleanupErrorCode.InvalidSourceSnapshot,
                            patch.Id.Value, seed == null ? -1 : seed.SectorIndex,
                            1, 0, "Patch seed linkage is invalid."));
                    else context.Protected[seed.SectorIndex] = true;
                }
            }
            if (coreCount != RequiredCoreCount ||
                intrusionCount != context.Intrusion.IntrusionPatchCount)
                errors.Add(Error(
                    PatchCleanupErrorCode.InvalidSourceSnapshot,
                    string.Empty, -1,
                    RequiredCoreCount + context.Intrusion.IntrusionPatchCount,
                    coreCount + intrusionCount,
                    "P03 role counts must match four Core patches and the producer Intrusion inventory."));

            foreach (var binding in source.SiteBindings)
            {
                if (binding == null || !source.TryGetPatch(binding.PatchId, out var patch) ||
                    patch.Role != BiomePatchRole.Core ||
                    !string.Equals(binding.BiomeId, patch.BiomeId, StringComparison.Ordinal))
                {
                    errors.Add(StructuralError(
                        PatchCleanupErrorCode.InvalidSourceSnapshot,
                        "Core site bindings must match Core patches."));
                    continue;
                }
                foreach (var sectorIndex in binding.OccupiedSectorIndices)
                    context.Protected[sectorIndex] = true;
            }

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var reservation = context.Reservations.GetSector(index);
                var ownership = source.GetSector(index);
                if (reservation == null || reservation.Index != index ||
                    ownership == null || ownership.SectorIndex != index ||
                    ownership.SecondaryBiomeId == null || ownership.SecondaryBiomeId.Length != 0 ||
                    ownership.IsAssigned != context.OwnerIds[index].HasValue ||
                    (ownership.IsAssigned && (!ownership.PatchId.HasValue ||
                     ownership.PatchId.Value != context.OwnerIds[index].Value)))
                    errors.Add(Error(
                        PatchCleanupErrorCode.InvalidSourceSnapshot,
                        string.Empty, index, 1, 0,
                        "P01/P03 sector identity or ownership is invalid."));
                if (reservation != null && reservation.IsReserved) context.Protected[index] = true;
            }

            foreach (var patch in source.Patches)
            {
                if (patch.Role != BiomePatchRole.Intrusion) continue;
                foreach (var sectorIndex in patch.SectorIndices)
                {
                    context.Protected[sectorIndex] = true;
                    foreach (var neighbor in GetNeighbors(sectorIndex)) context.Protected[neighbor] = true;
                }
            }

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                context.OriginalOwnerIds[index] = context.OwnerIds[index];
        }

        private static PatchCleanupResult Execute(ValidationContext context)
        {
            var initialScan = Scan(context, context.OwnerIds);
            var initialScore = initialScan.Score;
            var moves = new List<PatchCleanupMoveRecord>();

            while (true)
            {
                var scan = Scan(context, context.OwnerIds);
                if (scan.Actionable.Count == 0)
                {
                    var snapshot = BuildSnapshot(context, context.OwnerIds, null);
                    var publication = new PatchCleanupPublication(context.Result, snapshot, moves);
                    var diagnostics = CreateDiagnostics(
                        context, initialScore, scan.Score, initialScan.ProtectedCount,
                        moves, false);
                    return PatchCleanupResult.Completed(publication, diagnostics);
                }
                if (moves.Count >= CleanupStepLimit)
                    return Retry(
                        context, initialScore, initialScan.ProtectedCount,
                        PatchCleanupErrorCode.CleanupStepLimitExceeded,
                        scan.Actionable[0].Center,
                        "Patch cleanup reached the exact 676-step limit.");

                var actions = EnumerateActions(context, scan.Actionable, scan.Score);
                if (actions.Count == 0)
                    return Retry(
                        context, initialScore, initialScan.ProtectedCount,
                        PatchCleanupErrorCode.NoSafeCleanupMove,
                        scan.Actionable[0].Center,
                        "No strictly improving legal cleanup move exists.");

                actions.Sort(ActionCandidate.Compare);
                var chosen = actions[0];
                var donor = context.Works[chosen.DonorPatchId];
                var target = context.Works[chosen.TargetPatchId];
                var donorBefore = donor.Sectors.Count;
                var targetBefore = target.Sectors.Count;
                donor.Sectors.Remove(chosen.MovedSectorIndex);
                target.Sectors.Add(chosen.MovedSectorIndex);
                context.OwnerIds[chosen.MovedSectorIndex] = chosen.TargetPatchId;
                moves.Add(new PatchCleanupMoveRecord(
                    moves.Count, chosen.Kind, chosen.CenterSectorIndex,
                    chosen.MovedSectorIndex, chosen.DonorPatchId, chosen.TargetPatchId,
                    donor.BiomeId, target.BiomeId,
                    donorBefore, donorBefore - 1, targetBefore, targetBefore + 1,
                    chosen.ScoreBefore, chosen.ScoreAfter));
            }
        }

        private static PatchCleanupResult Retry(
            ValidationContext context,
            PatchCleanupScore initialScore,
            int protectedCount,
            PatchCleanupErrorCode code,
            int sectorIndex,
            string message)
        {
            var diagnostics = CreateDiagnostics(
                context, initialScore, initialScore, protectedCount,
                Array.Empty<PatchCleanupMoveRecord>(), true);
            return PatchCleanupResult.Retry(diagnostics, new[]
            {
                Error(code, string.Empty, sectorIndex, 1, 0, message)
            });
        }

        private static PatchCleanupDiagnostics CreateDiagnostics(
            ValidationContext context,
            PatchCleanupScore initialScore,
            PatchCleanupScore finalScore,
            int protectedCount,
            IEnumerable<PatchCleanupMoveRecord> moves,
            bool rollback)
        {
            var source = context.Source;
            return new PatchCleanupDiagnostics(
                source.Seed, context.Result.Diagnostics.RngDrawCountAfter,
                source.Patches.Count, source.Patches.Count,
                source.AssignedSectorCount, source.AssignedSectorCount,
                source.UnassignedSectorCount, source.UnassignedSectorCount,
                initialScore, finalScore, protectedCount, CleanupStepLimit, moves,
                0, 0, 0, 0, 0, 0, rollback);
        }

        private static List<ActionCandidate> EnumerateActions(
            ValidationContext context,
            IReadOnlyList<Anomaly> anomalies,
            PatchCleanupScore before)
        {
            var result = new List<ActionCandidate>();
            foreach (var anomaly in anomalies)
            {
                var collapseKind = anomaly.Kind == AnomalyKind.Checkerboard
                    ? PatchCleanupMoveKind.CheckerboardCollapse
                    : PatchCleanupMoveKind.NeckCollapse;
                if (TryCreateAction(
                    context, anomaly.Center, anomaly.Center, collapseKind,
                    anomaly.DonorPatchId, anomaly.TargetPatchId, before, out var collapse))
                {
                    result.Add(collapse);
                    continue;
                }
                if (anomaly.Kind != AnomalyKind.Neck) continue;

                var flanks = new[] { anomaly.FirstFlank, anomaly.SecondFlank };
                Array.Sort(flanks);
                foreach (var flank in flanks)
                    if (TryCreateAction(
                        context, anomaly.Center, flank, PatchCleanupMoveKind.NeckWiden,
                        anomaly.TargetPatchId, anomaly.DonorPatchId, before, out var widen))
                        result.Add(widen);
            }
            return result;
        }

        private static bool TryCreateAction(
            ValidationContext context,
            int center,
            int moved,
            PatchCleanupMoveKind kind,
            BiomePatchId donorId,
            BiomePatchId targetId,
            PatchCleanupScore before,
            out ActionCandidate candidate)
        {
            candidate = null;
            if (donorId == targetId || context.Protected[moved] ||
                !context.OwnerIds[moved].HasValue || context.OwnerIds[moved].Value != donorId ||
                !context.Works.TryGetValue(donorId, out var donor) ||
                !context.Works.TryGetValue(targetId, out var target) ||
                !IsNormal(donor) || !IsNormal(target) || !AreAdjacent(moved, target.Sectors))
                return false;

            var donorSectors = new HashSet<int>(donor.Sectors);
            var targetSectors = new HashSet<int>(target.Sectors);
            if (!donorSectors.Remove(moved) || !targetSectors.Add(moved)) return false;
            if (!IsLegalPatchSize(context, donor, donorSectors.Count) ||
                !IsLegalPatchSize(context, target, targetSectors.Count) ||
                !IsConnected(donorSectors) || !IsConnected(targetSectors) ||
                !PreservesSeeds(donor, donorSectors) || !PreservesSeeds(target, targetSectors) ||
                !PreservesBindings(context, donorId, donorSectors) ||
                !PreservesBindings(context, targetId, targetSectors))
                return false;

            var proposedOwners = (BiomePatchId?[])context.OwnerIds.Clone();
            proposedOwners[moved] = targetId;
            if (!PreservesFrozenOwners(context, proposedOwners) ||
                !PassesShareCaps(context, proposedOwners)) return false;

            var overrides = new Dictionary<BiomePatchId, HashSet<int>>
            {
                { donorId, donorSectors },
                { targetId, targetSectors }
            };
            try
            {
                var snapshot = BuildSnapshot(context, proposedOwners, overrides);
                if (snapshot.Patches.Count != context.Source.Patches.Count ||
                    snapshot.AssignedSectorCount != context.Source.AssignedSectorCount ||
                    snapshot.UnassignedSectorCount != context.Source.UnassignedSectorCount ||
                    snapshot.SiteBindings.Count != context.Source.SiteBindings.Count)
                    return false;
            }
            catch
            {
                return false;
            }

            var after = Scan(context, proposedOwners).Score;
            if (after.CompareTo(before) >= 0) return false;
            candidate = new ActionCandidate(
                kind, center, moved, donorId, targetId, before, after);
            return true;
        }

        private static ScanResult Scan(ValidationContext context, BiomePatchId?[] owners)
        {
            var anomalies = new List<Anomaly>();
            var protectedCount = 0;
            for (var y = 1; y < WorldGenConstants.SectorRows - 1; y++)
            for (var x = 1; x < WorldGenConstants.SectorColumns - 1; x++)
            {
                var center = (y * WorldGenConstants.SectorColumns) + x;
                if (!TryGetNormal(context, owners, center, out var donorId)) continue;
                var left = center - 1;
                var right = center + 1;
                var up = center + WorldGenConstants.SectorColumns;
                var down = center - WorldGenConstants.SectorColumns;
                var containsProtected = context.Protected[center] || context.Protected[left] ||
                    context.Protected[right] || context.Protected[up] || context.Protected[down];

                if (TryGetSameForeignNormal(
                    context, owners, donorId, left, right, up, down, out var checkerTarget))
                {
                    if (containsProtected) protectedCount++;
                    else anomalies.Add(new Anomaly(
                        AnomalyKind.Checkerboard, center, donorId, checkerTarget, -1, -1));
                }

                if (SameOwner(owners, up, donorId) && SameOwner(owners, down, donorId) &&
                    TryGetSameForeignNormal(
                        context, owners, donorId, left, right, out var verticalTarget))
                {
                    if (containsProtected) protectedCount++;
                    else anomalies.Add(new Anomaly(
                        AnomalyKind.Neck, center, donorId, verticalTarget, left, right));
                }
                if (SameOwner(owners, left, donorId) && SameOwner(owners, right, donorId) &&
                    TryGetSameForeignNormal(
                        context, owners, donorId, up, down, out var horizontalTarget))
                {
                    if (containsProtected) protectedCount++;
                    else anomalies.Add(new Anomaly(
                        AnomalyKind.Neck, center, donorId, horizontalTarget, up, down));
                }
            }
            anomalies.Sort(Anomaly.Compare);

            var checkerboards = 0;
            var necks = 0;
            foreach (var anomaly in anomalies)
                if (anomaly.Kind == AnomalyKind.Checkerboard) checkerboards++;
                else necks++;
            var crossEdges = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var right = WorldGridIndex.GetRightIndex(index);
                var up = WorldGridIndex.GetUpIndex(index);
                if (right >= 0 && owners[index].HasValue && owners[right].HasValue &&
                    owners[index].Value != owners[right].Value) crossEdges++;
                if (up >= 0 && owners[index].HasValue && owners[up].HasValue &&
                    owners[index].Value != owners[up].Value) crossEdges++;
            }
            return new ScanResult(
                new PatchCleanupScore(checkerboards, necks, crossEdges),
                protectedCount, anomalies);
        }

        private static bool TryGetSameForeignNormal(
            ValidationContext context,
            BiomePatchId?[] owners,
            BiomePatchId donor,
            int first,
            int second,
            out BiomePatchId target)
        {
            target = default(BiomePatchId);
            if (!TryGetNormal(context, owners, first, out var firstId) ||
                !TryGetNormal(context, owners, second, out var secondId) ||
                firstId != secondId || firstId == donor) return false;
            target = firstId;
            return true;
        }

        private static bool TryGetSameForeignNormal(
            ValidationContext context,
            BiomePatchId?[] owners,
            BiomePatchId donor,
            int first,
            int second,
            int third,
            int fourth,
            out BiomePatchId target)
        {
            target = default(BiomePatchId);
            if (!TryGetSameForeignNormal(context, owners, donor, first, second, out var pair) ||
                !TryGetNormal(context, owners, third, out var thirdId) ||
                !TryGetNormal(context, owners, fourth, out var fourthId) ||
                pair != thirdId || pair != fourthId) return false;
            target = pair;
            return true;
        }

        private static bool TryGetNormal(
            ValidationContext context,
            BiomePatchId?[] owners,
            int index,
            out BiomePatchId id)
        {
            id = default(BiomePatchId);
            if (index < 0 || index >= owners.Length || !owners[index].HasValue) return false;
            id = owners[index].Value;
            return context.Works.TryGetValue(id, out var patch) && IsNormal(patch);
        }

        private static bool SameOwner(BiomePatchId?[] owners, int index, BiomePatchId id)
        {
            return index >= 0 && index < owners.Length && owners[index].HasValue && owners[index].Value == id;
        }

        private static bool IsNormal(WorkingPatch patch)
        {
            return patch.Role == BiomePatchRole.Core || patch.Role == BiomePatchRole.Satellite;
        }

        private static bool IsLegalPatchSize(ValidationContext context, WorkingPatch patch, int count)
        {
            if (count < 2 || !context.Rules.TryGetValue(patch.PatchRuleId, out var rule)) return false;
            return count >= rule.MinSectorCount && count <= Math.Min(rule.MaxSectorCount, PatchHardMaximum);
        }

        private static bool PreservesSeeds(WorkingPatch patch, ISet<int> sectors)
        {
            foreach (var seed in patch.Seeds)
                if (seed == null || !sectors.Contains(seed.SectorIndex)) return false;
            return true;
        }

        private static bool PreservesBindings(
            ValidationContext context,
            BiomePatchId patchId,
            ISet<int> sectors)
        {
            foreach (var binding in context.Source.SiteBindings)
            {
                if (binding.PatchId != patchId) continue;
                foreach (var index in binding.OccupiedSectorIndices)
                    if (!sectors.Contains(index)) return false;
            }
            return true;
        }

        private static bool PreservesFrozenOwners(
            ValidationContext context,
            BiomePatchId?[] proposed)
        {
            for (var index = 0; index < proposed.Length; index++)
                if (context.Protected[index] && proposed[index] != context.OriginalOwnerIds[index])
                    return false;
            return true;
        }

        private static bool PassesShareCaps(
            ValidationContext context,
            BiomePatchId?[] owners)
        {
            var normal = new Dictionary<string, int>(StringComparer.Ordinal);
            var intrusion = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var owner in owners)
            {
                if (!owner.HasValue) continue;
                var patch = context.Works[owner.Value];
                var counts = patch.Role == BiomePatchRole.Intrusion ? intrusion : normal;
                if (!counts.ContainsKey(patch.BiomeId)) counts[patch.BiomeId] = 0;
                counts[patch.BiomeId]++;
            }
            foreach (var pair in normal)
                if (pair.Value > GetShareCap(context, pair.Key, false)) return false;
            foreach (var pair in intrusion)
                if (pair.Value > GetShareCap(context, pair.Key, true)) return false;
            return true;
        }

        private static int GetShareCap(ValidationContext context, string biomeId, bool intrusion)
        {
            var cap = int.MaxValue;
            foreach (var rule in context.Rules.Values)
            {
                if (!string.Equals(rule.BiomeId, biomeId, StringComparison.Ordinal) ||
                    !context.RuleRoles.TryGetValue(rule.PatchRuleId, out var role) ||
                    (role == BiomePatchRole.Intrusion) != intrusion) continue;
                var value = (int)Math.Floor((WorldGenConstants.SectorCount * (double)rule.MaxWorldShare) + .000001d);
                cap = Math.Min(cap, Math.Max(0, value));
            }
            return cap == int.MaxValue ? 0 : cap;
        }

        private static BiomePatchSnapshot BuildSnapshot(
            ValidationContext context,
            BiomePatchId?[] owners,
            IReadOnlyDictionary<BiomePatchId, HashSet<int>> overrides)
        {
            var patches = new List<BiomePatch>();
            foreach (var work in context.Works.Values.OrderBy(value => value.Id))
            {
                if (overrides != null && overrides.TryGetValue(work.Id, out var sectors))
                    patches.Add(work.Build(sectors));
                else patches.Add(work.Build(work.Sectors));
            }

            var ownership = new List<BiomeSectorOwnership>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                if (!owners[index].HasValue)
                {
                    var old = context.Source.GetSector(index);
                    ownership.Add(!old.IsAssigned
                        ? old
                        : BiomeSectorOwnership.CreateUnassigned(index, WorldGridIndex.ToCoordinate(index)));
                    continue;
                }
                var id = owners[index].Value;
                var source = context.Source.GetSector(index);
                if (source.IsAssigned && source.PatchId.HasValue && source.PatchId.Value == id)
                    ownership.Add(source);
                else
                {
                    var patch = context.Works[id];
                    ownership.Add(new BiomeSectorOwnership(
                        index, WorldGridIndex.ToCoordinate(index),
                        patch.BiomeId, string.Empty, id));
                }
            }
            return new BiomePatchSnapshot(
                context.Source.Seed, patches, ownership, context.Source.SiteBindings);
        }

        private static bool IsConnected(IEnumerable<int> source)
        {
            var values = new HashSet<int>(source);
            if (values.Count == 0) return false;
            var start = values.Min();
            var visited = new HashSet<int> { start };
            var queue = new Queue<int>();
            queue.Enqueue(start);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in GetNeighbors(current))
                    if (values.Contains(neighbor) && visited.Add(neighbor)) queue.Enqueue(neighbor);
            }
            return visited.Count == values.Count;
        }

        private static bool AreAdjacent(int index, ISet<int> sectors)
        {
            foreach (var neighbor in GetNeighbors(index))
                if (sectors.Contains(neighbor)) return true;
            return false;
        }

        private static IEnumerable<int> GetNeighbors(int sectorIndex)
        {
            var left = WorldGridIndex.GetLeftIndex(sectorIndex);
            if (left >= 0) yield return left;
            var right = WorldGridIndex.GetRightIndex(sectorIndex);
            if (right >= 0) yield return right;
            var up = WorldGridIndex.GetUpIndex(sectorIndex);
            if (up >= 0) yield return up;
            var down = WorldGridIndex.GetDownIndex(sectorIndex);
            if (down >= 0) yield return down;
        }

        private static string SafeId(string value)
        {
            return ReservationValidation.IsCanonicalId(value, true) ? value : string.Empty;
        }

        private static PatchCleanupError StructuralError(PatchCleanupErrorCode code, string message)
        {
            return Error(code, string.Empty, -1, 0, 0, message);
        }

        private static PatchCleanupError Error(
            PatchCleanupErrorCode code,
            string definitionId,
            int sectorIndex,
            int requiredCount,
            int availableCount,
            string message)
        {
            return new PatchCleanupError(
                code, definitionId ?? string.Empty, sectorIndex,
                requiredCount, availableCount, message);
        }

        private sealed class ValidationContext
        {
            public IntrusionPlacementResult Result;
            public IntrusionPlacementPublication Intrusion;
            public BiomePatchSnapshot Source;
            public SiteReservationSnapshot Reservations;
            public readonly Dictionary<string, BiomeTypeDefinition> Biomes =
                new Dictionary<string, BiomeTypeDefinition>(StringComparer.Ordinal);
            public readonly Dictionary<string, BiomePatchRuleDefinition> Rules =
                new Dictionary<string, BiomePatchRuleDefinition>(StringComparer.Ordinal);
            public readonly Dictionary<string, BiomePatchRole> RuleRoles =
                new Dictionary<string, BiomePatchRole>(StringComparer.Ordinal);
            public readonly Dictionary<BiomePatchId, WorkingPatch> Works =
                new Dictionary<BiomePatchId, WorkingPatch>();
            public readonly BiomePatchId?[] OwnerIds = new BiomePatchId?[WorldGenConstants.SectorCount];
            public readonly BiomePatchId?[] OriginalOwnerIds = new BiomePatchId?[WorldGenConstants.SectorCount];
            public readonly bool[] Protected = new bool[WorldGenConstants.SectorCount];
        }

        private sealed class WorkingPatch
        {
            public WorkingPatch(BiomePatch source)
            {
                Original = source;
                Id = source.Id;
                BiomeId = source.BiomeId;
                PatchRuleId = source.PatchRuleId;
                Role = source.Role;
                Seeds = source.Seeds;
                Sectors = new HashSet<int>(source.SectorIndices);
            }

            public BiomePatch Original { get; }
            public BiomePatchId Id { get; }
            public string BiomeId { get; }
            public string PatchRuleId { get; }
            public BiomePatchRole Role { get; }
            public IReadOnlyList<BiomePatchSeed> Seeds { get; }
            public HashSet<int> Sectors { get; }

            public BiomePatch Build(ISet<int> sectors)
            {
                if (sectors.SetEquals(Original.SectorIndices)) return Original;
                return new BiomePatch(Id, BiomeId, PatchRuleId, Role, Seeds, sectors);
            }
        }

        private enum AnomalyKind
        {
            Checkerboard,
            Neck
        }

        private sealed class Anomaly
        {
            public Anomaly(
                AnomalyKind kind,
                int center,
                BiomePatchId donorPatchId,
                BiomePatchId targetPatchId,
                int firstFlank,
                int secondFlank)
            {
                Kind = kind;
                Center = center;
                DonorPatchId = donorPatchId;
                TargetPatchId = targetPatchId;
                FirstFlank = firstFlank;
                SecondFlank = secondFlank;
            }

            public AnomalyKind Kind { get; }
            public int Center { get; }
            public BiomePatchId DonorPatchId { get; }
            public BiomePatchId TargetPatchId { get; }
            public int FirstFlank { get; }
            public int SecondFlank { get; }

            public static int Compare(Anomaly left, Anomaly right)
            {
                var value = left.Center.CompareTo(right.Center);
                if (value != 0) return value;
                value = left.Kind.CompareTo(right.Kind);
                if (value != 0) return value;
                value = left.DonorPatchId.CompareTo(right.DonorPatchId);
                return value != 0 ? value : left.TargetPatchId.CompareTo(right.TargetPatchId);
            }
        }

        private sealed class ScanResult
        {
            public ScanResult(PatchCleanupScore score, int protectedCount, IReadOnlyList<Anomaly> actionable)
            {
                Score = score;
                ProtectedCount = protectedCount;
                Actionable = actionable;
            }
            public PatchCleanupScore Score { get; }
            public int ProtectedCount { get; }
            public IReadOnlyList<Anomaly> Actionable { get; }
        }

        private sealed class ActionCandidate
        {
            public ActionCandidate(
                PatchCleanupMoveKind kind,
                int centerSectorIndex,
                int movedSectorIndex,
                BiomePatchId donorPatchId,
                BiomePatchId targetPatchId,
                PatchCleanupScore scoreBefore,
                PatchCleanupScore scoreAfter)
            {
                Kind = kind;
                CenterSectorIndex = centerSectorIndex;
                MovedSectorIndex = movedSectorIndex;
                DonorPatchId = donorPatchId;
                TargetPatchId = targetPatchId;
                ScoreBefore = scoreBefore;
                ScoreAfter = scoreAfter;
            }

            public PatchCleanupMoveKind Kind { get; }
            public int CenterSectorIndex { get; }
            public int MovedSectorIndex { get; }
            public BiomePatchId DonorPatchId { get; }
            public BiomePatchId TargetPatchId { get; }
            public PatchCleanupScore ScoreBefore { get; }
            public PatchCleanupScore ScoreAfter { get; }

            public static int Compare(ActionCandidate left, ActionCandidate right)
            {
                var value = left.ScoreAfter.CompareTo(right.ScoreAfter);
                if (value != 0) return value;
                value = left.CenterSectorIndex.CompareTo(right.CenterSectorIndex);
                if (value != 0) return value;
                value = left.Kind.CompareTo(right.Kind);
                if (value != 0) return value;
                value = left.MovedSectorIndex.CompareTo(right.MovedSectorIndex);
                if (value != 0) return value;
                value = left.DonorPatchId.CompareTo(right.DonorPatchId);
                return value != 0 ? value : left.TargetPatchId.CompareTo(right.TargetPatchId);
            }
        }
    }
}
