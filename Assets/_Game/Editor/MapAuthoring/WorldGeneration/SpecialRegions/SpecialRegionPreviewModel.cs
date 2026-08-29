using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Editor.WorldGeneration.SpecialRegions
{
    public enum SpecialRegionPreviewViewMode
    {
        Overview = 1,
        Footprint = 2,
        Layers = 3,
        Routes = 4,
        States = 5,
        Reset = 6,
        Audit = 7,
        Compare = 8,
    }

    [Flags]
    public enum SpecialRegionPreviewOverlay
    {
        None = 0,
        DesignChunks = 1 << 0,
        SectorSeams = 1 << 1,
        EntryReturn = 1 << 2,
        ApronsBuffers = 1 << 3,
        FixedCollision = 1 << 4,
        FixedAccess = 1 << 5,
        ReplaceableSlots = 1 << 6,
        LowRoute = 1 << 7,
        HighRoute = 1 << 8,
        RecoveryRoute = 1 << 9,
        RequiredReward = 1 << 10,
        StateMarkers = 1 << 11,
        ResetMarkers = 1 << 12,
        All = (1 << 13) - 1,
    }

    public sealed class SpecialRegionPreviewSelection
    {
        public SpecialRegionPreviewSelection(SpecialRegionAuditFamily family, string artifactId)
        {
            Family = family;
            ArtifactId = artifactId ?? string.Empty;
        }

        public SpecialRegionAuditFamily Family { get; }
        public string ArtifactId { get; }
    }

    public sealed class SpecialRegionPreviewLegendEntry
    {
        public SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind kind, string token, string meaning)
        {
            Kind = kind;
            Token = token ?? string.Empty;
            Meaning = meaning ?? string.Empty;
        }

        public SpecialRegionAuditTokenKind Kind { get; }
        public string Token { get; }
        public string Meaning { get; }
    }

    public sealed class SpecialRegionPreviewSnapshot
    {
        private readonly ReadOnlyCollection<SpecialRegionAuditToken> tokens;
        private readonly ReadOnlyCollection<SpecialRegionPreviewLegendEntry> legend;

        internal SpecialRegionPreviewSnapshot(
            SpecialRegionPreviewSelection selection,
            SpecialRegionPreviewViewMode viewMode,
            SpecialRegionPreviewOverlay overlays,
            SpecialRegionAuditArtifactResult artifact,
            IEnumerable<SpecialRegionAuditToken> tokens,
            IEnumerable<SpecialRegionPreviewLegendEntry> legend,
            string auditDigest)
        {
            Selection = selection;
            ViewMode = viewMode;
            Overlays = overlays;
            Artifact = artifact;
            this.tokens = new ReadOnlyCollection<SpecialRegionAuditToken>((tokens ?? Array.Empty<SpecialRegionAuditToken>()).ToArray());
            this.legend = new ReadOnlyCollection<SpecialRegionPreviewLegendEntry>((legend ?? Array.Empty<SpecialRegionPreviewLegendEntry>()).ToArray());
            BindingBanner = artifact.Binding == SpecialRegionAuditBinding.ReferenceFixture
                ? "REFERENCE FIXTURE" : "DEFERRED TO MAP14";
            ProvenanceLabel = artifact.Binding == SpecialRegionAuditBinding.ReferenceFixture
                ? "Deterministic in-memory reference input; not a live/generated world"
                : "Local authoring plan only; placement authority remains MAP14";
            PhysicsWarning = "PHYSICS NOT VERIFIED";
            AuditDigest = auditDigest ?? string.Empty;

            var maxX = Math.Max(1, artifact.Input.DesignWidth - 1);
            var maxY = Math.Max(1, artifact.Input.DesignHeight - 1);
            if (this.tokens.Count != 0)
            {
                maxX = Math.Max(maxX, this.tokens.Max(value => value.X));
                maxY = Math.Max(maxY, this.tokens.Max(value => value.Y));
            }
            GridMinimumX = 0;
            GridMinimumY = 0;
            GridMaximumX = maxX;
            GridMaximumY = maxY;
        }

        public SpecialRegionPreviewSelection Selection { get; }
        public SpecialRegionPreviewViewMode ViewMode { get; }
        public SpecialRegionPreviewOverlay Overlays { get; }
        public SpecialRegionAuditArtifactResult Artifact { get; }
        public IReadOnlyList<SpecialRegionAuditToken> Tokens => tokens;
        public IReadOnlyList<SpecialRegionPreviewLegendEntry> Legend => legend;
        public string BindingBanner { get; }
        public string ProvenanceLabel { get; }
        public string PhysicsWarning { get; }
        public string AuditDigest { get; }
        public int GridMinimumX { get; }
        public int GridMinimumY { get; }
        public int GridMaximumX { get; }
        public int GridMaximumY { get; }
        public int ScaleToFitTokenCount => tokens.Count;
        public int AuditSectionPassCount => Artifact.Sections.Count(value => value.Passed);
        public int AuditSectionFailCount => Artifact.Sections.Count(value => !value.Passed);
    }

    public sealed class SpecialRegionPreviewBuildResult
    {
        private readonly ReadOnlyCollection<string> errors;

        internal SpecialRegionPreviewBuildResult(SpecialRegionPreviewSnapshot snapshot, IEnumerable<string> errors)
        {
            var values = (errors ?? Array.Empty<string>()).Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            this.errors = new ReadOnlyCollection<string>(values);
            Snapshot = values.Length == 0 ? snapshot : null;
        }

        public bool Success => Snapshot != null && errors.Count == 0;
        public SpecialRegionPreviewSnapshot Snapshot { get; }
        public IReadOnlyList<string> Errors => errors;
    }

    public sealed class SpecialRegionPreviewModel
    {
        private static readonly ReadOnlyCollection<SpecialRegionPreviewLegendEntry> LegendEntries =
            new ReadOnlyCollection<SpecialRegionPreviewLegendEntry>(new[]
        {
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.DesignChunk, "CHUNK", "active design chunk"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.SectorSeam, "SEAM", "internal sector seam"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Entry, "ENTRY", "mandatory no-tool Entry"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Return, "RETURN", "mandatory no-tool Return"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Apron, "APRON", "protected entry/return apron"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Buffer, "BUFFER", "Before/After quiet buffer"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.FixedCollision, "FIXED-C", "immutable collision shell"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.FixedAccess, "FIXED-A", "immutable access cell"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Facility, "FACILITY", "replaceable Facility slot"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Npc, "NPC", "replaceable NPC marker"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Enemy, "ENEMY", "replaceable Enemy marker"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Event, "EVENT", "replaceable Event marker"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Reward, "REWARD", "required Reward persistence slot"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.LowRoute, "LOW", "mandatory low route"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.HighRoute, "HIGH", "optional high route"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.RecoveryRoute, "RECOVERY", "failure recovery route"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.State, "STATE", "shell-preserving state marker"),
            new SpecialRegionPreviewLegendEntry(SpecialRegionAuditTokenKind.Reset, "RESET", "manual/encounter/persistent reset"),
        });

        private IReadOnlyList<SpecialRegionAuditArtifactInput> inputs;
        private SpecialRegionValidationAuditResult audit;
        private SpecialRegionPreviewSelection selection;
        private SpecialRegionPreviewViewMode viewMode;
        private SpecialRegionPreviewOverlay overlays;

        public SpecialRegionPreviewModel()
        {
            viewMode = SpecialRegionPreviewViewMode.Overview;
            overlays = SpecialRegionPreviewOverlay.All;
            Reload();
        }

        public IReadOnlyList<SpecialRegionAuditArtifactInput> Artifacts => inputs;
        public SpecialRegionValidationAuditResult AuditResult => audit;
        public IReadOnlyList<SpecialRegionPreviewLegendEntry> Legend => LegendEntries;

        public SpecialRegionPreviewBuildResult BuildDefault()
        {
            if (inputs == null || inputs.Count == 0)
                return new SpecialRegionPreviewBuildResult(null, new[] { "No canonical artifacts are available." });
            selection = new SpecialRegionPreviewSelection(inputs[0].Family, inputs[0].ArtifactId);
            viewMode = SpecialRegionPreviewViewMode.Overview;
            overlays = SpecialRegionPreviewOverlay.All;
            return Build(selection, viewMode, overlays);
        }

        public SpecialRegionPreviewBuildResult Build(
            SpecialRegionPreviewSelection selected,
            SpecialRegionPreviewViewMode selectedView,
            SpecialRegionPreviewOverlay selectedOverlays)
        {
            if (audit == null || !audit.Success)
                return new SpecialRegionPreviewBuildResult(null,
                    audit == null ? new[] { "Audit has not been loaded." } : audit.Errors.Select(value => value.ToString()));
            if (selected == null)
                return new SpecialRegionPreviewBuildResult(null, new[] { "Selection is required." });
            if (!Enum.IsDefined(typeof(SpecialRegionPreviewViewMode), selectedView))
                return new SpecialRegionPreviewBuildResult(null, new[] { "Unknown view mode." });

            var artifact = audit.Report.Artifacts.SingleOrDefault(value =>
                value.Family == selected.Family && string.Equals(value.ArtifactId, selected.ArtifactId, StringComparison.Ordinal));
            if (artifact == null)
                return new SpecialRegionPreviewBuildResult(null, new[] { "Selected artifact is outside the canonical matrix." });

            selection = selected;
            viewMode = selectedView;
            overlays = selectedOverlays & SpecialRegionPreviewOverlay.All;
            var filtered = artifact.Tokens.Where(value => Visible(value.Kind, overlays)).ToArray();
            var snapshot = new SpecialRegionPreviewSnapshot(
                selection, viewMode, overlays, artifact, filtered, LegendEntries, audit.CanonicalDigest);
            return new SpecialRegionPreviewBuildResult(snapshot, Array.Empty<string>());
        }

        public bool TrySelectArtifact(string artifactId, out SpecialRegionPreviewSelection selected)
        {
            var input = (inputs ?? Array.Empty<SpecialRegionAuditArtifactInput>()).SingleOrDefault(value =>
                string.Equals(value.ArtifactId, artifactId, StringComparison.Ordinal));
            selected = input == null ? null : new SpecialRegionPreviewSelection(input.Family, input.ArtifactId);
            return selected != null;
        }

        public bool TrySelectViewMode(string value, out SpecialRegionPreviewViewMode selected)
        {
            SpecialRegionPreviewViewMode parsed;
            var success = Enum.TryParse(value ?? string.Empty, true, out parsed) &&
                          Enum.IsDefined(typeof(SpecialRegionPreviewViewMode), parsed);
            selected = success ? parsed : default(SpecialRegionPreviewViewMode);
            return success;
        }

        public SpecialRegionPreviewBuildResult Reload()
        {
            try
            {
                inputs = SpecialRegionReferenceFixtureFactory.BuildAll();
                audit = SpecialRegionValidationAuditor.Audit(new SpecialRegionAuditRequest(inputs));
                return BuildDefault();
            }
            catch (Exception exception)
            {
                inputs = Array.Empty<SpecialRegionAuditArtifactInput>();
                audit = null;
                return new SpecialRegionPreviewBuildResult(null,
                    new[] { exception.GetType().Name + ": " + exception.Message });
            }
        }

        private static bool Visible(SpecialRegionAuditTokenKind kind, SpecialRegionPreviewOverlay value)
        {
            switch (kind)
            {
                case SpecialRegionAuditTokenKind.DesignChunk: return Has(value, SpecialRegionPreviewOverlay.DesignChunks);
                case SpecialRegionAuditTokenKind.SectorSeam: return Has(value, SpecialRegionPreviewOverlay.SectorSeams);
                case SpecialRegionAuditTokenKind.Entry:
                case SpecialRegionAuditTokenKind.Return: return Has(value, SpecialRegionPreviewOverlay.EntryReturn);
                case SpecialRegionAuditTokenKind.Apron:
                case SpecialRegionAuditTokenKind.Buffer: return Has(value, SpecialRegionPreviewOverlay.ApronsBuffers);
                case SpecialRegionAuditTokenKind.FixedCollision: return Has(value, SpecialRegionPreviewOverlay.FixedCollision);
                case SpecialRegionAuditTokenKind.FixedAccess: return Has(value, SpecialRegionPreviewOverlay.FixedAccess);
                case SpecialRegionAuditTokenKind.Facility:
                case SpecialRegionAuditTokenKind.Npc:
                case SpecialRegionAuditTokenKind.Enemy:
                case SpecialRegionAuditTokenKind.Event: return Has(value, SpecialRegionPreviewOverlay.ReplaceableSlots);
                case SpecialRegionAuditTokenKind.LowRoute: return Has(value, SpecialRegionPreviewOverlay.LowRoute);
                case SpecialRegionAuditTokenKind.HighRoute: return Has(value, SpecialRegionPreviewOverlay.HighRoute);
                case SpecialRegionAuditTokenKind.RecoveryRoute: return Has(value, SpecialRegionPreviewOverlay.RecoveryRoute);
                case SpecialRegionAuditTokenKind.Reward: return Has(value, SpecialRegionPreviewOverlay.RequiredReward);
                case SpecialRegionAuditTokenKind.State: return Has(value, SpecialRegionPreviewOverlay.StateMarkers);
                case SpecialRegionAuditTokenKind.Reset: return Has(value, SpecialRegionPreviewOverlay.ResetMarkers);
                default: return false;
            }
        }

        private static bool Has(SpecialRegionPreviewOverlay value, SpecialRegionPreviewOverlay flag)
            => (value & flag) == flag;
    }

    internal static class SpecialRegionReferenceFixtureFactory
    {
        private sealed class SlotSpec
        {
            public SlotSpec(
                SpecialRegionSlotId id,
                SpecialRegionSlotKind kind,
                LocalTileCoord coordinate,
                bool required,
                SpecialPersistenceScope scope,
                SpecialPersistenceKey key,
                string occupantId)
            {
                Id = id;
                Kind = kind;
                Coordinate = coordinate;
                Required = required;
                Scope = scope;
                Key = key;
                OccupantId = occupantId ?? string.Empty;
            }

            public SpecialRegionSlotId Id { get; }
            public SpecialRegionSlotKind Kind { get; }
            public LocalTileCoord Coordinate { get; }
            public bool Required { get; }
            public SpecialPersistenceScope Scope { get; }
            public SpecialPersistenceKey Key { get; }
            public string OccupantId { get; }
        }

        private sealed class PlacedParts
        {
            public PlacedParts(
                SpecialRegionSiteBridge bridge,
                SpecialRegionEntryBufferPlan entry,
                SpecialRegionPlacementCollisionPlan collision,
                SpecialRegionFixedSlotLayerPlan layer,
                SpecialRegionRequiredResourceSafetyProof safety)
            {
                Bridge = bridge;
                Entry = entry;
                Collision = collision;
                Layer = layer;
                Safety = safety;
            }

            public SpecialRegionSiteBridge Bridge { get; }
            public SpecialRegionEntryBufferPlan Entry { get; }
            public SpecialRegionPlacementCollisionPlan Collision { get; }
            public SpecialRegionFixedSlotLayerPlan Layer { get; }
            public SpecialRegionRequiredResourceSafetyProof Safety { get; }
        }

        public static IReadOnlyList<SpecialRegionAuditArtifactInput> BuildAll()
        {
            var values = new List<SpecialRegionAuditArtifactInput>
            {
                BuildVillage(0, VillageLayoutShape.OneByOne, 5),
                BuildVillage(1, VillageLayoutShape.OneByTwo, 5),
                BuildVillage(2, VillageLayoutShape.TwoByOne, 6),
            };

            var coreOrder = 3;
            foreach (var definition in CoreResourceRegionStarterCatalog.Entries.OrderBy(value => value.RegionId))
                values.Add(BuildCore(coreOrder++, definition));

            var landmarkOrder = 6;
            foreach (var definition in SpecialLandmarkRegionStarterCatalog.Entries.OrderBy(value => value.RegionId))
                values.Add(BuildLandmark(landmarkOrder++, definition));
            return new ReadOnlyCollection<SpecialRegionAuditArtifactInput>(values);
        }

        private static SpecialRegionAuditArtifactInput BuildVillage(
            int order,
            VillageLayoutShape shape,
            int facilityCount)
        {
            Dimensions(shape, out var width, out var height);
            var shapeToken = shape == VillageLayoutShape.OneByOne ? "1X1" :
                shape == VillageLayoutShape.OneByTwo ? "1X2" : "2X1";
            var regionId = new SpecialRegionId("SR_MAP13_08_VILLAGE_" + shapeToken);
            var reservationId = new SiteReservationId("SITE_MAP13_08_VILLAGE_" + shapeToken);
            var facilities = FacilityPositions(shape, facilityCount).ToArray();
            var slots = facilities.Select((coordinate, index) =>
            {
                var id = new SpecialRegionSlotId("SR_SLOT_MAP13_08_VILLAGE_" + shapeToken + "_" + index);
                return new SlotSpec(id, SpecialRegionSlotKind.Facility, coordinate, index < 2,
                    SpecialPersistenceScope.Slot,
                    SpecialPersistenceKey.ForSlot(regionId, SpecialPersistenceScope.Slot, id),
                    "VILLAGE_OCCUPANT_" + index);
            }).ToArray();
            var parts = BuildPlacedParts(
                regionId, SpecialRegionKind.Village, reservationId, SiteReservationKind.Village,
                SpecialRegionPlacementOwnerKind.Village, width, height, slots,
                shape == VillageLayoutShape.OneByTwo ? new LocalTileCoord(24, 0) : new LocalTileCoord(0, 16),
                shape == VillageLayoutShape.OneByTwo ? SiteEntrySide.D : SiteEntrySide.L,
                shape == VillageLayoutShape.OneByTwo ? new LocalTileCoord(24, 63) : new LocalTileCoord(width * WorldGenConstants.SectorWidthTiles - 1, 16),
                shape == VillageLayoutShape.OneByTwo ? SiteEntrySide.U : SiteEntrySide.R,
                shape == VillageLayoutShape.OneByTwo ? new LocalTileCoord(23, 6) : new LocalTileCoord(6, 15));

            var road = Road(shape).Select((value, index) => new VillageRoadCell(index, value)).ToArray();
            var definitions = new List<VillageFacilityDefinition>();
            var witnesses = new List<VillageFacilityAccessWitness>();
            for (var index = 0; index < facilities.Length; index++)
            {
                var kind = index == 0 ? VillageFacilityKind.Kitchen :
                    index == 1 ? VillageFacilityKind.Repair : VillageFacilityKind.Optional;
                var requirement = index < 2 ? VillageFacilityRequirement.Required : VillageFacilityRequirement.Optional;
                var definitionId = index == 0 ? "VILLAGE_FACILITY_KITCHEN" :
                    index == 1 ? "VILLAGE_FACILITY_REPAIR" : "VILLAGE_FACILITY_OPTIONAL_" + (index - 2);
                var door = Door(shape, facilities[index]);
                definitions.Add(new VillageFacilityDefinition(
                    definitionId, kind, requirement, slots[index].Id, slots[index].OccupantId, door));
                witnesses.Add(new VillageFacilityAccessWitness(
                    "VILLAGE_ACCESS_" + shapeToken + "_" + index,
                    definitionId, new[] { door, RoadNeighbor(shape, facilities[index]) }));
            }

            var shellDefinition = new VillageShellDefinition(
                new VillageLayoutId("VILLAGE_LAYOUT_MAP13_08_" + shapeToken), shape,
                road, definitions, witnesses, "MAP13_08 reference fixture");
            var shellResult = VillageShellFacilityCompiler.Compile(new VillageShellCompileRequest(
                parts.Bridge, parts.Bridge.CanonicalDigest,
                parts.Entry, parts.Entry.CanonicalDigest,
                parts.Layer, parts.Layer.CanonicalDigest,
                shellDefinition));
            Require(shellResult.Success, "Village shell", shellResult.Errors);
            var shell = shellResult.Plan;

            var facilityIds = shell.FacilityBindings.Select(value => value.Definition.DefinitionId).ToArray();
            var npc = new[]
            {
                new VillageNpcMarkerDefinition("VILLAGE_NPC_0", facilityIds[0]),
                new VillageNpcMarkerDefinition("VILLAGE_NPC_1", facilityIds[1]),
                new VillageNpcMarkerDefinition("VILLAGE_NPC_2", facilityIds[2]),
            };
            var inventory = new[]
            {
                new VillageInventoryMarkerDefinition("VILLAGE_INVENTORY_0", facilityIds[0]),
                new VillageInventoryMarkerDefinition("VILLAGE_INVENTORY_1", facilityIds[1]),
            };
            var doors = shell.FacilityBindings.Select((binding, index) => new VillageDoorMarkerDefinition(
                "VILLAGE_DOOR_" + index, binding.Definition.DefinitionId, binding.Door.RegionTile)).ToArray();
            var variants = new[]
            {
                VillageStateKind.Normal, VillageStateKind.Friendly,
                VillageStateKind.IndividualHostile, VillageStateKind.AllHostile,
                VillageStateKind.Evacuation,
            };
            var markerSet = new VillageStateMarkerSetDefinition(
                npc, inventory, doors, "VILLAGE_NPC_1", variants, "MAP13_08 reference fixture");
            var stateResult = VillageStateVariantCompiler.Compile(new VillageStateVariantCompileRequest(
                SpecialRegionKind.Village, shell, shell.CanonicalDigest, markerSet));
            Require(stateResult.Success, "Village states", stateResult.Errors);
            var stateSet = stateResult.VariantSet;

            var routes = new List<SpecialRegionAuditRoute>
            {
                new SpecialRegionAuditRoute("VILLAGE_ROAD_" + shapeToken, "Low",
                    new[] { "Entry", "CentralRoad", "Return" }, true, shell.RoadAccess.IsBidirectional, false),
            };
            routes.AddRange(shell.FacilityBindings.Select(value => new SpecialRegionAuditRoute(
                value.Definition.DefinitionId, "FacilityAccess",
                new[] { "Entry", "CentralRoad", value.Definition.DefinitionId + "/Door", "Access", "CentralRoad", "Return" },
                value.ToolRequirementCount == 0,
                value.AccessCells.Count > 0 && value.AccessCells.Count == value.ReverseAccessCells.Count,
                false)));

            var seams = CountRoadSeams(shell.RoadCells);
            var metrics = new SpecialRegionAuditMetrics(
                shell.RegionId == regionId && shell.Shape == shape && stateSet.RegionId == regionId,
                shell.BridgeDigest == parts.Bridge.CanonicalDigest && shell.EntryBufferDigest == parts.Entry.CanonicalDigest &&
                shell.FixedSlotDigest == parts.Layer.CanonicalDigest && stateSet.VillageShellDigest == shell.CanonicalDigest,
                shell.WidthTiles == width * WorldGenConstants.SectorWidthTiles &&
                shell.HeightTiles == height * WorldGenConstants.SectorHeightTiles,
                parts.Bridge.SectorBindings.Count,
                seams,
                parts.Bridge.SectorBindings.Count == width * height,
                HasBufferProof(parts.Entry),
                parts.Collision.RejectedOwnerIds.Count == 0,
                CountLayerOverlap(parts.Layer),
                parts.Layer.ReplaceableSlots.All(value => !string.IsNullOrEmpty(value.PersistenceKey.Value)),
                routes.All(value => value.Ordered),
                routes.Sum(value => value.MandatoryNoTool ? 0 : 1),
                0,
                stateSet.Variants.Count == 5 && stateSet.Variants.All(value => value.VillageShellDigest == shell.CanonicalDigest),
                true,
                0,
                0,
                1, 1, 1, 1,
                shell.WorldMutationCount + shell.TileMutationCount + stateSet.WorldMutationCount + stateSet.TileMutationCount,
                !string.IsNullOrEmpty(shell.CanonicalDigest) && !string.IsNullOrEmpty(stateSet.CanonicalDigest));

            return new SpecialRegionAuditArtifactInput(
                order, regionId.Value, SpecialRegionAuditFamily.Village,
                SpecialRegionAuditBinding.ReferenceFixture, SpecialRegionKind.Village,
                shape.ToString(), width, height, shell.WidthTiles, shell.HeightTiles, width * height,
                parts.Layer.FixedCollision.Count, parts.Layer.FixedAccess.Count,
                new[] { SpecialRegionSlotKind.Facility, SpecialRegionSlotKind.Npc }, routes,
                stateSet.Variants.Count, 0, 0,
                parts.Layer.ReplaceableSlots.Select(value => value.PersistenceKey).Distinct().Count(), 0,
                shell.RoadDigest, shell.CanonicalDigest, stateSet.CanonicalDigest,
                metrics, VillageTokens(shell, stateSet, parts));
        }

        private static SpecialRegionAuditArtifactInput BuildCore(
            int order,
            CoreResourceRegionDefinition definition)
        {
            var reward = definition.RequiredReward;
            var rewardNode = definition.Nodes.Single(value => value.NodeId == reward.NodeId);
            var slots = new[]
            {
                new SlotSpec(reward.SlotId, SpecialRegionSlotKind.Reward, rewardNode.Coordinate, true,
                    reward.PersistenceScope, reward.PersistenceKey, reward.RewardId),
            };
            var token = definition.Resource.ToString().ToUpperInvariant();
            var parts = BuildPlacedParts(
                definition.RegionId, SpecialRegionKind.CoreResource,
                new SiteReservationId("RES_MAP13_08_" + token), SiteReservationKind.CoreResource,
                SpecialRegionPlacementOwnerKind.CoreResource, 1, 1, slots,
                new LocalTileCoord(0, 1), SiteEntrySide.L,
                new LocalTileCoord(47, 1), SiteEntrySide.R,
                new LocalTileCoord(42, 30));
            var compile = CoreResourceRegionCompiler.Compile(new CoreResourceRegionCompileRequest(
                definition,
                parts.Bridge, parts.Bridge.CanonicalDigest,
                parts.Entry, parts.Entry.CanonicalDigest,
                parts.Collision, parts.Collision.CanonicalDigest,
                parts.Layer, parts.Layer.CanonicalDigest,
                parts.Safety, parts.Safety.CanonicalDigest));
            Require(compile.Succeeded, "Core resource", compile.Errors);
            var plan = compile.Plan;

            var routes = new List<SpecialRegionAuditRoute>
            {
                Route(plan.LowWitness, true, false),
                Route(plan.HighWitness, true, false),
            };
            routes.AddRange(plan.RecoveryWitnesses.Select(value => Route(value, true, true)));
            var slotKinds = new List<SpecialRegionSlotKind> { SpecialRegionSlotKind.Reward, SpecialRegionSlotKind.Event };
            if (definition.Nodes.Any(value => value.MarkerKind == CoreResourceMarkerKind.EnemyCue))
                slotKinds.Add(SpecialRegionSlotKind.Enemy);
            var metrics = new SpecialRegionAuditMetrics(
                plan.RegionId == definition.RegionId && plan.Resource == definition.Resource,
                plan.BridgeDigest == parts.Bridge.CanonicalDigest && plan.EntryBufferDigest == parts.Entry.CanonicalDigest &&
                plan.CollisionDigest == parts.Collision.CanonicalDigest && plan.FixedSlotLayerDigest == parts.Layer.CanonicalDigest &&
                plan.SafetyProofDigest == parts.Safety.CanonicalDigest,
                plan.ReservedWidth == 1 && plan.ReservedHeight == 1,
                parts.Bridge.SectorBindings.Count, 0,
                parts.Bridge.RegionKind == SpecialRegionKind.CoreResource,
                HasBufferProof(parts.Entry), parts.Collision.RejectedOwnerIds.Count == 0,
                CountLayerOverlap(parts.Layer),
                parts.Safety.IsSafe && parts.Safety.PersistenceKey == reward.PersistenceKey,
                routes.All(value => value.Ordered), plan.MandatoryToolDependencyCount,
                plan.RecoveryWitnesses.Count == definition.Recoveries.Count ? 0 : 1,
                true, plan.RecoveryWitnesses.Count == definition.Recoveries.Count,
                plan.PermanentLossCount, plan.DuplicateRewardRiskCount,
                1, 1, 1, 1,
                plan.WorldMutationCount + plan.TileMutationCount + plan.InventoryMutationCount +
                plan.RewardGrantCount + plan.SaveWriteCount + plan.PathfindingCount,
                !string.IsNullOrEmpty(plan.CanonicalDigest));
            return new SpecialRegionAuditArtifactInput(
                order, plan.RegionId.Value, SpecialRegionAuditFamily.CoreResource,
                SpecialRegionAuditBinding.ReferenceFixture, plan.RegionKind,
                plan.Resource + " / " + plan.Mechanism,
                1, 1, plan.DesignWidth, plan.DesignHeight, plan.ActiveDesignChunks.Count,
                parts.Layer.FixedCollision.Count, parts.Layer.FixedAccess.Count,
                slotKinds, routes, parts.Safety.Evidence.Count, plan.Recoveries.Count,
                parts.Safety.Evidence.Count, 1, 1,
                CoreResourceRegionCanonicalDigest.ComputeDefinition(definition),
                plan.GraphDigest, plan.CanonicalDigest,
                metrics, CoreTokens(plan, parts));
        }

        private static SpecialRegionAuditArtifactInput BuildLandmark(
            int order,
            SpecialLandmarkRegionDefinition definition)
        {
            PlacedParts parts = null;
            SpecialLandmarkCompileRequest request;
            if (definition.Binding == SpecialLandmarkBindingKind.DeferredOptionalLocal)
            {
                request = new SpecialLandmarkCompileRequest(
                    definition, null, string.Empty, null, string.Empty, null, string.Empty,
                    null, string.Empty, null, string.Empty, null);
            }
            else
            {
                var slots = new List<SlotSpec>();
                if (definition.RequiredReward != null)
                {
                    var reward = definition.RequiredReward;
                    var coordinate = definition.Nodes.Single(value => value.NodeId == reward.NodeId).Coordinate;
                    slots.Add(new SlotSpec(reward.SlotId, SpecialRegionSlotKind.Reward, coordinate, true,
                        SpecialPersistenceScope.Reward, reward.PersistenceKey, reward.RewardId));
                }
                else
                {
                    var eventId = new SpecialRegionSlotId("SR_SLOT_BOSS_ENCOUNTER");
                    slots.Add(new SlotSpec(eventId, SpecialRegionSlotKind.Event, new LocalTileCoord(37, 15), true,
                        SpecialPersistenceScope.Encounter,
                        SpecialPersistenceKey.ForSlot(definition.RegionId, SpecialPersistenceScope.Encounter, eventId),
                        "BOSS_ENCOUNTER"));
                }
                var reservationKind = definition.RegionKind == SpecialRegionKind.Forge
                    ? SiteReservationKind.Forge : SiteReservationKind.Boss;
                var ownerKind = definition.RegionKind == SpecialRegionKind.Forge
                    ? SpecialRegionPlacementOwnerKind.Forge : SpecialRegionPlacementOwnerKind.Boss;
                parts = BuildPlacedParts(
                    definition.RegionId, definition.RegionKind,
                    new SiteReservationId("RES_MAP13_08_" + definition.Landmark.ToString().ToUpperInvariant()),
                    reservationKind, ownerKind, 1, 1, slots,
                    new LocalTileCoord(0, 1), SiteEntrySide.L,
                    new LocalTileCoord(47, 1), SiteEntrySide.R,
                    new LocalTileCoord(42, 30));
                request = new SpecialLandmarkCompileRequest(
                    definition,
                    parts.Bridge, parts.Bridge.CanonicalDigest,
                    parts.Entry, parts.Entry.CanonicalDigest,
                    parts.Collision, parts.Collision.CanonicalDigest,
                    parts.Layer, parts.Layer.CanonicalDigest,
                    parts.Safety, parts.Safety == null ? string.Empty : parts.Safety.CanonicalDigest,
                    CoreResourceRegionStarterCatalog.Entries);
            }

            var compile = SpecialLandmarkRegionCompiler.Compile(request);
            Require(compile.Succeeded, "Landmark", compile.Errors);
            var plan = compile.Plan;
            var deferred = plan.Binding == SpecialLandmarkBindingKind.DeferredOptionalLocal;
            var routes = plan.Witnesses.Select(value => new SpecialRegionAuditRoute(
                value.RouteId, value.Kind.ToString(), value.NodeIds, true,
                value.NodeIds.Count >= 2, value.Kind == SpecialLandmarkRouteKind.Recovery)).ToArray();
            var slotKinds = LandmarkSlotKinds(definition).ToArray();
            var safetyCount = parts == null || parts.Safety == null ? 0 : parts.Safety.Evidence.Count;
            var metrics = new SpecialRegionAuditMetrics(
                plan.RegionId == definition.RegionId && plan.Landmark == definition.Landmark,
                plan.DesignDigest == SpecialLandmarkCanonicalDigest.ComputeDesign(plan) &&
                plan.ShellDigest == SpecialLandmarkCanonicalDigest.ComputeShell(plan) &&
                plan.StateDigest == SpecialLandmarkCanonicalDigest.ComputeState(plan) &&
                plan.MarkerDigest == SpecialLandmarkCanonicalDigest.ComputeMarker(plan),
                deferred ? plan.ReservedWidth == 0 && plan.ReservedHeight == 0 :
                    plan.ReservedWidth == 1 && plan.ReservedHeight == 1,
                deferred ? 0 : parts.Bridge.SectorBindings.Count,
                0,
                deferred || (parts.Bridge.RegionId == plan.RegionId && parts.Layer.RegionId == plan.RegionId),
                deferred || HasBufferProof(parts.Entry),
                deferred || parts.Collision.RejectedOwnerIds.Count == 0,
                deferred ? 0 : CountLayerOverlap(parts.Layer),
                definition.RequiredReward == null ||
                    (parts != null && parts.Safety != null && parts.Safety.PersistenceKey == definition.RequiredReward.PersistenceKey),
                routes.All(value => value.Ordered), plan.MandatoryOptionalDependencyCount,
                plan.Resets.Count(value => string.IsNullOrEmpty(value.RecoveryNodeId)),
                !definition.StateMutatesShell,
                plan.Resets.Count == definition.Resets.Count,
                plan.ForgePermanentLossCount, plan.DuplicateBenefitRiskCount,
                plan.WorldOriginCount, plan.ReservationClaimCount, plan.BridgeClaimCount, plan.PlacedOwnershipClaimCount,
                plan.WorldMutationCount + plan.TileMutationCount + plan.InventoryMutationCount +
                plan.RewardGrantCount + plan.SaveWriteCount + plan.PlacementSolverCount + plan.GameplayExecutionCount,
                !string.IsNullOrEmpty(plan.CanonicalDigest));
            return new SpecialRegionAuditArtifactInput(
                order, plan.RegionId.Value, SpecialRegionAuditFamily.Landmark,
                deferred ? SpecialRegionAuditBinding.DeferredToMAP14 : SpecialRegionAuditBinding.ReferenceFixture,
                plan.RegionKind, plan.Landmark + " / " + plan.Theme,
                deferred ? 0 : 1, deferred ? 0 : 1,
                plan.DesignWidth, plan.DesignHeight, plan.ActiveDesignChunks.Count,
                deferred ? 0 : parts.Layer.FixedCollision.Count,
                deferred ? 0 : parts.Layer.FixedAccess.Count,
                slotKinds, routes, plan.States.Count, plan.Resets.Count,
                safetyCount,
                definition.Markers.Select(value => value.PersistenceKey.Value)
                    .Concat(definition.RequiredReward == null ? Array.Empty<string>() :
                        new[] { definition.RequiredReward.PersistenceKey.Value })
                    .Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal).Count(),
                definition.RequiredReward == null ? 0 : 1,
                definition.CanonicalDigest, plan.ShellDigest, plan.CanonicalDigest,
                metrics, LandmarkTokens(plan, parts));
        }

        private static PlacedParts BuildPlacedParts(
            SpecialRegionId regionId,
            SpecialRegionKind regionKind,
            SiteReservationId reservationId,
            SiteReservationKind reservationKind,
            SpecialRegionPlacementOwnerKind ownerKind,
            int width,
            int height,
            IEnumerable<SlotSpec> slotSpecs,
            LocalTileCoord entryRegionTile,
            SiteEntrySide entrySide,
            LocalTileCoord returnRegionTile,
            SiteEntrySide returnSide,
            LocalTileCoord fixedRegionTile)
        {
            var origin = new SectorCoord(5, 5);
            var offsets = RectangleOffsets(width, height).ToArray();
            var sectors = offsets.Select((offset, index) => new SpecialRegionSiteSectorBinding(
                offset, offset, new SectorCoord(origin.X + offset.X, origin.Y + offset.Y),
                index, "MAP13_08_SECTOR_" + index)).ToArray();
            var specifications = (slotSpecs ?? Array.Empty<SlotSpec>()).ToArray();
            var sources = specifications.Select(value =>
            {
                var placed = Place(origin, value.Coordinate);
                return new SpecialRegionSiteSlotBinding(
                    value.Id, value.Kind, value.Required, value.Scope, value.Key,
                    new SpecialRegionAuthoredCoordinate(placed.SectorOffset, placed.LocalTile), placed);
            }).ToArray();

            var entryPort = PortSource(regionId, origin, "MAP13_08_PORT_ENTRY", "MAP13_08_SOCKET_ENTRY",
                new SpecialRegionSlotId("SR_SLOT_MAP13_08_ENTRY"), SpecialRegionSlotKind.Entry,
                entryRegionTile, entrySide);
            var returnPort = PortSource(regionId, origin, "MAP13_08_PORT_RETURN", "MAP13_08_SOCKET_RETURN",
                new SpecialRegionSlotId("SR_SLOT_MAP13_08_RETURN"), SpecialRegionSlotKind.Return,
                returnRegionTile, returnSide);
            var fixedPlaced = Place(origin, fixedRegionTile);
            var fixedSource = new SpecialRegionSiteFixedShellBinding(
                "MAP13_08_FIXED_SHELL", new SpecialRegionAuthoredCoordinate(
                    fixedPlaced.SectorOffset, fixedPlaced.LocalTile), fixedPlaced);
            var bridge = CreateInternal<SpecialRegionSiteBridge>(
                regionId, regionKind, reservationId, reservationKind, "MAP13_08_REFERENCE_FIXTURE",
                origin, width, height, SiteFootprintTransform.R0,
                offsets, offsets, sectors, new[] { fixedSource }, sources,
                new[] { entryPort, returnPort },
                "MAP13_08_RESERVATION_IDENTITY_" + regionId.Value,
                "MAP13_08_CONTRACT_" + regionId.Value);
            SetCanonicalDigest(bridge, SpecialRegionSiteBridgeCanonicalDigest.Compute(bridge));

            var entryAnchor = Anchor(reservationId, entryPort, entrySide, false);
            var returnAnchor = Anchor(reservationId, returnPort, returnSide, true);
            var entryBinding = CreateInternal<SpecialRegionEntryPortBinding>(entryPort, entryAnchor, new[] { 1, 2, 3 });
            var returnBinding = CreateInternal<SpecialRegionEntryPortBinding>(returnPort, returnAnchor, new[] { 1, 2, 3 });
            var entryApron = Apron(entryPort.PortId, entryPort.Placed);
            var returnApron = Apron(returnPort.PortId, returnPort.Placed);
            var witness = CreateInternal<SpecialRegionBidirectionalWitness>(
                new[] { "BeforeQuiet", "EntrySocket", "EntryApron", "RegionInterior" },
                new[] { "RegionInterior", "ReturnApron", "ReturnSocket", "AfterQuiet" },
                new[] { 1, 2, 3 }, new[] { 1, 2, 3 });
            var quiet = new[]
            {
                CreateInternal<SpecialRegionQuietChunkBinding>(
                    "MAP13_08_BEFORE", SpecialRegionQuietChunkRole.Before,
                    "MAP13_08_QUIET", "MAP13_08_QUIET_DIGEST",
                    new ClusterChunkCoord(0, 0), entryPort.AnchorExteriorSector,
                    new ClusterChunkCoord(3, 0), null),
                CreateInternal<SpecialRegionQuietChunkBinding>(
                    "MAP13_08_AFTER", SpecialRegionQuietChunkRole.After,
                    "MAP13_08_QUIET", "MAP13_08_QUIET_DIGEST",
                    new ClusterChunkCoord(1, 0), returnPort.AnchorExteriorSector,
                    new ClusterChunkCoord(0, 0), null),
            };
            var entry = CreateInternal<SpecialRegionEntryBufferPlan>(
                bridge, entryBinding, returnBinding,
                new[] { entryApron, returnApron }, quiet, witness);
            SetCanonicalDigest(entry, SpecialRegionEntryBufferCanonicalDigest.Compute(entry));

            var fixedCollision = CreateInternal<SpecialRegionFixedCollisionCell>(
                fixedSource.ShellId, fixedSource.Source, fixedSource.Placed);
            var fixedAccess = new[]
            {
                CreateInternal<SpecialRegionFixedAccessBinding>(
                    SpecialRegionFixedAccessKind.Entry, entryPort.PortId, entryPort.SlotId,
                    entryPort.Kind, entryPort.AccessClass,
                    new SpecialRegionTileCoordinate(entryPort.Placed.WorldSector, entryPort.Placed.LocalTile),
                    entryPort.Source, entryPort.Placed, true),
                CreateInternal<SpecialRegionFixedAccessBinding>(
                    SpecialRegionFixedAccessKind.Return, returnPort.PortId, returnPort.SlotId,
                    returnPort.Kind, returnPort.AccessClass,
                    new SpecialRegionTileCoordinate(returnPort.Placed.WorldSector, returnPort.Placed.LocalTile),
                    returnPort.Source, returnPort.Placed, true),
            };
            var replaceable = sources.Select(source =>
            {
                var specification = specifications.Single(value => value.Id == source.SlotId);
                return CreateInternal<SpecialRegionReplaceableSlotBinding>(
                    source, SpecialRegionSlotReplacementIntent.Assign(
                        source.SlotId, source.Kind, specification.OccupantId));
            }).ToArray();

            var fixedCells = new[] { fixedCollision.Coordinate };
            var accessCells = fixedAccess.Select(value => value.Coordinate).ToArray();
            var collisionResult = SpecialRegionPlacementCollisionCompiler.Compile(
                new SpecialRegionPlacementCollisionCompileRequest(new[]
                {
                    new SpecialRegionOccupancyClaim("MAP13_08_FIXED_COLLISION_" + regionId.Value,
                        ownerKind, fixedCells, true),
                    new SpecialRegionOccupancyClaim("MAP13_08_FIXED_ACCESS_" + regionId.Value,
                        ownerKind, accessCells, true),
                }));
            Require(collisionResult.Succeeded, "Collision", collisionResult.Errors);
            var layer = CreateInternal<SpecialRegionFixedSlotLayerPlan>(
                regionId, regionKind, reservationId, bridge.ContractDigest,
                bridge.CanonicalDigest, entry.CanonicalDigest, collisionResult.CanonicalDigest,
                new[] { fixedCollision }, fixedAccess, replaceable, collisionResult.Plan.Claims);

            SpecialRegionRequiredResourceSafetyProof safety = null;
            var reward = replaceable.SingleOrDefault(value =>
                value.Kind == SpecialRegionSlotKind.Reward && value.Required);
            if (reward != null)
            {
                var evidence = PersistenceEvidence(layer, reward).ToArray();
                var safetyResult = SpecialRegionPersistenceSafetyCompiler.Compile(
                    new SpecialRegionPersistenceSafetyCompileRequest(
                        layer, layer.CanonicalDigest, evidence));
                Require(safetyResult.Succeeded, "Persistence safety", safetyResult.Errors);
                safety = safetyResult.Proofs.Single();
            }
            return new PlacedParts(bridge, entry, collisionResult.Plan, layer, safety);
        }

        private static IEnumerable<SpecialRegionPersistenceCheckpointEvidence> PersistenceEvidence(
            SpecialRegionFixedSlotLayerPlan layer,
            SpecialRegionReplaceableSlotBinding reward)
        {
            var values = new[]
            {
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Initial, SpecialRegionRequiredResourceState.Available),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Active, SpecialRegionRequiredResourceState.TemporarilyUnavailable),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Interrupted, SpecialRegionRequiredResourceState.Available),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Failed, SpecialRegionRequiredResourceState.Available),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Regenerated, SpecialRegionRequiredResourceState.Available),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Claimed, SpecialRegionRequiredResourceState.Claimed),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Revisited, SpecialRegionRequiredResourceState.Claimed),
            };
            return values.Select(value => new SpecialRegionPersistenceCheckpointEvidence(
                layer.RegionId, reward.SlotId, reward.PersistenceKey, reward.PersistenceScope,
                value.Item1, value.Item2, reward.IdentityDigest));
        }

        private static SpecialRegionSitePortBinding PortSource(
            SpecialRegionId regionId,
            SectorCoord origin,
            string portId,
            string socketId,
            SpecialRegionSlotId slotId,
            SpecialRegionSlotKind kind,
            LocalTileCoord regionTile,
            SiteEntrySide side)
        {
            var placed = Place(origin, regionTile, side);
            var exterior = new SectorCoord(
                placed.WorldSector.X + SiteReservationTokenCodec.GetDeltaX(side),
                placed.WorldSector.Y + SiteReservationTokenCodec.GetDeltaY(side));
            return new SpecialRegionSitePortBinding(
                portId, slotId, kind, AccessClass.MandatoryNoTool,
                SpecialPersistenceKey.ForRegion(regionId), socketId, exterior,
                new SpecialRegionAuthoredCoordinate(placed.SectorOffset, placed.LocalTile, side), placed);
        }

        private static SiteEntryAnchor Anchor(
            SiteReservationId reservationId,
            SpecialRegionSitePortBinding port,
            SiteEntrySide side,
            bool returnRequired)
            => new SiteEntryAnchor(
                reservationId, port.EntrySocketId, port.Placed.WorldSector,
                side, new[] { 1, 2, 3 }, true, returnRequired);

        private static SpecialRegionEntryApron Apron(string portId, SpecialRegionPlacedCoordinate placed)
        {
            var coordinate = new SpecialRegionTileCoordinate(placed.WorldSector, placed.LocalTile);
            return new SpecialRegionEntryApron(
                portId, placed.WorldSector, placed.LocalTile, 1, 1, new[] { coordinate });
        }

        private static SpecialRegionPlacedCoordinate Place(
            SectorCoord origin,
            LocalTileCoord regionTile,
            SiteEntrySide? side = null)
        {
            var offset = new SpecialRegionSectorOffset(
                regionTile.X / WorldGenConstants.SectorWidthTiles,
                regionTile.Y / WorldGenConstants.SectorHeightTiles);
            var local = new LocalTileCoord(
                regionTile.X % WorldGenConstants.SectorWidthTiles,
                regionTile.Y % WorldGenConstants.SectorHeightTiles);
            return new SpecialRegionPlacedCoordinate(
                offset, new SectorCoord(origin.X + offset.X, origin.Y + offset.Y),
                local, regionTile, side);
        }

        private static IEnumerable<SpecialRegionSectorOffset> RectangleOffsets(int width, int height)
        {
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    yield return new SpecialRegionSectorOffset(x, y);
        }

        private static IEnumerable<LocalTileCoord> Road(VillageLayoutShape shape)
        {
            if (shape == VillageLayoutShape.OneByTwo)
            {
                for (var y = 0; y < 64; y++) yield return new LocalTileCoord(24, y);
                yield break;
            }
            var width = shape == VillageLayoutShape.TwoByOne ? 96 : 48;
            for (var x = 0; x < width; x++) yield return new LocalTileCoord(x, 16);
        }

        private static IEnumerable<LocalTileCoord> FacilityPositions(VillageLayoutShape shape, int count)
        {
            var values = count == 6 ? new[] { 5, 12, 20, 28, 36, 43 } : new[] { 5, 12, 20, 28, 36 };
            if (shape == VillageLayoutShape.TwoByOne)
                values = count == 6 ? new[] { 5, 20, 35, 55, 72, 88 } : new[] { 5, 20, 40, 60, 80 };
            foreach (var value in values)
                yield return shape == VillageLayoutShape.OneByTwo
                    ? new LocalTileCoord(22, value) : new LocalTileCoord(value, 14);
        }

        private static LocalTileCoord Door(VillageLayoutShape shape, LocalTileCoord slot)
            => shape == VillageLayoutShape.OneByTwo
                ? new LocalTileCoord(slot.X + 1, slot.Y) : new LocalTileCoord(slot.X, slot.Y + 1);

        private static LocalTileCoord RoadNeighbor(VillageLayoutShape shape, LocalTileCoord slot)
            => shape == VillageLayoutShape.OneByTwo
                ? new LocalTileCoord(slot.X + 2, slot.Y) : new LocalTileCoord(slot.X, slot.Y + 2);

        private static void Dimensions(VillageLayoutShape shape, out int width, out int height)
        {
            width = shape == VillageLayoutShape.TwoByOne ? 2 : 1;
            height = shape == VillageLayoutShape.OneByTwo ? 2 : 1;
        }

        private static IEnumerable<SpecialRegionAuditToken> VillageTokens(
            VillageShellPlan shell,
            VillageStateVariantSet states,
            PlacedParts parts)
        {
            var values = new List<SpecialRegionAuditToken>();
            foreach (var sector in parts.Bridge.SectorBindings)
                values.Add(new SpecialRegionAuditToken(
                    SpecialRegionAuditTokenKind.DesignChunk, "SECTOR_" + sector.PlacedOffset,
                    sector.PlacedOffset.X * WorldGenConstants.SectorWidthTiles + 24,
                    sector.PlacedOffset.Y * WorldGenConstants.SectorHeightTiles + 16,
                    "active village sector"));
            if (shell.WidthTiles > WorldGenConstants.SectorWidthTiles)
                values.Add(new SpecialRegionAuditToken(SpecialRegionAuditTokenKind.SectorSeam,
                    "SEAM_X", WorldGenConstants.SectorWidthTiles, shell.HeightTiles / 2, "vertical sector seam"));
            if (shell.HeightTiles > WorldGenConstants.SectorHeightTiles)
                values.Add(new SpecialRegionAuditToken(SpecialRegionAuditTokenKind.SectorSeam,
                    "SEAM_Y", shell.WidthTiles / 2, WorldGenConstants.SectorHeightTiles, "horizontal sector seam"));
            foreach (var cell in shell.RoadCells.Where((value, index) => index % 4 == 0))
                values.Add(new SpecialRegionAuditToken(
                    SpecialRegionAuditTokenKind.LowRoute, "ROAD_" + cell.Order,
                    cell.RegionTile.X, cell.RegionTile.Y, "central road"));
            foreach (var facility in shell.FacilityBindings)
            {
                values.Add(new SpecialRegionAuditToken(
                    SpecialRegionAuditTokenKind.Facility, facility.Definition.DefinitionId,
                    facility.Slot.Placed.RegionTile.X, facility.Slot.Placed.RegionTile.Y,
                    facility.Definition.Kind.ToString()));
                values.Add(new SpecialRegionAuditToken(
                    SpecialRegionAuditTokenKind.FixedAccess, facility.Witness.WitnessId,
                    facility.Door.RegionTile.X, facility.Door.RegionTile.Y, "door/access"));
            }
            values.AddRange(PartsTokens(parts));
            for (var index = 0; index < states.Variants.Count; index++)
                values.Add(new SpecialRegionAuditToken(
                    SpecialRegionAuditTokenKind.State, states.Variants[index].StateKind.ToString(),
                    2 + index * 4, 2, "village state"));
            return values;
        }

        private static IEnumerable<SpecialRegionAuditToken> CoreTokens(
            CoreResourceRegionPlan plan,
            PlacedParts parts)
        {
            var values = new List<SpecialRegionAuditToken>();
            values.AddRange(plan.ActiveDesignChunks.Select(value => new SpecialRegionAuditToken(
                SpecialRegionAuditTokenKind.DesignChunk, "CHUNK_" + value,
                value.X * plan.DesignChunkWidth + plan.DesignChunkWidth / 2,
                value.Y * plan.DesignChunkHeight + plan.DesignChunkHeight / 2,
                "active design chunk")));
            foreach (var node in plan.Nodes)
            {
                var kind = node.Role == CoreResourceNodeRole.Entry ? SpecialRegionAuditTokenKind.Entry :
                    node.Role == CoreResourceNodeRole.Return ? SpecialRegionAuditTokenKind.Return :
                    node.Role == CoreResourceNodeRole.RequiredReward ? SpecialRegionAuditTokenKind.Reward :
                    node.Role == CoreResourceNodeRole.Failure || node.Role == CoreResourceNodeRole.RecoveryJoin
                        ? SpecialRegionAuditTokenKind.RecoveryRoute :
                    node.MarkerKind == CoreResourceMarkerKind.EnemyCue ? SpecialRegionAuditTokenKind.Enemy :
                    node.Role == CoreResourceNodeRole.EnvironmentTrigger || node.Role == CoreResourceNodeRole.MasteryTrigger
                        ? SpecialRegionAuditTokenKind.Event : SpecialRegionAuditTokenKind.LowRoute;
                values.Add(new SpecialRegionAuditToken(kind, node.NodeId,
                    node.Coordinate.X, node.Coordinate.Y, node.Role + "/" + node.MarkerKind));
            }
            values.AddRange(plan.Recoveries.Select((value, index) => new SpecialRegionAuditToken(
                SpecialRegionAuditTokenKind.Reset, value.RecoveryId, 2 + index * 4,
                Math.Max(0, plan.DesignHeight - 2), "failure reset")));
            values.AddRange(PartsTokens(parts));
            return values;
        }

        private static IEnumerable<SpecialRegionAuditToken> LandmarkTokens(
            SpecialLandmarkRegionPlan plan,
            PlacedParts parts)
        {
            var values = new List<SpecialRegionAuditToken>();
            values.AddRange(plan.ActiveDesignChunks.Select(value => new SpecialRegionAuditToken(
                SpecialRegionAuditTokenKind.DesignChunk, "CHUNK_" + value,
                value.X * plan.DesignChunkWidth + plan.DesignChunkWidth / 2,
                value.Y * plan.DesignChunkHeight + plan.DesignChunkHeight / 2,
                "active design chunk")));
            foreach (var node in plan.Nodes)
            {
                var kind = node.Role == SpecialLandmarkNodeRole.Entry ? SpecialRegionAuditTokenKind.Entry :
                    node.Role == SpecialLandmarkNodeRole.Return ? SpecialRegionAuditTokenKind.Return :
                    node.Role == SpecialLandmarkNodeRole.RequiredReward ? SpecialRegionAuditTokenKind.Reward :
                    node.Role == SpecialLandmarkNodeRole.Failure || node.Role == SpecialLandmarkNodeRole.RecoveryJoin
                        ? SpecialRegionAuditTokenKind.RecoveryRoute :
                    node.Role == SpecialLandmarkNodeRole.Arena ? SpecialRegionAuditTokenKind.Enemy :
                    node.Role == SpecialLandmarkNodeRole.Shop ? SpecialRegionAuditTokenKind.Npc :
                    node.Role == SpecialLandmarkNodeRole.Gate || node.Role == SpecialLandmarkNodeRole.Shrine
                        ? SpecialRegionAuditTokenKind.Event : SpecialRegionAuditTokenKind.LowRoute;
                values.Add(new SpecialRegionAuditToken(kind, node.NodeId,
                    node.Coordinate.X, node.Coordinate.Y, node.Role.ToString()));
            }
            foreach (var route in plan.Witnesses)
            {
                var tokenKind = route.Kind == SpecialLandmarkRouteKind.High
                    ? SpecialRegionAuditTokenKind.HighRoute :
                    route.Kind == SpecialLandmarkRouteKind.Recovery
                        ? SpecialRegionAuditTokenKind.RecoveryRoute : SpecialRegionAuditTokenKind.LowRoute;
                var coordinates = route.NodeIds.Select(id => plan.Nodes.Single(value => value.NodeId == id).Coordinate);
                foreach (var coordinate in coordinates)
                    values.Add(new SpecialRegionAuditToken(tokenKind, route.RouteId,
                        coordinate.X, coordinate.Y, route.Kind.ToString()));
            }
            values.AddRange(plan.States.Select((value, index) => new SpecialRegionAuditToken(
                SpecialRegionAuditTokenKind.State, value.StateId,
                2 + (index % 8) * 4, 2 + (index / 8) * 3, value.Role.ToString())));
            values.AddRange(plan.Resets.Select((value, index) => new SpecialRegionAuditToken(
                SpecialRegionAuditTokenKind.Reset, value.ResetId,
                2 + index * 4, Math.Max(0, plan.DesignHeight - 2), value.Policy.ToString())));
            if (parts != null) values.AddRange(PartsTokens(parts));
            return values;
        }

        private static IEnumerable<SpecialRegionAuditToken> PartsTokens(PlacedParts parts)
        {
            yield return new SpecialRegionAuditToken(
                SpecialRegionAuditTokenKind.Entry, parts.Entry.EntryPort.PortId,
                parts.Entry.EntryPort.Placed.RegionTile.X, parts.Entry.EntryPort.Placed.RegionTile.Y,
                "Entry / MandatoryNoTool");
            yield return new SpecialRegionAuditToken(
                SpecialRegionAuditTokenKind.Return, parts.Entry.ReturnPort.PortId,
                parts.Entry.ReturnPort.Placed.RegionTile.X, parts.Entry.ReturnPort.Placed.RegionTile.Y,
                "Return / MandatoryNoTool");
            foreach (var apron in parts.Entry.Aprons)
                yield return new SpecialRegionAuditToken(
                    SpecialRegionAuditTokenKind.Apron, apron.PortId,
                    apron.Minimum.X, apron.Minimum.Y, "protected apron");
            foreach (var buffer in parts.Entry.QuietChunks)
                yield return new SpecialRegionAuditToken(
                    SpecialRegionAuditTokenKind.Buffer, buffer.PlacementId,
                    buffer.MinimumTile.X, buffer.MinimumTile.Y, buffer.Role.ToString());
            foreach (var fixedCell in parts.Layer.FixedCollision)
                yield return new SpecialRegionAuditToken(
                    SpecialRegionAuditTokenKind.FixedCollision, fixedCell.ShellId,
                    fixedCell.Placed.RegionTile.X, fixedCell.Placed.RegionTile.Y, "immutable collision");
            foreach (var access in parts.Layer.FixedAccess)
                yield return new SpecialRegionAuditToken(
                    SpecialRegionAuditTokenKind.FixedAccess, access.PortId,
                    access.Placed.RegionTile.X, access.Placed.RegionTile.Y, access.AccessKind.ToString());
            foreach (var slot in parts.Layer.ReplaceableSlots)
            {
                var kind = slot.Kind == SpecialRegionSlotKind.Reward ? SpecialRegionAuditTokenKind.Reward :
                    slot.Kind == SpecialRegionSlotKind.Facility ? SpecialRegionAuditTokenKind.Facility :
                    slot.Kind == SpecialRegionSlotKind.Npc ? SpecialRegionAuditTokenKind.Npc :
                    slot.Kind == SpecialRegionSlotKind.Enemy ? SpecialRegionAuditTokenKind.Enemy :
                    SpecialRegionAuditTokenKind.Event;
                yield return new SpecialRegionAuditToken(kind, slot.SlotId.Value,
                    slot.Placed.RegionTile.X, slot.Placed.RegionTile.Y,
                    slot.Kind + " / " + slot.PersistenceKey.Value);
            }
        }

        private static SpecialRegionAuditRoute Route(
            CoreResourceRouteWitness witness,
            bool mandatoryNoTool,
            bool recovery)
            => new SpecialRegionAuditRoute(
                witness.RouteId, witness.Kind.ToString(), witness.NodeIds,
                mandatoryNoTool, witness.NodeIds.Count >= 2, recovery);

        private static IEnumerable<SpecialRegionSlotKind> LandmarkSlotKinds(
            SpecialLandmarkRegionDefinition definition)
        {
            if (definition.RequiredReward != null) yield return SpecialRegionSlotKind.Reward;
            if (definition.Landmark == SpecialLandmarkKind.BossSealArena)
            {
                yield return SpecialRegionSlotKind.Enemy;
                yield return SpecialRegionSlotKind.Event;
            }
            if (definition.Landmark == SpecialLandmarkKind.WanderingMerchantCave)
            {
                yield return SpecialRegionSlotKind.Npc;
                yield return SpecialRegionSlotKind.Event;
            }
            if (definition.Landmark == SpecialLandmarkKind.MaruTimeShrine)
            {
                yield return SpecialRegionSlotKind.Npc;
                yield return SpecialRegionSlotKind.Event;
            }
            if (definition.Landmark == SpecialLandmarkKind.MoonSealForge)
                yield return SpecialRegionSlotKind.Event;
        }

        private static bool HasBufferProof(SpecialRegionEntryBufferPlan entry)
            => entry != null && entry.EntryPort != null && entry.ReturnPort != null &&
               entry.Aprons.Count == 2 && entry.Witness != null && entry.Witness.IsBidirectional &&
               entry.QuietChunks.Any(value => value.Role == SpecialRegionQuietChunkRole.Before) &&
               entry.QuietChunks.Any(value => value.Role == SpecialRegionQuietChunkRole.After);

        private static int CountLayerOverlap(SpecialRegionFixedSlotLayerPlan layer)
        {
            var fixedCells = new HashSet<SpecialRegionTileCoordinate>(
                layer.FixedCollision.Select(value => value.Coordinate)
                .Concat(layer.FixedAccess.Select(value => value.Coordinate)));
            return layer.ReplaceableSlots.Count(value => fixedCells.Contains(value.Coordinate));
        }

        private static int CountRoadSeams(IEnumerable<VillageRoadCell> cells)
        {
            var ordered = cells.OrderBy(value => value.Order).ToArray();
            var count = 0;
            for (var index = 1; index < ordered.Length; index++)
            {
                var left = ordered[index - 1].RegionTile;
                var right = ordered[index].RegionTile;
                if (left.X / WorldGenConstants.SectorWidthTiles != right.X / WorldGenConstants.SectorWidthTiles ||
                    left.Y / WorldGenConstants.SectorHeightTiles != right.Y / WorldGenConstants.SectorHeightTiles)
                    count++;
            }
            return count;
        }

        private static T CreateInternal<T>(params object[] arguments)
            => (T)Activator.CreateInstance(
                typeof(T), BindingFlags.Instance | BindingFlags.NonPublic,
                null, arguments, CultureInfo.InvariantCulture);

        private static void SetCanonicalDigest(object target, string value)
        {
            var field = target.GetType().GetField(
                "<CanonicalDigest>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, "CanonicalDigest");
            field.SetValue(target, value);
        }

        private static void Require<T>(bool condition, string owner, IEnumerable<T> errors)
        {
            if (condition) return;
            throw new InvalidOperationException(owner + " reference fixture failed: " +
                                                string.Join("; ", (errors ?? Array.Empty<T>()).Select(value => value.ToString())));
        }
    }
}
