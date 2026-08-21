#if LEGACY_DISABLED
using System;

namespace StarNight.Generation.P6
{
    public struct P6DeterministicRandom
    {
        private ulong state;

        public P6DeterministicRandom(int seed)
        {
            state = Mix(unchecked((uint)seed) + 0x9E3779B97F4A7C15UL);
            if (state == 0) state = 0xD1B54A32D192ED03UL;
        }

        public uint NextUInt()
        {
            ulong value = state;
            value ^= value >> 12;
            value ^= value << 25;
            value ^= value >> 27;
            state = value;
            return (uint)((value * 2685821657736338717UL) >> 32);
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            return (int)(NextUInt() % (uint)exclusiveMax);
        }

        public int NextInt(int inclusiveMin, int exclusiveMax)
        {
            if (exclusiveMax <= inclusiveMin)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            return inclusiveMin + NextInt(exclusiveMax - inclusiveMin);
        }

        public bool NextBool() => (NextUInt() & 1u) != 0u;

        public void Shuffle<T>(T[] values)
        {
            for (int index = values.Length - 1; index > 0; index--)
            {
                int other = NextInt(index + 1);
                T temporary = values[index];
                values[index] = values[other];
                values[other] = temporary;
            }
        }

        public static int DeriveSeed(int rootSeed, int attempt)
        {
            ulong value = unchecked((uint)rootSeed);
            value ^= (ulong)(uint)attempt * 0x9E3779B9UL;
            return unchecked((int)Mix(value));
        }

        private static ulong Mix(ulong value)
        {
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}

#endif
