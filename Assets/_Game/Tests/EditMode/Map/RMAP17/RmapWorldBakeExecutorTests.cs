#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.Tests.EditMode.Rmap17
{
    [TestFixture]
    [Category("RMAP17")]
    public sealed class RmapWorldBakeExecutorTests
    {
        [Test]
        public void A04_ExactRmap16SnapshotMapsEveryBaseStateAndPreservesProtectedCells()
        {
            Rmap16ClusterAssemblyPlan source = SourcePlan();
            Rmap16BakeSnapshot snapshot = RmapClusterAssemblyPlanner.CreateBakeSnapshot(source);
            Rmap17WorldBakePlan bake = RmapWorldBakeExecutor.Build(source, snapshot);

            Assert.That(ReferenceEquals(bake.SourceCells, snapshot.Cells), Is.True);
            Assert.That(bake.SourcePlanDigest, Is.EqualTo(source.Digest));
            Assert.That(bake.SourceCellDigest, Is.EqualTo(source.CellDigest));
            Assert.That(bake.SourceCellDigest, Is.EqualTo("3a40b473d497178d19e11d746d4655cc9a0856c10c0a03e981341ca7b1bda733"));
            Assert.That(bake.SourceCellCount, Is.EqualTo(624 * 416));
            Assert.That(bake.SolidCellCount + bake.AirCellCount + bake.OneWayCellCount,
                Is.EqualTo(bake.SourceCellCount));
            Assert.That(bake.IsExactStaticWorld, Is.True);
            Assert.That(source.SpecialPlan.Cells.All(value =>
                bake.GetCell(value.World.X, value.World.Y).BaseCell == value.BaseCell), Is.True);
        }

        [Test]
        public void E08_StartSupportAndOverlayLayersAreConcreteAndInBounds()
        {
            Rmap17WorldBakePlan bake = Bake();
            Rmap16TerrainCell start = bake.GetCell(bake.PlayerStart.X, bake.PlayerStart.Y);
            Rmap16TerrainCell support = bake.GetCell(bake.PlayerStart.X, bake.PlayerStart.Y - 1);

            Assert.That(start.BaseCell, Is.EqualTo(RmapPatternBaseCell.Air));
            Assert.That(support.BaseCell, Is.EqualTo(RmapPatternBaseCell.Solid));
            Assert.That(bake.LadderOverlayCount, Is.GreaterThan(0));
            Assert.That(bake.GrabOverlayCount, Is.EqualTo(2));
            Assert.That(bake.MarkerOverlayCount, Is.EqualTo(3));
            Assert.That(bake.LadderOverlays.Concat(bake.GrabOverlays).Concat(bake.MarkerOverlays)
                .All(value => value.X >= 0 && value.X < 624 && value.Y >= 0 && value.Y < 416), Is.True);
            Assert.That(bake.LadderOverlays.Select(value => value.Id).Distinct().Count(),
                Is.EqualTo(bake.LadderOverlayCount));
        }

        [Test]
        public void Determinism_UsesTheSameFullBakeDigestWithoutASecondTerrainPath()
        {
            Rmap17WorldBakePlan first = Bake();
            Rmap17WorldBakePlan second = Bake();

            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            Assert.That(second.SourceCellDigest, Is.EqualTo(first.SourceCellDigest));
            Assert.That(second.SourceCells.Select(value => value.BaseCell),
                Is.EqualTo(first.SourceCells.Select(value => value.BaseCell)));
            TestContext.Out.WriteLine("RMAP17_BAKE_DIGEST=" + first.Digest);
            TestContext.Out.WriteLine("RMAP17_SOURCE_CELL_DIGEST=" + first.SourceCellDigest);
            TestContext.Out.WriteLine("RMAP17_COUNTS=" + first.SolidCellCount + "," + first.AirCellCount + "," +
                first.OneWayCellCount + "," + first.LadderOverlayCount);
        }

        private static Rmap17WorldBakePlan Bake()
        {
            Rmap16ClusterAssemblyPlan source = SourcePlan();
            return RmapWorldBakeExecutor.Build(source, RmapClusterAssemblyPlanner.CreateBakeSnapshot(source));
        }

        private static Rmap16ClusterAssemblyPlan SourcePlan()
        {
            WorldGenerationRngStreams streams = RngStreams();
            RmapWorldDefinition definition = RmapWorldDataGenerator.Generate(
                new RmapWorldDataRequest(1304, "CONTENT_V1", "GENERATOR_V1"), streams);
            return RmapClusterAssemblyPlanner.Plan(definition, streams);
        }

        private static WorldGenerationRngStreams RngStreams()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                { "RNG_WORLD_SITE", Rng("RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD") },
                { "RNG_BIOME_PATCH", Rng("RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS") },
                { "RNG_ROUTE", Rng("RNG_ROUTE", "C00FEE12AB341901", "PASS") },
                { "RNG_TYPE0", Rng("RNG_TYPE0", "D15EA5E007A4C883", "PASS") },
                { "RNG_SECTOR_RECIPE", Rng("RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR") },
                { "RNG_POPULATION", Rng("RNG_POPULATION", "F123456789ABCDEF", "SPAWN") },
            };
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new WorldGenerationRngStreams(set);
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "RMAP17 focused verified input");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            byte[] bytes = Enumerable.Range(0, value.Length / 2).Select(index => byte.Parse(
                value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            ConstructorInfo constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField("<" + name + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
#endif
