#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Campaign.P12;
using StarNight.Maru.P8;
using StarNight.Population.P7;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P12ChallengeEconomyAndRecordsTests
    {
        private readonly List<UnityEngine.Object> created =
            new List<UnityEngine.Object>();
        private string tempStorageRoot;

        [TearDown]
        public void TearDown()
        {
            P12PersistentStore.SetStorageRootForTests(null);
            if (tempStorageRoot != null
                && Directory.Exists(tempStorageRoot))
            {
                Directory.Delete(tempStorageRoot, true);
            }

            tempStorageRoot = null;
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        created[index]);
                }
            }

            created.Clear();
        }

        [Test]
        public void CrystalRules_DoubleGoldValueAndRejectNonPositive()
        {
            Assert.That(
                P12ReturnCrystalRules.ValueMultiplier,
                Is.EqualTo(2));
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(1),
                Is.EqualTo(2));
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(1),
                Is.EqualTo(P12ReturnCrystalRules.SmallCrystalValue));
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(3),
                Is.EqualTo(6));
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(
                    P7EconomyRules.BigGoldValue),
                Is.EqualTo(
                    P12ReturnCrystalRules.StandardCrystalValue));
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(0),
                Is.Zero);
            Assert.That(
                P12ReturnCrystalRules.CrystalValueFor(-5),
                Is.Zero);
        }

        [Test]
        public void Crystal_DriftsTowardTargetAndPrefersRunState()
        {
            GameObject anchor = Track("DriftAnchor");
            anchor.transform.position = new Vector3(10f, 0f, 0f);
            GameObject walletRoot = Track("Wallets");
            P5RunState2D runState =
                walletRoot.AddComponent<P5RunState2D>();
            P7EconomyWallet2D wallet =
                walletRoot.AddComponent<P7EconomyWallet2D>();
            runState.Configure(0);
            wallet.Configure(0);

            P12ReturnCrystal2D crystal =
                Track("Crystal").AddComponent<P12ReturnCrystal2D>();
            crystal.transform.position = Vector3.zero;
            crystal.Configure(
                P12ReturnCrystalRules.StandardCrystalValue,
                anchor.transform,
                runState,
                wallet);

            crystal.DriftForTests(1f);
            Assert.That(
                crystal.transform.position.x,
                Is.EqualTo(P12ReturnCrystal2D.DriftUnitsPerSecond)
                    .Within(0.0001f));

            Assert.That(crystal.CanCollect, Is.True);
            Assert.That(crystal.CollectForTests(), Is.True);
            Assert.That(
                runState.Gold,
                Is.EqualTo(
                    P12ReturnCrystalRules.StandardCrystalValue));
            Assert.That(
                wallet.Gold,
                Is.Zero,
                "P5 run state must take priority over the P7 wallet.");
            Assert.That(crystal.IsCollected, Is.True);
            Assert.That(crystal.gameObject.activeSelf, Is.False);
            Assert.That(crystal.CollectForTests(), Is.False);

            P12ReturnCrystal2D walletOnly =
                Track("WalletCrystal")
                    .AddComponent<P12ReturnCrystal2D>();
            walletOnly.Configure(
                P12ReturnCrystalRules.SmallCrystalValue,
                anchor.transform,
                null,
                wallet);
            Assert.That(walletOnly.CollectForTests(), Is.True);
            Assert.That(
                wallet.Gold,
                Is.EqualTo(P12ReturnCrystalRules.SmallCrystalValue));
        }

        [Test]
        public void Converter_ConvertsGoldInRadiusAtDoubleValue()
        {
            GameObject anchor = Track("StartRoomAnchor");
            anchor.transform.position = new Vector3(-8f, 0f, 0f);
            P5RunState2D runState =
                Track("RunState").AddComponent<P5RunState2D>();
            runState.Configure(0);
            P12ReturnCrystalConverter2D converter =
                Track("Converter")
                    .AddComponent<P12ReturnCrystalConverter2D>();
            converter.Configure(null, anchor.transform, runState);

            GameObject near = Track("GoldNear");
            near.transform.position = Vector3.zero;
            GameObject far = Track("GoldFar");
            far.transform.position = new Vector3(5f, 0f, 0f);

            Assert.That(
                converter.RegisterGoldPickup(
                    near.transform,
                    P7EconomyRules.BigGoldValue),
                Is.True);
            Assert.That(
                converter.RegisterGoldPickup(
                    near.transform,
                    P7EconomyRules.BigGoldValue),
                Is.False,
                "Duplicate registration must be rejected.");
            Assert.That(
                converter.RegisterGoldPickup(null, 3),
                Is.False);
            Assert.That(
                converter.RegisterGoldPickup(far.transform, 0),
                Is.False);
            Assert.That(
                converter.RegisterGoldPickup(
                    far.transform,
                    P7EconomyRules.SmallGoldValue),
                Is.True);
            Assert.That(converter.RegisteredCount, Is.EqualTo(2));

            P12ReturnCrystal2D spawned = null;
            converter.CrystalSpawned += crystal => spawned = crystal;

            Assert.That(
                converter.TryConvertAt(Vector2.zero),
                Is.True);
            Assert.That(converter.ConvertedCount, Is.EqualTo(1));
            Assert.That(spawned, Is.Not.Null);
            Assert.That(
                spawned.Value,
                Is.EqualTo(
                    P12ReturnCrystalRules.StandardCrystalValue));
            Assert.That(
                spawned.DriftTarget,
                Is.EqualTo(anchor.transform));
            Assert.That(near.activeSelf, Is.False);
            Assert.That(
                far.activeSelf,
                Is.True,
                "Gold outside the conversion radius must survive.");

            Assert.That(
                converter.TryConvertAt(Vector2.zero),
                Is.False,
                "Already converted gold must not convert again.");
            Assert.That(
                converter.TryConvertAt(new Vector2(5f, 0f)),
                Is.True);
            Assert.That(converter.ConvertedCount, Is.EqualTo(2));
            Assert.That(
                spawned.Value,
                Is.EqualTo(P12ReturnCrystalRules.SmallCrystalValue));
        }

        [Test]
        public void Converter_EnforcesEightActiveCrystalPool()
        {
            GameObject anchor = Track("PoolAnchor");
            P5RunState2D runState =
                Track("PoolRunState").AddComponent<P5RunState2D>();
            runState.Configure(0);
            P12ReturnCrystalConverter2D converter =
                Track("PoolConverter")
                    .AddComponent<P12ReturnCrystalConverter2D>();
            converter.Configure(null, anchor.transform, runState);
            var spawned = new List<P12ReturnCrystal2D>();
            converter.CrystalSpawned +=
                crystal => spawned.Add(crystal);

            int total =
                P12ReturnCrystalConverter2D.MaxActiveCrystals + 1;
            for (int index = 0; index < total; index++)
            {
                GameObject gold = Track($"PoolGold_{index}");
                gold.transform.position = Vector3.zero;
                Assert.That(
                    converter.RegisterGoldPickup(
                        gold.transform,
                        P7EconomyRules.SmallGoldValue),
                    Is.True);
            }

            Assert.That(
                converter.TryConvertAt(Vector2.zero),
                Is.True);
            Assert.That(
                converter.ConvertedCount,
                Is.EqualTo(
                    P12ReturnCrystalConverter2D.MaxActiveCrystals));
            Assert.That(
                converter.ActiveCrystalCount,
                Is.EqualTo(
                    P12ReturnCrystalConverter2D.MaxActiveCrystals));
            Assert.That(converter.CanConvert, Is.False);
            Assert.That(
                converter.TryConvertAt(Vector2.zero),
                Is.False,
                "The pool cap must block the ninth conversion.");

            Assert.That(spawned[0].CollectForTests(), Is.True);
            Assert.That(converter.CanConvert, Is.True);
            Assert.That(
                converter.TryConvertAt(Vector2.zero),
                Is.True);
            Assert.That(converter.ConvertedCount, Is.EqualTo(total));
        }

        [Test]
        public void RecordData_TracksAttemptsProgressAndFailures()
        {
            P12ChallengeRecordData record =
                P12ChallengeRecordData.CreateEmpty();
            Assert.That(record.HasCompletion, Is.False);
            Assert.That(
                record.FastestCompletionSeconds,
                Is.EqualTo(
                    P12ChallengeRecordData.NoCompletionSeconds));
            Assert.That(record.AssistUseOnlyAnnotatesRecord, Is.True);

            record.RegisterAttempt();
            record.RegisterAttempt();
            Assert.That(record.AttemptCount, Is.EqualTo(2));
            Assert.That(
                record.RegisterProgress(
                    P12ChallengeSegment.SecondSea,
                    P12StageId.StarlessSea05),
                Is.True);
            Assert.That(
                record.RegisterProgress(
                    P12ChallengeSegment.FirstSea,
                    P12StageId.StarlessSea01),
                Is.False,
                "Best progress must be monotonic.");
            Assert.That(
                record.RegisterFailure(
                    P12ChallengeFailureCause.MaruCaught),
                Is.True);
            Assert.That(
                record.RegisterFailure(P12ChallengeFailureCause.Fall),
                Is.True);
            Assert.That(
                record.RegisterFailure(P12ChallengeFailureCause.None),
                Is.False);
            Assert.That(record.MaruFailureCount, Is.EqualTo(1));
            Assert.That(record.FallFailureCount, Is.EqualTo(1));
            Assert.That(record.HealthFailureCount, Is.Zero);

            record.RegisterCompletion(415.5f, true);
            Assert.That(record.CompletionCount, Is.EqualTo(1));
            Assert.That(
                record.AssistedCompletionCount,
                Is.EqualTo(1));
            Assert.That(record.UnassistedCompletionCount, Is.Zero);
            Assert.That(
                record.BestSegmentReached,
                Is.EqualTo(P12ChallengeSegment.DawnPassage));
            Assert.That(
                record.BestStageReached,
                Is.EqualTo(P12StageId.StarlessSea12));
            Assert.That(
                record.FastestCompletionSeconds,
                Is.EqualTo(415.5f));

            record.RegisterCompletion(300f, false);
            Assert.That(
                record.FastestCompletionSeconds,
                Is.EqualTo(300f));
            record.RegisterCompletion(400f, false);
            Assert.That(
                record.FastestCompletionSeconds,
                Is.EqualTo(300f),
                "A slower completion must not overwrite the record.");
            Assert.That(record.UnassistedCompletionCount, Is.EqualTo(2));
        }

        [Test]
        public void Store_RoundTripsRecordAndFallsBackOnCorruption()
        {
            CreateStorageRoot();
            Assert.That(
                P12PersistentStore.StorageRoot,
                Is.EqualTo(tempStorageRoot));
            Assert.That(
                P12PersistentStore.StoresOnlyRecordsAndOptions,
                Is.True);

            P12ChallengeRecordData record =
                P12ChallengeRecordData.CreateEmpty();
            record.RegisterAttempt();
            record.RegisterAttempt();
            record.RegisterFailure(
                P12ChallengeFailureCause.HealthDepleted);
            record.RegisterCompletion(512.25f, true);
            Assert.That(
                P12PersistentStore.SaveChallengeRecord(record),
                Is.True);
            Assert.That(
                File.Exists(P12PersistentStore.ChallengeRecordPath),
                Is.True);

            P12ChallengeRecordData loaded =
                P12PersistentStore.LoadChallengeRecord();
            Assert.That(loaded.AttemptCount, Is.EqualTo(2));
            Assert.That(loaded.HealthFailureCount, Is.EqualTo(1));
            Assert.That(loaded.CompletionCount, Is.EqualTo(1));
            Assert.That(loaded.AssistedCompletionCount, Is.EqualTo(1));
            Assert.That(
                loaded.FastestCompletionSeconds,
                Is.EqualTo(512.25f));
            Assert.That(
                loaded.BestSegmentReached,
                Is.EqualTo(P12ChallengeSegment.DawnPassage));
            Assert.That(
                loaded.BestStageReached,
                Is.EqualTo(P12StageId.StarlessSea12));
            Assert.That(
                loaded.SchemaVersion,
                Is.EqualTo(
                    P12ChallengeRecordData.CurrentSchemaVersion));

            File.WriteAllText(
                P12PersistentStore.ChallengeRecordPath,
                "corrupted{{not-json");
            P12ChallengeRecordData fallback =
                P12PersistentStore.LoadChallengeRecord();
            Assert.That(fallback, Is.Not.Null);
            Assert.That(fallback.AttemptCount, Is.Zero);
            Assert.That(fallback.HasCompletion, Is.False);
            Assert.That(
                fallback.FastestCompletionSeconds,
                Is.EqualTo(
                    P12ChallengeRecordData.NoCompletionSeconds));

            Assert.That(
                P12PersistentStore.DeleteChallengeRecord(),
                Is.True);
            Assert.That(
                P12PersistentStore.DeleteChallengeRecord(),
                Is.False);
            P12ChallengeRecordData missing =
                P12PersistentStore.LoadChallengeRecord();
            Assert.That(missing.AttemptCount, Is.Zero);
        }

        [Test]
        public void Store_SerializedFieldsAreRecordsAndOptionsOnly()
        {
            string[] recordFields =
                typeof(P12ChallengeRecordData)
                    .GetFields(
                        BindingFlags.Instance
                        | BindingFlags.NonPublic
                        | BindingFlags.Public)
                    .Select(field => field.Name)
                    .ToArray();
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "schemaVersion",
                    "attemptCount",
                    "bestSegmentReached",
                    "bestStageReached",
                    "completionCount",
                    "fastestCompletionSeconds",
                    "maruFailureCount",
                    "healthFailureCount",
                    "fallFailureCount",
                    "assistedCompletionCount"
                },
                recordFields);

            string[] accessibilityFields =
                typeof(P12AccessibilityData)
                    .GetFields(
                        BindingFlags.Instance
                        | BindingFlags.NonPublic
                        | BindingFlags.Public)
                    .Select(field => field.Name)
                    .ToArray();
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "maruDelayEnabled",
                    "postHitInvulnerabilityEnabled",
                    "alwaysShowExitMarker",
                    "grappleAimAssist",
                    "bossSpeedReduced"
                },
                accessibilityFields);
        }

        [Test]
        public void Accessibility_TogglesFiveOptionsWithFixedScales()
        {
            P12AccessibilityOptions2D options =
                Track("AssistOptions")
                    .AddComponent<P12AccessibilityOptions2D>();
            options.ResetForTests();
            Assert.That(options.AnyAssistEnabled, Is.False);
            Assert.That(options.EffectiveMaruDelaySeconds, Is.Zero);
            Assert.That(
                options.EffectivePostHitInvulnerabilityBonusSeconds,
                Is.Zero);
            Assert.That(options.ExitMarkerAlwaysVisible, Is.False);
            Assert.That(
                options.EffectiveGrappleAimAssistScale,
                Is.EqualTo(1f));
            Assert.That(
                options.EffectiveBossAttackSpeedScale,
                Is.EqualTo(1f));

            Assert.That(
                P12AccessibilityOptions2D.MaruDelaySeconds,
                Is.EqualTo(30f));
            Assert.That(
                P12AccessibilityOptions2D.MaruDelaySeconds,
                Is.EqualTo(
                    P8MaruTimelineProfile.TravellerAssistSeconds));
            Assert.That(
                P12AccessibilityOptions2D
                    .PostHitInvulnerabilityBonusSeconds,
                Is.EqualTo(0.5f));
            Assert.That(
                P12AccessibilityOptions2D.BossAttackSpeedScale,
                Is.EqualTo(0.85f));

            int changedEvents = 0;
            options.OptionsChanged += _ => changedEvents++;
            var allOptions = new[]
            {
                P12AssistOption.MaruDelay,
                P12AssistOption.PostHitInvulnerability,
                P12AssistOption.AlwaysShowExitMarker,
                P12AssistOption.GrappleAimAssist,
                P12AssistOption.BossSpeedReduced
            };
            foreach (P12AssistOption option in allOptions)
            {
                Assert.That(options.SetOption(option, true), Is.True);
                Assert.That(options.GetOption(option), Is.True);
                Assert.That(
                    options.SetOption(option, true),
                    Is.False,
                    "Unchanged values must not raise events.");
            }

            Assert.That(changedEvents, Is.EqualTo(5));
            Assert.That(options.AnyAssistEnabled, Is.True);
            Assert.That(
                options.EffectiveMaruDelaySeconds,
                Is.EqualTo(30f));
            Assert.That(
                options.EffectivePostHitInvulnerabilityBonusSeconds,
                Is.EqualTo(0.5f));
            Assert.That(options.ExitMarkerAlwaysVisible, Is.True);
            Assert.That(
                options.EffectiveGrappleAimAssistScale,
                Is.EqualTo(
                    P12AccessibilityOptions2D.GrappleAimAssistScale));
            Assert.That(
                options.EffectiveBossAttackSpeedScale,
                Is.EqualTo(0.85f));
            Assert.That(options.AssistNeverBlocksEndings, Is.True);
            Assert.That(
                options.AssistOnlyMarkedOnChallengeRecord,
                Is.True);
        }

        [Test]
        public void Accessibility_SavesAndLoadsThroughStore()
        {
            CreateStorageRoot();
            P12AccessibilityOptions2D options =
                Track("AssistSave")
                    .AddComponent<P12AccessibilityOptions2D>();
            options.ResetForTests();
            options.SetOption(P12AssistOption.MaruDelay, true);
            options.SetOption(P12AssistOption.BossSpeedReduced, true);
            Assert.That(options.SaveToStore(), Is.True);

            P12AccessibilityOptions2D reloaded =
                Track("AssistLoad")
                    .AddComponent<P12AccessibilityOptions2D>();
            reloaded.ResetForTests();
            reloaded.LoadFromStore();
            Assert.That(reloaded.MaruDelayEnabled, Is.True);
            Assert.That(reloaded.BossSpeedReduced, Is.True);
            Assert.That(
                reloaded.PostHitInvulnerabilityEnabled,
                Is.False);
            Assert.That(reloaded.AlwaysShowExitMarker, Is.False);
            Assert.That(reloaded.GrappleAimAssist, Is.False);
        }

        [Test]
        public void Telemetry_TwentyUniqueSamplesGateTheBand()
        {
            P12ChallengeTelemetry2D telemetry =
                Track("Telemetry")
                    .AddComponent<P12ChallengeTelemetry2D>();
            telemetry.ResetForTests();
            Assert.That(
                telemetry.MinimumSkilledSamples,
                Is.EqualTo(
                    P12ChallengeTelemetry2D
                        .DefaultMinimumSkilledSamples));
            Assert.That(telemetry.InstrumentationReady, Is.True);

            for (int index = 0; index < 19; index++)
            {
                Assert.That(
                    telemetry.TryRecordSkilledRun(
                        $"human-{index}",
                        index < 2,
                        540f + index,
                        index == 1,
                        index == 0),
                    Is.True);
            }

            Assert.That(telemetry.SampleCount, Is.EqualTo(19));
            Assert.That(
                telemetry.HasMinimumSkilledSamples,
                Is.False);
            Assert.That(
                telemetry.CompletionRateGateInBand,
                Is.False);
            Assert.That(telemetry.UnfairDeathGatePassed, Is.False);
            Assert.That(telemetry.HumanGatePending, Is.True);
            Assert.That(
                telemetry.TryRecordSkilledRun(
                    "human-0",
                    true,
                    500f,
                    false,
                    false),
                Is.False,
                "Duplicate session ids must be rejected.");
            Assert.That(
                telemetry.TryRecordSkilledRun(
                    " human-0 ",
                    true,
                    500f,
                    false,
                    false),
                Is.False,
                "Trimmed duplicates must be rejected.");
            Assert.That(
                telemetry.TryRecordSkilledRun(
                    "   ",
                    true,
                    500f,
                    false,
                    false),
                Is.False);
            Assert.That(telemetry.SampleCount, Is.EqualTo(19));

            Assert.That(
                telemetry.TryRecordSkilledRun(
                    "human-19",
                    false,
                    600f,
                    false,
                    false),
                Is.True);
            Assert.That(telemetry.SampleCount, Is.EqualTo(20));
            Assert.That(telemetry.CompletedRunCount, Is.EqualTo(2));
            Assert.That(telemetry.AssistedRunCount, Is.EqualTo(1));
            Assert.That(telemetry.CompletionRate, Is.EqualTo(0.1f));
            Assert.That(
                telemetry.UnfairDeathRate,
                Is.EqualTo(0.05f));
            Assert.That(telemetry.CompletionRateGateInBand, Is.True);
            Assert.That(telemetry.UnfairDeathGatePassed, Is.True);
            Assert.That(telemetry.HumanGatePending, Is.False);
            Assert.That(
                telemetry.AutomatedStructureCanSatisfyHumanGate,
                Is.False);
            Assert.That(
                telemetry.CanAutomatedStructureSatisfyHumanGate(true),
                Is.False);
        }

        [Test]
        public void Telemetry_BandBoundariesAreInclusiveAndUnfairStrict()
        {
            P12ChallengeTelemetry2D telemetry =
                Track("TelemetryBounds")
                    .AddComponent<P12ChallengeTelemetry2D>();

            telemetry.ResetForTests(20);
            RecordRuns(telemetry, "low", 20, 1, 0);
            Assert.That(
                telemetry.CompletionRate,
                Is.EqualTo(
                    P12ChallengeTelemetry2D
                        .RequiredCompletionRateMin));
            Assert.That(telemetry.CompletionRateGateInBand, Is.True);

            telemetry.ResetForTests(20);
            RecordRuns(telemetry, "high", 20, 3, 0);
            Assert.That(
                telemetry.CompletionRate,
                Is.EqualTo(
                    P12ChallengeTelemetry2D
                        .RequiredCompletionRateMax));
            Assert.That(telemetry.CompletionRateGateInBand, Is.True);

            telemetry.ResetForTests(20);
            RecordRuns(telemetry, "over", 20, 4, 0);
            Assert.That(telemetry.CompletionRateGateInBand, Is.False);
            Assert.That(telemetry.HumanGatePending, Is.True);

            telemetry.ResetForTests(20);
            RecordRuns(telemetry, "zero", 20, 0, 0);
            Assert.That(telemetry.CompletionRateGateInBand, Is.False);

            telemetry.ResetForTests(20);
            RecordRuns(telemetry, "unfair", 20, 2, 2);
            Assert.That(
                telemetry.UnfairDeathRate,
                Is.EqualTo(
                    P12ChallengeTelemetry2D.UnfairDeathRateMax));
            Assert.That(
                telemetry.UnfairDeathGatePassed,
                Is.False,
                "The unfair death gate is strictly below ten percent.");

            telemetry.ResetForTests(20);
            RecordRuns(telemetry, "fair", 20, 2, 1);
            Assert.That(telemetry.UnfairDeathGatePassed, Is.True);
            Assert.That(telemetry.HumanGatePending, Is.False);
        }

        [Test]
        public void Telemetry_SessionMarksValidateInput()
        {
            P12ChallengeTelemetry2D telemetry =
                Track("TelemetryMarks")
                    .AddComponent<P12ChallengeTelemetry2D>();
            telemetry.ResetForTests(5);
            Assert.That(
                telemetry.MinimumSkilledSamples,
                Is.EqualTo(5));

            telemetry.MarkChallengeEntered();
            telemetry.MarkChallengeEntered();
            Assert.That(
                telemetry.ChallengeEnteredCount,
                Is.EqualTo(2));
            Assert.That(
                telemetry.MarkStageCompleted(P12StageId.None),
                Is.False);
            Assert.That(
                telemetry.MarkStageCompleted(
                    P12StageId.StarlessSea01),
                Is.True);
            Assert.That(
                telemetry.CompletedStages,
                Is.EqualTo(new[] { P12StageId.StarlessSea01 }));
            Assert.That(telemetry.MarkFailure("  "), Is.False);
            Assert.That(telemetry.MarkFailure(" maru "), Is.True);
            Assert.That(
                telemetry.FailureCauses,
                Is.EqualTo(new[] { "maru" }));
        }

        [Test]
        public void PerformanceProbe_JudgesFrameBudgetAndCaps()
        {
            P12PerformanceProbe2D probe =
                Track("Probe").AddComponent<P12PerformanceProbe2D>();
            probe.ResetForTests();
            Assert.That(probe.SampleCount, Is.Zero);
            Assert.That(probe.AverageFrameMilliseconds, Is.Zero);
            Assert.That(probe.WorstFrameMilliseconds, Is.Zero);
            Assert.That(
                probe.BudgetSatisfied,
                Is.False,
                "No samples means no proven budget.");

            probe.RecordFrameForTests(10f);
            probe.RecordFrameForTests(20f);
            Assert.That(probe.SampleCount, Is.EqualTo(2));
            Assert.That(
                probe.AverageFrameMilliseconds,
                Is.EqualTo(15f).Within(0.0001f));
            Assert.That(
                probe.WorstFrameMilliseconds,
                Is.EqualTo(20f));
            Assert.That(probe.BudgetSatisfied, Is.True);

            probe.RecordFrameForTests(40f);
            Assert.That(probe.SampleCount, Is.EqualTo(3));
            Assert.That(
                probe.WorstFrameMilliseconds,
                Is.GreaterThan(
                    P12PerformanceProbe2D.HardCapMilliseconds));
            Assert.That(probe.BudgetSatisfied, Is.False);

            probe.RecordFrameForTests(-5f);
            Assert.That(
                probe.SampleCount,
                Is.EqualTo(3),
                "Negative frame samples must be ignored.");

            probe.MarkStageTransition();
            Assert.That(probe.SampleCount, Is.Zero);

            Assert.That(
                P12PerformanceProbe2D.MaxActiveEnvironments,
                Is.EqualTo(1));
            Assert.That(
                P12PerformanceProbe2D.MaxLiveReturnCrystals,
                Is.EqualTo(8));
            Assert.That(
                P12PerformanceProbe2D.MaxLiveReturnCrystals,
                Is.EqualTo(
                    P12ReturnCrystalConverter2D.MaxActiveCrystals));
        }

        private static void RecordRuns(
            P12ChallengeTelemetry2D telemetry,
            string prefix,
            int total,
            int completedCount,
            int unfairCount)
        {
            for (int index = 0; index < total; index++)
            {
                Assert.That(
                    telemetry.TryRecordSkilledRun(
                        $"{prefix}-{index}",
                        index < completedCount,
                        600f,
                        false,
                        index < unfairCount),
                    Is.True);
            }
        }

        private void CreateStorageRoot()
        {
            tempStorageRoot = Path.Combine(
                Path.GetTempPath(),
                "StarNightP12Store_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempStorageRoot);
            P12PersistentStore.SetStorageRootForTests(tempStorageRoot);
        }

        private GameObject Track(string name)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }
    }
}

#endif
