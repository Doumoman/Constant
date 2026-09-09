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
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Rmap15
{
    [Category("RMAP15")]
    public sealed class RmapSpecialReservationPlannerTests
    {
        [Test]
        public void W04_EightPhysicalSitesBindEveryGraphReservationAndKeepVillageSeparate()
        {
            RmapSpecialReservationPlan plan = Plan(1304);

            Assert.That(plan.Success, Is.True);
            Assert.That(plan.Sites.Count, Is.EqualTo(8));
            Assert.That(plan.Sites.Select(value => value.Role), Is.EquivalentTo(Enum.GetValues(
                typeof(RmapSpecialPhysicalRole)).Cast<RmapSpecialPhysicalRole>()));
            Assert.That(plan.GraphBindings.Count, Is.EqualTo(8));
            Assert.That(plan.GraphBindings.Select(value => value.ReservationId).Distinct().Count(), Is.EqualTo(8));
            Assert.That(plan.GraphBindings.Select(value => value.Role), Is.EquivalentTo(Enum.GetValues(
                typeof(RmapWorldGraphRole)).Cast<RmapWorldGraphRole>()));

            RmapSpecialGraphBinding seal = plan.GraphBindings.Single(value => value.Role == RmapWorldGraphRole.Seal);
            RmapSpecialGraphBinding boss = plan.GraphBindings.Single(value => value.Role == RmapWorldGraphRole.Boss);
            Assert.That(seal.SiteId, Is.EqualTo(boss.SiteId));
            Assert.That(seal.LocalPoint, Is.Not.EqualTo(boss.LocalPoint));
            Assert.That(plan.Sites.Single(value => value.Role == RmapSpecialPhysicalRole.Village).Id,
                Is.Not.EqualTo(seal.SiteId));
            Assert.That(plan.GraphBindings.Any(value => value.SiteId == "RMAP15_SITE_VILLAGE"), Is.False);
            Assert.That(plan.BiomePlan.Graph.Success, Is.True);
            Assert.That(plan.BiomePlan.Graph.Proofs.Count, Is.EqualTo(6));
        }

        [Test]
        public void FixedShellsPublishEveryCellTypedOwnershipPortsAndSupportedSlots()
        {
            RmapSpecialReservationPlan plan = Plan(1304);

            Assert.That(plan.Cells.Count, Is.EqualTo(plan.Sites.Sum(value => value.WidthTiles * value.HeightTiles)));
            Assert.That(plan.Cells.Select(value => value.World).Distinct().Count(), Is.EqualTo(plan.Cells.Count));
            Assert.That(plan.Cells.All(value => value.World.X >= 0 && value.World.X < 624 &&
                                               value.World.Y >= 0 && value.World.Y < 416), Is.True);
            Assert.That(plan.Cells.Select(value => value.BaseCell), Does.Contain(RmapPatternBaseCell.Air));
            Assert.That(plan.Cells.Select(value => value.BaseCell), Does.Contain(RmapPatternBaseCell.Solid));
            Assert.That(plan.Cells.Select(value => value.BaseCell), Does.Contain(RmapPatternBaseCell.OneWayPlatform));
            Assert.That(plan.Cells.All(value => value.Protection == RmapSpecialProtectionKind.FixedSolid ||
                                               value.Protection == RmapSpecialProtectionKind.ProtectedAir), Is.True);
            Assert.That(plan.Sites.All(value => value.EligiblePatchIds.Count >= 4 &&
                                               value.OccupiedPatchIds.Count >= 1 && value.Attempts.Count >= 1), Is.True);
            Assert.That(plan.Accesses.All(value => value.OpenCells.Count == 3 && value.Required), Is.True);
            Assert.That(plan.Accesses.All(value => !string.IsNullOrWhiteSpace(value.SourceNodeId)), Is.True);
            Assert.That(plan.Accesses.SelectMany(value => value.OpenCells).All(point =>
                plan.TryGetCell(point, out RmapSpecialWorldCell cell) && cell.BaseCell == RmapPatternBaseCell.Air), Is.True);
            Assert.That(plan.Slots.Select(value => value.StableId.Value).Distinct().Count(), Is.EqualTo(plan.Slots.Count));
            Assert.That(plan.Slots.All(value =>
                plan.TryGetCell(new RmapSpecialWorldPoint(value.World.X, value.World.Y - 1), out RmapSpecialWorldCell support) &&
                (support.BaseCell == RmapPatternBaseCell.Solid || support.BaseCell == RmapPatternBaseCell.OneWayPlatform) &&
                plan.TryGetCell(new RmapSpecialWorldPoint(value.World.X, value.World.Y + 1), out RmapSpecialWorldCell headroom) &&
                headroom.BaseCell == RmapPatternBaseCell.Air), Is.True);
            Assert.That(plan.StateGeometry.Count, Is.EqualTo(6));
            Assert.That(plan.StateGeometry.Count(value => value.State == "SEALED" && value.BaseCell == RmapPatternBaseCell.Solid),
                Is.EqualTo(3));
            Assert.That(plan.StateGeometry.Count(value => value.State == "OPEN" && value.BaseCell == RmapPatternBaseCell.Air),
                Is.EqualTo(3));
        }

        [Test]
        public void ReservationConsumerRejectsFixedAndProtectedAirButAllowsUnownedTerrain()
        {
            RmapSpecialReservationPlan plan = Plan(1304);
            RmapSpecialWorldCell fixedSolid = plan.Cells.First(value => value.Protection == RmapSpecialProtectionKind.FixedSolid);
            RmapSpecialWorldCell protectedAir = plan.Cells.First(value => value.Protection == RmapSpecialProtectionKind.ProtectedAir);

            RmapSpecialTerrainReservationDecision solidResult = plan.EvaluateTerrainCells(new[] { fixedSolid.World });
            RmapSpecialTerrainReservationDecision airResult = plan.EvaluateTerrainCells(new[] { protectedAir.World });
            RmapSpecialWorldPoint unowned = Enumerable.Range(0, 624).SelectMany(x => Enumerable.Range(0, 416)
                .Select(y => new RmapSpecialWorldPoint(x, y))).First(point => !plan.TryGetCell(point, out _));
            RmapSpecialTerrainReservationDecision allowed = plan.EvaluateTerrainCells(new[] { unowned });

            Assert.That(solidResult.Kind, Is.EqualTo(RmapSpecialTerrainReservationDecisionKind.RejectedProtectedCell));
            Assert.That(airResult.Kind, Is.EqualTo(RmapSpecialTerrainReservationDecisionKind.RejectedProtectedCell));
            Assert.That(solidResult.ProtectedCells.Single().SiteId, Is.EqualTo(fixedSolid.SiteId));
            Assert.That(airResult.ProtectedCells.Single().Protection, Is.EqualTo(RmapSpecialProtectionKind.ProtectedAir));
            Assert.That(allowed.IsAllowed, Is.True);
            Assert.That(allowed.ProtectedCells, Is.Empty);
        }

        [Test]
        public void DeterminismAndExportsUseOnePlanWithVisualCellReview()
        {
            RmapSpecialReservationPlan first = Plan(1304);
            RmapSpecialReservationPlan second = Plan(1304);
            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            Assert.That(second.Sites.Select(value => value.StableId.Value), Is.EqualTo(first.Sites.Select(value => value.StableId.Value)));
            Assert.That(second.Slots.Select(value => value.StableId.Value), Is.EqualTo(first.Slots.Select(value => value.StableId.Value)));
            Assert.That(first.SpecialReservationInitialState, Is.Not.EqualTo(first.BiomePlan.ProfileRngInitialState));

            string directory = GeneratedDirectory();
            string reviewDirectory = Path.Combine(directory, "review");
            Directory.CreateDirectory(reviewDirectory);
            WriteText(Path.Combine(directory, "rmap15_manifest.json"), RmapSpecialReservationExport.ManifestJson(first));
            WriteText(Path.Combine(directory, "rmap15_sites.csv"), RmapSpecialReservationExport.SitesCsv(first));
            WriteText(Path.Combine(directory, "rmap15_graph_bindings.csv"), RmapSpecialReservationExport.GraphBindingsCsv(first));
            WriteText(Path.Combine(directory, "rmap15_fixed_cells.csv"), RmapSpecialReservationExport.FixedCellsCsv(first));
            WriteText(Path.Combine(directory, "rmap15_access.csv"), RmapSpecialReservationExport.AccessCsv(first));
            WriteText(Path.Combine(directory, "rmap15_slots.csv"), RmapSpecialReservationExport.SlotsCsv(first));
            WriteText(Path.Combine(directory, "rmap15_ownership.csv"), RmapSpecialReservationExport.OwnershipCsv(first));
            WriteText(Path.Combine(directory, "rmap15_state_geometry.csv"), RmapSpecialReservationExport.StateGeometryCsv(first));
            File.WriteAllBytes(Path.Combine(directory, "rmap15_layout.png"), LayoutPng(first));
            foreach (RmapSpecialSite site in first.Sites)
                File.WriteAllBytes(Path.Combine(reviewDirectory, site.Id.ToLowerInvariant() + ".png"), RegionPng(first, site));

            Assert.That(RmapSpecialReservationExport.ManifestJson(first), Does.Contain("\"site_count\": 8"));
            Assert.That(RmapSpecialReservationExport.FixedCellsCsv(first).Split('\n').Length,
                Is.EqualTo(first.Cells.Count + 2));
            Assert.That(RmapSpecialReservationExport.GraphBindingsCsv(first).Split('\n').Length, Is.EqualTo(10));
            Assert.That(RmapSpecialReservationExport.StateGeometryCsv(first), Does.Contain("\"SEALED\""));
            Assert.That(Directory.GetFiles(reviewDirectory, "*.png").Length, Is.EqualTo(8));
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                Assert.That(new FileInfo(file).Length, Is.GreaterThan(0), file);
            Assert.That(File.ReadAllBytes(Path.Combine(directory, "rmap15_layout.png")).Take(8).ToArray(),
                Is.EqualTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
            TestContext.Out.WriteLine("RMAP15_EXPORT_BEGIN");
            TestContext.Out.WriteLine("PLAN_DIGEST=" + first.Digest);
            TestContext.Out.WriteLine("OUTPUT_DIRECTORY=" + directory);
            TestContext.Out.WriteLine("RMAP15_EXPORT_END");
        }

        private static RmapSpecialReservationPlan Plan(ulong seed)
        {
            WorldGenerationRngStreams streams = RngStreams();
            RmapWorldDefinition definition = RmapWorldDataGenerator.Generate(
                new RmapWorldDataRequest(seed, "CONTENT_V1", "GENERATOR_V1"), streams);
            return RmapSpecialReservationPlanner.Plan(RmapWorldBiomePlanner.Plan(definition, streams), streams);
        }

        private static string GeneratedDirectory()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, "MapDesign", "MCP", "GENERATED", "RMAP15");
        }

        private static void WriteText(string path, string value) => File.WriteAllText(path, value, new UTF8Encoding(false));

        private static byte[] LayoutPng(RmapSpecialReservationPlan plan)
        {
            const int width = 624;
            const int height = 416;
            Color32[] pixels = Enumerable.Repeat(new Color32(25, 28, 34, 255), width * height).ToArray();
            foreach (RmapSpecialWorldCell cell in plan.Cells)
                pixels[(cell.World.Y * width) + cell.World.X] = CellColor(cell.BaseCell);
            foreach (RmapSpecialAccess access in plan.Accesses)
            foreach (RmapSpecialWorldPoint point in access.OpenCells)
                Fill(pixels, width, point.X - 1, point.Y - 1, 3, 3, new Color32(88, 214, 173, 255));
            foreach (RmapSpecialSlot slot in plan.Slots)
                Fill(pixels, width, slot.World.X - 1, slot.World.Y - 1, 3, 3, new Color32(250, 224, 112, 255));
            foreach (RmapSpecialSite site in plan.Sites)
                DrawText(pixels, width, site.Origin.X, Math.Min(height - 8, site.Origin.Y + site.HeightTiles),
                    RoleText(site.Role), new Color32(244, 244, 248, 255), 1);
            return Encode(width, height, pixels);
        }

        private static byte[] RegionPng(RmapSpecialReservationPlan plan, RmapSpecialSite site)
        {
            const int scale = 16;
            const int headerHeight = 32;
            int width = site.WidthTiles * scale;
            int height = headerHeight + (site.HeightTiles * scale);
            Color32[] pixels = Enumerable.Repeat(new Color32(12, 14, 20, 255), width * height).ToArray();
            foreach (RmapSpecialWorldCell cell in plan.Cells.Where(value => value.SiteId == site.Id))
                Fill(pixels, width, cell.LocalX * scale, headerHeight + cell.LocalY * scale, scale, scale, CellColor(cell.BaseCell));
            foreach (RmapSpecialAccess access in plan.Accesses.Where(value => value.SiteId == site.Id))
            foreach (RmapSpecialWorldPoint world in access.OpenCells)
                Fill(pixels, width, (world.X - site.Origin.X) * scale, headerHeight + (world.Y - site.Origin.Y) * scale,
                    scale, scale, new Color32(88, 214, 173, 255));
            foreach (RmapSpecialSlot slot in plan.Slots.Where(value => value.SiteId == site.Id))
                Fill(pixels, width, (slot.World.X - site.Origin.X) * scale + 3,
                    headerHeight + (slot.World.Y - site.Origin.Y) * scale + 3,
                    scale - 6, scale - 6, new Color32(250, 224, 112, 255));
            DrawText(pixels, width, 2, 2, RoleText(site.Role), new Color32(244, 244, 248, 255), 2);
            DrawText(pixels, width, 2, 19, "X" + site.Origin.X + "Y" + site.Origin.Y,
                new Color32(160, 214, 236, 255), 1);
            return Encode(width, height, pixels);
        }

        private static byte[] Encode(int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] bytes = ImageConversion.EncodeToPNG(texture);
            UnityEngine.Object.DestroyImmediate(texture);
            return bytes;
        }

        private static Color32 CellColor(RmapPatternBaseCell value)
        {
            switch (value)
            {
                case RmapPatternBaseCell.Air: return new Color32(91, 153, 175, 255);
                case RmapPatternBaseCell.Solid: return new Color32(72, 66, 83, 255);
                case RmapPatternBaseCell.OneWayPlatform: return new Color32(196, 112, 86, 255);
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static void Fill(Color32[] pixels, int width, int x, int y, int fillWidth, int fillHeight, Color32 color)
        {
            int height = pixels.Length / width;
            for (int row = Math.Max(0, y); row < Math.Min(height, y + fillHeight); row++)
            for (int column = Math.Max(0, x); column < Math.Min(width, x + fillWidth); column++)
                pixels[(row * width) + column] = color;
        }

        private static string RoleText(RmapSpecialPhysicalRole role)
        {
            switch (role)
            {
                case RmapSpecialPhysicalRole.Start: return "START";
                case RmapSpecialPhysicalRole.MooncoreOre: return "ORE";
                case RmapSpecialPhysicalRole.CondensedCoefficientSap: return "SAP";
                case RmapSpecialPhysicalRole.DeepStarYeast: return "YEAST";
                case RmapSpecialPhysicalRole.Village: return "VILL";
                case RmapSpecialPhysicalRole.Forge: return "FORGE";
                case RmapSpecialPhysicalRole.SealBoss: return "SEALBOSS";
                case RmapSpecialPhysicalRole.Exit: return "EXIT";
                default: throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        private static void DrawText(Color32[] pixels, int width, int x, int y, string value, Color32 color, int scale)
        {
            foreach (char glyph in value ?? string.Empty)
            {
                string[] rows = Glyph(glyph);
                for (int row = 0; row < rows.Length; row++)
                for (int column = 0; column < rows[row].Length; column++)
                    if (rows[row][column] == '1')
                        Fill(pixels, width, x + column * scale, y + row * scale, scale, scale, color);
                x += 6 * scale;
            }
        }

        private static string[] Glyph(char value)
        {
            switch (value)
            {
                case '0': return new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" };
                case '1': return new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" };
                case '2': return new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" };
                case '3': return new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" };
                case '4': return new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" };
                case '5': return new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" };
                case '6': return new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" };
                case '7': return new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" };
                case '8': return new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" };
                case '9': return new[] { "01110", "10001", "10001", "01111", "00001", "00010", "11100" };
                case 'A': return new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" };
                case 'B': return new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" };
                case 'E': return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" };
                case 'F': return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" };
                case 'G': return new[] { "01111", "10000", "10000", "10111", "10001", "10001", "01111" };
                case 'I': return new[] { "01110", "00100", "00100", "00100", "00100", "00100", "01110" };
                case 'L': return new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" };
                case 'N': return new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" };
                case 'O': return new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" };
                case 'P': return new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" };
                case 'R': return new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" };
                case 'S': return new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" };
                case 'T': return new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" };
                case 'V': return new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" };
                case 'X': return new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" };
                case 'Y': return new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" };
                default: return new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" };
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
            SetAutoProperty(definition, "DescriptionKo", "RMAP15 focused fixture");
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
