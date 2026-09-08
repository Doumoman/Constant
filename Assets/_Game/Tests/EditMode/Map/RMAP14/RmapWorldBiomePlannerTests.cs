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
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.WorldData;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Rmap14
{
    [Category("RMAP14")]
    public sealed class RmapWorldBiomePlannerTests
    {
        [Test]
        public void W05_Full52By52OwnershipHasFourBiomesNonEmptyPatchesAndExistingBoundarySources()
        {
            RmapWorldBiomePlan plan = Plan(1304);

            Assert.That(plan.Success, Is.True);
            Assert.That(plan.Cells.Count, Is.EqualTo(2704));
            Assert.That(plan.Cells.Select(cell => cell.Index).Distinct().Count(), Is.EqualTo(2704));
            Assert.That(plan.Patches.Count, Is.EqualTo(16));
            Assert.That(plan.Patches.All(patch => patch.AreaMicroChunks == 169), Is.True);
            Assert.That(plan.Patches.GroupBy(patch => patch.Biome.CanonicalId).Count(), Is.EqualTo(4));
            Assert.That(plan.Patches.GroupBy(patch => patch.Biome.CanonicalId)
                .All(group => group.Count() == 4), Is.True);
            Assert.That(plan.Cells.All(cell => cell.OwnershipState == RmapBiomeOwnershipState.Active), Is.True,
                "RMAP14 records planning ownership without inventing RMAP15 special footprints.");
            Assert.That(plan.Cells.All(cell => cell.X * 12 >= 0 && cell.X * 12 + 11 < 624 &&
                                              cell.Y * 8 >= 0 && cell.Y * 8 + 7 < 416), Is.True);

            Assert.That(plan.Boundaries.Count, Is.GreaterThan(0));
            Assert.That(plan.Boundaries.Select(boundary => boundary.Pair.PairId).Distinct().Count(), Is.EqualTo(6));
            foreach (RmapWorldBiomeBoundary boundary in plan.Boundaries)
            {
                Assert.That(boundary.Source.PatchId, Is.Not.EqualTo(boundary.Target.PatchId));
                Assert.That(boundary.SourceContract.CandidateIds.Count, Is.GreaterThan(0));
                Assert.That(boundary.SourceContract.ProfileIds.Count, Is.GreaterThan(0));
                Assert.That(boundary.SourceContract.PairDefinition.MandatoryToolRequirement,
                    Is.EqualTo(MoonpalaceBiomePairDefinition.NoToolRequirement));
                Assert.That(boundary.SourceContract.PairDefinition.MandatoryRouteAllowed, Is.True);
                Assert.That(boundary.EdgeSignatureId, Is.Not.Empty);
            }
        }

        [Test]
        public void A25_ProfileSelectionUsesRmap12BiomeStreamWeightsAndDefersMeasuredDensity()
        {
            RmapWorldDefinition definition = Definition(1305);
            RmapWorldBiomePlan first = RmapWorldBiomePlanner.Plan(definition, RngStreams());
            RmapWorldBiomePlan second = RmapWorldBiomePlanner.Plan(Definition(1305), RngStreams());

            Assert.That(first.Digest, Is.EqualTo(second.Digest));
            Assert.That(definition.RngBindings[RmapWorldRngStream.Biome].SourceStreamId,
                Is.EqualTo(WorldGenerationRngStreams.BiomePatchStreamId));
            Assert.That(RmapWorldBiomePlanner.ProfileCatalog[RmapBiomeDensityProfileId.Open].TargetMinimumPermille,
                Is.EqualTo(400));
            Assert.That(RmapWorldBiomePlanner.ProfileCatalog[RmapBiomeDensityProfileId.Open].TargetMaximumPermille,
                Is.EqualTo(550));
            Assert.That(RmapWorldBiomePlanner.ProfileCatalog[RmapBiomeDensityProfileId.Balanced].TargetMinimumPermille,
                Is.EqualTo(550));
            Assert.That(RmapWorldBiomePlanner.ProfileCatalog[RmapBiomeDensityProfileId.Balanced].TargetMaximumPermille,
                Is.EqualTo(650));
            Assert.That(RmapWorldBiomePlanner.ProfileCatalog[RmapBiomeDensityProfileId.Dense].TargetMinimumPermille,
                Is.EqualTo(650));
            Assert.That(RmapWorldBiomePlanner.ProfileCatalog[RmapBiomeDensityProfileId.Dense].TargetMaximumPermille,
                Is.EqualTo(750));
            Assert.That(first.Patches.All(patch => patch.OpenWeight > 0 && patch.BalancedWeight > 0 &&
                                                   patch.DenseWeight > 0), Is.True);
            Assert.That(first.Patches.All(patch => patch.DensityMetricStatus == "PENDING_GEOMETRY"), Is.True);
            Assert.That(first.Patches.All(patch => patch.DensityMetricScopeId.StartsWith("RMAP14_PATCH_SCOPE|",
                StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void Rmap13ReservationsRemainInputsForRmap15WithoutFixedFootprints()
        {
            RmapWorldBiomePlan plan = Plan(1306);

            Assert.That(plan.Graph.Success, Is.True);
            Assert.That(plan.ReservationInputs.Count, Is.EqualTo(plan.Graph.Reservations.Count));
            Assert.That(plan.ReservationInputs.Count, Is.EqualTo(8));
            foreach (RmapWorldBiomeReservationInput input in plan.ReservationInputs)
            {
                Assert.That(input.PlacementStatus, Is.EqualTo("NOT_PLACED_RMAP15"));
                Assert.That(input.BoundsStatus, Is.EqualTo("PATCH_SET_ONLY"));
                Assert.That(input.CandidatePatches.Count, Is.EqualTo(4));
                Assert.That(input.CandidatePatches.All(patch => patch.Biome == input.AllowedBiome), Is.True);
                Assert.That(input.CandidateBounds.Split('|').Length, Is.EqualTo(4));
                Assert.That(plan.Graph.Reservations.Any(reservation => reservation.ReservationId ==
                    input.Reservation.ReservationId), Is.True);
            }
        }

        [Test]
        public void ExportsAndUnityVisiblePreviewsAreDerivedFromOnePassingPlan()
        {
            RmapWorldBiomePlan plan = Plan(1304);
            string manifest = RmapWorldBiomeExport.ManifestJson(plan);
            string ownership = RmapWorldBiomeExport.PatchOwnershipCsv(plan);
            string patches = RmapWorldBiomeExport.PatchesCsv(plan);
            string boundaries = RmapWorldBiomeExport.BoundariesCsv(plan);
            string profiles = RmapWorldBiomeExport.ProfilesCsv(plan);
            string reservations = RmapWorldBiomeExport.ReservationInputsCsv(plan);

            Assert.That(manifest, Does.Contain("\"ownership_count\": 2704"));
            Assert.That(manifest, Does.Contain("\"measured_density\": \"PENDING_GEOMETRY\""));
            Assert.That(ownership.Split('\n').Length, Is.EqualTo(2706));
            Assert.That(patches.Split('\n').Length, Is.EqualTo(18));
            Assert.That(boundaries.Split('\n')[0], Does.StartWith("boundary_id,"));
            Assert.That(boundaries, Does.Contain("MoonpalaceCraterRootBoundaryAuthoringContract"));
            Assert.That(profiles.Split('\n').Length, Is.EqualTo(18));
            Assert.That(reservations.Split('\n').Length, Is.EqualTo(10));
            Assert.That(ownership, Does.Contain("\"Active\""));
            Assert.That(reservations, Does.Contain("\"NOT_PLACED_RMAP15\""));

            string directory = GeneratedDirectory();
            Directory.CreateDirectory(directory);
            WriteText(Path.Combine(directory, "rmap14_manifest.json"), manifest);
            WriteText(Path.Combine(directory, "rmap14_patch_ownership.csv"), ownership);
            WriteText(Path.Combine(directory, "rmap14_patches.csv"), patches);
            WriteText(Path.Combine(directory, "rmap14_boundaries.csv"), boundaries);
            WriteText(Path.Combine(directory, "rmap14_profiles.csv"), profiles);
            WriteText(Path.Combine(directory, "rmap14_reservation_inputs.csv"), reservations);
            File.WriteAllBytes(Path.Combine(directory, "rmap14_biome_layout.png"), PreviewPng(plan, false));
            File.WriteAllBytes(Path.Combine(directory, "rmap14_density_profiles.png"), PreviewPng(plan, true));

            foreach (string file in Directory.GetFiles(directory).OrderBy(value => value, StringComparer.Ordinal))
                Assert.That(new FileInfo(file).Length, Is.GreaterThan(0), file);
            Assert.That(File.ReadAllBytes(Path.Combine(directory, "rmap14_biome_layout.png")).Take(8).ToArray(),
                Is.EqualTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
            TestContext.Out.WriteLine("RMAP14_EXPORT_BEGIN");
            TestContext.Out.WriteLine("PLAN_DIGEST=" + plan.Digest);
            TestContext.Out.WriteLine("OUTPUT_DIRECTORY=" + directory);
            TestContext.Out.WriteLine("MANIFEST_BASE64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(manifest)));
            TestContext.Out.WriteLine("OWNERSHIP_BASE64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ownership)));
            TestContext.Out.WriteLine("PATCHES_BASE64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(patches)));
            TestContext.Out.WriteLine("BOUNDARIES_BASE64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(boundaries)));
            TestContext.Out.WriteLine("PROFILES_BASE64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(profiles)));
            TestContext.Out.WriteLine("RESERVATIONS_BASE64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(reservations)));
            TestContext.Out.WriteLine("RMAP14_EXPORT_END");
        }

        private static RmapWorldBiomePlan Plan(ulong seed) => RmapWorldBiomePlanner.Plan(Definition(seed), RngStreams());

        private static RmapWorldDefinition Definition(ulong seed) => RmapWorldDataGenerator.Generate(
            new RmapWorldDataRequest(seed, "CONTENT_V1", "GENERATOR_V1"), RngStreams());

        private static string GeneratedDirectory()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, "MapDesign", "MCP", "GENERATED", "RMAP14");
        }

        private static void WriteText(string path, string value)
        {
            File.WriteAllText(path, value, new UTF8Encoding(false));
        }

        private static byte[] PreviewPng(RmapWorldBiomePlan plan, bool density)
        {
            const int width = 624;
            const int headerHeight = 64;
            const int gridHeight = 416;
            var pixels = Enumerable.Repeat(new Color32(24, 26, 32, 255), width * (headerHeight + gridHeight)).ToArray();
            foreach (RmapWorldBiomeCell cell in plan.Cells)
            {
                Color32 color = density ? DensityColor(cell.Patch.DensityProfile) : BiomeColor(cell.Biome);
                for (var y = headerHeight + cell.Y * 8; y < headerHeight + (cell.Y + 1) * 8; y++)
                for (var x = cell.X * 12; x < (cell.X + 1) * 12; x++)
                    pixels[(y * width) + x] = color;
            }
            foreach (RmapWorldBiomeBoundary boundary in plan.Boundaries)
            {
                if (boundary.Direction == RmapWorldGraphDirection.Right)
                {
                    int x = (boundary.Source.X + 1) * 12 - 1;
                    for (int y = headerHeight + boundary.Source.Y * 8; y < headerHeight + (boundary.Source.Y + 1) * 8; y++)
                        pixels[(y * width) + x] = new Color32(15, 15, 18, 255);
                }
                else
                {
                    int y = headerHeight + (boundary.Source.Y + 1) * 8 - 1;
                    for (int x = boundary.Source.X * 12; x < (boundary.Source.X + 1) * 12; x++)
                        pixels[(y * width) + x] = new Color32(15, 15, 18, 255);
                }
            }
            DrawText(pixels, width, 8, 8, "SEED1304 CV1 GV1", new Color32(235, 238, 244, 255), 2);
            Color32[] legend = density
                ? new[] { DensityColor(RmapBiomeDensityProfileId.Open), DensityColor(RmapBiomeDensityProfileId.Balanced), DensityColor(RmapBiomeDensityProfileId.Dense) }
                : MoonpalaceBiomePairCatalog.Canonical.Biomes.Select(BiomeColor).ToArray();
            for (var index = 0; index < legend.Length; index++)
                Fill(pixels, width, 8 + (index * 28), 34, 20, 14, legend[index]);
            var texture = new Texture2D(width, headerHeight + gridHeight, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] bytes = ImageConversion.EncodeToPNG(texture);
            UnityEngine.Object.DestroyImmediate(texture);
            return bytes;
        }

        private static void DrawText(Color32[] pixels, int width, int x, int y, string value, Color32 color, int scale)
        {
            foreach (char glyph in value ?? string.Empty)
            {
                if (glyph == ' ')
                {
                    x += 4 * scale;
                    continue;
                }
                string[] rows = Glyph(glyph);
                for (var row = 0; row < rows.Length; row++)
                for (var column = 0; column < rows[row].Length; column++)
                    if (rows[row][column] == '1') Fill(pixels, width, x + column * scale,
                        y + row * scale, scale, scale, color);
                x += 6 * scale;
            }
        }

        private static string[] Glyph(char value)
        {
            switch (value)
            {
                case '0': return new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" };
                case '1': return new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" };
                case '3': return new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" };
                case '4': return new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" };
                case 'C': return new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" };
                case 'D': return new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" };
                case 'E': return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" };
                case 'G': return new[] { "01111", "10000", "10000", "10111", "10001", "10001", "01111" };
                case 'S': return new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" };
                case 'V': return new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" };
                default: return new[] { "11111", "10001", "00110", "00100", "01100", "10001", "11111" };
            }
        }

        private static void Fill(Color32[] pixels, int width, int x, int y, int fillWidth, int fillHeight, Color32 color)
        {
            int height = pixels.Length / width;
            for (int row = Math.Max(0, y); row < Math.Min(height, y + fillHeight); row++)
            for (int column = Math.Max(0, x); column < Math.Min(width, x + fillWidth); column++)
                pixels[(row * width) + column] = color;
        }

        private static Color32 BiomeColor(MoonpalaceBiomeId biome)
        {
            switch (biome.CanonicalId)
            {
                case "MoonCrater": return new Color32(92, 110, 151, 255);
                case "CassiaRoot": return new Color32(108, 142, 84, 255);
                case "AbandonedMill": return new Color32(155, 112, 80, 255);
                case "MoonDough": return new Color32(210, 168, 89, 255);
                default: throw new ArgumentOutOfRangeException(nameof(biome));
            }
        }

        private static Color32 DensityColor(RmapBiomeDensityProfileId profile)
        {
            switch (profile)
            {
                case RmapBiomeDensityProfileId.Open: return new Color32(80, 175, 138, 255);
                case RmapBiomeDensityProfileId.Balanced: return new Color32(222, 184, 84, 255);
                case RmapBiomeDensityProfileId.Dense: return new Color32(175, 82, 92, 255);
                default: throw new ArgumentOutOfRangeException(nameof(profile));
            }
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
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(
                typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new WorldGenerationRngStreams(set);
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(
                typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "RMAP14 focused fixture");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2).Select(index => byte.Parse(
                value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance |
                BindingFlags.NonPublic, null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string name, object value)
        {
            var field = target.GetType().GetField("<" + name + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
#endif
