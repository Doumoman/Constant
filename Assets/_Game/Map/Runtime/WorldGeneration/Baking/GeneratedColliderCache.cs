using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class GeneratedColliderRebuildPlanner
    {
        public static GeneratedColliderRebuildResult Build(GeneratedColliderRebuildRequest request)
        {
            var failures = new List<GeneratedColliderRebuildFailure>();
            if (request == null)
            {
                Add(failures, GeneratedColliderRebuildFailureCode.MissingRequest, "request",
                    "A collider rebuild request is required.");
                return new GeneratedColliderRebuildResult(null, null, failures);
            }

            var bake = request.BakePlan;
            if (bake == null)
                Add(failures, GeneratedColliderRebuildFailureCode.MissingBakePlan, "bakePlan",
                    "A MAP17_02 logical bake plan is required.");
            else
                ValidateBake(request, bake, failures);
            if (request.MutationRevision < 0)
                Add(failures, GeneratedColliderRebuildFailureCode.InvalidMutationRevision,
                    "mutationRevision", "Mutation revision must be non-negative.");
            if (string.IsNullOrWhiteSpace(request.DirtyReason))
                Add(failures, GeneratedColliderRebuildFailureCode.InvalidDirtyReason,
                    "dirtyReason", "A stable dirty reason is required.");
            if (request.NullRecordCount != 0)
                Add(failures, GeneratedColliderRebuildFailureCode.InvalidSourceRecord,
                    "sourceRecords", "Null source records are forbidden.");
            if (failures.Count != 0)
                return new GeneratedColliderRebuildResult(request, null, failures);

            var width = GeneratedTerrainGeometrySnapshot.CanonicalSectorWidth;
            var height = GeneratedTerrainGeometrySnapshot.CanonicalSectorHeight;
            var indicesByKind = Enum.GetValues(typeof(GeneratedCollisionMaskKind))
                .Cast<GeneratedCollisionMaskKind>()
                .ToDictionary(value => value, value => new List<int>());
            var classifiedRecordCount = 0;
            foreach (var record in request.SourceRecords)
            {
                var kind = Classify(record);
                if (!kind.HasValue) continue;
                indicesByKind[kind.Value].Add(record.SectorLocalIndex);
                classifiedRecordCount++;
            }

            var colliding = new HashSet<int>(indicesByKind
                .Where(pair => pair.Key != GeneratedCollisionMaskKind.NonCollidingDebug)
                .SelectMany(pair => pair.Value));
            for (var index = 0; index < width * height; index++)
                if (!colliding.Contains(index))
                    indicesByKind[GeneratedCollisionMaskKind.NonCollidingDebug].Add(index);

            var masks = indicesByKind.OrderBy(pair => pair.Key).Select(pair =>
            {
                var valid = pair.Value.Where(value => value >= 0 && value < width * height).ToArray();
                return new GeneratedColliderCellMask(pair.Key, width, height, valid,
                    valid.Length - valid.Distinct().Count(),
                    pair.Value.Count - valid.Length);
            }).ToArray();
            if (masks.Any(value => value.DuplicateSourceCellCount != 0))
                Add(failures, GeneratedColliderRebuildFailureCode.DuplicateMaskCell, "masks",
                    "Each mask kind must contain a cell at most once.");
            if (masks.Any(value => value.OutOfBoundsSourceCellCount != 0))
                Add(failures, GeneratedColliderRebuildFailureCode.OutOfBoundsMaskCell, "masks",
                    "Mask cells must remain inside the 48x32 sector.");

            var spans = masks.SelectMany(BuildHorizontalSpans).OrderBy(value => value).ToArray();
            var commands = spans.Where(value => value.MaskKind !=
                    GeneratedCollisionMaskKind.NonCollidingDebug)
                .Select((value, index) => new GeneratedColliderAdapterCommand(value, index)).ToArray();
            var plan = new GeneratedColliderRebuildPlan(request, masks, spans, commands,
                request.SourceRecords.Count - classifiedRecordCount);
            if (!plan.SpanCellsExactlyMatchMasks || plan.SpanOutOfBoundsCellCount != 0)
                Add(failures, GeneratedColliderRebuildFailureCode.MaskSpanMismatch, "spans",
                    "Horizontal spans must exactly cover their source masks inside sector bounds.");
            if (!GeneratedColliderRebuildDigest.IsLowerHexSha256(request.CanonicalDigest) ||
                !GeneratedColliderRebuildDigest.IsLowerHexSha256(plan.OutputDigest))
                Add(failures, GeneratedColliderRebuildFailureCode.InvalidDigest, "digest",
                    "Input and output digests must be lower-hex SHA-256.");
            return failures.Count == 0
                ? new GeneratedColliderRebuildResult(request, plan,
                    Array.Empty<GeneratedColliderRebuildFailure>())
                : new GeneratedColliderRebuildResult(request, null, failures);
        }

        private static void ValidateBake(
            GeneratedColliderRebuildRequest request,
            GeneratedTilemapBakePlan bake,
            ICollection<GeneratedColliderRebuildFailure> failures)
        {
            var expectedRecords = bake.LayerBuffers.SelectMany(value => value.Records)
                .OrderBy(value => value).Select(value => value.StableToken).ToArray();
            var actualRecords = request.SourceRecords.OrderBy(value => value)
                .Select(value => value.StableToken).ToArray();
            if (bake.LayerCount != SectorFinalCanvasLayerPlan.RequiredLayerCount ||
                bake.TotalLayerRecordCount != GeneratedTerrainGeometrySnapshot.CanonicalSectorLayerRecordCount ||
                bake.SectorCellCoverageCount != GeneratedTerrainGeometrySnapshot.CanonicalSectorCellCount ||
                bake.MissingLayerCellCount != 0 || bake.DuplicateLayerCellCount != 0 ||
                bake.OutOfBoundsLayerCellCount != 0 || bake.SeamReport == null ||
                (bake.SeamReport != null && GeneratedTilemapSeamValidator
                    .ValidateExposures(bake.SeamReport.Exposures).Count != 0) ||
                !expectedRecords.SequenceEqual(actualRecords) ||
                !string.Equals(GeneratedTilemapBakeDigest.ComputeOutput(bake), bake.OutputDigest,
                    StringComparison.Ordinal))
                Add(failures, GeneratedColliderRebuildFailureCode.StaleBakePlan, "bakePlan",
                    "The logical bake plan or its complete ordered record set is stale.");
        }

        private static GeneratedCollisionMaskKind? Classify(GeneratedTilemapCellBakeRecord record)
        {
            switch (record.LayerId)
            {
                case GeneratedTilemapLayerId.Terrain:
                    return record.CellKind == FinalCanvasCellKind.Solid ||
                           record.CellKind == FinalCanvasCellKind.Ground ||
                           record.CellKind == FinalCanvasCellKind.Blocked
                        ? GeneratedCollisionMaskKind.Solid : (GeneratedCollisionMaskKind?)null;
                case GeneratedTilemapLayerId.Affordance:
                    return record.CellKind == FinalCanvasCellKind.Traversable ||
                           record.CellKind == FinalCanvasCellKind.Ground
                        ? GeneratedCollisionMaskKind.Platform : (GeneratedCollisionMaskKind?)null;
                case GeneratedTilemapLayerId.Hazard:
                    return record.CellKind == FinalCanvasCellKind.Hazard ||
                           record.CellKind == FinalCanvasCellKind.Blocked
                        ? GeneratedCollisionMaskKind.Hazard : (GeneratedCollisionMaskKind?)null;
                case GeneratedTilemapLayerId.Protection:
                    return record.IsProtected || record.Protection != FinalCanvasProtectionKind.None ||
                           record.CellKind == FinalCanvasCellKind.ProtectedOpen
                        ? GeneratedCollisionMaskKind.Protection : (GeneratedCollisionMaskKind?)null;
                default:
                    return null;
            }
        }

        private static IEnumerable<GeneratedColliderSpan> BuildHorizontalSpans(
            GeneratedColliderCellMask mask)
        {
            for (var y = 0; y < mask.Height; y++)
            {
                var x = 0;
                while (x < mask.Width)
                {
                    if (!mask.IsOccupied(y * mask.Width + x))
                    {
                        x++;
                        continue;
                    }
                    var start = x;
                    while (x < mask.Width && mask.IsOccupied(y * mask.Width + x)) x++;
                    yield return new GeneratedColliderSpan(mask.Kind, start, y, x - start, mask.Width);
                }
            }
        }

        private static void Add(
            ICollection<GeneratedColliderRebuildFailure> failures,
            GeneratedColliderRebuildFailureCode code,
            string subject,
            string reason) => failures.Add(new GeneratedColliderRebuildFailure(code, subject, reason));
    }

    public sealed class GeneratedColliderCacheKey :
        IEquatable<GeneratedColliderCacheKey>, IComparable<GeneratedColliderCacheKey>
    {
        public GeneratedColliderCacheKey(
            string geometryDigest,
            string bakeDigest,
            string seamDigest,
            string registryDigest,
            GeneratedSectorIndexCoordinate sector,
            string generatorVersion,
            string dataVersion,
            int mutationRevision,
            string collisionPolicyVersion)
        {
            GeometryDigest = geometryDigest ?? string.Empty;
            BakeDigest = bakeDigest ?? string.Empty;
            SeamDigest = seamDigest ?? string.Empty;
            RegistryDigest = registryDigest ?? string.Empty;
            Sector = sector;
            GeneratorVersion = generatorVersion ?? string.Empty;
            DataVersion = dataVersion ?? string.Empty;
            MutationRevision = mutationRevision;
            CollisionPolicyVersion = collisionPolicyVersion ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "COLLIDER_CACHE_KEY", GeometryDigest, BakeDigest, SeamDigest, RegistryDigest,
                Sector == null ? "MISSING" : Sector.ToString(), GeneratorVersion, DataVersion,
                Number(MutationRevision), CollisionPolicyVersion,
            });
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[] { StableToken });
        }

        public string GeometryDigest { get; }
        public string BakeDigest { get; }
        public string SeamDigest { get; }
        public string RegistryDigest { get; }
        public GeneratedSectorIndexCoordinate Sector { get; }
        public string GeneratorVersion { get; }
        public string DataVersion { get; }
        public int MutationRevision { get; }
        public string CollisionPolicyVersion { get; }
        public string StableToken { get; }
        public string Digest { get; }
        public bool IsValid => GeneratedColliderRebuildDigest.IsLowerHexSha256(GeometryDigest) &&
            GeneratedColliderRebuildDigest.IsLowerHexSha256(BakeDigest) &&
            GeneratedColliderRebuildDigest.IsLowerHexSha256(SeamDigest) &&
            GeneratedColliderRebuildDigest.IsLowerHexSha256(RegistryDigest) &&
            Sector != null && Sector.IsInBounds && !string.IsNullOrWhiteSpace(GeneratorVersion) &&
            !string.IsNullOrWhiteSpace(DataVersion) && MutationRevision >= 0 &&
            string.Equals(CollisionPolicyVersion, GeneratedColliderRebuildPlan.CollisionPolicyVersion,
                StringComparison.Ordinal) && GeneratedColliderRebuildDigest.IsLowerHexSha256(Digest);

        public static GeneratedColliderCacheKey Create(
            GeneratedColliderRebuildPlan plan,
            string generatorVersion,
            string dataVersion)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var placementRequest = plan.SourceBakePlan.Request.PlacementPlan.Request;
            return new GeneratedColliderCacheKey(
                placementRequest.ExpectedGeometryDigest,
                plan.SourceBakePlan.OutputDigest,
                plan.SourceBakePlan.SeamReport.OutputDigest,
                plan.SourceBakePlan.Request.AssetRegistry.Digest,
                placementRequest.SectorIndex,
                generatorVersion,
                dataVersion,
                plan.MutationRevision,
                GeneratedColliderRebuildPlan.CollisionPolicyVersion);
        }

        public int CompareTo(GeneratedColliderCacheKey other) => other == null ? -1 :
            string.Compare(Digest, other.Digest, StringComparison.Ordinal);
        public bool Equals(GeneratedColliderCacheKey other) => other != null &&
            string.Equals(Digest, other.Digest, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedColliderCacheKey);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Digest);
        public override string ToString() => Digest;
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedColliderCacheEntry : IComparable<GeneratedColliderCacheEntry>
    {
        public GeneratedColliderCacheEntry(
            GeneratedColliderCacheKey key,
            GeneratedColliderRebuildPlan rebuildPlan)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            RebuildPlan = rebuildPlan ?? throw new ArgumentNullException(nameof(rebuildPlan));
            StableToken = "COLLIDER_CACHE_ENTRY|" + Key.Digest + "|" + RebuildPlan.OutputDigest;
        }

        public GeneratedColliderCacheKey Key { get; }
        public GeneratedColliderRebuildPlan RebuildPlan { get; }
        public string StableToken { get; }
        public bool IsCoherent => Key.IsValid && Key.MutationRevision == RebuildPlan.MutationRevision &&
            string.Equals(Key.BakeDigest, RebuildPlan.SourceBakePlan.OutputDigest, StringComparison.Ordinal) &&
            string.Equals(Key.SeamDigest, RebuildPlan.SourceBakePlan.SeamReport.OutputDigest,
                StringComparison.Ordinal);
        public int CompareTo(GeneratedColliderCacheEntry other) => other == null ? -1 :
            Key.CompareTo(other.Key);
    }

    public sealed class GeneratedColliderCacheLookupResult
    {
        internal GeneratedColliderCacheLookupResult(
            bool hit,
            GeneratedColliderCacheEntry entry,
            GeneratedColliderCacheSnapshot snapshot)
        {
            Hit = hit;
            Entry = entry;
            Snapshot = snapshot;
        }

        public bool Hit { get; }
        public GeneratedColliderCacheEntry Entry { get; }
        public GeneratedColliderCacheSnapshot Snapshot { get; }
    }

    public sealed class GeneratedColliderCacheSnapshot
    {
        private readonly ReadOnlyCollection<GeneratedColliderCacheEntry> entries;

        private GeneratedColliderCacheSnapshot(
            IEnumerable<GeneratedColliderCacheEntry> sourceEntries,
            int hitCount,
            int missCount,
            int invalidatedCount,
            int evictedCount)
        {
            entries = new ReadOnlyCollection<GeneratedColliderCacheEntry>((sourceEntries ??
                Array.Empty<GeneratedColliderCacheEntry>()).Where(value => value != null)
                .GroupBy(value => value.Key.Digest, StringComparer.Ordinal)
                .Select(group => group.Last()).OrderBy(value => value).ToArray());
            HitCount = hitCount;
            MissCount = missCount;
            InvalidatedCount = invalidatedCount;
            EvictedCount = evictedCount;
            Digest = ComputeDigest();
        }

        public static GeneratedColliderCacheSnapshot Empty { get; } =
            new GeneratedColliderCacheSnapshot(Array.Empty<GeneratedColliderCacheEntry>(), 0, 0, 0, 0);
        public IReadOnlyList<GeneratedColliderCacheEntry> Entries => entries;
        public int EntryCount => entries.Count;
        public int HitCount { get; }
        public int MissCount { get; }
        public int InvalidatedCount { get; }
        public int EvictedCount { get; }
        public string Digest { get; }

        public GeneratedColliderCacheSnapshot Store(GeneratedColliderCacheEntry entry)
        {
            if (entry == null || !entry.IsCoherent)
                throw new ArgumentException("A coherent collider cache entry is required.", nameof(entry));
            return new GeneratedColliderCacheSnapshot(entries.Where(value => !value.Key.Equals(entry.Key))
                .Concat(new[] { entry }), HitCount, MissCount, InvalidatedCount, EvictedCount);
        }

        public GeneratedColliderCacheLookupResult Lookup(GeneratedColliderCacheKey key)
        {
            var entry = key == null ? null : entries.SingleOrDefault(value => value.Key.Equals(key));
            var hit = entry != null;
            return new GeneratedColliderCacheLookupResult(hit, entry,
                new GeneratedColliderCacheSnapshot(entries, HitCount + (hit ? 1 : 0),
                    MissCount + (hit ? 0 : 1), InvalidatedCount, EvictedCount));
        }

        public GeneratedColliderCacheSnapshot Invalidate(GeneratedColliderCacheKey key)
        {
            var removed = key == null ? 0 : entries.Count(value => value.Key.Equals(key));
            return new GeneratedColliderCacheSnapshot(entries.Where(value => key == null ||
                !value.Key.Equals(key)), HitCount, MissCount, InvalidatedCount + removed, EvictedCount);
        }

        public GeneratedColliderCacheSnapshot EvictToCapacity(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            var retained = entries.Take(capacity).ToArray();
            return new GeneratedColliderCacheSnapshot(retained, HitCount, MissCount,
                InvalidatedCount, EvictedCount + entries.Count - retained.Length);
        }

        private string ComputeDigest()
        {
            var lines = new List<string>
            {
                "COLLIDER_CACHE_SNAPSHOT|" + Number(HitCount) + "|" + Number(MissCount) + "|" +
                    Number(InvalidatedCount) + "|" + Number(EvictedCount),
            };
            lines.AddRange(entries.OrderBy(value => value).Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
