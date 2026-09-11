#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.Map.WorldGeneration.WorldData;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    [Category("SV5_06")]
    public sealed class Sv5SpaceGraphPlanTests
    {
        private static readonly Lazy<Sv5CoreReservationPlan> Core1304 =
            new Lazy<Sv5CoreReservationPlan>(() => BuildCore(1304));
        private static readonly Lazy<Sv5SpaceGraphPlan> Representative =
            new Lazy<Sv5SpaceGraphPlan>(() => Sv5SpaceGraphPlanner.Plan(Core1304.Value, 1304));

        [Test]
        public void T01_CoreInputKeepsExactWorldGraphAndReservationIdentity()
        {
            Sv5CoreReservationPlan core = Core1304.Value;
            Sv5SpaceGraphPlan plan = Representative.Value;
            Assert.That(plan.Success, Is.True, string.Join(";", plan.Diagnostics));
            Assert.That(plan.Seed, Is.EqualTo(core.RouteSource.Definition.Request.Seed));
            Assert.That(ReferenceEquals(plan.Core, core), Is.True);
            Assert.That(ReferenceEquals(plan.Core.Source, core.RouteSource.SpecialPlan), Is.True);
            Assert.That(plan.Core.Sites.Count, Is.EqualTo(8));
            Assert.That(plan.Core.CoreCells.Count, Is.EqualTo(2432));
            Assert.That(plan.Core.Slots.Count, Is.EqualTo(10));
            Assert.That(plan.Core.StateGeometry.Count, Is.EqualTo(6));
            Assert.Throws<ArgumentException>(() => Sv5SpaceGraphPlanner.Plan(core, 1305));
        }

        [Test]
        public void T02_LayoutIsDeterministicDistributedAndNotTheApprovedExampleAnswer()
        {
            Sv5SpaceGraphPlan first = Representative.Value;
            Sv5SpaceGraphPlan second = Sv5SpaceGraphPlanner.Plan(Core1304.Value, 1304,
                new Sv5SpaceGraphAuthoringProfile(first.Profile.Id, first.Profile.Version,
                    first.Profile.Families.Reverse()));
            Sv5SpaceGraphPlan other = Sv5SpaceGraphPlanner.Plan(BuildCore(1305), 1305);
            Sv5SpaceGraphPlan third = Sv5SpaceGraphPlanner.Plan(BuildCore(1306), 1306);
            Assert.That(second.Digest, Is.EqualTo(first.Digest));
            Assert.That(other.Digest, Is.Not.EqualTo(first.Digest));
            Assert.That(third.Digest, Is.Not.EqualTo(first.Digest));
            Assert.That(first.Places.Count(value => value.Kind == Sv5SpacePlaceKind.Large), Is.GreaterThanOrEqualTo(8));
            Assert.That(first.Places.Count(value => value.Kind == Sv5SpacePlaceKind.Ordinary), Is.GreaterThanOrEqualTo(6));
            Assert.That(first.Places.Where(value => value.Kind != Sv5SpacePlaceKind.Core)
                .Select(value => value.DistributionSector).Distinct().Count(), Is.GreaterThanOrEqualTo(10));
            Assert.That(first.Places.Count, Is.Not.EqualTo(101));
            Assert.That(first.Connections.Count, Is.Not.EqualTo(166));
            Assert.That(first.Seed, Is.Not.EqualTo(40921));
            Assert.That(first.Places.Any(value => value.Bounds.Width >= 60), Is.True);
        }

        [Test]
        public void T03_PortsBindActualCoreAccessAndOptionalStartVillageReturn()
        {
            Sv5SpaceGraphPlan plan = Representative.Value;
            foreach (RmapSpecialAccess access in plan.Core.Source.Accesses)
            {
                Sv5SpacePort port = plan.Ports.Single(value => value.Id == access.Id);
                Assert.That(port.BoundaryCells, Is.EqualTo(access.OpenCells));
                Assert.That(port.Direction, Is.EqualTo(access.Side));
                Assert.That(port.Condition, Is.EqualTo(access.Condition));
                Assert.That(port.SourceAccessId, Is.EqualTo(access.Id));
            }
            Assert.That(plan.Connections.Any(value => value.FromPortId == "RMAP15_SITE_START_PORT_EXIT" ||
                value.ToPortId == "RMAP15_SITE_START_PORT_EXIT"), Is.False);
            Assert.That(plan.Ports.Single(value => value.Id == "RMAP15_SITE_START_PORT_EXIT").Status,
                Does.StartWith("UNUSED_WITH_REASON:"));
            Assert.That(plan.Connections.Any(value => value.FromPortId == "RMAP15_SITE_START_PORT_ENTRY" &&
                value.Kind == Sv5SpaceConnectionKind.OptionalBranch), Is.True);
            Assert.That(plan.Connections.Any(value => value.ToPortId == "RMAP15_SITE_VILLAGE_PORT_ENTRY"), Is.True);
            Assert.That(plan.Connections.Any(value => value.FromPortId == "RMAP15_SITE_VILLAGE_PORT_EXIT"), Is.True);
            Assert.That(plan.Connections.Any(value => value.Kind == Sv5SpaceConnectionKind.VillageInterior), Is.True);
            foreach (Sv5SpaceConnection connection in plan.Connections)
            {
                Assert.That(plan.Ports.Single(value => value.Id == connection.FromPortId).BoundaryCells,
                    Does.Contain(connection.Centerline.First()), connection.Id);
                Assert.That(plan.Ports.Single(value => value.Id == connection.ToPortId).BoundaryCells,
                    Does.Contain(connection.Centerline.Last()), connection.Id);
            }
        }

        [Test]
        public void T04_CompleteContactEnumeratorKeepsCommonRouteFacePairsAndLayers()
        {
            var p = new RmapSpecialWorldPoint(10, 10);
            var q = new RmapSpecialWorldPoint(11, 10);
            IReadOnlyList<Sv5RouteContactPair> face = Sv5RouteStatePolicy.EnumerateContactPairs(new[]
            {
                new Sv5RouteContactCell("A", p, Sv5RouteContactCellKind.Passage),
                new Sv5RouteContactCell("B", p, Sv5RouteContactCellKind.Clearance),
                new Sv5RouteContactCell("A", q, Sv5RouteContactCellKind.Clearance),
                new Sv5RouteContactCell("C", q, Sv5RouteContactCellKind.Passage),
            }).Where(value => value.Kind == "FACE").ToArray();
            Assert.That(face.Select(value => value.RouteA + value.RouteB), Is.EquivalentTo(new[] { "AB", "AC", "BC" }));
            Assert.That(face.All(value => value.Direction == "RIGHT"), Is.True);

            IReadOnlyList<Sv5RouteContactPair> shared = Sv5RouteStatePolicy.EnumerateContactPairs(new[]
            {
                new Sv5RouteContactCell("A", p, Sv5RouteContactCellKind.Passage),
                new Sv5RouteContactCell("B", p, Sv5RouteContactCellKind.Passage),
                new Sv5RouteContactCell("C", p, Sv5RouteContactCellKind.Clearance),
            }).Where(value => value.Kind == "SHARED").ToArray();
            Assert.That(shared.Count, Is.EqualTo(3));
            Assert.That(shared.Select(value => value.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(face.Single(value => value.RouteA == "A" && value.RouteB == "B").RouteAWorld,
                Is.EqualTo(q));
            Assert.That(face.Single(value => value.RouteA == "A" && value.RouteB == "B").RouteBWorld,
                Is.EqualTo(p));
            Assert.That(Representative.Value.ContactDecisions.Count, Is.GreaterThanOrEqualTo(1422));
        }

        [Test]
        public void T05_ContactTruthRequiresSharedFsmAndGuardsSensitiveMidRouteSwitches()
        {
            Sv5RouteStateAnalysis oldLayer = Sv5RouteStatePolicy.Analyze(Core1304.Value,
                Array.Empty<Sv5RouteShortcutCandidate>(), new Sv5RouteStateReviewInput(
                    Array.Empty<Sv5RouteStateReviewContact>(), Array.Empty<RmapSpecialWorldPoint>()));
            Assert.That(oldLayer.LogicalStateVerified, Is.True);
            Assert.That(oldLayer.ContactStateVerified, Is.False);
            Assert.That(oldLayer.ContactChecks.All(value => !value.LogicalStateTransitionChecked), Is.True);

            Sv5SpaceGraphPlan plan = Representative.Value;
            Assert.That(plan.ContactDecisions.All(value => value.LogicalStateTransitionChecked), Is.True);
            Assert.That(plan.ContactDecisions.Any(value => value.Crossing == Sv5SpaceCrossingKind.Join), Is.True);
            Assert.That(plan.ContactDecisions.Where(value => value.Predicate.Contains("seal=1"))
                .All(value => value.Crossing == Sv5SpaceCrossingKind.Join ||
                    value.Crossing == Sv5SpaceCrossingKind.ConditionalGate), Is.True);
            Assert.That(plan.Gates.Any(value => value.TypedPredicate.RequiresSeal), Is.True);
            Assert.That(plan.Gates.All(value => !value.RuntimeVerified), Is.True);
            Assert.That(plan.GeometryStateReady, Is.False);
            Assert.That(plan.PlayerVerified, Is.False);
        }

        [Test]
        public void T06_ActualConnectorProjectionPassesSixOrdersWithoutAbstractEdgeSubstitution()
        {
            Sv5SpaceGraphPlan plan = Representative.Value;
            Sv5SpaceConnection[] coreConnections = plan.Connections.Where(value =>
                value.Kind == Sv5SpaceConnectionKind.CoreProgression).ToArray();
            Assert.That(coreConnections.Length, Is.EqualTo(11));
            Assert.That(coreConnections.Select(value => value.SourceGraphEdgeId),
                Is.EquivalentTo(plan.Core.RouteSource.Graph.Edges.Select(value => value.EdgeId)));
            Assert.That(coreConnections.All(value => value.Centerline.Count > 1), Is.True);
            Assert.That(plan.ProjectionProofs.Count, Is.EqualTo(6));
            Assert.That(plan.ProjectionProofs.All(value => value.Success), Is.True);
            Assert.That(plan.ProjectionProofs.All(value => value.GoalProof.Actions.Count(action =>
                action.StartsWith("ACQUIRE|", StringComparison.Ordinal)) == 3), Is.True);
        }

        [Test]
        public void T07_GatesAndCrossingsAreExplicitAndCannotClaimRuntimeGeometry()
        {
            Sv5SpaceGraphPlan plan = Representative.Value;
            Assert.That(plan.ContactDecisions.All(value => value.Crossing != Sv5SpaceCrossingKind.Pending), Is.True);
            Assert.That(plan.ContactDecisions.All(value => value.Crossing == Sv5SpaceCrossingKind.Join ||
                value.Crossing == Sv5SpaceCrossingKind.ConditionalGate), Is.True);
            // A legal reroute can remove all cross-route ConditionalGate contacts.
            // The three actual gates and their nine physical state checks remain mandatory.
            Assert.That(plan.Gates.Count, Is.EqualTo(3));
            Assert.That(plan.PhysicalMovement.GateStateChecks.Count, Is.EqualTo(9));
            Assert.That(plan.PhysicalMovement.GateStateChecks.All(c => c.Success), Is.True);
            Assert.That(plan.Gates.Select(value => value.SourceConnectionId).Distinct().Count(),
                Is.EqualTo(plan.Gates.Count));
            Assert.That(plan.Gates.All(value => value.SealedState.Contains("GLOBAL_WORLD_FACE_CUT") &&
                value.OpenState.Contains("GLOBAL_WORLD_FACES")), Is.True);
            Assert.That(plan.Reservations.Count(value => value.Kind == Sv5SpaceReservationKind.ConditionalGate),
                Is.GreaterThanOrEqualTo(plan.Gates.Count));
            Assert.That(plan.Gates.All(value => value.PlannedBarrierVerified &&
                value.BlockingCells.Count == 0 && value.BlockingFaces.Count > 0), Is.True);
            Assert.That(plan.Gates.Any(value => value.TypedPredicate.RequiresBossComplete), Is.True);
        }

        [Test]
        public void T08_AllReachableStatesRecoverAndOptionalCircuitHasNoTrappingDeadEnd()
        {
            Sv5SpaceGraphPlan plan = Representative.Value;
            Assert.That(plan.ProjectionProofs.All(value => value.ReachableStates == value.ReverseReachableStates), Is.True);
            Assert.That(plan.ProjectionProofs.All(value => value.DeadEnds.Count == 0), Is.True);
            Assert.That(plan.Connections.Count(value => value.Kind == Sv5SpaceConnectionKind.OptionalBranch),
                Is.GreaterThan(plan.Places.Count(value => value.Kind == Sv5SpacePlaceKind.Large)));
            Assert.That(plan.Connections.Any(value => value.Kind == Sv5SpaceConnectionKind.OptionalBranch &&
                value.ToPlaceId == "RMAP15_SITE_START"), Is.True);
        }

        [Test]
        public void T09_DigestAndSchemasTrackSemanticsButIgnoreInputEnumerationOrder()
        {
            Sv5SpaceGraphPlan plan = Representative.Value;
            Sv5SpaceGraphAuthoringProfile reverse = new Sv5SpaceGraphAuthoringProfile(plan.Profile.Id,
                plan.Profile.Version, plan.Profile.Families.Reverse());
            Assert.That(reverse.Digest, Is.EqualTo(plan.Profile.Digest));
            Assert.That(Sv5SpaceGraphExport.SpaceGraphJson(plan), Does.Contain(plan.Digest));
            Assert.That(Sv5SpaceGraphExport.PlacesCsv(plan).Split('\n')[0], Does.StartWith("place_id,family,kind"));
            Assert.That(Sv5SpaceGraphExport.PortsCsv(plan).Split('\n')[0], Does.StartWith("port_id,place_id,boundary_cells"));
            Assert.That(Sv5SpaceGraphExport.ReservationCellsCsv(plan), Does.Contain("micro_chunk_x"));
            Assert.That(plan.Reservations.All(value => value.MicroChunkX == value.World.X / 12 &&
                value.MicroChunkY == value.World.Y / 8 && value.PatternX == value.World.X / 4 &&
                value.PatternY == value.World.Y / 4), Is.True);
            Assert.That(plan.InfillPendingTileCount, Is.GreaterThan(0));
        }

        [Test]
        public void T10_ExportsOnePlanWithoutChangingHistoricalEvidence()
        {
            string[] preserved =
            {
                Path.Combine(ProjectRoot(), "MapDesign", "MCP", "GENERATED", "SV5_04", "reservation_manifest.json"),
                Path.Combine(ProjectRoot(), "MapDesign", "MCP", "GENERATED", "SV5_05", "route_state_manifest.json"),
                Path.Combine(ProjectRoot(), "MapDesign", "MCP", "GENERATED", "SV5_05_FIX01", "route_state_manifest.json"),
            };
            string[] hashes = preserved.Select(HashFile).ToArray();
            string directory = GeneratedDirectory();
            Sv5SpaceGraphExport.WriteAll(Representative.Value, directory);
            Assert.That(preserved.Select(HashFile), Is.EqualTo(hashes));
            string[] required =
            {
                "space_graph.json", "places.csv", "ports.csv", "connections.csv", "reservation_cells.csv",
                "state_proofs.json", "contact_checks.csv", "gate_geometry.json", "obligations.csv", "validation.json",
            };
            Assert.That(required.All(value => File.Exists(Path.Combine(directory, value)) &&
                new FileInfo(Path.Combine(directory, value)).Length > 0), Is.True);
            string preview = Path.Combine(directory, "preview");
            Assert.That(Directory.GetFiles(preview, "*.svg").Length, Is.EqualTo(20));
            Assert.That(File.ReadAllText(Path.Combine(preview, "overview.svg")), Does.Contain("viewBox=\"0 0 624 416\""));
            Assert.That(File.ReadAllText(Path.Combine(preview, "A1.svg")), Does.Contain("viewBox=\"0 0 156 104\""));
            Assert.That(File.ReadAllText(Path.Combine(directory, "validation.json")), Does.Contain("\"status\": \"PASS\""));
            TestContext.Out.WriteLine("SV5_06_EXPORT_BEGIN");
            TestContext.Out.WriteLine("PLAN_DIGEST=" + Representative.Value.Digest);
            TestContext.Out.WriteLine("OUTPUT_DIRECTORY=" + directory);
            TestContext.Out.WriteLine("SV5_06_EXPORT_END");
        }

        private static Sv5CoreReservationPlan BuildCore(ulong seed)
        {
            WorldGenerationRngStreams streams = RngStreams();
            RmapWorldDefinition definition = RmapWorldDataGenerator.Generate(
                new RmapWorldDataRequest(seed, "CONTENT_V1", "GENERATOR_V1"), streams);
            Rmap16ClusterAssemblyPlan routeSource = RmapClusterAssemblyPlanner.Plan(definition, streams);
            return Sv5CoreReservationPlanner.Plan(routeSource);
        }

        private static string GeneratedDirectory() => Path.Combine(ProjectRoot(), "MapDesign", "MCP", "GENERATED",
            "SV5_08_FIX01", "_work", "legacy_exports", "sv5_06_original_t10");
        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(File.ReadAllBytes(path))
                .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
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
            SetAutoProperty(definition, "DescriptionKo", "SV5_06 focused fixture");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            byte[] bytes = Enumerable.Range(0, value.Length / 2).Select(index => byte.Parse(value.Substring(index * 2, 2),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            ConstructorInfo constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance |
                BindingFlags.NonPublic, null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
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
