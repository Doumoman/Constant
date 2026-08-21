using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class WorldGenerationPassRegistry
    {
        private readonly IReadOnlyDictionary<string, IWorldGenerationPass> passes;
        private readonly IReadOnlyList<string> passIds;

        public WorldGenerationPassRegistry(IEnumerable<IWorldGenerationPass> implementations)
        {
            if (implementations == null) throw new ArgumentNullException(nameof(implementations));
            var copy = new SortedDictionary<string, IWorldGenerationPass>(StringComparer.Ordinal);
            foreach (var implementation in implementations)
            {
                if (implementation == null)
                    throw new ArgumentException("Pass implementations cannot be null.", nameof(implementations));
                if (string.IsNullOrEmpty(implementation.PassId))
                    throw new ArgumentException("Pass identifiers must be non-empty.", nameof(implementations));
                if (string.IsNullOrEmpty(implementation.ClassName))
                    throw new ArgumentException("Pass class names must be non-empty.", nameof(implementations));
                if (copy.ContainsKey(implementation.PassId))
                    throw new ArgumentException("Pass identifiers must be unique.", nameof(implementations));
                copy.Add(implementation.PassId, implementation);
            }

            passes = new ReadOnlyDictionary<string, IWorldGenerationPass>(copy);
            passIds = new ReadOnlyCollection<string>(copy.Keys.ToArray());
        }

        public int Count => passes.Count;
        public IReadOnlyList<string> PassIds => passIds;

        public static WorldGenerationPassRegistry CreateProduction()
        {
            return new WorldGenerationPassRegistry(new IWorldGenerationPass[]
            {
                new GridInitializationPassAdapter()
            });
        }

        public bool TryGet(string passId, out IWorldGenerationPass implementation)
        {
            if (string.IsNullOrEmpty(passId))
                throw new ArgumentException("Pass identifier must be non-empty.", nameof(passId));
            return passes.TryGetValue(passId, out implementation);
        }

        public IWorldGenerationPass Get(string passId)
        {
            if (!TryGet(passId, out var implementation))
                throw new KeyNotFoundException("Pass implementation was not found: " + passId);
            return implementation;
        }
    }
}
