using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    /// <summary>
    /// RMAP07's typed 4x4 base-geometry catalog.  This deliberately does not
    /// model ports or 12x8 composition; RMAP08/RMAP09 own those contracts.
    /// Coordinates are lower-left origin, x right, y up, index = y * 4 + x.
    /// </summary>
    public enum RmapPatternBaseCell
    {
        Air = 0,
        Solid = 1,
        OneWayPlatform = 2,
    }

    public enum RmapPatternPrimaryRole
    {
        SlopeRiseRight = 0,
        SlopeRiseLeft = 1,
        CeilingFlat = 2,
        CeilingRough = 3,
        WallLeft = 4,
        WallRight = 5,
        VoidClear = 6,
        SparseAirPlatform = 7,
        StandableLedge = 8,
        VerticalPassage = 9,
    }

    public enum RmapPatternTransform
    {
        R0 = 0,
        MirrorX = 1,
        MirrorY = 2,
        R180 = 3,
    }

    public enum RmapPatternEdge
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3,
    }

    [Flags]
    public enum RmapPatternIntentTag : uint
    {
        None = 0,
        FlatFloor = 1u << 0,
        StepFloor = 1u << 1,
        ShortLedge = 1u << 2,
        LongLedge = 1u << 3,
        Takeoff = 1u << 4,
        Landing = 1u << 5,
        RunApproach = 1u << 6,
        GrabbableCorner = 1u << 7,
        LowCeiling = 1u << 8,
        OneTilePassage = 1u << 9,
        VerticalClearance = 1u << 10,
        LeftEntry = 1u << 11,
        RightExit = 1u << 12,
        UpEntry = 1u << 13,
        DownExit = 1u << 14,
        RequiredOk = 1u << 15,
        OptionalOnly = 1u << 16,
        SecretShell = 1u << 17,
        QuietFill = 1u << 18,
        LadderAllowed = 1u << 19,
        PillarAllowed = 1u << 20,
        HazardAllowed = 1u << 21,
        MechanismAllowed = 1u << 22,
        RewardAllowed = 1u << 23,
    }

    public readonly struct RmapPatternLocalCell : IEquatable<RmapPatternLocalCell>
    {
        public RmapPatternLocalCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public bool Equals(RmapPatternLocalCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is RmapPatternLocalCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," +
                                             Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class RmapPatternSource
    {
        internal RmapPatternSource(
            string sourcePatternId,
            string sourceMaskReference,
            string originalReference,
            RmapPatternIntentTag intentTags,
            IEnumerable<RmapPatternBaseCell> baseCells)
        {
            SourcePatternId = sourcePatternId ?? throw new ArgumentNullException(nameof(sourcePatternId));
            SourceMaskReference = sourceMaskReference ?? string.Empty;
            OriginalReference = originalReference ?? string.Empty;
            IntentTags = intentTags;
            BaseCells = new ReadOnlyCollection<RmapPatternBaseCell>((baseCells ??
                throw new ArgumentNullException(nameof(baseCells))).ToArray());
            if (BaseCells.Count != RmapPatternCatalog.CellCount)
            {
                throw new ArgumentException("A source pattern must contain exactly 16 base cells.",
                    nameof(baseCells));
            }
        }

        public string SourcePatternId { get; }
        public string SourceMaskReference { get; }
        public string OriginalReference { get; }
        public RmapPatternIntentTag IntentTags { get; }
        public IReadOnlyList<RmapPatternBaseCell> BaseCells { get; }
    }

    public sealed class RmapPatternOrigin
    {
        internal RmapPatternOrigin(
            RmapPatternSource source,
            RmapPatternTransform transform,
            RmapPatternIntentTag transformedTags,
            IEnumerable<string> unresolvedDirectionalTags)
        {
            SourcePatternId = source.SourcePatternId;
            SourceMaskReference = source.SourceMaskReference;
            OriginalReference = source.OriginalReference;
            Transform = transform;
            IntentTags = transformedTags;
            UnresolvedDirectionalTags = new ReadOnlyCollection<string>((unresolvedDirectionalTags ??
                Array.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(value => value,
                StringComparer.Ordinal).ToArray());
        }

        public string SourcePatternId { get; }
        public string SourceMaskReference { get; }
        public string OriginalReference { get; }
        public RmapPatternTransform Transform { get; }
        public RmapPatternIntentTag IntentTags { get; }
        public IReadOnlyList<string> UnresolvedDirectionalTags { get; }
    }

    public sealed class RmapPatternEdgeCell
    {
        internal RmapPatternEdgeCell(RmapPatternLocalCell coordinate, RmapPatternBaseCell cell)
        {
            Coordinate = coordinate;
            Cell = cell;
        }

        public RmapPatternLocalCell Coordinate { get; }
        public RmapPatternBaseCell Cell { get; }
    }

    public sealed class RmapPatternStandableSpan
    {
        internal RmapPatternStandableSpan(int y, int startX, int endX, int headroomCells,
            bool contextRequired)
        {
            Y = y;
            StartX = startX;
            EndX = endX;
            HeadroomCells = headroomCells;
            ContextRequired = contextRequired;
        }

        public int Y { get; }
        public int StartX { get; }
        public int EndX { get; }
        public int Length => EndX - StartX + 1;
        public int HeadroomCells { get; }
        public bool ContextRequired { get; }
    }

    public sealed class RmapPatternAirComponent
    {
        internal RmapPatternAirComponent(IEnumerable<RmapPatternLocalCell> cells)
        {
            Cells = new ReadOnlyCollection<RmapPatternLocalCell>((cells ?? Array.Empty<RmapPatternLocalCell>())
                .OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
        }

        public IReadOnlyList<RmapPatternLocalCell> Cells { get; }
    }

    public sealed class RmapPatternAutomaticCharacteristics
    {
        internal RmapPatternAutomaticCharacteristics(
            IDictionary<RmapPatternEdge, IReadOnlyList<RmapPatternEdgeCell>> edgeOpenCellSets,
            IEnumerable<RmapPatternStandableSpan> standableSpans,
            IEnumerable<RmapPatternLocalCell> launchCells,
            IEnumerable<RmapPatternLocalCell> landingCells,
            IEnumerable<RmapPatternLocalCell> grabCorners,
            IEnumerable<int> fallColumns,
            IEnumerable<RmapPatternLocalCell> climbOverlayCells,
            int solidCount,
            int airCount,
            int oneWayCount,
            IEnumerable<RmapPatternAirComponent> airComponents,
            IEnumerable<RmapPatternTransform> symmetricTransforms,
            IEnumerable<string> contextRequired)
        {
            EdgeOpenCellSets = new ReadOnlyDictionary<RmapPatternEdge, IReadOnlyList<RmapPatternEdgeCell>>(
                new Dictionary<RmapPatternEdge, IReadOnlyList<RmapPatternEdgeCell>>(edgeOpenCellSets));
            StandableSpans = new ReadOnlyCollection<RmapPatternStandableSpan>((standableSpans ??
                Array.Empty<RmapPatternStandableSpan>()).ToArray());
            LaunchCells = SortedCells(launchCells);
            LandingCells = SortedCells(landingCells);
            GrabCorners = SortedCells(grabCorners);
            FallColumns = new ReadOnlyCollection<int>((fallColumns ?? Array.Empty<int>()).Distinct().OrderBy(x => x).ToArray());
            ClimbOverlayCells = SortedCells(climbOverlayCells);
            SolidCount = solidCount;
            AirCount = airCount;
            OneWayCount = oneWayCount;
            SolidRatio = solidCount / (double)RmapPatternCatalog.CellCount;
            AirRatio = airCount / (double)RmapPatternCatalog.CellCount;
            OneWayRatio = oneWayCount / (double)RmapPatternCatalog.CellCount;
            AirComponents = new ReadOnlyCollection<RmapPatternAirComponent>((airComponents ??
                Array.Empty<RmapPatternAirComponent>()).ToArray());
            SymmetricTransforms = new ReadOnlyCollection<RmapPatternTransform>((symmetricTransforms ??
                Array.Empty<RmapPatternTransform>()).Distinct().OrderBy(value => value).ToArray());
            ContextRequired = new ReadOnlyCollection<string>((contextRequired ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public IReadOnlyDictionary<RmapPatternEdge, IReadOnlyList<RmapPatternEdgeCell>> EdgeOpenCellSets { get; }
        public IReadOnlyList<RmapPatternStandableSpan> StandableSpans { get; }
        public IReadOnlyList<RmapPatternLocalCell> LaunchCells { get; }
        public IReadOnlyList<RmapPatternLocalCell> LandingCells { get; }
        public IReadOnlyList<RmapPatternLocalCell> GrabCorners { get; }
        public IReadOnlyList<int> FallColumns { get; }
        public IReadOnlyList<RmapPatternLocalCell> ClimbOverlayCells { get; }
        public int SolidCount { get; }
        public int AirCount { get; }
        public int OneWayCount { get; }
        public double SolidRatio { get; }
        public double AirRatio { get; }
        public double OneWayRatio { get; }
        public IReadOnlyList<RmapPatternAirComponent> AirComponents { get; }
        public IReadOnlyList<RmapPatternTransform> SymmetricTransforms { get; }
        public IReadOnlyList<string> ContextRequired { get; }

        private static IReadOnlyList<RmapPatternLocalCell> SortedCells(IEnumerable<RmapPatternLocalCell> values) =>
            new ReadOnlyCollection<RmapPatternLocalCell>((values ?? Array.Empty<RmapPatternLocalCell>())
                .Distinct().OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
    }

    public sealed class RmapPatternCandidate
    {
        internal RmapPatternCandidate(
            string candidateId,
            IEnumerable<RmapPatternBaseCell> baseCells,
            RmapPatternPrimaryRole primaryRole,
            RmapPatternIntentTag intentTags,
            IEnumerable<RmapPatternOrigin> origins,
            IEnumerable<string> tagConflicts,
            RmapPatternAutomaticCharacteristics characteristics)
        {
            CandidateId = candidateId;
            BaseCells = new ReadOnlyCollection<RmapPatternBaseCell>(baseCells.ToArray());
            PrimaryRole = primaryRole;
            IntentTags = intentTags;
            Origins = new ReadOnlyCollection<RmapPatternOrigin>(origins.OrderBy(value => value.SourcePatternId,
                StringComparer.Ordinal).ThenBy(value => value.Transform).ToArray());
            TagConflicts = new ReadOnlyCollection<string>((tagConflicts ?? Array.Empty<string>()).Distinct(
                StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Characteristics = characteristics;
        }

        public string CandidateId { get; }
        public IReadOnlyList<RmapPatternBaseCell> BaseCells { get; }
        public RmapPatternPrimaryRole PrimaryRole { get; }
        public RmapPatternIntentTag IntentTags { get; }
        public IReadOnlyList<RmapPatternOrigin> Origins { get; }
        public IReadOnlyList<string> TagConflicts { get; }
        public RmapPatternAutomaticCharacteristics Characteristics { get; }

        public RmapPatternBaseCell GetCell(int x, int y)
        {
            if (x < 0 || x >= RmapPatternCatalog.Width || y < 0 || y >= RmapPatternCatalog.Height)
            {
                throw new ArgumentOutOfRangeException();
            }

            return BaseCells[y * RmapPatternCatalog.Width + x];
        }
    }

    public sealed class RmapPatternCatalogSnapshot
    {
        internal RmapPatternCatalogSnapshot(
            IEnumerable<RmapPatternCandidate> candidates,
            int generatedDistinctBaseGeometryCount,
            int sourceTransformRelationCount)
        {
            Candidates = new ReadOnlyCollection<RmapPatternCandidate>((candidates ??
                Array.Empty<RmapPatternCandidate>()).OrderBy(value => value.CandidateId,
                StringComparer.Ordinal).ToArray());
            GeneratedDistinctBaseGeometryCount = generatedDistinctBaseGeometryCount;
            SourceTransformRelationCount = sourceTransformRelationCount;
        }

        public IReadOnlyList<RmapPatternCandidate> Candidates { get; }
        public int GeneratedDistinctBaseGeometryCount { get; }
        public int SourceTransformRelationCount { get; }
        public int RelationsCollapsedByBaseGeometry => SourceTransformRelationCount -
            GeneratedDistinctBaseGeometryCount;
        public int ExcludedAfterInitialPoolCount => GeneratedDistinctBaseGeometryCount - Candidates.Count;

        public bool TryGetCandidate(string candidateId, out RmapPatternCandidate candidate)
        {
            candidate = Candidates.FirstOrDefault(value => string.Equals(value.CandidateId, candidateId,
                StringComparison.Ordinal));
            return candidate != null;
        }
    }

    public static class RmapPatternCatalog
    {
        public const int Width = 4;
        public const int Height = 4;
        public const int CellCount = Width * Height;
        public const int InitialPoolCount = 48;
        public const string DataVersion = "RMAP07_FIRST_POOL_V1";
        public const string LegacyMaskSource =
            "MapDesign/MCP/GENERATED/VIS01/moonpalace_vis01_pattern_candidates_500.json";

        private static readonly Lazy<RmapPatternCatalogSnapshot> InitialPool =
            new Lazy<RmapPatternCatalogSnapshot>(BuildInitialPoolInternal);

        public static RmapPatternCatalogSnapshot BuildInitialPool() => InitialPool.Value;

        public static IReadOnlyList<RmapPatternBaseCell> TransformCells(
            IReadOnlyList<RmapPatternBaseCell> source,
            RmapPatternTransform transform)
        {
            if (source == null || source.Count != CellCount)
            {
                throw new ArgumentException("Exactly 16 source cells are required.", nameof(source));
            }

            var result = new RmapPatternBaseCell[CellCount];
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    int targetX;
                    int targetY;
                    switch (transform)
                    {
                        case RmapPatternTransform.R0: targetX = x; targetY = y; break;
                        case RmapPatternTransform.MirrorX: targetX = Width - 1 - x; targetY = y; break;
                        case RmapPatternTransform.MirrorY: targetX = x; targetY = Height - 1 - y; break;
                        case RmapPatternTransform.R180: targetX = Width - 1 - x; targetY = Height - 1 - y; break;
                        default: throw new ArgumentOutOfRangeException(nameof(transform));
                    }

                    result[targetY * Width + targetX] = source[y * Width + x];
                }
            }

            return new ReadOnlyCollection<RmapPatternBaseCell>(result);
        }

        public static RmapPatternPrimaryRole Classify(IReadOnlyList<RmapPatternBaseCell> cells)
        {
            EnsureCells(cells);
            if (cells.All(value => value == RmapPatternBaseCell.Air)) return RmapPatternPrimaryRole.VoidClear;
            if (cells.Count(value => value == RmapPatternBaseCell.OneWayPlatform) > 0 &&
                cells.Count(value => value == RmapPatternBaseCell.Solid) <= 2)
                return RmapPatternPrimaryRole.SparseAirPlatform;
            if (IsVerticalPassage(cells)) return RmapPatternPrimaryRole.VerticalPassage;
            int slope = SlopeDirection(cells);
            if (slope > 0) return RmapPatternPrimaryRole.SlopeRiseRight;
            if (slope < 0) return RmapPatternPrimaryRole.SlopeRiseLeft;
            if (IsFullColumn(cells, 0) && !IsFullColumn(cells, Width - 1)) return RmapPatternPrimaryRole.WallLeft;
            if (IsFullColumn(cells, Width - 1) && !IsFullColumn(cells, 0)) return RmapPatternPrimaryRole.WallRight;
            if (IsFlatCeiling(cells)) return RmapPatternPrimaryRole.CeilingFlat;
            if (TopCount(cells) >= 2 && BottomCount(cells) < TopCount(cells))
                return RmapPatternPrimaryRole.CeilingRough;
            return RmapPatternPrimaryRole.StandableLedge;
        }

        public static RmapPatternAutomaticCharacteristics CalculateCharacteristics(
            IReadOnlyList<RmapPatternBaseCell> cells)
        {
            EnsureCells(cells);
            var edges = new Dictionary<RmapPatternEdge, IReadOnlyList<RmapPatternEdgeCell>>();
            foreach (RmapPatternEdge edge in Enum.GetValues(typeof(RmapPatternEdge)))
            {
                var values = EdgeCoordinates(edge).Select(coordinate => new RmapPatternEdgeCell(coordinate,
                        Cell(cells, coordinate.X, coordinate.Y)))
                    .Where(value => value.Cell != RmapPatternBaseCell.Solid).ToArray();
                edges.Add(edge, new ReadOnlyCollection<RmapPatternEdgeCell>(values));
            }

            var spans = FindStandableSpans(cells).ToArray();
            var launch = new List<RmapPatternLocalCell>();
            var landing = new List<RmapPatternLocalCell>();
            var grab = new List<RmapPatternLocalCell>();
            var fallColumns = new List<int>();
            var climb = new List<RmapPatternLocalCell>();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var coordinate = new RmapPatternLocalCell(x, y);
                    var value = Cell(cells, x, y);
                    if (value == RmapPatternBaseCell.Air)
                    {
                        climb.Add(coordinate);
                        if (y > 0 && IsStandable(Cell(cells, x, y - 1)))
                        {
                            launch.Add(coordinate);
                            landing.Add(coordinate);
                        }
                    }
                    else if (value == RmapPatternBaseCell.Solid && HasExposedSolidCorner(cells, x, y))
                    {
                        grab.Add(coordinate);
                    }
                }

            }

            for (var x = 0; x < Width; x++)
            {
                if (Enumerable.Range(0, Height).All(y => Cell(cells, x, y) == RmapPatternBaseCell.Air))
                {
                    fallColumns.Add(x);
                }
            }

            var symmetric = Enum.GetValues(typeof(RmapPatternTransform)).Cast<RmapPatternTransform>()
                .Where(transform => SameCells(cells, TransformCells(cells, transform))).ToArray();
            var context = new List<string>();
            if (spans.Any(value => value.ContextRequired)) context.Add("HEADROOM_OUTSIDE_4X4_UNKNOWN");
            if (edges.Values.Any(value => value.Count > 0)) context.Add("EDGE_NEIGHBOR_PORT_CONTEXT_REQUIRED");
            if (launch.Count > 0 || landing.Count > 0 || cells.Any(value => value == RmapPatternBaseCell.OneWayPlatform))
                context.Add("JUMP_ARC_AND_PLAYER_PROFILE_CONTEXT_REQUIRED");
            return new RmapPatternAutomaticCharacteristics(edges, spans, launch, landing, grab, fallColumns,
                climb, cells.Count(value => value == RmapPatternBaseCell.Solid),
                cells.Count(value => value == RmapPatternBaseCell.Air),
                cells.Count(value => value == RmapPatternBaseCell.OneWayPlatform),
                FindAirComponents(cells), symmetric, context);
        }

        public static string SerializeBaseCells16(IReadOnlyList<RmapPatternBaseCell> cells)
        {
            EnsureCells(cells);
            return new string(cells.Select(ToToken).ToArray());
        }

        public static IReadOnlyList<RmapPatternBaseCell> ParseBaseCells16(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != CellCount)
                throw new FormatException("BaseCells16 must hold exactly sixteen A/S/O tokens.");
            var cells = value.Select(ParseToken).ToArray();
            return new ReadOnlyCollection<RmapPatternBaseCell>(cells);
        }

        public static string ExportCatalogCsv(RmapPatternCatalogSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var lines = new List<string>
            {
                "CandidateId,BaseCells16,PrimaryRole,IntentTags,DataVersion,SourceCount"
            };
            lines.AddRange(snapshot.Candidates.Select(candidate => Csv(candidate.CandidateId,
                SerializeBaseCells16(candidate.BaseCells), candidate.PrimaryRole.ToString(),
                Tags(candidate.IntentTags), DataVersion, candidate.Origins.Count.ToString(CultureInfo.InvariantCulture))));
            return string.Join("\n", lines) + "\n";
        }

        public static string ExportOriginsCsv(RmapPatternCatalogSnapshot snapshot)
        {
            var lines = new List<string>
            {
                "SourcePatternId,SourceMaskReference,OriginalReference,Transform,CandidateId,IntentTags,UnresolvedDirectionalTags,TagConflicts"
            };
            foreach (var candidate in snapshot.Candidates)
            foreach (var origin in candidate.Origins)
                lines.Add(Csv(origin.SourcePatternId, origin.SourceMaskReference, origin.OriginalReference,
                    origin.Transform.ToString(), candidate.CandidateId, Tags(origin.IntentTags),
                    string.Join(";", origin.UnresolvedDirectionalTags), string.Join(";", candidate.TagConflicts)));
            return string.Join("\n", lines) + "\n";
        }

        public static string ExportCharacteristicsCsv(RmapPatternCatalogSnapshot snapshot)
        {
            var lines = new List<string>
            {
                "CandidateId,EdgeOpenCellSet,StandableSpans,LaunchCells,LandingCells,GrabCorners,FallColumns,ClimbOverlayCells,SolidAirOneWay,AirComponents,SymmetricTransforms,ContextRequired"
            };
            foreach (var candidate in snapshot.Candidates)
            {
                var c = candidate.Characteristics;
                string edges = string.Join(";", c.EdgeOpenCellSets.OrderBy(value => value.Key).Select(value =>
                    value.Key + ":" + string.Join("/", value.Value.Select(cell => cell.Coordinate + "=" + cell.Cell))));
                string spans = string.Join(";", c.StandableSpans.Select(value => value.StartX + "-" +
                    value.EndX + "@" + value.Y + " headroom=" + value.HeadroomCells +
                    (value.ContextRequired ? "?" : string.Empty)));
                lines.Add(Csv(candidate.CandidateId, edges, spans, Cells(c.LaunchCells), Cells(c.LandingCells),
                    Cells(c.GrabCorners), string.Join(";", c.FallColumns), Cells(c.ClimbOverlayCells),
                    c.SolidCount + "/" + c.AirCount + "/" + c.OneWayCount,
                    string.Join(";", c.AirComponents.Select(component => Cells(component.Cells))),
                    string.Join(";", c.SymmetricTransforms), string.Join(";", c.ContextRequired)));
            }
            return string.Join("\n", lines) + "\n";
        }

        public static void ValidateCatalogCsv(string csv, int expectedRows)
        {
            if (string.IsNullOrEmpty(csv)) throw new FormatException("CSV is required.");
            string[] rows = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length != expectedRows + 1) throw new FormatException("Unexpected CSV row count.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 1; index < rows.Length; index++)
            {
                string[] fields = SplitCsv(rows[index]);
                if (fields.Length != 6 || !ids.Add(fields[0])) throw new FormatException("Duplicate or malformed catalog row.");
                ParseBaseCells16(fields[1]);
                if (!Enum.TryParse(fields[2], out RmapPatternPrimaryRole ignored))
                    throw new FormatException("Unknown primary role.");
            }
        }

        private static RmapPatternCatalogSnapshot BuildInitialPoolInternal()
        {
            var pending = new Dictionary<string, PendingCandidate>(StringComparer.Ordinal);
            foreach (var source in BuildSources().OrderBy(value => value.SourcePatternId, StringComparer.Ordinal))
            {
                foreach (RmapPatternTransform transform in Enum.GetValues(typeof(RmapPatternTransform)))
                {
                    IReadOnlyList<RmapPatternBaseCell> cells = TransformCells(source.BaseCells, transform);
                    string key = SerializeBaseCells16(cells);
                    if (!pending.TryGetValue(key, out PendingCandidate candidate))
                    {
                        candidate = new PendingCandidate(cells);
                        pending.Add(key, candidate);
                    }

                    TransformTags(source.IntentTags, transform, out RmapPatternIntentTag transformedTags,
                        out IReadOnlyList<string> unresolved);
                    candidate.Origins.Add(new RmapPatternOrigin(source, transform, transformedTags, unresolved));
                }
            }

            var all = pending.Values.Select(value => value.ToCandidate()).OrderBy(value => value.CandidateId,
                StringComparer.Ordinal).ToArray();
            var selected = new List<RmapPatternCandidate>();
            foreach (RmapPatternPrimaryRole role in Enum.GetValues(typeof(RmapPatternPrimaryRole)))
            {
                RmapPatternCandidate representative = all.FirstOrDefault(value => value.PrimaryRole == role);
                if (representative == null)
                    throw new InvalidOperationException("Initial pool has no representative for " + role + ".");
                selected.Add(representative);
            }
            foreach (var candidate in all)
            {
                if (selected.Count >= InitialPoolCount) break;
                if (!selected.Contains(candidate)) selected.Add(candidate);
            }
            if (selected.Count != InitialPoolCount)
                throw new InvalidOperationException("Initial pool must contain exactly 48 distinct base geometries.");
            if (selected.Count(value => value.PrimaryRole == RmapPatternPrimaryRole.VoidClear) != 1)
                throw new InvalidOperationException("VOID_CLEAR must be represented by exactly one candidate.");
            return new RmapPatternCatalogSnapshot(selected, all.Length,
                pending.Values.Sum(value => value.Origins.Count));
        }

        private static IEnumerable<RmapPatternSource> BuildSources()
        {
            // The first eight sources are direct conversions of the existing 500
            // VIS01 binary mask candidates.  In VIS01, bit index y*4+x is solid.
            foreach (MoonPalaceMicroPatternCandidate legacy in MoonPalaceMicroPatternCandidateLibrary.Build()
                         .Candidates.Take(8))
            {
                yield return new RmapPatternSource(legacy.CandidateId, legacy.MaskU16Hex, LegacyMaskSource,
                    RmapPatternIntentTag.QuietFill | RmapPatternIntentTag.OptionalOnly,
                    Enumerable.Range(0, CellCount).Select(index =>
                        (legacy.Mask & (1 << index)) != 0 ? RmapPatternBaseCell.Solid : RmapPatternBaseCell.Air));
            }

            yield return Seed("RMAP07_VOID", "", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.QuietFill | RmapPatternIntentTag.OptionalOnly,
                "....", "....", "....", "....");
            yield return Seed("RMAP07_SLOPE_A", "0x8EFC", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.FlatFloor | RmapPatternIntentTag.Takeoff | RmapPatternIntentTag.LeftEntry |
                RmapPatternIntentTag.RightExit | RmapPatternIntentTag.RequiredOk,
                "####", ".###", "..##", "...#");
            yield return Seed("RMAP07_SLOPE_B", "0xC8F7", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.StepFloor | RmapPatternIntentTag.RunApproach | RmapPatternIntentTag.Landing |
                RmapPatternIntentTag.HazardAllowed,
                "####", "..##", "...#", "....");
            yield return Seed("RMAP07_CEILING_FLAT", "0xF000", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.LowCeiling | RmapPatternIntentTag.OneTilePassage |
                RmapPatternIntentTag.MechanismAllowed,
                "....", "....", "....", "####");
            yield return Seed("RMAP07_CEILING_ROUGH", "0xDB00", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.LowCeiling | RmapPatternIntentTag.SecretShell |
                RmapPatternIntentTag.RewardAllowed,
                "....", "....", "##.#", "####");
            yield return Seed("RMAP07_WALL_A", "0x111F", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.GrabbableCorner | RmapPatternIntentTag.LeftEntry |
                RmapPatternIntentTag.LadderAllowed,
                "#...", "#...", "##..", "#...");
            yield return Seed("RMAP07_WALL_B", "0x888F", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.GrabbableCorner | RmapPatternIntentTag.RightExit |
                RmapPatternIntentTag.PillarAllowed,
                "...#", "...#", "..##", "...#");
            yield return Seed("RMAP07_VERTICAL", "0x9999", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.VerticalClearance | RmapPatternIntentTag.UpEntry |
                RmapPatternIntentTag.DownExit | RmapPatternIntentTag.LadderAllowed | RmapPatternIntentTag.PillarAllowed,
                "#..#", "#..#", "#..#", "#..#");
            yield return Seed("RMAP07_LEDGE_A", "0x0037", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.ShortLedge | RmapPatternIntentTag.Landing | RmapPatternIntentTag.RequiredOk,
                ".###", "..#.", "....", "....");
            yield return Seed("RMAP07_LEDGE_B", "0x00EF", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.LongLedge | RmapPatternIntentTag.RunApproach | RmapPatternIntentTag.OptionalOnly,
                "####", ".#..", "....", "....");
            yield return Seed("RMAP07_ONEWAY_A", "0x0000", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.Takeoff | RmapPatternIntentTag.Landing | RmapPatternIntentTag.RequiredOk |
                RmapPatternIntentTag.RewardAllowed,
                ".==.", "....", "....", "....");
            yield return Seed("RMAP07_ONEWAY_B", "0x0000", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.ShortLedge | RmapPatternIntentTag.OneTilePassage | RmapPatternIntentTag.SecretShell,
                "....", "..=.", ".=..", "....");
            // Same geometry, incompatible authored use.  The catalog preserves
            // both origins and emits a conflict instead of granting a use case.
            yield return Seed("RMAP07_LEDGE_CONFLICT", "0x0037", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.ShortLedge | RmapPatternIntentTag.SecretShell,
                ".###", "..#.", "....", "....");
            yield return Seed("RMAP07_COMPLEX_A", "0x4EF3", "RMAP07_AUTHORED_TYPED_V1",
                RmapPatternIntentTag.StepFloor | RmapPatternIntentTag.GrabbableCorner |
                RmapPatternIntentTag.HazardAllowed | RmapPatternIntentTag.MechanismAllowed,
                "##..", ".##.", "...#", "..##");
        }

        private static RmapPatternSource Seed(string id, string mask, string reference,
            RmapPatternIntentTag tags, params string[] rowsBottomToTop)
        {
            if (rowsBottomToTop == null || rowsBottomToTop.Length != Height || rowsBottomToTop.Any(row => row == null || row.Length != Width))
                throw new ArgumentException("Seed rows must be four four-cell rows.");
            return new RmapPatternSource(id, mask, reference, tags, rowsBottomToTop.SelectMany(row => row.Select(ParseToken)));
        }

        private static bool IsVerticalPassage(IReadOnlyList<RmapPatternBaseCell> cells) =>
            IsFullColumn(cells, 0) && IsFullColumn(cells, Width - 1) &&
            Enumerable.Range(0, Height).All(y => Cell(cells, 1, y) == RmapPatternBaseCell.Air &&
                Cell(cells, 2, y) == RmapPatternBaseCell.Air);

        private static bool IsFullColumn(IReadOnlyList<RmapPatternBaseCell> cells, int x) =>
            Enumerable.Range(0, Height).All(y => Cell(cells, x, y) == RmapPatternBaseCell.Solid);

        private static int SlopeDirection(IReadOnlyList<RmapPatternBaseCell> cells)
        {
            int[] heights = Enumerable.Range(0, Width).Select(x => SolidHeight(cells, x)).ToArray();
            bool rising = heights.Zip(heights.Skip(1), (left, right) => right >= left).All(value => value) &&
                heights.Distinct().Count() >= 3;
            bool falling = heights.Zip(heights.Skip(1), (left, right) => right <= left).All(value => value) &&
                heights.Distinct().Count() >= 3;
            return rising ? 1 : falling ? -1 : 0;
        }

        private static int SolidHeight(IReadOnlyList<RmapPatternBaseCell> cells, int x)
        {
            var height = 0;
            while (height < Height && Cell(cells, x, height) == RmapPatternBaseCell.Solid) height++;
            return height;
        }

        private static bool IsFlatCeiling(IReadOnlyList<RmapPatternBaseCell> cells) =>
            Enumerable.Range(0, Width).All(x => Cell(cells, x, Height - 1) == RmapPatternBaseCell.Solid) &&
            Enumerable.Range(0, Width).All(x => Cell(cells, x, Height - 2) == RmapPatternBaseCell.Air);

        private static int TopCount(IReadOnlyList<RmapPatternBaseCell> cells) =>
            Enumerable.Range(0, Width).Count(x => Cell(cells, x, Height - 1) == RmapPatternBaseCell.Solid);

        private static int BottomCount(IReadOnlyList<RmapPatternBaseCell> cells) =>
            Enumerable.Range(0, Width).Count(x => Cell(cells, x, 0) == RmapPatternBaseCell.Solid);

        private static IEnumerable<RmapPatternStandableSpan> FindStandableSpans(IReadOnlyList<RmapPatternBaseCell> cells)
        {
            for (var y = 0; y < Height; y++)
            {
                var start = -1;
                for (var x = 0; x <= Width; x++)
                {
                    bool standable = x < Width && IsStandable(Cell(cells, x, y)) &&
                        (y == Height - 1 || Cell(cells, x, y + 1) == RmapPatternBaseCell.Air);
                    if (standable && start < 0) start = x;
                    if ((!standable || x == Width) && start >= 0)
                    {
                        int end = x - 1;
                        int headroom = 0;
                        while (y + 1 + headroom < Height && Enumerable.Range(start, end - start + 1)
                            .All(column => Cell(cells, column, y + 1 + headroom) == RmapPatternBaseCell.Air)) headroom++;
                        yield return new RmapPatternStandableSpan(y, start, end, headroom,
                            y + 1 + headroom >= Height);
                        start = -1;
                    }
                }
            }
        }

        private static bool HasExposedSolidCorner(IReadOnlyList<RmapPatternBaseCell> cells, int x, int y)
        {
            bool leftAir = x == 0 || Cell(cells, x - 1, y) == RmapPatternBaseCell.Air;
            bool rightAir = x == Width - 1 || Cell(cells, x + 1, y) == RmapPatternBaseCell.Air;
            bool aboveAir = y == Height - 1 || Cell(cells, x, y + 1) == RmapPatternBaseCell.Air;
            return aboveAir && (leftAir || rightAir);
        }

        private static IEnumerable<RmapPatternAirComponent> FindAirComponents(IReadOnlyList<RmapPatternBaseCell> cells)
        {
            var remaining = new HashSet<RmapPatternLocalCell>(Enumerable.Range(0, Height).SelectMany(y =>
                Enumerable.Range(0, Width).Where(x => Cell(cells, x, y) == RmapPatternBaseCell.Air)
                .Select(x => new RmapPatternLocalCell(x, y))));
            while (remaining.Count > 0)
            {
                var queue = new Queue<RmapPatternLocalCell>();
                var component = new List<RmapPatternLocalCell>();
                RmapPatternLocalCell first = remaining.OrderBy(value => value.Y).ThenBy(value => value.X).First();
                queue.Enqueue(first);
                remaining.Remove(first);
                while (queue.Count > 0)
                {
                    RmapPatternLocalCell current = queue.Dequeue();
                    component.Add(current);
                    foreach (RmapPatternLocalCell next in Neighbors(current).Where(remaining.Contains).ToArray())
                    {
                        remaining.Remove(next);
                        queue.Enqueue(next);
                    }
                }
                yield return new RmapPatternAirComponent(component);
            }
        }

        private static IEnumerable<RmapPatternLocalCell> Neighbors(RmapPatternLocalCell cell)
        {
            foreach (var offset in new[] { new RmapPatternLocalCell(-1, 0), new RmapPatternLocalCell(1, 0),
                         new RmapPatternLocalCell(0, -1), new RmapPatternLocalCell(0, 1) })
            {
                int x = cell.X + offset.X;
                int y = cell.Y + offset.Y;
                if (x >= 0 && x < Width && y >= 0 && y < Height) yield return new RmapPatternLocalCell(x, y);
            }
        }

        private static IEnumerable<RmapPatternLocalCell> EdgeCoordinates(RmapPatternEdge edge)
        {
            switch (edge)
            {
                case RmapPatternEdge.Left: return Enumerable.Range(0, Height).Select(y => new RmapPatternLocalCell(0, y));
                case RmapPatternEdge.Right: return Enumerable.Range(0, Height).Select(y => new RmapPatternLocalCell(Width - 1, y));
                case RmapPatternEdge.Up: return Enumerable.Range(0, Width).Select(x => new RmapPatternLocalCell(x, Height - 1));
                case RmapPatternEdge.Down: return Enumerable.Range(0, Width).Select(x => new RmapPatternLocalCell(x, 0));
                default: throw new ArgumentOutOfRangeException(nameof(edge));
            }
        }

        private static void TransformTags(RmapPatternIntentTag source, RmapPatternTransform transform,
            out RmapPatternIntentTag result, out IReadOnlyList<string> unresolved)
        {
            result = source;
            var review = new List<string>();
            bool flipX = transform == RmapPatternTransform.MirrorX || transform == RmapPatternTransform.R180;
            bool flipY = transform == RmapPatternTransform.MirrorY || transform == RmapPatternTransform.R180;
            if (flipX)
            {
                RemoveUnrepresentable(ref result, RmapPatternIntentTag.LeftEntry, "RIGHT_ENTRY_UNREPRESENTABLE", review);
                RemoveUnrepresentable(ref result, RmapPatternIntentTag.RightExit, "LEFT_EXIT_UNREPRESENTABLE", review);
            }
            if (flipY)
            {
                RemoveUnrepresentable(ref result, RmapPatternIntentTag.UpEntry, "DOWN_ENTRY_UNREPRESENTABLE", review);
                RemoveUnrepresentable(ref result, RmapPatternIntentTag.DownExit, "UP_EXIT_UNREPRESENTABLE", review);
            }
            unresolved = review;
        }

        private static void RemoveUnrepresentable(ref RmapPatternIntentTag tags, RmapPatternIntentTag tag,
            string reason, ICollection<string> review)
        {
            if ((tags & tag) == 0) return;
            tags &= ~tag;
            review.Add(reason);
        }

        private static bool IsStandable(RmapPatternBaseCell value) =>
            value == RmapPatternBaseCell.Solid || value == RmapPatternBaseCell.OneWayPlatform;

        private static RmapPatternBaseCell Cell(IReadOnlyList<RmapPatternBaseCell> cells, int x, int y) =>
            cells[y * Width + x];

        private static void EnsureCells(IReadOnlyList<RmapPatternBaseCell> cells)
        {
            if (cells == null || cells.Count != CellCount || cells.Any(value => value < RmapPatternBaseCell.Air ||
                value > RmapPatternBaseCell.OneWayPlatform))
                throw new ArgumentException("Base geometry requires exactly sixteen AIR/SOLID/ONE_WAY_PLATFORM cells.");
        }

        private static bool SameCells(IReadOnlyList<RmapPatternBaseCell> left, IReadOnlyList<RmapPatternBaseCell> right) =>
            left.SequenceEqual(right);

        private static char ToToken(RmapPatternBaseCell value)
        {
            switch (value)
            {
                case RmapPatternBaseCell.Air: return 'A';
                case RmapPatternBaseCell.Solid: return 'S';
                case RmapPatternBaseCell.OneWayPlatform: return 'O';
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static RmapPatternBaseCell ParseToken(char value)
        {
            switch (value)
            {
                case '.': case 'A': return RmapPatternBaseCell.Air;
                case '#': case 'S': return RmapPatternBaseCell.Solid;
                case '=': case 'O': return RmapPatternBaseCell.OneWayPlatform;
                default: throw new FormatException("Unknown base cell token: " + value);
            }
        }

        private static string Tags(RmapPatternIntentTag tags) => tags == RmapPatternIntentTag.None
            ? string.Empty
            : string.Join(";", Enum.GetValues(typeof(RmapPatternIntentTag)).Cast<RmapPatternIntentTag>()
                .Where(value => value != RmapPatternIntentTag.None && (tags & value) == value));

        private static string Cells(IEnumerable<RmapPatternLocalCell> cells) => string.Join(";", (cells ??
            Array.Empty<RmapPatternLocalCell>()).OrderBy(value => value.Y).ThenBy(value => value.X));

        private static string Csv(params string[] fields) => string.Join(",", fields.Select(field => "\"" +
            (field ?? string.Empty).Replace("\"", "\"\"") + "\""));

        private static string[] SplitCsv(string row)
        {
            var values = new List<string>();
            var value = new StringBuilder();
            bool quoted = false;
            for (var index = 0; index < row.Length; index++)
            {
                char current = row[index];
                if (current == '"' && quoted && index + 1 < row.Length && row[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else if (current == '"') quoted = !quoted;
                else if (current == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
                else value.Append(current);
            }
            if (quoted) throw new FormatException("Unclosed CSV quote.");
            values.Add(value.ToString());
            return values.ToArray();
        }

        private sealed class PendingCandidate
        {
            public PendingCandidate(IReadOnlyList<RmapPatternBaseCell> cells)
            {
                Cells = cells;
                Origins = new List<RmapPatternOrigin>();
            }

            public IReadOnlyList<RmapPatternBaseCell> Cells { get; }
            public List<RmapPatternOrigin> Origins { get; }

            public RmapPatternCandidate ToCandidate()
            {
                var tags = Origins.Aggregate(RmapPatternIntentTag.None, (current, value) => current | value.IntentTags);
                var conflicts = new List<string>();
                bool required = Origins.Any(value => (value.IntentTags & RmapPatternIntentTag.RequiredOk) != 0);
                bool secret = Origins.Any(value => (value.IntentTags & RmapPatternIntentTag.SecretShell) != 0);
                if (required && secret) conflicts.Add("REQUIRED_OK_VS_SECRET_SHELL_REVIEW_REQUIRED");
                string id = "RMAP07_" + Hash(SerializeBaseCells16(Cells)).Substring(0, 12).ToUpperInvariant();
                return new RmapPatternCandidate(id, Cells, Classify(Cells), tags, Origins, conflicts,
                    CalculateCharacteristics(Cells));
            }
        }

        private static string Hash(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(byteValue =>
                    byteValue.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }
}
