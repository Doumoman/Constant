using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Pipeline
{
    public static class GenerationLayerCatalog
    {
        private static readonly ReadOnlyCollection<GenerationLayerContract> entries =
            new ReadOnlyCollection<GenerationLayerContract>(CreateEntries());

        private static readonly ReadOnlyCollection<GenerationLayerOrderInvariant> orderInvariants =
            new ReadOnlyCollection<GenerationLayerOrderInvariant>(CreateOrderInvariants());

        static GenerationLayerCatalog()
        {
            var validation = GenerationLayerCatalogValidator.Validate(
                entries,
                orderInvariants,
                PacingRoleTokenCodec.Entries,
                AccessClassTokenCodec.Entries,
                AccessClass.MandatoryNoTool);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    "The built-in generation layer catalog is invalid: " +
                    string.Join("; ", validation.Errors.Select(value => value.ToString())));
            }
        }

        public static IReadOnlyList<GenerationLayerContract> Entries => entries;
        public static IReadOnlyList<GenerationLayerOrderInvariant> OrderInvariants => orderInvariants;
        public static string StableDigest => ComputeStableDigest(entries);

        public static string ComputeStableDigest(IEnumerable<GenerationLayerContract> contracts)
        {
            return ComputeStableDigest(
                contracts,
                orderInvariants,
                PacingRoleTokenCodec.Entries,
                AccessClassTokenCodec.Entries);
        }

        public static string ComputeStableDigest(
            IEnumerable<GenerationLayerContract> contracts,
            IEnumerable<GenerationLayerOrderInvariant> invariants,
            IEnumerable<PacingRoleToken> pacingTokens,
            IEnumerable<AccessClassToken> accessTokens)
        {
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));
            if (invariants == null) throw new ArgumentNullException(nameof(invariants));
            if (pacingTokens == null) throw new ArgumentNullException(nameof(pacingTokens));
            if (accessTokens == null) throw new ArgumentNullException(nameof(accessTokens));

            var records = new List<string>();
            records.AddRange(contracts
                .Select(value => value ?? throw new ArgumentException(
                    "Layer contracts cannot contain null.", nameof(contracts)))
                .OrderBy(value => value.Order)
                .ThenBy(value => (int)value.LayerId)
                .Select(CanonicalLayerRecord));
            records.AddRange(invariants
                .Select(value => value ?? throw new ArgumentException(
                    "Order invariants cannot contain null.", nameof(invariants)))
                .OrderBy(value => (int)value.InvariantId)
                .Select(CanonicalInvariantRecord));
            records.AddRange(pacingTokens
                .Select(value => value ?? throw new ArgumentException(
                    "Pacing token entries cannot contain null.", nameof(pacingTokens)))
                .OrderBy(value => (int)value.Role)
                .Select(value => "P|" + ((int)value.Role).ToString(CultureInfo.InvariantCulture) +
                                 "|" + value.Role + "|" + value.Token));
            records.AddRange(accessTokens
                .Select(value => value ?? throw new ArgumentException(
                    "Access token entries cannot contain null.", nameof(accessTokens)))
                .OrderBy(value => (int)value.AccessClass)
                .Select(value => "A|" + ((int)value.AccessClass).ToString(CultureInfo.InvariantCulture) +
                                 "|" + value.AccessClass + "|" + value.Token));

            var bytes = Encoding.UTF8.GetBytes(string.Join("\n", records));
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }

        private static string CanonicalLayerRecord(GenerationLayerContract value)
        {
            return string.Join("|", new[]
            {
                "L",
                value.Order.ToString(CultureInfo.InvariantCulture),
                ((int)value.LayerId).ToString(CultureInfo.InvariantCulture),
                value.LayerId.ToString(),
                JoinEnumValues(value.OwnedResponsibilities),
                value.PacingMode.ToString(),
                value.AccessMode.ToString(),
                JoinEnumValues(value.CompatiblePacingRoles),
                JoinEnumValues(value.CompatibleAccessClasses),
                value.ClaimsPacingAssignmentAuthority ? "PACING_ASSIGN" : "NO_PACING_ASSIGN",
                value.PreservesAccessWhenRemoved ? "REMOVE_SAFE_ACCESS" : "NOT_REMOVABLE",
                value.StoresAccessProvenanceOnly ? "ACCESS_PROVENANCE_ONLY" : "NO_ACCESS_PROVENANCE",
            });
        }

        private static string CanonicalInvariantRecord(GenerationLayerOrderInvariant value)
        {
            return string.Join("|", new[]
            {
                "I",
                ((int)value.InvariantId).ToString(CultureInfo.InvariantCulture),
                value.InvariantId.ToString(),
                value.Before.ToString(),
                value.After.ToString(),
                value.RequiresFinalLayer ? "FINAL" : "BEFORE",
            });
        }

        private static string JoinEnumValues<T>(IEnumerable<T> values)
        {
            return string.Join(",", values
                .Select(value => new
                {
                    Numeric = Convert.ToInt32(value, CultureInfo.InvariantCulture),
                    Name = value.ToString(),
                })
                .OrderBy(value => value.Numeric)
                .ThenBy(value => value.Name, StringComparer.Ordinal)
                .Select(value => value.Numeric.ToString(CultureInfo.InvariantCulture) + ":" + value.Name));
        }

        private static GenerationLayerContract[] CreateEntries()
        {
            var allPacing = PacingRoleTokenCodec.Entries.Select(value => value.Role).ToArray();
            var generalAccess = new[]
            {
                AccessClass.MandatoryNoTool,
                AccessClass.OptionalNoTool,
                AccessClass.OptionalTool,
                AccessClass.OptionalEnvironment,
                AccessClass.OptionalExplosive,
                AccessClass.OptionalHidden,
            };
            var allAccess = AccessClassTokenCodec.Entries.Select(value => value.AccessClass).ToArray();

            return new[]
            {
                Layer(
                    GenerationLayerId.RouteType,
                    new[]
                    {
                        LayerResponsibilityId.SectorExternalConnectivity,
                        LayerResponsibilityId.GeneralRouteAccess,
                    },
                    LayerPacingMode.PreserveOnly,
                    LayerAccessMode.GeneralAuthority,
                    allPacing,
                    generalAccess,
                    false,
                    false,
                    "LAYER_ROUTE_TYPE"),
                Layer(
                    GenerationLayerId.SpecialRegion,
                    new[]
                    {
                        LayerResponsibilityId.WorldReservedLandmark,
                        LayerResponsibilityId.SpecialEntryAccess,
                    },
                    LayerPacingMode.CompatibilityOnly,
                    LayerAccessMode.SpecialEntryAuthority,
                    allPacing,
                    allAccess,
                    false,
                    false,
                    "LAYER_SPECIAL_REGION"),
                Layer(
                    GenerationLayerId.TerrainCluster,
                    new[] { LayerResponsibilityId.StaticTerrainTraversal },
                    LayerPacingMode.CompatibilityOnly,
                    LayerAccessMode.CompatibilityOnly,
                    allPacing,
                    generalAccess,
                    false,
                    false,
                    "LAYER_TERRAIN_CLUSTER"),
                Layer(
                    GenerationLayerId.MicroPattern,
                    new[] { LayerResponsibilityId.LocalPatternTileOperation },
                    LayerPacingMode.CompatibilityOnly,
                    LayerAccessMode.CompatibilityOnly,
                    allPacing,
                    generalAccess,
                    false,
                    false,
                    "LAYER_MICRO_PATTERN"),
                Layer(
                    GenerationLayerId.ActivityStructure,
                    new[] { LayerResponsibilityId.StrongGameplayIncident },
                    LayerPacingMode.CompatibilityOnly,
                    LayerAccessMode.CompatibilityOnly,
                    allPacing,
                    generalAccess,
                    true,
                    false,
                    "LAYER_ACTIVITY_STRUCTURE"),
                Layer(
                    GenerationLayerId.EventOverlay,
                    new[] { LayerResponsibilityId.MarkerOnlyRunVariation },
                    LayerPacingMode.CompatibilityOnly,
                    LayerAccessMode.PreserveOnly,
                    allPacing,
                    allAccess,
                    true,
                    false,
                    "LAYER_EVENT_OVERLAY"),
                Layer(
                    GenerationLayerId.MicroChunk,
                    new[] { LayerResponsibilityId.SliceStorageAndBoundaryProjection },
                    LayerPacingMode.PreserveOnly,
                    LayerAccessMode.PreserveOnly,
                    allPacing,
                    allAccess,
                    false,
                    true,
                    "LAYER_MICRO_CHUNK"),
            };
        }

        private static GenerationLayerContract Layer(
            GenerationLayerId layerId,
            IEnumerable<LayerResponsibilityId> responsibilities,
            LayerPacingMode pacingMode,
            LayerAccessMode accessMode,
            IEnumerable<PacingRole> pacingRoles,
            IEnumerable<AccessClass> accessClasses,
            bool preservesAccessWhenRemoved,
            bool storesAccessProvenanceOnly,
            string displayId)
        {
            return new GenerationLayerContract(
                layerId,
                (int)layerId,
                responsibilities,
                pacingMode,
                accessMode,
                pacingRoles,
                accessClasses,
                false,
                preservesAccessWhenRemoved,
                storesAccessProvenanceOnly,
                displayId);
        }

        private static GenerationLayerOrderInvariant[] CreateOrderInvariants()
        {
            return new[]
            {
                Invariant(LayerOrderInvariantId.SpecialRegionBeforeTerrainCluster,
                    GenerationLayerId.SpecialRegion, GenerationLayerId.TerrainCluster),
                Invariant(LayerOrderInvariantId.TerrainClusterBeforeMicroPattern,
                    GenerationLayerId.TerrainCluster, GenerationLayerId.MicroPattern),
                Invariant(LayerOrderInvariantId.MicroPatternBeforeActivityStructure,
                    GenerationLayerId.MicroPattern, GenerationLayerId.ActivityStructure),
                Invariant(LayerOrderInvariantId.MicroPatternBeforeEventOverlay,
                    GenerationLayerId.MicroPattern, GenerationLayerId.EventOverlay),
                Invariant(LayerOrderInvariantId.ActivityStructureBeforeMicroChunk,
                    GenerationLayerId.ActivityStructure, GenerationLayerId.MicroChunk),
                Invariant(LayerOrderInvariantId.EventOverlayBeforeMicroChunk,
                    GenerationLayerId.EventOverlay, GenerationLayerId.MicroChunk),
                new GenerationLayerOrderInvariant(
                    LayerOrderInvariantId.MicroChunkFinal,
                    GenerationLayerId.MicroChunk,
                    GenerationLayerId.MicroChunk,
                    true),
            };
        }

        private static GenerationLayerOrderInvariant Invariant(
            LayerOrderInvariantId invariantId,
            GenerationLayerId before,
            GenerationLayerId after)
        {
            return new GenerationLayerOrderInvariant(invariantId, before, after);
        }
    }
}
