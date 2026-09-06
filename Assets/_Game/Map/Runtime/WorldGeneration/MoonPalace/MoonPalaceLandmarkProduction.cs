using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceLandmarkPreconditions
    {
        public const string TaskId = "MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS";
        public const string StrictMap2108ResultDigest = "7e5e7cfe0bb4e82ed7da405d11a1a05fe06e475208268a9e432840ca43b9d7d8";
        public const string StrictMap2108TaskDigest = "e3b149d0ddfac0138688e7284aad0afb8a8118abcb056f52f2dd91bf6e863e7b";
        public const string StrictMap2109HandoffDigest = "36e124fe4f63258eeca5b5097d45a71e80587efaa1fbb50fb6eeee7e36ef224b";
        public const string SourceMap13AuditDigest = "a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e";
        public const string SourceMap18ExportDigest = "358ac8cfe78eec502db049f8940ed0c71458179b89bb451680e837b0797b77b5";
        public const string SourceMap18DebugDigest = "59efb7fd30df9ec62014cadd04a111b222e7dd13e298789dbab88a661bea22ed";
        public const string SourceMap2107Digest = "0e03c7cb4780b078822297e9877d345480413a2b3b8d9ed0c3d1d455645a4a93";
        public const string SourceMap2108Digest = "9308092afbee03a8c08d5a5fbc2b4eb4a02fc747e5bbc6bd9afe7ce5ec018a73";
        public const string MoonSealSlot = "SR_SLOT_MOON_SEAL_REWARD";
        public const string MoonSealRewardKey = "SR_STATE_MOON_SEAL_FORGE_9_REWARD_MOON_SEAL_REWARD";
    }

    public sealed class MoonPalaceLandmarkProfile
    {
        public MoonPalaceLandmarkProfile(string landmarkId, string regionId, string binding,
            int width, int height, int chunks, int nodes, int edges, int routes, int states,
            int transitions, int resets, int markers, bool footprintClaim = false,
            bool worldOriginClaim = false, bool reservationClaim = false,
            bool bridgeClaim = false, bool fixedSlotClaim = false, bool placedOwnershipClaim = false)
        {
            LandmarkId = L.Required(landmarkId); RegionId = L.Required(regionId);
            Binding = L.Required(binding); DesignWidth = width; DesignHeight = height;
            ChunkCount = chunks; NodeCount = nodes; EdgeCount = edges; RouteCount = routes;
            StateCount = states; TransitionCount = transitions; ResetCount = resets;
            MarkerCount = markers; FootprintClaim = footprintClaim;
            WorldOriginClaim = worldOriginClaim; ReservationClaim = reservationClaim;
            BridgeClaim = bridgeClaim; FixedSlotClaim = fixedSlotClaim;
            PlacedOwnershipClaim = placedOwnershipClaim;
            CanonicalDigest = L.Hash("MAP21_09_LANDMARK_PROFILE_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string RegionId { get; }
        public string Binding { get; } public int DesignWidth { get; } public int DesignHeight { get; }
        public int ChunkCount { get; } public int NodeCount { get; } public int EdgeCount { get; }
        public int RouteCount { get; } public int StateCount { get; } public int TransitionCount { get; }
        public int ResetCount { get; } public int MarkerCount { get; }
        public bool FootprintClaim { get; } public bool WorldOriginClaim { get; }
        public bool ReservationClaim { get; } public bool BridgeClaim { get; }
        public bool FixedSlotClaim { get; } public bool PlacedOwnershipClaim { get; }
        public bool HasAnyPlacementClaim => FootprintClaim || WorldOriginClaim || ReservationClaim ||
            BridgeClaim || FixedSlotClaim || PlacedOwnershipClaim;
        public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, RegionId, Binding, L.N(DesignWidth),
            L.N(DesignHeight), L.N(ChunkCount), L.N(NodeCount), L.N(EdgeCount), L.N(RouteCount),
            L.N(StateCount), L.N(TransitionCount), L.N(ResetCount), L.N(MarkerCount),
            L.B(FootprintClaim), L.B(WorldOriginClaim), L.B(ReservationClaim), L.B(BridgeClaim),
            L.B(FixedSlotClaim), L.B(PlacedOwnershipClaim));
    }

    public sealed class MoonPalaceLandmarkChunk
    {
        public MoonPalaceLandmarkChunk(string landmarkId, string chunkId, int x, int y)
        {
            LandmarkId = L.Required(landmarkId); ChunkId = L.Required(chunkId);
            if (x < 0 || y < 0) throw new ArgumentOutOfRangeException("design chunk coordinate");
            ChunkX = x; ChunkY = y; LocalOriginX = x * 12; LocalOriginY = y * 8;
            CanonicalDigest = L.Hash("MAP21_09_LANDMARK_CHUNK_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string ChunkId { get; }
        public int ChunkX { get; } public int ChunkY { get; }
        public int LocalOriginX { get; } public int LocalOriginY { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, ChunkId, L.N(ChunkX), L.N(ChunkY),
            L.N(LocalOriginX), L.N(LocalOriginY), "12", "8");
    }

    public sealed class MoonPalaceLandmarkNode
    {
        public MoonPalaceLandmarkNode(string landmarkId, string nodeId, string role,
            int localX, int localY, bool required)
        {
            LandmarkId = L.Required(landmarkId); NodeId = L.Required(nodeId);
            Role = L.Required(role); if (localX < 0 || localY < 0)
                throw new ArgumentOutOfRangeException("design-local coordinate");
            LocalX = localX; LocalY = localY; Required = required;
            CanonicalDigest = L.Hash("MAP21_09_LANDMARK_NODE_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string NodeId { get; } public string Role { get; }
        public int LocalX { get; } public int LocalY { get; } public bool Required { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, NodeId, Role, L.N(LocalX), L.N(LocalY), L.B(Required));
    }

    public sealed class MoonPalaceLandmarkEdge
    {
        public MoonPalaceLandmarkEdge(string landmarkId, string edgeId, string fromNodeId,
            string toNodeId, string routeKind, int order, string accessClass, bool required,
            string dependency)
        {
            LandmarkId = L.Required(landmarkId); EdgeId = L.Required(edgeId);
            FromNodeId = L.Required(fromNodeId); ToNodeId = L.Required(toNodeId);
            RouteKind = L.Required(routeKind); Order = order; AccessClass = L.Required(accessClass);
            Required = required; Dependency = L.Required(dependency);
            CanonicalDigest = L.Hash("MAP21_09_LANDMARK_EDGE_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string EdgeId { get; }
        public string FromNodeId { get; } public string ToNodeId { get; }
        public string RouteKind { get; } public int Order { get; }
        public string AccessClass { get; } public bool Required { get; } public string Dependency { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, EdgeId, FromNodeId, ToNodeId,
            RouteKind, L.N(Order), AccessClass, L.B(Required), Dependency);
    }

    public sealed class MoonPalaceLandmarkRoute
    {
        public MoonPalaceLandmarkRoute(string landmarkId, string routeId, string kind,
            string startNodeId, string endNodeId, IEnumerable<string> edgeIds)
        {
            LandmarkId = L.Required(landmarkId); RouteId = L.Required(routeId); Kind = L.Required(kind);
            StartNodeId = L.Required(startNodeId); EndNodeId = L.Required(endNodeId);
            EdgeIds = string.Join("|", (edgeIds ?? throw new ArgumentNullException(nameof(edgeIds))).ToArray());
            if (string.IsNullOrWhiteSpace(EdgeIds)) throw new ArgumentException("Route edges required.");
            CanonicalDigest = L.Hash("MAP21_09_LANDMARK_ROUTE_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string RouteId { get; } public string Kind { get; }
        public string StartNodeId { get; } public string EndNodeId { get; } public string EdgeIds { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, RouteId, Kind, StartNodeId, EndNodeId, EdgeIds);
    }

    public sealed class MoonPalaceLandmarkState
    {
        public MoonPalaceLandmarkState(string landmarkId, string stateId, string role,
            int order, bool persistent)
        {
            LandmarkId = L.Required(landmarkId); StateId = L.Required(stateId);
            Role = L.Required(role); Order = order; Persistent = persistent;
            CanonicalDigest = L.Hash("MAP21_09_LANDMARK_STATE_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string StateId { get; } public string Role { get; }
        public int Order { get; } public bool Persistent { get; } public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, StateId, Role, L.N(Order), L.B(Persistent));
    }

    public sealed class MoonPalaceLandmarkTransition
    {
        public MoonPalaceLandmarkTransition(string landmarkId, string transitionId,
            string fromStateId, string toStateId, string trigger, int order)
        {
            LandmarkId = L.Required(landmarkId); TransitionId = L.Required(transitionId);
            FromStateId = L.Required(fromStateId); ToStateId = L.Required(toStateId);
            Trigger = L.Required(trigger); Order = order;
            CanonicalDigest = L.Hash("MAP21_09_LANDMARK_TRANSITION_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string TransitionId { get; }
        public string FromStateId { get; } public string ToStateId { get; }
        public string Trigger { get; } public int Order { get; } public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, TransitionId, FromStateId, ToStateId, Trigger, L.N(Order));
    }

    public sealed class MoonPalaceLandmarkReset
    {
        public MoonPalaceLandmarkReset(string landmarkId, string resetId, string policy,
            string failureNodeId, string recoveryNodeId, string fromStateId, string toStateId,
            bool returnsAllForgeInputs, bool preservesSealAcceptance, bool preventsReroll)
        {
            LandmarkId = L.Required(landmarkId); ResetId = L.Required(resetId);
            Policy = L.Required(policy); FailureNodeId = L.OrNone(failureNodeId);
            RecoveryNodeId = L.OrNone(recoveryNodeId); FromStateId = L.OrNone(fromStateId);
            ToStateId = L.OrNone(toStateId); ReturnsAllForgeInputs = returnsAllForgeInputs;
            PreservesSealAcceptance = preservesSealAcceptance; PreventsReroll = preventsReroll;
            CanonicalDigest = L.Hash("MAP21_09_LANDMARK_RESET_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string ResetId { get; } public string Policy { get; }
        public string FailureNodeId { get; } public string RecoveryNodeId { get; }
        public string FromStateId { get; } public string ToStateId { get; }
        public bool ReturnsAllForgeInputs { get; } public bool PreservesSealAcceptance { get; }
        public bool PreventsReroll { get; } public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, ResetId, Policy, FailureNodeId,
            RecoveryNodeId, FromStateId, ToStateId, L.B(ReturnsAllForgeInputs),
            L.B(PreservesSealAcceptance), L.B(PreventsReroll));
    }

    public sealed class MoonPalaceLandmarkMarker
    {
        public MoonPalaceLandmarkMarker(string landmarkId, string markerId, string kind,
            string nodeId, string stateId, int order, bool required, string dependency,
            string persistenceKey)
        {
            LandmarkId = L.Required(landmarkId); MarkerId = L.Required(markerId);
            Kind = L.Required(kind); NodeId = L.Required(nodeId); StateId = L.OrNone(stateId);
            Order = order; Required = required; Dependency = L.Required(dependency);
            PersistenceKey = L.OrNone(persistenceKey);
            CanonicalDigest = L.Hash("MAP21_09_LANDMARK_MARKER_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string MarkerId { get; } public string Kind { get; }
        public string NodeId { get; } public string StateId { get; } public int Order { get; }
        public bool Required { get; } public string Dependency { get; } public string PersistenceKey { get; }
        public bool StaticMarkerOnly => true; public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, MarkerId, Kind, NodeId, StateId,
            L.N(Order), L.B(Required), Dependency, PersistenceKey, "static_marker_only=true");
    }

    public sealed class MoonPalaceForgeResourceLedger
    {
        public MoonPalaceForgeResourceLedger(string resource, string sourceRewardKey,
            string available, string reserved, string consumed, string returned,
            string failureBranch, bool returnsAllForgeInputs, int partialLossCount = 0,
            int permanentLossCount = 0)
        {
            Resource = L.Required(resource); SourceRewardKey = L.Required(sourceRewardKey);
            AvailableStateId = L.Required(available); ReservedStateId = L.Required(reserved);
            ConsumedStateId = L.Required(consumed); ReturnedStateId = L.Required(returned);
            FailureBranch = L.Required(failureBranch); ReturnsAllForgeInputs = returnsAllForgeInputs;
            PartialLossCount = partialLossCount; PermanentLossCount = permanentLossCount;
            CanonicalDigest = L.Hash("MAP21_09_FORGE_LEDGER_V1", CanonicalLine);
        }
        public string Resource { get; } public string SourceRewardKey { get; }
        public string AvailableStateId { get; } public string ReservedStateId { get; }
        public string ConsumedStateId { get; } public string ReturnedStateId { get; }
        public string FailureBranch { get; } public bool ReturnsAllForgeInputs { get; }
        public int PartialLossCount { get; } public int PermanentLossCount { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(Resource, SourceRewardKey, AvailableStateId,
            ReservedStateId, ConsumedStateId, ReturnedStateId, FailureBranch,
            L.B(ReturnsAllForgeInputs), L.N(PartialLossCount), L.N(PermanentLossCount));
    }

    public sealed class MoonPalaceOptionalLandmarkVariant
    {
        public MoonPalaceOptionalLandmarkVariant(string landmarkId, string recordKind,
            string value, int order, bool persistentChoice, bool preventsReroll,
            bool hasPlacementClaim = false)
        {
            LandmarkId = L.Required(landmarkId); RecordKind = L.Required(recordKind);
            Value = L.Required(value); Order = order; PersistentChoice = persistentChoice;
            PreventsReroll = preventsReroll; HasPlacementClaim = hasPlacementClaim;
            CanonicalDigest = L.Hash("MAP21_09_OPTIONAL_VARIANT_V1", CanonicalLine);
        }
        public string LandmarkId { get; } public string RecordKind { get; } public string Value { get; }
        public int Order { get; } public bool PersistentChoice { get; }
        public bool PreventsReroll { get; } public bool HasPlacementClaim { get; }
        public bool StaticPayloadOnly => true; public string CanonicalDigest { get; }
        public string CanonicalLine => L.Join(LandmarkId, RecordKind, Value, L.N(Order),
            L.B(PersistentChoice), L.B(PreventsReroll), L.B(HasPlacementClaim), "static_payload_only=true");
    }

    public sealed class MoonPalaceLandmarkProduction
    {
        public const string SchemaVersion = "map21_09.landmark_production.v1";
        public static readonly string[] RequiredLandmarkIds =
            { "MoonSealForge", "BossSealArena", "WanderingMerchantCave", "MaruTimeShrine" };
        public static readonly string[] ForgeProcessStages = { "Grind", "Mix", "Press", "MoonlightCure", "MoonSeal" };
        private readonly ReadOnlyCollection<MoonPalaceLandmarkProfile> profiles;
        private readonly ReadOnlyCollection<MoonPalaceLandmarkChunk> chunks;
        private readonly ReadOnlyCollection<MoonPalaceLandmarkNode> nodes;
        private readonly ReadOnlyCollection<MoonPalaceLandmarkEdge> edges;
        private readonly ReadOnlyCollection<MoonPalaceLandmarkRoute> routes;
        private readonly ReadOnlyCollection<MoonPalaceLandmarkState> states;
        private readonly ReadOnlyCollection<MoonPalaceLandmarkTransition> transitions;
        private readonly ReadOnlyCollection<MoonPalaceLandmarkReset> resets;
        private readonly ReadOnlyCollection<MoonPalaceLandmarkMarker> markers;
        private readonly ReadOnlyCollection<MoonPalaceForgeResourceLedger> forgeLedgers;
        private readonly ReadOnlyCollection<MoonPalaceOptionalLandmarkVariant> optionalVariants;

        public MoonPalaceLandmarkProduction(IEnumerable<MoonPalaceLandmarkProfile> sourceProfiles,
            IEnumerable<MoonPalaceLandmarkChunk> sourceChunks, IEnumerable<MoonPalaceLandmarkNode> sourceNodes,
            IEnumerable<MoonPalaceLandmarkEdge> sourceEdges, IEnumerable<MoonPalaceLandmarkRoute> sourceRoutes,
            IEnumerable<MoonPalaceLandmarkState> sourceStates, IEnumerable<MoonPalaceLandmarkTransition> sourceTransitions,
            IEnumerable<MoonPalaceLandmarkReset> sourceResets, IEnumerable<MoonPalaceLandmarkMarker> sourceMarkers,
            IEnumerable<MoonPalaceForgeResourceLedger> sourceLedgers,
            IEnumerable<MoonPalaceOptionalLandmarkVariant> sourceOptionalVariants, string createdUtc)
        {
            profiles = L.Order(sourceProfiles, x => x.LandmarkId); chunks = L.Order(sourceChunks, x => x.LandmarkId + "/" + x.ChunkId);
            nodes = L.Order(sourceNodes, x => x.LandmarkId + "/" + x.NodeId); edges = L.Order(sourceEdges, x => x.LandmarkId + "/" + x.EdgeId);
            routes = L.Order(sourceRoutes, x => x.LandmarkId + "/" + x.RouteId); states = L.Order(sourceStates, x => x.LandmarkId + "/" + x.StateId);
            transitions = L.Order(sourceTransitions, x => x.LandmarkId + "/" + x.TransitionId); resets = L.Order(sourceResets, x => x.LandmarkId + "/" + x.ResetId);
            markers = L.Order(sourceMarkers, x => x.LandmarkId + "/" + x.MarkerId); forgeLedgers = L.Order(sourceLedgers, x => x.Resource);
            optionalVariants = L.Order(sourceOptionalVariants, x => x.LandmarkId + "/" + x.RecordKind + "/" + x.Value);
            CreatedUtc = createdUtc ?? string.Empty; Validate();
            ProfileSetDigest = SetDigest("MAP21_09_PROFILE_SET_V1", profiles.Select(x => x.CanonicalLine));
            GraphSetDigest = SetDigest("MAP21_09_GRAPH_SET_V1", chunks.Select(x => x.CanonicalLine).Concat(nodes.Select(x => x.CanonicalLine)).Concat(edges.Select(x => x.CanonicalLine)).Concat(routes.Select(x => x.CanonicalLine)));
            StateSetDigest = SetDigest("MAP21_09_STATE_SET_V1", states.Select(x => x.CanonicalLine).Concat(transitions.Select(x => x.CanonicalLine)).Concat(resets.Select(x => x.CanonicalLine)));
            MarkerSetDigest = SetDigest("MAP21_09_MARKER_SET_V1", markers.Select(x => x.CanonicalLine));
            ForgeSetDigest = SetDigest("MAP21_09_FORGE_SET_V1", forgeLedgers.Select(x => x.CanonicalLine));
            OptionalSetDigest = SetDigest("MAP21_09_OPTIONAL_SET_V1", optionalVariants.Select(x => x.CanonicalLine));
            LandmarkManifestDigest = L.Hash("MAP21_09_LANDMARK_MANIFEST_V1", ProfileSetDigest, GraphSetDigest, StateSetDigest, MarkerSetDigest, "created_utc_excluded=true");
            ForgeManifestDigest = L.Hash("MAP21_09_FORGE_MANIFEST_V1", ForgeSetDigest, MoonPalaceLandmarkPreconditions.MoonSealSlot, MoonPalaceLandmarkPreconditions.MoonSealRewardKey, "amount=1", "runtime_executions=0");
            BossManifestDigest = L.Hash("MAP21_09_BOSS_MANIFEST_V1", StateSetDigest, MarkerSetDigest, "seal_consumed=0", "runtime_executions=0");
            OptionalManifestDigest = L.Hash("MAP21_09_OPTIONAL_MANIFEST_V1", OptionalSetDigest, "placement_claims=0", "runtime_executions=0");
        }

        public IReadOnlyList<MoonPalaceLandmarkProfile> Profiles => profiles;
        public IReadOnlyList<MoonPalaceLandmarkChunk> Chunks => chunks;
        public IReadOnlyList<MoonPalaceLandmarkNode> Nodes => nodes;
        public IReadOnlyList<MoonPalaceLandmarkEdge> Edges => edges;
        public IReadOnlyList<MoonPalaceLandmarkRoute> Routes => routes;
        public IReadOnlyList<MoonPalaceLandmarkState> States => states;
        public IReadOnlyList<MoonPalaceLandmarkTransition> Transitions => transitions;
        public IReadOnlyList<MoonPalaceLandmarkReset> Resets => resets;
        public IReadOnlyList<MoonPalaceLandmarkMarker> Markers => markers;
        public IReadOnlyList<MoonPalaceForgeResourceLedger> ForgeLedgers => forgeLedgers;
        public IReadOnlyList<MoonPalaceOptionalLandmarkVariant> OptionalVariants => optionalVariants;
        public string CreatedUtc { get; } public string ProfileSetDigest { get; }
        public string GraphSetDigest { get; } public string StateSetDigest { get; }
        public string MarkerSetDigest { get; } public string ForgeSetDigest { get; }
        public string OptionalSetDigest { get; } public string LandmarkManifestDigest { get; }
        public string ForgeManifestDigest { get; } public string BossManifestDigest { get; }
        public string OptionalManifestDigest { get; }
        public bool CanExecuteRuntimeSideEffects => false;
        public void RequestRuntimeExecution() => throw new InvalidOperationException("MAP21_09 is static production data only.");

        public string SerializeProfilesCsv() => Csv("landmark_id,region_id,schema_version,binding,design_width,design_height,chunk_count,node_count,edge_count,route_count,state_count,transition_count,reset_count,marker_count,footprint_claim,world_origin_claim,reservation_claim,bridge_claim,fixed_slot_claim,placed_ownership_claim,canonical_digest", profiles.Select(x => Row(x.LandmarkId, x.RegionId, SchemaVersion, x.Binding, L.N(x.DesignWidth), L.N(x.DesignHeight), L.N(x.ChunkCount), L.N(x.NodeCount), L.N(x.EdgeCount), L.N(x.RouteCount), L.N(x.StateCount), L.N(x.TransitionCount), L.N(x.ResetCount), L.N(x.MarkerCount), L.B(x.FootprintClaim), L.B(x.WorldOriginClaim), L.B(x.ReservationClaim), L.B(x.BridgeClaim), L.B(x.FixedSlotClaim), L.B(x.PlacedOwnershipClaim), x.CanonicalDigest)));
        public string SerializeChunksCsv() => Csv("landmark_id,chunk_id,chunk_x,chunk_y,local_origin_x,local_origin_y,width,height,coordinate_space,canonical_digest", chunks.Select(x => Row(x.LandmarkId, x.ChunkId, L.N(x.ChunkX), L.N(x.ChunkY), L.N(x.LocalOriginX), L.N(x.LocalOriginY), "12", "8", "DesignLocalLandmark", x.CanonicalDigest)));
        public string SerializeNodesCsv() => Csv("landmark_id,node_id,role,local_x,local_y,required,coordinate_space,canonical_digest", nodes.Select(x => Row(x.LandmarkId, x.NodeId, x.Role, L.N(x.LocalX), L.N(x.LocalY), L.B(x.Required), "DesignLocalLandmark", x.CanonicalDigest)));
        public string SerializeEdgesCsv() => Csv("landmark_id,edge_id,from_node_id,to_node_id,route_kind,order,access_class,required,dependency,canonical_digest", edges.Select(x => Row(x.LandmarkId, x.EdgeId, x.FromNodeId, x.ToNodeId, x.RouteKind, L.N(x.Order), x.AccessClass, L.B(x.Required), x.Dependency, x.CanonicalDigest)));
        public string SerializeRoutesCsv() => Csv("landmark_id,route_id,route_kind,start_node_id,end_node_id,edge_ids,canonical_digest", routes.Select(x => Row(x.LandmarkId, x.RouteId, x.Kind, x.StartNodeId, x.EndNodeId, x.EdgeIds, x.CanonicalDigest)));
        public string SerializeForgeLedgerCsv() => Csv("resource,source_reward_key,available_state_id,reserved_state_id,consumed_state_id,returned_state_id,success_path,failure_path,failure_branch,returns_all_forge_inputs,partial_loss_count,permanent_loss_count,inventory_consume_executions,reward_grant_executions,canonical_digest", forgeLedgers.Select(x => Row(x.Resource, x.SourceRewardKey, x.AvailableStateId, x.ReservedStateId, x.ConsumedStateId, x.ReturnedStateId, "Available>Reserved>Consumed", "Reserved>Returned", x.FailureBranch, L.B(x.ReturnsAllForgeInputs), L.N(x.PartialLossCount), L.N(x.PermanentLossCount), "0", "0", x.CanonicalDigest)));
        public string SerializeBossGateEncounterCsv() => Csv("record_kind,record_id,order,from_or_role,to_or_policy,moon_seal_required,moon_seal_consumed,runtime_execution_count", states.Where(x => x.LandmarkId == "BossSealArena").OrderBy(x => x.Order).Select(x => Row("State", x.StateId, L.N(x.Order), x.Role, "NONE", x.Role == "GateLocked" ? "true" : "false", "0", "0")).Concat(transitions.Where(x => x.LandmarkId == "BossSealArena").OrderBy(x => x.Order).Select(x => Row("Transition", x.TransitionId, L.N(x.Order), x.FromStateId, x.ToStateId, x.Trigger == "PresentMoonSeal" ? "true" : "false", "0", "0"))).Concat(resets.Where(x => x.LandmarkId == "BossSealArena").Select(x => Row("Reset", x.ResetId, "0", x.FromStateId, x.ToStateId, "false", "0", "0"))));
        public string SerializeOptionalLandmarksCsv() => Csv("landmark_id,record_kind,value,order,persistent_choice,prevents_reroll,footprint_claim,world_origin_claim,reservation_claim,bridge_claim,fixed_slot_claim,placed_ownership_claim,runtime_execution_count,canonical_digest", optionalVariants.Select(x => Row(x.LandmarkId, x.RecordKind, x.Value, L.N(x.Order), L.B(x.PersistentChoice), L.B(x.PreventsReroll), "false", "false", "false", "false", "false", "false", "0", x.CanonicalDigest)));
        public string SerializeMarkersCsv() => Csv("landmark_id,marker_id,marker_kind,node_id,state_id_or_none,order,required,dependency,persistence_key_or_none,static_marker_only,canonical_digest", markers.Select(x => Row(x.LandmarkId, x.MarkerId, x.Kind, x.NodeId, x.StateId, L.N(x.Order), L.B(x.Required), x.Dependency, x.PersistenceKey, "true", x.CanonicalDigest)));
        public string SerializeStatePersistenceCsv() => Csv("landmark_id,record_kind,record_id,role_or_trigger_or_policy,order,from_state_or_failure_node,to_state_or_recovery_node,persistent,returns_all_forge_inputs,preserves_seal_acceptance,prevents_reroll,canonical_digest", states.Select(x => Row(x.LandmarkId, "State", x.StateId, x.Role, L.N(x.Order), "NONE", "NONE", L.B(x.Persistent), "false", "false", "false", x.CanonicalDigest)).Concat(transitions.Select(x => Row(x.LandmarkId, "Transition", x.TransitionId, x.Trigger, L.N(x.Order), x.FromStateId, x.ToStateId, "false", "false", "false", "false", x.CanonicalDigest))).Concat(resets.Select(x => Row(x.LandmarkId, "Reset", x.ResetId, x.Policy, "0", x.FailureNodeId, x.RecoveryNodeId, "false", L.B(x.ReturnsAllForgeInputs), L.B(x.PreservesSealAcceptance), L.B(x.PreventsReroll), x.CanonicalDigest))));
        public string SerializeLandmarkManifest() => MoonPalaceCanonical.ToJson(MoonPalaceLandmarkManifestDocument.From(this));
        public string SerializeForgeManifest() => MoonPalaceCanonical.ToJson(MoonPalaceForgeManifestDocument.From(this));
        public string SerializeBossManifest() => MoonPalaceCanonical.ToJson(MoonPalaceBossManifestDocument.From(this));
        public string SerializeOptionalManifest() => MoonPalaceCanonical.ToJson(MoonPalaceOptionalManifestDocument.From(this));

        private void Validate()
        {
            if (profiles.Count != 4 || chunks.Count != 29 || nodes.Count != 39 || edges.Count != 58 ||
                routes.Count != 18 || states.Count != 25 || transitions.Count != 18 || resets.Count != 9 ||
                markers.Count != 31 || forgeLedgers.Count != 3 || optionalVariants.Count != 7)
                throw new ArgumentException("Exact MAP21_09 inventory required.");
            L.Unique(profiles.Select(x => x.LandmarkId), "profile");
            if (!profiles.Select(x => x.LandmarkId).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(RequiredLandmarkIds.OrderBy(x => x, StringComparer.Ordinal), StringComparer.Ordinal))
                throw new ArgumentException("Exact landmark identities required.");
            foreach (var profile in profiles)
            {
                if (chunks.Count(x => x.LandmarkId == profile.LandmarkId) != profile.ChunkCount ||
                    nodes.Count(x => x.LandmarkId == profile.LandmarkId) != profile.NodeCount ||
                    edges.Count(x => x.LandmarkId == profile.LandmarkId) != profile.EdgeCount ||
                    routes.Count(x => x.LandmarkId == profile.LandmarkId) != profile.RouteCount ||
                    states.Count(x => x.LandmarkId == profile.LandmarkId) != profile.StateCount ||
                    transitions.Count(x => x.LandmarkId == profile.LandmarkId) != profile.TransitionCount ||
                    resets.Count(x => x.LandmarkId == profile.LandmarkId) != profile.ResetCount ||
                    markers.Count(x => x.LandmarkId == profile.LandmarkId) != profile.MarkerCount)
                    throw new ArgumentException("Landmark child count mismatch.");
                if (nodes.Any(x => x.LandmarkId == profile.LandmarkId &&
                    (x.LocalX >= profile.DesignWidth || x.LocalY >= profile.DesignHeight)))
                    throw new ArgumentException("Node is outside design-local canvas.");
                if (profile.Binding == "DeferredOptionalLocal" && profile.HasAnyPlacementClaim)
                    throw new ArgumentException("Optional landmark placement claim forbidden.");
            }
            var keys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "MoonCore", "SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD" },
                { "CassiaSap", "SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD" },
                { "StarNuruk", "SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD" },
            };
            if (forgeLedgers.Any(x => !keys.TryGetValue(x.Resource, out var key) || key != x.SourceRewardKey ||
                !x.ReturnsAllForgeInputs || x.PartialLossCount != 0 || x.PermanentLossCount != 0))
                throw new ArgumentException("Forge ledger safety mismatch.");
            var bossStates = states.Where(x => x.LandmarkId == "BossSealArena").OrderBy(x => x.Order).Select(x => x.Role).ToArray();
            if (!bossStates.SequenceEqual(new[] { "GateLocked", "GateAccepted", "EncounterActive", "Defeated" }) ||
                transitions.Count(x => x.LandmarkId == "BossSealArena") != 4 ||
                markers.Count(x => x.LandmarkId == "BossSealArena" && x.Kind == "MoonSealRequirement") != 1 ||
                resets.Count(x => x.LandmarkId == "BossSealArena" && x.Policy == "SafeReturn") != 2 ||
                resets.Count(x => x.LandmarkId == "BossSealArena" && x.Policy == "EncounterReset" &&
                    x.FromStateId == "SL_STATE_BOSS_ENCOUNTER_ACTIVE" && x.ToStateId == "SL_STATE_BOSS_ENCOUNTER_ACTIVE" && x.PreservesSealAcceptance) != 1)
                throw new ArgumentException("Boss gate/recovery contract mismatch.");
            if (optionalVariants.Count(x => x.LandmarkId == "WanderingMerchantCave" && x.RecordKind == "MerchantVariant") != 4 ||
                optionalVariants.Count(x => x.LandmarkId == "MaruTimeShrine" && x.RecordKind == "MaruChoice") != 3 ||
                optionalVariants.Any(x => x.HasPlacementClaim) ||
                !optionalVariants.Where(x => x.LandmarkId == "MaruTimeShrine").All(x => x.PersistentChoice && x.PreventsReroll))
                throw new ArgumentException("Optional landmark contract mismatch.");
            L.Unique(chunks.Select(x => x.LandmarkId + "/" + x.ChunkId), "chunk");
            L.Unique(nodes.Select(x => x.LandmarkId + "/" + x.NodeId), "node");
            L.Unique(edges.Select(x => x.LandmarkId + "/" + x.EdgeId), "edge");
            L.Unique(routes.Select(x => x.LandmarkId + "/" + x.RouteId), "route");
            L.Unique(states.Select(x => x.LandmarkId + "/" + x.StateId), "state");
            L.Unique(transitions.Select(x => x.LandmarkId + "/" + x.TransitionId), "transition");
            L.Unique(resets.Select(x => x.LandmarkId + "/" + x.ResetId), "reset");
            L.Unique(markers.Select(x => x.LandmarkId + "/" + x.MarkerId), "marker");
        }

        public static string SetDigest(string prefix, IEnumerable<string> lines) =>
            L.Hash(new[] { prefix }.Concat((lines ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal)).ToArray());
        private static string Csv(string header, IEnumerable<string> rows) => string.Join("\n", new[] { header }.Concat(rows)) + "\n";
        private static string Row(params string[] values) => string.Join(",", values.Select(L.Escape));
    }

    public sealed class MoonPalaceLandmarkDigestManifest
    {
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> observed;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> csvDigests;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> jsonDigests;
        public MoonPalaceLandmarkDigestManifest(MoonPalaceLandmarkProduction production,
            IEnumerable<MoonPalaceNamedDigest> observedResults, IEnumerable<MoonPalaceNamedDigest> csv,
            IEnumerable<MoonPalaceNamedDigest> json, string createdUtc)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            observed = Copy(observedResults, 6, "observed Result"); csvDigests = Copy(csv, 10, "CSV");
            jsonDigests = Copy(json, 4, "JSON"); CreatedUtc = createdUtc ?? string.Empty;
            Map2110HandoffDigest = L.Hash(new[] { "MAP21_10_LANDMARK_HANDOFF_V1",
                MoonPalaceLandmarkPreconditions.StrictMap2109HandoffDigest,
                MoonPalaceLandmarkPreconditions.SourceMap2107Digest,
                MoonPalaceLandmarkPreconditions.SourceMap2108Digest,
                production.LandmarkManifestDigest, production.ForgeManifestDigest,
                production.BossManifestDigest, production.OptionalManifestDigest }
                .Concat(csvDigests.Select(x => x.CanonicalLine)).Concat(jsonDigests.Select(x => x.CanonicalLine)).ToArray());
            CanonicalDigest = L.Hash(new[] { "map21_09.landmark_digest_manifest.v1",
                MoonPalaceLandmarkPreconditions.TaskId,
                MoonPalaceLandmarkPreconditions.StrictMap2108ResultDigest,
                MoonPalaceLandmarkPreconditions.StrictMap2108TaskDigest,
                MoonPalaceLandmarkPreconditions.StrictMap2109HandoffDigest,
                MoonPalaceLandmarkPreconditions.SourceMap13AuditDigest,
                MoonPalaceLandmarkPreconditions.SourceMap18ExportDigest,
                MoonPalaceLandmarkPreconditions.SourceMap18DebugDigest,
                MoonPalaceLandmarkPreconditions.SourceMap2107Digest,
                MoonPalaceLandmarkPreconditions.SourceMap2108Digest, Map2110HandoffDigest,
                "created_utc_excluded=true" }.Concat(observed.Select(x => x.CanonicalLine))
                .Concat(csvDigests.Select(x => x.CanonicalLine)).Concat(jsonDigests.Select(x => x.CanonicalLine)).ToArray());
        }
        public MoonPalaceLandmarkProduction Production { get; }
        public IReadOnlyList<MoonPalaceNamedDigest> ObservedSourceResultDigests => observed;
        public IReadOnlyList<MoonPalaceNamedDigest> CsvDigests => csvDigests;
        public IReadOnlyList<MoonPalaceNamedDigest> JsonDigests => jsonDigests;
        public string CreatedUtc { get; } public string Map2110HandoffDigest { get; }
        public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(MoonPalaceLandmarkDigestDocument.From(this));
        private static ReadOnlyCollection<MoonPalaceNamedDigest> Copy(IEnumerable<MoonPalaceNamedDigest> source, int expected, string label)
        {
            var values = (source ?? throw new ArgumentNullException(nameof(source))).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            if (values.Length != expected || values.Any(x => x == null) || values.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != expected)
                throw new ArgumentException("Exact " + label + " digest inventory required.");
            return new ReadOnlyCollection<MoonPalaceNamedDigest>(values);
        }
    }

    public sealed class MoonPalaceLandmarkForbiddenOperationCounters
    {
        public static MoonPalaceLandmarkForbiddenOperationCounters Zero => new MoonPalaceLandmarkForbiddenOperationCounters();
        public int ItemConsumes => 0; public int RewardGrants => 0; public int InventoryMutations => 0;
        public int ResourceMutations => 0; public int BossAiExecutions => 0; public int CombatExecutions => 0;
        public int PhysicsExecutions => 0; public int ShopTransactions => 0; public int MaruRuntimeSearchExecutions => 0;
        public int SaveFileWrites => 0; public int SaveFileReads => 0; public int PlayerPrefsWrites => 0;
        public int PlayerPrefsReads => 0; public int WorldSectorPlacements => 0; public int GenerationRunnerExecutions => 0;
        public int RendererExecutions => 0; public int ValidationRunnerExecutions => 0; public int ReplayExecutions => 0;
        public int RollbackExecutions => 0; public int TilemapWrites => 0; public int RuntimeObjectSpawns => 0;
        public int ScenePrefabChanges => 0; public int ColliderAddressablesChanges => 0; public int PriorCategorySelections => 0;
        public int PlayModeSelections => 0; public int LegacyRegressionSelections => 0; public int UnfilteredOrFullRegressionSelections => 0;
        public int UpstreamRegenerationRuns => 0;
        public bool AllZero => ItemConsumes + RewardGrants + InventoryMutations + ResourceMutations + BossAiExecutions +
            CombatExecutions + PhysicsExecutions + ShopTransactions + MaruRuntimeSearchExecutions + SaveFileWrites +
            SaveFileReads + PlayerPrefsWrites + PlayerPrefsReads + WorldSectorPlacements + GenerationRunnerExecutions +
            RendererExecutions + ValidationRunnerExecutions + ReplayExecutions + RollbackExecutions + TilemapWrites +
            RuntimeObjectSpawns + ScenePrefabChanges + ColliderAddressablesChanges + PriorCategorySelections +
            PlayModeSelections + LegacyRegressionSelections + UnfilteredOrFullRegressionSelections + UpstreamRegenerationRuns == 0;
    }

    internal static class L
    {
        public static string Required(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
        public static string OrNone(string value) => string.IsNullOrWhiteSpace(value) ? "NONE" : value.Trim();
        public static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        public static string B(bool value) => value ? "true" : "false";
        public static string Join(params string[] values) => MoonPalaceCanonical.Join(values);
        public static string Hash(params string[] values) => BakingCanonicalDigest.HashCanonicalLines(values);
        public static string Escape(string value) { var text = value ?? string.Empty; return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\""; }
        public static ReadOnlyCollection<T> Order<T>(IEnumerable<T> source, Func<T, string> key) { var values = (source ?? throw new ArgumentNullException(nameof(source))).ToArray(); if (values.Any(x => x == null)) throw new ArgumentException("Null record."); return new ReadOnlyCollection<T>(values.OrderBy(key, StringComparer.Ordinal).ToArray()); }
        public static void Unique(IEnumerable<string> source, string label) { var values = source.ToArray(); if (values.Distinct(StringComparer.Ordinal).Count() != values.Length) throw new ArgumentException("Duplicate " + label + "."); }
    }

    [Serializable] internal sealed class MoonPalaceLandmarkManifestDocument
    {
        public string schema_version; public string publication_kind; public int landmark_profile_count;
        public int placed_mandatory_count; public int deferred_optional_count; public int design_chunk_count;
        public int node_count; public int edge_count; public int route_count; public int state_count;
        public int transition_count; public int reset_count; public int marker_count; public int optional_world_placement_claim_count;
        public string profile_set_digest; public string graph_set_digest; public string state_set_digest;
        public string marker_set_digest; public string canonical_digest;
        public static MoonPalaceLandmarkManifestDocument From(MoonPalaceLandmarkProduction x) => new MoonPalaceLandmarkManifestDocument
        {
            schema_version = MoonPalaceLandmarkProduction.SchemaVersion, publication_kind = "StaticProductionDataNotRuntimeState",
            landmark_profile_count = x.Profiles.Count, placed_mandatory_count = x.Profiles.Count(v => v.Binding == "PlacedMandatorySite"),
            deferred_optional_count = x.Profiles.Count(v => v.Binding == "DeferredOptionalLocal"), design_chunk_count = x.Chunks.Count,
            node_count = x.Nodes.Count, edge_count = x.Edges.Count, route_count = x.Routes.Count, state_count = x.States.Count,
            transition_count = x.Transitions.Count, reset_count = x.Resets.Count, marker_count = x.Markers.Count,
            optional_world_placement_claim_count = x.Profiles.Count(v => v.Binding == "DeferredOptionalLocal" && v.HasAnyPlacementClaim),
            profile_set_digest = x.ProfileSetDigest, graph_set_digest = x.GraphSetDigest, state_set_digest = x.StateSetDigest,
            marker_set_digest = x.MarkerSetDigest, canonical_digest = x.LandmarkManifestDigest,
        };
    }

    [Serializable] internal sealed class MoonPalaceForgeManifestDocument
    {
        public string schema_version; public string[] process_order; public int resource_ledger_count;
        public int ledger_state_count; public int ledger_transition_count; public int manual_reset_count;
        public int returns_all_input_count; public int partial_loss_count; public int permanent_loss_count;
        public string moon_seal_slot; public string moon_seal_reward_key; public int moon_seal_amount;
        public bool moon_seal_required; public int inventory_consume_executions; public int reward_grant_executions;
        public string canonical_digest;
        public static MoonPalaceForgeManifestDocument From(MoonPalaceLandmarkProduction x) => new MoonPalaceForgeManifestDocument
        {
            schema_version = MoonPalaceLandmarkProduction.SchemaVersion, process_order = MoonPalaceLandmarkProduction.ForgeProcessStages,
            resource_ledger_count = x.ForgeLedgers.Count, ledger_state_count = x.ForgeLedgers.Count * 4,
            ledger_transition_count = x.Transitions.Count(v => v.LandmarkId == "MoonSealForge"),
            manual_reset_count = x.Resets.Count(v => v.LandmarkId == "MoonSealForge" && v.Policy == "ManualReset"),
            returns_all_input_count = x.Resets.Count(v => v.LandmarkId == "MoonSealForge" && v.ReturnsAllForgeInputs),
            partial_loss_count = x.ForgeLedgers.Sum(v => v.PartialLossCount), permanent_loss_count = x.ForgeLedgers.Sum(v => v.PermanentLossCount),
            moon_seal_slot = MoonPalaceLandmarkPreconditions.MoonSealSlot, moon_seal_reward_key = MoonPalaceLandmarkPreconditions.MoonSealRewardKey,
            moon_seal_amount = 1, moon_seal_required = true, inventory_consume_executions = 0, reward_grant_executions = 0,
            canonical_digest = x.ForgeManifestDigest,
        };
    }

    [Serializable] internal sealed class MoonPalaceBossManifestDocument
    {
        public string schema_version; public string[] gate_state_order; public int state_count; public int transition_count;
        public int moon_seal_requirement_marker_count; public int moon_seal_consumed_count; public int failure_recovery_count;
        public string central_recovery_target; public bool preserves_seal_acceptance; public bool introduces_new_movement_rule;
        public int boss_ai_executions; public int combat_executions; public int physics_executions; public string canonical_digest;
        public static MoonPalaceBossManifestDocument From(MoonPalaceLandmarkProduction x) => new MoonPalaceBossManifestDocument
        {
            schema_version = MoonPalaceLandmarkProduction.SchemaVersion,
            gate_state_order = x.States.Where(v => v.LandmarkId == "BossSealArena").OrderBy(v => v.Order).Select(v => v.Role).ToArray(),
            state_count = x.States.Count(v => v.LandmarkId == "BossSealArena"), transition_count = x.Transitions.Count(v => v.LandmarkId == "BossSealArena"),
            moon_seal_requirement_marker_count = x.Markers.Count(v => v.LandmarkId == "BossSealArena" && v.Kind == "MoonSealRequirement"),
            moon_seal_consumed_count = 0, failure_recovery_count = x.Resets.Count(v => v.LandmarkId == "BossSealArena" && v.Policy == "SafeReturn"),
            central_recovery_target = "SL_NODE_BOSS_CENTRAL_RECOVERY", preserves_seal_acceptance = true,
            introduces_new_movement_rule = false, boss_ai_executions = 0, combat_executions = 0, physics_executions = 0,
            canonical_digest = x.BossManifestDigest,
        };
    }

    [Serializable] internal sealed class MoonPalaceOptionalManifestDocument
    {
        public string schema_version; public int merchant_variant_count; public string[] merchant_variants;
        public int maru_choice_count; public string[] maru_choices; public bool persistent_choice; public bool prevents_reroll;
        public int optional_placement_claim_count; public int progression_blocker_count; public int required_reward_dependency_count;
        public int shop_transaction_count; public int maru_runtime_search_count; public string canonical_digest;
        public static MoonPalaceOptionalManifestDocument From(MoonPalaceLandmarkProduction x) => new MoonPalaceOptionalManifestDocument
        {
            schema_version = MoonPalaceLandmarkProduction.SchemaVersion,
            merchant_variant_count = x.OptionalVariants.Count(v => v.RecordKind == "MerchantVariant"),
            merchant_variants = x.OptionalVariants.Where(v => v.RecordKind == "MerchantVariant").OrderBy(v => v.Order).Select(v => v.Value).ToArray(),
            maru_choice_count = x.OptionalVariants.Count(v => v.RecordKind == "MaruChoice"),
            maru_choices = x.OptionalVariants.Where(v => v.RecordKind == "MaruChoice").OrderBy(v => v.Order).Select(v => v.Value).ToArray(),
            persistent_choice = true, prevents_reroll = true, optional_placement_claim_count = x.OptionalVariants.Count(v => v.HasPlacementClaim),
            progression_blocker_count = 0, required_reward_dependency_count = 0, shop_transaction_count = 0,
            maru_runtime_search_count = 0, canonical_digest = x.OptionalManifestDigest,
        };
    }

    [Serializable] internal sealed class MoonPalaceLandmarkDigestDocument
    {
        public string schema_version; public string task_id; public string strict_map21_08_result_sha256;
        public string strict_map21_08_installed_task_sha256; public string strict_map21_09_handoff_digest;
        public MoonPalaceLandmarkNamedDigestDocument[] observed_source_result_sha256;
        public string source_map13_audit_digest; public string source_map18_export_digest; public string source_map18_debug_digest;
        public string source_map21_07_digest; public string source_map21_08_digest;
        public MoonPalaceLandmarkNamedDigestDocument[] csv_digests; public MoonPalaceLandmarkNamedDigestDocument[] json_digests;
        public string MAP21_10_handoff_digest; public bool created_utc_excluded_from_canonical_digest;
        public string created_utc; public string canonical_digest;
        public static MoonPalaceLandmarkDigestDocument From(MoonPalaceLandmarkDigestManifest x) => new MoonPalaceLandmarkDigestDocument
        {
            schema_version = "map21_09.landmark_digest_manifest.v1", task_id = MoonPalaceLandmarkPreconditions.TaskId,
            strict_map21_08_result_sha256 = MoonPalaceLandmarkPreconditions.StrictMap2108ResultDigest,
            strict_map21_08_installed_task_sha256 = MoonPalaceLandmarkPreconditions.StrictMap2108TaskDigest,
            strict_map21_09_handoff_digest = MoonPalaceLandmarkPreconditions.StrictMap2109HandoffDigest,
            observed_source_result_sha256 = x.ObservedSourceResultDigests.Select(MoonPalaceLandmarkNamedDigestDocument.From).ToArray(),
            source_map13_audit_digest = MoonPalaceLandmarkPreconditions.SourceMap13AuditDigest,
            source_map18_export_digest = MoonPalaceLandmarkPreconditions.SourceMap18ExportDigest,
            source_map18_debug_digest = MoonPalaceLandmarkPreconditions.SourceMap18DebugDigest,
            source_map21_07_digest = MoonPalaceLandmarkPreconditions.SourceMap2107Digest,
            source_map21_08_digest = MoonPalaceLandmarkPreconditions.SourceMap2108Digest,
            csv_digests = x.CsvDigests.Select(MoonPalaceLandmarkNamedDigestDocument.From).ToArray(),
            json_digests = x.JsonDigests.Select(MoonPalaceLandmarkNamedDigestDocument.From).ToArray(),
            MAP21_10_handoff_digest = x.Map2110HandoffDigest, created_utc_excluded_from_canonical_digest = true,
            created_utc = x.CreatedUtc, canonical_digest = x.CanonicalDigest,
        };
    }

    [Serializable] internal sealed class MoonPalaceLandmarkNamedDigestDocument
    {
        public string name; public string sha256;
        public static MoonPalaceLandmarkNamedDigestDocument From(MoonPalaceNamedDigest x) =>
            new MoonPalaceLandmarkNamedDigestDocument { name = x.Name, sha256 = x.Digest };
    }
}
