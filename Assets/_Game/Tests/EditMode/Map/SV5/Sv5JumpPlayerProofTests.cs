using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.SV5
{
    [Category("SV5")]
    [Category("SV5_20_JUMP_PLAYER")]
    [Category("SV5_20_EDITMODE")]
    public sealed class Sv5JumpPlayerProofTests
    {
        [Test]
        public void E01_ContractShapeAcceptsExactlyThirtyEightBoundCases()
        {
            Sv5JumpPlayerProof proof = PassingProof();
            Assert.That(proof.Diagnostics, Is.Empty);
            Assert.That(proof.Cases.Count, Is.EqualTo(38));
            Assert.That(proof.PlayerVerified, Is.True);
        }

        [Test]
        public void E02_PlayerBindingsAreExactSixReadOnlyBaseBlobs()
        {
            IReadOnlyList<Sv5JumpPlayerBindingFile> files = Sv5JumpPlayerVerification.CanonicalPlayerBindings();
            Assert.That(files.Count, Is.EqualTo(6));
            Assert.That(files.All(value => value.Role == "READ_ONLY" && value.Bytes > 0 &&
                value.GitBlobOid.Length == 40 && value.Sha256.Length == 64), Is.True);
        }

        [Test]
        public void E03_PlayerMeasurementsRemainObservedAndNotRetuned()
        {
            Sv5JumpPlayerMeasurements value = Sv5JumpPlayerVerification.CanonicalMeasurements();
            Assert.That(value.Source, Is.EqualTo("ACTUAL_BASE_COMMIT_PLAYER_COMPONENTS"));
            Assert.That(value.Retuned, Is.False);
            Assert.That(value.CapsuleWidth, Is.EqualTo(0.72f));
            Assert.That(value.JumpVelocity, Is.EqualTo(7.2f));
        }

        [Test]
        public void E04_FakePlayerIsRejected()
        {
            AssertDiagnostic(ReplaceCase(PassingCases(), 0, actualPlayer: false), Sv5JumpPlayerDiagnostic.FakePlayer);
        }

        [Test]
        public void E05_MissingDriverIsRejected()
        {
            AssertDiagnostic(ReplaceCase(PassingCases(), 0, actualDriver: false), Sv5JumpPlayerDiagnostic.MissingDriver);
        }

        [Test]
        public void E06_MissingRigidbodyIsRejected()
        {
            AssertDiagnostic(ReplaceCase(PassingCases(), 0, actualBody: false), Sv5JumpPlayerDiagnostic.MissingBody);
        }

        [Test]
        public void E07_MissingCapsuleColliderIsRejected()
        {
            AssertDiagnostic(ReplaceCase(PassingCases(), 0, actualCollider: false), Sv5JumpPlayerDiagnostic.MissingCollider);
        }

        [Test]
        public void E08_PostStartTeleportIsRejected()
        {
            AssertDiagnostic(ReplaceCase(PassingCases(), 0, teleports: 1), Sv5JumpPlayerDiagnostic.PostStartTeleport);
        }

        [Test]
        public void E09_LogicalTraceReuseIsRejected()
        {
            var cases = PassingCases();
            Sv5JumpPlayerCase original = cases[0];
            var trace = original.Trace.Select((value, index) => index == 0
                ? Sample(0, "START", actual: false, logical: true) : value).ToArray();
            cases[0] = Copy(original, trace: trace);
            AssertDiagnostic(cases, Sv5JumpPlayerDiagnostic.LogicalTraceReuse);
        }

        [Test]
        public void E10_WrongFirstCatchIsRejected()
        {
            var cases = PassingCases();
            int index = cases.FindIndex(value => value.CaseKind == Sv5JumpPlayerCaseKind.Recovery);
            cases[index] = Copy(cases[index], firstCatch: false);
            AssertDiagnostic(cases, Sv5JumpPlayerDiagnostic.WrongFirstCatch);
        }

        [Test]
        public void E11_LaterCheckpointIsRejected()
        {
            var cases = PassingCases();
            int index = cases.FindIndex(value => value.CaseKind == Sv5JumpPlayerCaseKind.Recovery && value.MainLinkOrder == 0);
            cases[index] = Copy(cases[index], checkpoint: 1);
            AssertDiagnostic(cases, Sv5JumpPlayerDiagnostic.LaterCheckpoint);
        }

        [Test]
        public void E12_GrabEntryOrExitOmissionIsRejected()
        {
            var cases = PassingCases();
            int index = cases.FindIndex(value => value.CaseKind == Sv5JumpPlayerCaseKind.MainLink && value.UsedGrab);
            cases[index] = Copy(cases[index], trace: new[] { Sample(0, "START"), Sample(1, "PASS_TARGET") });
            AssertDiagnostic(cases, Sv5JumpPlayerDiagnostic.MissingGrabEvents);
        }

        [Test]
        public void E13_TopOnlyPlatformGrabIsRejected()
        {
            AssertDiagnostic(ReplaceCase(PassingCases(), 0, grabbedPlatform: true), Sv5JumpPlayerDiagnostic.PlatformGrab);
        }

        [Test]
        public void E14_ItemUseIsRejected()
        {
            AssertDiagnostic(ReplaceCase(PassingCases(), 0, usedItem: true), Sv5JumpPlayerDiagnostic.ItemUse);
        }

        [Test]
        public void E15_ReverseCompletionClaimIsRejected()
        {
            AssertDiagnostic(ReplaceCase(PassingCases(), 0, reverse: true), Sv5JumpPlayerDiagnostic.ReverseRequired);
        }

        [Test]
        public void E16_JumpPlusGrabRiseAboveTwoIsRejected()
        {
            AssertDiagnostic(ReplaceCase(PassingCases(), 0, rise: 2.01f), Sv5JumpPlayerDiagnostic.RiseCap);
        }

        [Test]
        public void E17_FalseReadinessIsRejected()
        {
            Sv5JumpPlayerProof proof = Sv5JumpPlayerVerification.Validate(
                Sv5JumpPlayerVerification.InputRecoveryDigest, PassingCases(), true, recoveryReady: false);
            Assert.That(proof.Diagnostics.Any(value => value.StartsWith(Sv5JumpPlayerDiagnostic.FalseReadiness,
                StringComparison.Ordinal)), Is.True);
            Assert.That(proof.PlayerVerified, Is.False);
        }

        [Test]
        public void E18_WrongRecoveryDigestIsRejected()
        {
            Sv5JumpPlayerProof proof = Sv5JumpPlayerVerification.Validate("wrong", PassingCases(), true);
            Assert.That(proof.Diagnostics, Does.Contain(Sv5JumpPlayerDiagnostic.RecoveryIdentity));
        }

        [Test]
        public void E19_ExportsContainEveryRequiredCanonicalArtifact()
        {
            IReadOnlyDictionary<string, string> artifacts = Sv5JumpPlayerExport.BuildArtifacts(PassingProof(),
                Sv5JumpPlayerVerification.CanonicalPlayerBindings(), Sv5JumpPlayerVerification.CanonicalMeasurements());
            CollectionAssert.AreEquivalent(new[] { "jump_player.json", "player_bindings.json",
                "player_measurements.json", "player_cases.csv", "player_trace.csv",
                "player_validation.json", "player_geometry_fix03.json", "player_geometry_patch.csv",
                "player_composed_occupancy.csv", "player_effective_links.csv", "player_scheduler_audit.json",
                "preview/jump_player.svg" }, artifacts.Keys);
        }

        [Test]
        public void E20_ExportsAreDeterministicAndDeclarePhysicalBoundaries()
        {
            var a = Sv5JumpPlayerExport.BuildArtifacts(PassingProof(),
                Sv5JumpPlayerVerification.CanonicalPlayerBindings(), Sv5JumpPlayerVerification.CanonicalMeasurements());
            var b = Sv5JumpPlayerExport.BuildArtifacts(PassingProof(),
                Sv5JumpPlayerVerification.CanonicalPlayerBindings(), Sv5JumpPlayerVerification.CanonicalMeasurements());
            CollectionAssert.AreEqual(a, b);
            Assert.That(a["jump_player.json"], Does.Contain("\"post_start_teleports\": 0"));
            Assert.That(a["jump_player.json"], Does.Contain("\"PlayerVerified\": true"));
        }

        [Test]
        public void E21_ExportRoundTripWritesUtf8Artifacts()
        {
            string directory = Path.Combine(Path.GetTempPath(), "SV5_20_" + Guid.NewGuid().ToString("N"));
            try
            {
                Sv5JumpPlayerExport.Write(directory, PassingProof(),
                    Sv5JumpPlayerVerification.CanonicalPlayerBindings(), Sv5JumpPlayerVerification.CanonicalMeasurements());
                Assert.That(File.Exists(Path.Combine(directory, "player_cases.csv")), Is.True);
                Assert.That(File.ReadAllText(Path.Combine(directory, "preview", "jump_player.svg")),
                    Does.Contain("SV5_20_JUMP_PLAYER"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void E22_InvalidProofCannotBeExported()
        {
            Sv5JumpPlayerProof invalid = Sv5JumpPlayerVerification.Validate(
                Sv5JumpPlayerVerification.InputRecoveryDigest, Array.Empty<Sv5JumpPlayerCase>(), true);
            Assert.Throws<InvalidOperationException>(() => Sv5JumpPlayerExport.BuildArtifacts(invalid,
                Sv5JumpPlayerVerification.CanonicalPlayerBindings(), Sv5JumpPlayerVerification.CanonicalMeasurements()));
        }

        [Test]
        public void E23_FullRoutesAreExactlyForwardOnlyOnePerRecipe()
        {
            Sv5JumpPlayerCase[] routes = PassingProof().Cases.Where(value =>
                value.CaseKind == Sv5JumpPlayerCaseKind.FullRoute).ToArray();
            Assert.That(routes.Select(value => value.RecipeId), Is.EquivalentTo(new[]
            {
                Sv5JumpRecipeCatalog.R0RecipeId, Sv5JumpRecipeCatalog.MirrorRecipeId,
            }));
            Assert.That(routes.All(value => !value.ReverseRequired && !value.UsedItem), Is.True);
        }

        [Test]
        public void E24_Fix03ComposesExactlyEighteenOperationsAndBorrowsRecoverySupports()
        {
            Sv5JumpPlayerFix03Geometry value = Sv5JumpPlayerVerification.CreateFix03Geometry();
            Assert.That(value.Diagnostics, Is.Empty);
            Assert.That(value.ChangedCellCount, Is.EqualTo(18));
            Assert.That(value.RemovedCellCount, Is.EqualTo(12));
            Assert.That(value.ConvertedCellCount, Is.EqualTo(6));
            Assert.That(value.AddedCellCount, Is.Zero);
            Assert.That(value.EffectiveLinks.Count(link => link.GeometryPatchId ==
                Sv5JumpPlayerVerification.Fix03PatchId), Is.EqualTo(2));
            AssertBorrowed(value, Sv5JumpRecipeCatalog.R0RecipeId, 16, 4);
            AssertBorrowed(value, Sv5JumpRecipeCatalog.R0RecipeId, 17, 4);
            AssertBorrowed(value, Sv5JumpRecipeCatalog.MirrorRecipeId, 6, 4);
            AssertBorrowed(value, Sv5JumpRecipeCatalog.MirrorRecipeId, 7, 4);
            AssertConverted(value, Sv5JumpRecipeCatalog.R0RecipeId, 3, 9);
            AssertConverted(value, Sv5JumpRecipeCatalog.R0RecipeId, 4, 9);
            AssertConverted(value, Sv5JumpRecipeCatalog.MirrorRecipeId, 19, 9);
            AssertConverted(value, Sv5JumpRecipeCatalog.MirrorRecipeId, 20, 9);
            AssertConverted(value, Sv5JumpRecipeCatalog.R0RecipeId, 14, 6,
                Sv5JumpPlayerVerification.Fix03Js04PassThroughOwnerId);
            AssertConverted(value, Sv5JumpRecipeCatalog.MirrorRecipeId, 9, 6,
                Sv5JumpPlayerVerification.Fix03Js04PassThroughOwnerId);
            float threshold = Sv5JumpPlayerVerification.SchedulerJumpThreshold(5.5f, 1f / 60f, 0.01f, 0.01f);
            Assert.That(Sv5JumpPlayerVerification.ShouldSubmitScheduledJump(true, threshold, threshold), Is.True);
            Assert.That(Sv5JumpPlayerVerification.ShouldSubmitScheduledJump(false, 0f, threshold), Is.False);
        }

        [Test]
        public void E25_Fix03RejectsANineteenthOperation()
        {
            var operations = Sv5JumpPlayerVerification.CanonicalFix03Operations().ToList();
            operations.Add(new Sv5JumpPlayerPatchOperation(Sv5JumpRecipeCatalog.R0RecipeId,
                "ADD", 16, 4, "AIR", "TOP_ONLY"));
            AssertFixDiagnostic(Sv5JumpPlayerVerification.ComposeFix03(operations),
                Sv5JumpPlayerVerification.FixOperationCount);
        }

        [Test]
        public void E26_Fix03RejectsAMissingMirrorOperation()
        {
            var operations = Sv5JumpPlayerVerification.CanonicalFix03Operations().Take(17).ToArray();
            Sv5JumpPlayerFix03Geometry value = Sv5JumpPlayerVerification.ComposeFix03(operations);
            AssertFixDiagnostic(value, Sv5JumpPlayerVerification.FixOperationCount);
            AssertFixDiagnostic(value, Sv5JumpPlayerVerification.FixMirror);
        }

        [Test]
        public void E27_Fix03RejectsInvalidConversion()
        {
            var operations = Sv5JumpPlayerVerification.CanonicalFix03Operations().ToList();
            operations[6] = new Sv5JumpPlayerPatchOperation(Sv5JumpRecipeCatalog.R0RecipeId,
                "CONVERT", 3, 9, "SOLID", "TOP_ONLY", "JS08_SOLID", "WRONG_OWNER",
                "PRESERVE_LINK07_08_SUPPORT");
            AssertFixDiagnostic(Sv5JumpPlayerVerification.ComposeFix03(operations),
                Sv5JumpPlayerVerification.FixOperationCount);
            AssertFixDiagnostic(Sv5JumpPlayerVerification.ComposeFix03(operations),
                Sv5JumpPlayerVerification.FixConversion);
        }

        [Test]
        public void E28_Fix03RejectsBorrowedSupportRelabeling()
        {
            AssertFixDiagnostic(Sv5JumpPlayerVerification.ComposeFix03(
                Sv5JumpPlayerVerification.CanonicalFix03Operations(), borrowedSupportChanged: true),
                Sv5JumpPlayerVerification.FixBorrowedSupport);
        }

        [Test]
        public void E29_Fix03RejectsChangedPredecessorInputs()
        {
            AssertFixDiagnostic(Sv5JumpPlayerVerification.ComposeFix03(
                Sv5JumpPlayerVerification.CanonicalFix03Operations(), predecessorChanged: true),
                Sv5JumpPlayerVerification.FixPredecessor);
        }

        [Test]
        public void E30_Fix03RejectsPlayerRetuning()
        {
            AssertFixDiagnostic(Sv5JumpPlayerVerification.ComposeFix03(
                Sv5JumpPlayerVerification.CanonicalFix03Operations(), playerRetuned: true),
                Sv5JumpPlayerVerification.FixPlayerRetune);
        }

        [Test]
        public void E31_TopOnlyOverheadChangesOnlyLink06Frontiers()
        {
            Sv5JumpPlayerFix03Geometry geometry = Sv5JumpPlayerVerification.CreateFix03Geometry();
            var changed = new List<string>();
            foreach (Sv5JumpPlayerEffectiveLink link in geometry.EffectiveLinks)
            {
                float legacy = SchedulerFrontier(geometry, link, topOnlyPassesOverhead: false);
                float corrected = SchedulerFrontier(geometry, link, topOnlyPassesOverhead: true);
                if (!legacy.Equals(corrected))
                {
                    changed.Add(link.RecipeId + "|" + link.SourceLinkId + "|" +
                        legacy.ToString("0.00") + "|" + corrected.ToString("0.00"));
                }
            }

            Assert.That(changed, Is.EquivalentTo(new[]
            {
                Sv5JumpRecipeCatalog.R0RecipeId + "|JS_LINK_06|5.00|4.00",
                Sv5JumpRecipeCatalog.MirrorRecipeId + "|JS_LINK_06|19.00|20.00",
            }));
        }

        [Test]
        public void E32_Js04LocalTopologyAndGrabApproachClassificationAreExact()
        {
            Sv5JumpPlayerFix03Geometry geometry = Sv5JumpPlayerVerification.CreateFix03Geometry();
            CollectionAssert.IsEmpty(geometry.Diagnostics);

            AssertJs04Topology(geometry, Sv5JumpRecipeCatalog.R0RecipeId,
                new Sv5JumpPoint(15, 5), new Sv5JumpPoint(15, 6), new Sv5JumpPoint(14, 6));
            AssertJs04Topology(geometry, Sv5JumpRecipeCatalog.MirrorRecipeId,
                new Sv5JumpPoint(8, 5), new Sv5JumpPoint(8, 6), new Sv5JumpPoint(9, 6));

            Assert.That(Sv5JumpPlayerVerification.IsSupportCollision("TOP_ONLY"), Is.True);
            Assert.That(Sv5JumpPlayerVerification.IsSolidBlockerCollision("TOP_ONLY"), Is.False);
            Assert.That(Sv5JumpPlayerVerification.IsGrabSurfaceCollision("TOP_ONLY"), Is.False);

            foreach (string recipeId in new[]
                     {
                         Sv5JumpRecipeCatalog.R0RecipeId,
                         Sv5JumpRecipeCatalog.MirrorRecipeId,
                     })
            {
                Sv5JumpPlayerEffectiveLink link02 = geometry.EffectiveLinks.Single(value =>
                    value.RecipeId == recipeId && value.Order == 2);
                Sv5JumpPlayerEffectiveLink link03 = geometry.EffectiveLinks.Single(value =>
                    value.RecipeId == recipeId && value.Order == 3);
                Sv5JumpPlayerEffectiveLink link04 = geometry.EffectiveLinks.Single(value =>
                    value.RecipeId == recipeId && value.Order == 4);
                Assert.That(Sv5JumpPlayerVerification.UsesTerminalSolidGrabApproach(link02, geometry), Is.False);
                Assert.That(Sv5JumpPlayerVerification.UsesTerminalSolidGrabApproach(link03, geometry), Is.True);
                Assert.That(Sv5JumpPlayerVerification.UsesTerminalSolidGrabApproach(link04, geometry), Is.False);
            }
        }

        private static Sv5JumpPlayerProof PassingProof()
        {
            return Sv5JumpPlayerVerification.Validate(Sv5JumpPlayerVerification.InputRecoveryDigest,
                PassingCases(), true, deterministicRepeatCount: 2, deterministicTerminalDeltaSteps: 0);
        }

        private static List<Sv5JumpPlayerCase> PassingCases()
        {
            var result = new List<Sv5JumpPlayerCase>();
            Sv5JumpClearancePlan clearance = Sv5JumpClearance.CreateCanonicalLocalProof();
            Sv5JumpRecoveryPlan recovery = Sv5JumpRecovery.CreateCanonicalLocalProof();
            foreach (Sv5JumpClearanceLinkResult link in clearance.Links)
            {
                bool grab = link.Trace.Mode == Sv5JumpMode.JumpGrab;
                Sv5JumpPlayerTraceSample[] trace = grab
                    ? new[] { Sample(0, "START"), Sample(1, "GRAB_ENTER", grabbing: true),
                        Sample(2, "GRAB_HOLD", grabbing: true), Sample(3, "GRAB_SPACE_EXIT", velocityY: 6f),
                        Sample(4, "GRAB_EXIT", velocityY: 5f), Sample(5, "PASS_TARGET", grounded: true) }
                    : new[] { Sample(0, "START"), Sample(1, "PASS_TARGET", grounded: true) };
                result.Add(NewCase("MAIN_" + Short(link.Trace.RecipeId) + "_" + link.Trace.Order.ToString("00"),
                    link.Trace.RecipeId, Sv5JumpPlayerCaseKind.MainLink, link.Trace.Order,
                    link.Trace.SourceLinkId, "MAIN_TARGET_" + link.Trace.Order.ToString("00"), -1,
                    grab, false, true, trace));
            }
            foreach (string recipe in new[] { Sv5JumpRecipeCatalog.R0RecipeId, Sv5JumpRecipeCatalog.MirrorRecipeId })
                result.Add(NewCase("FULL_" + Short(recipe), recipe, Sv5JumpPlayerCaseKind.FullRoute, -1,
                    "JS_LINK_00", "JS_LINK_08_TARGET", -1, false, true, true,
                    new[] { Sample(0, "START"), Sample(1, "PASS_TARGET", grounded: true) }));
            foreach (Sv5JumpRecoveryRoute route in recovery.Routes)
                result.Add(NewCase("RECOVERY_" + Short(route.RecipeId) + "_" + route.FailedLinkOrder.ToString("00"),
                    route.RecipeId, Sv5JumpPlayerCaseKind.Recovery, route.FailedLinkOrder,
                    route.RouteId, "CHECKPOINT_" + route.CheckpointLinkOrder.ToString("00"), route.CheckpointLinkOrder,
                    false, true, true, new[] { Sample(0, "START"), Sample(1, "RECOVERY_CATCH", grounded: true),
                        Sample(2, "REJOIN", grounded: true), Sample(3, "PASS_TARGET", grounded: true) }));
            return result;
        }

        private static Sv5JumpPlayerCase NewCase(string id, string recipe, Sv5JumpPlayerCaseKind kind, int order,
            string source, string target, int checkpoint, bool grab, bool oneWay, bool firstCatch,
            IEnumerable<Sv5JumpPlayerTraceSample> trace)
        {
            return new Sv5JumpPlayerCase(id, recipe, kind, order, source, target, checkpoint,
                true, true, true, true, true, grab, oneWay, false, false, 0, grab ? 2f : 1f,
                firstCatch, false, trace);
        }

        private static Sv5JumpPlayerTraceSample Sample(int step, string eventName, bool grounded = false,
            bool grabbing = false, bool actual = true, bool logical = false, float velocityY = 0f)
        {
            return new Sv5JumpPlayerTraceSample(step, 0f, false, false, false, false,
                step, 1f, 0f, velocityY, grounded, grabbing, false, false,
                grabbing ? "GRAB_SOLID" : grounded ? "SOLID" : "NONE", eventName, actual, logical);
        }

        private static List<Sv5JumpPlayerCase> ReplaceCase(List<Sv5JumpPlayerCase> cases, int index,
            bool? actualPlayer = null, bool? actualDriver = null, bool? actualBody = null,
            bool? actualCollider = null, int? teleports = null, bool? grabbedPlatform = null,
            bool? usedItem = null, bool? reverse = null, float? rise = null)
        {
            cases[index] = Copy(cases[index], actualPlayer, actualDriver, actualBody, actualCollider,
                teleports, grabbedPlatform, usedItem, reverse, rise);
            return cases;
        }

        private static Sv5JumpPlayerCase Copy(Sv5JumpPlayerCase value,
            bool? actualPlayer = null, bool? actualDriver = null, bool? actualBody = null,
            bool? actualCollider = null, int? teleports = null, bool? grabbedPlatform = null,
            bool? usedItem = null, bool? reverse = null, float? rise = null, bool? firstCatch = null,
            int? checkpoint = null, IEnumerable<Sv5JumpPlayerTraceSample> trace = null)
        {
            return new Sv5JumpPlayerCase(value.CaseId, value.RecipeId, value.CaseKind, value.MainLinkOrder,
                value.SourceId, value.TargetId, checkpoint ?? value.CheckpointLinkOrder, value.Passed,
                actualPlayer ?? value.ActualPlayer, actualDriver ?? value.ActualDriver,
                actualBody ?? value.ActualRigidbody2D, actualCollider ?? value.ActualCollider2D,
                value.UsedGrab, value.UsedOneWay, usedItem ?? value.UsedItem,
                reverse ?? value.ReverseRequired, teleports ?? value.TeleportsAfterStart,
                rise ?? value.MeasuredRiseCells, firstCatch ?? value.FirstCatchMatched,
                grabbedPlatform ?? value.GrabbedPlatform, trace ?? value.Trace);
        }

        private static void AssertDiagnostic(IEnumerable<Sv5JumpPlayerCase> cases, string expected)
        {
            Sv5JumpPlayerProof proof = Sv5JumpPlayerVerification.Validate(
                Sv5JumpPlayerVerification.InputRecoveryDigest, cases, true);
            Assert.That(proof.Diagnostics.Any(value => value.StartsWith(expected, StringComparison.Ordinal)), Is.True,
                string.Join("\n", proof.Diagnostics));
            Assert.That(proof.PlayerVerified, Is.False);
        }

        private static void AssertFixDiagnostic(Sv5JumpPlayerFix03Geometry value, string expected)
        {
            Assert.That(value.Diagnostics.Any(item => item.StartsWith(expected,
                StringComparison.Ordinal)), Is.True, string.Join("\n", value.Diagnostics));
            Assert.That(value.Ready, Is.False);
        }

        private static void AssertBorrowed(
            Sv5JumpPlayerFix03Geometry geometry,
            string recipeId,
            int x,
            int y)
        {
            Sv5JumpPlayerComposedCell value = geometry.ComposedCells.Single(cell =>
                cell.RecipeId == recipeId && cell.Point.Equals(new Sv5JumpPoint(x, y)));
            Assert.That(value.Collision, Is.EqualTo("TOP_ONLY"));
            Assert.That(value.SourceLayer, Is.EqualTo("SV5_19_RECOVERY"));
            Assert.That(value.OwnerId, Is.EqualTo("RG_GRAB_CATCH"));
        }

        private static void AssertConverted(
            Sv5JumpPlayerFix03Geometry geometry,
            string recipeId,
            int x,
            int y,
            string expectedOwnerId = Sv5JumpPlayerVerification.Fix03Js08PassThroughOwnerId)
        {
            Sv5JumpPlayerComposedCell value = geometry.ComposedCells.Single(cell =>
                cell.RecipeId == recipeId && cell.Point.Equals(new Sv5JumpPoint(x, y)));
            Assert.That(value.Collision, Is.EqualTo("TOP_ONLY"));
            Assert.That(value.SourceLayer, Is.EqualTo("SV5_20_FIX03"));
            Assert.That(value.OwnerId, Is.EqualTo(expectedOwnerId));
        }

        private static void AssertJs04Topology(
            Sv5JumpPlayerFix03Geometry geometry,
            string recipeId,
            Sv5JumpPoint removedLower,
            Sv5JumpPoint grabSupport,
            Sv5JumpPoint takeoffSupport)
        {
            Assert.That(geometry.ComposedCells.Any(cell => cell.RecipeId == recipeId &&
                cell.Point.Equals(removedLower)), Is.False);
            Sv5JumpPlayerComposedCell solid = geometry.ComposedCells.Single(cell =>
                cell.RecipeId == recipeId && cell.Point.Equals(grabSupport));
            Assert.That(solid.Collision, Is.EqualTo("SOLID"));
            Assert.That(solid.OwnerId, Is.EqualTo("JS04_SOLID"));
            Sv5JumpPlayerComposedCell topOnly = geometry.ComposedCells.Single(cell =>
                cell.RecipeId == recipeId && cell.Point.Equals(takeoffSupport));
            Assert.That(topOnly.Collision, Is.EqualTo("TOP_ONLY"));
            Assert.That(topOnly.OwnerId,
                Is.EqualTo(Sv5JumpPlayerVerification.Fix03Js04PassThroughOwnerId));

            Sv5JumpPlayerPatchOperation[] js04Changes = geometry.Operations.Where(operation =>
                operation.RecipeId == recipeId &&
                (operation.Point.Equals(removedLower) || operation.Point.Equals(takeoffSupport))).ToArray();
            Assert.That(js04Changes.Length, Is.EqualTo(2));
            Assert.That(js04Changes.Single(operation => operation.Point.Equals(removedLower)).Operation,
                Is.EqualTo("REMOVE"));
            Assert.That(js04Changes.Single(operation => operation.Point.Equals(takeoffSupport)).Operation,
                Is.EqualTo("CONVERT"));
        }

        private static float SchedulerFrontier(
            Sv5JumpPlayerFix03Geometry geometry,
            Sv5JumpPlayerEffectiveLink link,
            bool topOnlyPassesOverhead)
        {
            Dictionary<string, string> collision = geometry.ComposedCells.Where(value =>
                    value.RecipeId == link.RecipeId)
                .ToDictionary(value => CellKey(value.Point.X, value.Point.Y), value => value.Collision,
                    StringComparer.Ordinal);
            int supportY = link.Takeoff.Y - 1;
            float playerX = link.Takeoff.X + 0.5f;
            const float halfWidth = 0.36f;
            const float skin = 0.01f;
            var overlapping = new List<int>();
            for (int x = 0; x < Sv5JumpPlayerVerification.LocalWidth; x++)
            {
                if (IsSchedulerWalkable(collision, x, supportY, topOnlyPassesOverhead) &&
                    x + 1f >= playerX - halfWidth - skin &&
                    x <= playerX + halfWidth + skin)
                {
                    overlapping.Add(x);
                }
            }

            if (overlapping.Count == 0)
            {
                return -1f;
            }

            int sign = link.Direction == Sv5JumpDirection.LeftToRight ? 1 : -1;
            int supportCell = sign > 0 ? overlapping.Max() : overlapping.Min();
            while (IsSchedulerWalkable(collision, supportCell + sign, supportY,
                       topOnlyPassesOverhead))
            {
                supportCell += sign;
            }

            return sign > 0 ? supportCell + 1f : supportCell;
        }

        private static bool IsSchedulerWalkable(
            IReadOnlyDictionary<string, string> collision,
            int x,
            int supportY,
            bool topOnlyPassesOverhead)
        {
            if (x < 0 || x >= Sv5JumpPlayerVerification.LocalWidth)
            {
                return false;
            }

            string support = CollisionAt(collision, x, supportY);
            string body = CollisionAt(collision, x, supportY + 1);
            string head = CollisionAt(collision, x, supportY + 2);
            return topOnlyPassesOverhead
                ? Sv5JumpPlayerVerification.IsSchedulerWalkableSupport(support, body, head)
                : !string.IsNullOrEmpty(support) && string.IsNullOrEmpty(body) &&
                    string.IsNullOrEmpty(head);
        }

        private static string CollisionAt(
            IReadOnlyDictionary<string, string> collision,
            int x,
            int y)
        {
            string value;
            return collision.TryGetValue(CellKey(x, y), out value) ? value : string.Empty;
        }

        private static string CellKey(int x, int y)
        {
            return x + "," + y;
        }

        private static string Short(string recipe)
        {
            return recipe == Sv5JumpRecipeCatalog.R0RecipeId ? "R0" : "MX";
        }
    }
}
