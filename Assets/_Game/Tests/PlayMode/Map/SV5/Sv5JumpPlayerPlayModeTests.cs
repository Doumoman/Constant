#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests.PlayMode.SV5
{
    [Category("SV5")]
    [Category("SV5_20_JUMP_PLAYER")]
    [Category("SV5_20_PLAYMODE")]
    [Parallelizable(ParallelScope.None)]
    public sealed class Sv5JumpPlayerPlayModeTests
    {
        private const float HalfHeightWithSkin = 0.46f;
        private const int SolidMask = 1;
        private static readonly WaitForFixedUpdate Fixed = new WaitForFixedUpdate();

        [UnityTest] public IEnumerator D00_Main_R0_00() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId, 0); }
        [UnityTest] public IEnumerator D01_Main_R0_01() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId, 1); }
        [UnityTest] public IEnumerator D02_Main_R0_02() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId, 2); }
        [UnityTest] public IEnumerator D03_Main_R0_03() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId, 3); }
        [UnityTest] public IEnumerator D04_Main_R0_04() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId, 4); }
        [UnityTest] public IEnumerator D05_Main_R0_05() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId, 5); }
        [UnityTest] public IEnumerator D06_Main_R0_06() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId, 6); }
        [UnityTest] public IEnumerator D07_Main_R0_07() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId, 7); }
        [UnityTest] public IEnumerator D08_Main_R0_08() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId, 8); }
        [UnityTest] public IEnumerator D09_Main_MX_00() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId, 0); }
        [UnityTest] public IEnumerator D10_Main_MX_01() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId, 1); }
        [UnityTest] public IEnumerator D11_Main_MX_02() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId, 2); }
        [UnityTest] public IEnumerator D12_Main_MX_03() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId, 3); }
        [UnityTest] public IEnumerator D13_Main_MX_04() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId, 4); }
        [UnityTest] public IEnumerator D14_Main_MX_05() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId, 5); }
        [UnityTest] public IEnumerator D15_Main_MX_06() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId, 6); }
        [UnityTest] public IEnumerator D16_Main_MX_07() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId, 7); }
        [UnityTest] public IEnumerator D17_Main_MX_08() { return RunMainLinkDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId, 8); }

        [UnityTest]
        [Category("SV5_20_FULL_WATCHDOG_FOCUSED")]
        public IEnumerator F00_Full_R0_WatchdogScope()
        {
            return RunFullRouteDiagnostic(Sv5JumpRecipeCatalog.R0RecipeId);
        }

        [UnityTest]
        [Category("SV5_20_FULL_WATCHDOG_FOCUSED")]
        public IEnumerator F01_Full_MX_WatchdogScope()
        {
            return RunFullRouteDiagnostic(Sv5JumpRecipeCatalog.MirrorRecipeId);
        }

        [UnityTest]
        public IEnumerator P01_ActualCharacterLivePlayerPassesAllPhysicalCasesAndExportsTheProof()
        {
            Sv5JumpClearancePlan clearance = Sv5JumpClearance.CreateCanonicalLocalProof();
            Sv5JumpRecoveryPlan recovery = Sv5JumpRecovery.CreateCanonicalLocalProof();
            Sv5JumpRecipeCatalogModel catalog = Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
            Sv5JumpPlayerFix03Geometry geometry = Sv5JumpPlayerVerification.CreateFix03Geometry();
            CollectionAssert.IsEmpty(geometry.Diagnostics);
            var cases = new List<Sv5JumpPlayerCase>();
            var fullTerminalSteps = new Dictionary<string, int>(StringComparer.Ordinal);
            int grabEntries = 0;
            int grabExits = 0;
            int oneWayLandings = 0;

            foreach (Sv5JumpClearanceLinkResult link in clearance.Links)
            {
                Sv5JumpPlayerEffectiveLink effective = geometry.EffectiveLinks.Single(value =>
                    value.RecipeId == link.Trace.RecipeId && value.Order == link.Trace.Order);
                PhysicalRun run = CreateRun(catalog.Recipe(link.Trace.RecipeId), geometry,
                    StartPoint(effective.Takeoff));
                var record = new CaseRecord("MAIN_" + ShortRecipe(link.Trace.RecipeId) + "_" +
                    link.Trace.Order.ToString("00"), link.Trace.RecipeId,
                    Sv5JumpPlayerCaseKind.MainLink, link.Trace.Order, link.Trace.SourceLinkId,
                    "MAIN_TARGET_" + link.Trace.Order.ToString("00"), -1);
                BeginCase(run, record);
                yield return ExecuteJumpLink(run, record, effective);
                CompleteLink(run, record);
                yield return FinishCase(run, record);
                cases.Add(record.ToCase());
                grabEntries += record.GrabEntries;
                grabExits += record.GrabExits;
                oneWayLandings += record.OneWayLandings;
                Assert.IsTrue(record.Passed, record.CaseId + " failed at " + run.Player.Position +
                    "\n" + TraceTail(record));
                yield return DestroyRun(run);
            }

            foreach (string recipeId in new[]
                     {
                         Sv5JumpRecipeCatalog.R0RecipeId,
                         Sv5JumpRecipeCatalog.MirrorRecipeId,
                     })
            {
                Sv5JumpPlayerEffectiveLink[] links = geometry.EffectiveLinks
                    .Where(value => value.RecipeId == recipeId)
                    .OrderBy(value => value.Order).ToArray();
                PhysicalRun run = CreateRun(catalog.Recipe(recipeId), geometry,
                    StartPoint(links[0].Takeoff));
                var record = new CaseRecord("FULL_" + ShortRecipe(recipeId), recipeId,
                    Sv5JumpPlayerCaseKind.FullRoute, 8, "JS_LINK_00", "JS_LINK_08_TARGET", -1);
                record.ConfigureRouteBudget(links.Length);
                BeginCase(run, record);
                foreach (Sv5JumpPlayerEffectiveLink link in links)
                {
                    yield return ExecuteJumpLink(run, record, link);
                    if (record.Passed && link.HasWalkToNext)
                        yield return MoveGrounded(run, record, link.WalkToNextTakeoff.X + 0.5f,
                            link.WalkToNextTakeoff.Y, 90, "FIX03_WALK_TO_LINK03");
                    CompleteLink(run, record);
                    Assert.IsTrue(record.Passed, FullRouteFailureDetail(run, record));
                }

                yield return FinishCase(run, record);
                cases.Add(record.ToCase());
                fullTerminalSteps[recipeId] = record.Trace.Count;
                grabEntries += record.GrabEntries;
                grabExits += record.GrabExits;
                oneWayLandings += record.OneWayLandings;
                yield return DestroyRun(run);
            }

            foreach (Sv5JumpRecoveryRoute route in recovery.Routes)
            {
                Sv5JumpMissProbe probe = recovery.Probes.Single(value =>
                    value.RecipeId == route.RecipeId && value.FailedLinkOrder == route.FailedLinkOrder);
                PhysicalRun run = CreateRun(catalog.Recipe(route.RecipeId), geometry,
                    new Vector2(probe.Origin.X + 0.5f, probe.Origin.Y + 0.5f));
                var record = new CaseRecord("RECOVERY_" + ShortRecipe(route.RecipeId) + "_" +
                    route.FailedLinkOrder.ToString("00"), route.RecipeId,
                    Sv5JumpPlayerCaseKind.Recovery, route.FailedLinkOrder,
                    probe.SourceLinkId, route.RouteId, route.CheckpointLinkOrder);
                BeginCase(run, record);
                yield return ExecuteRecovery(run, record, probe, route);
                yield return FinishCase(run, record);
                cases.Add(record.ToCase());
                oneWayLandings += record.OneWayLandings;
                Assert.IsTrue(record.Passed, record.CaseId + " failed at " + run.Player.Position +
                    "\n" + TraceTail(record));
                Assert.IsTrue(record.FirstCatchMatched, record.CaseId + " caught a later surface");
                yield return DestroyRun(run);
            }

            int terminalDelta = 0;
            foreach (string recipeId in fullTerminalSteps.Keys.OrderBy(value => value, StringComparer.Ordinal))
            {
                Sv5JumpPlayerEffectiveLink[] links = geometry.EffectiveLinks
                    .Where(value => value.RecipeId == recipeId)
                    .OrderBy(value => value.Order).ToArray();
                PhysicalRun repeat = CreateRun(catalog.Recipe(recipeId), geometry,
                    StartPoint(links[0].Takeoff));
                var repeated = new CaseRecord("DETERMINISM_" + ShortRecipe(recipeId), recipeId,
                    Sv5JumpPlayerCaseKind.FullRoute, 8, "JS_LINK_00", "JS_LINK_08_TARGET", -1);
                repeated.ConfigureRouteBudget(links.Length);
                BeginCase(repeat, repeated);
                foreach (Sv5JumpPlayerEffectiveLink link in links)
                {
                    yield return ExecuteJumpLink(repeat, repeated, link);
                    if (repeated.Passed && link.HasWalkToNext)
                        yield return MoveGrounded(repeat, repeated, link.WalkToNextTakeoff.X + 0.5f,
                            link.WalkToNextTakeoff.Y, 90, "FIX03_WALK_TO_LINK03");
                    CompleteLink(repeat, repeated);
                }
                yield return FinishCase(repeat, repeated);
                Assert.IsTrue(repeated.Passed, repeated.CaseId + " repeat failed");
                terminalDelta = Mathf.Max(terminalDelta,
                    Mathf.Abs(repeated.Trace.Count - fullTerminalSteps[recipeId]));
                yield return DestroyRun(repeat);
            }

            Assert.AreEqual(38, cases.Count);
            Assert.AreEqual(18, cases.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.MainLink));
            Assert.AreEqual(2, cases.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.FullRoute));
            Assert.AreEqual(18, cases.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.Recovery));
            Assert.GreaterOrEqual(grabEntries, 4, "Both main and continuous routes must physically enter Grab.");
            Assert.AreEqual(grabEntries, grabExits, "Every actual Grab must have an actual exit.");
            Assert.Greater(oneWayLandings, 0, "The physical suite must land on real marked top-only colliders.");
            Assert.LessOrEqual(terminalDelta, 2, "Repeated full routes must terminate within two fixed steps.");

            Sv5JumpPlayerProof proof = Sv5JumpPlayerVerification.Validate(
                recovery.Digest, cases, playerVerifiedClaim: true,
                deterministicRepeatCount: 2, deterministicTerminalDeltaSteps: terminalDelta);
            CollectionAssert.IsEmpty(proof.Diagnostics);
            Assert.IsTrue(proof.PlayerVerified);
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                "MapDesign", "MCP", "GENERATED", "SV5_20_JUMP_PLAYER"));
            Sv5JumpPlayerExport.Write(output, proof,
                Sv5JumpPlayerVerification.CanonicalPlayerBindings(),
                Sv5JumpPlayerVerification.CanonicalMeasurements());
            Debug.Log("SV5_20 physical PASS: cases=38 grab=" + grabEntries + "/" + grabExits +
                " oneWayLandings=" + oneWayLandings + " deterministicDelta=" + terminalDelta);
        }

        private static IEnumerator RunMainLinkDiagnostic(string recipeId, int order)
        {
            Sv5JumpClearanceLinkResult source = Sv5JumpClearance.CreateCanonicalLocalProof().Links.Single(value =>
                value.Trace.RecipeId == recipeId && value.Trace.Order == order);
            Sv5JumpPlayerFix03Geometry geometry = Sv5JumpPlayerVerification.CreateFix03Geometry();
            CollectionAssert.IsEmpty(geometry.Diagnostics);
            Sv5JumpPlayerEffectiveLink link = geometry.EffectiveLinks.Single(value =>
                value.RecipeId == recipeId && value.Order == order);
            PhysicalRun run = CreateRun(Sv5JumpRecipeCatalog.CreateCanonicalCatalog().Recipe(recipeId), geometry,
                StartPoint(link.Takeoff));
            var record = new CaseRecord("MAIN_" + ShortRecipe(recipeId) + "_" + order.ToString("00"),
                recipeId, Sv5JumpPlayerCaseKind.MainLink, order, source.Trace.SourceLinkId,
                "MAIN_TARGET_" + order.ToString("00"), -1);
            BeginCase(run, record);
            yield return ExecuteJumpLink(run, record, link);
            CompleteLink(run, record);
            if (record.Passed)
                yield return FinishCase(run, record);
            string failure = FailureDetail(run, record, link);
            yield return DestroyRun(run);
            Assert.IsTrue(record.Passed, failure);
        }

        private static IEnumerator RunFullRouteDiagnostic(string recipeId)
        {
            Sv5JumpPlayerFix03Geometry geometry = Sv5JumpPlayerVerification.CreateFix03Geometry();
            CollectionAssert.IsEmpty(geometry.Diagnostics);
            Sv5JumpPlayerEffectiveLink[] links = geometry.EffectiveLinks
                .Where(value => value.RecipeId == recipeId)
                .OrderBy(value => value.Order).ToArray();
            PhysicalRun run = CreateRun(Sv5JumpRecipeCatalog.CreateCanonicalCatalog().Recipe(recipeId), geometry,
                StartPoint(links[0].Takeoff));
            var record = new CaseRecord("FULL_" + ShortRecipe(recipeId), recipeId,
                Sv5JumpPlayerCaseKind.FullRoute, 8, "JS_LINK_00", "JS_LINK_08_TARGET", -1);
            record.ConfigureRouteBudget(links.Length);
            BeginCase(run, record);
            foreach (Sv5JumpPlayerEffectiveLink link in links)
            {
                yield return ExecuteJumpLink(run, record, link);
                if (record.Passed && link.HasWalkToNext)
                    yield return MoveGrounded(run, record, link.WalkToNextTakeoff.X + 0.5f,
                        link.WalkToNextTakeoff.Y, 90, "FIX03_WALK_TO_LINK03");
                CompleteLink(run, record);
                if (!record.Passed)
                    break;
            }

            if (record.Passed)
                yield return FinishCase(run, record);
            string failure = FullRouteFailureDetail(run, record);
            yield return DestroyRun(run);
            Assert.IsTrue(record.Passed, failure);
        }

        private static IEnumerator ExecuteJumpLink(
            PhysicalRun run,
            CaseRecord record,
            Sv5JumpPlayerEffectiveLink link)
        {
            if (!record.Passed)
                yield break;

            BeginLink(run, record, link);

            while (!run.Player.IsGrounded && WithinDiagnosticBounds(run, record))
                yield return Step(run, record, 0f, false, false, false, false,
                    "GROUND_SETTLE_" + link.Order.ToString("00"));
            if (!run.Player.IsGrounded || !WithinDiagnosticBounds(run, record))
            {
                record.Passed = false;
                yield break;
            }

            float direction = Direction(link);
            bool useGrabApproach = Sv5JumpPlayerVerification.UsesTerminalSolidGrabApproach(
                link, run.Geometry);
            bool submittedJump = false;
            if (useGrabApproach)
            {
                float supportMinimumX;
                float supportMaximumX;
                if (run.GrabCollider == null ||
                    !run.TryGetTakeoffSupportBounds(link.Takeoff,
                        out supportMinimumX, out supportMaximumX))
                {
                    record.Passed = false;
                    yield break;
                }

                Bounds targetBounds = run.GrabCollider.bounds;
                float targetNearFaceX = direction > 0f ? targetBounds.min.x : targetBounds.max.x;
                try
                {
                    record.GrabApproach = Sv5JumpPlayerVerification.CalculateGrabApproachSchedule(
                        link, targetNearFaceX, supportMinimumX, supportMaximumX,
                        run.Player.CapsuleBounds.extents.x, Physics2D.defaultContactOffset,
                        run.Player.CollisionSkin, run.Player.GrabProbeDistance,
                        run.Player.JumpVelocity, run.Player.RiseGravity,
                        run.Player.MaxAirSpeed, run.Player.GroundAcceleration,
                        Time.fixedDeltaTime);
                }
                catch (Exception exception)
                {
                    record.FailureReason = exception.Message;
                    record.Passed = false;
                    yield break;
                }

                yield return MoveGroundedAndStop(run, record,
                    record.GrabApproach.StagingBodyX, link.Takeoff.Y, 120,
                    "GRAB_APPROACH_STAGE_" + link.Order.ToString("00"));
                if (!run.Player.IsGrounded ||
                    Mathf.Abs(run.Player.Position.x - record.GrabApproach.StagingBodyX) > 0.10f ||
                    Mathf.Abs(run.Player.Velocity.x) > 0.16f)
                {
                    record.FailureReason = "GRAB_APPROACH_STAGING_DID_NOT_SETTLE";
                    record.Passed = false;
                    yield break;
                }

                while (run.Player.IsGrounded && WithinDiagnosticBounds(run, record))
                {
                    float forwardSpeed = Mathf.Max(0f, direction * run.Player.Velocity.x);
                    float distanceToLaunch = direction *
                        (record.GrabApproach.LaunchBodyX - run.Player.Position.x);
                    float distanceToProbe = direction *
                        (record.GrabApproach.ProbeEntryBodyX - run.Player.Position.x);
                    float speedForPrediction = Mathf.Max(forwardSpeed,
                        run.Player.GroundAcceleration * Time.fixedDeltaTime);
                    float predictedTravelTime = distanceToProbe / speedForPrediction;
                    float predictedVelocityY = run.Player.JumpVelocity -
                        run.Player.RiseGravity * predictedTravelTime;
                    float nextStepTravel = speedForPrediction * Time.fixedDeltaTime +
                        run.Player.CollisionSkin;
                    bool launchLineReached = distanceToLaunch <= nextStepTravel;
                    bool jump = launchLineReached && predictedVelocityY <= 0.0001f;
                    if (launchLineReached && !jump)
                    {
                        record.FailureReason = "GRAB_APPROACH_PROJECTED_ASCENDING_FACE_CONTACT";
                        record.Passed = false;
                        break;
                    }

                    yield return Step(run, record, direction, false, false, jump, jump,
                        jump ? "GRAB_APPROACH_JUMP" : "GRAB_APPROACH_RUNUP",
                        record.GrabApproach.LaunchBodyX, distanceToLaunch, nextStepTravel,
                        schedulerPolicyId: Sv5JumpPlayerVerification.GrabApproachSchedulerPolicyId);
                    if (jump)
                    {
                        submittedJump = true;
                        record.JumpStep = record.Trace.Count - 1;
                        record.LaunchPosition = run.Player.Position;
                        record.LaunchVelocity = run.Player.Velocity;
                        break;
                    }
                }
            }
            else
            {
                while (WithinDiagnosticBounds(run, record))
                {
                    float walkableFrontierX;
                    float frontierDistance;
                    float jumpThreshold;
                    if (!run.Player.IsGrounded ||
                        !run.TryGetSchedulerMetrics(direction, out walkableFrontierX,
                            out frontierDistance, out jumpThreshold))
                        break;
                    bool jump = Sv5JumpPlayerVerification.ShouldSubmitScheduledJump(
                        true, frontierDistance, jumpThreshold);
                    yield return Step(run, record, direction, false, false, jump, jump,
                        jump ? "JUMP_" + link.Order.ToString("00") :
                            "EDGE_RUNUP_" + link.Order.ToString("00"),
                        walkableFrontierX, frontierDistance, jumpThreshold);
                    if (jump)
                    {
                        submittedJump = true;
                        record.JumpStep = record.Trace.Count - 1;
                        record.LaunchPosition = run.Player.Position;
                        record.LaunchVelocity = run.Player.Velocity;
                        break;
                    }
                }
            }
            if (!submittedJump)
            {
                record.Passed = false;
                yield break;
            }

            bool leftGround = !run.Player.IsGrounded;
            bool grabbed = false;
            bool exited = false;
            int consecutiveGrabObservations = 0;
            float targetX = link.Landing.X + 0.5f;
            float expectedY = link.Landing.Y + HalfHeightWithSkin;
            while (WithinDiagnosticBounds(run, record))
            {
                leftGround |= !run.Player.IsGrounded;
                if (useGrabApproach && !grabbed && !run.Player.IsGrabbing &&
                    run.IsInsideGrabProbeRange(direction, out float faceGap) &&
                    record.TargetFaceStep < 0)
                {
                    record.TargetFaceStep = record.Trace.Count - 1;
                    record.TargetFacePosition = run.Player.Position;
                    record.TargetFaceVelocity = run.Player.Velocity;
                    record.TargetFaceCapsuleBounds = run.Player.CapsuleBounds;
                    record.FirstColliderName = run.GrabCollider.name;
                    if (run.Player.Velocity.y > run.Player.GrabMaxUpwardVelocity)
                    {
                        record.FailureReason = "GRAB_APPROACH_ENTERED_PROBE_WHILE_ASCENDING gap=" +
                            faceGap.ToString("0.000");
                        MarkCurrent(run, record, "GRAB_APPROACH_RISING_CONTACT");
                        record.Passed = false;
                        break;
                    }
                }
                if (run.Player.IsGrabbing && !grabbed)
                {
                    grabbed = true;
                    record.GrabEntryVelocityY = record.Trace.Count > 1
                        ? record.Trace[record.Trace.Count - 2].VelocityY
                        : run.Player.Velocity.y;
                    record.FirstColliderName = run.GrabCollider != null
                        ? run.GrabCollider.name : string.Empty;
                    consecutiveGrabObservations = 1;
                    record.GrabEntries++;
                    MarkCurrent(run, record, "GRAB_ENTER");
                    yield return Step(run, record, 0f, false, false, false, false,
                        "GRAB_HOLD", jumpReleased: true);
                    if (run.Player.IsGrabbing)
                        consecutiveGrabObservations++;
                    if (consecutiveGrabObservations < 2)
                    {
                        record.Passed = false;
                        yield break;
                    }

                    yield return Step(run, record, direction, false, false, true, true,
                        "GRAB_SPACE_EXIT_PRESS");
                    for (int observation = 0; observation < 2; observation++)
                    {
                        if (!run.Player.IsGrabbing && run.Player.Velocity.y > 0f)
                        {
                            exited = true;
                            record.GrabExits++;
                            MarkCurrent(run, record, "GRAB_SPACE_EXIT");
                            break;
                        }
                        if (observation == 0)
                            yield return Step(run, record, direction, false, false, false, true,
                                "GRAB_SPACE_EXIT_OBSERVE");
                    }
                    if (!exited)
                    {
                        MarkCurrent(run, record, Sv5JumpPlayerVerification.GrabExitNoVerticalImpulse);
                        record.Passed = false;
                        yield break;
                    }

                    yield return Step(run, record, AirControlTowardLanding(run, link),
                        false, false, false, true, "GRAB_EXIT");
                    continue;
                }

                if (leftGround && run.Player.IsGrounded &&
                    Mathf.Abs(run.Player.Position.y - expectedY) < 0.16f &&
                    Mathf.Abs(run.Player.Position.x - targetX) < 0.86f)
                {
                    Collider2D landingSupport = run.FindLandingSupportCollider();
                    record.LandingSupportName = landingSupport != null
                        ? landingSupport.name : string.Empty;
                    if (useGrabApproach && landingSupport != run.GrabCollider)
                    {
                        record.FailureReason = "GRAB_APPROACH_LANDED_ON_DIFFERENT_COLLIDER";
                        record.Passed = false;
                        break;
                    }
                    record.OneWayLandings += run.ContactKind() == "TOP_ONLY" ? 1 : 0;
                    MarkCurrent(run, record, "LINK_TARGET_" + link.Order.ToString("00"));
                    break;
                }

                yield return Step(run, record, AirControlTowardLanding(run, link),
                    false, false, false, true,
                    "AIR_" + link.Order.ToString("00"));
            }

            bool grabExpected = link.Mode == Sv5JumpMode.JumpGrab;
            record.UsedGrab |= grabbed;
            record.UsedOneWay |= run.SupportKindAt(link.Takeoff) == "TOP_ONLY" ||
                run.SupportKindAt(link.Landing) == "TOP_ONLY";
            record.MeasuredRiseCells = Mathf.Max(record.MeasuredRiseCells,
                Mathf.Max(0f, link.Landing.Y - link.Takeoff.Y));
            bool landed = run.Player.IsGrounded &&
                Mathf.Abs(run.Player.Position.y - expectedY) < 0.16f &&
                Mathf.Abs(run.Player.Position.x - targetX) < 0.86f;
            if (useGrabApproach)
                landed &= run.FindLandingSupportCollider() == run.GrabCollider;
            record.Passed &= leftGround && landed && grabbed == grabExpected && (!grabbed || exited);
            record.GrabbedPlatform |= grabbed && run.ContactKind() == "TOP_ONLY";
        }

        private static float AirControlTowardLanding(PhysicalRun run, Sv5JumpPlayerEffectiveLink link)
        {
            float targetMinimum = link.Landing.X + run.Player.CapsuleHalfWidth;
            float targetMaximum = link.Landing.X + 1f - run.Player.CapsuleHalfWidth;
            if (run.Player.Position.x < targetMinimum)
                return 1f;
            if (run.Player.Position.x > targetMaximum)
                return -1f;
            return Mathf.Abs(run.Player.Velocity.x) > run.Player.CollisionSkin
                ? -Mathf.Sign(run.Player.Velocity.x) : 0f;
        }

        private static bool WithinDiagnosticBounds(PhysicalRun run, CaseRecord record)
        {
            if (run.Player.Position.y < Sv5JumpPlayerVerification.MinimumDiagnosticBodyY)
            {
                Fail(record, "MINIMUM_DIAGNOSTIC_BODY_Y");
                return false;
            }

            bool linkBudgetExhausted = record.Kind == Sv5JumpPlayerCaseKind.FullRoute
                ? record.LinkOpen &&
                    record.LinkStep >= Sv5JumpPlayerVerification.MaximumDiagnosticFixedSteps
                : record.Trace.Count >= Sv5JumpPlayerVerification.MaximumDiagnosticFixedSteps;
            if (linkBudgetExhausted)
            {
                Fail(record, "LINK_WATCHDOG_EXHAUSTED");
                return false;
            }

            int fullRouteLimit = record.RouteLinkCount *
                Sv5JumpPlayerVerification.MaximumDiagnosticFixedSteps;
            if (record.Kind == Sv5JumpPlayerCaseKind.FullRoute && record.CaseStep >= fullRouteLimit)
            {
                Fail(record, "FULL_ROUTE_WATCHDOG_EXHAUSTED");
                return false;
            }

            return true;
        }

        private static void BeginLink(
            PhysicalRun run,
            CaseRecord record,
            Sv5JumpPlayerEffectiveLink link)
        {
            record.CurrentLinkId = link.SourceLinkId;
            record.CurrentPhase = "LINK_ENTER";
            record.CurrentLinkEntryCaseStep = record.CaseStep;
            record.CurrentLinkReachedTerminal = false;
            record.LinkStep = 0;
            record.LinkOpen = true;
            record.WatchdogTrace.Add(WatchdogTraceSample.Capture(run, record, false));
            Assert.AreEqual(0, record.LinkStep,
                record.CaseId + " " + link.SourceLinkId + " must enter with linkStep=0.");
        }

        private static void CompleteLink(PhysicalRun run, CaseRecord record)
        {
            if (!record.LinkOpen)
                return;
            if (record.Passed && !record.CurrentLinkReachedTerminal)
                Fail(record, "LINK_TERMINAL_NOT_REACHED");
            else if (!record.Passed && string.IsNullOrEmpty(record.FailureReason))
                record.FailureReason = "PHYSICS_OR_TERMINAL_CONDITION_FAILED";
            record.CurrentPhase = "LINK_EXIT";
            record.WatchdogTrace.Add(WatchdogTraceSample.Capture(
                run, record, record.CurrentLinkReachedTerminal));
            record.LinkSummaries.Add(new LinkWatchdogSummary(
                record.CurrentLinkId,
                record.CurrentLinkEntryCaseStep,
                record.CaseStep,
                record.LinkStep,
                record.CurrentLinkReachedTerminal,
                run.Player.Position,
                run.Player.Velocity,
                record.FailureReason));
            record.LinkOpen = false;
        }

        private static void Fail(CaseRecord record, string reason)
        {
            if (string.IsNullOrEmpty(record.FailureReason))
                record.FailureReason = reason;
            record.Passed = false;
        }

        private static IEnumerator ExecuteRecovery(
            PhysicalRun run,
            CaseRecord record,
            Sv5JumpMissProbe probe,
            Sv5JumpRecoveryRoute route)
        {
            for (int index = 0; index < 180 && !run.Player.IsGrounded; index++)
                yield return Step(run, record, 0f, false, false, false, false, "MISS_FALL");

            float catchX = probe.LandingBody.X + 0.5f;
            float catchY = probe.LandingBody.Y + HalfHeightWithSkin;
            record.FirstCatchMatched = run.Player.IsGrounded &&
                Mathf.Abs(run.Player.Position.x - catchX) < 0.48f &&
                Mathf.Abs(run.Player.Position.y - catchY) < 0.16f;
            record.OneWayLandings += run.ContactKind() == "TOP_ONLY" ? 1 : 0;
            MarkCurrent(run, record, "RECOVERY_CATCH");

            if (route.RouteKind == Sv5JumpRecoveryRouteKind.AlreadyAtCheckpoint)
            {
                yield return Step(run, record, 0f, false, false, false, false, "REJOIN");
            }
            else
            {
                foreach (Sv5JumpRecoveryLink link in route.Links.OrderBy(value => value.LinkOrder))
                {
                    if (link.Mode == Sv5JumpRecoveryMode.Walk)
                    {
                        yield return MoveGrounded(run, record, link.Target.X + 0.5f,
                            link.Target.Y, 140, "RECOVERY_WALK");
                    }
                    else if (link.Mode == Sv5JumpRecoveryMode.Drop)
                    {
                        float targetX = link.Target.X + 0.5f;
                        float targetY = link.Target.Y + HalfHeightWithSkin;
                        bool leftSource = false;
                        for (int index = 0; index < 180; index++)
                        {
                            leftSource |= !run.Player.IsGrounded;
                            if (leftSource && run.Player.IsGrounded &&
                                Mathf.Abs(run.Player.Position.x - targetX) < 0.62f &&
                                Mathf.Abs(run.Player.Position.y - targetY) < 0.16f)
                                break;
                            float dx = targetX - run.Player.Position.x;
                            float horizontal = Mathf.Abs(dx) < 0.08f
                                ? -Mathf.Sign(run.Player.Velocity.x) : Mathf.Sign(dx);
                            yield return Step(run, record, horizontal, false, false, false, false,
                                "RECOVERY_DROP");
                        }
                    }
                    else
                    {
                        record.Passed = false;
                    }
                }
                MarkCurrent(run, record, "REJOIN");
            }

            float endX = route.End.X + 0.5f;
            float endY = route.End.Y + HalfHeightWithSkin;
            record.Passed &= record.FirstCatchMatched && run.Player.IsGrounded &&
                Mathf.Abs(run.Player.Position.x - endX) < 0.66f &&
                Mathf.Abs(run.Player.Position.y - endY) < 0.16f;
            record.UsedOneWay = true;
        }

        private static IEnumerator MoveGrounded(
            PhysicalRun run,
            CaseRecord record,
            float targetX,
            int feetY,
            int limit,
            string phase)
        {
            float expectedY = feetY + HalfHeightWithSkin;
            for (int index = 0; index < limit &&
                 (!record.LinkOpen || record.Passed && WithinDiagnosticBounds(run, record)); index++)
            {
                float dx = targetX - run.Player.Position.x;
                if (run.Player.IsGrounded && Mathf.Abs(dx) < 0.07f &&
                    Mathf.Abs(run.Player.Position.y - expectedY) < 0.16f)
                    yield break;
                float horizontal = Mathf.Abs(dx) < 0.05f ? 0f : Mathf.Sign(dx);
                if (Mathf.Abs(dx) < 0.28f && Mathf.Abs(run.Player.Velocity.x) > 0.12f)
                    horizontal = -Mathf.Sign(run.Player.Velocity.x);
                yield return Step(run, record, horizontal, false, false, false, false, phase);
            }
        }

        private static IEnumerator MoveGroundedAndStop(
            PhysicalRun run,
            CaseRecord record,
            float targetX,
            int feetY,
            int limit,
            string phase)
        {
            float expectedY = feetY + HalfHeightWithSkin;
            for (int index = 0; index < limit &&
                 (!record.LinkOpen || record.Passed && WithinDiagnosticBounds(run, record)); index++)
            {
                float dx = targetX - run.Player.Position.x;
                if (run.Player.IsGrounded && Mathf.Abs(dx) < 0.07f &&
                    Mathf.Abs(run.Player.Position.y - expectedY) < 0.16f &&
                    Mathf.Abs(run.Player.Velocity.x) < 0.08f)
                    yield break;

                float projectedStopX = run.Player.Position.x;
                if (Mathf.Abs(run.Player.Velocity.x) > 0.0001f)
                {
                    float stoppingDistance = run.Player.Velocity.x * run.Player.Velocity.x /
                        (2f * run.Player.GroundDeceleration);
                    projectedStopX += Mathf.Sign(run.Player.Velocity.x) * stoppingDistance;
                }

                float horizontal = 0f;
                if (projectedStopX < targetX - 0.025f)
                    horizontal = 1f;
                else if (projectedStopX > targetX + 0.025f)
                    horizontal = -1f;
                yield return Step(run, record, horizontal, false, false, false, false, phase);
            }
        }

        private static IEnumerator FinishCase(PhysicalRun run, CaseRecord record)
        {
            yield return Step(run, record, 0f, false, false, false, false, "PASS_TARGET");
            if (!record.Passed)
                record.Trace[record.Trace.Count - 1] = run.Player.Sample(record.Trace.Count - 1,
                    0f, false, false, false, false, run.ContactKind(), "PASS_TARGET");
        }

        private static IEnumerator Step(
            PhysicalRun run,
            CaseRecord record,
            float horizontal,
            bool up,
            bool down,
            bool jumpPressed,
            bool jumpHeld,
            string eventName,
            float walkableFrontierX = -1f,
            float frontierDistance = -1f,
            float jumpThreshold = -1f,
            bool jumpReleased = false,
            string schedulerPolicyId = "")
        {
            if (record.LinkOpen && !record.Passed)
                yield break;
            bool groundedBeforeStep = run.Player.IsGrounded;
            run.Player.Feed(horizontal, up, down, false, jumpPressed, jumpReleased, jumpHeld);
            yield return Fixed;
            record.CaseStep++;
            if (record.LinkOpen)
                record.LinkStep++;
            record.CurrentPhase = eventName;
            record.Trace.Add(run.Player.Sample(record.Trace.Count, horizontal, up, down,
                jumpPressed || jumpHeld, false, run.ContactKind(), eventName,
                groundedBeforeStep, walkableFrontierX, frontierDistance, jumpThreshold,
                !string.IsNullOrEmpty(schedulerPolicyId) ? schedulerPolicyId :
                    frontierDistance >= 0f ? Sv5JumpPlayerVerification.SchedulerPolicyId : string.Empty));
            record.WatchdogTrace.Add(WatchdogTraceSample.Capture(
                run, record, record.CurrentLinkReachedTerminal));
        }

        private static void BeginCase(PhysicalRun run, CaseRecord record)
        {
            record.Trace.Add(run.Player.Sample(0, 0f, false, false, false, false,
                run.ContactKind(), "START"));
            record.CurrentPhase = "START";
            record.WatchdogTrace.Add(WatchdogTraceSample.Capture(run, record, false));
        }

        private static void MarkCurrent(PhysicalRun run, CaseRecord record, string eventName)
        {
            int index = record.Trace.Count - 1;
            Sv5JumpPlayerTraceSample prior = record.Trace[index];
            record.Trace[index] = run.Player.Sample(index, prior.InputX, prior.InputUp,
                prior.InputDown, prior.InputJump, prior.InputShift, run.ContactKind(), eventName,
                prior.GroundedBeforeStep, prior.WalkableFrontierX, prior.FrontierDistance,
                prior.JumpThreshold,
                prior.SchedulerPolicyId);
            record.CurrentPhase = eventName;
            if (eventName.StartsWith("LINK_TARGET_", StringComparison.Ordinal))
                record.CurrentLinkReachedTerminal = true;
            if (record.WatchdogTrace.Count > 0)
            {
                WatchdogTraceSample current = record.WatchdogTrace[record.WatchdogTrace.Count - 1];
                current.Phase = eventName;
                current.Terminal = record.CurrentLinkReachedTerminal;
            }
        }

        private static PhysicalRun CreateRun(
            Sv5JumpRecipeVariant recipe,
            Sv5JumpPlayerFix03Geometry geometry,
            Vector2 start)
        {
            var run = new PhysicalRun(recipe, geometry);
            run.Root = new GameObject("SV5_20_" + recipe.RecipeId);
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (Sv5JumpPlayerComposedCell cell in geometry.ComposedCells.Where(value =>
                         value.RecipeId == recipe.RecipeId))
            {
                AddCell(run, cell.Point, cell.Collision, occupied);
            }

            Sv5JumpRecipeGrab grab = recipe.GrabEdges.Single();
            string grabKey = Key(grab.Edge.Contact.X, grab.Edge.Contact.Y);
            GameObject grabCell = run.Cells[grabKey];
            run.GrabCollider = grabCell.GetComponent<Collider2D>();
            Type surfaceType = LiveType("StarNight.Character.Live.Movement.CharacterLiveGrabSurface");
            Component surface = grabCell.AddComponent(surfaceType);
            Type kindType = surfaceType.GetNestedType("SurfaceKind", BindingFlags.Public);
            surfaceType.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(surface, new[] { Enum.Parse(kindType, "StaticSafe"), (object)false });

            Physics2D.SyncTransforms();
            GameObject prefab = LoadPlayerPrefab();
            GameObject host = new GameObject("ActualCharacterLivePlayerHost");
            host.transform.SetParent(run.Root.transform, false);
            host.SetActive(false);
            GameObject playerObject = UnityEngine.Object.Instantiate(prefab, host.transform);
            playerObject.transform.position = new Vector3(start.x, start.y, 0f);
            run.Player = new ReflectedPlayer(playerObject);
            run.Player.Configure(SolidMask);
            host.SetActive(true);
            run.Player.SetPreStartPosition(start);
            run.Player.ResetMotion();
            run.Player.DisableLiveInputSource();
            Physics2D.SyncTransforms();
            return run;
        }

        private static void AddCell(
            PhysicalRun run,
            Sv5JumpPoint point,
            string collision,
            ISet<string> occupied)
        {
            string key = Key(point.X, point.Y);
            if (!occupied.Add(key))
                return;
            var cell = new GameObject("Cell_" + point.X + "_" + point.Y + "_" + collision);
            cell.transform.SetParent(run.Root.transform, false);
            cell.transform.position = new Vector3(point.X + 0.5f, point.Y + 0.5f, 0f);
            BoxCollider2D collider = cell.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            if (collision == "TOP_ONLY")
            {
                PlatformEffector2D effector = cell.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 170f;
                collider.usedByEffector = true;
                Type markerType = LiveType("StarNight.Character.Live.Movement.CharacterLiveOneWayPlatform");
                Component marker = cell.AddComponent(markerType);
                markerType.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(marker, new object[] { collider });
            }
            run.Cells[key] = cell;
            run.Collision[key] = collision;
        }

        private static IEnumerator DestroyRun(PhysicalRun run)
        {
            UnityEngine.Object.Destroy(run.Root);
            yield return null;
        }

        private static Vector2 StartPoint(Sv5JumpPoint takeoff)
        {
            return new Vector2(takeoff.X + 0.5f, takeoff.Y + HalfHeightWithSkin);
        }

        private static float Direction(Sv5JumpPlayerEffectiveLink link)
        {
            return link.Direction == Sv5JumpDirection.LeftToRight ? 1f : -1f;
        }

        private static string ShortRecipe(string recipeId)
        {
            return recipeId.EndsWith("_R0", StringComparison.Ordinal) ? "R0" : "MX";
        }

        private static string Key(int x, int y)
        {
            return x + ":" + y;
        }

        private static string TraceTail(CaseRecord record)
        {
            return string.Join("\n", record.Trace.Select(value =>
                value.Step + " " + value.Event + " p=(" + value.BodyX.ToString("0.00") + "," +
                value.BodyY.ToString("0.00") + ") v=(" + value.VelocityX.ToString("0.00") + "," +
                value.VelocityY.ToString("0.00") + ") g=" + value.IsGrounded));
        }

        private static string FailureDetail(
            PhysicalRun run,
            CaseRecord record,
            Sv5JumpPlayerEffectiveLink link)
        {
            string launch = record.JumpStep >= 0
                ? "launchStep=" + record.JumpStep + " launchBody=" + record.LaunchPosition +
                    " launchVelocity=" + record.LaunchVelocity
                : "launchStep=NONE";
            string face = record.TargetFaceStep >= 0
                ? "targetFaceStep=" + record.TargetFaceStep + " body=" +
                    record.TargetFacePosition + " velocity=" + record.TargetFaceVelocity +
                    " capsule=" + record.TargetFaceCapsuleBounds
                : "targetFaceStep=NONE";
            string schedule = record.GrabApproach != null
                ? "probeEntryX=" + record.GrabApproach.ProbeEntryBodyX.ToString("0.000") +
                    " launchX=" + record.GrabApproach.LaunchBodyX.ToString("0.000") +
                    " stagingX=" + record.GrabApproach.StagingBodyX.ToString("0.000") +
                    " expectedProbeVy=" +
                    record.GrabApproach.ExpectedVelocityYAtProbeEntry.ToString("0.000")
                : "schedule=GENERAL_JUMP";
            string occupancy = string.Join(",", Sv5JumpPlayerVerification.CanonicalFix03Operations()
                .Where(operation => operation.OldOwnerId == "JS04_SOLID" &&
                    (operation.Reason == "LINK02_JS04_LOWER_PROTRUSION_CLEARANCE" ||
                     operation.Reason == "PRESERVE_LINK04_TOP_ONLY_TAKEOFF"))
                .Select(operation => operation.RecipeId + ":" + operation.Point + "=" +
                    operation.NewCollision));
            return record.CaseId + " failed at " + run.Player.Position +
                " reason=" + record.FailureReason +
                " takeoff=" + link.Takeoff + " terminal=" + link.Landing + "\n" +
                launch + "\n" + face + "\n" + schedule + "\n" +
                "firstCollider=" + record.FirstColliderName +
                " grabEntryVy=" + record.GrabEntryVelocityY.ToString("0.000") +
                " landingSupport=" + record.LandingSupportName + "\n" +
                "occupancy=" + occupancy + "\n" + TraceTail(record);
        }

        private static string FullRouteFailureDetail(PhysicalRun run, CaseRecord record)
        {
            string links = string.Join("\n", record.LinkSummaries.Select(value => value.ToString()));
            string trace = string.Join("\n", record.WatchdogTrace
                .Skip(Mathf.Max(0, record.WatchdogTrace.Count - 40))
                .Select(value => value.ToString()));
            return record.CaseId + " failed link=" + record.CurrentLinkId +
                " caseStep=" + record.CaseStep + " linkStep=" + record.LinkStep +
                " position=" + run.Player.Position + " velocity=" + run.Player.Velocity +
                " firstFailure=" + record.FailureReason + "\n" +
                "link entry/exit steps:\n" + links + "\nwatchdog trace tail:\n" + trace;
        }

        private static Type LiveType(string fullName)
        {
            Type type = Type.GetType(fullName + ", Game.Character.Live");
            Assert.IsNotNull(type, "Missing actual live type " + fullName);
            return type;
        }

        private static GameObject LoadPlayerPrefab()
        {
            Type assetDatabase = Type.GetType("UnityEditor.AssetDatabase, UnityEditor");
            Assert.IsNotNull(assetDatabase);
            MethodInfo load = assetDatabase.GetMethod("LoadAssetAtPath",
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(Type) }, null);
            Assert.IsNotNull(load);
            var prefab = load.Invoke(null, new object[]
            {
                Sv5JumpPlayerVerification.PlayerPrefabPath,
                typeof(GameObject),
            }) as GameObject;
            Assert.IsNotNull(prefab, "Actual CharacterLivePlayer prefab is required.");
            return prefab;
        }

        private sealed class PhysicalRun
        {
            public PhysicalRun(
                Sv5JumpRecipeVariant recipe,
                Sv5JumpPlayerFix03Geometry geometry)
            {
                Recipe = recipe;
                Geometry = geometry;
            }

            public readonly Sv5JumpRecipeVariant Recipe;
            public readonly Sv5JumpPlayerFix03Geometry Geometry;
            public readonly Dictionary<string, GameObject> Cells =
                new Dictionary<string, GameObject>(StringComparer.Ordinal);
            public readonly Dictionary<string, string> Collision =
                new Dictionary<string, string>(StringComparer.Ordinal);
            public GameObject Root;
            public ReflectedPlayer Player;
            public Collider2D GrabCollider;

            public string SupportKindAt(Sv5JumpPoint feet)
            {
                string value;
                return Collision.TryGetValue(Key(feet.X, feet.Y - 1), out value) ? value : string.Empty;
            }

            public string ContactKind()
            {
                if (Player.IsGrabbing)
                    return "GRAB_SAFE_SOLID";
                if (!Player.IsGrounded)
                    return "AIR";
                int x = Mathf.FloorToInt(Player.Position.x);
                int supportY = Mathf.RoundToInt(Player.Position.y - HalfHeightWithSkin) - 1;
                string value;
                return Collision.TryGetValue(Key(x, supportY), out value) ? value : "SOLID";
            }

            public bool TryGetSchedulerMetrics(
                float direction,
                out float walkableFrontierX,
                out float frontierDistance,
                out float jumpThreshold)
            {
                walkableFrontierX = -1f;
                frontierDistance = -1f;
                jumpThreshold = -1f;
                float feetY = Player.Position.y - Player.CapsuleHalfHeight - Player.CollisionSkin;
                int supportY = Mathf.RoundToInt(feetY) - 1;
                float left = Player.Position.x - Player.CapsuleHalfWidth;
                float right = Player.Position.x + Player.CapsuleHalfWidth;
                var overlapping = new List<int>();
                for (int x = 0; x < Sv5JumpPlayerVerification.LocalWidth; x++)
                {
                    if (IsClearSupportCell(x, supportY) &&
                        x + 1f >= left - Player.CollisionSkin &&
                        x <= right + Player.CollisionSkin)
                        overlapping.Add(x);
                }
                if (overlapping.Count == 0)
                    return false;

                int sign = direction > 0f ? 1 : -1;
                int supportCell = sign > 0 ? overlapping.Max() : overlapping.Min();
                while (IsClearSupportCell(supportCell + sign, supportY))
                    supportCell += sign;
                walkableFrontierX = sign > 0 ? supportCell + 1f : supportCell;
                float leadingCapsuleEdge = Player.Position.x + sign * Player.CapsuleHalfWidth;
                frontierDistance = Mathf.Abs(leadingCapsuleEdge - walkableFrontierX);
                jumpThreshold = Sv5JumpPlayerVerification.SchedulerJumpThreshold(
                    Player.Velocity.x, Time.fixedDeltaTime, Player.CollisionSkin,
                    Player.CollisionSkin);
                return true;
            }

            public bool TryGetTakeoffSupportBounds(
                Sv5JumpPoint takeoff,
                out float minimumX,
                out float maximumX)
            {
                minimumX = 0f;
                maximumX = 0f;
                int supportY = takeoff.Y - 1;
                if (!IsClearSupportCell(takeoff.X, supportY))
                    return false;

                int left = takeoff.X;
                int right = takeoff.X;
                while (IsClearSupportCell(left - 1, supportY))
                    left--;
                while (IsClearSupportCell(right + 1, supportY))
                    right++;
                minimumX = left;
                maximumX = right + 1f;
                return true;
            }

            public bool IsInsideGrabProbeRange(float direction, out float faceGap)
            {
                faceGap = float.PositiveInfinity;
                if (GrabCollider == null)
                    return false;

                Bounds playerBounds = Player.CapsuleBounds;
                Bounds targetBounds = GrabCollider.bounds;
                faceGap = direction > 0f
                    ? targetBounds.min.x - playerBounds.max.x
                    : playerBounds.min.x - targetBounds.max.x;
                return faceGap <= Player.GrabProbeDistance + Player.CollisionSkin + 0.0001f;
            }

            public Collider2D FindLandingSupportCollider()
            {
                Bounds bounds = Player.CapsuleBounds;
                Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + Player.CollisionSkin);
                RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down,
                    Player.CollisionSkin + Physics2D.defaultContactOffset + 0.08f, SolidMask);
                return hits.Where(hit => hit.collider != null &&
                        hit.collider != Player.BodyCollider && !hit.collider.isTrigger)
                    .OrderBy(hit => hit.distance).Select(hit => hit.collider).FirstOrDefault();
            }

            private bool IsClearSupportCell(int x, int supportY)
            {
                return x >= 0 && x < Sv5JumpPlayerVerification.LocalWidth &&
                    Sv5JumpPlayerVerification.IsSchedulerWalkableSupport(
                        CollisionAt(x, supportY),
                        CollisionAt(x, supportY + 1),
                        CollisionAt(x, supportY + 2));
            }

            private string CollisionAt(int x, int y)
            {
                string value;
                return Collision.TryGetValue(Key(x, y), out value) ? value : string.Empty;
            }
        }

        private sealed class ReflectedPlayer
        {
            private readonly Rigidbody2D body;
            private readonly CapsuleCollider2D capsule;
            private readonly Component inputSource;
            private readonly Component movement;
            private readonly object adapter;
            private readonly Type buttonType;
            private readonly ConstructorInfo buttonConstructor;
            private readonly MethodInfo accumulate;
            private readonly MethodInfo configure;
            private readonly MethodInfo reset;
            private readonly PropertyInfo grounded;
            private readonly PropertyInfo grabbing;
            private readonly PropertyInfo climbing;
            private readonly PropertyInfo dropping;
            private readonly PropertyInfo velocity;
            private readonly float collisionSkin;
            private readonly float grabProbeDistance;
            private readonly float grabMaxUpwardVelocity;
            private readonly float jumpVelocity;
            private readonly float riseGravity;
            private readonly float maxAirSpeed;
            private readonly float groundAcceleration;
            private readonly float groundDeceleration;

            public ReflectedPlayer(GameObject instance)
            {
                Type rigType = LiveType("StarNight.Character.Live.Player.CharacterLivePlayerRig");
                Type movementType = LiveType("StarNight.Character.Live.Movement.CharacterLiveMovementDriver");
                Type inputType = LiveType("StarNight.Character.Live.Input.CharacterLiveInputSource");
                buttonType = LiveType("StarNight.Character.Live.Input.CharacterLiveButtonFrame");
                Component rig = instance.GetComponent(rigType);
                movement = instance.GetComponent(movementType);
                inputSource = instance.GetComponent(inputType);
                Assert.IsNotNull(rig);
                Assert.IsNotNull(movement);
                Assert.IsNotNull(inputSource);
                body = (Rigidbody2D)rigType.GetProperty("Body").GetValue(rig);
                capsule = (CapsuleCollider2D)rigType.GetProperty("BodyCollider").GetValue(rig);
                adapter = inputType.GetProperty("Adapter").GetValue(inputSource);
                buttonConstructor = buttonType.GetConstructor(new[] { typeof(bool), typeof(bool), typeof(bool) });
                accumulate = adapter.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Single(method => method.Name == "AccumulateFrame" && method.GetParameters().Length == 6);
                configure = movementType.GetMethod("ConfigureRmap02", BindingFlags.Public | BindingFlags.Instance);
                reset = movementType.GetMethod("ResetMotion", BindingFlags.Public | BindingFlags.Instance);
                grounded = movementType.GetProperty("IsGroundedNow");
                grabbing = movementType.GetProperty("IsGrabbing");
                climbing = movementType.GetProperty("IsClimbing");
                dropping = movementType.GetProperty("IsDroppingThroughOneWay");
                velocity = movementType.GetProperty("Velocity");
                FieldInfo skin = movementType.GetField("Skin", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(skin);
                collisionSkin = (float)skin.GetRawConstantValue();
                object liveSettings = movementType.GetProperty("Settings",
                    BindingFlags.Public | BindingFlags.Instance).GetValue(movement);
                Assert.IsNotNull(liveSettings);
                Type settingsType = liveSettings.GetType();
                grabProbeDistance = ReadFloat(liveSettings, "GrabProbeDistance");
                grabMaxUpwardVelocity = ReadFloat(liveSettings, "GrabMaxUpwardVelocity");
                jumpVelocity = ReadFloat(liveSettings, "JumpVelocity");
                object gravitySettings = settingsType.GetMethod("CreateGravitySettings",
                    BindingFlags.Public | BindingFlags.Instance).Invoke(liveSettings, null);
                object airSettings = settingsType.GetMethod("CreateAirControlSettings",
                    BindingFlags.Public | BindingFlags.Instance).Invoke(liveSettings, null);
                object groundSettings = settingsType.GetMethod("CreateGroundMotorSettings",
                    BindingFlags.Public | BindingFlags.Instance).Invoke(liveSettings, null);
                riseGravity = ReadFloat(gravitySettings, "RiseGravity");
                maxAirSpeed = ReadFloat(airSettings, "MaxAirSpeed");
                groundAcceleration = ReadFloat(groundSettings, "GroundAcceleration");
                groundDeceleration = ReadFloat(groundSettings, "GroundDeceleration");
                Assert.AreEqual(new Vector2(0.72f, 0.9f), capsule.size);
                Assert.AreEqual(Vector2.zero, capsule.offset);
                Assert.AreEqual(RigidbodyType2D.Kinematic, body.bodyType);
            }

            public Vector2 Position { get { return body.position; } }
            public Vector2 Velocity { get { return (Vector2)velocity.GetValue(movement); } }
            public bool IsGrounded { get { return (bool)grounded.GetValue(movement); } }
            public bool IsGrabbing { get { return (bool)grabbing.GetValue(movement); } }
            public Bounds CapsuleBounds { get { return capsule.bounds; } }
            public Collider2D BodyCollider { get { return capsule; } }
            public float CapsuleHalfWidth { get { return capsule.bounds.extents.x; } }
            public float CapsuleHalfHeight { get { return capsule.bounds.extents.y; } }
            public float CollisionSkin { get { return collisionSkin; } }
            public float GrabProbeDistance { get { return grabProbeDistance; } }
            public float GrabMaxUpwardVelocity { get { return grabMaxUpwardVelocity; } }
            public float JumpVelocity { get { return jumpVelocity; } }
            public float RiseGravity { get { return riseGravity; } }
            public float MaxAirSpeed { get { return maxAirSpeed; } }
            public float GroundAcceleration { get { return groundAcceleration; } }
            public float GroundDeceleration { get { return groundDeceleration; } }

            public void Configure(int mask)
            {
                configure.Invoke(movement, new object[] { mask });
            }

            public void SetPreStartPosition(Vector2 value)
            {
                body.position = value;
            }

            public void ResetMotion()
            {
                reset.Invoke(movement, null);
            }

            public void DisableLiveInputSource()
            {
                ((Behaviour)inputSource).enabled = false;
            }

            public void Feed(
                float horizontal,
                bool up,
                bool down,
                bool walk,
                bool jumpPressed,
                bool jumpReleased,
                bool jumpHeld)
            {
                object jump = buttonConstructor.Invoke(new object[] { jumpPressed, jumpReleased, jumpHeld });
                object idle = buttonConstructor.Invoke(new object[] { false, false, false });
                accumulate.Invoke(adapter, new[] { (object)horizontal, down, jump, idle, idle, idle });
            }

            public Sv5JumpPlayerTraceSample Sample(
                int step,
                float horizontal,
                bool up,
                bool down,
                bool jump,
                bool shift,
                string contact,
                string eventName,
                bool groundedBeforeStep = false,
                float walkableFrontierX = -1f,
                float frontierDistance = -1f,
                float jumpThreshold = -1f,
                string schedulerPolicyId = "")
            {
                Vector2 currentVelocity = Velocity;
                return new Sv5JumpPlayerTraceSample(step, horizontal, up, down, jump, shift,
                    Position.x, Position.y, currentVelocity.x, currentVelocity.y,
                    IsGrounded, IsGrabbing, (bool)climbing.GetValue(movement),
                    (bool)dropping.GetValue(movement), contact, eventName,
                    true, false, groundedBeforeStep, walkableFrontierX, frontierDistance, jumpThreshold,
                    schedulerPolicyId);
            }

            private static float ReadFloat(object target, string propertyName)
            {
                Assert.IsNotNull(target);
                PropertyInfo property = target.GetType().GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(property, propertyName + " is required from the actual Player settings.");
                return (float)property.GetValue(target);
            }
        }

        private sealed class CaseRecord
        {
            public CaseRecord(
                string caseId,
                string recipeId,
                Sv5JumpPlayerCaseKind kind,
                int order,
                string sourceId,
                string targetId,
                int checkpoint)
            {
                CaseId = caseId;
                RecipeId = recipeId;
                Kind = kind;
                Order = order;
                SourceId = sourceId;
                TargetId = targetId;
                Checkpoint = checkpoint;
            }

            public readonly string CaseId;
            public readonly string RecipeId;
            public readonly Sv5JumpPlayerCaseKind Kind;
            public readonly int Order;
            public readonly string SourceId;
            public readonly string TargetId;
            public readonly int Checkpoint;
            public readonly List<Sv5JumpPlayerTraceSample> Trace =
                new List<Sv5JumpPlayerTraceSample>();
            public readonly List<WatchdogTraceSample> WatchdogTrace =
                new List<WatchdogTraceSample>();
            public readonly List<LinkWatchdogSummary> LinkSummaries =
                new List<LinkWatchdogSummary>();
            public bool Passed = true;
            public bool UsedGrab;
            public bool UsedOneWay;
            public bool FirstCatchMatched = true;
            public bool GrabbedPlatform;
            public int GrabEntries;
            public int GrabExits;
            public int OneWayLandings;
            public float MeasuredRiseCells;
            public Sv5JumpPlayerGrabApproachSchedule GrabApproach;
            public int JumpStep = -1;
            public Vector2 LaunchPosition;
            public Vector2 LaunchVelocity;
            public int TargetFaceStep = -1;
            public Vector2 TargetFacePosition;
            public Vector2 TargetFaceVelocity;
            public Bounds TargetFaceCapsuleBounds;
            public string FirstColliderName = string.Empty;
            public float GrabEntryVelocityY;
            public string LandingSupportName = string.Empty;
            public string FailureReason = string.Empty;
            public int CaseStep;
            public int LinkStep;
            public int RouteLinkCount = 1;
            public string CurrentLinkId = string.Empty;
            public string CurrentPhase = string.Empty;
            public int CurrentLinkEntryCaseStep;
            public bool CurrentLinkReachedTerminal;
            public bool LinkOpen;

            public void ConfigureRouteBudget(int routeLinkCount)
            {
                Assert.Greater(routeLinkCount, 0);
                RouteLinkCount = routeLinkCount;
            }

            public Sv5JumpPlayerCase ToCase()
            {
                return new Sv5JumpPlayerCase(CaseId, RecipeId, Kind, Order, SourceId, TargetId,
                    Checkpoint, Passed, true, true, true, true, UsedGrab, UsedOneWay,
                    false, false, 0, MeasuredRiseCells, FirstCatchMatched, GrabbedPlatform, Trace);
            }
        }

        private sealed class WatchdogTraceSample
        {
            public int CaseStep;
            public int LinkStep;
            public string LinkId;
            public string Phase;
            public Vector2 Position;
            public Vector2 Velocity;
            public bool Grounded;
            public bool Grabbing;
            public bool Terminal;

            public static WatchdogTraceSample Capture(
                PhysicalRun run,
                CaseRecord record,
                bool terminal)
            {
                return new WatchdogTraceSample
                {
                    CaseStep = record.CaseStep,
                    LinkStep = record.LinkStep,
                    LinkId = record.CurrentLinkId,
                    Phase = record.CurrentPhase,
                    Position = run.Player.Position,
                    Velocity = run.Player.Velocity,
                    Grounded = run.Player.IsGrounded,
                    Grabbing = run.Player.IsGrabbing,
                    Terminal = terminal,
                };
            }

            public override string ToString()
            {
                return "caseStep=" + CaseStep + " linkStep=" + LinkStep +
                    " LinkId=" + LinkId + " phase=" + Phase +
                    " position=" + Position + " velocity=" + Velocity +
                    " grounded=" + Grounded + " grab=" + Grabbing +
                    " terminal=" + Terminal;
            }
        }

        private sealed class LinkWatchdogSummary
        {
            public LinkWatchdogSummary(
                string linkId,
                int entryCaseStep,
                int exitCaseStep,
                int linkStep,
                bool terminal,
                Vector2 position,
                Vector2 velocity,
                string failure)
            {
                LinkId = linkId;
                EntryCaseStep = entryCaseStep;
                ExitCaseStep = exitCaseStep;
                LinkStep = linkStep;
                Terminal = terminal;
                Position = position;
                Velocity = velocity;
                Failure = failure;
            }

            private readonly string LinkId;
            private readonly int EntryCaseStep;
            private readonly int ExitCaseStep;
            private readonly int LinkStep;
            private readonly bool Terminal;
            private readonly Vector2 Position;
            private readonly Vector2 Velocity;
            private readonly string Failure;

            public override string ToString()
            {
                return "LinkId=" + LinkId + " entryCaseStep=" + EntryCaseStep +
                    " entryLinkStep=0 exitCaseStep=" + ExitCaseStep +
                    " exitLinkStep=" + LinkStep + " terminal=" + Terminal +
                    " position=" + Position + " velocity=" + Velocity +
                    " failure=" + Failure;
            }
        }
    }
}
#endif
