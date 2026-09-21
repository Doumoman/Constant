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
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.WorldData;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    [Category("SV5_04")]
    public sealed class Sv5CoreReservationPlanTests
    {
        private static readonly Lazy<Sv5CoreReservationPlan> Representative =
            new Lazy<Sv5CoreReservationPlan>(() => Build(1304));

        [Test]
        public void T01_RepresentativePlanRetainsEightSitesAnd2432CoreCells()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            Assert.That(plan.Sites.Count, Is.EqualTo(8));
            Assert.That(plan.CoreCells.Count, Is.EqualTo(2432));
            Assert.That(plan.Slots.Count, Is.EqualTo(10));
            Assert.That(plan.Source.Accesses.Sum(value => value.OpenCells.Count), Is.EqualTo(45));
            Assert.That(plan.StateGeometry.Count, Is.EqualTo(6));
            Assert.That(plan.Sites.Select(value => value.Id), Is.EquivalentTo(plan.Source.Sites.Select(value => value.Id)));
            Assert.That(ReferenceEquals(plan.CoreCells, plan.Source.Cells), Is.True);
            Assert.That(plan.Sites.Single(value => value.Role == Sv5SpecialPhysicalRole.Village).Id,
                Is.EqualTo("SV5_SITE_VILLAGE"));
            Assert.That(plan.GraphBindings.Single(value => value.Role == Sv5WorldGraphRole.Seal).SiteId,
                Is.EqualTo(plan.GraphBindings.Single(value => value.Role == Sv5WorldGraphRole.Boss).SiteId));
        }

        [Test]
        public void T02_ConsumerRejectsProtectedCoreAirAndSlotSupportOrHeadroom()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            Sv5SpecialWorldCell fixedSolid = plan.CoreCells.First(value => value.Protection == Sv5SpecialProtectionKind.FixedSolid);
            Sv5SpecialWorldCell protectedAir = plan.CoreCells.First(value => value.Protection == Sv5SpecialProtectionKind.ProtectedAir);
            Sv5SpecialSlot slot = plan.Slots.First();
            AssertRejected(plan, fixedSolid.World, Sv5PatternBaseCell.Air, Sv5CoreReservationDiagnosticCode.ProtectedCoreCell);
            AssertRejected(plan, protectedAir.World, Sv5PatternBaseCell.Solid, Sv5CoreReservationDiagnosticCode.ProtectedCoreCell);
            AssertRejected(plan, new Sv5SpecialWorldPoint(slot.World.X, slot.World.Y - 1), Sv5PatternBaseCell.Air,
                Sv5CoreReservationDiagnosticCode.ProtectedCoreCell);
            AssertRejected(plan, new Sv5SpecialWorldPoint(slot.World.X, slot.World.Y + 1), Sv5PatternBaseCell.Solid,
                Sv5CoreReservationDiagnosticCode.ProtectedCoreCell);
        }

        [Test]
        public void T03_NormalCandidatesAndCompatibleDuplicatesAreAllowedButConflictsFail()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            Sv5SpecialWorldPoint point = FindUnreservedPoint(plan, 0, 0);
            Sv5CoreReservationDecision allowed = plan.EvaluateTerrainCandidates(new[]
            {
                new Sv5CoreTerrainCandidate("SV5_04_TEST", point, Sv5PatternBaseCell.Air),
                new Sv5CoreTerrainCandidate("SV5_04_TEST_REPEAT", point, Sv5PatternBaseCell.Air),
            });
            Sv5CoreReservationDecision conflict = plan.EvaluateTerrainCandidates(new[]
            {
                new Sv5CoreTerrainCandidate("SV5_04_TEST", point, Sv5PatternBaseCell.Air),
                new Sv5CoreTerrainCandidate("SV5_04_TEST_CONFLICT", point, Sv5PatternBaseCell.Solid),
            });
            Assert.That(allowed.IsAllowed, Is.True);
            Assert.That(conflict.IsAllowed, Is.False);
            Assert.That(conflict.Diagnostics.Single().Code, Is.EqualTo(Sv5CoreReservationDiagnosticCode.DuplicateCandidate));
        }

        [Test]
        public void T04_EveryCurrentGraphRouteUsesContinuousPortsAndStaticEvidence()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            Assert.That(plan.Routes.Count, Is.EqualTo(plan.RouteSource.Graph.Edges.Count));
            Assert.That(plan.Routes.Count, Is.EqualTo(11));
            Assert.That(plan.Routes.All(value => value.Source.Evidence.IsValid), Is.True);
            foreach (Sv5CoreRouteReservation route in plan.Routes)
            {
                Assert.That(plan.AccessBindings.Single(value => value.Source.Id == route.FromPortId).RouteIds,
                    Does.Contain(route.RouteId));
                Assert.That(plan.AccessBindings.Single(value => value.Source.Id == route.ToPortId).RouteIds,
                    Does.Contain(route.RouteId));
                for (int index = 1; index < route.Cells.Count; index++)
                    Assert.That(Math.Abs(route.Cells[index].X - route.Cells[index - 1].X) +
                        Math.Abs(route.Cells[index].Y - route.Cells[index - 1].Y), Is.EqualTo(1), route.RouteId);
            }
            Assert.That(plan.AccessBindings.Count(value => value.IsGraphRouteBound), Is.EqualTo(12));
            Assert.That(plan.AccessBindings.Count(value => value.Status == "PRESERVED_NOT_SELECTED_BY_CURRENT_ROUTE_PLAN"), Is.EqualTo(1));
            Assert.That(plan.AccessBindings.Count(value => value.Status == "PRESERVED_NO_GRAPH_RESERVATION"), Is.EqualTo(2));
        }

        [Test]
        public void T05_ConsumerRejectsRouteAndStateGeometryIntrusions()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            Sv5CoreRouteCellReservation passage = plan.RouteCells.First(value => value.Kind == Sv5CoreRouteReservationKind.Passage);
            Sv5CoreRouteCellReservation clearance = plan.RouteCells.First(value => value.Kind == Sv5CoreRouteReservationKind.Clearance &&
                (!plan.TryGetRouteCells(value.World, out IReadOnlyList<Sv5CoreRouteCellReservation> all) ||
                 all.All(item => item.Kind != Sv5CoreRouteReservationKind.Passage)));
            Sv5CoreRouteCellReservation support = plan.RouteCells.First(value => value.Kind == Sv5CoreRouteReservationKind.Support);
            Sv5SpecialStateGeometryCell sealedGate = plan.StateGeometry.First(value => value.State == "SEALED");
            AssertRejected(plan, passage.World, Sv5PatternBaseCell.Solid, Sv5CoreReservationDiagnosticCode.RoutePassageBlocked);
            AssertRejected(plan, clearance.World, Sv5PatternBaseCell.Solid, Sv5CoreReservationDiagnosticCode.RouteClearanceBlocked);
            AssertRejected(plan, support.World, Sv5PatternBaseCell.Air, Sv5CoreReservationDiagnosticCode.RouteSupportRemoved);
            AssertRejected(plan, sealedGate.World, Sv5PatternBaseCell.Air, Sv5CoreReservationDiagnosticCode.StateGeometry);
        }

        [Test]
        public void T06_OutOfRangeAndMixedSourcePlansFailWithoutRelocation()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            Sv5CoreReservationDecision outside = plan.EvaluateTerrainCandidates(new[]
            {
                new Sv5CoreTerrainCandidate("SV5_04_TEST", new Sv5SpecialWorldPoint(-1, 0), Sv5PatternBaseCell.Air),
            });
            Sv5ClusterAssemblyPlan another = BuildRouteSource(1305);
            Assert.That(outside.Diagnostics.Single().Code, Is.EqualTo(Sv5CoreReservationDiagnosticCode.OutOfWorld));
            Assert.Throws<ArgumentException>(() => Sv5CoreReservationPlanner.Plan(another.SpecialPlan, plan.RouteSource));
            Assert.That(plan.Source.Sites.Count, Is.EqualTo(8));
            Assert.That(plan.CoreCells.Count, Is.EqualTo(2432));
        }

        [Test]
        public void T07_RebuildAndCandidateOrderAreDeterministicWithoutNewRng()
        {
            Sv5CoreReservationPlan first = Build(1304);
            Sv5CoreReservationPlan second = Build(1304);
            Sv5SpecialWorldPoint firstPoint = FindUnreservedPoint(first, 0, 0);
            Sv5SpecialWorldPoint secondPoint = FindUnreservedPoint(first, firstPoint.X + 1, firstPoint.Y);
            Sv5CoreReservationDecision forward = first.EvaluateTerrainCandidates(new[]
            {
                new Sv5CoreTerrainCandidate("A", firstPoint, Sv5PatternBaseCell.Air),
                new Sv5CoreTerrainCandidate("B", secondPoint, Sv5PatternBaseCell.Air),
            });
            Sv5CoreReservationDecision reverse = first.EvaluateTerrainCandidates(new[]
            {
                new Sv5CoreTerrainCandidate("B", secondPoint, Sv5PatternBaseCell.Air),
                new Sv5CoreTerrainCandidate("A", firstPoint, Sv5PatternBaseCell.Air),
            });
            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            Assert.That(second.Source.Digest, Is.EqualTo(first.Source.Digest));
            Assert.That(second.RouteSource.Digest, Is.EqualTo(first.RouteSource.Digest));
            Assert.That(forward.IsAllowed && reverse.IsAllowed, Is.True);
            Assert.That(forward.Diagnostics.Select(value => value.Reason), Is.EqualTo(reverse.Diagnostics.Select(value => value.Reason)));
        }

        [Test]
        public void T08_ExportsComeFromTheSameValidatedPlan()
        {
            Sv5CoreReservationPlan plan = Representative.Value;
            string directory = GeneratedDirectory();
            Directory.CreateDirectory(directory);
            Write(Path.Combine(directory, "core_sites.csv"), Sv5CoreReservationExport.CoreSitesCsv(plan));
            Write(Path.Combine(directory, "core_cells.csv"), Sv5CoreReservationExport.CoreCellsCsv(plan));
            Write(Path.Combine(directory, "access_bindings.csv"), Sv5CoreReservationExport.AccessBindingsCsv(plan));
            Write(Path.Combine(directory, "route_cells.csv"), Sv5CoreReservationExport.RouteCellsCsv(plan));
            Write(Path.Combine(directory, "state_geometry.csv"), Sv5CoreReservationExport.StateGeometryCsv(plan));
            Write(Path.Combine(directory, "reservation_manifest.json"), Sv5CoreReservationExport.ManifestJson(plan));
            Assert.That(File.ReadLines(Path.Combine(directory, "core_sites.csv")).Skip(1).Count(), Is.EqualTo(plan.Sites.Count));
            Assert.That(File.ReadLines(Path.Combine(directory, "core_cells.csv")).Skip(1).Count(), Is.EqualTo(2432));
            Assert.That(File.ReadLines(Path.Combine(directory, "access_bindings.csv")).Skip(1).Count(), Is.EqualTo(plan.AccessBindings.Count));
            Assert.That(File.ReadLines(Path.Combine(directory, "state_geometry.csv")).Skip(1).Count(), Is.EqualTo(6));
            Assert.That(Sv5CoreReservationExport.ManifestJson(plan), Does.Contain("\"core_reservation_digest\": \"" + plan.Digest));
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly))
                Assert.That(new FileInfo(file).Length, Is.GreaterThan(0), file);
            TestContext.Out.WriteLine("SV5_04_EXPORT_BEGIN");
            TestContext.Out.WriteLine("PLAN_DIGEST=" + plan.Digest);
            TestContext.Out.WriteLine("OUTPUT_DIRECTORY=" + directory);
            TestContext.Out.WriteLine("SV5_04_EXPORT_END");
        }

        private static void AssertRejected(Sv5CoreReservationPlan plan, Sv5SpecialWorldPoint world,
            Sv5PatternBaseCell proposed, Sv5CoreReservationDiagnosticCode code)
        {
            Sv5CoreReservationDecision decision = plan.EvaluateTerrainCandidates(new[]
            {
                new Sv5CoreTerrainCandidate("SV5_04_TEST", world, proposed),
            });
            Assert.That(decision.IsAllowed, Is.False);
            Assert.That(decision.Diagnostics.Select(value => value.Code), Does.Contain(code));
        }

        private static Sv5SpecialWorldPoint FindUnreservedPoint(Sv5CoreReservationPlan plan, int minimumX, int minimumY)
        {
            for (int y = Math.Max(0, minimumY); y < Sv5SpecialReservationPlanner.WorldHeightTiles; y++)
            for (int x = y == Math.Max(0, minimumY) ? Math.Max(0, minimumX) : 0; x < Sv5SpecialReservationPlanner.WorldWidthTiles; x++)
            {
                var point = new Sv5SpecialWorldPoint(x, y);
                if (!plan.Source.TryGetCell(point, out _) && !plan.TryGetRouteCells(point, out _)) return point;
            }
            throw new AssertionException("No unreserved representative point was found.");
        }

        private static Sv5CoreReservationPlan Build(ulong seed) => Sv5CoreReservationPlanner.Plan(BuildRouteSource(seed));

        private static Sv5ClusterAssemblyPlan BuildRouteSource(ulong seed)
        {
            WorldGenerationRngStreams streams = RngStreams();
            Sv5WorldDefinition definition = Sv5WorldDataGenerator.Generate(
                new Sv5WorldDataRequest(seed, "CONTENT_V1", "GENERATOR_V1"), streams);
            return Sv5ClusterAssemblyPlanner.Plan(definition, streams);
        }

        private static string GeneratedDirectory()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(root, "Temp", "SV5Tests", "core_reservation_t08");
        }

        private static void Write(string path, string text) => File.WriteAllText(path, text, new UTF8Encoding(false));

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
            SetAutoProperty(definition, "DescriptionKo", "SV5_04 focused fixture");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            byte[] bytes = Enumerable.Range(0, value.Length / 2).Select(index => byte.Parse(value.Substring(index * 2, 2),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string name, object value)
        {
            var field = target.GetType().GetField("<" + name + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
#endif
