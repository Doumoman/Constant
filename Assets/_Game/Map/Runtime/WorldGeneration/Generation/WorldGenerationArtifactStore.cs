using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class WorldGenerationArtifactStore
    {
        private readonly IReadOnlyDictionary<string, object> artifacts;
        private readonly IReadOnlyList<string> artifactIds;

        public WorldGenerationArtifactStore()
            : this(Array.Empty<KeyValuePair<string, object>>())
        {
        }

        public WorldGenerationArtifactStore(IEnumerable<KeyValuePair<string, object>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var copy = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var pair in source)
            {
                ValidateId(pair.Key, nameof(source));
                if (pair.Value == null) throw new ArgumentException("Artifact values cannot be null.", nameof(source));
                if (copy.ContainsKey(pair.Key))
                    throw new ArgumentException("Artifact identifiers must be unique.", nameof(source));
                copy.Add(pair.Key, pair.Value);
            }

            artifacts = new ReadOnlyDictionary<string, object>(copy);
            artifactIds = new ReadOnlyCollection<string>(copy.Keys.ToArray());
        }

        public int Count => artifacts.Count;
        public IReadOnlyList<string> ArtifactIds => artifactIds;

        public bool Contains(string artifactId)
        {
            ValidateId(artifactId, nameof(artifactId));
            return artifacts.ContainsKey(artifactId);
        }

        public object Get(string artifactId)
        {
            ValidateId(artifactId, nameof(artifactId));
            if (!artifacts.TryGetValue(artifactId, out var value))
                throw new KeyNotFoundException("Artifact was not found: " + artifactId);
            return value;
        }

        public T Get<T>(string artifactId)
        {
            var value = Get(artifactId);
            if (!(value is T typed))
                throw new InvalidCastException("Artifact has a different runtime type: " + artifactId);
            return typed;
        }

        public bool TryGet(string artifactId, out object value)
        {
            ValidateId(artifactId, nameof(artifactId));
            return artifacts.TryGetValue(artifactId, out value);
        }

        public bool TryGet<T>(string artifactId, out T value)
        {
            if (TryGet(artifactId, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default(T);
            return false;
        }

        internal WorldGenerationArtifactStore Select(IEnumerable<string> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            var selected = new List<KeyValuePair<string, object>>();
            foreach (var id in ids.OrderBy(value => value, StringComparer.Ordinal))
                selected.Add(new KeyValuePair<string, object>(id, Get(id)));
            return new WorldGenerationArtifactStore(selected);
        }

        internal WorldGenerationArtifactStore Commit(IReadOnlyDictionary<string, object> outputs)
        {
            if (outputs == null) throw new ArgumentNullException(nameof(outputs));
            var combined = artifacts.Select(pair => pair).ToList();
            foreach (var pair in outputs.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                if (artifacts.ContainsKey(pair.Key))
                    throw new InvalidOperationException("Artifact already has an owner: " + pair.Key);
                combined.Add(pair);
            }
            return new WorldGenerationArtifactStore(combined);
        }

        private static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Artifact identifier must be non-empty.", parameterName);
        }
    }
}
