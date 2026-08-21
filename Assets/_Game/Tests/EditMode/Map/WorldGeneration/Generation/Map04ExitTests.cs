using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class Map04ExitTests
    {
        private const ulong ViableWorldSeed = 0x0123456789ABCDF9UL;
        private const int ViableAttempt = 24;
        private Fixture fixture;

        public static IEnumerable<TestCaseData> DeterminismSeeds
        {
            get
            {
                for (var seed = 0; seed <= 101; seed++)
                    yield return new TestCaseData((ulong)seed).SetName(
                        "Determinism_FreshReusedAndReversedDefinitions_" +
                        seed.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            fixture = BuildFixture();
        }

        internal CleanupContractAudit AuditCleanupContract(
            bool reverseDefinitions,
            bool freshServices)
        {
            if (fixture == null) fixture = BuildFixture();
            var reusedServices = new PipelineServices();
            var legacyRows = new List<string>();
            var completed = 0;
            var handoff = 0;
            var invalid = 0;
            var cleanupRejectedWitnesses = 0;
            var sourceMutationCount = 0;

            for (ulong seed = 0; seed < 1000UL; seed++)
            {
                var services = freshServices ? new PipelineServices() : reusedServices;
                var source = BuildSourceSnapshot(seed);
                var world = CreateSourceWorld(seed);
                var resolved = false;
                for (var attempt = 0; attempt < 100; attempt++)
                {
                    var record = RunAttempt(
                        seed, attempt, source, world, services, fixture,
                        reverseDefinitions, true);
                    if (!record.SourceUnchanged) sourceMutationCount++;
                    if (!string.IsNullOrEmpty(record.LegacyCleanupFailureDetail))
                    {
                        if (!record.Completed || record.Cleanup == null ||
                            !record.StageStatuses.Contains("PatchCleanup=Completed"))
                            cleanupRejectedWitnesses++;
                        legacyRows.Add(
                            seed.ToString(CultureInfo.InvariantCulture) + "/" +
                            record.AttemptOrdinal.ToString(CultureInfo.InvariantCulture) + "|" +
                            record.LegacyCleanupFailureDetail);
                        completed++;
                        resolved = true;
                        break;
                    }
                    if (record.Completed)
                    {
                        completed++;
                        resolved = true;
                        break;
                    }
                    if (!record.RetryRequired || !record.SourceUnchanged)
                    {
                        invalid++;
                        resolved = true;
                        break;
                    }
                }
                if (!resolved) handoff++;
            }

            return new CleanupContractAudit(
                legacyRows, completed, handoff, invalid,
                cleanupRejectedWitnesses, sourceMutationCount);
        }

        [Test]
        public void ResultChainAndInventoryContract_IsCanonical()
        {
            var hashes = new[]
            {
                "b7362725e0a4bdf952372b67ece63e1b0f3e26c4306845d09b5250753eedeb6d",
                "d10a2350723ebe2d47b26a89f59d0c605eb242fa9d1fe432e811bb39ee608ee8",
                "6f4ace7b730f4df4662fcc4409d90d031555965bec47e1c62180a8632119280e",
                "2706853d660845b059737c15488221f5bd5d68a5d02e0a3d6c65e9375464e334",
                "ab23a2d0e30cb21df7fca6f098607cf20ccd5a3cc9a9da4f43f8fdb344ba6e2f",
                "17be290682faf4a69716424bed7eb38fa32049a63f5406c17d0c89af128644ed",
                "7fbef41a6b6f054e2a8c6270a9cec6d3825143d0291c7c6bf5952e57f46a51dd",
                "a65c8dd370d6b5bc315b1c0d901c7838045f7fc08f8acf596d585388fed0c206",
                "13cf132ed6fc3f10e2159352da64b1e9a8cde52fbae4c0918c78385e7a12dcb1",
                "76a982a0258f4348bdc52e1e73e6ffe56a3a05ad42d38a9db11186f32df84dca"
            };

            Assert.That(hashes, Has.Length.EqualTo(10));
            Assert.That(hashes.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(10));
            Assert.That(hashes.All(value => value.Length == 64 &&
                value.All(character => character >= '0' && character <= '9' ||
                                       character >= 'a' && character <= 'f')), Is.True);
            Assert.That(WorldGenConstants.SectorCount, Is.EqualTo(169));
        }

        [Test]
        public void Fixture_HasSevenReservationsFourCoreSourcesAndExactDefinitions()
        {
            var source = BuildSourceSnapshot(0UL);
            {
                Assert.That(source.Reservations.Count, Is.EqualTo(7));
                Assert.That(source.Sectors.Count, Is.EqualTo(169));
                Assert.That(source.Sectors.Count(value => value.IsReserved), Is.EqualTo(8));
                Assert.That(source.CoreBiomeSeeds.Count, Is.EqualTo(4));
                Assert.That(fixture.Definitions.Biomes.Count, Is.EqualTo(4));
                Assert.That(fixture.Definitions.AllRules.Count, Is.EqualTo(10));
                Assert.That(fixture.Definitions.Profiles.Count, Is.EqualTo(6));
                Assert.That(fixture.Definitions.Pairs.Count, Is.EqualTo(6));
            }
        }

        [Test]
        public void FullBatch_OneThousandWorldSeedsResolveAndPassAllExitGates()
        {
            var services = new PipelineServices();
            var histogram = new int[100];
            var terminalCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var aggregateDigest = new StringBuilder();
            var invalidLedger = new List<string>();
            var completed = 0;
            var handoff = 0;
            var invalid = 0;
            var initialSuccess = 0;
            var retryWorlds = 0;
            var totalAttempts = 0;
            var totalRetries = 0;
            var maximumOrdinal = 0;
            var minimumPatchCount = int.MaxValue;
            var maximumPatchCount = int.MinValue;
            long coreTotal = 0;
            long satelliteTotal = 0;
            long intrusionTotal = 0;

            for (ulong seed = 0; seed < 1000UL; seed++)
            {
                var result = RunWorld(seed, services, false);
                totalAttempts += result.Attempts.Count;
                totalRetries += result.Retries.Count;
                foreach (var retry in result.Retries)
                {
                    var key = retry.TerminalStage + ":" + retry.Reason;
                    terminalCounts[key] = terminalCounts.TryGetValue(key, out var count) ? count + 1 : 1;
                }
                maximumOrdinal = Math.Max(maximumOrdinal, result.Final.AttemptOrdinal);
                aggregateDigest.Append(seed.ToString(CultureInfo.InvariantCulture)).Append(':')
                    .Append(result.Disposition).Append(':').Append(result.CanonicalDigest).Append('\n');

                if (result.Disposition == WorldDisposition.PassSiteHandoffRequired)
                {
                    handoff++;
                    ValidatePassSiteHandoff(result);
                    continue;
                }
                if (result.Disposition == WorldDisposition.Invalid)
                {
                    invalid++;
                    invalidLedger.Add(
                        seed.ToString(CultureInfo.InvariantCulture) + "/" +
                        result.Final.AttemptOrdinal.ToString(CultureInfo.InvariantCulture) + "|" +
                        result.Final.FailureDetail);
                    continue;
                }

                completed++;
                ValidateSuccessfulAttempt(result.Final);

                histogram[result.Final.AttemptOrdinal]++;
                if (result.Final.AttemptOrdinal == 0) initialSuccess++;
                else retryWorlds++;
                minimumPatchCount = Math.Min(minimumPatchCount, result.Final.PatchCount);
                maximumPatchCount = Math.Max(maximumPatchCount, result.Final.PatchCount);
                coreTotal += result.Final.CoreCount;
                satelliteTotal += result.Final.SatelliteCount;
                intrusionTotal += result.Final.IntrusionCount;
            }

            var digest = Sha256(Encoding.UTF8.GetBytes(aggregateDigest.ToString()));
            var invalidLedgerText = string.Join("\n", invalidLedger);
            var invalidLedgerDigest = Sha256(Encoding.UTF8.GetBytes(invalidLedgerText));
            Debug.Log(
                "MAP04_11_INVALID_LEDGER count=" + invalidLedger.Count +
                " digest=" + invalidLedgerDigest + "\n" + invalidLedgerText);
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "MAP04_11_BATCH worlds=1000 completed={0} handoff={1} invalid={2} attempts={3} " +
                "initial={4} retryWorlds={5} retries={6} maxOrdinal={7} patchRange={8}-{9} " +
                "roles={10}/{11}/{12} histogram={13} terminals={14} digest={15}",
                completed, handoff, invalid, totalAttempts, initialSuccess, retryWorlds, totalRetries, maximumOrdinal,
                minimumPatchCount, maximumPatchCount, coreTotal, satelliteTotal, intrusionTotal,
                string.Join(",", histogram.Select((count, ordinal) => ordinal + ":" + count)),
                string.Join(",", terminalCounts.Select(value => value.Key + "=" + value.Value)), digest));
            var failureSummary = string.Format(CultureInfo.InvariantCulture,
                "completed={0} handoff={1} invalid={2} attempts={3} retries={4} maxOrdinal={5} " +
                "terminals={6} digest={7}",
                completed, handoff, invalid, totalAttempts, totalRetries, maximumOrdinal,
                string.Join(",", terminalCounts.Select(value => value.Key + "=" + value.Value)), digest);

            {
                Assert.That(completed + handoff, Is.EqualTo(1000), failureSummary);
                Assert.That(invalid, Is.Zero);
                Assert.That(completed, Is.GreaterThan(0));
                Assert.That(handoff, Is.GreaterThan(0));
                Assert.That(histogram.Sum(), Is.EqualTo(completed));
                Assert.That(initialSuccess + retryWorlds, Is.EqualTo(completed));
                Assert.That(totalAttempts, Is.GreaterThanOrEqualTo(1000));
                Assert.That(totalRetries, Is.GreaterThanOrEqualTo(retryWorlds));
                Assert.That(maximumOrdinal, Is.InRange(0, 99));
                Assert.That(minimumPatchCount, Is.GreaterThanOrEqualTo(4));
                Assert.That(maximumPatchCount, Is.LessThanOrEqualTo(WorldGenConstants.SectorCount));
                Assert.That(coreTotal, Is.EqualTo(completed * 4L));
                Assert.That(intrusionTotal, Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void KnownViableAttempt_MatchesFrozenCsvRngAndValidationVectors()
        {
            var source = BuildSourceSnapshot(ViableWorldSeed);
            var world = CreateSourceWorld(ViableWorldSeed);
            var record = RunAttempt(
                ViableWorldSeed, ViableAttempt, source, world,
                new PipelineServices(), fixture, false);

            Assert.That(record.Completed, Is.True, record.TerminalStage + ":" + record.Reason);
            ValidateSuccessfulAttempt(record);
            {
                Assert.That(record.AttemptOrdinal, Is.EqualTo(24));
                Assert.That(record.PatchCount, Is.EqualTo(17));
                Assert.That(record.CoreCount, Is.EqualTo(4));
                Assert.That(record.SatelliteCount, Is.EqualTo(10));
                Assert.That(record.IntrusionCount, Is.EqualTo(3));
                Assert.That(record.AssignedCount, Is.EqualTo(165));
                Assert.That(record.UnassignedCount, Is.EqualTo(4));
                Assert.That(record.RngDrawCount, Is.EqualTo(1912UL));
                Assert.That(record.PatchByteCount, Is.EqualTo(1956));
                Assert.That(record.WorldByteCount, Is.EqualTo(16380));
                Assert.That(record.PatchSha, Is.EqualTo("7ccf1fc1e6ebd298cc97bed3914395170fc38fe85b2d2392c80c9f30ec000543"));
                Assert.That(record.WorldSha, Is.EqualTo("07daa96fe5f6ea985aa9e32aa0609d65b95c620a0b05a99426d3093275f8ee1d"));
                Assert.That(record.RuleCount, Is.EqualTo(15));
            }
        }

        [Test]
        public void RetryClassification_ShortCircuitsAndFreshAttemptResetsRng()
        {
            var source = BuildSourceSnapshot(ViableWorldSeed);
            var world = CreateSourceWorld(ViableWorldSeed);
            var services = new PipelineServices();
            var failed = RunAttempt(ViableWorldSeed, 0, source, world, services, fixture, false);
            var completed = RunAttempt(ViableWorldSeed, ViableAttempt, source, world, services, fixture, false);
            var invalid = new CorePatchSeedInitializer().Initialize(null, null, null);

            {
                Assert.That(failed.Completed, Is.False);
                Assert.That(failed.RetryRequired, Is.True);
                Assert.That(failed.SourceUnchanged, Is.True);
                Assert.That(failed.TerminalStage, Is.EqualTo("MultiSeedBiomeGrower"));
                Assert.That(failed.StageStatuses, Does.Not.Contain("IntrusionPlacer"));
                Assert.That(failed.Cleanup, Is.Null);
                Assert.That(failed.Export, Is.Null);
                Assert.That(failed.Validation, Is.Null);
                Assert.That(failed.Overlay, Is.Null);
                Assert.That(completed.Completed, Is.True);
                Assert.That(completed.SourceUnchanged, Is.True);
                Assert.That(completed.RngDrawCount, Is.EqualTo(1912UL));
                Assert.That(invalid.Succeeded, Is.False);
                Assert.That(invalid.RetryRequired, Is.False);
                Assert.That(invalid.Status, Is.EqualTo(CorePatchInitializationStatus.InvalidInput));
            }
        }

        [Test]
        public void SameSeedAndAttempt_RepeatedOneHundredTimesHasExactDigest()
        {
            var source = BuildSourceSnapshot(ViableWorldSeed);
            var world = CreateSourceWorld(ViableWorldSeed);
            var services = new PipelineServices();
            string expected = null;
            for (var repeat = 0; repeat < 100; repeat++)
            {
                var record = RunAttempt(
                    ViableWorldSeed, ViableAttempt, source, world,
                    services, fixture, false);
                Assert.That(record.Completed, Is.True);
                if (expected == null) expected = record.CanonicalDigest;
                Assert.That(record.CanonicalDigest, Is.EqualTo(expected), "repeat=" + repeat);
            }
        }

        [Test]
        public void CultureAndShuffledDefinitions_PreserveRepresentativeDigests()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var baseline = RunWorld(0UL, new PipelineServices(), false);
                ValidateWorldResult(baseline);
                foreach (var cultureName in new[] { "en-US", "tr-TR" })
                {
                    var culture = CultureInfo.GetCultureInfo(cultureName);
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                    var shuffled = RunWorld(0UL, new PipelineServices(), true);
                    ValidateWorldResult(shuffled);
                    Assert.That(shuffled.Disposition, Is.EqualTo(baseline.Disposition));
                    Assert.That(shuffled.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                    Assert.That(shuffled.Attempts.Count, Is.EqualTo(baseline.Attempts.Count));
                    Assert.That(shuffled.Final.AttemptOrdinal, Is.EqualTo(baseline.Final.AttemptOrdinal));
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void OverlayAndOwnershipExitGate_UsesApprovedPublicationWithoutMutation()
        {
            var source = BuildSourceSnapshot(ViableWorldSeed);
            var sourceSignature = ReservationSignature(source);
            var record = RunAttempt(
                ViableWorldSeed, ViableAttempt, source, CreateSourceWorld(ViableWorldSeed),
                new PipelineServices(), fixture, false);

            Assert.That(record.Completed, Is.True);
            {
                Assert.That(record.Overlay.Cells, Has.Count.EqualTo(169));
                Assert.That(record.Overlay.Patches, Has.Count.EqualTo(17));
                Assert.That(record.Overlay.CoreCount, Is.EqualTo(4));
                Assert.That(record.Overlay.SatelliteCount, Is.EqualTo(10));
                Assert.That(record.Overlay.IntrusionCount, Is.EqualTo(3));
                Assert.That(record.Overlay.AssignedCount, Is.EqualTo(165));
                Assert.That(record.Overlay.UnassignedCount, Is.EqualTo(4));
                Assert.That(record.Overlay.PassedValidationRuleCount, Is.EqualTo(15));
                Assert.That(record.Overlay.Patches.Select(value => value.PatchId), Is.Ordered);
                Assert.That(record.Overlay.Patches.Count(value => value.Role == BiomePatchRole.Intrusion && value.Size == 1), Is.EqualTo(3));
                Assert.That(ReservationSignature(source), Is.EqualTo(sourceSignature));
            }
        }

        [TestCaseSource(nameof(DeterminismSeeds))]
        public void Determinism_FreshReusedAndReversedDefinitions(ulong seed)
        {
            var services = new PipelineServices();
            var fresh = RunWorld(seed, services, false);
            var reused = RunWorld(seed, services, false);
            var reversed = RunWorld(seed, services, true);

            {
                ValidateWorldResult(fresh);
                ValidateWorldResult(reused);
                ValidateWorldResult(reversed);
                Assert.That(reused.Disposition, Is.EqualTo(fresh.Disposition));
                Assert.That(reversed.Disposition, Is.EqualTo(fresh.Disposition));
                Assert.That(reused.CanonicalDigest, Is.EqualTo(fresh.CanonicalDigest));
                Assert.That(reversed.CanonicalDigest, Is.EqualTo(fresh.CanonicalDigest));
                Assert.That(reused.Attempts.Count, Is.EqualTo(fresh.Attempts.Count));
                Assert.That(reversed.Attempts.Count, Is.EqualTo(fresh.Attempts.Count));
                Assert.That(reused.Final.AttemptOrdinal, Is.EqualTo(fresh.Final.AttemptOrdinal));
                Assert.That(reversed.Final.AttemptOrdinal, Is.EqualTo(fresh.Final.AttemptOrdinal));
            }
        }

        private WorldResult RunWorld(ulong worldSeed, PipelineServices services, bool reverseDefinitions)
        {
            var source = BuildSourceSnapshot(worldSeed);
            var world = CreateSourceWorld(worldSeed);
            var attempts = new List<AttemptRecord>();
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var record = RunAttempt(
                    worldSeed, attempt, source, world, services, fixture, reverseDefinitions);
                attempts.Add(record);
                if (record.Completed)
                    return new WorldResult(WorldDisposition.Completed, record, attempts);
                if (!record.RetryRequired || !record.SourceUnchanged)
                {
                    Debug.Log(
                        "MAP04_11_INVALID_DETAIL seed=" + worldSeed +
                        " attempt=" + attempt + " " + record.FailureDetail);
                    return new WorldResult(WorldDisposition.Invalid, record, attempts);
                }
            }
            return new WorldResult(
                WorldDisposition.PassSiteHandoffRequired,
                attempts[attempts.Count - 1], attempts);
        }

        private static AttemptRecord RunAttempt(
            ulong worldSeed,
            int attemptOrdinal,
            SiteReservationSnapshot source,
            GeneratedWorldData world,
            PipelineServices services,
            Fixture fixture,
            bool reverseDefinitions,
            bool stopAfterCleanupForLegacyAudit = false)
        {
            var definitions = fixture.Definitions;
            var biomes = Ordered(definitions.Biomes, reverseDefinitions);
            var coreRules = Ordered(definitions.CoreRules, reverseDefinitions);
            var satelliteRules = Ordered(definitions.SatelliteRules, reverseDefinitions);
            var coreAndSatelliteRules = Ordered(definitions.CoreAndSatelliteRules, reverseDefinitions);
            var allRules = Ordered(definitions.AllRules, reverseDefinitions);
            var profiles = Ordered(definitions.Profiles, reverseDefinitions);
            var pairs = Ordered(definitions.Pairs, reverseDefinitions);
            var stages = new List<string>();
            var sourceBefore = ReservationSignature(source);

            var initialization = services.Initializer.Initialize(source, biomes, coreRules);
            stages.Add("CorePatchSeedInitializer=" + initialization.Status);
            if (!initialization.Succeeded)
                return Failure(worldSeed, attemptOrdinal, "CorePatchSeedInitializer",
                    initialization.RetryRequired,
                    ErrorCodes(initialization.Errors.Select(value => value.Code.ToString())), 0UL, stages,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal));

            var coreGrowth = services.CoreGrower.Grow(initialization.Publication, biomes, coreRules);
            stages.Add("CorePatchGrower=" + coreGrowth.Status);
            if (!coreGrowth.Succeeded)
                return Failure(worldSeed, attemptOrdinal, "CorePatchGrower",
                    coreGrowth.RetryRequired,
                    ErrorCodes(coreGrowth.Errors.Select(value => value.Code.ToString())), 0UL, stages,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal));

            var rng = fixture.RngStreams.CreateBiomePatch(worldSeed, "PASS_BIOME", attemptOrdinal);
            var satellites = services.SatellitePlacer.Place(
                coreGrowth.Publication, fixture.Profile, biomes, satelliteRules, rng);
            stages.Add("SatelliteSeedPlacer=" + satellites.Status);
            if (!satellites.Succeeded)
                return Failure(worldSeed, attemptOrdinal, "SatelliteSeedPlacer",
                    satellites.RetryRequired,
                    ErrorCodes(satellites.Errors.Select(value => value.Code.ToString())), rng.DrawCount, stages,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal));

            var growth = services.BiomeGrower.Grow(
                satellites, fixture.Profile, biomes, coreAndSatelliteRules, rng);
            stages.Add("MultiSeedBiomeGrower=" + growth.Status);
            if (!growth.Succeeded)
                return Failure(worldSeed, attemptOrdinal, "MultiSeedBiomeGrower",
                    growth.RetryRequired,
                    ErrorCodes(growth.Errors.Select(value => value.Code.ToString())), rng.DrawCount, stages,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal));

            var intrusion = services.IntrusionPlacer.Place(
                growth, fixture.Profile, biomes, allRules, profiles, pairs, rng);
            stages.Add("IntrusionPlacer=" + intrusion.Status);
            if (!intrusion.Succeeded)
                return Failure(worldSeed, attemptOrdinal, "IntrusionPlacer",
                    intrusion.RetryRequired,
                    ErrorCodes(intrusion.Errors.Select(value => value.Code.ToString())), rng.DrawCount, stages,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal));

            var legacyCleanupFailureDetail = LegacyCleanupFailureDetail(
                intrusion, biomes, allRules);
            var cleanup = services.Cleanup.Clean(intrusion, biomes, allRules);
            stages.Add("PatchCleanup=" + cleanup.Status);
            if (!cleanup.Succeeded)
                return Failure(worldSeed, attemptOrdinal, "PatchCleanup",
                    cleanup.RetryRequired,
                    ErrorCodes(cleanup.Errors.Select(value => value.Code.ToString())), rng.DrawCount, stages,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal),
                    CleanupFailureDetail(intrusion, cleanup), legacyCleanupFailureDetail);
            if (stopAfterCleanupForLegacyAudit && !string.IsNullOrEmpty(legacyCleanupFailureDetail))
                return CleanupAuditAccepted(
                    worldSeed, attemptOrdinal, rng.DrawCount, stages,
                    cleanup, legacyCleanupFailureDetail,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal));

            var export = services.Exporter.Export(cleanup, world);
            stages.Add("BiomePatchExporter=" + export.Status);
            if (!export.Succeeded)
                return Failure(worldSeed, attemptOrdinal, "BiomePatchExporter",
                    false, ErrorCodes(export.Errors.Select(value => value.Code.ToString())), rng.DrawCount, stages,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal),
                    string.Empty, legacyCleanupFailureDetail);

            var validation = services.Validator.Validate(export, biomes, allRules, profiles, pairs);
            stages.Add("BiomePatchValidator=" + validation.Status);
            if (!validation.Succeeded)
            {
                var reason = validation.Errors.Count != 0
                    ? ErrorCodes(validation.Errors.Select(value => value.Code.ToString()))
                    : string.Join(",", validation.Violations.Select(value => value.Rule.ToString()));
                return Failure(worldSeed, attemptOrdinal, "BiomePatchValidator",
                    validation.RetryRequired, reason, rng.DrawCount, stages,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal),
                    string.Empty, legacyCleanupFailureDetail);
            }

            BiomePatchOverlaySnapshot overlay;
            try
            {
                overlay = BiomePatchOverlaySnapshot.Create(validation.Publication);
                stages.Add("BiomePatchOverlaySnapshot=Completed");
            }
            catch (ArgumentException)
            {
                stages.Add("BiomePatchOverlaySnapshot=InvalidProjection");
                var diagnostics = validation.Diagnostics;
                var detail = string.Format(CultureInfo.InvariantCulture,
                    "patches={0}|roles={1}/{2}/{3}|assigned={4}|unassigned={5}|rules={6}/{7}",
                    diagnostics.PatchCount, diagnostics.CorePatchCount,
                    diagnostics.SatellitePatchCount, diagnostics.IntrusionPatchCount,
                    diagnostics.AssignedSectorCount, diagnostics.UnassignedSectorCount,
                    diagnostics.RuleResults.Count(value => value.Passed), diagnostics.RuleResults.Count);
                return Failure(
                    worldSeed, attemptOrdinal, "BiomePatchOverlaySnapshot", false,
                    "ExactProjectionRejected", rng.DrawCount, stages,
                    string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal),
                    detail, legacyCleanupFailureDetail);
            }
            if (!string.Equals(sourceBefore, ReservationSignature(source), StringComparison.Ordinal))
                throw new InvalidOperationException("The attempt mutated its source reservation snapshot.");

            return Success(
                worldSeed, attemptOrdinal, rng.DrawCount, stages,
                cleanup, export, validation, overlay, legacyCleanupFailureDetail);
        }

        private static AttemptRecord Failure(
            ulong worldSeed, int attemptOrdinal, string stage, bool retry,
            string reason, ulong rngDrawCount, IEnumerable<string> stages,
            bool sourceUnchanged, string failureDetail = "",
            string legacyCleanupFailureDetail = "")
        {
            var status = string.Join("|", stages);
            var digest = Sha256(Encoding.UTF8.GetBytes(string.Format(
                CultureInfo.InvariantCulture, "{0}/{1}/{2}/{3}/{4}/{5}/{6}/{7}",
                worldSeed, attemptOrdinal, status, retry, reason, rngDrawCount,
                sourceUnchanged, failureDetail)));
            return new AttemptRecord(
                worldSeed, attemptOrdinal, false, retry, stage, reason,
                rngDrawCount, status, 0, 0, 0, 0, 0, 0, 0,
                string.Empty, string.Empty, 0, string.Empty, digest,
                sourceUnchanged, failureDetail, legacyCleanupFailureDetail,
                null, null, null, null);
        }

        private static AttemptRecord Success(
            ulong worldSeed, int attemptOrdinal, ulong rngDrawCount,
            IEnumerable<string> stages, PatchCleanupResult cleanup,
            BiomePatchExportResult export, BiomePatchValidationResult validation,
            BiomePatchOverlaySnapshot overlay,
            string legacyCleanupFailureDetail)
        {
            var snapshot = cleanup.Publication.Snapshot;
            var patchBytes = export.Publication.GeneratedBiomePatchesCsv;
            var worldBytes = export.Publication.GeneratedWorldSectorsCsv;
            var patchSha = Sha256(patchBytes);
            var worldSha = Sha256(worldBytes);
            var snapshotDigest = SnapshotDigest(snapshot);
            var overlayDigest = OverlayDigest(overlay);
            var status = string.Join("|", stages);
            var builder = new StringBuilder();
            builder.Append(worldSeed.ToString(CultureInfo.InvariantCulture)).Append('/')
                .Append(attemptOrdinal.ToString(CultureInfo.InvariantCulture)).Append('/')
                .Append(status).Append('/').Append(rngDrawCount.ToString(CultureInfo.InvariantCulture)).Append('/')
                .Append(snapshotDigest).Append('/').Append(patchBytes.Length).Append('/').Append(worldBytes.Length)
                .Append('/').Append(patchSha).Append('/').Append(worldSha).Append('/')
                .Append(validation.Diagnostics.RuleResults.Count).Append('/').Append(overlayDigest);
            var canonicalDigest = Sha256(Encoding.UTF8.GetBytes(builder.ToString()));

            return new AttemptRecord(
                worldSeed, attemptOrdinal, true, false, "Completed", string.Empty,
                rngDrawCount, status, snapshot.Patches.Count,
                snapshot.Patches.Count(value => value.Role == BiomePatchRole.Core),
                snapshot.Patches.Count(value => value.Role == BiomePatchRole.Satellite),
                snapshot.Patches.Count(value => value.Role == BiomePatchRole.Intrusion),
                snapshot.AssignedSectorCount, snapshot.UnassignedSectorCount,
                patchBytes.Length, patchSha, worldSha, worldBytes.Length,
                snapshotDigest, canonicalDigest, true, string.Empty,
                legacyCleanupFailureDetail, cleanup, export, validation, overlay);
        }

        private static AttemptRecord CleanupAuditAccepted(
            ulong worldSeed,
            int attemptOrdinal,
            ulong rngDrawCount,
            IEnumerable<string> stages,
            PatchCleanupResult cleanup,
            string legacyCleanupFailureDetail,
            bool sourceUnchanged)
        {
            var snapshot = cleanup.Publication.Snapshot;
            var status = string.Join("|", stages);
            var snapshotDigest = SnapshotDigest(snapshot);
            var canonicalDigest = Sha256(Encoding.UTF8.GetBytes(string.Format(
                CultureInfo.InvariantCulture,
                "cleanup-audit/{0}/{1}/{2}/{3}/{4}",
                worldSeed, attemptOrdinal, status, rngDrawCount, snapshotDigest)));
            return new AttemptRecord(
                worldSeed, attemptOrdinal, true, false, "PatchCleanupAuditAccepted", string.Empty,
                rngDrawCount, status, snapshot.Patches.Count,
                snapshot.Patches.Count(value => value.Role == BiomePatchRole.Core),
                snapshot.Patches.Count(value => value.Role == BiomePatchRole.Satellite),
                snapshot.Patches.Count(value => value.Role == BiomePatchRole.Intrusion),
                snapshot.AssignedSectorCount, snapshot.UnassignedSectorCount, 0,
                string.Empty, string.Empty, 0, snapshotDigest, canonicalDigest,
                sourceUnchanged, string.Empty, legacyCleanupFailureDetail,
                cleanup, null, null, null);
        }

        private static string CleanupFailureDetail(
            IntrusionPlacementResult intrusion,
            PatchCleanupResult cleanup)
        {
            var errors = string.Join(",", cleanup.Errors.Select(value =>
                value.Code + ":" + value.DefinitionId + ":" +
                value.SectorIndex.ToString(CultureInfo.InvariantCulture) + ":" +
                value.RequiredCount.ToString(CultureInfo.InvariantCulture) + ":" +
                value.AvailableCount.ToString(CultureInfo.InvariantCulture) + ":" +
                value.Message));
            return CleanupFailureDetail(
                intrusion, errors, cleanup.Publication == null ? 0 : 1, "NOT_EVALUATED");
        }

        private static string LegacyCleanupFailureDetail(
            IntrusionPlacementResult intrusion,
            IReadOnlyList<BiomeTypeDefinition> biomes,
            IReadOnlyList<BiomePatchRuleDefinition> rules)
        {
            var publication = intrusion.Publication;
            if (publication == null || publication.Snapshot == null) return string.Empty;
            var source = publication.Snapshot;
            var errors = new List<string>();
            var core = source.Patches.Count(value => value.Role == BiomePatchRole.Core);
            var intrusionCount = source.Patches.Count(value => value.Role == BiomePatchRole.Intrusion);
            if (core != 4 || intrusionCount != 3)
                errors.Add(
                    "InvalidSourceSnapshot::-1:7:" +
                    (core + intrusionCount).ToString(CultureInfo.InvariantCulture) +
                    ":P03 must contain exact four Core and three Intrusion patches.");
            if (source.Patches.Count != 17 || source.AssignedSectorCount != 165 ||
                source.UnassignedSectorCount != 4 || source.IsComplete ||
                publication.TotalPatchCount != 17 || publication.AssignedSectorCount != 165 ||
                publication.UnassignedSectorCount != 4 || publication.CorePatchCount != 4 ||
                publication.IntrusionPatchCount != 3)
                errors.Add(
                    "InvalidSourceSnapshot::-1:165:" +
                    source.AssignedSectorCount.ToString(CultureInfo.InvariantCulture) +
                    ":P03 must preserve exact 17 patches, 165 assigned, and 4 reserved-unassigned sectors.");
            if (errors.Count == 0) return string.Empty;
            return CleanupFailureDetail(
                intrusion, string.Join(",", errors), 0,
                CleanupConformanceViolation(intrusion, biomes, rules));
        }

        private static string CleanupFailureDetail(
            IntrusionPlacementResult intrusion,
            string errors,
            int cleanupPublication,
            string matrixViolation)
        {
            var publication = intrusion.Publication;
            var sourceDigest = publication == null || publication.SourceGrowth == null ||
                publication.SourceGrowth.Snapshot == null
                ? "_"
                : SnapshotDigest(publication.SourceGrowth.Snapshot);
            var outputDigest = publication == null || publication.Snapshot == null
                ? "_"
                : SnapshotDigest(publication.Snapshot);
            var roles = publication == null
                ? "_"
                : publication.CorePatchCount.ToString(CultureInfo.InvariantCulture) + "/" +
                  publication.SatellitePatchCount.ToString(CultureInfo.InvariantCulture) + "/" +
                  publication.IntrusionPatchCount.ToString(CultureInfo.InvariantCulture);
            return "intrusion=" + intrusion.Status +
                "|source=" + sourceDigest +
                "|output=" + outputDigest +
                "|errors=" + errors +
                "|patches=" + (publication == null ? -1 : publication.TotalPatchCount)
                    .ToString(CultureInfo.InvariantCulture) +
                "|roles=" + roles +
                "|matrix=" + matrixViolation +
                "|rng=" + (intrusion.Diagnostics == null ? 0UL : intrusion.Diagnostics.RngDrawCountAfter)
                    .ToString(CultureInfo.InvariantCulture) +
                "|intrusionPublication=" + (publication == null ? 0 : 1)
                    .ToString(CultureInfo.InvariantCulture) +
                "|cleanupPublication=" + cleanupPublication.ToString(CultureInfo.InvariantCulture);
        }

        private static string CleanupConformanceViolation(
            IntrusionPlacementResult intrusion,
            IReadOnlyList<BiomeTypeDefinition> biomes,
            IReadOnlyList<BiomePatchRuleDefinition> rules)
        {
            if (intrusion == null || intrusion.Status != IntrusionPlacementStatus.Completed ||
                intrusion.Publication == null || intrusion.Diagnostics == null)
                return "IntrusionCompletion";
            var publication = intrusion.Publication;
            var source = publication.SourceGrowth == null ? null : publication.SourceGrowth.Snapshot;
            var output = publication.Snapshot;
            if (source == null || output == null || publication.SourceSiteSnapshot == null ||
                source.Seed != output.Seed || output.Seed != publication.SourceSiteSnapshot.Seed)
                return "WorldIdentity";
            if (output.Sectors == null || output.Sectors.Count != WorldGenConstants.SectorCount ||
                output.AssignedSectorCount != 165 || output.UnassignedSectorCount != 4)
                return "SectorInventory";
            if (!output.Sectors.Select(value => value.SectorIndex)
                    .SequenceEqual(Enumerable.Range(0, WorldGenConstants.SectorCount)) ||
                output.Patches.Select(value => value.Id).Distinct().Count() != output.Patches.Count)
                return "RowMajorIdentity";
            var boundSiteCells = new HashSet<int>(
                output.SiteBindings.SelectMany(value => value.OccupiedSectorIndices));
            var membership = new HashSet<int>();
            foreach (var patch in output.Patches)
                foreach (var sectorIndex in patch.SectorIndices)
                {
                    var ownership = output.GetSector(sectorIndex);
                    if (!membership.Add(sectorIndex) || !ownership.IsAssigned ||
                        !ownership.PatchId.HasValue || ownership.PatchId.Value != patch.Id ||
                        !string.Equals(ownership.PrimaryBiomeId, patch.BiomeId, StringComparison.Ordinal))
                        return "PatchOwnership";
                }
            for (var index = 0; index < output.Sectors.Count; index++)
            {
                var ownership = output.GetSector(index);
                if (ownership.IsAssigned)
                {
                    if (!ownership.PatchId.HasValue ||
                        !output.TryGetPatch(ownership.PatchId.Value, out var owner) ||
                        !owner.SectorIndices.Contains(index) ||
                        (publication.SourceSiteSnapshot.GetSector(index).IsReserved &&
                         !boundSiteCells.Contains(index)))
                        return "OwnershipMembership";
                }
                else if (!publication.SourceSiteSnapshot.GetSector(index).IsReserved)
                    return "UnreservedUnassigned";
            }
            if (membership.Count != output.AssignedSectorCount)
                return "OwnershipCoverage";
            if (publication.TotalPatchCount != output.Patches.Count ||
                publication.CorePatchCount != 4 ||
                publication.IntrusionPatchCount != publication.Intrusions.Count ||
                publication.CorePatchCount + publication.SatellitePatchCount +
                    publication.IntrusionPatchCount != publication.TotalPatchCount)
                return "PublicationInventory";
            if (intrusion.Diagnostics.FinalPatchCount != output.Patches.Count ||
                intrusion.Diagnostics.FinalAssignedSectorCount != output.AssignedSectorCount ||
                intrusion.Diagnostics.FinalUnassignedSectorCount != output.UnassignedSectorCount ||
                intrusion.Diagnostics.DonorMinimumViolationCount != 0 ||
                intrusion.Diagnostics.DonorDisconnectCount != 0 ||
                intrusion.Diagnostics.ProtectedCellTransferCount != 0 ||
                intrusion.Diagnostics.DisallowedPairCount != 0 ||
                intrusion.Diagnostics.ReservationIntrusionCount != 0 ||
                intrusion.Diagnostics.PatchOverlapCount != 0)
                return "ProducerDiagnostics";

            var ruleLookup = rules.ToDictionary(value => value.PatchRuleId, StringComparer.Ordinal);
            var normalCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var patch in output.Patches)
            {
                if (!ruleLookup.TryGetValue(patch.PatchRuleId, out var rule) ||
                    !BiomePatchRoleTokenCodec.TryParse(rule.PatchRole, out var role) ||
                    role != patch.Role || !string.Equals(rule.BiomeId, patch.BiomeId, StringComparison.Ordinal))
                    return "PatchRuleIdentity";
                if (patch.SectorCount < rule.MinSectorCount ||
                    patch.SectorCount > Math.Min(rule.MaxSectorCount, 59))
                    return "PatchSize";
                if (patch.Role == BiomePatchRole.Intrusion)
                {
                    if (!rule.AllowSingleSector || patch.SectorCount != 1 || patch.Seeds.Count != 1 ||
                        patch.Seeds[0].SourceSiteReservationId.HasValue ||
                        output.SiteBindings.Any(value => value.PatchId == patch.Id))
                        return "IntrusionShape";
                }
                else
                {
                    if (!IsCardinalConnected(patch.SectorIndices)) return "NormalConnectivity";
                    if (!source.TryGetPatch(patch.Id, out var sourcePatch) ||
                        sourcePatch.Seeds.Count != patch.Seeds.Count ||
                        !sourcePatch.Seeds.Zip(patch.Seeds, (left, right) =>
                            left.SectorIndex == right.SectorIndex &&
                            left.SourceSiteReservationId == right.SourceSiteReservationId).All(value => value))
                        return "NormalSeedPreservation";
                    normalCounts[patch.BiomeId] = normalCounts.TryGetValue(patch.BiomeId, out var count)
                        ? count + 1
                        : 1;
                }
                foreach (var seed in patch.Seeds)
                {
                    var ownership = output.GetSector(seed.SectorIndex);
                    if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                        ownership.PatchId.Value != patch.Id)
                        return "SeedOwnership";
                }
            }
            foreach (var biome in biomes)
            {
                var count = normalCounts.TryGetValue(biome.BiomeId, out var value) ? value : 0;
                if (count < biome.MinPatchCount || count > biome.MaxPatchCount)
                    return "NormalPatchCount";
            }
            if (output.SiteBindings.Count != source.SiteBindings.Count ||
                !output.SiteBindings.Zip(source.SiteBindings, ReferenceEquals).All(value => value))
                return "SiteBindingPreservation";
            foreach (var binding in output.SiteBindings)
                foreach (var sectorIndex in binding.OccupiedSectorIndices)
                {
                    var ownership = output.GetSector(sectorIndex);
                    if (!ownership.IsAssigned || !ownership.PatchId.HasValue ||
                        ownership.PatchId.Value != binding.PatchId)
                        return "SiteOwnership";
                }
            var records = publication.Intrusions.ToDictionary(value => value.SectorIndex);
            for (var index = 0; index < output.Sectors.Count; index++)
            {
                var before = source.GetSector(index);
                var after = output.GetSector(index);
                if (records.TryGetValue(index, out var record))
                {
                    if (!before.IsAssigned || !before.PatchId.HasValue ||
                        before.PatchId.Value != record.DonorPatchId || !after.IsAssigned ||
                        !after.PatchId.HasValue || after.PatchId.Value != record.IntrusionPatchId ||
                        record.SharedIntruderEdgeCount < 1)
                        return "IntrusionTransfer";
                }
                else if (before.IsAssigned != after.IsAssigned ||
                    before.PatchId != after.PatchId ||
                    !string.Equals(before.PrimaryBiomeId, after.PrimaryBiomeId, StringComparison.Ordinal))
                    return "NonselectedMutation";
            }
            var donorSizes = source.Patches
                .Where(value => value.Role != BiomePatchRole.Intrusion)
                .ToDictionary(value => value.Id, value => value.SectorCount);
            var touchedDonors = new HashSet<BiomePatchId>();
            foreach (var record in publication.Intrusions.OrderBy(value => value.Sequence))
            {
                if (!donorSizes.TryGetValue(record.DonorPatchId, out var before) ||
                    record.DonorSizeBefore != before || record.DonorSizeAfter != before - 1)
                    return "DonorConservation";
                donorSizes[record.DonorPatchId] = record.DonorSizeAfter;
                touchedDonors.Add(record.DonorPatchId);
            }
            foreach (var donorId in touchedDonors)
                if (!output.TryGetPatch(donorId, out var donorAfter) ||
                    donorAfter.SectorCount != donorSizes[donorId])
                    return "DonorConservation";
            return "NONE";
        }

        private static bool IsCardinalConnected(IEnumerable<int> source)
        {
            var values = new HashSet<int>(source);
            if (values.Count == 0) return false;
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            var start = values.Min();
            visited.Add(start);
            queue.Enqueue(start);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in new[]
                {
                    WorldGridIndex.GetLeftIndex(current), WorldGridIndex.GetRightIndex(current),
                    WorldGridIndex.GetUpIndex(current), WorldGridIndex.GetDownIndex(current)
                })
                    if (neighbor >= 0 && values.Contains(neighbor) && visited.Add(neighbor))
                        queue.Enqueue(neighbor);
            }
            return visited.Count == values.Count;
        }

        private static void ValidateSuccessfulAttempt(AttemptRecord record)
        {
            var cleanup = record.Cleanup;
            var export = record.Export;
            var validation = record.Validation;
            var overlay = record.Overlay;
            var snapshot = cleanup.Publication.Snapshot;
            var source = cleanup.Publication.SourceIntrusion.Publication.SourceSiteSnapshot;
            var diagnostics = validation.Diagnostics;
            var patchBytes = export.Publication.GeneratedBiomePatchesCsv;
            var worldBytes = export.Publication.GeneratedWorldSectorsCsv;

            {
                Assert.That(validation.Status, Is.EqualTo(BiomePatchValidationStatus.Completed));
                Assert.That(validation.Violations, Is.Empty);
                Assert.That(validation.Errors, Is.Empty);
                Assert.That(diagnostics.RuleResults, Has.Count.EqualTo(15));
                Assert.That(diagnostics.RuleResults.All(value => value.Passed), Is.True);
                Assert.That(diagnostics.SiteMisownershipCount, Is.Zero);
                Assert.That(diagnostics.DisconnectedPatchCount, Is.Zero);
                Assert.That(diagnostics.OverlapCount, Is.Zero);
                Assert.That(diagnostics.OrphanCount, Is.Zero);
                Assert.That(diagnostics.UnassignedNonReservedCount, Is.Zero);
                Assert.That(diagnostics.IntrusionInvalidCount, Is.Zero);
                Assert.That(diagnostics.RngDrawCount, Is.Zero);
                Assert.That(diagnostics.SourceMutationCount, Is.Zero);
                Assert.That(snapshot.Seed, Is.EqualTo(record.WorldSeed));
                Assert.That(snapshot.Sectors, Has.Count.EqualTo(169));
                Assert.That(snapshot.AssignedSectorCount, Is.EqualTo(165));
                Assert.That(snapshot.UnassignedSectorCount, Is.EqualTo(4));
                Assert.That(snapshot.Patches.Sum(value => value.SectorCount), Is.EqualTo(165));
                Assert.That(snapshot.SiteBindings, Has.Count.EqualTo(4));
                Assert.That(snapshot.Sectors.Count(value => !string.IsNullOrEmpty(value.SecondaryBiomeId)), Is.Zero);
                Assert.That(snapshot.Sectors.Where(value => !value.IsAssigned)
                    .All(value => source.GetSector(value.SectorIndex).IsReserved), Is.True);
                Assert.That(snapshot.Patches.Where(value => value.Role != BiomePatchRole.Intrusion)
                    .All(value => value.SectorCount >= 2 && value.SectorCount <= 59), Is.True);
                Assert.That(snapshot.Patches.Where(value => value.Role == BiomePatchRole.Intrusion)
                    .All(value => value.SectorCount == 1), Is.True);
                Assert.That(export.Publication.PatchRowCount, Is.EqualTo(snapshot.Patches.Count));
                Assert.That(export.Publication.WorldSectorRowCount, Is.EqualTo(169));
                Assert.That(GeneratedBiomePatchCsvSerializer.Serialize(export.Publication.PatchRows), Is.EqualTo(patchBytes));
                Assert.That(GeneratedWorldDataCsvSerializer.Serialize(export.Publication.WorldWithBiomeAssignments), Is.EqualTo(worldBytes));
                Assert.That(overlay.WorldSeed, Is.EqualTo(record.WorldSeed));
                Assert.That(overlay.Cells, Has.Count.EqualTo(169));
                Assert.That(overlay.Patches, Has.Count.EqualTo(snapshot.Patches.Count));
                Assert.That(cleanup.Diagnostics.FinalRngDrawCount, Is.EqualTo(cleanup.Diagnostics.SourceRngDrawCount));
                Assert.That(cleanup.Diagnostics.RngMethodCallCount, Is.Zero);
                Assert.That(cleanup.Diagnostics.SourceMutationCount, Is.Zero);
                Assert.That(cleanup.Diagnostics.Rollback, Is.False);
            }

            foreach (var binding in snapshot.SiteBindings)
                foreach (var sectorIndex in binding.OccupiedSectorIndices)
                {
                    var ownership = snapshot.GetSector(sectorIndex);
                    Assert.That(ownership.PrimaryBiomeId, Is.EqualTo(binding.BiomeId));
                    Assert.That(ownership.PatchId, Is.EqualTo(binding.PatchId));
                }
        }

        private static void ValidateWorldResult(WorldResult result)
        {
            if (result.Disposition == WorldDisposition.Completed)
            {
                ValidateSuccessfulAttempt(result.Final);
                return;
            }

            if (result.Disposition == WorldDisposition.PassSiteHandoffRequired)
            {
                ValidatePassSiteHandoff(result);
                return;
            }

            Assert.That(result.Disposition, Is.EqualTo(WorldDisposition.Invalid));
            Assert.That(result.Attempts.Count, Is.InRange(1, 100));
            Assert.That(result.Final, Is.SameAs(result.Attempts[result.Attempts.Count - 1]));
            Assert.That(result.Final.Completed, Is.False);
            Assert.That(result.Final.RetryRequired && result.Final.SourceUnchanged, Is.False);
            Assert.That(result.Final.Cleanup, Is.Null);
            Assert.That(result.Final.Export, Is.Null);
            Assert.That(result.Final.Validation, Is.Null);
            Assert.That(result.Final.Overlay, Is.Null);
            Assert.That(result.CanonicalDigest, Has.Length.EqualTo(64));
        }

        private static void ValidatePassSiteHandoff(WorldResult result)
        {
            Assert.That(result.Disposition, Is.EqualTo(WorldDisposition.PassSiteHandoffRequired));
            Assert.That(result.Attempts, Has.Count.EqualTo(100));
            Assert.That(result.Retries, Has.Count.EqualTo(100));
            Assert.That(result.Final, Is.SameAs(result.Attempts[99]));
            Assert.That(result.Final.AttemptOrdinal, Is.EqualTo(99));
            Assert.That(result.CanonicalDigest, Has.Length.EqualTo(64));

            for (var ordinal = 0; ordinal < result.Attempts.Count; ordinal++)
            {
                var record = result.Attempts[ordinal];
                Assert.That(record.AttemptOrdinal, Is.EqualTo(ordinal));
                Assert.That(record.Completed, Is.False);
                Assert.That(record.RetryRequired, Is.True,
                    "attempt=" + ordinal + " stage=" + record.TerminalStage + " reason=" + record.Reason);
                Assert.That(record.SourceUnchanged, Is.True);
                Assert.That(IsAllowedHandoffFailure(record), Is.True,
                    "attempt=" + ordinal + " stage=" + record.TerminalStage + " reason=" + record.Reason);
                Assert.That(record.Cleanup, Is.Null);
                Assert.That(record.Export, Is.Null);
                Assert.That(record.Validation, Is.Null);
                Assert.That(record.Overlay, Is.Null);
                Assert.That(record.PatchCount, Is.Zero);
                Assert.That(record.AssignedCount, Is.Zero);
                Assert.That(record.UnassignedCount, Is.Zero);
                Assert.That(record.CanonicalDigest, Has.Length.EqualTo(64));
            }
        }

        private static bool IsAllowedHandoffFailure(AttemptRecord record)
        {
            switch (record.TerminalStage + ":" + record.Reason)
            {
                case "CorePatchGrower:BufferOutsideWorld":
                case "CorePatchGrower:BufferBlockedByReservation":
                case "CorePatchGrower:MandatoryBufferConflict":
                case "CorePatchGrower:InsufficientUnreservedCapacity":
                case "SatelliteSeedPlacer:CandidateAttemptsExhausted":
                case "MultiSeedBiomeGrower:InsufficientAggregateCapacity":
                case "MultiSeedBiomeGrower:MinimumGrowthBlocked":
                case "MultiSeedBiomeGrower:GrowthFrontierExhausted":
                case "IntrusionPlacer:NoLegalIntrusionCandidate":
                case "PatchCleanup:NoSafeCleanupMove":
                case "PatchCleanup:CleanupStepLimitExceeded":
                    return true;
                default:
                    return false;
            }
        }

        private static IReadOnlyList<T> Ordered<T>(IReadOnlyList<T> source, bool reverse)
        {
            return reverse ? source.Reverse().ToArray() : source.ToArray();
        }

        private static string ErrorCodes(IEnumerable<string> errors)
        {
            return string.Join(",", errors);
        }

        private static string SnapshotDigest(BiomePatchSnapshot snapshot)
        {
            var builder = new StringBuilder();
            foreach (var patch in snapshot.Patches)
                builder.Append(patch.Id.Value).Append(':').Append(patch.BiomeId).Append(':')
                    .Append(patch.Role).Append(':').Append(string.Join(",", patch.SectorIndices)).Append('|');
            builder.Append('#');
            foreach (var ownership in snapshot.Sectors)
                builder.Append(ownership.SectorIndex).Append(':')
                    .Append(ownership.IsAssigned ? ownership.PrimaryBiomeId : "_").Append(':')
                    .Append(ownership.PatchId.HasValue ? ownership.PatchId.Value.Value : "_").Append('|');
            return Sha256(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private static string OverlayDigest(BiomePatchOverlaySnapshot overlay)
        {
            var builder = new StringBuilder();
            foreach (var cell in overlay.Cells)
                builder.Append(cell.Index).Append(':').Append(cell.CellLabel).Append(':')
                    .Append(cell.BorderLeft ? '1' : '0').Append(cell.BorderRight ? '1' : '0')
                    .Append(cell.BorderUp ? '1' : '0').Append(cell.BorderDown ? '1' : '0').Append('|');
            builder.Append('#');
            foreach (var patch in overlay.Patches)
                builder.Append(patch.PatchId.Value).Append(':').Append(patch.Size).Append(':')
                    .Append(patch.Perimeter).Append(':').Append(patch.CompactnessPermille).Append('|');
            return Sha256(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private static string ReservationSignature(SiteReservationSnapshot snapshot)
        {
            return snapshot.Seed.ToString(CultureInfo.InvariantCulture) + "|" +
                string.Join(";", snapshot.Sectors.Select(value =>
                    value.Index + ":" + value.IsReserved + ":" +
                    (value.ReservationId.HasValue ? value.ReservationId.Value.Value : string.Empty)));
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static GeneratedWorldData CreateSourceWorld(ulong seed)
        {
            var cells = new List<SectorCell>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                cells.Add(new SectorCell(
                    index, WorldGridIndex.ToCoordinate(index), GeneratedSectorRole.Unassigned,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, -1, false));
            }
            return new GeneratedWorldData(seed, cells);
        }

        private static SiteReservationSnapshot BuildSourceSnapshot(ulong worldSeed)
        {
            var reservations = new List<SiteReservation>
            {
                CreateReservation(0, StartId, "WORLD_MOONPALACE_V1", SiteReservationKind.Start, string.Empty, new SectorCoord(0, 0), 1),
                CreateReservation(1, BossId, "SITE_MOON_BOSS_VAULT", SiteReservationKind.Boss, "BIO_ABANDONED_MILL", new SectorCoord(12, 12), 1),
                CreateReservation(2, ForgeId, "SITE_MOON_SEAL_FORGE", SiteReservationKind.Forge, "BIO_ABANDONED_MILL", new SectorCoord(2, 2), 1),
                CreateReservation(3, CassiaId, "SITE_CASSIA_SAP_HEART", SiteReservationKind.CoreResource, "BIO_CASSIA_ROOT", new SectorCoord(8, 2), 1),
                CreateReservation(4, DoughId, "SITE_DEEP_STAR_YEAST", SiteReservationKind.CoreResource, "BIO_MOON_DOUGH", new SectorCoord(2, 8), 1),
                CreateReservation(5, CraterId, "SITE_MOON_CORE_METEOR", SiteReservationKind.CoreResource, "BIO_MOON_CRATER", new SectorCoord(8, 8), 1),
                CreateReservation(6, VillageId, "SITE_PRIMARY_VILLAGE", SiteReservationKind.Village, string.Empty, new SectorCoord(0, 12), 2)
            };
            var byId = reservations.ToDictionary(value => value.ReservationId.Value, StringComparer.Ordinal);
            var seeds = new[]
            {
                CoreSeed(byId[ForgeId], "BIO_ABANDONED_MILL", "PATCH_MILL_CORE", 4),
                CoreSeed(byId[CassiaId], "BIO_CASSIA_ROOT", "PATCH_ROOT_CORE", 5),
                CoreSeed(byId[DoughId], "BIO_MOON_DOUGH", "PATCH_DOUGH_CORE", 5),
                CoreSeed(byId[CraterId], "BIO_MOON_CRATER", "PATCH_CRATER_CORE", 5)
            };
            return new SiteReservationSnapshot(
                worldSeed, reservations, CreateSectorReservations(reservations), seeds);
        }

        private static SiteReservation CreateReservation(
            int order, string reservationId, string sourceDefinitionId,
            SiteReservationKind kind, string biomeId, SectorCoord origin, int width)
        {
            var cells = Enumerable.Range(0, width).Select(localX => new SiteFootprintCell(
                localX, 0, kind == SiteReservationKind.Start ? "START" : "CORE",
                biomeId, string.Empty, Array.Empty<SiteEntrySide>()));
            return new SiteReservation(
                new SiteReservationId(reservationId), kind, sourceDefinitionId, origin,
                new SiteFootprint(width, 1, SiteFootprintTransform.R0, cells), biomeId,
                order, Array.Empty<SiteEntryAnchor>());
        }

        private static CoreBiomeSeed CoreSeed(
            SiteReservation reservation, string biomeId, string ruleId, int minimum)
        {
            return new CoreBiomeSeed(
                reservation.ReservationId, biomeId, ruleId,
                reservation.OccupiedSectors.OrderBy(WorldGridIndex.ToIndex).First(), minimum, 1);
        }

        private static List<SectorReservation> CreateSectorReservations(
            IEnumerable<SiteReservation> reservations)
        {
            var occupied = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var reservation in reservations)
                foreach (var coordinate in reservation.OccupiedSectors)
                {
                    reservation.TryGetFootprintCell(coordinate, out var cell);
                    occupied.Add(coordinate, Tuple.Create(reservation, cell));
                }
            var result = new List<SectorReservation>();
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (occupied.TryGetValue(coordinate, out var binding))
                    result.Add(SectorReservation.CreateReserved(
                        index, coordinate, binding.Item1.ReservationId, binding.Item1.Kind,
                        binding.Item2.LocalX, binding.Item2.LocalY, binding.Item2.LocalRole));
                else result.Add(SectorReservation.CreateUnreserved(index, coordinate));
            }
            return result;
        }

        private static Fixture BuildFixture()
        {
            var definitions = BuildBiomeDefinitions();
            var routes = BuildRouteDefinitions();
            return new Fixture(
                definitions, routes,
                routes.GenerationProfiles["GEN_MOONPALACE_V1"],
                new WorldGenerationRngStreams(routes));
        }

        private static BiomeDefinitions BuildBiomeDefinitions()
        {
            var specs = CreateBiomeFileSpecs();
            var rows = new Dictionary<string, string[][]>(StringComparer.Ordinal)
            {
                { "biome_types.csv", new[]
                    {
                        BiomeRow("BIO_MOON_CRATER", 0, 7, "1.0"),
                        BiomeRow("BIO_CASSIA_ROOT", 2, 12, "1.0"),
                        BiomeRow("BIO_ABANDONED_MILL", 1, 11, "0.9"),
                        BiomeRow("BIO_MOON_DOUGH", 0, 7, "1.0")
                    }
                },
                { "biome_patch_rules.csv", PatchRows() },
                { "biome_boundary_profiles.csv", ProfileRows() },
                { "biome_boundary_pair_rules.csv", PairRows() },
                { "boundary_chunk_catalog.csv", Array.Empty<string[]>() }
            };
            var sources = specs.Select(spec => BuildBiomeSource(spec, rows[spec.FileName])).ToArray();
            var result = new BiomeBoundaryDefinitionBuilder().Build(sources);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            var set = result.DefinitionSet;
            var allRules = set.BiomePatchRules.Values.ToArray();
            return new BiomeDefinitions(
                set.BiomeTypes.Values.ToArray(),
                allRules.Where(value => value.PatchRole == "CORE").ToArray(),
                allRules.Where(value => value.PatchRole == "SATELLITE").ToArray(),
                allRules.Where(value => value.PatchRole != "INTRUSION").ToArray(),
                allRules, set.BoundaryProfiles.Values.ToArray(), set.BoundaryPairRules.Values.ToArray());
        }

        private static string[] BiomeRow(string id, int minY, int maxY, string weight)
        {
            return new[]
            {
                id, "NAME", "STAGE_MOON_01", "1", "1", "4", "1",
                minY.ToString(CultureInfo.InvariantCulture), maxY.ToString(CultureInfo.InvariantCulture),
                weight, "THEME", "AUDIO", "MICRO", "RECIPE", "RESOURCE", "ELEMENT",
                "SITE_REQUIRED", "1", string.Empty
            };
        }

        private static string[][] PatchRows()
        {
            return new[]
            {
                PatchRow("PATCH_CRATER_CORE", "BIO_MOON_CRATER", "CORE", 5, 18, 4, 1, 1, "100", true, 1, false, "0.35", "1.0", "0.25", "0.45", "0.75", "0.45"),
                PatchRow("PATCH_CRATER_SAT", "BIO_MOON_CRATER", "SATELLITE", 2, 16, 3, 0, 3, "70", true, 0, false, "0.35", "1.0", "0.25", "0.6", "0.65", "0.55"),
                PatchRow("PATCH_ROOT_CORE", "BIO_CASSIA_ROOT", "CORE", 5, 18, 4, 1, 1, "100", false, 1, false, "0.35", "1.0", "0.35", "0.45", "0.7", "0.55"),
                PatchRow("PATCH_ROOT_SAT", "BIO_CASSIA_ROOT", "SATELLITE", 2, 14, 3, 0, 3, "70", false, 0, false, "0.35", "1.0", "0.35", "0.6", "0.6", "0.65"),
                PatchRow("PATCH_MILL_CORE", "BIO_ABANDONED_MILL", "CORE", 4, 14, 4, 1, 1, "100", false, 1, false, "0.35", "1.0", "0.2", "0.35", "0.85", "0.3"),
                PatchRow("PATCH_MILL_SAT", "BIO_ABANDONED_MILL", "SATELLITE", 2, 10, 3, 0, 2, "45", false, 0, false, "0.35", "1.0", "0.2", "0.5", "0.8", "0.35"),
                PatchRow("PATCH_DOUGH_CORE", "BIO_MOON_DOUGH", "CORE", 5, 18, 4, 1, 1, "100", true, 1, false, "0.35", "1.0", "0.4", "0.45", "0.7", "0.5"),
                PatchRow("PATCH_DOUGH_SAT", "BIO_MOON_DOUGH", "SATELLITE", 2, 14, 3, 0, 3, "70", true, 0, false, "0.35", "1.0", "0.4", "0.6", "0.65", "0.6"),
                PatchRow("PATCH_ROOT_INTRUSION", "BIO_CASSIA_ROOT", "INTRUSION", 1, 5, 2, 0, 2, "20", false, 0, true, "0.1", "1.0", "0.3", "0.8", "0.2", "0.9"),
                PatchRow("PATCH_MILL_INTRUSION", "BIO_ABANDONED_MILL", "INTRUSION", 1, 4, 2, 0, 2, "15", false, 0, true, "0.1", "1.0", "0.1", "0.8", "0.25", "0.85")
            };
        }

        private static string[] PatchRow(
            string id, string biome, string role, int min, int max, int distance,
            int countMin, int countMax, string seedWeight, bool edge, int buffer,
            bool single, string share, string distanceWeight, string altitudeWeight,
            string noiseWeight, string compactnessWeight, string branchiness)
        {
            return new[]
            {
                id, biome, role, min.ToString(CultureInfo.InvariantCulture),
                max.ToString(CultureInfo.InvariantCulture), distance.ToString(CultureInfo.InvariantCulture),
                countMin.ToString(CultureInfo.InvariantCulture), countMax.ToString(CultureInfo.InvariantCulture),
                seedWeight, edge ? "1" : "0", buffer.ToString(CultureInfo.InvariantCulture),
                single ? "1" : "0", share, distanceWeight, altitudeWeight,
                noiseWeight, compactnessWeight, branchiness, "1", string.Empty
            };
        }

        private static string[][] ProfileRows()
        {
            return new[]
            {
                new[] { "BOUND_SOFT_BLEND", "NAME", "SOFT_BLEND", "HORIZONTAL|VERTICAL", "1", "2", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_CLIFF", "NAME", "CLIFF", "HORIZONTAL|VERTICAL", "1", "2", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_TUNNEL", "NAME", "TUNNEL_INTRUSION", "HORIZONTAL|VERTICAL", "1", "3", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_LAYER", "NAME", "LAYER", "VERTICAL", "1", "2", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_RUIN", "NAME", "RUIN", "HORIZONTAL|VERTICAL", "1", "3", "2", "1", "NONE", "0", "1", "" },
                new[] { "BOUND_HARD_STARSTONE", "NAME", "HARD_STARSTONE", "HORIZONTAL|VERTICAL", "1", "1", "1", "0", "NONE", "1", "1", "" }
            };
        }

        private static string[][] PairRows()
        {
            return new[]
            {
                PairRow("PAIR_CRATER_ROOT", "BIO_MOON_CRATER", "BIO_CASSIA_ROOT", "BOUND_SOFT_BLEND|BOUND_CLIFF|BOUND_TUNNEL", "50|25|25", "BOUND_SOFT_BLEND"),
                PairRow("PAIR_CRATER_MILL", "BIO_MOON_CRATER", "BIO_ABANDONED_MILL", "BOUND_RUIN|BOUND_SOFT_BLEND", "70|30", "BOUND_RUIN"),
                PairRow("PAIR_CRATER_DOUGH", "BIO_MOON_CRATER", "BIO_MOON_DOUGH", "BOUND_CLIFF|BOUND_LAYER|BOUND_SOFT_BLEND", "45|35|20", "BOUND_CLIFF"),
                PairRow("PAIR_ROOT_MILL", "BIO_CASSIA_ROOT", "BIO_ABANDONED_MILL", "BOUND_RUIN|BOUND_TUNNEL|BOUND_SOFT_BLEND", "45|35|20", "BOUND_RUIN"),
                PairRow("PAIR_ROOT_DOUGH", "BIO_CASSIA_ROOT", "BIO_MOON_DOUGH", "BOUND_TUNNEL|BOUND_LAYER|BOUND_SOFT_BLEND", "45|30|25", "BOUND_TUNNEL"),
                PairRow("PAIR_MILL_DOUGH", "BIO_ABANDONED_MILL", "BIO_MOON_DOUGH", "BOUND_RUIN|BOUND_LAYER|BOUND_TUNNEL", "45|30|25", "BOUND_RUIN")
            };
        }

        private static string[] PairRow(
            string id, string biomeA, string biomeB, string profiles, string weights, string defaultProfile)
        {
            return new[]
            {
                id, biomeA, biomeB, profiles, weights, defaultProfile,
                "POOL_RESOURCE", "POOL_ELEMENT", "1", "1", string.Empty
            };
        }

        private static FileSpec[] CreateBiomeFileSpecs()
        {
            return new[]
            {
                File("biome_types.csv", "biome_id:ID", "display_name_ko:STRING", "stage_id:ID", "required:BOOL", "min_patch_count:INT", "max_patch_count:INT", "min_core_patch_count:INT", "preferred_altitude_min_sector_y:INT", "preferred_altitude_max_sector_y:INT", "growth_weight:FLOAT", "tile_theme_id:ID", "audio_profile_id:ID", "microchunk_pool_prefix:ID", "sector_recipe_pool_prefix:ID", "common_resource_pool_id:ID", "map_element_pool_id:ID", "required_special_map_ids:ID_LIST", "active:BOOL", "notes:STRING"),
                File("biome_patch_rules.csv", "patch_rule_id:ID", "biome_id:ID", "patch_role:ENUM", "min_sector_count:INT", "max_sector_count:INT", "min_seed_distance:INT", "seed_count_min:INT", "seed_count_max:INT", "seed_weight:FLOAT", "can_touch_world_edge:BOOL", "buffer_ring_sectors:INT", "allow_single_sector:BOOL", "max_world_share:FLOAT", "distance_weight:FLOAT", "altitude_weight:FLOAT", "noise_weight:FLOAT", "compactness_weight:FLOAT", "branchiness_target:FLOAT", "active:BOOL", "notes:STRING"),
                File("biome_boundary_profiles.csv", "boundary_profile_id:ID", "display_name_ko:STRING", "boundary_type:ENUM", "allowed_orientations:ENUM_LIST", "width_microchunks_min:INT", "width_microchunks_max:INT", "warning_microchunks_min:INT", "mandatory_route_allowed:BOOL", "tool_requirement:ENUM", "hard_border:BOOL", "active:BOOL", "notes:STRING"),
                File("biome_boundary_pair_rules.csv", "boundary_pair_rule_id:ID", "biome_a_id:ID", "biome_b_id:ID", "allowed_boundary_profile_ids:ID_LIST", "boundary_profile_weights:INT_LIST", "default_boundary_profile_id:ID", "transition_resource_pool_id:ID", "transition_element_pool_id:ID", "min_shared_edge_count:INT", "active:BOOL", "notes:STRING"),
                File("boundary_chunk_catalog.csv", "boundary_chunk_id:ID", "microchunk_id:ID", "biome_a_id:ID", "biome_b_id:ID", "boundary_profile_id:ID", "orientation:ENUM", "route_type:INT", "entry_edge_signature_id:ID", "exit_edge_signature_id:ID", "weight:INT", "reversible:BOOL", "active:BOOL", "notes:STRING")
            };
        }

        private static WorldRouteDefinitionSet BuildRouteDefinitions()
        {
            var specs = CreateWorldFileSpecs();
            var rows = new Dictionary<string, string[][]>(StringComparer.Ordinal)
            {
                { "generation_profiles.csv", new[] { new[]
                    {
                        "GEN_MOONPALACE_V1", "WORLD_MOONPALACE_V1", "75", "105", "40", "70",
                        "8", "16", "7", "30", "0", "1", "4", "10", "1", "4", "8", "18",
                        "200", "100", "200", "20", "1", string.Empty
                    } }
                },
                { "rng_streams.csv", new[]
                    {
                        new[] { "RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD", "test", "1" },
                        new[] { "RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS", "test", "1" },
                        new[] { "RNG_ROUTE", "C00FEE12AB341901", "PASS", "test", "1" },
                        new[] { "RNG_TYPE0", "D15EA5E007A4C883", "PASS", "test", "1" },
                        new[] { "RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR", "test", "1" },
                        new[] { "RNG_POPULATION", "F123456789ABCDEF", "SPAWN", "test", "1" }
                    }
                }
            };
            var sources = specs.Select(spec => BuildWorldRouteSource(
                spec, rows.TryGetValue(spec.FileName, out var value) ? value : Array.Empty<string[]>())).ToArray();
            var result = new WorldRouteDefinitionBuilder().Build(sources);
            if (!result.Success) throw new InvalidOperationException(string.Join("\n", result.Errors));
            return result.DefinitionSet;
        }

        private static FileSpec[] CreateWorldFileSpecs()
        {
            return new[]
            {
                File("world_profiles.csv", "world_profile_id:ID", "display_name_ko:STRING", "width_tiles:INT", "height_tiles:INT", "sector_width_tiles:INT", "sector_height_tiles:INT", "sector_cols:INT", "sector_rows:INT", "micro_width_tiles:INT", "micro_height_tiles:INT", "micro_cols_per_sector:INT", "micro_rows_per_sector:INT", "min_completion_distance_tiles:INT", "max_shortest_completion_distance_tiles:INT", "normal_completion_min_tiles:INT", "normal_completion_max_tiles:INT", "optional_completion_max_tiles:INT", "max_revisit_ratio:FLOAT", "required_village_count:INT", "active:BOOL", "notes:STRING"),
                File("generation_profiles.csv", "generation_profile_id:ID", "world_profile_id:ID", "mandatory_sector_min:INT", "mandatory_sector_max:INT", "type0_sector_min:INT", "type0_sector_max:INT", "reserved_sector_min:INT", "reserved_sector_max:INT", "inactive_sector_min:INT", "inactive_sector_max:INT", "start_edge_ring_min:INT", "start_edge_ring_max:INT", "mandatory_loop_min:INT", "mandatory_loop_max:INT", "optional_region_depth_min:INT", "optional_region_depth_max:INT", "optional_region_count_min:INT", "optional_region_count_max:INT", "site_reservation_retry_max:INT", "biome_retry_max:INT", "route_retry_max:INT", "sector_solve_retry_max:INT", "active:BOOL", "notes:STRING"),
                File("generation_passes.csv", "generation_profile_id:ID", "pass_order:INT", "pass_id:ID", "class_name:STRING", "rng_stream_id:ID", "input_artifacts:ID_LIST", "output_artifacts:ID_LIST", "failure_policy:ENUM", "max_retry_count:INT", "enabled:BOOL", "notes:STRING"),
                File("rng_streams.csv", "rng_stream_id:ID", "salt_hex:HEX", "reset_scope:ENUM", "description_ko:STRING", "active:BOOL"),
                File("sector_route_masks.csv", "route_mask_id:ID", "route_type:INT", "open_l:BOOL", "open_r:BOOL", "open_u:BOOL", "open_d:BOOL", "mandatory_allowed:BOOL", "description_ko:STRING", "active:BOOL"),
                File("socket_band_definitions.csv", "band_id:ID", "axis:ENUM", "min_local_coord:INT", "max_local_coord:INT", "recommended_center:FLOAT", "minimum_clearance_tiles:INT", "description_ko:STRING"),
                File("edge_signatures.csv", "edge_signature_id:ID", "axis:ENUM", "band_id:ID", "traversal_kind:ENUM", "ground_entry_height:INT", "clearance_width:INT", "clearance_height:INT", "tool_requirement:ENUM", "mandatory_allowed:BOOL", "tags:ID_LIST", "notes:STRING"),
                File("edge_signature_compatibility.csv", "signature_a:ID", "signature_b:ID", "compatible:BOOL", "adapter_microchunk_pool_id:ID", "notes:STRING"),
                File("sector_recipe_catalog.csv", "sector_recipe_id:ID", "display_name_ko:STRING", "route_type:INT", "route_mask_id:ID", "primary_biome_id:ID", "secondary_biome_id:ID", "boundary_profile_id:ID", "recipe_kind:ENUM", "microchunk_budget_profile_id:ID", "selection_weight:INT", "supports_special_entry:BOOL", "supports_village_entry:BOOL", "active:BOOL", "notes:STRING"),
                File("sector_recipe_cells.csv", "sector_recipe_id:ID", "chunk_x:INT", "chunk_y:INT", "cell_role:ENUM", "fixed_microchunk_id:ID", "microchunk_pool_id:ID", "required_usage_class:ENUM_LIST", "required_route_roles:ID_LIST", "required_biome_ids:ID_LIST", "required_signature_l:ID", "required_signature_r:ID", "required_signature_u:ID", "required_signature_d:ID", "transform_policy:ENUM_LIST", "notes:STRING"),
                File("sector_recipe_paths.csv", "sector_recipe_id:ID", "path_id:ID", "path_order:INT", "chunk_x:INT", "chunk_y:INT", "enter_side:ENUM", "exit_side:ENUM", "mandatory:BOOL", "traversal_kind:ENUM", "max_jump_tiles:INT", "notes:STRING"),
                File("sector_external_sockets.csv", "sector_recipe_id:ID", "socket_id:ID", "side:ENUM", "edge_chunk_index:INT", "band_id:ID", "traversal_kind:ENUM", "mandatory_allowed:BOOL", "edge_signature_id:ID", "notes:STRING"),
                File("sector_recipe_pool_entries.csv", "sector_recipe_pool_id:ID", "entry_order:INT", "sector_recipe_id:ID", "weight:INT", "min_repeat_distance_sectors:INT", "required_patch_role:ENUM", "active:BOOL")
            };
        }

        private static BiomeBoundaryDefinitionSource BuildBiomeSource(
            FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var schema = BuildSchema(spec);
            return new BiomeBoundaryDefinitionSource(schema, Parse(spec, rows, schema));
        }

        private static WorldRouteDefinitionSource BuildWorldRouteSource(
            FileSpec spec, IReadOnlyList<string[]> rows)
        {
            var schema = BuildSchema(spec);
            return new WorldRouteDefinitionSource(schema, Parse(spec, rows, schema));
        }

        private static CsvScalarAndListParseResult Parse(
            FileSpec spec, IReadOnlyList<string[]> rows, CsvFileSchema schema)
        {
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            foreach (var row in rows) csv += "\n" + string.Join(",", row.Select(CsvCell));
            var read = new Rfc4180CsvReader().Read(new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            if (!validation.Success) throw new InvalidOperationException(string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            if (!keys.Success) throw new InvalidOperationException("Primary-key fixture failed.");
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!parsed.Success) throw new InvalidOperationException(string.Join("\n", parsed.Errors));
            return parsed;
        }

        private static CsvFileSchema BuildSchema(FileSpec spec)
        {
            var rows = spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName, (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name, column.DataType, index == 0 ? "1" : "0",
                index == 0 ? "1" : string.Empty, string.Empty, column.AllowedValues,
                string.Empty, string.Empty, index + 2));
            var catalog = new CsvSchemaCatalogBuilder().Build(rows);
            if (!catalog.Success) throw new InvalidOperationException(string.Join("\n", catalog.Errors));
            return catalog.Catalog.GetFile(spec.FileName);
        }

        private static FileSpec File(string fileName, params string[] definitions)
        {
            return new FileSpec(fileName, definitions.Select(value =>
            {
                var parts = value.Split(':');
                return new ColumnSpec(parts[0], parts[1], AllowedValues(parts[0], parts[1]));
            }).ToArray());
        }

        private static string AllowedValues(string name, string type)
        {
            if (name == "patch_role") return "CORE|SATELLITE|INTRUSION";
            if (name == "reset_scope") return "WORLD|PASS|SECTOR|PATCH|SITE|SPAWN";
            if (name == "boundary_type") return "SOFT_BLEND|CLIFF|TUNNEL_INTRUSION|LAYER|RUIN|HARD_STARSTONE";
            if (name == "allowed_orientations" || name == "orientation") return "HORIZONTAL|VERTICAL";
            if (name == "tool_requirement") return "NONE";
            return type == "ENUM" || type == "ENUM_LIST" ? "ENUM_A|ENUM_B" : string.Empty;
        }

        private static string CsvCell(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private const string StartId = "RSV_00_WORLD_MOONPALACE_V1";
        private const string BossId = "RSV_01_SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "RSV_02_SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "RSV_03_SITE_CASSIA_SAP_HEART";
        private const string DoughId = "RSV_04_SITE_DEEP_STAR_YEAST";
        private const string CraterId = "RSV_05_SITE_MOON_CORE_METEOR";
        private const string VillageId = "RSV_06_SITE_PRIMARY_VILLAGE";

        private sealed class PipelineServices
        {
            public readonly CorePatchSeedInitializer Initializer = new CorePatchSeedInitializer();
            public readonly CorePatchGrower CoreGrower = new CorePatchGrower();
            public readonly SatelliteSeedPlacer SatellitePlacer = new SatelliteSeedPlacer();
            public readonly MultiSeedBiomeGrower BiomeGrower = new MultiSeedBiomeGrower();
            public readonly IntrusionPlacer IntrusionPlacer = new IntrusionPlacer();
            public readonly PatchCleanup Cleanup = new PatchCleanup();
            public readonly BiomePatchExporter Exporter = new BiomePatchExporter();
            public readonly BiomePatchValidator Validator = new BiomePatchValidator();
        }

        private sealed class Fixture
        {
            public Fixture(
                BiomeDefinitions definitions, WorldRouteDefinitionSet routes,
                GenerationProfileDefinition profile, WorldGenerationRngStreams rngStreams)
            {
                Definitions = definitions;
                Routes = routes;
                Profile = profile;
                RngStreams = rngStreams;
            }

            public BiomeDefinitions Definitions { get; }
            public WorldRouteDefinitionSet Routes { get; }
            public GenerationProfileDefinition Profile { get; }
            public WorldGenerationRngStreams RngStreams { get; }
        }

        private sealed class BiomeDefinitions
        {
            public BiomeDefinitions(
                IReadOnlyList<BiomeTypeDefinition> biomes,
                IReadOnlyList<BiomePatchRuleDefinition> coreRules,
                IReadOnlyList<BiomePatchRuleDefinition> satelliteRules,
                IReadOnlyList<BiomePatchRuleDefinition> coreAndSatelliteRules,
                IReadOnlyList<BiomePatchRuleDefinition> allRules,
                IReadOnlyList<BiomeBoundaryProfileDefinition> profiles,
                IReadOnlyList<BiomeBoundaryPairRuleDefinition> pairs)
            {
                Biomes = biomes;
                CoreRules = coreRules;
                SatelliteRules = satelliteRules;
                CoreAndSatelliteRules = coreAndSatelliteRules;
                AllRules = allRules;
                Profiles = profiles;
                Pairs = pairs;
            }

            public IReadOnlyList<BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> CoreRules { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> SatelliteRules { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> CoreAndSatelliteRules { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> AllRules { get; }
            public IReadOnlyList<BiomeBoundaryProfileDefinition> Profiles { get; }
            public IReadOnlyList<BiomeBoundaryPairRuleDefinition> Pairs { get; }
        }

        internal sealed class CleanupContractAudit
        {
            private readonly IReadOnlyList<string> legacyRows;

            public CleanupContractAudit(
                IEnumerable<string> legacyRows,
                int completed,
                int handoff,
                int invalid,
                int cleanupRejectedWitnesses,
                int sourceMutationCount)
            {
                var rows = legacyRows.ToArray();
                this.legacyRows = Array.AsReadOnly(rows);
                Completed = completed;
                Handoff = handoff;
                Invalid = invalid;
                CleanupRejectedWitnesses = cleanupRejectedWitnesses;
                SourceMutationCount = sourceMutationCount;
                LegacyLedgerDigest = Sha256(Encoding.UTF8.GetBytes(string.Join("\n", rows)));
            }

            public IReadOnlyList<string> LegacyRows => legacyRows;
            public int Completed { get; }
            public int Handoff { get; }
            public int Invalid { get; }
            public int CleanupRejectedWitnesses { get; }
            public int SourceMutationCount { get; }
            public string LegacyLedgerDigest { get; }
        }

        private enum WorldDisposition
        {
            Completed,
            PassSiteHandoffRequired,
            Invalid
        }

        private sealed class WorldResult
        {
            private readonly IReadOnlyList<AttemptRecord> attempts;
            private readonly IReadOnlyList<AttemptRecord> retries;

            public WorldResult(
                WorldDisposition disposition,
                AttemptRecord final,
                IEnumerable<AttemptRecord> attempts)
            {
                Disposition = disposition;
                Final = final;
                var orderedAttempts = attempts.ToArray();
                if (orderedAttempts.Length == 0 || !ReferenceEquals(final, orderedAttempts[orderedAttempts.Length - 1]))
                    throw new ArgumentException("The final attempt must be the last recorded attempt.", nameof(attempts));
                this.attempts = Array.AsReadOnly(orderedAttempts);
                retries = Array.AsReadOnly(orderedAttempts.Where(value => !value.Completed).ToArray());

                var digest = new StringBuilder();
                digest.Append(disposition).Append('|');
                foreach (var attempt in orderedAttempts)
                {
                    digest.Append(attempt.AttemptOrdinal.ToString(CultureInfo.InvariantCulture)).Append(':')
                        .Append(attempt.CanonicalDigest).Append('|');
                }
                CanonicalDigest = Sha256(Encoding.UTF8.GetBytes(digest.ToString()));
            }

            public WorldDisposition Disposition { get; }
            public bool Resolved => Disposition == WorldDisposition.Completed;
            public AttemptRecord Final { get; }
            public IReadOnlyList<AttemptRecord> Attempts => attempts;
            public IReadOnlyList<AttemptRecord> Retries => retries;
            public string CanonicalDigest { get; }
        }

        private sealed class AttemptRecord
        {
            public AttemptRecord(
                ulong worldSeed, int attemptOrdinal, bool completed, bool retryRequired,
                string terminalStage, string reason, ulong rngDrawCount, string stageStatuses,
                int patchCount, int coreCount, int satelliteCount, int intrusionCount,
                int assignedCount, int unassignedCount, int patchByteCount,
                string patchSha, string worldSha, int worldByteCount,
                string snapshotDigest, string canonicalDigest,
                bool sourceUnchanged, string failureDetail,
                string legacyCleanupFailureDetail,
                PatchCleanupResult cleanup, BiomePatchExportResult export,
                BiomePatchValidationResult validation, BiomePatchOverlaySnapshot overlay)
            {
                WorldSeed = worldSeed;
                AttemptOrdinal = attemptOrdinal;
                Completed = completed;
                RetryRequired = retryRequired;
                TerminalStage = terminalStage;
                Reason = reason;
                RngDrawCount = rngDrawCount;
                StageStatuses = stageStatuses;
                PatchCount = patchCount;
                CoreCount = coreCount;
                SatelliteCount = satelliteCount;
                IntrusionCount = intrusionCount;
                AssignedCount = assignedCount;
                UnassignedCount = unassignedCount;
                PatchByteCount = patchByteCount;
                PatchSha = patchSha;
                WorldSha = worldSha;
                WorldByteCount = worldByteCount;
                SnapshotDigest = snapshotDigest;
                CanonicalDigest = canonicalDigest;
                SourceUnchanged = sourceUnchanged;
                FailureDetail = failureDetail;
                LegacyCleanupFailureDetail = legacyCleanupFailureDetail;
                Cleanup = cleanup;
                Export = export;
                Validation = validation;
                Overlay = overlay;
            }

            public ulong WorldSeed { get; }
            public int AttemptOrdinal { get; }
            public bool Completed { get; }
            public bool RetryRequired { get; }
            public string TerminalStage { get; }
            public string Reason { get; }
            public ulong RngDrawCount { get; }
            public string StageStatuses { get; }
            public int PatchCount { get; }
            public int CoreCount { get; }
            public int SatelliteCount { get; }
            public int IntrusionCount { get; }
            public int AssignedCount { get; }
            public int UnassignedCount { get; }
            public int PatchByteCount { get; }
            public string PatchSha { get; }
            public string WorldSha { get; }
            public int WorldByteCount { get; }
            public int RuleCount => Validation == null ? 0 : Validation.Diagnostics.RuleResults.Count;
            public string SnapshotDigest { get; }
            public string CanonicalDigest { get; }
            public bool SourceUnchanged { get; }
            public string FailureDetail { get; }
            public string LegacyCleanupFailureDetail { get; }
            public PatchCleanupResult Cleanup { get; }
            public BiomePatchExportResult Export { get; }
            public BiomePatchValidationResult Validation { get; }
            public BiomePatchOverlaySnapshot Overlay { get; }
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, IReadOnlyList<ColumnSpec> columns)
            {
                FileName = fileName;
                Columns = columns;
            }

            public string FileName { get; }
            public IReadOnlyList<ColumnSpec> Columns { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(string name, string dataType, string allowedValues)
            {
                Name = name;
                DataType = dataType;
                AllowedValues = allowedValues;
            }

            public string Name { get; }
            public string DataType { get; }
            public string AllowedValues { get; }
        }
    }
}
