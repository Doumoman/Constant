using System.Reflection;
using NUnit.Framework;
using StarNight.MapAuthoring.Editor.WorldGeneration;
using StarNight.MapAuthoring.Editor.WorldGeneration.Preview;
using UnityEditor;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Preview
{
    public sealed class WorldCoordinateDebugDisplayTests
    {
        [Test]
        public void Format_OriginShowsAllCoordinateSpaces()
        {
            Assert.That(
                WorldCoordinateDebugDisplay.Format(0f, 0f),
                Is.EqualTo(
                    "World: WorldTileCoord(0, 0)\n" +
                    "Sector: SectorCoord(0, 0)\n" +
                    "MicroChunk: MicroChunkCoord(0, 0)\n" +
                    "Local: LocalTileCoord(0, 0)"));
        }

        [Test]
        public void Format_FractionalPositionUsesFloor()
        {
            Assert.That(
                WorldCoordinateDebugDisplay.Format(61.99f, 42.01f),
                Is.EqualTo(
                    "World: WorldTileCoord(61, 42)\n" +
                    "Sector: SectorCoord(1, 1)\n" +
                    "MicroChunk: MicroChunkCoord(1, 1)\n" +
                    "Local: LocalTileCoord(1, 2)"));
        }

        [Test]
        public void Format_MidWorldShowsExpectedComponents()
        {
            Assert.That(
                WorldCoordinateDebugDisplay.Format(300.5f, 200.5f),
                Is.EqualTo(
                    "World: WorldTileCoord(300, 200)\n" +
                    "Sector: SectorCoord(6, 6)\n" +
                    "MicroChunk: MicroChunkCoord(1, 1)\n" +
                    "Local: LocalTileCoord(0, 0)"));
        }

        [Test]
        public void Format_LastWorldTileShowsExpectedComponents()
        {
            Assert.That(
                WorldCoordinateDebugDisplay.Format(623.999f, 415.999f),
                Is.EqualTo(
                    "World: WorldTileCoord(623, 415)\n" +
                    "Sector: SectorCoord(12, 12)\n" +
                    "MicroChunk: MicroChunkCoord(3, 3)\n" +
                    "Local: LocalTileCoord(11, 7)"));
        }

        [Test]
        public void Format_OutsideEdgesShowCandidateWithoutClamping()
        {
            Assert.That(
                WorldCoordinateDebugDisplay.Format(-0.01f, 0f),
                Is.EqualTo("World: OUTSIDE (-1, 0)\nSector: -\nMicroChunk: -\nLocal: -"));
            Assert.That(
                WorldCoordinateDebugDisplay.Format(624f, 0f),
                Is.EqualTo("World: OUTSIDE (624, 0)\nSector: -\nMicroChunk: -\nLocal: -"));
            Assert.That(
                WorldCoordinateDebugDisplay.Format(0f, 416f),
                Is.EqualTo("World: OUTSIDE (0, 416)\nSector: -\nMicroChunk: -\nLocal: -"));
        }

        [Test]
        public void Format_NonFiniteInputShowsUnavailable()
        {
            const string expected =
                "World: UNAVAILABLE\n" +
                "Sector: -\n" +
                "MicroChunk: -\n" +
                "Local: -";

            Assert.That(WorldCoordinateDebugDisplay.Format(float.NaN, 0f), Is.EqualTo(expected));
            Assert.That(WorldCoordinateDebugDisplay.Format(float.PositiveInfinity, 0f), Is.EqualTo(expected));
            Assert.That(WorldCoordinateDebugDisplay.Format(float.NegativeInfinity, 0f), Is.EqualTo(expected));
            Assert.That(WorldCoordinateDebugDisplay.Format(0f, float.NaN), Is.EqualTo(expected));
            Assert.That(WorldCoordinateDebugDisplay.Format(0f, float.PositiveInfinity), Is.EqualTo(expected));
            Assert.That(WorldCoordinateDebugDisplay.Format(0f, float.NegativeInfinity), Is.EqualTo(expected));
        }

        [Test]
        public void Window_HasLockedMenuPathAndEditorWindowType()
        {
            var windowType = typeof(WorldCoordinateDebugWindow);
            Assert.That(windowType.IsSealed, Is.True);
            Assert.That(typeof(EditorWindow).IsAssignableFrom(windowType), Is.True);

            var openMethod = windowType.GetMethod(
                "Open",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            Assert.That(openMethod, Is.Not.Null);

            CustomAttributeData menuAttribute = null;
            foreach (var attribute in openMethod.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(MenuItem))
                {
                    menuAttribute = attribute;
                    break;
                }
            }

            Assert.That(menuAttribute, Is.Not.Null);
            Assert.That(menuAttribute.ConstructorArguments, Is.Not.Empty);
            Assert.That(
                menuAttribute.ConstructorArguments[0].Value,
                Is.EqualTo("WorldGen/Coordinates"));
        }
    }
}
