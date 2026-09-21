using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.WorldGeneration.SV5.Foundation
{
    /// <summary>
    /// SV5's deliberately small, deterministic 3x2 composition boundary.
    /// It consumes SV5 base cells and SV5's declared port/profile data;
    /// it does not create a seeded run, repair terrain, or infer missing ports.
    /// </summary>
    public enum Sv5ComposerOverlayKind
    {
        Ladder = 0,
    }

    public readonly struct Sv5ComposerCell : IEquatable<Sv5ComposerCell>, IComparable<Sv5ComposerCell>
    {
        public Sv5ComposerCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool Equals(Sv5ComposerCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Sv5ComposerCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public int CompareTo(Sv5ComposerCell other) => Y != other.Y ? Y.CompareTo(other.Y) : X.CompareTo(other.X);
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," +
            Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class Sv5ComposerProposal
    {
        public Sv5ComposerProposal(int slotX, int slotY, string candidateId, Sv5PatternTransform transform)
        {
            SlotX = slotX;
            SlotY = slotY;
            CandidateId = candidateId ?? string.Empty;
            Transform = transform;
        }

        public int SlotX { get; }
        public int SlotY { get; }
        public string CandidateId { get; }
        public Sv5PatternTransform Transform { get; }
        public string StableKey => CandidateId + "|" + Transform;
    }

    public sealed class Sv5ComposerAttemptPlan
    {
        private readonly ReadOnlyCollection<Sv5ComposerProposal> proposals;

        public Sv5ComposerAttemptPlan(string attemptId, IEnumerable<Sv5ComposerProposal> sourceProposals)
        {
            AttemptId = string.IsNullOrWhiteSpace(attemptId) ? "UNNAMED" : attemptId.Trim();
            proposals = new ReadOnlyCollection<Sv5ComposerProposal>((sourceProposals ??
                Array.Empty<Sv5ComposerProposal>()).Where(value => value != null)
                .OrderBy(value => value.SlotY).ThenBy(value => value.SlotX).ToList());
        }

        public string AttemptId { get; }
        public IReadOnlyList<Sv5ComposerProposal> Proposals => proposals;
    }

    public sealed class Sv5ComposerRequest
    {
        private readonly ReadOnlyCollection<Sv5ComposerAttemptPlan> attemptPlans;

        public Sv5ComposerRequest(string seed, string targetChunkId, int maximumAttempts,
            IEnumerable<Sv5ComposerAttemptPlan> sourceAttemptPlans)
        {
            Seed = string.IsNullOrWhiteSpace(seed) ? "SV5_DEFAULT" : seed.Trim();
            TargetChunkId = string.IsNullOrWhiteSpace(targetChunkId) ? string.Empty : targetChunkId.Trim();
            MaximumAttempts = maximumAttempts;
            attemptPlans = new ReadOnlyCollection<Sv5ComposerAttemptPlan>((sourceAttemptPlans ??
                Array.Empty<Sv5ComposerAttemptPlan>()).Where(value => value != null).ToList());
        }

        public string Seed { get; }
        public string TargetChunkId { get; }
        public int MaximumAttempts { get; }
        public IReadOnlyList<Sv5ComposerAttemptPlan> AttemptPlans => attemptPlans;
    }

    public sealed class Sv5ComposerSelection
    {
        private readonly ReadOnlyCollection<Sv5PatternBaseCell> finalCells;

        internal Sv5ComposerSelection(Sv5ComposerProposal proposal, Sv5PatternCandidate candidate,
            IEnumerable<Sv5PatternBaseCell> sourceFinalCells)
        {
            Proposal = proposal ?? throw new ArgumentNullException(nameof(proposal));
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            finalCells = new ReadOnlyCollection<Sv5PatternBaseCell>((sourceFinalCells ??
                Array.Empty<Sv5PatternBaseCell>()).ToArray());
        }

        public Sv5ComposerProposal Proposal { get; }
        public Sv5PatternCandidate Candidate { get; }
        public int SlotX => Proposal.SlotX;
        public int SlotY => Proposal.SlotY;
        public string CandidateId => Candidate.CandidateId;
        public Sv5PatternTransform Transform => Proposal.Transform;
        public IReadOnlyList<Sv5PatternBaseCell> FinalCells => finalCells;
        public string FinalCells16 => Sv5PatternCatalog.SerializeBaseCells16(finalCells);
    }

    public sealed class Sv5ComposerOverlayCell
    {
        public Sv5ComposerOverlayCell(Sv5ComposerCell cell, Sv5ComposerOverlayKind kind)
        {
            Cell = cell;
            Kind = kind;
        }

        public Sv5ComposerCell Cell { get; }
        public Sv5ComposerOverlayKind Kind { get; }
    }

    public sealed class Sv5ComposerAttemptTrace
    {
        private readonly ReadOnlyCollection<Sv5ComposerSelection> selections;
        private readonly ReadOnlyCollection<string> rejections;

        internal Sv5ComposerAttemptTrace(int attemptNumber, string attemptId,
            IEnumerable<Sv5ComposerSelection> sourceSelections, IEnumerable<string> sourceRejections)
        {
            AttemptNumber = attemptNumber;
            AttemptId = attemptId ?? string.Empty;
            selections = new ReadOnlyCollection<Sv5ComposerSelection>((sourceSelections ??
                Array.Empty<Sv5ComposerSelection>()).OrderBy(value => value.SlotY)
                .ThenBy(value => value.SlotX).ToList());
            rejections = new ReadOnlyCollection<string>((sourceRejections ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).OrderBy(value => value,
                    StringComparer.Ordinal).ToList());
        }

        public int AttemptNumber { get; }
        public string AttemptId { get; }
        public IReadOnlyList<Sv5ComposerSelection> Selections => selections;
        public IReadOnlyList<string> Rejections => rejections;
        public bool Accepted => rejections.Count == 0;
    }

    public sealed class Sv5ComposerComposition
    {
        private readonly ReadOnlyCollection<Sv5PatternBaseCell> baseCells;
        private readonly ReadOnlyCollection<Sv5ComposerSelection> selections;
        private readonly ReadOnlyCollection<Sv5ComposerOverlayCell> overlays;
        private readonly ReadOnlyCollection<Sv5ComposerCell> protectedSpine;
        private readonly ReadOnlyCollection<string> requiredPortIds;

        internal Sv5ComposerComposition(Sv5ComposerRequest request, Sv5PortChunk portChunk,
            IEnumerable<Sv5PatternBaseCell> sourceBaseCells,
            IEnumerable<Sv5ComposerSelection> sourceSelections,
            IEnumerable<Sv5ComposerOverlayCell> sourceOverlays,
            IEnumerable<Sv5ComposerCell> sourceProtectedSpine,
            IEnumerable<string> sourceRequiredPortIds)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            PortChunk = portChunk ?? throw new ArgumentNullException(nameof(portChunk));
            baseCells = new ReadOnlyCollection<Sv5PatternBaseCell>((sourceBaseCells ??
                Array.Empty<Sv5PatternBaseCell>()).ToArray());
            selections = new ReadOnlyCollection<Sv5ComposerSelection>((sourceSelections ??
                Array.Empty<Sv5ComposerSelection>()).OrderBy(value => value.SlotY)
                .ThenBy(value => value.SlotX).ToList());
            overlays = new ReadOnlyCollection<Sv5ComposerOverlayCell>((sourceOverlays ??
                Array.Empty<Sv5ComposerOverlayCell>()).OrderBy(value => value.Cell).ToList());
            protectedSpine = new ReadOnlyCollection<Sv5ComposerCell>((sourceProtectedSpine ??
                Array.Empty<Sv5ComposerCell>()).Distinct().OrderBy(value => value).ToList());
            requiredPortIds = new ReadOnlyCollection<string>((sourceRequiredPortIds ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
            BaseDigest = Digest("SV5_BASE_V1", baseCells.Select(value => ((int)value).ToString(CultureInfo.InvariantCulture)));
            CompositionDigest = Digest("SV5_COMPOSITION_V1", new[]
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

        public Sv5ComposerRequest Request { get; }
        public Sv5PortChunk PortChunk { get; }
        public IReadOnlyList<Sv5PatternBaseCell> BaseCells => baseCells;
        public IReadOnlyList<Sv5ComposerSelection> Selections => selections;
        public IReadOnlyList<Sv5ComposerOverlayCell> Overlays => overlays;
        public IReadOnlyList<Sv5ComposerCell> ProtectedSpine => protectedSpine;
        public IReadOnlyList<string> RequiredPortIds => requiredPortIds;
        public string OptionalRouteEvidence => "PROFILE_CONTEXT_UNKNOWN_NOT_PROMOTED_TO_REQUIRED_PASS";
        public string BaseDigest { get; }
        public string CompositionDigest { get; }

        public Sv5PatternBaseCell GetBaseCell(int x, int y)
        {
            if (x < 0 || x >= Sv5PortCatalog.ChunkWidth || y < 0 || y >= Sv5PortCatalog.ChunkHeight)
                throw new ArgumentOutOfRangeException();
            return baseCells[(y * Sv5PortCatalog.ChunkWidth) + x];
        }

        public bool IsSolid(int x, int y) => GetBaseCell(x, y) == Sv5PatternBaseCell.Solid;

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

    public sealed class Sv5ComposerResult
    {
        private readonly ReadOnlyCollection<Sv5ComposerAttemptTrace> attemptTraces;

        internal Sv5ComposerResult(Sv5ComposerComposition composition,
            IEnumerable<Sv5ComposerAttemptTrace> sourceAttemptTraces)
        {
            Composition = composition;
            attemptTraces = new ReadOnlyCollection<Sv5ComposerAttemptTrace>((sourceAttemptTraces ??
                Array.Empty<Sv5ComposerAttemptTrace>()).ToList());
        }

        public bool Success => Composition != null;
        public Sv5ComposerComposition Composition { get; }
        public IReadOnlyList<Sv5ComposerAttemptTrace> AttemptTraces => attemptTraces;
        public int AttemptCount => attemptTraces.Count;
        public string FailureSummary => Success ? string.Empty : string.Join(";", attemptTraces
            .SelectMany(value => value.Rejections).Distinct().OrderBy(value => value, StringComparer.Ordinal));
    }

    public static class Sv5Composer
    {
        public const int Width = Sv5PortCatalog.ChunkWidth;
        public const int Height = Sv5PortCatalog.ChunkHeight;
        public const int SlotWidth = Sv5PatternCatalog.Width;
        public const int SlotHeight = Sv5PatternCatalog.Height;
        public const int SlotCount = 6;

        private static readonly Sv5ComposerCell[] RequiredWalkSpine = Enumerable.Range(0, 6)
            .SelectMany(x => new[] { new Sv5ComposerCell(x, 1), new Sv5ComposerCell(x, 2) }).ToArray();
        private static readonly Sv5ComposerCell[] RequiredClimbSpine = Enumerable.Range(1, 7)
            .Select(y => new Sv5ComposerCell(5, y)).ToArray();
        private static readonly Sv5ComposerCell[] RequiredSupport = Enumerable.Range(0, 6)
            .Select(x => new Sv5ComposerCell(x, 0)).ToArray();

        public static Sv5ComposerRequest CreateFixtureRequest()
        {
            return new Sv5ComposerRequest("SV5_COMPOSER_FIXTURE_V1", "T3_CLIMB", 3,
                new[]
                {
                    new Sv5ComposerAttemptPlan("blocked-left-spine", new[]
                    {
                        Proposal(0, 0, "SV5_4DD6D5BB9D7D", Sv5PatternTransform.R0),
                        Proposal(4, 0, "SV5_56FB16683040", Sv5PatternTransform.MirrorY),
                        Proposal(8, 0, "SV5_6F5D1DF424AA", Sv5PatternTransform.MirrorX),
                        Proposal(0, 4, "SV5_26DF04E23AA9", Sv5PatternTransform.R0),
                        Proposal(4, 4, "SV5_0A655882772A", Sv5PatternTransform.R0),
                        Proposal(8, 4, "SV5_27786F255580", Sv5PatternTransform.R0),
                    }),
                    new Sv5ComposerAttemptPlan("blocked-up-port", new[]
                    {
                        Proposal(0, 0, "SV5_6F5D1DF424AA", Sv5PatternTransform.R0),
                        Proposal(4, 0, "SV5_56FB16683040", Sv5PatternTransform.MirrorY),
                        Proposal(8, 0, "SV5_6F5D1DF424AA", Sv5PatternTransform.MirrorX),
                        Proposal(0, 4, "SV5_26DF04E23AA9", Sv5PatternTransform.R0),
                        Proposal(4, 4, "SV5_56FB16683040", Sv5PatternTransform.R0),
                        Proposal(8, 4, "SV5_27786F255580", Sv5PatternTransform.R0),
                    }),
                    new Sv5ComposerAttemptPlan("accepted-type3-ladder", new[]
                    {
                        Proposal(0, 0, "SV5_6F5D1DF424AA", Sv5PatternTransform.R0),
                        Proposal(4, 0, "SV5_56FB16683040", Sv5PatternTransform.MirrorY),
                        Proposal(8, 0, "SV5_6F5D1DF424AA", Sv5PatternTransform.MirrorX),
                        Proposal(0, 4, "SV5_26DF04E23AA9", Sv5PatternTransform.R0),
                        Proposal(4, 4, "SV5_0A655882772A", Sv5PatternTransform.R0),
                        Proposal(8, 4, "SV5_27786F255580", Sv5PatternTransform.R0),
                    }),
                });
        }

        public static Sv5ComposerResult Compose(Sv5ComposerRequest request)
        {
            var traces = new List<Sv5ComposerAttemptTrace>();
            if (request == null || request.MaximumAttempts <= 0)
                return new Sv5ComposerResult(null, new[] { Trace(1, "invalid-request", null,
                    new[] { "INVALID_REQUEST" }) });

            Sv5PortCatalogSnapshot ports = Sv5PortCatalog.BuildFixture();
            if (!ports.TryGetChunk(request.TargetChunkId, out Sv5PortChunk target))
                return new Sv5ComposerResult(null, new[] { Trace(1, "unknown-port-chunk", null,
                    new[] { "UNKNOWN_TARGET_CHUNK" }) });
            if (target.SpaceState != Sv5PortSpaceState.Active || target.ChunkType != Sv5PortChunkType.Type3)
                return new Sv5ComposerResult(null, new[] { Trace(1, "unsupported-port-chunk", null,
                    new[] { "TARGET_MUST_BE_ACTIVE_TYPE3" }) });

            Sv5PatternCatalogSnapshot patterns = Sv5PatternCatalog.BuildInitialPool();
            int bound = Math.Min(request.MaximumAttempts, request.AttemptPlans.Count);
            for (var index = 0; index < bound; index++)
            {
                Sv5ComposerAttemptPlan plan = request.AttemptPlans[index];
                var errors = new List<string>();
                List<Sv5ComposerSelection> selections = Select(patterns, plan, errors);
                Sv5PatternBaseCell[] baseCells = BuildBase(selections, errors);
                ValidateRequiredPortInputs(target, baseCells, errors);
                ValidateProtectedSpine(target, baseCells, errors);
                List<Sv5ComposerOverlayCell> overlays = BuildOverlay(baseCells, errors);
                Sv5ComposerAttemptTrace trace = Trace(index + 1, plan.AttemptId, selections, errors);
                traces.Add(trace);
                if (!trace.Accepted)
                    continue;

                string[] requiredPortIds = target.Ports.Where(value => value.Required)
                    .Select(value => value.PortId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                var protectedCells = RequiredWalkSpine.Concat(RequiredClimbSpine).Concat(RequiredSupport);
                return new Sv5ComposerResult(new Sv5ComposerComposition(request, target, baseCells, selections,
                    overlays, protectedCells, requiredPortIds), traces);
            }

            if (request.AttemptPlans.Count < request.MaximumAttempts)
                traces.Add(Trace(traces.Count + 1, "attempt-plan-exhausted", null,
                    new[] { "ATTEMPT_PLAN_EXHAUSTED" }));
            return new Sv5ComposerResult(null, traces);
        }

        private static Sv5ComposerProposal Proposal(int x, int y, string id, Sv5PatternTransform transform) =>
            new Sv5ComposerProposal(x, y, id, transform);

        private static List<Sv5ComposerSelection> Select(Sv5PatternCatalogSnapshot patterns,
            Sv5ComposerAttemptPlan plan, ICollection<string> errors)
        {
            var result = new List<Sv5ComposerSelection>();
            if (plan == null)
            {
                errors.Add("MISSING_ATTEMPT_PLAN");
                return result;
            }
            Sv5ComposerCell[] expected = new[]
            {
                new Sv5ComposerCell(0, 0), new Sv5ComposerCell(4, 0), new Sv5ComposerCell(8, 0),
                new Sv5ComposerCell(0, 4), new Sv5ComposerCell(4, 4), new Sv5ComposerCell(8, 4),
            };
            if (plan.Proposals.Count != SlotCount || plan.Proposals.Select(value => new Sv5ComposerCell(
                    value.SlotX, value.SlotY)).Distinct().Count() != SlotCount || !expected.All(cell =>
                    plan.Proposals.Any(value => value.SlotX == cell.X && value.SlotY == cell.Y)))
                errors.Add("SLOT_LAYOUT_MUST_BE_EXACT_3X2");
            if (plan.Proposals.GroupBy(value => value.StableKey, StringComparer.Ordinal).Any(value => value.Count() > 1))
                errors.Add("DUPLICATE_TRANSFORMED_PATTERN");

            foreach (Sv5ComposerProposal proposal in plan.Proposals)
            {
                if (proposal.SlotX < 0 || proposal.SlotX + SlotWidth > Width || proposal.SlotY < 0 ||
                    proposal.SlotY + SlotHeight > Height || proposal.SlotX % SlotWidth != 0 ||
                    proposal.SlotY % SlotHeight != 0)
                {
                    errors.Add("SLOT_OUT_OF_RANGE:" + proposal.SlotX.ToString(CultureInfo.InvariantCulture) + "," +
                        proposal.SlotY.ToString(CultureInfo.InvariantCulture));
                    continue;
                }
                if (!patterns.TryGetCandidate(proposal.CandidateId, out Sv5PatternCandidate candidate))
                {
                    errors.Add("UNKNOWN_SV507_CANDIDATE:" + proposal.CandidateId);
                    continue;
                }
                if (!Enum.IsDefined(typeof(Sv5PatternTransform), proposal.Transform))
                {
                    errors.Add("UNKNOWN_TRANSFORM:" + proposal.Transform);
                    continue;
                }
                result.Add(new Sv5ComposerSelection(proposal, candidate,
                    Sv5PatternCatalog.TransformCells(candidate.BaseCells, proposal.Transform)));
            }
            return result;
        }

        private static Sv5PatternBaseCell[] BuildBase(IEnumerable<Sv5ComposerSelection> selections,
            ICollection<string> errors)
        {
            var result = Enumerable.Repeat(Sv5PatternBaseCell.Air, Width * Height).ToArray();
            foreach (Sv5ComposerSelection selection in selections ?? Array.Empty<Sv5ComposerSelection>())
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

        private static void ValidateRequiredPortInputs(Sv5PortChunk target,
            IReadOnlyList<Sv5PatternBaseCell> baseCells, ICollection<string> errors)
        {
            foreach (Sv5EdgePort port in target.Ports.Where(value => value.Required))
            foreach (int coordinate in port.OpenCells)
            {
                Sv5PortCell edge = Sv5PortCatalog.ToChunkCell(port.Side, coordinate);
                if (!IsAir(baseCells, edge.X, edge.Y))
                    errors.Add("REQUIRED_PORT_BLOCKED:" + port.PortId + ":" + coordinate.ToString(CultureInfo.InvariantCulture));
            }
            foreach (Sv5PortInteriorLink link in Sv5PortCatalog.BuildFixture().InteriorLinks.Where(value =>
                         value.ChunkId == target.ChunkId && value.Required))
            {
                if (!string.Equals(link.ProfileDigest, GeneratedTraversalProfileCatalog.Create().Digest,
                        StringComparison.Ordinal) || link.EvidenceState != Sv5PortEvidenceState.FixturePassExpected)
                    errors.Add("REQUIRED_INTERIOR_LINK_NOT_VERIFIED:" + link.ConnectionId);
            }
        }

        private static void ValidateProtectedSpine(Sv5PortChunk target,
            IReadOnlyList<Sv5PatternBaseCell> baseCells, ICollection<string> errors)
        {
            foreach (Sv5ComposerCell cell in RequiredWalkSpine.Concat(RequiredClimbSpine))
                if (!IsAir(baseCells, cell.X, cell.Y))
                    errors.Add("PROTECTED_CLEARANCE_BLOCKED:" + cell);
            foreach (Sv5ComposerCell cell in RequiredSupport)
                if (Get(baseCells, cell.X, cell.Y) != Sv5PatternBaseCell.Solid)
                    errors.Add("PROTECTED_SUPPORT_MISSING:" + cell);

            Sv5EdgePort upPort = target.Ports.FirstOrDefault(value => value.Required &&
                value.Side == Sv5PortSide.Up && value.TraversalKind == Sv5PortTraversalKind.Climb);
            if (upPort == null)
            {
                errors.Add("REQUIRED_CLIMB_UP_PORT_MISSING");
                return;
            }
            foreach (int x in upPort.OpenCells)
                if (!IsAir(baseCells, x, Height - 1))
                    errors.Add("UP_PORT_CLEARANCE_BLOCKED:" + x.ToString(CultureInfo.InvariantCulture));
        }

        private static List<Sv5ComposerOverlayCell> BuildOverlay(IReadOnlyList<Sv5PatternBaseCell> baseCells,
            ICollection<string> errors)
        {
            var result = new List<Sv5ComposerOverlayCell>();
            foreach (Sv5ComposerCell cell in RequiredClimbSpine)
            {
                if (!IsAir(baseCells, cell.X, cell.Y))
                {
                    errors.Add("OVERLAY_COLLIDES_WITH_BASE:" + cell);
                    continue;
                }
                result.Add(new Sv5ComposerOverlayCell(cell, Sv5ComposerOverlayKind.Ladder));
            }
            return result;
        }

        private static Sv5ComposerAttemptTrace Trace(int number, string id,
            IEnumerable<Sv5ComposerSelection> selections, IEnumerable<string> errors) =>
            new Sv5ComposerAttemptTrace(number, id, selections, errors);

        private static bool IsAir(IReadOnlyList<Sv5PatternBaseCell> cells, int x, int y) =>
            Get(cells, x, y) == Sv5PatternBaseCell.Air;

        private static Sv5PatternBaseCell Get(IReadOnlyList<Sv5PatternBaseCell> cells, int x, int y)
        {
            if (cells == null || x < 0 || x >= Width || y < 0 || y >= Height)
                return Sv5PatternBaseCell.Solid;
            return cells[(y * Width) + x];
        }
    }
}
