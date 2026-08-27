using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Pipeline
{
    public static class V2PassCatalog
    {
        private static readonly ReadOnlyCollection<V2PassContract> entries =
            new ReadOnlyCollection<V2PassContract>(CreateEntries());

        private static readonly ReadOnlyCollection<V2InfrastructureFailureRule> infrastructureFailureRules =
            new ReadOnlyCollection<V2InfrastructureFailureRule>(new[]
            {
                new V2InfrastructureFailureRule(
                    V2InfrastructureFailureKind.Configuration,
                    V2FailureOwner.CatalogConfiguration),
                new V2InfrastructureFailureRule(
                    V2InfrastructureFailureKind.Schema,
                    V2FailureOwner.AuthoringSchema),
                new V2InfrastructureFailureRule(
                    V2InfrastructureFailureKind.Baseline,
                    V2FailureOwner.ApprovedBaseline),
            });

        static V2PassCatalog()
        {
            var validation = V2PassCatalogValidator.Validate(entries);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    "The built-in V2 pass catalog is invalid: " +
                    string.Join("; ", validation.Issues.Select(value => value.ToString())));
            }
        }

        public static IReadOnlyList<V2PassContract> Entries => entries;
        public static IReadOnlyList<V2InfrastructureFailureRule> InfrastructureFailureRules =>
            infrastructureFailureRules;
        public static string StableDigest => ComputeStableDigest(entries);

        public static string ComputeStableDigest(IEnumerable<V2PassContract> contracts)
        {
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));

            var material = contracts
                .Select(value => value ?? throw new ArgumentException(
                    "Catalog entries cannot contain null.", nameof(contracts)))
                .OrderBy(value => value.PassId.ToString(), StringComparer.Ordinal)
                .Select(CanonicalDigestRecord);
            var bytes = Encoding.UTF8.GetBytes(string.Join("\n", material));
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }

        private static string CanonicalDigestRecord(V2PassContract value)
        {
            return string.Join("|", new[]
            {
                value.PassId.ToString(),
                value.Order.ToString(),
                JoinStableIds(value.InputArtifactIds),
                JoinStableIds(value.OutputArtifactIds),
                value.FailureOwner.ToString(),
                value.FailurePolicy.ToString(),
                value.RetryScope.ToString(),
                string.Join(",", value.RetryEscalation.Select(item => item.ToString())),
                value.RngStream.ToString(),
                value.PreservesValidatedCanvasOnFailure ? "PRESERVE_VALIDATED_CANVAS" : "NONE",
            });
        }

        private static string JoinStableIds(IEnumerable<V2WorldGenerationArtifactId> values)
        {
            return string.Join(",", values
                .Select(value => value.ToString())
                .OrderBy(value => value, StringComparer.Ordinal));
        }

        private static V2PassContract[] CreateEntries()
        {
            return new[]
            {
                Pass(
                    V2WorldGenerationPassId.Pacing,
                    10,
                    V2WorldGenerationArtifactId.ApprovedMapBaseline,
                    V2WorldGenerationArtifactId.PacingPlan,
                    V2FailureOwner.PacingPlanner,
                    V2FailurePolicy.ImmediateFailure,
                    V2RetryScope.None,
                    V2RngStreamId.Pacing,
                    "V2_PASS_PACING"),
                Pass(
                    V2WorldGenerationPassId.SpecialRegionReservation,
                    20,
                    V2WorldGenerationArtifactId.PacingPlan,
                    V2WorldGenerationArtifactId.SpecialRegionReservationPlan,
                    V2FailureOwner.SpecialRegionPlanner,
                    V2FailurePolicy.ReselectWithinScope,
                    V2RetryScope.Footprint,
                    V2RngStreamId.SpecialRegionReservation,
                    "V2_PASS_SPECIAL_REGION_RESERVATION"),
                Pass(
                    V2WorldGenerationPassId.TerrainClusterReservation,
                    30,
                    V2WorldGenerationArtifactId.SpecialRegionReservationPlan,
                    V2WorldGenerationArtifactId.TerrainClusterPlacementPlan,
                    V2FailureOwner.TerrainClusterPlanner,
                    V2FailurePolicy.ReselectWithinScope,
                    V2RetryScope.Cluster,
                    V2RngStreamId.TerrainClusterReservation,
                    "V2_PASS_TERRAIN_CLUSTER_RESERVATION"),
                Pass(
                    V2WorldGenerationPassId.RouteSpine,
                    40,
                    V2WorldGenerationArtifactId.TerrainClusterPlacementPlan,
                    V2WorldGenerationArtifactId.RouteSpinePlan,
                    V2FailureOwner.RouteSpinePlanner,
                    V2FailurePolicy.ReselectWithinScope,
                    V2RetryScope.Footprint,
                    V2RngStreamId.RouteSpine,
                    "V2_PASS_ROUTE_SPINE"),
                Pass(
                    V2WorldGenerationPassId.TraversalEnvelope,
                    50,
                    V2WorldGenerationArtifactId.RouteSpinePlan,
                    V2WorldGenerationArtifactId.TraversalEnvelopePlan,
                    V2FailureOwner.TraversalEnvelopePlanner,
                    V2FailurePolicy.ImmediateFailure,
                    V2RetryScope.None,
                    V2RngStreamId.None,
                    "V2_PASS_TRAVERSAL_ENVELOPE"),
                Pass(
                    V2WorldGenerationPassId.MicroPattern,
                    60,
                    V2WorldGenerationArtifactId.TraversalEnvelopePlan,
                    V2WorldGenerationArtifactId.PatternApplicationPlan,
                    V2FailureOwner.MicroPatternPlanner,
                    V2FailurePolicy.ReselectWithinScope,
                    V2RetryScope.Pattern,
                    V2RngStreamId.MicroPattern,
                    "V2_PASS_MICRO_PATTERN"),
                Pass(
                    V2WorldGenerationPassId.TerrainCleanup,
                    70,
                    V2WorldGenerationArtifactId.PatternApplicationPlan,
                    V2WorldGenerationArtifactId.CleanTerrainCanvas,
                    V2FailureOwner.TerrainCleanupPlanner,
                    V2FailurePolicy.ReselectWithinScope,
                    V2RetryScope.Pattern,
                    V2RngStreamId.None,
                    "V2_PASS_TERRAIN_CLEANUP"),
                Pass(
                    V2WorldGenerationPassId.ActivityEventOverlay,
                    80,
                    V2WorldGenerationArtifactId.CleanTerrainCanvas,
                    V2WorldGenerationArtifactId.ActivityEventPlacementPlan,
                    V2FailureOwner.ActivityEventPlanner,
                    V2FailurePolicy.ImmediateFailure,
                    V2RetryScope.None,
                    V2RngStreamId.ActivityEventOverlay,
                    "V2_PASS_ACTIVITY_EVENT_OVERLAY"),
                new V2PassContract(
                    V2WorldGenerationPassId.TileValidation,
                    90,
                    new[] { V2WorldGenerationArtifactId.ActivityEventPlacementPlan },
                    new[] { V2WorldGenerationArtifactId.ValidatedSectorCanvas },
                    V2FailureOwner.TileValidator,
                    V2FailurePolicy.OrderedEscalation,
                    V2RetryScope.Pattern,
                    new[] { V2RetryScope.Pattern, V2RetryScope.Cluster, V2RetryScope.Footprint },
                    V2RngStreamId.None,
                    "V2_PASS_TILE_VALIDATION"),
                new V2PassContract(
                    V2WorldGenerationPassId.MicroChunkSlice,
                    100,
                    new[] { V2WorldGenerationArtifactId.ValidatedSectorCanvas },
                    new[] { V2WorldGenerationArtifactId.GeneratedMicroChunkSlices },
                    V2FailureOwner.MicroChunkSlicer,
                    V2FailurePolicy.ImmediateFailure,
                    V2RetryScope.None,
                    Array.Empty<V2RetryScope>(),
                    V2RngStreamId.None,
                    "V2_PASS_MICRO_CHUNK_SLICE",
                    preservesValidatedCanvasOnFailure: true),
            };
        }

        private static V2PassContract Pass(
            V2WorldGenerationPassId passId,
            int order,
            V2WorldGenerationArtifactId input,
            V2WorldGenerationArtifactId output,
            V2FailureOwner failureOwner,
            V2FailurePolicy failurePolicy,
            V2RetryScope retryScope,
            V2RngStreamId rngStream,
            string descriptionId)
        {
            return new V2PassContract(
                passId,
                order,
                new[] { input },
                new[] { output },
                failureOwner,
                failurePolicy,
                retryScope,
                Array.Empty<V2RetryScope>(),
                rngStream,
                descriptionId);
        }
    }
}
