using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Activities
{
    public enum ActivityStrengthClass
    {
        Ordinary = 0,
        Strong = 1,
    }

    public sealed class ActivityPlacementProfile
    {
        private readonly ReadOnlyCollection<MoonpalaceBiomeId> allowedBiomes;
        private readonly ReadOnlyCollection<PacingRole> allowedPacingRoles;
        private readonly ReadOnlyCollection<AccessClass> allowedAccessClasses;

        public ActivityPlacementProfile(
            ActivityStructureId activityId,
            TerrainClusterId terrainClusterId,
            SpineVariantId spineVariantId,
            string activityDigest,
            string shellDigest,
            string removalSafetyDigest,
            IEnumerable<MoonpalaceBiomeId> allowedBiomes,
            IEnumerable<PacingRole> allowedPacingRoles,
            IEnumerable<AccessClass> allowedAccessClasses,
            int minimumActiveChunkCount,
            int maximumActiveChunkCount,
            int requiredOpenClearanceWidth,
            int requiredOpenClearanceHeight,
            int weight,
            ActivityStrengthClass strength)
        {
            ActivityId = activityId;
            TerrainClusterId = terrainClusterId;
            SpineVariantId = spineVariantId;
            ActivityDigest = activityDigest ?? string.Empty;
            ShellDigest = shellDigest ?? string.Empty;
            RemovalSafetyDigest = removalSafetyDigest ?? string.Empty;
            this.allowedBiomes = CopyDistinct(allowedBiomes, CompareBiome);
            this.allowedPacingRoles = CopyDistinct(allowedPacingRoles, CompareEnum);
            this.allowedAccessClasses = CopyDistinct(allowedAccessClasses, CompareEnum);
            MinimumActiveChunkCount = minimumActiveChunkCount;
            MaximumActiveChunkCount = maximumActiveChunkCount;
            RequiredOpenClearanceWidth = requiredOpenClearanceWidth;
            RequiredOpenClearanceHeight = requiredOpenClearanceHeight;
            Weight = weight;
            Strength = strength;
        }

        public ActivityStructureId ActivityId { get; }
        public TerrainClusterId TerrainClusterId { get; }
        public SpineVariantId SpineVariantId { get; }
        public string ActivityDigest { get; }
        public string ShellDigest { get; }
        public string RemovalSafetyDigest { get; }
        public IReadOnlyList<MoonpalaceBiomeId> AllowedBiomes => allowedBiomes;
        public IReadOnlyList<PacingRole> AllowedPacingRoles => allowedPacingRoles;
        public IReadOnlyList<AccessClass> AllowedAccessClasses => allowedAccessClasses;
        public int MinimumActiveChunkCount { get; }
        public int MaximumActiveChunkCount { get; }
        public int RequiredOpenClearanceWidth { get; }
        public int RequiredOpenClearanceHeight { get; }
        public int Weight { get; }
        public ActivityStrengthClass Strength { get; }

        private static ReadOnlyCollection<T> CopyDistinct<T>(
            IEnumerable<T> values,
            Comparison<T> comparison)
        {
            var copy = values == null ? Array.Empty<T>() : values.Distinct().ToArray();
            Array.Sort(copy, comparison);
            return new ReadOnlyCollection<T>(copy);
        }

        private static int CompareBiome(MoonpalaceBiomeId left, MoonpalaceBiomeId right)
        {
            if (!left.IsDefined) return right.IsDefined ? 1 : 0;
            return !right.IsDefined ? -1 : left.CompareTo(right);
        }

        private static int CompareEnum<T>(T left, T right)
        {
            return Convert.ToInt32(left).CompareTo(Convert.ToInt32(right));
        }
    }

    public sealed class ActivityPlacementClearanceEvidence
    {
        private readonly ReadOnlyCollection<LocalTileCoord> coordinates;
        private readonly ReadOnlyCollection<LocalTileCoord> airCoordinates;
        private readonly ReadOnlyCollection<LocalTileCoord> reservedCoordinates;
        private readonly ReadOnlyCollection<LocalTileCoord> absoluteProtectedCoordinates;

        public ActivityPlacementClearanceEvidence(
            LocalTileCoord origin,
            int width,
            int height,
            IEnumerable<LocalTileCoord> coordinates,
            IEnumerable<LocalTileCoord> finalWorkingCanvasAirCoordinates,
            IEnumerable<LocalTileCoord> deviceHazardProjectileReservedCoordinates,
            IEnumerable<LocalTileCoord> absoluteProtectedCoordinates)
        {
            Origin = origin;
            Width = width;
            Height = height;
            this.coordinates = CopyCoordinates(coordinates);
            airCoordinates = CopyCoordinates(finalWorkingCanvasAirCoordinates);
            reservedCoordinates = CopyCoordinates(deviceHazardProjectileReservedCoordinates);
            this.absoluteProtectedCoordinates = CopyCoordinates(absoluteProtectedCoordinates);
        }

        public LocalTileCoord Origin { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<LocalTileCoord> Coordinates => coordinates;
        public IReadOnlyList<LocalTileCoord> FinalWorkingCanvasAirCoordinates => airCoordinates;
        public IReadOnlyList<LocalTileCoord> DeviceHazardProjectileReservedCoordinates => reservedCoordinates;
        public IReadOnlyList<LocalTileCoord> AbsoluteProtectedCoordinates => absoluteProtectedCoordinates;

        private static ReadOnlyCollection<LocalTileCoord> CopyCoordinates(IEnumerable<LocalTileCoord> values)
        {
            var copy = values == null ? Array.Empty<LocalTileCoord>() : values.ToArray();
            Array.Sort(copy, ActivityCompatibilityOrdering.CompareCoordinates);
            return new ReadOnlyCollection<LocalTileCoord>(copy);
        }
    }

    public sealed class ActivityPlacementOpportunity
    {
        public ActivityPlacementOpportunity(
            string opportunityId,
            SectorCoord sector,
            BiomePatchId patchId,
            MoonpalaceBiomeId primaryBiome,
            TerrainClusterId terrainClusterId,
            SpineVariantId spineVariantId,
            PacingRole pacingRole,
            AccessClass accessClass,
            int activeChunkCount,
            ActivityPlacementClearanceEvidence clearance,
            string map11CatalogDigest,
            string map11SignatureSetDigest,
            string authoringManifestDigest,
            string activityShellDigest,
            string removalSafetyDigest)
        {
            OpportunityId = opportunityId ?? string.Empty;
            Sector = sector;
            PatchId = patchId;
            PrimaryBiome = primaryBiome;
            TerrainClusterId = terrainClusterId;
            SpineVariantId = spineVariantId;
            PacingRole = pacingRole;
            AccessClass = accessClass;
            ActiveChunkCount = activeChunkCount;
            Clearance = clearance;
            Map11CatalogDigest = map11CatalogDigest ?? string.Empty;
            Map11SignatureSetDigest = map11SignatureSetDigest ?? string.Empty;
            AuthoringManifestDigest = authoringManifestDigest ?? string.Empty;
            ActivityShellDigest = activityShellDigest ?? string.Empty;
            RemovalSafetyDigest = removalSafetyDigest ?? string.Empty;
        }

        public string OpportunityId { get; }
        public SectorCoord Sector { get; }
        public BiomePatchId PatchId { get; }
        public MoonpalaceBiomeId PrimaryBiome { get; }
        public TerrainClusterId TerrainClusterId { get; }
        public SpineVariantId SpineVariantId { get; }
        public PacingRole PacingRole { get; }
        public AccessClass AccessClass { get; }
        public int ActiveChunkCount { get; }
        public ActivityPlacementClearanceEvidence Clearance { get; }
        public string Map11CatalogDigest { get; }
        public string Map11SignatureSetDigest { get; }
        public string AuthoringManifestDigest { get; }
        public string ActivityShellDigest { get; }
        public string RemovalSafetyDigest { get; }
    }

    public enum ActivityCompatibilityRejectionCode
    {
        BiomeMismatch,
        PacingRoleMismatch,
        AccessClassMismatch,
        ActiveChunkCountMismatch,
        TerrainClusterMismatch,
        SpineVariantMismatch,
        ActivityShellDigestMismatch,
        RemovalSafetyDigestMismatch,
        ClearanceTooSmall,
        ClearanceNotRectangular,
        ClearanceNotAir,
        ClearanceReserved,
        ClearanceAbsoluteProtected,
        DuplicateCandidate,
    }

    public sealed class ActivityCompatibilityRejection
    {
        public ActivityCompatibilityRejection(
            string opportunityId,
            ActivityStructureId activityId,
            ActivityCompatibilityRejectionCode code,
            string path,
            string detail)
        {
            OpportunityId = opportunityId ?? string.Empty;
            ActivityId = activityId;
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string OpportunityId { get; }
        public ActivityStructureId ActivityId { get; }
        public ActivityCompatibilityRejectionCode Code { get; }
        public string Path { get; }
        public string Detail { get; }
    }

    internal static class ActivityCompatibilityOrdering
    {
        public static int CompareCoordinates(LocalTileCoord left, LocalTileCoord right)
        {
            var comparison = left.Y.CompareTo(right.Y);
            return comparison != 0 ? comparison : left.X.CompareTo(right.X);
        }

        public static int CompareSectors(SectorCoord left, SectorCoord right)
        {
            var comparison = left.Y.CompareTo(right.Y);
            return comparison != 0 ? comparison : left.X.CompareTo(right.X);
        }
    }
}
