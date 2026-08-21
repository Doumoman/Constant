using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.Tests.WorldGeneration.Domain
{
    public sealed class CoordinateValueTypeTests
    {
        [Test]
        public void WorldTileCoord_StoresRawComponents()
        {
            AssertReadOnlyValueType<WorldTileCoord>();
            var value = new WorldTileCoord(-12, int.MaxValue);
            Assert.That(value.X, Is.EqualTo(-12));
            Assert.That(value.Y, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void WorldTileCoord_ImplementsValueEquality()
        {
            var value = new WorldTileCoord(12, -3);
            var same = new WorldTileCoord(12, -3);
            var differentX = new WorldTileCoord(13, -3);
            var differentY = new WorldTileCoord(12, -2);

            Assert.That(value.Equals(same), Is.True);
            Assert.That(value.Equals((object)same), Is.True);
            Assert.That(value.Equals(differentX), Is.False);
            Assert.That(value.Equals(differentY), Is.False);
            Assert.That(value == same, Is.True);
            Assert.That(value != differentX, Is.True);
            Assert.That(value != differentY, Is.True);
            Assert.That(value.GetHashCode(), Is.EqualTo(same.GetHashCode()));
        }

        [Test]
        public void WorldTileCoord_ToStringIsStable()
        {
            Assert.That(new WorldTileCoord(12, -3).ToString(), Is.EqualTo("WorldTileCoord(12, -3)"));
        }

        [Test]
        public void SectorCoord_StoresRawComponents()
        {
            AssertReadOnlyValueType<SectorCoord>();
            var value = new SectorCoord(int.MinValue, int.MaxValue);
            Assert.That(value.X, Is.EqualTo(int.MinValue));
            Assert.That(value.Y, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void SectorCoord_ImplementsValueEquality()
        {
            var value = new SectorCoord(4, 9);
            var same = new SectorCoord(4, 9);
            var differentX = new SectorCoord(5, 9);
            var differentY = new SectorCoord(4, 10);

            Assert.That(value.Equals(same), Is.True);
            Assert.That(value.Equals((object)same), Is.True);
            Assert.That(value.Equals(differentX), Is.False);
            Assert.That(value.Equals(differentY), Is.False);
            Assert.That(value == same, Is.True);
            Assert.That(value != differentX, Is.True);
            Assert.That(value != differentY, Is.True);
            Assert.That(value.GetHashCode(), Is.EqualTo(same.GetHashCode()));
        }

        [Test]
        public void SectorCoord_ToStringIsStable()
        {
            Assert.That(new SectorCoord(4, 9).ToString(), Is.EqualTo("SectorCoord(4, 9)"));
        }

        [Test]
        public void MicroChunkCoord_StoresRawComponents()
        {
            AssertReadOnlyValueType<MicroChunkCoord>();
            var value = new MicroChunkCoord(-2, int.MaxValue);
            Assert.That(value.X, Is.EqualTo(-2));
            Assert.That(value.Y, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void MicroChunkCoord_ImplementsValueEquality()
        {
            var value = new MicroChunkCoord(2, 1);
            var same = new MicroChunkCoord(2, 1);
            var differentX = new MicroChunkCoord(3, 1);
            var differentY = new MicroChunkCoord(2, 2);

            Assert.That(value.Equals(same), Is.True);
            Assert.That(value.Equals((object)same), Is.True);
            Assert.That(value.Equals(differentX), Is.False);
            Assert.That(value.Equals(differentY), Is.False);
            Assert.That(value == same, Is.True);
            Assert.That(value != differentX, Is.True);
            Assert.That(value != differentY, Is.True);
            Assert.That(value.GetHashCode(), Is.EqualTo(same.GetHashCode()));
        }

        [Test]
        public void MicroChunkCoord_ToStringIsStable()
        {
            Assert.That(new MicroChunkCoord(2, 1).ToString(), Is.EqualTo("MicroChunkCoord(2, 1)"));
        }

        [Test]
        public void LocalTileCoord_StoresRawComponents()
        {
            AssertReadOnlyValueType<LocalTileCoord>();
            var value = new LocalTileCoord(-11, int.MaxValue);
            Assert.That(value.X, Is.EqualTo(-11));
            Assert.That(value.Y, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void LocalTileCoord_ImplementsValueEquality()
        {
            var value = new LocalTileCoord(11, 7);
            var same = new LocalTileCoord(11, 7);
            var differentX = new LocalTileCoord(10, 7);
            var differentY = new LocalTileCoord(11, 6);

            Assert.That(value.Equals(same), Is.True);
            Assert.That(value.Equals((object)same), Is.True);
            Assert.That(value.Equals(differentX), Is.False);
            Assert.That(value.Equals(differentY), Is.False);
            Assert.That(value == same, Is.True);
            Assert.That(value != differentX, Is.True);
            Assert.That(value != differentY, Is.True);
            Assert.That(value.GetHashCode(), Is.EqualTo(same.GetHashCode()));
        }

        [Test]
        public void LocalTileCoord_ToStringIsStable()
        {
            Assert.That(new LocalTileCoord(11, 7).ToString(), Is.EqualTo("LocalTileCoord(11, 7)"));
        }

        private static void AssertReadOnlyValueType<T>()
        {
            Assert.That(typeof(T).IsValueType, Is.True);
            Assert.That(typeof(T).IsDefined(typeof(IsReadOnlyAttribute), false), Is.True);
        }
    }
}
