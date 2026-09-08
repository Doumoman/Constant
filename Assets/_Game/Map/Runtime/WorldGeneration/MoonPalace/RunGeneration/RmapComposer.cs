using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>
    /// RMAP09's deliberately small, deterministic 3x2 composition boundary.
    /// It consumes RMAP07 base cells and RMAP08's declared port/profile data;
    /// it does not create a seeded run, repair terrain, or infer missing ports.
    /// </summary>
    public enum RmapComposerOverlayKind
    {
        Ladder = 0,
    }

    public readonly struct RmapComposerCell : IEquatable<RmapComposerCell>, IComparable<RmapComposerCell>
    {
        public RmapComposerCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool Equals(RmapComposerCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is RmapComposerCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public int CompareTo(RmapComposerCell other) => Y != other.Y ? Y.CompareTo(other.Y) : X.CompareTo(other.X);
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," +
            Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class RmapComposerProposal
    {
        public RmapComposerProposal(int slotX, int slotY, string candidateId, RmapPatternTransform transform)
        {
            SlotX = slotX;
            SlotY = slotY;
            CandidateId = candidateId ?? string.Empty;
            Transform = transform;
        }

        public int SlotX { get; }
        public int SlotY { get; }
        public string CandidateId { get; }
        public RmapPatternTransform Transform { get; }
        public string StableKey => CandidateId + "|" + Transform;
    }

    public sealed class RmapComposerAttemptPlan
    {
        private readonly ReadOnlyCollection<RmapComposerProposal> proposals;

        public RmapComposerAttemptPlan(string attemptId, IEnumerable<RmapComposerProposal> sourceProposals)
        {
            AttemptId = string.IsNullOrWhiteSpace(attemptId) ? "UNNAMED" : attemptId.Trim();
            proposals = new ReadOnlyCollection<RmapComposerProposal>((sourceProposals ??
                Array.Empty<RmapComposerProposal>()).Where(value => value != null)
                .OrderBy(value => value.SlotY).ThenBy(value => value.SlotX).ToList());
        }

        public string AttemptId { get; }
        public IReadOnlyList<RmapComposerProposal> Proposals => proposals;
    }

    public sealed class RmapComposerRequest
    {
        private readonly ReadOnlyCollection<RmapComposerAttemptPlan> attemptPlans;

        public RmapComposerRequest(string seed, string targetChunkId, int maximumAttempts,
            IEnumerable<RmapComposerAttemptPlan> sourceAttemptPlans)
        {
            Seed = string.IsNullOrWhiteSpace(seed) ? "RMAP09_DEFAULT" : seed.Trim();
            TargetChunkId = string.IsNullOrWhiteSpace(targetChunkId) ? string.Empty : targetChunkId.Trim();
            MaximumAttempts = maximumAttempts;
            attemptPlans = new ReadOnlyCollection<RmapComposerAttemptPlan>((sourceAttemptPlans ??
                Array.Empty<RmapComposerAttemptPlan>()).Where(value => value != null).ToList());
        }

        public string Seed { get; }
        public string TargetChunkId { get; }
        public int MaximumAttempts { get; }
        public IReadOnlyList<RmapComposerAttemptPlan> AttemptPlans => attemptPlans;
    }

    public sealed class RmapComposerSelection
    {
        private readonly ReadOnlyCollection<RmapPatternBaseCell> finalCells;

        internal RmapComposerSelection(RmapComposerProposal proposal, RmapPatternCandidate candidate,
            IEnumerable<RmapPatternBaseCell> sourceFinalCells)
        {
            Proposal = proposal ?? throw new ArgumentNullException(nameof(proposal));
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            finalCells = new ReadOnlyCollection<RmapPatternBaseCell>((sourceFinalCells ??
                Array.Empty<RmapPatternBaseCell>()).ToArray());
        }

        public RmapComposerProposal Proposal { get; }
        public RmapPatternCandidate Candidate { get; }
        public int SlotX => Proposal.SlotX;
        public int SlotY => Proposal.SlotY;
        public string CandidateId => Candidate.CandidateId;
        public RmapPatternTransform Transform => Proposal.Transform;
        public IReadOnlyList<RmapPatternBaseCell> FinalCells => finalCells;
        public string FinalCells16 => RmapPatternCatalog.SerializeBaseCells16(finalCells);
    }

    public sealed class RmapComposerOverlayCell
    {
        public RmapComposerOverlayCell(RmapComposerCell cell, RmapComposerOverlayKind kind)
        {
            Cell = cell;
            Kind = kind;
        }

        public RmapComposerCell Cell { get; }
        public RmapComposerOverlayKind Kind { get; }
    }

    public sealed class RmapComposerAttemptTrace
    {
        private readonly ReadOnlyCollection<RmapComposerSelection> selections;
        private readonly ReadOnlyCollection<string> rejections;

        internal RmapComposerAttemptTrace(int attemptNumber, string attemptId,
            IEnumerable<RmapComposerSelection> sourceSelections, IEnumerable<string> sourceRejections)
        {
            AttemptNumber = attemptNumber;
            AttemptId = attemptId ?? string.Empty;
            selections = new ReadOnlyCollection<RmapComposerSelection>((sourceSelections ??
                Array.Empty<RmapComposerSelection>()).OrderBy(value => value.SlotY)
                .ThenBy(value => value.SlotX).ToList());
            rejections = new ReadOnlyCollection<string>((sourceRejections ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).OrderBy(value => value,
                    StringComparer.Ordinal).ToList());
        }

        public int AttemptNumber { get; }
        public string AttemptId { get; }
        public IReadOnlyList<RmapComposerSelection> Selections => selections;
        public IReadOnlyList<string> Rejections => rejections;
        public bool Accepted => rejections.Count == 0;
    }

    public sealed class RmapComposerComposition
    {
        private readonly ReadOnlyCollection<RmapPatternBaseCell> baseCells;
        private readonly ReadOnlyCollection<RmapComposerSelection> selections;
        private readonly ReadOnlyCollection<RmapComposerOverlayCell> overlays;
        private readonly ReadOnlyCollection<RmapComposerCell> protectedSpine;
        private readonly ReadOnlyCollection<string> requiredPortIds;

        internal RmapComposerComposition(RmapComposerRequest request, RmapPortChunk portChunk,
            IEnumerable<RmapPatternBaseCell> sourceBaseCells,
            IEnumerable<RmapComposerSelection> sourceSelections,
            IEnumerable<RmapComposerOverlayCell> sourceOverlays,
            IEnumerable<RmapComposerCell> sourceProtectedSpine,
            IEnumerable<string> sourceRequiredPortIds)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            PortChunk = portChunk ?? throw new ArgumentNullException(nameof(portChunk));
            baseCells = new ReadOnlyCollection<RmapPatternBaseCell>((sourceBaseCells ??
                Array.Empty<RmapPatternBaseCell>()).ToArray());
            selections = new ReadOnlyCollection<RmapComposerSelection>((sourceSelections ??
                Array.Empty<RmapComposerSelection>()).OrderBy(value => value.SlotY)
                .ThenBy(value => value.SlotX).ToList());
            overlays = new ReadOnlyCollection<RmapComposerOverlayCell>((sourceOverlays ??
                Array.Empty<RmapComposerOverlayCell>()).OrderBy(value => value.Cell).ToList());
            protectedSpine = new ReadOnlyCollection<RmapComposerCell>((sourceProtectedSpine ??
                Array.Empty<RmapComposerCell>()).Distinct().OrderBy(value => value).ToList());
            requiredPortIds = new ReadOnlyCollection<string>((sourceRequiredPortIds ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
            BaseDigest = Digest("RMAP09_BASE_V1", baseCells.Select(value => ((int)value).ToString(CultureInfo.InvariantCulture)));
            CompositionDigest = Digest("RMAP09_COMPOSITION_V1", new[]
            {
                Request.Seed, PortChunk.ChunkId, PortChunk.SourcePlacement.CandidateId,
                PortChunk.SourcePlacement.OriginX.ToString(CultureInfo.InvariantCulture),
                PortChunk.SourcePlacement.OriginY.ToString(CultureInfo.InvariantCulture), BaseDigest,
                string.Join(";", selections.Select(value => value.SlotX.ToString(CultureInfo.InvariantCulture) + "," +
                    value.SlotY.ToString(CultureInfo.InvariantCulture) + "," + value.CandidateId + "," +
                    value.Transform + "," + value.FinalCells16)),
                string.Join(";", overlays.Select(value => value.Cell + ":" + value.Kind)),
                string.Join(";", protectedSpine), string.Join(";", requiredPortIds),
            });
        }

        public RmapComposerRequest Request { get; }
        public RmapPortChunk PortChunk { get; }
        public IReadOnlyList<RmapPatternBaseCell> BaseCells => baseCells;
        public IReadOnlyList<RmapComposerSelection> Selections => selections;
        public IReadOnlyList<RmapComposerOverlayCell> Overlays => overlays;
        public IReadOnlyList<RmapComposerCell> ProtectedSpine => protectedSpine;
        public IReadOnlyList<string> RequiredPortIds => requiredPortIds;
        public string OptionalRouteEvidence => "PROFILE_CONTEXT_UNKNOWN_NOT_PROMOTED_TO_REQUIRED_PASS";
        public string BaseDigest { get; }
        public string CompositionDigest { get; }

        public RmapPatternBaseCell GetBaseCell(int x, int y)
        {
            if (x < 0 || x >= RmapPortCatalog.ChunkWidth || y < 0 || y >= RmapPortCatalog.ChunkHeight)
                throw new ArgumentOutOfRangeException();
            return baseCells[(y * RmapPortCatalog.ChunkWidth) + x];
        }

        public bool IsSolid(int x, int y) => GetBaseCell(x, y) == RmapPatternBaseCell.Solid;

        private static string Digest(string header, IEnumerable<string> source)
        {
            string text = header + "\n" + string.Join("\n", source ?? Array.Empty<string>());
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }

    public sealed class RmapComposerResult
    {
        private readonly ReadOnlyCollection<RmapComposerAttemptTrace> attemptTraces;

        internal RmapComposerResult(RmapComposerComposition composition,
            IEnumerable<RmapComposerAttemptTrace> sourceAttemptTraces)
        {
            Composition = composition;
            attemptTraces = new ReadOnlyCollection<RmapComposerAttemptTrace>((sourceAttemptTraces ??
                Array.Empty<RmapComposerAttemptTrace>()).ToList());
        }

        public bool Success => Composition != null;
        public RmapComposerComposition Composition { get; }
        public IReadOnlyList<RmapComposerAttemptTrace> AttemptTraces => attemptTraces;
        public int AttemptCount => attemptTraces.Count;
        public string FailureSummary => Success ? string.Empty : string.Join(";", attemptTraces
            .SelectMany(value => value.Rejections).Distinct().OrderBy(value => value, StringComparer.Ordinal));
    }

    public static class RmapComposer
    {
        public const int Width = RmapPortCatalog.ChunkWidth;
        public const int Height = RmapPortCatalog.ChunkHeight;
        public const int SlotWidth = RmapPatternCatalog.Width;
        public const int SlotHeight = RmapPatternCatalog.Height;
        public const int SlotCount = 6;

        private static readonly RmapComposerCell[] RequiredWalkSpine = Enumerable.Range(0, 6)
            .SelectMany(x => new[] { new RmapComposerCell(x, 1), new RmapComposerCell(x, 2) }).ToArray();
        private static readonly RmapComposerCell[] RequiredClimbSpine = Enumerable.Range(1, 7)
            .Select(y => new RmapComposerCell(5, y)).ToArray();
        private static readonly RmapComposerCell[] RequiredSupport = Enumerable.Range(0, 6)
            .Select(x => new RmapComposerCell(x, 0)).ToArray();

        public static RmapComposerRequest CreateFixtureRequest()
        {
            return new RmapComposerRequest("RMAP09_COMPOSER_FIXTURE_V1", "T3_CLIMB", 3,
                new[]
                {
                    new RmapComposerAttemptPlan("blocked-left-spine", new[]
                    {
                        Proposal(0, 0, "RMAP07_4DD6D5BB9D7D", RmapPatternTransform.R0),
                        Proposal(4, 0, "RMAP07_56FB16683040", RmapPatternTransform.MirrorY),
                        Proposal(8, 0, "RMAP07_6F5D1DF424AA", RmapPatternTransform.MirrorX),
                        Proposal(0, 4, "RMAP07_26DF04E23AA9", RmapPatternTransform.R0),
                        Proposal(4, 4, "RMAP07_0A655882772A", RmapPatternTransform.R0),
                        Proposal(8, 4, "RMAP07_27786F255580", RmapPatternTransform.R0),
                    }),
                    new RmapComposerAttemptPlan("blocked-up-port", new[]
                    {
                        Proposal(0, 0, "RMAP07_6F5D1DF424AA", RmapPatternTransform.R0),
                        Proposal(4, 0, "RMAP07_56FB16683040", RmapPatternTransform.MirrorY),
                        Proposal(8, 0, "RMAP07_6F5D1DF424AA", RmapPatternTransform.MirrorX),
                        Proposal(0, 4, "RMAP07_26DF04E23AA9", RmapPatternTransform.R0),
                        Proposal(4, 4, "RMAP07_56FB16683040", RmapPatternTransform.R0),
                        Proposal(8, 4, "RMAP07_27786F255580", RmapPatternTransform.R0),
                    }),
                    new RmapComposerAttemptPlan("accepted-type3-ladder", new[]
                    {
                        Proposal(0, 0, "RMAP07_6F5D1DF424AA", RmapPatternTransform.R0),
                        Proposal(4, 0, "RMAP07_56FB16683040", RmapPatternTransform.MirrorY),
                        Proposal(8, 0, "RMAP07_6F5D1DF424AA", RmapPatternTransform.MirrorX),
                        Proposal(0, 4, "RMAP07_26DF04E23AA9", RmapPatternTransform.R0),
                        Proposal(4, 4, "RMAP07_0A655882772A", RmapPatternTransform.R0),
                        Proposal(8, 4, "RMAP07_27786F255580", RmapPatternTransform.R0),
                    }),
                });
        }

        public static RmapComposerResult Compose(RmapComposerRequest request)
        {
            var traces = new List<RmapComposerAttemptTrace>();
            if (request == null || request.MaximumAttempts <= 0)
                return new RmapComposerResult(null, new[] { Trace(1, "invalid-request", null,
                    new[] { "INVALID_REQUEST" }) });

            RmapPortCatalogSnapshot ports = RmapPortCatalog.BuildFixture();
            if (!ports.TryGetChunk(request.TargetChunkId, out RmapPortChunk target))
                return new RmapComposerResult(null, new[] { Trace(1, "unknown-port-chunk", null,
                    new[] { "UNKNOWN_TARGET_CHUNK" }) });
            if (target.SpaceState != RmapPortSpaceState.Active || target.ChunkType != RmapPortChunkType.Type3)
                return new RmapComposerResult(null, new[] { Trace(1, "unsupported-port-chunk", null,
                    new[] { "TARGET_MUST_BE_ACTIVE_TYPE3" }) });

            RmapPatternCatalogSnapshot patterns = RmapPatternCatalog.BuildInitialPool();
            int bound = Math.Min(request.MaximumAttempts, request.AttemptPlans.Count);
            for (var index = 0; index < bound; index++)
            {
                RmapComposerAttemptPlan plan = request.AttemptPlans[index];
                var errors = new List<string>();
                List<RmapComposerSelection> selections = Select(patterns, plan, errors);
                RmapPatternBaseCell[] baseCells = BuildBase(selections, errors);
                ValidateRequiredPortInputs(target, baseCells, errors);
                ValidateProtectedSpine(target, baseCells, errors);
                List<RmapComposerOverlayCell> overlays = BuildOverlay(baseCells, errors);
                RmapComposerAttemptTrace trace = Trace(index + 1, plan.AttemptId, selections, errors);
                traces.Add(trace);
                if (!trace.Accepted)
                    continue;

                string[] requiredPortIds = target.Ports.Where(value => value.Required)
                    .Select(value => value.PortId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                var protectedCells = RequiredWalkSpine.Concat(RequiredClimbSpine).Concat(RequiredSupport);
                return new RmapComposerResult(new RmapComposerComposition(request, target, baseCells, selections,
                    overlays, protectedCells, requiredPortIds), traces);
            }

            if (request.AttemptPlans.Count < request.MaximumAttempts)
                traces.Add(Trace(traces.Count + 1, "attempt-plan-exhausted", null,
                    new[] { "ATTEMPT_PLAN_EXHAUSTED" }));
            return new RmapComposerResult(null, traces);
        }

        private static RmapComposerProposal Proposal(int x, int y, string id, RmapPatternTransform transform) =>
            new RmapComposerProposal(x, y, id, transform);

        private static List<RmapComposerSelection> Select(RmapPatternCatalogSnapshot patterns,
            RmapComposerAttemptPlan plan, ICollection<string> errors)
        {
            var result = new List<RmapComposerSelection>();
            if (plan == null)
            {
                errors.Add("MISSING_ATTEMPT_PLAN");
                return result;
            }
            RmapComposerCell[] expected = new[]
            {
                new RmapComposerCell(0, 0), new RmapComposerCell(4, 0), new RmapComposerCell(8, 0),
                new RmapComposerCell(0, 4), new RmapComposerCell(4, 4), new RmapComposerCell(8, 4),
            };
            if (plan.Proposals.Count != SlotCount || plan.Proposals.Select(value => new RmapComposerCell(
                    value.SlotX, value.SlotY)).Distinct().Count() != SlotCount || !expected.All(cell =>
                    plan.Proposals.Any(value => value.SlotX == cell.X && value.SlotY == cell.Y)))
                errors.Add("SLOT_LAYOUT_MUST_BE_EXACT_3X2");
            if (plan.Proposals.GroupBy(value => value.StableKey, StringComparer.Ordinal).Any(value => value.Count() > 1))
                errors.Add("DUPLICATE_TRANSFORMED_PATTERN");

            foreach (RmapComposerProposal proposal in plan.Proposals)
            {
                if (proposal.SlotX < 0 || proposal.SlotX + SlotWidth > Width || proposal.SlotY < 0 ||
                    proposal.SlotY + SlotHeight > Height || proposal.SlotX % SlotWidth != 0 ||
                    proposal.SlotY % SlotHeight != 0)
                {
                    errors.Add("SLOT_OUT_OF_RANGE:" + proposal.SlotX.ToString(CultureInfo.InvariantCulture) + "," +
                        proposal.SlotY.ToString(CultureInfo.InvariantCulture));
                    continue;
                }
                if (!patterns.TryGetCandidate(proposal.CandidateId, out RmapPatternCandidate candidate))
                {
                    errors.Add("UNKNOWN_RMAP07_CANDIDATE:" + proposal.CandidateId);
                    continue;
                }
                if (!Enum.IsDefined(typeof(RmapPatternTransform), proposal.Transform))
                {
                    errors.Add("UNKNOWN_TRANSFORM:" + proposal.Transform);
                    continue;
                }
                result.Add(new RmapComposerSelection(proposal, candidate,
                    RmapPatternCatalog.TransformCells(candidate.BaseCells, proposal.Transform)));
            }
            return result;
        }

        private static RmapPatternBaseCell[] BuildBase(IEnumerable<RmapComposerSelection> selections,
            ICollection<string> errors)
        {
            var result = Enumerable.Repeat(RmapPatternBaseCell.Air, Width * Height).ToArray();
            foreach (RmapComposerSelection selection in selections ?? Array.Empty<RmapComposerSelection>())
            {
                if (selection.FinalCells.Count != SlotWidth * SlotHeight)
                {
                    errors.Add("INVALID_16_CELL_PATTERN:" + selection.CandidateId);
                    continue;
                }
                for (var localY = 0; localY < SlotHeight; localY++)
                for (var localX = 0; localX < SlotWidth; localX++)
                {
                    int x = selection.SlotX + localX;
                    int y = selection.SlotY + localY;
                    result[(y * Width) + x] = selection.FinalCells[(localY * SlotWidth) + localX];
                }
            }
            return result;
        }

        private static void ValidateRequiredPortInputs(RmapPortChunk target,
            IReadOnlyList<RmapPatternBaseCell> baseCells, ICollection<string> errors)
        {
            foreach (RmapEdgePort port in target.Ports.Where(value => value.Required))
            foreach (int coordinate in port.OpenCells)
            {
                RmapPortCell edge = RmapPortCatalog.ToChunkCell(port.Side, coordinate);
                if (!IsAir(baseCells, edge.X, edge.Y))
                    errors.Add("REQUIRED_PORT_BLOCKED:" + port.PortId + ":" + coordinate.ToString(CultureInfo.InvariantCulture));
            }
            foreach (RmapPortInteriorLink link in RmapPortCatalog.BuildFixture().InteriorLinks.Where(value =>
                         value.ChunkId == target.ChunkId && value.Required))
            {
                if (!string.Equals(link.ProfileDigest, GeneratedTraversalProfileCatalog.Create().Digest,
                        StringComparison.Ordinal) || link.EvidenceState != RmapPortEvidenceState.FixturePassExpected)
                    errors.Add("REQUIRED_INTERIOR_LINK_NOT_VERIFIED:" + link.ConnectionId);
            }
        }

        private static void ValidateProtectedSpine(RmapPortChunk target,
            IReadOnlyList<RmapPatternBaseCell> baseCells, ICollection<string> errors)
        {
            foreach (RmapComposerCell cell in RequiredWalkSpine.Concat(RequiredClimbSpine))
                if (!IsAir(baseCells, cell.X, cell.Y))
                    errors.Add("PROTECTED_CLEARANCE_BLOCKED:" + cell);
            foreach (RmapComposerCell cell in RequiredSupport)
                if (Get(baseCells, cell.X, cell.Y) != RmapPatternBaseCell.Solid)
                    errors.Add("PROTECTED_SUPPORT_MISSING:" + cell);

            RmapEdgePort upPort = target.Ports.FirstOrDefault(value => value.Required &&
                value.Side == RmapPortSide.Up && value.TraversalKind == RmapPortTraversalKind.Climb);
            if (upPort == null)
            {
                errors.Add("REQUIRED_CLIMB_UP_PORT_MISSING");
                return;
            }
            foreach (int x in upPort.OpenCells)
                if (!IsAir(baseCells, x, Height - 1))
                    errors.Add("UP_PORT_CLEARANCE_BLOCKED:" + x.ToString(CultureInfo.InvariantCulture));
        }

        private static List<RmapComposerOverlayCell> BuildOverlay(IReadOnlyList<RmapPatternBaseCell> baseCells,
            ICollection<string> errors)
        {
            var result = new List<RmapComposerOverlayCell>();
            foreach (RmapComposerCell cell in RequiredClimbSpine)
            {
                if (!IsAir(baseCells, cell.X, cell.Y))
                {
                    errors.Add("OVERLAY_COLLIDES_WITH_BASE:" + cell);
                    continue;
                }
                result.Add(new RmapComposerOverlayCell(cell, RmapComposerOverlayKind.Ladder));
            }
            return result;
        }

        private static RmapComposerAttemptTrace Trace(int number, string id,
            IEnumerable<RmapComposerSelection> selections, IEnumerable<string> errors) =>
            new RmapComposerAttemptTrace(number, id, selections, errors);

        private static bool IsAir(IReadOnlyList<RmapPatternBaseCell> cells, int x, int y) =>
            Get(cells, x, y) == RmapPatternBaseCell.Air;

        private static RmapPatternBaseCell Get(IReadOnlyList<RmapPatternBaseCell> cells, int x, int y)
        {
            if (cells == null || x < 0 || x >= Width || y < 0 || y >= Height)
                return RmapPatternBaseCell.Solid;
            return cells[(y * Width) + x];
        }
    }
}
