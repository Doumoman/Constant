using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class PublishedStaticDataSnapshot
    {
        internal PublishedStaticDataSnapshot(
            StaticDataRegistry registry,
            ContentVersionHash contentHash,
            long version)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            ContentHash = contentHash ?? throw new ArgumentNullException(nameof(contentHash));
            if (version < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            Version = version;
        }

        public StaticDataRegistry Registry { get; }
        public ContentVersionHash ContentHash { get; }
        public long Version { get; }
    }
}
