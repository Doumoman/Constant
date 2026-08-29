using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public static class SpecialRegionValidationAuditor
    {
        private sealed class ExpectedArtifact
        {
            public ExpectedArtifact(
                int order,
                string id,
                SpecialRegionAuditFamily family,
                SpecialRegionAuditBinding binding)
            {
                Order = order;
                Id = id;
                Family = family;
                Binding = binding;
            }

            public int Order { get; }
            public string Id { get; }
            public SpecialRegionAuditFamily Family { get; }
            public SpecialRegionAuditBinding Binding { get; }
        }

        private static readonly ReadOnlyCollection<ExpectedArtifact> Expected =
            new ReadOnlyCollection<ExpectedArtifact>(new[]
        {
            new ExpectedArtifact(0, "SR_MAP13_08_VILLAGE_1X1", SpecialRegionAuditFamily.Village, SpecialRegionAuditBinding.ReferenceFixture),
            new ExpectedArtifact(1, "SR_MAP13_08_VILLAGE_1X2", SpecialRegionAuditFamily.Village, SpecialRegionAuditBinding.ReferenceFixture),
            new ExpectedArtifact(2, "SR_MAP13_08_VILLAGE_2X1", SpecialRegionAuditFamily.Village, SpecialRegionAuditBinding.ReferenceFixture),
            new ExpectedArtifact(3, "SR_CASSIA_SAP_SITE_5", SpecialRegionAuditFamily.CoreResource, SpecialRegionAuditBinding.ReferenceFixture),
            new ExpectedArtifact(4, "SR_MOON_CORE_SITE_5", SpecialRegionAuditFamily.CoreResource, SpecialRegionAuditBinding.ReferenceFixture),
            new ExpectedArtifact(5, "SR_STAR_NURUK_SITE_5", SpecialRegionAuditFamily.CoreResource, SpecialRegionAuditBinding.ReferenceFixture),
            new ExpectedArtifact(6, "SR_MARU_TIME_SHRINE_5", SpecialRegionAuditFamily.Landmark, SpecialRegionAuditBinding.DeferredToMAP14),
            new ExpectedArtifact(7, "SR_MOON_BOSS_SEAL_ARENA_12", SpecialRegionAuditFamily.Landmark, SpecialRegionAuditBinding.ReferenceFixture),
            new ExpectedArtifact(8, "SR_MOON_SEAL_FORGE_9", SpecialRegionAuditFamily.Landmark, SpecialRegionAuditBinding.ReferenceFixture),
            new ExpectedArtifact(9, "SR_WANDERING_MERCHANT_CAVE_3", SpecialRegionAuditFamily.Landmark, SpecialRegionAuditBinding.DeferredToMAP14),
        });

        public static SpecialRegionValidationAuditResult Audit(SpecialRegionAuditRequest request)
        {
            var errors = new List<SpecialRegionValidationAuditError>();
            if (request == null)
                return Failure(SpecialRegionValidationAuditErrorCode.MissingInput, "request", "Audit request is required.");

            var supplied = request.Artifacts.Where(value => value != null).ToArray();
            if (supplied.Length != request.Artifacts.Count)
                Add(errors, SpecialRegionValidationAuditErrorCode.MissingInput, "artifacts", "Null artifact input is not allowed.");

            foreach (var group in supplied.GroupBy(value => value.ArtifactId, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1))
                Add(errors, SpecialRegionValidationAuditErrorCode.DuplicateArtifact,
                    "artifacts/" + group.Key, "Artifact identity is duplicated.");

            foreach (var expected in Expected)
            {
                var matches = supplied.Where(value => string.Equals(value.ArtifactId, expected.Id, StringComparison.Ordinal)).ToArray();
                if (matches.Length == 0)
                    Add(errors, SpecialRegionValidationAuditErrorCode.MissingArtifact,
                        "artifacts/" + expected.Id, "Canonical artifact is missing.");
                if (matches.Length == 1)
                    ValidateIdentity(matches[0], expected, errors);
            }

            foreach (var input in supplied)
            {
                if (!Expected.Any(value => string.Equals(value.Id, input.ArtifactId, StringComparison.Ordinal)))
                    Add(errors, SpecialRegionValidationAuditErrorCode.IdentityMismatch,
                        "artifacts/" + input.ArtifactId, "Artifact is outside the canonical ten-item matrix.");
                ValidateArtifact(input, errors);
            }

            ValidateAggregateSlotKinds(supplied, errors);

            if (errors.Count != 0)
                return new SpecialRegionValidationAuditResult(null, errors, string.Empty);

            var artifactResults = supplied.OrderBy(value => value.CanonicalOrder)
                .Select(BuildArtifactResult).ToArray();
            var report = new SpecialRegionValidationReport(artifactResults, string.Empty);
            var reportDigest = SpecialRegionValidationCanonicalDigest.ComputeReport(report);
            report = new SpecialRegionValidationReport(artifactResults, reportDigest);
            return new SpecialRegionValidationAuditResult(report, Array.Empty<SpecialRegionValidationAuditError>(), reportDigest);
        }

        private static void ValidateIdentity(
            SpecialRegionAuditArtifactInput input,
            ExpectedArtifact expected,
            ICollection<SpecialRegionValidationAuditError> errors)
        {
            if (input.CanonicalOrder != expected.Order || input.Family != expected.Family || input.Binding != expected.Binding)
                Add(errors, SpecialRegionValidationAuditErrorCode.IdentityMismatch,
                    "artifacts/" + expected.Id + "/identity",
                    "Canonical order, family, and binding must match the published matrix.");
        }

        private static void ValidateArtifact(
            SpecialRegionAuditArtifactInput input,
            ICollection<SpecialRegionValidationAuditError> errors)
        {
            var path = "artifacts/" + input.ArtifactId;
            var metrics = input.Metrics;
            if (metrics == null)
            {
                Add(errors, SpecialRegionValidationAuditErrorCode.MissingInput, path + "/metrics", "Audit metrics are required.");
                return;
            }

            if (!metrics.IdentityMatches || string.IsNullOrEmpty(input.ArtifactId))
                Add(errors, SpecialRegionValidationAuditErrorCode.IdentityMismatch, path + "/identity", "Source identity does not match the artifact.");
            if (!metrics.DigestsMatch || !Digest(input.SourceDigest) || !Digest(input.ComponentDigest) || !Digest(input.ArtifactDigest))
                Add(errors, SpecialRegionValidationAuditErrorCode.DigestMismatch, path + "/digests", "Source, component, and artifact digests must be stable lowercase SHA-256 values.");
            if (!metrics.FootprintMatches || input.FootprintWidth < 0 || input.FootprintHeight < 0)
                Add(errors, SpecialRegionValidationAuditErrorCode.FootprintMismatch, path + "/footprint", "Footprint dimensions do not match the source plan.");

            if (input.Binding == SpecialRegionAuditBinding.ReferenceFixture)
            {
                var expectedCoverage = input.FootprintWidth * input.FootprintHeight;
                if (metrics.SectorCoverageCount != expectedCoverage || expectedCoverage == 0)
                    Add(errors, SpecialRegionValidationAuditErrorCode.MissingSectorCoverage,
                        path + "/footprint/coverage", "Every reserved sector needs source and placed coverage.");
                if (expectedCoverage > 1 && metrics.SeamCrossingCount == 0)
                    Add(errors, SpecialRegionValidationAuditErrorCode.MissingSeamCrossing,
                        path + "/footprint/seam", "Multi-sector reference fixtures need internal seam evidence.");
                if (metrics.WorldOriginClaimCount != 1 || metrics.ReservationClaimCount != 1 ||
                    metrics.BridgeClaimCount != 1 || metrics.PlacedOwnershipClaimCount != 1)
                    Add(errors, SpecialRegionValidationAuditErrorCode.SiteBindingMismatch,
                        path + "/binding", "Reference fixture placed claims must each be exactly one.");
                if (input.FixedCollisionCount == 0)
                    Add(errors, SpecialRegionValidationAuditErrorCode.CollisionOwnerMismatch,
                        path + "/fixedCollision", "Reference fixture needs immutable fixed-collision evidence.");
                if (input.FixedAccessCount == 0)
                    Add(errors, SpecialRegionValidationAuditErrorCode.SiteBindingMismatch,
                        path + "/fixedAccess", "Reference fixture needs immutable fixed-access evidence.");
            }
            else if (metrics.WorldOriginClaimCount != 0 || metrics.ReservationClaimCount != 0 ||
                     metrics.BridgeClaimCount != 0 || metrics.PlacedOwnershipClaimCount != 0 ||
                     input.FootprintWidth != 0 || input.FootprintHeight != 0 || metrics.SectorCoverageCount != 0)
                Add(errors, SpecialRegionValidationAuditErrorCode.DeferredWorldClaim,
                    path + "/binding", "DEFERRED TO MAP14 artifacts may not publish world placement claims.");

            if (!metrics.SiteBindingMatches)
                Add(errors, SpecialRegionValidationAuditErrorCode.SiteBindingMismatch, path + "/binding", "Site binding evidence drifted.");
            if (!metrics.BufferMatches)
                Add(errors, SpecialRegionValidationAuditErrorCode.BufferMismatch, path + "/buffer", "Entry, Return, apron, or quiet-buffer evidence drifted.");
            if (!metrics.CollisionOwnerMatches)
                Add(errors, SpecialRegionValidationAuditErrorCode.CollisionOwnerMismatch, path + "/collision", "Collision priority/owner evidence drifted.");
            if (metrics.FixedReplaceableOverlapCount != 0)
                Add(errors, SpecialRegionValidationAuditErrorCode.FixedReplaceableOverlap, path + "/layers", "Fixed and replaceable coordinates overlap.");
            if (!metrics.PersistenceMatches)
                Add(errors, SpecialRegionValidationAuditErrorCode.PersistenceMismatch, path + "/persistence", "Slot, scope, or persistence key identity drifted.");
            if (input.Routes.Count == 0)
                Add(errors, SpecialRegionValidationAuditErrorCode.MissingRouteWitness, path + "/routes", "At least one ordered route witness is required.");
            if (!metrics.RouteOrderMatches || input.Routes.Any(value => !value.Ordered || value.NodeIds.Count < 2))
                Add(errors, SpecialRegionValidationAuditErrorCode.RouteOrderMismatch, path + "/routes/order", "Ordered Entry-to-Return route proof failed.");
            if (metrics.MandatoryToolDependencyCount != 0 || input.Routes.Any(value => !value.MandatoryNoTool))
                Add(errors, SpecialRegionValidationAuditErrorCode.MandatoryToolDependency, path + "/routes/access", "Mandatory witness introduced a tool dependency.");
            if (metrics.UnrecoverableFailureCount != 0)
                Add(errors, SpecialRegionValidationAuditErrorCode.UnrecoverableFailure, path + "/routes/recovery", "Failure does not rejoin Recovery or Return.");
            if (!metrics.StateVariantMatches)
                Add(errors, SpecialRegionValidationAuditErrorCode.StateVariantMismatch, path + "/states", "State variant identity or shell preservation failed.");
            if (!metrics.ResetMatches)
                Add(errors, SpecialRegionValidationAuditErrorCode.ResetMismatch, path + "/resets", "Reset identity or recovery preservation failed.");
            if (metrics.ResourceLossRiskCount != 0)
                Add(errors, SpecialRegionValidationAuditErrorCode.ResourceLossRisk, path + "/resources", "Permanent required-resource loss risk is non-zero.");
            if (metrics.DuplicateBenefitRiskCount != 0)
                Add(errors, SpecialRegionValidationAuditErrorCode.DuplicateBenefitRisk, path + "/benefits", "Duplicate optional benefit risk is non-zero.");
            if (metrics.MutationClaimCount != 0)
                Add(errors, SpecialRegionValidationAuditErrorCode.MutationClaim, path + "/mutations", "Read-only audit source published mutation/solver/gameplay work.");
            if (!metrics.CanonicalPublication)
                Add(errors, SpecialRegionValidationAuditErrorCode.NonCanonicalPublication, path + "/publication", "Artifact publication is not canonical.");

            if (input.Family == SpecialRegionAuditFamily.Village && input.StateCount != 5)
                Add(errors, SpecialRegionValidationAuditErrorCode.StateVariantMismatch, path + "/states", "Village requires exact five variants.");
            if (input.Family == SpecialRegionAuditFamily.CoreResource &&
                (input.RequiredRewardCount != 1 || input.PersistenceCheckpointCount != 7 || input.ResetCount == 0))
                Add(errors, SpecialRegionValidationAuditErrorCode.PersistenceMismatch, path + "/resourceProof", "Core resource needs one Reward, seven checkpoints, and recovery reset proof.");
            if (string.Equals(input.ArtifactId, "SR_MOON_SEAL_FORGE_9", StringComparison.Ordinal) &&
                (input.RequiredRewardCount != 1 || input.PersistenceCheckpointCount != 7))
                Add(errors, SpecialRegionValidationAuditErrorCode.PersistenceMismatch, path + "/moonSeal", "Forge MoonSeal requires one authoritative Reward and seven-checkpoint proof.");
        }

        private static void ValidateAggregateSlotKinds(
            IEnumerable<SpecialRegionAuditArtifactInput> inputs,
            ICollection<SpecialRegionValidationAuditError> errors)
        {
            var kinds = new HashSet<SpecialRegionSlotKind>(inputs.SelectMany(value => value.SlotKinds));
            foreach (var required in new[]
                     {
                         SpecialRegionSlotKind.Facility, SpecialRegionSlotKind.Npc,
                         SpecialRegionSlotKind.Enemy, SpecialRegionSlotKind.Event,
                         SpecialRegionSlotKind.Reward,
                     })
                if (!kinds.Contains(required))
                    Add(errors, SpecialRegionValidationAuditErrorCode.MissingArtifact,
                        "slotKinds/" + required, "The canonical ten-item audit must expose all five replaceable slot meanings.");
        }

        private static SpecialRegionAuditArtifactResult BuildArtifactResult(SpecialRegionAuditArtifactInput input)
        {
            var sections = new[]
            {
                Section(input, SpecialRegionAuditSection.Identity, 1, input.KindOrTheme),
                Section(input, SpecialRegionAuditSection.FootprintBindingBuffer,
                    input.Metrics.SectorCoverageCount, input.FootprintWidth + "x" + input.FootprintHeight),
                Section(input, SpecialRegionAuditSection.FixedCollision, input.FixedCollisionCount, "immutable collision"),
                Section(input, SpecialRegionAuditSection.FixedAccess, input.FixedAccessCount, "immutable access"),
                Section(input, SpecialRegionAuditSection.ReplaceableSlots, input.SlotKinds.Count, "marker-only slot kinds"),
                Section(input, SpecialRegionAuditSection.Routes, input.Routes.Count, "ordered no-tool static witnesses"),
                Section(input, SpecialRegionAuditSection.States, input.StateCount, "shell-preserving state snapshots"),
                Section(input, SpecialRegionAuditSection.ResetPersistence,
                    input.ResetCount + input.PersistenceCheckpointCount, "recoverable reset and persistence proof"),
            };
            var digest = SpecialRegionValidationCanonicalDigest.ComputeArtifact(input, sections);
            return new SpecialRegionAuditArtifactResult(input, sections, digest);
        }

        private static SpecialRegionAuditSectionResult Section(
            SpecialRegionAuditArtifactInput input,
            SpecialRegionAuditSection section,
            int count,
            string detail)
        {
            var digest = SpecialRegionValidationCanonicalDigest.ComputeSection(input.ArtifactId, section, count, detail);
            return new SpecialRegionAuditSectionResult(section, true, count, detail, digest);
        }

        private static bool Digest(string value)
        {
            if (value == null || value.Length != 64) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var item = value[index];
                if (!((item >= '0' && item <= '9') || (item >= 'a' && item <= 'f'))) return false;
            }
            return true;
        }

        private static SpecialRegionValidationAuditResult Failure(
            SpecialRegionValidationAuditErrorCode code,
            string path,
            string detail)
            => new SpecialRegionValidationAuditResult(null,
                new[] { new SpecialRegionValidationAuditError(code, path, detail) }, string.Empty);

        private static void Add(
            ICollection<SpecialRegionValidationAuditError> errors,
            SpecialRegionValidationAuditErrorCode code,
            string path,
            string detail)
            => errors.Add(new SpecialRegionValidationAuditError(code, path, detail));
    }

    public static class SpecialRegionValidationCanonicalDigest
    {
        public static string ComputeSection(
            string artifactId,
            SpecialRegionAuditSection section,
            int count,
            string detail)
            => Sha256((artifactId ?? string.Empty) + "\n" + section + "\n" +
                      count.ToString(CultureInfo.InvariantCulture) + "\n" + (detail ?? string.Empty));

        public static string ComputeArtifact(
            SpecialRegionAuditArtifactInput input,
            IEnumerable<SpecialRegionAuditSectionResult> sections)
        {
            var value = new StringBuilder();
            Append(value, "order", input.CanonicalOrder.ToString(CultureInfo.InvariantCulture));
            Append(value, "id", input.ArtifactId);
            Append(value, "family", input.Family.ToString());
            Append(value, "binding", input.Binding.ToString());
            Append(value, "kind", input.RegionKind.ToString());
            Append(value, "theme", input.KindOrTheme);
            Append(value, "footprint", input.FootprintWidth + "x" + input.FootprintHeight);
            Append(value, "design", input.DesignWidth + "x" + input.DesignHeight);
            Append(value, "chunks", input.ActiveChunkCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "fixedCollision", input.FixedCollisionCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "fixedAccess", input.FixedAccessCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "states", input.StateCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "resets", input.ResetCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "checkpoints", input.PersistenceCheckpointCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "keys", input.PersistenceKeyCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "source", input.SourceDigest);
            Append(value, "component", input.ComponentDigest);
            Append(value, "artifact", input.ArtifactDigest);
            foreach (var kind in input.SlotKinds) Append(value, "slot", kind.ToString());
            foreach (var route in input.Routes)
                Append(value, "route", route.RouteId + ":" + route.RouteKind + ":" +
                                       string.Join(",", route.NodeIds) + ":" + Bool(route.MandatoryNoTool) +
                                       Bool(route.Ordered) + Bool(route.Recovery));
            foreach (var token in input.Tokens)
                Append(value, "token", token.Kind + ":" + token.Id + ":" + token.X + "," + token.Y + ":" + token.Label);
            foreach (var section in sections.OrderBy(item => item.Section))
                Append(value, "section", section.Section + ":" + section.CanonicalDigest);
            return Sha256(value.ToString());
        }

        public static string ComputeReport(SpecialRegionValidationReport report)
        {
            if (report == null) return string.Empty;
            var value = new StringBuilder();
            foreach (var artifact in report.Artifacts.OrderBy(item => item.Input.CanonicalOrder))
                Append(value, "artifact", artifact.Input.CanonicalOrder + ":" + artifact.ArtifactId + ":" + artifact.CanonicalDigest);
            Append(value, "sections", report.SectionPassCount + ":" + report.SectionFailCount);
            Append(value, "routes", report.RouteCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "states", report.StateCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "resets", report.ResetCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "checkpoints", report.PersistenceCheckpointCount.ToString(CultureInfo.InvariantCulture));
            Append(value, "mutations", report.MutationClaimCount.ToString(CultureInfo.InvariantCulture));
            return Sha256(value.ToString());
        }

        private static string Bool(bool value) => value ? "1" : "0";
        private static void Append(StringBuilder target, string name, string value)
            => target.Append(name).Append('=').Append(value ?? string.Empty).Append('\n');

        private static string Sha256(string material)
        {
            using (var algorithm = SHA256.Create())
            {
                var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty));
                var value = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) value.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return value.ToString();
            }
        }
    }
}
