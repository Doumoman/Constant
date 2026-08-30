using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorPlannerAttemptTraceBuilder
    {
        public static SectorPlannerAttemptTrace Build(
            SectorPlannerRetryFailure failure,
            int attemptOrdinal,
            int nodeOrdinal)
        {
            if (failure == null)
            {
                return new SectorPlannerAttemptTrace(
                    attemptOrdinal, nodeOrdinal, null, SectorPlannerRetryStage.Abort,
                    SectorPlannerRetryDecisionKind.AbortUnownedFailure,
                    "Missing typed failure authority.");
            }

            var stage = StageFor(failure);
            return new SectorPlannerAttemptTrace(
                attemptOrdinal,
                nodeOrdinal,
                failure,
                stage,
                DecisionFor(stage, failure.Owner),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}:{2}->{3}",
                    failure.Owner,
                    failure.Code,
                    failure.Subject,
                    stage));
        }

        private static SectorPlannerRetryStage StageFor(SectorPlannerRetryFailure failure)
        {
            switch (failure.Owner)
            {
                case SectorPlannerRetryFailureOwner.PatternSelection:
                    return SectorPlannerRetryStage.PatternCandidate;
                case SectorPlannerRetryFailureOwner.PatternApplication:
                    if (Contains(failure.Code, "TRANSFORM") || Contains(failure.Code, "PROTECTED"))
                    {
                        return failure.RecoverySequenceOrdinal == 0
                            ? SectorPlannerRetryStage.PatternTransform
                            : SectorPlannerRetryStage.PatternCandidate;
                    }
                    return failure.RecoverySequenceOrdinal == 0
                        ? SectorPlannerRetryStage.PatternCandidate
                        : SectorPlannerRetryStage.PatternTransform;
                case SectorPlannerRetryFailureOwner.PatternRender:
                    return failure.RecoverySequenceOrdinal == 0
                        ? SectorPlannerRetryStage.PatternCandidate
                        : SectorPlannerRetryStage.PatternTransform;
                case SectorPlannerRetryFailureOwner.ClusterPlacement:
                    return Contains(failure.Code, "FOOTPRINT") || Contains(failure.Code, "OVERLAP")
                        ? SectorPlannerRetryStage.ClusterFootprint
                        : SectorPlannerRetryStage.ClusterVariant;
                case SectorPlannerRetryFailureOwner.SpineEnvelope:
                    if (failure.RecoverySequenceOrdinal == 0) return SectorPlannerRetryStage.ClusterVariant;
                    if (failure.RecoverySequenceOrdinal == 1) return SectorPlannerRetryStage.ClusterFootprint;
                    return SectorPlannerRetryStage.Abort;
                case SectorPlannerRetryFailureOwner.CanvasOwnership:
                    return failure.RecoverySequenceOrdinal == 0
                        ? SectorPlannerRetryStage.PatternCandidate
                        : SectorPlannerRetryStage.ClusterVariant;
                case SectorPlannerRetryFailureOwner.ForbiddenFallback:
                case SectorPlannerRetryFailureOwner.Input:
                case SectorPlannerRetryFailureOwner.Anchor:
                case SectorPlannerRetryFailureOwner.QuietActivityEvent:
                case SectorPlannerRetryFailureOwner.RngPolicy:
                case SectorPlannerRetryFailureOwner.Unknown:
                default:
                    return SectorPlannerRetryStage.Abort;
            }
        }

        private static SectorPlannerRetryDecisionKind DecisionFor(
            SectorPlannerRetryStage stage,
            SectorPlannerRetryFailureOwner owner)
        {
            switch (stage)
            {
                case SectorPlannerRetryStage.PatternCandidate:
                    return SectorPlannerRetryDecisionKind.RetryPatternCandidate;
                case SectorPlannerRetryStage.PatternTransform:
                    return SectorPlannerRetryDecisionKind.RetryPatternTransform;
                case SectorPlannerRetryStage.ClusterVariant:
                    return SectorPlannerRetryDecisionKind.RetryClusterVariant;
                case SectorPlannerRetryStage.ClusterFootprint:
                    return SectorPlannerRetryDecisionKind.RetryClusterFootprint;
                default:
                    if (owner == SectorPlannerRetryFailureOwner.ForbiddenFallback)
                        return SectorPlannerRetryDecisionKind.AbortForbiddenFallback;
                    if (owner == SectorPlannerRetryFailureOwner.RngPolicy)
                        return SectorPlannerRetryDecisionKind.AbortNonDeterministicTrace;
                    if (owner == SectorPlannerRetryFailureOwner.SpineEnvelope)
                        return SectorPlannerRetryDecisionKind.AbortCapReached;
                    return SectorPlannerRetryDecisionKind.AbortUnownedFailure;
            }
        }

        private static bool Contains(string value, string token)
        {
            return value != null && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public static class SectorPlannerRetryExecutor
    {
        public const string ReferencePublicationLabel = "MAP14_08_REFERENCE_RETRY_PLAN";

        public static SectorPlannerRetryBuildResult Execute(SectorPlannerRetryBuildRequest request)
        {
            var errors = new List<SectorPlannerRetryError>();
            var attempts = new List<SectorPlannerAttemptTrace>();
            var nodes = new List<SectorPlannerRetryNodeTrace>();
            var terminal = SectorPlannerRetryDecisionKind.AbortUnownedFailure;

            ValidateRequest(request, errors);
            if (errors.Count > 0)
            {
                terminal = TerminalFor(errors);
                return Failure(request, attempts, nodes, terminal, errors);
            }

            if (request.AttemptInputs.Count == 0)
            {
                terminal = SectorPlannerRetryDecisionKind.AcceptFirstPass;
                var firstPass = CreatePlan(request, attempts, nodes, terminal);
                return Success(request, firstPass, attempts, nodes, terminal);
            }

            var countsByStage = new Dictionary<SectorPlannerRetryStage, int>();
            var localAttempts = new HashSet<int>();
            foreach (var input in request.AttemptInputs)
            {
                var attemptOrdinal = checked(request.InitialAttemptOrdinal + input.AttemptOrdinal);
                var trace = SectorPlannerAttemptTraceBuilder.Build(input.Failure, attemptOrdinal, input.NodeOrdinal);
                attempts.Add(trace);

                if (trace.NextStage == SectorPlannerRetryStage.Abort)
                {
                    AddAbortError(trace, errors);
                    terminal = trace.Decision;
                    return Failure(request, attempts, nodes, terminal, errors);
                }

                var attemptedAtStage = Count(countsByStage, trace.NextStage) + 1;
                if (attemptedAtStage > request.RetryPolicy.Limits.ForStage(trace.NextStage))
                {
                    Add(errors, SectorPlannerRetryErrorCode.RetryCapExceeded,
                        trace.NextStage.ToString(),
                        string.Format(CultureInfo.InvariantCulture, "cap={0};attempted={1}",
                            request.RetryPolicy.Limits.ForStage(trace.NextStage), attemptedAtStage));
                    terminal = SectorPlannerRetryDecisionKind.AbortCapReached;
                    return Failure(request, attempts, nodes, terminal, errors);
                }

                if (nodes.Count + 1 > request.RetryPolicy.Limits.MaxRetryNodesPerSector)
                {
                    Add(errors, SectorPlannerRetryErrorCode.NodeCapExceeded, "retryNodes",
                        string.Format(CultureInfo.InvariantCulture, "cap={0};attempted={1}",
                            request.RetryPolicy.Limits.MaxRetryNodesPerSector, nodes.Count + 1));
                    terminal = SectorPlannerRetryDecisionKind.AbortCapReached;
                    return Failure(request, attempts, nodes, terminal, errors);
                }

                if (!localAttempts.Contains(attemptOrdinal) &&
                    localAttempts.Count + 1 > request.RetryPolicy.Limits.MaxTotalLocalAttemptsPerSector)
                {
                    Add(errors, SectorPlannerRetryErrorCode.RetryCapExceeded, "localAttempts",
                        string.Format(CultureInfo.InvariantCulture, "cap={0};attempted={1}",
                            request.RetryPolicy.Limits.MaxTotalLocalAttemptsPerSector, localAttempts.Count + 1));
                    terminal = SectorPlannerRetryDecisionKind.AbortCapReached;
                    return Failure(request, attempts, nodes, terminal, errors);
                }

                if (input.CandidateIds.Count == 0)
                {
                    Add(errors, SectorPlannerRetryErrorCode.UnretryableFailure,
                        input.Failure.Subject, "Retry stage has no compatible candidates.");
                    terminal = SectorPlannerRetryDecisionKind.AbortUnownedFailure;
                    return Failure(request, attempts, nodes, terminal, errors);
                }

                SectorPlannerRngTrace rngTrace;
                try
                {
                    rngTrace = Draw(request, trace, input.CandidateIds);
                }
                catch (Exception exception)
                {
                    Add(errors, SectorPlannerRetryErrorCode.NonDeterministicRngTrace,
                        WorldGenerationRngStreams.SectorRecipeStreamId,
                        exception.GetType().Name + ":" + exception.Message);
                    terminal = SectorPlannerRetryDecisionKind.AbortNonDeterministicTrace;
                    return Failure(request, attempts, nodes, terminal, errors);
                }

                countsByStage[trace.NextStage] = attemptedAtStage;
                localAttempts.Add(attemptOrdinal);
                nodes.Add(new SectorPlannerRetryNodeTrace(
                    trace,
                    rngTrace,
                    rngTrace.ChosenCandidateId,
                    input.RecoverySucceeded,
                    input.RecoverySucceeded
                        ? SectorPlannerRetryDecisionKind.AcceptRecovered
                        : trace.Decision));
            }

            if (!request.AttemptInputs[request.AttemptInputs.Count - 1].RecoverySucceeded)
            {
                Add(errors, SectorPlannerRetryErrorCode.UnretryableFailure,
                    "terminal", "The bounded local retry sequence ended without recovery.");
                terminal = SectorPlannerRetryDecisionKind.AbortUnownedFailure;
                return Failure(request, attempts, nodes, terminal, errors);
            }

            terminal = SectorPlannerRetryDecisionKind.AcceptRecovered;
            var plan = CreatePlan(request, attempts, nodes, terminal);
            return Success(request, plan, attempts, nodes, terminal);
        }

        private static void ValidateRequest(
            SectorPlannerRetryBuildRequest request,
            ICollection<SectorPlannerRetryError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorPlannerRetryErrorCode.MissingInput, "request", "Retry request is required.");
                return;
            }

            if (request.OwnershipPlan == null)
            {
                Add(errors, SectorPlannerRetryErrorCode.MissingOwnershipPlan, "ownershipPlan", "MAP14_07 ownership plan is required.");
            }
            else
            {
                if (!request.OwnershipPlan.Map14_08HandoffReady)
                    Add(errors, SectorPlannerRetryErrorCode.MissingOwnershipPlan, "ownershipPlan", "MAP14_07 handoff is not ready.");
                if (!request.OwnershipPlan.Request.Input.TryGetSector(request.SectorCoordinate, out _))
                    Add(errors, SectorPlannerRetryErrorCode.SectorMismatch, request.SectorCoordinate.ToString(), "Sector is not present in MAP14_07 input.");
            }

            if (request.RetryPolicy == null)
            {
                Add(errors, SectorPlannerRetryErrorCode.MissingRetryPolicy, "retryPolicy", "Retry policy is required.");
            }
            else
            {
                if (request.RetryPolicy.Limits == null || !request.RetryPolicy.Limits.AllPositive)
                    Add(errors, SectorPlannerRetryErrorCode.InvalidRetryLimit, "retryLimits", "All retry limits must be positive.");
                if (!request.RetryPolicy.HasCanonicalOrder)
                    Add(errors, SectorPlannerRetryErrorCode.InvalidRetryOrder, "recoveryOrder", "Recovery order must be pattern, transform, variant, footprint, sector cap, abort.");
                if (!IsLowerSha(request.RetryPolicy.CanonicalDigest))
                    Add(errors, SectorPlannerRetryErrorCode.NonCanonicalPublication, "retryPolicy", "Policy digest must be lowercase SHA-256.");
            }

            if (request.RngAuthority == null)
                Add(errors, SectorPlannerRetryErrorCode.MissingRngAuthority, "rngAuthority", "Deterministic RNG authority is required.");
            if (request.InitialAttemptOrdinal < 0)
                Add(errors, SectorPlannerRetryErrorCode.NegativeAttemptOrdinal, "initialAttemptOrdinal", request.InitialAttemptOrdinal.ToString(CultureInfo.InvariantCulture));
            if (!string.Equals(request.PublicationLabel, ReferencePublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorPlannerRetryErrorCode.NonCanonicalPublication, "publicationLabel", request.PublicationLabel);

            var nodeKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var input in request.AttemptInputs)
            {
                if (input.AttemptOrdinal < 0)
                    Add(errors, SectorPlannerRetryErrorCode.NegativeAttemptOrdinal, "attemptInput", input.AttemptOrdinal.ToString(CultureInfo.InvariantCulture));
                if (input.NodeOrdinal < 0)
                    Add(errors, SectorPlannerRetryErrorCode.DuplicateNodeTrace, "nodeOrdinal", input.NodeOrdinal.ToString(CultureInfo.InvariantCulture));
                if (input.Failure == null)
                    Add(errors, SectorPlannerRetryErrorCode.MissingInput, "failure", "Typed failure is required.");
                else if (input.Failure.RecoverySequenceOrdinal < 0)
                    Add(errors, SectorPlannerRetryErrorCode.NegativeAttemptOrdinal, input.Failure.Subject, "Recovery sequence ordinal cannot be negative.");
                var key = Key(input.AttemptOrdinal, input.NodeOrdinal);
                if (!nodeKeys.Add(key))
                    Add(errors, SectorPlannerRetryErrorCode.DuplicateNodeTrace, key, "Attempt/node pair occurs more than once.");
            }

            var attemptTraceKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var trace in request.SourceAttemptTraces)
            {
                if (trace.AttemptOrdinal < 0)
                    Add(errors, SectorPlannerRetryErrorCode.NegativeAttemptOrdinal, "sourceAttemptTrace", trace.AttemptOrdinal.ToString(CultureInfo.InvariantCulture));
                var key = Key(trace.AttemptOrdinal, trace.NodeOrdinal);
                if (!attemptTraceKeys.Add(key))
                    Add(errors, SectorPlannerRetryErrorCode.DuplicateAttemptTrace, key, "Published attempt trace is duplicated.");
            }

            ValidateSourceRngTraces(request.SourceRngTraces, errors);
            ValidateMutationClaims(request, errors);
        }

        private static void ValidateSourceRngTraces(
            IEnumerable<SectorPlannerRngTrace> source,
            ICollection<SectorPlannerRetryError> errors)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var trace in source)
            {
                var key = Key(trace.AttemptOrdinal, trace.NodeOrdinal) + "|" + trace.PassScope;
                if (!keys.Add(key))
                    Add(errors, SectorPlannerRetryErrorCode.DuplicateNodeTrace, key, "Published RNG node trace is duplicated.");
                if (trace.AttemptOrdinal < 0)
                    Add(errors, SectorPlannerRetryErrorCode.NegativeAttemptOrdinal, key, "RNG trace attempt ordinal cannot be negative.");
                if (!string.Equals(trace.StreamId, WorldGenerationRngStreams.SectorRecipeStreamId, StringComparison.Ordinal))
                    Add(errors, SectorPlannerRetryErrorCode.RngStreamMismatch, key, trace.StreamId);
                if (!string.Equals(trace.ScopeLabel, ScopeLabel(trace.PassScope), StringComparison.Ordinal))
                    Add(errors, SectorPlannerRetryErrorCode.RngScopeMismatch, key, trace.ScopeLabel);
                if (trace.DrawOrdinalAfter < trace.DrawOrdinalBefore ||
                    trace.DrawOrdinalAfter - trace.DrawOrdinalBefore != trace.DrawCount)
                    Add(errors, SectorPlannerRetryErrorCode.RngDrawMismatch, key,
                        string.Format(CultureInfo.InvariantCulture, "before={0};after={1};count={2}",
                            trace.DrawOrdinalBefore, trace.DrawOrdinalAfter, trace.DrawCount));
                if (!IsLowerSha(trace.CanonicalDigest) ||
                    !string.Equals(trace.CanonicalDigest, SectorPlannerRetryCanonicalDigest.ComputeRngTrace(trace), StringComparison.Ordinal))
                    Add(errors, SectorPlannerRetryErrorCode.NonDeterministicRngTrace, key, "RNG trace digest is not canonical.");
            }
        }

        private static void ValidateMutationClaims(
            SectorPlannerRetryBuildRequest request,
            ICollection<SectorPlannerRetryError> errors)
        {
            if (request.UpstreamMutationClaim)
                Add(errors, SectorPlannerRetryErrorCode.UpstreamMutationClaim, "upstream", "Upstream mutation claim is forbidden.");
            if (request.PatternMutationClaim)
                Add(errors, SectorPlannerRetryErrorCode.PatternMutationClaim, "pattern", "Pattern mutation claim is forbidden.");
            if (request.ClusterMutationClaim)
                Add(errors, SectorPlannerRetryErrorCode.ClusterMutationClaim, "cluster", "Cluster mutation claim is forbidden.");
            if (request.FootprintMutationClaim)
                Add(errors, SectorPlannerRetryErrorCode.FootprintMutationClaim, "footprint", "Footprint mutation claim is forbidden.");
            if (request.OwnershipMutationClaim)
                Add(errors, SectorPlannerRetryErrorCode.OwnershipMutationClaim, "ownership", "Ownership mutation claim is forbidden.");
            CountError(errors, request.FallbackCorridorCarveCount, SectorPlannerRetryErrorCode.SyntheticCorridorAttempt, "fallbackCorridorCarve");
            CountError(errors, request.ValidationRelaxationCount, SectorPlannerRetryErrorCode.ValidationRelaxationAttempt, "validationRelaxation");
            CountError(errors, request.WholeSectorRerandomCount, SectorPlannerRetryErrorCode.WholeSectorRerandomAttempt, "wholeSectorRerandom");
            CountError(errors, request.WholeWorldRerandomCount, SectorPlannerRetryErrorCode.WholeWorldRerandomAttempt, "wholeWorldRerandom");
            CountError(errors, request.FixedAnchorMutationCount, SectorPlannerRetryErrorCode.UpstreamMutationClaim, "fixedAnchorMutation");
            CountError(errors, request.BoundarySocketMutationCount, SectorPlannerRetryErrorCode.SocketMutationAttempt, "boundarySocketMutation");
            CountError(errors, request.SpecialReservationMutationCount, SectorPlannerRetryErrorCode.SpecialReservationMutationAttempt, "specialReservationMutation");
            CountError(errors, request.ProtectedMaskRelaxationCount, SectorPlannerRetryErrorCode.ProtectedMaskRelaxationAttempt, "protectedMaskRelaxation");
            CountError(errors, request.TilemapWriteCount, SectorPlannerRetryErrorCode.TileMutationClaim, "tilemapWrite");
            CountError(errors, request.SceneMutationCount, SectorPlannerRetryErrorCode.SceneMutationClaim, "sceneMutation");
            CountError(errors, request.PrefabMutationCount, SectorPlannerRetryErrorCode.SceneMutationClaim, "prefabMutation");
            CountError(errors, request.GameObjectMutationCount, SectorPlannerRetryErrorCode.SceneMutationClaim, "gameObjectMutation");
            CountError(errors, request.ActivityRuntimeSpawnCount, SectorPlannerRetryErrorCode.SceneMutationClaim, "activityRuntimeSpawn");
            CountError(errors, request.EventRuntimeSpawnCount, SectorPlannerRetryErrorCode.SceneMutationClaim, "eventRuntimeSpawn");
            CountError(errors, request.GameplayExecutionCount, SectorPlannerRetryErrorCode.SceneMutationClaim, "gameplayExecution");
            CountError(errors, request.DebugExportWriteCount, SectorPlannerRetryErrorCode.SceneMutationClaim, "debugExportWrite");
        }

        private static SectorPlannerRngTrace Draw(
            SectorPlannerRetryBuildRequest request,
            SectorPlannerAttemptTrace trace,
            IReadOnlyList<string> candidates)
        {
            var stream = request.RngAuthority.Create(
                WorldGenerationRngStreams.SectorRecipeStreamId,
                request.WorldSeed,
                RngStreamScope.Sector(request.SectorCoordinate, trace.AttemptOrdinal));
            var before = stream.DrawCount;
            var ticket = stream.NextInt(candidates.Count);
            var after = stream.DrawCount;
            var chosen = candidates[ticket];
            var passScope = PassScope(trace.NextStage);
            var initialDigest = SectorPlannerRetryCanonicalDigest.Hash(
                "INITIAL|" + stream.InitialState.ToString("x16", CultureInfo.InvariantCulture));
            var finalDigest = SectorPlannerRetryCanonicalDigest.Hash(string.Format(
                CultureInfo.InvariantCulture,
                "FINAL|{0:x16}|{1}|{2}|{3}|{4}",
                stream.InitialState, after, ticket, trace.NodeOrdinal, chosen));
            return new SectorPlannerRngTrace(
                WorldGenerationRngStreams.SectorRecipeStreamId,
                passScope,
                ScopeLabel(passScope),
                request.WorldSeed,
                request.SectorCoordinate,
                trace.AttemptOrdinal,
                trace.NodeOrdinal,
                before,
                after,
                after - before,
                ticket,
                candidates.Count,
                chosen,
                initialDigest,
                finalDigest);
        }

        private static SectorPlannerRetryPlan CreatePlan(
            SectorPlannerRetryBuildRequest request,
            IEnumerable<SectorPlannerAttemptTrace> attempts,
            IEnumerable<SectorPlannerRetryNodeTrace> nodes,
            SectorPlannerRetryDecisionKind terminal)
        {
            var digest = SectorPlannerRetryCanonicalDigest.ComputePlan(request, attempts, nodes, terminal);
            return new SectorPlannerRetryPlan(request, attempts, nodes, terminal, digest);
        }

        private static SectorPlannerRetryBuildResult Success(
            SectorPlannerRetryBuildRequest request,
            SectorPlannerRetryPlan plan,
            IEnumerable<SectorPlannerAttemptTrace> attempts,
            IEnumerable<SectorPlannerRetryNodeTrace> nodes,
            SectorPlannerRetryDecisionKind terminal)
        {
            return new SectorPlannerRetryBuildResult(
                request, plan, attempts, nodes, terminal, Array.Empty<SectorPlannerRetryError>());
        }

        private static SectorPlannerRetryBuildResult Failure(
            SectorPlannerRetryBuildRequest request,
            IEnumerable<SectorPlannerAttemptTrace> attempts,
            IEnumerable<SectorPlannerRetryNodeTrace> nodes,
            SectorPlannerRetryDecisionKind terminal,
            IEnumerable<SectorPlannerRetryError> errors)
        {
            return new SectorPlannerRetryBuildResult(request, null, attempts, nodes, terminal, errors);
        }

        private static void AddAbortError(
            SectorPlannerAttemptTrace trace,
            ICollection<SectorPlannerRetryError> errors)
        {
            var failure = trace.Failure;
            if (failure == null)
            {
                Add(errors, SectorPlannerRetryErrorCode.MissingInput, "failure", trace.Reason);
                return;
            }
            if (failure.Owner == SectorPlannerRetryFailureOwner.ForbiddenFallback)
            {
                Add(errors, failure.ForbiddenErrorCode, failure.Subject, failure.Detail);
                Add(errors, SectorPlannerRetryErrorCode.ForbiddenFallbackAttempt, failure.Subject, failure.Code);
            }
            else if (failure.Owner == SectorPlannerRetryFailureOwner.Unknown)
            {
                Add(errors, SectorPlannerRetryErrorCode.UnknownFailureOwner, failure.Subject, failure.Detail);
            }
            else if (failure.Owner == SectorPlannerRetryFailureOwner.SpineEnvelope)
            {
                Add(errors, SectorPlannerRetryErrorCode.RetryCapExceeded, failure.Subject,
                    "Spine/envelope recovery exhausted cluster variant and footprint stages.");
            }
            else if (failure.Owner == SectorPlannerRetryFailureOwner.RngPolicy)
            {
                Add(errors, SectorPlannerRetryErrorCode.NonDeterministicRngTrace, failure.Subject, failure.Detail);
            }
            else
            {
                Add(errors, SectorPlannerRetryErrorCode.UnretryableFailure, failure.Subject,
                    failure.Owner + ":" + failure.Code + ":" + failure.Detail);
            }
        }

        private static SectorPlannerRetryDecisionKind TerminalFor(IEnumerable<SectorPlannerRetryError> errors)
        {
            var codes = new HashSet<SectorPlannerRetryErrorCode>(errors.Select(value => value.Code));
            if (codes.Any(IsForbidden)) return SectorPlannerRetryDecisionKind.AbortForbiddenFallback;
            if (codes.Contains(SectorPlannerRetryErrorCode.NonDeterministicRngTrace) ||
                codes.Contains(SectorPlannerRetryErrorCode.RngStreamMismatch) ||
                codes.Contains(SectorPlannerRetryErrorCode.RngScopeMismatch) ||
                codes.Contains(SectorPlannerRetryErrorCode.RngDrawMismatch))
                return SectorPlannerRetryDecisionKind.AbortNonDeterministicTrace;
            if (codes.Contains(SectorPlannerRetryErrorCode.RetryCapExceeded) ||
                codes.Contains(SectorPlannerRetryErrorCode.NodeCapExceeded))
                return SectorPlannerRetryDecisionKind.AbortCapReached;
            return SectorPlannerRetryDecisionKind.AbortUnownedFailure;
        }

        private static bool IsForbidden(SectorPlannerRetryErrorCode code)
        {
            return code == SectorPlannerRetryErrorCode.ForbiddenFallbackAttempt ||
                   code == SectorPlannerRetryErrorCode.ValidationRelaxationAttempt ||
                   code == SectorPlannerRetryErrorCode.WholeSectorRerandomAttempt ||
                   code == SectorPlannerRetryErrorCode.WholeWorldRerandomAttempt ||
                   code == SectorPlannerRetryErrorCode.SyntheticCorridorAttempt ||
                   code == SectorPlannerRetryErrorCode.SocketMutationAttempt ||
                   code == SectorPlannerRetryErrorCode.BoundaryMutationAttempt ||
                   code == SectorPlannerRetryErrorCode.SpecialReservationMutationAttempt ||
                   code == SectorPlannerRetryErrorCode.ProtectedMaskRelaxationAttempt ||
                   code == SectorPlannerRetryErrorCode.UpstreamMutationClaim ||
                   code == SectorPlannerRetryErrorCode.PatternMutationClaim ||
                   code == SectorPlannerRetryErrorCode.ClusterMutationClaim ||
                   code == SectorPlannerRetryErrorCode.FootprintMutationClaim ||
                   code == SectorPlannerRetryErrorCode.OwnershipMutationClaim ||
                   code == SectorPlannerRetryErrorCode.TileMutationClaim ||
                   code == SectorPlannerRetryErrorCode.SceneMutationClaim;
        }

        private static SectorPlannerRngPassScope PassScope(SectorPlannerRetryStage stage)
        {
            switch (stage)
            {
                case SectorPlannerRetryStage.PatternCandidate: return SectorPlannerRngPassScope.PatternCandidate;
                case SectorPlannerRetryStage.PatternTransform: return SectorPlannerRngPassScope.PatternTransform;
                case SectorPlannerRetryStage.ClusterVariant: return SectorPlannerRngPassScope.ClusterVariant;
                case SectorPlannerRetryStage.ClusterFootprint: return SectorPlannerRngPassScope.ClusterFootprint;
                default: return SectorPlannerRngPassScope.RetryDecision;
            }
        }

        public static string ScopeLabel(SectorPlannerRngPassScope scope)
        {
            switch (scope)
            {
                case SectorPlannerRngPassScope.SectorPlan: return "MAP14_SECTOR_PLAN";
                case SectorPlannerRngPassScope.PatternCandidate: return "MAP14_PATTERN_CANDIDATE";
                case SectorPlannerRngPassScope.PatternTransform: return "MAP14_PATTERN_TRANSFORM";
                case SectorPlannerRngPassScope.ClusterVariant: return "MAP14_CLUSTER_VARIANT";
                case SectorPlannerRngPassScope.ClusterFootprint: return "MAP14_CLUSTER_FOOTPRINT";
                case SectorPlannerRngPassScope.ActivitySelection: return "MAP14_ACTIVITY_SELECTION";
                case SectorPlannerRngPassScope.EventSelection: return "MAP14_EVENT_SELECTION";
                default: return "MAP14_RETRY_DECISION";
            }
        }

        private static int Count(IReadOnlyDictionary<SectorPlannerRetryStage, int> counts, SectorPlannerRetryStage stage)
        {
            return counts.TryGetValue(stage, out var value) ? value : 0;
        }

        private static string Key(int attemptOrdinal, int nodeOrdinal)
        {
            return attemptOrdinal.ToString(CultureInfo.InvariantCulture) + ":" +
                   nodeOrdinal.ToString(CultureInfo.InvariantCulture);
        }

        private static void CountError(
            ICollection<SectorPlannerRetryError> errors,
            int count,
            SectorPlannerRetryErrorCode code,
            string subject)
        {
            if (count != 0)
                Add(errors, code, subject, count.ToString(CultureInfo.InvariantCulture));
        }

        private static bool IsLowerSha(string value)
        {
            if (value == null || value.Length != 64) return false;
            return value.All(character => (character >= '0' && character <= '9') ||
                                          (character >= 'a' && character <= 'f'));
        }

        private static void Add(
            ICollection<SectorPlannerRetryError> errors,
            SectorPlannerRetryErrorCode code,
            string subject,
            string detail)
        {
            errors.Add(new SectorPlannerRetryError(code, subject, detail));
        }
    }
}
