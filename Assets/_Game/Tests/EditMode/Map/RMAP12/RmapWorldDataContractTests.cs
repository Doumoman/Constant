#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.Tests.EditMode.Rmap12
{
    [Category("RMAP12")]
    public sealed class RmapWorldDataContractTests
    {
        [Test]
        public void W08_W09_SameRequestIsDeterministicAndSevenStreamsAreIsolated()
        {
            var request = new RmapWorldDataRequest(1107, "CONTENT_V1", "GENERATOR_V1");
            RmapWorldDefinition first = RmapWorldDataGenerator.Generate(request);
            RmapWorldDefinition second = RmapWorldDataGenerator.Generate(request);
            Assert.That(first.Digest, Is.EqualTo(second.Digest));
            Assert.That(first.StreamSeeds.Count, Is.EqualTo(7));
            Assert.That(first.StreamSeeds.Values.Distinct().Count(), Is.EqualTo(7));
            Assert.That(first.PoolVersion, Does.StartWith("RMAP11_POOL500_V2_FIX25"));
        }

        [Test]
        public void W10_W11_MutationRoundTripDoesNotChangeDefinitionAndAllKindsHaveStableIds()
        {
            RmapWorldDefinition definition = RmapWorldDataGenerator.Generate(new RmapWorldDataRequest(42, "C", "G"));
            var mutation = new RmapWorldMutationState();
            foreach (RmapWorldStableId id in definition.StableIds) mutation.Apply(id, "changed");
            RmapWorldMutationState restored = RmapWorldMutationState.Deserialize(mutation.Serialize());
            Assert.That(restored.Serialize(), Is.EqualTo(mutation.Serialize()));
            Assert.That(definition.StableIds.Select(id => id.Kind).Distinct().Count(), Is.EqualTo(7));
            Assert.That(definition.StableIds.Select(id => id.Value).Distinct().Count(), Is.EqualTo(7));
            Assert.That(RmapWorldDataGenerator.Generate(new RmapWorldDataRequest(42, "C", "G")).Digest, Is.EqualTo(definition.Digest));
        }
    }
}
#endif
