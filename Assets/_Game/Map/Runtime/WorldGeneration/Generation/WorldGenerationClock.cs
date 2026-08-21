using System;
using System.Diagnostics;

namespace StarNight.Map.WorldGeneration.Generation
{
    public interface IWorldGenerationClock
    {
        DateTimeOffset GetUtcNow();
        long GetTimestamp();
        TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp);
    }

    public sealed class SystemWorldGenerationClock : IWorldGenerationClock
    {
        public static readonly SystemWorldGenerationClock Instance = new SystemWorldGenerationClock();

        private SystemWorldGenerationClock()
        {
        }

        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }

        public long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        public TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
        {
            if (endTimestamp < startTimestamp)
                throw new InvalidOperationException("The monotonic end timestamp precedes the start timestamp.");

            var elapsedTicks = (long)(
                (decimal)(endTimestamp - startTimestamp) * TimeSpan.TicksPerSecond /
                Stopwatch.Frequency);
            return TimeSpan.FromTicks(elapsedTicks);
        }
    }
}
