using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace
{
    public static class MoonPalaceCoreResourcePreconditions
    {
        public const string TaskId = "MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS";
        public const string SourceMap1309ResultDigest = "637fec406f42bf845be5ae9313a036b3ec49f66467539a3552c1f94ad68bd5e2";
        public const string SourceMap13AuditDigest = "a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e";
        public const string SourceMap1806ResultDigest = "ad2b88be043cb7e18289909a7ad44d76c9143a65e5228c11c24f1a60b86831fd";
        public const string SourceMap18ExportDigest = "358ac8cfe78eec502db049f8940ed0c71458179b89bb451680e837b0797b77b5";
        public const string SourceMap18DebugDigest = "59efb7fd30df9ec62014cadd04a111b222e7dd13e298789dbab88a661bea22ed";
        public const string SourceMap2104Digest = "d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72";
        public const string SourceMap2105Digest = "645f95b8c58757179223b8e5b219b60b5a732cb898a69f99594178c76f166a1e";
        public const string SourceMap2106Digest = "955ad4acb3e2294b3dc7c018c21472bf4ef34bbfc4075685324901179bd652ab";
        public const string SourceMap2107HandoffDigest = "c7c47d7dfe5928fe7e5f10f382ce2ccc3b9d20f218c1512c3e1ae60bb74e101b";
    }

    public sealed class MoonPalaceCoreResourceChunk
    {
        public MoonPalaceCoreResourceChunk(string regionId, string chunkId, int chunkX,
            int chunkY)
        {
            RegionId = Required(regionId); ChunkId = Required(chunkId);
            if (chunkX < 0 || chunkX >= 3 || chunkY < 0 || chunkY >= 2)
                throw new ArgumentOutOfRangeException("chunk coordinate");
            ChunkX = chunkX; ChunkY = chunkY; LocalOriginX = chunkX * 12;
            LocalOriginY = chunkY * 8;
            CanonicalDigest = Hash("MAP21_07_CORE_CHUNK_V1", CanonicalLine);
        }
        public string RegionId { get; }
        public string ChunkId { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public int LocalOriginX { get; }
        public int LocalOriginY { get; }
        public int Width => 12;
        public int Height => 8;
        public bool Active => true;
        public string CanonicalDigest { get; }
        public string CanonicalLine => Join(RegionId, ChunkId, Number(ChunkX), Number(ChunkY),
            Number(LocalOriginX), Number(LocalOriginY), "12", "8", "true");
        private static string Required(string value) => MoonPalaceCoreResourceCanonical.Required(value);
        private static string Hash(params string[] lines) => BakingCanonicalDigest.HashCanonicalLines(lines);
        private static string Join(params string[] values) => MoonPalaceCoreResourceCanonical.Join(values);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class MoonPalaceCoreResourceNode
    {
        public MoonPalaceCoreResourceNode(string regionId, string nodeId, string routeId,
            string routeKind, string nodeRole, int localX, int localY, int order,
            bool required, string accessClass, string mechanismToken,
            string dependencyKind, string targetRewardSlotOrNone)
        {
            RegionId = R(regionId); NodeId = R(nodeId); RouteId = R(routeId);
            RouteKind = R(routeKind); NodeRole = R(nodeRole);
            if (localX < 0 || localX >= 36 || localY < 0 || localY >= 16)
                throw new ArgumentOutOfRangeException("local coordinate");
            LocalX = localX; LocalY = localY; Order = order; Required = required;
            AccessClass = R(accessClass); MechanismToken = R(mechanismToken);
            DependencyKind = R(dependencyKind); TargetRewardSlotOrNone = R(targetRewardSlotOrNone);
            CanonicalDigest = H("MAP21_07_CORE_NODE_V1", CanonicalLine);
        }
        public string RegionId { get; } public string NodeId { get; }
        public string RouteId { get; } public string RouteKind { get; }
        public string NodeRole { get; } public int LocalX { get; } public int LocalY { get; }
        public int Order { get; } public bool Required { get; } public string AccessClass { get; }
        public string MechanismToken { get; } public string DependencyKind { get; }
        public string TargetRewardSlotOrNone { get; } public string CanonicalDigest { get; }
        public string CanonicalLine => J(RegionId, NodeId, RouteId, RouteKind, NodeRole,
            N(LocalX), N(LocalY), N(Order), Required ? "true" : "false", AccessClass,
            MechanismToken, DependencyKind, TargetRewardSlotOrNone);
        private static string R(string v) => MoonPalaceCoreResourceCanonical.Required(v);
        private static string H(params string[] v) => BakingCanonicalDigest.HashCanonicalLines(v);
        private static string J(params string[] v) => MoonPalaceCoreResourceCanonical.Join(v);
        private static string N(int v) => v.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class MoonPalaceCoreResourceEdge
    {
        public MoonPalaceCoreResourceEdge(string regionId, string edgeId, string routeId,
            string routeKind, string edgeRole, string fromNodeId, string toNodeId,
            int localX, int localY, int order, bool required, string accessClass,
            string mechanismToken, string dependencyKind, string targetRewardSlotOrNone)
        {
            RegionId = R(regionId); EdgeId = R(edgeId); RouteId = R(routeId);
            RouteKind = R(routeKind); EdgeRole = R(edgeRole); FromNodeId = R(fromNodeId);
            ToNodeId = R(toNodeId);
            if (localX < 0 || localX >= 36 || localY < 0 || localY >= 16)
                throw new ArgumentOutOfRangeException("local coordinate");
            LocalX = localX; LocalY = localY; Order = order; Required = required;
            AccessClass = R(accessClass); MechanismToken = R(mechanismToken);
            DependencyKind = R(dependencyKind); TargetRewardSlotOrNone = R(targetRewardSlotOrNone);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_07_CORE_EDGE_V1", CanonicalLine });
        }
        public string RegionId { get; } public string EdgeId { get; } public string RouteId { get; }
        public string RouteKind { get; } public string EdgeRole { get; } public string FromNodeId { get; }
        public string ToNodeId { get; } public int LocalX { get; } public int LocalY { get; }
        public int Order { get; } public bool Required { get; } public string AccessClass { get; }
        public string MechanismToken { get; } public string DependencyKind { get; }
        public string TargetRewardSlotOrNone { get; } public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCoreResourceCanonical.Join(RegionId, EdgeId,
            RouteId, RouteKind, EdgeRole, FromNodeId, ToNodeId, N(LocalX), N(LocalY), N(Order),
            Required ? "true" : "false", AccessClass, MechanismToken, DependencyKind,
            TargetRewardSlotOrNone);
        private static string R(string v) => MoonPalaceCoreResourceCanonical.Required(v);
        private static string N(int v) => v.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class MoonPalaceCoreResourceReward
    {
        public MoonPalaceCoreResourceReward(string regionId, string rewardId,
            string rewardKind, string nodeId, string resourceKind, string slotIdOrNone,
            string persistenceKeyOrNone, string benefitKindOrNone, int amount, bool required)
        {
            RegionId = R(regionId); RewardId = R(rewardId); RewardKind = R(rewardKind);
            NodeId = R(nodeId); ResourceKind = R(resourceKind); SlotIdOrNone = R(slotIdOrNone);
            PersistenceKeyOrNone = R(persistenceKeyOrNone); BenefitKindOrNone = R(benefitKindOrNone);
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Amount = amount; Required = required;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_07_CORE_REWARD_V1", CanonicalLine });
        }
        public string RegionId { get; } public string RewardId { get; } public string RewardKind { get; }
        public string NodeId { get; } public string ResourceKind { get; } public string SlotIdOrNone { get; }
        public string PersistenceKeyOrNone { get; } public string BenefitKindOrNone { get; }
        public int Amount { get; } public bool Required { get; } public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCoreResourceCanonical.Join(RegionId, RewardId,
            RewardKind, NodeId, ResourceKind, SlotIdOrNone, PersistenceKeyOrNone,
            BenefitKindOrNone, Amount.ToString(CultureInfo.InvariantCulture), Required ? "true" : "false");
        private static string R(string v) => MoonPalaceCoreResourceCanonical.Required(v);
    }

    public sealed class MoonPalaceCoreResourceCheckpoint
    {
        public MoonPalaceCoreResourceCheckpoint(string regionId, string checkpointId,
            string checkpointKind, string rewardKey, string availabilityState,
            string claimState, bool duplicateRisk, bool permanentLossRisk)
        {
            RegionId = R(regionId); CheckpointId = R(checkpointId); CheckpointKind = R(checkpointKind);
            RequiredRewardPersistenceKey = R(rewardKey); AvailabilityState = R(availabilityState);
            ClaimState = R(claimState); DuplicateRisk = duplicateRisk; PermanentLossRisk = permanentLossRisk;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_07_CORE_CHECKPOINT_V1", CanonicalLine });
        }
        public string RegionId { get; } public string CheckpointId { get; }
        public string CheckpointKind { get; } public string RequiredRewardPersistenceKey { get; }
        public string AvailabilityState { get; } public string ClaimState { get; }
        public bool DuplicateRisk { get; } public bool PermanentLossRisk { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCoreResourceCanonical.Join(RegionId, CheckpointId,
            CheckpointKind, RequiredRewardPersistenceKey, AvailabilityState, ClaimState,
            DuplicateRisk ? "true" : "false", PermanentLossRisk ? "true" : "false");
        private static string R(string v) => MoonPalaceCoreResourceCanonical.Required(v);
    }

    public sealed class MoonPalaceCoreResourceProfile
    {
        public const string SchemaVersion = "map21_07.core_resource_profile.v1";
        public MoonPalaceCoreResourceProfile(string regionId, string resourceKind, string biomeId,
            string mechanismKind, string lowRouteId, string highRouteId, string failureBranchId,
            string recoveryRouteId, string rewardSlotId, string rewardKey,
            string chunkDigest, string graphDigest, string rewardDigest, string persistenceDigest)
        {
            RegionId = R(regionId); ResourceKind = R(resourceKind); BiomeId = R(biomeId);
            MechanismKind = R(mechanismKind); LowRouteId = R(lowRouteId); HighRouteId = R(highRouteId);
            FailureBranchId = R(failureBranchId); RecoveryRouteId = R(recoveryRouteId);
            RequiredRewardSlotId = R(rewardSlotId); RequiredRewardPersistenceKey = R(rewardKey);
            ChunkDigest = D(chunkDigest); GraphDigest = D(graphDigest); RewardDigest = D(rewardDigest);
            PersistenceDigest = D(persistenceDigest);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_07_CORE_PROFILE_V1", CanonicalLine });
        }
        public string RegionId { get; } public string ResourceKind { get; } public string BiomeId { get; }
        public string MechanismKind { get; } public int DesignCanvasWidth => 36;
        public int DesignCanvasHeight => 16; public int ActiveChunkCount => 5;
        public string LowRouteId { get; } public string HighRouteId { get; }
        public string FailureBranchId { get; } public string RecoveryRouteId { get; }
        public string RequiredRewardSlotId { get; } public string RequiredRewardPersistenceKey { get; }
        public int OptionalBenefitCount => 2; public int PersistenceCheckpointCount => 7;
        public string SourceMap13AuditDigest => MoonPalaceCoreResourcePreconditions.SourceMap13AuditDigest;
        public string SourceMap18ExportDigest => MoonPalaceCoreResourcePreconditions.SourceMap18ExportDigest;
        public string ChunkDigest { get; } public string GraphDigest { get; }
        public string RewardDigest { get; } public string PersistenceDigest { get; }
        public string CanonicalDigest { get; }
        public string CanonicalLine => MoonPalaceCoreResourceCanonical.Join(RegionId, SchemaVersion,
            ResourceKind, BiomeId, MechanismKind, "36", "16", "5", LowRouteId, HighRouteId,
            FailureBranchId, RecoveryRouteId, RequiredRewardSlotId, RequiredRewardPersistenceKey,
            "2", "7", SourceMap13AuditDigest, SourceMap18ExportDigest, ChunkDigest, GraphDigest,
            RewardDigest, PersistenceDigest);
        private static string R(string v) => MoonPalaceCoreResourceCanonical.Required(v);
        private static string D(string v) => MoonPalaceCoreResourceCanonical.Digest(v);
    }

    public sealed class MoonPalaceCoreResourceProduction
    {
        public const string SchemaVersion = "map21_07.core_resource_production.v1";
        private static readonly string[] RegionIds = { "SR_MOON_CORE_SITE_5", "SR_CASSIA_SAP_SITE_5", "SR_STAR_NURUK_SITE_5" };
        private static readonly string[] CheckpointStates = { "InitialAvailable", "InterruptedAvailable", "FailedAvailable", "RegeneratedAvailable", "Claimed", "RevisitedClaimed", "RecoveryJoinAvailable" };
        private readonly ReadOnlyCollection<MoonPalaceCoreResourceProfile> profiles;
        private readonly ReadOnlyCollection<MoonPalaceCoreResourceChunk> chunks;
        private readonly ReadOnlyCollection<MoonPalaceCoreResourceNode> nodes;
        private readonly ReadOnlyCollection<MoonPalaceCoreResourceEdge> edges;
        private readonly ReadOnlyCollection<MoonPalaceCoreResourceReward> rewards;
        private readonly ReadOnlyCollection<MoonPalaceCoreResourceCheckpoint> checkpoints;

        public MoonPalaceCoreResourceProduction(IEnumerable<MoonPalaceCoreResourceProfile> sourceProfiles,
            IEnumerable<MoonPalaceCoreResourceChunk> sourceChunks, IEnumerable<MoonPalaceCoreResourceNode> sourceNodes,
            IEnumerable<MoonPalaceCoreResourceEdge> sourceEdges, IEnumerable<MoonPalaceCoreResourceReward> sourceRewards,
            IEnumerable<MoonPalaceCoreResourceCheckpoint> sourceCheckpoints, string createdUtc)
        {
            profiles = Order(sourceProfiles, x => x.RegionId); chunks = Order(sourceChunks, x => x.RegionId + "/" + x.ChunkId);
            nodes = Order(sourceNodes, x => x.RegionId + "/" + x.NodeId); edges = Order(sourceEdges, x => x.RegionId + "/" + x.EdgeId);
            rewards = Order(sourceRewards, x => x.RegionId + "/" + x.RewardId); checkpoints = Order(sourceCheckpoints, x => x.RegionId + "/" + x.CheckpointId);
            CreatedUtc = createdUtc ?? string.Empty; Validate();
            ProfileSetDigest = SetDigest("MAP21_07_PROFILE_SET_V1", profiles.Select(x => x.CanonicalLine));
            ChunkSetDigest = SetDigest("MAP21_07_CHUNK_SET_V1", chunks.Select(x => x.CanonicalLine));
            GraphSetDigest = SetDigest("MAP21_07_GRAPH_SET_V1", nodes.Select(x => x.CanonicalLine).Concat(edges.Select(x => x.CanonicalLine)));
            RewardSetDigest = SetDigest("MAP21_07_REWARD_SET_V1", rewards.Select(x => x.CanonicalLine));
            PersistenceSetDigest = SetDigest("MAP21_07_PERSISTENCE_SET_V1", checkpoints.Select(x => x.CanonicalLine));
            ResourceManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_07_RESOURCE_MANIFEST_V1", ProfileSetDigest, ChunkSetDigest, RewardSetDigest, "created_utc_excluded=true" });
            RouteManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_07_ROUTE_MANIFEST_V1", GraphSetDigest, "created_utc_excluded=true" });
            PersistenceManifestDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_07_PERSISTENCE_MANIFEST_V1", PersistenceSetDigest, "created_utc_excluded=true" });
        }

        public static IReadOnlyList<string> RequiredRegionIds => RegionIds;
        public static IReadOnlyList<string> RequiredCheckpointStates => CheckpointStates;
        public IReadOnlyList<MoonPalaceCoreResourceProfile> Profiles => profiles;
        public IReadOnlyList<MoonPalaceCoreResourceChunk> Chunks => chunks;
        public IReadOnlyList<MoonPalaceCoreResourceNode> Nodes => nodes;
        public IReadOnlyList<MoonPalaceCoreResourceEdge> Edges => edges;
        public IReadOnlyList<MoonPalaceCoreResourceReward> Rewards => rewards;
        public IReadOnlyList<MoonPalaceCoreResourceCheckpoint> Checkpoints => checkpoints;
        public string CreatedUtc { get; } public string ProfileSetDigest { get; }
        public string ChunkSetDigest { get; } public string GraphSetDigest { get; }
        public string RewardSetDigest { get; } public string PersistenceSetDigest { get; }
        public string ResourceManifestDigest { get; } public string RouteManifestDigest { get; }
        public string PersistenceManifestDigest { get; }
        public bool CanExecuteRuntimeSideEffects => false;
        public void RequestRuntimeExecution() => throw new InvalidOperationException("MAP21_07 is static data only.");

        public string SerializeProfilesCsv() => Csv("region_id,schema_version,resource_kind,biome_id,mechanism_kind,design_canvas_width,design_canvas_height,active_chunk_count,low_route_id,high_route_id,failure_branch_id,recovery_route_id,required_reward_slot_id,required_reward_persistence_key,optional_benefit_count,persistence_checkpoint_count,source_map13_audit_digest,source_map18_export_digest,chunk_digest,graph_digest,reward_digest,persistence_digest,canonical_digest", profiles.Select(x => Row(x.RegionId, MoonPalaceCoreResourceProfile.SchemaVersion, x.ResourceKind, x.BiomeId, x.MechanismKind, "36", "16", "5", x.LowRouteId, x.HighRouteId, x.FailureBranchId, x.RecoveryRouteId, x.RequiredRewardSlotId, x.RequiredRewardPersistenceKey, "2", "7", x.SourceMap13AuditDigest, x.SourceMap18ExportDigest, x.ChunkDigest, x.GraphDigest, x.RewardDigest, x.PersistenceDigest, x.CanonicalDigest)));
        public string SerializeChunksCsv() => Csv("region_id,chunk_id,chunk_x,chunk_y,local_origin_x,local_origin_y,width,height,active,canonical_digest", chunks.Select(x => Row(x.RegionId, x.ChunkId, N(x.ChunkX), N(x.ChunkY), N(x.LocalOriginX), N(x.LocalOriginY), "12", "8", "true", x.CanonicalDigest)));
        public string SerializeNodesCsv() => Csv("region_id,node_id_or_edge_id,route_id,route_kind,node_role_or_edge_role,local_x,local_y,order,required,access_class,mechanism_token,dependency_kind,target_reward_slot_or_none,canonical_digest", nodes.Select(x => Row(x.RegionId, x.NodeId, x.RouteId, x.RouteKind, x.NodeRole, N(x.LocalX), N(x.LocalY), N(x.Order), x.Required ? "true" : "false", x.AccessClass, x.MechanismToken, x.DependencyKind, x.TargetRewardSlotOrNone, x.CanonicalDigest)));
        public string SerializeEdgesCsv() => Csv("region_id,node_id_or_edge_id,route_id,route_kind,node_role_or_edge_role,from_node_id,to_node_id,local_x,local_y,order,required,access_class,mechanism_token,dependency_kind,target_reward_slot_or_none,canonical_digest", edges.Select(x => Row(x.RegionId, x.EdgeId, x.RouteId, x.RouteKind, x.EdgeRole, x.FromNodeId, x.ToNodeId, N(x.LocalX), N(x.LocalY), N(x.Order), x.Required ? "true" : "false", x.AccessClass, x.MechanismToken, x.DependencyKind, x.TargetRewardSlotOrNone, x.CanonicalDigest)));
        public string SerializeRewardsCsv() => Csv("region_id,reward_id,reward_kind,node_id,resource_kind,slot_id_or_none,persistence_key_or_none,benefit_kind_or_none,amount,required,canonical_digest", rewards.Select(x => Row(x.RegionId, x.RewardId, x.RewardKind, x.NodeId, x.ResourceKind, x.SlotIdOrNone, x.PersistenceKeyOrNone, x.BenefitKindOrNone, N(x.Amount), x.Required ? "true" : "false", x.CanonicalDigest)));
        public string SerializePersistenceCsv() => Csv("region_id,checkpoint_id,checkpoint_kind,required_reward_persistence_key,availability_state,claim_state,duplicate_risk,permanent_loss_risk,canonical_digest", checkpoints.Select(x => Row(x.RegionId, x.CheckpointId, x.CheckpointKind, x.RequiredRewardPersistenceKey, x.AvailabilityState, x.ClaimState, x.DuplicateRisk ? "true" : "false", x.PermanentLossRisk ? "true" : "false", x.CanonicalDigest)));
        public string SerializeResourceManifest() => MoonPalaceCanonical.ToJson(MoonPalaceCoreResourceManifestDocument.From(this));
        public string SerializeRouteManifest() => MoonPalaceCanonical.ToJson(MoonPalaceCoreRouteManifestDocument.From(this));
        public string SerializePersistenceManifest() => MoonPalaceCanonical.ToJson(MoonPalaceCorePersistenceManifestDocument.From(this));

        private void Validate()
        {
            if (profiles.Count != 3 || chunks.Count != 15 || nodes.Count != 44 || edges.Count != 51 || rewards.Count != 9 || checkpoints.Count != 21) throw new ArgumentException("Exact MAP21_07 inventory required.");
            RequireUnique(profiles.Select(x => x.RegionId), "region"); RequireUnique(chunks.Select(x => x.RegionId + "/" + x.ChunkId), "chunk");
            RequireUnique(nodes.Select(x => x.RegionId + "/" + x.NodeId), "node"); RequireUnique(edges.Select(x => x.RegionId + "/" + x.EdgeId), "edge");
            RequireUnique(rewards.Select(x => x.RegionId + "/" + x.RewardId), "reward"); RequireUnique(checkpoints.Select(x => x.RegionId + "/" + x.CheckpointId), "checkpoint");
            if (!profiles.Select(x => x.RegionId).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(RegionIds.OrderBy(x => x, StringComparer.Ordinal), StringComparer.Ordinal)) throw new ArgumentException("Exact regions required.");
            foreach (var profile in profiles)
            {
                var identityValid = profile.RegionId == "SR_MOON_CORE_SITE_5" &&
                        profile.ResourceKind == "MoonCore" && profile.BiomeId == "MoonCrater" &&
                        profile.MechanismKind == "ImpactChain" ||
                    profile.RegionId == "SR_CASSIA_SAP_SITE_5" &&
                        profile.ResourceKind == "CassiaSap" && profile.BiomeId == "CassiaRoot" &&
                        profile.MechanismKind == "WaterChannel" ||
                    profile.RegionId == "SR_STAR_NURUK_SITE_5" &&
                        profile.ResourceKind == "StarNuruk" && profile.BiomeId == "MoonDough" &&
                        profile.MechanismKind == "FermentationPressure";
                if (!identityValid) throw new ArgumentException("CoreResource region identity mismatch.");
                var regionChunks = chunks.Where(x => x.RegionId == profile.RegionId).ToArray();
                var regionNodes = nodes.Where(x => x.RegionId == profile.RegionId).ToArray();
                var regionEdges = edges.Where(x => x.RegionId == profile.RegionId).ToArray();
                var regionRewards = rewards.Where(x => x.RegionId == profile.RegionId).ToArray();
                var regionCheckpoints = checkpoints.Where(x => x.RegionId == profile.RegionId).ToArray();
                var expectedNodes = profile.ResourceKind == "MoonCore" ? 14 : 15;
                var expectedEdges = profile.ResourceKind == "MoonCore" ? 16 : profile.ResourceKind == "CassiaSap" ? 17 : 18;
                var expectedLow = profile.ResourceKind == "MoonCore" ? 5 : profile.ResourceKind == "CassiaSap" ? 7 : 8;
                var expectedHigh = profile.ResourceKind == "MoonCore" ? 8 : 7;
                if (regionChunks.Length != 5 || regionChunks.Select(x => x.ChunkX + "/" + x.ChunkY).Distinct(StringComparer.Ordinal).Count() != 5 || regionNodes.Length != expectedNodes || regionEdges.Length != expectedEdges) throw new ArgumentException("Region canvas/graph count mismatch.");
                if (regionEdges.Count(x => x.RouteKind == "Low") != expectedLow || regionEdges.Count(x => x.RouteKind == "High") != expectedHigh || regionEdges.Count(x => x.RouteKind == "Failure") != 1 || regionEdges.Count(x => x.RouteKind == "Recovery") != 2) throw new ArgumentException("Route count mismatch.");
                var nodeIds = new HashSet<string>(regionNodes.Select(x => x.NodeId), StringComparer.Ordinal);
                if (regionEdges.Any(x => !nodeIds.Contains(x.FromNodeId) || !nodeIds.Contains(x.ToNodeId) || x.DependencyKind == "Village" || x.DependencyKind == "Inventory")) throw new ArgumentException("Unknown graph node or forbidden dependency.");
                var low = regionEdges.Where(x => x.RouteKind == "Low").OrderBy(x => x.Order).ToArray();
                if (low.Any(x => !x.Required || x.AccessClass != "MandatoryNoTool" || x.DependencyKind != "None") || !IsChain(low) || Role(regionNodes, low[0].FromNodeId) != "Entry" || Role(regionNodes, low[low.Length - 1].ToNodeId) != "Return" || !low.Any(x => Role(regionNodes, x.ToNodeId) == "RequiredReward")) throw new ArgumentException("Low route is not mandatory reward-and-return chain.");
                var high = regionEdges.Where(x => x.RouteKind == "High").OrderBy(x => x.Order).ToArray();
                if (high.Any(x => x.Required) || !IsChain(high) || Role(regionNodes, high[0].FromNodeId) != "Entry" || !high.Any(x => Role(regionNodes, x.ToNodeId) == "RequiredReward" || low.Any(lowEdge => lowEdge.FromNodeId == x.ToNodeId || lowEdge.ToNodeId == x.ToNodeId))) throw new ArgumentException("High route convergence mismatch.");
                var failure = regionEdges.Single(x => x.RouteKind == "Failure");
                var recovery = regionEdges.Where(x => x.RouteKind == "Recovery").OrderBy(x => x.Order).ToArray();
                if (Role(regionNodes, failure.ToNodeId) != "Failure" || !IsChain(recovery) || recovery[0].FromNodeId != failure.ToNodeId || Role(regionNodes, recovery[1].ToNodeId) != "RecoveryJoin" || !low.Any(x => x.FromNodeId == recovery[1].ToNodeId || x.ToNodeId == recovery[1].ToNodeId)) throw new ArgumentException("RecoveryJoin closure mismatch.");
                var requiredReward = regionRewards.Where(x => x.Required).ToArray();
                if (requiredReward.Length != 1 || regionRewards.Count(x => !x.Required) != 2 || requiredReward[0].PersistenceKeyOrNone != profile.RequiredRewardPersistenceKey || !requiredReward[0].PersistenceKeyOrNone.StartsWith("SR_STATE_", StringComparison.Ordinal) || requiredReward[0].PersistenceKeyOrNone.Count(c => c == '_') < 7) throw new ArgumentException("Reward/persistence key mismatch.");
                if (regionCheckpoints.Length != 7 || !regionCheckpoints.Select(x => x.CheckpointKind).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(CheckpointStates.OrderBy(x => x, StringComparer.Ordinal), StringComparer.Ordinal) || regionCheckpoints.Any(x => x.RequiredRewardPersistenceKey != profile.RequiredRewardPersistenceKey || x.DuplicateRisk || x.PermanentLossRisk)) throw new ArgumentException("Persistence checkpoint mismatch.");
                if (profile.ChunkDigest != SetDigest("MAP21_07_REGION_CHUNKS_V1", regionChunks.Select(x => x.CanonicalLine)) || profile.GraphDigest != SetDigest("MAP21_07_REGION_GRAPH_V1", regionNodes.Select(x => x.CanonicalLine).Concat(regionEdges.Select(x => x.CanonicalLine))) || profile.RewardDigest != SetDigest("MAP21_07_REGION_REWARDS_V1", regionRewards.Select(x => x.CanonicalLine)) || profile.PersistenceDigest != SetDigest("MAP21_07_REGION_PERSISTENCE_V1", regionCheckpoints.Select(x => x.CanonicalLine))) throw new ArgumentException("Profile child digest mismatch.");
            }
        }

        private static string Role(IEnumerable<MoonPalaceCoreResourceNode> values, string id) => values.Single(x => x.NodeId == id).NodeRole;
        private static bool IsChain(IReadOnlyList<MoonPalaceCoreResourceEdge> values) { for (var i = 1; i < values.Count; i++) if (values[i - 1].ToNodeId != values[i].FromNodeId) return false; return values.Count > 0; }
        public static string SetDigest(string prefix, IEnumerable<string> lines) => BakingCanonicalDigest.HashCanonicalLines(new[] { prefix }.Concat((lines ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal)));
        private static ReadOnlyCollection<T> Order<T>(IEnumerable<T> source, Func<T, string> key) { var values = (source ?? throw new ArgumentNullException(nameof(source))).ToArray(); if (values.Any(x => x == null)) throw new ArgumentException("Null record."); return new ReadOnlyCollection<T>(values.OrderBy(key, StringComparer.Ordinal).ToArray()); }
        private static void RequireUnique(IEnumerable<string> source, string label) { var values = source.ToArray(); if (values.Distinct(StringComparer.Ordinal).Count() != values.Length) throw new ArgumentException("Duplicate " + label + "."); }
        private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Csv(string header, IEnumerable<string> rows) => string.Join("\n", new[] { header }.Concat(rows)) + "\n";
        private static string Row(params string[] values) => string.Join(",", values.Select(MoonPalaceCoreResourceCanonical.Escape));
    }

    public sealed class MoonPalaceCoreResourceDigestManifest
    {
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> csvDigests;
        private readonly ReadOnlyCollection<MoonPalaceNamedDigest> jsonDigests;
        public MoonPalaceCoreResourceDigestManifest(MoonPalaceCoreResourceProduction production,
            IEnumerable<MoonPalaceNamedDigest> sourceCsvDigests, IEnumerable<MoonPalaceNamedDigest> sourceJsonDigests, string createdUtc)
        {
            Production = production ?? throw new ArgumentNullException(nameof(production));
            csvDigests = Copy(sourceCsvDigests, 6); jsonDigests = Copy(sourceJsonDigests, 3); CreatedUtc = createdUtc ?? string.Empty;
            Map2108HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "MAP21_08_CORE_RESOURCE_HANDOFF_V1", MoonPalaceCoreResourcePreconditions.SourceMap2107HandoffDigest,
                MoonPalaceCoreResourcePreconditions.SourceMap13AuditDigest, MoonPalaceCoreResourcePreconditions.SourceMap18ExportDigest,
                MoonPalaceCoreResourcePreconditions.SourceMap2104Digest, MoonPalaceCoreResourcePreconditions.SourceMap2105Digest,
                MoonPalaceCoreResourcePreconditions.SourceMap2106Digest, production.ResourceManifestDigest,
                production.RouteManifestDigest, production.PersistenceManifestDigest }.Concat(csvDigests.Select(x => x.CanonicalLine)).Concat(jsonDigests.Select(x => x.CanonicalLine)));
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[] { "map21_07.core_resource_digest_manifest.v1", MoonPalaceCoreResourcePreconditions.TaskId,
                MoonPalaceCoreResourcePreconditions.SourceMap13AuditDigest, MoonPalaceCoreResourcePreconditions.SourceMap18ExportDigest,
                MoonPalaceCoreResourcePreconditions.SourceMap18DebugDigest, MoonPalaceCoreResourcePreconditions.SourceMap2104Digest,
                MoonPalaceCoreResourcePreconditions.SourceMap2105Digest, MoonPalaceCoreResourcePreconditions.SourceMap2106Digest,
                Map2108HandoffDigest, "created_utc_excluded=true" }.Concat(csvDigests.Select(x => x.CanonicalLine)).Concat(jsonDigests.Select(x => x.CanonicalLine)));
        }
        public MoonPalaceCoreResourceProduction Production { get; } public IReadOnlyList<MoonPalaceNamedDigest> CsvDigests => csvDigests;
        public IReadOnlyList<MoonPalaceNamedDigest> JsonDigests => jsonDigests; public string CreatedUtc { get; }
        public string Map2108HandoffDigest { get; } public string CanonicalDigest { get; }
        public string Serialize() => MoonPalaceCanonical.ToJson(MoonPalaceCoreDigestDocument.From(this));
        private static ReadOnlyCollection<MoonPalaceNamedDigest> Copy(IEnumerable<MoonPalaceNamedDigest> source, int count) { var values = (source ?? throw new ArgumentNullException(nameof(source))).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray(); if (values.Length != count || values.Any(x => x == null) || values.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != count) throw new ArgumentException("Digest inventory mismatch."); return new ReadOnlyCollection<MoonPalaceNamedDigest>(values); }
    }

    public sealed class MoonPalaceCoreResourceForbiddenOperationCounters
    {
        public static MoonPalaceCoreResourceForbiddenOperationCounters Zero => new MoonPalaceCoreResourceForbiddenOperationCounters();
        public int DeviceMonoBehaviours => 0; public int PhysicsSimulations => 0; public int RewardGrants => 0;
        public int InventoryMutations => 0; public int SaveFileWrites => 0; public int SaveFileReads => 0;
        public int PlayerPrefsWrites => 0; public int PlayerPrefsReads => 0; public int WorldSectorPlacements => 0;
        public int GenerationRunnerExecutions => 0; public int RendererExecutions => 0; public int ValidationRunnerExecutions => 0;
        public int ReplayExecutions => 0; public int RollbackExecutions => 0; public int TilemapWrites => 0;
        public int RuntimeObjectSpawns => 0; public int ScenePrefabChanges => 0; public int ColliderAddressablesChanges => 0;
        public int PriorCategorySelections => 0; public int PlayModeSelections => 0; public int LegacyRegressionSelections => 0;
        public int UnfilteredOrFullRegressionSelections => 0; public int UpstreamRegenerationRuns => 0;
        public bool AllZero => DeviceMonoBehaviours + PhysicsSimulations + RewardGrants + InventoryMutations + SaveFileWrites + SaveFileReads + PlayerPrefsWrites + PlayerPrefsReads + WorldSectorPlacements + GenerationRunnerExecutions + RendererExecutions + ValidationRunnerExecutions + ReplayExecutions + RollbackExecutions + TilemapWrites + RuntimeObjectSpawns + ScenePrefabChanges + ColliderAddressablesChanges + PriorCategorySelections + PlayModeSelections + LegacyRegressionSelections + UnfilteredOrFullRegressionSelections + UpstreamRegenerationRuns == 0;
    }

    internal static class MoonPalaceCoreResourceCanonical
    {
        public static string Required(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required value."); return value.Trim(); }
        public static string Digest(string value) { if (!BakingCanonicalDigest.IsLowerHexSha256(value)) throw new ArgumentException("Lower-hex SHA-256 required."); return value; }
        public static string Join(params string[] values) => string.Join("/", (values ?? Array.Empty<string>()).Select(value => { var text = value ?? string.Empty; return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text; }));
        public static string Escape(string value) { var text = value ?? string.Empty; return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\""; }
    }

    [Serializable] internal sealed class MoonPalaceCoreResourceManifestDocument
    {
        public string schema_version; public string publication_kind; public int profile_count; public int active_chunk_count; public int required_reward_count; public int optional_benefit_count; public string profile_set_digest; public string chunk_set_digest; public string reward_set_digest; public string canonical_digest;
        public static MoonPalaceCoreResourceManifestDocument From(MoonPalaceCoreResourceProduction x) => new MoonPalaceCoreResourceManifestDocument { schema_version = MoonPalaceCoreResourceProduction.SchemaVersion, publication_kind = "StaticProductionDataNotRuntimeState", profile_count = x.Profiles.Count, active_chunk_count = x.Chunks.Count, required_reward_count = x.Rewards.Count(r => r.Required), optional_benefit_count = x.Rewards.Count(r => !r.Required), profile_set_digest = x.ProfileSetDigest, chunk_set_digest = x.ChunkSetDigest, reward_set_digest = x.RewardSetDigest, canonical_digest = x.ResourceManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceCoreRouteManifestDocument
    {
        public string schema_version; public int node_count; public int edge_count; public int low_edge_count; public int high_edge_count; public int failure_edge_count; public int recovery_edge_count; public int mandatory_no_tool_low_count; public int recovery_join_closure_count; public string graph_set_digest; public string canonical_digest;
        public static MoonPalaceCoreRouteManifestDocument From(MoonPalaceCoreResourceProduction x) => new MoonPalaceCoreRouteManifestDocument { schema_version = MoonPalaceCoreResourceProduction.SchemaVersion, node_count = x.Nodes.Count, edge_count = x.Edges.Count, low_edge_count = x.Edges.Count(e => e.RouteKind == "Low"), high_edge_count = x.Edges.Count(e => e.RouteKind == "High"), failure_edge_count = x.Edges.Count(e => e.RouteKind == "Failure"), recovery_edge_count = x.Edges.Count(e => e.RouteKind == "Recovery"), mandatory_no_tool_low_count = x.Profiles.Count, recovery_join_closure_count = x.Profiles.Count, graph_set_digest = x.GraphSetDigest, canonical_digest = x.RouteManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceCorePersistenceManifestDocument
    {
        public string schema_version; public int checkpoint_count; public int legacy_short_keys_accepted; public int duplicate_reward_risk_count; public int permanent_loss_count; public string[] checkpoint_states; public string persistence_set_digest; public string canonical_digest;
        public static MoonPalaceCorePersistenceManifestDocument From(MoonPalaceCoreResourceProduction x) => new MoonPalaceCorePersistenceManifestDocument { schema_version = MoonPalaceCoreResourceProduction.SchemaVersion, checkpoint_count = x.Checkpoints.Count, legacy_short_keys_accepted = 0, duplicate_reward_risk_count = x.Checkpoints.Count(c => c.DuplicateRisk), permanent_loss_count = x.Checkpoints.Count(c => c.PermanentLossRisk), checkpoint_states = MoonPalaceCoreResourceProduction.RequiredCheckpointStates.ToArray(), persistence_set_digest = x.PersistenceSetDigest, canonical_digest = x.PersistenceManifestDigest };
    }
    [Serializable] internal sealed class MoonPalaceCoreDigestDocument
    {
        public string schema_version; public string task_id; public string source_map13_audit_digest; public string source_map18_export_digest; public string source_map18_debug_digest; public string source_map21_04_digest; public string source_map21_05_digest; public string source_map21_06_digest; public MoonPalaceCoreNamedDigestDocument[] csv_digests; public MoonPalaceCoreNamedDigestDocument[] json_digests; public string MAP21_08_handoff_digest; public bool created_utc_excluded_from_canonical_digest; public string created_utc; public string canonical_digest;
        public static MoonPalaceCoreDigestDocument From(MoonPalaceCoreResourceDigestManifest x) => new MoonPalaceCoreDigestDocument { schema_version = "map21_07.core_resource_digest_manifest.v1", task_id = MoonPalaceCoreResourcePreconditions.TaskId, source_map13_audit_digest = MoonPalaceCoreResourcePreconditions.SourceMap13AuditDigest, source_map18_export_digest = MoonPalaceCoreResourcePreconditions.SourceMap18ExportDigest, source_map18_debug_digest = MoonPalaceCoreResourcePreconditions.SourceMap18DebugDigest, source_map21_04_digest = MoonPalaceCoreResourcePreconditions.SourceMap2104Digest, source_map21_05_digest = MoonPalaceCoreResourcePreconditions.SourceMap2105Digest, source_map21_06_digest = MoonPalaceCoreResourcePreconditions.SourceMap2106Digest, csv_digests = x.CsvDigests.Select(MoonPalaceCoreNamedDigestDocument.From).ToArray(), json_digests = x.JsonDigests.Select(MoonPalaceCoreNamedDigestDocument.From).ToArray(), MAP21_08_handoff_digest = x.Map2108HandoffDigest, created_utc_excluded_from_canonical_digest = true, created_utc = x.CreatedUtc, canonical_digest = x.CanonicalDigest };
    }
    [Serializable] internal sealed class MoonPalaceCoreNamedDigestDocument { public string name; public string sha256; public static MoonPalaceCoreNamedDigestDocument From(MoonPalaceNamedDigest x) => new MoonPalaceCoreNamedDigestDocument { name = x.Name, sha256 = x.Digest }; }
}
