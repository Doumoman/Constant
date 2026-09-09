#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Biomes;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.WorldData;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Rmap16
{
    [Category("RMAP16")]
    public sealed class RmapClusterAssemblyPlannerTests
    {
        [Test]
        public void W06_PublicAssemblyConsumesRmap12Through15AndProtectsEverySpecialCell()
        {
            Rmap16ClusterAssemblyPlan plan = Plan(1304);

            Assert.That(plan.Success, Is.True);
            Assert.That(plan.Cells.Count, Is.EqualTo(624 * 416));
            Assert.That(plan.Chunks.Count, Is.EqualTo(52 * 52));
            Assert.That(plan.SpecialPlan.Cells.Count, Is.EqualTo(2432));
            Assert.That(plan.TerrainReservationGateCalls, Is.GreaterThan(2704));
            Assert.That(plan.RejectedProtectedWrites, Is.GreaterThan(0));
            Assert.That(plan.Cells.Select(value => value.BaseCell).Distinct(), Is.EquivalentTo(new[]
            {
                RmapPatternBaseCell.Air, RmapPatternBaseCell.Solid, RmapPatternBaseCell.OneWayPlatform,
            }));
            foreach (RmapSpecialWorldCell special in plan.SpecialPlan.Cells)
            {
                Rmap16TerrainCell actual = plan.GetCell(special.World.X, special.World.Y);
                Assert.That(actual.BaseCell, Is.EqualTo(special.BaseCell), special.World.ToString());
                Assert.That(actual.SourceKind, Is.EqualTo(Rmap16TerrainSourceKind.SpecialReservation));
            }
            Assert.That(plan.Clusters.Count, Is.EqualTo(4));
            Assert.That(plan.Clusters.All(value => value.MicroChunkCount >= 2 && value.MicroChunkCount <= 8), Is.True);
            Assert.That(plan.Clusters.Select(value => value.Shape), Is.EquivalentTo(new[] { "SLOPE", "CAVE", "CORRIDOR", "HALF_PIPE" }));
            Assert.That(plan.Patterns.Count, Is.EqualTo(plan.Clusters.Sum(value => value.MicroChunkCount) * 6));
            Assert.That(plan.Patterns.All(value => value.PoolIndex >= 0 && !string.IsNullOrWhiteSpace(value.CandidateId)), Is.True);
            Assert.That(plan.Ports.Count, Is.EqualTo(45));
            Assert.That(plan.Routes.Count, Is.EqualTo(plan.Graph.Edges.Count));
            Assert.That(plan.Routes.All(value => value.Cells.Count > 0 && value.StaticTraversalContract.Contains("PLAYER_0.4x0.8")), Is.True);
            Assert.That(plan.Secrets.Single().Chunks.Count, Is.InRange(1, 6));
            Assert.That(plan.Secrets.Single().Clues.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(plan.StateGeometry.Count(value => value.OwnerId == "RMAP15_SITE_SEALBOSS" && value.State == "SEALED" &&
                value.BaseCell == RmapPatternBaseCell.Solid), Is.EqualTo(3));
            Assert.That(plan.StateGeometry.Count(value => value.OwnerId == "RMAP15_SITE_SEALBOSS" && value.State == "OPEN" &&
                value.BaseCell == RmapPatternBaseCell.Air), Is.EqualTo(3));
            Assert.That(plan.Chunks.Count(value => value.State == Rmap16ChunkState.InactiveSolid), Is.GreaterThan(0));
        }

        [Test]
        public void A26_DensityUsesActualSOverAllThreeBaseStatesInEveryRmap14Patch()
        {
            Rmap16ClusterAssemblyPlan plan = Plan(1304);

            Assert.That(plan.Densities.Count, Is.EqualTo(16));
            Assert.That(plan.Densities.All(value => value.Total > 0 && value.IsWithinTarget), Is.True,
                string.Join(";", plan.Densities.Select(value => value.PatchId + ":" + value.DensityPermille)));
            Assert.That(plan.Densities.Select(value => value.Profile), Does.Contain(RmapBiomeDensityProfileId.Open));
            Assert.That(plan.Densities.Select(value => value.Profile), Does.Contain(RmapBiomeDensityProfileId.Balanced));
            Assert.That(plan.Densities.Select(value => value.Profile), Does.Contain(RmapBiomeDensityProfileId.Dense));
            foreach (Rmap16DensityMeasurement density in plan.Densities)
                Assert.That(density.DensityPermille, Is.InRange(density.MinimumPermille, density.MaximumPermille), density.PatchId);
        }

        [Test]
        public void DeterminismAndFullWorldExportsIncludeTerrainMapAndEnlargedReviews()
        {
            Rmap16ClusterAssemblyPlan first = Plan(1304);
            Rmap16ClusterAssemblyPlan second = Plan(1304);
            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            Assert.That(second.Patterns.Select(value => value.CandidateId), Is.EqualTo(first.Patterns.Select(value => value.CandidateId)));

            string directory = GeneratedDirectory();
            string review = Path.Combine(directory, "review");
            Directory.CreateDirectory(review);
            Write(Path.Combine(directory, "rmap16_manifest.json"), RmapClusterAssemblyExport.ManifestJson(first));
            Write(Path.Combine(directory, "rmap16_clusters.csv"), RmapClusterAssemblyExport.ClustersCsv(first));
            Write(Path.Combine(directory, "rmap16_chunks.csv"), RmapClusterAssemblyExport.ChunksCsv(first));
            Write(Path.Combine(directory, "rmap16_cells.csv"), RmapClusterAssemblyExport.CellsCsv(first));
            Write(Path.Combine(directory, "rmap16_patterns.csv"), RmapClusterAssemblyExport.PatternsCsv(first));
            Write(Path.Combine(directory, "rmap16_overlays.csv"), RmapClusterAssemblyExport.OverlaysCsv(first));
            Write(Path.Combine(directory, "rmap16_ports.csv"), RmapClusterAssemblyExport.PortsCsv(first));
            Write(Path.Combine(directory, "rmap16_routes.csv"), RmapClusterAssemblyExport.RoutesCsv(first));
            Write(Path.Combine(directory, "rmap16_secrets.csv"), RmapClusterAssemblyExport.SecretsCsv(first));
            Write(Path.Combine(directory, "rmap16_state_geometry.csv"), RmapClusterAssemblyExport.StateGeometryCsv(first));
            Write(Path.Combine(directory, "rmap16_density.csv"), RmapClusterAssemblyExport.DensityCsv(first));
            Write(Path.Combine(directory, "rmap16_shape_summary.csv"), RmapClusterAssemblyExport.ShapeSummaryCsv(first));
            File.WriteAllBytes(Path.Combine(directory, "rmap16_layout.png"), FullMapPng(first, false));
            File.WriteAllBytes(Path.Combine(review, "rmap16_terrain_overview.png"), FullMapPng(first, false));
            File.WriteAllBytes(Path.Combine(review, "rmap16_route_overlay.png"), FullMapPng(first, true));
            foreach (Rmap16Cluster cluster in first.Clusters)
                File.WriteAllBytes(Path.Combine(review, cluster.Id.ToLowerInvariant() + ".png"), ClusterPng(first, cluster));
            File.WriteAllBytes(Path.Combine(review, "rmap16_secret_enlarged.png"), SecretPng(first));

            Assert.That(new FileInfo(Path.Combine(directory, "rmap16_cells.csv")).Length, Is.GreaterThan(10 * 1024 * 1024));
            Assert.That(Directory.GetFiles(review, "*.png").Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(File.ReadAllBytes(Path.Combine(directory, "rmap16_layout.png")).Take(8).ToArray(),
                Is.EqualTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                Assert.That(new FileInfo(file).Length, Is.GreaterThan(0), file);
            TestContext.Out.WriteLine("RMAP16_EXPORT_BEGIN");
            TestContext.Out.WriteLine("PLAN_DIGEST=" + first.Digest);
            TestContext.Out.WriteLine("OUTPUT_DIRECTORY=" + directory);
            TestContext.Out.WriteLine("RMAP16_EXPORT_END");
        }

        private static Rmap16ClusterAssemblyPlan Plan(ulong seed)
        {
            WorldGenerationRngStreams streams = RngStreams();
            RmapWorldDefinition definition = RmapWorldDataGenerator.Generate(new RmapWorldDataRequest(seed, "CONTENT_V1", "GENERATOR_V1"), streams);
            return RmapClusterAssemblyPlanner.Plan(definition, streams);
        }

        private static string GeneratedDirectory()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, "MapDesign", "MCP", "GENERATED", "RMAP16");
        }

        private static void Write(string path, string content) => File.WriteAllText(path, content, new UTF8Encoding(false));

        private static byte[] FullMapPng(Rmap16ClusterAssemblyPlan plan, bool showRoute)
        {
            const int width = 624; const int height = 416;
            Color32[] pixels = plan.Cells.Select(value => ColorFor(value.BaseCell)).ToArray();
            if (showRoute)
            {
                foreach (Rmap16Route route in plan.Routes) foreach (RmapSpecialWorldPoint point in route.Cells)
                    pixels[(point.Y * width) + point.X] = new Color32(245, 122, 90, 255);
                foreach (Rmap16Port port in plan.Ports) pixels[(port.Y * width) + port.X] = new Color32(90, 224, 190, 255);
            }
            foreach (Rmap16Overlay overlay in plan.Overlays.Where(value => value.Kind == "BREAKABLE_ACCESS" || value.Kind == "SECRET_CLUE"))
                pixels[(overlay.Y * width) + overlay.X] = overlay.Kind == "SECRET_CLUE" ? new Color32(250, 224, 112, 255) : new Color32(226, 110, 164, 255);
            return Encode(width, height, pixels);
        }

        private static byte[] ClusterPng(Rmap16ClusterAssemblyPlan plan, Rmap16Cluster cluster)
        {
            const int scale = 8;
            int minX = cluster.MicroX * 12; int minY = cluster.MicroY * 8;
            int width = cluster.WidthMicroChunks * 12; int height = cluster.HeightMicroChunks * 8;
            var pixels = new Color32[width * scale * height * scale];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                Fill(pixels, width * scale, x * scale, y * scale, scale, scale, ColorFor(plan.GetCell(minX + x, minY + y).BaseCell));
            foreach (Rmap16Overlay overlay in plan.Overlays.Where(value => value.OwnerId == cluster.Id))
                Fill(pixels, width * scale, (overlay.X - minX) * scale, (overlay.Y - minY) * scale, scale, scale, new Color32(86, 218, 239, 255));
            return Encode(width * scale, height * scale, pixels);
        }

        private static byte[] SecretPng(Rmap16ClusterAssemblyPlan plan)
        {
            Rmap16Secret secret = plan.Secrets.Single();
            int minX = secret.Chunks.Min(value => value.X) * 12; int minY = secret.Chunks.Min(value => value.Y) * 8;
            int width = (secret.Chunks.Max(value => value.X) - secret.Chunks.Min(value => value.X) + 1) * 12;
            int height = (secret.Chunks.Max(value => value.Y) - secret.Chunks.Min(value => value.Y) + 1) * 8;
            const int scale = 12; var pixels = new Color32[width * scale * height * scale];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                Fill(pixels, width * scale, x * scale, y * scale, scale, scale, ColorFor(plan.GetCell(minX + x, minY + y).BaseCell));
            foreach (RmapSpecialWorldPoint clue in secret.Clues)
                Fill(pixels, width * scale, (clue.X - minX) * scale, (clue.Y - minY) * scale, scale, scale, new Color32(250, 224, 112, 255));
            Fill(pixels, width * scale, (secret.BreakableAccess.X - minX) * scale, (secret.BreakableAccess.Y - minY) * scale, scale, scale, new Color32(226, 110, 164, 255));
            return Encode(width * scale, height * scale, pixels);
        }

        private static Color32 ColorFor(RmapPatternBaseCell value)
        {
            switch (value)
            {
                case RmapPatternBaseCell.Air: return new Color32(95, 157, 182, 255);
                case RmapPatternBaseCell.Solid: return new Color32(67, 61, 81, 255);
                default: return new Color32(202, 126, 84, 255);
            }
        }

        private static byte[] Encode(int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels); texture.Apply(false, false);
            byte[] bytes = ImageConversion.EncodeToPNG(texture);
            UnityEngine.Object.DestroyImmediate(texture); return bytes;
        }

        private static void Fill(Color32[] pixels, int width, int x, int y, int fillWidth, int fillHeight, Color32 color)
        {
            int height = pixels.Length / width;
            for (int row = Math.Max(0, y); row < Math.Min(height, y + fillHeight); row++)
            for (int column = Math.Max(0, x); column < Math.Min(width, x + fillWidth); column++) pixels[(row * width) + column] = color;
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
            SetAutoProperty(definition, "RngStreamId", id); SetAutoProperty(definition, "SaltHex", CreateHex(salt));
            SetAutoProperty(definition, "ResetScope", scope); SetAutoProperty(definition, "DescriptionKo", "RMAP16 focused fixture");
            SetAutoProperty(definition, "Active", true); return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            byte[] bytes = Enumerable.Range(0, value.Length / 2).Select(index => byte.Parse(value.Substring(index * 2, 2),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null); return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string name, object value)
        {
            var field = target.GetType().GetField("<" + name + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name); field.SetValue(target, value);
        }
    }
}
#endif
