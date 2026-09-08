using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Validation;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>RMAP10's bounded, physical small-run plan.  It deliberately
    /// consumes the RMAP07/08/09 contracts and does not enter the historical
    /// 500-pattern seeded-run pipeline.</summary>
    public enum RmapSmallRunRecipe { PortGalleryV1 = 0 }

    public sealed class RmapSmallRunRequest
    {
        public RmapSmallRunRequest(int seed, int width, int height, RmapSmallRunRecipe recipe)
        {
            Seed = seed; Width = width; Height = height; Recipe = recipe;
        }
        public int Seed { get; }
        public int Width { get; }
        public int Height { get; }
        public RmapSmallRunRecipe Recipe { get; }
        public string RecipeId => Recipe.ToString();
    }

    public sealed class RmapSmallRunPatternSelection
    {
        internal RmapSmallRunPatternSelection(int slotX, int slotY, RmapPatternCandidate candidate,
            RmapPatternTransform transform, IEnumerable<RmapPatternBaseCell> cells)
        {
            SlotX = slotX; SlotY = slotY; CandidateId = candidate.CandidateId; Transform = transform;
            FinalCells = new ReadOnlyCollection<RmapPatternBaseCell>((cells ?? Array.Empty<RmapPatternBaseCell>()).ToArray());
        }
        public int SlotX { get; }
        public int SlotY { get; }
        public string CandidateId { get; }
        public RmapPatternTransform Transform { get; }
        public IReadOnlyList<RmapPatternBaseCell> FinalCells { get; }
        public string FinalCells16 => RmapPatternCatalog.SerializeBaseCells16(FinalCells);
    }

    public sealed class RmapSmallRunChunk
    {
        internal RmapSmallRunChunk(string instanceId, int originX, int originY, RmapPortChunk source,
            IEnumerable<RmapSmallRunPatternSelection> selections, IEnumerable<RmapPatternBaseCell> baseCells,
            IEnumerable<RmapComposerOverlayCell> overlays, int attemptCount)
        {
            InstanceId = instanceId; OriginX = originX; OriginY = originY; Source = source;
            Selections = new ReadOnlyCollection<RmapSmallRunPatternSelection>((selections ?? Array.Empty<RmapSmallRunPatternSelection>()).ToArray());
            BaseCells = new ReadOnlyCollection<RmapPatternBaseCell>((baseCells ?? Array.Empty<RmapPatternBaseCell>()).ToArray());
            Overlays = new ReadOnlyCollection<RmapComposerOverlayCell>((overlays ?? Array.Empty<RmapComposerOverlayCell>()).ToArray());
            AttemptCount = attemptCount;
        }
        public string InstanceId { get; }
        public int OriginX { get; }
        public int OriginY { get; }
        public RmapPortChunk Source { get; }
        public IReadOnlyList<RmapSmallRunPatternSelection> Selections { get; }
        public IReadOnlyList<RmapPatternBaseCell> BaseCells { get; }
        public IReadOnlyList<RmapComposerOverlayCell> Overlays { get; }
        public int AttemptCount { get; }
        public RmapPatternBaseCell GetBaseCell(int x, int y) => BaseCells[(y * RmapPortCatalog.ChunkWidth) + x];
    }

    public sealed class RmapSmallRunPort
    {
        internal RmapSmallRunPort(RmapSmallRunChunk chunk, RmapEdgePort port)
        {
            ChunkInstanceId = chunk.InstanceId; ChunkId = chunk.Source.ChunkId; PortId = port.PortId;
            Side = port.Side; TraversalKind = port.TraversalKind; FlowDirection = port.FlowDirection;
            Required = port.Required;
            GlobalCells = new ReadOnlyCollection<Vector2Int>(port.OpenCells.Select(value =>
            {
                RmapPortCell local = RmapPortCatalog.ToChunkCell(port.Side, value);
                return new Vector2Int(chunk.OriginX + local.X, chunk.OriginY + local.Y);
            }).ToArray());
        }
        public string ChunkInstanceId { get; }
        public string ChunkId { get; }
        public string PortId { get; }
        public RmapPortSide Side { get; }
        public RmapPortTraversalKind TraversalKind { get; }
        public RmapPortFlowDirection FlowDirection { get; }
        public bool Required { get; }
        public IReadOnlyList<Vector2Int> GlobalCells { get; }
    }

    public sealed class RmapSmallRunPlan
    {
        internal RmapSmallRunPlan(RmapSmallRunRequest request, IEnumerable<RmapSmallRunChunk> chunks,
            IEnumerable<RmapSmallRunPort> ports, IEnumerable<string> failures)
        {
            Request = request;
            Chunks = new ReadOnlyCollection<RmapSmallRunChunk>((chunks ?? Array.Empty<RmapSmallRunChunk>()).ToArray());
            Ports = new ReadOnlyCollection<RmapSmallRunPort>((ports ?? Array.Empty<RmapSmallRunPort>()).ToArray());
            Failures = new ReadOnlyCollection<string>((failures ?? Array.Empty<string>()).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray());
            BaseDigest = Digest("RMAP10_BASE_V1", Chunks.SelectMany(chunk => chunk.BaseCells.Select((cell, index) =>
                chunk.InstanceId + ":" + index.ToString(CultureInfo.InvariantCulture) + ":" + cell)));
            PlanDigest = Digest("RMAP10_PLAN_V1", new[]
            {
                request == null ? string.Empty : request.Seed.ToString(CultureInfo.InvariantCulture),
                request == null ? string.Empty : request.Width + "x" + request.Height,
                request == null ? string.Empty : request.RecipeId,
                BaseDigest,
                string.Join(";", Chunks.Select(chunk => chunk.InstanceId + "=" + chunk.Source.ChunkId + ":" + string.Join("|", chunk.Selections.Select(selection => selection.CandidateId + ":" + selection.Transform)))),
                string.Join(";", Ports.Select(port => port.ChunkInstanceId + ":" + port.PortId + ":" + string.Join("/", port.GlobalCells))),
            });
        }
        public RmapSmallRunRequest Request { get; }
        public IReadOnlyList<RmapSmallRunChunk> Chunks { get; }
        public IReadOnlyList<RmapSmallRunPort> Ports { get; }
        public IReadOnlyList<string> Failures { get; }
        public bool Success => Request != null && Failures.Count == 0;
        public string FailureSummary => string.Join(";", Failures);
        public string BaseDigest { get; }
        public string PlanDigest { get; }
        public Vector2Int StartTile => new Vector2Int(1, 1);
        public Vector2Int ExitTile => Request == null ? Vector2Int.zero : new Vector2Int(Request.Width - 2, 1);
        public int MaximumAttempts => 3;
        public int TotalSelectionAttempts => Chunks.Sum(value => value.AttemptCount);
        public bool IsSolid(int x, int y)
        {
            RmapSmallRunChunk chunk = Chunks.FirstOrDefault(value => x >= value.OriginX && x < value.OriginX + RmapPortCatalog.ChunkWidth && y >= value.OriginY && y < value.OriginY + RmapPortCatalog.ChunkHeight);
            return chunk != null && chunk.GetBaseCell(x - chunk.OriginX, y - chunk.OriginY) == RmapPatternBaseCell.Solid;
        }
        private static string Digest(string title, IEnumerable<string> lines)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(title + "\n" + string.Join("\n", lines ?? Array.Empty<string>()));
                return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }

    /// <summary>Serialized scene identity, never a world-state or save system.</summary>
    public sealed class RmapSmallRunSceneState : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private RmapSmallRunRecipe recipe;
        [SerializeField] private string planDigest;
        public int Seed => seed;
        public int Width => width;
        public int Height => height;
        public RmapSmallRunRecipe Recipe => recipe;
        public string PlanDigest => planDigest;
        public RmapSmallRunRequest Request => new RmapSmallRunRequest(seed, width, height, recipe);
        public void Configure(RmapSmallRunPlan plan)
        {
            if (plan == null || !plan.Success) throw new ArgumentException("A successful RMAP10 plan is required.", nameof(plan));
            seed = plan.Request.Seed; width = plan.Request.Width; height = plan.Request.Height;
            recipe = plan.Request.Recipe; planDigest = plan.PlanDigest;
        }
    }

    public static class RmapSmallRunHarness
    {
        public const int MinimumWidth = 36;
        public const int MinimumHeight = 24;
        public const int MaximumWidth = 48;
        public const int MaximumHeight = 24;
        private const string FloorCandidateId = "RMAP07_6F5D1DF424AA";
        private const string ClearCandidateId = "RMAP07_991204FBA2B6";

        public static RmapSmallRunPlan Generate(RmapSmallRunRequest request)
        {
            var failures = new List<string>();
            if (request == null) failures.Add("MISSING_REQUEST");
            else
            {
                if (request.Width % RmapPortCatalog.ChunkWidth != 0 || request.Height % RmapPortCatalog.ChunkHeight != 0)
                    failures.Add("SIZE_NOT_CHUNK_ALIGNED:" + request.Width + "x" + request.Height);
                if (request.Width < MinimumWidth || request.Width > MaximumWidth || request.Height != MinimumHeight)
                    failures.Add("UNSUPPORTED_SIZE:" + request.Width + "x" + request.Height + ";SUPPORTED=36x24,48x24");
                if (request.Recipe != RmapSmallRunRecipe.PortGalleryV1) failures.Add("UNSUPPORTED_RECIPE:" + request.Recipe);
            }
            if (failures.Count != 0) return new RmapSmallRunPlan(request, Array.Empty<RmapSmallRunChunk>(), Array.Empty<RmapSmallRunPort>(), failures);

            RmapPortCatalogSnapshot ports = RmapPortCatalog.BuildFixture();
            if (!RmapPortCatalog.Validate(ports).IsValid) failures.Add("PORT_CATALOG_INVALID");
            var chunks = new List<RmapSmallRunChunk>();
            int columns = request.Width / RmapPortCatalog.ChunkWidth;
            int rows = request.Height / RmapPortCatalog.ChunkHeight;
            for (var y = 0; y < rows; y++)
            for (var x = 0; x < columns; x++)
            {
                string sourceId = SourceFor(x, y, columns);
                if (!ports.TryGetChunk(sourceId, out RmapPortChunk source))
                {
                    failures.Add("MISSING_PORT_CHUNK:" + sourceId);
                    continue;
                }
                RmapSmallRunChunk chunk = CreateChunk(request, source, x, y, chunks.Count, failures);
                chunks.Add(chunk);
            }
            var globalPorts = chunks.SelectMany(chunk => chunk.Source.Ports.Select(port => new RmapSmallRunPort(chunk, port))).ToList();
            ValidatePlan(request, chunks, globalPorts, failures);
            return new RmapSmallRunPlan(request, chunks, globalPorts, failures);
        }

        private static string SourceFor(int x, int y, int columns)
        {
            if (y == 0) return x == 1 ? "T1_B" : "T1_A";
            if (y == 1) return x == 0 ? "T2_DROP" : x == 1 ? "T3_CLIMB" : x == 2 ? "T4_SPLIT" : "T0_SINGLE_ENTRANCE";
            if (y == 2) return x == 0 ? "T0_BREAKABLE_SECRET" : x == 1 ? "T0_SINGLE_ENTRANCE" : x == 2 ? "T1_B" : "T4_SPLIT";
            return "INACTIVE_SOLID_WALL";
        }

        private static RmapSmallRunChunk CreateChunk(RmapSmallRunRequest request, RmapPortChunk source,
            int chunkX, int chunkY, int ordinal, ICollection<string> failures)
        {
            int originX = chunkX * RmapPortCatalog.ChunkWidth;
            int originY = chunkY * RmapPortCatalog.ChunkHeight;
            string instanceId = "C" + chunkX.ToString(CultureInfo.InvariantCulture) + "_" + chunkY.ToString(CultureInfo.InvariantCulture);
            if (source.ChunkId == "T3_CLIMB")
            {
                RmapComposerResult composed = RmapComposer.Compose(RmapComposer.CreateFixtureRequest());
                if (!composed.Success) failures.Add("T3_COMPOSER_FAILED:" + composed.FailureSummary);
                RmapComposerComposition composition = composed.Composition;
                return new RmapSmallRunChunk(instanceId, originX, originY, source,
                    composition == null ? Array.Empty<RmapSmallRunPatternSelection>() : composition.Selections.Select(selection =>
                        new RmapSmallRunPatternSelection(selection.SlotX, selection.SlotY, selection.Candidate,
                            selection.Transform, selection.FinalCells)),
                    composition == null ? Array.Empty<RmapPatternBaseCell>() : composition.BaseCells,
                    composition == null ? Array.Empty<RmapComposerOverlayCell>() : composition.Overlays,
                    composed.AttemptCount);
            }

            RmapPatternCatalogSnapshot pool = RmapPatternCatalog.BuildInitialPool();
            RmapPatternCandidate floor = pool.Candidates.Single(value => value.CandidateId == FloorCandidateId);
            RmapPatternCandidate clear = pool.Candidates.Single(value => value.CandidateId == ClearCandidateId);
            RmapPatternCandidate detail = pool.Candidates[(StableIndex(request.Seed, ordinal, pool.Candidates.Count))];
            RmapPatternCandidate oneWay = pool.Candidates.Single(value => value.CandidateId == "RMAP07_348D65F87C81");
            var selections = new List<RmapSmallRunPatternSelection>();
            for (var slotY = 0; slotY < 2; slotY++)
            for (var slotX = 0; slotX < 3; slotX++)
            {
                RmapPatternCandidate candidate;
                if (source.SpaceState == RmapPortSpaceState.InactiveSolid || source.ChunkId == "T0_BREAKABLE_SECRET") candidate = clear;
                else if (slotY == 0) candidate = source.ChunkType == RmapPortChunkType.Type2 || source.ChunkType == RmapPortChunkType.Type4 ? clear : floor;
                else if (source.ChunkId == "T0_SINGLE_ENTRANCE" && slotX == 0) candidate = oneWay;
                else candidate = (slotX == 1 || source.ChunkType == RmapPortChunkType.Type4) ? clear : detail;
                RmapPatternTransform transform = candidate == detail ? (RmapPatternTransform)(StableIndex(request.Seed + 17, ordinal + slotX + (slotY * 3), 4)) : RmapPatternTransform.R0;
                selections.Add(new RmapSmallRunPatternSelection(slotX * RmapPatternCatalog.Width, slotY * RmapPatternCatalog.Height,
                    candidate, transform, RmapPatternCatalog.TransformCells(candidate.BaseCells, transform)));
            }
            RmapPatternBaseCell[] baseCells = AssembleSelections(selections);
            if (source.SpaceState == RmapPortSpaceState.InactiveSolid || source.ChunkId == "T0_BREAKABLE_SECRET")
            {
                baseCells = Enumerable.Range(0, RmapPortCatalog.ChunkCellCount).Select(index =>
                    source.IsSolid(index % RmapPortCatalog.ChunkWidth, index / RmapPortCatalog.ChunkWidth)
                        ? RmapPatternBaseCell.Solid : RmapPatternBaseCell.Air).ToArray();
            }
            foreach (RmapEdgePort port in source.Ports)
            foreach (int coordinate in port.OpenCells)
            {
                RmapPortCell cell = RmapPortCatalog.ToChunkCell(port.Side, coordinate);
                if (baseCells[(cell.Y * RmapPortCatalog.ChunkWidth) + cell.X] != RmapPatternBaseCell.Air)
                    failures.Add("PORT_BLOCKED:" + instanceId + ":" + port.PortId + ":" + coordinate.ToString(CultureInfo.InvariantCulture));
            }
            return new RmapSmallRunChunk(instanceId, originX, originY, source, selections, baseCells,
                Array.Empty<RmapComposerOverlayCell>(), 1);
        }

        private static RmapPatternBaseCell[] AssembleSelections(IEnumerable<RmapSmallRunPatternSelection> selections)
        {
            var cells = Enumerable.Repeat(RmapPatternBaseCell.Air, RmapPortCatalog.ChunkCellCount).ToArray();
            foreach (RmapSmallRunPatternSelection selection in selections)
            for (var y = 0; y < RmapPatternCatalog.Height; y++)
            for (var x = 0; x < RmapPatternCatalog.Width; x++)
                cells[((selection.SlotY + y) * RmapPortCatalog.ChunkWidth) + selection.SlotX + x] = selection.FinalCells[(y * RmapPatternCatalog.Width) + x];
            return cells;
        }

        private static void ValidatePlan(RmapSmallRunRequest request, IEnumerable<RmapSmallRunChunk> chunks,
            IEnumerable<RmapSmallRunPort> ports, ICollection<string> failures)
        {
            RmapSmallRunChunk[] all = chunks.ToArray();
            if (all.Length != (request.Width / 12) * (request.Height / 8)) failures.Add("CHUNK_COUNT_MISMATCH");
            if (all.Any(chunk => chunk.BaseCells.Count != RmapPortCatalog.ChunkCellCount || chunk.Selections.Count != 6)) failures.Add("CHUNK_96_CELL_OR_6_SELECTION_MISMATCH");
            if (!all.Any(chunk => chunk.Source.ChunkType == RmapPortChunkType.Type1) || !all.Any(chunk => chunk.Source.ChunkType == RmapPortChunkType.Type2) ||
                !all.Any(chunk => chunk.Source.ChunkType == RmapPortChunkType.Type3) || !all.Any(chunk => chunk.Source.ChunkType == RmapPortChunkType.Type4)) failures.Add("MISSING_TYPE1_TO_TYPE4");
            if (all.Count(chunk => chunk.Source.ChunkType == RmapPortChunkType.Type0) < 2 || !all.Any(chunk => chunk.Source.ChunkId == "T0_BREAKABLE_SECRET")) failures.Add("MISSING_TYPE0_FORMS");
            if (!ports.Any(port => port.TraversalKind == RmapPortTraversalKind.Drop) || !ports.Any(port => port.TraversalKind == RmapPortTraversalKind.Climb)) failures.Add("MISSING_DIRECTIONAL_PORTS");
            if (!string.Equals(RmapPortCatalog.BuildFixture().ProfileDigest, GeneratedTraversalProfileCatalog.Create().Digest, StringComparison.Ordinal)) failures.Add("PROFILE_DIGEST_MISMATCH");
            if (request.Width <= 2 || request.Height <= 1 || all.FirstOrDefault(chunk => chunk.OriginY == 0) == null) failures.Add("START_EXIT_OUTSIDE_PLAN");
        }

        private static int StableIndex(int seed, int ordinal, int limit)
        {
            unchecked { return (int)((uint)(seed * 1103515245 + ordinal * 12345) % (uint)limit); }
        }
    }
}
