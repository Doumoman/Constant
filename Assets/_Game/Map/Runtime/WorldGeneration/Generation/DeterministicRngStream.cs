using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class DeterministicRngStream
    {
        private const ulong Increment = 0x9E3779B97F4A7C15UL;
        private ulong state;

        public DeterministicRngStream(ulong initialState)
        {
            InitialState = initialState;
            state = initialState;
        }

        public ulong InitialState { get; }
        public ulong DrawCount { get; private set; }

        public ulong NextUInt64()
        {
            unchecked
            {
                state += Increment;
                var value = state;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                DrawCount++;
                return value ^ (value >> 31);
            }
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            }

            return NextInt(0, exclusiveMax);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            var width = (ulong)((long)maxExclusive - minInclusive);
            var offset = NextBounded(width);
            return (int)((long)minInclusive + (long)offset);
        }

        public double NextDouble01()
        {
            return (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
        }

        private ulong NextBounded(ulong bound)
        {
            unchecked
            {
                var threshold = (0UL - bound) % bound;
                while (true)
                {
                    var value = NextUInt64();
                    if (value >= threshold)
                    {
                        return value % bound;
                    }
                }
            }
        }
    }
}
