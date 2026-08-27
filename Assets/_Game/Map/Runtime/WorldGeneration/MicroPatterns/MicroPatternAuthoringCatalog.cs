using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public sealed class MicroPatternAuthoringCatalog
    {
        private readonly ReadOnlyCollection<MicroPatternDefinition> definitions;
        private readonly IReadOnlyDictionary<MicroPatternId, MicroPatternDefinition> byId;

        internal MicroPatternAuthoringCatalog(IEnumerable<MicroPatternDefinition> source)
        {
            var ordered = (source ?? throw new ArgumentNullException(nameof(source)))
                .OrderBy(value => value.Id)
                .ToArray();
            if (ordered.Length == 0)
            {
                throw new ArgumentException("A published authoring catalog cannot be empty.", nameof(source));
            }

            definitions = new ReadOnlyCollection<MicroPatternDefinition>(ordered);
            var dictionary = new SortedDictionary<MicroPatternId, MicroPatternDefinition>();
            foreach (var definition in ordered)
            {
                dictionary.Add(definition.Id, definition);
            }

            byId = new ReadOnlyDictionary<MicroPatternId, MicroPatternDefinition>(dictionary);
            StableDigest = ComputeDigest(ordered);
        }

        public IReadOnlyList<MicroPatternDefinition> Definitions => definitions;
        public IReadOnlyDictionary<MicroPatternId, MicroPatternDefinition> DefinitionsById => byId;
        public int Count => definitions.Count;
        public string StableDigest { get; }

        public bool TryGetDefinition(MicroPatternId id, out MicroPatternDefinition definition)
        {
            return byId.TryGetValue(id, out definition);
        }

        private static string ComputeDigest(IEnumerable<MicroPatternDefinition> source)
        {
            var material = new StringBuilder();
            foreach (var definition in source.OrderBy(value => value.Id))
            {
                Append(material, definition.Id.Value);
                Append(material, definition.ComputeStableDigest());
            }

            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(Encoding.UTF8.GetBytes(material.ToString()))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void Append(StringBuilder target, string value)
        {
            target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            target.Append(':');
            target.Append(value);
            target.Append('\n');
        }
    }
}
