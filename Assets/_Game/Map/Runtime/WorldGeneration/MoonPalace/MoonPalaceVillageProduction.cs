using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceVillagePreconditions
    {
        public const string TaskId = "MAP21_08_COMPLETE_MOONPALACE_VILLAGE";
        public const string SourceMap13AuditDigest = "a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e";
        public const string SourceMap18ExportDigest = "358ac8cfe78eec502db049f8940ed0c71458179b89bb451680e837b0797b77b5";
        public const string SourceMap18DebugDigest = "59efb7fd30df9ec62014cadd04a111b222e7dd13e298789dbab88a661bea22ed";
        public const string SourceMap2104Digest = "d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72";
        public const string SourceMap2105Digest = "645f95b8c58757179223b8e5b219b60b5a732cb898a69f99594178c76f166a1e";
        public const string SourceMap2106Digest = "955ad4acb3e2294b3dc7c018c21472bf4ef34bbfc4075685324901179bd652ab";
        public const string SourceMap2107Digest = "0e03c7cb4780b078822297e9877d345480413a2b3b8d9ed0c3d1d455645a4a93";
        public const string StrictMap2107ResultDigest = "68170bda01e3f2df4fbf14e9e841b04be58b3b61b1b18de9af3e5e2a3a486c8b";
        public const string StrictMap2107TaskDigest = "4bee068adbf40ee2d55afaea0b35d6e3d635079de2ee6f23f9e0636b0fbf4321";
        public const string StrictMap2108HandoffDigest = "7cd8adb0e27cffad1a4187400c6e72cd47e24291663aa5fe69457096efb535e0";
    }

    public sealed class MoonPalaceVillageLayoutProfile
    {
        public const string SchemaVersion = "map21_08.village.v1";

        public MoonPalaceVillageLayoutProfile(string layoutId, string shape, int width,
            int height, int activeSectorCount, int roadCellCount, int facilityCount,
            int fixedFacilityCount, int optionalFacilityCount, int doorMarkerCount,
            int npcMarkerCount, int inventoryMarkerCount, int shopkeeperMarkerCount,
            int stateVariantCount, string facilityDigest, string roadDigest,
            string doorDigest, string stateDigest)
        {
            VillageLayoutId = Village(layoutId);
            SourceMap13Shape = MoonPalaceVillageCanonical.Required(shape);
            BoundsWidth = width;
            BoundsHeight = height;
            ActiveSectorCount = activeSectorCount;
            RoadCellCount = roadCellCount;
            FacilityCount = facilityCount;
            FixedFacilityCount = fixedFacilityCount;
            OptionalFacilityCount = optionalFacilityCount;
            DoorMarkerCount = doorMarkerCount;
            NpcMarkerCount = npcMarkerCount;
            InventoryMarkerCount = inventoryMarkerCount;
            ShopkeeperMarkerCount = shopkeeperMarkerCount;
            StateVariantCount = stateVariantCount;
            FacilityDigest = MoonPalaceVillageCanonical.Digest(facilityDigest);
            RoadDigest = MoonPalaceVillageCanonical.Digest(roadDigest);
            DoorDigest = MoonPalaceVillageCanonical.Digest(doorDigest);
            StateDigest = MoonPalaceVillageCanonical.Digest(stateDigest);
            CanonicalDigest = MoonPalaceVillageCanonical.Hash("MAP21_08_VILLAGE_PROFILE_V1", CanonicalLine);
        }

        public string VillageLayoutId { get; }
        public string SourceMap13Shape { get; }
        public int BoundsWidth { get; }
        public int BoundsHeight { get; }
        public int ActiveSectorCount { get; }
        public int RoadCellCount { get; }
        public int FacilityCount { get; }
        public int FixedFacilityCount { get; }
        public int OptionalFacilityCount { get; }
        public int DoorMarkerCount { get; }
        public int NpcMarkerCount { get; }
        public int InventoryMarkerCount { get; }
        public int ShopkeeperMarkerCount { get; }
        public int StateVariantCount { get; }
        public string SourceMap13AuditDigest => MoonPalaceVillagePreconditions.SourceMap13AuditDigest;
        public string SourceMap18ExportDigest => MoonPalaceVillagePreconditions.SourceMap18ExportDigest;
        public string FacilityDigest { get; }
        public string RoadDigest { get; }
        public string DoorDigest { get; }
        public string StateDigest { get; }
        public string CanonicalDigest { get; }
        public bool IsOptionalReferenceLocal => true;
        public bool IsProgressionBlocker => false;
        public bool HasRequiredRewardDependency => false;
        public bool HasCoreResourceDependency => false;
        public bool HasForgeBossDependency => false;
        public string CanonicalLine => MoonPalaceVillageCanonical.Join(VillageLayoutId,
            SchemaVersion, SourceMap13Shape, N(BoundsWidth), N(BoundsHeight),
            N(ActiveSectorCount), N(RoadCellCount), N(FacilityCount),
            N(FixedFacilityCount), N(OptionalFacilityCount), N(DoorMarkerCount),
            N(NpcMarkerCount), N(InventoryMarkerCount), N(ShopkeeperMarkerCount),
            N(StateVariantCount), SourceMap13AuditDigest, SourceMap18ExportDigest,
            FacilityDigest, RoadDigest, DoorDigest, StateDigest, "true", "false",
            "false", "false", "false");

        private static string Village(string value)
        {
            value = MoonPalaceVillageCanonical.Required(value);
            if (!value.StartsWith("VLG_MOONPALACE_", StringComparison.Ordinal))
                throw new ArgumentException("MoonPalace Village layout id required.");
            return value;
        }
        private static string N(int value) => MoonPalaceVillageCanonical.Number(value);
    }

    public sealed class MoonPalaceVillageFacility
    {
        public MoonPalaceVillageFacility(string layoutId, string facilityId,
            string facilityKind, string requirement, string slotId, int localX,
            int localY)
        {
            VillageLayoutId = MoonPalaceVillageCanonical.Required(layoutId);
            FacilityId = MoonPalaceVillageCanonical.Required(facilityId);
            FacilityKind = MoonPalaceVillageCanonical.Required(facilityKind);
            Requirement = MoonPalaceVillageCanonical.Required(requirement);
            SlotId = MoonPalaceVillageCanonical.Required(slotId);
            LocalX = localX;
            LocalY = localY;
            CanonicalDigest = MoonPalaceVillageCanonical.Hash("MAP21_08_VILLAGE_FACILITY_V1", CanonicalLine);
        }

        public string VillageLayoutId { get; }
        public string FacilityId { get; }
        public string FacilityKind { get; }
        public string Requirement { get; }
        public string SlotId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public bool IsFixed => Requirement == "Fixed";
        public bool IsOptional => Requirement == "Optional";
        public bool IsStaticAuthoringOnly => true;
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceVillageCanonical.Join(VillageLayoutId,
            FacilityId, FacilityKind, Requirement, SlotId,
            MoonPalaceVillageCanonical.Number(LocalX),
            MoonPalaceVillageCanonical.Number(LocalY), "true");
    }

    public sealed class MoonPalaceVillageRoadCell
    {
        public MoonPalaceVillageRoadCell(string layoutId, string roadCellId, int order,
            int localX, int localY)
        {
            VillageLayoutId = MoonPalaceVillageCanonical.Required(layoutId);
            RoadCellId = MoonPalaceVillageCanonical.Required(roadCellId);
            Order = order;
            LocalX = localX;
            LocalY = localY;
            CanonicalDigest = MoonPalaceVillageCanonical.Hash("MAP21_08_VILLAGE_ROAD_V1", CanonicalLine);
        }

        public string VillageLayoutId { get; }
        public string RoadCellId { get; }
        public int Order { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public bool CentralRoad => true;
        public string AccessClass => "MandatoryNoTool";
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceVillageCanonical.Join(VillageLayoutId,
            RoadCellId, MoonPalaceVillageCanonical.Number(Order),
            MoonPalaceVillageCanonical.Number(LocalX),
            MoonPalaceVillageCanonical.Number(LocalY), "true", AccessClass);
    }

    public sealed class MoonPalaceVillageDoor
    {
        public MoonPalaceVillageDoor(string layoutId, string doorId, string facilityId,
            string witnessId, int localX, int localY, int roadReturnX, int roadReturnY,
            bool ownsCollision = false, bool ownsLock = false, bool pathBlocking = false)
        {
            VillageLayoutId = MoonPalaceVillageCanonical.Required(layoutId);
            DoorId = MoonPalaceVillageCanonical.Required(doorId);
            FacilityId = MoonPalaceVillageCanonical.Required(facilityId);
            WitnessId = MoonPalaceVillageCanonical.Required(witnessId);
            LocalX = localX;
            LocalY = localY;
            RoadReturnX = roadReturnX;
            RoadReturnY = roadReturnY;
            OwnsCollision = ownsCollision;
            OwnsLock = ownsLock;
            PathBlocking = pathBlocking;
            CanonicalDigest = MoonPalaceVillageCanonical.Hash("MAP21_08_VILLAGE_DOOR_V1", CanonicalLine);
        }

        public string VillageLayoutId { get; }
        public string DoorId { get; }
        public string FacilityId { get; }
        public string WitnessId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public int RoadReturnX { get; }
        public int RoadReturnY { get; }
        public bool ForwardWitness => true;
        public bool ReverseRoadReturnWitness => true;
        public bool MarkerOnly => true;
        public bool OwnsCollision { get; }
        public bool OwnsLock { get; }
        public bool PathBlocking { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceVillageCanonical.Join(VillageLayoutId,
            DoorId, FacilityId, WitnessId, N(LocalX), N(LocalY), N(RoadReturnX),
            N(RoadReturnY), "true", "true", "true", B(OwnsCollision), B(OwnsLock),
            B(PathBlocking));

        private static string N(int value) => MoonPalaceVillageCanonical.Number(value);
        private static string B(bool value) => MoonPalaceVillageCanonical.Bool(value);
    }

    public sealed class MoonPalaceVillageMarker
    {
        public MoonPalaceVillageMarker(string layoutId, string markerId, string markerKind,
            string facilityId, string role, int localX, int localY, bool shopkeeper,
            bool staticPayloadOnly = true)
        {
            VillageLayoutId = MoonPalaceVillageCanonical.Required(layoutId);
            MarkerId = MoonPalaceVillageCanonical.Required(markerId);
            MarkerKind = MoonPalaceVillageCanonical.Required(markerKind);
            FacilityId = MoonPalaceVillageCanonical.Required(facilityId);
            Role = MoonPalaceVillageCanonical.Required(role);
            LocalX = localX;
            LocalY = localY;
            IsShopkeeper = shopkeeper;
            StaticPayloadOnly = staticPayloadOnly;
            CanonicalDigest = MoonPalaceVillageCanonical.Hash("MAP21_08_VILLAGE_MARKER_V1", CanonicalLine);
        }

        public string VillageLayoutId { get; }
        public string MarkerId { get; }
        public string MarkerKind { get; }
        public string FacilityId { get; }
        public string Role { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public bool IsShopkeeper { get; }
        public bool StaticPayloadOnly { get; }
        public string RuntimeControllerId => "NONE";
        public string InventoryOrPricePayload => "NONE";
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceVillageCanonical.Join(VillageLayoutId,
            MarkerId, MarkerKind, FacilityId, Role,
            MoonPalaceVillageCanonical.Number(LocalX),
            MoonPalaceVillageCanonical.Number(LocalY),
            MoonPalaceVillageCanonical.Bool(IsShopkeeper),
            MoonPalaceVillageCanonical.Bool(StaticPayloadOnly), RuntimeControllerId,
            InventoryOrPricePayload);
    }

    public sealed class MoonPalaceVillageStateVariant
    {
        public MoonPalaceVillageStateVariant(string layoutId, string variantKind,
            string npcMarkerIds, string npcStates, string inventoryMarkerIds,
            string inventoryStates, string doorState, string individualTargetOrNone,
            string roadDigest, string facilityDigest, string doorDigest)
        {
            VillageLayoutId = MoonPalaceVillageCanonical.Required(layoutId);
            VariantKind = MoonPalaceVillageCanonical.Required(variantKind);
            NpcMarkerIds = MoonPalaceVillageCanonical.Required(npcMarkerIds);
            NpcStates = MoonPalaceVillageCanonical.Required(npcStates);
            InventoryMarkerIds = MoonPalaceVillageCanonical.Required(inventoryMarkerIds);
            InventoryStates = MoonPalaceVillageCanonical.Required(inventoryStates);
            DoorState = MoonPalaceVillageCanonical.Required(doorState);
            IndividualTargetOrNone = MoonPalaceVillageCanonical.Required(individualTargetOrNone);
            RoadDigest = MoonPalaceVillageCanonical.Digest(roadDigest);
            FacilityDigest = MoonPalaceVillageCanonical.Digest(facilityDigest);
            DoorDigest = MoonPalaceVillageCanonical.Digest(doorDigest);
            CanonicalDigest = MoonPalaceVillageCanonical.Hash("MAP21_08_VILLAGE_STATE_V1", CanonicalLine);
        }

        public string VillageLayoutId { get; }
        public string VariantKind { get; }
        public string NpcMarkerIds { get; }
        public string NpcStates { get; }
        public string InventoryMarkerIds { get; }
        public string InventoryStates { get; }
        public string DoorState { get; }
        public string IndividualTargetOrNone { get; }
        public string RoadDigest { get; }
        public string FacilityDigest { get; }
        public string DoorDigest { get; }
        public int RoadMutationCount => 0;
        public int FacilityCoordinateMutationCount => 0;
        public int DoorCoordinateMutationCount => 0;
        public int AccessWitnessMutationCount => 0;
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceVillageCanonical.Join(VillageLayoutId,
            VariantKind, NpcMarkerIds, NpcStates, InventoryMarkerIds, InventoryStates,
            DoorState, IndividualTargetOrNone, RoadDigest, FacilityDigest, DoorDigest,
            "0", "0", "0", "0");
    }

    public sealed class MoonPalaceVillageProduction
    {
        private static readonly string[] LayoutIds =
        {
            "VLG_MOONPALACE_1X1_OVERVIEW",
            "VLG_MOONPALACE_2X1_MARKET",
            "VLG_MOONPALACE_1X2_ASCENT",
        };
        private static readonly string[] Variants =
        {
            "Normal", "Friendly", "IndividualHostile", "AllHostile", "Evacuation",
        };
        private readonly ReadOnlyCollection<MoonPalaceVillageLayoutProfile> profiles;
        private readonly ReadOnlyCollection<MoonPalaceVillageFacility> facilities;
        private readonly ReadOnlyCollection<MoonPalaceVillageRoadCell> roads;
        private readonly ReadOnlyCollection<MoonPalaceVillageDoor> doors;
        private readonly ReadOnlyCollection<MoonPalaceVillageMarker> markers;
        private readonly ReadOnlyCollection<MoonPalaceVillageStateVariant> states;

        public MoonPalaceVillageProduction(
            IEnumerable<MoonPalaceVillageLayoutProfile> profiles,
            IEnumerable<MoonPalaceVillageFacility> facilities,
            IEnumerable<MoonPalaceVillageRoadCell> roads,
            IEnumerable<MoonPalaceVillageDoor> doors,
            IEnumerable<MoonPalaceVillageMarker> markers,
            IEnumerable<MoonPalaceVillageStateVariant> states, string createdUtc)
        {
            this.profiles = Freeze(profiles, value => value.VillageLayoutId);
            this.facilities = Freeze(facilities, value => value.VillageLayoutId + "/" + value.FacilityId);
            this.roads = Freeze(roads, value => value.VillageLayoutId + "/" + value.Order.ToString("D3", CultureInfo.InvariantCulture));
            this.doors = Freeze(doors, value => value.VillageLayoutId + "/" + value.DoorId);
            this.markers = Freeze(markers, value => value.VillageLayoutId + "/" + value.MarkerId);
            this.states = Freeze(states, value => value.VillageLayoutId + "/" + VariantOrder(value.VariantKind).ToString("D2", CultureInfo.InvariantCulture));
            CreatedUtc = createdUtc ?? string.Empty;
            Validate();
            ProfileSetDigest = SetDigest("MAP21_08_PROFILE_SET_V1", this.profiles.Select(value => value.CanonicalLine));
            FacilitySetDigest = SetDigest("MAP21_08_FACILITY_SET_V1", this.facilities.Select(value => value.CanonicalLine));
            RoadSetDigest = SetDigest("MAP21_08_ROAD_SET_V1", this.roads.Select(value => value.CanonicalLine));
            DoorSetDigest = SetDigest("MAP21_08_DOOR_SET_V1", this.doors.Select(value => value.CanonicalLine));
            MarkerSetDigest = SetDigest("MAP21_08_MARKER_SET_V1", this.markers.Select(value => value.CanonicalLine));
            StateSetDigest = SetDigest("MAP21_08_STATE_SET_V1", this.states.Select(value => value.CanonicalLine));
            VillageManifestDigest = MoonPalaceVillageCanonical.Hash("MAP21_08_VILLAGE_MANIFEST_V1",
                ProfileSetDigest, FacilitySetDigest, MarkerSetDigest, "created_utc_excluded=true");
            AccessManifestDigest = MoonPalaceVillageCanonical.Hash("MAP21_08_ACCESS_MANIFEST_V1",
                RoadSetDigest, DoorSetDigest, "created_utc_excluded=true");
            StateManifestDigest = MoonPalaceVillageCanonical.Hash("MAP21_08_STATE_MANIFEST_V1",
                StateSetDigest, "created_utc_excluded=true");
        }

        public static IReadOnlyList<string> RequiredLayoutIds => LayoutIds;
        public static IReadOnlyList<string> RequiredStateVariants => Variants;
        public IReadOnlyList<MoonPalaceVillageLayoutProfile> Profiles => profiles;
        public IReadOnlyList<MoonPalaceVillageFacility> Facilities => facilities;
        public IReadOnlyList<MoonPalaceVillageRoadCell> Roads => roads;
        public IReadOnlyList<MoonPalaceVillageDoor> Doors => doors;
        public IReadOnlyList<MoonPalaceVillageMarker> Markers => markers;
        public IReadOnlyList<MoonPalaceVillageStateVariant> StateVariants => states;
        public string CreatedUtc { get; }
        public string ProfileSetDigest { get; }
        public string FacilitySetDigest { get; }
        public string RoadSetDigest { get; }
        public string DoorSetDigest { get; }
        public string MarkerSetDigest { get; }
        public string StateSetDigest { get; }
        public string VillageManifestDigest { get; }
        public string AccessManifestDigest { get; }
        public string StateManifestDigest { get; }
        public bool CanExecuteRuntimeSideEffects => false;

        public void RequestRuntimeExecution() => throw new InvalidOperationException(
            "MAP21_08 is static Village production data only.");

        public string SerializeProfilesCsv() => Csv(
            "village_layout_id,schema_version,source_map13_shape,bounds_width,bounds_height,active_sector_count,road_cell_count,facility_count,fixed_facility_count,optional_facility_count,door_marker_count,npc_marker_count,inventory_marker_count,shopkeeper_marker_count,state_variant_count,source_map13_audit_digest,source_map18_export_digest,facility_digest,road_digest,door_digest,state_digest,canonical_digest",
            profiles.Select(value => Row(value.VillageLayoutId,
                MoonPalaceVillageLayoutProfile.SchemaVersion, value.SourceMap13Shape,
                N(value.BoundsWidth), N(value.BoundsHeight), N(value.ActiveSectorCount),
                N(value.RoadCellCount), N(value.FacilityCount), N(value.FixedFacilityCount),
                N(value.OptionalFacilityCount), N(value.DoorMarkerCount),
                N(value.NpcMarkerCount), N(value.InventoryMarkerCount),
                N(value.ShopkeeperMarkerCount), N(value.StateVariantCount),
                value.SourceMap13AuditDigest, value.SourceMap18ExportDigest,
                value.FacilityDigest, value.RoadDigest, value.DoorDigest,
                value.StateDigest, value.CanonicalDigest)));

        public string SerializeFacilitiesCsv() => Csv(
            "village_layout_id,facility_id,facility_kind,requirement,slot_id,local_x,local_y,static_authoring_only,canonical_digest",
            facilities.Select(value => Row(value.VillageLayoutId, value.FacilityId,
                value.FacilityKind, value.Requirement, value.SlotId, N(value.LocalX),
                N(value.LocalY), "true", value.CanonicalDigest)));

        public string SerializeRoadsCsv() => Csv(
            "village_layout_id,road_cell_id,order,local_x,local_y,central_road,access_class,canonical_digest",
            roads.Select(value => Row(value.VillageLayoutId, value.RoadCellId,
                N(value.Order), N(value.LocalX), N(value.LocalY), "true",
                value.AccessClass, value.CanonicalDigest)));

        public string SerializeDoorsCsv() => Csv(
            "village_layout_id,door_id,facility_id,witness_id,local_x,local_y,road_return_x,road_return_y,forward_witness,reverse_road_return_witness,marker_only,owns_collision,owns_lock,path_blocking,canonical_digest",
            doors.Select(value => Row(value.VillageLayoutId, value.DoorId,
                value.FacilityId, value.WitnessId, N(value.LocalX), N(value.LocalY),
                N(value.RoadReturnX), N(value.RoadReturnY), "true", "true", "true",
                B(value.OwnsCollision), B(value.OwnsLock), B(value.PathBlocking),
                value.CanonicalDigest)));

        public string SerializeMarkersCsv() => Csv(
            "village_layout_id,marker_id,marker_kind,facility_id,role,local_x,local_y,is_shopkeeper,static_payload_only,runtime_controller_id,inventory_or_price_payload,canonical_digest",
            markers.Select(value => Row(value.VillageLayoutId, value.MarkerId,
                value.MarkerKind, value.FacilityId, value.Role, N(value.LocalX),
                N(value.LocalY), B(value.IsShopkeeper), B(value.StaticPayloadOnly),
                value.RuntimeControllerId, value.InventoryOrPricePayload,
                value.CanonicalDigest)));

        public string SerializeStateVariantsCsv() => Csv(
            "village_layout_id,variant_kind,npc_marker_ids,npc_states,inventory_marker_ids,inventory_states,door_state,individual_target_or_none,road_digest,facility_digest,door_digest,road_mutation_count,facility_coordinate_mutation_count,door_coordinate_mutation_count,access_witness_mutation_count,canonical_digest",
            states.Select(value => Row(value.VillageLayoutId, value.VariantKind,
                value.NpcMarkerIds, value.NpcStates, value.InventoryMarkerIds,
                value.InventoryStates, value.DoorState, value.IndividualTargetOrNone,
                value.RoadDigest, value.FacilityDigest, value.DoorDigest, "0", "0", "0",
                "0", value.CanonicalDigest)));

        public string SerializeVillageManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceVillageManifestDocument.From(this));
        public string SerializeAccessManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceVillageAccessDocument.From(this));
        public string SerializeStateManifest() => MoonPalaceCanonical.ToJson(
            MoonPalaceVillageStateDocument.From(this));

        private void Validate()
        {
            if (profiles.Count != 3 || facilities.Count != 16 || roads.Count != 208 ||
                doors.Count != 16 || markers.Count != 15 || states.Count != 15)
                throw new ArgumentException("Exact MAP21_08 inventory required.");
            RequireUnique(profiles.Select(value => value.VillageLayoutId), "layout");
            RequireUnique(facilities.Select(value => value.VillageLayoutId + "/" + value.FacilityId), "facility");
            RequireUnique(roads.Select(value => value.VillageLayoutId + "/" + value.RoadCellId), "road");
            RequireUnique(doors.Select(value => value.VillageLayoutId + "/" + value.DoorId), "door");
            RequireUnique(markers.Select(value => value.VillageLayoutId + "/" + value.MarkerId), "marker");
            RequireUnique(states.Select(value => value.VillageLayoutId + "/" + value.VariantKind), "state");

            var specs = new Dictionary<string, object[]>(StringComparer.Ordinal)
            {
                { "VLG_MOONPALACE_1X1_OVERVIEW", new object[] { "1x1", 48, 32, 1, 48, 5, 3, "none" } },
                { "VLG_MOONPALACE_2X1_MARKET", new object[] { "2x1", 96, 32, 2, 96, 6, 4, "x=47/48" } },
                { "VLG_MOONPALACE_1X2_ASCENT", new object[] { "1x2", 48, 64, 2, 64, 5, 3, "y=31/32" } },
            };
            foreach (var profile in profiles)
            {
                if (!specs.TryGetValue(profile.VillageLayoutId, out var spec) ||
                    profile.SourceMap13Shape != (string)spec[0] ||
                    profile.BoundsWidth != (int)spec[1] || profile.BoundsHeight != (int)spec[2] ||
                    profile.ActiveSectorCount != (int)spec[3] || profile.RoadCellCount != (int)spec[4] ||
                    profile.FacilityCount != (int)spec[5] || profile.FixedFacilityCount != 2 ||
                    profile.OptionalFacilityCount != (int)spec[6] ||
                    profile.DoorMarkerCount != profile.FacilityCount || profile.NpcMarkerCount != 3 ||
                    profile.InventoryMarkerCount != 2 || profile.ShopkeeperMarkerCount != 1 ||
                    profile.StateVariantCount != 5 || !profile.IsOptionalReferenceLocal ||
                    profile.IsProgressionBlocker || profile.HasRequiredRewardDependency ||
                    profile.HasCoreResourceDependency || profile.HasForgeBossDependency)
                    throw new ArgumentException("Village profile contract mismatch.");
                ValidateLayout(profile);
            }
        }

        private void ValidateLayout(MoonPalaceVillageLayoutProfile profile)
        {
            var layoutFacilities = facilities.Where(value => value.VillageLayoutId == profile.VillageLayoutId).ToArray();
            var layoutRoads = roads.Where(value => value.VillageLayoutId == profile.VillageLayoutId).OrderBy(value => value.Order).ToArray();
            var layoutDoors = doors.Where(value => value.VillageLayoutId == profile.VillageLayoutId).ToArray();
            var layoutMarkers = markers.Where(value => value.VillageLayoutId == profile.VillageLayoutId).ToArray();
            var layoutStates = states.Where(value => value.VillageLayoutId == profile.VillageLayoutId).ToArray();
            if (layoutFacilities.Length != profile.FacilityCount || layoutRoads.Length != profile.RoadCellCount ||
                layoutDoors.Length != profile.DoorMarkerCount || layoutStates.Length != 5 ||
                layoutMarkers.Count(value => value.MarkerKind == "Npc") != 3 ||
                layoutMarkers.Count(value => value.MarkerKind == "Inventory") != 2 ||
                layoutMarkers.Count(value => value.IsShopkeeper) != 1 ||
                layoutMarkers.Any(value => !value.StaticPayloadOnly || value.RuntimeControllerId != "NONE" ||
                    value.InventoryOrPricePayload != "NONE"))
                throw new ArgumentException("Village child inventory mismatch.");
            var requiredKinds = new[] { "Kitchen", "Repair", "OptionalRest", "OptionalStorage", "OptionalMarket" };
            if (requiredKinds.Any(kind => layoutFacilities.Count(value => value.FacilityKind == kind) != 1) ||
                layoutFacilities.Count(value => value.FacilityKind == "OptionalLore") !=
                    (profile.SourceMap13Shape == "2x1" ? 1 : 0) ||
                layoutFacilities.Count(value => value.IsFixed) != 2 ||
                layoutFacilities.Count(value => value.IsOptional) != profile.OptionalFacilityCount ||
                layoutFacilities.Single(value => value.FacilityKind == "Kitchen").Requirement != "Fixed" ||
                layoutFacilities.Single(value => value.FacilityKind == "Repair").Requirement != "Fixed")
                throw new ArgumentException("Village facility matrix mismatch.");
            if (layoutFacilities.Any(value => !Inside(value.LocalX, value.LocalY, profile)) ||
                layoutRoads.Any(value => !Inside(value.LocalX, value.LocalY, profile)) ||
                layoutDoors.Any(value => !Inside(value.LocalX, value.LocalY, profile) ||
                    !Inside(value.RoadReturnX, value.RoadReturnY, profile)) ||
                layoutMarkers.Any(value => !Inside(value.LocalX, value.LocalY, profile)))
                throw new ArgumentException("Village region-local coordinate out of range.");
            for (var index = 0; index < layoutRoads.Length; index++)
            {
                if (layoutRoads[index].Order != index) throw new ArgumentException("Road order mismatch.");
                if (index > 0 && Math.Abs(layoutRoads[index].LocalX - layoutRoads[index - 1].LocalX) +
                    Math.Abs(layoutRoads[index].LocalY - layoutRoads[index - 1].LocalY) != 1)
                    throw new ArgumentException("Central road must be cardinally continuous.");
            }
            if (profile.SourceMap13Shape == "2x1" && !HasRoadPair(layoutRoads, 47, 16, 48, 16))
                throw new ArgumentException("Missing x=47/48 seam evidence.");
            if (profile.SourceMap13Shape == "1x2" && !HasRoadPair(layoutRoads, 24, 31, 24, 32))
                throw new ArgumentException("Missing y=31/32 seam evidence.");
            foreach (var facility in layoutFacilities)
            {
                var door = layoutDoors.SingleOrDefault(value => value.FacilityId == facility.FacilityId);
                if (door == null || !door.ForwardWitness || !door.ReverseRoadReturnWitness ||
                    !door.MarkerOnly || door.OwnsCollision || door.OwnsLock || door.PathBlocking ||
                    Math.Abs(door.LocalX - door.RoadReturnX) + Math.Abs(door.LocalY - door.RoadReturnY) != 1 ||
                    !layoutRoads.Any(value => value.LocalX == door.RoadReturnX && value.LocalY == door.RoadReturnY))
                    throw new ArgumentException("Door road-return witness mismatch.");
            }
            if (profile.FacilityDigest != SetDigest("MAP21_08_LAYOUT_FACILITIES_V1", layoutFacilities.Select(value => value.CanonicalLine)) ||
                profile.RoadDigest != SetDigest("MAP21_08_LAYOUT_ROADS_V1", layoutRoads.Select(value => value.CanonicalLine)) ||
                profile.DoorDigest != SetDigest("MAP21_08_LAYOUT_DOORS_V1", layoutDoors.Select(value => value.CanonicalLine)) ||
                profile.StateDigest != SetDigest("MAP21_08_LAYOUT_STATES_V1", layoutMarkers.Select(value => value.CanonicalLine).Concat(layoutStates.Select(value => value.CanonicalLine))))
                throw new ArgumentException("Village child digest mismatch.");
            ValidateStates(profile, layoutMarkers, layoutStates);
        }

        private static void ValidateStates(MoonPalaceVillageLayoutProfile profile,
            IReadOnlyCollection<MoonPalaceVillageMarker> layoutMarkers,
            IReadOnlyCollection<MoonPalaceVillageStateVariant> layoutStates)
        {
            if (layoutStates.Select(value => value.VariantKind).OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(Variants.OrderBy(value => value, StringComparer.Ordinal)) == false)
                throw new ArgumentException("Five state variants required.");
            var npcIds = layoutMarkers.Where(value => value.MarkerKind == "Npc").Select(value => value.MarkerId)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var inventoryIds = layoutMarkers.Where(value => value.MarkerKind == "Inventory").Select(value => value.MarkerId)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            foreach (var state in layoutStates)
            {
                var stateNpcIds = Parts(state.NpcMarkerIds);
                var npcStates = Parts(state.NpcStates);
                var stateInventoryIds = Parts(state.InventoryMarkerIds);
                var inventoryStates = Parts(state.InventoryStates);
                if (!stateNpcIds.SequenceEqual(npcIds) || !stateInventoryIds.SequenceEqual(inventoryIds) ||
                    npcStates.Length != 3 || inventoryStates.Length != 2 ||
                    state.RoadDigest != profile.RoadDigest || state.FacilityDigest != profile.FacilityDigest ||
                    state.DoorDigest != profile.DoorDigest || state.RoadMutationCount != 0 ||
                    state.FacilityCoordinateMutationCount != 0 || state.DoorCoordinateMutationCount != 0 ||
                    state.AccessWitnessMutationCount != 0)
                    throw new ArgumentException("State invariant mismatch.");
                switch (state.VariantKind)
                {
                    case "Normal": RequireStates(npcStates, "Normal", inventoryStates, "Standard", state.DoorState, "Standard", state.IndividualTargetOrNone, "NONE"); break;
                    case "Friendly": RequireStates(npcStates, "Friendly", inventoryStates, "FriendlyAccess", state.DoorState, "Welcome", state.IndividualTargetOrNone, "NONE"); break;
                    case "AllHostile": RequireStates(npcStates, "Hostile", inventoryStates, "Unavailable", state.DoorState, "Alert", state.IndividualTargetOrNone, "NONE"); break;
                    case "Evacuation": RequireStates(npcStates, "Evacuated", inventoryStates, "Evacuated", state.DoorState, "Evacuated", state.IndividualTargetOrNone, "NONE"); break;
                    case "IndividualHostile":
                        var hostile = Array.FindIndex(npcStates, value => value == "Hostile");
                        if (npcStates.Count(value => value == "Hostile") != 1 || npcStates.Count(value => value == "Normal") != 2 ||
                            hostile < 0 || stateNpcIds[hostile] != state.IndividualTargetOrNone ||
                            inventoryStates.Any(value => value != "Standard") || state.DoorState != "Standard")
                            throw new ArgumentException("IndividualHostile target mismatch.");
                        break;
                    default: throw new ArgumentException("Unknown state variant.");
                }
            }
        }

        private static void RequireStates(string[] npc, string npcState, string[] inventory,
            string inventoryState, string actualDoor, string doorState, string actualTarget,
            string target)
        {
            if (npc.Any(value => value != npcState) || inventory.Any(value => value != inventoryState) ||
                actualDoor != doorState || actualTarget != target)
                throw new ArgumentException("Marker state matrix mismatch.");
        }

        private static bool Inside(int x, int y, MoonPalaceVillageLayoutProfile profile) =>
            x >= 0 && x < profile.BoundsWidth && y >= 0 && y < profile.BoundsHeight;
        private static bool HasRoadPair(IEnumerable<MoonPalaceVillageRoadCell> values,
            int ax, int ay, int bx, int by) => values.Any(value => value.LocalX == ax && value.LocalY == ay) &&
            values.Any(value => value.LocalX == bx && value.LocalY == by);
        private static string[] Parts(string value) => value.Split('|');
        private static int VariantOrder(string value)
        {
            var index = Array.IndexOf(Variants, value);
            return index < 0 ? int.MaxValue : index;
        }
        private static void RequireUnique(IEnumerable<string> values, string label)
        {
            var array = values.ToArray();
            if (array.Any(string.IsNullOrWhiteSpace) || array.Distinct(StringComparer.Ordinal).Count() != array.Length)
                throw new ArgumentException("Duplicate or empty " + label + " id.");
        }
        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, Func<T, string> key)
        {
            var values = (source ?? throw new ArgumentNullException(nameof(source))).ToArray();
            if (values.Any(value => value == null)) throw new ArgumentException("Null record.");
            return new ReadOnlyCollection<T>(values.OrderBy(key, StringComparer.Ordinal).ToArray());
        }
        public static string SetDigest(string prefix, IEnumerable<string> lines) =>
            MoonPalaceVillageCanonical.Hash(new[] { prefix }.Concat((lines ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal)).ToArray());
        private static string Csv(string header, IEnumerable<string> rows) =>
            string.Join("\n", new[] { header }.Concat(rows)) + "\n";
        private static string Row(params string[] values) => string.Join(",", values.Select(MoonPalaceVillageCanonical.Escape));
        private static string N(int value) => MoonPalaceVillageCanonical.Number(value);
        private static string B(bool value) => MoonPalaceVillageCanonical.Bool(value);
    }

    public sealed class MoonPalaceVillageDigestManifest
    {
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> observedSourceResults;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> csvDigests;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> jsonDigests;

        public MoonPalaceVillageDigestManifest(MoonPalaceVillageProduction production,
            IEnumerable<MoonPalaceNamedDigest> observedSourceResults,
            IEnumerable<MoonPalaceNamedDigest> csvDigests,
            IEnumerable<MoonPalaceNamedDigest> jsonDigests, string createdUtc)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            this.observedSourceResults = Copy(observedSourceResults, 4, "historical Result");
            this.csvDigests = Copy(csvDigests, 6, "CSV");
            this.jsonDigests = Copy(jsonDigests, 3, "JSON");
            CreatedUtc = createdUtc ?? string.Empty;
            Map2109HandoffDigest = MoonPalaceVillageCanonical.Hash(new[]
            {
                "MAP21_09_VILLAGE_HANDOFF_V1", MoonPalaceVillagePreconditions.StrictMap2108HandoffDigest,
                MoonPalaceVillagePreconditions.SourceMap13AuditDigest,
                MoonPalaceVillagePreconditions.SourceMap18ExportDigest,
                MoonPalaceVillagePreconditions.SourceMap2104Digest,
                MoonPalaceVillagePreconditions.SourceMap2105Digest,
                MoonPalaceVillagePreconditions.SourceMap2106Digest,
                MoonPalaceVillagePreconditions.SourceMap2107Digest,
                production.VillageManifestDigest, production.AccessManifestDigest,
                production.StateManifestDigest,
            }.Concat(this.observedSourceResults.Select(value => value.CanonicalLine))
             .Concat(this.csvDigests.Select(value => value.CanonicalLine))
             .Concat(this.jsonDigests.Select(value => value.CanonicalLine)).ToArray());
            CanonicalDigest = MoonPalaceVillageCanonical.Hash(new[]
            {
                "map21_08.village_digest_manifest.v1", MoonPalaceVillagePreconditions.TaskId,
                MoonPalaceVillagePreconditions.StrictMap2107ResultDigest,
                MoonPalaceVillagePreconditions.StrictMap2107TaskDigest,
                MoonPalaceVillagePreconditions.StrictMap2108HandoffDigest,
                MoonPalaceVillagePreconditions.SourceMap13AuditDigest,
                MoonPalaceVillagePreconditions.SourceMap18ExportDigest,
                MoonPalaceVillagePreconditions.SourceMap18DebugDigest,
                MoonPalaceVillagePreconditions.SourceMap2104Digest,
                MoonPalaceVillagePreconditions.SourceMap2105Digest,
                MoonPalaceVillagePreconditions.SourceMap2106Digest,
                MoonPalaceVillagePreconditions.SourceMap2107Digest,
                Map2109HandoffDigest, "created_utc_excluded=true",
            }.Concat(this.observedSourceResults.Select(value => value.CanonicalLine))
             .Concat(this.csvDigests.Select(value => value.CanonicalLine))
             .Concat(this.jsonDigests.Select(value => value.CanonicalLine)).ToArray());
        }

        public MoonPalaceVillageProduction Production { get; }
        public IReadOnlyList<MoonPalaceNamedDigest> ObservedSourceResultDigests => observedSourceResults;
        public IReadOnlyList<MoonPalaceNamedDigest> CsvDigests => csvDigests;
        public IReadOnlyList<MoonPalaceNamedDigest> JsonDigests => jsonDigests;
        public string CreatedUtc { get; }
        public string Map2109HandoffDigest { get; }
        public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(MoonPalaceVillageDigestDocument.From(this));

        private static ReadOnlyCollection<MoonPalaceNamedDigest> Copy(
            IEnumerable<MoonPalaceNamedDigest> source, int expected, string label)
        {
            var values = (source ?? throw new ArgumentNullException(nameof(source)))
                .OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
            if (values.Length != expected || values.Any(value => value == null) ||
                values.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count() != expected)
                throw new ArgumentException("Exact " + label + " digest inventory required.");
            return new ReadOnlyCollection<MoonPalaceNamedDigest>(values);
        }
    }

    public sealed class MoonPalaceVillageForbiddenOperationCounters
    {
        public static MoonPalaceVillageForbiddenOperationCounters Zero => new MoonPalaceVillageForbiddenOperationCounters();
        public int NpcSpawns => 0;
        public int NpcAiExecutions => 0;
        public int CombatExecutions => 0;
        public int ShopTransactions => 0;
        public int DoorCollisionOrLockWrites => 0;
        public int HostileEvacuationRuntimeExecutions => 0;
        public int SaveFileWrites => 0;
        public int SaveFileReads => 0;
        public int PlayerPrefsWrites => 0;
        public int PlayerPrefsReads => 0;
        public int WorldSectorPlacements => 0;
        public int GenerationRunnerExecutions => 0;
        public int RendererExecutions => 0;
        public int ValidationRunnerExecutions => 0;
        public int ReplayExecutions => 0;
        public int RollbackExecutions => 0;
        public int TilemapWrites => 0;
        public int RuntimeObjectSpawns => 0;
        public int ScenePrefabChanges => 0;
        public int ColliderAddressablesChanges => 0;
        public int PriorCategorySelections => 0;
        public int PlayModeSelections => 0;
        public int LegacyRegressionSelections => 0;
        public int UnfilteredOrFullRegressionSelections => 0;
        public int UpstreamRegenerationRuns => 0;
        public bool AllZero => NpcSpawns + NpcAiExecutions + CombatExecutions + ShopTransactions +
            DoorCollisionOrLockWrites + HostileEvacuationRuntimeExecutions + SaveFileWrites +
            SaveFileReads + PlayerPrefsWrites + PlayerPrefsReads + WorldSectorPlacements +
            GenerationRunnerExecutions + RendererExecutions + ValidationRunnerExecutions +
            ReplayExecutions + RollbackExecutions + TilemapWrites + RuntimeObjectSpawns +
            ScenePrefabChanges + ColliderAddressablesChanges + PriorCategorySelections +
            PlayModeSelections + LegacyRegressionSelections + UnfilteredOrFullRegressionSelections +
            UpstreamRegenerationRuns == 0;
    }

    internal static class MoonPalaceVillageCanonical
    {
        public static string Required(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value.");
            return value.Trim();
        }
        public static string Digest(string value)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(value)) throw new ArgumentException("SHA-256 required.");
            return value;
        }
        public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        public static string Bool(bool value) => value ? "true" : "false";
        public static string Join(params string[] values) => MoonPalaceCanonical.Join(values);
        public static string Hash(params string[] lines) => BakingCanonicalDigest.HashCanonicalLines(lines);
        public static string Escape(string value)
        {
            var text = value ?? string.Empty;
            return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text :
                "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }

    [Serializable]
    internal sealed class MoonPalaceVillageManifestDocument
    {
        public string schema_version;
        public string publication_kind;
        public int layout_count;
        public int facility_count;
        public int fixed_kitchen_count;
        public int fixed_repair_count;
        public int optional_facility_count;
        public int npc_marker_count;
        public int inventory_marker_count;
        public int shopkeeper_marker_count;
        public int progression_blocker_count;
        public int required_reward_dependency_count;
        public MoonPalaceVillageLayoutSummaryDocument[] layouts;
        public string canonical_digest;

        public static MoonPalaceVillageManifestDocument From(MoonPalaceVillageProduction value) => new MoonPalaceVillageManifestDocument
        {
            schema_version = MoonPalaceVillageLayoutProfile.SchemaVersion,
            publication_kind = "StaticProductionDataNotRuntimeState",
            layout_count = value.Profiles.Count,
            facility_count = value.Facilities.Count,
            fixed_kitchen_count = value.Facilities.Count(item => item.FacilityKind == "Kitchen"),
            fixed_repair_count = value.Facilities.Count(item => item.FacilityKind == "Repair"),
            optional_facility_count = value.Facilities.Count(item => item.IsOptional),
            npc_marker_count = value.Markers.Count(item => item.MarkerKind == "Npc"),
            inventory_marker_count = value.Markers.Count(item => item.MarkerKind == "Inventory"),
            shopkeeper_marker_count = value.Markers.Count(item => item.IsShopkeeper),
            progression_blocker_count = value.Profiles.Count(item => item.IsProgressionBlocker),
            required_reward_dependency_count = value.Profiles.Count(item => item.HasRequiredRewardDependency),
            layouts = value.Profiles.Select(MoonPalaceVillageLayoutSummaryDocument.From).ToArray(),
            canonical_digest = value.VillageManifestDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceVillageLayoutSummaryDocument
    {
        public string village_layout_id;
        public string shape;
        public string bounds;
        public int road_cell_count;
        public int facility_count;
        public int door_marker_count;
        public int state_variant_count;
        public static MoonPalaceVillageLayoutSummaryDocument From(MoonPalaceVillageLayoutProfile value) => new MoonPalaceVillageLayoutSummaryDocument
        {
            village_layout_id = value.VillageLayoutId,
            shape = value.SourceMap13Shape,
            bounds = value.BoundsWidth.ToString(CultureInfo.InvariantCulture) + "x" + value.BoundsHeight.ToString(CultureInfo.InvariantCulture),
            road_cell_count = value.RoadCellCount,
            facility_count = value.FacilityCount,
            door_marker_count = value.DoorMarkerCount,
            state_variant_count = value.StateVariantCount,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceVillageAccessDocument
    {
        public string schema_version;
        public int road_cell_count;
        public int door_marker_count;
        public int forward_witness_count;
        public int road_return_witness_count;
        public int horizontal_seam_pair_count;
        public int vertical_seam_pair_count;
        public int collision_lock_path_block_write_count;
        public string road_set_digest;
        public string door_set_digest;
        public string canonical_digest;
        public static MoonPalaceVillageAccessDocument From(MoonPalaceVillageProduction value) => new MoonPalaceVillageAccessDocument
        {
            schema_version = MoonPalaceVillageLayoutProfile.SchemaVersion,
            road_cell_count = value.Roads.Count,
            door_marker_count = value.Doors.Count,
            forward_witness_count = value.Doors.Count(item => item.ForwardWitness),
            road_return_witness_count = value.Doors.Count(item => item.ReverseRoadReturnWitness),
            horizontal_seam_pair_count = 1,
            vertical_seam_pair_count = 1,
            collision_lock_path_block_write_count = value.Doors.Count(item => item.OwnsCollision || item.OwnsLock || item.PathBlocking),
            road_set_digest = value.RoadSetDigest,
            door_set_digest = value.DoorSetDigest,
            canonical_digest = value.AccessManifestDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceVillageStateDocument
    {
        public string schema_version;
        public int state_variant_count;
        public int individual_hostile_target_count;
        public int all_hostile_variant_count;
        public int evacuation_variant_count;
        public int road_door_access_mutation_count;
        public MoonPalaceVillageStateSummaryDocument[] variants;
        public string state_set_digest;
        public string canonical_digest;
        public static MoonPalaceVillageStateDocument From(MoonPalaceVillageProduction value) => new MoonPalaceVillageStateDocument
        {
            schema_version = MoonPalaceVillageLayoutProfile.SchemaVersion,
            state_variant_count = value.StateVariants.Count,
            individual_hostile_target_count = value.StateVariants.Count(item => item.VariantKind == "IndividualHostile" && item.IndividualTargetOrNone != "NONE"),
            all_hostile_variant_count = value.StateVariants.Count(item => item.VariantKind == "AllHostile"),
            evacuation_variant_count = value.StateVariants.Count(item => item.VariantKind == "Evacuation"),
            road_door_access_mutation_count = value.StateVariants.Sum(item => item.RoadMutationCount + item.DoorCoordinateMutationCount + item.AccessWitnessMutationCount),
            variants = value.StateVariants.Select(MoonPalaceVillageStateSummaryDocument.From).ToArray(),
            state_set_digest = value.StateSetDigest,
            canonical_digest = value.StateManifestDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceVillageStateSummaryDocument
    {
        public string village_layout_id;
        public string variant_kind;
        public string npc_states;
        public string inventory_states;
        public string door_state;
        public string individual_target_or_none;
        public static MoonPalaceVillageStateSummaryDocument From(MoonPalaceVillageStateVariant value) => new MoonPalaceVillageStateSummaryDocument
        {
            village_layout_id = value.VillageLayoutId,
            variant_kind = value.VariantKind,
            npc_states = value.NpcStates,
            inventory_states = value.InventoryStates,
            door_state = value.DoorState,
            individual_target_or_none = value.IndividualTargetOrNone,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceVillageDigestDocument
    {
        public string schema_version;
        public string task_id;
        public string strict_map21_07_result_sha256;
        public string strict_map21_07_installed_task_sha256;
        public string strict_map21_08_handoff_digest;
        public MoonPalaceVillageNamedDigestDocument[] observed_historical_source_result_sha256;
        public string source_map13_audit_digest;
        public string source_map18_export_digest;
        public string source_map18_debug_digest;
        public string source_map21_04_digest;
        public string source_map21_05_digest;
        public string source_map21_06_digest;
        public string source_map21_07_digest;
        public MoonPalaceVillageNamedDigestDocument[] csv_digests;
        public MoonPalaceVillageNamedDigestDocument[] json_digests;
        public string MAP21_09_handoff_digest;
        public bool created_utc_excluded_from_canonical_digest;
        public string created_utc;
        public string canonical_digest;

        public static MoonPalaceVillageDigestDocument From(MoonPalaceVillageDigestManifest value) => new MoonPalaceVillageDigestDocument
        {
            schema_version = "map21_08.village_digest_manifest.v1",
            task_id = MoonPalaceVillagePreconditions.TaskId,
            strict_map21_07_result_sha256 = MoonPalaceVillagePreconditions.StrictMap2107ResultDigest,
            strict_map21_07_installed_task_sha256 = MoonPalaceVillagePreconditions.StrictMap2107TaskDigest,
            strict_map21_08_handoff_digest = MoonPalaceVillagePreconditions.StrictMap2108HandoffDigest,
            observed_historical_source_result_sha256 = value.ObservedSourceResultDigests.Select(MoonPalaceVillageNamedDigestDocument.From).ToArray(),
            source_map13_audit_digest = MoonPalaceVillagePreconditions.SourceMap13AuditDigest,
            source_map18_export_digest = MoonPalaceVillagePreconditions.SourceMap18ExportDigest,
            source_map18_debug_digest = MoonPalaceVillagePreconditions.SourceMap18DebugDigest,
            source_map21_04_digest = MoonPalaceVillagePreconditions.SourceMap2104Digest,
            source_map21_05_digest = MoonPalaceVillagePreconditions.SourceMap2105Digest,
            source_map21_06_digest = MoonPalaceVillagePreconditions.SourceMap2106Digest,
            source_map21_07_digest = MoonPalaceVillagePreconditions.SourceMap2107Digest,
            csv_digests = value.CsvDigests.Select(MoonPalaceVillageNamedDigestDocument.From).ToArray(),
            json_digests = value.JsonDigests.Select(MoonPalaceVillageNamedDigestDocument.From).ToArray(),
            MAP21_09_handoff_digest = value.Map2109HandoffDigest,
            created_utc_excluded_from_canonical_digest = true,
            created_utc = value.CreatedUtc,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class MoonPalaceVillageNamedDigestDocument
    {
        public string name;
        public string sha256;
        public static MoonPalaceVillageNamedDigestDocument From(MoonPalaceNamedDigest value) => new MoonPalaceVillageNamedDigestDocument
        {
            name = value.Name,
            sha256 = value.Digest,
        };
    }
}
