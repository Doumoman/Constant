using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedTilemapLayerBaker
    {
        private static readonly GeneratedTilemapLayerId[] RequiredLayers =
            (GeneratedTilemapLayerId[])Enum.GetValues(typeof(GeneratedTilemapLayerId));

        public static GeneratedTilemapBakeResult Bake(GeneratedTilemapBakeRequest request)
        {
            var failures = ValidateRequest(request).ToList();
            if (request == null)
                return Result(null, null, failures);
            if (request.NullRecordCount > 0)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.Gap,
                    Number(request.NullRecordCount), "Null layer records are not valid gaps."));
            failures.AddRange(ValidateLayerRecords(request.SourceRecords,
                request.PlacementPlan, request.AssetRegistry));
            if (failures.Count > 0)
                return Result(request, null, failures);

            var buffers = RequiredLayers.OrderBy(value => value)
                .Select(layer => new GeneratedTilemapLayerBuffer(layer,
                    request.SourceRecords.Where(value => value.LayerId == layer)))
                .ToArray();
            var commands = request.SourceRecords.Select(value =>
                new GeneratedTilemapBakeCommand(value)).ToArray();
            var seamReport = GeneratedTilemapSeamValidator.BuildReport(
                request.SourceRecords, request.PlacementPlan.Request.Geometry);
            failures.AddRange(GeneratedTilemapSeamValidator.ValidateExposures(seamReport.Exposures));
            if (seamReport.MicroPatternSeamPairCount != 688 ||
                seamReport.MicroChunkSeamPairCount != 240 ||
                seamReport.MicroPatternOnlySeamPairCount != 448)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.InvalidDigest,
                    "seam_counts", "Canonical 4x4 and 12x8 seam counts must be 688/240/448."));
            if (!GeneratedTilemapSeamDigest.IsLowerHexSha256(seamReport.OutputDigest) ||
                !string.Equals(GeneratedTilemapSeamDigest.Compute(seamReport),
                    seamReport.OutputDigest, StringComparison.Ordinal))
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.InvalidDigest,
                    "seam_digest", "The seam report digest is missing or stale."));
            if (failures.Count > 0)
                return Result(request, null, failures);

            var plan = new GeneratedTilemapBakePlan(request, buffers, commands,
                request.PlacementPlan.SocketReferences,
                request.PlacementPlan.Records.SelectMany(value => value.SlotReferences), seamReport);
            if (plan.LayerCount != 7 || plan.TotalLayerRecordCount != 10752 ||
                plan.UniqueLayerCellKeyCount != 10752 || plan.SectorCellCoverageCount != 1536 ||
                plan.MissingLayerCellCount != 0 || plan.DuplicateLayerCellCount != 0 ||
                plan.OutOfBoundsLayerCellCount != 0 || plan.CommandCount != 10752)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.InvalidDigest,
                    "bake_counts", "The logical bake packet does not match canonical layer coverage."));
            if (!GeneratedTilemapBakeDigest.IsLowerHexSha256(plan.InputDigest) ||
                !GeneratedTilemapBakeDigest.IsLowerHexSha256(plan.OutputDigest) ||
                !string.Equals(GeneratedTilemapBakeDigest.ComputeOutput(plan),
                    plan.OutputDigest, StringComparison.Ordinal))
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.InvalidDigest,
                    "bake_digest", "The logical bake packet digest is missing or stale."));
            return failures.Count == 0
                ? Result(request, plan, failures)
                : Result(request, null, failures);
        }

        public static IReadOnlyList<GeneratedTilemapBakeFailure> ValidateLayerRecords(
            IEnumerable<GeneratedTilemapCellBakeRecord> sourceRecords,
            GeneratedCellPlacementPlan placementPlan,
            GeneratedTerrainAssetRegistrySnapshot assetRegistry)
        {
            var failures = new List<GeneratedTilemapBakeFailure>();
            var raw = (sourceRecords ?? Array.Empty<GeneratedTilemapCellBakeRecord>()).ToArray();
            var records = raw.Where(value => value != null).ToArray();
            var geometry = placementPlan == null || placementPlan.Request == null
                ? null : placementPlan.Request.Geometry;
            var invalidLayers = records.Count(value => !Enum.IsDefined(
                typeof(GeneratedTilemapLayerId), value.LayerId));
            if (invalidLayers > 0)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.InvalidLayerId,
                    Number(invalidLayers), "Every record must use one of the seven approved layers."));
            var outOfBounds = records.Count(value => !value.IsCoordinateValid(geometry));
            if (outOfBounds > 0)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.OutOfBoundsLayerCell,
                    Number(outOfBounds), "Layer-cell coordinates must be inside the canonical 48x32 sector."));

            var valid = records.Where(value => Enum.IsDefined(typeof(GeneratedTilemapLayerId),
                    value.LayerId) && value.IsCoordinateValid(geometry)).ToArray();
            var duplicateCount = valid.GroupBy(Key, StringComparer.Ordinal)
                .Sum(group => Math.Max(0, group.Count() - 1));
            if (duplicateCount > 0)
            {
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.DuplicateLayerCell,
                    Number(duplicateCount), "A layer and sector-local index may be emitted only once."));
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.Overlap,
                    Number(duplicateCount), "Overlapping logical layer records are rejected without repair."));
            }
            var uniqueKeys = new HashSet<string>(valid.Select(Key), StringComparer.Ordinal);
            var expectedRecordCount = geometry == null ? 0 :
                geometry.SectorCellCount * RequiredLayers.Length;
            var missingCount = Math.Max(0, expectedRecordCount - uniqueKeys.Count);
            if (missingCount > 0)
            {
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.MissingLayerCell,
                    Number(missingCount), "Every canonical cell requires one record on each layer."));
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.Gap,
                    Number(missingCount), "Logical layer gaps are rejected without repair."));
            }

            ValidatePlacementIdentity(valid, placementPlan, failures);
            ValidateAssets(valid, placementPlan, assetRegistry, failures);
            return Ordered(failures);
        }

        private static IEnumerable<GeneratedTilemapBakeFailure> ValidateRequest(
            GeneratedTilemapBakeRequest request)
        {
            var failures = new List<GeneratedTilemapBakeFailure>();
            if (request == null)
            {
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.MissingRequest,
                    "request", "A logical tilemap bake request is required."));
                return failures;
            }
            var plan = request.PlacementPlan;
            if (plan == null)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.MissingPlacementPlan,
                    "placement", "The immutable MAP17_01 placement plan is required."));
            else if (!GeneratedCellPlacementDigest.IsLowerHexSha256(request.ExpectedPlacementDigest) ||
                !string.Equals(plan.OutputDigest, request.ExpectedPlacementDigest, StringComparison.Ordinal) ||
                !string.Equals(GeneratedCellPlacementDigest.ComputeOutput(plan),
                    plan.OutputDigest, StringComparison.Ordinal) ||
                plan.PlacedCellCount != GeneratedTerrainGeometrySnapshot.CanonicalSectorCellCount ||
                plan.PlacedLayerReferenceCount !=
                    GeneratedTerrainGeometrySnapshot.CanonicalSectorLayerRecordCount ||
                plan.DuplicateSectorCoordinateCount != 0 || plan.MissingSectorCoordinateCount != 0 ||
                plan.OutOfBoundsCoordinateCount != 0 || plan.AssetResolution == null ||
                !plan.AssetResolution.Success)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.StalePlacementInput,
                    "placement", "The MAP17_01 placement plan or expected digest is stale."));
            return failures;
        }

        private static void ValidatePlacementIdentity(
            IEnumerable<GeneratedTilemapCellBakeRecord> sourceRecords,
            GeneratedCellPlacementPlan placementPlan,
            ICollection<GeneratedTilemapBakeFailure> failures)
        {
            if (placementPlan == null) return;
            var expected = placementPlan.Records.SelectMany(placement => placement.Layers
                .Select(layer => GeneratedTilemapCellBakeRecord.FromPlacement(placement, layer)))
                .ToDictionary(Key, value => value, StringComparer.Ordinal);
            var mismatches = 0;
            foreach (var record in sourceRecords)
            {
                GeneratedTilemapCellBakeRecord expectedRecord;
                if (!expected.TryGetValue(Key(record), out expectedRecord) ||
                    !string.Equals(record.StableToken, expectedRecord.StableToken,
                        StringComparison.Ordinal))
                    mismatches++;
            }
            var sourceKeys = new HashSet<string>(sourceRecords.Select(Key), StringComparer.Ordinal);
            mismatches += expected.Keys.Count(value => !sourceKeys.Contains(value));
            if (mismatches > 0)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.StalePlacementInput,
                    Number(mismatches), "Layer records must preserve MAP17_01 tile and provenance identity."));
            var invalidProvenance = sourceRecords.Count(value =>
                string.IsNullOrEmpty(value.PlacementId) || string.IsNullOrEmpty(value.ProvenanceId) ||
                string.IsNullOrEmpty(value.SourceCellToken) ||
                string.IsNullOrEmpty(value.SourceLayerStableToken));
            if (invalidProvenance > 0)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.InvalidProvenance,
                    Number(invalidProvenance), "Source placement and layer provenance cannot be dropped."));
        }

        private static void ValidateAssets(
            IEnumerable<GeneratedTilemapCellBakeRecord> sourceRecords,
            GeneratedCellPlacementPlan placementPlan,
            GeneratedTerrainAssetRegistrySnapshot assetRegistry,
            ICollection<GeneratedTilemapBakeFailure> failures)
        {
            if (placementPlan == null) return;
            var records = sourceRecords.ToArray();
            var prefabs = placementPlan.Records.SelectMany(value => value.SlotReferences)
                .Select(value => value.PrefabId).ToArray();
            var resolution = GeneratedTerrainAssetResolver.Resolve(assetRegistry,
                records.Select(value => value.TileCode), prefabs);
            if (!resolution.Success)
            {
                foreach (var failure in resolution.Failures)
                {
                    var code = failure.Code == GeneratedTerrainAssetResolutionFailureCode.MissingPrefabId ||
                               failure.Code == GeneratedTerrainAssetResolutionFailureCode.InvalidPrefabId
                        ? GeneratedTilemapBakeFailureCode.MissingPrefabId
                        : GeneratedTilemapBakeFailureCode.MissingTileCode;
                    failures.Add(Failure(code, failure.Subject, failure.Reason));
                }
                return;
            }
            var tiles = resolution.ResolvedTiles.ToDictionary(value => value.Code.Value,
                value => value.AssetKey, StringComparer.Ordinal);
            var mismatches = records.Count(record => record.TileCode == null ||
                !tiles.ContainsKey(record.TileCode.Value) ||
                !string.Equals(tiles[record.TileCode.Value], record.ResolvedAssetKey,
                    StringComparison.Ordinal));
            if (mismatches > 0)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.MissingTileCode,
                    Number(mismatches), "Resolved tile keys must match the MAP17_01 registry snapshot."));
            var resolvedPrefabs = resolution.ResolvedPrefabs.ToDictionary(value => value.Id.Value,
                value => value.AssetKey, StringComparer.Ordinal);
            var prefabMismatches = placementPlan.Records.SelectMany(value => value.SlotReferences)
                .Count(value => value.PrefabId == null || !resolvedPrefabs.ContainsKey(value.PrefabId.Value) ||
                    !string.Equals(resolvedPrefabs[value.PrefabId.Value], value.ResolvedAssetKey,
                        StringComparison.Ordinal));
            if (prefabMismatches > 0)
                failures.Add(Failure(GeneratedTilemapBakeFailureCode.MissingPrefabId,
                    Number(prefabMismatches), "Resolved marker-slot prefab keys must remain stable."));
        }

        private static string Key(GeneratedTilemapCellBakeRecord record) => string.Join("|", new[]
        {
            Number((int)record.LayerId), Number(record.SectorLocalIndex),
        });

        private static GeneratedTilemapBakeResult Result(
            GeneratedTilemapBakeRequest request,
            GeneratedTilemapBakePlan plan,
            IEnumerable<GeneratedTilemapBakeFailure> failures) =>
            new GeneratedTilemapBakeResult(request, plan, Ordered(failures));
        private static ReadOnlyCollection<GeneratedTilemapBakeFailure> Ordered(
            IEnumerable<GeneratedTilemapBakeFailure> failures) =>
            new ReadOnlyCollection<GeneratedTilemapBakeFailure>((failures ??
                Array.Empty<GeneratedTilemapBakeFailure>()).Distinct()
                .OrderBy(value => value).ToArray());
        private static GeneratedTilemapBakeFailure Failure(
            GeneratedTilemapBakeFailureCode code, string subject, string reason) =>
            new GeneratedTilemapBakeFailure(code, subject ?? string.Empty, reason);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
