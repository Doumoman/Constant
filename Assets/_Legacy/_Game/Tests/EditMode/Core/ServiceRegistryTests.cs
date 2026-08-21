#if LEGACY_DISABLED
using System;
using NUnit.Framework;
using StarNight.Core.Flow;

namespace StarNight.Core.Tests
{
    public sealed class ServiceRegistryTests
    {
        [Test]
        public void Register_RejectsDuplicateServiceType()
        {
            using var registry = new ServiceRegistry();
            registry.Register(new TestService());

            Assert.Throws<InvalidOperationException>(() => registry.Register(new TestService()));
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetRequired_ReturnsRegisteredInstance()
        {
            using var registry = new ServiceRegistry();
            var expected = new TestService();
            registry.Register(expected);

            Assert.That(registry.GetRequired<TestService>(), Is.SameAs(expected));
        }

        private sealed class TestService
        {
        }
    }
}

#endif
