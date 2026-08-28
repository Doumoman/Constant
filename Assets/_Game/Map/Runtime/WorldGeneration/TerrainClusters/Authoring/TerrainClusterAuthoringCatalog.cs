using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.TerrainClusters.Authoring
{
    public sealed class TerrainClusterAuthoringEntry
    {
        private readonly ReadOnlyDictionary<string, AccessClass> portAccess;

        internal TerrainClusterAuthoringEntry(
            TerrainClusterContract contract,
            PacingRole pacingRole,
            MoonpalaceBiomeId biome,
            string footprintVariantId,
            SpineVariantId baselineVariantId,
            TerrainClusterRouteWitnessIntent routeIntent,
            IEnumerable<KeyValuePair<string, AccessClass>> sourcePortAccess,
            string structuralSignature)
        {
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            PacingRole = pacingRole;
            Biome = biome;
            FootprintVariantId = footprintVariantId ?? string.Empty;
            BaselineVariantId = baselineVariantId;
            RouteIntent = routeIntent ?? throw new ArgumentNullException(nameof(routeIntent));
            var access = (sourcePortAccess ?? throw new ArgumentNullException(nameof(sourcePortAccess)))
                .ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal);
            portAccess = new ReadOnlyDictionary<string, AccessClass>(access);
            StructuralSignature = structuralSignature ?? string.Empty;
        }

        public TerrainClusterId Id => Contract.Id;
        public TerrainClusterContract Contract { get; }
        public PacingRole PacingRole { get; }
        public MoonpalaceBiomeId Biome { get; }
        public string FootprintVariantId { get; }
        public SpineVariantId BaselineVariantId { get; }
        public TerrainClusterRouteWitnessIntent RouteIntent { get; }
        public IReadOnlyDictionary<string, AccessClass> PortAccess => portAccess;
        public string StructuralSignature { get; }

        public bool TryGetPortAccess(string portId, out AccessClass accessClass)
        {
            return portAccess.TryGetValue(portId ?? string.Empty, out accessClass);
        }
    }

    public sealed class TerrainClusterAuthoringCatalog
    {
        private readonly ReadOnlyCollection<TerrainClusterAuthoringEntry> entries;
        private readonly ReadOnlyDictionary<TerrainClusterId, TerrainClusterAuthoringEntry> byId;

        internal TerrainClusterAuthoringCatalog(
            IEnumerable<TerrainClusterAuthoringEntry> sourceEntries,
            string canonicalContent)
        {
            var copy = (sourceEntries ?? throw new ArgumentNullException(nameof(sourceEntries)))
                .OrderBy(value => value.Id)
                .ToArray();
            entries = new ReadOnlyCollection<TerrainClusterAuthoringEntry>(copy);
            byId = new ReadOnlyDictionary<TerrainClusterId, TerrainClusterAuthoringEntry>(
                copy.ToDictionary(value => value.Id));
            StableDigest = Sha256(canonicalContent ?? string.Empty);
        }

        public IReadOnlyList<TerrainClusterAuthoringEntry> Entries => entries;
        public IReadOnlyDictionary<TerrainClusterId, TerrainClusterAuthoringEntry> ById => byId;
        public string StableDigest { get; }

        public bool TryGet(TerrainClusterId id, out TerrainClusterAuthoringEntry entry)
        {
            return byId.TryGetValue(id, out entry);
        }

        private static string Sha256(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(new UTF8Encoding(false).GetBytes(value))
                    .Select(item => item.ToString("x2")));
            }
        }
    }
}
