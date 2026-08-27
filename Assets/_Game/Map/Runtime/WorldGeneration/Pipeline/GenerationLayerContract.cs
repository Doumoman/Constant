using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Pipeline
{
    public enum GenerationLayerId
    {
        RouteType = 10,
        SpecialRegion = 20,
        TerrainCluster = 30,
        MicroPattern = 40,
        ActivityStructure = 50,
        EventOverlay = 60,
        MicroChunk = 70,
    }

    public enum LayerResponsibilityId
    {
        SectorExternalConnectivity,
        GeneralRouteAccess,
        WorldReservedLandmark,
        SpecialEntryAccess,
        StaticTerrainTraversal,
        LocalPatternTileOperation,
        StrongGameplayIncident,
        MarkerOnlyRunVariation,
        SliceStorageAndBoundaryProjection,
    }

    public enum PacingRole
    {
        None,
        Quiet,
        Traversal,
        Discovery,
        Risk,
        Recovery,
        Safe,
        Machinery,
        Flow,
        Activity,
        Narrative,
        Reward,
        Landmark,
        Resource,
        Boss,
        Integrated,
    }

    public enum AccessClass
    {
        Unspecified,
        MandatoryNoTool,
        OptionalNoTool,
        OptionalTool,
        OptionalEnvironment,
        OptionalExplosive,
        OptionalHidden,
        ProgressionGate,
    }

    public enum LayerPacingMode
    {
        CompatibilityOnly,
        PreserveOnly,
    }

    public enum LayerAccessMode
    {
        GeneralAuthority,
        SpecialEntryAuthority,
        CompatibilityOnly,
        PreserveOnly,
    }

    public enum LayerOrderInvariantId
    {
        SpecialRegionBeforeTerrainCluster,
        TerrainClusterBeforeMicroPattern,
        MicroPatternBeforeActivityStructure,
        MicroPatternBeforeEventOverlay,
        ActivityStructureBeforeMicroChunk,
        EventOverlayBeforeMicroChunk,
        MicroChunkFinal,
    }

    public sealed class PacingRoleToken
    {
        public PacingRoleToken(PacingRole role, string token)
        {
            Role = role;
            Token = token ?? string.Empty;
        }

        public PacingRole Role { get; }
        public string Token { get; }
    }

    public sealed class AccessClassToken
    {
        public AccessClassToken(AccessClass accessClass, string token)
        {
            AccessClass = accessClass;
            Token = token ?? string.Empty;
        }

        public AccessClass AccessClass { get; }
        public string Token { get; }
    }

    public static class PacingRoleTokenCodec
    {
        private static readonly ReadOnlyCollection<PacingRoleToken> entries =
            new ReadOnlyCollection<PacingRoleToken>(new[]
            {
                new PacingRoleToken(PacingRole.Quiet, "QUIET"),
                new PacingRoleToken(PacingRole.Traversal, "TRAVERSAL"),
                new PacingRoleToken(PacingRole.Discovery, "DISCOVERY"),
                new PacingRoleToken(PacingRole.Risk, "RISK"),
                new PacingRoleToken(PacingRole.Recovery, "RECOVERY"),
                new PacingRoleToken(PacingRole.Safe, "SAFE"),
                new PacingRoleToken(PacingRole.Machinery, "MACHINERY"),
                new PacingRoleToken(PacingRole.Flow, "FLOW"),
                new PacingRoleToken(PacingRole.Activity, "ACTIVITY"),
                new PacingRoleToken(PacingRole.Narrative, "NARRATIVE"),
                new PacingRoleToken(PacingRole.Reward, "REWARD"),
                new PacingRoleToken(PacingRole.Landmark, "LANDMARK"),
                new PacingRoleToken(PacingRole.Resource, "RESOURCE"),
                new PacingRoleToken(PacingRole.Boss, "BOSS"),
                new PacingRoleToken(PacingRole.Integrated, "INTEGRATED"),
            });

        public static IReadOnlyList<PacingRoleToken> Entries => entries;

        public static bool IsPublished(PacingRole value)
        {
            return value >= PacingRole.Quiet && value <= PacingRole.Integrated;
        }

        public static bool TryParse(string token, out PacingRole value)
        {
            switch (token)
            {
                case "QUIET": value = PacingRole.Quiet; return true;
                case "TRAVERSAL": value = PacingRole.Traversal; return true;
                case "DISCOVERY": value = PacingRole.Discovery; return true;
                case "RISK": value = PacingRole.Risk; return true;
                case "RECOVERY": value = PacingRole.Recovery; return true;
                case "SAFE": value = PacingRole.Safe; return true;
                case "MACHINERY": value = PacingRole.Machinery; return true;
                case "FLOW": value = PacingRole.Flow; return true;
                case "ACTIVITY": value = PacingRole.Activity; return true;
                case "NARRATIVE": value = PacingRole.Narrative; return true;
                case "REWARD": value = PacingRole.Reward; return true;
                case "LANDMARK": value = PacingRole.Landmark; return true;
                case "RESOURCE": value = PacingRole.Resource; return true;
                case "BOSS": value = PacingRole.Boss; return true;
                case "INTEGRATED": value = PacingRole.Integrated; return true;
                default: value = PacingRole.None; return false;
            }
        }

        public static string ToToken(PacingRole value)
        {
            switch (value)
            {
                case PacingRole.Quiet: return "QUIET";
                case PacingRole.Traversal: return "TRAVERSAL";
                case PacingRole.Discovery: return "DISCOVERY";
                case PacingRole.Risk: return "RISK";
                case PacingRole.Recovery: return "RECOVERY";
                case PacingRole.Safe: return "SAFE";
                case PacingRole.Machinery: return "MACHINERY";
                case PacingRole.Flow: return "FLOW";
                case PacingRole.Activity: return "ACTIVITY";
                case PacingRole.Narrative: return "NARRATIVE";
                case PacingRole.Reward: return "REWARD";
                case PacingRole.Landmark: return "LANDMARK";
                case PacingRole.Resource: return "RESOURCE";
                case PacingRole.Boss: return "BOSS";
                case PacingRole.Integrated: return "INTEGRATED";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    public static class AccessClassTokenCodec
    {
        private static readonly ReadOnlyCollection<AccessClassToken> entries =
            new ReadOnlyCollection<AccessClassToken>(new[]
            {
                new AccessClassToken(AccessClass.MandatoryNoTool, "MANDATORY_NO_TOOL"),
                new AccessClassToken(AccessClass.OptionalNoTool, "OPTIONAL_NO_TOOL"),
                new AccessClassToken(AccessClass.OptionalTool, "OPTIONAL_TOOL"),
                new AccessClassToken(AccessClass.OptionalEnvironment, "OPTIONAL_ENVIRONMENT"),
                new AccessClassToken(AccessClass.OptionalExplosive, "OPTIONAL_EXPLOSIVE"),
                new AccessClassToken(AccessClass.OptionalHidden, "OPTIONAL_HIDDEN"),
                new AccessClassToken(AccessClass.ProgressionGate, "PROGRESSION_GATE"),
            });

        public static IReadOnlyList<AccessClassToken> Entries => entries;

        public static bool IsPublished(AccessClass value)
        {
            return value >= AccessClass.MandatoryNoTool && value <= AccessClass.ProgressionGate;
        }

        public static bool TryParse(string token, out AccessClass value)
        {
            switch (token)
            {
                case "MANDATORY_NO_TOOL": value = AccessClass.MandatoryNoTool; return true;
                case "OPTIONAL_NO_TOOL": value = AccessClass.OptionalNoTool; return true;
                case "OPTIONAL_TOOL": value = AccessClass.OptionalTool; return true;
                case "OPTIONAL_ENVIRONMENT": value = AccessClass.OptionalEnvironment; return true;
                case "OPTIONAL_EXPLOSIVE": value = AccessClass.OptionalExplosive; return true;
                case "OPTIONAL_HIDDEN": value = AccessClass.OptionalHidden; return true;
                case "PROGRESSION_GATE": value = AccessClass.ProgressionGate; return true;
                default: value = AccessClass.Unspecified; return false;
            }
        }

        public static string ToToken(AccessClass value)
        {
            switch (value)
            {
                case AccessClass.MandatoryNoTool: return "MANDATORY_NO_TOOL";
                case AccessClass.OptionalNoTool: return "OPTIONAL_NO_TOOL";
                case AccessClass.OptionalTool: return "OPTIONAL_TOOL";
                case AccessClass.OptionalEnvironment: return "OPTIONAL_ENVIRONMENT";
                case AccessClass.OptionalExplosive: return "OPTIONAL_EXPLOSIVE";
                case AccessClass.OptionalHidden: return "OPTIONAL_HIDDEN";
                case AccessClass.ProgressionGate: return "PROGRESSION_GATE";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    public sealed class PacingRoleSet
    {
        private readonly ReadOnlyCollection<PacingRole> roles;

        public PacingRoleSet(IEnumerable<PacingRole> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = source.ToArray();
            if (values.Length == 0)
                throw new ArgumentException("A pacing role set cannot be empty.", nameof(source));
            if (values.Any(value => !PacingRoleTokenCodec.IsPublished(value)))
                throw new ArgumentOutOfRangeException(nameof(source), "Pacing roles must be published atomic values.");
            if (values.Distinct().Count() != values.Length)
                throw new ArgumentException("A pacing role set cannot contain duplicates.", nameof(source));
            Array.Sort(values);
            roles = new ReadOnlyCollection<PacingRole>(values);
        }

        public IReadOnlyList<PacingRole> Roles => roles;
        public bool Contains(PacingRole value) => roles.Contains(value);
    }

    public static class AccessClassMappings
    {
        public static AccessClass FromOptionalRegionAccessRule(OptionalRegionAccessRule value)
        {
            switch (value)
            {
                case OptionalRegionAccessRule.Basic: return AccessClass.OptionalNoTool;
                case OptionalRegionAccessRule.Tool: return AccessClass.OptionalTool;
                case OptionalRegionAccessRule.Environment: return AccessClass.OptionalEnvironment;
                case OptionalRegionAccessRule.Explosive: return AccessClass.OptionalExplosive;
                case OptionalRegionAccessRule.Hidden: return AccessClass.OptionalHidden;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static bool TryMapMandatoryRoute(
            bool mandatoryAllowed,
            string toolRequirement,
            out AccessClass value)
        {
            if (mandatoryAllowed && string.Equals(toolRequirement, "NONE", StringComparison.Ordinal))
            {
                value = AccessClass.MandatoryNoTool;
                return true;
            }

            value = AccessClass.Unspecified;
            return false;
        }

        public static bool TryMapMandatoryBoundary(string toolRequirement, out AccessClass value)
        {
            return TryMapMandatoryRoute(true, toolRequirement, out value);
        }

        public static bool IsValidForGeneralMandatory(AccessClass value)
        {
            return value == AccessClass.MandatoryNoTool;
        }
    }

    public sealed class PacingAccessContract
    {
        public PacingAccessContract(int routeType, PacingRoleSet pacing, AccessClass access)
        {
            if (pacing == null) throw new ArgumentNullException(nameof(pacing));
            if (!AccessClassTokenCodec.IsPublished(access))
                throw new ArgumentOutOfRangeException(nameof(access));
            RouteType = routeType;
            Pacing = pacing;
            Access = access;
        }

        public int RouteType { get; }
        public PacingRoleSet Pacing { get; }
        public AccessClass Access { get; }

        public PacingAccessContract WithPacing(PacingRoleSet pacing)
        {
            return new PacingAccessContract(RouteType, pacing, Access);
        }

        public PacingAccessContract WithAccess(AccessClass access)
        {
            return new PacingAccessContract(RouteType, Pacing, access);
        }
    }

    public sealed class GenerationLayerOrderInvariant
    {
        public GenerationLayerOrderInvariant(
            LayerOrderInvariantId invariantId,
            GenerationLayerId before,
            GenerationLayerId after,
            bool requiresFinalLayer = false)
        {
            InvariantId = invariantId;
            Before = before;
            After = after;
            RequiresFinalLayer = requiresFinalLayer;
        }

        public LayerOrderInvariantId InvariantId { get; }
        public GenerationLayerId Before { get; }
        public GenerationLayerId After { get; }
        public bool RequiresFinalLayer { get; }
    }

    public sealed class GenerationLayerContract
    {
        private readonly ReadOnlyCollection<LayerResponsibilityId> ownedResponsibilities;
        private readonly ReadOnlyCollection<PacingRole> compatiblePacingRoles;
        private readonly ReadOnlyCollection<AccessClass> compatibleAccessClasses;

        public GenerationLayerContract(
            GenerationLayerId layerId,
            int order,
            IEnumerable<LayerResponsibilityId> responsibilities,
            LayerPacingMode pacingMode,
            LayerAccessMode accessMode,
            IEnumerable<PacingRole> pacingRoles,
            IEnumerable<AccessClass> accessClasses,
            bool claimsPacingAssignmentAuthority,
            bool preservesAccessWhenRemoved,
            bool storesAccessProvenanceOnly,
            string displayId)
        {
            if (responsibilities == null) throw new ArgumentNullException(nameof(responsibilities));
            if (pacingRoles == null) throw new ArgumentNullException(nameof(pacingRoles));
            if (accessClasses == null) throw new ArgumentNullException(nameof(accessClasses));
            LayerId = layerId;
            Order = order;
            ownedResponsibilities = new ReadOnlyCollection<LayerResponsibilityId>(responsibilities.ToArray());
            PacingMode = pacingMode;
            AccessMode = accessMode;
            compatiblePacingRoles = new ReadOnlyCollection<PacingRole>(pacingRoles.ToArray());
            compatibleAccessClasses = new ReadOnlyCollection<AccessClass>(accessClasses.ToArray());
            ClaimsPacingAssignmentAuthority = claimsPacingAssignmentAuthority;
            PreservesAccessWhenRemoved = preservesAccessWhenRemoved;
            StoresAccessProvenanceOnly = storesAccessProvenanceOnly;
            DisplayId = displayId ?? string.Empty;
        }

        public GenerationLayerId LayerId { get; }
        public int Order { get; }
        public IReadOnlyList<LayerResponsibilityId> OwnedResponsibilities => ownedResponsibilities;
        public LayerPacingMode PacingMode { get; }
        public LayerAccessMode AccessMode { get; }
        public IReadOnlyList<PacingRole> CompatiblePacingRoles => compatiblePacingRoles;
        public IReadOnlyList<AccessClass> CompatibleAccessClasses => compatibleAccessClasses;
        public bool ClaimsPacingAssignmentAuthority { get; }
        public bool PreservesAccessWhenRemoved { get; }
        public bool StoresAccessProvenanceOnly { get; }
        public string DisplayId { get; }

        public GenerationLayerContract WithDisplayId(string displayId)
        {
            return new GenerationLayerContract(
                LayerId,
                Order,
                ownedResponsibilities,
                PacingMode,
                AccessMode,
                compatiblePacingRoles,
                compatibleAccessClasses,
                ClaimsPacingAssignmentAuthority,
                PreservesAccessWhenRemoved,
                StoresAccessProvenanceOnly,
                displayId);
        }
    }
}
