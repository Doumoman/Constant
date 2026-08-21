using System.Threading;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class StaticDataRegistryStore
    {
        private readonly object publishGate = new object();
        private PublishedStaticDataSnapshot current;

        public PublishedStaticDataSnapshot Current => Volatile.Read(ref current);

        internal object PublishGate => publishGate;

        internal PublishedStaticDataSnapshot Exchange(PublishedStaticDataSnapshot snapshot)
        {
            return Interlocked.Exchange(ref current, snapshot);
        }
    }
}
