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

namespace StarNight.Map.WorldGeneration.SV5.Foundation
{
    /// <summary>Bounded, physical small-run plan. SV5 keeps the existing
    /// SV5/08/09 contracts while making the finalized 500-pattern catalog
    /// the direct source for its non-composer selections.</summary>
    public enum Sv5SmallRunRecipe { PortGalleryV1 = 0 }

    public sealed class Sv5SmallRunRequest
    {
        public Sv5SmallRunRequest(int seed, int width, int height, Sv5SmallRunRecipe recipe)
        {
            Seed = seed; Width = width; Height = height; Recipe = recipe;
        }
        public int Seed { get; }
        public int Width { get; }
        public int Height { get; }
        public Sv5SmallRunRecipe Recipe { get; }
        public string RecipeId => Recipe.ToString();
    }

    public sealed class Sv5SmallRunPatternSelection
    {
        internal Sv5SmallRunPatternSelection(int slotX, int slotY, Sv5PatternCandidate candidate,
            Sv5PatternTransform transform, IEnumerable<Sv5PatternBaseCell> cells)
            : this(slotX, slotY, candidate == null ? throw new ArgumentNullException(nameof(candidate)) : candidate.CandidateId,
                transform, cells)
        {
        }

        internal Sv5SmallRunPatternSelection(int slotX, int slotY, string candidateId,
            Sv5PatternTransform transform, IEnumerable<Sv5PatternBaseCell> cells)
        {
            SlotX = slotX; SlotY = slotY; CandidateId = candidateId ?? throw new ArgumentNullException(nameof(candidateId)); Transform = transform;
            FinalCells = new ReadOnlyCollection<Sv5PatternBaseCell>((cells ?? Array.Empty<Sv5PatternBaseCell>()).ToArray());
        }
        public int SlotX { get; }
        public int SlotY { get; }
        public string CandidateId { get; }
        public Sv5PatternTransform Transform { get; }
        public IReadOnlyList<Sv5PatternBaseCell> FinalCells { get; }
        public string FinalCells16 => Sv5PatternCatalog.SerializeBaseCells16(FinalCells);
    }

    public sealed class Sv5SmallRunChunk
    {
        internal Sv5SmallRunChunk(string instanceId, int originX, int originY, Sv5PortChunk source,
            IEnumerable<Sv5SmallRunPatternSelection> selections, IEnumerable<Sv5PatternBaseCell> baseCells,
            IEnumerable<Sv5ComposerOverlayCell> overlays, int attemptCount)
        {
            InstanceId = instanceId; OriginX = originX; OriginY = originY; Source = source;
            Selections = new ReadOnlyCollection<Sv5SmallRunPatternSelection>((selections ?? Array.Empty<Sv5SmallRunPatternSelection>()).ToArray());
            BaseCells = new ReadOnlyCollection<Sv5PatternBaseCell>((baseCells ?? Array.Empty<Sv5PatternBaseCell>()).ToArray());
            Overlays = new ReadOnlyCollection<Sv5ComposerOverlayCell>((overlays ?? Array.Empty<Sv5ComposerOverlayCell>()).ToArray());
            AttemptCount = attemptCount;
        }
        public string InstanceId { get; }
        public int OriginX { get; }
        public int OriginY { get; }
        public Sv5PortChunk Source { get; }
        public IReadOnlyList<Sv5SmallRunPatternSelection> Selections { get; }
        public IReadOnlyList<Sv5PatternBaseCell> BaseCells { get; }
        public IReadOnlyList<Sv5ComposerOverlayCell> Overlays { get; }
        public int AttemptCount { get; }
        public Sv5PatternBaseCell GetBaseCell(int x, int y) => BaseCells[(y * Sv5PortCatalog.ChunkWidth) + x];
    }

    public sealed class Sv5SmallRunPort
    {
        internal Sv5SmallRunPort(Sv5SmallRunChunk chunk, Sv5EdgePort port)
        {
            ChunkInstanceId = chunk.InstanceId; ChunkId = chunk.Source.ChunkId; PortId = port.PortId;
            Side = port.Side; TraversalKind = port.TraversalKind; FlowDirection = port.FlowDirection;
            Required = port.Required;
            GlobalCells = new ReadOnlyCollection<Vector2Int>(port.OpenCells.Select(value =>
            {
                Sv5PortCell local = Sv5PortCatalog.ToChunkCell(port.Side, value);
                return new Vector2Int(chunk.OriginX + local.X, chunk.OriginY + local.Y);
            }).ToArray());
        }
        public string ChunkInstanceId { get; }
        public string ChunkId { get; }
        public string PortId { get; }
        public Sv5PortSide Side { get; }
        public Sv5PortTraversalKind TraversalKind { get; }
        public Sv5PortFlowDirection FlowDirection { get; }
        public bool Required { get; }
        public IReadOnlyList<Vector2Int> GlobalCells { get; }
    }

    public sealed class Sv5SmallRunPlan
    {
        internal Sv5SmallRunPlan(Sv5SmallRunRequest request, IEnumerable<Sv5SmallRunChunk> chunks,
            IEnumerable<Sv5SmallRunPort> ports, IEnumerable<string> failures, string poolVersion)
        {
            Request = request;
            Chunks = new ReadOnlyCollection<Sv5SmallRunChunk>((chunks ?? Array.Empty<Sv5SmallRunChunk>()).ToArray());
            Ports = new ReadOnlyCollection<Sv5SmallRunPort>((ports ?? Array.Empty<Sv5SmallRunPort>()).ToArray());
            Failures = new ReadOnlyCollection<string>((failures ?? Array.Empty<string>()).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray());
            PoolVersion = poolVersion ?? string.Empty;
            BaseDigest = Digest("SV5_BASE_V1", Chunks.SelectMany(chunk => chunk.BaseCells.Select((cell, index) =>
                chunk.InstanceId + ":" + index.ToString(CultureInfo.InvariantCulture) + ":" + cell)));
            PlanDigest = Digest("SV5_PLAN_V1", new[]
            {
                request == null ? string.Empty : request.Seed.ToString(CultureInfo.InvariantCulture),
                request == null ? string.Empty : request.Width + "x" + request.Height,
                request == null ? string.Empty : request.RecipeId,
                PoolVersion,
                BaseDigest,
                string.Join(";", Chunks.Select(chunk => chunk.InstanceId + "=" + chunk.Source.ChunkId + ":" + string.Join("|", chunk.Selections.Select(selection => selection.CandidateId + ":" + selection.Transform)))),
                string.Join(";", Ports.Select(port => port.ChunkInstanceId + ":" + port.PortId + ":" + string.Join("/", port.GlobalCells))),
            });
        }
        public Sv5SmallRunRequest Request { get; }
        public IReadOnlyList<Sv5SmallRunChunk> Chunks { get; }
        public IReadOnlyList<Sv5SmallRunPort> Ports { get; }
        public IReadOnlyList<string> Failures { get; }
        public bool Success => Request != null && Failures.Count == 0;
        public string FailureSummary => string.Join(";", Failures);
        public string BaseDigest { get; }
        public string PlanDigest { get; }
        public string PoolVersion { get; }
        public Vector2Int StartTile => new Vector2Int(1, 1);
        public Vector2Int ExitTile => Request == null ? Vector2Int.zero : new Vector2Int(Request.Width - 2, 1);
        public int MaximumAttempts => 3;
        public int TotalSelectionAttempts => Chunks.Sum(value => value.AttemptCount);
        public bool IsSolid(int x, int y)
        {
            Sv5SmallRunChunk chunk = Chunks.FirstOrDefault(value => x >= value.OriginX && x < value.OriginX + Sv5PortCatalog.ChunkWidth && y >= value.OriginY && y < value.OriginY + Sv5PortCatalog.ChunkHeight);
            return chunk != null && chunk.GetBaseCell(x - chunk.OriginX, y - chunk.OriginY) == Sv5PatternBaseCell.Solid;
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
    public sealed class Sv5SmallRunSceneState : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private Sv5SmallRunRecipe recipe;
        [SerializeField] private string planDigest;
        [SerializeField] private string poolVersion;
        public int Seed => seed;
        public int Width => width;
        public int Height => height;
        public Sv5SmallRunRecipe Recipe => recipe;
        public string PlanDigest => planDigest;
        public string PoolVersion => poolVersion;
        public Sv5SmallRunRequest Request => new Sv5SmallRunRequest(seed, width, height, recipe);
        public void Configure(Sv5SmallRunPlan plan)
        {
            if (plan == null || !plan.Success) throw new ArgumentException("A successful SV5 plan is required.", nameof(plan));
            seed = plan.Request.Seed; width = plan.Request.Width; height = plan.Request.Height;
            recipe = plan.Request.Recipe; planDigest = plan.PlanDigest; poolVersion = plan.PoolVersion;
        }
    }

    public static class Sv5SmallRunHarness
    {
        public const int MinimumWidth = 36;
        public const int MinimumHeight = 24;
        public const int MaximumWidth = 48;
        public const int MaximumHeight = 24;
        private const string FloorCandidateId = "SV5_6F5D1DF424AA";
        private const string ClearCandidateId = "SV5_991204FBA2B6";

        public static Sv5SmallRunPlan Generate(Sv5SmallRunRequest request)
        {
            var failures = new List<string>();
            if (request == null) failures.Add("MISSING_REQUEST");
            else
            {
                if (request.Width % Sv5PortCatalog.ChunkWidth != 0 || request.Height % Sv5PortCatalog.ChunkHeight != 0)
                    failures.Add("SIZE_NOT_CHUNK_ALIGNED:" + request.Width + "x" + request.Height);
                if (request.Width < MinimumWidth || request.Width > MaximumWidth || request.Height != MinimumHeight)
                    failures.Add("UNSUPPORTED_SIZE:" + request.Width + "x" + request.Height + ";SUPPORTED=36x24,48x24");
                if (request.Recipe != Sv5SmallRunRecipe.PortGalleryV1) failures.Add("UNSUPPORTED_RECIPE:" + request.Recipe);
            }
            if (failures.Count != 0) return new Sv5SmallRunPlan(request, Array.Empty<Sv5SmallRunChunk>(), Array.Empty<Sv5SmallRunPort>(), failures,
                Sv5PatternPool500.DataVersion);

            Sv5PortCatalogSnapshot ports = Sv5PortCatalog.BuildFixture();
            if (!Sv5PortCatalog.Validate(ports).IsValid) failures.Add("PORT_CATALOG_INVALID");
            var chunks = new List<Sv5SmallRunChunk>();
            int columns = request.Width / Sv5PortCatalog.ChunkWidth;
            int rows = request.Height / Sv5PortCatalog.ChunkHeight;
            for (var y = 0; y < rows; y++)
            for (var x = 0; x < columns; x++)
            {
                string sourceId = SourceFor(x, y, columns);
                if (!ports.TryGetChunk(sourceId, out Sv5PortChunk source))
                {
                    failures.Add("MISSING_PORT_CHUNK:" + sourceId);
                    continue;
                }
                Sv5SmallRunChunk chunk = CreateChunk(request, source, x, y, chunks.Count, failures);
                chunks.Add(chunk);
            }
            var globalPorts = chunks.SelectMany(chunk => chunk.Source.Ports.Select(port => new Sv5SmallRunPort(chunk, port))).ToList();
            ValidatePlan(request, chunks, globalPorts, failures);
            return new Sv5SmallRunPlan(request, chunks, globalPorts, failures, Sv5PatternPool500.DataVersion);
        }

        private static string SourceFor(int x, int y, int columns)
        {
            if (y == 0) return x == 1 ? "T1_B" : "T1_A";
            if (y == 1) return x == 0 ? "T2_DROP" : x == 1 ? "T3_CLIMB" : x == 2 ? "T4_SPLIT" : "T0_SINGLE_ENTRANCE";
            if (y == 2) return x == 0 ? "T0_BREAKABLE_SECRET" : x == 1 ? "T0_SINGLE_ENTRANCE" : x == 2 ? "T1_B" : "T4_SPLIT";
            return "INACTIVE_SOLID_WALL";
        }

        private static Sv5SmallRunChunk CreateChunk(Sv5SmallRunRequest request, Sv5PortChunk source,
            int chunkX, int chunkY, int ordinal, ICollection<string> failures)
        {
            int originX = chunkX * Sv5PortCatalog.ChunkWidth;
            int originY = chunkY * Sv5PortCatalog.ChunkHeight;
            string instanceId = "C" + chunkX.ToString(CultureInfo.InvariantCulture) + "_" + chunkY.ToString(CultureInfo.InvariantCulture);
            if (source.ChunkId == "T3_CLIMB")
            {
                Sv5ComposerResult composed = Sv5Composer.Compose(Sv5Composer.CreateFixtureRequest());
                if (!composed.Success) failures.Add("T3_COMPOSER_FAILED:" + composed.FailureSummary);
                Sv5ComposerComposition composition = composed.Composition;
                return new Sv5SmallRunChunk(instanceId, originX, originY, source,
                    composition == null ? Array.Empty<Sv5SmallRunPatternSelection>() : composition.Selections.Select(selection =>
                        new Sv5SmallRunPatternSelection(selection.SlotX, selection.SlotY, selection.Candidate,
                            selection.Transform, selection.FinalCells)),
                    composition == null ? Array.Empty<Sv5PatternBaseCell>() : composition.BaseCells,
                    composition == null ? Array.Empty<Sv5ComposerOverlayCell>() : composition.Overlays,
                    composed.AttemptCount);
            }

            Sv5PatternPool500Snapshot pool = Sv5PatternPool500.BuildFinalPool();
            Sv5PatternPool500Entry floor = pool.Candidates.Single(value => value.CandidateId == FloorCandidateId);
            Sv5PatternPool500Entry clear = pool.Candidates.Single(value => value.CandidateId == ClearCandidateId);
            Sv5PatternPool500Entry detail = Sv5PatternPool500.SelectPortSafeDetail(request.Seed, ordinal);
            Sv5PatternPool500Entry oneWay = pool.Candidates.Single(value => value.CandidateId == "SV5_348D65F87C81");
            var selections = new List<Sv5SmallRunPatternSelection>();
            for (var slotY = 0; slotY < 2; slotY++)
            for (var slotX = 0; slotX < 3; slotX++)
            {
                Sv5PatternPool500Entry candidate;
                if (source.SpaceState == Sv5PortSpaceState.InactiveSolid || source.ChunkId == "T0_BREAKABLE_SECRET") candidate = clear;
                else if (slotY == 0) candidate = source.ChunkType == Sv5PortChunkType.Type2 || source.ChunkType == Sv5PortChunkType.Type4 ? clear : floor;
                else if (source.ChunkId == "T0_SINGLE_ENTRANCE" && slotX == 0) candidate = oneWay;
                else candidate = (slotX == 1 || source.ChunkType == Sv5PortChunkType.Type4) ? clear : detail;
                Sv5PatternTransform transform = candidate == detail ? (Sv5PatternTransform)(StableIndex(request.Seed + 17, ordinal + slotX + (slotY * 3), 4)) : Sv5PatternTransform.R0;
                selections.Add(new Sv5SmallRunPatternSelection(slotX * Sv5PatternCatalog.Width, slotY * Sv5PatternCatalog.Height,
                    candidate.CandidateId, transform, Sv5PatternCatalog.TransformCells(candidate.BaseCells, transform)));
            }
            Sv5PatternBaseCell[] baseCells = AssembleSelections(selections);
            if (source.SpaceState == Sv5PortSpaceState.InactiveSolid || source.ChunkId == "T0_BREAKABLE_SECRET")
            {
                baseCells = Enumerable.Range(0, Sv5PortCatalog.ChunkCellCount).Select(index =>
                    source.IsSolid(index % Sv5PortCatalog.ChunkWidth, index / Sv5PortCatalog.ChunkWidth)
                        ? Sv5PatternBaseCell.Solid : Sv5PatternBaseCell.Air).ToArray();
            }
            foreach (Sv5EdgePort port in source.Ports)
            foreach (int coordinate in port.OpenCells)
            {
                Sv5PortCell cell = Sv5PortCatalog.ToChunkCell(port.Side, coordinate);
                if (baseCells[(cell.Y * Sv5PortCatalog.ChunkWidth) + cell.X] != Sv5PatternBaseCell.Air)
                    failures.Add("PORT_BLOCKED:" + instanceId + ":" + port.PortId + ":" + coordinate.ToString(CultureInfo.InvariantCulture));
            }
            return new Sv5SmallRunChunk(instanceId, originX, originY, source, selections, baseCells,
                Array.Empty<Sv5ComposerOverlayCell>(), 1);
        }

        private static Sv5PatternBaseCell[] AssembleSelections(IEnumerable<Sv5SmallRunPatternSelection> selections)
        {
            var cells = Enumerable.Repeat(Sv5PatternBaseCell.Air, Sv5PortCatalog.ChunkCellCount).ToArray();
            foreach (Sv5SmallRunPatternSelection selection in selections)
            for (var y = 0; y < Sv5PatternCatalog.Height; y++)
            for (var x = 0; x < Sv5PatternCatalog.Width; x++)
                cells[((selection.SlotY + y) * Sv5PortCatalog.ChunkWidth) + selection.SlotX + x] = selection.FinalCells[(y * Sv5PatternCatalog.Width) + x];
            return cells;
        }

        private static void ValidatePlan(Sv5SmallRunRequest request, IEnumerable<Sv5SmallRunChunk> chunks,
            IEnumerable<Sv5SmallRunPort> ports, ICollection<string> failures)
        {
            Sv5SmallRunChunk[] all = chunks.ToArray();
            if (all.Length != (request.Width / 12) * (request.Height / 8)) failures.Add("CHUNK_COUNT_MISMATCH");
            if (all.Any(chunk => chunk.BaseCells.Count != Sv5PortCatalog.ChunkCellCount || chunk.Selections.Count != 6)) failures.Add("CHUNK_96_CELL_OR_6_SELECTION_MISMATCH");
            if (!all.Any(chunk => chunk.Source.ChunkType == Sv5PortChunkType.Type1) || !all.Any(chunk => chunk.Source.ChunkType == Sv5PortChunkType.Type2) ||
                !all.Any(chunk => chunk.Source.ChunkType == Sv5PortChunkType.Type3) || !all.Any(chunk => chunk.Source.ChunkType == Sv5PortChunkType.Type4)) failures.Add("MISSING_TYPE1_TO_TYPE4");
            if (all.Count(chunk => chunk.Source.ChunkType == Sv5PortChunkType.Type0) < 2 || !all.Any(chunk => chunk.Source.ChunkId == "T0_BREAKABLE_SECRET")) failures.Add("MISSING_TYPE0_FORMS");
            if (!ports.Any(port => port.TraversalKind == Sv5PortTraversalKind.Drop) || !ports.Any(port => port.TraversalKind == Sv5PortTraversalKind.Climb)) failures.Add("MISSING_DIRECTIONAL_PORTS");
            if (!string.Equals(Sv5PortCatalog.BuildFixture().ProfileDigest, GeneratedTraversalProfileCatalog.Create().Digest, StringComparison.Ordinal)) failures.Add("PROFILE_DIGEST_MISMATCH");
            if (request.Width <= 2 || request.Height <= 1 || all.FirstOrDefault(chunk => chunk.OriginY == 0) == null) failures.Add("START_EXIT_OUTSIDE_PLAN");
        }

        private static int StableIndex(int seed, int ordinal, int limit)
        {
            unchecked { return (int)((uint)(seed * 1103515245 + ordinal * 12345) % (uint)limit); }
        }
    }
}
