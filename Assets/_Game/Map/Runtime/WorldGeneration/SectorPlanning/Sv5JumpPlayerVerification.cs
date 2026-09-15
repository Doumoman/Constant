using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5JumpPlayerCaseKind
    {
        MainLink,
        FullRoute,
        Recovery,
    }

    public static class Sv5JumpPlayerDiagnostic
    {
        public const string RecoveryIdentity = "SV5_19_RECOVERY_IDENTITY_MISMATCH";
        public const string CaseSet = "PHYSICAL_CASE_SET_MUST_BE_EXACTLY_18_2_18";
        public const string FakePlayer = "ACTUAL_CHARACTER_LIVE_PLAYER_REQUIRED";
        public const string MissingDriver = "CHARACTER_LIVE_MOVEMENT_DRIVER_REQUIRED";
        public const string MissingBody = "RIGIDBODY2D_REQUIRED";
        public const string MissingCollider = "CAPSULE_COLLIDER2D_REQUIRED";
        public const string PostStartTeleport = "POST_START_TELEPORT_FORBIDDEN";
        public const string LogicalTraceReuse = "ACTUAL_FIXED_STEP_TRACE_REQUIRED";
        public const string WrongFirstCatch = "PHYSICAL_FIRST_CATCH_MISMATCH";
        public const string LaterCheckpoint = "RECOVERY_CHECKPOINT_LATER_THAN_FAILED_LINK";
        public const string MissingGrabEvents = "GRAB_ENTER_AND_EXIT_REQUIRED";
        public const string PlatformGrab = "TOP_ONLY_PLATFORM_MUST_NOT_BE_GRABBABLE";
        public const string ItemUse = "ITEM_USE_FORBIDDEN";
        public const string ReverseRequired = "REVERSE_COMPLETION_CLAIM_FORBIDDEN";
        public const string RiseCap = "JUMP_PLUS_GRAB_RISE_EXCEEDS_TWO_CELLS";
        public const string TraceBoundary = "TRACE_START_AND_PASS_TARGET_REQUIRED";
        public const string RecoveryEvents = "RECOVERY_CATCH_AND_REJOIN_REQUIRED";
        public const string TraceOrder = "FIXED_STEP_TRACE_ORDER_INVALID";
        public const string Outcome = "PHYSICAL_CASE_DID_NOT_PASS";
        public const string FalseReadiness = "PLAYER_VERIFIED_REQUIRES_ALL_PHYSICAL_GATES";
    }

    public sealed class Sv5JumpPlayerTraceSample
    {
        public Sv5JumpPlayerTraceSample(
            int step,
            float inputX,
            bool inputUp,
            bool inputDown,
            bool inputJump,
            bool inputShift,
            float bodyX,
            float bodyY,
            float velocityX,
            float velocityY,
            bool isGrounded,
            bool isGrabbing,
            bool isClimbing,
            bool isDroppingThrough,
            string contactKind,
            string eventName,
            bool actualComponentSample = true,
            bool reusedLogicalTrace = false,
            bool groundedBeforeStep = false,
            float walkableFrontierX = -1f,
            float frontierDistance = -1f,
            float jumpThreshold = -1f,
            string schedulerPolicyId = "")
        {
            Step = step;
            InputX = inputX;
            InputUp = inputUp;
            InputDown = inputDown;
            InputJump = inputJump;
            InputShift = inputShift;
            BodyX = bodyX;
            BodyY = bodyY;
            VelocityX = velocityX;
            VelocityY = velocityY;
            IsGrounded = isGrounded;
            IsGrabbing = isGrabbing;
            IsClimbing = isClimbing;
            IsDroppingThrough = isDroppingThrough;
            ContactKind = contactKind ?? string.Empty;
            Event = eventName ?? string.Empty;
            ActualComponentSample = actualComponentSample;
            ReusedLogicalTrace = reusedLogicalTrace;
            GroundedBeforeStep = groundedBeforeStep;
            WalkableFrontierX = walkableFrontierX;
            FrontierDistance = frontierDistance;
            JumpThreshold = jumpThreshold;
            SchedulerPolicyId = schedulerPolicyId ?? string.Empty;
        }

        public int Step { get; }
        public float InputX { get; }
        public bool InputUp { get; }
        public bool InputDown { get; }
        public bool InputJump { get; }
        public bool InputShift { get; }
        public float BodyX { get; }
        public float BodyY { get; }
        public float VelocityX { get; }
        public float VelocityY { get; }
        public bool IsGrounded { get; }
        public bool IsGrabbing { get; }
        public bool IsClimbing { get; }
        public bool IsDroppingThrough { get; }
        public string ContactKind { get; }
        public string Event { get; }
        public bool ActualComponentSample { get; }
        public bool ReusedLogicalTrace { get; }
        public bool GroundedBeforeStep { get; }
        public float WalkableFrontierX { get; }
        public float FrontierDistance { get; }
        public float JumpThreshold { get; }
        public string SchedulerPolicyId { get; }

        internal string DigestToken
        {
            get
            {
                return Step + "|" + F(InputX) + "|" + InputUp + "|" + InputDown + "|" +
                    InputJump + "|" + InputShift + "|" + F(BodyX) + "|" + F(BodyY) + "|" +
                    F(VelocityX) + "|" + F(VelocityY) + "|" + IsGrounded + "|" +
                    IsGrabbing + "|" + IsClimbing + "|" + IsDroppingThrough + "|" +
                    ContactKind + "|" + Event + "|" + ActualComponentSample + "|" +
                    ReusedLogicalTrace + "|" + GroundedBeforeStep + "|" +
                    F(WalkableFrontierX) + "|" + F(FrontierDistance) + "|" +
                    F(JumpThreshold) + "|" + SchedulerPolicyId;
            }
        }

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }

    public sealed class Sv5JumpPlayerCase
    {
        public Sv5JumpPlayerCase(
            string caseId,
            string recipeId,
            Sv5JumpPlayerCaseKind caseKind,
            int mainLinkOrder,
            string sourceId,
            string targetId,
            int checkpointLinkOrder,
            bool passed,
            bool actualPlayer,
            bool actualDriver,
            bool actualRigidbody2D,
            bool actualCollider2D,
            bool usedGrab,
            bool usedOneWay,
            bool usedItem,
            bool reverseRequired,
            int teleportsAfterStart,
            float measuredRiseCells,
            bool firstCatchMatched,
            bool grabbedPlatform,
            IEnumerable<Sv5JumpPlayerTraceSample> trace)
        {
            CaseId = caseId ?? string.Empty;
            RecipeId = recipeId ?? string.Empty;
            CaseKind = caseKind;
            MainLinkOrder = mainLinkOrder;
            SourceId = sourceId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            CheckpointLinkOrder = checkpointLinkOrder;
            Passed = passed;
            ActualPlayer = actualPlayer;
            ActualDriver = actualDriver;
            ActualRigidbody2D = actualRigidbody2D;
            ActualCollider2D = actualCollider2D;
            UsedGrab = usedGrab;
            UsedOneWay = usedOneWay;
            UsedItem = usedItem;
            ReverseRequired = reverseRequired;
            TeleportsAfterStart = teleportsAfterStart;
            MeasuredRiseCells = measuredRiseCells;
            FirstCatchMatched = firstCatchMatched;
            GrabbedPlatform = grabbedPlatform;
            Trace = Array.AsReadOnly((trace ?? Array.Empty<Sv5JumpPlayerTraceSample>()).ToArray());
        }

        public string CaseId { get; }
        public string RecipeId { get; }
        public Sv5JumpPlayerCaseKind CaseKind { get; }
        public int MainLinkOrder { get; }
        public string SourceId { get; }
        public string TargetId { get; }
        public int CheckpointLinkOrder { get; }
        public bool Passed { get; }
        public bool ActualPlayer { get; }
        public bool ActualDriver { get; }
        public bool ActualRigidbody2D { get; }
        public bool ActualCollider2D { get; }
        public bool UsedGrab { get; }
        public bool UsedOneWay { get; }
        public bool UsedItem { get; }
        public bool ReverseRequired { get; }
        public int TeleportsAfterStart { get; }
        public float MeasuredRiseCells { get; }
        public bool FirstCatchMatched { get; }
        public bool GrabbedPlatform { get; }
        public IReadOnlyList<Sv5JumpPlayerTraceSample> Trace { get; }
        public int FixedSteps { get { return Trace.Count; } }

        internal string DigestToken
        {
            get
            {
                return CaseId + "|" + RecipeId + "|" + Sv5JumpPlayerVerification.CaseKindName(CaseKind) +
                    "|" + MainLinkOrder + "|" + SourceId + "|" + TargetId + "|" +
                    CheckpointLinkOrder + "|" + Passed + "|" + ActualPlayer + "|" +
                    ActualDriver + "|" + ActualRigidbody2D + "|" + ActualCollider2D + "|" +
                    UsedGrab + "|" + UsedOneWay + "|" + UsedItem + "|" + ReverseRequired +
                    "|" + TeleportsAfterStart + "|" + MeasuredRiseCells.ToString("R", CultureInfo.InvariantCulture) +
                    "|" + FirstCatchMatched + "|" + GrabbedPlatform + "|" +
                    string.Join(";", Trace.Select(value => value == null ? "NULL" : value.DigestToken));
            }
        }
    }

    public sealed class Sv5JumpPlayerBindingFile
    {
        public Sv5JumpPlayerBindingFile(string path, string oid, string sha256, int bytes)
        {
            Path = path ?? string.Empty;
            GitBlobOid = oid ?? string.Empty;
            Sha256 = sha256 ?? string.Empty;
            Bytes = bytes;
        }

        public string Path { get; }
        public string GitBlobOid { get; }
        public string Sha256 { get; }
        public int Bytes { get; }
        public string Role { get { return "READ_ONLY"; } }
    }

    public sealed class Sv5JumpPlayerMeasurements
    {
        public Sv5JumpPlayerMeasurements(
            float fixedDeltaTime,
            float capsuleWidth,
            float capsuleHeight,
            float jumpVelocity,
            float runSpeed,
            float walkSpeed,
            float grabProbe,
            float grabVerticalWindow,
            int deterministicTerminalToleranceSteps)
        {
            FixedDeltaTime = fixedDeltaTime;
            CapsuleWidth = capsuleWidth;
            CapsuleHeight = capsuleHeight;
            JumpVelocity = jumpVelocity;
            RunSpeed = runSpeed;
            WalkSpeed = walkSpeed;
            GrabProbe = grabProbe;
            GrabVerticalWindow = grabVerticalWindow;
            DeterministicTerminalToleranceSteps = deterministicTerminalToleranceSteps;
        }

        public float FixedDeltaTime { get; }
        public float CapsuleWidth { get; }
        public float CapsuleHeight { get; }
        public float JumpVelocity { get; }
        public float RunSpeed { get; }
        public float WalkSpeed { get; }
        public float GrabProbe { get; }
        public float GrabVerticalWindow { get; }
        public int DeterministicTerminalToleranceSteps { get; }
        public string Source { get { return "ACTUAL_BASE_COMMIT_PLAYER_COMPONENTS"; } }
        public bool Retuned { get { return false; } }
    }

    public sealed class Sv5JumpPlayerProof
    {
        internal Sv5JumpPlayerProof(
            IEnumerable<Sv5JumpPlayerCase> cases,
            IEnumerable<string> diagnostics,
            bool jumpRecipeReady,
            bool composedGeometryReady,
            bool sweptClearanceReady,
            bool recoveryReady,
            bool playerVerifiedClaim,
            int deterministicRepeatCount,
            int deterministicTerminalDeltaSteps)
        {
            Cases = Array.AsReadOnly((cases ?? Array.Empty<Sv5JumpPlayerCase>())
                .OrderBy(value => value == null ? string.Empty : value.CaseId, StringComparer.Ordinal).ToArray());
            Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<string>()).Distinct().OrderBy(value => value,
                StringComparer.Ordinal).ToArray());
            JumpRecipeReady = jumpRecipeReady;
            ComposedGeometryReady = composedGeometryReady;
            SweptClearanceReady = sweptClearanceReady;
            RecoveryReady = recoveryReady;
            PlayerVerifiedClaim = playerVerifiedClaim;
            DeterministicRepeatCount = deterministicRepeatCount;
            DeterministicTerminalDeltaSteps = deterministicTerminalDeltaSteps;
            Digest = Sv5JumpPlayerVerification.Hash(string.Join("\n", Cases.Select(value =>
                value == null ? "NULL" : value.DigestToken)));
        }

        public IReadOnlyList<Sv5JumpPlayerCase> Cases { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public bool JumpRecipeReady { get; }
        public bool ComposedGeometryReady { get; }
        public bool SweptClearanceReady { get; }
        public bool RecoveryReady { get; }
        public bool PlayerVerifiedClaim { get; }
        public bool PlayerVerified
        {
            get
            {
                return PlayerVerifiedClaim && Diagnostics.Count == 0 && JumpRecipeReady &&
                    ComposedGeometryReady && SweptClearanceReady && RecoveryReady;
            }
        }
        public int DeterministicRepeatCount { get; }
        public int DeterministicTerminalDeltaSteps { get; }
        public int WholeWorldBuilds { get { return 0; } }
        public int WholeWorldSearches { get { return 0; } }
        public int GlobalEndpointComparisons { get { return 0; } }
        public int SectorPartitions { get { return 0; } }
    }

    public sealed class Sv5JumpPlayerPatchOperation
    {
        public Sv5JumpPlayerPatchOperation(
            string recipeId,
            string operation,
            int x,
            int y,
            string oldCollision,
            string newCollision,
            string oldOwnerId = "",
            string newOwnerId = "",
            string reason = "")
        {
            RecipeId = recipeId ?? string.Empty;
            Operation = operation ?? string.Empty;
            Point = new Sv5JumpPoint(x, y);
            OldCollision = oldCollision ?? string.Empty;
            NewCollision = newCollision ?? string.Empty;
            OldOwnerId = oldOwnerId ?? string.Empty;
            NewOwnerId = newOwnerId ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string RecipeId { get; }
        public string Operation { get; }
        public Sv5JumpPoint Point { get; }
        public string OldCollision { get; }
        public string NewCollision { get; }
        public string OldOwnerId { get; }
        public string NewOwnerId { get; }
        public string OwnerId { get { return NewOwnerId; } }
        public string Reason { get; }
        internal string Token
        {
            get
            {
                return RecipeId + "|" + Operation + "|" + Point.X + "|" + Point.Y +
                    "|" + OldCollision + "|" + NewCollision + "|" + OldOwnerId +
                    "|" + NewOwnerId + "|" + Reason;
            }
        }
    }

    public sealed class Sv5JumpPlayerComposedCell
    {
        public Sv5JumpPlayerComposedCell(
            string recipeId,
            Sv5JumpPoint point,
            string collision,
            string sourceLayer,
            string ownerId)
        {
            RecipeId = recipeId ?? string.Empty;
            Point = point;
            Collision = collision ?? string.Empty;
            SourceLayer = sourceLayer ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
        }

        public string RecipeId { get; }
        public Sv5JumpPoint Point { get; }
        public string Collision { get; }
        public string SourceLayer { get; }
        public string OwnerId { get; }
    }

    public sealed class Sv5JumpPlayerEffectiveLink
    {
        public Sv5JumpPlayerEffectiveLink(
            string recipeId,
            int order,
            string sourceLinkId,
            Sv5JumpMode mode,
            Sv5JumpDirection direction,
            Sv5JumpPoint takeoff,
            Sv5JumpPoint landing,
            bool hasWalkToNext,
            Sv5JumpPoint walkToNextTakeoff,
            string geometryPatchId)
        {
            RecipeId = recipeId ?? string.Empty;
            Order = order;
            SourceLinkId = sourceLinkId ?? string.Empty;
            Mode = mode;
            Direction = direction;
            Takeoff = takeoff;
            Landing = landing;
            HasWalkToNext = hasWalkToNext;
            WalkToNextTakeoff = walkToNextTakeoff;
            GeometryPatchId = geometryPatchId ?? string.Empty;
        }

        public string RecipeId { get; }
        public int Order { get; }
        public string SourceLinkId { get; }
        public Sv5JumpMode Mode { get; }
        public Sv5JumpDirection Direction { get; }
        public Sv5JumpPoint Takeoff { get; }
        public Sv5JumpPoint Landing { get; }
        public bool HasWalkToNext { get; }
        public Sv5JumpPoint WalkToNextTakeoff { get; }
        public string GeometryPatchId { get; }
    }

    public sealed class Sv5JumpPlayerGrabApproachSchedule
    {
        public Sv5JumpPlayerGrabApproachSchedule(
            float targetNearFaceX,
            float probeEntryBodyX,
            float launchBodyX,
            float stagingBodyX,
            float expectedProbeEntrySeconds,
            float expectedVelocityYAtProbeEntry,
            float safeSupportMinimumX,
            float safeSupportMaximumX)
        {
            TargetNearFaceX = targetNearFaceX;
            ProbeEntryBodyX = probeEntryBodyX;
            LaunchBodyX = launchBodyX;
            StagingBodyX = stagingBodyX;
            ExpectedProbeEntrySeconds = expectedProbeEntrySeconds;
            ExpectedVelocityYAtProbeEntry = expectedVelocityYAtProbeEntry;
            SafeSupportMinimumX = safeSupportMinimumX;
            SafeSupportMaximumX = safeSupportMaximumX;
        }

        public float TargetNearFaceX { get; }
        public float ProbeEntryBodyX { get; }
        public float LaunchBodyX { get; }
        public float StagingBodyX { get; }
        public float ExpectedProbeEntrySeconds { get; }
        public float ExpectedVelocityYAtProbeEntry { get; }
        public float SafeSupportMinimumX { get; }
        public float SafeSupportMaximumX { get; }
    }

    public sealed class Sv5JumpPlayerFix03Geometry
    {
        internal Sv5JumpPlayerFix03Geometry(
            IEnumerable<Sv5JumpPlayerPatchOperation> operations,
            IEnumerable<Sv5JumpPlayerComposedCell> cells,
            IEnumerable<Sv5JumpPlayerEffectiveLink> links,
            IEnumerable<string> diagnostics)
        {
            Operations = Array.AsReadOnly((operations ?? Array.Empty<Sv5JumpPlayerPatchOperation>()).ToArray());
            ComposedCells = Array.AsReadOnly((cells ?? Array.Empty<Sv5JumpPlayerComposedCell>())
                .OrderBy(value => value.RecipeId, StringComparer.Ordinal).ThenBy(value => value.Point).ToArray());
            EffectiveLinks = Array.AsReadOnly((links ?? Array.Empty<Sv5JumpPlayerEffectiveLink>())
                .OrderBy(value => value.RecipeId, StringComparer.Ordinal).ThenBy(value => value.Order).ToArray());
            Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<string>()).Distinct()
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public IReadOnlyList<Sv5JumpPlayerPatchOperation> Operations { get; }
        public IReadOnlyList<Sv5JumpPlayerComposedCell> ComposedCells { get; }
        public IReadOnlyList<Sv5JumpPlayerEffectiveLink> EffectiveLinks { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public bool Ready { get { return Diagnostics.Count == 0; } }
        public int ChangedCellCount { get { return Operations.Count; } }
        public int RemovedCellCount { get { return Operations.Count(value => value.Operation == "REMOVE"); } }
        public int ConvertedCellCount { get { return Operations.Count(value => value.Operation == "CONVERT"); } }
        public int AddedCellCount { get { return Operations.Count(value => value.Operation == "ADD"); } }
    }

    public static class Sv5JumpPlayerVerification
    {
        public const string BaseCommit = "660d0c58ec0f65cbbbe200716d0e3c36b0ea4ba7";
        public const string InputRecoveryDigest = "f8b2e69686dbd9b7a29b82e4e4a8ca2c46c6183a005a5373e986e04b21f4288b";
        public const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        public const int LocalWidth = 24;
        public const int LocalHeight = 32;
        public const int RequiredMainCases = 18;
        public const int RequiredFullRouteCases = 2;
        public const int RequiredRecoveryCases = 18;
        public const string Fix03PatchId = "SV5_20_FIX03_LOCAL_PHYSICAL_CLEARANCE";
        public const string Fix03Js04PassThroughOwnerId = "SV5_20_FIX03_JS04_PASS_THROUGH";
        public const string Fix03Js08PassThroughOwnerId = "SV5_20_FIX03_JS08_PASS_THROUGH";
        public const string Fix03FilesSha256 = "52d19aeb525ee2463b949335e8fc200758e7ee64c17c43e449d8c54222c6dc91";
        public const string Fix03GeometryPatchSha256 = "763c6b0bb379556b9364631a30c10fc89ef38fd99ffc890a9aa7c33f47621665";
        public const string Fix03SchedulerProfileSha256 = "eb7ab01d02b51f4b2dcaa25e2ea81b3fa7e7c112b41392cf461f7ba2b986ae43";
        public const string SchedulerPolicyId = "WALKABLE_FRONTIER_AND_GRAB_EXIT_V2";
        public const string GrabApproachSchedulerPolicyId = "TERMINAL_SOLID_DESCENDING_GRAB_APPROACH_V1";
        public const float MinimumDiagnosticBodyY = -2f;
        public const int MaximumDiagnosticFixedSteps = 240;
        public const string FixOperationCount = "FIX03_EXACT_EIGHTEEN_OPERATIONS_REQUIRED";
        public const string FixMirror = "FIX03_EXACT_MIRROR_REQUIRED";
        public const string FixConversion = "FIX03_CONVERSION_IDENTITY_REQUIRED";
        public const string FixBorrowedSupport = "FIX03_BORROWED_SUPPORT_IDENTITY_REQUIRED";
        public const string FixPredecessor = "FIX03_PREDECESSOR_CHANGED";
        public const string FixPlayerRetune = "FIX03_PLAYER_RETUNE_FORBIDDEN";
        public const string GrabExitNoVerticalImpulse = "GRAB_EXIT_NO_VERTICAL_IMPULSE";

        public static IReadOnlyList<Sv5JumpPlayerPatchOperation> CanonicalFix03Operations()
        {
            return Array.AsReadOnly(new[]
            {
                Op(Sv5JumpRecipeCatalog.R0RecipeId, "REMOVE", 14, 4, "JS04_SOLID", Fix03PatchId, "LINK02_BODY_CLEARANCE"),
                Op(Sv5JumpRecipeCatalog.R0RecipeId, "REMOVE", 14, 5, "JS04_SOLID", Fix03PatchId, "LINK02_HEAD_CLEARANCE"),
                Op(Sv5JumpRecipeCatalog.R0RecipeId, "CONVERT", 14, 6, "JS04_SOLID", Fix03Js04PassThroughOwnerId, "PRESERVE_LINK04_TOP_ONLY_TAKEOFF"),
                Op(Sv5JumpRecipeCatalog.R0RecipeId, "REMOVE", 15, 5, "JS04_SOLID", Fix03PatchId, "LINK02_JS04_LOWER_PROTRUSION_CLEARANCE"),
                Op(Sv5JumpRecipeCatalog.R0RecipeId, "REMOVE", 3, 7, "JS08_SOLID", Fix03PatchId, "LINK06_PASS_THROUGH"),
                Op(Sv5JumpRecipeCatalog.R0RecipeId, "REMOVE", 3, 8, "JS08_SOLID", Fix03PatchId, "LINK06_PASS_THROUGH"),
                Op(Sv5JumpRecipeCatalog.R0RecipeId, "REMOVE", 4, 8, "JS08_SOLID", Fix03PatchId, "LINK06_PASS_THROUGH"),
                Op(Sv5JumpRecipeCatalog.R0RecipeId, "CONVERT", 3, 9, "JS08_SOLID", Fix03Js08PassThroughOwnerId, "PRESERVE_LINK07_08_SUPPORT"),
                Op(Sv5JumpRecipeCatalog.R0RecipeId, "CONVERT", 4, 9, "JS08_SOLID", Fix03Js08PassThroughOwnerId, "PRESERVE_LINK07_08_SUPPORT"),
                Op(Sv5JumpRecipeCatalog.MirrorRecipeId, "REMOVE", 9, 4, "JS04_SOLID", Fix03PatchId, "LINK02_BODY_CLEARANCE"),
                Op(Sv5JumpRecipeCatalog.MirrorRecipeId, "REMOVE", 9, 5, "JS04_SOLID", Fix03PatchId, "LINK02_HEAD_CLEARANCE"),
                Op(Sv5JumpRecipeCatalog.MirrorRecipeId, "CONVERT", 9, 6, "JS04_SOLID", Fix03Js04PassThroughOwnerId, "PRESERVE_LINK04_TOP_ONLY_TAKEOFF"),
                Op(Sv5JumpRecipeCatalog.MirrorRecipeId, "REMOVE", 8, 5, "JS04_SOLID", Fix03PatchId, "LINK02_JS04_LOWER_PROTRUSION_CLEARANCE"),
                Op(Sv5JumpRecipeCatalog.MirrorRecipeId, "REMOVE", 20, 7, "JS08_SOLID", Fix03PatchId, "LINK06_PASS_THROUGH"),
                Op(Sv5JumpRecipeCatalog.MirrorRecipeId, "REMOVE", 20, 8, "JS08_SOLID", Fix03PatchId, "LINK06_PASS_THROUGH"),
                Op(Sv5JumpRecipeCatalog.MirrorRecipeId, "REMOVE", 19, 8, "JS08_SOLID", Fix03PatchId, "LINK06_PASS_THROUGH"),
                Op(Sv5JumpRecipeCatalog.MirrorRecipeId, "CONVERT", 20, 9, "JS08_SOLID", Fix03Js08PassThroughOwnerId, "PRESERVE_LINK07_08_SUPPORT"),
                Op(Sv5JumpRecipeCatalog.MirrorRecipeId, "CONVERT", 19, 9, "JS08_SOLID", Fix03Js08PassThroughOwnerId, "PRESERVE_LINK07_08_SUPPORT"),
            });
        }

        public static Sv5JumpPlayerFix03Geometry CreateFix03Geometry()
        {
            return ComposeFix03(CanonicalFix03Operations());
        }

        public static Sv5JumpPlayerFix03Geometry ComposeFix03(
            IEnumerable<Sv5JumpPlayerPatchOperation> operations,
            bool predecessorChanged = false,
            bool playerRetuned = false,
            bool borrowedSupportChanged = false)
        {
            Sv5JumpRecipeCatalogModel catalog = Sv5JumpRecipeCatalog.CreateCanonicalCatalog();
            Sv5JumpRecoveryPlan recovery = Sv5JumpRecovery.CreateCanonicalLocalProof();
            Sv5JumpClearancePlan clearance = Sv5JumpClearance.CreateCanonicalLocalProof();
            var diagnostics = new List<string>();
            var cells = new Dictionary<string, Sv5JumpPlayerComposedCell>(StringComparer.Ordinal);
            foreach (Sv5JumpRecipeVariant recipe in catalog.Recipes)
            {
                foreach (Sv5JumpRecipeOccupancy cell in recipe.Occupancy)
                {
                    cells.Add(CellKey(recipe.RecipeId, cell.Point), new Sv5JumpPlayerComposedCell(
                        recipe.RecipeId, cell.Point, cell.Collision, "SV5_17_BASE", cell.SourceOwnerId));
                }
            }
            foreach (Sv5JumpRecoveryCell cell in recovery.Overlay)
            {
                string key = CellKey(cell.RecipeId, cell.Point);
                if (cells.ContainsKey(key))
                    diagnostics.Add(FixPredecessor + ":OVERLAY_COLLISION:" + key);
                else
                    cells.Add(key, new Sv5JumpPlayerComposedCell(cell.RecipeId, cell.Point,
                        cell.Collision, "SV5_19_RECOVERY", cell.OwnerId));
            }

            Sv5JumpPlayerPatchOperation[] materialized = (operations ??
                Array.Empty<Sv5JumpPlayerPatchOperation>()).Where(value => value != null).ToArray();
            string[] expected = CanonicalFix03Operations().Select(value => value.Token)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] actual = materialized.Select(value => value.Token)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (materialized.Length != 18 || !actual.SequenceEqual(expected))
                diagnostics.Add(FixOperationCount);

            foreach (Sv5JumpPlayerPatchOperation operation in materialized)
            {
                string key = CellKey(operation.RecipeId, operation.Point);
                Sv5JumpPlayerComposedCell existing;
                if (operation.Operation == "REMOVE")
                {
                    if (!cells.TryGetValue(key, out existing) || existing.Collision != operation.OldCollision ||
                        existing.OwnerId != operation.OldOwnerId)
                        diagnostics.Add(FixOperationCount + ":REMOVE_SOURCE:" + key);
                    else
                        cells.Remove(key);
                }
                else if (operation.Operation == "CONVERT")
                {
                    bool knownPassThroughOwner = operation.NewOwnerId == Fix03Js04PassThroughOwnerId ||
                        operation.NewOwnerId == Fix03Js08PassThroughOwnerId;
                    if (!cells.TryGetValue(key, out existing) || existing.Collision != operation.OldCollision ||
                        existing.OwnerId != operation.OldOwnerId || operation.NewCollision != "TOP_ONLY" ||
                        !knownPassThroughOwner)
                    {
                        diagnostics.Add(FixConversion + ":SOURCE:" + key);
                    }
                    else
                    {
                        cells[key] = new Sv5JumpPlayerComposedCell(operation.RecipeId, operation.Point,
                            operation.NewCollision, "SV5_20_FIX03", operation.NewOwnerId);
                    }
                }
                else
                {
                    diagnostics.Add(FixOperationCount + ":OPERATION:" + operation.Operation);
                }
            }

            var r0Tokens = new HashSet<string>(materialized
                .Where(value => value.RecipeId == Sv5JumpRecipeCatalog.R0RecipeId)
                .Select(value => value.Operation + "|" + (23 - value.Point.X) + "|" + value.Point.Y +
                    "|" + value.OldCollision + "|" + value.NewCollision + "|" + value.OldOwnerId +
                    "|" + value.NewOwnerId + "|" + value.Reason), StringComparer.Ordinal);
            var mxTokens = new HashSet<string>(materialized
                .Where(value => value.RecipeId == Sv5JumpRecipeCatalog.MirrorRecipeId)
                .Select(value => value.Operation + "|" + value.Point.X + "|" + value.Point.Y +
                    "|" + value.OldCollision + "|" + value.NewCollision + "|" + value.OldOwnerId +
                    "|" + value.NewOwnerId + "|" + value.Reason), StringComparer.Ordinal);
            if (!r0Tokens.SetEquals(mxTokens)) diagnostics.Add(FixMirror);
            foreach (Tuple<string, Sv5JumpPoint> support in new[]
                     {
                         Tuple.Create(Sv5JumpRecipeCatalog.R0RecipeId, new Sv5JumpPoint(16, 4)),
                         Tuple.Create(Sv5JumpRecipeCatalog.R0RecipeId, new Sv5JumpPoint(17, 4)),
                         Tuple.Create(Sv5JumpRecipeCatalog.MirrorRecipeId, new Sv5JumpPoint(6, 4)),
                         Tuple.Create(Sv5JumpRecipeCatalog.MirrorRecipeId, new Sv5JumpPoint(7, 4)),
                     })
            {
                Sv5JumpPlayerComposedCell value;
                if (!cells.TryGetValue(CellKey(support.Item1, support.Item2), out value) ||
                    value.Collision != "TOP_ONLY" || value.SourceLayer != "SV5_19_RECOVERY" ||
                    value.OwnerId != "RG_GRAB_CATCH")
                    diagnostics.Add(FixBorrowedSupport + ":" + support.Item1);
            }
            if (borrowedSupportChanged) diagnostics.Add(FixBorrowedSupport);
            if (predecessorChanged) diagnostics.Add(FixPredecessor);
            if (playerRetuned) diagnostics.Add(FixPlayerRetune);

            var links = new List<Sv5JumpPlayerEffectiveLink>();
            foreach (Sv5JumpClearanceLinkResult value in clearance.Links)
            {
                Sv5JumpClearanceTrace trace = value.Trace;
                bool corrected = trace.Order == 2;
                Sv5JumpPoint landing = corrected
                    ? trace.RecipeId == Sv5JumpRecipeCatalog.R0RecipeId
                        ? new Sv5JumpPoint(17, 5) : new Sv5JumpPoint(6, 5)
                    : trace.Landing;
                Sv5JumpPoint walk = corrected
                    ? trace.RecipeId == Sv5JumpRecipeCatalog.R0RecipeId
                        ? new Sv5JumpPoint(18, 5) : new Sv5JumpPoint(5, 5)
                    : default;
                links.Add(new Sv5JumpPlayerEffectiveLink(trace.RecipeId, trace.Order,
                    trace.SourceLinkId, trace.Mode, trace.Direction, trace.EffectiveTakeoff,
                    landing, corrected, walk, corrected ? Fix03PatchId : string.Empty));
            }

            return new Sv5JumpPlayerFix03Geometry(materialized, cells.Values, links, diagnostics);
        }

        private static Sv5JumpPlayerPatchOperation Op(
            string recipeId,
            string operation,
            int x,
            int y,
            string oldOwnerId,
            string newOwnerId,
            string reason)
        {
            return new Sv5JumpPlayerPatchOperation(recipeId, operation, x, y, "SOLID",
                operation == "CONVERT" ? "TOP_ONLY" : "AIR", oldOwnerId, newOwnerId, reason);
        }

        public static float SchedulerJumpThreshold(
            float velocityX,
            float fixedDeltaTime,
            float collisionSkin,
            float minimumEdgeMargin)
        {
            return Math.Max(Math.Abs(velocityX) * fixedDeltaTime + collisionSkin, minimumEdgeMargin);
        }

        public static bool IsSchedulerWalkableSupport(
            string supportCollision,
            string bodyCollision,
            string headCollision)
        {
            return IsSupportCollision(supportCollision) &&
                !IsSolidBlockerCollision(bodyCollision) && !IsSolidBlockerCollision(headCollision);
        }

        public static bool IsSupportCollision(string collision)
        {
            return collision == "SOLID" || collision == "TOP_ONLY";
        }

        public static bool IsSolidBlockerCollision(string collision)
        {
            return collision == "SOLID";
        }

        public static bool IsGrabSurfaceCollision(string collision)
        {
            return collision == "SOLID";
        }

        public static bool UsesTerminalSolidGrabApproach(
            Sv5JumpPlayerEffectiveLink link,
            Sv5JumpPlayerFix03Geometry geometry)
        {
            if (link == null || geometry == null || link.Mode != Sv5JumpMode.JumpGrab)
                return false;

            Sv5JumpPoint terminalSupport = new Sv5JumpPoint(link.Landing.X, link.Landing.Y - 1);
            return geometry.ComposedCells.Any(cell => cell.RecipeId == link.RecipeId &&
                cell.Point.Equals(terminalSupport) && IsGrabSurfaceCollision(cell.Collision));
        }

        public static Sv5JumpPlayerGrabApproachSchedule CalculateGrabApproachSchedule(
            Sv5JumpPlayerEffectiveLink link,
            float targetNearFaceX,
            float supportBoundsMinimumX,
            float supportBoundsMaximumX,
            float actualCapsuleHalfWidth,
            float physicsContactOffset,
            float collisionSkin,
            float grabProbeDistance,
            float jumpVelocity,
            float riseGravity,
            float horizontalSpeed,
            float groundAcceleration,
            float fixedDeltaTime)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));
            if (actualCapsuleHalfWidth <= 0f || physicsContactOffset < 0f || collisionSkin < 0f ||
                grabProbeDistance <= 0f || jumpVelocity <= 0f || riseGravity <= 0f ||
                horizontalSpeed <= 0f || groundAcceleration <= 0f || fixedDeltaTime <= 0f ||
                supportBoundsMaximumX <= supportBoundsMinimumX)
                throw new ArgumentOutOfRangeException(nameof(actualCapsuleHalfWidth));

            float direction = link.Direction == Sv5JumpDirection.LeftToRight ? 1f : -1f;
            float safeSupportMinimumX = supportBoundsMinimumX + actualCapsuleHalfWidth +
                physicsContactOffset + collisionSkin;
            float safeSupportMaximumX = supportBoundsMaximumX - actualCapsuleHalfWidth -
                physicsContactOffset - collisionSkin;
            if (safeSupportMinimumX > safeSupportMaximumX)
                throw new InvalidOperationException("TAKEOFF_SUPPORT_TOO_NARROW_FOR_ACTUAL_CAPSULE");

            float probeEntryBodyX = targetNearFaceX - direction *
                (actualCapsuleHalfWidth + collisionSkin + grabProbeDistance);
            float expectedProbeEntrySeconds = jumpVelocity / riseGravity + fixedDeltaTime * 2f;
            float launchBodyX = probeEntryBodyX - direction *
                horizontalSpeed * expectedProbeEntrySeconds;
            if (launchBodyX < safeSupportMinimumX || launchBodyX > safeSupportMaximumX)
                throw new InvalidOperationException("GRAB_APPROACH_LAUNCH_OUTSIDE_TAKEOFF_SUPPORT");

            float runUpDistance = horizontalSpeed * horizontalSpeed /
                (2f * groundAcceleration) + horizontalSpeed * fixedDeltaTime * 2f;
            float stagingBodyX = launchBodyX - direction * runUpDistance;
            stagingBodyX = Math.Max(safeSupportMinimumX,
                Math.Min(safeSupportMaximumX, stagingBodyX));
            if (direction * (launchBodyX - stagingBodyX) + 0.0001f < runUpDistance)
                throw new InvalidOperationException("GRAB_APPROACH_RUNUP_OUTSIDE_TAKEOFF_SUPPORT");

            float expectedVelocityY = jumpVelocity - riseGravity * expectedProbeEntrySeconds;
            return new Sv5JumpPlayerGrabApproachSchedule(targetNearFaceX, probeEntryBodyX,
                launchBodyX, stagingBodyX, expectedProbeEntrySeconds, expectedVelocityY,
                safeSupportMinimumX, safeSupportMaximumX);
        }

        public static bool ShouldSubmitScheduledJump(
            bool grounded,
            float supportEdgeDistance,
            float jumpThreshold)
        {
            return grounded && supportEdgeDistance >= 0f && jumpThreshold >= 0f &&
                supportEdgeDistance <= jumpThreshold + 0.0001f;
        }

        public static Sv5JumpPlayerProof Validate(
            string inputRecoveryDigest,
            IEnumerable<Sv5JumpPlayerCase> cases,
            bool playerVerifiedClaim,
            bool jumpRecipeReady = true,
            bool composedGeometryReady = true,
            bool sweptClearanceReady = true,
            bool recoveryReady = true,
            int deterministicRepeatCount = 2,
            int deterministicTerminalDeltaSteps = 0)
        {
            var materialized = (cases ?? Array.Empty<Sv5JumpPlayerCase>()).ToArray();
            var errors = new List<string>();
            errors.AddRange(CreateFix03Geometry().Diagnostics);
            if (!string.Equals(inputRecoveryDigest, InputRecoveryDigest, StringComparison.Ordinal))
                errors.Add(Sv5JumpPlayerDiagnostic.RecoveryIdentity);

            var nonNull = materialized.Where(value => value != null).ToArray();
            if (nonNull.Length != materialized.Length || nonNull.Select(value => value.CaseId).Distinct().Count() != nonNull.Length ||
                nonNull.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.MainLink) != RequiredMainCases ||
                nonNull.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.FullRoute) != RequiredFullRouteCases ||
                nonNull.Count(value => value.CaseKind == Sv5JumpPlayerCaseKind.Recovery) != RequiredRecoveryCases)
                errors.Add(Sv5JumpPlayerDiagnostic.CaseSet);

            var clearance = Sv5JumpClearance.CreateCanonicalLocalProof();
            var recovery = Sv5JumpRecovery.CreateCanonicalLocalProof();
            var links = clearance.Links.ToDictionary(value => Key(value.Trace.RecipeId, value.Trace.Order));
            var routes = recovery.Routes.ToDictionary(value => Key(value.RecipeId, value.FailedLinkOrder));

            foreach (Sv5JumpPlayerCase value in nonNull)
            {
                ValidateCase(value, links, routes, errors);
            }

            if (deterministicRepeatCount != 2 || deterministicTerminalDeltaSteps < 0 ||
                deterministicTerminalDeltaSteps > 2)
                errors.Add(Sv5JumpPlayerDiagnostic.Outcome + ":DETERMINISM");
            if ((!jumpRecipeReady || !composedGeometryReady || !sweptClearanceReady || !recoveryReady ||
                 errors.Count != 0) && playerVerifiedClaim)
                errors.Add(Sv5JumpPlayerDiagnostic.FalseReadiness);

            return new Sv5JumpPlayerProof(nonNull, errors, jumpRecipeReady, composedGeometryReady,
                sweptClearanceReady, recoveryReady, playerVerifiedClaim, deterministicRepeatCount,
                deterministicTerminalDeltaSteps);
        }

        public static IReadOnlyList<Sv5JumpPlayerBindingFile> CanonicalPlayerBindings()
        {
            return Array.AsReadOnly(new[]
            {
                new Sv5JumpPlayerBindingFile(PlayerPrefabPath,
                    "db0e6f93fa441de2f9ed1bf76c1309aa781f6627", "760a418c951f904ec982e227fae1319d0df3d99daf7229e0e45636c6f4614025", 6961),
                new Sv5JumpPlayerBindingFile("Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs",
                    "8eda20b5a2b1e854cf13c062adea411ca8798411", "df58bb6edca4179cefb3d149baf032f03724f2aac4f5afafb8cc10a66b40a836", 33883),
                new Sv5JumpPlayerBindingFile("Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementSettings.cs",
                    "0af95f89b5d549aafc6a7d96737002485fe0b942", "e719bda4fe9514a12dfa923ebe3ea0c2b5af6c050899c27a208500dc14c09c03", 8327),
                new Sv5JumpPlayerBindingFile("Assets/_Game/Live/Runtime/Movement/CharacterLiveGrabSurface.cs",
                    "67ecf54917e594731082e0a5a67ec15546c8eb09", "2063a45e59fcaea7a3c8f2746da6991d54c7132a4e67be78d0e1dd77bbd5ed0e", 1596),
                new Sv5JumpPlayerBindingFile("Assets/_Game/Live/Runtime/Movement/CharacterLiveOneWayPlatform.cs",
                    "a483f3959c86134a5b6aaf11049e23c6e3b20f7d", "7aa3789330ed7c4bb2c749cef81b3c2a1c45782d59005a8055ff5bee4caa5c77", 1803),
                new Sv5JumpPlayerBindingFile("Assets/_Game/Character/Runtime/Input/CharacterInputSnapshot.cs",
                    "309f522f8a7d54403aaec482e4c0e58624e28e56", "6df246bd74fccf394c72ef590db04013b70a4ce568adb1dbd30788387aa30e0f", 4832),
            });
        }

        public static Sv5JumpPlayerMeasurements CanonicalMeasurements()
        {
            return new Sv5JumpPlayerMeasurements(0.02f, 0.72f, 0.9f, 7.2f, 5.5f, 2.8f, 0.35f, 0.45f, 2);
        }

        public static string CaseKindName(Sv5JumpPlayerCaseKind value)
        {
            switch (value)
            {
                case Sv5JumpPlayerCaseKind.MainLink: return "MAIN_LINK";
                case Sv5JumpPlayerCaseKind.FullRoute: return "FULL_ROUTE";
                case Sv5JumpPlayerCaseKind.Recovery: return "RECOVERY";
                default: return "INVALID";
            }
        }

        internal static string Hash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)))
                    .Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void ValidateCase(
            Sv5JumpPlayerCase value,
            IReadOnlyDictionary<string, Sv5JumpClearanceLinkResult> links,
            IReadOnlyDictionary<string, Sv5JumpRecoveryRoute> routes,
            ICollection<string> errors)
        {
            string suffix = ":" + value.CaseId;
            if (!value.Passed) errors.Add(Sv5JumpPlayerDiagnostic.Outcome + suffix);
            if (!value.ActualPlayer) errors.Add(Sv5JumpPlayerDiagnostic.FakePlayer + suffix);
            if (!value.ActualDriver) errors.Add(Sv5JumpPlayerDiagnostic.MissingDriver + suffix);
            if (!value.ActualRigidbody2D) errors.Add(Sv5JumpPlayerDiagnostic.MissingBody + suffix);
            if (!value.ActualCollider2D) errors.Add(Sv5JumpPlayerDiagnostic.MissingCollider + suffix);
            if (value.TeleportsAfterStart != 0) errors.Add(Sv5JumpPlayerDiagnostic.PostStartTeleport + suffix);
            if (value.UsedItem) errors.Add(Sv5JumpPlayerDiagnostic.ItemUse + suffix);
            if (value.ReverseRequired) errors.Add(Sv5JumpPlayerDiagnostic.ReverseRequired + suffix);
            if (value.MeasuredRiseCells > 2.0001f) errors.Add(Sv5JumpPlayerDiagnostic.RiseCap + suffix);
            if (value.GrabbedPlatform) errors.Add(Sv5JumpPlayerDiagnostic.PlatformGrab + suffix);

            if (value.Trace.Count == 0 || value.Trace.Where(sample => sample != null)
                    .Select(sample => sample.Step).SequenceEqual(Enumerable.Range(0, value.Trace.Count)) == false)
                errors.Add(Sv5JumpPlayerDiagnostic.TraceOrder + suffix);
            if (value.Trace.Any(sample => sample == null || !sample.ActualComponentSample || sample.ReusedLogicalTrace))
                errors.Add(Sv5JumpPlayerDiagnostic.LogicalTraceReuse + suffix);
            var events = new HashSet<string>(value.Trace.Where(sample => sample != null).Select(sample => sample.Event));
            if (!events.Contains("START") || !events.Contains("PASS_TARGET"))
                errors.Add(Sv5JumpPlayerDiagnostic.TraceBoundary + suffix);

            string key = Key(value.RecipeId, value.MainLinkOrder);
            if (value.CaseKind == Sv5JumpPlayerCaseKind.MainLink)
            {
                Sv5JumpClearanceLinkResult link;
                if (!links.TryGetValue(key, out link) || value.SourceId != link.Trace.SourceLinkId)
                    errors.Add(Sv5JumpPlayerDiagnostic.CaseSet + ":MAIN_BINDING" + suffix);
                bool grabExpected = link != null && link.Trace.Mode == Sv5JumpMode.JumpGrab;
                if (grabExpected != value.UsedGrab || (grabExpected &&
                    (!events.Contains("GRAB_ENTER") || !events.Contains("GRAB_HOLD") ||
                     !events.Contains("GRAB_SPACE_EXIT") || !events.Contains("GRAB_EXIT"))))
                    errors.Add(Sv5JumpPlayerDiagnostic.MissingGrabEvents + suffix);
                if (grabExpected)
                {
                    Sv5JumpPlayerTraceSample exit = value.Trace.FirstOrDefault(sample =>
                        sample != null && sample.Event == "GRAB_SPACE_EXIT");
                    if (exit == null || exit.VelocityY <= 0f)
                        errors.Add(GrabExitNoVerticalImpulse + suffix);
                }
            }
            else if (value.CaseKind == Sv5JumpPlayerCaseKind.Recovery)
            {
                Sv5JumpRecoveryRoute route;
                if (!routes.TryGetValue(key, out route) || value.CheckpointLinkOrder != route.CheckpointLinkOrder)
                    errors.Add(Sv5JumpPlayerDiagnostic.CaseSet + ":RECOVERY_BINDING" + suffix);
                if (value.CheckpointLinkOrder > value.MainLinkOrder)
                    errors.Add(Sv5JumpPlayerDiagnostic.LaterCheckpoint + suffix);
                if (!value.FirstCatchMatched)
                    errors.Add(Sv5JumpPlayerDiagnostic.WrongFirstCatch + suffix);
                if (!events.Contains("RECOVERY_CATCH") || !events.Contains("REJOIN"))
                    errors.Add(Sv5JumpPlayerDiagnostic.RecoveryEvents + suffix);
            }
        }

        private static string Key(string recipeId, int order)
        {
            return (recipeId ?? string.Empty) + "|" + order;
        }

        private static string CellKey(string recipeId, Sv5JumpPoint point)
        {
            return (recipeId ?? string.Empty) + "|" + point.X + "|" + point.Y;
        }
    }
}
